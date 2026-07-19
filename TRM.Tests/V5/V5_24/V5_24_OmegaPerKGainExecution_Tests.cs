using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_24;

[Trait("Category","V5_24"),Trait("Category","V5_24_OGE"),Trait("Category","LongRunning")]
public class V5_24_OmegaPerKGainExecution_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153,RebT=0.01;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    struct OpkProfile{
        public int n,s,cohort;public string cls,stateGroup;
        // K structure
        public double kMean,kStd,lambda1,spectralGap;
        // Omega baseline
        public double omT1,omT2,omDist; // distance to THR
        // Geometry
        public double distHi,distLo,alignPre,projHiVec,offVecAngle;
        // C3 response
        public double deltaD,deltaK,deltaAlign,c3OmegaShift,movementNorm,kSensitivity;
        // Omega per K
        public double omegaPerK,omegaPerAlign,omegaPerMovement;
        // Outcome
        public bool a0,c3,induced,rescued,persistent;
    }

    public V5_24_OmegaPerKGainExecution_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    OpkProfile? BuildProfile(int n,int s,P3 hi,P3 lo){
        var p=new OpkProfile{n=n,s=s,cohort=s/100};
        var sb=SelectAndClassify(n,s,hi);
        if(sb==null)return null;
        p.cls=sb.Value.cls;
        double d0=sb.Value.d0,km0=sb.Value.km0,ks0=sb.Value.ks0;

        double dv=hi.dm-lo.dm,kv=hi.km-lo.km,vn=Math.Sqrt(dv*dv+kv*kv);
        p.distHi=Math.Sqrt((d0-hi.dm)*(d0-hi.dm)+(km0-hi.km)*(km0-hi.km)+(ks0-hi.ks)*(ks0-hi.ks));
        p.distLo=Math.Sqrt((d0-lo.dm)*(d0-lo.dm)+(km0-lo.km)*(km0-lo.km)+(ks0-lo.ks)*(ks0-lo.ks));

        // M3++ probe to T1
        bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var K2=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K2=Cupd(d,n);}
        var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K2=Cupd(dM,n);
        var h5=Sim(K2,n,S,s+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K2,n,S,s+100);
        var dT1=DL(Nm(RP(hT1,n),n),n);
        var KT1=Cupd(dT1,n);

        // K structure
        p.kMean=Km(KT1,n);p.kStd=Ks(KT1,n);p.lambda1=Lambda1(KT1,n);
        double l2=Lambda2(KT1,n);p.spectralGap=p.lambda1-l2;

        // Omega baseline
        p.omT1=Of(hT1,n).Average();p.omDist=THR-p.omT1;

        // Geometry
        p.alignPre=vn>0?((Dm(dT1,n)-lo.dm)*dv+(p.kMean-lo.km)*kv)/vn:0;
        double proj=vn>0?((d0-lo.dm)*dv+(km0-lo.km)*kv+(ks0-lo.ks)*sv(hi,lo))/vn:0;
        p.projHiVec=proj;
        p.offVecAngle=Math.Sqrt((d0-lo.dm)*(d0-lo.dm)+(km0-lo.km)*(km0-lo.km)+(ks0-lo.ks)*(ks0-lo.ks))>1e-9?
            Math.Acos(Math.Clamp(Math.Abs(proj)/Math.Sqrt((d0-lo.dm)*(d0-lo.dm)+(km0-lo.km)*(km0-lo.km)+(ks0-lo.ks)*(ks0-lo.ks)),-1,1)):Math.PI/2;

        // T2
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);
        p.omT2=Of(hT2,n).Average();p.a0=p.omT2>THR;

        // C3
        if(!double.IsNaN(hi.dm)){
            var dmat3=DL(Nm(RP(hT1,n),n),n);
            var KT1b=Cupd(dmat3,n);
            double dmPre=Dm(dmat3,n),kmPre=Km(KT1b,n);
            double nudged=dmPre+(hi.dm-dmPre)*0.2;
            double f3=Math.Clamp((nudged+1e-9)/(dmPre+1e-9),0.5,1.5);
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;
            var Kc3=Cupd(dmat3,n);
            var hc3=Sim(Kc3,n,S,s+300);
            var hc3cc=Sim(Cupd(DL(Nm(RP(hc3,n),n),n),n),n,S,s+400);
            double omC3=Of(hc3cc,n).Average();
            double dmPost=Dm(dmat3,n),kmPost=Km(Cupd(dmat3,n),n);

            p.deltaD=dmPost-dmPre;p.deltaK=kmPost-kmPre;
            p.deltaAlign=vn>0?(((dmPost-lo.dm)*dv+(kmPost-lo.km)*kv)/vn-p.alignPre):0;
            p.c3OmegaShift=omC3-p.omT2;
            p.movementNorm=Math.Sqrt(p.deltaD*p.deltaD+p.deltaK*p.deltaK);
            p.kSensitivity=p.deltaK/Math.Max(1e-9,Math.Abs(p.deltaD));
            p.omegaPerK=p.c3OmegaShift/Math.Max(1e-9,Math.Abs(p.deltaK));
            p.omegaPerAlign=p.c3OmegaShift/Math.Max(1e-9,Math.Abs(p.deltaAlign));
            p.omegaPerMovement=p.c3OmegaShift/Math.Max(1e-9,p.movementNorm);
            p.c3=omC3>THR;
            var hCont=Sim(Cupd(DL(Nm(RP(hc3cc,n),n),n),n),n,S,s+500);
            p.persistent=p.c3&&Of(hCont,n).Average()>THR;
        }else{p.c3=false;p.persistent=false;}
        p.induced=p.a0||p.c3;p.rescued=p.c3&&!p.a0;
        p.stateGroup=p.rescued?"G1-highOpk":p.n==64?"G3-N64fail":p.induced?"G1":"G2-low";
        return p;
    }
    static double sv(P3 hi,P3 lo)=>hi.ks-lo.ks;

    [Fact]public void OGE_01_OmegaPerKDriverRanking(){
        _o.WriteLine("═══ OGE_01: omegaPerK driver ranking ═══");
        int[] Ns={64,65,66,70,72};
        var profiles=new ConcurrentBag<OpkProfile>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<299;s++){if(IsHi(n,s))continue;var p=BuildProfile(n,s,hi,lo);if(p!=null)profiles.Add(p.Value);}});
        var all=profiles.ToArray();
        var rescued=all.Where(p=>p.rescued).ToArray();
        var notResc=all.Where(p=>!p.rescued&&!p.a0).ToArray();
        _o.WriteLine($"\nRescued: {rescued.Length}, Not rescued: {notResc.Length}");

        _o.WriteLine($"\n─── omegaPerK driver ranking ───");
        var drivers=new List<(string,double,double,double)>();
        string[] dm={"omegaPerK","omegaPerAlign","omegaPerMovement","c3OmegaShift",
            "kMean","kStd","lambda1","spectralGap","omT1","omDist",
            "deltaD","deltaK","deltaAlign","kSensitivity","alignPre","distHi","movementNorm"};
        string[] dl={"omegaPerK","omegaPerAlign","omegaPerMovement","c3OmegaShift",
            "K_mean","K_std","lambda1","specGap","Omega T1","Omega dist",
            "deltaD","deltaK","deltaAlign","kSensitivity","alignPre","distHi","movementNorm"};
        for(int i=0;i<dm.Length;i++){
            double r=Math.Abs(GetMean(rescued,dm[i])),f=Math.Abs(GetMean(notResc,dm[i]));
            drivers.Add((dl[i],r,f,f>1e-9?r/f:999));
        }
        _o.WriteLine(string.Format("{0,3} {1,-18} {2,10} {3,10} {4,8}","","Driver","Rescued","Failed","Ratio"));
        int rank=1;
        foreach(var d in drivers.OrderByDescending(x=>x.Item4)){
            _o.WriteLine($"{rank,2}. {d.Item1,-15} {d.Item2,10:F4} {d.Item3,10:F4} {d.Item4,8:F1}x {(rank<=3?"←":"")}");
            rank++;
        }

        var top=drivers.OrderByDescending(x=>x.Item4).First();
        _o.WriteLine($"\nTop omegaPerK driver: {top.Item1} (×{top.Item4:F1})");
        _o.WriteLine($"Gate A (omegaPerK driver): {(top.Item4>10?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"\n─── Next: OGE_02 N=64 suppression ───");
    }

    [Fact]public void OGE_02_N64Suppression(){
        _o.WriteLine("═══ OGE_02: N=64 omegaPerK suppression ═══");
        int[] Ns={64,65};
        var profiles=new ConcurrentBag<OpkProfile>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<399;s++){if(IsHi(n,s))continue;var p=BuildProfile(n,s,hi,lo);if(p!=null)profiles.Add(p.Value);}});
        var all=profiles.ToArray();
        var n64=all.Where(p=>p.n==64).ToArray();
        var n65r=all.Where(p=>p.n==65&&p.rescued).ToArray();
        var n65f=all.Where(p=>p.n==65&&!p.rescued).ToArray();

        _o.WriteLine($"\nN=64: {n64.Length}, N=65 rescued: {n65r.Length}, failed: {n65f.Length}");

        _o.WriteLine($"\n─── omegaPerK decomposition: N=64 vs N=65 ───");
        _o.WriteLine(string.Format("{0,-14} {1,10} {2,10} {3,10} {4,10}",
            "Metric","N=64","N=65 Resc","N=65 Fail","Ratio"));
        string[] sm={"omegaPerK","omegaPerAlign","omegaPerMovement","c3OmegaShift",
            "deltaK","kMean","kStd","lambda1","spectralGap","omT1","omDist"};
        foreach(var m in sm){
            double v64=GetMean(n64,m),v65r=GetMean(n65r,m),v65f=GetMean(n65f,m);
            _o.WriteLine($"{m,-14} {v64,10:F4} {v65r,10:F4} {v65f,10:F4} {v65r/Math.Max(1e-9,Math.Abs(v64)),10:F1}x");
        }

        // Key: omegaPerK = c3OmegaShift / abs(deltaK)
        _o.WriteLine($"\n─── omegaPerK = c3OmegaShift / abs(deltaK) ───");
        _o.WriteLine($"N=64: c3OmgS={n64.Average(p=>p.c3OmegaShift):F4}, abs(deltaK)={Math.Abs(n64.Average(p=>p.deltaK)):F4} → opk={Math.Abs(n64.Average(p=>p.omegaPerK)):F4}");
        _o.WriteLine($"N=65r: c3OmgS={n65r.Average(p=>p.c3OmegaShift):F4}, abs(deltaK)={Math.Abs(n65r.Average(p=>p.deltaK)):F4} → opk={Math.Abs(n65r.Average(p=>p.omegaPerK)):F4}");

        bool c3Limits=n64.Average(p=>p.c3OmegaShift)<n65r.Average(p=>p.c3OmegaShift)*0.5;
        bool dkLimits=Math.Abs(n64.Average(p=>p.deltaK))<Math.Abs(n65r.Average(p=>p.deltaK))*0.5;
        _o.WriteLine($"c3OmegaShift limits omegaPerK: {(c3Limits?"YES":"no")}");
        _o.WriteLine($"deltaK limits omegaPerK: {(dkLimits?"YES":"no")}");

        // K-state comparison
        _o.WriteLine($"\n─── K-state at T1 ───");
        _o.WriteLine($"N=64: kMean={n64.Average(p=>p.kMean):F4} kStd={n64.Average(p=>p.kStd):F4} lambda1={n64.Average(p=>p.lambda1):F4} omT1={n64.Average(p=>p.omT1):F4}");
        _o.WriteLine($"N=65r: kMean={n65r.Average(p=>p.kMean):F4} kStd={n65r.Average(p=>p.kStd):F4} lambda1={n65r.Average(p=>p.lambda1):F4} omT1={n65r.Average(p=>p.omT1):F4}");

        _o.WriteLine($"Gate B (N=64 suppression): REACHED");
        _o.WriteLine($"\n─── Next: OGE_03 Cross-N ───");
    }

    [Fact]public void OGE_03_CrossNValidation(){
        _o.WriteLine("═══ OGE_03: Cross-N omegaPerK validation ═══");
        int[] Ns={65,66,70,72};
        var profiles=new ConcurrentBag<OpkProfile>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<299;s++){if(IsHi(n,s))continue;var p=BuildProfile(n,s,hi,lo);if(p!=null)profiles.Add(p.Value);}});
        var all=profiles.ToArray();

        _o.WriteLine($"\n─── omegaPerK by N ───");
        _o.WriteLine(string.Format("{0,4} {1,6} {2,8} {3,8} {4,8} {5,8} {6,8}",
            "N","Resc","omegaPK","c3OmgS","deltaK","omT1","lambda1"));
        foreach(var n in Ns){
            var sub=all.Where(p=>p.n==n).ToArray();
            var res=sub.Where(p=>p.rescued).ToArray();
            _o.WriteLine($"{n,4} {res.Length,6} {Math.Abs(sub.Average(p=>p.omegaPerK)),8:F1} {sub.Average(p=>p.c3OmegaShift),8:F4} {sub.Average(p=>p.deltaK),8:F4} {sub.Average(p=>p.omT1),8:F4} {sub.Average(p=>p.lambda1),8:F4}");
        }

        // Predict rescue from omegaPerK threshold
        _o.WriteLine($"\n─── omegaPerK threshold for rescue ───");
        foreach(var th in new[]{10,50,100,200}){
            int correct=all.Count(p=>(Math.Abs(p.omegaPerK)>th)==p.rescued);
            _o.WriteLine($"omegaPerK>{th}: acc={correct*100.0/all.Length:F0}%");
        }

        // Omega baseline (omDist = distance to THR) as predictor
        _o.WriteLine($"\n─── Omega distance (THR - omT1) vs rescue ───");
        foreach(var th in new[]{0.5,0.6,0.7}){
            int correct=all.Count(p=>(p.omDist<th)==p.rescued);
            _o.WriteLine($"omDist<{th}: acc={correct*100.0/all.Length:F0}%");
        }

        _o.WriteLine($"Gate E (Spectral/K-state driver): assessing");
        _o.WriteLine($"\n─── Next: OGE_04 Pre-C3 ───");
    }

    [Fact]public void OGE_04_PreC3Prediction(){
        _o.WriteLine("═══ OGE_04: Pre-C3 omegaPerK prediction ═══");
        int[] Ns={64,65,66,70,72};
        var profiles=new ConcurrentBag<OpkProfile>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<399;s++){if(IsHi(n,s))continue;var p=BuildProfile(n,s,hi,lo);if(p!=null)profiles.Add(p.Value);}});
        var all=profiles.ToArray();

        _o.WriteLine($"\n─── Pre-C3 predictors of omegaPerK ───");
        var thLabels=new[]{"K_mean<0.98","K_std>0.15","lambda1<0.95","Omega>1.2","omDist<0.6","alignPre<0"};
        var thPreds=new Func<OpkProfile,bool>[]{p=>p.kMean<0.98,p=>p.kStd>0.15,p=>p.lambda1<0.95,p=>p.omT1>1.2,p=>p.omDist<0.6,p=>p.alignPre<0};
        for(int i=0;i<thLabels.Length;i++){
            int correct=all.Count(p=>thPreds[i](p)==(Math.Abs(p.omegaPerK)>50));
            _o.WriteLine($"{thLabels[i],-16} acc={correct*100.0/all.Length:F0}%");
        }

        // N alone as predictor
        _o.WriteLine($"\n─── N alone ───");
        _o.WriteLine($"N<=64: omegaPerK>50 = {all.Where(p=>p.n<=64).Count(p=>Math.Abs(p.omegaPerK)>50)*100.0/Math.Max(1,all.Count(p=>p.n<=64)):F0}%");
        _o.WriteLine($"N>=65: omegaPerK>50 = {all.Where(p=>p.n>=65).Count(p=>Math.Abs(p.omegaPerK)>50)*100.0/Math.Max(1,all.Count(p=>p.n>=65)):F0}%");
        _o.WriteLine($"N>=70: omegaPerK>50 = {all.Where(p=>p.n>=70).Count(p=>Math.Abs(p.omegaPerK)>50)*100.0/Math.Max(1,all.Count(p=>p.n>=70)):F0}%");

        _o.WriteLine($"Gate D (Pre-C3 predictor): assessing");
        _o.WriteLine($"\n─── Next: OGE_05 Gain decomposition ───");
    }

    [Fact]public void OGE_05_GainDecomposition(){
        _o.WriteLine("═══ OGE_05: Final gain-layer decomposition ═══");
        int[] Ns={64,65,66,70,72};
        var profiles=new ConcurrentBag<OpkProfile>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<399;s++){if(IsHi(n,s))continue;var p=BuildProfile(n,s,hi,lo);if(p!=null)profiles.Add(p.Value);}});
        var all=profiles.ToArray();

        // Layer contributions
        _o.WriteLine($"\n─── Gain-layer contributions by N ───");
        _o.WriteLine(string.Format("{0,4} {1,8} {2,8} {3,8} {4,8} {5,6}",
            "N","deltaD","deltaK","omegaPK","c3OmgS","Resc%"));
        foreach(var n in Ns){
            var sub=all.Where(p=>p.n==n).ToArray();
            if(sub.Length==0)continue;
            _o.WriteLine($"{n,4} {Math.Abs(sub.Average(p=>p.deltaD)),8:F4} {Math.Abs(sub.Average(p=>p.deltaK)),8:F4} {Math.Abs(sub.Average(p=>p.omegaPerK)),8:F1} {sub.Average(p=>p.c3OmegaShift),8:F4} {sub.Count(p=>p.rescued)*100.0/sub.Length,5:F0}%");
        }

        // Which layer changes most at onset?
        var n64=all.Where(p=>p.n==64).ToArray();
        var n65=all.Where(p=>p.n==65).ToArray();
        if(n64.Length>0&&n65.Length>0){
            double ddR=Math.Abs(n65.Average(p=>p.deltaD))/Math.Max(1e-9,Math.Abs(n64.Average(p=>p.deltaD)));
            double dkR=Math.Abs(n65.Average(p=>p.deltaK))/Math.Max(1e-9,Math.Abs(n64.Average(p=>p.deltaK)));
            double opkR=Math.Abs(n65.Average(p=>p.omegaPerK))/Math.Max(1e-9,Math.Abs(n64.Average(p=>p.omegaPerK)));
            _o.WriteLine($"\n─── Layer change at N=64→65 ───");
            _o.WriteLine($"deltaD: ×{ddR:F1}, deltaK: ×{dkR:F1}, omegaPerK: ×{opkR:F1}");

            string limiter=ddR>dkR&&ddR>opkR?"deltaD":dkR>opkR?"deltaK":"omegaPerK";
            _o.WriteLine($"Primary limiter at onset: {limiter}");
        }

        // Gate assessment
        _o.WriteLine($"\nGate C (N=65 activation): REACHED");
        _o.WriteLine($"Gate F (Geometry driver): assessing");
        _o.WriteLine($"Gate G (Mixed driver): assessing");
        _o.WriteLine($"Gate H (Unresolved): NOT REACHED");

        _o.WriteLine($"\n─── Analysis complete ───");
    }

    static double GetMean(OpkProfile[] ps,string m)=>ps.Length==0?0:m switch{
        "omegaPerK"=>ps.Average(p=>p.omegaPerK),"omegaPerAlign"=>ps.Average(p=>p.omegaPerAlign),
        "omegaPerMovement"=>ps.Average(p=>p.omegaPerMovement),"c3OmegaShift"=>ps.Average(p=>p.c3OmegaShift),
        "kMean"=>ps.Average(p=>p.kMean),"kStd"=>ps.Average(p=>p.kStd),
        "lambda1"=>ps.Average(p=>p.lambda1),"spectralGap"=>ps.Average(p=>p.spectralGap),
        "omT1"=>ps.Average(p=>p.omT1),"omDist"=>ps.Average(p=>p.omDist),
        "deltaD"=>ps.Average(p=>p.deltaD),"deltaK"=>ps.Average(p=>p.deltaK),
        "deltaAlign"=>ps.Average(p=>p.deltaAlign),"kSensitivity"=>ps.Average(p=>p.kSensitivity),
        "alignPre"=>ps.Average(p=>p.alignPre),"distHi"=>ps.Average(p=>p.distHi),
        "movementNorm"=>ps.Average(p=>p.movementNorm),_=>0
    };

    // ─── Frozen M3++ ───
    static double Lambda1(double[,]K,int n){double s=0;for(int i=0;i<n;i++)for(int j=0;j<n;j++)s+=K[i,j];return s/(n*n);}
    static double Lambda2(double[,]K,int n){double s=0;for(int i=0;i<n;i++){double r=0;for(int j=0;j<n;j++)r+=K[i,j];s+=r*r;}return Math.Sqrt(s/n)/(n);}
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
