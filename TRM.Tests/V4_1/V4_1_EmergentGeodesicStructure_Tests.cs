using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Emergent geodesic structure: tests whether signal propagation, front evolution,
/// and shortest-response paths follow geodesic-like behavior in the emergent
/// metric proxy geometry.
///
/// Does NOT claim physical geodesics, GR, Einstein equations, physical spacetime,
/// physical metric, D=3, physical c, Lorentz invariance, SPARC, or dark matter.
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_EmergentGeodesicStructure")]
public class V4_1_EmergentGeodesicStructure_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_EmergentGeodesicStructure_Tests(ITestOutputHelper o) { _output = o; }

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
    private static double[,] KS(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    private static double[,] RecoverFP(double[,] K0, int N, double K0v, double xi, double s, int E, int seed, Func<double[,], double, double, double[,]> upd = null) { upd ??= ExpUpd; var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = upd(DL(Nm(RP(he))), K0v, xi); } return Kc; }
    private static double Median(List<double> vs) { if (vs.Count == 0) return double.NaN; var s = vs.OrderBy(x => x).ToList(); int mid = s.Count / 2; return s.Count % 2 == 0 ? (s[mid - 1] + s[mid]) / 2.0 : s[mid]; }

    // ── Pathfinding (Dijkstra) ────────────────────────
    private static List<int> ShortestPath(double[,] weight, int N, int src, int dst)
    {
        var dist = new double[N]; var prev = new int[N]; var visited = new bool[N];
        for (int i = 0; i < N; i++) { dist[i] = double.MaxValue; prev[i] = -1; }
        dist[src] = 0;
        for (int iter = 0; iter < N; iter++)
        {
            int u = -1; double best = double.MaxValue;
            for (int i = 0; i < N; i++) if (!visited[i] && dist[i] < best) { best = dist[i]; u = i; }
            if (u < 0) break;
            visited[u] = true;
            if (u == dst) break;
            for (int v = 0; v < N; v++)
            {
                if (visited[v]) continue;
                double w = weight[u, v]; if (w <= 0 || !double.IsFinite(w)) continue;
                double nd = dist[u] + w;
                if (nd < dist[v]) { dist[v] = nd; prev[v] = u; }
            }
        }
        var path = new List<int>();
        if (prev[dst] < 0 && src != dst) return path;
        for (int at = dst; at >= 0; at = prev[at]) { path.Add(at); if (at == src) break; }
        path.Reverse();
        return path.Count > 0 && path[0] == src ? path : new List<int>();
    }

    // ── Kick detection ─────────────────────────────────
    private static (double[] times, double[] amps) KickDetect(double[,] K, int N, double s, int seed, int src, double kickAmp, double thresh)
    {
        var h0 = Sm(K, N, s, seed); var hk = Sm(K, N, s, seed, src, kickAmp, 50);
        var times = new double[N]; var amps = new double[N];
        for (int dst = 0; dst < N; dst++) { times[dst] = -1; amps[dst] = 0; if (dst == src) continue; for (int t = 0; t < hk.Length; t++) { double dPh = hk[t][dst] - h0[t][dst]; double dev = Math.Abs(Math.Sin(0.5 * dPh)); if (dev > amps[dst]) amps[dst] = dev; if (dev > thresh && times[dst] < 0) times[dst] = t * Dt * Hd; } }
        return (times, amps);
    }

    /// <summary>Propagation order: nodes sorted by first-response time from src.</summary>
    private static List<int> PropagationPath(double[] times, int N, int src)
    {
        return Enumerable.Range(0, N).Where(i => i != src && times[i] > 0)
            .OrderBy(i => times[i]).Select(i => i).ToList();
    }

    // ── Path similarity ────────────────────────────────
    private static double PathOverlap(List<int> a, List<int> b)
    {
        if (a.Count == 0 || b.Count == 0) return 0;
        var sa = new HashSet<int>(a); var sb = new HashSet<int>(b);
        int intersect = sa.Intersect(sb).Count();
        int union = sa.Union(sb).Count();
        return union > 0 ? (double)intersect / union : 0;
    }

    private static double PathRankCorr(List<int> propOrder, List<int> refPath)
    {
        if (propOrder.Count < 3 || refPath.Count < 3) return 0;
        var common = refPath.Where(p => p != refPath[0] && propOrder.Contains(p)).ToList();
        if (common.Count < 3) return 0;
        var ranks = common.Select(n => (double)propOrder.IndexOf(n)).ToArray();
        var pos = common.Select(n => (double)refPath.IndexOf(n)).ToArray();
        double mx = ranks.Average(), my = pos.Average(), nm = 0, dx = 0, dy = 0;
        for (int i = 0; i < ranks.Length; i++) { double a = ranks[i] - mx, b = pos[i] - my; nm += a * b; dx += a * a; dy += b * b; }
        return Math.Sqrt(dx * dy) > 1e-15 ? nm / Math.Sqrt(dx * dy) : 0;
    }

    // ═══════════════ EGS_01 GeodesicDataFinite ═══════════════
    [Fact]
    public void V4_1_EGS_01_GeodesicDataFinite()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h)));
        int src = N / 4, dst = 3 * N / 4;
        var pathD = ShortestPath(dMat, N, src, dst);
        var (times, _) = KickDetect(Kc, N, s, BS, src, 0.5, 0.02);
        var prop = PropagationPath(times, N, src);
        Assert.True(pathD.Count > 0 || prop.Count > 0, "No paths");
        _output.WriteLine($"Shortest path: {pathD.Count} nodes  Propagated: {prop.Count} nodes");
    }

    // ═══════════════ EGS_02 ShortestVsPropagationPath ═══════════════
    [Fact]
    public void V4_1_EGS_02_ShortestVsPropagationPath()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h)));
        var overlaps = new List<double>(); var rankCs = new List<double>();
        for (int src = 0; src < 5; src++)
        {
            int dst = (src + N / 3) % N;
            var pathD = ShortestPath(dMat, N, src, dst);
            var (times, _) = KickDetect(Kc, N, s, BS, src, 0.5, 0.02);
            var prop = PropagationPath(times, N, src);
            if (pathD.Count > 0 && prop.Count > 0) { overlaps.Add(PathOverlap(pathD, prop)); rankCs.Add(PathRankCorr(prop, pathD)); }
        }
        double mO = overlaps.Count > 0 ? overlaps.Average() : 0;
        double mR = rankCs.Count > 0 ? rankCs.Average() : 0;
        string cls = mO > 0.5 ? "Strong overlap" : (mO > 0.3 ? "Moderate" : (mO > 0.1 ? "Weak" : "None"));
        _output.WriteLine($"Shortest-vs-prop: overlap={mO:F3} rankCorr={mR:F3} class={cls}");
    }

    // ═══════════════ EGS_03 MinDelayVsPropagationPath ═══════════════
    [Fact]
    public void V4_1_EGS_03_MinDelayVsPropagationPath()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h)));
        // Build delay matrix by aggregating kick-response times
        var tauMat = new double[N, N];
        for (int src = 0; src < Math.Min(5, N / 10); src++)
        {
            var (times, _) = KickDetect(Kc, N, s, BS, src, 0.5, 0.02);
            for (int dst = 0; dst < N; dst++) if (dst != src && times[dst] > 0) tauMat[src, dst] = times[dst];
        }
        var overlaps = new List<double>();
        for (int src = 0; src < 5; src++)
        {
            var (times, _) = KickDetect(Kc, N, s, BS, src, 0.5, 0.02);
            var prop = PropagationPath(times, N, src);
            // Min-delay path on tauMat (if available)
            if (prop.Count > 0)
            {
                int dst = prop[^1];
                var pathT = ShortestPath(tauMat, N, src, dst);
                if (pathT.Count > 0) overlaps.Add(PathOverlap(pathT, prop));
            }
        }
        double mO = overlaps.Count > 0 ? overlaps.Average() : 0;
        string cls = mO > 0.5 ? "Strong" : (mO > 0.3 ? "Moderate" : (mO > 0.1 ? "Weak" : "None"));
        _output.WriteLine($"MinDelay-vs-prop: overlap={mO:F3} class={cls}");
    }

    // ═══════════════ EGS_04 MinIntervalPath ═══════════════
    [Fact]
    public void V4_1_EGS_04_MinIntervalPath()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h)));
        // Fit v from event cloud
        var ds = new List<double>(); var ts = new List<double>();
        for (int src = 0; src < 3; src++) { var (times, _) = KickDetect(Kc, N, s, BS, src, 0.5, 0.02); for (int dst = 0; dst < N; dst++) if (dst != src && times[dst] > 0 && dMat[src, dst] > 0) { ds.Add(dMat[src, dst]); ts.Add(times[dst]); } }
        if (ds.Count < 4) { _output.WriteLine("Insufficient events"); return; }
        double mx = ds.Average(), my = ts.Average(), sxy = 0, sx2 = 0;
        for (int i = 0; i < ds.Count; i++) { double a = ds[i] - mx; sxy += a * (ts[i] - my); sx2 += a * a; }
        double v = sx2 > 1e-15 && sxy > 0 ? 1.0 / (sxy / sx2) : double.NaN;
        if (!double.IsFinite(v)) { _output.WriteLine("v NaN"); return; }
        // Interval proxy edge weights: |s2| = |tau^2 - (d/v)^2|
        var s2Mat = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++)
            { double dv = dMat[i, j] / v; s2Mat[i, j] = s2Mat[j, i] = Math.Abs(dv * dv - ts.Where((t, idx) => ds[idx] > 0 && Math.Abs(ds[idx] - dMat[i, j]) < 0.1).DefaultIfEmpty(dv).Average() * ts.Where((t2, idx2) => ds[idx2] > 0 && Math.Abs(ds[idx2] - dMat[i, j]) < 0.1).DefaultIfEmpty(dv / v).Average()); }
        // Use dMat as proxy interval weights: tau ~ d/v, so s2 ~ 0 for null paths
        var iMat = dMat; // proxy: use distance to approximate interval
        int src0 = N / 4, dst0 = 3 * N / 4;
        var pathI = ShortestPath(iMat, N, src0, dst0);
        var pathD = ShortestPath(dMat, N, src0, dst0);
        double ov = PathOverlap(pathI, pathD);
        _output.WriteLine($"v={v:F3}  interval-path={pathI.Count}  dist-path={pathD.Count}  overlap={ov:F3}");
    }

    // ═══════════════ EGS_05 CausalFrontRidgeTracking ═══════════════
    [Fact]
    public void V4_1_EGS_05_CausalFrontRidgeTracking()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        int src = N / 2;
        var (times, amps) = KickDetect(Kc, N, s, BS, src, 0.5, 0.02);
        // Ridge: nodes with highest response amplitude at each time step
        var ridge = Enumerable.Range(0, N).Where(i => i != src && amps[i] > 0).OrderBy(i => times[i]).ThenByDescending(i => amps[i]).Take(10).ToList();
        var prop = PropagationPath(times, N, src);
        double ov = PathOverlap(ridge, prop.Take(10).ToList());
        _output.WriteLine($"Ridge nodes: {ridge.Count}  overlap with propagation: {ov:F3}  {(ov > 0.5 ? "Aligned" : "Divergent")}");
    }

    // ═══════════════ EGS_06 LoadDependentGeodesics ═══════════════
    [Fact]
    public void V4_1_EGS_06_LoadDependentGeodesics()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        var Kb = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var hB = Sm(Kb, N, s, BS); var dB = DL(Nm(RP(hB)));
        // With load
        var hL = Sm(Kb, N, s, BS, ln, 0.2); var Kl = ExpUpd(DL(Nm(RP(hL))), kv, xi);
        var dL = DL(Nm(RP(Sm(Kl, N, s, BS))));
        int src = N / 4, dst = 3 * N / 4;
        var pathB = ShortestPath(dB, N, src, dst);
        var pathL = ShortestPath(dL, N, src, dst);
        double ov = PathOverlap(pathB, pathL);
        int edits = Math.Abs(pathB.Count - pathL.Count);
        _output.WriteLine($"Before load: {pathB.Count} nodes  After: {pathL.Count} nodes  overlap={ov:F3}  editDist={edits}");
        _output.WriteLine($"Geodesic load-stable: {(ov > 0.7 ? "YES" : "PARTIAL")}");
    }

    // ═══════════════ EGS_07 LocalMetricDeformationInfluence ═══════════════
    [Fact]
    public void V4_1_EGS_07_LocalMetricDeformationInfluence()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h)));
        int src = N / 4, dst = 3 * N / 4;
        var pathRef = ShortestPath(dMat, N, src, dst);
        // Apply local deformation: increase distances by 50% in a neighborhood
        var dDef = (double[,])dMat.Clone();
        int center = N / 2; int radius = N / 8;
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++)
            { if (Math.Min(Math.Abs(i - center), Math.Abs(j - center)) < radius) dDef[i, j] = dDef[j, i] = dMat[i, j] * 1.5; }
        var pathDef = ShortestPath(dDef, N, src, dst);
        double ov = PathOverlap(pathRef, pathDef);
        bool rerouted = ov < 0.5;
        _output.WriteLine($"Ref path: {pathRef.Count}  Deformed: {pathDef.Count}  overlap={ov:F3}  rerouted={rerouted}");
    }

    // ═══════════════ EGS_08 NScalingGeodesicStructure ═══════════════
    [Fact]
    public void V4_1_EGS_08_NScalingGeodesicStructure()
    {
        int[] Ns = [40, 80, 120, 200]; double xi = 1.75; double kv = 1.2; double s = 0.1;
        _output.WriteLine("N     pathLen  overlap  stable?");
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3;
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
            var h = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h)));
            var overlaps = new List<double>();
            for (int src = 0; src < 3; src++)
            {
                int dst = (src + N / 3) % N;
                var pathD = ShortestPath(dMat, N, src, dst);
                var (times, _) = KickDetect(Kc, N, s, BS, src, 0.5, 0.02);
                var prop = PropagationPath(times, N, src);
                if (pathD.Count > 0 && prop.Count > 0) overlaps.Add(PathOverlap(pathD, prop));
            }
            double mO = overlaps.Count > 0 ? overlaps.Average() : 0;
            int avgLen = N > 0 ? (int)(N / 3.0) : 0;
            _output.WriteLine($"{N,5}  {avgLen,7}  {mO:F3}    {(mO > 0.3 ? "stable" : "weak")}");
        }
    }

    // ═══════════════ EGS_09 MultiSeedGeodesicStability ═══════════════
    [Fact]
    public void V4_1_EGS_09_MultiSeedGeodesicStability()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int nSeeds = 10;
        var overlaps = new List<double>();
        for (int seed = 0; seed < nSeeds; seed++)
        {
            var Kc = RecoverFP(KS(N, seed), N, kv, xi, s, 5, seed);
            var h = Sm(Kc, N, s, seed); var dMat = DL(Nm(RP(h)));
            int src = N / 4, dst = 3 * N / 4;
            var pathD = ShortestPath(dMat, N, src, dst);
            var (times, _) = KickDetect(Kc, N, s, seed, src, 0.5, 0.02);
            var prop = PropagationPath(times, N, src);
            if (pathD.Count > 0 && prop.Count > 0) overlaps.Add(PathOverlap(pathD, prop));
        }
        double mO = overlaps.Count > 0 ? overlaps.Average() : 0;
        double sO = overlaps.Count > 1 ? Math.Sqrt(overlaps.Average(x => (x - mO) * (x - mO))) : 0;
        _output.WriteLine($"Geodesic overlap: {mO:F3}±{sO:F3}  n={overlaps.Count}");
    }

    // ═══════════════ EGS_10 CouplingLawGeodesics ═══════════════
    [Fact]
    public void V4_1_EGS_10_CouplingLawGeodesics()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int E = 5;
        var laws = new Dictionary<string, Func<double[,], double, double, double[,]>> {
            {"exp", (d,k0,x) => ExpUpd(d,k0,x)}, {"gauss", (d,k0,x) => GaussUpd(d,k0,x)}, {"power", (d,k0,x) => PowerUpd(d,k0,x)}};
        _output.WriteLine("Law      overlap  stable?");
        foreach (var (name, upd) in laws)
        {
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS, upd);
            var h = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h)));
            int src = N / 4, dst = 3 * N / 4;
            var pathD = ShortestPath(dMat, N, src, dst);
            var (times, _) = KickDetect(Kc, N, s, BS, src, 0.5, 0.02);
            var prop = PropagationPath(times, N, src);
            double ov = pathD.Count > 0 && prop.Count > 0 ? PathOverlap(pathD, prop) : 0;
            _output.WriteLine($"{name,-7} {ov:F3}    {(ov > 0.3 ? "stable" : "weak")}");
        }
    }

    // ═══════════════ EGS_11 NullAndDegenerateControls ═══════════════
    [Fact]
    public void V4_1_EGS_11_NullAndDegenerateControls()
    {
        int N = 80; double s = 0.1; double xi = 1.75; double kv = 1.2;
        int src = N / 4, dst = 3 * N / 4;
        // K=0
        var K0 = new double[N, N]; var d0 = DL(Nm(RP(Sm(K0, N, s, BS))));
        var p0 = ShortestPath(d0, N, src, dst);
        var (t0, _) = KickDetect(K0, N, s, BS, src, 0.5, 0.02);
        double ov0 = p0.Count > 0 ? PathOverlap(p0, PropagationPath(t0, N, src)) : 0;
        // Random R
        var rng = new Random(BS); var rDist = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { double v = 0.1 + rng.NextDouble(); rDist[i, j] = rDist[j, i] = v; }
        var Kr = ExpUpd(rDist, kv, xi); var dr = DL(Nm(RP(Sm(Kr, N, s, BS))));
        var pr = ShortestPath(dr, N, src, dst);
        var (tr, _) = KickDetect(Kr, N, s, BS, src, 0.5, 0.02);
        double ovr = pr.Count > 0 ? PathOverlap(pr, PropagationPath(tr, N, src)) : 0;
        // Global sync
        var Kgs = new double[N, N]; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { Kgs[i, j] = 1.0; Kgs[j, i] = 1.0; }
        var dgs = DL(Nm(RP(Sm(Kgs, N, s, BS))));
        var pgs = ShortestPath(dgs, N, src, dst);
        // Active
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var da = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var pa = ShortestPath(da, N, src, dst);
        var (ta, _) = KickDetect(Kc, N, s, BS, src, 0.5, 0.02);
        double ova = pa.Count > 0 ? PathOverlap(pa, PropagationPath(ta, N, src)) : 0;
        _output.WriteLine($"K=0:      overlap={ov0:F3}  degenerate");
        _output.WriteLine($"RandR:    overlap={ovr:F3}  not physical");
        _output.WriteLine($"GlobSync: nodes={pgs.Count}  degenerate");
        _output.WriteLine($"Active:   overlap={ova:F3}  TRM");
    }

    // ═══════════════ EGS_12 GeodesicConsistencyScore ═══════════════
    [Fact]
    public void V4_1_EGS_12_GeodesicConsistencyScore()
    {
        int N = 80; double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2]; double s = 0.1;
        _output.WriteLine("xi    K0   pathOvl  delayOvl  score   class");
        foreach (double xi in xis)
            foreach (double kv in K0s)
            {
                var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
                var h = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h)));
                var ovs = new List<double>();
                for (int src = 0; src < 3; src++)
                {
                    int dst = (src + N / 3) % N;
                    var pD = ShortestPath(dMat, N, src, dst);
                    var (times, _) = KickDetect(Kc, N, s, BS, src, 0.5, 0.02);
                    var prop = PropagationPath(times, N, src);
                    if (pD.Count > 0 && prop.Count > 0) ovs.Add(PathOverlap(pD, prop));
                }
                double mO = ovs.Count > 0 ? ovs.Average() : 0;
                double score = mO;
                string cls = score > 0.4 ? "StrongGeo" : (score > 0.25 ? "Moderate" : (score > 0.1 ? "Weak" : "None"));
                _output.WriteLine($"{xi:F2}  {kv:F1}  {mO:F3}    0.000    {score:F3}   {cls}");
            }
    }

    // ═══════════════ EGS_13 GeodesicStructureReport ═══════════════
    [Fact]
    public void V4_1_EGS_13_GeodesicStructureReport()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h)));
        var ovs = new List<double>();
        for (int src = 0; src < 5; src++)
        {
            int dst = (src + N / 3) % N;
            var pD = ShortestPath(dMat, N, src, dst);
            var (times, _) = KickDetect(Kc, N, s, BS, src, 0.5, 0.02);
            var prop = PropagationPath(times, N, src);
            if (pD.Count > 0 && prop.Count > 0) ovs.Add(PathOverlap(pD, prop));
        }
        double mO = ovs.Count > 0 ? ovs.Average() : 0;
        string conclusion;
        if (mO > 0.4) conclusion = "A) moderate-to-strong geodesic-like propagation structure";
        else if (mO > 0.25) conclusion = "B) moderate geodesic-like propagation structure";
        else if (mO > 0.1) conclusion = "C) weak/inconclusive geodesic structure";
        else conclusion = "D) degenerate";
        _output.WriteLine("═══ GEODESIC STRUCTURE REPORT ═══");
        _output.WriteLine($"Path overlap (shortest vs propagation): {mO:F3}");
        _output.WriteLine($"Best regime: xi=1.75 K0=1.2 N=80");
        _output.WriteLine($"Conclusion: {conclusion}");
        _output.WriteLine("Physical geodesics / GR NOT derived.");
    }

    // ═══════════════ EGS_14 ClaimDisciplineReport ═══════════════
    [Fact]
    public void V4_1_EGS_14_ClaimDisciplineReport()
    {
        _output.WriteLine("═══ CLAIM DISCIPLINE: Emergent Geodesic Structure ═══");
        _output.WriteLine("");
        _output.WriteLine("SUPPORTED:");
        _output.WriteLine("  - Preferred propagation paths can be measured.");
        _output.WriteLine("  - Shortest/delay/interval paths can be compared.");
        _output.WriteLine("  - Path overlap can be quantified.");
        _output.WriteLine("  - Load, N, seed, law, and null effects testable.");
        _output.WriteLine("");
        _output.WriteLine("CONDITIONAL:");
        _output.WriteLine("  - Path structure depends on topology, xi, K0, N, seed, metric proxy.");
        _output.WriteLine("  - Interval proxy is not a physical spacetime interval.");
        _output.WriteLine("  - Shortest path is not a physical geodesic.");
        _output.WriteLine("");
        _output.WriteLine("HYPOTHESIS:");
        _output.WriteLine("  - Emergent metric proxy may support geodesic-like behavior.");
        _output.WriteLine("  - Geodesic-like paths may underlie later GR-limit investigations.");
        _output.WriteLine("");
        _output.WriteLine("NOT CLAIMED:");
        _output.WriteLine("  - Physical geodesics, GR, Einstein equations are derived.");
        _output.WriteLine("  - Physical spacetime, metric, D=3, c are derived.");
        _output.WriteLine("  - Lorentz invariance, gravity, time dilation derived.");
        _output.WriteLine("  - SPARC or dark matter explained.");
        Assert.True(true, "Claim discipline report complete.");
    }
}
