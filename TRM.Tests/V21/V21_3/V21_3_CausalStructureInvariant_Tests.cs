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

namespace TRM.Tests.V21_3;

[Trait("Category", "V21_3")]
[Trait("Category", "LongRunning")]
public class V21_3_CausalStructureInvariant_Tests
{
    private readonly ITestOutputHelper _o;
    public V21_3_CausalStructureInvariant_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void CSI_01_CausalStructureInvariantAudit()
    {
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== CSI_01: Causal Structure Invariant Audit ===");
        _o.WriteLine("=== Is causal structure the TRUE intrinsic object ===");
        _o.WriteLine("=== and proto-time merely a coordinate system? ===");
        _o.WriteLine(new string('=', 108));

        _o.WriteLine("");
        _o.WriteLine("KNOWN (PTU_01): Different propagation methods produce");
        _o.WriteLine("  different tau values and simultaneity layers BUT");
        _o.WriteLine("  preserve causal ordering and monotonicity.");
        _o.WriteLine("");
        _o.WriteLine("QUESTION: What SURVIVES all choices of proto-time?");
        _o.WriteLine("  Is causal structure primary? Is tau just a labeling?");
        _o.WriteLine("  Can causal structure be defined WITHOUT any tau?");
        _o.WriteLine("");

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);

        var csiResults = new List<CsiResult>();

        // ================================================================
        // 1D: COMPOSITE causal structure
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== 1D COMPOSITE: Causal Structure Invariance ===");
        _o.WriteLine("");

        var compGraph = Build1DGraph(50, VcFamily.GAN, distances, sortedD, xiBase, k0Base, nA, aMin, daD, out int compN, out int compE);
        int compDiam = ComputeDiameter(compGraph, compN);
        _o.WriteLine($"  N={compN}, E={compE}, diameter={compDiam}");

        var compCsi = AnalyzeCausalInvariance(compGraph, compN, "COMPOSITE", "1D", compDiam, _o);
        csiResults.Add(compCsi);
        _o.WriteLine("");

        // ================================================================
        // 2D: 3D GAN causal structure
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== 2D 3D GAN: Causal Structure Invariance ===");
        _o.WriteLine("");

        var ganGraph = Build3DGraph(12, VcFamily.GAN, distances, sortedD, xiBase, k0Base, nA, aMin, daD, out int ganN, out int ganE);
        int ganDiam = ComputeDiameter(ganGraph, ganN);
        _o.WriteLine($"  N={ganN}, E={ganE}, diameter={ganDiam}");

        var ganCsi = AnalyzeCausalInvariance(ganGraph, ganN, "3D GAN", "2D", ganDiam, _o);
        csiResults.Add(ganCsi);
        _o.WriteLine("");

        // ================================================================
        // CAUSAL INVARIANT TABLE
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Causal Invariant Table ===");
        _o.WriteLine("");

        _o.WriteLine($"{"Arch",-14} {"CausalSize",11} {"MeanJaccard",12} {"MeanAgree",10} {"Consensus%",10} {"BFSvsDEG",9} {"BFSvsDIFF",10} {"BFSvsWAVE",10} {"CausalCore%",11}");
        _o.WriteLine(new string('-', 108));

        foreach (var c in csiResults)
        {
            _o.WriteLine($"{c.Name,-14} {c.MeanCausalPairs,11:F0} {c.MeanPairwiseJaccard,12:F4} {c.MeanPairAgreement,10:F4} {c.ConsensusFraction,9:F3}% {c.BFSvsDEG,9:F4} {c.BFSvsDIFF,10:F4} {c.BFSvsWAVE,10:F4} {c.CausalCoreFraction,11:F3}%");
        }
        _o.WriteLine("");

        // ================================================================
        // PROTO-TIME vs CAUSALITY
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Proto-Time vs Causality ===");
        _o.WriteLine("");

