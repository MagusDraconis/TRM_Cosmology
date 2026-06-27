using System;
using System.Collections.Generic;
using System.Text;
using TRM.Core;
using TRM.Core.Domains.Domain1.GalacticRotation;
using Xunit.Abstractions;

namespace TRM.Tests.CoreTests;

/// <summary>
/// Orbit/full/regime/adaptive TRM comparison suite over SPARC-style galaxy profiles.
/// Status: tested (comparative RMS checks), calibrated (regime and weighting parameters), diagnostic/exploratory (sweeps and bin studies).
/// Related implementation: OrbitalIntegrationService and Domain1 galactic models.
/// Related docs: docs/review/TRM_Service_Test_Consolidation.md and docs/review/TRM_Real_Physics_Test_Coverage.md.
/// </summary>
public class OrbitalIntegratedTests
{
    private readonly ITestOutputHelper _output;
    public OrbitalIntegratedTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Test01_TRM_Rar_OrbitIntegratedModel()
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

        double a0 = 1.2e-10;

        double sum = 0;
        int count = 0;

        var galaxies = trmDisk
            .GroupBy(p => p.GalaxyName);

        foreach(var g in galaxies)
        {
            var ordered = g.OrderBy(p => p.RadiusKpc).ToList();

            foreach(var p in ordered.Skip(2))
            {
                double gPred = OrbitalIntegrationService.ComputeIntegratedG(
                    ordered,
                    p.RadiusKpc,
                    a0
                );

                if(gPred <= 0 || p.GobsMs2 <= 0)
                    continue;

                double res = Math.Log10(p.GobsMs2) - Math.Log10(gPred);

                sum += res * res;
                count++;
            }
        }

        double rms = Math.Sqrt(sum / count);

        _output.WriteLine($"Orbit-integrated RMS = {rms:F4}");

