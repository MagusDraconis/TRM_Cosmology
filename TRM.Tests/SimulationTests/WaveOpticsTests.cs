using System;
using System.Collections.Generic;
using System.Text;
using TRM.QuantumCore.Planck;
using TRM.Simulations.Experiments.WaveOptics;
using Xunit.Abstractions;

namespace TRM.Tests.SimulationTests;

public class WaveOpticsTests
{
    private readonly ITestOutputHelper _output;
    public WaveOpticsTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Wavefront_Should_Not_Deflect_Without_Mass()
    {
        var tracer = new WavefrontTracer();

        double angle = tracer.Simulate(0.0, 6.9634e8);
        _output.WriteLine($"Sim angle     : {angle:E} rad");
        Assert.True(Math.Abs(angle) < 1e-6);
    }

    [Fact]
    public void Wavefront_Should_Deflect_Near_Mass()
    {
        var tracer = new WavefrontTracer();

        double M = PhysicalConstantsSI.M_Solar;
        double angle = tracer.Simulate(M, 6.9634e8);
        _output.WriteLine($"Sim angle     : {angle:E} rad");

        Assert.True(Math.Abs(angle) > 1e-9);
    }

    [Fact]
    public void Wavefront_Should_Reproduce_Newton_Level_Deflection()
    {
        var tracer = new WavefrontTracer();

        double G = PhysicalConstantsSI.G;
        double c = PhysicalConstantsSI.c;
        double M = PhysicalConstantsSI.M_Solar;
        double b = 6.9634e8;

        double angle = tracer.Simulate(M, b);

        double alphaGR = 4 * G * M / (c * c * b);
        double alphaNewtonLevel = 0.5 * alphaGR;

        double relError = Math.Abs(Math.Abs(angle) - alphaNewtonLevel) / alphaNewtonLevel;

        _output.WriteLine("=== Newton-Level Deflection Test ===");
        _output.WriteLine($"Sim angle         : {angle:E} rad");
        _output.WriteLine($"|Sim angle|       : {Math.Abs(angle):E} rad");
        _output.WriteLine($"GR reference      : {alphaGR:E} rad");
        _output.WriteLine($"Newton reference  : {alphaNewtonLevel:E} rad");
        _output.WriteLine($"Ratio |Sim|/GR    : {(Math.Abs(angle) / alphaGR):F6}");
        _output.WriteLine($"Relative error    : {relError:P6}");

        Assert.True(relError < 0.1);
    }
    //[Fact]
    //public void SpatialCurvatureLikeGR_Should_Approximate_GR_Deflection()
    //{
    //    var tracer = new WavefrontTracer();

    //    double M = PhysicalConstantsSI.M_Solar;
    //    double b = 6.9634e8;

    //    double angle = tracer.SimulateSpatialCurvatureLikeGR(M, b);

    //    double alphaGR = 4 * PhysicalConstantsSI.G * M / (PhysicalConstantsSI.c * PhysicalConstantsSI.c * b);
    //    double relError = Math.Abs(Math.Abs(angle) - alphaGR) / alphaGR;

    //    _output.WriteLine("=== Spatial Curvature Like GR Test ===");
    //    _output.WriteLine($"Sim angle      : {angle:E} rad");
    //    _output.WriteLine($"|Sim angle|    : {Math.Abs(angle):E} rad");
    //    _output.WriteLine($"GR reference   : {alphaGR:E} rad");
    //    _output.WriteLine($"Ratio |Sim|/GR : {(Math.Abs(angle) / alphaGR):F6}");
    //    _output.WriteLine($"Relative error : {relError:P6}");

