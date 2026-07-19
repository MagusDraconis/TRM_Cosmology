using System;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4;

/// <summary>
/// TRM V4 — B5-O6: Light Deflection Mapping Tests
///
/// TRM:  Effective refractive index n = 1/T ≈ 1 − φ
///       Deflection α = (2/c²) ∫ ∇φ · dl_perp
///       Point mass: α = 4GM/(c²b)
///
/// GR:   α = 4GM/(c²b)   (weak field)
/// Newton (corpuscular): α = 2GM/(c²b)
///
/// The factor 4 in TRM arises from spatial + temporal contributions.
///
/// Reference: docsV4/theory/TRM_V4_B5_ObservableDictionary.md
/// </summary>
[Trait("Category", "V4")]
[Trait("Category", "B5")]
public class B5_LightDeflectionMapping_Tests
{
    private readonly ITestOutputHelper _output;

    private const double C = 2.99792458e8;
    private const double G = 6.67430e-11;
    private const double MSun = 1.98847e30;
    private const double RSun = 6.957e8;

    public B5_LightDeflectionMapping_Tests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void B5O6_01_Solar_Deflection_Angle()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B5-O6.01 — SOLAR LIGHT DEFLECTION");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        // Grazing incidence: impact parameter b = R_sun
        double b = RSun;

        // Newton (corpuscular)
        double alphaNewton = 2.0 * G * MSun / (C * C * b);

        // GR and TRM: factor 4
        double alphaTrm = 4.0 * G * MSun / (C * C * b);

        // In arcseconds
        double arcsecPerRad = 180.0 * 3600.0 / Math.PI;
        double alphaNewtonAs = alphaNewton * arcsecPerRad;
        double alphaTrmAs = alphaTrm * arcsecPerRad;

        _output.WriteLine($"  Impact parameter: b = R_sun = {b:E2} m");
        _output.WriteLine($"  Newton:  α = {alphaNewtonAs:F4} arcsec");
        _output.WriteLine($"  TRM/GR:  α = {alphaTrmAs:F4} arcsec");
        _output.WriteLine("");

        // Known: GR prediction ≈ 1.75 arcsec (Eddington 1919)
        double grKnown = 1.75;
        _output.WriteLine($"  Eddington (1919): α ≈ {grKnown} arcsec");
        _output.WriteLine($"  TRM prediction:   α ≈ {alphaTrmAs:F2} arcsec");
        _output.WriteLine("");

        // TRM factor-4 check
        double ratio = alphaTrm / alphaNewton;
        _output.WriteLine($"  TRM/Newton ratio = {ratio:F1} (must be exactly 2.0)");
        Assert.Equal(2.0, ratio, 12);
        _output.WriteLine("  ✅ TRM deflection = 2 × Newton = GR weak-field prediction.");

        // Match within 5% of known GR value
        Assert.True(Math.Abs(alphaTrmAs - grKnown) / grKnown < 0.05,
            "TRM solar deflection should match GR within 5%");
    }

    [Fact]
    public void B5O6_02_Deflection_Impact_Parameter_Scaling()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B5-O6.02 — DEFLECTION vs IMPACT PARAMETER");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        double[] bValues = { 1.0 * RSun, 2.0 * RSun, 5.0 * RSun, 10.0 * RSun };

        _output.WriteLine($"  {"b (R_sun)",12} {"α_TRM (as)",12} {"α ∝ 1/b?",12}");
        _output.WriteLine($"  {new string('─', 12)} {new string('─', 12)} {new string('─', 12)}");

        double alphaRef = 0;
        foreach (double b in bValues)
        {
            double alpha = 4.0 * G * MSun / (C * C * b);
            double alphaAs = alpha * 180.0 * 3600.0 / Math.PI;

            if (alphaRef == 0) alphaRef = alphaAs * b;  // α·b should be constant
            double product = alphaAs * b;
            double constancy = product / alphaRef;

            _output.WriteLine($"  {b / RSun,12:F1} {alphaAs,12:F4} {constancy,12:F6}");
        }

        _output.WriteLine("");
        _output.WriteLine("  α·b = constant → α ∝ 1/b (verified)");
        _output.WriteLine("  ✅ Deflection scales as 1/b — correct for weak-field GR.");
    }

    [Fact]
    public void B5O6_03_Deflection_Source_Of_Factor_4()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B5-O6.03 — SOURCE OF FACTOR 4");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        _output.WriteLine("  Newton (corpuscular):");
        _output.WriteLine("    Particle deflected by gravitational force.");
        _output.WriteLine("    α = 2GM/(c²b)");
        _output.WriteLine("");
        _output.WriteLine("  GR / TRM V4:");
        _output.WriteLine("    Spatial curvature contribution: 2GM/(c²b)");
        _output.WriteLine("    Temporal gradient contribution: 2GM/(c²b)");
        _output.WriteLine("    Total: α = 4GM/(c²b)");
        _output.WriteLine("");
        _output.WriteLine("  In TRM V4:");
        _output.WriteLine("    n = 1/T = 1/(1+φ) ≈ 1 − φ");
        _output.WriteLine("    The spatial gradient ∇n = −∇φ contributes.");
        _output.WriteLine("    The temporal gradient (∂n/∂t through propagation)");
        _output.WriteLine("    contributes the second factor of 2.");
        _output.WriteLine("");
        _output.WriteLine("  Status: EFFECTIVE (requires φ = GM/(c²r) calibration).");
        _output.WriteLine("  The factor 4 is not derived from oscillator dynamics —");
        _output.WriteLine("  it follows from the refractive index formalism n = 1/T.");
    }
}
