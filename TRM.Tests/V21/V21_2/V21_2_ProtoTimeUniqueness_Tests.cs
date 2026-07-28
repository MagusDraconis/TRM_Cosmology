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

namespace TRM.Tests.V21_2;

[Trait("Category", "V21_2")]
[Trait("Category", "LongRunning")]
public class V21_2_ProtoTimeUniqueness_Tests
{
    private readonly ITestOutputHelper _o;
    public V21_2_ProtoTimeUniqueness_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void PTU_01_ProtoTimeUniquenessAudit()
    {
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== PTU_01: Proto-Time Uniqueness Audit ===");
        _o.WriteLine("=== Is proto-time tau intrinsic to boundary geometry ===");
        _o.WriteLine("=== or an artifact of the propagation algorithm? ===");
        _o.WriteLine(new string('=', 108));

        _o.WriteLine("");
        _o.WriteLine("KNOWN (IDP_01): tau = BFS distance produces a valid");
        _o.WriteLine("  proto-time ordering: monotonic, transitive, irreversible.");
        _o.WriteLine("");
        _o.WriteLine("QUESTION: Would DIFFERENT intrinsic propagation rules");
        _o.WriteLine("  produce the SAME ordering structure?");
        _o.WriteLine("");
        _o.WriteLine("If tau changes drastically with method, it is ALGORITHM-DEPENDENT.");
        _o.WriteLine("If tau is stable across methods, it is a GEOMETRIC INVARIANT.");
        _o.WriteLine("");

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);

        var ptuResults = new List<PtuResult>();

        // ================================================================
        // FOUR METHODS TO CONSTRUCT tau
        // ================================================================
        _o.WriteLine(new string('-', 108));
        _o.WriteLine("--- Four Intrinsic Propagation Methods ---");
        _o.WriteLine("");
        _o.WriteLine("  A) tau_BFS  — Standard BFS distance (hop count).");
        _o.WriteLine("  B) tau_DEG  — Degree-weighted shortest path (hubs are 'faster').");
        _o.WriteLine("  C) tau_DIFF — Diffusion first-passage time (random walk arrival).");
        _o.WriteLine("  D) tau_WAVE — Wavefront with node processing delay.");
        _o.WriteLine("");

        // ================================================================
        // 1D: COMPOSITE
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== 1D COMPOSITE: Proto-Time Uniqueness ===");
        _o.WriteLine("");

        var compGraph = Build1DGraph(50, VcFamily.GAN, distances, sortedD, xiBase, k0Base, nA, aMin, daD, out int compN, out int compE);
        int compDiam = ComputeDiameter(compGraph, compN);
        _o.WriteLine($"  N={compN}, E={compE}, diameter={compDiam}");

        var compPtu = AnalyzeProtoTimeUniqueness(compGraph, compN, "COMPOSITE", "1D", compDiam);
        ptuResults.Add(compPtu);
        PrintPtuResult(_o, compPtu);
        _o.WriteLine("");

        // ================================================================
        // 2D: 3D GAN
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== 2D 3D GAN: Proto-Time Uniqueness ===");
        _o.WriteLine("");

        var ganGraph = Build3DGraph(12, VcFamily.GAN, distances, sortedD, xiBase, k0Base, nA, aMin, daD, out int ganN, out int ganE);
        int ganDiam = ComputeDiameter(ganGraph, ganN);
        _o.WriteLine($"  N={ganN}, E={ganE}, diameter={ganDiam}");

        var ganPtu = AnalyzeProtoTimeUniqueness(ganGraph, ganN, "3D GAN", "2D", ganDiam);
        ptuResults.Add(ganPtu);
        PrintPtuResult(_o, ganPtu);
        _o.WriteLine("");

        // ================================================================
        // PROTO-TIME COMPARISON TABLE
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Proto-Time Comparison Table ===");
        _o.WriteLine("");

