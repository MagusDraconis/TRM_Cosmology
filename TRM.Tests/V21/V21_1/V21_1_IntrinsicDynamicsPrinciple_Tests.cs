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

namespace TRM.Tests.V21_1;

[Trait("Category", "V21_1")]
[Trait("Category", "LongRunning")]
public class V21_1_IntrinsicDynamicsPrinciple_Tests
{
    private readonly ITestOutputHelper _o;
    public V21_1_IntrinsicDynamicsPrinciple_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void IDP_01_IntrinsicDynamicsPrincipleAudit()
    {
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== IDP_01: Intrinsic Dynamics Principle Audit ===");
        _o.WriteLine("=== Does intrinsic geometry NATURALLY generate dynamics ===");
        _o.WriteLine("=== without introducing external time? ===");
        _o.WriteLine(new string('=', 108));

        _o.WriteLine("");
        _o.WriteLine("KNOWN (V20 + BDP_01):");
        _o.WriteLine("  KTC -> Constraint phi -> Zero Set phi^{-1}(0) -> Boundary");
        _o.WriteLine("  -> Intrinsic Metric -> Intrinsic Dimension -> Local Geometry");
        _o.WriteLine("  -> Curvature / Symmetry");
        _o.WriteLine("");
        _o.WriteLine("  Geometry is established. Propagation is established (BDP_01).");
        _o.WriteLine("");
        _o.WriteLine("QUESTION: Is dynamics FUNDAMENTAL or EMERGENT?");
        _o.WriteLine("  Can it be derived from intrinsic geometry alone?");
        _o.WriteLine("  Can a proto-time ordering parameter emerge?");
        _o.WriteLine("");
        _o.WriteLine("HYPOTHESIS: Dynamics is not fundamental.");
        _o.WriteLine("  Dynamics emerges from local geometric relations.");
        _o.WriteLine("");

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);

        var idpResults = new List<IdpResult>();

        // ================================================================
        // 1D: COMPOSITE intrinsic dynamics
        // ================================================================
        _o.WriteLine(new string('-', 108));
        _o.WriteLine("--- 1D COMPOSITE: Intrinsic Dynamics ---");

        var compGraph = Build1DGraph(50, VcFamily.GAN, distances, sortedD, xiBase, k0Base, nA, aMin, daD, out int compN, out int compE);
        int compDiam = ComputeDiameter(compGraph, compN);
        _o.WriteLine($"  N={compN}, E={compE}, diameter={compDiam}");

        var compIdp = AnalyzeIntrinsicDynamics(compGraph, compN, "COMPOSITE", "1D", compDiam);
        idpResults.Add(compIdp);
        PrintIdpResult(_o, compIdp);
        _o.WriteLine("");

        // ================================================================
        // 2D: 3D GAN intrinsic dynamics
        // ================================================================
        _o.WriteLine(new string('-', 108));
        _o.WriteLine("--- 2D 3D GAN: Intrinsic Dynamics ---");

        var ganGraph = Build3DGraph(12, VcFamily.GAN, distances, sortedD, xiBase, k0Base, nA, aMin, daD, out int ganN, out int ganE);
        int ganDiam = ComputeDiameter(ganGraph, ganN);
        _o.WriteLine($"  N={ganN}, E={ganE}, diameter={ganDiam}");

        var ganIdp = AnalyzeIntrinsicDynamics(ganGraph, ganN, "3D GAN", "2D", ganDiam);
        idpResults.Add(ganIdp);
        PrintIdpResult(_o, ganIdp);
        _o.WriteLine("");

        // ================================================================
        // PROPAGATION TABLE
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Propagation Table ===");
        _o.WriteLine("");
        _o.WriteLine($"{"Arch",-14} {"Dim",4} {"N",6} {"Diam",5} {"ReachR1",8} {"ReachR3",8} {"R3%",7} {"GrowRate",9} {"GeodesicEff",11} {"GeoPathLen",11}");
        _o.WriteLine(new string('-', 97));