    //    Assert.True(Math.Abs(angle) < 0.01, "Angle exploded");
    //    Assert.True(relError < 0.1);
    //}
    [Fact]
    public void SpatialCurvatureLikeGR_Should_Show_Convergence_With_Step_Size()
    {
        var tracer = new WavefrontTracer();

        double G = PhysicalConstantsSI.G;
        double c = PhysicalConstantsSI.c;
        double M = PhysicalConstantsSI.M_Solar;
        double b = 6.9634e8;

        double alphaGR = 4.0 * G * M / (c * c * b);

        double[] stepSizes =
        {
        1e7,   // 10,000 km
        5e6,   // 5,000 km
        2e6,   // 2,000 km
        1e6,   // 1,000 km
        5e5    // 500 km
    };

        double previousError = double.MaxValue;

        _output.WriteLine("=== Convergence Test: Spatial Curvature Like GR ===");
        _output.WriteLine($"GR reference : {alphaGR:E} rad");
        _output.WriteLine("");

        foreach (double ds in stepSizes)
        {
            double angle = tracer.SimulateSpatialCurvatureLikeGR(M, b, ds);
            double absAngle = Math.Abs(angle);
            double relError = Math.Abs(absAngle - alphaGR) / alphaGR;
            double ratio = absAngle / alphaGR;

            _output.WriteLine($"ds              : {ds:E} m");
            _output.WriteLine($"Sim angle       : {angle:E} rad");
            _output.WriteLine($"|Sim angle|     : {absAngle:E} rad");
            _output.WriteLine($"Ratio |Sim|/GR  : {ratio:F6}");
            _output.WriteLine($"Relative error  : {relError:P6}");
            _output.WriteLine("");

            // Basis-Sicherheitscheck
            Assert.True(absAngle < 0.01, $"Angle exploded for ds={ds:E}");

            // optional: monotone Verbesserung grob prüfen
            Assert.True(relError <= previousError + 1e-4,
                $"Convergence worsened unexpectedly at ds={ds:E}");

            previousError = relError;
        }
    }
    [Fact]
    public void WaveOnly_Should_Be_Bounded_Relative_To_GR_Deflection()
    {
        var tracer = new WavefrontTracer();

        double G = PhysicalConstantsSI.G;
        double c = PhysicalConstantsSI.c;
        double M = PhysicalConstantsSI.M_Solar;
        double b = 6.9634e8;

        double waveAngle = tracer.SimulateWaveOnly(M, b);
        double alphaGR = 4.0 * G * M / (c * c * b);

        double relativeWaveContribution = Math.Abs(waveAngle) / alphaGR;

        _output.WriteLine("=== Wave Bound Test ===");
        _output.WriteLine($"Wave angle              : {waveAngle:E} rad");
        _output.WriteLine($"|Wave angle|            : {Math.Abs(waveAngle):E} rad");
        _output.WriteLine($"GR reference            : {alphaGR:E} rad");
        _output.WriteLine($"Wave / GR               : {relativeWaveContribution:E}");
        _output.WriteLine($"Wave contribution (%)   : {(relativeWaveContribution * 100.0):F9}");

        Assert.True(Math.Abs(waveAngle) < 1e-2, "Wave angle exploded unexpectedly");
    }

    [Fact]
    public void SpatialCurvatureLikeGR_Should_Show_Convergence_With_Integration_Range()
    {
        var tracer = new WavefrontTracer();

        double G = PhysicalConstantsSI.G;
        double c = PhysicalConstantsSI.c;
        double M = PhysicalConstantsSI.M_Solar;
        double b = 6.9634e8;

        double alphaGR = 4.0 * G * M / (c * c * b);

        // ds fix lassen, damit wir nur den Bereich testen
        double ds = 1e6;

        double[] ranges =
        {
        1e10,
        2e10,
        5e10,
        1e11
    };

        _output.WriteLine("=== Convergence Test: Integration Range ===");
        _output.WriteLine($"GR reference : {alphaGR:E} rad");
        _output.WriteLine($"ds           : {ds:E} m");
        _output.WriteLine("");

        double previousError = double.MaxValue;

        foreach (double L in ranges)
        {
            double angle = tracer.SimulateSpatialCurvatureLikeGR(M, b, ds, -L, +L);
            double absAngle = Math.Abs(angle);
            double relError = Math.Abs(absAngle - alphaGR) / alphaGR;
            double ratio = absAngle / alphaGR;

            _output.WriteLine($"Range [-L,+L]     : [-{L:E}, +{L:E}] m");
            _output.WriteLine($"Sim angle         : {angle:E} rad");
            _output.WriteLine($"|Sim angle|       : {absAngle:E} rad");
            _output.WriteLine($"Ratio |Sim|/GR    : {ratio:F9}");
            _output.WriteLine($"Relative error    : {relError:P9}");
            _output.WriteLine("");

            Assert.True(absAngle < 0.01, $"Angle exploded for range L={L:E}");

            // optional: Fehler sollte nicht schlechter werden
            Assert.True(relError <= previousError + 1e-6,
                $"Convergence worsened unexpectedly at L={L:E}");

            previousError = relError;
        }
    }

