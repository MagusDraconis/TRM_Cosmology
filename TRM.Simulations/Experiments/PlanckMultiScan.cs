using System;
using System.Collections.Generic;
using System.Text;
using TRM.QuantumCore.Planck;

namespace TRM.Simulations.Experiments;

public class PlanckMultiScan
{
    private readonly PlanckConstants baseP;

    public PlanckMultiScan(PlanckConstants basePlanck)
    {
        baseP = basePlanck;
    }

    public List<ScanResult> Run(int steps, double maxVariation)
    {
        var results = new List<ScanResult>();

        for (int i = 0; i < steps; i++)
        {
            double epsL = RandomRange(-maxVariation, maxVariation);
            double epsT = RandomRange(-maxVariation, maxVariation);
            double epsM = RandomRange(-maxVariation, maxVariation);

            var pVar = new PlanckConstants(
                baseP.lP * (1 + epsL),
                baseP.tP * (1 + epsT),
                baseP.mP * (1 + epsM)
            );

            var d = new DerivedConstants(pVar);

            results.Add(new ScanResult
            {
                epsL = epsL,
                epsT = epsT,
                epsM = epsM,
                c = d.SpeedOfLight,
                hbar = d.ReducedPlanck,
                G = d.G
            });
        }

        return results;
    }

    private static double RandomRange(double min, double max)
    {
        return min + (max - min) * Random.Shared.NextDouble();
    }
}
