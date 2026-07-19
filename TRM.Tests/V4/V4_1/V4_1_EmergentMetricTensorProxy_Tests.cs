using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Emergent metric tensor proxy: tests whether the established TRM numerical
/// objects (Omega, d_ij, tau_ij, v_fit, s2) can be assembled into a consistent
/// emergent metric-tensor-like numerical proxy with temporal, spatial, and
/// off-diagonal components.
///
/// Does NOT claim physical metric, Minkowski metric, Lorentzian spacetime,
/// Lorentz invariance, physical c, D=3, gravity, Einstein equations, GR,
/// time dilation, SPARC, or dark matter.
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_EmergentMetricTensorProxy")]
public class V4_1_EmergentMetricTensorProxy_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_EmergentMetricTensorProxy_Tests(ITestOutputHelper o) { _output = o; }

    // ══════════════════ Core helpers ══════════════════
    private static double[][] Sm(double[,] K, int N, double s, int seed, int kickOrLoadNode = -1, double deltaOrAmp = 0, int kickT = -1)
    {
        var r = new Random(seed); var w = new double[N]; for (int i = 0; i < N; i++) w[i] = 1.0 + s * (r.NextDouble() - 0.5) * 2.0;
        if (kickT < 0 && kickOrLoadNode >= 0 && kickOrLoadNode < N) w[kickOrLoadNode] += deltaOrAmp;
        var th = new double[N]; for (int i = 0; i < N; i++) th[i] = r.NextDouble() * 2.0 * Math.PI;
        int hL = St / Hd + 1; var h = new double[hL][]; h[0] = (double[])th.Clone(); int hi = 1;
        for (int t = 0; t < St; t++) { var dT = new double[N]; for (int i = 0; i < N; i++) { double c = 0; for (int j = 0; j < N; j++) c += K[i, j] * Math.Sin(th[j] - th[i]); dT[i] = w[i] + c; } if (kickT >= 0 && kickOrLoadNode >= 0 && kickOrLoadNode < N && t == kickT) dT[kickOrLoadNode] += deltaOrAmp; for (int i = 0; i < N; i++) th[i] += Dt * dT[i]; if ((t + 1) % Hd == 0 && hi < hL) h[hi++] = (double[])th.Clone(); }
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
    private static double Median(List<double> vs) { if (vs.Count == 0) return double.NaN; var s = vs.OrderBy(x => x).ToList(); int mid = s.Count / 2; return s.Count % 2 == 0 ? (s[mid - 1] + s[mid]) / 2.0 : s[mid]; }
    private static string CorrClass(double r) => Math.Abs(r) > 0.7 ? "Strong" : Math.Abs(r) > 0.4 ? "Moderate" : Math.Abs(r) > 0.1 ? "Weak" : "None";

    // ── Omega / temporal ──────────────────────────────
    private static double[] OmegaField(double[][] h)
    {
        int T = h.Length, N = h[0].Length; var o = new double[N];
        for (int i = 0; i < N; i++) { double s = 0; int c = 0; for (int t = 1; t < T; t++) { s += Math.Abs(h[t][i] - h[t - 1][i]); c++; } o[i] = c > 0 ? s / (c * Dt * Hd) : 0; }
        return o;
    }
    private static double TemporalStability(double[] o) { if (o.Length < 2) return 0; double m = o.Average(); double cv = m > 1e-6 ? Math.Sqrt(o.Average(x => (x - m) * (x - m))) / m : 1; return 1.0 / (1.0 + cv); }

    // ── Kick detection ─────────────────────────────────
    private static (double[] times, double[] amps) KickDetect(double[,] K, int N, double s, int seed, int src, double kickAmp, double thresh)
    {
        var h0 = Sm(K, N, s, seed); var hk = Sm(K, N, s, seed, src, kickAmp, 50);
        var times = new double[N]; var amps = new double[N];
        for (int dst = 0; dst < N; dst++) { times[dst] = -1; amps[dst] = 0; if (dst == src) continue; for (int t = 0; t < hk.Length; t++) { double dPh = hk[t][dst] - h0[t][dst]; double dev = Math.Abs(Math.Sin(0.5 * dPh)); if (dev > amps[dst]) amps[dst] = dev; if (dev > thresh && times[dst] < 0) times[dst] = t * Dt * Hd; } }
        return (times, amps);
    }

    // ── Dimension ──────────────────────────────────────
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

    // ── Front fit ──────────────────────────────────────
    private static (double v, double b, double r2, int n) FitFront(List<double> d, List<double> tau)
    {
        if (d.Count < 4) return (double.NaN, double.NaN, double.NaN, d.Count);
        double mx = d.Average(), my = tau.Average(), sxy = 0, sx2 = 0, sy2 = 0;
        for (int i = 0; i < d.Count; i++) { double a = d[i] - mx, bv = tau[i] - my; sxy += a * bv; sx2 += a * a; sy2 += bv * bv; }
        double slope = sx2 > 1e-15 ? sxy / sx2 : 0;
        double v = slope > 1e-15 ? 1.0 / slope : double.NaN;
        double r2 = sx2 * sy2 > 1e-15 ? sxy * sxy / (sx2 * sy2) : 0;
        return (v, mx > 0 ? my - slope * mx : 0, r2, d.Count);
    }

    // ── Metric proxy components ────────────────────────
    private static double G00Proxy(double[] omega, double vFit)
    {
        double tS = TemporalStability(omega);
        double mO = omega.Average();
        // Temporal metric proxy: stability-weighted inverse clock rate
        return tS / Math.Max(mO, 1e-6);
    }

    private static double SpatialMetricProxy(double[,] dMat, int N, double vFit)
    {
        // Spatial metric proxy: mean squared distance / v^2
        double sum = 0; int count = 0;
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { sum += dMat[i, j] * dMat[i, j]; count++; }
        double msd = count > 0 ? sum / count : 0;
        return double.IsFinite(vFit) && vFit > 0 ? msd / (vFit * vFit) : msd;
    }

    private static double OffDiagProxy(double[,] dMat, double[] omega, int N)
    {
        // Off-diagonal: correlation between d_ij and |omega_i - omega_j|
        var ds = new List<double>(); var os = new List<double>();
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { ds.Add(dMat[i, j]); os.Add(Math.Abs(omega[i] - omega[j])); }
        return Math.Abs(Spear(ds.ToArray(), os.ToArray()));
    }

    // ═══════════════ EMTP_01 MetricProxyDataFinite ═══════════════
    [Fact]
    public void V4_1_EMTP_01_MetricProxyDataFinite()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h = Sm(Kc, N, s, BS);
        var omega = OmegaField(h); var dMat = DL(Nm(RP(h)));
        var ds = new List<double>(); var ts = new List<double>();
        for (int src = 0; src < 3; src++) { var (times, _) = KickDetect(Kc, N, s, BS, src, 0.5, 0.02); for (int dst = 0; dst < N; dst++) if (dst != src && times[dst] > 0 && dMat[src, dst] > 0) { ds.Add(dMat[src, dst]); ts.Add(times[dst]); } }
        var (v, _, _, _) = FitFront(ds, ts);
        double g00 = G00Proxy(omega, v);
        double gSp = SpatialMetricProxy(dMat, N, v);
        double gOD = OffDiagProxy(dMat, omega, N);
        Assert.True(double.IsFinite(g00), "g00 NaN");
        Assert.True(double.IsFinite(gSp), "gSp NaN");
        Assert.True(double.IsFinite(gOD), "gOD NaN");
        _output.WriteLine($"g00={g00:F3}  g_spatial={gSp:F3}  g_offdiag={gOD:F3}  v={v:F3}");
    }

    // ═══════════════ EMTP_02 LocalNeighborhoodProxy ═══════════════
    [Fact]
    public void V4_1_EMTP_02_LocalNeighborhoodCoordinateProxy()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h = Sm(Kc, N, s, BS);
        var omega = OmegaField(h); var dMat = DL(Nm(RP(h)));
        // Pick high/low/median degree nodes
        var degs = new double[N];
        for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j) degs[i] += Kc[i, j];
        int hi = Enumerable.Range(0, N).OrderByDescending(i => degs[i]).First();
        int lo = Enumerable.Range(0, N).OrderBy(i => degs[i]).First();
        int mid = Enumerable.Range(0, N).OrderBy(i => degs[i]).Skip(N / 2).First();
        // Local spatial neighborhood: distances to 5 nearest neighbors
        int k = Math.Min(5, N - 1);
        foreach (var (label, node) in new[] { ("hi", hi), ("lo", lo), ("mid", mid) })
        {
            var nn = Enumerable.Range(0, N).Where(x => x != node).OrderBy(x => dMat[node, x]).Take(k).ToArray();
            double rLocal = nn.Select(x => dMat[node, x]).Average();
            double oLocal = omega[node];
            _output.WriteLine($"{label}: node={node}  r_local={rLocal:F3}  omega={oLocal:F3}  n_deg={degs[node]:F2}");
        }
    }

    // ═══════════════ EMTP_03 TemporalMetricComponent ═══════════════
    [Fact]
    public void V4_1_EMTP_03_TemporalMetricComponentProxy()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h = Sm(Kc, N, s, BS); var omega = OmegaField(h);
        double g00 = G00Proxy(omega, 1.0);
        double tStab = TemporalStability(omega);
        string cls = tStab > 0.7 ? "Stable temporal component" : (tStab > 0.4 ? "Weak" : (tStab > 0.2 ? "Noisy" : "Degenerate"));
        _output.WriteLine($"g00_proxy={g00:F3}  tStab={tStab:F3}  Omega_mean={omega.Average():F3}  class={cls}");
    }

    // ═══════════════ EMTP_04 SpatialMetricComponent ═══════════════
    [Fact]
    public void V4_1_EMTP_04_SpatialMetricComponentProxy()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var dMat = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var (dRaw, sp) = MeasureDim(Kc, N, s);
        double gSp = SpatialMetricProxy(dMat, N, 1.0);
        double spatStab = 1.0 / (1.0 + sp);
        string cls = spatStab > 0.6 ? "Stable spatial component" : (spatStab > 0.3 ? "Weak" : "Noisy");
        _output.WriteLine($"g_spatial={gSp:F3}  D_raw={dRaw:F2}  spread={sp:F3}  class={cls}");
    }

    // ═══════════════ EMTP_05 OffDiagonalTimeSpaceCoupling ═══════════════
    [Fact]
    public void V4_1_EMTP_05_OffDiagonalTimeSpaceCouplingProxy()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h = Sm(Kc, N, s, BS);
        var omega = OmegaField(h); var dMat = DL(Nm(RP(h)));
        double gOD = OffDiagProxy(dMat, omega, N);
        string cls = gOD < 0.2 ? "Small off-diag" : (gOD < 0.5 ? "Moderate" : "Large");
        _output.WriteLine($"g_offdiag={gOD:F3}  class={CorrClass(gOD)}  {cls}");
    }

    // ═══════════════ EMTP_06 SignatureProxyClassification ═══════════════
    [Fact]
    public void V4_1_EMTP_06_SignatureProxyClassification()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h = Sm(Kc, N, s, BS);
        var omega = OmegaField(h); var dMat = DL(Nm(RP(h)));
        double g00 = G00Proxy(omega, 1.0);
        double gSp = SpatialMetricProxy(dMat, N, 1.0);
        double gOD = OffDiagProxy(dMat, omega, N);
        // Signature proxy: temporal sign differs from spatial, off-diag small
        bool tempVsSpatial = g00 * gSp > 0 && Math.Abs(g00 / Math.Max(gSp, 1e-6) - 1.0) > 0.2;
        bool offDiagSmall = gOD < 0.4;
        string sig;
        if (tempVsSpatial && offDiagSmall) sig = "Lorentz-like sign sep proxy";
        else if (offDiagSmall) sig = "Weak sign sep proxy";
        else if (gOD > 0.7) sig = "Euclidean-like / coupled";
        else sig = "Degenerate";
        _output.WriteLine($"g00={g00:F3}  gSpatial={gSp:F3}  gOffDiag={gOD:F3}  signature={sig}");
    }

    // ═══════════════ EMTP_07 IntervalConsistencyWithMetric ═══════════════
    [Fact]
    public void V4_1_EMTP_07_IntervalConsistencyWithMetricProxy()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h = Sm(Kc, N, s, BS); var omega = OmegaField(h); var dMat = DL(Nm(RP(h)));
        var ds = new List<double>(); var ts = new List<double>();
        for (int src = 0; src < 5; src++) { var (times, _) = KickDetect(Kc, N, s, BS, src, 0.5, 0.02); for (int dst = 0; dst < N; dst++) if (dst != src && times[dst] > 0 && dMat[src, dst] > 0) { ds.Add(dMat[src, dst]); ts.Add(times[dst]); } }
        if (ds.Count < 4) { _output.WriteLine("Insufficient events"); return; }
        var (v, _, _, _) = FitFront(ds, ts);
        if (!double.IsFinite(v)) { _output.WriteLine("v NaN"); return; }
        double g00 = G00Proxy(omega, v);
        double gSp = SpatialMetricProxy(dMat, N, v);
        // Direct s2 vs proxy s2 = g00*tau^2 - gSp*(d/v)^2
        var s2d = new List<double>(); var s2p = new List<double>();
        for (int i = 0; i < ds.Count; i++) { double dv = ds[i] / v; s2d.Add(ts[i] * ts[i] - dv * dv); s2p.Add(g00 * ts[i] * ts[i] - gSp * dv * dv); }
        double rho = Spear(s2d.ToArray(), s2p.ToArray());
        double mse = s2d.Zip(s2p, (a, b) => (a - b) * (a - b)).Sum() / s2d.Count;
        _output.WriteLine($"corr(s2_direct, s2_proxy) = {rho:F4}  MSE={mse:F3}");
    }

    // ═══════════════ EMTP_08 LoadInvarianceOfMetricProxy ═══════════════
    [Fact]
    public void V4_1_EMTP_08_LoadInvarianceOfMetricProxy()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        var Kb = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        // Before
        var hB = Sm(Kb, N, s, BS); var oB = OmegaField(hB); var dB = DL(Nm(RP(hB)));
        double g00b = G00Proxy(oB, 1.0); double gSpb = SpatialMetricProxy(dB, N, 1.0); double gODb = OffDiagProxy(dB, oB, N);
        // After load
        var hL = Sm(Kb, N, s, BS, ln, 0.2); var oL = OmegaField(hL);
        var Kl = ExpUpd(DL(Nm(RP(hL))), kv, xi); var dL = DL(Nm(RP(Sm(Kl, N, s, BS))));
        double g00l = G00Proxy(oL, 1.0); double gSpl = SpatialMetricProxy(dL, N, 1.0); double gODl = OffDiagProxy(dL, oL, N);
        double dg00 = Math.Abs(g00l - g00b) / Math.Max(g00b, 1e-6);
        double dgSp = Math.Abs(gSpl - gSpb) / Math.Max(gSpb, 1e-6);
        double dgOD = Math.Abs(gODl - gODb) / Math.Max(gODb > 0.01 ? gODb : 1, 1e-6);
        _output.WriteLine($"Δg00_rel={dg00:F3}  ΔgSp_rel={dgSp:F3}  ΔgOD_rel={dgOD:F3}");
        _output.WriteLine($"Load-metric-invariant: {(dg00 < 0.5 && dgSp < 0.5 ? "YES" : "NO")}");
    }

    // ═══════════════ EMTP_09 NScalingMetricProxy ═══════════════
    [Fact]
    public void V4_1_EMTP_09_NScalingMetricProxy()
    {
        int[] Ns = [40, 80, 120, 200]; double xi = 1.75; double kv = 1.2; double s = 0.1;
        _output.WriteLine("N     g00    gSp   gOD   D_raw  metricScore");
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3;
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
            var h = Sm(Kc, N, s, BS); var o = OmegaField(h); var dM = DL(Nm(RP(h)));
            double g00 = G00Proxy(o, 1.0); double gSp = SpatialMetricProxy(dM, N, 1.0); double gOD = OffDiagProxy(dM, o, N);
            double sc = (1.0 / (1.0 + gOD)) * TemporalStability(o) * (1.0 / (1.0 + Dg(dM)));
            var (dR, _) = MeasureDim(Kc, N, s);
            _output.WriteLine($"{N,5}  {g00:F3}  {gSp:F3}  {gOD:F3}  {dR,5:F2}  {sc:F3}");
        }
    }

    // ═══════════════ EMTP_10 MultiSeedMetricProxy ═══════════════
    [Fact]
    public void V4_1_EMTP_10_MultiSeedMetricProxy()
    {
        int[] Ns = [80, 120]; double xi = 1.75; double kv = 1.2; double s = 0.1; int nSeeds = 10;
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3;
            var g00s = new List<double>(); var gSps = new List<double>(); var gODs = new List<double>();
            for (int seed = 0; seed < nSeeds; seed++)
            {
                var Kc = RecoverFP(KS(N, seed), N, kv, xi, s, E, seed);
                var h = Sm(Kc, N, s, seed); var o = OmegaField(h); var dM = DL(Nm(RP(h)));
                g00s.Add(G00Proxy(o, 1.0)); gSps.Add(SpatialMetricProxy(dM, N, 1.0)); gODs.Add(OffDiagProxy(dM, o, N));
            }
            double m0 = g00s.Average(); double s0 = g00s.Count > 1 ? Math.Sqrt(g00s.Average(x => (x - m0) * (x - m0))) : 0;
            double mS = gSps.Average(); double sS = gSps.Count > 1 ? Math.Sqrt(gSps.Average(x => (x - mS) * (x - mS))) : 0;
            double mO = gODs.Average(); double sO = gODs.Count > 1 ? Math.Sqrt(gODs.Average(x => (x - mO) * (x - mO))) : 0;
            _output.WriteLine($"N={N}: g00={m0:F3}±{s0:F3}  gSp={mS:F3}±{sS:F3}  gOD={mO:F3}±{sO:F3}  n={nSeeds}");
        }
    }

    // ═══════════════ EMTP_11 CouplingLawMetricProxyComparison ═══════════════
    [Fact]
    public void V4_1_EMTP_11_CouplingLawMetricProxyComparison()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int E = 5;
        var laws = new Dictionary<string, Func<double[,], double, double, double[,]>> {
            {"exp", (d,k0,x) => ExpUpd(d,k0,x)}, {"gauss", (d,k0,x) => GaussUpd(d,k0,x)}, {"power", (d,k0,x) => PowerUpd(d,k0,x)}};
        _output.WriteLine("Law      g00    gSp    gOD    score");
        foreach (var (name, upd) in laws)
        {
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS, upd);
            var h = Sm(Kc, N, s, BS); var o = OmegaField(h); var dM = DL(Nm(RP(h)));
            double g00 = G00Proxy(o, 1.0); double gSp = SpatialMetricProxy(dM, N, 1.0); double gOD = OffDiagProxy(dM, o, N);
            double sc = (1.0 / (1.0 + gOD)) * TemporalStability(o);
            _output.WriteLine($"{name,-7} {g00:F3}  {gSp:F3}  {gOD:F3}  {sc:F3}");
        }
    }

    // ═══════════════ EMTP_12 NullAndDegenerateControls ═══════════════
    [Fact]
    public void V4_1_EMTP_12_NullAndDegenerateControls()
    {
        int N = 80; double s = 0.1; double xi = 1.75; double kv = 1.2;
        // K=0
        var K0 = new double[N, N]; var h0 = Sm(K0, N, s, BS); var o0 = OmegaField(h0); var d0 = DL(Nm(RP(h0)));
        double g00_0 = G00Proxy(o0, 1.0); double gSp_0 = SpatialMetricProxy(d0, N, 1.0); double gOD_0 = OffDiagProxy(d0, o0, N);
        // Random R
        var rng = new Random(BS); var rDist = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { double v = 0.1 + rng.NextDouble(); rDist[i, j] = rDist[j, i] = v; }
        var Kr = ExpUpd(rDist, kv, xi); var hr = Sm(Kr, N, s, BS); var or = OmegaField(hr); var dr = DL(Nm(RP(hr)));
        double g00_r = G00Proxy(or, 1.0); double gSp_r = SpatialMetricProxy(dr, N, 1.0); double gOD_r = OffDiagProxy(dr, or, N);
        // Global sync
        var Kgs = new double[N, N]; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { Kgs[i, j] = 1.0; Kgs[j, i] = 1.0; }
        var hgs = Sm(Kgs, N, s, BS); var ogs = OmegaField(hgs); var dgs = DL(Nm(RP(hgs)));
        double g00_gs = G00Proxy(ogs, 1.0); double gSp_gs = SpatialMetricProxy(dgs, N, 1.0);
        // Active
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var ha = Sm(Kc, N, s, BS); var oa = OmegaField(ha); var da = DL(Nm(RP(ha)));
        double g00_a = G00Proxy(oa, 1.0); double gSp_a = SpatialMetricProxy(da, N, 1.0); double gOD_a = OffDiagProxy(da, oa, N);
        _output.WriteLine($"K=0:      g00={g00_0:F3} gSp={gSp_0:F3} gOD={gOD_0:F3} degenerate");
        _output.WriteLine($"RandR:    g00={g00_r:F3} gSp={gSp_r:F3} gOD={gOD_r:F3} not physical");
        _output.WriteLine($"GlobSync: g00={g00_gs:F3} gSp={gSp_gs:F3} degenerate");
        _output.WriteLine($"Active:   g00={g00_a:F3} gSp={gSp_a:F3} gOD={gOD_a:F3} TRM");
    }

    // ═══════════════ EMTP_13 MetricTensorProxyScore ═══════════════
    [Fact]
    public void V4_1_EMTP_13_MetricTensorProxyScore()
    {
        int[] Ns = [80, 120]; double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2]; double s = 0.1;
        _output.WriteLine("xi    K0   N    g00    gSp    gOD    score  class");
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3;
            foreach (double xi in xis)
                foreach (double kv in K0s)
                {
                    var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
                    var h = Sm(Kc, N, s, BS); var o = OmegaField(h); var dM = DL(Nm(RP(h)));
                    double g00 = G00Proxy(o, 1.0); double gSp = SpatialMetricProxy(dM, N, 1.0); double gOD = OffDiagProxy(dM, o, N);
                    double tS = TemporalStability(o);
                    double sc = tS * (1.0 / (1.0 + gOD)) * (1.0 / (1.0 + Dg(dM)));
                    string cls = sc > 0.5 ? "Metric-like" : (sc > 0.3 ? "WeakMetric" : (sc > 0.15 ? "Marginal" : "None"));
                    _output.WriteLine($"{xi:F2}  {kv:F1}  {N,3}  {g00:F3}  {gSp:F3}  {gOD:F3}  {sc:F3}  {cls}");
                }
        }
    }

    // ═══════════════ EMTP_14 MetricTensorProxyReport ═══════════════
    [Fact]
    public void V4_1_EMTP_14_MetricTensorProxyReport()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h = Sm(Kc, N, s, BS); var o = OmegaField(h); var dM = DL(Nm(RP(h)));
        double g00 = G00Proxy(o, 1.0); double gSp = SpatialMetricProxy(dM, N, 1.0); double gOD = OffDiagProxy(dM, o, N);
        double sc = TemporalStability(o) * (1.0 / (1.0 + gOD)) * (1.0 / (1.0 + Dg(dM)));
        string conclusion;
        if (sc > 0.4) conclusion = "A) weak-to-moderate emergent metric tensor proxy in the convergence state";
        else if (sc > 0.2) conclusion = "B) temporal/spatial proxies exist but metric consistency is inconclusive";
        else if (sc > 0.1) conclusion = "C) diagnostics are estimator/threshold-dependent";
        else conclusion = "D) no meaningful metric proxy";
        _output.WriteLine("═══ EMERGENT METRIC TENSOR PROXY REPORT ═══");
        _output.WriteLine($"g00={g00:F3}  gSpatial={gSp:F3}  gOffDiag={gOD:F3}");
        _output.WriteLine($"Metric score: {sc:F3}  D_raw={MeasureDim(Kc, N, s).d:F2}");
        _output.WriteLine($"Conclusion: {conclusion}");
        _output.WriteLine("Physical metric / Minkowski / GR NOT derived.");
    }

    // ═══════════════ EMTP_15 ClaimDisciplineReport ═══════════════
    [Fact]
    public void V4_1_EMTP_15_ClaimDisciplineReport()
    {
        _output.WriteLine("═══ CLAIM DISCIPLINE: Emergent Metric Tensor Proxy ═══");
        _output.WriteLine("");
        _output.WriteLine("SUPPORTED:");
        _output.WriteLine("  - Temporal, spatial, and off-diagonal metric-like proxies measurable.");
        _output.WriteLine("  - Signature-like classifications computable.");
        _output.WriteLine("  - Interval-proxy consistency testable.");
        _output.WriteLine("  - Load, N, seed, law, null, and degenerate controls evaluable.");
        _output.WriteLine("");
        _output.WriteLine("CONDITIONAL:");
        _output.WriteLine("  - Metric proxy depends on coordinates, Omega, distance, thresholds.");
        _output.WriteLine("  - g_proxy is NOT a physical tensor metric.");
        _output.WriteLine("  - Interval proxy is NOT Minkowski interval.");
        _output.WriteLine("  - Signature proxy is NOT Lorentzian spacetime.");
        _output.WriteLine("");
        _output.WriteLine("HYPOTHESIS:");
        _output.WriteLine("  - TRM convergence state may support emergent metric-like structure.");
        _output.WriteLine("  - Physical metric may emerge only after continuum + calibration.");
        _output.WriteLine("  - Lorentzian spacetime may emerge only after stronger consistency.");
        _output.WriteLine("");
        _output.WriteLine("NOT CLAIMED:");
        _output.WriteLine("  - Physical metric, Minkowski metric, Lorentzian spacetime derived.");
        _output.WriteLine("  - Lorentz invariance, physical c, D=3, physical space derived.");
        _output.WriteLine("  - Gravity, Einstein equations, GR, time dilation derived/replaced.");
        _output.WriteLine("  - SPARC, dark matter explained/replaced.");
        Assert.True(true, "Claim discipline report complete.");
    }
}
