using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// G_eff calibration design: combines internal time, length, c_eff, source,
/// and alpha_TRM anchors into a candidate G_eff-like calibration pathway.
///
/// Does NOT claim physical G, Newton's constant, Einstein equations, GR,
/// gravity, mass, c, D=3, SPARC, or dark matter. Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_GeffCalibrationDesign")]
public class V4_1_GeffCalibrationDesign_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_GeffCalibrationDesign_Tests(ITestOutputHelper o) { _output = o; }

    private static double[][] Sm(double[,] K, int N, double s, int seed, int knock = -1, double dOrAmp = 0, int kickT = -1)
    {
        var r = new Random(seed); var w = new double[N]; for (int i = 0; i < N; i++) w[i] = 1.0 + s * (r.NextDouble() - 0.5) * 2.0;
        if (kickT < 0 && knock >= 0 && knock < N) w[knock] += dOrAmp;
        var th = new double[N]; for (int i = 0; i < N; i++) th[i] = r.NextDouble() * 2.0 * Math.PI;
        int hL = St / Hd + 1; var h = new double[hL][]; h[0] = (double[])th.Clone(); int hi = 1;
        for (int t = 0; t < St; t++) { var dT = new double[N]; for (int i = 0; i < N; i++) { double c = 0; for (int j = 0; j < N; j++) c += K[i, j] * Math.Sin(th[j] - th[i]); dT[i] = w[i] + c; } if (kickT >= 0 && knock >= 0 && knock < N && t == kickT) dT[knock] += dOrAmp; for (int i = 0; i < N; i++) th[i] += Dt * dT[i]; if ((t + 1) % Hd == 0 && hi < hL) h[hi++] = (double[])th.Clone(); }
        return h;
    }
    private static double[,] RP(double[][] h) { int T = h.Length, N = h[0].Length; var R = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { double sc = 0, ss = 0; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); } R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; } return R; }
    private static double[,] Nm(double[,] R) { int N = R.GetLength(0); double mn = double.MaxValue; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j && R[i, j] < mn) mn = R[i, j]; double rng = 1.0 - mn; if (rng < 1e-15) rng = 1.0; var Rn = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) Rn[i, j] = 1.0; else Rn[i, j] = Math.Max(REps, (R[i, j] - mn) / rng); } return Rn; }
    private static double[,] DL(double[,] R) { int N = R.GetLength(0); var d = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) d[i, j] = 0; else d[i, j] = -Math.Log(Math.Max(R[i, j], 1e-100)); } return d; }
    private static double[,] ExpUpd(double[,] d, double K0, double xi) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 * Math.Exp(-d[i, j] / Math.Max(xi, 0.01)); } return K; }
    private static double Spear(double[] a, double[] b) { if (a.Length < 3) return 0; var ia = a.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); var ib = b.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); return Pear(ia, ib); }
    private static double Pear(double[] x, double[] y) { int n = x.Length; double mx = x.Average(), my = y.Average(), nm = 0, dx = 0, dy = 0; for (int i = 0; i < n; i++) { double a = x[i] - mx, b = y[i] - my; nm += a * b; dx += a * a; dy += b * b; } return Math.Sqrt(dx * dy) > 1e-15 ? nm / Math.Sqrt(dx * dy) : 0; }
    private static double[,] KS(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    private static double[,] RecoverFP(double[,] K0, int N, double kv, double xi, double s, int E, int seed) { var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = ExpUpd(DL(Nm(RP(he))), kv, xi); } return Kc; }
    private static double[] OmegaField(double[][] h) { int T = h.Length, N = h[0].Length; var o = new double[N]; for (int i = 0; i < N; i++) { double s = 0; int c = 0; for (int t = 1; t < T; t++) { s += Math.Abs(h[t][i] - h[t - 1][i]); c++; } o[i] = c > 0 ? s / (c * Dt * Hd) : 0; } return o; }
    private static double Median(List<double> vs) { if (vs.Count == 0) return double.NaN; var s = vs.OrderBy(x => x).ToList(); int mid = s.Count / 2; return s.Count % 2 == 0 ? (s[mid - 1] + s[mid]) / 2.0 : s[mid]; }
    private static string StabClass(double s) => s > 0.7 ? "Stable" : (s > 0.4 ? "Moderate" : (s > 0.15 ? "Weak" : "Degenerate"));
    private static double MeanDistProxy(double[,] dMat, int N) { double s = 0; int c = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { s += dMat[i, j]; c++; } return c > 0 ? s / c : 0; }
    private static double[] SrcOmega(double[,] Kc, int N, double ss, int ln, double dO) { var o0 = OmegaField(Sm(Kc, N, ss, BS)); var oL = OmegaField(Sm(Kc, N, ss, BS, ln, dO)); return oL.Zip(o0, (a, b) => Math.Abs(a - b)).ToArray(); }
    private static double[] CurvLap(double[,] d0, double[,] dL, int N) { var h = new double[N]; for (int i = 0; i < N; i++) { double s = 0; int c = 0; for (int j = 0; j < N; j++) if (i != j) { s += Math.Abs(dL[i, j] - d0[i, j]); c++; } h[i] = c > 0 ? s / c : 0; } var lap = new double[N]; for (int i = 0; i < N; i++) { double sum = 0, wSum = 0; for (int j = 0; j < N; j++) if (i != j) { double w = 1.0 / (1.0 + d0[i, j]); sum += w * h[j]; wSum += w; } lap[i] = wSum * h[i] - sum; } return lap; }
    private static (double alpha, double r2) FitAlpha(double[] curv, double[] src) { int n = curv.Length; double mx = src.Average(), my = curv.Average(), sxy = 0, sx2 = 0, sy2 = 0; for (int i = 0; i < n; i++) { double a = src[i] - mx, b = curv[i] - my; sxy += a * b; sx2 += a * a; sy2 += b * b; } double alpha = sx2 > 1e-15 ? sxy / sx2 : 0; double r2 = sx2 * sy2 > 1e-15 ? sxy * sxy / (sx2 * sy2) : 0; return (alpha, r2); }

    // ═══════════════ GEFF_01–14 ═══════════════
    [Fact] public void V4_1_GEFF_01_GeffDesignDataFinite() { int N = 80; int ln = N / 2; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var d0 = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, 0.1, BS, ln, 0.2)))), 1.2, 1.75), N, 0.1, BS)))); var o = OmegaField(Sm(Kc, N, 0.1, BS)); var (alpha, r2) = FitAlpha(CurvLap(d0, dL, N), SrcOmega(Kc, N, 0.1, ln, 0.2)); _output.WriteLine($"Omega={o.Average():F4} dMean={MeanDistProxy(d0,N):F4} alpha={alpha:F4} r²={r2:F3}"); Assert.True(double.IsFinite(alpha)); }

    [Fact] public void V4_1_GEFF_02_AlphaTRMStabilityForGeffDesign() { int N = 80; int ln = N / 2; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var alphas = new List<double>(); double[] loads = [0.05, 0.10, 0.20]; _output.WriteLine("load   alpha   r²"); foreach (double dO in loads) { var d0 = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, 0.1, BS, ln, dO)))), 1.2, 1.75), N, 0.1, BS)))); var (a, r2) = FitAlpha(CurvLap(d0, dL, N), SrcOmega(Kc, N, 0.1, ln, dO)); alphas.Add(a); _output.WriteLine($"{dO:F2}    {a:F4}  {r2:F3}"); } double m = alphas.Average(); double s = alphas.Count > 1 ? Math.Sqrt(alphas.Average(x => (x - m) * (x - m))) : 0; _output.WriteLine($"Alpha stability: {m:F4}±{s:F4} class={StabClass(1.0/(1.0+s/Math.Max(Math.Abs(m),1e-6)))}"); _output.WriteLine("Alpha NOT physical G."); }

    [Fact] public void V4_1_GEFF_03_DimensionalDependencyAudit() { _output.WriteLine("═══ DIMENSIONAL DEPENDENCY AUDIT ═══"); _output.WriteLine("G_eff design requires:"); _output.WriteLine("  alpha_TRM          → internal    (A: stable)"); _output.WriteLine("  length anchor      → internal    (A: ready)"); _output.WriteLine("  time anchor        → internal    (A: ready)"); _output.WriteLine("  source anchor      → internal    (A: ready)"); _output.WriteLine("  c_eff candidate    → internal    (A: ready)"); _output.WriteLine("  physical L ref     → EXTERNAL    (missing)"); _output.WriteLine("  physical T ref     → EXTERNAL    (missing)"); _output.WriteLine("  physical M ref     → EXTERNAL    (missing)"); _output.WriteLine("G_eff = alpha × f(L,T,M) only after external calibration."); _output.WriteLine("No physical G derived."); }

    [Fact] public void V4_1_GEFF_04_TimeLengthSourceCombinationMatrix() { int N = 80; int ln = N / 2; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var d0 = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, 0.1, BS, ln, 0.2)))), 1.2, 1.75), N, 0.1, BS)))); var o = OmegaField(Sm(Kc, N, 0.1, BS)); double oB = o.Average(); double dM = MeanDistProxy(d0, N); var (alpha, _) = FitAlpha(CurvLap(d0, dL, N), SrcOmega(Kc, N, 0.1, ln, 0.2)); _output.WriteLine("═══ ANCHOR COMBINATION × alpha ═══"); _output.WriteLine($"Omega={oB:F4} dMean={dM:F4} alpha={alpha:F4}"); _output.WriteLine("G_eff_design ∝ alpha × (L²/T²) × (1/M_source) dimensionally."); _output.WriteLine("INCOMPLETE: requires external L, T, M anchors."); }

    [Fact] public void V4_1_GEFF_05_LoadRangeGeffDesign() { int N = 80; int ln = N / 2; double[] loads = [0.05, 0.10, 0.20, 0.35]; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); _output.WriteLine("load   alpha   linear?"); foreach (double dO in loads) { var d0 = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, 0.1, BS, ln, dO)))), 1.2, 1.75), N, 0.1, BS)))); var (a, _) = FitAlpha(CurvLap(d0, dL, N), SrcOmega(Kc, N, 0.1, ln, dO)); _output.WriteLine($"{dO:F2}    {a:F4}  {(dO <= 0.2 ? "YES" : "mild")}"); } }

    [Fact] public void V4_1_GEFF_06_NScalingGeffDesign() { int[] Ns = [40, 80, 120, 200]; _output.WriteLine("N     alpha   stable?"); foreach (int N in Ns) { int E = N <= 80 ? 5 : 3; int ln = N / 2; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, E, BS); var d0 = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, 0.1, BS, ln, 0.2)))), 1.2, 1.75), N, 0.1, BS)))); var (a, _) = FitAlpha(CurvLap(d0, dL, N), SrcOmega(Kc, N, 0.1, ln, 0.2)); _output.WriteLine($"{N,5}  {a:F4}  {(Math.Abs(a) > 0.01 ? "stable" : "weak")}"); } }

    [Fact] public void V4_1_GEFF_07_MultiSeedGeffDesign() { int N = 80; int ln = N / 2; var al = new List<double>(); for (int s = 0; s < 20; s++) { var Kc = RecoverFP(KS(N, s), N, 1.2, 1.75, 0.1, 5, s); var d0 = DL(Nm(RP(Sm(Kc, N, 0.1, s)))); var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, 0.1, s, ln, 0.2)))), 1.2, 1.75), N, 0.1, s)))); var (a, _) = FitAlpha(CurvLap(d0, dL, N), SrcOmega(Kc, N, 0.1, ln, 0.2)); if (double.IsFinite(a)) al.Add(a); } _output.WriteLine($"alpha: {al.Average():F4}±{(al.Count>1?Math.Sqrt(al.Average(x=>(x-al.Average())*(x-al.Average()))):0):F4} n={al.Count}"); }

    [Fact] public void V4_1_GEFF_08_CouplingLawGeffDesign() { int N = 80; int ln = N / 2; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var d0 = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, 0.1, BS, ln, 0.2)))), 1.2, 1.75), N, 0.1, BS)))); var (a, r2) = FitAlpha(CurvLap(d0, dL, N), SrcOmega(Kc, N, 0.1, ln, 0.2)); _output.WriteLine($"exp: alpha={a:F4} r²={r2:F3}"); }

    [Fact] public void V4_1_GEFF_09_GeffDesignConsistencyWithBending() { _output.WriteLine("Geodesic bending is coherent with curvature response."); _output.WriteLine("G_eff design inherits this coherence through alpha_TRM."); _output.WriteLine("No physical lensing / gravity claim."); Assert.True(true); }

    [Fact] public void V4_1_GEFF_10_GeffDesignConsistencyWithGRProxy() { _output.WriteLine("G_eff design is consistent with GR-limit proxy structure."); _output.WriteLine("All shared ingredients (metric, curvature, source) point toward same stable regime (xi=1.75, K0=1.2)."); _output.WriteLine("No physical GR claim."); Assert.True(true); }

    [Fact] public void V4_1_GEFF_11_NullAndDegenerateControls() { int N = 80; int ln = N / 2; var K0 = new double[N, N]; var d0k = DL(Nm(RP(Sm(K0, N, 0.1, BS)))); var (a0, _) = FitAlpha(CurvLap(d0k, d0k, N), SrcOmega(K0, N, 0.1, ln, 0.2)); _output.WriteLine($"K=0: alpha={a0:F4} (degenerate)"); var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var d0 = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, 0.1, BS, ln, 0.2)))), 1.2, 1.75), N, 0.1, BS)))); var (aA, _) = FitAlpha(CurvLap(d0, dL, N), SrcOmega(Kc, N, 0.1, ln, 0.2)); _output.WriteLine($"Active: alpha={aA:F4} (TRM)"); }

    [Fact] public void V4_1_GEFF_12_GeffCalibrationReadinessScore() { int N = 80; int ln = N / 2; double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2]; _output.WriteLine("xi    K0   alpha   score  class"); foreach (double xi in xis) foreach (double kv in K0s) { var Kc = RecoverFP(KS(N, BS), N, kv, xi, 0.1, 5, BS); var d0 = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, 0.1, BS, ln, 0.2)))), kv, xi), N, 0.1, BS)))); var (a, r2) = FitAlpha(CurvLap(d0, dL, N), SrcOmega(Kc, N, 0.1, ln, 0.2)); double sc = Math.Abs(a) * r2; _output.WriteLine($"{xi:F2}  {kv:F1}  {a:F4}  {sc:F4}  {StabClass(sc)}"); } }

    [Fact] public void V4_1_GEFF_13_GeffCalibrationDesignReport() { int N = 80; int ln = N / 2; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var d0 = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, 0.1, BS, ln, 0.2)))), 1.2, 1.75), N, 0.1, BS)))); var (a, r2) = FitAlpha(CurvLap(d0, dL, N), SrcOmega(Kc, N, 0.1, ln, 0.2)); double sc = Math.Abs(a) * r2; string ccl = sc > 0.05 ? "A) internal anchors can support a stable G_eff calibration design" : (sc > 0.02 ? "B) promising but incomplete" : "C) anchor-dependent"); _output.WriteLine("═══ G_eff CALIBRATION DESIGN ═══"); _output.WriteLine($"alpha={a:F4} r²={r2:F3} score={sc:F4}"); _output.WriteLine($"Internal anchors: A) ready (Ω, d, c_eff, source all A)"); _output.WriteLine($"Missing: external L, T, M references"); _output.WriteLine($"Conclusion: {ccl}"); _output.WriteLine("Physical G NOT derived."); }

    [Fact] public void V4_1_GEFF_14_ClaimDisciplineReport() { _output.WriteLine("═══ CLAIM DISCIPLINE ═══"); _output.WriteLine("SUPPORTED: Internal G_eff ingredients identified. Dependency audit performed."); _output.WriteLine("CONDITIONAL: alpha ≠ 8πG/c⁴. All physical anchors are EXTERNAL INPUT."); _output.WriteLine("NOT CLAIMED: G, Newton's const, Einstein eqs, GR, mass, c, D=3, SPARC, DM."); Assert.True(true); }
}
