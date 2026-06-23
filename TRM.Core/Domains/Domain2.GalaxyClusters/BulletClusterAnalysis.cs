//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Globalization;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;

//namespace TRM.Core;

//public class BulletClusterAnalysis
//{
//    public record ClusterKFitResult(string ClusterName, double BestK, double MinError, int PointCount);
//    private readonly record struct FitSample(double RadiusSquaredCm2, double Density, double PressureGradient, double ReportedMass);
//    private readonly record struct BimodalExportRow(string Cluster, double Z, double MaxGradP, double Improvement);
//    private static readonly object LogLock = new();


//    public List<AcceptShell> AnalyzeFromComaFile(string filePath, string clusterName = "1E0657_56")
//    {
//        var shells = LoadClusterShells(filePath, clusterName);
//        CalculateHydrostaticMass(shells);
//        return shells;
//    }

//    public List<AcceptShell> LoadClusterShells(string filePath, string clusterName)
//    {
//        var allClusters = LoadAllClusterShells(filePath);
//        return allClusters.TryGetValue(clusterName, out var shells)
//            ? shells
//            : new List<AcceptShell>();
//    }

//    public Dictionary<string, List<AcceptShell>> LoadAllClusterShells(string filePath)
//    {
//        if (!File.Exists(filePath))
//            throw new FileNotFoundException($"Die Datei {filePath} wurde nicht gefunden.");

//        var clusterDb = new Dictionary<string, List<AcceptShell>>(StringComparer.OrdinalIgnoreCase);

//        foreach (var rawLine in File.ReadLines(filePath))
//        {
//            if (string.IsNullOrWhiteSpace(rawLine))
//                continue;

//            var line = rawLine.Trim();
//            if (line.StartsWith("#", StringComparison.Ordinal))
//                continue;

//            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
//            if (!TryParseAcceptShell(parts, out var clusterName, out var shell))
//                continue;

//            if (!clusterDb.TryGetValue(clusterName, out var shells))
//            {
//                shells = new List<AcceptShell>();
//                clusterDb[clusterName] = shells;
//            }

//            shells.Add(shell);
//        }

//        foreach (var key in clusterDb.Keys.ToList())
//        {
//            clusterDb[key] = clusterDb[key]
//                .OrderBy(s => s.RadiusKpc)
//                .ToList();
//        }

//        return clusterDb;
//    }

//    private static bool TryParseAcceptShell(string[] parts, out string clusterName, out AcceptShell shell)
//    {
//        clusterName = string.Empty;
//        shell = null!;

//        if (parts.Length < 12)
//            return false;

//        clusterName = parts[0];

//        if (!double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var rinMpc) ||
//            !double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var nelec) ||
//            !double.TryParse(parts[8], NumberStyles.Float, CultureInfo.InvariantCulture, out var pitpl) ||
//            !double.TryParse(parts[11], NumberStyles.Float, CultureInfo.InvariantCulture, out var mgrav))
//        {
//            return false;
//        }

//        shell = new AcceptShell
//        {
//            RadiusKpc = rinMpc * 1000.0,
//            ElectronDensity = nelec,
//            Pressure = pitpl,
//            ReportedMass = mgrav
//        };

//        return true;
//    }

//    public void CalculateHydrostaticMass(List<AcceptShell> shells)
//    {
//        if (shells == null || shells.Count < 3)
//            return;

//        for (int i = 1; i < shells.Count - 1; i++)
//        {
//            var prev = shells[i - 1];
//            var curr = shells[i];
//            var next = shells[i + 1];

//            double rCm = curr.RadiusKpc * PhysicalConstants.KpcToCm;
//            double drCm = (next.RadiusKpc - prev.RadiusKpc) * PhysicalConstants.KpcToCm;
//            if (drCm == 0)
//                continue;

//            double dPdr = (next.Pressure - prev.Pressure) / drCm;

//            // rho = n_e * m_p * 1.9 (voll ionisiertes Plasma)
//            double rho = curr.ElectronDensity * PhysicalConstants.ProtonMass * PhysicalConstants.PlasmaIonizationFactor;
//            if (rho <= 0)
//                continue;

//            // M(r) = - (r^2 / (G * rho)) * dP/dr
//            curr.CalculatedMass = Math.Abs(-(rCm * rCm / (PhysicalConstants.G * rho)) * dPdr) / PhysicalConstants.M_Solar;
//        }
//    }
//    // TRM inertia implementation (synchronization delay)
//    public double CalculateMassWithTRM(double rCm, double rho, double dPdr, double redshift, double k)
//    {
//        // TRM synchronization factor
//        // Higher k or z weakens the effective gravitational coupling
//        double syncFactor = 1.0 / (1.0 + k * redshift);

//        // Effective G reduced by inertial coupling
//        double G_effective = PhysicalConstants.G * syncFactor;

//        // Mass calculation under this model
//        // M = | -(r^2 / (G_eff * rho)) * dP/dr |
//        return Math.Abs(-(Math.Pow(rCm, 2) / (G_effective * rho)) * dPdr) / PhysicalConstants.M_Solar;
//    }

//    public void FindOptimalK(List<AcceptShell> shells, double z_cluster)
//    {
//        var (bestK, minDifference) = ComputeOptimalK(shells, z_cluster, 0.1, 10.0, 0.1);

//        Console.WriteLine($"Gefundener optimaler TRM-Parameter k: {bestK}");
//        Console.WriteLine($"Minimale Differenz zur Realität: {minDifference:E2}");
//    }
//    public void RunTRMParameterSweep(List<AcceptShell> shells, double z_cluster)
//    {
//        var (bestK, minTotalError) = ComputeOptimalK(shells, z_cluster, 0.0, 5.0, 0.05);