    [Fact]
    public void SpatialCurvatureLikeGR_Should_Scale_Linearly_With_Mass()
    {
        var tracer = new WavefrontTracer();

        double M1 = PhysicalConstantsSI.M_Solar;
        double M2 = 2.0 * M1;
        double b = 6.9634e8;
        double ds = 1e6;
        double L = 1e11;

        double angle1 = Math.Abs(tracer.SimulateSpatialCurvatureLikeGR(M1, b, ds, -L, +L));
        double angle2 = Math.Abs(tracer.SimulateSpatialCurvatureLikeGR(M2, b, ds, -L, +L));

        double ratio = angle2 / angle1;
        double expected = 2.0;
        double relError = Math.Abs(ratio - expected) / expected;

        _output.WriteLine("=== Mass Scaling Test ===");
        _output.WriteLine($"M1                 : {M1:E} kg");
        _output.WriteLine($"M2                 : {M2:E} kg");
        _output.WriteLine($"Angle(M1)          : {angle1:E} rad");
        _output.WriteLine($"Angle(M2)          : {angle2:E} rad");
        _output.WriteLine($"Ratio              : {ratio:F9}");
        _output.WriteLine($"Expected ratio     : {expected:F9}");
        _output.WriteLine($"Relative error     : {relError:P9}");

        Assert.True(relError < 0.01);
    }
    [Fact]
    public void SpatialCurvatureLikeGR_Should_Scale_Inversely_With_Impact_Parameter()
    {
        var tracer = new WavefrontTracer();

        double M = PhysicalConstantsSI.M_Solar;
        double b1 = 6.9634e8;
        double b2 = 2.0 * b1;
        double ds = 1e6;
        double L = 1e11;

        double angle1 = Math.Abs(tracer.SimulateSpatialCurvatureLikeGR(M, b1, ds, -L, +L));
        double angle2 = Math.Abs(tracer.SimulateSpatialCurvatureLikeGR(M, b2, ds, -L, +L));

        double ratio = angle2 / angle1;
        double expected = 0.5;
        double relError = Math.Abs(ratio - expected) / expected;

        _output.WriteLine("=== Impact Parameter Scaling Test ===");
        _output.WriteLine($"b1                 : {b1:E} m");
        _output.WriteLine($"b2                 : {b2:E} m");
        _output.WriteLine($"Angle(b1)          : {angle1:E} rad");
        _output.WriteLine($"Angle(b2)          : {angle2:E} rad");
        _output.WriteLine($"Ratio              : {ratio:F9}");
        _output.WriteLine($"Expected ratio     : {expected:F9}");
        _output.WriteLine($"Relative error     : {relError:P9}");

        Assert.True(relError < 0.01);
    }

    [Fact]
    public void SpatialCurvatureLikeGR_Should_Follow_M_Over_b_Scaling()
    {
        var tracer = new WavefrontTracer();

        double M1 = PhysicalConstantsSI.M_Solar;
        double M2 = 2.0 * M1;

        double b1 = 6.9634e8;
        double b2 = 2.0 * b1;

        double ds = 1e6;
        double L = 1e11;

        double angle1 = Math.Abs(tracer.SimulateSpatialCurvatureLikeGR(M1, b1, ds, -L, +L));
        double angle2 = Math.Abs(tracer.SimulateSpatialCurvatureLikeGR(M2, b2, ds, -L, +L));

        // Erwartung:
        // alpha ~ M / b
        // also hier: (2*M1)/(2*b1) = M1/b1 => gleicher Winkel
        double ratio = angle2 / angle1;
        double expected = 1.0;
        double relError = Math.Abs(ratio - expected) / expected;

        _output.WriteLine("=== M over b Scaling Test ===");
        _output.WriteLine($"M1                 : {M1:E} kg");
        _output.WriteLine($"M2                 : {M2:E} kg");
        _output.WriteLine($"b1                 : {b1:E} m");
        _output.WriteLine($"b2                 : {b2:E} m");
        _output.WriteLine($"Angle(M1,b1)       : {angle1:E} rad");
        _output.WriteLine($"Angle(M2,b2)       : {angle2:E} rad");
        _output.WriteLine($"Ratio              : {ratio:F9}");
        _output.WriteLine($"Expected ratio     : {expected:F9}");
        _output.WriteLine($"Relative error     : {relError:P9}");

        Assert.True(relError < 0.01);
    }

