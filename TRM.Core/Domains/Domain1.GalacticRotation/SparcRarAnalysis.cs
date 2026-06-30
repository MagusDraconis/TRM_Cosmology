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

public enum TurningMemoryCorrectionMode
{
    None,
    BinBased,
    Interpolated
}

public enum TurningMemoryGateVariable
{
    MeanLogGbar,
    MeanAbsDlnGbarDr,
    MeanAbsDlnGbarDrOverRadiusSpan,
    MeanAbsDlnGbarDrOverPointCount,
    ProfileSharpness
}

public sealed record TurningMemoryCorrectionOptions(
    TurningMemoryCorrectionMode Mode = TurningMemoryCorrectionMode.None,
    int HoldoutModulo = 5,
    int HoldoutRemainder = 0,
    int BinCount = 3);

public sealed record TurningMemoryGalaxyImprovement(
    string GalaxyKey,
    int PointCount,
    double BaselineRms,
    double CorrectedRms,
    double DeltaRms);

public sealed record TurningMemoryPointDiagnostic(
    string GalaxyKey,
    double RadiusKpc,
    double Omega,
    double DLogGbarDr,
    double SignedTurningProxy,
    double MeanLogGbar,
    double BaselineResidual,
    double CorrectedResidual,
    double GateWeight,
    double TurningCorrection);

public sealed record TurningMemoryCorrectionMetrics(
    TurningMemoryCorrectionMode Mode,
    int TrainPointCount,
    int HoldoutPointCount,
    double BaselineRms,
    double CorrectedRms,
    double DeltaRms,
    double FittedGateThreshold,
    double FittedGateWidth,
    IReadOnlyList<TurningMemoryGalaxyImprovement> PerGalaxyImprovement,
    IReadOnlyList<TurningMemoryPointDiagnostic> HoldoutPoints);

public sealed record TurningMemoryDiagnosticSummary(
    string Label,
    double BaselineRmsAll,
    double CorrectedRmsAll,
    double DeltaRmsAll,
    double BaselineRmsHsb,
    double CorrectedRmsHsb,
    double DeltaRmsHsb,
    double BaselineRmsLsb,
    double CorrectedRmsLsb,
    double DeltaRmsLsb,
    int ImprovedGalaxyCount,
    int WorsenedGalaxyCount,
    double GateThreshold,
    double GateWidth,
    TurningMemoryGateVariable? GateVariable,
    IReadOnlyList<TurningMemoryGalaxyImprovement> TopImproved,
    IReadOnlyList<TurningMemoryGalaxyImprovement> TopWorsened);

public sealed record TurningMemoryGateBestResult(
    TurningMemoryGateVariable GateVariable,
    double GateThreshold,
    double GateWidth,
    double DeltaRmsAll);

public sealed record TurningMemoryGateComparisonReport(
    TurningMemoryCorrectionMode Mode,
    TurningMemoryDiagnosticSummary Baseline,
    TurningMemoryDiagnosticSummary Ungated,
    TurningMemoryDiagnosticSummary HsbSoftGated,
    IReadOnlyList<TurningMemoryDiagnosticSummary> GradientSoftGated,
    TurningMemoryGateBestResult BestGradientGate);

public sealed record DiskEdgeSurfaceProxyCorrelation(
    string ProxyName,
    double ResidualPearson,
    double ResidualSpearman,
    double TurningDeltaPearson,
    double TurningDeltaSpearman);

public sealed record OuterInnerTaktProxyCorrelation(
    string ProxyName,
    double ResidualPearson,
    double ResidualSpearman,
    double TurningDeltaPearson,
    double TurningDeltaSpearman);

public sealed record DiskEdgeSurfaceCouplingReport(
    TurningMemoryCorrectionMode Mode,
    int TrainGalaxyCount,
    int HoldoutGalaxyCount,
    double BaselineRmsAll,
    double UngatedRmsAll,
    double UngatedDeltaRmsAll,
    IReadOnlyList<DiskEdgeSurfaceProxyCorrelation> ProxyCorrelations,
    string BestProxyName,
    double BestProxyThreshold,
    double BestProxyWidth,
    double BestProxyCorrectedRmsAll,
    double BestProxyDeltaRmsAll,
    int BestProxyImprovedGalaxyCount,
    int BestProxyWorsenedGalaxyCount,
    IReadOnlyList<TurningMemoryGalaxyImprovement> TopImproved,
    IReadOnlyList<TurningMemoryGalaxyImprovement> TopWorsened);

public sealed record OuterInnerTaktSynchronizationReport(
    TurningMemoryCorrectionMode Mode,
    int TrainGalaxyCount,
    int HoldoutGalaxyCount,
    double BaselineRmsAll,
    double UngatedRmsAll,
    double UngatedDeltaRmsAll,
    IReadOnlyList<OuterInnerTaktProxyCorrelation> ProxyCorrelations,
    string BestProxyName,
    double BestProxyThreshold,
    double BestProxyWidth,
    double BestProxyCorrectedRmsAll,
    double BestProxyDeltaRmsAll,
    int BestProxyImprovedGalaxyCount,
    int BestProxyWorsenedGalaxyCount,
    IReadOnlyList<TurningMemoryGalaxyImprovement> TopImproved,
    IReadOnlyList<TurningMemoryGalaxyImprovement> TopWorsened);

public sealed record DiskCoherenceProxyCorrelation(
    string ProxyName,
    double ResidualPearson,
    double ResidualSpearman,
    double TurningDeltaPearson,
    double TurningDeltaSpearman);

public sealed record GlobalDiskCoherenceReport(
    TurningMemoryCorrectionMode Mode,
    int TrainGalaxyCount,
    int HoldoutGalaxyCount,
    double BaselineRmsAll,
    double UngatedRmsAll,
    double UngatedDeltaRmsAll,
    IReadOnlyList<DiskCoherenceProxyCorrelation> ProxyCorrelations,
    string BestProxyName,
    double BestProxyThreshold,
    double BestProxyWidth,
    double BestProxyCorrectedRmsAll,
    double BestProxyDeltaRmsAll,
    int BestProxyImprovedGalaxyCount,
    int BestProxyWorsenedGalaxyCount,
    IReadOnlyList<TurningMemoryGalaxyImprovement> TopImproved,
    IReadOnlyList<TurningMemoryGalaxyImprovement> TopWorsened);

public sealed record GeometryVariantProxyCorrelation(
    string VariantName,
    double OuterInnerRatioPearson,
    double OuterInnerRatioSpearman,
    double GasDominancePearson,
    double GasDominanceSpearman,
    double RadialSpanPearson,
    double RadialSpanSpearman,
    double PointCountPearson,
    double PointCountSpearman,
    bool CorrelatesWithDiskStructure);

public sealed record WorstGalaxyGeometryVariationEntry(
    string GalaxyKey,
    double BaselineRms,
    double SingleCenterRms,
    double SingleCenterDelta,
    double DiskDistributedRms,
    double DiskDistributedDelta,
    double MultiCenterToyRms,
    double MultiCenterToyDelta,
    double SmoothDistributedFieldRms,
    double SmoothDistributedFieldDelta,
    double OuterInnerRatio,
    double GasDominance,
    double RadialSpanKpc,
    int PointCount,
    bool ImprovementCorrelatesWithDiskStructure);

public sealed record WorstGalaxyGeometryVariationReport(
    double FixedA0,
    int TopGalaxyCount,
    string BestSmoothKernelKind,
    double BestSmoothKernelWidthKpc,
    int SmoothBeatsSingleCount,
    int SmoothBeatsToyCount,
    double MeanSmoothDeltaVsSingle,
    double MeanSmoothDeltaVsToy,
    IReadOnlyList<WorstGalaxyGeometryVariationEntry> Galaxies,
    IReadOnlyList<GeometryVariantProxyCorrelation> VariantCorrelations,
    string BestVariantName);

public sealed record PhysicalDiskStructureProxyCorrelation(
    string ProxyName,
    double ResidualPearson,
    double ResidualSpearman,
    double TurningDeltaPearson,
    double TurningDeltaSpearman,
    double MeanHsb,
    double MeanLsb,
    double HsbMinusLsb);

public sealed record PhysicalDiskStructureCouplingReport(
    TurningMemoryCorrectionMode Mode,
    int TrainGalaxyCount,
    int HoldoutGalaxyCount,
    double BaselineRmsAll,
    double UngatedRmsAll,
    double UngatedDeltaRmsAll,
    double BaselineRmsHsb,
    double UngatedRmsHsb,
    double BaselineRmsLsb,
    double UngatedRmsLsb,
    IReadOnlyList<PhysicalDiskStructureProxyCorrelation> ProxyCorrelations,
    string BestProxyName,
    double BestProxyThreshold,
    double BestProxyWidth,
    double BestProxyCorrectedRmsAll,
    double BestProxyDeltaRmsAll,
    double BestProxyCorrectedRmsHsb,
    double BestProxyCorrectedRmsLsb,
    int BestProxyImprovedGalaxyCount,
    int BestProxyWorsenedGalaxyCount,
    IReadOnlyList<TurningMemoryGalaxyImprovement> TopImproved,
    IReadOnlyList<TurningMemoryGalaxyImprovement> TopWorsened);

