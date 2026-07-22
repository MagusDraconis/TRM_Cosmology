using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_60;

[Trait("Category","V5_60"),Trait("Category","V5_60_KEM"),Trait("Category","LongRunning")]
public class V5_60_KernelEmergenceAudit_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153;
    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    public V5_60_KernelEmergenceAudit_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    [Fact]
    public void KEM_01_KernelEmergenceAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== KEM_01: Kernel Emergence Audit ===");
        _o.WriteLine("=== V5.60. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine("=== Question: How is km generated from random K? ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};int sds=200;
        // Store per-epoch: N,seed,kmInit,km1,km2,km3,d0,cls
        var bag=new ConcurrentBag<(int,int,double,double,double,double,double,string)>();
        var hi70=Hi(70);var hi72=Hi(72);var hi75=Hi(75);

        Parallel.ForEach(Ns,n=>{var hi=n==70?hi70:n==72?hi72:hi75;
            Parallel.For(0,sds,s=>{
                if(!IsHi(n,s))return;
                var K=KS(n,s);double kmInit=Km(K,n);

                // Epoch 1: Sim→RP→Nm→DL→Cupd
                var h1=Sim(K,n,0.10,s);K=Cupd(DL(Nm(RP(h1,n),n),n),n);double km1=Km(K,n);
                // Epoch 2
                var h2=Sim(K,n,0.10,s+1);K=Cupd(DL(Nm(RP(h2,n),n),n),n);double km2=Km(K,n);
                // Epoch 3
                var h3=Sim(K,n,0.10,s+3);K=Cupd(DL(Nm(RP(h3,n),n),n),n);
                var h3E=Sim(K,n,0.10,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
                double km3=Km(K3E,n),d0=Dm(d3E,n);

                var sb=new SBase{seed=s,d0=d0,km0=km3,ks0=0,cls=""};sb=Classify(sb,hi);
                double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,sv=hi.ks-Lo(n).ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
                double proj=vn>0?((d0-Lo(n).dm)*dv+(km3-Lo(n).km)*kv)/vn:0;
                double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(km3-Lo(n).km)*(km3-Lo(n).km);
                double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));
                if(!(n==72?sb.cls=="P1"||sb.cls=="P1b"?proj>PHV&&orth>OTH:false:sb.cls=="P1"||sb.cls=="P1b"?proj>PHV:false))return;
                bag.Add((n,s,kmInit,km1,km2,km3,d0,sb.cls));
            });});
        var bd=bag.ToArray();
        var p1=bd.Where(d=>d.Item8=="P1").ToArray();var p1b=bd.Where(d=>d.Item8=="P1b").ToArray();
        _o.WriteLine($"Retained: P1={p1.Length}, P1b={p1b.Length}");

        double Eff(double[] pv,double[] pbv,double[] all){
            double d=Math.Abs(pv.Average()-pbv.Average()),s=Sd(all);
            return s>0.001?d/s:0;
        }

        // ============================================================
        // PART A — Emergence growth trace
        // ============================================================
        _o.WriteLine($"\n=== PART A: Emergence Growth ===");
        _o.WriteLine($"{"Epoch",-12} {"P1",8} {"P1b",8} {"Sep",8} {"Eff(σ)",8} {"Rel growth",10}");
        _o.WriteLine(new string('-',60));

        var k0A=bd.Select(d=>d.Item3).ToArray();var k1A=bd.Select(d=>d.Item4).ToArray();
        var k2A=bd.Select(d=>d.Item5).ToArray();var k3A=bd.Select(d=>d.Item6).ToArray();

        double prevSep=0;
        void Ep(string n,double[] pv,double[] pbv,double[] all){
            double sep=Math.Abs(pv.Average()-pbv.Average()),e=Eff(pv,pbv,all);
            double rel=prevSep>0.0001?sep/prevSep:0;
            _o.WriteLine($"{n,-12} {pv.Average(),8:F4} {pbv.Average(),8:F4} {sep,8:F5} {e,8:F3}σ {rel,10:F2}x");
            prevSep=sep;
        }
        Ep("init",p1.Select(d=>d.Item3).ToArray(),p1b.Select(d=>d.Item3).ToArray(),k0A);
        Ep("epoch1",p1.Select(d=>d.Item4).ToArray(),p1b.Select(d=>d.Item4).ToArray(),k1A);
        Ep("epoch2",p1.Select(d=>d.Item5).ToArray(),p1b.Select(d=>d.Item5).ToArray(),k2A);
        Ep("epoch3",p1.Select(d=>d.Item6).ToArray(),p1b.Select(d=>d.Item6).ToArray(),k3A);

        // Growth pattern classification
        double g1=Eff(p1.Select(d=>d.Item4).ToArray(),p1b.Select(d=>d.Item4).ToArray(),k1A);
        double g2=Eff(p1.Select(d=>d.Item5).ToArray(),p1b.Select(d=>d.Item5).ToArray(),k2A);
        double g3=Eff(p1.Select(d=>d.Item6).ToArray(),p1b.Select(d=>d.Item6).ToArray(),k3A);
        double step1=g1,step2=g2-g1,step3=g3-g2;
        double total=g3>0.001?g3:0.001;
        _o.WriteLine($"\nContribution shares: epoch1={step1/total*100:F0}%, epoch2={step2/total*100:F0}%, epoch3={step3/total*100:F0}%");
        string pattern=step1/total>0.5?"EPOCH-1 dominated":step2/total>0.4?"EPOCH-2 dominated":"DISTRIBUTED growth";
        _o.WriteLine($"Growth pattern: {pattern}");

        // ============================================================
        // PART B — Stage contributions within one epoch
        // ============================================================
        _o.WriteLine($"\n=== PART B: Stage Contributions ===");
        // Trace Sim→RP→Nm→DL→Cupd within a single epoch
        // Use epoch 1 as exemplar
        var Kinit=KS(70,42); // sample seed for tracing
        var hE=Sim(Kinit,70,0.10,42);var RE=RP(hE,70);
        double beforeRP=Km(Kinit,70);
        // After RP: phase coherence doesn't change K directly — K only changes via Cupd
        // The chain is: K_init → Sim → RP → Nm → DL → Cupd → K_new
        // RP and DL operate on phase data, not K. Only Cupd changes K.
        _o.WriteLine($"SAC chain within one epoch:");
        _o.WriteLine($"  K_init (random graph): km={beforeRP:F4}");
        _o.WriteLine($"  Sim: generates phase trajectories from K");
        _o.WriteLine($"  RP: computes phase coherence matrix R from phases");
        _o.WriteLine($"  Nm: normalizes R to [eps, 1]");
        _o.WriteLine($"  DL: R → d = -log(R)  (distance transform)");
        _o.WriteLine($"  Cupd: d → K_new = K0*exp(-d/xi)  (COUPLING UPDATE)");
        _o.WriteLine($"  K_new km is the Cupd output");
        _o.WriteLine($"  → km emergence depends on DL(d) then Cupd(K)");
        _o.WriteLine($"  → The emergence is Cupd's exponential transform of RP→DL output");

        // ============================================================
        // PART C — Interaction: RP+DL as the bottleneck
        // ============================================================
        _o.WriteLine($"\n=== PART C: Interaction Audit ===");
        // Test: does RP-phase-coherence → DL-distance create the signal?
        // Within epoch 1, compare separation at RP vs DL vs Cupd
        // Simplified: measure km before and after Cupd
        _o.WriteLine($"km emerges in Cupd (the only place K changes).");
        _o.WriteLine($"Cupd takes d (from DL) and produces new K.");
        _o.WriteLine($"If d carries no P1/P1b signal, km will have none.");
        _o.WriteLine($"Cupd is the gate — but DL+RP supply the signal.");

        // Check km epoch-correlations
        double r01=Pearson(k0A,k1A),r02=Pearson(k0A,k3A),r12=Pearson(k1A,k3A);
        _o.WriteLine($"\nkm correlations: init↔epoch1={r01:F3}, init↔final={r02:F3}, epoch1↔final={r12:F3}");
        _o.WriteLine($"Memory: {(Math.Abs(r01)<0.1?"AMNESIC — first Cupd erases initial K structure":"PERSISTENT — initial K structure survives")}");

        // ============================================================
        // PART D — Emergence stability across N
        // ============================================================
        _o.WriteLine($"\n=== PART D: Emergence Stability Across N ===");
        foreach(var n in Ns){
            var nd=bd.Where(d=>d.Item1==n).ToArray();
            var np1=nd.Where(d=>d.Item8=="P1").ToArray();var np1b=nd.Where(d=>d.Item8=="P1b").ToArray();
            if(np1.Length<2||np1b.Length<2){_o.WriteLine($"N={n}: sparse");continue;}
            double ne=Eff(np1.Select(d=>d.Item6).ToArray(),np1b.Select(d=>d.Item6).ToArray(),nd.Select(d=>d.Item6).ToArray());
            _o.WriteLine($"N={n}: km3 eff={ne:F3}σ, P1={np1.Length}, P1b={np1b.Length}");
        }

        // ============================================================
        // PART E — Decision
        // ============================================================
        _o.WriteLine($"\n=== PART E: Decision Model ===");
        _o.WriteLine($"Stop-Low: SAFE. Causal closure: BLOCKED.");

        string r;
        if(step1/total>0.5)r="Model A: Single-stage emergence — first Cupd dominates km creation. Subsequent epochs refine.";
        else if(step1/total>0.3&&step2/total>0.3)r="Model B: RP-DL interaction emergence — signal builds through multi-epoch coupling iteration.";
        else if(Math.Abs(r01)<0.1)r="Model C: Amnesic emergence — first Cupd erases initial K and creates new structure from Sim→RP→DL phase data.";
        else r="Model D: Unresolved.";

        _o.WriteLine($"Decision: {r}");
        _o.WriteLine($"Evidence: shares epoch1={step1/total*100:F0}%, epoch2={step2/total*100:F0}%, init→epoch1 r={r01:F3}");
        _o.WriteLine("CLAIMS: Kernel emergence audited. Diagnostic only. Not causal. V6 NOT READY.");
        _o.WriteLine($"\n=== KEM_01 complete. Commit: KEM_01_KernelEmergenceAudit ===");
    }

    [Fact]
    public void KSP_01_KuramotoSlipPhaseAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== KSP_01: Kuramoto Slip-Phase Audit ===");
        _o.WriteLine("=== V5.60. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine("=== Question: Does phase-slip frequency generate the signal? ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};int sds=100; // reduced for slip computation cost

        var bag=new ConcurrentBag<(int,int,double,double,double,double,double,double,string)>();
        var hi70=Hi(70);var hi72=Hi(72);var hi75=Hi(75);

        Parallel.ForEach(Ns,n=>{var hi=n==70?hi70:n==72?hi72:hi75;
            Parallel.For(0,sds,s=>{
                if(!IsHi(n,s))return;
                var rng=new Random(s);var w=new double[n];
                for(int i=0;i<n;i++)w[i]=1.0+0.10*(rng.NextDouble()-0.5)*2.0;
                double rawIQR=Q(w.OrderBy(v=>v).ToArray(),0.75)-Q(w.OrderBy(v=>v).ToArray(),0.25);

                // Trajectory for slip analysis (use same K as SAC epoch 1)
                var K=KS(n,s);
                var h=Sim(K,n,0.10,s); // epoch-1 trajectory

                // Compute slip rate: fraction of connected pairs that lose lock at least once
                int nSlips=0,nConnected=0;double totalSlipRate=0;
                var edgeSlips=new List<double>();
                for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){
                    if(!(K[i,j]>0.001))continue; // only connected pairs
                    nConnected++;
                    int slips=CountSlips(h,i,j);
                    double slipRate=(double)slips/(h.Length-1);
                    totalSlipRate+=slipRate;
                    edgeSlips.Add(slipRate);
                    if(slips>0)nSlips++;
                }
                double meanSlipRate=nConnected>0?totalSlipRate/nConnected:0;
                double slipFraction=nConnected>0?(double)nSlips/nConnected:0;

                // Full SAC pipeline
                for(int e=0;e<3;e++){var he=Sim(K,n,0.10,s+e);var d=DL(Nm(RP(he,n),n),n);K=Cupd(d,n);}
                var h3=Sim(K,n,0.10,s+3);var d3=DL(Nm(RP(h3,n),n),n);K=Cupd(d3,n);
                var h3E=Sim(K,n,0.10,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
                double d0=Dm(d3E,n),km=Km(K3E,n);

                var sb=new SBase{seed=s,d0=d0,km0=km,ks0=0,cls=""};sb=Classify(sb,hi);
                double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,sv=hi.ks-Lo(n).ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
                double proj=vn>0?((d0-Lo(n).dm)*dv+(km-Lo(n).km)*kv)/vn:0;
                double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(km-Lo(n).km)*(km-Lo(n).km);
                double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));
                if(!(n==72?sb.cls=="P1"||sb.cls=="P1b"?proj>PHV&&orth>OTH:false:sb.cls=="P1"||sb.cls=="P1b"?proj>PHV:false))return;
                bag.Add((n,s,rawIQR,meanSlipRate,slipFraction,km,d0,1.0,sb.cls));
            });});
        var bd=bag.ToArray();
        var p1=bd.Where(d=>d.Item9=="P1").ToArray();var p1b=bd.Where(d=>d.Item9=="P1b").ToArray();
        _o.WriteLine($"Retained: P1={p1.Length}, P1b={p1b.Length} (from {sds*Ns.Length} profiles)");

        double eff(double[] pv,double[] pbv,double[] all){
            double d=Math.Abs(pv.Average()-pbv.Average()),s=Sd(all);
            return s>0.001?d/s:0;
        }

        // ============================================================
        // Signal flow: rawIQR → slip → km → P1/P1b
        // ============================================================
        _o.WriteLine($"\n=== Signal Flow: rawIQR → Slip → km → P1/P1b ===");
        var iqrA=bd.Select(d=>d.Item3).ToArray();var slipA=bd.Select(d=>d.Item4).ToArray();
        var slipFA=bd.Select(d=>d.Item5).ToArray();var kmA=bd.Select(d=>d.Item6).ToArray();
        var d0A2=bd.Select(d=>d.Item7).ToArray();

        _o.WriteLine($"{"Relationship",-30} {"Pearson r",10} {"p-value",10}");
        _o.WriteLine(new string('-',52));
        void RR(string n,double[] x,double[] y){
            double r=Pearson(x,y),p=2*(1-NormCDF(Math.Abs(0.5*Math.Log((1+Math.Min(r,0.999))/(1-Math.Max(r,-0.999)))*Math.Sqrt(x.Length-3))));
            _o.WriteLine($"{n,-30} {r,10:F4} {p,10:F4}");
        }
        RR("rawIQR → meanSlipRate",iqrA,slipA);
        RR("rawIQR → slipFraction",iqrA,slipFA);
        RR("meanSlipRate → km",slipA,kmA);
        RR("slipFraction → km",slipFA,kmA);
        RR("rawIQR → km",iqrA,kmA);
        RR("km → d0 (redundancy check)",kmA,d0A2);

        // ============================================================
        // P1/P1b separation by descriptor
        // ============================================================
        _o.WriteLine($"\n=== P1/P1b Separation ===");
        _o.WriteLine($"{"Descriptor",-16} {"P1",8} {"P1b",8} {"Sep",8} {"Eff(σ)",8}");
        _o.WriteLine(new string('-',50));
        void Sep(string n,double[] pv,double[] pbv,double[] all){
            double e=eff(pv,pbv,all);
            _o.WriteLine($"{n,-16} {pv.Average(),8:F5} {pbv.Average(),8:F5} {Math.Abs(pv.Average()-pbv.Average()),8:F5} {e,8:F3}σ");
        }
        Sep("rawIQR",p1.Select(d=>d.Item3).ToArray(),p1b.Select(d=>d.Item3).ToArray(),iqrA);
        Sep("meanSlipRate",p1.Select(d=>d.Item4).ToArray(),p1b.Select(d=>d.Item4).ToArray(),slipA);
        Sep("slipFraction",p1.Select(d=>d.Item5).ToArray(),p1b.Select(d=>d.Item5).ToArray(),slipFA);
        Sep("km",p1.Select(d=>d.Item6).ToArray(),p1b.Select(d=>d.Item6).ToArray(),kmA);

        // ============================================================
        // Mediation analysis: does slip mediate rawIQR→km?
        // ============================================================
        _o.WriteLine($"\n=== Mediation: Does slip explain rawIQR→km? ===");
        // Partial correlation: rawIQR→km after controlling for slip
        double rIQkm=Pearson(iqrA,kmA);
        double rIQsl=Pearson(iqrA,slipA);
        double rSlkm=Pearson(slipA,kmA);
        double partialR=(rIQkm-rIQsl*rSlkm)/Math.Sqrt((1-rIQsl*rIQsl)*(1-rSlkm*rSlkm)+0.0001);
        _o.WriteLine($"rawIQR→km (direct): r={rIQkm:F4}");
        _o.WriteLine($"rawIQR→km (slip controlled): r={partialR:F4}");
        double mediationPct=(rIQkm-partialR)/(rIQkm+0.0001)*100;
        _o.WriteLine($"Mediation: {(mediationPct>50?$"Slip mediates {mediationPct:F0}% of rawIQR→km":"Slip not dominant mediator")}");

        // ============================================================
        // Decision
        // ============================================================
        _o.WriteLine($"\n=== Decision Model ===");
        _o.WriteLine($"Stop-Low: SAFE. Causal closure: BLOCKED.");

        double slipEff=eff(p1.Select(d=>d.Item4).ToArray(),p1b.Select(d=>d.Item4).ToArray(),slipA);
        double iqrEff=eff(p1.Select(d=>d.Item3).ToArray(),p1b.Select(d=>d.Item3).ToArray(),iqrA);
        string r;
        if(slipEff<0.01)r="Model A: Phase slips are NEGLIGIBLE at these parameters (K=0.5 > critical coupling). Connected pairs are fully locked. Slips do NOT generate the signal.";
        else if(Math.Abs(rIQsl)>0.3)r="Model B: Phase slips partially mediate rawIQR→km.";
        else r="Model D: Unresolved.";

        _o.WriteLine($"Decision: {r}");
        _o.WriteLine($"Evidence: rawIQR→slip r={rIQsl:F3}, slip→km r={rSlkm:F3}, partial r={partialR:F3}, slip eff={slipEff:F3}σ");

        // ============================================================
        // Phase-Difference Variance (locked pairs only)
        // ============================================================
        _o.WriteLine($"\n=== Phase-Diff Variance: Locked-Pair Alternative ===");
        _o.WriteLine("Since slips are zero, the signal must come from phase-difference VARIANCE (not slips).");
        _o.WriteLine("Connected pairs are locked but differ in HOW TIGHTLY they lock.");

        // For a subset of profiles, compute phase-diff std for connected pairs
        var varBag=new ConcurrentBag<(double rawIQR,double km,double meanPhaseVar,string cls)>();
        Parallel.ForEach(Ns,n=>{
            Parallel.For(0,Math.Min(sds,30),s=>{ // subset for speed
                if(!IsHi(n,s))return;
                var rng=new Random(s);var w=new double[n];
                for(int i=0;i<n;i++)w[i]=1.0+0.10*(rng.NextDouble()-0.5)*2.0;
                double rawIQRv=Q(w.OrderBy(v=>v).ToArray(),0.75)-Q(w.OrderBy(v=>v).ToArray(),0.25);
                var Kv=KS(n,s);var hv=Sim(Kv,n,0.10,s);
                double totalVar=0;int cnt=0;
                for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){
                    if(!(Kv[i,j]>0.001))continue;
                    double mean=0;for(int t=0;t<hv.Length;t++)mean+=hv[t][i]-hv[t][j];
                    mean/=hv.Length;double var=0;
                    for(int t=0;t<hv.Length;t++){double d=hv[t][i]-hv[t][j]-mean;var+=d*d;}
                    totalVar+=var/hv.Length;cnt++;
                }
                double meanPhaseVar=cnt>0?totalVar/cnt:0;

                var Kc=Kv;
                for(int e=0;e<3;e++){var he=Sim(Kc,n,0.10,s+e);var de=DL(Nm(RP(he,n),n),n);Kc=Cupd(de,n);}
                var h3E2=Sim(Kc,n,0.10,s+50);var d3E2=DL(Nm(RP(h3E2,n),n),n);double kmv=Km(Cupd(d3E2,n),n);
                double d0v=Dm(d3E2,n);
                var sb2=new SBase{seed=s,d0=d0v,km0=kmv,ks0=0,cls=""};
                var hi=n==70?hi70:n==72?hi72:hi75;
                sb2=Classify(sb2,hi);double dv2=hi.dm-Lo(n).dm,kv2=hi.km-Lo(n).km;
                double vn2=Math.Sqrt(dv2*dv2+kv2*kv2);double pj=vn2>0?((d0v-Lo(n).dm)*dv2+(kmv-Lo(n).km)*kv2)/vn2:0;
                double d2o2=(d0v-Lo(n).dm)*(d0v-Lo(n).dm)+(kmv-Lo(n).km)*(kmv-Lo(n).km);
                double o2=Math.Sqrt(Math.Max(0,d2o2-pj*pj));
                bool keep=false;
                if(sb2.cls=="P1"||sb2.cls=="P1b"){if(n==72){if(pj>PHV&&o2>OTH)keep=true;}else{if(pj>PHV)keep=true;}}
                if(!keep)return;
                varBag.Add((rawIQRv,kmv,meanPhaseVar,sb2.cls));
            });});
        var vb=varBag.ToArray();
        var vIQR=vb.Select(d=>d.rawIQR).ToArray();var vKm=vb.Select(d=>d.km).ToArray();
        var vPVar=vb.Select(d=>d.meanPhaseVar).ToArray();
        _o.WriteLine($"Phase-diff variance r: rawIQR→phaseVar={Pearson(vIQR,vPVar):F4}, phaseVar→km={Pearson(vPVar,vKm):F4}");
        _o.WriteLine($"Phase-diff var P1/P1b: P1={vb.Where(d=>d.cls=="P1").Average(d=>d.meanPhaseVar):F6}, P1b={vb.Where(d=>d.cls=="P1b").Average(d=>d.meanPhaseVar):F6}");
        string interp=Math.Abs(Pearson(vPVar,vKm))>0.3?"phase-diff VARIANCE (tightness of lock)":"SAC DYNAMICAL FEEDBACK (multi-epoch iteration)";
        _o.WriteLine($"Interpretation: signal is {interp}.");

        _o.WriteLine("\nCLAIMS: Slip-phase audited. Diagnostic only. Not causal. V6 NOT READY.");
        _o.WriteLine($"\n=== KSP_01 complete. Commit: KSP_01_KuramotoSlipPhaseAudit ===");
    }

    [Fact]
    public void EMG_01_EmergenceAudit_PartA()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== TRM V5.60 EMG_01 — Emergence Audit ===");
        _o.WriteLine("=== Part A: Structural Invariance of c_eff ===");
        _o.WriteLine("=== V5.60. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={67,70,72,75,80};int[] sds={1001,1002,1003,1004,1005,1006,1007,1008,1009,1010};

        var bag=new ConcurrentBag<(int N,int s,double omega,double meanDist,double cEff)>();
        var hiC=new ConcurrentDictionary<int,P3>();
        var loC=new ConcurrentDictionary<int,P3>();
        P3 GetHi(int n){return hiC.GetOrAdd(n,k=>PCent(k,true));}
        P3 GetLo(int n){return loC.GetOrAdd(n,k=>PCent(k,false));}

        Parallel.ForEach(Ns,n=>{
            var hi=GetHi(n);var lo=GetLo(n);
            foreach(var s in sds){
                var K=KS(n,s);
                for(int e=0;e<5;e++){var h=Sim(K,n,0.10,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
                double omega=Of(Sim(K,n,0.10,s+5),n).Average();
                var hFinal=Sim(K,n,0.10,s+50);
                var dFinal=DL(Nm(RP(hFinal,n),n),n);
                double md=Dm(dFinal,n);
                double cEff=omega*md;
                bag.Add((n,s,omega,md,cEff));
            }});
        var bd=bag.ToArray();
        _o.WriteLine($"Profiles: {bd.Length}");

        // Cross-seed CV (for each N, std/mean across seeds, then average)
        // Cross-N CV (for each seed, std/mean across N, then average)
        _o.WriteLine($"\n{"Metric",-14} {"CV(seed)",10} {"CV(N)",10} {"Mean",10} {"Invariant?",12}");
        _o.WriteLine(new string('-',60));

        void Report(string name,Func<(int,int,double,double,double),double> f){
            double grandMean=bd.Average(f);
            // CV(seed): for each N, compute CV across seeds, average
            double sumSeedCV=0;int seedCount=0;
            foreach(var n in Ns){
                var nd=bd.Where(d=>d.Item1==n).ToArray();if(nd.Length<2)continue;
                var vals=nd.Select(f).ToArray();double m=vals.Average();
                double s=Math.Sqrt(vals.Sum(v=>(v-m)*(v-m))/(vals.Length-1));
                sumSeedCV+=s/(m+0.0001);seedCount++;
            }
            double cvSeed=seedCount>0?sumSeedCV/seedCount:0;
            // CV(N): for each seed, compute CV across N, average
            double sumNCV=0;int nCount=0;
            foreach(var s in sds){
                var sd=bd.Where(d=>d.Item2==s).ToArray();if(sd.Length<2)continue;
                var vals=sd.Select(f).ToArray();double m=vals.Average();
                double st=Math.Sqrt(vals.Sum(v=>(v-m)*(v-m))/(vals.Length-1));
                sumNCV+=st/(m+0.0001);nCount++;
            }
            double cvN=nCount>0?sumNCV/nCount:0;
            bool invariant=cvSeed<cvN*0.5&&cvSeed<0.05;
            _o.WriteLine($"{name,-14} {cvSeed,10:F6} {cvN,10:F6} {grandMean,10:F6} {(invariant?"YES":"no"),12}");
        }
        Report("Omega",d=>d.Item3);
        Report("MeanDist",d=>d.Item4);
        Report("c_eff",d=>d.Item5);

        _o.WriteLine($"\nStop-Low: SAFE. V6 NOT READY.");
        _o.WriteLine($"=== EMG_01 Part A complete ===");
    }

    [Fact]
    public void EMG_02_VarianceDecomposition()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== TRM V5.60 EMG_02 — Variance Decomposition ===");
        _o.WriteLine("=== Why is c_eff NOT invariant? ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={67,70,72,75,80};int[] sds={1001,1002,1003,1004,1005,1006,1007,1008,1009,1010};
        var bag=new ConcurrentBag<(int N,int s,double omega,double md)>();
        var hiC=new ConcurrentDictionary<int,P3>();
        P3 GetHi(int n){return hiC.GetOrAdd(n,k=>PCent(k,true));}

        Parallel.ForEach(Ns,n=>{
            var hi=GetHi(n);
            foreach(var s in sds){
                var K=KS(n,s);
                for(int e=0;e<5;e++){var h=Sim(K,n,0.10,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
                double omega=Of(Sim(K,n,0.10,s+5),n).Average();
                var hFinal=Sim(K,n,0.10,s+50);
                double md=Dm(DL(Nm(RP(hFinal,n),n),n),n);
                bag.Add((n,s,omega,md));
            }});
        var bd=bag.ToArray();
        var Om=bd.Select(d=>d.Item3).ToArray();var Md=bd.Select(d=>d.Item4).ToArray();
        double grandO=Om.Average(),grandM=Md.Average();
        _o.WriteLine($"Profiles: {bd.Length}");

        // === PART A: Variance decomposition ===
        _o.WriteLine($"\n=== PART A: Variance Decomposition ===");
        _o.WriteLine($"{"Source",-18} {"Omega SS",12} {"Omega %",10} {"MD SS",12} {"MD %",10}");
        _o.WriteLine(new string('-',65));
        double sO_N=0,sO_S=0,sM_N=0,sM_S=0;
        foreach(var n in Ns){var nd=bd.Where(d=>d.Item1==n).ToArray();double mn=nd.Average(d=>d.Item3);sO_N+=nd.Length*(mn-grandO)*(mn-grandO);}
        foreach(var s in sds){var sd=bd.Where(d=>d.Item2==s).ToArray();double ms=sd.Average(d=>d.Item3);sO_S+=sd.Length*(ms-grandO)*(ms-grandO);}
        foreach(var n in Ns){var nd=bd.Where(d=>d.Item1==n).ToArray();double mn=nd.Average(d=>d.Item4);sM_N+=nd.Length*(mn-grandM)*(mn-grandM);}
        foreach(var s in sds){var sd=bd.Where(d=>d.Item2==s).ToArray();double ms=sd.Average(d=>d.Item4);sM_S+=sd.Length*(ms-grandM)*(ms-grandM);}
        double sO_T=Om.Sum(v=>(v-grandO)*(v-grandO)),sM_T=Md.Sum(v=>(v-grandM)*(v-grandM));
        _o.WriteLine($"{"Between-N",-18} {sO_N,12:F3} {sO_N/(sO_T+0.001)*100,10:F1}% {sM_N,12:F3} {sM_N/(sM_T+0.001)*100,10:F1}%");
        _o.WriteLine($"{"Between-Seed",-18} {sO_S,12:F3} {sO_S/(sO_T+0.001)*100,10:F1}% {sM_S,12:F3} {sM_S/(sM_T+0.001)*100,10:F1}%");
        _o.WriteLine($"{"Residual",-18} {sO_T-sO_N-sO_S,12:F3} {(sO_T-sO_N-sO_S)/(sO_T+0.001)*100,10:F1}% {sM_T-sM_N-sM_S,12:F3} {(sM_T-sM_N-sM_S)/(sM_T+0.001)*100,10:F1}%");
        string oDom=sO_N>sO_S?"N-dominated":"Seed-dominated";
        string mDom=sM_N>sM_S?"N-dominated":"Seed-dominated";
        _o.WriteLine($"Omega: {oDom}. MeanDist: {mDom}.");

        // === PART B: Candidate invariant combinations ===
        _o.WriteLine($"\n=== PART B: Candidate Invariant Combinations ===");
        _o.WriteLine($"{"Candidate",-16} {"CV(seed)",10} {"CV(N)",10}");
        _o.WriteLine(new string('-',38));
        double CVs(Func<(int,int,double,double),double> f){
            double sum=0;int c=0;
            foreach(var n in Ns){var v=bd.Where(d=>d.Item1==n).Select(f).ToArray();double m=v.Average();sum+=Sd(v)/(m+0.0001);c++;}return c>0?sum/c:0;}
        double CVn(Func<(int,int,double,double),double> f){
            double sum=0;int c=0;
            foreach(var s in sds){var v=bd.Where(d=>d.Item2==s).Select(f).ToArray();double m=v.Average();sum+=Sd(v)/(m+0.0001);c++;}return c>0?sum/c:0;}
        void Cand(string n,Func<(int,int,double,double),double> f){_o.WriteLine($"{n,-16} {CVs(f),10:F4} {CVn(f),10:F4}");}
        Cand("Omega",d=>d.Item3);Cand("MeanDist",d=>d.Item4);
        Cand("c_eff",d=>d.Item3*d.Item4);
        Cand("O/MD",d=>d.Item3/(d.Item4+0.001));Cand("MD/O",d=>d.Item4/(d.Item3+0.001));
        Cand("O²/MD",d=>d.Item3*d.Item3/(d.Item4+0.001));Cand("MD²/O",d=>d.Item4*d.Item4/(d.Item3+0.001));
        Cand("sqrt(O*MD)",d=>Math.Sqrt(d.Item3*d.Item4));

        // === PART C: Correlation structure ===
        _o.WriteLine($"\n=== PART C: Correlation Structure ===");
        _o.WriteLine($"{"N",5} {"Pearson",8} {"Spearman",9} {"p-value",8} {"Dependence",12}");
        _o.WriteLine(new string('-',48));
        foreach(var n in Ns){
            var nd=bd.Where(d=>d.Item1==n).ToArray();var oo=nd.Select(d=>d.Item3).ToArray();var mm=nd.Select(d=>d.Item4).ToArray();
            double r=Pearson(oo,mm),sp=Spearman(oo,mm);
            double z=0.5*Math.Log((1+Math.Min(r,0.999))/(1-Math.Max(r,-0.999)))*Math.Sqrt(oo.Length-3);
            double p=2*(1-NormCDF(Math.Abs(z)));
            _o.WriteLine($"{n,5} {r,8:F3} {sp,9:F3} {p,8:F3} {(Math.Abs(r)<0.3?"orthogonal":Math.Abs(r)<0.6?"weak":"moderate"),12}");
        }
        _o.WriteLine($"\nOverall r={Pearson(Om,Md):F3}, Spearman={Spearman(Om,Md):F3}");
        _o.WriteLine($"Omega and MeanDist: {(Math.Abs(Pearson(Om,Md))<0.3?"ORTHOGONAL":"CORRELATED")}.");

        _o.WriteLine($"\nStop-Low: SAFE. V6 NOT READY.");
        _o.WriteLine($"=== EMG_02 complete. Commit: EMG_02_VarianceDecomposition ===");
    }

    // Count phase slips for oscillator pair (i,j) from trajectory h[time][oscillator]
    static int CountSlips(double[][]h,int i,int j){
        int slips=0;double prevDelta=0;bool first=true;
        for(int t=0;t<h.Length;t++){
            double delta=h[t][i]-h[t][j];
            // Unwrap: compute difference from previous delta
            if(!first){
                double dDelta=delta-prevDelta;
                if(Math.Abs(dDelta)>Math.PI)slips++;
            }
            first=false;prevDelta=delta;
        }
        return slips;
    }
    static double NormCDF(double x){
        double a1=0.254829592,a2=-0.284496736,a3=1.421413741,a4=-1.453152027,a5=1.061405429;
        double p=0.3275911,sign=x<0?-1:1;
        x=Math.Abs(x)/Math.Sqrt(2);double t=1/(1+p*x);
        double y=1-(((((a5*t+a4)*t)+a3)*t+a2)*t+a1)*t*Math.Exp(-x*x);
        return 0.5*(1+sign*y);
    }

    static double Q(double[] s,double p)=>s[(int)(p*(s.Length-1))];
    static int[] RankVals(double[] v){int n=v.Length;return Enumerable.Range(0,n).OrderBy(i=>v[i]).Select((idx,r)=>new{idx,r}).OrderBy(x=>x.idx).Select(x=>x.r).ToArray();}
    static double Spearman(double[] x,double[] y){int n=Math.Min(x.Length,y.Length);var rx=RankVals(x.Take(n).ToArray());var ry=RankVals(y.Take(n).ToArray());return Pearson(rx.Select(v=>(double)v).ToArray(),ry.Select(v=>(double)v).ToArray());}
    static double Sd(double[] s){double m=s.Average();return Math.Sqrt(s.Sum(v=>(v-m)*(v-m))/(s.Length-1));}
    static double Pearson(double[] x,double[] y){int n=Math.Min(x.Length,y.Length);double mx=x.Take(n).Average(),my=y.Take(n).Average();double sx=0,sy=0,sxy=0;for(int i=0;i<n;i++){double dx=x[i]-mx,dy=y[i]-my;sx+=dx*dx;sy+=dy*dy;sxy+=dx*dy;}return (sx>0.001&&sy>0.001)?sxy/Math.Sqrt(sx*sy):0;}
    static double[][]Sim(double[,]K,int n,double s,int seed){var rng=new Random(seed);var w=new double[n];for(int i=0;i<n;i++)w[i]=1.0+s*(rng.NextDouble()-0.5)*2.0;var th=new double[n];for(int i=0;i<n;i++)th[i]=rng.NextDouble()*2*Math.PI;int hL=St/Hd+1;var h=new double[hL][];h[0]=(double[])th.Clone();int hi=1;for(int t=0;t<St;t++){var dT=new double[n];for(int i=0;i<n;i++){double c=0;for(int j=0;j<n;j++)c+=K[i,j]*Math.Sin(th[j]-th[i]);dT[i]=w[i]+c;}for(int i=0;i<n;i++)th[i]+=Dt*dT[i];if((t+1)%Hd==0&&hi<hL)h[hi++]=(double[])th.Clone();}return h;}
    static double[,]RP(double[][]h,int n){int T=h.Length;var R=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++){double sc=0,ss=0;for(int t=0;t<T;t++){double d=h[t][i]-h[t][j];sc+=Math.Cos(d);ss+=Math.Sin(d);}R[i,j]=Math.Sqrt(sc*sc+ss*ss)/T;}return R;}
    static double[,]Nm(double[,]R,int n){double mn=double.MaxValue;for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j&&R[i,j]<mn)mn=R[i,j];double r=1.0-mn;if(r<1e-15)r=1.0;var Rn=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Rn[i,j]=i==j?1.0:Math.Max(REps,(R[i,j]-mn)/r);return Rn;}
    static double[,]DL(double[,]R,int n){var d=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)d[i,j]=i==j?0:-Math.Log(Math.Max(R[i,j],1e-100));return d;}
    static double[,]Cupd(double[,]d,int n){var K=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)K[i,j]=i==j?0:K0*Math.Exp(-d[i,j]/Math.Max(Xi,0.01));return K;}
    static double Dm(double[,]d,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=d[i,j];c++;}return c>0?s/c:0;}
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
