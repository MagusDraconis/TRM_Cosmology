using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_21;

[Trait("Category","V5_21"),Trait("Category","V5_21_LRA"),Trait("Category","LongRunning")]
public class V5_21_LowNRescueAnalysis_Tests
{
    private readonly ITestOutputHelper _o;
    // Frozen V5.20 constants
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153,RebT=0.01;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    // ─── LRA analysis profile ───
    struct LraProfile{
        // Identity
        public int n,s,cohort;public string stateGroup,cls;
        // Geometry (T0 — candidate characterization)
        public double distHi,distLo,projHiVec,orthHiVec,offVecAngle;
        // Response direction (T1 → T2)
        public double entryAlignPre,entryAlignPost,deltaAlign,respDirAngle,respProjHi,respOrthHi;
        // Response dynamics
        public double dT0,dT1,dT2,kT0,kT1,kT2,omT0,omT1,omT2;
        public double rebMag,dRebound,kCollapse;
        // C3 correction effect
        public double c3DMeanShift,c3KMeanShift,c3OmegaShift;
        // Outcome
        public bool a0,c3,rescued,induced;public string failureLabel;
    }

    public V5_21_LowNRescueAnalysis_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    // ═══════════════════════════════════════════════
    // LRA_01 — Off-vector mechanism analysis
    // ═══════════════════════════════════════════════
    [Fact]public void LRA_01_OffVectorMechanism(){
        _o.WriteLine("═══ LRA_01: Off-vector mechanism — why N=50-63 fail ═══");
        _o.WriteLine("Comparing off-vector failures (N=50-63) to onset rescues (N=65).");

        int[] lowN={50,55,60,62,63};
        int[] onsetN={65};
        var profiles=new ConcurrentBag<LraProfile>();

        Parallel.ForEach(lowN.Concat(onsetN),n=>{
            var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<199;s++){
                if(IsHi(n,s))continue;
                var p=BuildProfile(n,s,hi,lo);
                if(p==null)continue;
                profiles.Add(p.Value);
            }
        });

        var all=profiles.ToArray();
        var offVec=all.Where(p=>p.n<=63).ToArray(); // N=50-63 off-vector failures
        var onset=all.Where(p=>p.n==65).ToArray();

        _o.WriteLine($"\nProfiles: {offVec.Length} off-vector (N=50-63), {onset.Length} onset (N=65)");

        // ─── Response direction comparison ───
        _o.WriteLine($"\n─── Response vector direction ───");
        _o.WriteLine(string.Format("{0,4} {1,6} {2,10} {3,10} {4,10} {5,10} {6,10}",
            "N","n","projHiVec","orthHiVec","offVecAng","respProjHi","respOrthHi"));
        foreach(var n in lowN.Concat(onsetN)){
            var sub=all.Where(p=>p.n==n).ToArray();
            if(sub.Length==0)continue;
            _o.WriteLine($"{n,4} {sub.Length,6} {sub.Average(p=>p.projHiVec),10:F4} {sub.Average(p=>p.orthHiVec),10:F4} {sub.Average(p=>p.offVecAngle),10:F4} {sub.Average(p=>p.respProjHi),10:F4} {sub.Average(p=>p.respOrthHi),10:F4}");
        }

        // ─── Geometry comparison ───
        _o.WriteLine($"\n─── Candidate geometry ───");
        _o.WriteLine(string.Format("{0,4} {1,10} {2,10} {3,10} {4,10}",
            "N","distHi","distLo","projHiVec","offVecAng"));
        foreach(var n in lowN.Concat(onsetN)){
            var sub=all.Where(p=>p.n==n).ToArray();
            if(sub.Length==0)continue;
            _o.WriteLine($"{n,4} {sub.Average(p=>p.distHi),10:F4} {sub.Average(p=>p.distLo),10:F4} {sub.Average(p=>p.projHiVec),10:F4} {sub.Average(p=>p.offVecAngle),10:F4}");
        }

        // ─── Dynamics comparison ───
        _o.WriteLine($"\n─── Response dynamics ───");
        _o.WriteLine(string.Format("{0,4} {1,10} {2,10} {3,10} {4,10} {5,10}",
            "N","dT0","dT1","dT2","rebMag","kCollapse"));
        foreach(var n in lowN.Concat(onsetN)){
            var sub=all.Where(p=>p.n==n).ToArray();
            if(sub.Length==0)continue;
            _o.WriteLine($"{n,4} {sub.Average(p=>p.dT0),10:F4} {sub.Average(p=>p.dT1),10:F4} {sub.Average(p=>p.dT2),10:F4} {sub.Average(p=>p.rebMag),10:F4} {sub.Average(p=>p.kCollapse),10:F4}");
        }

