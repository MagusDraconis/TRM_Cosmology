using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ScottPlot;

namespace TRM.CMD
{
    // ── Multi-Curve Result ─────────────────────────────────────────

    /// <summary>
    /// Holds per-curve analysis results plus cross-curve comparison metrics.
    /// </summary>
    public class MultiCurveResult
    {
        public CsvAnalysisResult CurveK025 { get; set; } = new();
        public CsvAnalysisResult CurveK012 { get; set; } = new();

        /// <summary>RMS difference between the two curves on a common x-grid
        /// (normalised by the y-range). Smaller = better collapse.</summary>
        public double CollapseRms { get; set; }

        /// <summary>Ratio of thresholds: threshold_k025 / threshold_k012.
        /// Near 1.0 suggests scaling with Omega_RMS/K.</summary>
        public double ThresholdRatio { get; set; }

        /// <summary>Ratio of tail a-coefficients: a_k025 / a_k012.</summary>
        public double TailARatio { get; set; }

        public string Verdict { get; set; } = string.Empty;
    }

    // ── Multi-Curve Analyser ──────────────────────────────────────

    /// <summary>
    /// Compares two digitised laser-array curves (K = 0.25 and K = 0.12)
    /// to test whether the synchronisation metric collapses onto a single
    /// master curve when plotted against Omega_RMS / K.
    ///
    /// Reuses LaserCsvAnalyzer for per-curve threshold, tail-fit, and
    /// descriptive statistics.
    /// </summary>
    public static class LaserArrayMultiCurveAnalyzer
    {
        // ── File Paths ─────────────────────────────────────────────

        public static string PathK025 => Path.Combine(LaserDataPath.InputDir, "figure3_k025.csv");
        public static string PathK012 => Path.Combine(LaserDataPath.InputDir, "figure3_k012.csv");

        // ── Curve Comparison ───────────────────────────────────────

        /// <summary>
        /// Computes the RMS difference between two curves by linearly
        /// interpolating both onto a common x-grid.
        /// The result is normalised by the pooled y-range so that
        /// 0 = perfect collapse, 1 = full-scale scatter.
        /// </summary>
        public static double ComputeCollapseRms(List<DataPoint> a, List<DataPoint> b,
            int gridPoints = 100, double tailStart = 1.2)
        {
            // Only compare in the tail region where both curves have data
            double xMin = Math.Max(
                a.Where(p => p.X > tailStart).Min(p => p.X),
                b.Where(p => p.X > tailStart).Min(p => p.X));
            double xMax = Math.Min(
                a.Max(p => p.X),
                b.Max(p => p.X));

            if (xMin >= xMax) return double.NaN;

            double[] grid = Enumerable.Range(0, gridPoints)
                .Select(i => xMin + (xMax - xMin) * i / (gridPoints - 1.0))
                .ToArray();

            double[] yA = grid.Select(x => Interpolate(a, x)).ToArray();
            double[] yB = grid.Select(x => Interpolate(b, x)).ToArray();

            double ss = 0;
            int valid = 0;
            for (int i = 0; i < gridPoints; i++)
            {
                if (double.IsNaN(yA[i]) || double.IsNaN(yB[i])) continue;
                double d = yA[i] - yB[i];
                ss += d * d;
                valid++;
            }

            if (valid == 0) return double.NaN;

            double rmsRaw = Math.Sqrt(ss / valid);

            // Normalise by pooled y-range of both curves in the overlap region
            double yAllMin = Math.Min(a.Min(p => p.Y), b.Min(p => p.Y));
            double yAllMax = Math.Max(a.Max(p => p.Y), b.Max(p => p.Y));
            double yRange = yAllMax - yAllMin;
            if (yRange < 1e-12) return 0.0;

            return rmsRaw / yRange;
        }

        /// <summary>
        /// Linear interpolation of a sorted-by-x list at a given x value.
        /// Returns NaN if x is outside the data range.
        /// </summary>
        private static double Interpolate(List<DataPoint> data, double x)
        {
            if (data.Count < 2) return double.NaN;

            // Find bracketing segment
            if (x < data[0].X || x > data[^1].X) return double.NaN;

            for (int i = 1; i < data.Count; i++)
            {
                if (x <= data[i].X)
                {
                    var lo = data[i - 1];
                    var hi = data[i];
                    double t = (x - lo.X) / (hi.X - lo.X);
                    return lo.Y + t * (hi.Y - lo.Y);
                }
            }

            return double.NaN;
        }