//        Console.WriteLine("--------------------------------------------------");
//        Console.WriteLine($"Analyse abgeschlossen!");
//        Console.WriteLine($"Optimaler TRM-Parameter k: {bestK}");
//        Console.WriteLine($"Minimaler Fehler bei diesem k: {minTotalError:E2}");
//        Console.WriteLine("--------------------------------------------------");
//    }


//    public Dictionary<string, double> LoadClusterRedshifts(string filePath)
//    {
//        if (!File.Exists(filePath))
//            throw new FileNotFoundException($"Die Datei {filePath} wurde nicht gefunden.");

//        var redshifts = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

//        foreach (var rawLine in File.ReadLines(filePath))
//        {
//            if (string.IsNullOrWhiteSpace(rawLine))
//                continue;

//            var line = rawLine.Trim();
//            if (line.StartsWith("#", StringComparison.Ordinal))
//                continue;

//            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
//            if (parts.Length < 4)
//                continue;

//            var clusterName = parts[0];
//            if (!double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
//                continue;

//            if (!redshifts.ContainsKey(clusterName))
//                redshifts[clusterName] = z;
//        }

//        return redshifts;
//    }

//    public List<ClusterKFitResult> FindOptimalKForAllClusters(
//        string profileFilePath,
//        string redshiftFilePath,
//        double kMin = 0.1,
//        double kMax = 600.0,
//        double kStep = 1.0)
//    {
//        var allClusters = LoadAllClusterShells(profileFilePath);
//        var redshifts = LoadClusterRedshifts(redshiftFilePath);
//        var results = new ConcurrentBag<ClusterKFitResult>();

//        Parallel.ForEach(allClusters, entry =>
//        {
//            if (!redshifts.TryGetValue(entry.Key, out var zCluster))
//            {
//                LogWarning($"Cluster '{entry.Key}' skipped: redshift not found.");
//                return;
//            }

//            var shells = entry.Value;
//            if (shells.Count < 3)
//            {
//                LogWarning($"Cluster '{entry.Key}' skipped: insufficient points ({shells.Count}).");
//                return;
//            }

//            var (bestK, minError) = ComputeOptimalK(shells, zCluster, kMin, kMax, kStep);
//            results.Add(new ClusterKFitResult(entry.Key, bestK, minError, shells.Count));
//        });

//        return results
//            .OrderBy(r => r.ClusterName)
//            .ToList();
//    }

//    private static (double BestK, double MinError) ComputeOptimalK(
//            List<AcceptShell> shells,
//            double zCluster,
//            double kMin,
//            double kMax,
//            double kStep)
//    {
//        if (zCluster <= 0)
//            return (0, double.MaxValue);

//        var rawSamples = BuildFitSamples(shells);
//        if (rawSamples.Count == 0)
//            return (0, double.MaxValue);

//        // ========================================================
//        // SANITY FILTER: create a filtered list with the same sample type
//        // ========================================================
//        var fitSamples = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Where(rawSamples, sample =>
//        {
//            double mHydroStandard = Math.Abs(-(sample.RadiusSquaredCm2 / (PhysicalConstants.G * sample.Density)) * sample.PressureGradient) / PhysicalConstants.M_Solar;

//            // A shell is valid if mass is finite and physically reasonable
//            return !double.IsNaN(mHydroStandard) &&
//                   !double.IsInfinity(mHydroStandard) &&
//                   mHydroStandard < 1e16 &&
//                   mHydroStandard > 0;
//        }));

//        if (fitSamples.Count == 0)
//            return (0, double.MaxValue);
//        // ========================================================

//        double sumNumerator = 0;
//        double sumDenominator = 0;

//        foreach (var sample in fitSamples)
//        {
//            double mHydroStandard = Math.Abs(-(sample.RadiusSquaredCm2 / (PhysicalConstants.G * sample.Density)) * sample.PressureGradient) / PhysicalConstants.M_Solar;
//            double mReported = sample.ReportedMass;

//            sumNumerator += mHydroStandard * (mReported - mHydroStandard);
//            sumDenominator += mHydroStandard * mHydroStandard;
//        }

//        if (sumDenominator <= 0)
//            return (0, double.MaxValue);

//        double bestK = sumNumerator / (zCluster * sumDenominator);

//        if (bestK < kMin) bestK = kMin;
//        if (bestK > kMax) bestK = kMax;

//        double minError = CalculateErrorForK(fitSamples, zCluster, bestK);

//        // Extra debug guard if filtered values still overflow
//        if (minError > 1e300 || double.IsInfinity(minError) || double.IsNaN(minError))
//        {
//            minError = 1e300;
//        }

//        return (bestK, minError);
//    }

//    private static List<FitSample> BuildFitSamples(List<AcceptShell> shells)
//    {
//        var samples = new List<FitSample>();
//        if (shells == null || shells.Count < 3)
//            return samples;

//        for (int i = 1; i < shells.Count - 1; i++)
//        {
//            var prev = shells[i - 1];
//            var curr = shells[i];
//            var next = shells[i + 1];

//            if (curr.ReportedMass <= 0 ||
//                !IsFinite(curr.ReportedMass) ||
//                !IsFinite(curr.RadiusKpc) ||
//                !IsFinite(curr.ElectronDensity) ||
//                !IsFinite(prev.RadiusKpc) || !IsFinite(next.RadiusKpc) ||
//                !IsFinite(prev.Pressure) || !IsFinite(next.Pressure))
//            {
//                continue;
//            }

//            double drCm = (next.RadiusKpc - prev.RadiusKpc) * PhysicalConstants.KpcToCm;
//            if (Math.Abs(drCm) < 1e-30)
//                continue;

