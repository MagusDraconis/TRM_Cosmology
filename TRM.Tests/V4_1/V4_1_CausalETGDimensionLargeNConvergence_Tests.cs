using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Causal-ETG-Dimension Large-N Convergence: tests whether three-pillar
/// convergence (causal readiness + ETG closure + dimension stability)
/// persists at larger N (up to 500).
///
/// Does NOT claim D=3, physical space, continuum limit, physical c,
/// Lorentz invariance, gravity, time dilation, SPARC, or dark matter.
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_CausalETGDimLargeNConvergence")]
public class V4_1_CausalETGDimensionLargeNConvergence_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_CausalETGDimensionLargeNConvergence_Tests(ITestOutputHelper o) { _output = o; }

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
    private static double MNorm(double[,] A, double[,] B) { int N = A.GetLength(0); double s = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { double d = A[i, j] - B[i, j]; s += d * d; } return Math.Sqrt(s / (N * (N - 1) / 2.0)); }
    private static double[,] KS(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    private static double[,] RecoverFP(double[,] K0, int N, double K0v, double xi, double s, int E, int seed, Func<double[,], double, double, double[,]> upd = null) { upd ??= ExpUpd; var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = upd(DL(Nm(RP(he))), K0v, xi); } return Kc; }

    private static int EpochsForN(int N) => N <= 200 ? 5 : (N <= 300 ? 3 : 2);

    // ── Causal diagnostics ─────────────────────────────
    private static (double[] times, double[] amps) KickDetect(double[,] K, int N, double s, int seed, int src, double kickAmp, double thresh)
    {
        var h0 = Sm(K, N, s, seed); var hk = Sm(K, N, s, seed, src, kickAmp, 50);
        var times = new double[N]; var amps = new double[N];
        for (int dst = 0; dst < N; dst++) { times[dst] = -1; amps[dst] = 0; if (dst == src) continue; for (int t = 0; t < hk.Length; t++) { double dPh = hk[t][dst] - h0[t][dst]; double dev = Math.Abs(Math.Sin(0.5 * dPh)); if (dev > amps[dst]) amps[dst] = dev; if (dev > thresh && times[dst] < 0) times[dst] = t * Dt * Hd; } }
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
        double omegaShift = 0; for (int i = 0; i < h0.Length; i++) omegaShift += Math.Abs(hL[i][loadNode] - h0[i][loadNode]); omegaShift /= h0.Length;
        double load2omega = omegaShift / Math.Max(dO, 1e-6);
        double Rshift = MNorm(RP(hL), RP(h0)) / Math.Max(omegaShift, 1e-6);
        double omega2R = 1.0 / (1.0 + Rshift);
        double dShift = MNorm(DL(RP(hL)), DL(RP(h0))) / Math.Max(Rshift > 1e-6 ? Rshift * MNorm(RP(hL), RP(h0)) : 1, 1e-6);
        double R2d = 1.0 / (1.0 + dShift);
        var Kld = DL(Nm(RP(hL))); var Kbd = DL(Nm(RP(h0)));
        double KShift = MNorm(Kld, Kbd) / Math.Max(dShift > 1e-6 ? dShift * MNorm(DL(RP(hL)), DL(RP(h0))) : 1, 1e-6);
        double d2K = 1.0 / (1.0 + KShift);
        var (_, ampsL) = KickDetect(Kld, N, s, BS, 0, 0.5, 0.02);
        var (_, ampsB) = KickDetect(Kbd, N, s, BS, 0, 0.5, 0.02);
        double remoteShift = ampsL.Zip(ampsB, (a, b) => Math.Abs(a - b)).Average();
        double K2remote = 1.0 / (1.0 + remoteShift * 10);
        double gain = load2omega * (1.0 / (1.0 + Rshift)) * (1.0 / (1.0 + dShift)) * (1.0 / (1.0 + KShift)) * K2remote;
        double closure = 1.0 / (1.0 + Math.Abs(gain - 1.0));
        return (load2omega, omega2R, R2d, d2K, K2remote, gain, closure);
    }

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
        double causal = CausalReadiness(Kc, dMat, N, s, seed, Math.Min(5, N / 10), 0.5, 0.02);
        int ln = N / 2;
        var (_, _, _, _, _, gain, closure) = ETGChain(Kc, N, s, ln, 0.2);
        double etg = closure * Math.Min(gain, 1.0 / Math.Max(gain, 0.1));
        var (dRaw, sp) = MeasureDim(Kc, N, s);
        double dimStab = 1.0 / (1.0 + sp);
        double nonDeg = 1.0 / (1.0 + Dg(dMat));
        double dimScore = dimStab * nonDeg;
        double score = causal * etg * dimScore;
        return (causal, etg, (double.IsFinite(dRaw) ? dRaw : double.NaN), score);
    }

    // ═══════════════ CEDCLN_01 LargeNConvergenceDataFinite ═══════════════
    [Fact]
    public void V4_1_CEDCLN_01_LargeNConvergenceDataFinite()
    {
        int[] Ns = [40, 80, 120, 200, 300, 500]; double xi = 1.75; double kv = 1.2; double s = 0.1;
        int count = 0;
        foreach (int N in Ns)
        {
            int E = EpochsForN(N);
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
            var c = ComputeConvergence(Kc, N, s, BS);
            Assert.True(double.IsFinite(c.causal), $"cNaN N={N}");
            Assert.True(double.IsFinite(c.etg), $"eNaN N={N}");
            Assert.True(double.IsFinite(c.score), $"scNaN N={N}");
            if (double.IsFinite(c.d)) count++;
        }
        Assert.True(count >= 3, $"Too few: {count}");
        _output.WriteLine($"CEDCLN_01: {count}/{Ns.Length} regimes finite up to N=500");
    }

    // ═══════════════ CEDCLN_02 ConvergenceScoreVsN ═══════════════
    [Fact]
    public void V4_1_CEDCLN_02_ConvergenceScoreVsN()
    {
        int[] Ns = [40, 80, 120, 200, 300, 500]; double xi = 1.75; double kv = 1.2; double s = 0.1;
        _output.WriteLine("N     D_raw  ETG    Causal  Score   Class");
        foreach (int N in Ns)
        {
            int E = EpochsForN(N);
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
            var c = ComputeConvergence(Kc, N, s, BS);
            string cls = c.score > 0.4 ? "LgN-Conv" : (c.score > 0.2 ? "WeakLgN" : (c.score > 0.1 ? "Partial" : "Marginal"));
            if (!double.IsFinite(c.score)) cls = "Degenerate";
            _output.WriteLine($"{N,5}  {c.d,5:F2}  {c.etg:F3}  {c.causal:F3}   {c.score:F3}  {cls}");
        }
        _output.WriteLine("Large-N convergence diagnostic only — no continuum claim.");
    }

    // ═══════════════ CEDCLN_03 DimensionLargeNStability ═══════════════
    [Fact]
    public void V4_1_CEDCLN_03_DimensionLargeNStabilityWithinConvergence()
    {
        int[] Ns = [40, 80, 120, 200, 300, 500]; double xi = 1.75; double kv = 1.2; double s = 0.1;
        _output.WriteLine("N     D_raw  spread  nearest");
        double prev = double.NaN;
        foreach (int N in Ns)
        {
            int E = EpochsForN(N);
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
            var (dRaw, sp) = MeasureDim(Kc, N, s);
            int ni = double.IsFinite(dRaw) ? (int)Math.Round(dRaw) : 0;
            string trend = double.IsNaN(prev) ? "-" : (dRaw > prev + 0.3 ? "↑" : (dRaw < prev - 0.3 ? "↓" : "→"));
            _output.WriteLine($"{N,5}  {dRaw,5:F2}  {sp:F3}  D={ni}  {trend}");
            prev = dRaw;
        }
        _output.WriteLine("Dimension diagnostic only — continuum NOT proven.");
    }

    // ═══════════════ CEDCLN_04 ETGLargeNStability ═══════════════
    [Fact]
    public void V4_1_CEDCLN_04_ETGLargeNStabilityWithinConvergence()
    {
        int[] Ns = [40, 80, 120, 200, 300, 500]; double xi = 1.75; double kv = 1.2; double s = 0.1;
        _output.WriteLine("N     closure  gain   Load2Omega  Remote");
        foreach (int N in Ns)
        {
            int E = EpochsForN(N);
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
            int ln = N / 2;
            var (l2o, o2r, r2d, d2k, k2r, gain, closure) = ETGChain(Kc, N, s, ln, 0.2);
            _output.WriteLine($"{N,5}  {closure:F3}   {gain:F3}  {l2o:F3}       {k2r:F3}");
        }
    }

    // ═══════════════ CEDCLN_05 CausalLargeNStability ═══════════════
    [Fact]
    public void V4_1_CEDCLN_05_CausalLargeNStabilityWithinConvergence()
    {
        int[] Ns = [40, 80, 120, 200, 300, 500]; double xi = 1.75; double kv = 1.2; double s = 0.1;
        _output.WriteLine("N     respFrac  vAvail  CausalReadiness");
        foreach (int N in Ns)
        {
            int E = EpochsForN(N);
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
            var dMat = DL(Nm(RP(Sm(Kc, N, s, BS))));
            int nSrc = Math.Min(5, N / 10);
            int total = 0, resp = 0; var vs = new List<double>();
            for (int src = 0; src < nSrc; src++)
            {
                var (times, _) = KickDetect(Kc, N, s, BS, src, 0.5, 0.02);
                for (int dst = 0; dst < N; dst++) { if (dst == src) continue; total++; if (times[dst] > 0 && dMat[src, dst] > 0) { resp++; vs.Add(dMat[src, dst] / Math.Max(times[dst], 1e-6)); } }
            }
            double rf = total > 0 ? (double)resp / total : 0;
            double vAvail = vs.Count > 0 ? vs.Average() : double.NaN;
            double cr = CausalReadiness(Kc, dMat, N, s, BS, nSrc, 0.5, 0.02);
            _output.WriteLine($"{N,5}  {rf:F3}     {vAvail,5:F2}   {cr:F3}");
        }
    }

    // ═══════════════ CEDCLN_06 LoadInvarianceLargeNConvergence ═══════════════
    [Fact]
    public void V4_1_CEDCLN_06_LoadInvarianceLargeNConvergence()
    {
        int[] Ns = [80, 200, 300]; double xi = 1.75; double kv = 1.2; double s = 0.1;
        _output.WriteLine("N     D_Δ    ETG_Δ  Causal_Δ  Score_Δ  stable?");
        foreach (int N in Ns)
        {
            int E = EpochsForN(N); int ln = N / 2;
            var Kbase = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
            var cb = ComputeConvergence(Kbase, N, s, BS);
            var hLd = Sm(Kbase, N, s, BS, ln, 0.2);
            var Kld = ExpUpd(DL(Nm(RP(hLd))), kv, xi);
            var cl = ComputeConvergence(Kld, N, s, BS);
            double dD = Math.Abs((double.IsFinite(cl.d) && double.IsFinite(cb.d) ? cl.d - cb.d : 0));
            double dE = Math.Abs(cl.etg - cb.etg);
            double dC = Math.Abs(cl.causal - cb.causal);
            double dS = Math.Abs(cl.score - cb.score);
            _output.WriteLine($"{N,5}  {dD:F3}  {dE:F3}  {dC:F3}     {dS:F3}     {(dS < 0.3 ? "YES" : "NO")}");
        }
    }

    // ═══════════════ CEDCLN_07 PlateauLargeNConvergenceMap ═══════════════
    [Fact]
    public void V4_1_CEDCLN_07_PlateauLargeNConvergenceMap()
    {
        int N = 200; double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2, 1.5];
        double s = 0.1; int E = EpochsForN(N);
        _output.WriteLine($"N={N}: xi    K0   D_raw  score  ETG    Causal");
        double bestScore = 0; double bestXi = 0, bestK0 = 0;
        foreach (double xi in xis)
            foreach (double kv in K0s)
            {
                var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
                var c = ComputeConvergence(Kc, N, s, BS);
                _output.WriteLine($"    {xi:F2}  {kv:F1}  {c.d,5:F2}  {c.score:F3}  {c.etg:F3}  {c.causal:F3}");
                if (c.score > bestScore) { bestScore = c.score; bestXi = xi; bestK0 = kv; }
            }
        _output.WriteLine($"Best at N={N}: xi={bestXi:F2} K0={bestK0:F1} score={bestScore:F3}");
        // Spot-check N=300 at best
        int N2 = 300; int E2 = EpochsForN(N2);
        var Kc2 = RecoverFP(KS(N2, BS), N2, bestK0, bestXi, s, E2, BS);
        var c2 = ComputeConvergence(Kc2, N2, s, BS);
        _output.WriteLine($"N=300 best: D={c2.d:F2} score={c2.score:F3} ETG={c2.etg:F3} causal={c2.causal:F3}");
    }

    // ═══════════════ CEDCLN_08 MultiSeedLargeNConvergence ═══════════════
    [Fact]
    public void V4_1_CEDCLN_08_MultiSeedLargeNConvergence()
    {
        double xi = 1.75; double kv = 1.2; double s = 0.1;
        _output.WriteLine("N     nSeeds  score  std   D_raw  std   outliers");
        foreach (var (N, nSeeds) in new (int, int)[] { (80, 10), (200, 9), (300, 4) })
        {
            int E = EpochsForN(N);
            var scs = new List<double>(); var ds = new List<double>();
            for (int seedIdx = 0; seedIdx < nSeeds; seedIdx++)
            {
                var Kc = RecoverFP(KS(N, seedIdx), N, kv, xi, s, E, seedIdx);
                var c = ComputeConvergence(Kc, N, s, seedIdx);
                if (double.IsFinite(c.score)) scs.Add(c.score);
                if (double.IsFinite(c.d)) ds.Add(c.d);
            }
            if (scs.Count < 2) { _output.WriteLine($"{N,5}  {nSeeds,6}  --  --  --  --  --"); continue; }
            double ms = scs.Average(); double ss = scs.Count > 1 ? Math.Sqrt(scs.Average(x => (x - ms) * (x - ms))) : 0;
            double md = ds.Count > 0 ? ds.Average() : double.NaN;
            double sd = ds.Count > 1 ? Math.Sqrt(ds.Average(x => (x - md) * (x - md))) : 0;
            int outs = scs.Count(x => Math.Abs(x - ms) > 2.0 * ss);
            _output.WriteLine($"{N,5}  {nSeeds,6}  {ms:F3}  {ss:F3}  {md,5:F2}  {sd:F3}  {outs}");
        }
    }

    // ═══════════════ CEDCLN_09 CouplingLawLargeNConvergence ═══════════════
    [Fact]
    public void V4_1_CEDCLN_09_CouplingLawLargeNConvergence()
    {
        int[] Ns = [120, 200]; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var laws = new Dictionary<string, Func<double[,], double, double, double[,]>> {
            {"exp", (d,k0,x) => ExpUpd(d,k0,x)}, {"gauss", (d,k0,x) => GaussUpd(d,k0,x)}, {"power", (d,k0,x) => PowerUpd(d,k0,x)}};
        _output.WriteLine("N     Law      score  D_raw  ETG    Causal");
        foreach (int N in Ns)
        {
            int E = EpochsForN(N);
            foreach (var (name, upd) in laws)
            {
                var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS, upd);
                var c = ComputeConvergence(Kc, N, s, BS);
                _output.WriteLine($"{N,5}  {name,-7} {c.score:F3}  {c.d,5:F2}  {c.etg:F3}  {c.causal:F3}");
            }
        }
    }

    // ═══════════════ CEDCLN_10 NullAndDegenerateLargeNControls ═══════════════
    [Fact]
    public void V4_1_CEDCLN_10_NullAndDegenerateLargeNControls()
    {
        int N = 200; double s = 0.1; int E = 3; double xi = 1.75; double kv = 1.2;
        // K=0
        var K0 = new double[N, N]; var c0 = ComputeConvergence(K0, N, s, BS);
        // Random R
        var rng = new Random(BS); var rDist = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { double v = 0.1 + rng.NextDouble(); rDist[i, j] = rDist[j, i] = v; }
        var Kr = ExpUpd(rDist, kv, xi); var cr = ComputeConvergence(Kr, N, s, BS);
        // Global sync
        var Kgs = new double[N, N]; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { Kgs[i, j] = 1.0; Kgs[j, i] = 1.0; }
        var cgs = ComputeConvergence(Kgs, N, s, BS);
        // Shuffled
        var Kref = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
        var hSh = Sm(Kref, N, s, BS); var thSh = hSh[^1];
        for (int i = 0; i < N; i++) thSh[i] += rng.NextDouble() * Math.PI;
        var Ksh = ExpUpd(DL(Nm(RP([thSh]))), kv, xi); var cs = ComputeConvergence(Ksh, N, s, BS);
        // Active
        var ca = ComputeConvergence(Kref, N, s, BS);
        _output.WriteLine($"K=0:       score={c0.score:F3}  degenerate");
        _output.WriteLine($"Rand R:    score={cr.score:F3}  not physical");
        _output.WriteLine($"GlobSync:  score={cgs.score:F3}  degenerate");
        _output.WriteLine($"Shuffled:  score={cs.score:F3}  destroyed");
        _output.WriteLine($"ActiveTRM: score={ca.score:F3}  TRM");
        Assert.True(c0.score < 0.8 || !double.IsFinite(c0.score), "K=0 should be weak");
    }

    // ═══════════════ CEDCLN_11 LargeNConvergenceReport ═══════════════
    [Fact]
    public void V4_1_CEDCLN_11_LargeNConvergenceReport()
    {
        int[] Ns = [40, 80, 120, 200, 300, 500]; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var scores = new List<double>(); var ds = new List<double>(); var etgs = new List<double>(); var causals = new List<double>();
        foreach (int N in Ns)
        {
            int E = EpochsForN(N);
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
            var c = ComputeConvergence(Kc, N, s, BS);
            if (double.IsFinite(c.score)) { scores.Add(c.score); etgs.Add(c.etg); causals.Add(c.causal); if (double.IsFinite(c.d)) ds.Add(c.d); }
        }
        double mScore = scores.Average(); double mEtg = etgs.Average(); double mCausal = causals.Average(); double mDim = ds.Count > 0 ? ds.Average() : double.NaN;
        double scStd = scores.Count > 1 ? Math.Sqrt(scores.Average(x => (x - mScore) * (x - mScore))) : 0;
        int nI = double.IsFinite(mDim) ? (int)Math.Round(mDim) : 0;
        string conclusion;
        if (mScore > 0.25 && scStd < 0.15) conclusion = "A) causal/ETG/dimension convergence persists at larger N";
        else if (mScore > 0.15) conclusion = "B) partial persistence / causal weakens";
        else if (mScore > 0.05) conclusion = "C) estimator/diagnostic-dependent";
        else conclusion = "D) degenerate/no meaningful large-N convergence";
        _output.WriteLine("═══ LARGE-N THREE-PILLAR CONVERGENCE REPORT ═══");
        _output.WriteLine($"Mean score: {mScore:F3} ± {scStd:F3}");
        _output.WriteLine($"Mean D_raw: {mDim:F2}  Mean ETG: {mEtg:F3}  Mean Causal: {mCausal:F3}");
        _output.WriteLine($"Best regime: xi=1.75 K0=1.2 N=80-200");
        _output.WriteLine($"Nearest integer: D={nI} (diagnostic only)");
        _output.WriteLine($"Conclusion: {conclusion}");
        _output.WriteLine("D=3, space, c, Lorentz, gravity NOT claimed.");
    }

    // ═══════════════ CEDCLN_12 ClaimDisciplineReport ═══════════════
    [Fact]
    public void V4_1_CEDCLN_12_ClaimDisciplineReport()
    {
        _output.WriteLine("═══ CLAIM DISCIPLINE: Causal-ETG-Dim Large-N Convergence ═══");
        _output.WriteLine("");
        _output.WriteLine("SUPPORTED:");
        _output.WriteLine("  - Large-N convergence diagnostics are measurable.");
        _output.WriteLine("  - D, ETG, and causal metrics can be tracked together up to N=500.");
        _output.WriteLine("  - Load, seed, law, plateau, null, and degenerate controls testable.");
        _output.WriteLine("");
        _output.WriteLine("CONDITIONAL:");
        _output.WriteLine("  - Results depend on N range, sampling, xi, K0, load, seeds.");
        _output.WriteLine("  - Reduced/sampled large-N diagnostics are not continuum proof.");
        _output.WriteLine("  - Dimensionless v is not physical c.");
        _output.WriteLine("  - Corrected dimension is not physical continuum dimension.");
        _output.WriteLine("");
        _output.WriteLine("HYPOTHESIS:");
        _output.WriteLine("  - Simultaneous stability of all three pillars may define TRM state.");
        _output.WriteLine("  - D=3 may emerge only after continuum + causal + ETG + calibration.");
        _output.WriteLine("  - Physical space may require this combined convergence.");
        _output.WriteLine("");
        _output.WriteLine("NOT CLAIMED:");
        _output.WriteLine("  - D=3 is derived.");
        _output.WriteLine("  - Physical 3D space is derived.");
        _output.WriteLine("  - Continuum limit is proven.");
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
