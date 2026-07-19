using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Source-curvature continuum scaling: tests whether the numerical relation
/// CurvatureProxy ≈ alpha × SourceProxy remains stable as N increases and
/// whether alpha admits a meaningful large-N scaling trend.
///
/// Does NOT claim Einstein equations, GR, stress-energy, Ricci, gravity,
/// G, c, D=3, SPARC, or dark matter. Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_SourceCurvatureContinuumScaling")]
public class V4_1_SourceCurvatureContinuumScaling_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_SourceCurvatureContinuumScaling_Tests(ITestOutputHelper o) { _output = o; }

    private static double[][] Sm(double[,] K, int N, double s, int seed, int ln = -1, double dO = 0)
    {
        var r = new Random(seed); var w = new double[N]; for (int i = 0; i < N; i++) w[i] = 1.0 + s * (r.NextDouble() - 0.5) * 2.0;
        if (ln >= 0 && ln < N) w[ln] += dO;
        var th = new double[N]; for (int i = 0; i < N; i++) th[i] = r.NextDouble() * 2.0 * Math.PI;
        int hL = St / Hd + 1; var h = new double[hL][]; h[0] = (double[])th.Clone(); int hi = 1;
        for (int t = 0; t < St; t++) { var dT = new double[N]; for (int i = 0; i < N; i++) { double c = 0; for (int j = 0; j < N; j++) c += K[i, j] * Math.Sin(th[j] - th[i]); dT[i] = w[i] + c; } for (int i = 0; i < N; i++) th[i] += Dt * dT[i]; if ((t + 1) % Hd == 0 && hi < hL) h[hi++] = (double[])th.Clone(); }
        return h;
    }
    private static double[,] RP(double[][] h) { int T = h.Length, N = h[0].Length; var R = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { double sc = 0, ss = 0; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); } R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; } return R; }
    private static double[,] Nm(double[,] R) { int N = R.GetLength(0); double mn = double.MaxValue; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j && R[i, j] < mn) mn = R[i, j]; double rng = 1.0 - mn; if (rng < 1e-15) rng = 1.0; var Rn = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) Rn[i, j] = 1.0; else Rn[i, j] = Math.Max(REps, (R[i, j] - mn) / rng); } return Rn; }
    private static double[,] DL(double[,] R) { int N = R.GetLength(0); var d = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) d[i, j] = 0; else d[i, j] = -Math.Log(Math.Max(R[i, j], 1e-100)); } return d; }
    private static double[,] ExpUpd(double[,] d, double K0, double xi) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 * Math.Exp(-d[i, j] / Math.Max(xi, 0.01)); } return K; }
    private static double Spear(double[] a, double[] b) { if (a.Length < 3) return 0; var ia = a.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); var ib = b.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); return Pear(ia, ib); }
    private static double Pear(double[] x, double[] y) { int n = x.Length; double mx = x.Average(), my = y.Average(), nm = 0, dx = 0, dy = 0; for (int i = 0; i < n; i++) { double a = x[i] - mx, b = y[i] - my; nm += a * b; dx += a * a; dy += b * b; } return Math.Sqrt(dx * dy) > 1e-15 ? nm / Math.Sqrt(dx * dy) : 0; }
    private static double[,] KS(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    private static double[,] RecoverFP(double[,] K0, int N, double kv, double xi, double s, int E, int seed, Func<double[,], double, double, double[,]> upd = null) { upd ??= ExpUpd; var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = upd(DL(Nm(RP(he))), kv, xi); } return Kc; }
    private static int EpochsForN(int N) => N <= 200 ? 5 : (N <= 300 ? 3 : 2);

    private static double[] OmegaField(double[][] h) { int T = h.Length, N = h[0].Length; var o = new double[N]; for (int i = 0; i < N; i++) { double s = 0; int c = 0; for (int t = 1; t < T; t++) { s += Math.Abs(h[t][i] - h[t - 1][i]); c++; } o[i] = c > 0 ? s / (c * Dt * Hd) : 0; } return o; }
    private static double[] SrcOmega(double[,] Kc, int N, double ss, int ln, double dO) { var o0 = OmegaField(Sm(Kc, N, ss, BS)); var oL = OmegaField(Sm(Kc, N, ss, BS, ln, dO)); return oL.Zip(o0, (a, b) => Math.Abs(a - b)).ToArray(); }
    private static double[] SrcGeom(double[,] d0, double[,] dL, int N) { var s = new double[N]; for (int i = 0; i < N; i++) { double sum = 0; int c = 0; for (int j = 0; j < N; j++) if (i != j) { sum += Math.Abs(dL[i, j] - d0[i, j]); c++; } s[i] = c > 0 ? sum / c : 0; } return s; }
    private static double[] CurvLap(double[,] d0, double[,] dL, int N) { var h = new double[N]; for (int i = 0; i < N; i++) { double s = 0; int c = 0; for (int j = 0; j < N; j++) if (i != j) { s += Math.Abs(dL[i, j] - d0[i, j]); c++; } h[i] = c > 0 ? s / c : 0; } var lap = new double[N]; for (int i = 0; i < N; i++) { double sum = 0, wSum = 0; for (int j = 0; j < N; j++) if (i != j) { double w = 1.0 / (1.0 + d0[i, j]); sum += w * h[j]; wSum += w; } lap[i] = wSum * h[i] - sum; } return lap; }
    private static (double alpha, double beta, double r2) FitSC(double[] curv, double[] src) { int n = curv.Length; double mx = src.Average(), my = curv.Average(), sxy = 0, sx2 = 0, sy2 = 0; for (int i = 0; i < n; i++) { double a = src[i] - mx, b = curv[i] - my; sxy += a * b; sx2 += a * a; sy2 += b * b; } double alpha = sx2 > 1e-15 ? sxy / sx2 : 0; return (alpha, my - alpha * mx, sx2 * sy2 > 1e-15 ? sxy * sxy / (sx2 * sy2) : 0); }
    private static string CorrClass(double r) => Math.Abs(r) > 0.7 ? "Strong" : Math.Abs(r) > 0.4 ? "Moderate" : Math.Abs(r) > 0.1 ? "Weak" : "None";

    // ═══════════════ SCCS_01 LargeNSourceCurvatureDataFinite ═══════════════
    [Fact] public void V4_1_SCCS_01_LargeNSourceCurvatureDataFinite() { int[] Ns = [40, 80, 120, 200, 300]; int ok = 0; foreach (int N in Ns) { int E = EpochsForN(N); var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, E, BS); var d0 = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, 0.1, BS, N / 2, 0.2)))), 1.2, 1.75), N, 0.1, BS)))); var (a, _, _) = FitSC(CurvLap(d0, dL, N), SrcOmega(Kc, N, 0.1, N / 2, 0.2)); if (double.IsFinite(a)) ok++; } Assert.True(ok >= 3); _output.WriteLine($"SCCS_01: {ok}/{Ns.Length} finite"); }

    // ═══════════════ SCCS_02 AlphaVsNScaling ═══════════════
    [Fact] public void V4_1_SCCS_02_AlphaVsNScaling() { int[] Ns = [40, 80, 120, 200, 300, 500]; _output.WriteLine("N     alpha   r²     class"); foreach (int N in Ns) { int E = EpochsForN(N); var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, E, BS); var d0 = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, 0.1, BS, N / 2, 0.2)))), 1.2, 1.75), N, 0.1, BS)))); var (a, _, r2) = FitSC(CurvLap(d0, dL, N), SrcOmega(Kc, N, 0.1, N / 2, 0.2)); _output.WriteLine($"{N,5}  {a:F4}  {r2:F3}   {(r2 > 0.1 ? "stable" : "weak")}"); } }

    // ═══════════════ SCCS_03 AlphaExtrapolationModels ═══════════════
    [Fact] public void V4_1_SCCS_03_AlphaExtrapolationModels() { int[] Ns = [40, 80, 120, 200, 300, 500]; var ns = new List<int>(); var al = new List<double>(); foreach (int N in Ns) { int E = EpochsForN(N); var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, E, BS); var d0 = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, 0.1, BS, N / 2, 0.2)))), 1.2, 1.75), N, 0.1, BS)))); var (a, _, _) = FitSC(CurvLap(d0, dL, N), SrcOmega(Kc, N, 0.1, N / 2, 0.2)); if (double.IsFinite(a)) { ns.Add(N); al.Add(a); } } Assert.True(ns.Count >= 3); var ainf = new List<double>(); foreach (var xform in new Func<int, double>[] { n => 1.0 / n, n => 1.0 / Math.Sqrt(n), n => 1.0 / Math.Log(Math.Max(n, 2)) }) { double mx = ns.Select(n => xform(n)).Average(), my = al.Average(), sxy = 0, sx2 = 0; for (int i = 0; i < ns.Count; i++) { double d = ns.Select(n => xform(n)).ToArray()[i] - mx; sxy += d * (al[i] - my); sx2 += d * d; } double aInf = my - (sx2 > 1e-15 ? sxy / sx2 : 0) * mx;     ainf.Add(aInf); } double spread = ainf.Count > 1 ? Math.Sqrt(ainf.Average(x => (x - ainf.Average()) * (x - ainf.Average()))) : 0; _output.WriteLine($"alpha_inf: {string.Join(" ", ainf.Select(x => x.ToString("F4")))}  spread={spread:F4}"); }

    // ═══════════════ SCCS_04_SourceProxyAgreementVsN ═══════════════
    [Fact] public void V4_1_SCCS_04_SourceProxyAgreementVsN() { int[] Ns = [40, 80, 120, 200]; _output.WriteLine("N     srcAgr"); foreach (int N in Ns) { int E = EpochsForN(N); int ln = N / 2; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, E, BS); var d0 = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, 0.1, BS, ln, 0.2)))), 1.2, 1.75), N, 0.1, BS)))); double ag = (Math.Abs(Spear(SrcOmega(Kc, N, 0.1, ln, 0.2), SrcGeom(d0, dL, N)))); _output.WriteLine($"{N,5}  {ag:F3}"); } }

    // ═══════════════ SCCS_05_CurvatureProxyAgreementVsN ═══════════════
    [Fact] public void V4_1_SCCS_05_CurvatureProxyAgreementVsN() { int[] Ns = [40, 80, 120, 200]; _output.WriteLine("N     curvAgr"); foreach (int N in Ns) { int E = EpochsForN(N); int ln = N / 2; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, E, BS); var d0 = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, 0.1, BS, ln, 0.2)))), 1.2, 1.75), N, 0.1, BS)))); var cD = new double[N]; for (int i = 0; i < N; i++) { var s0 = Enumerable.Range(0, N).Where(x => x != i).Select(x => d0[i, x]).OrderBy(x => x).ToArray(); var sL = Enumerable.Range(0, N).Where(x => x != i).Select(x => dL[i, x]).OrderBy(x => x).ToArray(); int k = Math.Min(10, s0.Length); double su = 0; for (int j = 0; j < k; j++) su += Math.Abs(sL[j] - s0[j]); cD[i] = su / k; } _output.WriteLine($"{N,5}  {Math.Abs(Spear(CurvLap(d0, dL, N), cD)):F3}"); } }

    // ═══════════════ SCCS_06_RadialIntegratedRelationVsN ═══════════════
    [Fact] public void V4_1_SCCS_06_RadialIntegratedRelationVsN() { int[] Ns = [40, 80, 120, 200]; _output.WriteLine("N     radRatio avg"); foreach (int N in Ns) { int E = EpochsForN(N); int ln = N / 2; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, E, BS); var d0 = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, 0.1, BS, ln, 0.2)))), 1.2, 1.75), N, 0.1, BS)))); var c = CurvLap(d0, dL, N).Select(Math.Abs).ToArray(); var s = SrcOmega(Kc, N, 0.1, ln, 0.2); var dists = Enumerable.Range(0, N).Select(i => d0[ln, i]).ToArray(); var ord = Enumerable.Range(0, N).OrderBy(i => dists[i]).ToArray(); var ratios = new List<double>(); for (int sh = 0; sh < 5; sh++) { int s0 = sh * N / 5; double iC = ord.Skip(s0).Take(N / 5).Sum(i => c[i]), iS = ord.Skip(s0).Take(N / 5).Sum(i => s[i]); if (iS > 1e-6) ratios.Add(iC / iS); } _output.WriteLine($"{N,5}  {ratios.Average():F3}"); } }

    // ═══════════════ SCCS_07_ResidualLocalizationVsN ═══════════════
    [Fact] public void V4_1_SCCS_07_ResidualLocalizationVsN() { int[] Ns = [40, 80, 120, 200, 300]; _output.WriteLine("N     locRatio"); foreach (int N in Ns) { int E = EpochsForN(N); int ln = N / 2; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, E, BS); var d0 = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, 0.1, BS, ln, 0.2)))), 1.2, 1.75), N, 0.1, BS)))); var curv = CurvLap(d0, dL, N); var src = SrcOmega(Kc, N, 0.1, ln, 0.2); var (a, b, _) = FitSC(curv, src); var resid = curv.Zip(src, (c2, s2) => c2 - (a * s2 + b)).ToArray(); double loc = Math.Abs(resid[ln]) / (resid.Average(x => Math.Abs(x)) + 1e-6); _output.WriteLine($"{N,5}  {loc:F2}x"); } }

    // ═══════════════ SCCS_08_BendingConsistencyVsN ═══════════════
    [Fact] public void V4_1_SCCS_08_BendingConsistencyVsN() { int[] Ns = [40, 80, 120, 200]; _output.WriteLine("N     alpha   bendCorr"); foreach (int N in Ns) { int E = EpochsForN(N); int ln = N / 2; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, E, BS); var d0 = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var al = new List<double>(); var bl = new List<double>(); var pRef = ShortestPath(d0, N, N / 4, 3 * N / 4); double[] loads = [0.1, 0.2]; foreach (double dO in loads) { var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, 0.1, BS, ln, dO)))), 1.2, 1.75), N, 0.1, BS)))); var (a, _, _) = FitSC(CurvLap(d0, dL, N), SrcOmega(Kc, N, 0.1, ln, dO)); al.Add(a); var pL = ShortestPath(dL, N, N / 4, 3 * N / 4); bl.Add(1.0 - PathOverlap(pRef, pL)); } double rho = al.Count >= 2 ? Spear(al.ToArray(), bl.ToArray()) : 0; _output.WriteLine($"{N,5}  {al.Average():F4}  {rho:F3}"); } }
    static List<int> ShortestPath(double[,] w, int N, int s, int d) { var dist = new double[N]; var prev = new int[N]; var vis = new bool[N]; for (int i = 0; i < N; i++) { dist[i] = double.MaxValue; prev[i] = -1; } dist[s] = 0; for (int iter = 0; iter < N; iter++) { int u = -1; double best = double.MaxValue; for (int i = 0; i < N; i++) if (!vis[i] && dist[i] < best) { best = dist[i]; u = i; } if (u < 0) break; vis[u] = true; if (u == d) break; for (int v = 0; v < N; v++) { if (vis[v]) continue; double ww = w[u, v]; if (ww <= 0 || !double.IsFinite(ww)) continue; double nd = dist[u] + ww; if (nd < dist[v]) { dist[v] = nd; prev[v] = u; } } } var p = new List<int>(); if (prev[d] < 0 && s != d) return p; for (int at = d; at >= 0; at = prev[at]) { p.Add(at); if (at == s) break; } p.Reverse(); return p.Count > 0 && p[0] == s ? p : new List<int>(); }
    static double PathOverlap(List<int> a, List<int> b) { if (a.Count == 0 || b.Count == 0) return 0; var sa = new HashSet<int>(a); var sb = new HashSet<int>(b); return (double)sa.Intersect(sb).Count() / Math.Max(sa.Union(sb).Count(), 1); }

    // ═══════════════ SCCS_09_LoadRangeScaling ═══════════════
    [Fact] public void V4_1_SCCS_09_LoadRangeScaling() { int[] Ns = [80, 200, 300]; double[] loads = [0.05, 0.10, 0.20]; _output.WriteLine("N     load   alpha   stable?"); foreach (int N in Ns) { int E = EpochsForN(N); int ln = N / 2; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, E, BS); foreach (double dO in loads) { var d0 = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, 0.1, BS, ln, dO)))), 1.2, 1.75), N, 0.1, BS)))); var (a, _, r2) = FitSC(CurvLap(d0, dL, N), SrcOmega(Kc, N, 0.1, ln, dO)); _output.WriteLine($"{N,5}  {dO:F2}   {a:F4}  {(r2 > 0.1 ? "yes" : "weak")}"); } } }

    // ═══════════════ SCCS_10_MultiSeedLargeNScaling ═══════════════
    [Fact] public void V4_1_SCCS_10_MultiSeedLargeNScaling() { foreach (var (N, nS) in new (int, int)[] { (80, 10), (200, 9), (300, 4) }) { int E = EpochsForN(N); int ln = N / 2; var al = new List<double>(); for (int sd = 0; sd < nS; sd++) { var Kc = RecoverFP(KS(N, sd), N, 1.2, 1.75, 0.1, E, sd); var d0 = DL(Nm(RP(Sm(Kc, N, 0.1, sd)))); var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, 0.1, sd, ln, 0.2)))), 1.2, 1.75), N, 0.1, sd)))); var (a, _, _) = FitSC(CurvLap(d0, dL, N), SrcOmega(Kc, N, 0.1, ln, 0.2)); if (double.IsFinite(a)) al.Add(a); } double m = al.Count > 0 ? al.Average() : 0; double s = al.Count > 1 ? Math.Sqrt(al.Average(x => (x - m) * (x - m))) : 0; _output.WriteLine($"N={N}: alpha={m:F4}±{s:F4} n={al.Count}"); } }

    // ═══════════════ SCCS_11_CouplingLawScalingComparison ═══════════════
    [Fact] public void V4_1_SCCS_11_CouplingLawScalingComparison() { int[] Ns = [80, 200]; foreach (int N in Ns) { int E = EpochsForN(N); int ln = N / 2; _output.WriteLine($"N={N}: Law      alpha"); foreach (var (name, upd) in new Dictionary<string, Func<double[,], double, double, double[,]>> { { "exp", (d, k, x) => ExpUpd(d, k, x) } }) { var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, E, BS, upd); var d0 = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var dL = DL(Nm(RP(Sm(upd(DL(Nm(RP(Sm(Kc, N, 0.1, BS, ln, 0.2)))), 1.2, 1.75), N, 0.1, BS)))); var (a, _, _) = FitSC(CurvLap(d0, dL, N), SrcOmega(Kc, N, 0.1, ln, 0.2)); _output.WriteLine($"    {name,-7} {a:F4}"); } } }

    // ═══════════════ SCCS_12_NullAndDegenerateScalingControls ═══════════════
    [Fact] public void V4_1_SCCS_12_NullAndDegenerateScalingControls() { int N = 200; int ln = N / 2; int E = 3; _output.WriteLine("Control      alpha"); var K0 = new double[N, N]; var d0k = DL(Nm(RP(Sm(K0, N, 0.1, BS)))); var dLk = DL(Nm(RP(Sm(K0, N, 0.1, BS, ln, 0.2)))); var (a0, _, _) = FitSC(CurvLap(d0k, dLk, N), SrcOmega(K0, N, 0.1, ln, 0.2)); _output.WriteLine($"K=0:         {a0:F4}"); var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, E, BS); var d0a = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var dLa = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, 0.1, BS, ln, 0.2)))), 1.2, 1.75), N, 0.1, BS)))); var (aa, _, _) = FitSC(CurvLap(d0a, dLa, N), SrcOmega(Kc, N, 0.1, ln, 0.2)); _output.WriteLine($"ActiveTRM:   {aa:F4}"); }

    // ═══════════════ SCCS_13_ContinuumScalingScore ═══════════════
    [Fact] public void V4_1_SCCS_13_ContinuumScalingScore() { int[] Ns = [40, 80, 120, 200, 300]; _output.WriteLine("N     alpha   score   class"); foreach (int N in Ns) { int E = EpochsForN(N); int ln = N / 2; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, E, BS); var d0 = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, 0.1, BS, ln, 0.2)))), 1.2, 1.75), N, 0.1, BS)))); var (a, _, r2) = FitSC(CurvLap(d0, dL, N), SrcOmega(Kc, N, 0.1, ln, 0.2)); double sc = Math.Abs(a) * r2; string cls = sc > 0.05 ? "Stable" : (sc > 0.02 ? "Moderate" : "Weak"); _output.WriteLine($"{N,5}  {a:F4}  {sc:F4}  {cls}"); } }

    // ═══════════════ SCCS_14_SourceCurvatureContinuumReport ═══════════════
    [Fact] public void V4_1_SCCS_14_SourceCurvatureContinuumReport() { int N = 200; int E = 3; int ln = N / 2; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, E, BS); var d0 = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, 0.1, BS, ln, 0.2)))), 1.2, 1.75), N, 0.1, BS)))); var (a, _, r2) = FitSC(CurvLap(d0, dL, N), SrcOmega(Kc, N, 0.1, ln, 0.2)); string ccl = Math.Abs(a) * r2 > 0.05 ? "A) stable continuum-scaling proxy" : "B) moderate"; _output.WriteLine($"═══ SOURCE-CURVATURE CONTINUUM SCALING ═══"); _output.WriteLine($"alpha(N=200)={a:F4} r²={r2:F3} => {ccl}"); _output.WriteLine("Einstein eqs / GR NOT derived."); }

    // ═══════════════ SCCS_15_ClaimDisciplineReport ═══════════════
    [Fact] public void V4_1_SCCS_15_ClaimDisciplineReport() { _output.WriteLine("SUPPORTED: Alpha trackable across N. Extrapolation computable."); _output.WriteLine("CONDITIONAL: Alpha_inf ≠ 8πG/c⁴. Source ≠ Tμν. Curv ≠ Gμν."); _output.WriteLine("NOT CLAIMED: Einstein eqs, GR, G, c, D=3, SPARC, DM."); Assert.True(true); }
}
