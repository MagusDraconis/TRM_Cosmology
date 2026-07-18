using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_22;

[Trait("Category","V5_22"),Trait("Category","V5_22_IOE"),Trait("Category","LongRunning")]
public class V5_22_InducibilityOnsetExecution_Tests
{
    private readonly ITestOutputHelper _o;
    // Frozen V5.20 constants
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153,RebT=0.01;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    struct OnsetProfile{
        public int n,s,cohort;public string cls;
        // Geometry
        public double distHi,distLo,projHiVec,orthHiVec,offVecAngle;
        // Response direction
        public double entryAlignPre,entryAlignPost,deltaAlign,respProjHi,respOrthHi;
        // Dynamics
        public double dT0,dT1,dT2,kT0,kT1,kT2,omT0,omT1,omT2;
        public double rebMag,kCollapse,dRebound;
        // C3 effectiveness
        public double c3DMeanShift,c3KMeanShift,c3OmegaShift,c3AlignGain;
        // Outcome
        public bool a0,c3,induced,rescued,persistent;public string outcomeLabel;
    }

    public V5_22_InducibilityOnsetExecution_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    // ─── Profile builder ───
    OnsetProfile? BuildProfile(int n,int s,P3 hi,P3 lo){
        var p=new OnsetProfile{n=n,s=s,cohort=s/100};
        var sb=SelectAndClassify(n,s,hi);
        if(sb==null)return null;
        p.cls=sb.Value.cls;
        double d0=sb.Value.d0,km0=sb.Value.km0,ks0=sb.Value.ks0;

        double dv=hi.dm-lo.dm,kv=hi.km-lo.km,sv=hi.ks-lo.ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
        p.projHiVec=vn>0?((d0-lo.dm)*dv+(km0-lo.km)*kv+(ks0-lo.ks)*sv)/vn:0;
        double d2=(d0-lo.dm)*(d0-lo.dm)+(km0-lo.km)*(km0-lo.km)+(ks0-lo.ks)*(ks0-lo.ks);
        p.orthHiVec=Math.Sqrt(Math.Max(0,d2-p.projHiVec*p.projHiVec));
        double candNorm=Math.Sqrt(d2);
        p.offVecAngle=candNorm>1e-9&&vn>1e-9?Math.Acos(Math.Clamp(Math.Abs(p.projHiVec)/candNorm,-1,1)):Math.PI/2;
        p.distHi=Math.Sqrt((d0-hi.dm)*(d0-hi.dm)+(km0-hi.km)*(km0-hi.km)+(ks0-hi.ks)*(ks0-hi.ks));
        p.distLo=Math.Sqrt((d0-lo.dm)*(d0-lo.dm)+(km0-lo.km)*(km0-lo.km)+(ks0-lo.ks)*(ks0-lo.ks));

        // T0
        var K0=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K0,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K0=Cupd(d,n);}
        var hT0=Sim(K0,n,S,s+3);var dT0=DL(Nm(RP(hT0,n),n),n);p.dT0=Dm(dT0,n);p.kT0=Km(Cupd(dT0,n),n);p.omT0=Of(hT0,n).Average();