public sealed record OuterInnerContrastGateReport(
    TurningMemoryCorrectionMode Mode,
    TurningMemoryDiagnosticSummary Baseline,
    TurningMemoryDiagnosticSummary Ungated,
    TurningMemoryDiagnosticSummary OuterToInnerRatioGate,
    TurningMemoryDiagnosticSummary InverseOuterToInnerRatioGate,
    TurningMemoryDiagnosticSummary LogOuterToInnerRatioGate,
    string BestVariantLabel);


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

            //double rTrm = mapper.ConvertGrDistanceToTrm(
            //    zEff,
            //    p.RadiusKpc,
            //    DistanceMeasureKind.ComovingLike
            //);
            double rTrm = mapper.ConvertLocalRadiusToTrm(
                zEff,
                p.RadiusKpc,
                distanceMpc, // globale Referenzdistanz der Galaxie
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

                if (p.GalaxyName == "UGC02953")
                {
                    Console.WriteLine(
                        $"{p.GalaxyName}  raw r={p.RadiusKpc:F6}  ->  rTrm={rTrm:F6}  zEff={zEff:F6}");
                }

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
                    string? line;
                    while ((line = reader.ReadLine()) is not null)
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
                    string? dataLine;
                    while ((dataLine = reader.ReadLine()) is not null)
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

    public static TurningMemoryCorrectionMetrics EvaluateTurningMemoryCorrectionNoRefit(
        List<RarPoint> points,
        double a0,
        TurningMemoryCorrectionOptions? options = null)
    {
        if (points == null || points.Count == 0)
            throw new ArgumentException("No RAR points were provided.", nameof(points));
        if (a0 <= 0 || !double.IsFinite(a0))
            throw new ArgumentOutOfRangeException(nameof(a0), "a0 must be positive and finite.");

        options ??= new TurningMemoryCorrectionOptions();

        if (options.BinCount < 2)
            throw new ArgumentOutOfRangeException(nameof(options), "BinCount must be >= 2.");
        if (options.HoldoutModulo < 2)
            throw new ArgumentOutOfRangeException(nameof(options), "HoldoutModulo must be >= 2.");
        if (options.HoldoutRemainder < 0 || options.HoldoutRemainder >= options.HoldoutModulo)
            throw new ArgumentOutOfRangeException(nameof(options), "HoldoutRemainder must be in [0, HoldoutModulo).");

        var rows = BuildTurningResidualRows(points, a0);
        if (rows.Count == 0)
            throw new InvalidOperationException("No valid turning-memory diagnostic points could be built.");

        var trainRows = rows
            .Where(r => !IsHoldoutGalaxy(r.GalaxyKey, options.HoldoutModulo, options.HoldoutRemainder))
            .ToList();

        var holdoutRows = rows
            .Where(r => IsHoldoutGalaxy(r.GalaxyKey, options.HoldoutModulo, options.HoldoutRemainder))
            .ToList();

        if (trainRows.Count == 0 || holdoutRows.Count == 0)
            throw new InvalidOperationException("Train/holdout split produced an empty subset.");

        var holdoutBaselineResiduals = holdoutRows.Select(r => r.Residual).ToList();
        double baselineRms = ComputeRmsFromResiduals(holdoutBaselineResiduals);

        if (options.Mode == TurningMemoryCorrectionMode.None)
        {
            var unchanged = holdoutRows
                .Select(r => new TurningMemoryPointDiagnostic(
                    r.GalaxyKey,
                    r.RadiusKpc,
                    r.Omega,
                    r.DLogGbarDr,
                    r.SignedTurningProxy,
                    r.MeanLogGbar,
                    r.Residual,
                    r.Residual,
                    0.0,
                    0.0))
                .ToList();

            var perGalaxyBaseline = BuildPerGalaxyImprovement(unchanged);

            return new TurningMemoryCorrectionMetrics(
                Mode: TurningMemoryCorrectionMode.None,
                TrainPointCount: trainRows.Count,
                HoldoutPointCount: holdoutRows.Count,
                BaselineRms: baselineRms,
                CorrectedRms: baselineRms,
                DeltaRms: 0.0,
                FittedGateThreshold: double.NaN,
                FittedGateWidth: double.NaN,
                PerGalaxyImprovement: perGalaxyBaseline,
                HoldoutPoints: unchanged);
        }

        var correctionModel = BuildTurningCorrectionModel(trainRows, options.BinCount, options.Mode);

        var (bestThreshold, bestWidth) = FitSoftGateOnTrain(
            trainRows,
            correctionModel.GetCorrection);

        var holdoutDiagnostics = holdoutRows
            .Select(r =>
            {
                double correction = correctionModel.GetCorrection(r.SignedTurningProxy);
                double weight = Sigmoid((r.MeanLogGbar - bestThreshold) / bestWidth);
                double correctedResidual = r.Residual - (weight * correction);

                return new TurningMemoryPointDiagnostic(
                    r.GalaxyKey,
                    r.RadiusKpc,
                    r.Omega,
                    r.DLogGbarDr,
                    r.SignedTurningProxy,
                    r.MeanLogGbar,
                    r.Residual,
                    correctedResidual,
                    weight,
                    correction);
            })
            .ToList();

        double correctedRms = ComputeRmsFromResiduals(holdoutDiagnostics.Select(x => x.CorrectedResidual).ToList());
        var perGalaxy = BuildPerGalaxyImprovement(holdoutDiagnostics);

        return new TurningMemoryCorrectionMetrics(
            Mode: options.Mode,
            TrainPointCount: trainRows.Count,
            HoldoutPointCount: holdoutRows.Count,
            BaselineRms: baselineRms,
            CorrectedRms: correctedRms,
            DeltaRms: baselineRms - correctedRms,
            FittedGateThreshold: bestThreshold,
            FittedGateWidth: bestWidth,
            PerGalaxyImprovement: perGalaxy,
            HoldoutPoints: holdoutDiagnostics);
    }

    private sealed record TurningResidualRow(
        string GalaxyKey,
        double RadiusKpc,
        double GbarMs2,
        double Omega,
        double DLogGbarDr,
        double SignedTurningProxy,
        double MeanLogGbar,
        double Residual);

    private sealed record TurningCorrectionModel(
        Func<double, double> GetCorrection);

    private sealed record TurningGalaxyStats(
        double MeanLogGbar,
        double MeanAbsDlnGbarDr,
        double OuterEdgeGradient,
        double InnerGradient,
        double EdgeToInnerGradientRatio,
        double RadiusSpanKpc,
        int PointCount,
        double SurfaceProfileSharpness);

    private sealed record OuterInnerTaktStats(
        double GInner,
        double GOuter,
        double OuterInnerRatio,
        double OuterGradient,
        double OmegaOuter,
        double SyncProxy,
        double SyncGradientProxy,
        double SyncContrastProxy);

    private sealed record DiskCoherenceStats(
        double ProfileSmoothness,
        double DlnGbarDrVariance,
        double InnerToOuterCoherenceRatio,
        double ShearProxy,
        double OuterToInnerRatioTimesProfileSmoothness,
        double OuterToInnerRatioTimesShearProxy);

    private sealed record GeometryVariantEvaluation(
        string GalaxyKey,
        double BaselineRms,
        double SingleCenterRms,
        double DiskDistributedRms,
        double MultiCenterToyRms,
        double SmoothDistributedFieldRms,
        double OuterInnerRatio,
        double GasDominance,
        double RadialSpanKpc,
        int PointCount);

    private sealed record PhysicalDiskStructureStats(
        double OuterBaryonicMassFraction,
        double GasDominanceProxy,
        double DiskToBulgeProxy,
        double OuterToInnerBaryonicAccelerationRatio,
        double TransitionRadiusKpc,
        double TransitionSharpness);

    private static List<TurningResidualRow> BuildTurningResidualRows(List<RarPoint> points, double a0)
    {
        var rows = new List<TurningResidualRow>();

        foreach (var galaxyGroup in points.GroupBy(p => NormalizeGalaxyKey(p.GalaxyName)))
        {
            var ordered = galaxyGroup.OrderBy(p => p.RadiusKpc).ToList();
            if (ordered.Count < 3)
                continue;

            var logGbarValues = ordered
                .Where(p => p.GbarMs2 > 0 && double.IsFinite(p.GbarMs2))
                .Select(p => Math.Log10(p.GbarMs2))
                .ToList();

            if (logGbarValues.Count == 0)
                continue;

            double meanLogGbar = logGbarValues.Average();

            for (int i = 0; i < ordered.Count; i++)
            {
                var p = ordered[i];
                if (p.RadiusKpc <= 0 || p.Vobs <= 0 || p.GobsMs2 <= 0 || p.GbarMs2 <= 0)
                    continue;

                double gPred = PredictGobs(p.GbarMs2, a0, ModelType.ClockworkTRM);
                if (gPred <= 0 || !double.IsFinite(gPred))
                    continue;

                int leftIndex = i == 0 ? 0 : i - 1;
                int rightIndex = i == ordered.Count - 1 ? ordered.Count - 1 : i + 1;
                if (leftIndex == rightIndex)
                    continue;

                var pLeft = ordered[leftIndex];
                var pRight = ordered[rightIndex];

                if (pLeft.GbarMs2 <= 0 || pRight.GbarMs2 <= 0)
                    continue;

                double dr = pRight.RadiusKpc - pLeft.RadiusKpc;
                if (dr <= 0)
                    continue;

                double omega = (p.Vobs * 1000.0) / (p.RadiusKpc * PhysicalConstants.KpcToM);
                double dLogGbarDr = (Math.Log(pRight.GbarMs2) - Math.Log(pLeft.GbarMs2)) / dr;
                double signedTurningProxy = omega * dLogGbarDr;
                double residual = Math.Log10(p.GobsMs2) - Math.Log10(gPred);

                rows.Add(new TurningResidualRow(
                    galaxyGroup.Key,
                    p.RadiusKpc,
                    p.GbarMs2,
                    omega,
                    dLogGbarDr,
                    signedTurningProxy,
                    meanLogGbar,
                    residual));
            }
        }

        return rows;
    }

    private static TurningCorrectionModel BuildTurningCorrectionModel(
        List<TurningResidualRow> trainRows,
        int binCount,
        TurningMemoryCorrectionMode mode)
    {
        var sortedProxy = trainRows
            .Select(r => r.SignedTurningProxy)
            .OrderBy(x => x)
            .ToList();

        if (sortedProxy.Count < binCount * 10)
            throw new InvalidOperationException("Insufficient train rows for turning-memory binning.");

        var cuts = new List<double>();
        for (int i = 1; i < binCount; i++)
        {
            int index = (i * sortedProxy.Count) / binCount;
            cuts.Add(sortedProxy[Math.Min(index, sortedProxy.Count - 1)]);
        }

        int GetBin(double proxy)
        {
            for (int i = 0; i < cuts.Count; i++)
            {
                if (proxy < cuts[i])
                    return i;
            }

            return cuts.Count;
        }

        var grouped = trainRows
            .GroupBy(r => GetBin(r.SignedTurningProxy))
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    MeanResidual = g.Average(x => x.Residual),
                    MeanProxy = g.Average(x => x.SignedTurningProxy)
                });

        for (int bin = 0; bin < binCount; bin++)
        {
            if (!grouped.ContainsKey(bin))
                throw new InvalidOperationException("A turning-memory bin is empty in train data.");
        }

        if (mode == TurningMemoryCorrectionMode.BinBased)
        {
            return new TurningCorrectionModel(
                proxy =>
                {
                    int bin = GetBin(proxy);
                    return grouped[bin].MeanResidual;
                });
        }

        var nodes = grouped
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => (Proxy: kvp.Value.MeanProxy, Correction: kvp.Value.MeanResidual))
            .ToList();

        return new TurningCorrectionModel(
            proxy =>
            {
                if (proxy <= nodes[0].Proxy)
                    return nodes[0].Correction;
                if (proxy >= nodes[^1].Proxy)
                    return nodes[^1].Correction;

                for (int i = 0; i < nodes.Count - 1; i++)
                {
                    var left = nodes[i];
                    var right = nodes[i + 1];
                    if (proxy < right.Proxy)
                    {
                        double dx = right.Proxy - left.Proxy;
                        if (dx <= 0)
                            return left.Correction;

                        double t = (proxy - left.Proxy) / dx;
                        return left.Correction + (t * (right.Correction - left.Correction));
                    }
                }

                return nodes[^1].Correction;
            });
    }

    /// <summary>
    /// Compares baseline, ungated, HSB-soft-gated, and gradient-soft-gated turning-memory corrections.
    ///
    /// Hypothesis:
    /// Residual turning-memory signal is regime-dependent and should be detected by train-fitted gates.
    ///
    /// Status:
    /// diagnostic (candidate screening only).
    ///
    /// Limitation:
    /// No main-path activation; baseline TRM-RAR remains unchanged.
    /// </summary>
    public static TurningMemoryGateComparisonReport EvaluateTurningMemoryGateComparisonNoRefit(
        List<RarPoint> points,
        double a0,
        TurningMemoryCorrectionMode mode,
        TurningMemoryCorrectionOptions? options = null)
    {
        if (mode == TurningMemoryCorrectionMode.None)
            throw new ArgumentOutOfRangeException(nameof(mode), "Use BinBased or Interpolated for gate comparison diagnostics.");

        if (points == null || points.Count == 0)
            throw new ArgumentException("No RAR points were provided.", nameof(points));
        if (a0 <= 0 || !double.IsFinite(a0))
            throw new ArgumentOutOfRangeException(nameof(a0), "a0 must be positive and finite.");

        options ??= new TurningMemoryCorrectionOptions(mode, 5, 0, 3);
        if (options.BinCount < 2)
            throw new ArgumentOutOfRangeException(nameof(options), "BinCount must be >= 2.");

        // Build the residual/proxy table from the fixed-a0 baseline model.
        var rows = BuildTurningResidualRows(points, a0);
        if (rows.Count == 0)
            throw new InvalidOperationException("No valid turning-memory diagnostic points could be built.");

        // Train/holdout separation is mandatory to avoid per-case overfitting in diagnostics.
        var trainRows = rows
            .Where(r => !IsHoldoutGalaxy(r.GalaxyKey, options.HoldoutModulo, options.HoldoutRemainder))
            .ToList();
        var holdoutRows = rows
            .Where(r => IsHoldoutGalaxy(r.GalaxyKey, options.HoldoutModulo, options.HoldoutRemainder))
            .ToList();

        if (trainRows.Count == 0 || holdoutRows.Count == 0)
            throw new InvalidOperationException("Train/holdout split produced an empty subset.");

        var trainStats = BuildGalaxyStats(trainRows);
        var holdoutStats = BuildGalaxyStats(holdoutRows);

        if (trainStats.Count < 5 || holdoutStats.Count == 0)
            throw new InvalidOperationException("Insufficient train/holdout galaxy stats for gate diagnostics.");

        double hsbThreshold = Percentile(trainStats.Values.Select(x => x.MeanLogGbar).OrderBy(x => x).ToList(), 0.50);

        // Fit corrections only on train rows; holdout is evaluation-only (no refit).
        var correctionModel = BuildTurningCorrectionModel(trainRows, options.BinCount, mode);

        var baselineDiagnostics = holdoutRows
            .Select(r => CreatePointDiagnostic(r, r.Residual, 0.0, 0.0))
            .ToList();

        var ungatedDiagnostics = holdoutRows
            .Select(r =>
            {
                double correction = correctionModel.GetCorrection(r.SignedTurningProxy);
                return CreatePointDiagnostic(r, r.Residual - correction, 1.0, correction);
            })
            .ToList();

        var hsbGateFit = FitSoftGateOnTrainByVariable(
            trainRows,
            trainStats,
            correctionModel.GetCorrection,
            TurningMemoryGateVariable.MeanLogGbar);

        var hsbDiagnostics = holdoutRows
            .Select(r =>
            {
                double gateValue = ResolveGateValue(holdoutStats[r.GalaxyKey], TurningMemoryGateVariable.MeanLogGbar);
                double correction = correctionModel.GetCorrection(r.SignedTurningProxy);
                double weight = Sigmoid((gateValue - hsbGateFit.Threshold) / hsbGateFit.Width);
                return CreatePointDiagnostic(r, r.Residual - (weight * correction), weight, correction);
            })
            .ToList();

        var gradientVariables = new[]
        {
            TurningMemoryGateVariable.MeanAbsDlnGbarDr,
            TurningMemoryGateVariable.MeanAbsDlnGbarDrOverRadiusSpan,
            TurningMemoryGateVariable.MeanAbsDlnGbarDrOverPointCount,
            TurningMemoryGateVariable.ProfileSharpness
        };

        var gradientSummaries = new List<TurningMemoryDiagnosticSummary>();
        foreach (var variable in gradientVariables)
        {
            var fit = FitSoftGateOnTrainByVariable(trainRows, trainStats, correctionModel.GetCorrection, variable);
            var corrected = holdoutRows
                .Select(r =>
                {
                    double gateValue = ResolveGateValue(holdoutStats[r.GalaxyKey], variable);
                    double correction = correctionModel.GetCorrection(r.SignedTurningProxy);
                    double weight = Sigmoid((gateValue - fit.Threshold) / fit.Width);
                    return CreatePointDiagnostic(r, r.Residual - (weight * correction), weight, correction);
                })
                .ToList();

            gradientSummaries.Add(BuildDiagnosticSummary(
                label: variable.ToString(),
                diagnostics: corrected,
                hsbThreshold: hsbThreshold,
                gateThreshold: fit.Threshold,
                gateWidth: fit.Width,
                gateVariable: variable));
        }

        var baselineSummary = BuildDiagnosticSummary(
            label: "Baseline",
            diagnostics: baselineDiagnostics,
            hsbThreshold: hsbThreshold,
            gateThreshold: double.NaN,
            gateWidth: double.NaN,
            gateVariable: null);

        var ungatedSummary = BuildDiagnosticSummary(
            label: "UngatedTurningMemory",
            diagnostics: ungatedDiagnostics,
            hsbThreshold: hsbThreshold,
            gateThreshold: double.NaN,
            gateWidth: double.NaN,
            gateVariable: null);

        var hsbSummary = BuildDiagnosticSummary(
            label: "HsbSoftGated",
            diagnostics: hsbDiagnostics,
            hsbThreshold: hsbThreshold,
            gateThreshold: hsbGateFit.Threshold,
            gateWidth: hsbGateFit.Width,
            gateVariable: TurningMemoryGateVariable.MeanLogGbar);

        var bestGradient = gradientSummaries
            .OrderByDescending(x => x.DeltaRmsAll)
            .First();

        return new TurningMemoryGateComparisonReport(
            Mode: mode,
            Baseline: baselineSummary,
            Ungated: ungatedSummary,
            HsbSoftGated: hsbSummary,
            GradientSoftGated: gradientSummaries,
            BestGradientGate: new TurningMemoryGateBestResult(
                GateVariable: bestGradient.GateVariable ?? TurningMemoryGateVariable.MeanAbsDlnGbarDr,
                GateThreshold: bestGradient.GateThreshold,
                GateWidth: bestGradient.GateWidth,
                DeltaRmsAll: bestGradient.DeltaRmsAll));
    }

    /// <summary>
    /// Evaluates disk edge/surface coupling proxies against residual RMS and turning-memory delta.
    ///
    /// Hypothesis:
    /// Residual behavior can be partially explained by edge-vs-inner profile structure.
    ///
    /// Status:
    /// diagnostic.
    ///
    /// Limitation:
    /// Correlation-guided proxy scan; not a causal proof.
    /// </summary>
    public static DiskEdgeSurfaceCouplingReport EvaluateDiskEdgeSurfaceCouplingNoRefit(
        List<RarPoint> points,
        double a0,
        TurningMemoryCorrectionMode mode,
        TurningMemoryCorrectionOptions? options = null)
    {
        if (mode == TurningMemoryCorrectionMode.None)
            throw new ArgumentOutOfRangeException(nameof(mode), "Use BinBased or Interpolated for disk-edge coupling diagnostics.");

        if (points == null || points.Count == 0)
            throw new ArgumentException("No RAR points were provided.", nameof(points));
        if (a0 <= 0 || !double.IsFinite(a0))
            throw new ArgumentOutOfRangeException(nameof(a0), "a0 must be positive and finite.");

        options ??= new TurningMemoryCorrectionOptions(mode, 5, 0, 3);

        var rows = BuildTurningResidualRows(points, a0);
        if (rows.Count == 0)
            throw new InvalidOperationException("No valid turning-memory diagnostic points could be built.");

        // No-refit discipline: separate train for fitting from holdout for scoring.
        var trainRows = rows
            .Where(r => !IsHoldoutGalaxy(r.GalaxyKey, options.HoldoutModulo, options.HoldoutRemainder))
            .ToList();
        var holdoutRows = rows
            .Where(r => IsHoldoutGalaxy(r.GalaxyKey, options.HoldoutModulo, options.HoldoutRemainder))
            .ToList();

        if (trainRows.Count == 0 || holdoutRows.Count == 0)
            throw new InvalidOperationException("Train/holdout split produced an empty subset.");

        var trainStats = BuildGalaxyStats(trainRows);
        var holdoutStats = BuildGalaxyStats(holdoutRows);

        // Fit turning correction on train only; keep a0 and correction form fixed on holdout.
        var correctionModel = BuildTurningCorrectionModel(trainRows, options.BinCount, mode);

        var baselineDiagnostics = holdoutRows
            .Select(r => CreatePointDiagnostic(r, r.Residual, 0.0, 0.0))
            .ToList();

        var ungatedDiagnostics = holdoutRows
            .Select(r =>
            {
                double correction = correctionModel.GetCorrection(r.SignedTurningProxy);
                return CreatePointDiagnostic(r, r.Residual - correction, 1.0, correction);
            })
            .ToList();

        var baselinePerGalaxy = BuildPerGalaxyImprovement(baselineDiagnostics)
            .ToDictionary(x => x.GalaxyKey, x => x.BaselineRms);

        var ungatedPerGalaxy = BuildPerGalaxyImprovement(ungatedDiagnostics);
        var ungatedDeltaByGalaxy = ungatedPerGalaxy
            .ToDictionary(x => x.GalaxyKey, x => x.DeltaRms);

        var proxyCorrelations = new List<DiskEdgeSurfaceProxyCorrelation>();

        var correlationRows = holdoutStats
            .Where(kvp => baselinePerGalaxy.ContainsKey(kvp.Key) && ungatedDeltaByGalaxy.ContainsKey(kvp.Key))
            .Select(kvp => new
            {
                Stats = kvp.Value,
                ResidualRms = baselinePerGalaxy[kvp.Key],
                TurningDelta = ungatedDeltaByGalaxy[kvp.Key]
            })
            .ToList();

        void AddCorrelation(
            string name,
            Func<TurningGalaxyStats, double> selector)
        {
            var x = correlationRows.Select(r => selector(r.Stats)).ToList();
            var yResidual = correlationRows.Select(r => r.ResidualRms).ToList();
            var yTurning = correlationRows.Select(r => r.TurningDelta).ToList();

            proxyCorrelations.Add(new DiskEdgeSurfaceProxyCorrelation(
                ProxyName: name,
                ResidualPearson: PearsonCorrelation(x, yResidual),
                ResidualSpearman: SpearmanCorrelation(x, yResidual),
                TurningDeltaPearson: PearsonCorrelation(x, yTurning),
                TurningDeltaSpearman: SpearmanCorrelation(x, yTurning)));
        }

        AddCorrelation("outerEdgeGradient", s => s.OuterEdgeGradient);
        AddCorrelation("innerGradient", s => s.InnerGradient);
        AddCorrelation("edgeToInnerGradientRatio", s => s.EdgeToInnerGradientRatio);
        AddCorrelation("radialSpanKpc", s => s.RadiusSpanKpc);
        AddCorrelation("pointCount", s => s.PointCount);
        AddCorrelation("surfaceProfileSharpness", s => s.SurfaceProfileSharpness);

        var proxyDefinitions = new (string Name, Func<TurningGalaxyStats, double> Selector)[]
        {
            ("outerEdgeGradient", s => s.OuterEdgeGradient),
            ("innerGradient", s => s.InnerGradient),
            ("edgeToInnerGradientRatio", s => s.EdgeToInnerGradientRatio),
            ("radialSpanKpc", s => s.RadiusSpanKpc),
            ("pointCount", s => s.PointCount),
            ("surfaceProfileSharpness", s => s.SurfaceProfileSharpness)
        };

        var proxyFits = proxyDefinitions
            .Select(p => new
            {
                p.Name,
                Fit = FitSoftGateOnTrainBySelector(trainRows, trainStats, correctionModel.GetCorrection, p.Selector),
                Selector = p.Selector
            })
            .OrderBy(x => x.Fit.TrainRms)
            .ToList();

        var best = proxyFits.First();
        var bestDiagnostics = holdoutRows
            .Select(r =>
            {
                double gateValue = best.Selector(holdoutStats[r.GalaxyKey]);
                double correction = correctionModel.GetCorrection(r.SignedTurningProxy);
                double weight = Sigmoid((gateValue - best.Fit.Threshold) / best.Fit.Width);
                return CreatePointDiagnostic(r, r.Residual - (weight * correction), weight, correction);
            })
            .ToList();

        double baselineRmsAll = ComputeRmsFromResiduals(baselineDiagnostics.Select(x => x.BaselineResidual).ToList());
        double ungatedRmsAll = ComputeRmsFromResiduals(ungatedDiagnostics.Select(x => x.CorrectedResidual).ToList());
        double bestRmsAll = ComputeRmsFromResiduals(bestDiagnostics.Select(x => x.CorrectedResidual).ToList());

        var bestPerGalaxy = BuildPerGalaxyImprovement(bestDiagnostics);
        int improved = bestPerGalaxy.Count(x => x.DeltaRms > 0);
        int worsened = bestPerGalaxy.Count(x => x.DeltaRms < 0);

        return new DiskEdgeSurfaceCouplingReport(
            Mode: mode,
            TrainGalaxyCount: trainStats.Count,
            HoldoutGalaxyCount: holdoutStats.Count,
            BaselineRmsAll: baselineRmsAll,
            UngatedRmsAll: ungatedRmsAll,
            UngatedDeltaRmsAll: baselineRmsAll - ungatedRmsAll,
            ProxyCorrelations: proxyCorrelations,
            BestProxyName: best.Name,
            BestProxyThreshold: best.Fit.Threshold,
            BestProxyWidth: best.Fit.Width,
            BestProxyCorrectedRmsAll: bestRmsAll,
            BestProxyDeltaRmsAll: baselineRmsAll - bestRmsAll,
            BestProxyImprovedGalaxyCount: improved,
            BestProxyWorsenedGalaxyCount: worsened,
            TopImproved: bestPerGalaxy.Where(x => x.DeltaRms > 0).Take(10).ToList(),
            TopWorsened: bestPerGalaxy.Where(x => x.DeltaRms < 0).OrderBy(x => x.DeltaRms).Take(10).ToList());
    }

    /// <summary>
    /// Tests outer-inner takt synchronization proxies against residuals and turning-memory improvements.
    ///
    /// Hypothesis:
    /// A synchronization term from outer rotation and inner/outer baryonic contrast carries residual signal.
    ///
    /// Status:
    /// diagnostic + candidate.
    ///
    /// Limitation:
    /// Proxy-level effective model, not a full microscopic derivation.
    /// </summary>
    public static OuterInnerTaktSynchronizationReport EvaluateOuterInnerTaktSynchronizationNoRefit(
        List<RarPoint> points,
        double a0,
        TurningMemoryCorrectionMode mode,
        TurningMemoryCorrectionOptions? options = null)
    {
        if (mode == TurningMemoryCorrectionMode.None)
            throw new ArgumentOutOfRangeException(nameof(mode), "Use BinBased or Interpolated for outer-inner takt synchronization diagnostics.");

        if (points == null || points.Count == 0)
            throw new ArgumentException("No RAR points were provided.", nameof(points));
        if (a0 <= 0 || !double.IsFinite(a0))
            throw new ArgumentOutOfRangeException(nameof(a0), "a0 must be positive and finite.");

        options ??= new TurningMemoryCorrectionOptions(mode, 5, 0, 3);

        var rows = BuildTurningResidualRows(points, a0);
        if (rows.Count == 0)
            throw new InvalidOperationException("No valid turning-memory diagnostic points could be built.");

        // Holdout-only validation after train-only gate fitting.
        var trainRows = rows
            .Where(r => !IsHoldoutGalaxy(r.GalaxyKey, options.HoldoutModulo, options.HoldoutRemainder))
            .ToList();
        var holdoutRows = rows
            .Where(r => IsHoldoutGalaxy(r.GalaxyKey, options.HoldoutModulo, options.HoldoutRemainder))
            .ToList();

        if (trainRows.Count == 0 || holdoutRows.Count == 0)
            throw new InvalidOperationException("Train/holdout split produced an empty subset.");

        var trainStats = BuildOuterInnerTaktStats(trainRows);
        var holdoutStats = BuildOuterInnerTaktStats(holdoutRows);

        var correctionModel = BuildTurningCorrectionModel(trainRows, options.BinCount, mode);

        var baselineDiagnostics = holdoutRows
            .Select(r => CreatePointDiagnostic(r, r.Residual, 0.0, 0.0))
            .ToList();

        var ungatedDiagnostics = holdoutRows
            .Select(r =>
            {
                double correction = correctionModel.GetCorrection(r.SignedTurningProxy);
                return CreatePointDiagnostic(r, r.Residual - correction, 1.0, correction);
            })
            .ToList();

        var baselinePerGalaxy = BuildPerGalaxyImprovement(baselineDiagnostics)
            .ToDictionary(x => x.GalaxyKey, x => x.BaselineRms);

        var ungatedPerGalaxy = BuildPerGalaxyImprovement(ungatedDiagnostics)
            .ToDictionary(x => x.GalaxyKey, x => x.DeltaRms);

        var proxyCorrelations = new List<OuterInnerTaktProxyCorrelation>();
        var correlationRows = holdoutStats
            .Where(kvp => baselinePerGalaxy.ContainsKey(kvp.Key) && ungatedPerGalaxy.ContainsKey(kvp.Key))
            .Select(kvp => new
            {
                Stats = kvp.Value,
                ResidualRms = baselinePerGalaxy[kvp.Key],
                TurningDelta = ungatedPerGalaxy[kvp.Key]
            })
            .ToList();

        void AddCorrelation(string name, Func<OuterInnerTaktStats, double> selector)
        {
            var x = correlationRows.Select(r => selector(r.Stats)).ToList();
            var yResidual = correlationRows.Select(r => r.ResidualRms).ToList();
            var yTurning = correlationRows.Select(r => r.TurningDelta).ToList();

            proxyCorrelations.Add(new OuterInnerTaktProxyCorrelation(
                ProxyName: name,
                ResidualPearson: PearsonCorrelation(x, yResidual),
                ResidualSpearman: SpearmanCorrelation(x, yResidual),
                TurningDeltaPearson: PearsonCorrelation(x, yTurning),
                TurningDeltaSpearman: SpearmanCorrelation(x, yTurning)));
        }

        AddCorrelation("syncProxy", s => s.SyncProxy);
        AddCorrelation("syncGradientProxy", s => s.SyncGradientProxy);
        AddCorrelation("syncContrastProxy", s => s.SyncContrastProxy);

        var proxyDefinitions = new (string Name, Func<OuterInnerTaktStats, double> Selector)[]
        {
            ("syncProxy", s => s.SyncProxy),
            ("syncGradientProxy", s => s.SyncGradientProxy),
            ("syncContrastProxy", s => s.SyncContrastProxy)
        };

        // Select the best proxy on train RMS only; then freeze and evaluate on holdout.
        var proxyFits = proxyDefinitions
            .Select(p => new
            {
                p.Name,
                p.Selector,
                Fit = FitSoftGateOnTrainByOuterInnerTaktSelector(trainRows, trainStats, correctionModel.GetCorrection, p.Selector)
            })
            .OrderBy(x => x.Fit.TrainRms)
            .ToList();

        if (proxyFits.Count == 0)
            throw new InvalidOperationException("No outer-inner takt proxy fit could be evaluated.");

        var best = proxyFits.First();
        var bestDiagnostics = holdoutRows
            .Select(r =>
            {
                if (!holdoutStats.TryGetValue(r.GalaxyKey, out var stats))
                    return CreatePointDiagnostic(r, r.Residual, 0.0, 0.0);

                double gateValue = best.Selector(stats);
                double correction = correctionModel.GetCorrection(r.SignedTurningProxy);
                double weight = Sigmoid((gateValue - best.Fit.Threshold) / best.Fit.Width);
                return CreatePointDiagnostic(r, r.Residual - (weight * correction), weight, correction);
            })
            .ToList();

        double baselineRmsAll = ComputeRmsFromResiduals(baselineDiagnostics.Select(x => x.BaselineResidual).ToList());
        double ungatedRmsAll = ComputeRmsFromResiduals(ungatedDiagnostics.Select(x => x.CorrectedResidual).ToList());
        double bestRmsAll = ComputeRmsFromResiduals(bestDiagnostics.Select(x => x.CorrectedResidual).ToList());

        var bestPerGalaxy = BuildPerGalaxyImprovement(bestDiagnostics);
        int improved = bestPerGalaxy.Count(x => x.DeltaRms > 0);
        int worsened = bestPerGalaxy.Count(x => x.DeltaRms < 0);

        return new OuterInnerTaktSynchronizationReport(
            Mode: mode,
            TrainGalaxyCount: trainStats.Count,
            HoldoutGalaxyCount: holdoutStats.Count,
            BaselineRmsAll: baselineRmsAll,
            UngatedRmsAll: ungatedRmsAll,
            UngatedDeltaRmsAll: baselineRmsAll - ungatedRmsAll,
            ProxyCorrelations: proxyCorrelations,
            BestProxyName: best.Name,
            BestProxyThreshold: best.Fit.Threshold,
            BestProxyWidth: best.Fit.Width,
            BestProxyCorrectedRmsAll: bestRmsAll,
            BestProxyDeltaRmsAll: baselineRmsAll - bestRmsAll,
            BestProxyImprovedGalaxyCount: improved,
            BestProxyWorsenedGalaxyCount: worsened,
            TopImproved: bestPerGalaxy.Where(x => x.DeltaRms > 0).Take(10).ToList(),
            TopWorsened: bestPerGalaxy.Where(x => x.DeltaRms < 0).OrderBy(x => x.DeltaRms).Take(10).ToList());
    }

    /// <summary>
    /// Scans global disk-coherence proxies and applies a best-proxy soft-gated no-refit correction.
    ///
    /// Hypothesis:
    /// Global profile coherence/shear structure contributes to unresolved residual sectors.
    ///
    /// Status:
    /// diagnostic.
    ///
    /// Limitation:
    /// Exploratory proxy family; not part of the core TRM baseline.
    /// </summary>
    public static GlobalDiskCoherenceReport EvaluateGlobalDiskCoherenceNoRefit(
        List<RarPoint> points,
        double a0,
        TurningMemoryCorrectionMode mode,
        TurningMemoryCorrectionOptions? options = null)
    {
        if (mode == TurningMemoryCorrectionMode.None)
            throw new ArgumentOutOfRangeException(nameof(mode), "Use BinBased or Interpolated for global disk-coherence diagnostics.");

        if (points == null || points.Count == 0)
            throw new ArgumentException("No RAR points were provided.", nameof(points));
        if (a0 <= 0 || !double.IsFinite(a0))
            throw new ArgumentOutOfRangeException(nameof(a0), "a0 must be positive and finite.");

        options ??= new TurningMemoryCorrectionOptions(mode, 5, 0, 3);

        var rows = BuildTurningResidualRows(points, a0);
        if (rows.Count == 0)
            throw new InvalidOperationException("No valid turning-memory diagnostic points could be built.");

        // Maintain train/holdout split to keep no-refit generalization signal interpretable.
        var trainRows = rows
            .Where(r => !IsHoldoutGalaxy(r.GalaxyKey, options.HoldoutModulo, options.HoldoutRemainder))
            .ToList();
        var holdoutRows = rows
            .Where(r => IsHoldoutGalaxy(r.GalaxyKey, options.HoldoutModulo, options.HoldoutRemainder))
            .ToList();

        if (trainRows.Count == 0 || holdoutRows.Count == 0)
            throw new InvalidOperationException("Train/holdout split produced an empty subset.");

        var trainStats = BuildDiskCoherenceStats(trainRows);
        var holdoutStats = BuildDiskCoherenceStats(holdoutRows);

        var correctionModel = BuildTurningCorrectionModel(trainRows, options.BinCount, mode);

        var baselineDiagnostics = holdoutRows
            .Select(r => CreatePointDiagnostic(r, r.Residual, 0.0, 0.0))
            .ToList();

        var ungatedDiagnostics = holdoutRows
            .Select(r =>
            {
                double correction = correctionModel.GetCorrection(r.SignedTurningProxy);
                return CreatePointDiagnostic(r, r.Residual - correction, 1.0, correction);
            })
            .ToList();

        var baselinePerGalaxy = BuildPerGalaxyImprovement(baselineDiagnostics)
            .ToDictionary(x => x.GalaxyKey, x => x.BaselineRms);
        var ungatedPerGalaxy = BuildPerGalaxyImprovement(ungatedDiagnostics)
            .ToDictionary(x => x.GalaxyKey, x => x.DeltaRms);

        var proxyCorrelations = new List<DiskCoherenceProxyCorrelation>();
        var correlationRows = holdoutStats
            .Where(kvp => baselinePerGalaxy.ContainsKey(kvp.Key) && ungatedPerGalaxy.ContainsKey(kvp.Key))
            .Select(kvp => new
            {
                Stats = kvp.Value,
                ResidualRms = baselinePerGalaxy[kvp.Key],
                TurningDelta = ungatedPerGalaxy[kvp.Key]
            })
            .ToList();

        void AddCorrelation(string name, Func<DiskCoherenceStats, double> selector)
        {
            var x = correlationRows.Select(r => selector(r.Stats)).ToList();
            var yResidual = correlationRows.Select(r => r.ResidualRms).ToList();
            var yTurning = correlationRows.Select(r => r.TurningDelta).ToList();

            proxyCorrelations.Add(new DiskCoherenceProxyCorrelation(
                ProxyName: name,
                ResidualPearson: PearsonCorrelation(x, yResidual),
                ResidualSpearman: SpearmanCorrelation(x, yResidual),
                TurningDeltaPearson: PearsonCorrelation(x, yTurning),
                TurningDeltaSpearman: SpearmanCorrelation(x, yTurning)));
        }

        AddCorrelation("profileSmoothness", s => s.ProfileSmoothness);
        AddCorrelation("varianceDlnGbarDr", s => s.DlnGbarDrVariance);
        AddCorrelation("innerToOuterCoherenceRatio", s => s.InnerToOuterCoherenceRatio);
        AddCorrelation("shearProxy", s => s.ShearProxy);
        AddCorrelation("outerToInnerRatioTimesProfileSmoothness", s => s.OuterToInnerRatioTimesProfileSmoothness);
        AddCorrelation("outerToInnerRatioTimesShearProxy", s => s.OuterToInnerRatioTimesShearProxy);

        var proxyDefinitions = new (string Name, Func<DiskCoherenceStats, double> Selector)[]
        {
            ("profileSmoothness", s => s.ProfileSmoothness),
            ("varianceDlnGbarDr", s => s.DlnGbarDrVariance),
            ("innerToOuterCoherenceRatio", s => s.InnerToOuterCoherenceRatio),
            ("shearProxy", s => s.ShearProxy),
            ("outerToInnerRatioTimesProfileSmoothness", s => s.OuterToInnerRatioTimesProfileSmoothness),
            ("outerToInnerRatioTimesShearProxy", s => s.OuterToInnerRatioTimesShearProxy)
        };

        // Best coherence gate is selected on train and applied unchanged to holdout.
        var proxyFits = proxyDefinitions
            .Select(p => new
            {
                p.Name,
                p.Selector,
                Fit = FitSoftGateOnTrainByDiskCoherenceSelector(trainRows, trainStats, correctionModel.GetCorrection, p.Selector)
            })
            .OrderBy(x => x.Fit.TrainRms)
            .ToList();

        if (proxyFits.Count == 0)
            throw new InvalidOperationException("No disk-coherence proxy fit could be evaluated.");

        var best = proxyFits.First();
        var bestDiagnostics = holdoutRows
            .Select(r =>
            {
                if (!holdoutStats.TryGetValue(r.GalaxyKey, out var stats))
                    return CreatePointDiagnostic(r, r.Residual, 0.0, 0.0);

                double gateValue = best.Selector(stats);
                double correction = correctionModel.GetCorrection(r.SignedTurningProxy);
                double weight = Sigmoid((gateValue - best.Fit.Threshold) / best.Fit.Width);
                return CreatePointDiagnostic(r, r.Residual - (weight * correction), weight, correction);
            })
            .ToList();

        double baselineRmsAll = ComputeRmsFromResiduals(baselineDiagnostics.Select(x => x.BaselineResidual).ToList());
        double ungatedRmsAll = ComputeRmsFromResiduals(ungatedDiagnostics.Select(x => x.CorrectedResidual).ToList());
        double bestRmsAll = ComputeRmsFromResiduals(bestDiagnostics.Select(x => x.CorrectedResidual).ToList());

        var bestPerGalaxy = BuildPerGalaxyImprovement(bestDiagnostics);
        int improved = bestPerGalaxy.Count(x => x.DeltaRms > 0);
        int worsened = bestPerGalaxy.Count(x => x.DeltaRms < 0);

        return new GlobalDiskCoherenceReport(
            Mode: mode,
            TrainGalaxyCount: trainStats.Count,
            HoldoutGalaxyCount: holdoutStats.Count,
            BaselineRmsAll: baselineRmsAll,
            UngatedRmsAll: ungatedRmsAll,
            UngatedDeltaRmsAll: baselineRmsAll - ungatedRmsAll,
            ProxyCorrelations: proxyCorrelations,
            BestProxyName: best.Name,
            BestProxyThreshold: best.Fit.Threshold,
            BestProxyWidth: best.Fit.Width,
            BestProxyCorrectedRmsAll: bestRmsAll,
            BestProxyDeltaRmsAll: baselineRmsAll - bestRmsAll,
            BestProxyImprovedGalaxyCount: improved,
            BestProxyWorsenedGalaxyCount: worsened,
            TopImproved: bestPerGalaxy.Where(x => x.DeltaRms > 0).Take(10).ToList(),
            TopWorsened: bestPerGalaxy.Where(x => x.DeltaRms < 0).OrderBy(x => x.DeltaRms).Take(10).ToList());
    }

    /// <summary>
    /// Evaluates worst-baseline galaxies across geometry variants including smooth distributed takt-field kernels.
    ///
    /// Hypothesis:
    /// Worst TRM residual failures may stem from single-center geometry assumptions.
    ///
    /// Status:
    /// diagnostic + candidate.
    ///
    /// Limitation:
    /// Exploratory geometry family; no direct activation in the main model path.
    /// </summary>
    public static WorstGalaxyGeometryVariationReport EvaluateWorstGalaxyGeometryVariationNoRefit(
        List<RarPoint> points,
        double a0,
        int topGalaxyCount = 20,
        int holdoutModulo = 5,
        int holdoutRemainder = 0)
    {
        if (points == null || points.Count == 0)
            throw new ArgumentException("No RAR points were provided.", nameof(points));
        if (a0 <= 0 || !double.IsFinite(a0))
            throw new ArgumentOutOfRangeException(nameof(a0), "a0 must be positive and finite.");
        if (topGalaxyCount < 1)
            throw new ArgumentOutOfRangeException(nameof(topGalaxyCount), "topGalaxyCount must be >= 1.");
        if (holdoutModulo < 2)
            throw new ArgumentOutOfRangeException(nameof(holdoutModulo), "holdoutModulo must be >= 2.");
        if (holdoutRemainder < 0 || holdoutRemainder >= holdoutModulo)
            throw new ArgumentOutOfRangeException(nameof(holdoutRemainder), "holdoutRemainder must be in [0, holdoutModulo).");

        var groupedPoints = points
            .GroupBy(p => NormalizeGalaxyKey(p.GalaxyName))
            .ToDictionary(g => g.Key, g => g.OrderBy(p => p.RadiusKpc).ToList(), StringComparer.OrdinalIgnoreCase);

        var physicalStats = BuildPhysicalDiskStructureStats(groupedPoints, a0);
        // Train-only kernel search for smooth distributed field width/kind.
        var kernelCandidates = new (string Kernel, double WidthKpc)[]
        {
            ("gaussian", 0.35),
            ("gaussian", 0.60),
            ("gaussian", 1.00),
            ("gaussian", 1.60),
            ("gaussian", 2.40),
            ("gaussian", 3.60),
            ("gaussian", 5.40),
            ("gaussian", 8.00),
            ("exponential", 0.35),
            ("exponential", 0.60),
            ("exponential", 1.00),
            ("exponential", 1.60),
            ("exponential", 2.40),
            ("exponential", 3.60),
            ("exponential", 5.40),
            ("exponential", 8.00)
        };

        var trainResidualByCandidate = kernelCandidates.ToDictionary(
            c => $"{c.Kernel}:{c.WidthKpc:F2}",
            _ => new List<double>(),
            StringComparer.OrdinalIgnoreCase);

        var baselineByGalaxy = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var singleByGalaxy = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var distributedByGalaxy = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var toyByGalaxy = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        // First pass: accumulate baseline/variant residuals and train objective for kernel selection.
        foreach (var kvp in groupedPoints)
        {
            bool isHoldout = IsHoldoutGalaxy(kvp.Key, holdoutModulo, holdoutRemainder);
            var ordered = kvp.Value
                .Where(p =>
                    p.RadiusKpc > 0 &&
                    p.GobsMs2 > 0 &&
                    p.GbarMs2 > 0 &&
                    p.Vobs > 0 &&
                    double.IsFinite(p.RadiusKpc) &&
                    double.IsFinite(p.GobsMs2) &&
                    double.IsFinite(p.GbarMs2) &&
                    double.IsFinite(p.Vobs))
                .OrderBy(p => p.RadiusKpc)
                .ToList();

            if (ordered.Count < 4)
                continue;
            if (!physicalStats.TryGetValue(kvp.Key, out var ps))
                continue;

            double minR = ordered.Min(p => p.RadiusKpc);
            double maxR = ordered.Max(p => p.RadiusKpc);
            double span = Math.Max(0.0, maxR - minR);
            if (span <= 0.0)
                continue;

            var inner = ordered.Where(p => ((p.RadiusKpc - minR) / span) <= 0.30).ToList();
            var outer = ordered.Where(p => ((p.RadiusKpc - minR) / span) >= 0.70).ToList();
            if (inner.Count == 0 || outer.Count == 0)
                continue;

            double innerMeanGbar = inner.Average(p => p.GbarMs2);
            double outerMeanGbar = outer.Average(p => p.GbarMs2);
            var peakIndices = FindBaryonicPeakIndices(ordered);

            var baselineResiduals = new List<double>(ordered.Count);
            var singleCenterResiduals = new List<double>(ordered.Count);
            var distributedResiduals = new List<double>(ordered.Count);
            var multiCenterResiduals = new List<double>(ordered.Count);
            var smoothResidualByCandidate = kernelCandidates.ToDictionary(
                c => $"{c.Kernel}:{c.WidthKpc:F2}",
                _ => new List<double>(),
                StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < ordered.Count; i++)
            {
                var p = ordered[i];

                double baselinePred = PredictGobs(p.GbarMs2, a0, ModelType.ClockworkTRM);
                if (baselinePred <= 0 || !double.IsFinite(baselinePred))
                    continue;

                double singleCenterPred = baselinePred;

                double distributedGbar = ComputeDistributedGeometryGbar(p, minR, span, innerMeanGbar, outerMeanGbar);
                double distributedPred = PredictGobs(distributedGbar, a0, ModelType.ClockworkTRM);

                double multiCenterGbar = ComputeMultiCenterToyGeometryGbar(ordered, i, span, peakIndices);
                double multiCenterPred = PredictGobs(multiCenterGbar, a0, ModelType.ClockworkTRM);

                if (distributedPred <= 0 || !double.IsFinite(distributedPred))
                    distributedPred = baselinePred;
                if (multiCenterPred <= 0 || !double.IsFinite(multiCenterPred))
                    multiCenterPred = baselinePred;

                double logObs = Math.Log10(p.GobsMs2);
                baselineResiduals.Add(logObs - Math.Log10(baselinePred));
                singleCenterResiduals.Add(logObs - Math.Log10(singleCenterPred));
                distributedResiduals.Add(logObs - Math.Log10(distributedPred));
                multiCenterResiduals.Add(logObs - Math.Log10(multiCenterPred));

                foreach (var candidate in kernelCandidates)
                {
                    string candidateKey = $"{candidate.Kernel}:{candidate.WidthKpc:F2}";
                    double smoothGbar = ComputeSmoothDistributedTaktFieldGbar(
                        ordered,
                        i,
                        candidate.WidthKpc,
                        candidate.Kernel);
                    double smoothPred = PredictGobs(smoothGbar, a0, ModelType.ClockworkTRM);
                    if (smoothPred <= 0 || !double.IsFinite(smoothPred))
                        smoothPred = baselinePred;

                    smoothResidualByCandidate[candidateKey].Add(logObs - Math.Log10(smoothPred));
                }
            }

            if (baselineResiduals.Count < 4)
                continue;

            baselineByGalaxy[kvp.Key] = ComputeRmsFromResiduals(baselineResiduals);
            singleByGalaxy[kvp.Key] = ComputeRmsFromResiduals(singleCenterResiduals);
            distributedByGalaxy[kvp.Key] = ComputeRmsFromResiduals(distributedResiduals);
            toyByGalaxy[kvp.Key] = ComputeRmsFromResiduals(multiCenterResiduals);

            if (!isHoldout)
            {
                foreach (var candidate in kernelCandidates)
                {
                    string candidateKey = $"{candidate.Kernel}:{candidate.WidthKpc:F2}";
                    trainResidualByCandidate[candidateKey].AddRange(smoothResidualByCandidate[candidateKey]);
                }
            }
        }

        if (baselineByGalaxy.Count == 0)
            throw new InvalidOperationException("No valid galaxies for worst-galaxy geometry variation diagnostic.");

        // Freeze best kernel from train statistics before worst-galaxy ranking/validation.
        var bestKernel = kernelCandidates
            .Select(c =>
            {
                string key = $"{c.Kernel}:{c.WidthKpc:F2}";
                var residuals = trainResidualByCandidate[key];
                double trainRms = residuals.Count > 0 ? ComputeRmsFromResiduals(residuals) : double.PositiveInfinity;
                return new { c.Kernel, c.WidthKpc, TrainRms = trainRms };
            })
            .OrderBy(x => x.TrainRms)
            .First();

        var evaluations = new List<GeometryVariantEvaluation>();
        foreach (var kvp in groupedPoints)
        {
            if (!baselineByGalaxy.ContainsKey(kvp.Key) || !singleByGalaxy.ContainsKey(kvp.Key) || !distributedByGalaxy.ContainsKey(kvp.Key) || !toyByGalaxy.ContainsKey(kvp.Key))
                continue;
            if (!physicalStats.TryGetValue(kvp.Key, out var ps))
                continue;

            var ordered = kvp.Value
                .Where(p =>
                    p.RadiusKpc > 0 &&
                    p.GobsMs2 > 0 &&
                    p.GbarMs2 > 0 &&
                    p.Vobs > 0 &&
                    double.IsFinite(p.RadiusKpc) &&
                    double.IsFinite(p.GobsMs2) &&
                    double.IsFinite(p.GbarMs2) &&
                    double.IsFinite(p.Vobs))
                .OrderBy(p => p.RadiusKpc)
                .ToList();
            if (ordered.Count < 4)
                continue;

            var smoothResiduals = new List<double>(ordered.Count);
            for (int i = 0; i < ordered.Count; i++)
            {
                var p = ordered[i];
                double baselinePred = PredictGobs(p.GbarMs2, a0, ModelType.ClockworkTRM);
                if (baselinePred <= 0 || !double.IsFinite(baselinePred))
                    continue;

                double smoothGbar = ComputeSmoothDistributedTaktFieldGbar(
                    ordered,
                    i,
                    bestKernel.WidthKpc,
                    bestKernel.Kernel);
                double smoothPred = PredictGobs(smoothGbar, a0, ModelType.ClockworkTRM);
                if (smoothPred <= 0 || !double.IsFinite(smoothPred))
                    smoothPred = baselinePred;

                double logObs = Math.Log10(p.GobsMs2);
                smoothResiduals.Add(logObs - Math.Log10(smoothPred));
            }

            if (smoothResiduals.Count < 4)
                continue;

            double minR = ordered.Min(p => p.RadiusKpc);
            double maxR = ordered.Max(p => p.RadiusKpc);
            double span = Math.Max(0.0, maxR - minR);

            evaluations.Add(new GeometryVariantEvaluation(
                GalaxyKey: kvp.Key,
                BaselineRms: baselineByGalaxy[kvp.Key],
                SingleCenterRms: singleByGalaxy[kvp.Key],
                DiskDistributedRms: distributedByGalaxy[kvp.Key],
                MultiCenterToyRms: toyByGalaxy[kvp.Key],
                SmoothDistributedFieldRms: ComputeRmsFromResiduals(smoothResiduals),
                OuterInnerRatio: ps.OuterToInnerBaryonicAccelerationRatio,
                GasDominance: ps.GasDominanceProxy,
                RadialSpanKpc: span,
                PointCount: smoothResiduals.Count));
        }

        if (evaluations.Count == 0)
            throw new InvalidOperationException("No galaxies available for worst-galaxy geometry variation diagnostic.");

        // Validation view: rank worst baseline galaxies and compare frozen variants without per-galaxy refit.
        var top = evaluations
            .OrderByDescending(x => x.BaselineRms)
            .Take(Math.Min(topGalaxyCount, evaluations.Count))
            .ToList();

        var correlations = new List<GeometryVariantProxyCorrelation>
        {
            BuildGeometryVariantCorrelation(top, "single-center radial TRM", x => x.BaselineRms - x.SingleCenterRms),
            BuildGeometryVariantCorrelation(top, "disk-distributed outer/inner weighted TRM", x => x.BaselineRms - x.DiskDistributedRms),
            BuildGeometryVariantCorrelation(top, "off-center/multi-center toy geometry", x => x.BaselineRms - x.MultiCenterToyRms),
            BuildGeometryVariantCorrelation(
                top,
                $"smooth distributed takt-field ({bestKernel.Kernel}, width={bestKernel.WidthKpc:F2} kpc)",
                x => x.BaselineRms - x.SmoothDistributedFieldRms)
        };

        var variantAverages = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["single-center radial TRM"] = top.Average(x => x.BaselineRms - x.SingleCenterRms),
            ["disk-distributed outer/inner weighted TRM"] = top.Average(x => x.BaselineRms - x.DiskDistributedRms),
            ["off-center/multi-center toy geometry"] = top.Average(x => x.BaselineRms - x.MultiCenterToyRms),
            ["smooth distributed takt-field"] = top.Average(x => x.BaselineRms - x.SmoothDistributedFieldRms)
        };

        string bestVariant = variantAverages
            .OrderByDescending(kvp => kvp.Value)
            .First()
            .Key;

        Func<GeometryVariantEvaluation, double> bestDeltaSelector = bestVariant switch
        {
            "single-center radial TRM" => x => x.BaselineRms - x.SingleCenterRms,
            "disk-distributed outer/inner weighted TRM" => x => x.BaselineRms - x.DiskDistributedRms,
            "off-center/multi-center toy geometry" => x => x.BaselineRms - x.MultiCenterToyRms,
            _ => x => x.BaselineRms - x.SmoothDistributedFieldRms
        };

        var structureScores = BuildStructureScores(top);
        var bestDeltas = top.Select(bestDeltaSelector).ToList();
        var scoreValues = top.Select(x => structureScores[x.GalaxyKey]).ToList();
        double bestCorr = PearsonCorrelation(bestDeltas, scoreValues);
        bool corrUsable = double.IsFinite(bestCorr) && Math.Abs(bestCorr) >= 0.15;

        var entries = top
            .Select(x =>
            {
                double alignedScore = structureScores[x.GalaxyKey] * (corrUsable ? Math.Sign(bestCorr) : 0.0);
                bool correlatedImprovement = corrUsable && (bestDeltaSelector(x) * alignedScore >= 0.0);

                return new WorstGalaxyGeometryVariationEntry(
                    GalaxyKey: x.GalaxyKey,
                    BaselineRms: x.BaselineRms,
                    SingleCenterRms: x.SingleCenterRms,
                    SingleCenterDelta: x.BaselineRms - x.SingleCenterRms,
                    DiskDistributedRms: x.DiskDistributedRms,
                    DiskDistributedDelta: x.BaselineRms - x.DiskDistributedRms,
                    MultiCenterToyRms: x.MultiCenterToyRms,
                    MultiCenterToyDelta: x.BaselineRms - x.MultiCenterToyRms,
                    SmoothDistributedFieldRms: x.SmoothDistributedFieldRms,
                    SmoothDistributedFieldDelta: x.BaselineRms - x.SmoothDistributedFieldRms,
                    OuterInnerRatio: x.OuterInnerRatio,
                    GasDominance: x.GasDominance,
                    RadialSpanKpc: x.RadialSpanKpc,
                    PointCount: x.PointCount,
                    ImprovementCorrelatesWithDiskStructure: correlatedImprovement);
            })
            .ToList();

        int smoothBeatsSingle = entries.Count(x => x.SmoothDistributedFieldRms < x.SingleCenterRms);
        int smoothBeatsToy = entries.Count(x => x.SmoothDistributedFieldRms < x.MultiCenterToyRms);
        double meanSmoothDeltaVsSingle = entries.Average(x => x.SingleCenterRms - x.SmoothDistributedFieldRms);
        double meanSmoothDeltaVsToy = entries.Average(x => x.MultiCenterToyRms - x.SmoothDistributedFieldRms);

        return new WorstGalaxyGeometryVariationReport(
            FixedA0: a0,
            TopGalaxyCount: entries.Count,
            BestSmoothKernelKind: bestKernel.Kernel,
            BestSmoothKernelWidthKpc: bestKernel.WidthKpc,
            SmoothBeatsSingleCount: smoothBeatsSingle,
            SmoothBeatsToyCount: smoothBeatsToy,
            MeanSmoothDeltaVsSingle: meanSmoothDeltaVsSingle,
            MeanSmoothDeltaVsToy: meanSmoothDeltaVsToy,
            Galaxies: entries,
            VariantCorrelations: correlations,
            BestVariantName: bestVariant);
    }

    public static PhysicalDiskStructureCouplingReport EvaluatePhysicalDiskStructureCouplingNoRefit(
        List<RarPoint> points,
        double a0,
        TurningMemoryCorrectionMode mode,
        TurningMemoryCorrectionOptions? options = null)
    {
        if (mode == TurningMemoryCorrectionMode.None)
            throw new ArgumentOutOfRangeException(nameof(mode), "Use BinBased or Interpolated for physical structure diagnostics.");

        if (points == null || points.Count == 0)
            throw new ArgumentException("No RAR points were provided.", nameof(points));
        if (a0 <= 0 || !double.IsFinite(a0))
            throw new ArgumentOutOfRangeException(nameof(a0), "a0 must be positive and finite.");

        options ??= new TurningMemoryCorrectionOptions(mode, 5, 0, 3);

        var rows = BuildTurningResidualRows(points, a0);
        if (rows.Count == 0)
            throw new InvalidOperationException("No valid turning-memory diagnostic points could be built.");

        var pointGroups = points
            .GroupBy(p => NormalizeGalaxyKey(p.GalaxyName))
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        var trainRows = rows
            .Where(r => !IsHoldoutGalaxy(r.GalaxyKey, options.HoldoutModulo, options.HoldoutRemainder))
            .ToList();
        var holdoutRows = rows
            .Where(r => IsHoldoutGalaxy(r.GalaxyKey, options.HoldoutModulo, options.HoldoutRemainder))
            .ToList();

        if (trainRows.Count == 0 || holdoutRows.Count == 0)
            throw new InvalidOperationException("Train/holdout split produced an empty subset.");

        var trainStats = BuildGalaxyStats(trainRows);
        var holdoutStats = BuildGalaxyStats(holdoutRows);
        var trainPhysical = BuildPhysicalDiskStructureStats(pointGroups, a0)
            .Where(kvp => trainStats.ContainsKey(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        var holdoutPhysical = BuildPhysicalDiskStructureStats(pointGroups, a0)
            .Where(kvp => holdoutStats.ContainsKey(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var correctionModel = BuildTurningCorrectionModel(trainRows, options.BinCount, mode);

        var baselineDiagnostics = holdoutRows
            .Select(r => CreatePointDiagnostic(r, r.Residual, 0.0, 0.0))
            .ToList();
        var ungatedDiagnostics = holdoutRows
            .Select(r =>
            {
                double c = correctionModel.GetCorrection(r.SignedTurningProxy);
                return CreatePointDiagnostic(r, r.Residual - c, 1.0, c);
            })
            .ToList();

        var baselinePerGalaxy = BuildPerGalaxyImprovement(baselineDiagnostics)
            .ToDictionary(x => x.GalaxyKey, x => x.BaselineRms);
        var ungatedPerGalaxy = BuildPerGalaxyImprovement(ungatedDiagnostics)
            .ToDictionary(x => x.GalaxyKey, x => x.DeltaRms);

        double hsbThreshold = Percentile(trainStats.Values.Select(x => x.MeanLogGbar).OrderBy(x => x).ToList(), 0.50);

        var correlationRows = holdoutPhysical
            .Where(kvp => baselinePerGalaxy.ContainsKey(kvp.Key) && ungatedPerGalaxy.ContainsKey(kvp.Key))
            .Select(kvp => new
            {
                Key = kvp.Key,
                Stats = kvp.Value,
                ResidualRms = baselinePerGalaxy[kvp.Key],
                TurningDelta = ungatedPerGalaxy[kvp.Key],
                IsHsb = holdoutStats.TryGetValue(kvp.Key, out var st) && st.MeanLogGbar > hsbThreshold
            })
            .ToList();

        var proxyDefs = new (string Name, Func<PhysicalDiskStructureStats, double> Selector)[]
        {
            ("outerBaryonicMassFraction", s => s.OuterBaryonicMassFraction),
            ("gasDominanceProxy", s => s.GasDominanceProxy),
            ("diskToBulgeProxy", s => s.DiskToBulgeProxy),
            ("outerToInnerBaryonicAccelerationRatio", s => s.OuterToInnerBaryonicAccelerationRatio),
            ("transitionRadiusKpc", s => s.TransitionRadiusKpc),
            ("transitionSharpness", s => s.TransitionSharpness)
        };

        var correlations = new List<PhysicalDiskStructureProxyCorrelation>();
        foreach (var p in proxyDefs)
        {
            var hsbVals = correlationRows.Where(r => r.IsHsb).Select(r => p.Selector(r.Stats)).ToList();
            var lsbVals = correlationRows.Where(r => !r.IsHsb).Select(r => p.Selector(r.Stats)).ToList();

            var x = correlationRows.Select(r => p.Selector(r.Stats)).ToList();
            var yR = correlationRows.Select(r => r.ResidualRms).ToList();
            var yT = correlationRows.Select(r => r.TurningDelta).ToList();

            correlations.Add(new PhysicalDiskStructureProxyCorrelation(
                ProxyName: p.Name,
                ResidualPearson: PearsonCorrelation(x, yR),
                ResidualSpearman: SpearmanCorrelation(x, yR),
                TurningDeltaPearson: PearsonCorrelation(x, yT),
                TurningDeltaSpearman: SpearmanCorrelation(x, yT),
                MeanHsb: hsbVals.Count > 0 ? hsbVals.Average() : double.NaN,
                MeanLsb: lsbVals.Count > 0 ? lsbVals.Average() : double.NaN,
                HsbMinusLsb: (hsbVals.Count > 0 ? hsbVals.Average() : double.NaN) - (lsbVals.Count > 0 ? lsbVals.Average() : double.NaN)));
        }

        var proxyFits = proxyDefs
            .Where(p => trainPhysical.Count >= 5)
            .Select(p => new
            {
                p.Name,
                p.Selector,
                Fit = FitSoftGateOnTrainByPhysicalSelector(trainRows, trainPhysical, correctionModel.GetCorrection, p.Selector)
            })
            .OrderBy(x => x.Fit.TrainRms)
            .ToList();

        if (proxyFits.Count == 0)
            throw new InvalidOperationException("No physical proxy fit could be evaluated.");

        var best = proxyFits.First();

        var bestDiagnostics = holdoutRows
            .Select(r =>
            {
                if (!holdoutPhysical.TryGetValue(r.GalaxyKey, out var ps))
                    return CreatePointDiagnostic(r, r.Residual, 0.0, 0.0);

                double gateValue = best.Selector(ps);
                double correction = correctionModel.GetCorrection(r.SignedTurningProxy);
                double weight = Sigmoid((gateValue - best.Fit.Threshold) / best.Fit.Width);
                return CreatePointDiagnostic(r, r.Residual - (weight * correction), weight, correction);
            })
            .ToList();

        double baselineRmsAll = ComputeRmsFromResiduals(baselineDiagnostics.Select(x => x.BaselineResidual).ToList());
        double ungatedRmsAll = ComputeRmsFromResiduals(ungatedDiagnostics.Select(x => x.CorrectedResidual).ToList());
        double bestRmsAll = ComputeRmsFromResiduals(bestDiagnostics.Select(x => x.CorrectedResidual).ToList());

        double baselineRmsHsb = ComputeRmsFromResiduals(baselineDiagnostics.Where(x => x.MeanLogGbar > hsbThreshold).Select(x => x.BaselineResidual).ToList());
        double ungatedRmsHsb = ComputeRmsFromResiduals(ungatedDiagnostics.Where(x => x.MeanLogGbar > hsbThreshold).Select(x => x.CorrectedResidual).ToList());
        double baselineRmsLsb = ComputeRmsFromResiduals(baselineDiagnostics.Where(x => x.MeanLogGbar <= hsbThreshold).Select(x => x.BaselineResidual).ToList());
        double ungatedRmsLsb = ComputeRmsFromResiduals(ungatedDiagnostics.Where(x => x.MeanLogGbar <= hsbThreshold).Select(x => x.CorrectedResidual).ToList());
        double bestRmsHsb = ComputeRmsFromResiduals(bestDiagnostics.Where(x => x.MeanLogGbar > hsbThreshold).Select(x => x.CorrectedResidual).ToList());
        double bestRmsLsb = ComputeRmsFromResiduals(bestDiagnostics.Where(x => x.MeanLogGbar <= hsbThreshold).Select(x => x.CorrectedResidual).ToList());

        var bestPerGalaxy = BuildPerGalaxyImprovement(bestDiagnostics);
        int improved = bestPerGalaxy.Count(x => x.DeltaRms > 0);
        int worsened = bestPerGalaxy.Count(x => x.DeltaRms < 0);

        return new PhysicalDiskStructureCouplingReport(
            Mode: mode,
            TrainGalaxyCount: trainPhysical.Count,
            HoldoutGalaxyCount: holdoutPhysical.Count,
            BaselineRmsAll: baselineRmsAll,
            UngatedRmsAll: ungatedRmsAll,
            UngatedDeltaRmsAll: baselineRmsAll - ungatedRmsAll,
            BaselineRmsHsb: baselineRmsHsb,
            UngatedRmsHsb: ungatedRmsHsb,
            BaselineRmsLsb: baselineRmsLsb,
            UngatedRmsLsb: ungatedRmsLsb,
            ProxyCorrelations: correlations,
            BestProxyName: best.Name,
            BestProxyThreshold: best.Fit.Threshold,
            BestProxyWidth: best.Fit.Width,
            BestProxyCorrectedRmsAll: bestRmsAll,
            BestProxyDeltaRmsAll: baselineRmsAll - bestRmsAll,
            BestProxyCorrectedRmsHsb: bestRmsHsb,
            BestProxyCorrectedRmsLsb: bestRmsLsb,
            BestProxyImprovedGalaxyCount: improved,
            BestProxyWorsenedGalaxyCount: worsened,
            TopImproved: bestPerGalaxy.Where(x => x.DeltaRms > 0).Take(10).ToList(),
            TopWorsened: bestPerGalaxy.Where(x => x.DeltaRms < 0).OrderBy(x => x.DeltaRms).Take(10).ToList());
    }

    /// <summary>
    /// Forced outer/inner acceleration-contrast gate comparison (direct, inverse, log) with no-refit holdout scoring.
    ///
    /// Hypothesis:
    /// The sign/direction of inner-to-outer baryonic contrast is physically informative for residual correction.
    ///
    /// Status:
    /// tested-effective candidate (diagnostic scope).
    ///
    /// Limitation:
    /// Optional diagnostic branch only; baseline a0 and main path are preserved.
    /// </summary>
    public static OuterInnerContrastGateReport EvaluateOuterInnerContrastGateNoRefit(
        List<RarPoint> points,
        double a0,
        TurningMemoryCorrectionMode mode,
        TurningMemoryCorrectionOptions? options = null)
    {
        if (mode == TurningMemoryCorrectionMode.None)
            throw new ArgumentOutOfRangeException(nameof(mode), "Use BinBased or Interpolated for outer/inner contrast diagnostics.");

        if (points == null || points.Count == 0)
            throw new ArgumentException("No RAR points were provided.", nameof(points));
        if (a0 <= 0 || !double.IsFinite(a0))
            throw new ArgumentOutOfRangeException(nameof(a0), "a0 must be positive and finite.");

        options ??= new TurningMemoryCorrectionOptions(mode, 5, 0, 3);

        var rows = BuildTurningResidualRows(points, a0);
        if (rows.Count == 0)
            throw new InvalidOperationException("No valid turning-memory diagnostic points could be built.");

        var pointGroups = points
            .GroupBy(p => NormalizeGalaxyKey(p.GalaxyName))
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        // No-refit separation: all gate parameters are fitted on train and frozen for holdout.
        var trainRows = rows
            .Where(r => !IsHoldoutGalaxy(r.GalaxyKey, options.HoldoutModulo, options.HoldoutRemainder))
            .ToList();
        var holdoutRows = rows
            .Where(r => IsHoldoutGalaxy(r.GalaxyKey, options.HoldoutModulo, options.HoldoutRemainder))
            .ToList();

        if (trainRows.Count == 0 || holdoutRows.Count == 0)
            throw new InvalidOperationException("Train/holdout split produced an empty subset.");

        var trainStats = BuildGalaxyStats(trainRows);
        var holdoutStats = BuildGalaxyStats(holdoutRows);
        var trainPhysical = BuildPhysicalDiskStructureStats(pointGroups, a0)
            .Where(kvp => trainStats.ContainsKey(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        var holdoutPhysical = BuildPhysicalDiskStructureStats(pointGroups, a0)
            .Where(kvp => holdoutStats.ContainsKey(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        // Keep correction model tied to train data only for a clean holdout diagnostic.
        var correctionModel = BuildTurningCorrectionModel(trainRows, options.BinCount, mode);

        double hsbThreshold = Percentile(trainStats.Values.Select(x => x.MeanLogGbar).OrderBy(x => x).ToList(), 0.50);

        var baselineDiagnostics = holdoutRows
            .Select(r => CreatePointDiagnostic(r, r.Residual, 0.0, 0.0))
            .ToList();
        var ungatedDiagnostics = holdoutRows
            .Select(r =>
            {
                double c = correctionModel.GetCorrection(r.SignedTurningProxy);
                return CreatePointDiagnostic(r, r.Residual - c, 1.0, c);
            })
            .ToList();

        TurningMemoryDiagnosticSummary BuildVariant(
            string label,
            Func<PhysicalDiskStructureStats, double> selector)
        {
            var fit = FitSoftGateOnTrainByPhysicalSelector(trainRows, trainPhysical, correctionModel.GetCorrection, selector);

            var corrected = holdoutRows
                .Select(r =>
                {
                    if (!holdoutPhysical.TryGetValue(r.GalaxyKey, out var stats))
                        return CreatePointDiagnostic(r, r.Residual, 0.0, 0.0);

                    double gateValue = selector(stats);
                    double correction = correctionModel.GetCorrection(r.SignedTurningProxy);
                    double weight = Sigmoid((gateValue - fit.Threshold) / fit.Width);
                    return CreatePointDiagnostic(r, r.Residual - (weight * correction), weight, correction);
                })
                .ToList();

            return BuildDiagnosticSummary(
                label: label,
                diagnostics: corrected,
                hsbThreshold: hsbThreshold,
                gateThreshold: fit.Threshold,
                gateWidth: fit.Width,
                gateVariable: null);
        }

        const double eps = 1e-12;
        var ratioSummary = BuildVariant(
            "OuterToInnerRatioGate",
            s => s.OuterToInnerBaryonicAccelerationRatio);
        var inverseSummary = BuildVariant(
            "InverseOuterToInnerRatioGate",
            s => 1.0 / Math.Max(s.OuterToInnerBaryonicAccelerationRatio, eps));
        var logSummary = BuildVariant(
            "LogOuterToInnerRatioGate",
            s => Math.Log(Math.Max(s.OuterToInnerBaryonicAccelerationRatio, eps)));

        var baselineSummary = BuildDiagnosticSummary(
            label: "Baseline",
            diagnostics: baselineDiagnostics,
            hsbThreshold: hsbThreshold,
            gateThreshold: double.NaN,
            gateWidth: double.NaN,
            gateVariable: null);

        var ungatedSummary = BuildDiagnosticSummary(
            label: "UngatedTurningMemory",
            diagnostics: ungatedDiagnostics,
            hsbThreshold: hsbThreshold,
            gateThreshold: double.NaN,
            gateWidth: double.NaN,
            gateVariable: null);

        var best = new[] { ratioSummary, inverseSummary, logSummary }
            .OrderByDescending(x => x.DeltaRmsAll)
            .First();

        return new OuterInnerContrastGateReport(
            Mode: mode,
            Baseline: baselineSummary,
            Ungated: ungatedSummary,
            OuterToInnerRatioGate: ratioSummary,
            InverseOuterToInnerRatioGate: inverseSummary,
            LogOuterToInnerRatioGate: logSummary,
            BestVariantLabel: best.Label);
    }

    private static TurningMemoryPointDiagnostic CreatePointDiagnostic(
        TurningResidualRow row,
        double correctedResidual,
        double gateWeight,
        double turningCorrection)
    {
        return new TurningMemoryPointDiagnostic(
            row.GalaxyKey,
            row.RadiusKpc,
            row.Omega,
            row.DLogGbarDr,
            row.SignedTurningProxy,
            row.MeanLogGbar,
            row.Residual,
            correctedResidual,
            gateWeight,
            turningCorrection);
    }

    private static Dictionary<string, TurningGalaxyStats> BuildGalaxyStats(List<TurningResidualRow> rows)
    {
        return rows
            .GroupBy(r => r.GalaxyKey)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var ordered = g.OrderBy(x => x.RadiusKpc).ToList();
                    if (ordered.Count == 0)
                    {
                        return new TurningGalaxyStats(
                            MeanLogGbar: 0.0,
                            MeanAbsDlnGbarDr: 0.0,
                            OuterEdgeGradient: 0.0,
                            InnerGradient: 0.0,
                            EdgeToInnerGradientRatio: 0.0,
                            RadiusSpanKpc: 0.0,
                            PointCount: 0,
                            SurfaceProfileSharpness: 0.0);
                    }

                    const double eps = 1e-12;
                    double minR = g.Min(x => x.RadiusKpc);
                    double maxR = g.Max(x => x.RadiusKpc);
                    double span = Math.Max(0.0, maxR - minR);
                    double meanAbs = g.Average(x => Math.Abs(x.DLogGbarDr));

                    var inner = ordered
                        .Where(x => span <= eps || ((x.RadiusKpc - minR) / span) <= 0.30)
                        .Select(x => Math.Abs(x.DLogGbarDr))
                        .ToList();

                    var outer = ordered
                        .Where(x => span <= eps || ((x.RadiusKpc - minR) / span) >= 0.70)
                        .Select(x => Math.Abs(x.DLogGbarDr))
                        .ToList();

                    double innerGradient = inner.Count > 0 ? inner.Average() : meanAbs;
                    double outerGradient = outer.Count > 0 ? outer.Average() : meanAbs;
                    double ratio = outerGradient / Math.Max(innerGradient, eps);
                    double sharpness = outerGradient / Math.Max(Math.Log(1.0 + Math.Max(span, 0.0)), eps);

                    return new TurningGalaxyStats(
                        MeanLogGbar: g.Average(x => x.MeanLogGbar),
                        MeanAbsDlnGbarDr: meanAbs,
                        OuterEdgeGradient: outerGradient,
                        InnerGradient: innerGradient,
                        EdgeToInnerGradientRatio: ratio,
                        RadiusSpanKpc: span,
                        PointCount: g.Count(),
                        SurfaceProfileSharpness: sharpness);
                });
    }

    private static Dictionary<string, OuterInnerTaktStats> BuildOuterInnerTaktStats(List<TurningResidualRow> rows)
    {
        const double eps = 1e-12;

        return rows
            .GroupBy(r => r.GalaxyKey)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var ordered = g.OrderBy(x => x.RadiusKpc).ToList();
                    if (ordered.Count == 0)
                    {
                        return new OuterInnerTaktStats(
                            GInner: 0.0,
                            GOuter: 0.0,
                            OuterInnerRatio: 0.0,
                            OuterGradient: 0.0,
                            OmegaOuter: 0.0,
                            SyncProxy: 0.0,
                            SyncGradientProxy: 0.0,
                            SyncContrastProxy: 0.0);
                    }

                    double minR = ordered.Min(x => x.RadiusKpc);
                    double maxR = ordered.Max(x => x.RadiusKpc);
                    double span = Math.Max(0.0, maxR - minR);

                    var inner = ordered
                        .Where(x => span <= eps || ((x.RadiusKpc - minR) / span) <= 0.30)
                        .ToList();
                    var outer = ordered
                        .Where(x => span <= eps || ((x.RadiusKpc - minR) / span) >= 0.70)
                        .ToList();

                    if (inner.Count == 0)
                        inner = ordered;
                    if (outer.Count == 0)
                        outer = ordered;

                    double gInner = inner.Average(x => x.GbarMs2);
                    double gOuter = outer.Average(x => x.GbarMs2);
                    double outerInnerRatio = gOuter / Math.Max(gInner, eps);
                    double outerGradient = outer.Average(x => Math.Abs(x.DLogGbarDr));
                    double omegaOuter = outer.Average(x => x.Omega);

                    double syncProxy = omegaOuter * outerInnerRatio;
                    double syncGradientProxy = omegaOuter * outerInnerRatio * outerGradient;
                    double syncContrastProxy = omegaOuter * (1.0 - outerInnerRatio) * outerGradient;

                    return new OuterInnerTaktStats(
                        GInner: gInner,
                        GOuter: gOuter,
                        OuterInnerRatio: outerInnerRatio,
                        OuterGradient: outerGradient,
                        OmegaOuter: omegaOuter,
                        SyncProxy: syncProxy,
                        SyncGradientProxy: syncGradientProxy,
                        SyncContrastProxy: syncContrastProxy);
                });
    }

    private static Dictionary<string, DiskCoherenceStats> BuildDiskCoherenceStats(List<TurningResidualRow> rows)
    {
        const double eps = 1e-12;

        return rows
            .GroupBy(r => r.GalaxyKey)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var ordered = g.OrderBy(x => x.RadiusKpc).ToList();
                    if (ordered.Count == 0)
                    {
                        return new DiskCoherenceStats(
                            ProfileSmoothness: 0.0,
                            DlnGbarDrVariance: 0.0,
                            InnerToOuterCoherenceRatio: 1.0,
                            ShearProxy: 0.0,
                            OuterToInnerRatioTimesProfileSmoothness: 0.0,
                            OuterToInnerRatioTimesShearProxy: 0.0);
                    }

                    double minR = ordered.Min(x => x.RadiusKpc);
                    double maxR = ordered.Max(x => x.RadiusKpc);
                    double span = Math.Max(0.0, maxR - minR);

                    var inner = ordered
                        .Where(x => span <= eps || ((x.RadiusKpc - minR) / span) <= 0.30)
                        .ToList();
                    var outer = ordered
                        .Where(x => span <= eps || ((x.RadiusKpc - minR) / span) >= 0.70)
                        .ToList();

                    if (inner.Count == 0)
                        inner = ordered;
                    if (outer.Count == 0)
                        outer = ordered;

                    var dlnValues = ordered.Select(x => x.DLogGbarDr).ToList();
                    double dlnVariance = Variance(dlnValues);
                    double profileSmoothness = 1.0 / (1.0 + dlnVariance);

                    double innerVariance = Variance(inner.Select(x => x.DLogGbarDr).ToList());
                    double outerVariance = Variance(outer.Select(x => x.DLogGbarDr).ToList());
                    double innerCoherence = 1.0 / (1.0 + innerVariance);
                    double outerCoherence = 1.0 / (1.0 + outerVariance);
                    double coherenceRatio = innerCoherence / Math.Max(outerCoherence, eps);

                    double meanOmega = outer.Average(x => x.Omega);
                    double omegaVariance = Variance(outer.Select(x => x.Omega).ToList());
                    double omegaStd = Math.Sqrt(Math.Max(0.0, omegaVariance));
                    double shearProxy = omegaStd / Math.Max(Math.Abs(meanOmega), eps);

                    double gInner = inner.Average(x => x.GbarMs2);
                    double gOuter = outer.Average(x => x.GbarMs2);
                    double outerToInnerRatio = gOuter / Math.Max(gInner, eps);

                    return new DiskCoherenceStats(
                        ProfileSmoothness: profileSmoothness,
                        DlnGbarDrVariance: dlnVariance,
                        InnerToOuterCoherenceRatio: coherenceRatio,
                        ShearProxy: shearProxy,
                        OuterToInnerRatioTimesProfileSmoothness: outerToInnerRatio * profileSmoothness,
                        OuterToInnerRatioTimesShearProxy: outerToInnerRatio * shearProxy);
                });
    }

    private static (double Threshold, double Width) FitSoftGateOnTrainByVariable(
        List<TurningResidualRow> trainRows,
        Dictionary<string, TurningGalaxyStats> trainStats,
        Func<double, double> correctionSelector,
        TurningMemoryGateVariable gateVariable)
    {
        var gateValues = trainStats.Values
            .Select(stats => ResolveGateValue(stats, gateVariable))
            .Where(double.IsFinite)
            .OrderBy(x => x)
            .ToList();

        if (gateValues.Count < 5)
            throw new InvalidOperationException($"Insufficient train galaxies for gate variable {gateVariable}.");

        double q20 = Percentile(gateValues, 0.20);
        double q80 = Percentile(gateValues, 0.80);
        if (!double.IsFinite(q20) || !double.IsFinite(q80) || q80 <= q20)
            throw new InvalidOperationException($"Invalid gate variable distribution for {gateVariable}.");

        var thresholdGrid = BuildLinearGrid(q20, q80, 13);
        var widthGrid = new[] { 0.03, 0.05, 0.08, 0.12, 0.18, 0.26, 0.38, 0.55 };

        double bestThreshold = thresholdGrid[0];
        double bestWidth = widthGrid[0];
        double bestRms = double.MaxValue;

        foreach (double threshold in thresholdGrid)
        {
            foreach (double width in widthGrid)
            {
                var correctedResiduals = trainRows
                    .Select(r =>
                    {
                        double gateValue = ResolveGateValue(trainStats[r.GalaxyKey], gateVariable);
                        double correction = correctionSelector(r.SignedTurningProxy);
                        double weight = Sigmoid((gateValue - threshold) / width);
                        return r.Residual - (weight * correction);
                    })
                    .ToList();

                double rms = ComputeRmsFromResiduals(correctedResiduals);
                if (rms < bestRms)
                {
                    bestRms = rms;
                    bestThreshold = threshold;
                    bestWidth = width;
                }
            }
        }

        return (bestThreshold, bestWidth);
    }

    private sealed record GateFitResult(
        double Threshold,
        double Width,
        double TrainRms);

    private static GateFitResult FitSoftGateOnTrainBySelector(
        List<TurningResidualRow> trainRows,
        Dictionary<string, TurningGalaxyStats> trainStats,
        Func<double, double> correctionSelector,
        Func<TurningGalaxyStats, double> gateSelector)
    {
        var gateValues = trainStats.Values
            .Select(gateSelector)
            .Where(double.IsFinite)
            .OrderBy(x => x)
            .ToList();

        if (gateValues.Count < 5)
            throw new InvalidOperationException("Insufficient train galaxies for proxy soft-gate fitting.");

        double q20 = Percentile(gateValues, 0.20);
        double q80 = Percentile(gateValues, 0.80);
        if (!double.IsFinite(q20) || !double.IsFinite(q80) || q80 <= q20)
            throw new InvalidOperationException("Invalid proxy distribution for soft-gate fitting.");

        var thresholdGrid = BuildLinearGrid(q20, q80, 13);
        var widthGrid = new[] { 0.03, 0.05, 0.08, 0.12, 0.18, 0.26, 0.38, 0.55 };

        double bestThreshold = thresholdGrid[0];
        double bestWidth = widthGrid[0];
        double bestRms = double.MaxValue;

        foreach (double threshold in thresholdGrid)
        {
            foreach (double width in widthGrid)
            {
                var correctedResiduals = trainRows
                    .Select(r =>
                    {
                        double gateValue = gateSelector(trainStats[r.GalaxyKey]);
                        double correction = correctionSelector(r.SignedTurningProxy);
                        double weight = Sigmoid((gateValue - threshold) / width);
                        return r.Residual - (weight * correction);
                    })
                    .ToList();

                double rms = ComputeRmsFromResiduals(correctedResiduals);
                if (rms < bestRms)
                {
                    bestRms = rms;
                    bestThreshold = threshold;
                    bestWidth = width;
                }
            }
        }

        return new GateFitResult(bestThreshold, bestWidth, bestRms);
    }

    private static GateFitResult FitSoftGateOnTrainByOuterInnerTaktSelector(
        List<TurningResidualRow> trainRows,
        Dictionary<string, OuterInnerTaktStats> trainStats,
        Func<double, double> correctionSelector,
        Func<OuterInnerTaktStats, double> gateSelector)
    {
        var gateValues = trainStats.Values
            .Select(gateSelector)
            .Where(double.IsFinite)
            .OrderBy(x => x)
            .ToList();

        if (gateValues.Count < 5)
            throw new InvalidOperationException("Insufficient train galaxies for outer-inner takt soft-gate fitting.");

        double q20 = Percentile(gateValues, 0.20);
        double q80 = Percentile(gateValues, 0.80);
        if (!double.IsFinite(q20) || !double.IsFinite(q80) || q80 <= q20)
            throw new InvalidOperationException("Invalid outer-inner takt proxy distribution for soft-gate fitting.");

        var thresholdGrid = BuildLinearGrid(q20, q80, 13);
        var widthGrid = new[] { 0.03, 0.05, 0.08, 0.12, 0.18, 0.26, 0.38, 0.55 };

        double bestThreshold = thresholdGrid[0];
        double bestWidth = widthGrid[0];
        double bestRms = double.MaxValue;

        foreach (double threshold in thresholdGrid)
        {
            foreach (double width in widthGrid)
            {
                var correctedResiduals = trainRows
                    .Select(r =>
                    {
                        if (!trainStats.TryGetValue(r.GalaxyKey, out var stats))
                            return r.Residual;

                        double gateValue = gateSelector(stats);
                        double correction = correctionSelector(r.SignedTurningProxy);
                        double weight = Sigmoid((gateValue - threshold) / width);
                        return r.Residual - (weight * correction);
                    })
                    .ToList();

                double rms = ComputeRmsFromResiduals(correctedResiduals);
                if (rms < bestRms)
                {
                    bestRms = rms;
                    bestThreshold = threshold;
                    bestWidth = width;
                }
            }
        }

        return new GateFitResult(bestThreshold, bestWidth, bestRms);
    }

    private static GateFitResult FitSoftGateOnTrainByDiskCoherenceSelector(
        List<TurningResidualRow> trainRows,
        Dictionary<string, DiskCoherenceStats> trainStats,
        Func<double, double> correctionSelector,
        Func<DiskCoherenceStats, double> gateSelector)
    {
        var gateValues = trainStats.Values
            .Select(gateSelector)
            .Where(double.IsFinite)
            .OrderBy(x => x)
            .ToList();

        if (gateValues.Count < 5)
            throw new InvalidOperationException("Insufficient train galaxies for disk-coherence soft-gate fitting.");

        double q20 = Percentile(gateValues, 0.20);
        double q80 = Percentile(gateValues, 0.80);
        if (!double.IsFinite(q20) || !double.IsFinite(q80) || q80 <= q20)
            throw new InvalidOperationException("Invalid disk-coherence proxy distribution for soft-gate fitting.");

        var thresholdGrid = BuildLinearGrid(q20, q80, 13);
        var widthGrid = new[] { 0.03, 0.05, 0.08, 0.12, 0.18, 0.26, 0.38, 0.55 };

        double bestThreshold = thresholdGrid[0];
        double bestWidth = widthGrid[0];
        double bestRms = double.MaxValue;

        foreach (double threshold in thresholdGrid)
        {
            foreach (double width in widthGrid)
            {
                var correctedResiduals = trainRows
                    .Select(r =>
                    {
                        if (!trainStats.TryGetValue(r.GalaxyKey, out var stats))
                            return r.Residual;

                        double gateValue = gateSelector(stats);
                        double correction = correctionSelector(r.SignedTurningProxy);
                        double weight = Sigmoid((gateValue - threshold) / width);
                        return r.Residual - (weight * correction);
                    })
                    .ToList();

                double rms = ComputeRmsFromResiduals(correctedResiduals);
                if (rms < bestRms)
                {
                    bestRms = rms;
                    bestThreshold = threshold;
                    bestWidth = width;
                }
            }
        }

        return new GateFitResult(bestThreshold, bestWidth, bestRms);
    }

    private static GateFitResult FitSoftGateOnTrainByPhysicalSelector(
        List<TurningResidualRow> trainRows,
        Dictionary<string, PhysicalDiskStructureStats> trainPhysical,
        Func<double, double> correctionSelector,
        Func<PhysicalDiskStructureStats, double> gateSelector)
    {
        var gateValues = trainPhysical.Values
            .Select(gateSelector)
            .Where(double.IsFinite)
            .OrderBy(x => x)
            .ToList();

        if (gateValues.Count < 5)
            throw new InvalidOperationException("Insufficient train galaxies for physical-proxy soft-gate fitting.");

        double q20 = Percentile(gateValues, 0.20);
        double q80 = Percentile(gateValues, 0.80);
        if (!double.IsFinite(q20) || !double.IsFinite(q80) || q80 <= q20)
            throw new InvalidOperationException("Invalid physical-proxy distribution for soft-gate fitting.");

        var thresholdGrid = BuildLinearGrid(q20, q80, 13);
        var widthGrid = new[] { 0.03, 0.05, 0.08, 0.12, 0.18, 0.26, 0.38, 0.55 };

        double bestThreshold = thresholdGrid[0];
        double bestWidth = widthGrid[0];
        double bestRms = double.MaxValue;

        foreach (double threshold in thresholdGrid)
        {
            foreach (double width in widthGrid)
            {
                var correctedResiduals = trainRows
                    .Select(r =>
                    {
                        if (!trainPhysical.TryGetValue(r.GalaxyKey, out var stats))
                            return r.Residual;

                        double gateValue = gateSelector(stats);
                        double correction = correctionSelector(r.SignedTurningProxy);
                        double weight = Sigmoid((gateValue - threshold) / width);
                        return r.Residual - (weight * correction);
                    })
                    .ToList();

                double rms = ComputeRmsFromResiduals(correctedResiduals);
                if (rms < bestRms)
                {
                    bestRms = rms;
                    bestThreshold = threshold;
                    bestWidth = width;
                }
            }
        }

        return new GateFitResult(bestThreshold, bestWidth, bestRms);
    }

    private static Dictionary<string, PhysicalDiskStructureStats> BuildPhysicalDiskStructureStats(
        Dictionary<string, List<RarPoint>> galaxyPointGroups,
        double a0)
    {
        var map = new Dictionary<string, PhysicalDiskStructureStats>(StringComparer.OrdinalIgnoreCase);
        const double eps = 1e-12;

        foreach (var kvp in galaxyPointGroups)
        {
            var ordered = kvp.Value
                .Where(p => p.RadiusKpc > 0 && p.GbarMs2 > 0)
                .OrderBy(p => p.RadiusKpc)
                .ToList();

            if (ordered.Count < 3)
                continue;

            double minR = ordered.Min(p => p.RadiusKpc);
            double maxR = ordered.Max(p => p.RadiusKpc);
            double span = Math.Max(0.0, maxR - minR);

            var outer = ordered.Where(p => span <= eps || ((p.RadiusKpc - minR) / span) >= 0.70).ToList();
            var inner = ordered.Where(p => span <= eps || ((p.RadiusKpc - minR) / span) <= 0.30).ToList();

            double SumBaryonicPower(IEnumerable<RarPoint> pts) =>
                pts.Sum(p =>
                {
                    double gas = Math.Max(p.Vgas, 0.0);
                    double disk = Math.Max(p.Vdisk, 0.0);
                    double bulge = Math.Max(p.Vbulge, 0.0);
                    return (gas * gas) + (0.5 * disk * disk) + (0.7 * bulge * bulge);
                });

            double totalPower = SumBaryonicPower(ordered);
            double outerPower = SumBaryonicPower(outer);
            double outerMassFraction = totalPower > eps ? outerPower / totalPower : 0.0;

            double gasPower = ordered.Sum(p => Math.Max(p.Vgas, 0.0) * Math.Max(p.Vgas, 0.0));
            double diskPower = ordered.Sum(p => Math.Max(p.Vdisk, 0.0) * Math.Max(p.Vdisk, 0.0));
            double bulgePower = ordered.Sum(p => Math.Max(p.Vbulge, 0.0) * Math.Max(p.Vbulge, 0.0));

            double gasDominance = (gasPower + diskPower + bulgePower) > eps
                ? gasPower / (gasPower + diskPower + bulgePower)
                : 0.0;
            double totalStellarPower = diskPower + bulgePower;
            double guardedBulgeDenominator = Math.Max(
                Math.Max(bulgePower, eps),
                0.02 * Math.Max(totalStellarPower, eps));
            double diskToBulge = diskPower / guardedBulgeDenominator;
            diskToBulge = Math.Clamp(diskToBulge, 0.0, 50.0);

            double meanOuterGbar = outer.Count > 0 ? outer.Average(p => p.GbarMs2) : ordered.Average(p => p.GbarMs2);
            double meanInnerGbar = inner.Count > 0 ? inner.Average(p => p.GbarMs2) : ordered.Average(p => p.GbarMs2);
            double outerToInnerRatio = meanOuterGbar / Math.Max(meanInnerGbar, eps);

            double transitionRadius = maxR;
            int transitionIndex = -1;
            for (int i = 0; i < ordered.Count; i++)
            {
                if (ordered[i].GbarMs2 <= a0)
                {
                    transitionRadius = ordered[i].RadiusKpc;
                    transitionIndex = i;
                    break;
                }
            }

            double transitionSharpness = 0.0;
            if (transitionIndex >= 1 && transitionIndex < ordered.Count - 1)
            {
                int iStart = Math.Max(0, transitionIndex - 2);
                int iEnd = Math.Min(ordered.Count - 1, transitionIndex + 2);
                var grads = new List<double>();
                for (int i = iStart; i < iEnd; i++)
                {
                    var p1 = ordered[i];
                    var p2 = ordered[i + 1];
                    if (p1.GbarMs2 <= 0 || p2.GbarMs2 <= 0)
                        continue;
                    double dr = p2.RadiusKpc - p1.RadiusKpc;
                    if (dr <= 0)
                        continue;
                    grads.Add(Math.Abs((Math.Log(p2.GbarMs2) - Math.Log(p1.GbarMs2)) / dr));
                }

                transitionSharpness = grads.Count > 0 ? grads.Average() : 0.0;
            }

            map[kvp.Key] = new PhysicalDiskStructureStats(
                OuterBaryonicMassFraction: outerMassFraction,
                GasDominanceProxy: gasDominance,
                DiskToBulgeProxy: diskToBulge,
                OuterToInnerBaryonicAccelerationRatio: outerToInnerRatio,
                TransitionRadiusKpc: transitionRadius,
                TransitionSharpness: transitionSharpness);
        }

        return map;
    }

    private static double ResolveGateValue(
        TurningGalaxyStats stats,
        TurningMemoryGateVariable variable)
    {
        const double eps = 1e-12;

        return variable switch
        {
            TurningMemoryGateVariable.MeanLogGbar => stats.MeanLogGbar,
            TurningMemoryGateVariable.MeanAbsDlnGbarDr => stats.MeanAbsDlnGbarDr,
            TurningMemoryGateVariable.MeanAbsDlnGbarDrOverRadiusSpan =>
                stats.MeanAbsDlnGbarDr / Math.Max(stats.RadiusSpanKpc, eps),
            TurningMemoryGateVariable.MeanAbsDlnGbarDrOverPointCount =>
                stats.MeanAbsDlnGbarDr / Math.Max(stats.PointCount, 1),
            TurningMemoryGateVariable.ProfileSharpness =>
                stats.MeanAbsDlnGbarDr / Math.Max(Math.Log(1.0 + Math.Max(stats.RadiusSpanKpc, 0.0)), eps),
            _ => throw new ArgumentOutOfRangeException(nameof(variable), variable, "Unknown gate variable.")
        };
    }

    private static TurningMemoryDiagnosticSummary BuildDiagnosticSummary(
        string label,
        List<TurningMemoryPointDiagnostic> diagnostics,
        double hsbThreshold,
        double gateThreshold,
        double gateWidth,
        TurningMemoryGateVariable? gateVariable)
    {
        var hsbPoints = diagnostics.Where(x => x.MeanLogGbar > hsbThreshold).ToList();
        var lsbPoints = diagnostics.Where(x => x.MeanLogGbar <= hsbThreshold).ToList();

        double baselineAll = ComputeRmsFromResiduals(diagnostics.Select(x => x.BaselineResidual).ToList());
        double correctedAll = ComputeRmsFromResiduals(diagnostics.Select(x => x.CorrectedResidual).ToList());

        double baselineHsb = ComputeRmsFromResiduals(hsbPoints.Select(x => x.BaselineResidual).ToList());
        double correctedHsb = ComputeRmsFromResiduals(hsbPoints.Select(x => x.CorrectedResidual).ToList());

        double baselineLsb = ComputeRmsFromResiduals(lsbPoints.Select(x => x.BaselineResidual).ToList());
        double correctedLsb = ComputeRmsFromResiduals(lsbPoints.Select(x => x.CorrectedResidual).ToList());

        var perGalaxy = BuildPerGalaxyImprovement(diagnostics);
        var improvedCount = perGalaxy.Count(x => x.DeltaRms > 0);
        var worsenedCount = perGalaxy.Count(x => x.DeltaRms < 0);

        return new TurningMemoryDiagnosticSummary(
            Label: label,
            BaselineRmsAll: baselineAll,
            CorrectedRmsAll: correctedAll,
            DeltaRmsAll: baselineAll - correctedAll,
            BaselineRmsHsb: baselineHsb,
            CorrectedRmsHsb: correctedHsb,
            DeltaRmsHsb: baselineHsb - correctedHsb,
            BaselineRmsLsb: baselineLsb,
            CorrectedRmsLsb: correctedLsb,
            DeltaRmsLsb: baselineLsb - correctedLsb,
            ImprovedGalaxyCount: improvedCount,
            WorsenedGalaxyCount: worsenedCount,
            GateThreshold: gateThreshold,
            GateWidth: gateWidth,
            GateVariable: gateVariable,
            TopImproved: perGalaxy.Where(x => x.DeltaRms > 0).Take(10).ToList(),
            TopWorsened: perGalaxy.Where(x => x.DeltaRms < 0).OrderBy(x => x.DeltaRms).Take(10).ToList());
    }

    private static (double Threshold, double Width) FitSoftGateOnTrain(
        List<TurningResidualRow> trainRows,
        Func<double, double> correctionSelector)
    {
        var brightness = trainRows
            .GroupBy(r => r.GalaxyKey)
            .Select(g => g.Average(x => x.MeanLogGbar))
            .OrderBy(x => x)
            .ToList();

        if (brightness.Count < 5)
            throw new InvalidOperationException("Insufficient train galaxies for soft-gate fitting.");

        double q20 = Percentile(brightness, 0.20);
        double q80 = Percentile(brightness, 0.80);
        if (!double.IsFinite(q20) || !double.IsFinite(q80) || q80 <= q20)
            throw new InvalidOperationException("Invalid brightness distribution for soft-gate fitting.");

        var thresholdGrid = BuildLinearGrid(q20, q80, 13);
        var widthGrid = new[] { 0.03, 0.05, 0.08, 0.12, 0.18, 0.26, 0.38, 0.55 };

        double bestThreshold = thresholdGrid[0];
        double bestWidth = widthGrid[0];
        double bestRms = double.MaxValue;

        foreach (double threshold in thresholdGrid)
        {
            foreach (double width in widthGrid)
            {
                var correctedResiduals = trainRows
                    .Select(r =>
                    {
                        double correction = correctionSelector(r.SignedTurningProxy);
                        double weight = Sigmoid((r.MeanLogGbar - threshold) / width);
                        return r.Residual - (weight * correction);
                    })
                    .ToList();

                double rms = ComputeRmsFromResiduals(correctedResiduals);
                if (rms < bestRms)
                {
                    bestRms = rms;
                    bestThreshold = threshold;
                    bestWidth = width;
                }
            }
        }

        return (bestThreshold, bestWidth);
    }

    private static List<TurningMemoryGalaxyImprovement> BuildPerGalaxyImprovement(
        List<TurningMemoryPointDiagnostic> points)
    {
        return points
            .GroupBy(p => p.GalaxyKey)
            .Select(g =>
            {
                double baseline = ComputeRmsFromResiduals(g.Select(x => x.BaselineResidual).ToList());
                double corrected = ComputeRmsFromResiduals(g.Select(x => x.CorrectedResidual).ToList());
                return new TurningMemoryGalaxyImprovement(
                    GalaxyKey: g.Key,
                    PointCount: g.Count(),
                    BaselineRms: baseline,
                    CorrectedRms: corrected,
                    DeltaRms: baseline - corrected);
            })
            .OrderByDescending(x => x.DeltaRms)
            .ToList();
    }

    private static bool IsHoldoutGalaxy(string galaxyKey, int modulo, int remainder)
    {
        int checksum = galaxyKey.Sum(c => c);
        return checksum % modulo == remainder;
    }

    private static double ComputeDistributedGeometryGbar(
        RarPoint point,
        double minRadiusKpc,
        double spanKpc,
        double innerMeanGbar,
        double outerMeanGbar)
    {
        const double eps = 1e-12;
        const double blendStrength = 0.35;

        double t = spanKpc > eps
            ? Math.Clamp((point.RadiusKpc - minRadiusKpc) / spanKpc, 0.0, 1.0)
            : 0.5;

        double distributedMean = ((1.0 - t) * innerMeanGbar) + (t * outerMeanGbar);
        double effectiveGbar = ((1.0 - blendStrength) * point.GbarMs2) + (blendStrength * distributedMean);
        return Math.Max(effectiveGbar, eps);
    }

    private static double ComputeSmoothDistributedTaktFieldGbar(
        List<RarPoint> orderedPoints,
        int pointIndex,
        double kernelWidthKpc,
        string kernelKind)
    {
        const double eps = 1e-12;
        const double blendStrength = 0.45;

        var point = orderedPoints[pointIndex];
        double width = Math.Max(kernelWidthKpc, 0.05);

        double weightedSum = 0.0;
        double weightNorm = 0.0;
        foreach (var src in orderedPoints)
        {
            double distance = Math.Abs(point.RadiusKpc - src.RadiusKpc);
            double weight = kernelKind.Equals("exponential", StringComparison.OrdinalIgnoreCase)
                ? Math.Exp(-(distance / width))
                : Math.Exp(-0.5 * Math.Pow(distance / width, 2.0));

            weightedSum += weight * src.GbarMs2;
            weightNorm += weight;
        }

        double smoothedSource = weightNorm > eps ? weightedSum / weightNorm : point.GbarMs2;
        double taktPotentialContribution = blendStrength * (smoothedSource - point.GbarMs2);
        return Math.Max(point.GbarMs2 + taktPotentialContribution, eps);
    }

    private static double ComputeMultiCenterToyGeometryGbar(
        List<RarPoint> orderedPoints,
        int pointIndex,
        double spanKpc,
        List<int> peakIndices)
    {
        const double eps = 1e-12;
        const double blendStrength = 0.45;

        var point = orderedPoints[pointIndex];
        if (peakIndices.Count == 0)
            return Math.Max(point.GbarMs2, eps);

        double numerator = 0.0;
        double denominator = 0.0;
        double effectiveSpan = Math.Max(spanKpc, eps);

        foreach (int peakIndex in peakIndices)
        {
            var peakPoint = orderedPoints[peakIndex];
            double distNorm = Math.Abs(point.RadiusKpc - peakPoint.RadiusKpc) / effectiveSpan;
            double weight = 1.0 / (1.0 + (distNorm * distNorm));
            numerator += weight * peakPoint.GbarMs2;
            denominator += weight;
        }

        double multiCenterMean = denominator > eps ? numerator / denominator : point.GbarMs2;
        double effectiveGbar = ((1.0 - blendStrength) * point.GbarMs2) + (blendStrength * multiCenterMean);
        return Math.Max(effectiveGbar, eps);
    }

    private static List<int> FindBaryonicPeakIndices(List<RarPoint> orderedPoints)
    {
        var peaks = new List<int>();
        if (orderedPoints.Count == 0)
            return peaks;

        for (int i = 1; i < orderedPoints.Count - 1; i++)
        {
            double prev = orderedPoints[i - 1].GbarMs2;
            double cur = orderedPoints[i].GbarMs2;
            double next = orderedPoints[i + 1].GbarMs2;

            if (cur >= prev && cur > next)
                peaks.Add(i);
        }

        if (peaks.Count == 0)
        {
            int maxIndex = orderedPoints
                .Select((p, idx) => new { p.GbarMs2, Index = idx })
                .OrderByDescending(x => x.GbarMs2)
                .First()
                .Index;
            peaks.Add(maxIndex);
            return peaks;
        }

        return peaks
            .OrderByDescending(i => orderedPoints[i].GbarMs2)
            .Take(3)
            .OrderBy(i => i)
            .ToList();
    }

    private static GeometryVariantProxyCorrelation BuildGeometryVariantCorrelation(
        List<GeometryVariantEvaluation> entries,
        string variantName,
        Func<GeometryVariantEvaluation, double> deltaSelector)
    {
        var deltas = entries.Select(deltaSelector).ToList();
        var outerInner = entries.Select(x => x.OuterInnerRatio).ToList();
        var gas = entries.Select(x => x.GasDominance).ToList();
        var span = entries.Select(x => x.RadialSpanKpc).ToList();
        var pointCount = entries.Select(x => (double)x.PointCount).ToList();

        double outerInnerPearson = PearsonCorrelation(deltas, outerInner);
        double outerInnerSpearman = SpearmanCorrelation(deltas, outerInner);
        double gasPearson = PearsonCorrelation(deltas, gas);
        double gasSpearman = SpearmanCorrelation(deltas, gas);

        return new GeometryVariantProxyCorrelation(
            VariantName: variantName,
            OuterInnerRatioPearson: outerInnerPearson,
            OuterInnerRatioSpearman: outerInnerSpearman,
            GasDominancePearson: gasPearson,
            GasDominanceSpearman: gasSpearman,
            RadialSpanPearson: PearsonCorrelation(deltas, span),
            RadialSpanSpearman: SpearmanCorrelation(deltas, span),
            PointCountPearson: PearsonCorrelation(deltas, pointCount),
            PointCountSpearman: SpearmanCorrelation(deltas, pointCount),
            CorrelatesWithDiskStructure:
                (double.IsFinite(outerInnerPearson) && Math.Abs(outerInnerPearson) >= 0.20) ||
                (double.IsFinite(outerInnerSpearman) && Math.Abs(outerInnerSpearman) >= 0.20) ||
                (double.IsFinite(gasPearson) && Math.Abs(gasPearson) >= 0.20) ||
                (double.IsFinite(gasSpearman) && Math.Abs(gasSpearman) >= 0.20));
    }

    private static Dictionary<string, double> BuildStructureScores(List<GeometryVariantEvaluation> entries)
    {
        const double eps = 1e-12;

        double meanOuter = entries.Average(x => x.OuterInnerRatio);
        double meanGas = entries.Average(x => x.GasDominance);
        double stdOuter = Math.Sqrt(Math.Max(Variance(entries.Select(x => x.OuterInnerRatio).ToList()), eps));
        double stdGas = Math.Sqrt(Math.Max(Variance(entries.Select(x => x.GasDominance).ToList()), eps));

        return entries.ToDictionary(
            x => x.GalaxyKey,
            x =>
            {
                double zOuter = (x.OuterInnerRatio - meanOuter) / stdOuter;
                double zGas = (x.GasDominance - meanGas) / stdGas;
                return 0.5 * (zOuter + zGas);
            },
            StringComparer.OrdinalIgnoreCase);
    }

    private static double ComputeRmsFromResiduals(List<double> residuals)
    {
        if (residuals == null || residuals.Count == 0)
            return double.NaN;

        return Math.Sqrt(residuals.Average(x => x * x));
    }

    private static double Variance(IReadOnlyList<double> values)
    {
        if (values == null || values.Count == 0)
            return 0.0;

        double mean = values.Average();
        return values.Select(v => (v - mean) * (v - mean)).Average();
    }

    private static double Percentile(List<double> sortedValues, double quantile)
    {
        if (sortedValues == null || sortedValues.Count == 0)
            return double.NaN;

        quantile = Math.Clamp(quantile, 0.0, 1.0);
        double pos = quantile * (sortedValues.Count - 1);
        int lower = (int)Math.Floor(pos);
        int upper = (int)Math.Ceiling(pos);
        if (lower == upper)
            return sortedValues[lower];

        double t = pos - lower;
        return sortedValues[lower] + (t * (sortedValues[upper] - sortedValues[lower]));
    }

    private static List<double> BuildLinearGrid(double min, double max, int count)
    {
        var values = new List<double>();
        if (count <= 1 || max <= min)
        {
            values.Add(min);
            return values;
        }

        double step = (max - min) / (count - 1);
        for (int i = 0; i < count; i++)
        {
            values.Add(min + (i * step));
        }

        return values;
    }

    private static double Sigmoid(double x)
    {
        x = Math.Clamp(x, -50.0, 50.0);
        return 1.0 / (1.0 + Math.Exp(-x));
    }

    private static double PearsonCorrelation(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        if (x == null || y == null || x.Count != y.Count || x.Count < 3)
            return double.NaN;

        double meanX = x.Average();
        double meanY = y.Average();

        double cov = 0.0;
        double varX = 0.0;
        double varY = 0.0;

        for (int i = 0; i < x.Count; i++)
        {
            double dx = x[i] - meanX;
            double dy = y[i] - meanY;
            cov += dx * dy;
            varX += dx * dx;
            varY += dy * dy;
        }

        if (varX <= 0 || varY <= 0)
            return double.NaN;

        return cov / Math.Sqrt(varX * varY);
    }

    private static double SpearmanCorrelation(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        if (x == null || y == null || x.Count != y.Count || x.Count < 3)
            return double.NaN;

        var rx = RankValues(x);
        var ry = RankValues(y);
        return PearsonCorrelation(rx, ry);
    }

    private static List<double> RankValues(IReadOnlyList<double> values)
    {
        var indexed = values
            .Select((v, i) => new { Value = v, Index = i })
            .OrderBy(x => x.Value)
            .ToList();

        var ranks = Enumerable.Repeat(0.0, values.Count).ToList();

        int i = 0;
        while (i < indexed.Count)
        {
            int j = i;
            while (j + 1 < indexed.Count && indexed[j + 1].Value == indexed[i].Value)
                j++;

            double rank = 0.5 * (i + j) + 1.0;
            for (int k = i; k <= j; k++)
            {
                ranks[indexed[k].Index] = rank;
            }

            i = j + 1;
        }

        return ranks;
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