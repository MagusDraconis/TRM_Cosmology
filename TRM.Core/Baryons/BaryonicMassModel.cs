using System;
using System.Collections.Generic;
using System.Text;
using TRM.Core.Shared;
using TRM.Core;
namespace TRM.Core.Baryons;

public static class BaryonicMassModel
{
    public static double ComputeGbarFromMassProfile(
        List<RarPoint> galaxyPoints,
        RarPoint target,
        double upsilonDisk = 0.5,
        double upsilonBulge = 0.7)
    {
        if (galaxyPoints == null || galaxyPoints.Count == 0)
            throw new ArgumentException("Galaxy points missing.");

        double rKpc = target.RadiusKpc;

        double totalMass = 0.0;


        var ordered = galaxyPoints
            .Where(p => p.RadiusKpc <= rKpc)
            .OrderBy(p => p.RadiusKpc)
            .ToList();

        for (int i = 0; i < ordered.Count; i++)
        {
            var p = ordered[i];

            double mCurrent = ComputeMassAtRadius(p, upsilonDisk, upsilonBulge);

            double mPrev = 0.0;
            if (i > 0)
                mPrev = ComputeMassAtRadius(ordered[i - 1], upsilonDisk, upsilonBulge);

            double shellMass = Math.Max(0, mCurrent - mPrev);

            totalMass += shellMass;
        }


        // ✅ kpc → cm (entscheidend!)
        double rCm = rKpc * PhysicalConstants.KpcToCm;

        // ✅ g = G M / r² (CGS)
        double gBar = PhysicalConstants.G * totalMass / (rCm * rCm);

        return gBar;
    }

    private static double ComputeMassAtRadius(
        RarPoint p,
        double upsilonDisk,
        double upsilonBulge)
    {
        double vGasSq = p.Vgas * p.Vgas;
        double vDiskSq = p.Vdisk * p.Vdisk * upsilonDisk;
        double vBulgeSq = p.Vbulge * p.Vbulge * upsilonBulge;

        double vTotalSq = vGasSq + vDiskSq + vBulgeSq;

        // ✅ km/s → cm/s
        double vTotalSq_CGS = vTotalSq * Math.Pow(PhysicalConstants.KmsToCmS, 2);

        // ✅ kpc → cm
        double rCm = p.RadiusKpc * PhysicalConstants.KpcToCm;

        // ✅ M = v² r / G  (CGS)
        double mass = vTotalSq_CGS * rCm / PhysicalConstants.G;

        return mass;
    }

}

    // Approximation für elliptisches Integral
