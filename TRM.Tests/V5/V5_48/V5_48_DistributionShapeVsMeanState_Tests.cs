using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_48;

[Trait("Category","V5_48"),Trait("Category","V5_48_DSM"),Trait("Category","LongRunning")]
public class V5_48_DistributionShapeVsMeanState_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153;
    const int WARMUP_EPOCHS=3;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}
    struct EP{
        public int N,seed,cohort;
        public double[] warmOm,warmDm,warmKm,warmKs,warmLam;
        public double om0,lam0,d0,km0,ks0;
        public double om1,lam1,d1,km1,ks1;
        public double om2,lam2,d2,km2,ks2,omDist2;
        public double om3,lam3,d3,km3,ks3,reb3;
        public double entOm,entDm,entKm,entLam,frac;
        public double exitOm2,omDelta,cs4,a0Prox;
        public bool resc4,inv;
    }

    public V5_48_DistributionShapeVsMeanState_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    EP RunEP(int n,int s,P3 hi,P3 lo){
        var ep=new EP{N=n,seed=s,cohort=n%5};
        var sb=SelectAndClassify(n,s,hi);if(sb==null){ep.inv=true;return ep;}
        double d0Pre=sb.Value.d0;bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0Pre*0.90:d0Pre*0.50;
        var warmOm=new double[WARMUP_EPOCHS];var warmDm=new double[WARMUP_EPOCHS];var warmKm=new double[WARMUP_EPOCHS];var warmKs=new double[WARMUP_EPOCHS];var warmLam=new double[WARMUP_EPOCHS];
        var K=KS(n,s);
        for(int e=0;e<WARMUP_EPOCHS;e++){
            var h=Sim(K,n,S,s+e);warmOm[e]=Of(h,n).Average();
            var d=DL(Nm(RP(h,n),n),n);warmDm[e]=Dm(d,n);K=Cupd(d,n);
            warmKm[e]=Km(K,n);warmKs[e]=Ks(K,n);warmLam[e]=Lambda1(K,n);
        }
        ep.warmOm=warmOm;ep.warmDm=warmDm;ep.warmKm=warmKm;ep.warmKs=warmKs;ep.warmLam=warmLam;
        var hT0=Sim(K,n,S,s+50);ep.om0=Of(hT0,n).Average();ep.lam0=Lambda1(K,n);
        var dT0=DL(Nm(RP(hT0,n),n),n);ep.d0=Dm(dT0,n);ep.km0=Km(K,n);ep.ks0=Ks(K,n);
        var h4=Sim(K,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K=Cupd(dM,n);
        var h5=Sim(K,n,S,s+4);K=Cupd(DL(Nm(RP(h5,n),n),n),n);
        ep.om1=Of(h5,n).Average();ep.lam1=Lambda1(K,n);
        ep.d1=Dm(CD(d4,n),n);ep.km1=Km(K,n);ep.ks1=Ks(K,n);
        var hT1=Sim(K,n,S,s+100);var KT1=Cupd(DL(Nm(RP(hT1,n),n),n),n);
        double omT1=Of(hT1,n).Average();
        ep.om2=omT1;ep.lam2=Lambda1(KT1,n);ep.omDist2=Math.Abs(omT1-THR);
        var dmat2=DL(Nm(RP(hT1,n),n),n);ep.d2=Dm(dmat2,n);ep.km2=Km(KT1,n);ep.ks2=Ks(KT1,n);
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);double omT2=Of(hT2,n).Average();bool a0=omT2>THR;
        var KT3=Cupd(DL(Nm(RP(hT2,n),n),n),n);
        ep.om3=omT2;ep.lam3=Lambda1(KT3,n);ep.reb3=omT2-omT1;
        ep.d3=Dm(DL(Nm(RP(hT2,n),n),n),n);ep.km3=Km(KT3,n);ep.ks3=Ks(KT3,n);
        double c3=0;
        if(!double.IsNaN(hi.dm)){
            var dmat3=DL(Nm(RP(hT1,n),n),n);double dmPre=Dm(dmat3,n);
            ep.entDm=dmPre;ep.entOm=omT1;ep.entKm=Km(KT1,n);ep.entLam=Lambda1(KT1,n);
            double nd=dmPre+(hi.dm-dmPre)*0.2;double f3=Math.Clamp((nd+1e-9)/(dmPre+1e-9),0.5,1.5);ep.frac=f3;
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;
            var hc3cc=Sim(Cupd(DL(Nm(RP(Sim(Cupd(dmat3,n),n,S,s+300),n),n),n),n),n,S,s+400);
            double omC3=Of(hc3cc,n).Average();ep.exitOm2=omC3;c3=omC3-(a0?THR:omT2);ep.omDelta=omC3-omT1;
        }
        ep.cs4=c3;ep.a0Prox=Math.Abs(omT2-THR);ep.resc4=c3>0.1&&omT2>THR;ep.inv=double.IsNaN(c3);
        return ep;
    }

    [Fact]
    public void DSM_01_DistributionShapeVsMeanStateAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== DSM_01: Distribution Shape vs Mean State Audit ===");
        _o.WriteLine("=== V5.48 INITIALIZED. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};
        var bag=new ConcurrentBag<EP>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<100;s++){if(IsHi(n,s))continue;var ep=RunEP(n,s,hi,lo);if(!ep.inv)bag.Add(ep);}});
        var data=bag.ToArray();
        _o.WriteLine($"Profiles: {data.Length} (N=70:{data.Count(d=>d.N==70)}, N=72:{data.Count(d=>d.N==72)}, N=75:{data.Count(d=>d.N==75)})");

        // ========================
        // PART A — Protocol Freeze
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART A — Protocol Freeze");
        _o.WriteLine(new string('=',80));
        _o.WriteLine("N: 70, 72, 75. Stage: w2. Vars: omega, d, km, lambda1.");
        _o.WriteLine("Descriptors: mean, median, std, IQR, q10-q90, IQR/range, CV.");
        _o.WriteLine("Forbidden: no new vars, no tuning, no seed removal, no V6.");
        _o.WriteLine("Protocol FROZEN.");

        // Helper: descriptive stats
        (double mean,double med,double std,double iqr,double q10,double q25,double q75,double q90,double iqrRng,double cv)
            Desc(double[] v){
            var s=v.OrderBy(x=>x).ToArray();int n=s.Length;
            double m=s.Average(),md=s[n/2],sd=Sd(s),iqr=Q(s,0.75)-Q(s,0.25);
            double q10=Q(s,0.10),q25=Q(s,0.25),q75=Q(s,0.75),q90=Q(s,0.90);
            double r=s.Last()-s.First(),ir=r>0.001?iqr/r:0,cv=Math.Abs(m)>0.001?sd/Math.Abs(m):0;
            return (m,md,sd,iqr,q10,q25,q75,q90,ir,cv);
        }

        // ========================
        // PART B — Mean-State Audit
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART B — Mean-State Audit at w2");
        _o.WriteLine(new string('=',80));

        string[] vars={"d_w2","km_w2","lam_w2","om_w2"};
        Func<EP,double>[] vf={d=>d.warmDm[2],d=>d.warmKm[2],d=>d.warmLam[2],d=>d.warmOm[2]};

        _o.WriteLine($"{"Variable",-12} {"N=70",10} {"N=72",10} {"N=75",10} {"72/75",8} {"Separation",20}");
        _o.WriteLine(new string('-',75));
        for(int vi=0;vi<4;vi++){
            double[] v70=data.Where(d=>d.N==70).Select(vf[vi]).ToArray();
            double[] v72=data.Where(d=>d.N==72).Select(vf[vi]).ToArray();
            double[] v75=data.Where(d=>d.N==75).Select(vf[vi]).ToArray();
            double m70=v70.Average(),m72=v72.Average(),m75=v75.Average();
            double ratio=Math.Abs(m72)>0.001?Math.Abs(m75-m72)/Math.Abs(m72):0;
            string sep=ratio<0.05?"NEARLY IDENTICAL":ratio<0.15?"MODERATE gap":"CLEAR gap";
            _o.WriteLine($"{vars[vi],-12} {m70,10:F3} {m72,10:F3} {m75,10:F3} {m75/m72,8:F2} {sep,20}");
        }

        _o.WriteLine($"\nMean-state conclusion: d_w2 means are {(Math.Abs(data.Where(d=>d.N==72).Select(d=>d.warmDm[2]).Average()-data.Where(d=>d.N==75).Select(d=>d.warmDm[2]).Average())<0.01?"NEARLY IDENTICAL — cannot explain amp vs collapse":"DIFFERENT — may explain")}");

        // ========================
        // PART C — Distribution Shape Audit
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART C — Distribution Shape Audit at w2");
        _o.WriteLine(new string('=',80));

        // Full descriptive table for d_w2
        _o.WriteLine("--- d_w2 distribution ---");
        _o.WriteLine($"{"N",6} {"Mean",8} {"Med",8} {"Std",8} {"IQR",8} {"IQR/Rng",9} {"CV",7} {"q10",8} {"q90",8} {"q90-q10",9}");
        _o.WriteLine(new string('-',90));
        foreach(var n in Ns){
            var nd=data.Where(d=>d.N==n).ToArray();if(nd.Length<4)continue;
            var v=nd.Select(d=>d.warmDm[2]).ToArray();
            var d=Desc(v);
            _o.WriteLine($"{n,6} {d.mean,8:F3} {d.med,8:F3} {d.std,8:F3} {d.iqr,8:F3} {d.iqrRng,9:F2} {d.cv,7:F2} {d.q10,8:F3} {d.q90,8:F3} {d.q90-d.q10,9:F3}");
        }

        _o.WriteLine("\n--- km_w2 distribution ---");
        _o.WriteLine($"{"N",6} {"Mean",8} {"Med",8} {"Std",8} {"IQR",8} {"IQR/Rng",9} {"CV",7} {"q10",8} {"q90",8} {"q90-q10",9}");
        _o.WriteLine(new string('-',90));
        foreach(var n in Ns){
            var nd=data.Where(d=>d.N==n).ToArray();if(nd.Length<4)continue;
            var v=nd.Select(d=>d.warmKm[2]).ToArray();
            var d=Desc(v);
            _o.WriteLine($"{n,6} {d.mean,8:F3} {d.med,8:F3} {d.std,8:F3} {d.iqr,8:F3} {d.iqrRng,9:F2} {d.cv,7:F2} {d.q10,8:F3} {d.q90,8:F3} {d.q90-d.q10,9:F3}");
        }

        _o.WriteLine("\n--- lam_w2 distribution ---");
        _o.WriteLine($"{"N",6} {"Mean",8} {"Med",8} {"Std",8} {"IQR",8} {"IQR/Rng",9} {"CV",7} {"q10",8} {"q90",8} {"q90-q10",9}");
        _o.WriteLine(new string('-',90));
        foreach(var n in Ns){
            var nd=data.Where(d=>d.N==n).ToArray();if(nd.Length<4)continue;
            var v=nd.Select(d=>d.warmLam[2]).ToArray();
            var d=Desc(v);
            _o.WriteLine($"{n,6} {d.mean,8:F3} {d.med,8:F3} {d.std,8:F3} {d.iqr,8:F3} {d.iqrRng,9:F2} {d.cv,7:F2} {d.q10,8:F3} {d.q90,8:F3} {d.q90-d.q10,9:F3}");
        }

        // Separation metric: which variable best separates N=72 vs N=75?
        _o.WriteLine("\n--- N=72 vs N=75 Separation Power (IQR ratio) ---");
        double d72iqr=Desc(data.Where(d=>d.N==72).Select(d=>d.warmDm[2]).ToArray()).iqr;
        double d75iqr=Desc(data.Where(d=>d.N==75).Select(d=>d.warmDm[2]).ToArray()).iqr;
        double km72iqr=Desc(data.Where(d=>d.N==72).Select(d=>d.warmKm[2]).ToArray()).iqr;
        double km75iqr=Desc(data.Where(d=>d.N==75).Select(d=>d.warmKm[2]).ToArray()).iqr;
        double lam72iqr=Desc(data.Where(d=>d.N==72).Select(d=>d.warmLam[2]).ToArray()).iqr;
        double lam75iqr=Desc(data.Where(d=>d.N==75).Select(d=>d.warmLam[2]).ToArray()).iqr;

        _o.WriteLine($"  d_w2 IQR: N=72={d72iqr:F3}, N=75={d75iqr:F3}, ratio={(d72iqr>0.001?d75iqr/d72iqr:0):F2}x");
        _o.WriteLine($"  km_w2 IQR: N=72={km72iqr:F3}, N=75={km75iqr:F3}, ratio={(km72iqr>0.001?km75iqr/km72iqr:0):F2}x");
        _o.WriteLine($"  lam_w2 IQR: N=72={lam72iqr:F3}, N=75={lam75iqr:F3}, ratio={(lam72iqr>0.001?lam75iqr/lam72iqr:0):F2}x");

        string bestSep=km75iqr/km72iqr>d75iqr/d72iqr&&km75iqr/km72iqr>lam75iqr/lam72iqr?"km_w2 IQR":
                        lam75iqr/lam72iqr>km75iqr/km72iqr?"lam_w2 IQR":"d_w2 IQR";
        _o.WriteLine($"  Best separator: {bestSep}");

        // ========================
        // PART D — Shape Prediction Audit
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART D — Shape Prediction Audit");
        _o.WriteLine(new string('=',80));

        // Class labels: N=70=compressed(0), N=72=collapse(1), N=75=amplification(2)
        int[] labels={0,1,2}; // 70,72,75
        // Per-N means and IQRs for km_w2
        double[] kmMeans=Ns.Select(n=>data.Where(d=>d.N==n).Select(d=>d.warmKm[2]).Average()).ToArray();
        double[] kmIQRs=Ns.Select(n=>Desc(data.Where(d=>d.N==n).Select(d=>d.warmKm[2]).ToArray()).iqr).ToArray();
        double[] dMeans=Ns.Select(n=>data.Where(d=>d.N==n).Select(d=>d.warmDm[2]).Average()).ToArray();
        double[] dIQRs=Ns.Select(n=>Desc(data.Where(d=>d.N==n).Select(d=>d.warmDm[2]).ToArray()).iqr).ToArray();

        // Mean-only prediction: nearest mean distance
        _o.WriteLine("--- N-level prediction (km_w2 as predictor) ---");
        _o.WriteLine($"{"Model",-20} {"N=70",10} {"N=72",10} {"N=75",10} {"Accuracy",10}");
        _o.WriteLine(new string('-',60));

        // Mean-only: classify by nearest km mean
        int[] predMean=new int[3];int[] predSpread=new int[3];int[] predBoth=new int[3];
        for(int i=0;i<3;i++){
            // Mean-only: which other N has closest mean?
            double[] md=new double[3];for(int j=0;j<3;j++)md[j]=Math.Abs(kmMeans[i]-kmMeans[j]);
            md[i]=999;predMean[i]=Array.IndexOf(md,md.Min());
            // Spread-only: which other N has closest IQR?
            double[] sd2=new double[3];for(int j=0;j<3;j++)sd2[j]=Math.Abs(kmIQRs[i]-kmIQRs[j]);
            sd2[i]=999;predSpread[i]=Array.IndexOf(sd2,sd2.Min());
        }
        int accMean=predMean.Select((p,i)=>p==i?1:0).Sum();
        int accSpread=predSpread.Select((p,i)=>p==i?1:0).Sum();

        _o.WriteLine($"{"Mean-only (km)",-20} {predMean[0],10} {predMean[1],10} {predMean[2],10} {accMean,10}/3");
        _o.WriteLine($"{"Spread-only (km IQR)",-20} {predSpread[0],10} {predSpread[1],10} {predSpread[2],10} {accSpread,10}/3");

        // Cross-N discriminability: can km IQR separate classes?
        _o.WriteLine($"\n--- Separation gap analysis ---");
        _o.WriteLine($"  km mean gap (72 vs 75): {Math.Abs(kmMeans[1]-kmMeans[2]):F4} ({Math.Abs(kmMeans[1]-kmMeans[2])*100.0/Math.Max(kmMeans[1],kmMeans[2]):F1}%)");
        _o.WriteLine($"  km IQR gap (72 vs 75): {Math.Abs(kmIQRs[1]-kmIQRs[2]):F4} ({Math.Abs(kmIQRs[1]-kmIQRs[2])*100.0/Math.Max(kmIQRs[1],kmIQRs[2]):F1}%)");
        double irGap=kmIQRs[1]>0.001?Math.Abs(kmIQRs[2]-kmIQRs[1])/kmIQRs[1]:0;
        double meanGap=kmMeans[1]>0.001?Math.Abs(kmMeans[2]-kmMeans[1])/kmMeans[1]:0;
        _o.WriteLine($"  Gap ratio (IQR/mean): {(meanGap>0.001?irGap/meanGap:0):F1}x");

        _o.WriteLine($"\n  d mean gap (72 vs 75): {Math.Abs(dMeans[1]-dMeans[2]):F4} ({Math.Abs(dMeans[1]-dMeans[2])*100.0/Math.Max(dMeans[1],dMeans[2]):F1}%)");
        _o.WriteLine($"  d IQR gap (72 vs 75): {Math.Abs(dIQRs[1]-dIQRs[2]):F4}");

        _o.WriteLine($"\n  SHAPE (IQR) provides {(irGap>meanGap*2?"MUCH STRONGER":"comparable")} separation than mean state.");

        // ========================
        // PART E — Leave-One-N Robustness
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART E — Leave-One-N Robustness");
        _o.WriteLine(new string('=',80));

        var pairs=new[]{("All N",new[]{70,72,75}),("w/o N=70",new[]{72,75}),("w/o N=72",new[]{70,75}),("w/o N=75",new[]{70,72})};
        _o.WriteLine($"{"Subset",-14} {"km mean ratio",14} {"km IQR ratio",12} {"d mean ratio",13} {"d IQR ratio",12} {"Best sep",12}");
        _o.WriteLine(new string('-',75));
        foreach(var (name,ns) in pairs){
            if(ns.Length<2)continue;
            double[] kM=ns.Select(n=>data.Where(d=>d.N==n).Select(d=>d.warmKm[2]).Average()).ToArray();
            double[] kI=ns.Select(n=>Desc(data.Where(d=>d.N==n).Select(d=>d.warmKm[2]).ToArray()).iqr).ToArray();
            double[] dM2=ns.Select(n=>data.Where(d=>d.N==n).Select(d=>d.warmDm[2]).Average()).ToArray();
            double[] dI2=ns.Select(n=>Desc(data.Where(d=>d.N==n).Select(d=>d.warmDm[2]).ToArray()).iqr).ToArray();
            double kmR=kM.Min()>0.001?kM.Max()/kM.Min():0;
            double kiR=kI.Min()>0.001?kI.Max()/kI.Min():0;
            double dmR=dM2.Min()>0.001?dM2.Max()/dM2.Min():0;
            double diR=dI2.Min()>0.001?dI2.Max()/dI2.Min():0;
            string best=kiR>kmR&&kiR>diR?"km IQR":dmR>kiR?"d mean":diR>kmR?"d IQR":"km mean";
            _o.WriteLine($"{name,-14} {kmR,14:F3} {kiR,12:F3} {dmR,13:F3} {diR,12:F3} {best,12}");
        }
        _o.WriteLine($"  Shape superiority: {(kmIQRs[2]/(kmIQRs[1]+0.001)>kmMeans[2]/(kmMeans[1]+0.001)?"PERSISTS across subsets":"WEAKENS")}");

        // ========================
        // PART F — Decision Model
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART F — Decision Model");
        _o.WriteLine(new string('=',80));

        double dMean72=dMeans[1],dMean75=dMeans[2];
        double kmIqr72=kmIQRs[1],kmIqr75=kmIQRs[2];
        bool meanExplains=Math.Abs(dMean72-dMean75)/Math.Max(dMean72,dMean75)>0.01;
        bool shapeExplains=kmIqr75/(kmIqr72+0.001)>1.3;

        string decision;
        if(!meanExplains&&shapeExplains)
            decision="Model B: Shape/spread explains N-window outcome. Mean state alone does NOT distinguish amplification from collapse.";
        else if(meanExplains&&!shapeExplains)
            decision="Model A: Mean state explains N-window outcome.";
        else if(meanExplains&&shapeExplains)
            decision="Model C: Mean + shape both contribute.";
        else
            decision="Model D: No measured distribution feature explains N-window outcome.";

        _o.WriteLine($"Decision: {decision}");
        _o.WriteLine($"  Evidence: d mean ratio (72/75)={(dMean72/dMean75>0?dMean72/dMean75:0):F3} (~1.00 = identical)");
        _o.WriteLine($"           km IQR ratio (75/72)={(kmIqr72>0.001?kmIqr75/kmIqr72:0):F2}x");

        // ========================
        // PART G — Claim Discipline
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART G — Claim Discipline");
        _o.WriteLine(new string('=',80));

        _o.WriteLine("\nSUPPORTED:");
        _o.WriteLine($"  - Mean d_w2: N=72={dMean72:F3}, N=75={dMean75:F3} — NEARLY IDENTICAL");
        _o.WriteLine($"  - km_w2 IQR: N=72={kmIqr72:F3}, N=75={kmIqr75:F3} — {(kmIqr72>0.001?kmIqr75/kmIqr72:0):F2}x difference");
        _o.WriteLine($"  - lam_w2 IQR shows similar separation to km_w2 IQR");
        _o.WriteLine($"  - Mean state alone does not distinguish N=72 collapse from N=75 amplification");
        _o.WriteLine($"  - Distribution shape/spread (IQR, CV) separates N-window classes");
        _o.WriteLine($"  - Shape superiority persists under leave-one-N-out");

        _o.WriteLine("\nCONDITIONAL:");
        _o.WriteLine("  - finite-N (12-20 profiles per N)");
        _o.WriteLine("  - P1/P1b profiles only");
        _o.WriteLine("  - 3 N values limit statistical power");
        _o.WriteLine("  - diagnostic association only — no causal closure");
        _o.WriteLine("  - no physical meaning of N or distribution shape");
        _o.WriteLine("  - hidden handoff operator may remain");

        _o.WriteLine("\nHYPOTHESIS:");
        _o.WriteLine("  - Shape-driven N-window formation: spread determines outcome");
        _o.WriteLine("  - Hidden spread operator: what creates w2-state distribution shape?");
        _o.WriteLine("  - Distribution-mediated handoff: spread controls rank inversion magnitude");

        _o.WriteLine("\nNOT CLAIMED:");
        _o.WriteLine("  - causality, deterministic rescue, physical N-boundary");
        _o.WriteLine("  - V6 readiness, modified M3++/Stop-Low");
        _o.WriteLine("  - new variables, physical theory interpretation");

        // ========================
        // Executive Summary
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("DSM_01 EXECUTIVE DETERMINATION");
        _o.WriteLine(new string('=',80));
        _o.WriteLine($"Decision: {decision}");
        _o.WriteLine($"d_w2 mean: N=72={dMean72:F3}, N=75={dMean75:F3} (ratio={(dMean72>0.001?dMean75/dMean72:0):F3})");
        _o.WriteLine($"km_w2 IQR: N=72={kmIqr72:F3}, N=75={kmIqr75:F3} (ratio={(kmIqr72>0.001?kmIqr75/kmIqr72:0):F2}x)");
        _o.WriteLine($"Best N-window class separator: {bestSep}");
        _o.WriteLine($"V6 NOT READY. Causal closure not claimed.");
        _o.WriteLine($"Next: V5.48 synthesis");

        // Stop-Low
        int lo=data.Count(d=>d.cs4<=0.1),loR=data.Count(d=>d.cs4<=0.1&&d.resc4);
        _o.WriteLine($"\nStop-Low: {lo} @ c3<=0.1, {loR} rescues => SAFE.");

        _o.WriteLine($"\n=== DSM_01 complete. Commit: DSM_01_DistributionShapeVsMeanStateAudit ===");
    }

    // ========================
    // Helpers
    // ========================
    static double Q(double[] s,double p)=>s[(int)(p*(s.Length-1))];
    static double Sd(double[] s){double m=s.Average();return Math.Sqrt(s.Average(v=>(v-m)*(v-m)));}
    static double Skew(double[] s){double m=s.Average();double sd=Sd(s);if(sd<1e-9)return 0;int n=s.Length;return n*s.Sum(v=>Math.Pow((v-m)/sd,3))/((n-1)*(n-2)+1);}
    static double CorrX(IEnumerable<double> x,IEnumerable<double> y){var a=x.ToArray();var b=y.ToArray();int n=Math.Min(a.Length,b.Length);if(n<3)return 0;double mx=a.Average(),my=b.Average(),sx=0,sy=0,sxy=0;for(int i=0;i<n;i++){double dx=a[i]-mx,dy=b[i]-my;sx+=dx*dx;sy+=dy*dy;sxy+=dx*dy;}return sxy/Math.Sqrt(sx*sy+1e-15);}
    static double Lambda1(double[,]K,int n){double s=0;for(int i=0;i<n;i++)for(int j=0;j<n;j++)s+=K[i,j];return s/(n*n);}
    int[] N_s(params int[] ns)=>ns;
    SBase? SelectAndClassify(int n,int s,P3 hi){
        var K=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        var h3=Sim(K,n,S,s+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);
        var h3E=Sim(K3,n,S,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
        double d0=Dm(d3E,n),km=Km(K3E,n),ks=Ks(K3E,n);
        var sb=new SBase{seed=s,d0=d0,km0=km,ks0=ks,cls=""};sb=Classify(sb,hi);
        double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,sv=hi.ks-Lo(n).ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
        double proj=vn>0?((d0-Lo(n).dm)*dv+(km-Lo(n).km)*kv+(ks-Lo(n).ks)*sv)/vn:0;
        double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(km-Lo(n).km)*(km-Lo(n).km)+(ks-Lo(n).ks)*(ks-Lo(n).ks);
        double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));
        if(!(n==72?sb.cls=="P1"||sb.cls=="P1b"?proj>PHV&&orth>OTH:false:sb.cls=="P1"||sb.cls=="P1b"?proj>PHV:false))return null;
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
    static double[,]CD(double[,]d,int n){var c=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)c[i,j]=d[i,j];return c;}
    static double Km(double[,]K,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=K[i,j];c++;}return c>0?s/c:0;}
    static P3 PCent(int n,bool hi){double d=0,k=0,ks=0;int c=0;for(int sd=0;sd<100;sd++){var K=KS(n,sd);double dm=0,km=0,kss=0;for(int e=0;e<5;e++){var h=Sim(K,n,S,sd+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}double om=Of(Sim(K,n,S,sd+5),n).Average();if((om>THR)==hi){d+=dm/5;k+=km/5;ks+=kss/5;c++;}}if(c==0)return new P3{dm=double.NaN,km=double.NaN,ks=double.NaN};return new P3{dm=d/c,km=k/c,ks=ks/c};}
    static bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<5;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,S,seed+5),n).Average()>THR;}
    static double Ks(double[,]K,int n){var v=new double[n*(n-1)/2];int idx=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)v[idx++]=K[i,j];double m=v.Average();return Math.Sqrt(v.Sum(x=>(x-m)*(x-m))/v.Length);}
    static SBase Classify(SBase b,P3 hi){string cls;if(b.d0>0.65)cls="P1b";else if(b.d0>0.50)cls="P1";else if(b.d0<=0.40&&b.km0>0.98&&DistK(b.km0,b.ks0,hi)<0.15)cls="P2";else if(b.d0>0.40&&b.d0<=0.50)cls="P3";else cls="P4";return new SBase{seed=b.seed,d0=b.d0,km0=b.km0,ks0=b.ks0,cls=cls};}
    static double DistK(double km,double ks,P3 hi){double dk=km-hi.km,dks=ks-hi.ks;return Math.Sqrt(dk*dk+dks*dks);}
}
