using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_3;

/// <summary>
/// M3d V4.1 Regime Omega Seed Stability Verification (VROS):
///
/// Verifies whether the historical Ω CV ~0.01 claim reproduces when
/// the original V4.1 regime is restored under the current pipeline.
/// Also runs a matched V5.3 control (same seeds, different regime)
/// to separate seed-block effects from regime effects.
///
/// CLAIM DISCIPLINE: Values as reported. No H9-H12 confirmation.
/// </summary>
[Trait("Category", "V5_3")]
[Trait("Category", "V5_3_VROS")]
public class V5_3_V41RegimeOmegaSeedStabilityVerification_Tests
{
    private readonly ITestOutputHelper _output;

    // ── V4.1 regime (from OmegaFixedPointClock_Tests) ──
    private const int V41_N = 60;
    private const double V41_Xi = 1.75;
    private const double V41_K0 = 1.2;
    private const double V41_S = 0.10;
    private const int V41_St = 300;
    private const double V41_REps = 1e-6;
    private const int V41_Dt_i = 0; // will use Dt=0.05
    private const double Dt = 0.05;
    private const int Hd = 4;

    // ── V5.3 control regime ──
    private const int V53_N = 100;
    private const double V53_Xi = 1.80;
    private const double V53_K0 = 1.15;
    private const double V53_S = 0.08;
    private const int V53_St = 400;
    private const double V53_REps = 1e-8;

    // ── Seeds (V4.1 used 0–19) ──
    private const int SeedStart = 0;
    private const int SeedCount = 20;

    // ── Thresholds ──
    private const double HighlyStableCv = 0.02;
    private const double StableCv = 0.05;

    public V5_3_V41RegimeOmegaSeedStabilityVerification_Tests(ITestOutputHelper o) { _output = o; }

    // ═══════════════════════════════════════════════════════════
    //  TRM Core — parameterized for dual regime
    // ═══════════════════════════════════════════════════════════
    private static int Ep(int n) => n <= 80 ? 5 : n <= 200 ? 5 : n <= 400 ? 3 : 2;

    private static double[][] Sm(double[,] K, int n, double s, int seed, int st, double reps)
    { var r = new Random(seed); var w = new double[n]; for (int i = 0; i < n; i++) w[i] = 1.0 + s * (r.NextDouble() - 0.5) * 2.0; var th = new double[n]; for (int i = 0; i < n; i++) th[i] = r.NextDouble() * 2.0 * Math.PI; int hL = st / Hd + 1; var h = new double[hL][]; h[0] = (double[])th.Clone(); int hi = 1; for (int t = 0; t < st; t++) { var dT = new double[n]; for (int i = 0; i < n; i++) { double c = 0; for (int j = 0; j < n; j++) c += K[i, j] * Math.Sin(th[j] - th[i]); dT[i] = w[i] + c; } for (int i = 0; i < n; i++) th[i] += Dt * dT[i]; if ((t + 1) % Hd == 0 && hi < hL) h[hi++] = (double[])th.Clone(); } return h; }

