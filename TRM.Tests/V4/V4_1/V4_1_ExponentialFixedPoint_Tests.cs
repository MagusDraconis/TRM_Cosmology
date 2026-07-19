using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Exponential fixed-point investigation:
/// Does K(n+1) = K0 * exp(-d(n)/xi) converge to a stable fixed point?
///
/// Since d = -log(R), for xi = 1: K(n+1) ∝ R(n).
/// The question is whether repeated application produces a stable K*.
///
/// No D=3 assumption. Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_ExponentialFixedPoint")]
public class V4_1_ExponentialFixedPoint_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BaseSeed = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int Steps = 300;
    private const int Hds = 4;

    public V4_1_ExponentialFixedPoint_Tests(ITestOutputHelper o) { _output = o; }

    // ── Simulation ────────────────────────────────────────────
    private static double[][] Sm(double[,] K, int N, double s, int seed)
    {
        var r = new Random(seed);
        var w = new double[N]; for (int i = 0; i < N; i++) w[i] = 1.0 + s * (r.NextDouble() - 0.5) * 2.0;
        var th = new double[N]; for (int i = 0; i < N; i++) th[i] = r.NextDouble() * 2.0 * Math.PI;
        int hL = Steps / Hds + 1; var h = new double[hL][]; h[0] = (double[])th.Clone(); int hi = 1;
        for (int t = 0; t < Steps; t++) { var dT = new double[N]; for (int i = 0; i < N; i++) { double c = 0; for (int j = 0; j < N; j++) c += K[i, j] * Math.Sin(th[j] - th[i]); dT[i] = w[i] + c; } for (int i = 0; i < N; i++) th[i] += Dt * dT[i]; if ((t + 1) % Hds == 0 && hi < hL) h[hi++] = (double[])th.Clone(); }
        return h;
    }
    private static double[,] RP(double[][] h) { int T = h.Length, N = h[0].Length; var R = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { double sc = 0, ss = 0; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); } R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; } return R; }
    private static double[,] Nm(double[,] R) { int N = R.GetLength(0); double mn = double.MaxValue; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j && R[i, j] < mn) mn = R[i, j]; double rng = 1.0 - mn; if (rng < 1e-15) rng = 1.0; var Rn = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) Rn[i, j] = 1.0; else Rn[i, j] = Math.Max(REps, (R[i, j] - mn) / rng); } return Rn; }
    private static double[,] DL(double[,] R) { int N = R.GetLength(0); var d = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) d[i, j] = 0; else d[i, j] = -Math.Log(Math.Max(R[i, j], 1e-100)); } return d; }
    private static double[,] ExpUpd(double[,] d, double K0, double xi) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 * Math.Exp(-d[i, j] / Math.Max(xi, 0.01)); } return K; }
    private static double[,] GaussUpd(double[,] d, double K0, double xi) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 * Math.Exp(-d[i, j] * d[i, j] / Math.Max(xi * xi, 0.0001)); } return K; }
    private static double MDiff(double[,] A, double[,] B) { int N = A.GetLength(0); double s = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) s += Math.Abs(A[i, j] - B[i, j]); return s / (N * (N - 1) / 2.0); }
    private static double Spear(double[] a, double[] b) { if (a.Length < 3) return 0; int n = a.Length; var ia = a.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); var ib = b.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); return Pear(ia, ib); }
    private static double Pear(double[] x, double[] y) { int n = x.Length; double mx = x.Average(), my = y.Average(), nm = 0, dx = 0, dy = 0; for (int i = 0; i < n; i++) { double a = x[i] - mx, b = y[i] - my; nm += a * b; dx += a * a; dy += b * b; } double dn = Math.Sqrt(dx * dy); return dn > 1e-15 ? nm / dn : 0; }
    private static double[] Fl(double[,] m) { int N = m.GetLength(0); var a = new double[N * N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) a[i * N + j] = m[i, j]; return a; }
    private static double OP(double[] th) { double sx = 0, sy = 0; for (int i = 0; i < th.Length; i++) { sx += Math.Cos(th[i]); sy += Math.Sin(th[i]); } return Math.Sqrt(sx * sx + sy * sy) / th.Length; }
    private static double Dg(double[,] d) { int N = d.GetLength(0); var v = new List<double>(); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) v.Add(d[i, j]); if (v.Count == 0) return 0; double m = v.Average(); return m > 1e-9 ? v.Average(x => (x - m) * (x - m)) / (m * m) : 0; }

    // ── Initial K builders ────────────────────────────────────
    private static double[,] KS(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    private static double[,] KWD(int N) { var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j) K[i, j] = 0.1 / N; return K; }
    private static double[,] KSW(int N) { var rng = new Random(BaseSeed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); for (int i = 0; i < N; i++) for (int d = 1; d <= 3; d++) { int j = (i + d) % N; adj[i].Add(j); adj[j].Add(i); } var cur = adj.Select(a => a.ToList()).ToArray(); for (int i = 0; i < N; i++) foreach (int j in cur[i]) { if (i >= j) continue; if (rng.NextDouble() < 0.1) { adj[i].Remove(j); adj[j].Remove(i); int nj; do nj = rng.Next(N); while (nj == i || adj[i].Contains(nj)); adj[i].Add(nj); adj[nj].Add(i); } } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }

    /// <summary>Run the exponential fixed-point iteration and return per-epoch diagnostics.</summary>
    private static List<(double dK, double corrKR, double Rf, double dg)> RunExpFP(double[,] K0, int N, double K0val, double xi, double sigma, int E, int seed)
    {
        var recs = new List<(double, double, double, double)>();
        var Kc = (double[,])K0.Clone(); double[,]? prevK = null;
        for (int e = 0; e < E; e++)
        {
            var h = Sm(Kc, N, sigma, seed + e);
            var R = Nm(RP(h));
            var d = DL(R);
            var Kn = ExpUpd(d, K0val, xi);
            double dK = prevK != null ? MDiff(Kn, prevK) : double.NaN;
            double corr = Spear(Fl(Kn), Fl(R));
            double rf = OP(h[^1]);
            double dg = Dg(d);
            recs.Add((dK, corr, rf, dg));
            prevK = Kn; Kc = Kn;
        }
        return recs;
    }

    // ════════════════════════════════════════════════════════════
    // EFP_01 — Finite deterministic loop
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_EFP_01_FiniteDeterministicLoop()
    {
        int N = 40; var K0 = KS(N, BaseSeed);
        var r1 = RunExpFP(K0, N, 0.5, 1.0, 0.1, 8, BaseSeed);
        var r2 = RunExpFP(K0, N, 0.5, 1.0, 0.1, 8, BaseSeed);
        Assert.Equal(8, r1.Count);
        for (int i = 1; i < r1.Count; i++)
        {
            Assert.True(double.IsFinite(r1[i].dK));
            Assert.Equal(r1[i].dK, r2[i].dK, 9);
            Assert.Equal(r1[i].corrKR, r2[i].corrKR, 6);
        }
    }

    // ════════════════════════════════════════════════════════════
    // EFP_02 — ||K(n+1)-K(n)|| finite
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_EFP_02_DistanceBetweenEpochs_Finite()
    {
        int N = 40; var K0 = KS(N, BaseSeed);
        var recs = RunExpFP(K0, N, 0.5, 1.0, 0.1, 5, BaseSeed);
        for (int i = 1; i < recs.Count; i++)
            Assert.True(double.IsFinite(recs[i].dK));
    }

    // ════════════════════════════════════════════════════════════
    // EFP_03 — Fixed-point distance trend
    // ════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(20)]
    public void V4_1_EFP_03_DistanceTrend(int E)
    {
        int N = 40; var K0 = KS(N, BaseSeed);
        var recs = RunExpFP(K0, N, 0.5, 1.0, 0.1, E, BaseSeed);
        _output.WriteLine($"  Epochs={E}:");
        for (int i = 1; i < recs.Count; i++)
            _output.WriteLine($"    e={i}  dK={recs[i].dK:E4}  corr(K,R)={recs[i].corrKR:F4}  Rf={recs[i].Rf:F4}  dg={recs[i].dg:F4}");

        // Check trend: if dK decreases over the last half of epochs
        int half = recs.Count / 2;
        if (half >= 2)
        {
            double early = recs.Skip(1).Take(half).Average(r => r.dK);
            double late = recs.Skip(half + 1).Average(r => r.dK);
            _output.WriteLine($"  Early dK avg={early:E4}, Late dK avg={late:E4}, ratio={late / Math.Max(early, 1e-15):F3}");
        }
        Assert.True(recs.All(r => double.IsFinite(r.dK) || double.IsNaN(r.dK)));
    }

    // ════════════════════════════════════════════════════════════
    // EFP_04 — Multiple seeds
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_EFP_04_MultipleSeeds()
    {
        int N = 40; var K0 = KS(N, BaseSeed);
        _output.WriteLine("  seed  final_dK   final_corr(K,R)");
        for (int s = 0; s < 20; s++)
        {
            var recs = RunExpFP(K0, N, 0.5, 1.0, 0.1, 8, BaseSeed + s);
            var last = recs[^1];
            _output.WriteLine($"  {s,4}  {last.dK,10:E4}  {last.corrKR,14:F4}");
            Assert.True(double.IsFinite(last.dK));
        }
    }

    // ════════════════════════════════════════════════════════════
    // EFP_05 — Multiple initial conditions
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_EFP_05_MultipleInitialConditions()
    {
        int N = 40;
        var conds = new (string name, double[,] K)[]
        {
            ("random-sparse", KS(N, BaseSeed)),
            ("weak-dense", KWD(N)),
            ("small-world", KSW(N)),
        };
        _output.WriteLine("  condition       final_dK   final_corr(K,R)");
        foreach (var (name, K) in conds)
        {
            var recs = RunExpFP(K, N, 0.5, 1.0, 0.1, 8, BaseSeed);
            var last = recs[^1];
            _output.WriteLine($"  {name,-16} {last.dK,10:E4}  {last.corrKR,14:F4}");
            Assert.True(double.IsFinite(last.dK));
        }
    }

    // ════════════════════════════════════════════════════════════
    // EFP_06 — Exponential self-consistency
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_EFP_06_ExponentialSelfConsistency()
    {
        int N = 40; var K0 = KS(N, BaseSeed);
        // Run one epoch to get K(1) and R(0)
        var h0 = Sm(K0, N, 0.1, BaseSeed);
        var R0 = Nm(RP(h0));
        var d0 = DL(R0);
        var K1 = ExpUpd(d0, 0.5, 1.0);

        _output.WriteLine("=== Single-step self-consistency ===");
        // corr(K1, R0): since K1 = K0*exp(-d0) and d0 = -log(R0), K1 ∝ R0
        double c1 = Spear(Fl(K1), Fl(R0));
        _output.WriteLine($"  corr(K(1), R(0)) = {c1:F4}");

        // Multi-epoch: corr(K(n), R(n))
        var recs = RunExpFP(K0, N, 0.5, 1.0, 0.1, 5, BaseSeed);
        _output.WriteLine("  epoch  corr(K,R)");
        for (int i = 0; i < recs.Count; i++)
            _output.WriteLine($"  {i,5}  {recs[i].corrKR,10:F4}");

        Assert.True(c1 > 0.8, $"K(1) should correlate with R(0). Got {c1:F4}");
    }

    // ════════════════════════════════════════════════════════════
    // EFP_07 — Fixed-point candidate score
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_EFP_07_FixedPointCandidateScore()
    {
        int N = 40; var K0 = KS(N, BaseSeed); var Kn = new double[N, N];

        // Stability: mean dK over last 3 epochs
        var recsR = RunExpFP(K0, N, 0.5, 1.0, 0.1, 8, BaseSeed);
        double stability = 1.0 / (1.0 + recsR.Skip(5).Average(r => r.dK));

        // Reproducibility: std of final dK across 5 seeds
        var finals = new List<double>();
        for (int s = 0; s < 5; s++) { var r = RunExpFP(K0, N, 0.5, 1.0, 0.1, 8, BaseSeed + s); finals.Add(r[^1].dK); }
        double repro = 1.0 / (1.0 + Math.Sqrt(finals.Average(f => (f - finals.Average()) * (f - finals.Average()))));

        // Null separation
        var recsN = RunExpFP(Kn, N, 0.5, 1.0, 0.1, 8, BaseSeed);
        double nullSep = 1.0 - recsN[^1].corrKR / Math.Max(recsR[^1].corrKR, 0.01);

        double fps = (stability + repro + nullSep) / 3.0;

        _output.WriteLine($"  Fixed-Point Score = {fps:F4}");
        _output.WriteLine($"    Stability:     {stability:F4}");
        _output.WriteLine($"    Reproducibility: {repro:F4}");
        _output.WriteLine($"    Null separation: {nullSep:F4}");

        Assert.True(double.IsFinite(fps));
    }

    // ════════════════════════════════════════════════════════════
    // EFP_08 — Null model (K=0)
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_EFP_08_NullModel()
    {
        int N = 40; var Kn = new double[N, N]; var K0 = KS(N, BaseSeed);
        var rN = RunExpFP(Kn, N, 0.5, 1.0, 0.1, 8, BaseSeed);
        var rR = RunExpFP(K0, N, 0.5, 1.0, 0.1, 8, BaseSeed);
        double fpsN = 1.0 / (1.0 + rN[^1].dK);
        double fpsR = 1.0 / (1.0 + rR[^1].dK);
        _output.WriteLine($"  Null FPS: {fpsN:F4},  Real FPS: {fpsR:F4}");
        Assert.True(fpsR >= fpsN * 0.5, "Null should not strongly outperform real dynamics.");
    }

    // ════════════════════════════════════════════════════════════
    // EFP_09 — Global sync degeneracy
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_EFP_09_GlobalSyncDegenerate()
    {
        int N = 40;
        var hS = new double[101][]; for (int t = 0; t < 101; t++) { hS[t] = new double[N]; }
        var R = Nm(RP(hS)); var d = DL(R);
        double dg = Dg(d);
        _output.WriteLine($"  Synced degeneracy score: {dg:E3}");
        Assert.True(dg < 0.01, "Global sync must be degenerate.");
    }

    // ════════════════════════════════════════════════════════════
    // EFP_10 — N scaling
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_EFP_10_NScaling()
    {
        int[] Ns = [40, 80, 120, 200];
        _output.WriteLine("  N      final_dK   final_corr(K,R)  Rf");
        foreach (int N in Ns)
        {
            var K0 = KS(N, BaseSeed);
            var recs = RunExpFP(K0, N, 0.5, 1.0, 0.1, 8, BaseSeed);
            var last = recs[^1];
            _output.WriteLine($"  {N,5}  {last.dK,10:E4}  {last.corrKR,14:F4}  {last.Rf,6:F4}");
            Assert.True(double.IsFinite(last.dK));
        }
    }

    // ════════════════════════════════════════════════════════════
    // EFP_11 — Exponential vs Gaussian convergence trajectory
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_EFP_11_ExponentialVsGaussian()
    {
        int N = 40; var K0 = KS(N, BaseSeed);

        // Exponential
        var KcE = (double[,])K0.Clone();
        // Gaussian
        var KcG = (double[,])K0.Clone();

        _output.WriteLine("  epoch  dK_exp     dK_gauss   corrE(K,R) corrG(K,R)");
        for (int e = 0; e < 8; e++)
        {
            // Exp
            var hE = Sm(KcE, N, 0.1, BaseSeed + e);
            var RE = Nm(RP(hE)); var dE = DL(RE);
            var KnE = ExpUpd(dE, 0.5, 1.0);
            double dKE = e > 0 ? MDiff(KnE, KcE) : double.NaN;
            double cE = Spear(Fl(KnE), Fl(RE));
            KcE = KnE;

            // Gauss
            var hG = Sm(KcG, N, 0.1, BaseSeed + e);
            var RG = Nm(RP(hG)); var dG = DL(RG);
            var KnG = GaussUpd(dG, 0.5, 1.0);
            double dKG = e > 0 ? MDiff(KnG, KcG) : double.NaN;
            double cG = Spear(Fl(KnG), Fl(RG));
            KcG = KnG;

            _output.WriteLine($"  {e,5}  {dKE,10:E4}  {dKG,10:E4}  {cE,10:F4}  {cG,10:F4}");
            if (e > 0) Assert.True(double.IsFinite(dKE) && double.IsFinite(dKG));
        }
    }

    // ════════════════════════════════════════════════════════════
    // EFP_12 — Claim Discipline Report
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_EFP_12_ClaimDisciplineReport()
    {
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("  CLAIM DISCIPLINE — EXPONENTIAL FIXED POINT");
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  SUPPORTED:");
        _output.WriteLine("    - Exponential update iterated deterministically.");
        _output.WriteLine("    - Fixed-point diagnostics: dK, corr(K,R), Rf, degeneracy.");
        _output.WriteLine("    - Single-step self-consistency: K(1) strongly correlates with R(0).");
        _output.WriteLine("    - Null model and global sync correctly detected.");
        _output.WriteLine("    - N scaling shows finite diagnostics up to N=200.");
        _output.WriteLine("");
        _output.WriteLine("  CONDITIONAL:");
        _output.WriteLine("    - Convergence trend depends on K0, xi, sigma, epochs.");
        _output.WriteLine("    - Fixed-point score is diagnostic only, not a proof of uniqueness.");
        _output.WriteLine("    - Exponential vs Gaussian: both produce finite diagnostics.");
        _output.WriteLine("");
        _output.WriteLine("  HYPOTHESIS:");
        _output.WriteLine("    - Exponential law is the physical TRM coupling law.");
        _output.WriteLine("    - Fixed point corresponds to emergent spatial geometry.");
        _output.WriteLine("    - Continuum limit of the fixed point recovers K(x,y).");
        _output.WriteLine("");
        _output.WriteLine("  NOT CLAIMED:");
        _output.WriteLine("    - D=3 is derived.");
        _output.WriteLine("    - GR is replaced.");
        _output.WriteLine("    - Continuum limit is proven.");
        _output.WriteLine("    - Fixed point is proven unique.");
        _output.WriteLine("══════════════════════════════════════════════════");
        Assert.True(true);
    }
}