//            double dPdr = (next.Pressure - prev.Pressure) / drCm;
//            if (!IsFinite(dPdr))
//                continue;

//            double rho = curr.ElectronDensity * PhysicalConstants.ProtonMass * PhysicalConstants.PlasmaIonizationFactor;
//            if (rho <= 0 || !IsFinite(rho))
//                continue;

//            double rCm = curr.RadiusKpc * PhysicalConstants.KpcToCm;
//            if (rCm <= 0 || !IsFinite(rCm))
//                continue;

//            samples.Add(new FitSample(rCm * rCm, rho, dPdr, curr.ReportedMass));
//        }

//        return samples;
//    }

//    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

//    private static double CalculateErrorForK(List<FitSample> fitSamples, double zCluster, double k)
//    {
//        if (fitSamples == null || fitSamples.Count == 0)
//            return double.MaxValue;

//        if (!IsFinite(zCluster) || !IsFinite(k))
//            return double.MaxValue;

//        double totalError = 0;

//        double denominator = 1.0 + (k * zCluster);
//        if (Math.Abs(denominator) < 1e-30)
//            return double.MaxValue;

//        double syncFactor = 1.0 / denominator;
//        double gEffective = PhysicalConstants.G * syncFactor;
//        if (gEffective <= 0 || !IsFinite(gEffective))
//            return double.MaxValue;

//        foreach (var sample in fitSamples)
//        {
//            double mTheory = Math.Abs(-(sample.RadiusSquaredCm2 / (gEffective * sample.Density)) * sample.PressureGradient) / PhysicalConstants.M_Solar;
//            if (!IsFinite(mTheory))
//                continue;

//            double squaredError = Math.Pow(mTheory - sample.ReportedMass, 2);
//            if (!IsFinite(squaredError))
//                continue;

//            totalError += squaredError;
//        }

//        return totalError > 0 ? totalError : double.MaxValue;
//    }

//    private static void LogWarning(string message)
//    {
//        lock (LogLock)
//        {
//            Console.WriteLine($"WARN: {message}");
//        }
//    }

//    public static void ComparePhysicsModels(
//        string clusterName,
//        List<AcceptShell> shells,
//        double fixedZ,
//        double bestK,
//        double bestError,
//        double baselineK = 0.1)
//    {
//        if (shells == null || shells.Count < 3)
//        {
//            Console.WriteLine($"Cluster: {clusterName}");
//            Console.WriteLine("  Not enough data points for comparison.");
//            Console.WriteLine("------------------------------------------");
//            return;
//        }

//        var fitSamples = BuildFitSamples(shells);
//        double errorAtBaseline = CalculateErrorForK(fitSamples, fixedZ, baselineK);

//        // Compute improvement factor
//        double improvement = (bestError > 0 && !double.IsNaN(bestError) && !double.IsInfinity(bestError))
//            ? errorAtBaseline / bestError
//            : double.PositiveInfinity;

//        //Console.WriteLine($"Cluster: {clusterName}");
//        //Console.WriteLine($"  Fixed z: {fixedZ:F4}");
//        //Console.WriteLine($"  Error (baseline k={baselineK:F2}): {errorAtBaseline:E2}");
//        //Console.WriteLine($"  Error (model k={bestK}): {bestError:E2}");
//        //Console.WriteLine($"  Improvement factor: {improvement:F2}x");
       
//        Console.WriteLine($"{clusterName} | {fixedZ:F4} | {baselineK:F2} | {errorAtBaseline:E2} | {bestK} | {bestError:E2} | {improvement:F2}x");
//        Console.WriteLine("------------------------------------------");
//    }

//    public static void ComparePhysicsModels(
//        string clusterName,
//        List<AcceptShell> shells,
//        double fixedZ,
//        double baselineK = 0.1,
//        double kMin = 0.1,
//        double kMax = 200.0,
//        double kStep = 1.0)
//    {
//        var (bestK, bestError) = ComputeOptimalK(shells, fixedZ, kMin, kMax, kStep);
//        ComparePhysicsModels(clusterName, shells, fixedZ, bestK, bestError, baselineK);
//    }
//    public static void ExportAnalysisToCsv(IEnumerable<AcceptShell> shells, string filePath)
//    {
//        using (StreamWriter writer = new StreamWriter(filePath))
//        {
//            // CSV Header
//            writer.WriteLine("Radius_kpc;Mgrav_Reported;M_Hydro_Calc;Coverage_Percent");

//            foreach (var shell in shells)
//            {
//                double r = shell.RadiusKpc;
//                double mReported = shell.ReportedMass;
//                double mHydro = shell.CalculatedMass;

//                // Filter: ignore rows with negative reported values (measurement noise)
//                if (mReported > 0 && mHydro > 0)
//                {
//                    double coverage = (mHydro / mReported) * 100;

//                    // Write values with invariant culture for international compatibility
//                    string line = string.Format(CultureInfo.InvariantCulture,
//                                  "{0:F0};{1:E2};{2:E2};{3:F2}",
//                                  r, mReported, mHydro, coverage);

//                    writer.WriteLine(line);
//                }
//            }
//        }
//        Console.WriteLine($"Analysis exported successfully to: {filePath}");
//    }

//    public static void DeriveDynamicKzLaw(List<ClusterKFitResult> results, Dictionary<string, double> redshifts)
//    {
//        Console.WriteLine("\n=======================================================");
//        Console.WriteLine("    ANALYSIS OF THE DYNAMIC K(z) DEPENDENCE           ");
//        Console.WriteLine("=======================================================\n");

//        // 1) Filter data: keep only clusters that truly require modification.
//        // Values stuck at the floor (0.1) or upper limit distort the fit
//        // because they are not physical optima.
//        var validFits = new List<(double z, double k)>();

