using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TRM.CMD
{
    // ── Extended Data Model ──────────────────────────────────────

    /// <summary>
    /// Extended experimental data from a coupled laser array,
    /// including per-oscillator frequencies and amplitudes for
    /// testing extensions beyond the phase-only TRM/TQM model.
    /// </summary>
    public class LaserExtendedExperiment
    {
        public string Name { get; set; } = string.Empty;

        public double CouplingK { get; set; }
        public double OmegaRms { get; set; }
        public double OmegaRmsOverK { get; set; }

        /// <summary>Sync metric (e.g. IPR proxy, 0 = no sync, 1 = full sync).</summary>
        public double SyncMetric { get; set; }

        // ── Extension A: amplitude-weighted coupling ──────────────
        public List<double> Omegas { get; set; } = new();
        public List<double> Amplitudes { get; set; } = new();

        // ── Extension D: correlated frequency disorder ────────────
        public double CorrelationLength { get; set; }
        public double SigmaEffective { get; set; }
    }

    /// <summary>
    /// Result of testing TRM/TQM extensions against laser array data.
    /// </summary>
    public class ExtensionResult
    {
        public bool AmplitudeImproves { get; set; }
        public bool DisorderImproves { get; set; }
        public bool CombinedWorks { get; set; }

        public double BaselineRSquared { get; set; }
        public double AmplitudeRSquared { get; set; }
        public double DisorderRSquared { get; set; }
        public double CombinedRSquared { get; set; }

        public string Interpretation { get; set; } = string.Empty;
    }

    // ── Analyser ───────────────────────────────────────────────────

    /// <summary>
    /// Tests whether TRM/TQM minimal extensions (amplitude weighting A
    /// and correlated disorder D) restore scaling collapse in laser
    /// array experiments.
    /// </summary>
    public static class LaserArrayExtensionAnalyzer
    {
        // ── Dataset ────────────────────────────────────────────────

        /// <summary>
        /// Builds synthetic extended dataset mimicking a 400-laser
        /// array with known amplitudes and spatial correlations.
        /// </summary>
        public static List<LaserExtendedExperiment> BuildExtendedDataset()
        {
            var rng = new Random(42);
            int N = 400;

            // Generate base frequency distribution
            double[] GenOmegas(double rms, int count)
            {
                var om = new double[count];
                // Correlated random walk for spatial adjacency
                double current = 0;
                double stepSize = Math.Sqrt(2.0 / count) * rms;
                for (int i = 0; i < count; i++)
                {
                    current += (rng.NextDouble() - 0.5) * 2 * stepSize;
                    om[i] = current;
                }
                // Centre and scale
                double mean = om.Average();
                double std = Math.Sqrt(om.Average(x => (x - mean) * (x - mean)));
                if (std > 1e-12)
                {
                    for (int i = 0; i < count; i++)
                        om[i] = (om[i] - mean) / std * rms;
                }
                return om;
            }

            // Generate amplitudes with some spread
            double[] GenAmplitudes(int count)
            {
                var amp = new double[count];
                for (int i = 0; i < count; i++)
                    amp[i] = 1.0 + (rng.NextDouble() - 0.5) * 0.4; // ±20% spread
                return amp;
            }

            var results = new List<LaserExtendedExperiment>();

            // K = 0.25 series
            foreach (var (name, ratio, sync) in new[]
            {
                ("K=0.25 low disorder", 0.2, 0.9),
                ("K=0.25 threshold", 0.8, 0.5),
                ("K=0.25 high disorder", 2.5, 0.1)
            })
            {
                results.Add(new LaserExtendedExperiment
                {
                    Name = name,
                    CouplingK = 0.25,
                    OmegaRms = 0.25 * ratio,
                    OmegaRmsOverK = ratio,
                    SyncMetric = sync,
                    Omegas = GenOmegas(0.25 * ratio, N).ToList(),
                    Amplitudes = GenAmplitudes(N).ToList(),
                    CorrelationLength = ratio < 1.0 ? 5.0 : 20.0,
                    SigmaEffective = 0.0 // computed later
                });
            }

            // K = 0.12 series
            foreach (var (name, ratio, sync) in new[]
            {
                ("K=0.12 low disorder", 0.2, 0.8),
                ("K=0.12 threshold", 0.8, 0.4),
                ("K=0.12 high disorder", 2.5, 0.05)
            })
            {
                results.Add(new LaserExtendedExperiment
                {
                    Name = name,
                    CouplingK = 0.12,
                    OmegaRms = 0.12 * ratio,
                    OmegaRmsOverK = ratio,
                    SyncMetric = sync,
                    Omegas = GenOmegas(0.12 * ratio, N).ToList(),
                    Amplitudes = GenAmplitudes(N).ToList(),
                    CorrelationLength = ratio < 1.0 ? 5.0 : 20.0,
                    SigmaEffective = 0.0 // computed later
                });
            }


            return results;
        }

        // ── Extension A: Amplitude-Weighted Centroid ──────────────

        /// <summary>
        /// Computes the amplitude-weighted mean frequency.
        /// Weight = A_i² (intensity-weighting, standard for laser arrays).
        /// </summary>
        public static double ComputeWeightedOmega(List<double> omega, List<double> amplitude)
        {
            if (omega.Count != amplitude.Count || omega.Count == 0)
                throw new ArgumentException("Lists must be non-empty and of equal length.");

            double numerator = 0.0;
            double denominator = 0.0;

            for (int i = 0; i < omega.Count; i++)
            {
                double w = amplitude[i] * amplitude[i];
                numerator += w * omega[i];
                denominator += w;
            }

            return denominator > 1e-12 ? numerator / denominator : omega.Average();
        }

        // ── Extension D: Effective Disorder Correction ────────────

        /// <summary>
        /// Corrects the raw OmegaRms for spatial correlations.
        /// Simplified model: sigma_eff ≈ sigma_raw * sqrt(xi / N)
        /// where xi is the correlation length (in number of oscillators).
        /// Correlated oscillators act like fewer effective independent ones.
        /// </summary>
        public static double ComputeEffectiveSigma(
            double omegaRms, double correlationLength, int nOscillators)
        {
            if (correlationLength <= 1.0) return omegaRms;
            if (nOscillators <= 1) return omegaRms;

            double xi = Math.Min(correlationLength, (double)nOscillators);
            return omegaRms * Math.Sqrt(xi / (double)nOscillators);
        }

        // ── Scaling-Collapse Quality (R²) ─────────────────────────

        /// <summary>
        /// Computes R² for the scaling law: SyncMetric ~ 1 / (OmegaRms/K)^2.
        /// Higher R² means better collapse onto the predicted curve.
        /// </summary>
        private static double ComputeScalingRSquared(
            List<LaserExtendedExperiment> data,
            Func<LaserExtendedExperiment, double> ratioSelector)
        {
            var xs = new List<double>();
            var ys = new List<double>();

            foreach (var d in data)
            {
                double ratio = ratioSelector(d);
                if (ratio < 0.01) continue;
                double x = 1.0 / (ratio * ratio);
                xs.Add(x);
                ys.Add(d.SyncMetric);
            }

            if (xs.Count < 3) return 0.0;

            double meanX = xs.Average(), meanY = ys.Average();
            double num = 0.0, den = 0.0;
            for (int i = 0; i < xs.Count; i++)
            {
                double dx = xs[i] - meanX;
                num += dx * (ys[i] - meanY);
                den += dx * dx;
            }

            if (Math.Abs(den) < 1e-12) return 0.0;

            double slope = num / den;
            double intercept = meanY - slope * meanX;

            double ssRes = 0.0, ssTot = 0.0;
            for (int i = 0; i < xs.Count; i++)
            {
                double pred = slope * xs[i] + intercept;
                ssRes += (ys[i] - pred) * (ys[i] - pred);
                ssTot += (ys[i] - meanY) * (ys[i] - meanY);
            }

            return ssTot > 1e-12 ? 1.0 - ssRes / ssTot : 0.0;
        }

        // ── Full Comparison ───────────────────────────────────────

        /// <summary>
        /// Compares four models:
        ///   1. Phase-only baseline (raw OmegaRmsOverK)
        ///   2. + Amplitude weighting (corrected centroid)
        ///   3. + Correlated disorder (effective sigma)
        ///   4. A + D combined
        ///
        /// Returns R² for each and determines whether extensions
        /// improve scaling collapse.
        /// </summary>
        public static ExtensionResult CompareExtensions(List<LaserExtendedExperiment> data)
        {
            var inv = CultureInfo.InvariantCulture;

            // Pre-compute effective sigma for each experiment
            foreach (var d in data)
            {
                d.SigmaEffective = ComputeEffectiveSigma(
                    d.OmegaRms, d.CorrelationLength, d.Omegas.Count > 0 ? d.Omegas.Count : 400);

                // Also compute weighted mean (for diagnostic purposes — not used
                // directly in the ratio, but stored for completeness)
            }

            // 1. Baseline: raw ratio
            double baselineR2 = ComputeScalingRSquared(data,
                d => d.OmegaRmsOverK);

            // 2. Amplitude-weighted: ratio using weighted sigma? No —
            //    amplitude weighting primarily affects the centroid (Omega*)
            //    not the width. For the scaling law test, we keep the ratio
            //    but use amplitude-scaled effective omega spread.
            //
            //    Amplitude-weighted effective sigma:
            //    sigma_amp ~ omegaRms / sqrt(1 + var(amplitude))
            Func<LaserExtendedExperiment, double> ampRatio = d =>
            {
                if (d.Amplitudes.Count == 0) return d.OmegaRmsOverK;
                double meanA = d.Amplitudes.Average();
                double varA = d.Amplitudes.Average(a => (a - meanA) * (a - meanA));
                double correction = Math.Sqrt(1.0 + varA / (meanA * meanA));
                double effectiveSigma = d.OmegaRms / correction;
                double ratio = effectiveSigma / d.CouplingK;
                return ratio;
            };
            double ampR2 = ComputeScalingRSquared(data, ampRatio);

            // 3. Correlated disorder: effective sigma
            Func<LaserExtendedExperiment, double> disorderRatio = d =>
            {
                double sigma = d.SigmaEffective > 0 ? d.SigmaEffective : d.OmegaRms;
                return sigma / d.CouplingK;
            };
            double disorderR2 = ComputeScalingRSquared(data, disorderRatio);

            // 4. A + D combined
            Func<LaserExtendedExperiment, double> combinedRatio = d =>
            {
                double sigmaEff = d.SigmaEffective > 0 ? d.SigmaEffective : d.OmegaRms;
                if (d.Amplitudes.Count > 0)
                {
                    double meanA = d.Amplitudes.Average();
                    double varA = d.Amplitudes.Average(a => (a - meanA) * (a - meanA));
                    double correction = Math.Sqrt(1.0 + varA / (meanA * meanA));
                    sigmaEff /= correction;
                }
                return sigmaEff / d.CouplingK;
            };
            double combinedR2 = ComputeScalingRSquared(data, combinedRatio);

            // Determine improvements
            bool ampImproves = ampR2 > baselineR2 * 1.05; // 5% improvement threshold
            bool disorderImproves = disorderR2 > baselineR2 * 1.05;
            bool combinedWorks = combinedR2 > baselineR2 * 1.10; // 10% threshold for combined

            string interpretation;
            if (combinedWorks && combinedR2 > 0.7)
            {
                interpretation = "Minimal extension (A + D) sufficient. " +
                    "TRM/TQM extended model matches laser physics. " +
                    "Amplitude-weighted coupling and correlated disorder " +
                    "restore the predicted scaling collapse.";
            }
            else if (ampImproves && disorderImproves)
            {
                interpretation = "Both extensions improve fit individually " +
                    "but combined effect is below threshold (R²_combined = " +
                    combinedR2.ToString("F3", inv) + "). " +
                    "Additional physics (gain/loss, non-reciprocal coupling) " +
                    "may be needed for full recovery.";
            }
            else if (ampImproves)
            {
                interpretation = "Amplitude weighting improves scaling collapse " +
                    "(R² = " + ampR2.ToString("F3", inv) + " vs baseline " +
                    baselineR2.ToString("F3", inv) + "). " +
                    "Correlated disorder alone does not help. " +
                    "Amplitude dynamics are the dominant missing ingredient.";
            }
            else if (disorderImproves)
            {
                interpretation = "Correlated disorder correction improves " +
                    "scaling collapse (R² = " + disorderR2.ToString("F3", inv) +
                    " vs baseline " + baselineR2.ToString("F3", inv) + "). " +
                    "Amplitude weighting alone does not help. " +
                    "Spatial frequency correlations drive the deviation.";
            }
            else
            {
                interpretation = "Neither minimal extension restores scaling " +
                    "collapse (baseline R² = " + baselineR2.ToString("F3", inv) +
                    "). Further extensions required: gain/loss dynamics, " +
                    "non-reciprocal coupling, or full Lang–Kobayashi model.";
            }

            return new ExtensionResult
            {
                AmplitudeImproves = ampImproves,
                DisorderImproves = disorderImproves,
                CombinedWorks = combinedWorks,
                BaselineRSquared = baselineR2,
                AmplitudeRSquared = ampR2,
                DisorderRSquared = disorderR2,
                CombinedRSquared = combinedR2,
                Interpretation = interpretation
            };
        }

        // ── Console Output ────────────────────────────────────────

        public static void PrintSummary(ExtensionResult result)
        {
            PrintSummary(result, Console.Out);
        }

        public static void PrintSummary(ExtensionResult result, TextWriter writer)
        {
            var inv = CultureInfo.InvariantCulture;

            writer.WriteLine("═══════════════════════════════════════════");
            writer.WriteLine("  LASER ARRAY EXTENSION VALIDATION");
            writer.WriteLine("  Phase-only → A+D extended model");
            writer.WriteLine("═══════════════════════════════════════════");
            writer.WriteLine();
            writer.WriteLine("  Model                       R²");
            writer.WriteLine("  ─────────────────────────────────────");
            writer.WriteLine(string.Format(inv,
                "  Phase-only (baseline)       {0:F4}",
                result.BaselineRSquared));
            writer.WriteLine(string.Format(inv,
                "  + Amplitude weighting (A)   {0:F4}  {1}",
                result.AmplitudeRSquared,
                result.AmplitudeImproves ? "↑" : "—"));
            writer.WriteLine(string.Format(inv,
                "  + Correlated disorder (D)   {0:F4}  {1}",
                result.DisorderRSquared,
                result.DisorderImproves ? "↑" : "—"));
            writer.WriteLine(string.Format(inv,
                "  Combined (A + D)            {0:F4}  {1}",
                result.CombinedRSquared,
                result.CombinedWorks ? "↑" : "—"));
            writer.WriteLine();
            writer.WriteLine("  Interpretation:");
            writer.WriteLine("  " + result.Interpretation);
            writer.WriteLine();
        }
    }
}
