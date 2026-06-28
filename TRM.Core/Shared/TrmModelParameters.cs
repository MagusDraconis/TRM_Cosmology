namespace TRM.Core;

public static class TrmModelParameters
{
    // Fundamental acceleration scale
    public static readonly double DefaultA0_Ms2 = 1.2e-10;
    public static readonly double DefaultA0_Cgs = 1.2e-8;

    // Global synchronization / phase coupling
    public static readonly double DefaultPhiBeta = 0.4;

    // Radial regime correction strength
    public static readonly double DefaultRegimeGamma = 0.25;

    // Bulge treatment
    public static readonly double DefaultBulgeSofteningKpc = 0.5;

    // Radial transition regime in units of Rd
    public static readonly double InnerRegimeStart_Rd = 1.0;
    public static readonly double InnerRegimeEnd_Rd = 4.0;

    // Orbit integration stability
    public static readonly double MinimumIntegrationWeight = 1e-20;

    // Optional safety clamps
    public static readonly double MinCorrectionFactor = 0.75;
    public static readonly double MaxCorrectionFactor = 1.25;

    public static readonly double MinLocalToFullRatio = 0.5;
    public static readonly double MaxLocalToFullRatio = 1.5;
}
