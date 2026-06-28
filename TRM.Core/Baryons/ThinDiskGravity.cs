using TRM.Core;

public static class ThinDiskGravity
{
    public static double ComputeAcceleration(
        double rKpc,
        double diskMassSolar,
        double scaleLengthKpc)
    {
        if (rKpc <= 0) return 0;

        double rCm = rKpc * PhysicalConstants.KpcToCm;
        double RdCm = scaleLengthKpc * PhysicalConstants.KpcToCm;
        double M = diskMassSolar * PhysicalConstants.M_Solar;

        // Sigma0
        double sigma0 = M / (2.0 * Math.PI * RdCm * RdCm);

        int steps = 200;
        double rMax = 10 * RdCm;
        double dr = rMax / steps;

        double gTotal = 0;

        for (int i = 1; i <= steps; i++)
        {
            double rPrime = i * dr;

            double sigma = sigma0 * Math.Exp(-rPrime / RdCm);

            double dM = 2.0 * Math.PI * rPrime * sigma * dr;

            // ✅ Disk kernel (WICHTIG!)
            double kernel = ComputeKernel(rCm, rPrime);

            gTotal += PhysicalConstants.G * dM * kernel;
        }

        return gTotal / 100.0; // → m/s²
    }


    private static double ComputeKernel(double r, double rPrime)
    {
        int thetaSteps = 16; // klein halten, reicht!

        double sum = 0;

        for (int i = 0; i < thetaSteps; i++)
        {
            double theta = 2.0 * Math.PI * i / thetaSteps;

            double distance = Math.Sqrt(
                r * r + rPrime * rPrime - 2.0 * r * rPrime * Math.Cos(theta)
            );

            if (distance <= 0) continue;

            double contribution = (r - rPrime * Math.Cos(theta)) / (distance * distance * distance);

            sum += contribution;
        }

        return sum / thetaSteps;
    }


    // Approximation für elliptisches Integral
    private static double CompleteEllipticApprox(double x)
    {
        if (x < 1)
            return 1.0 + 0.5 * x * x;
        else
            return 1.0 / x * (1 + 0.5 / (x * x));
    }
}
