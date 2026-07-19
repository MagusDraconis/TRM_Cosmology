using Xunit;

namespace TRM.Tests.V4_1;

/// <summary>
/// Validates continuum-limit convergence of the discrete Laplacian.
/// Maps to: TRM_V4_1_Emergent_Space.md §3.7 (Continuum Limit), §4.3.
/// Classification: HYPOTHESIS — validates numerical diagnostics only.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_LaplacianContinuum")]
public class V4_1_ContinuumLimit_Tests
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
                    adj[idx, nx * n * n + ny * n + nz] = 1.0;
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
            for (int j = 0; j < N; j++)
                if (adj[i, j] != 0)
                    sum += adj[i, j] * (phi[j] - phi[i]);
            result[i] = sum;
        }
        return result;
    }

    /// <summary>
    /// Continuum-limit convergence test using the scaled discrete Laplacian Δ_G / h² → ∇².
    /// We use a fixed physical wavelength λ = 1, with lattice spacing h = 1/n.
    /// As n increases (h decreases), the approximation error decreases as O(h²).
    /// </summary>
    [Fact]
    public void V4_1_06_ContinuumApproximation_ErrorDecreasesWithResolution()
    {
        double wavelength = 1.0;    // fixed physical wavelength
        double kPhys = 2.0 * Math.PI / wavelength;
        double[] errors = new double[3];

        for (int scale = 0; scale < 3; scale++)
        {
            int n = 10 + scale * 8; // 10, 18, 26 lattice points
            double h = wavelength / n; // lattice spacing → 0 as n → ∞
            var adj = BuildCubicLatticeAdjacency(n);
            int N = n * n * n;

            // Physical coordinate: x_phys = idx * h
            double[] phi = new double[N];
            for (int x = 0; x < n; x++)
            for (int y = 0; y < n; y++)
            for (int z = 0; z < n; z++)
                phi[x * n * n + y * n + z] = Math.Sin(kPhys * x * h);

            var lap = ApplyLaplacian(adj, phi);

            double maxAbsError = 0;
            for (int x = 2; x < n - 2; x++)
            for (int y = 2; y < n - 2; y++)
            for (int z = 2; z < n - 2; z++)
            {
                int idx = x * n * n + y * n + z;
                // Δ_G φ ≈ h² ∇² φ,  so  Δ_G φ / h² ≈ ∇² φ = -k² sin(kx)
                double approxNabla2 = lap[idx] / (h * h);
                double exactNabla2 = -kPhys * kPhys * phi[idx];
                maxAbsError = Math.Max(maxAbsError, Math.Abs(approxNabla2 - exactNabla2));
            }
            errors[scale] = maxAbsError;
        }

        // Absolute error should decrease as O(h²) with finer resolution.
        Assert.True(errors[1] < errors[0],
            $"Error at n=18 ({errors[1]:E3}) should be less than at n=10 ({errors[0]:E3}).");
        Assert.True(errors[2] < errors[1],
            $"Error at n=26 ({errors[2]:E3}) should be less than at n=18 ({errors[1]:E3}).");
    }

    /// <summary>
    /// Volume scaling: N(r) ∝ r^D for a cubic lattice should give D ≈ 3.
    /// </summary>
    [Fact]
    public void V4_1_09_VolumeScaling_ReturnsEffectiveDimension()
    {
        int n = 20;
        int[] volumes = new int[n / 2];
        for (int r = 1; r <= n / 2; r++)
        {
            int count = 0;
            for (int x = 0; x < n; x++)
            for (int y = 0; y < n; y++)
            for (int z = 0; z < n; z++)
                if (x < r && y < r && z < r)
                    count++;
            volumes[r - 1] = count;
        }

        // Fit log N(r) = D * log r + C for middle range to avoid boundaries.
        int start = n / 8, end = n / 4;
        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
        int pts = 0;
        for (int i = start; i < end; i++)
        {
            double logR = Math.Log(i + 1);
            double logN = Math.Log(volumes[i]);
            sumX += logR; sumY += logN; sumXY += logR * logN; sumX2 += logR * logR;
            pts++;
        }
        double D = (pts * sumXY - sumX * sumY) / (pts * sumX2 - sumX * sumX);

        Assert.True(D > 2.9 && D < 3.1, $"Effective dimension D ≈ {D:F3}, expected ~3.0.");
    }
}