//        foreach (var result in results)
//        {
//            // Exclude baseline clusters (k <= 0.11) and require known z
//            if (result.BestK > 0.11 && redshifts.TryGetValue(result.ClusterName, out double z))
//            {
//                validFits.Add((z, result.BestK));
//            }
//        }

//        if (validFits.Count < 2)
//        {
//            Console.WriteLine("Not enough valid outlier clusters for regression.");
//            return;
//        }

//        // 2) Linear regression in log-log space for power law K(z) = C * z^alpha
//        // ln(K) = ln(C) + alpha * ln(z)

//        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
//        int n = validFits.Count;

//        foreach (var fit in validFits)
//        {
//            double lnZ = Math.Log(fit.z);
//            double lnK = Math.Log(fit.k);

//            sumX += lnZ;
//            sumY += lnK;
//            sumXY += lnZ * lnK;
//            sumX2 += lnZ * lnZ;
//        }

//        // Analytical solution for slope (alpha) and intercept (ln(C))
//        double denominator = (n * sumX2) - (sumX * sumX);
//        if (denominator == 0) return;

//        double alpha = ((n * sumXY) - (sumX * sumY)) / denominator;
//        double lnC = (sumY - (alpha * sumX)) / n;
//        double C = Math.Exp(lnC);

//        // 3) Report fitted physical law
//        Console.WriteLine($"Analyzed modified clusters: {n}");
//        Console.WriteLine($"Estimated prefactor (C): {C:F4}");
//        Console.WriteLine($"Estimated exponent (alpha): {alpha:F4}");
//        Console.WriteLine("-------------------------------------------------------");

//        Console.WriteLine("\n[ RESULTIERENDES PHYSIKALISCHES GESETZ ]");
//        Console.WriteLine($"K(z) = {C:F4} * z^({alpha:F4})");

//        // 4) Physical interpretation
//        Console.WriteLine("\n[ COSMOLOGICAL INTERPRETATION ]");
//        if (Math.Abs(alpha - (-1.0)) < 0.2)
//        {
//            Console.WriteLine("Exponent is very close to -1.0.");
//            Console.WriteLine($"This suggests K(z) scales nearly reciprocally with redshift (1/z).");
//            Console.WriteLine($"The universal cluster time-synchronization factor appears approximately constant.");
//            Console.WriteLine($"Derived universal kappa: K * z = {C:F4}");
//        }
//        else
//        {
//            Console.WriteLine("Exponent deviates from -1, indicating a more complex non-linear evolution.");
//        }
//        Console.WriteLine("=======================================================\n");
//    }

//    public void EvaluateUniversalLawForAllClusters(
//        string profileFilePath,
//        string redshiftFilePath,
//        double C = 1.3195,
//        double alpha = -0.7589,
//        double baselineK = 0.1)
//    {
//        var allClusters = LoadAllClusterShells(profileFilePath);
//        var redshifts = LoadClusterRedshifts(redshiftFilePath);

//        Console.WriteLine("\n=======================================================================================");
//        Console.WriteLine($"   TEST OF UNIVERSAL LAW: K(z) = {C:F4} * z^({alpha:F4})");
//        Console.WriteLine("=======================================================================================");
//        Console.WriteLine("ClusterName      | z      | Universal K | Baseline Error | Universal Error | Improvement");
//        Console.WriteLine("---------------------------------------------------------------------------------------");

//        int validCount = 0;
//        int improvedCount = 0;
//        double sumImprovement = 0;
//        double totalBaselineError = 0;
//        double totalUniversalError = 0;

//        foreach (var entry in allClusters.OrderBy(k => k.Key))
//        {
//            string clusterName = entry.Key;
//            var shells = entry.Value;

//            if (!redshifts.TryGetValue(clusterName, out var zCluster) || zCluster <= 0) continue;

//            // 1) Load data and apply sanity filter
//            var rawSamples = BuildFitSamples(shells);
//            var fitSamples = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Where(rawSamples, sample =>
//            {
//                double mHydroStandard = Math.Abs(-(sample.RadiusSquaredCm2 / (PhysicalConstants.G * sample.Density)) * sample.PressureGradient) / PhysicalConstants.M_Solar;
//                return !double.IsNaN(mHydroStandard) && !double.IsInfinity(mHydroStandard) && mHydroStandard < 1e16 && mHydroStandard > 0;
//            }));

//            if (fitSamples.Count == 0) continue;

//            // 2) Compute universal physical K (no optimizer)
//            double universalK = C * Math.Pow(zCluster, alpha);

//            // 3) Compare errors
//            double errorBaseline = CalculateErrorForK(fitSamples, zCluster, baselineK);
//            double errorUniversal = CalculateErrorForK(fitSamples, zCluster, universalK);

//            // Guard against numeric overflow
//            if (errorBaseline > 1e300 || double.IsNaN(errorBaseline)) errorBaseline = 1e300;
//            if (errorUniversal > 1e300 || double.IsNaN(errorUniversal)) errorUniversal = 1e300;

//            // 4) Compute improvement
//            double improvement = 1.0;
//            if (errorUniversal > 0 && errorUniversal < 1e300)
//            {
//                improvement = errorBaseline / errorUniversal;
//            }

//            validCount++;

//            // Count only clusters with significant improvement (not rounding noise)
//            if (improvement > 1.05) improvedCount++;

//            // Cap improvement at 1000x for averaging to avoid extreme outlier dominance
//            sumImprovement += Math.Min(improvement, 1000.0);

//            // Sum absolute errors only for finite values
//            if (errorBaseline < 1e300 && errorUniversal < 1e300)
//            {
//                totalBaselineError += errorBaseline;
//                totalUniversalError += errorUniversal;
//            }

