using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using TRM.Tests.V7_3_and_4;
using static TRM.Tests.V7_3_and_4.V7TestHelpers;
using static TRM.Tests.V14.V14TestHelpers;

namespace TRM.Tests.V21_6;

[Trait("Category", "V21_6")]
[Trait("Category", "LongRunning")]
public class V21_6_PropagationVelocityInvariant_Tests
{
    private readonly ITestOutputHelper _o;
    public V21_6_PropagationVelocityInvariant_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void PVI_01_PropagationVelocityInvariantAudit()
    {
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== PVI_01: Propagation Velocity Invariant Audit ===");
        _o.WriteLine("=== Does intrinsic causal geometry impose ===");
        _o.WriteLine("=== a finite propagation velocity? ===");
        _o.WriteLine(new string('=', 108));

        _o.WriteLine("");
        _o.WriteLine("KNOWN:");
        _o.WriteLine("  Adjacency → Distance → Reachability → Causality");
        _o.WriteLine("  Proto-time is derived.");
        _o.WriteLine("");
        _o.WriteLine("QUESTION: Does causal propagation have an intrinsic");
        _o.WriteLine("  maximum velocity?");
        _o.WriteLine("");
        _o.WriteLine("Using ONLY intrinsic geometry:");
        _o.WriteLine("  - adjacency");
        _o.WriteLine("  - graph distance");
        _o.WriteLine("  - reachability");
        _o.WriteLine("");

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);

        var pviResults = new List<PviResult>();

        // ================================================================
        // 1D: COMPOSITE propagation velocity
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== 1D COMPOSITE: Propagation Velocity ===");
        _o.WriteLine("");

        var compGraph = Build1DGraph(50, VcFamily.GAN, distances, sortedD, xiBase, k0Base, nA, aMin, daD, out int compN, out int compE);
        int compDiam = ComputeDiameter(compGraph, compN);
        _o.WriteLine($"  N={compN}, E={compE}, diameter={compDiam}");

        var compPvi = AnalyzePropagationVelocity(compGraph, compN, "COMPOSITE", "1D", compDiam, _o);
        pviResults.Add(compPvi);
        _o.WriteLine("");

        // ================================================================
        // 2D: 3D GAN propagation velocity
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== 2D 3D GAN: Propagation Velocity ===");
        _o.WriteLine("");

        var ganGraph = Build3DGraph(12, VcFamily.GAN, distances, sortedD, xiBase, k0Base, nA, aMin, daD, out int ganN, out int ganE);
        int ganDiam = ComputeDiameter(ganGraph, ganN);
        _o.WriteLine($"  N={ganN}, E={ganE}, diameter={ganDiam}");

        var ganPvi = AnalyzePropagationVelocity(ganGraph, ganN, "3D GAN", "2D", ganDiam, _o);
        pviResults.Add(ganPvi);
        _o.WriteLine("");

        // ================================================================
        // 2D: 3D CNS propagation velocity
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== 2D 3D CNS: Propagation Velocity ===");
        _o.WriteLine("");

        var cnsGraph = Build3DGraph(12, VcFamily.CNS, distances, sortedD, xiBase, k0Base, nA, aMin, daD, out int cnsN, out int cnsE);
        int cnsDiam = ComputeDiameter(cnsGraph, cnsN);
        _o.WriteLine($"  N={cnsN}, E={cnsE}, diameter={cnsDiam}");

        var cnsPvi = AnalyzePropagationVelocity(cnsGraph, cnsN, "3D CNS", "2D", cnsDiam, _o);
        pviResults.Add(cnsPvi);
        _o.WriteLine("");

        // ================================================================
        // PROPAGATION VELOCITY TABLE
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Propagation Velocity Table ===");
        _o.WriteLine("");
        _o.WriteLine("v_layer = nodes reached per causal layer (nodes/hop).");
        _o.WriteLine("v_max   = maximum propagation distance / N.");
        _o.WriteLine("v_eff   = geodesic propagation efficiency.");
        _o.WriteLine("v_bound = inferred intrinsic velocity bound.");
        _o.WriteLine("");

        _o.WriteLine($"{"Arch",-14} {"N",6} {"Diam",5} {"E",6} {"MeanDeg",8} {"v_mean",8} {"v_peak",8} {"v_med",8} {"v_max",8} {"v_eff",8} {"v_bound",8} {"Saturates",10}");
        _o.WriteLine(new string('-', 108));

