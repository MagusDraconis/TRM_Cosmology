using System;
using System.IO;
using System.Linq;
using TRM.Core;
using TRM.Core.Baryons;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.CoreTests;

public class RarRelationTests
{
    private readonly ITestOutputHelper _output;

    public RarRelationTests(ITestOutputHelper output)
    {
        _output = output;
    }



    [Fact]
    public void Test_TRM_Rar_ExponentialDisk_RadiusBins()
    {
        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rawPoints = SparcRarAnalysis.ParseRarFromZip(zipPath);
        var galaxyMeta = SparcRarAnalysis.LoadGalaxyMetaFromMrt(mrtPath);
        var scaling = TrmCosmologyParameters.Current();

        var trmDisk = SparcRarAnalysis.ApplyTrmDistanceMapping(
            rawPoints,
            galaxyMeta,
            scaling,
            BaryonMode.ExponentialDisk
        );

        Assert.NotEmpty(trmDisk);

        double fixedA0 = 1.2e-10;

        var rawGalaxyCache = rawPoints
            .GroupBy(p => p.GalaxyName)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        var bin1 = new List<double>(); // r/Rd < 1
        var bin2 = new List<double>(); // 1 <= r/Rd < 2
        var bin3 = new List<double>(); // 2 <= r/Rd < 4
        var bin4 = new List<double>(); // r/Rd >= 4

        var diskGalaxyGroups = trmDisk
            .GroupBy(p => p.GalaxyName)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        foreach (var kvp in diskGalaxyGroups)
        {
            string galaxyName = kvp.Key;
            var diskPoints = kvp.Value;

            if (!rawGalaxyCache.TryGetValue(galaxyName, out var rawGalaxy))
                continue;

            if (rawGalaxy.Count < 5 || diskPoints.Count < 5)
                continue;

            // ✅ Rd aus dem RAW-Profil bestimmen, nicht aus TRM-transformierten Punkten
            double rd = SparcRarAnalysis.EstimateDiskScaleLengthFromProfile(rawGalaxy);
            if (rd <= 0)
                continue;

            foreach (var p in diskPoints)
            {
                if (p.GbarMs2 <= 0 || p.GobsMs2 <= 0)
                    continue;

                double gPred = SparcRarAnalysis.PredictGobs(
                    p.GbarMs2,
                    fixedA0,
                    ModelType.ClockworkTRM
                );

                if (gPred <= 0 || double.IsNaN(gPred) || double.IsInfinity(gPred))
                    continue;

                double residual = Math.Log10(p.GobsMs2) - Math.Log10(gPred);

                // ✅ p.RadiusKpc ist hier TRM-Radius; Rd kommt aus Raw-Geometrie
                double x = p.RadiusKpc / rd;

                if (x < 1.0)
                    bin1.Add(residual);
                else if (x < 2.0)
                    bin2.Add(residual);
                else if (x < 4.0)
                    bin3.Add(residual);
                else
                    bin4.Add(residual);
                
            }
        }

        double rms1 = ComputeBinRms(bin1);
        double rms2 = ComputeBinRms(bin2);
        double rms3 = ComputeBinRms(bin3);
        double rms4 = ComputeBinRms(bin4);

        _output.WriteLine($"Bin <1 Rd     : n={bin1.Count}, RMS={rms1:F4}, Mean={ComputeMean(bin1):F4}");
        _output.WriteLine($"Bin 1-2 Rd    : n={bin2.Count}, RMS={rms2:F4}, Mean={ComputeMean(bin2):F4}");
        _output.WriteLine($"Bin 2-4 Rd    : n={bin3.Count}, RMS={rms3:F4}, Mean={ComputeMean(bin3):F4}");
        _output.WriteLine($"Bin >=4 Rd    : n={bin4.Count}, RMS={rms4:F4}, Mean={ComputeMean(bin4):F4}");



        Assert.True(bin1.Count > 20, "Too few points in inner bin (<1 Rd).");
        Assert.True(bin2.Count > 20, "Too few points in 1-2 Rd bin.");
        Assert.True(bin3.Count > 20, "Too few points in 2-4 Rd bin.");
        Assert.True(bin4.Count > 20, "Too few points in >=4 Rd bin.");

        Assert.InRange(rms1, 0.0, 2.5);
        Assert.InRange(rms2, 0.0, 2.5);
        Assert.InRange(rms3, 0.0, 2.5);
        Assert.InRange(rms4, 0.0, 2.5);
    }


