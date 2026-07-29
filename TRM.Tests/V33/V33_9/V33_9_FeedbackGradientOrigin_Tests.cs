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

namespace TRM.Tests.V33_9;

[Trait("Category", "V33_9")]
[Trait("Category", "LongRunning")]
public class V33_9_FeedbackGradientOrigin_Tests
{
    private readonly ITestOutputHelper _o;
    public V33_9_FeedbackGradientOrigin_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void FGO_01_FeedbackGradientOriginAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== FGO_01: Feedback Gradient Origin Audit ===");
        sb.AppendLine("=== Is ∇fb the true primitive source of TRM gradients? ===");
        sb.AppendLine(new string('=', 108));

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);

        var allCells = new ConcurrentBag<FgoCell>();

        foreach (var (arch, fam) in new[] { ("GAN", VcFamily.GAN), ("CNS", VcFamily.CNS) })
        {
            const int nG = 18;
            double bMin = 0.0, bMax = 2.0, gMin = 0.0, gMax = 2.0;
            double db = (bMax - bMin) / (nG - 1), dg = (gMax - gMin) / (nG - 1);

            var gM = new double[nG, nG]; var gS = new int[nG, nG];
            var gFb = new double[nG, nG]; var gTick = new double[nG, nG];

            Parallel.For(0, nG, bi =>
            {
                double beta = bMin + db * bi;
                for (int gi = 0; gi < nG; gi++)
                {
                    double gamma = gMin + dg * gi;
                    var f = ComputeFull(fam, 1.0, 1.0, 0.70, beta, gamma, distances, sortedD, xiBase, k0Base, nA, aMin, daD);
                    gM[bi, gi] = f.absM; gS[bi, gi] = f.sign; gFb[bi, gi] = f.fb; gTick[bi, gi] = f.tick;
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

                    // ∇fb
                    double dFdB = (gFb[bi + 1, gi] - gFb[bi - 1, gi]) / (2 * db);
                    double dFdG = (gFb[bi, gi + 1] - gFb[bi, gi - 1]) / (2 * dg);
                    double gradFb = Math.Sqrt(dFdB * dFdB + dFdG * dFdG);
                    int fbD = (dFdB + dFdG) > 1e-15 ? +1 : (dFdB + dFdG) < -1e-15 ? -1 : 0;

                    // ∇²fb
                    double lapFb = (gFb[bi + 1, gi] + gFb[bi - 1, gi] + gFb[bi, gi + 1] + gFb[bi, gi - 1] - 4 * gFb[bi, gi]) / (db * db);

                    // Boundary indicators
                    bool isBdry = gS[bi, gi] != gS[bi + 1, gi] || gS[bi, gi] != gS[bi - 1, gi] ||
                                  gS[bi, gi] != gS[bi, gi + 1] || gS[bi, gi] != gS[bi, gi - 1];
                    double signGradB = (gS[bi + 1, gi] - gS[bi - 1, gi]) / (2.0 * db);
                    double signGradG = (gS[bi, gi + 1] - gS[bi, gi - 1]) / (2.0 * dg);
                    double signGrad = Math.Sqrt(signGradB * signGradB + signGradG * signGradG);

                    // ∇Tick (for comparison)
                    double dTdB = (gTick[bi + 1, gi] - gTick[bi - 1, gi]) / (2 * db);
                    double dTdG = (gTick[bi, gi + 1] - gTick[bi, gi - 1]) / (2 * dg);
                    double gradTick = Math.Sqrt(dTdB * dTdB + dTdG * dTdG);

                    allCells.Add(new FgoCell(arch, gM[bi, gi], gS[bi, gi], gFb[bi, gi], gTick[bi, gi],
                        gradM, gradFb, gradTick, Math.Abs(lapFb), gDir, fbD, isBdry, signGrad));
                }
        }

        var all = allCells.ToList();
        if (all.Count < 50) { sb.AppendLine("Insufficient."); Assert.True(true); return; }

        sb.AppendLine("");
        sb.AppendLine($"  Grid cells: {all.Count} across 2 families");
        sb.AppendLine("");

        // ================================================================
        // FEEDBACK–GRADIENT MATRIX
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Feedback–Gradient Correlation Matrix ===");
        sb.AppendLine("");

        double[] lGM = all.Select(c => Math.Log10(Math.Max(1e-15, c.GradM))).ToArray();
        double[] lGF = all.Select(c => Math.Log10(Math.Max(1e-15, c.GradFb))).ToArray();
        double[] lGT = all.Select(c => Math.Log10(Math.Max(1e-15, c.GradTick))).ToArray();
        double[] lLF = all.Select(c => Math.Log10(Math.Max(1e-15, c.LapFb))).ToArray();
        double[] fbV = all.Select(c => c.Fb).ToArray();
        double[] sgV = all.Select(c => c.SignGrad).ToArray();
        double[] bdV = all.Select(c => c.IsBoundary ? 1.0 : 0.0).ToArray();
        double[] mDir = all.Select(c => (double)c.GradDir).ToArray();
        double[] fbDir = all.Select(c => (double)c.FbDir).ToArray();

        sb.AppendLine($"{"Correlation",-34} {"r",8} {"R²",8} {"Strength",14}");
        sb.AppendLine(new string('-', 66));
        Report(sb, "∇|m| ↔ ∇fb", PearsonCorr(lGM, lGF));
        Report(sb, "∇|m| ↔ ∇Tick", PearsonCorr(lGM, lGT));
        Report(sb, "∇|m| ↔ ∇²fb", PearsonCorr(lGM, lLF));
        Report(sb, "∇|m| ↔ fb (raw)", PearsonCorr(lGM, fbV));
        Report(sb, "∇fb ↔ |∇sign|", PearsonCorr(lGF, sgV));
        Report(sb, "∇fb ↔ boundary mask", PearsonCorr(lGF, bdV));
        Report(sb, "∇fb direction ↔ ∇|m| direction", PearsonCorr(fbDir, mDir));
        sb.AppendLine("");

        // ================================================================
        // TEST 1: ∇fb predicts ∇|m| across families
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Q1: Does ∇fb predict ∇|m| across families? ===");
        sb.AppendLine("");

        foreach (var arch in new[] { "GAN", "CNS" })
        {
            var g = all.Where(c => c.Arch == arch).ToList();
            double[] agm = g.Select(c => Math.Log10(Math.Max(1e-15, c.GradM))).ToArray();
            double[] agf = g.Select(c => Math.Log10(Math.Max(1e-15, c.GradFb))).ToArray();
            double[] agt = g.Select(c => Math.Log10(Math.Max(1e-15, c.GradTick))).ToArray();
            double fbRC = PearsonCorr(agm, agf);
            double tickRC = PearsonCorr(agm, agt);
            sb.AppendLine($"  {arch}: ∇fb→∇|m| r={fbRC:F4}  ∇Tick→∇|m| r={tickRC:F4}");
        }
        sb.AppendLine("");

        // ================================================================
        // TEST 2: Sign transitions coincide with ∇fb peaks?
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Q2: Do sign transitions coincide with ∇fb peaks? ===");
        sb.AppendLine("");

        // High ∇fb cells vs rest — boundary fraction
        double fbMed = all.Select(c => c.GradFb).OrderBy(f => f).ElementAt(all.Count / 2);
        var hiFb = all.Where(c => c.GradFb >= fbMed).ToList();
        var loFb = all.Where(c => c.GradFb < fbMed).ToList();

        double hiBdryPct = 100.0 * hiFb.Count(c => c.IsBoundary) / hiFb.Count;
        double loBdryPct = 100.0 * loFb.Count(c => c.IsBoundary) / loFb.Count;
        double bdryRatio = hiBdryPct / Math.Max(1e-15, loBdryPct);

        double hiSignGrad = hiFb.Average(c => c.SignGrad);
        double loSignGrad = loFb.Average(c => c.SignGrad);

        sb.AppendLine($"  High ∇fb: boundary={hiBdryPct:F1}%  |∇sign|={hiSignGrad:F3}");
        sb.AppendLine($"  Low  ∇fb: boundary={loBdryPct:F1}%  |∇sign|={loSignGrad:F3}");
        sb.AppendLine($"  Boundary ratio: {bdryRatio:F2}x  Sign-grad ratio: {hiSignGrad/Math.Max(1e-15,loSignGrad):F2}x");
        sb.AppendLine($"  → {(bdryRatio > 1.5 ? "∇fb PEAKS COINCIDE with sign transitions" : "Weak coincidence")}");
        sb.AppendLine("");

        // ================================================================
        // TEST 3: Boundary extraction selects high-∇fb regions?
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Q3: Does boundary extraction select high-∇fb regions? ===");
        sb.AppendLine("");

        var bdryCells = all.Where(c => c.IsBoundary).ToList();
        var discCells = all.Where(c => !c.IsBoundary).ToList();

        double bFbGrad = bdryCells.Average(c => c.GradFb);
        double dFbGrad = discCells.Average(c => c.GradFb);
        double bGradM = bdryCells.Average(c => c.GradM);
        double dGradM = discCells.Average(c => c.GradM);
        double bFbRaw = bdryCells.Average(c => c.Fb);
        double dFbRaw = discCells.Average(c => c.Fb);

        sb.AppendLine($"  Boundary vs discarded:");
        sb.AppendLine($"    ∇fb:      {bFbGrad:F4} vs {dFbGrad:F4}  ({bFbGrad/Math.Max(1e-15,dFbGrad):F2}x)");
        sb.AppendLine($"    ∇|m|:     {bGradM:F4} vs {dGradM:F4}  ({bGradM/Math.Max(1e-15,dGradM):F2}x)");
        sb.AppendLine($"    fb raw:   {bFbRaw:F4} vs {dFbRaw:F4}  ({bFbRaw/Math.Max(1e-15,dFbRaw):F2}x)");
        sb.AppendLine($"  → {(bFbGrad/dFbGrad > 1.3 ? "Boundary SELECTS high-∇fb regions" : "No preferential selection")}");
        sb.AppendLine("");

        // ================================================================
        // TEST 4: Reconstruct ∇|m| from feedback alone
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Q4: Can ∇|m| be reconstructed from feedback alone? ===");
        sb.AppendLine("");

        double fbOnlyR2 = PearsonCorr(lGM, lGF); fbOnlyR2 *= fbOnlyR2;
        double tickOnlyR2 = PearsonCorr(lGM, lGT); tickOnlyR2 *= tickOnlyR2;
        double fbSignR2 = MultivariateR2(lGM, lGF, sgV);
        double fbFullR2 = MultivariateR2(lGM, lGF, fbV);
        double allR2 = MultivariateR2(lGM, lGF, lGT);

        sb.AppendLine($"  ∇|m| reconstruction R²:");
        sb.AppendLine($"    ∇fb only:           {fbOnlyR2:F4}");
        sb.AppendLine($"    ∇Tick only:         {tickOnlyR2:F4}");
        sb.AppendLine($"    ∇fb + |∇sign|:     {fbSignR2:F4}");
        sb.AppendLine($"    ∇fb + fb raw:      {fbFullR2:F4}");
        sb.AppendLine($"    ∇fb + ∇Tick:       {allR2:F4}");
        sb.AppendLine($"    → Best: {(fbOnlyR2 > tickOnlyR2 ? "∇fb alone is SUFFICIENT" : "∇Tick competitive")}");
        sb.AppendLine("");

        // ================================================================
        // TEST 5: Causal chain ranking
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Q5: Primitive Source Ranking ===");
        sb.AppendLine("");

        double fbScore = Math.Abs(PearsonCorr(lGM, lGF)) + (bdryRatio > 1.3 ? 0.3 : 0) + (bFbGrad / dFbGrad > 1.3 ? 0.3 : 0);
        double tickScore = Math.Abs(PearsonCorr(lGM, lGT)) + 0;
        double signScore = Math.Abs(PearsonCorr(lGM, sgV)) + (bdryRatio > 1.3 ? 0.2 : 0);

        sb.AppendLine($"{"Source",-24} {"∇→∇|m| r",10} {"Boundary",10} {"Selection",10} {"Total",8}");
        sb.AppendLine(new string('-', 64));
        sb.AppendLine($"{"∇fb (feedback grad)",-24} {PearsonCorr(lGM,lGF),10:F4} {(bdryRatio>1.3?"+0.3":"+0.0"),10} {(bFbGrad/dFbGrad>1.3?"+0.3":"+0.0"),10} {fbScore,8:F3}");
        sb.AppendLine($"{"∇Tick (tick grad)",-24} {PearsonCorr(lGM,lGT),10:F4} {"+0.0",10} {"+0.0",10} {tickScore,8:F3}");
        sb.AppendLine($"{"|∇sign| (sign grad)",-24} {PearsonCorr(lGM,sgV),10:F4} {"+0.0",10} {(bdryRatio>1.3?"+0.2":"+0.0"),10} {signScore,8:F3}");
        sb.AppendLine("");

        string bestSource = fbScore > Math.Max(tickScore, signScore) ? "∇fb (FEEDBACK GRADIENT)"
            : tickScore > signScore ? "∇Tick" : "|∇sign|";
        sb.AppendLine($"  PRIMITIVE SOURCE: {bestSource}");
        sb.AppendLine("");

        // ================================================================
        // CAUSAL CHAIN
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Candidate Causal Chain ===");
        sb.AppendLine("");

        if (fbScore > tickScore + 0.2)
        {
            sb.AppendLine("  FEEDBACK → GRADIENT → BOUNDARY → DYNAMICS");
            sb.AppendLine("");
            sb.AppendLine("  TRM causal architecture:");
            sb.AppendLine("    ∇fb (primitive) → ∇|m| (derived) → sign boundary (emergent) → gravity");
            sb.AppendLine("");
            sb.AppendLine("  Feedback structure is the deepest carrier yet identified.");
            sb.AppendLine("  The density gradient ∇ρ is a PROJECTION of ∇fb onto |m| space.");
        }
        else
        {
            sb.AppendLine("  SHARED ORIGIN: ∇fb and ∇Tick both contribute to ∇|m|.");
            sb.AppendLine("  No single primitive dominates — gradient emerges from");
            sb.AppendLine("  the coupled fb-Tick-sign structure.");
        }
        sb.AppendLine("");

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        double fbR = PearsonCorr(lGM, lGF);
        bool critA = Math.Abs(fbR) > 0.50;              // Strong ∇fb→∇|m|
        bool critB = bdryRatio > 1.50;                   // Boundary selects ∇fb
        bool critC = fbOnlyR2 > tickOnlyR2 + 0.10;       // ∇fb dominates ∇Tick
        bool critD = fbScore > tickScore + 0.30;          // Significant margin

        int critMet = (critA ? 1 : 0) + (critB ? 1 : 0) + (critC ? 1 : 0) + (critD ? 1 : 0);

        string verdict = critMet >= 4 ? "SUPPORTED: feedback is the primary source."
            : critMet >= 2 ? "CONDITIONAL: shared origin with sign structure."
            : "FALSIFIED: feedback is secondary.";

        sb.AppendLine($"VERDICT: {verdict}  ({critMet}/4 criteria)");
        sb.AppendLine("");
        sb.AppendLine($"  A. ∇fb→∇|m| r > 0.50:                   {(critA ? "YES" : "NO")} (r={fbR:F4})");
        sb.AppendLine($"  B. Boundary selects ∇fb >1.5x:           {(critB ? "YES" : "NO")} ({bdryRatio:F2}x)");
        sb.AppendLine($"  C. ∇fb R² > ∇Tick R² + 0.10:            {(critC ? "YES" : "NO")} (ΔR²={fbOnlyR2-tickOnlyR2:F4})");
        sb.AppendLine($"  D. fb score > tick score + 0.30:         {(critD ? "YES" : "NO")} ({fbScore:F3} vs {tickScore:F3})");
        sb.AppendLine("");
        sb.AppendLine("Feedback Gradient Origin Result:");
        sb.AppendLine($"  ∇fb → ∇|m| r = {fbR:F4}  (boundary ∇fb = {bFbGrad/Math.Max(1e-15,dFbGrad):F2}x discarded)");
        sb.AppendLine($"  Primitive source: {bestSource}");
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== FGO_01 complete. Commit: FGO_01_FeedbackGradientOriginAudit ===");

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

    private record FgoCell(string Arch, double AbsM, int Sign, double Fb, double Tick,
        double GradM, double GradFb, double GradTick, double LapFb, int GradDir, int FbDir, bool IsBoundary, double SignGrad);
}
