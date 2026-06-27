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
        double c = PhysicalConstantsSI.c;
        double hbar = PhysicalConstantsSI.hbar;
        double G = PhysicalConstantsSI.G;

        double lP = Math.Sqrt(hbar * G / Math.Pow(c, 3));
        double tP = lP / c;
        double mP = Math.Sqrt(hbar * c / G);

        return new PlanckConstants(lP, tP, mP);
    }
    

}

public static class PhysicalConstantsSI
{

    public const double hbar = 1.054571817e-34;

    public const double G = 6.67430e-11; // SI
    public const double M_Solar = 1.989e30; // kg
    public const double c = 299792458.0; // m/s
    public const double b = 6.9634e8;    // m


    // ✅ NEU: Erde
    public const double M_Earth = 5.972e24;     // kg
    public const double R_Earth = 6.371e6;      // m

    // ✅ optional: typische Orbit-Höhe (low orbit)
    public const double Earth_LowOrbit = R_Earth + 4.0e5;

}