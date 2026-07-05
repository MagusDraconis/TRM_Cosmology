using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace TRM.CMD
{
    // ── Data Model ─────────────────────────────────────────────────

    /// <summary>
    /// Extracted data from a coupled laser array experiment.
    /// </summary>
    public class LaserExperiment
    {
        public string Name { get; set; } = string.Empty;
        public int OscillatorCount { get; set; }
        public double CouplingK { get; set; }
        public double OmegaRms { get; set; }
        public double OmegaRmsOverK { get; set; }
        /// <summary>Sync metric (e.g. IPR proxy, 0 = no sync, 1 = full sync).</summary>
        public double SyncMetric { get; set; }
    }

    /// <summary>
    /// Validation result after testing TRM/TQM predictions against laser array data.
    /// </summary>
    public class ValidationResult
    {
        public bool ThresholdConfirmed { get; set; }
        public bool ScalingConfirmed { get; set; }
        public bool TopologyConsistent { get; set; }
        public string Interpretation { get; set; } = string.Empty;
    }

    // ── Analyser ───────────────────────────────────────────────────

    /// <summary>
    /// Analyses coupled laser array experimental data from the Weizmann Institute
    /// (400-laser array paper) to test TRM/TQM structural predictions:
    ///   - synchronization threshold near K / sigma_omega ≈ 1
    ///   - scaling collapse across different K values
    ///   - cluster size scaling ~1 / (OmegaRms/K)^2
    /// </summary>
    public static class LaserArrayPaperAnalyzer
    {
        // ── Dataset ────────────────────────────────────────────────

        /// <summary>
        /// Builds the extracted dataset from paper figures.
        /// Values are approximate readings from published plots.
        /// </summary>
        public static List<LaserExperiment> BuildDataset()
        {
            return new List<LaserExperiment>
            {
                // K = 0.25 series
                new LaserExperiment
                {
                    Name = "K=0.25 low disorder", OscillatorCount = 400,
                    CouplingK = 0.25,
                    OmegaRms = 0.25 * 0.2, OmegaRmsOverK = 0.2,
                    SyncMetric = 0.9
                },
                new LaserExperiment
                {
                    Name = "K=0.25 threshold", OscillatorCount = 400,
                    CouplingK = 0.25,
                    OmegaRms = 0.25 * 0.8, OmegaRmsOverK = 0.8,
                    SyncMetric = 0.5
                },
                new LaserExperiment
                {
                    Name = "K=0.25 high disorder", OscillatorCount = 400,
                    CouplingK = 0.25,
                    OmegaRms = 0.25 * 2.5, OmegaRmsOverK = 2.5,
                    SyncMetric = 0.1
                },

                // K = 0.12 series
                new LaserExperiment
                {
                    Name = "K=0.12 low disorder", OscillatorCount = 400,
                    CouplingK = 0.12,
                    OmegaRms = 0.12 * 0.2, OmegaRmsOverK = 0.2,
                    SyncMetric = 0.8
                },
                new LaserExperiment
                {
                    Name = "K=0.12 threshold", OscillatorCount = 400,
                    CouplingK = 0.12,
                    OmegaRms = 0.12 * 0.8, OmegaRmsOverK = 0.8,
                    SyncMetric = 0.4
                },
                new LaserExperiment
                {
                    Name = "K=0.12 high disorder", OscillatorCount = 400,
                    CouplingK = 0.12,
                    OmegaRms = 0.12 * 2.5, OmegaRmsOverK = 2.5,
                    SyncMetric = 0.05
                }
            };
        }

        // ── Test 1 — Synchronization Threshold ─────────────────────

        /// <summary>
        /// Checks whether the sync metric collapses sharply near OmegaRmsOverK ≈ 1,
        /// indicating a Kuramoto-like threshold.
        /// </summary>
        public static bool HasThresholdBehavior(List<LaserExperiment> data)
        {
            if (data.Count < 2) return false;

            // Sort by OmegaRmsOverK
            var sorted = data.OrderBy(d => d.OmegaRmsOverK).ToList();

            // Look for a sharp drop: find the point where SyncMetric falls below 0.5
            double maxDrop = 0.0;
            double dropRatio = 0.0;
            for (int i = 1; i < sorted.Count; i++)
            {
                double drop = sorted[i - 1].SyncMetric - sorted[i].SyncMetric;
                if (drop > maxDrop)
                {
                    maxDrop = drop;
                    dropRatio = sorted[i].OmegaRmsOverK;
                }
            }

            // Threshold behaviour: sharp drop (>0.3) occurs in the range 0.5–1.5
            const double minDrop = 0.3;
            const double ratioLow = 0.5;
            const double ratioHigh = 1.5;

            bool sharpDrop = maxDrop >= minDrop;
            bool nearUnity = dropRatio >= ratioLow && dropRatio <= ratioHigh;

            return sharpDrop && nearUnity;
        }

        // ── Test 2 — Scaling Collapse ──────────────────────────────

        /// <summary>
        /// Tests whether experiments with different absolute K values collapse
        /// onto the same curve when plotted against OmegaRmsOverK (the ratio).
        ///
        /// This tests the TRM/TQM prediction that synchronization depends on K / sigma_omega,
        /// not on absolute K or absolute sigma_omega separately.
        /// </summary>
        public static bool TestScalingCollapse(List<LaserExperiment> data)
        {
            // Group by OmegaRmsOverK
            var groups = data.GroupBy(d => d.OmegaRmsOverK).ToList();

            // For each ratio, check that SyncMetric is similar across different K values
            foreach (var group in groups)
            {
                if (group.Count() < 2) continue;
                var metrics = group.Select(d => d.SyncMetric).ToList();
                double mean = metrics.Average();
                // Allow 20% relative tolerance for experimental scatter
                double relDiff = metrics.Max() - metrics.Min();
                if (relDiff > 0.2 * mean)
                    return false;
            }

            return true;
        }

        // ── Test 3 — Cluster Size Scaling ──────────────────────────

        /// <summary>
        /// Tests the paper's reported scaling: N_sync ∝ K² / OmegaRms².
        /// The sync metric should follow SyncMetric ~ 1 / (OmegaRmsOverK)^2.
        /// </summary>
        public static (bool valid, double rSquared) TestClusterScaling(List<LaserExperiment> data)
        {
            var xs = new List<double>();
            var ys = new List<double>();

            foreach (var d in data)
            {
                if (d.OmegaRmsOverK < 0.01) continue; // avoid division issues
                double x = 1.0 / (d.OmegaRmsOverK * d.OmegaRmsOverK);
                double y = d.SyncMetric;
                xs.Add(x);
                ys.Add(y);
            }

            if (xs.Count < 3) return (false, 0.0);

            // Linear regression: SyncMetric = slope * (1 / ratio²) + intercept
            double meanX = xs.Average(), meanY = ys.Average();
            double num = 0.0, den = 0.0;
            for (int i = 0; i < xs.Count; i++)
            {
                double dx = xs[i] - meanX;
                num += dx * (ys[i] - meanY);
                den += dx * dx;
            }

            if (Math.Abs(den) < 1e-12) return (false, 0.0);

            double slope = num / den;
            double intercept = meanY - slope * meanX;

            // R-squared
            double ssRes = 0.0, ssTot = 0.0;
            for (int i = 0; i < xs.Count; i++)
            {
                double pred = slope * xs[i] + intercept;
                ssRes += (ys[i] - pred) * (ys[i] - pred);
                ssTot += (ys[i] - meanY) * (ys[i] - meanY);
            }
            double rSquared = ssTot > 1e-12 ? 1.0 - ssRes / ssTot : 0.0;

            // Valid if R² > 0.7
            return (rSquared > 0.7, rSquared);
        }

        // ── Full Analysis ──────────────────────────────────────────

        /// <summary>
        /// Runs the complete validation pipeline and returns the result.
        /// </summary>
        public static ValidationResult Analyze(List<LaserExperiment> data)
        {
            bool threshold = HasThresholdBehavior(data);
            bool collapse = TestScalingCollapse(data);
            var (scaling, r2) = TestClusterScaling(data);

            string interpretation;
            if (threshold && collapse && scaling)
            {
                interpretation = "Real laser array matches TRM/TQM structure: " +
                    "collective synchronization governed by K / sigma_omega. " +
                    "The centroid condition, threshold behaviour, and cluster " +
                    "scaling all agree with the minimal coupled-oscillator model.";
            }
            else if (threshold && collapse)
            {
                interpretation = "Laser array shows Kuramoto-like threshold and " +
                    "ratio scaling but cluster-size scaling deviates " +
                    "(R² = " + r2.ToString("F3", CultureInfo.InvariantCulture) + "). " +
                    "TRM/TQM structure is partially validated.";
            }
            else if (threshold)
            {
                interpretation = "Laser array shows a synchronization threshold " +
                    "near OmegaRms/K ≈ 1 but scaling collapse is inconsistent. " +
                    "The phase-only model may need amplitude or nonlinearity extensions.";
            }
            else
            {
                interpretation = "Laser array data do not show clean Kuramoto-like " +
                    "threshold behaviour. The TRM/TQM minimal model may not directly " +
                    "apply to this system without additional physics (amplitude dynamics, " +
                    "dissipation, noise).";
            }

            return new ValidationResult
            {
                ThresholdConfirmed = threshold,
                ScalingConfirmed = scaling,
                TopologyConsistent = collapse,
                Interpretation = interpretation
            };
        }

        // ── Console Output ─────────────────────────────────────────

        /// <summary>
        /// Prints the analysis summary to the console.
        /// </summary>
        public static void PrintSummary(ValidationResult result) { PrintSummary(result, Console.Out); }

        public static void PrintSummary(ValidationResult result, TextWriter writer)
        {
            var inv = CultureInfo.InvariantCulture;

            writer.WriteLine("═══════════════════════════════════════════");
            writer.WriteLine("  LASER ARRAY VALIDATION — TRM/TQM");
            writer.WriteLine("  Weizmann Institute 400-laser array data");
            writer.WriteLine("═══════════════════════════════════════════");
            writer.WriteLine();
            writer.WriteLine(string.Format(inv,
                "  Synchronization threshold  : {0}",
                CheckOrCross(result.ThresholdConfirmed)));
            writer.WriteLine(string.Format(inv,
                "  Scaling collapse (ratio)   : {0}",
                CheckOrCross(result.TopologyConsistent)));
            writer.WriteLine(string.Format(inv,
                "  Cluster scaling (~1/r²)    : {0}",
                CheckOrCross(result.ScalingConfirmed)));
            writer.WriteLine();
            writer.WriteLine("  Interpretation:");
            writer.WriteLine("  " + result.Interpretation);
            writer.WriteLine();
        }

        private static string CheckOrCross(bool value) => value ? "✔ PASS" : "✘ FAIL";
    }
}
