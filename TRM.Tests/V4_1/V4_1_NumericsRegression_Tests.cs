using Xunit;
using TRM.Core.V4_1.Graphs;

namespace TRM.Tests.V4_1;

/// <summary>
/// Regression tests for dense eigensolver and numerical pipeline stability.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_LaplacianContinuum")]
public class V4_1_NumericsRegression_Tests
{
    [Fact]
    public void V4_1_52_DenseEigensolver_ReturnsSortedNondecreasingEigenvalues()
    {
        var g = GraphFactory.CubicLattice(3);
        var L = GraphMetrics.LaplacianMatrix(g);
        var ev = DenseSymmetricEigenSolver.Eigenvalues(L);
        for (int i = 1; i < ev.Length; i++)
            Assert.True(ev[i] >= ev[i - 1],
                $"Eigenvalues not sorted: ev[{i - 1}]={ev[i - 1]:F6} > ev[{i}]={ev[i]:F6}.");
    }

    [Fact]
    public void V4_1_53_Laplacian_ConstantField_ZeroMode()
    {
        var g = GraphFactory.SquareGrid(4);
        var L = GraphMetrics.LaplacianMatrix(g);
        var ev = DenseSymmetricEigenSolver.Eigenvalues(L);
        // First eigenvalue (zero mode) should be ~0.
        Assert.True(Math.Abs(ev[0]) < 1e-10,
            $"First eigenvalue should be zero mode, got {ev[0]:E3}.");
    }

    [Fact]
    public void V4_1_54_TinyAdjacencyPerturbation_NoNaN()
    {
        var g = GraphFactory.CubicLattice(3);
        var L = GraphMetrics.LaplacianMatrix(g);
        // Perturb one element slightly.
        L[0, 1] += 1e-10;
        L[1, 0] += 1e-10;
        L[0, 0] -= 1e-10;

        var ev = DenseSymmetricEigenSolver.Eigenvalues(L);
        foreach (double e in ev)
            Assert.False(double.IsNaN(e) || double.IsInfinity(e),
                "Eigenvalue should not be NaN/Inf after tiny perturbation.");
    }

    [Fact]
    public void V4_1_55_SpectralPipeline_IsDeterministic()
    {
        var g = GraphFactory.CubicLattice(3);
        double l2a = GraphMetrics.Lambda2(g);
        double l2b = GraphMetrics.Lambda2(g);
        Assert.Equal(l2a, l2b, 12);

        double lma = GraphMetrics.LambdaMax(g);
        double lmb = GraphMetrics.LambdaMax(g);
        Assert.Equal(lma, lmb, 12);
    }

    [Fact]
    public void V4_1_56_SpectralDiagnostics_DifferAcrossDimensionFamilies()
    {
        var graphs = new (int D, GraphTopology g)[]
        {
            (1, GraphFactory.Chain(6)),
            (2, GraphFactory.SquareGrid(4)),
            (3, GraphFactory.CubicLattice(3)),
        };
        var gaps = new Dictionary<int, double>();
        foreach (var (D, g) in graphs)
            gaps[D] = GraphMetrics.Lambda2(g);

        // Spectral gaps should be distinguishable across D.
        Assert.NotEqual(gaps[1], gaps[2], 4);
        Assert.NotEqual(gaps[2], gaps[3], 4);
    }
}
