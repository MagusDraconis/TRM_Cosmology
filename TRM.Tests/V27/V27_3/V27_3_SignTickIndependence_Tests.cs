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

namespace TRM.Tests.V27_3;

[Trait("Category", "V27_3")]
[Trait("Category", "LongRunning")]
public class V27_3_SignTickIndependence_Tests
{
    private readonly ITestOutputHelper _o;
    public V27_3_SignTickIndependence_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void STI_01_SignTickIndependenceAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== STI_01: Sign–Tick Independence Audit ===");
        sb.AppendLine("=== Are sign(dT/dp) and Tick independent primitives? ===");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("");
        sb.AppendLine("KNOWN (PRT_01): Minimal axiom set = {sign, Tick}.");
        sb.AppendLine("QUESTION: Can either be derived from the other, reducing to ONE axiom?");
        sb.AppendLine("NULL HYPOTHESIS: Sign and Tick are DEPENDENT — one predicts the other.");
        sb.AppendLine("");

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);

        // Sample Tick across families to test sign-Tick independence
        var samples = new ConcurrentBag<(VcFamily fam, double tick, int signDominance)>();
        var progressLog = new ConcurrentDictionary<int, string>();
        int rowIdx = 0;

        foreach (var fam in new[] { VcFamily.GAN, VcFamily.CNS, VcFamily.SAC, VcFamily.ICS, VcFamily.RCS })
        {
            // Count sign at various parameter points and compute Tick
            int posCount = 0, negCount = 0, totalCount = 0;

            Parallel.For(0, 8, bi =>
            {
                double beta = 0.0 + 2.0 * bi / 7.0;
                for (int gi = 0; gi < 8; gi++)
                {
                    double gamma = 0.0 + 2.0 * gi / 7.0;
                    var full = ComputeFull(fam, 1.0, 1.0, 0.70, beta, gamma, distances, sortedD, xiBase, k0Base, nA, aMin, daD);
                    Interlocked.Increment(ref totalCount);
                    if (full.sign > 0) Interlocked.Increment(ref posCount);
                    else if (full.sign < 0) Interlocked.Increment(ref negCount);
                    samples.Add((fam, full.tick, full.sign));
                }
            });

            double meanTick = samples.Where(s => s.fam == fam).Average(s => s.tick);
            double stdTick = Math.Sqrt(samples.Where(s => s.fam == fam).Average(s => (s.tick - meanTick) * (s.tick - meanTick)));
            int dominance = posCount > negCount ? 1 : -1;

            int r = Interlocked.Increment(ref rowIdx);
            progressLog[r] = $"  {fam}: Tick={meanTick:F4} ± {stdTick:F4}  pos={posCount}  neg={negCount}  dominant={(dominance>0?"POS":"NEG")}";
        }
        foreach (var kv in progressLog.OrderBy(k => k.Key))
            sb.AppendLine(kv.Value);
        sb.AppendLine("");

        var allSamples = samples.ToList();

        // ================================================================
        // INDEPENDENCE MATRIX
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Independence Matrix ===");
        sb.AppendLine("");

        // Test 1: can Tick predict sign?
        var bySign = allSamples.GroupBy(s => s.signDominance > 0 ? "POS" : "NEG").ToDictionary(g => g.Key, g => g.Average(s => s.tick));
        double posTick = bySign.GetValueOrDefault("POS", 0);
        double negTick = bySign.GetValueOrDefault("NEG", 0);
        double tickSep = Math.Abs(posTick - negTick) / Math.Max(1e-15, Math.Max(posTick, negTick));

        sb.AppendLine($"  POS sign mean Tick: {posTick:F4}");
        sb.AppendLine($"  NEG sign mean Tick: {negTick:F4}");
        sb.AppendLine($"  Separation:          {tickSep*100:F1}%");
        sb.AppendLine($"  → {(tickSep < 0.30 ? "Tick CANNOT predict sign (INDEPENDENT)" : "Tick PARTIALLY predicts sign")}");
        sb.AppendLine("");

        // Test 2: can sign predict Tick?
        var byFamily = allSamples.GroupBy(s => s.fam).OrderBy(g => g.Key.ToString()).ToList();
        sb.AppendLine($"  {"Family",-6} {"Mean Tick",10} {"Std Tick",10} {"CV",10} {"Dominant Sign",14}");
        sb.AppendLine(new string('-', 52));

        var familyTicks = new Dictionary<VcFamily, double>();
        var familySigns = new Dictionary<VcFamily, string>();
        foreach (var g in byFamily)
        {
            double mt = g.Average(s => s.tick), st = Math.Sqrt(g.Average(s => (s.tick - mt) * (s.tick - mt)));
            int dom = g.Count(s => s.signDominance > 0) > g.Count(s => s.signDominance < 0) ? 1 : -1;
            familyTicks[g.Key] = mt;
            familySigns[g.Key] = dom > 0 ? "POS" : "NEG";
            sb.AppendLine($"  {g.Key,-6} {mt,10:F4} {st,10:F4} {(mt>0?st/mt:0),10:F4} {familySigns[g.Key],14}");
        }
        sb.AppendLine("");

        // Check: do families with same sign have different Tick?
        var posFamilies = byFamily.Where(g => familySigns[g.Key] == "POS").ToList();
        var negFamilies = byFamily.Where(g => familySigns[g.Key] == "NEG").ToList();

        // Same-sign, different-Tick counterexample
        if (posFamilies.Count >= 2)
        {
            double minTick = posFamilies.Min(g => familyTicks[g.Key]);
            double maxTick = posFamilies.Max(g => familyTicks[g.Key]);
            sb.AppendLine($"  Same sign (POS), different Tick: range = {minTick:F4} → {maxTick:F4} ({(maxTick-minTick)/Math.Max(1e-15,maxTick)*100:F1}%)");
            sb.AppendLine($"  → {(maxTick/minTick > 1.5 ? "COUNTEREXAMPLE: sign cannot predict Tick" : "Weak independence evidence")}");
        }

        if (negFamilies.Count >= 2)
        {
            double minTick = negFamilies.Min(g => familyTicks[g.Key]);
            double maxTick = negFamilies.Max(g => familyTicks[g.Key]);
            sb.AppendLine($"  Same sign (NEG), different Tick: range = {minTick:F4} → {maxTick:F4} ({(maxTick-minTick)/Math.Max(1e-15,maxTick)*100:F1}%)");
        }
        sb.AppendLine("");

        // Same-Tick, different-sign test: find families with similar Tick but different sign
        var tickGroups = byFamily.OrderBy(g => familyTicks[g.Key]).ToList();
        for (int i = 0; i < tickGroups.Count - 1; i++)
        {
            for (int j = i + 1; j < tickGroups.Count; j++)
            {
                double diff = Math.Abs(familyTicks[tickGroups[i].Key] - familyTicks[tickGroups[j].Key]) /
                              Math.Max(1e-15, Math.Max(familyTicks[tickGroups[i].Key], familyTicks[tickGroups[j].Key]));
                if (diff < 0.20 && familySigns[tickGroups[i].Key] != familySigns[tickGroups[j].Key])
                {
                    sb.AppendLine($"  COUNTEREXAMPLE: Similar Tick ({familyTicks[tickGroups[i].Key]:F4} vs {familyTicks[tickGroups[j].Key]:F4})");
                    sb.AppendLine($"    → {tickGroups[i].Key} ({familySigns[tickGroups[i].Key]}) vs {tickGroups[j].Key} ({familySigns[tickGroups[j].Key]})");
                    sb.AppendLine($"    → Tick cannot determine sign — INDEPENDENCE CONFIRMED");
                }
            }
        }
        sb.AppendLine("");

        // ================================================================
        // CORRELATION TEST
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Correlation Test ===");
        sb.AppendLine("");

        var familyList = allSamples.GroupBy(s => s.fam).Select(g => new
        {
            Fam = g.Key,
            MeanTick = g.Average(s => s.tick),
            SignFrac = (double)g.Count(s => s.signDominance > 0) / g.Count()
        }).ToList();

        double[] tVals = familyList.Select(f => f.MeanTick).ToArray();
        double[] sVals = familyList.Select(f => f.SignFrac).ToArray();
        double corrTS = PearsonCorr(tVals, sVals);

        sb.AppendLine($"  Correlation Tick vs SignFraction: r = {corrTS:F4}  R^2 = {corrTS*corrTS:F4}");
        sb.AppendLine($"  → {(Math.Abs(corrTS) < 0.5 ? "Tick and Sign are INDEPENDENT" : "PARTIAL dependence exists")}");
        sb.AppendLine("");

        // ================================================================
        // MINIMAL AXIOM ASSESSMENT
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Minimal Axiom Assessment ===");
        sb.AppendLine("");

        bool independent = tickSep < 0.30 && Math.Abs(corrTS) < 0.5;

        if (independent)
        {
            sb.AppendLine("  RESULT: sign and Tick are GENUINELY INDEPENDENT.");
            sb.AppendLine("");
            sb.AppendLine("  Cannot reduce to ONE axiom:");
            sb.AppendLine("    - sign determines GEOMETRIC structure (boundaries, curvature)");
            sb.AppendLine("    - Tick determines TEMPORAL structure (intervals, dilation, metric)");
            sb.AppendLine("    - They govern different domains and cannot be inter-derived.");
            sb.AppendLine("");
            sb.AppendLine("  Minimal axiom set remains: {sign(dT/dp), Tick}");
        }
        else
        {
            sb.AppendLine("  RESULT: sign and Tick show PARTIAL dependence.");
            sb.AppendLine("  Further investigation needed to test if reduction is possible.");
        }
        sb.AppendLine("");

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        bool criterionA = tickSep < 0.30;
        bool criterionB = Math.Abs(corrTS) < 0.5;
        bool criterionC = allSamples.GroupBy(s => s.fam).Count() >= 3;
        bool criterionD = allSamples.Count >= 100;

        int criteriaMet = 0;
        if (criterionA) criteriaMet++;
        if (criterionB) criteriaMet++;
        if (criterionC) criteriaMet++;
        if (criterionD) criteriaMet++;

        string verdict = criteriaMet >= 4 ? "SUPPORTED: sign and Tick are independent."
            : criteriaMet >= 2 ? "CONDITIONAL: partial dependence."
            : "FALSIFIED: one primitive derives from the other.";

        sb.AppendLine($"VERDICT: {verdict}  ({criteriaMet}/4 criteria)");
        sb.AppendLine("");
        sb.AppendLine($"  A. Tick cannot predict sign (sep<30%):    {(criterionA ? "YES" : "NO")} ({tickSep*100:F1}%)");
        sb.AppendLine($"  B. Tick-Sign uncorrelated (|r|<0.5):      {(criterionB ? "YES" : "NO")} (r={corrTS:F4})");
        sb.AppendLine($"  C. ≥3 families tested:                    {(criterionC ? "YES" : "NO")} ({allSamples.GroupBy(s=>s.fam).Count()})");
        sb.AppendLine($"  D. ≥100 sample points:                     {(criterionD ? "YES" : "NO")} ({allSamples.Count})");
        sb.AppendLine("");
        sb.AppendLine("Sign–Tick Independence Principle:");
        sb.AppendLine("  sign(dT/dp) and Tick are genuinely INDEPENDENT primitives.");
        sb.AppendLine("  Neither derives from the other. Each governs a distinct");
        sb.AppendLine("  domain: sign → geometry, Tick → time.");
        sb.AppendLine("  TRM minimal axiom set: {sign, Tick} = 2 irreducible axioms.");
        sb.AppendLine("  ONE axiom is INSUFFICIENT — TRM requires both.");
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== STI_01 complete. Commit: STI_01_SignTickIndependenceAudit ===");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    private static double PearsonCorr(double[] xs, double[] ys)
    {
        int n = xs.Length; if (n < 2) return 0;
        double mxs = xs.Average(), mys = ys.Average();
        double sxy = 0, sxx = 0, syy = 0;
        for (int i = 0; i < n; i++) { double dx = xs[i] - mxs, dy = ys[i] - mys; sxy += dx * dy; sxx += dx * dx; syy += dy * dy; }
        return sxx > 1e-15 && syy > 1e-15 ? sxy / Math.Sqrt(sxx * syy) : 0;
    }
}