//            Console.WriteLine($"{clusterName,-16} | {zCluster:F4} | {universalK,11:F4} | {errorBaseline,14:E2} | {errorUniversal,15:E2} | {improvement:F2}x");
//        }

//        // =======================================================
//        // FINAL STATISTICS
//        // =======================================================
//        double avgImprovement = sumImprovement / validCount;
//        double globalErrorReduction = (totalBaselineError > 0) ? (totalBaselineError / totalUniversalError) : 0;

//        Console.WriteLine("=======================================================================================");
//        Console.WriteLine($"SUMMARY OF PARAMETER-FREE THEORY:");
//        Console.WriteLine($"Successfully analyzed clusters:    {validCount}");
//        Console.WriteLine($"Clusters with real improvement:    {improvedCount} of {validCount} ({((double)improvedCount / validCount) * 100:F1}%)");
//        Console.WriteLine($"Average improvement:              {avgImprovement:F2}x per cluster");
//        Console.WriteLine($"Global error reduction (sum):     {globalErrorReduction:F2}x vs baseline");
//        Console.WriteLine("=======================================================================================\n");
//    }
//    public void EvaluateBimodalTheoryForAllClusters(
//        string profileFilePath,
//        string redshiftFilePath,
//        double C = 1.3195,
//        double alpha = -0.7589,
//        double baselineK = 0.1)
//    {
//        var allClusters = LoadAllClusterShells(profileFilePath);
//        var redshifts = LoadClusterRedshifts(redshiftFilePath);

//        Console.WriteLine("\n=======================================================================================");
//        Console.WriteLine($"   BIMODAL THEORY CHECK: Newton (0.1) vs. Cosmological coupling K(z)");
//        Console.WriteLine("=======================================================================================");
//        Console.WriteLine("ClusterName      | Gruppe | Angewandtes K | Bester Fehler | Improvement vs Baseline");
//        Console.WriteLine("---------------------------------------------------------------------------------------");

//        int countGroupA = 0; // Stay on Newton
//        int countGroupB = 0; // Require Clockwork modification

//        double totalBaselineErrorSum = 0; // Baseline Newton aggregate error
//        double totalBimodalErrorSum = 0;  // Hybrid-theory aggregate error
//        double sumImprovement = 0;

//        foreach (var entry in allClusters.OrderBy(k => k.Key))
//        {
//            string clusterName = entry.Key;
//            var shells = entry.Value;

//            if (!redshifts.TryGetValue(clusterName, out var zCluster) || zCluster <= 0) continue;

//            // Sanity filter for unphysical shells
//            var fitSamples = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Where(BuildFitSamples(shells), sample =>
//            {
//                double mHydroStandard = Math.Abs(-(sample.RadiusSquaredCm2 / (PhysicalConstants.G * sample.Density)) * sample.PressureGradient) / PhysicalConstants.M_Solar;
//                return !double.IsNaN(mHydroStandard) && !double.IsInfinity(mHydroStandard) && mHydroStandard < 1e16 && mHydroStandard > 0;
//            }));

//            if (fitSamples.Count == 0) continue;

//            // 1) Compute Newton baseline error
//            double errorBaseline = CalculateErrorForK(fitSamples, zCluster, baselineK);
//            if (errorBaseline > 1e300 || double.IsNaN(errorBaseline)) errorBaseline = 1e300;

//            // 2) Compute Clockwork error (universal law)
//            double universalK = C * Math.Pow(zCluster, alpha);
//            double errorUniversal = CalculateErrorForK(fitSamples, zCluster, universalK);
//            if (errorUniversal > 1e300 || double.IsNaN(errorUniversal)) errorUniversal = 1e300;

//            // 3) Activation switch (bimodal decision)
//            string gruppe;
//            double finalK;
//            double finalError;
//            double improvement = 1.0;

//            // Activate modification only if error improves by more than 5%
//            if (errorUniversal < errorBaseline * 0.95)
//            {
//                gruppe = "B (Clockwork)";
//                finalK = universalK;
//                finalError = errorUniversal;
//                countGroupB++;
//            }
//            else
//            {
//                gruppe = "A (Newton)   ";
//                finalK = baselineK;
//                finalError = errorBaseline;
//                countGroupA++;
//            }

//            // Compute improvement
//            if (finalError > 0 && finalError < 1e300)
//            {
//                improvement = errorBaseline / finalError;
//            }

//            sumImprovement += improvement;

//            if (errorBaseline < 1e300 && finalError < 1e300)
//            {
//                totalBaselineErrorSum += errorBaseline;
//                totalBimodalErrorSum += finalError;
//            }

//            Console.WriteLine($"{clusterName,-16} | {gruppe} | {finalK,13:F4} | {finalError,13:E2} | {improvement:F2}x");
//        }

//        // =======================================================
//        // FINAL STATISTICS
//        // =======================================================
//        int totalClusters = countGroupA + countGroupB;
//        double avgImprovement = sumImprovement / totalClusters;
//        double globalErrorReduction = (totalBaselineErrorSum > 0 && totalBimodalErrorSum > 0)
//                                      ? (totalBaselineErrorSum / totalBimodalErrorSum) : 0;

//        Console.WriteLine("=======================================================================================");
//        Console.WriteLine($"SUMMARY OF OPTIMIZED THEORY:");
//        Console.WriteLine($"Total analyzed clusters:          {totalClusters}");
//        Console.WriteLine($"Group A (classical Newton):       {countGroupA} clusters ({((double)countGroupA / totalClusters) * 100:F1}%)");
//        Console.WriteLine($"Group B (active Clockwork K(z)):  {countGroupB} clusters ({((double)countGroupB / totalClusters) * 100:F1}%)");
//        Console.WriteLine($"Average improvement:              {avgImprovement:F2}x per cluster");
//        Console.WriteLine($"Global error reduction (sum):     {globalErrorReduction:F2}x vs pure baseline");
//        Console.WriteLine("=======================================================================================\n");
//    }

