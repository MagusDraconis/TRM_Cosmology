
namespace TRM.Core.Shared
{
    public static class LambdaCdMHelper
    {
        public static double CalculateAngularDiameterDistance(
            double z,
            double H0 = 70.0,
            double OmegaM = 0.3)
        {
            double c = PhysicalConstants.C_Kms;

            int n = 1000;
            double dz = z / n;

            double integral = 0.0;

            for(int i = 0; i < n; i++)
            {
                double zp = i * dz;

                double E = Math.Sqrt(OmegaM * Math.Pow(1 + zp, 3) + (1 - OmegaM));
                integral += dz / E;
            }

            double Dc = c / H0 * integral;

            return Dc / (1 + z);
        }
    }
}