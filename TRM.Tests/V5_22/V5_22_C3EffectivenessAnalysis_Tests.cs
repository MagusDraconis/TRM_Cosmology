using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_22;

[Trait("Category","V5_22"),Trait("Category","V5_22_IOA"),Trait("Category","LongRunning")]
public class V5_22_C3EffectivenessAnalysis_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153,RebT=0.01;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    struct C3Profile{
        public int n,s,cohort;public string cls,stateGroup,resonanceType;
        // Geometry
        public double distHi,distLo,projHiVec,orthHiVec,offVecAngle;
        // Pre-C3 state
        public double alignPre,dT1,kT1,omT1,respProjHi,respOrthHi;
        // C3 response
        public double c3OmegaShift,deltaAlign,c3DShift,c3KShift;
        public double shiftToHi,shiftFromLo;
        // Effectiveness ratio
        public double c3Effectiveness;
        // Outcome
        public bool a0,c3,induced,rescued,persistent,invalid;
    }

    public V5_22_C3EffectivenessAnalysis_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    C3Profile? BuildC3Profile(int n,int s,P3 hi,P3 lo){
        var p=new C3Profile{n=n,s=s,cohort=s/100};
        var sb=SelectAndClassify(n,s,hi);
        if(sb==null)return null;
        p.cls=sb.Value.cls;
        double d0=sb.Value.d0,km0=sb.Value.km0,ks0=sb.Value.ks0;

        double dv=hi.dm-lo.dm,kv=hi.km-lo.km,sv=hi.ks-lo.ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
        p.projHiVec=vn>0?((d0-lo.dm)*dv+(km0-lo.km)*kv+(ks0-lo.ks)*sv)/vn:0;
        double d2=(d0-lo.dm)*(d0-lo.dm)+(km0-lo.km)*(km0-lo.km)+(ks0-lo.ks)*(ks0-lo.ks);
        p.orthHiVec=Math.Sqrt(Math.Max(0,d2-p.projHiVec*p.projHiVec));
        double cNorm=Math.Sqrt(d2);
        p.offVecAngle=cNorm>1e-9&&vn>1e-9?Math.Acos(Math.Clamp(Math.Abs(p.projHiVec)/cNorm,-1,1)):Math.PI/2;
        p.distHi=Math.Sqrt((d0-hi.dm)*(d0-hi.dm)+(km0-hi.km)*(km0-hi.km)+(ks0-hi.ks)*(ks0-hi.ks));
        p.distLo=Math.Sqrt((d0-lo.dm)*(d0-lo.dm)+(km0-lo.km)*(km0-lo.km)+(ks0-lo.ks)*(ks0-lo.ks));

        // Frozen M3++ probe
        bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var K2=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K2=Cupd(d,n);}
        var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K2=Cupd(dM,n);
        var h5=Sim(K2,n,S,s+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K2,n,S,s+100);
        var dT1=DL(Nm(RP(hT1,n),n),n);p.dT1=Dm(dT1,n);p.kT1=Km(Cupd(dT1,n),n);p.omT1=Of(hT1,n).Average();

        // Pre-C3 alignment
        double dT1m=p.dT1,kT1m=p.kT1,ksT1m=Ks(Cupd(dT1,n),n);
        p.respProjHi=vn>0?((dT1m-lo.dm)*dv+(kT1m-lo.km)*kv+(ksT1m-lo.ks)*sv)/vn:0;
        double respD2=(dT1m-lo.dm)*(dT1m-lo.dm)+(kT1m-lo.km)*(kT1m-lo.km)+(ksT1m-lo.ks)*(ksT1m-lo.ks);
        p.respOrthHi=Math.Sqrt(Math.Max(0,respD2-p.respProjHi*p.respProjHi));
        p.alignPre=vn>0?((dT1m-lo.dm)*dv+(kT1m-lo.km)*kv)/vn:0;

        // T2
        var Kt2=Cupd(DL(Nm(RP(hT1,n),n),n),n);
        var hT2=Sim(Kt2,n,S,s+200);
        var dT2=DL(Nm(RP(hT2,n),n),n);double dT2m=Dm(dT2,n),kT2m=Km(Cupd(dT2,n),n),omT2=Of(hT2,n).Average();
        double rebMag=dT2m-p.dT1;p.a0=omT2>THR;

        // C3 correction
        if(!double.IsNaN(hi.dm)){
            var dmat3=DL(Nm(RP(hT1,n),n),n);double dmPre=Dm(dmat3,n),kmPre=Km(Cupd(dmat3,n),n);
            double nudged=dmPre+(hi.dm-dmPre)*0.2;
            double f3=Math.Clamp((nudged+1e-9)/(dmPre+1e-9),0.5,1.5);
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;
            var hc3=Sim(Cupd(dmat3,n),n,S,s+300);
            var Kc3c=Cupd(DL(Nm(RP(hc3,n),n),n),n);
            var hc3cc=Sim(Kc3c,n,S,s+400);
            double omC3=Of(hc3cc,n).Average();
            double dmPost=Dm(dmat3,n),kmPost=Km(Cupd(dmat3,n),n);

            p.c3OmegaShift=omC3-omT2;
            p.c3DShift=dmPost-dmPre;p.c3KShift=kmPost-kmPre;
            p.deltaAlign=vn>0?(((dmPost-lo.dm)*dv+(kmPost-lo.km)*kv)/vn-p.alignPre):0;
            p.shiftToHi=-(Math.Sqrt((dmPost-hi.dm)*(dmPost-hi.dm)+(kmPost-hi.km)*(kmPost-hi.km))-p.distHi);
            p.shiftFromLo=p.shiftToHi;
            p.c3Effectiveness=p.c3OmegaShift/Math.Max(1e-9,Math.Abs(p.alignPre)+0.001);
            p.c3=omC3>THR;
            var hCont=Sim(Cupd(DL(Nm(RP(hc3cc,n),n),n),n),n,S,s+500);
            p.persistent=p.c3&&Of(hCont,n).Average()>THR;
        }else{
            p.c3OmegaShift=0;p.deltaAlign=0;p.c3DShift=0;p.c3KShift=0;
            p.shiftToHi=0;p.shiftFromLo=0;p.c3Effectiveness=0;
            p.c3=p.a0;p.persistent=p.a0;
        }

        p.induced=p.a0||p.c3;p.rescued=p.c3&&!p.a0;p.invalid=double.IsNaN(hi.dm);

        // State group
        if(p.rescued&&p.n<=65)p.stateGroup="G4-onset-rescued";
        else if(p.rescued&&p.n>=70)p.stateGroup="G6-adaptive-rescued";
        else if(p.induced&&!p.rescued)p.stateGroup="G1-C3-effective";
        else if(p.n==64)p.stateGroup="G3-N64-failure";
        else if(p.n==65&&!p.induced)p.stateGroup="G5-N65-nonrescued";
        else if(p.n>=70&&!p.induced)p.stateGroup="G7-adaptive-nonrescued";
        else p.stateGroup="G2-ineffective";

        // Resonance type
        if(p.rescued){
            if(p.alignPre<-0.1&&p.c3OmegaShift>0.1&&Math.Abs(p.dT1)>0.5)p.resonanceType="resonant-reversal";
            else if(p.alignPre>0.05&&p.c3OmegaShift>0.05)p.resonanceType="direct-alignment";
            else if(p.c3OmegaShift<0.05)p.resonanceType="weak-correction";
            else p.resonanceType="unclear";
        }else{p.resonanceType="none";}

        return p;
    }

    // ═══════════════════════════════════════════════
    // IOA_01 — C3-effective vs ineffective
    // ═══════════════════════════════════════════════
    [Fact]public void IOA_01_C3EffectiveVsIneffective(){
        _o.WriteLine("═══ IOA_01: C3-effective vs ineffective seeds ═══");
        _o.WriteLine("What separates seeds where C3 works from those where it fails?");

        int[] Ns={64,65,66,67,70,72};
        var profiles=new ConcurrentBag<C3Profile>();

        Parallel.ForEach(Ns,n=>{
            var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<399;s++){
                if(IsHi(n,s))continue;
                var p=BuildC3Profile(n,s,hi,lo);
                if(p==null)continue;
                profiles.Add(p.Value);
            }
        });

        var all=profiles.ToArray();
        var effective=all.Where(p=>p.induced&&p.n>=65).ToArray();
        var ineffective=all.Where(p=>!p.induced&&p.n>=65).ToArray();

        _o.WriteLine($"\nC3-effective: {effective.Length}, C3-ineffective: {ineffective.Length}");

        // Pre-C3 state comparison
        _o.WriteLine($"\n─── Pre-C3 state: effective vs ineffective ───");
        _o.WriteLine(string.Format("{0,-16} {1,10} {2,10} {3,8}",
            "Metric","Effective","Ineffective","Ratio"));
        string[] metrics={"alignPre","dT1","kT1","omT1","respProjHi","respOrthHi","distHi","projHiVec","offVecAngle"};
        foreach(var m in metrics){
            double ev=GetMean(effective,m),iv=GetMean(ineffective,m);
            double r=Math.Abs(ev)>1e-9?iv/Math.Max(1e-9,Math.Abs(ev)):0;
            _o.WriteLine($"{m,-16} {ev,10:F4} {iv,10:F4} {r,8:F2}x");
        }

        // C3 response comparison
        _o.WriteLine($"\n─── C3 response: effective vs ineffective ───");
        string[] c3m={"c3OmegaShift","deltaAlign","c3DShift","c3KShift","c3Effectiveness","shiftToHi"};
        foreach(var m in c3m){
            double ev=GetMean(effective,m),iv=GetMean(ineffective,m);
            double r=iv>1e-9?ev/iv:ev>1e-9?999:0;
            _o.WriteLine($"{m,-16} {ev,10:F4} {iv,10:F4} {r,8:F1}x");
        }

        // Per-N breakdown
        _o.WriteLine($"\n─── Per-N C3 effectiveness ───");
        _o.WriteLine(string.Format("{0,4} {1,6} {2,6} {3,8} {4,8} {5,8}",
            "N","Eff","Ineff","avgC3Omg","avgDisp","avgAPre"));
        foreach(var n in Ns.Where(nn=>nn>=65)){
            var sub=all.Where(p=>p.n==n).ToArray();
            var eff=sub.Where(p=>p.induced).ToArray();
            var ineff=sub.Where(p=>!p.induced).ToArray();
            if(sub.Length==0)continue;
            _o.WriteLine($"{n,4} {eff.Length,6} {ineff.Length,6} {sub.Average(p=>p.c3OmegaShift),8:F4} {sub.Average(p=>p.dT1),8:F4} {sub.Average(p=>p.alignPre),8:F4}");
        }

        _o.WriteLine($"\n─── Next: IOA_02 N=64 failure ───");
    }

    // ═══════════════════════════════════════════════
    // IOA_02 — N=64 failure analysis
    // ═══════════════════════════════════════════════
    [Fact]public void IOA_02_N64FailureAnalysis(){
        _o.WriteLine("═══ IOA_02: N=64 failure — why no resonant reversal? ═══");

        int[] Ns={64,65};
        var profiles=new ConcurrentBag<C3Profile>();

        Parallel.ForEach(Ns,n=>{
            var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<399;s++){
                if(IsHi(n,s))continue;
                var p=BuildC3Profile(n,s,hi,lo);
                if(p==null)continue;
                profiles.Add(p.Value);
            }
        });

        var all=profiles.ToArray();
        var n64=all.Where(p=>p.n==64).ToArray();
        var n65rescued=all.Where(p=>p.n==65&&p.rescued).ToArray();
        var n65failed=all.Where(p=>p.n==65&&!p.induced).ToArray();

        _o.WriteLine($"\nN=64: {n64.Length}, N=65 rescued: {n65rescued.Length}, failed: {n65failed.Length}");

        // Full metric comparison
        _o.WriteLine($"\n─── N=64 vs N=65 rescued vs N=65 failed ───");
        _o.WriteLine(string.Format("{0,-14} {1,10} {2,10} {3,10} {4,10}",
            "Metric","N=64","N=65 Resc","N=65 Fail","64→65R"));
        string[] allM={"alignPre","dT1","c3OmegaShift","deltaAlign","omT1","kT1","distHi","projHiVec","offVecAngle","c3Effectiveness"};
        foreach(var m in allM){
            double v64=GetMean(n64,m),v65r=GetMean(n65rescued,m),v65f=GetMean(n65failed,m);
            double ratio=Math.Abs(v64)>1e-9?v65r/Math.Abs(v64):999;
            _o.WriteLine($"{m,-14} {v64,10:F4} {v65r,10:F4} {v65f,10:F4} {ratio,10:F1}x");
        }

        // Gap analysis: what's missing at N=64?
        _o.WriteLine($"\n─── Gap analysis: what N=64 lacks ───");
        var gaps=new List<(string metric,double gap)>();
        foreach(var m in allM){
            double v64=GetMean(n64,m),v65r=GetMean(n65rescued,m);
            double gap=Math.Abs(v65r-v64)/Math.Max(1e-9,Math.Max(Math.Abs(v64),Math.Abs(v65r)));
            gaps.Add((m,gap));
        }
        foreach(var g in gaps.OrderByDescending(x=>x.gap).Take(5)){
            double v64=GetMean(n64,g.metric),v65r=GetMean(n65rescued,g.metric);
            _o.WriteLine($"  {g.metric}: N=64={v64:F4} → N=65r={v65r:F4} (gap={g.gap:F2})");
        }

        // Does N=64 have any seed meeting resonant reversal criteria?
        double rrCount=n64.Count(p=>p.alignPre<-0.1&&p.dT1>0.5);
        _o.WriteLine($"\nN=64 seeds meeting resonant reversal preconditions: {rrCount}/{n64.Length} ({rrCount*100.0/n64.Length:F0}%)");
        _o.WriteLine($"  (alignPre<-0.1 && dT1>0.5)");

        // N=64 max c3OmegaShift
        double max64=n64.Max(p=>p.c3OmegaShift);
        double max64dT1=n64.Max(p=>p.dT1);
        _o.WriteLine($"N=64 max c3OmegaShift: {max64:F4}, max dT1: {max64dT1:F4}");

        // N=64 best-case C3 effectiveness
        var best64=n64.OrderByDescending(p=>p.c3OmegaShift).Take(5).ToArray();
        _o.WriteLine($"\nN=64 top 5 c3OmegaShift seeds:");
        _o.WriteLine(string.Format("{0,4} {1,8} {2,8} {3,8}","seed","c3OmgS","dT1","alignPre"));
        foreach(var p in best64)
            _o.WriteLine($"{p.s,4} {p.c3OmegaShift,8:F4} {p.dT1,8:F4} {p.alignPre,8:F4}");

        _o.WriteLine($"\n─── Next: IOA_03 Resonant reversal profile ───");
    }

    // ═══════════════════════════════════════════════
    // IOA_03 — Resonant reversal profile
    // ═══════════════════════════════════════════════
    [Fact]public void IOA_03_ResonantReversalProfile(){
        _o.WriteLine("═══ IOA_03: Resonant reversal profile definition ═══");

        int[] Ns={65,66,70,72};
        var profiles=new ConcurrentBag<C3Profile>();

        Parallel.ForEach(Ns,n=>{
            var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<399;s++){
                if(IsHi(n,s))continue;
                var p=BuildC3Profile(n,s,hi,lo);
                if(p==null)continue;
                profiles.Add(p.Value);
            }
        });

        var all=profiles.ToArray();
        var rescued=all.Where(p=>p.rescued).ToArray();
        var notRescued=all.Where(p=>!p.rescued&&!p.a0).ToArray();

        _o.WriteLine($"\nRescued: {rescued.Length}, Not rescued: {notRescued.Length}");

        // Resonant reversal criteria distribution
        _o.WriteLine($"\n─── Resonance type distribution ───");
        foreach(var rt in new[]{"resonant-reversal","direct-alignment","weak-correction","unclear"}){
            int cnt=rescued.Count(p=>p.resonanceType==rt);
            _o.WriteLine($"{rt}: {cnt}/{rescued.Length} ({cnt*100.0/rescued.Length:F0}%)");
        }

        // Profile of resonant reversal seeds
        var rr=rescued.Where(p=>p.resonanceType=="resonant-reversal").ToArray();
        _o.WriteLine($"\n─── Resonant reversal profile ({rr.Length} seeds) ───");
        _o.WriteLine(string.Format("{0,-14} {1,10} {2,10} {3,10}",
            "Metric","Mean","Min","Max"));
        string[] rrM={"alignPre","dT1","c3OmegaShift","deltaAlign","omT1","kT1","projHiVec","offVecAngle"};
        foreach(var m in rrM){
            double mn=GetMean(rr,m),minV=rr.Min(p=>GetVal(p,m)),maxV=rr.Max(p=>GetVal(p,m));
            _o.WriteLine($"{m,-14} {mn,10:F4} {minV,10:F4} {maxV,10:F4}");
        }

        // Ranges for onset profile
        if(rr.Length>0){
            _o.WriteLine($"\n─── Resonant reversal ranges ───");
            _o.WriteLine($"alignPre:     [{rr.Min(p=>p.alignPre):F3}, {rr.Max(p=>p.alignPre):F3}]");
            _o.WriteLine($"dT1:          [{rr.Min(p=>p.dT1):F3}, {rr.Max(p=>p.dT1):F3}]");
            _o.WriteLine($"c3OmegaShift: [{rr.Min(p=>p.c3OmegaShift):F3}, {rr.Max(p=>p.c3OmegaShift):F3}]");
            _o.WriteLine($"deltaAlign:   [{rr.Min(p=>p.deltaAlign):F3}, {rr.Max(p=>p.deltaAlign):F3}]");
        }

        // Non-resonant rescues
        var nonRR=rescued.Where(p=>p.resonanceType!="resonant-reversal").ToArray();
        if(nonRR.Length>0){
            _o.WriteLine($"\n─── Non-resonant rescues ({nonRR.Length}) ───");
            _o.WriteLine($"Mean alignPre: {nonRR.Average(p=>p.alignPre):F4}");
            _o.WriteLine($"Mean c3OmegaShift: {nonRR.Average(p=>p.c3OmegaShift):F4}");
            _o.WriteLine($"Mean dT1: {nonRR.Average(p=>p.dT1):F4}");
        }

        _o.WriteLine($"\n─── Next: IOA_04 C3 threshold ───");
    }

    // ═══════════════════════════════════════════════
    // IOA_04 — C3 effectiveness threshold
    // ═══════════════════════════════════════════════
    [Fact]public void IOA_04_C3EffectivenessThreshold(){
        _o.WriteLine("═══ IOA_04: C3 effectiveness threshold ═══");

        int[] Ns={64,65,66,67,70,72};
        var profiles=new ConcurrentBag<C3Profile>();

        Parallel.ForEach(Ns,n=>{
            var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<399;s++){
                if(IsHi(n,s))continue;
                var p=BuildC3Profile(n,s,hi,lo);
                if(p==null)continue;
                profiles.Add(p.Value);
            }
        });

        var all=profiles.ToArray();
        var valid=all.Where(p=>!p.invalid).ToArray();

        // Use N=65 as reference to find best threshold
        var refSet=valid.Where(p=>p.n==65).ToArray();
        var holdout=valid.Where(p=>p.n!=65).ToArray();

        _o.WriteLine($"\nReference (N=65): {refSet.Length}, Holdout: {holdout.Length}");

        // Test simple thresholds
        _o.WriteLine($"\n─── Threshold candidates (N=65 reference) ───");
        var thresholds=new[]{
            ("c3OmegaShift>0.05",(Func<C3Profile,bool>)(p=>p.c3OmegaShift>0.05)),
            ("c3OmegaShift>0.10",p=>p.c3OmegaShift>0.10),
            ("dT1>0.50",p=>p.dT1>0.50),
            ("dT1>0.80",p=>p.dT1>0.80),
            ("alignPre<0",p=>p.alignPre<0),
            ("alignPre<-0.2",p=>p.alignPre<-0.2),
            ("deltaAlign>0.05",p=>p.deltaAlign>0.05),
            ("c3Effectiveness>1.0",p=>p.c3Effectiveness>1.0),
            ("dT1>0.5 & c3OmgS>0.1",p=>p.dT1>0.5&&p.c3OmegaShift>0.1),
        };

        _o.WriteLine(string.Format("{0,-28} {1,6} {2,6} {3,8} {4,8}",
            "Threshold","RefAcc","HoAcc","RefPrec","HoPrec"));
        foreach(var (label,fn) in thresholds){
            int refCorrect=refSet.Count(p=>fn(p)==p.rescued);
            int hoCorrect=holdout.Count(p=>fn(p)==p.rescued);
            int refPred=refSet.Count(fn);
            int hoPred=holdout.Count(fn);
            double refAcc=refCorrect*100.0/refSet.Length;
            double hoAcc=hoCorrect*100.0/holdout.Length;
            double refPrec=refPred>0?refSet.Count(p=>fn(p)&&p.rescued)*100.0/refPred:0;
            double hoPrec=hoPred>0?holdout.Count(p=>fn(p)&&p.rescued)*100.0/hoPred:0;
            _o.WriteLine($"{label,-28} {refAcc,5:F0}% {hoAcc,5:F0}% {refPrec,7:F0}% {hoPrec,7:F0}%");
        }

        // Best threshold
        _o.WriteLine($"\n─── Best threshold ───");
        var best=thresholds.OrderByDescending(t=>{
            int c=refSet.Count(p=>t.Item2(p)==p.rescued);
            return c*100.0/refSet.Length;
        }).First();
        _o.WriteLine($"Best: {best.Item1}");

        _o.WriteLine($"\n─── Next: IOA_05 Cross-N validation ───");
    }

    // ═══════════════════════════════════════════════
    // IOA_05 — Cross-N validation
    // ═══════════════════════════════════════════════
    [Fact]public void IOA_05_CrossNValidation(){
        _o.WriteLine("═══ IOA_05: Cross-N validation — does C3 signature generalize? ═══");

        int[] Ns={65,66,67,70,72};
        var profiles=new ConcurrentBag<C3Profile>();

        Parallel.ForEach(Ns,n=>{
            var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<399;s++){
                if(IsHi(n,s))continue;
                var p=BuildC3Profile(n,s,hi,lo);
                if(p==null)continue;
                profiles.Add(p.Value);
            }
        });

        var all=profiles.ToArray();

        // Per-N C3 signature
        _o.WriteLine($"\n─── Per-N C3 signature ───");
        _o.WriteLine(string.Format("{0,4} {1,6} {2,6} {3,8} {4,8} {5,8} {6,8} {7,8}",
            "N","n","Resc%","c3OmgS","dT1","alignPre","c3Eff","deltaAl"));
        foreach(var n in Ns){
            var sub=all.Where(p=>p.n==n).ToArray();
            var rescued=sub.Where(p=>p.rescued).ToArray();
            if(sub.Length==0)continue;
            _o.WriteLine($"{n,4} {sub.Length,6} {rescued.Length*100.0/sub.Length,5:F0}% {sub.Average(p=>p.c3OmegaShift),8:F4} {sub.Average(p=>p.dT1),8:F4} {sub.Average(p=>p.alignPre),8:F4} {sub.Average(p=>p.c3Effectiveness),8:F4} {sub.Average(p=>p.deltaAlign),8:F4}");
        }

        // Does N=65 resonant reversal predict N=70/72 rescues?
        var n65rr=all.Where(p=>p.n==65&&p.resonanceType=="resonant-reversal").ToArray();
        var n70rescued=all.Where(p=>p.n==70&&p.rescued).ToArray();
        var n72rescued=all.Where(p=>p.n==72&&p.rescued).ToArray();

        _o.WriteLine($"\n─── Resonance type by N ───");
        foreach(var n in Ns){
            var rescued=all.Where(p=>p.n==n&&p.rescued).ToArray();
            if(rescued.Length==0)continue;
            int rrCount=rescued.Count(p=>p.resonanceType=="resonant-reversal");
            int daCount=rescued.Count(p=>p.resonanceType=="direct-alignment");
            _o.WriteLine($"N={n}: resonant-reversal={rrCount} direct-alignment={daCount}/{rescued.Length}");
        }

        // Cross-N metric comparison for rescued seeds
        _o.WriteLine($"\n─── Rescued seed metrics across N ───");
        _o.WriteLine(string.Format("{0,4} {1,6} {2,8} {3,8} {4,8} {5,8}",
            "N","n","c3OmgS","dT1","alignPre","deltaAl"));
        foreach(var n in Ns){
            var res=all.Where(p=>p.n==n&&p.rescued).ToArray();
            if(res.Length==0)continue;
            _o.WriteLine($"{n,4} {res.Length,6} {res.Average(p=>p.c3OmegaShift),8:F4} {res.Average(p=>p.dT1),8:F4} {res.Average(p=>p.alignPre),8:F4} {res.Average(p=>p.deltaAlign),8:F4}");
        }

        _o.WriteLine($"\n─── Next: IOA_06 Resonance classification ───");
    }

    // ═══════════════════════════════════════════════
    // IOA_06 — Resonant reversal classification
    // ═══════════════════════════════════════════════
    [Fact]public void IOA_06_ResonantReversalClassification(){
        _o.WriteLine("═══ IOA_06: Resonant reversal classification ═══");

        int[] Ns={65,66,67,70,72};
        var profiles=new ConcurrentBag<C3Profile>();

        Parallel.ForEach(Ns,n=>{
            var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<399;s++){
                if(IsHi(n,s))continue;
                var p=BuildC3Profile(n,s,hi,lo);
                if(p==null)continue;
                profiles.Add(p.Value);
            }
        });

        var all=profiles.ToArray();
        var rescued=all.Where(p=>p.rescued).ToArray();

        _o.WriteLine($"\nTotal rescued seeds: {rescued.Length}");

        // Individual rescued seeds
        _o.WriteLine($"\n─── All rescued seeds ───");
        _o.WriteLine(string.Format("{0,4} {1,4} {2,-18} {3,8} {4,8} {5,8} {6,8}",
            "N","seed","type","c3OmgS","dT1","alignPre","deltaAl"));
        foreach(var p in rescued.OrderBy(x=>x.n).ThenBy(x=>x.s))
            _o.WriteLine($"{p.n,4} {p.s,4} {p.resonanceType,-18} {p.c3OmegaShift,8:F4} {p.dT1,8:F4} {p.alignPre,8:F4} {p.deltaAlign,8:F4}");

        // Summary
        _o.WriteLine($"\n─── Classification summary ───");
        var types=rescued.GroupBy(p=>p.resonanceType).OrderByDescending(g=>g.Count());
        foreach(var g in types)
            _o.WriteLine($"{g.Key}: {g.Count()} ({g.Count()*100.0/rescued.Length:F0}%)");

        _o.WriteLine($"\n─── Next: IOA_07 Driver ranking ───");
    }

    // ═══════════════════════════════════════════════
    // IOA_07 — Final driver ranking
    // ═══════════════════════════════════════════════
    [Fact]public void IOA_07_FinalDriverRanking(){
        _o.WriteLine("═══ IOA_07: Final C3-effectiveness driver ranking ═══");

        int[] Ns={65,66,67,70,72};
        var profiles=new ConcurrentBag<C3Profile>();

        Parallel.ForEach(Ns,n=>{
            var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<399;s++){
                if(IsHi(n,s))continue;
                var p=BuildC3Profile(n,s,hi,lo);
                if(p==null)continue;
                profiles.Add(p.Value);
            }
        });

        var all=profiles.ToArray();
        var rescued=all.Where(p=>p.rescued).ToArray();
        var failed=all.Where(p=>!p.rescued&&!p.a0).ToArray();

        _o.WriteLine($"\nRescued: {rescued.Length}, Failed: {failed.Length}");

        // Driver ranking
        var drivers=new List<(string label,double rescuedMean,double failedMean,double ratio)>();
        var dm=new[]{("c3OmegaShift","C3 omega shift"),
            ("dT1","Probe displacement"),("alignPre","Pre-C3 alignment"),
            ("deltaAlign","C3 deltaAlign"),("c3Effectiveness","C3 effectiveness ratio"),
            ("shiftToHi","Shift toward Hi"),("omT1","Omega at T1"),
            ("kT1","K at T1"),("distHi","Distance to Hi")};

        foreach(var (key,label) in dm){
            double rMean=Math.Abs(GetMean(rescued,key)),fMean=Math.Abs(GetMean(failed,key));
            double ratio=fMean>1e-9?rMean/fMean:rMean>1e-9?999:1;
            drivers.Add((label,rMean,fMean,ratio));
        }

        _o.WriteLine(string.Format("{0,3} {1,-22} {2,10} {3,10} {4,8}",
            "","Driver","Rescued","Failed","Ratio"));
        int rank=1;
        foreach(var d in drivers.OrderByDescending(x=>x.ratio)){
            string marker=rank<=3?"←":"";
            _o.WriteLine($"{rank,2}. {d.label,-19} {d.rescuedMean,10:F4} {d.failedMean,10:F4} {d.ratio,8:F1}x {marker}");
            rank++;
        }

        // Gate assessment
        var top=drivers.OrderByDescending(x=>x.ratio).First();
        _o.WriteLine($"\nTop driver: {top.label} (×{top.ratio:F1})");
        bool c3Dom=top.label.Contains("C3");
        _o.WriteLine($"Gate A (C3 driver confirmed): {(c3Dom?"REACHED":"NOT REACHED")}");

        // Resonant reversal consistency
        bool rrDefined=rescued.Any(p=>p.resonanceType=="resonant-reversal");
        _o.WriteLine($"Gate B (Resonant reversal defined): {(rrDefined?"REACHED":"NOT REACHED")}");

        // N=64 failure explained
        _o.WriteLine($"Gate C (N=64 failure): assessing from IOA_02 data");

        // Cross-N check
        bool crossN=rescued.Where(p=>p.n>=70).Any(p=>p.resonanceType=="resonant-reversal");
        _o.WriteLine($"Gate E (Cross-N generalization): {(crossN?"REACHED":"NOT REACHED")}");
        bool n65Only=!crossN&&rescued.Any(p=>p.n==65&&p.resonanceType=="resonant-reversal");
        _o.WriteLine($"Gate F (N=65 unique mechanism): {(n65Only?"REACHED":"NOT REACHED")}");

        _o.WriteLine($"\n─── Analysis complete ───");
    }

    // ─── Helpers ───
    static double GetMean(C3Profile[] ps,string m)=>ps.Length==0?0:m switch{
        "alignPre"=>ps.Average(p=>p.alignPre),"dT1"=>ps.Average(p=>p.dT1),
        "kT1"=>ps.Average(p=>p.kT1),"omT1"=>ps.Average(p=>p.omT1),
        "respProjHi"=>ps.Average(p=>p.respProjHi),"respOrthHi"=>ps.Average(p=>p.respOrthHi),
        "distHi"=>ps.Average(p=>p.distHi),"projHiVec"=>ps.Average(p=>p.projHiVec),
        "offVecAngle"=>ps.Average(p=>p.offVecAngle),
        "c3OmegaShift"=>ps.Average(p=>p.c3OmegaShift),"deltaAlign"=>ps.Average(p=>p.deltaAlign),
        "c3DShift"=>ps.Average(p=>p.c3DShift),"c3KShift"=>ps.Average(p=>p.c3KShift),
        "c3Effectiveness"=>ps.Average(p=>p.c3Effectiveness),
        "shiftToHi"=>ps.Average(p=>p.shiftToHi),"_"=>0
    };

    static double GetVal(C3Profile p,string m)=>m switch{
        "alignPre"=>p.alignPre,"dT1"=>p.dT1,"kT1"=>p.kT1,"omT1"=>p.omT1,
        "respProjHi"=>p.respProjHi,"respOrthHi"=>p.respOrthHi,
        "distHi"=>p.distHi,"projHiVec"=>p.projHiVec,"offVecAngle"=>p.offVecAngle,
        "c3OmegaShift"=>p.c3OmegaShift,"deltaAlign"=>p.deltaAlign,
        "c3DShift"=>p.c3DShift,"c3KShift"=>p.c3KShift,
        "c3Effectiveness"=>p.c3Effectiveness,"shiftToHi"=>p.shiftToHi,_=>0
    };

    // ─── Frozen M3++ infrastructure ───
    SBase? SelectAndClassify(int n,int s,P3 hi){
        var K=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        var h3=Sim(K,n,S,s+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);
        var h3E=Sim(K3,n,S,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
        double d0=Dm(d3E,n),km=Km(K3E,n),ks=Ks(K3E,n);
        var sb=new SBase{seed=s,d0=d0,km0=km,ks0=ks,cls=""};sb=Classify(sb,hi);
        double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,sv=hi.ks-Lo(n).ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
        double proj=vn>0&&!double.IsNaN(vn)?((d0-Lo(n).dm)*dv+(km-Lo(n).km)*kv+(ks-Lo(n).ks)*sv)/vn:0;
        double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(km-Lo(n).km)*(km-Lo(n).km)+(ks-Lo(n).ks)*(ks-Lo(n).ks);
        double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));
        bool isP1=sb.cls=="P1"||sb.cls=="P1b";
        if(!(n==72?isP1&&proj>PHV&&orth>OTH:isP1&&proj>PHV))return null;
        return sb;
    }

    static double[][]Sim(double[,]K,int n,double s,int seed){var rng=new Random(seed);var w=new double[n];for(int i=0;i<n;i++)w[i]=1.0+s*(rng.NextDouble()-0.5)*2.0;var th=new double[n];for(int i=0;i<n;i++)th[i]=rng.NextDouble()*2*Math.PI;int hL=St/Hd+1;var h=new double[hL][];h[0]=(double[])th.Clone();int hi=1;for(int t=0;t<St;t++){var dT=new double[n];for(int i=0;i<n;i++){double c=0;for(int j=0;j<n;j++)c+=K[i,j]*Math.Sin(th[j]-th[i]);dT[i]=w[i]+c;}for(int i=0;i<n;i++)th[i]+=Dt*dT[i];if((t+1)%Hd==0&&hi<hL)h[hi++]=(double[])th.Clone();}return h;}
    static double[,]RP(double[][]h,int n){int T=h.Length;var R=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++){double sc=0,ss=0;for(int t=0;t<T;t++){double d=h[t][i]-h[t][j];sc+=Math.Cos(d);ss+=Math.Sin(d);}R[i,j]=Math.Sqrt(sc*sc+ss*ss)/T;}return R;}
    static double[,]Nm(double[,]R,int n){double mn=double.MaxValue;for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j&&R[i,j]<mn)mn=R[i,j];double rng=1.0-mn;if(rng<1e-15)rng=1.0;var Rn=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Rn[i,j]=i==j?1.0:Math.Max(REps,(R[i,j]-mn)/rng);return Rn;}
    static double[,]DL(double[,]R,int n){var d=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)d[i,j]=i==j?0:-Math.Log(Math.Max(R[i,j],1e-100));return d;}
    static double[,]Cupd(double[,]d,int n){var K=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)K[i,j]=i==j?0:K0*Math.Exp(-d[i,j]/Math.Max(Xi,0.01));return K;}
    static double[]Of(double[][]h,int n){int T=h.Length;var o=new double[n];for(int i=0;i<n;i++){double su=0;int c=0;for(int t=1;t<T;t++){su+=Math.Abs(h[t][i]-h[t-1][i]);c++;}o[i]=c>0?su/(c*Dt*Hd):0;}return o;}
    static double[,]KS(int n,int seed){var rng=new Random(seed);var adj=new HashSet<int>[n];for(int i=0;i<n;i++)adj[i]=new HashSet<int>();double p=6.0/(n-1);for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)if(rng.NextDouble()<p){adj[i].Add(j);adj[j].Add(i);}var v=new bool[n];var cs=new List<List<int>>();for(int i=0;i<n;i++){if(v[i])continue;var c=new List<int>();var q=new Queue<int>();v[i]=true;q.Enqueue(i);while(q.Count>0){int u=q.Dequeue();c.Add(u);foreach(int x in adj[u])if(!v[x]){v[x]=true;q.Enqueue(x);}}cs.Add(c);}for(int i=1;i<cs.Count;i++){adj[cs[i][0]].Add(cs[i-1][0]);adj[cs[i-1][0]].Add(cs[i][0]);}var K=new double[n,n];for(int i=0;i<n;i++)foreach(int j in adj[i])if(i<j){K[i,j]=0.5;K[j,i]=0.5;}return K;}
    static double Dm(double[,]d,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=d[i,j];c++;}return c>0?s/c:0;}
    static double Km(double[,]K,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=K[i,j];c++;}return c>0?s/c:0;}
    static double Ks(double[,]K,int n){var v=new double[n*(n-1)/2];int idx=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)v[idx++]=K[i,j];double m=v.Average();return Math.Sqrt(v.Sum(x=>(x-m)*(x-m))/v.Length);}
    static double[,]CD(double[,]d,int n){var c=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)c[i,j]=d[i,j];return c;}
    static P3 PCent(int n,bool hi){double d=0,k=0,ks=0;int c=0;for(int sd=0;sd<100;sd++){var K=KS(n,sd);double dm=0,km=0,kss=0;for(int e=0;e<5;e++){var h=Sim(K,n,S,sd+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}double om=Of(Sim(K,n,S,sd+5),n).Average();if((om>THR)==hi){d+=dm/5;k+=km/5;ks+=kss/5;c++;}}if(c==0)return new P3{dm=double.NaN,km=double.NaN,ks=double.NaN};return new P3{dm=d/c,km=k/c,ks=ks/c};}
    static bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<5;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,S,seed+5),n).Average()>THR;}
    static SBase Classify(SBase b,P3 hi){string cls;if(b.d0>0.65)cls="P1b";else if(b.d0>0.50)cls="P1";else if(b.d0<=0.40&&b.km0>0.98&&DistK(b.km0,b.ks0,hi)<0.15)cls="P2";else if(b.d0>0.40&&b.d0<=0.50)cls="P3";else cls="P4";return new SBase{seed=b.seed,d0=b.d0,km0=b.km0,ks0=b.ks0,cls=cls};}
    static double DistK(double km,double ks,P3 hi){double dk=km-hi.km,dks=ks-hi.ks;return Math.Sqrt(dk*dk+dks*dks);}
}
