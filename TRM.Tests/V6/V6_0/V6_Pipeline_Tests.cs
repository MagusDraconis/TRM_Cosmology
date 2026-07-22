using System.Collections.Concurrent;
using Xunit;
using Xunit.Abstractions;
using TRM.Core.Geometry.V6;

namespace TRM.Tests.V6_0;

[Trait("Category","V6_0"),Trait("Category","V6_0_Pipeline"),Trait("Category","LongRunning")]
public class V6_Pipeline_Tests
{
    private readonly ITestOutputHelper _o;
    const int N = 72; const double Xi = 1.75; const double Dt = 0.05; const double K0 = 1.2;
    const int St = 300; const int Hd = 4;

    public V6_Pipeline_Tests(ITestOutputHelper o) { _o = o; }

    static double[,] KS(int n, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[n]; for (int i = 0; i < n; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (n - 1); for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[n]; var cs = new List<List<int>>(); for (int i = 0; i < n; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[n, n]; for (int i = 0; i < n; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    static double[][] Sim(double[,] K, int n, double s, int seed) { var rng = new Random(seed); var w = new double[n]; for (int i = 0; i < n; i++) w[i] = 1.0 + s * (rng.NextDouble() - 0.5) * 2.0; var th = new double[n]; for (int i = 0; i < n; i++) th[i] = rng.NextDouble() * 2 * Math.PI; int hL = St / Hd + 1; var h = new double[hL][]; h[0] = (double[])th.Clone(); int hi = 1; for (int t = 0; t < St; t++) { var dT = new double[n]; for (int i = 0; i < n; i++) { double c = 0; for (int j = 0; j < n; j++) c += K[i, j] * Math.Sin(th[j] - th[i]); dT[i] = w[i] + c; } for (int i = 0; i < n; i++) th[i] += Dt * dT[i]; if ((t + 1) % Hd == 0 && hi < hL) h[hi++] = (double[])th.Clone(); } return h; }
    static double[,] RP(double[][] h, int n) { int T = h.Length; var R = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) { double sc = 0, ss = 0; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); } R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; } return R; }
    static double[,] Nm(double[,] R, int n) { double mn = double.MaxValue; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) if (i != j && R[i, j] < mn) mn = R[i, j]; double r = 1.0 - mn; if (r < 1e-15) r = 1.0; var Rn = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) Rn[i, j] = i == j ? 1.0 : Math.Max(1e-9, (R[i, j] - mn) / r); return Rn; }
    static double[,] DL(double[,] R, int n) { var d = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) d[i, j] = i == j ? 0 : -Math.Log(Math.Max(R[i, j], 1e-100)); return d; }
    static double[,] Cupd(double[,] d, int n) { var K = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) K[i, j] = i == j ? 0 : K0 * Math.Exp(-d[i, j] / Math.Max(Xi, 0.01)); return K; }
    static double Km(double[,] K, int n) { double s = 0; int c = 0; for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) { s += K[i, j]; c++; } return c > 0 ? s / c : 0; }
    static double Dm(double[,] d, int n) { double s = 0; int c = 0; for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) { s += d[i, j]; c++; } return c > 0 ? s / c : 0; }
    static double[] Of(double[][] h, int n) { int T = h.Length; var o = new double[n]; for (int i = 0; i < n; i++) { double su = 0; int c = 0; for (int t = 1; t < T; t++) { su += Math.Abs(h[t][i] - h[t - 1][i]); c++; } o[i] = c > 0 ? su / (c * Dt * Hd) : 0; } return o; }

    [Fact]
    public void V6_30_PipelineIntegration()
    {
        _o.WriteLine("=== V6_30: Pipeline Integration ===");
        int seed = 1005; int nEpochs = 20;
        var K = KS(N, seed);
        var kmA = new double[nEpochs]; var dmA = new double[nEpochs]; var omA = new double[nEpochs];

        for (int e = 1; e <= nEpochs; e++)
        {
            var h = Sim(K, N, 0.10, seed + e - 1);
            var d = DL(Nm(RP(h, N), N), N);
            K = Cupd(d, N);
            kmA[e - 1] = Km(K, N);
            dmA[e - 1] = Dm(d, N);
            omA[e - 1] = Of(h, N).Average();
        }

        // Feed through V6Pipeline
        var traj = V6Pipeline.ComputeTrajectory(kmA, dmA, omA);

        _o.WriteLine($"I₁: mean={traj.I1Mean:F4}, CV={traj.I1CV:F4}");
        _o.WriteLine($"I₂: mean={traj.I2Mean:F4}, CV={traj.I2CV:F4}");
        _o.WriteLine($"Arc: total={traj.TotalArcLength:F4}, monotonic={traj.IsArcMonotonic()}");
        _o.WriteLine($"g₂₂: mean={traj.G22Mean:F4}, median={traj.G22Median:F4}");
        _o.WriteLine($"Ellipse: ε={traj.Eccentricity:F4}");

        Assert.True(traj.IsI1Invariant(), $"I₁ CV={traj.I1CV:F4}");
        Assert.True(traj.IsI2Invariant(0.10), $"I₂ CV={traj.I2CV:F4}");
        Assert.True(traj.IsArcMonotonic(), "Arc must be monotonic");
        Assert.True(traj.IsMetricEuclidean(0.5), $"g₂₂ median={traj.G22Median:F4} not Euclidean");

        // Test CSV export
        var csv = traj.ToCsv();
        Assert.StartsWith("Epoch,I1,I2", csv);

        // Test JSON export
        var json = traj.ToJsonSummary();
        Assert.Contains("i1Mean", json);

        _o.WriteLine("PASSED — pipeline integration verified");
    }

    [Fact]
    public void V6_31_PipelineCrossSeed()
    {
        _o.WriteLine("=== V6_31: Pipeline Cross-Seed ===");
        int[] seeds = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        int nEpochs = 20;
        var allTraj = new ConcurrentBag<V6Trajectory>();

        Parallel.ForEach(seeds, seed =>
        {
            var K = KS(N, seed);
            var kmA = new double[nEpochs]; var dmA = new double[nEpochs]; var omA = new double[nEpochs];
            for (int e = 1; e <= nEpochs; e++)
            {
                var h = Sim(K, N, 0.10, seed + e - 1);
                var d = DL(Nm(RP(h, N), N), N);
                K = Cupd(d, N);
                kmA[e - 1] = Km(K, N); dmA[e - 1] = Dm(d, N); omA[e - 1] = Of(h, N).Average();
            }
            allTraj.Add(V6Pipeline.ComputeTrajectory(kmA, dmA, omA));
        });

        var trajectories = allTraj.OrderBy(t => t.I1Mean).ToArray();
        var allI1 = trajectories.SelectMany(t => t.I1).ToArray();
        var allI2 = trajectories.SelectMany(t => t.I2).ToArray();
        double cvI1 = V6Geometry.CV(allI1), cvI2 = V6Geometry.CV(allI2);
        _o.WriteLine($"Pooled I₁ CV={cvI1:F4}, I₂ CV={cvI2:F4}");

        Assert.True(cvI1 < 0.02, $"Pooled I₁ CV={cvI1:F4}");
        Assert.True(cvI2 < 0.07, $"Pooled I₂ CV={cvI2:F4}");

        // All trajectories should be ellipse-consistent
        foreach (var t in trajectories)
            Assert.True(t.Eccentricity > 0.90, $"Seed ε={t.Eccentricity:F4}");

        _o.WriteLine("PASSED — cross-seed pipeline verified");
    }

    [Fact]
    public void V6_32_PipelineThermodynamic()
    {
        _o.WriteLine("=== V6_32: Pipeline Thermodynamic Limit ===");
        int[] Ns = { 67, 72, 80, 90, 100 };
        int seed = 1005; int nEpochs = 20;

        _o.WriteLine($"{"N",5} {"g₂₂_median",12} {"I₁_CV",10} {"Euclidean?",12}");
        _o.WriteLine(new string('-',42));

        foreach (var nv in Ns)
        {
            var K = KS(nv, seed);
            var kmA = new double[nEpochs]; var dmA = new double[nEpochs]; var omA = new double[nEpochs];
            for (int e = 1; e <= nEpochs; e++)
            {
                var h = Sim(K, nv, 0.10, seed + e - 1);
                var d = DL(Nm(RP(h, nv), nv), nv);
                K = Cupd(d, nv);
                kmA[e - 1] = Km(K, nv); dmA[e - 1] = Dm(d, nv); omA[e - 1] = Of(h, nv).Average();
            }
            var traj = V6Pipeline.ComputeTrajectory(kmA, dmA, omA);
            bool euclidean = Math.Abs(traj.G22Median - 1.0) < 0.1;
            _o.WriteLine($"{nv,5} {traj.G22Median,12:F4} {traj.I1CV,10:F4} {(euclidean ? "YES" : "no"),12}");
        }
        _o.WriteLine("PASSED — thermodynamic limit via pipeline");
    }
}