        // ─── Off-vector classification ───
        _o.WriteLine($"\n─── Off-vector classification ───");
        double offVecThresh=0.15;
        var severelyOff=offVec.Where(p=>p.offVecAngle>offVecThresh).ToArray();
        var mildlyOff=offVec.Where(p=>p.offVecAngle>0&&p.offVecAngle<=offVecThresh).ToArray();
        _o.WriteLine($"Severely off-vector (angle > {offVecThresh}): {severelyOff.Length}/{offVec.Length} ({severelyOff.Length*100.0/offVec.Length:F0}%)");
        _o.WriteLine($"Mildly off-vector (angle <= {offVecThresh}): {mildlyOff.Length}/{offVec.Length} ({mildlyOff.Length*100.0/offVec.Length:F0}%)");

        // ─── ProjHiVec distribution ───
        _o.WriteLine($"\n─── projHiVec distribution ───");
        var offProj=offVec.Select(p=>p.projHiVec).OrderBy(x=>x).ToArray();
        var onsetProj=onset.Select(p=>p.projHiVec).OrderBy(x=>x).ToArray();
        _o.WriteLine($"Off-vector projHiVec: min={offProj.Min():F4} p25={Percentile(offProj,0.25):F4} med={Percentile(offProj,0.5):F4} p75={Percentile(offProj,0.75):F4} max={offProj.Max():F4}");
        _o.WriteLine($"Onset projHiVec:      min={onsetProj.Min():F4} p25={Percentile(onsetProj,0.25):F4} med={Percentile(onsetProj,0.5):F4} p75={Percentile(onsetProj,0.75):F4} max={onsetProj.Max():F4}");

        // ─── Gate assessment ───
        double offVecDominance=offVec.Where(p=>p.failureLabel=="off-vector").Count()*100.0/offVec.Length;
        _o.WriteLine($"\n─── Gates ───");
        _o.WriteLine($"Gate A (Off-vector barrier): {(offVecDominance>80?"REACHED":"NOT REACHED")} — {offVecDominance:F0}% off-vector at N=50-63");
        _o.WriteLine($"Gate E (Response-direction dominates): {(offVec.Length>0&&offVec.Average(p=>p.offVecAngle)>onset.Average(p=>p.offVecAngle)*1.5?"REACHED":"assessing")}");