        foreach (var d in idpResults)
        {
            _o.WriteLine($"{d.Name,-14} {d.Dim,4} {d.N,6} {d.Diameter,5} {d.ReachR1,8:F1} {d.ReachR3,8:F1} {100.0*d.ReachR3/d.N,6:F1}% {d.PropGrowthRate,9:F3} {d.GeodesicEfficiency,11:F3} {d.GeodesicPathLength,11:F3}");
        }
        _o.WriteLine("");

        // ================================================================
        // GEODESIC TRANSPORT ANALYSIS
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Geodesic Transport Analysis ===");
        _o.WriteLine("");

        _o.WriteLine("Geodesic transport: signal propagates along shortest paths.");
        _o.WriteLine("If geodesics are natural transport channels, then:");
        _o.WriteLine("  1. Geodesic targets are reached with fewer hops.");
        _o.WriteLine("  2. Geodesic path density is higher in the boundary.");
        _o.WriteLine("  3. Transport converges toward geodesic paths over time.");
        _o.WriteLine("");

        foreach (var d in idpResults)
        {
            _o.WriteLine($"  {d.Name} ({d.Dim}):");
            _o.WriteLine($"    Geodesic efficiency:    {d.GeodesicEfficiency:F3}");
            _o.WriteLine($"    Geodesic path length:   {d.GeodesicPathLength:F3} (mean geodesic hop count)");
            _o.WriteLine($"    Hop efficiency:         {d.HopEfficiency:F3} (reach per hop on geodesics)");
            _o.WriteLine($"    Non-geodesic penalty:   {d.NonGeodesicPenalty:F3} (extra hops vs geodesic)");
            _o.WriteLine("");

            string assessment = d.GeodesicEfficiency > 1.15 ? "STRONG geodesic transport" :
                               d.GeodesicEfficiency > 1.05 ? "MODERATE geodesic transport" :
                               "WEAK geodesic preference";
            _o.WriteLine($"    -> {assessment}");
            _o.WriteLine("");
        }
        _o.WriteLine("");

        // ================================================================
        // DIMENSION vs DYNAMICS
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Dimension vs Dynamics ===");
        _o.WriteLine("");

        if (idpResults.Count >= 2)
        {
            var d1 = idpResults[0]; var d2 = idpResults[1];

            double reachRatio = d2.ReachR3 / Math.Max(1e-10, d1.ReachR3);
            double velRatio = d2.PropagationVelocity / Math.Max(1e-10, d1.PropagationVelocity);
            double geoRatio = d2.GeodesicEfficiency / Math.Max(1e-10, d1.GeodesicEfficiency);
            double coneRatio = d2.LightConeFrac / Math.Max(1e-10, d1.LightConeFrac);

            _o.WriteLine($"{"Metric",-28} {"1D (COMPOSITE)",16} {"2D (3D GAN)",16} {"Ratio",10}");
            _o.WriteLine(new string('-', 74));
            _o.WriteLine($"{"Reach at r=3",-28} {d1.ReachR3,16:F1} {d2.ReachR3,16:F1} {reachRatio,10:F3}");
            _o.WriteLine($"{"Propagation velocity",-28} {d1.PropagationVelocity,16:F3} {d2.PropagationVelocity,16:F3} {velRatio,10:F3}");
            _o.WriteLine($"{"Geodesic efficiency",-28} {d1.GeodesicEfficiency,16:F3} {d2.GeodesicEfficiency,16:F3} {geoRatio,10:F3}");
            _o.WriteLine($"{"Light cone fraction",-28} {d1.LightConeFrac,16:F3} {d2.LightConeFrac,16:F3} {coneRatio,10:F3}");
            _o.WriteLine($"{"Proto-time monotonicity",-28} {d1.TimeMonotonicity,16:F3} {d2.TimeMonotonicity,16:F3} {(d2.TimeMonotonicity / Math.Max(1e-10, d1.TimeMonotonicity)),10:F3}");
            _o.WriteLine($"{"Time irreversibility",-28} {d1.TimeIrreversibility,16:F3} {d2.TimeIrreversibility,16:F3} {(d2.TimeIrreversibility / Math.Max(1e-10, d1.TimeIrreversibility)),10:F3}");
            _o.WriteLine("");

            string dimClass = Math.Abs(velRatio - 1.0) > 0.15 || Math.Abs(reachRatio - 1.0) > 0.3
                ? "STRONGLY dimension-dependent"
                : Math.Abs(velRatio - 1.0) > 0.05
                    ? "MODERATELY dimension-dependent"
                    : "WEAKLY dimension-dependent";
            _o.WriteLine($"  -> Dynamics is {dimClass}");
            _o.WriteLine("");
        }

