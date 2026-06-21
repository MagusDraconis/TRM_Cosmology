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

        // Referenzwerte (baseline Planck → gibt dir "wahre" c, ħ, G)
        var baseDerived = new DerivedConstants(baseP);

        double c0 = baseDerived.SpeedOfLight;
        double hbar0 = baseDerived.ReducedPlanck;
        double G0 = baseDerived.G;

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

            // Ratio zur Referenz
            double cRatio = d.SpeedOfLight / c0;
            double hbarRatio = d.ReducedPlanck / hbar0;
            double GRatio = d.G / G0;

            // 🔥 DAS ist deine Stability-Metric
            double stability =
                Math.Pow(cRatio - 1, 2) +
                Math.Pow(hbarRatio - 1, 2) +
                Math.Pow(GRatio - 1, 2);

            results.Add(new ScanResult
            {
                epsL = epsL,
                epsT = epsT,
                epsM = epsM,
                c = d.SpeedOfLight,
                hbar = d.ReducedPlanck,
                G = d.G,
                Stability = stability

            });
        }

        return results;
    }

    private static double RandomRange(double min, double max)
    {
        return min + (max - min) * Random.Shared.NextDouble();
    }
}
