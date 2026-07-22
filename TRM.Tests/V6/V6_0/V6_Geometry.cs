using System.Collections.Generic;

namespace TRM.Tests.V6_0;

/// <summary>
/// V6 Geometry module — invariant-based geometry on the SAC limit cycle.
/// Computes I₁, I₂, metric g₂₂, and arc length from SAC simulation data.
/// </summary>
/// <remarks>
/// <para>The V6 geometry is defined on a 2D invariant manifold (I₁, I₂) discovered
/// in V5.60 LCM_02-LCM_03. The invariants are:</para>
/// <list type="bullet">
///   <item><b>I₁ = 0.70·km + 0.30·d_mean</b> — coupling-distance invariant (CV=0.0025)</item>
///   <item><b>I₂ = 0.90·km + 0.10·Omega</b> — coupling-frequency invariant (CV=0.0121)</item>
/// </list>
/// <para>The metric on the manifold is ds² = g₂₂·dI₂², where g₂₂ → 1.0 as N → ∞
/// (asymptotically flat Euclidean manifold). See docsV6/TRM_V6_Theory.md for details.</para>
/// <para>Classification: FRAMEWORK — computational infrastructure for V6 geometry.</para>
/// </remarks>
public static class V6Geometry
{
    /// <summary>Coefficient for km in I₁ invariant (coupling-distance).</summary>
    public const double W_KM_I1 = 0.70;
    /// <summary>Coefficient for d_mean in I₁ invariant (coupling-distance).</summary>
    public const double W_DM_I1 = 0.30;
    /// <summary>Coefficient for km in I₂ invariant (coupling-frequency).</summary>
    public const double W_KM_I2 = 0.90;
    /// <summary>Coefficient for Omega in I₂ invariant (coupling-frequency).</summary>
    public const double W_OM_I2 = 0.10;

    /// <summary>
    /// Computes the first invariant I₁ = 0.70·km + 0.30·d_mean.
    /// </summary>
    /// <param name="km">Mean coupling strength Km(K).</param>
    /// <param name="dMean">Mean phase distance Dm(d).</param>
    /// <returns>I₁ value. CV(I₁) ≈ 0.0025 across seeds at N=72.</returns>
    /// <remarks>
    /// I₁ is approximately conserved along the SAC limit cycle. It represents
    /// the coupling-distance trade-off: when coupling is high, distance is low.
    /// </remarks>
    public static double ComputeI1(double km, double dMean) =>
        W_KM_I1 * km + W_DM_I1 * dMean;

    /// <summary>
    /// Computes the second invariant I₂ = 0.90·km + 0.10·Omega.
    /// </summary>
    /// <param name="km">Mean coupling strength Km(K).</param>
    /// <param name="omega">Collective oscillator frequency (mean of Of(h)).</param>
    /// <returns>I₂ value. CV(I₂) ≈ 0.0121 across seeds at N=72.</returns>
    /// <remarks>
    /// I₂ parameterizes position along the invariant manifold. It grows with
    /// system size N, converging as I₂(N) → 1.573 for N → ∞.
    /// </remarks>
    public static double ComputeI2(double km, double omega) =>
        W_KM_I2 * km + W_OM_I2 * omega;

    /// <summary>
    /// Computes cumulative arc length along the (I₁, I₂) trajectory.
    /// </summary>
    /// <param name="i1">Array of I₁ values across epochs (length n).</param>
    /// <param name="i2">Array of I₂ values across epochs (length n).</param>
    /// <returns>Array of cumulative arc lengths s[0..n-1], where s[0]=0.</returns>
    /// <remarks>
    /// Arc length s(t) is strictly monotonic (10/10 seeds confirmed) and serves
    /// as the V6 time coordinate, replacing Omega (which is non-monotonic).
    /// </remarks>
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

    /// <summary>
    /// Computes the metric component g₂₂ = (ds)² / (dI₂)² for a single step.
    /// </summary>
    /// <param name="dI1">Change in I₁ between consecutive epochs.</param>
    /// <param name="dI2">Change in I₂ between consecutive epochs.</param>
    /// <returns>g₂₂ ≥ 1. Returns 1.0 if |dI₂| is negligible.</returns>
    /// <remarks>
    /// g₂₂ = 1 + (dI₁/dI₂)². At finite N, dI₁ fluctuations make g₂₂ > 1.
    /// As N → ∞, dI₁/dI₂ → 0 and g₂₂ → 1 (Euclidean metric).
    /// </remarks>
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

    /// <summary>
    /// Full V6 geometry at a single epoch (or step).
    /// </summary>
    /// <remarks>
    /// <para>Contains all computed V6 geometric quantities:</para>
    /// <list type="bullet">
    ///   <item><b>I1</b> — coupling-distance invariant (should be ~constant)</item>
    ///   <item><b>I2</b> — coupling-frequency invariant (sweeps the ellipse)</item>
    ///   <item><b>S</b> — cumulative arc length (monotonic time coordinate)</item>
    ///   <item><b>G22</b> — metric component (→1.0 at N→∞)</item>
    ///   <item><b>Omega</b> — collective frequency (cyclic driver)</item>
    ///   <item><b>Km</b> — mean coupling strength</item>
    ///   <item><b>DMean</b> — mean phase distance</item>
    /// </list>
    /// </remarks>
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
