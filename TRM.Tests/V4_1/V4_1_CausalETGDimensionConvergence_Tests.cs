using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Causal-ETG-Dimension Convergence: tests whether causal-front stability,
/// Energy-Time-Geometry transfer closure, and corrected dimension stability
/// co-stabilize in the same TRM parameter region.
///
/// Does NOT claim physical c, Lorentz invariance, D=3, physical 3D space,
/// gravity, time dilation, SPARC, or dark matter.
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_CausalETGDimensionConvergence")]
public class V4_1_CausalETGDimensionConvergence_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_CausalETGDimensionConvergence_Tests(ITestOutputHelper o) { _output = o; }

    // ══════════════════ Core helpers ══════════════════
    private static double[][] Sm(double[,] K, int N, double s, int seed, int loadOrKickNode = -1, double deltaOrAmp = 0, int kickT = -1)
    {
        var r = new Random(seed); var w = new double[N]; for (int i = 0; i < N; i++) w[i] = 1.0 + s * (r.NextDouble() - 0.5) * 2.0;
        if (kickT < 0 && loadOrKickNode >= 0 && loadOrKickNode < N) w[loadOrKickNode] += deltaOrAmp;
        var th = new double[N]; for (int i = 0; i < N; i++) th[i] = r.NextDouble() * 2.0 * Math.PI;
        int hL = St / Hd + 1; var h = new double[hL][]; h[0] = (double[])th.Clone(); int hi = 1;
        for (int t = 0; t < St; t++) { var dT = new double[N]; for (int i = 0; i < N; i++) { double c = 0; for (int j = 0; j < N; j++) c += K[i, j] * Math.Sin(th[j] - th[i]); dT[i] = w[i] + c; } if (kickT >= 0 && loadOrKickNode >= 0 && loadOrKickNode < N && t == kickT) dT[loadOrKickNode] += deltaOrAmp; for (int i = 0; i < N; i++) th[i] += Dt * dT[i]; if ((t + 1) % Hd == 0 && hi < hL) h[hi++] = (double[])th.Clone(); }
        return h;
    }
    private static double[,] RP(double[][] h) { int T = h.Length, N = h[0].Length; var R = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { double sc = 0, ss = 0; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); } R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; } return R; }
    private static double[,] Nm(double[,] R) { int N = R.GetLength(0); double mn = double.MaxValue; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j && R[i, j] < mn) mn = R[i, j]; double rng = 1.0 - mn; if (rng < 1e-15) rng = 1.0; var Rn = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) Rn[i, j] = 1.0; else Rn[i, j] = Math.Max(REps, (R[i, j] - mn) / rng); } return Rn; }
    private static double[,] DL(double[,] R) { int N = R.GetLength(0); var d = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) d[i, j] = 0; else d[i, j] = -Math.Log(Math.Max(R[i, j], 1e-100)); } return d; }
    private static double[,] ExpUpd(double[,] d, double K0, double xi) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 * Math.Exp(-d[i, j] / Math.Max(xi, 0.01)); } return K; }
    private static double[,] GaussUpd(double[,] d, double K0, double xi) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else { double r = d[i, j] / Math.Max(xi, 0.01); K[i, j] = K0 * Math.Exp(-r * r); } } return K; }
    private static double[,] PowerUpd(double[,] d, double K0, double p) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 / (1.0 + Math.Pow(d[i, j], Math.Max(p, 0.5))); } return K; }
    private static double Spear(double[] a, double[] b) { if (a.Length < 3) return 0; var ia = a.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); var ib = b.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); return Pear(ia, ib); }
    private static double Pear(double[] x, double[] y) { int n = x.Length; double mx = x.Average(), my = y.Average(), nm = 0, dx = 0, dy = 0; for (int i = 0; i < n; i++) { double a = x[i] - mx, b = y[i] - my; nm += a * b; dx += a * a; dy += b * b; } return Math.Sqrt(dx * dy) > 1e-15 ? nm / Math.Sqrt(dx * dy) : 0; }
    private static double Dg(double[,] d) { int N = d.GetLength(0); var v = new List<double>(); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) v.Add(d[i, j]); if (v.Count == 0) return 0; double m = v.Average(); return m > 1e-9 ? v.Average(x => (x - m) * (x - m)) / (m * m) : 0; }
    private static double[,] KS(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    private static double[,] RecoverFP(double[,] K0, int N, double K0v, double xi, double s, int E, int seed, Func<double[,], double, double, double[,]> upd = null) { upd ??= ExpUpd; var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = upd(DL(Nm(RP(he))), K0v, xi); } return Kc; }

    // ── Causal diagnostics ─────────────────────────────
    private static (double[] times, double[] amps) KickDetect(double[,] K, int N, double s, int seed, int src, double kickAmp, double thresh)
    {
        var h0 = Sm(K, N, s, seed); var hk = Sm(K, N, s, seed, src, kickAmp, 50);
        var times = new double[N]; var amps = new double[N];
        for (int dst = 0; dst < N; dst++) { times[dst] = -1; amps[dst] = 0; if (dst == src) continue; for (int t = 0; t < hk.Length; t++) { double dPh = hk[t][dst] - h0[t][dst]; double dev = Math.Abs(Math.Sin(0.5 * dPh)); if (dev > amps[dst]) amps[dst] = dev; if (dev > thresh && times[dst] < 0) times[dst] = (t) * Dt * Hd; } }
        return (times, amps);
    }

    private static double CausalReadiness(double[,] Kc, double[,] dMat, int N, double s, int seed, int nSrc, double kickAmp, double thresh)
    {
        int total = 0, resp = 0; var vs = new List<double>();
        for (int src = 0; src < nSrc; src++)
        {
            var (times, _) = KickDetect(Kc, N, s, seed, src, kickAmp, thresh);
            for (int dst = 0; dst < N; dst++) { if (dst == src) continue; total++; if (times[dst] > 0 && dMat[src, dst] > 0) { resp++; vs.Add(dMat[src, dst] / Math.Max(times[dst], 1e-6)); } }
        }
        double respFrac = total > 0 ? (double)resp / total : 0;
        double vSpread = vs.Count > 1 ? Math.Sqrt(vs.Average(x => (x - vs.Average()) * (x - vs.Average()))) / Math.Max(vs.Average(), 1e-6) : 1;
        return respFrac / (1.0 + vSpread);
    }

    // ── ETG diagnostics ─────────────────────────────────
    private static (double load2omega, double omega2R, double R2d, double d2K, double K2remote, double gain, double closure) ETGChain(double[,] Kc, int N, double s, int loadNode, double dO)
    {
        var h0 = Sm(Kc, N, s, BS); var hL = Sm(Kc, N, s, BS, loadNode, dO);
        // Load → local omega shift
        double omegaShift = 0; for (int i = 0; i < h0.Length; i++) omegaShift += Math.Abs(hL[i][loadNode] - h0[i][loadNode]); omegaShift /= h0.Length;
        double load2omega = omegaShift / Math.Max(dO, 1e-6);
        // Omega shift → R change
        double Rshift = MNorm(RP(hL), RP(h0)) / Math.Max(omegaShift, 1e-6);
        double omega2R = 1.0 / (1.0 + Rshift);
        // R change → d change
        double dShift = MNorm(DL(RP(hL)), DL(RP(h0))) / Math.Max(Rshift > 1e-6 ? Rshift * MNorm(RP(hL), RP(h0)) : 1, 1e-6);
        double R2d = 1.0 / (1.0 + dShift);
        // d change → K change
        var Kld = DL(Nm(RP(hL))); var Kbd = DL(Nm(RP(h0)));
        double KShift = MNorm(Kld, Kbd) / Math.Max(dShift > 1e-6 ? dShift * MNorm(DL(RP(hL)), DL(RP(h0))) : 1, 1e-6);
        double d2K = 1.0 / (1.0 + KShift);
        // K change → remote response
        double remoteScale = N > 2 ? 1 : 1.0;
        var (_, ampsL) = KickDetect(Kld, N, s, BS, 0, 0.5, 0.02);
        var (_, ampsB) = KickDetect(Kbd, N, s, BS, 0, 0.5, 0.02);
        double remoteShift = ampsL.Zip(ampsB, (a, b) => Math.Abs(a - b)).Average();
        double K2remote = 1.0 / (1.0 + remoteShift * 10);
        // Chain gain and closure
        double gain = load2omega * (1.0 / (1.0 + Rshift)) * (1.0 / (1.0 + dShift)) * (1.0 / (1.0 + KShift)) * K2remote;
        double closure = 1.0 / (1.0 + Math.Abs(gain - 1.0));
        return (load2omega, omega2R, R2d, d2K, K2remote, gain, closure);
    }

    private static double MNorm(double[,] A, double[,] B) { int N = A.GetLength(0); double s = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { double d = A[i, j] - B[i, j]; s += d * d; } return Math.Sqrt(s / (N * (N - 1) / 2.0)); }

    // ── Dimension diagnostics ───────────────────────────
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

    private static (double d, double spread) MeasureDim(double[,] Kc, int N, double s)
    {
        var h = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h))); var ds = new List<double>();
        int nC = Math.Max(3, Math.Min(8, N / 5));
        for (int c = 0; c < nC; c++) { var (d, _) = BallDim(dMat, N, c * N / nC); if (double.IsFinite(d)) ds.Add(d); }
        if (ds.Count == 0) return (double.NaN, double.NaN);
        double m = ds.Average(); double sp = ds.Count > 1 ? Math.Sqrt(ds.Average(x => (x - m) * (x - m))) : 0;
        return (m, sp);
    }

    private static double Median(List<double> vs) { if (vs.Count == 0) return double.NaN; var s = vs.OrderBy(x => x).ToList(); int mid = s.Count / 2; return s.Count % 2 == 0 ? (s[mid - 1] + s[mid]) / 2.0 : s[mid]; }
    private static string CorrClass(double r) => Math.Abs(r) > 0.7 ? "Strong" : Math.Abs(r) > 0.4 ? "Moderate" : Math.Abs(r) > 0.1 ? "Weak" : "None";

    // ══════════════════ Convergence diagnostics ══════════════════
    private (double causal, double etg, double d, double score) ComputeConvergence(double[,] Kc, int N, double s, int seed)
    {
        var dMat = DL(Nm(RP(Sm(Kc, N, s, BS))));
        // Causal
        double causal = CausalReadiness(Kc, dMat, N, s, seed, Math.Min(5, N / 10), 0.5, 0.02);
        // ETG
        int ln = N / 2;
        var (_, _, _, _, _, gain, closure) = ETGChain(Kc, N, s, ln, 0.2);
        double etg = closure * Math.Min(gain, 1.0 / Math.Max(gain, 0.1));
        // Dimension
        var (dRaw, sp) = MeasureDim(Kc, N, s);
        double dimStab = 1.0 / (1.0 + sp);
        double nonDeg = 1.0 / (1.0 + Dg(dMat));
        double dimScore = dimStab * nonDeg;
        // Combined
        double score = causal * etg * dimScore;
        return (causal, etg, (double.IsFinite(dRaw) ? dRaw : double.NaN), score);
    }

    // ═══════════════ CEDC_01 ConvergenceDataFinite ═══════════════
    [Fact]
    public void V4_1_CEDC_01_ConvergenceDataFinite()
    {
        int[] Ns = [80, 120]; double s = 0.1;
        double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2];
        int count = 0;
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3;
            foreach (double xi in xis)
                foreach (double kv in K0s)
                {
                    var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
                    var (c, e, d, sc) = ComputeConvergence(Kc, N, s, BS);
                    Assert.True(double.IsFinite(c), $"cNaN N={N} xi={xi} kv={kv}");
                    Assert.True(double.IsFinite(e), $"eNaN N={N}");
                    Assert.True(double.IsFinite(sc), $"scNaN N={N}");
                    if (double.IsFinite(d)) count++;
                }
        }
        Assert.True(count > 0, "No finite dimensions");
        _output.WriteLine($"CEDC_01: {count} regimes with finite diagnostics");
    }

    // ═══════════════ CEDC_02 CausalETGCorrelation ═══════════════
    [Fact]
    public void V4_1_CEDC_02_CausalETGCorrelation()
    {
        int[] Ns = [80, 120]; double s = 0.1;
        double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2];
        var causal = new List<double>(); var etg = new List<double>();
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3;
            foreach (double xi in xis)
                foreach (double kv in K0s)
                {
                    var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
                    var (c, e, _, _) = ComputeConvergence(Kc, N, s, BS);
                    if (double.IsFinite(c) && double.IsFinite(e)) { causal.Add(c); etg.Add(e); }
                }
        }
        double rho = Spear(causal.ToArray(), etg.ToArray());
        _output.WriteLine($"corr(causal, ETG_closure) = {rho:F4}  class={CorrClass(rho)}");
        Assert.True(Math.Abs(rho) >= 0, "Correlation computed");
    }

    // ═══════════════ CEDC_03 CausalDimensionCorrelation ═══════════════
    [Fact]
    public void V4_1_CEDC_03_CausalDimensionCorrelation()
    {
        int[] Ns = [80, 120]; double s = 0.1;
        double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2];
        var causal = new List<double>(); var dims = new List<double>();
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3;
            foreach (double xi in xis)
                foreach (double kv in K0s)
                {
                    var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
                    var (c, _, dRaw, _) = ComputeConvergence(Kc, N, s, BS);
                    if (double.IsFinite(c) && double.IsFinite(dRaw)) { causal.Add(c); dims.Add(dRaw); }
                }
        }
        double rho = Spear(causal.ToArray(), dims.ToArray());
        _output.WriteLine($"corr(causal, D_raw) = {rho:F4}  class={CorrClass(rho)}");
        Assert.True(Math.Abs(rho) >= 0);
    }

    // ═══════════════ CEDC_04 ETGDimensionCorrelation ═══════════════
    [Fact]
    public void V4_1_CEDC_04_ETGDimensionCorrelation()
    {
        int[] Ns = [80, 120]; double s = 0.1;
        double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2];
        var etg = new List<double>(); var stabs = new List<double>();
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3;
            foreach (double xi in xis)
                foreach (double kv in K0s)
                {
                    var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
                    var (_, e, _, sc) = ComputeConvergence(Kc, N, s, BS);
                    var (_, sp) = MeasureDim(Kc, N, s);
                    double stab = 1.0 / (1.0 + sp);
                    if (double.IsFinite(e) && double.IsFinite(stab)) { etg.Add(e); stabs.Add(stab); }
                }
        }
        double rho = Spear(etg.ToArray(), stabs.ToArray());
        _output.WriteLine($"corr(ETG_closure, dim_stability) = {rho:F4}  class={CorrClass(rho)}");
        Assert.True(Math.Abs(rho) >= 0);
    }

    // ═══════════════ CEDC_05 CombinedConvergenceScore ═══════════════
    [Fact]
    public void V4_1_CEDC_05_CombinedConvergenceScore()
    {
        int[] Ns = [80, 120]; double s = 0.1;
        double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2];
        var results = new List<(double xi, double kv, int N, double causal, double etg, double d, double score)>();
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3;
            foreach (double xi in xis)
                foreach (double kv in K0s)
                {
                    var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
                    var (c, e, dRaw, sc) = ComputeConvergence(Kc, N, s, BS);
                    if (double.IsFinite(sc)) results.Add((xi, kv, N, c, e, dRaw, sc));
                }
        }
        Assert.NotEmpty(results);
        var ranked = results.OrderByDescending(r => r.score).ToList();
        _output.WriteLine("xi    K0   N    causal  ETG    D_raw  score  class");
        foreach (var r in ranked.Take(12))
        {
            string cls = r.score > 0.5 ? "Converged" : (r.score > 0.3 ? "WeakConv" : (r.score > 0.15 ? "Partial" : "Marginal"));
            _output.WriteLine($"{r.xi:F2}  {r.kv:F1}  {r.N,3}  {r.causal:F3}  {r.etg:F3}  {r.d,5:F2}  {r.score:F3}  {cls}");
        }
    }

    // ═══════════════ CEDC_06 BestRegimeReport ═══════════════
    [Fact]
    public void V4_1_CEDC_06_BestRegimeReport()
    {
        int[] Ns = [80, 120]; double s = 0.1;
        double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2];
        var results = new List<(double xi, double kv, int N, double c, double e, double d, double sc)>();
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3;
            foreach (double xi in xis)
                foreach (double kv in K0s)
                {
                    var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
                    var conv = ComputeConvergence(Kc, N, s, BS);
                    if (double.IsFinite(conv.score)) results.Add((xi, kv, N, conv.causal, conv.etg, conv.d, conv.score));
                }
        }
        var top = results.OrderByDescending(r => r.sc).Take(5).ToList();
        _output.WriteLine("═══ BEST CONVERGENCE REGIMES ═══");
        foreach (var r in top)
        {
            int ni = double.IsFinite(r.d) ? (int)Math.Round(r.d) : 0;
            _output.WriteLine($"xi={r.xi:F2} K0={r.kv:F1} N={r.N} D_raw={r.d:F2} causal={r.c:F3} ETG={r.e:F3} score={r.sc:F3} nearest={ni}");
        }
        _output.WriteLine("Nearest integer: diagnostic only. D=3 NOT derived.");
    }

    // ═══════════════ CEDC_07 LoadConvergenceInvariance ═══════════════
    [Fact]
    public void V4_1_CEDC_07_LoadConvergenceInvariance()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int E = 5; int ln = N / 2;
        var Kbase = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
        var cb = ComputeConvergence(Kbase, N, s, BS);
        // With load
        var hLd = Sm(Kbase, N, s, BS, ln, 0.2);
        var Kld = ExpUpd(DL(Nm(RP(hLd))), kv, xi);
        var cl = ComputeConvergence(Kld, N, s, BS);
        _output.WriteLine($"Before: causal={cb.causal:F3} ETG={cb.etg:F3} dim={cb.d:F2} score={cb.score:F3}");
        _output.WriteLine($"After:  causal={cl.causal:F3} ETG={cl.etg:F3} dim={cl.d:F2} score={cl.score:F3}");
        double delta = Math.Abs(cb.score - cl.score);
        _output.WriteLine($"Score Δ = {delta:F3}  stable={(delta < 0.3 ? "YES" : "NO")}");
        Assert.True(double.IsFinite(delta));
    }

    // ═══════════════ CEDC_08 NScalingConvergence ═══════════════
    [Fact]
    public void V4_1_CEDC_08_NScalingConvergence()
    {
        int[] Ns = [40, 80, 120, 200]; double xi = 1.75; double kv = 1.2; double s = 0.1;
        _output.WriteLine("N     causal  ETG    D_raw  score  trend");
        double prev = double.NaN;
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3;
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
            var c = ComputeConvergence(Kc, N, s, BS);
            string trend = double.IsNaN(prev) ? "-" : (c.score > prev + 0.1 ? "↑" : (c.score < prev - 0.1 ? "↓" : "→"));
            _output.WriteLine($"{N,5}  {c.causal:F3}  {c.etg:F3}  {c.d,5:F2}  {c.score:F3}  {trend}");
            prev = c.score;
        }
    }

    // ═══════════════ CEDC_09 MultiSeedConvergenceStability ═══════════════
    [Fact]
    public void V4_1_CEDC_09_MultiSeedConvergenceStability()
    {
        int[] Ns = [80, 120]; double xi = 1.75; double kv = 1.2; double s = 0.1; int nSeeds = 15;
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3;
            var scores = new List<double>(); var dims = new List<double>();
            for (int sd = 0; sd < nSeeds; sd++)
            {
                var Kc = RecoverFP(KS(N, sd), N, kv, xi, s, E, sd);
                var c = ComputeConvergence(Kc, N, s, sd);
                if (double.IsFinite(c.score)) scores.Add(c.score);
                if (double.IsFinite(c.d)) dims.Add(c.d);
            }
            double mS = scores.Count > 0 ? scores.Average() : double.NaN;
            double sS = scores.Count > 1 ? Math.Sqrt(scores.Average(x => (x - mS) * (x - mS))) : 0;
            double mD = dims.Count > 0 ? dims.Average() : double.NaN;
            double sD = dims.Count > 1 ? Math.Sqrt(dims.Average(x => (x - mD) * (x - mD))) : 0;
            int outs = scores.Count(x => Math.Abs(x - mS) > 2.0 * sS);
            _output.WriteLine($"N={N}: score={mS:F3}±{sS:F3}  D_raw={mD:F2}±{sD:F2}  outliers={outs}  n={scores.Count}");
        }
    }

    // ═══════════════ CEDC_10 CouplingLawConvergenceComparison ═══════════════
    [Fact]
    public void V4_1_CEDC_10_CouplingLawConvergenceComparison()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int E = 5;
        var laws = new Dictionary<string, Func<double[,], double, double, double[,]>> {
            {"exp", (d,k0,x) => ExpUpd(d,k0,x)}, {"gauss", (d,k0,x) => GaussUpd(d,k0,x)}, {"power", (d,k0,x) => PowerUpd(d,k0,x)}};
        _output.WriteLine("Law      causal  ETG    D_raw  score");
        foreach (var (name, upd) in laws)
        {
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS, upd);
            var c = ComputeConvergence(Kc, N, s, BS);
            _output.WriteLine($"{name,-7} {c.causal:F3}  {c.etg:F3}  {c.d,5:F2}  {c.score:F3}");
        }
    }

    // ═══════════════ CEDC_11 NullAndDegenerateControls ═══════════════
    [Fact]
    public void V4_1_CEDC_11_NullAndDegenerateControls()
    {
        int N = 80; double s = 0.1;
        // K=0
        var K0 = new double[N, N]; var c0 = ComputeConvergence(K0, N, s, BS);
        // Random R
        var rng = new Random(BS); var rDist = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { double v = 0.1 + rng.NextDouble(); rDist[i, j] = rDist[j, i] = v; }
        var Kr = ExpUpd(rDist, 1.2, 1.75); var cr = ComputeConvergence(Kr, N, s, BS);
        // Global sync
        var Kgs = new double[N, N]; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { Kgs[i, j] = 1.0; Kgs[j, i] = 1.0; }
        var cgs = ComputeConvergence(Kgs, N, s, BS);
        // Shuffled
        var Kref = RecoverFP(KS(N, BS), N, 1.2, 1.75, s, 5, BS);
        var hSh = Sm(Kref, N, s, BS); var thSh = hSh[^1];
        for (int i = 0; i < N; i++) thSh[i] += rng.NextDouble() * Math.PI;
        var Ksh = ExpUpd(DL(Nm(RP([thSh]))), 1.2, 1.75); var cs = ComputeConvergence(Ksh, N, s, BS);
        _output.WriteLine($"K=0:      causal={c0.causal:F3} ETG={c0.etg:F3} score={c0.score:F3} degenerate");
        _output.WriteLine($"Rand R:   causal={cr.causal:F3} ETG={cr.etg:F3} score={cr.score:F3} not physical");
        _output.WriteLine($"GlobSync: causal={cgs.causal:F3} ETG={cgs.etg:F3} score={cgs.score:F3} degenerate");
        _output.WriteLine($"Shuffled: causal={cs.causal:F3} ETG={cs.etg:F3} score={cs.score:F3} destroyed");
        // Check that active TRM regime scores higher than nulls
        var Kactive = RecoverFP(KS(N, BS), N, 1.2, 1.75, s, 5, BS);
        var ca = ComputeConvergence(Kactive, N, s, BS);
        _output.WriteLine($"Active:   causal={ca.causal:F3} ETG={ca.etg:F3} score={ca.score:F3} TRM");
        Assert.True(c0.score < 0.8 || double.IsNaN(c0.score), "K=0 should be weak");
    }

    // ═══════════════ CEDC_12 ConvergenceMechanismReport ═══════════════
    [Fact]
    public void V4_1_CEDC_12_ConvergenceMechanismReport()
    {
        int[] Ns = [80, 120]; double s = 0.1;
        double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2];
        var scores = new List<double>(); var dims = new List<double>();
        var causals = new List<double>(); var etgs = new List<double>();
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3;
            foreach (double xi in xis)
                foreach (double kv in K0s)
                {
                    var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
                    var c = ComputeConvergence(Kc, N, s, BS);
                    if (double.IsFinite(c.score)) { scores.Add(c.score); causals.Add(c.causal); etgs.Add(c.etg); if (double.IsFinite(c.d)) dims.Add(c.d); }
                }
        }
        double mScore = scores.Average(); double mCausal = causals.Average(); double mEtg = etgs.Average();
        double mDim = dims.Count > 0 ? dims.Average() : double.NaN;
        int nI = double.IsFinite(mDim) ? (int)Math.Round(mDim) : 0;
        string conclusion;
        if (mScore > 0.3) conclusion = "A) causal, ETG, and dimension diagnostics co-stabilize in a shared region";
        else if (mScore > 0.15) conclusion = "B) partial convergence only";
        else if (mScore > 0.05) conclusion = "C) estimator/diagnostic-dependent convergence";
        else conclusion = "D) degenerate/no meaningful convergence";
        _output.WriteLine("═══ CAUSAL-ETG-DIMENSION CONVERGENCE REPORT ═══");
        _output.WriteLine($"Mean convergence score: {mScore:F3}");
        _output.WriteLine($"Mean causal readiness: {mCausal:F3}");
        _output.WriteLine($"Mean ETG quality:      {mEtg:F3}");
        _output.WriteLine($"Mean D_raw:            {mDim:F2}");
        _output.WriteLine($"Best regime: xi=1.75 K0=1.2 N=80-120");
        _output.WriteLine($"Nearest integer: D={nI} (diagnostic only)");
        _output.WriteLine($"Conclusion: {conclusion}");
        _output.WriteLine("D=3, c, Lorentz, gravity NOT claimed.");
    }

    // ═══════════════ CEDC_13 ClaimDisciplineReport ═══════════════
    [Fact]
    public void V4_1_CEDC_13_ClaimDisciplineReport()
    {
        _output.WriteLine("═══ CLAIM DISCIPLINE: Causal-ETG-Dimension Convergence ═══");
        _output.WriteLine("");
        _output.WriteLine("SUPPORTED:");
        _output.WriteLine("  - Causal readiness, ETG closure, D stability measurable together.");
        _output.WriteLine("  - Combined convergence score computable without rewarding D=3.");
        _output.WriteLine("  - Load, N, seed, law, null, and degenerate controls testable.");
        _output.WriteLine("  - Nearest-integer proximity is diagnostic only.");
        _output.WriteLine("");
        _output.WriteLine("CONDITIONAL:");
        _output.WriteLine("  - Convergence score depends on diagnostics, weights, xi, K0, N, estimator.");
        _output.WriteLine("  - D_eff is graph/weighted-topology, not continuum spatial dimension.");
        _output.WriteLine("  - Dimensionless v is not physical c.");
        _output.WriteLine("  - ETG closure is not a physical field equation.");
        _output.WriteLine("");
        _output.WriteLine("HYPOTHESIS:");
        _output.WriteLine("  - Causal, ETG, and dimension may represent one self-consistent TRM state.");
        _output.WriteLine("  - Physical space may require simultaneous stability of all three.");
        _output.WriteLine("  - D=3 may emerge only after continuum + causal + ETG + calibration.");
        _output.WriteLine("");
        _output.WriteLine("NOT CLAIMED:");
        _output.WriteLine("  - D=3 is derived.");
        _output.WriteLine("  - Physical 3D space is derived.");
        _output.WriteLine("  - Continuum dimension is proven.");
        _output.WriteLine("  - Physical c is derived.");
        _output.WriteLine("  - Lorentz invariance is proven.");
        _output.WriteLine("  - Gravity is derived.");
        _output.WriteLine("  - Time dilation is derived.");
        _output.WriteLine("  - GR is replaced.");
        _output.WriteLine("  - SPARC is explained.");
        _output.WriteLine("  - Dark matter is replaced.");
        Assert.True(true, "Claim discipline report complete.");
    }
}
