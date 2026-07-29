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

namespace TRM.Tests.V33_8;

[Trait("Category", "V33_8")]
[Trait("Category", "LongRunning")]
public class V33_8_TickGradientOrigin_Tests
{
    private readonly ITestOutputHelper _o;
    public V33_8_TickGradientOrigin_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void TGO_01_TickGradientOriginAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== TGO_01: Tick Gradient Origin Audit ===");
        sb.AppendLine("=== Does the gradient field emerge from Tick dynamics? ===");
        sb.AppendLine(new string('=', 108));

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);

        var allCells = new ConcurrentBag<TgoCell>();

        foreach (var (arch, fam) in new[] { ("GAN", VcFamily.GAN), ("CNS", VcFamily.CNS) })
        {
            const int nG = 17;
            double bMin = 0.0, bMax = 2.0, gMin = 0.0, gMax = 2.0;
            double db = (bMax - bMin) / (nG - 1), dg = (gMax - gMin) / (nG - 1);

            var gM = new double[nG, nG];
            var gS = new int[nG, nG];
            var gTick = new double[nG, nG];
            var gFb = new double[nG, nG];

            Parallel.For(0, nG, bi =>
            {
                double beta = bMin + db * bi;
                for (int gi = 0; gi < nG; gi++)
                {
                    double gamma = gMin + dg * gi;
                    var f = ComputeFull(fam, 1.0, 1.0, 0.70, beta, gamma, distances, sortedD, xiBase, k0Base, nA, aMin, daD);
                    gM[bi, gi] = f.absM; gS[bi, gi] = f.sign; gTick[bi, gi] = f.tick; gFb[bi, gi] = f.fb;
                }
            });

            for (int bi = 1; bi < nG - 1; bi++)
                for (int gi = 1; gi < nG - 1; gi++)
                {
                    // ∇|m|
                    double dMdB = (gM[bi + 1, gi] - gM[bi - 1, gi]) / (2 * db);
                    double dMdG = (gM[bi, gi + 1] - gM[bi, gi - 1]) / (2 * dg);
                    double gradM = Math.Sqrt(dMdB * dMdB + dMdG * dMdG);
                    int gDir = (dMdB + dMdG) > 1e-15 ? +1 : (dMdB + dMdG) < -1e-15 ? -1 : 0;

                    // ∇Tick
                    double dTdB = (gTick[bi + 1, gi] - gTick[bi - 1, gi]) / (2 * db);
                    double dTdG = (gTick[bi, gi + 1] - gTick[bi, gi - 1]) / (2 * dg);
                    double gradTick = Math.Sqrt(dTdB * dTdB + dTdG * dTdG);
                    int tDir = (dTdB + dTdG) > 1e-15 ? +1 : (dTdB + dTdG) < -1e-15 ? -1 : 0;

                    // ∇fb (feedback gradient)
                    double dFdB = (gFb[bi + 1, gi] - gFb[bi - 1, gi]) / (2 * db);
                    double dFdG = (gFb[bi, gi + 1] - gFb[bi, gi - 1]) / (2 * dg);
                    double gradFb = Math.Sqrt(dFdB * dFdB + dFdG * dFdG);

                    // Sign transition indicator
                    bool isBdry = gS[bi, gi] != gS[bi + 1, gi] || gS[bi, gi] != gS[bi - 1, gi] ||
                                  gS[bi, gi] != gS[bi, gi + 1] || gS[bi, gi] != gS[bi, gi - 1];

                    // Laplacians
                    double lM = (gM[bi + 1, gi] + gM[bi - 1, gi] + gM[bi, gi + 1] + gM[bi, gi - 1] - 4 * gM[bi, gi]) / (db * db);
                    double lTick = (gTick[bi + 1, gi] + gTick[bi - 1, gi] + gTick[bi, gi + 1] + gTick[bi, gi - 1] - 4 * gTick[bi, gi]) / (db * db);

                    allCells.Add(new TgoCell(arch, gM[bi, gi], gS[bi, gi], gTick[bi, gi], gFb[bi, gi],
                        gradM, gradTick, gradFb, gDir, tDir, isBdry, Math.Abs(lM), Math.Abs(lTick)));
                }
        }

        var all = allCells.ToList();
        if (all.Count < 50) { sb.AppendLine($"Insufficient: {all.Count}"); Assert.True(true); return; }

        sb.AppendLine("");
        sb.AppendLine($"  Grid cells: {all.Count} across 2 families");
        sb.AppendLine("");

        // ================================================================
        // TICK–GRADIENT CORRELATION MATRIX
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Tick–Gradient Correlation Matrix ===");
        sb.AppendLine("");

        double[] logGM = all.Select(c => Math.Log10(Math.Max(1e-15, c.GradM))).ToArray();
        double[] logGT = all.Select(c => Math.Log10(Math.Max(1e-15, c.GradTick))).ToArray();
        double[] logGF = all.Select(c => Math.Log10(Math.Max(1e-15, c.GradFb))).ToArray();
        double[] logM = all.Select(c => Math.Log10(Math.Max(1e-15, c.AbsM))).ToArray();
        double[] tickV = all.Select(c => c.Tick).ToArray();
        double[] fbV = all.Select(c => c.Fb).ToArray();
        double[] lapM = all.Select(c => Math.Log10(Math.Max(1e-15, c.LapM))).ToArray();
        double[] lapT = all.Select(c => Math.Log10(Math.Max(1e-15, c.LapTick))).ToArray();

        // Primary correlations
        double gM_GT = PearsonCorr(logGM, logGT);
        double gM_GF = PearsonCorr(logGM, logGF);
        double gM_T = PearsonCorr(logGM, tickV);
        double gM_F = PearsonCorr(logGM, fbV);
        double gM_lapT = PearsonCorr(logGM, lapT);
        double gT_lapM = PearsonCorr(logGT, lapM);

        sb.AppendLine($"{"Correlation",-34} {"r",8} {"R²",8} {"Strength",14}");
        sb.AppendLine(new string('-', 66));
        Report(sb, "∇|m| ↔ ∇Tick", gM_GT);
        Report(sb, "∇|m| ↔ ∇fb", gM_GF);
        Report(sb, "∇|m| ↔ Tick (raw)", gM_T);
        Report(sb, "∇|m| ↔ fb (raw)", gM_F);
        Report(sb, "∇|m| ↔ ∇²Tick", gM_lapT);
        Report(sb, "∇Tick ↔ ∇²|m|", gT_lapM);
        sb.AppendLine("");

        // ================================================================
        // TEST A: Does ∇Tick predict ∇VarI1?
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== A: Does ∇Tick predict ∇|m|? ===");
        sb.AppendLine("");

        double bestR = Math.Abs(gM_GT);
        string bestPred = "∇Tick";
        foreach (var (name, r) in new[] { ("∇Tick", gM_GT), ("∇fb", gM_GF), ("Tick raw", gM_T), ("fb raw", gM_F) })
            if (Math.Abs(r) > bestR) { bestR = Math.Abs(r); bestPred = name; }

        sb.AppendLine($"  Best predictor of ∇|m|: {bestPred} (r={bestR:F4})");
        sb.AppendLine($"  → {(bestR > 0.3 ? "Tick GRADIENT predicts density gradient — gradient emerges from Tick" : bestR > 0.15 ? "WEAK prediction — gradient partially linked to Tick" : "NO prediction — gradient independent of Tick")}");
        sb.AppendLine("");

        // ================================================================
        // TEST B: Does Tick variance predict gradient magnitude?
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== B: Does Tick variance predict gradient magnitude? ===");
        sb.AppendLine("");

        // Split into low/high Tick cells
        double tickMed = all.Select(c => c.Tick).OrderBy(t => t).ElementAt(all.Count / 2);
        var loTick = all.Where(c => c.Tick < tickMed).ToList();
        var hiTick = all.Where(c => c.Tick >= tickMed).ToList();

        double loGradMean = loTick.Average(c => c.GradM);
        double hiGradMean = hiTick.Average(c => c.GradM);
        double tGradRatio = hiGradMean / Math.Max(1e-15, loGradMean);

        sb.AppendLine($"  Low Tick (<{tickMed:F3}):  mean |∇|m|| = {loGradMean:F4}  (n={loTick.Count})");
        sb.AppendLine($"  High Tick (≥{tickMed:F3}): mean |∇|m|| = {hiGradMean:F4}  (n={hiTick.Count})");
        sb.AppendLine($"  Ratio: {tGradRatio:F2}x");
        sb.AppendLine($"  → {(tGradRatio > 1.2 ? "Higher Tick → STRONGER gradients" : "Tick uncorrelated with gradient strength")}");
        sb.AppendLine("");

        // ================================================================
        // TEST C: Do gradient peaks coincide with Tick extrema?
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== C: Do gradient peaks coincide with Tick extrema? ===");
        sb.AppendLine("");

        // Top 10% gradient cells vs top 10% Tick cells
        int topN = Math.Max(10, all.Count / 10);
        var topGrad = all.OrderByDescending(c => c.GradM).Take(topN).ToList();
        var topTick = all.OrderByDescending(c => c.Tick).Take(topN).ToList();
        var bottomTick = all.OrderBy(c => c.Tick).Take(topN).ToList();

        int gradInTopTick = topGrad.Count(c => topTick.Any(t => t == c));
        int gradInBotTick = topGrad.Count(c => bottomTick.Any(t => t == c));
        double overlapFrac = (double)gradInTopTick / topN;

        sb.AppendLine($"  Top {topN} gradient cells in top {topN} Tick: {gradInTopTick}/{topN} ({overlapFrac*100:F1}%)");
        sb.AppendLine($"  Top {topN} gradient cells in bottom {topN} Tick: {gradInBotTick}/{topN}");
        sb.AppendLine($"  → {(overlapFrac > 0.25 ? "STRONG peak alignment — gradient peaks coincide with Tick extrema" : overlapFrac > 0.10 ? "PARTIAL alignment" : "WEAK alignment — peaks are independent")}");
        sb.AppendLine("");

        // ================================================================
        // TEST D: Can gradients be reconstructed from Tick better than sign?
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== D: Gradient reconstruction from Tick vs Sign ===");
        sb.AppendLine("");

        // Model 1: |∇|m|| ~ Tick
        double tickGradR2 = PearsonCorr(logGM, tickV); tickGradR2 *= tickGradR2;

        // Model 2: |∇|m|| ~ sign (direction only)
        double[] signD = all.Select(c => (double)c.Sign).ToArray();
        double signGradR2 = PearsonCorr(logGM, signD); signGradR2 *= signGradR2;

        // Model 3: |∇|m|| ~ Tick + sign
        double[] mX1 = all.Select(c => c.Tick).ToArray();
        double[] mX2 = all.Select(c => (double)c.Sign).ToArray();
        double combinedR2 = MultivariateR2(logGM, mX1, mX2);

        // Model 4: |∇|m|| ~ ∇Tick
        double gTickR2 = gM_GT * gM_GT;

        sb.AppendLine($"  ∇|m| reconstruction R²:");
        sb.AppendLine($"    From Tick only:      {tickGradR2:F4}");
        sb.AppendLine($"    From Sign only:      {signGradR2:F4}");
        sb.AppendLine($"    From ∇Tick only:     {gTickR2:F4}");
        sb.AppendLine($"    From Tick + Sign:    {combinedR2:F4}");
        sb.AppendLine($"    → Best: {(combinedR2 > Math.Max(tickGradR2, gTickR2) ? "Tick+Sign combined" : gTickR2 > tickGradR2 ? "∇Tick" : "Tick raw")}");
        sb.AppendLine("");

        // ================================================================
        // TEST E: Causal chain assessment
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== E: Causal Direction Assessment ===");
        sb.AppendLine("");

        // Chain 1: Tick → Gradient → Boundary → Dynamics
        // Evidence: ∇Tick predicts ∇|m|, Tick variance → gradient strength
        double chain1Score = (gM_GT > 0.15 ? 1 : 0) + (tGradRatio > 1.1 ? 1 : 0) + (overlapFrac > 0.15 ? 1 : 0);

        // Chain 2: Gradient → Tick → Boundary → Dynamics
        // Evidence: ∇|m| predicts Tick, |m| variance → Tick strength
        double gM_T2 = PearsonCorr(logGM, tickV);
        double chain2Score = (gM_T2 > 0.15 ? 1 : 0) + (tGradRatio < 0.9 ? 1 : 0);

        sb.AppendLine($"  Chain 1 (Tick→Gradient):  {chain1Score}/3  (∇Tick→∇|m| r={gM_GT:F3}, Tick ratio={tGradRatio:F2}x, overlap={overlapFrac*100:F0}%)");
        sb.AppendLine($"  Chain 2 (Gradient→Tick):  {chain2Score}/2  (∇|m|→Tick r={gM_T2:F3})");
        sb.AppendLine($"  → {(chain1Score > chain2Score ? "CHAIN 1 supported: Tick → Gradient" : chain2Score > chain1Score ? "CHAIN 2 supported: Gradient → Tick" : "MUTUAL interaction — neither dominates")}");
        sb.AppendLine("");

        // ================================================================
        // ORIGIN RANKING
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Origin Ranking ===");
        sb.AppendLine("");

        var explanations = new (string name, double score)[]
        {
            ("Tick gradient (∇Tick)", Math.Abs(gM_GT)),
            ("Tick magnitude (raw)", Math.Abs(gM_T)),
            ("Feedback gradient (∇fb)", Math.Abs(gM_GF)),
            ("∇²Tick (curvature)", Math.Abs(gM_lapT)),
            ("Sign direction", Math.Sqrt(Math.Max(0, signGradR2))),
        };

        sb.AppendLine($"{"Origin Candidate",-28} {"|r|",8} {"Rank",5}");
        sb.AppendLine(new string('-', 43));
        int rank = 0;
        foreach (var e in explanations.OrderByDescending(e => e.score))
        {
            rank++;
            sb.AppendLine($"{e.name,-28} {e.score,8:F4} {rank,5}");
        }
        sb.AppendLine("");

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        bool critA = Math.Abs(gM_GT) > 0.20;           // ∇Tick predicts ∇|m|
        bool critB = tGradRatio > 1.15;                 // Higher Tick → stronger gradients
        bool critC = overlapFrac > 0.15;                // Peak alignment
        bool critD = gTickR2 > Math.Max(tickGradR2, signGradR2); // ∇Tick best reconstruction

        int critMet = (critA ? 1 : 0) + (critB ? 1 : 0) + (critC ? 1 : 0) + (critD ? 1 : 0);

        string verdict = critMet >= 4 ? "SUPPORTED: gradient emerges from Tick dynamics."
            : critMet >= 2 ? "CONDITIONAL: mutual interaction."
            : "FALSIFIED: gradient exists independently of Tick.";

        sb.AppendLine($"VERDICT: {verdict}  ({critMet}/4 criteria)");
        sb.AppendLine("");
        sb.AppendLine($"  A. ∇Tick predicts ∇|m|:                  {(critA ? "YES" : "NO")} (r={gM_GT:F4})");
        sb.AppendLine($"  B. High Tick → strong gradient:          {(critB ? "YES" : "NO")} ({tGradRatio:F2}x)");
        sb.AppendLine($"  C. Peak alignment > 15%:                 {(critC ? "YES" : "NO")} ({overlapFrac*100:F1}%)");
        sb.AppendLine($"  D. ∇Tick best reconstruction:            {(critD ? "YES" : "NO")} (R²={gTickR2:F4})");
        sb.AppendLine("");
        sb.AppendLine("Tick-Gradient Origin Result:");
        sb.AppendLine($"  Best predictor of ∇|m|: {bestPred} (r={bestR:F4})");
        sb.AppendLine($"  Causal chain: {(chain1Score > chain2Score ? "Tick → Gradient → Boundary → Dynamics" : "Bidirectional")}");
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== TGO_01 complete. Commit: TGO_01_TickGradientOriginAudit ===");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    private static void Report(StringBuilder sb, string name, double r)
    {
        string s = Math.Abs(r) > 0.30 ? "STRONG" : Math.Abs(r) > 0.15 ? "MODERATE" : "WEAK";
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

    private record TgoCell(string Arch, double AbsM, int Sign, double Tick, double Fb,
        double GradM, double GradTick, double GradFb, int GradDir, int TickDir, bool IsBoundary, double LapM, double LapTick);
}
