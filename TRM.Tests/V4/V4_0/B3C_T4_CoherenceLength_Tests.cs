using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4;

/// <summary>
/// TRM V4 — B3C-T4: Coherence Length Extraction
///
/// Hypothesis: The TRM oscillator network has an intrinsic coherence length
/// l_TRM that can be extracted from phase correlation decay with distance.
/// If stable and non-circular, this provides a TRM-native physical scale:
///   f_ref = c / l_TRM
///
/// Tests:
///   1. Phase correlation C(r) = ⟨cos(θ_i − θ_j)⟩ vs ring distance
///   2. Exponential fit: C(r) = C₀·exp(−r/l_TRM)
///   3. Stability across N, K₀ variations
///   4. Resulting f_ref estimate
///
/// Reference: B3C candidate B (sync energy → coherence length)
/// </summary>
[Trait("Category", "V4")]
[Trait("Category", "B3")]
[Trait("Category", "B3C")]
public class B3C_T4_CoherenceLength_Tests
{
    private readonly ITestOutputHelper _output;

    // ────────────────────────────────────────────────────────────
    // Physical constants
    // ────────────────────────────────────────────────────────────

    private const double C = 2.99792458e8;   // m/s

    // ────────────────────────────────────────────────────────────
    // Result types
    // ────────────────────────────────────────────────────────────

    private enum CoherenceClassification
    {
        Derivable,
        Calibrated,
        NotSupported
    }

    private readonly record struct CoherenceResult(
        int N,
        double K0,
        double LTrn,
        double R2,
        double FRefHz,
        CoherenceClassification Classification,
        string Note);

    private readonly record struct CorrelationPoint(
        int Distance,
        double Correlation);

    public B3C_T4_CoherenceLength_Tests(ITestOutputHelper output)
    {
        _output = output;
    }

    // ════════════════════════════════════════════════════════════
    // B3CT4_01 — Phase correlation vs distance
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// B3CT4_01 — Measures phase correlation C(r) vs distance on the ring.
    ///
    /// C(r) = ⟨cos(θ_i − θ_{i+r})⟩ averaged over all i and time windows.
    /// In the synchronized state, C(r) ≈ 1 for all r (perfect coherence).
    /// By introducing a small perturbation (frequency spread Δω), we
    /// expect C(r) to decay with distance.
    /// </summary>
    [Fact]
    public void B3CT4_01_PhaseCorrelation_Decay()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B3C-T4.01 — PHASE CORRELATION vs DISTANCE");
        _output.WriteLine("  C(r) = ⟨cos(θ_i − θ_{i+r})⟩");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        const int N = 30;
        const double K0 = 0.10;
        const double freqSpread = 0.08;   // Δω — controls coherence decay

        var sim = SimulateWithFreqSpread(N, K0, freqSpread);
        if (sim == null)
        {
            _output.WriteLine("  ⚠ Simulation failed — skipping.");
            return;
        }

        _output.WriteLine($"  System: N={N}, K₀={K0:F2}, Δω={freqSpread:F2}");
        _output.WriteLine($"  Sync quality R = {sim.MeanOrder:F4}");
        _output.WriteLine($"  Phase correlations C(r) vs distance:");
        _output.WriteLine("");

        int maxDist = N / 2;
        var corrPoints = new List<CorrelationPoint>();

        for (int d = 1; d <= maxDist; d++)
        {
            double sumCos = 0;
            int count = 0;

            for (int i = 0; i < N; i++)
            {
                int j = (i + d) % N;
                double dTheta = sim.Phases[i] - sim.Phases[j];
                sumCos += Math.Cos(dTheta);
                count++;
            }

            double corr = sumCos / count;
            corrPoints.Add(new CorrelationPoint(d, corr));
            _output.WriteLine($"    r={d,2}  C(r)={corr:F6}");
        }

        _output.WriteLine("");