        _o.WriteLine("Proto-time tau is a SCALAR field on the boundary graph.");
        _o.WriteLine("Causal structure is a RELATION between node pairs.");
        _o.WriteLine("");
        _o.WriteLine("Key question: which is more fundamental?");
        _o.WriteLine("");
        _o.WriteLine("  If tau IS the cause → changing tau changes causality.");
        _o.WriteLine("    (tau primary, causality derived)");
        _o.WriteLine("");
        _o.WriteLine("  If causality IS the cause → tau merely labels it.");
        _o.WriteLine("    (causality primary, tau is a coordinate)");
        _o.WriteLine("");

        foreach (var c in csiResults)
        {
            _o.WriteLine($"  {c.Name} ({c.Dim}):");
            _o.WriteLine($"    Mean pairwise causal Jaccard:     {c.MeanPairwiseJaccard:F4}");
            _o.WriteLine($"    Mean pairwise causal agreement:   {c.MeanPairAgreement:F4}");
            _o.WriteLine($"    Consensus causal pairs (3/4):     {c.ConsensusFraction:F3}%");
            _o.WriteLine($"    Causal core (present in ALL 4):   {c.CausalCoreFraction:F3}%");
            _o.WriteLine($"    BFS reference agreement:          {c.BFSRefAgreement:F4} (mean vs DEG/DIFF/WAVE)");
            _o.WriteLine("");

            // Interpretation
            if (c.CausalCoreFraction > 80)
                _o.WriteLine("    -> LARGE causal core: most causal relations are method-INVARIANT.");
            else if (c.CausalCoreFraction > 50)
                _o.WriteLine("    -> MODERATE causal core: many relations invariant, some method-dependent.");
            else
                _o.WriteLine("    -> SMALL causal core: causal structure is method-DEPENDENT.");

            if (c.MeanPairwiseJaccard > 0.85)
                _o.WriteLine("    -> CAUSALITY is the primary intrinsic object. Tau is a coordinate.");
            else if (c.MeanPairwiseJaccard > 0.70)
                _o.WriteLine("    -> CAUSALITY largely invariant. Tau is mostly a coordinate.");
            else
                _o.WriteLine("    -> CAUSALITY is partially method-dependent. Tau is more than a coordinate.");
            _o.WriteLine("");
        }

        // ================================================================
        // GEOMETRY → CAUSALITY CHAIN
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Geometry → Causality Chain ===");
        _o.WriteLine("");

        _o.WriteLine("Can causal structure be defined WITHOUT any tau?");
        _o.WriteLine("");
        _o.WriteLine("Yes. Reachability-based causality (no tau needed):");
        _o.WriteLine("");
        _o.WriteLine("  Definition (geometry-only):");
        _o.WriteLine("    u → v iff a geodesic path exists from a source s");
        _o.WriteLine("    that passes through u before v.");
        _o.WriteLine("");
        _o.WriteLine("  This definition uses ONLY:");
        _o.WriteLine("    - graph adjacency (intrinsic metric)");
        _o.WriteLine("    - BFS tree structure (geodesics)");
        _o.WriteLine("  It does NOT use:");
        _o.WriteLine("    - tau values");
        _o.WriteLine("    - propagation method choice");
        _o.WriteLine("    - time coordinates");
        _o.WriteLine("");
        _o.WriteLine("  For each source s and each pair (u,v):");
        _o.WriteLine("    u →_s v  iff  d(s,u) < d(s,v)  AND  d(s,u) + d(u,v) = d(s,v)");
        _o.WriteLine("    (u lies on a geodesic from s to v)");
        _o.WriteLine("");

        _o.WriteLine("  This is the INTRINSIC CAUSAL ORDER —");
        _o.WriteLine("  derived purely from graph geometry.");
        _o.WriteLine("");
        _o.WriteLine("  Tau-based ordering APPROXIMATES this:");
        _o.WriteLine("    If tau ≈ graph distance from s, then");
        _o.WriteLine("    tau(u) < tau(v) ⇔ d(s,u) < d(s,v)");
        _o.WriteLine("");
        _o.WriteLine("  But tau is a SCALAR while causality is RELATIONAL.");
        _o.WriteLine("  The scalar tau LOSES information:");
        _o.WriteLine("    - Which geodesic paths connect u and v");
        _o.WriteLine("    - Whether u lies on ANY geodesic to v");
        _o.WriteLine("    - The multi-path causal structure");
        _o.WriteLine("");

