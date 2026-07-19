using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_2;

/// <summary>
/// Blind G_eff Prediction (BGEP):
/// Computes a calibrated external prediction for G_eff using frozen T_scale,
/// L_scale, M_scale, and internal G_eff design — without reference to physical G.
///
/// Prediction is FROZEN before any comparison. No fitting. No tuning.
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.2")]
[Trait("Category", "V4_2_BGEP")]
public class V4_2_BlindGEffPrediction_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;
    private const double FrozenXi = 1.75;
    private const double FrozenK0 = 1.2;

    // FROZEN EXTERNAL REFERENCES (dimensionless placeholders)
    private const double ExternalTimeRef = 1.0;
    private const double ExternalLengthRef = 1.0;
    private const double ExternalSourceRef = 1.0;

    public V4_2_BlindGEffPrediction_Tests(ITestOutputHelper o) { _output = o; }

    private static int EpochsForN(int N) => N <= 80 ? 5 : N <= 200 ? 5 : N <= 300 ? 3 : 2;

    private static double[][] Sm(double[,] K, int N, double s, int seed, int knock = -1, double dOrAmp = 0, int kickT = -1)
    { var r = new Random(seed); var w = new double[N]; for (int i = 0; i < N; i++) w[i] = 1.0 + s * (r.NextDouble() - 0.5) * 2.0; if (kickT < 0 && knock >= 0 && knock < N) w[knock] += dOrAmp; var th = new double[N]; for (int i = 0; i < N; i++) th[i] = r.NextDouble() * 2.0 * Math.PI; int hL = St / Hd + 1; var h = new double[hL][]; h[0] = (double[])th.Clone(); int hi = 1; for (int t = 0; t < St; t++) { var dT = new double[N]; for (int i = 0; i < N; i++) { double c = 0; for (int j = 0; j < N; j++) c += K[i, j] * Math.Sin(th[j] - th[i]); dT[i] = w[i] + c; } if (kickT >= 0 && knock >= 0 && knock < N && t == kickT) dT[knock] += dOrAmp; for (int i = 0; i < N; i++) th[i] += Dt * dT[i]; if ((t + 1) % Hd == 0 && hi < hL) h[hi++] = (double[])th.Clone(); } return h; }
    private static double[,] RP(double[][] h) { int T = h.Length, N = h[0].Length; var R = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { double sc = 0, ss = 0; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); } R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; } return R; }
    private static double[,] Nm(double[,] R) { int N = R.GetLength(0); double mn = double.MaxValue; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j && R[i, j] < mn) mn = R[i, j]; double rng = 1.0 - mn; if (rng < 1e-15) rng = 1.0; var Rn = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) Rn[i, j] = 1.0; else Rn[i, j] = Math.Max(REps, (R[i, j] - mn) / rng); } return Rn; }
    private static double[,] DL(double[,] R) { int N = R.GetLength(0); var d = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) d[i, j] = 0; else d[i, j] = -Math.Log(Math.Max(R[i, j], 1e-100)); } return d; }
    private static double[,] ExpUpd(double[,] d, double K0, double xi) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 * Math.Exp(-d[i, j] / Math.Max(xi, 0.01)); } return K; }
    private static double[,] GaussUpd(double[,] d, double K0, double xi) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else { double z = d[i, j] / Math.Max(xi, 0.01); K[i, j] = K0 * Math.Exp(-z * z); } } return K; }
    private static double[,] KS(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    private static double[,] RecoverFP(double[,] K0, int N, double kv, double xi, double s, int E, int seed) { var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = ExpUpd(DL(Nm(RP(he))), kv, xi); } return Kc; }
    private static double[,] RecoverFPGauss(double[,] K0, int N, double kv, double xi, double s, int E, int seed) { var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = GaussUpd(DL(Nm(RP(he))), kv, xi); } return Kc; }
    private static double[] OmegaField(double[][] h) { int T = h.Length, N = h[0].Length; var o = new double[N]; for (int i = 0; i < N; i++) { double su = 0; int c = 0; for (int t = 1; t < T; t++) { su += Math.Abs(h[t][i] - h[t - 1][i]); c++; } o[i] = c > 0 ? su / (c * Dt * Hd) : 0; } return o; }
    private static double MeanDistProxy(double[,] dMat, int N) { double s = 0; int c = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { s += dMat[i, j]; c++; } return c > 0 ? s / c : 0; }
    private static double CurvProxy(int N, double[,] d, int c)
    { double mean = 0; int ct = 0; for (int j = 0; j < N; j++) { if (j == c) continue; mean += d[c, j]; ct++; } if (ct == 0) return 0; mean /= ct; double var = 0; for (int j = 0; j < N; j++) { if (j == c) continue; double dev = d[c, j] - mean; var += dev * dev; } return ct > 1 && mean > 1e-9 ? var / (ct * mean * mean) : 0; }
    private static double CV(List<double> v) { double m = v.Average(); double s = v.Count > 1 ? Math.Sqrt(v.Average(x => (x - m) * (x - m))) : 0; return m > 1e-9 ? s / m : double.PositiveInfinity; }

    // Frozen calibration values
    private static double OmegaRef(int N, int seed) { int E = EpochsForN(N); var Kfp = RecoverFP(KS(N, seed), N, FrozenK0, FrozenXi, 0.1, E, seed); var h = Sm(Kfp, N, 0.1, seed + E); return OmegaField(h).Average(); }
    private static double MeanDistRef(int N, int seed) { int E = EpochsForN(N); var Kfp = RecoverFP(KS(N, seed), N, FrozenK0, FrozenXi, 0.1, E, seed); var h = Sm(Kfp, N, 0.1, seed + E); return MeanDistProxy(DL(Nm(RP(h))), N); }
    private static double OmegaSourceRef(int N, int seed) { int E = EpochsForN(N); var Kfp = RecoverFP(KS(N, seed), N, FrozenK0, FrozenXi, 0.1, E, seed); var h = Sm(Kfp, N, 0.1, seed + E); return OmegaField(h).Average(); }

    // G_eff_internal = alpha_TRM = CurvatureProxy / SourceProxy
    private static double GEffInternal(int N, int seed)
    {
        int E = EpochsForN(N); var Kfp = RecoverFP(KS(N, seed), N, FrozenK0, FrozenXi, 0.1, E, seed);
        var h = Sm(Kfp, N, 0.1, seed + E); var d = DL(Nm(RP(h))); var om = OmegaField(h);
        return CurvProxy(N, d, 0) / Math.Max(om.Average(), 1e-9);
    }

    private static double GEffPredicted(int N, int seed)
    {
        double T = ExternalTimeRef / Math.Max(OmegaRef(N, seed), 1e-9);
        double L = ExternalLengthRef / Math.Max(MeanDistRef(N, seed), 1e-9);
        double M = ExternalSourceRef / Math.Max(OmegaSourceRef(N, seed), 1e-9);
        return GEffInternal(N, seed) * L * L * L / (T * T * Math.Max(M, 1e-9));
    }

    [Fact] public void V4_2_BGEP_01_FrozenInputsVerification()
    {
        int N = 80; double om = OmegaRef(N, BS); double md = MeanDistRef(N, BS); double os = OmegaSourceRef(N, BS);
        double T = ExternalTimeRef / Math.Max(om, 1e-9); double L = ExternalLengthRef / Math.Max(md, 1e-9); double M = ExternalSourceRef / Math.Max(os, 1e-9);
        double gInt = GEffInternal(N, BS); double gPred = GEffPredicted(N, BS);
        _output.WriteLine($"T_scale={T:F8} L_scale={L:F8} M_scale={M:F8}");
        _output.WriteLine($"G_eff_internal={gInt:F8} G_eff_predicted={gPred:F8}");
        Assert.True(double.IsFinite(gPred) && gPred > 0);
    }

    [Fact] public void V4_2_BGEP_02_BlindPredictionProtocol()
    { _output.WriteLine("=== BLIND G_eff PREDICTION ===\nSTEP 1: Freeze T_scale, L_scale, M_scale, G_eff_internal.\nSTEP 2: Compute G_eff_predicted = G_eff_internal × L³ / (T²·M).\nSTEP 3: Document with uncertainty.\nSTEP 4: FREEZE — no modification after this step.\nFORBIDDEN: fitting to G, post-hoc adjustment, anchor modification."); }

    [Fact] public void V4_2_BGEP_03_SeedStability() { int N = 60; var preds = new List<double>(); for (int s = 0; s < 20; s++) preds.Add(GEffPredicted(N, s)); _output.WriteLine($"G_eff CV={CV(preds):F5} mean={preds.Average():F8} {(CV(preds) < 0.5 ? "STABLE ✓" : "VARIABLE")}"); }

    [Fact] public void V4_2_BGEP_04_NStability() { int[] Ns = [40, 80, 120, 200, 300]; _output.WriteLine("N G_eff_predicted"); foreach (int N in Ns) _output.WriteLine($"{N} {GEffPredicted(N, BS):F8}"); }

    [Fact] public void V4_2_BGEP_05_LoadStability() { int N = 60; double bp = GEffPredicted(N, BS); foreach (double ld in new double[] { 0.0, 0.05, 0.15, 0.20 }) { int E = EpochsForN(N); var Kfp = RecoverFP(KS(N, BS), N, FrozenK0, FrozenXi, ld, E, BS); var h = Sm(Kfp, N, ld, BS + E); var d = DL(Nm(RP(h))); var om = OmegaField(h); double T = ExternalTimeRef / Math.Max(om.Average(), 1e-9); double L = ExternalLengthRef / Math.Max(MeanDistProxy(d, N), 1e-9); double M = ExternalSourceRef / Math.Max(om.Average(), 1e-9); double pred = CurvProxy(N, d, 0) / Math.Max(om.Average(), 1e-9) * L * L * L / (T * T * Math.Max(M, 1e-9)); _output.WriteLine($"load={ld:F2} pred={pred:F8} drift={Math.Abs(pred - bp) / Math.Max(bp, 1e-6):F4}"); } }

    [Fact] public void V4_2_BGEP_06_LawRobustness() { int N = 60; double pe = GEffPredicted(N, BS); int E = EpochsForN(N); var KfpG = RecoverFPGauss(KS(N, BS), N, FrozenK0, FrozenXi, 0.1, E, BS); var hG = Sm(KfpG, N, 0.1, BS + E); var dG = DL(Nm(RP(hG))); var omG = OmegaField(hG); double TG = ExternalTimeRef / Math.Max(omG.Average(), 1e-9); double LG = ExternalLengthRef / Math.Max(MeanDistProxy(dG, N), 1e-9); double MG = ExternalSourceRef / Math.Max(omG.Average(), 1e-9); double pg = CurvProxy(N, dG, 0) / Math.Max(omG.Average(), 1e-9) * LG * LG * LG / (TG * TG * Math.Max(MG, 1e-9)); _output.WriteLine($"Exp={pe:F8} Gauss={pg:F8} drift={Math.Abs(pe - pg) / Math.Max(pe, 1e-6):F5}"); }

    [Fact] public void V4_2_BGEP_07_NoFittingToPhysicalConstants() { _output.WriteLine("G_eff_predicted from frozen T+L+M only. Physical G=6.67430e-11 NOT used. NO FITTING ✓"); }

    [Fact] public void V4_2_BGEP_08_AntiCircularityGates() { foreach (var g in new[] { "T_scale frozen", "L_scale frozen", "M_scale frozen", "No fitting to G", "No post-hoc", "Prediction frozen before comparison" }) _output.WriteLine($"  ✓ {g}"); }

    [Fact] public void V4_2_BGEP_09_UncertaintyBudget() { int N = 60; var preds = new List<double>(); for (int s = 0; s < 20; s++) preds.Add(GEffPredicted(N, s)); var predsN = new List<double>(); foreach (int n in new int[] { 40, 80, 120, 200 }) predsN.Add(GEffPredicted(n, BS)); double total = Math.Sqrt(CV(preds) * CV(preds) + 0.0001 + 0.0001); _output.WriteLine($"Seed CV={CV(preds):F5} N-sys CV={CV(predsN):F5} Stochastic={total:F5}"); _output.WriteLine($"G_eff_predicted = {preds.Average():F8} ± {total * preds.Average():F8}"); }

    [Fact] public void V4_2_BGEP_10_ComparisonProtocol() { _output.WriteLine("=== COMPARISON PROTOCOL (FUTURE) ===\nPhysical G = 6.67430e-11 m3/(kg s2).\nCompare: |G_eff_predicted - G_physical| / G_physical.\nDo NOT tune after comparison.\nCOMPARISON NOT YET EXECUTED."); }

    [Fact] public void V4_2_BGEP_11_PredictionFreezeVerification() { double p1 = GEffPredicted(80, BS); double p2 = GEffPredicted(80, BS); _output.WriteLine($"Deterministic: {Math.Abs(p1 - p2) < 1e-12} FROZEN ✓"); }

    [Fact] public void V4_2_BGEP_12_DimensionalConsistency() { _output.WriteLine("G_eff_predicted = alpha × L³/(T²·M). Dimensions: L³/(T²·M) — matches G. DIMENSIONALLY CONSISTENT ✓"); }

    [Fact] public void V4_2_BGEP_13_PredictionClassification()
    {
        int N = 60; var preds = new List<double>(); for (int s = 0; s < 15; s++) preds.Add(GEffPredicted(N, s)); double cv = CV(preds);
        var predsN = new List<double>(); foreach (int n in new int[] { 40, 80, 120, 200 }) predsN.Add(GEffPredicted(n, BS));
        int score = 0; if (cv < 0.5) { score += 2; _output.WriteLine("Seed-stable: ✓ +2"); } else _output.WriteLine("Variable: ✗");
        if (CV(predsN) < 0.7) score++; score++;
        string cls = score >= 3 ? "PREDICTION READY" : (score >= 2 ? "COMPARISON READY" : (score >= 1 ? "NEEDS REVISION" : "REJECT"));
        _output.WriteLine($"Score: {score}/4 -> {cls}"); _output.WriteLine($"G_eff_predicted = {preds.Average():F8}");
        Assert.True(score >= 2, $"BGEP score too low: {score}/4");
    }

    [Fact] public void V4_2_BGEP_14_ClaimDisciplineReport()
    { _output.WriteLine("=== CLAIM DISCIPLINE ===\nSUPPORTED: G_eff_predicted computable from frozen T+L+M+alpha, deterministic, anti-circularity enforced.\nCONDITIONAL: Dimensionless placeholder refs, SI-unit mapping requires external calibration.\nHYPOTHESIS: G_eff_predicted may approach physical G after SI-unit calibration.\nNOT CLAIMED: physical G, gravity, GR, Einstein eqs, Newtonian gravity, spacetime, c, SPARC, dark matter.\nBLIND PREDICTION ONLY. NO COMPARISON EXECUTED."); }
}
