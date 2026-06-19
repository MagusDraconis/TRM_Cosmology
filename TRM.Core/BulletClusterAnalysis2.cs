using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TRM.Core;

public class BulletClusterAnalysis2
{
    public record ClusterKFitResult(string ClusterName, double BestK, double MinError, int PointCount);
    private readonly record struct FitSample(double RadiusSquaredCm2, double Density, double PressureGradient, double ReportedMass);
    private readonly record struct BimodalExportRow(string Cluster, double Z, double MaxGradP, double Improvement);
    private static readonly object LogLock = new();

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

            double rCm = curr.RadiusKpc * PhysicalConstants.KpcToCm;
            double drCm = (next.RadiusKpc - prev.RadiusKpc) * PhysicalConstants.KpcToCm;
            if (drCm == 0)
                continue;

            double dPdr = (next.Pressure - prev.Pressure) / drCm;

            double rho = curr.ElectronDensity * PhysicalConstants.ProtonMass * PhysicalConstants.PlasmaIonizationFactor;
            if (rho <= 0)
                continue;

            curr.CalculatedMass = Math.Abs(-(rCm * rCm / (PhysicalConstants.G * rho)) * dPdr) / PhysicalConstants.M_Solar;
        }
    }

    public double CalculateMassWithTRM(double rCm, double rho, double dPdr, double redshift, double k)
    {
        double syncFactor = 1.0 / (1.0 + k * redshift);
        double G_effective = PhysicalConstants.G * syncFactor;
        return Math.Abs(-(Math.Pow(rCm, 2) / (G_effective * rho)) * dPdr) / PhysicalConstants.M_Solar;
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

            double rCm = curr.RadiusKpc * PhysicalConstants.KpcToCm;
            if (rCm <= 0 || !IsFinite(rCm))
                continue;

            samples.Add(new FitSample(rCm * rCm, rho, dPdr, curr.ReportedMass));
        }

        return samples;
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    private static double CalculateErrorForK(List<FitSample> fitSamples, double zCluster, double k)
    {
        if (fitSamples == null || fitSamples.Count == 0)
            return double.MaxValue;

        if (!IsFinite(zCluster) || !IsFinite(k))
            return double.MaxValue;

        double totalError = 0;
        double denominator = 1.0 + (k * zCluster);
        if (Math.Abs(denominator) < 1e-30)
            return double.MaxValue;

        double syncFactor = 1.0 / denominator;
        double gEffective = PhysicalConstants.G * syncFactor;
        if (gEffective <= 0 || !IsFinite(gEffective))
            return double.MaxValue;

        foreach (var sample in fitSamples)
        {
            double mTheory = Math.Abs(-(sample.RadiusSquaredCm2 / (gEffective * sample.Density)) * sample.PressureGradient) / PhysicalConstants.M_Solar;
            if (!IsFinite(mTheory))
                continue;

            double squaredError = Math.Pow(mTheory - sample.ReportedMass, 2);
            if (!IsFinite(squaredError))
                continue;

            totalError += squaredError;
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

            double errorBaseline = CalculateErrorForK(fitSamples, zCluster, baselineK);
            if (errorBaseline > 1e300 || double.IsNaN(errorBaseline)) errorBaseline = 1e300;

            // Evaluate universal K(z)
            double universalK = C * Math.Pow(zCluster, alpha);

            // Apply geometric damping to universal K
            double ellipticity = GetClusterEllipticity(clusterName);
            double geometricK = universalK * (1.0 - beta * ellipticity);
            if (geometricK < baselineK) geometricK = baselineK; // Guard against non-physical sign inversions

            double errorUniversal = CalculateErrorForK(fitSamples, zCluster, geometricK);
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

            double errorBaseline = CalculateErrorForK(fitSamples, zCluster, baselineK);
            if (double.IsNaN(errorBaseline) || errorBaseline > 1e300) errorBaseline = 1e300;

            double errorFinal = CalculateErrorForK(fitSamples, zCluster, finalK);
            if (double.IsNaN(errorFinal) || errorFinal > 1e300) errorFinal = 1e300;

            // Oracle comparison also uses the geometry-corrected value
            double oracleError = CalculateErrorForK(fitSamples, zCluster, geometricK);
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

            double errorBaseline = CalculateErrorForK(fitSamples, zCluster, baselineK);
            if (double.IsNaN(errorBaseline) || errorBaseline > 1e300) errorBaseline = 1e300;

            double oracleUniversalError = CalculateErrorForK(fitSamples, zCluster, geometricK);
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
}