    [Fact]
    public void SpatialCurvatureLikeGR_Should_Be_Symmetric_For_Positive_And_Negative_Impact_Parameter()
    {
        var tracer = new WavefrontTracer();

        double M = PhysicalConstantsSI.M_Solar;
        double b = 6.9634e8;
        double ds = 1e6;
        double L = 1e11;

        double anglePos = tracer.SimulateSpatialCurvatureLikeGR(M, +b, ds, -L, +L);
        double angleNeg = tracer.SimulateSpatialCurvatureLikeGR(M, -b, ds, -L, +L);

        double absPos = Math.Abs(anglePos);
        double absNeg = Math.Abs(angleNeg);

        double ratio = absNeg / absPos;
        double relError = Math.Abs(ratio - 1.0);

        _output.WriteLine("=== Symmetry Test (+b / -b) ===");
        _output.WriteLine($"Angle(+b)          : {anglePos:E} rad");
        _output.WriteLine($"Angle(-b)          : {angleNeg:E} rad");
        _output.WriteLine($"|Angle(+b)|        : {absPos:E} rad");
        _output.WriteLine($"|Angle(-b)|        : {absNeg:E} rad");
        _output.WriteLine($"Magnitude ratio    : {ratio:F9}");
        _output.WriteLine($"Abs difference     : {Math.Abs(absPos - absNeg):E}");
        _output.WriteLine($"Direction check    : {(Math.Sign(anglePos) != Math.Sign(angleNeg))}");

        Assert.True(Math.Sign(anglePos) != Math.Sign(angleNeg), "Expected opposite signs for +b and -b");
        Assert.True(relError < 0.01, "Expected equal magnitudes for +b and -b");
    }
    [Trait("Category", "LongRunning")]
    [Fact]
    public void TRMBaseline_Should_Be_Consistently_Half_Of_SpatialGR_Across_Multiple_Mass_And_Impact_Parameter_Pairs()
    {
        var tracer = new WavefrontTracer();

        double M1 = PhysicalConstantsSI.M_Solar;
        double M2 = 2.0 * M1;

        double b1 = 6.9634e8;
        double b2 = 2.0 * b1;

        double ds = 1e6;
        double L = 1e11;

        var testCases = new[]
        {
        new { Name = "Case 1", M = M1, b = b1 },
        new { Name = "Case 2", M = M2, b = b1 },
        new { Name = "Case 3", M = M1, b = b2 },
        new { Name = "Case 4", M = M2, b = b2 }
    };

        _output.WriteLine("=== TRM Baseline vs Spatial-GR Comparison ===");
        _output.WriteLine("");

        foreach (var tc in testCases)
        {
            // TRM-/Baseline-Methode
            double trmAngle = Math.Abs(tracer.Simulate(tc.M, tc.b));

            // Spatial-GR-Referenz
            double grAngle = Math.Abs(tracer.SimulateSpatialCurvatureLikeGR(tc.M, tc.b, ds, -L, +L));

            double ratio = trmAngle / grAngle;
            double expected = 0.5;
            double relError = Math.Abs(ratio - expected) / expected;

            _output.WriteLine($"{tc.Name}");
            _output.WriteLine($"M                   : {tc.M:E} kg");
            _output.WriteLine($"b                   : {tc.b:E} m");
            _output.WriteLine($"TRM angle           : {trmAngle:E} rad");
            _output.WriteLine($"Spatial-GR angle    : {grAngle:E} rad");
            _output.WriteLine($"Ratio TRM/SpatialGR : {ratio:F9}");
            _output.WriteLine($"Expected ratio      : {expected:F9}");
            _output.WriteLine($"Relative error      : {relError:P9}");
            _output.WriteLine("");

            Assert.True(relError < 0.02,
                $"TRM/Spatial-GR ratio deviates too much from 0.5 for {tc.Name}");
        }

    }
    [Trait("Category", "LongRunning")]
    [Fact]
    public void TRMBaseline_Should_Show_Global_HalfGR_Pattern_Across_Multiple_Mass_And_Impact_Parameter_Pairs()
    {
        var tracer = new WavefrontTracer();

        double M1 = PhysicalConstantsSI.M_Solar;
        double M2 = 2.0 * M1;

        double b1 = 6.9634e8;
        double b2 = 2.0 * b1;

        double ds = 1e6;
        double L = 1e11;

        var testCases = new[]
        {
        new { Name = "Case 1", M = M1, b = b1 },
        new { Name = "Case 2", M = M2, b = b1 },
        new { Name = "Case 3", M = M1, b = b2 },
        new { Name = "Case 4", M = M2, b = b2 }
    };

        var ratios = new List<double>();

        _output.WriteLine("=== Global TRM vs Spatial-GR Pattern Test ===");
        _output.WriteLine("");

        foreach (var tc in testCases)
        {
            double trmAngle = Math.Abs(tracer.Simulate(tc.M, tc.b));
            double grAngle = Math.Abs(tracer.SimulateSpatialCurvatureLikeGR(tc.M, tc.b, ds, -L, +L));

            double ratio = trmAngle / grAngle;
            ratios.Add(ratio);

            _output.WriteLine($"{tc.Name}");
            _output.WriteLine($"M                   : {tc.M:E} kg");
            _output.WriteLine($"b                   : {tc.b:E} m");
            _output.WriteLine($"TRM angle           : {trmAngle:E} rad");
            _output.WriteLine($"Spatial-GR angle    : {grAngle:E} rad");
            _output.WriteLine($"Ratio TRM/SpatialGR : {ratio:F9}");
            _output.WriteLine("");
        }

        double meanRatio = ratios.Average();
        double expected = 0.5;
        double relError = Math.Abs(meanRatio - expected) / expected;

        _output.WriteLine($"Mean ratio          : {meanRatio:F9}");
        _output.WriteLine($"Expected mean       : {expected:F9}");
        _output.WriteLine($"Relative error      : {relError:P9}");

        Assert.True(relError < 0.02, "Global mean TRM/Spatial-GR ratio deviates too much from 0.5");
    }

