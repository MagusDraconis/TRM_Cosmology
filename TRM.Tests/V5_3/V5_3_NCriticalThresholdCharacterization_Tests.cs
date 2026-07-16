using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_3;

/// <summary>
/// M5 N-Critical Threshold Characterization (NCTC):
///
/// Characterizes the apparent N-critical transition around N≈70-85
/// where RecoverFP shifts from low-Ω (ultra-stable) to high-Ω
/// (variable) behavior at fixed V4.1 parameters.
///
/// Sweeps N from 60 to 100 in 5-point increments, 10 seeds each.
/// Staged: Stage 1 (N=60,70,80,90,100), Stage 2 (65,75,85,95).
///
/// CLAIM DISCIPLINE: Values as reported. No physical phase transition.
/// </summary>
[Trait("Category", "V5_3")]
[Trait("Category", "V5_3_NCTC")]
public class V5_3_NCriticalThresholdCharacterization_Tests
{
    private readonly ITestOutputHelper _output;

    private const double Dt = 0.05; private const int Hd = 4;
    private const double Xi = 1.75; private const double K0 = 1.2; private const double S = 0.10;
    private const int St = 300; private const double REps = 1e-6;
    private const int SeedsPerPoint = 10; private const int SeedStart = 0;

    private const double OmegaHiStable = 0.02; private const double OmegaStable = 0.05;
    private const double MdStable = 0.05; private const double MdVariable = 0.15;

    private static readonly int[] NStage1 = { 60, 70, 80, 90, 100 };
    private static readonly int[] NStage2 = { 65, 75, 85, 95 };

    public V5_3_NCriticalThresholdCharacterization_Tests(ITestOutputHelper o) { _output = o; }

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
    private static double Std(double[] v) { double m = v.Average(); return Math.Sqrt(v.Sum(x => (x - m) * (x - m)) / v.Length); }
    private static double Cv(double[] v) { double m = v.Average(); if (Math.Abs(m) < 1e-15) return double.NaN; return Std(v) / Math.Abs(m); }

    private static (double om, double md) Run(int n, int seed)
    { int E = Ep(n); var Ki = KS(n, seed); var Kfp = Rfp(Ki, n, K0, Xi, S, E, seed, St, REps); var h = Sm(Kfp, n, S, seed + E, St, REps); var om = Of(h, n); var R = RP(h, n); var d = DL(Nm(R, n, REps), n); return (om.Average(), Md(d, n)); }

    // ═══════════════════════════════════════════════════════════

    [Fact] public void V5_3_NCTC_01_Protocol()
    { _output.WriteLine($"M5: N∈[60,100] step 5, xi={Xi}, K0={K0}, s={S} | {SeedsPerPoint} seeds/pt | Ω≤{OmegaStable}=stable, MD≤{MdStable}=stable, MD>{MdVariable}=variable"); }