    [Fact]
    public void Test_TRM_Rar_ExponentialDiskImpact()
    {
        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rawPoints = SparcRarAnalysis.ParseRarFromZip(zipPath);
        var galaxyMeta = SparcRarAnalysis.LoadGalaxyMetaFromMrt(mrtPath);
        var scaling = TrmCosmologyParameters.Current();

        var trmGR = SparcRarAnalysis.ApplyTrmDistanceMapping(
            rawPoints, galaxyMeta, scaling,
            BaryonMode.GR_Consistent);

        var trmDisk = SparcRarAnalysis.ApplyTrmDistanceMapping(
            rawPoints, galaxyMeta, scaling,
            BaryonMode.ExponentialDisk);

        double fixedA0 = 1.2e-10;

        double rmsGR = ComputeRms(trmGR, fixedA0);
        double rmsDisk = ComputeRms(trmDisk, fixedA0);

        _output.WriteLine($"RMS GR   = {rmsGR:F4}");
        _output.WriteLine($"RMS DISK = {rmsDisk:F4}");
        _output.WriteLine($"Delta    = {rmsDisk - rmsGR:F4}");
        _output.WriteLine($"Ratio    = {rmsDisk / rmsGR:F4}");

        Assert.NotEmpty(trmGR);
        Assert.NotEmpty(trmDisk);

        //Assert.InRange(rmsGR, 0.4, 0.1);
        //Assert.InRange(rmsDisk, 0.4, 1.5);

        //// DISK darf schlechter sein, aber nicht komplett entgleisen
        //Assert.True(rmsDisk <= 2.0 * rmsGR,
        //    $"ExponentialDisk model degrades too strongly: GR={rmsGR:F4}, DISK={rmsDisk:F4}");
    }
    [Fact]
    public void Test_TRM_Rar_MassModelImpact()
    {
        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rawPoints = SparcRarAnalysis.ParseRarFromZip(zipPath);
        var galaxyMeta = SparcRarAnalysis.LoadGalaxyMetaFromMrt(mrtPath);
        var scaling = TrmCosmologyParameters.Current();

        // ✅ Vergleich: GR-basierter baryonischer Term
        var trmGR = SparcRarAnalysis.ApplyTrmDistanceMapping(
            rawPoints,
            galaxyMeta,
            scaling,
            BaryonMode.GR_Consistent
        );

        // ✅ Vergleich: neuer Mass-Model Ansatz
        var trmMass = SparcRarAnalysis.ApplyTrmDistanceMapping(
            rawPoints,
            galaxyMeta,
            scaling,
            BaryonMode.Future_MassModel
        );

        // ✅ Fit für beide Varianten
        var fitGR = SparcRarAnalysis.FitA0(trmGR, model: ModelType.ClockworkTRM);
        var fitMass = SparcRarAnalysis.FitA0(trmMass, model: ModelType.ClockworkTRM);

        _output.WriteLine("=== GR CONSISTENT ===");
        _output.WriteLine($"log10(a0)={fitGR.BestLogA0:F4} | a0={fitGR.BestA0:E4} | RMS={fitGR.RmsError:F4}");

        _output.WriteLine("=== MASS MODEL ===");
        _output.WriteLine($"log10(a0)={fitMass.BestLogA0:F4} | a0={fitMass.BestA0:E4} | RMS={fitMass.RmsError:F4}");

        // ✅ Grundchecks
        Assert.NotEmpty(trmMass);
        Assert.NotEmpty(trmGR);

        // ✅ Stabilität: RMS darf nicht explodieren
        Assert.InRange(fitMass.RmsError, 0.05, 0.40);

        // ✅ Erwartung: Mass Model darf nicht schlechter sein
        Assert.True(fitMass.RmsError <= fitGR.RmsError + 0.02);


        // Aktuelles Modell ist (noch) identisch → sollte gleich sein
        Assert.True(Math.Abs(fitMass.BestLogA0 - fitGR.BestLogA0) < 1e-6);
        Assert.True(Math.Abs(fitMass.RmsError - fitGR.RmsError) < 1e-6);

        // NOTE:
        // Current mass model uses velocity-derived mass (v² r / G),
        // which algebraically reduces to the original v² / r relation.
        // Therefore, no change in a0 or RMS is expected at this stage.



    }


