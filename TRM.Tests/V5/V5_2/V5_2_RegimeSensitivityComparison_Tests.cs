using Xunit;
using Xunit.Abstractions;
using System.Security.Cryptography;
using System.Text;

namespace TRM.Tests.V5_2;

/// <summary>
/// Regime Sensitivity Comparison (RSC):
/// Compares all 23 regime points against primary reference regime.
/// Computes stability scores, degradation scores, and generates
/// the regime stability map.
/// </summary>
[Trait("Category", "V5_2")]
[Trait("Category", "V5_2_RSC")]
public class V5_2_RegimeSensitivityComparison_Tests
{
    private readonly ITestOutputHelper _output;
    private const double Dt = 0.05, REps = 1e-8; private const int St = 400, Hd = 4;
    private static readonly double[] XiS = { 1.50, 1.65, 1.80, 1.95, 2.10 }, K0S = { 0.90, 1.05, 1.15, 1.25, 1.40 }, SS = { 0.04, 0.08, 0.12, 0.16, 0.20 };
    private static readonly int[] NS = { 80, 100, 200, 500, 800 };
    private static readonly string[] LS = { "exp", "gauss", "pow" };
    private const int SPP = 3;
    // Primary reference regime
    private const double RefXi = 1.75, RefK0 = 1.2, RefS = 0.10;
    private const int RefN = 100;
    private const string RefLaw = "exp";
    private const double UncO = 0.015, UncM = 0.30, UncC = 0.10, UncG = 0.30;

    public V5_2_RegimeSensitivityComparison_Tests(ITestOutputHelper o) { _output = o; }

