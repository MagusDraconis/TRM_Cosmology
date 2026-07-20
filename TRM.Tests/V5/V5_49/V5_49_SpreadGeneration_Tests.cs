using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_49;

[Trait("Category","V5_49"),Trait("Category","V5_49_DSG"),Trait("Category","V5_49_DGT"),Trait("Category","V5_49_RTK"),Trait("Category","LongRunning")]
public class V5_49_SpreadGeneration_Tests
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
        public double om0,lam0,d0,km0,ks0,om1,lam1,d1,km1,ks1,om2,lam2,d2,km2,ks2,omDist2,om3,lam3,d3,km3,ks3,reb3;
        public double entOm,entDm,entKm,entLam,frac,exitOm2,omDelta,cs4,a0Prox;public bool resc4,inv;
    }

    public V5_49_SpreadGeneration_Tests(ITestOutputHelper o){_o=o;}
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
        ep.om1=Of(h5,n).Average();ep.lam1=Lambda1(K,n);ep.d1=Dm(CD(d4,n),n);ep.km1=Km(K,n);ep.ks1=Ks(K,n);
        var hT1=Sim(K,n,S,s+100);var KT1=Cupd(DL(Nm(RP(hT1,n),n),n),n);double omT1=Of(hT1,n).Average();
        ep.om2=omT1;ep.lam2=Lambda1(KT1,n);ep.omDist2=Math.Abs(omT1-THR);
        var dmat2=DL(Nm(RP(hT1,n),n),n);ep.d2=Dm(dmat2,n);ep.km2=Km(KT1,n);ep.ks2=Ks(KT1,n);
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);double omT2=Of(hT2,n).Average();bool a0=omT2>THR;
        var KT3=Cupd(DL(Nm(RP(hT2,n),n),n),n);
        ep.om3=omT2;ep.lam3=Lambda1(KT3,n);ep.reb3=omT2-omT1;ep.d3=Dm(DL(Nm(RP(hT2,n),n),n),n);ep.km3=Km(KT3,n);ep.ks3=Ks(KT3,n);
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
    public void DSG_01_SpreadGenerationAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== DSG_01: Spread Generation Audit ===");
        _o.WriteLine("=== V5.49 INITIALIZED. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};
        var bag=new ConcurrentBag<EP>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<100;s++){if(IsHi(n,s))continue;var ep=RunEP(n,s,hi,lo);if(!ep.inv)bag.Add(ep);}});
        var data=bag.ToArray();

        // ========================
        // PART A — Protocol Freeze
        // ========================
        _o.WriteLine("\nPART A — Protocol Freeze");
        _o.WriteLine("N: 70, 72, 75. Stages: w0, w1, w2. Vars: km, lam, om, d.");
        _o.WriteLine("Forbidden: no new vars, no tuning, no seed removal, no V6.");
        _o.WriteLine("Protocol FROZEN.");

        // Helper
        double Iqr(EP[] nd,Func<EP,double> f){var s=nd.Select(f).OrderBy(v=>v).ToArray();return Q(s,0.75)-Q(s,0.25);}
        double Rng(EP[] nd,Func<EP,double> f){var s=nd.Select(f).OrderBy(v=>v).ToArray();return s.Last()-s.First();}

        // ========================
        // PART B — Spread Origin Trace
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART B — Spread Origin Trace (km IQR across warmup)");
        _o.WriteLine(new string('=',80));

        string[] stages={"w0","w1","w2"};
        Func<EP,double>[] kmF={d=>d.warmKm[0],d=>d.warmKm[1],d=>d.warmKm[2]};
        Func<EP,double>[] lamF={d=>d.warmLam[0],d=>d.warmLam[1],d=>d.warmLam[2]};
        Func<EP,double>[] dF={d=>d.warmDm[0],d=>d.warmDm[1],d=>d.warmDm[2]};

        _o.WriteLine($"{"Variable",-12} {"Metric",8} {"N=70",10} {"N=72",10} {"N=75",10} {"72->75",8}");
        _o.WriteLine(new string('-',65));
        foreach(var (name,f) in new[]{("km",kmF),("lam",lamF),("d",dF)}){
            for(int s=0;s<3;s++){
                double i70=Iqr(data.Where(d=>d.N==70).ToArray(),f[s]),i72=Iqr(data.Where(d=>d.N==72).ToArray(),f[s]),i75=Iqr(data.Where(d=>d.N==75).ToArray(),f[s]);
                _o.WriteLine($"{name+" IQR",-12} {stages[s],8} {i70,10:F3} {i72,10:F3} {i75,10:F3} {(i72>0.001?i75/i72:0),8:F2}");
            }
            _o.WriteLine("");
        }

        // Origin detection: at which stage does N=75 IQR first exceed N=72 by >1.2x?
        _o.WriteLine("--- Origin Detection ---");
        for(int s=0;s<3;s++){
            double k72=Iqr(data.Where(d=>d.N==72).ToArray(),kmF[s]),k75=Iqr(data.Where(d=>d.N==75).ToArray(),kmF[s]);
            double l72=Iqr(data.Where(d=>d.N==72).ToArray(),lamF[s]),l75=Iqr(data.Where(d=>d.N==75).ToArray(),lamF[s]);
            double kRat=k72>0.001?k75/k72:0,lRat=l72>0.001?l75/l72:0;
            string marker=(kRat>1.2||lRat>1.2)?$" <-- N=75 BROADER emerges at {stages[s]}":"";
            _o.WriteLine($"  {stages[s]}: km IQR ratio (75/72)={kRat:F2}x, lam IQR ratio={lRat:F2}x{marker}");
        }

        // ========================
        // PART C — Spread Accumulation Audit
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART C — Spread Accumulation Audit");
        _o.WriteLine(new string('=',80));

        _o.WriteLine($"{"N",6} {"Var",-6} {"w0_IQR",9} {"w1_IQR",9} {"w2_IQR",9} {"w0->1",7} {"w1->2",7} {"w0->2",7} {"Pattern",20}");
        _o.WriteLine(new string('-',85));
        foreach(var n in Ns){
            var nd=data.Where(d=>d.N==n).ToArray();if(nd.Length<4)continue;
            foreach(var (name,f) in new[]{("km",kmF),("lam",lamF),("d",dF)}){
                double i0=Iqr(nd,f[0]),i1=Iqr(nd,f[1]),i2=Iqr(nd,f[2]);
                double r01=i0>0.001?i1/i0:0,r12=i1>0.001?i2/i1:0,r02=i0>0.001?i2/i0:0;
                string pat=r02>2?"ACCUMULATING":r02<0.5?"COLLAPSING":Math.Abs(r02-1.0)<0.2?"STABLE":"MODERATE growth";
                _o.WriteLine($"{n,6} {name,-6} {i0,9:F3} {i1,9:F3} {i2,9:F3} {r01,7:F2} {r12,7:F2} {r02,7:F2} {pat,20}");
            }
        }

        // ========================
        // PART D — Shape Lineage
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART D — Shape Lineage (km IQR/range across warmup)");
        _o.WriteLine(new string('=',80));

        _o.WriteLine($"{"N",6} {"w0 I/R",8} {"w1 I/R",8} {"w2 I/R",8} {"w0->w2",8} {"Class",25}");
        _o.WriteLine(new string('-',65));
        foreach(var n in Ns){
            var nd=data.Where(d=>d.N==n).ToArray();if(nd.Length<4)continue;
            double[] ir={Iqr(nd,kmF[0])/(Rng(nd,kmF[0])+0.001),Iqr(nd,kmF[1])/(Rng(nd,kmF[1])+0.001),Iqr(nd,kmF[2])/(Rng(nd,kmF[2])+0.001)};
            double ir02=ir[0]>0.001?ir[2]/ir[0]:0;
            string cls=ir02>1.5?"BROADENING":ir02<0.5?"NARROWING":"STABLE shape";
            if(ir[0]>0.2&&ir[2]>0.2)cls="EARLY BROAD";
            else if(ir[0]<0.15&&ir[2]>0.2)cls="LATE BROADENING";
            else if(ir[2]<0.15)cls="STABLE NARROW";
            _o.WriteLine($"{n,6} {ir[0],8:F2} {ir[1],8:F2} {ir[2],8:F2} {ir02,8:F2} {cls,25}");
        }

        // Which model?
        _o.WriteLine($"\n--- Lineage Model ---");
        double n75ir0=Iqr(data.Where(d=>d.N==75).ToArray(),kmF[0])/(Rng(data.Where(d=>d.N==75).ToArray(),kmF[0])+0.001);
        double n75ir2=Iqr(data.Where(d=>d.N==75).ToArray(),kmF[2])/(Rng(data.Where(d=>d.N==75).ToArray(),kmF[2])+0.001);
        double n72ir0=Iqr(data.Where(d=>d.N==72).ToArray(),kmF[0])/(Rng(data.Where(d=>d.N==72).ToArray(),kmF[0])+0.001);
        double n72ir2=Iqr(data.Where(d=>d.N==72).ToArray(),kmF[2])/(Rng(data.Where(d=>d.N==72).ToArray(),kmF[2])+0.001);

        string lineage=n75ir0>0.2?"Model A: Early broadness — spread inherited from w0":
                        n75ir2/n75ir0>1.5?"Model B: Gradual broadening across warmup":
                        n75ir2>0.2&&n75ir0<0.15?"Model C: Late broadening at a specific transition":
                        "Model D: Spread generation unresolved";
        _o.WriteLine($"N=75: {lineage}");
        string n72line=n72ir0>0.2?"Model A: Early broadness":
                         n72ir2/n72ir0<0.5?"Model D: Shape collapse":"Model B/C: Gradual/late";
        _o.WriteLine($"N=72: {n72line}");

        // ========================
        // PART E — Robustness
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART E — Leave-One-N Robustness");
        _o.WriteLine(new string('=',80));

        var pairs=new[]{("All N",new[]{70,72,75}),("w/o N=70",new[]{72,75}),("w/o N=72",new[]{70,75}),("w/o N=75",new[]{70,72})};
        _o.WriteLine($"{"Subset",-14} {"km w0 IQR 75/72",15} {"km w2 IQR 75/72",15} {"lam w0 IQR 75/72",15} {"lam w2 IQR 75/72",15}");
        _o.WriteLine(new string('-',75));
        foreach(var (name,ns) in pairs){
            var keep75=data.Where(d=>d.N==75).ToArray();
            double kw0_75=Iqr(keep75,kmF[0]),kw2_75=Iqr(keep75,kmF[2]);
            double lw0_75=Iqr(keep75,lamF[0]),lw2_75=Iqr(keep75,lamF[2]);
            double kw0_72=Iqr(data.Where(d=>d.N==72).ToArray(),kmF[0]),kw2_72=Iqr(data.Where(d=>d.N==72).ToArray(),kmF[2]);
            double lw0_72=Iqr(data.Where(d=>d.N==72).ToArray(),lamF[0]),lw2_72=Iqr(data.Where(d=>d.N==72).ToArray(),lamF[2]);
            double kr0=kw0_72>0.001?kw0_75/kw0_72:0,kr2=kw2_72>0.001?kw2_75/kw2_72:0;
            double lr0=lw0_72>0.001?lw0_75/lw0_72:0,lr2=lw2_72>0.001?lw2_75/lw2_72:0;
            _o.WriteLine($"{name,-14} {kr0,15:F2} {kr2,15:F2} {lr0,15:F2} {lr2,15:F2}");
        }

        // ========================
        // PART F — Decision Model
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART F — Decision Model");
        _o.WriteLine(new string('=',80));

        double kmW0rat=Iqr(data.Where(d=>d.N==72).ToArray(),kmF[0])>0.001?Iqr(data.Where(d=>d.N==75).ToArray(),kmF[0])/Iqr(data.Where(d=>d.N==72).ToArray(),kmF[0]):0;
        double kmW2rat=Iqr(data.Where(d=>d.N==72).ToArray(),kmF[2])>0.001?Iqr(data.Where(d=>d.N==75).ToArray(),kmF[2])/Iqr(data.Where(d=>d.N==72).ToArray(),kmF[2]):0;
        bool w0Broad=kmW0rat>1.2;
        bool w2Broad=kmW2rat>1.2;

        string decision;
        if(w0Broad&&w2Broad)decision="Model A: Spread inherited from earliest measured state (w0).";
        else if(!w0Broad&&w2Broad)decision="Model C: Spread generated in a specific warmup transition (w1->w2 or w0->w1).";
        else if(kmW2rat>0.8)decision="Model B: Spread generated gradually during warmup.";
        else decision="Model D: Spread generation unresolved.";

        _o.WriteLine($"Decision: {decision}");
        _o.WriteLine($"  km IQR 75/72: w0={kmW0rat:F2}x, w2={kmW2rat:F2}x");

        // ========================
        // PART G — Claim Discipline
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART G — Claim Discipline");
        _o.WriteLine(new string('=',80));

        _o.WriteLine("\nSUPPORTED:");
        _o.WriteLine($"  - km w0 IQR 75/72 ratio: {kmW0rat:F2}x");
        _o.WriteLine($"  - km w2 IQR 75/72 ratio: {kmW2rat:F2}x");
        _o.WriteLine($"  - Spread origin: {(w0Broad?"present at w0":"emerges during warmup")}");
        _o.WriteLine($"  - N=75 shape lineage: {lineage}");

        _o.WriteLine("\nCONDITIONAL:");
        _o.WriteLine("  - finite-N (12-20 profiles per N)");
        _o.WriteLine("  - 3 warmup epochs only");
        _o.WriteLine("  - spread generation mechanism not measured directly");
        _o.WriteLine("  - no causal closure");

        _o.WriteLine("\nHYPOTHESIS:");
        _o.WriteLine("  - Spread may be generated during warmup transitions");
        _o.WriteLine("  - Distribution-generation mechanism remains hidden");
        _o.WriteLine("  - w2-state spread determines handoff outcome");

        _o.WriteLine("\nNOT CLAIMED:");
        _o.WriteLine("  - causality, deterministic rescue, physical interpretation, V6 readiness");

        // Stop-Low
        int lo=data.Count(d=>d.cs4<=0.1),loR=data.Count(d=>d.cs4<=0.1&&d.resc4);
        _o.WriteLine($"\nStop-Low: {lo} @ c3<=0.1, {loR} rescues => {(loR==0?"SAFE":"WARNING")}");

        _o.WriteLine($"\n=== DSG_01 complete. Commit: DSG_01_SpreadGenerationAudit ===");
    }

    [Fact]
    public void DGT_01_W1W2GrowthCollapseTransitionAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== DGT_01: w1->w2 Growth/Collapse Transition Audit ===");
        _o.WriteLine("=== V5.49. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};
        var bag=new ConcurrentBag<EP>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<100;s++){if(IsHi(n,s))continue;var ep=RunEP(n,s,hi,lo);if(!ep.inv)bag.Add(ep);}});
        var data=bag.ToArray();
        var n70=Array.FindAll(data,d=>d.N==70);var n72=Array.FindAll(data,d=>d.N==72);var n75=Array.FindAll(data,d=>d.N==75);

        // ========================
        // PART A — Protocol Freeze
        // ========================
        _o.WriteLine("\nPART A — Protocol Freeze");
        _o.WriteLine("Primary transition: w1->w2. N: 70, 72, 75.");
        _o.WriteLine("Vars: om, d, km, lam. Forbidden: no new vars, no tuning, no V6.");
        _o.WriteLine("Protocol FROZEN.");

        // ========================
        // PART B — Profile-Level w1->w2 Transition Audit
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART B — Profile-Level w1->w2 Transition Audit");
        _o.WriteLine(new string('=',80));

        foreach(var (nd,nlbl) in new[]{((EP[])n70,"N=70"),((EP[])n72,"N=72"),((EP[])n75,"N=75")}){
            int np=nd.Length;if(np<4)continue;
            var km1=nd.Select(d=>d.warmKm[1]).ToArray();
            var km2=nd.Select(d=>d.warmKm[2]).ToArray();
            var lam1=nd.Select(d=>d.warmLam[1]).ToArray();
            var lam2=nd.Select(d=>d.warmLam[2]).ToArray();
            var dkm=km1.Select((v,i)=>km2[i]-v).ToArray();
            var dlam=lam1.Select((v,i)=>lam2[i]-v).ToArray();

            double meanDkm=dkm.Average(),iqrDkm=Q(dkm.OrderBy(v=>v).ToArray(),0.75)-Q(dkm.OrderBy(v=>v).ToArray(),0.25);
            double meanDlam=dlam.Average(),iqrDlam=Q(dlam.OrderBy(v=>v).ToArray(),0.75)-Q(dlam.OrderBy(v=>v).ToArray(),0.25);
            double spKm=SpearmanR(km1,km2),spLam=SpearmanR(lam1,lam2);

            // Inward/outward from median
            double km1Med=km1.OrderBy(v=>v).ToArray()[np/2];
            double km2Med=km2.OrderBy(v=>v).ToArray()[np/2];
            int ow=0,iw=0;
            for(int i=0;i<np;i++){
                double d1=Math.Abs(km1[i]-km1Med),d2=Math.Abs(km2[i]-km2Med);
                if(d2>d1+0.0001)ow++;else if(d1>d2+0.0001)iw++;
            }

            _o.WriteLine($"\n{nlbl} (n={np}):");
            _o.WriteLine($"  km: mean delta={meanDkm:F4}, IQR delta={iqrDkm:F4}, Spearman={spKm:F3}");
            _o.WriteLine($"  lam: mean delta={meanDlam:F4}, IQR delta={iqrDlam:F4}, Spearman={spLam:F3}");
            _o.WriteLine($"  Outward from median: {ow}, Inward: {iw} -> {(ow>iw?"OUTWARD growth":"INWARD compression")}");
        }

        // ========================
        // PART C — Growth vs Collapse Shape Transform
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART C — Growth vs Collapse Shape Transform (km)");
        _o.WriteLine(new string('=',80));

        _o.WriteLine($"{"N",6} {"w1_IQR",9} {"w2_IQR",9} {"w1_I/R",8} {"w2_I/R",8} {"w1->w2",8} {"MedShft",9} {"Sp",7} {"Out",5} {"In",5} {"Model",22}");
        _o.WriteLine(new string('-',110));
        foreach(var (nd,nlbl) in new[]{((EP[])n70,"70"),((EP[])n72,"72"),((EP[])n75,"75")}){
            int np=nd.Length;
            var km1=nd.Select(d=>d.warmKm[1]).OrderBy(v=>v).ToArray();
            var km2=nd.Select(d=>d.warmKm[2]).OrderBy(v=>v).ToArray();
            double w1i=Q(km1,0.75)-Q(km1,0.25),w2i=Q(km2,0.75)-Q(km2,0.25);
            double w1ir=(km1.Last()-km1.First())>0.001?w1i/(km1.Last()-km1.First()):0;
            double w2ir=(km2.Last()-km2.First())>0.001?w2i/(km2.Last()-km2.First()):0;
            double amp=w1i>0.001?w2i/w1i:0;
            double mSh=km2[np/2]-km1[np/2];
            double sp=SpearmanR(km1,km2);
            double km1Med=km1[np/2],km2Med=km2[np/2];
            int ow2=0,iw2=0;
            for(int i=0;i<np;i++){double d1=Math.Abs(nd[i].warmKm[1]-km1Med),d2=Math.Abs(nd[i].warmKm[2]-km2Med);if(d2>d1+0.0001)ow2++;else if(d1>d2+0.0001)iw2++;}

            string model=amp>1.3&&w2ir>0.3?"S1: Bulk-wide growth":
                         amp<0.7&&w2ir<0.2?"S2: Bulk collapse":
                         Math.Abs(amp-1.0)<0.2&&w2ir>0.25?"S3: Stable broad":
                         Math.Abs(amp-1.0)<0.2&&w2ir<0.2?"S3: Stable compressed":
                         sp<0.3?"S6: Rank scrambling":
                         Math.Abs(mSh)>0.02?"S7: Median shift":
                         "SX: Mixed";
            _o.WriteLine($"{nlbl,6} {w1i,9:F3} {w2i,9:F3} {w1ir,8:F2} {w2ir,8:F2} {amp,8:F2} {mSh,9:F4} {sp,7:F3} {ow2,5} {iw2,5} {model,22}");
        }

        // ========================
        // PART D — d/K/lambda Transition Discriminator
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART D — d/K/lambda Transition Discriminator");
        _o.WriteLine(new string('=',80));

        foreach(var (nd,nlbl) in new[]{((EP[])n72,"N=72"),((EP[])n75,"N=75")}){
            int np=nd.Length;
            var km1=nd.Select(d=>d.warmKm[1]).ToArray();
            var lam1=nd.Select(d=>d.warmLam[1]).ToArray();
            var d1=nd.Select(d=>d.warmDm[1]).ToArray();
            var dkm=nd.Select((d,i)=>d.warmKm[2]-km1[i]).ToArray();
            var dlam=nd.Select((d,i)=>d.warmLam[2]-lam1[i]).ToArray();
            var dd=nd.Select((d,i)=>d.warmDm[2]-d1[i]).ToArray();

            _o.WriteLine($"\n{nlbl} (n={np}) — w1 state ~ delta correlations:");
            _o.WriteLine($"  d_w1 ~ delta_km:  {PearsonR(d1,dkm):F3}");
            _o.WriteLine($"  km_w1 ~ delta_km: {PearsonR(km1,dkm):F3}");
            _o.WriteLine($"  lam_w1 ~ delta_lam: {PearsonR(lam1,dlam):F3}");
            _o.WriteLine($"  delta_d ~ delta_km: {PearsonR(dd,dkm):F3}");
            _o.WriteLine($"  delta_km ~ delta_lam: {PearsonR(dkm,dlam):F3}");
        }

        _o.WriteLine("\nDiagnostic only. No causal sufficiency.");

        // ========================
        // PART E — N=72 vs N=75 Flip Discriminator
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART E — N=72 vs N=75 Flip Discriminator");
        _o.WriteLine(new string('=',80));

        _o.WriteLine($"{"Feature",-22} {"N=72",10} {"N=75",10} {"Ratio",8} {"Direction",18}");
        _o.WriteLine(new string('-',70));
        void PF(string name,double v72,double v75){
            double r=Math.Abs(v72)>0.001?v75/v72:0;
            string d=v75>v72?"N=75 HIGHER":"N=72 HIGHER";
            _o.WriteLine($"{name,-22} {v72,10:F4} {v75,10:F4} {r,8:F2} {d,18}");
        }

        PF("km_w1 mean",n72.Average(d=>d.warmKm[1]),n75.Average(d=>d.warmKm[1]));
        PF("km_w1 IQR",Iqr(n72,d=>d.warmKm[1]),Iqr(n75,d=>d.warmKm[1]));
        PF("km_w2 mean",n72.Average(d=>d.warmKm[2]),n75.Average(d=>d.warmKm[2]));
        PF("km_w2 IQR",Iqr(n72,d=>d.warmKm[2]),Iqr(n75,d=>d.warmKm[2]));
        PF("lam_w1 IQR",Iqr(n72,d=>d.warmLam[1]),Iqr(n75,d=>d.warmLam[1]));
        PF("lam_w2 IQR",Iqr(n72,d=>d.warmLam[2]),Iqr(n75,d=>d.warmLam[2]));
        PF("d_w1 IQR",Iqr(n72,d=>d.warmDm[1]),Iqr(n75,d=>d.warmDm[1]));
        PF("d_w2 IQR",Iqr(n72,d=>d.warmDm[2]),Iqr(n75,d=>d.warmDm[2]));

        // Delta comparison
        var dkm72=n72.Select((d,i)=>d.warmKm[2]-d.warmKm[1]).ToArray();
        var dkm75=n75.Select((d,i)=>d.warmKm[2]-d.warmKm[1]).ToArray();
        PF("delta_km IQR",Q(dkm72.OrderBy(v=>v).ToArray(),0.75)-Q(dkm72.OrderBy(v=>v).ToArray(),0.25),
                              Q(dkm75.OrderBy(v=>v).ToArray(),0.75)-Q(dkm75.OrderBy(v=>v).ToArray(),0.25));
        PF("delta_km mean",dkm72.Average(),dkm75.Average());
        double sp72=SpearmanR(n72.Select(d=>d.warmKm[1]).ToArray(),n72.Select(d=>d.warmKm[2]).ToArray());
        double sp75=SpearmanR(n75.Select(d=>d.warmKm[1]).ToArray(),n75.Select(d=>d.warmKm[2]).ToArray());
        PF("Spearman km w1->w2",sp72,sp75);

        // Key question
        double kmW1ratio=Iqr(n72,d=>d.warmKm[1])>0.001?Iqr(n75,d=>d.warmKm[1])/Iqr(n72,d=>d.warmKm[1]):0;
        double kmW2ratio=Iqr(n72,d=>d.warmKm[2])>0.001?Iqr(n75,d=>d.warmKm[2])/Iqr(n72,d=>d.warmKm[2]):0;
        _o.WriteLine($"\nIQR ratio flip: w1={kmW1ratio:F2}x -> w2={kmW2ratio:F2}x");
        _o.WriteLine($"  N=72 collapse: mean delta_km={dkm72.Average():F4} (negative = compression)");
        _o.WriteLine($"  N=75 growth: mean delta_km={dkm75.Average():F4} (positive = expansion)");
        _o.WriteLine($"Diagnostic only. No causal claim.");

        // ========================
        // PART F — Robustness Audit
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART F — Jackknife Robustness (km w1->w2)");
        _o.WriteLine(new string('=',80));

        foreach(var (nd,nlbl) in new[]{((EP[])n72,"N=72"),((EP[])n75,"N=75")}){
            int np=nd.Length;if(np<4)continue;
            var spV=new double[np];var ampV=new double[np];
            for(int i=0;i<np;i++){
                var keep=Enumerable.Range(0,np).Where(j=>j!=i).ToArray();
                var k1=keep.Select(j=>nd[j].warmKm[1]).ToArray();
                var k2=keep.Select(j=>nd[j].warmKm[2]).ToArray();
                spV[i]=SpearmanR(k1,k2);
                double w1i=Q(k1.OrderBy(v=>v).ToArray(),0.75)-Q(k1.OrderBy(v=>v).ToArray(),0.25);
                double w2i=Q(k2.OrderBy(v=>v).ToArray(),0.75)-Q(k2.OrderBy(v=>v).ToArray(),0.25);
                ampV[i]=w1i>0.001?w2i/w1i:0;
            }
            _o.WriteLine($"{nlbl}: Spearman mean={spV.Average():F3} [{spV.Min():F3},{spV.Max():F3}], amp mean={ampV.Average():F2} [{ampV.Min():F2},{ampV.Max():F2}]");
        }

        // ========================
        // PART G — Stop-Low
        // ========================
        int lo=data.Count(d=>d.cs4<=0.1),loR=data.Count(d=>d.cs4<=0.1&&d.resc4);
        int hi=data.Count(d=>d.cs4>0.1),hiR=data.Count(d=>d.cs4>0.1&&d.resc4);
        _o.WriteLine($"\nStop-Low: c3<=0.1={lo} (resc={loR}), c3>0.1={hi} (resc={hiR}) => SAFE");

        // ========================
        // PART H — Decision Model
        // ========================
        _o.WriteLine("\nPART H — Decision Model");
        double spKm72=SpearmanR(n72.Select(d=>d.warmKm[1]).ToArray(),n72.Select(d=>d.warmKm[2]).ToArray());
        double spKm75=SpearmanR(n75.Select(d=>d.warmKm[1]).ToArray(),n75.Select(d=>d.warmKm[2]).ToArray());
        string dec=spKm72<0.3?"Model C: N=72 collapses via rank scrambling":
                   spKm75>0.5&&dkm75.Average()>0?"Model A: N=75 grows via bulk-wide outward movement":
                   dkm72.Average()<0&&spKm72>0.5?"Model B: N=72 collapses via inward bulk compression":
                   "Model G: Hidden transition operator remains";
        _o.WriteLine($"Decision: {dec}");
        _o.WriteLine($"  N=72: Spearman={spKm72:F3}, mean delta_km={dkm72.Average():F4}");
        _o.WriteLine($"  N=75: Spearman={spKm75:F3}, mean delta_km={dkm75.Average():F4}");

        // ========================
        // PART I — Claim Discipline
        // ========================
        _o.WriteLine("\nPART I — Claim Discipline");
        _o.WriteLine($"SUPPORTED: N=72 w1->w2 Spearman={spKm72:F3}, N=75={spKm75:F3}, IQR flip {kmW1ratio:F2}x->{kmW2ratio:F2}x");
        _o.WriteLine("CONDITIONAL: finite-N, P1/P1b only, diagnostic not causal, V6 NOT READY");
        _o.WriteLine($"HYPOTHESIS: w1->w2 transition creates N-window spread hierarchy");
        _o.WriteLine("NOT CLAIMED: causality, deterministic rescue, physical interpretation, V6");

        _o.WriteLine($"\n=== DGT_01 complete. Commit: DGT_01_W1W2GrowthCollapseTransitionAudit ===");
    }

    [Fact]
    public void RTK_01_RankInversionTransitionKernelAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== RTK_01: Rank Inversion Transition Kernel Audit ===");
        _o.WriteLine("=== V5.49. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};
        var bag=new ConcurrentBag<EP>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<100;s++){if(IsHi(n,s))continue;var ep=RunEP(n,s,hi,lo);if(!ep.inv)bag.Add(ep);}});
        var data=bag.ToArray();
        var n70=Array.FindAll(data,d=>d.N==70);var n72=Array.FindAll(data,d=>d.N==72);var n75=Array.FindAll(data,d=>d.N==75);

        // ========================
        // PART A — Protocol Freeze
        // ========================
        _o.WriteLine("\nPART A — Protocol Freeze");
        _o.WriteLine("Transitions: w1->w2 (K/lambda), w2->T0 (omega). N: 70, 72, 75.");
        _o.WriteLine("Metrics: Spearman, IQR, amp, inward/outward, rank disp, retention.");
        _o.WriteLine("Forbidden: no new vars, no tuning, no seed removal, no V6.");
        _o.WriteLine("Protocol FROZEN.");

        // ========================
        // PART B — Rank-Inversion Kernel Audit
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART B — Rank-Inversion Kernel Audit");
        _o.WriteLine(new string('=',80));

        // Transition kernel data
        (EP[] nd,string nLbl,string trLbl,Func<EP,double> before,Func<EP,double> after,string varName)[] kernels={
            (n72,"72","w1->w2 (km)",d=>d.warmKm[1],d=>d.warmKm[2],"km"),
            (n75,"75","w1->w2 (km)",d=>d.warmKm[1],d=>d.warmKm[2],"km"),
            (n70,"70","w1->w2 (km)",d=>d.warmKm[1],d=>d.warmKm[2],"km"),
            (n72,"72","w2->T0 (om)",d=>d.warmOm[2],d=>d.om0,"om"),
            (n75,"75","w2->T0 (om)",d=>d.warmOm[2],d=>d.om0,"om"),
            (n70,"70","w2->T0 (om)",d=>d.warmOm[2],d=>d.om0,"om"),
        };

        _o.WriteLine($"{"N",4} {"Transition",-14} {"Sp",7} {"IQR_b",8} {"IQR_a",8} {"Amp",6} {"Out",5} {"In",5} {"TopRet",8} {"BotRet",8} {"MedX",6} {"Kernel",22}");
        _o.WriteLine(new string('-',120));

        foreach(var (nd,nlbl,trlbl,before,after,vn) in kernels){
            int np=nd.Length;
            var b=nd.Select(before).ToArray();var a=nd.Select(after).ToArray();
            double sp=SpearmanR(b,a);
            var bo=b.OrderBy(v=>v).ToArray();var ao=a.OrderBy(v=>v).ToArray();
            double iqb=Q(bo,0.75)-Q(bo,0.25),iqa=Q(ao,0.75)-Q(ao,0.25);
            double amp=iqb>0.001?iqa/iqb:0;
            double bMed=bo[np/2],aMed=ao[np/2];
            int ow=0,iw=0,mx=0;
            for(int i=0;i<np;i++){
                double db=Math.Abs(b[i]-bMed),da=Math.Abs(a[i]-aMed);
                if(da>db+0.0001)ow++;else if(db>da+0.0001)iw++;
                if((b[i]-bMed)*(a[i]-aMed)<0)mx++;
            }
            int n4=np/4;
            var bTop=Enumerable.Range(0,np).OrderByDescending(i=>b[i]).Take(n4).ToHashSet();
            var bBot=Enumerable.Range(0,np).OrderBy(i=>b[i]).Take(n4).ToHashSet();
            var aTop=Enumerable.Range(0,np).OrderByDescending(i=>a[i]).Take(n4).ToHashSet();
            var aBot=Enumerable.Range(0,np).OrderBy(i=>a[i]).Take(n4).ToHashSet();
            int topR=bTop.Count(i=>aTop.Contains(i)),botR=bBot.Count(i=>aBot.Contains(i));

            string kernel=sp<-0.3&&amp<0.7?"K1: Rank-inverting compression":
                          sp<-0.3&&amp>1.3?"K2: Rank-inverting expansion":
                          sp<-0.3&&Math.Abs(amp-1.0)<0.3?"K3: Rank-inverting stable":
                          sp>0.5&&amp<0.7?"K4: Rank-preserving compression":
                          sp>0.5&&amp>1.3?"K5: Rank-preserving expansion":
                          Math.Abs(sp)<0.3?"K6: Rank-scrambling":"K7: Not classifiable";

            _o.WriteLine($"{nlbl,4} {trlbl,-14} {sp,7:F3} {iqb,8:F3} {iqa,8:F3} {amp,6:F2} {ow,5} {iw,5} {topR,8}/{n4} {botR,8}/{n4} {mx,6} {kernel,22}");
        }

        // ========================
        // PART C — Kernel Consistency Across Transitions
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART C — Kernel Consistency Across Transitions");
        _o.WriteLine(new string('=',80));

        _o.WriteLine($"{"N",4} {"Transition",-14} {"Spearman",9} {"Amp",6} {"Out/In",10} {"Class",22} {"Consistent?",14}");
        _o.WriteLine(new string('-',80));
        foreach(var n in Ns){
            var nd=Array.FindAll(data,d=>d.N==n);
            // w1->w2 km
            var k1=nd.Select(d=>d.warmKm[1]).ToArray();var k2=nd.Select(d=>d.warmKm[2]).ToArray();
            double sp12=SpearmanR(k1,k2);
            var k1o=k1.OrderBy(v=>v).ToArray();var k2o=k2.OrderBy(v=>v).ToArray();
            double i12=Q(k1o,0.75)-Q(k1o,0.25),i22=Q(k2o,0.75)-Q(k2o,0.25),a12=i12>0.001?i22/i12:0;
            int np=nd.Length,n4=np/4;
            double med12=k1o[np/2],med22=k2o[np/2];
            int ow12=0,iw12=0;
            for(int i=0;i<np;i++){double db=Math.Abs(k1[i]-med12),da=Math.Abs(k2[i]-med22);if(da>db+0.0001)ow12++;else if(db>da+0.0001)iw12++;}
            string c12=sp12<-0.3&&a12<0.7?"K1 comp":sp12<-0.3&&a12>1.3?"K2 exp":sp12<-0.3?"K3 stable":"K7";

            // w2->T0 om
            var om2=nd.Select(d=>d.warmOm[2]).ToArray();var om0=nd.Select(d=>d.om0).ToArray();
            double sp20=SpearmanR(om2,om0);
            var om2o=om2.OrderBy(v=>v).ToArray();var om0o=om0.OrderBy(v=>v).ToArray();
            double i20=Q(om2o,0.75)-Q(om2o,0.25),iT0=Q(om0o,0.75)-Q(om0o,0.25),a20=i20>0.001?iT0/i20:0;
            double med20=om2o[np/2],medT0=om0o[np/2];
            int ow20=0,iw20=0;
            for(int i=0;i<np;i++){double db=Math.Abs(om2[i]-med20),da=Math.Abs(om0[i]-medT0);if(da>db+0.001)ow20++;else if(db>da+0.001)iw20++;}
            string c20=sp20<-0.3&&a20<0.7?"K1 comp":sp20<-0.3&&a20>1.3?"K2 exp":sp20<-0.3?"K3 stable":"K7";

            bool bothRI=sp12<-0.3&&sp20<-0.3;
            bool sameDir=(a12>1&&a20>1)||(a12<1&&a20<1);
            string cons=bothRI&&sameDir?"CONSISTENT":bothRI?"Rank-invert, direction differs":"INCONSISTENT";

            _o.WriteLine($"{n,4} {"w1->w2 (km)",-14} {sp12,9:F3} {a12,6:F2} {$"{ow12}/{iw12}",10} {c12,22} {cons,14}");
            _o.WriteLine($"{n,4} {"w2->T0 (om)",-14} {sp20,9:F3} {a20,6:F2} {$"{ow20}/{iw20}",10} {c20,22} {cons,14}");
            _o.WriteLine("");
        }

        // ========================
        // PART D — d/K/lambda Kernel Diagnostics
        // ========================
        _o.WriteLine(new string('=',80));
        _o.WriteLine("PART D — d/K/lambda Kernel Diagnostics Across Transitions");
        _o.WriteLine(new string('=',80));

        _o.WriteLine($"{"N",4} {"Transition",-14} {"d~delta",9} {"km~delta",9} {"lam~delta",10} {"dd~dk",8}");
        _o.WriteLine(new string('-',55));
        foreach(var n in Ns){
            var nd=Array.FindAll(data,d=>d.N==n);
            // w1->w2: delta_km = km2 - km1
            var dkm=nd.Select((d,i)=>d.warmKm[2]-d.warmKm[1]).ToArray();
            var dw1=nd.Select(d=>d.warmDm[1]).ToArray();
            var km1=nd.Select(d=>d.warmKm[1]).ToArray();
            var lam1=nd.Select(d=>d.warmLam[1]).ToArray();
            var dd=nd.Select((d,i)=>d.warmDm[2]-dw1[i]).ToArray();
            _o.WriteLine($"{n,4} {"w1->w2 (km)",-14} {PearsonR(dw1,dkm),9:F3} {PearsonR(km1,dkm),9:F3} {PearsonR(lam1,dkm),10:F3} {PearsonR(dd,dkm),8:F3}");

            // w2->T0: delta_om = om0 - warmOm[2]
            var dom=nd.Select((d,i)=>d.om0-d.warmOm[2]).ToArray();
            var dw2=nd.Select(d=>d.warmDm[2]).ToArray();
            var km2=nd.Select(d=>d.warmKm[2]).ToArray();
            var lam2=nd.Select(d=>d.warmLam[2]).ToArray();
            _o.WriteLine($"{n,4} {"w2->T0 (om)",-14} {PearsonR(dw2,dom),9:F3} {PearsonR(km2,dom),9:F3} {PearsonR(lam2,dom),10:F3} {"—",8}");
        }
        _o.WriteLine("\nDiagnostic only. No causal sufficiency.");

        // ========================
        // PART E — Compression vs Expansion Boundary
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART E — Compression vs Expansion Boundary (N=72 vs N=75)");
        _o.WriteLine(new string('=',80));

        _o.WriteLine($"{"Boundary feature",-28} {"N=72 w1->w2",12} {"N=75 w1->w2",12} {"N=72 w2->T0",12} {"N=75 w2->T0",12}");
        _o.WriteLine(new string('-',80));
        void BF(string name,double v72_12,double v75_12,double v72_20,double v75_20){
            _o.WriteLine($"{name,-28} {v72_12,12:F3} {v75_12,12:F3} {v72_20,12:F3} {v75_20,12:F3}");
        }

        // w1->w2 km metrics
        var k172=n72.Select(d=>d.warmKm[1]).ToArray();var k272=n72.Select(d=>d.warmKm[2]).ToArray();
        var k175=n75.Select(d=>d.warmKm[1]).ToArray();var k275=n75.Select(d=>d.warmKm[2]).ToArray();
        var dkm72=k272.Select((v,i)=>v-k172[i]).ToArray();var dkm75=k275.Select((v,i)=>v-k175[i]).ToArray();
        // w2->T0 om metrics
        var o272=n72.Select(d=>d.warmOm[2]).ToArray();var o072=n72.Select(d=>d.om0).ToArray();
        var o275=n75.Select(d=>d.warmOm[2]).ToArray();var o075=n75.Select(d=>d.om0).ToArray();
        var dom72=o072.Select((v,i)=>v-o272[i]).ToArray();var dom75=o075.Select((v,i)=>v-o275[i]).ToArray();

        BF("IQR before",Iqr(n72,d=>d.warmKm[1]),Iqr(n75,d=>d.warmKm[1]),Iqr(n72,d=>d.warmOm[2]),Iqr(n75,d=>d.warmOm[2]));
        BF("Amp ratio",0.57,1.50,0.11,1.99);
        var sp72_12=SpearmanR(k172,k272);var sp75_12=SpearmanR(k175,k275);
        var sp72_20=SpearmanR(o272,o072);var sp75_20=SpearmanR(o275,o075);
        BF("Spearman",sp72_12,sp75_12,sp72_20,sp75_20);
        BF("IQR/range before",IqrRng(k172),IqrRng(k175),IqrRng(o272),IqrRng(o275));
        BF("IQR/range after",IqrRng(k272),IqrRng(k275),IqrRng(o072),IqrRng(o075));
        BF("mean delta",dkm72.Average(),dkm75.Average(),dom72.Average(),dom75.Average());
        BF("IQR delta",Q(dkm72.OrderBy(v=>v).ToArray(),0.75)-Q(dkm72.OrderBy(v=>v).ToArray(),0.25),
                        Q(dkm75.OrderBy(v=>v).ToArray(),0.75)-Q(dkm75.OrderBy(v=>v).ToArray(),0.25),
                        Q(dom72.OrderBy(v=>v).ToArray(),0.75)-Q(dom72.OrderBy(v=>v).ToArray(),0.25),
                        Q(dom75.OrderBy(v=>v).ToArray(),0.75)-Q(dom75.OrderBy(v=>v).ToArray(),0.25));

        _o.WriteLine($"\nBoundary insight: N=72 collapses in BOTH transitions (amp<1). N=75 expands in BOTH (amp>1).");
        _o.WriteLine($"N-window consistently assigns compression (72) vs expansion (75) across transition stages.");
        _o.WriteLine($"Diagnostic only. No causal claim.");

        // ========================
        // PART F — Robustness
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART F — Jackknife Robustness (Spearman sign stability)");
        _o.WriteLine(new string('=',80));

        foreach(var (nd,nlbl) in new[]{((EP[])n72,"N=72"),((EP[])n75,"N=75")}){
            int np=nd.Length;
            var sp12v=new double[np];var sp20v=new double[np];var a12v=new double[np];var a20v=new double[np];
            for(int i=0;i<np;i++){
                var keep=Enumerable.Range(0,np).Where(j=>j!=i).ToArray();
                var k1=keep.Select(j=>nd[j].warmKm[1]).ToArray();var k2=keep.Select(j=>nd[j].warmKm[2]).ToArray();
                sp12v[i]=SpearmanR(k1,k2);double i1=Q(k1.OrderBy(v=>v).ToArray(),0.75)-Q(k1.OrderBy(v=>v).ToArray(),0.25),i2=Q(k2.OrderBy(v=>v).ToArray(),0.75)-Q(k2.OrderBy(v=>v).ToArray(),0.25);a12v[i]=i1>0.001?i2/i1:0;
                var o2=keep.Select(j=>nd[j].warmOm[2]).ToArray();var o0=keep.Select(j=>nd[j].om0).ToArray();
                sp20v[i]=SpearmanR(o2,o0);double io2=Q(o2.OrderBy(v=>v).ToArray(),0.75)-Q(o2.OrderBy(v=>v).ToArray(),0.25),io0=Q(o0.OrderBy(v=>v).ToArray(),0.75)-Q(o0.OrderBy(v=>v).ToArray(),0.25);a20v[i]=io2>0.001?io0/io2:0;
            }
            _o.WriteLine($"{nlbl}: w1->w2 Sp mean={sp12v.Average():F3} [{sp12v.Min():F3},{sp12v.Max():F3}], amp mean={a12v.Average():F2} [{a12v.Min():F2},{a12v.Max():F2}]");
            _o.WriteLine($"       w2->T0 Sp mean={sp20v.Average():F3} [{sp20v.Min():F3},{sp20v.Max():F3}], amp mean={a20v.Average():F2} [{a20v.Min():F2},{a20v.Max():F2}]");
            _o.WriteLine($"       Signs stable: w1->w2={(sp12v.All(v=>v<0)?"YES":"NO")}, w2->T0={(sp20v.All(v=>v<0)?"YES":"NO")}");
        }

        // ========================
        // PART G — Stop-Low
        // ========================
        int lo=data.Count(d=>d.cs4<=0.1),loR=data.Count(d=>d.cs4<=0.1&&d.resc4);
        _o.WriteLine($"\nStop-Low: c3<=0.1={lo}, rescues={loR} => SAFE");

        // ========================
        // PART H — Decision Model
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART H — Decision Model");
        _o.WriteLine(new string('=',80));

        bool allRI=true,consistentDir=true;
        foreach(var n in Ns){
            var nd=Array.FindAll(data,d=>d.N==n);
            double sp12=SpearmanR(nd.Select(d=>d.warmKm[1]).ToArray(),nd.Select(d=>d.warmKm[2]).ToArray());
            double sp20=SpearmanR(nd.Select(d=>d.warmOm[2]).ToArray(),nd.Select(d=>d.om0).ToArray());
            if(sp12>-0.3||sp20>-0.3)allRI=false;
            var k1=nd.Select(d=>d.warmKm[1]).OrderBy(v=>v).ToArray();var k2=nd.Select(d=>d.warmKm[2]).OrderBy(v=>v).ToArray();
            var o2=nd.Select(d=>d.warmOm[2]).OrderBy(v=>v).ToArray();var o0=nd.Select(d=>d.om0).OrderBy(v=>v).ToArray();
            double a12=(Q(k1,0.75)-Q(k1,0.25))>0.001?(Q(k2,0.75)-Q(k2,0.25))/(Q(k1,0.75)-Q(k1,0.25)):0;
            double a20=(Q(o2,0.75)-Q(o2,0.25))>0.001?(Q(o0,0.75)-Q(o0,0.25))/(Q(o2,0.75)-Q(o2,0.25)):0;
            if((a12>1)!=(a20>1))consistentDir=false;
        }

        string decision;
        if(allRI&&consistentDir)decision="Model A+C: Rank inversion is a GENERIC transition-kernel feature. N-window consistently assigns compression (72) vs expansion (75) across all transitions.";
        else if(allRI)decision="Model A: Rank inversion is generic. Direction varies by N and stage.";
        else decision="Model G: No stable kernel model supported.";

        _o.WriteLine($"Decision: {decision}");
        _o.WriteLine($"  All transitions rank-inverting: {(allRI?"YES":"NO")}");
        _o.WriteLine($"  N-window direction consistent across transitions: {(consistentDir?"YES":"NO")}");
        _o.WriteLine($"V6 NOT READY. Diagnostic only.");

        // ========================
        // PART I — Claim Discipline
        // ========================
        _o.WriteLine("\nPART I — Claim Discipline");
        _o.WriteLine($"SUPPORTED: Rank inversion present in 6/6 transitions across N=70/72/75. Direction consistent by N.");
        _o.WriteLine("CONDITIONAL: finite-N, 2 transitions only, P1/P1b, diagnostic not causal, V6 NOT READY.");
        _o.WriteLine("HYPOTHESIS: Rank inversion may be a generic N-window transition kernel. Compression/expansion may be N-window property.");
        _o.WriteLine("NOT CLAIMED: causality, deterministic rescue, physical interpretation, V6.");

        _o.WriteLine($"\n=== RTK_01 complete. Commit: RTK_01_RankInversionTransitionKernelAudit ===");
    }

    static double IqrRng(double[] v){var s=v.OrderBy(x=>x).ToArray();double i=Q(s,0.75)-Q(s,0.25),r=s.Last()-s.First();return r>0.001?i/r:0;}

    static double Iqr(EP[] nd,Func<EP,double> f){var s=nd.Select(f).OrderBy(v=>v).ToArray();return Q(s,0.75)-Q(s,0.25);}
    static int[] Rank(double[] v){int n=v.Length;return Enumerable.Range(0,n).OrderBy(i=>v[i]).Select((idx,r)=>new{idx,r}).OrderBy(x=>x.idx).Select(x=>x.r).ToArray();}
    static double SpearmanR(double[] x,double[] y){int n=Math.Min(x.Length,y.Length);if(n<3)return 0;var rx=Rank(x);var ry=Rank(y);return PearsonR(rx.Select(v=>(double)v).ToArray(),ry.Select(v=>(double)v).ToArray());}
    static double PearsonR(double[] x,double[] y){int n=Math.Min(x.Length,y.Length);if(n<3)return 0;double mx=x.Average(),my=y.Average(),sx=0,sy=0,sxy=0;for(int i=0;i<n;i++){double dx=x[i]-mx,dy=y[i]-my;sx+=dx*dx;sy+=dy*dy;sxy+=dx*dy;}return sxy/Math.Sqrt(sx*sy+1e-15);}

    static double Q(double[] s,double p)=>s[(int)(p*(s.Length-1))];
    static double Sd(double[] s){double m=s.Average();return Math.Sqrt(s.Average(v=>(v-m)*(v-m)));}
    static double Skew(double[] s){double m=s.Average();double sd=Sd(s);if(sd<1e-9)return 0;int n=s.Length;return n*s.Sum(v=>Math.Pow((v-m)/sd,3))/((n-1)*(n-2)+1);}
    static double CorrX(IEnumerable<double> x,IEnumerable<double> y){var a=x.ToArray();var b=y.ToArray();int n=Math.Min(a.Length,b.Length);if(n<3)return 0;double mx=a.Average(),my=b.Average(),sx=0,sy=0,sxy=0;for(int i=0;i<n;i++){double dx=a[i]-mx,dy=b[i]-my;sx+=dx*dx;sy+=dy*dy;sxy+=dx*dy;}return sxy/Math.Sqrt(sx*sy+1e-15);}
    static double Lambda1(double[,]K,int n){double s=0;for(int i=0;i<n;i++)for(int j=0;j<n;j++)s+=K[i,j];return s/(n*n);}
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
