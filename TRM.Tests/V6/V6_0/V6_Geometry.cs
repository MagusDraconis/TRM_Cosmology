using System.Collections.Generic;

namespace TRM.Tests.V6_0;

/// <summary>
/// V6 Geometry module — invariant-based geometry on the SAC limit cycle.
/// Computes I₁, I₂, metric g₂₂, and arc length from SAC simulation data.
/// Classification: FRAMEWORK — computational infrastructure for V6 geometry.
/// </summary>
public static class V6Geometry
{
    // Invariant coefficients (from V5.60 LCM_02-LCM_03)
    public const double W_KM_I1 = 0.70;
    public const double W_DM_I1 = 0.30;
    public const double W_KM_I2 = 0.90;
    public const double W_OM_I2 = 0.10;

    /// <summary>I₁ = 0.70·km + 0.30·d_mean (coupling-distance invariant).</summary>
    public static double ComputeI1(double km, double dMean) =>
        W_KM_I1 * km + W_DM_I1 * dMean;

    /// <summary>I₂ = 0.90·km + 0.10·Omega (coupling-frequency invariant).</summary>
    public static double ComputeI2(double km, double omega) =>
        W_KM_I2 * km + W_OM_I2 * omega;

    /// <summary>Arc length along the (I₁, I₂) trajectory (cumulative).</summary>
    public static double[] ComputeArcLength(double[] i1, double[] i2)
    {
        int n = i1.Length;
        var s = new double[n];
        for (int i = 1; i < n; i++)
        {
            double d1 = i1[i] - i1[i - 1];
            double d2 = i2[i] - i2[i - 1];
            s[i] = s[i - 1] + Math.Sqrt(d1 * d1 + d2 * d2);
        }
        return s;
    }

    /// <summary>Metric component g₂₂ = (ds)² / (dI₂)² for a single step.</summary>
    public static double ComputeG22(double dI1, double dI2)
    {
        double ds = Math.Sqrt(dI1 * dI1 + dI2 * dI2);
        double absDI2 = Math.Abs(dI2);
        if (absDI2 < 1e-10) return 1.0;
        return (ds / absDI2) * (ds / absDI2);
    }

    /// <summary>Compute g₂₂ along the full trajectory. Returns array of length n-1.</summary>
    public static double[] ComputeG22Trajectory(double[] i1, double[] i2)
    {
        int n = i1.Length;
        var g22 = new double[n - 1];
        for (int i = 1; i < n; i++)
            g22[i - 1] = ComputeG22(i1[i] - i1[i - 1], i2[i] - i2[i - 1]);
        return g22;
    }

    /// <summary>PCA eccentricity of (I₁, I₂) trajectory. Returns (eccentricity, axisRatio).</summary>
    public static (double eccentricity, double axisRatio, double orientation)
        ComputeEllipseParams(double[] i1, double[] i2)
    {
        int n = i1.Length;
        double m1 = i1.Average(), m2 = i2.Average();
        double c11 = 0, c22 = 0, c12 = 0;
        for (int i = 0; i < n; i++)
        {
            double d1 = i1[i] - m1, d2 = i2[i] - m2;
            c11 += d1 * d1; c22 += d2 * d2; c12 += d1 * d2;
        }
        c11 /= n; c22 /= n; c12 /= n;

        double trace = c11 + c22;
        double det = c11 * c22 - c12 * c12;
        double disc = Math.Sqrt(Math.Max(0, trace * trace - 4 * det));
        double e1 = (trace + disc) / 2;
        double e2 = (trace - disc) / 2;

        double ratio = Math.Sqrt(Math.Max(e2 / e1, 1e-15));
        double ecc = Math.Sqrt(Math.Max(0, 1 - ratio * ratio));
        double orient = Math.Atan2(2 * c12, c11 - c22) / 2 * 180 / Math.PI;

        return (ecc, ratio, orient);
    }

    /// <summary>Standard deviation of an array.</summary>
    public static double StdDev(double[] values)
    {
        double m = values.Average();
        return Math.Sqrt(values.Sum(v => (v - m) * (v - m)) / (values.Length - 1));
    }

    /// <summary>Coefficient of variation.</summary>
    public static double CV(double[] values)
    {
        double m = values.Average();
        return Math.Abs(m) > 1e-10 ? Math.Abs(StdDev(values) / m) : StdDev(values);
    }

    /// <summary>Check if I₁ is invariant along the trajectory (CV < threshold).</summary>
    public static bool IsI1Invariant(double[] i1, double threshold = 0.02) =>
        CV(i1) < threshold;

    /// <summary>Check if I₂ is invariant along the trajectory (CV < threshold).</summary>
    public static bool IsI2Invariant(double[] i2, double threshold = 0.05) =>
        CV(i2) < threshold;

    /// <summary>Check if arc length is monotonic.</summary>
    public static bool IsArcMonotonic(double[] s)
    {
        for (int i = 1; i < s.Length; i++)
            if (s[i] <= s[i - 1]) return false;
        return true;
    }

    /// <summary>Check if g₂₂ is approximately Euclidean (within tolerance).</summary>
    public static bool IsMetricEuclidean(double[] g22, double tolerance = 0.02)
    {
        double mean = g22.Average();
        return Math.Abs(mean - 1.0) < tolerance;
    }

    /// <summary>MetricResult: full V6 geometry at a single epoch.</summary>
    public struct MetricResult
    {
        public double I1;
        public double I2;
        public double S;
        public double G22;
        public double Omega;
        public double Km;
        public double DMean;
    }

    /// <summary>
    /// Compute full V6 geometry trajectory from SAC simulation data.
    /// km, dMean, Omega arrays must all have the same length.
    /// </summary>
    public static List<MetricResult> ComputeTrajectory(
        double[] km, double[] dMean, double[] omega)
    {
        int n = km.Length;
        var results = new List<MetricResult>(n);
        var i1 = new double[n];
        var i2 = new double[n];

        for (int i = 0; i < n; i++)
        {
            i1[i] = ComputeI1(km[i], dMean[i]);
            i2[i] = ComputeI2(km[i], omega[i]);
        }

        var s = ComputeArcLength(i1, i2);
        var g22 = ComputeG22Trajectory(i1, i2);

        for (int i = 0; i < n; i++)
        {
            results.Add(new MetricResult
            {
                I1 = i1[i],
                I2 = i2[i],
                S = s[i],
                G22 = i > 0 ? g22[i - 1] : 1.0,
                Omega = omega[i],
                Km = km[i],
                DMean = dMean[i]
            });
        }
        return results;
    }
}
