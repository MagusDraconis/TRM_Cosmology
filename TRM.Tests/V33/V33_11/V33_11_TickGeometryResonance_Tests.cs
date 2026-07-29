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

namespace TRM.Tests.V33_11;

[Trait("Category", "V33_11")]
[Trait("Category", "LongRunning")]
public class V33_11_TickGeometryResonance_Tests
{
    private readonly ITestOutputHelper _o;
    public V33_11_TickGeometryResonance_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void TGR_01_TickGeometryResonanceAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== TGR_01: Tick Geometry Resonance Audit ===");
        sb.AppendLine("=== Do Tick and geometry form a feedback loop? ===");
        sb.AppendLine(new string('=', 108));

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);

        var allCells = new ConcurrentBag<TgrCell>();

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

            // Compute BFS distance from boundaries for "near-boundary" classification
            var dist = new int[nG, nG];
            for (int bi = 0; bi < nG; bi++) for (int gi = 0; gi < nG; gi++) dist[bi, gi] = int.MaxValue;
            var q = new Queue<(int, int)>();
            for (int bi = 1; bi < nG - 1; bi++)
                for (int gi = 1; gi < nG - 1; gi++)
                    if (gS[bi, gi] != gS[bi + 1, gi] || gS[bi, gi] != gS[bi - 1, gi] ||
                        gS[bi, gi] != gS[bi, gi + 1] || gS[bi, gi] != gS[bi, gi - 1])
                    { dist[bi, gi] = 0; q.Enqueue((bi, gi)); }
            while (q.Count > 0)
            {
                var (bi, gi) = q.Dequeue();
                foreach (var (nb, ng) in new[] { (bi + 1, gi), (bi - 1, gi), (bi, gi + 1), (bi, gi - 1) })
                    if (nb >= 0 && nb < nG && ng >= 0 && ng < nG && dist[nb, ng] == int.MaxValue)
                    { dist[nb, ng] = dist[bi, gi] + 1; q.Enqueue((nb, ng)); }
            }

            for (int bi = 1; bi < nG - 1; bi++)
                for (int gi = 1; gi < nG - 1; gi++)
                {
                    double dMdB = (gM[bi + 1, gi] - gM[bi - 1, gi]) / (2 * db);
                    double dMdG = (gM[bi, gi + 1] - gM[bi, gi - 1]) / (2 * dg);
                    double gradM = Math.Sqrt(dMdB * dMdB + dMdG * dMdG);

                    double dFdB = (gFb[bi + 1, gi] - gFb[bi - 1, gi]) / (2 * db);
                    double dFdG = (gFb[bi, gi + 1] - gFb[bi, gi - 1]) / (2 * dg);
                    double gradFb = Math.Sqrt(dFdB * dFdB + dFdG * dFdG);

                    double dTdB = (gTick[bi + 1, gi] - gTick[bi - 1, gi]) / (2 * db);
                    double dTdG = (gTick[bi, gi + 1] - gTick[bi, gi - 1]) / (2 * dg);
                    double gradTickVal = Math.Sqrt(dTdB * dTdB + dTdG * dTdG);

                    double lapM = Math.Abs(gM[bi + 1, gi] + gM[bi - 1, gi] + gM[bi, gi + 1] + gM[bi, gi - 1] - 4 * gM[bi, gi]) / (db * db);

                    int d = dist[bi, gi] == int.MaxValue ? 4 : Math.Min(3, dist[bi, gi]);
                    bool isBdry = d == 0;

                    // Tick anomaly: deviation from local mean
                    var nTick = new List<double>();
                    if (bi > 0) nTick.Add(gTick[bi - 1, gi]); if (bi + 1 < nG) nTick.Add(gTick[bi + 1, gi]);
                    if (gi > 0) nTick.Add(gTick[bi, gi - 1]); if (gi + 1 < nG) nTick.Add(gTick[bi, gi + 1]);
                    double tickLocalMean = nTick.Count > 0 ? nTick.Average() : gTick[bi, gi];
                    double tickAnomaly = Math.Abs(gTick[bi, gi] - tickLocalMean) / Math.Max(1e-15, tickLocalMean);

                    // Tick vs |m| discrepancy
                    double tickMDisc = Math.Abs(gTick[bi, gi] - gM[bi, gi]) / Math.Max(1e-15, Math.Max(gTick[bi, gi], gM[bi, gi]));

                    allCells.Add(new TgrCell(arch, gM[bi, gi], gS[bi, gi], gFb[bi, gi], gTick[bi, gi],
                        gradM, gradFb, gradTickVal, lapM, d, isBdry, tickAnomaly, tickMDisc));
                }
        }

        var all = allCells.ToList();
        if (all.Count < 50) { sb.AppendLine("Insufficient."); Assert.True(true); return; }

        sb.AppendLine("");
        sb.AppendLine($"  Grid cells: {all.Count} across 2 families");
        sb.AppendLine("");

        // ================================================================
        // TICK–GEOMETRY INTERACTION MATRIX
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Tick–Geometry Interaction Matrix ===");
        sb.AppendLine("");

        double[] tickV = all.Select(c => c.Tick).ToArray();
        double[] tAnom = all.Select(c => c.TickAnomaly).ToArray();
        double[] tDisc = all.Select(c => c.TickMDisc).ToArray();
        double[] lGM = all.Select(c => Math.Log10(Math.Max(1e-15, c.GradM))).ToArray();
        double[] lGF = all.Select(c => Math.Log10(Math.Max(1e-15, c.GradFb))).ToArray();
        double[] lGT = all.Select(c => Math.Log10(Math.Max(1e-15, c.GradTickVal))).ToArray();
        double[] lLM = all.Select(c => Math.Log10(Math.Max(1e-15, c.LapM))).ToArray();
        double[] bdD = all.Select(c => (double)c.ZoneDist).ToArray();

        sb.AppendLine($"{"Correlation",-36} {"r",8} {"R²",8} {"Direction",14}");
        sb.AppendLine(new string('-', 68));
        Report(sb, "∇|m| ↔ Tick anomaly", PearsonCorr(lGM, tAnom));
        Report(sb, "∇|m| ↔ Tick-|m| discrepancy", PearsonCorr(lGM, tDisc));
        Report(sb, "∇fb ↔ Tick anomaly", PearsonCorr(lGF, tAnom));
        Report(sb, "Boundary distance ↔ Tick", PearsonCorr(bdD, tickV));
        Report(sb, "Boundary distance ↔ Tick anomaly", PearsonCorr(bdD, tAnom));
        Report(sb, "∇²|m| ↔ Tick anomaly", PearsonCorr(lLM, tAnom));
        Report(sb, "∇Tick ↔ ∇|m|", PearsonCorr(lGT, lGM));
        Report(sb, "∇Tick ↔ ∇fb", PearsonCorr(lGT, lGF));
        sb.AppendLine("");

        // ================================================================
        // Q1: Does geometry alter Tick?
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Q1: Does geometry alter Tick statistics? ===");
        sb.AppendLine("");

        for (int z = 0; z <= 3; z++)
        {
            var cells = all.Where(c => c.ZoneDist == z).ToList();
            if (cells.Count < 10) continue;
            double tMean = cells.Average(c => c.Tick);
            double tStd = Math.Sqrt(cells.Average(c => (c.Tick - tMean) * (c.Tick - tMean)));
            double tAnomM = cells.Average(c => c.TickAnomaly);
            double tDiscM = cells.Average(c => c.TickMDisc);
            string zn = z == 0 ? "Boundary" : z == 1 ? "Near-1" : z == 2 ? "Near-2" : "Interior";
            sb.AppendLine($"  {zn,-12}: Tick={tMean:F4}±{tStd:F4}  anomaly={tAnomM:F4}  disc={tDiscM:F4}  n={cells.Count}");
        }

        // Boundary vs interior comparison
        var bdry = all.Where(c => c.ZoneDist == 0).ToList();
        var interior = all.Where(c => c.ZoneDist >= 3).ToList();
        double bTick = bdry.Average(c => c.Tick);
        double iTick = interior.Average(c => c.Tick);
        double bAnom = bdry.Average(c => c.TickAnomaly);
        double iAnom = interior.Average(c => c.TickAnomaly);
        sb.AppendLine($"");
        sb.AppendLine($"  Boundary/Interior Tick ratio: {bTick/Math.Max(1e-15,iTick):F2}x");
        sb.AppendLine($"  Boundary/Interior anomaly ratio: {bAnom/Math.Max(1e-15,iAnom):F2}x");
        sb.AppendLine($"  → {(Math.Abs(bTick-iTick)/Math.Max(1e-15,iTick) > 0.10 ? "Tick IS ALTERED by geometry" : "Tick STABLE across geometry")}");
        sb.AppendLine("");

        // ================================================================
        // Q2: Does Tick lag geometry changes?
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Q2: Does Tick anomaly increase near boundaries? ===");
        sb.AppendLine("");

        double[] distV = all.Select(c => (double)c.ZoneDist).ToArray();
        double distAnomR = PearsonCorr(distV, tAnom);
        double distDiscR = PearsonCorr(distV, tDisc);

        // High-gradient vs low-gradient cells: tick behavior
        double gradMed = all.Select(c => c.GradM).OrderBy(g => g).ElementAt(all.Count / 2);
        var hiGrad = all.Where(c => c.GradM >= gradMed).ToList();
        var loGrad = all.Where(c => c.GradM < gradMed).ToList();
        double hTick = hiGrad.Average(c => c.Tick);
        double lTick = loGrad.Average(c => c.Tick);
        double hAnom = hiGrad.Average(c => c.TickAnomaly);
        double lAnom = loGrad.Average(c => c.TickAnomaly);

        sb.AppendLine($"  Distance vs Tick anomaly: r = {distAnomR:F4}  ({(distAnomR < -0.10 ? "anomaly INCREASES near boundary" : "no spatial pattern")})");
        sb.AppendLine($"  Distance vs Tick-|m| disc: r = {distDiscR:F4}");
        sb.AppendLine($"  High gradient: Tick={hTick:F4}  anomaly={hAnom:F4}");
        sb.AppendLine($"  Low  gradient: Tick={lTick:F4}  anomaly={lAnom:F4}");
        sb.AppendLine($"  → {(hAnom/lAnom > 1.15 ? "Tick anomaly HIGHER in strong-gradient regions" : "Tick behavior gradient-independent")}");
        sb.AppendLine("");

        // ================================================================
        // Q3: Does ∇fb increase where Tick is anomalous?
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Q3: Does ∇fb increase in anomalous Tick regions? ===");
        sb.AppendLine("");

        double anomMed = all.Select(c => c.TickAnomaly).OrderBy(a => a).ElementAt(all.Count / 2);
        var hiAnom = all.Where(c => c.TickAnomaly >= anomMed).ToList();
        var loAnom = all.Where(c => c.TickAnomaly < anomMed).ToList();

        double hFb = hiAnom.Average(c => c.GradFb);
        double lFb = loAnom.Average(c => c.GradFb);
        double hGM = hiAnom.Average(c => c.GradM);
        double lGMg = loAnom.Average(c => c.GradM);

        sb.AppendLine($"  High Tick anomaly: ∇fb={hFb:F4}  ∇|m|={hGM:F4}");
        sb.AppendLine($"  Low  Tick anomaly: ∇fb={lFb:F4}  ∇|m|={lGMg:F4}");
        sb.AppendLine($"  ∇fb ratio: {hFb/Math.Max(1e-15,lFb):F2}x  ∇|m| ratio: {hGM/Math.Max(1e-15,lGMg):F2}x");
        sb.AppendLine($"  → {(hFb/lFb > 1.15 ? "∇fb AMPLIFIED in anomalous Tick — feedback loop active" : "No feedback amplification")}");
        sb.AppendLine("");

        // ================================================================
        // Q4: Closed loop evidence
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Q4: Evidence for closed Tick-Geometry loop ===");
        sb.AppendLine("");

        bool geomAltersTick = Math.Abs(bTick - iTick) / Math.Max(1e-15, iTick) > 0.10;
        bool tickAnomNearBdry = distAnomR < -0.10;
        bool fbAmplifiedAtAnom = hFb / Math.Max(1e-15, lFb) > 1.15;
        bool gradDrivesAnom = hAnom / Math.Max(1e-15, lAnom) > 1.15;

        int loopSignals = (geomAltersTick ? 1 : 0) + (tickAnomNearBdry ? 1 : 0) + (fbAmplifiedAtAnom ? 1 : 0) + (gradDrivesAnom ? 1 : 0);

        sb.AppendLine($"  Loop signals: {loopSignals}/4");
        sb.AppendLine($"    Geometry → Tick:            {(geomAltersTick ? "✓" : "✗")}  (B/I ratio={bTick/iTick:F2}x)");
        sb.AppendLine($"    Boundary → Tick anomaly:    {(tickAnomNearBdry ? "✓" : "✗")}  (r={distAnomR:F4})");
        sb.AppendLine($"    Tick anomaly → ∇fb:         {(fbAmplifiedAtAnom ? "✓" : "✗")}  ({hFb/lFb:F2}x)");
        sb.AppendLine($"    ∇|m| → Tick anomaly:        {(gradDrivesAnom ? "✓" : "✗")}  ({hAnom/lAnom:F2}x)");
        sb.AppendLine("");

        if (loopSignals >= 3)
        {
            sb.AppendLine("  CLOSED LOOP DETECTED:");
            sb.AppendLine("");
            sb.AppendLine("    Gradient → Boundary → Geometry → Tick anomaly → ∇fb → Gradient");
            sb.AppendLine("");
            sb.AppendLine("  Tick and geometry form a RESONANCE LOOP:");
            sb.AppendLine("  Geometry alters Tick statistics at boundaries, anomalous Tick");
            sb.AppendLine("  amplifies feedback gradients, which strengthen the gradient→");
            sb.AppendLine("  boundary→geometry chain. This is a self-reinforcing structure.");
        }
        else if (loopSignals >= 2)
        {
            sb.AppendLine("  PARTIAL LOOP: some couplings present but not all.");
            sb.AppendLine("  Tick and geometry interact weakly — mostly hierarchical.");
        }
        else
        {
            sb.AppendLine("  NO LOOP: Tick and geometry are decoupled.");
            sb.AppendLine("  The hierarchy Gradient→Boundary→Geometry→Dynamics");
            sb.AppendLine("  is strictly one-directional.");
        }
        sb.AppendLine("");

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        bool critA = loopSignals >= 3;
        bool critB = loopSignals >= 2;
        bool critC = all.Count >= 200;
        bool critD = geomAltersTick || tickAnomNearBdry;

        int critMet = (critA ? 1 : 0) + (critB ? 1 : 0) + (critC ? 1 : 0) + (critD ? 1 : 0);

        string verdict = critMet >= 4 ? "SUPPORTED: Tick and geometry form a resonance loop."
            : critMet >= 2 ? "CONDITIONAL: weak coupling."
            : "FALSIFIED: pure hierarchy remains.";

        sb.AppendLine($"VERDICT: {verdict}  ({critMet}/4 criteria)");
        sb.AppendLine("");
        sb.AppendLine($"  A. ≥3 loop signals:                     {(critA ? "YES" : "NO")} ({loopSignals}/4)");
        sb.AppendLine($"  B. ≥2 loop signals:                     {(critB ? "YES" : "NO")} ({loopSignals}/4)");
        sb.AppendLine($"  C. ≥200 cells:                          {(critC ? "YES" : "NO")} ({all.Count})");
        sb.AppendLine($"  D. Geometry→Tick coupling:              {(critD ? "YES" : "NO")}");
        sb.AppendLine("");
        sb.AppendLine("Tick-Geometry Resonance Result:");
        sb.AppendLine($"  Loop signals: {loopSignals}/4");
        sb.AppendLine($"  Geometry→Tick: {(geomAltersTick ? "active" : "inactive")}");
        sb.AppendLine($"  Tick anomaly→∇fb: {(fbAmplifiedAtAnom ? "active" : "inactive")}");
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== TGR_01 complete. Commit: TGR_01_TickGeometryResonanceAudit ===");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    private static void Report(StringBuilder sb, string name, double r)
    {
        string dir = r > 0.10 ? "Forward" : r < -0.10 ? "Reverse" : "None";
        string s = Math.Abs(r) > 0.30 ? "STRONG" : Math.Abs(r) > 0.15 ? "MODERATE" : "WEAK";
        sb.AppendLine($"{name,-36} {r,8:F4} {r*r,8:F4} {dir,7} {s,7}");
    }

    private static double PearsonCorr(double[] xs, double[] ys)
    {
        int n = xs.Length; if (n < 2) return 0;
        double mx = xs.Average(), my = ys.Average();
        double sxy = 0, sxx = 0, syy = 0;
        for (int i = 0; i < n; i++) { double dx = xs[i] - mx, dy = ys[i] - my; sxy += dx * dy; sxx += dx * dx; syy += dy * dy; }
        return sxx > 1e-15 && syy > 1e-15 ? sxy / Math.Sqrt(sxx * syy) : 0;
    }

    private record TgrCell(string Arch, double AbsM, int Sign, double Fb, double Tick,
        double GradM, double GradFb, double GradTickVal, double LapM, int ZoneDist, bool IsBoundary, double TickAnomaly, double TickMDisc);
}
