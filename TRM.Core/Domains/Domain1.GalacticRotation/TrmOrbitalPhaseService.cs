using System;

namespace TRM.Core.Domains.Domain1.GalacticRotation
{
    public static class TrmOrbitalPhaseService
    {
        public static double ComputeIntegratedPhase(
            List<RarPoint> galaxy,
            double targetRadius)
        {
            if(galaxy == null || galaxy.Count < 2)
                return 0;

            var ordered = galaxy.OrderBy(p => p.RadiusKpc).ToList();

            double phi = 0.0;
            double lastG = -1;

            foreach(var p in ordered)
            {
                if(p.RadiusKpc > targetRadius)
                    break;

                if(p.GbarMs2 <= 0)
                    continue;

                if(lastG > 0)
                {
                    // d ln g
                    double dLog = Math.Log(p.GbarMs2) - Math.Log(lastG);

                    // stabil integriert
                    phi += dLog;
                }

                lastG = p.GbarMs2;
            }

            // ✅ Sättigung → verhindert Explosion
            double phiEff = Math.Tanh(phi);

            return phiEff;
        }
    }
}
