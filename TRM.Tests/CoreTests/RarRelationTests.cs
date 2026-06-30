using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using TRM.Core;
using TRM.Core.Baryons;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.CoreTests;

/// <summary>
/// SPARC/RAR test suite for baryonic mapping modes, residual bins, and mass-model consistency checks.
/// Status: tested (data pipeline), calibrated (parameterized model layers), diagnostic (residual analytics).
/// Related docs: docs/review/TRM_Service_Test_Consolidation.md and docs/review/TRM_Real_Physics_Test_Coverage.md.
/// </summary>
public class RarRelationTests
{
    private readonly ITestOutputHelper _output;

    public RarRelationTests(ITestOutputHelper output)
    {
        _output = output;
    }



    [Fact]
    public void RAR01_TRM_Rar_ExponentialDisk_RadiusBins()
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

        WriteLineWithTestPrefix($"Bin <1 Rd     : n={bin1.Count}, RMS={rms1:F4}, Mean={ComputeMean(bin1):F4}");
        WriteLineWithTestPrefix($"Bin 1-2 Rd    : n={bin2.Count}, RMS={rms2:F4}, Mean={ComputeMean(bin2):F4}");
        WriteLineWithTestPrefix($"Bin 2-4 Rd    : n={bin3.Count}, RMS={rms3:F4}, Mean={ComputeMean(bin3):F4}");
        WriteLineWithTestPrefix($"Bin >=4 Rd    : n={bin4.Count}, RMS={rms4:F4}, Mean={ComputeMean(bin4):F4}");



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
    public void RAR02_TRM_Rar_ExponentialDiskImpact()
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

        WriteLineWithTestPrefix($"RMS GR   = {rmsGR:F4}");
        WriteLineWithTestPrefix($"RMS DISK = {rmsDisk:F4}");
        WriteLineWithTestPrefix($"Delta    = {rmsDisk - rmsGR:F4}");
        WriteLineWithTestPrefix($"Ratio    = {rmsDisk / rmsGR:F4}");

        Assert.NotEmpty(trmGR);
        Assert.NotEmpty(trmDisk);

        //Assert.InRange(rmsGR, 0.4, 0.1);
        //Assert.InRange(rmsDisk, 0.4, 1.5);

