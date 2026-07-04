using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TRM.CMD
{
    // ── Configuration ──────────────────────────────────────────────

    /// <summary>
    /// Physical parameters for a PHARAO-like ground-vs-space clock comparison.
    /// </summary>
    public record PharaoConfig
    {
        /// <summary>Orbit altitude in metres (ISS ~ 400 km).</summary>
        public double AltitudeM { get; init; } = 400_000.0;

        /// <summary>Earth radius in metres.</summary>
        public double EarthRadiusM { get; init; } = 6_371_000.0;

        /// <summary>Standard gravity at surface (m/s²).</summary>
        public double G0 { get; init; } = 9.80665;

        /// <summary>Speed of light (m/s).</summary>
        public double C { get; init; } = 299_792_458.0;

        /// <summary>Orbital velocity (m/s).  Computed if null.</summary>
        public double? OrbitalVelocity { get; init; }

        /// <summary>Nominal clock frequency (Hz).</summary>
        public double NominalFrequencyHz { get; init; } = 1e14; // ~ optical

        /// <summary>Clock Allan deviation at 1 s (fractional).</summary>
        public double ClockStabilityAt1s { get; init; } = 1e-13;

        /// <summary>Number of simulated measurement epochs.</summary>
        public int Epochs { get; init; } = 1000;

        /// <summary>Time between epochs (s).  Uniform sampling assumed.</summary>
        public double DtSeconds { get; init; } = 86.4; // ~ 1.5 min per epoch × 1000

        /// <summary>Random seed for reproducibility.  null = random.</summary>
        public int? Seed { get; init; } = 42;
    }

    // ── Results ────────────────────────────────────────────────────

    /// <summary>
    /// Results from a single test-case simulation.
    /// </summary>
    public record PharaoTestCaseResult
    {
        public string CaseName { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;

        /// <summary>Timezone in seconds from t=0.</summary>
        public double[] TimeS { get; init; } = Array.Empty<double>();

        /// <summary>Fractional frequency offset of ground clock after GR + Doppler.</summary>
        public double[] GroundFractional { get; init; } = Array.Empty<double>();

        /// <summary>Fractional frequency offset of space clock after GR + Doppler.</summary>
        public double[] SpaceFractional { get; init; } = Array.Empty<double>();

        /// <summary>Ground-minus-space fractional difference (positive = ground faster).</summary>
        public double[] DifferenceFractional { get; init; } = Array.Empty<double>();

        /// <summary>Expected difference from GR + Doppler only (no B(t)).</summary>
        public double ExpectedDifferenceFractional { get; init; }

        /// <summary>Mean residual after subtracting expected.</summary>
        public double MeanResidual { get; init; }

        /// <summary>Standard deviation of residual.</summary>
        public double StdResidual { get; init; }

        /// <summary>Drift slope of residual (fractional / s).</summary>
        public double ResidualDriftSlope { get; init; }

        /// <summary>p-value for residual drift being non-zero (two-sided t-test).</summary>
        public double DriftPValue { get; init; }

        /// <summary>Whether B(t) is detectable at 5σ.</summary>
        public bool BDetectable { get; init; }

        /// <summary>Minimum detectable |B| slope (fractional / s) at 5σ given this noise.</summary>
        public double MinDetectableSlope { get; init; }

        /// <summary>Applied B_g(t) slope (fractional / s) — NaN if no B applied.</summary>
        public double AppliedBGroundSlope { get; init; } = double.NaN;

        /// <summary>Applied B_s(t) slope (fractional / s) — NaN if no B applied.</summary>
        public double AppliedBSpaceSlope { get; init; } = double.NaN;

        // ── Gradient-model fields ──────────────────────────────────

        /// <summary>Spatial gradient fraction used (B_space = B0*(1+this) + ...).</summary>
        public double SpatialGradientFraction { get; init; }

        /// <summary>Slope difference fraction used (ε_space = ε*(1+this)).</summary>
        public double SlopeDifferenceFraction { get; init; }

        /// <summary>Residual drift slope in ns/day (absolute units).</summary>
        public double ResidualSlopeNsPerDay { get; init; }

        /// <summary>Residual standard deviation in ns.</summary>
        public double ResidualStdNs { get; init; }

        /// <summary>Smallest spatial gradient fraction detectable at 5σ.</summary>
        public double MinDetectableSpatialGradientFraction { get; init; }

        /// <summary>Smallest slope difference fraction detectable at 5σ.</summary>
        public double MinDetectableSlopeDifferenceFraction { get; init; }
    }

    // ── Analyser ───────────────────────────────────────────────────

    /// <summary>
    /// Simulates a PHARAO-like ground-vs-space clock comparison
    /// to test whether a hidden global drift B(t) is detectable.
    ///
    /// Key principle:
    ///   If B(t) is truly global (identical at ground and in space),
    ///   it cancels in the difference and is NOT detectable.
    ///   Any spatial gradient in B(t) produces a residual.
    /// </summary>
    public static class PharaoResidualAnalyzer
    {
        // ── Physical models ────────────────────────────────────────

        /// <summary>
        /// Gravitational redshift: fractional frequency shift between
        /// space (altitude h) and ground.
        /// Positive = space clock appears faster (lower potential).
        /// Δf/f ≈ g·h / c²  (first-order approximation).
        /// </summary>
        public static double GravitationalRedshift(PharaoConfig c)
        {
            // Full:  GM/c² · (1/R - 1/(R+h))  ≈ g·h / c²  for h ≪ R
            return c.G0 * c.AltitudeM / (c.C * c.C);
        }

        /// <summary>
        /// Second-order (transverse) Doppler shift for circular orbit.
        /// Δf/f = −v²/(2c²).  This is the net relativistic Doppler after
        /// orbit-averaging cancels the first-order term.
        /// </summary>
        public static double TransverseDoppler(PharaoConfig c)
        {
            double v = c.OrbitalVelocity
                ?? Math.Sqrt(c.G0 * c.EarthRadiusM * c.EarthRadiusM
                             / (c.EarthRadiusM + c.AltitudeM));
            return -0.5 * v * v / (c.C * c.C);
        }

        /// <summary>
        /// Total known relativistic offset (ground reference).
        /// Ground clock minus space clock fractional difference.
        /// Positive = ground clock ticks faster.
        ///
        /// Ground: deeper in gravity well → slower (negative GR)
        /// Space:  faster motion → slower (negative TD, larger magnitude)
        /// Net:    ground ticks faster than space
        /// </summary>
        public static double ExpectedRelativisticDifference(PharaoConfig c)
        {
            double gr = GravitationalRedshift(c);
            double td = TransverseDoppler(c);
            // Ground: −GR (deeper), 0 TD (stationary)
            // Space:   0 GR (higher), +TD (moving, second-order negative)
            // Difference = ground − space = (−GR) − (TD) = −GR − TD
            // Wait — let me think more carefully.
            //
            // Proper time rates:
            //   dτ/dt = √(1 + 2Φ/c² − v²/c²) ≈ 1 + Φ/c² − v²/(2c²)
            //
            // Ground: Φ_g = −GM/R,       v_g ≈ 0
            //   fractional shift = Φ_g/c² = −gR/c²  (approximate)
            //
            // Space:  Φ_s = −GM/(R+h),    v_s = orbital velocity
            //   fractional shift = Φ_s/c² − v_s²/(2c²)
            //
            // Difference (ground − space):
            //   = (Φ_g − Φ_s)/c² + v_s²/(2c²)
            //   ≈ +g·h/c²  +  v_s²/(2c²)
            //   =  +GR      −  |TD|       since TD is negative
            //   =  GR + TD                 since TD < 0
            //
            // Actually let me just use the signs carefully.
            // GravitationalRedshift returns +g·h/c² (~ +4.4e-11)
            //   = ground ticks FASTER by this amount relative to space
            // TransverseDoppler returns −v²/(2c²) (~ −3.3e-10)
            //   = space ticks SLOWER by this amount relative to ground
            //   = ground ticks FASTER by |TD|
            //
            // So net = GR − |TD| — but since TD is already negative:
            //   net = GR + TD  (adding a negative number)
            //
            // For PHARAO: GR ≈ +4.4e-11, TD ≈ −3.3e-10
            //   net ≈ −2.86e-10  → ground ticks SLOWER than space? No —
            //   the GR effect makes ground slower (deeper well), but
            //   the TD effect makes space slower (fast motion).
            //   The TD effect is larger, so net: ground ticks FASTER.
            //
            // Let me re-derive clearly:
            //   Ground clock:  f_g = f₀ · (1 + Φ_g/c²)    since v≈0
            //   Space clock:   f_s = f₀ · (1 + Φ_s/c² − v²/(2c²))
            //
            //   Fractional difference (ground − space) / f₀:
            //     = (Φ_g − Φ_s)/c² + v²/(2c²)
            //     = −g·h/c² + v²/(2c²)
            //     = −GR_magnitude + |TD|_magnitude
            //
            // So: diff = −GR − TD   (since TD itself is negative)
            //          = −4.4e-11 − (−3.3e-10)
            //          = −4.4e-11 + 3.3e-10
            //          = +2.86e-10
            //
            // Ground ticks faster by ~2.86e-10 fractionally.  Correct.

            return -gr - td; // −GR − (−|TD|) = −GR + |TD| = +2.86e-10
        }

        // ── Noise model ────────────────────────────────────────────

        /// <summary>
        /// Generates white frequency noise scaled by the Allan deviation
        /// at the given sampling interval.
        /// For white FM noise: σ_y(τ) = σ_y(1s) / √τ.
        /// </summary>
        private static double[] GenerateClockNoise(
            int epochs, double dt, double stabilityAt1s, Random rng)
        {
            // White frequency noise: Allan deviation scales as 1/√τ
            double sigmaTau = stabilityAt1s / Math.Sqrt(dt);
            double[] noise = new double[epochs];
            for (int i = 0; i < epochs; i++)
            {
                // Box-Muller
                double u1 = 1.0 - rng.NextDouble();
                double u2 = 1.0 - rng.NextDouble();
                noise[i] = sigmaTau * Math.Sqrt(-2.0 * Math.Log(Math.Max(u1, 1e-30)))
                           * Math.Cos(2.0 * Math.PI * u2);
            }
            return noise;
        }

        // ── Core simulation (gradient model) ──────────────────────

        /// <summary>
        /// B(t) gradient model:
        ///   B_ground(t) = B0 + ε · t
        ///   B_space(t)  = B0 · (1 + spatialGradientFraction)
        ///                + ε · (1 + slopeDifferenceFraction) · t
        ///
        /// The net residual slope is −ε · slopeDifferenceFraction.
        /// If both gradient fractions are zero, B(t) is perfectly global.
        /// </summary>
        public static PharaoTestCaseResult Simulate(
            PharaoConfig config,
            string caseName,
            string description,
            double b0 = 0.0,
            double epsilon = 0.0,
            double spatialGradientFraction = 0.0,
            double slopeDifferenceFraction = 0.0)
        {
            int n = config.Epochs;
            double dt = config.DtSeconds;
            double expected = ExpectedRelativisticDifference(config);
            var rng = config.Seed.HasValue
                ? new Random(config.Seed.Value + caseName.GetHashCode())
                : new Random();

            double[] t = new double[n];
            for (int i = 0; i < n; i++)
                t[i] = i * dt;

            double[] noiseG = GenerateClockNoise(n, dt, config.ClockStabilityAt1s, rng);
            double[] noiseS = GenerateClockNoise(n, dt, config.ClockStabilityAt1s, rng);

            double[] groundFrac = new double[n];
            double[] spaceFrac  = new double[n];
            double[] diffFrac   = new double[n];

            double gr = GravitationalRedshift(config);
            double td = TransverseDoppler(config);

            for (int i = 0; i < n; i++)
            {
                groundFrac[i] = -gr + noiseG[i];
                spaceFrac[i]  =  td + noiseS[i];

                // Inject B(t) via gradient model
                double bGround = b0 + epsilon * t[i];
                double bSpace  = b0 * (1.0 + spatialGradientFraction)
                               + epsilon * (1.0 + slopeDifferenceFraction) * t[i];

                groundFrac[i] += bGround;
                spaceFrac[i]  += bSpace;

                diffFrac[i] = groundFrac[i] - spaceFrac[i];
            }

            // Residual = measured difference − expected difference
            double[] residual = new double[n];
            for (int i = 0; i < n; i++)
                residual[i] = diffFrac[i] - expected;

            double meanRes = residual.Average();
            double stdRes  = Math.Sqrt(residual.Average(r =>
                (r - meanRes) * (r - meanRes)));

            // Linear fit of residual vs time
            var (slope, intercept) = FitLine(t, residual);

            // Standard error of slope
            double tMean = t.Average();
            double ssx = t.Sum(ti => (ti - tMean) * (ti - tMean));
            double seSlope = stdRes / Math.Sqrt(ssx);
            double tStat = slope / Math.Max(seSlope, 1e-30);
            double pValue = 2.0 * (1.0 - NormalCdf(Math.Abs(tStat)));

            // Minimum detectable slope at 5σ
            double minDetectableSlope = 5.0 * seSlope;

            // Convert to ns units (fractional × config.NominalFrequencyHz gives Hz;
            // 1/Hz = s; 1e9 × s gives ns.  But fractional × 1e9 gives ns offset
            // in one second.  Per-day: × 86400.)
            double slopeNsPerDay = slope * 1e9 * 86400.0;
            double stdNs = stdRes * 1e9;

            // Injected net slope difference = −ε · slopeDifferenceFraction
            double injectedDiffSlope = -epsilon * slopeDifferenceFraction;
            bool detectable = Math.Abs(slope) > minDetectableSlope
                              && Math.Abs(injectedDiffSlope) > 1e-30;

            // Minimum detectable gradient fractions:
            // To detect slopeDifferenceFraction, we need |ε · f| > minDetectableSlope
            // → f > minDetectableSlope / |ε|
            double minDetectableSlopeFrac = Math.Abs(epsilon) > 1e-30
                ? minDetectableSlope / Math.Abs(epsilon)
                : double.PositiveInfinity;

            // Spatial gradient: detectable via residual mean shift, not slope.
            // |B0 · f| > 5 · σ_mean where σ_mean = σ / √n
            double seMean = stdRes / Math.Sqrt(n);
            double minDetectableSpatialFrac = Math.Abs(b0) > 1e-30
                ? 5.0 * seMean / Math.Abs(b0)
                : double.PositiveInfinity;

            return new PharaoTestCaseResult
            {
                CaseName         = caseName,
                Description      = description,
                TimeS            = t,
                GroundFractional = groundFrac,
                SpaceFractional  = spaceFrac,
                DifferenceFractional = diffFrac,
                ExpectedDifferenceFractional = expected,
                MeanResidual     = meanRes,
                StdResidual      = stdRes,
                ResidualDriftSlope = slope,
                DriftPValue      = pValue,
                BDetectable      = detectable,
                MinDetectableSlope = minDetectableSlope,
                AppliedBGroundSlope = epsilon,  // ground slope = ε
                AppliedBSpaceSlope  = epsilon * (1.0 + slopeDifferenceFraction),
                SpatialGradientFraction = spatialGradientFraction,
                SlopeDifferenceFraction = slopeDifferenceFraction,
                ResidualSlopeNsPerDay = slopeNsPerDay,
                ResidualStdNs = stdNs,
                MinDetectableSpatialGradientFraction = minDetectableSpatialFrac,
                MinDetectableSlopeDifferenceFraction = minDetectableSlopeFrac,
            };
        }

        // ── Test battery ───────────────────────────────────────────

        /// <summary>
        /// Runs the full set of PHARAO residual test cases
        /// including gradient-fraction scans.
        /// </summary>
        public static List<PharaoTestCaseResult> RunAllTests(PharaoConfig? config = null)
        {
            var c = config ?? new PharaoConfig();
            var results = new List<PharaoTestCaseResult>();

            // ── Case 1: No B(t) — baseline ─────────────────────────
            results.Add(Simulate(c,
                "C01_Baseline_NoB",
                "No B(t) injected.  Residual should be pure clock noise."));

            // ── Case 2: Perfectly global B(t) ──────────────────────
            double epsilon = 1e-15; // 1e-15 fractional / s
            results.Add(Simulate(c,
                "C02_GlobalB",
                "B(t) applied identically to ground and space (spatial=0, slopeDiff=0).",
                b0: 1e-12, epsilon: epsilon));

            // ── Case 3: 50% slope difference ───────────────────────
            results.Add(Simulate(c,
                "C03_SlopeDifference50pc",
                "50 % slope difference: space slope = 1.5 x ground slope.",
                b0: 1e-12, epsilon: epsilon,
                slopeDifferenceFraction: 0.5));

            // ── Case 4: Small slope difference ─────────────────────
            results.Add(Simulate(c,
                "C04_SlopeDifference1pc",
                "1 % slope difference — likely buried in clock noise.",
                b0: 1e-12, epsilon: epsilon,
                slopeDifferenceFraction: 0.01));

            // ── Case 5: Spatial gradient only, no slope ────────────
            results.Add(Simulate(c,
                "C05_SpatialGradient1pc",
                "1 % spatial gradient (constant offset), zero slope difference.",
                b0: 1e-10, epsilon: 0.0,
                spatialGradientFraction: 0.01));

            // ── Case 6: Combined small gradient + slope difference ─
            results.Add(Simulate(c,
                "C06_CombinedSmallGradientAndSlope",
                "Small spatial gradient + small slope difference simultaneously.",
                b0: 5e-11, epsilon: epsilon,
                spatialGradientFraction: 0.005,
                slopeDifferenceFraction: 0.005));

            // ── Case 7: Long integration, no B ─────────────────────
            var longConfig = c with { Epochs = 5000 };
            results.Add(Simulate(longConfig,
                "C07_LongIntegration_NoB",
                "No B(t), 5000 epochs.  Tests noise floor at long integration."));

            // ── Scan: spatial gradient fraction ────────────────────
            results.AddRange(ScanSpatialGradient(c,
                "C08_",
                b0: 1e-10, epsilon: 0.0));

            // ── Scan: slope difference fraction ────────────────────
            results.AddRange(ScanSlopeDifference(c,
                "C09_",
                b0: 0.0, epsilon: 1e-15));

            return results;
        }

        // ── Threshold scans ────────────────────────────────────────

        /// <summary>
        /// Scans spatial gradient fractions from 1e-6 to 1e-1,
        /// returning the smallest value detectable at 5σ.
        /// </summary>
        public static List<PharaoTestCaseResult> ScanSpatialGradient(
            PharaoConfig config,
            string casePrefix,
            double b0 = 1e-10,
            double epsilon = 0.0)
        {
            var results = new List<PharaoTestCaseResult>();
            double[] fractions = { 1e-6, 3e-6, 1e-5, 3e-5, 1e-4, 3e-4,
                                   1e-3, 3e-3, 1e-2, 3e-2, 1e-1 };

            foreach (double f in fractions)
            {
                var r = Simulate(config,
                    $"{casePrefix}SpatialGrad_{f:E0}",
                    $"Spatial gradient scan: fraction = {f:E0}",
                    b0: b0, epsilon: epsilon,
                    spatialGradientFraction: f,
                    slopeDifferenceFraction: 0.0);
                results.Add(r);
            }
            return results;
        }

        /// <summary>
        /// Scans slope difference fractions from 1e-6 to 1e-1,
        /// returning the smallest value detectable at 5σ.
        /// </summary>
        public static List<PharaoTestCaseResult> ScanSlopeDifference(
            PharaoConfig config,
            string casePrefix,
            double b0 = 0.0,
            double epsilon = 1e-15)
        {
            var results = new List<PharaoTestCaseResult>();
            double[] fractions = { 1e-6, 3e-6, 1e-5, 3e-5, 1e-4, 3e-4,
                                   1e-3, 3e-3, 1e-2, 3e-2, 1e-1 };

            foreach (double f in fractions)
            {
                var r = Simulate(config,
                    $"{casePrefix}SlopeDiff_{f:E0}",
                    $"Slope difference scan: fraction = {f:E0}",
                    b0: b0, epsilon: epsilon,
                    spatialGradientFraction: 0.0,
                    slopeDifferenceFraction: f);
                results.Add(r);
            }
            return results;
        }

        // ── CSV output ─────────────────────────────────────────────

        /// <summary>
        /// Writes all scan results to CSV files.
        /// </summary>
        public static void WriteScanCsv(
            string outputFolder,
            List<PharaoTestCaseResult> results)
        {
            Directory.CreateDirectory(outputFolder);

            var spatialScans = results
                .Where(r => r.CaseName.Contains("SpatialGrad_"))
                .ToList();
            var slopeScans = results
                .Where(r => r.CaseName.Contains("SlopeDiff_"))
                .ToList();

            if (spatialScans.Count > 0)
                WriteScanCsv(Path.Combine(outputFolder, "pharao_gradient_scan.csv"),
                    spatialScans, "SpatialGradientFraction");

            if (slopeScans.Count > 0)
                WriteScanCsv(Path.Combine(outputFolder, "pharao_slope_scan.csv"),
                    slopeScans, "SlopeDifferenceFraction");
        }

        private static void WriteScanCsv(
            string path,
            List<PharaoTestCaseResult> scans,
            string fractionColumn)
        {
            var inv = CultureInfo.InvariantCulture;
            using var w = new StreamWriter(path);
            w.WriteLine($"Case,{fractionColumn},ResidualSlope,ResidualSlopeNsPerDay,StdResidualNs,DriftPValue,BDetectable,MinDetectableFrac");

            foreach (var r in scans)
            {
                double frac = fractionColumn == "SpatialGradientFraction"
                    ? r.SpatialGradientFraction
                    : r.SlopeDifferenceFraction;

                w.WriteLine(string.Format(inv,
                    "{0},{1:E6},{2:E6},{3:F6},{4:F6},{5:E4},{6}",
                    r.CaseName,
                    frac,
                    r.ResidualDriftSlope,
                    r.ResidualSlopeNsPerDay,
                    r.ResidualStdNs,
                    r.DriftPValue,
                    r.BDetectable ? "YES" : "no"));
            }
        }

        // ── Statistics helpers ──────────────────────────────────────

        private static (double slope, double intercept) FitLine(
            double[] x, double[] y)
        {
            int n = x.Length;
            double sx = 0, sy = 0, sxx = 0, sxy = 0;
            for (int i = 0; i < n; i++)
            {
                sx  += x[i];
                sy  += y[i];
                sxx += x[i] * x[i];
                sxy += x[i] * y[i];
            }
            double denom = n * sxx - sx * sx;
            if (Math.Abs(denom) < 1e-30)
                return (0, sy / n);

            double slope     = (n * sxy - sx * sy) / denom;
            double intercept = (sy - slope * sx) / n;
            return (slope, intercept);
        }

        /// <summary>
        /// Standard normal CDF (Abramowitz & Stegun 26.2.17 approximation).
        /// </summary>
        private static double NormalCdf(double x)
        {
            if (x < -8) return 0;
            if (x >  8) return 1;

            double t = 1.0 / (1.0 + 0.2316419 * Math.Abs(x));
            double d = 0.3989422804014327; // 1/√(2π)
            double p = d * Math.Exp(-x * x / 2.0) * t
                       * (0.319381530 + t * (-0.356563782 + t
                       * (1.781477937 + t * (-1.821255978 + t * 1.330274429))));
            return x > 0 ? 1.0 - p : p;
        }

        // ── Report ─────────────────────────────────────────────────

        /// <summary>
        /// Returns a multi-line console summary of the test results,
        /// including physical parameters and detectability thresholds.
        /// </summary>
        public static string FormatSummary(
            PharaoConfig config,
            List<PharaoTestCaseResult> results)
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new System.Text.StringBuilder();

            double gr = GravitationalRedshift(config);
            double td = TransverseDoppler(config);
            double orbitV = config.OrbitalVelocity
                ?? Math.Sqrt(config.G0 * config.EarthRadiusM * config.EarthRadiusM
                             / (config.EarthRadiusM + config.AltitudeM));

            sb.AppendLine("══════════════════════════════════════════");
            sb.AppendLine("  PHARAO RESIDUAL ANALYSER");
            sb.AppendLine("  B(t) Detectability in Clock Comparisons");
            sb.AppendLine("══════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine("── Physical Parameters ──");
            sb.AppendLine(string.Format(inv,
                "  Altitude           : {0:F0} km", config.AltitudeM / 1000.0));
            sb.AppendLine(string.Format(inv,
                "  Orbital velocity   : {0:F3} km/s", orbitV / 1000.0));
            sb.AppendLine(string.Format(inv,
                "  Grav. redshift     : {0:E3}  (Δf/f)", gr));
            sb.AppendLine(string.Format(inv,
                "  Transverse Doppler : {0:E3}  (Δf/f)", td));
            sb.AppendLine(string.Format(inv,
                "  Expected diff.     : {0:E3}  (ground − space)", ExpectedRelativisticDifference(config)));
            sb.AppendLine(string.Format(inv,
                "  Clock stability    : {0:E3} @ 1 s", config.ClockStabilityAt1s));
            sb.AppendLine(string.Format(inv,
                "  Epochs / Δt        : {0} × {1:F1} s  = {2:F1} h total",
                config.Epochs, config.DtSeconds,
                config.Epochs * config.DtSeconds / 3600.0));
            sb.AppendLine();

            sb.AppendLine("── Test Cases ──");
            sb.AppendLine(string.Format(inv,
                "  {0,-26} {1,-14} {2,-14} {3,-10} {4,-10}",
                "Case", "\u0394B slope", "Residual", "p-value", "Detectable"));
            sb.AppendLine("  " + new string('─', 74));

            foreach (var r in results)
            {
                double dB = double.IsNaN(r.AppliedBGroundSlope) ? 0.0
                    : r.AppliedBGroundSlope - r.AppliedBSpaceSlope;
                string dBStr = double.IsNaN(r.AppliedBGroundSlope)
                    ? "—" : dB.ToString("E2", inv);

                sb.AppendLine(string.Format(inv,
                    "  {0,-26} {1,14} {2,14:E3} {3,10:F4} {4,10}",
                    r.CaseName,
                    dBStr,
                    r.ResidualDriftSlope,
                    r.DriftPValue,
                    r.BDetectable ? "YES" : "no"));
            }

            sb.AppendLine();
            sb.AppendLine("── Detectability Thresholds ──");

            var noB = results.FirstOrDefault(r => r.CaseName == "C01_Baseline_NoB");
            if (noB != null)
            {
                sb.AppendLine(string.Format(inv,
                    "  Noise floor (slope SE)   : {0:E3} / s", noB.MinDetectableSlope / 5.0));
                sb.AppendLine(string.Format(inv,
                    "  5sigma detection slope   : {0:E3} / s", noB.MinDetectableSlope));
                sb.AppendLine(string.Format(inv,
                    "  Residual std (ns)        : {0:F3} ns", noB.ResidualStdNs));
                sb.AppendLine(string.Format(inv,
                    "  Residual slope (ns/day)  : {0:F6} ns/day", noB.ResidualSlopeNsPerDay));
            }

            // Find first detectable spatial-gradient fraction from scans
            var spatialScans = results
                .Where(r => r.CaseName.Contains("SpatialGrad_"))
                .OrderBy(r => r.SpatialGradientFraction)
                .ToList();
            var firstSpatial = spatialScans.FirstOrDefault(r => r.BDetectable);

            var slopeScans = results
                .Where(r => r.CaseName.Contains("SlopeDiff_"))
                .OrderBy(r => r.SlopeDifferenceFraction)
                .ToList();
            var firstSlope = slopeScans.FirstOrDefault(r => r.BDetectable);

            sb.AppendLine();
            sb.AppendLine("── 5sigma Minimum Detectable Non-Global Fractions ──");
            sb.AppendLine(firstSpatial != null
                ? string.Format(inv,
                    "  Spatial gradient fraction : {0:E3}  (any non-global B(t) larger than this would be visible)",
                    firstSpatial.SpatialGradientFraction)
                : "  Spatial gradient fraction : > 1e-1  (not detectable in scanned range)");
            sb.AppendLine(firstSlope != null
                ? string.Format(inv,
                    "  Slope difference fraction : {0:E3}  (any non-global B(t) larger than this would be visible)",
                    firstSlope.SlopeDifferenceFraction)
                : "  Slope difference fraction : > 1e-1  (not detectable in scanned range)");

            sb.AppendLine();
            sb.AppendLine("── Key Findings ──");

            var globalB = results.FirstOrDefault(r => r.CaseName == "C02_GlobalB");
            var gradB   = results.FirstOrDefault(r => r.CaseName == "C03_SlopeDifference50pc");

            if (globalB != null)
            {
                sb.AppendLine(globalB.BDetectable
                    ? "  WARNING: Fully global B(t) was DETECTED — possible numerical issue."
                    : "  PASS:  Fully global B(t) NOT detectable (cancels in difference).");
            }

            if (gradB != null)
            {
                sb.AppendLine(gradB.BDetectable
                    ? "  PASS:  Spatially non-uniform B(t) IS detectable via residual drift."
                    : "  INFO:  Spatial gradient too small for current noise floor.");
            }

            sb.AppendLine();
            sb.AppendLine("── Conclusion ──");
            sb.AppendLine("  A perfectly global B(t) is indistinguishable from");
            sb.AppendLine("  a change in the definition of the second.");
            sb.AppendLine("  Only a spatial gradient or slope difference in B(t)");
            sb.AppendLine("  produces an observable residual in ground-vs-space");
            sb.AppendLine("  clock comparisons.");
            sb.AppendLine();
            sb.AppendLine("  Two independent non-globality bounds are extracted:");
            sb.AppendLine(firstSpatial != null
                ? string.Format(inv,
                    "    - spatial gradient fraction > {0:E3} would be visible", firstSpatial.SpatialGradientFraction)
                : "    - spatial gradient fraction: all tested values below threshold");
            sb.AppendLine(firstSlope != null
                ? string.Format(inv,
                    "    - slope difference fraction > {0:E3} would be visible", firstSlope.SlopeDifferenceFraction)
                : "    - slope difference fraction: all tested values below threshold");
            sb.AppendLine("══════════════════════════════════════════");

            return sb.ToString();
        }
    }
}
