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

    

    public static PlanckConstants FromPhysicalConstants()
    {
        double c = PhysicalConstants.c;
        double hbar = PhysicalConstants.hbar;
        double G = PhysicalConstants.G;

        double lP = Math.Sqrt(hbar * G / Math.Pow(c, 3));
        double tP = lP / c;
        double mP = Math.Sqrt(hbar * c / G);

        return new PlanckConstants(lP, tP, mP);
    }
    

}

public static class PhysicalConstants
{
    public const double c = 299792458.0;
    public const double hbar = 1.054571817e-34;
    public const double G = 6.67430e-11;
}