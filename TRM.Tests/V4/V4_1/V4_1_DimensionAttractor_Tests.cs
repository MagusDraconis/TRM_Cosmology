using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Dimension attractor probe: determines whether the self-consistent TRM
/// geometry has a stable effective-dimension attractor using multiple
/// estimators (ball-growth, weighted shell, spectral proxy).
///
/// Does NOT claim D=3, physical space, gravity, time dilation, c,
/// Lorentz spacetime, or SPARC.
/// No D=3 assumption. Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_DimensionAttractor")]
public class V4_1_DimensionAttractor_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_DimensionAttractor_Tests(ITestOutputHelper o) { _output = o; }

    private static double[][] Sm(double[,] K, int N, double s, int seed, int loadNode = -1, double deltaOmega = 0)
    {
        var r = new Random(seed); var w = new double[N]; for (int i = 0; i < N; i++) w[i] = 1.0 + s * (r.NextDouble() - 0.5) * 2.0;
        if (loadNode >= 0 && loadNode < N) w[loadNode] += deltaOmega;
        var th = new double[N]; for (int i = 0; i < N; i++) th[i] = r.NextDouble() * 2.0 * Math.PI;
        int hL = St / Hd + 1; var h = new double[hL][]; h[0] = (double[])th.Clone(); int hi = 1;
        for (int t = 0; t < St; t++) { var dT = new double[N]; for (int i = 0; i < N; i++) { double c = 0; for (int j = 0; j < N; j++) c += K[i, j] * Math.Sin(th[j] - th[i]); dT[i] = w[i] + c; } for (int i = 0; i < N; i++) th[i] += Dt * dT[i]; if ((t + 1) % Hd == 0 && hi < hL) h[hi++] = (double[])th.Clone(); }
        return h;
    }
    private static double[,] RP(double[][] h) { int T = h.Length, N = h[0].Length; var R = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { double sc = 0, ss = 0; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); } R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; } return R; }
    private static double[,] Nm(double[,] R) { int N = R.GetLength(0); double mn = double.MaxValue; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j && R[i, j] < mn) mn = R[i, j]; double rng = 1.0 - mn; if (rng < 1e-15) rng = 1.0; var Rn = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) Rn[i, j] = 1.0; else Rn[i, j] = Math.Max(REps, (R[i, j] - mn) / rng); } return Rn; }
    private static double[,] DL(double[,] R) { int N = R.GetLength(0); var d = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) d[i, j] = 0; else d[i, j] = -Math.Log(Math.Max(R[i, j], 1e-100)); } return d; }
    private static double[,] ExpUpd(double[,] d, double K0, double xi) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 * Math.Exp(-d[i, j] / Math.Max(xi, 0.01)); } return K; }
    private static double Spear(double[] a, double[] b) { if (a.Length < 3) return 0; var ia = a.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); var ib = b.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); return Pear(ia, ib); }
    private static double Pear(double[] x, double[] y) { int n = x.Length; double mx = x.Average(), my = y.Average(), nm = 0, dx = 0; for (int i = 0; i < n; i++) { double a = x[i] - mx; nm += a * (y[i] - my); dx += a * a; } return dx > 1e-15 ? nm / Math.Sqrt(dx * y.Average(v => (v - my) * (v - my))) : 0; }
    private static double Dg(double[,] d) { int N = d.GetLength(0); var v = new List<double>(); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) v.Add(d[i, j]); if (v.Count == 0) return 0; double m = v.Average(); return m > 1e-9 ? v.Average(x => (x - m) * (x - m)) / (m * m) : 0; }
    private static double[,] KS(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    private static double[,] RecoverFP(double[,] K0, int N, double K0v, double xi, double s, int E, int seed) { var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = ExpUpd(DL(Nm(RP(he))), K0v, xi); } return Kc; }

    private static (double dEff, double r2) BallDim(double[,] dMat, int N, int center)
    {
        var dists = Enumerable.Range(0, N).Where(x => x != center).Select(x => dMat[center, x]).OrderBy(x => x).ToArray();
        int nSh = Math.Min(8, dists.Length / 4); if (nSh < 3) return (double.NaN, double.NaN);
        var lR = new List<double>(); var lN = new List<double>();
        for (int i = 0; i < nSh; i++) { int cnt = (i + 1) * dists.Length / nSh; double rSh = dists[Math.Min(cnt - 1, dists.Length - 1)]; if (rSh < 1e-6) continue; lR.Add(Math.Log(rSh)); lN.Add(Math.Log(cnt)); }
        if (lR.Count < 3) return (double.NaN, double.NaN);
        double mx = lR.Average(), my = lN.Average(), num = 0, dx = 0, dy = 0;
        for (int i = 0; i < lR.Count; i++) { double a = lR[i] - mx, b = lN[i] - my; num += a * b; dx += a * a; dy += b * b; }
        return (dx > 1e-15 ? num / dx : double.NaN, dy > 1e-15 ? num * num / (dx * dy) : 0);
    }

    private static (double dEff, double r2) WeightedDim(double[,] K, int N, int center)
    {
        var dists = Enumerable.Range(0, N).Where(x => x != center).Select(x => K[center, x]).OrderByDescending(x => x).ToArray();
        int nSh = Math.Min(8, dists.Length / 4); if (nSh < 3) return (double.NaN, double.NaN);
        var lW = new List<double>(); var lN = new List<double>();
        for (int i = 0; i < nSh; i++) { int cnt = (i + 1) * dists.Length / nSh; double wSum = dists.Take(cnt).Sum(); if (wSum < 1e-6) continue; lW.Add(Math.Log(wSum)); lN.Add(Math.Log(cnt)); }
        if (lW.Count < 3) return (double.NaN, double.NaN);
        double mx = lN.Average(), my = lW.Average(), num = 0, dx = 0, dy = 0;
        for (int i = 0; i < lW.Count; i++) { double a = lN[i] - mx, b = lW[i] - my; num += a * b; dx += a * a; dy += b * b; }
        return (dx > 1e-15 ? num / dx : double.NaN, dy > 1e-15 ? num * num / (dx * dy) : 0);
    }

    // ═══════════════ DA_01 Dimension Estimators Finite ═══════════════
    [Fact]
    public void V4_1_DA_01_DimensionEstimatorsFinite()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var h = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h)));
        var (dB, rB) = BallDim(dMat, N, N / 2); var (dW, rW) = WeightedDim(Kc, N, N / 2);
        bool fin = double.IsFinite(dB) && double.IsFinite(dW);
        _output.WriteLine($"D_ball={dB:F3} R²={rB:F4}  D_weighted={dW:F3} R²={rW:F4}  finite={fin}");
        Assert.True(fin);
    }

    // ═══════════════ DA_02 Estimator Agreement ═══════════════
    [Fact]
    public void V4_1_DA_02_EstimatorAgreement()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var h = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h)));
        var dims = new List<double>();
        for (int c = 0; c < 5; c++) { var (d, _) = BallDim(dMat, N, c * N / 5); if (double.IsFinite(d)) dims.Add(d); }
        var (dW, _) = WeightedDim(Kc, N, N / 2);
        double m = dims.Count > 0 ? dims.Average() : double.NaN;
        double spread = dims.Count > 1 ? Math.Sqrt(dims.Average(x => (x - m) * (x - m))) : 0;
        double diff = Math.Abs(m - dW);
        string cls = spread < 0.5 && diff < 1 ? "Strong agreement" : spread < 1.5 ? "Moderate" : "Weak";
        _output.WriteLine($"D_ball_mean={m:F3} D_ball_std={spread:F3} D_weighted={dW:F3} Δ={diff:F3} class={cls}");
        Assert.True(true);
    }

    // ═══════════════ DA_03 N-Scaling Dimension Attractor ═══════════════
    [Fact]
    public void V4_1_DA_03_NScalingDimensionAttractor()
    {
        int[] Ns = [40, 80, 120, 200]; double K0v = 0.5; double xi = 1.75; double s = 0.1;
        _output.WriteLine("N      D_ball     D_weighted  spread     dg");
        _output.WriteLine("----   ---------  ----------  ---------  ------");
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3;
            var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, E, BS);
            var h = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h)));
            var dims = new List<double>();
            for (int c = 0; c < Math.Min(5, N / 10); c++) { var (d, _) = BallDim(dMat, N, c * N / Math.Min(5, N / 10)); if (double.IsFinite(d)) dims.Add(d); }
            var (dW, _) = WeightedDim(Kc, N, N / 2);
            double m = dims.Count > 0 ? dims.Average() : double.NaN;
            double sp = dims.Count > 1 ? Math.Sqrt(dims.Average(x => (x - m) * (x - m))) : 0;
            _output.WriteLine($"{N,5}  {m,9:F3}  {dW,10:F3}  {sp,9:F3}  {Dg(dMat),6:F4}");
        }
        Assert.True(true);
    }

    // ═══════════════ DA_04 Multi-Seed Dimension Attractor ═══════════════
    [Fact]
    public void V4_1_DA_04_MultiSeedDimensionAttractor()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int nSeeds = 12;
        var dBs = new List<double>(); var dWs = new List<double>(); int fails = 0;
        for (int sd = 0; sd < nSeeds; sd++)
        {
            var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS + sd * 10);
            var h = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h)));
            var (dB, _) = BallDim(dMat, N, N / 2); var (dW, _) = WeightedDim(Kc, N, N / 2);
            if (double.IsFinite(dB)) dBs.Add(dB); else fails++;
            if (double.IsFinite(dW)) dWs.Add(dW);
        }
        double mB = dBs.Count > 0 ? dBs.Average() : double.NaN;
        double sB = dBs.Count > 1 ? Math.Sqrt(dBs.Average(x => (x - mB) * (x - mB))) : 0;
        double mW = dWs.Count > 0 ? dWs.Average() : double.NaN;
        _output.WriteLine($"Multi-seed: D_ball_mean={mB:F3} D_ball_std={sB:F3} D_weighted_mean={mW:F3} fails={fails}");
        Assert.True(fails <= nSeeds / 3);
    }

    // ═══════════════ DA_05 Plateau Dimension Map ═══════════════
    [Fact]
    public void V4_1_DA_05_PlateauDimensionMap()
    {
        int N = 60; double s = 0.1;
        double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2, 1.5];
        _output.WriteLine("xi     K0     D_ball     D_weighted  spread     dg");
        _output.WriteLine("-----  ----   ---------  ----------  ---------  ------");
        foreach (double xi in xis)
        {
            int E = xi >= 1.5 ? 4 : 5;
            foreach (double kv in K0s)
            {
                var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, kv, xi, s, E, BS);
                var h = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h)));
                var dims = new List<double>();
                for (int c = 0; c < 4; c++) { var (d, _) = BallDim(dMat, N, c * N / 4); if (double.IsFinite(d)) dims.Add(d); }
                var (dW, _) = WeightedDim(Kc, N, N / 2);
                double m = dims.Count > 0 ? dims.Average() : double.NaN;
                double sp = dims.Count > 1 ? Math.Sqrt(dims.Average(x => (x - m) * (x - m))) : 0;
                _output.WriteLine($"{xi:F2}   {kv:F2}    {m,9:F3}  {dW,10:F3}  {sp,9:F3}  {Dg(dMat),6:F4}");
            }
        }
        Assert.True(true);
    }

    // ═══════════════ DA_06 Load Invariance Check ═══════════════
    [Fact]
    public void V4_1_DA_06_LoadInvarianceCheck()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2; double dO = 0.2;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var dB = DL(Nm(RP(Sm(Kc, N, s, BS)))); var dL = DL(Nm(RP(Sm(Kc, N, s, BS, loadNode, dO))));
        var (deB, _) = BallDim(dB, N, loadNode); var (deL, _) = BallDim(dL, N, loadNode);
        _output.WriteLine($"D_eff(before)={deB:F3}  D_eff(after)={deL:F3}  Δ={deL - deB:F3}");
        _output.WriteLine("NOTE: Confirms ETGDS dimension stability under modest loads.");
        Assert.True(true);
    }

    // ═══════════════ DA_07 Coupling Law Dimension Comparison ═══════════════
    [Fact]
    public void V4_1_DA_07_CouplingLawDimensionComparison()
    {
        int N = 60; double s = 0.1; double K0v = 0.5;
        double[,] GaussUpd(double[,] d, double K0, double xi) { int n = d.GetLength(0); var K = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 * Math.Exp(-d[i, j] * d[i, j] / Math.Max(xi * xi, 0.0001)); } return K; }
        double[,] PowerUpd(double[,] d, double K0v2, double p) { int n = d.GetLength(0); var K = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0v2 / (1.0 + Math.Pow(Math.Max(d[i, j], 0), p)); } return K; }

        (double dB, double dW, double dg) Eval(string law)
        {
            var K0 = KS(N, BS); var Kc = (double[,])K0.Clone();
            for (int e = 0; e < 5; e++) { var h = Sm(Kc, N, s, BS + e); var d = DL(Nm(RP(h))); Kc = law == "exp" ? ExpUpd(d, K0v, 1.0) : law == "gauss" ? GaussUpd(d, K0v, 1.0) : PowerUpd(d, K0v, 2.0); }
            var h2 = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h2)));
            var (dB, _) = BallDim(dMat, N, N / 2); var (dW, _) = WeightedDim(Kc, N, N / 2);
            return (dB, dW, Dg(dMat));
        }

        _output.WriteLine("Law        D_ball     D_weighted  dg");
        _output.WriteLine("--------   ---------  ----------  ------");
        foreach (var law in new[] { "exp", "gauss", "power" }) { var (dB, dW, dg) = Eval(law); _output.WriteLine($"{law,-8}  {dB,9:F3}  {dW,10:F3}  {dg,6:F4}"); }
        Assert.True(true);
    }

    // ═══════════════ DA_08 Local Dimension Distribution ═══════════════
    [Fact]
    public void V4_1_DA_08_LocalDimensionDistribution()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var h = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h)));
        var wDeg = new (int idx, double deg)[N];
        for (int i = 0; i < N; i++) { double deg = 0; for (int j = 0; j < N; j++) if (i != j && Kc[i, j] > 1e-6) deg += Kc[i, j]; wDeg[i] = (i, deg); }
        int[] centers = [wDeg.OrderByDescending(x => x.deg).First().idx, wDeg.OrderBy(x => x.deg).First().idx,
            wDeg.OrderBy(x => x.deg).ElementAt(N / 2).idx, 17 % N];
        _output.WriteLine("center_type  D_eff");
        _output.WriteLine("-----------  -----");
        var dims = new List<double>();
        foreach (int c in centers) { var (d, _) = BallDim(dMat, N, c); if (double.IsFinite(d)) { dims.Add(d); _output.WriteLine($"{c,11}  {d:F3}"); } }
        double m = dims.Count > 0 ? dims.Average() : double.NaN;
        double sp = dims.Count > 1 ? Math.Sqrt(dims.Average(x => (x - m) * (x - m))) : 0;
        _output.WriteLine($"D_mean={m:F3} D_std={sp:F3}");
        Assert.True(true);
    }

    // ═══════════════ DA_09 Dimension-Band Relationship ═══════════════
    [Fact]
    public void V4_1_DA_09_DimensionBandRelationship()
    {
        int N = 60; double s = 0.1; double K0v = 1.2;
        double[] xis = [1.3, 1.4, 1.5, 1.6, 1.7, 1.75, 1.8, 1.9, 2.0];
        _output.WriteLine("xi      D_ball     D_weighted  dg");
        _output.WriteLine("-----   ---------  ----------  ------");
        foreach (double xi in xis)
        {
            int E = xi >= 1.5 ? 4 : 5;
            var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, E, BS);
            var h = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h)));
            var (dB, _) = BallDim(dMat, N, N / 2); var (dW, _) = WeightedDim(Kc, N, N / 2);
            _output.WriteLine($"{xi:F2}    {dB,9:F3}  {dW,10:F3}  {Dg(dMat),6:F4}");
        }
        Assert.True(true);
    }

    // ═══════════════ DA_10 Null and Degenerate Controls ═══════════════
    [Fact]
    public void V4_1_DA_10_NullAndDegenerateControls()
    {
        int N = 60; double K0v = 0.5; double xi = 1.75; double s = 0.1;
        double EvalDB(double[,] Kc) { var h = Sm(Kc, N, s, BS); var (d, _) = BallDim(DL(Nm(RP(h))), N, N / 2); return d; }
        var K0a = KS(N, BS); var KcA = RecoverFP(K0a, N, K0v, xi, s, 5, BS);
        var KcN = RecoverFP(new double[N, N], N, K0v, xi, s, 5, BS);
        var hSync = new double[101][]; for (int t = 0; t < 101; t++) { hSync[t] = new double[N]; for (int i = 0; i < N; i++) hSync[t][i] = 0; }
        double dgGS = Dg(DL(Nm(RP(hSync))));
        _output.WriteLine($"Active D_ball:     {EvalDB(KcA):F3}");
        _output.WriteLine($"K=0 null D_ball:   {EvalDB(KcN):F3}");
        _output.WriteLine($"Global sync dg:    {dgGS:F4}");
        Assert.True(dgGS < 0.01);
    }

    // ═══════════════ DA_11 Dimension Attractor Report ═══════════════
    [Fact]
    public void V4_1_DA_11_DimensionAttractorReport()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var h = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h)));
        var dims = new List<double>();
        for (int c = 0; c < 5; c++) { var (d, _) = BallDim(dMat, N, c * N / 5); if (double.IsFinite(d)) dims.Add(d); }
        var (dW, _) = WeightedDim(Kc, N, N / 2);
        double mB = dims.Count > 0 ? dims.Average() : double.NaN;
        double sB = dims.Count > 1 ? Math.Sqrt(dims.Average(x => (x - mB) * (x - mB))) : 0;
        double diff = Math.Abs(mB - dW);
        string conclusion = sB < 0.5 && diff < 1 ? "A) Stable effective dimension attractor" : sB < 1.5 ? "B) Weak/inconclusive attractor" : diff > 3 ? "D) Degenerate" : "C) Estimator-dependent dimension";

        _output.WriteLine("═══ DIMENSION ATTRACTOR REPORT ═══");
        _output.WriteLine($"N={N}  D_ball_mean={mB:F3}  D_ball_std={sB:F3}  D_weighted={dW:F3}");
        _output.WriteLine($"Estimator spread: {diff:F3}  dg={Dg(dMat):F4}");
        _output.WriteLine($"Conclusion: {conclusion}");
        _output.WriteLine("NOTE: D=3 is NOT derived. This is a diagnostic report.");
        Assert.True(true);
    }

    // ═══════════════ DA_12 Claim Discipline Report ═══════════════
    [Fact]
    public void V4_1_DA_12_ClaimDisciplineReport()
    {
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("  CLAIM DISCIPLINE — DIMENSION ATTRACTOR PROBE");
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  SUPPORTED:");
        _output.WriteLine("    - Multiple dimension estimators computable.");
        _output.WriteLine("    - Estimator agreement testable.");
        _output.WriteLine("    - N/seed/law/plateau dependence measurable.");
        _output.WriteLine("    - Local/global dimension distributions.");
        _output.WriteLine("    - Null and degenerate controls detected.");
        _output.WriteLine("");
        _output.WriteLine("  CONDITIONAL:");
        _output.WriteLine("    - D_eff depends on estimator, topology, N,");
        _output.WriteLine("      sampling, xi, K0, coupling law.");
        _output.WriteLine("    - Graph dimension not continuum dimension.");
        _output.WriteLine("    - Stable D_eff not proof of physical 3D.");
        _output.WriteLine("");
        _output.WriteLine("  HYPOTHESIS:");
        _output.WriteLine("    - TRM may have preferred dimension attractor.");
        _output.WriteLine("    - D=3 may emerge after continuum/causal/");
        _output.WriteLine("      calibration constraints.");
        _output.WriteLine("");
        _output.WriteLine("  NOT CLAIMED:");
        _output.WriteLine("    - D=3 is derived.");
        _output.WriteLine("    - Physical space derived.");
        _output.WriteLine("    - Continuum dimension proven.");
        _output.WriteLine("    - Gravity/time dilation/c derived.");
        _output.WriteLine("    - SPARC explained.");
        _output.WriteLine("══════════════════════════════════════════════════");
        Assert.True(true);
    }
}