        // Frozen M3++ probe
        bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var K2=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K2=Cupd(d,n);}
        var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K2=Cupd(dM,n);
        var h5=Sim(K2,n,S,s+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K2,n,S,s+100);
        var dT1=DL(Nm(RP(hT1,n),n),n);p.dT1=Dm(dT1,n);p.kT1=Km(Cupd(dT1,n),n);p.omT1=Of(hT1,n).Average();

        // Response direction at T1
        double dT1m=p.dT1,kT1m=p.kT1,ksT1m=Ks(Cupd(dT1,n),n);
        p.respProjHi=vn>0?((dT1m-lo.dm)*dv+(kT1m-lo.km)*kv+(ksT1m-lo.ks)*sv)/vn:0;
        double respD2=(dT1m-lo.dm)*(dT1m-lo.dm)+(kT1m-lo.km)*(kT1m-lo.km)+(ksT1m-lo.ks)*(ksT1m-lo.ks);
        p.respOrthHi=Math.Sqrt(Math.Max(0,respD2-p.respProjHi*p.respProjHi));
        p.entryAlignPre=vn>0?((dT1m-lo.dm)*dv+(kT1m-lo.km)*kv)/vn:0;

        // T2
        var Kt2=Cupd(DL(Nm(RP(hT1,n),n),n),n);
        var hT2=Sim(Kt2,n,S,s+200);
        var dT2=DL(Nm(RP(hT2,n),n),n);p.dT2=Dm(dT2,n);p.kT2=Km(Cupd(dT2,n),n);p.omT2=Of(hT2,n).Average();
        p.rebMag=p.dT2-p.dT1;p.kCollapse=p.kT2-p.kT1;p.dRebound=p.dT2-p.dT1;
        p.a0=p.omT2>THR;

        // C3 correction
        if(p.rebMag<RebT&&!p.a0&&!double.IsNaN(hi.dm)){
            var dmat3=DL(Nm(RP(hT1,n),n),n);double dmPre=Dm(dmat3,n),kmPre=Km(Cupd(dmat3,n),n);
            double nudged=dmPre+(hi.dm-dmPre)*0.2;
            double f3=Math.Clamp((nudged+1e-9)/(dmPre+1e-9),0.5,1.5);
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;
            var hc3=Sim(Cupd(dmat3,n),n,S,s+300);
            var Kc3c=Cupd(DL(Nm(RP(hc3,n),n),n),n);
            var hc3cc=Sim(Kc3c,n,S,s+400);
            double omC3=Of(hc3cc,n).Average();
            double dmPost=Dm(dmat3,n),kmPost=Km(Cupd(dmat3,n),n);

            p.c3OmegaShift=omC3-p.omT2;
            p.c3DMeanShift=dmPost-dmPre;
            p.c3KMeanShift=kmPost-kmPre;
            p.entryAlignPost=vn>0?((dmPost-lo.dm)*dv+(kmPost-lo.km)*kv)/vn:0;
            p.deltaAlign=p.entryAlignPost-p.entryAlignPre;
            p.c3AlignGain=Math.Abs(p.deltaAlign);
            p.c3=omC3>THR;

            // Persistence
            var hCont=Sim(Cupd(DL(Nm(RP(hc3cc,n),n),n),n),n,S,s+500);
            p.persistent=p.c3&&Of(hCont,n).Average()>THR;
        }else{
            p.c3OmegaShift=0;p.c3DMeanShift=0;p.c3KMeanShift=0;
            p.entryAlignPost=p.entryAlignPre;p.deltaAlign=0;p.c3AlignGain=0;
            p.c3=p.a0;p.persistent=p.a0;
        }

        p.induced=p.a0||p.c3;p.rescued=p.c3&&!p.a0;
        p.outcomeLabel=p.rescued?"rescued":p.induced?"induced":"failed";
        return p;
    }

    // ═══════════════════════════════════════════════
    // IOE_01 — Fine onset scan N=62→67
    // ═══════════════════════════════════════════════
    [Fact]public void IOE_01_FineOnsetScan(){
        _o.WriteLine("═══ IOE_01: Fine onset scan N=62→67 ═══");
        _o.WriteLine("Detailed metric panel across the onset boundary.");

        int[] Ns={62,63,64,65,66,67};
        var profiles=new ConcurrentBag<OnsetProfile>();

        Parallel.ForEach(Ns,n=>{
            var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<199;s++){
                if(IsHi(n,s))continue;
                var p=BuildProfile(n,s,hi,lo);
                if(p==null)continue;
                profiles.Add(p.Value);
            }
        });

        var all=profiles.ToArray();
        _o.WriteLine($"\nOnset scan: {all.Length} profiles across {Ns.Length} N values");

        // Geometry panel
        _o.WriteLine($"\n─── Candidate geometry ───");
        _o.WriteLine(string.Format("{0,4} {1,6} {2,8} {3,8} {4,8} {5,8}",
            "N","n","distHi","projHi","offAng","distLo"));
        foreach(var n in Ns){
            var sub=all.Where(p=>p.n==n).ToArray();
            if(sub.Length==0)continue;
            _o.WriteLine($"{n,4} {sub.Length,6} {sub.Average(p=>p.distHi),8:F4} {sub.Average(p=>p.projHiVec),8:F4} {sub.Average(p=>p.offVecAngle),8:F4} {sub.Average(p=>p.distLo),8:F4}");
        }

        // Response panel
        _o.WriteLine($"\n─── Response direction ───");
        _o.WriteLine(string.Format("{0,4} {1,10} {2,10} {3,10} {4,10}",
            "N","alignPre","alignPost","deltaAlign","c3AlGain"));
        foreach(var n in Ns){
            var sub=all.Where(p=>p.n==n).ToArray();
            if(sub.Length==0)continue;
            _o.WriteLine($"{n,4} {sub.Average(p=>p.entryAlignPre),10:F4} {sub.Average(p=>p.entryAlignPost),10:F4} {sub.Average(p=>p.deltaAlign),10:F4} {sub.Average(p=>p.c3AlignGain),10:F4}");
        }

        // Dynamics panel
        _o.WriteLine($"\n─── Dynamics ───");
        _o.WriteLine(string.Format("{0,4} {1,8} {2,8} {3,8} {4,8} {5,8} {6,8} {7,8}",
            "N","dT1","dT2","rebMag","kT1","kCollapse","omT1","omT2"));
        foreach(var n in Ns){
            var sub=all.Where(p=>p.n==n).ToArray();
            if(sub.Length==0)continue;
            _o.WriteLine($"{n,4} {sub.Average(p=>p.dT1),8:F4} {sub.Average(p=>p.dT2),8:F4} {sub.Average(p=>p.rebMag),8:F4} {sub.Average(p=>p.kT1),8:F4} {sub.Average(p=>p.kCollapse),8:F4} {sub.Average(p=>p.omT1),8:F4} {sub.Average(p=>p.omT2),8:F4}");
        }

        // C3 effectiveness panel
        _o.WriteLine($"\n─── C3 effectiveness ───");
        _o.WriteLine(string.Format("{0,4} {1,10} {2,10} {3,10} {4,6} {5,6}",
            "N","c3OmgShift","c3DShift","c3KShift","Resc%","Pers%"));
        foreach(var n in Ns){
            var sub=all.Where(p=>p.n==n).ToArray();
            if(sub.Length==0)continue;
            _o.WriteLine($"{n,4} {sub.Average(p=>p.c3OmegaShift),10:F4} {sub.Average(p=>p.c3DMeanShift),10:F4} {sub.Average(p=>p.c3KMeanShift),10:F4} {sub.Count(p=>p.rescued)*100.0/sub.Length,5:F0}% {sub.Count(p=>p.persistent)*100.0/sub.Length,5:F0}%");
        }

        // Onset step changes
        _o.WriteLine($"\n─── Step changes: N=64→65→66 ───");
        var n64=all.Where(p=>p.n==64).ToArray();
        var n65=all.Where(p=>p.n==65).ToArray();
        var n66=all.Where(p=>p.n==66).ToArray();
        string[] metrics={"distHi","projHiVec","offVecAngle","entryAlignPre","deltaAlign",
            "c3OmegaShift","c3AlignGain","dT1","dT2","rebMag","kCollapse"};
        _o.WriteLine(string.Format("{0,-14} {1,8} {2,8} {3,8} {4,8} {5,8}",
            "Metric","64","65","66","64→65","65→66"));
        foreach(var m in metrics){
            double v64=GetVal(n64,m),v65=GetVal(n65,m),v66=GetVal(n66,m);
            _o.WriteLine($"{m,-14} {v64,8:F4} {v65,8:F4} {v66,8:F4} {(v65-v64),8:+0.0000;-0.0000} {(v66-v65),8:+0.0000;-0.0000}");
        }

        _o.WriteLine($"\n─── Next: IOE_02 C3 effectiveness curve ───");
    }

    // ═══════════════════════════════════════════════
    // IOE_02 — C3 effectiveness curve
    // ═══════════════════════════════════════════════
    [Fact]public void IOE_02_C3EffectivenessCurve(){
        _o.WriteLine("═══ IOE_02: C3 effectiveness curve vs N ═══");
        _o.WriteLine("Tracking c3OmegaShift and c3AlignGain across N=62-72.");

        int[] Ns={62,63,64,65,66,67,70,72};
        var profiles=new ConcurrentBag<OnsetProfile>();

        Parallel.ForEach(Ns,n=>{
            var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<199;s++){
                if(IsHi(n,s))continue;
                var p=BuildProfile(n,s,hi,lo);
                if(p==null)continue;
                profiles.Add(p.Value);
            }
        });

        var all=profiles.ToArray();

        // C3 effectiveness curve
        _o.WriteLine($"\n─── C3 effectiveness vs N ───");
        _o.WriteLine(string.Format("{0,4} {1,6} {2,10} {3,10} {4,10} {5,10} {6,10}",
            "N","n","c3OmgShift","c3DShift","c3KShift","c3AlGain","deltaAlign"));
        foreach(var n in Ns){
            var sub=all.Where(p=>p.n==n).ToArray();
            if(sub.Length==0)continue;
            _o.WriteLine($"{n,4} {sub.Length,6} {sub.Average(p=>p.c3OmegaShift),10:F4} {sub.Average(p=>p.c3DMeanShift),10:F4} {sub.Average(p=>p.c3KMeanShift),10:F4} {sub.Average(p=>p.c3AlignGain),10:F4} {sub.Average(p=>p.deltaAlign),10:F4}");
        }

        // C3 jump quantification
        var n64=all.Where(p=>p.n==64).ToArray();
        var n65=all.Where(p=>p.n==65).ToArray();
        if(n64.Length>0&&n65.Length>0){
            double omgJump=n65.Average(p=>p.c3OmegaShift)/Math.Max(1e-9,Math.Abs(n64.Average(p=>p.c3OmegaShift)));
            double algJump=n65.Average(p=>p.c3AlignGain)/Math.Max(1e-9,n64.Average(p=>p.c3AlignGain));
            _o.WriteLine($"\n─── C3 jump at N=64→65 ───");
            _o.WriteLine($"c3OmegaShift: {n64.Average(p=>p.c3OmegaShift):F4} → {n65.Average(p=>p.c3OmegaShift):F4} (×{omgJump:F1})");
            _o.WriteLine($"c3AlignGain:  {n64.Average(p=>p.c3AlignGain):F4} → {n65.Average(p=>p.c3AlignGain):F4} (×{algJump:F1})");
            _o.WriteLine($"deltaAlign:   {n64.Average(p=>p.deltaAlign):F4} → {n65.Average(p=>p.deltaAlign):F4} (×{n65.Average(p=>p.deltaAlign)/Math.Max(1e-9,Math.Abs(n64.Average(p=>p.deltaAlign))):F1})");
        }

        // Rescued vs failed C3 profiles at N=65
        var n65all=all.Where(p=>p.n==65).ToArray();
        var rescued=n65all.Where(p=>p.rescued).ToArray();
        var failed=n65all.Where(p=>!p.rescued).ToArray();
        if(rescued.Length>0&&failed.Length>0){
            _o.WriteLine($"\n─── N=65: rescued ({rescued.Length}) vs failed ({failed.Length}) ───");
            _o.WriteLine(string.Format("{0,-14} {1,10} {2,10} {3,8}",
                "Metric","Rescued","Failed","Ratio"));
            _o.WriteLine($"{"c3OmegaShift",-14} {rescued.Average(p=>p.c3OmegaShift),10:F4} {failed.Average(p=>p.c3OmegaShift),10:F4} {rescued.Average(p=>p.c3OmegaShift)/Math.Max(1e-9,Math.Abs(failed.Average(p=>p.c3OmegaShift))),8:F1}x");
            _o.WriteLine($"{"c3AlignGain",-14} {rescued.Average(p=>p.c3AlignGain),10:F4} {failed.Average(p=>p.c3AlignGain),10:F4} {rescued.Average(p=>p.c3AlignGain)/Math.Max(1e-9,failed.Average(p=>p.c3AlignGain)),8:F1}x");
            _o.WriteLine($"{"deltaAlign",-14} {rescued.Average(p=>p.deltaAlign),10:F4} {failed.Average(p=>p.deltaAlign),10:F4} {rescued.Average(p=>p.deltaAlign)/Math.Max(1e-9,Math.Abs(failed.Average(p=>p.deltaAlign))),8:F1}x");
            _o.WriteLine($"{"alignPre",-14} {rescued.Average(p=>p.entryAlignPre),10:F4} {failed.Average(p=>p.entryAlignPre),10:F4} —");
            _o.WriteLine($"{"dT1",-14} {rescued.Average(p=>p.dT1),10:F4} {failed.Average(p=>p.dT1),10:F4} {rescued.Average(p=>p.dT1)/Math.Max(1e-9,failed.Average(p=>p.dT1)),8:F1}x");
            _o.WriteLine($"{"rebMag",-14} {rescued.Average(p=>p.rebMag),10:F4} {failed.Average(p=>p.rebMag),10:F4} —");
        }

        _o.WriteLine($"\n─── Next: IOE_03 Alignment gain curve ───");
    }

    // ═══════════════════════════════════════════════
    // IOE_03 — Alignment gain curve
    // ═══════════════════════════════════════════════
    [Fact]public void IOE_03_AlignmentGainCurve(){
        _o.WriteLine("═══ IOE_03: Entry-vector alignment gain vs N ═══");

        int[] Ns={62,63,64,65,66,67,70,72};
        var profiles=new ConcurrentBag<OnsetProfile>();

        Parallel.ForEach(Ns,n=>{
            var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<199;s++){
                if(IsHi(n,s))continue;
                var p=BuildProfile(n,s,hi,lo);
                if(p==null)continue;
                profiles.Add(p.Value);
            }
        });

        var all=profiles.ToArray();

        // Alignment metrics
        _o.WriteLine($"\n─── Entry-vector alignment vs N ───");
        _o.WriteLine(string.Format("{0,4} {1,10} {2,10} {3,10} {4,10} {5,10} {6,10}",
            "N","alignPre","alignPost","deltaAlign","respProj","respOrth","omT1"));
        foreach(var n in Ns){
            var sub=all.Where(p=>p.n==n).ToArray();
            if(sub.Length==0)continue;
            _o.WriteLine($"{n,4} {sub.Average(p=>p.entryAlignPre),10:F4} {sub.Average(p=>p.entryAlignPost),10:F4} {sub.Average(p=>p.deltaAlign),10:F4} {sub.Average(p=>p.respProjHi),10:F4} {sub.Average(p=>p.respOrthHi),10:F4} {sub.Average(p=>p.omT1),10:F4}");
        }

        // Alignment vs rescue at onset
        var n65=all.Where(p=>p.n==65).ToArray();
        var n66=all.Where(p=>p.n==66).ToArray();
        _o.WriteLine($"\n─── Alignment vs rescue ───");
        _o.WriteLine(string.Format("{0,4} {1,8} {2,8} {3,8} {4,8}",
            "N","resc%","rAlignPre","fAlignPre","rDeltaAl"));
        foreach(var grp in new[]{n65,n66}){
            var res=grp.Where(p=>p.rescued).ToArray();
            var fail=grp.Where(p=>!p.rescued).ToArray();
            double rAlignPre=res.Length>0?res.Average(p=>p.entryAlignPre):0;
            double fAlignPre=fail.Length>0?fail.Average(p=>p.entryAlignPre):0;
            double rDeltaAl=res.Length>0?res.Average(p=>p.deltaAlign):0;
            _o.WriteLine($"{grp[0].n,4} {res.Length*100.0/grp.Length,7:F0}% {rAlignPre,8:F4} {fAlignPre,8:F4} {rDeltaAl,8:F4}");
        }

        // Is alignment gain the onset trigger?
        var n64=all.Where(p=>p.n==64).ToArray();
        _o.WriteLine($"\n─── Alignment gain accumulation ───");
        foreach(var n in Ns){
            var sub=all.Where(p=>p.n==n).ToArray();
            if(sub.Length==0)continue;
            double posGain=sub.Count(p=>p.deltaAlign>0)*100.0/sub.Length;
            double sigGain=sub.Count(p=>Math.Abs(p.deltaAlign)>0.01)*100.0/sub.Length;
            _o.WriteLine($"N={n}: posAlignGain {posGain:F0}% sigAlignGain {sigGain:F0}%");
        }

        _o.WriteLine($"\n─── Next: IOE_04 Inducibility emergence ───");
    }

    // ═══════════════════════════════════════════════
    // IOE_04 — Inducibility emergence
    // ═══════════════════════════════════════════════
    [Fact]public void IOE_04_InducibilityEmergence(){
        _o.WriteLine("═══ IOE_04: Inducibility emergence — which metric changes first? ═══");

        int[] Ns={62,63,64,65,66,67};
        var profiles=new ConcurrentBag<OnsetProfile>();

        Parallel.ForEach(Ns,n=>{
            var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<299;s++){
                if(IsHi(n,s))continue;
                var p=BuildProfile(n,s,hi,lo);
                if(p==null)continue;
                profiles.Add(p.Value);
            }
        });

        var all=profiles.ToArray();

        // Per-N metric means
        var table=new Dictionary<int,Dictionary<string,double>>();
        foreach(var n in Ns){
            var sub=all.Where(p=>p.n==n).ToArray();
            if(sub.Length==0)continue;
            table[n]=new Dictionary<string,double>{
                ["distHi"]=sub.Average(p=>p.distHi),
                ["projHiVec"]=sub.Average(p=>Math.Abs(p.projHiVec)),
                ["offVecAngle"]=sub.Average(p=>p.offVecAngle),
                ["entryAlignPre"]=sub.Average(p=>Math.Abs(p.entryAlignPre)),
                ["deltaAlign"]=sub.Average(p=>Math.Abs(p.deltaAlign)),
                ["c3OmegaShift"]=sub.Average(p=>p.c3OmegaShift),
                ["c3AlignGain"]=sub.Average(p=>p.c3AlignGain),
                ["dT1"]=sub.Average(p=>p.dT1),
                ["rebMag"]=sub.Average(p=>p.rebMag),
                ["kCollapse"]=sub.Average(p=>Math.Abs(p.kCollapse)),
                ["rescueRate"]=sub.Count(p=>p.rescued)*100.0/sub.Length,
            };
        }

        _o.WriteLine($"\n─── Metric emergence: N=62→67 ───");
        _o.WriteLine(string.Format("{0,4} {1,8} {2,8} {3,8} {4,8} {5,8} {6,8} {7,8}",
            "N","distHi","projHi","c3OmgS","deltaAl","dT1","rebMag","rescue%"));
        foreach(var n in Ns){
            if(!table.ContainsKey(n))continue;
            var t=table[n];
            _o.WriteLine($"{n,4} {t["distHi"],8:F4} {t["projHiVec"],8:F4} {t["c3OmegaShift"],8:F4} {t["deltaAlign"],8:F4} {t["dT1"],8:F4} {t["rebMag"],8:F4} {t["rescueRate"],7:F0}%");
        }

        // First-changer detection: which metric changes most at 64→65?
        _o.WriteLine($"\n─── First-changer: N=64→65 ───");
        var changes=new List<(string metric,double ratio)>();
        if(table.ContainsKey(64)&&table.ContainsKey(65)){
            var t64=table[64];var t65=table[65];
            foreach(var k in t64.Keys.Where(k=>k!="rescueRate")){
                double r=t65[k]/Math.Max(1e-9,Math.Abs(t64[k]));
                changes.Add((k,Math.Abs(r-1)));
            }
            foreach(var c in changes.OrderByDescending(x=>x.ratio).Take(5))
                _o.WriteLine($"  {c.metric}: ×{table[65][c.metric]/Math.Max(1e-9,Math.Abs(table[64][c.metric])):F2}");
        }

        // Pre-onset signal detection
        _o.WriteLine($"\n─── Pre-onset signals (N=63→64, before first rescue) ───");
        if(table.ContainsKey(63)&&table.ContainsKey(64)){
            var t63=table[63];var t64=table[64];
            _o.WriteLine($"distHi: {t63["distHi"]:F4} → {t64["distHi"]:F4}");
            _o.WriteLine($"projHiVec: {t63["projHiVec"]:F4} → {t64["projHiVec"]:F4} ← pre-onset change");
            _o.WriteLine($"c3OmegaShift: {t63["c3OmegaShift"]:F4} → {t64["c3OmegaShift"]:F4}");
            _o.WriteLine($"deltaAlign: {t63["deltaAlign"]:F4} → {t64["deltaAlign"]:F4}");
            _o.WriteLine($"dT1: {t63["dT1"]:F4} → {t64["dT1"]:F4}");
        }

        // Gate assessment
        bool sharpOnset=table.ContainsKey(65)&&table[65]["rescueRate"]>0&&
            (!table.ContainsKey(64)||table[64]["rescueRate"]==0);
        _o.WriteLine($"\nGate A (Sharp onset): {(sharpOnset?"REACHED":"NOT REACHED")}");

        _o.WriteLine($"\n─── Next: IOE_05 Driver ranking ───");
    }

    // ═══════════════════════════════════════════════
    // IOE_05 — Onset driver ranking
    // ═══════════════════════════════════════════════
    [Fact]public void IOE_05_OnsetDriverRanking(){
        _o.WriteLine("═══ IOE_05: Onset driver ranking ═══");

        int[] Ns={62,63,64,65,66,67,70,72};
        var profiles=new ConcurrentBag<OnsetProfile>();

        Parallel.ForEach(Ns,n=>{
            var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<199;s++){
                if(IsHi(n,s))continue;
                var p=BuildProfile(n,s,hi,lo);
                if(p==null)continue;
                profiles.Add(p.Value);
            }
        });

        var all=profiles.ToArray();
        var pre=all.Where(p=>p.n<=64).ToArray();
        var post=all.Where(p=>p.n>=65).ToArray();

        // Driver ranking
        _o.WriteLine($"\n─── Onset driver ranking (pre-N≤64 vs post-N≥65) ───");
        var drivers=new List<(string label,double preMean,double postMean,double ratio)>();
        var metrics=new[]{("distHi","Basin distance",false),("projHiVec","projHiVec",false),
            ("offVecAngle","Off-vector angle",false),("entryAlignPre","Pre-C3 alignment",false),
            ("deltaAlign","C3 deltaAlign",false),("c3OmegaShift","C3 omega shift",true),
            ("c3AlignGain","C3 alignment gain",true),("dT1","Probe displacement",false),
            ("rebMag","rebMagnitude",false),("kCollapse","K collapse",false)};

        foreach(var (key,label,higherBetter) in metrics){
            double preMean=pre.Average(p=>GetMetric(p,key));
            double postMean=post.Average(p=>GetMetric(p,key));
            double preAbs=Math.Abs(preMean),postAbs=Math.Abs(postMean);
            double ratio=Math.Max(preAbs,postAbs)/Math.Max(1e-9,Math.Min(preAbs,postAbs));
            drivers.Add((label,preMean,postMean,ratio));
        }

        _o.WriteLine(string.Format("{0,-18} {1,10} {2,10} {3,8}",
            "Driver","Pre-onset","Post-onset","Contrast"));
        int rank=1;
        foreach(var d in drivers.OrderByDescending(x=>x.ratio)){
            string marker=rank<=3?"←":"";
            _o.WriteLine($"{rank,2}. {d.label,-15} {d.preMean,10:F4} {d.postMean,10:F4} {d.ratio,8:F1}x {marker}");
            rank++;
        }

        // Top driver analysis
        var top=drivers.OrderByDescending(x=>x.ratio).First();
        _o.WriteLine($"\nTop onset driver: {top.label} (×{top.ratio:F1} contrast)");

        // Is it single-driver or multi-driver?
        var top3=drivers.OrderByDescending(x=>x.ratio).Take(3).ToArray();
        bool singleDriver=top3[0].ratio>top3[1].ratio*2;
        _o.WriteLine($"\nGate C (C3 threshold): {(top.label.Contains("C3")?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate D (Single driver): {(singleDriver?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate E (Mixed onset): {(!singleDriver?"REACHED":"NOT REACHED")}");

        _o.WriteLine($"\n─── Analysis complete ───");
    }

    // ─── Helpers ───
    static double GetVal(OnsetProfile[] p,string m)=>m switch{
        "distHi"=>p.Average(x=>x.distHi),"projHiVec"=>p.Average(x=>x.projHiVec),
        "offVecAngle"=>p.Average(x=>x.offVecAngle),"entryAlignPre"=>p.Average(x=>x.entryAlignPre),
        "deltaAlign"=>p.Average(x=>x.deltaAlign),"c3OmegaShift"=>p.Average(x=>x.c3OmegaShift),
        "c3AlignGain"=>p.Average(x=>x.c3AlignGain),"dT1"=>p.Average(x=>x.dT1),
        "dT2"=>p.Average(x=>x.dT2),"rebMag"=>p.Average(x=>x.rebMag),
        "kCollapse"=>p.Average(x=>x.kCollapse),_=>0
    };

    static double GetMetric(OnsetProfile p,string m)=>m switch{
        "distHi"=>p.distHi,"projHiVec"=>Math.Abs(p.projHiVec),
        "offVecAngle"=>p.offVecAngle,"entryAlignPre"=>Math.Abs(p.entryAlignPre),
        "deltaAlign"=>Math.Abs(p.deltaAlign),"c3OmegaShift"=>p.c3OmegaShift,
        "c3AlignGain"=>p.c3AlignGain,"dT1"=>p.dT1,"rebMag"=>Math.Abs(p.rebMag),
        "kCollapse"=>Math.Abs(p.kCollapse),_=>0
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
