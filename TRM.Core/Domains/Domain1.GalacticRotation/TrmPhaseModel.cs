using System;
using System.Collections.Generic;
using System.Text;

namespace TRM.Core.Domains.Domain1.GalacticRotation;

public static class TrmPhaseModel
{
    public static double ComputePhaseTerm(
        double gCurrent,
        double gNext,
        double dr)
    {
        if(gCurrent <= 0 || gNext <= 0 || dr <= 0)
            return 0;

        double dLog = Math.Log(gNext) - Math.Log(gCurrent);
        double gradient = dLog / dr;

        // ✅ Normierung (Skalenkontrolle)
        double scale = 1.0; // kpc^-1 Skala (tunable)

        // ✅ Sättigung via tanh → verhindert Explosion
        double phase = Math.Tanh(gradient / scale);

        return phase;
    }
}