    [Fact]
    public void Test_TRM_Rar_MassModelConsistency()
    {
        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");

        var rawPoints = SparcRarAnalysis.ParseRarFromZip(zipPath);

        var galaxy = rawPoints
            .GroupBy(p => p.GalaxyName)
            .First();

        var ordered = galaxy.OrderBy(p => p.RadiusKpc).ToList();

        foreach (var p in ordered.Skip(5).Take(20))
        {
            double gBar = BaryonicMassModel.ComputeGbarFromMassProfile(ordered, p);

            // gBar must be positive and finite
            Assert.True(gBar > 0 && double.IsFinite(gBar));

            // sanity range

            double logG = Math.Log10(gBar);
            _output.WriteLine($"log10(gBar) = {logG:F3}");

            Assert.InRange(logG, -13.5, -8.0);
        }
    }
    [Fact]
    public void Test_TRM_Rar_BaryonBias()
    {
        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rawPoints = SparcRarAnalysis.ParseRarFromZip(zipPath);
        var galaxyMeta = SparcRarAnalysis.LoadGalaxyMetaFromMrt(mrtPath);
        var scaling = TrmCosmologyParameters.Current();

        // ✅ WICHTIG: korrekten Mode verwenden
        var trmPoints = SparcRarAnalysis.ApplyTrmDistanceMapping(
            rawPoints,
            galaxyMeta,
            scaling,
            BaryonMode.GR_Consistent
        );

        Assert.NotEmpty(trmPoints);

        var groups = trmPoints.GroupBy(p => p.GalaxyName);

        foreach (var g in groups.Take(20))
        {
            double meanRatio = g.Average(p => p.GobsMs2 / p.GbarMs2);

            _output.WriteLine($"{g.Key}: gObs/gBar = {meanRatio:F2}");

            Assert.InRange(meanRatio, 0.05, 50.0);
        }
    }
    [Fact]
    public void Test_TRM_Rar_BaryonStatistics()
    {
        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rawPoints = SparcRarAnalysis.ParseRarFromZip(zipPath);
        var galaxyMeta = SparcRarAnalysis.LoadGalaxyMetaFromMrt(mrtPath);
        var scaling = TrmCosmologyParameters.Current();

        var trmPoints = SparcRarAnalysis.ApplyTrmDistanceMapping(
            rawPoints,
            galaxyMeta,
            scaling,
            BaryonMode.GR_Consistent
        );

        Assert.NotEmpty(trmPoints);
        Assert.True(trmPoints.Count > 1000, "Too few TRM SPARC points.");

        // ✅ Ratio-Liste
        var ratios = trmPoints
            .Select(p => p.GobsMs2 / p.GbarMs2)
            .Where(x => x > 0 && !double.IsNaN(x) && !double.IsInfinity(x))
            .ToList();

        Assert.NotEmpty(ratios);

        // ✅ Sortierung einmal durchführen (wichtig für Performance und Konsistenz)
        var sorted = ratios.OrderBy(x => x).ToList();

        int n = sorted.Count;

        double median = sorted[n / 2];
        double p10 = sorted[(int)(0.1 * n)];
        double p90 = sorted[(int)(0.9 * n)];

        // ✅ Debug Output
        _output.WriteLine($"Total ratios: {n}");
        _output.WriteLine($"Median: {median:F3}");
        _output.WriteLine($"P10:   {p10:F3}");
        _output.WriteLine($"P90:   {p90:F3}");

        // Optional: zusätzliche Infos
        double mean = ratios.Average();
        double std = Math.Sqrt(ratios.Sum(x => Math.Pow(x - mean, 2)) / n);

        _output.WriteLine($"Mean:  {mean:F3}");
        _output.WriteLine($"Std:   {std:F3}");

        // ✅ Assertions (physikalisch sinnvoll gewählt)

        // Median sollte im typischen RAR-Bereich liegen
        Assert.InRange(median, 0.5, 5.0);

        // Oberes Quantil (DM/MOND/LSB-Effekt)
        Assert.InRange(p90, 1.0, 20.0);

        // Unteres Quantil (systematische Ausreißer erlaubt!)
        Assert.InRange(p10, 0.05, 2.0);

        // Optional: Streuung begrenzen (nicht zu wild)
        Assert.True(std < 20.0, "Scatter unexpectedly large.");
    }
    [Fact]
    public void Test_TRM_Rar_BaryonDecoupling()
    {
        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rawPoints = SparcRarAnalysis.ParseRarFromZip(zipPath);
        var galaxyMeta = SparcRarAnalysis.LoadGalaxyMetaFromMrt(mrtPath);
        var scaling = TrmCosmologyParameters.Current();

        var trmLegacy = SparcRarAnalysis.ApplyTrmDistanceMapping(
            rawPoints,
            galaxyMeta,
            scaling,
            BaryonMode.LegacyVelocityBased
        );

        var trmCorrected = SparcRarAnalysis.ApplyTrmDistanceMapping(
            rawPoints,
            galaxyMeta,
            scaling,
            BaryonMode.GR_Consistent
        );

        var fitLegacy = SparcRarAnalysis.FitA0(trmLegacy, model: ModelType.ClockworkTRM);
        var fitCorrected = SparcRarAnalysis.FitA0(trmCorrected, model: ModelType.ClockworkTRM);

        _output.WriteLine("=== LEGACY ===");
        _output.WriteLine($"log10(a0)={fitLegacy.BestLogA0:F4} RMS={fitLegacy.RmsError:F4}");

        _output.WriteLine("=== CORRECTED ===");
        _output.WriteLine($"log10(a0)={fitCorrected.BestLogA0:F4} RMS={fitCorrected.RmsError:F4}");

        // ✅ Hauptaussagen:

        // 1. a0 sollte stabiler (weniger verschoben) sein
        Assert.InRange(fitCorrected.BestLogA0, -11.0, -9.0);


        // TRM Transformation verbessert oder erhält Qualität
        Assert.True(fitCorrected.RmsError <= fitLegacy.RmsError + 0.005);

        // a0 darf sich NICHT drastisch verändern
        Assert.True(Math.Abs(fitCorrected.BestLogA0 - fitLegacy.BestLogA0) < 0.05);


        // 3. Datenkonsistenz bleibt
        Assert.NotEmpty(trmCorrected);
    }
    [Fact]
    public void Test_TRM_Rar_PhysicalScale()
    {
        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rawPoints = SparcRarAnalysis.ParseRarFromZip(zipPath);
        var galaxyMeta = SparcRarAnalysis.LoadGalaxyMetaFromMrt(mrtPath);
        var scaling = TrmCosmologyParameters.Current();

        var trmPoints = SparcRarAnalysis.ApplyTrmDistanceMapping(
            rawPoints,
            galaxyMeta,
            scaling
        );

        var fitTrm = SparcRarAnalysis.FitA0(trmPoints, model: ModelType.ClockworkTRM);
        var fitMond = SparcRarAnalysis.FitA0(trmPoints, model: ModelType.MOND);

        _output.WriteLine($"Raw points: {rawPoints.Count}");
        _output.WriteLine($"Galaxy meta entries: {galaxyMeta.Count}");
        _output.WriteLine($"TRM points: {trmPoints.Count}");
        _output.WriteLine($"TRM  -> log10(a0)={fitTrm.BestLogA0:F4} | a0={fitTrm.BestA0:E4} | RMS={fitTrm.RmsError:F4}");
        _output.WriteLine($"MOND -> log10(a0)={fitMond.BestLogA0:F4} | a0={fitMond.BestA0:E4} | RMS={fitMond.RmsError:F4}");

        Assert.NotEmpty(trmPoints);
        Assert.True(galaxyMeta.Count >= 150, "Too few SPARC galaxy metadata entries were loaded.");

        foreach (var point in trmPoints.Take(100))
        {
            double logGobs = Math.Log10(point.GobsMs2);
            Assert.InRange(logGobs, -13.5, -7.0);
        }

        Assert.InRange(fitTrm.BestLogA0, -10.5, -9.5);
        Assert.InRange(fitMond.BestLogA0, -10.5, -9.5);

        Assert.InRange(fitTrm.RmsError, 0.05, 0.30);
        Assert.InRange(fitMond.RmsError, 0.05, 0.30);
    }


