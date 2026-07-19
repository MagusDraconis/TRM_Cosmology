using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_23;

[Trait("Category","V5_23"),Trait("Category","V5_23_CGE"),Trait("Category","LongRunning")]
public class V5_23_C3GainExecution_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153,RebT=0.01;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    struct GainProfile{
        public int n,s,cohort;public string cls,stateGroup,gainModel;
        // Geometry
        public double distHi,distLo,projHiVec,orthHiVec,offVecAngle,alignPre,alignPost;
        // T1 state
        public double dT1,kT1,kStdT1,omT1,rebMag,respProjHi,respOrthHi;
        // C3 movement
        public double deltaD,deltaK,deltaKS,deltaAlign,movementNorm;
        // Gain decomposition
        public double c3OmegaShift,c3Effectiveness;
        public double omegaPerMovement,omegaPerD,omegaPerK,omegaPerAlign;
        public double kSensitivity,alignmentGain;
        // Outcome
        public bool a0,c3,induced,rescued,persistent;
    }

    public V5_23_C3GainExecution_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    GainProfile? BuildProfile(int n,int s,P3 hi,P3 lo){
        var p=new GainProfile{n=n,s=s,cohort=s/100};
        var sb=SelectAndClassify(n,s,hi);
        if(sb==null)return null;
        p.cls=sb.Value.cls;
        double d0=sb.Value.d0,km0=sb.Value.km0,ks0=sb.Value.ks0;

        double dv=hi.dm-lo.dm,kv=hi.km-lo.km,sv=hi.ks-lo.ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
        p.projHiVec=vn>0?((d0-lo.dm)*dv+(km0-lo.km)*kv+(ks0-lo.ks)*sv)/vn:0;
        double d2c=(d0-lo.dm)*(d0-lo.dm)+(km0-lo.km)*(km0-lo.km)+(ks0-lo.ks)*(ks0-lo.ks);
        p.orthHiVec=Math.Sqrt(Math.Max(0,d2c-p.projHiVec*p.projHiVec));
        double cNorm=Math.Sqrt(d2c);
        p.offVecAngle=cNorm>1e-9&&vn>1e-9?Math.Acos(Math.Clamp(Math.Abs(p.projHiVec)/cNorm,-1,1)):Math.PI/2;
        p.distHi=Math.Sqrt((d0-hi.dm)*(d0-hi.dm)+(km0-hi.km)*(km0-hi.km)+(ks0-hi.ks)*(ks0-hi.ks));
        p.distLo=Math.Sqrt((d0-lo.dm)*(d0-lo.dm)+(km0-lo.km)*(km0-lo.km)+(ks0-lo.ks)*(ks0-lo.ks));

        // M3++ probe
        bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var K2=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K2=Cupd(d,n);}
        var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K2=Cupd(dM,n);
        var h5=Sim(K2,n,S,s+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K2,n,S,s+100);
        var dT1=DL(Nm(RP(hT1,n),n),n);p.dT1=Dm(dT1,n);
        var KT1=Cupd(dT1,n);p.kT1=Km(KT1,n);p.kStdT1=Ks(KT1,n);p.omT1=Of(hT1,n).Average();

        // Pre-C3 alignment
        double dT1m=p.dT1,kT1m=p.kT1,ksT1m=p.kStdT1;
        p.respProjHi=vn>0?((dT1m-lo.dm)*dv+(kT1m-lo.km)*kv+(ksT1m-lo.ks)*sv)/vn:0;
        double respD2=(dT1m-lo.dm)*(dT1m-lo.dm)+(kT1m-lo.km)*(kT1m-lo.km)+(ksT1m-lo.ks)*(ksT1m-lo.ks);
        p.respOrthHi=Math.Sqrt(Math.Max(0,respD2-p.respProjHi*p.respProjHi));
        p.alignPre=vn>0?((dT1m-lo.dm)*dv+(kT1m-lo.km)*kv)/vn:0;

        // T2
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);
        var dT2=DL(Nm(RP(hT2,n),n),n);double dT2m=Dm(dT2,n),kT2m=Km(Cupd(dT2,n),n),omT2=Of(hT2,n).Average();
        p.rebMag=dT2m-p.dT1;p.a0=omT2>THR;

        // C3 correction with full metric capture
        if(!double.IsNaN(hi.dm)){
            var dmat3=DL(Nm(RP(hT1,n),n),n);
            double dmPre=Dm(dmat3,n),kmPre=Km(Cupd(dmat3,n),n),ksPre=Ks(Cupd(dmat3,n),n);
            double nudged=dmPre+(hi.dm-dmPre)*0.2;
            double f3=Math.Clamp((nudged+1e-9)/(dmPre+1e-9),0.5,1.5);
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;
            var hc3=Sim(Cupd(dmat3,n),n,S,s+300);
            var Kc3c=Cupd(DL(Nm(RP(hc3,n),n),n),n);
            var hc3cc=Sim(Kc3c,n,S,s+400);
            double omC3=Of(hc3cc,n).Average();
            double dmPost=Dm(dmat3,n),kmPost=Km(Cupd(dmat3,n),n),ksPost=Ks(Cupd(dmat3,n),n);

            // C3 movement metrics
            p.deltaD=dmPost-dmPre;p.deltaK=kmPost-kmPre;p.deltaKS=ksPost-ksPre;
            p.alignPost=vn>0?((dmPost-lo.dm)*dv+(kmPost-lo.km)*kv)/vn:0;
            p.deltaAlign=p.alignPost-p.alignPre;
            p.movementNorm=Math.Sqrt(p.deltaD*p.deltaD+p.deltaK*p.deltaK+p.deltaKS*p.deltaKS);

            // Gain decomposition
            p.c3OmegaShift=omC3-omT2;
            p.c3Effectiveness=p.c3OmegaShift/Math.Max(1e-9,Math.Abs(p.alignPre)+0.001);
            p.omegaPerMovement=p.c3OmegaShift/Math.Max(1e-9,p.movementNorm);
            p.omegaPerD=p.c3OmegaShift/Math.Max(1e-9,Math.Abs(p.deltaD));
            p.omegaPerK=p.c3OmegaShift/Math.Max(1e-9,Math.Abs(p.deltaK));
            p.omegaPerAlign=p.c3OmegaShift/Math.Max(1e-9,Math.Abs(p.deltaAlign));
            p.kSensitivity=p.deltaK/Math.Max(1e-9,Math.Abs(p.deltaD));
            p.alignmentGain=Math.Abs(p.deltaAlign)/Math.Max(1e-9,Math.Abs(p.alignPre)+0.001);

            p.c3=omC3>THR;
            var hCont=Sim(Cupd(DL(Nm(RP(hc3cc,n),n),n),n),n,S,s+500);
            p.persistent=p.c3&&Of(hCont,n).Average()>THR;
        }else{
            p.c3OmegaShift=0;p.c3=false;p.persistent=false;
        }
        p.induced=p.a0||p.c3;p.rescued=p.c3&&!p.a0;
        p.stateGroup=p.rescued?"G1-highGain":p.n==64?"G3-N64fail":p.induced?"G1-highGain":"G2-lowGain";
        return p;
    }

    // ═══════════════════════════════════════════════
    // CGE_01 — Gain source ranking
    // ═══════════════════════════════════════════════
    [Fact]public void CGE_01_GainSourceRanking(){
        _o.WriteLine("═══ CGE_01: Gain source ranking ═══");

        int[] Ns={64,65,66,70,72};
        var profiles=new ConcurrentBag<GainProfile>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<299;s++){if(IsHi(n,s))continue;var p=BuildProfile(n,s,hi,lo);if(p!=null)profiles.Add(p.Value);}});

        var all=profiles.ToArray();
        var rescued=all.Where(p=>p.rescued).ToArray();
        var notRescued=all.Where(p=>!p.rescued&&!p.a0).ToArray();
        _o.WriteLine($"\nRescued: {rescued.Length}, Not rescued: {notRescued.Length}");

        // Gain driver ranking
        _o.WriteLine($"\n─── Gain driver ranking ───");
        var drivers=new List<(string label,double rMean,double fMean,double ratio)>();
        string[] dm={"c3OmegaShift","omegaPerMovement","omegaPerD","omegaPerK","omegaPerAlign",
            "deltaD","deltaK","deltaAlign","kSensitivity","alignmentGain",
            "alignPre","dT1","kT1","kStdT1","omT1","movementNorm",
            "rebMag","distHi","offVecAngle"};
        string[] dl={"C3 omega shift","Omega per movement","Omega per d-shift","Omega per K-shift","Omega per alignment",
            "C3 d-shift","C3 K-shift","C3 deltaAlign","K sensitivity","Alignment gain",
            "Pre-C3 align","Probe dT1","K at T1","K std T1","Omega T1","Movement norm",
            "rebMagnitude","Dist to Hi","Off-vec angle"};

        for(int i=0;i<dm.Length;i++){
            double r=Math.Abs(GetMean(rescued,dm[i])),f=Math.Abs(GetMean(notRescued,dm[i]));
            double ratio=f>1e-9?r/f:r>1e-9?999:1;
            drivers.Add((dl[i],r,f,ratio));
        }

        _o.WriteLine(string.Format("{0,3} {1,-22} {2,10} {3,10} {4,8}","","Driver","Rescued","Failed","Ratio"));
        int rank=1;
        foreach(var d in drivers.OrderByDescending(x=>x.ratio)){
            string m=rank<=3?"←":"";
            _o.WriteLine($"{rank,2}. {d.label,-19} {d.rMean,10:F4} {d.fMean,10:F4} {d.ratio,8:F1}x {m}");
            rank++;
        }

        var top=drivers.OrderByDescending(x=>x.ratio).First();
        _o.WriteLine($"\nTop gain source: {top.label} (×{top.ratio:F1})");
        _o.WriteLine($"Gate A (Gain source identified): {(top.ratio>10?"REACHED":"NOT REACHED")}");

        _o.WriteLine($"\n─── Next: CGE_02 N=64 gain cap ───");
    }

    // ═══════════════════════════════════════════════
    // CGE_02 — N=64 gain-cap analysis
    // ═══════════════════════════════════════════════
    [Fact]public void CGE_02_N64GainCapAnalysis(){
        _o.WriteLine("═══ CGE_02: N=64 gain cap — why is gain suppressed? ═══");

        int[] Ns={64,65};
        var profiles=new ConcurrentBag<GainProfile>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<399;s++){if(IsHi(n,s))continue;var p=BuildProfile(n,s,hi,lo);if(p!=null)profiles.Add(p.Value);}});

        var all=profiles.ToArray();
        var n64=all.Where(p=>p.n==64).ToArray();
        var n65resc=all.Where(p=>p.n==65&&p.rescued).ToArray();
        var n65fail=all.Where(p=>p.n==65&&!p.rescued&&!p.a0).ToArray();

        _o.WriteLine($"\nN=64: {n64.Length}, N=65 rescued: {n65resc.Length}, failed: {n65fail.Length}");

        // Gain decomposition comparison
        _o.WriteLine($"\n─── Gain decomposition: N=64 vs N=65 ───");
        _o.WriteLine(string.Format("{0,-18} {1,10} {2,10} {3,10} {4,10}",
            "Gain Metric","N=64","N=65 Resc","N=65 Fail","64→65R"));
        string[] gm={"c3OmegaShift","omegaPerMovement","omegaPerD","omegaPerK","omegaPerAlign",
            "deltaD","deltaK","deltaAlign","kSensitivity","movementNorm","alignmentGain"};

        foreach(var m in gm){
            double v64=GetMean(n64,m),v65r=GetMean(n65resc,m),v65f=GetMean(n65fail,m);
            double gap=Math.Abs(v64)>1e-9?v65r/Math.Abs(v64):999;
            _o.WriteLine($"{m,-18} {v64,10:F4} {v65r,10:F4} {v65f,10:F4} {gap,10:F1}x");
        }

        // Is N=64's low gain from weak movement or weak omega sensitivity?
        _o.WriteLine($"\n─── Movement vs sensitivity ───");
        _o.WriteLine($"N=64 movementNorm: {n64.Average(p=>p.movementNorm):F4}");
        _o.WriteLine($"N=65 rescued movementNorm: {n65resc.Average(p=>p.movementNorm):F4}");
        double moveGap=n65resc.Average(p=>p.movementNorm)/Math.Max(1e-9,n64.Average(p=>p.movementNorm));
        _o.WriteLine($"Movement gap: {moveGap:F2}x");

        _o.WriteLine($"N=64 omegaPerMovement: {Math.Abs(n64.Average(p=>p.omegaPerMovement)):F4}");
        _o.WriteLine($"N=65 rescued omegaPerMovement: {Math.Abs(n65resc.Average(p=>p.omegaPerMovement)):F4}");
        double sensGap=Math.Abs(n65resc.Average(p=>p.omegaPerMovement))/Math.Max(1e-9,Math.Abs(n64.Average(p=>p.omegaPerMovement)));
        _o.WriteLine($"Sensitivity gap: {sensGap:F2}x");

        string explanation=moveGap>sensGap?"Movement-limited":"Sensitivity-limited";
        _o.WriteLine($"\nN=64 is primarily: {(moveGap>2||sensGap>2?explanation:"both")}");
        _o.WriteLine($"Gate B (N=64 cap explained): {(moveGap>2||sensGap>2?"REACHED":"NOT REACHED")}");

        _o.WriteLine($"\n─── Next: CGE_03 N=65 activation ───");
    }

    // ═══════════════════════════════════════════════
    // CGE_03 — N=65 activation analysis
    // ═══════════════════════════════════════════════
    [Fact]public void CGE_03_N65ActivationAnalysis(){
        _o.WriteLine("═══ CGE_03: N=65 activation — what changes? ═══");

        int[] Ns={63,64,65,66};
        var profiles=new ConcurrentBag<GainProfile>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<299;s++){if(IsHi(n,s))continue;var p=BuildProfile(n,s,hi,lo);if(p!=null)profiles.Add(p.Value);}});

        var all=profiles.ToArray();

        // Per-N gain metrics
        _o.WriteLine($"\n─── Gain activation: N=63→66 ───");
        _o.WriteLine(string.Format("{0,4} {1,6} {2,8} {3,8} {4,8} {5,8} {6,8}",
            "N","n","c3OmgS","deltaD","deltaK","deltaAlign","movNorm"));
        foreach(var n in Ns){
            var sub=all.Where(p=>p.n==n).ToArray();
            if(sub.Length==0)continue;
            _o.WriteLine($"{n,4} {sub.Length,6} {sub.Average(p=>p.c3OmegaShift),8:F4} {sub.Average(p=>p.deltaD),8:F4} {sub.Average(p=>p.deltaK),8:F4} {sub.Average(p=>p.deltaAlign),8:F4} {sub.Average(p=>p.movementNorm),8:F4}");
        }

        // Omega sensitivity per N
        _o.WriteLine($"\n─── Omega sensitivity per N ───");
        _o.WriteLine(string.Format("{0,4} {1,10} {2,10} {3,10} {4,10}",
            "N","omegaPerMov","omegaPerD","omegaPerK","omegaPerAlign"));
        foreach(var n in Ns){
            var sub=all.Where(p=>p.n==n).ToArray();
            if(sub.Length==0)continue;
            _o.WriteLine($"{n,4} {Math.Abs(sub.Average(p=>p.omegaPerMovement)),10:F4} {Math.Abs(sub.Average(p=>p.omegaPerD)),10:F4} {Math.Abs(sub.Average(p=>p.omegaPerK)),10:F4} {Math.Abs(sub.Average(p=>p.omegaPerAlign)),10:F4}");
        }

        // First-changer at N=64→65
        var n64=all.Where(p=>p.n==64).ToArray();
        var n65=all.Where(p=>p.n==65).ToArray();
        if(n64.Length>0&&n65.Length>0){
            _o.WriteLine($"\n─── First-changer: N=64→65 ───");
            var changes=new List<(string,double)>();
            string[] fm={"c3OmegaShift","omegaPerMovement","omegaPerD","omegaPerK","omegaPerAlign",
                "deltaD","deltaK","deltaAlign","kSensitivity","movementNorm"};
            foreach(var m in fm){
                double v64=Math.Abs(GetMean(n64,m)),v65=Math.Abs(GetMean(n65,m));
                double chg=v65/Math.Max(1e-9,v64);
                changes.Add((m,chg));
            }
            foreach(var c in changes.OrderByDescending(x=>x.Item2).Take(5))
                _o.WriteLine($"  {c.Item1}: ×{c.Item2:F1}");
        }

        _o.WriteLine($"Gate C (Activation explained): {(n64.Length>0&&n65.Length>0?"REACHED":"NOT REACHED")}");

        _o.WriteLine($"\n─── Next: CGE_04 Cross-N ───");
    }

    // ═══════════════════════════════════════════════
    // CGE_04 — Cross-N gain stability
    // ═══════════════════════════════════════════════
    [Fact]public void CGE_04_CrossNGainStability(){
        _o.WriteLine("═══ CGE_04: Cross-N gain stability ═══");

        int[] Ns={65,66,70,72};
        var profiles=new ConcurrentBag<GainProfile>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<299;s++){if(IsHi(n,s))continue;var p=BuildProfile(n,s,hi,lo);if(p!=null)profiles.Add(p.Value);}});

        var all=profiles.ToArray();

        // Per-N gain profile for rescued seeds
        _o.WriteLine($"\n─── Rescued seed gain profiles ───");
        _o.WriteLine(string.Format("{0,4} {1,6} {2,8} {3,8} {4,8} {5,8} {6,8}",
            "N","n","c3OmgS","omegaPerD","omegaPerK","omegaPerAl","movNorm"));
        foreach(var n in Ns){
            var res=all.Where(p=>p.n==n&&p.rescued).ToArray();
            if(res.Length==0)continue;
            _o.WriteLine($"{n,4} {res.Length,6} {res.Average(p=>p.c3OmegaShift),8:F4} {Math.Abs(res.Average(p=>p.omegaPerD)),8:F4} {Math.Abs(res.Average(p=>p.omegaPerK)),8:F4} {Math.Abs(res.Average(p=>p.omegaPerAlign)),8:F4} {res.Average(p=>p.movementNorm),8:F4}");
        }

        // Gain stability: coefficient of variation of key metrics
        _o.WriteLine($"\n─── Gain metric stability across N ───");
        string[] sm={"c3OmegaShift","omegaPerMovement","omegaPerD","omegaPerK","omegaPerAlign","movementNorm"};
        foreach(var m in sm){
            var means=Ns.Select(n=>{
                var res=all.Where(p=>p.n==n&&p.rescued).ToArray();
                return res.Length>0?Math.Abs(GetMean(res,m)):0.0;
            }).Where(x=>x>1e-9).ToArray();
            if(means.Length<2)continue;
            double avg=means.Average(),std=Math.Sqrt(means.Average(x=>(x-avg)*(x-avg)));
            _o.WriteLine($"{m,-18} mean={avg:F4} cv={std/Math.Max(1e-9,avg):F3}");
        }

        _o.WriteLine($"\n─── Next: CGE_05 Pre-C3 predictability ───");
    }

    // ═══════════════════════════════════════════════
    // CGE_05 — Pre-C3 predictability
    // ═══════════════════════════════════════════════
    [Fact]public void CGE_05_PreC3Predictability(){
        _o.WriteLine("═══ CGE_05: Can c3OmegaShift be predicted before C3? ═══");

        int[] Ns={64,65,66,70,72};
        var profiles=new ConcurrentBag<GainProfile>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<399;s++){if(IsHi(n,s))continue;var p=BuildProfile(n,s,hi,lo);if(p!=null)profiles.Add(p.Value);}});

        var all=profiles.ToArray();

        // Pre-C3 predictors vs c3OmegaShift
        _o.WriteLine($"\n─── Pre-C3 predictors ───");
        _o.WriteLine(string.Format("{0,-16} {1,6} {2,10} {3,10}",
            "Predictor","Dir","Lo-Gain","Hi-Gain"));
        string[] preds={"alignPre","dT1","omT1","kT1","kStdT1","rebMag","distHi","offVecAngle","respProjHi"};

        // Split by c3OmegaShift median
        double median=all.Select(p=>p.c3OmegaShift).Where(x=>!double.IsNaN(x)).DefaultIfEmpty(0).OrderBy(x=>x).ElementAt(all.Length/2);
        var hiGain=all.Where(p=>p.c3OmegaShift>median).ToArray();
        var loGain=all.Where(p=>p.c3OmegaShift<=median).ToArray();
        _o.WriteLine($"Median c3OmegaShift: {median:F4}. Hi-gain: {hiGain.Length}, Lo-gain: {loGain.Length}");

        foreach(var m in preds){
            double lo=Math.Abs(GetMean(loGain,m)),hi=Math.Abs(GetMean(hiGain,m));
            double dir=hi>lo?1:-1;
            _o.WriteLine($"{m,-16} {(dir>0?"↑":"↓"),6} {lo,10:F4} {hi,10:F4}");
        }

        // Simple threshold: predict rescue
        _o.WriteLine($"\n─── Threshold prediction for rescue ───");
        string[] thresh={"dT1>0.5","alignPre<-0.1","omT1>1.2","kT1<0.95","dT1>0.5&&alignPre<-0.1"};
        foreach(var t in thresh){
            int correct=0,total=all.Length;
            if(t=="dT1>0.5")correct=all.Count(p=>(p.dT1>0.5)==p.rescued);
            else if(t=="alignPre<-0.1")correct=all.Count(p=>(p.alignPre<-0.1)==p.rescued);
            else if(t=="omT1>1.2")correct=all.Count(p=>(p.omT1>1.2)==p.rescued);
            else if(t=="kT1<0.95")correct=all.Count(p=>(p.kT1<0.95)==p.rescued);
            else if(t=="dT1>0.5&&alignPre<-0.1")correct=all.Count(p=>((p.dT1>0.5&&p.alignPre<-0.1))==p.rescued);
            _o.WriteLine($"{t,-24} acc={correct*100.0/total:F0}%");
        }

        _o.WriteLine($"Gate D (Pre-C3 predictor): assessing");

        _o.WriteLine($"\n─── Next: CGE_06 Gain decomposition ───");
    }

    // ═══════════════════════════════════════════════
    // CGE_06 — Gain decomposition verdict
    // ═══════════════════════════════════════════════
    [Fact]public void CGE_06_GainDecompositionVerdict(){
        _o.WriteLine("═══ CGE_06: Gain decomposition verdict ═══");

        int[] Ns={64,65,66,70,72};
        var profiles=new ConcurrentBag<GainProfile>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<399;s++){if(IsHi(n,s))continue;var p=BuildProfile(n,s,hi,lo);if(p!=null)profiles.Add(p.Value);}});

        var all=profiles.ToArray();
        var n64=all.Where(p=>p.n==64).ToArray();
        var n65pp=all.Where(p=>p.n>=65).ToArray();

        // Classify gain mechanism
        _o.WriteLine($"\n─── Gain model classification ───");

        // Model A: Movement-limited
        double moveRatio=n65pp.Average(p=>p.movementNorm)/Math.Max(1e-9,n64.Average(p=>p.movementNorm));
        bool modelA=moveRatio>2;
        _o.WriteLine($"Model A (Movement-limited): movement gap={moveRatio:F2}x → {(modelA?"SUPPORTED":"not supported")}");

        // Model B: K response limited
        double kSensRatio=Math.Abs(n65pp.Average(p=>p.kSensitivity))/Math.Max(1e-9,Math.Abs(n64.Average(p=>p.kSensitivity)));
        bool modelB=kSensRatio>2;
        _o.WriteLine($"Model B (K response limited): kSens gap={kSensRatio:F2}x → {(modelB?"SUPPORTED":"not supported")}");

        // Model C: Omega sensitivity limited
        double omSensRatio=Math.Abs(n65pp.Average(p=>p.omegaPerMovement))/Math.Max(1e-9,Math.Abs(n64.Average(p=>p.omegaPerMovement)));
        bool modelC=omSensRatio>2;
        _o.WriteLine($"Model C (Omega sensitivity): omSens gap={omSensRatio:F2}x → {(modelC?"SUPPORTED":"not supported")}");

        // Model D: Alignment-gain limited
        double alignGainRatio=Math.Abs(n65pp.Average(p=>p.alignmentGain))/Math.Max(1e-9,Math.Abs(n64.Average(p=>p.alignmentGain)));
        bool modelD=alignGainRatio>2;
        _o.WriteLine($"Model D (Alignment-gain): alignGain gap={alignGainRatio:F2}x → {(modelD?"SUPPORTED":"not supported")}");

        // Model E: Hidden N-dependent
        // If movement, K, Omega sens, and alignment ratios are all similar but c3OmegaShift is very different
        double c3Ratio=Math.Abs(n65pp.Average(p=>p.c3OmegaShift))/Math.Max(1e-9,Math.Abs(n64.Average(p=>p.c3OmegaShift)));
        double avgComponentRatio=(moveRatio+kSensRatio+omSensRatio+alignGainRatio)/4;
        bool modelE=c3Ratio>avgComponentRatio*2;
        _o.WriteLine($"Model E (Hidden N-gain): c3Ratio={c3Ratio:F1}x vs avgComponent={avgComponentRatio:F1}x → {(modelE?"SUPPORTED":"not supported")}");

        // Final classification
        string verdict;
        if(modelE)verdict="E: Hidden N-dependent gain factor";
        else if(modelC)verdict="C: Omega sensitivity limited";
        else if(modelB)verdict="B: K response limited";
        else if(modelA)verdict="A: Movement limited";
        else if(modelD)verdict="D: Alignment-gain limited";
        else verdict="F: Mixed gain mechanism";

        _o.WriteLine($"\n─── Verdict: {verdict} ───");
        _o.WriteLine($"Gate E (Omega sensitivity): {(modelC?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate F (K response): {(modelB?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate G (Hidden N-gain): {(modelE?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate H (Unresolved): {(!modelA&&!modelB&&!modelC&&!modelD&&!modelE?"REACHED":"NOT REACHED")}");

        _o.WriteLine($"\n─── Analysis complete ───");
    }

    // ─── Helpers ───
    static double GetMean(GainProfile[] ps,string m)=>ps.Length==0?0:m switch{
        "c3OmegaShift"=>ps.Average(p=>p.c3OmegaShift),"omegaPerMovement"=>ps.Average(p=>p.omegaPerMovement),
        "omegaPerD"=>ps.Average(p=>p.omegaPerD),"omegaPerK"=>ps.Average(p=>p.omegaPerK),
        "omegaPerAlign"=>ps.Average(p=>p.omegaPerAlign),"deltaD"=>ps.Average(p=>p.deltaD),
        "deltaK"=>ps.Average(p=>p.deltaK),"deltaKS"=>ps.Average(p=>p.deltaKS),
        "deltaAlign"=>ps.Average(p=>p.deltaAlign),"kSensitivity"=>ps.Average(p=>p.kSensitivity),
        "alignmentGain"=>ps.Average(p=>p.alignmentGain),"movementNorm"=>ps.Average(p=>p.movementNorm),
        "alignPre"=>ps.Average(p=>p.alignPre),"alignPost"=>ps.Average(p=>p.alignPost),
        "dT1"=>ps.Average(p=>p.dT1),"kT1"=>ps.Average(p=>p.kT1),"kStdT1"=>ps.Average(p=>p.kStdT1),
        "omT1"=>ps.Average(p=>p.omT1),"rebMag"=>ps.Average(p=>p.rebMag),
        "distHi"=>ps.Average(p=>p.distHi),"distLo"=>ps.Average(p=>p.distLo),
        "offVecAngle"=>ps.Average(p=>p.offVecAngle),"respProjHi"=>ps.Average(p=>p.respProjHi),_=>0
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
