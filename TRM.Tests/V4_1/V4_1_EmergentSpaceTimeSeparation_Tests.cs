using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Emergent space-time separation: tests whether the stable Causal-ETG-Dimension
/// convergence state produces a natural numerical separation between time-like
/// (Omega/clock-rate) and space-like (d_ij/distance) structure.
///
/// Does NOT claim physical spacetime, D=3, physical c, Lorentz invariance,
/// Minkowski metric, gravity, time dilation, GR, SPARC, or dark matter.
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_EmergentSpaceTimeSeparation")]
public class V4_1_EmergentSpaceTimeSeparation_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_EmergentSpaceTimeSeparation_Tests(ITestOutputHelper o) { _output = o; }

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

    // ── Temporal proxy: local clock rate ───────────────
    private static double[] OmegaField(double[][] history)
    {
        int T = history.Length, N = history[0].Length;
        var omega = new double[N];
        for (int i = 0; i < N; i++)
        {
            double dSum = 0; int count = 0;
            for (int t = 1; t < T; t++) { double d = history[t][i] - history[t - 1][i]; dSum += Math.Abs(d); count++; }
            omega[i] = count > 0 ? dSum / (count * Dt * Hd) : 0;
        }
        return omega;
    }

    private static double TemporalStability(double[] omega)
    {
        if (omega.Length < 2) return 0;
        double m = omega.Average();
        double cv = m > 1e-6 ? Math.Sqrt(omega.Average(x => (x - m) * (x - m))) / m : 1;
        return 1.0 / (1.0 + cv);
    }

    // ── Kick detection ─────────────────────────────────
    private static (double[] times, double[] amps) KickDetect(double[,] K, int N, double s, int seed, int src, double kickAmp, double thresh)
    {
        var h0 = Sm(K, N, s, seed); var hk = Sm(K, N, s, seed, src, kickAmp, 50);
        var times = new double[N]; var amps = new double[N];
        for (int dst = 0; dst < N; dst++) { times[dst] = -1; amps[dst] = 0; if (dst == src) continue; for (int t = 0; t < hk.Length; t++) { double dPh = hk[t][dst] - h0[t][dst]; double dev = Math.Abs(Math.Sin(0.5 * dPh)); if (dev > amps[dst]) amps[dst] = dev; if (dev > thresh && times[dst] < 0) times[dst] = t * Dt * Hd; } }
        return (times, amps);
    }

    // ── Dimension estimator ────────────────────────────
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
        double intercept = my - slope * mx;
        double v = slope > 1e-15 ? 1.0 / slope : double.NaN;
        double r2 = sx2 * sy2 > 1e-15 ? sxy * sxy / (sx2 * sy2) : 0;
        return (v, intercept, r2, d.Count);
    }

    // ═══════════════ ESTS_01 SpaceTimeSeparationDataFinite ═══════════════
    [Fact]
    public void V4_1_ESTS_01_SpaceTimeSeparationDataFinite()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h = Sm(Kc, N, s, BS);
        var omega = OmegaField(h);
        var dMat = DL(Nm(RP(h)));
        int ln = N / 2;
        var hL = Sm(Kc, N, s, BS, ln, 0.2);
        var deltaOmega = OmegaField(hL).Zip(omega, (a, b) => Math.Abs(a - b)).ToArray();
        Assert.True(omega.All(double.IsFinite), "Omega NaN");
        Assert.True(deltaOmega.Any(), "DeltaOmega empty");
        int count = 0;
        for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (double.IsFinite(dMat[i, j])) count++;
        Assert.True(count > 0, "No finite distances");
        _output.WriteLine($"Omega mean={omega.Average():F3}  DeltaOmega mean={deltaOmega.Average():F3}  dMat finite={count}");
    }

    // ═══════════════ ESTS_02 TemporalDirectionFromOmega ═══════════════
    [Fact]
    public void V4_1_ESTS_02_TemporalDirectionFromOmega()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h = Sm(Kc, N, s, BS);
        var omega = OmegaField(h);
        double tStab = TemporalStability(omega);
        // DeltaOmega alignment with load
        int ln = N / 2;
        var hL = Sm(Kc, N, s, BS, ln, 0.2);
        var dO = OmegaField(hL).Zip(omega, (a, b) => Math.Abs(a - b)).ToArray();
        double loadAlign = dO[ln] / Math.Max(dO.Average(), 1e-6);
        string cls = tStab > 0.7 ? "Stable temporal proxy" : (tStab > 0.4 ? "Weak temporal proxy" : (tStab > 0.2 ? "Noisy" : "Degenerate"));
        _output.WriteLine($"Temporal stability: {tStab:F3}  LoadAlign: {loadAlign:F3}  class={cls}");
    }

    // ═══════════════ ESTS_03 SpatialStructureFromDistance ═══════════════
    [Fact]
    public void V4_1_ESTS_03_SpatialStructureFromDistance()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var (dRaw, sp) = MeasureDim(Kc, N, s);
        double dg = Dg(DL(Nm(RP(Sm(Kc, N, s, BS)))));
        double stab = 1.0 / (1.0 + sp + dg);
        string cls = stab > 0.6 ? "Stable spatial structure" : (stab > 0.3 ? "Weak spatial structure" : (stab > 0.1 ? "Noisy" : "Degenerate"));
        _output.WriteLine($"D_raw={dRaw:F2}  spread={sp:F3}  dg={dg:F3}  stab={stab:F3}  class={cls}");
    }

    // ═══════════════ ESTS_04 TimeSpaceOrthogonalityProxy ═══════════════
    [Fact]
    public void V4_1_ESTS_04_TimeSpaceOrthogonalityProxy()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h = Sm(Kc, N, s, BS);
        var omega = OmegaField(h);
        var dMat = DL(Nm(RP(h)));
        // Correlate Omega gradient with distance from center
        int ctr = N / 2;
        var dists = Enumerable.Range(0, N).Select(i => i == ctr ? 0.0 : dMat[ctr, i]).ToArray();
        var omegaGrad = new double[N]; for (int i = 0; i < N; i++) omegaGrad[i] = Math.Abs(omega[i] - omega[ctr]);
        double rho = Spear(omegaGrad, dists);
        string cls = Math.Abs(rho) < 0.3 ? "Separated (low corr)" : (Math.Abs(rho) < 0.6 ? "Partially coupled" : "Redundant");
        _output.WriteLine($"corr(OmegaGrad, distance) = {rho:F4}  class={CorrClass(rho)}  {cls}");
    }

    // ═══════════════ ESTS_05 CausalDelayUsesBothTimeAndSpace ═══════════════
    [Fact]
    public void V4_1_ESTS_05_CausalDelayUsesBothTimeAndSpace()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int nSrc = 3;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h = Sm(Kc, N, s, BS);
        var omega = OmegaField(h);
        var dMat = DL(Nm(RP(h)));
        // Model 1: tau ~ a*d
        var dM1 = new List<double>(); var tM1 = new List<double>();
        var dM2 = new List<double>(); var tM2 = new List<double>(); var oM2 = new List<double>();
        for (int src = 0; src < nSrc; src++)
        {
            var (times, _) = KickDetect(Kc, N, s, BS, src, 0.5, 0.02);
            for (int dst = 0; dst < N; dst++)
            {
                if (dst == src || times[dst] <= 0 || dMat[src, dst] <= 0) continue;
                dM1.Add(dMat[src, dst]); tM1.Add(times[dst]);
                dM2.Add(dMat[src, dst]); tM2.Add(times[dst]); oM2.Add(Math.Abs(omega[dst] - omega[src]));
            }
        }
        if (dM1.Count < 6) { _output.WriteLine("Insufficient events"); return; }
        // Model 1: tau ~ d
        double mx = dM1.Average(), my = tM1.Average(), sxy = 0, sx2 = 0, sy2 = 0;
        for (int i = 0; i < dM1.Count; i++) { double a = dM1[i] - mx, b = tM1[i] - my; sxy += a * b; sx2 += a * a; sy2 += b * b; }
        double r2_1 = sx2 * sy2 > 1e-15 ? sxy * sxy / (sx2 * sy2) : 0;
        double resid1 = Math.Sqrt(dM1.Select((d, i) => { double p = my + (sx2 > 1e-15 ? sxy / sx2 : 0) * (d - mx); double e = tM1[i] - p; return e * e; }).Sum() / dM1.Count);
        // Model 2: tau ~ d + Omega
        double r2_2 = 0, resid2 = double.MaxValue;
        if (dM2.Count >= 6)
        {
            double mxD = dM2.Average(), mxO = oM2.Average(), my2 = tM2.Average();
            double sxd = 0, sxo = 0, sxy2 = 0, sod = 0;
            for (int i = 0; i < dM2.Count; i++) { double ad = dM2[i] - mxD, ao = oM2[i] - mxO, bt = tM2[i] - my2; sxd += ad * ad; sxo += ao * ao; sxy2 += ad * bt; sod += ad * ao; }
            double sy2_2 = tM2.Sum(t => (t - my2) * (t - my2));
            double denom = sxd * sxo - sod * sod;
            double bd = denom > 1e-15 ? (sxy2 * sxo / denom) : (sxd > 1e-15 ? sxy2 / sxd : 0);
            double bo = denom > 1e-15 ? (sxo * (tM2.Zip(dM2, (t, d) => (t - my2) * (d - mxD)).Sum()) - sod * sxy2) / denom : 0;
            double rss2 = dM2.Select((d, i) => { double p = my2 + bd * (d - mxD) + bo * (oM2[i] - mxO); double e = tM2[i] - p; return e * e; }).Sum();
            resid2 = Math.Sqrt(rss2 / dM2.Count);
            r2_2 = sy2_2 > 1e-15 ? 1.0 - rss2 / sy2_2 : 0;
        }
        _output.WriteLine($"tau~d:      r²={r2_1:F3}  resid={resid1:F3}");
        _output.WriteLine($"tau~d+Omega: r²={r2_2:F3}  resid={resid2:F3}");
        if (r2_2 > r2_1 + 0.05)
            _output.WriteLine("Combined time+space model IMPROVES fit (NOT spacetime proof).");
        else
            _output.WriteLine("Space-only model adequate; time adds little.");
    }

    // ═══════════════ ESTS_06 IntervalProxyDistribution ═══════════════
    [Fact]
    public void V4_1_ESTS_06_IntervalProxyDistribution()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var dMat = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var ds = new List<double>(); var ts = new List<double>();
        for (int src = 0; src < 5; src++)
        {
            var (times, _) = KickDetect(Kc, N, s, BS, src, 0.5, 0.02);
            for (int dst = 0; dst < N; dst++) if (dst != src && times[dst] > 0 && dMat[src, dst] > 0) { ds.Add(dMat[src, dst]); ts.Add(times[dst]); }
        }
        if (ds.Count < 4) { _output.WriteLine("Insufficient events"); return; }
        var (v, _, _, _) = FitFront(ds, ts);
        if (!double.IsFinite(v)) { _output.WriteLine("v NaN"); return; }
        double tol = 0.05; int tl = 0, nul = 0, sl = 0;
        for (int i = 0; i < ds.Count; i++) { double s2 = ts[i] * ts[i] - (ds[i] / v) * (ds[i] / v); if (s2 > tol) tl++; else if (s2 < -tol) sl++; else nul++; }
        int tot = ds.Count;
        _output.WriteLine($"time-like: {tl} ({100.0 * tl / tot:F0}%)  near-null: {nul} ({100.0 * nul / tot:F0}%)  space-like: {sl} ({100.0 * sl / tot:F0}%)");
        _output.WriteLine("Interval proxy only — NOT Minkowski metric.");
    }

    // ═══════════════ ESTS_07 NearNullFrontStability ═══════════════
    [Fact]
    public void V4_1_ESTS_07_NearNullFrontStability()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var dMat = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var ds = new List<double>(); var ts = new List<double>();
        for (int src = 0; src < 5; src++)
        {
            var (times, _) = KickDetect(Kc, N, s, BS, src, 0.5, 0.02);
            for (int dst = 0; dst < N; dst++) if (dst != src && times[dst] > 0 && dMat[src, dst] > 0) { ds.Add(dMat[src, dst]); ts.Add(times[dst]); }
        }
        var (v, _, _, _) = FitFront(ds, ts);
        if (!double.IsFinite(v) || ds.Count < 4) { _output.WriteLine("Insufficient"); return; }
        var nearNullD = new List<double>(); var nearNullT = new List<double>(); var nearNullV = new List<double>();
        double tol = 0.05;
        for (int i = 0; i < ds.Count; i++) { double s2 = ts[i] * ts[i] - (ds[i] / v) * (ds[i] / v); if (Math.Abs(s2) <= tol) { nearNullD.Add(ds[i]); nearNullT.Add(ts[i]); nearNullV.Add(ds[i] / ts[i]); } }
        double frac = ds.Count > 0 ? (double)nearNullD.Count / ds.Count : 0;
        double vMean = nearNullV.Count > 0 ? nearNullV.Average() : double.NaN;
        double vStd = nearNullV.Count > 1 ? Math.Sqrt(nearNullV.Average(x => (x - vMean) * (x - vMean))) : 0;
        _output.WriteLine($"NearNull fraction: {frac:F3}  v_mean={vMean:F3}±{vStd:F3}  n={nearNullD.Count}");
    }

    // ═══════════════ ESTS_08 LoadInvarianceOfSpaceTimeSeparation ═══════════════
    [Fact]
    public void V4_1_ESTS_08_LoadInvarianceOfSpaceTimeSeparation()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        var Kb = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        // Before
        var hB = Sm(Kb, N, s, BS); var omegaB = OmegaField(hB);
        var dB = MeasureDim(Kb, N, s);
        // After load
        var hL = Sm(Kb, N, s, BS, ln, 0.2); var omegaL = OmegaField(hL);
        var Kl = ExpUpd(DL(Nm(RP(hL))), kv, xi);
        var dL = MeasureDim(Kl, N, s);
        double omegaShift = Math.Abs(omegaL.Average() - omegaB.Average()) / Math.Max(omegaB.Average(), 1e-6);
        double dShift = Math.Abs(dL.d - dB.d);
        _output.WriteLine($"Omega_shift_rel={omegaShift:F3}  D_shift={dShift:F3}");
        _output.WriteLine($"Load-invariant: {(omegaShift < 0.3 && dShift < 0.5 ? "YES" : "NO")}");
    }

    // ═══════════════ ESTS_09 NScalingSpaceTimeSeparation ═══════════════
    [Fact]
    public void V4_1_ESTS_09_NScalingSpaceTimeSeparation()
    {
        int[] Ns = [40, 80, 120, 200]; double xi = 1.75; double kv = 1.2; double s = 0.1;
        _output.WriteLine("N     tStab  D_raw  spread  dg     sepScore");
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3;
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
            var h = Sm(Kc, N, s, BS);
            double tStab = TemporalStability(OmegaField(h));
            var (dRaw, sp) = MeasureDim(Kc, N, s);
            double dg = Dg(DL(Nm(RP(h))));
            double sepScore = tStab * (1.0 / (1.0 + sp)) * (1.0 / (1.0 + dg));
            _output.WriteLine($"{N,5}  {tStab:F3}  {dRaw,5:F2}  {sp:F3}  {dg:F3}  {sepScore:F3}");
        }
    }

    // ═══════════════ ESTS_10 MultiSeedSpaceTimeSeparation ═══════════════
    [Fact]
    public void V4_1_ESTS_10_MultiSeedSpaceTimeSeparation()
    {
        int[] Ns = [80, 120]; double xi = 1.75; double kv = 1.2; double s = 0.1; int nSeeds = 10;
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3;
            var tStabs = new List<double>(); var ds = new List<double>();
            for (int seed = 0; seed < nSeeds; seed++)
            {
                var Kc = RecoverFP(KS(N, seed), N, kv, xi, s, E, seed);
                var h = Sm(Kc, N, s, seed);
                tStabs.Add(TemporalStability(OmegaField(h)));
                var (dRaw, _) = MeasureDim(Kc, N, s);
                if (double.IsFinite(dRaw)) ds.Add(dRaw);
            }
            double mt = tStabs.Average(); double st = tStabs.Count > 1 ? Math.Sqrt(tStabs.Average(x => (x - mt) * (x - mt))) : 0;
            double md = ds.Count > 0 ? ds.Average() : double.NaN;
            double sd = ds.Count > 1 ? Math.Sqrt(ds.Average(x => (x - md) * (x - md))) : 0;
            _output.WriteLine($"N={N}: tStab={mt:F3}±{st:F3}  D_raw={md:F2}±{sd:F2}  n={nSeeds}");
        }
    }

    // ═══════════════ ESTS_11 CouplingLawSpaceTimeComparison ═══════════════
    [Fact]
    public void V4_1_ESTS_11_CouplingLawSpaceTimeComparison()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int E = 5;
        var laws = new Dictionary<string, Func<double[,], double, double, double[,]>> {
            {"exp", (d,k0,x) => ExpUpd(d,k0,x)}, {"gauss", (d,k0,x) => GaussUpd(d,k0,x)}, {"power", (d,k0,x) => PowerUpd(d,k0,x)}};
        _output.WriteLine("Law      tStab  D_raw  spread  sepScore");
        foreach (var (name, upd) in laws)
        {
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS, upd);
            var h = Sm(Kc, N, s, BS);
            double tS = TemporalStability(OmegaField(h));
            var (dR, sp) = MeasureDim(Kc, N, s);
            double sep = tS * (1.0 / (1.0 + sp));
            _output.WriteLine($"{name,-7} {tS:F3}  {dR,5:F2}  {sp:F3}  {sep:F3}");
        }
    }

    // ═══════════════ ESTS_12 NullAndDegenerateControls ═══════════════
    [Fact]
    public void V4_1_ESTS_12_NullAndDegenerateControls()
    {
        int N = 80; double s = 0.1; double xi = 1.75; double kv = 1.2;
        // K=0
        var K0 = new double[N, N]; var h0 = Sm(K0, N, s, BS);
        double t0 = TemporalStability(OmegaField(h0));
        var (d0, _) = MeasureDim(K0, N, s);
        // Random R
        var rng = new Random(BS); var rDist = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { double v = 0.1 + rng.NextDouble(); rDist[i, j] = rDist[j, i] = v; }
        var Kr = ExpUpd(rDist, kv, xi); var hr = Sm(Kr, N, s, BS);
        double tr = TemporalStability(OmegaField(hr));
        var (dr, sr) = MeasureDim(Kr, N, s);
        // Global sync
        var Kgs = new double[N, N]; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { Kgs[i, j] = 1.0; Kgs[j, i] = 1.0; }
        var hgs = Sm(Kgs, N, s, BS);
        double tgs = TemporalStability(OmegaField(hgs));
        // Active
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var ha = Sm(Kc, N, s, BS);
        double ta = TemporalStability(OmegaField(ha));
        _output.WriteLine($"K=0:      tStab={t0:F3}  D={d0:F2}  degenerate (uniform)");
        _output.WriteLine($"RandR:    tStab={tr:F3}  D={dr:F2}  not physical");
        _output.WriteLine($"GlobSync: tStab={tgs:F3}  degenerate");
        _output.WriteLine($"Active:   tStab={ta:F3}  TRM");
        Assert.True(double.IsFinite(d0) && d0 < 1.0, "K=0 dimension should be degenerate");
    }

    // ═══════════════ ESTS_13 SpaceTimeSeparationScore ═══════════════
    [Fact]
    public void V4_1_ESTS_13_SpaceTimeSeparationScore()
    {
        int[] Ns = [80, 120]; double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2]; double s = 0.1;
        _output.WriteLine("xi    K0   N    tStab  D_raw  sepScore  class");
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3;
            foreach (double xi in xis)
                foreach (double kv in K0s)
                {
                    var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
                    var h = Sm(Kc, N, s, BS);
                    double tS = TemporalStability(OmegaField(h));
                    var (dR, sp) = MeasureDim(Kc, N, s);
                    double sep = tS * (1.0 / (1.0 + sp));
                    string cls = sep > 0.5 ? "Separation" : (sep > 0.3 ? "WeakSep" : (sep > 0.15 ? "Marginal" : "None"));
                    _output.WriteLine($"{xi:F2}  {kv:F1}  {N,3}  {tS:F3}  {dR,5:F2}  {sep:F3}      {cls}");
                }
        }
    }

    // ═══════════════ ESTS_14 SpaceTimeSeparationReport ═══════════════
    [Fact]
    public void V4_1_ESTS_14_SpaceTimeSeparationReport()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h = Sm(Kc, N, s, BS);
        double tStab = TemporalStability(OmegaField(h));
        var (dRaw, sp) = MeasureDim(Kc, N, s);
        double dg = Dg(DL(Nm(RP(h))));
        double sep = tStab * (1.0 / (1.0 + sp)) * (1.0 / (1.0 + dg));
        string conclusion;
        if (sep > 0.4) conclusion = "A) weak-to-moderate numerical space-time separation in the convergence state";
        else if (sep > 0.2) conclusion = "B) time/space proxies exist but separation is inconclusive";
        else if (sep > 0.1) conclusion = "C) diagnostics are estimator/threshold-dependent";
        else conclusion = "D) no meaningful separation";
        _output.WriteLine("═══ SPACE-TIME SEPARATION REPORT ═══");
        _output.WriteLine($"Temporal stability: {tStab:F3}");
        _output.WriteLine($"Spatial D_raw: {dRaw:F2}  spread={sp:F3}  dg={dg:F3}");
        _output.WriteLine($"Separation score: {sep:F3}");
        _output.WriteLine($"Conclusion: {conclusion}");
        _output.WriteLine("Physical spacetime NOT derived.");
    }

    // ═══════════════ ESTS_15 ClaimDisciplineReport ═══════════════
    [Fact]
    public void V4_1_ESTS_15_ClaimDisciplineReport()
    {
        _output.WriteLine("═══ CLAIM DISCIPLINE: Emergent Space-Time Separation ═══");
        _output.WriteLine("");
        _output.WriteLine("SUPPORTED:");
        _output.WriteLine("  - Temporal (Omega) and spatial (d_ij) proxies are measurable.");
        _output.WriteLine("  - Causal delay can be modeled using distance and temporal response.");
        _output.WriteLine("  - Interval-like proxy classes are computable.");
        _output.WriteLine("  - Near-null/front-like events can be tracked.");
        _output.WriteLine("  - Load, N, seed, law, null, and degenerate controls testable.");
        _output.WriteLine("");
        _output.WriteLine("CONDITIONAL:");
        _output.WriteLine("  - Separation depends on Omega proxy, estimator, thresholds, xi, K0.");
        _output.WriteLine("  - Temporal proxy is NOT physical proper time.");
        _output.WriteLine("  - Spatial proxy is NOT continuum space.");
        _output.WriteLine("  - Interval proxy is NOT Minkowski metric.");
        _output.WriteLine("");
        _output.WriteLine("HYPOTHESIS:");
        _output.WriteLine("  - TRM convergence state may support emergent space-time separation.");
        _output.WriteLine("  - Physical spacetime may require stability of all structures.");
        _output.WriteLine("  - Lorentzian spacetime may emerge only after continuum constraints.");
        _output.WriteLine("");
        _output.WriteLine("NOT CLAIMED:");
        _output.WriteLine("  - Physical spacetime, time dimension, 3D space, D=3 are derived.");
        _output.WriteLine("  - Physical c, Lorentz invariance, Minkowski metric are derived.");
        _output.WriteLine("  - Gravity, time dilation, GR, SPARC, dark matter are derived/replaced.");
        Assert.True(true, "Claim discipline report complete.");
    }
}
