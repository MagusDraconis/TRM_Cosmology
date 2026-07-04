using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace TRM.CMD
{
    /// <summary>
    /// Structure of a single line from a BIPM "uh____dd.ddd" file.
    /// </summary>
    public record ClockMeasurement(
        double Mjd,
        int LabCode,
        int ClockId,
        double OffsetNs,
        string Lab);

    /// <summary>
    /// Time-ordered samples for a single clock.
    /// </summary>
    public record ClockTimeSeries(
        int ClockId,
        List<(double Mjd, double OffsetNs)> Samples);

    /// <summary>
    /// Result of the BIPM clock analysis pipeline.
    /// </summary>
    public record BipmAnalysisResult(
        List<(double Mjd, double Mean, double Std)> Ensemble,
        (double Slope, double Intercept) Drift,
        List<(int ClockI, int ClockJ, int PairSize, double Variance)> Pairwise,
        bool DetectedGlobalDrift,
        double DriftSlopeNsPerDay);

    /// <summary>
    /// Analyzes BIPM UTCr clock data to detect global common-mode drift
    /// (candidate B(t) from BB13–BB16).
    ///
    /// Pipeline:
    ///   1. Load all uh____* files from a folder.
    ///   2. Group into per-clock time series.
    ///   3. Compute ensemble mean per MJD.
    ///   4. Fit linear drift slope.
    ///   5. Compute pairwise stability between long-running clocks.
    ///   6. Decide whether a global B(t) signal is present.
    /// </summary>
    public static class BipmClockAnalyzer
    {
        // ── Fixed-column offsets for BIPM "uh" format ────────────────
        private const int MJD_START  = 0;
        private const int MJD_LEN    = 9;
        private const int LAB_START  = 12;
        private const int LAB_LEN    = 5;
        private const int CLOCK_START = 18;
        private const int CLOCK_LEN  = 7;
        private const int OFFSET_START = 26;
        private const int OFFSET_LEN = 11;
        private const int LAB_NAME_START = 49;
        private const int LAB_NAME_LEN = 4;

        // ── Step 2: File loader ─────────────────────────────────────

        /// <summary>
        /// Iterates all files starting with "uh" in <paramref name="folder"/>
        /// and parses each line into a <see cref="ClockMeasurement"/>.
        /// Lines whose MJD, lab, clock, or offset fields are empty are skipped.
        /// </summary>
        public static List<ClockMeasurement> LoadUhFiles(string folder)
        {
            var measurements = new List<ClockMeasurement>();

            if (!Directory.Exists(folder))
                throw new DirectoryNotFoundException($"BIPM clocks folder not found: {folder}");

            foreach (var filePath in Directory.GetFiles(folder, "uh*"))
            {
                foreach (var line in File.ReadLines(filePath))
                {
                    if (line.Length < OFFSET_START + OFFSET_LEN)
                        continue;

                    string mjdStr    = line.Substring(MJD_START, MJD_LEN).Trim();
                    string labStr    = line.Substring(LAB_START, LAB_LEN).Trim();
                    string clockStr  = line.Substring(CLOCK_START, CLOCK_LEN).Trim();
                    string offsetStr = line.Substring(OFFSET_START, OFFSET_LEN).Trim();
                    string labName   = line.Length >= LAB_NAME_START + LAB_NAME_LEN
                        ? line.Substring(LAB_NAME_START, LAB_NAME_LEN).Trim()
                        : "";

                    if (string.IsNullOrEmpty(mjdStr) || string.IsNullOrEmpty(labStr)
                        || string.IsNullOrEmpty(clockStr) || string.IsNullOrEmpty(offsetStr))
                        continue;

                    if (!double.TryParse(mjdStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double mjd))
                        continue;
                    if (!int.TryParse(labStr, out int labCode))
                        continue;
                    if (!int.TryParse(clockStr, out int clockId))
                        continue;
                    if (!double.TryParse(offsetStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double offset))
                        continue;

                    measurements.Add(new ClockMeasurement(mjd, labCode, clockId, offset, labName));
                }
            }

            return measurements;
        }

        // ── Step 3: Time-series builder ─────────────────────────────

        /// <summary>
        /// Groups measurements by ClockId, orders by MJD, and filters
        /// to series with at least <paramref name="minSamples"/> points.
        /// </summary>
        public static List<ClockTimeSeries> BuildTimeSeries(
            List<ClockMeasurement> measurements,
            int minSamples = 10)
        {
            return measurements
                .GroupBy(m => m.ClockId)
                .Select(g => new ClockTimeSeries(
                    g.Key,
                    g.OrderBy(x => x.Mjd)
                     .Select(x => (x.Mjd, x.OffsetNs))
                     .ToList()))
                .Where(s => s.Samples.Count >= minSamples)
                .ToList();
        }

        // ── Step 4: Ensemble mean ───────────────────────────────────

        /// <summary>
        /// Computes, for each distinct MJD, the mean and population
        /// standard deviation of all clock offsets recorded at that MJD.
        /// </summary>
        public static List<(double Mjd, double Mean, double Std)> ComputeEnsemble(
            List<ClockMeasurement> measurements)
        {
            return measurements
                .GroupBy(m => m.Mjd)
                .Select(g =>
                {
                    double mean = g.Average(x => x.OffsetNs);
                    double std = Math.Sqrt(g.Average(x =>
                        (x.OffsetNs - mean) * (x.OffsetNs - mean)));
                    return (Mjd: g.Key, Mean: mean, Std: std);
                })
                .OrderBy(x => x.Mjd)
                .ToList();
        }

        // ── Step 5: Linear drift fit ────────────────────────────────

        /// <summary>
        /// Ordinary least-squares linear regression.
        /// Returns (slope, intercept) such that y ≈ slope · x + intercept.
        /// </summary>
        public static (double Slope, double Intercept) FitLine(
            List<(double X, double Y)> data)
        {
            int n = data.Count;
            if (n < 2)
                return (0.0, data.Count == 1 ? data[0].Y : 0.0);

            double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
            foreach (var (x, y) in data)
            {
                sumX  += x;
                sumY  += y;
                sumXY += x * y;
                sumX2 += x * x;
            }

            double denom = n * sumX2 - sumX * sumX;
            if (Math.Abs(denom) < 1e-30)
                return (0.0, sumY / n);

            double slope     = (n * sumXY - sumX * sumY) / denom;
            double intercept = (sumY - slope * sumX) / n;
            return (slope, intercept);
        }

        // ── Step 6: Pairwise stability ──────────────────────────────

        /// <summary>
        /// Selects the <paramref name="topN"/> longest time series,
        /// then for every clock pair (i &lt; j) computes the variance of
        /// Δ_ij(t) = offset_i(t) − offset_j(t) over the intersection
        /// of their MJD grids.
        /// </summary>
        public static List<(int ClockI, int ClockJ, int PairSize, double Variance)>
            ComputePairwiseStability(
                List<ClockTimeSeries> series,
                int topN = 20)
        {
            var top = series
                .OrderByDescending(s => s.Samples.Count)
                .Take(topN)
                .ToList();

            var result = new List<(int, int, int, double)>();

            for (int a = 0; a < top.Count; a++)
            {
                for (int b = a + 1; b < top.Count; b++)
                {
                    var dictA = top[a].Samples.ToDictionary(
                        x => x.Mjd, x => x.OffsetNs);
                    var dictB = top[b].Samples.ToDictionary(
                        x => x.Mjd, x => x.OffsetNs);

                    var common = dictA.Keys
                        .Where(k => dictB.ContainsKey(k))
                        .Select(k => dictA[k] - dictB[k])
                        .ToList();

                    if (common.Count < 5)
                        continue;

                    double mean = common.Average();
                    double variance = common.Average(x =>
                        (x - mean) * (x - mean));

                    result.Add((top[a].ClockId, top[b].ClockId,
                        common.Count, variance));
                }
            }

            return result;
        }

        // ── Step 7: Detection logic ─────────────────────────────────

        /// <summary>
        /// A global B(t) candidate is flagged when:
        ///   • The ensemble drift slope has |slope| > driftThreshold,
        ///   • The average pairwise variance is below varianceCeiling
        ///     (clocks move together despite the drift).
        /// </summary>
        public static bool DetectGlobalDrift(
            List<(double Mjd, double Mean, double Std)> ensemble,
            List<(int ClockI, int ClockJ, int PairSize, double Variance)> pairwise,
            double driftThreshold = 1e-4,
            double varianceCeiling = 1e6)
        {
            if (ensemble.Count < 3 || pairwise.Count == 0)
                return false;

            var fitData = ensemble
                .Select(e => (e.Mjd, e.Mean))
                .ToList();
            var (slope, _) = FitLine(fitData);

            double avgPairVar = pairwise.Average(p => p.Variance);

            return Math.Abs(slope) > driftThreshold
                && avgPairVar < varianceCeiling;
        }

        // ── Step 7 (full pipeline) ──────────────────────────────────

        /// <summary>
        /// Runs the complete pipeline and returns a
        /// <see cref="BipmAnalysisResult"/>.
        /// </summary>
        public static BipmAnalysisResult Analyze(
            List<ClockMeasurement> measurements,
            int minSeriesSamples = 10,
            int topNPairwise = 20)
        {
            var ensemble = ComputeEnsemble(measurements);
            var series = BuildTimeSeries(measurements, minSeriesSamples);
            var pairwise = ComputePairwiseStability(series, topNPairwise);

            var fitData = ensemble
                .Select(e => (e.Mjd, e.Mean))
                .ToList();
            var drift = FitLine(fitData);

            bool detected = DetectGlobalDrift(ensemble, pairwise);

            return new BipmAnalysisResult(
                ensemble, drift, pairwise, detected, drift.Slope);
        }

        // ── Step 8: CSV export ──────────────────────────────────────

        public static void WriteCsv(
            string outputFolder,
            BipmAnalysisResult result)
        {
            Directory.CreateDirectory(outputFolder);

            // ensemble.csv
            string ensemblePath = Path.Combine(outputFolder, "ensemble.csv");
            using (var w = new StreamWriter(ensemblePath))
            {
                w.WriteLine("Mjd,Mean_ns,Std_ns");
                foreach (var (mjd, mean, std) in result.Ensemble)
                    w.WriteLine(
                        FormattableString.Invariant(
                            $"{mjd:F3},{mean:F3},{std:F3}"));
            }

            // drift.csv
            string driftPath = Path.Combine(outputFolder, "drift.csv");
            using (var w = new StreamWriter(driftPath))
            {
                w.WriteLine("Slope_ns_per_day,Intercept_ns");
                w.WriteLine(
                    FormattableString.Invariant(
                        $"{result.Drift.Slope:F6},{result.Drift.Intercept:F3}"));
            }

            // pairwise.csv
            string pairwisePath = Path.Combine(outputFolder, "pairwise.csv");
            using (var w = new StreamWriter(pairwisePath))
            {
                w.WriteLine("ClockI,ClockJ,PairSize,Variance_ns2");
                foreach (var (ci, cj, size, variance) in result.Pairwise)
                    w.WriteLine($"{ci},{cj},{size}," +
                        FormattableString.Invariant($"{variance:F3}"));
            }
        }

        // ── Step 8b: ScottPlot visualisation ────────────────────────

        /// <summary>
        /// Saves an ensemble-mean-vs-MJD plot with the linear drift fit
        /// overlaid.
        /// </summary>
        public static void SaveEnsemblePlot(
            string path,
            BipmAnalysisResult result,
            int width = 1200,
            int height = 700)
        {
            var plt = new ScottPlot.Plot();

            double[] mjds = result.Ensemble.Select(e => e.Mjd).ToArray();
            double[] means = result.Ensemble.Select(e => e.Mean).ToArray();

            // Drift fit line
            double[] fitY = mjds.Select(m =>
                result.Drift.Slope * m + result.Drift.Intercept).ToArray();

            // Ensemble scatter
            var scatter = plt.Add.Scatter(mjds, means);
            scatter.MarkerSize = 5;
            scatter.Color = ScottPlot.Color.FromHex("#1f77b4");
            scatter.LegendText = "Ensemble mean";

            // Drift fit line
            var line = plt.Add.Scatter(mjds, fitY);
            line.LineWidth = 2;
            line.Color = ScottPlot.Colors.Red;
            line.LegendText = FormattableString.Invariant(
                $"Drift: {result.Drift.Slope:E3} ns/day");

            plt.Title("BIPM UTCr — Ensemble Mean & Global Drift");
            plt.XLabel("MJD");
            plt.YLabel("Offset (ns)");
            plt.ShowLegend();

            plt.SavePng(path, width, height);
        }

        /// <summary>
        /// Saves a histogram of pairwise clock variances.
        /// Low variance across pairs supports the global-drift hypothesis.
        /// </summary>
        public static void SavePairwiseHistogram(
            string path,
            BipmAnalysisResult result,
            int width = 1000,
            int height = 600)
        {
            var plt = new ScottPlot.Plot();

            double[] variances = result.Pairwise
                .Select(p => p.Variance)
                .ToArray();

            if (variances.Length == 0)
            {
                plt.Title("Pairwise Variance — No data");
                plt.SavePng(path, width, height);
                return;
            }

            // Compute histogram bins manually
            int binCount = 30;
            double minV = variances.Min();
            double maxV = variances.Max();
            double binWidth = (maxV - minV) / binCount;
            if (binWidth < 1e-12) binWidth = 1.0;

            double[] counts = new double[binCount];
            double[] binCenters = new double[binCount];
            for (int i = 0; i < binCount; i++)
            {
                binCenters[i] = minV + (i + 0.5) * binWidth;
            }

            foreach (double v in variances)
            {
                int idx = (int)((v - minV) / binWidth);
                if (idx >= binCount) idx = binCount - 1;
                if (idx < 0) idx = 0;
                counts[idx]++;
            }

            double barWidth = binWidth * 0.8;

            var bars = plt.Add.Bars(binCenters, counts);
            foreach (var bar in bars.Bars)
            {
                bar.Size = barWidth;
                bar.FillColor = ScottPlot.Color.FromHex("#1f77b4");
            }

            bars.LegendText = $"{variances.Length} pairs";

            plt.Title("Pairwise Clock Variance Distribution");
            plt.XLabel("Variance (ns²)");
            plt.YLabel("Count");
            plt.ShowLegend();

            plt.SavePng(path, width, height);
        }

        // ── Step 9: Helpers ─────────────────────────────────────────

        /// <summary>
        /// Returns a formatted summary string for console output.
        /// </summary>
        public static string FormatSummary(BipmAnalysisResult result)
        {
            var inv = CultureInfo.InvariantCulture;
            double avgPairVar = result.Pairwise.Count > 0
                ? result.Pairwise.Average(p => p.Variance)
                : 0.0;

            return string.Format(inv,
                "── BIPM Clock Analysis ──\n" +
                "Ensemble points   : {0}\n" +
                "Drift slope       : {1:E3} ns/day\n" +
                "Drift intercept   : {2:F1} ns\n" +
                "Pairwise pairs    : {3}\n" +
                "Avg pairwise var  : {4:F1} ns²\n" +
                "Global B(t) detect: {5}",
                result.Ensemble.Count,
                result.Drift.Slope,
                result.Drift.Intercept,
                result.Pairwise.Count,
                avgPairVar,
                result.DetectedGlobalDrift ? "YES" : "NO");
        }
    }
}