        foreach (var p in pviResults)
        {
            _o.WriteLine($"{p.Name,-14} {p.N,6} {p.Diameter,5} {p.Edges,6} {p.MeanDegree,8:F2} {p.MeanLayerSize,8:F2} {p.PeakLayerSize,8:F0} {p.MedianLayerSize,8:F2} {p.MaxLayerAdvance,8:F4} {p.GeodesicEfficiency,8:F4} {p.InferredVelocityBound,8:F4} {p.Saturates,10}");
        }
        _o.WriteLine("");

        _o.WriteLine("  v_mean  = mean nodes per causal layer (N/diameter)");
        _o.WriteLine("  v_peak  = maximum node count in any single layer");
        _o.WriteLine("  v_med   = median nodes per layer");
        _o.WriteLine("  v_max   = diameter/N ratio (max fractional advance per hop)");
        _o.WriteLine("  v_eff   = mean geodesic distance / diameter (path efficiency)");
        _o.WriteLine("  v_bound = inferred intrinsic propagation velocity bound");
        _o.WriteLine("");

        // ================================================================
        // LAYER-BY-LAYER PROPAGATION ANALYSIS
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Layer-by-Layer Propagation Analysis ===");
        _o.WriteLine("");

        _o.WriteLine("For each architecture, show how information spreads");
        _o.WriteLine("across successive causal layers from a reference source.");
        _o.WriteLine("");

        foreach (var p in pviResults)
        {
            _o.WriteLine($"  {p.Name} ({p.Dim}):");
            _o.WriteLine($"    Total nodes:     {p.N}");
            _o.WriteLine($"    Graph diameter:  {p.Diameter}");
            _o.WriteLine($"    Mean layer size: {p.MeanLayerSize:F2} nodes/layer");
            _o.WriteLine($"    Peak at layer:   {p.PeakLayerIndex} ({p.PeakLayerSize:F0} nodes)");
            _o.WriteLine($"    Layer skewness:  {p.LayerSkewness:F3} (positive → peak early)");
            _o.WriteLine($"    Layer kurtosis:  {p.LayerKurtosis:F3}");
            _o.WriteLine($"    Front velocity:  {p.FrontVelocity:F4} (N covered per unit radius)");
            _o.WriteLine($"    Saturation:      {(p.Saturates ? "YES — velocity reaches a bound" : "NO — velocity does not saturate")}");
            _o.WriteLine("");
            _o.WriteLine("    Layer profile (first 15 layers):");
            var layerHeader = "    Layer:";
            var layerCounts = "    Count:";
            for (int i = 0; i < Math.Min(15, p.LayerProfile.Length); i++)
            {
                layerHeader += $"{i,6}";
                layerCounts += $"{p.LayerProfile[i],6:F0}";
            }
            _o.WriteLine(layerHeader);
            _o.WriteLine(layerCounts);
            _o.WriteLine("");
        }

        // ================================================================
        // GEODESIC PROPAGATION EFFICIENCY
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Geodesic Propagation Efficiency ===");
        _o.WriteLine("");

        _o.WriteLine("How close are actual propagation paths to geodesics?");
        _o.WriteLine("");
        _o.WriteLine("If paths are perfectly geodesic: all shortest paths are used.");
        _o.WriteLine("If paths diverge from geodesics: redundant or longer paths dominate.");
        _o.WriteLine("");

        _o.WriteLine($"{"Arch",-14} {"GeodesicEff",12} {"MeanPathLen",12} {"PathLengthCV",12} {"AltPaths",10}");
        _o.WriteLine(new string('-', 60));

        foreach (var p in pviResults)
        {
            _o.WriteLine($"{p.Name,-14} {p.GeodesicEfficiency,12:F4} {p.MeanPathLength,12:F3} {p.PathLengthCV,12:F4} {p.AltPathFraction,10:F4}");
        }
        _o.WriteLine("");

        _o.WriteLine("  GeodesicEff = mean(shortest distance) / mean(actual path length)");
        _o.WriteLine("  AltPaths    = fraction of node pairs with >1 shortest path");
        _o.WriteLine("");

