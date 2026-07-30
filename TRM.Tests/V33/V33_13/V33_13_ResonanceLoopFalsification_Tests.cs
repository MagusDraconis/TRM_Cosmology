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

namespace TRM.Tests.V33_13;

[Trait("Category", "V33_13")]
[Trait("Category", "LongRunning")]
public class V33_13_ResonanceLoopFalsification_Tests
{
    private readonly ITestOutputHelper _o;
    public V33_13_ResonanceLoopFalsification_Tests(ITestOutputHelper o) { _o = o; }

    // ====================================================================
    // RLF_01: LOOP GAIN NULL TEST — Is G significantly different from 1?
    //
    // Null:        G = G1·G2·G3·G4 = 1 (chain-rule tautology — no amplification)
    // Alt:         G ≠ 1 (genuine amplification or damping)
    // Observable:  Loop gain G across architectures, with bootstrap confidence
    // Pass (RL falsified):  |G - 1| < ε — loop gain is trivial
    // Fail (RL survives):   |G - 1| > ε with significance — genuine loop
    // ====================================================================
    [Fact]
    public void RLF_01_LoopGainNullTest_IsGDeviatingFromUnity()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== RLF_01: Loop Gain Null Test — Is G ≠ 1? ===");

        var loopResults = ComputeLoopGains();
        if (loopResults.Count < 2) { sb.AppendLine("Insufficient."); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        // Bootstrap: resample cells within each architecture to get G distribution
        const int nBoot = 200;
        var rng = new Random(629471);
        var bootGains = new List<double>();

        // Re-run with bootstrapping at the cell level
        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);

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

            var dist = new int[nG, nG];
            for (int bi = 0; bi < nG; bi++) for (int gi = 0; gi < nG; gi++) dist[bi, gi] = int.MaxValue;
            var q = new Queue<(int, int)>();
            for (int bi = 1; bi < nG - 1; bi++)
                for (int gi = 1; gi < nG - 1; gi++)
                    if (gS[bi, gi] != gS[bi + 1, gi] || gS[bi, gi] != gS[bi - 1, gi] ||
                        gS[bi, gi] != gS[bi, gi + 1] || gS[bi, gi] != gS[bi, gi - 1])
                    { dist[bi, gi] = 0; q.Enqueue((bi, gi)); }
            while (q.Count > 0) { var (bi, gi) = q.Dequeue(); foreach (var (nb, ng) in new[] { (bi + 1, gi), (bi - 1, gi), (bi, gi + 1), (bi, gi - 1) }) if (nb >= 0 && nb < nG && ng >= 0 && ng < nG && dist[nb, ng] == int.MaxValue) { dist[nb, ng] = dist[bi, gi] + 1; q.Enqueue((nb, ng)); } }