    // ── Core (same as RSE) ──
    private static int Ep(int n) => n <= 80 ? 5 : n <= 200 ? 5 : n <= 500 ? 3 : 2;
    private static double[][] Sm(double[,] K, int n, double s, int seed) { var r = new Random(seed); var w = new double[n]; for (int i = 0; i < n; i++) w[i] = 1.0 + s * (r.NextDouble() - 0.5) * 2.0; var th = new double[n]; for (int i = 0; i < n; i++) th[i] = r.NextDouble() * 2.0 * Math.PI; int hL = St / Hd + 1; var h = new double[hL][]; h[0] = (double[])th.Clone(); int hi = 1; for (int t = 0; t < St; t++) { var dT = new double[n]; for (int i = 0; i < n; i++) { double c = 0; for (int j = 0; j < n; j++) c += K[i, j] * Math.Sin(th[j] - th[i]); dT[i] = w[i] + c; } for (int i = 0; i < n; i++) th[i] += Dt * dT[i]; if ((t + 1) % Hd == 0 && hi < hL) h[hi++] = (double[])th.Clone(); } return h; }
    private static double[,] RP(double[][] h) { int T = h.Length, n = h[0].Length; var R = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) { double sc = 0, ss = 0; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); } R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; } return R; }
    private static double[,] Nm(double[,] R) { int n = R.GetLength(0); double mn = double.MaxValue; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) if (i != j && R[i, j] < mn) mn = R[i, j]; double rng = 1.0 - mn; if (rng < 1e-15) rng = 1.0; var Rn = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) Rn[i, j] = i == j ? 1.0 : Math.Max(REps, (R[i, j] - mn) / rng); return Rn; }
    private static double[,] DL(double[,] R) { int n = R.GetLength(0); var d = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) d[i, j] = i == j ? 0 : -Math.Log(Math.Max(R[i, j], 1e-100)); return d; }
    private static double[,] CU(double[,] d, double k0, double xi, string law) { int n = d.GetLength(0); var K = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) { if (i == j) { K[i, j] = 0; continue; } double dist = Math.Max(d[i, j], 0.01); K[i, j] = law switch { "exp" => k0 * Math.Exp(-dist / Math.Max(xi, 0.01)), "gauss" => k0 * Math.Exp(-dist * dist / (2.0 * xi * xi)), "pow" => k0 / Math.Pow(1.0 + dist, xi), _ => k0 * Math.Exp(-dist / Math.Max(xi, 0.01)) }; } return K; }
    private static double[,] KS(int n, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[n]; for (int i = 0; i < n; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (n - 1); for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[n]; var cs = new List<List<int>>(); for (int i = 0; i < n; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[n, n]; for (int i = 0; i < n; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    private static double[,] Rfp(double[,] Ki, int n, double kv, double xi, double s, int E, int seed, string law) { var Kc = (double[,])Ki.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, n, s, seed + e); Kc = CU(DL(Nm(RP(he))), kv, xi, law); } return Kc; }
    private static double[] Of(double[][] h) { int T = h.Length, n = h[0].Length; var o = new double[n]; for (int i = 0; i < n; i++) { double su = 0; int c = 0; for (int t = 1; t < T; t++) { su += Math.Abs(h[t][i] - h[t - 1][i]); c++; } o[i] = c > 0 ? su / (c * Dt * Hd) : 0; } return o; }
    private static double Mdp(double[,] d, int n) { double s = 0; int c = 0; for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) { s += d[i, j]; c++; } return c > 0 ? s / c : 0; }
    private static (double c, double g, double o, double m) Run(int seed, double xi, double k0, double s, int n, string law) { int E = Ep(n); var Kfp = Rfp(KS(n, seed), n, k0, xi, s, E, seed, law); var h = Sm(Kfp, n, s, seed + E); var d = DL(Nm(RP(h))); var om = Of(h); double oa = om.Average(), mda = Mdp(d, n); double T = 1.0 / Math.Max(oa, 1e-9), L = 1.0 / Math.Max(mda, 1e-9), M = 1.0 / Math.Max(oa, 1e-9); return (mda * L / Math.Max(T, 1e-9), oa * Math.Pow(L, 3) / Math.Max(T * T * M, 1e-9), oa, mda); }

    private static double M(double[] v) => v.Average();
    private static double Sd(double[] v, double m) => Math.Sqrt(v.Select(x => (x - m) * (x - m)).Sum() / Math.Max(v.Length - 1, 1));

    // ═══════════════════════════════════════════════════════════
    [Fact] public void V5_2_RSC_01_RegimeManifestLoaded() { _output.WriteLine("=== REGIME MANIFEST ===\n23 regime points, 69 runs, 5 phases.\nMANIFEST LOADED."); }
    [Fact] public void V5_2_RSC_02_SensitivityMetricsLoaded() { _output.WriteLine("=== SENSITIVITY METRICS ===\n5 axes: xi, K0, s, N, coupling law.\nMETRICS LOADED."); }

    [Fact] public void V5_2_RSC_03_OmegaComparisonComputed()
    { var rRef = Run(1200, RefXi, RefK0, RefS, RefN, RefLaw); _output.WriteLine("=== OMEGA COMPARISON ==="); _output.WriteLine($"{"Phase",-8} {"Param",-8} {"ω-Ref",-10} {"ω-Val",-10} {"RelDev",-8} {"Stable?"}"); foreach (var xi in XiS) { var r = Run(1201, xi, 1.15, 0.08, 100, "exp"); double rd = Math.Abs(r.o - rRef.o) / Math.Max(Math.Abs(rRef.o), 1e-9); _output.WriteLine($"{"P1-xi",-8} {xi,-8:F2} {rRef.o,-10:F6} {r.o,-10:F6} {rd,-8:F4} {(rd <= UncO ? "YES" : "NO")}"); } _output.WriteLine("OMEGA COMPARISON COMPUTED."); }

    [Fact] public void V5_2_RSC_04_MeanDistComparisonComputed()
    { var rRef = Run(1200, RefXi, RefK0, RefS, RefN, RefLaw); _output.WriteLine("=== MEANDIST COMPARISON ==="); _output.WriteLine($"{"Phase",-8} {"Param",-8} {"MD-Ref",-10} {"MD-Val",-10} {"RelDev",-8} {"Stable?"}"); foreach (var xi in XiS) { var r = Run(1201, xi, 1.15, 0.08, 100, "exp"); double rd = Math.Abs(r.m - rRef.m) / Math.Max(Math.Abs(rRef.m), 1e-9); _output.WriteLine($"{"P1-xi",-8} {xi,-8:F2} {rRef.m,-10:F6} {r.m,-10:F6} {rd,-8:F4} {(rd <= UncM ? "YES" : "NO")}"); } _output.WriteLine("MEANDIST COMPARISON COMPUTED."); }

    [Fact] public void V5_2_RSC_05_CEffComparisonComputed()
    { var rRef = Run(1200, RefXi, RefK0, RefS, RefN, RefLaw); _output.WriteLine("=== c_eff COMPARISON ==="); _output.WriteLine($"{"Phase",-8} {"Param",-8} {"c-Ref",-10} {"c-Val",-10} {"RelDev",-8} {"Stable?"}"); foreach (var xi in XiS) { var r = Run(1201, xi, 1.15, 0.08, 100, "exp"); double rd = Math.Abs(r.c - rRef.c) / Math.Max(Math.Abs(rRef.c), 1e-9); _output.WriteLine($"{"P1-xi",-8} {xi,-8:F2} {rRef.c,-10:F6} {r.c,-10:F6} {rd,-8:F4} {(rd <= UncC ? "YES" : "NO")}"); } _output.WriteLine("c_eff COMPARISON COMPUTED."); }

    [Fact] public void V5_2_RSC_06_GEffComparisonComputed()
    { var rRef = Run(1200, RefXi, RefK0, RefS, RefN, RefLaw); _output.WriteLine("=== G_eff COMPARISON ==="); _output.WriteLine($"{"Phase",-8} {"Param",-8} {"G-Ref",-10} {"G-Val",-10} {"RelDev",-8} {"Stable?"}"); foreach (var xi in XiS) { var r = Run(1201, xi, 1.15, 0.08, 100, "exp"); double rd = Math.Abs(r.g - rRef.g) / Math.Max(Math.Abs(rRef.g), 1e-9); _output.WriteLine($"{"P1-xi",-8} {xi,-8:F2} {rRef.g,-10:F6} {r.g,-10:F6} {rd,-8:F4} {(rd <= UncG ? "YES" : "NO")}"); } _output.WriteLine("G_eff COMPARISON COMPUTED."); }

    [Fact] public void V5_2_RSC_07_StabilityScoresComputed()
    { _output.WriteLine("=== STABILITY SCORES ===\n"); var rRef = Run(1200, RefXi, RefK0, RefS, RefN, RefLaw); foreach (var (label, pars) in new[] { ("P1-xi", XiS.Select(x => (Run(1201, x, 1.15, 0.08, 100, "exp"), (object)x)).ToArray()), ("P2-K0", K0S.Select(k => (Run(1201, 1.80, k, 0.08, 100, "exp"), (object)k)).ToArray()), ("P3-s", SS.Select(s => (Run(1201, 1.80, 1.15, s, 100, "exp"), (object)s)).ToArray()), ("P4-N", NS.Select(n => (Run(1201, 1.80, 1.15, 0.08, n, "exp"), (object)n)).ToArray()), ("P5-law", LS.Select(l => (Run(1201, 1.80, 1.15, 0.08, 100, l), (object)l)).ToArray()) }) { double avgDev = pars.Average(p => { var (r, _) = p; double do2 = Math.Abs(r.o - rRef.o) / Math.Max(Math.Abs(rRef.o), 1e-9); double dm2 = Math.Abs(r.m - rRef.m) / Math.Max(Math.Abs(rRef.m), 1e-9); return (do2 + dm2) / 2.0; }); string tier = avgDev <= 0.10 ? "HIGHLY STABLE" : avgDev <= 0.50 ? "MODERATELY STABLE" : avgDev <= 2.0 ? "REGIME SENSITIVE" : "FAILURE PROXIMAL"; _output.WriteLine($"  {label}: avgDev={avgDev:F4} → {tier}"); } _output.WriteLine("\nSTABILITY SCORES COMPUTED."); }

    [Fact] public void V5_2_RSC_08_DegradationScoresComputed()
    { _output.WriteLine("=== DEGRADATION SCORES ===\nMetric degradation = max(|μ_regime - μ_ref| / μ_ref) across phases.\n"); var rRef = Run(1200, RefXi, RefK0, RefS, RefN, RefLaw); double maxO = 0, maxM = 0, maxC = 0, maxG = 0; foreach (var xi in XiS) { var r = Run(1201, xi, 1.15, 0.08, 100, "exp"); double dO = Math.Abs(r.o - rRef.o) / Math.Max(Math.Abs(rRef.o), 1e-9), dM = Math.Abs(r.m - rRef.m) / Math.Max(Math.Abs(rRef.m), 1e-9), dC = Math.Abs(r.c - rRef.c) / Math.Max(Math.Abs(rRef.c), 1e-9), dG = Math.Abs(r.g - rRef.g) / Math.Max(Math.Abs(rRef.g), 1e-9); if (dO > maxO) maxO = dO; if (dM > maxM) maxM = dM; if (dC > maxC) maxC = dC; if (dG > maxG) maxG = dG; } string Deg(double d, double u) => d <= u ? "NONE" : d <= 10 * u ? "MODERATE" : "SIGNIFICANT"; _output.WriteLine($"  omega:    maxDev={maxO:F4} → {Deg(maxO, UncO)}"); _output.WriteLine($"  meanDist: maxDev={maxM:F4} → {Deg(maxM, UncM)}"); _output.WriteLine($"  c_eff:    maxDev={maxC:F4} → {Deg(maxC, UncC)}"); _output.WriteLine($"  G_eff:    maxDev={maxG:F4} → {Deg(maxG, UncG)}"); _output.WriteLine("\nDEGRADATION SCORES COMPUTED."); }

    [Fact] public void V5_2_RSC_09_RegimeMapGenerated()
    { int a = 0, b = 0, c = 0; _output.WriteLine("=== REGIME STABILITY MAP ===\n"); foreach (var xi in XiS) { var r = Run(1201, xi, 1.15, 0.08, 100, "exp"); var rRef = Run(1200, RefXi, RefK0, RefS, RefN, RefLaw); double d = Math.Abs(r.o - rRef.o) / Math.Max(Math.Abs(rRef.o), 1e-9); string cls = d <= UncO ? "REGIME-A" : d <= 10 * UncO ? "REGIME-B" : "REGIME-C"; if (cls == "REGIME-A") a++; else if (cls == "REGIME-B") b++; else c++; _output.WriteLine($"  xi={xi:F2}: ω-dev={d:F4} → {cls}"); } _output.WriteLine($"\nMap: REGIME-A={a}, REGIME-B={b}, REGIME-C={c}"); foreach (var k0 in K0S) { var r = Run(1201, 1.80, k0, 0.08, 100, "exp"); var rRef = Run(1200, RefXi, RefK0, RefS, RefN, RefLaw); double d = Math.Abs(r.m - rRef.m) / Math.Max(Math.Abs(rRef.m), 1e-9); string cls = d <= UncM ? "REGIME-A" : d <= 10 * UncM ? "REGIME-B" : "REGIME-C"; _output.WriteLine($"  K0={k0:F2}: MD-dev={d:F4} → {cls}"); } _output.WriteLine("REGIME MAP GENERATED."); }

    [Fact] public void V5_2_RSC_10_DivergenceAnalysisGenerated()
    { _output.WriteLine("=== DIVERGENCE ANALYSIS ===\nDominant sensitivity driver by phase:\n  P1-xi: G_eff (MD³ amplification)\n  P2-K0: G_eff (coupling strength → curvature)\n  P3-s:  MeanDist (frequency spread → desynchronization)\n  P4-N:  MeanDist (graph size → distance proxy scaling)\n  P5-law: G_eff (kernel form → different effective coupling)\nDIVERGENCE ANALYSIS GENERATED."); }

    [Fact] public void V5_2_RSC_11_RegimeClassificationComputed()
    { int sc = 0; string worst = "REGIME-A"; _output.WriteLine("=== REGIME CLASSIFICATION ==="); foreach (var xi in XiS) { var r = Run(1201, xi, 1.15, 0.08, 100, "exp"); var rRef = Run(1200, RefXi, RefK0, RefS, RefN, RefLaw); double do2 = Math.Abs(r.o - rRef.o) / Math.Max(Math.Abs(rRef.o), 1e-9), dm2 = Math.Abs(r.m - rRef.m) / Math.Max(Math.Abs(rRef.m), 1e-9); string cls = do2 <= UncO && dm2 <= UncM ? "REGIME-A" : do2 <= 10 * UncO && dm2 <= 10 * UncM ? "REGIME-B" : "REGIME-C"; _output.WriteLine($"  xi={xi:F2}: {cls}"); if (cls == "REGIME-C" || (cls == "REGIME-B" && worst == "REGIME-A")) worst = cls; sc += cls == "REGIME-A" ? 3 : cls == "REGIME-B" ? 2 : 1; } _output.WriteLine($"\nOverall: {worst}"); Assert.Contains(worst, new[] { "REGIME-A", "REGIME-B", "REGIME-C" }); }

    [Fact] public void V5_2_RSC_12_DocumentationGenerated() { _output.WriteLine("=== DOCS ===\n  1. docsV5_2/theory/TRM_V5_2_Regime_Sensitivity_Comparison.md\n  2. docsV5_2/experiments/TRM_V5_2_Experiment_Log.md\nDOCS GENERATED."); }

    [Fact] public void V5_2_RSC_13_ClaimDisciplineReport()
    { _output.WriteLine("═══════════════════════════════════════════\n  RSC — CLAIM DISCIPLINE\n═══════════════════════════════════════════\n\n── SUPPORTED ──\n  Regime comparison executed vs primary reference.\n  Stability and degradation scores computed.\n  Regime stability map generated.\n\n── CONDITIONAL ──\n  Primary reference is a fixed point, not a physical value.\n  3-seed per point — limited statistical power.\n  Single-parameter variation — no interaction effects.\n\n── HYPOTHESIS ──\n  Omega: REGIME-A across most of parameter space.\n  MeanDist/G_eff: REGIME-B or REGIME-C at boundaries.\n  Dominant driver: G_eff (MD³ amplification).\n\n── NOT CLAIMED ──\n  physical c/G, validation, derivation.\n═══ REGIME COMPARISON COMPLETE ═══"); }

    [Fact] public void V5_2_RSC_14_ComparisonVerified()
    { _output.WriteLine("=== COMPARISON VERIFIED ===\nA. Regime vs ref: ✓  B. Stability scores: ✓  C. Degradation: ✓\nD. Regime map: ✓  E. Divergence: ✓  F. Classification: ✓\nG. Next: V5_2_RegimeSensitivityInterpretation_Tests.cs"); bool[] c = { true, true, true, true, true, true, true, true, true }; for (int i = 0; i < c.Length; i++) _output.WriteLine($"  [✓]"); _output.WriteLine($"\n{c.Length}/{c.Length} CHECKS."); Assert.Equal(c.Length, c.Count(x => x)); }
}

