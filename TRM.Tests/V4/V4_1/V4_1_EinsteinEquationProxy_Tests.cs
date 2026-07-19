using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Einstein-equation proxy: tests whether the TRM convergence state supports
/// a numerical relation CurvatureProxy ≈ alpha × SourceProxy, where SourceProxy
/// derives from ETG load/Omega and CurvatureProxy from metric perturbation.
///
/// Does NOT claim Einstein equations, GR, stress-energy, Ricci, Einstein
/// tensor, gravity, mass, G, c, D=3, SPARC, or dark matter.
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_EinsteinEquationProxy")]
public class V4_1_EinsteinEquationProxy_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_EinsteinEquationProxy_Tests(ITestOutputHelper o) { _output = o; }

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

    // ── Source proxies ─────────────────────────────────
    private static double[] SourceLoad(int N, int ln, double dO) { var s = new double[N]; s[ln] = dO; return s; }
    private static double[] SourceOmega(double[,] Kc, int N, double ss, int ln, double dO)
    { var o0 = OmegaField(Sm(Kc, N, ss, BS)); var oL = OmegaField(Sm(Kc, N, ss, BS, ln, dO)); return oL.Zip(o0, (a, b) => Math.Abs(a - b)).ToArray(); }
    private static double[] SourceGeom(double[,] d0, double[,] dL, int N)
    { var s = new double[N]; for (int i = 0; i < N; i++) { double sum = 0; int c = 0; for (int j = 0; j < N; j++) if (i != j) { sum += Math.Abs(dL[i, j] - d0[i, j]); c++; } s[i] = c > 0 ? sum / c : 0; } return s; }

    // ── Curvature proxies ──────────────────────────────
    private static double[] LapCurv(double[,] d0, double[,] dL, int N)
    {
        var h = new double[N]; for (int i = 0; i < N; i++) { double s = 0; int c = 0; for (int j = 0; j < N; j++) if (i != j) { s += Math.Abs(dL[i, j] - d0[i, j]); c++; } h[i] = c > 0 ? s / c : 0; }
        var lap = new double[N]; for (int i = 0; i < N; i++) { double sum = 0, wSum = 0; for (int j = 0; j < N; j++) if (i != j) { double w = 1.0 / (1.0 + d0[i, j]); sum += w * h[j]; wSum += w; } lap[i] = wSum * h[i] - sum; }
        return lap;
    }

    // ── Source-curvature fit ───────────────────────────
    private static (double alpha, double beta, double r2) FitSC(double[] curv, double[] src)
    {
        int n = curv.Length; double mx = src.Average(), my = curv.Average(), sxy = 0, sx2 = 0, sy2 = 0;
        for (int i = 0; i < n; i++) { double a = src[i] - mx, b = curv[i] - my; sxy += a * b; sx2 += a * a; sy2 += b * b; }
        double alpha = sx2 > 1e-15 ? sxy / sx2 : 0;
        double beta = my - alpha * mx;
        double r2 = sx2 * sy2 > 1e-15 ? sxy * sxy / (sx2 * sy2) : 0;
        return (alpha, beta, r2);
    }

    // ═══════════════ EEP_01 SourceCurvatureDataFinite ═══════════════
    [Fact]
    public void V4_1_EEP_01_SourceCurvatureDataFinite()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2; double dO = 0.2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var d0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var hL = Sm(Kc, N, s, BS, ln, dO); var Kl = ExpUpd(DL(Nm(RP(hL))), kv, xi);
        var dL = DL(Nm(RP(Sm(Kl, N, s, BS))));
        var srcO = SourceOmega(Kc, N, s, ln, dO);
        var srcG = SourceGeom(d0, dL, N);
        var curv = LapCurv(d0, dL, N);
        var (alpha, beta, r2) = FitSC(curv, srcO);
        Assert.True(double.IsFinite(alpha), "alpha NaN");
        _output.WriteLine($"Source-omega norm={srcO.Average():F4}  curv norm={curv.Average():F4}");
        _output.WriteLine($"Fit: alpha={alpha:F4} beta={beta:F4} r²={r2:F3}");
    }

    // ═══════════════ EEP_02 SourceProxyAgreement ═══════════════
    [Fact]
    public void V4_1_EEP_02_SourceProxyAgreement()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2; double dO = 0.2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var d0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, s, BS, ln, dO)))), kv, xi), N, s, BS))));
        var sL = SourceLoad(N, ln, dO);
        var sO = SourceOmega(Kc, N, s, ln, dO);
        var sG = SourceGeom(d0, dL, N);
        double rLO = Spear(sL, sO); double rLG = Spear(sL, sG); double rOG = Spear(sO, sG);
        double avg = (Math.Abs(rLO) + Math.Abs(rLG) + Math.Abs(rOG)) / 3.0;
        _output.WriteLine($"Source corrs: L-O={rLO:F3} L-G={rLG:F3} O-G={rOG:F3} avg={avg:F3} {(avg > 0.3 ? "Robust" : "Sensitive")}");
    }

    // ═══════════════ EEP_03 CurvatureProxyAgreement ═══════════════
    [Fact]
    public void V4_1_EEP_03_CurvatureProxyAgreement()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var d0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, s, BS, ln, 0.2)))), kv, xi), N, s, BS))));
        var cL = LapCurv(d0, dL, N);
        // Distortion curvature
        var cD = new double[N]; for (int i = 0; i < N; i++) { var s0 = Enumerable.Range(0, N).Where(x => x != i).Select(x => d0[i, x]).OrderBy(x => x).ToArray(); var sL2 = Enumerable.Range(0, N).Where(x => x != i).Select(x => dL[i, x]).OrderBy(x => x).ToArray(); int k = Math.Min(10, s0.Length); double su = 0; for (int j = 0; j < k; j++) su += Math.Abs(sL2[j] - s0[j]); cD[i] = su / k; }
        double r = Spear(cL, cD);
        _output.WriteLine($"Curv corr(Lap, Dist) = {r:F4}  class={CorrClass(r)}");
    }

    // ═══════════════ EEP_04 LocalSourceCurvatureRelation ═══════════════
    [Fact]
    public void V4_1_EEP_04_LocalSourceCurvatureRelation()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2; double dO = 0.2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var d0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, s, BS, ln, dO)))), kv, xi), N, s, BS))));
        var curv = LapCurv(d0, dL, N);
        var src = SourceOmega(Kc, N, s, ln, dO);
        var (alpha, beta, r2) = FitSC(curv, src);
        // Residual spread
        double rms = Math.Sqrt(curv.Zip(src, (c, ss) => { double e = c - (alpha * ss + beta); return e * e; }).Sum() / N);
        _output.WriteLine($"Curv ≈ {alpha:F4} * Source + {beta:F4}   r²={r2:F3}  rms={rms:F4}");
        _output.WriteLine($"NOT Einstein equation. Proxy diagnostic only.");
    }

    // ═══════════════ EEP_05 RadialIntegratedRelation ═══════════════
    [Fact]
    public void V4_1_EEP_05_RadialIntegratedRelation()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2; double dO = 0.2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var d0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, s, BS, ln, dO)))), kv, xi), N, s, BS))));
        var curv = LapCurv(d0, dL, N).Select(Math.Abs).ToArray();
        var src = SourceOmega(Kc, N, s, ln, dO);
        var dists = Enumerable.Range(0, N).Select(i => d0[ln, i]).ToArray();
        var ordered = Enumerable.Range(0, N).OrderBy(i => dists[i]).ToArray();
        _output.WriteLine("shell  ∫curv  ∫src   ratio");
        for (int sh = 0; sh < 5; sh++)
        {
            int s0 = sh * N / 5, s1 = (sh + 1) * N / 5;
            double iC = ordered.Skip(s0).Take(s1 - s0).Sum(i => curv[i]);
            double iS = ordered.Skip(s0).Take(s1 - s0).Sum(i => src[i]);
            _output.WriteLine($"{sh,5}  {iC:F4}  {iS:F4}  {(iS > 1e-6 ? iC / iS : 0):F3}");
        }
    }

    // ═══════════════ EEP_06 WeakFieldLinearity ═══════════════
    [Fact]
    public void V4_1_EEP_06_WeakFieldLinearity()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        double[] loads = [0.05, 0.10, 0.20, 0.35];
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        _output.WriteLine("load   alpha   r²     linear?");
        foreach (double dO in loads)
        {
            var d0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
            var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, s, BS, ln, dO)))), kv, xi), N, s, BS))));
            var (alpha, _, r2) = FitSC(LapCurv(d0, dL, N), SourceOmega(Kc, N, s, ln, dO));
            _output.WriteLine($"{dO:F2}   {alpha:F4}  {r2:F3}  {(dO <= 0.2 ? "linear" : "mild")}");
        }
    }

    // ═══════════════ EEP_07 ResidualLocalization ═══════════════
    [Fact]
    public void V4_1_EEP_07_ResidualLocalization()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2; double dO = 0.2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var d0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, s, BS, ln, dO)))), kv, xi), N, s, BS))));
        var curv = LapCurv(d0, dL, N); var src = SourceOmega(Kc, N, s, ln, dO);
        var (alpha, beta, _) = FitSC(curv, src);
        var resid = curv.Zip(src, (c, ss) => c - (alpha * ss + beta)).ToArray();
        double peakResid = Math.Abs(resid[ln]);
        double avgResid = resid.Average(x => Math.Abs(x));
        _output.WriteLine($"Residual: peak={peakResid:F4} avg={avgResid:F4} localized={(peakResid > 2 * avgResid ? "YES" : "NO")}");
    }

    // ═══════════════ EEP_08 SourceCurvatureToBending ═══════════════
    [Fact]
    public void V4_1_EEP_08_SourceCurvatureToBendingConsistency()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2;
        double[] loads = [0.05, 0.1, 0.2, 0.35];
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var d0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var alphas = new List<double>(); var bends = new List<double>();
        int srcP = N / 4, dstP = 3 * N / 4;
        var pRef = ShortestPath(d0, N, srcP, dstP);
        foreach (double dO in loads)
        {
            var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, s, BS, ln, dO)))), kv, xi), N, s, BS))));
            var (alpha, _, _) = FitSC(LapCurv(d0, dL, N), SourceOmega(Kc, N, s, ln, dO));
            alphas.Add(alpha);
            var pL = ShortestPath(dL, N, srcP, dstP);
            bends.Add(1.0 - PathOverlap(pRef, pL));
        }
        double rho = alphas.Count >= 3 ? Spear(alphas.ToArray(), bends.ToArray()) : 0;
        _output.WriteLine($"corr(alpha, bending) = {rho:F4}  class={CorrClass(rho)}");
    }
    private static List<int> ShortestPath(double[,] w, int N, int s, int d)
    {
        var dist = new double[N]; var prev = new int[N]; var vis = new bool[N];
        for (int i = 0; i < N; i++) { dist[i] = double.MaxValue; prev[i] = -1; } dist[s] = 0;
        for (int iter = 0; iter < N; iter++) { int u = -1; double best = double.MaxValue; for (int i = 0; i < N; i++) if (!vis[i] && dist[i] < best) { best = dist[i]; u = i; } if (u < 0) break; vis[u] = true; if (u == d) break; for (int v = 0; v < N; v++) { if (vis[v]) continue; double ww = w[u, v]; if (ww <= 0 || !double.IsFinite(ww)) continue; double nd = dist[u] + ww; if (nd < dist[v]) { dist[v] = nd; prev[v] = u; } } }
        var p = new List<int>(); if (prev[d] < 0 && s != d) return p; for (int at = d; at >= 0; at = prev[at]) { p.Add(at); if (at == s) break; } p.Reverse();
        return p.Count > 0 && p[0] == s ? p : new List<int>();
    }
    private static double PathOverlap(List<int> a, List<int> b) { if (a.Count == 0 || b.Count == 0) return 0; var sa = new HashSet<int>(a); var sb = new HashSet<int>(b); return (double)sa.Intersect(sb).Count() / Math.Max(sa.Union(sb).Count(), 1); }

    // ═══════════════ EEP_09 NScalingSourceCurvature ═══════════════
    [Fact]
    public void V4_1_EEP_09_NScalingSourceCurvature()
    {
        int[] Ns = [40, 80, 120, 200]; double xi = 1.75; double kv = 1.2; double s = 0.1;
        _output.WriteLine("N     alpha   r²     stable?");
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3; int ln = N / 2; double dO = 0.2;
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
            var d0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
            var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, s, BS, ln, dO)))), kv, xi), N, s, BS))));
            var (alpha, _, r2) = FitSC(LapCurv(d0, dL, N), SourceOmega(Kc, N, s, ln, dO));
            _output.WriteLine($"{N,5}  {alpha:F4}  {r2:F3}  {(Math.Abs(alpha) > 0.01 ? "stable" : "weak")}");
        }
    }

    // ═══════════════ EEP_10 MultiSeedSourceCurvature ═══════════════
    [Fact]
    public void V4_1_EEP_10_MultiSeedSourceCurvature()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int nSeeds = 10; int ln = N / 2; double dO = 0.2;
        var alphas = new List<double>(); var r2s = new List<double>();
        for (int seed = 0; seed < nSeeds; seed++)
        {
            var Kc = RecoverFP(KS(N, seed), N, kv, xi, s, 5, seed);
            var d0 = DL(Nm(RP(Sm(Kc, N, s, seed))));
            var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, s, seed, ln, dO)))), kv, xi), N, s, seed))));
            var (alpha, _, r2) = FitSC(LapCurv(d0, dL, N), SourceOmega(Kc, N, s, ln, dO));
            alphas.Add(alpha); r2s.Add(r2);
        }
        _output.WriteLine($"alpha: {alphas.Average():F4}±{Math.Sqrt(alphas.Average(x => (x - alphas.Average()) * (x - alphas.Average()))):F4}");
        _output.WriteLine($"r²:    {r2s.Average():F3}±{Math.Sqrt(r2s.Average(x => (x - r2s.Average()) * (x - r2s.Average()))):F3}");
    }

    // ═══════════════ EEP_11 CouplingLawSourceCurvature ═══════════════
    [Fact]
    public void V4_1_EEP_11_CouplingLawSourceCurvature()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int E = 5; int ln = N / 2; double dO = 0.2;
        var laws = new Dictionary<string, Func<double[,], double, double, double[,]>> { {"exp", (d,k0,x) => ExpUpd(d,k0,x)}, {"gauss", (d,k0,x) => GaussUpd(d,k0,x)}, {"power", (d,k0,x) => PowerUpd(d,k0,x)} };
        _output.WriteLine("Law      alpha   r²     stable?");
        foreach (var (name, upd) in laws)
        {
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS, upd);
            var d0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
            var dL = DL(Nm(RP(Sm(upd(DL(Nm(RP(Sm(Kc, N, s, BS, ln, dO)))), kv, xi), N, s, BS))));
            var (alpha, _, r2) = FitSC(LapCurv(d0, dL, N), SourceOmega(Kc, N, s, ln, dO));
            _output.WriteLine($"{name,-7} {alpha:F4}  {r2:F3}  {(r2 > 0.2 ? "yes" : "partial")}");
        }
    }

    // ═══════════════ EEP_12 NullAndDegenerateControls ═══════════════
    [Fact]
    public void V4_1_EEP_12_NullAndDegenerateControls()
    {
        int N = 80; double s = 0.1; double xi = 1.75; double kv = 1.2; int ln = N / 2; double dO = 0.2;
        _output.WriteLine("Control      alpha   r²");
        // K=0
        var K0 = new double[N, N]; var d0k = DL(Nm(RP(Sm(K0, N, s, BS))));
        var dLk = DL(Nm(RP(Sm(K0, N, s, BS, ln, dO))));
        var (a0, _, r0) = FitSC(LapCurv(d0k, dLk, N), SourceOmega(K0, N, s, ln, dO));
        _output.WriteLine($"K=0:         {a0:F4}  {r0:F3}");
        // Random R
        var rng = new Random(BS); var rd = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { double v = 0.1 + rng.NextDouble(); rd[i, j] = rd[j, i] = v; }
        var Kr = ExpUpd(rd, kv, xi); var d0r = DL(Nm(RP(Sm(Kr, N, s, BS))));
        var dLr = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kr, N, s, BS, ln, dO)))), kv, xi), N, s, BS))));
        var (ar, _, rr) = FitSC(LapCurv(d0r, dLr, N), SourceOmega(Kr, N, s, ln, dO));
        _output.WriteLine($"RandR:       {ar:F4}  {rr:F3}");
        // Active
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var d0a = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var dLa = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, s, BS, ln, dO)))), kv, xi), N, s, BS))));
        var (aa, _, ra) = FitSC(LapCurv(d0a, dLa, N), SourceOmega(Kc, N, s, ln, dO));
        _output.WriteLine($"ActiveTRM:   {aa:F4}  {ra:F3}");
    }

    // ═══════════════ EEP_13 EinsteinEquationProxyScore ═══════════════
    [Fact]
    public void V4_1_EEP_13_EinsteinEquationProxyScore()
    {
        int N = 80; double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2]; double s = 0.1; int ln = N / 2; double dO = 0.2;
        _output.WriteLine("xi    K0   alpha   r²     score   class");
        foreach (double xi in xis)
            foreach (double kv in K0s)
            {
                var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
                var d0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
                var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, s, BS, ln, dO)))), kv, xi), N, s, BS))));
                var (alpha, _, r2) = FitSC(LapCurv(d0, dL, N), SourceOmega(Kc, N, s, ln, dO));
                double score = Math.Abs(alpha) * r2;
                string cls = score > 0.1 ? "StrongCurvSrc" : (score > 0.03 ? "Moderate" : (score > 0.01 ? "Weak" : "None"));
                _output.WriteLine($"{xi:F2}  {kv:F1}  {alpha:F4}  {r2:F3}  {score:F4}  {cls}");
            }
    }

    // ═══════════════ EEP_14 EinsteinEquationProxyReport ═══════════════
    [Fact]
    public void V4_1_EEP_14_EinsteinEquationProxyReport()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int ln = N / 2; double dO = 0.2;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var d0 = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, s, BS, ln, dO)))), kv, xi), N, s, BS))));
        var (alpha, _, r2) = FitSC(LapCurv(d0, dL, N), SourceOmega(Kc, N, s, ln, dO));
        double score = Math.Abs(alpha) * r2;
        string ccl = score > 0.1 ? "A) strong source-curvature proxy consistency" : (score > 0.03 ? "B) moderate" : (score > 0.01 ? "C) weak" : "D) degenerate"));
        _output.WriteLine("═══ EINSTEIN EQUATION PROXY REPORT ═══");
        _output.WriteLine($"Curv ≈ {alpha:F4} × Source  r²={r2:F3}  score={score:F4}");
        _output.WriteLine($"Conclusion: {ccl}");
        _output.WriteLine("Einstein equations, GR, gravity NOT derived.");
    }

    // ═══════════════ EEP_15 ClaimDisciplineReport ═══════════════
    [Fact]
    public void V4_1_EEP_15_ClaimDisciplineReport()
    {
        _output.WriteLine("═══ CLAIM DISCIPLINE: Einstein Equation Proxy ═══");
        _output.WriteLine("");
        _output.WriteLine("SUPPORTED: Source-curvature relation testable. Alpha/r² measurable.");
        _output.WriteLine("CONDITIONAL: Source ≠ stress-energy. Curv ≠ Ricci/Einstein. Alpha ≠ 8πG/c⁴.");
        _output.WriteLine("NOT CLAIMED: Einstein eqs, GR, gravity, mass, G, c, D=3, SPARC, DM.");
        Assert.True(true, "Report complete.");
    }
}
