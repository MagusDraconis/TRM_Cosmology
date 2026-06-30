namespace TRM.QuantumCore.Planck;

/// <summary>
/// SI constants used by Planck conversion and related test scenarios.
/// Status: calibrated/defined constants; tested indirectly by Planck consistency tests.
/// </summary>
public static class PhysicalConstantsSI
{

    public const double hbar = 1.054571817e-34;

    public const double G = 6.67430e-11; // SI
    public const double M_Solar = 1.989e30; // kg
    public const double c = 299792458.0; // m/s
    public const double b = 6.9634e8;    // m


    // ✅ NEU: Erde
    public const double M_Earth = 5.972e24;     // kg
    public const double R_Earth = 6.371e6;      // m

    // ✅ optional: typische Orbit-Höhe (low orbit)
    public const double Earth_LowOrbit = R_Earth + 4.0e5;

}