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

        // Act: Der k-Raum Sweep feuert
        var result = solver.FindPerfectPhysicalParameters();

        _output.WriteLine("--- TRM KOSMOLOGISCHER CMB-FIT (k-Raum) ---");
        _output.WriteLine($"Isolierte TRM-Taktungsfrequenz:      {result.TrmDriveFreq:F3}");
        _output.WriteLine($"Kinetisches Doppler-Gewicht:         {result.DopplerWeight:F3}");
        _output.WriteLine($"Errechnete Winkeldurchmesserdistanz: {result.BestDa:F2} Mpc");
        _output.WriteLine($"Berechneter 1. Akustischer Peak (\u2113): {result.Peak1:F1}");
        _output.WriteLine($"Berechneter 2. Akustischer Peak (\u2113): {result.Peak2:F1}");
        _output.WriteLine($"Abweichungs-Standardfehler (Fitness):{result.Fitness:F4}");

        // Assert: Absolute Punktlandung erwartet
        Assert.InRange(result.Peak1, 218.0, 222.0);
        Assert.InRange(result.Peak2, 538.0, 542.0);
        Assert.True(result.Fitness < 10.0, $"Fitness-Fehler zu hoch: {result.Fitness}");
    }
    
}