        // ── Full Pipeline ──────────────────────────────────────────

        /// <summary>
        /// Loads both CSVs, runs per-curve analysis, and computes
        /// cross-curve comparison metrics (collapse RMS, threshold ratio,
        /// tail-a ratio).
        /// </summary>
        public static MultiCurveResult Analyze(double tailStart = 1.2)
        {
            var r = new MultiCurveResult();

            // Per-curve analysis (reuses LaserCsvAnalyzer)
            r.CurveK025 = LaserCsvAnalyzer.Analyze(PathK025, tailStart);
            r.CurveK012 = LaserCsvAnalyzer.Analyze(PathK012, tailStart);

            // Reload raw data for comparison (already sorted by X)
            var raw025 = LaserCsvAnalyzer.LoadCsv(PathK025);
            var raw012 = LaserCsvAnalyzer.LoadCsv(PathK012);

            // Collapse RMS
            r.CollapseRms = ComputeCollapseRms(raw025, raw012, tailStart: tailStart);

            // Threshold ratio (if both have valid thresholds)
            if (!double.IsNaN(r.CurveK025.Threshold) && !double.IsNaN(r.CurveK012.Threshold)
                && Math.Abs(r.CurveK012.Threshold) > 1e-12)
                r.ThresholdRatio = r.CurveK025.Threshold / r.CurveK012.Threshold;

            // Tail-a ratio
            if (!double.IsNaN(r.CurveK025.TailA) && !double.IsNaN(r.CurveK012.TailA)
                && Math.Abs(r.CurveK012.TailA) > 1e-12)
                r.TailARatio = r.CurveK025.TailA / r.CurveK012.TailA;

            // Verdict
            r.Verdict = BuildVerdict(r);

            return r;
        }

        // ── Verdict ────────────────────────────────────────────────

        private static string BuildVerdict(MultiCurveResult r)
        {
            var parts = new List<string>();

            // Collapse quality
            if (double.IsNaN(r.CollapseRms))
                parts.Add("COLLAPSE: cannot compute (insufficient tail overlap).");
            else if (r.CollapseRms < 0.08)
                parts.Add(string.Format(CultureInfo.InvariantCulture,
                    "COLLAPSE: RMS = {0:F3} — excellent; curves overlap when scaled by Omega_RMS/K.",
                    r.CollapseRms));
            else if (r.CollapseRms < 0.15)
                parts.Add(string.Format(CultureInfo.InvariantCulture,
                    "COLLAPSE: RMS = {0:F3} — moderate; some deviation between K=0.25 and K=0.12.",
                    r.CollapseRms));
            else
                parts.Add(string.Format(CultureInfo.InvariantCulture,
                    "COLLAPSE: RMS = {0:F3} — poor; curves do NOT scale cleanly with Omega_RMS/K alone.",
                    r.CollapseRms));

            // Threshold ratio
            if (!double.IsNaN(r.ThresholdRatio))
            {
                if (Math.Abs(r.ThresholdRatio - 1.0) < 0.3)
                    parts.Add("THRESHOLD RATIO ≈ 1 — consistent with K/σ_ω scaling law.");
                else
                    parts.Add(string.Format(CultureInfo.InvariantCulture,
                        "THRESHOLD RATIO = {0:F2} — deviates from unity; other physics may contribute.",
                        r.ThresholdRatio));
            }

            // Tail-a ratio
            if (!double.IsNaN(r.TailARatio))
            {
                if (Math.Abs(r.TailARatio - 1.0) < 0.5)
                    parts.Add("TAIL-a RATIO ≈ 1 — both curves follow similar ~1/x² decay.");
                else
                    parts.Add(string.Format(CultureInfo.InvariantCulture,
                        "TAIL-a RATIO = {0:F2} — amplitudes differ; effective cluster size may depend on K.",
                        r.TailARatio));
            }

            // Final
            bool collapseGood = !double.IsNaN(r.CollapseRms) && r.CollapseRms < 0.10;
            bool thresholdGood = !double.IsNaN(r.ThresholdRatio)
                && Math.Abs(r.ThresholdRatio - 1.0) < 0.3;
            if (collapseGood && thresholdGood)
                parts.Add("FINAL: Strong evidence for scaling collapse. "
                    + "TRM/TQM predicts that synchronisation is governed by K/σ_ω alone.");
            else if (collapseGood || thresholdGood)
                parts.Add("FINAL: Partial support — some TRM/TQM predictions hold; "
                    + "extensions (A+D) may improve collapse.");
            else
                parts.Add("FINAL: Weak collapse — amplitude or gain/loss dynamics likely required.");

            return string.Join("  ", parts);
        }