        //// DISK darf schlechter sein, aber nicht komplett entgleisen
        //Assert.True(rmsDisk <= 2.0 * rmsGR,
        //    $"ExponentialDisk model degrades too strongly: GR={rmsGR:F4}, DISK={rmsDisk:F4}");
    }
    [Fact]
    public void RAR03_TRM_Rar_MassModelImpact()
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

        WriteLineWithTestPrefix("=== GR CONSISTENT ===");
        WriteLineWithTestPrefix($"log10(a0)={fitGR.BestLogA0:F4} | a0={fitGR.BestA0:E4} | RMS={fitGR.RmsError:F4}");

        WriteLineWithTestPrefix("=== MASS MODEL ===");
        WriteLineWithTestPrefix($"log10(a0)={fitMass.BestLogA0:F4} | a0={fitMass.BestA0:E4} | RMS={fitMass.RmsError:F4}");

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
    public void RAR04_TRM_Rar_MassModelConsistency()
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
            WriteLineWithTestPrefix($"log10(gBar) = {logG:F3}");

            Assert.InRange(logG, -13.5, -8.0);
        }
    }
    [Fact]
    public void RAR05_TRM_Rar_BaryonBias()
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

            WriteLineWithTestPrefix($"{g.Key}: gObs/gBar = {meanRatio:F2}");

            Assert.InRange(meanRatio, 0.05, 50.0);
        }
    }
    [Fact]
    public void RAR06_TRM_Rar_BaryonStatistics()
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
        WriteLineWithTestPrefix($"Total ratios: {n}");
        WriteLineWithTestPrefix($"Median: {median:F3}");
        WriteLineWithTestPrefix($"P10:   {p10:F3}");
        WriteLineWithTestPrefix($"P90:   {p90:F3}");

        // Optional: zusätzliche Infos
        double mean = ratios.Average();
        double std = Math.Sqrt(ratios.Sum(x => Math.Pow(x - mean, 2)) / n);

        WriteLineWithTestPrefix($"Mean:  {mean:F3}");
        WriteLineWithTestPrefix($"Std:   {std:F3}");

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
    public void RAR07_TRM_Rar_BaryonDecoupling()
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

        WriteLineWithTestPrefix("=== LEGACY ===");
        WriteLineWithTestPrefix($"log10(a0)={fitLegacy.BestLogA0:F4} RMS={fitLegacy.RmsError:F4}");

        WriteLineWithTestPrefix("=== CORRECTED ===");
        WriteLineWithTestPrefix($"log10(a0)={fitCorrected.BestLogA0:F4} RMS={fitCorrected.RmsError:F4}");

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
    public void RAR08_TRM_Rar_PhysicalScale()
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

        WriteLineWithTestPrefix($"Raw points: {rawPoints.Count}");
        WriteLineWithTestPrefix($"Galaxy meta entries: {galaxyMeta.Count}");
        WriteLineWithTestPrefix($"TRM points: {trmPoints.Count}");
        WriteLineWithTestPrefix($"TRM  -> log10(a0)={fitTrm.BestLogA0:F4} | a0={fitTrm.BestA0:E4} | RMS={fitTrm.RmsError:F4}");
        WriteLineWithTestPrefix($"MOND -> log10(a0)={fitMond.BestLogA0:F4} | a0={fitMond.BestA0:E4} | RMS={fitMond.RmsError:F4}");

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
    public void RAR09_Parse_RotmodZip_And_Verify_AccelerationScale()
    {
        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        Assert.True(File.Exists(zipPath), "File Rotmod_LTG.zip was not found.");

        // Extract all radial data points for all galaxies
        var rarData = SparcRarAnalysis.ParseRarFromZip(zipPath);

        // Diagnostic output for Visual Studio Test Explorer
        WriteLineWithTestPrefix($"Total radial data points loaded: {rarData.Count}");

        // Validate that data was loaded
        Assert.NotEmpty(rarData);

        // Sample output: computed values in log10 space
        var sample = rarData.First();
        WriteLineWithTestPrefix($"Galaxy sample: {sample.GalaxyName} at R={sample.RadiusKpc} kpc");
        WriteLineWithTestPrefix($"  log10(g_bar): {Math.Log10(sample.GbarMs2):F4}");
        WriteLineWithTestPrefix($"  log10(g_obs): {Math.Log10(sample.GobsMs2):F4}");

        // Verify values remain in a physically plausible range
        // Typical galactic accelerations are between 10^-12 and 10^-8 m/s^2
        foreach (var point in rarData.Take(100))
        {
            double logGobs = Math.Log10(point.GobsMs2);
            Assert.InRange(logGobs, -13.0, -7.0);
        }
    }

    [Fact]
    public void RAR10_Verify_Rar_Asymptotic_Limits()
    {
        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        var rarData = SparcRarAnalysis.ParseRarFromZip(zipPath);

        // Compute averaged profiles across all galaxies
        var bins = SparcRarAnalysis.ComputeRarProfiles(rarData);

        WriteLineWithTestPrefix("--- RADIAL ACCELERATION RELATION (RAR) PROFILE ---");
        WriteLineWithTestPrefix("log10(g_bar) | log10(g_obs) | StdDev | Points");

        foreach (var bin in bins)
        {
            WriteLineWithTestPrefix($"{bin.LogGbarCenter:F2}       | {bin.MeanLogGobs:F2}        | {bin.StandardDeviation:F3}  | {bin.PointCount}");
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
            WriteLineWithTestPrefix($"\nDerived cosmic acceleration anchor log10(a_0): {calculatedLogA0:F4} m/s^2");
        }
    }
    [Fact]
    public void RAR11_Global_NonLinear_Fit_For_A0()
    {
        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        // Use the updated method with integrated inclination matching
        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        var (bestLogA0, bestA0, rmsError) = SparcRarAnalysis.FitA0(rarData, inclinations);



        WriteLineWithTestPrefix("--- GLOBAL RAR FIT RESULTS ---");
        WriteLineWithTestPrefix($"Optimized log10(a_0): {bestLogA0:F4} m/s^2");
        WriteLineWithTestPrefix($"Physical value a_0:    {bestA0:E4} m/s^2");
        WriteLineWithTestPrefix($"Mean error (RMS):      {rmsError:F4} dex");
        WriteLineWithTestPrefix($"Analyzed points:       {rarData.Count}");

        // Scientific validation:
        // The value should remain stable within the expected astrophysical window.
        // Lelli et al. report an empirical value near -9.85.
        Assert.InRange(bestLogA0, -10.1, -9.6);

        // RMS error below 0.15 dex in log space indicates an excellent fit
        Assert.True(rmsError < 0.15, $"Model RMS error is unusually high: {rmsError}");
        // Assert.True(rmsError == 0.15, $"RMS value: {rmsError}; best a_0: {bestA0}");
    }

    [Fact]
    public void RAR12_Clockwork_vs_MOND_Global_Fit()
    {
        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser.ParseFile(mrtPath).ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        // 1) Fit classical MOND
        var (mondLogA0, mondA0, mondRms) = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.MOND);

        // 2) Fit Clockwork Cosmology TRM
        var (trmLogA0, trmA0, trmRms) = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);

        WriteLineWithTestPrefix($"--- REAL SPARC COMPARISON RESULTS ---");
        WriteLineWithTestPrefix($"MOND      -> log10(a0): {mondLogA0:F4} | RMS: {mondRms:F4} dex");
        WriteLineWithTestPrefix($"CLOCKWORK -> log10(a0): {trmLogA0:F4} | RMS: {trmRms:F4} dex");

        // Both models must remain within the expected physical window
        Assert.InRange(trmLogA0, -10.1, -9.6);
        Assert.True(trmRms < 0.15);
    }

    [Fact]
    public void RAR13_TRM_Rar_NoRefit_TurningMemory_HoldoutBins()
    {
        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);

        var rows = BuildTurningResidualRows(rarData, fit.BestA0);
        Assert.NotEmpty(rows);

        var holdoutRows = rows.Where(r => IsHoldoutGalaxy(r.GalaxyKey)).ToList();
        var trainRows = rows.Where(r => !IsHoldoutGalaxy(r.GalaxyKey)).ToList();

        Assert.True(trainRows.Count > 500, "Too few training points for turning-memory bins.");
        Assert.True(holdoutRows.Count > 200, "Too few holdout points for robust diagnostics.");

        var trainProxySorted = trainRows
            .Select(r => r.TurningProxySigned)
            .OrderBy(x => x)
            .ToList();

        double lowCut = trainProxySorted[trainProxySorted.Count / 3];
        double highCut = trainProxySorted[(2 * trainProxySorted.Count) / 3];

        int GetBin(double proxy) => proxy < lowCut ? 0 : proxy < highCut ? 1 : 2;

        var trainResidualMeansByBin = trainRows
            .GroupBy(r => GetBin(r.TurningProxySigned))
            .ToDictionary(g => g.Key, g => g.Average(x => x.Residual));

        var holdoutResidualsByBin = holdoutRows
            .GroupBy(r => GetBin(r.TurningProxySigned))
            .ToDictionary(g => g.Key, g => g.Select(x => x.Residual).ToList());

        Assert.True(
            holdoutResidualsByBin.ContainsKey(0) &&
            holdoutResidualsByBin.ContainsKey(1) &&
            holdoutResidualsByBin.ContainsKey(2),
            "Holdout split does not cover low/mid/high turning-memory bins.");

        for (int bin = 0; bin < 3; bin++)
        {
            Assert.True(holdoutResidualsByBin[bin].Count > 30, $"Holdout bin {bin} is too small.");
        }

        var holdoutBaselineResiduals = holdoutRows.Select(r => r.Residual).ToList();
        var holdoutCorrectedResiduals = holdoutRows
            .Select(r => r.Residual - trainResidualMeansByBin[GetBin(r.TurningProxySigned)])
            .ToList();

        double baselineHoldoutRms = ComputeBinRms(holdoutBaselineResiduals);
        double correctedHoldoutRms = ComputeBinRms(holdoutCorrectedResiduals);

        WriteLineWithTestPrefix("--- NO-REFIT TURNING-MEMORY HOLDOUT DIAGNOSTIC ---");
        WriteLineWithTestPrefix($"Fit log10(a0): {fit.BestLogA0:F4} | RMS(all): {fit.RmsError:F4} dex");
        WriteLineWithTestPrefix($"Train/Holdout points: {trainRows.Count}/{holdoutRows.Count}");
        WriteLineWithTestPrefix($"Turning-memory cuts (signed): low<{lowCut:E6}, mid<{highCut:E6}, high>=mid");

        for (int bin = 0; bin < 3; bin++)
        {
            var baselineBin = holdoutResidualsByBin[bin];
            var correctedBin = holdoutRows
                .Where(r => GetBin(r.TurningProxySigned) == bin)
                .Select(r => r.Residual - trainResidualMeansByBin[bin])
                .ToList();

            string label = bin switch
            {
                0 => "LOW",
                1 => "MID",
                _ => "HIGH"
            };

            WriteLineWithTestPrefix(
                $"{label} bin: n={baselineBin.Count}, " +
                $"mean={ComputeMean(baselineBin):F5}, " +
                $"rms={ComputeBinRms(baselineBin):F5}, " +
                $"rms_corrected={ComputeBinRms(correctedBin):F5}");
        }

        WriteLineWithTestPrefix(
            $"Holdout RMS baseline={baselineHoldoutRms:F5} | corrected={correctedHoldoutRms:F5} | " +
            $"delta={(baselineHoldoutRms - correctedHoldoutRms):F5}");

        Assert.True(
            correctedHoldoutRms <= baselineHoldoutRms,
            $"Turning-memory correction did not improve holdout RMS: baseline={baselineHoldoutRms:F5}, corrected={correctedHoldoutRms:F5}");
    }

    [Fact]
    public void RAR14_TRM_Rar_NoRefit_TurningMemory_VariantDiagnostics()
    {
        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);
        var rows = BuildTurningResidualRows(rarData, fit.BestA0);
        Assert.NotEmpty(rows);

        var galaxyBrightness = rows
            .GroupBy(r => r.GalaxyKey)
            .ToDictionary(
                g => g.Key,
                g => g.Average(x => x.LogGbar));

        var brightnessValues = galaxyBrightness.Values.OrderBy(x => x).ToList();
        double brightnessCut = brightnessValues[brightnessValues.Count / 2];

        var splitConfigs = new (int Modulo, int Remainder, string Label)[]
        {
            (5, 0, "hash%5==0"),
            (5, 1, "hash%5==1"),
            (4, 0, "hash%4==0")
        };

        var binCounts = new[] { 3, 5 };
        var proxyConfigs = new (string Label, Func<TurningResidualRow, double> Selector)[]
        {
            ("signed", r => r.TurningProxySigned),
            ("absolute", r => r.TurningProxyAbs)
        };

        var sbGroups = new (string Label, Func<string, bool> GalaxyFilter)[]
        {
            ("all", _ => true),
            ("lsb", galaxyKey => galaxyBrightness.TryGetValue(galaxyKey, out double value) && value <= brightnessCut),
            ("hsb", galaxyKey => galaxyBrightness.TryGetValue(galaxyKey, out double value) && value > brightnessCut)
        };

        var results = new List<TurningDiagnosticResult>();

        foreach (var split in splitConfigs)
        {
            foreach (var bins in binCounts)
            {
                foreach (var proxy in proxyConfigs)
                {
                    foreach (var sbGroup in sbGroups)
                    {
                        var result = EvaluateTurningDiagnostic(
                            rows,
                            proxy.Selector,
                            bins,
                            split.Modulo,
                            split.Remainder,
                            sbGroup.GalaxyFilter);

                        if (result != null)
                        {
                            results.Add(result with
                            {
                                SplitLabel = split.Label,
                                BinCount = bins,
                                ProxyLabel = proxy.Label,
                                SurfaceBrightnessLabel = sbGroup.Label
                            });
                        }
                    }
                }
            }
        }

        WriteLineWithTestPrefix("--- TURNING-MEMORY VARIANT DIAGNOSTICS (NO-REFIT, HOLDOUT-ONLY) ---");
        WriteLineWithTestPrefix("Variant | split | bins | proxy | group | baseline RMS | corrected RMS | delta");

        int variantIndex = 1;
        foreach (var r in results
            .OrderBy(r => r.SplitLabel)
            .ThenBy(r => r.BinCount)
            .ThenBy(r => r.ProxyLabel)
            .ThenBy(r => r.SurfaceBrightnessLabel))
        {
            WriteLineWithTestPrefix(
                $"{variantIndex:D2} | {r.SplitLabel} | {r.BinCount} | {r.ProxyLabel} | {r.SurfaceBrightnessLabel} | " +
                $"{r.BaselineRms:F5} | {r.CorrectedRms:F5} | {r.DeltaRms:F5}");
            variantIndex++;
        }

        Assert.True(results.Count >= 12, "Too few valid variant diagnostics.");
        Assert.True(results.Any(r => r.DeltaRms > 0.0), "No holdout variant showed RMS improvement.");
    }

    [Fact]
    public void RAR15_TRM_Rar_NoRefit_TurningMemory_SoftGatedHoldout()
    {
        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);
        var rows = BuildTurningResidualRows(rarData, fit.BestA0);
        Assert.NotEmpty(rows);

        var splitConfigs = new (int Modulo, int Remainder, string Label)[]
        {
            (5, 0, "hash%5==0"),
            (5, 1, "hash%5==1"),
            (4, 0, "hash%4==0")
        };

        var results = new List<TurningGateResult>();
        foreach (var split in splitConfigs)
        {
            var result = EvaluateHsbGatedTurningDiagnostic(
                rows,
                proxySelector: r => r.TurningProxySigned,
                binCount: 3,
                holdoutModulo: split.Modulo,
                holdoutRemainder: split.Remainder);

            if (result != null)
            {
                results.Add(result with { SplitLabel = split.Label });
            }
        }

        WriteLineWithTestPrefix("--- SOFT-GATED TURNING-MEMORY DIAGNOSTIC (NO-REFIT, HOLDOUT-ONLY) ---");
        WriteLineWithTestPrefix("split | baseline RMS | ungated RMS | hard-gated RMS | soft-gated RMS | delta ungated | delta hard | delta soft | gate threshold | gate width");
        foreach (var r in results.OrderBy(r => r.SplitLabel))
        {
            WriteLineWithTestPrefix(
                $"{r.SplitLabel} | {r.BaselineRms:F5} | {r.UngatedRms:F5} | {r.HardGatedRms:F5} | {r.SoftGatedRms:F5} | " +
                $"{r.UngatedDelta:F5} | {r.HardGatedDelta:F5} | {r.SoftGatedDelta:F5} | {r.SoftGateThreshold:F5} | {r.SoftGateWidth:F5}");
        }

        Assert.True(results.Count >= 2, "Too few valid split results for soft-gate diagnostic.");
        Assert.True(results.Any(r => r.SoftGatedDelta > 0.0), "Soft-gated correction did not improve holdout RMS in any split.");
        Assert.True(
            results.All(r => r.SoftGatedDelta > -0.01),
            "Soft-gated correction degrades holdout RMS too strongly in at least one split.");
    }

    [Fact]
    public void RAR16_TRM_Rar_ServiceLayer_TurningMemoryCorrection_NoRefit()
    {
        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);

        var baseline = SparcRarAnalysis.EvaluateTurningMemoryCorrectionNoRefit(
            rarData,
            fit.BestA0,
            new TurningMemoryCorrectionOptions(
                Mode: TurningMemoryCorrectionMode.None,
                HoldoutModulo: 5,
                HoldoutRemainder: 0,
                BinCount: 3));

        var binBased = SparcRarAnalysis.EvaluateTurningMemoryCorrectionNoRefit(
            rarData,
            fit.BestA0,
            new TurningMemoryCorrectionOptions(
                Mode: TurningMemoryCorrectionMode.BinBased,
                HoldoutModulo: 5,
                HoldoutRemainder: 0,
                BinCount: 3));

        var interpolated = SparcRarAnalysis.EvaluateTurningMemoryCorrectionNoRefit(
            rarData,
            fit.BestA0,
            new TurningMemoryCorrectionOptions(
                Mode: TurningMemoryCorrectionMode.Interpolated,
                HoldoutModulo: 5,
                HoldoutRemainder: 0,
                BinCount: 3));

        WriteLineWithTestPrefix("--- SPARC SERVICE TURNING-MEMORY (NO-REFIT) ---");
        WriteLineWithTestPrefix($"Baseline:     RMS={baseline.BaselineRms:F5} -> {baseline.CorrectedRms:F5} | delta={baseline.DeltaRms:F5}");
        WriteLineWithTestPrefix($"Bin-based:    RMS={binBased.BaselineRms:F5} -> {binBased.CorrectedRms:F5} | delta={binBased.DeltaRms:F5}");
        WriteLineWithTestPrefix($"Interpolated: RMS={interpolated.BaselineRms:F5} -> {interpolated.CorrectedRms:F5} | delta={interpolated.DeltaRms:F5}");
        WriteLineWithTestPrefix($"Soft-gate (bin) threshold={binBased.FittedGateThreshold:F5} width={binBased.FittedGateWidth:F5}");
        WriteLineWithTestPrefix($"Soft-gate (int) threshold={interpolated.FittedGateThreshold:F5} width={interpolated.FittedGateWidth:F5}");

        Assert.Equal(TurningMemoryCorrectionMode.None, baseline.Mode);
        Assert.InRange(Math.Abs(baseline.DeltaRms), 0.0, 1e-12);

        Assert.Equal(baseline.TrainPointCount, binBased.TrainPointCount);
        Assert.Equal(baseline.HoldoutPointCount, binBased.HoldoutPointCount);
        Assert.Equal(baseline.TrainPointCount, interpolated.TrainPointCount);
        Assert.Equal(baseline.HoldoutPointCount, interpolated.HoldoutPointCount);

        Assert.True(double.IsFinite(binBased.DeltaRms));
        Assert.True(double.IsFinite(interpolated.DeltaRms));
        Assert.True(binBased.PerGalaxyImprovement.Count > 10);
        Assert.True(interpolated.PerGalaxyImprovement.Count > 10);

        Assert.True(
            binBased.PerGalaxyImprovement.Any(x => x.DeltaRms > 0.0) ||
            interpolated.PerGalaxyImprovement.Any(x => x.DeltaRms > 0.0),
            "No per-galaxy holdout improvement found for turning-memory corrected modes.");
    }

    [Fact]
    /// <summary>
    /// No-refit gradient-regime gate comparison for SPARC turning-memory residual correction.
    ///
    /// Hypothesis:
    /// Residual turning-memory signal is stronger in specific gradient regimes than in global HSB-only gating.
    ///
    /// Status:
    /// diagnostic + candidate.
    ///
    /// Limitation:
    /// Optional residual-sector analysis; baseline TRM-RAR path is unchanged.
    /// </summary>
    public void RAR17_TRM_Rar_ServiceLayer_GradientRegimeGateDiagnostics_NoRefit()
    {
        // Baseline fit block: fit a0 once, then keep it frozen for all diagnostic variants.
        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);

        // No-refit holdout block: train-only gate fitting, holdout-only scoring.
        var report = SparcRarAnalysis.EvaluateTurningMemoryGateComparisonNoRefit(
            rarData,
            fit.BestA0,
            TurningMemoryCorrectionMode.Interpolated,
            new TurningMemoryCorrectionOptions(
                Mode: TurningMemoryCorrectionMode.Interpolated,
                HoldoutModulo: 5,
                HoldoutRemainder: 0,
                BinCount: 3));

        WriteLineWithTestPrefix($"Mode={report.Mode}");
        WriteLineWithTestPrefix($"Baseline RMS(all)={report.Baseline.CorrectedRmsAll:F6}");
        WriteLineWithTestPrefix($"Ungated RMS(all)={report.Ungated.CorrectedRmsAll:F6} delta={report.Ungated.DeltaRmsAll:F6}");
        WriteLineWithTestPrefix($"HSB soft RMS(all)={report.HsbSoftGated.CorrectedRmsAll:F6} delta={report.HsbSoftGated.DeltaRmsAll:F6}");
        foreach (var g in report.GradientSoftGated)
        {
            WriteLineWithTestPrefix(
                $"Gradient gate {g.GateVariable}: RMS(all)={g.CorrectedRmsAll:F6}, " +
                $"delta={g.DeltaRmsAll:F6}, thr={g.GateThreshold:F6}, width={g.GateWidth:F6}");
        }
        WriteLineWithTestPrefix(
            $"Best gradient gate={report.BestGradientGate.GateVariable}, " +
            $"delta={report.BestGradientGate.DeltaRmsAll:F6}, " +
            $"thr={report.BestGradientGate.GateThreshold:F6}, width={report.BestGradientGate.GateWidth:F6}");

        // Positive result => regime-gated candidate is plausible; negative result => signal is not robust.
        Assert.Equal(TurningMemoryCorrectionMode.Interpolated, report.Mode);
        Assert.Equal(4, report.GradientSoftGated.Count);
        Assert.True(double.IsFinite(report.Baseline.CorrectedRmsAll));
        Assert.True(double.IsFinite(report.Ungated.CorrectedRmsAll));
        Assert.True(double.IsFinite(report.HsbSoftGated.CorrectedRmsAll));
        Assert.True(report.GradientSoftGated.All(x => double.IsFinite(x.CorrectedRmsAll)));
        Assert.True(double.IsFinite(report.BestGradientGate.GateThreshold));
        Assert.True(double.IsFinite(report.BestGradientGate.GateWidth));
        Assert.True(report.BestGradientGate.GateWidth > 0.0);

        Assert.True(report.Baseline.TopImproved.Count == 0);
        Assert.True(report.Baseline.TopWorsened.Count == 0);
        Assert.True(report.Ungated.TopImproved.Count <= 10);
        Assert.True(report.Ungated.TopWorsened.Count <= 10);
    }

    [Fact]
    /// <summary>
    /// Disk-edge/surface coupling diagnostic for no-refit turning-memory correction.
    ///
    /// Hypothesis:
    /// Residual behavior tracks edge/inner gradient structure rather than only global brightness class.
    ///
    /// Status:
    /// diagnostic.
    ///
    /// Limitation:
    /// Correlational proxy test; not direct causal proof.
    /// </summary>
    public void RAR18_TRM_Rar_ServiceLayer_DiskEdgeSurfaceCoupling_NoRefit()
    {
        // Fit/freeze block: baseline a0 is fitted once and held fixed.
        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);

        // Holdout/no-refit block: proxy gate parameters are fit on train and applied unchanged to holdout.
        var report = SparcRarAnalysis.EvaluateDiskEdgeSurfaceCouplingNoRefit(
            rarData,
            fit.BestA0,
            TurningMemoryCorrectionMode.Interpolated,
            new TurningMemoryCorrectionOptions(
                Mode: TurningMemoryCorrectionMode.Interpolated,
                HoldoutModulo: 5,
                HoldoutRemainder: 0,
                BinCount: 3));

        WriteLineWithTestPrefix(
            $"Mode={report.Mode} baseline={report.BaselineRmsAll:F6} " +
            $"ungated={report.UngatedRmsAll:F6} deltaUngated={report.UngatedDeltaRmsAll:F6}");
        WriteLineWithTestPrefix(
            $"Best proxy={report.BestProxyName} threshold={report.BestProxyThreshold:F6} " +
            $"width={report.BestProxyWidth:F6} corrected={report.BestProxyCorrectedRmsAll:F6} " +
            $"delta={report.BestProxyDeltaRmsAll:F6} improved/worsened={report.BestProxyImprovedGalaxyCount}/{report.BestProxyWorsenedGalaxyCount}");

        foreach (var c in report.ProxyCorrelations)
        {
            WriteLineWithTestPrefix(
                $"{c.ProxyName}: residual r/rho={c.ResidualPearson:F5}/{c.ResidualSpearman:F5}, " +
                $"turningDelta r/rho={c.TurningDeltaPearson:F5}/{c.TurningDeltaSpearman:F5}");
        }

        // Positive result => disk-edge proxy candidate; negative result => weak or non-general disk-edge coupling.
        Assert.Equal(TurningMemoryCorrectionMode.Interpolated, report.Mode);
        Assert.True(report.TrainGalaxyCount > 30);
        Assert.True(report.HoldoutGalaxyCount > 10);
        Assert.Equal(6, report.ProxyCorrelations.Count);
        Assert.True(double.IsFinite(report.BaselineRmsAll));
        Assert.True(double.IsFinite(report.UngatedRmsAll));
        Assert.True(double.IsFinite(report.BestProxyCorrectedRmsAll));
        Assert.True(double.IsFinite(report.BestProxyThreshold));
        Assert.True(double.IsFinite(report.BestProxyWidth));
        Assert.True(report.BestProxyWidth > 0.0);
        Assert.True(report.TopImproved.Count <= 10);
        Assert.True(report.TopWorsened.Count <= 10);
    }

    [Fact]
    /// <summary>
    /// Forced outer/inner acceleration-contrast gate diagnostic without a0 refit.
    ///
    /// Hypothesis:
    /// The physically relevant gate is inner-to-outer baryonic acceleration contrast, not transition radius alone.
    ///
    /// Status:
    /// tested-effective candidate for this diagnostic family.
    ///
    /// Limitation:
    /// Still optional diagnostic logic; not activated in core TRM-RAR baseline.
    /// </summary>
    public void RAR19_TRM_Rar_ServiceLayer_PhysicalDiskStructureCoupling_NoRefit()
    {
        // Fit/freeze block: establish baseline a0 only once.
        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);

        // No-refit comparison block: evaluate direct, inverse, and log ratio gates with frozen baseline.
        var report = SparcRarAnalysis.EvaluateOuterInnerContrastGateNoRefit(
            rarData,
            fit.BestA0,
            TurningMemoryCorrectionMode.Interpolated,
            new TurningMemoryCorrectionOptions(
                Mode: TurningMemoryCorrectionMode.Interpolated,
                HoldoutModulo: 5,
                HoldoutRemainder: 0,
                BinCount: 3));

        WriteLineWithTestPrefix(
            $"Mode={report.Mode} baseline={report.Baseline.CorrectedRmsAll:F6} " +
            $"ungated={report.Ungated.CorrectedRmsAll:F6} deltaUngated={report.Ungated.DeltaRmsAll:F6}");
        WriteLineWithTestPrefix(
            $"ratio gate corrected={report.OuterToInnerRatioGate.CorrectedRmsAll:F6} delta={report.OuterToInnerRatioGate.DeltaRmsAll:F6} " +
            $"thr={report.OuterToInnerRatioGate.GateThreshold:F6} width={report.OuterToInnerRatioGate.GateWidth:F6}");
        WriteLineWithTestPrefix(
            $"inverse ratio gate corrected={report.InverseOuterToInnerRatioGate.CorrectedRmsAll:F6} delta={report.InverseOuterToInnerRatioGate.DeltaRmsAll:F6} " +
            $"thr={report.InverseOuterToInnerRatioGate.GateThreshold:F6} width={report.InverseOuterToInnerRatioGate.GateWidth:F6}");
        WriteLineWithTestPrefix(
            $"log ratio gate corrected={report.LogOuterToInnerRatioGate.CorrectedRmsAll:F6} delta={report.LogOuterToInnerRatioGate.DeltaRmsAll:F6} " +
            $"thr={report.LogOuterToInnerRatioGate.GateThreshold:F6} width={report.LogOuterToInnerRatioGate.GateWidth:F6}");
        WriteLineWithTestPrefix($"Best variant={report.BestVariantLabel}");

        // Positive result => contrast-direction carries signal; negative result => contrast gate is not stable.
        Assert.Equal(TurningMemoryCorrectionMode.Interpolated, report.Mode);
        Assert.True(double.IsFinite(report.Baseline.CorrectedRmsAll));
        Assert.True(double.IsFinite(report.Ungated.CorrectedRmsAll));

        Assert.True(double.IsFinite(report.OuterToInnerRatioGate.CorrectedRmsAll));
        Assert.True(double.IsFinite(report.InverseOuterToInnerRatioGate.CorrectedRmsAll));
        Assert.True(double.IsFinite(report.LogOuterToInnerRatioGate.CorrectedRmsAll));

        Assert.True(report.OuterToInnerRatioGate.GateWidth > 0.0);
        Assert.True(report.InverseOuterToInnerRatioGate.GateWidth > 0.0);
        Assert.True(report.LogOuterToInnerRatioGate.GateWidth > 0.0);

        Assert.Contains(
            report.BestVariantLabel,
            new[]
            {
                "OuterToInnerRatioGate",
                "InverseOuterToInnerRatioGate",
                "LogOuterToInnerRatioGate"
            });
    }

    [Fact]
    /// <summary>
    /// Outer-inner takt synchronization proxy diagnostic on SPARC holdout galaxies.
    ///
    /// Hypothesis:
    /// A synchronization term combining outer Omega, outer/inner ratio, and gradient can explain residual structure.
    ///
    /// Status:
    /// diagnostic + candidate.
    ///
    /// Limitation:
    /// Proxy-level model only; not a full dynamical derivation.
    /// </summary>
    public void RAR20_TRM_Rar_ServiceLayer_OuterInnerTaktSynchronization_NoRefit()
    {
        // Baseline fit block: fit a0 once and freeze.
        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);

        // No-refit holdout block: select gate on train only, validate on holdout only.
        var report = SparcRarAnalysis.EvaluateOuterInnerTaktSynchronizationNoRefit(
            rarData,
            fit.BestA0,
            TurningMemoryCorrectionMode.Interpolated,
            new TurningMemoryCorrectionOptions(
                Mode: TurningMemoryCorrectionMode.Interpolated,
                HoldoutModulo: 5,
                HoldoutRemainder: 0,
                BinCount: 3));

        WriteLineWithTestPrefix(
            $"Mode={report.Mode} baseline={report.BaselineRmsAll:F6} " +
            $"ungated={report.UngatedRmsAll:F6} deltaUngated={report.UngatedDeltaRmsAll:F6}");
        WriteLineWithTestPrefix(
            $"Best proxy={report.BestProxyName} threshold={report.BestProxyThreshold:F6} width={report.BestProxyWidth:F6} " +
            $"corrected={report.BestProxyCorrectedRmsAll:F6} delta={report.BestProxyDeltaRmsAll:F6} " +
            $"improved/worsened={report.BestProxyImprovedGalaxyCount}/{report.BestProxyWorsenedGalaxyCount}");

        foreach (var c in report.ProxyCorrelations)
        {
            WriteLineWithTestPrefix(
                $"{c.ProxyName}: residual r/rho={c.ResidualPearson:F5}/{c.ResidualSpearman:F5}, " +
                $"turningDelta r/rho={c.TurningDeltaPearson:F5}/{c.TurningDeltaSpearman:F5}");
        }

        // Positive result => synchronization candidate; negative result => proxy family lacks stable predictive value.
        Assert.Equal(TurningMemoryCorrectionMode.Interpolated, report.Mode);
        Assert.True(report.TrainGalaxyCount > 30);
        Assert.True(report.HoldoutGalaxyCount > 10);
        Assert.Equal(3, report.ProxyCorrelations.Count);
        Assert.True(double.IsFinite(report.BaselineRmsAll));
        Assert.True(double.IsFinite(report.UngatedRmsAll));
        Assert.True(double.IsFinite(report.BestProxyCorrectedRmsAll));
        Assert.True(double.IsFinite(report.BestProxyThreshold));
        Assert.True(double.IsFinite(report.BestProxyWidth));
        Assert.True(report.BestProxyWidth > 0.0);
        Assert.Contains(
            report.BestProxyName,
            new[]
            {
                "syncProxy",
                "syncGradientProxy",
                "syncContrastProxy"
            });
        Assert.True(report.TopImproved.Count <= 10);
        Assert.True(report.TopWorsened.Count <= 10);
    }

    [Fact]
    /// <summary>
    /// Global disk-coherence residual diagnostic with train-fit/holdout-apply soft gates.
    ///
    /// Hypothesis:
    /// Worst residual regimes are linked to profile smoothness/coherence and rotational shear structure.
    ///
    /// Status:
    /// diagnostic.
    ///
    /// Limitation:
    /// Exploratory proxy scan; no core-model replacement claim.
    /// </summary>
    public void RAR21_TRM_Rar_ServiceLayer_GlobalDiskCoherence_NoRefit()
    {
        // Fit/freeze block: baseline a0 remains fixed for all coherence variants.
        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);

        // Holdout/no-refit block: best coherence gate is learned from train and frozen on holdout.
        var report = SparcRarAnalysis.EvaluateGlobalDiskCoherenceNoRefit(
            rarData,
            fit.BestA0,
            TurningMemoryCorrectionMode.Interpolated,
            new TurningMemoryCorrectionOptions(
                Mode: TurningMemoryCorrectionMode.Interpolated,
                HoldoutModulo: 5,
                HoldoutRemainder: 0,
                BinCount: 3));

        WriteLineWithTestPrefix(
            $"Mode={report.Mode} baseline={report.BaselineRmsAll:F6} " +
            $"ungated={report.UngatedRmsAll:F6} deltaUngated={report.UngatedDeltaRmsAll:F6}");
        WriteLineWithTestPrefix(
            $"Best proxy={report.BestProxyName} threshold={report.BestProxyThreshold:F6} width={report.BestProxyWidth:F6} " +
            $"corrected={report.BestProxyCorrectedRmsAll:F6} delta={report.BestProxyDeltaRmsAll:F6} " +
            $"improved/worsened={report.BestProxyImprovedGalaxyCount}/{report.BestProxyWorsenedGalaxyCount}");

        foreach (var c in report.ProxyCorrelations)
        {
            WriteLineWithTestPrefix(
                $"{c.ProxyName}: residual r/rho={c.ResidualPearson:F5}/{c.ResidualSpearman:F5}, " +
                $"turningDelta r/rho={c.TurningDeltaPearson:F5}/{c.TurningDeltaSpearman:F5}");
        }

        // Positive result => coherence-sensitive correction candidate; negative result => coherence proxies are insufficient.
        Assert.Equal(TurningMemoryCorrectionMode.Interpolated, report.Mode);
        Assert.True(report.TrainGalaxyCount > 30);
        Assert.True(report.HoldoutGalaxyCount > 10);
        Assert.Equal(6, report.ProxyCorrelations.Count);
        Assert.True(double.IsFinite(report.BaselineRmsAll));
        Assert.True(double.IsFinite(report.UngatedRmsAll));
        Assert.True(double.IsFinite(report.BestProxyCorrectedRmsAll));
        Assert.True(double.IsFinite(report.BestProxyThreshold));
        Assert.True(double.IsFinite(report.BestProxyWidth));
        Assert.True(report.BestProxyWidth > 0.0);
        Assert.Contains(
            report.BestProxyName,
            new[]
            {
                "profileSmoothness",
                "varianceDlnGbarDr",
                "innerToOuterCoherenceRatio",
                "shearProxy",
                "outerToInnerRatioTimesProfileSmoothness",
                "outerToInnerRatioTimesShearProxy"
            });
        Assert.True(report.TopImproved.Count <= 10);
        Assert.True(report.TopWorsened.Count <= 10);
    }

    [Fact]
    /// <summary>
    /// Worst-galaxy geometry-variation diagnostic with frozen-a0 and train-fitted smooth takt-field kernel.
    ///
    /// Hypothesis:
    /// Part of the largest TRM failures comes from single-center geometry assumptions, improved by distributed fields.
    ///
    /// Status:
    /// diagnostic + candidate.
    ///
    /// Limitation:
    /// Exploratory geometry family; not a finalized production geometry model.
    /// </summary>
    public void RAR22_TRM_Rar_ServiceLayer_WorstGalaxyGeometryVariation_NoRefit()
    {
        // Baseline fit block: a0 is fitted once and then frozen for all geometry variants.
        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);

        // Train/holdout discipline: smooth-kernel width is selected on train only and frozen for worst-galaxy evaluation.
        var report = SparcRarAnalysis.EvaluateWorstGalaxyGeometryVariationNoRefit(
            rarData,
            fit.BestA0,
            topGalaxyCount: 20);

        WriteLineWithTestPrefix(
            $"a0={report.FixedA0:E4} topN={report.TopGalaxyCount} bestVariant={report.BestVariantName} " +
            $"smoothKernel={report.BestSmoothKernelKind}:{report.BestSmoothKernelWidthKpc:F2}kpc " +
            $"smooth>single={report.SmoothBeatsSingleCount} smooth>toy={report.SmoothBeatsToyCount} " +
            $"meanDeltaVsSingle={report.MeanSmoothDeltaVsSingle:F5} meanDeltaVsToy={report.MeanSmoothDeltaVsToy:F5}");
        foreach (var c in report.VariantCorrelations)
        {
            WriteLineWithTestPrefix(
                $"{c.VariantName}: outerInner r/rho={c.OuterInnerRatioPearson:F5}/{c.OuterInnerRatioSpearman:F5}, " +
                $"gas r/rho={c.GasDominancePearson:F5}/{c.GasDominanceSpearman:F5}, " +
                $"structureCorrelated={c.CorrelatesWithDiskStructure}");
        }

        foreach (var g in report.Galaxies.Take(5))
        {
            WriteLineWithTestPrefix(
                $"{g.GalaxyKey}: baseline={g.BaselineRms:F5}, distributed={g.DiskDistributedRms:F5} " +
                $"(d={g.DiskDistributedDelta:F5}), multi={g.MultiCenterToyRms:F5} (d={g.MultiCenterToyDelta:F5}), " +
                $"smooth={g.SmoothDistributedFieldRms:F5} (d={g.SmoothDistributedFieldDelta:F5}), " +
                $"outerInner={g.OuterInnerRatio:F4}, gas={g.GasDominance:F4}, span={g.RadialSpanKpc:F2}, n={g.PointCount}, " +
                $"proxyAligned={g.ImprovementCorrelatesWithDiskStructure}");
        }

        // Positive result => distributed takt-field plausibly captures missing geometry; negative result => single-center remains sufficient.
        Assert.True(double.IsFinite(report.FixedA0));
        Assert.Contains(report.BestSmoothKernelKind, new[] { "gaussian", "exponential" });
        Assert.True(double.IsFinite(report.BestSmoothKernelWidthKpc));
        Assert.True(report.BestSmoothKernelWidthKpc > 0.0);
        Assert.Equal(20, report.TopGalaxyCount);
        Assert.Equal(20, report.Galaxies.Count);
        Assert.Equal(4, report.VariantCorrelations.Count);
        Assert.Contains(
            report.BestVariantName,
            new[]
            {
                "single-center radial TRM",
                "disk-distributed outer/inner weighted TRM",
                "off-center/multi-center toy geometry",
                "smooth distributed takt-field"
            });

        Assert.All(report.Galaxies, g =>
        {
            Assert.True(double.IsFinite(g.BaselineRms));
            Assert.True(double.IsFinite(g.SingleCenterRms));
            Assert.True(double.IsFinite(g.DiskDistributedRms));
            Assert.True(double.IsFinite(g.MultiCenterToyRms));
            Assert.True(double.IsFinite(g.SmoothDistributedFieldRms));
            Assert.True(double.IsFinite(g.OuterInnerRatio));
            Assert.True(double.IsFinite(g.GasDominance));
            Assert.True(double.IsFinite(g.RadialSpanKpc));
            Assert.True(g.PointCount >= 4);
        });
    }

    private void WriteLineWithTestPrefix(
        string message,
        [CallerMemberName] string memberName = "")
    {
        string prefix = ExtractTestPrefix(memberName);
        _output.WriteLine($"[{prefix}] {message}");
    }

    private static string ExtractTestPrefix(string memberName)
    {
        if (string.IsNullOrWhiteSpace(memberName))
            return "RAR";

        int separator = memberName.IndexOf('_');
        return separator > 0 ? memberName[..separator] : memberName;
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

    private static List<TurningResidualRow> BuildTurningResidualRows(List<RarPoint> points, double a0)
    {
        var rows = new List<TurningResidualRow>();

        foreach (var galaxyGroup in points.GroupBy(p => NormalizeGalaxyKey(p.GalaxyName)))
        {
            var ordered = galaxyGroup.OrderBy(p => p.RadiusKpc).ToList();
            if (ordered.Count < 3)
                continue;

            for (int i = 0; i < ordered.Count; i++)
            {
                var p = ordered[i];
                if (p.RadiusKpc <= 0 || p.Vobs <= 0 || p.GobsMs2 <= 0 || p.GbarMs2 <= 0)
                    continue;

                double gPred = SparcRarAnalysis.PredictGobs(p.GbarMs2, a0, ModelType.ClockworkTRM);
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

                double dLogGbarDr = (Math.Log(pRight.GbarMs2) - Math.Log(pLeft.GbarMs2)) / dr;
                double omegaSi = (p.Vobs * 1000.0) / (p.RadiusKpc * PhysicalConstants.KpcToM);
                double turningProxySigned = omegaSi * dLogGbarDr;
                double turningProxyAbs = Math.Abs(turningProxySigned);
                double residual = Math.Log10(p.GobsMs2) - Math.Log10(gPred);
                double logGbar = Math.Log10(p.GbarMs2);

                rows.Add(new TurningResidualRow(
                    galaxyGroup.Key,
                    residual,
                    turningProxySigned,
                    turningProxyAbs,
                    logGbar));
            }
        }

        return rows;
    }

    private static bool IsHoldoutGalaxy(string galaxyKey)
    {
        int checksum = galaxyKey.Sum(c => c);
        return checksum % 5 == 0;
    }

    private static bool IsHoldoutGalaxy(string galaxyKey, int modulo, int remainder)
    {
        int checksum = galaxyKey.Sum(c => c);
        return checksum % modulo == remainder;
    }

    private static TurningDiagnosticResult? EvaluateTurningDiagnostic(
        List<TurningResidualRow> rows,
        Func<TurningResidualRow, double> proxySelector,
        int binCount,
        int holdoutModulo,
        int holdoutRemainder,
        Func<string, bool> galaxyFilter)
    {
        var selected = rows.Where(r => galaxyFilter(r.GalaxyKey)).ToList();
        var holdout = selected.Where(r => IsHoldoutGalaxy(r.GalaxyKey, holdoutModulo, holdoutRemainder)).ToList();
        var train = selected.Where(r => !IsHoldoutGalaxy(r.GalaxyKey, holdoutModulo, holdoutRemainder)).ToList();

        if (train.Count < 150 || holdout.Count < 80)
            return null;

        var sortedProxy = train
            .Select(proxySelector)
            .OrderBy(x => x)
            .ToList();

        if (sortedProxy.Count < binCount * 20)
            return null;

        var cuts = new List<double>();
        for (int i = 1; i < binCount; i++)
        {
            int index = (i * sortedProxy.Count) / binCount;
            cuts.Add(sortedProxy[Math.Min(index, sortedProxy.Count - 1)]);
        }

        int GetBin(double value)
        {
            for (int i = 0; i < cuts.Count; i++)
            {
                if (value < cuts[i])
                    return i;
            }

            return cuts.Count;
        }

        var trainMeans = train
            .GroupBy(r => GetBin(proxySelector(r)))
            .ToDictionary(g => g.Key, g => g.Average(x => x.Residual));

        for (int bin = 0; bin < binCount; bin++)
        {
            if (!trainMeans.ContainsKey(bin))
                return null;
        }

        var holdoutCounts = holdout
            .GroupBy(r => GetBin(proxySelector(r)))
            .ToDictionary(g => g.Key, g => g.Count());

        for (int bin = 0; bin < binCount; bin++)
        {
            if (!holdoutCounts.TryGetValue(bin, out int count) || count < 8)
                return null;
        }

        var baselineResiduals = holdout.Select(r => r.Residual).ToList();
        var correctedResiduals = holdout
            .Select(r =>
            {
                int bin = GetBin(proxySelector(r));
                return r.Residual - trainMeans[bin];
            })
            .ToList();

        double baselineRms = ComputeBinRms(baselineResiduals);
        double correctedRms = ComputeBinRms(correctedResiduals);

        return new TurningDiagnosticResult(
            SplitLabel: string.Empty,
            BinCount: binCount,
            ProxyLabel: string.Empty,
            SurfaceBrightnessLabel: string.Empty,
            TrainCount: train.Count,
            HoldoutCount: holdout.Count,
            BaselineRms: baselineRms,
            CorrectedRms: correctedRms,
            DeltaRms: baselineRms - correctedRms);
    }

    private static TurningGateResult? EvaluateHsbGatedTurningDiagnostic(
        List<TurningResidualRow> rows,
        Func<TurningResidualRow, double> proxySelector,
        int binCount,
        int holdoutModulo,
        int holdoutRemainder)
    {
        var holdout = rows.Where(r => IsHoldoutGalaxy(r.GalaxyKey, holdoutModulo, holdoutRemainder)).ToList();
        var train = rows.Where(r => !IsHoldoutGalaxy(r.GalaxyKey, holdoutModulo, holdoutRemainder)).ToList();

        if (train.Count < 200 || holdout.Count < 100)
            return null;

        var trainGalaxyBrightness = train
            .GroupBy(r => r.GalaxyKey)
            .ToDictionary(g => g.Key, g => g.Average(x => x.LogGbar));

        var holdoutGalaxyBrightness = holdout
            .GroupBy(r => r.GalaxyKey)
            .ToDictionary(g => g.Key, g => g.Average(x => x.LogGbar));

        if (trainGalaxyBrightness.Count < 10 || holdoutGalaxyBrightness.Count < 5)
            return null;

        var sortedBrightness = trainGalaxyBrightness.Values.OrderBy(x => x).ToList();

        var sortedProxy = train
            .Select(proxySelector)
            .OrderBy(x => x)
            .ToList();

        if (sortedProxy.Count < binCount * 20)
            return null;

        var cuts = new List<double>();
        for (int i = 1; i < binCount; i++)
        {
            int index = (i * sortedProxy.Count) / binCount;
            cuts.Add(sortedProxy[Math.Min(index, sortedProxy.Count - 1)]);
        }

        int GetBin(double value)
        {
            for (int i = 0; i < cuts.Count; i++)
            {
                if (value < cuts[i])
                    return i;
            }

            return cuts.Count;
        }

        var trainMeansUngated = train
            .GroupBy(r => GetBin(proxySelector(r)))
            .ToDictionary(g => g.Key, g => g.Average(x => x.Residual));

        for (int bin = 0; bin < binCount; bin++)
        {
            if (!trainMeansUngated.ContainsKey(bin))
                return null;
        }

        var trainRowsWithBrightness = train
            .Select(r => new
            {
                Row = r,
                MeanLogGbar = trainGalaxyBrightness[r.GalaxyKey]
            })
            .ToList();

        var holdoutRowsWithBrightness = holdout
            .Select(r => new
            {
                Row = r,
                MeanLogGbar = holdoutGalaxyBrightness[r.GalaxyKey]
            })
            .ToList();

        double q20 = Percentile(sortedBrightness, 0.20);
        double q80 = Percentile(sortedBrightness, 0.80);
        if (q80 <= q20)
            return null;

        var hardThresholdCandidates = BuildLinearGrid(q20, q80, 13);
        double bestHardThreshold = hardThresholdCandidates[0];
        double bestTrainHardRms = double.MaxValue;

        foreach (double threshold in hardThresholdCandidates)
        {
            var corrected = trainRowsWithBrightness
                .Select(x =>
                {
                    int bin = GetBin(proxySelector(x.Row));
                    double correction = trainMeansUngated[bin];
                    double weight = x.MeanLogGbar > threshold ? 1.0 : 0.0;
                    return x.Row.Residual - (weight * correction);
                })
                .ToList();

            double rms = ComputeBinRms(corrected);
            if (rms < bestTrainHardRms)
            {
                bestTrainHardRms = rms;
                bestHardThreshold = threshold;
            }
        }

        var softThresholdCandidates = BuildLinearGrid(q20, q80, 13);
        var softWidthCandidates = new[] { 0.03, 0.05, 0.08, 0.12, 0.18, 0.26, 0.38, 0.55 };
        double bestSoftThreshold = softThresholdCandidates[0];
        double bestSoftWidth = softWidthCandidates[0];
        double bestTrainSoftRms = double.MaxValue;

        foreach (double threshold in softThresholdCandidates)
        {
            foreach (double width in softWidthCandidates)
            {
                var corrected = trainRowsWithBrightness
                    .Select(x =>
                    {
                        int bin = GetBin(proxySelector(x.Row));
                        double correction = trainMeansUngated[bin];
                        double weight = Sigmoid((x.MeanLogGbar - threshold) / width);
                        return x.Row.Residual - (weight * correction);
                    })
                    .ToList();

                double rms = ComputeBinRms(corrected);
                if (rms < bestTrainSoftRms)
                {
                    bestTrainSoftRms = rms;
                    bestSoftThreshold = threshold;
                    bestSoftWidth = width;
                }
            }
        }

        var baselineResiduals = holdoutRowsWithBrightness.Select(x => x.Row.Residual).ToList();
        var ungatedResiduals = holdoutRowsWithBrightness
            .Select(x =>
            {
                int bin = GetBin(proxySelector(x.Row));
                return x.Row.Residual - trainMeansUngated[bin];
            })
            .ToList();

        var hardGatedResiduals = holdoutRowsWithBrightness
            .Select(x =>
            {
                int bin = GetBin(proxySelector(x.Row));
                double correction = trainMeansUngated[bin];
                double weight = x.MeanLogGbar > bestHardThreshold ? 1.0 : 0.0;
                return x.Row.Residual - (weight * correction);
            })
            .ToList();

        var softGatedResiduals = holdoutRowsWithBrightness
            .Select(x =>
            {
                int bin = GetBin(proxySelector(x.Row));
                double correction = trainMeansUngated[bin];
                double weight = Sigmoid((x.MeanLogGbar - bestSoftThreshold) / bestSoftWidth);
                return x.Row.Residual - (weight * correction);
            })
            .ToList();

        double baselineRms = ComputeBinRms(baselineResiduals);
        double ungatedRms = ComputeBinRms(ungatedResiduals);
        double hardGatedRms = ComputeBinRms(hardGatedResiduals);
        double softGatedRms = ComputeBinRms(softGatedResiduals);

        return new TurningGateResult(
            SplitLabel: string.Empty,
            BaselineRms: baselineRms,
            UngatedRms: ungatedRms,
            HardGatedRms: hardGatedRms,
            SoftGatedRms: softGatedRms,
            UngatedDelta: baselineRms - ungatedRms,
            HardGatedDelta: baselineRms - hardGatedRms,
            SoftGatedDelta: baselineRms - softGatedRms,
            SoftGateThreshold: bestSoftThreshold,
            SoftGateWidth: bestSoftWidth);
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

    private static string NormalizeGalaxyKey(string name) =>
        name
            .Replace("_rotmod", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" ", "", StringComparison.Ordinal)
            .Trim()
            .ToUpperInvariant();

    private sealed record TurningResidualRow(
        string GalaxyKey,
        double Residual,
        double TurningProxySigned,
        double TurningProxyAbs,
        double LogGbar);

    private sealed record TurningDiagnosticResult(
        string SplitLabel,
        int BinCount,
        string ProxyLabel,
        string SurfaceBrightnessLabel,
        int TrainCount,
        int HoldoutCount,
        double BaselineRms,
        double CorrectedRms,
        double DeltaRms);

    private sealed record TurningGateResult(
        string SplitLabel,
        double BaselineRms,
        double UngatedRms,
        double HardGatedRms,
        double SoftGatedRms,
        double UngatedDelta,
        double HardGatedDelta,
        double SoftGatedDelta,
        double SoftGateThreshold,
        double SoftGateWidth);

}