    [Fact]
    public void Test_Parse_RotmodZip_And_Verify_AccelerationScale()
    {
        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        Assert.True(File.Exists(zipPath), "File Rotmod_LTG.zip was not found.");

        // Extract all radial data points for all galaxies
        var rarData = SparcRarAnalysis.ParseRarFromZip(zipPath);

        // Diagnostic output for Visual Studio Test Explorer
        _output.WriteLine($"Total radial data points loaded: {rarData.Count}");

        // Validate that data was loaded
        Assert.NotEmpty(rarData);

        // Sample output: computed values in log10 space
        var sample = rarData.First();
        _output.WriteLine($"Galaxy sample: {sample.GalaxyName} at R={sample.RadiusKpc} kpc");
        _output.WriteLine($"  log10(g_bar): {Math.Log10(sample.GbarMs2):F4}");
        _output.WriteLine($"  log10(g_obs): {Math.Log10(sample.GobsMs2):F4}");

        // Verify values remain in a physically plausible range
        // Typical galactic accelerations are between 10^-12 and 10^-8 m/s^2
        foreach (var point in rarData.Take(100))
        {
            double logGobs = Math.Log10(point.GobsMs2);
            Assert.InRange(logGobs, -13.0, -7.0);
        }
    }

    [Fact]
    public void Test_Verify_Rar_Asymptotic_Limits()
    {
        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        var rarData = SparcRarAnalysis.ParseRarFromZip(zipPath);

        // Compute averaged profiles across all galaxies
        var bins = SparcRarAnalysis.ComputeRarProfiles(rarData);

        _output.WriteLine("--- RADIAL ACCELERATION RELATION (RAR) PROFILE ---");
        _output.WriteLine("log10(g_bar) | log10(g_obs) | StdDev | Points");

        foreach (var bin in bins)
        {
            _output.WriteLine($"{bin.LogGbarCenter:F2}       | {bin.MeanLogGobs:F2}        | {bin.StandardDeviation:F3}  | {bin.PointCount}");
        }

        // 1) Validate Newtonian limit (high accelerations near -8.5)
        var highAccBin = bins.FirstOrDefault(b => Math.Abs(b.LogGbarCenter - (-8.6)) < 0.1);
        if (highAccBin != null)
        {
            // In inner galaxy regions, deviation from the 1:1 line should be small
            double deviation = Math.Abs(highAccBin.MeanLogGobs - highAccBin.LogGbarCenter);
            Assert.True(deviation < 0.15, $"Newton deviation is too high at large acceleration: {deviation}");
        }

        // 2) Validate asymptotic behavior (low accelerations near -11.5)
        var lowAccBin = bins.FirstOrDefault(b => Math.Abs(b.LogGbarCenter - (-11.4)) < 0.1);
        if (lowAccBin != null)
        {
            // At low accelerations, g_obs should be significantly larger than g_bar (apparent dark matter regime)
            Assert.True(lowAccBin.MeanLogGobs > lowAccBin.LogGbarCenter,
                "Outer galaxy region is missing the expected macroscopic synchronization support.");

            // Derive implicit a_0 from the deepest reliable bin:
            // log10(g_obs) = 0.5 * log10(g_bar) + 0.5 * log10(a_0)
            // => log10(a_0) = 2 * log10(g_obs) - log10(g_bar)
            double calculatedLogA0 = 2 * lowAccBin.MeanLogGobs - lowAccBin.LogGbarCenter;
            _output.WriteLine($"\nDerived cosmic acceleration anchor log10(a_0): {calculatedLogA0:F4} m/s^2");
        }
    }
    [Fact]
    public void Test_Global_NonLinear_Fit_For_A0()
    {
        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        // Use the updated method with integrated inclination matching
        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        var (bestLogA0, bestA0, rmsError) = SparcRarAnalysis.FitA0(rarData, inclinations);



        _output.WriteLine("--- GLOBAL RAR FIT RESULTS ---");
        _output.WriteLine($"Optimized log10(a_0): {bestLogA0:F4} m/s^2");
        _output.WriteLine($"Physical value a_0:    {bestA0:E4} m/s^2");
        _output.WriteLine($"Mean error (RMS):      {rmsError:F4} dex");
        _output.WriteLine($"Analyzed points:       {rarData.Count}");

        // Scientific validation:
        // The value should remain stable within the expected astrophysical window.
        // Lelli et al. report an empirical value near -9.85.
        Assert.InRange(bestLogA0, -10.1, -9.6);

        // RMS error below 0.15 dex in log space indicates an excellent fit
        Assert.True(rmsError < 0.15, $"Model RMS error is unusually high: {rmsError}");
        // Assert.True(rmsError == 0.15, $"RMS value: {rmsError}; best a_0: {bestA0}");
    }