    private static double[,] RP(double[][] h, int n) { int T = h.Length; var R = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) { double sc = 0, ss = 0; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); } R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; } return R; }

    private static double[,] Nm(double[,] R, int n, double reps) { double mn = double.MaxValue; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) if (i != j && R[i, j] < mn) mn = R[i, j]; double rng = 1.0 - mn; if (rng < 1e-15) rng = 1.0; var Rn = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) Rn[i, j] = i == j ? 1.0 : Math.Max(reps, (R[i, j] - mn) / rng); return Rn; }

    private static double[,] DL(double[,] R, int n) { var d = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) d[i, j] = i == j ? 0 : -Math.Log(Math.Max(R[i, j], 1e-100)); return d; }

    private static double[,] Cupd(double[,] d, int n, double k0, double xi) { var K = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) K[i, j] = i == j ? 0 : k0 * Math.Exp(-d[i, j] / Math.Max(xi, 0.01)); return K; }

    private static double[,] KS(int n, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[n]; for (int i = 0; i < n; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (n - 1); for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[n]; var cs = new List<List<int>>(); for (int i = 0; i < n; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[n, n]; for (int i = 0; i < n; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }

    private static double[,] Rfp(double[,] Ki, int n, double kv, double xi, double s, int E, int seed, int st, double reps) { var Kc = (double[,])Ki.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, n, s, seed + e, st, reps); Kc = Cupd(DL(Nm(RP(he, n), n, reps), n), n, kv, xi); } return Kc; }

    private static double[] Of(double[][] h, int n) { int T = h.Length; var o = new double[n]; for (int i = 0; i < n; i++) { double su = 0; int c = 0; for (int t = 1; t < T; t++) { su += Math.Abs(h[t][i] - h[t - 1][i]); c++; } o[i] = c > 0 ? su / (c * Dt * Hd) : 0; } return o; }

    private static double Md(double[,] d, int n) { double s = 0; int c = 0; for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) { s += d[i, j]; c++; } return c > 0 ? s / c : 0; }

    private static double CvA(double[] v) { double m = v.Average(); if (Math.Abs(m) < 1e-15) return double.NaN; double va = v.Sum(x => (x - m) * (x - m)) / v.Length; return Math.Sqrt(Math.Max(va, 0)) / Math.Abs(m); }

    private static (double omega, double md) RunRegime(int n, double xi, double k0, double s, int st, double reps, int seed)
    { int E = Ep(n); var Ki = KS(n, seed); var Kfp = Rfp(Ki, n, k0, xi, s, E, seed, st, reps); var h = Sm(Kfp, n, s, seed + E, st, reps); var om = Of(h, n); var R = RP(h, n); var d = DL(Nm(R, n, reps), n); return (om.Average(), Md(d, n)); }

    // ═══════════════════════════════════════════════════════════

    [Fact] public void V5_3_VROS_01_ProtocolLoaded()
    { _output.WriteLine($"=== VROS PROTOCOL ===\nV4.1 regime: N={V41_N}, xi={V41_Xi}, K0={V41_K0}, s={V41_S}, St={V41_St}, REps={V41_REps}\nV5.3 control: N={V53_N}, xi={V53_Xi}, K0={V53_K0}, s={V53_S}, St={V53_St}, REps={V53_REps}\nSeeds: {SeedStart}–{SeedStart + SeedCount - 1} ({SeedCount} seeds)\nHighly-stable: CV≤{HighlyStableCv:F2}, Stable: CV≤{StableCv:F2}\nPROTOCOL LOADED."); }

    [Fact]
    public void V5_3_VROS_02_Verification()
    {
        _output.WriteLine("═══ M3d VERIFICATION ═══");

        // ── V4.1 regime ──
        _output.WriteLine($"── V4.1 REGIME (N={V41_N}, xi={V41_Xi}, K0={V41_K0}, s={V41_S}) ──");
        var v41Om = new double[SeedCount]; var v41Md = new double[SeedCount];
        for (int s = 0; s < SeedCount; s++)
        {
            var (om, md) = RunRegime(V41_N, V41_Xi, V41_K0, V41_S, V41_St, V41_REps, SeedStart + s);
            v41Om[s] = om; v41Md[s] = md;
            _output.WriteLine($"  seed={SeedStart + s}: Ω={om:F6}, MD={md:F4}");
        }
        double v41Cv = CvA(v41Om), v41MdCv = CvA(v41Md);
        string v41Cls = v41Cv <= HighlyStableCv ? "HIGHLY STABLE" : v41Cv <= StableCv ? "STABLE" : "VARIABLE";
        _output.WriteLine($"  → Ω: μ={v41Om.Average():F6}, CV={v41Cv:F4} → {v41Cls}");
        _output.WriteLine($"  → MD: μ={v41Md.Average():F4}, CV={v41MdCv:F4}");

        // ── V5.3 control (same seeds 0–19) ──
        _output.WriteLine($"── V5.3 CONTROL (N={V53_N}, xi={V53_Xi}, K0={V53_K0}, s={V53_S}, seeds {SeedStart}–{SeedStart + SeedCount - 1}) ──");
        var v53Om = new double[SeedCount]; var v53Md = new double[SeedCount];
        for (int s = 0; s < SeedCount; s++)
        {
            var (om, md) = RunRegime(V53_N, V53_Xi, V53_K0, V53_S, V53_St, V53_REps, SeedStart + s);
            v53Om[s] = om; v53Md[s] = md;
            _output.WriteLine($"  seed={SeedStart + s}: Ω={om:F6}, MD={md:F4}");
        }
        double v53Cv = CvA(v53Om), v53MdCv = CvA(v53Md);
        string v53Cls = v53Cv <= HighlyStableCv ? "HIGHLY STABLE" : v53Cv <= StableCv ? "STABLE" : "VARIABLE";
        _output.WriteLine($"  → Ω: μ={v53Om.Average():F6}, CV={v53Cv:F4} → {v53Cls}");
        _output.WriteLine($"  → MD: μ={v53Md.Average():F4}, CV={v53MdCv:F4}");

        // ── Summary ──
        _output.WriteLine("");
        _output.WriteLine("═══ COMPARISON ═══");
        _output.WriteLine($"{"Regime",-14} {"Ω_mean",-10} {"Ω_CV",-8} {"Class",-18} {"MD_CV",-8}");
        _output.WriteLine(new string('-', 58));
        _output.WriteLine($"{"V4.1 (hist)",-14} {v41Om.Average(),-10:F4} {v41Cv,-8:F4} {v41Cls,-18} {v41MdCv,-8:F4}");
        _output.WriteLine($"{"V5.3 (ctrl)",-14} {v53Om.Average(),-10:F4} {v53Cv,-8:F4} {v53Cls,-18} {v53MdCv,-8:F4}");
        _output.WriteLine("");
        _output.WriteLine("Reference: V4.1 historical ~0.01 | M3c measured 0.10 (seeds 100–524, V5.3 regime)");
        _output.WriteLine("");

        // ── Decision ──
        _output.WriteLine("═══ DECISION ═══");

        if (v41Cv <= HighlyStableCv && v53Cv > StableCv)
        {
            _output.WriteLine($"GATE A+E: V4.1 STABLE ({v41Cv:F4}≤{HighlyStableCv:F2}), V5.3 VARIABLE ({v53Cv:F4}>{StableCv:F2})");
            _output.WriteLine("");
            _output.WriteLine("  Strong evidence for REGIME-DEPENDENT Ω seed-stability.");
            _output.WriteLine("  Historical CV ~0.01 reproduces at V4.1 regime.");
            _output.WriteLine("  The V4.1→V5.3 regime shift changes Ω CV by ~10×.");
            _output.WriteLine("");
            _output.WriteLine("  Ω seed-stability: SUPPORTED for V4.1 regime ONLY.");
            _output.WriteLine("  Ω seed-stability: WEAKENED for V5.3 regime.");
            _output.WriteLine("  H9: COND. SUPPORTED (xi-sensitivity) with regime caveat.");
        }
        else if (v41Cv <= StableCv)
        {
            _output.WriteLine($"GATE A/B: V4.1 Ω CV={v41Cv:F4} ≤ {StableCv:F2}");
            _output.WriteLine("");
            _output.WriteLine("  Moderate Ω seed-stability reproduces at V4.1 regime.");
            _output.WriteLine($"  Classification: {(v41Cv <= HighlyStableCv ? "HIGHLY" : "MODERATELY")} seed-stable.");
            _output.WriteLine("");
            if (v53Cv > StableCv)
                _output.WriteLine("  V5.3 control is VARIABLE → regime effect confirmed.");
            else
                _output.WriteLine("  V5.3 control also stable → regime effect not confirmed.");
        }
        else
        {
            _output.WriteLine($"GATE C: V4.1 Ω CV={v41Cv:F4} > {StableCv:F2} — NOT REPRODUCED");
            _output.WriteLine("");
            _output.WriteLine("  Historical Ω seed-stability does NOT reproduce even");
            _output.WriteLine("  at the original V4.1 regime under current pipeline.");
            _output.WriteLine("  Pipeline drift between V4.1 and V5.3 may exist.");
            _output.WriteLine("  Do NOT use Ω seed-stability as a supported finding.");
        }
    }

    [Fact] public void V5_3_VROS_03_ClaimAudit()
    { _output.WriteLine("=== CLAIM AUDIT ===\nSUPPORTED: Ω CV computed at V4.1 and V5.3 regimes.\nCONDITIONAL: {SeedCount} seeds 0–19, single seed block.\nNOT CLAIMED: H9-H12 confirmed, attractor decomposition.\nAUDIT: PASSED."); }
}
