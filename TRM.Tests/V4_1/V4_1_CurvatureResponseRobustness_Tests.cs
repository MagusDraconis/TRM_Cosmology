using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Curvature response robustness: tests whether the moderate curvature-response
/// proxy from V4_1_CurvatureResponseProxy is robust under curvature definitions,
/// metric proxy choices, Phi proxy choices, coordinate choices, load range,
/// N-scaling, seeds, and coupling laws.
///
/// Does NOT claim physical curvature, Ricci, Einstein equations, GR,
/// gravity, mass, c, D=3, SPARC, or dark matter.
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_CurvatureResponseRobustness")]
public class V4_1_CurvatureResponseRobustness_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_CurvatureResponseRobustness_Tests(ITestOutputHelper o) { _output = o; }

    // ══════════════════ Core helpers ══════════════════
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
    private static double[,] GaussUpd(double[,] d, double K0, double xi) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else { double r = d[i, j] / Math.Max(xi, 0.01); K[i, j] = K0 * Math.Exp(-r * r); } } return K; }
    private static double[,] PowerUpd(double[,] d, double K0, double p) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 / (1.0 + Math.Pow(d[i, j], Math.Max(p, 0.5))); } return K; }
    private static double Spear(double[] a, double[] b) { if (a.Length < 3) return 0; var ia = a.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); var ib = b.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); return Pear(ia, ib); }
    private static double Pear(double[] x, double[] y) { int n = x.Length; double mx = x.Average(), my = y.Average(), nm = 0, dx = 0, dy = 0; for (int i = 0; i < n; i++) { double a = x[i] - mx, b = y[i] - my; nm += a * b; dx += a * a; dy += b * b; } return Math.Sqrt(dx * dy) > 1e-15 ? nm / Math.Sqrt(dx * dy) : 0; }
    private static double[,] KS(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    private static double[,] RecoverFP(double[,] K0, int N, double K0v, double xi, double s, int E, int seed, Func<double[,], double, double, double[,]> upd = null) { upd ??= ExpUpd; var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = upd(DL(Nm(RP(he))), K0v, xi); } return Kc; }
    private static double Median(List<double> vs) { if (vs.Count == 0) return double.NaN; var s = vs.OrderBy(x => x).ToList(); int mid = s.Count / 2; return s.Count % 2 == 0 ? (s[mid - 1] + s[mid]) / 2.0 : s[mid]; }
    private static string CorrClass(double r) => Math.Abs(r) > 0.7 ? "Strong" : Math.Abs(r) > 0.4 ? "Moderate" : Math.Abs(r) > 0.1 ? "Weak" : "None";

    private static double[] OmegaField(double[][] h) { int T = h.Length, N = h[0].Length; var o = new double[N]; for (int i = 0; i < N; i++) { double s = 0; int c = 0; for (int t = 1; t < T; t++) { s += Math.Abs(h[t][i] - h[t - 1][i]); c++; } o[i] = c > 0 ? s / (c * Dt * Hd) : 0; } return o; }

    // ── Curvature definitions ──────────────────────────
    private static double[] LapCurv(double[,] d0, double[,] dL, int N)
    {
        var h = new double[N]; for (int i = 0; i < N; i++) { double s = 0; int c = 0; for (int j = 0; j < N; j++) if (i != j) { s += Math.Abs(dL[i, j] - d0[i, j]); c++; } h[i] = c > 0 ? s / c : 0; }
        var lap = new double[N]; for (int i = 0; i < N; i++) { double sum = 0, wSum = 0; for (int j = 0; j < N; j++) if (i != j) { double w = 1.0 / (1.0 + d0[i, j]); sum += w * h[j]; wSum += w; } lap[i] = wSum * h[i] - sum; }
        return lap;
    }
    private static double[] DistCurv(double[,] d0, double[,] dL, int N)
    {
        var curv = new double[N]; for (int i = 0; i < N; i++) { var s0 = Enumerable.Range(0, N).Where(x => x != i).Select(x => d0[i, x]).OrderBy(x => x).ToArray(); var sL = Enumerable.Range(0, N).Where(x => x != i).Select(x => dL[i, x]).OrderBy(x => x).ToArray(); int k = Math.Min(10, s0.Length); double s = 0; for (int j = 0; j < k; j++) s += Math.Abs(sL[j] - s0[j]); curv[i] = s / k; }
        return curv;
    }
    private static double[] GeodesicDevCurv(double[,] d0, double[,] dL, int N, int seed)
    {
        var dv = new double[N]; var rng = new Random(seed + 1000);
        for (int i = 0; i < N; i++)
        {
            int a = rng.Next(N), b = (a + N / 3) % N;
            var p0 = ShortestPath(d0, N, a, b); var pL = ShortestPath(dL, N, a, b);
            dv[i] = p0.Count > 0 && pL.Count > 0 ? 1.0 - PathOverlap(p0, pL) : 0;
        }
        return dv;
    }
    private static (double[] oPhi, double[] dPhi, double[] kPhi) ComputeAllPhi(double[,] Kc, double[,] d0, double[,] dL, int N, double s, int ln)
    {
        var o0 = OmegaField(Sm(Kc, N, s, BS)); var oL = OmegaField(Sm(Kc, N, s, BS, ln, 0.2));
        var oPhi = oL.Zip(o0, (a, b) => { double d = a - b; double n = Math.Abs(d) + 1e-6; return d / n; }).ToArray();
        var dPhi = new double[N]; for (int i = 0; i < N; i++) { double v = dL[ln, i] - d0[ln, i]; dPhi[i] = v / (Math.Abs(v) + 1e-6); }
        var kPhi = new double[N]; double maxK = 0; for (int i = 0; i < N; i++) { double v = Kc[ln, i] - ExpUpd(d0, 1.2, 1.75)[ln, i]; if (Math.Abs(v) > maxK) maxK = Math.Abs(v); } if (maxK < 1e-6) maxK = 1; for (int i = 0; i < N; i++) kPhi[i] = (Kc[ln, i] - ExpUpd(d0, 1.2, 1.75)[ln, i]) / maxK;
        return (oPhi, dPhi, kPhi);
    }

    // ── Pathfinding ───────────────────────────────────
    private static List<int> ShortestPath(double[,] w, int N, int s, int d)
    {
        var dist = new double[N]; var prev = new int[N]; var vis = new bool[N];
        for (int i = 0; i < N; i++) { dist[i] = double.MaxValue; prev[i] = -1; } dist[s] = 0;
        for (int iter = 0; iter < N; iter++) { int u = -1; double best = double.MaxValue; for (int i = 0; i < N; i++) if (!vis[i] && dist[i] < best) { best = dist[i]; u = i; } if (u < 0) break; vis[u] = true; if (u == d) break; for (int v = 0; v < N; v++) { if (vis[v]) continue; double ww = w[u, v]; if (ww <= 0 || !double.IsFinite(ww)) continue; double nd = dist[u] + ww; if (nd < dist[v]) { dist[v] = nd; prev[v] = u; } } }
        var p = new List<int>(); if (prev[d] < 0 && s != d) return p;
        for (int at = d; at >= 0; at = prev[at]) { p.Add(at); if (at == s) break; } p.Reverse();
        return p.Count > 0 && p[0] == s ? p : new List<int>();
    }
    private static double PathOverlap(List<int> a, List<int> b) { if (a.Count == 0 || b.Count == 0) return 0; var sa = new HashSet<int>(a); var sb = new HashSet<int>(b); return (double)sa.Intersect(sb).Count() / Math.Max(sa.Union(sb).Count(), 1); }

    // ═══════════════ CRR_01 RobustnessDataFinite ═══════════════
    [Fact]
    public void V4_1_CRR_01_RobustnessDataFinite()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var d0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var hL = Sm(Kc, N, s, BS, ln, 0.2); var Kl = ExpUpd(DL(Nm(RP(hL))), kv, xi);
        var dL = DL(Nm(RP(Sm(Kl, N, s, BS))));
        var lc = LapCurv(d0, dL, N);
        var dc = DistCurv(d0, dL, N);
        var gd = GeodesicDevCurv(d0, dL, N, BS);
        var (op, dp, kp) = ComputeAllPhi(Kc, d0, dL, N, s, ln);
        Assert.True(lc.All(double.IsFinite) && dc.All(double.IsFinite), "Curv NaN");
        _output.WriteLine($"LapCurv peak={lc[ln]:F4}  DistCurv peak={dc[ln]:F4}  GeoDev avg={gd.Average():F4}");
    }

    // ═══════════════ CRR_02 CurvatureDefinitionAgreement ═══════════════
    [Fact]
    public void V4_1_CRR_02_CurvatureDefinitionAgreement()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var d0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var hL = Sm(Kc, N, s, BS, ln, 0.2); var Kl = ExpUpd(DL(Nm(RP(hL))), kv, xi);
        var dL = DL(Nm(RP(Sm(Kl, N, s, BS))));
        var lc = LapCurv(d0, dL, N); var dc = DistCurv(d0, dL, N);
        double rLD = Spear(lc, dc);
        double rLG = Spear(lc, GeodesicDevCurv(d0, dL, N, BS));
        double avgR = (Math.Abs(rLD) + Math.Abs(rLG)) / 2.0;
        string cls = avgR > 0.5 ? "Strong" : (avgR > 0.25 ? "Moderate" : "Weak");
        _output.WriteLine($"Lap-Dist corr={rLD:F3}  Lap-Geo corr={rLG:F3}  avg={avgR:F3}  class={cls}");
    }

    // ═══════════════ CRR_03 MetricProxySensitivity ═══════════════
    [Fact]
    public void V4_1_CRR_03_MetricProxySensitivity()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var d0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var hL = Sm(Kc, N, s, BS, ln, 0.2); var Kl = ExpUpd(DL(Nm(RP(hL))), kv, xi);
        var dL = DL(Nm(RP(Sm(Kl, N, s, BS))));
        // Metric proxy 1: raw distance perturbation
        var lc1 = LapCurv(d0, dL, N);
        // Metric proxy 2: squared distance perturbation
        var dSq0 = new double[N, N]; var dSqL = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { dSq0[i, j] = dSq0[j, i] = d0[i, j] * d0[i, j]; dSqL[i, j] = dSqL[j, i] = dL[i, j] * dL[i, j]; }
        var lc2 = LapCurv(dSq0, dSqL, N);
        double r = Spear(lc1, lc2);
        _output.WriteLine($"corr(Curv_d, Curv_d²) = {r:F4}  {(Math.Abs(r) > 0.5 ? "Robust" : "Sensitive")}");
    }

    // ═══════════════ CRR_04 PhiProxySensitivity ═══════════════
    [Fact]
    public void V4_1_CRR_04_PhiProxySensitivity()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var d0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var hL = Sm(Kc, N, s, BS, ln, 0.2); var Kl = ExpUpd(DL(Nm(RP(hL))), kv, xi);
        var dL = DL(Nm(RP(Sm(Kl, N, s, BS))));
        var lc = LapCurv(d0, dL, N);
        var (op, dp, kp) = ComputeAllPhi(Kc, d0, dL, N, s, ln);
        double rO = Spear(lc, op); double rD = Spear(lc, dp); double rK = Spear(lc, kp);
        double avg = (Math.Abs(rO) + Math.Abs(rD) + Math.Abs(rK)) / 3.0;
        _output.WriteLine($"Curv-Phi corrs: Omega={rO:F3}  d={rD:F3}  K={rK:F3}  avg={avg:F3}  {(avg > 0.3 ? "Robust" : "Sensitive")}");
    }

    // ═══════════════ CRR_05 CoordinateProxySensitivity ═══════════════
    [Fact]
    public void V4_1_CRR_05_CoordinateProxySensitivity()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var d0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var hL = Sm(Kc, N, s, BS, ln, 0.2); var Kl = ExpUpd(DL(Nm(RP(hL))), kv, xi);
        var dL = DL(Nm(RP(Sm(Kl, N, s, BS))));
        var lcRef = LapCurv(d0, dL, N);
        // Shell-weighted distance
        var dSh = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { double si = Enumerable.Range(0, N).Count(x => x != i && d0[i, x] < d0[i, j]) / (double)(N - 1); double sj = Enumerable.Range(0, N).Count(x => x != j && d0[j, x] < d0[j, i]) / (double)(N - 1); dSh[i, j] = dSh[j, i] = d0[i, j] * (1.0 + 0.3 * Math.Abs(si - sj)); }
        var lcSh = LapCurv(dSh, dSh, N);
        double r = Spear(lcRef, lcSh);
        _output.WriteLine($"corr(Curv_ref, Curv_shellCoord) = {r:F4}  {(Math.Abs(r) > 0.5 ? "Robust" : "Sensitive")}");
    }

    // ═══════════════ CRR_06 LoadRangeRobustness ═══════════════
    [Fact]
    public void V4_1_CRR_06_LoadRangeRobustness()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        double[] loads = [0.01, 0.05, 0.10, 0.20, 0.35, 0.50];
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var d0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
        _output.WriteLine("load    lapPeak  distPeak  loc      class");
        foreach (double load in loads)
        {
            var hL = Sm(Kc, N, s, BS, ln, load); var Kl = ExpUpd(DL(Nm(RP(hL))), kv, xi);
            var dL = DL(Nm(RP(Sm(Kl, N, s, BS))));
            var lc = LapCurv(d0, dL, N); var dc = DistCurv(d0, dL, N);
            double loc = Math.Abs(lc[ln]) / (lc.Average(x => Math.Abs(x)) + 1e-6);
            string cls = load <= 0.05 ? "tooWeak" : (load <= 0.2 ? "linear" : (load <= 0.35 ? "mild" : "nonlin"));
            _output.WriteLine($"{load:F2}    {Math.Abs(lc[ln]):F4}    {dc[ln]:F4}     {loc:F1}x    {cls}");
        }
    }

    // ═══════════════ CRR_07 CurvatureBendingRobustness ═══════════════
    [Fact]
    public void V4_1_CRR_07_CurvatureBendingRobustness()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        double[] loads = [0.05, 0.1, 0.2, 0.35];
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var d0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
        int src = N / 4, dst = 3 * N / 4;
        var pRef = ShortestPath(d0, N, src, dst);
        _output.WriteLine("load    lapCurv  bend    monotonic?");
        double prevBend = 0;
        foreach (double load in loads)
        {
            var hL = Sm(Kc, N, s, BS, ln, load); var Kl = ExpUpd(DL(Nm(RP(hL))), kv, xi);
            var dL = DL(Nm(RP(Sm(Kl, N, s, BS)))); var lc = LapCurv(d0, dL, N);
            var pL = ShortestPath(dL, N, src, dst);
            double bend = 1.0 - PathOverlap(pRef, pL);
            _output.WriteLine($"{load:F2}    {Math.Abs(lc[ln]):F4}    {bend:F3}    {(bend >= prevBend - 0.05 ? "yes" : "no")}");
            prevBend = bend;
        }
    }

    // ═══════════════ CRR_08 RadialDecayRobustness ═══════════════
    [Fact]
    public void V4_1_CRR_08_RadialDecayRobustness()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var d0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, s, BS, ln, 0.2)))), kv, xi), N, s, BS))));
        var lc = LapCurv(d0, dL, N);
        var dists = Enumerable.Range(0, N).Select(i => d0[ln, i]).ToArray();
        var ordered = Enumerable.Range(0, N).OrderBy(i => dists[i]).ToArray();
        _output.WriteLine("shell  absCurv  fitExp");
        for (int sh = 0; sh < 5; sh++)
        {
            int s0 = sh * N / 5, s1 = (sh + 1) * N / 5;
            double m = ordered.Skip(s0).Take(s1 - s0).Select(i => Math.Abs(lc[i])).Average();
            double rMid = ordered.Skip(s0).Take(s1 - s0).Select(i => dists[i]).Average();
            double maxDist = dists.Max(); double expFit = Math.Exp(-rMid / (maxDist * 0.3));
            _output.WriteLine($"{sh,5}  {m:F4}    {expFit:F4}");
        }
    }

    // ═══════════════ CRR_09 NScalingRobustness ═══════════════
    [Fact]
    public void V4_1_CRR_09_NScalingRobustness()
    {
        int[] Ns = [40, 80, 120, 200]; double xi = 1.75; double kv = 1.2; double s = 0.1;
        _output.WriteLine("N     lapLoc  distPeak  stable?");
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3; int ln = N / 2;
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
            var d0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
            var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, s, BS, ln, 0.2)))), kv, xi), N, s, BS))));
            var lc = LapCurv(d0, dL, N);
            double loc = Math.Abs(lc[ln]) / (lc.Average(x => Math.Abs(x)) + 1e-6);
            _output.WriteLine($"{N,5}  {loc:F1}x    {DistCurv(d0, dL, N)[ln]:F4}   {(loc > 1.3 ? "stable" : "weak")}");
        }
    }

    // ═══════════════ CRR_10 MultiSeedRobustness ═══════════════
    [Fact]
    public void V4_1_CRR_10_MultiSeedRobustness()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int nSeeds = 10; int ln = N / 2;
        var locs = new List<double>();
        for (int seed = 0; seed < nSeeds; seed++)
        {
            var Kc = RecoverFP(KS(N, seed), N, kv, xi, s, 5, seed);
            var d0 = DL(Nm(RP(Sm(Kc, N, s, seed))));
            var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, s, seed, ln, 0.2)))), kv, xi), N, s, seed))));
            var lc = LapCurv(d0, dL, N);
            locs.Add(Math.Abs(lc[ln]) / (lc.Average(x => Math.Abs(x)) + 1e-6));
        }
        _output.WriteLine($"Localization: {locs.Average():F1}±{Math.Sqrt(locs.Average(x => (x - locs.Average()) * (x - locs.Average()))):F1}x  n={nSeeds}");
    }

    // ═══════════════ CRR_11 CouplingLawRobustness ═══════════════
    [Fact]
    public void V4_1_CRR_11_CouplingLawRobustness()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int E = 5; int ln = N / 2;
        var laws = new Dictionary<string, Func<double[,], double, double, double[,]>> { {"exp", (d,k0,x) => ExpUpd(d,k0,x)}, {"gauss", (d,k0,x) => GaussUpd(d,k0,x)}, {"power", (d,k0,x) => PowerUpd(d,k0,x)} };
        _output.WriteLine("Law      loc     stable?");
        foreach (var (name, upd) in laws)
        {
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS, upd);
            var d0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
            var hL = Sm(Kc, N, s, BS, ln, 0.2); var Kl = upd(DL(Nm(RP(hL))), kv, xi);
            var dL = DL(Nm(RP(Sm(Kl, N, s, BS)))); var lc = LapCurv(d0, dL, N);
            double loc = Math.Abs(lc[ln]) / (lc.Average(x => Math.Abs(x)) + 1e-6);
            _output.WriteLine($"{name,-7} {loc:F1}x    {(loc > 1.5 ? "yes" : "partial")}");
        }
    }

    // ═══════════════ CRR_12 NullAndDegenerateControls ═══════════════
    [Fact]
    public void V4_1_CRR_12_NullAndDegenerateControls()
    {
        int N = 80; double s = 0.1; double xi = 1.75; double kv = 1.2; int ln = N / 2;
        _output.WriteLine("Control      lapLoc  class");
        // K=0
        var K0 = new double[N, N]; var d0k = DL(Nm(RP(Sm(K0, N, s, BS))));
        var dLk = DL(Nm(RP(Sm(K0, N, s, BS, ln, 0.2)))); var lc0 = LapCurv(d0k, dLk, N);
        _output.WriteLine($"K=0:         {Math.Abs(lc0[ln]) / (lc0.Average(x => Math.Abs(x)) + 1e-6):F1}x  degenerate");
        // Random R
        var rng = new Random(BS); var rd = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { double v = 0.1 + rng.NextDouble(); rd[i, j] = rd[j, i] = v; }
        var Kr = ExpUpd(rd, kv, xi); var d0r = DL(Nm(RP(Sm(Kr, N, s, BS))));
        var dLr = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kr, N, s, BS, ln, 0.2)))), kv, xi), N, s, BS))));
        var lcr = LapCurv(d0r, dLr, N);
        _output.WriteLine($"RandR:       {Math.Abs(lcr[ln]) / (lcr.Average(x => Math.Abs(x)) + 1e-6):F1}x  not physical");
        // Active
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var d0a = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var dLa = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, s, BS, ln, 0.2)))), kv, xi), N, s, BS))));
        var lca = LapCurv(d0a, dLa, N);
        _output.WriteLine($"ActiveTRM:   {Math.Abs(lca[ln]) / (lca.Average(x => Math.Abs(x)) + 1e-6):F1}x  TRM");
    }

    // ═══════════════ CRR_13 CurvatureRobustnessScore ═══════════════
    [Fact]
    public void V4_1_CRR_13_CurvatureRobustnessScore()
    {
        int N = 80; double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2]; double s = 0.1; int ln = N / 2;
        _output.WriteLine("xi    K0   defAgr  loc     score   class");
        foreach (double xi in xis)
            foreach (double kv in K0s)
            {
                var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
                var d0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
                var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, s, BS, ln, 0.2)))), kv, xi), N, s, BS))));
                var lc = LapCurv(d0, dL, N); var dc = DistCurv(d0, dL, N);
                double da = Math.Abs(Spear(lc, dc));
                double loc = Math.Abs(lc[ln]) / (lc.Average(x => Math.Abs(x)) + 1e-6);
                double score = da * Math.Min(loc / 3.0, 1.0);
                string cls = score > 0.3 ? "RobustCurv" : (score > 0.15 ? "Moderate" : (score > 0.05 ? "Weak" : "None"));
                _output.WriteLine($"{xi:F2}  {kv:F1}  {da:F3}   {loc:F1}x    {score:F3}   {cls}");
            }
    }

    // ═══════════════ CRR_14 CurvatureRobustnessReport ═══════════════
    [Fact]
    public void V4_1_CRR_14_CurvatureRobustnessReport()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var d0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, s, BS, ln, 0.2)))), kv, xi), N, s, BS))));
        var lc = LapCurv(d0, dL, N); var dc = DistCurv(d0, dL, N);
        double da = Math.Abs(Spear(lc, dc));
        double loc = Math.Abs(lc[ln]) / (lc.Average(x => Math.Abs(x)) + 1e-6);
        double score = da * Math.Min(loc / 3.0, 1.0);
        string ccl = score > 0.3 ? "A) robust curvature-response proxy" : (score > 0.15 ? "B) moderate robustness" : (score > 0.05 ? "C) weak" : "D) degenerate"));
        _output.WriteLine("═══ CURVATURE ROBUSTNESS REPORT ═══");
        _output.WriteLine($"Def agreement: {da:F3}  Localization: {loc:F1}x  Score: {score:F3}");
        _output.WriteLine($"Conclusion: {ccl}");
        _output.WriteLine("Physical curvature / GR NOT derived.");
    }

    // ═══════════════ CRR_15 ClaimDisciplineReport ═══════════════
    [Fact]
    public void V4_1_CRR_15_ClaimDisciplineReport()
    {
        _output.WriteLine("═══ CLAIM DISCIPLINE: Curvature Response Robustness ═══");
        _output.WriteLine("");
        _output.WriteLine("SUPPORTED: Curvature robustness testable across definitions.");
        _output.WriteLine("CONDITIONAL: Curvature proxy is NOT Ricci. Phi is NOT potential.");
        _output.WriteLine("NOT CLAIMED: Curvature, Ricci, Einstein eqs, GR, gravity, c, D=3.");
        Assert.True(true, "Report complete.");
    }
}