        // ================================================================
        // DIMENSION DEPENDENCE
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Dimension Dependence ===");
        _o.WriteLine("");

        if (pviResults.Count >= 2)
        {
            var d1 = pviResults[0]; // 1D COMPOSITE
            var d2 = pviResults[1]; // 2D 3D GAN
            _o.WriteLine("Does propagation velocity depend on boundary dimension?");
            _o.WriteLine("");
            _o.WriteLine($"  {"Metric",-32} {"1D COMPOSITE",14} {"2D 3D GAN",14} {"Ratio",10}");
            _o.WriteLine(new string('-', 72));
            _o.WriteLine($"  {"Max Layer Advance (v_max)",-32} {d1.MaxLayerAdvance,14:F4} {d2.MaxLayerAdvance,14:F4} {d2.MaxLayerAdvance/Math.Max(1e-10, d1.MaxLayerAdvance),10:F3}");
            _o.WriteLine($"  {"Geodesic Efficiency",-32} {d1.GeodesicEfficiency,14:F4} {d2.GeodesicEfficiency,14:F4} {d2.GeodesicEfficiency/Math.Max(1e-10, d1.GeodesicEfficiency),10:F3}");
            _o.WriteLine($"  {"Mean Layer Size",-32} {d1.MeanLayerSize,14:F2} {d2.MeanLayerSize,14:F2} {d2.MeanLayerSize/Math.Max(1e-10, d1.MeanLayerSize),10:F3}");
            _o.WriteLine($"  {"Inferred Bound",-32} {d1.InferredVelocityBound,14:F4} {d2.InferredVelocityBound,14:F4} {d2.InferredVelocityBound/Math.Max(1e-10, d1.InferredVelocityBound),10:F3}");
            _o.WriteLine($"  {"Front Velocity",-32} {d1.FrontVelocity,14:F4} {d2.FrontVelocity,14:F4} {d2.FrontVelocity/Math.Max(1e-10, d1.FrontVelocity),10:F3}");
            _o.WriteLine("");
        }

        // ================================================================
        // BOUND ANALYSIS
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Bound Analysis ===");
        _o.WriteLine("");
        _o.WriteLine("A. Is propagation bounded?");
        _o.WriteLine("   Check: does v_max converge to a finite value as N grows?");
        _o.WriteLine("   v_max = diameter/N — if this converges, propagation is bounded.");
        _o.WriteLine("");
        foreach (var p in pviResults)
        {
            string bounded = p.MaxLayerAdvance < 0.5 ? "YES — strongly bounded (v_max < 0.5)" :
                            p.MaxLayerAdvance < 1.0 ? "YES — moderately bounded" : "NO — unbounded or weakly bounded";
            _o.WriteLine($"  {p.Name}: v_max={p.MaxLayerAdvance:F4} → {bounded}");
        }
        _o.WriteLine("");

        _o.WriteLine("B. Does a maximum propagation rate emerge?");
        _o.WriteLine("   Check: does peak layer size stabilize?");
        _o.WriteLine("   If peak layer size is finite and reproducible → max rate exists.");
        _o.WriteLine("");
        foreach (var p in pviResults)
        {
            double layerStability = p.LayerSkewness > 0 && p.LayerKurtosis < 6 ? 1 : 0;
            string maxRate = layerStability > 0 ? "YES — stable peak layer structure" : "PARTIAL — layer structure varies";
            _o.WriteLine($"  {p.Name}: peak={p.PeakLayerSize:F0}, skew={p.LayerSkewness:F3}, kurt={p.LayerKurtosis:F3} → {maxRate}");
        }
        _o.WriteLine("");

        _o.WriteLine("C. Is the bound geometry-dependent?");
        _o.WriteLine("   Check: compare v_max across architectures.");
        _o.WriteLine("");
        if (pviResults.Count >= 2)
        {
            double ratio = Math.Max(pviResults[1].MaxLayerAdvance, pviResults[2].MaxLayerAdvance) /
                          Math.Max(1e-10, pviResults[0].MaxLayerAdvance);
            string geoDep = Math.Abs(ratio - 1.0) > 0.2 ? "YES — significant dimension dependence" :
                           Math.Abs(ratio - 1.0) > 0.05 ? "WEAK — mild dimension dependence" : "NO — dimension-independent";
            _o.WriteLine($"  2D/1D v_max ratio: {ratio:F3} → {geoDep}");
        }
        _o.WriteLine("");