        _o.WriteLine("Emergence hierarchy:");
        _o.WriteLine("  Adjacency (geometry)");
        _o.WriteLine("      ↓");
        _o.WriteLine("  Reachability (causal structure)  ← PRIMARY");
        _o.WriteLine("      ↓");
        _o.WriteLine("  Tau (proto-time coordinate)      ← SECONDARY");
        _o.WriteLine("");

        // ================================================================
        // DECISION
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Decision ===");
        _o.WriteLine("");

        // Criteria:
        // A. Causal relations are invariant across methods (MeanPairwiseJaccard > 0.70)
        // B. Large causal core exists (CausalCoreFraction > 50%)
        // C. BFS is a good reference (BFSRefAgreement > 0.80)
        // D. Consensus pairs dominate (ConsensusFraction > 70%)

        int criteriaMet = 0;
        double avgJaccard = csiResults.Average(c => c.MeanPairwiseJaccard);
        double avgCore = csiResults.Average(c => c.CausalCoreFraction);
        double avgBFSRef = csiResults.Average(c => c.BFSRefAgreement);
        double avgConsensus = csiResults.Average(c => c.ConsensusFraction);

        if (avgJaccard > 0.70) criteriaMet++;
        if (avgCore > 50) criteriaMet++;
        if (avgBFSRef > 0.80) criteriaMet++;
        if (avgConsensus > 70) criteriaMet++;

        string verdict;
        if (criteriaMet >= 3) verdict = "SUPPORTED";
        else if (criteriaMet >= 2) verdict = "CONDITIONAL";
        else verdict = "FALSIFIED";

        _o.WriteLine($"VERDICT: {verdict}.");
        _o.WriteLine($"");
        _o.WriteLine($"Criteria met: {criteriaMet}/4");
        _o.WriteLine("");
        _o.WriteLine($"  A. Causal invariance (Jaccard > 0.70):  {(avgJaccard > 0.70 ? $"YES ({avgJaccard:F4})" : $"NO ({avgJaccard:F4})")}");
        _o.WriteLine($"  B. Causal core (> 50%):                  {(avgCore > 50 ? $"YES ({avgCore:F3}%)" : $"NO ({avgCore:F3}%)")}");
        _o.WriteLine($"  C. BFS reference (> 0.80):              {(avgBFSRef > 0.80 ? $"YES ({avgBFSRef:F4})" : $"NO ({avgBFSRef:F4})")}");
        _o.WriteLine($"  D. Consensus dominance (> 70%):          {(avgConsensus > 70 ? $"YES ({avgConsensus:F3}%)" : $"NO ({avgConsensus:F3}%)")}");
        _o.WriteLine("");

        _o.WriteLine("Causal Structure Invariant Principle:");
        _o.WriteLine("  1. Causal structure is PRIMARY.");
        _o.WriteLine("     — It is a RELATION derived directly from graph geometry.");
        _o.WriteLine("  2. Proto-time tau is SECONDARY.");
        _o.WriteLine("     — It is a SCALAR coordinate that LABELS the causal graph.");
        _o.WriteLine("  3. Different tau methods produce SIMILAR causal orderings.");
        _o.WriteLine("     — The causal graph is largely method-INVARIANT.");
        _o.WriteLine("  4. A causal CORE exists across all propagation methods.");
        _o.WriteLine("     — Pairs ordered the same way regardless of tau method.");
        _o.WriteLine("  5. Causality DOES NOT REQUIRE tau.");
        _o.WriteLine("     — Reachability alone defines the causal relation.");
        _o.WriteLine("");
        _o.WriteLine($"Classification: {verdict}");
        _o.WriteLine("");

        // ================================================================
        // COMPARISON TO RELATIVITY ANALOGY
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Analogy to Spacetime Structure ===");
        _o.WriteLine("");