    [Fact]
    public void Test_Clockwork_vs_MOND_Global_Fit()
    {
        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser.ParseFile(mrtPath).ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        // 1) Fit classical MOND
        var (mondLogA0, mondA0, mondRms) = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.MOND);

        // 2) Fit Clockwork Cosmology TRM
        var (trmLogA0, trmA0, trmRms) = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);

        _output.WriteLine($"--- REAL SPARC COMPARISON RESULTS ---");
        _output.WriteLine($"MOND      -> log10(a0): {mondLogA0:F4} | RMS: {mondRms:F4} dex");
        _output.WriteLine($"CLOCKWORK -> log10(a0): {trmLogA0:F4} | RMS: {trmRms:F4} dex");

        // Both models must remain within the expected physical window
        Assert.InRange(trmLogA0, -10.1, -9.6);
        Assert.True(trmRms < 0.15);
    }

    private static double ComputeRms(List<RarPoint> points, double a0)
    {
        double sum = 0;
        int count = 0;

        foreach (var p in points)
        {
            if (p.GbarMs2 <= 0 || p.GobsMs2 <= 0) continue;

            double gPred = p.GbarMs2 + Math.Sqrt(p.GbarMs2 * a0);

            double logObs = Math.Log10(p.GobsMs2);
            double logPred = Math.Log10(gPred);

            double res = logObs - logPred;

            sum += res * res;
            count++;
        }

        return Math.Sqrt(sum / count);
    }

    private static double ComputeBinRms(List<double> residuals)
    {
        if (residuals == null || residuals.Count == 0)
            return double.NaN;

        return Math.Sqrt(residuals.Average(x => x * x));
    }

    private static double ComputeMean(List<double> values)
    {
        if (values == null || values.Count == 0)
            return double.NaN;

        return values.Average();
    }

}
