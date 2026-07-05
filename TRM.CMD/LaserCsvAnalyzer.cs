using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace TRM.CMD
{
    // ── Data Model ─────────────────────────────────────────────────

    /// <summary>
    /// Single data point from a digitized laser-array paper curve.
    /// </summary>
    public class DataPoint
    {
        /// <summary>Omega_RMS / K (x-axis of the paper figure).</summary>
        public double X { get; set; }
        /// <summary>Sync metric, e.g. IPR (y-axis of the paper figure).</summary>
        public double Y { get; set; }
    }

    /// <summary>
    /// Results produced by the CSV-based laser-array analysis pipeline.
    /// </summary>
    public class CsvAnalysisResult
    {
        public double Threshold { get; set; }
        public double TailA { get; set; }
        public double TailB { get; set; }
        public double RSquared { get; set; }
        public int PointCount { get; set; }
        public double MeanX { get; set; }
        public double MeanY { get; set; }
        public double MedianX { get; set; }
        public double MedianY { get; set; }
        public double YMax { get; set; }
        public double YMin { get; set; }
        public string Interpretation { get; set; } = string.Empty;
    }

    // ── Analyser ───────────────────────────────────────────────────

    /// <summary>
    /// Loads digitized paper data from CSV and computes:
    ///   - synchronization threshold (y = 0.5 crossing)
    ///   - tail fit (y ~ a/x^2 + b for x > 1.2)
    ///   - R² goodness-of-fit for the tail model
    ///   - basic descriptive statistics
    /// </summary>
    public static class LaserCsvAnalyzer
    {
        // ── CSV Loader ─────────────────────────────────────────────

        /// <summary>
        /// Loads a two-column CSV (x, y) with an optional header row.
        /// Skips non-numeric rows silently.
        /// </summary>
        public static List<DataPoint> LoadCsv(string path)
        {
            var lines = File.ReadAllLines(path);
            var data = new List<DataPoint>();

            foreach (var line in lines)
            {
                var parts = line.Split(',');
                if (parts.Length < 2) continue;

                if (!double.TryParse(parts[0].Trim(), NumberStyles.Float,
                        CultureInfo.InvariantCulture, out double x))
                    continue;
                if (!double.TryParse(parts[1].Trim(), NumberStyles.Float,
                        CultureInfo.InvariantCulture, out double y))
                    continue;

                data.Add(new DataPoint { X = x, Y = y });
            }

            // Sort by X so threshold search and tail selection are reliable
            data.Sort((a, b) => a.X.CompareTo(b.X));

            return data;
        }

        // ── Threshold Detection (y = 0.5) ──────────────────────────

        /// <summary>
        /// Finds the x-value where the curve crosses y = 0.5 via
        /// linear interpolation between bracketing data points.
        /// Returns NaN if no crossing is found.
        /// </summary>
        public static double FindThreshold(List<DataPoint> data)
        {
            for (int i = 1; i < data.Count; i++)
            {
                var p1 = data[i - 1];
                var p2 = data[i];

                // Check for crossing (handle both directions)
                if ((p1.Y >= 0.5 && p2.Y <= 0.5) ||
                    (p1.Y <= 0.5 && p2.Y >= 0.5))
                {
                    if (Math.Abs(p2.Y - p1.Y) < 1e-12)
                        return p1.X;   // degenerate flat segment — avoid div-by-zero

                    // Linear interpolation
                    double t = (0.5 - p1.Y) / (p2.Y - p1.Y);
                    return p1.X + t * (p2.X - p1.X);
                }
            }

            return double.NaN;
        }

        // ── Tail Fit ───────────────────────────────────────────────

        /// <summary>
        /// Fits y = a / x^2 + b to the tail region (x > 1.2) using
        /// ordinary least-squares on the transformed variable z = 1/x^2.
        /// Returns (a, b) coefficients.
        /// </summary>
        public static (double a, double b) FitTail(List<DataPoint> data,
            double tailStart = 1.2)
        {
            var tail = data.Where(p => p.X > tailStart).ToList();

            if (tail.Count < 2)
                return (double.NaN, double.NaN);

            // Transform: y = a * z + b   where z = 1/x^2
            var z = tail.Select(p => 1.0 / (p.X * p.X)).ToArray();
            var y = tail.Select(p => p.Y).ToArray();

            int n = z.Length;

            double sumZ = z.Sum();
            double sumY = y.Sum();
            double sumZZ = z.Select(v => v * v).Sum();
            double sumZY = z.Zip(y, (zi, yi) => zi * yi).Sum();

            double denom = n * sumZZ - sumZ * sumZ;
            if (Math.Abs(denom) < 1e-12)
                return (double.NaN, double.NaN);

            double a = (n * sumZY - sumZ * sumY) / denom;
            double b = (sumY - a * sumZ) / n;

            return (a, b);
        }

        // ── R² Calculation ─────────────────────────────────────────

        /// <summary>
        /// Computes the coefficient of determination R² for the
        /// tail model y = a / x^2 + b over data points with x > tailStart.
        /// R² = 1 - SS_res / SS_tot
        /// </summary>
        public static double ComputeRSquared(List<DataPoint> data,
            double a, double b, double tailStart = 1.2)
        {
            var tail = data.Where(p => p.X > tailStart).ToList();
            if (tail.Count < 2) return double.NaN;

            double yMean = tail.Average(p => p.Y);

            double ssRes = tail.Sum(p =>
            {
                double pred = a / (p.X * p.X) + b;
                return (p.Y - pred) * (p.Y - pred);
            });

            double ssTot = tail.Sum(p => (p.Y - yMean) * (p.Y - yMean));

            if (ssTot < 1e-12) return 1.0;   // perfect fit (all y equal)

            return 1.0 - ssRes / ssTot;
        }

        // ── Basic Statistics ───────────────────────────────────────

        /// <summary>
        /// Computes descriptive statistics on the loaded data set.
        /// </summary>
        public static (double meanX, double meanY, double medianX, double medianY,
            double yMax, double yMin) ComputeStats(List<DataPoint> data)
        {
            int n = data.Count;
            if (n == 0) return (0, 0, 0, 0, 0, 0);

            double meanX = data.Average(p => p.X);
            double meanY = data.Average(p => p.Y);

            var sortedX = data.Select(p => p.X).OrderBy(x => x).ToList();
            var sortedY = data.Select(p => p.Y).OrderBy(y => y).ToList();
            double medianX = sortedX[n / 2];
            double medianY = sortedY[n / 2];

            double yMax = data.Max(p => p.Y);
            double yMin = data.Min(p => p.Y);

            return (meanX, meanY, medianX, medianY, yMax, yMin);
        }

        // ── Interpretation ─────────────────────────────────────────

        /// <summary>
        /// Generates a human-readable interpretation string based on
        /// the computed metrics.
        /// </summary>
        public static string Interpret(CsvAnalysisResult r)
        {
            var parts = new List<string>();

            // Threshold assessment
            if (double.IsNaN(r.Threshold))
                parts.Add("THRESHOLD: no crossing at y=0.5 detected — "
                    + "sync may remain high or start low across full range.");
            else if (r.Threshold > 0.5 && r.Threshold < 1.5)
                parts.Add(string.Format(CultureInfo.InvariantCulture,
                    "THRESHOLD: y=0.5 crossing at x ≈ {0:F2} — "
                    + "consistent with Kuramoto critical ratio K/σ_ω ≈ 1.",
                    r.Threshold));
            else
                parts.Add(string.Format(CultureInfo.InvariantCulture,
                    "THRESHOLD: y=0.5 crossing at x ≈ {0:F2} — "
                    + "deviates from Kuramoto expectation (~1).",
                    r.Threshold));

            // Tail quality
            if (double.IsNaN(r.RSquared))
                parts.Add("TAIL FIT: insufficient tail data (need X > 1.2).");
            else if (r.RSquared > 0.8)
                parts.Add(string.Format(CultureInfo.InvariantCulture,
                    "TAIL FIT: R² = {0:F3} — strong ~1/x² decay, "
                    + "consistent with cluster-size scaling prediction.",
                    r.RSquared));
            else if (r.RSquared > 0.5)
                parts.Add(string.Format(CultureInfo.InvariantCulture,
                    "TAIL FIT: R² = {0:F3} — moderate ~1/x² decay.",
                    r.RSquared));
            else
                parts.Add(string.Format(CultureInfo.InvariantCulture,
                    "TAIL FIT: R² = {0:F3} — poor ~1/x² decay, "
                    + "other physics may dominate tail.",
                    r.RSquared));

            // Overall verdict
            if (!double.IsNaN(r.Threshold) && r.Threshold > 0.5 && r.Threshold < 1.5
                && !double.IsNaN(r.RSquared) && r.RSquared > 0.7)
                parts.Add("OVERALL: Digitized data supports both Kuramoto threshold "
                    + "and cluster-size scaling. TRM/TQM structure consistent.");
            else if (!double.IsNaN(r.Threshold) && r.Threshold > 0.5 && r.Threshold < 1.5)
                parts.Add("OVERALL: Threshold consistent; tail scaling inconclusive.");
            else
                parts.Add("OVERALL: Data does not clearly support phase-only model. "
                    + "Extension analysis (A+D) recommended.");

            return string.Join("  ", parts);
        }

        // ── Full Pipeline ──────────────────────────────────────────

        /// <summary>
        /// Runs the complete analysis pipeline on a given CSV file and
        /// returns a populated result object.
        /// </summary>
        public static CsvAnalysisResult Analyze(string csvPath,
            double tailStart = 1.2)
        {
            var data = LoadCsv(csvPath);

            var result = new CsvAnalysisResult { PointCount = data.Count };

            if (data.Count == 0)
            {
                result.Interpretation = "No valid data points loaded.";
                return result;
            }

            // Descriptive stats
            (result.MeanX, result.MeanY, result.MedianX, result.MedianY,
                result.YMax, result.YMin) = ComputeStats(data);

            // Threshold
            result.Threshold = FindThreshold(data);

            // Tail fit
            (result.TailA, result.TailB) = FitTail(data, tailStart);
            result.RSquared = ComputeRSquared(data, result.TailA, result.TailB, tailStart);

            result.Interpretation = Interpret(result);

            return result;
        }

        // ── Console Output ─────────────────────────────────────────

        /// <summary>
        /// Prints the analysis results to the console in a formatted block.
        /// </summary>
        public static void PrintSummary(CsvAnalysisResult r)
        {
            var ci = CultureInfo.InvariantCulture;

            Console.WriteLine("  ── CSV DATA STATISTICS ──");
            Console.WriteLine(string.Format(ci,
                "  Points    : {0}", r.PointCount));
            Console.WriteLine(string.Format(ci,
                "  Mean  (x, y) : ({0:F3}, {1:F3})", r.MeanX, r.MeanY));
            Console.WriteLine(string.Format(ci,
                "  Median (x, y) : ({0:F3}, {1:F3})", r.MedianX, r.MedianY));
            Console.WriteLine(string.Format(ci,
                "  Y range : [{0:F3}, {1:F3}]", r.YMin, r.YMax));
            Console.WriteLine();

            Console.WriteLine("  ── THRESHOLD (y = 0.5) ──");
            if (double.IsNaN(r.Threshold))
                Console.WriteLine("  No crossing found.");
            else
                Console.WriteLine(string.Format(ci,
                    "  x_threshold ≈ {0:F3}   (expected ~0.8–1.2 for Kuramoto)",
                    r.Threshold));
            Console.WriteLine();

            Console.WriteLine("  ── TAIL FIT (x > 1.2) ──");
            Console.WriteLine(string.Format(ci,
                "  Model  : y = a / x² + b"));
            Console.WriteLine(string.Format(ci,
                "  a      : {0:F4}", r.TailA));
            Console.WriteLine(string.Format(ci,
                "  b      : {0:F4}", r.TailB));
            Console.WriteLine(string.Format(ci,
                "  R²     : {0:F4}", r.RSquared));
            Console.WriteLine();

            Console.WriteLine("  ── INTERPRETATION ──");
            Console.WriteLine("  " + r.Interpretation);
        }
    }
}