        // ── Plotting ───────────────────────────────────────────────

        /// <summary>
        /// Generates a dual-curve comparison plot and saves it as PNG.
        /// Returns the file path of the saved plot.
        /// </summary>
        public static string PlotComparison(string outputPath, double tailStart = 1.2)
        {
            var raw025 = LaserCsvAnalyzer.LoadCsv(PathK025);
            var raw012 = LaserCsvAnalyzer.LoadCsv(PathK012);

            var r = Analyze(tailStart);

            var plt = new Plot();

            // Raw data
            var s025 = plt.Add.Scatter(
                raw025.Select(p => p.X).ToArray(),
                raw025.Select(p => p.Y).ToArray());
            s025.LegendText = "K = 0.25";
            s025.MarkerSize = 4f;
            s025.LineWidth = 0f;
            s025.Color = ScottPlot.Color.FromHex("#1f77b4");

            var s012 = plt.Add.Scatter(
                raw012.Select(p => p.X).ToArray(),
                raw012.Select(p => p.Y).ToArray());
            s012.LegendText = "K = 0.12";
            s012.MarkerSize = 4f;
            s012.LineWidth = 0f;
            s012.Color = ScottPlot.Color.FromHex("#ff7f0e");

            // Tail fits (dashed lines)
            double xFitMin = tailStart;
            double xFitMax = Math.Max(raw025.Max(p => p.X), raw012.Max(p => p.X));
            int nFit = 80;
            double[] xFit = Enumerable.Range(0, nFit)
                .Select(i => xFitMin + (xFitMax - xFitMin) * i / (nFit - 1.0))
                .ToArray();

            if (!double.IsNaN(r.CurveK025.TailA))
            {
                double[] yFit025 = xFit.Select(x => r.CurveK025.TailA / (x * x) + r.CurveK025.TailB).ToArray();
                var f025 = plt.Add.Scatter(xFit, yFit025);
                f025.LegendText = string.Format(CultureInfo.InvariantCulture,
                    "K=0.25 fit (a={0:F3})", r.CurveK025.TailA);
                f025.LineWidth = 1.5f;
                f025.LinePattern = LinePattern.Dashed;
                f025.Color = ScottPlot.Color.FromHex("#1f77b4");
            }

            if (!double.IsNaN(r.CurveK012.TailA))
            {
                double[] yFit012 = xFit.Select(x => r.CurveK012.TailA / (x * x) + r.CurveK012.TailB).ToArray();
                var f012 = plt.Add.Scatter(xFit, yFit012);
                f012.LegendText = string.Format(CultureInfo.InvariantCulture,
                    "K=0.12 fit (a={0:F3})", r.CurveK012.TailA);
                f012.LineWidth = 1.5f;
                f012.LinePattern = LinePattern.Dashed;
                f012.Color = ScottPlot.Color.FromHex("#ff7f0e");
            }

            // Threshold marker (y = 0.5)
            var hline = plt.Add.HorizontalLine(0.5);
            hline.LineWidth = 1f;
            hline.LinePattern = LinePattern.Dotted;
            hline.Color = ScottPlot.Color.FromHex("#888888");
            hline.LegendText = "y = 0.5 (threshold)";

            plt.Title("Scaling Collapse: SyncMetric vs Omega_RMS / K");
            plt.XLabel("Omega_RMS / K");
            plt.YLabel("SyncMetric (IPR normalised)");
            plt.Axes.SetLimits(-0.05f, 2.5f, -0.05f, 1.05f);
            plt.ShowLegend();

            plt.SavePng(outputPath, 1200, 800);

            return outputPath;
        }

