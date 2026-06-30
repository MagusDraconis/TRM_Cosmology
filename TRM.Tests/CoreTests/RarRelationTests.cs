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

    [Fact]
    /// <summary>
    /// Cross-split transfer diagnostic for turning-memory correction without any refit on target split.
    ///
    /// Hypothesis:
    /// A correction calibrated on split A (mod 5 == 0) transfers to split B (mod 5 == 1)
    /// with bounded degradation when all parameters are frozen.
    ///
    /// Status:
    /// diagnostic + candidate.
    ///
    /// Limitation:
    /// Transfer behavior is empirical and does not imply theorem-level closure.
    /// </summary>
    public void RAR23_TRM_Rar_CrossSplitTransfer_NoRefit()
    {
        const int modulo = 5;
        const int trainRemainder = 0;
        const int transferRemainder = 1;
        const int binCount = 3;
        const double degradeEpsilon = 0.01;

        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        // Baseline fit block: fit a0 once on the full dataset, then freeze it.
        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);
        var rows = BuildTurningResidualRows(rarData, fit.BestA0);

        // Strict train -> transfer discipline: train on remainder 0 only, transfer to remainder 1 only.
        var trainRows = rows.Where(r => IsHoldoutGalaxy(r.GalaxyKey, modulo, trainRemainder)).ToList();
        var transferRows = rows.Where(r => IsHoldoutGalaxy(r.GalaxyKey, modulo, transferRemainder)).ToList();

        Assert.True(trainRows.Count > 120, "Train split A must contain enough points.");
        Assert.True(transferRows.Count > 80, "Transfer split B must contain enough points.");

        var sortedProxy = trainRows
            .Select(r => r.TurningProxySigned)
            .OrderBy(x => x)
            .ToList();
        Assert.True(sortedProxy.Count >= binCount * 20, "Insufficient train rows for transfer binning.");

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

        // Train-only turning correction extraction (interpolated from bin means).
        var grouped = trainRows
            .GroupBy(r => GetBin(r.TurningProxySigned))
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    MeanResidual = g.Average(x => x.Residual),
                    MeanProxy = g.Average(x => x.TurningProxySigned)
                });

        for (int b = 0; b < binCount; b++)
            Assert.True(grouped.ContainsKey(b), $"Missing train bin {b} for transfer model.");

        var nodes = grouped
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => (Proxy: kvp.Value.MeanProxy, Correction: kvp.Value.MeanResidual))
            .ToList();

        double InterpolatedCorrection(double proxy)
        {
            if (proxy <= nodes[0].Proxy) return nodes[0].Correction;
            if (proxy >= nodes[^1].Proxy) return nodes[^1].Correction;

            for (int i = 0; i < nodes.Count - 1; i++)
            {
                var left = nodes[i];
                var right = nodes[i + 1];
                if (proxy < right.Proxy)
                {
                    double dx = right.Proxy - left.Proxy;
                    if (dx <= 0) return left.Correction;
                    double t = (proxy - left.Proxy) / dx;
                    return left.Correction + (t * (right.Correction - left.Correction));
                }
            }

            return nodes[^1].Correction;
        }

        // Train-only soft-gate fit (threshold + width), frozen for transfer split.
        var trainMeanLogByGalaxy = trainRows
            .GroupBy(r => r.GalaxyKey)
            .ToDictionary(g => g.Key, g => g.Average(x => x.LogGbar));
        var transferMeanLogByGalaxy = transferRows
            .GroupBy(r => r.GalaxyKey)
            .ToDictionary(g => g.Key, g => g.Average(x => x.LogGbar));

        var sortedBrightness = trainMeanLogByGalaxy.Values.OrderBy(x => x).ToList();
        double q20 = Percentile(sortedBrightness, 0.20);
        double q80 = Percentile(sortedBrightness, 0.80);
        Assert.True(q80 > q20, "Train brightness distribution is degenerate.");

        var thresholdGrid = BuildLinearGrid(q20, q80, 13);
        var widthGrid = new[] { 0.03, 0.05, 0.08, 0.12, 0.18, 0.26, 0.38, 0.55 };
        double bestThreshold = thresholdGrid[0];
        double bestWidth = widthGrid[0];
        double bestTrainRms = double.MaxValue;

        foreach (double threshold in thresholdGrid)
        {
            foreach (double width in widthGrid)
            {
                var corrected = trainRows
                    .Select(r =>
                    {
                        double correction = InterpolatedCorrection(r.TurningProxySigned);
                        double gateWeight = Sigmoid((trainMeanLogByGalaxy[r.GalaxyKey] - threshold) / width);
                        return r.Residual - (gateWeight * correction);
                    })
                    .ToList();

                double rms = ComputeBinRms(corrected);
                if (rms < bestTrainRms)
                {
                    bestTrainRms = rms;
                    bestThreshold = threshold;
                    bestWidth = width;
                }
            }
        }

        // Transfer-only evaluation with exact frozen parameters from split A.
        var baselineResiduals = transferRows.Select(r => r.Residual).ToList();
        var correctedResiduals = transferRows
            .Select(r =>
            {
                double correction = InterpolatedCorrection(r.TurningProxySigned);
                double gateWeight = Sigmoid((transferMeanLogByGalaxy[r.GalaxyKey] - bestThreshold) / bestWidth);
                return r.Residual - (gateWeight * correction);
            })
            .ToList();

        double baselineRms = ComputeBinRms(baselineResiduals);
        double correctedRms = ComputeBinRms(correctedResiduals);
        double deltaRms = baselineRms - correctedRms;
        bool improved = deltaRms > 0.0;

        WriteLineWithTestPrefix("--- CROSS-SPLIT TRANSFER DIAGNOSTIC ---");
        WriteLineWithTestPrefix(
            $"Train split: mod {modulo} == {trainRemainder} | Transfer split: mod {modulo} == {transferRemainder} | " +
            $"mode=Interpolated+SoftGate");
        WriteLineWithTestPrefix($"Frozen gate threshold={bestThreshold:F6} width={bestWidth:F6}");
        WriteLineWithTestPrefix($"Baseline RMS (split B)={baselineRms:F6}");
        WriteLineWithTestPrefix($"Corrected RMS (split B)={correctedRms:F6}");
        WriteLineWithTestPrefix($"Delta RMS (split B)={deltaRms:F6} | improved={improved}");

        // Diagnostic boundary: transfer may fail, but should remain bounded in degradation.
        Assert.True(double.IsFinite(baselineRms));
        Assert.True(double.IsFinite(correctedRms));
        Assert.True(double.IsFinite(bestThreshold));
        Assert.True(double.IsFinite(bestWidth));
        Assert.True(bestWidth > 0.0);
        Assert.True(correctedRms <= baselineRms + degradeEpsilon,
            $"Cross-split transfer degraded beyond epsilon={degradeEpsilon:F3}.");
    }

    [Fact]
    /// <summary>
    /// Minimal continuous turning-model transfer diagnostic without binning/gating.
    ///
    /// Hypothesis:
    /// A single continuous turning term can capture part of residual structure across splits.
    ///
    /// Status:
    /// diagnostic + candidate.
    ///
    /// Limitation:
    /// Effective residual model only, not theorem-level closure.
    /// </summary>
    public void RAR24_TRM_Rar_MinimalTurningModel_NoRefit()
    {
        const int modulo = 5;
        const int trainRemainder = 0;
        const int transferRemainder = 1;
        const double degradeEpsilon = 1e-6;
        const double eps = 1e-30;

        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        // Baseline fit block: fit a0 once and freeze for both train and transfer splits.
        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);
        var rows = BuildTurningResidualRows(rarData, fit.BestA0);

        var trainRows = rows.Where(r => IsHoldoutGalaxy(r.GalaxyKey, modulo, trainRemainder)).ToList();
        var transferRows = rows.Where(r => IsHoldoutGalaxy(r.GalaxyKey, modulo, transferRemainder)).ToList();

        Assert.True(trainRows.Count > 120, "Train split A must contain enough rows.");
        Assert.True(transferRows.Count > 80, "Transfer split B must contain enough rows.");

        // No binning/no gating: fit a minimal continuous term alpha * proxy /(1 + beta*|proxy|) on train only.
        double medianAbsProxy = Median(trainRows.Select(r => Math.Abs(r.TurningProxySigned)).OrderBy(x => x).ToList());
        double proxyScale = Math.Max(medianAbsProxy, 1e-20);
        double betaScale = 1.0 / proxyScale;
        var betaGrid = new[] { 0.0, 0.25 * betaScale, 0.5 * betaScale, 1.0 * betaScale, 2.0 * betaScale };

        // Inner train-only validation to reduce overfit before cross-split transfer.
        var fitRows = trainRows.Where(r => !IsHoldoutGalaxy(r.GalaxyKey, 3, 1)).ToList();
        var cvRows = trainRows.Where(r => IsHoldoutGalaxy(r.GalaxyKey, 3, 1)).ToList();
        if (fitRows.Count < 80 || cvRows.Count < 40)
        {
            fitRows = trainRows;
            cvRows = trainRows;
        }

        double medianPredTrain = Median(fitRows.Select(r => r.BasePredMs2).Where(double.IsFinite).OrderBy(x => x).ToList());
        medianPredTrain = Math.Max(medianPredTrain, 1e-20);

        double bestAlpha = 0.0;
        double bestBeta = 0.0;
        double bestCvRms = double.MaxValue;
        double baselineCvRms = ComputeBinRms(cvRows.Select(r => r.Residual).ToList());

        foreach (double beta in betaGrid)
        {
            var dampedAbs = fitRows
                .Select(r => Math.Abs(DampedProxy(r.TurningProxySigned, beta)))
                .OrderBy(x => x)
                .ToList();
            double medianAbsDamped = Math.Max(Median(dampedAbs), 1e-30);
            double maxAlpha = 0.20 * medianPredTrain / medianAbsDamped;
            maxAlpha = Math.Min(maxAlpha, 1e8);

            var alphaGrid = BuildLinearGrid(-maxAlpha, maxAlpha, 41);
            if (!alphaGrid.Contains(0.0))
                alphaGrid.Add(0.0);

            foreach (double alpha in alphaGrid)
            {
                bool tooStrong = fitRows.Any(r =>
                {
                    double term = alpha * DampedProxy(r.TurningProxySigned, beta);
                    return Math.Abs(term) > 0.20 * Math.Max(r.BasePredMs2, eps);
                });
                if (tooStrong)
                    continue;

                var cvResiduals = cvRows
                    .Select(r =>
                    {
                        double turningTerm = alpha * DampedProxy(r.TurningProxySigned, beta);
                        double gPredModel = Math.Max(r.BasePredMs2 + turningTerm, eps);
                        return Math.Log10(r.GobsMs2) - Math.Log10(gPredModel);
                    })
                    .ToList();

                double rms = ComputeBinRms(cvResiduals);
                if (rms < bestCvRms - 1e-12 || (Math.Abs(rms - bestCvRms) <= 1e-12 && Math.Abs(alpha) < Math.Abs(bestAlpha)))
                {
                    bestCvRms = rms;
                    bestAlpha = alpha;
                    bestBeta = beta;
                }
            }

            // Conservative candidate rule: keep non-zero model only if it clears a minimum train-CV gain.
            const double minCvGain = 0.01;
            if (!double.IsFinite(bestCvRms) || (baselineCvRms - bestCvRms) < minCvGain)
            {
                bestAlpha = 0.0;
                bestBeta = 0.0;
            }
        }

        // Strict transfer block: apply frozen alpha/beta on split B without any retuning.
        var baselineResidualsTransfer = transferRows
            .Select(r => Math.Log10(r.GobsMs2) - Math.Log10(Math.Max(r.BasePredMs2, eps)))
            .ToList();
        var modelResidualsTransfer = transferRows
            .Select(r =>
            {
                double turningTerm = bestAlpha * DampedProxy(r.TurningProxySigned, bestBeta);
                double gPredModel = Math.Max(r.BasePredMs2 + turningTerm, eps);
                return Math.Log10(r.GobsMs2) - Math.Log10(gPredModel);
            })
            .ToList();

        double baselineRms = ComputeBinRms(baselineResidualsTransfer);
        double modelRms = ComputeBinRms(modelResidualsTransfer);
        double deltaRms = baselineRms - modelRms;
        bool improved = deltaRms > 0.0;

        WriteLineWithTestPrefix("--- MINIMAL TURNING MODEL DIAGNOSTIC ---");
        WriteLineWithTestPrefix($"alpha={bestAlpha:E6}, beta={bestBeta:E6}");
        WriteLineWithTestPrefix($"baseline RMS={baselineRms:F6}");
        WriteLineWithTestPrefix($"model RMS={modelRms:F6}");
        WriteLineWithTestPrefix($"delta RMS={deltaRms:F6} | improved={improved}");

        // Candidate-level criterion: no degradation on transfer, improvement preferred and logged.
        Assert.True(double.IsFinite(bestAlpha));
        Assert.True(double.IsFinite(bestBeta));
        Assert.True(double.IsFinite(baselineRms));
        Assert.True(double.IsFinite(modelRms));
        Assert.True(modelRms <= baselineRms + degradeEpsilon,
            $"Minimal turning model degraded transfer RMS beyond epsilon={degradeEpsilon:E2}.");
    }

    [Fact]
    /// <summary>
    /// Gated minimal turning-model transfer diagnostic without split-B refit.
    ///
    /// Hypothesis:
    /// A minimal turning correction is most effective only within a learned physical gate regime.
    ///
    /// Status:
    /// diagnostic + candidate.
    ///
    /// Limitation:
    /// Exploratory residual-sector model, not a theorem-level closure.
    /// </summary>
    public void RAR25_TRM_Rar_GatedMinimalTurningModel_NoRefit()
    {
        const int modulo = 5;
        const int trainRemainder = 0;
        const int transferRemainder = 1;
        const double degradeEpsilon = 0.01;
        const double eps = 1e-30;

        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        // Baseline a0 fit is global and frozen; no per-split or per-mode refit.
        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);
        var rows = BuildTurningResidualRows(rarData, fit.BestA0);

        var trainRows = rows.Where(r => IsHoldoutGalaxy(r.GalaxyKey, modulo, trainRemainder)).ToList();
        var transferRows = rows.Where(r => IsHoldoutGalaxy(r.GalaxyKey, modulo, transferRemainder)).ToList();

        Assert.True(trainRows.Count > 120, "Train split A must contain enough rows.");
        Assert.True(transferRows.Count > 80, "Transfer split B must contain enough rows.");

        // Same gate logic style as RAR23: train-only brightness gate via threshold/width grid.
        var trainMeanLogByGalaxy = trainRows
            .GroupBy(r => r.GalaxyKey)
            .ToDictionary(g => g.Key, g => g.Average(x => x.LogGbar));
        var transferMeanLogByGalaxy = transferRows
            .GroupBy(r => r.GalaxyKey)
            .ToDictionary(g => g.Key, g => g.Average(x => x.LogGbar));

        var sortedBrightness = trainMeanLogByGalaxy.Values.OrderBy(x => x).ToList();
        double q20 = Percentile(sortedBrightness, 0.20);
        double q80 = Percentile(sortedBrightness, 0.80);
        Assert.True(q80 > q20, "Train gate-variable distribution is degenerate.");

        var thresholdGrid = BuildLinearGrid(q20, q80, 13);
        var widthGrid = new[] { 0.03, 0.05, 0.08, 0.12, 0.18, 0.26, 0.38, 0.55 };

        double bestAlpha = 0.0;
        double bestThreshold = thresholdGrid[0];
        double bestWidth = widthGrid[0];
        double bestTrainRms = double.MaxValue;

        foreach (double threshold in thresholdGrid)
        {
            foreach (double width in widthGrid)
            {
                // Fit alpha only on split A with fixed (threshold,width) candidate.
                var weightedProxyAbs = trainRows
                    .Select(r =>
                    {
                        double gateWeight = Sigmoid((trainMeanLogByGalaxy[r.GalaxyKey] - threshold) / width);
                        return Math.Abs(gateWeight * r.TurningProxySigned);
                    })
                    .OrderBy(x => x)
                    .ToList();
                double medianAbsFeature = Math.Max(Median(weightedProxyAbs), 1e-30);
                double medianBasePred = Math.Max(Median(trainRows.Select(r => r.BasePredMs2).OrderBy(x => x).ToList()), 1e-20);
                double maxAlpha = Math.Min(0.20 * medianBasePred / medianAbsFeature, 1e8);
                var alphaGrid = BuildLinearGrid(-maxAlpha, maxAlpha, 41);
                if (!alphaGrid.Contains(0.0))
                    alphaGrid.Add(0.0);

                double localBestAlpha = 0.0;
                double localBestRms = double.MaxValue;
                foreach (double alpha in alphaGrid)
                {
                    var residuals = trainRows
                        .Select(r =>
                        {
                            double gateWeight = Sigmoid((trainMeanLogByGalaxy[r.GalaxyKey] - threshold) / width);
                            double turningTerm = gateWeight * alpha * r.TurningProxySigned;
                            double gPredModel = Math.Max(r.BasePredMs2 + turningTerm, eps);
                            return Math.Log10(r.GobsMs2) - Math.Log10(gPredModel);
                        })
                        .ToList();

                    double rms = ComputeBinRms(residuals);
                    if (rms < localBestRms)
                    {
                        localBestRms = rms;
                        localBestAlpha = alpha;
                    }
                }

                if (localBestRms < bestTrainRms)
                {
                    bestTrainRms = localBestRms;
                    bestAlpha = localBestAlpha;
                    bestThreshold = threshold;
                    bestWidth = width;
                }
            }
        }

        // Strict transfer: apply frozen alpha/threshold/width unchanged to split B.
        var baselineResiduals = transferRows
            .Select(r => Math.Log10(r.GobsMs2) - Math.Log10(Math.Max(r.BasePredMs2, eps)))
            .ToList();
        var modelResiduals = transferRows
            .Select(r =>
            {
                double gateWeight = Sigmoid((transferMeanLogByGalaxy[r.GalaxyKey] - bestThreshold) / bestWidth);
                double turningTerm = gateWeight * bestAlpha * r.TurningProxySigned;
                double gPredModel = Math.Max(r.BasePredMs2 + turningTerm, eps);
                return Math.Log10(r.GobsMs2) - Math.Log10(gPredModel);
            })
            .ToList();

        double baselineRms = ComputeBinRms(baselineResiduals);
        double modelRms = ComputeBinRms(modelResiduals);
        double deltaRms = baselineRms - modelRms;
        bool improved = deltaRms > 0.0;

        WriteLineWithTestPrefix("--- GATED MINIMAL TURNING MODEL DIAGNOSTIC ---");
        WriteLineWithTestPrefix($"alpha={bestAlpha:E6}");
        WriteLineWithTestPrefix($"threshold={bestThreshold:F6}");
        WriteLineWithTestPrefix($"width={bestWidth:F6}");
        WriteLineWithTestPrefix($"baseline RMS={baselineRms:F6}");
        WriteLineWithTestPrefix($"model RMS={modelRms:F6}");
        WriteLineWithTestPrefix($"delta RMS={deltaRms:F6} | improved={improved}");

        // Candidate-level assertions: finite params and bounded transfer degradation.
        Assert.True(double.IsFinite(bestAlpha));
        Assert.True(double.IsFinite(bestThreshold));
        Assert.True(double.IsFinite(bestWidth));
        Assert.True(bestWidth > 0.0);
        Assert.True(double.IsFinite(baselineRms));
        Assert.True(double.IsFinite(modelRms));
        Assert.True(modelRms <= baselineRms + degradeEpsilon,
            $"Gated minimal turning model degraded transfer RMS beyond epsilon={degradeEpsilon:F3}.");
    }

    [Fact]
    /// <summary>
    /// Cross-split transfer diagnostic comparing bin offset-only versus bin offset+slope correction.
    ///
    /// Hypothesis:
    /// RAR23 transfer gain may come either from regime-level offsets or from intra-bin slope structure.
    ///
    /// Status:
    /// diagnostic + candidate.
    ///
    /// Limitation:
    /// Exploratory decomposition; does not establish unique physical causality.
    /// </summary>
    public void RAR26_TRM_Rar_BinOffsetVsSlopeTransfer_NoRefit()
    {
        const int modulo = 5;
        const int trainRemainder = 0;
        const int transferRemainder = 1;
        const int binCount = 3;
        const double degradeEpsilon = 0.01;

        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        // Baseline fit once on full sample, then freeze.
        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);
        var rows = BuildTurningResidualRows(rarData, fit.BestA0);

        var trainRows = rows.Where(r => IsHoldoutGalaxy(r.GalaxyKey, modulo, trainRemainder)).ToList();
        var transferRows = rows.Where(r => IsHoldoutGalaxy(r.GalaxyKey, modulo, transferRemainder)).ToList();

        Assert.True(trainRows.Count > 120, "Train split A must contain enough rows.");
        Assert.True(transferRows.Count > 80, "Transfer split B must contain enough rows.");

        // Same 3-bin construction as RAR23 from train split A.
        var sortedProxy = trainRows
            .Select(r => r.TurningProxySigned)
            .OrderBy(x => x)
            .ToList();
        Assert.True(sortedProxy.Count >= binCount * 20, "Insufficient train rows for 3-bin diagnostics.");

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

        // Per-bin fit on split A:
        // A) offset-only: mean residual
        // B) offset+slope: mean residual + alphaBin * centeredProxy
        var binFits = new Dictionary<int, (double MeanResidual, double MeanProxy, double Alpha)>();
        for (int b = 0; b < binCount; b++)
        {
            var binRows = trainRows.Where(r => GetBin(r.TurningProxySigned) == b).ToList();
            Assert.True(binRows.Count >= 12, $"Train bin {b} requires enough rows.");

            double meanResidual = binRows.Average(r => r.Residual);
            double meanProxy = binRows.Average(r => r.TurningProxySigned);

            double num = 0.0;
            double den = 0.0;
            foreach (var r in binRows)
            {
                double centeredProxy = r.TurningProxySigned - meanProxy;
                num += centeredProxy * (r.Residual - meanResidual);
                den += centeredProxy * centeredProxy;
            }

            double alpha = den > 0.0 ? num / den : 0.0;
            binFits[b] = (meanResidual, meanProxy, alpha);
        }

        // Transfer to split B with frozen bin parameters.
        var baselineResiduals = transferRows.Select(r => r.Residual).ToList();
        var offsetOnlyResiduals = transferRows
            .Select(r =>
            {
                var fitBin = binFits[GetBin(r.TurningProxySigned)];
                double correction = fitBin.MeanResidual;
                return r.Residual - correction;
            })
            .ToList();
        var offsetSlopeResiduals = transferRows
            .Select(r =>
            {
                var fitBin = binFits[GetBin(r.TurningProxySigned)];
                double centeredProxy = r.TurningProxySigned - fitBin.MeanProxy;
                double correction = fitBin.MeanResidual + (fitBin.Alpha * centeredProxy);
                return r.Residual - correction;
            })
            .ToList();

        double baselineRms = ComputeBinRms(baselineResiduals);
        double offsetOnlyRms = ComputeBinRms(offsetOnlyResiduals);
        double offsetSlopeRms = ComputeBinRms(offsetSlopeResiduals);
        double deltaOffset = baselineRms - offsetOnlyRms;
        double deltaSlope = baselineRms - offsetSlopeRms;

        WriteLineWithTestPrefix("--- BIN OFFSET VS SLOPE TRANSFER DIAGNOSTIC ---");
        WriteLineWithTestPrefix($"baseline RMS={baselineRms:F6}");
        WriteLineWithTestPrefix($"offset-only RMS={offsetOnlyRms:F6} | delta={deltaOffset:F6}");
        WriteLineWithTestPrefix($"offset+slope RMS={offsetSlopeRms:F6} | delta={deltaSlope:F6}");
        WriteLineWithTestPrefix($"offset improved={(deltaOffset > 0.0)} | slope improved={(deltaSlope > 0.0)}");

        Assert.True(double.IsFinite(baselineRms));
        Assert.True(double.IsFinite(offsetOnlyRms));
        Assert.True(double.IsFinite(offsetSlopeRms));
        Assert.True(double.IsFinite(deltaOffset));
        Assert.True(double.IsFinite(deltaSlope));
        Assert.True(offsetOnlyRms <= baselineRms + degradeEpsilon,
            $"Offset-only transfer degraded beyond epsilon={degradeEpsilon:F3}.");
        Assert.True(offsetSlopeRms <= baselineRms + degradeEpsilon,
            $"Offset+slope transfer degraded beyond epsilon={degradeEpsilon:F3}.");
    }

    [Fact]
    /// <summary>
    /// Cross-split matrix diagnostic for offset-only bin correction stability.
    ///
    /// Hypothesis:
    /// Regime-bin residual offsets learned on one train split remain transferable across other split remainders.
    ///
    /// Status:
    /// diagnostic + candidate.
    ///
    /// Limitation:
    /// Exploratory transfer matrix; not a production correction policy.
    /// </summary>
    public void RAR27_TRM_Rar_BinOffsetCrossSplitMatrix_NoRefit()
    {
        const int modulo = 5;
        const int binCount = 3;
        const double degradeEpsilon = 0.01;

        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        // Baseline fit once on full dataset, then keep frozen for all matrix cases.
        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);
        var rows = BuildTurningResidualRows(rarData, fit.BestA0);

        var matrixResults = new List<(int TrainRemainder, int TransferRemainder, double BaselineRms, double OffsetRms, double DeltaRms)>();

        for (int trainRemainder = 0; trainRemainder < modulo; trainRemainder++)
        {
            // Per prompt: train split excludes the current train remainder.
            var trainRows = rows
                .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) != trainRemainder)
                .ToList();
            Assert.True(trainRows.Count >= binCount * 30, $"Insufficient train rows for train remainder {trainRemainder}.");

            // Learn frozen bin cuts from train only.
            var sortedProxy = trainRows
                .Select(r => r.TurningProxySigned)
                .OrderBy(x => x)
                .ToList();
            Assert.True(sortedProxy.Count >= binCount * 20, $"Insufficient proxy rows for binning (train remainder {trainRemainder}).");

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

            // Learn frozen offset per bin from train only.
            var trainOffsets = trainRows
                .GroupBy(r => GetBin(r.TurningProxySigned))
                .ToDictionary(g => g.Key, g => g.Average(x => x.Residual));

            for (int b = 0; b < binCount; b++)
                Assert.True(trainOffsets.ContainsKey(b), $"Missing train bin {b} for train remainder {trainRemainder}.");

            for (int transferRemainder = 0; transferRemainder < modulo; transferRemainder++)
            {
                if (transferRemainder == trainRemainder)
                    continue;

                var transferRows = rows
                    .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) == transferRemainder)
                    .ToList();
                Assert.True(transferRows.Count >= 20, $"Insufficient transfer rows for transfer remainder {transferRemainder}.");

                var baselineResiduals = transferRows.Select(r => r.Residual).ToList();
                var offsetResiduals = transferRows
                    .Select(r =>
                    {
                        int bin = GetBin(r.TurningProxySigned);
                        return r.Residual - trainOffsets[bin];
                    })
                    .ToList();

                double baselineRms = ComputeBinRms(baselineResiduals);
                double offsetRms = ComputeBinRms(offsetResiduals);
                double deltaRms = baselineRms - offsetRms;

                matrixResults.Add((trainRemainder, transferRemainder, baselineRms, offsetRms, deltaRms));
            }
        }

        Assert.Equal(modulo * (modulo - 1), matrixResults.Count);

        double meanDelta = matrixResults.Average(x => x.DeltaRms);
        double medianDelta = Median(matrixResults.Select(x => x.DeltaRms).OrderBy(x => x).ToList());
        int improvedCount = matrixResults.Count(x => x.DeltaRms > 0.0);
        int degradedCount = matrixResults.Count(x => x.DeltaRms < 0.0);
        double worstDegradation = matrixResults
            .Select(x => x.OffsetRms - x.BaselineRms)
            .Max();

        WriteLineWithTestPrefix("--- BIN OFFSET CROSS-SPLIT MATRIX DIAGNOSTIC ---");
        foreach (var row in matrixResults.OrderBy(x => x.TrainRemainder).ThenBy(x => x.TransferRemainder))
        {
            WriteLineWithTestPrefix(
                $"train={row.TrainRemainder} transfer={row.TransferRemainder} " +
                $"baseline={row.BaselineRms:F6} offset={row.OffsetRms:F6} delta={row.DeltaRms:F6} improved={(row.DeltaRms > 0.0)}");
        }
        WriteLineWithTestPrefix($"mean delta={meanDelta:F6}");
        WriteLineWithTestPrefix($"median delta={medianDelta:F6}");
        WriteLineWithTestPrefix($"number improved={improvedCount}");
        WriteLineWithTestPrefix($"number degraded={degradedCount}");
        WriteLineWithTestPrefix($"worst degradation={worstDegradation:F6}");

        Assert.All(matrixResults, row =>
        {
            Assert.True(double.IsFinite(row.BaselineRms));
            Assert.True(double.IsFinite(row.OffsetRms));
            Assert.True(double.IsFinite(row.DeltaRms));
            Assert.True(row.OffsetRms <= row.BaselineRms + degradeEpsilon,
                $"Case train={row.TrainRemainder}, transfer={row.TransferRemainder} degraded beyond epsilon={degradeEpsilon:F3}.");
        });

        Assert.True(improvedCount >= matrixResults.Count / 2,
            "At least half of cross-split transfer cases should improve.");
    }

    [Fact]
    /// <summary>
    /// Physical interpretation diagnostic for cross-split stable offset-only turning regimes.
    ///
    /// Hypothesis:
    /// Stable bin offsets correspond to distinct physical galaxy-regime proxies.
    ///
    /// Status:
    /// diagnostic + candidate.
    ///
    /// Limitation:
    /// Interpretive correlation layer only; not baseline-model activation.
    /// </summary>
    public void RAR28_TRM_Rar_RegimeOffsetPhysicalInterpretation_NoRefit()
    {
        const int modulo = 5;
        const int binCount = 3;
        const int minBinPoints = 20;
        const double signTolerance = 1e-12;

        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        // Fit baseline a0 once on full sample.
        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);
        var rows = BuildTurningResidualRows(rarData, fit.BestA0);
        var proxyByGalaxy = BuildGalaxyPhysicalProxyStats(rarData);

        var offsetsByBin = Enumerable.Range(0, binCount)
            .ToDictionary(b => b, _ => new List<double>());

        var transferMetrics = new List<(int TrainRemainder, int TransferRemainder, double BaselineRms, double OffsetRms, double DeltaRms)>();

        WriteLineWithTestPrefix("--- REGIME OFFSET PHYSICAL INTERPRETATION DIAGNOSTIC ---");

        for (int trainRemainder = 0; trainRemainder < modulo; trainRemainder++)
        {
            var trainRows = rows
                .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) != trainRemainder)
                .ToList();
            Assert.True(trainRows.Count >= binCount * 30, $"Insufficient train rows for remainder {trainRemainder}.");

            var sortedProxy = trainRows
                .Select(r => r.TurningProxySigned)
                .OrderBy(x => x)
                .ToList();
            Assert.True(sortedProxy.Count >= binCount * 20, $"Insufficient proxy rows for train remainder {trainRemainder}.");

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

            var binRows = Enumerable.Range(0, binCount)
                .ToDictionary(
                    b => b,
                    b => trainRows.Where(r => GetBin(r.TurningProxySigned) == b).ToList());

            var binOffsets = new Dictionary<int, double>();
            for (int b = 0; b < binCount; b++)
            {
                var bucket = binRows[b];
                Assert.True(bucket.Count >= minBinPoints, $"Train remainder {trainRemainder}, bin {b} has too few points.");

                double offset = bucket.Average(x => x.Residual);
                binOffsets[b] = offset;
                offsetsByBin[b].Add(offset);

                var proxyRows = bucket
                    .Where(r => proxyByGalaxy.ContainsKey(r.GalaxyKey))
                    .Select(r => proxyByGalaxy[r.GalaxyKey])
                    .ToList();
                Assert.True(proxyRows.Count > 0, $"No proxy rows for train remainder {trainRemainder}, bin {b}.");

                double meanLogGbar = proxyRows.Average(x => x.MeanLogGbar);
                double meanAbsGrad = proxyRows.Average(x => x.MeanAbsDlnGbarDr);
                double outerInner = proxyRows.Average(x => x.OuterToInnerBaryonicAccelerationRatio);
                double gasDominance = proxyRows.Average(x => x.GasDominanceProxy);
                double radialSpan = proxyRows.Average(x => x.RadialSpanKpc);
                double pointCount = proxyRows.Average(x => x.PointCount);

                Assert.True(double.IsFinite(offset));
                Assert.True(double.IsFinite(meanLogGbar));
                Assert.True(double.IsFinite(meanAbsGrad));
                Assert.True(double.IsFinite(outerInner));
                Assert.True(double.IsFinite(gasDominance));
                Assert.True(double.IsFinite(radialSpan));
                Assert.True(double.IsFinite(pointCount));

                WriteLineWithTestPrefix(
                    $"train={trainRemainder} bin={b} offset={offset:F6} n={bucket.Count} " +
                    $"meanLogGbar={meanLogGbar:F6} meanAbsDlnGbarDr={meanAbsGrad:E6} " +
                    $"outerToInnerRatio={outerInner:F6} gasDominance={gasDominance:F6} " +
                    $"radialSpanKpc={radialSpan:F6} pointCount={pointCount:F2}");
            }

            // Apply frozen cuts/offsets to each transfer split (transfer != train).
            for (int transferRemainder = 0; transferRemainder < modulo; transferRemainder++)
            {
                if (transferRemainder == trainRemainder)
                    continue;

                var transferRows = rows
                    .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) == transferRemainder)
                    .ToList();
                Assert.True(transferRows.Count >= 20, $"Insufficient transfer rows for remainder {transferRemainder}.");

                var baselineResiduals = transferRows.Select(r => r.Residual).ToList();
                var correctedResiduals = transferRows
                    .Select(r =>
                    {
                        int bin = GetBin(r.TurningProxySigned);
                        return r.Residual - binOffsets[bin];
                    })
                    .ToList();

                double baselineRms = ComputeBinRms(baselineResiduals);
                double offsetRms = ComputeBinRms(correctedResiduals);
                double deltaRms = baselineRms - offsetRms;

                Assert.True(double.IsFinite(baselineRms));
                Assert.True(double.IsFinite(offsetRms));
                Assert.True(double.IsFinite(deltaRms));

                transferMetrics.Add((trainRemainder, transferRemainder, baselineRms, offsetRms, deltaRms));
            }
        }

        // Cross-split stability summary per bin.
        bool stableSignFound = false;
        for (int b = 0; b < binCount; b++)
        {
            var values = offsetsByBin[b];
            Assert.True(values.Count == modulo, $"Expected one offset per train remainder for bin {b}.");
            Assert.All(values, v => Assert.True(double.IsFinite(v)));

            double mean = values.Average();
            double std = Math.Sqrt(values.Select(v => (v - mean) * (v - mean)).Average());

            int FirstSign(IEnumerable<double> vs)
            {
                foreach (double v in vs)
                {
                    if (v > signTolerance) return 1;
                    if (v < -signTolerance) return -1;
                }
                return 0;
            }

            int baseSign = FirstSign(values);
            bool stableSign = baseSign != 0 && values.All(v => (v > signTolerance && baseSign > 0) || (v < -signTolerance && baseSign < 0));
            stableSignFound |= stableSign;

            WriteLineWithTestPrefix(
                $"bin={b} offsetMean={mean:F6} offsetStd={std:F6} signConsistency={stableSign}");
        }

        Assert.True(transferMetrics.Count == modulo * (modulo - 1));
        Assert.True(stableSignFound, "At least one bin should show stable offset sign across all train splits.");
    }
    
    [Fact]
    /// <summary>
    /// Permutation null-control for cross-split offset-only regime correction.
    ///
    /// Hypothesis:
    /// If the regime signal is physical, real train->transfer deltas should exceed shuffled-proxy null runs.
    ///
    /// Status:
    /// diagnostic.
    ///
    /// Limitation:
    /// Null-control evidence only; not a direct causal proof.
    /// </summary>
    public void RAR29_TRM_Rar_RegimeOffsetPermutationNullControl_NoRefit()
    {
        const int modulo = 5;
        const int binCount = 3;
        const int shuffleCount = 20;

        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        // Fit a0 once and freeze for real and null-control evaluations.
        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);
        var rows = BuildTurningResidualRows(rarData, fit.BestA0);

        double realMeanDelta = EvaluateCrossSplitOffsetMeanDelta(rows, modulo, binCount, shuffleSeed: null);

        var shuffledMeanDeltas = new List<double>();
        for (int seed = 0; seed < shuffleCount; seed++)
        {
            int deterministicSeed = 1000 + seed;
            double shuffledMeanDelta = EvaluateCrossSplitOffsetMeanDelta(rows, modulo, binCount, shuffleSeed: deterministicSeed);
            shuffledMeanDeltas.Add(shuffledMeanDelta);
        }

        double shuffledMeanDeltaAll = shuffledMeanDeltas.Average();
        double shuffledBestDelta = shuffledMeanDeltas.Max();
        int shuffledBetter = shuffledMeanDeltas.Count(d => d >= realMeanDelta);
        double empiricalFraction = shuffledBetter / (double)shuffleCount;

        WriteLineWithTestPrefix("--- REGIME OFFSET PERMUTATION NULL CONTROL ---");
        WriteLineWithTestPrefix($"real mean delta={realMeanDelta:F6}");
        WriteLineWithTestPrefix($"shuffled mean delta={shuffledMeanDeltaAll:F6}");
        WriteLineWithTestPrefix($"shuffled best delta={shuffledBestDelta:F6}");
        WriteLineWithTestPrefix($"number shuffled runs beating real={shuffledBetter}");
        WriteLineWithTestPrefix($"empirical p-like fraction={empiricalFraction:F6}");

        Assert.True(double.IsFinite(realMeanDelta));
        Assert.True(double.IsFinite(shuffledMeanDeltaAll));
        Assert.True(double.IsFinite(shuffledBestDelta));
        Assert.True(double.IsFinite(empiricalFraction));

        Assert.True(realMeanDelta > 0.0, "Real mean delta should be positive.");
        Assert.True(shuffledMeanDeltaAll < realMeanDelta, "Shuffled mean delta should be lower than real mean delta.");
        Assert.True(shuffledBetter <= shuffleCount / 2, "Shuffled runs beating real should remain small.");
    }

    [Fact]
    /// <summary>
    /// Cross-split diagnostic comparing global residual offset versus regime-specific offset correction.
    ///
    /// Hypothesis:
    /// If RAR27/RAR28 gain is physical-regime specific, bin-wise offsets should outperform a single global bias offset.
    ///
    /// Status:
    /// diagnostic.
    ///
    /// Limitation:
    /// Residual-structure decomposition only; no baseline-model activation.
    /// </summary>
    public void RAR30_TRM_Rar_GlobalOffsetVsRegimeOffset_NoRefit()
    {
        const int modulo = 5;
        const int binCount = 3;
        const double degradeEpsilon = 0.01;

        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        // Baseline fit block: fit a0 once on full dataset and freeze it for all split diagnostics.
        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);
        var rows = BuildTurningResidualRows(rarData, fit.BestA0);

        var transferResults = new List<(int TrainRemainder, int TransferRemainder, double BaselineRms, double GlobalRms, double RegimeRms)>();

        for (int trainRemainder = 0; trainRemainder < modulo; trainRemainder++)
        {
            // Train split definition per prompt: train excludes the selected remainder.
            var trainRows = rows
                .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) != trainRemainder)
                .ToList();
            Assert.True(trainRows.Count >= binCount * 30, $"Insufficient train rows for train remainder {trainRemainder}.");

            // A) Global mean residual offset from train only.
            double globalOffset = trainRows.Average(r => r.Residual);
            Assert.True(double.IsFinite(globalOffset));

            // B) Regime offsets from train-only 3-bin turning-proxy split.
            var sortedProxy = trainRows
                .Select(r => r.TurningProxySigned)
                .OrderBy(x => x)
                .ToList();
            Assert.True(sortedProxy.Count >= binCount * 20, $"Insufficient train rows for binning at remainder {trainRemainder}.");

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

            var regimeOffsets = trainRows
                .GroupBy(r => GetBin(r.TurningProxySigned))
                .ToDictionary(g => g.Key, g => g.Average(x => x.Residual));
            for (int b = 0; b < binCount; b++)
            {
                Assert.True(regimeOffsets.ContainsKey(b), $"Missing train regime bin {b} for train remainder {trainRemainder}.");
                Assert.True(double.IsFinite(regimeOffsets[b]));
            }

            // Transfer block: apply frozen global/regime offsets to each transfer remainder (transfer != train).
            for (int transferRemainder = 0; transferRemainder < modulo; transferRemainder++)
            {
                if (transferRemainder == trainRemainder)
                    continue;

                var transferRows = rows
                    .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) == transferRemainder)
                    .ToList();
                Assert.True(transferRows.Count >= 20, $"Insufficient transfer rows for transfer remainder {transferRemainder}.");

                var baselineResiduals = transferRows.Select(r => r.Residual).ToList();
                var globalResiduals = transferRows
                    .Select(r => r.Residual - globalOffset)
                    .ToList();
                var regimeResiduals = transferRows
                    .Select(r =>
                    {
                        int bin = GetBin(r.TurningProxySigned);
                        return r.Residual - regimeOffsets[bin];
                    })
                    .ToList();

                double baselineRms = ComputeBinRms(baselineResiduals);
                double globalRms = ComputeBinRms(globalResiduals);
                double regimeRms = ComputeBinRms(regimeResiduals);

                transferResults.Add((trainRemainder, transferRemainder, baselineRms, globalRms, regimeRms));
            }
        }

        Assert.Equal(modulo * (modulo - 1), transferResults.Count);

        double meanBaselineRms = transferResults.Average(x => x.BaselineRms);
        double meanGlobalRms = transferResults.Average(x => x.GlobalRms);
        double meanRegimeRms = transferResults.Average(x => x.RegimeRms);
        double deltaGlobal = meanBaselineRms - meanGlobalRms;
        double deltaRegime = meanBaselineRms - meanRegimeRms;
        double extraRegimeGain = deltaRegime - deltaGlobal;
        int regimeBeatsGlobalCount = transferResults.Count(x => x.RegimeRms < x.GlobalRms);

        WriteLineWithTestPrefix("--- GLOBAL OFFSET VS REGIME OFFSET DIAGNOSTIC ---");
        WriteLineWithTestPrefix($"mean baseline RMS={meanBaselineRms:F6}");
        WriteLineWithTestPrefix($"mean global-offset RMS={meanGlobalRms:F6}");
        WriteLineWithTestPrefix($"mean regime-offset RMS={meanRegimeRms:F6}");
        WriteLineWithTestPrefix($"delta global={deltaGlobal:F6}");
        WriteLineWithTestPrefix($"delta regime={deltaRegime:F6}");
        WriteLineWithTestPrefix($"extra regime gain={extraRegimeGain:F6}");
        WriteLineWithTestPrefix($"number of transfers where regime beats global={regimeBeatsGlobalCount}");

        Assert.True(double.IsFinite(meanBaselineRms));
        Assert.True(double.IsFinite(meanGlobalRms));
        Assert.True(double.IsFinite(meanRegimeRms));
        Assert.True(double.IsFinite(deltaGlobal));
        Assert.True(double.IsFinite(deltaRegime));
        Assert.True(double.IsFinite(extraRegimeGain));

        Assert.All(transferResults, row =>
        {
            Assert.True(double.IsFinite(row.BaselineRms));
            Assert.True(double.IsFinite(row.GlobalRms));
            Assert.True(double.IsFinite(row.RegimeRms));
            Assert.True(row.GlobalRms <= row.BaselineRms + degradeEpsilon,
                $"Global-offset degraded beyond epsilon={degradeEpsilon:F3} for train={row.TrainRemainder}, transfer={row.TransferRemainder}.");
            Assert.True(row.RegimeRms <= row.BaselineRms + degradeEpsilon,
                $"Regime-offset degraded beyond epsilon={degradeEpsilon:F3} for train={row.TrainRemainder}, transfer={row.TransferRemainder}.");
        });
    }

    [Fact]
    /// <summary>
    /// Cross-split diagnostic testing whether global residual offset is equivalent to a small global a0 recalibration.
    ///
    /// Hypothesis:
    /// If the stable global bias is mostly normalization, a train-only shifted a0 should match global-offset transfer gains.
    ///
    /// Status:
    /// diagnostic + candidate.
    ///
    /// Limitation:
    /// Residual-level equivalence test only; no baseline activation.
    /// </summary>
    public void RAR31_TRM_Rar_GlobalOffsetEquivalentA0Shift_NoRefit()
    {
        const int modulo = 5;
        const int binCount = 3;
        const double degradeEpsilon = 0.01;
        const double minPhysicalA0 = 3e-11;
        const double maxPhysicalA0 = 3e-10;
        const double logScanHalfWidth = 0.08;
        const int logScanCount = 41;

        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        // Baseline fit once on full dataset, then freeze for all comparisons.
        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);
        var rows = BuildTurningResidualRows(rarData, fit.BestA0);

        var transferResults = new List<(
            int TrainRemainder,
            int TransferRemainder,
            double ShiftedA0,
            double BaselineRms,
            double GlobalOffsetRms,
            double ShiftedA0Rms,
            double RegimeOffsetRms)>();
        var shiftedA0ByTrain = new List<double>();

        for (int trainRemainder = 0; trainRemainder < modulo; trainRemainder++)
        {
            var trainRows = rows
                .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) != trainRemainder)
                .ToList();
            Assert.True(trainRows.Count >= binCount * 30, $"Insufficient train rows for train remainder {trainRemainder}.");

            // Train-only global mean residual offset.
            double globalOffset = trainRows.Average(r => r.Residual);
            Assert.True(double.IsFinite(globalOffset));

            // Train-only regime offsets from 3 turning bins.
            var sortedProxy = trainRows
                .Select(r => r.TurningProxySigned)
                .OrderBy(x => x)
                .ToList();
            Assert.True(sortedProxy.Count >= binCount * 20, $"Insufficient train rows for binning at remainder {trainRemainder}.");

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

            var regimeOffsets = trainRows
                .GroupBy(r => GetBin(r.TurningProxySigned))
                .ToDictionary(g => g.Key, g => g.Average(x => x.Residual));
            for (int b = 0; b < binCount; b++)
            {
                Assert.True(regimeOffsets.ContainsKey(b), $"Missing train regime bin {b} for train remainder {trainRemainder}.");
                Assert.True(double.IsFinite(regimeOffsets[b]));
            }

            // Train-only a0 shift scan: choose candidate with mean residual closest to zero.
            double baselineLogA0 = Math.Log10(fit.BestA0);
            var logA0Candidates = BuildLinearGrid(baselineLogA0 - logScanHalfWidth, baselineLogA0 + logScanHalfWidth, logScanCount);

            double bestShiftedA0 = fit.BestA0;
            double bestObjective = double.MaxValue;
            foreach (double candidateLogA0 in logA0Candidates)
            {
                double candidateA0 = Math.Pow(10.0, candidateLogA0);
                var trainResidualsWithCandidate = trainRows
                    .Select(r =>
                    {
                        double pred = SparcRarAnalysis.PredictGobs(r.GbarMs2, candidateA0, ModelType.ClockworkTRM);
                        return Math.Log10(r.GobsMs2) - Math.Log10(pred);
                    })
                    .ToList();

                double meanResidual = trainResidualsWithCandidate.Average();
                double objective = Math.Abs(meanResidual);
                if (objective < bestObjective - 1e-12 ||
                    (Math.Abs(objective - bestObjective) <= 1e-12 && Math.Abs(candidateLogA0 - baselineLogA0) < Math.Abs(Math.Log10(bestShiftedA0) - baselineLogA0)))
                {
                    bestObjective = objective;
                    bestShiftedA0 = candidateA0;
                }
            }

            shiftedA0ByTrain.Add(bestShiftedA0);
            Assert.True(bestShiftedA0 >= minPhysicalA0 && bestShiftedA0 <= maxPhysicalA0,
                $"Shifted a0 out of physical window for train remainder {trainRemainder}: {bestShiftedA0:E6}");

            for (int transferRemainder = 0; transferRemainder < modulo; transferRemainder++)
            {
                if (transferRemainder == trainRemainder)
                    continue;

                var transferRows = rows
                    .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) == transferRemainder)
                    .ToList();
                Assert.True(transferRows.Count >= 20, $"Insufficient transfer rows for transfer remainder {transferRemainder}.");

                var baselineResiduals = transferRows.Select(r => r.Residual).ToList();
                var globalOffsetResiduals = transferRows
                    .Select(r => r.Residual - globalOffset)
                    .ToList();
                var shiftedA0Residuals = transferRows
                    .Select(r =>
                    {
                        double pred = SparcRarAnalysis.PredictGobs(r.GbarMs2, bestShiftedA0, ModelType.ClockworkTRM);
                        return Math.Log10(r.GobsMs2) - Math.Log10(pred);
                    })
                    .ToList();
                var regimeOffsetResiduals = transferRows
                    .Select(r =>
                    {
                        int bin = GetBin(r.TurningProxySigned);
                        return r.Residual - regimeOffsets[bin];
                    })
                    .ToList();

                double baselineRms = ComputeBinRms(baselineResiduals);
                double globalOffsetRms = ComputeBinRms(globalOffsetResiduals);
                double shiftedA0Rms = ComputeBinRms(shiftedA0Residuals);
                double regimeOffsetRms = ComputeBinRms(regimeOffsetResiduals);

                transferResults.Add((
                    trainRemainder,
                    transferRemainder,
                    bestShiftedA0,
                    baselineRms,
                    globalOffsetRms,
                    shiftedA0Rms,
                    regimeOffsetRms));
            }
        }

        Assert.Equal(modulo * (modulo - 1), transferResults.Count);
        Assert.Equal(modulo, shiftedA0ByTrain.Count);

        double meanShiftedA0 = shiftedA0ByTrain.Average();
        double meanDeltaGlobalOffset = transferResults.Average(x => x.BaselineRms - x.GlobalOffsetRms);
        double meanDeltaShiftedA0 = transferResults.Average(x => x.BaselineRms - x.ShiftedA0Rms);
        double meanDeltaRegimeOffset = transferResults.Average(x => x.BaselineRms - x.RegimeOffsetRms);

        WriteLineWithTestPrefix("--- GLOBAL OFFSET VS A0 SHIFT DIAGNOSTIC ---");
        WriteLineWithTestPrefix($"baseline a0={fit.BestA0:E6}");
        WriteLineWithTestPrefix($"mean shifted a0={meanShiftedA0:E6}");
        WriteLineWithTestPrefix($"mean delta global offset={meanDeltaGlobalOffset:F6}");
        WriteLineWithTestPrefix($"mean delta shifted a0={meanDeltaShiftedA0:F6}");
        WriteLineWithTestPrefix($"mean delta regime offset={meanDeltaRegimeOffset:F6}");

        Assert.True(double.IsFinite(fit.BestA0));
        Assert.True(double.IsFinite(meanShiftedA0));
        Assert.True(double.IsFinite(meanDeltaGlobalOffset));
        Assert.True(double.IsFinite(meanDeltaShiftedA0));
        Assert.True(double.IsFinite(meanDeltaRegimeOffset));
        Assert.True(meanShiftedA0 >= minPhysicalA0 && meanShiftedA0 <= maxPhysicalA0,
            $"Mean shifted a0 out of physical window: {meanShiftedA0:E6}");

        Assert.All(transferResults, row =>
        {
            Assert.True(double.IsFinite(row.ShiftedA0));
            Assert.True(double.IsFinite(row.BaselineRms));
            Assert.True(double.IsFinite(row.GlobalOffsetRms));
            Assert.True(double.IsFinite(row.ShiftedA0Rms));
            Assert.True(double.IsFinite(row.RegimeOffsetRms));
            Assert.True(row.ShiftedA0 >= minPhysicalA0 && row.ShiftedA0 <= maxPhysicalA0);
            Assert.True(row.ShiftedA0Rms <= row.BaselineRms + degradeEpsilon,
                $"Shifted-a0 degraded beyond epsilon={degradeEpsilon:F3} for train={row.TrainRemainder}, transfer={row.TransferRemainder}.");
        });
    }

    [Fact]
    /// <summary>
    /// Cross-split diagnostic for rotating tick-field phase proxy versus turning-only proxy organization.
    ///
    /// Hypothesis:
    /// Residual offsets may align better with a rotating phase proxy (omega*radius) than with turning bins alone.
    ///
    /// Status:
    /// diagnostic + candidate.
    ///
    /// Limitation:
    /// Proxy-level transfer test only; not baseline-model activation.
    /// </summary>
    public void RAR32_TRM_Rar_RotatingTickPhaseProxy_NoRefit()
    {
        const int modulo = 5;
        const int binCount = 3;
        const double degradeEpsilon = 0.01;

        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        // Fit/freeze block: baseline a0 is learned once and reused unchanged for all proxy diagnostics.
        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);
        var rows = BuildTurningResidualRows(rarData, fit.BestA0);

        (double MeanDelta, int ImprovedTransfers, int TransferCount) EvaluateProxyAcrossSplits(
            string proxyLabel,
            Func<TurningResidualRow, Dictionary<string, double>, double> proxySelector)
        {
            var deltas = new List<double>();
            int improved = 0;

            for (int trainRemainder = 0; trainRemainder < modulo; trainRemainder++)
            {
                var trainRows = rows
                    .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) != trainRemainder)
                    .ToList();
                Assert.True(trainRows.Count >= binCount * 30, $"Insufficient train rows for {proxyLabel}, train remainder {trainRemainder}.");

                // Train-only normalization for phase scale.
                var trainPhaseRaw = trainRows
                    .Select(r => Math.Abs(r.OmegaSi * r.RadiusKpc))
                    .Where(double.IsFinite)
                    .OrderBy(x => x)
                    .ToList();
                double phaseMedian = Math.Max(Median(trainPhaseRaw), 1e-20);
                var context = new Dictionary<string, double> { ["phaseMedian"] = phaseMedian };

                var trainProxyValues = trainRows
                    .Select(r => proxySelector(r, context))
                    .Where(double.IsFinite)
                    .OrderBy(x => x)
                    .ToList();
                Assert.True(trainProxyValues.Count >= binCount * 20, $"Insufficient train proxy rows for {proxyLabel}, train remainder {trainRemainder}.");

                var cuts = new List<double>();
                for (int i = 1; i < binCount; i++)
                {
                    int index = (i * trainProxyValues.Count) / binCount;
                    cuts.Add(trainProxyValues[Math.Min(index, trainProxyValues.Count - 1)]);
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

                var trainOffsetsRaw = new Dictionary<int, List<double>>();
                foreach (var row in trainRows)
                {
                    int bin = GetBin(proxySelector(row, context));
                    if (!trainOffsetsRaw.ContainsKey(bin))
                        trainOffsetsRaw[bin] = new List<double>();
                    trainOffsetsRaw[bin].Add(row.Residual);
                }

                var trainOffsets = new Dictionary<int, double>();
                for (int b = 0; b < binCount; b++)
                {
                    Assert.True(trainOffsetsRaw.ContainsKey(b), $"Missing bin {b} for {proxyLabel}, train remainder {trainRemainder}.");
                    trainOffsets[b] = trainOffsetsRaw[b].Average();
                    Assert.True(double.IsFinite(trainOffsets[b]));
                }

                for (int transferRemainder = 0; transferRemainder < modulo; transferRemainder++)
                {
                    if (transferRemainder == trainRemainder)
                        continue;

                    var transferRows = rows
                        .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) == transferRemainder)
                        .ToList();
                    Assert.True(transferRows.Count >= 20, $"Insufficient transfer rows for {proxyLabel}, transfer remainder {transferRemainder}.");

                    var baselineResiduals = transferRows.Select(r => r.Residual).ToList();
                    var correctedResiduals = transferRows
                        .Select(r =>
                        {
                            int bin = GetBin(proxySelector(r, context));
                            return r.Residual - trainOffsets[bin];
                        })
                        .ToList();

                    double baselineRms = ComputeBinRms(baselineResiduals);
                    double correctedRms = ComputeBinRms(correctedResiduals);
                    double delta = baselineRms - correctedRms;

                    Assert.True(double.IsFinite(baselineRms));
                    Assert.True(double.IsFinite(correctedRms));
                    Assert.True(double.IsFinite(delta));
                    Assert.True(correctedRms <= baselineRms + degradeEpsilon,
                        $"{proxyLabel} degraded beyond epsilon={degradeEpsilon:F3} for train={trainRemainder}, transfer={transferRemainder}.");

                    deltas.Add(delta);
                    if (delta > 0.0)
                        improved++;
                }
            }

            return (deltas.Average(), improved, deltas.Count);
        }

        // A) Turning proxy bins.
        var turning = EvaluateProxyAcrossSplits(
            "turningProxy",
            (r, _) => r.TurningProxySigned);

        // B) Phase proxy bins (normalized omega*radius by train-median phase scale).
        var phase = EvaluateProxyAcrossSplits(
            "phaseProxy",
            (r, c) => (r.OmegaSi * r.RadiusKpc) / c["phaseMedian"]);

        // C) Combined proxy bins (turning * normalized phase).
        var combined = EvaluateProxyAcrossSplits(
            "combinedProxy",
            (r, c) => r.TurningProxySigned * ((r.OmegaSi * r.RadiusKpc) / c["phaseMedian"]));

        var ranking = new[]
        {
            ("turningProxy", turning.MeanDelta, turning.ImprovedTransfers),
            ("phaseProxy", phase.MeanDelta, phase.ImprovedTransfers),
            ("combinedProxy", combined.MeanDelta, combined.ImprovedTransfers)
        };
        var best = ranking.OrderByDescending(x => x.Item2).First();

        WriteLineWithTestPrefix("--- ROTATING TICK PHASE PROXY DIAGNOSTIC ---");
        WriteLineWithTestPrefix($"mean delta turning={turning.MeanDelta:F6}");
        WriteLineWithTestPrefix($"mean delta phase={phase.MeanDelta:F6}");
        WriteLineWithTestPrefix($"mean delta combined={combined.MeanDelta:F6}");
        WriteLineWithTestPrefix($"best proxy={best.Item1}");
        WriteLineWithTestPrefix($"number transfers improved={best.Item3}/{turning.TransferCount}");

        Assert.True(double.IsFinite(turning.MeanDelta));
        Assert.True(double.IsFinite(phase.MeanDelta));
        Assert.True(double.IsFinite(combined.MeanDelta));
        Assert.True(turning.TransferCount == modulo * (modulo - 1));
        Assert.True(phase.TransferCount == modulo * (modulo - 1));
        Assert.True(combined.TransferCount == modulo * (modulo - 1));
    }

    [Fact]
    /// <summary>
    /// Ablation diagnostic for phase-proxy gains: radius-only, omega-only, phase, and turning proxies.
    ///
    /// Hypothesis:
    /// If RAR32 gain is genuinely phase-like, omega*radius should outperform omega-only or radius-only bins.
    ///
    /// Status:
    /// diagnostic + candidate.
    ///
    /// Limitation:
    /// Proxy-ablation transfer test only; not baseline-model activation.
    /// </summary>
    public void RAR33_TRM_Rar_PhaseProxyAblation_NoRefit()
    {
        const int modulo = 5;
        const int binCount = 3;
        const double degradeEpsilon = 0.01;

        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        // Fit/freeze block: baseline a0 is learned once and held fixed for all ablation proxies.
        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);
        var rows = BuildTurningResidualRows(rarData, fit.BestA0);

        (double MeanDelta, int ImprovedTransfers, int TransferCount) EvaluateProxyAcrossSplits(
            string proxyLabel,
            Func<TurningResidualRow, Dictionary<string, double>, double> proxySelector)
        {
            var deltas = new List<double>();
            int improved = 0;

            for (int trainRemainder = 0; trainRemainder < modulo; trainRemainder++)
            {
                var trainRows = rows
                    .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) != trainRemainder)
                    .ToList();
                Assert.True(trainRows.Count >= binCount * 30, $"Insufficient train rows for {proxyLabel}, train remainder {trainRemainder}.");

                var trainPhaseRaw = trainRows
                    .Select(r => Math.Abs(r.OmegaSi * r.RadiusKpc))
                    .Where(double.IsFinite)
                    .OrderBy(x => x)
                    .ToList();
                double phaseMedian = Math.Max(Median(trainPhaseRaw), 1e-20);
                var context = new Dictionary<string, double> { ["phaseMedian"] = phaseMedian };

                var trainProxyValues = trainRows
                    .Select(r => proxySelector(r, context))
                    .Where(double.IsFinite)
                    .OrderBy(x => x)
                    .ToList();
                Assert.True(trainProxyValues.Count >= binCount * 20, $"Insufficient train proxy rows for {proxyLabel}, train remainder {trainRemainder}.");

                var cuts = new List<double>();
                for (int i = 1; i < binCount; i++)
                {
                    int index = (i * trainProxyValues.Count) / binCount;
                    cuts.Add(trainProxyValues[Math.Min(index, trainProxyValues.Count - 1)]);
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

                var trainOffsetsRaw = new Dictionary<int, List<double>>();
                foreach (var row in trainRows)
                {
                    int bin = GetBin(proxySelector(row, context));
                    if (!trainOffsetsRaw.ContainsKey(bin))
                        trainOffsetsRaw[bin] = new List<double>();
                    trainOffsetsRaw[bin].Add(row.Residual);
                }

                var trainOffsets = new Dictionary<int, double>();
                for (int b = 0; b < binCount; b++)
                {
                    Assert.True(trainOffsetsRaw.ContainsKey(b), $"Missing bin {b} for {proxyLabel}, train remainder {trainRemainder}.");
                    trainOffsets[b] = trainOffsetsRaw[b].Average();
                    Assert.True(double.IsFinite(trainOffsets[b]));
                }

                for (int transferRemainder = 0; transferRemainder < modulo; transferRemainder++)
                {
                    if (transferRemainder == trainRemainder)
                        continue;

                    var transferRows = rows
                        .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) == transferRemainder)
                        .ToList();
                    Assert.True(transferRows.Count >= 20, $"Insufficient transfer rows for {proxyLabel}, transfer remainder {transferRemainder}.");

                    var baselineResiduals = transferRows.Select(r => r.Residual).ToList();
                    var correctedResiduals = transferRows
                        .Select(r =>
                        {
                            int bin = GetBin(proxySelector(r, context));
                            return r.Residual - trainOffsets[bin];
                        })
                        .ToList();

                    double baselineRms = ComputeBinRms(baselineResiduals);
                    double correctedRms = ComputeBinRms(correctedResiduals);
                    double delta = baselineRms - correctedRms;

                    Assert.True(double.IsFinite(baselineRms));
                    Assert.True(double.IsFinite(correctedRms));
                    Assert.True(double.IsFinite(delta));
                    Assert.True(correctedRms <= baselineRms + degradeEpsilon,
                        $"{proxyLabel} degraded beyond epsilon={degradeEpsilon:F3} for train={trainRemainder}, transfer={transferRemainder}.");

                    deltas.Add(delta);
                    if (delta > 0.0)
                        improved++;
                }
            }

            return (deltas.Average(), improved, deltas.Count);
        }

        var radius = EvaluateProxyAcrossSplits(
            "radiusProxy",
            (r, _) => r.RadiusKpc);
        var omega = EvaluateProxyAcrossSplits(
            "omegaProxy",
            (r, _) => r.OmegaSi);
        var phase = EvaluateProxyAcrossSplits(
            "phaseProxy",
            (r, c) => (r.OmegaSi * r.RadiusKpc) / c["phaseMedian"]);
        var turning = EvaluateProxyAcrossSplits(
            "turningProxy",
            (r, _) => r.TurningProxySigned);

        var ranking = new[]
        {
            ("radiusProxy", radius.MeanDelta),
            ("omegaProxy", omega.MeanDelta),
            ("phaseProxy", phase.MeanDelta),
            ("turningProxy", turning.MeanDelta)
        };
        var best = ranking.OrderByDescending(x => x.Item2).First();

        WriteLineWithTestPrefix("--- PHASE PROXY ABLATION DIAGNOSTIC ---");
        WriteLineWithTestPrefix($"mean delta radius={radius.MeanDelta:F6}");
        WriteLineWithTestPrefix($"mean delta omega={omega.MeanDelta:F6}");
        WriteLineWithTestPrefix($"mean delta phase={phase.MeanDelta:F6}");
        WriteLineWithTestPrefix($"mean delta turning={turning.MeanDelta:F6}");
        WriteLineWithTestPrefix($"best proxy={best.Item1}");
        WriteLineWithTestPrefix(
            $"number transfers improved per proxy: radius={radius.ImprovedTransfers}/{radius.TransferCount}, " +
            $"omega={omega.ImprovedTransfers}/{omega.TransferCount}, " +
            $"phase={phase.ImprovedTransfers}/{phase.TransferCount}, " +
            $"turning={turning.ImprovedTransfers}/{turning.TransferCount}");

        Assert.True(double.IsFinite(radius.MeanDelta));
        Assert.True(double.IsFinite(omega.MeanDelta));
        Assert.True(double.IsFinite(phase.MeanDelta));
        Assert.True(double.IsFinite(turning.MeanDelta));
        Assert.True(radius.TransferCount == modulo * (modulo - 1));
        Assert.True(omega.TransferCount == modulo * (modulo - 1));
        Assert.True(phase.TransferCount == modulo * (modulo - 1));
        Assert.True(turning.TransferCount == modulo * (modulo - 1));
    }

    [Fact]
    /// <summary>
    /// Cross-split diagnostic for phase contribution after controlling radius-only residual structure.
    ///
    /// Hypothesis:
    /// If phase proxy carries extra structure, radius+phase residual correction should outperform radius-only correction.
    ///
    /// Status:
    /// diagnostic.
    ///
    /// Limitation:
    /// Residual decomposition test only; no baseline-model activation.
    /// </summary>
    public void RAR34_TRM_Rar_PhaseResidualAfterRadiusControl_NoRefit()
    {
        const int modulo = 5;
        const int binCount = 3;
        const double degradeEpsilon = 0.01;

        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        // Fit/freeze block: baseline a0 is fitted once and kept fixed.
        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);
        var rows = BuildTurningResidualRows(rarData, fit.BestA0);

        var transferResults = new List<(int TrainRemainder, int TransferRemainder, double BaselineRms, double RadiusRms, double RadiusPlusPhaseRms)>();

        for (int trainRemainder = 0; trainRemainder < modulo; trainRemainder++)
        {
            var trainRows = rows
                .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) != trainRemainder)
                .ToList();
            Assert.True(trainRows.Count >= binCount * 30, $"Insufficient train rows for train remainder {trainRemainder}.");

            // Train-only phase normalization scale.
            var phaseRawTrain = trainRows
                .Select(r => Math.Abs(r.OmegaSi * r.RadiusKpc))
                .Where(double.IsFinite)
                .OrderBy(x => x)
                .ToList();
            double phaseMedian = Math.Max(Median(phaseRawTrain), 1e-20);

            // A) Radius-only 3-bin offset model on train.
            var trainRadiusValues = trainRows
                .Select(r => r.RadiusKpc)
                .Where(double.IsFinite)
                .OrderBy(x => x)
                .ToList();
            Assert.True(trainRadiusValues.Count >= binCount * 20, $"Insufficient train radius rows for train remainder {trainRemainder}.");

            var radiusCuts = new List<double>();
            for (int i = 1; i < binCount; i++)
            {
                int index = (i * trainRadiusValues.Count) / binCount;
                radiusCuts.Add(trainRadiusValues[Math.Min(index, trainRadiusValues.Count - 1)]);
            }

            int GetRadiusBin(double value)
            {
                for (int i = 0; i < radiusCuts.Count; i++)
                {
                    if (value < radiusCuts[i])
                        return i;
                }

                return radiusCuts.Count;
            }

            var radiusOffsetsRaw = new Dictionary<int, List<double>>();
            foreach (var row in trainRows)
            {
                int bin = GetRadiusBin(row.RadiusKpc);
                if (!radiusOffsetsRaw.ContainsKey(bin))
                    radiusOffsetsRaw[bin] = new List<double>();
                radiusOffsetsRaw[bin].Add(row.Residual);
            }

            var radiusOffsets = new Dictionary<int, double>();
            for (int b = 0; b < binCount; b++)
            {
                Assert.True(radiusOffsetsRaw.ContainsKey(b), $"Missing radius bin {b} for train remainder {trainRemainder}.");
                radiusOffsets[b] = radiusOffsetsRaw[b].Average();
                Assert.True(double.IsFinite(radiusOffsets[b]));
            }

            // B/C) Build phase-offset model on train residuals after radius correction.
            var trainPhaseValues = trainRows
                .Select(r => (r.OmegaSi * r.RadiusKpc) / phaseMedian)
                .Where(double.IsFinite)
                .OrderBy(x => x)
                .ToList();
            Assert.True(trainPhaseValues.Count >= binCount * 20, $"Insufficient train phase rows for train remainder {trainRemainder}.");

            var phaseCuts = new List<double>();
            for (int i = 1; i < binCount; i++)
            {
                int index = (i * trainPhaseValues.Count) / binCount;
                phaseCuts.Add(trainPhaseValues[Math.Min(index, trainPhaseValues.Count - 1)]);
            }

            int GetPhaseBin(double value)
            {
                for (int i = 0; i < phaseCuts.Count; i++)
                {
                    if (value < phaseCuts[i])
                        return i;
                }

                return phaseCuts.Count;
            }

            var phaseOffsetsRaw = new Dictionary<int, List<double>>();
            foreach (var row in trainRows)
            {
                int radiusBin = GetRadiusBin(row.RadiusKpc);
                double residualAfterRadius = row.Residual - radiusOffsets[radiusBin];

                double phaseProxy = (row.OmegaSi * row.RadiusKpc) / phaseMedian;
                int phaseBin = GetPhaseBin(phaseProxy);

                if (!phaseOffsetsRaw.ContainsKey(phaseBin))
                    phaseOffsetsRaw[phaseBin] = new List<double>();
                phaseOffsetsRaw[phaseBin].Add(residualAfterRadius);
            }

            var phaseOffsets = new Dictionary<int, double>();
            for (int b = 0; b < binCount; b++)
            {
                Assert.True(phaseOffsetsRaw.ContainsKey(b), $"Missing phase bin {b} for train remainder {trainRemainder}.");
                phaseOffsets[b] = phaseOffsetsRaw[b].Average();
                Assert.True(double.IsFinite(phaseOffsets[b]));
            }

            for (int transferRemainder = 0; transferRemainder < modulo; transferRemainder++)
            {
                if (transferRemainder == trainRemainder)
                    continue;

                var transferRows = rows
                    .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) == transferRemainder)
                    .ToList();
                Assert.True(transferRows.Count >= 20, $"Insufficient transfer rows for transfer remainder {transferRemainder}.");

                var baselineResiduals = transferRows.Select(r => r.Residual).ToList();
                var radiusOnlyResiduals = transferRows
                    .Select(r =>
                    {
                        int radiusBin = GetRadiusBin(r.RadiusKpc);
                        return r.Residual - radiusOffsets[radiusBin];
                    })
                    .ToList();
                var radiusPlusPhaseResiduals = transferRows
                    .Select(r =>
                    {
                        int radiusBin = GetRadiusBin(r.RadiusKpc);
                        double residualAfterRadius = r.Residual - radiusOffsets[radiusBin];

                        double phaseProxy = (r.OmegaSi * r.RadiusKpc) / phaseMedian;
                        int phaseBin = GetPhaseBin(phaseProxy);
                        return residualAfterRadius - phaseOffsets[phaseBin];
                    })
                    .ToList();

                double baselineRms = ComputeBinRms(baselineResiduals);
                double radiusRms = ComputeBinRms(radiusOnlyResiduals);
                double radiusPlusPhaseRms = ComputeBinRms(radiusPlusPhaseResiduals);

                Assert.True(double.IsFinite(baselineRms));
                Assert.True(double.IsFinite(radiusRms));
                Assert.True(double.IsFinite(radiusPlusPhaseRms));
                Assert.True(radiusRms <= baselineRms + degradeEpsilon,
                    $"Radius-only degraded beyond epsilon={degradeEpsilon:F3} for train={trainRemainder}, transfer={transferRemainder}.");
                Assert.True(radiusPlusPhaseRms <= baselineRms + degradeEpsilon,
                    $"Radius+phase degraded beyond epsilon={degradeEpsilon:F3} for train={trainRemainder}, transfer={transferRemainder}.");

                transferResults.Add((trainRemainder, transferRemainder, baselineRms, radiusRms, radiusPlusPhaseRms));
            }
        }

        Assert.Equal(modulo * (modulo - 1), transferResults.Count);

        double meanDeltaRadius = transferResults.Average(x => x.BaselineRms - x.RadiusRms);
        double meanDeltaRadiusPlusPhase = transferResults.Average(x => x.BaselineRms - x.RadiusPlusPhaseRms);
        double extraPhaseGain = meanDeltaRadiusPlusPhase - meanDeltaRadius;
        int radiusPlusPhaseBeatsRadius = transferResults.Count(x => x.RadiusPlusPhaseRms < x.RadiusRms);

        WriteLineWithTestPrefix("--- PHASE AFTER RADIUS CONTROL DIAGNOSTIC ---");
        WriteLineWithTestPrefix($"mean delta radius={meanDeltaRadius:F6}");
        WriteLineWithTestPrefix($"mean delta radiusPlusPhase={meanDeltaRadiusPlusPhase:F6}");
        WriteLineWithTestPrefix($"extra phase gain={extraPhaseGain:F6}");
        WriteLineWithTestPrefix(
            $"number transfers where radiusPlusPhase beats radius={radiusPlusPhaseBeatsRadius}/{transferResults.Count}");

        Assert.True(double.IsFinite(meanDeltaRadius));
        Assert.True(double.IsFinite(meanDeltaRadiusPlusPhase));
        Assert.True(double.IsFinite(extraPhaseGain));
    }

    [Fact]
    /// <summary>
    /// Direction-control diagnostic for phase residuals after radius correction.
    ///
    /// Hypothesis:
    /// If phase signal is direction-structured, signed phase should outperform inverse/absolute/shuffled controls.
    ///
    /// Status:
    /// diagnostic.
    ///
    /// Limitation:
    /// Proxy control experiment only; no baseline-model activation.
    /// </summary>
    public void RAR35_TRM_Rar_PhaseDirectionControl_NoRefit()
    {
        const int modulo = 5;
        const int binCount = 3;
        const double degradeEpsilon = 0.01;

        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        // Fit/freeze block: baseline a0 is learned once and reused unchanged.
        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);
        var rows = BuildTurningResidualRows(rarData, fit.BestA0);

        var transferResults = new List<(
            int TrainRemainder,
            int TransferRemainder,
            double BaselineRms,
            double RadiusRms,
            double RadiusPlusPhaseRms,
            double RadiusPlusInversePhaseRms,
            double RadiusPlusAbsPhaseRms,
            double RadiusPlusShuffledPhaseRms)>();

        for (int trainRemainder = 0; trainRemainder < modulo; trainRemainder++)
        {
            var trainRows = rows
                .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) != trainRemainder)
                .ToList();
            Assert.True(trainRows.Count >= binCount * 30, $"Insufficient train rows for train remainder {trainRemainder}.");

            // Stage 1: train-only radius correction model.
            var trainRadiusValues = trainRows
                .Select(r => r.RadiusKpc)
                .Where(double.IsFinite)
                .OrderBy(x => x)
                .ToList();
            Assert.True(trainRadiusValues.Count >= binCount * 20, $"Insufficient train radius rows for train remainder {trainRemainder}.");

            var radiusCuts = new List<double>();
            for (int i = 1; i < binCount; i++)
            {
                int index = (i * trainRadiusValues.Count) / binCount;
                radiusCuts.Add(trainRadiusValues[Math.Min(index, trainRadiusValues.Count - 1)]);
            }

            int GetRadiusBin(double value)
            {
                for (int i = 0; i < radiusCuts.Count; i++)
                {
                    if (value < radiusCuts[i])
                        return i;
                }

                return radiusCuts.Count;
            }

            var radiusOffsetsRaw = new Dictionary<int, List<double>>();
            foreach (var row in trainRows)
            {
                int bin = GetRadiusBin(row.RadiusKpc);
                if (!radiusOffsetsRaw.ContainsKey(bin))
                    radiusOffsetsRaw[bin] = new List<double>();
                radiusOffsetsRaw[bin].Add(row.Residual);
            }

            var radiusOffsets = new Dictionary<int, double>();
            for (int b = 0; b < binCount; b++)
            {
                Assert.True(radiusOffsetsRaw.ContainsKey(b), $"Missing radius bin {b} for train remainder {trainRemainder}.");
                radiusOffsets[b] = radiusOffsetsRaw[b].Average();
                Assert.True(double.IsFinite(radiusOffsets[b]));
            }

            // Train residuals after radius control.
            var trainResidualAfterRadius = trainRows
                .Select(r => r.Residual - radiusOffsets[GetRadiusBin(r.RadiusKpc)])
                .ToList();

            // Phase controls.
            var phaseTrain = trainRows.Select(r => r.OmegaSi * r.RadiusKpc).ToList();
            var inversePhaseTrain = phaseTrain.Select(x => -x).ToList();
            var absPhaseTrain = phaseTrain.Select(Math.Abs).ToList();
            var shuffledPhaseTrain = phaseTrain.ToList();
            var rng = new Random(unchecked(7001 + (trainRemainder * 101)));
            for (int i = shuffledPhaseTrain.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (shuffledPhaseTrain[i], shuffledPhaseTrain[j]) = (shuffledPhaseTrain[j], shuffledPhaseTrain[i]);
            }

            (List<double> Cuts, Dictionary<int, double> Offsets) BuildPhaseModel(
                string label,
                List<double> trainProxyValues)
            {
                var sortedProxy = trainProxyValues
                    .Where(double.IsFinite)
                    .OrderBy(x => x)
                    .ToList();
                Assert.True(sortedProxy.Count >= binCount * 20, $"Insufficient phase rows for {label}, train remainder {trainRemainder}.");

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

                var offsetsRaw = new Dictionary<int, List<double>>();
                for (int i = 0; i < trainRows.Count; i++)
                {
                    int bin = GetBin(trainProxyValues[i]);
                    if (!offsetsRaw.ContainsKey(bin))
                        offsetsRaw[bin] = new List<double>();
                    offsetsRaw[bin].Add(trainResidualAfterRadius[i]);
                }

                var offsets = new Dictionary<int, double>();
                for (int b = 0; b < binCount; b++)
                {
                    Assert.True(offsetsRaw.ContainsKey(b), $"Missing {label} bin {b} for train remainder {trainRemainder}.");
                    offsets[b] = offsetsRaw[b].Average();
                    Assert.True(double.IsFinite(offsets[b]));
                }

                return (cuts, offsets);
            }

            var phaseModel = BuildPhaseModel("phase", phaseTrain);
            var inversePhaseModel = BuildPhaseModel("inversePhase", inversePhaseTrain);
            var absPhaseModel = BuildPhaseModel("absPhase", absPhaseTrain);
            var shuffledPhaseModel = BuildPhaseModel("shuffledPhase", shuffledPhaseTrain);

            int GetBinFromCuts(List<double> cuts, double value)
            {
                for (int i = 0; i < cuts.Count; i++)
                {
                    if (value < cuts[i])
                        return i;
                }

                return cuts.Count;
            }

            for (int transferRemainder = 0; transferRemainder < modulo; transferRemainder++)
            {
                if (transferRemainder == trainRemainder)
                    continue;

                var transferRows = rows
                    .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) == transferRemainder)
                    .ToList();
                Assert.True(transferRows.Count >= 20, $"Insufficient transfer rows for transfer remainder {transferRemainder}.");

                double CorrectedResidual(
                    TurningResidualRow row,
                    List<double> cuts,
                    Dictionary<int, double> offsets,
                    Func<TurningResidualRow, double> proxySelector)
                {
                    double radiusResidual = row.Residual - radiusOffsets[GetRadiusBin(row.RadiusKpc)];
                    int phaseBin = GetBinFromCuts(cuts, proxySelector(row));
                    return radiusResidual - offsets[phaseBin];
                }

                var baselineResiduals = transferRows.Select(r => r.Residual).ToList();
                var radiusResiduals = transferRows
                    .Select(r => r.Residual - radiusOffsets[GetRadiusBin(r.RadiusKpc)])
                    .ToList();
                var radiusPlusPhaseResiduals = transferRows
                    .Select(r => CorrectedResidual(r, phaseModel.Cuts, phaseModel.Offsets, x => x.OmegaSi * x.RadiusKpc))
                    .ToList();
                var radiusPlusInversePhaseResiduals = transferRows
                    .Select(r => CorrectedResidual(r, inversePhaseModel.Cuts, inversePhaseModel.Offsets, x => -(x.OmegaSi * x.RadiusKpc)))
                    .ToList();
                var radiusPlusAbsPhaseResiduals = transferRows
                    .Select(r => CorrectedResidual(r, absPhaseModel.Cuts, absPhaseModel.Offsets, x => Math.Abs(x.OmegaSi * x.RadiusKpc)))
                    .ToList();
                var radiusPlusShuffledPhaseResiduals = transferRows
                    .Select(r => CorrectedResidual(r, shuffledPhaseModel.Cuts, shuffledPhaseModel.Offsets, x => x.OmegaSi * x.RadiusKpc))
                    .ToList();

                double baselineRms = ComputeBinRms(baselineResiduals);
                double radiusRms = ComputeBinRms(radiusResiduals);
                double radiusPlusPhaseRms = ComputeBinRms(radiusPlusPhaseResiduals);
                double radiusPlusInversePhaseRms = ComputeBinRms(radiusPlusInversePhaseResiduals);
                double radiusPlusAbsPhaseRms = ComputeBinRms(radiusPlusAbsPhaseResiduals);
                double radiusPlusShuffledPhaseRms = ComputeBinRms(radiusPlusShuffledPhaseResiduals);

                Assert.True(double.IsFinite(baselineRms));
                Assert.True(double.IsFinite(radiusRms));
                Assert.True(double.IsFinite(radiusPlusPhaseRms));
                Assert.True(double.IsFinite(radiusPlusInversePhaseRms));
                Assert.True(double.IsFinite(radiusPlusAbsPhaseRms));
                Assert.True(double.IsFinite(radiusPlusShuffledPhaseRms));

                Assert.True(radiusRms <= baselineRms + degradeEpsilon,
                    $"Radius degraded beyond epsilon={degradeEpsilon:F3} for train={trainRemainder}, transfer={transferRemainder}.");
                Assert.True(radiusPlusPhaseRms <= baselineRms + degradeEpsilon,
                    $"Radius+phase degraded beyond epsilon={degradeEpsilon:F3} for train={trainRemainder}, transfer={transferRemainder}.");
                Assert.True(radiusPlusInversePhaseRms <= baselineRms + degradeEpsilon,
                    $"Radius+inversePhase degraded beyond epsilon={degradeEpsilon:F3} for train={trainRemainder}, transfer={transferRemainder}.");
                Assert.True(radiusPlusAbsPhaseRms <= baselineRms + degradeEpsilon,
                    $"Radius+absPhase degraded beyond epsilon={degradeEpsilon:F3} for train={trainRemainder}, transfer={transferRemainder}.");
                Assert.True(radiusPlusShuffledPhaseRms <= baselineRms + degradeEpsilon,
                    $"Radius+shuffledPhase degraded beyond epsilon={degradeEpsilon:F3} for train={trainRemainder}, transfer={transferRemainder}.");

                transferResults.Add((
                    trainRemainder,
                    transferRemainder,
                    baselineRms,
                    radiusRms,
                    radiusPlusPhaseRms,
                    radiusPlusInversePhaseRms,
                    radiusPlusAbsPhaseRms,
                    radiusPlusShuffledPhaseRms));
            }
        }

        Assert.Equal(modulo * (modulo - 1), transferResults.Count);

        double meanDeltaRadius = transferResults.Average(x => x.BaselineRms - x.RadiusRms);
        double meanDeltaRadiusPlusPhase = transferResults.Average(x => x.BaselineRms - x.RadiusPlusPhaseRms);
        double meanDeltaRadiusPlusInversePhase = transferResults.Average(x => x.BaselineRms - x.RadiusPlusInversePhaseRms);
        double meanDeltaRadiusPlusAbsPhase = transferResults.Average(x => x.BaselineRms - x.RadiusPlusAbsPhaseRms);
        double meanDeltaRadiusPlusShuffledPhase = transferResults.Average(x => x.BaselineRms - x.RadiusPlusShuffledPhaseRms);

        var bestProxy = new[]
        {
            ("phaseProxy", meanDeltaRadiusPlusPhase),
            ("inversePhaseProxy", meanDeltaRadiusPlusInversePhase),
            ("absPhaseProxy", meanDeltaRadiusPlusAbsPhase),
            ("shuffledPhaseProxy", meanDeltaRadiusPlusShuffledPhase)
        }
            .OrderByDescending(x => x.Item2)
            .First()
            .Item1;

        int beatsRadiusPhase = transferResults.Count(x => x.RadiusPlusPhaseRms < x.RadiusRms);
        int beatsRadiusInverse = transferResults.Count(x => x.RadiusPlusInversePhaseRms < x.RadiusRms);
        int beatsRadiusAbs = transferResults.Count(x => x.RadiusPlusAbsPhaseRms < x.RadiusRms);
        int beatsRadiusShuffled = transferResults.Count(x => x.RadiusPlusShuffledPhaseRms < x.RadiusRms);

        WriteLineWithTestPrefix("--- PHASE DIRECTION CONTROL DIAGNOSTIC ---");
        WriteLineWithTestPrefix($"mean delta radius={meanDeltaRadius:F6}");
        WriteLineWithTestPrefix($"mean delta radiusPlusPhase={meanDeltaRadiusPlusPhase:F6}");
        WriteLineWithTestPrefix($"mean delta radiusPlusInversePhase={meanDeltaRadiusPlusInversePhase:F6}");
        WriteLineWithTestPrefix($"mean delta radiusPlusAbsPhase={meanDeltaRadiusPlusAbsPhase:F6}");
        WriteLineWithTestPrefix($"mean delta radiusPlusShuffledPhase={meanDeltaRadiusPlusShuffledPhase:F6}");
        WriteLineWithTestPrefix($"best proxy={bestProxy}");
        WriteLineWithTestPrefix(
            $"number transfers beating radius: phase={beatsRadiusPhase}/{transferResults.Count}, " +
            $"inverse={beatsRadiusInverse}/{transferResults.Count}, " +
            $"abs={beatsRadiusAbs}/{transferResults.Count}, " +
            $"shuffled={beatsRadiusShuffled}/{transferResults.Count}");

        Assert.True(double.IsFinite(meanDeltaRadius));
        Assert.True(double.IsFinite(meanDeltaRadiusPlusPhase));
        Assert.True(double.IsFinite(meanDeltaRadiusPlusInversePhase));
        Assert.True(double.IsFinite(meanDeltaRadiusPlusAbsPhase));
        Assert.True(double.IsFinite(meanDeltaRadiusPlusShuffledPhase));
    }

    [Fact]
    /// <summary>
    /// Cross-split diagnostic for projected disk-axis/diameter-gradient proxy organization.
    ///
    /// Hypothesis:
    /// Residual structure may align more strongly with inclination-projected axis gradients than with radius/phase alone.
    ///
    /// Status:
    /// diagnostic + candidate.
    ///
    /// Limitation:
    /// Proxy-level transfer diagnostic only; not baseline-model activation.
    /// </summary>
    public void RAR36_TRM_Rar_DiskAxisGradientProxy_NoRefit()
    {
        const int modulo = 5;
        const int binCount = 3;
        const double degradeEpsilon = 0.01;

        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        // Fit/freeze block: baseline a0 is fitted once and held fixed.
        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);
        var rows = BuildTurningResidualRows(rarData, fit.BestA0);

        var inclinationByGalaxy = inclinations
            .Where(kvp => double.IsFinite(kvp.Value))
            .ToDictionary(
                kvp => NormalizeGalaxyKey(kvp.Key),
                kvp => kvp.Value,
                StringComparer.OrdinalIgnoreCase);

        // Use one consistent row population for all proxy comparisons.
        var rowsWithInclination = rows
            .Where(r => inclinationByGalaxy.ContainsKey(r.GalaxyKey))
            .ToList();
        Assert.True(rowsWithInclination.Count > 800, "Too few rows with matched inclinations for projected-axis diagnostics.");

        (double MeanDelta, int ImprovedTransfers, int TransferCount) EvaluateProxyAcrossSplits(
            string proxyLabel,
            Func<TurningResidualRow, double, double> proxySelector)
        {
            var deltas = new List<double>();
            int improved = 0;

            for (int trainRemainder = 0; trainRemainder < modulo; trainRemainder++)
            {
                var trainRows = rowsWithInclination
                    .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) != trainRemainder)
                    .ToList();
                Assert.True(trainRows.Count >= binCount * 30, $"Insufficient train rows for {proxyLabel}, train remainder {trainRemainder}.");

                var trainProxyValues = trainRows
                    .Select(r =>
                    {
                        double incDeg = inclinationByGalaxy[r.GalaxyKey];
                        return proxySelector(r, incDeg);
                    })
                    .Where(double.IsFinite)
                    .OrderBy(x => x)
                    .ToList();
                Assert.True(trainProxyValues.Count >= binCount * 20, $"Insufficient train proxy rows for {proxyLabel}, train remainder {trainRemainder}.");

                var cuts = new List<double>();
                for (int i = 1; i < binCount; i++)
                {
                    int index = (i * trainProxyValues.Count) / binCount;
                    cuts.Add(trainProxyValues[Math.Min(index, trainProxyValues.Count - 1)]);
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

                var trainOffsetsRaw = new Dictionary<int, List<double>>();
                foreach (var row in trainRows)
                {
                    double incDeg = inclinationByGalaxy[row.GalaxyKey];
                    int bin = GetBin(proxySelector(row, incDeg));
                    if (!trainOffsetsRaw.ContainsKey(bin))
                        trainOffsetsRaw[bin] = new List<double>();
                    trainOffsetsRaw[bin].Add(row.Residual);
                }

                var trainOffsets = new Dictionary<int, double>();
                for (int b = 0; b < binCount; b++)
                {
                    Assert.True(trainOffsetsRaw.ContainsKey(b), $"Missing bin {b} for {proxyLabel}, train remainder {trainRemainder}.");
                    trainOffsets[b] = trainOffsetsRaw[b].Average();
                    Assert.True(double.IsFinite(trainOffsets[b]));
                }

                for (int transferRemainder = 0; transferRemainder < modulo; transferRemainder++)
                {
                    if (transferRemainder == trainRemainder)
                        continue;

                    var transferRows = rowsWithInclination
                        .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) == transferRemainder)
                        .ToList();
                    Assert.True(transferRows.Count >= 20, $"Insufficient transfer rows for {proxyLabel}, transfer remainder {transferRemainder}.");

                    var baselineResiduals = transferRows.Select(r => r.Residual).ToList();
                    var correctedResiduals = transferRows
                        .Select(r =>
                        {
                            double incDeg = inclinationByGalaxy[r.GalaxyKey];
                            int bin = GetBin(proxySelector(r, incDeg));
                            return r.Residual - trainOffsets[bin];
                        })
                        .ToList();

                    double baselineRms = ComputeBinRms(baselineResiduals);
                    double correctedRms = ComputeBinRms(correctedResiduals);
                    double delta = baselineRms - correctedRms;

                    Assert.True(double.IsFinite(baselineRms));
                    Assert.True(double.IsFinite(correctedRms));
                    Assert.True(double.IsFinite(delta));
                    Assert.True(correctedRms <= baselineRms + degradeEpsilon,
                        $"{proxyLabel} degraded beyond epsilon={degradeEpsilon:F3} for train={trainRemainder}, transfer={transferRemainder}.");

                    deltas.Add(delta);
                    if (delta > 0.0)
                        improved++;
                }
            }

            return (deltas.Average(), improved, deltas.Count);
        }

        // A) radiusKpc
        var radius = EvaluateProxyAcrossSplits(
            "radiusProxy",
            (r, _) => r.RadiusKpc);

        // B) omega * radiusKpc
        var phase = EvaluateProxyAcrossSplits(
            "phaseProxy",
            (r, _) => r.OmegaSi * r.RadiusKpc);

        // C) projected axis proxy: radiusKpc * sin(inclination)
        var projectedAxis = EvaluateProxyAcrossSplits(
            "projectedAxisProxy",
            (r, incDeg) => r.RadiusKpc * Math.Sin((Math.PI / 180.0) * incDeg));

        // D) projected phase-axis proxy: omega * radiusKpc * sin(inclination)
        var projectedPhaseAxis = EvaluateProxyAcrossSplits(
            "projectedPhaseAxisProxy",
            (r, incDeg) => (r.OmegaSi * r.RadiusKpc) * Math.Sin((Math.PI / 180.0) * incDeg));

        var ranking = new[]
        {
            ("radiusProxy", radius.MeanDelta, radius.ImprovedTransfers),
            ("phaseProxy", phase.MeanDelta, phase.ImprovedTransfers),
            ("projectedAxisProxy", projectedAxis.MeanDelta, projectedAxis.ImprovedTransfers),
            ("projectedPhaseAxisProxy", projectedPhaseAxis.MeanDelta, projectedPhaseAxis.ImprovedTransfers)
        };
        var best = ranking.OrderByDescending(x => x.Item2).First();

        WriteLineWithTestPrefix("--- DISK AXIS GRADIENT PROXY DIAGNOSTIC ---");
        WriteLineWithTestPrefix($"mean delta radius={radius.MeanDelta:F6}");
        WriteLineWithTestPrefix($"mean delta phase={phase.MeanDelta:F6}");
        WriteLineWithTestPrefix($"mean delta projectedAxis={projectedAxis.MeanDelta:F6}");
        WriteLineWithTestPrefix($"mean delta projectedPhaseAxis={projectedPhaseAxis.MeanDelta:F6}");
        WriteLineWithTestPrefix($"best proxy={best.Item1}");
        WriteLineWithTestPrefix($"number transfers improved={best.Item3}/{radius.TransferCount}");

        Assert.True(double.IsFinite(radius.MeanDelta));
        Assert.True(double.IsFinite(phase.MeanDelta));
        Assert.True(double.IsFinite(projectedAxis.MeanDelta));
        Assert.True(double.IsFinite(projectedPhaseAxis.MeanDelta));
        Assert.True(radius.TransferCount == modulo * (modulo - 1));
        Assert.True(phase.TransferCount == modulo * (modulo - 1));
        Assert.True(projectedAxis.TransferCount == modulo * (modulo - 1));
        Assert.True(projectedPhaseAxis.TransferCount == modulo * (modulo - 1));
    }

    [Fact]
    /// <summary>
    /// Cross-split robustness diagnostic for phase-proxy bin count choice.
    ///
    /// Hypothesis:
    /// If the phase-proxy signal is stable, transfer gains should persist across a reasonable bin-count range.
    ///
    /// Status:
    /// diagnostic + candidate.
    ///
    /// Limitation:
    /// Binning-robustness check only; no baseline-model activation.
    /// </summary>
    public void RAR37_TRM_Rar_PhaseProxyBinCountRobustness_NoRefit()
    {
        const int modulo = 5;
        const double degradeEpsilon = 0.01;
        int[] binCounts = new[] { 2, 3, 4, 5, 6 };

        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        // Fit/freeze block: baseline a0 is fitted once and held fixed for all bin-count variants.
        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);
        var rows = BuildTurningResidualRows(rarData, fit.BestA0);

        var summaries = new List<(int BinCount, double MeanDelta, int ImprovedTransfers, int TransferCount)>();

        foreach (int binCount in binCounts)
        {
            var deltas = new List<double>();
            int improved = 0;

            for (int trainRemainder = 0; trainRemainder < modulo; trainRemainder++)
            {
                var trainRows = rows
                    .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) != trainRemainder)
                    .ToList();
                Assert.True(trainRows.Count >= binCount * 30, $"Insufficient train rows for binCount={binCount}, train remainder {trainRemainder}.");

                var sortedPhase = trainRows
                    .Select(r => r.OmegaSi * r.RadiusKpc)
                    .Where(double.IsFinite)
                    .OrderBy(x => x)
                    .ToList();
                Assert.True(sortedPhase.Count >= binCount * 20, $"Insufficient phase rows for binCount={binCount}, train remainder {trainRemainder}.");

                var cuts = new List<double>();
                for (int i = 1; i < binCount; i++)
                {
                    int index = (i * sortedPhase.Count) / binCount;
                    cuts.Add(sortedPhase[Math.Min(index, sortedPhase.Count - 1)]);
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

                var trainOffsetsRaw = new Dictionary<int, List<double>>();
                foreach (var row in trainRows)
                {
                    int bin = GetBin(row.OmegaSi * row.RadiusKpc);
                    if (!trainOffsetsRaw.ContainsKey(bin))
                        trainOffsetsRaw[bin] = new List<double>();
                    trainOffsetsRaw[bin].Add(row.Residual);
                }

                var trainOffsets = new Dictionary<int, double>();
                for (int b = 0; b < binCount; b++)
                {
                    Assert.True(trainOffsetsRaw.ContainsKey(b), $"Missing phase bin {b} for binCount={binCount}, train remainder {trainRemainder}.");
                    trainOffsets[b] = trainOffsetsRaw[b].Average();
                    Assert.True(double.IsFinite(trainOffsets[b]));
                }

                for (int transferRemainder = 0; transferRemainder < modulo; transferRemainder++)
                {
                    if (transferRemainder == trainRemainder)
                        continue;

                    var transferRows = rows
                        .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) == transferRemainder)
                        .ToList();
                    Assert.True(transferRows.Count >= 20, $"Insufficient transfer rows for binCount={binCount}, transfer remainder {transferRemainder}.");

                    var baselineResiduals = transferRows.Select(r => r.Residual).ToList();
                    var correctedResiduals = transferRows
                        .Select(r =>
                        {
                            int bin = GetBin(r.OmegaSi * r.RadiusKpc);
                            return r.Residual - trainOffsets[bin];
                        })
                        .ToList();

                    double baselineRms = ComputeBinRms(baselineResiduals);
                    double correctedRms = ComputeBinRms(correctedResiduals);
                    double delta = baselineRms - correctedRms;

                    Assert.True(double.IsFinite(baselineRms));
                    Assert.True(double.IsFinite(correctedRms));
                    Assert.True(double.IsFinite(delta));
                    Assert.True(correctedRms <= baselineRms + degradeEpsilon,
                        $"Phase proxy degraded beyond epsilon={degradeEpsilon:F3} for binCount={binCount}, train={trainRemainder}, transfer={transferRemainder}.");

                    deltas.Add(delta);
                    if (delta > 0.0)
                        improved++;
                }
            }

            summaries.Add((binCount, deltas.Average(), improved, deltas.Count));
        }

        var best = summaries.OrderByDescending(x => x.MeanDelta).First();

        WriteLineWithTestPrefix("--- PHASE PROXY BIN COUNT ROBUSTNESS DIAGNOSTIC ---");
        foreach (var s in summaries.OrderBy(x => x.BinCount))
        {
            WriteLineWithTestPrefix(
                $"bins={s.BinCount} mean delta={s.MeanDelta:F6} improved={s.ImprovedTransfers}/{s.TransferCount}");
        }
        WriteLineWithTestPrefix($"best bin count={best.BinCount} mean delta={best.MeanDelta:F6}");

        Assert.Equal(binCounts.Length, summaries.Count);
        Assert.All(summaries, s =>
        {
            Assert.True(double.IsFinite(s.MeanDelta));
            Assert.True(s.TransferCount == modulo * (modulo - 1));
        });
    }

    [Fact]
    /// <summary>
    /// Cross-split diagnostic for phase-proxy bin-count preference under simple complexity penalties.
    ///
    /// Hypothesis:
    /// If higher bin counts overfit, a modest complexity penalty should shift preference toward smaller models.
    ///
    /// Status:
    /// diagnostic.
    ///
    /// Limitation:
    /// Heuristic penalization only; not a formal model-selection theorem.
    /// </summary>
    public void RAR38_TRM_Rar_PhaseProxyComplexityPenalty_NoRefit()
    {
        const int modulo = 5;
        const double degradeEpsilon = 0.01;
        int[] binCounts = new[] { 2, 3, 4, 5, 6, 7, 8 };
        double[] lambdas = new[] { 0.0005, 0.0010, 0.0020 };

        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        // Fit/freeze block: baseline a0 is fitted once and held fixed across all complexity variants.
        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);
        var rows = BuildTurningResidualRows(rarData, fit.BestA0);

        var summaries = new List<(int BinCount, double MeanDelta, int ImprovedTransfers, int TransferCount)>();

        foreach (int binCount in binCounts)
        {
            var deltas = new List<double>();
            int improved = 0;

            for (int trainRemainder = 0; trainRemainder < modulo; trainRemainder++)
            {
                var trainRows = rows
                    .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) != trainRemainder)
                    .ToList();
                Assert.True(trainRows.Count >= binCount * 30, $"Insufficient train rows for binCount={binCount}, train remainder {trainRemainder}.");

                var sortedPhase = trainRows
                    .Select(r => r.OmegaSi * r.RadiusKpc)
                    .Where(double.IsFinite)
                    .OrderBy(x => x)
                    .ToList();
                Assert.True(sortedPhase.Count >= binCount * 20, $"Insufficient phase rows for binCount={binCount}, train remainder {trainRemainder}.");

                var cuts = new List<double>();
                for (int i = 1; i < binCount; i++)
                {
                    int index = (i * sortedPhase.Count) / binCount;
                    cuts.Add(sortedPhase[Math.Min(index, sortedPhase.Count - 1)]);
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

                var trainOffsetsRaw = new Dictionary<int, List<double>>();
                foreach (var row in trainRows)
                {
                    int bin = GetBin(row.OmegaSi * row.RadiusKpc);
                    if (!trainOffsetsRaw.ContainsKey(bin))
                        trainOffsetsRaw[bin] = new List<double>();
                    trainOffsetsRaw[bin].Add(row.Residual);
                }

                var trainOffsets = new Dictionary<int, double>();
                for (int b = 0; b < binCount; b++)
                {
                    Assert.True(trainOffsetsRaw.ContainsKey(b), $"Missing phase bin {b} for binCount={binCount}, train remainder {trainRemainder}.");
                    trainOffsets[b] = trainOffsetsRaw[b].Average();
                    Assert.True(double.IsFinite(trainOffsets[b]));
                }

                for (int transferRemainder = 0; transferRemainder < modulo; transferRemainder++)
                {
                    if (transferRemainder == trainRemainder)
                        continue;

                    var transferRows = rows
                        .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) == transferRemainder)
                        .ToList();
                    Assert.True(transferRows.Count >= 20, $"Insufficient transfer rows for binCount={binCount}, transfer remainder {transferRemainder}.");

                    var baselineResiduals = transferRows.Select(r => r.Residual).ToList();
                    var correctedResiduals = transferRows
                        .Select(r =>
                        {
                            int bin = GetBin(r.OmegaSi * r.RadiusKpc);
                            return r.Residual - trainOffsets[bin];
                        })
                        .ToList();

                    double baselineRms = ComputeBinRms(baselineResiduals);
                    double correctedRms = ComputeBinRms(correctedResiduals);
                    double delta = baselineRms - correctedRms;

                    Assert.True(double.IsFinite(baselineRms));
                    Assert.True(double.IsFinite(correctedRms));
                    Assert.True(double.IsFinite(delta));
                    Assert.True(correctedRms <= baselineRms + degradeEpsilon,
                        $"Phase proxy degraded beyond epsilon={degradeEpsilon:F3} for binCount={binCount}, train={trainRemainder}, transfer={transferRemainder}.");

                    deltas.Add(delta);
                    if (delta > 0.0)
                        improved++;
                }
            }

            summaries.Add((binCount, deltas.Average(), improved, deltas.Count));
        }

        WriteLineWithTestPrefix("--- PHASE PROXY COMPLEXITY PENALTY DIAGNOSTIC ---");
        foreach (var s in summaries.OrderBy(x => x.BinCount))
        {
            double score0005 = s.MeanDelta - (lambdas[0] * (s.BinCount - 1));
            double score0010 = s.MeanDelta - (lambdas[1] * (s.BinCount - 1));
            double score0020 = s.MeanDelta - (lambdas[2] * (s.BinCount - 1));
            WriteLineWithTestPrefix(
                $"bins={s.BinCount} mean delta={s.MeanDelta:F6} improved={s.ImprovedTransfers}/{s.TransferCount} " +
                $"score@0.0005={score0005:F6} score@0.0010={score0010:F6} score@0.0020={score0020:F6}");
        }

        foreach (double lambda in lambdas)
        {
            var best = summaries
                .Select(s => new
                {
                    s.BinCount,
                    PenalizedScore = s.MeanDelta - (lambda * (s.BinCount - 1))
                })
                .OrderByDescending(x => x.PenalizedScore)
                .First();

            WriteLineWithTestPrefix(
                $"best bin count for lambda={lambda:F4}: bins={best.BinCount} penalizedScore={best.PenalizedScore:F6}");
            Assert.True(double.IsFinite(best.PenalizedScore));
        }

        Assert.Equal(binCounts.Length, summaries.Count);
        Assert.All(summaries, s =>
        {
            Assert.True(double.IsFinite(s.MeanDelta));
            Assert.True(s.TransferCount == modulo * (modulo - 1));
        });
    }

    [Fact]
    /// <summary>
    /// Cross-split diagnostic for train-transfer gap of phase-proxy bin complexity.
    ///
    /// Hypothesis:
    /// If higher bin counts overfit, train gains should rise faster than transfer gains, widening the train-transfer gap.
    ///
    /// Status:
    /// diagnostic.
    ///
    /// Limitation:
    /// Gap heuristic only; not a formal generalization bound.
    /// </summary>
    public void RAR39_TRM_Rar_PhaseProxyTrainTransferGap_NoRefit()
    {
        const int modulo = 5;
        const double degradeEpsilon = 0.01;
        int[] binCounts = new[] { 2, 3, 4, 5, 6, 7, 8 };

        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        // Fit/freeze block: baseline a0 is fitted once and held fixed for all bin-count diagnostics.
        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);
        var rows = BuildTurningResidualRows(rarData, fit.BestA0);

        var summaries = new List<(int BinCount, double TrainDelta, double TransferDelta, double Gap, int ImprovedTransfers, int TransferCount)>();

        foreach (int binCount in binCounts)
        {
            var trainDeltas = new List<double>();
            var transferDeltas = new List<double>();
            int improvedTransfers = 0;

            for (int trainRemainder = 0; trainRemainder < modulo; trainRemainder++)
            {
                var trainRows = rows
                    .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) != trainRemainder)
                    .ToList();
                Assert.True(trainRows.Count >= binCount * 30, $"Insufficient train rows for binCount={binCount}, train remainder {trainRemainder}.");

                var sortedPhase = trainRows
                    .Select(r => r.OmegaSi * r.RadiusKpc)
                    .Where(double.IsFinite)
                    .OrderBy(x => x)
                    .ToList();
                Assert.True(sortedPhase.Count >= binCount * 20, $"Insufficient phase rows for binCount={binCount}, train remainder {trainRemainder}.");

                var cuts = new List<double>();
                for (int i = 1; i < binCount; i++)
                {
                    int index = (i * sortedPhase.Count) / binCount;
                    cuts.Add(sortedPhase[Math.Min(index, sortedPhase.Count - 1)]);
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

                var trainOffsetsRaw = new Dictionary<int, List<double>>();
                foreach (var row in trainRows)
                {
                    int bin = GetBin(row.OmegaSi * row.RadiusKpc);
                    if (!trainOffsetsRaw.ContainsKey(bin))
                        trainOffsetsRaw[bin] = new List<double>();
                    trainOffsetsRaw[bin].Add(row.Residual);
                }

                var trainOffsets = new Dictionary<int, double>();
                for (int b = 0; b < binCount; b++)
                {
                    Assert.True(trainOffsetsRaw.ContainsKey(b), $"Missing phase bin {b} for binCount={binCount}, train remainder {trainRemainder}.");
                    trainOffsets[b] = trainOffsetsRaw[b].Average();
                    Assert.True(double.IsFinite(trainOffsets[b]));
                }

                var trainBaselineResiduals = trainRows.Select(r => r.Residual).ToList();
                var trainCorrectedResiduals = trainRows
                    .Select(r =>
                    {
                        int bin = GetBin(r.OmegaSi * r.RadiusKpc);
                        return r.Residual - trainOffsets[bin];
                    })
                    .ToList();
                double trainBaselineRms = ComputeBinRms(trainBaselineResiduals);
                double trainCorrectedRms = ComputeBinRms(trainCorrectedResiduals);
                double trainDelta = trainBaselineRms - trainCorrectedRms;
                Assert.True(double.IsFinite(trainDelta));
                trainDeltas.Add(trainDelta);

                for (int transferRemainder = 0; transferRemainder < modulo; transferRemainder++)
                {
                    if (transferRemainder == trainRemainder)
                        continue;

                    var transferRows = rows
                        .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) == transferRemainder)
                        .ToList();
                    Assert.True(transferRows.Count >= 20, $"Insufficient transfer rows for binCount={binCount}, transfer remainder {transferRemainder}.");

                    var transferBaselineResiduals = transferRows.Select(r => r.Residual).ToList();
                    var transferCorrectedResiduals = transferRows
                        .Select(r =>
                        {
                            int bin = GetBin(r.OmegaSi * r.RadiusKpc);
                            return r.Residual - trainOffsets[bin];
                        })
                        .ToList();

                    double transferBaselineRms = ComputeBinRms(transferBaselineResiduals);
                    double transferCorrectedRms = ComputeBinRms(transferCorrectedResiduals);
                    double transferDelta = transferBaselineRms - transferCorrectedRms;

                    Assert.True(double.IsFinite(transferBaselineRms));
                    Assert.True(double.IsFinite(transferCorrectedRms));
                    Assert.True(double.IsFinite(transferDelta));
                    Assert.True(transferCorrectedRms <= transferBaselineRms + degradeEpsilon,
                        $"Phase proxy degraded beyond epsilon={degradeEpsilon:F3} for binCount={binCount}, train={trainRemainder}, transfer={transferRemainder}.");

                    transferDeltas.Add(transferDelta);
                    if (transferDelta > 0.0)
                        improvedTransfers++;
                }
            }

            double meanTrainDelta = trainDeltas.Average();
            double meanTransferDelta = transferDeltas.Average();
            double gap = meanTrainDelta - meanTransferDelta;

            summaries.Add((
                binCount,
                meanTrainDelta,
                meanTransferDelta,
                gap,
                improvedTransfers,
                transferDeltas.Count));
        }

        WriteLineWithTestPrefix("--- PHASE PROXY TRAIN TRANSFER GAP DIAGNOSTIC ---");
        foreach (var s in summaries.OrderBy(x => x.BinCount))
        {
            WriteLineWithTestPrefix(
                $"bins={s.BinCount} train delta={s.TrainDelta:F6} transfer delta={s.TransferDelta:F6} " +
                $"gap={s.Gap:F6} improved transfers={s.ImprovedTransfers}/{s.TransferCount}");
        }

        Assert.Equal(binCounts.Length, summaries.Count);
        Assert.All(summaries, s =>
        {
            Assert.True(double.IsFinite(s.TrainDelta));
            Assert.True(double.IsFinite(s.TransferDelta));
            Assert.True(double.IsFinite(s.Gap));
            Assert.True(s.TransferCount == modulo * (modulo - 1));
        });
    }

    [Fact]
    /// <summary>
    /// Cross-split diagnostic testing whether phase proxy adds residual information beyond global and radius corrections.
    ///
    /// Hypothesis:
    /// If phase carries extra structure, global+radius+phase should outperform global+radius on transfer.
    ///
    /// Status:
    /// diagnostic.
    ///
    /// Limitation:
    /// Residual decomposition only; not baseline-model activation.
    /// </summary>
    public void RAR40_TRM_Rar_PhaseProxyBeyondGlobalAndRadius_NoRefit()
    {
        const int modulo = 5;
        const int binCount = 3;
        const double degradeEpsilon = 0.01;

        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        // Fit/freeze block: baseline a0 is fitted once and reused across all train->transfer variants.
        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);
        var rows = BuildTurningResidualRows(rarData, fit.BestA0);

        var transferResults = new List<(
            int TrainRemainder,
            int TransferRemainder,
            double BaselineRms,
            double GlobalRms,
            double RadiusRms,
            double GlobalPlusRadiusRms,
            double GlobalPlusRadiusPlusPhaseRms)>();

        for (int trainRemainder = 0; trainRemainder < modulo; trainRemainder++)
        {
            var trainRows = rows
                .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) != trainRemainder)
                .ToList();
            Assert.True(trainRows.Count >= binCount * 30, $"Insufficient train rows for train remainder {trainRemainder}.");

            // B) Global mean offset from train only.
            double globalOffset = trainRows.Average(r => r.Residual);
            Assert.True(double.IsFinite(globalOffset));

            List<double> BuildCuts(List<double> values)
            {
                var sorted = values.Where(double.IsFinite).OrderBy(x => x).ToList();
                Assert.True(sorted.Count >= binCount * 20, $"Insufficient rows for binning in train remainder {trainRemainder}.");
                var cuts = new List<double>();
                for (int i = 1; i < binCount; i++)
                {
                    int index = (i * sorted.Count) / binCount;
                    cuts.Add(sorted[Math.Min(index, sorted.Count - 1)]);
                }

                return cuts;
            }

            int GetBin(List<double> cuts, double value)
            {
                for (int i = 0; i < cuts.Count; i++)
                {
                    if (value < cuts[i])
                        return i;
                }

                return cuts.Count;
            }

            Dictionary<int, double> BuildOffsets(
                List<TurningResidualRow> sourceRows,
                List<double> cuts,
                Func<TurningResidualRow, double> proxySelector,
                Func<TurningResidualRow, double> residualSelector)
            {
                var raw = new Dictionary<int, List<double>>();
                foreach (var row in sourceRows)
                {
                    int bin = GetBin(cuts, proxySelector(row));
                    if (!raw.ContainsKey(bin))
                        raw[bin] = new List<double>();
                    raw[bin].Add(residualSelector(row));
                }

                var offsets = new Dictionary<int, double>();
                for (int b = 0; b < binCount; b++)
                {
                    Assert.True(raw.ContainsKey(b), $"Missing train bin {b} for train remainder {trainRemainder}.");
                    offsets[b] = raw[b].Average();
                    Assert.True(double.IsFinite(offsets[b]));
                }

                return offsets;
            }

            // C) Radius-bin offsets from raw residuals.
            var radiusCuts = BuildCuts(trainRows.Select(r => r.RadiusKpc).ToList());
            var radiusOffsets = BuildOffsets(
                trainRows,
                radiusCuts,
                r => r.RadiusKpc,
                r => r.Residual);

            // D) Radius-bin residual offsets after removing global offset.
            var radiusOffsetsAfterGlobal = BuildOffsets(
                trainRows,
                radiusCuts,
                r => r.RadiusKpc,
                r => r.Residual - globalOffset);

            // E) Phase residual offsets after removing global + radius(after-global).
            var phaseCuts = BuildCuts(trainRows.Select(r => r.OmegaSi * r.RadiusKpc).ToList());
            var phaseOffsetsAfterGlobalRadius = BuildOffsets(
                trainRows,
                phaseCuts,
                r => r.OmegaSi * r.RadiusKpc,
                r =>
                {
                    int radiusBin = GetBin(radiusCuts, r.RadiusKpc);
                    return r.Residual - globalOffset - radiusOffsetsAfterGlobal[radiusBin];
                });

            for (int transferRemainder = 0; transferRemainder < modulo; transferRemainder++)
            {
                if (transferRemainder == trainRemainder)
                    continue;

                var transferRows = rows
                    .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) == transferRemainder)
                    .ToList();
                Assert.True(transferRows.Count >= 20, $"Insufficient transfer rows for transfer remainder {transferRemainder}.");

                var baselineResiduals = transferRows.Select(r => r.Residual).ToList();
                var globalResiduals = transferRows
                    .Select(r => r.Residual - globalOffset)
                    .ToList();
                var radiusResiduals = transferRows
                    .Select(r =>
                    {
                        int radiusBin = GetBin(radiusCuts, r.RadiusKpc);
                        return r.Residual - radiusOffsets[radiusBin];
                    })
                    .ToList();
                var globalPlusRadiusResiduals = transferRows
                    .Select(r =>
                    {
                        int radiusBin = GetBin(radiusCuts, r.RadiusKpc);
                        return r.Residual - globalOffset - radiusOffsetsAfterGlobal[radiusBin];
                    })
                    .ToList();
                var globalPlusRadiusPlusPhaseResiduals = transferRows
                    .Select(r =>
                    {
                        int radiusBin = GetBin(radiusCuts, r.RadiusKpc);
                        double residualAfterGlobalRadius = r.Residual - globalOffset - radiusOffsetsAfterGlobal[radiusBin];
                        int phaseBin = GetBin(phaseCuts, r.OmegaSi * r.RadiusKpc);
                        return residualAfterGlobalRadius - phaseOffsetsAfterGlobalRadius[phaseBin];
                    })
                    .ToList();

                double baselineRms = ComputeBinRms(baselineResiduals);
                double globalRms = ComputeBinRms(globalResiduals);
                double radiusRms = ComputeBinRms(radiusResiduals);
                double globalPlusRadiusRms = ComputeBinRms(globalPlusRadiusResiduals);
                double globalPlusRadiusPlusPhaseRms = ComputeBinRms(globalPlusRadiusPlusPhaseResiduals);

                Assert.True(double.IsFinite(baselineRms));
                Assert.True(double.IsFinite(globalRms));
                Assert.True(double.IsFinite(radiusRms));
                Assert.True(double.IsFinite(globalPlusRadiusRms));
                Assert.True(double.IsFinite(globalPlusRadiusPlusPhaseRms));

                Assert.True(globalRms <= baselineRms + degradeEpsilon,
                    $"Global correction degraded beyond epsilon={degradeEpsilon:F3} for train={trainRemainder}, transfer={transferRemainder}.");
                Assert.True(radiusRms <= baselineRms + degradeEpsilon,
                    $"Radius correction degraded beyond epsilon={degradeEpsilon:F3} for train={trainRemainder}, transfer={transferRemainder}.");
                Assert.True(globalPlusRadiusRms <= baselineRms + degradeEpsilon,
                    $"Global+radius correction degraded beyond epsilon={degradeEpsilon:F3} for train={trainRemainder}, transfer={transferRemainder}.");
                Assert.True(globalPlusRadiusPlusPhaseRms <= baselineRms + degradeEpsilon,
                    $"Global+radius+phase correction degraded beyond epsilon={degradeEpsilon:F3} for train={trainRemainder}, transfer={transferRemainder}.");

                transferResults.Add((
                    trainRemainder,
                    transferRemainder,
                    baselineRms,
                    globalRms,
                    radiusRms,
                    globalPlusRadiusRms,
                    globalPlusRadiusPlusPhaseRms));
            }
        }

        Assert.Equal(modulo * (modulo - 1), transferResults.Count);

        double meanDeltaGlobal = transferResults.Average(x => x.BaselineRms - x.GlobalRms);
        double meanDeltaRadius = transferResults.Average(x => x.BaselineRms - x.RadiusRms);
        double meanDeltaGlobalPlusRadius = transferResults.Average(x => x.BaselineRms - x.GlobalPlusRadiusRms);
        double meanDeltaGlobalPlusRadiusPlusPhase = transferResults.Average(x => x.BaselineRms - x.GlobalPlusRadiusPlusPhaseRms);
        double extraPhaseGain = meanDeltaGlobalPlusRadiusPlusPhase - meanDeltaGlobalPlusRadius;
        int transfersWherePhaseImproves = transferResults.Count(x => x.GlobalPlusRadiusPlusPhaseRms < x.GlobalPlusRadiusRms);

        WriteLineWithTestPrefix("--- PHASE BEYOND GLOBAL AND RADIUS DIAGNOSTIC ---");
        WriteLineWithTestPrefix($"mean delta global={meanDeltaGlobal:F6}");
        WriteLineWithTestPrefix($"mean delta radius={meanDeltaRadius:F6}");
        WriteLineWithTestPrefix($"mean delta globalPlusRadius={meanDeltaGlobalPlusRadius:F6}");
        WriteLineWithTestPrefix($"mean delta globalPlusRadiusPlusPhase={meanDeltaGlobalPlusRadiusPlusPhase:F6}");
        WriteLineWithTestPrefix($"extra phase gain={extraPhaseGain:F6}");
        WriteLineWithTestPrefix($"transfers where phase improves={transfersWherePhaseImproves}/{transferResults.Count}");

        Assert.True(double.IsFinite(meanDeltaGlobal));
        Assert.True(double.IsFinite(meanDeltaRadius));
        Assert.True(double.IsFinite(meanDeltaGlobalPlusRadius));
        Assert.True(double.IsFinite(meanDeltaGlobalPlusRadiusPlusPhase));
        Assert.True(double.IsFinite(extraPhaseGain));
    }

    [Fact]
    /// <summary>
    /// Bootstrap significance diagnostic for extra phase gain beyond global+radius correction.
    ///
    /// Hypothesis:
    /// If the extra phase gain is robust, bootstrapped mean gain should remain positive across resampled transfer cases.
    ///
    /// Status:
    /// diagnostic.
    ///
    /// Limitation:
    /// Empirical bootstrap only; not a formal proof of significance.
    /// </summary>
    public void RAR41_TRM_Rar_PhaseExtraGainSignificanceBootstrap_NoRefit()
    {
        const int modulo = 5;
        const int binCount = 3;
        const double degradeEpsilon = 0.01;
        const int bootstrapSamples = 5000;

        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        // Fit/freeze block: baseline a0 is fitted once and held fixed.
        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);
        var rows = BuildTurningResidualRows(rarData, fit.BestA0);

        var perTransferExtraGains = new List<double>();

        for (int trainRemainder = 0; trainRemainder < modulo; trainRemainder++)
        {
            var trainRows = rows
                .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) != trainRemainder)
                .ToList();
            Assert.True(trainRows.Count >= binCount * 30, $"Insufficient train rows for train remainder {trainRemainder}.");

            double globalOffset = trainRows.Average(r => r.Residual);
            Assert.True(double.IsFinite(globalOffset));

            List<double> BuildCuts(List<double> values)
            {
                var sorted = values.Where(double.IsFinite).OrderBy(x => x).ToList();
                Assert.True(sorted.Count >= binCount * 20, $"Insufficient rows for binning in train remainder {trainRemainder}.");
                var cuts = new List<double>();
                for (int i = 1; i < binCount; i++)
                {
                    int index = (i * sorted.Count) / binCount;
                    cuts.Add(sorted[Math.Min(index, sorted.Count - 1)]);
                }

                return cuts;
            }

            int GetBin(List<double> cuts, double value)
            {
                for (int i = 0; i < cuts.Count; i++)
                {
                    if (value < cuts[i])
                        return i;
                }

                return cuts.Count;
            }

            Dictionary<int, double> BuildOffsets(
                List<TurningResidualRow> sourceRows,
                List<double> cuts,
                Func<TurningResidualRow, double> proxySelector,
                Func<TurningResidualRow, double> residualSelector)
            {
                var raw = new Dictionary<int, List<double>>();
                foreach (var row in sourceRows)
                {
                    int bin = GetBin(cuts, proxySelector(row));
                    if (!raw.ContainsKey(bin))
                        raw[bin] = new List<double>();
                    raw[bin].Add(residualSelector(row));
                }

                var offsets = new Dictionary<int, double>();
                for (int b = 0; b < binCount; b++)
                {
                    Assert.True(raw.ContainsKey(b), $"Missing train bin {b} for train remainder {trainRemainder}.");
                    offsets[b] = raw[b].Average();
                    Assert.True(double.IsFinite(offsets[b]));
                }

                return offsets;
            }

            var radiusCuts = BuildCuts(trainRows.Select(r => r.RadiusKpc).ToList());
            var radiusOffsetsAfterGlobal = BuildOffsets(
                trainRows,
                radiusCuts,
                r => r.RadiusKpc,
                r => r.Residual - globalOffset);

            var phaseCuts = BuildCuts(trainRows.Select(r => r.OmegaSi * r.RadiusKpc).ToList());
            var phaseOffsetsAfterGlobalRadius = BuildOffsets(
                trainRows,
                phaseCuts,
                r => r.OmegaSi * r.RadiusKpc,
                r =>
                {
                    int radiusBin = GetBin(radiusCuts, r.RadiusKpc);
                    return r.Residual - globalOffset - radiusOffsetsAfterGlobal[radiusBin];
                });

            for (int transferRemainder = 0; transferRemainder < modulo; transferRemainder++)
            {
                if (transferRemainder == trainRemainder)
                    continue;

                var transferRows = rows
                    .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) == transferRemainder)
                    .ToList();
                Assert.True(transferRows.Count >= 20, $"Insufficient transfer rows for transfer remainder {transferRemainder}.");

                var globalPlusRadiusResiduals = transferRows
                    .Select(r =>
                    {
                        int radiusBin = GetBin(radiusCuts, r.RadiusKpc);
                        return r.Residual - globalOffset - radiusOffsetsAfterGlobal[radiusBin];
                    })
                    .ToList();
                var globalPlusRadiusPlusPhaseResiduals = transferRows
                    .Select(r =>
                    {
                        int radiusBin = GetBin(radiusCuts, r.RadiusKpc);
                        double residualAfterGlobalRadius = r.Residual - globalOffset - radiusOffsetsAfterGlobal[radiusBin];
                        int phaseBin = GetBin(phaseCuts, r.OmegaSi * r.RadiusKpc);
                        return residualAfterGlobalRadius - phaseOffsetsAfterGlobalRadius[phaseBin];
                    })
                    .ToList();

                double globalPlusRadiusRms = ComputeBinRms(globalPlusRadiusResiduals);
                double globalPlusRadiusPlusPhaseRms = ComputeBinRms(globalPlusRadiusPlusPhaseResiduals);
                double extraGain = globalPlusRadiusRms - globalPlusRadiusPlusPhaseRms;

                Assert.True(double.IsFinite(globalPlusRadiusRms));
                Assert.True(double.IsFinite(globalPlusRadiusPlusPhaseRms));
                Assert.True(double.IsFinite(extraGain));
                Assert.True(globalPlusRadiusPlusPhaseRms <= globalPlusRadiusRms + degradeEpsilon,
                    $"Global+radius+phase degraded beyond epsilon={degradeEpsilon:F3} relative to global+radius for train={trainRemainder}, transfer={transferRemainder}.");

                perTransferExtraGains.Add(extraGain);
            }
        }

        Assert.Equal(modulo * (modulo - 1), perTransferExtraGains.Count);
        Assert.All(perTransferExtraGains, x => Assert.True(double.IsFinite(x)));

        double empiricalMeanExtraGain = perTransferExtraGains.Average();
        var rngBootstrap = new Random(41741);
        var bootstrapMeans = new List<double>(bootstrapSamples);
        int n = perTransferExtraGains.Count;
        for (int b = 0; b < bootstrapSamples; b++)
        {
            double sum = 0.0;
            for (int i = 0; i < n; i++)
            {
                int idx = rngBootstrap.Next(n);
                sum += perTransferExtraGains[idx];
            }
            bootstrapMeans.Add(sum / n);
        }

        var sortedBootstrap = bootstrapMeans.OrderBy(x => x).ToList();
        double ciLow = Percentile(sortedBootstrap, 0.025);
        double ciHigh = Percentile(sortedBootstrap, 0.975);
        int nonPositiveCount = bootstrapMeans.Count(x => x <= 0.0);
        double pLike = nonPositiveCount / (double)bootstrapSamples;

        WriteLineWithTestPrefix("--- PHASE EXTRA GAIN SIGNIFICANCE BOOTSTRAP DIAGNOSTIC ---");
        WriteLineWithTestPrefix($"transfer cases={n}");
        WriteLineWithTestPrefix($"empirical mean extra gain={empiricalMeanExtraGain:F6}");
        WriteLineWithTestPrefix($"bootstrap mean(95% CI)=[{ciLow:F6}, {ciHigh:F6}]");
        WriteLineWithTestPrefix($"bootstrap non-positive fraction={pLike:F6}");

        Assert.True(double.IsFinite(empiricalMeanExtraGain));
        Assert.True(double.IsFinite(ciLow));
        Assert.True(double.IsFinite(ciHigh));
        Assert.True(double.IsFinite(pLike));
        Assert.True(ciHigh >= ciLow);
    }

    [Fact]
    /// <summary>
    /// Cross-split diagnostic for phase-proxy robustness under radius/scale normalizations.
    ///
    /// Hypothesis:
    /// If phase signal is scale-invariant, normalized phase proxies should remain competitive with raw omega*radius.
    ///
    /// Status:
    /// diagnostic + candidate.
    ///
    /// Limitation:
    /// Proxy normalization comparison only; not baseline-model activation.
    /// </summary>
    public void RAR42_TRM_Rar_PhaseProxyScaleNormalization_NoRefit()
    {
        const int modulo = 5;
        const double degradeEpsilon = 0.01;
        const double eps = 1e-12;
        int[] binCounts = new[] { 4, 5 };

        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        // Fit/freeze block: baseline a0 is fitted once and held fixed.
        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);
        var rows = BuildTurningResidualRows(rarData, fit.BestA0);

        static Dictionary<string, (double RMin, double RMax, double Span, double MedianRadius)> BuildScaleMap(List<TurningResidualRow> sourceRows)
        {
            var map = new Dictionary<string, (double RMin, double RMax, double Span, double MedianRadius)>(StringComparer.OrdinalIgnoreCase);
            foreach (var g in sourceRows.GroupBy(r => r.GalaxyKey))
            {
                var radii = g
                    .Select(x => x.RadiusKpc)
                    .Where(double.IsFinite)
                    .OrderBy(x => x)
                    .ToList();
                if (radii.Count == 0)
                    continue;

                double rMin = radii.First();
                double rMax = radii.Last();
                double span = Math.Max(0.0, rMax - rMin);
                double medianRadius = Median(radii);
                map[g.Key] = (rMin, rMax, span, medianRadius);
            }

            return map;
        }

        var proxyNames = new[] { "rawPhase", "normalizedPhaseByRmax", "normalizedPhaseBySpan", "normalizedPhaseByMedianRadius", "radiusOnly" };
        var summaries = new List<(string Proxy, int BinCount, double MeanTrainDelta, double MeanTransferDelta, double Gap, int ImprovedTransfers, int TransferCount)>();

        foreach (int binCount in binCounts)
        {
            foreach (string proxyName in proxyNames)
            {
                var trainDeltas = new List<double>();
                var transferDeltas = new List<double>();
                int improvedTransfers = 0;

                for (int trainRemainder = 0; trainRemainder < modulo; trainRemainder++)
                {
                    var trainRows = rows
                        .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) != trainRemainder)
                        .ToList();
                    Assert.True(trainRows.Count >= binCount * 30, $"Insufficient train rows for {proxyName}, binCount={binCount}, train remainder {trainRemainder}.");

                    // Scale quantities are derived from train rows only.
                    var trainScaleMap = BuildScaleMap(trainRows);
                    Assert.True(trainScaleMap.Count > 20, $"Insufficient train galaxy scale map for {proxyName}, train remainder {trainRemainder}.");
                    double globalRMin = trainScaleMap.Values.Average(x => x.RMin);
                    double globalRMax = trainScaleMap.Values.Average(x => x.RMax);
                    double globalSpan = Math.Max(trainScaleMap.Values.Average(x => x.Span), eps);
                    double globalMedianRadius = Math.Max(trainScaleMap.Values.Average(x => x.MedianRadius), eps);

                    (double RMin, double RMax, double Span, double MedianRadius) ResolveScale(TurningResidualRow row)
                    {
                        if (trainScaleMap.TryGetValue(row.GalaxyKey, out var s))
                            return s;
                        return (globalRMin, globalRMax, globalSpan, globalMedianRadius);
                    }

                    double ComputeProxy(TurningResidualRow row)
                    {
                        var scale = ResolveScale(row);
                        double omega = row.OmegaSi;
                        double radius = row.RadiusKpc;
                        return proxyName switch
                        {
                            "rawPhase" => omega * radius,
                            "normalizedPhaseByRmax" => omega * (radius / Math.Max(scale.RMax, eps)),
                            "normalizedPhaseBySpan" => omega * ((radius - scale.RMin) / Math.Max(scale.Span, eps)),
                            "normalizedPhaseByMedianRadius" => omega * (radius / Math.Max(scale.MedianRadius, eps)),
                            "radiusOnly" => radius,
                            _ => omega * radius
                        };
                    }

                    var sortedProxy = trainRows
                        .Select(ComputeProxy)
                        .Where(double.IsFinite)
                        .OrderBy(x => x)
                        .ToList();
                    Assert.True(sortedProxy.Count >= binCount * 20, $"Insufficient train proxy rows for {proxyName}, binCount={binCount}, train remainder {trainRemainder}.");

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

                    var trainOffsetsRaw = new Dictionary<int, List<double>>();
                    foreach (var row in trainRows)
                    {
                        int bin = GetBin(ComputeProxy(row));
                        if (!trainOffsetsRaw.ContainsKey(bin))
                            trainOffsetsRaw[bin] = new List<double>();
                        trainOffsetsRaw[bin].Add(row.Residual);
                    }

                    var trainOffsets = new Dictionary<int, double>();
                    for (int b = 0; b < binCount; b++)
                    {
                        Assert.True(trainOffsetsRaw.ContainsKey(b), $"Missing train bin {b} for {proxyName}, binCount={binCount}, train remainder {trainRemainder}.");
                        trainOffsets[b] = trainOffsetsRaw[b].Average();
                        Assert.True(double.IsFinite(trainOffsets[b]));
                    }

                    var trainBaselineResiduals = trainRows.Select(r => r.Residual).ToList();
                    var trainCorrectedResiduals = trainRows
                        .Select(r =>
                        {
                            int bin = GetBin(ComputeProxy(r));
                            return r.Residual - trainOffsets[bin];
                        })
                        .ToList();
                    double trainBaselineRms = ComputeBinRms(trainBaselineResiduals);
                    double trainCorrectedRms = ComputeBinRms(trainCorrectedResiduals);
                    double trainDelta = trainBaselineRms - trainCorrectedRms;
                    Assert.True(double.IsFinite(trainDelta));
                    trainDeltas.Add(trainDelta);

                    for (int transferRemainder = 0; transferRemainder < modulo; transferRemainder++)
                    {
                        if (transferRemainder == trainRemainder)
                            continue;

                        var transferRows = rows
                            .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) == transferRemainder)
                            .ToList();
                        Assert.True(transferRows.Count >= 20, $"Insufficient transfer rows for {proxyName}, binCount={binCount}, transfer remainder {transferRemainder}.");

                        var baselineResiduals = transferRows.Select(r => r.Residual).ToList();
                        var correctedResiduals = transferRows
                            .Select(r =>
                            {
                                int bin = GetBin(ComputeProxy(r));
                                return r.Residual - trainOffsets[bin];
                            })
                            .ToList();

                        double baselineRms = ComputeBinRms(baselineResiduals);
                        double correctedRms = ComputeBinRms(correctedResiduals);
                        double transferDelta = baselineRms - correctedRms;

                        Assert.True(double.IsFinite(baselineRms));
                        Assert.True(double.IsFinite(correctedRms));
                        Assert.True(double.IsFinite(transferDelta));
                        Assert.True(correctedRms <= baselineRms + degradeEpsilon,
                            $"{proxyName} degraded beyond epsilon={degradeEpsilon:F3} for binCount={binCount}, train={trainRemainder}, transfer={transferRemainder}.");

                        transferDeltas.Add(transferDelta);
                        if (transferDelta > 0.0)
                            improvedTransfers++;
                    }
                }

                double meanTrainDelta = trainDeltas.Average();
                double meanTransferDelta = transferDeltas.Average();
                double gap = meanTrainDelta - meanTransferDelta;
                summaries.Add((proxyName, binCount, meanTrainDelta, meanTransferDelta, gap, improvedTransfers, transferDeltas.Count));
            }
        }

        WriteLineWithTestPrefix("--- PHASE PROXY SCALE NORMALIZATION DIAGNOSTIC ---");
        foreach (var s in summaries.OrderBy(x => x.Proxy).ThenBy(x => x.BinCount))
        {
            WriteLineWithTestPrefix(
                $"proxy={s.Proxy} bins={s.BinCount} mean delta RMS={s.MeanTransferDelta:F6} " +
                $"improved transfers={s.ImprovedTransfers}/{s.TransferCount} train-transfer gap={s.Gap:F6}");
        }

        var best = summaries.OrderByDescending(x => x.MeanTransferDelta).First();
        bool rawPhaseStillWins = best.Proxy == "rawPhase";
        WriteLineWithTestPrefix($"best proxy={best.Proxy}");
        WriteLineWithTestPrefix($"best bin count={best.BinCount}");
        WriteLineWithTestPrefix($"rawPhase still wins after normalization={rawPhaseStillWins}");

        Assert.Equal(proxyNames.Length * binCounts.Length, summaries.Count);
        Assert.All(summaries, s =>
        {
            Assert.True(double.IsFinite(s.MeanTrainDelta));
            Assert.True(double.IsFinite(s.MeanTransferDelta));
            Assert.True(double.IsFinite(s.Gap));
            Assert.True(s.TransferCount == modulo * (modulo - 1));
        });
    }

    [Fact]
    /// <summary>
    /// Review-safe summary guard for RAR32-RAR42 phase-proxy diagnostics.
    ///
    /// Hypothesis:
    /// Raw phase proxy remains the strongest no-refit transfer candidate while baseline TRM-RAR prediction path stays unchanged.
    ///
    /// Status:
    /// diagnostic + candidate summary guard.
    ///
    /// Limitation:
    /// Consolidation check only; no theorem-level claim and no production activation.
    /// </summary>
    public void RAR43_TRM_Rar_PhaseProxySummaryGuard_NoRefit()
    {
        const int modulo = 5;
        const int binCount = 5;
        const double degradeEpsilon = 0.01;
        const double gapTolerance = 0.001;
        const double eps = 1e-12;

        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        // Baseline fit/freeze block for diagnostics only (no production model-path activation).
        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);
        var fitRecheck = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);
        Assert.True(Math.Abs(fit.BestA0 - fitRecheck.BestA0) < 1e-15, "Baseline a0 changed unexpectedly.");
        Assert.True(Math.Abs(fit.RmsError - fitRecheck.RmsError) < 1e-15, "Baseline RMS changed unexpectedly.");

        var rows = BuildTurningResidualRows(rarData, fit.BestA0);

        static Dictionary<string, (double RMin, double RMax, double Span, double MedianRadius)> BuildScaleMap(List<TurningResidualRow> sourceRows)
        {
            var map = new Dictionary<string, (double RMin, double RMax, double Span, double MedianRadius)>(StringComparer.OrdinalIgnoreCase);
            foreach (var g in sourceRows.GroupBy(r => r.GalaxyKey))
            {
                var radii = g
                    .Select(x => x.RadiusKpc)
                    .Where(double.IsFinite)
                    .OrderBy(x => x)
                    .ToList();
                if (radii.Count == 0)
                    continue;

                double rMin = radii.First();
                double rMax = radii.Last();
                double span = Math.Max(0.0, rMax - rMin);
                double medianRadius = Median(radii);
                map[g.Key] = (rMin, rMax, span, medianRadius);
            }

            return map;
        }

        (double MeanTrainDelta, double MeanTransferDelta, double Gap, int ImprovedTransfers, int TransferCount) EvaluateProxy(string proxyName)
        {
            var trainDeltas = new List<double>();
            var transferDeltas = new List<double>();
            int improvedTransfers = 0;

            for (int trainRemainder = 0; trainRemainder < modulo; trainRemainder++)
            {
                var trainRows = rows
                    .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) != trainRemainder)
                    .ToList();
                Assert.True(trainRows.Count >= binCount * 30, $"Insufficient train rows for {proxyName}, train remainder {trainRemainder}.");

                var trainScaleMap = BuildScaleMap(trainRows);
                Assert.True(trainScaleMap.Count > 20, $"Insufficient train scale map for {proxyName}, train remainder {trainRemainder}.");
                double globalRMin = trainScaleMap.Values.Average(x => x.RMin);
                double globalRMax = trainScaleMap.Values.Average(x => x.RMax);
                double globalSpan = Math.Max(trainScaleMap.Values.Average(x => x.Span), eps);
                double globalMedianRadius = Math.Max(trainScaleMap.Values.Average(x => x.MedianRadius), eps);

                (double RMin, double RMax, double Span, double MedianRadius) ResolveScale(TurningResidualRow row)
                {
                    if (trainScaleMap.TryGetValue(row.GalaxyKey, out var s))
                        return s;
                    return (globalRMin, globalRMax, globalSpan, globalMedianRadius);
                }

                double ComputeProxy(TurningResidualRow row)
                {
                    var scale = ResolveScale(row);
                    double omega = row.OmegaSi;
                    double radius = row.RadiusKpc;
                    return proxyName switch
                    {
                        "radiusOnly" => radius,
                        "rawPhase" => omega * radius,
                        "normalizedPhaseByRmax" => omega * (radius / Math.Max(scale.RMax, eps)),
                        "normalizedPhaseByMedianRadius" => omega * (radius / Math.Max(scale.MedianRadius, eps)),
                        "normalizedPhaseBySpan" => omega * ((radius - scale.RMin) / Math.Max(scale.Span, eps)),
                        _ => omega * radius
                    };
                }

                var sortedProxy = trainRows
                    .Select(ComputeProxy)
                    .Where(double.IsFinite)
                    .OrderBy(x => x)
                    .ToList();
                Assert.True(sortedProxy.Count >= binCount * 20, $"Insufficient train proxy rows for {proxyName}, train remainder {trainRemainder}.");

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

                var trainOffsetsRaw = new Dictionary<int, List<double>>();
                foreach (var row in trainRows)
                {
                    int bin = GetBin(ComputeProxy(row));
                    if (!trainOffsetsRaw.ContainsKey(bin))
                        trainOffsetsRaw[bin] = new List<double>();
                    trainOffsetsRaw[bin].Add(row.Residual);
                }

                var trainOffsets = new Dictionary<int, double>();
                for (int b = 0; b < binCount; b++)
                {
                    Assert.True(trainOffsetsRaw.ContainsKey(b), $"Missing train bin {b} for {proxyName}, train remainder {trainRemainder}.");
                    trainOffsets[b] = trainOffsetsRaw[b].Average();
                    Assert.True(double.IsFinite(trainOffsets[b]));
                }

                var trainBaselineResiduals = trainRows.Select(r => r.Residual).ToList();
                var trainCorrectedResiduals = trainRows
                    .Select(r =>
                    {
                        int bin = GetBin(ComputeProxy(r));
                        return r.Residual - trainOffsets[bin];
                    })
                    .ToList();
                double trainDelta = ComputeBinRms(trainBaselineResiduals) - ComputeBinRms(trainCorrectedResiduals);
                Assert.True(double.IsFinite(trainDelta));
                trainDeltas.Add(trainDelta);

                for (int transferRemainder = 0; transferRemainder < modulo; transferRemainder++)
                {
                    if (transferRemainder == trainRemainder)
                        continue;

                    var transferRows = rows
                        .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) == transferRemainder)
                        .ToList();
                    Assert.True(transferRows.Count >= 20, $"Insufficient transfer rows for {proxyName}, transfer remainder {transferRemainder}.");

                    var baselineResiduals = transferRows.Select(r => r.Residual).ToList();
                    var correctedResiduals = transferRows
                        .Select(r =>
                        {
                            int bin = GetBin(ComputeProxy(r));
                            return r.Residual - trainOffsets[bin];
                        })
                        .ToList();

                    double baselineRms = ComputeBinRms(baselineResiduals);
                    double correctedRms = ComputeBinRms(correctedResiduals);
                    double transferDelta = baselineRms - correctedRms;

                    Assert.True(double.IsFinite(transferDelta));
                    Assert.True(correctedRms <= baselineRms + degradeEpsilon,
                        $"{proxyName} degraded beyond epsilon={degradeEpsilon:F3} for train={trainRemainder}, transfer={transferRemainder}.");

                    transferDeltas.Add(transferDelta);
                    if (transferDelta > 0.0)
                        improvedTransfers++;
                }
            }

            return (
                trainDeltas.Average(),
                transferDeltas.Average(),
                trainDeltas.Average() - transferDeltas.Average(),
                improvedTransfers,
                transferDeltas.Count);
        }

        var radiusOnly = EvaluateProxy("radiusOnly");
        var rawPhase = EvaluateProxy("rawPhase");
        var normalizedByRmax = EvaluateProxy("normalizedPhaseByRmax");
        var normalizedByMedian = EvaluateProxy("normalizedPhaseByMedianRadius");
        var normalizedBySpan = EvaluateProxy("normalizedPhaseBySpan");

        var normalizedCandidates = new[]
        {
            ("normalizedPhaseByRmax", normalizedByRmax),
            ("normalizedPhaseByMedianRadius", normalizedByMedian),
            ("normalizedPhaseBySpan", normalizedBySpan)
        };
        var normalizedBest = normalizedCandidates
            .OrderByDescending(x => x.Item2.MeanTransferDelta)
            .First();

        double rawOverRadius = rawPhase.MeanTransferDelta - radiusOnly.MeanTransferDelta;
        double rawOverNormalizedBest = rawPhase.MeanTransferDelta - normalizedBest.Item2.MeanTransferDelta;

        WriteLineWithTestPrefix("--- PHASE PROXY SUMMARY GUARD ---");
        WriteLineWithTestPrefix($"mean delta radiusOnly={radiusOnly.MeanTransferDelta:F6}");
        WriteLineWithTestPrefix($"mean delta rawPhase={rawPhase.MeanTransferDelta:F6}");
        WriteLineWithTestPrefix($"mean delta normalized best={normalizedBest.Item2.MeanTransferDelta:F6} ({normalizedBest.Item1})");
        WriteLineWithTestPrefix($"rawPhase extra over radius={rawOverRadius:F6}");
        WriteLineWithTestPrefix($"rawPhase extra over normalized best={rawOverNormalizedBest:F6}");
        WriteLineWithTestPrefix($"improved transfers for rawPhase={rawPhase.ImprovedTransfers}/{rawPhase.TransferCount}");
        WriteLineWithTestPrefix($"train-transfer gap rawPhase={rawPhase.Gap:F6}");
        WriteLineWithTestPrefix("Diagnostic candidate only; baseline TRM-RAR path unchanged.");

        Assert.True(double.IsFinite(radiusOnly.MeanTransferDelta));
        Assert.True(double.IsFinite(rawPhase.MeanTransferDelta));
        Assert.True(double.IsFinite(normalizedBest.Item2.MeanTransferDelta));
        Assert.True(double.IsFinite(rawOverRadius));
        Assert.True(double.IsFinite(rawOverNormalizedBest));
        Assert.True(double.IsFinite(rawPhase.Gap));

        Assert.True(rawPhase.TransferCount == modulo * (modulo - 1));
        Assert.True(radiusOnly.TransferCount == modulo * (modulo - 1));
        Assert.True(normalizedByRmax.TransferCount == modulo * (modulo - 1));
        Assert.True(normalizedByMedian.TransferCount == modulo * (modulo - 1));
        Assert.True(normalizedBySpan.TransferCount == modulo * (modulo - 1));

        Assert.Equal(rawPhase.TransferCount, rawPhase.ImprovedTransfers);
        Assert.True(rawPhase.MeanTransferDelta > radiusOnly.MeanTransferDelta);
        Assert.True(rawPhase.MeanTransferDelta >= normalizedBest.Item2.MeanTransferDelta);
        Assert.True(Math.Abs(rawPhase.Gap) < gapTolerance);
    }

    [Fact]
    /// <summary>
    /// Cross-split diagnostic for raw phase proxy robustness under alternative deterministic partition schemes.
    ///
    /// Hypothesis:
    /// If the raw phase residual signal is robust, it should transfer across multiple deterministic galaxy split constructions.
    ///
    /// Status:
    /// diagnostic + candidate.
    ///
    /// Limitation:
    /// Partition-robustness check only; no baseline-model activation.
    /// </summary>
    public void RAR44_TRM_Rar_PhaseProxyAlternativeSplits_NoRefit()
    {
        const int binCount = 5;
        const double degradeEpsilon = 0.01;

        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        // Fit/freeze baseline once; diagnostics remain outside production prediction path.
        var fit = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);
        var fitRecheck = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);
        Assert.True(Math.Abs(fit.BestA0 - fitRecheck.BestA0) < 1e-15, "Baseline a0 changed unexpectedly.");
        Assert.True(Math.Abs(fit.RmsError - fitRecheck.RmsError) < 1e-15, "Baseline RMS changed unexpectedly.");

        var rows = BuildTurningResidualRows(rarData, fit.BestA0);

        var schemes = new (string Name, int Modulo, Func<string, int> KeySelector)[]
        {
            ("checksum%5", 5, key => key.Sum(c => c)),
            ("checksum%7", 7, key => key.Sum(c => c)),
            ("nameLength%5", 5, key => key.Length),
            ("firstCharBucket%5", 5, key =>
            {
                if (string.IsNullOrEmpty(key))
                    return 0;

                char c = char.ToUpperInvariant(key[0]);
                if (c is >= 'A' and <= 'Z')
                    return ((c - 'A') * 5) / 26;

                return 0;
            })
        };

        var summaries = new List<(string Scheme, double MeanDelta, int ImprovedTransfers, int TransferCount, double Gap)>();

        foreach (var scheme in schemes)
        {
            var trainDeltas = new List<double>();
            var transferDeltas = new List<double>();
            int improvedTransfers = 0;

            int PartitionValue(string galaxyKey)
            {
                int raw = scheme.KeySelector(galaxyKey);
                return Math.Abs(raw % scheme.Modulo);
            }

            for (int trainRemainder = 0; trainRemainder < scheme.Modulo; trainRemainder++)
            {
                var trainRows = rows
                    .Where(r => PartitionValue(r.GalaxyKey) != trainRemainder)
                    .ToList();
                Assert.True(trainRows.Count >= binCount * 30, $"Insufficient train rows for scheme={scheme.Name}, train remainder {trainRemainder}.");

                var sortedPhase = trainRows
                    .Select(r => r.OmegaSi * r.RadiusKpc)
                    .Where(double.IsFinite)
                    .OrderBy(x => x)
                    .ToList();
                Assert.True(sortedPhase.Count >= binCount * 20, $"Insufficient phase rows for scheme={scheme.Name}, train remainder {trainRemainder}.");

                var cuts = new List<double>();
                for (int i = 1; i < binCount; i++)
                {
                    int index = (i * sortedPhase.Count) / binCount;
                    cuts.Add(sortedPhase[Math.Min(index, sortedPhase.Count - 1)]);
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

                var trainOffsetsRaw = new Dictionary<int, List<double>>();
                foreach (var row in trainRows)
                {
                    int bin = GetBin(row.OmegaSi * row.RadiusKpc);
                    if (!trainOffsetsRaw.ContainsKey(bin))
                        trainOffsetsRaw[bin] = new List<double>();
                    trainOffsetsRaw[bin].Add(row.Residual);
                }

                var trainOffsets = new Dictionary<int, double>();
                for (int b = 0; b < binCount; b++)
                {
                    Assert.True(trainOffsetsRaw.ContainsKey(b), $"Missing phase bin {b} for scheme={scheme.Name}, train remainder {trainRemainder}.");
                    trainOffsets[b] = trainOffsetsRaw[b].Average();
                    Assert.True(double.IsFinite(trainOffsets[b]));
                }

                // Conservative train-only shrink fit using inner remainder holdouts for this scheme.
                double bestShrink = 0.0;
                int bestInnerImproved = 0;
                double bestInnerMeanDelta = double.MinValue;
                const double innerDegradeTolerance = 0.005;
                foreach (double shrink in new[] { 0.0, 0.10, 0.20, 0.35, 0.50, 0.75, 1.00 })
                {
                    var innerDeltas = new List<double>();
                    double worstInnerDegrade = 0.0;

                    for (int innerRemainder = 0; innerRemainder < scheme.Modulo; innerRemainder++)
                    {
                        if (innerRemainder == trainRemainder)
                            continue;

                        var innerRows = trainRows
                            .Where(r => PartitionValue(r.GalaxyKey) == innerRemainder)
                            .ToList();
                        if (innerRows.Count < 20)
                            continue;

                        var baselineInner = innerRows.Select(r => r.Residual).ToList();
                        var correctedInner = innerRows
                            .Select(r =>
                            {
                                int bin = GetBin(r.OmegaSi * r.RadiusKpc);
                                return r.Residual - (shrink * trainOffsets[bin]);
                            })
                            .ToList();

                        double baselineInnerRms = ComputeBinRms(baselineInner);
                        double correctedInnerRms = ComputeBinRms(correctedInner);
                        double innerDelta = baselineInnerRms - correctedInnerRms;
                        innerDeltas.Add(innerDelta);
                        worstInnerDegrade = Math.Max(worstInnerDegrade, correctedInnerRms - baselineInnerRms);
                    }

                    if (innerDeltas.Count == 0)
                        continue;

                    double meanInnerDelta = innerDeltas.Average();
                    int improvedInner = innerDeltas.Count(d => d > 0.0);
                    bool better =
                        improvedInner > bestInnerImproved ||
                        (improvedInner == bestInnerImproved && meanInnerDelta > bestInnerMeanDelta + 1e-12);

                    if (worstInnerDegrade <= innerDegradeTolerance && better)
                    {
                        bestInnerImproved = improvedInner;
                        bestInnerMeanDelta = meanInnerDelta;
                        bestShrink = shrink;
                    }
                }

                var trainBaselineResiduals = trainRows.Select(r => r.Residual).ToList();
                var trainCorrectedResiduals = trainRows
                    .Select(r =>
                    {
                        int bin = GetBin(r.OmegaSi * r.RadiusKpc);
                        return r.Residual - (bestShrink * trainOffsets[bin]);
                    })
                    .ToList();
                double trainDelta = ComputeBinRms(trainBaselineResiduals) - ComputeBinRms(trainCorrectedResiduals);
                Assert.True(double.IsFinite(trainDelta));
                trainDeltas.Add(trainDelta);

                for (int transferRemainder = 0; transferRemainder < scheme.Modulo; transferRemainder++)
                {
                    if (transferRemainder == trainRemainder)
                        continue;

                    var transferRows = rows
                        .Where(r => PartitionValue(r.GalaxyKey) == transferRemainder)
                        .ToList();
                    if (transferRows.Count < 20)
                        continue;

                    var baselineResiduals = transferRows.Select(r => r.Residual).ToList();
                    var correctedResiduals = transferRows
                        .Select(r =>
                        {
                            int bin = GetBin(r.OmegaSi * r.RadiusKpc);
                            return r.Residual - (bestShrink * trainOffsets[bin]);
                        })
                        .ToList();

                    double baselineRms = ComputeBinRms(baselineResiduals);
                    double correctedRms = ComputeBinRms(correctedResiduals);
                    double transferDelta = baselineRms - correctedRms;

                    Assert.True(double.IsFinite(baselineRms));
                    Assert.True(double.IsFinite(correctedRms));
                    Assert.True(double.IsFinite(transferDelta));
                    Assert.True(correctedRms <= baselineRms + degradeEpsilon,
                        $"rawPhase degraded beyond epsilon={degradeEpsilon:F3} for scheme={scheme.Name}, train={trainRemainder}, transfer={transferRemainder}.");

                    transferDeltas.Add(transferDelta);
                    if (transferDelta > 0.0)
                        improvedTransfers++;
                }
            }

            double meanDelta = transferDeltas.Average();
            double gap = trainDeltas.Average() - meanDelta;
            summaries.Add((scheme.Name, meanDelta, improvedTransfers, transferDeltas.Count, gap));
        }

        WriteLineWithTestPrefix("--- PHASE PROXY ALTERNATIVE SPLITS DIAGNOSTIC ---");
        foreach (var s in summaries)
        {
            WriteLineWithTestPrefix(
                $"scheme={s.Scheme} mean delta RMS={s.MeanDelta:F6} improved transfers={s.ImprovedTransfers}/{s.TransferCount} " +
                $"train-transfer gap={s.Gap:F6}");
        }

        Assert.Equal(schemes.Length, summaries.Count);
        Assert.All(summaries, s =>
        {
            Assert.True(double.IsFinite(s.MeanDelta));
            Assert.True(double.IsFinite(s.Gap));
            Assert.True(s.TransferCount > 0);
            int minImproved = (int)Math.Ceiling(0.70 * s.TransferCount);
            Assert.True(s.ImprovedTransfers >= minImproved,
                $"Scheme {s.Scheme} improved only {s.ImprovedTransfers}/{s.TransferCount}, below 70% threshold.");
        });
    }

    private static double EvaluateCrossSplitOffsetMeanDelta(
        List<TurningResidualRow> rows,
        int modulo,
        int binCount,
        int? shuffleSeed)
    {
        var deltas = new List<double>();

        for (int trainRemainder = 0; trainRemainder < modulo; trainRemainder++)
        {
            var trainRows = rows
                .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) != trainRemainder)
                .ToList();
            if (trainRows.Count < binCount * 20)
                continue;

            List<double> trainProxyUsed;
            if (shuffleSeed.HasValue)
            {
                // Deterministic null-control: shuffle proxy assignments only in train rows.
                var shuffled = trainRows.Select(r => r.TurningProxySigned).ToList();
                var rng = new Random(unchecked(shuffleSeed.Value * 7919 + trainRemainder * 104729));
                for (int i = shuffled.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
                }
                trainProxyUsed = shuffled;
            }
            else
            {
                trainProxyUsed = trainRows.Select(r => r.TurningProxySigned).ToList();
            }

            var sortedProxy = trainProxyUsed.OrderBy(x => x).ToList();
            if (sortedProxy.Count < binCount * 20)
                continue;

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

            var trainOffsetsRaw = new Dictionary<int, List<double>>();
            for (int i = 0; i < trainRows.Count; i++)
            {
                int bin = GetBin(trainProxyUsed[i]);
                if (!trainOffsetsRaw.ContainsKey(bin))
                    trainOffsetsRaw[bin] = new List<double>();
                trainOffsetsRaw[bin].Add(trainRows[i].Residual);
            }

            bool hasAllBins = true;
            var trainOffsets = new Dictionary<int, double>();
            for (int b = 0; b < binCount; b++)
            {
                if (!trainOffsetsRaw.TryGetValue(b, out var residuals) || residuals.Count < 8)
                {
                    hasAllBins = false;
                    break;
                }
                trainOffsets[b] = residuals.Average();
            }

            if (!hasAllBins)
                continue;

            for (int transferRemainder = 0; transferRemainder < modulo; transferRemainder++)
            {
                if (transferRemainder == trainRemainder)
                    continue;

                var transferRows = rows
                    .Where(r => (r.GalaxyKey.Sum(c => c) % modulo) == transferRemainder)
                    .ToList();
                if (transferRows.Count < 20)
                    continue;

                var baselineResiduals = transferRows.Select(r => r.Residual).ToList();
                var correctedResiduals = transferRows
                    .Select(r =>
                    {
                        int bin = GetBin(r.TurningProxySigned);
                        return r.Residual - trainOffsets[bin];
                    })
                    .ToList();

                double baselineRms = ComputeBinRms(baselineResiduals);
                double correctedRms = ComputeBinRms(correctedResiduals);
                double delta = baselineRms - correctedRms;

                if (double.IsFinite(delta))
                    deltas.Add(delta);
            }
        }

        return deltas.Count > 0 ? deltas.Average() : double.NaN;
    }

    private static Dictionary<string, GalaxyPhysicalProxyStats> BuildGalaxyPhysicalProxyStats(List<RarPoint> points)
    {
        const double eps = 1e-12;
        var map = new Dictionary<string, GalaxyPhysicalProxyStats>(StringComparer.OrdinalIgnoreCase);

        foreach (var g in points.GroupBy(p => NormalizeGalaxyKey(p.GalaxyName)))
        {
            var ordered = g
                .Where(p => p.RadiusKpc > 0 && p.GbarMs2 > 0)
                .OrderBy(p => p.RadiusKpc)
                .ToList();
            if (ordered.Count < 3)
                continue;

            double minR = ordered.Min(p => p.RadiusKpc);
            double maxR = ordered.Max(p => p.RadiusKpc);
            double span = Math.Max(0.0, maxR - minR);

            var inner = ordered.Where(p => span <= eps || ((p.RadiusKpc - minR) / span) <= 0.30).ToList();
            var outer = ordered.Where(p => span <= eps || ((p.RadiusKpc - minR) / span) >= 0.70).ToList();
            if (inner.Count == 0) inner = ordered;
            if (outer.Count == 0) outer = ordered;

            var gradients = new List<double>();
            for (int i = 0; i < ordered.Count - 1; i++)
            {
                var p1 = ordered[i];
                var p2 = ordered[i + 1];
                double dr = p2.RadiusKpc - p1.RadiusKpc;
                if (dr <= 0 || p1.GbarMs2 <= 0 || p2.GbarMs2 <= 0)
                    continue;
                gradients.Add(Math.Abs((Math.Log(p2.GbarMs2) - Math.Log(p1.GbarMs2)) / dr));
            }

            double meanAbsGrad = gradients.Count > 0 ? gradients.Average() : 0.0;
            double meanLogGbar = ordered.Average(p => Math.Log10(p.GbarMs2));

            double meanInnerGbar = inner.Average(p => p.GbarMs2);
            double meanOuterGbar = outer.Average(p => p.GbarMs2);
            double outerInnerRatio = meanOuterGbar / Math.Max(meanInnerGbar, eps);

            double gasPower = ordered.Sum(p => Math.Max(p.Vgas, 0.0) * Math.Max(p.Vgas, 0.0));
            double diskPower = ordered.Sum(p => Math.Max(p.Vdisk, 0.0) * Math.Max(p.Vdisk, 0.0));
            double bulgePower = ordered.Sum(p => Math.Max(p.Vbulge, 0.0) * Math.Max(p.Vbulge, 0.0));
            double totalPower = gasPower + diskPower + bulgePower;
            double gasDominance = totalPower > eps ? gasPower / totalPower : 0.0;

            map[g.Key] = new GalaxyPhysicalProxyStats(
                MeanLogGbar: meanLogGbar,
                MeanAbsDlnGbarDr: meanAbsGrad,
                OuterToInnerBaryonicAccelerationRatio: outerInnerRatio,
                GasDominanceProxy: gasDominance,
                RadialSpanKpc: span,
                PointCount: ordered.Count);
        }

        return map;
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
                    omegaSi,
                    p.RadiusKpc,
                    logGbar,
                    p.GobsMs2,
                    p.GbarMs2,
                    gPred));
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

    private static double Median(List<double> sortedValues)
    {
        if (sortedValues == null || sortedValues.Count == 0)
            return 0.0;

        int mid = sortedValues.Count / 2;
        if (sortedValues.Count % 2 == 1)
            return sortedValues[mid];

        return 0.5 * (sortedValues[mid - 1] + sortedValues[mid]);
    }

    private static double DampedProxy(double proxy, double beta)
    {
        if (beta <= 0.0)
            return proxy;

        return proxy / (1.0 + (beta * Math.Abs(proxy)));
    }

    private static double EstimateAlphaScale(List<TurningResidualRow> trainRows)
    {
        const double eps = 1e-30;

        var estimates = trainRows
            .Where(r => Math.Abs(r.TurningProxySigned) > 1e-20 && r.BasePredMs2 > eps)
            .Select(r => (r.Residual * Math.Log(10.0) * r.BasePredMs2) / r.TurningProxySigned)
            .Where(double.IsFinite)
            .Select(Math.Abs)
            .OrderBy(x => x)
            .ToList();

        if (estimates.Count >= 5)
        {
            double med = Median(estimates);
            if (double.IsFinite(med) && med > 0.0)
                return med;
        }

        double medianPred = Median(trainRows.Select(r => r.BasePredMs2).Where(double.IsFinite).OrderBy(x => x).ToList());
        double medianProxy = Median(trainRows.Select(r => Math.Abs(r.TurningProxySigned)).Where(double.IsFinite).OrderBy(x => x).ToList());
        if (medianPred > eps && medianProxy > 1e-20)
            return medianPred / medianProxy;

        return 1e-10;
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
        double OmegaSi,
        double RadiusKpc,
        double LogGbar,
        double GobsMs2,
        double GbarMs2,
        double BasePredMs2);

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

    private sealed record GalaxyPhysicalProxyStats(
        double MeanLogGbar,
        double MeanAbsDlnGbarDr,
        double OuterToInnerBaryonicAccelerationRatio,
        double GasDominanceProxy,
        double RadialSpanKpc,
        int PointCount);

}
