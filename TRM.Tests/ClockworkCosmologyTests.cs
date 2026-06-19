using System;
using TRM.Core;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests;

public class ClockworkCosmologyTests
{
    private readonly ITestOutputHelper _output;
    public ClockworkCosmologyTests(ITestOutputHelper output)
    {
        _output = output;
    }
    [Fact]
    public void Test_CMB_Peak_Alignment_With_Planck()
    {
        var solver = new CmbAcousticSolver();

        // Act: run the k-space sweep
        var result = solver.FindPerfectPhysicalParameters();

        _output.WriteLine("--- TRM COSMOLOGICAL CMB FIT (k-space) ---");
        _output.WriteLine($"Isolated TRM drive frequency:         {result.TrmDriveFreq:F3}");
        _output.WriteLine($"Kinetic Doppler weight:               {result.DopplerWeight:F3}");
        _output.WriteLine($"Calculated angular diameter distance: {result.BestDa:F2} Mpc");
        _output.WriteLine($"Calculated 1st acoustic peak (\u2113):  {result.Peak1:F1}");
        _output.WriteLine($"Calculated 2nd acoustic peak (\u2113):  {result.Peak2:F1}");
        _output.WriteLine($"Deviation standard error (fitness):   {result.Fitness:F4}");

        // Assert: expect tight peak alignment
        Assert.InRange(result.Peak1, 218.0, 222.0);
        Assert.InRange(result.Peak2, 538.0, 542.0);
        Assert.True(result.Fitness < 10.0, $"Fitness error is too high: {result.Fitness}");
    }
    [Fact]
    public void Test_DarkEnergy_Replacement_Pantheon()
    {
        // Arrange
        var solver = new PantheonTrmSolver();

        // UPDATE: Now looking for the official Pantheon+ master file
        var dataPath = Path.Combine(AppContext.BaseDirectory, "Data", "Pantheon+SH0ES.dat");

        if (!File.Exists(dataPath))
        {
            _output.WriteLine($"[SKIPPED] Data file not found at: {dataPath}");
            return;
        }

        var snData = solver.LoadPantheonData(dataPath);

        // Act
        var result = solver.FindDarkEnergyReplacement(snData);

        _output.WriteLine("--- TRM DARK ENERGY REPLACEMENT (PANTHEON) ---");
        _output.WriteLine($"Analyzed Supernovae:              {result.AnalyzedPoints}");
        _output.WriteLine($"TRM Base Temporal Pacing (H_T):   {result.BestHt:F3} km/s/Mpc");
        _output.WriteLine($"TRM Drift Coefficient (\u03B2_T):       {result.BestBetaTrm:F4}");
        _output.WriteLine($"Deviation Error (RMS):            {result.RmsError:F4} dex");

        // Assert: The foundational pacing perfectly matches the SH0ES empirical measurement (~73.0)
        Assert.InRange(result.BestHt, 71.0, 75.0);

        // Assert: The temporal matrix must exhibit a NEGATIVE drift (damping) to emulate Lambda
        Assert.True(result.BestBetaTrm < 0.0, "The temporal matrix must exhibit a negative damping drift (-0.284) to replace Dark Energy.");

        // Assert: The RMS error must show a high-quality fit
        Assert.True(result.RmsError < 1.0, "The fit deviation is too high.");
    }
}