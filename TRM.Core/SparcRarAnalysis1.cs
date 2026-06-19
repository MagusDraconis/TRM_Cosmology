using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace TRM.Core
{

    public class SparcRarAnalysis1
    {
        // Conversion factor from (km/s)^2 / kpc to m/s^2:
        // (10^3 m/s)^2 / (3.08567758 * 10^19 m) = 10^6 / 3.08567758 * 10^19
        private const double Kms2KpcToMs2 = 3.240779289e-14;
        static HashSet<string> blacklist = new(StringComparer.OrdinalIgnoreCase)
            {
                "CamB", "NGC6789", "UGC07399", "UGC11557", "KK98-251",
                "UGC07125", "UGC08837", "F568-V1", "NGC2915", "UGC06667"
            };


        public static List<RarPoint> ParseRarFromZip(string zipPath, double upsilonDisk = 0.5, double upsilonBulge = 0.7)
        {
            var rarPoints = new List<RarPoint>();

            if (!File.Exists(zipPath))
                throw new FileNotFoundException($"File not found: {zipPath}.");

            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (!entry.FullName.EndsWith(".rot", StringComparison.OrdinalIgnoreCase) &&
                        !entry.FullName.EndsWith(".dat", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string galaxyName = Path.GetFileNameWithoutExtension(entry.Name)
                                        .Replace("_rotmod", "", StringComparison.OrdinalIgnoreCase);

                    using (Stream stream = entry.Open())
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            string trimmed = line.Trim();
                            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                                continue;

                            var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 5)
                            {
                                try
                                {
                                    double r = double.Parse(parts[0], CultureInfo.InvariantCulture);
                                    double vObs = double.Parse(parts[1], CultureInfo.InvariantCulture);

                                    double vGas, vDisk, vBulge;
                                    double errVobs = 0;

                                    if (parts.Length >= 6)
                                    {
                                        errVobs = double.Parse(parts[2], CultureInfo.InvariantCulture);
                                        vGas = double.Parse(parts[3], CultureInfo.InvariantCulture);
                                        vDisk = double.Parse(parts[4], CultureInfo.InvariantCulture);
                                        vBulge = double.Parse(parts[5], CultureInfo.InvariantCulture);
                                    }
                                    else
                                    {
                                        vGas = double.Parse(parts[2], CultureInfo.InvariantCulture);
                                        vDisk = double.Parse(parts[3], CultureInfo.InvariantCulture);
                                        vBulge = double.Parse(parts[4], CultureInfo.InvariantCulture);
                                    }

                                    // --- PHYSICAL PRUNING ---
                                    // 1) Exclude division-by-zero and unstable core regions (R < 0.5 kpc)
                                    if (r < 0.5) continue;

                                    // 2) Exclude heavily noisy points (Vobs error > 15% or > 20 km/s)
                                    if (parts.Length >= 6 && (errVobs > 20.0 || (vObs > 0 && errVobs / vObs > 0.15)))
                                        continue;

                                    // 3) Keep only points with stable rotation
                                    if (vObs < 10.0) continue;

                                    double gObsAstronomical = (vObs * vObs) / r;
                                    double gObsMs2 = gObsAstronomical * Kms2KpcToMs2;

                                    double vDiskSq = vDisk > 0 ? vDisk * vDisk : 0;
                                    double vBulgeSq = vBulge > 0 ? vBulge * vBulge : 0;
                                    double vGasSq = vGas > 0 ? vGas * vGas : 0;

                                    double vBarSq = vGasSq + (upsilonDisk * vDiskSq) + (upsilonBulge * vBulgeSq);

                                    if (vBarSq > 0)
                                    {
                                        double gBarAstronomical = vBarSq / r;
                                        double gBarMs2 = gBarAstronomical * Kms2KpcToMs2;

                                        // Exclude asymmetric-shock regime (when observed energy is far below baryonic estimate)
                                        if (gObsMs2 > 0 && gBarMs2 > 0 && (gObsMs2 / gBarMs2 > 0.01))
                                        {
                                            rarPoints.Add(new RarPoint(
                                                galaxyName, r, vObs, vGas, vDisk, vBulge, gObsMs2, gBarMs2
                                            ));
                                        }
                                    }

                                }
                                catch (FormatException)
                                {
                                    continue;
                                }
                            }
                        }
                    }
                }
            }

            return rarPoints;
        }

        public static List<RarPoint> ParseRarWithFixedWidthInclinationFilter(string zipPath, string mrtPath, double upsilonDisk = 0.5, double upsilonBulge = 0.7)
        {
            var validGalaxies = new Dictionary<string, (double Inclination, int Quality)>(StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(zipPath))
                throw new FileNotFoundException($"File not found: {zipPath}.");

            if (!File.Exists(mrtPath))
                throw new FileNotFoundException($"File not found: {mrtPath}.");

            bool dataSectionStarted = false;
            int separatorCount = 0;

            foreach (var line in File.ReadLines(mrtPath))
            {
                string trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                // Data section starts after the third "-----" separator line
                if (trimmed.StartsWith("----", StringComparison.Ordinal))
                {
                    separatorCount++;
                    if (separatorCount >= 3)
                        dataSectionStarted = true;

                    continue;
                }

                if (!dataSectionStarted || trimmed.StartsWith("#", StringComparison.Ordinal))
                    continue;

                var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 18)
                    continue;

                string galaxyKey = NormalizeGalaxyKey(parts[0]);
                if (double.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out double inc) &&
                    int.TryParse(parts[17], NumberStyles.Integer, CultureInfo.InvariantCulture, out int quality) &&
                    !validGalaxies.ContainsKey(galaxyKey))
                {
                    validGalaxies.Add(galaxyKey, (inc, quality));
                }
            }

            var rarPoints = new List<RarPoint>();

            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (!entry.FullName.EndsWith(".rot", StringComparison.OrdinalIgnoreCase) &&
                        !entry.FullName.EndsWith(".dat", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Normalize galaxy name from file name
                    string galaxyName = Path.GetFileNameWithoutExtension(entry.Name)
                                        .Replace("_rotmod", "", StringComparison.OrdinalIgnoreCase)
                                        .Trim();
                    string galaxyKey = NormalizeGalaxyKey(galaxyName);

                    // Cross-match with the master reference table
                    if (validGalaxies.TryGetValue(galaxyKey, out var galaxyMeta))
                    {
                        // Rule-based filter: face-on galaxies strongly distort circular velocity
                        if (galaxyMeta.Inclination < 30.0 || galaxyMeta.Quality >= 3)
                            continue;
                    }
                    else
                    {
                        // Not found in table 1 -> exclude to preserve data quality
                        continue;
                    }

                    using (Stream stream = entry.Open())
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string dataLine;
                        while ((dataLine = reader.ReadLine()) != null)
                        {
                            string trimmed = dataLine.Trim();
                            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                                continue;

                            var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 5)
                            {
                                if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double r) ||
                                    !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double vObs))
                                    continue;

                                double errVobs = 0;
                                int componentStart = 2;
                                if (parts.Length >= 6)
                                {
                                    if (!double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out errVobs))
                                        continue;
                                    componentStart = 3;
                                }

                                if (parts.Length <= componentStart + 2)
                                    continue;

                                if (!double.TryParse(parts[componentStart], NumberStyles.Float, CultureInfo.InvariantCulture, out double vGas) ||
                                    !double.TryParse(parts[componentStart + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out double vDisk) ||
                                    !double.TryParse(parts[componentStart + 2], NumberStyles.Float, CultureInfo.InvariantCulture, out double vBulge))
                                    continue;

                                // Local pruning
                                if (r < 0.5) continue;
                                if (vObs < 10.0) continue;
                                if (parts.Length >= 6 && (errVobs > 20.0 || (vObs > 0 && errVobs / vObs > 0.15)))
                                    continue;

                                double gObsAstronomical = (vObs * vObs) / r;
                                double gObsMs2 = gObsAstronomical * Kms2KpcToMs2;

                                double vDiskSq = vDisk > 0 ? vDisk * vDisk : 0;
                                double vBulgeSq = vBulge > 0 ? vBulge * vBulge : 0;
                                double vGasSq = vGas > 0 ? vGas * vGas : 0;

                                double vBarSq = vGasSq + (upsilonDisk * vDiskSq) + (upsilonBulge * vBulgeSq);

                                if (vBarSq > 0)
                                {
                                    double gBarAstronomical = vBarSq / r;
                                    double gBarMs2 = gBarAstronomical * Kms2KpcToMs2;

                                    if (gObsMs2 > 0 && gBarMs2 > 0 && (gObsMs2 / gBarMs2 > 0.01))
                                    {
                                        rarPoints.Add(new RarPoint(
                                            galaxyName, r, vObs, vGas, vDisk, vBulge, gObsMs2, gBarMs2
                                        ));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return rarPoints;
        }




        public record RarBinResult(
                    double LogGbarCenter,
                    double MeanLogGobs,
                    double StandardDeviation,
                    int PointCount
                );

        public static List<RarBinResult> ComputeRarProfiles(List<RarPoint> points, double binSize = 0.2)
        {
            // Group data in log10 space of baryonic acceleration
            var groupedPoints = points
                .GroupBy(p => Math.Floor(Math.Log10(p.GbarMs2) / binSize) * binSize)
                .OrderBy(g => g.Key)
                .ToList();

            var binResults = new List<RarBinResult>();

            foreach (var group in groupedPoints)
            {
                double binCenter = group.Key + (binSize / 2.0);

                // Log-transformed observed accelerations in this bin
                var logGobsValues = group.Select(p => Math.Log10(p.GobsMs2)).ToList();

                int count = logGobsValues.Count;
                if (count < 5) continue; // Ignore low-population edge bins

                double meanLogGobs = logGobsValues.Average();

                // Compute variance and standard deviation
                double sumOfSquares = logGobsValues.Sum(v => Math.Pow(v - meanLogGobs, 2));
                double stdDev = Math.Sqrt(sumOfSquares / count);

                binResults.Add(new RarBinResult(binCenter, meanLogGobs, stdDev, count));
            }

            return binResults;
        }
        /// <summary>
        /// Computes the theoretical observed acceleration using
        /// the standard RAR interpolation function for a given a0 value.
        /// </summary>
        public static double PredictGobs(double gBar, double a0)
        {
            if (gBar <= 0 || a0 <= 0) return 0;

            // g_obs = g_bar / (1 - exp(-sqrt(g_bar / a_0)))
            double absoluteDeviation = Math.Sqrt(gBar / a0);
            return gBar / (1.0 - Math.Exp(-absoluteDeviation));
        }

        /// <summary>
        /// Runs a high-precision two-stage grid search to find log10(a0)
        /// that minimizes the sum of squared log-residuals across all points.
        /// </summary>
        public static (double BestLogA0, double BestA0, double RmsError) FitA0(
            List<RarPoint> points,
            Dictionary<string, double>? inclinations = null)
        {


            if (points == null || points.Count == 0)
                throw new ArgumentException("No data points were provided for the fit.");

            var normalizedInclinations = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            if (inclinations is { Count: > 0 })
            {
                foreach (var kvp in inclinations!)
                {
                    var key = NormalizeGalaxyKey(kvp.Key);
                    if (!normalizedInclinations.ContainsKey(key))
                        normalizedInclinations[key] = kvp.Value;
                }
            }

            // Build heuristic Upsilon map before fitting
            var upsilonDiskMap = EstimateUpsilonMap(points);

            double bestLogA0 = 0;
            double minSsr = double.MaxValue; // Sum of Squared Residuals

            // First stage: coarse grid from -11.0 to -9.0 in steps of 0.01
            for (double logA0 = -11.0; logA0 <= -9.0; logA0 += 0.01)
            {
                double a0 = Math.Pow(10, logA0);
                double ssr = CalculateWeightedSsr(points, a0, normalizedInclinations, upsilonDiskMap);

                if (ssr < minSsr)
                {
                    minSsr = ssr;
                    bestLogA0 = logA0;
                }
            }

            // Second stage: fine sweep around the minimum (±0.02) with 0.0001 step
            double startFine = bestLogA0 - 0.02;
            double endFine = bestLogA0 + 0.02;

            for (double logA0 = startFine; logA0 <= endFine; logA0 += 0.0001)
            {
                double a0 = Math.Pow(10, logA0);
                double ssr = CalculateWeightedSsr(points, a0, normalizedInclinations, upsilonDiskMap);

                if (ssr < minSsr)
                {
                    minSsr = ssr;
                    bestLogA0 = logA0;
                }
            }

            // Compute root mean square error
            double rmsError = Math.Sqrt(minSsr);
            double bestA0 = Math.Pow(10, bestLogA0);

            return (bestLogA0, bestA0, rmsError);
        }

        private static string NormalizeGalaxyKey(string name)
    => name.Replace("_rotmod", "", StringComparison.OrdinalIgnoreCase)
           .Replace(" ", "", StringComparison.Ordinal)
           .Trim();

        public static void AnalyzeOutliers(
    List<RarPoint> points,
    double bestA0,
    Dictionary<string, double> inclinations,
    Dictionary<string, double> upsilonDiskMap)
        {
            Console.WriteLine("\n--- Galaxy residual analysis ---");

            // Group by galaxy to inspect average error per galaxy
            var residuals = points.GroupBy(p => p.GalaxyName)
                .Select(group =>
                {
                    double sumResidual = 0;
                    int count = 0;

                    // Recompute residual for this galaxy using best A0
                    foreach (var p in group)
                    {
                        string galaxyKey = NormalizeGalaxyKey(p.GalaxyName);
                        double inc = inclinations.GetValueOrDefault(galaxyKey, 60.0);
                        double sinI = Math.Sin(inc * Math.PI / 180.0);
                        double upsilonDisk = upsilonDiskMap.GetValueOrDefault(galaxyKey, 0.5);

                        double vBarSq = (p.Vgas * p.Vgas) + (upsilonDisk * p.Vdisk * p.Vdisk) + (0.7 * p.Vbulge * p.Vbulge);
                        double gBarMs2 = (vBarSq / p.RadiusKpc) * Kms2KpcToMs2;

                        double gObsPredicted = PredictGobs(gBarMs2, bestA0);
                        if (gObsPredicted > 0)
                        {
                            double res = Math.Abs(Math.Log10(p.GobsMs2) - Math.Log10(gObsPredicted));
                            sumResidual += res;
                            count++;
                        }
                    }
                    return new { Name = group.Key, AvgResidual = count > 0 ? sumResidual / count : 0 };
                })
                .OrderByDescending(x => x.AvgResidual) // Worst outliers first
                .ToList();

            foreach (var item in residuals.Take(20)) // Show top 20 problematic galaxies
            {
                Console.WriteLine($"Galaxy: {item.Name,-15} | Avg residual: {item.AvgResidual:F4}");
            }
        }
        private static double CalculateWeightedSsr(
                List<RarPoint> points,
                double a0,
                Dictionary<string, double> inclinations,
                Dictionary<string, double> upsilonDiskMap) // Per-galaxy mapping
        {
            double totalWeight = 0;
            double weightedSsr = 0;
            foreach (var p in points)
            {
                string galaxyKey = NormalizeGalaxyKey(p.GalaxyName);
                // Check whether galaxy is blacklisted
                if (blacklist.Contains(galaxyKey))
                {
                    continue; // Ignore this galaxy entirely
                }
                // 1) Get inclination
                double inc = inclinations.GetValueOrDefault(galaxyKey, 60.0); // Fallback if inclination is unavailable
                double sinI = Math.Sin(inc * Math.PI / 180.0);
                double weight = sinI * sinI;

                // 2) Get per-galaxy Upsilon (default 0.5 if missing)
                double upsilonDisk = upsilonDiskMap.GetValueOrDefault(galaxyKey, 0.5);

                // 3) Compute baryonic acceleration with dynamic Upsilon
                // g_bar = (v_gas^2 + upsilon * v_disk^2 + upsilon_bulge * v_bulge^2) / r
                double vDiskSq = p.Vdisk * p.Vdisk;
                double vBulgeSq = p.Vbulge * p.Vbulge;
                double vGasSq = p.Vgas * p.Vgas;

                // Assume bulge Upsilon remains stable (typically older stellar population)
                double vBarSq = vGasSq + (upsilonDisk * vDiskSq) + (0.7 * vBulgeSq);
                double gBarMs2 = (vBarSq / p.RadiusKpc) * Kms2KpcToMs2;

                // Remaining log-residual computation
                double logGobsActual = Math.Log10(p.GobsMs2);
                double gObsPredicted = PredictGobs(gBarMs2, a0);
                if (gObsPredicted <= 0) continue;

                double residual = logGobsActual - Math.Log10(gObsPredicted);

                weightedSsr += weight * (residual * residual);
                totalWeight += weight;
                // Debug step:
                
            }
            
            
            return totalWeight <= 0 ? double.MaxValue : weightedSsr / totalWeight;
        }

        public static Dictionary<string, double> EstimateUpsilonMap(List<RarPoint> allPoints)
        {
            var map = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            if (allPoints == null || allPoints.Count == 0)
                return map;

            // Heuristic:
            // - Gasanteil > 30% => Upsilon_disk = 0.3
            // - sonst         => Upsilon_disk = 0.5
            foreach (var galaxyGroup in allPoints.GroupBy(p => NormalizeGalaxyKey(p.GalaxyName)))
            {
                double gasContribution = 0;
                double stellarContribution = 0;

                foreach (var p in galaxyGroup)
                {
                    double vGasSq = p.Vgas > 0 ? p.Vgas * p.Vgas : 0;
                    double vDiskSq = p.Vdisk > 0 ? p.Vdisk * p.Vdisk : 0;
                    double vBulgeSq = p.Vbulge > 0 ? p.Vbulge * p.Vbulge : 0;

                    gasContribution += vGasSq;
                    // Reference stellar contribution with default values
                    stellarContribution += (0.5 * vDiskSq) + (0.7 * vBulgeSq);
                }

                double baryonicTotal = gasContribution + stellarContribution;
                double gasFraction = baryonicTotal > 0 ? gasContribution / baryonicTotal : 0;

                // Instead of a hard step:
                // Scale Upsilon between 0.5 (0% gas) and 0.3 (50% gas)
                double upsilonDisk = 0.5 - (gasFraction * 0.4);
                // Keep value in a physically meaningful range
                upsilonDisk = Math.Max(0.3, Math.Min(0.5, upsilonDisk));

                map[galaxyGroup.Key] = upsilonDisk;
            }

            return map;
        }

    }
}