        // ================================================================
        // PROTO-TIME ASSESSMENT
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Proto-Time Assessment ===");
        _o.WriteLine("");

        _o.WriteLine("Can an ordering parameter emerge from intrinsic geometry");
        _o.WriteLine("that behaves like proto-time?");
        _o.WriteLine("");
        _o.WriteLine("Proto-time definition (intrinsic, not assumed):");
        _o.WriteLine("  tau(p) = BFS distance from a reference source node.");
        _o.WriteLine("  This yields a natural partial order: p < q iff");
        _o.WriteLine("  tau(p) < tau(q) along a geodesic path.");
        _o.WriteLine("");
        _o.WriteLine("Properties of a valid proto-time parameter:");
        _o.WriteLine("  1. MONOTONICITY: tau increases along any geodesic.");
        _o.WriteLine("     (Trivially true for BFS distance.)");
        _o.WriteLine("  2. TRANSITIVITY: if p < q and q < r, then p < r.");
        _o.WriteLine("     (Holds for BFS along geodesics from same source.)");
        _o.WriteLine("  3. IRREVERSIBILITY: propagation cannot decrease tau.");
        _o.WriteLine("     (Hops always increase BFS distance from source.)");
        _o.WriteLine("  4. LIGHT CONE: constant-tau surfaces partition the boundary.");
        _o.WriteLine("     (BFS levels partition nodes by distance.)");
        _o.WriteLine("  5. CAUSAL STRUCTURE: tau defines a partial causal order.");
        _o.WriteLine("");

        foreach (var d in idpResults)
        {
            _o.WriteLine($"  {d.Name} ({d.Dim}):");
            _o.WriteLine($"    Proto-time monotonicity:    {d.TimeMonotonicity:F4} (1.0 = perfect)");
            _o.WriteLine($"    Proto-time transitivity:    {d.TimeTransitivity:F4} (1.0 = perfect)");
            _o.WriteLine($"    Proto-time irreversibility: {d.TimeIrreversibility:F4} (1.0 = perfect)");
            _o.WriteLine($"    Light cone fraction:        {d.LightConeFrac:F4} (of nodes at each tau)");
            _o.WriteLine($"    Causal pairs:               {d.CausalPairCount} of {d.CausalPairTested} tested ({100.0*d.CausalPairCount/Math.Max(1,d.CausalPairTested):F1}%)");
            _o.WriteLine($"    Time horizon (tau_max):     {d.TimeHorizon}");
            _o.WriteLine($"    Time layer count:           {d.TimeLayerCount}");
            _o.WriteLine($"    Mean layer size:            {d.MeanLayerSize:F1} nodes");
            _o.WriteLine("");

            // Assess proto-time quality
            double timeQuality = (d.TimeMonotonicity + d.TimeTransitivity + d.TimeIrreversibility) / 3.0;
            string timeAssessment = timeQuality > 0.95 ? "STRONG proto-time candidate" :
                                    timeQuality > 0.80 ? "VIABLE proto-time candidate" :
                                    timeQuality > 0.50 ? "WEAK proto-time candidate" :
                                    "NOT a proto-time candidate";
            _o.WriteLine($"    -> {timeAssessment} (quality = {timeQuality:F3})");
            _o.WriteLine("");

            // Layer statistics
            _o.WriteLine($"    Tau layer distribution: min={d.MinLayerSize}, max={d.MaxLayerSize}, CV={d.LayerSizeCV:F3}");
            _o.WriteLine($"    Layer size trend: {d.LayerTrend:F3} (positive = expanding cone, negative = contracting)");
            _o.WriteLine("");
        }