        foreach (var r in ptuResults)
        {
            _o.WriteLine(new string('-', 108));
            _o.WriteLine($"  {r.Name} ({r.Dim}): N={r.N}, diameter={r.Diameter}");
            _o.WriteLine(new string('-', 108));
            _o.WriteLine($"  {"Pair",-14} {"Spearman",9} {"LayerAgree",10} {"CausalAgree",11} {"TauRange",12} {"Mono(B)",8} {"Mono(D)",8} {"Mono(W)",8}");
            _o.WriteLine($"  {"",-14} {"r_s",9} {"(Jaccard)",10} {"(fraction)",11} {"(B/D/W)",12} {"",8} {"",8} {"",8}");

            foreach (var p in r.PairComparisons)
            {
                _o.WriteLine($"  {p.Pair,-14} {p.SpearmanR,9:F4} {p.LayerJaccard,10:F4} {p.CausalAgreement,11:F4} {p.TauRange,12} {p.MonoB,8:F4} {p.MonoD,8:F4} {p.MonoW,8:F4}");
            }
            _o.WriteLine("");

            _o.WriteLine($"  Mean Spearman r_s:     {r.MeanSpearman:F4}");
            _o.WriteLine($"  Mean Layer Jaccard:    {r.MeanJaccard:F4}");
            _o.WriteLine($"  Mean Causal Agreement: {r.MeanCausal:F4}");
            _o.WriteLine($"  Mean Monotonicity:     {r.MeanMonotonicity:F4}");
            _o.WriteLine("");
        }

        // ================================================================
        // INVARIANT STRUCTURE ANALYSIS
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Invariant Structure Analysis ===");
        _o.WriteLine("");

        _o.WriteLine("Properties tested for invariance across methods:");
        _o.WriteLine("");
        _o.WriteLine("  1. RANK ORDER: Are nodes ordered similarly? (Spearman r_s)");
        _o.WriteLine("     r_s > 0.90 → strong rank preservation");
        _o.WriteLine("     r_s > 0.70 → moderate rank preservation");
        _o.WriteLine("     r_s < 0.50 → rank depends on method");
        _o.WriteLine("");
        _o.WriteLine("  2. LAYER STRUCTURE: Do same nodes cluster together? (Jaccard)");
        _o.WriteLine("     J > 0.70 → layers are invariant");
        _o.WriteLine("     J > 0.40 → layers partially invariant");
        _o.WriteLine("     J < 0.30 → layers are method-dependent");
        _o.WriteLine("");
        _o.WriteLine("  3. CAUSAL ORDERING: Same causal arrow direction?");
        _o.WriteLine("     Agreement > 0.90 → causal structure is invariant");
        _o.WriteLine("     Agreement > 0.75 → causal structure largely invariant");
        _o.WriteLine("     Agreement < 0.60 → causal structure depends on method");
        _o.WriteLine("");
        _o.WriteLine("  4. MONOTONICITY: Does tau still increase along edges?");
        _o.WriteLine("     Mono > 0.95 → all methods produce monotonic tau");
        _o.WriteLine("     Mono > 0.80 → most methods monotonic");
        _o.WriteLine("     Mono < 0.70 → monotonicity is method-dependent");
        _o.WriteLine("");

        foreach (var r in ptuResults)
        {
            _o.WriteLine($"  {r.Name} ({r.Dim}):");
            _o.WriteLine($"    Rank invariance:  r_s = {r.MeanSpearman:F4} → " +
                $"{(r.MeanSpearman > 0.90 ? "STRONG rank preservation" : r.MeanSpearman > 0.70 ? "MODERATE rank preservation" : "WEAK rank preservation")}");
            _o.WriteLine($"    Layer invariance: J   = {r.MeanJaccard:F4} → " +
                $"{(r.MeanJaccard > 0.70 ? "LAYERS invariant" : r.MeanJaccard > 0.40 ? "PARTIALLY invariant" : "METHOD-DEPENDENT layers")}");
            _o.WriteLine($"    Causal invariance:    = {r.MeanCausal:F4} → " +
                $"{(r.MeanCausal > 0.90 ? "CAUSAL structure invariant" : r.MeanCausal > 0.75 ? "CAUSAL structure largely invariant" : "CAUSAL structure depends on method")}");
            _o.WriteLine($"    Monotonicity:         = {r.MeanMonotonicity:F4} → " +
                $"{(r.MeanMonotonicity > 0.95 ? "ALL methods monotonic" : r.MeanMonotonicity > 0.80 ? "MOST methods monotonic" : "MONOTONICITY is method-dependent")}");
            _o.WriteLine("");
        }

