namespace TRM.Core;

public static class PhysicalConstants
{
    // Physical constants in CGS units
    public const double G = 6.674e-8;
    public const double M_Solar = 1.989e33;
    public const double ProtonMass = 1.67e-24;
    public const double PlasmaIonizationFactor = 1.9;
    //public const double c = 2.99792458e10; // cm/s ✅

    public const double KpcToCm = 3.08567758e21;
    public const double KmsToCmS = 100000.0;
    public const double Kms2KpcToMs2 = 3.240779289e-14;

    // Milgrom acceleration constant (linked to TRM background drift field H_T)
    public const double A0_Cosmic = 1.2e-8; // cm/s^2

    public const double C_Kms = 299792.458; // Lichtgeschwindigkeit in km/s
}

