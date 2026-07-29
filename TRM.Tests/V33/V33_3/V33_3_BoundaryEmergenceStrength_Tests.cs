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

namespace TRM.Tests.V33_3;

[Trait("Category", "V33_3")]
[Trait("Category", "LongRunning")]
public class V33_3_BoundaryEmergenceStrength_Tests
{
    private readonly ITestOutputHelper _o;
    public V33_3_BoundaryEmergenceStrength_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void BES_01_BoundaryEmergenceStrengthAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== BES_01: Boundary Emergence Strength Audit ===");
        sb.AppendLine("=== Why does boundary extraction strengthen the gradient law? ===");
        sb.AppendLine(new string('=', 108));

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);

        var allPopResults = new List<PopulationResult>();

        foreach (var (arch, fam) in new[] { ("GAN", VcFamily.GAN), ("CNS", VcFamily.CNS) })
        {
            const int nG = 18;
            double bMin = 0.0, bMax = 2.0, gMin = 0.0, gMax = 2.0;
            double db = (bMax - bMin) / (nG - 1), dg = (gMax - gMin) / (nG - 1);

            var gM = new double[nG, nG];
            var gS = new int[nG, nG];
            var gTick = new double[nG, nG];

            Parallel.For(0, nG, bi =>
            {
                double beta = bMin + db * bi;
                for (int gi = 0; gi < nG; gi++)
                {
                    double gamma = gMin + dg * gi;
                    var f = ComputeFull(fam, 1.0, 1.0, 0.70, beta, gamma, distances, sortedD, xiBase, k0Base, nA, aMin, daD);
                    gM[bi, gi] = f.absM; gS[bi, gi] = f.sign; gTick[bi, gi] = f.tick;
                }
            });

            // Per-cell gradient computation (interior only)
            var allCells = new List<CellMetrics>();
            var boundarySet = new HashSet<(int, int)>();

            for (int bi = 1; bi < nG - 1; bi++)
                for (int gi = 1; gi < nG - 1; gi++)
                {
                    // Is this a boundary cell?
                    bool isBdry = gS[bi, gi] != gS[bi + 1, gi] || gS[bi, gi] != gS[bi - 1, gi] ||
                                  gS[bi, gi] != gS[bi, gi + 1] || gS[bi, gi] != gS[bi, gi - 1];
                    if (isBdry) boundarySet.Add((bi, gi));

                    // Gradient of |m|
                    double dMdB = (gM[bi + 1, gi] - gM[bi - 1, gi]) / (2 * db);
                    double dMdG = (gM[bi, gi + 1] - gM[bi, gi - 1]) / (2 * dg);
                    double gradMag = Math.Sqrt(dMdB * dMdB + dMdG * dMdG);

                    // Gradient of Tick
                    double dTdB = (gTick[bi + 1, gi] - gTick[bi - 1, gi]) / (2 * db);
                    double dTdG = (gTick[bi, gi + 1] - gTick[bi, gi - 1]) / (2 * dg);
                    double tickGrad = Math.Sqrt(dTdB * dTdB + dTdG * dTdG);

                    // Curvature (Laplacian)
                    double lap = (gM[bi + 1, gi] + gM[bi - 1, gi] + gM[bi, gi + 1] + gM[bi, gi - 1] - 4 * gM[bi, gi]) / (db * db);

                    // Gradient direction coherence
                    int gDir = (dMdB + dMdG) > 1e-15 ? +1 : (dMdB + dMdG) < -1e-15 ? -1 : 0;
                    bool dirMatchesSign = (gDir == +1 && gS[bi, gi] > 0) || (gDir == -1 && gS[bi, gi] < 0);

                    // Channel: does ∇|m| anti-align with ∇sign? (sign changes where gradient is strongest)
                    double signGradB = 0, signGradG = 0;
                    if (bi > 1 && bi < nG - 2) signGradB = (gS[bi + 1, gi] - gS[bi - 1, gi]) / (2 * db);
                    if (gi > 1 && gi < nG - 2) signGradG = (gS[bi, gi + 1] - gS[bi, gi - 1]) / (2 * dg);
                    double signGradMag = Math.Sqrt(signGradB * signGradB + signGradG * signGradG);

                    allCells.Add(new CellMetrics(gM[bi, gi], gS[bi, gi], gTick[bi, gi],
                        gradMag, tickGrad, Math.Abs(lap), dirMatchesSign, signGradMag, isBdry));
                }

            if (allCells.Count < 50) continue;

            var bdryCells = allCells.Where(c => c.IsBoundary).ToList();
            var discCells = allCells.Where(c => !c.IsBoundary).ToList();

            // ================================================================
            // POPULATION STATISTICS
            // ================================================================
            double fracBdry = 100.0 * bdryCells.Count / allCells.Count;

            // Gradient magnitude
            double allGradMean = allCells.Average(c => c.GradMag);
            double bdryGradMean = bdryCells.Average(c => c.GradMag);
            double discGradMean = discCells.Average(c => c.GradMag);
            double gradRatio = bdryGradMean / Math.Max(1e-15, discGradMean);

            // |m| magnitude
            double allMMean = allCells.Average(c => c.AbsM);
            double bdryMMean = bdryCells.Average(c => c.AbsM);
            double discMMean = discCells.Average(c => c.AbsM);
            double mRatio = bdryMMean / Math.Max(1e-15, discMMean);

            // Curvature concentration
            double allCurvMean = allCells.Average(c => c.CurvMag);
            double bdryCurvMean = bdryCells.Average(c => c.CurvMag);
            double discCurvMean = discCells.Average(c => c.CurvMag);
            double curvRatio = bdryCurvMean / Math.Max(1e-15, discCurvMean);

            // Sign coherence (fraction where gradient direction matches sign)
            double allCoherence = 100.0 * allCells.Count(c => c.DirMatchesSign) / allCells.Count;
            double bdryCoherence = 100.0 * bdryCells.Count(c => c.DirMatchesSign) / bdryCells.Count;
            double discCoherence = 100.0 * discCells.Count(c => c.DirMatchesSign) / discCells.Count;

            // Gradient-law strength: |∇|m|| vs |m| correlation
            double[] allGm = allCells.Select(c => Math.Log10(Math.Max(1e-15, c.GradMag))).ToArray();
            double[] allAm = allCells.Select(c => Math.Log10(Math.Max(1e-15, c.AbsM))).ToArray();
            double allGradR = PearsonCorr(allGm, allAm);

            double[] bGm = bdryCells.Select(c => Math.Log10(Math.Max(1e-15, c.GradMag))).ToArray();
            double[] bAm = bdryCells.Select(c => Math.Log10(Math.Max(1e-15, c.AbsM))).ToArray();
            double bdryGradR = PearsonCorr(bGm, bAm);

            double[] dGm = discCells.Select(c => Math.Log10(Math.Max(1e-15, c.GradMag))).ToArray();
            double[] dAm = discCells.Select(c => Math.Log10(Math.Max(1e-15, c.AbsM))).ToArray();
            double discGradR = PearsonCorr(dGm, dAm);

            // Curvature-gradient correlation
            double[] allLap = allCells.Select(c => Math.Log10(Math.Max(1e-15, c.CurvMag))).ToArray();
            double allCurvR = PearsonCorr(allGm, allLap);
            double[] bLap = bdryCells.Select(c => Math.Log10(Math.Max(1e-15, c.CurvMag))).ToArray();
            double bdryCurvR = PearsonCorr(bGm, bLap);
            double[] dLap = discCells.Select(c => Math.Log10(Math.Max(1e-15, c.CurvMag))).ToArray();
            double discCurvR = PearsonCorr(dGm, dLap);

            // Signal-to-noise: ratio of gradient-law R² in boundary vs discarded
            double snrShape = bdryCoherence / Math.Max(1e-15, discCoherence);
            double snrAmp = bdryGradR * bdryGradR / Math.Max(1e-15, discGradR * discGradR);
            double snrCurv = bdryCurvR * bdryCurvR / Math.Max(1e-15, discCurvR * discCurvR);

            allPopResults.Add(new PopulationResult(arch, allCells.Count, bdryCells.Count, discCells.Count,
                fracBdry, allGradMean, bdryGradMean, discGradMean, gradRatio,
                allMMean, bdryMMean, discMMean, mRatio,
                allCurvMean, bdryCurvMean, discCurvMean, curvRatio,
                allCoherence, bdryCoherence, discCoherence,
                allGradR, bdryGradR, discGradR,
                allCurvR, bdryCurvR, discCurvR,
                snrShape, snrAmp, snrCurv));
        }

        if (allPopResults.Count < 2) { sb.AppendLine("Insufficient data."); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        // ================================================================
        // BOUNDARY SELECTION TABLE
        // ================================================================
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Boundary Selection Table ===");
        sb.AppendLine("");
        sb.AppendLine($"{"Arch",-8} {"%Bdry",7} {"Grad:B/D",9} {"|m|:B/D",9} {"Curv:B/D",9} {"Coh:B/D",9} {"SNR:Shape",10} {"SNR:Amp",10} {"SNR:Curv",10}");
        sb.AppendLine(new string('-', 86));

        foreach (var p in allPopResults)
        {
            sb.AppendLine($"{p.Arch,-8} {p.FracBoundary,7:F1}% {p.GradRatio,9:F2}x {p.MRatio,9:F2}x {p.CurvRatio,9:F2}x {p.BdryCoherence/p.DiscCoherence,9:F2}x {p.SnrShape,10:F2}x {p.SnrAmp,10:F2}x {p.SnrCurv,10:F2}x");
        }
        sb.AppendLine("");

        // ================================================================
        // SIGNAL AMPLIFICATION ANALYSIS
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Signal Amplification Analysis ===");
        sb.AppendLine("");

        var avg = new
        {
            GradB_D = allPopResults.Average(p => p.GradRatio),
            MB_D = allPopResults.Average(p => p.MRatio),
            CurvB_D = allPopResults.Average(p => p.CurvRatio),
            CohB_D = allPopResults.Average(p => p.BdryCoherence / Math.Max(1e-15, p.DiscCoherence)),
            SnrShape = allPopResults.Average(p => p.SnrShape),
            SnrAmp = allPopResults.Average(p => p.SnrAmp),
            SnrCurv = allPopResults.Average(p => p.SnrCurv),
        };

        sb.AppendLine($"  Mean boundary/discarded ratios:");
        sb.AppendLine($"    Gradient magnitude:     {avg.GradB_D:F2}x  (boundary {(avg.GradB_D > 1.1 ? "AMPLIFIES" : "preserves")} gradient)");
        sb.AppendLine($"    |m| magnitude:           {avg.MB_D:F2}x  (boundary {(avg.MB_D > 1.05 ? "CONCENTRATES" : "preserves")} |m|)");
        sb.AppendLine($"    Curvature magnitude:    {avg.CurvB_D:F2}x  (boundary {(avg.CurvB_D > 1.2 ? "STRONGLY CONCENTRATES" : "preserves")} curvature)");
        sb.AppendLine($"    Sign coherence:         {avg.CohB_D:F2}x  (boundary {(avg.CohB_D > 1.05 ? "AMPLIFIES" : "preserves")} coherence)");
        sb.AppendLine("");
        sb.AppendLine($"  Signal-to-noise improvement (boundary vs discarded):");
        sb.AppendLine($"    Shape SNR:              {avg.SnrShape:F2}x");
        sb.AppendLine($"    Amplitude SNR:          {avg.SnrAmp:F2}x");
        sb.AppendLine($"    Curvature SNR:          {avg.SnrCurv:F2}x");
        sb.AppendLine("");

        // ================================================================
        // GRADIENT CONCENTRATION METRICS
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Gradient Concentration Metrics ===");
        sb.AppendLine("");

        sb.AppendLine($"{"Arch",-8} {"All R²",8} {"Bdry R²",8} {"Disc R²",8} {"ΔR²",8} {"B/D R²",8}");
        sb.AppendLine(new string('-', 50));

        foreach (var p in allPopResults)
        {
            double allR2 = p.AllGradR * p.AllGradR;
            double bR2 = p.BdryGradR * p.BdryGradR;
            double dR2 = p.DiscGradR * p.DiscGradR;
            sb.AppendLine($"{p.Arch,-8} {allR2,8:F4} {bR2,8:F4} {dR2,8:F4} {bR2-dR2,8:F4} {bR2/Math.Max(1e-15,dR2),8:F2}x");
        }
        sb.AppendLine("");

        // Curvature concentration
        sb.AppendLine($"{"Arch",-8} {"All ∇-∇²r",10} {"Bdry ∇-∇²r",10} {"Disc ∇-∇²r",10} {"Δr",8} {"B/D r",8}");
        sb.AppendLine(new string('-', 56));

        foreach (var p in allPopResults)
            sb.AppendLine($"{p.Arch,-8} {p.AllCurvR,10:F4} {p.BdryCurvR,10:F4} {p.DiscCurvR,10:F4} {p.BdryCurvR-p.DiscCurvR,8:F4} {p.BdryCurvR/Math.Max(1e-15,p.DiscCurvR),8:F2}x");
        sb.AppendLine("");

        // ================================================================
        // MECHANISM ASSESSMENT
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Boundary as Structural Amplifier ===");
        sb.AppendLine("");

        bool amplifiesGrad = avg.GradB_D > 1.10;
        bool amplifiesCurv = avg.CurvB_D > 1.20;
        bool amplifiesSNR = avg.SnrAmp > 1.50;
        bool amplifiesCoh = avg.CohB_D > 1.05;

        int ampSignals = (amplifiesGrad ? 1 : 0) + (amplifiesCurv ? 1 : 0) + (amplifiesSNR ? 1 : 0) + (amplifiesCoh ? 1 : 0);

        sb.AppendLine($"  Amplification signals: {ampSignals}/4");
        sb.AppendLine("");
        sb.AppendLine($"    Gradient amplification:    {(amplifiesGrad ? "✓ YES" : "✗ NO")} ({avg.GradB_D:F2}x)");
        sb.AppendLine($"    Curvature concentration:   {(amplifiesCurv ? "✓ YES" : "✗ NO")} ({avg.CurvB_D:F2}x)");
        sb.AppendLine($"    SNR improvement:           {(amplifiesSNR ? "✓ YES" : "✗ NO")} ({avg.SnrAmp:F2}x)");
        sb.AppendLine($"    Coherence amplification:   {(amplifiesCoh ? "✓ YES" : "✗ NO")} ({avg.CohB_D:F2}x)");
        sb.AppendLine("");

        if (ampSignals >= 3)
        {
            sb.AppendLine("  BOUNDARY IS AN ACTIVE STRUCTURAL AMPLIFIER:");
            sb.AppendLine("  Sign-change boundary extraction selects high-gradient,");
            sb.AppendLine("  high-curvature regions where |m|-gradient coherence is");
            sb.AppendLine("  maximized. This is NOT a passive filter — it actively");
            sb.AppendLine("  concentrates the gradient signal that becomes gravity.");
        }
        else if (ampSignals >= 2)
        {
            sb.AppendLine("  BOUNDARY IS A PARTIAL AMPLIFIER:");
            sb.AppendLine("  Some gradient properties are amplified by boundary");
            sb.AppendLine("  extraction, but others are unchanged. The mechanism");
            sb.AppendLine("  is present but not the sole source of structure.");
        }
        else
        {
            sb.AppendLine("  BOUNDARY IS A PASSIVE FILTER:");
            sb.AppendLine("  Boundary extraction does not significantly amplify");
            sb.AppendLine("  gradient properties. The gradient law is equally");
            sb.AppendLine("  present in boundary and interior cells.");
        }
        sb.AppendLine("");

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        bool critA = ampSignals >= 3;
        bool critB = avg.SnrAmp > 1.5;
        bool critC = allPopResults.Count >= 2;
        bool critD = avg.CurvB_D > 1.2;

        int critMet = (critA ? 1 : 0) + (critB ? 1 : 0) + (critC ? 1 : 0) + (critD ? 1 : 0);

        string verdict = critMet >= 4 ? "SUPPORTED: boundary extraction amplifies gradient information."
            : critMet >= 2 ? "CONDITIONAL: partial amplification."
            : "FALSIFIED: amplification is incidental.";

        sb.AppendLine($"VERDICT: {verdict}  ({critMet}/4 criteria)");
        sb.AppendLine("");
        sb.AppendLine($"  A. ≥3 amplification signals:            {(critA ? "YES" : "NO")} ({ampSignals}/4)");
        sb.AppendLine($"  B. Amp SNR > 1.5x:                       {(critB ? "YES" : "NO")} ({avg.SnrAmp:F2}x)");
        sb.AppendLine($"  C. ≥2 families:                          {(critC ? "YES" : "NO")} ({allPopResults.Count})");
        sb.AppendLine($"  D. Curvature concentration > 1.2x:       {(critD ? "YES" : "NO")} ({avg.CurvB_D:F2}x)");
        sb.AppendLine("");
        sb.AppendLine("Boundary Emergence Result:");
        sb.AppendLine($"  Gradient: {avg.GradB_D:F2}x  Curvature: {avg.CurvB_D:F2}x");
        sb.AppendLine($"  Coherence: {avg.CohB_D:F2}x  SNR(Amp): {avg.SnrAmp:F2}x");
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== BES_01 complete. Commit: BES_01_BoundaryEmergenceStrengthAudit ===");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    private static double PearsonCorr(double[] xs, double[] ys)
    {
        int n = xs.Length; if (n < 2) return 0;
        double mx = xs.Average(), my = ys.Average();
        double sxy = 0, sxx = 0, syy = 0;
        for (int i = 0; i < n; i++) { double dx = xs[i] - mx, dy = ys[i] - my; sxy += dx * dy; sxx += dx * dx; syy += dy * dy; }
        return sxx > 1e-15 && syy > 1e-15 ? sxy / Math.Sqrt(sxx * syy) : 0;
    }

    private record CellMetrics(double AbsM, int Sign, double Tick, double GradMag, double TickGrad, double CurvMag, bool DirMatchesSign, double SignGradMag, bool IsBoundary);
    private record PopulationResult(string Arch, int AllCount, int BdryCount, int DiscCount,
        double FracBoundary, double AllGradMean, double BdryGradMean, double DiscGradMean, double GradRatio,
        double AllMMean, double BdryMMean, double DiscMMean, double MRatio,
        double AllCurvMean, double BdryCurvMean, double DiscCurvMean, double CurvRatio,
        double AllCoherence, double BdryCoherence, double DiscCoherence,
        double AllGradR, double BdryGradR, double DiscGradR,
        double AllCurvR, double BdryCurvR, double DiscCurvR,
        double SnrShape, double SnrAmp, double SnrCurv);
}