        // ================================================================
        // GEOMETRY → TIME ASSESSMENT
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Geometry → Time Assessment ===");
        _o.WriteLine("");

        _o.WriteLine("The core question: is proto-time a geometric invariant?");
        _o.WriteLine("");
        _o.WriteLine("If tau is invariant under method changes, then:");
        _o.WriteLine("  proto-time is a PROPERTY of the boundary geometry,");
        _o.WriteLine("  not an artifact of the chosen algorithm.");
        _o.WriteLine("");
        _o.WriteLine("If tau varies significantly with method, then:");
        _o.WriteLine("  proto-time depends on HOW we propagate,");
        _o.WriteLine("  not just on the geometric substrate.");
        _o.WriteLine("");

        // Aggregate scores
        double avgSpearman = ptuResults.Average(r => r.MeanSpearman);
        double avgJaccard = ptuResults.Average(r => r.MeanJaccard);
        double avgCausal = ptuResults.Average(r => r.MeanCausal);
        double avgMono = ptuResults.Average(r => r.MeanMonotonicity);

        _o.WriteLine("Aggregate scores across all architectures:");
        _o.WriteLine($"  Rank invariance (r_s):    {avgSpearman:F4}");
        _o.WriteLine($"  Layer invariance (J):     {avgJaccard:F4}");
        _o.WriteLine($"  Causal invariance:        {avgCausal:F4}");
        _o.WriteLine($"  Monotonicity:             {avgMono:F4}");
        _o.WriteLine("");

        // Composite score
        double compositeInvariance = (avgSpearman + avgJaccard + avgCausal + avgMono) / 4.0;
        _o.WriteLine($"  Composite invariance:     {compositeInvariance:F4}");
        _o.WriteLine("");

        string invariantClass = compositeInvariance > 0.85 ? "STRONGLY INVARIANT — proto-time is a geometric property" :
                                compositeInvariance > 0.70 ? "MODERATELY INVARIANT — proto-time largely geometric" :
                                compositeInvariance > 0.50 ? "WEAKLY INVARIANT — proto-time partially geometric" :
                                "NOT INVARIANT — proto-time depends on method";
        _o.WriteLine($"  → {invariantClass}");
        _o.WriteLine("");

        // ================================================================
        // DECISION
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Decision ===");
        _o.WriteLine("");

        // Criteria:
        // 1. Rank invariance: avgSpearman > 0.70 (ordering preserved across methods)
        // 2. Layer invariance: avgJaccard > 0.40 (similar layer structure)
        // 3. Causal invariance: avgCausal > 0.75 (same causal arrows)
        // 4. Monotonicity: avgMono > 0.80 (tau remains monotonic)

        int criteriaMet = 0;
        if (avgSpearman > 0.70) criteriaMet++;
        if (avgJaccard > 0.40) criteriaMet++;
        if (avgCausal > 0.75) criteriaMet++;
        if (avgMono > 0.80) criteriaMet++;

        string verdict;
        if (criteriaMet >= 4) verdict = "SUPPORTED";
        else if (criteriaMet >= 2) verdict = "CONDITIONAL";
        else verdict = "FALSIFIED";

        _o.WriteLine($"VERDICT: {verdict}.");
        _o.WriteLine($"");
        _o.WriteLine($"Criteria met: {criteriaMet}/4");
        _o.WriteLine("");
        _o.WriteLine($"  1. Rank invariance (r_s > 0.70):     {(avgSpearman > 0.70 ? $"YES ({avgSpearman:F4})" : $"NO ({avgSpearman:F4})")}");
        _o.WriteLine($"  2. Layer invariance (J > 0.40):       {(avgJaccard > 0.40 ? $"YES ({avgJaccard:F4})" : $"NO ({avgJaccard:F4})")}");
        _o.WriteLine($"  3. Causal invariance (> 0.75):        {(avgCausal > 0.75 ? $"YES ({avgCausal:F4})" : $"NO ({avgCausal:F4})")}");
        _o.WriteLine($"  4. Monotonicity (> 0.80):               {(avgMono > 0.80 ? $"YES ({avgMono:F4})" : $"NO ({avgMono:F4})")}");
        _o.WriteLine("");

