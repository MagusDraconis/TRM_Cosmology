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
    
}