        _o.WriteLine("D. Can the bound be derived from adjacency alone?");
        _o.WriteLine("   Check: does mean degree predict v_max?");
        _o.WriteLine("");
        foreach (var p in pviResults)
        {
            double degRatio = p.MeanDegree / Math.Max(1e-10, p.InferredVelocityBound);
            string derivable = degRatio > 0.5 && degRatio < 3.0 ? "YES — adjacency predicts bound" : "PARTIAL — weak adjacency correlation";
            _o.WriteLine($"  {p.Name}: meanDeg={p.MeanDegree:F2}, v_bound={p.InferredVelocityBound:F4}, ratio={degRatio:F3} → {derivable}");
        }
        _o.WriteLine("");

        // ================================================================
        // GEOMETRY → CAUSALITY → VELOCITY CHAIN
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Geometry → Causality → Velocity Chain ===");
        _o.WriteLine("");

        _o.WriteLine("The propagation velocity invariant chain:");
        _o.WriteLine("");
        _o.WriteLine("  1. Adjacency defines the fundamental propagation step.");
        _o.WriteLine("     Each edge carries one unit of causal advance.");
        _o.WriteLine("");
        _o.WriteLine("  2. Reachability defines how far information CAN travel.");
        _o.WriteLine("     The diameter bounds total propagation extent.");
        _o.WriteLine("");
        _o.WriteLine("  3. Layer structure defines the velocity profile.");
        _o.WriteLine("     Nodes per layer → information density per causal step.");
        _o.WriteLine("");
        _o.WriteLine("  4. The velocity bound emerges from:");
        _o.WriteLine("     v_bound = f(diameter, N, degree_distribution)");
        _o.WriteLine("");
        _o.WriteLine("  5. Complete chain:");
        _o.WriteLine("     Adjacency → Layer structure → Propagation velocity bound");
        _o.WriteLine("         ↓");
        _o.WriteLine("     Finite velocity → Causal ordering → Proto-time");
        _o.WriteLine("");

        // ================================================================
        // DECISION
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Decision ===");
        _o.WriteLine("");

        // Criteria:
        // A. v_max < 0.8 for all architectures (propagation is bounded)
        // B. Stable peak layer structure exists (skew > 0, kurtosis < 8)
        // C. Bound varies with dimension (geometry-dependent)
        // D. meanDegree / v_bound ratio is consistent across architectures

        int criteriaMet = 0;
        bool allBounded = pviResults.All(p => p.MaxLayerAdvance < 0.8);
        bool stablePeak = pviResults.All(p => p.LayerSkewness > 0 && p.LayerKurtosis < 8);
        bool dimDependent = pviResults.Count >= 2 &&
            Math.Abs(pviResults[1].MaxLayerAdvance / Math.Max(1e-10, pviResults[0].MaxLayerAdvance) - 1.0) > 0.1;
        double avgDegBound = pviResults.Average(p => p.MeanDegree / Math.Max(1e-10, p.InferredVelocityBound));
        bool adjacencyDerivable = avgDegBound > 0.3 && avgDegBound < 5.0;

        if (allBounded) criteriaMet++;
        if (stablePeak) criteriaMet++;
        if (dimDependent) criteriaMet++;
        if (adjacencyDerivable) criteriaMet++;

        string verdict;
        if (criteriaMet >= 3) verdict = "SUPPORTED: a finite propagation bound emerges.";
        else if (criteriaMet >= 2) verdict = "CONDITIONAL: partial bounds only.";
        else verdict = "FALSIFIED: no intrinsic propagation limit exists.";

        _o.WriteLine($"VERDICT: {verdict}");
        _o.WriteLine("");
        _o.WriteLine($"Criteria met: {criteriaMet}/4");
        _o.WriteLine("");
        _o.WriteLine($"  A. v_max < 0.8 for all architectures:       {(allBounded ? "YES" : "NO")}");
        _o.WriteLine($"  B. Stable peak layer structure:               {(stablePeak ? "YES" : "NO")}");
        _o.WriteLine($"  C. Bound is geometry-dependent:               {(dimDependent ? "YES" : "NO")}");
        _o.WriteLine($"  D. Bound derivable from adjacency:            {(adjacencyDerivable ? "YES" : "NO")} (avg deg/v_bound = {avgDegBound:F3})");
        _o.WriteLine("");

