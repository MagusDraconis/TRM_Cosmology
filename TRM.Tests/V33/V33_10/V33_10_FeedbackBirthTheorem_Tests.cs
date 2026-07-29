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

namespace TRM.Tests.V33_10;

[Trait("Category", "V33_10")]
[Trait("Category", "LongRunning")]
public class V33_10_FeedbackBirthTheorem_Tests
{
    private readonly ITestOutputHelper _o;
    public V33_10_FeedbackBirthTheorem_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void FBT_01_FeedbackBirthTheoremAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== FBT_01: Feedback Birth Theorem Audit ===");
        sb.AppendLine("=== What generates ∇fb? Is fb primitive or emergent? ===");
        sb.AppendLine(new string('=', 108));

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);

        var allCells = new ConcurrentBag<FbtCell>();

        foreach (var (arch, fam) in new[] { ("GAN", VcFamily.GAN), ("CNS", VcFamily.CNS) })
        {
            const int nG = 17;
            double bMin = 0.0, bMax = 2.0, gMin = 0.0, gMax = 2.0;
            double db = (bMax - bMin) / (nG - 1), dg = (gMax - gMin) / (nG - 1);

            var gM = new double[nG, nG]; var gS = new int[nG, nG];
            var gFb = new double[nG, nG]; var gTick = new double[nG, nG];
            var gV = new double[nG, nG]; // |1+m| proxy

            Parallel.For(0, nG, bi =>
            {
                double beta = bMin + db * bi;
                for (int gi = 0; gi < nG; gi++)
                {
                    double gamma = gMin + dg * gi;
                    var f = ComputeFull(fam, 1.0, 1.0, 0.70, beta, gamma, distances, sortedD, xiBase, k0Base, nA, aMin, daD);
                    gM[bi, gi] = f.absM; gS[bi, gi] = f.sign; gFb[bi, gi] = f.fb; gTick[bi, gi] = f.tick; gV[bi, gi] = f.V;
                }
            });

            for (int bi = 1; bi < nG - 1; bi++)
                for (int gi = 1; gi < nG - 1; gi++)
                {
                    // Gradients
                    double dMdB = (gM[bi + 1, gi] - gM[bi - 1, gi]) / (2 * db);
                    double dMdG = (gM[bi, gi + 1] - gM[bi, gi - 1]) / (2 * dg);
                    double gradM = Math.Sqrt(dMdB * dMdB + dMdG * dMdG);

                    double dFdB = (gFb[bi + 1, gi] - gFb[bi - 1, gi]) / (2 * db);
                    double dFdG = (gFb[bi, gi + 1] - gFb[bi, gi - 1]) / (2 * dg);
                    double gradFb = Math.Sqrt(dFdB * dFdB + dFdG * dFdG);

                    double dTdB = (gTick[bi + 1, gi] - gTick[bi - 1, gi]) / (2 * db);
                    double dTdG = (gTick[bi, gi + 1] - gTick[bi, gi - 1]) / (2 * dg);
                    double gradTick = Math.Sqrt(dTdB * dTdB + dTdG * dTdG);

                    // Oscillator divergence proxies
                    // fb itself = -(ΔVT/ΔV1) across α — this IS the oscillator divergence measure
                    // |m| = |d(VT)/d(V1)| — closely related to fb
                    // fb-|m| difference = independent information

                    double fbMDiff = Math.Abs(gFb[bi, gi] - gM[bi, gi]); // fb vs |m| divergence
                    double vDiv = Math.Abs(gV[bi, gi] - 1.0); // V = |1+m| divergence from 1

                    // Laplacians
                    double lapFb = Math.Abs(gFb[bi + 1, gi] + gFb[bi - 1, gi] + gFb[bi, gi + 1] + gFb[bi, gi - 1] - 4 * gFb[bi, gi]) / (db * db);
                    double lapM = Math.Abs(gM[bi + 1, gi] + gM[bi - 1, gi] + gM[bi, gi + 1] + gM[bi, gi - 1] - 4 * gM[bi, gi]) / (db * db);

                    bool isBdry = gS[bi, gi] != gS[bi + 1, gi] || gS[bi, gi] != gS[bi - 1, gi] ||
                                  gS[bi, gi] != gS[bi, gi + 1] || gS[bi, gi] != gS[bi, gi - 1];

                    allCells.Add(new FbtCell(arch, gM[bi, gi], gS[bi, gi], gFb[bi, gi], gTick[bi, gi], gV[bi, gi],
                        gradM, gradFb, gradTick, fbMDiff, vDiv, lapFb, lapM, isBdry));
                }
        }

        var all = allCells.ToList();
        if (all.Count < 50) { sb.AppendLine("Insufficient."); Assert.True(true); return; }

        sb.AppendLine("");
        sb.AppendLine($"  Grid cells: {all.Count}");
        sb.AppendLine("");

        // ================================================================
        // FEEDBACK ORIGIN MATRIX
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Feedback Origin Matrix ===");
        sb.AppendLine("");

        double[] lGF = all.Select(c => Math.Log10(Math.Max(1e-15, c.GradFb))).ToArray();
        double[] lGM = all.Select(c => Math.Log10(Math.Max(1e-15, c.GradM))).ToArray();
        double[] lGT = all.Select(c => Math.Log10(Math.Max(1e-15, c.GradTick))).ToArray();
        double[] fbD = all.Select(c => c.FbMDiff).ToArray();
        double[] vDv = all.Select(c => c.VDiv).ToArray();
        double[] fbV = all.Select(c => c.Fb).ToArray();
        double[] mV = all.Select(c => c.AbsM).ToArray();
        double[] lLF = all.Select(c => Math.Log10(Math.Max(1e-15, c.LapFb))).ToArray();
        double[] lLM = all.Select(c => Math.Log10(Math.Max(1e-15, c.LapM))).ToArray();

        sb.AppendLine($"{"Correlation",-34} {"r",8} {"R²",8} {"Strength",14}");
        sb.AppendLine(new string('-', 66));
        Report(sb, "∇fb ↔ ∇|m|", PearsonCorr(lGF, lGM));
        Report(sb, "∇fb ↔ ∇Tick", PearsonCorr(lGF, lGT));
        Report(sb, "∇fb ↔ |fb-|m|| (divergence)", PearsonCorr(lGF, fbD));
        Report(sb, "∇fb ↔ |V-1| (osc. mismatch)", PearsonCorr(lGF, vDv));
        Report(sb, "∇fb ↔ fb (raw)", PearsonCorr(lGF, fbV));
        Report(sb, "∇fb ↔ |m| (raw)", PearsonCorr(lGF, mV));
        Report(sb, "∇²fb ↔ ∇²|m|", PearsonCorr(lLF, lLM));
        Report(sb, "fb ↔ |m| (raw fields)", PearsonCorr(fbV, mV));
        sb.AppendLine("");

        // ================================================================
        // OSCILLATOR DIVERGENCE ANALYSIS
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Oscillator Divergence Analysis ===");
        sb.AppendLine("");

        // Does fb ≈ |m|? (If r > 0.9, fb is just |m| measured differently)
        double fb_mR = PearsonCorr(fbV, mV);
        sb.AppendLine($"  fb vs |m| raw correlation: r = {fb_mR:F4}  R² = {fb_mR*fb_mR:F4}");
        sb.AppendLine($"  → {(fb_mR > 0.90 ? "fb IS |m| — feedback is redundant with density" : fb_mR > 0.60 ? "fb PARTIALLY INDEPENDENT of |m|" : "fb is DISTINCT from |m|")}");
        sb.AppendLine("");

        // Where does ∇fb peak? High |fb-|m|| or high |m|?
        double divMed = all.Select(c => c.FbMDiff).OrderBy(d => d).ElementAt(all.Count / 2);
        var hiDiv = all.Where(c => c.FbMDiff >= divMed).ToList();
        var loDiv = all.Where(c => c.FbMDiff < divMed).ToList();
        double hiGradFb = hiDiv.Average(c => c.GradFb);
        double loGradFb = loDiv.Average(c => c.GradFb);

        sb.AppendLine($"  High |fb-|m|| (divergent): mean ∇fb = {hiGradFb:F4}");
        sb.AppendLine($"  Low  |fb-|m|| (aligned):   mean ∇fb = {loGradFb:F4}");
        sb.AppendLine($"  Ratio: {hiGradFb/Math.Max(1e-15,loGradFb):F2}x");
        sb.AppendLine($"  → {(hiGradFb/loGradFb > 1.3 ? "∇fb peaks WHERE fb and |m| DIVERGE — oscillator mismatch drives feedback" : "∇fb independent of fb-|m| divergence")}");
        sb.AppendLine("");

        // ================================================================
        // RECONSTRUCTION: Can ∇fb be predicted without fb?
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Can ∇fb be reconstructed without fb? ===");
        sb.AppendLine("");

        // If ∇fb ≈ f(∇|m|, ∇Tick, |fb-|m||) then fb is not primitive — it emerges
        double r2_gm = PearsonCorr(lGF, lGM); r2_gm *= r2_gm;
        double r2_gt = PearsonCorr(lGF, lGT); r2_gt *= r2_gt;
        double r2_div = PearsonCorr(lGF, fbD); r2_div *= r2_div;
        double combinedR2 = MultivariateR2(lGF, lGM, fbD);
        double fullR2 = MultivariateR2(lGF, lGM, lGT);

        sb.AppendLine($"  ∇fb reconstruction R²:");
        sb.AppendLine($"    From ∇|m| only:           {r2_gm:F4}");
        sb.AppendLine($"    From ∇Tick only:          {r2_gt:F4}");
        sb.AppendLine($"    From |fb-|m|| only:      {r2_div:F4}");
        sb.AppendLine($"    From ∇|m| + |fb-|m||:   {combinedR2:F4}");
        sb.AppendLine($"    From ∇|m| + ∇Tick:       {fullR2:F4}");
        sb.AppendLine($"    → ∇fb is {(combinedR2 > 0.70 ? "RECONSTRUCTIBLE — fb emerges from |m| structure" : combinedR2 > 0.30 ? "PARTIALLY reconstructible" : "IRREDUCIBLE — fb is primitive")}");
        sb.AppendLine("");

        // ================================================================
        // CAUSAL CHAIN COMPARISON
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Causal Chain Comparison ===");
        sb.AppendLine("");

        // Chain A: Oscillator mismatch → ∇fb → ∇|m| → Boundary → Dynamics
        double chainA_score = (hiGradFb/loGradFb > 1.3 ? 2 : 0) + (combinedR2 > 0.50 ? 2 : combinedR2 > 0.30 ? 1 : 0);

        // Chain B: Independent fb field → ∇fb → ∇|m|
        double chainB_score = (fb_mR < 0.80 ? 2 : fb_mR < 0.95 ? 1 : 0) + (combinedR2 < 0.50 ? 2 : combinedR2 < 0.70 ? 1 : 0);

        sb.AppendLine($"  Chain A (fb emergent):       score = {chainA_score}/4");
        sb.AppendLine($"    Oscillator mismatch → ∇fb → ∇|m| → Boundary");
        sb.AppendLine($"  Chain B (fb primitive):      score = {chainB_score}/4");
        sb.AppendLine($"    Independent fb → ∇fb → ∇|m|");
        sb.AppendLine($"  → {(chainA_score > chainB_score ? "CHAIN A: fb EMERGES from oscillator interactions" : chainB_score > chainA_score ? "CHAIN B: fb is PRIMITIVE" : "MIXED — partial emergence")}");
        sb.AppendLine("");

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        bool critA = hiGradFb / Math.Max(1e-15, loGradFb) > 1.2;
        bool critB = combinedR2 > 0.30;
        bool critC = chainA_score > chainB_score;
        bool critD = fb_mR < 0.95;

        int critMet = (critA ? 1 : 0) + (critB ? 1 : 0) + (critC ? 1 : 0) + (critD ? 1 : 0);

        string verdict = critMet >= 4 ? "SUPPORTED: feedback emerges from oscillator interactions."
            : critMet >= 2 ? "CONDITIONAL: mixed origin."
            : "FALSIFIED: feedback remains primitive.";

        sb.AppendLine($"VERDICT: {verdict}  ({critMet}/4 criteria)");
        sb.AppendLine("");
        sb.AppendLine($"  A. ∇fb peaks at fb-|m| divergence:      {(critA ? "YES" : "NO")} ({hiGradFb/loGradFb:F2}x)");
        sb.AppendLine($"  B. ∇fb reconstructible (R²>0.30):       {(critB ? "YES" : "NO")} (R²={combinedR2:F4})");
        sb.AppendLine($"  C. Chain A > Chain B:                   {(critC ? "YES" : "NO")} ({chainA_score} vs {chainB_score})");
        sb.AppendLine($"  D. fb ≠ |m| (r<0.95):                    {(critD ? "YES" : "NO")} (r={fb_mR:F4})");
        sb.AppendLine("");
        sb.AppendLine("Feedback Birth Result:");
        sb.AppendLine($"  fb vs |m| raw: r = {fb_mR:F4}");
        sb.AppendLine($"  ∇fb reconstructible from ∇|m| + |fb-|m||: R² = {combinedR2:F4}");
        sb.AppendLine("");
        if (chainA_score > chainB_score)
        {
            sb.AppendLine("  FEEDBACK IS EMERGENT:");
            sb.AppendLine("  fb arises from the same VarTerms-VarI1 oscillator relationship");
            sb.AppendLine("  that produces |m|. fb is a DIFFERENT PROJECTION of the same");
            sb.AppendLine("  underlying oscillator mismatch, not an independent primitive.");
            sb.AppendLine("");
            sb.AppendLine("  Deepest causal chain:");
            sb.AppendLine("    Oscillator interaction → {fb, |m|} → ∇fb, ∇|m| → Boundary → Gravity");
        }
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== FBT_01 complete. Commit: FBT_01_FeedbackBirthTheoremAudit ===");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    private static void Report(StringBuilder sb, string name, double r)
    {
        string s = Math.Abs(r) > 0.40 ? "STRONG" : Math.Abs(r) > 0.20 ? "MODERATE" : "WEAK";
        sb.AppendLine($"{name,-34} {r,8:F4} {r*r,8:F4} {s,14}");
    }

    private static double PearsonCorr(double[] xs, double[] ys)
    {
        int n = xs.Length; if (n < 2) return 0;
        double mx = xs.Average(), my = ys.Average();
        double sxy = 0, sxx = 0, syy = 0;
        for (int i = 0; i < n; i++) { double dx = xs[i] - mx, dy = ys[i] - my; sxy += dx * dy; sxx += dx * dx; syy += dy * dy; }
        return sxx > 1e-15 && syy > 1e-15 ? sxy / Math.Sqrt(sxx * syy) : 0;
    }

    private static double MultivariateR2(double[] y, double[] x1, double[] x2)
    {
        int n = y.Length; if (n < 3) return 0;
        double sy = y.Sum(), s1 = x1.Sum(), s2 = x2.Sum();
        double s11 = 0, s22 = 0, s12 = 0, s1y = 0, s2y = 0;
        for (int i = 0; i < n; i++) { s11 += x1[i] * x1[i]; s22 += x2[i] * x2[i]; s12 += x1[i] * x2[i]; s1y += x1[i] * y[i]; s2y += x2[i] * y[i]; }
        double s1yc = s1y - s1 * sy / n, s2yc = s2y - s2 * sy / n;
        double s11c = s11 - s1 * s1 / n, s22c = s22 - s2 * s2 / n, s12c = s12 - s1 * s2 / n;
        double det = s11c * s22c - s12c * s12c;
        double b1 = 0, b2 = 0;
        if (Math.Abs(det) > 1e-15) { b1 = (s1yc * s22c - s2yc * s12c) / det; b2 = (s2yc * s11c - s1yc * s12c) / det; }
        double b0 = sy / n - b1 * s1 / n - b2 * s2 / n;
        double ssRes = 0, ssTot = 0; double my = sy / n;
        for (int i = 0; i < n; i++) { double pred = b0 + b1 * x1[i] + b2 * x2[i]; ssRes += (y[i] - pred) * (y[i] - pred); ssTot += (y[i] - my) * (y[i] - my); }
        double r2 = ssTot > 1e-15 ? 1 - ssRes / ssTot : 0;
        return Math.Max(0, Math.Min(1, r2));
    }

    private record FbtCell(string Arch, double AbsM, int Sign, double Fb, double Tick, double V,
        double GradM, double GradFb, double GradTick, double FbMDiff, double VDiv, double LapFb, double LapM, bool IsBoundary);
}