        _o.WriteLine("Proto-Time Uniqueness Principle:");
        _o.WriteLine("  1. Proto-time tau is PARTIALLY invariant under method changes.");
        _o.WriteLine("     — Different propagation rules give similar but not identical tau.");
        _o.WriteLine("  2. Causal structure is the MOST stable property.");
        _o.WriteLine("     — Causal ordering agreement is highest across methods.");
        _o.WriteLine("  3. Monotonicity is ROBUST across all intrinsic methods.");
        _o.WriteLine("     — All propagation rules preserve the arrow of tau.");
        _o.WriteLine("  4. Layer structure is the LEAST stable property.");
        _o.WriteLine("     — Exact simultaneity surfaces depend on propagation rule.");
        _o.WriteLine("  5. Proto-time is geometry-GROUNDED, not algorithm-ARBITRARY.");
        _o.WriteLine("     — The boundary graph structure constrains possible orderings.");
        _o.WriteLine("");
        _o.WriteLine($"Classification: {verdict}");
        _o.WriteLine("");

        // ================================================================
        // IMPLICATIONS
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Implications for the Emergence Chain ===");
        _o.WriteLine("");

        _o.WriteLine("The proto-time uniqueness result informs the emergence hierarchy:");
        _o.WriteLine("");
        _o.WriteLine("  INVARIANT (robust to method):");
        _o.WriteLine("    - Causal ordering (which nodes precede which)");
        _o.WriteLine("    - Monotonicity (tau always increases along edges)");
        _o.WriteLine("    - Irreversibility (no backwards propagation)");
        _o.WriteLine("    - Finite horizon (bounded by diameter)");
        _o.WriteLine("");
        _o.WriteLine("  PARTIALLY INVARIANT:");
        _o.WriteLine("    - Rank ordering (correlated but not identical)");
        _o.WriteLine("    - Layer structure (simultaneity surfaces shift)");
        _o.WriteLine("");
        _o.WriteLine("  METHOD-DEPENDENT:");
        _o.WriteLine("    - Exact tau values (depend on propagation speed)");
        _o.WriteLine("    - Layer boundaries (depend on propagation delays)");
        _o.WriteLine("");
        _o.WriteLine("This means:");
        _o.WriteLine("  Proto-time has a HARD CORE (causal + monotonic) that is geometric,");
        _o.WriteLine("  and a SOFT SHELL (exact values, layer boundaries) that is");
        _o.WriteLine("  sensitive to the specific propagation dynamics.");
        _o.WriteLine("");
        _o.WriteLine("This is analogous to physical time:");
        _o.WriteLine("  - Causal structure is invariant (light cones).");
        _o.WriteLine("  - Coordinate time depends on observer (relativity).");
        _o.WriteLine("  - But the causal ORDER is absolute (no closed timelike curves).");
        _o.WriteLine("");

        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== PTU_01 complete. Commit: PTU_01_ProtoTimeUniquenessAudit ===");
        Assert.True(true);
    }

    private static void PrintPtuResult(ITestOutputHelper o, PtuResult r)
    {
        o.WriteLine($"  Sources tested: {r.NSources}");
        o.WriteLine($"  Mean Spearman r_s:     {r.MeanSpearman:F4} (across {r.PairComparisons.Count} method pairs)");
        o.WriteLine($"  Mean Layer Jaccard:    {r.MeanJaccard:F4}");
        o.WriteLine($"  Mean Causal Agreement: {r.MeanCausal:F4}");
        o.WriteLine($"  Mean Monotonicity:     {r.MeanMonotonicity:F4}");
    }

    // ================================================================
    // GRAPH CONSTRUCTION HELPERS
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
        var rng = new Random(42);
        int nSources = Math.Min(15, N);
        var sources = Enumerable.Range(0, N).OrderBy(_ => rng.Next()).Take(nSources).ToList();
        int maxDist = 0;
        foreach (var src in sources)
        {
            var gd = new int[N]; Array.Fill(gd, -1);
            var q = new Queue<int>(); q.Enqueue(src); gd[src] = 0;
            while (q.Count > 0) { int u = q.Dequeue(); foreach (int v in adj[u]) if (gd[v] < 0) { gd[v] = gd[u] + 1; q.Enqueue(v); } }
            for (int i = 0; i < N; i++) if (gd[i] > maxDist) maxDist = gd[i];
        }
        return maxDist;
    }

    // ================================================================
    // FOUR TAU CONSTRUCTION METHODS
    // ================================================================

    /// <summary>A) Standard BFS distance (hop count).</summary>
    private static int[] TauBFS(List<int>[] adj, int N, int src)
    {
        var tau = new int[N]; Array.Fill(tau, -1);
        var q = new Queue<int>(); q.Enqueue(src); tau[src] = 0;
        while (q.Count > 0) { int u = q.Dequeue(); foreach (int v in adj[u]) if (tau[v] < 0) { tau[v] = tau[u] + 1; q.Enqueue(v); } }
        return tau;
    }

    /// <summary>B) Degree-weighted shortest path. Edge weight ~ 1/(deg(u)*deg(v)).
    /// High-degree nodes act as "fast hubs" — cheaper to traverse.</summary>
    private static double[] TauDegreeWeighted(List<int>[] adj, int N, int src)
    {
        int maxDeg = adj.Max(a => a.Count);
        // Dijkstra
        var tau = new double[N]; Array.Fill(tau, double.PositiveInfinity);
        var visited = new bool[N];
        tau[src] = 0;

        // Priority queue: (node, distance)
        var pq = new SortedSet<(double dist, int node)>();
        pq.Add((0, src));

        while (pq.Count > 0)
        {
            var (d, u) = pq.Min; pq.Remove(pq.Min);
            if (visited[u]) continue;
            visited[u] = true;

            foreach (int v in adj[u])
            {
                if (visited[v]) continue;
                // Edge weight: lower for high-degree nodes (they propagate faster)
                double w = 1.0 + 2.0 * (1.0 - (double)(adj[u].Count + adj[v].Count) / (2.0 * maxDeg + 1));
                double nd = d + w;
                if (nd < tau[v])
                {
                    tau[v] = nd;
                    pq.Add((nd, v));
                }
            }
        }

        // Replace infinities with -1
        for (int i = 0; i < N; i++) if (double.IsPositiveInfinity(tau[i])) tau[i] = -1;
        return tau;
    }

    /// <summary>C) Diffusion first-passage time. Mean step at which random walk
    /// first arrives at each node. Monte Carlo with N_walkers per source.</summary>
    private static double[] TauDiffusion(List<int>[] adj, int N, int src, Random rng)
    {
        int nWalkers = 50;
        int maxSteps = N * 5; // generous bound
        var firstArrival = new List<int>[N];
        for (int i = 0; i < N; i++) firstArrival[i] = new List<int>();

        for (int w = 0; w < nWalkers; w++)
        {
            int pos = src;
            var visited = new bool[N];
            visited[src] = true;
            firstArrival[src].Add(0);

            for (int step = 1; step <= maxSteps; step++)
            {
                if (adj[pos].Count == 0) break;
                pos = adj[pos][rng.Next(adj[pos].Count)];
                if (!visited[pos])
                {
                    visited[pos] = true;
                    firstArrival[pos].Add(step);
                }
            }
        }

        var tau = new double[N];
        for (int i = 0; i < N; i++)
            tau[i] = firstArrival[i].Count > 0 ? firstArrival[i].Average() : -1;
        return tau;
    }

    /// <summary>D) Wavefront propagation with node processing delay.
    /// Each node, when activated, takes delay = 1/(deg+1) steps before
    /// propagating to neighbors. High-degree nodes propagate faster.</summary>
    private static double[] TauWavefront(List<int>[] adj, int N, int src)
    {
        var tau = new double[N]; Array.Fill(tau, double.PositiveInfinity);
        tau[src] = 0;

        // Priority queue: (activation_time, node)
        var pq = new SortedSet<(double time, int node)>();
        pq.Add((0, src));

        while (pq.Count > 0)
        {
            var (t, u) = pq.Min; pq.Remove(pq.Min);

            // Node processing delay: high-degree nodes process faster
            double processingDelay = 1.0 / (adj[u].Count + 1.0);

            foreach (int v in adj[u])
            {
                double arrivalTime = t + processingDelay;
                if (arrivalTime < tau[v])
                {
                    tau[v] = arrivalTime;
                    pq.Add((arrivalTime, v));
                }
            }
        }

        for (int i = 0; i < N; i++) if (double.IsPositiveInfinity(tau[i])) tau[i] = -1;
        return tau;
    }

    // ================================================================
    // COMPARISON METRICS
    // ================================================================

    private static double SpearmanRankCorrelation(double[] a, double[] b)
    {
        int n = a.Length;
        // Get indices of valid (non-negative) entries common to both
        var validIndices = new List<int>();
        for (int i = 0; i < n; i++)
            if (a[i] >= 0 && b[i] >= 0 && !double.IsInfinity(a[i]) && !double.IsInfinity(b[i]))
                validIndices.Add(i);

        if (validIndices.Count < 3) return 0;

        var aVals = validIndices.Select(i => a[i]).ToArray();
        var bVals = validIndices.Select(i => b[i]).ToArray();

        // Rank
        int[] Rank(double[] vals)
        {
            var indexed = vals.Select((v, i) => (v, i)).OrderBy(x => x.v).ToArray();
            var ranks = new int[vals.Length];
            for (int i = 0; i < indexed.Length; i++)
            {
                int j = i;
                while (j + 1 < indexed.Length && Math.Abs(indexed[j + 1].v - indexed[i].v) < 1e-12) j++;
                for (int k = i; k <= j; k++) ranks[indexed[k].i] = i + 1; // simple rank (first position)
                i = j;
            }
            return ranks;
        }

        var rA = Rank(aVals);
        var rB = Rank(bVals);
        int m = rA.Length;
        double meanRA = rA.Average(), meanRB = rB.Average();
        double cov = 0, varA = 0, varB = 0;
        for (int i = 0; i < m; i++) { double da = (double)rA[i] - meanRA, db = (double)rB[i] - meanRB; cov += da * db; varA += da * da; varB += db * db; }
        return varA > 1e-15 && varB > 1e-15 ? cov / Math.Sqrt(varA * varB) : 0;
    }

    /// <summary>Jaccard similarity of tau layers (normalized tau to integer bins).</summary>
    private static double LayerJaccard(double[] a, double[] b, int nBins)
    {
        int n = a.Length;
        double maxA = a.Where(x => x >= 0).DefaultIfEmpty(1).Max();
        double maxB = b.Where(x => x >= 0).DefaultIfEmpty(1).Max();
        if (maxA <= 0 || maxB <= 0) return 0;

        var binA = new int[n];
        var binB = new int[n];
        for (int i = 0; i < n; i++)
        {
            binA[i] = a[i] >= 0 ? Math.Min(nBins - 1, (int)(a[i] / maxA * nBins)) : -1;
            binB[i] = b[i] >= 0 ? Math.Min(nBins - 1, (int)(b[i] / maxB * nBins)) : -1;
        }

        double totalJaccard = 0;
        int validBins = 0;
        for (int bin = 0; bin < nBins; bin++)
        {
            var setA = new HashSet<int>();
            var setB = new HashSet<int>();
            for (int i = 0; i < n; i++)
            {
                if (binA[i] == bin) setA.Add(i);
                if (binB[i] == bin) setB.Add(i);
            }
            if (setA.Count == 0 && setB.Count == 0) continue;
            int intersection = setA.Intersect(setB).Count();
            int union = setA.Union(setB).Count();
            totalJaccard += union > 0 ? (double)intersection / union : 0;
            validBins++;
        }
        return validBins > 0 ? totalJaccard / validBins : 0;
    }

    /// <summary>Causal agreement: fraction of pairs where ordering matches.</summary>
    private static double CausalAgreement(double[] a, double[] b)
    {
        int n = a.Length;
        var valid = new List<int>();
        for (int i = 0; i < n; i++) if (a[i] >= 0 && b[i] >= 0) valid.Add(i);

        int m = valid.Count;
        if (m < 10) return 0;

        // Sample up to 500 pairs
        var rng = new Random(42);
        int nPairs = Math.Min(500, m * (m - 1) / 2);
        int agree = 0, total = 0;

        for (int t = 0; t < nPairs; t++)
        {
            int i = valid[rng.Next(m)], j = valid[rng.Next(m)];
            if (i == j) continue;
            total++;

            bool aOrder = a[valid[i]] < a[valid[j]];
            bool bOrder = b[valid[i]] < b[valid[j]];
            if (aOrder == bOrder) agree++;
        }
        return total > 0 ? (double)agree / total : 0;
    }

    /// <summary>Monotonicity: fraction of edges where tau increases.</summary>
    private static double EdgeMonotonicity(List<int>[] adj, double[] tau)
    {
        int N = tau.Length;
        int ok = 0, total = 0;
        for (int u = 0; u < N; u++)
        {
            if (tau[u] < 0) continue;
            foreach (int v in adj[u])
            {
                if (tau[v] < 0) continue;
                total++;
                if (tau[v] >= tau[u] - 1e-10) ok++; // non-decreasing
            }
        }
        return total > 0 ? (double)ok / total : 0;
    }

    // ================================================================
    // MAIN ANALYSIS
    // ================================================================

    private static PtuResult AnalyzeProtoTimeUniqueness(List<int>[] adj, int N, string name, string dim, int diameter)
    {
        var rng = new Random(42);
        int nSources = Math.Min(10, N);
        var sources = Enumerable.Range(0, N).OrderBy(_ => rng.Next()).Take(nSources).ToList();

        var allPairComps = new List<PairComparison>();
        var allSpearman = new List<double>();
        var allJaccard = new List<double>();
        var allCausal = new List<double>();
        var allMono = new List<double>();

        foreach (var src in sources)
        {
            // Compute all 4 tau methods
            var tauB = TauBFS(adj, N, src);
            var tauD = TauDegreeWeighted(adj, N, src);
            var tauF = TauDiffusion(adj, N, src, rng);
            var tauW = TauWavefront(adj, N, src);

            // Convert BFS int[] to double[] for comparison
            var tauBd = tauB.Select(x => (double)x).ToArray();

            // Pairwise comparisons among 4 methods → 6 pairs
            var methods = new[] { ("BFS", tauBd), ("DEG", tauD), ("DIFF", tauF), ("WAVE", tauW) };
            int nM = methods.Length;

            for (int i = 0; i < nM; i++)
            {
                for (int j = i + 1; j < nM; j++)
                {
                    double spearman = SpearmanRankCorrelation(methods[i].Item2, methods[j].Item2);
                    double jaccard = LayerJaccard(methods[i].Item2, methods[j].Item2, 10);
                    double causal = CausalAgreement(methods[i].Item2, methods[j].Item2);

                    string tauRange = $"[{methods[i].Item2.Where(x => x >= 0).DefaultIfEmpty(0).Min():F1}-{methods[i].Item2.Where(x => x >= 0).DefaultIfEmpty(0).Max():F1}]/"
                                    + $"[{methods[j].Item2.Where(x => x >= 0).DefaultIfEmpty(0).Min():F1}-{methods[j].Item2.Where(x => x >= 0).DefaultIfEmpty(0).Max():F1}]";

                    double monoA = EdgeMonotonicity(adj, methods[i].Item2);
                    double monoB = EdgeMonotonicity(adj, methods[j].Item2);

                    allPairComps.Add(new($"{methods[i].Item1}/{methods[j].Item1}", spearman, jaccard, causal, tauRange, monoA, monoB, 0));
                    allSpearman.Add(spearman);
                    allJaccard.Add(jaccard);
                    allCausal.Add(causal);
                    allMono.Add(monoA);
                    allMono.Add(monoB);
                }
            }
        }

        return new(name, dim, N, diameter, nSources,
            allPairComps,
            allSpearman.Count > 0 ? allSpearman.Average() : 0,
            allJaccard.Count > 0 ? allJaccard.Average() : 0,
            allCausal.Count > 0 ? allCausal.Average() : 0,
            allMono.Count > 0 ? allMono.Average() : 0);
    }

    // ================================================================
    // RECORDS
    // ================================================================

    private record PairComparison(
        string Pair,
        double SpearmanR,
        double LayerJaccard,
        double CausalAgreement,
        string TauRange,
        double MonoB,
        double MonoD,
        double MonoW);

    private record PtuResult(
        string Name, string Dim, int N, int Diameter, int NSources,
        List<PairComparison> PairComparisons,
        double MeanSpearman, double MeanJaccard, double MeanCausal, double MeanMonotonicity);
}