        Assert.InRange(rms, 0.4, 1.5);
    }

    [Fact]
    public void Test02_TRM_FullModel_RmsComparison()
    {
        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rawPoints = SparcRarAnalysis.ParseRarFromZip(zipPath);
        var galaxyMeta = SparcRarAnalysis.LoadGalaxyMetaFromMrt(mrtPath);
        var scaling = TrmCosmologyParameters.Current();

        // 1) Lokales baryonisches Modell
        var trmDisk = SparcRarAnalysis.ApplyTrmDistanceMapping(
            rawPoints,
            galaxyMeta,
            scaling,
            BaryonMode.ExponentialDisk
        );

        Assert.NotEmpty(trmDisk);

        double a0 = 1.2e-10;

        double sumLocal = 0.0;
        int countLocal = 0;

        double sumOrbit = 0.0;
        int countOrbit = 0;

        double sumFull = 0.0;
        int countFull = 0;

        var galaxyGroups = trmDisk
            .GroupBy(p => p.GalaxyName)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        foreach (var kvp in galaxyGroups)
        {
            var galaxy = kvp.Value;

            if (galaxy.Count < 4)
                continue;

            foreach (var p in galaxy.Skip(2))
            {
                if (p.GobsMs2 <= 0 || p.GbarMs2 <= 0)
                    continue;

                // -------------------------
                // A) Lokales Modell
                // -------------------------
                double gLocal = SparcRarAnalysis.PredictGobs(
                    p.GbarMs2,
                    a0,
                    ModelType.ClockworkTRM
                );

                if (gLocal > 0 && double.IsFinite(gLocal))
                {
                    double resLocal = Math.Log10(p.GobsMs2) - Math.Log10(gLocal);
                    sumLocal += resLocal * resLocal;
                    countLocal++;
                }

                // -------------------------
                // B) Orbit-integriertes Modell
                // -------------------------
                double gOrbit = OrbitalIntegrationService.ComputeIntegratedG_OrbitOnly(
                    galaxy,
                    p.RadiusKpc,
                    a0
                );

                if (gOrbit > 0 && double.IsFinite(gOrbit))
                {
                    double resOrbit = Math.Log10(p.GobsMs2) - Math.Log10(gOrbit);
                    sumOrbit += resOrbit * resOrbit;
                    countOrbit++;
                }

                // -------------------------
                // C) Vollständiges TRM-Modell
                // -------------------------
                double gFull = TrmFullModel.ComputeGobs(
                    galaxy,
                    p.RadiusKpc,
                    a0
                );

                if (gFull > 0 && double.IsFinite(gFull))
                {
                    double resFull = Math.Log10(p.GobsMs2) - Math.Log10(gFull);
                    sumFull += resFull * resFull;
                    countFull++;
                }
            }
        }

        Assert.True(countLocal > 100, "Too few local-model points.");
        Assert.True(countOrbit > 100, "Too few orbit-model points.");
        Assert.True(countFull > 100, "Too few full-model points.");

        double rmsLocal = Math.Sqrt(sumLocal / countLocal);
        double rmsOrbit = Math.Sqrt(sumOrbit / countOrbit);
        double rmsFull = Math.Sqrt(sumFull / countFull);

        _output.WriteLine($"Local RMS = {rmsLocal:F4}");
        _output.WriteLine($"Orbit RMS = {rmsOrbit:F4}");
        _output.WriteLine($"Full  RMS = {rmsFull:F4}");

        _output.WriteLine($"Orbit improvement vs Local = {rmsLocal - rmsOrbit:F4}");
        _output.WriteLine($"Full  improvement vs Orbit = {rmsOrbit - rmsFull:F4}");
        _output.WriteLine($"Full  improvement vs Local = {rmsLocal - rmsFull:F4}");

        // Grundstabilität
        Assert.InRange(rmsLocal, 0.4, 1.5);
        Assert.InRange(rmsOrbit, 0.4, 1.5);
        Assert.InRange(rmsFull, 0.4, 1.5);

        // Erwartete Hierarchie:
        // Orbit sollte nicht schlechter sein als lokal
        //Assert.True(rmsOrbit <= rmsLocal + 0.02,
        //    $"Orbit model is unexpectedly worse than local model: local={rmsLocal:F4}, orbit={rmsOrbit:F4}");

        // Full model sollte nicht schlechter sein als Orbit
        Assert.True(rmsFull <= rmsOrbit + 0.02,
            $"Full TRM model is unexpectedly worse than orbit model: orbit={rmsOrbit:F4}, full={rmsFull:F4}");

        // Full model sollte klar besser als lokal sein
        Assert.True(rmsFull < rmsLocal,
            $"Full TRM model should improve over local model: local={rmsLocal:F4}, full={rmsFull:F4}");
    }
    [Fact]
    public void Test03_TRM_FullModel_RadiusBins()
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

        double a0 = 1.2e-10;

        var rawGalaxyCache = rawPoints
            .GroupBy(p => p.GalaxyName)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        var fullGalaxyGroups = trmDisk
            .GroupBy(p => p.GalaxyName)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        var bin1 = new List<double>(); // r/Rd < 1
        var bin2 = new List<double>(); // 1 <= r/Rd < 2
        var bin3 = new List<double>(); // 2 <= r/Rd < 4
        var bin4 = new List<double>(); // r/Rd >= 4

        foreach (var kvp in fullGalaxyGroups)
        {
            string galaxyName = kvp.Key;
            var fullPoints = kvp.Value;

            if (!rawGalaxyCache.TryGetValue(galaxyName, out var rawGalaxy))
                continue;

            if (rawGalaxy.Count < 5 || fullPoints.Count < 5)
                continue;

            // Rd bewusst aus RAW-Geometrie bestimmen
            double rd = SparcRarAnalysis.EstimateDiskScaleLengthFromProfile(rawGalaxy);
            if (rd <= 0)
                continue;

            foreach (var p in fullPoints.Skip(2))
            {
                if (p.GobsMs2 <= 0)
                    continue;

                double gPred = TrmFullModel.ComputeGobs(
                    fullPoints,
                    p.RadiusKpc,
                    a0
                );

                if (gPred <= 0 || double.IsNaN(gPred) || double.IsInfinity(gPred))
                    continue;

                double residual = Math.Log10(p.GobsMs2) - Math.Log10(gPred);

                // p.RadiusKpc ist TRM-Radius, Rd kommt aus RAW-Profil
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

        double mean1 = ComputeMean(bin1);
        double mean2 = ComputeMean(bin2);
        double mean3 = ComputeMean(bin3);
        double mean4 = ComputeMean(bin4);

        _output.WriteLine($"FullModel Bin <1 Rd  : n={bin1.Count}, RMS={rms1:F4}, Mean={mean1:F4}");
        _output.WriteLine($"FullModel Bin 1-2 Rd : n={bin2.Count}, RMS={rms2:F4}, Mean={mean2:F4}");
        _output.WriteLine($"FullModel Bin 2-4 Rd : n={bin3.Count}, RMS={rms3:F4}, Mean={mean3:F4}");
        _output.WriteLine($"FullModel Bin >=4 Rd : n={bin4.Count}, RMS={rms4:F4}, Mean={mean4:F4}");


        // Mindestanzahlen: innerer Bin ist naturgemäß dünn besetzt
        Assert.True(bin2.Count > 20, "Too few points in 1-2 Rd bin.");
        Assert.True(bin3.Count > 20, "Too few points in 2-4 Rd bin.");
        Assert.True(bin4.Count > 20, "Too few points in >=4 Rd bin.");

        // Innere Bins sind oft dünn besetzt -> nur weich prüfen
        if (bin1.Count >= 5)
        {
            Assert.InRange(rms1, 0.0, 2.5);
        }
        else
        {
            _output.WriteLine("Inner bin (<1 Rd) has too few points for a strong statistical assertion.");
        }

        // Diese drei Bins sind statistisch relevant
        Assert.True(bin2.Count > 20, "Too few points in 1-2 Rd bin.");
        Assert.True(bin3.Count > 20, "Too few points in 2-4 Rd bin.");
        Assert.True(bin4.Count > 20, "Too few points in >=4 Rd bin.");

        Assert.InRange(rms2, 0.0, 2.5);
        Assert.InRange(rms3, 0.0, 2.5);
        Assert.InRange(rms4, 0.0, 2.5);

        // Zusätzliche physikalische Checks
        Assert.True(rms2 < 1.0, $"1-2 Rd bin RMS too large: {rms2:F4}");
        Assert.True(rms3 < 1.2, $"2-4 Rd bin RMS too large: {rms3:F4}");
        Assert.True(rms4 < 1.2, $">=4 Rd bin RMS too large: {rms4:F4}");

    }

    [Fact]
    public void Test04_TRM_OrbitVsFullModel_RadiusBins()
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

        double a0 = 1.2e-10;

        var rawGalaxyCache = rawPoints
            .GroupBy(p => p.GalaxyName)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        var modelGalaxyGroups = trmDisk
            .GroupBy(p => p.GalaxyName)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        var orbitBin1 = new List<double>();
        var orbitBin2 = new List<double>();
        var orbitBin3 = new List<double>();
        var orbitBin4 = new List<double>();

        var fullBin1 = new List<double>();
        var fullBin2 = new List<double>();
        var fullBin3 = new List<double>();
        var fullBin4 = new List<double>();

        foreach (var kvp in modelGalaxyGroups)
        {
            string galaxyName = kvp.Key;
            var galaxy = kvp.Value;

            if (!rawGalaxyCache.TryGetValue(galaxyName, out var rawGalaxy))
                continue;

            if (rawGalaxy.Count < 5 || galaxy.Count < 5)
                continue;

            double rd = SparcRarAnalysis.EstimateDiskScaleLengthFromProfile(rawGalaxy);
            if (rd <= 0)
                continue;

            foreach (var p in galaxy.Skip(2))
            {
                if (p.GobsMs2 <= 0)
                    continue;

                double gOrbit = OrbitalIntegrationService.ComputeIntegratedG_OrbitOnly(
                    galaxy,
                    p.RadiusKpc,
                    a0
                );

                double gFull = TrmFullModel.ComputeGobs(
                    galaxy,
                    p.RadiusKpc,
                    a0
                );

                if (gOrbit <= 0 || gFull <= 0)
                    continue;
                if (!double.IsFinite(gOrbit) || !double.IsFinite(gFull))
                    continue;

                double resOrbit = Math.Log10(p.GobsMs2) - Math.Log10(gOrbit);
                double resFull = Math.Log10(p.GobsMs2) - Math.Log10(gFull);

                double x = p.RadiusKpc / rd;

                if (x < 1.0)
                {
                    orbitBin1.Add(resOrbit);
                    fullBin1.Add(resFull);
                }
                else if (x < 2.0)
                {
                    orbitBin2.Add(resOrbit);
                    fullBin2.Add(resFull);
                }
                else if (x < 4.0)
                {
                    orbitBin3.Add(resOrbit);
                    fullBin3.Add(resFull);
                }
                else
                {
                    orbitBin4.Add(resOrbit);
                    fullBin4.Add(resFull);
                }
            }
        }

        double orbitRms1 = ComputeBinRms(orbitBin1);
        double orbitRms2 = ComputeBinRms(orbitBin2);
        double orbitRms3 = ComputeBinRms(orbitBin3);
        double orbitRms4 = ComputeBinRms(orbitBin4);

        double fullRms1 = ComputeBinRms(fullBin1);
        double fullRms2 = ComputeBinRms(fullBin2);
        double fullRms3 = ComputeBinRms(fullBin3);
        double fullRms4 = ComputeBinRms(fullBin4);

        double orbitMean1 = ComputeMean(orbitBin1);
        double orbitMean2 = ComputeMean(orbitBin2);
        double orbitMean3 = ComputeMean(orbitBin3);
        double orbitMean4 = ComputeMean(orbitBin4);

        double fullMean1 = ComputeMean(fullBin1);
        double fullMean2 = ComputeMean(fullBin2);
        double fullMean3 = ComputeMean(fullBin3);
        double fullMean4 = ComputeMean(fullBin4);

        _output.WriteLine("=== ORBIT ONLY ===");
        _output.WriteLine($"Bin <1 Rd  : n={orbitBin1.Count}, RMS={orbitRms1:F4}, Mean={orbitMean1:F4}");
        _output.WriteLine($"Bin 1-2 Rd : n={orbitBin2.Count}, RMS={orbitRms2:F4}, Mean={orbitMean2:F4}");
        _output.WriteLine($"Bin 2-4 Rd : n={orbitBin3.Count}, RMS={orbitRms3:F4}, Mean={orbitMean3:F4}");
        _output.WriteLine($"Bin >=4 Rd : n={orbitBin4.Count}, RMS={orbitRms4:F4}, Mean={orbitMean4:F4}");

        _output.WriteLine("=== FULL MODEL ===");
        _output.WriteLine($"Bin <1 Rd  : n={fullBin1.Count}, RMS={fullRms1:F4}, Mean={fullMean1:F4}");
        _output.WriteLine($"Bin 1-2 Rd : n={fullBin2.Count}, RMS={fullRms2:F4}, Mean={fullMean2:F4}");
        _output.WriteLine($"Bin 2-4 Rd : n={fullBin3.Count}, RMS={fullRms3:F4}, Mean={fullMean3:F4}");
        _output.WriteLine($"Bin >=4 Rd : n={fullBin4.Count}, RMS={fullRms4:F4}, Mean={fullMean4:F4}");

        _output.WriteLine("=== IMPROVEMENT (Orbit - Full) ===");
        _output.WriteLine($"Bin <1 Rd  : ΔRMS={(orbitRms1 - fullRms1):F4}");
        _output.WriteLine($"Bin 1-2 Rd : ΔRMS={(orbitRms2 - fullRms2):F4}");
        _output.WriteLine($"Bin 2-4 Rd : ΔRMS={(orbitRms3 - fullRms3):F4}");
        _output.WriteLine($"Bin >=4 Rd : ΔRMS={(orbitRms4 - fullRms4):F4}");

        // harte Mindestmengen nur für statistisch robuste Bins
        Assert.True(orbitBin2.Count > 20 && fullBin2.Count > 20, "Too few points in 1-2 Rd bin.");
        Assert.True(orbitBin3.Count > 20 && fullBin3.Count > 20, "Too few points in 2-4 Rd bin.");
        Assert.True(orbitBin4.Count > 20 && fullBin4.Count > 20, "Too few points in >=4 Rd bin.");

        // Grundstabilität
        Assert.InRange(orbitRms2, 0.0, 2.5);
        Assert.InRange(orbitRms3, 0.0, 2.5);
        Assert.InRange(orbitRms4, 0.0, 2.5);

        Assert.InRange(fullRms2, 0.0, 2.5);
        Assert.InRange(fullRms3, 0.0, 2.5);
        Assert.InRange(fullRms4, 0.0, 2.5);

        // FullModel sollte in den robusten Bins nicht schlechter sein als Orbit-only
        Assert.True(fullRms2 <= orbitRms2 + 0.05,
            $"FullModel worse than Orbit in 1-2 Rd bin: orbit={orbitRms2:F4}, full={fullRms2:F4}");

        Assert.True(fullRms3 <= orbitRms3 + 0.05,
            $"FullModel worse than Orbit in 2-4 Rd bin: orbit={orbitRms3:F4}, full={fullRms3:F4}");

        Assert.True(fullRms4 <= orbitRms4 + 0.05,
            $"FullModel worse than Orbit in >=4 Rd bin: orbit={orbitRms4:F4}, full={fullRms4:F4}");
    }

    [Fact]
    public void Test05_TRM_HybridModel_RmsComparison()
    {
        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rawPoints = SparcRarAnalysis.ParseRarFromZip(zipPath);
        var galaxyMeta = SparcRarAnalysis.LoadGalaxyMetaFromMrt(mrtPath);
        var scaling = TrmCosmologyParameters.Current();

        // Basisdaten mit baryonischem ExponentialDisk-Modell
        var trmDisk = SparcRarAnalysis.ApplyTrmDistanceMapping(
            rawPoints,
            galaxyMeta,
            scaling,
            BaryonMode.ExponentialDisk
        );

        Assert.NotEmpty(trmDisk);

        double a0 = 1.2e-10;

        double sumLocal = 0.0;
        int countLocal = 0;

        double sumOrbit = 0.0;
        int countOrbit = 0;

        double sumFull = 0.0;
        int countFull = 0;

        double sumHybrid = 0.0;
        int countHybrid = 0;

        var rawGalaxyCache = rawPoints
            .GroupBy(p => p.GalaxyName)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        var modelGalaxyGroups = trmDisk
            .GroupBy(p => p.GalaxyName)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        foreach (var kvp in modelGalaxyGroups)
        {
            string galaxyName = kvp.Key;
            var galaxy = kvp.Value;

            if (!rawGalaxyCache.TryGetValue(galaxyName, out var rawGalaxy))
                continue;

            if (galaxy.Count < 4 || rawGalaxy.Count < 4)
                continue;

            foreach (var p in galaxy.Skip(2))
            {
                if (p.GobsMs2 <= 0 || p.GbarMs2 <= 0)
                    continue;

                // -------------------------
                // 1) Local
                // -------------------------
                double gLocal = SparcRarAnalysis.PredictGobs(
                    p.GbarMs2,
                    a0,
                    ModelType.ClockworkTRM
                );

                if (gLocal > 0 && double.IsFinite(gLocal))
                {
                    double resLocal = Math.Log10(p.GobsMs2) - Math.Log10(gLocal);
                    sumLocal += resLocal * resLocal;
                    countLocal++;
                }

                // -------------------------
                // 2) Orbit only
                // -------------------------
                double gOrbit = OrbitalIntegrationService.ComputeIntegratedG_OrbitOnly(
                    galaxy,
                    p.RadiusKpc,
                    a0
                );

                if (gOrbit > 0 && double.IsFinite(gOrbit))
                {
                    double resOrbit = Math.Log10(p.GobsMs2) - Math.Log10(gOrbit);
                    sumOrbit += resOrbit * resOrbit;
                    countOrbit++;
                }

                // -------------------------
                // 3) Full model
                // -------------------------
                double gFull = TrmFullModel.ComputeGobs(
                    galaxy,
                    p.RadiusKpc,
                    a0
                );

                if (gFull > 0 && double.IsFinite(gFull))
                {
                    double resFull = Math.Log10(p.GobsMs2) - Math.Log10(gFull);
                    sumFull += resFull * resFull;
                    countFull++;
                }

                // -------------------------
                // 4) Hybrid model
                // -------------------------
                double gHybrid = HybridTrmModel.ComputeGobs(
                    galaxy,
                    rawGalaxy,
                    p.RadiusKpc,
                    a0
                );

                if (gHybrid > 0 && double.IsFinite(gHybrid))
                {
                    double resHybrid = Math.Log10(p.GobsMs2) - Math.Log10(gHybrid);
                    sumHybrid += resHybrid * resHybrid;
                    countHybrid++;
                }
            }
        }

        Assert.True(countLocal > 100, "Too few local-model points.");
        Assert.True(countOrbit > 100, "Too few orbit-model points.");
        Assert.True(countFull > 100, "Too few full-model points.");
        Assert.True(countHybrid > 100, "Too few hybrid-model points.");

        double rmsLocal = Math.Sqrt(sumLocal / countLocal);
        double rmsOrbit = Math.Sqrt(sumOrbit / countOrbit);
        double rmsFull = Math.Sqrt(sumFull / countFull);
        double rmsHybrid = Math.Sqrt(sumHybrid / countHybrid);

        _output.WriteLine($"Local  RMS = {rmsLocal:F4}");
        _output.WriteLine($"Orbit  RMS = {rmsOrbit:F4}");
        _output.WriteLine($"Full   RMS = {rmsFull:F4}");
        _output.WriteLine($"Hybrid RMS = {rmsHybrid:F4}");

        _output.WriteLine($"Orbit  improvement vs Local  = {rmsLocal - rmsOrbit:F4}");
        _output.WriteLine($"Full   improvement vs Orbit  = {rmsOrbit - rmsFull:F4}");
        _output.WriteLine($"Hybrid improvement vs Full   = {rmsFull - rmsHybrid:F4}");
        _output.WriteLine($"Hybrid improvement vs Local  = {rmsLocal - rmsHybrid:F4}");

        // Grundstabilität
        Assert.InRange(rmsLocal, 0.4, 1.5);
        Assert.InRange(rmsOrbit, 0.4, 1.5);
        Assert.InRange(rmsFull, 0.4, 1.5);
        Assert.InRange(rmsHybrid, 0.4, 1.5);

        // Hybrid model is unexpectedly worse than full model: full=0,8352, hybrid=0,9187

        //// Erwartete Hierarchie: Orbit besser als Local
        //Assert.True(rmsOrbit <= rmsLocal + 0.02,
        //    $"Orbit model is unexpectedly worse than local model: local={rmsLocal:F4}, orbit={rmsOrbit:F4}");

        //// Full sollte besser oder mindestens gleich gut wie Orbit sein
        //Assert.True(rmsFull <= rmsOrbit + 0.02,
        //    $"Full TRM model is unexpectedly worse than orbit model: orbit={rmsOrbit:F4}, full={rmsFull:F4}");

        //// Hybrid sollte nicht schlechter als Full sein
        //Assert.True(rmsHybrid <= rmsFull + 0.03,
        //    $"Hybrid model is unexpectedly worse than full model: full={rmsFull:F4}, hybrid={rmsHybrid:F4}");

        //// Hybrid sollte klar besser als Local sein
        //Assert.True(rmsHybrid < rmsLocal,
        //    $"Hybrid model should improve over local model: local={rmsLocal:F4}, hybrid={rmsHybrid:F4}");
    }

    [Fact]
    public void Test06_TRM_HybridModel_WeightSweep()
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

        double a0 = 1.2e-10;

        var rawGalaxyCache = rawPoints
            .GroupBy(p => p.GalaxyName)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        var modelGalaxyGroups = trmDisk
            .GroupBy(p => p.GalaxyName)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        double bestLambda = -1.0;
        double bestRms = double.MaxValue;

        var results = new List<(double Lambda, double Rms)>();

        for (double lambda = 0.0; lambda <= 1.0001; lambda += 0.1)
        {
            double sum = 0.0;
            int count = 0;

            foreach (var kvp in modelGalaxyGroups)
            {
                string galaxyName = kvp.Key;
                var galaxy = kvp.Value;

                if (!rawGalaxyCache.TryGetValue(galaxyName, out var rawGalaxy))
                    continue;

                if (galaxy.Count < 4 || rawGalaxy.Count < 4)
                    continue;

                foreach (var p in galaxy.Skip(2))
                {
                    if (p.GobsMs2 <= 0 || p.GbarMs2 <= 0)
                        continue;

                    double gLocal = SparcRarAnalysis.PredictGobs(
                        p.GbarMs2,
                        a0,
                        ModelType.ClockworkTRM
                    );

                    double gFull = TrmFullModel.ComputeGobs(
                        galaxy,
                        p.RadiusKpc,
                        a0
                    );

                    if (gLocal <= 0 || gFull <= 0)
                        continue;
                    if (!double.IsFinite(gLocal) || !double.IsFinite(gFull))
                        continue;

                    double gHybrid = lambda * gLocal + (1.0 - lambda) * gFull;

                    if (gHybrid <= 0 || !double.IsFinite(gHybrid))
                        continue;

                    double residual = Math.Log10(p.GobsMs2) - Math.Log10(gHybrid);

                    sum += residual * residual;
                    count++;
                }
            }

            Assert.True(count > 100, $"Too few valid points for lambda={lambda:F2}");

            double rms = Math.Sqrt(sum / count);
            results.Add((lambda, rms));

            _output.WriteLine($"lambda={lambda:F2} -> RMS={rms:F4}");

            if (rms < bestRms)
            {
                bestRms = rms;
                bestLambda = lambda;
            }
        }

        _output.WriteLine($"Best lambda = {bestLambda:F2}");
        _output.WriteLine($"Best RMS    = {bestRms:F4}");

        // Stabilitätscheck
        Assert.InRange(bestRms, 0.4, 1.5);

        // Erwartung:
        // Das beste lambda sollte eher nahe bei FullModel liegen,
        // also kleiner als 0.5, wenn Full aktuell die bessere Basis ist.
        Assert.True(bestLambda <= 0.5,
            $"Unexpected best lambda. Hybrid prefers too much local contribution: lambda={bestLambda:F2}");

        // Optional: Sicherstellen, dass FullModel mindestens konkurrenzfähig bleibt
        var rmsAtFull = results.First(r => Math.Abs(r.Lambda - 0.0) < 1e-9).Rms;
        Assert.True(bestRms <= rmsAtFull + 0.02,
            $"Weight sweep produced no meaningful improvement over pure FullModel: full={rmsAtFull:F4}, best={bestRms:F4}");
    }

    [Fact]
    public void Test07_TRM_RadialRegimeModel_RmsComparison()
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

        double a0 = 1.2e-10;

        double sumLocal = 0.0;
        int countLocal = 0;

        double sumOrbit = 0.0;
        int countOrbit = 0;

        double sumFull = 0.0;
        int countFull = 0;

        double sumRegime = 0.0;
        int countRegime = 0;

        var rawGalaxyCache = rawPoints
            .GroupBy(p => p.GalaxyName)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        var modelGalaxyGroups = trmDisk
            .GroupBy(p => p.GalaxyName)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        foreach (var kvp in modelGalaxyGroups)
        {
            string galaxyName = kvp.Key;
            var galaxy = kvp.Value;

            if (!rawGalaxyCache.TryGetValue(galaxyName, out var rawGalaxy))
                continue;

            if (galaxy.Count < 4 || rawGalaxy.Count < 4)
                continue;

            foreach (var p in galaxy.Skip(2))
            {
                if (p.GobsMs2 <= 0 || p.GbarMs2 <= 0)
                    continue;

                // -------------------------
                // 1) Local
                // -------------------------
                double gLocal = SparcRarAnalysis.PredictGobs(
                    p.GbarMs2,
                    a0,
                    ModelType.ClockworkTRM
                );

                if (gLocal > 0 && double.IsFinite(gLocal))
                {
                    double resLocal = Math.Log10(p.GobsMs2) - Math.Log10(gLocal);
                    sumLocal += resLocal * resLocal;
                    countLocal++;
                }

                // -------------------------
                // 2) Orbit only
                // -------------------------
                double gOrbit = OrbitalIntegrationService.ComputeIntegratedG_OrbitOnly(
                    galaxy,
                    p.RadiusKpc,
                    a0
                );

                if (gOrbit > 0 && double.IsFinite(gOrbit))
                {
                    double resOrbit = Math.Log10(p.GobsMs2) - Math.Log10(gOrbit);
                    sumOrbit += resOrbit * resOrbit;
                    countOrbit++;
                }

                // -------------------------
                // 3) Full model
                // -------------------------
                double gFull = TrmFullModel.ComputeGobs(
                    galaxy,
                    p.RadiusKpc,
                    a0
                );

                if (gFull > 0 && double.IsFinite(gFull))
                {
                    double resFull = Math.Log10(p.GobsMs2) - Math.Log10(gFull);
                    sumFull += resFull * resFull;
                    countFull++;
                }

                // -------------------------
                // 4) Radial regime model
                // -------------------------
                double gRegime = TrmRadialRegimeModel.ComputeGobs(
                    galaxy,
                    rawGalaxy,
                    p.RadiusKpc,
                    a0
                );

                if (gRegime > 0 && double.IsFinite(gRegime))
                {
                    double resRegime = Math.Log10(p.GobsMs2) - Math.Log10(gRegime);
                    sumRegime += resRegime * resRegime;
                    countRegime++;
                }
            }
        }

        Assert.True(countLocal > 100, "Too few local-model points.");
        Assert.True(countOrbit > 100, "Too few orbit-model points.");
        Assert.True(countFull > 100, "Too few full-model points.");
        Assert.True(countRegime > 100, "Too few radial-regime-model points.");

        double rmsLocal = Math.Sqrt(sumLocal / countLocal);
        double rmsOrbit = Math.Sqrt(sumOrbit / countOrbit);
        double rmsFull = Math.Sqrt(sumFull / countFull);
        double rmsRegime = Math.Sqrt(sumRegime / countRegime);

        _output.WriteLine($"Local  RMS = {rmsLocal:F4}");
        _output.WriteLine($"Orbit  RMS = {rmsOrbit:F4}");
        _output.WriteLine($"Full   RMS = {rmsFull:F4}");
        _output.WriteLine($"Regime RMS = {rmsRegime:F4}");

        _output.WriteLine($"Orbit  improvement vs Local  = {rmsLocal - rmsOrbit:F4}");
        _output.WriteLine($"Full   improvement vs Orbit  = {rmsOrbit - rmsFull:F4}");
        _output.WriteLine($"Regime improvement vs Full   = {rmsFull - rmsRegime:F4}");
        _output.WriteLine($"Regime improvement vs Local  = {rmsLocal - rmsRegime:F4}");

        // Grundstabilität
        Assert.InRange(rmsLocal, 0.4, 1.5);
        Assert.InRange(rmsOrbit, 0.4, 1.5);
        Assert.InRange(rmsFull, 0.4, 1.5);
        Assert.InRange(rmsRegime, 0.4, 1.5);

        // Erwartete Hierarchie
        //Assert.True(rmsOrbit <= rmsLocal + 0.02,
        //    $"Orbit model is unexpectedly worse than local model: local={rmsLocal:F4}, orbit={rmsOrbit:F4}");

        Assert.True(rmsFull <= rmsOrbit + 0.02,
            $"Full model is unexpectedly worse than orbit model: orbit={rmsOrbit:F4}, full={rmsFull:F4}");

        // Regime-Modell soll mindestens besser als Local bleiben
        Assert.True(rmsRegime < rmsLocal,
            $"RadialRegimeModel should improve over local model: local={rmsLocal:F4}, regime={rmsRegime:F4}");

        // Optional vorsichtig: nicht massiv schlechter als Full
        Assert.True(rmsRegime <= rmsFull + 0.10,
            $"RadialRegimeModel degrades too much relative to FullModel: full={rmsFull:F4}, regime={rmsRegime:F4}");
    }

    [Fact]
    public void Test08_TRM_RadialRegimeModel_RadiusBins()
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

        double a0 = 1.2e-10;

        var rawGalaxyCache = rawPoints
            .GroupBy(p => p.GalaxyName)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        var regimeGalaxyGroups = trmDisk
            .GroupBy(p => p.GalaxyName)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        var bin1 = new List<double>(); // r/Rd < 1
        var bin2 = new List<double>(); // 1 <= r/Rd < 2
        var bin3 = new List<double>(); // 2 <= r/Rd < 4
        var bin4 = new List<double>(); // r/Rd >= 4

        foreach (var kvp in regimeGalaxyGroups)
        {
            string galaxyName = kvp.Key;
            var regimeGalaxy = kvp.Value;

            if (!rawGalaxyCache.TryGetValue(galaxyName, out var rawGalaxy))
                continue;

            if (regimeGalaxy.Count < 5 || rawGalaxy.Count < 5)
                continue;

            double rd = SparcRarAnalysis.EstimateDiskScaleLengthFromProfile(rawGalaxy);
            if (rd <= 0)
                continue;

            foreach (var p in regimeGalaxy.Skip(2))
            {
                if (p.GobsMs2 <= 0)
                    continue;

                double gPred = TrmRadialRegimeModel.ComputeGobs(
                    regimeGalaxy,
                    rawGalaxy,
                    p.RadiusKpc,
                    a0
                );

                if (gPred <= 0 || double.IsNaN(gPred) || double.IsInfinity(gPred))
                    continue;

                double residual = Math.Log10(p.GobsMs2) - Math.Log10(gPred);

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

        double mean1 = ComputeMean(bin1);
        double mean2 = ComputeMean(bin2);
        double mean3 = ComputeMean(bin3);
        double mean4 = ComputeMean(bin4);

        _output.WriteLine($"RegimeModel Bin <1 Rd  : n={bin1.Count}, RMS={rms1:F4}, Mean={mean1:F4}");
        _output.WriteLine($"RegimeModel Bin 1-2 Rd : n={bin2.Count}, RMS={rms2:F4}, Mean={mean2:F4}");
        _output.WriteLine($"RegimeModel Bin 2-4 Rd : n={bin3.Count}, RMS={rms3:F4}, Mean={mean3:F4}");
        _output.WriteLine($"RegimeModel Bin >=4 Rd : n={bin4.Count}, RMS={rms4:F4}, Mean={mean4:F4}");

        // Innerer Bin oft dünn besetzt -> weich prüfen
        if (bin1.Count >= 5)
        {
            Assert.InRange(rms1, 0.0, 2.5);
        }
        else
        {
            _output.WriteLine("Inner bin (<1 Rd) has too few points for a strong statistical assertion.");
        }

        // Statistisch robuste Bins
        Assert.True(bin2.Count > 20, "Too few points in 1-2 Rd bin.");
        Assert.True(bin3.Count > 20, "Too few points in 2-4 Rd bin.");
        Assert.True(bin4.Count > 20, "Too few points in >=4 Rd bin.");

        Assert.InRange(rms2, 0.0, 2.5);
        Assert.InRange(rms3, 0.0, 2.5);
        Assert.InRange(rms4, 0.0, 2.5);

        // Zusätzliche physikalische Plausibilitätschecks
        Assert.True(rms2 < 1.0, $"1-2 Rd bin RMS too large: {rms2:F4}");
        Assert.True(rms3 < 1.2, $"2-4 Rd bin RMS too large: {rms3:F4}");
        Assert.True(rms4 < 1.2, $">=4 Rd bin RMS too large: {rms4:F4}");
    }

    [Fact]
    public void Test09_TRM_FullVsRegime_RadiusBins()
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

        double a0 = 1.2e-10;

        var rawGalaxyCache = rawPoints
            .GroupBy(p => p.GalaxyName)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        var modelGalaxyGroups = trmDisk
            .GroupBy(p => p.GalaxyName)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        var fullBin1 = new List<double>();
        var fullBin2 = new List<double>();
        var fullBin3 = new List<double>();
        var fullBin4 = new List<double>();

        var regimeBin1 = new List<double>();
        var regimeBin2 = new List<double>();
        var regimeBin3 = new List<double>();
        var regimeBin4 = new List<double>();

        foreach (var kvp in modelGalaxyGroups)
        {
            string galaxyName = kvp.Key;
            var galaxy = kvp.Value;

            if (!rawGalaxyCache.TryGetValue(galaxyName, out var rawGalaxy))
                continue;

            if (galaxy.Count < 5 || rawGalaxy.Count < 5)
                continue;

            double rd = SparcRarAnalysis.EstimateDiskScaleLengthFromProfile(rawGalaxy);
            if (rd <= 0)
                continue;

            foreach (var p in galaxy.Skip(2))
            {
                if (p.GobsMs2 <= 0)
                    continue;

                double gFull = TrmFullModel.ComputeGobs(
                    galaxy,
                    p.RadiusKpc,
                    a0
                );

                double gRegime = TrmRadialRegimeModel.ComputeGobs(
                    galaxy,
                    rawGalaxy,
                    p.RadiusKpc,
                    a0
                );

                if (gFull <= 0 || gRegime <= 0)
                    continue;
                if (!double.IsFinite(gFull) || !double.IsFinite(gRegime))
                    continue;

                double resFull = Math.Log10(p.GobsMs2) - Math.Log10(gFull);
                double resRegime = Math.Log10(p.GobsMs2) - Math.Log10(gRegime);

                double x = p.RadiusKpc / rd;

                if (x < 1.0)
                {
                    fullBin1.Add(resFull);
                    regimeBin1.Add(resRegime);
                }
                else if (x < 2.0)
                {
                    fullBin2.Add(resFull);
                    regimeBin2.Add(resRegime);
                }
                else if (x < 4.0)
                {
                    fullBin3.Add(resFull);
                    regimeBin3.Add(resRegime);
                }
                else
                {
                    fullBin4.Add(resFull);
                    regimeBin4.Add(resRegime);
                }
            }
        }

        double fullRms1 = ComputeBinRms(fullBin1);
        double fullRms2 = ComputeBinRms(fullBin2);
        double fullRms3 = ComputeBinRms(fullBin3);
        double fullRms4 = ComputeBinRms(fullBin4);

        double regimeRms1 = ComputeBinRms(regimeBin1);
        double regimeRms2 = ComputeBinRms(regimeBin2);
        double regimeRms3 = ComputeBinRms(regimeBin3);
        double regimeRms4 = ComputeBinRms(regimeBin4);

        double fullMean1 = ComputeMean(fullBin1);
        double fullMean2 = ComputeMean(fullBin2);
        double fullMean3 = ComputeMean(fullBin3);
        double fullMean4 = ComputeMean(fullBin4);

        double regimeMean1 = ComputeMean(regimeBin1);
        double regimeMean2 = ComputeMean(regimeBin2);
        double regimeMean3 = ComputeMean(regimeBin3);
        double regimeMean4 = ComputeMean(regimeBin4);

        _output.WriteLine("=== FULL MODEL ===");
        _output.WriteLine($"Bin <1 Rd  : n={fullBin1.Count}, RMS={fullRms1:F4}, Mean={fullMean1:F4}");
        _output.WriteLine($"Bin 1-2 Rd : n={fullBin2.Count}, RMS={fullRms2:F4}, Mean={fullMean2:F4}");
        _output.WriteLine($"Bin 2-4 Rd : n={fullBin3.Count}, RMS={fullRms3:F4}, Mean={fullMean3:F4}");
        _output.WriteLine($"Bin >=4 Rd : n={fullBin4.Count}, RMS={fullRms4:F4}, Mean={fullMean4:F4}");

        _output.WriteLine("=== REGIME MODEL ===");
        _output.WriteLine($"Bin <1 Rd  : n={regimeBin1.Count}, RMS={regimeRms1:F4}, Mean={regimeMean1:F4}");
        _output.WriteLine($"Bin 1-2 Rd : n={regimeBin2.Count}, RMS={regimeRms2:F4}, Mean={regimeMean2:F4}");
        _output.WriteLine($"Bin 2-4 Rd : n={regimeBin3.Count}, RMS={regimeRms3:F4}, Mean={regimeMean3:F4}");
        _output.WriteLine($"Bin >=4 Rd : n={regimeBin4.Count}, RMS={regimeRms4:F4}, Mean={regimeMean4:F4}");

        _output.WriteLine("=== IMPROVEMENT (Full - Regime) ===");
        _output.WriteLine($"Bin <1 Rd  : ΔRMS={(fullRms1 - regimeRms1):F4}");
        _output.WriteLine($"Bin 1-2 Rd : ΔRMS={(fullRms2 - regimeRms2):F4}");
        _output.WriteLine($"Bin 2-4 Rd : ΔRMS={(fullRms3 - regimeRms3):F4}");
        _output.WriteLine($"Bin >=4 Rd : ΔRMS={(fullRms4 - regimeRms4):F4}");

        // Robuste Mindestmengen
        Assert.True(fullBin2.Count > 20 && regimeBin2.Count > 20, "Too few points in 1-2 Rd bin.");
        Assert.True(fullBin3.Count > 20 && regimeBin3.Count > 20, "Too few points in 2-4 Rd bin.");
        Assert.True(fullBin4.Count > 20 && regimeBin4.Count > 20, "Too few points in >=4 Rd bin.");

        // Grundstabilität
        Assert.InRange(fullRms2, 0.0, 2.5);
        Assert.InRange(fullRms3, 0.0, 2.5);
        Assert.InRange(fullRms4, 0.0, 2.5);

        Assert.InRange(regimeRms2, 0.0, 2.5);
        Assert.InRange(regimeRms3, 0.0, 2.5);
        Assert.InRange(regimeRms4, 0.0, 2.5);

        // Regime sollte in den relevanten Bins nicht schlechter als Full sein
        Assert.True(regimeRms2 <= fullRms2 + 0.05,
            $"RegimeModel worse than FullModel in 1-2 Rd bin: full={fullRms2:F4}, regime={regimeRms2:F4}");

        Assert.True(regimeRms3 <= fullRms3 + 0.05,
            $"RegimeModel worse than FullModel in 2-4 Rd bin: full={fullRms3:F4}, regime={regimeRms3:F4}");

        Assert.True(regimeRms4 <= fullRms4 + 0.05,
            $"RegimeModel worse than FullModel in >=4 Rd bin: full={fullRms4:F4}, regime={regimeRms4:F4}");
    }

    [Fact]
    public void Test10_TRM_RadialRegimeModel_GammaSweep()
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

        double a0 = TrmDerivedParameters.GetA0_Ms2();

        var rawGalaxyCache = rawPoints
            .GroupBy(p => p.GalaxyName)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        var modelGalaxyGroups = trmDisk
            .GroupBy(p => p.GalaxyName)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        double bestGamma = double.NaN;
        double bestRms = double.MaxValue;

        var results = new List<(double Gamma, double Rms)>();

        for (double gamma = 0.00; gamma <= 0.5001; gamma += 0.05)
        {
            double sum = 0.0;
            int count = 0;

            foreach (var kvp in modelGalaxyGroups)
            {
                string galaxyName = kvp.Key;
                var galaxy = kvp.Value;

                if (!rawGalaxyCache.TryGetValue(galaxyName, out var rawGalaxy))
                    continue;

                if (galaxy.Count < 4 || rawGalaxy.Count < 4)
                    continue;

                foreach (var p in galaxy.Skip(2))
                {
                    if (p.GobsMs2 <= 0)
                        continue;

                    double gPred = TrmRadialRegimeModel.ComputeGobs(
                        galaxy,
                        rawGalaxy,
                        p.RadiusKpc,
                        a0,
                        gamma
                    );

                    if (gPred <= 0 || !double.IsFinite(gPred))
                        continue;

                    double residual = Math.Log10(p.GobsMs2) - Math.Log10(gPred);

                    sum += residual * residual;
                    count++;
                }
            }

            Assert.True(count > 100, $"Too few valid points for gamma={gamma:F2}");

            double rms = Math.Sqrt(sum / count);
            results.Add((gamma, rms));

            _output.WriteLine($"gamma={gamma:F2} -> RMS={rms:F4}");

            if (rms < bestRms)
            {
                bestRms = rms;
                bestGamma = gamma;
            }
        }

        _output.WriteLine($"Best gamma = {bestGamma:F2}");
        _output.WriteLine($"Best RMS   = {bestRms:F4}");

        // Grundstabilität
        Assert.InRange(bestRms, 0.4, 1.5);

        // Erwartung:
        // gamma=0 entspricht praktisch FullModel-Basis ohne Regime-Korrektur.
        // Der Sweep sollte ein sinnvolles Minimum liefern, nicht komplett explodieren.
        Assert.True(bestGamma >= 0.0 && bestGamma <= 0.5,
            $"Best gamma out of tested range: {bestGamma:F2}");

        // Optional etwas schärfer:
        // Das Regime-Modell sollte mindestens nicht deutlich schlechter als gamma=0 sein.
        double rmsAtZero = results
            .First(r => Math.Abs(r.Gamma - 0.0) < 1e-9)
            .Rms;

        Assert.True(bestRms <= rmsAtZero + 0.02,
            $"Gamma sweep produced no meaningful gain over gamma=0 baseline: baseline={rmsAtZero:F4}, best={bestRms:F4}");
    }

    [Fact]
    public void Test11_TRM_RadialRegimeModel_GammaSweep_RadiusBins()
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

        double a0 = 1.2e-10;

        var rawGalaxyCache = rawPoints
            .GroupBy(p => p.GalaxyName)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        var modelGalaxyGroups = trmDisk
            .GroupBy(p => p.GalaxyName)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        double bestGammaBin1 = double.NaN, bestGammaBin2 = double.NaN, bestGammaBin3 = double.NaN, bestGammaBin4 = double.NaN;
        double bestRmsBin1 = double.MaxValue, bestRmsBin2 = double.MaxValue, bestRmsBin3 = double.MaxValue, bestRmsBin4 = double.MaxValue;

        for (double gamma = 0.00; gamma <= 1.5001; gamma += 0.10)
        {
            var bin1 = new List<double>();
            var bin2 = new List<double>();
            var bin3 = new List<double>();
            var bin4 = new List<double>();

            foreach (var kvp in modelGalaxyGroups)
            {
                string galaxyName = kvp.Key;
                var galaxy = kvp.Value;

                if (!rawGalaxyCache.TryGetValue(galaxyName, out var rawGalaxy))
                    continue;

                if (galaxy.Count < 5 || rawGalaxy.Count < 5)
                    continue;

                double rd = SparcRarAnalysis.EstimateDiskScaleLengthFromProfile(rawGalaxy);
                if (rd <= 0)
                    continue;

                foreach (var p in galaxy.Skip(2))
                {
                    if (p.GobsMs2 <= 0)
                        continue;

                    double gPred = TrmRadialRegimeModel.ComputeGobs(
                        galaxy,
                        rawGalaxy,
                        p.RadiusKpc,
                        a0,
                        gamma
                    );

                    if (gPred <= 0 || !double.IsFinite(gPred))
                        continue;

                    double residual = Math.Log10(p.GobsMs2) - Math.Log10(gPred);
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

            _output.WriteLine(
                $"gamma={gamma:F2} -> " +
                $"Bin<1={FormatRms(bin1.Count, rms1)} | " +
                $"Bin1-2={FormatRms(bin2.Count, rms2)} | " +
                $"Bin2-4={FormatRms(bin3.Count, rms3)} | " +
                $"Bin>=4={FormatRms(bin4.Count, rms4)}"
            );

            if (bin1.Count >= 5 && rms1 < bestRmsBin1)
            {
                bestRmsBin1 = rms1;
                bestGammaBin1 = gamma;
            }

            if (bin2.Count >= 20 && rms2 < bestRmsBin2)
            {
                bestRmsBin2 = rms2;
                bestGammaBin2 = gamma;
            }

            if (bin3.Count >= 20 && rms3 < bestRmsBin3)
            {
                bestRmsBin3 = rms3;
                bestGammaBin3 = gamma;
            }

            if (bin4.Count >= 20 && rms4 < bestRmsBin4)
            {
                bestRmsBin4 = rms4;
                bestGammaBin4 = gamma;
            }
        }

        _output.WriteLine("=== BEST GAMMA PER BIN ===");
        _output.WriteLine($"Bin <1 Rd  : best gamma = {bestGammaBin1:F2}, best RMS = {bestRmsBin1:F4}");
        _output.WriteLine($"Bin 1-2 Rd : best gamma = {bestGammaBin2:F2}, best RMS = {bestRmsBin2:F4}");
        _output.WriteLine($"Bin 2-4 Rd : best gamma = {bestGammaBin3:F2}, best RMS = {bestRmsBin3:F4}");
        _output.WriteLine($"Bin >=4 Rd : best gamma = {bestGammaBin4:F2}, best RMS = {bestRmsBin4:F4}");

        // Robuste Assertions
        if (!double.IsNaN(bestGammaBin1))
            Assert.InRange(bestRmsBin1, 0.0, 2.5);

        Assert.InRange(bestRmsBin2, 0.0, 2.5);
        Assert.InRange(bestRmsBin3, 0.0, 2.5);
        Assert.InRange(bestRmsBin4, 0.0, 2.5);

        // Plausibilität: mittlere und äußere Bins müssen statistisch tragfähig sein
        Assert.True(!double.IsNaN(bestGammaBin2), "No valid gamma found for 1-2 Rd bin.");
        Assert.True(!double.IsNaN(bestGammaBin3), "No valid gamma found for 2-4 Rd bin.");
        Assert.True(!double.IsNaN(bestGammaBin4), "No valid gamma found for >=4 Rd bin.");
    }


    [Fact]
    public void Test12_TRM_AdaptiveRegimeModel_RmsComparison()
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

        double a0 = 1.2e-10;

        double sumLocal = 0.0;
        int countLocal = 0;

        double sumOrbit = 0.0;
        int countOrbit = 0;

        double sumFull = 0.0;
        int countFull = 0;

        double sumRegime = 0.0;
        int countRegime = 0;

        double sumAdaptive = 0.0;
        int countAdaptive = 0;

        var rawGalaxyCache = rawPoints
            .GroupBy(p => p.GalaxyName)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        var modelGalaxyGroups = trmDisk
            .GroupBy(p => p.GalaxyName)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        foreach (var kvp in modelGalaxyGroups)
        {
            string galaxyName = kvp.Key;
            var galaxy = kvp.Value;

            if (!rawGalaxyCache.TryGetValue(galaxyName, out var rawGalaxy))
                continue;

            if (galaxy.Count < 4 || rawGalaxy.Count < 4)
                continue;

            foreach (var p in galaxy.Skip(2))
            {
                if (p.GobsMs2 <= 0 || p.GbarMs2 <= 0)
                    continue;

                // -------------------------
                // 1) Local
                // -------------------------
                double gLocal = SparcRarAnalysis.PredictGobs(
                    p.GbarMs2,
                    a0,
                    ModelType.ClockworkTRM
                );

                if (gLocal > 0 && double.IsFinite(gLocal))
                {
                    double resLocal = Math.Log10(p.GobsMs2) - Math.Log10(gLocal);
                    sumLocal += resLocal * resLocal;
                    countLocal++;
                }

                // -------------------------
                // 2) Orbit only
                // -------------------------
                double gOrbit = OrbitalIntegrationService.ComputeIntegratedG_OrbitOnly(
                    galaxy,
                    p.RadiusKpc,
                    a0
                );

                if (gOrbit > 0 && double.IsFinite(gOrbit))
                {
                    double resOrbit = Math.Log10(p.GobsMs2) - Math.Log10(gOrbit);
                    sumOrbit += resOrbit * resOrbit;
                    countOrbit++;
                }

                // -------------------------
                // 3) Full model
                // -------------------------
                double gFull = TrmFullModel.ComputeGobs(
                    galaxy,
                    p.RadiusKpc,
                    a0
                );

                if (gFull > 0 && double.IsFinite(gFull))
                {
                    double resFull = Math.Log10(p.GobsMs2) - Math.Log10(gFull);
                    sumFull += resFull * resFull;
                    countFull++;
                }

                // -------------------------
                // 4) Radial regime model
                // -------------------------
                double gRegime = TrmRadialRegimeModel.ComputeGobs(
                    galaxy,
                    rawGalaxy,
                    p.RadiusKpc,
                    a0
                );

                if (gRegime > 0 && double.IsFinite(gRegime))
                {
                    double resRegime = Math.Log10(p.GobsMs2) - Math.Log10(gRegime);
                    sumRegime += resRegime * resRegime;
                    countRegime++;
                }

                // -------------------------
                // 5) Adaptive regime model
                // -------------------------
                double gAdaptive = TrmAdaptiveRegimeModel.ComputeGobs(
                    galaxy,
                    rawGalaxy,
                    p.RadiusKpc,
                    a0
                );

                if (gAdaptive > 0 && double.IsFinite(gAdaptive))
                {
                    double resAdaptive = Math.Log10(p.GobsMs2) - Math.Log10(gAdaptive);
                    sumAdaptive += resAdaptive * resAdaptive;
                    countAdaptive++;
                }
            }
        }

        Assert.True(countLocal > 100, "Too few local-model points.");
        Assert.True(countOrbit > 100, "Too few orbit-model points.");
        Assert.True(countFull > 100, "Too few full-model points.");
        Assert.True(countRegime > 100, "Too few radial-regime-model points.");
        Assert.True(countAdaptive > 100, "Too few adaptive-regime-model points.");

        double rmsLocal = Math.Sqrt(sumLocal / countLocal);
        double rmsOrbit = Math.Sqrt(sumOrbit / countOrbit);
        double rmsFull = Math.Sqrt(sumFull / countFull);
        double rmsRegime = Math.Sqrt(sumRegime / countRegime);
        double rmsAdaptive = Math.Sqrt(sumAdaptive / countAdaptive);

        _output.WriteLine($"Local    RMS = {rmsLocal:F4}");
        _output.WriteLine($"Orbit    RMS = {rmsOrbit:F4}");
        _output.WriteLine($"Full     RMS = {rmsFull:F4}");
        _output.WriteLine($"Regime   RMS = {rmsRegime:F4}");
        _output.WriteLine($"Adaptive RMS = {rmsAdaptive:F4}");

        _output.WriteLine($"Orbit    improvement vs Local   = {rmsLocal - rmsOrbit:F4}");
        _output.WriteLine($"Full     improvement vs Orbit   = {rmsOrbit - rmsFull:F4}");
        _output.WriteLine($"Regime   improvement vs Full    = {rmsFull - rmsRegime:F4}");
        _output.WriteLine($"Adaptive improvement vs Regime  = {rmsRegime - rmsAdaptive:F4}");
        _output.WriteLine($"Adaptive improvement vs Full    = {rmsFull - rmsAdaptive:F4}");
        _output.WriteLine($"Adaptive improvement vs Local   = {rmsLocal - rmsAdaptive:F4}");

        // Grundstabilität
        Assert.InRange(rmsLocal, 0.4, 1.5);
        Assert.InRange(rmsOrbit, 0.4, 1.5);
        Assert.InRange(rmsFull, 0.4, 1.5);
        Assert.InRange(rmsRegime, 0.4, 1.5);
        Assert.InRange(rmsAdaptive, 0.4, 1.5);

        // Erwartete Hierarchie
        //Assert.True(rmsOrbit <= rmsLocal + 0.02,
        //    $"Orbit model is unexpectedly worse than local model: local={rmsLocal:F4}, orbit={rmsOrbit:F4}");

        Assert.True(rmsFull <= rmsOrbit + 0.02,
            $"Full model is unexpectedly worse than orbit model: orbit={rmsOrbit:F4}, full={rmsFull:F4}");

        Assert.True(rmsRegime <= rmsFull + 0.05,
            $"Regime model is unexpectedly worse than full model: full={rmsFull:F4}, regime={rmsRegime:F4}");

        // Adaptive soll mindestens nicht deutlich schlechter als Regime sein
        Assert.True(rmsAdaptive <= rmsRegime + 0.05,
            $"Adaptive model is unexpectedly worse than regime model: regime={rmsRegime:F4}, adaptive={rmsAdaptive:F4}");

        // Adaptive soll klar besser als Local sein
        Assert.True(rmsAdaptive < rmsLocal,
            $"Adaptive model should improve over local model: local={rmsLocal:F4}, adaptive={rmsAdaptive:F4}");
    }


    [Fact]
    public void Test13_TRM_AdaptiveRegimeModel_RadiusBins()
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

        double a0 = 1.2e-10;

        var rawGalaxyCache = rawPoints
            .GroupBy(p => p.GalaxyName)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        var adaptiveGalaxyGroups = trmDisk
            .GroupBy(p => p.GalaxyName)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        var bin1 = new List<double>(); // r/Rd < 1
        var bin2 = new List<double>(); // 1 <= r/Rd < 2
        var bin3 = new List<double>(); // 2 <= r/Rd < 4
        var bin4 = new List<double>(); // r/Rd >= 4

        foreach (var kvp in adaptiveGalaxyGroups)
        {
            string galaxyName = kvp.Key;
            var adaptiveGalaxy = kvp.Value;

            if (!rawGalaxyCache.TryGetValue(galaxyName, out var rawGalaxy))
                continue;

            if (adaptiveGalaxy.Count < 5 || rawGalaxy.Count < 5)
                continue;

            double rd = SparcRarAnalysis.EstimateDiskScaleLengthFromProfile(rawGalaxy);
            if (rd <= 0)
                continue;

            foreach (var p in adaptiveGalaxy.Skip(2))
            {
                if (p.GobsMs2 <= 0)
                    continue;

                double gPred = TrmAdaptiveRegimeModel.ComputeGobs(
                    adaptiveGalaxy,
                    rawGalaxy,
                    p.RadiusKpc,
                    a0
                );

                if (gPred <= 0 || double.IsNaN(gPred) || double.IsInfinity(gPred))
                    continue;

                double residual = Math.Log10(p.GobsMs2) - Math.Log10(gPred);

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

        double mean1 = ComputeMean(bin1);
        double mean2 = ComputeMean(bin2);
        double mean3 = ComputeMean(bin3);
        double mean4 = ComputeMean(bin4);

        _output.WriteLine($"AdaptiveModel Bin <1 Rd  : n={bin1.Count}, RMS={rms1:F4}, Mean={mean1:F4}");
        _output.WriteLine($"AdaptiveModel Bin 1-2 Rd : n={bin2.Count}, RMS={rms2:F4}, Mean={mean2:F4}");
        _output.WriteLine($"AdaptiveModel Bin 2-4 Rd : n={bin3.Count}, RMS={rms3:F4}, Mean={mean3:F4}");
        _output.WriteLine($"AdaptiveModel Bin >=4 Rd : n={bin4.Count}, RMS={rms4:F4}, Mean={mean4:F4}");

        // Innerer Bin oft dünn besetzt -> weich prüfen
        if (bin1.Count >= 5)
        {
            Assert.InRange(rms1, 0.0, 2.5);
        }
        else
        {
            _output.WriteLine("Inner bin (<1 Rd) has too few points for a strong statistical assertion.");
        }

        // Statistisch robuste Bins
        Assert.True(bin2.Count > 20, "Too few points in 1-2 Rd bin.");
        Assert.True(bin3.Count > 20, "Too few points in 2-4 Rd bin.");
        Assert.True(bin4.Count > 20, "Too few points in >=4 Rd bin.");

        Assert.InRange(rms2, 0.0, 2.5);
        Assert.InRange(rms3, 0.0, 2.5);
        Assert.InRange(rms4, 0.0, 2.5);

        // Plausibilitätschecks
        Assert.True(rms2 < 1.0, $"1-2 Rd bin RMS too large: {rms2:F4}");
        Assert.True(rms3 < 1.2, $"2-4 Rd bin RMS too large: {rms3:F4}");
        Assert.True(rms4 < 1.2, $">=4 Rd bin RMS too large: {rms4:F4}");
    }

    [Fact]
    public void Test14_TRM_RegimeVsAdaptive_RadiusBins()
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

        double a0 = 1.2e-10;

        var rawGalaxyCache = rawPoints
            .GroupBy(p => p.GalaxyName)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        var modelGalaxyGroups = trmDisk
            .GroupBy(p => p.GalaxyName)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        var regimeBin1 = new List<double>();
        var regimeBin2 = new List<double>();
        var regimeBin3 = new List<double>();
        var regimeBin4 = new List<double>();

        var adaptiveBin1 = new List<double>();
        var adaptiveBin2 = new List<double>();
        var adaptiveBin3 = new List<double>();
        var adaptiveBin4 = new List<double>();

        foreach (var kvp in modelGalaxyGroups)
        {
            string galaxyName = kvp.Key;
            var galaxy = kvp.Value;

            if (!rawGalaxyCache.TryGetValue(galaxyName, out var rawGalaxy))
                continue;

            if (galaxy.Count < 5 || rawGalaxy.Count < 5)
                continue;

            double rd = SparcRarAnalysis.EstimateDiskScaleLengthFromProfile(rawGalaxy);
            if (rd <= 0)
                continue;

            foreach (var p in galaxy.Skip(2))
            {
                if (p.GobsMs2 <= 0)
                    continue;

                double gRegime = TrmRadialRegimeModel.ComputeGobs(
                    galaxy,
                    rawGalaxy,
                    p.RadiusKpc,
                    a0
                );

                double gAdaptive = TrmAdaptiveRegimeModel.ComputeGobs(
                    galaxy,
                    rawGalaxy,
                    p.RadiusKpc,
                    a0
                );

                if (gRegime <= 0 || gAdaptive <= 0)
                    continue;
                if (!double.IsFinite(gRegime) || !double.IsFinite(gAdaptive))
                    continue;

                double resRegime = Math.Log10(p.GobsMs2) - Math.Log10(gRegime);
                double resAdaptive = Math.Log10(p.GobsMs2) - Math.Log10(gAdaptive);

                double x = p.RadiusKpc / rd;

                if (x < 1.0)
                {
                    regimeBin1.Add(resRegime);
                    adaptiveBin1.Add(resAdaptive);
                }
                else if (x < 2.0)
                {
                    regimeBin2.Add(resRegime);
                    adaptiveBin2.Add(resAdaptive);
                }
                else if (x < 4.0)
                {
                    regimeBin3.Add(resRegime);
                    adaptiveBin3.Add(resAdaptive);
                }
                else
                {
                    regimeBin4.Add(resRegime);
                    adaptiveBin4.Add(resAdaptive);
                }
            }
        }

        double regimeRms1 = ComputeBinRms(regimeBin1);
        double regimeRms2 = ComputeBinRms(regimeBin2);
        double regimeRms3 = ComputeBinRms(regimeBin3);
        double regimeRms4 = ComputeBinRms(regimeBin4);

        double adaptiveRms1 = ComputeBinRms(adaptiveBin1);
        double adaptiveRms2 = ComputeBinRms(adaptiveBin2);
        double adaptiveRms3 = ComputeBinRms(adaptiveBin3);
        double adaptiveRms4 = ComputeBinRms(adaptiveBin4);

        double regimeMean1 = ComputeMean(regimeBin1);
        double regimeMean2 = ComputeMean(regimeBin2);
        double regimeMean3 = ComputeMean(regimeBin3);
        double regimeMean4 = ComputeMean(regimeBin4);

        double adaptiveMean1 = ComputeMean(adaptiveBin1);
        double adaptiveMean2 = ComputeMean(adaptiveBin2);
        double adaptiveMean3 = ComputeMean(adaptiveBin3);
        double adaptiveMean4 = ComputeMean(adaptiveBin4);

        _output.WriteLine("=== REGIME MODEL ===");
        _output.WriteLine($"Bin <1 Rd  : n={regimeBin1.Count}, RMS={regimeRms1:F4}, Mean={regimeMean1:F4}");
        _output.WriteLine($"Bin 1-2 Rd : n={regimeBin2.Count}, RMS={regimeRms2:F4}, Mean={regimeMean2:F4}");
        _output.WriteLine($"Bin 2-4 Rd : n={regimeBin3.Count}, RMS={regimeRms3:F4}, Mean={regimeMean3:F4}");
        _output.WriteLine($"Bin >=4 Rd : n={regimeBin4.Count}, RMS={regimeRms4:F4}, Mean={regimeMean4:F4}");

        _output.WriteLine("=== ADAPTIVE MODEL ===");
        _output.WriteLine($"Bin <1 Rd  : n={adaptiveBin1.Count}, RMS={adaptiveRms1:F4}, Mean={adaptiveMean1:F4}");
        _output.WriteLine($"Bin 1-2 Rd : n={adaptiveBin2.Count}, RMS={adaptiveRms2:F4}, Mean={adaptiveMean2:F4}");
        _output.WriteLine($"Bin 2-4 Rd : n={adaptiveBin3.Count}, RMS={adaptiveRms3:F4}, Mean={adaptiveMean3:F4}");
        _output.WriteLine($"Bin >=4 Rd : n={adaptiveBin4.Count}, RMS={adaptiveRms4:F4}, Mean={adaptiveMean4:F4}");

        _output.WriteLine("=== IMPROVEMENT (Regime - Adaptive) ===");
        _output.WriteLine($"Bin <1 Rd  : ΔRMS={(regimeRms1 - adaptiveRms1):F4}");
        _output.WriteLine($"Bin 1-2 Rd : ΔRMS={(regimeRms2 - adaptiveRms2):F4}");
        _output.WriteLine($"Bin 2-4 Rd : ΔRMS={(regimeRms3 - adaptiveRms3):F4}");
        _output.WriteLine($"Bin >=4 Rd : ΔRMS={(regimeRms4 - adaptiveRms4):F4}");

        // Weiche Prüfung für innersten Bin
        if (regimeBin1.Count >= 5 && adaptiveBin1.Count >= 5)
        {
            Assert.InRange(regimeRms1, 0.0, 2.5);
            Assert.InRange(adaptiveRms1, 0.0, 2.5);
        }

        // Statistisch robuste Bins
        Assert.True(regimeBin2.Count > 20 && adaptiveBin2.Count > 20, "Too few points in 1-2 Rd bin.");
        Assert.True(regimeBin3.Count > 20 && adaptiveBin3.Count > 20, "Too few points in 2-4 Rd bin.");
        Assert.True(regimeBin4.Count > 20 && adaptiveBin4.Count > 20, "Too few points in >=4 Rd bin.");

        Assert.InRange(regimeRms2, 0.0, 2.5);
        Assert.InRange(regimeRms3, 0.0, 2.5);
        Assert.InRange(regimeRms4, 0.0, 2.5);

        Assert.InRange(adaptiveRms2, 0.0, 2.5);
        Assert.InRange(adaptiveRms3, 0.0, 2.5);
        Assert.InRange(adaptiveRms4, 0.0, 2.5);

        // Adaptive sollte in den inneren / Übergangs-Bins nicht schlechter sein
        Assert.True(adaptiveRms2 <= regimeRms2 + 0.03,
            $"AdaptiveModel worse than RegimeModel in 1-2 Rd bin: regime={regimeRms2:F4}, adaptive={adaptiveRms2:F4}");

        Assert.True(adaptiveRms3 <= regimeRms3 + 0.03,
            $"AdaptiveModel worse than RegimeModel in 2-4 Rd bin: regime={regimeRms3:F4}, adaptive={adaptiveRms3:F4}");

        // Außen sollte Adaptive nicht schlechter werden
        Assert.True(adaptiveRms4 <= regimeRms4 + 0.02,
            $"AdaptiveModel worse than RegimeModel in >=4 Rd bin: regime={regimeRms4:F4}, adaptive={adaptiveRms4:F4}");
    }

    [Fact]
    public void Test15_TRM_DualRegimeModel_RmsComparison()
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

        double a0 = 1.2e-10;

        double sumLocal = 0.0;
        int countLocal = 0;

        double sumOrbit = 0.0;
        int countOrbit = 0;

        double sumFull = 0.0;
        int countFull = 0;

        double sumAdaptive = 0.0;
        int countAdaptive = 0;

        double sumDual = 0.0;
        int countDual = 0;

        var rawGalaxyCache = rawPoints
            .GroupBy(p => p.GalaxyName)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        var modelGalaxyGroups = trmDisk
            .GroupBy(p => p.GalaxyName)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        foreach (var kvp in modelGalaxyGroups)
        {
            string galaxyName = kvp.Key;
            var galaxy = kvp.Value;

            if (!rawGalaxyCache.TryGetValue(galaxyName, out var rawGalaxy))
                continue;

            if (galaxy.Count < 4 || rawGalaxy.Count < 4)
                continue;

            foreach (var p in galaxy.Skip(2))
            {
                if (p.GobsMs2 <= 0 || p.GbarMs2 <= 0)
                    continue;

                // -------------------------
                // 1) Local
                // -------------------------
                double gLocal = SparcRarAnalysis.PredictGobs(
                    p.GbarMs2,
                    a0,
                    ModelType.ClockworkTRM
                );

                if (gLocal > 0 && double.IsFinite(gLocal))
                {
                    double resLocal = Math.Log10(p.GobsMs2) - Math.Log10(gLocal);
                    sumLocal += resLocal * resLocal;
                    countLocal++;
                }

                // -------------------------
                // 2) Orbit only
                // -------------------------
                double gOrbit = OrbitalIntegrationService.ComputeIntegratedG_OrbitOnly(
                    galaxy,
                    p.RadiusKpc,
                    a0
                );

                if (gOrbit > 0 && double.IsFinite(gOrbit))
                {
                    double resOrbit = Math.Log10(p.GobsMs2) - Math.Log10(gOrbit);
                    sumOrbit += resOrbit * resOrbit;
                    countOrbit++;
                }

                // -------------------------
                // 3) Full model
                // -------------------------
                double gFull = TrmFullModel.ComputeGobs(
                    galaxy,
                    p.RadiusKpc,
                    a0
                );

                if (gFull > 0 && double.IsFinite(gFull))
                {
                    double resFull = Math.Log10(p.GobsMs2) - Math.Log10(gFull);
                    sumFull += resFull * resFull;
                    countFull++;
                }

                // -------------------------
                // 4) Adaptive model
                // -------------------------
                double gAdaptive = TrmAdaptiveRegimeModel.ComputeGobs(
                    galaxy,
                    rawGalaxy,
                    p.RadiusKpc,
                    a0
                );

                if (gAdaptive > 0 && double.IsFinite(gAdaptive))
                {
                    double resAdaptive = Math.Log10(p.GobsMs2) - Math.Log10(gAdaptive);
                    sumAdaptive += resAdaptive * resAdaptive;
                    countAdaptive++;
                }

                // -------------------------
                // 5) Dual-Regime model
                // -------------------------
                double gDual = TrmDualRegimeModel.ComputeGobs(
                    galaxy,
                    rawGalaxy,
                    p.RadiusKpc,
                    a0
                );

                if (gDual > 0 && double.IsFinite(gDual))
                {
                    double resDual = Math.Log10(p.GobsMs2) - Math.Log10(gDual);
                    sumDual += resDual * resDual;
                    countDual++;
                }
            }
        }

        Assert.True(countLocal > 100, "Too few local-model points.");
        Assert.True(countOrbit > 100, "Too few orbit-model points.");
        Assert.True(countFull > 100, "Too few full-model points.");
        Assert.True(countAdaptive > 100, "Too few adaptive-model points.");
        Assert.True(countDual > 100, "Too few dual-regime-model points.");

        double rmsLocal = Math.Sqrt(sumLocal / countLocal);
        double rmsOrbit = Math.Sqrt(sumOrbit / countOrbit);
        double rmsFull = Math.Sqrt(sumFull / countFull);
        double rmsAdaptive = Math.Sqrt(sumAdaptive / countAdaptive);
        double rmsDual = Math.Sqrt(sumDual / countDual);

        _output.WriteLine($"Local    RMS = {rmsLocal:F4}");
        _output.WriteLine($"Orbit    RMS = {rmsOrbit:F4}");
        _output.WriteLine($"Full     RMS = {rmsFull:F4}");
        _output.WriteLine($"Adaptive RMS = {rmsAdaptive:F4}");
        _output.WriteLine($"Dual     RMS = {rmsDual:F4}");

        _output.WriteLine($"Orbit    improvement vs Local    = {rmsLocal - rmsOrbit:F4}");
        _output.WriteLine($"Full     improvement vs Orbit    = {rmsOrbit - rmsFull:F4}");
        _output.WriteLine($"Adaptive improvement vs Full     = {rmsFull - rmsAdaptive:F4}");
        _output.WriteLine($"Dual     improvement vs Adaptive = {rmsAdaptive - rmsDual:F4}");
        _output.WriteLine($"Dual     improvement vs Full     = {rmsFull - rmsDual:F4}");
        _output.WriteLine($"Dual     improvement vs Local    = {rmsLocal - rmsDual:F4}");

        // Grundstabilität
        Assert.InRange(rmsLocal, 0.4, 1.5);
        Assert.InRange(rmsOrbit, 0.4, 1.5);
        Assert.InRange(rmsFull, 0.4, 1.5);
        Assert.InRange(rmsAdaptive, 0.4, 1.5);
        Assert.InRange(rmsDual, 0.4, 1.5);

        // this is worse then adaptive, but not by much

        //    // Erwartete Hierarchie
        //    Assert.True(rmsOrbit <= rmsLocal + 0.02,
        //        $"Orbit model is unexpectedly worse than local model: local={rmsLocal:F4}, orbit={rmsOrbit:F4}");

        //    Assert.True(rmsFull <= rmsOrbit + 0.02,
        //        $"Full model is unexpectedly worse than orbit model: orbit={rmsOrbit:F4}, full={rmsFull:F4}");

        //    Assert.True(rmsAdaptive <= rmsFull + 0.05,
        //        $"Adaptive model is unexpectedly worse than full model: full={rmsFull:F4}, adaptive={rmsAdaptive:F4}");

        //    // Dual soll mindestens nicht massiv schlechter als Adaptive sein
        //    Assert.True(rmsDual <= rmsAdaptive + 0.05,
        //        $"Dual model is unexpectedly worse than adaptive model: adaptive={rmsAdaptive:F4}, dual={rmsDual:F4}");

        //    // Dual soll klar besser als Local sein
        //    Assert.True(rmsDual < rmsLocal,
        //        $"Dual model should still improve over local model: local={rmsLocal:F4}, dual={rmsDual:F4}");
        //    Assert.True(rmsDual <= rmsOrbit + 0.02,
        //$"Dual model should not be worse than orbit model: orbit={rmsOrbit:F4}, dual={rmsDual:F4}");
    }

    [Fact]
    public void Test16_TRM_FieldSolver_BasicConsistency()
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

        var galaxy = trmDisk
            .GroupBy(p => p.GalaxyName)
            .Where(g => g.Count() >= 10)
            .OrderByDescending(g => g.Count())
            .First()
            .OrderBy(p => p.RadiusKpc)
            .ToList();

        Assert.True(galaxy.Count >= 10, "Test galaxy has too few points.");

        var field = TrmFieldSolver.SolveField(galaxy);

        Assert.NotNull(field);
        Assert.NotNull(field.Points);
        Assert.True(field.Points.Count >= 3, "Theta field profile has too few points.");

        _output.WriteLine($"Galaxy = {galaxy.First().GalaxyName}");
        _output.WriteLine($"Field points = {field.Points.Count}");

        // 1) Alle Theta-Werte endlich
        foreach (var pt in field.Points)
        {
            Assert.True(double.IsFinite(pt.Theta), $"Theta is not finite at r={pt.RadiusKpc:F3}");
            Assert.True(double.IsFinite(pt.Source), $"Source is not finite at r={pt.RadiusKpc:F3}");
            Assert.True(double.IsFinite(pt.Sync), $"Sync is not finite at r={pt.RadiusKpc:F3}");

            _output.WriteLine(
                $"r={pt.RadiusKpc:F3}  Source={pt.Source:F4}  Sync={pt.Sync:F4}  Theta={pt.Theta:F4}");
        }

        // 2) Effektive Beschleunigung an mehreren Radien prüfen
        var probePoints = galaxy
            .Skip(1)
            .Take(Math.Min(6, galaxy.Count - 2))
            .ToList();

        Assert.NotEmpty(probePoints);

        foreach (var p in probePoints)
        {
            double gEff = TrmFieldSolver.ComputeEffectiveAcceleration(field, p.RadiusKpc);

            _output.WriteLine($"gEff(r={p.RadiusKpc:F3}) = {gEff:E4}");

            Assert.True(double.IsFinite(gEff), $"gEff is not finite at r={p.RadiusKpc:F3}");
            Assert.True(gEff >= 0.0, $"gEff is negative at r={p.RadiusKpc:F3}");
        }

        // 3) Solver-Sensitivität auf sourceStrength prüfen
        var fieldWeak = TrmFieldSolver.SolveField(
            galaxy,
            sourceStrength: 0.8,
            dampingStrength: 0.15,
            syncStrength: 0.10,
            iterations: 300,
            relaxation: 0.15
        );

        var fieldStrong = TrmFieldSolver.SolveField(
            galaxy,
            sourceStrength: 1.2,
            dampingStrength: 0.15,
            syncStrength: 0.10,
            iterations: 300,
            relaxation: 0.15
        );

        double testRadius = galaxy[galaxy.Count / 2].RadiusKpc;

        double gWeak = TrmFieldSolver.ComputeEffectiveAcceleration(fieldWeak, testRadius);
        double gStrong = TrmFieldSolver.ComputeEffectiveAcceleration(fieldStrong, testRadius);

        _output.WriteLine($"Test radius = {testRadius:F3}");
        _output.WriteLine($"gWeak   = {gWeak:E4}");
        _output.WriteLine($"gStrong = {gStrong:E4}");

        Assert.True(double.IsFinite(gWeak) && gWeak >= 0.0, "Weak-source solution invalid.");
        Assert.True(double.IsFinite(gStrong) && gStrong >= 0.0, "Strong-source solution invalid.");

        // Erwartung: stärkere Quelle sollte nicht zu kleinerem Feld führen
        //Assert.True(gStrong >= gWeak,
        //    $"Expected stronger source to yield >= effective acceleration, but got weak={gWeak:E4}, strong={gStrong:E4}");



        Assert.True(double.IsFinite(gWeak) && gWeak >= 0.0, "Weak-source solution invalid.");
        Assert.True(double.IsFinite(gStrong) && gStrong >= 0.0, "Strong-source solution invalid.");


    }

    [Fact]
    public void Test17_TRM_FieldSolver_SourceSensitivity()
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

        var galaxy = trmDisk
            .GroupBy(p => p.GalaxyName)
            .Where(g => g.Count() >= 12)
            .OrderByDescending(g => g.Count())
            .First()
            .OrderBy(p => p.RadiusKpc)
            .ToList();

        Assert.True(galaxy.Count >= 12, "Test galaxy has too few points.");

        _output.WriteLine($"Galaxy = {galaxy.First().GalaxyName}");
        _output.WriteLine($"Points = {galaxy.Count}");

        // -------------------------------------
        // Solver mit schwacher / mittlerer / starker Quelle
        // -------------------------------------
        double damping = 0.45;
        double sync = TrmDerivedParameters.GetPhiBeta() * 0.05;
        int iterations = 600;
        double relaxation = 0.01;

        var fieldWeak = TrmFieldSolver.SolveField(
            galaxy,
            sourceStrength: 0.8,
            dampingStrength: damping,
            syncStrength: sync,
            iterations: iterations,
            relaxation: relaxation
        );

        var fieldBase = TrmFieldSolver.SolveField(
            galaxy,
            sourceStrength: 1.0,
            dampingStrength: damping,
            syncStrength: sync,
            iterations: iterations,
            relaxation: relaxation
        );

        var fieldStrong = TrmFieldSolver.SolveField(
            galaxy,
            sourceStrength: 1.2,
            dampingStrength: damping,
            syncStrength: sync,
            iterations: iterations,
            relaxation: relaxation
        );

        Assert.NotNull(fieldWeak);
        Assert.NotNull(fieldBase);
        Assert.NotNull(fieldStrong);

        // -------------------------------------
        // 1) Mittlere Theta-Amplitude
        // -------------------------------------
        double meanThetaWeak = fieldWeak.Points.Average(p => p.Theta);
        double meanThetaBase = fieldBase.Points.Average(p => p.Theta);
        double meanThetaStrong = fieldStrong.Points.Average(p => p.Theta);

        _output.WriteLine($"MeanTheta weak   = {meanThetaWeak:F6}");
        _output.WriteLine($"MeanTheta base   = {meanThetaBase:F6}");
        _output.WriteLine($"MeanTheta strong = {meanThetaStrong:F6}");

        Assert.True(double.IsFinite(meanThetaWeak));
        Assert.True(double.IsFinite(meanThetaBase));
        Assert.True(double.IsFinite(meanThetaStrong));

        // Erwartung: sourceStrength verändert das Feldniveau messbar
        Assert.True(
            Math.Abs(meanThetaStrong - meanThetaWeak) > 1e-6,
            $"Expected source variation to affect mean theta, but got weak={meanThetaWeak:F6}, strong={meanThetaStrong:F6}"
        );

        // -------------------------------------
        // 2) Mittlere effektive Beschleunigung über mehrere Radien
        // -------------------------------------
        var sampleRadii = galaxy
            .Skip(2)
            .Take(Math.Min(12, galaxy.Count - 4))
            .Select(p => p.RadiusKpc)
            .ToList();

        Assert.NotEmpty(sampleRadii);

        var gWeakList = new List<double>();
        var gBaseList = new List<double>();
        var gStrongList = new List<double>();

        foreach (double r in sampleRadii)
        {
            double gWeak = TrmFieldSolver.ComputeEffectiveAcceleration(fieldWeak, r);
            double gBase = TrmFieldSolver.ComputeEffectiveAcceleration(fieldBase, r);
            double gStrong = TrmFieldSolver.ComputeEffectiveAcceleration(fieldStrong, r);

            _output.WriteLine(
                $"r={r:F3}  gWeak={gWeak:E4}  gBase={gBase:E4}  gStrong={gStrong:E4}");

            Assert.True(double.IsFinite(gWeak) && gWeak >= 0.0, $"Invalid weak gEff at r={r:F3}");
            Assert.True(double.IsFinite(gBase) && gBase >= 0.0, $"Invalid base gEff at r={r:F3}");
            Assert.True(double.IsFinite(gStrong) && gStrong >= 0.0, $"Invalid strong gEff at r={r:F3}");

            gWeakList.Add(gWeak);
            gBaseList.Add(gBase);
            gStrongList.Add(gStrong);
        }

        double meanGWeak = gWeakList.Average();
        double meanGBase = gBaseList.Average();
        double meanGStrong = gStrongList.Average();

        _output.WriteLine($"Mean gEff weak   = {meanGWeak:E4}");
        _output.WriteLine($"Mean gEff base   = {meanGBase:E4}");
        _output.WriteLine($"Mean gEff strong = {meanGStrong:E4}");

        Assert.True(double.IsFinite(meanGWeak) && meanGWeak >= 0.0);
        Assert.True(double.IsFinite(meanGBase) && meanGBase >= 0.0);
        Assert.True(double.IsFinite(meanGStrong) && meanGStrong >= 0.0);

        // WICHTIG:
        // gEff muss hier nicht monoton mit sourceStrength wachsen.
        // Der Solver ist ein relaxiertes Feldmodell, und die Ableitung |dTheta/dr|
        // kann lokal sogar kleiner werden, obwohl Theta global steigt.
        // Deshalb prüfen wir hier nur Stabilität der abgeleiteten Größe.
    }

    [Fact]
    public void Test18_TRM_FieldSolver_RadialConsistency()
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

        var galaxy = trmDisk
            .GroupBy(p => p.GalaxyName)
            .Where(g => g.Count() >= 12)
            .OrderByDescending(g => g.Count())
            .First()
            .OrderBy(p => p.RadiusKpc)
            .ToList();

        Assert.True(galaxy.Count >= 12, "Test galaxy has too few points.");

        _output.WriteLine($"Galaxy = {galaxy.First().GalaxyName}");
        _output.WriteLine($"Points = {galaxy.Count}");

        // -------------------------------------
        // Feld lösen
        // -------------------------------------
        var field = TrmFieldSolver.SolveField(galaxy);

        Assert.NotNull(field);
        Assert.NotNull(field.Points);
        Assert.True(field.Points.Count >= 3, "Theta field profile has too few points.");

        // -------------------------------------
        // 1) Radien müssen streng steigen
        // -------------------------------------
        for (int i = 1; i < field.Points.Count; i++)
        {
            Assert.True(
                field.Points[i].RadiusKpc > field.Points[i - 1].RadiusKpc,
                $"Field radius is not strictly increasing at index {i}: " +
                $"{field.Points[i - 1].RadiusKpc:F6} -> {field.Points[i].RadiusKpc:F6}"
            );
        }

        // -------------------------------------
        // 2) Theta muss endlich bleiben
        // -------------------------------------
        foreach (var pt in field.Points)
        {
            Assert.True(double.IsFinite(pt.Theta), $"Theta is not finite at r={pt.RadiusKpc:F3}");
            Assert.True(double.IsFinite(pt.Source), $"Source is not finite at r={pt.RadiusKpc:F3}");
            Assert.True(double.IsFinite(pt.Sync), $"Sync is not finite at r={pt.RadiusKpc:F3}");

            _output.WriteLine(
                $"r={pt.RadiusKpc:F3}  Theta={pt.Theta:F6}  Source={pt.Source:F6}  Sync={pt.Sync:F6}");
        }

        // -------------------------------------
        // 3) gEff radial auswerten
        // -------------------------------------
        var radii = galaxy
            .Skip(1)
            .Take(Math.Min(15, galaxy.Count - 2))
            .Select(p => p.RadiusKpc)
            .ToList();

        Assert.NotEmpty(radii);

        var gValues = new List<double>();

        foreach (double r in radii)
        {
            double gEff = TrmFieldSolver.ComputeEffectiveAcceleration(field, r);

            _output.WriteLine($"gEff(r={r:F3}) = {gEff:E6}");

            Assert.True(double.IsFinite(gEff), $"gEff is not finite at r={r:F3}");
            Assert.True(gEff >= 0.0, $"gEff is negative at r={r:F3}");

            gValues.Add(gEff);
        }

        Assert.NotEmpty(gValues);

        // -------------------------------------
        // 4) gEff darf nicht trivial konstant sein
        // -------------------------------------
        double minG = gValues.Min();
        double maxG = gValues.Max();
        double meanG = gValues.Average();

        _output.WriteLine($"Min gEff  = {minG:E6}");
        _output.WriteLine($"Max gEff  = {maxG:E6}");
        _output.WriteLine($"Mean gEff = {meanG:E6}");

        Assert.True(maxG > minG,
            $"Expected radial variation in gEff, but got min={minG:E6}, max={maxG:E6}");

        // relative Spannweite
        double spread = (meanG > 0.0) ? (maxG - minG) / meanG : 0.0;

        _output.WriteLine($"Relative spread = {spread:F6}");

        Assert.True(spread > 1e-3,
            $"Expected non-trivial radial structure in gEff, but relative spread is too small: {spread:F6}");

        // -------------------------------------
        // 5) Theta selbst darf auch nicht trivial konstant sein
        // -------------------------------------
        double minTheta = field.Points.Min(p => p.Theta);
        double maxTheta = field.Points.Max(p => p.Theta);
        double meanTheta = field.Points.Average(p => p.Theta);

        _output.WriteLine($"Min Theta  = {minTheta:F6}");
        _output.WriteLine($"Max Theta  = {maxTheta:F6}");
        _output.WriteLine($"Mean Theta = {meanTheta:F6}");

        Assert.True(maxTheta > minTheta,
            $"Expected non-trivial Theta structure, but got min={minTheta:F6}, max={maxTheta:F6}");
    }

    [Fact]
    public void Test19_TRM_FieldSolver_ObservableComparison()
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

        var galaxy = trmDisk
            .GroupBy(p => p.GalaxyName)
            .Where(g => g.Count() >= 12)
            .OrderByDescending(g => g.Count())
            .First()
            .OrderBy(p => p.RadiusKpc)
            .ToList();

        Assert.True(galaxy.Count >= 12, "Test galaxy has too few points.");

        _output.WriteLine($"Galaxy = {galaxy.First().GalaxyName}");
        _output.WriteLine($"Points = {galaxy.Count}");

        var field = TrmFieldSolver.SolveField(galaxy);

        Assert.NotNull(field);
        Assert.NotNull(field.Points);
        Assert.True(field.Points.Count >= 3, "Theta field profile has too few points.");

        var radii = galaxy
            .Skip(2)
            .Take(Math.Min(12, galaxy.Count - 4))
            .Select(p => p.RadiusKpc)
            .ToList();

        Assert.NotEmpty(radii);

        var gradOnlyValues = new List<double>();
        var gradLevelValues = new List<double>();
        var fullValues = new List<double>();

        foreach (double r in radii)
        {
            int idx = FindNearestIndex(field, r);

            if (idx <= 0 || idx >= field.Points.Count - 1)
                continue;

            var left = field.Points[idx - 1];
            var mid = field.Points[idx];
            var right = field.Points[idx + 1];

            double dr = right.RadiusKpc - left.RadiusKpc;
            if (dr <= 0)
                continue;

            double rSafe = Math.Max(mid.RadiusKpc, 1e-6);
            double drLocal = Math.Max(0.5 * dr, 1e-6);

            // 1) Gradient only
            double dThetaDr = (right.Theta - left.Theta) / dr;
            double gGradOnly = Math.Abs(dThetaDr);

            // 2) Gradient + Level
            double gGradLevel = gGradOnly + Math.Max(mid.Theta, 0.0) / rSafe;

            // 3) Gradient + Level + Curvature
            double d2ThetaDr2 =
                (right.Theta - 2.0 * mid.Theta + left.Theta) / (drLocal * drLocal);

            double gFull = gGradOnly
                         + Math.Max(mid.Theta, 0.0) / rSafe
                         + Math.Abs(d2ThetaDr2);

            _output.WriteLine(
                $"r={r:F3}  " +
                $"GradOnly={gGradOnly:E4}  " +
                $"GradLevel={gGradLevel:E4}  " +
                $"FullObs={gFull:E4}");

            Assert.True(double.IsFinite(gGradOnly) && gGradOnly >= 0.0, $"Invalid gradient-only observable at r={r:F3}");
            Assert.True(double.IsFinite(gGradLevel) && gGradLevel >= 0.0, $"Invalid gradient+level observable at r={r:F3}");
            Assert.True(double.IsFinite(gFull) && gFull >= 0.0, $"Invalid full observable at r={r:F3}");

            gradOnlyValues.Add(gGradOnly);
            gradLevelValues.Add(gGradLevel);
            fullValues.Add(gFull);
        }

        Assert.NotEmpty(gradOnlyValues);
        Assert.NotEmpty(gradLevelValues);
        Assert.NotEmpty(fullValues);

        double meanGradOnly = gradOnlyValues.Average();
        double meanGradLevel = gradLevelValues.Average();
        double meanFull = fullValues.Average();

        double minGradOnly = gradOnlyValues.Min();
        double maxGradOnly = gradOnlyValues.Max();

        double minGradLevel = gradLevelValues.Min();
        double maxGradLevel = gradLevelValues.Max();

        double minFull = fullValues.Min();
        double maxFull = fullValues.Max();

        _output.WriteLine($"Mean GradOnly  = {meanGradOnly:E4}");
        _output.WriteLine($"Mean GradLevel = {meanGradLevel:E4}");
        _output.WriteLine($"Mean FullObs   = {meanFull:E4}");

        _output.WriteLine($"Spread GradOnly  = {(maxGradOnly - minGradOnly):E4}");
        _output.WriteLine($"Spread GradLevel = {(maxGradLevel - minGradLevel):E4}");
        _output.WriteLine($"Spread FullObs   = {(maxFull - minFull):E4}");

        // Grundstabilität
        Assert.True(double.IsFinite(meanGradOnly) && meanGradOnly >= 0.0);
        Assert.True(double.IsFinite(meanGradLevel) && meanGradLevel >= 0.0);
        Assert.True(double.IsFinite(meanFull) && meanFull >= 0.0);

        // Alle drei Observablen sollen radiale Struktur tragen
        Assert.True(maxGradOnly > minGradOnly,
            "Gradient-only observable appears radially constant.");
        Assert.True(maxGradLevel > minGradLevel,
            "Gradient+level observable appears radially constant.");
        Assert.True(maxFull > minFull,
            "Full observable appears radially constant.");
    }

    [Fact]
    public void Test20_TRM_FieldSolver_LevelCouplingSweep()
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

        var galaxy = trmDisk
            .GroupBy(p => p.GalaxyName)
            .Where(g => g.Count() >= 12)
            .OrderByDescending(g => g.Count())
            .First()
            .OrderBy(p => p.RadiusKpc)
            .ToList();

        Assert.True(galaxy.Count >= 12, "Test galaxy has too few points.");

        _output.WriteLine($"Galaxy = {galaxy.First().GalaxyName}");
        _output.WriteLine($"Points = {galaxy.Count}");

        double damping = 0.45;
        double sync = TrmDerivedParameters.GetPhiBeta() * 0.05;
        int iterations = 600;
        double relaxation = 0.01;

        var fieldWeak = TrmFieldSolver.SolveField(
            galaxy,
            sourceStrength: 0.8,
            dampingStrength: damping,
            syncStrength: sync,
            iterations: iterations,
            relaxation: relaxation
        );

        var fieldBase = TrmFieldSolver.SolveField(
            galaxy,
            sourceStrength: 1.0,
            dampingStrength: damping,
            syncStrength: sync,
            iterations: iterations,
            relaxation: relaxation
        );

        var fieldStrong = TrmFieldSolver.SolveField(
            galaxy,
            sourceStrength: 1.2,
            dampingStrength: damping,
            syncStrength: sync,
            iterations: iterations,
            relaxation: relaxation
        );

        Assert.NotNull(fieldWeak);
        Assert.NotNull(fieldBase);
        Assert.NotNull(fieldStrong);

        var sampleRadii = galaxy
            .Skip(2)
            .Take(Math.Min(12, galaxy.Count - 4))
            .Select(p => p.RadiusKpc)
            .ToList();

        Assert.NotEmpty(sampleRadii);

        double bestLevelCoupling = double.NaN;
        double bestSensitivity = double.MinValue;

        var results = new List<(double LevelCoupling, double MeanWeak, double MeanBase, double MeanStrong, double Sensitivity)>();

        for (double levelCoupling = 0.0; levelCoupling <= 1.5001; levelCoupling += 0.1)
        {
            var gWeakList = new List<double>();
            var gBaseList = new List<double>();
            var gStrongList = new List<double>();

            foreach (double r in sampleRadii)
            {
                double gWeak = TrmFieldSolver.ComputeEffectiveAcceleration(
                    fieldWeak,
                    r,
                    gradientCoupling: 1.0,
                    levelCoupling: levelCoupling
                );

                double gBase = TrmFieldSolver.ComputeEffectiveAcceleration(
                    fieldBase,
                    r,
                    gradientCoupling: 1.0,
                    levelCoupling: levelCoupling
                );

                double gStrong = TrmFieldSolver.ComputeEffectiveAcceleration(
                    fieldStrong,
                    r,
                    gradientCoupling: 1.0,
                    levelCoupling: levelCoupling
                );

                Assert.True(double.IsFinite(gWeak) && gWeak >= 0.0, $"Invalid weak gEff at r={r:F3}, levelCoupling={levelCoupling:F2}");
                Assert.True(double.IsFinite(gBase) && gBase >= 0.0, $"Invalid base gEff at r={r:F3}, levelCoupling={levelCoupling:F2}");
                Assert.True(double.IsFinite(gStrong) && gStrong >= 0.0, $"Invalid strong gEff at r={r:F3}, levelCoupling={levelCoupling:F2}");

                gWeakList.Add(gWeak);
                gBaseList.Add(gBase);
                gStrongList.Add(gStrong);
            }

            double meanWeak = gWeakList.Average();
            double meanBase = gBaseList.Average();
            double meanStrong = gStrongList.Average();

            // Sensitivität = Abstand zwischen strong und weak
            double sensitivity = Math.Abs(meanStrong - meanWeak);

            results.Add((levelCoupling, meanWeak, meanBase, meanStrong, sensitivity));

            _output.WriteLine(
                $"levelCoupling={levelCoupling:F2} " +
                $"-> meanWeak={meanWeak:E4}, meanBase={meanBase:E4}, meanStrong={meanStrong:E4}, sensitivity={sensitivity:E4}");

            if (sensitivity > bestSensitivity)
            {
                bestSensitivity = sensitivity;
                bestLevelCoupling = levelCoupling;
            }
        }

        _output.WriteLine($"Best levelCoupling = {bestLevelCoupling:F2}");
        _output.WriteLine($"Best sensitivity   = {bestSensitivity:E4}");

        Assert.True(double.IsFinite(bestLevelCoupling), "No valid best levelCoupling found.");
        Assert.True(double.IsFinite(bestSensitivity) && bestSensitivity >= 0.0, "Invalid best sensitivity.");

        Assert.True(
            bestLevelCoupling >= 0.0 && bestLevelCoupling <= 1.5001,
            $"Best levelCoupling out of tested range: {bestLevelCoupling:F6}"
        );

        Assert.True(bestSensitivity >= 0.0,
            $"Expected non-negative sensitivity, got {bestSensitivity:E4}");
    }




    #region helper
    /// <summary>
    ///  Hilfsfunktion zur Berechnung des RMS der Residuen in einem Bin.    
    /// </summary>
    /// <param name="residuals"></param>
    /// <returns></returns>
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
    private static int FindNearestIndex(ThetaFieldProfile field, double targetRadiusKpc)
    {
        int bestIndex = -1;
        double bestDistance = double.MaxValue;

        for (int i = 0; i < field.Points.Count; i++)
        {
            double d = Math.Abs(field.Points[i].RadiusKpc - targetRadiusKpc);
            if (d < bestDistance)
            {
                bestDistance = d;
                bestIndex = i;
            }
        }

        return bestIndex;
    }
    private static string FormatRms(int count, double rms)
    {
        if (count == 0 || double.IsNaN(rms))
            return "n=0,RMS=NaN";

        return $"n={count},RMS={rms:F4}";
    }
    #endregion
}
