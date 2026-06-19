namespace TRM.Core
{
    // Datenstruktur für einen einzelnen Radius-Messpunkt einer Galaxie
    public record RarPoint(
        string GalaxyName,
        double RadiusKpc,
        double Vobs,
        double Vgas,
        double Vdisk,
        double Vbulge,
        double GobsMs2,  // Beobachtete Beschleunigung in m/s^2
        double GbarMs2   // Erwartete Newton-Beschleunigung in m/s^2
    );
}
