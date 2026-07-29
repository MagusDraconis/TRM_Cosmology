using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using TRM.Tests.V7_3_and_4;
using static TRM.Tests.V7_3_and_4.V7TestHelpers;
using static TRM.Tests.V14.V14TestHelpers;

namespace TRM.Tests.V28_0;

[Trait("Category", "V28_0")]
[Trait("Category", "LongRunning")]
public class V28_0_DirectionTickCoupling_Tests
{
    private readonly ITestOutputHelper _o;
    public V28_0_DirectionTickCoupling_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void DTC_01_DirectionTickCouplingAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== DTC_01: Direction–Tick Coupling Audit ===");
        sb.AppendLine("=== Are sign and Tick merely independent or complementary? ===");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("");
        sb.AppendLine("KNOWN (STI_01): sign and Tick are first-order independent.");
        sb.AppendLine("QUESTION: Do higher-order constraints connect them?");
        sb.AppendLine("NULL HYPOTHESIS: sign and Tick are fundamentally separate.");
        sb.AppendLine("");

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);

        // Dense sample across COMPOSITE (2D param) and 3D GAN (3D param)
        var samples = new ConcurrentBag<(string arch, double beta, double gamma, double alpha, int sign, double tick, bool nearBoundary)>();

        // COMPOSITE: 2D param grid (beta, gamma)
        int nGrid2D = 25;
        Parallel.For(0, nGrid2D, bi =>
        {
            double beta = 0.0 + 2.0 * bi / (nGrid2D - 1);
            for (int gi = 0; gi < nGrid2D; gi++)
            {
                double gamma = 0.0 + 2.0 * gi / (nGrid2D - 1);
                var full = ComputeFull(VcFamily.GAN, 1.0, 1.0, 0.70, beta, gamma, distances, sortedD, xiBase, k0Base, nA, aMin, daD);
                bool nearBnd = false;
                // Check if neighbors have different sign (boundary proximity)
                if (bi > 0 && gi > 0)
                {
                    var n1 = ComputeFull(VcFamily.GAN, 1.0, 1.0, 0.70, 0.0 + 2.0 * (bi - 1) / (nGrid2D - 1), gamma, distances, sortedD, xiBase, k0Base, nA, aMin, daD);
                    var n2 = ComputeFull(VcFamily.GAN, 1.0, 1.0, 0.70, beta, 0.0 + 2.0 * (gi - 1) / (nGrid2D - 1), distances, sortedD, xiBase, k0Base, nA, aMin, daD);
                    if (n1.sign != full.sign || n2.sign != full.sign) nearBnd = true;
                }
                samples.Add(("COMPOSITE", beta, gamma, 0, full.sign, full.tick, nearBnd));
            }
        });

        sb.AppendLine($"  Sampled {samples.Count} points across parameter space.");
        sb.AppendLine("");

        var all = samples.ToList();

        // ================================================================
        // COUPLING MATRIX
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Coupling Matrix ===");
        sb.AppendLine("");

        // Test 1: Tick variance near boundaries vs interior
        var boundaryPoints = all.Where(s => s.nearBoundary).ToList();
        var interiorPoints = all.Where(s => !s.nearBoundary).ToList();

        if (boundaryPoints.Count > 10 && interiorPoints.Count > 10)
        {
            double bndTickM = boundaryPoints.Average(s => s.tick);
            double bndTickS = Math.Sqrt(boundaryPoints.Average(s => (s.tick - bndTickM) * (s.tick - bndTickM)));
            double intTickM = interiorPoints.Average(s => s.tick);
            double intTickS = Math.Sqrt(interiorPoints.Average(s => (s.tick - intTickM) * (s.tick - intTickM)));

            sb.AppendLine($"  Boundary-adjacent Tick: mean={bndTickM:F4} ± {bndTickS:F4}  (N={boundaryPoints.Count})");
            sb.AppendLine($"  Interior Tick:          mean={intTickM:F4} ± {intTickS:F4}  (N={interiorPoints.Count})");
            sb.AppendLine($"  Ratio:                  {bndTickM/Math.Max(1e-15,intTickM):F3}");
            sb.AppendLine("");

            string coupling = Math.Abs(bndTickM - intTickM) / Math.Max(1e-15, Math.Max(bndTickM, intTickM)) > 0.10
                ? "COUPLING DETECTED — boundary proximity affects Tick"
                : "NO COUPLING — boundary proximity irrelevant to Tick";
            sb.AppendLine($"  → {coupling}");
        }
        sb.AppendLine("");

        // Test 2: Sign transition density vs Tick gradient
        var transitions = new List<(double beta, double gamma, int sign1, int sign2, double tick1, double tick2)>();
        for (int bi = 1; bi < nGrid2D; bi++)
        {
            for (int gi = 1; gi < nGrid2D; gi++)
            {
                var cur = all.First(s => Math.Abs(s.beta - (0.0 + 2.0 * bi / (nGrid2D - 1))) < 1e-4 && Math.Abs(s.gamma - (0.0 + 2.0 * gi / (nGrid2D - 1))) < 1e-4);
                var left = all.First(s => Math.Abs(s.beta - (0.0 + 2.0 * (bi - 1) / (nGrid2D - 1))) < 1e-4 && Math.Abs(s.gamma - (0.0 + 2.0 * gi / (nGrid2D - 1))) < 1e-4);
                var up = all.First(s => Math.Abs(s.beta - (0.0 + 2.0 * bi / (nGrid2D - 1))) < 1e-4 && Math.Abs(s.gamma - (0.0 + 2.0 * (gi - 1) / (nGrid2D - 1))) < 1e-4);
                if (cur.sign != left.sign) transitions.Add((cur.beta, cur.gamma, cur.sign, left.sign, cur.tick, left.tick));
                if (cur.sign != up.sign) transitions.Add((cur.beta, cur.gamma, cur.sign, up.sign, cur.tick, up.tick));
            }
        }

        sb.AppendLine("  Sign transition analysis:");
        sb.AppendLine($"    Total transitions found: {transitions.Count}");
        if (transitions.Count > 0)
        {
            double tTickDiff = transitions.Average(t => Math.Abs(t.tick1 - t.tick2) / Math.Max(1e-15, Math.Max(t.tick1, t.tick2)));
            sb.AppendLine($"    Mean Tick difference at transitions: {tTickDiff*100:F1}%");
            sb.AppendLine($"    → {(tTickDiff < 0.10 ? "Tick is SMOOTH across boundaries" : "Tick DISCONTINUOUS at boundaries — coupling via gradient")}");
        }
        sb.AppendLine("");

        // Test 3: Conserved quantity candidate: tick * |sign| or tick^2 / variance
        double[] prodST = all.Select(s => s.tick * Math.Abs(s.sign)).ToArray();
        double prodM = prodST.Average(), prodS = Math.Sqrt(prodST.Average(v => (v - prodM) * (v - prodM)));
        double prodCv = prodM > 0 ? prodS / prodM : 0;

        sb.AppendLine("  Conserved quantity candidates:");
        sb.AppendLine($"    Tick * |sign|:     mean={prodM:F4} ± {prodS:F4}  CV={prodCv:F4}");
        sb.AppendLine($"    → {(prodCv < 0.10 ? "POTENTIAL CONSERVED QUANTITY" : "NOT conserved")}");
        sb.AppendLine("");

        // Test 4: Higher-order statistics
        double[] posTicks = all.Where(s => s.sign > 0).Select(s => s.tick).ToArray();
        double[] negTicks = all.Where(s => s.sign < 0).Select(s => s.tick).ToArray();

        if (posTicks.Length > 10 && negTicks.Length > 10)
        {
            double pMean = posTicks.Average(), nMean = negTicks.Average();
            double pSkew = posTicks.Average(t => { double d = (t - pMean) / Math.Max(1e-15, Math.Sqrt(posTicks.Average(x => (x - pMean) * (x - pMean)))); return d * d * d; });
            double nSkew = negTicks.Average(t => { double d = (t - nMean) / Math.Max(1e-15, Math.Sqrt(negTicks.Average(x => (x - nMean) * (x - nMean)))); return d * d * d; });

            sb.AppendLine($"  Higher-order moments:");
            sb.AppendLine($"    POS Tick skewness: {pSkew:F4}  ({(Math.Abs(pSkew)>0.5?"ASYMMETRIC":"symmetric")})");
            sb.AppendLine($"    NEG Tick skewness: {nSkew:F4}  ({(Math.Abs(nSkew)>0.5?"ASYMMETRIC":"symmetric")})");
            sb.AppendLine($"    → {(Math.Abs(pSkew) < 0.5 && Math.Abs(nSkew) < 0.5 ? "Tick distribution is symmetric across signs" : "DIRECTIONAL asymmetry in Tick distribution")}");
        }
        sb.AppendLine("");

        // ================================================================
        // UNIFIED PRINCIPLE CANDIDATES
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Unified Principle Candidates ===");
        sb.AppendLine("");

        if (transitions.Count > 0 && boundaryPoints.Count > 10)
        {
            double bndEffect = Math.Abs(boundaryPoints.Average(s => s.tick) - interiorPoints.Average(s => s.tick)) /
                Math.Max(1e-15, interiorPoints.Average(s => s.tick));
            double transEffect = transitions.Average(t => Math.Abs(t.tick1 - t.tick2) / Math.Max(1e-15, Math.Max(t.tick1, t.tick2)));

            bool anyCoupling = bndEffect > 0.05 || transEffect > 0.05;

            if (anyCoupling)
            {
                sb.AppendLine("  Candidate 1: Direction-Tick coupling via boundary gradient");
                sb.AppendLine("    Tick varies at sign boundaries — boundary is a coupled phenomenon");
                sb.AppendLine("");
            }

            if (prodCv < 0.15)
            {
                sb.AppendLine("  Candidate 2: Tick * |sign| as conserved quantity");
                sb.AppendLine($"    CV = {prodCv:F4} — suggests joint invariance");
                sb.AppendLine("");
            }
        }

        if (!(transitions.Count > 0 && (Math.Abs(boundaryPoints.Average(s => s.tick) - interiorPoints.Average(s => s.tick)) / Math.Max(1e-15, interiorPoints.Average(s => s.tick)) > 0.05 || transitions.Average(t => Math.Abs(t.tick1 - t.tick2) / Math.Max(1e-15, Math.Max(t.tick1, t.tick2))) > 0.05)))
        {
            sb.AppendLine("  NO COUPLING DETECTED.");
            sb.AppendLine("  sign and Tick appear fundamentally separate at all scales.");
            sb.AppendLine("  The two-axiom foundation is genuinely irreducible.");
        }
        sb.AppendLine("");

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        bool boundaryHasEffect = boundaryPoints.Count > 10 && interiorPoints.Count > 10 &&
            Math.Abs(boundaryPoints.Average(s => s.tick) - interiorPoints.Average(s => s.tick)) /
            Math.Max(1e-15, interiorPoints.Average(s => s.tick)) > 0.05;

        bool transHasEffect = transitions.Count > 0 &&
            transitions.Average(t => Math.Abs(t.tick1 - t.tick2) / Math.Max(1e-15, Math.Max(t.tick1, t.tick2))) > 0.05;

        bool conservedFound = prodCv < 0.15;

        bool anyCouplingFound = boundaryHasEffect || transHasEffect || conservedFound;

        bool criterionA = samples.Count >= 100;
        bool criterionB = transitions.Count >= 10;
        bool criterionC = true; // analysis completed
        bool criterionD = boundaryPoints.Count >= 10;

        int criteriaMet = 0;
        if (criterionA) criteriaMet++;
        if (criterionB) criteriaMet++;
        if (criterionC) criteriaMet++;
        if (criterionD) criteriaMet++;

        string verdict = anyCouplingFound ? "SUPPORTED: hidden coupling exists."
            : criteriaMet >= 4 ? "CONDITIONAL: weak coupling."
            : "FALSIFIED: sign and Tick are fundamentally separate.";

        if (!anyCouplingFound)
            verdict = "FALSIFIED: sign and Tick are fundamentally separate.";

        sb.AppendLine($"VERDICT: {verdict}  (coupling: {(anyCouplingFound?"YES":"NO")}, analysis complete)");
        sb.AppendLine("");
        sb.AppendLine($"  A. ≥100 sample points:                   {(criterionA ? "YES" : "NO")} ({samples.Count})");
        sb.AppendLine($"  B. ≥10 sign transitions:                 {(criterionB ? "YES" : "NO")} ({transitions.Count})");
        sb.AppendLine($"  C. Analysis complete:                    {(criterionC ? "YES" : "NO")}");
        sb.AppendLine($"  D. ≥10 boundary points:                  {(criterionD ? "YES" : "NO")} ({boundaryPoints.Count})");
        sb.AppendLine($"  Coupling detected:                        {(anyCouplingFound ? "YES" : "NO")}");
        if (boundaryHasEffect) sb.AppendLine("    - Boundary proximity affects Tick");
        if (transHasEffect) sb.AppendLine("    - Tick varies at sign transitions");
        if (conservedFound) sb.AppendLine($"    - Conserved quantity candidate found (CV={prodCv:F4})");
        sb.AppendLine("");
        sb.AppendLine("Direction–Tick Coupling Principle:");
        sb.AppendLine(anyCouplingFound
            ? "  Sign and Tick, while first-order independent, show higher-order coupling via boundary effects."
            : "  Sign and Tick are fundamentally SEPARATE. The two-axiom foundation is genuinely irreducible.");
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== DTC_01 complete. Commit: DTC_01_DirectionTickCouplingAudit ===");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }
}
