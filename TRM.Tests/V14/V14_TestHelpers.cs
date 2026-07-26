using System;
using System.Collections.Generic;
using System.Linq;
using TRM.Tests.V7_3_and_4;
using static TRM.Tests.V7_3_and_4.V7TestHelpers;

namespace TRM.Tests.V14;

/// <summary>
/// Shared test helpers for V14.x audit tests.
/// </summary>
public static class V14TestHelpers
{
    /// <summary>
    /// Compute m = d(VT)/d(V1) and dT/dp for a given kernel configuration.
    /// Uses a simplified grid for dT/dp estimation.
    /// </summary>
    public static (double m, double dTdp) ComputeM_and_DTdp(VcFamily fam,
        double xiScale, double k0Scale, double alpha, double beta, double gamma,
        double[] distances, double[] sortedD, double xiBase, double k0Base,
        int nA, double aMin, double da)
    {
        var v1s = new List<double>(); var vts = new List<double>();
        for (int si = 0; si < nA; si++)
        {
            double a = aMin + da * si;
            var v = new VariantSpec($"{fam}_AT", fam, xiScale, k0Scale, a, beta, gamma);
            double sv1 = 0, svt = 0;
            for (int pIdx = 0; pIdx < 3; pIdx++)
            {
                double p = 1.5 + pIdx * 1.0;
                var cci = EvaluateCciVariantAtP(distances, sortedD, xiBase, k0Base, p, v);
                sv1 += cci.VarI1; svt += cci.VarTerms;
            }
            v1s.Add(sv1 / 3.0); vts.Add(svt / 3.0);
        }
        var v1a = v1s.ToArray(); var vta = vts.ToArray();
        double mV1 = v1a.Average(), mVT = vta.Average();
        double cov = 0, vx = 0;
        for (int i = 0; i < v1a.Length; i++) { double dx = v1a[i] - mV1; cov += dx * (vta[i] - mVT); vx += dx * dx; }
        double m = vx > 1e-15 ? cov / vx : 0;

        const int nAG = 5, nPG = 7;
        double pMin = 0.5, pMax = 4.5, dpG = (pMax - pMin) / (nPG - 1);
        double daG = (1.40 - 0.21) / (nAG - 1);
        double sumD = 0; int nD = 0;
        for (int ag = 0; ag < nAG; ag++)
        {
            double alphaA = 0.21 + daG * ag;
            for (int pi = 1; pi < nPG - 1; pi++)
            {
                double p = pMin + dpG * pi;
                var vP = new VariantSpec($"{fam}_DP", fam, xiScale, k0Scale, alphaA, beta, gamma);
                var vM = new VariantSpec($"{fam}_DM", fam, xiScale, k0Scale, alphaA, beta, gamma);
                var cciP = EvaluateCciVariantAtP(distances, sortedD, xiBase, k0Base, p + dpG, vP);
                var cciM = EvaluateCciVariantAtP(distances, sortedD, xiBase, k0Base, p - dpG, vM);
                sumD += ((cciP.VarI1 + cciP.VarTerms) - (cciM.VarI1 + cciM.VarTerms)) / (2 * dpG); nD++;
            }
        }
        double dTdp = nD > 0 ? sumD / nD : 0;
        return (m, dTdp);
    }