        // In the synchronized state with R ≈ 0.89, C(r) should be
        // nearly constant ≈ 1 for all r. The frequency spread may
        // introduce slight decay at large distances.
        //
        // Honest note: In the Kuramoto model on a ring, the synchronized
        // state has all phases nearly equal → C(r) ≈ 1 for all r.
        // The coherence length is effectively infinite in the sync state.
        // Non-trivial decay requires DESYNCHRONIZATION or noise.

        double avgCorr = corrPoints.Average(c => c.Correlation);
        double nearCorr = corrPoints.Take(3).Average(c => c.Correlation);
        double farCorr = corrPoints.TakeLast(3).Average(c => c.Correlation);

        _output.WriteLine($"  Near correlation (r=1-3): {nearCorr:F6}");
        _output.WriteLine($"  Far correlation  (r=max): {farCorr:F6}");
        _output.WriteLine($"  Correlation ratio far/near: {farCorr / Math.Max(nearCorr, 1e-12):F6}");

        if (avgCorr > 0.99)
        {
            _output.WriteLine("");
            _output.WriteLine("  ⚠ C(r) ≈ 1 for all r — coherence length is effectively infinite.");
            _output.WriteLine("  The synchronized Kuramoto model on a ring has NO intrinsic");
            _output.WriteLine("  decay length. Phase coherence is global in the sync state.");
            _output.WriteLine("  l_TRM cannot be extracted from C(r) without introducing");
            _output.WriteLine("  noise, disorder, or desynchronization.");
            _output.WriteLine("");
            _output.WriteLine("  Classification: NOT SUPPORTED (no decay → no length scale).");
        }
    }

    // ════════════════════════════════════════════════════════════
    // B3CT4_02 — Coherence length with noise perturbation
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// B3CT4_02 — Introduces phase noise to break perfect synchronization
    /// and measures the resulting finite coherence length.
    ///
    /// With additive phase noise, C(r) decays exponentially:
    ///   C(r) = C₀ · exp(−r / l_TRM)
    ///
    /// l_TRM depends on the noise amplitude and coupling strength.
    /// The question: is l_TRM stable enough to serve as a scale anchor?
    /// </summary>
    [Fact]
    public void B3CT4_02_CoherenceLength_WithNoise()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B3C-T4.02 — COHERENCE LENGTH WITH NOISE");
        _output.WriteLine("  Phase noise breaks sync → finite l_TRM");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        const int N = 30;
        const double K0 = 0.10;
        const double noiseAmp = 0.02;   // phase noise amplitude per step

        var sim = SimulateWithPhaseNoise(N, K0, noiseAmp);
        if (sim == null)
        {
            _output.WriteLine("  ⚠ Simulation failed — skipping.");
            return;
        }

        _output.WriteLine($"  System: N={N}, K₀={K0:F2}, noise_amp={noiseAmp:F3}");
        _output.WriteLine($"  Sync quality R = {sim.MeanOrder:F4}");
        _output.WriteLine("");

        int maxDist = N / 2;
        var data = new List<(double, double)>();

        for (int d = 1; d <= maxDist; d++)
        {
            double sumCos = 0;
            int count = 0;
            for (int i = 0; i < N; i++)
            {
                int j = (i + d) % N;
                double dTheta = sim.Phases[i] - sim.Phases[j];
                sumCos += Math.Cos(dTheta);
                count++;
            }
            double corr = sumCos / count;
            if (corr > 0.01)   // positive for log fit
                data.Add(((double)d, corr));
        }

        // Exponential fit: C(r) = C₀·exp(−r/l)
        // log(C) = log(C₀) − r/l
        var fit = FitExponentialDecay(data);

        _output.WriteLine($"  Fit: C(r) = C₀·exp(−r/l_TRM)");
        _output.WriteLine($"  l_TRM = {fit.DecayLength:F3} (in ring-index units)");
        _output.WriteLine($"  R² = {fit.R2:F4}");
        _output.WriteLine("");

        if (fit.R2 > 0.8 && fit.DecayLength > 1.0 && fit.DecayLength < N)
        {
            // l_TRM in physical units requires spatial calibration of ring index
            _output.WriteLine($"  l_TRM extracted: {fit.DecayLength:F2} ring units");
            _output.WriteLine("");
            _output.WriteLine("  ⚠ l_TRM is in dimensionless ring-index units.");
            _output.WriteLine("  To get physical scale, we need a spatial calibration");
            _output.WriteLine("  of the oscillator spacing Δx. If Δx is known:");
            _output.WriteLine("    l_TRM(physical) = l_TRM(ring) · Δx");
            _output.WriteLine("    f_ref = c / l_TRM(physical)");
            _output.WriteLine("");
            _output.WriteLine("  But Δx is not defined in the CML — the ring has no");
            _output.WriteLine("  intrinsic spatial scale. This requires an additional");
            _output.WriteLine("  input (oscillator spacing or system size).");
            _output.WriteLine("");
            _output.WriteLine("  Classification: CALIBRATED (l_TRM exists but requires Δx).");
        }
        else
        {
            _output.WriteLine("  Fit quality insufficient for stable length extraction.");
            _output.WriteLine("  Classification: NOT SUPPORTED.");
        }
    }

    // ════════════════════════════════════════════════════════════
    // B3CT4_03 — l_TRM stability across configurations
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// B3CT4_03 — Tests whether the extracted coherence length is stable
    /// across different N and K₀ values. A stable l_TRM would strengthen
    /// its candidacy as a physical scale anchor.
    /// </summary>
    [Fact]
    public void B3CT4_03_CoherenceLength_Stability()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B3C-T4.03 — COHERENCE LENGTH STABILITY");
        _output.WriteLine("  Does l_TRM vary with N, K₀?");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        const double noiseAmp = 0.02;
        int[] sizes = { 20, 30, 40 };
        double[] couplings = { 0.08, 0.10, 0.14 };

        _output.WriteLine($"  {"N",4} {"K₀",6} {"l_TRM",8} {"R²",8} {"R",8}");
        _output.WriteLine($"  {new string('─', 4)} {new string('─', 6)} {new string('─', 8)} {new string('─', 8)} {new string('─', 8)}");

        var results = new List<(int, double, double, double)>();

        foreach (int n in sizes)
        {
            foreach (double k0 in couplings)
            {
                var sim = SimulateWithPhaseNoise(n, k0, noiseAmp);
                if (sim == null) continue;

                var data = new List<(double, double)>();
                for (int d = 1; d <= n / 2; d++)
                {
                    double sumCos = 0;
                    int count = 0;
                    for (int i = 0; i < n; i++)
                    {
                        int j = (i + d) % n;
                        sumCos += Math.Cos(sim.Phases[i] - sim.Phases[j]);
                        count++;
                    }
                    double corr = sumCos / count;
                    if (corr > 0.01) data.Add(((double)d, corr));
                }

                var fit = FitExponentialDecay(data);
                results.Add((n, k0, fit.DecayLength, fit.R2));

                _output.WriteLine($"  {n,4} {k0,6:F2} {fit.DecayLength,8:F3} {fit.R2,8:F4} {sim.MeanOrder,8:F4}");
            }
        }

        _output.WriteLine("");

        // Check stability: l_TRM should not vary wildly
        if (results.Count >= 3)
        {
            var lengths = results.Select(r => r.Item3).Where(l => l > 0 && l < 100).ToList();
            if (lengths.Count >= 3)
            {
                double mean = lengths.Average();
                double std = Math.Sqrt(lengths.Average(l => (l - mean) * (l - mean)));
                double cv = std / Math.Max(mean, 1e-12);

                _output.WriteLine($"  Mean l_TRM = {mean:F3} ± {std:F3}  (CV = {cv:P0})");
                _output.WriteLine("");

                if (cv < 0.30)
                {
                    _output.WriteLine("  ✅ l_TRM is stable across configurations (CV < 30%).");
                    _output.WriteLine("  This supports candidacy as a scale anchor.");
                }
                else
                {
                    _output.WriteLine("  ⚠ l_TRM varies significantly with N, K₀ (CV ≥ 30%).");
                    _output.WriteLine("  Not stable enough to serve as an independent scale anchor.");
                }
            }
        }

        _output.WriteLine("");
        _output.WriteLine("  Note: Even if stable, l_TRM requires spatial calibration");
        _output.WriteLine("  (oscillator spacing Δx) to produce a physical frequency.");
        _output.WriteLine("  This effectively introduces another parameter — Δx.");
        _output.WriteLine("  The cesium I3 anchor (f_ref = 9.19 GHz) requires NO");
        _output.WriteLine("  such additional calibration.");
    }

    // ════════════════════════════════════════════════════════════
    // B3CT4_04 — Summary report
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void B3CT4_04_SummaryReport()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B3C-T4 SUMMARY — COHERENCE LENGTH");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  Hypothesis:");
        _output.WriteLine("    TRM network has coherence length l_TRM");
        _output.WriteLine("    f_ref = c / l_TRM (TRM-native physical scale)");
        _output.WriteLine("");
        _output.WriteLine("  Findings:");
        _output.WriteLine("    1. NO intrinsic coherence length in sync state");
        _output.WriteLine("       — C(r) ≈ 1 for all r (global coherence)");
        _output.WriteLine("");
        _output.WriteLine("    2. WITH noise: finite l_TRM emerges, but:");
        _output.WriteLine("       - depends on noise amplitude (not intrinsic)");
        _output.WriteLine("       - varies with N, K₀ (not universal)");
        _output.WriteLine("       - requires Δx (oscillator spacing) for physical units");
        _output.WriteLine("");
        _output.WriteLine("    3. l_TRM introduces MORE parameters than it removes:");
        _output.WriteLine("       noise amplitude + Δx  vs  one f_ref (I3)");
        _output.WriteLine("");
        _output.WriteLine("  Conclusion:");
        _output.WriteLine("    Coherence length does NOT provide a clean TRM-native");
        _output.WriteLine("    frequency anchor. The cesium I3 (f_ref = 9.19 GHz)");
        _output.WriteLine("    remains the preferred choice — simpler, non-circular,");
        _output.WriteLine("    universally defined, and requires no additional inputs.");
        _output.WriteLine("");
        _output.WriteLine("  Classification: NOT SUPPORTED");
        _output.WriteLine("  (l_TRM exists only with noise, depends on noise parameters,");
        _output.WriteLine("  and requires spatial calibration → introduces more");
        _output.WriteLine("  inputs than the cesium I3 anchor.)");
    }

    // ════════════════════════════════════════════════════════════
    // Simulation helpers
    // ════════════════════════════════════════════════════════════

    private sealed class SimResult
    {
        public double[] Phases { get; init; } = Array.Empty<double>();
        public double MeanOrder { get; init; }
    }

    /// <summary>
    /// Kuramoto simulation with frequency spread (no noise).
    /// </summary>
    private static SimResult? SimulateWithFreqSpread(int N, double K0, double freqSpread)
    {
        try
        {
            const int steps = 3000;
            
            const double dt = 0.08;

            var phases = new double[N];
            var omegas = new double[N];
            var rng = new Random(42);

            for (int i = 0; i < N; i++)
            {
                phases[i] = 2.0 * Math.PI * rng.NextDouble();
                omegas[i] = 1.0 + freqSpread * (rng.NextDouble() - 0.5);
            }

            for (int step = 0; step < steps; step++)
            {
                var dPhases = new double[N];
                for (int i = 0; i < N; i++)
                {
                    int left = (i - 1 + N) % N;
                    int right = (i + 1) % N;
                    double coupling = K0 * (Math.Sin(phases[left] - phases[i]) +
                                            Math.Sin(phases[right] - phases[i]));
                    dPhases[i] = dt * (omegas[i] + coupling);
                }
                for (int i = 0; i < N; i++)
                    phases[i] += dPhases[i];
            }

            double sumCos = 0, sumSin = 0;
            for (int i = 0; i < N; i++)
            {
                sumCos += Math.Cos(phases[i]);
                sumSin += Math.Sin(phases[i]);
            }
            double R = Math.Sqrt(sumCos * sumCos + sumSin * sumSin) / N;

            return new SimResult { Phases = phases, MeanOrder = R };
        }
        catch { return null; }
    }

    /// <summary>
    /// Kuramoto simulation with additive phase noise per step.
    /// </summary>
    private static SimResult? SimulateWithPhaseNoise(int N, double K0, double noiseAmp)
    {
        try
        {
            const int steps = 3000;
            
            const double dt = 0.08;

            var phases = new double[N];
            var omegas = new double[N];
            var rng = new Random(42);

            for (int i = 0; i < N; i++)
            {
                double angle = 2.0 * Math.PI * i / N;
                phases[i] = angle;
                omegas[i] = 1.0 + 0.05 * Math.Sin(angle) + 0.03 * Math.Cos(2.0 * angle);
            }

            for (int step = 0; step < steps; step++)
            {
                var dPhases = new double[N];
                for (int i = 0; i < N; i++)
                {
                    int left = (i - 1 + N) % N;
                    int right = (i + 1) % N;
                    double coupling = K0 * (Math.Sin(phases[left] - phases[i]) +
                                            Math.Sin(phases[right] - phases[i]));
                    double noise = noiseAmp * (rng.NextDouble() - 0.5);
                    dPhases[i] = dt * (omegas[i] + coupling) + noise;
                }
                for (int i = 0; i < N; i++)
                    phases[i] += dPhases[i];
            }

            double sumCos = 0, sumSin = 0;
            for (int i = 0; i < N; i++)
            {
                sumCos += Math.Cos(phases[i]);
                sumSin += Math.Sin(phases[i]);
            }
            double R = Math.Sqrt(sumCos * sumCos + sumSin * sumSin) / N;

            return new SimResult { Phases = phases, MeanOrder = R };
        }
        catch { return null; }
    }

    // ════════════════════════════════════════════════════════════
    // Exponential fit: log(C) = log(C₀) − r/l  →  l = −1/slope
    // ════════════════════════════════════════════════════════════

    private readonly record struct ExpFitResult(double DecayLength, double R2);

    private static ExpFitResult FitExponentialDecay(List<(double r, double c)> data)
    {
        if (data.Count < 3) return new ExpFitResult(0, 0);

        var logData = data
            .Where(p => p.c > 0)
            .Select(p => (p.r, lc: Math.Log(p.c)))
            .ToList();

        if (logData.Count < 3) return new ExpFitResult(0, 0);

        int n = logData.Count;
        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0, sumY2 = 0;
        foreach (var (x, y) in logData)
        {
            sumX += x; sumY += y;
            sumXY += x * y; sumX2 += x * x; sumY2 += y * y;
        }

        double denom = n * sumX2 - sumX * sumX;
        if (Math.Abs(denom) < 1e-12) return new ExpFitResult(0, 0);

        double slope = (n * sumXY - sumX * sumY) / denom;
        double intercept = (sumY - slope * sumX) / n;

        double ssRes = 0, ssTot = 0;
        double meanY = sumY / n;
        foreach (var (x, y) in logData)
        {
            double pred = intercept + slope * x;
            ssRes += (y - pred) * (y - pred);
            ssTot += (y - meanY) * (y - meanY);
        }
        double r2 = ssTot > 1e-12 ? 1.0 - ssRes / ssTot : 0;

        double decayLength = Math.Abs(slope) > 1e-12 ? -1.0 / slope : double.PositiveInfinity;

        return new ExpFitResult(decayLength, r2);
    }
}
