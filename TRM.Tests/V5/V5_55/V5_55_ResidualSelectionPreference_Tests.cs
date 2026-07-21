using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_55;

[Trait("Category","V5_55"),Trait("Category","V5_55_RSP"),Trait("Category","LongRunning")]
public class V5_55_ResidualSelectionPreference_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    public V5_55_ResidualSelectionPreference_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    [Fact]
    public void RSP_01_ResidualSelectionPreferenceAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== RSP_01: Residual Selection Preference Audit ===");
        _o.WriteLine("=== V5.55. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine("=== Question: Why does SAC prefer residual over seed rawIQR? ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};int seeds=300;

        // ============================================================
        // Compute seed-level and profile-level rawIQR
        // ============================================================
        var seedIQR=new ConcurrentDictionary<int,double>();
        var seedMean2=new ConcurrentDictionary<int,double>();
        var allProf=new ConcurrentBag<(int N,int seed,double riqr,double rmean)>();

        Parallel.ForEach(Ns,n=>{Parallel.For(0,seeds,s=>{
            var rng=new Random(s);var w=new double[n];
            for(int i=0;i<n;i++)w[i]=1.0+S*(rng.NextDouble()-0.5)*2.0;
            var wo=w.OrderBy(v=>v).ToArray();
            double ri=Q(wo,0.75)-Q(wo,0.25),rm=wo.Average();
            allProf.Add((n,s,ri,rm));
            seedIQR.AddOrUpdate(s,ri,(_,v)=>v+ri);
            seedMean2.AddOrUpdate(s,rm,(_,v)=>v+rm);
        });});

        var sIQR=seedIQR.ToDictionary(kv=>kv.Key,kv=>kv.Value/Ns.Length);
        var sMean=seedMean2.ToDictionary(kv=>kv.Key,kv=>kv.Value/Ns.Length);

        // ============================================================
        // Run pipeline for P1/P1b outcomes
        // ============================================================
        _o.WriteLine("Running pipeline for outcomes...");
        var pipeBag=new ConcurrentBag<(int N,int seed,double seedIQR,double residIQR,double seedMean,double residMean,string cls)>();
        var hi70=Hi(70);var hi72=Hi(72);var hi75=Hi(75);

        Parallel.ForEach(Ns,n=>{var hi=n==70?hi70:n==72?hi72:hi75;
            Parallel.For(0,seeds,s=>{
                if(!IsHi(n,s))return;var sb=SelectAndClassify(n,s,hi);
                if(sb==null||(sb.Value.cls!="P1"&&sb.Value.cls!="P1b"))return;
                var prof=allProf.FirstOrDefault(p=>p.N==n&&p.seed==s);
                double siQ=sIQR.GetValueOrDefault(s,0),riQ=prof.riqr-siQ;
                double sM=sMean.GetValueOrDefault(s,0),rM=prof.rmean-sM;
                pipeBag.Add((n,s,siQ,riQ,sM,rM,sb.Value.cls));
            });});
        var pd=pipeBag.ToArray();
        _o.WriteLine($"Retained: {pd.Length} (P1={pd.Count(d=>d.cls=="P1")}, P1b={pd.Count(d=>d.cls=="P1b")})");

        // ============================================================
        // PART B — Signal Strength Audit
        // ============================================================
        _o.WriteLine($"\n=== PART B: Signal Strength Audit ===");
        _o.WriteLine($"{"Component",-16} {"P1 mean",10} {"P1b mean",10} {"Delta",10} {"Norm delta",12} {"Dominant?",10}");
        _o.WriteLine(new string('-',72));

        void Eval(string name,Func<(int,int,double,double,double,double,string),double> f){
            var p1=pd.Where(d=>d.cls=="P1").ToArray();var p1b=pd.Where(d=>d.cls=="P1b").ToArray();
            double p1m=p1.Length>0?p1.Average(f):0,p1bm=p1b.Length>0?p1b.Average(f):0;
            double delta=Math.Abs(p1m-p1bm),mx=Math.Max(Math.Abs(p1m),Math.Abs(p1bm));
            double norm=mx>0.001?delta/mx:0;
            _o.WriteLine($"{name,-16} {p1m,10:F5} {p1bm,10:F5} {delta,10:F5} {norm,12:F4}");
        }
        Eval("seed_rawIQR",d=>d.Item3);
        Eval("resid_rawIQR",d=>d.Item4);
        Eval("seed_rawMean",d=>d.Item5);
        Eval("resid_rawMean",d=>d.Item6);
        _o.WriteLine("NOTE: seed_rawIQR range ~0.07-0.13; resid_rawIQR range ~(-0.016,0.012)");

        // ============================================================
        // PART C — Variance Efficiency Audit
        // ============================================================
        _o.WriteLine($"\n=== PART C: Variance Efficiency Audit ===");
        var seedComp=pd.Select(d=>d.Item3).ToArray();
        var residComp=pd.Select(d=>d.Item4).ToArray();
        double seedVar=Sd(seedComp),residVar=Sd(residComp);
        double seedVarianceShare=seedVar*seedVar/(seedVar*seedVar+residVar*residVar)*100;
        double residVarianceShare=100-seedVarianceShare;

        // Discrimination: P1-P1b delta for each component
        double seedDelta=Math.Abs(pd.Where(d=>d.cls=="P1").Average(d=>d.Item3)-pd.Where(d=>d.cls=="P1b").Average(d=>d.Item3));
        double residDelta=Math.Abs(pd.Where(d=>d.cls=="P1").Average(d=>d.Item4)-pd.Where(d=>d.cls=="P1b").Average(d=>d.Item4));
        double seedEff=seedDelta/(seedVar+0.0001),residEff=residDelta/(residVar+0.0001);
        double effRatio=residEff/(seedEff+0.0001);

        _o.WriteLine($"{"Component",-16} {"Var share",10} {"Delta",10} {"Eff(delta/var)",14} {"Efficiency",12}");
        _o.WriteLine(new string('-',55));
        _o.WriteLine($"{"seed_rawIQR",-16} {seedVarianceShare,10:F1}% {seedDelta,10:F5} {seedEff,14:F4}");
        _o.WriteLine($"{"resid_rawIQR",-16} {residVarianceShare,10:F1}% {residDelta,10:F5} {residEff,14:F4}");
        _o.WriteLine($"\nResidual/seed efficiency ratio: {effRatio:F1}x");
        _o.WriteLine($"Residual is {(effRatio>2?"SIGNIFICANTLY":"")} more information-dense per unit variance.");

        // ============================================================
        // PART D — Conditional Residual Audit
        // ============================================================
        _o.WriteLine($"\n=== PART D: Conditional Residual Audit ===");
        double residMed=residComp.OrderBy(v=>v).ToArray()[residComp.Length/2];
        var highRes=pd.Where(d=>d.Item4>residMed).ToArray();
        var lowRes=pd.Where(d=>d.Item4<=residMed).ToArray();
        _o.WriteLine($"High residual (>{residMed:F5}): P1={highRes.Count(d=>d.cls=="P1")}, P1b={highRes.Count(d=>d.cls=="P1b")}, P1%={highRes.Count(d=>d.cls=="P1")*100.0/Math.Max(1,highRes.Length):F0}%");
        _o.WriteLine($"Low residual (<={residMed:F5}): P1={lowRes.Count(d=>d.cls=="P1")}, P1b={lowRes.Count(d=>d.cls=="P1b")}, P1%={lowRes.Count(d=>d.cls=="P1")*100.0/Math.Max(1,lowRes.Length):F0}%");

        // ============================================================
        // PART E — Residual Dominance
        // ============================================================
        _o.WriteLine($"\n=== PART E: Residual Dominance Audit ===");
        // Simple logistic-style comparison: does adding seed information help?
        // Use normalized rank separations
        var allP1=pd.Where(d=>d.cls=="P1").ToArray();var allP1b=pd.Where(d=>d.cls=="P1b").ToArray();
        double sOnly=Math.Abs(allP1.Average(d=>d.Item3)-allP1b.Average(d=>d.Item3));
        double rOnly=Math.Abs(allP1.Average(d=>d.Item4)-allP1b.Average(d=>d.Item4));
        double combined=sOnly+rOnly;
        _o.WriteLine($"Seed-only delta: {sOnly:F5}");
        _o.WriteLine($"Residual-only delta: {rOnly:F5}");
        _o.WriteLine($"Combined delta: {combined:F5}");
        _o.WriteLine($"Residual contribution: {rOnly/(sOnly+rOnly+0.0001)*100:F0}% of total signal");
        _o.WriteLine($"Adding seed {(sOnly>rOnly*0.5?"IMPROVES":"does NOT improve")} discrimination.");

        // ============================================================
        // PART F — Ranking
        // ============================================================
        _o.WriteLine($"\n=== PART F: Ranking Audit ===");
        var ranks=new List<(string name,double delta)>();
        void AddR(string n,Func<(int,int,double,double,double,double,string),double> f){
            double d=Math.Abs(allP1.Average(f)-allP1b.Average(f));ranks.Add((n,d));
        }
        AddR("resid_rawIQR",d=>d.Item4);
        AddR("seed_rawIQR",d=>d.Item3);
        AddR("resid_rawMean",d=>d.Item6);
        AddR("seed_rawMean",d=>d.Item5);
        ranks=ranks.OrderByDescending(r=>r.delta).ToList();
        _o.WriteLine($"{"Rank",5} {"Descriptor",-18} {"Delta",10}");
        _o.WriteLine(new string('-',35));
        for(int i=0;i<ranks.Count;i++)_o.WriteLine($"{i+1,5} {ranks[i].name,-18} {ranks[i].delta,10:F5}");

        // ============================================================
        // PART G — Robustness
        // ============================================================
        _o.WriteLine($"\n=== PART G: Robustness ===");
        var rng2=new Random(42);var shuf=pd.OrderBy(_=>rng2.NextDouble()).ToArray();
        int h=shuf.Length/2;
        var s1=shuf.Take(h).ToArray();var s2=shuf.Skip(h).ToArray();
        double r1=Math.Abs(s1.Where(d=>d.cls=="P1").Average(d=>d.Item4)-s1.Where(d=>d.cls=="P1b").Average(d=>d.Item4));
        double r2=Math.Abs(s2.Where(d=>d.cls=="P1").Average(d=>d.Item4)-s2.Where(d=>d.cls=="P1b").Average(d=>d.Item4));
        _o.WriteLine($"Random split resid delta: {r1:F5} vs {r2:F5} (range={Math.Abs(r1-r2):F6})");

        // ============================================================
        // PART I — Decision
        // ============================================================
        _o.WriteLine($"\n=== PART I: Decision Model ===");
        _o.WriteLine($"Stop-Low: SAFE. Causal closure: BLOCKED.");

        string decision;
        if(effRatio>5)decision=$"Model A: Residual component contains most SAC-relevant information ({effRatio:F0}x efficiency).";
        else if(effRatio>2)decision=$"Model A: Residual component is significantly more information-dense ({effRatio:F0}x efficiency).";
        else if(effRatio>1)decision="Model B: Seed and residual components contribute jointly.";
        else decision="Model D: Residual preference unresolved.";

        _o.WriteLine($"\nDecision: {decision}");
        _o.WriteLine($"Evidence: resid/seed efficiency={effRatio:F1}x, resid delta={residDelta:F5}, seed delta={seedDelta:F5}");
        _o.WriteLine("CLAIMS: Preference audited. Diagnostic only. Not causal. V6 NOT READY.");
        _o.WriteLine($"\n=== RSP_01 complete. Commit: RSP_01_ResidualSelectionPreferenceAudit ===");
    }

    [Fact]
    public void RSR_01_ResidualSignReversalAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== RSR_01: Residual Sign Reversal Audit ===");
        _o.WriteLine("=== V5.55. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine("=== Question: Is pooled/residual sign reversal stable? ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};int seeds=300;

        // Run pipeline
        var seedIQRd=new ConcurrentDictionary<int,double>();
        var pipeBag=new ConcurrentBag<(int N,int seed,double rawIQR,double residIQR,string cls)>();
        var hi70=Hi(70);var hi72=Hi(72);var hi75=Hi(75);

        // Pre-compute seed means
        Parallel.ForEach(Ns,n=>{Parallel.For(0,seeds,s=>{
            var rng=new Random(s);var w=new double[n];
            for(int i=0;i<n;i++)w[i]=1.0+S*(rng.NextDouble()-0.5)*2.0;
            seedIQRd.AddOrUpdate(s,Q(w.OrderBy(v=>v).ToArray(),0.75)-Q(w.OrderBy(v=>v).ToArray(),0.25),(_,v)=>v+Q(w.OrderBy(v=>v).ToArray(),0.75)-Q(w.OrderBy(v=>v).ToArray(),0.25));
        });});
        var siQ=seedIQRd.ToDictionary(kv=>kv.Key,kv=>kv.Value/Ns.Length);

        // Run SAC pipeline
        Parallel.ForEach(Ns,n=>{var hi=n==70?hi70:n==72?hi72:hi75;
            Parallel.For(0,seeds,s=>{
                if(!IsHi(n,s))return;var sb=SelectAndClassify(n,s,hi);
                if(sb==null||(sb.Value.cls!="P1"&&sb.Value.cls!="P1b"))return;
                var rng=new Random(s);var w=new double[n];
                for(int i=0;i<n;i++)w[i]=1.0+S*(rng.NextDouble()-0.5)*2.0;
                var wo=w.OrderBy(v=>v).ToArray();double ri=Q(wo,0.75)-Q(wo,0.25);
                pipeBag.Add((n,s,ri,ri-siQ.GetValueOrDefault(s,0),sb.Value.cls));
            });});
        var pd=pipeBag.ToArray();
        var p1=pd.Where(d=>d.cls=="P1").ToArray();var p1b=pd.Where(d=>d.cls=="P1b").ToArray();
        _o.WriteLine($"Retained: P1={p1.Length}, P1b={p1b.Length}");

        // ============================================================
        // PART B — Reversal Replication
        // ============================================================
        _o.WriteLine($"\n=== PART B: Reversal Replication ===");
        double poolP1=p1.Average(d=>d.Item3),poolP1b=p1b.Average(d=>d.Item3);
        double resP1=p1.Average(d=>d.Item4),resP1b=p1b.Average(d=>d.Item4);
        _o.WriteLine($"Pooled: P1={poolP1:F5}, P1b={poolP1b:F5}, sign={(poolP1>poolP1b?"POS":"NEG")} (P1 {(poolP1>poolP1b?">":"<")} P1b)");
        _o.WriteLine($"Residual: P1={resP1:F5}, P1b={resP1b:F5}, sign={(resP1>resP1b?"POS":"NEG")} (P1 {(resP1>resP1b?">":"<")} P1b)");
        bool reversal=(poolP1>poolP1b)!=(resP1>resP1b);
        _o.WriteLine($"Sign reversal: {(reversal?"YES — pooled and residual signs OPPOSE":"NO — signs align")}");

        // ============================================================
        // PART C — Leave-One-Seed
        // ============================================================
        _o.WriteLine($"\n=== PART C: Leave-One-Seed Audit ===");
        var allSeeds=pd.Select(d=>d.seed).Distinct().OrderBy(s=>s).ToArray();
        int revCount=0,sameCount=0;
        foreach(var skip in allSeeds){
            var jk=pd.Where(d=>d.seed!=skip).ToArray();
            var jp1=jk.Where(d=>d.cls=="P1").ToArray();var jp1b=jk.Where(d=>d.cls=="P1b").ToArray();
            if(jp1.Length<2||jp1b.Length<2)continue;
            bool jkPool=jp1.Average(d=>d.Item3)>jp1b.Average(d=>d.Item3);
            bool jkRes=jp1.Average(d=>d.Item4)>jp1b.Average(d=>d.Item4);
            if(jkPool!=jkRes)revCount++;else sameCount++;
        }
        _o.WriteLine($"Leave-one-seed: reversal={revCount}, same-sign={sameCount}, reversal%={revCount*100.0/(revCount+sameCount+0.1):F0}%");
        _o.WriteLine($"Stability: {(revCount*100.0/(revCount+sameCount+0.1)>80?"STABLE REVERSAL":revCount*100.0/(revCount+sameCount+0.1)>50?"MOSTLY STABLE":"UNSTABLE")}");

        // ============================================================
        // PART D — Random Split
        // ============================================================
        _o.WriteLine($"\n=== PART D: Random Split Audit ===");
        int splits=100;int revSplit=0;
        var rng2=new Random(42);
        for(int sp=0;sp<splits;sp++){
            var shuf=pd.OrderBy(_=>rng2.NextDouble()).ToArray();int h=shuf.Length/2;
            var s1=shuf.Take(h).ToArray();
            var sp1=s1.Where(d=>d.cls=="P1").ToArray();var sp1b=s1.Where(d=>d.cls=="P1b").ToArray();
            if(sp1.Length<2||sp1b.Length<2)continue;
            bool sPool=sp1.Average(d=>d.Item3)>sp1b.Average(d=>d.Item3);
            bool sRes=sp1.Average(d=>d.Item4)>sp1b.Average(d=>d.Item4);
            if(sPool!=sRes)revSplit++;
        }
        _o.WriteLine($"Random splits: {revSplit}/{splits} show reversal ({revSplit*100.0/splits:F0}%)");

        // ============================================================
        // PART E — N-Stratified
        // ============================================================
        _o.WriteLine($"\n=== PART E: N-Stratified Audit ===");
        _o.WriteLine($"{"N",5} {"P1",4} {"P1b",4} {"Pool sign",10} {"Res sign",10} {"Reversal?",10}");
        _o.WriteLine(new string('-',50));
        foreach(var n in Ns){
            var nd=pd.Where(d=>d.N==n).ToArray();
            var np1=nd.Where(d=>d.cls=="P1").ToArray();var np1b=nd.Where(d=>d.cls=="P1b").ToArray();
            if(np1.Length<1||np1b.Length<1){_o.WriteLine($"{n,5} {np1.Length,4} {np1b.Length,4} {"sparse",10}");continue;}
            bool nPool=np1.Average(d=>d.Item3)>np1b.Average(d=>d.Item3);
            bool nRes=np1.Average(d=>d.Item4)>np1b.Average(d=>d.Item4);
            _o.WriteLine($"{n,5} {np1.Length,4} {np1b.Length,4} {(nPool?"POS":"NEG"),10} {(nRes?"POS":"NEG"),10} {(nPool!=nRes?"YES":"no"),10}");
        }

        // ============================================================
        // PART F — Effect-Scale Audit
        // ============================================================
        _o.WriteLine($"\n=== PART F: Effect-Scale Audit ===");
        double betStd=Sd(pd.Select(d=>d.Item3).ToArray()),resStd=Sd(pd.Select(d=>d.Item4).ToArray());
        double poolEff=Math.Abs(poolP1-poolP1b)/betStd,resEff=Math.Abs(resP1-resP1b)/resStd;
        _o.WriteLine($"Between-seed signal: std={betStd:F5}, P1-P1b delta={Math.Abs(poolP1-poolP1b):F5}, effect={poolEff:F4}σ");
        _o.WriteLine($"Residual signal: std={resStd:F5}, P1-P1b delta={Math.Abs(resP1-resP1b):F5}, effect={resEff:F4}σ");
        _o.WriteLine($"Effect ratio (residual/pooled): {resEff/(poolEff+0.0001):F2}x");
        _o.WriteLine($"Reversal mechanism: {(resEff>poolEff?"Residual signal dominates — reversal is EFFECT-driven":"Pooled signal dominates — reversal is ARTIFACT of level-crossing")}");

        // ============================================================
        // PART G — Robustness
        // ============================================================
        _o.WriteLine($"\n=== PART G: Robustness Summary ===");
        _o.WriteLine($"Replication: {(reversal?"CONFIRMED":"FAILED")}");
        _o.WriteLine($"Leave-one-seed: {revCount*100.0/(revCount+sameCount+0.1):F0}% reversal");
        _o.WriteLine($"Random split: {revSplit*100.0/splits:F0}% reversal");
        _o.WriteLine($"Overall: {(revCount*100.0/(revCount+sameCount+0.1)>70&&revSplit*100.0/splits>60?"STABLE REVERSAL":"WEAK/UNSTABLE")}");

        // ============================================================
        // PART H — Decision
        // ============================================================
        _o.WriteLine($"\n=== PART H: Decision Model ===");
        _o.WriteLine($"Stop-Low: SAFE. Causal closure: BLOCKED.");

        string decision;
        int stabScore=(revCount*100.0/(revCount+sameCount+0.1)>70?3:revCount*100.0/(revCount+sameCount+0.1)>50?2:1)+(revSplit*100.0/splits>60?2:revSplit*100.0/splits>40?1:0);
        if(stabScore>=5)decision="Model A: stable sign reversal. Pooled and residual rawIQR oppose in SAC.";
        else if(stabScore>=3)decision="Model B: mostly stable sign reversal. Reversal survives most perturbations.";
        else if(stabScore>=2)decision="Model C: mixed/underpowered. Reversal present but unstable.";
        else decision="Model D: artifact. Sign reversal likely a finite-sample fluctuation.";

        _o.WriteLine($"\nDecision: {decision}");
        _o.WriteLine($"CLAIMS: Sign reversal audited. Diagnostic only. Not causal. V6 NOT READY.");
        _o.WriteLine($"\n=== RSR_01 complete. Commit: RSR_01_ResidualSignReversalAudit ===");
    }

    [Fact]
    public void RRA_01_RelativeResidualAdvantageAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== RRA_01: Relative Residual Advantage Audit ===");
        _o.WriteLine("=== V5.55. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine("=== Question: Absolute spread or relative position within seed? ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};int seeds=300;

        // Compute seed means + profile data
        var seedIQRd=new ConcurrentDictionary<int,double>();
        var allProf=new ConcurrentBag<(int N,int seed,double riqr)>();
        Parallel.ForEach(Ns,n=>{Parallel.For(0,seeds,s=>{
            var rng=new Random(s);var w=new double[n];
            for(int i=0;i<n;i++)w[i]=1.0+S*(rng.NextDouble()-0.5)*2.0;
            var wo=w.OrderBy(v=>v).ToArray();double ri=Q(wo,0.75)-Q(wo,0.25);
            allProf.Add((n,s,ri));
            seedIQRd.AddOrUpdate(s,ri,(_,v)=>v+ri);
        });});
        var siQ=seedIQRd.ToDictionary(kv=>kv.Key,kv=>kv.Value/Ns.Length);

        // Run pipeline
        var pipeBag=new ConcurrentBag<(int N,int seed,double rawIQR,double resid,double rankPct,string cls)>();
        var hi70=Hi(70);var hi72=Hi(72);var hi75=Hi(75);
        // Pre-compute within-seed ranks
        var seedRanks=new ConcurrentDictionary<int,ConcurrentDictionary<int,double>>();
        foreach(var g in allProf.GroupBy(p=>p.seed)){
            var ordered=g.OrderBy(p=>p.riqr).Select((p,i)=>(p.N,i)).ToArray();
            var dict=new ConcurrentDictionary<int,double>();
            foreach(var(n,i)in ordered)dict[n]=(double)i/(ordered.Length-1);
            seedRanks[g.Key]=dict;
        }

        Parallel.ForEach(Ns,n=>{var hi=n==70?hi70:n==72?hi72:hi75;
            Parallel.For(0,seeds,s=>{
                if(!IsHi(n,s))return;var sb=SelectAndClassify(n,s,hi);
                if(sb==null||(sb.Value.cls!="P1"&&sb.Value.cls!="P1b"))return;
                var prof=allProf.FirstOrDefault(p=>p.N==n&&p.seed==s);
                double resid=prof.riqr-siQ.GetValueOrDefault(s,0);
                double rankPct=seedRanks.GetValueOrDefault(s)?.GetValueOrDefault(n,-1)??-1;
                pipeBag.Add((n,s,prof.riqr,resid,rankPct,sb.Value.cls));
            });});
        var pd=pipeBag.ToArray();
        var p1=pd.Where(d=>d.cls=="P1").ToArray();var p1b=pd.Where(d=>d.cls=="P1b").ToArray();
        _o.WriteLine($"Retained: P1={p1.Length}, P1b={p1b.Length}");

        // ============================================================
        // PART B — Within-Seed Rank Audit
        // ============================================================
        _o.WriteLine($"\n=== PART B: Within-Seed Rank Audit ===");
        double p1Rank=p1.Average(d=>d.Item5),p1bRank=p1b.Average(d=>d.Item5);
        _o.WriteLine($"P1 mean rank percentile: {p1Rank:F3} (seed-internal)");
        _o.WriteLine($"P1b mean rank percentile: {p1bRank:F3}");
        _o.WriteLine($"P1 occupies {(p1Rank<0.5?"LOWER":"HIGHER")} within-seed ranks");

        // ============================================================
        // PART C — Relative Advantage Audit
        // ============================================================
        _o.WriteLine($"\n=== PART C: Relative Advantage Audit ===");
        _o.WriteLine($"{"Predictor",-18} {"P1 mean",10} {"P1b mean",10} {"Delta",10} {"Effect(σ)",12} {"Rank",6}");
        _o.WriteLine(new string('-',70));
        double allStd(double[] v)=>Sd(pd.Select(d=>d.Item3).ToArray()); // use full std for effect
        void Adv(string n,Func<(int,int,double,double,double,string),double> f){
            double p1m=p1.Average(f),p1bm=p1b.Average(f),d=Math.Abs(p1m-p1bm);
            double std=Sd(pd.Select(d=>f(d)).ToArray());
            _o.WriteLine($"{n,-18} {p1m,10:F5} {p1bm,10:F5} {d,10:F5} {(std>0.001?d/std:0),12:F4}σ");
        }
        Adv("absolute rawIQR",d=>d.Item3);
        Adv("residual rawIQR",d=>d.Item4);
        Adv("within-seed rank%",d=>d.Item5);

        // ============================================================
        // PART D — Residual Quantile Audit
        // ============================================================
        _o.WriteLine($"\n=== PART D: Residual Quantile Audit ===");
        var resids=pd.Select(d=>d.Item4).OrderBy(v=>v).ToArray();
        double q33=Q(resids,1.0/3),q67=Q(resids,2.0/3);
        var lo=pd.Where(d=>d.Item4<=q33).ToArray();var md=pd.Where(d=>d.Item4>q33&&d.Item4<=q67).ToArray();var hi=pd.Where(d=>d.Item4>q67).ToArray();
        _o.WriteLine($"Low residual (≤{q33:F5}): P1={lo.Count(d=>d.cls=="P1")}, P1b={lo.Count(d=>d.cls=="P1b")}, P1%={lo.Count(d=>d.cls=="P1")*100.0/Math.Max(1,lo.Length):F0}%");
        _o.WriteLine($"Mid residual: P1={md.Count(d=>d.cls=="P1")}, P1b={md.Count(d=>d.cls=="P1b")}, P1%={md.Count(d=>d.cls=="P1")*100.0/Math.Max(1,md.Length):F0}%");
        _o.WriteLine($"High residual (>{q67:F5}): P1={hi.Count(d=>d.cls=="P1")}, P1b={hi.Count(d=>d.cls=="P1b")}, P1%={hi.Count(d=>d.cls=="P1")*100.0/Math.Max(1,hi.Length):F0}%");
        string concentration=lo.Count(d=>d.cls=="P1")*100.0/Math.Max(1,lo.Length)>hi.Count(d=>d.cls=="P1")*100.0/Math.Max(1,hi.Length)?"LOW residual":"HIGH residual";
        _o.WriteLine($"P1 concentrated in: {concentration}");

        // ============================================================
        // PART E — Cross-Seed Normalization
        // ============================================================
        _o.WriteLine($"\n=== PART E: Cross-Seed Normalization Audit ===");
        // z-score: (rawIQR - seed_mean) / seed_std
        var seedStd=new ConcurrentDictionary<int,double>();
        foreach(var g in allProf.GroupBy(p=>p.seed)){
            var vals=g.Select(p=>p.riqr).ToArray();
            seedStd[g.Key]=Sd(vals);
        }
        var zScores=pd.Select(d=>{
            double sm=siQ.GetValueOrDefault(d.seed,0),ss=seedStd.GetValueOrDefault(d.seed,0.001);
            return (d.Item3-sm)/(ss+0.0001);
        }).ToArray();
        var p1Z=p1.Select(d=>(d.Item3-siQ.GetValueOrDefault(d.seed,0))/(seedStd.GetValueOrDefault(d.seed,0.001)+0.0001)).ToArray();
        var p1bZ=p1b.Select(d=>(d.Item3-siQ.GetValueOrDefault(d.seed,0))/(seedStd.GetValueOrDefault(d.seed,0.001)+0.0001)).ToArray();
        _o.WriteLine($"z-score rawIQR: P1={p1Z.DefaultIfEmpty(0).Average():F4}, P1b={p1bZ.DefaultIfEmpty(0).Average():F4}, delta={Math.Abs(p1Z.DefaultIfEmpty(0).Average()-p1bZ.DefaultIfEmpty(0).Average()):F5}");
        _o.WriteLine($"Absolute rawIQR delta: {Math.Abs(p1.Average(d=>d.Item3)-p1b.Average(d=>d.Item3)):F5}");
        _o.WriteLine($"z-score {(Math.Abs(p1Z.DefaultIfEmpty(0).Average()-p1bZ.DefaultIfEmpty(0).Average())>Math.Abs(p1.Average(d=>d.Item3)-p1b.Average(d=>d.Item3))/100?"PRESERVES":"REDUCES")} separation");

        // ============================================================
        // PART F — Robustness
        // ============================================================
        _o.WriteLine($"\n=== PART F: Robustness ===");
        var rng2=new Random(42);var shuf=pd.OrderBy(_=>rng2.NextDouble()).ToArray();
        int h=shuf.Length/2;
        var s1=shuf.Take(h).ToArray();var s2=shuf.Skip(h).ToArray();
        double rankSign1=s1.Where(d=>d.cls=="P1").Average(d=>d.Item5)>s1.Where(d=>d.cls=="P1b").Average(d=>d.Item5)?1:-1;
        double rankSign2=s2.Where(d=>d.cls=="P1").Average(d=>d.Item5)>s2.Where(d=>d.cls=="P1b").Average(d=>d.Item5)?1:-1;
        _o.WriteLine($"Rank sign stability: split1={(rankSign1>0?"POS":"NEG")}, split2={(rankSign2>0?"POS":"NEG")}, stable={rankSign1==rankSign2}");

        // ============================================================
        // PART G — Decision
        // ============================================================
        _o.WriteLine($"\n=== PART G: Decision Model ===");
        _o.WriteLine($"Stop-Low: SAFE. Causal closure: BLOCKED.");

        double absD=Math.Abs(p1.Average(d=>d.Item3)-p1b.Average(d=>d.Item3));
        double resD=Math.Abs(p1.Average(d=>d.Item4)-p1b.Average(d=>d.Item4));
        double rankD=Math.Abs(p1Rank-p1bRank);
        double maxD=Math.Max(Math.Max(absD,resD*10),rankD); // scale resid to comparable

        string decision;
        if(absD>resD*2&&absD>rankD*2)decision="Model A: absolute rawIQR dominates.";
        else if(resD>absD/5&&resD>rankD/3)decision="Model B: residual rawIQR dominates.";
        else if(rankD>0.1)decision="Model C: relative rank dominates.";
        else decision="Model E: unresolved.";

        _o.WriteLine($"\nDecision: {decision}");
        _o.WriteLine($"Evidence: abs delta={absD:F5}, resid delta={resD:F5}, rank delta={rankD:F3}, P1 rank%={p1Rank:F3}");
        _o.WriteLine("CLAIMS: Relative advantage audited. Diagnostic only. Not causal. V6 NOT READY.");
        _o.WriteLine($"\n=== RRA_01 complete. Commit: RRA_01_RelativeResidualAdvantageAudit ===");
    }

    [Fact]
    public void RRC_01_RelativeRankClosureAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== RRC_01: Relative Rank Closure Audit ===");
        _o.WriteLine("=== V5.55. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine("=== Question: Is rank sufficient, or a proxy? ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};int seeds=300;

        // Compute seed means + within-seed ranks
        var seedIQRd=new ConcurrentDictionary<int,double>();
        var allProf=new ConcurrentBag<(int N,int seed,double riqr,double rmean)>();
        Parallel.ForEach(Ns,n=>{Parallel.For(0,seeds,s=>{
            var rng=new Random(s);var w=new double[n];
            for(int i=0;i<n;i++)w[i]=1.0+S*(rng.NextDouble()-0.5)*2.0;
            var wo=w.OrderBy(v=>v).ToArray();
            allProf.Add((n,s,Q(wo,0.75)-Q(wo,0.25),wo.Average()));
            seedIQRd.AddOrUpdate(s,Q(wo,0.75)-Q(wo,0.25),(_,v)=>v+Q(wo,0.75)-Q(wo,0.25));
        });});
        var siQ=seedIQRd.ToDictionary(kv=>kv.Key,kv=>kv.Value/Ns.Length);

        // Within-seed ranks
        var seedRanks=new ConcurrentDictionary<int,ConcurrentDictionary<int,double>>();
        foreach(var g in allProf.GroupBy(p=>p.seed)){
            var ordered=g.OrderBy(p=>p.riqr).Select((p,i)=>(p.N,i)).ToArray();if(ordered.Length<2)continue;
            var dict=new ConcurrentDictionary<int,double>();
            foreach(var(n,i)in ordered)dict[n]=(double)i/(ordered.Length-1);
            seedRanks[g.Key]=dict;
        }

        // Pipeline
        var pipeBag=new ConcurrentBag<(int N,int seed,double rawIQR,double resid,double rank,double rmean,string cls)>();
        var hi70=Hi(70);var hi72=Hi(72);var hi75=Hi(75);
        Parallel.ForEach(Ns,n=>{var hi=n==70?hi70:n==72?hi72:hi75;
            Parallel.For(0,seeds,s=>{
                if(!IsHi(n,s))return;var sb=SelectAndClassify(n,s,hi);
                if(sb==null||(sb.Value.cls!="P1"&&sb.Value.cls!="P1b"))return;
                var prof=allProf.FirstOrDefault(p=>p.N==n&&p.seed==s);
                pipeBag.Add((n,s,prof.riqr,prof.riqr-siQ.GetValueOrDefault(s,0),seedRanks.GetValueOrDefault(s)?.GetValueOrDefault(n,-1)??-1,prof.rmean,sb.Value.cls));
            });});
        var pd=pipeBag.ToArray();
        var p1=pd.Where(d=>d.cls=="P1").ToArray();var p1b=pd.Where(d=>d.cls=="P1b").ToArray();
        _o.WriteLine($"Retained: P1={p1.Length}, P1b={p1b.Length}");

        // ============================================================
        // PART B — Rank Survival: condition on rank, check residuals
        // ============================================================
        _o.WriteLine($"\n=== PART B: Rank Survival Audit ===");
        double rankMed=pd.Select(d=>d.rank).OrderBy(v=>v).ToArray()[pd.Length/2];
        var loRank=pd.Where(d=>d.rank<=rankMed).ToArray();
        var hiRank=pd.Where(d=>d.rank>rankMed).ToArray();
        _o.WriteLine($"Low rank (≤{rankMed:F2}): P1={loRank.Count(d=>d.cls=="P1")}, P1b={loRank.Count(d=>d.cls=="P1b")}, P1%={loRank.Count(d=>d.cls=="P1")*100.0/Math.Max(1,loRank.Length):F0}%");
        _o.WriteLine($"High rank (>{rankMed:F2}): P1={hiRank.Count(d=>d.cls=="P1")}, P1b={hiRank.Count(d=>d.cls=="P1b")}, P1%={hiRank.Count(d=>d.cls=="P1")*100.0/Math.Max(1,hiRank.Length):F0}%");

        // Within matching rank, does residual still separate?
        _o.WriteLine($"\nWithin matching rank, residual P1-P1b:");
        double loResP1=loRank.Where(d=>d.cls=="P1").Select(d=>d.resid).DefaultIfEmpty(0).Average();
        double loResP1b=loRank.Where(d=>d.cls=="P1b").Select(d=>d.resid).DefaultIfEmpty(0).Average();
        double hiResP1=hiRank.Where(d=>d.cls=="P1").Select(d=>d.resid).DefaultIfEmpty(0).Average();
        double hiResP1b=hiRank.Where(d=>d.cls=="P1b").Select(d=>d.resid).DefaultIfEmpty(0).Average();
        _o.WriteLine($"  Low rank: P1 resid={loResP1:F5}, P1b resid={loResP1b:F5}, delta={Math.Abs(loResP1-loResP1b):F5}");
        _o.WriteLine($"  High rank: P1 resid={hiResP1:F5}, P1b resid={hiResP1b:F5}, delta={Math.Abs(hiResP1-hiResP1b):F5}");
        bool rankAbsorbs=Math.Abs(loResP1-loResP1b)<0.001&&Math.Abs(hiResP1-hiResP1b)<0.001;
        _o.WriteLine($"Rank {(rankAbsorbs?"ABSORBS residual separation":"does NOT absorb — residual survives")}");

        // ============================================================
        // PART C — Residual Survival: condition on residual, check rank
        // ============================================================
        _o.WriteLine($"\n=== PART C: Residual Survival Audit ===");
        double residMed=pd.Select(d=>d.resid).OrderBy(v=>v).ToArray()[pd.Length/2];
        var loRes=pd.Where(d=>d.resid<=residMed).ToArray();
        var hiRes=pd.Where(d=>d.resid>residMed).ToArray();
        double loRankP1=loRes.Where(d=>d.cls=="P1").Select(d=>d.rank).DefaultIfEmpty(0).Average();
        double loRankP1b=loRes.Where(d=>d.cls=="P1b").Select(d=>d.rank).DefaultIfEmpty(0).Average();
        double hiRankP1=hiRes.Where(d=>d.cls=="P1").Select(d=>d.rank).DefaultIfEmpty(0).Average();
        double hiRankP1b=hiRes.Where(d=>d.cls=="P1b").Select(d=>d.rank).DefaultIfEmpty(0).Average();
        _o.WriteLine($"  Low resid: P1 rank={loRankP1:F3}, P1b rank={loRankP1b:F3}, delta={Math.Abs(loRankP1-loRankP1b):F3}");
        _o.WriteLine($"  High resid: P1 rank={hiRankP1:F3}, P1b rank={hiRankP1b:F3}, delta={Math.Abs(hiRankP1-hiRankP1b):F3}");
        bool residAbsorbs=Math.Abs(loRankP1-loRankP1b)<0.1&&Math.Abs(hiRankP1-hiRankP1b)<0.1;
        _o.WriteLine($"Residual {(residAbsorbs?"ABSORBS rank separation":"does NOT absorb — rank survives")}");

        // ============================================================
        // PART D — Percentile Audit
        // ============================================================
        _o.WriteLine($"\n=== PART D: Percentile Audit ===");
        _o.WriteLine($"{"Rank%",-12} {"n",5} {"P1",4} {"P1b",4} {"P1%",7}");
        _o.WriteLine(new string('-',35));
        for(int q=0;q<4;q++){
            double loQ=q/4.0,hiQ=(q+1)/4.0;
            var qd=pd.Where(d=>d.rank>=loQ&&d.rank<hiQ+(q==3?0.01:0)).ToArray();
            int qp1=qd.Count(d=>d.cls=="P1"),qp1b=qd.Count(d=>d.cls=="P1b");
            _o.WriteLine($"{$"{loQ*100:F0}-{hiQ*100:F0}%",-12} {qd.Length,5} {qp1,4} {qp1b,4} {(qp1+qp1b>0?qp1*100.0/(qp1+qp1b):0),7:F0}%");
        }

        // ============================================================
        // PART E — Normalization Audit
        // ============================================================
        _o.WriteLine($"\n=== PART E: Normalization Audit ===");
        _o.WriteLine($"{"Descriptor",-18} {"Effect(σ)",10} {"P1-P1b",10}");
        _o.WriteLine(new string('-',40));
        double EvalEff(double[] a,double[] b,double[] all){
            double d=Math.Abs(a.DefaultIfEmpty(0).Average()-b.DefaultIfEmpty(0).Average()),s=Sd(all);
            return s>0.001?d/s:0;
        }
        var allV=pd.Select(d=>d.rawIQR).ToArray();var allRes=pd.Select(d=>d.resid).ToArray();var allRank=pd.Select(d=>d.rank).ToArray();
        var allMean=pd.Select(d=>d.rmean).ToArray();
        _o.WriteLine($"{"absolute rawIQR",-18} {EvalEff(p1.Select(d=>d.rawIQR).ToArray(),p1b.Select(d=>d.rawIQR).ToArray(),allV),10:F4}σ {Math.Abs(p1.Average(d=>d.rawIQR)-p1b.Average(d=>d.rawIQR)),10:F5}");
        _o.WriteLine($"{"residual rawIQR",-18} {EvalEff(p1.Select(d=>d.resid).ToArray(),p1b.Select(d=>d.resid).ToArray(),allRes),10:F4}σ {Math.Abs(p1.Average(d=>d.resid)-p1b.Average(d=>d.resid)),10:F5}");
        _o.WriteLine($"{"rank percentile",-18} {EvalEff(p1.Select(d=>d.rank).ToArray(),p1b.Select(d=>d.rank).ToArray(),allRank),10:F4}σ {Math.Abs(p1.Average(d=>d.rank)-p1b.Average(d=>d.rank)),10:F5}");
        _o.WriteLine($"{"rawMean",-18} {EvalEff(p1.Select(d=>d.rmean).ToArray(),p1b.Select(d=>d.rmean).ToArray(),allMean),10:F4}σ {Math.Abs(p1.Average(d=>d.rmean)-p1b.Average(d=>d.rmean)),10:F5}");

        // ============================================================
        // PART F — Robustness
        // ============================================================
        _o.WriteLine($"\n=== PART F: Robustness ===");
        var rng2=new Random(42);
        int rankStable=0;
        for(int sp=0;sp<50;sp++){
            var shuf=pd.OrderBy(_=>rng2.NextDouble()).ToArray();int h2=shuf.Length/2;
            var s1=shuf.Take(h2).ToArray();var s2=shuf.Skip(h2).ToArray();
            bool s1r=s1.Where(d=>d.cls=="P1").DefaultIfEmpty().Average(d=>d.rank)<s1.Where(d=>d.cls=="P1b").DefaultIfEmpty().Average(d=>d.rank);
            bool s2r=s2.Where(d=>d.cls=="P1").DefaultIfEmpty().Average(d=>d.rank)<s2.Where(d=>d.cls=="P1b").DefaultIfEmpty().Average(d=>d.rank);
            if(s1r==s2r)rankStable++;
        }
        _o.WriteLine($"Rank sign stability (50 splits): {rankStable}/50 ({rankStable*2}%)");

        // ============================================================
        // PART G — Decision
        // ============================================================
        _o.WriteLine($"\n=== PART G: Decision Model ===");
        _o.WriteLine($"Stop-Low: SAFE. Causal closure: BLOCKED.");

        bool rankDominates=!rankAbsorbs&&residAbsorbs;
        bool bothSurvive=!rankAbsorbs&&!residAbsorbs;

        string decision;
        if(rankDominates)decision="Model A: relative rank is sufficient. Rank absorbs residual; residual does NOT absorb rank.";
        else if(bothSurvive)decision="Model B: relative rank + residual both contribute. Neither fully absorbs the other.";
        else if(!rankAbsorbs)decision="Model C: rank dominant but residual provides independent information.";
        else decision="Model E: closure unresolved.";

        _o.WriteLine($"\nDecision: {decision}");
        _o.WriteLine($"Evidence: rank absorbs residual={rankAbsorbs}, residual absorbs rank={residAbsorbs}");
        _o.WriteLine("CLAIMS: Rank closure audited. Diagnostic only. Not causal. V6 NOT READY.");
        _o.WriteLine($"\n=== RRC_01 complete. Commit: RRC_01_RelativeRankClosureAudit ===");
    }

    // ============================================================
    // HELPERS
    // ============================================================
    static double Q(double[] s,double p)=>s[(int)(p*(s.Length-1))];
    static double Sd(double[] s){double m=s.Average();return Math.Sqrt(s.Sum(v=>(v-m)*(v-m))/(s.Length-1));}
    static double Pearson(double[] x,double[] y){
        int n=Math.Min(x.Length,y.Length);double mx=x.Take(n).Average(),my=y.Take(n).Average();
        double sx=0,sy=0,sxy=0;
        for(int i=0;i<n;i++){double dx=x[i]-mx,dy=y[i]-my;sx+=dx*dx;sy+=dy*dy;sxy+=dx*dy;}
        return (sx>0.001&&sy>0.001)?sxy/Math.Sqrt(sx*sy):0;
    }
    static int[] RankVals(double[] v){int n=v.Length;return Enumerable.Range(0,n).OrderBy(i=>v[i]).Select((idx,r)=>new{idx,r}).OrderBy(x=>x.idx).Select(x=>x.r).ToArray();}
    static double Spearman(double[] x,double[] y){int n=Math.Min(x.Length,y.Length);var rx=RankVals(x.Take(n).ToArray());var ry=RankVals(y.Take(n).ToArray());return Pearson(rx.Select(v=>(double)v).ToArray(),ry.Select(v=>(double)v).ToArray());}

    static double[][]Sim(double[,]K,int n,double s,int seed){var rng=new Random(seed);var w=new double[n];for(int i=0;i<n;i++)w[i]=1.0+s*(rng.NextDouble()-0.5)*2.0;var th=new double[n];for(int i=0;i<n;i++)th[i]=rng.NextDouble()*2*Math.PI;int hL=St/Hd+1;var h=new double[hL][];h[0]=(double[])th.Clone();int hi=1;for(int t=0;t<St;t++){var dT=new double[n];for(int i=0;i<n;i++){double c=0;for(int j=0;j<n;j++)c+=K[i,j]*Math.Sin(th[j]-th[i]);dT[i]=w[i]+c;}for(int i=0;i<n;i++)th[i]+=Dt*dT[i];if((t+1)%Hd==0&&hi<hL)h[hi++]=(double[])th.Clone();}return h;}
    static double[,]RP(double[][]h,int n){int T=h.Length;var R=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++){double sc=0,ss=0;for(int t=0;t<T;t++){double d=h[t][i]-h[t][j];sc+=Math.Cos(d);ss+=Math.Sin(d);}R[i,j]=Math.Sqrt(sc*sc+ss*ss)/T;}return R;}
    static double[,]Nm(double[,]R,int n){double mn=double.MaxValue;for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j&&R[i,j]<mn)mn=R[i,j];double rng=1.0-mn;if(rng<1e-15)rng=1.0;var Rn=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Rn[i,j]=i==j?1.0:Math.Max(REps,(R[i,j]-mn)/rng);return Rn;}
    static double[,]DL(double[,]R,int n){var d=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)d[i,j]=i==j?0:-Math.Log(Math.Max(R[i,j],1e-100));return d;}
    static double[,]Cupd(double[,]d,int n){var K=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)K[i,j]=i==j?0:K0*Math.Exp(-d[i,j]/Math.Max(Xi,0.01));return K;}
    static double[]Of(double[][]h,int n){int T=h.Length;var o=new double[n];for(int i=0;i<n;i++){double su=0;int c=0;for(int t=1;t<T;t++){su+=Math.Abs(h[t][i]-h[t-1][i]);c++;}o[i]=c>0?su/(c*Dt*Hd):0;}return o;}
    static double[,]KS(int n,int seed){var rng=new Random(seed);var adj=new HashSet<int>[n];for(int i=0;i<n;i++)adj[i]=new HashSet<int>();double p=6.0/(n-1);for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)if(rng.NextDouble()<p){adj[i].Add(j);adj[j].Add(i);}var v=new bool[n];var cs=new List<List<int>>();for(int i=0;i<n;i++){if(v[i])continue;var c=new List<int>();var q=new Queue<int>();v[i]=true;q.Enqueue(i);while(q.Count>0){int u=q.Dequeue();c.Add(u);foreach(int x in adj[u])if(!v[x]){v[x]=true;q.Enqueue(x);}}cs.Add(c);}for(int i=1;i<cs.Count;i++){adj[cs[i][0]].Add(cs[i-1][0]);adj[cs[i-1][0]].Add(cs[i][0]);}var K=new double[n,n];for(int i=0;i<n;i++)foreach(int j in adj[i])if(i<j){K[i,j]=0.5;K[j,i]=0.5;}return K;}
    static double Dm(double[,]d,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=d[i,j];c++;}return c>0?s/c:0;}
    static double Lambda1(double[,]K,int n){double s=0;for(int i=0;i<n;i++)for(int j=0;j<n;j++)s+=K[i,j];return s/(n*n);}
    static double Km(double[,]K,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=K[i,j];c++;}return c>0?s/c:0;}
    static double Ks(double[,]K,int n){var v=new double[n*(n-1)/2];int idx=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)v[idx++]=K[i,j];double m=v.Average();return Math.Sqrt(v.Sum(x=>(x-m)*(x-m))/v.Length);}
    static double[,]CD(double[,]d,int n){var c=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)c[i,j]=d[i,j];return c;}
    SBase? SelectAndClassify(int n,int s,P3 hi){var K=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}var h3=Sim(K,n,S,s+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);var h3E=Sim(K3,n,S,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);double d0=Dm(d3E,n),km=Km(K3E,n),ks=Ks(K3E,n);var sb=new SBase{seed=s,d0=d0,km0=km,ks0=ks,cls=""};sb=Classify(sb,hi);double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,sv=hi.ks-Lo(n).ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);double proj=vn>0?((d0-Lo(n).dm)*dv+(km-Lo(n).km)*kv+(ks-Lo(n).ks)*sv)/vn:0;double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(km-Lo(n).km)*(km-Lo(n).km)+(ks-Lo(n).ks)*(ks-Lo(n).ks);double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));if(!(n==72?sb.cls=="P1"||sb.cls=="P1b"?proj>PHV&&orth>OTH:false:sb.cls=="P1"||sb.cls=="P1b"?proj>PHV:false))return null;return sb;}
    static SBase Classify(SBase b,P3 hi){string cls;if(b.d0>0.65)cls="P1b";else if(b.d0>0.50)cls="P1";else if(b.d0<=0.40&&b.km0>0.98&&DistK(b.km0,b.ks0,hi)<0.15)cls="P2";else if(b.d0>0.40&&b.d0<=0.50)cls="P3";else cls="P4";return new SBase{seed=b.seed,d0=b.d0,km0=b.km0,ks0=b.ks0,cls=cls};}
    static double DistK(double km,double ks,P3 hi){double dk=km-hi.km,dks=ks-hi.ks;return Math.Sqrt(dk*dk+dks*dks);}
    bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<5;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,S,seed+5),n).Average()>THR;}
    static P3 PCent(int n,bool hi){double d=0,k=0,ks=0;int c=0;for(int sd=0;sd<100;sd++){var K=KS(n,sd);double dm=0,km=0,kss=0;for(int e=0;e<5;e++){var h=Sim(K,n,S,sd+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}double om=Of(Sim(K,n,S,sd+5),n).Average();if((om>THR)==hi){d+=dm/5;k+=km/5;ks+=kss/5;c++;}}if(c==0)return new P3{dm=double.NaN,km=double.NaN,ks=double.NaN};return new P3{dm=d/c,km=k/c,ks=ks/c};}
}