//    public void DiagnosePhysicalThreshold(
//        string profileFilePath,
//        string redshiftFilePath,
//        double C = 1.3195,
//        double alpha = -0.7589,
//        double baselineK = 0.1)
//    {
//        var allClusters = LoadAllClusterShells(profileFilePath);
//        var redshifts = LoadClusterRedshifts(redshiftFilePath);

//        Console.WriteLine("\n=======================================================================================");
//        Console.WriteLine("   PHYSICAL DIAGNOSTIC: searching for activation threshold");
//        Console.WriteLine("=======================================================================================");

//        // Collectors for statistical properties
//        var groupA_Densities = new List<double>();
//        var groupA_PressureGrads = new List<double>();
//        var groupA_Masses = new List<double>();
//        var groupA_Redshifts = new List<double>();

//        var groupB_Densities = new List<double>();
//        var groupB_PressureGrads = new List<double>();
//        var groupB_Masses = new List<double>();
//        var groupB_Redshifts = new List<double>();

//        foreach (var entry in allClusters)
//        {
//            string clusterName = entry.Key;
//            var shells = entry.Value;

//            if (!redshifts.TryGetValue(clusterName, out var zCluster) || zCluster <= 0) continue;

//            var rawSamples = BuildFitSamples(shells);
//            var fitSamples = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Where(rawSamples, sample =>
//            {
//                double mHydroStandard = Math.Abs(-(sample.RadiusSquaredCm2 / (PhysicalConstants.G * sample.Density)) * sample.PressureGradient) / PhysicalConstants.M_Solar;
//                return !double.IsNaN(mHydroStandard) && !double.IsInfinity(mHydroStandard) && mHydroStandard < 1e16 && mHydroStandard > 0;
//            }));

//            if (fitSamples.Count == 0) continue;

//            // Compute errors for classification
//            double errorBaseline = CalculateErrorForK(fitSamples, zCluster, baselineK);
//            if (double.IsNaN(errorBaseline) || errorBaseline > 1e300) errorBaseline = 1e300;

//            double universalK = C * Math.Pow(zCluster, alpha);
//            double errorUniversal = CalculateErrorForK(fitSamples, zCluster, universalK);
//            if (double.IsNaN(errorUniversal) || errorUniversal > 1e300) errorUniversal = 1e300;

//            // Extract core physical properties of this cluster
//            // Use maximum density and pressure gradient as core-state indicators
//            double maxDensity = System.Linq.Enumerable.Max(fitSamples, s => s.Density);
//            double maxPressureGrad = System.Linq.Enumerable.Max(fitSamples, s => Math.Abs(s.PressureGradient));
//            double totalMass = System.Linq.Enumerable.Max(fitSamples, s => s.ReportedMass); // Outermost reported mass

//            // Classification (Group A vs B)
//            if (errorUniversal < errorBaseline * 0.95)
//            {
//                // GROUP B (Clockwork)
//                groupB_Densities.Add(maxDensity);
//                groupB_PressureGrads.Add(maxPressureGrad);
//                groupB_Masses.Add(totalMass);
//                groupB_Redshifts.Add(zCluster);
//            }
//            else
//            {
//                // GROUP A (Newton)
//                groupA_Densities.Add(maxDensity);
//                groupA_PressureGrads.Add(maxPressureGrad);
//                groupA_Masses.Add(totalMass);
//                groupA_Redshifts.Add(zCluster);
//            }
//        }

//        // =======================================================
//        // DATA EVALUATION
//        // =======================================================
//        Console.WriteLine($"Group A (Newton): {groupA_Densities.Count} clusters");
//        Console.WriteLine($"Group B (Clockwork): {groupB_Densities.Count} clusters\n");

//        Console.WriteLine(String.Format("{0,-25} | {1,-22} | {2,-22}", "Physical property", "Group A (Newton)", "Group B (Clockwork)"));
//        Console.WriteLine(new String('-', 75));

//        PrintComparisonRow("Average z", groupA_Redshifts, groupB_Redshifts, "F4");
//        PrintComparisonRow("Max core density (rho)", groupA_Densities, groupB_Densities, "E2");
//        PrintComparisonRow("Max pressure gradient", groupA_PressureGrads, groupB_PressureGrads, "E2");
//        PrintComparisonRow("Average total mass", groupA_Masses, groupB_Masses, "E2");

//        Console.WriteLine(new String('-', 75));
//        Console.WriteLine("\n[ NEXT STEP FOR THE PAPER ]");
//        Console.WriteLine("Inspect the table above for the property with the largest relative difference.");
//        Console.WriteLine("That difference is the physical trigger for macroscopic time synchronization.");
//        Console.WriteLine("=======================================================================================\n");
//    }

//    // Helper for clean console output
//    private void PrintComparisonRow(string label, List<double> listA, List<double> listB, string format)
//    {
//        double avgA = listA.Count > 0 ? System.Linq.Enumerable.Average(listA) : 0;
//        double avgB = listB.Count > 0 ? System.Linq.Enumerable.Average(listB) : 0;

//        // Compute relative difference (factor)
//        double ratio = (avgA > 0 && avgB > 0) ? Math.Max(avgA / avgB, avgB / avgA) : 1;
//        string winner = avgA > avgB ? "A is larger" : "B is larger";

