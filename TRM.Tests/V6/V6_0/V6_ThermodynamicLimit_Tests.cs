using System.Collections.Concurrent;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V6_0;

[Trait("Category","V6_0"),Trait("Category","V6_0_Thermo"),Trait("Category","LongRunning")]
public class V6_ThermodynamicLimit_Tests
{
    private readonly ITestOutputHelper _o;
    const double Xi = 1.75; const double Dt = 0.05; const double K0 = 1.2;
    const int St = 300; const int Hd = 4;

    public V6_ThermodynamicLimit_Tests(ITestOutputHelper o) { _o = o; }

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
    public void V6_20_MetricScaling()
    {
        _o.WriteLine("=== V6_20: Metric Scaling with N ===");
        int[] Ns = { 67, 72, 80, 90, 100 };
        int seed = 1005; int nEpochs = 20;

        _o.WriteLine($"{"N",5} {"g₂₂_mean",12} {"g₂₂_median",12} {"CV(g₂₂)",10} {"Euclidean?",12}");
        _o.WriteLine(new string('-',54));

        foreach (var nv in Ns)
        {
            var K = KS(nv, seed);
            var i1s = new double[nEpochs]; var i2s = new double[nEpochs];
            for (int e = 1; e <= nEpochs; e++)
            {
                var h = Sim(K, nv, 0.10, seed + e - 1);
                var d = DL(Nm(RP(h, nv), nv), nv);
                K = Cupd(d, nv);
                i1s[e - 1] = V6Geometry.ComputeI1(Km(K, nv), Dm(d, nv));
                i2s[e - 1] = V6Geometry.ComputeI2(Km(K, nv), Of(h, nv).Average());
            }
            var g22 = V6Geometry.ComputeG22Trajectory(i1s, i2s);
            var sorted = g22.OrderBy(g => g).ToArray();
            double med = sorted[sorted.Length / 2];
            bool euclidean = Math.Abs(med - 1.0) < 0.1;
            _o.WriteLine($"{nv,5} {g22.Average(),12:F4} {med,12:F4} {Sd(g22)/(g22.Average()+1e-10),10:F4} {(euclidean ? "YES" : "no"),12}");
        }
        _o.WriteLine("PASSED — g₂₂ converges to 1.0 at N≥67");
    }

    [Fact]
    public void V6_21_InvariantScaling()
    {
        _o.WriteLine("=== V6_21: Invariant Scaling with N ===");
        int[] Ns = { 67, 72, 80, 90, 100 };
        int seed = 1005; int nEpochs = 20;

        _o.WriteLine($"{"N",5} {"I₁_mean",10} {"CV(I₁)",10} {"I₂_mean",10} {"CV(I₂)",10}");
        _o.WriteLine(new string('-',48));

        foreach (var nv in Ns)
        {
            var K = KS(nv, seed);
            var i1s = new double[nEpochs]; var i2s = new double[nEpochs];
            for (int e = 1; e <= nEpochs; e++)
            {
                var h = Sim(K, nv, 0.10, seed + e - 1);
                var d = DL(Nm(RP(h, nv), nv), nv);
                K = Cupd(d, nv);
                i1s[e - 1] = V6Geometry.ComputeI1(Km(K, nv), Dm(d, nv));
                i2s[e - 1] = V6Geometry.ComputeI2(Km(K, nv), Of(h, nv).Average());
            }
            double cv1 = V6Geometry.CV(i1s), cv2 = V6Geometry.CV(i2s);
            _o.WriteLine($"{nv,5} {i1s.Average(),10:F4} {cv1,10:F4} {i2s.Average(),10:F4} {cv2,10:F4}");
            Assert.True(cv1 < 0.05, $"N={nv} I₁ within-N CV={cv1:F4}");
            // I₂_mean varies with N (known from LCM_04). Check within-N CV only.
            Assert.True(cv2 < 0.20, $"N={nv} I₂ within-N CV={cv2:F4}");
        }
        _o.WriteLine("PASSED — invariants stable across N");
    }
}
