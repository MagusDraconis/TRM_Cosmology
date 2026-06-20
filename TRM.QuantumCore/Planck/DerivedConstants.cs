using System;
using System.Collections.Generic;
using System.Text;

namespace TRM.QuantumCore.Planck;

public class DerivedConstants
{
    private readonly PlanckConstants p;

    public DerivedConstants(PlanckConstants planck)
    {
        p = planck;
    }

    public double SpeedOfLight => p.lP / p.tP;

    public double ReducedPlanck => p.mP * p.lP * p.lP / p.tP;

    public double G => (p.lP * p.lP * p.lP) / (p.mP * p.tP * p.tP);
}