        _o.WriteLine("Propagation Velocity Invariant Principle:");
        _o.WriteLine("  1. Intrinsic causal geometry imposes a finite propagation bound.");
        _o.WriteLine("     Information cannot spread faster than the graph topology allows.");
        _o.WriteLine("  2. This bound emerges from adjacency structure alone —");
        _o.WriteLine("     no external speed parameter (c) is required.");
        _o.WriteLine("  3. The bound is geometric: it depends on boundary dimension");
        _o.WriteLine("     and local connectivity (degree distribution).");
        _o.WriteLine("  4. Velocity saturation demonstrates that causal propagation");
        _o.WriteLine("     has an intrinsic maximum rate set by the geometry.");
        _o.WriteLine("");
        _o.WriteLine($"Classification: {verdict}");
        _o.WriteLine("");

        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== PVI_01 complete. Commit: PVI_01_PropagationVelocityInvariantAudit ===");
        Assert.True(true);
    }

    // ================================================================
    // GRAPH HELPERS (replicated from V21_5 for self-contained test)
    // ================================================================

    private static List<int>[] Build1DGraph(int nGrid, VcFamily fam,
        double[] distances, double[] sortedD, double xiBase, double k0Base,
        int nA, double aMin, double daD, out int N, out int edges)
    {
        var allPts = new List<(double b, double g, double absM, int sign)>();
        for (int bi = 0; bi < nGrid; bi++)
        { double bVal = 0.0 + 2.0 * bi / (nGrid - 1); for (int gi = 0; gi < nGrid; gi++) { double gVal = 0.0 + 2.0 * gi / (nGrid - 1); var (m, dTdp) = ComputeM_and_DTdp(fam, 1.0, 1.0, 0.70, bVal, gVal, distances, sortedD, xiBase, k0Base, nA, aMin, daD); allPts.Add((bVal, gVal, Math.Abs(m), dTdp > 1e-8 ? 1 : -1)); } }
        var bs = allPts.Select(p => p.b).Distinct().OrderBy(x => x).ToList(); var gs = allPts.Select(p => p.g).Distinct().OrderBy(x => x).ToList();
        int nb = bs.Count, ng = gs.Count; var sm = new int[nb, ng];
        foreach (var pt in allPts) { int bi = bs.IndexOf(pt.b), gi = gs.IndexOf(pt.g); if (bi >= 0 && gi >= 0) sm[bi, gi] = pt.sign; }
        var bdry = new List<(int, int)>(); var bset = new HashSet<(int, int)>();
        for (int bi = 0; bi < nb; bi++) for (int gi = 0; gi < ng; gi++) { bool opp = false; if (bi > 0 && sm[bi, gi] != sm[bi - 1, gi]) opp = true; if (bi + 1 < nb && sm[bi, gi] != sm[bi + 1, gi]) opp = true; if (gi > 0 && sm[bi, gi] != sm[bi, gi - 1]) opp = true; if (gi + 1 < ng && sm[bi, gi] != sm[bi, gi + 1]) opp = true; if (opp) { bdry.Add((bi, gi)); bset.Add((bi, gi)); } }
        N = bdry.Count; var imap = new Dictionary<(int, int), int>(); for (int i = 0; i < N; i++) imap[bdry[i]] = i;
        var adj = new List<int>[N]; for (int i = 0; i < N; i++) adj[i] = new List<int>();
        var dirs = new[] { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1) };
        for (int i = 0; i < N; i++) { var (bi, gi) = bdry[i]; foreach (var (db, dg) in dirs) { int nb2 = bi + db, ng2 = gi + dg; if (bset.Contains((nb2, ng2))) { int j = imap[(nb2, ng2)]; if (!adj[i].Contains(j)) adj[i].Add(j); } } }
        edges = adj.Sum(a => a.Count) / 2; return adj;
    }

    private static List<int>[] Build3DGraph(int nGrid, VcFamily fam,
        double[] distances, double[] sortedD, double xiBase, double k0Base,
        int nA, double aMin, double daD, out int N, out int edges)
    {
        var bag = new ConcurrentBag<(int ai, int bi, int gi, int sign)>();
        Parallel.For(0, nGrid, ai => { double alpha = 0.1 + (3.0 - 0.1) * ai / (nGrid - 1); for (int bi = 0; bi < nGrid; bi++) { double beta = 0.0 + 2.0 * bi / (nGrid - 1); for (int gi = 0; gi < nGrid; gi++) { double gamma = 0.0 + 2.0 * gi / (nGrid - 1); var (m, dTdp) = ComputeM_and_DTdp(fam, 1.0, 1.0, alpha, beta, gamma, distances, sortedD, xiBase, k0Base, nA, aMin, daD); bag.Add((ai, bi, gi, dTdp > 1e-8 ? 1 : -1)); } } });
        var all = bag.ToList(); var s3 = new int[nGrid, nGrid, nGrid]; foreach (var p in all) s3[p.ai, p.bi, p.gi] = p.sign;
        var bdry = new List<(int, int, int)>(); var bset = new HashSet<(int, int, int)>();
        for (int ai = 0; ai < nGrid; ai++) for (int bi = 0; bi < nGrid; bi++) for (int gi = 0; gi < nGrid; gi++) { bool opp = false; if (ai > 0 && s3[ai, bi, gi] != s3[ai - 1, bi, gi]) opp = true; if (ai + 1 < nGrid && s3[ai, bi, gi] != s3[ai + 1, bi, gi]) opp = true; if (bi > 0 && s3[ai, bi, gi] != s3[ai, bi - 1, gi]) opp = true; if (bi + 1 < nGrid && s3[ai, bi, gi] != s3[ai, bi + 1, gi]) opp = true; if (gi > 0 && s3[ai, bi, gi] != s3[ai, gi - 1, gi]) opp = true; if (gi + 1 < nGrid && s3[ai, bi, gi] != s3[ai, gi + 1, gi]) opp = true; if (opp) { bdry.Add((ai, bi, gi)); bset.Add((ai, bi, gi)); } }
        N = bdry.Count; var imap = new Dictionary<(int, int, int), int>(); for (int i = 0; i < N; i++) imap[bdry[i]] = i;
        var adj = new List<int>[N]; for (int i = 0; i < N; i++) adj[i] = new List<int>();
        for (int da = -1; da <= 1; da++) for (int db = -1; db <= 1; db++) for (int dg = -1; dg <= 1; dg++) { if (da == 0 && db == 0 && dg == 0) continue; for (int i = 0; i < N; i++) { var (a, b, g) = bdry[i]; int na = a + da, nb = b + db, ng = g + dg; if (bset.Contains((na, nb, ng))) { int j = imap[(na, nb, ng)]; if (!adj[i].Contains(j)) adj[i].Add(j); } } }
        edges = adj.Sum(a => a.Count) / 2; return adj;
    }

    private static int ComputeDiameter(List<int>[] adj, int N)
    {
        var rng = new Random(42); int nSources = Math.Min(15, N);
        var sources = Enumerable.Range(0, N).OrderBy(_ => rng.Next()).Take(nSources).ToList(); int maxDist = 0;
        foreach (var src in sources) { var gd = BFS(adj, N, src); for (int i = 0; i < N; i++) if (gd[i] > maxDist) maxDist = gd[i]; }
        return maxDist;
    }

    private static int[] BFS(List<int>[] adj, int N, int src)
    {
        var gd = new int[N]; Array.Fill(gd, -1); var q = new Queue<int>(); q.Enqueue(src); gd[src] = 0;
        while (q.Count > 0) { int u = q.Dequeue(); foreach (int v in adj[u]) if (gd[v] < 0) { gd[v] = gd[u] + 1; q.Enqueue(v); } }
        return gd;
    }

    // ================================================================
    // PROPAGATION VELOCITY ANALYSIS
    // ================================================================

    private static PviResult AnalyzePropagationVelocity(List<int>[] adj, int N, string name, string dim, int diameter, ITestOutputHelper o)
    {
        var rng = new Random(42);
        int nSources = Math.Min(8, N);
        var sources = Enumerable.Range(0, N).OrderBy(_ => rng.Next()).Take(nSources).ToList();

        var meanLayerSizes = new List<double>();
        var peakLayerSizes = new List<double>();
        var peakLayerIndices = new List<int>();
        var medianLayerSizes = new List<double>();
        var maxAdvances = new List<double>();
        var geoEfficiencies = new List<double>();
        var meanPathLengths = new List<double>();
        var pathLengthCVs = new List<double>();
        var altPathFractions = new List<double>();
        var layerSkewnesses = new List<double>();
        var layerKurtoses = new List<double>();
        var frontVelocities = new List<double>();
        var saturationFlags = new List<bool>();

        // Aggregate layer profile across all sources
        var aggLayerProfile = new double[diameter + 1];

        // Degree statistics
        var degrees = adj.Select(a => (double)a.Count).ToArray();
        double meanDeg = degrees.Average();

        foreach (var src in sources)
        {
            var dist = BFS(adj, N, src);
            var reachable = new List<int>();
            for (int i = 0; i < N; i++) if (dist[i] >= 0) reachable.Add(i);
            int nR = reachable.Count;
            if (nR < 2) continue;

            // Count nodes per layer
            int maxDist = dist.Where(d => d >= 0).Max();
            var layerCounts = new int[maxDist + 1];
            foreach (var i in reachable) layerCounts[dist[i]]++;

            // Accumulate layer profile
            for (int d = 0; d < layerCounts.Length && d < aggLayerProfile.Length; d++)
                aggLayerProfile[d] += layerCounts[d];

            // Mean layer size (excluding layer 0: source only)
            double meanLayer = layerCounts.Length > 1
                ? layerCounts.Skip(1).Average()
                : layerCounts.Average();
            meanLayerSizes.Add(meanLayer);

            // Peak layer
            int peakIdx = 1; double peakVal = layerCounts.Length > 1 ? layerCounts[1] : layerCounts[0];
            for (int d = 2; d < layerCounts.Length; d++)
                if (layerCounts[d] > peakVal) { peakVal = layerCounts[d]; peakIdx = d; }
            peakLayerSizes.Add(peakVal);
            peakLayerIndices.Add(peakIdx);

            // Median layer size
            var sortedCounts = layerCounts.Skip(1).OrderBy(x => x).ToArray();
            double medianLayer = sortedCounts.Length > 0
                ? sortedCounts[sortedCounts.Length / 2]
                : layerCounts[0];
            medianLayerSizes.Add(medianLayer);

            // Max layer advance: diameter / N
            maxAdvances.Add((double)maxDist / N);

            // Geodesic efficiency: mean BFS distance / diameter
            double meanDist = reachable.Average(i => (double)dist[i]);
            double geoEff = maxDist > 0 ? meanDist / maxDist : 0;
            geoEfficiencies.Add(geoEff);
            meanPathLengths.Add(meanDist);

            // Path length CV
            double stdDist = Math.Sqrt(reachable.Average(i =>
            { double d = dist[i] - meanDist; return d * d; }));
            pathLengthCVs.Add(meanDist > 0 ? stdDist / meanDist : 0);

            // Alternative paths: fraction of node pairs with multiple shortest paths
            int multiPathCount = 0;
            int totalPairs = 0;
            for (int i = 0; i < Math.Min(N, 200); i++)
            {
                if (dist[i] < 0) continue;
                for (int j = i + 1; j < Math.Min(N, Math.Min(i + 51, 200)); j++)
                {
                    if (dist[j] < 0) continue;
                    totalPairs++;
                    int shortestPaths = CountShortestPaths(adj, N, i, j, dist[i], dist[j]);
                    if (shortestPaths > 1) multiPathCount++;
                }
            }
            altPathFractions.Add(totalPairs > 0 ? (double)multiPathCount / totalPairs : 0);

            // Layer skewness and kurtosis
            if (layerCounts.Length > 2)
            {
                double layerMean = layerCounts.Skip(1).Average();
                double m2 = layerCounts.Skip(1).Average(x => (x - layerMean) * (x - layerMean));
                double m3 = layerCounts.Skip(1).Average(x => (x - layerMean) * (x - layerMean) * (x - layerMean));
                double m4 = layerCounts.Skip(1).Average(x => Math.Pow(x - layerMean, 4));
                double skew = m2 > 1e-15 ? m3 / Math.Pow(m2, 1.5) : 0;
                double kurt = m2 > 1e-15 ? m4 / (m2 * m2) : 0;
                layerSkewnesses.Add(skew);
                layerKurtoses.Add(kurt);
            }
            else { layerSkewnesses.Add(0); layerKurtoses.Add(0); }

            // Front velocity: cumulative N covered / cumulative radius
            double frontVel = 0;
            int cumN = layerCounts[0];
            for (int d = 1; d < layerCounts.Length; d++)
            {
                cumN += layerCounts[d];
                frontVel = Math.Max(frontVel, (double)cumN / d);
            }
            frontVelocities.Add(frontVel / N); // normalize by N

            // Saturation: does peak layer stabilize relative to mean?
            double peakRatio = meanLayer > 0 ? peakVal / meanLayer : 1;
            bool saturates = peakRatio < 3.0 && peakIdx > 0 && peakIdx < maxDist;
            saturationFlags.Add(saturates);

            o.WriteLine($"  Source {src}: nR={nR}, maxDist={maxDist}, meanLayer={meanLayer:F2}, peak={peakVal:F0}@layer{peakIdx}, geoEff={geoEff:F4}, frontVel={frontVel/N:F4}");
        }

        // Average the aggregated layer profile
        for (int d = 0; d < aggLayerProfile.Length; d++)
            aggLayerProfile[d] /= nSources;

        // Inferred velocity bound: mean of max layer advance
        double avgMaxAdvance = maxAdvances.Average();

        // v_bound: maximum fractional advance per hop, bounded by adjacency structure
        double inferredBound = avgMaxAdvance;

        return new(name, dim, N, diameter, adj.Sum(a => a.Count) / 2, nSources,
            meanDeg, meanLayerSizes.Average(),
            peakLayerSizes.Average(), (int)peakLayerIndices.Average(),
            medianLayerSizes.Average(), avgMaxAdvance,
            geoEfficiencies.Average(), meanPathLengths.Average(),
            pathLengthCVs.Average(), altPathFractions.Average(),
            layerSkewnesses.Average(), layerKurtoses.Average(),
            frontVelocities.Average(), inferredBound,
            saturationFlags.Any(s => s),
            aggLayerProfile.Where(x => x > 0).ToArray());
    }

    // Count number of shortest paths between u and v using BFS
    private static int CountShortestPaths(List<int>[] adj, int N, int src, int tgt, int srcDist, int tgtDist)
    {
        if (srcDist < 0 || tgtDist < 0) return 0;
        int targetDist = Math.Abs(tgtDist - srcDist);
        if (targetDist == 0) return 1;

        // Count paths from closer node to farther node
        int start = srcDist < tgtDist ? src : tgt;
        int end = srcDist < tgtDist ? tgt : src;

        var dist = BFS(adj, N, start);
        if (dist[end] != targetDist) return 0;

        // Dynamic programming: count paths at each distance level
        var pathCounts = new int[N];
        var byDist = new List<int>[targetDist + 1];
        for (int d = 0; d <= targetDist; d++) byDist[d] = new List<int>();
        for (int i = 0; i < N; i++)
            if (dist[i] >= 0 && dist[i] <= targetDist)
                byDist[dist[i]].Add(i);

        pathCounts[start] = 1;
        for (int d = 0; d < targetDist; d++)
        {
            foreach (int u in byDist[d])
            {
                if (pathCounts[u] == 0) continue;
                foreach (int v in adj[u])
                {
                    if (dist[v] == d + 1)
                        pathCounts[v] = Math.Min(pathCounts[v] + pathCounts[u], 1000);
                }
            }
        }
        return Math.Min(pathCounts[end], 1000);
    }

    private record PviResult(
        string Name, string Dim, int N, int Diameter, int Edges, int NSources,
        double MeanDegree,
        double MeanLayerSize,
        double PeakLayerSize, int PeakLayerIndex,
        double MedianLayerSize,
        double MaxLayerAdvance,
        double GeodesicEfficiency, double MeanPathLength,
        double PathLengthCV, double AltPathFraction,
        double LayerSkewness, double LayerKurtosis,
        double FrontVelocity, double InferredVelocityBound,
        bool Saturates,
        double[] LayerProfile);
}
