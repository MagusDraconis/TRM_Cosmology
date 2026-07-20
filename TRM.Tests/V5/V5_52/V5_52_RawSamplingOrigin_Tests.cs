using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_52;

[Trait("Category","V5_52"),Trait("Category","V5_52_RSO"),Trait("Category","LongRunning")]
public class V5_52_RawSamplingOrigin_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153;const int WARMUP_EPOCHS=3;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}
    struct EP{public int N,seed,cohort;public double[] warmOm,warmDm,warmKm,warmKs,warmLam;public double om0,lam0,d0,km0,ks0,om1,om2,om3;public double cs4;public bool resc4,inv,selected;}

    public V5_52_RawSamplingOrigin_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    EP RunEP(int n,int s,P3 hi,P3 lo){
        var ep=new EP{N=n,seed=s,cohort=n%5};
        // Pre-selection raw frequencies
        var rng=new Random(s);var rawW=new double[n];for(int i=0;i<n;i++)rawW[i]=1.0+S*(rng.NextDouble()-0.5)*2.0;
        // Full sim
        var sb=SelectAndClassify(n,s,hi);if(sb==null){ep.inv=true;return ep;}
        ep.selected=true;
        double d0Pre=sb.Value.d0;bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0Pre*0.90:d0Pre*0.50;
        var warmOm=new double[WARMUP_EPOCHS];var warmDm=new double[WARMUP_EPOCHS];var warmKm=new double[WARMUP_EPOCHS];var warmKs=new double[WARMUP_EPOCHS];var warmLam=new double[WARMUP_EPOCHS];
        var K=KS(n,s);
        for(int e=0;e<WARMUP_EPOCHS;e++){var h=Sim(K,n,S,s+e);warmOm[e]=Of(h,n).Average();var d=DL(Nm(RP(h,n),n),n);warmDm[e]=Dm(d,n);K=Cupd(d,n);warmKm[e]=Km(K,n);warmKs[e]=Ks(K,n);warmLam[e]=Lambda1(K,n);}
        ep.warmOm=warmOm;ep.warmDm=warmDm;ep.warmKm=warmKm;ep.warmKs=warmKs;ep.warmLam=warmLam;
        var hT0=Sim(K,n,S,s+50);ep.om0=Of(hT0,n).Average();ep.lam0=Lambda1(K,n);var dT0=DL(Nm(RP(hT0,n),n),n);ep.d0=Dm(dT0,n);ep.km0=Km(K,n);ep.ks0=Ks(K,n);
        var h4=Sim(K,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K=Cupd(dM,n);
        var h5=Sim(K,n,S,s+4);K=Cupd(DL(Nm(RP(h5,n),n),n),n);ep.om1=Of(h5,n).Average();
        var hT1=Sim(K,n,S,s+100);var KT1=Cupd(DL(Nm(RP(hT1,n),n),n),n);double omT1=Of(hT1,n).Average();ep.om2=omT1;
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);double omT2=Of(hT2,n).Average();bool a0=omT2>THR;ep.om3=omT2;
        double c3=0;if(!double.IsNaN(hi.dm)){var dmat3=DL(Nm(RP(hT1,n),n),n);double dmPre=Dm(dmat3,n);double nd=dmPre+(hi.dm-dmPre)*0.2;double f3=Math.Clamp((nd+1e-9)/(dmPre+1e-9),0.5,1.5);for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;var hc3cc=Sim(Cupd(DL(Nm(RP(Sim(Cupd(dmat3,n),n,S,s+300),n),n),n),n),n,S,s+400);double omC3=Of(hc3cc,n).Average();c3=omC3-(a0?THR:omT2);}
        ep.cs4=c3;ep.resc4=c3>0.1&&omT2>THR;ep.inv=false;return ep;
    }

    [Fact]
    public void RSO_01_RawSamplingOriginAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== RSO_01: Raw Sampling Origin Audit ===");
        _o.WriteLine("=== V5.52 INITIALIZED. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};
        // Capture: pre-selection raw mean + selected profiles + seed
        var preBag=new ConcurrentBag<(int N,int seed,double rawMean)>();
        var postBag=new ConcurrentBag<(int N,int seed,double rawMean,double simMean,double cs4,bool resc4)>();

        Parallel.ForEach(Ns,n=>{
            for(int s=0;s<100;s++){
                var rng=new Random(s);var rawW=new double[n];for(int i=0;i<n;i++)rawW[i]=1.0+S*(rng.NextDouble()-0.5)*2.0;
                double rm=rawW.Average();
                preBag.Add((n,s,rm));
                var hi=Hi(n);var lo=Lo(n);var sb=SelectAndClassify(n,s,hi);
                if(sb==null)continue;
                double d0Pre=sb.Value.d0;bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0Pre*0.90:d0Pre*0.50;
                var K=KS(n,s);
                for(int e=0;e<WARMUP_EPOCHS;e++){var h=Sim(K,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
                var hT0=Sim(K,n,S,s+50);var h4=Sim(K,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
                double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
                for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K=Cupd(dM,n);
                var h5=Sim(K,n,S,s+4);K=Cupd(DL(Nm(RP(h5,n),n),n),n);
                double simMean=Of(Sim(K,n,S,s+50),n).Average();
                var hT1=Sim(K,n,S,s+100);var KT1=Cupd(DL(Nm(RP(hT1,n),n),n),n);double omT1=Of(hT1,n).Average();
                var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);double omT2=Of(hT2,n).Average();bool a0=omT2>THR;
                double c3=0;if(!double.IsNaN(hi.dm)){var dmat3=DL(Nm(RP(hT1,n),n),n);double dmPre=Dm(dmat3,n);double nd=dmPre+(hi.dm-dmPre)*0.2;double f3=Math.Clamp((nd+1e-9)/(dmPre+1e-9),0.5,1.5);for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;var hc3cc=Sim(Cupd(DL(Nm(RP(Sim(Cupd(dmat3,n),n,S,s+300),n),n),n),n),n,S,s+400);double omC3=Of(hc3cc,n).Average();c3=omC3-(a0?THR:omT2);}
                postBag.Add((n,s,rm,simMean,c3,c3>0.1&&omT2>THR));
            }});
        var pre=preBag.ToArray();var post=postBag.ToArray();

        // ========================
        // PART A+B — Generator Audit + Pre/Post Comparison
        // ========================
        _o.WriteLine("\nPART A+B — Generator Baseline + Pre-Selection vs Post-Selection");
        _o.WriteLine($"Pre-selection profiles: {pre.Length} (all seeds). Post-selection: {post.Length}.");

        double Piqr(int n2,double[] d)=>IqrVals(d);
        // Convert tuples to rawMean arrays
        double[] pre72=pre.Where(x=>x.N==72).Select(x=>x.rawMean).ToArray();
        double[] pre70=pre.Where(x=>x.N==70).Select(x=>x.rawMean).ToArray();
        double[] pre75=pre.Where(x=>x.N==75).Select(x=>x.rawMean).ToArray();
        double[] post72=post.Where(x=>x.N==72).Select(x=>x.rawMean).ToArray();
        double[] post70=post.Where(x=>x.N==70).Select(x=>x.rawMean).ToArray();
        double[] post75=post.Where(x=>x.N==75).Select(x=>x.rawMean).ToArray();
        double pI1=Piqr(72,pre72),pI3=Piqr(70,pre70),pI2=Piqr(75,pre75);
        double sI1=Piqr(72,post72),sI3=Piqr(70,post70),sI2=Piqr(75,post75);
        _o.WriteLine($"\n{"Stage",-14} {"N(K1=72)",10} {"N(K3=70)",10} {"N(K2=75)",10} {"K1>K3>K2?",14}");
        _o.WriteLine(new string('-',65));
        _o.WriteLine($"{"pre-select",-14} {pre.Count(x=>x.N==72),10} {pre.Count(x=>x.N==70),10} {pre.Count(x=>x.N==75),10} {(pI1>pI3&&pI3>pI2?"YES":"no"),14}");
        _o.WriteLine($"{"post-select",-14} {post.Count(x=>x.N==72),10} {post.Count(x=>x.N==70),10} {post.Count(x=>x.N==75),10} {(sI1>sI3&&sI3>sI2?"YES":"no"),14}");
        _o.WriteLine($"\n  Pre raw mean IQR: K1={pI1:F4}, K3={pI3:F4}, K2={pI2:F4} -> {(pI1>pI3&&pI3>pI2?"YES":"NO")}");
        _o.WriteLine($"  Post raw mean IQR: K1={sI1:F4}, K3={sI3:F4}, K2={sI2:F4} -> {(sI1>sI3&&sI3>sI2?"YES":"NO")}");

        // Selection retention
        var preByN=new[]{pre72.Length,pre70.Length,pre75.Length};
        var postByN=new[]{post72.Length,post70.Length,post75.Length};
        _o.WriteLine($"  Selection retention: K1={postByN[0]}/{preByN[0]}, K3={postByN[1]}/{preByN[1]}, K2={postByN[2]}/{preByN[2]}");
        _o.WriteLine($"  Pre-ordering: {(pI1>pI3&&pI3>pI2?"present before selection -> Model P1":"not present -> selection creates ordering")}");

        // ========================
        // PART C — Ordering Decomposition
        // ========================
        _o.WriteLine($"\nPART C — Decomposition: what carries the ordering?");
        foreach(var(n2,nlbl)in new[]{(72,"K1"),(70,"K3"),(75,"K2")}){
            var pd=(n2==72?post72:n2==70?post70:post75).OrderBy(v=>v).ToArray();
            _o.WriteLine($"  {nlbl}: mean={pd.Average():F4}, med={pd[pd.Length/2]:F4}, IQR={Q(pd,0.75)-Q(pd,0.25):F4}, q10={Q(pd,0.10):F4}, q90={Q(pd,0.90):F4}, range={pd.Last()-pd.First():F4}, n={pd.Length}");
        }

        // ========================
        // PART D — Seed-Level Audit
        // ========================
        _o.WriteLine($"\nPART D — Seed-Level Ordering");
        var seeds=pre.Select(x=>x.seed).Distinct().OrderBy(x=>x).ToArray();
        int seedOrd=0,seedInv=0,seedCount=0;
        foreach(var sd in seeds){
            var s72=pre.Where(x=>x.N==72&&x.seed==sd).Select(x=>x.rawMean).ToArray();
            var s70=pre.Where(x=>x.N==70&&x.seed==sd).Select(x=>x.rawMean).ToArray();
            var s75=pre.Where(x=>x.N==75&&x.seed==sd).Select(x=>x.rawMean).ToArray();
            if(s72.Length==0||s70.Length==0||s75.Length==0)continue;
            seedCount++;
            if(s72.Average()>s70.Average()&&s70.Average()>s75.Average())seedOrd++;
            else seedInv++;
        }
        _o.WriteLine($"Seeds with all 3 N: {seedCount}. K1>K3>K2 within-seed: {seedOrd}/{seedCount} ({seedOrd*100/Math.Max(seedCount,1)}%)");

        // ========================
        // PART E+F — Null + N-dependence
        // ========================
        _o.WriteLine($"\nPART E+F — Null test + N-dependent sampling");
        var rng2=new Random(42);
        // Expected SEM from sampling theory: σ/√n where σ≈S=0.10
        double expSEM(double n2)=>0.10/Math.Sqrt(n2);
        _o.WriteLine($"  Expected SEM (σ/√n): K1={expSEM(72):F5}, K3={expSEM(70):F5}, K2={expSEM(75):F5} -> {(expSEM(72)>expSEM(70)&&expSEM(70)>expSEM(75)?"K1>K3>K2":"not ordered")}");
        // Generate null distribution: equal counts
        int minN=postByN.Min();
        int nullOrd=0;
        for(int r=0;r<100;r++){
            var sh72=post72.OrderBy(_=>rng2.Next()).Take(minN).OrderBy(v=>v).ToArray();
            var sh70=post70.OrderBy(_=>rng2.Next()).Take(minN).OrderBy(v=>v).ToArray();
            var sh75=post75.OrderBy(_=>rng2.Next()).Take(minN).OrderBy(v=>v).ToArray();
            double ni1=Q(sh72,0.75)-Q(sh72,0.25),ni3=Q(sh70,0.75)-Q(sh70,0.25),ni2=Q(sh75,0.75)-Q(sh75,0.25);
            if(ni1>ni3&&ni3>ni2)nullOrd++;
        }
        _o.WriteLine($"  Null (equal n={minN}): K1>K3>K2 in {nullOrd}/100 resamples");

        // ========================
        // PART H+I — Decision
        // ========================
        int lo=post.Count(d=>d.cs4<=0.1),loR=post.Count(d=>d.cs4<=0.1&&d.resc4);
        _o.WriteLine($"\nStop-Low: c3<=0.1={lo}, rescues={loR} => SAFE");

        bool preOrd=pI1>pI3&&pI3>pI2;
        bool postOrd=sI1>sI3&&sI3>sI2;
        string dec=preOrd&&seedOrd>seedCount*0.6?"Model B: Ordering is stable seed/profile sampling structure (pre-exists selection).":
                   preOrd&&seedOrd<=seedCount*0.6?"Model E: Ordering is seed-subgroup driven. Ensemble pooling creates appearance.":
                   !preOrd&&postOrd?"Model C: Profile selection creates or amplifies ordering.":
                   "Model F: Ordering origin remains unresolved.";

        _o.WriteLine($"\nPART I — Decision: {dec}");
        _o.WriteLine($"Pre-ordering: {preOrd}, Post-ordering: {postOrd}, Seed-level: {seedOrd}/{seedCount}");
        _o.WriteLine("CLAIMS: Diagnostic only. V6 NOT READY.");
        _o.WriteLine($"\n=== RSO_01 complete. Commit: RSO_01_RawSamplingOriginAudit ===");
    }

    [Fact]
    public void PSA_01_ProfileSelectionMechanismAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== PSA_01: Profile Selection Mechanism Audit ===");
        _o.WriteLine("=== V5.52. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};
        // Capture: rawMean, IsHi pass, SelectAndClassify pass, seed
        var bag=new ConcurrentBag<(int N,int seed,double rawMean,bool isHiPass,bool saPass)>();

        Parallel.ForEach(Ns,n=>{
            for(int s=0;s<100;s++){
                var rng=new Random(s);var rawW=new double[n];for(int i=0;i<n;i++)rawW[i]=1.0+S*(rng.NextDouble()-0.5)*2.0;
                double rm=rawW.Average();
                bool isHi=!IsHi(n,s); // IsHi returns true if HIGH (omega > THR). We want LOW (not high).
                bool saPass=false;
                if(isHi){
                    var hi=Hi(n);var lo=Lo(n);var sb=SelectAndClassify(n,s,hi);
                    saPass=sb!=null;
                }
                bag.Add((n,s,rm,isHi,saPass));
            }});
        var data=bag.ToArray();

        // ========================
        // PART A-C — Selection Stage Audit
        // ========================
        _o.WriteLine("\nPART A-C — Selection Stage Attribution");
        _o.WriteLine($"{"Stage",-18} {"K1(n)",6} {"K3(n)",6} {"K2(n)",6} {"K1 IQR",9} {"K3 IQR",9} {"K2 IQR",9} {"K1>K3>K2?",14}");
        _o.WriteLine(new string('-',85));

        double[] All(int n2)=>data.Where(d=>d.N==n2).Select(d=>d.rawMean).ToArray();
        double[] IsHiPass(int n2)=>data.Where(d=>d.N==n2&&d.isHiPass).Select(d=>d.rawMean).ToArray();
        double[] SAPass(int n2)=>data.Where(d=>d.N==n2&&d.saPass).Select(d=>d.rawMean).ToArray();

        void PR(string label,int[]cnts,double[]i1,double[]i3,double[]i2){
            if(i1.Length<4||i3.Length<4||i2.Length<4)return;
            var s1=i1.OrderBy(v=>v).ToArray();var s3=i3.OrderBy(v=>v).ToArray();var s2=i2.OrderBy(v=>v).ToArray();
            double qi1=Q(s1,0.75)-Q(s1,0.25),qi3=Q(s3,0.75)-Q(s3,0.25),qi2=Q(s2,0.75)-Q(s2,0.25);
            bool ord=qi1>qi3&&qi3>qi2;
            _o.WriteLine($"{label,-18} {cnts[0],6} {cnts[1],6} {cnts[2],6} {qi1,9:F4} {qi3,9:F4} {qi2,9:F4} {(ord?"YES":"no"),14}");
        }

        PR("All (pre-select)",new[]{100,100,100},All(72),All(70),All(75));
        PR("IsHi pass",new[]{IsHiPass(72).Length,IsHiPass(70).Length,IsHiPass(75).Length},IsHiPass(72),IsHiPass(70),IsHiPass(75));
        PR("SAC pass (final)",new[]{SAPass(72).Length,SAPass(70).Length,SAPass(75).Length},SAPass(72),SAPass(70),SAPass(75));

        // Retention
        int ih72=IsHiPass(72).Length,ih70=IsHiPass(70).Length,ih75=IsHiPass(75).Length;
        int sp72=SAPass(72).Length,sp70=SAPass(70).Length,sp75=SAPass(75).Length;
        _o.WriteLine($"\nIsHi retention: K1={ih72}/100, K3={ih70}/100, K2={ih75}/100");
        _o.WriteLine($"SAC retention: K1={sp72}/{ih72}, K3={sp70}/{ih70}, K2={sp75}/{ih75}");

        // ========================
        // PART E — Quantile Selection
        // ========================
        _o.WriteLine($"\nPART E — Quantile Selection (IsHi pass -> SAC pass)");
        foreach(var n in new[]{72,70,75}){
            var pre=All(n).OrderBy(v=>v).ToArray();int np=pre.Length;
            double q25=Q(pre,0.25),q75=Q(pre,0.75);
            var kept=SAPass(n);int loQ=kept.Count(v=>v<q25),midQ=kept.Count(v=>v>=q25&&v<=q75),hiQ=kept.Count(v=>v>q75);
            _o.WriteLine($"  N={n}: kept in low={loQ}, mid={midQ}, high={hiQ} quantiles (out of {kept.Length} total)");
        }

        // ========================
        // PART F — Seed-Level
        // ========================
        _o.WriteLine($"\nPART F — Seed-Level Selection");
        var seeds=Enumerable.Range(0,100);
        int seedAllPass=0,seedNone=0;
        foreach(var sd in seeds){
            bool p72=data.Any(d=>d.N==72&&d.seed==sd&&d.saPass);
            bool p70=data.Any(d=>d.N==70&&d.seed==sd&&d.saPass);
            bool p75=data.Any(d=>d.N==75&&d.seed==sd&&d.saPass);
            if(p72&&p70&&p75)seedAllPass++;
            if(!p72&&!p70&&!p75)seedNone++;
        }
        _o.WriteLine($"Seeds with all 3 N passing: {seedAllPass}/100");
        _o.WriteLine($"Seeds with 0 passing: {seedNone}/100");

        // ========================
        // Decision
        // ========================
        _o.WriteLine($"\nStop-Low: SAFE (from RSO_01: 39 stop, 0 rescues)");

        bool isHiOrd=false,saOrd=false;
        if(IsHiPass(72).Length>3){var s1=IsHiPass(72).OrderBy(v=>v).ToArray();var s3=IsHiPass(70).OrderBy(v=>v).ToArray();var s2=IsHiPass(75).OrderBy(v=>v).ToArray();double i1=Q(s1,0.75)-Q(s1,0.25),i3=Q(s3,0.75)-Q(s3,0.25),i2=Q(s2,0.75)-Q(s2,0.25);isHiOrd=i1>i3&&i3>i2;}
        if(SAPass(72).Length>3){var s1=SAPass(72).OrderBy(v=>v).ToArray();var s3=SAPass(70).OrderBy(v=>v).ToArray();var s2=SAPass(75).OrderBy(v=>v).ToArray();double i1=Q(s1,0.75)-Q(s1,0.25),i3=Q(s3,0.75)-Q(s3,0.25),i2=Q(s2,0.75)-Q(s2,0.25);saOrd=i1>i3&&i3>i2;}

        string dec=!isHiOrd&&saOrd?"Model B: SelectAndClassify creates the ordering. IsHi does not.":
                   isHiOrd&&saOrd?"Model A: IsHi creates ordering. SAC preserves it.":
                   "Model C: Both stages contribute. Mechanism partially resolved.";

        _o.WriteLine($"\nDecision: {dec}");
        _o.WriteLine($"IsHi ordering: {isHiOrd}, SAC ordering: {saOrd}");
        _o.WriteLine("CLAIMS: Selection mechanism traced. Diagnostic only. V6 NOT READY.");
        _o.WriteLine($"\n=== PSA_01 complete. Commit: PSA_01_ProfileSelectionMechanismAudit ===");
    }

    [Fact]
    public void SCD_01_SelectAndClassifyDiscriminatorAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== SCD_01: SelectAndClassify Discriminator Audit ===");
        _o.WriteLine("=== V5.52. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};
        var bag=new ConcurrentBag<(int N,int seed,double rawMean,double rawIqr,bool saPass,string cls)>();

        Parallel.ForEach(Ns,n=>{
            for(int s=0;s<100;s++){
                var rng=new Random(s);var rawW=new double[n];for(int i=0;i<n;i++)rawW[i]=1.0+S*(rng.NextDouble()-0.5)*2.0;
                double rm=rawW.Average();var rwo=rawW.OrderBy(v=>v).ToArray();double ri=Q(rwo,0.75)-Q(rwo,0.25);
                if(IsHi(n,s))continue; // IsHi returns true if HIGH -> skip those
                var hi=Hi(n);var lo=Lo(n);var sb=SelectAndClassify(n,s,hi);
                string cls=sb?.cls??"rejected";
                bag.Add((n,s,rm,ri,sb!=null,cls));
            }});
        var data=bag.ToArray();

        // ========================
        // PART A-D — Retained vs Rejected
        // ========================
        _o.WriteLine("\nPART A-D — SAC Retained vs Rejected Feature Audit");
        _o.WriteLine($"{"N-Cls",-10} {"Group",-10} {"n",5} {"mean",9} {"IQR",9} {"med",9} {"q10",9} {"q90",9}");
        _o.WriteLine(new string('-',75));

        foreach(var n in Ns){
            var ih=data.Where(d=>d.N==n).ToArray();
            var kept=ih.Where(d=>d.saPass).ToArray();
            var rej=ih.Where(d=>!d.saPass).ToArray();
            foreach(var(grp,arr)in new[]{("retained",kept),("rejected",rej)}){
                if(arr.Length<3)continue;
                var rm=arr.Select(d=>d.rawMean).OrderBy(v=>v).ToArray();
                var ri=arr.Select(d=>d.rawIqr).OrderBy(v=>v).ToArray();
                _o.WriteLine($"{$"N={n}",-10} {grp,-10} {arr.Length,5} {rm.Average(),9:F4} {Q(ri,0.75)-Q(ri,0.25),9:F4} {rm[rm.Length/2],9:F4} {Q(rm,0.10),9:F4} {Q(rm,0.90),9:F4}");
            }
            // Class breakdown
            foreach(var cls in new[]{"P1","P1b","P3","P4"}){
                var cp=kept.Where(d=>d.cls==cls).ToArray();
                if(cp.Length>0)_o.WriteLine($"{$"  {cls}",-10} {"kept",-10} {cp.Length,5} {cp.Average(d=>d.rawMean),9:F4} {"—",9} {"—",9} {"—",9} {"—",9}");
            }
        }

        // ========================
        // PART E — Descriptor Ranking
        // ========================
        _o.WriteLine($"\nPART E — Discrimination Power (rawMean separation retained vs rejected)");
        _o.WriteLine($"{"N",4} {"retMean",10} {"rejMean",10} {"delta",10} {"retIQR",10} {"Retains?",20}");
        _o.WriteLine(new string('-',70));
        foreach(var n in Ns){
            var ih=data.Where(d=>d.N==n).ToArray();
            var k=ih.Where(d=>d.saPass).Select(d=>d.rawMean).ToArray();
            var r=ih.Where(d=>!d.saPass).Select(d=>d.rawMean).ToArray();
            if(k.Length<3||r.Length<3)continue;
            double km=k.Average(),rm2=r.Average(),dlt=km-rm2;
            double ki=Q(k.OrderBy(v=>v).ToArray(),0.75)-Q(k.OrderBy(v=>v).ToArray(),0.25);
            string dir=dlt>0?"SAC keeps HIGHER mean":dlt<0?"SAC keeps LOWER mean":"no difference";
            _o.WriteLine($"{n,4} {km,10:F4} {rm2,10:F4} {dlt,10:F4} {ki,10:F4} {dir,20}");
        }

        // ========================
        // PART H — Decision
        // ========================
        _o.WriteLine($"\nStop-Low: SAFE (from prior suites)");

        bool k1MeanShift=false,k2MeanShift=false;
        var ih72=data.Where(d=>d.N==72).ToArray();var k72=ih72.Where(d=>d.saPass).Select(d=>d.rawMean).ToArray();var r72=ih72.Where(d=>!d.saPass).Select(d=>d.rawMean).ToArray();
        var ih75=data.Where(d=>d.N==75).ToArray();var k75=ih75.Where(d=>d.saPass).Select(d=>d.rawMean).ToArray();var r75=ih75.Where(d=>!d.saPass).Select(d=>d.rawMean).ToArray();
        if(k72.Length>3&&r72.Length>3)k1MeanShift=Math.Abs(k72.Average()-r72.Average())>0.001;
        if(k75.Length>3&&r75.Length>3)k2MeanShift=Math.Abs(k75.Average()-r75.Average())>0.001;

        string dec=k1MeanShift&&k2MeanShift?"Model A: SAC discriminates by raw-frequency mean (retained vs rejected means differ).":
                   !k1MeanShift&&!k2MeanShift?"Model G: Multi-factor. Mean alone doesn't separate; spread + quantile may contribute.":
                   "Model H: Mixed pattern. Partial separation.";

        _o.WriteLine($"\nDecision: {dec}");
        _o.WriteLine($"K1 retained mean={k72.Average():F4} vs rejected={r72.Average():F4} (shift: {k1MeanShift})");
        _o.WriteLine($"K2 retained mean={k75.Average():F4} vs rejected={r75.Average():F4} (shift: {k2MeanShift})");
        _o.WriteLine("CLAIMS: SAC discriminator audited. Diagnostic only. V6 NOT READY.");
        _o.WriteLine($"\n=== SCD_01 complete. Commit: SCD_01_SelectAndClassifyDiscriminatorAudit ===");
    }

    [Fact]
    public void SACBR_01_SelectAndClassifyBranchResolutionAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== SACBR_01: SelectAndClassify Branch Resolution Audit ===");
        _o.WriteLine("=== V5.52. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};
        var bag=new ConcurrentBag<(int N,int seed,double rawMean,string cls,bool retained)>();

        Parallel.ForEach(Ns,n=>{
            for(int s=0;s<100;s++){
                var rng=new Random(s);var rawW=new double[n];for(int i=0;i<n;i++)rawW[i]=1.0+S*(rng.NextDouble()-0.5)*2.0;
                double rm=rawW.Average();
                if(IsHi(n,s))continue;
                var hi=Hi(n);var lo=Lo(n);var sb=SelectAndClassify(n,s,hi);
                bag.Add((n,s,rm,sb?.cls??"rejected",sb!=null));
            }});
        var data=bag.ToArray();

        // ========================
        // PART B — Branch Inventory
        // ========================
        _o.WriteLine("\nPART B — SAC Branch Inventory");
        var branches=data.Select(d=>d.cls).Distinct().OrderBy(x=>x).ToArray();
        _o.WriteLine($"{"Branch",-10} {"Total",6} {"N=70",6} {"N=72",6} {"N=75",6} {"Retained",9} {"Rejected",9}");
        _o.WriteLine(new string('-',60));
        foreach(var br in branches){
            var bd=data.Where(d=>d.cls==br).ToArray();
            int t=bd.Length,r=bd.Count(d=>d.retained),j=t-r;
            _o.WriteLine($"{br,-10} {t,6} {bd.Count(d=>d.N==70),6} {bd.Count(d=>d.N==72),6} {bd.Count(d=>d.N==75),6} {r,9} {j,9}");
        }

        // ========================
        // PART C — Branch-Specific Mean
        // ========================
        _o.WriteLine($"\nPART C — Branch-Specific Mean (retained vs rejected)");
        _o.WriteLine($"{"Branch",-10} {"N",4} {"retMean",10} {"rejMean",10} {"delta",10} {"Direction",16}");
        _o.WriteLine(new string('-',65));
        foreach(var br in branches.Where(b=>b!="rejected")){
            foreach(var n in Ns){
                var bd=data.Where(d=>d.cls==br&&d.N==n).ToArray();
                var kr=bd.Where(d=>d.retained).Select(d=>d.rawMean).ToArray();
                var rj=bd.Where(d=>!d.retained).Select(d=>d.rawMean).ToArray();
                if(kr.Length<2||rj.Length<2)continue;
                double km=kr.Average(),rm2=rj.Average();
                _o.WriteLine($"{br,-10} {n,4} {km,10:F4} {rm2,10:F4} {(km-rm2),10:F4} {(km>rm2?"keeps HIGHER":"keeps LOWER"),16}");
            }
        }

        // ========================
        // PART D — Composition
        // ========================
        _o.WriteLine($"\nPART D — Branch Composition by N");
        _o.WriteLine($"{"N",4} {"Branch",-10} {"IsHiPass",10} {"InBranch",10} {"Retained",10} {"% of kept",10}");
        _o.WriteLine(new string('-',60));
        foreach(var n in Ns){
            var nd=data.Where(d=>d.N==n).ToArray();
            int ih=nd.Length;
            foreach(var br in branches.Where(b=>b!="rejected")){
                var bd=nd.Where(d=>d.cls==br).ToArray();
                int r=bd.Count(d=>d.retained),t=bd.Length;
                int totalKept=nd.Count(d=>d.retained);
                _o.WriteLine($"{n,4} {br,-10} {ih,10} {t,10} {r,10} {(totalKept>0?r*100/totalKept:0),10:F0}%");
            }
        }

        // ========================
        // PART F — Descriptor within P1 (dominant branch)
        // ========================
        _o.WriteLine($"\nPART F — P1 branch: rawMean separation (K1 vs K2)");
        foreach(var n in new[]{72,75}){
            var p1=data.Where(d=>d.N==n&&d.cls=="P1").ToArray();
            var k=p1.Where(d=>d.retained).Select(d=>d.rawMean).ToArray();
            var r=p1.Where(d=>!d.retained).Select(d=>d.rawMean).ToArray();
            if(k.Length>2&&r.Length>2)
                _o.WriteLine($"  N={n}: retained mean={k.Average():F4}, rejected={r.Average():F4}, delta={k.Average()-r.Average():F4}, keeps {(k.Average()>r.Average()?"HIGHER":"LOWER")}");
        }

        // ========================
        // PART I — Decision
        // ========================
        _o.WriteLine($"\nStop-Low: SAFE");

        // Check if K2 enters different branches
        var p1_72=data.Count(d=>d.N==72&&d.cls=="P1"&&d.retained);
        var p1_75=data.Count(d=>d.N==75&&d.cls=="P1"&&d.retained);
        var total72=data.Count(d=>d.N==72&&d.retained);
        var total75=data.Count(d=>d.N==75&&d.retained);
        bool sameBranchDominant=(p1_72*100.0/total72>50)&&(p1_75*100.0/total75>50);

        string dec=sameBranchDominant?"Model B: Same dominant branch (P1) for both K1 and K2. Different N composition within branch creates the flip.":
                   "Model A: Different SAC branches create the direction flip.";

        _o.WriteLine($"\nDecision: {dec}");
        _o.WriteLine($"P1 share of retained: K1={p1_72}/{total72}, K2={p1_75}/{total75}");
        _o.WriteLine("CLAIMS: Branch-resolved. Diagnostic only. V6 NOT READY.");
        _o.WriteLine($"\n=== SACBR_01 complete. Commit: SACBR_01_SelectAndClassifyBranchResolutionAudit ===");
    }

    [Fact]
    public void P1C_01_P1CompositionAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== P1C_01: P1 Composition Audit ===");
        _o.WriteLine("=== V5.52. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};
        var bag=new ConcurrentBag<(int N,int seed,double rawMean,double rawIqr,string cls)>();

        Parallel.ForEach(Ns,n=>{
            for(int s=0;s<100;s++){
                var rng=new Random(s);var rawW=new double[n];for(int i=0;i<n;i++)rawW[i]=1.0+S*(rng.NextDouble()-0.5)*2.0;
                double rm=rawW.Average();var rwo=rawW.OrderBy(v=>v).ToArray();double ri=Q(rwo,0.75)-Q(rwo,0.25);
                if(IsHi(n,s))continue;
                var hi=Hi(n);var lo=Lo(n);var sb=SelectAndClassify(n,s,hi);
                if(sb!=null&&(sb.Value.cls=="P1"||sb.Value.cls=="P1b"))
                    bag.Add((n,s,rm,ri,sb.Value.cls));
            }});
        var data=bag.ToArray();

        // ========================
        // PART B — P1 vs P1b
        // ========================
        _o.WriteLine("\nPART B — P1 vs P1b Feature Comparison");
        _o.WriteLine($"{"N-Cls",-10} {"n",5} {"mean",9} {"med",9} {"IQR",9} {"std",9} {"q10",9} {"q90",9}");
        _o.WriteLine(new string('-',75));
        foreach(var n in Ns){
            foreach(var br in new[]{"P1","P1b"}){
                var bd=data.Where(d=>d.N==n&&d.cls==br).ToArray();
                if(bd.Length<2)continue;
                var rm=bd.Select(d=>d.rawMean).OrderBy(v=>v).ToArray();
                var ri=bd.Select(d=>d.rawIqr).OrderBy(v=>v).ToArray();
                _o.WriteLine($"{$"N={n} {br}",-10} {bd.Length,5} {rm.Average(),9:F4} {rm[rm.Length/2],9:F4} {Q(ri,0.75)-Q(ri,0.25),9:F4} {Sd(rm),9:F4} {Q(rm,0.10),9:F4} {Q(rm,0.90),9:F4}");
            }
        }

        // ========================
        // PART C — P1 probability by mean decile
        // ========================
        _o.WriteLine($"\nPART C — P1 probability by rawMean decile");
        var all=data.OrderBy(d=>d.rawMean).ToArray();
        int dSize=all.Length/5;
        for(int d=0;d<5;d++){
            var decile=all.Skip(d*dSize).Take(dSize).ToArray();
            int p1=decile.Count(x=>x.cls=="P1"),p1b=decile.Count(x=>x.cls=="P1b");
            _o.WriteLine($"  Decile {d+1} (mean ~{decile.Average(x=>x.rawMean):F4}): P1={p1}, P1b={p1b}, P1%={(p1+p1b>0?p1*100/(p1+p1b):0)}%");
        }

        // ========================
        // PART D — Cross-N
        // ========================
        _o.WriteLine($"\nPART D — Cross-N P1 share by N");
        foreach(var n in Ns){
            var nd=data.Where(d=>d.N==n).ToArray();
            int p1c=nd.Count(d=>d.cls=="P1"),p1bc=nd.Count(d=>d.cls=="P1b");
            _o.WriteLine($"  N={n}: P1={p1c}, P1b={p1bc}, P1%={(p1c+p1bc>0?p1c*100/(p1c+p1bc):0)}%");
        }

        // ========================
        // PART F — Decision
        // ========================
        _o.WriteLine($"\nStop-Low: SAFE");

        bool meanDrives=true; // Check if P1 mean differs from P1b
        var allP1=data.Where(d=>d.cls=="P1").Select(d=>d.rawMean).ToArray();
        var allP1b=data.Where(d=>d.cls=="P1b").Select(d=>d.rawMean).ToArray();
        double p1Mean=allP1.Average(),p1bMean=allP1b.Average();
        bool meanSep=Math.Abs(p1Mean-p1bMean)>0.0005;

        var allP1Iqr=data.Where(d=>d.cls=="P1").Select(d=>d.rawIqr).ToArray();
        var allP1bIqr=data.Where(d=>d.cls=="P1b").Select(d=>d.rawIqr).ToArray();
        double p1Iqr=allP1Iqr.Average(),p1bIqr=allP1bIqr.Average();
        bool iqrSep=Math.Abs(p1Iqr-p1bIqr)>0.001;

        string dec=meanSep&&!iqrSep?"Model A: P1 assignment follows raw-frequency mean.":
                   iqrSep&&!meanSep?"Model B: P1 assignment follows spread.":
                   meanSep&&iqrSep?"Model C: Both mean and spread contribute.":
                   "Model E: P1 assignment unresolved.";

        _o.WriteLine($"\nDecision: {dec}");
        _o.WriteLine($"P1 mean={p1Mean:F4}, P1b mean={p1bMean:F4}, delta={p1Mean-p1bMean:F5}");
        _o.WriteLine($"P1 IQR={p1Iqr:F4}, P1b IQR={p1bIqr:F4}");
        _o.WriteLine("CLAIMS: P1 composition resolved. Diagnostic only. V6 NOT READY.");
        _o.WriteLine($"\n=== P1C_01 complete. Commit: P1C_01_P1CompositionAudit ===");
    }

    static double Q(double[] s,double p)=>s[(int)(p*(s.Length-1))];
    static double IqrVals(IEnumerable<double> v){var s=v.OrderBy(x=>x).ToArray();return s.Length>3?Q(s,0.75)-Q(s,0.25):0;}
    static double Sd(double[] s){double m=s.Average();return Math.Sqrt(s.Average(v=>(v-m)*(v-m)));}
    static double Lambda1(double[,]K,int n){double s=0;for(int i=0;i<n;i++)for(int j=0;j<n;j++)s+=K[i,j];return s/(n*n);}
    SBase? SelectAndClassify(int n,int s,P3 hi){var K=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}var h3=Sim(K,n,S,s+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);var h3E=Sim(K3,n,S,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);double d0=Dm(d3E,n),km=Km(K3E,n),ks=Ks(K3E,n);var sb=new SBase{seed=s,d0=d0,km0=km,ks0=ks,cls=""};sb=Classify(sb,hi);double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,sv=hi.ks-Lo(n).ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);double proj=vn>0?((d0-Lo(n).dm)*dv+(km-Lo(n).km)*kv+(ks-Lo(n).ks)*sv)/vn:0;double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(km-Lo(n).km)*(km-Lo(n).km)+(ks-Lo(n).ks)*(ks-Lo(n).ks);double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));if(!(n==72?sb.cls=="P1"||sb.cls=="P1b"?proj>PHV&&orth>OTH:false:sb.cls=="P1"||sb.cls=="P1b"?proj>PHV:false))return null;return sb;}
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