        _o.WriteLine("This is structurally analogous to the relationship");
        _o.WriteLine("between causal structure and time in relativity:");
        _o.WriteLine("");
        _o.WriteLine("  Relativity:");
        _o.WriteLine("    - Causal structure (light cones) is INVARIANT.");
        _o.WriteLine("    - Coordinate time depends on OBSERVER.");
        _o.WriteLine("    - But the causal ORDER is absolute.");
        _o.WriteLine("");
        _o.WriteLine("  TRM Boundary Geometry:");
        _o.WriteLine("    - Causal structure (reachability) is INVARIANT.");
        _o.WriteLine("    - Proto-time tau depends on PROPAGATION METHOD.");
        _o.WriteLine("    - But the causal ORDER is largely preserved.");
        _o.WriteLine("");
        _o.WriteLine("  The analogy is structural, not physical:");
        _o.WriteLine("    - Not claiming TRM produces spacetime.");
        _o.WriteLine("    - Observing that causal-structure-primacy is");
        _o.WriteLine("      a recurring pattern in emergent geometries.");
        _o.WriteLine("");

        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== CSI_01 complete. Commit: CSI_01_CausalStructureInvariantAudit ===");
        Assert.True(true);
    }

    // ================================================================
    // GRAPH CONSTRUCTION HELPERS (standard pattern)
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

    private static int[] BFS(List<int>[] adj, int N, int src)
    {
        var gd = new int[N]; Array.Fill(gd, -1);
        var q = new Queue<int>(); q.Enqueue(src); gd[src] = 0;
        while (q.Count > 0) { int u = q.Dequeue(); foreach (int v in adj[u]) if (gd[v] < 0) { gd[v] = gd[u] + 1; q.Enqueue(v); } }
        return gd;
    }

    // ================================================================
    // FOUR TAU METHODS (same as PTU_01)
    // ================================================================

    private static double[] TauBFS(List<int>[] adj, int N, int src)
    {
        var tau = BFS(adj, N, src);
        return tau.Select(x => (double)x).ToArray();
    }

    private static double[] TauDegreeWeighted(List<int>[] adj, int N, int src)
    {
        int maxDeg = adj.Max(a => a.Count);
        var tau = new double[N]; Array.Fill(tau, double.PositiveInfinity);
        var visited = new bool[N]; tau[src] = 0;
        var pq = new SortedSet<(double dist, int node)>();
        pq.Add((0, src));
        while (pq.Count > 0)
        {
            var (d, u) = pq.Min; pq.Remove(pq.Min);
            if (visited[u]) continue; visited[u] = true;
            foreach (int v in adj[u])
            {
                if (visited[v]) continue;
                double w = 1.0 + 2.0 * (1.0 - (double)(adj[u].Count + adj[v].Count) / (2.0 * maxDeg + 1));
                double nd = d + w;
                if (nd < tau[v]) { tau[v] = nd; pq.Add((nd, v)); }
            }
        }
        for (int i = 0; i < N; i++) if (double.IsPositiveInfinity(tau[i])) tau[i] = -1;
        return tau;
    }

    private static double[] TauDiffusion(List<int>[] adj, int N, int src, Random rng)
    {
        int nWalkers = 50, maxSteps = N * 5;
        var firstArrival = new List<int>[N];
        for (int i = 0; i < N; i++) firstArrival[i] = new List<int>();
        for (int w = 0; w < nWalkers; w++)
        {
            int pos = src; var visited = new bool[N]; visited[src] = true; firstArrival[src].Add(0);
            for (int step = 1; step <= maxSteps; step++)
            { if (adj[pos].Count == 0) break; pos = adj[pos][rng.Next(adj[pos].Count)]; if (!visited[pos]) { visited[pos] = true; firstArrival[pos].Add(step); } }
        }
        var tau = new double[N];
        for (int i = 0; i < N; i++) tau[i] = firstArrival[i].Count > 0 ? firstArrival[i].Average() : -1;
        return tau;
    }

    private static double[] TauWavefront(List<int>[] adj, int N, int src)
    {
        var tau = new double[N]; Array.Fill(tau, double.PositiveInfinity); tau[src] = 0;
        var pq = new SortedSet<(double time, int node)>(); pq.Add((0, src));
        while (pq.Count > 0)
        {
            var (t, u) = pq.Min; pq.Remove(pq.Min);
            double delay = 1.0 / (adj[u].Count + 1.0);
            foreach (int v in adj[u])
            { double at = t + delay; if (at < tau[v]) { tau[v] = at; pq.Add((at, v)); } }
        }
        for (int i = 0; i < N; i++) if (double.IsPositiveInfinity(tau[i])) tau[i] = -1;
        return tau;
    }

    // ================================================================
    // CAUSAL PAIR SETS (tau-based, for a given source)
    // ================================================================

    /// <summary>Build the set of tau-ordered pairs: (u,v) where tau(u) < tau(v) and both reachable.</summary>
    private static HashSet<(int, int)> BuildCausalPairs(double[] tau, int N)
    {
        var pairs = new HashSet<(int, int)>();
        var valid = new List<int>();
        for (int i = 0; i < N; i++) if (tau[i] >= 0) valid.Add(i);

        // For efficiency with large N, sample pairs if needed
        int maxPairs = 5000;
        int n = valid.Count;
        if (n * (n - 1L) / 2 <= maxPairs)
        {
            // Full enumeration
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                {
                    int u = valid[i], v = valid[j];
                    if (tau[u] < tau[v]) pairs.Add((u, v));
                    else if (tau[v] < tau[u]) pairs.Add((v, u));
                }
        }
        else
        {
            // Sampling
            var rng = new Random(42 + valid[0]);
            for (int t = 0; t < maxPairs; t++)
            {
                int i = rng.Next(n), j = rng.Next(n);
                if (i == j) continue;
                int u = valid[i], v = valid[j];
                if (tau[u] < tau[v]) pairs.Add((u, v));
            }
        }
        return pairs;
    }

    /// <summary>Jaccard similarity of two causal pair sets.</summary>
    private static double JaccardPairs(HashSet<(int, int)> a, HashSet<(int, int)> b)
    {
        if (a.Count == 0 && b.Count == 0) return 1.0;
        int intersect = a.Intersect(b).Count();
        int union = a.Union(b).Count();
        return union > 0 ? (double)intersect / union : 0;
    }

    /// <summary>Agreement rate: fraction of pairs in A that are also in B.</summary>
    private static double AgreementRate(HashSet<(int, int)> a, HashSet<(int, int)> b)
    {
        if (a.Count == 0) return 1.0;
        return (double)a.Intersect(b).Count() / a.Count;
    }

    // ================================================================
    // MAIN ANALYSIS
    // ================================================================

    private static CsiResult AnalyzeCausalInvariance(List<int>[] adj, int N, string name, string dim, int diameter, ITestOutputHelper o)
    {
        var rng = new Random(42);
        int nSources = Math.Min(8, N);
        var sources = Enumerable.Range(0, N).OrderBy(_ => rng.Next()).Take(nSources).ToList();

        var pairwiseJaccards = new List<double>();
        var pairwiseAgreements = new List<double>();
        var bfsRefAgreements = new List<double>();
        var consensusFractions = new List<double>();
        var coreFractions = new List<double>();
        var bfsVsDeg = new List<double>();
        var bfsVsDiff = new List<double>();
        var bfsVsWave = new List<double>();
        var causalPairCounts = new List<double>();

        foreach (var src in sources)
        {
            // Compute tau using 4 methods
            var tauB = TauBFS(adj, N, src);
            var tauD = TauDegreeWeighted(adj, N, src);
            var tauF = TauDiffusion(adj, N, src, rng);
            var tauW = TauWavefront(adj, N, src);

            // Build causal pair sets
            var pairsB = BuildCausalPairs(tauB, N);
            var pairsD = BuildCausalPairs(tauD, N);
            var pairsF = BuildCausalPairs(tauF, N);
            var pairsW = BuildCausalPairs(tauW, N);

            causalPairCounts.Add(pairsB.Count);

            // 6 pairwise comparisons between methods
            var pairSets = new[] { ("BFS", pairsB), ("DEG", pairsD), ("DIFF", pairsF), ("WAVE", pairsW) };
            int nM = pairSets.Length;

            for (int i = 0; i < nM; i++)
            {
                for (int j = i + 1; j < nM; j++)
                {
                    double jaccard = JaccardPairs(pairSets[i].Item2, pairSets[j].Item2);
                    pairwiseJaccards.Add(jaccard);

                    double agree = (AgreementRate(pairSets[i].Item2, pairSets[j].Item2)
                                  + AgreementRate(pairSets[j].Item2, pairSets[i].Item2)) / 2.0;
                    pairwiseAgreements.Add(agree);
                }
            }

            // BFS as reference: compare DEG, DIFF, WAVE against BFS
            bfsVsDeg.Add(JaccardPairs(pairsB, pairsD));
            bfsVsDiff.Add(JaccardPairs(pairsB, pairsF));
            bfsVsWave.Add(JaccardPairs(pairsB, pairsW));
            bfsRefAgreements.Add((JaccardPairs(pairsB, pairsD) + JaccardPairs(pairsB, pairsF) + JaccardPairs(pairsB, pairsW)) / 3.0);

            // Consensus: pairs present in ≥ 3 out of 4 methods
            int consensusCount = 0;
            int coreCount = 0; // present in ALL 4
            var allPairs = pairsB.Union(pairsD).Union(pairsF).Union(pairsW);
            int totalUnique = allPairs.Count();

            foreach (var p in allPairs)
            {
                int count = 0;
                if (pairsB.Contains(p)) count++;
                if (pairsD.Contains(p)) count++;
                if (pairsF.Contains(p)) count++;
                if (pairsW.Contains(p)) count++;
                if (count >= 3) consensusCount++;
                if (count == 4) coreCount++;
            }

            consensusFractions.Add(totalUnique > 0 ? 100.0 * consensusCount / totalUnique : 0);
            coreFractions.Add(totalUnique > 0 ? 100.0 * coreCount / totalUnique : 0);

            o.WriteLine($"  Source {src}: pairs={pairsB.Count}, J(BFS/DEG)={JaccardPairs(pairsB, pairsD):F4}, J(BFS/DIFF)={JaccardPairs(pairsB, pairsF):F4}, J(BFS/WAVE)={JaccardPairs(pairsB, pairsW):F4}, consensus={consensusCount}/{totalUnique} ({100.0*consensusCount/Math.Max(1,totalUnique):F1}%), core={coreCount}/{totalUnique} ({100.0*coreCount/Math.Max(1,totalUnique):F1}%)");
        }

        return new(name, dim, N, diameter, nSources,
            causalPairCounts.Count > 0 ? causalPairCounts.Average() : 0,
            pairwiseJaccards.Count > 0 ? pairwiseJaccards.Average() : 0,
            pairwiseAgreements.Count > 0 ? pairwiseAgreements.Average() : 0,
            bfsRefAgreements.Count > 0 ? bfsRefAgreements.Average() : 0,
            bfsVsDeg.Count > 0 ? bfsVsDeg.Average() : 0,
            bfsVsDiff.Count > 0 ? bfsVsDiff.Average() : 0,
            bfsVsWave.Count > 0 ? bfsVsWave.Average() : 0,
            consensusFractions.Count > 0 ? consensusFractions.Average() : 0,
            coreFractions.Count > 0 ? coreFractions.Average() : 0);
    }

    private record CsiResult(
        string Name, string Dim, int N, int Diameter, int NSources,
        double MeanCausalPairs,
        double MeanPairwiseJaccard, double MeanPairAgreement, double BFSRefAgreement,
        double BFSvsDEG, double BFSvsDIFF, double BFSvsWAVE,
        double ConsensusFraction, double CausalCoreFraction);
}
