using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace TRM.Core;

public enum ModelType { MOND, ClockworkTRM }


public class SparcRarAnalysis
{
    

    // Science-based blacklist for strongly disturbed kinematics
    static HashSet<string> blacklist = new(StringComparer.OrdinalIgnoreCase)
    {
        "CamB", "NGC6789", "UGC07399", "UGC11557", "KK98-251",
        "UGC07125", "UGC08837", "F568-V1", "NGC2915", "UGC06667"
    };

    public static List<RarPoint> ParseRarFromZip(string zipPath, double upsilonDisk = 0.5, double upsilonBulge = 0.7)
    {
        var rarPoints = new List<RarPoint>();
        if (!File.Exists(zipPath)) throw new FileNotFoundException($"File not found: {zipPath}.");

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
                        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;

                        var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 5) continue;

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

                            if (r < 0.5 || vObs < 10.0) continue;
                            if (parts.Length >= 6 && (errVobs > 20.0 || (vObs > 0 && errVobs / vObs > 0.15))) continue;

                            double gObsMs2 = ((vObs * vObs) / r) * PhysicalConstants.Kms2KpcToMs2;
                            double vDiskSq = vDisk > 0 ? vDisk * vDisk : 0;
                            double vBulgeSq = vBulge > 0 ? vBulge * vBulge : 0;
                            double vGasSq = vGas > 0 ? vGas * vGas : 0;

                            double vBarSq = vGasSq + (upsilonDisk * vDiskSq) + (upsilonBulge * vBulgeSq);
                            if (vBarSq <= 0) continue;

                            double gBarMs2 = (vBarSq / r) * PhysicalConstants.Kms2KpcToMs2;