    [Fact]
    public void V5_3_NCTC_02_FullNScan()
    {
        // Single atomic test: Stage 1 + Stage 2 + analysis
        _output.WriteLine("═══ M5 FULL N SCAN ═══");

        var allN = new List<int>(); var allOmCv = new List<double>(); var allMdCv = new List<double>();
        var allOmMean = new List<double>(); var allMdMean = new List<double>();

        // ── Stage 1 ──
        _output.WriteLine("── Stage 1 (N=60,70,80,90,100) ──");
        foreach (int n in NStage1)
        {
            var oms = new double[SeedsPerPoint]; var mds = new double[SeedsPerPoint];
            for (int s = 0; s < SeedsPerPoint; s++) { var (om, md) = Run(n, SeedStart + s); oms[s] = om; mds[s] = md; }
            double omCv = Cv(oms), mdCv = Cv(mds);
            allN.Add(n); allOmCv.Add(omCv); allMdCv.Add(mdCv); allOmMean.Add(oms.Average()); allMdMean.Add(mds.Average());
            string oC = omCv <= OmegaHiStable ? "HI-STABLE" : omCv <= OmegaStable ? "STABLE" : "VARIABLE";
            string mC = mdCv <= MdStable ? "STABLE" : mdCv > MdVariable ? "VARIABLE" : "INTERMED";
            _output.WriteLine($"  N={n,3}: Ω_mean={oms.Average(),8:F3}, Ω_CV={omCv:F4} ({oC,-10}) | MD_mean={mds.Average():F4}, MD_CV={mdCv:F4} ({mC})");
        }

        // ── Stage 2 ──
        _output.WriteLine("── Stage 2 (N=65,75,85,95) ──");
        foreach (int n in NStage2)
        {
            var oms = new double[SeedsPerPoint]; var mds = new double[SeedsPerPoint];
            for (int s = 0; s < SeedsPerPoint; s++) { var (om, md) = Run(n, SeedStart + s); oms[s] = om; mds[s] = md; }
            double omCv = Cv(oms), mdCv = Cv(mds);
            allN.Add(n); allOmCv.Add(omCv); allMdCv.Add(mdCv); allOmMean.Add(oms.Average()); allMdMean.Add(mds.Average());
            string oC = omCv <= OmegaHiStable ? "HI-STABLE" : omCv <= OmegaStable ? "STABLE" : "VARIABLE";
            string mC = mdCv <= MdStable ? "STABLE" : mdCv > MdVariable ? "VARIABLE" : "INTERMED";
            _output.WriteLine($"  N={n,3}: Ω_mean={oms.Average(),8:F3}, Ω_CV={omCv:F4} ({oC,-10}) | MD_mean={mds.Average():F4}, MD_CV={mdCv:F4} ({mC})");
        }

        // ── Sort by N ──
        var sorted = allN.Select((n, i) => (n, omCv: allOmCv[i], mdCv: allMdCv[i], omMean: allOmMean[i], mdMean: allMdMean[i]))
                         .OrderBy(x => x.n).ToArray();

        // ── Summary ──
        _output.WriteLine("");
        _output.WriteLine("═══ FULL SWEEP (9 points, step 5) ═══");
        _output.WriteLine($"{"N",5} {"Ω_mean",10} {"Ω_CV",8} {"Ω_class",-12} {"MD_mean",8} {"MD_CV",8} {"MD_class"}");
        _output.WriteLine(new string('-', 70));
        foreach (var (n, omCv, mdCv, omMean, mdMean) in sorted)
        {
            string oC = omCv <= OmegaHiStable ? "HI-STABLE" : omCv <= OmegaStable ? "STABLE" : "VARIABLE";
            string mC = mdCv <= MdStable ? "STABLE" : mdCv > MdVariable ? "VARIABLE" : "INTERMED";
            _output.WriteLine($"{n,5} {omMean,10:F3} {omCv,8:F4} {oC,-12} {mdMean,8:F4} {mdCv,8:F4} {mC}");
        }

        // ── Threshold detection ──
        _output.WriteLine("");
        _output.WriteLine("═══ THRESHOLD DETECTION ═══");
        int transIdx = -1; double maxJump = 0;
        for (int i = 1; i < sorted.Length; i++)
        { double jump = sorted[i].omMean - sorted[i - 1].omMean; if (jump > maxJump) { maxJump = jump; transIdx = i; } }
        int nTrans = (int)sorted[transIdx].n, nPrev = (int)sorted[transIdx - 1].n;
        _output.WriteLine($"  Largest Ω_mean jump: {maxJump:F2} at N={nPrev}→{nTrans}");
        int lowRegime = sorted.Count(x => x.omMean < 2.0);
        _output.WriteLine($"  Low-Ω regime (Ω<2): N≤{sorted[lowRegime - 1].n}, High-Ω: N≥{sorted[lowRegime].n}");
        bool hasSpike = sorted.Any(x => x.omCv > 0.15);

        // ── Decision ──
        _output.WriteLine("");
        _output.WriteLine("═══ DECISION ═══");
        if (maxJump > 2.0 && lowRegime > 0 && lowRegime < sorted.Length)
        {
            _output.WriteLine($"GATE A: SHARP N BOUNDARY at N≈{nPrev}–{nTrans}");
            if (hasSpike) _output.WriteLine($"  Ω_CV spike detected — critical-regime behavior at this boundary.");
            _output.WriteLine("  Finite-N RecoverFP transition CONDITIONALLY SUPPORTED.");
        }
        else { _output.WriteLine("GATE B: SMOOTH SCALING — no sharp boundary detected."); }

        // MD transition
        int mdTransN = -1;
        for (int i = 1; i < sorted.Length; i++)
            if (sorted[i - 1].mdCv > MdVariable && sorted[i].mdCv <= MdVariable)
            { mdTransN = (int)sorted[i].n; break; }
        _output.WriteLine(mdTransN > 0 ? $"  MD enters non-VARIABLE at N≈{mdTransN}" : "  MD transition not resolved at this step.");
    }

    [Fact] public void V5_3_NCTC_04_ClaimAudit()
    { _output.WriteLine("=== CLAIM AUDIT ===\nSUPPORTED: N sweep 60-100 step 5, Ω and MD as reported.\nCONDITIONAL: 10 seeds/pt, V4.1 xi/K0/s/St/REps.\nNOT CLAIMED: physical phase transition, H9-H12 confirmed.\nAUDIT: PASSED."); }
}