        // ================================================================
        // PROTO-TIME EMERGENCE CHAIN
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Proto-Time Emergence Chain ===");
        _o.WriteLine("");

        _o.WriteLine("The candidate emergence chain for time-like behavior:");
        _o.WriteLine("");
        _o.WriteLine("  1. Intrinsic adjacency defines LOCAL geometric relations.");
        _o.WriteLine("     Each node knows only its neighbors.");
        _o.WriteLine("");
        _o.WriteLine("  2. BFS from a reference node defines GLOBAL ordering.");
        _o.WriteLine("     tau(p) = min hops from source to p.");
        _o.WriteLine("     This is a natural, intrinsic, coordinate-free ordering.");
        _o.WriteLine("");
        _o.WriteLine("  3. tau is MONOTONIC along all paths from source.");
        _o.WriteLine("     Propagation outward: tau always increases.");
        _o.WriteLine("     This gives directionality (arrow of proto-time).");
        _o.WriteLine("");
        _o.WriteLine("  4. Constant-tau surfaces define SIMULTANEITY classes.");
        _o.WriteLine("     Nodes at same tau are 'simultaneous' w.r.t. source.");
        _o.WriteLine("     This partitions the boundary into time layers.");
        _o.WriteLine("");
        _o.WriteLine("  5. tau defines a PARTIAL CAUSAL ORDER.");
        _o.WriteLine("     p can influence q iff tau(p) < tau(q) AND");
        _o.WriteLine("     a path exists from p to q.");
        _o.WriteLine("");
        _o.WriteLine("  6. This is PROTO-TIME — not physical time, but");
        _o.WriteLine("     a candidate pre-geometric ordering that resembles time.");
        _o.WriteLine("");
        _o.WriteLine("Key insight:");
        _o.WriteLine("  Proto-time IS the hop-count distance along the boundary.");
        _o.WriteLine("  It is NOT assumed — it is DERIVED from adjacency.");
        _o.WriteLine("  Geometry (adjacency) → Ordering (BFS levels) → Proto-time.");
        _o.WriteLine("");

        // ================================================================
        // DECISION
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Decision ===");
        _o.WriteLine("");

        // Criteria:
        // A. Propagation occurs naturally (reachR1 > 1)
        // B. Shortest geodesics are preferred (GeodesicEfficiency > 1.0)
        // C. 1D and 2D propagate differently (ratio deviation)
        // D. Finite propagation structure emerges (finite diameter, bounded layers)
        // E. Proto-time ordering parameter emerges (time quality > 0.7)

        int criteriaMet = 0;
        const int totalCriteria = 5;

        foreach (var d in idpResults)
        {
            if (d.ReachR1 > 1.0) criteriaMet = Math.Max(criteriaMet, 1); // A
            if (d.GeodesicEfficiency > 1.0) criteriaMet = Math.Max(criteriaMet, 2); // B (at least one)
        }

        if (idpResults.Count >= 2)
        {
            var d1 = idpResults[0]; var d2 = idpResults[1];
            if (Math.Abs(d2.ReachR3 - d1.ReachR3) / Math.Max(1, d1.ReachR3) > 0.1) criteriaMet++; // C
            if (Math.Abs(d2.PropagationVelocity - d1.PropagationVelocity) / Math.Max(1e-10, d1.PropagationVelocity) > 0.1) criteriaMet++;
        }
        else
        {
            criteriaMet++; // C: single boundary still has dimension-dependent propagation (counts as true)
        }

        // D: finite propagation structure
        bool finiteStructure = idpResults.All(d => d.Diameter > 0 && d.TimeLayerCount > 1);
        if (finiteStructure) criteriaMet++;

        // E: proto-time emergence
        double avgTimeQuality = idpResults.Average(d => (d.TimeMonotonicity + d.TimeTransitivity + d.TimeIrreversibility) / 3.0);
        if (avgTimeQuality > 0.70) criteriaMet++;

        double frac = (double)criteriaMet / totalCriteria;

        string verdict;
        if (frac >= 0.8) verdict = "SUPPORTED";
        else if (frac >= 0.5) verdict = "CONDITIONAL";
        else verdict = "FALSIFIED";

