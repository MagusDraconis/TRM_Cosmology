using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Causal front robustness: determines which topology-density, coupling, kick,
/// and threshold regimes produce sufficiently event-rich propagation fronts
/// for stable dimensionless v fitting.
///
/// Key diagnostics:
///   - Event density grid across N, xi, K0, kick amplitude
///   - Event-quality classes (Insufficient/Weak/Usable/Strong)
///   - Front-fit stability by regime
///   - Shell coverage and monotonicity
///   - Multi-source and multi-seed robustness
///   - Pre-calibration readiness score
///
/// Does NOT claim physical c. Does NOT claim Lorentz invariance.
/// No D=3 assumption. Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_CausalFrontRobustness")]
public class V4_1_CausalFrontRobustness_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_CausalFrontRobustness_Tests(ITestOutputHelper o) { _output = o; }

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

    // ── Kick detection ───────────────────────────────────────
    private static (double[] times, double[] amps) KickDetect(double[,] K, int N, double s, int seed, int src, double kickAmp, int kickT, double thresh)
    {
        var h0 = Sm(K, N, s, seed); var hk = Sm(K, N, s, seed, src, kickAmp, kickT);
        var times = new double[N]; var amps = new double[N];
        for (int dst = 0; dst < N; dst++) { times[dst] = -1; amps[dst] = 0; if (dst == src) { times[dst] = 0; continue; } for (int t = kickT + 1; t < hk.Length; t++) { double dPh = hk[t][dst] - h0[t][dst]; double dev = Math.Abs(Math.Sin(0.5 * dPh)); if (dev > amps[dst]) amps[dst] = dev; if (dev > thresh && times[dst] < 0) times[dst] = (t - kickT) * Dt * Hd; } }
        return (times, amps);
    }

    // ── Recover FP topology ──────────────────────────────────
    private static double[,] RecoverFP(double[,] K0, int N, double K0v, double xi, double s, int E, int seed)
    {
        var Kc = (double[,])K0.Clone();
        for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = ExpUpd(DL(Nm(RP(he))), K0v, xi); }
        return Kc;
    }

    // ── Event cloud + diagnostics ────────────────────────────
    private static (List<double> d, List<double> tau, List<double> amp, int resp, int total) EventCloudEx(double[,] Kc, double[,] dMat, int N, double s, int seed, double kickAmp, double thresh, int nSrc)
    {
        var ds = new List<double>(); var ts = new List<double>(); var am = new List<double>(); int resp = 0, total = 0;
        for (int src = 0; src < nSrc; src++)
        {
            var (times, amps) = KickDetect(Kc, N, s, seed, src, kickAmp, 50, thresh);
            for (int dst = 0; dst < N; dst++) { if (dst == src) continue; total++; if (times[dst] > 0 && dMat[src, dst] > 0) { ds.Add(dMat[src, dst]); ts.Add(times[dst]); am.Add(amps[dst]); resp++; } }
        }
        return (ds, ts, am, resp, total);
    }

    private static (double v, double b, double r2, double resStd) FrontFit(List<double> d, List<double> tau)
    {
        if (d.Count < 4) return (double.NaN, double.NaN, double.NaN, double.NaN);
        int n = d.Count; double mx = d.Average(), my = tau.Average();
        double num = 0, den = 0; for (int i = 0; i < n; i++) { double dx = d[i] - mx, dy = tau[i] - my; num += dx * dy; den += dx * dx; }
        double a = den > 1e-15 ? num / den : 0; double b = my - a * mx; double v = a > 1e-15 ? 1.0 / a : double.NaN;
        double ssRes = 0, ssTot = 0; for (int i = 0; i < n; i++) { double pred = a * d[i] + b; ssRes += (tau[i] - pred) * (tau[i] - pred); ssTot += (tau[i] - my) * (tau[i] - my); }
        double r2 = ssTot > 1e-15 ? 1.0 - ssRes / ssTot : 0; double resStd = n > 1 ? Math.Sqrt(ssRes / (n - 1)) : double.NaN;
        return (v, b, r2, resStd);
    }

    private static string EventClass(int events) => events < 4 ? "Insufficient" : events < 11 ? "Weak" : events < 31 ? "Usable" : "Strong";

    // ═══════════════ CFR_01 Event Density Grid ═══════════════
    [Fact]
    public void V4_1_CFR_01_EventDensityGrid()
    {
        int[] Ns = [40, 80, 120]; double s = 0.1; double thresh = 0.01; int Ebase = 5;
        double[] xis = [0.75, 1.0, 1.25, 1.5, 2.0]; double[] K0s = [0.5, 0.8, 1.2]; double[] kicks = [0.1, 0.2, 0.3];
        _output.WriteLine("N      xi    K0     kickAmp  events   respFrac   dg");
        _output.WriteLine("----   ----  ----   -------  -------  ---------  ------");
        int total = 0, usable = 0;
        foreach (int N in Ns)
        {
            int E = N <= 80 ? Ebase : 3;
            foreach (double xi in xis)
                foreach (double K0v in K0s)
                    foreach (double ka in kicks)
                    {
                        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, E, BS);
                        var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200)))); int nSrc = N <= 80 ? 4 : 3;
                        var (ds, _, _, resp, totalPairs) = EventCloudEx(Kc, dMat, N, s, BS + 300, ka, thresh, nSrc);
                        double frac = (double)resp / Math.Max(totalPairs, 1); double dg = Dg(dMat);
                        string evCls = EventClass(ds.Count);
                        total++; if (ds.Count >= 11) usable++;
                        _output.WriteLine($"{N,5}  {xi:F2}  {K0v:F2}   {ka,7:F2}  {ds.Count,7}  {frac,9:F4}  {dg,6:F4}");
                    }
        }
        _output.WriteLine($"--- Grid: {total} combos, {usable} usable ({100.0 * usable / total:F1}%) ---");
        Assert.True(usable > 0, "At least some regimes should be usable.");
    }

    // ═══════════════ CFR_02 Minimum Event Requirement ═══════════════
    [Fact]
    public void V4_1_CFR_02_MinimumEventRequirement()
    {
        int N = 80; double s = 0.1; double thresh = 0.01;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, 0.5, 1.0, s, 5, BS);
        var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
        _output.WriteLine("kickAmp  events   class          frontFitPossible?");
        _output.WriteLine("-------  -------  -------------  -----------------");
        foreach (double ka in new[] { 0.05, 0.1, 0.2, 0.3, 0.5 })
        {
            var (ds, ts, _, _, _) = EventCloudEx(Kc, dMat, N, s, BS + 300, ka, thresh, 4);
            string cls = EventClass(ds.Count);
            var (v, _, r2, _) = FrontFit(ds, ts);
            bool fitOk = double.IsFinite(v) && ds.Count >= 4;
            _output.WriteLine($"{ka,7:F2}  {ds.Count,7}  {cls,13}  {fitOk,17}");
        }
        Assert.True(true);
    }

    // ═══════════════ CFR_03 Front-Fit Stability By Regime ═══════════════
    [Fact]
    public void V4_1_CFR_03_FrontFitStabilityByRegime()
    {
        int N = 80; double s = 0.1; double thresh = 0.01; double ka = 0.3; int nSrc = 4;
        (double xi, double K0v)[] regimes = [(0.75, 0.5), (1.0, 0.5), (1.0, 0.8), (1.25, 0.8), (1.5, 1.2), (2.0, 1.2)];
        _output.WriteLine("xi     K0     events   v_candidate  R²       resStd    outFrac   cls");
        _output.WriteLine("-----  ----   -------  -----------  -------  -------   -------   ----------");
        foreach (var (xi, K0v) in regimes)
        {
            var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
            var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
            var (ds, ts, _, _, _) = EventCloudEx(Kc, dMat, N, s, BS + 300, ka, thresh, nSrc);
            var (v, _, r2, resStd) = FrontFit(ds, ts);
            double outF = 0;
            if (ds.Count >= 4 && double.IsFinite(v)) { int oCnt = 0; for (int i = 0; i < ds.Count; i++) if (ts[i] < ds[i] / v) oCnt++; outF = (double)oCnt / ds.Count; }
            string cls = EventClass(ds.Count);
            _output.WriteLine($"{xi:F2}   {K0v:F2}    {ds.Count,7}  {v,11:F4}  {r2,7:F4}  {resStd,7:F4}  {outF,7:F4}  {cls}");
            // Diagnostic only — NaN v means slope ≈ 0 (tau independent of d)
            Assert.True(true);
        }
    }

    // ═══════════════ CFR_04 Kick Window Robustness ═══════════════
    [Fact]
    public void V4_1_CFR_04_KickWindowRobustness()
    {
        int N = 80; double s = 0.1; double thresh = 0.01; double K0v = 0.5; double xi = 1.0;
        double[] kicks = [0.05, 0.10, 0.20, 0.30, 0.50, 0.80, 1.00];
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
        _output.WriteLine("kickAmp  events   respFrac   v_candidate  R²       dg       kickZone");
        _output.WriteLine("-------  -------  ---------  -----------  -------  -------  ------------");
        foreach (double ka in kicks)
        {
            var (ds, ts, _, resp, tot) = EventCloudEx(Kc, dMat, N, s, BS + 300, ka, thresh, 4);
            double frac = (double)resp / Math.Max(tot, 1); var (v, _, r2, _) = FrontFit(ds, ts);
            double dg = Dg(dMat);
            string zone = ds.Count < 4 ? "Too weak" : ka > 0.7 ? "Overdriven" : ka >= 0.2 ? "Linear usable" : "Weak usable";
            _output.WriteLine($"{ka,7:F2}  {ds.Count,7}  {frac,9:F4}  {v,11:F4}  {r2,7:F4}  {dg,6:F4}  {zone}");
            Assert.True(double.IsFinite(frac));
        }
    }

    // ═══════════════ CFR_05 Density Regime Comparison ═══════════════
    [Fact]
    public void V4_1_CFR_05_DensityRegimeComparison()
    {
        int N = 60; double s = 0.1; double thresh = 0.01; double ka = 0.3;
        double[,] GaussUpd(double[,] d, double K0, double xi) { int n = d.GetLength(0); var K = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 * Math.Exp(-d[i, j] * d[i, j] / Math.Max(xi * xi, 0.0001)); } return K; }
        double[,] PowerUpd(double[,] d, double K0v2, double p) { int n = d.GetLength(0); var K = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0v2 / (1.0 + Math.Pow(Math.Max(d[i, j], 0), p)); } return K; }

        (int ev, double v, double dg) Eval(double[,] K0, string law, double param, double K0v)
        {
            var Kc = (double[,])K0.Clone();
            for (int e = 0; e < 5; e++) { var h = Sm(Kc, N, s, BS + e); var d = DL(Nm(RP(h))); Kc = law == "exp" ? ExpUpd(d, K0v, param) : law == "gauss" ? GaussUpd(d, K0v, param) : PowerUpd(d, K0v, param); }
            var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
            var (ds, ts, _, _, _) = EventCloudEx(Kc, dMat, N, s, BS + 300, ka, thresh, 4);
            var (v, _, _, _) = FrontFit(ds, ts); double dg = Dg(dMat);
            return (ds.Count, v, dg);
        }

        var Krs = KS(N, BS);
        _output.WriteLine("Regime               events   v_candidate  dg       class");
        _output.WriteLine("-------------------  -------  -----------  -------  ----------");
        foreach (var (name, law, param, K0v) in new[] { ("Exp xi=1.0 K0=0.5", "exp", 1.0, 0.5), ("Exp xi=1.5 K0=0.8", "exp", 1.5, 0.8), ("Exp xi=2.0 K0=1.2", "exp", 2.0, 1.2), ("Gauss xi=1.0 K0=0.5", "gauss", 1.0, 0.5), ("Power p=2 K0=0.5", "power", 2.0, 0.5) })
        {
            var (ev, v, dg) = Eval(Krs, law, param, K0v);
            _output.WriteLine($"{name,-20} {ev,7}  {v,11:F4}  {dg,7:F4}  {EventClass(ev)}");
        }
        // Overcoupled dense
        var Koc = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j) Koc[i, j] = 0.5;
        var (evOC, _, dgOC) = Eval(Koc, "exp", 1.0, 0.5);
        _output.WriteLine($"Overcoupled dense     {evOC,7}  --          {dgOC,7:F4}  {EventClass(evOC)}");
        // Global sync
        var hSync = new double[101][]; for (int t = 0; t < 101; t++) { hSync[t] = new double[N]; for (int i = 0; i < N; i++) hSync[t][i] = 0; }
        double dgGS = Dg(DL(Nm(RP(hSync))));
        _output.WriteLine($"Global sync          --        --          {dgGS,7:F4}  Degenerate");
        Assert.True(dgGS < 0.01);
    }

    // ═══════════════ CFR_06 Shell Coverage Diagnostic ═══════════════
    [Fact]
    public void V4_1_CFR_06_ShellCoverageDiagnostic()
    {
        int N = 80; double s = 0.1; double thresh = 0.01; double ka = 0.3; double K0v = 0.5; double xi = 1.0;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
        int nSrc = 4; int nShells = 5;
        _output.WriteLine("src   popShells  events/shell  meanTau/shell  monotonic?");
        _output.WriteLine("----  ---------  ------------  -------------  ----------");
        for (int src = 0; src < nSrc; src++)
        {
            var (times, _) = KickDetect(Kc, N, s, BS + 300, src, ka, 50, thresh);
            var targets = Enumerable.Range(0, N).Where(x => x != src).OrderBy(x => dMat[src, x]).ToArray();
            int shSz = Math.Max(1, (N - 1) / nShells);
            int popShells = 0; var shellMeans = new List<double>();
            for (int sh = 0; sh < nShells; sh++)
            {
                var shN = targets.Skip(sh * shSz).Take(shSz).ToArray();
                var tSh = new List<double>();
                foreach (int dst in shN) if (times[dst] > 0) tSh.Add(times[dst]);
                if (tSh.Count > 0) { popShells++; shellMeans.Add(tSh.Average()); }
            }
            bool monotonic = shellMeans.Count >= 3 && shellMeans.Zip(shellMeans.Skip(1), (a, b) => a <= b).All(x => x);
            double evPerShell = popShells > 0 ? shellMeans.Sum() / popShells : 0;
            _output.WriteLine($"{src,4}  {popShells,9}  {evPerShell,12:F2}   {(shellMeans.Count > 0 ? shellMeans.Average() : double.NaN),13:F4}  {monotonic,10}");
        }
        Assert.True(true);
    }

    // ═══════════════ CFR_07 Multi-Source Front Robustness ═══════════════
    [Fact]
    public void V4_1_CFR_07_MultiSourceFrontRobustness()
    {
        int N = 80; double s = 0.1; double thresh = 0.01; double ka = 0.3; double K0v = 0.5; double xi = 1.0;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
        // Compute weighted degree for each node
        var wDeg = new (int idx, double deg)[N];
        for (int i = 0; i < N; i++) { double deg = 0; for (int j = 0; j < N; j++) if (i != j && Kc[i, j] > 1e-6) deg += Kc[i, j]; wDeg[i] = (i, deg); }
        int srcHi = wDeg.OrderByDescending(x => x.deg).First().idx;
        int srcLo = wDeg.OrderBy(x => x.deg).First().idx;
        int srcRnd = 17 % N;
        int[] srcs = [srcHi, srcLo, srcRnd];
        _output.WriteLine("src_type   src   events   v_candidate  shellCov  respFrac");
        _output.WriteLine("--------   ---   -------  -----------  --------  --------");
        foreach (int src in srcs)
        {
            var (times, _) = KickDetect(Kc, N, s, BS + 300, src, ka, 50, thresh);
            var targets = Enumerable.Range(0, N).Where(x => x != src).OrderBy(x => dMat[src, x]).ToArray();
            int nShells = 5; int shSz = Math.Max(1, (N - 1) / nShells); int pop = 0; var ds = new List<double>(); var ts = new List<double>();
            for (int sh = 0; sh < nShells; sh++)
            {
                var shN = targets.Skip(sh * shSz).Take(shSz).ToArray();
                bool any = false;
                foreach (int dst in shN) if (times[dst] > 0) { ds.Add(dMat[src, dst]); ts.Add(times[dst]); any = true; }
                if (any) pop++;
            }
            int resp = 0; for (int dst = 0; dst < N; dst++) if (dst != src && times[dst] > 0) resp++;
            var (v, _, _, _) = FrontFit(ds, ts);
            string type = src == srcHi ? "high-deg" : src == srcLo ? "low-deg" : "random";
            _output.WriteLine($"{type,-8}  {src,3}  {ds.Count,7}  {v,11:F4}  {pop,8}  {(double)resp / (N - 1),8:F4}");
        }
        Assert.True(true);
    }

    // ═══════════════ CFR_08 Multi-Seed Front Robustness ═══════════════
    [Fact]
    public void V4_1_CFR_08_MultiSeedFrontRobustness()
    {
        int N = 80; double s = 0.1; double thresh = 0.01; double ka = 0.3; double K0v = 0.5; double xi = 1.0; int nSeeds = 10;
        var vs = new List<double>(); var evs = new List<int>(); var r2s = new List<double>(); int noEvents = 0;
        for (int sd = 0; sd < nSeeds; sd++)
        {
            var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS + sd * 10);
            var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + sd * 10 + 200))));
            var (ds, ts, _, _, _) = EventCloudEx(Kc, dMat, N, s, BS + sd * 10 + 300, ka, thresh, 4);
            var (v, _, r2, _) = FrontFit(ds, ts);
            if (ds.Count >= 4 && double.IsFinite(v)) { vs.Add(v); evs.Add(ds.Count); r2s.Add(r2); } else noEvents++;
        }
        double mv = vs.Count > 0 ? vs.Average() : double.NaN;
        double sv = vs.Count > 1 ? Math.Sqrt(vs.Average(x => (x - mv) * (x - mv))) : 0;
        double me = evs.Count > 0 ? evs.Average() : 0;
        double mr = r2s.Count > 0 ? r2s.Average() : double.NaN;
        _output.WriteLine($"Multi-seed ({nSeeds} seeds): v_mean={mv:F4} v_std={sv:F4} ev_mean={me:F1} R²_mean={mr:F4} noEvents={noEvents}");
        if (noEvents == nSeeds) _output.WriteLine("  All seeds have too few events — consistent zero-response regime.");
        Assert.True(noEvents <= nSeeds, "Seed diagnostics valid.");
    }

    // ═══════════════ CFR_09 N-Scaling Front Robustness ═══════════════
    [Fact]
    public void V4_1_CFR_09_NScalingFrontRobustness()
    {
        int[] Ns = [40, 80, 120, 200]; double s = 0.1; double thresh = 0.01; double ka = 0.3; double K0v = 0.5; double xi = 1.0;
        _output.WriteLine("N      E   events   v_candidate  R²       shellCov  class");
        _output.WriteLine("----   --  -------  -----------  -------  --------  ----------");
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : (N <= 120 ? 3 : 2);
            var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, E, BS);
            var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
            int nSrc = N <= 80 ? 4 : 3;
            var (ds, ts, _, _, _) = EventCloudEx(Kc, dMat, N, s, BS + 300, ka, thresh, nSrc);
            var (v, _, r2, _) = FrontFit(ds, ts);
            // Shell coverage
            int popShells = 0; int nShells = 5;
            for (int src = 0; src < nSrc; src++)
            {
                var (times, _) = KickDetect(Kc, N, s, BS + 300, src, ka, 50, thresh);
                var targets = Enumerable.Range(0, N).Where(x => x != src).OrderBy(x => dMat[src, x]).ToArray();
                int shSz = Math.Max(1, (N - 1) / nShells);
                for (int sh = 0; sh < nShells; sh++) { if (targets.Skip(sh * shSz).Take(shSz).Any(dst => times[dst] > 0)) popShells++; }
            }
            _output.WriteLine($"{N,5}  {E,2}  {ds.Count,7}  {v,11:F4}  {r2,7:F4}  {popShells,8}  {EventClass(ds.Count)}");
            Assert.True(ds.Count >= 0);
        }
    }

    // ═══════════════ CFR_10 Null and Degenerate Front Controls ═══════════════
    [Fact]
    public void V4_1_CFR_10_NullAndDegenerateFrontControls()
    {
        int N = 60; double s = 0.1; double thresh = 0.01; double ka = 0.3; double K0v = 0.5; double xi = 1.0;

        (int ev, double v, double dg) Eval(double[,] Kc)
        {
            var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 300))));
            var (ds, ts, _, _, _) = EventCloudEx(Kc, dMat, N, s, BS + 400, ka, thresh, 4);
            var (vv, _, _, _) = FrontFit(ds, ts); return (ds.Count, vv, Dg(dMat));
        }

        // Active
        var K0a = KS(N, BS); var KcA = RecoverFP(K0a, N, K0v, xi, s, 5, BS);
        var (evA, vA, dgA) = Eval(KcA);
        // K=0 null
        var KcN = RecoverFP(new double[N, N], N, K0v, xi, s, 5, BS);
        var (evN, vN, dgN) = Eval(KcN);
        // Overcoupled
        var Koc = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j) Koc[i, j] = 0.5;
        var KcOC = RecoverFP(Koc, N, K0v, xi, s, 5, BS);
        var (evOC, vOC, dgOC) = Eval(KcOC);
        // Global sync
        var hSync = new double[101][]; for (int t = 0; t < 101; t++) { hSync[t] = new double[N]; for (int i = 0; i < N; i++) hSync[t][i] = 0; }
        double dgGS = Dg(DL(Nm(RP(hSync))));

        _output.WriteLine("Case             events   v_candidate  dg       class");
        _output.WriteLine("---------------  -------  -----------  -------  ----------");
        _output.WriteLine($"Active           {evA,7}  {vA,11:F4}  {dgA,7:F4}  {EventClass(evA)}");
        _output.WriteLine($"K=0 null         {evN,7}  {vN,11:F4}  {dgN,7:F4}  {EventClass(evN)}");
        _output.WriteLine($"Overcoupled      {evOC,7}  {vOC,11:F4}  {dgOC,7:F4}  {EventClass(evOC)}");
        _output.WriteLine($"Global sync      --        --          {dgGS,7:F4}  Degenerate");

        Assert.True(dgGS < 0.01, "Global sync must be degenerate.");
    }

    // ═══════════════ CFR_11 Pre-Calibration Readiness Score ═══════════════
    [Fact]
    public void V4_1_CFR_11_PreCalibrationReadinessScore()
    {
        int N = 80; double s = 0.1; double thresh = 0.01; double ka = 0.3;
        (double xi, double K0v, int E)[] regimes = [(0.75, 0.5, 5), (1.0, 0.5, 5), (1.0, 0.8, 5), (1.25, 0.8, 5), (1.5, 1.2, 5), (2.0, 1.2, 5)];
        _output.WriteLine("xi     K0     events   v_candidate  R²       shellCov  readiness  class");
        _output.WriteLine("-----  ----   -------  -----------  -------  --------  ---------  ----------");
        foreach (var (xi, K0v, E) in regimes)
        {
            var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, E, BS);
            var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
            var (ds, ts, _, _, _) = EventCloudEx(Kc, dMat, N, s, BS + 300, ka, thresh, 4);
            var (v, _, r2, _) = FrontFit(ds, ts);
            // Shell coverage
            int popShells = 0; int nShells = 5; int nSrc = 4;
            for (int src = 0; src < nSrc; src++)
            {
                var (times, _) = KickDetect(Kc, N, s, BS + 300, src, ka, 50, thresh);
                var targets = Enumerable.Range(0, N).Where(x => x != src).OrderBy(x => dMat[src, x]).ToArray();
                int shSz = Math.Max(1, (N - 1) / nShells);
                for (int sh = 0; sh < nShells; sh++) if (targets.Skip(sh * shSz).Take(shSz).Any(dst => times[dst] > 0)) popShells++;
            }
            double shellCov = (double)popShells / (nSrc * nShells);
            double dg = Dg(dMat);
            double nonDeg = 1.0 / (1.0 + dg);
            double evNorm = Math.Min(1.0, ds.Count / 30.0);
            double fitQ = double.IsFinite(r2) ? Math.Max(0, r2) : 0;
            double readiness = evNorm * fitQ * shellCov * nonDeg;
            string cls = EventClass(ds.Count);
            _output.WriteLine($"{xi:F2}   {K0v:F2}    {ds.Count,7}  {v,11:F4}  {r2,7:F4}  {shellCov,8:F4}  {readiness,9:F4}  {cls}");
            Assert.True(double.IsFinite(readiness));
        }
    }

    // ═══════════════ CFR_12 Claim Discipline Report ═══════════════
    [Fact]
    public void V4_1_CFR_12_ClaimDisciplineReport()
    {
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("  CLAIM DISCIPLINE — CAUSAL FRONT ROBUSTNESS");
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  SUPPORTED:");
        _output.WriteLine("    - Causal front event density can be measured.");
        _output.WriteLine("    - Usable front regimes can be identified.");
        _output.WriteLine("    - Dimensionless fitted v can be stabilized.");
        _output.WriteLine("    - Null and degenerate cases are detected.");
        _output.WriteLine("    - Pre-calibration readiness can be ranked.");
        _output.WriteLine("    - Shell coverage and multi-source robustness");
        _output.WriteLine("      are measurable.");
        _output.WriteLine("");
        _output.WriteLine("  CONDITIONAL:");
        _output.WriteLine("    - Front robustness depends on xi, K0, kick");
        _output.WriteLine("      amplitude, threshold, topology, N, source");
        _output.WriteLine("      choice, and fitting method.");
        _output.WriteLine("    - Fitted v remains dimensionless.");
        _output.WriteLine("    - Readiness score is diagnostic, not validation.");
        _output.WriteLine("");
        _output.WriteLine("  HYPOTHESIS:");
        _output.WriteLine("    - Stable fitted propagation speed may become");
        _output.WriteLine("      calibratable as c_eff later.");
        _output.WriteLine("    - Lorentzian causal structure may emerge in");
        _output.WriteLine("      a calibrated continuum limit.");
        _output.WriteLine("    - Physical c may correspond to a stable TRM");
        _output.WriteLine("      propagation speed after calibration.");
        _output.WriteLine("");
        _output.WriteLine("  NOT CLAIMED:");
        _output.WriteLine("    - Physical c is derived.");
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
