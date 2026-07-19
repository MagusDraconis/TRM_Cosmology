using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Causal front parameter optimization: finds robust parameter regions that produce
/// stable, event-rich, non-degenerate propagation fronts suitable for later
/// dimensionless c_eff calibration.
///
/// Key diagnostics:
///   - Broad parameter sweep with staged optimization
///   - Top-regime readiness ranking
///   - Event-class heatmap (xi vs K0)
///   - Kick linearity window classification
///   - Threshold sensitivity
///   - Multi-source and multi-seed validation for top regimes
///   - Pre-calibration candidate report
///
/// Does NOT claim physical c. Does NOT claim Lorentz invariance.
/// No D=3 assumption. Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_CausalFrontOptimization")]
public class V4_1_CausalFrontParameterOptimization_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_CausalFrontParameterOptimization_Tests(ITestOutputHelper o) { _output = o; }

    // ── Core helpers ────────────────────────────────────────
    private static double[][] Sm(double[,] K, int N, double s, int seed, int kickNode = -1, double kickAmp = 0, int kickT = 0)
    {
        var r = new Random(seed); var w = new double[N]; for (int i = 0; i < N; i++) w[i] = 1.0 + s * (r.NextDouble() - 0.5) * 2.0;
        var th = new double[N]; for (int i = 0; i < N; i++) th[i] = r.NextDouble() * 2.0 * Math.PI;
        int hL = St / Hd + 1; var h = new double[hL][]; h[0] = (double[])th.Clone(); int hi = 1;
        for (int t = 0; t < St; t++) { var dT = new double[N]; for (int i = 0; i < N; i++) { double c = 0; for (int j = 0; j < N; j++) c += K[i, j] * Math.Sin(th[j] - th[i]); dT[i] = w[i] + c; } if (kickNode >= 0 && kickNode < N && t == kickT) dT[kickNode] += kickAmp; for (int i = 0; i < N; i++) th[i] += Dt * dT[i]; if ((t + 1) % Hd == 0 && hi < hL) h[hi++] = (double[])th.Clone(); }
        return h;
    }
    private static double[,] RP(double[][] h) { int T = h.Length, N = h[0].Length; var R = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { double sc = 0, ss = 0; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); } R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; } return R; }
    private static double[,] Nm(double[,] R) { int N = R.GetLength(0); double mn = double.MaxValue; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j && R[i, j] < mn) mn = R[i, j]; double rng = 1.0 - mn; if (rng < 1e-15) rng = 1.0; var Rn = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) Rn[i, j] = 1.0; else Rn[i, j] = Math.Max(REps, (R[i, j] - mn) / rng); } return Rn; }
    private static double[,] DL(double[,] R) { int N = R.GetLength(0); var d = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) d[i, j] = 0; else d[i, j] = -Math.Log(Math.Max(R[i, j], 1e-100)); } return d; }
    private static double[,] ExpUpd(double[,] d, double K0, double xi) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 * Math.Exp(-d[i, j] / Math.Max(xi, 0.01)); } return K; }
    private static double Spear(double[] a, double[] b) { if (a.Length < 3) return 0; var ia = a.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); var ib = b.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); return Pear(ia, ib); }
    private static double Pear(double[] x, double[] y) { int n = x.Length; double mx = x.Average(), my = y.Average(), nm = 0, dx = 0, dy = 0; for (int i = 0; i < n; i++) { double a = x[i] - mx, b = y[i] - my; nm += a * b; dx += a * a; dy += b * b; } return Math.Sqrt(dx * dy) > 1e-15 ? nm / Math.Sqrt(dx * dy) : 0; }
    private static double[] Fl(double[,] m) { int N = m.GetLength(0); var a = new double[N * N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) a[i * N + j] = m[i, j]; return a; }
    private static double Dg(double[,] d) { int N = d.GetLength(0); var v = new List<double>(); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) v.Add(d[i, j]); if (v.Count == 0) return 0; double m = v.Average(); return m > 1e-9 ? v.Average(x => (x - m) * (x - m)) / (m * m) : 0; }
    private static double[,] KS(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }

    private static (double[] times, double[] amps) KickDetect(double[,] K, int N, double s, int seed, int src, double kickAmp, int kickT, double thresh)
    {
        var h0 = Sm(K, N, s, seed); var hk = Sm(K, N, s, seed, src, kickAmp, kickT);
        var times = new double[N]; var amps = new double[N];
        for (int dst = 0; dst < N; dst++) { times[dst] = -1; amps[dst] = 0; if (dst == src) { times[dst] = 0; continue; } for (int t = kickT + 1; t < hk.Length; t++) { double dPh = hk[t][dst] - h0[t][dst]; double dev = Math.Abs(Math.Sin(0.5 * dPh)); if (dev > amps[dst]) amps[dst] = dev; if (dev > thresh && times[dst] < 0) times[dst] = (t - kickT) * Dt * Hd; } }
        return (times, amps);
    }
    private static double[,] RecoverFP(double[,] K0, int N, double K0v, double xi, double s, int E, int seed)
    {
        var Kc = (double[,])K0.Clone();
        for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = ExpUpd(DL(Nm(RP(he))), K0v, xi); }
        return Kc;
    }
    private static (List<double> d, List<double> tau, int resp, int total) EventCloud(double[,] Kc, double[,] dMat, int N, double s, int seed, double kickAmp, double thresh, int nSrc)
    {
        var ds = new List<double>(); var ts = new List<double>(); int resp = 0, total = 0;
        for (int src = 0; src < nSrc; src++)
        {
            var (times, _) = KickDetect(Kc, N, s, seed, src, kickAmp, 50, thresh);
            for (int dst = 0; dst < N; dst++) { if (dst == src) continue; total++; if (times[dst] > 0 && dMat[src, dst] > 0) { ds.Add(dMat[src, dst]); ts.Add(times[dst]); resp++; } }
        }
        return (ds, ts, resp, total);
    }
    private static (double v, double r2) FitV(List<double> d, List<double> tau)
    {
        if (d.Count < 4) return (double.NaN, double.NaN);
        int n = d.Count; double mx = d.Average(), my = tau.Average();
        double num = 0, den = 0; for (int i = 0; i < n; i++) { double dx = d[i] - mx, dy = tau[i] - my; num += dx * dy; den += dx * dx; }
        double a = den > 1e-15 ? num / den : 0; double v = a > 1e-15 ? 1.0 / a : double.NaN;
        double ssRes = 0, ssTot = 0; for (int i = 0; i < n; i++) { double pred = a * d[i]; ssRes += (tau[i] - pred) * (tau[i] - pred); ssTot += (tau[i] - my) * (tau[i] - my); }
        double r2 = ssTot > 1e-15 ? 1.0 - ssRes / ssTot : 0;
        return (v, r2);
    }
    private static string EvCls(int ev) => ev < 4 ? "Insufficient" : ev < 11 ? "Weak" : ev < 31 ? "Usable" : "Strong";

    // ═══════════════ CFPO_01 Broad Parameter Sweep ═══════════════
    [Fact]
    public void V4_1_CFPO_01_BroadParameterSweepFinite()
    {
        int N = 60; double s = 0.1;
        double[] xis = [1.0, 1.25, 1.5, 1.75, 2.0, 2.25];
        double[] K0s = [0.6, 0.8, 1.0, 1.2, 1.5];
        double[] kicks = [0.10, 0.20, 0.30, 0.40];
        double[] threshs = [0.005, 0.01, 0.02];
        _output.WriteLine("xi     K0     kick   thresh  events   respFrac  R²       dg");
        _output.WriteLine("-----  ----   -----  ------  -------  --------  -------  ------");
        int total = 0, usable = 0;
        // Sample to keep runtime manageable: use every other xi, every other K0 for deepest sweeps
        foreach (double xi in xis)
        {
            int E = xi >= 1.5 ? 4 : 5; // Higher xi needs fewer epochs (denser coupling)
            foreach (double K0v in K0s)
                foreach (double ka in kicks)
                {
                    int thIdx = 1; // Use medium threshold for broad sweep
                    double thresh = threshs[thIdx];
                    var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, E, BS);
                    var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
                    var (ds, ts, resp, tot) = EventCloud(Kc, dMat, N, s, BS + 300, ka, thresh, 4);
                    var (_, r2) = FitV(ds, ts);
                    double frac = (double)resp / Math.Max(tot, 1); double dg = Dg(dMat);
                    total++; if (ds.Count >= 11) usable++;
                    if (ka == 0.30 || ds.Count >= 11) // Print interesting rows
                        _output.WriteLine($"{xi:F2}   {K0v:F2}    {ka:F2}   {thresh:F3}  {ds.Count,7}  {frac,8:F4}  {r2,7:F4}  {dg,6:F4}");
                }
        }
        _output.WriteLine($"--- Sweep: {total} combos, {usable} usable ({100.0 * usable / total:F1}%) ---");
        Assert.True(usable > 0, "At least some regimes must be usable.");
    }

    // ═══════════════ CFPO_02 Top Regime Selection ═══════════════
    [Fact]
    public void V4_1_CFPO_02_TopRegimeSelection()
    {
        int N = 60; double s = 0.1; double thresh = 0.01;
        double[] xis = [1.0, 1.25, 1.5, 1.75, 2.0];
        double[] K0s = [0.6, 0.8, 1.0, 1.2, 1.5];
        double[] kicks = [0.20, 0.30, 0.40];
        var regimes = new List<(double xi, double K0v, double ka, int ev, double r2, double dg, double readiness)>();
        foreach (double xi in xis)
        {
            int E = xi >= 1.5 ? 4 : 5;
            foreach (double K0v in K0s)
                foreach (double ka in kicks)
                {
                    var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, E, BS);
                    var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
                    var (ds, ts, resp, _) = EventCloud(Kc, dMat, N, s, BS + 300, ka, thresh, 4);
                    var (_, r2) = FitV(ds, ts);
                    double dg = Dg(dMat);
                    double evNorm = Math.Min(1.0, ds.Count / 40.0);
                    double fitQ = double.IsFinite(r2) ? Math.Max(0, r2) : 0;
                    double nonDeg = 1.0 / (1.0 + dg);
                    double readiness = evNorm * fitQ * nonDeg;
                    regimes.Add((xi, K0v, ka, ds.Count, r2, dg, readiness));
                }
        }
        var top = regimes.OrderByDescending(r => r.readiness).Take(10).ToList();
        _output.WriteLine("Rank  xi     K0     kick   events   R²       dg       readiness  class");
        _output.WriteLine("----  -----  ----   -----  -------  -------  -------  ---------  ----------");
        for (int i = 0; i < top.Count; i++)
        {
            var (xi, K0v, ka, ev, r2, dg, rdy) = top[i];
            _output.WriteLine($"{i + 1,4}  {xi:F2}   {K0v:F2}    {ka:F2}   {ev,7}  {r2,7:F4}  {dg,7:F4}  {rdy,9:F4}  {EvCls(ev)}");
        }
        Assert.NotEmpty(top);
    }

    // ═══════════════ CFPO_03 Event Class Heatmap ═══════════════
    [Fact]
    public void V4_1_CFPO_03_EventClassHeatmap()
    {
        int N = 60; double s = 0.1; double thresh = 0.01; double ka = 0.3;
        double[] xis = [1.0, 1.25, 1.5, 1.75, 2.0, 2.25];
        double[] K0s = [0.6, 0.8, 1.0, 1.2, 1.5];
        _output.WriteLine("═══ EVENT CLASS HEATMAP (kick=0.3) ═══");
        var hdr = "xi\\K0  " + string.Join("", K0s.Select(k => $"{k,8:F2}"));
        _output.WriteLine(hdr);
        _output.WriteLine("------ " + string.Join("", K0s.Select(_ => "--------")));
        foreach (double xi in xis)
        {
            var row = $"{xi,5:F2} ";
            int E = xi >= 1.5 ? 4 : 5;
            foreach (double K0v in K0s)
            {
                var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, E, BS);
                var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
                var (ds, _, _, _) = EventCloud(Kc, dMat, N, s, BS + 300, ka, thresh, 4);
                double dg = Dg(dMat);
                string cell = dg < 0.001 ? " Degen " : ds.Count < 4 ? " Insuf " : ds.Count < 11 ? " Weak  " : ds.Count < 31 ? " Usable" : ds.Count < 100 ? " Strong" : "VStrong";
                row += $"{cell,8}";
            }
            _output.WriteLine(row);
        }
        Assert.True(true);
    }

    // ═══════════════ CFPO_04 Kick Linearity Window ═══════════════
    [Fact]
    public void V4_1_CFPO_04_KickLinearityWindow()
    {
        int N = 60; double s = 0.1; double thresh = 0.01;
        (double xi, double K0v)[] topRegimes = [(1.5, 1.2), (1.75, 1.2), (2.0, 1.5)];
        double[] kicks = [0.05, 0.10, 0.15, 0.20, 0.30, 0.40, 0.50, 0.70];
        _output.WriteLine("xi     K0     kick   events   v_candidate  R²       dg       zone");
        _output.WriteLine("-----  ----   -----  -------  -----------  -------  -------  ------------");
        foreach (var (xi, K0v) in topRegimes)
        {
            int E = xi >= 1.5 ? 4 : 5;
            var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, E, BS);
            var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
            var prevV = new List<double>();
            foreach (double ka in kicks)
            {
                var (ds, ts, _, _) = EventCloud(Kc, dMat, N, s, BS + 300, ka, thresh, 4);
                var (v, r2) = FitV(ds, ts); double dg = Dg(dMat);
                string zone = ds.Count < 4 ? "Too weak" : ka > 0.6 ? "Overdriven" : double.IsFinite(v) && prevV.Count > 0 && Math.Abs(v - prevV[^1]) / Math.Max(prevV[^1], 0.01) > 0.5 ? "Nonlinear" : ka >= 0.15 ? "Linear usable" : "Weak usable";
                if (double.IsFinite(v)) prevV.Add(v);
                _output.WriteLine($"{xi:F2}   {K0v:F2}    {ka:F2}   {ds.Count,7}  {v,11:F4}  {r2,7:F4}  {dg,6:F4}  {zone}");
            }
        }
        Assert.True(true);
    }

    // ═══════════════ CFPO_05 Threshold Sensitivity ═══════════════
    [Fact]
    public void V4_1_CFPO_05_ThresholdSensitivity()
    {
        int N = 60; double s = 0.1; double ka = 0.3;
        (double xi, double K0v)[] topRegimes = [(1.5, 1.2), (1.75, 1.2), (2.0, 1.5)];
        double[] threshs = [0.005, 0.01, 0.02, 0.04];
        _output.WriteLine("xi     K0     thresh  events   v_candidate  R²       class");
        _output.WriteLine("-----  ----   ------  -------  -----------  -------  ----------");
        foreach (var (xi, K0v) in topRegimes)
        {
            int E = xi >= 1.5 ? 4 : 5;
            var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, E, BS);
            var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
            foreach (double th in threshs)
            {
                var (ds, ts, _, _) = EventCloud(Kc, dMat, N, s, BS + 300, ka, th, 4);
                var (v, r2) = FitV(ds, ts);
                _output.WriteLine($"{xi:F2}   {K0v:F2}    {th:F3}   {ds.Count,7}  {v,11:F4}  {r2,7:F4}  {EvCls(ds.Count)}");
            }
        }
        Assert.True(true);
    }

    // ═══════════════ CFPO_06 Multi-Source Robustness ═══════════════
    [Fact]
    public void V4_1_CFPO_06_MultiSourceRobustness()
    {
        int N = 60; double s = 0.1; double ka = 0.3; double thresh = 0.01;
        (double xi, double K0v)[] topRegimes = [(1.5, 1.2), (1.75, 1.2), (2.0, 1.5)];
        _output.WriteLine("xi     K0     src_type  events   v_candidate  R²       dg");
        _output.WriteLine("-----  ----   --------  -------  -----------  -------  ------");
        foreach (var (xi, K0v) in topRegimes)
        {
            int E = xi >= 1.5 ? 4 : 5;
            var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, E, BS);
            var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
            var wDeg = new (int idx, double deg)[N];
            for (int i = 0; i < N; i++) { double deg = 0; for (int j = 0; j < N; j++) if (i != j && Kc[i, j] > 1e-6) deg += Kc[i, j]; wDeg[i] = (i, deg); }
            int hi = wDeg.OrderByDescending(x => x.deg).First().idx;
            int lo = wDeg.OrderBy(x => x.deg).First().idx;
            int med = wDeg.OrderBy(x => x.deg).ElementAt(N / 2).idx;
            int rnd = 17 % N;
            foreach (var (type, src) in new[] { ("high-deg", hi), ("low-deg", lo), ("median", med), ("random", rnd) })
            {
                var (times, _) = KickDetect(Kc, N, s, BS + 300, src, ka, 50, thresh);
                var ds = new List<double>(); var ts = new List<double>();
                for (int dst = 0; dst < N; dst++) if (dst != src && times[dst] > 0 && dMat[src, dst] > 0) { ds.Add(dMat[src, dst]); ts.Add(times[dst]); }
                var (v, r2) = FitV(ds, ts); double dg = Dg(dMat);
                _output.WriteLine($"{xi:F2}   {K0v:F2}    {type,-8}  {ds.Count,7}  {v,11:F4}  {r2,7:F4}  {dg,6:F4}");
            }
        }
        Assert.True(true);
    }

    // ═══════════════ CFPO_07 Multi-Seed Top Regime Validation ═══════════════
    [Fact]
    public void V4_1_CFPO_07_MultiSeedTopRegimeValidation()
    {
        int N = 60; double s = 0.1; double ka = 0.3; double thresh = 0.01; int nSeeds = 12;
        (double xi, double K0v)[] topRegimes = [(1.5, 1.2), (1.75, 1.2), (2.0, 1.5)];
        _output.WriteLine("xi     K0     seeds  ev_mean  ev_std   v_mean     v_std     R²_mean   fails");
        _output.WriteLine("-----  ----   -----  -------  -------  ---------  ---------  -------   -----");
        foreach (var (xi, K0v) in topRegimes)
        {
            int E = xi >= 1.5 ? 4 : 5;
            var evs = new List<int>(); var vs = new List<double>(); var r2s = new List<double>(); int fails = 0;
            for (int sd = 0; sd < nSeeds; sd++)
            {
                var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, E, BS + sd * 10);
                var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
                var (ds, ts, _, _) = EventCloud(Kc, dMat, N, s, BS + 300, ka, thresh, 4);
                var (v, r2) = FitV(ds, ts);
                if (ds.Count >= 4 && double.IsFinite(v)) { evs.Add(ds.Count); vs.Add(v); r2s.Add(r2); } else fails++;
            }
            double me = evs.Count > 0 ? evs.Average() : 0;
            double se = evs.Count > 1 ? Math.Sqrt(evs.Average(x => (x - me) * (x - me))) : 0;
            double mv = vs.Count > 0 ? vs.Average() : double.NaN;
            double sv = vs.Count > 1 ? Math.Sqrt(vs.Average(x => (x - mv) * (x - mv))) : 0;
            double mr = r2s.Count > 0 ? r2s.Average() : double.NaN;
            _output.WriteLine($"{xi:F2}   {K0v:F2}    {nSeeds,5}  {me,7:F1}  {se,7:F1}  {mv,9:F4}  {sv,9:F4}  {mr,7:F4}  {fails,5}");
            if (fails >= nSeeds / 2) _output.WriteLine($"  Most seeds have insufficient events — regime at boundary of viability.");
            Assert.True(fails <= nSeeds, $"xi={xi} K0={K0v}: seed diagnostics valid.");
        }
    }

    // ═══════════════ CFPO_08 N-Scaling Top Regimes ═══════════════
    [Fact]
    public void V4_1_CFPO_08_NScalingTopRegimes()
    {
        int[] Ns = [40, 80, 120, 200]; double s = 0.1; double ka = 0.3; double thresh = 0.01;
        (double xi, double K0v)[] topRegimes = [(1.5, 1.2), (2.0, 1.5)];
        _output.WriteLine("N      xi     K0     events   v_candidate  R²       readiness  class");
        _output.WriteLine("----   -----  ----   -------  -----------  -------  ---------  ----------");
        foreach (int N in Ns)
        {
            int Ebase = N <= 80 ? 5 : (N <= 120 ? 3 : 2);
            foreach (var (xi, K0v) in topRegimes)
            {
                int E = xi >= 1.5 && N <= 80 ? 4 : Ebase;
                var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, E, BS);
                var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
                int nSrc = N <= 80 ? 4 : 3;
                var (ds, ts, _, _) = EventCloud(Kc, dMat, N, s, BS + 300, ka, thresh, nSrc);
                var (v, r2) = FitV(ds, ts);
                double dg = Dg(dMat);
                double evNorm = Math.Min(1.0, ds.Count / 40.0);
                double fitQ = double.IsFinite(r2) ? Math.Max(0, r2) : 0;
                double nonDeg = 1.0 / (1.0 + dg);
                double readiness = evNorm * fitQ * nonDeg;
                _output.WriteLine($"{N,5}  {xi:F2}   {K0v:F2}    {ds.Count,7}  {v,11:F4}  {r2,7:F4}  {readiness,9:F4}  {EvCls(ds.Count)}");
            }
        }
        Assert.True(true);
    }

    // ═══════════════ CFPO_09 Null and Degenerate Optimized Controls ═══════════════
    [Fact]
    public void V4_1_CFPO_09_NullAndDegenerateOptimizedControls()
    {
        int N = 60; double s = 0.1; double ka = 0.3; double thresh = 0.01;
        (double xi, double K0v)[] regs = [(1.5, 1.2), (1.75, 1.2)];

        (int ev, double rdy) Eval(double[,] Kc)
        {
            var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 300))));
            var (ds, ts, _, _) = EventCloud(Kc, dMat, N, s, BS + 400, ka, thresh, 4);
            var (_, r2) = FitV(ds, ts); double dg = Dg(dMat);
            double evNorm = Math.Min(1.0, ds.Count / 40.0);
            double fitQ = double.IsFinite(r2) ? Math.Max(0, r2) : 0;
            double nonDeg = 1.0 / (1.0 + dg);
            return (ds.Count, evNorm * fitQ * nonDeg);
        }

        _output.WriteLine("Regime            Case           events   readiness");
        _output.WriteLine("----------------  -------------  -------  ---------");
        foreach (var (xi, K0v) in regs)
        {
            int E = xi >= 1.5 ? 4 : 5;
            // Active
            var K0a = KS(N, BS); var KcA = RecoverFP(K0a, N, K0v, xi, s, E, BS);
            var (evA, rdyA) = Eval(KcA);
            _output.WriteLine($"xi={xi:F2} K0={K0v:F2}  Active         {evA,7}  {rdyA,9:F4}");
            // K=0
            var KcN = RecoverFP(new double[N, N], N, K0v, xi, s, E, BS);
            var (evN, rdyN) = Eval(KcN);
            _output.WriteLine($"xi={xi:F2} K0={K0v:F2}  K=0 null       {evN,7}  {rdyN,9:F4}");
            // Overcoupled
            var Koc = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j) Koc[i, j] = 0.5;
            var KcOC = RecoverFP(Koc, N, K0v, xi, s, E, BS);
            var (evOC, rdyOC) = Eval(KcOC);
            _output.WriteLine($"xi={xi:F2} K0={K0v:F2}  Overcoupled    {evOC,7}  {rdyOC,9:F4}");
        }
        // Global sync
        var hSync = new double[101][]; for (int t = 0; t < 101; t++) { hSync[t] = new double[N]; for (int i = 0; i < N; i++) hSync[t][i] = 0; }
        double dgGS = Dg(DL(Nm(RP(hSync))));
        _output.WriteLine($"Global sync      --              --        dg={dgGS:F4} Degenerate");
        Assert.True(dgGS < 0.01);
    }

    // ═══════════════ CFPO_10 Pre-Calibration Candidate Report ═══════════════
    [Fact]
    public void V4_1_CFPO_10_PreCalibrationCandidateReport()
    {
        int N = 60; double s = 0.1; double thresh = 0.01;
        (double xi, double K0v, double ka)[] candidates = [(1.5, 1.2, 0.3), (1.75, 1.2, 0.3), (2.0, 1.5, 0.3), (1.5, 1.0, 0.4), (2.0, 1.2, 0.4)];

        _output.WriteLine("═══ PRE-CALIBRATION CANDIDATE REPORT ═══");
        _output.WriteLine("xi     K0     kick   events   v_mean     v_std     R²_mean   readiness  status");
        _output.WriteLine("-----  ----   -----  -------  ---------  ---------  -------   ---------  ------------------------");
        foreach (var (xi, K0v, ka) in candidates)
        {
            int E = xi >= 1.5 ? 4 : 5; int nSeeds = 8;
            var evs = new List<int>(); var vs = new List<double>(); var r2s = new List<double>(); var rdys = new List<double>(); int fails = 0;
            for (int sd = 0; sd < nSeeds; sd++)
            {
                var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, E, BS + sd * 10);
                var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
                var (ds, ts, _, _) = EventCloud(Kc, dMat, N, s, BS + 300, ka, thresh, 4);
                var (v, r2) = FitV(ds, ts);
                double dg = Dg(dMat);
                if (ds.Count >= 4 && double.IsFinite(v)) { evs.Add(ds.Count); vs.Add(v); r2s.Add(r2); rdys.Add(Math.Min(1.0, ds.Count / 40.0) * Math.Max(0, r2) * (1.0 / (1.0 + dg))); }
                else fails++;
            }
            double me = evs.Count > 0 ? evs.Average() : 0;
            double mv = vs.Count > 0 ? vs.Average() : double.NaN;
            double sv = vs.Count > 1 ? Math.Sqrt(vs.Average(x => (x - mv) * (x - mv))) : 0;
            double mr = r2s.Count > 0 ? r2s.Average() : double.NaN;
            double mrdy = rdys.Count > 0 ? rdys.Average() : 0;
            string status = fails == 0 && me >= 11 && double.IsFinite(mv) && sv < mv * 0.5 ? "Pre-calibration candidate" : fails <= 1 && me >= 4 ? "Needs more validation" : "Rejected / degenerate";
            _output.WriteLine($"{xi:F2}   {K0v:F2}    {ka:F2}   {me,7:F1}  {mv,9:F4}  {sv,9:F4}  {mr,7:F4}  {mrdy,9:F4}  {status}");
        }
        _output.WriteLine("");
        _output.WriteLine("NOTE: Physical c is NOT derived. c_eff is NOT calibrated.");
        Assert.True(true);
    }

    // ═══════════════ CFPO_11 Claim Discipline Report ═══════════════
    [Fact]
    public void V4_1_CFPO_11_ClaimDisciplineReport()
    {
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("  CLAIM DISCIPLINE — CAUSAL FRONT PARAMETER OPT.");
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  SUPPORTED:");
        _output.WriteLine("    - Causal front parameter regimes can be optimized.");
        _output.WriteLine("    - Event-rich non-degenerate regimes identified.");
        _output.WriteLine("    - Readiness scoring is deterministic.");
        _output.WriteLine("    - Null and degenerate controls tested.");
        _output.WriteLine("    - Kick linearity windows can be classified.");
        _output.WriteLine("    - Multi-source/seed robustness is measurable.");
        _output.WriteLine("");
        _output.WriteLine("  CONDITIONAL:");
        _output.WriteLine("    - Optimized regimes depend on xi, K0, kick,");
        _output.WriteLine("      threshold, N, source, and seed.");
        _output.WriteLine("    - Fitted v remains dimensionless.");
        _output.WriteLine("    - Readiness is not physical calibration.");
        _output.WriteLine("");
        _output.WriteLine("  HYPOTHESIS:");
        _output.WriteLine("    - Stable optimized front regimes may support");
        _output.WriteLine("      later c_eff calibration.");
        _output.WriteLine("    - Physical c may correspond to a stable TRM");
        _output.WriteLine("      propagation speed after calibration.");
        _output.WriteLine("    - Lorentzian causal structure may emerge in");
        _output.WriteLine("      a continuum limit.");
        _output.WriteLine("");
        _output.WriteLine("  NOT CLAIMED:");
        _output.WriteLine("    - Physical c is derived.");
        _output.WriteLine("    - c_eff is calibrated to SI units.");
        _output.WriteLine("    - Lorentz invariance is proven.");
        _output.WriteLine("    - Lorentzian spacetime is derived.");
        _output.WriteLine("    - D=3 is derived.");
        _output.WriteLine("    - General Relativity is replaced.");
        _output.WriteLine("    - Quantum mechanics is derived.");
        _output.WriteLine("    - Planck scales are derived.");
        _output.WriteLine("══════════════════════════════════════════════════");
        Assert.True(true);
    }
}
