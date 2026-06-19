using System;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using TRM.Core;

namespace TRM.Tests;

public class RarRelationTests
{
    private readonly ITestOutputHelper _output;

    public RarRelationTests(ITestOutputHelper output)
    {
        _output = output;
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
        var bins = SparcRarAnalysis1.ComputeRarProfiles(rarData);

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
}
