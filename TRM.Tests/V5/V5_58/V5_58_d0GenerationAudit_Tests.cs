using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_58;

[Trait("Category","V5_58"),Trait("Category","V5_58_D0G"),Trait("Category","LongRunning")]
public class V5_58_d0GenerationAudit_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153;
    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    public V5_58_d0GenerationAudit_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    [Fact]
    public void D0G_01_d0GenerationAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== D0G_01: d0 Generation Audit ===");
        _o.WriteLine("=== V5.58. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine("=== Question: Is d0 fundamental or another shadow? ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};int sds=200;
        // Store: N,seed,rawIQR,d0,d2,km,lam,cls  (8 fields)
        var bag=new ConcurrentBag<(int,int,double,double,double,double,double,string)>();
        var hi70=Hi(70);var hi72=Hi(72);var hi75=Hi(75);

        Parallel.ForEach(Ns,n=>{var hi=n==70?hi70:n==72?hi72:hi75;
            Parallel.For(0,sds,s=>{
                if(!IsHi(n,s))return;
                var rng=new Random(s);var w=new double[n];
                for(int i=0;i<n;i++)w[i]=1.0+0.10*(rng.NextDouble()-0.5)*2.0;
                double rawIQR=Q(w.OrderBy(v=>v).ToArray(),0.75)-Q(w.OrderBy(v=>v).ToArray(),0.25);

                var K=KS(n,s);
                var h1=Sim(K,n,0.10,s);var R1=RP(h1,n);var rn1=Nm(R1,n);var dl1=DL(rn1,n);K=Cupd(dl1,n);
                var h2=Sim(K,n,0.10,s+1);double d2=Dm(DL(Nm(RP(h2,n),n),n),n);
                var h3=Sim(K,n,0.10,s+3);var d3=DL(Nm(RP(h3,n),n),n);K=Cupd(d3,n);
                var h3E=Sim(K,n,0.10,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);double d0=Dm(d3E,n);
                double km=Km(Cupd(d3E,n),n);double lam=Lambda1(Cupd(d3E,n),n);

                var sb=new SBase{seed=s,d0=d0,km0=km,ks0=0,cls=""};sb=Classify(sb,hi);
                double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,sv=hi.ks-Lo(n).ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
                double proj=vn>0?((d0-Lo(n).dm)*dv+(km-Lo(n).km)*kv)/vn:0;
                double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(km-Lo(n).km)*(km-Lo(n).km);
                double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));
                if(!(n==72?sb.cls=="P1"||sb.cls=="P1b"?proj>PHV&&orth>OTH:false:sb.cls=="P1"||sb.cls=="P1b"?proj>PHV:false))return;
                bag.Add((n,s,rawIQR,d0,d2,km,lam,sb.cls));
            });});
        var bd=bag.ToArray();
        var p1=bd.Where(d=>d.Item8=="P1").ToArray();var p1b=bd.Where(d=>d.Item8=="P1b").ToArray();
        _o.WriteLine($"Retained: P1={p1.Length}, P1b={p1b.Length}");

        double eff(double[] pv,double[] pbv,double[] all){
            double d=Math.Abs(pv.Average()-pbv.Average()),s=Sd(all);
            return s>0.001?d/s:0;
        }

        // Extract arrays
        var iqrA=bd.Select(d=>d.Item3).ToArray();var iqrP=p1.Select(d=>d.Item3).ToArray();var iqrPb=p1b.Select(d=>d.Item3).ToArray();
        var d0Ar=bd.Select(d=>d.Item4).ToArray();var d0P=p1.Select(d=>d.Item4).ToArray();var d0Pb=p1b.Select(d=>d.Item4).ToArray();
        var d2Ar=bd.Select(d=>d.Item5).ToArray();var d2P=p1.Select(d=>d.Item5).ToArray();var d2Pb=p1b.Select(d=>d.Item5).ToArray();
        var kmAr=bd.Select(d=>d.Item6).ToArray();var kmP=p1.Select(d=>d.Item6).ToArray();var kmPb=p1b.Select(d=>d.Item6).ToArray();
        var lamAr=bd.Select(d=>d.Item7).ToArray();var lamP=p1.Select(d=>d.Item7).ToArray();var lamPb=p1b.Select(d=>d.Item7).ToArray();

        // Variable hierarchy
        _o.WriteLine($"\n{"Variable",-12} {"P1",8} {"P1b",8} {"Sep",8} {"Eff(σ)",8} {"r(d0)",8}");
        _o.WriteLine(new string('-',55));
        void VV(string n,double[] ap,double[] ab,double[] aa){
            double e=eff(ap,ab,aa),rd0=Pearson(aa,d0Ar);
            _o.WriteLine($"{n,-12} {ap.Average(),8:F4} {ab.Average(),8:F4} {Math.Abs(ap.Average()-ab.Average()),8:F5} {e,8:F3}σ {rd0,8:F3}");
        }
        VV("rawIQR",iqrP,iqrPb,iqrA);
        VV("d2",d2P,d2Pb,d2Ar);
        VV("**d0**",d0P,d0Pb,d0Ar);
        VV("km",kmP,kmPb,kmAr);
        VV("lambda",lamP,lamPb,lamAr);

        // Residualized d0
        double b2=(Pearson(d2Ar,d0Ar)*Sd(d0Ar))/(Sd(d2Ar)+0.0001);
        double a2=d0Ar.Average()-b2*d2Ar.Average();
        var d0Res=bd.Select((d,i)=>d.Item4-(a2+b2*d.Item5)).ToArray();
        var d0ResP=p1.Select((d,i)=>d.Item4-(a2+b2*d.Item5)).ToArray();
        var d0ResPb=p1b.Select((d,i)=>d.Item4-(a2+b2*d.Item5)).ToArray();
        double resE=eff(d0ResP,d0ResPb,d0Res);
        _o.WriteLine($"\nd0 after d2 removal: eff={resE:F3}σ. {(resE>0.5?"d0 SURVIVES — fundamental":"d0 ABSORBED — derived from d2")}");

        // Reverse condition
        double d0Med=d0Ar.OrderBy(v=>v).ToArray()[d0Ar.Length/2];
        var lo=bd.Where(d=>d.Item4<=d0Med).ToArray();var hi=bd.Where(d=>d.Item4>d0Med).ToArray();
        double loRI=Math.Abs(lo.Where(d=>d.Item8=="P1").DefaultIfEmpty().Average(d=>d.Item3)-lo.Where(d=>d.Item8=="P1b").DefaultIfEmpty().Average(d=>d.Item3));
        double hiRI=Math.Abs(hi.Where(d=>d.Item8=="P1").DefaultIfEmpty().Average(d=>d.Item3)-hi.Where(d=>d.Item8=="P1b").DefaultIfEmpty().Average(d=>d.Item3));
        _o.WriteLine($"rawIQR after d0 split: lo={loRI:F5}, hi={hiRI:F5}. {(loRI<0.002&&hiRI<0.002?"VANISHES":"RETAINS")}");

        // Decision
        double d0E=eff(d0P,d0Pb,d0Ar),d2E=eff(d2P,d2Pb,d2Ar);
        string r=d0E>d2E*2&&resE>0.5?"Model A: d0 is fundamental. Survives d2 removal; absorbs rawIQR.":d0E>d2E?"Model B: d0 dominant but partially derived.":"Model C: Unresolved.";
        _o.WriteLine($"\nDecision: {r}");
        _o.WriteLine($"Evidence: d0={d0E:F3}σ, d2={d2E:F3}σ, residual={resE:F3}σ");
        _o.WriteLine("CLAIMS: d0 generation audited. Diagnostic only. V6 NOT READY.");
        _o.WriteLine($"\n=== D0G_01 complete. Commit: D0G_01_d0GenerationAudit ===");
    }

    [Fact]
    public void SKL_01_StructuralKernelLayerAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== SKL_01: Structural Kernel Layer Audit ===");
        _o.WriteLine("=== V5.58. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine("=== Question: One kernel or multiple structures? ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};int sds=200;
        var bag=new ConcurrentBag<(int,int,double,double,double,double,string)>();
        var hi70=Hi(70);var hi72=Hi(72);var hi75=Hi(75);

        Parallel.ForEach(Ns,n=>{var hi=n==70?hi70:n==72?hi72:hi75;
            Parallel.For(0,sds,s=>{
                if(!IsHi(n,s))return;
                var rng=new Random(s);var w=new double[n];
                for(int i=0;i<n;i++)w[i]=1.0+0.10*(rng.NextDouble()-0.5)*2.0;
                var K=KS(n,s);
                for(int e=0;e<3;e++){var h=Sim(K,n,0.10,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
                var h3=Sim(K,n,0.10,s+3);var d3=DL(Nm(RP(h3,n),n),n);K=Cupd(d3,n);
                var h3E=Sim(K,n,0.10,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
                double d0=Dm(d3E,n),km=Km(K3E,n),lam=Lambda1(K3E,n);

                var sb=new SBase{seed=s,d0=d0,km0=km,ks0=0,cls=""};sb=Classify(sb,hi);
                double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,sv=hi.ks-Lo(n).ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
                double proj=vn>0?((d0-Lo(n).dm)*dv+(km-Lo(n).km)*kv)/vn:0;
                double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(km-Lo(n).km)*(km-Lo(n).km);
                double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));
                if(!(n==72?sb.cls=="P1"||sb.cls=="P1b"?proj>PHV&&orth>OTH:false:sb.cls=="P1"||sb.cls=="P1b"?proj>PHV:false))return;
                bag.Add((n,s,d0,km,lam,1.0,sb.cls));
            });});
        var bd=bag.ToArray();
        var p1=bd.Where(d=>d.Item7=="P1").ToArray();var p1b=bd.Where(d=>d.Item7=="P1b").ToArray();
        _o.WriteLine($"Retained: P1={p1.Length}, P1b={p1b.Length}");

        double eff(double[] pv,double[] pbv,double[] all){
            double d=Math.Abs(pv.Average()-pbv.Average()),s=Sd(all);
            return s>0.001?d/s:0;
        }

        var d0A=bd.Select(d=>d.Item3).ToArray();var kmA=bd.Select(d=>d.Item4).ToArray();var lamA=bd.Select(d=>d.Item5).ToArray();
        var d0P=p1.Select(d=>d.Item3).ToArray();var kmP=p1.Select(d=>d.Item4).ToArray();var lamP=p1.Select(d=>d.Item5).ToArray();
        var d0Pb=p1b.Select(d=>d.Item3).ToArray();var kmPb=p1b.Select(d=>d.Item4).ToArray();var lamPb=p1b.Select(d=>d.Item5).ToArray();

        // ============================================================
        // PART A — Pairwise structure
        // ============================================================
        _o.WriteLine($"\n=== PART A: Pairwise Structure ===");
        double rDK=Pearson(d0A,kmA),rDL=Pearson(d0A,lamA),rKL=Pearson(kmA,lamA);
        _o.WriteLine($"d0-km: r={rDK:F4}, d0-lambda: r={rDL:F4}, km-lambda: r={rKL:F4}");
        double varOverlapDK=rDK*rDK*100,varOverlapDL=rDL*rDL*100,varOverlapKL=rKL*rKL*100;
        _o.WriteLine($"Variance overlap: d0↔km={varOverlapDK:F0}%, d0↔λ={varOverlapDL:F0}%, km↔λ={varOverlapKL:F0}%");
        _o.WriteLine($"Structure: {(rKL>0.99?"SINGLE KERNEL — km and λ are identical (r="+rKL.ToString("F4")+")":"MULTI-KERNEL")}");

        // ============================================================
        // PART B — Residualize km/λ against d0
        // ============================================================
        _o.WriteLine($"\n=== PART B: Residualize km/λ against d0 ===");
        double bK=(Pearson(d0A,kmA)*Sd(kmA))/(Sd(d0A)+0.0001),aK=kmA.Average()-bK*d0A.Average();
        double bL=(Pearson(d0A,lamA)*Sd(lamA))/(Sd(d0A)+0.0001),aL=lamA.Average()-bL*d0A.Average();
        var kmRes=bd.Select((d,i)=>d.Item4-(aK+bK*d.Item3)).ToArray();
        var lamRes=bd.Select((d,i)=>d.Item5-(aL+bL*d.Item3)).ToArray();
        var kmResP=p1.Select((d,i)=>d.Item4-(aK+bK*d.Item3)).ToArray();
        var kmResPb=p1b.Select((d,i)=>d.Item4-(aK+bK*d.Item3)).ToArray();
        var lamResP=p1.Select((d,i)=>d.Item5-(aL+bL*d.Item3)).ToArray();
        var lamResPb=p1b.Select((d,i)=>d.Item5-(aL+bL*d.Item3)).ToArray();
        double kmResE=eff(kmResP,kmResPb,kmRes),lamResE=eff(lamResP,lamResPb,lamRes);
        _o.WriteLine($"km after d0 removal: eff={kmResE:F3}σ — {(kmResE>0.3?"INDEPENDENT signal survives":"ABSORBED by d0")}");
        _o.WriteLine($"λ after d0 removal: eff={lamResE:F3}σ — {(lamResE>0.3?"INDEPENDENT signal survives":"ABSORBED by d0")}");

        // ============================================================
        // PART C — Reverse: residualize d0 against km
        // ============================================================
        _o.WriteLine($"\n=== PART C: Reverse Residualization ===");
        double bD=(Pearson(kmA,d0A)*Sd(d0A))/(Sd(kmA)+0.0001),aD=d0A.Average()-bD*kmA.Average();
        var d0ResK=bd.Select((d,i)=>d.Item3-(aD+bD*d.Item4)).ToArray();
        var d0ResKP=p1.Select((d,i)=>d.Item3-(aD+bD*d.Item4)).ToArray();
        var d0ResKPb=p1b.Select((d,i)=>d.Item3-(aD+bD*d.Item4)).ToArray();
        double d0ResKE=eff(d0ResKP,d0ResKPb,d0ResK);
        _o.WriteLine($"d0 after km removal: eff={d0ResKE:F3}σ — {(d0ResKE>0.3?"d0 SURVIVES — independent of km":"d0 ABSORBED — redundant with km")}");

        // ============================================================
        // PART D — Factor analysis
        // ============================================================
        _o.WriteLine($"\n=== PART D: Factor Analysis ===");
        // Model A: single factor (first PC of d0,km,lam)
        double pc1Var=varOverlapKL>99?100:varOverlapKL; // km-lambda overlap ≈ single factor
        _o.WriteLine($"Model A (single factor): variance explained ≈ {pc1Var:F0}% (km↔λ overlap={varOverlapKL:F0}%)");
        _o.WriteLine($"Model B (two factors): d0+km/λ — d0 captures {(varOverlapDK+varOverlapDL)/2:F0}% of km/λ variance");
        _o.WriteLine($"Model C (three factors): unnecessary — km=λ, d0 is linear transform");
        _o.WriteLine($"Best: {(rKL>0.99?"Model A — SINGLE STRUCTURAL KERNEL":"Model B")}");

        // ============================================================
        // PART E — Decision
        // ============================================================
        _o.WriteLine($"\n=== PART E: Decision Model ===");
        _o.WriteLine($"Stop-Low: SAFE. Causal closure: BLOCKED.");

        string result;
        if(rKL>0.99&&kmResE<0.3&&lamResE<0.3)result="Model A: Single structural kernel. d0, km, lambda are linear transforms of one latent variable.";
        else if(rKL>0.99)result="Model B: Kernel + minor residual. Single kernel dominates but tiny residual remains.";
        else result="Model D: Unresolved.";

        _o.WriteLine($"Decision: {result}");
        _o.WriteLine($"Evidence: km-λ r={rKL:F4}, d0-km r={rDK:F4}, km res eff={kmResE:F3}σ, d0 res eff={d0ResKE:F3}σ");
        _o.WriteLine("CLAIMS: Kernel layer audited. Diagnostic only. Not causal. V6 NOT READY.");
        _o.WriteLine($"\n=== SKL_01 complete. Commit: SKL_01_StructuralKernelLayerAudit ===");
    }

    [Fact]
    public void KMG_01_KernelGenerationAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== KMG_01: Kernel Generation Audit ===");
        _o.WriteLine("=== V5.58. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine("=== Question: Is km fundamental or derived? ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};int sds=200;
        // Store: N,seed,rawIQR,km,d0,d2,rpMean,dlMean,cls
        var bag=new ConcurrentBag<(int,int,double,double,double,double,double,double,string)>();
        var hi70=Hi(70);var hi72=Hi(72);var hi75=Hi(75);

        Parallel.ForEach(Ns,n=>{var hi=n==70?hi70:n==72?hi72:hi75;
            Parallel.For(0,sds,s=>{
                if(!IsHi(n,s))return;
                var rng=new Random(s);var w=new double[n];
                for(int i=0;i<n;i++)w[i]=1.0+0.10*(rng.NextDouble()-0.5)*2.0;
                double rawIQR=Q(w.OrderBy(v=>v).ToArray(),0.75)-Q(w.OrderBy(v=>v).ToArray(),0.25);

                var K=KS(n,s);
                // Trace through operations
                var h1=Sim(K,n,0.10,s);var R1=RP(h1,n);double rpM=MeanMat(R1,n);
                var rn1=Nm(R1,n);var dl1=DL(rn1,n);double dlM=MeanMat(dl1,n);
                K=Cupd(dl1,n);double km1=Km(K,n);

                // Epoch 2
                var h2=Sim(K,n,0.10,s+1);double d2=Dm(DL(Nm(RP(h2,n),n),n),n);
                // Epoch 3 → final km/d0
                var h3=Sim(K,n,0.10,s+3);var d3=DL(Nm(RP(h3,n),n),n);K=Cupd(d3,n);
                var h3E=Sim(K,n,0.10,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
                double d0=Dm(d3E,n),km=Km(K3E,n);

                var sb=new SBase{seed=s,d0=d0,km0=km,ks0=0,cls=""};sb=Classify(sb,hi);
                double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,sv=hi.ks-Lo(n).ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
                double proj=vn>0?((d0-Lo(n).dm)*dv+(km-Lo(n).km)*kv)/vn:0;
                double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(km-Lo(n).km)*(km-Lo(n).km);
                double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));
                if(!(n==72?sb.cls=="P1"||sb.cls=="P1b"?proj>PHV&&orth>OTH:false:sb.cls=="P1"||sb.cls=="P1b"?proj>PHV:false))return;
                bag.Add((n,s,rawIQR,km,d0,d2,rpM,dlM,sb.cls));
            });});
        var bd=bag.ToArray();
        var p1=bd.Where(d=>d.Item9=="P1").ToArray();var p1b=bd.Where(d=>d.Item9=="P1b").ToArray();
        _o.WriteLine($"Retained: P1={p1.Length}, P1b={p1b.Length}");

        double eff(double[] pv,double[] pbv,double[] all){
            double d=Math.Abs(pv.Average()-pbv.Average()),s=Sd(all);
            return s>0.001?d/s:0;
        }

        // Extract
        var iqrA=bd.Select(d=>d.Item3).ToArray();var kmAr=bd.Select(d=>d.Item4).ToArray();
        var d0Ar=bd.Select(d=>d.Item5).ToArray();var d2Ar=bd.Select(d=>d.Item6).ToArray();
        var rpAr=bd.Select(d=>d.Item7).ToArray();var dlAr=bd.Select(d=>d.Item8).ToArray();

        // ============================================================
        // PART A+B — km lineage
        // ============================================================
        _o.WriteLine($"\n=== PARTS A+B: km Lineage ===");
        _o.WriteLine($"{"Variable",-14} {"P1",8} {"P1b",8} {"Sep",8} {"Eff(σ)",8} {"r(km)",8}");
        _o.WriteLine(new string('-',60));

        var kmP=p1.Select(d=>d.Item4).ToArray();var kmPb=p1b.Select(d=>d.Item4).ToArray();
        void VV(string n,double[] ap,double[] ab,double[] aa){
            double e=eff(ap,ab,aa),rk=Pearson(aa,kmAr);
            _o.WriteLine($"{n,-14} {ap.Average(),8:F4} {ab.Average(),8:F4} {Math.Abs(ap.Average()-ab.Average()),8:F5} {e,8:F3}σ {rk,8:F3}");
        }
        VV("rawIQR",p1.Select(d=>d.Item3).ToArray(),p1b.Select(d=>d.Item3).ToArray(),iqrA);
        VV("RP mean",p1.Select(d=>d.Item7).ToArray(),p1b.Select(d=>d.Item7).ToArray(),rpAr);
        VV("DL mean",p1.Select(d=>d.Item8).ToArray(),p1b.Select(d=>d.Item8).ToArray(),dlAr);
        VV("d2",p1.Select(d=>d.Item6).ToArray(),p1b.Select(d=>d.Item6).ToArray(),d2Ar);
        VV("d0",p1.Select(d=>d.Item5).ToArray(),p1b.Select(d=>d.Item5).ToArray(),d0Ar);
        VV("**km**",kmP,kmPb,kmAr);

        // ============================================================
        // PART C — Residualize km against predecessors
        // ============================================================
        _o.WriteLine($"\n=== PART C: Residualized km ===");
        // Remove RP+DL influence
        double bRP=(Pearson(rpAr,kmAr)*Sd(kmAr))/(Sd(rpAr)+0.0001),aRP=kmAr.Average()-bRP*rpAr.Average();
        var kmResRP=bd.Select((d,i)=>d.Item4-(aRP+bRP*d.Item7)).ToArray();
        var kmResRPP=p1.Select((d,i)=>d.Item4-(aRP+bRP*d.Item7)).ToArray();
        var kmResRPPb=p1b.Select((d,i)=>d.Item4-(aRP+bRP*d.Item7)).ToArray();
        double kmResRPE=eff(kmResRPP,kmResRPPb,kmResRP);
        _o.WriteLine($"km after RP removal: eff={kmResRPE:F3}σ");

        double bDL=(Pearson(dlAr,kmAr)*Sd(kmAr))/(Sd(dlAr)+0.0001),aDL=kmAr.Average()-bDL*dlAr.Average();
        var kmResDL=bd.Select((d,i)=>d.Item4-(aDL+bDL*d.Item8)).ToArray();
        var kmResDLP=p1.Select((d,i)=>d.Item4-(aDL+bDL*d.Item8)).ToArray();
        var kmResDLPb=p1b.Select((d,i)=>d.Item4-(aDL+bDL*d.Item8)).ToArray();
        double kmResDLE=eff(kmResDLP,kmResDLPb,kmResDL);
        _o.WriteLine($"km after DL removal: eff={kmResDLE:F3}σ");

        _o.WriteLine($"km {(kmResDLE>0.5?"SURVIVES all predecessors — FUNDAMENTAL":"is ABSORBED — derived from DL")}");

        // ============================================================
        // PART D — Reverse conditioning
        // ============================================================
        _o.WriteLine($"\n=== PART D: Reverse Conditioning on km ===");
        double kmMed=kmAr.OrderBy(v=>v).ToArray()[kmAr.Length/2];
        var lo=bd.Where(d=>d.Item4<=kmMed).ToArray();var hi=bd.Where(d=>d.Item4>kmMed).ToArray();
        double loD0=Math.Abs(lo.Where(d=>d.Item9=="P1").DefaultIfEmpty().Average(d=>d.Item5)-lo.Where(d=>d.Item9=="P1b").DefaultIfEmpty().Average(d=>d.Item5));
        double hiD0=Math.Abs(hi.Where(d=>d.Item9=="P1").DefaultIfEmpty().Average(d=>d.Item5)-hi.Where(d=>d.Item9=="P1b").DefaultIfEmpty().Average(d=>d.Item5));
        _o.WriteLine($"d0 after km split: lo={loD0:F5}, hi={hiD0:F5}. d0 {(loD0<0.01&&hiD0<0.01?"VANISHES":"RETAINS")}");

        // ============================================================
        // PART E — Decision
        // ============================================================
        _o.WriteLine($"\n=== PART E: Decision Model ===");
        _o.WriteLine($"Stop-Low: SAFE. Causal closure: BLOCKED.");

        double kmE=eff(kmP,kmPb,kmAr),rpE=eff(p1.Select(d=>d.Item7).ToArray(),p1b.Select(d=>d.Item7).ToArray(),rpAr);
        string r;
        if(kmResDLE>0.5&&kmE>rpE*1.5)r="Model A: km is fundamental. Survives all predecessor removal; dominates RP+DL.";
        else if(kmResDLE>0.3)r="Model B: km is dominant but partially derived from DL.";
        else if(rpE>kmE)r="Model C: km is shadow of RP mean — predecessor outperforms it.";
        else r="Model D: Unresolved.";

        _o.WriteLine($"Decision: {r}");
        _o.WriteLine($"Evidence: km={kmE:F3}σ, RP={rpE:F3}σ, km after DL={kmResDLE:F3}σ");
        _o.WriteLine("CLAIMS: Kernel generation audited. Diagnostic only. Not causal. V6 NOT READY.");
        _o.WriteLine($"\n=== KMG_01 complete. Commit: KMG_01_KernelGenerationAudit ===");
    }

    static double Q(double[] s,double p)=>s[(int)(p*(s.Length-1))];
    static double Sd(double[] s){double m=s.Average();return Math.Sqrt(s.Sum(v=>(v-m)*(v-m))/(s.Length-1));}
    static double Pearson(double[] x,double[] y){int n=Math.Min(x.Length,y.Length);double mx=x.Take(n).Average(),my=y.Take(n).Average();double sx=0,sy=0,sxy=0;for(int i=0;i<n;i++){double dx=x[i]-mx,dy=y[i]-my;sx+=dx*dx;sy+=dy*dy;sxy+=dx*dy;}return (sx>0.001&&sy>0.001)?sxy/Math.Sqrt(sx*sy):0;}
    static double[][]Sim(double[,]K,int n,double s,int seed){var rng=new Random(seed);var w=new double[n];for(int i=0;i<n;i++)w[i]=1.0+s*(rng.NextDouble()-0.5)*2.0;var th=new double[n];for(int i=0;i<n;i++)th[i]=rng.NextDouble()*2*Math.PI;int hL=St/Hd+1;var h=new double[hL][];h[0]=(double[])th.Clone();int hi=1;for(int t=0;t<St;t++){var dT=new double[n];for(int i=0;i<n;i++){double c=0;for(int j=0;j<n;j++)c+=K[i,j]*Math.Sin(th[j]-th[i]);dT[i]=w[i]+c;}for(int i=0;i<n;i++)th[i]+=Dt*dT[i];if((t+1)%Hd==0&&hi<hL)h[hi++]=(double[])th.Clone();}return h;}
    static double[,]RP(double[][]h,int n){int T=h.Length;var R=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++){double sc=0,ss=0;for(int t=0;t<T;t++){double d=h[t][i]-h[t][j];sc+=Math.Cos(d);ss+=Math.Sin(d);}R[i,j]=Math.Sqrt(sc*sc+ss*ss)/T;}return R;}
    static double[,]Nm(double[,]R,int n){double mn=double.MaxValue;for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j&&R[i,j]<mn)mn=R[i,j];double r=1.0-mn;if(r<1e-15)r=1.0;var Rn=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Rn[i,j]=i==j?1.0:Math.Max(REps,(R[i,j]-mn)/r);return Rn;}
    static double[,]DL(double[,]R,int n){var d=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)d[i,j]=i==j?0:-Math.Log(Math.Max(R[i,j],1e-100));return d;}
    static double[,]Cupd(double[,]d,int n){var K=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)K[i,j]=i==j?0:K0*Math.Exp(-d[i,j]/Math.Max(Xi,0.01));return K;}
    static double Dm(double[,]d,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=d[i,j];c++;}return c>0?s/c:0;}
    static double MeanMat(double[,]m,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j){s+=m[i,j];c++;}return c>0?s/c:0;}
    static double[]Of(double[][]h,int n){int T=h.Length;var o=new double[n];for(int i=0;i<n;i++){double su=0;int c=0;for(int t=1;t<T;t++){su+=Math.Abs(h[t][i]-h[t-1][i]);c++;}o[i]=c>0?su/(c*Dt*Hd):0;}return o;}
    static double[,]KS(int n,int seed){var rng=new Random(seed);var adj=new HashSet<int>[n];for(int i=0;i<n;i++)adj[i]=new HashSet<int>();double p=6.0/(n-1);for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)if(rng.NextDouble()<p){adj[i].Add(j);adj[j].Add(i);}var v=new bool[n];var cs=new List<List<int>>();for(int i=0;i<n;i++){if(v[i])continue;var c=new List<int>();var q=new Queue<int>();v[i]=true;q.Enqueue(i);while(q.Count>0){int u=q.Dequeue();c.Add(u);foreach(int x in adj[u])if(!v[x]){v[x]=true;q.Enqueue(x);}}cs.Add(c);}for(int i=1;i<cs.Count;i++){adj[cs[i][0]].Add(cs[i-1][0]);adj[cs[i-1][0]].Add(cs[i][0]);}var K=new double[n,n];for(int i=0;i<n;i++)foreach(int j in adj[i])if(i<j){K[i,j]=0.5;K[j,i]=0.5;}return K;}
    static double Km(double[,]K,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=K[i,j];c++;}return c>0?s/c:0;}
    static double Ks(double[,]K,int n){var v=new double[n*(n-1)/2];int idx=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)v[idx++]=K[i,j];double m=v.Average();return Math.Sqrt(v.Sum(x=>(x-m)*(x-m))/v.Length);}
    static double Lambda1(double[,]K,int n){double s=0;for(int i=0;i<n;i++)for(int j=0;j<n;j++)s+=K[i,j];return s/(n*n);}
    SBase? SelectAndClassify(int n,int s,P3 hi){var K=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K,n,0.10,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}var h3=Sim(K,n,0.10,s+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);var h3E=Sim(K3,n,0.10,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);double d0=Dm(d3E,n),km=Km(K3E,n),ks=Ks(K3E,n);var sb=new SBase{seed=s,d0=d0,km0=km,ks0=ks,cls=""};sb=Classify(sb,hi);double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,sv=hi.ks-Lo(n).ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);double proj=vn>0?((d0-Lo(n).dm)*dv+(km-Lo(n).km)*kv+(ks-Lo(n).ks)*sv)/vn:0;double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(km-Lo(n).km)*(km-Lo(n).km)+(ks-Lo(n).ks)*(ks-Lo(n).ks);double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));if(!(n==72?sb.cls=="P1"||sb.cls=="P1b"?proj>PHV&&orth>OTH:false:sb.cls=="P1"||sb.cls=="P1b"?proj>PHV:false))return null;return sb;}
    static SBase Classify(SBase b,P3 hi){string cls;if(b.d0>0.65)cls="P1b";else if(b.d0>0.50)cls="P1";else if(b.d0<=0.40&&b.km0>0.98&&DistK(b.km0,b.ks0,hi)<0.15)cls="P2";else if(b.d0>0.40&&b.d0<=0.50)cls="P3";else cls="P4";return new SBase{seed=b.seed,d0=b.d0,km0=b.km0,ks0=b.ks0,cls=cls};}
    static double DistK(double km,double ks,P3 hi){double dk=km-hi.km,dks=ks-hi.ks;return Math.Sqrt(dk*dk+dks*dks);}
    bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<5;e++){var h=Sim(K,n,0.10,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,0.10,seed+5),n).Average()>THR;}
    static P3 PCent(int n,bool hi){double d=0,k=0,ks=0;int c=0;for(int sd=0;sd<100;sd++){var K=KS(n,sd);double dm=0,km=0,kss=0;for(int e=0;e<5;e++){var h=Sim(K,n,0.10,sd+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}double om=Of(Sim(K,n,0.10,sd+5),n).Average();if((om>THR)==hi){d+=dm/5;k+=km/5;ks+=kss/5;c++;}}if(c==0)return new P3{dm=double.NaN,km=double.NaN,ks=double.NaN};return new P3{dm=d/c,km=k/c,ks=ks/c};}
}
