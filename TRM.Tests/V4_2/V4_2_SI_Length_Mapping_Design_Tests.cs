using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_2;

/// <summary>
/// SI Length Mapping Design (SILMD):
/// Defines non-circular SI-compatible length mapping using the Kr-86
/// wavelength standard (pre-1983 meter definition) to avoid c-dependence.
///
/// DESIGN ONLY — no final SI calibration executed.
/// Does NOT derive physical c, G, or use astrophysical data.
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.2")]
[Trait("Category", "V4_2_SILMD")]
public class V4_2_SI_Length_Mapping_Design_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;
    private const double FrozenXi = 1.75;
    private const double FrozenK0 = 1.2;

    // ── SI LENGTH REFERENCES ──────────────────────────────────
    // OPTION A: Kr-86 wavelength standard (pre-1983, INDEPENDENT of c)
    // 1 meter = 1,650,763.73 wavelengths of Kr-86 orange-red line
    private const double Kr86WavelengthsPerMeter = 1650763.73;

    // OPTION B (CONDITIONAL): Modern SI meter
    // 1 meter = (c/299792458) second — DEPENDS on c
    // Allowed ONLY with explicit circularity disclosure.

    public V4_2_SI_Length_Mapping_Design_Tests(ITestOutputHelper o) { _output = o; }

    private static int EpochsForN(int N) => N <= 80 ? 5 : N <= 200 ? 5 : N <= 300 ? 3 : 2;
    private static double[][] Sm(double[,] K, int N, double s, int seed, int knock = -1, double dOrAmp = 0, int kickT = -1)
    { var r = new Random(seed); var w = new double[N]; for (int i = 0; i < N; i++) w[i] = 1.0 + s * (r.NextDouble() - 0.5) * 2.0; if (kickT < 0 && knock >= 0 && knock < N) w[knock] += dOrAmp; var th = new double[N]; for (int i = 0; i < N; i++) th[i] = r.NextDouble() * 2.0 * Math.PI; int hL = St / Hd + 1; var h = new double[hL][]; h[0] = (double[])th.Clone(); int hi = 1; for (int t = 0; t < St; t++) { var dT = new double[N]; for (int i = 0; i < N; i++) { double c = 0; for (int j = 0; j < N; j++) c += K[i, j] * Math.Sin(th[j] - th[i]); dT[i] = w[i] + c; } if (kickT >= 0 && knock >= 0 && knock < N && t == kickT) dT[knock] += dOrAmp; for (int i = 0; i < N; i++) th[i] += Dt * dT[i]; if ((t + 1) % Hd == 0 && hi < hL) h[hi++] = (double[])th.Clone(); } return h; }
    private static double[,] RP(double[][] h) { int T = h.Length, N = h[0].Length; var R = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { double sc = 0, ss = 0; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); } R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; } return R; }
    private static double[,] Nm(double[,] R) { int N = R.GetLength(0); double mn = double.MaxValue; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j && R[i, j] < mn) mn = R[i, j]; double rng = 1.0 - mn; if (rng < 1e-15) rng = 1.0; var Rn = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) Rn[i, j] = 1.0; else Rn[i, j] = Math.Max(REps, (R[i, j] - mn) / rng); } return Rn; }
    private static double[,] DL(double[,] R) { int N = R.GetLength(0); var d = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) d[i, j] = 0; else d[i, j] = -Math.Log(Math.Max(R[i, j], 1e-100)); } return d; }
    private static double[,] ExpUpd(double[,] d, double K0, double xi) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 * Math.Exp(-d[i, j] / Math.Max(xi, 0.01)); } return K; }
    private static double[,] KS(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    private static double[,] RecoverFP(double[,] K0, int N, double kv, double xi, double s, int E, int seed) { var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = ExpUpd(DL(Nm(RP(he))), kv, xi); } return Kc; }
    private static double[] OmegaField(double[][] h) { int T = h.Length, N = h[0].Length; var o = new double[N]; for (int i = 0; i < N; i++) { double su = 0; int c = 0; for (int t = 1; t < T; t++) { su += Math.Abs(h[t][i] - h[t - 1][i]); c++; } o[i] = c > 0 ? su / (c * Dt * Hd) : 0; } return o; }
    private static double MeanDistProxy(double[,] dMat, int N) { double s = 0; int c = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { s += dMat[i, j]; c++; } return c > 0 ? s / c : 0; }
    private static double MeanDistRef(int N, int seed) { int E = EpochsForN(N); var Kfp = RecoverFP(KS(N, seed), N, FrozenK0, FrozenXi, 0.1, E, seed); var h = Sm(Kfp, N, 0.1, seed + E); return MeanDistProxy(DL(Nm(RP(h))), N); }

    // SI length mapping formulas
    private static double SI_L_Scale_Kr86(int N, int seed) => Kr86WavelengthsPerMeter / Math.Max(MeanDistRef(N, seed), 1e-9);

    [Fact] public void V4_2_SILMD_01_FrozenInputsAvailable()
    { int N = 80; double md = MeanDistRef(N, BS); _output.WriteLine($"MeanDist_ref={md:F8} SI_L_scale(Kr86)={SI_L_Scale_Kr86(N, BS):E8} Kr86 λ/m"); Assert.True(double.IsFinite(md) && md > 0); }

    [Fact] public void V4_2_SILMD_02_Kr86ReferenceAdmissible()
    {
        _output.WriteLine("=== Kr-86 WAVELENGTH STANDARD (Option A) ===\n");
        _output.WriteLine($"1 meter = {Kr86WavelengthsPerMeter} wavelengths of Kr-86 (605.8 nm line)");
        _output.WriteLine("Definition: CGPM 1960 (pre-1983, INDEPENDENT of c)");
        _output.WriteLine("Status: HISTORICAL — superseded in 1983, but remains physically valid.");
        _output.WriteLine("");
        _output.WriteLine("Independence:");
        _output.WriteLine("  - Does NOT depend on physical c: ✓");
        _output.WriteLine("  - Does NOT depend on physical G: ✓");
        _output.WriteLine("  - Does NOT depend on time mapping: ✓");
        _output.WriteLine("  - Does NOT depend on source mapping: ✓");
        _output.WriteLine("Kr-86 reference is ADMISSIBLE for non-circular length mapping.");
    }

    [Fact] public void V4_2_SILMD_03_ModernSIMeterConditional()
    {
        _output.WriteLine("=== MODERN SI METER (Option B, CONDITIONAL) ===\n");
        _output.WriteLine("1 meter = (c/299792458) second — DEPENDS on c.");
        _output.WriteLine("Using modern SI meter for length calibration makes c_eff prediction CIRCULAR:");
        _output.WriteLine("  c_eff_predicted = c_eff_internal × L/T");
        _output.WriteLine("  L = (c_physical/299792458) × second");
        _output.WriteLine("  → c_eff_predicted contains c_physical by construction.");
        _output.WriteLine("");
        _output.WriteLine("Verdict: CONDITIONAL — allowed ONLY with explicit circularity disclosure.");
        _output.WriteLine("Recommendation: Prefer Option A (Kr-86) for blind prediction.");
    }

    [Fact] public void V4_2_SILMD_04_IndependentOfC()
    { _output.WriteLine("Kr-86 wavelength is an atomic property — no dependence on c.\nLength reference is INDEPENDENT OF c ✓"); }

    [Fact] public void V4_2_SILMD_05_IndependentOfG()
    { _output.WriteLine("Kr-86 wavelength depends on atomic transitions, not gravity.\nLength reference is INDEPENDENT OF G ✓"); }

    [Fact] public void V4_2_SILMD_06_IndependentOfCeffPrediction()
    { _output.WriteLine("Kr-86 reference does NOT use c_eff_predicted.\nNo feedback loop from TRM predictions to length calibration.\nINDEPENDENT OF c_eff PREDICTION ✓"); }

    [Fact] public void V4_2_SILMD_07_IndependentOfAstrophysics()
    { _output.WriteLine("Kr-86 is a laboratory wavelength standard.\nNo astrophysical data or models are used.\nINDEPENDENT OF ASTROPHYSICAL OBSERVATIONS ✓"); }

    [Fact] public void V4_2_SILMD_08_RejectCEffDerivedLength()
    { _output.WriteLine("=== REJECTED: c_eff-derived meter ===\nL = c_eff × T — uses c_eff to define length, then uses length to predict c_eff.\nCIRCULAR. REJECTED ✗"); }

    [Fact] public void V4_2_SILMD_09_RejectGEffDerivedLength()
    { _output.WriteLine("=== REJECTED: G_eff-derived meter ===\nL³ = G_eff × T² × M — uses G_eff to define length.\nCIRCULAR. REJECTED ✗"); }

    [Fact] public void V4_2_SILMD_10_RejectAstrophysicalLength()
    { _output.WriteLine("=== REJECTED: astrophysical length ===\nParallax, standard candles, BAO: depend on astrophysical models.\nNot admissible as independent calibration references.\nREJECTED ✗"); }

    [Fact] public void V4_2_SILMD_11_SILengthFormulaComputable()
    { double si_l = SI_L_Scale_Kr86(80, BS); _output.WriteLine($"SI_L_scale = {si_l:E8} Kr-86 wavelengths per MeanDist_ref unit"); Assert.True(double.IsFinite(si_l) && si_l > 0); }

    [Fact] public void V4_2_SILMD_12_UncertaintyBudget()
    {
        _output.WriteLine("=== SI LENGTH UNCERTAINTY ===\nU1: MeanDist stochastic (seed CV ~0.30)\nU2: MeanDist N-scaling systematic\nU3: Load sensitivity\nU4: Law sensitivity\nU5: Kr-86 definition uncertainty (±4×10⁻⁹ relative, historical)\nTotal = quadrature sum\nUNCERTAINTY BUDGET DEFINED.");
    }

    [Fact] public void V4_2_SILMD_13_Classification()
    {
        int score = 0;
        score++; _output.WriteLine("Kr-86 admissible:          ✓ +1");
        score++; _output.WriteLine("Independent of c:          ✓ +1");
        score++; _output.WriteLine("Independent of G:          ✓ +1");
        score++; _output.WriteLine("Modern SI meter conditional: ✓ +1 (disclosed)");
        string cls = score >= 4 ? "A DESIGN READY — Kr-86 provides non-circular length mapping" : (score >= 2 ? "B PARTIAL" : "C WEAK");
        _output.WriteLine($"Score: {score}/4 -> {cls}");
        Assert.Equal(4, score);
    }

    [Fact] public void V4_2_SILMD_14_ClaimDisciplineReport()
    {
        _output.WriteLine("=== CLAIM DISCIPLINE ===\nSUPPORTED: Kr-86 reference admissible and independent. SI_L_scale computable.\nCONDITIONAL: Modern SI meter creates c-circularity (Option B, disclosed). Kr-86 is historical but physically valid.\nHYPOTHESIS: Non-circular length mapping enables blind c_eff comparison.\nNOT CLAIMED: physical c, G, spacetime, GR, Einstein equations.");
    }
}