    /// <summary>
    /// Full state computation: m, |m|, |m+1|, dT/dp, sign, feedback, tick.
    /// </summary>
    public static (VcFamily fam, double m, double absM, double V, double dTdp, int sign, double fb, double tick)
        ComputeFull(VcFamily fam, double xiScale, double k0Scale, double alpha, double beta, double gamma,
            double[] distances, double[] sortedD, double xiBase, double k0Base,
            int nA, double aMin, double da)
    {
        var v1s = new List<double>(); var vts = new List<double>();
        for (int si = 0; si < nA; si++)
        {
            double a = aMin + da * si;
            var v = new VariantSpec($"{fam}_CF", fam, xiScale, k0Scale, a, beta, gamma);
            double sv1 = 0, svt = 0;
            for (int pIdx = 0; pIdx < 3; pIdx++)
            { double p = 1.5 + pIdx * 1.0; var cci = EvaluateCciVariantAtP(distances, sortedD, xiBase, k0Base, p, v); sv1 += cci.VarI1; svt += cci.VarTerms; }
            v1s.Add(sv1 / 3.0); vts.Add(svt / 3.0);
        }
        var v1a = v1s.ToArray(); var vta = vts.ToArray();
        double mV1 = v1a.Average(), mVT = vta.Average();
        double cov = 0, vx = 0;
        for (int i = 0; i < v1a.Length; i++) { double dx = v1a[i] - mV1; cov += dx * (vta[i] - mVT); vx += dx * dx; }
        double m = vx > 1e-15 ? cov / vx : 0;

        const int nAG = 5, nPG = 7;
        double pMin = 0.5, pMax = 4.5, dpG = (pMax - pMin) / (nPG - 1);
        double daG = (1.40 - 0.21) / (nAG - 1);
        double sumD = 0; int nD = 0;
        for (int ag = 0; ag < nAG; ag++)
        {
            double alphaA = 0.21 + daG * ag;
            for (int pi = 1; pi < nPG - 1; pi++)
            {
                double p = pMin + dpG * pi;
                var vP = new VariantSpec("P", fam, xiScale, k0Scale, alphaA, beta, gamma);
                var vM = new VariantSpec("M", fam, xiScale, k0Scale, alphaA, beta, gamma);
                var cciP = EvaluateCciVariantAtP(distances, sortedD, xiBase, k0Base, p + dpG, vP);
                var cciM = EvaluateCciVariantAtP(distances, sortedD, xiBase, k0Base, p - dpG, vM);
                sumD += ((cciP.VarI1 + cciP.VarTerms) - (cciM.VarI1 + cciM.VarTerms)) / (2 * dpG); nD++;
            }
        }
        double dTdp = nD > 0 ? sumD / nD : 0;

        var sf = new List<double>();
        for (int i = 1; i < v1a.Length; i++) { double dV1 = (v1a[i] - v1a[i - 1]) / da; if (Math.Abs(dV1) < 1e-12) continue; sf.Add(-(vta[i] - vta[i - 1]) / da / dV1); }
        double fb = sf.Count > 0 ? sf.Average() : 0;

        double tick = 0;
        for (int i = 1; i < v1a.Length; i++) tick += Math.Abs((v1a[i] + vta[i]) - (v1a[i - 1] + vta[i - 1])) / da;
        tick /= (v1a.Length - 1);

        int sign = dTdp > 1e-8 ? 1 : dTdp < -1e-8 ? -1 : 0;
        return (fam, m, Math.Abs(m), Math.Abs(1 + m), dTdp, sign, fb, tick);
    }

    /// <summary>
    /// Z-score normalization.
    /// </summary>
    public static double[] ZScore(double[] x)
    {
        double m = x.Average();
        double sd = Math.Sqrt(x.Select(v => (v - m) * (v - m)).Average());
        return x.Select(v => sd > 1e-15 ? (v - m) / sd : 0.0).ToArray();
    }

    /// <summary>
    /// Evaluate a single grid point (U = VarI1 + VarTerms) for a family at (α, p).
    /// </summary>
    public static double ComputeGridPoint(VcFamily fam, int ai, int pi,
        double aMin, double da, double pMin, double dp,
        double[] distances, double[] sorted, double xiBase, double k0Base)
    {
        double alpha = aMin + da * ai;
        double p = pMin + dp * pi;
        var v = new VariantSpec($"{fam}_GP", fam, 1.0, 1.0, alpha, 0.5, 0.0);
        double sv1 = 0, svt = 0;
        for (int ss = 0; ss < 2; ss++)
        {
            double pp = p + (ss - 0.5) * 0.1;
            var cci = EvaluateCciVariantAtP(distances, sorted, xiBase, k0Base, pp, v);
            sv1 += cci.VarI1; svt += cci.VarTerms;
        }
        return (sv1 + svt) / 2.0;
    }
}
