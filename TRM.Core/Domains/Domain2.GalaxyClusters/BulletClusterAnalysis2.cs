using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TRM.Core.Shared;

namespace TRM.Core;

public class BulletClusterAnalysis2
{
    public record ClusterKFitResult(string ClusterName, double BestK, double MinError, int PointCount);
    private readonly record struct FitSample(double RadiusSquaredCm2, double Density, double PressureGradient, double ReportedMass);
    private readonly record struct BimodalExportRow(string Cluster, double Z, double MaxGradP, double Improvement);
    private static readonly object LogLock = new();
    private readonly TrmDistanceMapper _mapper;

    public BulletClusterAnalysis2()
    {
        var scaling = TrmCosmologyParameters.Current();
        _mapper = new TrmDistanceMapper(scaling);
    }


    // Global registry of cluster ellipticities (morphological asymmetry) from Chandra data
    public Dictionary<string, double> ClusterEllipticities { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        // Known calibration extremes:
        { "1E0657_56", 0.48 },    // Bullet Cluster (highly disturbed)
        { "ABELL_2744", 0.55 },   // Pandora's Cluster (major multi-merger)
        { "ABELL_3391", 0.45 },   // strongly asymmetric system
        { "AC_114", 0.42 },       // known merger
        { "ABELL_2029", 0.04 },   // very symmetric, relaxed cool-core cluster
        { "ABELL_1060", 0.05 }    // near-spherical
    };

    // Helper to resolve ellipticity with fallback
    private double GetClusterEllipticity(string clusterName)
    {
        if (ClusterEllipticities.TryGetValue(clusterName, out double ellipticity))
        {
            return ellipticity;
        }
        return 0.15; // Phenomenological global average for unlisted clusters
    }

    public List<AcceptShell> AnalyzeFromComaFile(string filePath, string clusterName = "1E0657_56")
    {
        var shells = LoadClusterShells(filePath, clusterName);
        CalculateHydrostaticMass(shells);
        return shells;
    }

    public List<AcceptShell> LoadClusterShells(string filePath, string clusterName)
    {
        var allClusters = LoadAllClusterShells(filePath);
        return allClusters.TryGetValue(clusterName, out var shells)
            ? shells
            : new List<AcceptShell>();
    }

    public Dictionary<string, List<AcceptShell>> LoadAllClusterShells(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}.");

