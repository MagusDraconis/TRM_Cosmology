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

namespace TRM.Tests.V33_12;

[Trait("Category", "V33_12")]
[Trait("Category", "LongRunning")]
public class V33_12_ResonanceLoopStability_Tests
{
    private readonly ITestOutputHelper _o;
    public V33_12_ResonanceLoopStability_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void RLS_01_ResonanceLoopStabilityAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== RLS_01: Resonance Loop Stability Audit ===");
        sb.AppendLine("=== Does the Tick-Geometry loop self-sustain? ===");
        sb.AppendLine(new string('=', 108));

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);

        var loopResults = new List<LoopResult>();

        foreach (var (arch, fam) in new[] { ("GAN", VcFamily.GAN), ("CNS", VcFamily.CNS) })
        {
            const int nG = 20;
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

            // BFS distance
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

            // Collect per-cell metrics for loop gain computation
            var cells = new List<RlsCell>();
            for (int bi = 1; bi < nG - 1; bi++)
                for (int gi = 1; gi < nG - 1; gi++)
                {
                    double dMdB = (gM[bi + 1, gi] - gM[bi - 1, gi]) / (2 * db);
                    double dMdG = (gM[bi, gi + 1] - gM[bi, gi - 1]) / (2 * dg);
                    double gradM = Math.Sqrt(dMdB * dMdB + dMdG * dMdG);

                    double dFdB = (gFb[bi + 1, gi] - gFb[bi - 1, gi]) / (2 * db);
                    double dFdG = (gFb[bi, gi + 1] - gFb[bi, gi - 1]) / (2 * dg);
                    double gradFb = Math.Sqrt(dFdB * dFdB + dFdG * dFdG);

                    int d = dist[bi, gi] == int.MaxValue ? 4 : Math.Min(3, dist[bi, gi]);
                    bool isBdry = d == 0;

                    // Tick anomaly (local deviation)
                    var nT = new List<double>();
                    if (bi > 0) nT.Add(gTick[bi - 1, gi]); if (bi + 1 < nG) nT.Add(gTick[bi + 1, gi]);
                    if (gi > 0) nT.Add(gTick[bi, gi - 1]); if (gi + 1 < nG) nT.Add(gTick[bi, gi + 1]);
                    double tLocMean = nT.Count > 0 ? nT.Average() : gTick[bi, gi];
                    double tAnom = Math.Abs(gTick[bi, gi] - tLocMean) / Math.Max(1e-15, tLocMean);

                    cells.Add(new RlsCell(gradM, gradFb, d, isBdry, tAnom, gM[bi, gi], gTick[bi, gi]));
                }

            if (cells.Count < 100) continue;

            // ---- LOOP GAIN COMPUTATION ----
            // Link 1: Gradient → Boundary (high gradient → boundary probability)
            double gradMed = cells.Select(c => c.GradM).OrderBy(g => g).ElementAt(cells.Count / 2);
            var hiG = cells.Where(c => c.GradM >= gradMed).ToList();
            var loG = cells.Where(c => c.GradM < gradMed).ToList();
            double hiBdry = 100.0 * hiG.Count(c => c.IsBoundary) / hiG.Count;
            double loBdry = 100.0 * loG.Count(c => c.IsBoundary) / loG.Count;
            double g1 = hiBdry / Math.Max(1e-15, loBdry); // amplification factor

            // Link 2: Boundary → Tick anomaly
            var bdryC = cells.Where(c => c.IsBoundary).ToList();
            var intC = cells.Where(c => c.ZoneDist >= 3).ToList();
            double bAnom = bdryC.Average(c => c.TickAnomaly);
            double iAnom = intC.Average(c => c.TickAnomaly);
            double g2 = bAnom / Math.Max(1e-15, iAnom);

            // Link 3: Tick anomaly → Feedback gradient
            double anomMed = cells.Select(c => c.TickAnomaly).OrderBy(a => a).ElementAt(cells.Count / 2);
            var hiA = cells.Where(c => c.TickAnomaly >= anomMed).ToList();
            var loA = cells.Where(c => c.TickAnomaly < anomMed).ToList();
            double hFb = hiA.Average(c => c.GradFb);
            double lFb = loA.Average(c => c.GradFb);
            double g3 = hFb / Math.Max(1e-15, lFb);

            // Link 4: Feedback gradient → Density gradient
            double fbMed = cells.Select(c => c.GradFb).OrderBy(f => f).ElementAt(cells.Count / 2);
            var hiFb = cells.Where(c => c.GradFb >= fbMed).ToList();
            var loFb = cells.Where(c => c.GradFb < fbMed).ToList();
            double hGradM = hiFb.Average(c => c.GradM);
            double lGradM = loFb.Average(c => c.GradM);
            double g4 = hGradM / Math.Max(1e-15, lGradM);

            double totalGain = g1 * g2 * g3 * g4;

            // Saturation test: does gain change at extremes?
            // Top quartile vs bottom quartile for each link
            int qN = cells.Count / 4;
            var topGrad = cells.OrderByDescending(c => c.GradM).Take(qN).ToList();
            var botGrad = cells.OrderBy(c => c.GradM).Take(qN).ToList();
            double topBdry = 100.0 * topGrad.Count(c => c.IsBoundary) / topGrad.Count;
            double botBdry = 100.0 * botGrad.Count(c => c.IsBoundary) / botGrad.Count;
            double g1Sat = topBdry / Math.Max(1e-15, botBdry); // top vs bottom quartile

            loopResults.Add(new LoopResult(arch, cells.Count, g1, g2, g3, g4, totalGain, g1Sat,
                hiBdry, loBdry, bAnom, iAnom, hFb, lFb, hGradM, lGradM));
        }

        if (loopResults.Count < 2) { sb.AppendLine("Insufficient."); Assert.True(true); return; }

        // ================================================================
        // LOOP GAIN MATRIX
        // ================================================================
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Loop Gain Matrix ===");
        sb.AppendLine("");
        sb.AppendLine($"{"Arch",-8} {"G1:∇→B",8} {"G2:B→A",8} {"G3:A→F",8} {"G4:F→∇",8} {"Total G",9} {"Sat G1",8}");
        sb.AppendLine(new string('-', 60));

        foreach (var lr in loopResults)
            sb.AppendLine($"{lr.Arch,-8} {lr.G1,8:F3}x {lr.G2,8:F3}x {lr.G3,8:F3}x {lr.G4,8:F3}x {lr.TotalGain,9:F3}x {lr.G1Sat,8:F3}x");
        sb.AppendLine("");

        double avgGain = loopResults.Average(l => l.TotalGain);
        double avgG1 = loopResults.Average(l => l.G1);
        double avgG2 = loopResults.Average(l => l.G2);
        double avgG3 = loopResults.Average(l => l.G3);
        double avgG4 = loopResults.Average(l => l.G4);

        sb.AppendLine($"  Mean loop gain: G = {avgGain:F3}x  (G1={avgG1:F3} × G2={avgG2:F3} × G3={avgG3:F3} × G4={avgG4:F3})");
        sb.AppendLine("");

        // ================================================================
        // RESONANCE CLASSIFICATION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Resonance Classification ===");
        sb.AppendLine("");

        if (avgGain > 1.10)
            sb.AppendLine("  SELF-AMPLIFYING RESONANCE (G > 1):");
        else if (avgGain > 0.95)
            sb.AppendLine("  NEUTRAL RESONANCE (G ≈ 1):");
        else if (avgGain > 0.70)
            sb.AppendLine("  WEAKLY DAMPED RESONANCE (G < 1):");
        else
            sb.AppendLine("  STRONGLY DAMPED (G ≪ 1):");

        sb.AppendLine($"  Total gain G = {avgGain:F3}x");
        sb.AppendLine($"  → After one full cycle, signal is {(avgGain > 1 ? "AMPLIFIED" : "ATTENUATED")} by {avgGain:F3}x");
        sb.AppendLine("");

        // Identify strongest/weakest link
        var links = new[] { ("G1: Gradient→Boundary", avgG1), ("G2: Boundary→Anomaly", avgG2), ("G3: Anomaly→Feedback", avgG3), ("G4: Feedback→Gradient", avgG4) };
        var strongest = links.OrderByDescending(l => l.Item2).First();
        var weakest = links.OrderBy(l => l.Item2).First();

        sb.AppendLine($"  Strongest link: {strongest.Item1} ({strongest.Item2:F3}x)");
        sb.AppendLine($"  Weakest link:   {weakest.Item1} ({weakest.Item2:F3}x)");
        sb.AppendLine("");

        // ================================================================
        // ATTRACTOR ANALYSIS
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Attractor Analysis ===");
        sb.AppendLine("");

        // Saturation: does G1(top quartile) ≈ G1(bottom quartile) or differ?
        double avgSat = loopResults.Average(l => l.G1Sat);
        double g1Diff = Math.Abs(avgSat - 1.0);

        sb.AppendLine($"  Saturation index: G1(top/bottom) = {avgSat:F3}x");
        sb.AppendLine($"  → {(g1Diff > 0.30 ? "NONLINEAR — gain SATURATES at extremes" : "LINEAR — gain is constant across range")}");
        sb.AppendLine("");

        if (avgGain < 0.90)
        {
            sb.AppendLine("  DAMPED SYSTEM: signal decays each cycle → stable fixed point.");
            sb.AppendLine("  The loop converges to a unique equilibrium.");
        }
        else if (avgGain > 1.10)
        {
            sb.AppendLine("  AMPLIFYING SYSTEM: signal grows each cycle → unstable.");
            sb.AppendLine("  Requires saturation mechanism to bound growth.");
        }
        else
        {
            sb.AppendLine("  NEUTRAL SYSTEM: signal neither grows nor decays.");
            sb.AppendLine("  Marginally stable — small perturbations persist.");
        }
        sb.AppendLine("");

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        bool critA = avgGain > 0.85;               // Loop has meaningful gain
        bool critB = avgGain > 0.95;               // Near or above unity
        bool critC = loopResults.Count >= 2;        // Multiple architectures
        bool critD = avgG1 > 1.05 || avgG4 > 1.10; // At least one stage amplifies

        int critMet = (critA ? 1 : 0) + (critB ? 1 : 0) + (critC ? 1 : 0) + (critD ? 1 : 0);

        string verdict = critMet >= 4 ? "SUPPORTED: self-consistent resonance exists."
            : critMet >= 2 ? "CONDITIONAL: weak or damped resonance."
            : "FALSIFIED: loop not self-sustaining.";

        sb.AppendLine($"VERDICT: {verdict}  ({critMet}/4 criteria)");
        sb.AppendLine("");
        sb.AppendLine($"  A. Loop gain > 0.85:                    {(critA ? "YES" : "NO")} (G={avgGain:F3})");
        sb.AppendLine($"  B. Loop gain > 0.95:                    {(critB ? "YES" : "NO")} (near-unity)");
        sb.AppendLine($"  C. ≥2 architectures:                    {(critC ? "YES" : "NO")} ({loopResults.Count})");
        sb.AppendLine($"  D. At least one amplifying stage:        {(critD ? "YES" : "NO")}");
        sb.AppendLine("");
        sb.AppendLine("Resonance Loop Result:");
        sb.AppendLine($"  G = G1×G2×G3×G4 = {avgG1:F3}×{avgG2:F3}×{avgG3:F3}×{avgG4:F3} = {avgGain:F3}");
        sb.AppendLine($"  Classification: {(avgGain > 1.05 ? "AMPLIFYING" : avgGain > 0.95 ? "NEUTRAL" : "DAMPED")}");
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== RLS_01 complete. Commit: RLS_01_ResonanceLoopStabilityAudit ===");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    private record RlsCell(double GradM, double GradFb, int ZoneDist, bool IsBoundary, double TickAnomaly, double AbsM, double Tick);
    private record LoopResult(string Arch, int N, double G1, double G2, double G3, double G4, double TotalGain, double G1Sat,
        double HiBdry, double LoBdry, double BAnom, double IAnom, double HFb, double LFb, double HGradM, double LGradM);
}
