using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace TRM.CMD
{
    /// <summary>
    /// A single [UTCr-UTC(k)] measurement for one lab at one MJD.
    /// </summary>
    public record UtcrMeasurement(
        double Mjd,
        string Lab,
        double ValueNs);

    /// <summary>
    /// Time-ordered series for a single laboratory.
    /// </summary>
    public record UtcrLabSeries(
        string Lab,
        List<(double Mjd, double ValueNs)> Samples);

    /// <summary>
    /// Ensemble statistics at a single MJD.
    /// </summary>
    public record UtcrEnsemblePoint(
        double Mjd,
        double Mean,
        double Median,
        double Std,
        int Count);

    /// <summary>
    /// Result of the BIPM UTCr analysis pipeline.
    /// </summary>
    public record UtcrAnalysisResult(
        List<UtcrMeasurement> AllMeasurements,
        List<UtcrLabSeries> LabSeries,
        List<UtcrEnsemblePoint> Ensemble,
        (double Slope, double Intercept) GlobalDrift,
        double AverageLabSlope,
        double AveragePairwiseCorrelation,
        List<(string Lab, double Slope)> LabSlopes,
        double[,] CorrelationMatrix,
        List<string> LabOrder,
        bool CandidateGlobalDrift)
    {
        // ── v2 fields (stable-lab, demeaned, robust) ────────────────
        public List<UtcrLabSeries> StableLabSeries { get; init; } = null!;
        public int StableLabCount => StableLabSeries?.Count ?? 0;
        public List<UtcrEnsemblePoint> DemeanedEnsemble { get; init; } = null!;
        public List<UtcrEnsemblePoint> MedianEnsemble { get; init; } = null!;
        public (double Slope, double Intercept) DemeanedDrift { get; init; }
        public (double Slope, double Intercept) MedianDrift { get; init; }
        public double CommonModeCorrelation { get; init; }
        public double SameSignDailyFraction { get; init; }
        public bool CandidateGlobalDriftV2 { get; init; }
    }

    /// <summary>
    /// Analyzes BIPM UTCr weekly files to detect a global common-mode
    /// drift (candidate B(t) from BB13–BB16).
    ///
    /// Pipeline:
    ///   1. Parse all UTCr_YYWW files.
    ///   2. Build per-lab time series.
    ///   3. Compute ensemble statistics per MJD.
    ///   4. Fit global linear drift to ensemble mean.
    ///   5. Compute common-mode strength (average pairwise correlation).
    ///   6. Detect whether a global B(t) candidate is present.
    /// </summary>
    public static class BipmUtcrAnalyzer
    {
        // ── Step 2: File loader ─────────────────────────────────────

        /// <summary>
        /// Parses all UTCr_* files in <paramref name="folder"/> and
        /// returns a flat list of <see cref="UtcrMeasurement"/> entries.
        /// </summary>
        public static List<UtcrMeasurement> LoadUtcrFiles(string folder)
        {
            var measurements = new List<UtcrMeasurement>();

            if (!Directory.Exists(folder))
                throw new DirectoryNotFoundException($"UTCr folder not found: {folder}");

            foreach (var filePath in Directory.GetFiles(folder, "UTCr_*"))
            {
                measurements.AddRange(ParseUtcrFile(filePath));
            }

            return measurements;
        }

        /// <summary>
        /// Parses a single UTCr_YYWW file.
        /// </summary>
        private static List<UtcrMeasurement> ParseUtcrFile(string filePath)
        {
            var result = new List<UtcrMeasurement>();
            var lines = File.ReadAllLines(filePath);

            List<double> mjds = null!;
            bool inDataSection = false;

            foreach (var line in lines)
            {
                string trimmed = line.TrimEnd();

                // Detect MJD line: contains "MJD" and numeric values.
                if (!inDataSection && trimmed.Contains("MJD"))
                {
                    mjds = ParseMjdLine(trimmed);
                    inDataSection = mjds != null && mjds.Count > 0;
                    continue;
                }

                // Start of data section: line begins with "Laboratory k"
                if (!inDataSection && trimmed.StartsWith("Laboratory k",
                    StringComparison.OrdinalIgnoreCase))
                {
                    inDataSection = true;
                    continue;
                }

                // Stop at footer lines after data.
                if (mjds == null || !inDataSection)
                    continue;

                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    // Empty line after data → stop parsing this file.
                    if (result.Count > 0)
                        break;
                    continue;
                }

                // Skip non-data header/footer lines.
                if (trimmed.StartsWith("UTC remains", StringComparison.OrdinalIgnoreCase))
                    break;
                if (trimmed.StartsWith("Date", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (trimmed.StartsWith("BUREAU", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (trimmed.StartsWith("THE INTER", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (trimmed.StartsWith("PAVILLON", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (trimmed.StartsWith("Computed values", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (trimmed.StartsWith("0h UTC", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (trimmed.StartsWith("202", StringComparison.OrdinalIgnoreCase) && trimmed.Length < 10)
                    continue;

                // Parse a data row: first token is lab acronym, rest are values.
                var measurementRow = ParseDataRow(trimmed, mjds);
                if (measurementRow != null)
                    result.AddRange(measurementRow);
            }

            return result;
        }

        /// <summary>
        /// Extracts MJD values from a line like
        /// "       MJD                   61038    61039    61040  …"
        /// </summary>
        private static List<double> ParseMjdLine(string line)
        {
            var mjds = new List<double>();
            int idx = line.IndexOf("MJD", StringComparison.Ordinal);
            if (idx < 0) return mjds;

            string afterMjd = line.Substring(idx + 3).Trim();
            var tokens = afterMjd.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var token in tokens)
            {
                if (double.TryParse(token, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out double mjd))
                {
                    mjds.Add(mjd);
                }
            }

            return mjds;
        }

        /// <summary>
        /// Parses a lab data row. First token = "LAB (city)", remainder
        /// are numeric values or "-" for missing.
        /// </summary>
        private static List<UtcrMeasurement>? ParseDataRow(
            string line, List<double> mjds)
        {
            // Lab acronym is the first whitespace-delimited token.
            int firstSpace = line.IndexOf(' ');
            if (firstSpace < 0) return null;

            string labToken = line.Substring(0, firstSpace).Trim();

            // Heuristic: lab acronyms are short uppercase strings.
            if (labToken.Length < 2 || labToken.Length > 6)
                return null;
            if (!labToken.All(c => char.IsUpper(c) || char.IsDigit(c)))
                return null;

            string lab = labToken;

            // Remaining values — space-separated tokens.
            string remainder = line.Substring(firstSpace).Trim();

            // Remove the city in parentheses if present.
            int parenOpen = remainder.IndexOf('(');
            int parenClose = remainder.IndexOf(')');
            if (parenOpen >= 0 && parenClose > parenOpen)
            {
                remainder = remainder.Substring(parenClose + 1).Trim();
            }

            var tokens = remainder.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var result = new List<UtcrMeasurement>();

            for (int i = 0; i < Math.Min(tokens.Length, mjds.Count); i++)
            {
                if (tokens[i] == "-")
                    continue;

                if (double.TryParse(tokens[i], NumberStyles.Float,
                    CultureInfo.InvariantCulture, out double value))
                {
                    result.Add(new UtcrMeasurement(mjds[i], lab, value));
                }
            }

            return result;
        }

        // ── Step 3: Time-series builder ─────────────────────────────

        /// <summary>
        /// Groups measurements by lab, sorts by MJD, and keeps labs
        /// with at least <paramref name="minObservations"/> entries.
        /// </summary>
        public static List<UtcrLabSeries> BuildLabSeries(
            List<UtcrMeasurement> measurements,
            int minObservations = 20)
        {
            return measurements
                .GroupBy(m => m.Lab)
                .Select(g => new UtcrLabSeries(
                    g.Key,
                    g.OrderBy(x => x.Mjd)
                     .Select(x => (x.Mjd, x.ValueNs))
                     .ToList()))
                .Where(s => s.Samples.Count >= minObservations)
                .OrderBy(s => s.Lab)
                .ToList();
        }

        // ── Step 4: Ensemble statistics per MJD ─────────────────────

        /// <summary>
        /// Computes mean, median, std dev, and lab count for each MJD.
        /// </summary>
        public static List<UtcrEnsemblePoint> ComputeEnsemble(
            List<UtcrMeasurement> measurements)
        {
            return measurements
                .GroupBy(m => m.Mjd)
                .Select(g =>
                {
                    var values = g.Select(x => x.ValueNs).OrderBy(v => v).ToList();
                    double mean = values.Average();
                    double median = values[values.Count / 2];
                    double std = Math.Sqrt(values.Average(v =>
                        (v - mean) * (v - mean)));
                    return new UtcrEnsemblePoint(g.Key, mean, median, std, values.Count);
                })
                .OrderBy(e => e.Mjd)
                .ToList();
        }

        // ── Step 5: Linear drift fit ────────────────────────────────

        /// <summary>
        /// Ordinary least-squares linear regression.
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

        // ── Step 6: Common-mode strength ────────────────────────────

        /// <summary>
        /// Fits a linear drift to each lab's time series and returns
        /// lab name + slope in ns/day.
        /// </summary>
        public static List<(string Lab, double Slope)> ComputeLabSlopes(
            List<UtcrLabSeries> series)
        {
            return series.Select(s =>
            {
                var data = s.Samples
                    .Select(p => (p.Mjd, p.ValueNs))
                    .ToList();
                var (slope, _) = FitLine(data);
                return (s.Lab, slope);
            }).ToList();
        }

        /// <summary>
        /// Computes the average pairwise Pearson correlation between
        /// all lab time series, using only MJDs where both labs have data.
        /// Returns the average and populates the correlation matrix and
        /// lab order for export.
        /// </summary>
        public static double ComputeAveragePairwiseCorrelation(
            List<UtcrLabSeries> series,
            out double[,] matrix,
            out List<string> labOrder)
        {
            labOrder = series.Select(s => s.Lab).ToList();
            int n = labOrder.Count;
            matrix = new double[n, n];

            // Build MJD → value lookup per lab.
            var lookup = series.Select(s =>
                s.Samples.ToDictionary(p => p.Mjd, p => p.ValueNs)).ToList();

            double sumR = 0;
            int pairCount = 0;

            for (int i = 0; i < n; i++)
            {
                matrix[i, i] = 1.0;
                for (int j = i + 1; j < n; j++)
                {
                    double r = PearsonCorrelation(lookup[i], lookup[j]);
                    matrix[i, j] = r;
                    matrix[j, i] = r;
                    sumR += r;
                    pairCount++;
                }
            }

            return pairCount > 0 ? sumR / pairCount : 0.0;
        }

        /// <summary>
        /// Pearson correlation between two sparse MJD→value dictionaries
        /// using their intersection.
        /// </summary>
        private static double PearsonCorrelation(
            Dictionary<double, double> a,
            Dictionary<double, double> b)
        {
            var common = a.Keys
                .Where(k => b.ContainsKey(k))
                .Select(k => (a[k], b[k]))
                .ToList();

            if (common.Count < 5)
                return 0.0;

            double meanX = common.Average(p => p.Item1);
            double meanY = common.Average(p => p.Item2);

            double cov = 0, varX = 0, varY = 0;
            foreach (var (x, y) in common)
            {
                double dx = x - meanX;
                double dy = y - meanY;
                cov  += dx * dy;
                varX += dx * dx;
                varY += dy * dy;
            }

            double denom = Math.Sqrt(varX * varY);
            if (denom < 1e-30)
                return 0.0;

            return cov / denom;
        }

        // ── Step 7: Detection logic ─────────────────────────────────

        /// <summary>
        /// Flags a global B(t) candidate when:
        ///   • Ensemble drift slope is significantly non-zero (> 1e-4 ns/day).
        ///   • Average pairwise correlation > 0.3 (labs move together).
        ///   • Majority of lab slopes have the same sign as the global slope.
        /// </summary>
        public static bool DetectGlobalDrift(
            (double Slope, double Intercept) globalDrift,
            List<(string Lab, double Slope)> labSlopes,
            double avgCorrelation,
            double driftThreshold = 1e-4,
            double correlationThreshold = 0.3,
            double sameSignFraction = 0.6)
        {
            if (Math.Abs(globalDrift.Slope) <= driftThreshold)
                return false;

            if (avgCorrelation < correlationThreshold)
                return false;

            int sameSign = labSlopes.Count(ls =>
                Math.Sign(ls.Slope) == Math.Sign(globalDrift.Slope));

            double fraction = (double)sameSign / Math.Max(1, labSlopes.Count);

            return fraction >= sameSignFraction;
        }

        // ── Step 1b: Stable-lab intersection ────────────────────────

        /// <summary>
        /// Returns labs whose MJD coverage is at least
        /// <paramref name="coverageFraction"/> of the total MJD span.
        /// </summary>
        public static List<UtcrLabSeries> FilterStableLabs(
            List<UtcrLabSeries> series,
            double coverageFraction = 0.80)
        {
            if (series.Count == 0) return new List<UtcrLabSeries>();

            var allMjds = series
                .SelectMany(s => s.Samples)
                .Select(p => p.Mjd)
                .Distinct()
                .OrderBy(m => m)
                .ToList();

            int totalMjds = allMjds.Count;
            if (totalMjds == 0) return new List<UtcrLabSeries>();

            return series
                .Where(s => s.Samples.Select(p => p.Mjd).Distinct().Count()
                            >= coverageFraction * totalMjds)
                .ToList();
        }

        // ── Step 2b: Demeaned lab series ────────────────────────────

        /// <summary>
        /// Returns (lab, demeaned samples) for each input series,
        /// where demeaned(t) = value(t) − mean(value over the series).
        /// </summary>
        public static List<UtcrLabSeries> ComputeDemeanedSeries(
            List<UtcrLabSeries> series)
        {
            return series.Select(s =>
            {
                double mean = s.Samples.Average(p => p.ValueNs);
                var demeaned = s.Samples
                    .Select(p => (p.Mjd, p.ValueNs - mean))
                    .ToList();
                return new UtcrLabSeries(s.Lab, demeaned);
            }).ToList();
        }

        // ── Step 2c: Demeaned ensemble ──────────────────────────────

        /// <summary>
        /// For each MJD in the union of all stable-lab observations,
        /// computes the mean of the demeaned values.
        /// </summary>
        public static List<UtcrEnsemblePoint> ComputeDemeanedEnsemble(
            List<UtcrLabSeries> stableLabs)
        {
            return ComputeEnsembleFromSeries(stableLabs);
        }

        /// <summary>
        /// Computes median-based ensemble statistics per MJD for stable labs.
        /// </summary>
        public static List<UtcrEnsemblePoint> ComputeMedianEnsemble(
            List<UtcrLabSeries> stableLabs)
        {
            var allMjds = stableLabs
                .SelectMany(s => s.Samples)
                .Select(p => p.Mjd)
                .Distinct()
                .OrderBy(m => m)
                .ToList();

            var lookup = stableLabs.ToDictionary(
                s => s.Lab,
                s => s.Samples.ToDictionary(p => p.Mjd, p => p.ValueNs));

            return allMjds.Select(mjd =>
            {
                var values = stableLabs
                    .Where(s => lookup[s.Lab].ContainsKey(mjd))
                    .Select(s => lookup[s.Lab][mjd])
                    .OrderBy(v => v)
                    .ToList();

                if (values.Count == 0)
                    return new UtcrEnsemblePoint(mjd, 0, 0, 0, 0);

                double mean = values.Average();
                double median = values[values.Count / 2];
                double std = Math.Sqrt(values.Average(v =>
                    (v - mean) * (v - mean)));

                return new UtcrEnsemblePoint(mjd, mean, median, std, values.Count);
            }).ToList();
        }

        /// <summary>
        /// Internal: computes ensemble from UtcrLabSeries without going
        /// through the measurement flat list.
        /// </summary>
        private static List<UtcrEnsemblePoint> ComputeEnsembleFromSeries(
            List<UtcrLabSeries> series)
        {
            var allMjds = series
                .SelectMany(s => s.Samples)
                .Select(p => p.Mjd)
                .Distinct()
                .OrderBy(m => m)
                .ToList();

            var lookup = series.ToDictionary(
                s => s.Lab,
                s => s.Samples.ToDictionary(p => p.Mjd, p => p.ValueNs));

            return allMjds.Select(mjd =>
            {
                var values = series
                    .Where(s => lookup[s.Lab].ContainsKey(mjd))
                    .Select(s => lookup[s.Lab][mjd])
                    .OrderBy(v => v)
                    .ToList();

                if (values.Count == 0)
                    return new UtcrEnsemblePoint(mjd, 0, 0, 0, 0);

                double mean = values.Average();
                double median = values[values.Count / 2];
                double std = Math.Sqrt(values.Average(v =>
                    (v - mean) * (v - mean)));

                return new UtcrEnsemblePoint(mjd, mean, median, std, values.Count);
            }).ToList();
        }

        // ── Step 6b: Common-mode strength v2 ────────────────────────

        /// <summary>
        /// Computes the average Pearson correlation between each stable
        /// lab's demeaned series and the demeaned ensemble mean.
        /// This is a first principal-component proxy.
        /// </summary>
        public static double ComputeCommonModeCorrelation(
            List<UtcrLabSeries> stableLabs,
            List<UtcrEnsemblePoint> demeanedEnsemble)
        {
            var ensembleLookup = demeanedEnsemble
                .ToDictionary(e => e.Mjd, e => e.Mean);

            double sumR = 0;
            int count = 0;

            foreach (var lab in stableLabs)
            {
                var common = lab.Samples
                    .Where(p => ensembleLookup.ContainsKey(p.Mjd))
                    .Select(p => (p.ValueNs, ensembleLookup[p.Mjd]))
                    .ToList();

                if (common.Count < 5) continue;

                double meanX = common.Average(p => p.Item1);
                double meanY = common.Average(p => p.Item2);
                double cov = 0, varX = 0, varY = 0;

                foreach (var (x, y) in common)
                {
                    cov  += (x - meanX) * (y - meanY);
                    varX += (x - meanX) * (x - meanX);
                    varY += (y - meanY) * (y - meanY);
                }

                double denom = Math.Sqrt(varX * varY);
                if (denom > 1e-30)
                {
                    sumR += cov / denom;
                    count++;
                }
            }

            return count > 0 ? sumR / count : 0.0;
        }

        /// <summary>
        /// For each MJD transition, computes the fraction of labs
        /// whose value moved in the same direction. Averages across
        /// all consecutive MJD pairs.
        /// </summary>
        public static double ComputeSameSignDailyFraction(
            List<UtcrLabSeries> stableLabs)
        {
            var allMjds = stableLabs
                .SelectMany(s => s.Samples)
                .Select(p => p.Mjd)
                .Distinct()
                .OrderBy(m => m)
                .ToList();

            if (allMjds.Count < 2) return 0.0;

            var lookup = stableLabs.ToDictionary(
                s => s.Lab,
                s => s.Samples.ToDictionary(p => p.Mjd, p => p.ValueNs));

            double totalFrac = 0;
            int transitions = 0;

            for (int t = 1; t < allMjds.Count; t++)
            {
                double mjdPrev = allMjds[t - 1];
                double mjdCurr = allMjds[t];

                // Compute median increment sign across labs, then count
                // the fraction of labs matching that direction.
                var increments = new List<(string Lab, double Inc)>();
                foreach (var lab in stableLabs)
                {
                    if (lookup.TryGetValue(lab.Lab, out var dict)
                        && dict.TryGetValue(mjdPrev, out double vPrev)
                        && dict.TryGetValue(mjdCurr, out double vCurr))
                    {
                        increments.Add((lab.Lab, vCurr - vPrev));
                    }
                }

                if (increments.Count < 5) continue;

                double medianInc = increments
                    .OrderBy(x => x.Inc)
                    .ElementAt(increments.Count / 2).Inc;

                int signMedian = Math.Sign(medianInc);
                if (signMedian == 0) continue;

                int matching = increments.Count(x =>
                    Math.Sign(x.Inc) == signMedian);

                totalFrac += (double)matching / increments.Count;
                transitions++;
            }

            return transitions > 0 ? totalFrac / transitions : 0.0;
        }

        // ── Step 7c: Detection logic v2 ─────────────────────────────

        /// <summary>
        /// Stricter detection: ALL criteria must hold.
        ///   1. Demeaned ensemble drift significantly non-zero.
        ///   2. Median drift agrees in sign and magnitude (within factor 3).
        ///   3. Average pairwise correlation > 0.3.
        ///   4. Same-sign daily fraction > 0.5.
        ///   5. Common-mode correlation > 0.3.
        /// </summary>
        public static bool DetectGlobalDriftV2(
            (double Slope, double Intercept) demeanedDrift,
            (double Slope, double Intercept) medianDrift,
            double avgPairwiseCorrelation,
            double sameSignDailyFraction,
            double commonModeCorrelation,
            double driftThreshold = 1e-4,
            double correlationThreshold = 0.3,
            double sameSignThreshold = 0.5)
        {
            if (Math.Abs(demeanedDrift.Slope) <= driftThreshold)
                return false;

            if (Math.Abs(medianDrift.Slope) <= driftThreshold)
                return false;

            if (Math.Sign(medianDrift.Slope) != Math.Sign(demeanedDrift.Slope))
                return false;

            double ratio = Math.Abs(medianDrift.Slope) /
                Math.Max(1e-30, Math.Abs(demeanedDrift.Slope));
            if (ratio > 3.0 || ratio < 1.0 / 3.0)
                return false;

            if (avgPairwiseCorrelation < correlationThreshold)
                return false;

            if (sameSignDailyFraction < sameSignThreshold)
                return false;

            if (commonModeCorrelation < correlationThreshold)
                return false;

            return true;
        }

        // ── Step 7b: Full pipeline ──────────────────────────────────

        /// <summary>
        /// Runs the complete UTCr analysis pipeline (v2 with stable labs).
        /// </summary>
        public static UtcrAnalysisResult Analyze(
            string folder,
            int minObservations = 20,
            double stableCoverage = 0.80)
        {
            var measurements = LoadUtcrFiles(folder);
            var labSeries = BuildLabSeries(measurements, minObservations);
            var ensemble = ComputeEnsemble(measurements);
            var labSlopes = ComputeLabSlopes(labSeries);

            var fitData = ensemble
                .Select(e => (e.Mjd, e.Mean))
                .ToList();
            var globalDrift = FitLine(fitData);

            double avgLabSlope = labSlopes.Count > 0
                ? labSlopes.Average(ls => ls.Slope)
                : 0.0;

            double avgCorr = ComputeAveragePairwiseCorrelation(
                labSeries, out var corrMatrix, out var labOrder);

            bool candidate = DetectGlobalDrift(globalDrift, labSlopes, avgCorr);

            // ── v2: stable-lab, demeaned, robust ────────────────────
            var stableLabs = FilterStableLabs(labSeries, stableCoverage);
            var demeanedStable = ComputeDemeanedSeries(stableLabs);
            var demeanedEnsemble = ComputeDemeanedEnsemble(demeanedStable);
            var medianEnsemble = ComputeMedianEnsemble(stableLabs);

            var demeanedFit = FitLine(demeanedEnsemble
                .Select(e => (e.Mjd, e.Mean)).ToList());
            var medianFit = FitLine(medianEnsemble
                .Select(e => (e.Mjd, e.Median)).ToList());

            double commonModeCorr = ComputeCommonModeCorrelation(
                demeanedStable, demeanedEnsemble);
            double sameSignFrac = ComputeSameSignDailyFraction(stableLabs);

            bool candidateV2 = DetectGlobalDriftV2(
                demeanedFit, medianFit, avgCorr, sameSignFrac, commonModeCorr);

            return new UtcrAnalysisResult(
                measurements, labSeries, ensemble, globalDrift,
                avgLabSlope, avgCorr, labSlopes, corrMatrix, labOrder,
                candidate)
            {
                StableLabSeries = stableLabs,
                DemeanedEnsemble = demeanedEnsemble,
                MedianEnsemble = medianEnsemble,
                DemeanedDrift = demeanedFit,
                MedianDrift = medianFit,
                CommonModeCorrelation = commonModeCorr,
                SameSignDailyFraction = sameSignFrac,
                CandidateGlobalDriftV2 = candidateV2
            };
        }

        // ── Step 8: CSV export ──────────────────────────────────────

        /// <summary>
        /// Writes all CSV and PNG output files.
        /// </summary>
        public static void WriteOutputs(
            string outputFolder,
            UtcrAnalysisResult result)
        {
            Directory.CreateDirectory(outputFolder);

            WriteAllCsv(Path.Combine(outputFolder, "utcr_all.csv"), result);
            WriteEnsembleCsv(Path.Combine(outputFolder, "utcr_ensemble.csv"), result);
            WriteLabSlopesCsv(Path.Combine(outputFolder, "utcr_lab_slopes.csv"), result);
            WriteCorrelationsCsv(Path.Combine(outputFolder, "utcr_correlations.csv"), result);

            // v2 CSVs
            WriteStableLabsCsv(Path.Combine(outputFolder, "utcr_stable_labs.csv"), result);
            WriteDemeanedEnsembleCsv(Path.Combine(outputFolder, "utcr_demeaned_ensemble.csv"), result);
            WriteMedianEnsembleCsv(Path.Combine(outputFolder, "utcr_median_ensemble.csv"), result);
            WriteCommonModeReportCsv(Path.Combine(outputFolder, "utcr_common_mode_report.csv"), result);

            // v1 plots
            SaveEnsemblePlot(Path.Combine(outputFolder, "utcr_ensemble.png"), result);
            SaveLabSlopesHistogram(Path.Combine(outputFolder, "utcr_lab_slopes.png"), result);

            // v2 plots
            SaveDemeanedEnsemblePlot(Path.Combine(outputFolder, "demeaned_ensemble_drift.png"), result);
            SaveMedianVsMeanPlot(Path.Combine(outputFolder, "median_vs_mean_drift.png"), result);
            SaveStableLabOverlay(Path.Combine(outputFolder, "stable_lab_overlay.png"), result);
        }

        private static void WriteAllCsv(string path, UtcrAnalysisResult result)
        {
            var labs = result.LabSeries.Select(s => s.Lab).ToList();
            using var w = new StreamWriter(path);
            w.Write("Mjd");
            foreach (var lab in labs) w.Write($",{lab}");
            w.WriteLine();

            var allMjds = result.AllMeasurements
                .Select(m => m.Mjd).Distinct().OrderBy(m => m).ToList();

            var lookup = result.LabSeries.ToDictionary(
                s => s.Lab,
                s => s.Samples.ToDictionary(p => p.Mjd, p => p.ValueNs));

            foreach (var mjd in allMjds)
            {
                w.Write(FormattableString.Invariant($"{mjd:F3}"));
                foreach (var lab in labs)
                {
                    if (lookup.TryGetValue(lab, out var dict) && dict.TryGetValue(mjd, out double v))
                        w.Write(FormattableString.Invariant($",{v:F3}"));
                    else
                        w.Write(",");
                }
                w.WriteLine();
            }
        }

        private static void WriteEnsembleCsv(string path, UtcrAnalysisResult result)
        {
            using var w = new StreamWriter(path);
            w.WriteLine("Mjd,Mean_ns,Median_ns,Std_ns,LabCount");
            foreach (var e in result.Ensemble)
                w.WriteLine(FormattableString.Invariant(
                    $"{e.Mjd:F3},{e.Mean:F3},{e.Median:F3},{e.Std:F3},{e.Count}"));
        }

        private static void WriteLabSlopesCsv(string path, UtcrAnalysisResult result)
        {
            using var w = new StreamWriter(path);
            w.WriteLine("Lab,Slope_ns_per_day");
            foreach (var (lab, slope) in result.LabSlopes.OrderBy(ls => ls.Lab))
                w.WriteLine(FormattableString.Invariant($"{lab},{slope:F6}"));
        }

        private static void WriteCorrelationsCsv(string path, UtcrAnalysisResult result)
        {
            using var w = new StreamWriter(path);
            w.Write("Lab");
            foreach (var lab in result.LabOrder) w.Write($",{lab}");
            w.WriteLine();

            for (int i = 0; i < result.LabOrder.Count; i++)
            {
                w.Write(result.LabOrder[i]);
                for (int j = 0; j < result.LabOrder.Count; j++)
                    w.Write(FormattableString.Invariant(
                        $",{result.CorrelationMatrix[i, j]:F4}"));
                w.WriteLine();
            }
        }

        // ── Step 8b: v2 CSV exports ──────────────────────────────────

        private static void WriteStableLabsCsv(string path, UtcrAnalysisResult result)
        {
            using var w = new StreamWriter(path);
            w.WriteLine("Lab,ObservationCount");
            foreach (var lab in result.StableLabSeries.OrderBy(l => l.Lab))
                w.WriteLine($"{lab.Lab},{lab.Samples.Count}");
        }

        private static void WriteDemeanedEnsembleCsv(string path, UtcrAnalysisResult result)
        {
            using var w = new StreamWriter(path);
            w.WriteLine("Mjd,DemeanedMean_ns,Std_ns,LabCount");
            foreach (var e in result.DemeanedEnsemble)
                w.WriteLine(FormattableString.Invariant(
                    $"{e.Mjd:F3},{e.Mean:F3},{e.Std:F3},{e.Count}"));
        }

        private static void WriteMedianEnsembleCsv(string path, UtcrAnalysisResult result)
        {
            using var w = new StreamWriter(path);
            w.WriteLine("Mjd,Median_ns,Mean_ns,Std_ns,LabCount");
            foreach (var e in result.MedianEnsemble)
                w.WriteLine(FormattableString.Invariant(
                    $"{e.Mjd:F3},{e.Median:F3},{e.Mean:F3},{e.Std:F3},{e.Count}"));
        }

        private static void WriteCommonModeReportCsv(string path, UtcrAnalysisResult result)
        {
            using var w = new StreamWriter(path);
            w.WriteLine("Metric,Value");
            w.WriteLine(FormattableString.Invariant(
                $"StableLabCount,{result.StableLabCount}"));
            w.WriteLine(FormattableString.Invariant(
                $"DemeanedDriftSlope_ns_per_day,{result.DemeanedDrift.Slope:E6}"));
            w.WriteLine(FormattableString.Invariant(
                $"MedianDriftSlope_ns_per_day,{result.MedianDrift.Slope:E6}"));
            w.WriteLine(FormattableString.Invariant(
                $"CommonModeCorrelation,{result.CommonModeCorrelation:F4}"));
            w.WriteLine(FormattableString.Invariant(
                $"SameSignDailyFraction,{result.SameSignDailyFraction:F4}"));
            w.WriteLine(FormattableString.Invariant(
                $"CandidateV1,{result.CandidateGlobalDrift}"));
            w.WriteLine(FormattableString.Invariant(
                $"CandidateV2,{result.CandidateGlobalDriftV2}"));
        }

        // ── Step 9: ScottPlot visualisation ─────────────────────────

        public static void SaveEnsemblePlot(
            string path,
            UtcrAnalysisResult result,
            int width = 1200,
            int height = 700)
        {
            var plt = new ScottPlot.Plot();

            double[] mjds = result.Ensemble.Select(e => e.Mjd).ToArray();
            double[] means = result.Ensemble.Select(e => e.Mean).ToArray();
            double[] fitY = mjds.Select(m =>
                result.GlobalDrift.Slope * m + result.GlobalDrift.Intercept).ToArray();

            var scatter = plt.Add.Scatter(mjds, means);
            scatter.MarkerSize = 4;
            scatter.Color = ScottPlot.Color.FromHex("#1f77b4");
            scatter.LegendText = "Ensemble mean";

            var line = plt.Add.Scatter(mjds, fitY);
            line.LineWidth = 2;
            line.Color = ScottPlot.Colors.Red;
            line.LegendText = FormattableString.Invariant(
                $"Drift: {result.GlobalDrift.Slope:E3} ns/day");

            plt.Title("BIPM UTCr — Ensemble Mean & Global Drift");
            plt.XLabel("MJD");
            plt.YLabel("[UTCr-UTC(k)] (ns)");
            plt.ShowLegend();

            plt.SavePng(path, width, height);
        }

        public static void SaveLabSlopesHistogram(
            string path,
            UtcrAnalysisResult result,
            int width = 1000,
            int height = 600)
        {
            var plt = new ScottPlot.Plot();

            double[] slopes = result.LabSlopes
                .Select(ls => ls.Slope).ToArray();

            if (slopes.Length == 0)
            {
                plt.Title("Lab Drift Slopes — No data");
                plt.SavePng(path, width, height);
                return;
            }

            int binCount = 25;
            double minS = slopes.Min();
            double maxS = slopes.Max();
            double binWidth = (maxS - minS) / binCount;
            if (binWidth < 1e-12) binWidth = 0.001;

            double[] counts = new double[binCount];
            double[] binCenters = new double[binCount];
            for (int i = 0; i < binCount; i++)
                binCenters[i] = minS + (i + 0.5) * binWidth;

            foreach (double s in slopes)
            {
                int idx = (int)((s - minS) / binWidth);
                if (idx >= binCount) idx = binCount - 1;
                if (idx < 0) idx = 0;
                counts[idx]++;
            }

            var bars = plt.Add.Bars(binCenters, counts);
            foreach (var bar in bars.Bars)
            {
                bar.Size = binWidth * 0.8;
                bar.FillColor = ScottPlot.Color.FromHex("#1f77b4");
            }

            bars.LegendText = $"{slopes.Length} labs";

            // Mark ensemble slope with a vertical line
            var vLine = plt.Add.VerticalLine(result.GlobalDrift.Slope);
            vLine.Color = ScottPlot.Colors.Red;
            vLine.LineWidth = 2;
            vLine.LegendText = "Ensemble slope";

            plt.Title("Lab Drift Slope Distribution");
            plt.XLabel("Slope (ns/day)");
            plt.YLabel("Count");
            plt.ShowLegend();

            plt.SavePng(path, width, height);
        }

        /// <summary>
        /// Plots the demeaned ensemble mean vs MJD.
        /// </summary>
        private static void SaveDemeanedEnsemblePlot(
            string path, UtcrAnalysisResult result)
        {
            if (result.DemeanedEnsemble.Count == 0) return;

            using var plt = new ScottPlot.Plot();
            var xs = result.DemeanedEnsemble.Select(e => e.Mjd).ToArray();
            var ys = result.DemeanedEnsemble.Select(e => e.Mean).ToArray();

            var scatter = plt.Add.Scatter(xs, ys);
            scatter.MarkerSize = 3;
            scatter.Color = ScottPlot.Color.FromHex("#1f77b4");
            scatter.LegendText = "Demeaned ensemble mean";

            if (Math.Abs(result.DemeanedDrift.Slope) > 0)
            {
                double[] fitYs = xs.Select(x =>
                    result.DemeanedDrift.Slope * x + result.DemeanedDrift.Intercept)
                    .ToArray();
                var line = plt.Add.Scatter(xs, fitYs);
                line.MarkerSize = 0;
                line.Color = ScottPlot.Colors.Red;
                line.LegendText = "Linear fit";
            }

            var hLine = plt.Add.HorizontalLine(0, 0.5f);
            hLine.Color = ScottPlot.Colors.Black;
            hLine.LegendText = "Zero line";

            plt.Title("UTCr Demeaned Ensemble Drift");
            plt.XLabel("MJD");
            plt.YLabel("Demeaned [UTCr - UTC(k)] (ns)");
            plt.ShowLegend();
            plt.Save(path, 1200, 800);
        }

        /// <summary>
        /// Overlays mean and median ensemble drift for comparison.
        /// </summary>
        private static void SaveMedianVsMeanPlot(
            string path, UtcrAnalysisResult result)
        {
            if (result.DemeanedEnsemble.Count == 0
                || result.MedianEnsemble.Count == 0) return;

            using var plt = new ScottPlot.Plot();

            // Demeaned mean
            var xs = result.DemeanedEnsemble.Select(e => e.Mjd).ToArray();
            var meanYs = result.DemeanedEnsemble.Select(e => e.Mean).ToArray();
            var meanScatter = plt.Add.Scatter(xs, meanYs);
            meanScatter.MarkerSize = 3;
            meanScatter.Color = ScottPlot.Color.FromHex("#1f77b4");
            meanScatter.LegendText = "Demeaned mean";

            // Median
            var medXs = result.MedianEnsemble.Select(e => e.Mjd).ToArray();
            var medYs = result.MedianEnsemble.Select(e => e.Median).ToArray();
            var medScatter = plt.Add.Scatter(medXs, medYs);
            medScatter.MarkerSize = 3;
            medScatter.Color = ScottPlot.Colors.Green;
            medScatter.LegendText = "Median";

            var hLine = plt.Add.HorizontalLine(0, 0.5f);
            hLine.Color = ScottPlot.Colors.Black;
            hLine.LegendText = "Zero line";

            plt.Title("UTCr Drift: Mean vs Median (Stable Labs)");
            plt.XLabel("MJD");
            plt.YLabel("Offset (ns)");
            plt.ShowLegend();
            plt.Save(path, 1200, 800);
        }

        /// <summary>
        /// Overlays all stable-lab demeanded series as faint lines.
        /// </summary>
        private static void SaveStableLabOverlay(
            string path, UtcrAnalysisResult result)
        {
            if (result.StableLabSeries.Count == 0) return;

            using var plt = new ScottPlot.Plot();

            int idx = 0;
            var palette = new[] { "#1f77b4", "#ff7f0e", "#2ca02c", "#d62728",
                "#9467bd", "#8c564b", "#e377c2", "#7f7f7f", "#bcbd22", "#17becf" };

            foreach (var lab in result.StableLabSeries.OrderBy(l => l.Lab))
            {
                var xs = lab.Samples.Select(p => p.Mjd).ToArray();
                var ys = lab.Samples.Select(p => p.ValueNs).ToArray();
                string colorHex = palette[idx % palette.Length];
                var labScatter = plt.Add.Scatter(xs, ys);
                labScatter.MarkerSize = 0;
                labScatter.Color = ScottPlot.Color.FromHex(colorHex);
                labScatter.LegendText = lab.Lab;
                idx++;
            }

            // Overlay demeaned ensemble mean in black
            if (result.DemeanedEnsemble.Count > 0)
            {
                var xsE = result.DemeanedEnsemble.Select(e => e.Mjd).ToArray();
                var ysE = result.DemeanedEnsemble.Select(e => e.Mean).ToArray();
                var ensScatter = plt.Add.Scatter(xsE, ysE);
                ensScatter.MarkerSize = 4;
                ensScatter.LineWidth = 2;
                ensScatter.Color = ScottPlot.Colors.Black;
                ensScatter.LegendText = "Ensemble mean";
            }

            plt.Title("UTCr Stable-Laboratory Demeaned Overlay");
            plt.XLabel("MJD");
            plt.YLabel("Demaneaned offset (ns)");
            if (result.StableLabSeries.Count <= 15)
                plt.ShowLegend();
            plt.Save(path, 1200, 800);
        }

        // ── Step 10: Console summary ────────────────────────────────

        /// <summary>
        /// Returns a formatted multi-line summary for console output.
        /// </summary>
        public static string FormatSummary(UtcrAnalysisResult result)
        {
            var inv = CultureInfo.InvariantCulture;
            var mjds = result.AllMeasurements
                .Select(m => m.Mjd).Distinct().OrderBy(m => m).ToList();
            double mjdMin = mjds.FirstOrDefault();
            double mjdMax = mjds.LastOrDefault();

            int sameSign = result.LabSlopes.Count(ls =>
                Math.Sign(ls.Slope) == Math.Sign(result.GlobalDrift.Slope));
            double sameSignPct = result.LabSlopes.Count > 0
                ? 100.0 * sameSign / result.LabSlopes.Count
                : 0.0;

            return string.Format(inv,
                "── BIPM UTCr Analysis ──\n" +
                "Files parsed      : {0}\n" +
                "Measurements      : {1:N0}\n" +
                "Labs (≥20 obs)    : {2}\n" +
                "MJD range         : {3:F0} – {4:F0}\n" +
                "Ensemble points   : {5}\n" +
                "─────────────────────────\n" +
                "Global drift slope: {6:E3} ns/day\n" +
                "Global intercept  : {7:F2} ns\n" +
                "Avg lab slope     : {8:E3} ns/day\n" +
                "Same-sign labs    : {9}/{10} ({11:F0}%)\n" +
                "Avg pairwise corr : {12:F4}\n" +
                "─────────────────────────\n" +
                "Candidate B(t) v1 : {13}\n" +
                "─────────────────────────\n" +
                "── v2 Refined ──\n" +
                "Stable labs       : {14}\n" +
                "Mean slope (raw)  : {15:E3} ns/day\n" +
                "Mean slope (dem.) : {16:E3} ns/day\n" +
                "Median slope      : {17:E3} ns/day\n" +
                "CM correlation    : {18:F4}\n" +
                "Same-sign daily % : {19:F4}\n" +
                "─────────────────────────\n" +
                "Candidate B(t) v2 : {20}",
                result.AllMeasurements
                    .Select(m => Path.GetFileName("UTCr_")).Distinct().Count(),
                result.AllMeasurements.Count,
                result.LabSeries.Count,
                mjdMin, mjdMax,
                result.Ensemble.Count,
                result.GlobalDrift.Slope,
                result.GlobalDrift.Intercept,
                result.AverageLabSlope,
                sameSign, result.LabSlopes.Count, sameSignPct,
                result.AveragePairwiseCorrelation,
                result.CandidateGlobalDrift ? "YES" : "NO",
                result.StableLabCount,
                result.GlobalDrift.Slope,
                result.DemeanedDrift.Slope,
                result.MedianDrift.Slope,
                result.CommonModeCorrelation,
                result.SameSignDailyFraction,
                result.CandidateGlobalDriftV2 ? "YES" : "NO");
        }
    }
}
