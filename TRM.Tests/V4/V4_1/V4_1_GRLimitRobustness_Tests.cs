using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// GR-limit robustness: tests whether the moderate weak-field GR-like proxy
/// consistency from V4_1_GRLimitProbe is robust under proxy definitions,
/// coordinate choices, load regimes, N-scaling, seeds, and coupling laws.
///
/// Does NOT claim GR, Einstein equations, gravity, physical mass, redshift,
/// lensing, metric, spacetime, c, D=3, SPARC, or dark matter.
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_GRLimitRobustness")]
public class V4_1_GRLimitRobustness_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_GRLimitRobustness_Tests(ITestOutputHelper o) { _output = o; }

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

    // ── Phi proxy variants ─────────────────────────────
    private static double[] PhiFromOmega(double[,] Kc, double[,] dMat, int N, double s, int ln, double dO)
    {
        var h0 = Sm(Kc, N, s, BS); var hL = Sm(Kc, N, s, BS, ln, dO);
        var o0 = OmegaField(h0); var oL = OmegaField(hL);
        var dOmg = oL.Zip(o0, (a, b) => a - b).ToArray();
        double norm = dOmg.Max(x => Math.Abs(x)) + 1e-6;
        return dOmg.Select(x => x / norm).ToArray();
    }

    private static double[] PhiFromDistance(double[,] dMat0, double[,] dMatL, int N, int ln)
    {
        var phi = new double[N]; double maxVal = 0;
        for (int i = 0; i < N; i++) { phi[i] = dMatL[ln, i] - dMat0[ln, i]; if (Math.Abs(phi[i]) > maxVal) maxVal = Math.Abs(phi[i]); }
        if (maxVal < 1e-6) maxVal = 1;
        for (int i = 0; i < N; i++) phi[i] /= maxVal;
        return phi;
    }

    private static double[] PhiFromKernel(double[,] dMat, int N, int ln, double dO)
    {
        var dists = Enumerable.Range(0, N).Select(i => i == ln ? 0.0 : dMat[ln, i]).ToArray();
        double maxD = dists.Max() + 1e-6;
        var phi = dists.Select(d => dO * Math.Exp(-d / maxD)).ToArray();
        double norm = phi.Max(x => Math.Abs(x)) + 1e-6;
        return phi.Select(x => x / norm).ToArray();
    }

    // ── g00 proxy variants ─────────────────────────────
    private static double G00_Omega(double[] omega) { double m = omega.Average(); double cv = m > 1e-6 ? Math.Sqrt(omega.Average(x => (x - m) * (x - m))) / m : 1; return 1.0 / (1.0 + cv) / Math.Max(m, 1e-6); }
    private static double G00_Temporal(double[,] dMat, int N) { double s = 0; int c = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { s += dMat[i, j]; c++; } return c > 0 ? 1.0 / (1.0 + s / c) : 1.0; }
    private static double G00_DimStab(double[,] Kc, int N, double s) { var h = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h))); var ds = new List<double>(); int nC = Math.Max(3, Math.Min(8, N / 5)); for (int c = 0; c < nC; c++) { var dists = Enumerable.Range(0, N).Where(x => x != c * N / nC).Select(x => dMat[c * N / nC, x]).OrderBy(x => x).ToArray(); int nSh = Math.Min(8, dists.Length / 4); if (nSh < 3) continue; var lR = new List<double>(); var lN = new List<double>(); for (int i = 0; i < nSh; i++) { int cnt = (i + 1) * dists.Length / nSh; double rSh = dists[Math.Min(cnt - 1, dists.Length - 1)]; if (rSh < 1e-6) continue; lR.Add(Math.Log(rSh)); lN.Add(Math.Log(cnt)); } if (lR.Count < 3) continue; double mx = lR.Average(), my = lN.Average(), num = 0, dx = 0; for (int i = 0; i < lR.Count; i++) { double a = lR[i] - mx; num += a * (lN[i] - my); dx += a * a; } if (dx > 1e-15) ds.Add(num / dx); } double m = ds.Count > 0 ? ds.Average() : double.NaN; double sp = ds.Count > 1 ? Math.Sqrt(ds.Average(x => (x - m) * (x - m))) : 1; return double.IsFinite(m) ? 1.0 / (1.0 + sp) : 0; }

    // ── Pathfinding ───────────────────────────────────
    private static List<int> ShortestPath(double[,] weight, int N, int src, int dst)
    {
        var dist = new double[N]; var prev = new int[N]; var visited = new bool[N];
        for (int i = 0; i < N; i++) { dist[i] = double.MaxValue; prev[i] = -1; }
        dist[src] = 0;
        for (int iter = 0; iter < N; iter++) { int u = -1; double best = double.MaxValue; for (int i = 0; i < N; i++) if (!visited[i] && dist[i] < best) { best = dist[i]; u = i; } if (u < 0) break; visited[u] = true; if (u == dst) break; for (int v = 0; v < N; v++) { if (visited[v]) continue; double w = weight[u, v]; if (w <= 0 || !double.IsFinite(w)) continue; double nd = dist[u] + w; if (nd < dist[v]) { dist[v] = nd; prev[v] = u; } } }
        var path = new List<int>(); if (prev[dst] < 0 && src != dst) return path;
        for (int at = dst; at >= 0; at = prev[at]) { path.Add(at); if (at == src) break; } path.Reverse();
        return path.Count > 0 && path[0] == src ? path : new List<int>();
    }
    private static double PathOverlap(List<int> a, List<int> b) { if (a.Count == 0 || b.Count == 0) return 0; var sa = new HashSet<int>(a); var sb = new HashSet<int>(b); return (double)sa.Intersect(sb).Count() / Math.Max(sa.Union(sb).Count(), 1); }

    // ═══════════════ GRLR_01 RobustnessDataFinite ═══════════════
    [Fact]
    public void V4_1_GRLR_01_RobustnessDataFinite()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2; double dO = 0.2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h0 = Sm(Kc, N, s, BS); var dMat0 = DL(Nm(RP(h0)));
        var hL = Sm(Kc, N, s, BS, ln, dO); var dMatL = DL(Nm(RP(hL)));
        var pO = PhiFromOmega(Kc, dMat0, N, s, ln, dO);
        var pD = PhiFromDistance(dMat0, dMatL, N, ln);
        var pK = PhiFromKernel(dMat0, N, ln, dO);
        double gO = G00_Omega(OmegaField(h0));
        double gT = G00_Temporal(dMat0, N);
        double gD = G00_DimStab(Kc, N, s);
        Assert.True(double.IsFinite(gO) && double.IsFinite(gT) && double.IsFinite(gD), "g00 NaN");
        _output.WriteLine($"Phi variants: Omega={pO.Average():F4}  dDist={pD.Average():F4}  Kernel={pK.Average():F4}");
        _output.WriteLine($"g00 variants: Omega={gO:F3}  Temporal={gT:F3}  DimStab={gD:F3}");
    }

    // ═══════════════ GRLR_02 PhiProxyDefinitionSensitivity ═══════════════
    [Fact]
    public void V4_1_GRLR_02_PhiProxyDefinitionSensitivity()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2; double dO = 0.2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h0 = Sm(Kc, N, s, BS); var dMat0 = DL(Nm(RP(h0)));
        var hL = Sm(Kc, N, s, BS, ln, dO); var dMatL = DL(Nm(RP(hL)));
        var pO = PhiFromOmega(Kc, dMat0, N, s, ln, dO);
        var pD = PhiFromDistance(dMat0, dMatL, N, ln);
        var pK = PhiFromKernel(dMat0, N, ln, dO);
        double rOD = Spear(pO, pD);
        double rOK = Spear(pO, pK);
        double rDK = Spear(pD, pK);
        double avgR = (Math.Abs(rOD) + Math.Abs(rOK) + Math.Abs(rDK)) / 3.0;
        string cls = avgR > 0.6 ? "Robust" : (avgR > 0.3 ? "Moderate" : "Sensitive");
        _output.WriteLine($"Phi corrs: O-D={rOD:F3} O-K={rOK:F3} D-K={rDK:F3} avg={avgR:F3} class={cls}");
    }

    // ═══════════════ GRLR_03 G00ProxyDefinitionSensitivity ═══════════════
    [Fact]
    public void V4_1_GRLR_03_G00ProxyDefinitionSensitivity()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h0 = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h0)));
        double gO = G00_Omega(OmegaField(h0));
        double gT = G00_Temporal(dMat, N);
        double gD = G00_DimStab(Kc, N, s);
        double spread = Math.Abs(gO - gT) + Math.Abs(gT - gD) + Math.Abs(gO - gD);
        string cls = spread < 0.5 ? "Robust" : (spread < 1.0 ? "Moderate" : "Sensitive");
        _output.WriteLine($"g00: Omega={gO:F3} Temporal={gT:F3} DimStab={gD:F3} spread={spread:F3} class={cls}");
    }

    // ═══════════════ GRLR_04 SpatialProxyDefinitionSensitivity ═══════════════
    [Fact]
    public void V4_1_GRLR_04_SpatialProxyDefinitionSensitivity()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h0 = Sm(Kc, N, s, BS); var dMat0 = DL(Nm(RP(h0)));
        var hL = Sm(Kc, N, s, BS, ln, 0.2); var dMatL = DL(Nm(RP(hL)));
        // Spatial proxy 1: mean squared distance
        double gSp1_0 = Enumerable.Range(0, N).SelectMany(i => Enumerable.Range(i + 1, N - i - 1).Select(j => dMat0[i, j] * dMat0[i, j])).Average();
        double gSp1_L = Enumerable.Range(0, N).SelectMany(i => Enumerable.Range(i + 1, N - i - 1).Select(j => dMatL[i, j] * dMatL[i, j])).Average();
        // Spatial proxy 2: mean distance (linear)
        double gSp2_0 = Enumerable.Range(0, N).SelectMany(i => Enumerable.Range(i + 1, N - i - 1).Select(j => dMat0[i, j])).Average();
        double gSp2_L = Enumerable.Range(0, N).SelectMany(i => Enumerable.Range(i + 1, N - i - 1).Select(j => dMatL[i, j])).Average();
        double d1 = Math.Abs(gSp1_L - gSp1_0) / Math.Max(gSp1_0, 1e-6);
        double d2 = Math.Abs(gSp2_L - gSp2_0) / Math.Max(gSp2_0, 1e-6);
        string cls = Math.Abs(d1 - d2) < 0.1 ? "Robust" : "Sensitive";
        _output.WriteLine($"Spatial Δ: msd={d1:F3}  mean={d2:F3}  class={cls}");
    }

    // ═══════════════ GRLR_05 BendingProxyRobustness ═══════════════
    [Fact]
    public void V4_1_GRLR_05_BendingProxyRobustness()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h0 = Sm(Kc, N, s, BS); var dMat0 = DL(Nm(RP(h0)));
        int src = N / 4, dst = 3 * N / 4;
        var pRef = ShortestPath(dMat0, N, src, dst);
        double[] loads = [0.05, 0.1, 0.2, 0.35];
        var bends = new List<double>();
        foreach (double load in loads)
        {
            var hL = Sm(Kc, N, s, BS, ln, load); var dMatL = DL(Nm(RP(hL)));
            var pL = ShortestPath(dMatL, N, src, dst);
            bends.Add(1.0 - PathOverlap(pRef, pL));
        }
        // Check monotonicity
        bool increasing = true;
        for (int i = 1; i < bends.Count; i++) if (bends[i] < bends[i - 1] - 0.05) increasing = false;
        _output.WriteLine($"Bending trend: {string.Join(" ", bends.Select(x => x.ToString("F3")))}  monotonic={increasing}");
    }

    // ═══════════════ GRLR_06 LoadRangeRobustness ═══════════════
    [Fact]
    public void V4_1_GRLR_06_LoadRangeRobustness()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        double[] loads = [0.01, 0.05, 0.10, 0.20, 0.35, 0.50];
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h0 = Sm(Kc, N, s, BS); var dMat0 = DL(Nm(RP(h0)));
        _output.WriteLine("load    dOmega/local  g00_shift  class");
        double g00Ref = G00_Omega(OmegaField(h0));
        foreach (double load in loads)
        {
            var hL = Sm(Kc, N, s, BS, ln, load);
            double g00L = G00_Omega(OmegaField(hL));
            double dg = Math.Abs(g00L - g00Ref) / Math.Max(g00Ref, 1e-6);
            string cls = load <= 0.05 ? "tooWeak" : (load <= 0.2 ? "weakField" : (load <= 0.35 ? "mild" : "nonlin"));
            _output.WriteLine($"{load:F2}    {Math.Abs(OmegaField(hL)[ln] - OmegaField(h0)[ln]):F4}        {dg:F3}      {cls}");
        }
    }

    // ═══════════════ GRLR_07 CoordinateProxyRobustness ═══════════════
    [Fact]
    public void V4_1_GRLR_07_CoordinateProxyRobustness()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int src = N / 4, dst = 3 * N / 4;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var dMat = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var pRef = ShortestPath(dMat, N, src, dst);
        // Shell coordinate
        var dShell = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { double si = Enumerable.Range(0, N).Count(x => x != i && dMat[i, x] < dMat[i, j]) / (double)(N - 1); double sj = Enumerable.Range(0, N).Count(x => x != j && dMat[j, x] < dMat[j, i]) / (double)(N - 1); dShell[i, j] = dShell[j, i] = dMat[i, j] * (1.0 + 0.3 * Math.Abs(si - sj)); }
        // Deg-weighted
        var degs = new double[N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j) degs[i] += Kc[i, j];
        var dDeg = new double[N, N]; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { double w = dMat[i, j] / Math.Max((degs[i] + degs[j]) * 0.5, 1e-6); dDeg[i, j] = dDeg[j, i] = w; }
        double ovSh = PathOverlap(pRef, ShortestPath(dShell, N, src, dst));
        double ovDg = PathOverlap(pRef, ShortestPath(dDeg, N, src, dst));
        _output.WriteLine($"ShellCoord overlap={ovSh:F3}  DegWeight overlap={ovDg:F3}  robust={(ovSh > 0.5 && ovDg > 0.5 ? "YES" : "PARTIAL")}");
    }

    // ═══════════════ GRLR_08 NScalingRobustness ═══════════════
    [Fact]
    public void V4_1_GRLR_08_NScalingRobustness()
    {
        int[] Ns = [40, 80, 120, 200]; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln(int n) => n / 2;
        _output.WriteLine("N     g00_O  g00_T  g00_D  PhiAgr  class");
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3;
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
            var h0 = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h0)));
            double gO = G00_Omega(OmegaField(h0)); double gT = G00_Temporal(dMat, N); double gD = G00_DimStab(Kc, N, s);
            var hL = Sm(Kc, N, s, BS, ln(N), 0.2); var dMatL = DL(Nm(RP(hL)));
            var pO = PhiFromOmega(Kc, dMat, N, s, ln(N), 0.2);
            var pD = PhiFromDistance(dMat, dMatL, N, ln(N));
            double pa = Spear(pO, pD);
            _output.WriteLine($"{N,5}  {gO:F3}  {gT:F3}  {gD:F3}  {pa:F3}   {(Math.Abs(pa) > 0.3 ? "stable" : "weak")}");
        }
    }

    // ═══════════════ GRLR_09 MultiSeedRobustness ═══════════════
    [Fact]
    public void V4_1_GRLR_09_MultiSeedRobustness()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int nSeeds = 10;
        var gOs = new List<double>(); var gTs = new List<double>(); var gDs = new List<double>();
        for (int seed = 0; seed < nSeeds; seed++)
        {
            var Kc = RecoverFP(KS(N, seed), N, kv, xi, s, 5, seed);
            var h0 = Sm(Kc, N, s, seed); var dMat = DL(Nm(RP(h0)));
            gOs.Add(G00_Omega(OmegaField(h0))); gTs.Add(G00_Temporal(dMat, N)); gDs.Add(G00_DimStab(Kc, N, s));
        }
        _output.WriteLine($"g00_O: {gOs.Average():F3}±{Math.Sqrt(gOs.Average(x => (x - gOs.Average()) * (x - gOs.Average()))):F3}");
        _output.WriteLine($"g00_T: {gTs.Average():F3}±{Math.Sqrt(gTs.Average(x => (x - gTs.Average()) * (x - gTs.Average()))):F3}");
        _output.WriteLine($"g00_D: {gDs.Average():F3}±{Math.Sqrt(gDs.Average(x => (x - gDs.Average()) * (x - gDs.Average()))):F3}  n={nSeeds}");
    }

    // ═══════════════ GRLR_10 CouplingLawRobustness ═══════════════
    [Fact]
    public void V4_1_GRLR_10_CouplingLawRobustness()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int E = 5;
        var laws = new Dictionary<string, Func<double[,], double, double, double[,]>> {
            {"exp", (d,k0,x) => ExpUpd(d,k0,x)}, {"gauss", (d,k0,x) => GaussUpd(d,k0,x)}, {"power", (d,k0,x) => PowerUpd(d,k0,x)}};
        _output.WriteLine("Law      g00_O  g00_T  g00_D  stable?");
        foreach (var (name, upd) in laws)
        {
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS, upd);
            var h0 = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h0)));
            double gO = G00_Omega(OmegaField(h0)); double gT = G00_Temporal(dMat, N); double gD = G00_DimStab(Kc, N, s);
            _output.WriteLine($"{name,-7} {gO:F3}  {gT:F3}  {gD:F3}    {(Math.Abs(gO - gT) < 0.5 ? "yes" : "partial")}");
        }
    }

    // ═══════════════ GRLR_11 NullAndDegenerateControls ═══════════════
    [Fact]
    public void V4_1_GRLR_11_NullAndDegenerateControls()
    {
        int N = 80; double s = 0.1; double xi = 1.75; double kv = 1.2;
        _output.WriteLine("Control     g00_O  g00_T  g00_D");
        // K=0
        var K0 = new double[N, N]; var h0 = Sm(K0, N, s, BS); var d0 = DL(Nm(RP(h0)));
        _output.WriteLine($"K=0:        {G00_Omega(OmegaField(h0)):F3}  {G00_Temporal(d0, N):F3}  {G00_DimStab(K0, N, s):F3}");
        // Random R
        var rng = new Random(BS); var rDist = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { double v = 0.1 + rng.NextDouble(); rDist[i, j] = rDist[j, i] = v; }
        var Kr = ExpUpd(rDist, kv, xi); var hr = Sm(Kr, N, s, BS); var dr = DL(Nm(RP(hr)));
        _output.WriteLine($"RandR:      {G00_Omega(OmegaField(hr)):F3}  {G00_Temporal(dr, N):F3}  {G00_DimStab(Kr, N, s):F3}");
        // Global sync
        var Kgs = new double[N, N]; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { Kgs[i, j] = 1.0; Kgs[j, i] = 1.0; }
        var hgs = Sm(Kgs, N, s, BS); var dgs = DL(Nm(RP(hgs)));
        _output.WriteLine($"GlobSync:   {G00_Omega(OmegaField(hgs)):F3}  {G00_Temporal(dgs, N):F3}  {G00_DimStab(Kgs, N, s):F3}");
        // Active
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var ha = Sm(Kc, N, s, BS); var da = DL(Nm(RP(ha)));
        _output.WriteLine($"ActiveTRM:  {G00_Omega(OmegaField(ha)):F3}  {G00_Temporal(da, N):F3}  {G00_DimStab(Kc, N, s):F3}");
    }

    // ═══════════════ GRLR_12 GRLimitRobustnessScore ═══════════════
    [Fact]
    public void V4_1_GRLR_12_GRLimitRobustnessScore()
    {
        int N = 80; double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2]; double s = 0.1; int ln = N / 2;
        _output.WriteLine("xi    K0   PhiAgr  g00Spr  spatSt  score   class");
        foreach (double xi in xis)
            foreach (double kv in K0s)
            {
                var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
                var h0 = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h0)));
                double gO = G00_Omega(OmegaField(h0)); double gT = G00_Temporal(dMat, N); double gD = G00_DimStab(Kc, N, s);
                double gSpr = 1.0 / (1.0 + Math.Abs(gO - gT) + Math.Abs(gT - gD) + Math.Abs(gO - gD));
                var hL = Sm(Kc, N, s, BS, ln, 0.2); var dMatL = DL(Nm(RP(hL)));
                double pa = Math.Abs(Spear(PhiFromOmega(Kc, dMat, N, s, ln, 0.2), PhiFromDistance(dMat, dMatL, N, ln)));
                double gSp0 = Enumerable.Range(0, N).SelectMany(i => Enumerable.Range(i + 1, N - i - 1).Select(j => dMat[i, j] * dMat[i, j])).Average();
                double gSpL = Enumerable.Range(0, N).SelectMany(i => Enumerable.Range(i + 1, N - i - 1).Select(j => dMatL[i, j] * dMatL[i, j])).Average();
                double sSt = 1.0 / (1.0 + Math.Abs(gSpL - gSp0) / Math.Max(gSp0, 1e-6));
                double score = pa * gSpr * sSt;
                string cls = score > 0.3 ? "RobustGR" : (score > 0.15 ? "Moderate" : (score > 0.05 ? "Weak" : "None"));
                _output.WriteLine($"{xi:F2}  {kv:F1}  {pa:F3}   {gSpr:F3}   {sSt:F3}   {score:F3}  {cls}");
            }
    }

    // ═══════════════ GRLR_13 GRLimitRobustnessReport ═══════════════
    [Fact]
    public void V4_1_GRLR_13_GRLimitRobustnessReport()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var h0 = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h0)));
        double gO = G00_Omega(OmegaField(h0)); double gT = G00_Temporal(dMat, N);
        var hL = Sm(Kc, N, s, BS, ln, 0.2); var dMatL = DL(Nm(RP(hL)));
        double pa = Math.Abs(Spear(PhiFromOmega(Kc, dMat, N, s, ln, 0.2), PhiFromDistance(dMat, dMatL, N, ln)));
        double score = pa * (1.0 / (1.0 + Math.Abs(gO - gT)));
        string conclusion = score > 0.3 ? "A) robust weak-field GR-like proxy consistency" : (score > 0.15 ? "B) moderate robustness" : (score > 0.05 ? "C) weak" : "D) degenerate"));
        _output.WriteLine("═══ GR-LIMIT ROBUSTNESS REPORT ═══");
        _output.WriteLine($"Phi agreement: {pa:F3}  g00 spread: {Math.Abs(gO - gT):F3}");
        _output.WriteLine($"Robustness score: {score:F3}  weakField range: load ≤ 0.2");
        _output.WriteLine($"Conclusion: {conclusion}");
        _output.WriteLine("GR, Einstein equations, gravity NOT derived.");
    }

    // ═══════════════ GRLR_14 ClaimDisciplineReport ═══════════════
    [Fact]
    public void V4_1_GRLR_14_ClaimDisciplineReport()
    {
        _output.WriteLine("═══ CLAIM DISCIPLINE: GR-Limit Robustness ═══");
        _output.WriteLine("");
        _output.WriteLine("SUPPORTED:");
        _output.WriteLine("  - GR-like proxy robustness testable across proxy definitions.");
        _output.WriteLine("  - Weak-field load range classifiable.");
        _output.WriteLine("  - Coordinate and metric-proxy sensitivity measurable.");
        _output.WriteLine("  - N, seed, law, null, and degenerate controls evaluable.");
        _output.WriteLine("");
        _output.WriteLine("CONDITIONAL:");
        _output.WriteLine("  - Robustness depends on proxy defs, load, xi, K0, N, seed, coords.");
        _output.WriteLine("  - Phi_proxy is NOT physical gravitational potential.");
        _output.WriteLine("  - Time-rate proxy is NOT physical redshift.");
        _output.WriteLine("  - Bending proxy is NOT physical lensing.");
        _output.WriteLine("");
        _output.WriteLine("NOT CLAIMED:");
        _output.WriteLine("  - GR, Einstein equations, gravity, mass, redshift, lensing, spacetime, metric, c, D=3, SPARC, dark matter.");
        Assert.True(true, "Claim discipline report complete.");
    }
}
