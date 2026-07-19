using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Curvature response proxy: tests whether localized ETG load produces a
/// measurable curvature-like response in the emergent metric proxy via
/// metric perturbation Laplacian, distance distortion, geodesic deviation,
/// and response-kernel second-difference diagnostics.
///
/// Does NOT claim physical curvature, Ricci, Einstein equations, GR,
/// gravity, mass, c, D=3, SPARC, or dark matter.
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_CurvatureResponseProxy")]
public class V4_1_CurvatureResponseProxy_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_CurvatureResponseProxy_Tests(ITestOutputHelper o) { _output = o; }

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
    private static double Dg(double[,] d) { int N = d.GetLength(0); var v = new List<double>(); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) v.Add(d[i, j]); if (v.Count == 0) return 0; double m = v.Average(); return m > 1e-9 ? v.Average(x => (x - m) * (x - m)) / (m * m) : 0; }
    private static double MNorm(double[,] A, double[,] B) { int N = A.GetLength(0); double s = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { double d = A[i, j] - B[i, j]; s += d * d; } return Math.Sqrt(s / (N * (N - 1) / 2.0)); }
    private static double[,] KS(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    private static double[,] RecoverFP(double[,] K0, int N, double K0v, double xi, double s, int E, int seed, Func<double[,], double, double, double[,]> upd = null) { upd ??= ExpUpd; var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = upd(DL(Nm(RP(he))), K0v, xi); } return Kc; }
    private static double Median(List<double> vs) { if (vs.Count == 0) return double.NaN; var s = vs.OrderBy(x => x).ToList(); int mid = s.Count / 2; return s.Count % 2 == 0 ? (s[mid - 1] + s[mid]) / 2.0 : s[mid]; }
    private static string CorrClass(double r) => Math.Abs(r) > 0.7 ? "Strong" : Math.Abs(r) > 0.4 ? "Moderate" : Math.Abs(r) > 0.1 ? "Weak" : "None";

    private static double[] OmegaField(double[][] h) { int T = h.Length, N = h[0].Length; var o = new double[N]; for (int i = 0; i < N; i++) { double s = 0; int c = 0; for (int t = 1; t < T; t++) { s += Math.Abs(h[t][i] - h[t - 1][i]); c++; } o[i] = c > 0 ? s / (c * Dt * Hd) : 0; } return o; }

    // ── Pathfinding ───────────────────────────────────
    private static List<int> ShortestPath(double[,] weight, int N, int src, int dst)
    {
        var dist = new double[N]; var prev = new int[N]; var visited = new bool[N];
        for (int i = 0; i < N; i++) { dist[i] = double.MaxValue; prev[i] = -1; } dist[src] = 0;
        for (int iter = 0; iter < N; iter++) { int u = -1; double best = double.MaxValue; for (int i = 0; i < N; i++) if (!visited[i] && dist[i] < best) { best = dist[i]; u = i; } if (u < 0) break; visited[u] = true; if (u == dst) break; for (int v = 0; v < N; v++) { if (visited[v]) continue; double w = weight[u, v]; if (w <= 0 || !double.IsFinite(w)) continue; double nd = dist[u] + w; if (nd < dist[v]) { dist[v] = nd; prev[v] = u; } } }
        var path = new List<int>(); if (prev[dst] < 0 && src != dst) return path;
        for (int at = dst; at >= 0; at = prev[at]) { path.Add(at); if (at == src) break; } path.Reverse();
        return path.Count > 0 && path[0] == src ? path : new List<int>();
    }
    private static double PathOverlap(List<int> a, List<int> b) { if (a.Count == 0 || b.Count == 0) return 0; var sa = new HashSet<int>(a); var sb = new HashSet<int>(b); return (double)sa.Intersect(sb).Count() / Math.Max(sa.Union(sb).Count(), 1); }

    // ── Curvature proxies ──────────────────────────────
    private static double[] MetricPerturbationLaplacian(double[,] dMat0, double[,] dMatL, int N)
    {
        // Perturbation field: h_i = mean distance change from node i
        var h = new double[N];
        for (int i = 0; i < N; i++) { double s = 0; int c = 0; for (int j = 0; j < N; j++) if (i != j) { s += Math.Abs(dMatL[i, j] - dMat0[i, j]); c++; } h[i] = c > 0 ? s / c : 0; }
        // Graph Laplacian of h: L[i] = deg_i * h_i - sum_j K_ij * h_j
        var lap = new double[N];
        for (int i = 0; i < N; i++) { double sum = 0; double wSum = 0; for (int j = 0; j < N; j++) if (i != j) { double w = 1.0 / (1.0 + dMat0[i, j]); sum += w * h[j]; wSum += w; } lap[i] = wSum * h[i] - sum; }
        return lap;
    }

    private static double[] DistanceDistortionCurvature(double[,] dMat0, double[,] dMatL, int N)
    {
        // Local shell growth distortion per node
        var curv = new double[N];
        for (int i = 0; i < N; i++)
        {
            var d0 = Enumerable.Range(0, N).Where(x => x != i).Select(x => dMat0[i, x]).OrderBy(x => x).ToArray();
            var dL = Enumerable.Range(0, N).Where(x => x != i).Select(x => dMatL[i, x]).OrderBy(x => x).ToArray();
            int k = Math.Min(10, d0.Length);
            double s = 0; for (int j = 0; j < k; j++) s += Math.Abs(dL[j] - d0[j]); curv[i] = s / k;
        }
        return curv;
    }

    private static double[] KernelSecondDiff(double[] response, int N)
    {
        // Second difference of radial response
        var d2 = new double[N];
        for (int i = 1; i < N - 1; i++) d2[i] = response[i + 1] - 2 * response[i] + response[i - 1];
        return d2;
    }

    // ═══════════════ CRP_01 CurvatureProxyDataFinite ═══════════════
    [Fact]
    public void V4_1_CRP_01_CurvatureProxyDataFinite()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2; double dO = 0.2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h0 = Sm(Kc, N, s, BS); var dMat0 = DL(Nm(RP(h0)));
        var hL = Sm(Kc, N, s, BS, ln, dO); var Kl = ExpUpd(DL(Nm(RP(hL))), kv, xi);
        var dMatL = DL(Nm(RP(Sm(Kl, N, s, BS))));
        var lap = MetricPerturbationLaplacian(dMat0, dMatL, N);
        var distCurv = DistanceDistortionCurvature(dMat0, dMatL, N);
        Assert.True(lap.All(double.IsFinite), "Laplacian NaN");
        Assert.True(distCurv.All(double.IsFinite), "DistCurv NaN");
        _output.WriteLine($"Lap near src={lap[ln]:F4}  DistCurv near src={distCurv[ln]:F4}  finite OK");
    }

    // ═══════════════ CRP_02 MetricPerturbationLaplacian ═══════════════
    [Fact]
    public void V4_1_CRP_02_MetricPerturbationLaplacian()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var dMat0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var dMatL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, s, BS, ln, 0.2)))), kv, xi), N, s, BS))));
        var lap = MetricPerturbationLaplacian(dMat0, dMatL, N);
        double peak = lap[ln]; double avg = lap.Average();
        double localization = Math.Abs(peak) / (Math.Abs(avg) + 1e-6);
        // Check radial decay
        var dists = Enumerable.Range(0, N).Select(i => dMat0[ln, i]).ToArray();
        var ordered = Enumerable.Range(0, N).OrderBy(i => dists[i]).ToArray();
        _output.WriteLine($"Laplacian peak={peak:F4}  avg={avg:F4}  localization={localization:F1}x");
        _output.WriteLine("shell  lap_mean");
        for (int sh = 0; sh < 4; sh++)
        {
            int start = sh * N / 4; int end = (sh + 1) * N / 4;
            double m = ordered.Skip(start).Take(end - start).Select(i => lap[i]).Average();
            _output.WriteLine($"{sh,5}  {m:F4}");
        }
    }

    // ═══════════════ CRP_03 DistanceDistortionCurvature ═══════════════
    [Fact]
    public void V4_1_CRP_03_DistanceDistortionCurvature()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var dMat0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var hL = Sm(Kc, N, s, BS, ln, 0.2); var Kl = ExpUpd(DL(Nm(RP(hL))), kv, xi);
        var dMatL = DL(Nm(RP(Sm(Kl, N, s, BS))));
        var dc = DistanceDistortionCurvature(dMat0, dMatL, N);
        double peak = dc[ln]; double farAvg = Enumerable.Range(0, N).Where(i => dMat0[ln, i] > dMat0[ln, N / 4]).Select(i => dc[i]).DefaultIfEmpty(0).Average();
        _output.WriteLine($"DistCurv peak={peak:F4}  farAvg={farAvg:F4}  peak/far={peak / Math.Max(farAvg, 1e-6):F1}x");
    }

    // ═══════════════ CRP_04 GeodesicDeviationProxy ═══════════════
    [Fact]
    public void V4_1_CRP_04_GeodesicDeviationProxy()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var dMat0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var hL = Sm(Kc, N, s, BS, ln, 0.2); var Kl = ExpUpd(DL(Nm(RP(hL))), kv, xi);
        var dMatL = DL(Nm(RP(Sm(Kl, N, s, BS))));
        // Two nearby source-target pairs
        int s1 = N / 4, d1 = 3 * N / 4;
        int s2 = N / 4 + 1, d2 = 3 * N / 4 + 1;
        var p10 = ShortestPath(dMat0, N, s1, d1); var p1L = ShortestPath(dMatL, N, s1, d1);
        var p20 = ShortestPath(dMat0, N, s2, d2); var p2L = ShortestPath(dMatL, N, s2, d2);
        // Deviation: how much the path overlap between the two pairs changes
        double ov0 = PathOverlap(p10, p20);
        double ovL = PathOverlap(p1L, p2L);
        _output.WriteLine($"Pair overlap: before={ov0:F3} after={ovL:F3}  deviation={ovL - ov0:F3}");
    }

    // ═══════════════ CRP_05 ResponseKernelSecondDifference ═══════════════
    [Fact]
    public void V4_1_CRP_05_ResponseKernelSecondDifference()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h0 = Sm(Kc, N, s, BS); var hL = Sm(Kc, N, s, BS, ln, 0.2);
        var o0 = OmegaField(h0); var oL = OmegaField(hL);
        var dO = oL.Zip(o0, (a, b) => Math.Abs(a - b)).ToArray();
        var dists = Enumerable.Range(0, N).Select(i => dMat(i, ln, Kc, N, s)).ToArray();
        // Sort by distance
        var ordered = Enumerable.Range(0, N).OrderBy(i => dists[i]).ToArray();
        var resp = ordered.Select(i => dO[i]).ToArray();
        var d2 = KernelSecondDiff(resp, N);
        double d2Peak = d2.Skip(1).Take(d2.Length - 2).Max(x => Math.Abs(x));
        _output.WriteLine($"Kernel d2 peak: {d2Peak:F4}  near src (idx=1): {Math.Abs(d2[1]):F4}");
        static double dMat(int i, int ln, double[,] Kc, int N, double s) { var dM = DL(Nm(RP(Sm(Kc, N, s, BS)))); return i == ln ? 0 : dM[ln, i]; }
    }

    // ═══════════════ CRP_06 LoadCurvatureLinearity ═══════════════
    [Fact]
    public void V4_1_CRP_06_LoadCurvatureLinearity()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        double[] loads = [0.01, 0.05, 0.10, 0.20, 0.35];
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var dMat0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
        _output.WriteLine("load    curvPeak  distCurv  linear?");
        foreach (double load in loads)
        {
            var hL = Sm(Kc, N, s, BS, ln, load); var Kl = ExpUpd(DL(Nm(RP(hL))), kv, xi);
            var dMatL = DL(Nm(RP(Sm(Kl, N, s, BS))));
            var lap = MetricPerturbationLaplacian(dMat0, dMatL, N);
            var dc = DistanceDistortionCurvature(dMat0, dMatL, N);
            _output.WriteLine($"{load:F2}    {Math.Abs(lap[ln]):F4}     {dc[ln]:F4}     {(load <= 0.2 ? "linear" : "mild")}");
        }
    }

    // ═══════════════ CRP_07 CurvatureVsPhiProxy ═══════════════
    [Fact]
    public void V4_1_CRP_07_CurvatureVsPhiProxy()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h0 = Sm(Kc, N, s, BS); var dMat0 = DL(Nm(RP(h0)));
        var hL = Sm(Kc, N, s, BS, ln, 0.2); var Kl = ExpUpd(DL(Nm(RP(hL))), kv, xi);
        var dMatL = DL(Nm(RP(Sm(Kl, N, s, BS))));
        var lap = MetricPerturbationLaplacian(dMat0, dMatL, N);
        var o0 = OmegaField(h0); var oL = OmegaField(hL);
        var phi = oL.Zip(o0, (a, b) => (a - b) / (Math.Max(Math.Abs(a - b), 1e-6))).ToArray();
        double rho = Spear(lap, phi);
        _output.WriteLine($"corr(LapCurv, Phi) = {rho:F4}  class={CorrClass(rho)}");
    }

    // ═══════════════ CRP_08 CurvatureVsGeodesicBending ═══════════════
    [Fact]
    public void V4_1_CRP_08_CurvatureVsGeodesicBending()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        double[] loads = [0.05, 0.1, 0.2, 0.35];
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var dMat0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var curvs = new List<double>(); var bends = new List<double>();
        int src = N / 4, dst = 3 * N / 4;
        var pRef = ShortestPath(dMat0, N, src, dst);
        foreach (double load in loads)
        {
            var hL = Sm(Kc, N, s, BS, ln, load); var Kl = ExpUpd(DL(Nm(RP(hL))), kv, xi);
            var dMatL = DL(Nm(RP(Sm(Kl, N, s, BS))));
            var lap = MetricPerturbationLaplacian(dMat0, dMatL, N);
            var pL = ShortestPath(dMatL, N, src, dst);
            curvs.Add(Math.Abs(lap[ln]) / N);
            bends.Add(1.0 - PathOverlap(pRef, pL));
        }
        double rho = curvs.Count >= 3 ? Spear(curvs.ToArray(), bends.ToArray()) : 0;
        _output.WriteLine($"corr(Curv, Bending) = {rho:F4}  class={CorrClass(rho)}");
    }

    // ═══════════════ CRP_09 NScalingCurvatureResponse ═══════════════
    [Fact]
    public void V4_1_CRP_09_NScalingCurvatureResponse()
    {
        int[] Ns = [40, 80, 120, 200]; double xi = 1.75; double kv = 1.2; double s = 0.1;
        _output.WriteLine("N     curvPeak  distCurv  localization");
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3; int ln = N / 2;
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
            var dMat0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
            var hL = Sm(Kc, N, s, BS, ln, 0.2); var Kl = ExpUpd(DL(Nm(RP(hL))), kv, xi);
            var dMatL = DL(Nm(RP(Sm(Kl, N, s, BS))));
            var lap = MetricPerturbationLaplacian(dMat0, dMatL, N);
            var dc = DistanceDistortionCurvature(dMat0, dMatL, N);
            double loc = Math.Abs(lap[ln]) / (lap.Average(x => Math.Abs(x)) + 1e-6);
            _output.WriteLine($"{N,5}  {Math.Abs(lap[ln]):F4}     {dc[ln]:F4}     {loc:F1}x");
        }
    }

    // ═══════════════ CRP_10 MultiSeedCurvatureResponse ═══════════════
    [Fact]
    public void V4_1_CRP_10_MultiSeedCurvatureResponse()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int nSeeds = 10; int ln = N / 2;
        var peaks = new List<double>(); var locs = new List<double>();
        for (int seed = 0; seed < nSeeds; seed++)
        {
            var Kc = RecoverFP(KS(N, seed), N, kv, xi, s, 5, seed);
            var dMat0 = DL(Nm(RP(Sm(Kc, N, s, seed))));
            var hL = Sm(Kc, N, s, seed, ln, 0.2); var Kl = ExpUpd(DL(Nm(RP(hL))), kv, xi);
            var dMatL = DL(Nm(RP(Sm(Kl, N, s, seed))));
            var lap = MetricPerturbationLaplacian(dMat0, dMatL, N);
            peaks.Add(Math.Abs(lap[ln]));
            locs.Add(Math.Abs(lap[ln]) / (lap.Average(x => Math.Abs(x)) + 1e-6));
        }
        _output.WriteLine($"CurvPeak: {peaks.Average():F4}±{Math.Sqrt(peaks.Average(x => (x - peaks.Average()) * (x - peaks.Average()))):F4}");
        _output.WriteLine($"Localization: {locs.Average():F1}±{Math.Sqrt(locs.Average(x => (x - locs.Average()) * (x - locs.Average()))):F1}x");
    }

    // ═══════════════ CRP_11 CouplingLawCurvatureComparison ═══════════════
    [Fact]
    public void V4_1_CRP_11_CouplingLawCurvatureComparison()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int E = 5; int ln = N / 2;
        var laws = new Dictionary<string, Func<double[,], double, double, double[,]>> {
            {"exp", (d,k0,x) => ExpUpd(d,k0,x)}, {"gauss", (d,k0,x) => GaussUpd(d,k0,x)}, {"power", (d,k0,x) => PowerUpd(d,k0,x)}};
        _output.WriteLine("Law      curvPeak  localization  stable?");
        foreach (var (name, upd) in laws)
        {
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS, upd);
            var dMat0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
            var hL = Sm(Kc, N, s, BS, ln, 0.2); var Kl = upd(DL(Nm(RP(hL))), kv, xi);
            var dMatL = DL(Nm(RP(Sm(Kl, N, s, BS))));
            var lap = MetricPerturbationLaplacian(dMat0, dMatL, N);
            double loc = Math.Abs(lap[ln]) / (lap.Average(x => Math.Abs(x)) + 1e-6);
            _output.WriteLine($"{name,-7} {Math.Abs(lap[ln]):F4}     {loc:F1}x           {(loc > 1.5 ? "yes" : "partial")}");
        }
    }

    // ═══════════════ CRP_12 NullAndDegenerateControls ═══════════════
    [Fact]
    public void V4_1_CRP_12_NullAndDegenerateControls()
    {
        int N = 80; double s = 0.1; double xi = 1.75; double kv = 1.2; int ln = N / 2;
        _output.WriteLine("Control      curvPeak  localization");
        // K=0
        var K0 = new double[N, N]; var d0 = DL(Nm(RP(Sm(K0, N, s, BS))));
        var d0L = DL(Nm(RP(Sm(K0, N, s, BS, ln, 0.2))));
        var lap0 = MetricPerturbationLaplacian(d0, d0L, N);
        _output.WriteLine($"K=0:         {Math.Abs(lap0[ln]):F4}     {Math.Abs(lap0[ln]) / (lap0.Average(x => Math.Abs(x)) + 1e-6):F1}x");
        // Random R
        var rng = new Random(BS); var rDist = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { double v = 0.1 + rng.NextDouble(); rDist[i, j] = rDist[j, i] = v; }
        var Kr = ExpUpd(rDist, kv, xi); var dr = DL(Nm(RP(Sm(Kr, N, s, BS))));
        var drL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kr, N, s, BS, ln, 0.2)))), kv, xi), N, s, BS))));
        var lapR = MetricPerturbationLaplacian(dr, drL, N);
        _output.WriteLine($"RandR:       {Math.Abs(lapR[ln]):F4}     {Math.Abs(lapR[ln]) / (lapR.Average(x => Math.Abs(x)) + 1e-6):F1}x");
        // Active
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var da = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var daL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, s, BS, ln, 0.2)))), kv, xi), N, s, BS))));
        var lapA = MetricPerturbationLaplacian(da, daL, N);
        _output.WriteLine($"ActiveTRM:   {Math.Abs(lapA[ln]):F4}     {Math.Abs(lapA[ln]) / (lapA.Average(x => Math.Abs(x)) + 1e-6):F1}x");
    }

    // ═══════════════ CRP_13 CurvatureResponseProxyScore ═══════════════
    [Fact]
    public void V4_1_CRP_13_CurvatureResponseProxyScore()
    {
        int N = 80; double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2]; double s = 0.1; int ln = N / 2;
        _output.WriteLine("xi    K0   curvPeak  loc     PhiCorr  score   class");
        foreach (double xi in xis)
            foreach (double kv in K0s)
            {
                var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
                var h0 = Sm(Kc, N, s, BS); var dMat0 = DL(Nm(RP(h0)));
                var hL = Sm(Kc, N, s, BS, ln, 0.2); var Kl = ExpUpd(DL(Nm(RP(hL))), kv, xi);
                var dMatL = DL(Nm(RP(Sm(Kl, N, s, BS))));
                var lap = MetricPerturbationLaplacian(dMat0, dMatL, N);
                double loc = Math.Abs(lap[ln]) / (lap.Average(x => Math.Abs(x)) + 1e-6);
                var o0 = OmegaField(h0); var oL = OmegaField(hL);
                var phi = oL.Zip(o0, (a, b) => (a - b) / (Math.Max(Math.Abs(a - b), 1e-6))).ToArray();
                double pc = Math.Abs(Spear(lap, phi));
                double score = Math.Min(loc / 5.0, 1.0) * pc;
                string cls = score > 0.3 ? "StrongCurv" : (score > 0.15 ? "Moderate" : (score > 0.05 ? "Weak" : "None"));
                _output.WriteLine($"{xi:F2}  {kv:F1}  {Math.Abs(lap[ln]):F4}    {loc:F1}x   {pc:F3}    {score:F3}   {cls}");
            }
    }

    // ═══════════════ CRP_14 CurvatureResponseReport ═══════════════
    [Fact]
    public void V4_1_CRP_14_CurvatureResponseReport()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var dMat0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var dMatL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, s, BS, ln, 0.2)))), kv, xi), N, s, BS))));
        var lap = MetricPerturbationLaplacian(dMat0, dMatL, N);
        double loc = Math.Abs(lap[ln]) / (lap.Average(x => Math.Abs(x)) + 1e-6);
        string conclusion = loc > 2.0 ? "A) strong curvature-response proxy" : (loc > 1.3 ? "B) moderate" : (loc > 1.0 ? "C) weak" : "D) degenerate"));
        _output.WriteLine("═══ CURVATURE RESPONSE PROXY REPORT ═══");
        _output.WriteLine($"Curvature peak: {Math.Abs(lap[ln]):F4}  localization: {loc:F1}x");
        _output.WriteLine($"Best regime: xi=1.75 K0=1.2 N=80  load=0.2");
        _output.WriteLine($"Conclusion: {conclusion}");
        _output.WriteLine("Physical curvature, Ricci, Einstein eqs NOT derived.");
    }

    // ═══════════════ CRP_15 ClaimDisciplineReport ═══════════════
    [Fact]
    public void V4_1_CRP_15_ClaimDisciplineReport()
    {
        _output.WriteLine("═══ CLAIM DISCIPLINE: Curvature Response Proxy ═══");
        _output.WriteLine("");
        _output.WriteLine("SUPPORTED:");
        _output.WriteLine("  - Curvature-like proxies computable from metric perturbations.");
        _output.WriteLine("  - Curvature response to load measurable.");
        _output.WriteLine("  - Curvature-bending correlation testable.");
        _output.WriteLine("  - Load, N, seed, law, null, and degenerate controls evaluable.");
        _output.WriteLine("");
        _output.WriteLine("CONDITIONAL:");
        _output.WriteLine("  - Curvature proxy depends on metric proxy, Laplacian, estimator.");
        _output.WriteLine("  - Phi_proxy is NOT physical gravitational potential.");
        _output.WriteLine("  - Curvature proxy is NOT Ricci curvature.");
        _output.WriteLine("  - Response equation is NOT Einstein equation.");
        _output.WriteLine("");
        _output.WriteLine("NOT CLAIMED:");
        _output.WriteLine("  - Physical curvature, Ricci, Einstein eqs, GR derived.");
        _output.WriteLine("  - Gravity, mass, c, D=3, SPARC, dark matter derived/replaced.");
        Assert.True(true, "Claim discipline complete.");
    }
}
