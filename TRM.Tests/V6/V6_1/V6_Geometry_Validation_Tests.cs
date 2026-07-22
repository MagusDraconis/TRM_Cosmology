using System.Collections.Concurrent;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V6_1;

[Trait("Category","V6_1"),Trait("Category","V6_1_Validation"),Trait("Category","LongRunning")]
public class V6_Geometry_Validation_Tests
{
    private readonly ITestOutputHelper _o;
    const int N = 72; const double Xi = 1.75; const double Dt = 0.05; const double K0 = 1.2;
    const int St = 300; const int Hd = 4;

    public V6_Geometry_Validation_Tests(ITestOutputHelper o) { _o = o; }

    // Copy simulation helpers (replicated for independence)
    static double[,] KS(int n, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[n]; for (int i = 0; i < n; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (n - 1); for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[n]; var cs = new List<List<int>>(); for (int i = 0; i < n; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[n, n]; for (int i = 0; i < n; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    static double[][] Sim(double[,] K, int n, double s, int seed) { var rng = new Random(seed); var w = new double[n]; for (int i = 0; i < n; i++) w[i] = 1.0 + s * (rng.NextDouble() - 0.5) * 2.0; var th = new double[n]; for (int i = 0; i < n; i++) th[i] = rng.NextDouble() * 2 * Math.PI; int hL = St / Hd + 1; var h = new double[hL][]; h[0] = (double[])th.Clone(); int hi = 1; for (int t = 0; t < St; t++) { var dT = new double[n]; for (int i = 0; i < n; i++) { double c = 0; for (int j = 0; j < n; j++) c += K[i, j] * Math.Sin(th[j] - th[i]); dT[i] = w[i] + c; } for (int i = 0; i < n; i++) th[i] += Dt * dT[i]; if ((t + 1) % Hd == 0 && hi < hL) h[hi++] = (double[])th.Clone(); } return h; }
    static double[,] RP(double[][] h, int n) { int T = h.Length; var R = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) { double sc = 0, ss = 0; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); } R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; } return R; }
    static double[,] Nm(double[,] R, int n) { double mn = double.MaxValue; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) if (i != j && R[i, j] < mn) mn = R[i, j]; double r = 1.0 - mn; if (r < 1e-15) r = 1.0; var Rn = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) Rn[i, j] = i == j ? 1.0 : Math.Max(1e-9, (R[i, j] - mn) / r); return Rn; }
    static double[,] DL(double[,] R, int n) { var d = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) d[i, j] = i == j ? 0 : -Math.Log(Math.Max(R[i, j], 1e-100)); return d; }
    static double[,] Cupd(double[,] d, int n) { var K = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) K[i, j] = i == j ? 0 : K0 * Math.Exp(-d[i, j] / Math.Max(Xi, 0.01)); return K; }
    static double Km(double[,] K, int n) { double s = 0; int c = 0; for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) { s += K[i, j]; c++; } return c > 0 ? s / c : 0; }
    static double Dm(double[,] d, int n) { double s = 0; int c = 0; for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) { s += d[i, j]; c++; } return c > 0 ? s / c : 0; }
    static double[] Of(double[][] h, int n) { int T = h.Length; var o = new double[n]; for (int i = 0; i < n; i++) { double su = 0; int c = 0; for (int t = 1; t < T; t++) { su += Math.Abs(h[t][i] - h[t - 1][i]); c++; } o[i] = c > 0 ? su / (c * Dt * Hd) : 0; } return o; }
    static double Sd(double[] v) { double m = v.Average(); return Math.Sqrt(v.Sum(x => (x - m) * (x - m)) / (v.Length - 1)); }

    [Fact]
    public void V6_10_CrossSeedInvariants()
    {
        _o.WriteLine("=== V6_10: Cross-Seed Invariants (seeds 0-9) ===");
        int[] seeds = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        int nEpochs = 20;
        var allI1 = new ConcurrentBag<double>();
        var allI2 = new ConcurrentBag<double>();

        Parallel.ForEach(seeds, seed =>
        {
            var K = KS(N, seed);
            for (int e = 1; e <= nEpochs; e++)
            {
                var h = Sim(K, N, 0.10, seed + e - 1);
                var d = DL(Nm(RP(h, N), N), N);
                K = Cupd(d, N);
                allI1.Add(V6Geometry.ComputeI1(Km(K, N), Dm(d, N)));
                allI2.Add(V6Geometry.ComputeI2(Km(K, N), Of(h, N).Average()));
            }
        });

        var i1a = allI1.ToArray(); var i2a = allI2.ToArray();
        double cvI1 = V6Geometry.CV(i1a), cvI2 = V6Geometry.CV(i2a);
        _o.WriteLine($"I₁: mean={i1a.Average():F4}, CV={cvI1:F4}");
        _o.WriteLine($"I₂: mean={i2a.Average():F4}, CV={cvI2:F4}");
        Assert.True(cvI1 < 0.02, $"Cross-seed I₁ CV={cvI1:F4}");
        Assert.True(cvI2 < 0.07, $"Cross-seed I₂ CV={cvI2:F4}");
        _o.WriteLine("PASSED");
    }

    [Fact]
    public void V6_11_CrossSeedMetric()
    {
        _o.WriteLine("=== V6_11: Cross-Seed Metric (seeds 0-9) ===");
        int[] seeds = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        int nEpochs = 20;
        var allG22 = new ConcurrentBag<double>();

        Parallel.ForEach(seeds, seed =>
        {
            var K = KS(N, seed);
            var i1s = new double[nEpochs]; var i2s = new double[nEpochs];
            for (int e = 1; e <= nEpochs; e++)
            {
                var h = Sim(K, N, 0.10, seed + e - 1);
                var d = DL(Nm(RP(h, N), N), N);
                K = Cupd(d, N);
                i1s[e - 1] = V6Geometry.ComputeI1(Km(K, N), Dm(d, N));
                i2s[e - 1] = V6Geometry.ComputeI2(Km(K, N), Of(h, N).Average());
            }
            var g22 = V6Geometry.ComputeG22Trajectory(i1s, i2s);
            foreach (var g in g22) allG22.Add(g);
        });

        var ga = allG22.ToArray();
        double median = ga.OrderBy(g => g).ElementAt(ga.Length / 2);
        _o.WriteLine($"g₂₂: mean={ga.Average():F4}, median={median:F4}, CV={Sd(ga) / (ga.Average() + 1e-10):F2}");
        // Use median (robust to outliers) for assertion
        Assert.True(Math.Abs(median - 1.0) < 0.5, $"Median g₂₂={median:F4} too far from 1.0");
        _o.WriteLine("PASSED (median-based, robust to seed outliers)");
    }

    [Fact]
    public void V6_12_CrossSeedGeometry()
    {
        _o.WriteLine("=== V6_12: Cross-Seed Geometry (seeds 0-9) ===");
        int[] seeds = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        int nEpochs = 20;

        foreach (var seed in seeds)
        {
            var K = KS(N, seed);
            var i1s = new double[nEpochs]; var i2s = new double[nEpochs];
            for (int e = 1; e <= nEpochs; e++)
            {
                var h = Sim(K, N, 0.10, seed + e - 1);
                var d = DL(Nm(RP(h, N), N), N);
                K = Cupd(d, N);
                i1s[e - 1] = V6Geometry.ComputeI1(Km(K, N), Dm(d, N));
                i2s[e - 1] = V6Geometry.ComputeI2(Km(K, N), Of(h, N).Average());
            }
            var (ecc, ratio, orient) = V6Geometry.ComputeEllipseParams(i1s, i2s);
            _o.WriteLine($"Seed {seed}: ε={ecc:F4}, ratio={ratio:F4}");
            Assert.True(ecc > 0.90, $"Seed {seed} eccentricity {ecc:F4} below 0.90");
        }
        _o.WriteLine("PASSED — all seeds elliptical (ε > 0.90)");
    }
}
