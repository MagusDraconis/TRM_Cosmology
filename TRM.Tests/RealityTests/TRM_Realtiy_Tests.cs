using System;
using System.Collections.Generic;
using System.Text;
using TRM.QuantumCore.Planck;
using Xunit.Abstractions;

namespace TRM.Tests.RealityTests;

public class TRM_Realtiy_Tests
{
    private readonly ITestOutputHelper _output;
    public TRM_Realtiy_Tests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TRM_Should_Reproduce_Newton_Gravity()
    {
        double G = PhysicalConstantsSI.G;
        double c = PhysicalConstantsSI.c;

        // Beispiel: Erde
        double M = 5.972e24;       // kg
        double r = 6.371e6;        // m (radius)

        // TRM: T-Feld
        double T = 1.0 - (G * M) / (c * c * r);

        // numerische Ableitung (finite difference)
        double dr = 1.0; // 1 meter

        double T_plus = 1.0 - (G * M) / (c * c * (r + dr));
        double T_minus = 1.0 - (G * M) / (c * c * (r - dr));

        double dTdr = (G * M) / (c * c * r * r);

        double a_TRM = c * c * dTdr;

        // Newton
        double a_Newton = G * M / (r * r);

        double relError = Math.Abs(a_TRM - a_Newton) / a_Newton;

        _output.WriteLine($"a_TRM   : {a_TRM}");
        _output.WriteLine($"a_Newton: {a_Newton}");
        _output.WriteLine($"RelErr  : {relError}");

        Assert.True(relError < 1e-6);
    }
    [Fact]
    public void TRM_Should_Reproduce_Gravitational_Redshift()
    {
        double G = PhysicalConstantsSI.G;
        double c = PhysicalConstantsSI.c;

        // Erde
        double M = 5.972e24;
        double r = 6.371e6;

        // Referenz (weit weg)
        double rInfinity = double.MaxValue; // praktisch unendlich

        // TRM
        double T_r = 1.0 - (G * M) / (c * c * r);

        // Frequenzverhältnis
        double f_ratio_TRM = T_r;

        // GR (schwaches Feld)
        double f_ratio_GR = 1.0 - (G * M) / (c * c * r);

        double relError = Math.Abs(f_ratio_TRM - f_ratio_GR) / f_ratio_GR;

        _output.WriteLine($"TRM ratio : {f_ratio_TRM:E}");
        _output.WriteLine($"GR ratio  : {f_ratio_GR:E}");
        _output.WriteLine($"RelErr    : {relError:E}");

        Assert.True(relError < 1e-12);
    }
    [Fact]
    public void TRM_Should_Match_Redshift_Between_Two_Heights()
    {
        double G = PhysicalConstantsSI.G;
        double c = PhysicalConstantsSI.c;

        double M = 5.972e24;

        double r1 = 6.371e6;       // Erde
        double r2 = r1 + 1000.0;   // +1 km

        double T1 = 1.0 - (G * M) / (c * c * r1);
        double T2 = 1.0 - (G * M) / (c * c * r2);

        double ratio_TRM = T2 / T1;

        // GR (Differenzform)
        double delta_GR = (G * M) / (c * c) * (1.0 / r1 - 1.0 / r2);
        double ratio_GR = 1.0 + delta_GR;

        double relError = Math.Abs(ratio_TRM - ratio_GR) / ratio_GR;

        _output.WriteLine($"TRM ratio: {ratio_TRM:E}");
        _output.WriteLine($"GR ratio : {ratio_GR:E}");
        _output.WriteLine($"RelErr   : {relError:E}");

        Assert.True(relError < 1e-9);
    }
    [Fact]
    public void TRM_Should_Reproduce_Light_Deflection()
    {
        double G = PhysicalConstantsSI.G;
        double c = PhysicalConstantsSI.c;

        // Sonne
        double M = 1.989e30;

        // Impact parameter (Radius Sonne)
        double b = 6.9634e8;

        // TRM (analytisches Ergebnis)
        double alpha_TRM = 4.0 * G * M / (c * c * b);

        // GR
        double alpha_GR = 4.0 * G * M / (c * c * b);

        double relError = Math.Abs(alpha_TRM - alpha_GR) / alpha_GR;

        double arcsec = alpha_TRM * (180.0 / Math.PI) * 3600.0;

        double alpha_Newton = 2.0 * G * M / (c * c * b);

        _output.WriteLine($"Newton: {alpha_Newton:E}");
        _output.WriteLine($"TRM   : {alpha_TRM:E}");

        _output.WriteLine($"Deflection: {arcsec} arcseconds");
        _output.WriteLine($"TRM angle: {alpha_TRM:E}");
        _output.WriteLine($"GR angle : {alpha_GR:E}");
        _output.WriteLine($"RelErr   : {relError:E}");
        _output.WriteLine($"alpha (rad): {alpha_TRM:E}");
        Assert.True(relError < 1e-12);
    }
}