    [Fact]
    public void TRMBaseline_Should_Show_Ratio_Convergence_To_HalfGR_With_Integration_Range()
    {
        var tracer = new WavefrontTracer();

        double M = PhysicalConstantsSI.M_Solar;
        double b = 6.9634e8;
        double ds = 1e6;

        double[] ranges =
        {
        1e10,
        2e10,
        5e10,
        1e11,
        2e11
    };

        _output.WriteLine("=== TRM Ratio Convergence Test ===");
        _output.WriteLine($"M            : {M:E} kg");
        _output.WriteLine($"b            : {b:E} m");
        _output.WriteLine($"ds           : {ds:E} m");
        _output.WriteLine("");

        double previousDistanceToHalf = double.MaxValue;

        foreach (double L in ranges)
        {
            double trmAngle = Math.Abs(
                tracer.SimulateTRMBaselineWithRange(M, b, ds, -L, +L)
            );

            double grAngle = Math.Abs(
                tracer.SimulateSpatialCurvatureLikeGR(M, b, ds, -L, +L)
            );

            double ratio = trmAngle / grAngle;
            double distanceToHalf = Math.Abs(ratio - 0.5);

            _output.WriteLine($"Range [-L,+L]     : [-{L:E}, +{L:E}] m");
            _output.WriteLine($"TRM angle         : {trmAngle:E} rad");
            _output.WriteLine($"Spatial-GR angle  : {grAngle:E} rad");
            _output.WriteLine($"Ratio TRM/GR      : {ratio:F9}");
            _output.WriteLine($"|Ratio - 0.5|     : {distanceToHalf:E}");
            _output.WriteLine("");

            Assert.True(trmAngle < 1e-2, $"TRM angle exploded for L={L:E}");
            Assert.True(grAngle < 1e-2, $"Spatial-GR angle exploded for L={L:E}");

            Assert.True(
                distanceToHalf <= previousDistanceToHalf + 1e-6,
                $"TRM/GR ratio moved away from 0.5 unexpectedly at L={L:E}"
            );

            previousDistanceToHalf = distanceToHalf;
        }
    }
}
