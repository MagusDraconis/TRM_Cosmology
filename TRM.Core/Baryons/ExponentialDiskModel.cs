namespace TRM.Core.Baryons;

public static class ExponentialDiskModel
{
    public static double ComputeGbar(
        double rKpc,
        double MdiskSolarMass,
        double RdKpc)
    {
        if (rKpc <= 0 || MdiskSolarMass <= 0 || RdKpc <= 0)
            return 0;

        // ✅ Einheiten
        double rCm = rKpc * PhysicalConstants.KpcToCm;
        double RdCm = RdKpc * PhysicalConstants.KpcToCm;

        double Mdisk = MdiskSolarMass * PhysicalConstants.M_Solar;

        // ✅ Sigma0
        double sigma0 = Mdisk / (2.0 * Math.PI * RdCm * RdCm);

        // ✅ numerische Integration (Ring-Sum)
        int steps = 200;
        double rMax = 10 * RdCm;
        double dr = rMax / steps;

        double enclosedMass = 0.0;

        for (int i = 1; i <= steps; i++)
        {
            double rPrime = i * dr;
            double sigma = sigma0 * Math.Exp(-rPrime / RdCm);

            // Ring-Fläche: 2π r dr
            double dM = 2.0 * Math.PI * rPrime * sigma * dr;

            if (rPrime <= rCm)
                enclosedMass += dM;
        }

        // ✅ g = G M / r²
        double gCgs = PhysicalConstants.G * enclosedMass / (rCm * rCm);

        return gCgs / 100.0; // → m/s²
    }


}