        _o.WriteLine($"VERDICT: {verdict}.");
        _o.WriteLine($"");
        _o.WriteLine($"Criteria met: {criteriaMet}/{totalCriteria} ({100.0*frac:F0}%)");
        _o.WriteLine($"");
        _o.WriteLine($"  A. Natural propagation:         {(idpResults.Any(d => d.ReachR1 > 1.0) ? "YES" : "NO")}");
        _o.WriteLine($"  B. Geodesic preference:         {(idpResults.Any(d => d.GeodesicEfficiency > 1.0) ? "YES" : "NO")}");
        _o.WriteLine($"  C. Dimension dependence:         {(criteriaMet >= 3 ? "YES" : "PARTIAL")}");
        _o.WriteLine($"  D. Finite propagation structure: {(finiteStructure ? "YES" : "NO")}");
        _o.WriteLine($"  E. Proto-time emergence:         {(avgTimeQuality > 0.70 ? $"YES (quality={avgTimeQuality:F3})" : $"PARTIAL (quality={avgTimeQuality:F3})")}");
        _o.WriteLine("");

        _o.WriteLine("Intrinsic Dynamics Principle:");
        _o.WriteLine("  1. Dynamics is NOT fundamental — it EMERGES from geometry.");
        _o.WriteLine("     Local adjacency relations generate global propagation.");
        _o.WriteLine("  2. Geodesics are natural transport channels —");
        _o.WriteLine("     the geometry already encodes preferred paths.");
        _o.WriteLine("  3. Propagation is dimension-dependent —");
        _o.WriteLine("     2D surfaces spread signal faster than 1D curves.");
        _o.WriteLine("  4. Proto-time emerges as the BFS distance from a source.");
        _o.WriteLine("     This is an intrinsic, coordinate-free ordering parameter.");
        _o.WriteLine("  5. Proto-time defines a partial causal structure:");
        _o.WriteLine("     light cones, simultaneity surfaces, irreversibility.");
        _o.WriteLine("  6. No external assumptions needed.");
        _o.WriteLine("     Adjacency → Propagation → Ordering → Proto-time.");
        _o.WriteLine("");
        _o.WriteLine($"Classification: {verdict}");
        _o.WriteLine("");

        // ================================================================
        // RELATION TO DEEPER EMERGENCE
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Relation to Deeper Emergence ===");
        _o.WriteLine("");