        var clusterDb = new Dictionary<string, List<AcceptShell>>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in File.ReadLines(filePath))
        {
            if (string.IsNullOrWhiteSpace(rawLine))
                continue;

            var line = rawLine.Trim();
            if (line.StartsWith("#", StringComparison.Ordinal))
                continue;

            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (!TryParseAcceptShell(parts, out var clusterName, out var shell))
                continue;

            if (!clusterDb.TryGetValue(clusterName, out var shells))
            {
                shells = new List<AcceptShell>();
                clusterDb[clusterName] = shells;
            }

            shells.Add(shell);
        }

        foreach (var key in clusterDb.Keys.ToList())
        {
            clusterDb[key] = clusterDb[key]
                .OrderBy(s => s.RadiusKpc)
                .ToList();
        }

        return clusterDb;
    }

    private static bool TryParseAcceptShell(string[] parts, out string clusterName, out AcceptShell shell)
    {
        clusterName = string.Empty;
        shell = null!;

        if (parts.Length < 12)
            return false;

        clusterName = parts[0];

        if (!double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var rinMpc) ||
            !double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var nelec) ||
            !double.TryParse(parts[8], NumberStyles.Float, CultureInfo.InvariantCulture, out var pitpl) ||
            !double.TryParse(parts[11], NumberStyles.Float, CultureInfo.InvariantCulture, out var mgrav))
        {
            return false;
        }

        shell = new AcceptShell
        {
            RadiusKpc = rinMpc * 1000.0,
            ElectronDensity = nelec,
            Pressure = pitpl,
            ReportedMass = mgrav
        };

        return true;
    }

    public void CalculateHydrostaticMass(List<AcceptShell> shells)
    {
        if (shells == null || shells.Count < 3)
            return;

        for (int i = 1; i < shells.Count - 1; i++)
        {
            var prev = shells[i - 1];
            var curr = shells[i];
            var next = shells[i + 1];


            double rCm = curr.RadiusKpc_TRM > 0
                ? curr.RadiusKpc_TRM * PhysicalConstants.KpcToCm
                : curr.RadiusKpc * PhysicalConstants.KpcToCm;

            double drCm = ((next.RadiusKpc_TRM > 0 ? next.RadiusKpc_TRM : next.RadiusKpc)
             - (prev.RadiusKpc_TRM > 0 ? prev.RadiusKpc_TRM : prev.RadiusKpc))
             * PhysicalConstants.KpcToCm;

            if (drCm == 0)
                continue;

            double dPdr = (next.Pressure - prev.Pressure) / drCm;

            double rho = curr.ElectronDensity * PhysicalConstants.ProtonMass * PhysicalConstants.PlasmaIonizationFactor;
            if (rho <= 0)
                continue;

            curr.CalculatedMass = Math.Abs(-(rCm * rCm / (PhysicalConstants.G * rho)) * dPdr) / PhysicalConstants.M_Solar;
        }
    }

    public double CalculateMassUnified(double rCm, double rho, double dPdr, double redshift, double k)
    {
        if(rho <= 0 || !IsFinite(dPdr))
            return double.MaxValue;

        // --- 1. Dynamikgewicht (kein Switch mehr!) ---
        double referenceGrad = 1e-33;

        double turbulence = Math.Abs(dPdr) / referenceGrad;

        // glatte Übergangsfunktion
        double w = 1.0 / (1.0 + 2.0 * turbulence);//double w = 1.0 / (1.0 + turbulence);
        // ruhig → w≈1, turbulent → w≈0

        // --- 2. TRM-Kopplung ---
        double trmFactor = 1.0 / (1.0 + k * redshift);

        // --- 3. GEMISCHTES G ---
        double effectiveFactor = (1.0 - w) + w * trmFactor;

        double G_effective = PhysicalConstants.G * effectiveFactor;

        // --- 4. Masse ---
        return Math.Abs(-(Math.Pow(rCm, 2) / (G_effective * rho)) * dPdr)
               / PhysicalConstants.M_Solar;
    }

    private static List<FitSample> BuildFitSamples(List<AcceptShell> shells)
    {
        var samples = new List<FitSample>();
        if (shells == null || shells.Count < 3)
            return samples;

        for (int i = 1; i < shells.Count - 1; i++)
        {
            var prev = shells[i - 1];
            var curr = shells[i];
            var next = shells[i + 1];

            if (curr.ReportedMass <= 0 ||
                !IsFinite(curr.ReportedMass) ||
                !IsFinite(curr.RadiusKpc) ||
                !IsFinite(curr.ElectronDensity) ||
                !IsFinite(prev.RadiusKpc) || !IsFinite(next.RadiusKpc) ||
                !IsFinite(prev.Pressure) || !IsFinite(next.Pressure))
            {
                continue;
            }

            double drCm = (next.RadiusKpc - prev.RadiusKpc) * PhysicalConstants.KpcToCm;
            if (Math.Abs(drCm) < 1e-30)
                continue;

            double dPdr = (next.Pressure - prev.Pressure) / drCm;
            if (!IsFinite(dPdr))
                continue;

            double rho = curr.ElectronDensity * PhysicalConstants.ProtonMass * PhysicalConstants.PlasmaIonizationFactor;
            if (rho <= 0 || !IsFinite(rho))
                continue;



            double radius = curr.RadiusKpc_TRM > 0 ? curr.RadiusKpc_TRM : curr.RadiusKpc;
            double rCm = radius * PhysicalConstants.KpcToCm;


            if(rCm <= 0 || !IsFinite(rCm))
                continue;

            samples.Add(new FitSample(rCm * rCm, rho, dPdr, curr.ReportedMass));
        }

        return samples;
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    private static double CalculateErrorUnified(List<FitSample> samples, double zCluster, double k)
    {
        if(samples == null || samples.Count == 0)
            return double.MaxValue;

        double totalError = 0;

        foreach(var s in samples)
        {
            if(!IsFinite(s.PressureGradient) || !IsFinite(s.Density))
                continue;

            double referenceGrad = 1e-33;
            double turbulence = Math.Abs(s.PressureGradient) / referenceGrad;

            double w = 1.0 / (1.0 + 2.0 * turbulence);

            double trmFactor = 1.0 / (1.0 + k * zCluster);
            double effectiveFactor = (1.0 - w) + w * trmFactor;

            double G_eff = PhysicalConstants.G * effectiveFactor;


            double shear = Math.Abs(s.PressureGradient - samples.Average(p => p.PressureGradient));

            double correction = 1.0 + shear / referenceGrad;

            double effectiveGradient = s.PressureGradient / correction;


            double mTheory =
                Math.Abs(-(s.RadiusSquaredCm2 / (G_eff * s.Density)) * effectiveGradient)
                / PhysicalConstants.M_Solar;

            if(!IsFinite(mTheory))
                continue;

            double err = Math.Pow(mTheory - s.ReportedMass, 2);

            if(!IsFinite(err))
                continue;

            totalError += err;
        }

        return totalError > 0 ? totalError : double.MaxValue;
    }

    // Evaluates the bimodal theory including geometric damping factor beta
    public void EvaluateBimodalTheoryForAllClusters(
        string profileFilePath,
        string redshiftFilePath,
        double C = 1.3195,
        double alpha = -0.7589,
        double baselineK = 0.1,
        double beta = 0.0) // beta = 0 reproduces the V2.1 behavior
    {
        var allClusters = LoadAllClusterShells(profileFilePath);
        var redshifts = LoadClusterRedshifts(redshiftFilePath);

        Console.WriteLine("\n=======================================================================================");
        Console.WriteLine($"   BIMODAL THEORY CHECK (WITH GEOMETRY): Newton vs. TRM K(z, \u03b5)");
        Console.WriteLine("=======================================================================================");
        Console.WriteLine("ClusterName      | Gruppe | Angewandtes K | Bester Fehler | Improvement vs Baseline");
        Console.WriteLine("---------------------------------------------------------------------------------------");

        int countGroupA = 0;
        int countGroupB = 0;
        double totalBaselineErrorSum = 0;
        double totalBimodalErrorSum = 0;
        double sumImprovement = 0;

        foreach (var entry in allClusters.OrderBy(k => k.Key))
        {
            string clusterName = entry.Key;
            var shells = entry.Value;

            if (!redshifts.TryGetValue(clusterName, out var zCluster) || zCluster <= 0) continue;

            var fitSamples = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Where(BuildFitSamples(shells), sample =>
            {
                double mHydroStandard = Math.Abs(-(sample.RadiusSquaredCm2 / (PhysicalConstants.G * sample.Density)) * sample.PressureGradient) / PhysicalConstants.M_Solar;
                return !double.IsNaN(mHydroStandard) && !double.IsInfinity(mHydroStandard) && mHydroStandard < 1e16 && mHydroStandard > 0;
            }));

            if (fitSamples.Count == 0) continue;

            double errorBaseline = CalculateErrorUnified(fitSamples, zCluster, baselineK);
            if (errorBaseline > 1e300 || double.IsNaN(errorBaseline)) errorBaseline = 1e300;

            // Evaluate universal K(z)
            double universalK = C * Math.Pow(zCluster, alpha);

            // Apply geometric damping to universal K
            double ellipticity = GetClusterEllipticity(clusterName);
            double geometricK = universalK * (1.0 - beta * ellipticity);
            if (geometricK < baselineK) geometricK = baselineK; // Guard against non-physical sign inversions

            double errorUniversal = CalculateErrorUnified(fitSamples, zCluster, geometricK);
            if (errorUniversal > 1e300 || double.IsNaN(errorUniversal)) errorUniversal = 1e300;

            string gruppe;
            double finalK;
            double finalError;
            double improvement = 1.0;

            if (errorUniversal < errorBaseline * 0.95)
            {
                gruppe = "B (Clockwork)";
                finalK = geometricK;
                finalError = errorUniversal;
                countGroupB++;
            }
            else
            {
                gruppe = "A (Newton)   ";
                finalK = baselineK;
                finalError = errorBaseline;
                countGroupA++;
            }

            if (finalError > 0 && finalError < 1e300)
            {
                improvement = errorBaseline / finalError;
            }

            sumImprovement += improvement;

            if (errorBaseline < 1e300 && finalError < 1e300)
            {
                totalBaselineErrorSum += errorBaseline;
                totalBimodalErrorSum += finalError;
            }

            Console.WriteLine($"{clusterName,-16} | {gruppe} | {finalK,13:F4} | {finalError,13:E2} | {improvement:F2}x");
        }

        int totalClusters = countGroupA + countGroupB;
        double avgImprovement = sumImprovement / totalClusters;
        double globalErrorReduction = (totalBaselineErrorSum > 0 && totalBimodalErrorSum > 0)
                                      ? (totalBaselineErrorSum / totalBimodalErrorSum) : 0;

        Console.WriteLine("=======================================================================================");
        Console.WriteLine($"SUMMARY OF GEOMETRY-OPTIMIZED THEORY:");
        Console.WriteLine($"Total analyzed clusters:          {totalClusters}");
        Console.WriteLine($"Group A (classical Newton):       {countGroupA} clusters ({((double)countGroupA / totalClusters) * 100:F1}%)");
        Console.WriteLine($"Group B (Clockwork active):       {countGroupB} clusters ({((double)countGroupB / totalClusters) * 100:F1}%)");
        Console.WriteLine($"Average improvement:              {avgImprovement:F2}x per cluster");
        Console.WriteLine($"Global total error reduction:     {globalErrorReduction:F2}x vs baseline");
        Console.WriteLine("=======================================================================================\n");
    }

    // Physical trigger now includes beta in the decision path
    public void EvaluatePhysicsDrivenBimodalTheory(
            Dictionary<string, List<AcceptShell>> allClusters,
            Dictionary<string, double> redshifts,
            double C = 1.3195,
            double alpha = -0.7589,
            double baselineK = 0.1,
            double pressureThreshold = 6.0e-34,
            double beta = 0.0, // Geometric damping coefficient
            string resultsCsvPath = "results.csv")
    {
        Console.WriteLine("\n=========================================================================================================");
        Console.WriteLine($"   FINAL EVIDENCE V2.2: TRM vs. Newton (Pressure trigger: {pressureThreshold:E2} | Beta: {beta:F2})");
        Console.WriteLine("=========================================================================================================");
        Console.WriteLine(String.Format("{0,-18} | {1,-6} | {2,-11} | {3,-13} | {4,-8} | {5,-12} | {6,-12} | {7}",
            "Cluster", "z", "Max Grad(P)", "Decision", "K-value", "Baseline-Err", "Final-Error", "Improvement"));
        Console.WriteLine(new String('-', 108));

        int countNewton = 0;
        int countTRM = 0;
        int successfulPredictions = 0;
        var exportRows = new List<BimodalExportRow>();

        double sumLogImprovement = 0;
        int validImprovementCount = 0;

        foreach (var entry in allClusters.OrderBy(k => k.Key))
        {
            string clusterName = entry.Key;
            var shells = entry.Value;

            if (!redshifts.TryGetValue(clusterName, out var zCluster) || zCluster <= 0) continue;

            var fitSamples = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Where(BuildFitSamples(shells), sample =>
            {
                double mHydroStandard = Math.Abs(-(sample.RadiusSquaredCm2 / (PhysicalConstants.G * sample.Density)) * sample.PressureGradient) / PhysicalConstants.M_Solar;
                return !double.IsNaN(mHydroStandard) && !double.IsInfinity(mHydroStandard) && mHydroStandard < 1e16 && mHydroStandard > 0;
            }));

            if (fitSamples.Count == 0) continue;

            double maxPressureGrad = System.Linq.Enumerable.Max(fitSamples, s => Math.Abs(s.PressureGradient));
            bool useClockwork = maxPressureGrad < pressureThreshold;

            // Load geometric modification
            double ellipticity = GetClusterEllipticity(clusterName);
            double universalK = C * Math.Pow(zCluster, alpha);
            double geometricK = universalK * (1.0 - beta * ellipticity);
            if (geometricK < baselineK) geometricK = baselineK;

            double finalK = useClockwork ? geometricK : baselineK;
            string decisionStr = useClockwork ? "TRM" : "Newton";

            if (useClockwork) countTRM++; else countNewton++;

            double errorBaseline = CalculateErrorUnified(fitSamples, zCluster, baselineK);
            if (double.IsNaN(errorBaseline) || errorBaseline > 1e300) errorBaseline = 1e300;

            double errorFinal = CalculateErrorUnified(fitSamples, zCluster, finalK);
            if (double.IsNaN(errorFinal) || errorFinal > 1e300) errorFinal = 1e300;

            // Oracle comparison also uses the geometry-corrected value
            double oracleError = CalculateErrorUnified(fitSamples, zCluster, geometricK);
            bool truthNeedsClockwork = (oracleError < errorBaseline * 0.95);
            if (useClockwork == truthNeedsClockwork) successfulPredictions++;

            double improvement = 1.0;
            if (errorFinal > 0 && errorFinal < 1e300 && errorBaseline < 1e300)
            {
                improvement = errorBaseline / errorFinal;
                sumLogImprovement += Math.Log(improvement);
                validImprovementCount++;
            }

            Console.WriteLine(String.Format("{0,-18} | {1:F4} | {2,11:E2} | {3,-13} | {4,8:F4} | {5,12:E2} | {6,12:E2} | {7:F2}x",
                clusterName, zCluster, maxPressureGrad, decisionStr, finalK, errorBaseline, errorFinal, improvement));

            exportRows.Add(new BimodalExportRow(clusterName, zCluster, maxPressureGrad, improvement));
        }

        int totalClusters = countNewton + countTRM;
        double predictionAccuracy = ((double)successfulPredictions / totalClusters) * 100;
        double geometricMeanImprovement = validImprovementCount > 0 ? Math.Exp(sumLogImprovement / validImprovementCount) : 1.0;

        Console.WriteLine(new String('-', 108));
        Console.WriteLine($"PUBLICATION SUMMARY:");
        Console.WriteLine($"Classified as Newton:             {countNewton} clusters");
        Console.WriteLine($"Classified as TRM:                {countTRM} clusters");
        Console.WriteLine($"Prediction accuracy:              {predictionAccuracy:F1}% (trigger selected correct physics)");
        Console.WriteLine($"Global average improvement:       {geometricMeanImprovement:F2}x per cluster (geometric mean)");
        Console.WriteLine("=========================================================================================================\n");

        ExportBimodalResultsToCsv(exportRows, resultsCsvPath);
    }

    // Silent prediction test for 2D grid (threshold and beta)
    public double RunSilentPredictionTest(
        Dictionary<string, List<AcceptShell>> allClusters,
        Dictionary<string, double> redshifts,
        double C,
        double alpha,
        double baselineK,
        double pressureThreshold,
        double beta)
    {
        int countNewton = 0;
        int countTRM = 0;
        int successfulPredictions = 0;

        foreach (var entry in allClusters)
        {
            string clusterName = entry.Key;
            var shells = entry.Value;

            if (!redshifts.TryGetValue(clusterName, out var zCluster) || zCluster <= 0) continue;

            var fitSamples = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Where(BuildFitSamples(shells), sample =>
            {
                double mHydroStandard = Math.Abs(-(sample.RadiusSquaredCm2 / (PhysicalConstants.G * sample.Density)) * sample.PressureGradient) / PhysicalConstants.M_Solar;
                return !double.IsNaN(mHydroStandard) && !double.IsInfinity(mHydroStandard) && mHydroStandard < 1e16 && mHydroStandard > 0;
            }));

            if (fitSamples.Count == 0) continue;

            double maxPressureGrad = System.Linq.Enumerable.Max(fitSamples, s => Math.Abs(s.PressureGradient));
            bool useClockwork = maxPressureGrad < pressureThreshold;

            double ellipticity = GetClusterEllipticity(clusterName);
            double universalK = C * Math.Pow(zCluster, alpha);
            double geometricK = universalK * (1.0 - beta * ellipticity);
            if (geometricK < baselineK) geometricK = baselineK;

            if (useClockwork) countTRM++; else countNewton++;

            double errorBaseline = CalculateErrorUnified(fitSamples, zCluster, baselineK);
            if (double.IsNaN(errorBaseline) || errorBaseline > 1e300) errorBaseline = 1e300;

            double oracleUniversalError = CalculateErrorUnified(fitSamples, zCluster, geometricK);
            bool truthNeedsClockwork = (oracleUniversalError < errorBaseline * 0.95);

            if (useClockwork == truthNeedsClockwork)
            {
                successfulPredictions++;
            }
        }

        int totalClusters = countNewton + countTRM;
        return totalClusters > 0 ? ((double)successfulPredictions / totalClusters) * 100 : 0.0;
    }

    // 2D optimization sweep to balance trigger and geometry
    public void FindBestPhysicalThresholdAndBeta(
        Dictionary<string, List<AcceptShell>> allClusters,
        Dictionary<string, double> redshifts)
    {
        Console.WriteLine("\n==========================================================================");
        Console.WriteLine(" Starting 2D parameter sweep: optimizing pressure trigger and beta");
        Console.WriteLine("==========================================================================");

        double bestThreshold = 0;
        double bestBeta = 0;
        double maxAccuracy = 0;

        // Outer loop: pressure trigger (1.0E-34 to 2.0E-32)
        for (double t = 1.0e-34; t <= 2.0e-32; t += 2.0e-34)
        {
            // Inner loop: beta (geometric damping from 0.0 to 1.5)
            for (double b = 0.0; b <= 1.5; b += 0.05)
            {
                double accuracy = RunSilentPredictionTest(allClusters, redshifts, 1.3195, -0.7589, 0.1, t, b);

                if (accuracy > maxAccuracy)
                {
                    maxAccuracy = accuracy;
                    bestThreshold = t;
                    bestBeta = b;
                }
            }
        }

        Console.WriteLine($"\nBest optimum found.");
        Console.WriteLine($"-> Best pressure threshold: {bestThreshold:E2}");
        Console.WriteLine($"-> Best geometric coefficient (beta): {bestBeta:F2}");
        Console.WriteLine($"-> Maximum bimodal prediction accuracy: {maxAccuracy:F1}%");
        Console.WriteLine("==========================================================================\n");
    }

    private static void ExportBimodalResultsToCsv(IEnumerable<BimodalExportRow> rows, string filePath)
    {
        using var writer = new StreamWriter(filePath);
        writer.WriteLine("Cluster,z,MaxGradP,Improvement");
        foreach (var row in rows)
        {
            writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0},{1:F6},{2:E6},{3:F6}", row.Cluster, row.Z, row.MaxGradP, row.Improvement));
        }
    }

    public Dictionary<string, double> LoadClusterRedshifts(string filePath)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException($"File not found: {filePath}.");
        var redshifts = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in File.ReadLines(filePath))
        {
            if (string.IsNullOrWhiteSpace(rawLine)) continue;
            var line = rawLine.Trim();
            if (line.StartsWith("#", StringComparison.Ordinal)) continue;
            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4) continue;
            if (!double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var z)) continue;
            if (!redshifts.ContainsKey(parts[0])) redshifts[parts[0]] = z;
        }
        return redshifts;
    }
    public void ApplyTrmGeometry(
    Dictionary<string, List<AcceptShell>> clusters,
    Dictionary<string, double> redshifts,
    TrmDistanceMapper mapper)
    {
        foreach(var (clusterName, shells) in clusters)
        {
            if(!redshifts.TryGetValue(clusterName, out double z))
                continue;

            double dA_TRM = mapper.CalculateTrmAngularDiameterDistance(z);
            double dA_GR = LambdaCdMHelper.CalculateAngularDiameterDistance(z);

            double f = dA_TRM / dA_GR;

            foreach(var shell in shells)
            {
                // ✅ Radius transformieren
                shell.RadiusKpc_TRM = shell.RadiusKpc * f;

                // ✅ Density transformieren
                shell.ElectronDensity /= Math.Pow(f, 3);

                // ✅ Pressure transformieren
                shell.Pressure /= Math.Pow(f, 3);

                // ✅ Mass skalieren
                shell.ReportedMass *= f;
            }
            Console.WriteLine($"Cluster {clusterName}: f(z) = {f}");
        }
    }
    public List<ClusterDiagnostic> RunClusterDiagnostics(
    Dictionary<string, List<AcceptShell>> allClusters,
    Dictionary<string, double> redshifts,
    double pressureThreshold,
    double C,
    double alpha,
    double beta,
    double baselineK = 0.1)
    {
        var results = new List<ClusterDiagnostic>();

        foreach(var entry in allClusters)
        {
            string clusterName = entry.Key;
            var shells = entry.Value;

            if(!redshifts.TryGetValue(clusterName, out double z) || z <= 0)
                continue;

            var samples = BuildFitSamples(shells);
            if(samples.Count == 0)
                continue;

            double maxGradP = samples.Max(s => Math.Abs(s.PressureGradient));

            double referenceGrad = 1e-33;

            double gradVariance = ComputeGradientVariance(samples);
            double inertial = ComputeInertialProxy(samples);

            double meanGrad = samples.Average(s => s.PressureGradient);

            double anisotropy = samples
                .Average(s => Math.Abs(s.PressureGradient - meanGrad))
                / (Math.Abs(meanGrad) + 1e-40);

            double turbulence = maxGradP / referenceGrad;
            double shear = gradVariance / referenceGrad;
            double inertialNorm = inertial / (referenceGrad + 1e-40);

            double dynamicFactor =
                turbulence
                + 0.5 * shear
                + 0.5 * anisotropy
                + 0.5 * inertialNorm;

            double weight = 1.0 / (1.0 + 2.0 * dynamicFactor);
            string regime = ClassifyRegime(weight);


            double dA_TRM = _mapper.CalculateTrmAngularDiameterDistance(z);
            double dA_GR = LambdaCdMHelper.CalculateAngularDiameterDistance(z);

            double fz = dA_TRM / dA_GR;            

            double universalK = C * Math.Pow(z, alpha);
            double ellipticity = GetClusterEllipticity(clusterName);


            string morphologyClass = ClassifyEffectiveMorphology(
                weight,
                turbulence,
                shear,
                anisotropy,
                inertialNorm,
                ellipticity);


            double geometricK = universalK * (1.0 - beta * ellipticity);

            if(geometricK < baselineK)
                geometricK = baselineK;

            double chosenK = baselineK * (1.0 - weight) + geometricK * weight;

            double errorBaseline = CalculateErrorUnified(samples, z, baselineK);
            double errorFinal = CalculateErrorUnified(samples, z, chosenK);

            double improvement = (errorFinal > 0) ? errorBaseline / errorFinal : 1.0;

            string diagnosis = DiagnoseCluster(fz, maxGradP, improvement, weight);



            results.Add(new ClusterDiagnostic(
                clusterName,
                z,
                fz,
                maxGradP,
                improvement,
                diagnosis,
                weight,
                turbulence,
                shear,
                anisotropy,
                inertialNorm,
                dynamicFactor,
                ellipticity,
                morphologyClass,
                regime));
        }

        return results;
    }
    private string DiagnoseCluster(double fz, double gradP, double improvement, double weight)
    {
        double geometryLeverage = 1.0 - fz;

        bool isTrmDominant = weight > 0.7;
        bool isNewtonDominant = weight < 0.3;

        const double lowGeometryLimit = 0.07;
        const double highTurbulenceLimit = 1e-33;

        // starke Verbesserung
        if(improvement > 1.5)
            return "TRM highly effective";

        // moderate Verbesserung
        if(improvement > 1.1)
            return "TRM beneficial";

        // neutral
        if(improvement > 0.95)
            return "Neutral regime";

        // schlechte Performance genauer unterscheiden
        if(improvement < 0.8)
        {
            if(gradP > highTurbulenceLimit)
                return "High turbulence (dynamics dominated)";

            if(geometryLeverage < lowGeometryLimit)
                return "Low geometry leverage (f too close to 1)";

            if(isNewtonDominant)
                return "Correctly Newton-dominant";

            if(isTrmDominant)
                return "TRM overcorrection";

            return "Mixed instability";
        }

        return "Mixed/unclear regime";
    }

    private static double ComputeGradientVariance(List<FitSample> samples)
    {
        var grads = samples.Select(s => s.PressureGradient).ToList();

        double mean = grads.Average();
        double variance = grads.Select(g => Math.Pow(g - mean, 2)).Average();

        return Math.Sqrt(variance);
    }
    private static double ComputeInertialProxy(List<FitSample> samples)
    {
        if(samples.Count < 2)
            return 0;

        // nach Radius sortieren (wichtig!)
        var ordered = samples.OrderBy(s => s.RadiusSquaredCm2).ToList();

        double totalChange = 0;
        int count = 0;

        for(int i = 1; i < ordered.Count; i++)
        {
            double gradPrev = ordered[i - 1].PressureGradient;
            double gradCurr = ordered[i].PressureGradient;

            double delta = Math.Abs(gradCurr - gradPrev);

            totalChange += delta;
            count++;
        }

        return count > 0 ? totalChange / count : 0;
    }

    private static string ClassifyEffectiveMorphology(
        double weight,
        double turbulence,
        double shear,
        double anisotropy,
        double inertialNorm,
        double ellipticity)
    {
        bool highDynamics =
            turbulence > 2.0 ||
            shear > 2.0 ||
            anisotropy > 2.0 ||
            inertialNorm > 2.0;

        bool veryRelaxed =
            turbulence < 0.7 &&
            shear < 0.7 &&
            anisotropy < 1.0 &&
            inertialNorm < 0.7 &&
            ellipticity < 0.25;

        if(weight > 0.65 && veryRelaxed)
            return "Relaxed / TRM-like";

        if(highDynamics || weight < 0.30)
            return "Disturbed / Newton-like";

        if(ellipticity > 0.35)
            return "Elliptic / asymmetric";

        return "Transitional / mixed";
    }

    public static string ClassifyRegime(double weight)
    {
        if(weight > 0.7)
            return "TRM-dominant";

        if(weight < 0.3)
            return "Newton-dominant";

        return "Mixed";
    }

    public void PrintWeightSystematics(List<ClusterDiagnostic> diagnostics)
    {
        Console.WriteLine();
        Console.WriteLine("--- WEIGHT SYSTEMATICS: z, f(z), morphology ---");

        PrintCorrelationBlock(diagnostics);

        Console.WriteLine();
        Console.WriteLine("--- WEIGHT BY REDSHIFT BIN ---");

        PrintBin(
            diagnostics.Where(d => d.Z < 0.1),
            "low z < 0.1");

        PrintBin(
            diagnostics.Where(d => d.Z >= 0.1 && d.Z < 0.3),
            "mid z 0.1–0.3");

        PrintBin(
            diagnostics.Where(d => d.Z >= 0.3 && d.Z < 0.6),
            "high z 0.3–0.6");

        PrintBin(
            diagnostics.Where(d => d.Z >= 0.6),
            "very high z >= 0.6");

        Console.WriteLine();
        Console.WriteLine("--- WEIGHT BY EFFECTIVE MORPHOLOGY ---");

        foreach(var group in diagnostics.GroupBy(d => d.MorphologyClass).OrderBy(g => g.Key))
        {
            PrintBin(group, group.Key);
        }
    }
    private static void PrintBin(IEnumerable<ClusterDiagnostic> items, string label)
    {
        var list = items.ToList();

        if(list.Count == 0)
        {
            Console.WriteLine($"{label,-28} | n=0");
            return;
        }

        double avgWeight = list.Average(d => d.Weight);
        double avgImprovement = list.Average(d => d.Improvement);
        double avgZ = list.Average(d => d.Z);
        double avgFz = list.Average(d => d.Fz);
        double avgDyn = list.Average(d => d.DynamicFactor);

        Console.WriteLine(
            $"{label,-28} | n={list.Count,3} | " +
            $"<z>={avgZ:F3} | <f>={avgFz:F3} | " +
            $"<w>={avgWeight:F3} | <dyn>={avgDyn:F3} | " +
            $"<imp>={avgImprovement:F2}x"
        );
    }
    private static void PrintCorrelationBlock(List<ClusterDiagnostic> diagnostics)
    {
        Console.WriteLine();
        Console.WriteLine("--- PEARSON CORRELATIONS ---");

        Console.WriteLine($"corr(z, weight):             {Pearson(diagnostics.Select(d => d.Z), diagnostics.Select(d => d.Weight)):F3}");
        Console.WriteLine($"corr(fz, weight):            {Pearson(diagnostics.Select(d => d.Fz), diagnostics.Select(d => d.Weight)):F3}");
        Console.WriteLine($"corr(dynamicFactor, weight): {Pearson(diagnostics.Select(d => d.DynamicFactor), diagnostics.Select(d => d.Weight)):F3}");
        Console.WriteLine($"corr(turbulence, weight):    {Pearson(diagnostics.Select(d => d.Turbulence), diagnostics.Select(d => d.Weight)):F3}");
        Console.WriteLine($"corr(shear, weight):         {Pearson(diagnostics.Select(d => d.Shear), diagnostics.Select(d => d.Weight)):F3}");
        Console.WriteLine($"corr(anisotropy, weight):    {Pearson(diagnostics.Select(d => d.Anisotropy), diagnostics.Select(d => d.Weight)):F3}");
        Console.WriteLine($"corr(inertial, weight):      {Pearson(diagnostics.Select(d => d.InertialNorm), diagnostics.Select(d => d.Weight)):F3}");
        Console.WriteLine($"corr(ellipticity, weight):   {Pearson(diagnostics.Select(d => d.Ellipticity), diagnostics.Select(d => d.Weight)):F3}");
        Console.WriteLine($"corr(improvement, weight):   {Pearson(diagnostics.Select(d => d.Improvement), diagnostics.Select(d => d.Weight)):F3}");
    }
    private static double Pearson(IEnumerable<double> xs, IEnumerable<double> ys)
    {
        var x = xs.ToList();
        var y = ys.ToList();

        if(x.Count != y.Count || x.Count < 2)
            return double.NaN;

        double meanX = x.Average();
        double meanY = y.Average();

        double numerator = 0.0;
        double denomX = 0.0;
        double denomY = 0.0;

        for(int i = 0; i < x.Count; i++)
        {
            double dx = x[i] - meanX;
            double dy = y[i] - meanY;

            numerator += dx * dy;
            denomX += dx * dx;
            denomY += dy * dy;
        }

        double denom = Math.Sqrt(denomX * denomY);

        if(denom <= 0 || double.IsNaN(denom))
            return double.NaN;

        return numerator / denom;
    }
}