            var cells = new List<RlfCell>();
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
                    var nT = new List<double>();
                    if (bi > 0) nT.Add(gTick[bi - 1, gi]); if (bi + 1 < nG) nT.Add(gTick[bi + 1, gi]);
                    if (gi > 0) nT.Add(gTick[bi, gi - 1]); if (gi + 1 < nG) nT.Add(gTick[bi, gi + 1]);
                    double tLocMean = nT.Count > 0 ? nT.Average() : gTick[bi, gi];
                    double tAnom = Math.Abs(gTick[bi, gi] - tLocMean) / Math.Max(1e-15, tLocMean);
                    cells.Add(new RlfCell(gradM, gradFb, d, isBdry, tAnom));
                }

            if (cells.Count < 100) continue;

            // Bootstrap
            for (int b = 0; b < nBoot; b++)
            {
                var bootCells = new List<RlfCell>(cells.Count);
                for (int i = 0; i < cells.Count; i++) bootCells.Add(cells[rng.Next(cells.Count)]);
                double g = ComputeGain(bootCells);
                if (g > 0) bootGains.Add(g);
            }
        }

        if (bootGains.Count < 50) { sb.AppendLine("Insufficient bootstrap samples."); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        var sortedG = bootGains.OrderBy(g => g).ToList();
        double gMedian = sortedG[sortedG.Count / 2];
        double gLo = sortedG[(int)(sortedG.Count * 0.025)];
        double gHi = sortedG[(int)(sortedG.Count * 0.975)];
        double gMean = bootGains.Average();
        double gStd = Math.Sqrt(bootGains.Select(g => (g - gMean) * (g - gMean)).Average());

        sb.AppendLine($"  Bootstrap N = {bootGains.Count}");
        sb.AppendLine($"  G median = {gMedian:F4}");
        sb.AppendLine($"  G 95% CI = [{gLo:F4}, {gHi:F4}]");
        sb.AppendLine($"  G mean ± σ = {gMean:F4} ± {gStd:F4}");
        sb.AppendLine("");

        bool unityInCI = gLo <= 1.0 && gHi >= 1.0;
        bool rlFalsified = unityInCI;
        sb.AppendLine($"  Unity in 95% CI: {(unityInCI ? "YES" : "NO")}");
        sb.AppendLine($"  → {(rlFalsified ? "G NOT significantly different from 1 — loop gain is trivial chain-rule decomposition" : "G significantly different from 1 — genuine amplification/damping")}");
        sb.AppendLine(rlFalsified
            ? "  VERDICT: RL FALSIFIED — G = 1 is not rejected. The 'loop' is joint-distribution decomposition."
            : "  VERDICT: RL SURVIVES — G ≠ 1 with confidence. Genuine amplification exists.");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(rlFalsified, $"RLF_01: G 95% CI = [{gLo:F4}, {gHi:F4}]. RL falsified if unity is in interval.");
    }

    // ====================================================================
    // RLF_02: FEEDBACK LINK COLLINEARITY — Is G4 (∇fb → ∇|m|) trivial?
    //
    // Null:        ∇fb and ∇|m| carry independent gradient information
    // Alt:         ∇fb ≈ ∇|m| — the Feedback→Geometry link is estimator collinearity
    // Observable:  G4 = mean(∇|m| | high ∇fb) / mean(∇|m| | low ∇fb)
    // Pass (RL falsified):  G4 ≈ 1.0 (∇fb adds no ∇|m| amplification)
    // Fail (RL survives):   G4 significantly > 1.0
    // ====================================================================
    [Fact]
    public void RLF_02_FeedbackLinkCollinearity_IsG4Trivial()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== RLF_02: Feedback Link Collinearity — Is G4 (∇fb → ∇|m|) trivial? ===");

        var allCells = new ConcurrentBag<(string arch, double gradM, double gradFb)>();

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);

        foreach (var (arch, fam) in new[] { ("GAN", VcFamily.GAN), ("CNS", VcFamily.CNS) })
        {
            const int nG = 18;
            double bMin = 0.0, bMax = 2.0, gMin = 0.0, gMax = 2.0;
            double db = (bMax - bMin) / (nG - 1), dg = (gMax - gMin) / (nG - 1);

            var gM = new double[nG, nG]; var gFb = new double[nG, nG];
            var gS = new int[nG, nG]; var gTick = new double[nG, nG];

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
                    double dMdB = (gM[bi + 1, gi] - gM[bi - 1, gi]) / (2 * db);
                    double dMdG = (gM[bi, gi + 1] - gM[bi, gi - 1]) / (2 * dg);
                    double dFdB = (gFb[bi + 1, gi] - gFb[bi - 1, gi]) / (2 * db);
                    double dFdG = (gFb[bi, gi + 1] - gFb[bi, gi - 1]) / (2 * dg);
                    allCells.Add((arch, Math.Sqrt(dMdB * dMdB + dMdG * dMdG), Math.Sqrt(dFdB * dFdB + dFdG * dFdG)));
                }
        }

        var all = allCells.ToList();
        if (all.Count < 200) { sb.AppendLine("Insufficient."); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        // Direct ∇fb ↔ ∇|m| correlation
        double[] gmV = all.Select(c => Math.Log10(Math.Max(1e-15, c.gradM))).ToArray();
        double[] gfV = all.Select(c => Math.Log10(Math.Max(1e-15, c.gradFb))).ToArray();
        double rDirect = PearsonCorr(gmV, gfV);

        // G4: ratio of ∇|m| in high-∇fb vs low-∇fb
        double fbMed = all.Select(c => c.gradFb).OrderBy(f => f).ElementAt(all.Count / 2);
        double hiGm = all.Where(c => c.gradFb >= fbMed).Average(c => c.gradM);
        double loGm = all.Where(c => c.gradFb < fbMed).Average(c => c.gradM);
        double g4 = hiGm / Math.Max(1e-15, loGm);

        // What G4 would be expected if ∇fb and ∇|m| are collinear with noise?
        // If ∇|m| = a·∇fb + noise, then G4 depends on the signal-to-noise ratio.
        // For collinear estimators, G4 should approach the ratio of means of the conditioning variable.
        double fbHiMean = all.Where(c => c.gradFb >= fbMed).Average(c => c.gradFb);
        double fbLoMean = all.Where(c => c.gradFb < fbMed).Average(c => c.gradFb);
        double expectedG4FromCollinearity = fbHiMean / Math.Max(1e-15, fbLoMean);

        sb.AppendLine($"  ∇fb ↔ ∇|m| direct r:   {rDirect:F4}  R² = {rDirect*rDirect:F4}");
        sb.AppendLine($"  G4 (observed):          {g4:F4}x");
        sb.AppendLine($"  G4 (collinearity null): {expectedG4FromCollinearity:F4}x");
        sb.AppendLine($"  G4 / CollinearityNull:  {g4/expectedG4FromCollinearity:F4}x");
        sb.AppendLine("");

        // G4 trivial if the observed ratio is explained by collinearity
        double excessRatio = g4 / Math.Max(1e-15, expectedG4FromCollinearity);
        bool rlFalsified = Math.Abs(excessRatio - 1.0) < 0.15;
        sb.AppendLine($"  → {(rlFalsified ? "G4 IS EXPLAINED by estimator collinearity — no feedback amplification" : "G4 EXCEEDS collinearity expectation — genuine amplification")}");
        sb.AppendLine(rlFalsified
            ? "  VERDICT: RL FALSIFIED — the Feedback→Geometry link is estimator collinearity, not causal"
            : "  VERDICT: RL SURVIVES — ∇fb amplifies ∇|m| beyond what collinearity predicts");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(rlFalsified, $"RLF_02: G4={g4:F3}x, expected from collinearity={expectedG4FromCollinearity:F3}x. RL falsified if no excess amplification.");
    }

    // ====================================================================
    // RLF_03: DIRECTIONALITY ASYMMETRY — Is there genuine causal asymmetry?
    //
    // Null:        ∇|m| → tick and tick → ∇|m| are symmetric
    // Alt:         One direction dominates (causal asymmetry)
    // Observable:  |r(∇|m|, tick_anomaly)| vs |r(tick_anomaly, ∇fb)|
    //              Using partial correlation to isolate direct paths
    // Pass (RL falsified):  No asymmetry — correlations are symmetric
    // Fail (RL survives):   Significant asymmetry with correct loop direction
    // ====================================================================
    [Fact]
    public void RLF_03_DirectionalityAsymmetry_IsDirectionDetectable()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== RLF_03: Directionality Asymmetry — Is causal direction detectable? ===");

        var data = CollectRlfGridData();
        if (data.Count < 200) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        double[] gmV = data.Select(c => Math.Log10(Math.Max(1e-15, c.GradM))).ToArray();
        double[] gfV = data.Select(c => Math.Log10(Math.Max(1e-15, c.GradFb))).ToArray();
        double[] taV = data.Select(c => c.TickAnomaly).ToArray();
        double[] bdV = data.Select(c => (double)c.ZoneDist).ToArray();

        // Forward path: ∇|m| → TickAnomaly (partial, controlling for boundary)
        double r_forward = PartialCorrelation(gmV, taV, bdV);

        // Reverse path: TickAnomaly → ∇fb (partial, controlling for boundary)
        double r_reverse = PartialCorrelation(taV, gfV, bdV);

        // Cross-path: does ∇|m| → ∇fb go through TickAnomaly or directly?
        double r_direct = PartialCorrelation(gmV, gfV, bdV);
        double r_mediated = PartialCorrelation(gmV, gfV, taV); // controlling for TickAnomaly

        double mediationDrop = r_direct - r_mediated;

        sb.AppendLine($"  Forward (∇|m| → TickAnom | bd):      r = {r_forward:F4}");
        sb.AppendLine($"  Reverse (TickAnom → ∇fb | bd):        r = {r_reverse:F4}");
        sb.AppendLine($"  Direct   (∇|m| → ∇fb | bd):           r = {r_direct:F4}");
        sb.AppendLine($"  Mediated (∇|m| → ∇fb | bd,TickAnom):  r = {r_mediated:F4}");
        sb.AppendLine($"  Mediation drop:                       {mediationDrop:F4}");
        sb.AppendLine("");

        double asymmetry = Math.Abs(Math.Abs(r_forward) - Math.Abs(r_reverse));
        bool rlFalsified = asymmetry < 0.10 && Math.Abs(mediationDrop) < 0.10;
        sb.AppendLine($"  Asymmetry |r_fwd| - |r_rev|: {asymmetry:F4}");
        sb.AppendLine($"  → {(rlFalsified ? "NO ASYMMETRY — correlations are symmetric, no causal direction detected" : "ASYMMETRY DETECTED — one direction dominates")}");
        sb.AppendLine(rlFalsified
            ? "  VERDICT: RL FALSIFIED — cross-sectional correlations have no causal direction information"
            : "  VERDICT: RL SURVIVES — directional asymmetry exists in the expected loop orientation");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(rlFalsified, $"RLF_03: Asymmetry = {asymmetry:F4}, mediation drop = {mediationDrop:F4}. RL falsified if no directional asymmetry.");
    }

    // ====================================================================
    // RLF_04: TEMPORAL PROXY TEST — Does the loop operate in time?
    //
    // Null:        The "loop" is a static spatial correlation in (β,γ)
    // Alt:         The loop requires temporal dynamics — spatial proxy fails
    // Observable:  Can the loop be reconstructed from a single static scan?
    //              (Use α as time-proxy: does the loop structure change along α?)
    // Pass (RL falsified):  α-variation does not change loop structure
    // Fail (RL survives):   Loop structure varies systematically with α (temporal proxy)
    // ====================================================================
    [Fact]
    public void RLF_04_TemporalProxyTest_DoesLoopRequireTime()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== RLF_04: Temporal Proxy Test — Is the loop static or dynamic? ===");
        sb.AppendLine("=== Compare loop metrics at low-α vs high-α (α as temporal proxy) ===");

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();

        var alphaGains = new ConcurrentBag<(double alpha, double gain)>();

        foreach (var (arch, fam) in new[] { ("GAN", VcFamily.GAN), ("CNS", VcFamily.CNS) })
        {
            const int nG = 16;
            double bMin = 0.0, bMax = 2.0, gMin = 0.0, gMax = 2.0;
            double db = (bMax - bMin) / (nG - 1), dg = (gMax - gMin) / (nG - 1);

            // Test at 3 different α values
            double[] alphaTest = { 0.30, 0.70, 1.20 };

            foreach (double alpha in alphaTest)
            {
                var gM = new double[nG, nG]; var gS = new int[nG, nG];
                var gFb = new double[nG, nG]; var gTick = new double[nG, nG];

                Parallel.For(0, nG, bi =>
                {
                    double beta = bMin + db * bi;
                    for (int gi = 0; gi < nG; gi++)
                    {
                        double gamma = gMin + dg * gi;
                        var f = ComputeFull(fam, 1.0, 1.0, alpha, beta, gamma, distances, sortedD, xiBase, k0Base, 21, 0.21, (1.40 - 0.21) / 20.0);
                        gM[bi, gi] = f.absM; gS[bi, gi] = f.sign; gFb[bi, gi] = f.fb; gTick[bi, gi] = f.tick;
                    }
                });

                var dist = new int[nG, nG];
                for (int bi = 0; bi < nG; bi++) for (int gi = 0; gi < nG; gi++) dist[bi, gi] = int.MaxValue;
                var q = new Queue<(int, int)>();
                for (int bi = 1; bi < nG - 1; bi++)
                    for (int gi = 1; gi < nG - 1; gi++)
                        if (gS[bi, gi] != gS[bi + 1, gi] || gS[bi, gi] != gS[bi - 1, gi] ||
                            gS[bi, gi] != gS[bi, gi + 1] || gS[bi, gi] != gS[bi, gi - 1])
                        { dist[bi, gi] = 0; q.Enqueue((bi, gi)); }
                while (q.Count > 0) { var (bi, gi) = q.Dequeue(); foreach (var (nb, ng) in new[] { (bi + 1, gi), (bi - 1, gi), (bi, gi + 1), (bi, gi - 1) }) if (nb >= 0 && nb < nG && ng >= 0 && ng < nG && dist[nb, ng] == int.MaxValue) { dist[nb, ng] = dist[bi, gi] + 1; q.Enqueue((nb, ng)); } }

                var cells = new List<RlfCell>();
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
                        var nT = new List<double>();
                        if (bi > 0) nT.Add(gTick[bi - 1, gi]); if (bi + 1 < nG) nT.Add(gTick[bi + 1, gi]);
                        if (gi > 0) nT.Add(gTick[bi, gi - 1]); if (gi + 1 < nG) nT.Add(gTick[bi, gi + 1]);
                        double tLocMean = nT.Count > 0 ? nT.Average() : gTick[bi, gi];
                        double tAnom = Math.Abs(gTick[bi, gi] - tLocMean) / Math.Max(1e-15, tLocMean);
                        cells.Add(new RlfCell(gradM, gradFb, d, isBdry, tAnom));
                    }

                if (cells.Count >= 100)
                {
                    double g = ComputeGain(cells);
                    alphaGains.Add((alpha, g));
                }
            }
        }

        var results = alphaGains.ToList();
        if (results.Count < 6) { sb.AppendLine("Insufficient α-variation data."); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        var byAlpha = results.GroupBy(r => r.alpha).OrderBy(g => g.Key).ToList();
        sb.AppendLine($"{"α",8} {"G mean",8} {"G std",8} {"N",5}");
        sb.AppendLine(new string('-', 32));
        foreach (var grp in byAlpha)
        {
            double gMean = grp.Average(r => r.gain);
            double gStd = Math.Sqrt(grp.Select(r => (r.gain - gMean) * (r.gain - gMean)).Average());
            sb.AppendLine($"{grp.Key,8:F2} {gMean,8:F4} {gStd,8:F4} {grp.Count(),5}");
        }
        sb.AppendLine("");

        // Test: does G vary with α?
        double[] aVals = results.Select(r => r.alpha).ToArray();
        double[] gVals = results.Select(r => r.gain).ToArray();
        double r_alphaGain = PearsonCorr(aVals, gVals);

        bool rlFalsified = Math.Abs(r_alphaGain) < 0.30;
        sb.AppendLine($"  r(G, α) = {r_alphaGain:F4}");
        sb.AppendLine($"  → {(rlFalsified ? "G is STABLE across α — loop structure is static, not dynamical" : "G VARIES with α — loop structure has temporal dependence")}");
        sb.AppendLine(rlFalsified
            ? "  VERDICT: RL FALSIFIED — the 'loop' is a static parameter-space correlation, not a temporal feedback loop"
            : "  VERDICT: RL SURVIVES — loop structure varies with α, consistent with temporal dynamics");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(rlFalsified, $"RLF_04: r(G,α) = {r_alphaGain:F4}. RL falsified if loop structure is static.");
    }

    // ====================================================================
    // RLF_05: INTERVENTION SIMULATION — Counterfactual test without boundary
    //
    // Null:        Removing boundary structure should NOT affect G-F-T correlations
    //              (if CC is correct — correlations are from (β,γ) directly)
    // Alt:         Removing boundary structure SHOULD affect G-F-T correlations
    //              (if RL is correct — boundary mediates the loop)
    // Observable:  Compare correlations in boundary vs interior cells
    //              Compare full grid vs interior-only grid
    // Pass (RL falsified):  Interior-only correlations ≈ full-grid correlations
    // Fail (RL survives):   Interior-only correlations differ significantly
    // ====================================================================
    [Fact]
    public void RLF_05_InterventionSimulation_RemoveBoundary()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== RLF_05: Intervention Simulation — Remove Boundary Cells ===");

        var data = CollectRlfGridData();
        if (data.Count < 200) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        double[] gmV = data.Select(c => Math.Log10(Math.Max(1e-15, c.GradM))).ToArray();
        double[] gfV = data.Select(c => Math.Log10(Math.Max(1e-15, c.GradFb))).ToArray();
        double[] taV = data.Select(c => c.TickAnomaly).ToArray();

        // Full grid
        double rGF_full = PearsonCorr(gmV, gfV);
        double rGT_full = PearsonCorr(gmV, taV);
        double rFT_full = PearsonCorr(gfV, taV);

        // Interior only (zone ≥ 2)
        var intIdx = new List<int>();
        for (int i = 0; i < data.Count; i++) if (data[i].ZoneDist >= 2) intIdx.Add(i);
        double[] iGm = intIdx.Select(i => gmV[i]).ToArray();
        double[] iGf = intIdx.Select(i => gfV[i]).ToArray();
        double[] iTa = intIdx.Select(i => taV[i]).ToArray();

        double rGF_int = PearsonCorr(iGm, iGf);
        double rGT_int = PearsonCorr(iGm, iTa);
        double rFT_int = PearsonCorr(iGf, iTa);

        // Random subset of same size as interior (control for sample size)
        var rng = new Random(629471);
        var randIdx = new HashSet<int>();
        while (randIdx.Count < intIdx.Count) randIdx.Add(rng.Next(data.Count));
        double[] rGm = randIdx.Select(i => gmV[i]).ToArray();
        double[] rGf = randIdx.Select(i => gfV[i]).ToArray();
        double[] rTa = randIdx.Select(i => taV[i]).ToArray();

        double rGF_rand = PearsonCorr(rGm, rGf);
        double rGT_rand = PearsonCorr(rGm, rTa);
        double rFT_rand = PearsonCorr(rGf, rTa);

        sb.AppendLine($"{"Correlation",-22} {"Full",8} {"Interior",8} {"Random",8} {"Δ(Full-Int)",12}");
        sb.AppendLine(new string('-', 62));
        sb.AppendLine($"{"∇|m| ↔ ∇fb",-22} {rGF_full,8:F4} {rGF_int,8:F4} {rGF_rand,8:F4} {rGF_full-rGF_int,12:F4}");
        sb.AppendLine($"{"∇|m| ↔ TickAnom",-22} {rGT_full,8:F4} {rGT_int,8:F4} {rGT_rand,8:F4} {rGT_full-rGT_int,12:F4}");
        sb.AppendLine($"{"∇fb ↔ TickAnom",-22} {rFT_full,8:F4} {rFT_int,8:F4} {rFT_rand,8:F4} {rFT_full-rFT_int,12:F4}");
        sb.AppendLine("");

        double avgDrop = (Math.Abs(rGF_full - rGF_int) + Math.Abs(rGT_full - rGT_int) + Math.Abs(rFT_full - rFT_int)) / 3.0;
        bool rlFalsified = avgDrop < 0.10;

        sb.AppendLine($"  Average correlation drop: {avgDrop:F4}");
        sb.AppendLine($"  → {(rlFalsified ? "Correlations PERSIST without boundary — loop does not require boundary mediation" : "Correlations DROP without boundary — boundary is a necessary mediator")}");
        sb.AppendLine(rlFalsified
            ? "  VERDICT: RL FALSIFIED — G-F-T coupling is insensitive to boundary removal. Loop not boundary-mediated."
            : "  VERDICT: RL SURVIVES — boundary is a structural requirement for the loop correlations");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(rlFalsified, $"RLF_05: Avg correlation drop = {avgDrop:F4}. RL falsified if coupling persists without boundary.");
    }

    // ====================================================================
    // RLF_06: SATURATION LINEARITY — Is loop gain linear or does it saturate?
    //
    // Null:        Gain is constant across gradient magnitude (linear)
    // Alt:         Gain saturates at extremes (nonlinear, attractor-like)
    // Observable:  G computed within quartiles of gradient strength
    // Pass (RL falsified):  G constant across quartiles — no attractor structure
    // Fail (RL survives):   G varies across quartiles — attractor behavior
    // ====================================================================
    [Fact]
    public void RLF_06_SaturationLinearity_DoesGainSaturate()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== RLF_06: Saturation Linearity — Does loop gain saturate at extremes? ===");

        var data = CollectRlfGridData();
        if (data.Count < 200) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        // Sort by gradient magnitude, split into quartiles
        var sorted = data.OrderBy(c => c.GradM).ToList();
        int qN = sorted.Count / 4;
        var quartiles = new[] {
            sorted.Take(qN).ToList(),
            sorted.Skip(qN).Take(qN).ToList(),
            sorted.Skip(2*qN).Take(qN).ToList(),
            sorted.Skip(3*qN).ToList()
        };

        sb.AppendLine($"{"Quartile",-12} {"GradM range",16} {"G1",8} {"G2",8} {"G3",8} {"G4",8} {"G",8} {"N",5}");
        sb.AppendLine(new string('-', 75));

        for (int qi = 0; qi < quartiles.Length; qi++)
        {
            var q = quartiles[qi];
            if (q.Count < 20) continue;
            double gMin = q.Min(c => c.GradM);
            double gMax = q.Max(c => c.GradM);
            var cells = q.Select(c => new RlfCell(c.GradM, c.GradFb, c.ZoneDist, c.IsBoundary, c.TickAnomaly)).ToList();

            double g1 = ComputeG1(cells);
            double g2 = ComputeG2(cells);
            double g3 = ComputeG3(cells);
            double g4 = ComputeG4(cells);
            double g = g1 * g2 * g3 * g4;

            sb.AppendLine($"{"Q" + (qi+1),-12} [{gMin:F3},{gMax:F3}] {g1,8:F3}x {g2,8:F3}x {g3,8:F3}x {g4,8:F3}x {g,8:F3}x {q.Count,5}");
        }
        sb.AppendLine("");

        // Test: is G(Q4) / G(Q1) ≈ 1?
        var q1Cells = quartiles[0].Select(c => new RlfCell(c.GradM, c.GradFb, c.ZoneDist, c.IsBoundary, c.TickAnomaly)).ToList();
        var q4Cells = quartiles[3].Select(c => new RlfCell(c.GradM, c.GradFb, c.ZoneDist, c.IsBoundary, c.TickAnomaly)).ToList();
        double g1Total = ComputeGain(q1Cells);
        double g4Total = ComputeGain(q4Cells);
        double saturationRatio = g4Total / Math.Max(1e-15, g1Total);

        bool rlFalsified = Math.Abs(saturationRatio - 1.0) < 0.30;
        sb.AppendLine($"  G(Q4) / G(Q1) = {saturationRatio:F3}x");
        sb.AppendLine($"  → {(rlFalsified ? "GAIN IS CONSTANT — no saturation, no attractor" : "GAIN SATURATES — nonlinear attractor behavior")}");
        sb.AppendLine(rlFalsified
            ? "  VERDICT: RL FALSIFIED — gain is linear, no attractor dynamics"
            : "  VERDICT: RL SURVIVES — gain saturates, consistent with attractor");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(rlFalsified, $"RLF_06: Saturation ratio = {saturationRatio:F3}x. RL falsified if gain is constant across quartiles.");
    }

    // ====================================================================
    // HELPERS
    // ====================================================================

    private static double ComputeGain(List<RlfCell> cells)
    {
        double g1 = ComputeG1(cells);
        double g2 = ComputeG2(cells);
        double g3 = ComputeG3(cells);
        double g4 = ComputeG4(cells);
        return g1 * g2 * g3 * g4;
    }

    private static double ComputeG1(List<RlfCell> cells)
    {
        double med = cells.Select(c => c.GradM).OrderBy(g => g).ElementAt(cells.Count / 2);
        double hi = 100.0 * cells.Where(c => c.GradM >= med).Count(c => c.IsBoundary) / Math.Max(1, cells.Count(c => c.GradM >= med));
        double lo = 100.0 * cells.Where(c => c.GradM < med).Count(c => c.IsBoundary) / Math.Max(1, cells.Count(c => c.GradM < med));
        return hi / Math.Max(1e-15, lo);
    }

    private static double ComputeG2(List<RlfCell> cells)
    {
        double bAnom = cells.Where(c => c.IsBoundary).DefaultIfEmpty().Average(c => c?.TickAnomaly ?? 0);
        double iAnom = cells.Where(c => c.ZoneDist >= 3).DefaultIfEmpty().Average(c => c?.TickAnomaly ?? 0);
        return bAnom / Math.Max(1e-15, iAnom);
    }

    private static double ComputeG3(List<RlfCell> cells)
    {
        double med = cells.Select(c => c.TickAnomaly).OrderBy(a => a).ElementAt(cells.Count / 2);
        double hi = cells.Where(c => c.TickAnomaly >= med).Average(c => c.GradFb);
        double lo = cells.Where(c => c.TickAnomaly < med).Average(c => c.GradFb);
        return hi / Math.Max(1e-15, lo);
    }

    private static double ComputeG4(List<RlfCell> cells)
    {
        double med = cells.Select(c => c.GradFb).OrderBy(f => f).ElementAt(cells.Count / 2);
        double hi = cells.Where(c => c.GradFb >= med).Average(c => c.GradM);
        double lo = cells.Where(c => c.GradFb < med).Average(c => c.GradM);
        return hi / Math.Max(1e-15, lo);
    }

    private List<LoopResult> ComputeLoopGains()
    {
        var results = new List<LoopResult>();
        var data = CollectRlfGridData();
        foreach (var grp in data.GroupBy(c => c.Arch))
        {
            var cells = grp.Select(c => new RlfCell(c.GradM, c.GradFb, c.ZoneDist, c.IsBoundary, c.TickAnomaly)).ToList();
            if (cells.Count < 100) continue;
            double g1 = ComputeG1(cells); double g2 = ComputeG2(cells);
            double g3 = ComputeG3(cells); double g4 = ComputeG4(cells);
            results.Add(new LoopResult(grp.Key, cells.Count, g1, g2, g3, g4));
        }
        return results;
    }

    private List<RlfGridCell> CollectRlfGridData()
    {
        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);
        var allCells = new ConcurrentBag<RlfGridCell>();

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

            var dist = new int[nG, nG];
            for (int bi = 0; bi < nG; bi++) for (int gi = 0; gi < nG; gi++) dist[bi, gi] = int.MaxValue;
            var q = new Queue<(int, int)>();
            for (int bi = 1; bi < nG - 1; bi++)
                for (int gi = 1; gi < nG - 1; gi++)
                    if (gS[bi, gi] != gS[bi + 1, gi] || gS[bi, gi] != gS[bi - 1, gi] ||
                        gS[bi, gi] != gS[bi, gi + 1] || gS[bi, gi] != gS[bi, gi - 1])
                    { dist[bi, gi] = 0; q.Enqueue((bi, gi)); }
            while (q.Count > 0) { var (bi, gi) = q.Dequeue(); foreach (var (nb, ng) in new[] { (bi + 1, gi), (bi - 1, gi), (bi, gi + 1), (bi, gi - 1) }) if (nb >= 0 && nb < nG && ng >= 0 && ng < nG && dist[nb, ng] == int.MaxValue) { dist[nb, ng] = dist[bi, gi] + 1; q.Enqueue((nb, ng)); } }

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
                    var nT = new List<double>();
                    if (bi > 0) nT.Add(gTick[bi - 1, gi]); if (bi + 1 < nG) nT.Add(gTick[bi + 1, gi]);
                    if (gi > 0) nT.Add(gTick[bi, gi - 1]); if (gi + 1 < nG) nT.Add(gTick[bi, gi + 1]);
                    double tLocMean = nT.Count > 0 ? nT.Average() : gTick[bi, gi];
                    double tAnom = Math.Abs(gTick[bi, gi] - tLocMean) / Math.Max(1e-15, tLocMean);
                    allCells.Add(new RlfGridCell(arch, gradM, gradFb, d, isBdry, tAnom));
                }
        }
        return allCells.ToList();
    }

    private static double PearsonCorr(double[] xs, double[] ys)
    {
        int n = xs.Length; if (n < 2) return 0;
        double mx = xs.Average(), my = ys.Average();
        double sxy = 0, sxx = 0, syy = 0;
        for (int i = 0; i < n; i++) { double dx = xs[i] - mx, dy = ys[i] - my; sxy += dx * dy; sxx += dx * dx; syy += dy * dy; }
        return sxx > 1e-15 && syy > 1e-15 ? sxy / Math.Sqrt(sxx * syy) : 0;
    }

    private static double PartialCorrelation(double[] x, double[] y, double[] z)
    {
        double rxy = PearsonCorr(x, y), rxz = PearsonCorr(x, z), ryz = PearsonCorr(y, z);
        double denom = (1 - rxz * rxz) * (1 - ryz * ryz);
        return denom > 1e-15 ? (rxy - rxz * ryz) / Math.Sqrt(denom) : 0;
    }

    private record RlfCell(double GradM, double GradFb, int ZoneDist, bool IsBoundary, double TickAnomaly);
    private record RlfGridCell(string Arch, double GradM, double GradFb, int ZoneDist, bool IsBoundary, double TickAnomaly);
    private record LoopResult(string Arch, int N, double G1, double G2, double G3, double G4);
}
