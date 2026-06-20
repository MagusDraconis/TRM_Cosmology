using System;
using System.Collections.Generic;
using System.Text;

namespace TRM.QuantumCore.Planck;


public class PlanckConstants
{
    public double lP { get; }
    public double tP { get; }
    public double mP { get; }

    public PlanckConstants(double lp, double tp, double mp)
    {
        lP = lp;
        tP = tp;
        mP = mp;
    }
}

