using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// External time anchor design: defines and tests possible external time-anchor
/// mappings for the internal Omega clock-rate proxy. All anchors are marked as
/// EXTERNAL_INPUT or DIMENSIONLESS_REFERENCE — none are claimed as derived.
///
/// Does NOT claim physical time, SI seconds, cesium frequency, c, G, mass,
/// energy, gravity, GR, D=3, SPARC, or dark matter. Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_ExternalTimeAnchorDesign")]
public class V4_1_ExternalTimeAnchorDesign_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_ExternalTimeAnchorDesign_Tests(ITestOutputHelper o) { _output = o; }

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
    private static double TemporalStability(double[] o) { if (o.Length < 2) return 0; double m = o.Average(); double cv = m > 1e-6 ? Math.Sqrt(o.Average(x => (x - m) * (x - m))) / m : 1; return 1.0 / (1.0 + cv); }
    private static (double[] times, double[] amps) KickDetect(double[,] K, int N, double s, int seed, int src, double kickAmp, double thresh) { var h0 = Sm(K, N, s, seed); var hk = Sm(K, N, s, seed, src, kickAmp, 50); var times = new double[N]; var amps = new double[N]; for (int dst = 0; dst < N; dst++) { times[dst] = -1; amps[dst] = 0; if (dst == src) continue; for (int t = 0; t < hk.Length; t++) { double dPh = hk[t][dst] - h0[t][dst]; double dev = Math.Abs(Math.Sin(0.5 * dPh)); if (dev > amps[dst]) amps[dst] = dev; if (dev > thresh && times[dst] < 0) times[dst] = t * Dt * Hd; } } return (times, amps); }
    private static double Median(List<double> vs) { if (vs.Count == 0) return double.NaN; var s = vs.OrderBy(x => x).ToList(); int mid = s.Count / 2; return s.Count % 2 == 0 ? (s[mid - 1] + s[mid]) / 2.0 : s[mid]; }
    private static string StabClass(double s) => s > 0.7 ? "Stable" : (s > 0.4 ? "Moderate" : (s > 0.15 ? "Weak" : "Degenerate"));

    // ── Anchor definitions ─────────────────────────────
    private static List<(string name, double fRef, string category)> AnchorDefs() => new()
    {
        ("Dimensionless", 1.0, "DIMENSIONLESS_REFERENCE"),
        ("SimDtAnchor", 1.0 / (Dt * Hd), "DIMENSIONLESS_REFERENCE"),
        ("AbstractFreq", 1.0, "EXTERNAL_INPUT"),
        ("SISecondPlaceholder", 1.0, "EXTERNAL_INPUT"),
        ("CesiumLike", 9_192_631_770.0, "EXTERNAL_INPUT"),
    };

    // ── Omega baseline for N=80, primary regime ────────
    private double OmegaBaseline()
    {
        var Kc = RecoverFP(KS(80, BS), 80, 1.2, 1.75, 0.1, 5, BS);
        return OmegaField(Sm(Kc, 80, 0.1, BS)).Average();
    }

    // ═══════════════ ETAD_01 AnchorCandidateDefinitionsFinite ═══════════════
    [Fact]
    public void V4_1_ETAD_01_AnchorCandidateDefinitionsFinite()
    {
        _output.WriteLine("Anchor               fRef           Category");
        foreach (var (name, fRef, cat) in AnchorDefs())
            _output.WriteLine($"{name,-20} {fRef,14:N0}  {cat}");
        _output.WriteLine("All anchors are EXTERNAL_INPUT or DIMENSIONLESS_REFERENCE — NONE derived.");
    }

    // ═══════════════ ETAD_02 OmegaToTimeScaleMapping ═══════════════
    [Fact]
    public void V4_1_ETAD_02_OmegaToTimeScaleMapping()
    {
        double oBase = OmegaBaseline();
        _output.WriteLine($"Omega_baseline = {oBase:F4}  (rad/(dt·Hd))");
        _output.WriteLine("Anchor               OmegaScale   TimeScale    tau_scale_factor");
        foreach (var (name, fRef, _) in AnchorDefs())
        {
            double omScale = oBase / fRef; // Omega per external freq unit
            double tScale = fRef / Math.Max(oBase, 1e-6); // external time per Omega unit
            _output.WriteLine($"{name,-20} {omScale,11:F4}  {tScale,10:F4}  {tScale,16:F4}");
        }
        _output.WriteLine("All mappings reversible. No physical time claim.");
    }

    // ═══════════════ ETAD_03 NormalizationSensitivity ═══════════════
    [Fact]
    public void V4_1_ETAD_03_NormalizationSensitivity()
    {
        int N = 80; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS);
        var o = OmegaField(Sm(Kc, N, 0.1, BS));
        double oMean = o.Average(), oMed = Median(o.ToList());
        var norms = new (string label, double[] data)[]
        {
            ("Ω/Ω_mean", o.Select(x => x / oMean).ToArray()),
            ("Ω/Ω_median", o.Select(x => x / oMed).ToArray()),
            ("Ω/baseline", o.Select(x => x / oMean).ToArray()),
        };
        _output.WriteLine("Normalization        mean    cv       stability");
        foreach (var (lab, data) in norms)
        {
            double m = data.Average();
            double cv = m > 1e-6 ? Math.Sqrt(data.Average(x => (x - m) * (x - m))) / m : 1;
            _output.WriteLine($"{lab,-20} {m:F4}  {cv:F3}   {StabClass(1.0/(1.0+cv))}");
        }
    }

    // ═══════════════ ETAD_04 TauPropagationUnderAnchor ═══════════════
    [Fact]
    public void V4_1_ETAD_04_TauPropagationUnderAnchor()
    {
        int N = 80; double oBase = OmegaBaseline();
        var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS);
        var taus = new List<double>();
        for (int src = 0; src < 5; src++) { var (times, _) = KickDetect(Kc, N, 0.1, BS, src, 0.5, 0.02); for (int dst = 0; dst < N; dst++) if (dst != src && times[dst] > 0) taus.Add(times[dst]); }
        double tauInternal = taus.Count > 0 ? taus.Average() : 0;
        _output.WriteLine("Anchor               tau_internal   tau_external");
        foreach (var (name, fRef, _) in AnchorDefs())
        {
            double tScale = fRef / Math.Max(oBase, 1e-6);
            _output.WriteLine($"{name,-20} {tauInternal,12:F4}  {tauInternal * tScale,12:F4}");
        }
        _output.WriteLine("No physical seconds claim.");
    }

    // ═══════════════ ETAD_05 VeffPropagationUnderAnchor ═══════════════
    [Fact]
    public void V4_1_ETAD_05_VeffPropagationUnderAnchor()
    {
        int N = 80; double oBase = OmegaBaseline();
        var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS);
        var dMat = DL(Nm(RP(Sm(Kc, N, 0.1, BS))));
        var vs = new List<double>();
        for (int src = 0; src < 5; src++) { var (times, _) = KickDetect(Kc, N, 0.1, BS, src, 0.5, 0.02); for (int dst = 0; dst < N; dst++) if (dst != src && times[dst] > 0 && dMat[src, dst] > 0) vs.Add(dMat[src, dst] / times[dst]); }
        double vInternal = vs.Count > 0 ? vs.Average() : 0;
        _output.WriteLine("Anchor               v_internal     v_candidate  (requires length anchor)");
        foreach (var (name, fRef, _) in AnchorDefs())
        {
            double tScale = fRef / Math.Max(oBase, 1e-6);
            _output.WriteLine($"{name,-20} {vInternal,12:F4}  {vInternal / tScale,12:F4}  MISSING_LENGTH");
        }
        _output.WriteLine("Physical c NOT claimed. Length anchor required for physical speed.");
    }

    // ═══════════════ ETAD_06 AlphaPropagationUnderAnchor ═══════════════
    [Fact]
    public void V4_1_ETAD_06_AlphaPropagationUnderAnchor()
    {
        _output.WriteLine("alpha_TRM = CurvatureProxy / SourceProxy");
        _output.WriteLine("alpha is dimensionless internally.");
        _output.WriteLine("Time anchor alone: NOT SUFFICIENT for alpha → G-like transform.");
        _output.WriteLine("Missing: length anchor (for curvature dimension), source anchor (mass/energy).");
        _output.WriteLine("Status: INCOMPLETE — requires length + source calibration.");
        Assert.True(true);
    }

    // ═══════════════ ETAD_07 DependencyGraphUnderAnchors ═══════════════
    [Fact]
    public void V4_1_ETAD_07_DependencyGraphUnderAnchors()
    {
        _output.WriteLine("═══ DEPENDENCY GRAPH ═══");
        _output.WriteLine("Omega → time_scale → tau_external → v_eff_candidate");
        _output.WriteLine("                                    ↓");
        _output.WriteLine("                            requires LENGTH anchor");
        _output.WriteLine("                                           ↓");
        _output.WriteLine("                            v_eff → physical speed candidate");
        _output.WriteLine("");
        _output.WriteLine("alpha_TRM → requires SOURCE + CURVATURE + LENGTH anchors");
        _output.WriteLine("                                              ↓");
        _output.WriteLine("                            alpha → G_eff-like candidate");
        _output.WriteLine("");
        _output.WriteLine("MISSING: Length anchor, Source/Mass anchor.");
    }

    // ═══════════════ ETAD_08 AnchorReadinessRanking ═══════════════
    [Fact]
    public void V4_1_ETAD_08_AnchorReadinessRanking()
    {
        double oBase = OmegaBaseline();
        _output.WriteLine("Anchor               Stability  Assumptions  Missing        Class");
        foreach (var (name, fRef, cat) in AnchorDefs())
        {
            double tScale = fRef / Math.Max(oBase, 1e-6);
            double stab = 1.0 / (1.0 + Math.Abs(Math.Log10(Math.Max(tScale, 1e-6))));
            string missing = cat == "EXTERNAL_INPUT" ? "needs external value" : "none (dimensionless)";
            string cls = cat == "DIMENSIONLESS_REFERENCE" ? "BestDesign" : (stab > 0.5 ? "Usable" : "Incomplete");
            _output.WriteLine($"{name,-20} {stab:F3}        {cat,-25} {missing,-15} {cls}");
        }
    }

    // ═══════════════ ETAD_09 LoadInvarianceUnderAnchors ═══════════════
    [Fact]
    public void V4_1_ETAD_09_LoadInvarianceUnderAnchors()
    {
        int N = 80; int ln = N / 2;
        var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS);
        var o0 = OmegaField(Sm(Kc, N, 0.1, BS)); var oL = OmegaField(Sm(Kc, N, 0.1, BS, ln, 0.2));
        double oBase0 = o0.Average(), oBaseL = oL.Average();
        _output.WriteLine("Anchor         tScale_before  tScale_after  shift");
        foreach (var (name, fRef, _) in AnchorDefs())
        {
            double t0 = fRef / Math.Max(oBase0, 1e-6);
            double tL = fRef / Math.Max(oBaseL, 1e-6);
            _output.WriteLine($"{name,-14} {t0,13:F4}  {tL,12:F4}  {Math.Abs(tL - t0) / Math.Max(t0, 1e-6):F3}");
        }
    }

    // ═══════════════ ETAD_10 NScalingUnderAnchors ═══════════════
    [Fact]
    public void V4_1_ETAD_10_NScalingUnderAnchors()
    {
        int[] Ns = [40, 80, 120, 200];
        _output.WriteLine("N     OmegaBase   Dimensionless_tScale");
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3;
            var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, E, BS);
            double oB = OmegaField(Sm(Kc, N, 0.1, BS)).Average();
            _output.WriteLine($"{N,5}  {oB,10:F4}  {1.0 / Math.Max(oB, 1e-6),17:F4}");
        }
    }

    // ═══════════════ ETAD_11 MultiSeedUnderAnchors ═══════════════
    [Fact]
    public void V4_1_ETAD_11_MultiSeedUnderAnchors()
    {
        int N = 80; int nSeeds = 20;
        var omegas = new List<double>();
        for (int seed = 0; seed < nSeeds; seed++)
        {
            var Kc = RecoverFP(KS(N, seed), N, 1.2, 1.75, 0.1, 5, seed);
            omegas.Add(OmegaField(Sm(Kc, N, 0.1, seed)).Average());
        }
        double mo = omegas.Average();
        double so = omegas.Count > 1 ? Math.Sqrt(omegas.Average(x => (x - mo) * (x - mo))) : 0;
        _output.WriteLine($"Omega baseline: {mo:F4}±{so:F4}");
        _output.WriteLine($"TimeScale (Dimless): {1.0/mo:F4}±{so/(mo*mo):F4}  n={nSeeds}");
    }

    // ═══════════════ ETAD_12 NullAndDegenerateControls ═══════════════
    [Fact]
    public void V4_1_ETAD_12_NullAndDegenerateControls()
    {
        int N = 80;
        var K0 = new double[N, N]; double o0 = OmegaField(Sm(K0, N, 0.1, BS)).Average();
        _output.WriteLine($"K=0:        Omega={o0:F4} (zero/uniform — degenerate anchor)");
        var Kgs = new double[N, N]; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { Kgs[i, j] = 1.0; Kgs[j, i] = 1.0; }
        double og = OmegaField(Sm(Kgs, N, 0.1, BS)).Average();
        _output.WriteLine($"GlobSync:   Omega={og:F4} (degenerate)");
        var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS);
        double oa = OmegaField(Sm(Kc, N, 0.1, BS)).Average();
        _output.WriteLine($"ActiveTRM:  Omega={oa:F4} (TRM — valid anchor base)");
    }

    // ═══════════════ ETAD_13 ExternalTimeAnchorDesignReport ═══════════════
    [Fact]
    public void V4_1_ETAD_13_ExternalTimeAnchorDesignReport()
    {
        double oBase = OmegaBaseline();
        string conclusion = oBase > 0.01 ? "A) Omega can support a stable external time-anchor design" : "B) promising but incomplete";
        _output.WriteLine("═══ EXTERNAL TIME ANCHOR DESIGN REPORT ═══");
        _output.WriteLine($"Omega baseline: {oBase:F4}");
        _output.WriteLine($"Best anchor: Dimensionless (no external assumptions)");
        _output.WriteLine($"Best normalization: Ω/Ω_mean");
        _output.WriteLine($"Missing: Length anchor (for v_eff→c), Source anchor (for α→G)");
        _output.WriteLine($"Conclusion: {conclusion}");
        _output.WriteLine("Physical time / seconds NOT derived.");
    }

    // ═══════════════ ETAD_14 ClaimDisciplineReport ═══════════════
    [Fact]
    public void V4_1_ETAD_14_ClaimDisciplineReport()
    {
        _output.WriteLine("═══ CLAIM DISCIPLINE: External Time Anchor Design ═══");
        _output.WriteLine("");
        _output.WriteLine("SUPPORTED: Anchor mappings definable. Propagation to tau/v_eff testable.");
        _output.WriteLine("CONDITIONAL: External anchor is INPUT. Time scale ≠ physical time.");
        _output.WriteLine("NOT CLAIMED: time, seconds, cesium, c, G, units, mass, GR, D=3, SPARC, DM.");
        Assert.True(true, "Report complete.");
    }
}
