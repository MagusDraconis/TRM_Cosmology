using Xunit;

namespace TRM.Tests.V4_1;

/// <summary>
/// Validates discrete graph Laplacian properties.
/// Maps to: TRM_V4_1_Emergent_Space.md §4.2 (Graph Laplacian).
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_LaplacianContinuum")]
public class V4_1_GraphLaplacian_Tests
{
    private static double[,] BuildCubicLatticeAdjacency(int n)
    {
        int N = n * n * n;
        var adj = new double[N, N];
        for (int x = 0; x < n; x++)
        for (int y = 0; y < n; y++)
        for (int z = 0; z < n; z++)
        {
            int idx = x * n * n + y * n + z;
            foreach (var (dx, dy, dz) in new (int, int, int)[]
                         { (1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1) })
            {
                int nx = x + dx, ny = y + dy, nz = z + dz;
                if (nx >= 0 && nx < n && ny >= 0 && ny < n && nz >= 0 && nz < n)
                {
                    int nidx = nx * n * n + ny * n + nz;
                    adj[idx, nidx] = 1.0;
                }
            }
        }
        return adj;
    }

    private static double[] ApplyLaplacian(double[,] adj, double[] phi)
    {
        int N = phi.Length;
        var result = new double[N];
        for (int i = 0; i < N; i++)
        {
            double sum = 0;
            double degree = 0;
            for (int j = 0; j < N; j++)
            {
                if (adj[i, j] != 0)
                {
                    sum += adj[i, j] * (phi[j] - phi[i]);
                    degree += adj[i, j];
                }
            }
            result[i] = sum;
        }
        return result;
    }

    [Fact]
    public void V4_1_04_GraphLaplacian_EqualsDegreeMinusAdjacency()
    {
        var adj = new double[,] { { 0, 1, 0 }, { 1, 0, 1 }, { 0, 1, 0 } };
        double[] degree = { 1, 2, 1 };
        double[] phi = { 1.0, 2.0, 3.0 };

        var lap = ApplyLaplacian(adj, phi);
        // Δφ(i) = Σ_j A_ij (φ_j - φ_i) = -(Dφ)_i + (Aφ)_i
        for (int i = 0; i < 3; i++)
        {
            double sumAdjPhi = 0;
            for (int j = 0; j < 3; j++)
                sumAdjPhi += adj[i, j] * phi[j];
            double expected = -degree[i] * phi[i] + sumAdjPhi;
            Assert.Equal(expected, lap[i], 10);
        }
    }

    [Fact]
    public void V4_1_05_DiscreteLaplacian_AnnihilatesConstantField()
    {
        var adj = BuildCubicLatticeAdjacency(3);
        int N = 27;
        double[] constant = Enumerable.Repeat(5.0, N).ToArray();
        var lap = ApplyLaplacian(adj, constant);
        for (int i = 0; i < N; i++)
            Assert.Equal(0.0, lap[i], 12);
    }

    [Fact]
    public void V4_1_06_DiscreteLaplacian_LinearFieldGivesZero()
    {
        // On an infinite regular lattice, ∇²f = 0 for linear f.
        // On a finite cubic lattice, interior points should give ~0.
        var adj = BuildCubicLatticeAdjacency(5);
        int n = 5, N = n * n * n;
        double[] linear = new double[N];
        for (int x = 0; x < n; x++)
        for (int y = 0; y < n; y++)
        for (int z = 0; z < n; z++)
            linear[x * n * n + y * n + z] = 2.0 * x + 3.0 * y + 5.0 * z;

        var lap = ApplyLaplacian(adj, linear);
        // Interior nodes: all 6 neighbours exist → Laplacian ≈ 0.
        for (int x = 1; x < n - 1; x++)
        for (int y = 1; y < n - 1; y++)
        for (int z = 1; z < n - 1; z++)
        {
            int idx = x * n * n + y * n + z;
            Assert.Equal(0.0, lap[idx], 12);
        }
    }

    [Fact]
    public void V4_1_07_DiscreteLaplacian_QuadraticField()
    {
        // ∇²(x²) = 2. On a regular lattice with spacing h:
        // (Δ_G φ)(i) / h² → ∇²φ = 2.
        var adj = BuildCubicLatticeAdjacency(7);
        int n = 7, N = n * n * n;
        double[] quadratic = new double[N];
        for (int x = 0; x < n; x++)
        for (int y = 0; y < n; y++)
        for (int z = 0; z < n; z++)
            quadratic[x * n * n + y * n + z] = x * x;

        var lap = ApplyLaplacian(adj, quadratic);
        double h = 1.0;
        // Interior nodes: (Δ_G φ)/h² ≈ 2.
        for (int x = 2; x < n - 2; x++)
        for (int y = 2; y < n - 2; y++)
        for (int z = 2; z < n - 2; z++)
        {
            int idx = x * n * n + y * n + z;
            double approxLaplacian = lap[idx] / (h * h);
            Assert.Equal(2.0, approxLaplacian, 6);
        }
    }
}
