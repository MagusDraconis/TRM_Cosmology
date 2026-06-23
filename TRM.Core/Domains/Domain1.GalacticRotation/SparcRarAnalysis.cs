using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using TRM.Core.Baryons;
using TRM.Core.Shared;

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


    // ✅ NEW: zentrale TRM-Integration
    private static double ComputeEffectiveZ(double distanceMpc, TrmCosmologyParameters scaling)
    {
        return distanceMpc * scaling.HT / PhysicalConstants.C_Kms;
    }

    private static double TransformRadiusToTrm(
        double rKpc,
        double galaxyDistanceMpc,
        TrmDistanceMapper mapper,
        TrmCosmologyParameters scaling)
    {
        double zEff = ComputeEffectiveZ(galaxyDistanceMpc, scaling);

        return mapper.ConvertGrDistanceToTrm(
            zEff,
            rKpc,
            DistanceMeasureKind.ComovingLike
        );
    }


    public static Dictionary<string, (double DistanceMpc, double InclinationDeg, int Quality)>
        LoadGalaxyMetaFromMrt(string mrtPath)
    {
        var meta = new Dictionary<string, (double DistanceMpc, double InclinationDeg, int Quality)>(
            StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(mrtPath))
            throw new FileNotFoundException($"SPARC catalog not found: {mrtPath}");

        foreach (var line in File.ReadLines(mrtPath))
        {
            string trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            // Header / Notes / Trenner überspringen
            if (trimmed.StartsWith("Title:") ||
                trimmed.StartsWith("Authors:") ||
                trimmed.StartsWith("Table:") ||
                trimmed.StartsWith("Byte-by-byte") ||
                trimmed.StartsWith("Note") ||
                trimmed.StartsWith("---") ||
                trimmed.StartsWith("==="))
                continue;

            var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            // Echte Datenzeilen müssen genug Spalten haben
            if (parts.Length < 18)
                continue;

            string galaxyKey = NormalizeGalaxyKey(parts[0]);

            if (!double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double distanceMpc))
                continue;

            if (!double.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out double incDeg))
                continue;

            if (!int.TryParse(parts[17], NumberStyles.Integer, CultureInfo.InvariantCulture, out int quality))
                continue;

            if (!meta.ContainsKey(galaxyKey))
            {
                meta.Add(galaxyKey, (distanceMpc, incDeg, quality));
            }
        }

        return meta;
    }

    private static string SafeSlice(string s, int start, int length)
    {
        if (start >= s.Length) return string.Empty;
        int maxLen = Math.Min(length, s.Length - start);
        return s.Substring(start, maxLen).Trim();
    }



    public static List<RarPoint> ApplyTrmDistanceMapping(
        List<RarPoint> rawPoints,
        Dictionary<string, (double DistanceMpc, double InclinationDeg, int Quality)> galaxyMeta,
        TrmCosmologyParameters scaling,
        BaryonMode baryonMode = BaryonMode.GR_Consistent,
        bool applyQualityFilter = false)

    {
        if (rawPoints == null || rawPoints.Count == 0)
            throw new ArgumentException("No raw RAR points provided.", nameof(rawPoints));

        var mapper = new TrmDistanceMapper(scaling);
        var transformed = new List<RarPoint>();

        var galaxyCache = rawPoints
            .GroupBy(p => NormalizeGalaxyKey(p.GalaxyName))
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(p => p.RadiusKpc).ToList()
            );

        foreach (var p in rawPoints)
        {
            string galaxyKey = NormalizeGalaxyKey(p.GalaxyName);

            double distanceMpc = 10.0;
            double incDeg = 60.0;
            int quality = 1;

            if (galaxyMeta.TryGetValue(galaxyKey, out var meta))
            {
                distanceMpc = meta.DistanceMpc;
                incDeg = meta.InclinationDeg;
                quality = meta.Quality;
            }

            if (applyQualityFilter && (incDeg < 30.0 || quality >= 3))
                continue;

            // Low-z effective redshift from distance
            double zEff = distanceMpc * scaling.HT / PhysicalConstants.C_Kms;

            double rTrm = mapper.ConvertGrDistanceToTrm(
                zEff,
                p.RadiusKpc,
                DistanceMeasureKind.ComovingLike
            );

            double gObsMs2 = ((p.Vobs * p.Vobs) / rTrm) * PhysicalConstants.Kms2KpcToMs2;

            double vDiskSq = p.Vdisk > 0 ? p.Vdisk * p.Vdisk : 0;
            double vBulgeSq = p.Vbulge > 0 ? p.Vbulge * p.Vbulge : 0;
            double vGasSq = p.Vgas > 0 ? p.Vgas * p.Vgas : 0;


            double vBarSq = vGasSq + (0.5 * vDiskSq) + (0.7 * vBulgeSq);
            if (vGasSq <= 0 && vDiskSq <= 0 && vBulgeSq <= 0)
                continue;




            double gBarMs2;

            switch (baryonMode)
            {
                case BaryonMode.GR_Consistent:
                    gBarMs2 = (vBarSq / p.RadiusKpc) * PhysicalConstants.Kms2KpcToMs2;
                    break;

                case BaryonMode.LegacyVelocityBased:
                    gBarMs2 = (vBarSq / rTrm) * PhysicalConstants.Kms2KpcToMs2;
                    break;

                case BaryonMode.Future_MassModel:

                    // 🔥 Gruppe nur einmal holen
                    var galaxyGroupA = galaxyCache[galaxyKey];

                    // ✅ echtes baryonisches Feld (CGS → m/s²)
                    double gBarCgs = BaryonicMassModel.ComputeGbarFromMassProfile(
                        galaxyGroupA,
                        p
                    );

                    gBarMs2 = gBarCgs / 100.0; // cm/s² → m/s² ✅
                    break;

                case BaryonMode.ExponentialDisk:

                    var galaxyGroup = rawPoints
                        .Where(x => NormalizeGalaxyKey(x.GalaxyName) == galaxyKey)
                        .OrderBy(x => x.RadiusKpc)
                        .ToList();

                    if (galaxyGroup.Count < 3)
                        continue;

                    // -------------------------
                    // 1) Geometrische Skalen
                    // -------------------------
                    double rMax = galaxyGroup.Max(pt => pt.RadiusKpc);

                    double diskScaleLength = EstimateDiskScaleLengthFromProfile(galaxyGroup);
                    double gasScaleLength = Math.Max(1.2, 1.8 * diskScaleLength);

                    // -------------------------
                    // 2) Komponentenmassen aus Profil-Normierung
                    //    (nicht direkt g = v²/r verwenden!)
                    // -------------------------
                    double diskMassSolar = EstimateComponentMassFromVelocityProfile(
                        galaxyGroup,
                        selector: pt => pt.Vdisk,
                        radialScaleKpc: diskScaleLength,
                        massScaleFactor: 1.0
                    );

                    double gasMassSolar = EstimateComponentMassFromVelocityProfile(
                        galaxyGroup,
                        selector: pt => pt.Vgas,
                        radialScaleKpc: gasScaleLength,
                        massScaleFactor: 0.85
                    );

                    double bulgeMassSolar = EstimateBulgeMassFromProfile(
                        galaxyGroup,
                        diskMassSolar
                    );

                    // Sanfte Begrenzungen für Robustheit
                    diskMassSolar = Math.Clamp(diskMassSolar, 1e8, 3e11);
                    gasMassSolar = Math.Clamp(gasMassSolar, 0.0, 2e11);
                    bulgeMassSolar = Math.Clamp(bulgeMassSolar, 0.0, 2e11);


                    // -------------------------
                    // 3) Feldbeiträge
                    // -------------------------

                    double gDisk = ThinDiskGravity.ComputeAcceleration(
                        p.RadiusKpc,
                        diskMassSolar,
                        diskScaleLength
                    );

                    double gGas = ThinDiskGravity.ComputeAcceleration(
                        p.RadiusKpc,
                        gasMassSolar,
                        gasScaleLength
                    );

                    double gBulge = ComputeSoftenedBulgeAcceleration(
                        bulgeMassSolar,
                        p.RadiusKpc,
                        softeningKpc: Math.Max(0.25, 0.20 * diskScaleLength)
                    );

                    // Bisherige Basis
                    double gBase = gDisk + 0.5 * gGas + 0.3 * gBulge;

                    // -------------------------
                    // 4) Experimenteller SPARC-dynamicFactor
                    // -------------------------
                    double dynamicFactor = ComputeSparcDynamicFactor(
                        galaxyGroup,
                        p.RadiusKpc,
                        diskScaleLength
                    );

                    // alpha: Stärke der Reduktion außen
                    double alpha = 0.35;

                    // Außen / gasreich -> sanfte Dämpfung
                    double correction = 1.0 - alpha * dynamicFactor;

                    // Sicherheitsbegrenzung
                    correction = Math.Clamp(correction, 0.65, 1.0);

                    // -------------------------
                    // 5) Gesamt
                    // -------------------------
                    gBarMs2 = gBase * correction;




                    break;



                default:
                    throw new NotImplementedException("Baryon mode not implemented yet.");
            }



            if (gObsMs2 > 0 && gBarMs2 > 0 && (gObsMs2 / gBarMs2 > 0.01))
            {
                transformed.Add(new RarPoint(
                    p.GalaxyName,
                    rTrm,
                    p.Vobs,
                    p.Vgas,
                    p.Vdisk,
                    p.Vbulge,
                    gObsMs2,
                    gBarMs2
                ));
            }
        }

        return transformed;
    }
    


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
            name.Replace("_rotmod", "", StringComparison.OrdinalIgnoreCase)
                .Replace(" ", "", StringComparison.Ordinal)
                .Trim();


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

    public static double EstimateDiskScaleLengthFromProfile(List<RarPoint> galaxyGroup)
    {
        if (galaxyGroup == null || galaxyGroup.Count == 0)
            return 3.0;

        double rMax = galaxyGroup.Max(p => p.RadiusKpc);

        // Solide Default-Näherung:
        // Rotationsdaten reichen typischerweise mehrere Skalenlängen hinaus
        double rd = rMax / 4.5;

        return Math.Clamp(rd, 0.6, 8.0);
    }
    private static double EstimateComponentMassFromVelocityProfile(
    List<RarPoint> galaxyGroup,
    Func<RarPoint, double> selector,
    double radialScaleKpc,
    double massScaleFactor = 1.0)
    {
        if (galaxyGroup == null || galaxyGroup.Count == 0 || radialScaleKpc <= 0)
            return 0.0;

        // Nur positive Profilwerte verwenden
        var values = galaxyGroup
            .Select(selector)
            .Where(v => v > 1.0)
            .ToList();

        if (values.Count == 0)
            return 0.0;

        // Robuste Profilhöhe: RMS statt max
        double vRms = Math.Sqrt(values.Average(v => v * v));

        // Charakteristischer Radius ~ einige Skalenlängen
        double rEffKpc = Math.Max(radialScaleKpc, 2.0 * radialScaleKpc);

        double vCgs = vRms * PhysicalConstants.KmsToCmS;
        double rCm = rEffKpc * PhysicalConstants.KpcToCm;

        // Nur als Massen-Normierung, nicht als lokale Beschleunigung
        double massGram = massScaleFactor * (vCgs * vCgs * rCm / PhysicalConstants.G);

        return massGram / PhysicalConstants.M_Solar;
    }
    private static double EstimateBulgeMassFromProfile(
    List<RarPoint> galaxyGroup,
    double diskMassSolar)
    {
        if (galaxyGroup == null || galaxyGroup.Count == 0)
            return 0.0;

        var bulgeValues = galaxyGroup
            .Select(p => p.Vbulge)
            .Where(v => v > 1.0)
            .ToList();

        if (bulgeValues.Count == 0)
            return 0.0;

        double vBulgeRms = Math.Sqrt(bulgeValues.Average(v => v * v));

        // Kleine effektive Bulge-Skala
        double rEffKpc = 0.8;

        double vCgs = vBulgeRms * PhysicalConstants.KmsToCmS;
        double rCm = rEffKpc * PhysicalConstants.KpcToCm;

        double bulgeMassGram = 0.8 * (vCgs * vCgs * rCm / PhysicalConstants.G);
        double bulgeMassSolar = bulgeMassGram / PhysicalConstants.M_Solar;

        // realistische Begrenzung relativ zur Disk
        return Math.Clamp(bulgeMassSolar, 0.0, 0.6 * Math.Max(diskMassSolar, 1e8));
    }
    private static double ComputeSoftenedBulgeAcceleration(
    double bulgeMassSolar,
    double rKpc,
    double softeningKpc = 0.5)
    {
        if (bulgeMassSolar <= 0 || rKpc <= 0)
            return 0.0;

        double mGram = bulgeMassSolar * PhysicalConstants.M_Solar;
        double rCm = rKpc * PhysicalConstants.KpcToCm;
        double epsCm = softeningKpc * PhysicalConstants.KpcToCm;

        double denom = Math.Pow(rCm * rCm + epsCm * epsCm, 1.5);
        double gCgs = PhysicalConstants.G * mGram * rCm / denom;

        return gCgs / 100.0; // cm/s² -> m/s²
    }
    private static double ComputeOuterRadiusFactor(double rKpc, double rdKpc)
    {
        if (rKpc <= 0 || rdKpc <= 0)
            return 0.0;

        double x = rKpc / rdKpc;

        // Kein Effekt innen, dann sanfter Anstieg
        if (x < 1.5) return 0.0;
        if (x > 5.0) return 1.0;

        return (x - 1.5) / (5.0 - 1.5);
    }
    private static double EstimateGasDominanceProxy(List<RarPoint> galaxyGroup)
    {
        if (galaxyGroup == null || galaxyGroup.Count == 0)
            return 0.0;

        double gasPower = galaxyGroup
            .Where(p => p.Vgas > 0)
            .Sum(p => p.Vgas * p.Vgas);

        double diskPower = galaxyGroup
            .Where(p => p.Vdisk > 0)
            .Sum(p => p.Vdisk * p.Vdisk);

        double bulgePower = galaxyGroup
            .Where(p => p.Vbulge > 0)
            .Sum(p => p.Vbulge * p.Vbulge);

        double total = gasPower + diskPower + bulgePower;
        if (total <= 0) return 0.0;

        double gasFraction = gasPower / total;

        return Math.Clamp(gasFraction, 0.0, 1.0);
    }
    private static double ComputeSparcDynamicFactor(
    List<RarPoint> galaxyGroup,
    double rKpc,
    double rdKpc)
    {
        double outerFactor = ComputeOuterRadiusFactor(rKpc, rdKpc);
        double gasProxy = EstimateGasDominanceProxy(galaxyGroup);

        // Basis:
        // - außen stärker
        // - gasreich = stärker
        double dynamicFactor = outerFactor * (0.5 + 0.5 * gasProxy);

        return Math.Clamp(dynamicFactor, 0.0, 1.0);
    }



    //private static double EstimateDiskMass(List<RarPoint> galaxy)
    //{
    //    double vMax = galaxy.Max(p => p.Vobs);

    //    double v = vMax * PhysicalConstants.KmsToCmS;

    //    //double M = Math.Pow(v, 4) /
    //    //           (PhysicalConstants.G * PhysicalConstants.A0_Cosmic);
    //    double M = 2e10; // Solar masses (konstant)
    //    return M / PhysicalConstants.M_Solar;
    //}

    //private static double EstimateScaleLength(List<RarPoint> galaxy)
    //{
    //    double rMax = galaxy.Max(p => p.RadiusKpc);

    //    return rMax / 3.0;
    //}
    //private static double EstimateGasMass(List<RarPoint> galaxy)
    //{
    //    double total = 0;

    //    foreach (var p in galaxy)
    //    {
    //        if (p.Vgas <= 0) continue;

    //        // v² r / G (CGS)
    //        double v = p.Vgas * PhysicalConstants.KmsToCmS;
    //        double r = p.RadiusKpc * PhysicalConstants.KpcToCm;

    //        double mass = v * v * r / PhysicalConstants.G;

    //        total += mass;
    //    }

    //    return total / PhysicalConstants.M_Solar;
    //}
    //private static double EstimateBulgeMass(List<RarPoint> galaxy)
    //{
    //    double total = 0;

    //    foreach (var p in galaxy)
    //    {
    //        if (p.Vbulge <= 0) continue;

    //        double v = p.Vbulge * PhysicalConstants.KmsToCmS;
    //        double r = p.RadiusKpc * PhysicalConstants.KpcToCm;

    //        double mass = v * v * r / PhysicalConstants.G;

    //        total += mass;
    //    }

    //    return total / PhysicalConstants.M_Solar;
    //}
    //private static double ComputeGasAcceleration(double gasMassSolar, double rKpc)
    //{
    //    double rCm = rKpc * PhysicalConstants.KpcToCm;
    //    double M = gasMassSolar * PhysicalConstants.M_Solar;

    //    double g = PhysicalConstants.G * M / (rCm * rCm);

    //    return g / 100.0;
    //}
    //private static double ComputeBulgeAcceleration(double bulgeMassSolar, double rKpc)
    //{
    //    double rCm = rKpc * PhysicalConstants.KpcToCm;
    //    double M = bulgeMassSolar * PhysicalConstants.M_Solar;

    //    double g = PhysicalConstants.G * M / (rCm * rCm);

    //    return g / 100.0;
    //}
    //private static double EstimateDiskMassFromSize(double rMaxKpc)
    //{
    //    // Nur geometrische Skalierung:
    //    // größere ausgedehnte Systeme -> größere Scheibenmasse
    //    // bewusst ohne velocity-Abhängigkeit

    //    double r = Math.Max(1.0, rMaxKpc);

    //    // Referenz: 10 kpc -> ~5e10 Msun
    //    double massSolar = 5.0e10 * Math.Pow(r / 10.0, 1.7);

    //    // Begrenzen, damit kleine/ große Systeme numerisch stabil bleiben
    //    return Math.Clamp(massSolar, 5.0e8, 3.0e11);
    //}
    //private static double EstimateDiskScaleLengthFromSize(double rMaxKpc)
    //{
    //    double r = Math.Max(1.0, rMaxKpc);

    //    // typische Näherung: sichtbare Rotationsdaten reichen bis mehrere Rd
    //    double rd = r / 4.5;

    //    return Math.Clamp(rd, 0.5, 8.0);
    //}
    //private static double EstimateGasScaleLengthFromDisk(double diskScaleLengthKpc)
    //{
    //    // Gas ist typischerweise ausgedehnter als die Sternscheibe
    //    double rg = 1.8 * diskScaleLengthKpc;

    //    return Math.Clamp(rg, 1.0, 15.0);
    //}
    //private static double EstimateGasMassFromSize(double rMaxKpc, double diskMassSolar)
    //{
    //    double r = Math.Max(1.0, rMaxKpc);

    //    // größere/ausgedehntere Systeme tendenziell gasreicher
    //    double gasFraction = 0.18 + 0.12 * Math.Tanh((r - 8.0) / 6.0);

    //    gasFraction = Math.Clamp(gasFraction, 0.08, 0.35);

    //    return gasFraction * diskMassSolar;
    //}
    //private static double EstimateBulgeMassFromStructure(List<RarPoint> galaxyGroup, double diskMassSolar)
    //{
    //    if (galaxyGroup == null || galaxyGroup.Count == 0)
    //        return 0.0;

    //    // Nur struktureller Indikator:
    //    // Gibt es überhaupt eine erkennbare Bulge-Komponente?
    //    bool hasBulge = galaxyGroup.Any(p => p.Vbulge > 1.0);

    //    if (!hasBulge)
    //        return 0.0;

    //    // Einfache moderate Bulge-Fraktion
    //    return 0.12 * diskMassSolar;
    //}

}