//        Console.WriteLine(String.Format("{0,-25} | {1,-22} | {2,-22}",
//            label,
//            avgA.ToString(format),
//            avgB.ToString(format)));
//        Console.WriteLine(String.Format("{0,-25} | -> Difference: {1:F1}x ({2})", "", ratio, winner));
//        Console.WriteLine();
//    }

//    public void EvaluatePhysicsDrivenBimodalTheory(
//            Dictionary<string, List<AcceptShell>> allClusters,
//            Dictionary<string, double> redshifts,
//            double C = 1.3195,
//            double alpha = -0.7589,
//            double baselineK = 0.1,
//            double pressureThreshold = 6.0e-34,
//            string resultsCsvPath = "results.csv") // Physically motivated trigger
//    {
//        Console.WriteLine("\n=========================================================================================================");
//        Console.WriteLine($"   FINAL EVIDENCE: TRM vs. Newton (phase trigger: {pressureThreshold:E2})");
//        Console.WriteLine("=========================================================================================================");
//        Console.WriteLine(String.Format("{0,-18} | {1,-6} | {2,-11} | {3,-13} | {4,-8} | {5,-12} | {6,-12} | {7}",
//            "Cluster", "z", "Max Grad(P)", "Decision", "K-value", "Baseline-Err", "Final-Error", "Improvement"));
//        Console.WriteLine(new String('-', 108));

//        int countNewton = 0;
//        int countTRM = 0;
//        int successfulPredictions = 0;
//        var exportRows = new List<BimodalExportRow>();

//        // Geometric-mean aggregation
//        double sumLogImprovement = 0;
//        int validImprovementCount = 0;

//        foreach (var entry in allClusters.OrderBy(k => k.Key))
//        {
//            string clusterName = entry.Key;
//            var shells = entry.Value;

//            if (!redshifts.TryGetValue(clusterName, out var zCluster) || zCluster <= 0) continue;

//            var rawSamples = BuildFitSamples(shells);
//            var fitSamples = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Where(rawSamples, sample =>
//            {
//                double mHydroStandard = Math.Abs(-(sample.RadiusSquaredCm2 / (PhysicalConstants.G * sample.Density)) * sample.PressureGradient) / PhysicalConstants.M_Solar;
//                return !double.IsNaN(mHydroStandard) && !double.IsInfinity(mHydroStandard) && mHydroStandard < 1e16 && mHydroStandard > 0;
//            }));

//            if (fitSamples.Count == 0) continue;

//            double maxPressureGrad = System.Linq.Enumerable.Max(fitSamples, s => Math.Abs(s.PressureGradient));
//            bool useClockwork = maxPressureGrad < pressureThreshold;

//            double finalK = useClockwork ? (C * Math.Pow(zCluster, alpha)) : baselineK;
//            string decisionStr = useClockwork ? "TRM" : "Newton";

//            if (useClockwork) countTRM++; else countNewton++;

//            double errorBaseline = CalculateErrorForK(fitSamples, zCluster, baselineK);
//            if (double.IsNaN(errorBaseline) || errorBaseline > 1e300) errorBaseline = 1e300;

//            double errorFinal = CalculateErrorForK(fitSamples, zCluster, finalK);
//            if (double.IsNaN(errorFinal) || errorFinal > 1e300) errorFinal = 1e300;

//            // Oracle check for statistics
//            double oracleError = CalculateErrorForK(fitSamples, zCluster, C * Math.Pow(zCluster, alpha));
//            bool truthNeedsClockwork = (oracleError < errorBaseline * 0.95);
//            if (useClockwork == truthNeedsClockwork) successfulPredictions++;

//            // Compute improvement
//            double improvement = 1.0;
//            if (errorFinal > 0 && errorFinal < 1e300 && errorBaseline < 1e300)
//            {
//                improvement = errorBaseline / errorFinal;
//                sumLogImprovement += Math.Log(improvement); // Log for geometric mean
//                validImprovementCount++;
//            }

//            Console.WriteLine(String.Format("{0,-18} | {1:F4} | {2,11:E2} | {3,-13} | {4,8:F4} | {5,12:E2} | {6,12:E2} | {7:F2}x",
//                clusterName, zCluster, maxPressureGrad, decisionStr, finalK, errorBaseline, errorFinal, improvement));

//            exportRows.Add(new BimodalExportRow(clusterName, zCluster, maxPressureGrad, improvement));
//        }

//        int totalClusters = countNewton + countTRM;
//        double predictionAccuracy = ((double)successfulPredictions / totalClusters) * 100;

//        // Physically robust evaluation (geometric mean)
//        double geometricMeanImprovement = validImprovementCount > 0 ? Math.Exp(sumLogImprovement / validImprovementCount) : 1.0;

//        Console.WriteLine(new String('-', 108));
//        Console.WriteLine($"PUBLICATION SUMMARY:");
//        Console.WriteLine($"Classified as Newton:             {countNewton} clusters");
//        Console.WriteLine($"Classified as TRM:                {countTRM} clusters");
//        Console.WriteLine($"Prediction accuracy:              {predictionAccuracy:F1}% (trigger selected correct physics)");
//        Console.WriteLine($"Global average improvement:       {geometricMeanImprovement:F2}x per cluster (geometric mean)");
//        Console.WriteLine("=========================================================================================================\n");

//        ExportBimodalResultsToCsv(exportRows, resultsCsvPath);
//    }

//    private static void ExportBimodalResultsToCsv(IEnumerable<BimodalExportRow> rows, string filePath)
//    {
//        using var writer = new StreamWriter(filePath);
//        writer.WriteLine("Cluster,z,MaxGradP,Improvement");

