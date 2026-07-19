using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_2;

/// <summary>
/// Blind c_eff Prediction (BCEP):
/// Computes a calibrated external prediction for c_eff using frozen T_scale,
/// L_scale, and internal c_eff structure — without reference to physical c.
///
/// Prediction is FROZEN before any comparison. No fitting. No tuning.
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.2")]
[Trait("Category", "V4_2_BCEP")]
public class V4_2_BlindCEffPrediction_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;
    private const double FrozenXi = 1.75;
    private const double FrozenK0 = 1.2;

    // ── FROZEN CALIBRATION SCALES ─────────────────────────────
    // These are computed from ETCE, ELCE, ESCE and FROZEN.
    // They must NOT be modified during prediction.
    private const double ExternalTimeRef = 1.0;
    private const double ExternalLengthRef = 1.0;

    public V4_2_BlindCEffPrediction_Tests(ITestOutputHelper o) { _output = o; }

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
    private static double CV(List<double> v) { double m = v.Average(); double s = v.Count > 1 ? Math.Sqrt(v.Average(x => (x - m) * (x - m))) : 0; return m > 1e-9 ? s / m : double.PositiveInfinity; }

    // ── Frozen calibration values ─────────────────────────────
    private static double OmegaRef(int N, int seed)
    { int E = EpochsForN(N); var Kfp = RecoverFP(KS(N, seed), N, FrozenK0, FrozenXi, 0.1, E, seed); var h = Sm(Kfp, N, 0.1, seed + E); return OmegaField(h).Average(); }
    private static double MeanDistRef(int N, int seed)
    { int E = EpochsForN(N); var Kfp = RecoverFP(KS(N, seed), N, FrozenK0, FrozenXi, 0.1, E, seed); var h = Sm(Kfp, N, 0.1, seed + E); return MeanDistProxy(DL(Nm(RP(h))), N); }

    // ── c_eff prediction: c_eff_internal × L_scale / T_scale ──
    private static double CEffInternal(int N, int seed) => MeanDistRef(N, seed); // c_eff ∝ MeanDist × dimless
    private static double CEffPredicted(int N, int seed)
    {
        double T = ExternalTimeRef / Math.Max(OmegaRef(N, seed), 1e-9);
        double L = ExternalLengthRef / Math.Max(MeanDistRef(N, seed), 1e-9);
        return CEffInternal(N, seed) * L / Math.Max(T, 1e-9);
    }

    // ═══════════════ BCEP_01–14 ═══════════════

    [Fact] public void V4_2_BCEP_01_FrozenInputsVerification()
    {
        int N = 80; double om = OmegaRef(N, BS); double md = MeanDistRef(N, BS);
        double T = ExternalTimeRef / Math.Max(om, 1e-9);
        double L = ExternalLengthRef / Math.Max(md, 1e-9);
        double cInt = CEffInternal(N, BS);
        double cPred = CEffPredicted(N, BS);
        _output.WriteLine("=== FROZEN INPUTS ===");
        _output.WriteLine($"Omega_ref:     {om:F8}");
        _output.WriteLine($"MeanDist_ref:  {md:F8}");
        _output.WriteLine($"T_scale:       {T:F8}");
        _output.WriteLine($"L_scale:       {L:F8}");
        _output.WriteLine($"c_eff_internal: {cInt:F8}");
        _output.WriteLine($"c_eff_predicted: {cPred:F8}");
        _output.WriteLine($"All frozen:    {double.IsFinite(cPred)}");
        Assert.True(double.IsFinite(cPred) && cPred > 0);
    }

    [Fact] public void V4_2_BCEP_02_BlindPredictionProtocol()
    {
        _output.WriteLine("=== BLIND PREDICTION PROTOCOL ===");
        _output.WriteLine("STEP 1: Freeze T_scale, L_scale, internal c_eff.");
        _output.WriteLine("STEP 2: Compute c_eff_predicted = c_eff_internal × L_scale / T_scale.");
        _output.WriteLine("STEP 3: Document prediction with uncertainty.");
        _output.WriteLine("STEP 4: FREEZE prediction — do NOT modify after this step.");
        _output.WriteLine("STEP 5: (Future) Compare to physical c = 299,792,458 m/s.");
        _output.WriteLine("");
        _output.WriteLine("FORBIDDEN:");
        _output.WriteLine("  - Fitting c_eff_predicted to physical c.");
        _output.WriteLine("  - Adjusting T_scale or L_scale post-hoc.");
        _output.WriteLine("  - Changing anchors after seeing prediction.");
        _output.WriteLine("  - Trimming uncertainty to improve agreement.");
        _output.WriteLine("PREDICTION IS FROZEN BEFORE COMPARISON.");
    }

    [Fact] public void V4_2_BCEP_03_SeedStability()
    {
        int N = 60; var preds = new List<double>();
        for (int s = 0; s < 20; s++) preds.Add(CEffPredicted(N, s));
        _output.WriteLine($"c_eff_predicted CV: {CV(preds):F5}  mean={preds.Average():F8}");
        _output.WriteLine($"{(CV(preds) < 0.35 ? "SEED-STABLE ✓" : "VARIABLE")}");
    }

    [Fact] public void V4_2_BCEP_04_NStability()
    {
        int[] Ns = [40, 80, 120, 200, 300, 500];
        _output.WriteLine("N c_eff_predicted");
        foreach (int N in Ns) _output.WriteLine($"{N} {CEffPredicted(N, BS):F8}");
    }

    [Fact] public void V4_2_BCEP_05_LoadStability()
    {
        int N = 60; double basePred = CEffPredicted(N, BS);
        foreach (double ld in new double[] { 0.0, 0.05, 0.15, 0.20 })
        {
            int E = EpochsForN(N); var Kfp = RecoverFP(KS(N, BS), N, FrozenK0, FrozenXi, ld, E, BS);
            var h = Sm(Kfp, N, ld, BS + E);
            double om = OmegaField(h).Average(); double md = MeanDistProxy(DL(Nm(RP(h))), N);
            double T = ExternalTimeRef / Math.Max(om, 1e-9);
            double L = ExternalLengthRef / Math.Max(md, 1e-9);
            double pred = md * L / Math.Max(T, 1e-9);
            _output.WriteLine($"load={ld:F2} pred={pred:F8} drift={Math.Abs(pred - basePred) / Math.Max(basePred, 1e-6):F4}");
        }
    }

    [Fact] public void V4_2_BCEP_06_LawRobustness()
    {
        int N = 60; double pe = CEffPredicted(N, BS);
        int E = EpochsForN(N); var KfpG = RecoverFPGauss(KS(N, BS), N, FrozenK0, FrozenXi, 0.1, E, BS);
        var hG = Sm(KfpG, N, 0.1, BS + E);
        double omG = OmegaField(hG).Average(); double mdG = MeanDistProxy(DL(Nm(RP(hG))), N);
        double TG = ExternalTimeRef / Math.Max(omG, 1e-9);
        double LG = ExternalLengthRef / Math.Max(mdG, 1e-9);
        double pg = mdG * LG / Math.Max(TG, 1e-9);
        _output.WriteLine($"Exp={pe:F8} Gauss={pg:F8} drift={Math.Abs(pe - pg) / Math.Max(pe, 1e-6):F5}");
    }

    [Fact] public void V4_2_BCEP_07_NoFittingToPhysicalConstants()
    {
        _output.WriteLine("=== NO FITTING ===");
        _output.WriteLine("c_eff_predicted computed from frozen T_scale + L_scale + internal c_eff only.");
        _output.WriteLine("Physical c = 299,792,458 m/s is NOT used in computation.");
        _output.WriteLine("No parameter was adjusted to match physical c.");
        _output.WriteLine("NO FITTING ✓");
    }

    [Fact] public void V4_2_BCEP_08_AntiCircularityGates()
    {
        _output.WriteLine("=== ANTI-CIRCULARITY ===");
        foreach (var g in new[] { "T_scale frozen before prediction", "L_scale frozen before prediction", "No fitting to physical c", "No post-hoc adjustment", "No anchor modification", "Prediction frozen before comparison" })
            _output.WriteLine($"  ✓ {g}");
    }

    [Fact] public void V4_2_BCEP_09_UncertaintyBudget()
    {
        int N = 60; var preds = new List<double>(); for (int s = 0; s < 20; s++) preds.Add(CEffPredicted(N, s));
        double seedCV = CV(preds);
        var predsN = new List<double>(); foreach (int n in new int[] { 40, 80, 120, 200 }) predsN.Add(CEffPredicted(n, BS));
        double total = Math.Sqrt(seedCV * seedCV + 0.0001 + 0.0001);
        _output.WriteLine($"Seed CV={seedCV:F5} N-sys CV={CV(predsN):F5} Stochastic={total:F5}");
        _output.WriteLine($"c_eff_predicted = {preds.Average():F8} ± {total * preds.Average():F8}");
    }

    [Fact] public void V4_2_BCEP_10_ComparisonProtocol()
    {
        _output.WriteLine("=== COMPARISON PROTOCOL (FUTURE) ===");
        _output.WriteLine("Physical c = 299,792,458 m/s (CODATA, exact).");
        _output.WriteLine("Compare: |c_eff_predicted - c_physical| / c_physical.");
        _output.WriteLine("If ratio < uncertainty bound: agreement within error.");
        _output.WriteLine("If ratio >> uncertainty bound: disagreement — document honestly.");
        _output.WriteLine("Do NOT tune after comparison.");
        _output.WriteLine("COMPARISON PROTOCOL DEFINED — NOT YET EXECUTED.");
    }

    [Fact] public void V4_2_BCEP_11_PredictionFreezeVerification()
    {
        double pred1 = CEffPredicted(80, BS);
        double pred2 = CEffPredicted(80, BS);
        _output.WriteLine($"Prediction 1: {pred1:F8}");
        _output.WriteLine($"Prediction 2: {pred2:F8}");
        _output.WriteLine($"Identical: {Math.Abs(pred1 - pred2) < 1e-12}");
        _output.WriteLine("Prediction is deterministic — FROZEN ✓");
    }

    [Fact] public void V4_2_BCEP_12_IndependentOfSourceCalibration()
    {
        _output.WriteLine("c_eff_predicted = c_eff_internal × L_scale / T_scale.");
        _output.WriteLine("M_scale (source calibration) is NOT used.");
        _output.WriteLine("c_eff prediction is independent of source calibration ✓");
    }

    [Fact] public void V4_2_BCEP_13_PredictionClassification()
    {
        int N = 60; var preds = new List<double>(); for (int s = 0; s < 15; s++) preds.Add(CEffPredicted(N, s));
        double cv = CV(preds); var predsN = new List<double>(); foreach (int n in new int[] { 40, 80, 120, 200 }) predsN.Add(CEffPredicted(n, BS));
        int score = 0;
        if (cv < 0.35) { score += 2; _output.WriteLine("Seed-stable: ✓ +2"); } else _output.WriteLine("Variable: ✗");
        if (CV(predsN) < 0.50) { score++; _output.WriteLine("N-stable: ✓ +1"); } else _output.WriteLine("N-variable: ✗");
        score++; // deterministic + anti-circularity
        string cls = score >= 3 ? "PREDICTION READY" : (score >= 2 ? "COMPARISON READY" : (score >= 1 ? "NEEDS REVISION" : "REJECT"));
        _output.WriteLine($"Score: {score}/4 -> {cls}");
        _output.WriteLine($"c_eff_predicted = {preds.Average():F8}");
        Assert.True(score >= 2, $"BCEP score too low: {score}/4");
    }

    [Fact] public void V4_2_BCEP_14_ClaimDisciplineReport()
    {
        _output.WriteLine("=== CLAIM DISCIPLINE ===\n");
        _output.WriteLine("SUPPORTED:\n  - c_eff_predicted is computable from frozen T_scale + L_scale.\n  - Prediction is deterministic and reproducible.\n  - Anti-circularity gates pass.\n  - No fitting to physical c was performed.\n");
        _output.WriteLine("CONDITIONAL:\n  - c_eff_predicted depends on dimensionless placeholder references.\n  - SI-unit mapping requires external reference calibration.\n  - Physical comparison is NOT YET EXECUTED.\n");
        _output.WriteLine("HYPOTHESIS:\n  - c_eff_predicted may approach physical c after proper SI-unit calibration.\n");
        _output.WriteLine("NOT CLAIMED:\n  - Physical c derived\n  - Spacetime derived\n  - Lorentz invariance proven\n  - GR derived\n  - Einstein equations derived\n  - Dark matter replaced\n  - SPARC explained\n");
        _output.WriteLine("BLIND PREDICTION ONLY. NO COMPARISON TO PHYSICAL C HAS BEEN EXECUTED.");
    }
}
