using System.Collections.Generic;

namespace TRM.Core.Geometry.V6;

/// <summary>
/// V6 Pipeline — integrates V6 geometry computation with SAC simulation data.
/// Takes arrays of km, d_mean, and Omega from SAC epochs and produces
/// full V6 geometric trajectory.
/// </summary>
/// <remarks>
/// <para>Usage:</para>
/// <code>
///   var traj = V6Pipeline.ComputeTrajectory(kmArray, dmArray, omArray);
///   Console.WriteLine($"I1 CV = {traj.I1CV:F4}");
///   Console.WriteLine($"Euclidean = {traj.IsMetricEuclidean()}");
/// </code>
/// </remarks>
public static class V6Pipeline
{
    /// <summary>
    /// Computes the full V6 geometric trajectory from SAC epoch data.
    /// </summary>
    /// <param name="km">Mean coupling strengths per epoch.</param>
    /// <param name="dMean">Mean phase distances per epoch.</param>
    /// <param name="omega">Collective frequencies per epoch.</param>
    /// <returns>V6Trajectory with all geometric quantities computed.</returns>
    public static V6Trajectory ComputeTrajectory(double[] km, double[] dMean, double[] omega)
    {
        int n = km.Length;
        var i1 = new double[n];
        var i2 = new double[n];

        for (int i = 0; i < n; i++)
        {
            i1[i] = V6Geometry.ComputeI1(km[i], dMean[i]);
            i2[i] = V6Geometry.ComputeI2(km[i], omega[i]);
        }

        var arcLength = V6Geometry.ComputeArcLength(i1, i2);
        var g22 = V6Geometry.ComputeG22Trajectory(i1, i2);
        var (ecc, ratio, orient) = V6Geometry.ComputeEllipseParams(i1, i2);

        return new V6Trajectory
        {
            Epochs = n,
            I1 = i1,
            I2 = i2,
            ArcLength = arcLength,
            G22 = g22,
            Omega = omega,
            Km = km,
            DMean = dMean,
            Eccentricity = ecc,
            AxisRatio = ratio,
            Orientation = orient
        };
    }
}

/// <summary>
/// Complete V6 geometric trajectory across multiple SAC epochs.
/// Contains all computed geometric quantities and summary statistics.
/// </summary>
public sealed class V6Trajectory
{
    /// <summary>Number of epochs.</summary>
    public int Epochs { get; init; }

    /// <summary>I₁ values per epoch (coupling-distance invariant).</summary>
    public double[] I1 { get; init; } = [];

    /// <summary>I₂ values per epoch (coupling-frequency invariant).</summary>
    public double[] I2 { get; init; } = [];

    /// <summary>Cumulative arc length per epoch (time coordinate).</summary>
    public double[] ArcLength { get; init; } = [];

    /// <summary>Metric component g₂₂ per step (length = Epochs-1).</summary>
    public double[] G22 { get; init; } = [];

    /// <summary>Collective frequency per epoch.</summary>
    public double[] Omega { get; init; } = [];

    /// <summary>Mean coupling strength per epoch.</summary>
    public double[] Km { get; init; } = [];

    /// <summary>Mean phase distance per epoch.</summary>
    public double[] DMean { get; init; } = [];

    /// <summary>Ellipse eccentricity in (I₁, I₂) plane.</summary>
    public double Eccentricity { get; init; }

    /// <summary>Ellipse axis ratio (minor/major).</summary>
    public double AxisRatio { get; init; }

    /// <summary>Ellipse orientation in degrees.</summary>
    public double Orientation { get; init; }

    // === Summary statistics ===

    /// <summary>Mean I₁ across epochs.</summary>
    public double I1Mean => I1.Average();

    /// <summary>Coefficient of variation of I₁.</summary>
    public double I1CV => V6Geometry.CV(I1);

    /// <summary>Mean I₂ across epochs.</summary>
    public double I2Mean => I2.Average();

    /// <summary>Coefficient of variation of I₂.</summary>
    public double I2CV => V6Geometry.CV(I2);

    /// <summary>Total arc length.</summary>
    public double TotalArcLength => ArcLength[^1];

    /// <summary>Mean g₂₂ across steps.</summary>
    public double G22Mean => G22.Average();

    /// <summary>Median g₂₂ (robust to outliers).</summary>
    public double G22Median
    {
        get
        {
            if (G22.Length == 0) return 1.0;
            var sorted = G22.OrderBy(g => g).ToArray();
            return sorted[sorted.Length / 2];
        }
    }

    /// <summary>Whether arc length is strictly monotonic.</summary>
    public bool IsArcMonotonic() => V6Geometry.IsArcMonotonic(ArcLength);

    /// <summary>Whether I₁ is invariant (CV less than threshold).</summary>
    public bool IsI1Invariant(double threshold = 0.02) => I1CV < threshold;

    /// <summary>Whether I₂ is invariant (CV less than threshold).</summary>
    public bool IsI2Invariant(double threshold = 0.05) => I2CV < threshold;

    /// <summary>Whether metric is approximately Euclidean.</summary>
    public bool IsMetricEuclidean(double tolerance = 0.1) =>
        Math.Abs(G22Median - 1.0) < tolerance;

    // === Serialization ===

    /// <summary>Export trajectory as CSV with header.</summary>
    public string ToCsv()
    {
        var lines = new List<string> { "Epoch,I1,I2,ArcLength,G22,Omega,Km,DMean" };
        for (int i = 0; i < Epochs; i++)
        {
            double g = i > 0 && i - 1 < G22.Length ? G22[i - 1] : 1.0;
            lines.Add($"{i},{I1[i]:F6},{I2[i]:F6},{ArcLength[i]:F6},{g:F6},{Omega[i]:F6},{Km[i]:F6},{DMean[i]:F6}");
        }
        return string.Join('\n', lines);
    }

    /// <summary>Export summary as JSON string (simplified).</summary>
    public string ToJsonSummary()
    {
        return $"{{\"i1Mean\":{I1Mean:F4},\"i1CV\":{I1CV:F4},\"i2Mean\":{I2Mean:F4},\"i2CV\":{I2CV:F4}," +
               $"\"g22Mean\":{G22Mean:F4},\"g22Median\":{G22Median:F4}," +
               $"\"eccentricity\":{Eccentricity:F4},\"totalArc\":{TotalArcLength:F4}," +
               $"\"monotonic\":{IsArcMonotonic().ToString().ToLower()}," +
               $"\"euclidean\":{IsMetricEuclidean().ToString().ToLower()}}}";
    }
}