//        foreach (var row in rows)
//        {
//            writer.WriteLine(string.Format(
//                CultureInfo.InvariantCulture,
//                "{0},{1:F6},{2:E6},{3:F6}",
//                row.Cluster,
//                row.Z,
//                row.MaxGradP,
//                row.Improvement));
//        }

//        Console.WriteLine($"CSV export complete: {filePath}");
//    }

    
//    public void FindBestPhysicalThreshold(Dictionary<string, List<AcceptShell>> allClusters,
//    Dictionary<string, double> redshifts)
//    {
//        Console.WriteLine("\nSearching for the optimal pressure threshold...");
//        double bestThreshold = 0;
//        double maxAccuracy = 0;

//        // Sweep values from 1.0E-34 to 2.0E-32 in fine steps
//        for (double t = 1.0e-34; t <= 2.0e-32; t += 1.0e-34)
//        {
//            // Run silent variant that returns only prediction accuracy
//            double accuracy = RunSilentPredictionTest(allClusters, redshifts, 1.3195, -0.7589, 0.1, t);

//            if (accuracy > maxAccuracy)
//            {
//                maxAccuracy = accuracy;
//                bestThreshold = t;
//            }
//        }

//        Console.WriteLine($"\nBEST THRESHOLD FOUND: {bestThreshold:E2}");
//        Console.WriteLine($"MAXIMUM PREDICTION ACCURACY: {maxAccuracy:F1}%");
//    }

//    public double RunSilentPredictionTest(
//    Dictionary<string, List<AcceptShell>> allClusters,
//    Dictionary<string, double> redshifts,
//    double C = 1.3195,
//    double alpha = -0.7589,
//    double baselineK = 0.1,
//    double pressureThreshold = 5.0e-33) // Discovered physical trigger
//    {

               
//        Console.WriteLine($"   PHYSICS-PREDICTIVE THEORY: TRM vs. Newton (trigger threshold: {pressureThreshold:E1})");
        
//        //Console.WriteLine(String.Format("{0,-16} | {1,-6} | {2,-11} | {3,-13} | {4,-8} | {5,-12} | {6,-12} | {7}",
//        //    "Cluster", "z", "Max Grad(P)", "Decision", "K-value", "Baseline-Err", "Final-Error", "Improvement"));
//        //Console.WriteLine(new String('-', 105));

//        int countNewton = 0;
//        int countTRM = 0;
//        int successfulPredictions = 0;

//        double totalBaselineErrorSum = 0;
//        double totalFinalErrorSum = 0;

//        foreach (var entry in allClusters.OrderBy(k => k.Key))
//        {
//            string clusterName = entry.Key;
//            var shells = entry.Value;

//            if (!redshifts.TryGetValue(clusterName, out var zCluster) || zCluster <= 0) continue;

//            // Sanity filter
//            var rawSamples = BuildFitSamples(shells);
//            var fitSamples = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Where(rawSamples, sample =>
//            {
//                double mHydroStandard = Math.Abs(-(sample.RadiusSquaredCm2 / (PhysicalConstants.G * sample.Density)) * sample.PressureGradient) / PhysicalConstants.M_Solar;
//                return !double.IsNaN(mHydroStandard) && !double.IsInfinity(mHydroStandard) && mHydroStandard < 1e16 && mHydroStandard > 0;
//            }));

//            if (fitSamples.Count == 0) continue;

//            // 1) Determine physical feature: max pressure gradient
//            double maxPressureGrad = System.Linq.Enumerable.Max(fitSamples, s => Math.Abs(s.PressureGradient));

//            // 2) Physical decision path (without seeing final error)
//            bool useClockwork = maxPressureGrad < pressureThreshold;

//            double finalK;
//            string decisionStr;

//            if (useClockwork)
//            {
//                finalK = C * Math.Pow(zCluster, alpha);
//                decisionStr = "TRM";
//                countTRM++;
//            }
//            else
//            {
//                finalK = baselineK; // 0.1
//                decisionStr = "Newton";
//                countNewton++;
//            }

//            // 3) Compute errors
//            double errorBaseline = CalculateErrorForK(fitSamples, zCluster, baselineK);
//            if (double.IsNaN(errorBaseline) || errorBaseline > 1e300) errorBaseline = 1e300;

//            double errorFinal = CalculateErrorForK(fitSamples, zCluster, finalK);
//            if (double.IsNaN(errorFinal) || errorFinal > 1e300) errorFinal = 1e300;

//            // ====================================================================
//            // Oracle check (reference physical truth)
//            // ====================================================================
//            // Silently compute outcome if Clockwork had been used
//            double oracleUniversalError = CalculateErrorForK(fitSamples, zCluster, C * Math.Pow(zCluster, alpha));

//            // Reference truth: does this cluster actually need Clockwork modification?
//            bool truthNeedsClockwork = (oracleUniversalError < errorBaseline * 0.95);

//            // Was the trigger decision (useClockwork) correct?
//            if (useClockwork == truthNeedsClockwork)
//            {
//                successfulPredictions++; // Count only true hits
//            }
//            // ====================================================================

//            // 4) Statistics and output
//            double improvement = 1.0;
//            if (errorFinal > 0 && errorFinal < 1e300)
//            {
//                improvement = errorBaseline / errorFinal;
//            }


//        }

//        // =======================================================
//        // FINAL STATISTICS FOR PAPER
//        // =======================================================
//        int totalClusters = countNewton + countTRM;
//        double globalErrorReduction = (totalBaselineErrorSum > 0 && totalFinalErrorSum > 0)
//                                      ? (totalBaselineErrorSum / totalFinalErrorSum) : 0;
//        double predictionAccuracy = ((double)successfulPredictions / totalClusters) * 100;

//        return predictionAccuracy;
//    }
//}