        /// <summary>
        /// Generates a residual plot (difference between the two curves
        /// on the common interpolation grid).
        /// </summary>
        public static string PlotResiduals(string outputPath, double tailStart = 1.2)
        {
            var raw025 = LaserCsvAnalyzer.LoadCsv(PathK025);
            var raw012 = LaserCsvAnalyzer.LoadCsv(PathK012);

            double xMin = Math.Max(
                raw025.Where(p => p.X > tailStart).Min(p => p.X),
                raw012.Where(p => p.X > tailStart).Min(p => p.X));
            double xMax = Math.Min(
                raw025.Max(p => p.X),
                raw012.Max(p => p.X));

            int n = 100;
            double[] xs = Enumerable.Range(0, n)
                .Select(i => xMin + (xMax - xMin) * i / (n - 1.0))
                .ToArray();
            double[] diffs = xs.Select(x => Interpolate(raw025, x) - Interpolate(raw012, x)).ToArray();

            var plt = new Plot();

            var scatter = plt.Add.Scatter(xs, diffs);
            scatter.MarkerSize = 3f;
            scatter.LineWidth = 1f;
            scatter.Color = ScottPlot.Color.FromHex("#d62728");

            // Zero line
            var zero = plt.Add.HorizontalLine(0);
            zero.LineWidth = 1f;
            zero.LinePattern = LinePattern.Dotted;
            zero.Color = ScottPlot.Color.FromHex("#888888");

            // ± collapse RMS band
            var r = Analyze(tailStart);
            if (!double.IsNaN(r.CollapseRms))
            {
                double band = r.CollapseRms
                    * (Math.Max(raw025.Max(p => p.Y), raw012.Max(p => p.Y))
                     - Math.Min(raw025.Min(p => p.Y), raw012.Min(p => p.Y)));
                var hPlus = plt.Add.HorizontalLine(band);
                hPlus.LineWidth = 0.8f;
                hPlus.LinePattern = LinePattern.Dotted;
                hPlus.Color = ScottPlot.Color.FromHex("#aaaaaa");
                var hMinus = plt.Add.HorizontalLine(-band);
                hMinus.LineWidth = 0.8f;
                hMinus.LinePattern = LinePattern.Dotted;
                hMinus.Color = ScottPlot.Color.FromHex("#aaaaaa");
            }

            plt.Title(string.Format(CultureInfo.InvariantCulture,
                "Residuals: K=0.25 minus K=0.12  (collapse RMS = {0:F3})",
                r.CollapseRms));
            plt.XLabel("Omega_RMS / K");
            plt.YLabel("Delta SyncMetric");

            plt.SavePng(outputPath, 1200, 800);

            return outputPath;
        }

        // ── CSV Output ─────────────────────────────────────────────

        /// <summary>
        /// Writes a comparison CSV with columns:
        ///   metric, k025, k012, ratio, interpretation
        /// </summary>
        public static string SaveComparisonCsv(string outputPath, double tailStart = 1.2)
        {
            var r = Analyze(tailStart);
            var ci = CultureInfo.InvariantCulture;

            using var writer = new StreamWriter(outputPath);

            writer.WriteLine("metric,k025,k012,ratio,interpretation");

            writer.WriteLine(string.Format(ci,
                "point_count,{0},{1},,",
                r.CurveK025.PointCount, r.CurveK012.PointCount));

            writer.WriteLine(string.Format(ci,
                "threshold_x,{0:F4},{1:F4},{2:F3},{3}",
                r.CurveK025.Threshold, r.CurveK012.Threshold,
                r.ThresholdRatio,
                Math.Abs(r.ThresholdRatio - 1.0) < 0.3 ? "consistent_with_unity" : "deviates"));

            writer.WriteLine(string.Format(ci,
                "tail_a,{0:F4},{1:F4},{2:F3},{3}",
                r.CurveK025.TailA, r.CurveK012.TailA,
                r.TailARatio,
                Math.Abs(r.TailARatio - 1.0) < 0.5 ? "similar_amplitudes" : "different_amplitudes"));

            writer.WriteLine(string.Format(ci,
                "tail_b,{0:F4},{1:F4},,",
                r.CurveK025.TailB, r.CurveK012.TailB));

            writer.WriteLine(string.Format(ci,
                "r_squared,{0:F4},{1:F4},,",
                r.CurveK025.RSquared, r.CurveK012.RSquared));

            writer.WriteLine(string.Format(ci,
                "collapse_rms,{0:F4},,,{1}",
                r.CollapseRms,
                r.CollapseRms < 0.10 ? "excellent" : r.CollapseRms < 0.15 ? "moderate" : "poor"));

            writer.WriteLine(string.Format(ci,
                "y_range,{0:F4},{1:F4},,",
                r.CurveK025.YMax - r.CurveK025.YMin,
                r.CurveK012.YMax - r.CurveK012.YMin));

            Console.WriteLine("  Comparison CSV saved: " + outputPath);
            return outputPath;
        }