                            if (gObsMs2 > 0 && gBarMs2 > 0 && (gObsMs2 / gBarMs2 > 0.01))
                            {
                                rarPoints.Add(new RarPoint(galaxyName, r, vObs, vGas, vDisk, vBulge, gObsMs2, gBarMs2));
                            }
                        }
                        catch (FormatException) { continue; }
                    }
                }
            }
        }
        return rarPoints;
    }

    public static List<RarPoint> ParseRarWithFixedWidthInclinationFilter(string zipPath, string mrtPath, double upsilonDisk = 0.5, double upsilonBulge = 0.7)
    {
        var validGalaxies = new Dictionary<string, (double Inclination, int Quality)>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(zipPath) || !File.Exists(mrtPath)) throw new FileNotFoundException("SPARC input files are incomplete.");

        bool dataSectionStarted = false;
        int separatorCount = 0;

        foreach (var line in File.ReadLines(mrtPath))
        {
            string trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            if (trimmed.StartsWith("----", StringComparison.Ordinal))
            {
                separatorCount++;
                if (separatorCount >= 3) dataSectionStarted = true;
                continue;
            }

            if (!dataSectionStarted || trimmed.StartsWith("#", StringComparison.Ordinal)) continue;

            var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 18) continue;

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

                string galaxyName = Path.GetFileNameWithoutExtension(entry.Name).Replace("_rotmod", "", StringComparison.OrdinalIgnoreCase).Trim();
                string galaxyKey = NormalizeGalaxyKey(galaxyName);

                if (validGalaxies.TryGetValue(galaxyKey, out var galaxyMeta))
                {
                    if (galaxyMeta.Inclination < 30.0 || galaxyMeta.Quality >= 3) continue;
                }
                else continue;

                using (Stream stream = entry.Open())
                using (StreamReader reader = new StreamReader(stream))
                {
                    string dataLine;
                    while ((dataLine = reader.ReadLine()) != null)
                    {
                        string trimmed = dataLine.Trim();
                        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;

                        var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 5) continue;

                        if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double r) ||
                            !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double vObs))
                            continue;

                        double errVobs = 0;
                        int componentStart = 2;
                        if (parts.Length >= 6)
                        {
                            if (!double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out errVobs)) continue;
                            componentStart = 3;
                        }

                        if (parts.Length <= componentStart + 2) continue;

                        if (!double.TryParse(parts[componentStart], NumberStyles.Float, CultureInfo.InvariantCulture, out double vGas) ||
                            !double.TryParse(parts[componentStart + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out double vDisk) ||
                            !double.TryParse(parts[componentStart + 2], NumberStyles.Float, CultureInfo.InvariantCulture, out double vBulge))
                            continue;

                        if (r < 0.5 || vObs < 10.0) continue;
                        if (parts.Length >= 6 && (errVobs > 20.0 || (vObs > 0 && errVobs / vObs > 0.15))) continue;

                        double gObsMs2 = ((vObs * vObs) / r) * PhysicalConstants.Kms2KpcToMs2;
                        double vDiskSq = vDisk > 0 ? vDisk * vDisk : 0;
                        double vBulgeSq = vBulge > 0 ? vBulge * vBulge : 0;
                        double vGasSq = vGas > 0 ? vGas * vGas : 0;

                        double vBarSq = vGasSq + (upsilonDisk * vDiskSq) + (upsilonBulge * vBulgeSq);
                        if (vBarSq <= 0) continue;

                        double gBarMs2 = (vBarSq / r) * PhysicalConstants.Kms2KpcToMs2;

                        if (gObsMs2 > 0 && gBarMs2 > 0 && (gObsMs2 / gBarMs2 > 0.01))
                        {
                            rarPoints.Add(new RarPoint(galaxyName, r, vObs, vGas, vDisk, vBulge, gObsMs2, gBarMs2));
                        }
                    }
                }
            }
        }
        return rarPoints;
    }

    // Selectable physical law (classical MOND vs. Clockwork Cosmology)
    public static double PredictGobs(double gBar, double a0, ModelType model)
    {
        if (gBar <= 0 || a0 <= 0) return 0;

        if (model == ModelType.ClockworkTRM)
        {
            // TRM V2.2: g_obs = g_bar + sqrt(g_bar * a_0)
            // Interpreted as non-local temporal phase synchronization with the background
            return gBar + Math.Sqrt(gBar * a0);
        }
        else
        {
            // Standard MOND (McGaugh/Lelli)
            double absoluteDeviation = Math.Sqrt(gBar / a0);
            return gBar / (1.0 - Math.Exp(-absoluteDeviation));
        }
    }

    // High-precision 2D co-fit for a0 and Upsilon scaling factor
    public static (double BestLogA0, double BestA0, double RmsError) FitA0(
        List<RarPoint> points,
        Dictionary<string, double>? inclinations = null,
        ModelType model = ModelType.ClockworkTRM) // Default to TRM model
    {
        if (points == null || points.Count == 0)
            throw new ArgumentException("No data points were provided.");

        var normalizedInclinations = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        if (inclinations is { Count: > 0 })
        {
            foreach (var kvp in inclinations)
            {
                var key = NormalizeGalaxyKey(kvp.Key);
                if (!normalizedInclinations.ContainsKey(key))
                    normalizedInclinations[key] = kvp.Value;
            }
        }

        double bestLogA0 = 0;
        double minSsr = double.MaxValue;
        double bestUpsilonScale = 1.0;

        // 2D parameter sweep across a0 and gas-heuristic scaling
        // Allow a global tuning factor from 0.8 to 1.2
        for (double logA0 = -11.0; logA0 <= -9.0; logA0 += 0.01)
        {
            for (double upsScale = 0.8; upsScale <= 1.2; upsScale += 0.05)
            {
                double a0 = Math.Pow(10, logA0);
                var currentUpsilonMap = EstimateUpsilonMap(points, upsScale);
                double ssr = CalculateWeightedSsr(points, a0, normalizedInclinations, currentUpsilonMap, model);

                if (ssr < minSsr)
                {
                    minSsr = ssr;
                    bestLogA0 = logA0;
                    bestUpsilonScale = upsScale;
                }
            }
        }

        // Second stage: fine-grained local sweep around the minimum
        double startFine = bestLogA0 - 0.02;
        double endFine = bestLogA0 + 0.02;

        for (double logA0 = startFine; logA0 <= endFine; logA0 += 0.0001)
        {
            double a0 = Math.Pow(10, logA0);
            var currentUpsilonMap = EstimateUpsilonMap(points, bestUpsilonScale); // Fixed at best scale
            double ssr = CalculateWeightedSsr(points, a0, normalizedInclinations, currentUpsilonMap, model);

            if (ssr < minSsr)
            {
                minSsr = ssr;
                bestLogA0 = logA0;
            }
        }

        double rmsError = Math.Sqrt(minSsr);
        double bestA0 = Math.Pow(10, bestLogA0);

        return (bestLogA0, bestA0, rmsError);
    }

    private static double CalculateWeightedSsr(
        List<RarPoint> points,
        double a0,
        Dictionary<string, double> inclinations,
        Dictionary<string, double> upsilonDiskMap,
        ModelType model)
    {
        double totalWeight = 0;
        double weightedSsr = 0;

        foreach (var p in points)
        {
            string galaxyKey = NormalizeGalaxyKey(p.GalaxyName);
            if (blacklist.Contains(galaxyKey)) continue;

            double inc = inclinations.GetValueOrDefault(galaxyKey, 60.0);
            double sinI = Math.Sin(inc * Math.PI / 180.0);
            double weight = sinI * sinI; // Weighted by orbital stability

            double upsilonDisk = upsilonDiskMap.GetValueOrDefault(galaxyKey, 0.5);

            double vDiskSq = p.Vdisk * p.Vdisk;
            double vBulgeSq = p.Vbulge * p.Vbulge;
            double vGasSq = p.Vgas * p.Vgas;

            // Keep bulge at 0.7 (older stellar population), apply dynamic Upsilon to disk
            double vBarSq = vGasSq + (upsilonDisk * vDiskSq) + (0.7 * vBulgeSq);
            double gBarMs2 = (vBarSq / p.RadiusKpc) * PhysicalConstants.Kms2KpcToMs2;

            double logGobsActual = Math.Log10(p.GobsMs2);
            double gObsPredicted = PredictGobs(gBarMs2, a0, model); // Use selected model
            if (gObsPredicted <= 0) continue;

            double residual = logGobsActual - Math.Log10(gObsPredicted);

            weightedSsr += weight * (residual * residual);
            totalWeight += weight;
        }

        return totalWeight <= 0 ? double.MaxValue : weightedSsr / totalWeight;
    }

    // Extended heuristic with scale factor for co-fit
    public static Dictionary<string, double> EstimateUpsilonMap(List<RarPoint> allPoints, double globalScaleFactor = 1.0)
    {
        var map = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        if (allPoints == null || allPoints.Count == 0) return map;

        foreach (var galaxyGroup in allPoints.GroupBy(p => NormalizeGalaxyKey(p.GalaxyName)))
        {
            double gasContribution = 0;
            double stellarContribution = 0;

            foreach (var p in galaxyGroup)
            {
                gasContribution += p.Vgas > 0 ? p.Vgas * p.Vgas : 0;
                stellarContribution += (0.5 * p.Vdisk * p.Vdisk) + (0.7 * p.Vbulge * p.Vbulge);
            }

            double baryonicTotal = gasContribution + stellarContribution;
            double gasFraction = baryonicTotal > 0 ? gasContribution / baryonicTotal : 0;

            // Base scaling from gas fraction
            double upsilonDisk = 0.5 - (gasFraction * 0.4);

            // Apply global optimization parameter from co-fit
            upsilonDisk *= globalScaleFactor;

            upsilonDisk = Math.Max(0.3, Math.Min(0.5, upsilonDisk));
            map[galaxyGroup.Key] = upsilonDisk;
        }

        return map;
    }

    private static string NormalizeGalaxyKey(string name) =>
        name.Replace("_rotmod", "", StringComparison.OrdinalIgnoreCase).Replace(" ", "", StringComparison.Ordinal).Trim();


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

}