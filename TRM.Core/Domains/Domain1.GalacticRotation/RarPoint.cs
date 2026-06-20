namespace TRM.Core
{
    // Data structure for a single galaxy radial measurement point
    public record RarPoint(
        string GalaxyName,
        double RadiusKpc,
        double Vobs,
        double Vgas,
        double Vdisk,
        double Vbulge,
        double GobsMs2,  // Observed acceleration in m/s^2
        double GbarMs2   // Expected Newtonian acceleration in m/s^2
    );
}
