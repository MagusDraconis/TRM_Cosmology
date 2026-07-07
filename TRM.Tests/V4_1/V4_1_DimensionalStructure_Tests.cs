using Xunit;
using TRM.Core.V4_1.Graphs;

namespace TRM.Tests.V4_1;

/// <summary>
/// Validates dimensional scaling trends via shell growth N(r) ~ r^D.
/// Uses small graph sizes; validates trend direction, not exact asymptotics.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_DimensionSelection")]
public class V4_1_DimensionalStructure_Tests
{
    private static double FitExponent(int[] volumes)
    {
        // Log-log linear fit: log N(r) = D * log r + C
        int n = volumes.Length;
        double sx = 0, sy = 0, sxy = 0, sx2 = 0;
        for (int i = 0; i < n; i++)
        {
            double lr = Math.Log(i + 1);
            double lv = Math.Log(Math.Max(volumes[i], 1));
            sx += lr; sy += lv; sxy += lr * lv; sx2 += lr * lr;
        }
        return (n * sxy - sx * sy) / (n * sx2 - sx * sx);
    }

    private static int CenterNode(int n, int D)
    {
        // Approximate center in each dimension for better scaling.
        int half = n / 2;
        return D switch
        {
            1 => half,
            2 => half * n + half,
            3 => (half * n + half) * n + half,
            4 => ((half * n + half) * n + half) * n + half,
            _ => 0
        };
    }

    [Fact]
    public void V4_1_36_ShellGrowth_Chain_IsApproximatelyLinear()
    {
        var g = GraphFactory.Chain(50);
        int center = CenterNode(50, 1);
        var vol = GraphMetrics.ShellGrowth(g, center, 10);
        double D = FitExponent(vol);
        Assert.True(D > 0.7 && D < 1.3, $"Chain D ≈ {D:F2}, expected ~1.");
    }

    [Fact]
    public void V4_1_37_ShellGrowth_Grid2D_IsApproximatelyQuadratic()
    {
        var g = GraphFactory.SquareGrid(20);
        int center = CenterNode(20, 2);
        var vol = GraphMetrics.ShellGrowth(g, center, 10);
        double D = FitExponent(vol);
        Assert.True(D > 1.5 && D < 2.5, $"Square grid D ≈ {D:F2}, expected ~2.");
    }

    [Fact]
    public void V4_1_38_ShellGrowth_Cubic3D_IsApproximatelyCubic()
    {
        var g = GraphFactory.CubicLattice(10);
        int center = CenterNode(10, 3);
        var vol = GraphMetrics.ShellGrowth(g, center, 4);
        double D = FitExponent(vol);
        Assert.True(D > 2.0 && D < 4.0, $"Cubic lattice D ≈ {D:F2}, expected ~3.");
    }

    [Fact]
    public void V4_1_39_ShellGrowth_Hypercubic4D_IsApproximatelyQuartic()
    {
        var g = GraphFactory.Hypercubic4D(5);
        int center = CenterNode(5, 4);
        var vol = GraphMetrics.ShellGrowth(g, center, 3);
        // Use interior shells only (r=1..3) to avoid boundary effects.
        double D = FitExponent(vol);
        // Extremely loose: 4D is hard to measure on small graphs.
        Assert.True(D > 2.0 && D < 6.0, $"Hypercubic D ≈ {D:F2}, expected ~4.");
    }
}