        // ── Console Output ─────────────────────────────────────────

        /// <summary>
        /// Prints the full multi-curve analysis to the console,
        /// including per-curve summaries, comparison metrics, and verdict.
        /// </summary>
        public static void PrintSummary(MultiCurveResult r)
        {
            Console.WriteLine("  ╔══════════════════════════════════════════╗");
            Console.WriteLine("  ║  MULTI-CURVE LASER ARRAY ANALYSIS        ║");
            Console.WriteLine("  ╚══════════════════════════════════════════╝");
            Console.WriteLine();

            // --- Per-curve ---
            Console.WriteLine("  ── K = 0.25 (figure3_k025.csv) ──");
            PrintCompact(r.CurveK025);
            Console.WriteLine();
            Console.WriteLine("  ── K = 0.12 (figure3_k012.csv) ──");
            PrintCompact(r.CurveK012);
            Console.WriteLine();

            // --- Cross-curve ---
            Console.WriteLine("  ═══════════════════════════════════════════");
            Console.WriteLine("  CROSS-CURVE COMPARISON");
            Console.WriteLine("  ═══════════════════════════════════════════");
            Console.WriteLine();

            var ci = CultureInfo.InvariantCulture;

            Console.WriteLine(string.Format(ci,
                "  Collapse RMS     : {0:F4}  (0 = perfect, < 0.10 = excellent)",
                r.CollapseRms));
            Console.WriteLine(string.Format(ci,
                "  Threshold ratio  : {0:F3}  (≈ 1.0 implies K/σ_ω scaling)",
                r.ThresholdRatio));
            Console.WriteLine(string.Format(ci,
                "  Tail-a ratio     : {0:F3}  (≈ 1.0 implies same ~1/x² amplitude)",
                r.TailARatio));

            Console.WriteLine();
            Console.WriteLine("  ── VERDICT ──");
            Console.WriteLine("  " + r.Verdict);
        }

        private static void PrintCompact(CsvAnalysisResult r)
        {
            var ci = CultureInfo.InvariantCulture;
            Console.WriteLine(string.Format(ci,
                "    Points: {0}   Threshold: {1:F3}   Tail R²: {2:F3}",
                r.PointCount, r.Threshold, r.RSquared));
            Console.WriteLine(string.Format(ci,
                "    Y range: [{0:F3}, {1:F3}]   Tail a = {2:F4}",
                r.YMin, r.YMax, r.TailA));
        }

        // ── Convenience Runner ─────────────────────────────────────

        /// <summary>
        /// Runs the full pipeline (analysis, plots, CSV) and prints
        /// everything to the console.  Designed to be called from
        /// the Program.cs menu entry.
        /// </summary>
        public static void RunAndPrintAll()
        {
            Console.WriteLine("  Data directory : " + LaserDataPath.InputDir);
            Console.WriteLine("  Results dir    : " + LaserDataPath.ResultsDir);
            Console.WriteLine();

            if (!File.Exists(PathK025))
            {
                Console.WriteLine("  ERROR: " + PathK025 + " not found.");
                return;
            }
            if (!File.Exists(PathK012))
            {
                Console.WriteLine("  ERROR: " + PathK012 + " not found.");
                return;
            }

            double tailStart = 1.2;

            // Analysis
            var result = Analyze(tailStart);
            PrintSummary(result);

            // Plots
            Console.WriteLine();
            Console.WriteLine("  Generating plots...");

            string comparisonPath = LaserDataPath.UniquePlotPath("comparison_plot.png");
            PlotComparison(comparisonPath, tailStart);
            Console.WriteLine("    Comparison plot : " + comparisonPath);

            string residualPath = LaserDataPath.UniquePlotPath("residual_plot.png");
            PlotResiduals(residualPath, tailStart);
            Console.WriteLine("    Residual plot   : " + residualPath);

            // Comparison CSV
            string csvPath = LaserDataPath.UniqueResultPath("comparison_metrics.csv");
            SaveComparisonCsv(csvPath, tailStart);

            // Open plots
            Console.WriteLine();
            Console.WriteLine("  Opening comparison plot...");
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(comparisonPath) { UseShellExecute = true }); }
            catch { /* non-interactive — skip */ }
        }
    }
}
