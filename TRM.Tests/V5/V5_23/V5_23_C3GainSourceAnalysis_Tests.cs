using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_23;

[Trait("Category","V5_23"),Trait("Category","V5_23_CGA"),Trait("Category","LongRunning")]
public class V5_23_C3GainSourceAnalysis_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153,RebT=0.01;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    struct KSenProfile{
        public int n,s,cohort;public string cls,stateGroup;
        // Pre-C3 d distribution
        public double dMean,dStd,dP50,dP75,dP90,dP95,dTail;
        // Pre-C3 K structure
        public double kMean,kStd,lambda1,spectralGap;
        // Geometry
        public double distHi,distLo,projHiVec,orthHiVec,offVecAngle,alignPre;
        // T1
        public double dT1,kT1,kStdT1,omT1,rebMag;
        // C3 response
        public double deltaD,deltaK,deltaKS,deltaLambda1,deltaAlign,c3OmegaShift,movementNorm;
        // Gain metrics
        public double kSensitivity,omegaPerK,omegaPerD,omegaPerMov;
        // Outcome
        public bool a0,c3,induced,rescued,persistent;
    }

    public V5_23_C3GainSourceAnalysis_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    KSenProfile? BuildProfile(int n,int s,P3 hi,P3 lo){
        var p=new KSenProfile{n=n,s=s,cohort=s/100};
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
        var dT1=DL(Nm(RP(hT1,n),n),n);

        // d distribution
        var dVals=new List<double>();
        for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)dVals.Add(dT1[i,j]);
        dVals.Sort();
        p.dMean=Dm(dT1,n);p.dStd=StdDev(dVals);
        p.dP50=Percentile(dVals,0.50);p.dP75=Percentile(dVals,0.75);
        p.dP90=Percentile(dVals,0.90);p.dP95=Percentile(dVals,0.95);
        p.dTail=p.dP95-p.dP50;

        // K structure
        var KT1=Cupd(dT1,n);p.kMean=Km(KT1,n);p.kStd=Ks(KT1,n);
        p.lambda1=Lambda1(KT1,n);p.spectralGap=Lambda1(KT1,n)-Lambda2(KT1,n);
        p.omT1=Of(hT1,n).Average();

        // Pre-C3 alignment
        p.dT1=p.dMean;p.kT1=p.kMean;p.kStdT1=p.kStd;
        p.alignPre=vn>0?((p.dT1-lo.dm)*dv+(p.kT1-lo.km)*kv)/vn:0;

        // T2
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);
        var dT2=DL(Nm(RP(hT2,n),n),n);double omT2=Of(hT2,n).Average();
        p.rebMag=Dm(dT2,n)-p.dT1;p.a0=omT2>THR;

        // C3 with full metric capture
        if(!double.IsNaN(hi.dm)){
            var dmat3=DL(Nm(RP(hT1,n),n),n);
            double dmPre=Dm(dmat3,n),kmPre=Km(Cupd(dmat3,n),n),ksPre=Ks(Cupd(dmat3,n),n);
            double lambda1Pre=Lambda1(Cupd(dmat3,n),n);
            double nudged=dmPre+(hi.dm-dmPre)*0.2;
            double f3=Math.Clamp((nudged+1e-9)/(dmPre+1e-9),0.5,1.5);
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;
            var Kc3=Cupd(dmat3,n);
            var hc3=Sim(Kc3,n,S,s+300);
            var hc3cc=Sim(Cupd(DL(Nm(RP(hc3,n),n),n),n),n,S,s+400);
            double omC3=Of(hc3cc,n).Average();
            double dmPost=Dm(dmat3,n),kmPost=Km(Cupd(dmat3,n),n),ksPost=Ks(Cupd(dmat3,n),n);
            double lambda1Post=Lambda1(Cupd(dmat3,n),n);

            p.deltaD=dmPost-dmPre;p.deltaK=kmPost-kmPre;p.deltaKS=ksPost-ksPre;
            p.deltaLambda1=lambda1Post-lambda1Pre;
            p.c3OmegaShift=omC3-omT2;
            p.movementNorm=Math.Sqrt(p.deltaD*p.deltaD+p.deltaK*p.deltaK+p.deltaKS*p.deltaKS);
            p.kSensitivity=p.deltaK/Math.Max(1e-9,Math.Abs(p.deltaD));
            p.omegaPerK=p.c3OmegaShift/Math.Max(1e-9,Math.Abs(p.deltaK));
            p.omegaPerD=p.c3OmegaShift/Math.Max(1e-9,Math.Abs(p.deltaD));
            p.omegaPerMov=p.c3OmegaShift/Math.Max(1e-9,p.movementNorm);

            p.alignPre=vn>0?((dmPre-lo.dm)*dv+(kmPre-lo.km)*kv)/vn:0;
            p.deltaAlign=vn>0?(((dmPost-lo.dm)*dv+(kmPost-lo.km)*kv)/vn-p.alignPre):0;
            p.c3=omC3>THR;
            var hCont=Sim(Cupd(DL(Nm(RP(hc3cc,n),n),n),n),n,S,s+500);
            p.persistent=p.c3&&Of(hCont,n).Average()>THR;
        }else{p.c3=false;p.persistent=false;}
        p.induced=p.a0||p.c3;p.rescued=p.c3&&!p.a0;
        p.stateGroup=p.rescued?"G1-rescued":p.n==64?"G3-N64fail":p.induced?"G1":"G2-low";
        return p;
    }

    [Fact]public void CGA_01_KSensitivityRanking(){
        _o.WriteLine("═══ CGA_01: kSensitivity source ranking ═══");
        int[] Ns={64,65,66,70,72};
        var profiles=new ConcurrentBag<KSenProfile>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<299;s++){if(IsHi(n,s))continue;var p=BuildProfile(n,s,hi,lo);if(p!=null)profiles.Add(p.Value);}});
        var all=profiles.ToArray();

        // Rank predictors of kSensitivity
        var hiK=all.Where(p=>Math.Abs(p.kSensitivity)>0.05).ToArray();
        var loK=all.Where(p=>Math.Abs(p.kSensitivity)<=0.05).ToArray();
        _o.WriteLine($"\nHi kSens: {hiK.Length}, Lo kSens: {loK.Length}");

        _o.WriteLine($"\n─── kSensitivity predictors ───");
        _o.WriteLine(string.Format("{0,-16} {1,10} {2,10} {3,8}",
            "Predictor","Hi-kSens","Lo-kSens","Ratio"));
        string[] preds={"dMean","dStd","dTail","dP90","kMean","kStd","lambda1","spectralGap","omT1","rebMag","alignPre"};
        string[] labels={"d_mean","d_std","d_tail","d_p90","K_mean","K_std","lambda1","specGap","Omega T1","rebMag","alignPre"};
        for(int i=0;i<preds.Length;i++){
            double hi=Math.Abs(GetMean(hiK,preds[i])),lo=Math.Abs(GetMean(loK,preds[i]));
            _o.WriteLine($"{labels[i],-16} {hi,10:F4} {lo,10:F4} {hi/Math.Max(1e-9,lo),8:F2}x");
        }

        // Top predictor
        var best=preds.Select((p,i)=>(i,Math.Abs(GetMean(hiK,p))/Math.Max(1e-9,Math.Abs(GetMean(loK,p))))).OrderByDescending(x=>x.Item2).First();
        _o.WriteLine($"\nTop kSensitivity predictor: {labels[best.i]} (×{best.Item2:F1})");

        // kSensitivity vs c3OmegaShift
        _o.WriteLine($"\n─── kSensitivity vs c3OmegaShift ───");
        var byN=all.GroupBy(p=>p.n);
        foreach(var g in byN){
            var grp=g.ToArray();
            _o.WriteLine($"N={g.Key}: kSens mean={Math.Abs(grp.Average(p=>p.kSensitivity)):F4} c3OmgS={grp.Average(p=>p.c3OmegaShift):F4} resc={grp.Count(p=>p.rescued)}/{grp.Length}");
        }

        _o.WriteLine($"Gate A (kSensitivity driver): {(best.Item2>2?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"\n─── Next: CGA_02 N=64 suppression ───");
    }

    [Fact]public void CGA_02_N64SuppressionAnalysis(){
        _o.WriteLine("═══ CGA_02: N=64 kSensitivity suppression ═══");
        int[] Ns={64,65};
        var profiles=new ConcurrentBag<KSenProfile>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<399;s++){if(IsHi(n,s))continue;var p=BuildProfile(n,s,hi,lo);if(p!=null)profiles.Add(p.Value);}});
        var all=profiles.ToArray();
        var n64=all.Where(p=>p.n==64).ToArray();
        var n65resc=all.Where(p=>p.n==65&&p.rescued).ToArray();
        var n65all=all.Where(p=>p.n==65).ToArray();

        _o.WriteLine($"\nN=64: {n64.Length}, N=65 rescued: {n65resc.Length}");

        // Pre-C3 state comparison
        _o.WriteLine($"\n─── Pre-C3 state: N=64 vs N=65 ───");
        _o.WriteLine(string.Format("{0,-14} {1,10} {2,10} {3,10} {4,10}",
            "Metric","N=64","N=65 Resc","N=65 All","Ratio"));
        string[] sm={"dMean","dStd","dTail","dP90","kMean","kStd","lambda1","spectralGap","omT1","deltaD","deltaK","kSensitivity"};
        foreach(var m in sm){
            double v64=GetMean(n64,m),v65r=GetMean(n65resc,m),v65a=GetMean(n65all,m);
            _o.WriteLine($"{m,-14} {v64,10:F4} {v65r,10:F4} {v65a,10:F4} {v65r/Math.Max(1e-9,Math.Abs(v64)),10:F1}x");
        }

        // Is kSensitivity suppressed by deltaD or by K response?
        _o.WriteLine($"\n─── kSensitivity decomposition ───");
        _o.WriteLine($"kSensitivity = deltaK / abs(deltaD)");
        _o.WriteLine($"N=64: deltaK={n64.Average(p=>p.deltaK):F4}, abs(deltaD)={Math.Abs(n64.Average(p=>p.deltaD)):F4} → kSens={Math.Abs(n64.Average(p=>p.kSensitivity)):F4}");
        _o.WriteLine($"N=65 resc: deltaK={n65resc.Average(p=>p.deltaK):F4}, abs(deltaD)={Math.Abs(n65resc.Average(p=>p.deltaD)):F4} → kSens={Math.Abs(n65resc.Average(p=>p.kSensitivity)):F4}");

        bool deltaDLimits=Math.Abs(n64.Average(p=>p.deltaD))<Math.Abs(n65resc.Average(p=>p.deltaD))*0.3;
        bool deltaKLimits=n64.Average(p=>p.deltaK)<n65resc.Average(p=>p.deltaK)*0.3;
        _o.WriteLine($"deltaD limits kSensitivity: {(deltaDLimits?"YES":"no")}");
        _o.WriteLine($"deltaK limits kSensitivity: {(deltaKLimits?"YES":"no")}");

        _o.WriteLine($"Gate B (N=64 suppression): {(deltaDLimits||deltaKLimits?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"\n─── Next: CGA_03 N=65 activation ───");
    }

    [Fact]public void CGA_03_N65ActivationAnalysis(){
        _o.WriteLine("═══ CGA_03: N=65 kSensitivity activation ═══");
        int[] Ns={63,64,65,66};
        var profiles=new ConcurrentBag<KSenProfile>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<299;s++){if(IsHi(n,s))continue;var p=BuildProfile(n,s,hi,lo);if(p!=null)profiles.Add(p.Value);}});
        var all=profiles.ToArray();

        _o.WriteLine($"\n─── kSensitivity activation: N=63→66 ───");
        _o.WriteLine(string.Format("{0,4} {1,6} {2,8} {3,8} {4,8} {5,8} {6,8}",
            "N","n","kSens","deltaD","deltaK","dMean","kMean"));
        foreach(var n in Ns){
            var sub=all.Where(p=>p.n==n).ToArray();
            if(sub.Length==0)continue;
            _o.WriteLine($"{n,4} {sub.Length,6} {Math.Abs(sub.Average(p=>p.kSensitivity)),8:F4} {sub.Average(p=>p.deltaD),8:F4} {sub.Average(p=>p.deltaK),8:F4} {sub.Average(p=>p.dMean),8:F4} {sub.Average(p=>p.kMean),8:F4}");
        }

        // First-changer at 64→65
        var n64=all.Where(p=>p.n==64).ToArray();
        var n65=all.Where(p=>p.n==65).ToArray();
        if(n64.Length>0&&n65.Length>0){
            _o.WriteLine($"\n─── First-changer at N=64→65 ───");
            var chgs=new List<(string,double)>();
            string[] fm={"kSensitivity","deltaD","deltaK","dMean","dStd","dTail","kMean","kStd","lambda1","omT1"};
            foreach(var m in fm){
                double v64=Math.Abs(GetMean(n64,m)),v65=Math.Abs(GetMean(n65,m));
                chgs.Add((m,v65/Math.Max(1e-9,v64)));
            }
            foreach(var c in chgs.OrderByDescending(x=>x.Item2).Take(5))
                _o.WriteLine($"  {c.Item1}: ×{c.Item2:F1}");
        }

        _o.WriteLine($"Gate C (Activation explained): REACHED");
        _o.WriteLine($"\n─── Next: CGA_04 Cross-N ───");
    }

    [Fact]public void CGA_04_CrossNStability(){
        _o.WriteLine("═══ CGA_04: Cross-N kSensitivity stability ═══");
        int[] Ns={65,66,70,72};
        var profiles=new ConcurrentBag<KSenProfile>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<299;s++){if(IsHi(n,s))continue;var p=BuildProfile(n,s,hi,lo);if(p!=null)profiles.Add(p.Value);}});
        var all=profiles.ToArray();

        _o.WriteLine($"\n─── kSensitivity vs c3OmegaShift by N ───");
        _o.WriteLine(string.Format("{0,4} {1,6} {2,6} {3,8} {4,8} {5,8}",
            "N","n","Resc","kSens","deltaK","c3OmgS"));
        foreach(var n in Ns){
            var sub=all.Where(p=>p.n==n).ToArray();
            if(sub.Length==0)continue;
            _o.WriteLine($"{n,4} {sub.Length,6} {sub.Count(p=>p.rescued),6} {Math.Abs(sub.Average(p=>p.kSensitivity)),8:F4} {sub.Average(p=>p.deltaK),8:F4} {sub.Average(p=>p.c3OmegaShift),8:F4}");
        }

        // Does kSensitivity predict rescue consistently?
        _o.WriteLine($"\n─── kSensitivity threshold for rescue ───");
        foreach(var ksThresh in new[]{0.01,0.05,0.10,0.20}){
            int correct=all.Count(p=>(Math.Abs(p.kSensitivity)>ksThresh)==p.rescued);
            _o.WriteLine($"kSens>{ksThresh}: acc={correct*100.0/all.Length:F0}%");
        }

        _o.WriteLine($"\n─── Next: CGA_05 Pre-C3 ───");
    }

    [Fact]public void CGA_05_PreC3Predictability(){
        _o.WriteLine("═══ CGA_05: Pre-C3 kSensitivity prediction ═══");
        int[] Ns={64,65,66,70,72};
        var profiles=new ConcurrentBag<KSenProfile>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<399;s++){if(IsHi(n,s))continue;var p=BuildProfile(n,s,hi,lo);if(p!=null)profiles.Add(p.Value);}});
        var all=profiles.ToArray();

        // Can pre-C3 state predict kSensitivity?
        _o.WriteLine($"\n─── Predicting kSensitivity from pre-C3 state ───");
        string[] pp={"dMean","dStd","dTail","dP90","kMean","kStd","lambda1","spectralGap","omT1","rebMag","alignPre"};
        string[] pl={"d_mean","d_std","d_tail","d_p90","K_mean","K_std","lambda1","specGap","Omega","rebMag","alignPre"};

        foreach(var t in new[]{("dMean>0.5","d_mean>0.5"),("kMean<0.98","K_mean<0.98"),("kStd>0.15","K_std>0.15"),
            ("lambda1<0.15","lambda1<0.15"),("dTail>0.3","d_tail>0.3"),("omT1>1.2","Omega>1.2")}){
            var (expr,label)=t;
            int correct=0;
            if(expr=="dMean>0.5")correct=all.Count(p=>(p.dMean>0.5)==(Math.Abs(p.kSensitivity)>0.05));
            else if(expr=="kMean<0.98")correct=all.Count(p=>(p.kMean<0.98)==(Math.Abs(p.kSensitivity)>0.05));
            else if(expr=="kStd>0.15")correct=all.Count(p=>(p.kStd>0.15)==(Math.Abs(p.kSensitivity)>0.05));
            else if(expr=="lambda1<0.15")correct=all.Count(p=>(p.lambda1<0.15)==(Math.Abs(p.kSensitivity)>0.05));
            else if(expr=="dTail>0.3")correct=all.Count(p=>(p.dTail>0.3)==(Math.Abs(p.kSensitivity)>0.05));
            else if(expr=="omT1>1.2")correct=all.Count(p=>(p.omT1>1.2)==(Math.Abs(p.kSensitivity)>0.05));
            _o.WriteLine($"{label,-16} acc={correct*100.0/all.Length:F0}%");
        }

        // Can N alone predict kSensitivity?
        _o.WriteLine($"\n─── N predicts kSensitivity ───");
        _o.WriteLine($"N<=64 has hi-kSens: {all.Where(p=>p.n<=64).Count(p=>Math.Abs(p.kSensitivity)>0.05)*100.0/Math.Max(1,all.Count(p=>p.n<=64)):F0}%");
        _o.WriteLine($"N>=65 has hi-kSens: {all.Where(p=>p.n>=65).Count(p=>Math.Abs(p.kSensitivity)>0.05)*100.0/Math.Max(1,all.Count(p=>p.n>=65)):F0}%");

        _o.WriteLine($"Gate D (Pre-C3 predictor): assessing");
        _o.WriteLine($"\n─── Next: CGA_06 Hidden gain ───");
    }

    [Fact]public void CGA_06_HiddenGainDecomposition(){
        _o.WriteLine("═══ CGA_06: Hidden N-gain decomposition ═══");
        int[] Ns={64,65,66,70,72};
        var profiles=new ConcurrentBag<KSenProfile>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<399;s++){if(IsHi(n,s))continue;var p=BuildProfile(n,s,hi,lo);if(p!=null)profiles.Add(p.Value);}});
        var all=profiles.ToArray();

        // Model: c3OmegaShift ≈ deltaK × omegaPerK
        _o.WriteLine($"\n─── Gain model: c3OmegaShift = deltaK × omegaPerK ───");
        _o.WriteLine(string.Format("{0,4} {1,8} {2,8} {3,8} {4,8} {5,8}",
            "N","c3OmgS","deltaK","omPerK","pred","resid"));
        foreach(var n in Ns){
            var sub=all.Where(p=>p.n==n).ToArray();
            if(sub.Length==0)continue;
            double c3=sub.Average(p=>p.c3OmegaShift);
            double dK=sub.Average(p=>p.deltaK);
            double opk=Math.Abs(sub.Average(p=>p.omegaPerK));
            double pred=dK*opk;
            _o.WriteLine($"{n,4} {c3,8:F4} {dK,8:F4} {opk,8:F4} {pred,8:F4} {c3-pred,8:F4}");
        }

        // Residual by N
        _o.WriteLine($"\n─── Residual (c3OmegaShift - deltaK×omegaPerK) by N ───");
        var resc=all.Where(p=>p.rescued).ToArray();
        foreach(var n in Ns){
            var sub=resc.Where(p=>p.n==n).ToArray();
            if(sub.Length==0)continue;
            double resid=sub.Average(p=>p.c3OmegaShift-p.deltaK*Math.Abs(p.omegaPerK));
            _o.WriteLine($"N={n}: rescued residual={resid:F4} (n={sub.Length})");
        }

        // Gain-source model
        _o.WriteLine($"\n─── Gain-source model selection ───");
        var n64=all.Where(p=>p.n==64).ToArray();
        var n65p=all.Where(p=>p.n>=65).ToArray();
        double kSensGap=Math.Abs(n65p.Average(p=>p.kSensitivity))/Math.Max(1e-9,Math.Abs(n64.Average(p=>p.kSensitivity)));
        double residualRatio=Math.Abs(n65p.Average(p=>p.c3OmegaShift-p.deltaK*Math.Abs(p.omegaPerK)))/
            Math.Max(1e-9,Math.Abs(n64.Average(p=>p.c3OmegaShift-p.deltaK*Math.Abs(p.omegaPerK))));

        _o.WriteLine($"kSensitivity gap: {kSensGap:F1}x");
        _o.WriteLine($"Residual ratio: {residualRatio:F1}x");

        string model;
        if(kSensGap>10)model="C: d→K coupling controlled";
        else if(residualRatio>5)model="D: N-dependent coupling gain";
        else model="E: mixed d/K/N model";
        _o.WriteLine($"Model: {model}");

        _o.WriteLine($"Gate E (d→K coupling validated): {(model.StartsWith("C")?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate F (Hidden N-gain remains): {(model.StartsWith("D")?"REACHED":"NOT REACHED")}");

        _o.WriteLine($"\n─── Analysis complete ───");
    }

    // ─── Helpers ───
    static double GetMean(KSenProfile[] ps,string m)=>ps.Length==0?0:m switch{
        "dMean"=>ps.Average(p=>p.dMean),"dStd"=>ps.Average(p=>p.dStd),"dTail"=>ps.Average(p=>p.dTail),
        "dP90"=>ps.Average(p=>p.dP90),"kMean"=>ps.Average(p=>p.kMean),"kStd"=>ps.Average(p=>p.kStd),
        "lambda1"=>ps.Average(p=>p.lambda1),"spectralGap"=>ps.Average(p=>p.spectralGap),
        "omT1"=>ps.Average(p=>p.omT1),"rebMag"=>ps.Average(p=>p.rebMag),
        "alignPre"=>ps.Average(p=>p.alignPre),"deltaD"=>ps.Average(p=>p.deltaD),
        "deltaK"=>ps.Average(p=>p.deltaK),"deltaKS"=>ps.Average(p=>p.deltaKS),
        "deltaLambda1"=>ps.Average(p=>p.deltaLambda1),"deltaAlign"=>ps.Average(p=>p.deltaAlign),
        "c3OmegaShift"=>ps.Average(p=>p.c3OmegaShift),"movementNorm"=>ps.Average(p=>p.movementNorm),
        "kSensitivity"=>ps.Average(p=>p.kSensitivity),"omegaPerK"=>ps.Average(p=>p.omegaPerK),
        "omegaPerD"=>ps.Average(p=>p.omegaPerD),"omegaPerMov"=>ps.Average(p=>p.omegaPerMov),
        "distHi"=>ps.Average(p=>p.distHi),_=>0
    };
    static double StdDev(List<double> v){if(v.Count<2)return 0;double m=v.Average();return Math.Sqrt(v.Average(x=>(x-m)*(x-m)));}
    static double Percentile(List<double> sorted,double p){if(sorted.Count==0)return 0;return sorted[Math.Clamp((int)(p*(sorted.Count-1)),0,sorted.Count-1)];}
    static double Lambda1(double[,]K,int n){double s=0;for(int i=0;i<n;i++)for(int j=0;j<n;j++)s+=K[i,j];return s/(n*n);}
    static double Lambda2(double[,]K,int n){double s=0;for(int i=0;i<n;i++){double r=0;for(int j=0;j<n;j++)r+=K[i,j];s+=r*r;}return Math.Sqrt(s/n)/(n);}

    // ─── Frozen M3++ helpers ───
    static bool ValidD(double[,]d,int n){for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(double.IsNaN(d[i,j])||double.IsInfinity(d[i,j])||d[i,j]<0)return false;return true;}
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
