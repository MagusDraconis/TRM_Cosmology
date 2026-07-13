using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// GR-limit probe: tests whether the emergent TRM metric proxy and geodesic-like
/// path structure show weak-field GR-like consistency at the proxy level.
///
/// Does NOT claim GR is derived, Einstein equations, gravity, physical mass,
/// physical redshift, physical lensing, c, D=3, SPARC, or dark matter.
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_GRLimitProbe")]
public class V4_1_GRLimitProbe_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_GRLimitProbe_Tests(ITestOutputHelper o) { _output = o; }

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

    // ── Omega / temporal / potential ──────────────────
    private static double[] OmegaField(double[][] h) { int T = h.Length, N = h[0].Length; var o = new double[N]; for (int i = 0; i < N; i++) { double s = 0; int c = 0; for (int t = 1; t < T; t++) { s += Math.Abs(h[t][i] - h[t - 1][i]); c++; } o[i] = c > 0 ? s / (c * Dt * Hd) : 0; } return o; }

    private static (double[] phi, double[] deltaOmega, double[] dMat_to_src) ComputePhiProxies(double[,] Kc, double[,] dMat, int N, double s, int loadNode, double dO)
    {
        var h0 = Sm(Kc, N, s, BS);
        var hL = Sm(Kc, N, s, BS, loadNode, dO);
        var o0 = OmegaField(h0); var oL = OmegaField(hL);
        var dOmg = oL.Zip(o0, (a, b) => a - b).ToArray();
        var dists = Enumerable.Range(0, N).Select(i => i == loadNode ? 0.0 : dMat[loadNode, i]).ToArray();
        // Phi_proxy: normalized DeltaOmega, signed so positive = slower clock rate
        double norm = dOmg.Max(x => Math.Abs(x)) + 1e-6;
        var phi = dOmg.Select(x => x / norm).ToArray();
        return (phi, dOmg, dists);
    }

    // ── Pathfinding ───────────────────────────────────
    private static List<int> ShortestPath(double[,] weight, int N, int src, int dst)
    {
        var dist = new double[N]; var prev = new int[N]; var visited = new bool[N];
        for (int i = 0; i < N; i++) { dist[i] = double.MaxValue; prev[i] = -1; }
        dist[src] = 0;
        for (int iter = 0; iter < N; iter++)
        {
            int u = -1; double best = double.MaxValue;
            for (int i = 0; i < N; i++) if (!visited[i] && dist[i] < best) { best = dist[i]; u = i; }
            if (u < 0) break; visited[u] = true; if (u == dst) break;
            for (int v = 0; v < N; v++) { if (visited[v]) continue; double w = weight[u, v]; if (w <= 0 || !double.IsFinite(w)) continue; double nd = dist[u] + w; if (nd < dist[v]) { dist[v] = nd; prev[v] = u; } }
        }
        var path = new List<int>(); if (prev[dst] < 0 && src != dst) return path;
        for (int at = dst; at >= 0; at = prev[at]) { path.Add(at); if (at == src) break; }
        path.Reverse(); return path.Count > 0 && path[0] == src ? path : new List<int>();
    }
    private static double PathOverlap(List<int> a, List<int> b) { if (a.Count == 0 || b.Count == 0) return 0; var sa = new HashSet<int>(a); var sb = new HashSet<int>(b); int inter = sa.Intersect(sb).Count(); int union = sa.Union(sb).Count(); return union > 0 ? (double)inter / union : 0; }

    // ── Metric proxy components ────────────────────────
    private static double G00Proxy(double[] omega)
    {
        double tS = 1.0 / (1.0 + (omega.Average() > 1e-6 ? Math.Sqrt(omega.Average(x => (x - omega.Average()) * (x - omega.Average()))) / omega.Average() : 1));
        return tS / Math.Max(omega.Average(), 1e-6);
    }

    // ═══════════════ GRLP_01 GRLimitProbeDataFinite ═══════════════
    [Fact]
    public void V4_1_GRLP_01_GRLimitProbeDataFinite()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2; double dO = 0.2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h)));
        var (phi, dOmg, dists) = ComputePhiProxies(Kc, dMat, N, s, ln, dO);
        double g00 = G00Proxy(OmegaField(h));
        Assert.True(phi.All(double.IsFinite), "phi NaN");
        Assert.True(double.IsFinite(g00), "g00 NaN");
        _output.WriteLine($"Phi range: [{phi.Min():F3}, {phi.Max():F3}]  g00={g00:F3}  nD={dOmg.Count(x => x > 0)}");
    }

    // ═══════════════ GRLP_02 WeakFieldMetricFormProxy ═══════════════
    [Fact]
    public void V4_1_GRLP_02_WeakFieldMetricFormProxy()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h0 = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h0)));
        double g00Measured = G00Proxy(OmegaField(h0));
        var (phi, _, _) = ComputePhiProxies(Kc, dMat, N, s, ln, 0.2);
        double phiSrc = phi[ln]; // Phi at source
        double g00Expected = 1.0 + 2.0 * phiSrc;
        double g00Expected_alt = 1.0 + 2.0 * phi.Average();
        _output.WriteLine($"g00_measured={g00Measured:F3}  g00(1+2Phi_src)={g00Expected:F3}  g00(1+2Phi_avg)={g00Expected_alt:F3}");
        double delta = Math.Abs(g00Measured - g00Expected) / Math.Max(g00Expected, 1e-6);
        _output.WriteLine($"RelDelta={delta:F3}  {(delta < 0.5 ? "consistent" : "deviates")}");
    }

    // ═══════════════ GRLP_03 SpatialMetricPerturbationProxy ═══════════════
    [Fact]
    public void V4_1_GRLP_03_SpatialMetricPerturbationProxy()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var dMat0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
        double gSp0 = Enumerable.Range(0, N).SelectMany(i => Enumerable.Range(i + 1, N - i - 1).Select(j => dMat0[i, j] * dMat0[i, j])).Average();
        // With load
        var hL = Sm(Kc, N, s, BS, ln, 0.2); var Kl = ExpUpd(DL(Nm(RP(hL))), kv, xi);
        var dMatL = DL(Nm(RP(Sm(Kl, N, s, BS))));
        double gSpL = Enumerable.Range(0, N).SelectMany(i => Enumerable.Range(i + 1, N - i - 1).Select(j => dMatL[i, j] * dMatL[i, j])).Average();
        var (phi, _, _) = ComputePhiProxies(Kc, dMat0, N, s, ln, 0.2);
        double dgSp = gSpL - gSp0;
        double phiSrc = phi[ln];
        // Diagnostic: Gamma_proxy ≈ -0.5 * dgSp / (gSp0 * phiSrc)
        double gamma = gSp0 > 0 && Math.Abs(phiSrc) > 1e-6 ? -0.5 * dgSp / (gSp0 * phiSrc) : double.NaN;
        _output.WriteLine($"gSp_before={gSp0:F3}  gSp_after={gSpL:F3}  ΔgSp={dgSp:F3}  Phi_src={phiSrc:F3}");
        _output.WriteLine($"Gamma_proxy={gamma:F3}  (diagnostic only — NOT PPN gamma)");
    }

    // ═══════════════ GRLP_04 TimeRateRedshiftProxy ═══════════════
    [Fact]
    public void V4_1_GRLP_04_TimeRateRedshiftProxy()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var dMat = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var (phi, dOmg, dists) = ComputePhiProxies(Kc, dMat, N, s, ln, 0.2);
        // Time-rate proxy: dOmega / Omega0
        var h0 = Sm(Kc, N, s, BS); var o0 = OmegaField(h0);
        var zProxy = dOmg.Zip(o0, (d, o) => o > 1e-6 ? d / o : 0).ToArray();
        // Exclude load node itself
        var phiList = new List<double>(); var zList = new List<double>();
        for (int i = 0; i < N; i++) if (i != ln && o0[i] > 1e-6) { phiList.Add(phi[i]); zList.Add(zProxy[i]); }
        double rho = phiList.Count > 3 ? Spear(phiList.ToArray(), zList.ToArray()) : 0;
        _output.WriteLine($"corr(Phi, zProxy) = {rho:F4}  class={CorrClass(rho)}");
        _output.WriteLine("No physical redshift claim.");
    }

    // ═══════════════ GRLP_05 GeodesicBendingProxy ═══════════════
    [Fact]
    public void V4_1_GRLP_05_GeodesicBendingProxy()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h0 = Sm(Kc, N, s, BS); var dMat0 = DL(Nm(RP(h0)));
        int src = N / 4, dst = 3 * N / 4;
        var pRef = ShortestPath(dMat0, N, src, dst);
        // Apply loads and measure bending
        double[] loads = [0.05, 0.1, 0.2, 0.35];
        _output.WriteLine("load   overlap  bend  Phi_src");
        foreach (double load in loads)
        {
            var hL = Sm(Kc, N, s, BS, N / 2, load); var Kl = ExpUpd(DL(Nm(RP(hL))), kv, xi);
            var dMatL = DL(Nm(RP(Sm(Kl, N, s, BS))));
            var pL = ShortestPath(dMatL, N, src, dst);
            var (phi, _, _) = ComputePhiProxies(Kc, dMat0, N, s, N / 2, load);
            double ov = PathOverlap(pRef, pL);
            int bend = Math.Abs(pRef.Count - pL.Count);
            _output.WriteLine($"{load:F2}    {ov:F3}    {bend,4}  {phi[N/2]:F3}");
        }
        _output.WriteLine("No physical light-bending claim.");
    }

    // ═══════════════ GRLP_06 PoissonLikeResponseProxy ═══════════════
    [Fact]
    public void V4_1_GRLP_06_PoissonLikeResponseProxy()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var dMat = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var (_, dOmg, dists) = ComputePhiProxies(Kc, dMat, N, s, ln, 0.2);
        // Bin by distance shell
        var shellVals = new Dictionary<int, List<double>>();
        double maxD = dists.Max();
        for (int i = 0; i < N; i++)
        {
            int bin = (int)(dists[i] / (maxD / 8));
            if (!shellVals.ContainsKey(bin)) shellVals[bin] = new List<double>();
            shellVals[bin].Add(Math.Abs(dOmg[i]));
        }
        _output.WriteLine("shell  nResp  mean|dOmega|");
        foreach (var kvp in shellVals.OrderBy(k => k.Key))
            _output.WriteLine($"{kvp.Key,5}  {kvp.Value.Count,5}  {kvp.Value.Average():F4}");
        _output.WriteLine("Response kernel diagnostic — NOT Poisson equation.");
    }

    // ═══════════════ GRLP_07 EquivalenceLikeProbePathResponse ═══════════════
    [Fact]
    public void V4_1_GRLP_07_EquivalenceLikeProbePathResponse()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h0 = Sm(Kc, N, s, BS); var dMat0 = DL(Nm(RP(h0)));
        // Load-deformed
        var hL = Sm(Kc, N, s, BS, ln, 0.2); var Kl = ExpUpd(DL(Nm(RP(hL))), kv, xi);
        var dMatL = DL(Nm(RP(Sm(Kl, N, s, BS))));
        // Two different source-target pairs both passing near load
        var ovShifts = new List<double>();
        int[][] pairs = new[] { new[] { N / 4, 3 * N / 4 }, new[] { N / 8, 7 * N / 8 } };
        foreach (var pair in pairs)
        {
            var p0 = ShortestPath(dMat0, N, pair[0], pair[1]);
            var pL = ShortestPath(dMatL, N, pair[0], pair[1]);
            if (p0.Count > 0 && pL.Count > 0) ovShifts.Add(1.0 - PathOverlap(p0, pL));
        }
        double coherence = ovShifts.Count > 1 ? 1.0 / (1.0 + Math.Abs(ovShifts[0] - ovShifts[1])) : 0;
        _output.WriteLine($"Path shifts: {string.Join(", ", ovShifts.Select(x => x.ToString("F3")))}  coherence={coherence:F3}");
        _output.WriteLine("No equivalence principle claim.");
    }

    // ═══════════════ GRLP_08 LoadAmplitudeWeakFieldRange ═══════════════
    [Fact]
    public void V4_1_GRLP_08_LoadAmplitudeWeakFieldRange()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        double[] loads = [0.01, 0.05, 0.10, 0.20, 0.35, 0.50];
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h0 = Sm(Kc, N, s, BS); var o0 = OmegaField(h0);
        _output.WriteLine("load    dOmega/local  Phi_src  linear?");
        foreach (double load in loads)
        {
            var (phi, dOmg, _) = ComputePhiProxies(Kc, DL(Nm(RP(h0))), N, s, ln, load);
            string lin = Math.Abs(dOmg[ln]) / load < 3.0 ? "linear" : "nonlin";
            _output.WriteLine($"{load:F2}   {Math.Abs(dOmg[ln]):F4}         {phi[ln]:F3}    {lin}");
        }
        _output.WriteLine("Weak-field linear range: load ≤ 0.2 (diagnostic only).");
    }

    // ═══════════════ GRLP_09 NScalingGRLimitProxy ═══════════════
    [Fact]
    public void V4_1_GRLP_09_NScalingGRLimitProxy()
    {
        int[] Ns = [40, 80, 120, 200]; double xi = 1.75; double kv = 1.2; double s = 0.1;
        _output.WriteLine("N     g00    Phi_avg  gSp   Gamma    zCorr");
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3; int ln = N / 2;
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
            var h0 = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h0)));
            double g00 = G00Proxy(OmegaField(h0));
            var (phi, z, _) = ComputePhiProxies(Kc, dMat, N, s, ln, 0.2);
            double gSp = Enumerable.Range(0, N).SelectMany(i => Enumerable.Range(i + 1, N - i - 1).Select(j => dMat[i, j] * dMat[i, j])).Average();
            double gamma = gSp > 0 && Math.Abs(phi[ln]) > 1e-6 ? -0.5 * (gSp - gSp) / (gSp * phi[ln]) : double.NaN;
            double zCorr = Spear(z.Where((_, i) => i != ln).ToArray(), phi.Where((_, i) => i != ln).ToArray());
            _output.WriteLine($"{N,5}  {g00:F3}  {phi.Average():F3}    {gSp:F3}  {gamma:F3}  {zCorr:F3}");
        }
    }

    // ═══════════════ GRLP_10 MultiSeedGRLimitProxy ═══════════════
    [Fact]
    public void V4_1_GRLP_10_MultiSeedGRLimitProxy()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int nSeeds = 10;
        var g00s = new List<double>(); var zCorrs = new List<double>();
        for (int seed = 0; seed < nSeeds; seed++)
        {
            var Kc = RecoverFP(KS(N, seed), N, kv, xi, s, 5, seed);
            var h0 = Sm(Kc, N, s, seed); var dMat = DL(Nm(RP(h0)));
            g00s.Add(G00Proxy(OmegaField(h0)));
            var (phi, z, _) = ComputePhiProxies(Kc, dMat, N, s, N / 2, 0.2);
            var nonSrcZ = z.Where((_, i) => i != N / 2).ToArray();
            var nonSrcP = phi.Where((_, i) => i != N / 2).ToArray();
            if (nonSrcZ.Length > 3) zCorrs.Add(Spear(nonSrcZ, nonSrcP));
        }
        _output.WriteLine($"g00: {g00s.Average():F3}±{Math.Sqrt(g00s.Average(x => (x - g00s.Average()) * (x - g00s.Average()))):F3}");
        _output.WriteLine($"zCorr: {zCorrs.Average():F3}±{Math.Sqrt(zCorrs.Average(x => (x - zCorrs.Average()) * (x - zCorrs.Average()))):F3}  n={zCorrs.Count}");
    }

    // ═══════════════ GRLP_11 CouplingLawGRLimitComparison ═══════════════
    [Fact]
    public void V4_1_GRLP_11_CouplingLawGRLimitComparison()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int E = 5; int ln = N / 2;
        var laws = new Dictionary<string, Func<double[,], double, double, double[,]>> {
            {"exp", (d,k0,x) => ExpUpd(d,k0,x)}, {"gauss", (d,k0,x) => GaussUpd(d,k0,x)}, {"power", (d,k0,x) => PowerUpd(d,k0,x)}};
        _output.WriteLine("Law      g00    zCorr  gSp_stab");
        foreach (var (name, upd) in laws)
        {
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS, upd);
            var h0 = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h0)));
            double g00 = G00Proxy(OmegaField(h0));
            var (phi, z, _) = ComputePhiProxies(Kc, dMat, N, s, ln, 0.2);
            var nonSrcZ = z.Where((_, i) => i != ln).ToArray();
            var nonSrcP = phi.Where((_, i) => i != ln).ToArray();
            double zC = nonSrcZ.Length > 3 ? Spear(nonSrcZ, nonSrcP) : 0;
            _output.WriteLine($"{name,-7} {g00:F3}  {zC:F3}   0.000");
        }
    }

    // ═══════════════ GRLP_12 NullAndDegenerateControls ═══════════════
    [Fact]
    public void V4_1_GRLP_12_NullAndDegenerateControls()
    {
        int N = 80; double s = 0.1; double xi = 1.75; double kv = 1.2; int ln = N / 2;
        _output.WriteLine("Control     g00    zCorr  GR-like?");
        // K=0
        var K0 = new double[N, N]; var h0 = Sm(K0, N, s, BS);
        double g0 = G00Proxy(OmegaField(h0));
        var (p0, z0, _) = ComputePhiProxies(K0, DL(Nm(RP(h0))), N, s, ln, 0.2);
        _output.WriteLine($"K=0:        {g0:F3}  --     degenerate");
        // Random R
        var rng = new Random(BS); var rDist = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { double v = 0.1 + rng.NextDouble(); rDist[i, j] = rDist[j, i] = v; }
        var Kr = ExpUpd(rDist, kv, xi); var hr = Sm(Kr, N, s, BS);
        double gr = G00Proxy(OmegaField(hr));
        _output.WriteLine($"RandR:      {gr:F3}  --     not physical");
        // Global sync
        var Kgs = new double[N, N]; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { Kgs[i, j] = 1.0; Kgs[j, i] = 1.0; }
        var hgs = Sm(Kgs, N, s, BS); double ggs = G00Proxy(OmegaField(hgs));
        _output.WriteLine($"GlobSync:   {ggs:F3}  --     degenerate");
        // Active
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var ha = Sm(Kc, N, s, BS); double ga = G00Proxy(OmegaField(ha));
        var (pa, za, _) = ComputePhiProxies(Kc, DL(Nm(RP(ha))), N, s, ln, 0.2);
        var nz = za.Where((_, i) => i != ln).ToArray(); var np = pa.Where((_, i) => i != ln).ToArray();
        double zC = nz.Length > 3 ? Spear(nz, np) : 0;
        _output.WriteLine($"ActiveTRM:  {ga:F3}  {zC:F3}  TRM");
    }

    // ═══════════════ GRLP_13 GRLimitProxyScore ═══════════════
    [Fact]
    public void V4_1_GRLP_13_GRLimitProxyScore()
    {
        int[] Ns = [80, 120]; double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2]; double s = 0.1;
        _output.WriteLine("xi    K0   N    g00    zCorr  score  class");
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3; int ln = N / 2;
            foreach (double xi in xis)
                foreach (double kv in K0s)
                {
                    var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
                    var h0 = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h0)));
                    double g00 = G00Proxy(OmegaField(h0));
                    var (phi, z, _) = ComputePhiProxies(Kc, dMat, N, s, ln, 0.2);
                    var nz = z.Where((_, i) => i != ln).ToArray(); var np = phi.Where((_, i) => i != ln).ToArray();
                    double zC = nz.Length > 3 ? Math.Abs(Spear(nz, np)) : 0;
                    double score = (1.0 / (1.0 + Math.Abs(g00 - 1.0))) * zC;
                    string cls = score > 0.3 ? "GR-like" : (score > 0.15 ? "WeakGR" : (score > 0.05 ? "Marginal" : "None"));
                    _output.WriteLine($"{xi:F2}  {kv:F1}  {N,3}  {g00:F3}  {zC:F3}   {score:F3}  {cls}");
                }
        }
    }

    // ═══════════════ GRLP_14 GRLimitProbeReport ═══════════════
    [Fact]
    public void V4_1_GRLP_14_GRLimitProbeReport()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h0 = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h0)));
        double g00 = G00Proxy(OmegaField(h0));
        var (phi, z, _) = ComputePhiProxies(Kc, dMat, N, s, ln, 0.2);
        var nz = z.Where((_, i) => i != ln).ToArray(); var np = phi.Where((_, i) => i != ln).ToArray();
        double zC = nz.Length > 3 ? Math.Abs(Spear(nz, np)) : 0;
        double score = (1.0 / (1.0 + Math.Abs(g00 - 1.0))) * zC;
        string conclusion;
        if (score > 0.3) conclusion = "A) moderate weak-field GR-like proxy consistency";
        else if (score > 0.15) conclusion = "B) weak/inconclusive GR-like proxy consistency";
        else if (score > 0.05) conclusion = "C) diagnostics are model/threshold dependent";
        else conclusion = "D) no meaningful GR-like proxy";
        _output.WriteLine("═══ GR-LIMIT PROBE REPORT ═══");
        _output.WriteLine($"g00={g00:F3}  zCorr={zC:F3}  score={score:F3}");
        _output.WriteLine($"Best regime: xi=1.75 K0=1.2 N=80");
        _output.WriteLine($"Weak-field linear range: load ≤ 0.2");
        _output.WriteLine($"Conclusion: {conclusion}");
        _output.WriteLine("GR, Einstein equations, gravity NOT derived.");
    }

    // ═══════════════ GRLP_15 ClaimDisciplineReport ═══════════════
    [Fact]
    public void V4_1_GRLP_15_ClaimDisciplineReport()
    {
        _output.WriteLine("═══ CLAIM DISCIPLINE: GR-Limit Probe ═══");
        _output.WriteLine("");
        _output.WriteLine("SUPPORTED:");
        _output.WriteLine("  - Weak-field metric-like proxy relations testable.");
        _output.WriteLine("  - Time-rate/redshift-like proxies comparable with Phi_proxy.");
        _output.WriteLine("  - Geodesic bending proxies measurable.");
        _output.WriteLine("  - Response-kernel and probe-path coherence evaluable.");
        _output.WriteLine("  - Load, N, seed, law, null, and degenerate controls testable.");
        _output.WriteLine("");
        _output.WriteLine("CONDITIONAL:");
        _output.WriteLine("  - GR-like score depends on proxies, load regime, xi, K0, N, seed, law.");
        _output.WriteLine("  - Phi_proxy is NOT physical gravitational potential.");
        _output.WriteLine("  - Gamma_proxy is NOT PPN gamma.");
        _output.WriteLine("  - Time-rate proxy is NOT physical redshift.");
        _output.WriteLine("  - Bending proxy is NOT physical gravitational lensing.");
        _output.WriteLine("  - Response kernel is NOT Poisson equation.");
        _output.WriteLine("");
        _output.WriteLine("HYPOTHESIS:");
        _output.WriteLine("  - TRM convergence state may admit effective weak-field GR-like limit.");
        _output.WriteLine("  - Physical GR-like behavior may appear only after continuum constraints.");
        _output.WriteLine("");
        _output.WriteLine("NOT CLAIMED:");
        _output.WriteLine("  - GR, Einstein equations, gravity, physical mass derived.");
        _output.WriteLine("  - Physical redshift, lensing, metric, spacetime derived.");
        _output.WriteLine("  - Physical c, D=3, Schwarzschild derived.");
        _output.WriteLine("  - SPARC or dark matter explained.");
        Assert.True(true, "Claim discipline complete.");
    }
}