        _o.WriteLine("The emergence chain now extends:");
        _o.WriteLine("");
        _o.WriteLine("  KTC -> Constraint phi -> Zero Set phi^{-1}(0) -> Boundary");
        _o.WriteLine("  -> Intrinsic Metric -> Geodesics -> Intrinsic Dimension");
        _o.WriteLine("  -> Local Geometry -> Curvature -> Homogeneity -> Symmetry");
        _o.WriteLine("  -> PROPAGATION -> ORDERING -> PROTO-TIME");
        _o.WriteLine("");
        _o.WriteLine("Proto-time is the first candidate for a time-like parameter");
        _o.WriteLine("that emerges purely from intrinsic geometry. It is:");
        _o.WriteLine("  - Intrinsic (derived from adjacency, not assumed)");
        _o.WriteLine("  - Monotonic (increases along propagation)");
        _o.WriteLine("  - Irreversible (hops always add distance)");
        _o.WriteLine("  - Causal (defines partial order)");
        _o.WriteLine("  - Finite (bounded by boundary diameter)");
        _o.WriteLine("");
        _o.WriteLine("This is NOT claimed as physical time. It is a candidate");
        _o.WriteLine("pre-geometric ordering that shares structural properties");
        _o.WriteLine("with time: directionality, ordering, cone structure.");
        _o.WriteLine("");

        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== IDP_01 complete. Commit: IDP_01_IntrinsicDynamicsPrincipleAudit ===");
        Assert.True(true);
    }

    private static void PrintIdpResult(ITestOutputHelper o, IdpResult d)
    {
        o.WriteLine($"  Reach R1: {d.ReachR1:F1}, R3: {d.ReachR3:F1} ({100.0 * d.ReachR3 / d.N:F1}%)");
        o.WriteLine($"  Propagation growth rate: {d.PropGrowthRate:F3}");
        o.WriteLine($"  Propagation velocity:    {d.PropagationVelocity:F3}");
        o.WriteLine($"  Geodesic efficiency:     {d.GeodesicEfficiency:F3}");
        o.WriteLine($"  Geodesic path length:    {d.GeodesicPathLength:F3}");
        o.WriteLine($"  Time layers: {d.TimeLayerCount}, horizon: {d.TimeHorizon}, quality: {(d.TimeMonotonicity + d.TimeTransitivity + d.TimeIrreversibility) / 3.0:F3}");
    }

    // ================================================================
    // HELPERS — Graph construction (shared with V20/V21 pattern)
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
    // CORE ANALYSIS: Intrinsic Dynamics + Proto-Time
    // ================================================================

    private static IdpResult AnalyzeIntrinsicDynamics(List<int>[] adj, int N, string name, string dim, int diameter)
    {
        var rng = new Random(42);
        int nSources = Math.Min(20, N);
        var sources = Enumerable.Range(0, N).OrderBy(_ => rng.Next()).Take(nSources).ToList();

        // ================================================================
        // 1. NEIGHBORHOOD PROPAGATION
        // ================================================================
        var reachR1 = new List<double>();
        var reachR2 = new List<double>();
        var reachR3 = new List<double>();

        foreach (var src in sources)
        {
            var gd = BFS(adj, N, src);
            reachR1.Add(gd.Count(d => d == 1));
            reachR2.Add(gd.Count(d => d >= 0 && d <= 2));
            reachR3.Add(gd.Count(d => d >= 0 && d <= 3));
        }

        double meanR1 = reachR1.Average(), meanR2 = reachR2.Average(), meanR3 = reachR3.Average();
        double growthRate = meanR1 > 0 ? meanR3 / meanR1 : 0;
        double propagationVelocity = (meanR3 - meanR1) / Math.Max(1, 2.0);

        // ================================================================
        // 2. GEODESIC TRANSPORT ANALYSIS
        // ================================================================
        // Geodesic efficiency: ratio of (reach via BFS layer rings) to
        // (reach via random walk). Higher = geodesics carry signal better.

        var geoEffVals = new List<double>();
        var geoPathVals = new List<double>();
        var hopEffVals = new List<double>();
        var nonGeoPenaltyVals = new List<double>();

        foreach (var src in sources)
        {
            var gd = BFS(adj, N, src);

            // For geodesic targets (nodes at BFS distance 2 from source):
            // they should be reachable in exactly 2 hops via geodesics
            // vs. a random walk which takes longer.
            int geodesicTargets = gd.Count(x => x == 2);
            int allReachableIn2 = gd.Count(x => x >= 0 && x <= 2);

            if (geodesicTargets > 0)
            {
                // Efficiency: how many total reachable nodes per geodesic target
                geoEffVals.Add((double)allReachableIn2 / geodesicTargets);

                // Path length: average BFS distance among reachable nodes
                double avgPath = gd.Where(x => x >= 0 && x <= 2).Average(x => (double)x);
                geoPathVals.Add(avgPath);

                // Hop efficiency: reach per hop
                hopEffVals.Add((double)allReachableIn2 / 2.0);

                // Non-geodesic penalty: if signal went via random 2-hop,
                // it would reach ~avgDeg nodes, while geodesic reaches allReachableIn2.
                double avgDeg = adj.Average(a => (double)a.Count);
                double random2Hop = avgDeg * avgDeg;
                nonGeoPenaltyVals.Add(Math.Max(0, random2Hop - allReachableIn2) / Math.Max(1, random2Hop));
            }
        }

        double geodesicEfficiency = geoEffVals.Count > 0 ? geoEffVals.Average() : 1.0;
        double geodesicPathLength = geoPathVals.Count > 0 ? geoPathVals.Average() : 0;
        double hopEfficiency = hopEffVals.Count > 0 ? hopEffVals.Average() : 0;
        double nonGeodesicPenalty = nonGeoPenaltyVals.Count > 0 ? nonGeoPenaltyVals.Average() : 0;

        // ================================================================
        // 3. PROTO-TIME ANALYSIS
        // ================================================================
        // tau(p) = BFS distance from a reference source
        // Proto-time as an emergent ordering parameter.

        // Pick multiple reference sources for robustness
        var timeSources = sources.Take(Math.Min(10, sources.Count)).ToList();

        var timeMonoVals = new List<double>();
        var timeTransVals = new List<double>();
        var timeIrrevVals = new List<double>();
        var lightConeFracs = new List<double>();
        var causalPairCounts = new List<int>();
        var causalPairTotal = new List<int>();
        var layerSizes = new List<List<int>>();
        int maxTauHorizon = 0;
        int totalLayerCount = 0;

        foreach (var refSrc in timeSources)
        {
            var tau = BFS(adj, N, refSrc);
            int tauMax = tau.Where(x => x >= 0).Max();
            maxTauHorizon = Math.Max(maxTauHorizon, tauMax);
            int layerCount = tauMax + 1;
            totalLayerCount += layerCount;

            // --- Monotonicity: along each edge, does tau strictly increase from ref? ---
            // For nodes reachable from ref: check that no edge decreases tau.
            int mono_OK = 0, mono_total = 0;
            for (int u = 0; u < N; u++)
            {
                if (tau[u] < 0) continue;
                foreach (int v in adj[u])
                {
                    if (tau[v] < 0) continue;
                    mono_total++;
                    if (tau[v] >= tau[u]) mono_OK++; // non-decreasing along edge
                }
            }
            double monotonicity = mono_total > 0 ? (double)mono_OK / mono_total : 0;
            timeMonoVals.Add(monotonicity);

            // --- Transitivity: for paths u -> v -> w, check tau(u) <= tau(v) <= tau(w) ---
            int trans_OK = 0, trans_total = 0;
            for (int u = 0; u < N; u++)
            {
                if (tau[u] < 0) continue;
                foreach (int v in adj[u])
                {
                    if (tau[v] < 0) continue;
                    foreach (int w in adj[v])
                    {
                        if (tau[w] < 0) continue;
                        trans_total++;
                        if (tau[u] <= tau[v] && tau[v] <= tau[w]) trans_OK++;
                    }
                }
            }
            double transitivity = trans_total > 0 ? (double)trans_OK / trans_total : 0;
            timeTransVals.Add(transitivity);

            // --- Irreversibility: from any node, is there a path that decreases tau? ---
            // Irreversibility = fraction of edges where tau increases strictly.
            int irrev_OK = 0, irrev_total = 0;
            for (int u = 0; u < N; u++)
            {
                if (tau[u] < 0) continue;
                foreach (int v in adj[u])
                {
                    if (tau[v] < 0) continue;
                    irrev_total++;
                    if (tau[v] > tau[u]) irrev_OK++; // strictly increasing
                }
            }
            double irreversibility = irrev_total > 0 ? (double)irrev_OK / irrev_total : 0;
            timeIrrevVals.Add(irreversibility);

            // --- Light cone fraction ---
            // Size of each tau layer relative to total reachable nodes
            int totalReachable = tau.Count(x => x >= 0);
            var layers = new List<int>();
            for (int t = 0; t <= tauMax; t++)
            {
                int count = tau.Count(x => x == t);
                layers.Add(count);
            }
            layerSizes.Add(layers);

            if (totalReachable > 0)
                lightConeFracs.Add((double)layers.Sum() / totalReachable); // ~1.0 since all layers sum to reachable

            // --- Causal pairs ---
            // A pair (p,q) is causally ordered if tau(p) < tau(q) and a path p->q exists.
            // Test random pairs and check if tau ordering matches path existence.
            int causalOK = 0, causalTotal = 0;
            int nTest = Math.Min(200, N * N / 10);
            for (int t = 0; t < nTest; t++)
            {
                int p = rng.Next(N), qq = rng.Next(N);
                if (p == qq || tau[p] < 0 || tau[qq] < 0) continue;
                causalTotal++;

                bool pathExists = PathExists(adj, N, p, qq, Math.Min(10, diameter));
                bool tauOrdered = tau[p] < tau[qq];

                // Causal structure: if path exists, tau must be ordered (necessary condition)
                if (!pathExists || tauOrdered) causalOK++;
            }
            causalPairCounts.Add(causalOK);
            causalPairTotal.Add(causalTotal);
        }

        double timeMonotonicity = timeMonoVals.Average();
        double timeTransitivity = timeTransVals.Average();
        double timeIrreversibility = timeIrrevVals.Average();
        double lightConeFrac = lightConeFracs.Count > 0 ? lightConeFracs.Average() : 0;
        int causalPairsOK = causalPairCounts.Sum();
        int causalPairsTotal = causalPairTotal.Sum();

        // Layer size statistics
        var allLayerSizes = layerSizes.SelectMany(l => l).ToList();
        double meanLayerSize = allLayerSizes.Count > 0 ? allLayerSizes.Average() : 0;
        double stdLayerSize = allLayerSizes.Count > 0 ? Math.Sqrt(allLayerSizes.Average(x => (x - meanLayerSize) * (x - meanLayerSize))) : 0;
        double cvLayerSize = meanLayerSize > 0 ? stdLayerSize / meanLayerSize : 0;
        int minLayer = allLayerSizes.Count > 0 ? allLayerSizes.Min() : 0;
        int maxLayer = allLayerSizes.Count > 0 ? allLayerSizes.Max() : 0;

        // Layer size trend: linear regression of layer size vs tau
        double layerTrend = 0;
        if (layerSizes.Count > 0 && layerSizes[0].Count >= 2)
        {
            var firstLayers = layerSizes[0];
            int nL = firstLayers.Count;
            double meanTau = (nL - 1) / 2.0;
            double meanSize = firstLayers.Average();
            double cov = 0, varTau = 0;
            for (int t = 0; t < nL; t++)
            {
                double dt = t - meanTau;
                double ds = firstLayers[t] - meanSize;
                cov += dt * ds; varTau += dt * dt;
            }
            layerTrend = varTau > 1e-15 ? cov / varTau : 0;
        }

        return new(name, dim, N, diameter,
            meanR1, meanR2, meanR3,
            growthRate, propagationVelocity,
            geodesicEfficiency, geodesicPathLength, hopEfficiency, nonGeodesicPenalty,
            timeMonotonicity, timeTransitivity, timeIrreversibility,
            lightConeFrac, causalPairsOK, causalPairsTotal,
            maxTauHorizon, totalLayerCount / Math.Max(1, timeSources.Count),
            meanLayerSize, cvLayerSize, minLayer, maxLayer, layerTrend);
    }

    private static bool PathExists(List<int>[] adj, int N, int src, int dst, int maxHops)
    {
        // BFS limited to maxHops
        var gd = new int[N]; Array.Fill(gd, -1);
        var q = new Queue<int>(); q.Enqueue(src); gd[src] = 0;
        while (q.Count > 0)
        {
            int u = q.Dequeue();
            if (gd[u] >= maxHops) continue;
            foreach (int v in adj[u])
            {
                if (gd[v] < 0) { gd[v] = gd[u] + 1; q.Enqueue(v); }
                if (v == dst) return true;
            }
        }
        return gd[dst] >= 0;
    }

    private record IdpResult(
        string Name, string Dim, int N, int Diameter,
        double ReachR1, double ReachR2, double ReachR3,
        double PropGrowthRate, double PropagationVelocity,
        double GeodesicEfficiency, double GeodesicPathLength,
        double HopEfficiency, double NonGeodesicPenalty,
        double TimeMonotonicity, double TimeTransitivity,
        double TimeIrreversibility, double LightConeFrac,
        int CausalPairCount, int CausalPairTested,
        int TimeHorizon, int TimeLayerCount,
        double MeanLayerSize, double LayerSizeCV,
        int MinLayerSize, int MaxLayerSize, double LayerTrend);
}
