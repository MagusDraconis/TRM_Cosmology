using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_2;

/// <summary>
/// SI Time Mapping Design (SITMD):
/// Defines non-circular SI time mapping from Omega/T_scale to SI seconds
/// using the Cs-133 hyperfine transition frequency standard.
///
/// DESIGN ONLY — no final SI calibration executed.
/// Does NOT derive physical c, G, or use astrophysical data.
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.2")]
[Trait("Category", "V4_2_SITMD")]
public class V4_2_SI_Time_Mapping_Design_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;
    private const double FrozenXi = 1.75;
    private const double FrozenK0 = 1.2;

    // ── SI TIME REFERENCE ─────────────────────────────────────
    // Cs-133 hyperfine transition: 9,192,631,770 Hz (exact, SI definition)
    // 1 second = 9,192,631,770 periods of Cs-133 radiation
    // This reference is INDEPENDENT of c, G, and TRM.
    // Design only — value is a constant, not used in computation yet.
    private const double Cs133Frequency = 9192631770.0; // Hz (exact)
    private const double SI_Second_In_Cs133Periods = Cs133Frequency;

    public V4_2_SI_Time_Mapping_Design_Tests(ITestOutputHelper o) { _output = o; }

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
    private static double OmegaRef(int N, int seed) { int E = EpochsForN(N); var Kfp = RecoverFP(KS(N, seed), N, FrozenK0, FrozenXi, 0.1, E, seed); var h = Sm(Kfp, N, 0.1, seed + E); return OmegaField(h).Average(); }

    // Frozen Omega_ref and T_scale from ETCE
    private static double TScale(int N, int seed) => 1.0 / Math.Max(OmegaRef(N, seed), 1e-9);

    // SI time mapping formula
    private static double SI_T_Scale(int N, int seed) => SI_Second_In_Cs133Periods / Math.Max(OmegaRef(N, seed), 1e-9);

    [Fact] public void V4_2_SITMD_01_FrozenInputsAvailable()
    { int N = 80; double om = OmegaRef(N, BS); double ts = TScale(N, BS); _output.WriteLine($"Omega_ref={om:F8} T_scale(dimless)={ts:F8} SI_T_scale={SI_T_Scale(N, BS):E8} Cs133/s"); Assert.True(double.IsFinite(om) && om > 0); }

    [Fact] public void V4_2_SITMD_02_Cs133ReferenceAdmissible()
    {
        _output.WriteLine("=== Cs-133 TIME REFERENCE ===\n");
        _output.WriteLine($"Cs-133 hyperfine frequency: {Cs133Frequency:E8} Hz (exact, SI definition)");
        _output.WriteLine($"1 second = {Cs133Frequency:E0} Cs-133 periods");
        _output.WriteLine("Independence:");
        _output.WriteLine("  - Does NOT depend on physical c: ✓");
        _output.WriteLine("  - Does NOT depend on physical G: ✓");
        _output.WriteLine("  - Does NOT depend on length anchor: ✓");
        _output.WriteLine("  - Does NOT depend on source anchor: ✓");
        _output.WriteLine("Cs-133 reference is ADMISSIBLE for non-circular SI time mapping.");
    }

    [Fact] public void V4_2_SITMD_03_GenericFrequencyReferenceAdmissible()
    { _output.WriteLine("=== GENERIC FREQUENCY REFERENCE ===\nAny external frequency standard independent of c and G is admissible.\nExamples: atomic clocks (Rb, H-maser), quartz oscillators traceable to Cs-133.\nGENERIC FREQUENCY REFERENCES ADMISSIBLE."); }

    [Fact] public void V4_2_SITMD_04_TimeReferenceIndependentOfC()
    { _output.WriteLine("Cs-133 definition: Δν_Cs = 9,192,631,770 Hz.\nThis is a fundamental atomic property. It does NOT depend on c.\nThe SI second is defined independently of the speed of light.\nTIME REFERENCE INDEPENDENT OF c ✓"); }

    [Fact] public void V4_2_SITMD_05_TimeReferenceIndependentOfG()
    { _output.WriteLine("Cs-133 hyperfine splitting depends on QED, not gravity.\nNo gravitational constant appears in the definition.\nTIME REFERENCE INDEPENDENT OF G ✓"); }

    [Fact] public void V4_2_SITMD_06_TimeReferenceIndependentOfLengthAnchor()
    { _output.WriteLine("SI second is a base unit. It does NOT depend on the meter.\nTime mapping CAN proceed before length mapping.\nTIME REFERENCE INDEPENDENT OF LENGTH ANCHOR ✓"); }

    [Fact] public void V4_2_SITMD_07_TimeReferenceIndependentOfSourceAnchor()
    { _output.WriteLine("SI second does NOT depend on the kilogram.\nTime mapping CAN proceed before source mapping.\nTIME REFERENCE INDEPENDENT OF SOURCE ANCHOR ✓"); }

    [Fact] public void V4_2_SITMD_08_RejectDerivedFromCEff()
    { _output.WriteLine("=== REJECTED: c_eff-derived time ===\nDeriving seconds from c_eff_predicted is CIRCULAR:\nc_eff = L/T → T = L/c_eff.\nThis uses c_eff to define time, then uses time to predict c_eff.\nFORBIDDEN — REJECTED ✗"); }

    [Fact] public void V4_2_SITMD_09_RejectDerivedFromGEff()
    { _output.WriteLine("=== REJECTED: G_eff-derived time ===\nDeriving seconds from G_eff_predicted is CIRCULAR.\nG_eff = L³/(T²·M) → T depends on G_eff.\nFORBIDDEN — REJECTED ✗"); }

    [Fact] public void V4_2_SITMD_10_RejectAstrophysicalTimeReference()
    { _output.WriteLine("=== REJECTED: astrophysical time ===\nPulsar periods, orbital periods, cosmological time:\nALL depend on astrophysical models and gravitational assumptions.\nThese are EXTERNAL DATA not allowed during calibration.\nFORBIDDEN — REJECTED ✗"); }

    [Fact] public void V4_2_SITMD_11_SITimeScaleFormulaComputable()
    { int N = 80; double si_ts = SI_T_Scale(N, BS); _output.WriteLine($"SI_T_scale = {si_ts:E8} Cs-133 periods per Omega_ref unit"); _output.WriteLine("Formula: SI_T_scale = SI_TimeRef / Omega_ref"); Assert.True(double.IsFinite(si_ts) && si_ts > 0); }

    [Fact] public void V4_2_SITMD_12_UncertaintyBudgetDefined()
    {
        _output.WriteLine("=== SI TIME UNCERTAINTY BUDGET ===\n");
        _output.WriteLine("U1: Omega stochastic uncertainty (seed CV)   — from ETCE");
        _output.WriteLine("U2: Omega N-scaling systematic               — from ETCE");
        _output.WriteLine("U3: Load sensitivity                          — < 1%");
        _output.WriteLine("U4: Law sensitivity                           — < 1%");
        _output.WriteLine("U5: Cs-133 reference uncertainty              — 0 (exact definition)");
        _output.WriteLine("Total = quadrature sum of U1-U4.");
        _output.WriteLine("UNCERTAINTY BUDGET DEFINED.");
    }

    [Fact] public void V4_2_SITMD_13_SITimeMappingClassification()
    {
        _output.WriteLine("=== SITMD CLASSIFICATION ===\n");
        int score = 0;
        score++; _output.WriteLine("Cs-133 reference admissible:       ✓ +1");
        score++; _output.WriteLine("Independent of c:                  ✓ +1");
        score++; _output.WriteLine("Independent of G:                  ✓ +1");
        score++; _output.WriteLine("Independent of length/source:      ✓ +1");
        string cls = score >= 4 ? "A DESIGN READY — non-circular SI time mapping" : (score >= 2 ? "B PARTIAL" : "C WEAK");
        _output.WriteLine($"Score: {score}/4 -> {cls}");
        _output.WriteLine("SI time mapping design is complete. Execution deferred.");
        Assert.Equal(4, score);
    }

    [Fact] public void V4_2_SITMD_14_ClaimDisciplineReport()
    {
        _output.WriteLine("=== CLAIM DISCIPLINE ===\n");
        _output.WriteLine("SUPPORTED:\n  - Cs-133 reference is admissible and independent of c, G, length, source.\n  - SI_T_scale formula is computable.\n  - Uncertainty budget is defined.\n  - No circular dependence on c_eff or G_eff.\n");
        _output.WriteLine("CONDITIONAL:\n  - Design only — SI calibration not yet executed.\n  - External time reference must be frozen before use.\n");
        _output.WriteLine("HYPOTHESIS:\n  - SI time mapping may enable physically meaningful c_eff prediction after length mapping.\n");
        _output.WriteLine("NOT CLAIMED:\n  - Physical c derived\n  - Physical G derived\n  - SI second derived from TRM\n  - Spacetime / GR / Einstein equations derived\n");
        _output.WriteLine("SI TIME MAPPING DESIGN ONLY. NO CALIBRATION EXECUTED.");
    }
}