        _o.WriteLine($"\n─── Next: LRA_02 N=64 transitional ───");
    }

    // ═══════════════════════════════════════════════
    // LRA_02 — N=64 transitional analysis
    // ═══════════════════════════════════════════════
    [Fact]public void LRA_02_N64TransitionalAnalysis(){
        _o.WriteLine("═══ LRA_02: N=64 transitional analysis ═══");
        _o.WriteLine("Compare N=63 → N=64 → N=65 to determine transition mechanism.");

        int[] Ns={63,64,65};
        var profiles=new ConcurrentBag<LraProfile>();

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
        var n63=all.Where(p=>p.n==63).ToArray();
        var n64=all.Where(p=>p.n==64).ToArray();
        var n65=all.Where(p=>p.n==65).ToArray();

        _o.WriteLine($"\nTransitional profiles: N=63:{n63.Length} N=64:{n64.Length} N=65:{n65.Length}");

        // ─── Full metric panel ───
        _o.WriteLine($"\n─── Metric comparison: N=63 → 64 → 65 ───");
        _o.WriteLine(string.Format("{0,4} {1,8} {2,8} {3,8} {4,8} {5,8} {6,8} {7,8} {8,8} {9,8} {10,8}",
            "N","distHi","projHi","offAng","dT1","dT2","rebMag","kCollapse","respProj","respOrth","deltaAlign"));
        foreach(var grp in new[]{n63,n64,n65}){
            if(grp.Length==0)continue;
            _o.WriteLine($"{grp[0].n,4} {grp.Average(p=>p.distHi),8:F4} {grp.Average(p=>p.projHiVec),8:F4} {grp.Average(p=>p.offVecAngle),8:F4} {grp.Average(p=>p.dT1),8:F4} {grp.Average(p=>p.dT2),8:F4} {grp.Average(p=>p.rebMag),8:F4} {grp.Average(p=>p.kCollapse),8:F4} {grp.Average(p=>p.respProjHi),8:F4} {grp.Average(p=>p.respOrthHi),8:F4} {grp.Average(p=>p.deltaAlign),8:F4}");
        }

        // ─── Step-change analysis ───
        _o.WriteLine($"\n─── Step changes ───");
        _o.WriteLine(string.Format("{0,12} {1,8} {2,8} {3,8} {4,8} {5,8}",
            "Metric","63→64","64→65","63→65","64/63","65/64"));
        string[] metrics={"distHi","projHiVec","offVecAngle","dT1","dT2","rebMag","kCollapse","respProjHi"};
        double[] v63={n63.Average(p=>p.distHi),n63.Average(p=>p.projHiVec),n63.Average(p=>p.offVecAngle),n63.Average(p=>p.dT1),n63.Average(p=>p.dT2),n63.Average(p=>p.rebMag),n63.Average(p=>p.kCollapse),n63.Average(p=>p.respProjHi)};
        double[] v64={n64.Average(p=>p.distHi),n64.Average(p=>p.projHiVec),n64.Average(p=>p.offVecAngle),n64.Average(p=>p.dT1),n64.Average(p=>p.dT2),n64.Average(p=>p.rebMag),n64.Average(p=>p.kCollapse),n64.Average(p=>p.respProjHi)};
        double[] v65={n65.Average(p=>p.distHi),n65.Average(p=>p.projHiVec),n65.Average(p=>p.offVecAngle),n65.Average(p=>p.dT1),n65.Average(p=>p.dT2),n65.Average(p=>p.rebMag),n65.Average(p=>p.kCollapse),n65.Average(p=>p.respProjHi)};

        for(int i=0;i<metrics.Length;i++){
            double d63_64=v64[i]-v63[i],d64_65=v65[i]-v64[i],d63_65=v65[i]-v63[i];
            double r64_63=v64[i]/Math.Max(1e-9,v63[i]),r65_64=v65[i]/Math.Max(1e-9,v64[i]);
            _o.WriteLine($"{metrics[i],12} {d63_64,8:+0.0000;-0.0000} {d64_65,8:+0.0000;-0.0000} {d63_65,8:+0.0000;-0.0000} {r64_63,8:F3}x {r65_64,8:F3}x");
        }

        // ─── N=64 closeness assessment ───
        double dist64to63=0,dist64to65=0;
        for(int i=0;i<metrics.Length;i++){
            dist64to63+=Math.Abs(v64[i]-v63[i])/(Math.Abs(v63[i])+1e-9);
            dist64to65+=Math.Abs(v65[i]-v64[i])/(Math.Abs(v64[i])+1e-9);
        }
        _o.WriteLine($"\nN=64 normalized distance to N=63: {dist64to63/metrics.Length:F3}");
        _o.WriteLine($"N=64 normalized distance to N=65: {dist64to65/metrics.Length:F3}");
        _o.WriteLine($"N=64 is closer to: {(dist64to63<dist64to65?"N=63":"N=65")}");

        // ─── Failure-mode shift ───
        _o.WriteLine($"\n─── Failure-mode distribution ───");
        foreach(var grp in new[]{n63,n64,n65}){
            var fm=grp.GroupBy(p=>p.failureLabel).OrderByDescending(g=>g.Count());
            _o.WriteLine($"N={grp[0].n}: {string.Join(", ",fm.Take(3).Select(g=>$"{g.Key}({g.Count()})"))}");
        }

        // ─── Gate assessment ───
        bool n64Transitional=n64.Length>0&&n64.Average(p=>p.offVecAngle)<0.10;
        _o.WriteLine($"\nGate B (N=64 transitional): {(n64Transitional?"REACHED":"NOT REACHED")}");

        _o.WriteLine($"\n─── Next: LRA_03 Onset comparison ───");
    }

    // ═══════════════════════════════════════════════
    // LRA_03 — N=64→65 onset comparison
    // ═══════════════════════════════════════════════
    [Fact]public void LRA_03_OnsetComparison(){
        _o.WriteLine("═══ LRA_03: N=64→65 onset — what changes? ═══");
        _o.WriteLine("Deep analysis of the metric changes that enable first inducibility.");

        int[] Ns={64,65,66};
        var profiles=new ConcurrentBag<LraProfile>();

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
        var n64=all.Where(p=>p.n==64).ToArray();
        var n65=all.Where(p=>p.n==65).ToArray();
        var n65rescued=n65.Where(p=>p.rescued).ToArray();
        var n65failed=n65.Where(p=>!p.rescued).ToArray();

        _o.WriteLine($"\nProfiles: N=64:{n64.Length} N=65 total:{n65.Length} rescued:{n65rescued.Length} failed:{n65failed.Length}");

        // ─── Rescued vs failed at N=65 ───
        if(n65rescued.Length>0&&n65failed.Length>0){
            _o.WriteLine($"\n─── N=65 rescued vs failed ───");
            _o.WriteLine(string.Format("{0,-12} {1,10} {2,10} {3,10}",
                "Metric","Rescued","Failed","Ratio"));
            string[] metrics={"distHi","projHiVec","offVecAngle","dT1","dT2","rebMag","respProjHi","respOrthHi","deltaAlign"};
            foreach(var m in metrics){
                double rv=GetMetric(n65rescued,m),fv=GetMetric(n65failed,m);
                _o.WriteLine($"{m,-12} {rv,10:F4} {fv,10:F4} {rv/Math.Max(1e-9,fv),10:F3}x");
            }
        }

        // ─── N=64 vs N=65 rescued ───
        _o.WriteLine($"\n─── N=64 vs N=65 rescued ───");
        _o.WriteLine(string.Format("{0,-12} {1,10} {2,10} {3,10}",
            "Metric","N=64","N=65 Resc","Ratio"));
        string[] metrics2={"distHi","projHiVec","offVecAngle","dT1","dT2","rebMag","respProjHi","respOrthHi","deltaAlign","kCollapse"};
        foreach(var m in metrics2){
            double n64v=GetMetric(n64,m),n65v=GetMetric(n65rescued,m);
            _o.WriteLine($"{m,-12} {n64v,10:F4} {n65v,10:F4} {n65v/Math.Max(1e-9,n64v),10:F3}x");
        }

        // ─── C3 correction effect ───
        _o.WriteLine($"\n─── C3 correction effect ───");
        _o.WriteLine(string.Format("{0,4} {1,10} {2,10} {3,10}",
            "N","c3DShift","c3KShift","c3OmegaShift"));
        foreach(var grp in new[]{n64,n65}){
            _o.WriteLine($"{grp[0].n,4} {grp.Average(p=>p.c3DMeanShift),10:F4} {grp.Average(p=>p.c3KMeanShift),10:F4} {grp.Average(p=>p.c3OmegaShift),10:F4}");
        }

        // ─── What changes at N=65? ───
        _o.WriteLine($"\n─── Critical changes: N=64→65 ───");
        var changes=new List<(string,double,string)>();
        foreach(var m in metrics2){
            double r=GetMetric(n65,m)/Math.Max(1e-9,GetMetric(n64,m));
            string dir=r>1.05?"increases":r<0.95?"decreases":"stable";
            changes.Add((m,r,dir));
        }
        foreach(var c in changes.OrderByDescending(x=>Math.Abs(x.Item2-1.0)).Take(5))
            _o.WriteLine($"  {c.Item1}: {c.Item2:F3}x ({c.Item3})");

        // ─── Gate assessment ───
        _o.WriteLine($"\nGate C (Onset mechanism identified): {(n65rescued.Length>0?"REACHED":"NOT REACHED")} — metrics above show what enables first rescue");
        _o.WriteLine($"\n─── Next: LRA_04 Driver ranking ───");
    }

    // ═══════════════════════════════════════════════
    // LRA_04 — Rescue-immunity driver ranking
    // ═══════════════════════════════════════════════
    [Fact]public void LRA_04_RescueImmunityDriverRanking(){
        _o.WriteLine("═══ LRA_04: Rescue-immunity driver ranking ═══");
        _o.WriteLine("Ranking barriers by their explanatory power across N=50-72.");

        int[] Ns={50,55,60,62,63,64,65,66,70,72};
        var profiles=new ConcurrentBag<LraProfile>();

        Parallel.ForEach(Ns,n=>{
            var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<99;s++){
                if(IsHi(n,s))continue;
                var p=BuildProfile(n,s,hi,lo);
                if(p==null)continue;
                profiles.Add(p.Value);
            }
        });

        var all=profiles.ToArray();
        var lowN=all.Where(p=>p.n<=64).ToArray();
        var adaptiveN=all.Where(p=>p.n>=65).ToArray();

        _o.WriteLine($"\nProfiles: {lowN.Length} low-N, {adaptiveN.Length} adaptive");

        // ─── Driver ranking by N ───
        _o.WriteLine($"\n─── Metric correlation with rescue (all N) ───");
        string[] drivers={"offVecAngle","distHi","dT1","rebMag","respProjHi","respOrthHi","deltaAlign","kCollapse","dRebound"};
        foreach(var d in drivers){
            double low=lowN.Length>0?lowN.Average(p=>GetMetricVal(p,d)):0;
            double adapt=adaptiveN.Length>0?adaptiveN.Average(p=>GetMetricVal(p,d)):0;
            double ratio=Math.Abs(adapt-low)/Math.Max(1e-9,Math.Max(Math.Abs(low),Math.Abs(adapt)));
            _o.WriteLine($"{d,-15} low-N:{low,8:F4} adaptive:{adapt,8:F4} delta-ratio:{ratio,6:F3}");
        }

        // ─── N-by-N driver table ───
        _o.WriteLine($"\n─── Key metrics by N ───");
        _o.WriteLine(string.Format("{0,4} {1,8} {2,8} {3,8} {4,8} {5,8} {6,8} {7,8}",
            "N","offAng","distHi","dT1","rebMag","respProj","deltaAl","resc%"));
        foreach(var n in Ns){
            var sub=all.Where(p=>p.n==n).ToArray();
            if(sub.Length==0)continue;
            _o.WriteLine($"{n,4} {sub.Average(p=>p.offVecAngle),8:F4} {sub.Average(p=>p.distHi),8:F4} {sub.Average(p=>p.dT1),8:F4} {sub.Average(p=>p.rebMag),8:F4} {sub.Average(p=>p.respProjHi),8:F4} {sub.Average(p=>p.deltaAlign),8:F4} {sub.Count(p=>p.rescued)*100.0/sub.Length,7:F0}%");
        }

        // ─── Rank barriers ───
        _o.WriteLine($"\n─── Barrier ranking ───");
        var barriers=new List<(string label,double score)>();
        // Off-vector: high offVecAngle in low-N vs adaptive
        double offScore=lowN.Average(p=>p.offVecAngle)/Math.Max(1e-9,adaptiveN.Average(p=>p.offVecAngle));
        barriers.Add(("off-vector response",offScore));
        // Basin distance: high distHi in low-N
        double distScore=lowN.Average(p=>p.distHi)/Math.Max(1e-9,adaptiveN.Average(p=>p.distHi));
        barriers.Add(("basin distance",distScore));
        // Insufficient displacement: low dT1 in low-N
        double dispScore=adaptiveN.Average(p=>p.dT1)/Math.Max(1e-9,lowN.Average(p=>p.dT1));
        barriers.Add(("insufficient displacement",dispScore));
        // Weak C3: low deltaAlign in low-N
        double c3Score=adaptiveN.Average(p=>p.deltaAlign)/Math.Max(1e-9,lowN.Average(p=>p.deltaAlign));
        barriers.Add(("weak C3 alignment",c3Score));
        // K collapse
        double kScore=Math.Abs(lowN.Average(p=>p.kCollapse))/Math.Max(1e-9,Math.Abs(adaptiveN.Average(p=>p.kCollapse)));
        barriers.Add(("K collapse",kScore));

        int rank=1;
        foreach(var b in barriers.OrderByDescending(x=>x.score)){
            _o.WriteLine($"{rank}. {b.label}: {b.score:F2}x (higher = more explanatory)");
            rank++;
        }

        // ─── Gate assessment ───
        var top=barriers.OrderByDescending(x=>x.score).First();
        _o.WriteLine($"\nDominant barrier: {top.label} ({top.score:F2}x)");
        _o.WriteLine($"Gate D (Distance dominates): {(top.label=="basin distance"?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate E (Response-direction dominates): {(top.label=="off-vector response"?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate F (Mixed barrier): {(!new[]{"basin distance","off-vector response"}.Contains(top.label)?"assessing":"NOT REACHED — single dominant")}");

        _o.WriteLine($"\n─── Next: LRA_05 Boundary classification ───");
    }

    // ═══════════════════════════════════════════════
    // LRA_05 — Boundary mechanism classification
    // ═══════════════════════════════════════════════
    [Fact]public void LRA_05_BoundaryMechanismClassification(){
        _o.WriteLine("═══ LRA_05: Boundary mechanism classification ═══");

        int[] Ns={50,55,60,62,63,64,65,66,70,72};
        var profiles=new ConcurrentBag<LraProfile>();

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

        // ─── Classification criteria ───
        _o.WriteLine($"\n─── Model classification evidence ───");

        // Model A: Distance barrier — low-N has consistently higher distHi
        var lowN=all.Where(p=>p.n<=64).ToArray();
        var adaptN=all.Where(p=>p.n>=65).ToArray();
        bool distDominates=lowN.Average(p=>p.distHi)>adaptN.Average(p=>p.distHi)*1.2;
        _o.WriteLine($"Model A (Distance barrier): distHi lowN={lowN.Average(p=>p.distHi):F4} vs adapt={adaptN.Average(p=>p.distHi):F4} → {(distDominates?"SUPPORTED":"not supported")}");

        // Model B: Response-vector direction barrier — low-N has higher offVecAngle
        bool offVecDominates=lowN.Average(p=>p.offVecAngle)>adaptN.Average(p=>p.offVecAngle)*1.5;
        _o.WriteLine($"Model B (Response-direction barrier): offAng lowN={lowN.Average(p=>p.offVecAngle):F4} vs adapt={adaptN.Average(p=>p.offVecAngle):F4} → {(offVecDominates?"SUPPORTED":"not supported")}");

        // Model C: Invalid geometry — check invalid rates
        _o.WriteLine($"Model C (Invalid geometry): N=50-63 basin push invalid, N=64 all valid → SUPPORTED for N<64");

        // Model D: d/K response barrier
        bool dkDom=Math.Abs(lowN.Average(p=>p.rebMag)-adaptN.Average(p=>p.rebMag))>0.02;
        _o.WriteLine($"Model D (d/K response barrier): rebMag lowN={lowN.Average(p=>p.rebMag):F4} vs adapt={adaptN.Average(p=>p.rebMag):F4} → {(dkDom?"SUPPORTED":"not supported")}");

        // ─── N-by-N classification ───
        _o.WriteLine($"\n─── Per-N classification ───");
        _o.WriteLine(string.Format("{0,4} {1,20} {2,12} {3,12} {4,12}",
            "N","Model","offAng","distHi","rebMag"));
        foreach(var n in Ns){
            var sub=all.Where(p=>p.n==n).ToArray();
            if(sub.Length==0)continue;
            string model;
            if(sub.Average(p=>p.offVecAngle)>0.12)model="B: response-direction";
            else if(sub.Average(p=>p.distHi)>0.25)model="A: distance barrier";
            else if(sub.Count(p=>p.rescued)>0)model="adaptive (post-onset)";
            else model="F: unresolved";
            _o.WriteLine($"{n,4} {model,-20} {sub.Average(p=>p.offVecAngle),12:F4} {sub.Average(p=>p.distHi),12:F4} {sub.Average(p=>p.rebMag),12:F4}");
        }

        // ─── Final classification ───
        _o.WriteLine($"\n─── Boundary mechanism ───");
        if(offVecDominates&&distDominates)
            _o.WriteLine("Model E: MIXED LOW-N BARRIER — both response-direction AND basin distance contribute");
        else if(offVecDominates)
            _o.WriteLine("Model B: RESPONSE-DIRECTION BARRIER — off-vector response is primary barrier");
        else if(distDominates)
            _o.WriteLine("Model A: DISTANCE BARRIER — basin distance is primary barrier");
        else
            _o.WriteLine("Model F: UNRESOLVED — no single mechanism clearly dominates");

        _o.WriteLine($"\nGate G (Hidden variable): {(offVecDominates||distDominates?"NOT REACHED — explained":"REACHED — unexplained")}");

        _o.WriteLine($"\n─── Next: LRA_06 Response-vector analysis ───");
    }

    // ═══════════════════════════════════════════════
    // LRA_06 — Response-vector trajectory analysis
    // ═══════════════════════════════════════════════
    [Fact]public void LRA_06_ResponseVectorTrajectory(){
        _o.WriteLine("═══ LRA_06: Response-vector trajectory — entry alignment across N ═══");
        _o.WriteLine("Tracking how C3 entry-vector alignment changes across the boundary.");

        int[] Ns={50,55,60,62,63,64,65,66,70,72};
        var profiles=new ConcurrentBag<LraProfile>();

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

        // ─── Entry-vector alignment panel ───
        _o.WriteLine($"\n─── C3 entry-vector alignment ───");
        _o.WriteLine(string.Format("{0,4} {1,10} {2,10} {3,10} {4,10} {5,10} {6,10}",
            "N","alignPre","alignPost","deltaAlign","respProj","respOrth","omegaD"));
        foreach(var n in Ns){
            var sub=all.Where(p=>p.n==n).ToArray();
            if(sub.Length==0)continue;
            _o.WriteLine($"{n,4} {sub.Average(p=>p.entryAlignPre),10:F4} {sub.Average(p=>p.entryAlignPost),10:F4} {sub.Average(p=>p.deltaAlign),10:F4} {sub.Average(p=>p.respProjHi),10:F4} {sub.Average(p=>p.respOrthHi),10:F4} {sub.Average(p=>p.c3OmegaShift),10:F4}");
        }

        // ─── Alignment improvement at boundary ───
        var n64=all.Where(p=>p.n==64).ToArray();
        var n65=all.Where(p=>p.n==65).ToArray();
        if(n64.Length>0&&n65.Length>0){
            _o.WriteLine($"\n─── Boundary alignment shift: N=64→65 ───");
            _o.WriteLine($"alignPre:  {n64.Average(p=>p.entryAlignPre):F4} → {n65.Average(p=>p.entryAlignPre):F4} ({n65.Average(p=>p.entryAlignPre)/Math.Max(1e-9,n64.Average(p=>p.entryAlignPre)):F3}x)");
            _o.WriteLine($"alignPost: {n64.Average(p=>p.entryAlignPost):F4} → {n65.Average(p=>p.entryAlignPost):F4} ({n65.Average(p=>p.entryAlignPost)/Math.Max(1e-9,n64.Average(p=>p.entryAlignPost)):F3}x)");
            _o.WriteLine($"deltaAlign:{n64.Average(p=>p.deltaAlign):F4} → {n65.Average(p=>p.deltaAlign):F4} ({n65.Average(p=>p.deltaAlign)/Math.Max(1e-9,n64.Average(p=>p.deltaAlign)):F3}x)");
            _o.WriteLine($"c3OmegaShift: {n64.Average(p=>p.c3OmegaShift):F4} → {n65.Average(p=>p.c3OmegaShift):F4}");
        }

        // ─── Alignment vs rescue correlation ───
        _o.WriteLine($"\n─── Alignment vs rescue by N ───");
        foreach(var n in Ns){
            var sub=all.Where(p=>p.n==n).ToArray();
            if(sub.Length==0)continue;
            var rescued=sub.Where(p=>p.rescued).ToArray();
            var failed=sub.Where(p=>!p.rescued).ToArray();
            _o.WriteLine($"N={n}: rescued alignPre={(rescued.Length>0?rescued.Average(p=>p.entryAlignPre):0):F4} failed={(failed.Length>0?failed.Average(p=>p.entryAlignPre):0):F4} deltaAlign rescued={(rescued.Length>0?rescued.Average(p=>p.deltaAlign):0):F4} failed={(failed.Length>0?failed.Average(p=>p.deltaAlign):0):F4}");
        }

        _o.WriteLine($"\n─── Analysis complete ───");
    }

    // ═══════════════════════════════════════════════
    // Profile builder
    // ═══════════════════════════════════════════════

    LraProfile? BuildProfile(int n,int s,P3 hi,P3 lo){
        var p=new LraProfile{n=n,s=s,cohort=s/100};

        // T0: Characterize candidate
        var sb=SelectAndClassify(n,s,hi);
        if(sb==null)return null;
        p.cls=sb.Value.cls;
        double d0=sb.Value.d0,km0=sb.Value.km0,ks0=sb.Value.ks0;

        // Compute entry vector from Lo to Hi
        double dv=hi.dm-lo.dm,kv=hi.km-lo.km,sv=hi.ks-lo.ks;
        double vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);

        // Candidate projection onto Hi-entry vector
        p.projHiVec=vn>0?((d0-lo.dm)*dv+(km0-lo.km)*kv+(ks0-lo.ks)*sv)/vn:0;
        double d2=(d0-lo.dm)*(d0-lo.dm)+(km0-lo.km)*(km0-lo.km)+(ks0-lo.ks)*(ks0-lo.ks);
        p.orthHiVec=Math.Sqrt(Math.Max(0,d2-p.projHiVec*p.projHiVec));

        // Off-vector angle: angle between candidate position and Hi-entry direction
        double candNorm=Math.Sqrt(d2);
        p.offVecAngle=candNorm>1e-9&&vn>1e-9?Math.Acos(Math.Clamp(Math.Abs(p.projHiVec)/candNorm,-1,1)):Math.PI/2;

        // Distances
        p.distHi=Math.Sqrt((d0-hi.dm)*(d0-hi.dm)+(km0-hi.km)*(km0-hi.km)+(ks0-hi.ks)*(ks0-hi.ks));
        p.distLo=Math.Sqrt((d0-lo.dm)*(d0-lo.dm)+(km0-lo.km)*(km0-lo.km)+(ks0-lo.ks)*(ks0-lo.ks));

        // T0 d/K/Omega
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

        // T1 response direction
        double dT1m=p.dT1,kT1m=p.kT1,ksT1m=Ks(Cupd(dT1,n),n);
        p.respProjHi=vn>0?((dT1m-lo.dm)*dv+(kT1m-lo.km)*kv+(ksT1m-lo.ks)*sv)/vn:0;
        double respD2=(dT1m-lo.dm)*(dT1m-lo.dm)+(kT1m-lo.km)*(kT1m-lo.km)+(ksT1m-lo.ks)*(ksT1m-lo.ks);
        p.respOrthHi=Math.Sqrt(Math.Max(0,respD2-p.respProjHi*p.respProjHi));
        double respNorm=Math.Sqrt(respD2);
        p.respDirAngle=respNorm>1e-9&&vn>1e-9?Math.Acos(Math.Clamp(Math.Abs(p.respProjHi)/respNorm,-1,1)):Math.PI/2;

        // Entry-vector alignment pre-C3
        p.entryAlignPre=vn>0?((dT1m-lo.dm)*dv+(kT1m-lo.km)*kv)/vn:0;

        // T2 continuation
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);
        var dT2=DL(Nm(RP(hT2,n),n),n);p.dT2=Dm(dT2,n);p.kT2=Km(Cupd(dT2,n),n);p.omT2=Of(hT2,n).Average();
        p.rebMag=p.dT2-p.dT1;p.kCollapse=p.kT2-p.kT1;p.dRebound=p.dT2-p.dT1;

        p.a0=p.omT2>THR;
        p.c3=p.a0;

        // C3 correction
        if(p.rebMag<RebT&&!p.a0){
            var dmat3=DL(Nm(RP(hT1,n),n),n);double dmPre=Dm(dmat3,n);double kmPre=Km(Cupd(dmat3,n),n);
            double nudged=dmPre+(hi.dm-dmPre)*0.2;
            double f3=Math.Clamp((nudged+1e-9)/(dmPre+1e-9),0.5,1.5);
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;
            var Kc3=Cupd(dmat3,n);
            var hc3=Sim(Kc3,n,S,s+300);
            var Kc3c=Cupd(DL(Nm(RP(hc3,n),n),n),n);
            var hc3cc=Sim(Kc3c,n,S,s+400);
            double omC3=Of(hc3cc,n).Average();
            double dmPost=Dm(dmat3,n),kmPost=Km(Cupd(dmat3,n),n);

            p.c3OmegaShift=omC3-p.omT2;
            p.c3DMeanShift=dmPost-dmPre;
            p.c3KMeanShift=kmPost-kmPre;
            p.entryAlignPost=vn>0?((dmPost-lo.dm)*dv+(kmPost-lo.km)*kv)/vn:0;
            p.deltaAlign=p.entryAlignPost-p.entryAlignPre;
            p.c3=omC3>THR;
        }else{
            p.c3OmegaShift=0;p.c3DMeanShift=0;p.c3KMeanShift=0;
            p.entryAlignPost=p.entryAlignPre;p.deltaAlign=0;
        }

        p.rescued=p.c3&&!p.a0;
        p.induced=p.a0||p.c3;

        // State group
        if(p.n<=64&&!p.induced)p.stateGroup="S"+(p.n==64?"2":"1");
        else if(p.n==65&&p.rescued)p.stateGroup="S3";
        else if(p.n==65&&!p.induced)p.stateGroup="S4";
        else if(p.n>=70&&p.rescued)p.stateGroup="S5";
        else if(p.n>=70&&!p.induced)p.stateGroup="S6";
        else p.stateGroup="S0";

        // Failure label
        if(p.induced)p.failureLabel="none-induced";
        else if(p.offVecAngle>0.12)p.failureLabel="off-vector";
        else if(p.rebMag<RebT)p.failureLabel="no-rebMag";
        else if(p.distHi>0.25)p.failureLabel="dist-Hi";
        else if(Math.Abs(p.kCollapse)>0.05)p.failureLabel="K-collapse";
        else if(Math.Abs(p.dRebound)>0.05)p.failureLabel="d-rebound";
        else p.failureLabel="unresolved";

        return p;
    }

    // ─── Metric accessor ───
    static double GetMetric(LraProfile[] profiles,string metric)=>metric switch{
        "distHi"=>profiles.Average(p=>p.distHi),
        "projHiVec"=>profiles.Average(p=>p.projHiVec),
        "offVecAngle"=>profiles.Average(p=>p.offVecAngle),
        "dT1"=>profiles.Average(p=>p.dT1),
        "dT2"=>profiles.Average(p=>p.dT2),
        "rebMag"=>profiles.Average(p=>p.rebMag),
        "respProjHi"=>profiles.Average(p=>p.respProjHi),
        "respOrthHi"=>profiles.Average(p=>p.respOrthHi),
        "deltaAlign"=>profiles.Average(p=>p.deltaAlign),
        "kCollapse"=>profiles.Average(p=>p.kCollapse),
        "dRebound"=>profiles.Average(p=>p.dRebound),
        "entryAlignPre"=>profiles.Average(p=>p.entryAlignPre),
        "entryAlignPost"=>profiles.Average(p=>p.entryAlignPost),
        "c3OmegaShift"=>profiles.Average(p=>p.c3OmegaShift),
        _=>0
    };

    static double GetMetricVal(LraProfile p,string metric)=>metric switch{
        "offVecAngle"=>p.offVecAngle,"distHi"=>p.distHi,"dT1"=>p.dT1,
        "rebMag"=>p.rebMag,"respProjHi"=>p.respProjHi,"respOrthHi"=>p.respOrthHi,
        "deltaAlign"=>p.deltaAlign,"kCollapse"=>Math.Abs(p.kCollapse),"dRebound"=>Math.Abs(p.dRebound),_=>0
    };

    static double Percentile(double[] sorted,double pct){
        if(sorted.Length==0)return 0;
        int idx=(int)(pct*(sorted.Length-1));
        return sorted[Math.Clamp(idx,0,sorted.Length-1)];
    }

    // ─── Frozen M3++ helpers (same as V5.20/LRE) ───

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
    static P3 PCent(int n,bool hi){double d=0,k=0,ks=0;int c=0;for(int sd=0;sd<100;sd++){var K=KS(n,sd);double dm=0,km=0,kss=0;for(int e=0;e<5;e++){var h=Sim(K,n,S,sd+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}double om=Of(Sim(K,n,S,sd+5),n).Average();if((om>THR)==hi){d+=dm/5;k+=km/5;ks+=kss/5;c++;}}return new P3{dm=d/c,km=k/c,ks=ks/c};}
    static bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<5;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,S,seed+5),n).Average()>THR;}
    static SBase Classify(SBase b,P3 hi){string cls;if(b.d0>0.65)cls="P1b";else if(b.d0>0.50)cls="P1";else if(b.d0<=0.40&&b.km0>0.98&&DistK(b.km0,b.ks0,hi)<0.15)cls="P2";else if(b.d0>0.40&&b.d0<=0.50)cls="P3";else cls="P4";return new SBase{seed=b.seed,d0=b.d0,km0=b.km0,ks0=b.ks0,cls=cls};}
    static double DistK(double km,double ks,P3 hi){double dk=km-hi.km,dks=ks-hi.ks;return Math.Sqrt(dk*dk+dks*dks);}
}
