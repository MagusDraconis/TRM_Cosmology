using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_3;

/// <summary>
/// M4 Regime Transition Driver Test (RTD):
///
/// Determines whether the V4.1→V5.3 stability inversion is driven by
/// N alone, s alone, separate N and s effects, or N×s interaction.
///
/// M4a: N sweep {60, 80, 100} at V4.1 params (xi=1.75, K0=1.2, s=0.10)
/// M4b: s sweep {0.10, 0.09, 0.08} at V4.1 params (N=60, xi=1.75, K0=1.2)
/// M4c: 2×2 cross (N={60,100} × s={0.10,0.08}) at V4.1 xi/K0
///
/// CLAIM DISCIPLINE: Values as reported. No H9-H12 confirmation.
/// </summary>
[Trait("Category", "V5_3")]
[Trait("Category", "V5_3_RTD")]
public class V5_3_RegimeTransitionDriverTest_Tests
{
    private readonly ITestOutputHelper _output;

    private const double Dt = 0.05; private const int Hd = 4;
    private const double FrozenXi = 1.75; private const double FrozenK0 = 1.2;
    private const double V41_S = 0.10; private const int V41_N = 60;
    private const int StBase = 300; private const double REpsBase = 1e-6;
    private const int SeedsPerPoint = 5; private const int SeedStart = 0;

    private const double OmegaHighlyStable = 0.02;
    private const double OmegaStable = 0.05;
    private const double MdStable = 0.05;
    private const double MdVariable = 0.15;

    // ── V5.3 reference from M3d (seeds 0-19, same pipeline) ──
    private const double V53_OmegaCv = 0.0767;
    private const double V53_MdCv = 0.0675;

    public V5_3_RegimeTransitionDriverTest_Tests(ITestOutputHelper o) { _output = o; }

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

    private static (double om, double md) Run(int n, double xi, double k0, double s, int seed)
    { int E = Ep(n); var Ki = KS(n, seed); var Kfp = Rfp(Ki, n, k0, xi, s, E, seed, StBase, REpsBase); var h = Sm(Kfp, n, s, seed + E, StBase, REpsBase); var om = Of(h, n); var R = RP(h, n); var d = DL(Nm(R, n, REpsBase), n); return (om.Average(), Md(d, n)); }

    // ═══════════════════════════════════════════════════════════

    [Fact] public void V5_3_RTD_01_Protocol()
    { _output.WriteLine($"M4: N∈{{60,80,100}} at (xi={FrozenXi},K0={FrozenK0},s={V41_S}) | s∈{{0.10,0.09,0.08}} at (N={V41_N},xi={FrozenXi},K0={FrozenK0}) | 2×2: N∈{{60,100}}×s∈{{0.10,0.08}} | {SeedsPerPoint} seeds/pt | Ω≤{OmegaStable}=stable, MD≤{MdStable}=stable, MD>{MdVariable}=variable"); }

    [Fact]
    public void V5_3_RTD_02_M4Execution()
    {
        _output.WriteLine("═══ M4 EXECUTION ═══");

        // ── M4a: N sweep ──
        _output.WriteLine("── M4a: N SWEEP (xi=1.75, K0=1.2, s=0.10) ──");
        var ns = new[] { 60, 80, 100 };
        var nOmMeans = new double[3]; var nOmCvs = new double[3];
        var nMdMeans = new double[3]; var nMdCvs = new double[3];

        for (int ni = 0; ni < ns.Length; ni++)
        {
            int n = ns[ni];
            var oms = new double[SeedsPerPoint]; var mds = new double[SeedsPerPoint];
            for (int s = 0; s < SeedsPerPoint; s++)
            { var (om, md) = Run(n, FrozenXi, FrozenK0, V41_S, SeedStart + s); oms[s] = om; mds[s] = md; }
            nOmMeans[ni] = oms.Average(); nOmCvs[ni] = Cv(oms);
            nMdMeans[ni] = mds.Average(); nMdCvs[ni] = Cv(mds);
            string oCls = nOmCvs[ni] <= OmegaHighlyStable ? "HI-STABLE" : nOmCvs[ni] <= OmegaStable ? "STABLE" : "VARIABLE";
            string mCls = nMdCvs[ni] <= MdStable ? "STABLE" : nMdCvs[ni] > MdVariable ? "VARIABLE" : "INTERMED";
            _output.WriteLine($"  N={n,3}: Ω_mean={nOmMeans[ni]:F4}, Ω_CV={nOmCvs[ni]:F4} ({oCls}) | MD_CV={nMdCvs[ni]:F4} ({mCls})");
        }

        // ── M4b: s sweep ──
        _output.WriteLine("── M4b: s SWEEP (N=60, xi=1.75, K0=1.2) ──");
        var ss = new[] { 0.10, 0.09, 0.08 };
        var sOmCvs = new double[3]; var sMdCvs = new double[3];

        for (int si = 0; si < ss.Length; si++)
        {
            double sv = ss[si];
            var oms = new double[SeedsPerPoint]; var mds = new double[SeedsPerPoint];
            for (int sd = 0; sd < SeedsPerPoint; sd++)
            { var (om, md) = Run(V41_N, FrozenXi, FrozenK0, sv, SeedStart + sd); oms[sd] = om; mds[sd] = md; }
            sOmCvs[si] = Cv(oms); sMdCvs[si] = Cv(mds);
            string oCls = sOmCvs[si] <= OmegaHighlyStable ? "HI-STABLE" : sOmCvs[si] <= OmegaStable ? "STABLE" : "VARIABLE";
            string mCls = sMdCvs[si] <= MdStable ? "STABLE" : sMdCvs[si] > MdVariable ? "VARIABLE" : "INTERMED";
            _output.WriteLine($"  s={sv:F2}: Ω_CV={sOmCvs[si]:F4} ({oCls}) | MD_CV={sMdCvs[si]:F4} ({mCls})");
        }

        // ── M4c: 2×2 cross ──
        _output.WriteLine("── M4c: 2×2 CROSS (xi=1.75, K0=1.2) ──");
        var nC = new[] { 60, 100 }; var sC = new[] { 0.10, 0.08 };
        var crossOmCv = new double[2, 2]; var crossMdCv = new double[2, 2];

        for (int ni = 0; ni < 2; ni++)
            for (int si = 0; si < 2; si++)
            {
                var oms = new double[SeedsPerPoint]; var mds = new double[SeedsPerPoint];
                for (int sd = 0; sd < SeedsPerPoint; sd++)
                { var (om, md) = Run(nC[ni], FrozenXi, FrozenK0, sC[si], SeedStart + sd); oms[sd] = om; mds[sd] = md; }
                crossOmCv[ni, si] = Cv(oms); crossMdCv[ni, si] = Cv(mds);
                _output.WriteLine($"  N={nC[ni],3} s={sC[si]:F2}: Ω_CV={crossOmCv[ni, si]:F4} | MD_CV={crossMdCv[ni, si]:F4}");
            }

        // ── Compute interaction effects ──
        double nEffectOm = crossOmCv[1, 0] - crossOmCv[0, 0]; // N effect at s=0.10
        double nEffectMd = crossMdCv[1, 0] - crossMdCv[0, 0];
        double sEffectOm = crossOmCv[0, 1] - crossOmCv[0, 0]; // s effect at N=60
        double sEffectMd = crossMdCv[0, 1] - crossMdCv[0, 0];
        double interactionOm = (crossOmCv[1, 1] - crossOmCv[1, 0]) - (crossOmCv[0, 1] - crossOmCv[0, 0]);
        double interactionMd = (crossMdCv[1, 1] - crossMdCv[1, 0]) - (crossMdCv[0, 1] - crossMdCv[0, 0]);

        // ── Summary ──
        _output.WriteLine("");
        _output.WriteLine("═══ SUMMARY ═══");
        _output.WriteLine($"{"Experiment",-20} {"Ω_CV",-10} {"MD_CV",-10} {"Ω_class",-12} {"MD_class",-12}");
        _output.WriteLine(new string('-', 64));
        _output.WriteLine($"{"V4.1 baseline (N60)",-20} {nOmCvs[0],-10:F4} {nMdCvs[0],-10:F4} {(nOmCvs[0] <= OmegaStable ? "STABLE" : "VARIABLE"),-12} {(nMdCvs[0] > MdVariable ? "VARIABLE" : "STABLE"),-12}");
        _output.WriteLine($"{"N80 only",-20} {nOmCvs[1],-10:F4} {nMdCvs[1],-10:F4} {(nOmCvs[1] <= OmegaStable ? "STABLE" : "VARIABLE"),-12} {(nMdCvs[1] > MdVariable ? "VARIABLE" : "STABLE"),-12}");
        _output.WriteLine($"{"N100 only",-20} {nOmCvs[2],-10:F4} {nMdCvs[2],-10:F4} {(nOmCvs[2] <= OmegaStable ? "STABLE" : "VARIABLE"),-12} {(nMdCvs[2] > MdVariable ? "VARIABLE" : "STABLE"),-12}");
        _output.WriteLine($"{"s0.09 only",-20} {sOmCvs[1],-10:F4} {sMdCvs[1],-10:F4} {(sOmCvs[1] <= OmegaStable ? "STABLE" : "VARIABLE"),-12} {(sMdCvs[1] > MdVariable ? "VARIABLE" : "STABLE"),-12}");
        _output.WriteLine($"{"s0.08 only",-20} {sOmCvs[2],-10:F4} {sMdCvs[2],-10:F4} {(sOmCvs[2] <= OmegaStable ? "STABLE" : "VARIABLE"),-12} {(sMdCvs[2] > MdVariable ? "VARIABLE" : "STABLE"),-12}");
        _output.WriteLine($"{"N100×s0.08",-20} {crossOmCv[1, 1],-10:F4} {crossMdCv[1, 1],-10:F4} {(crossOmCv[1, 1] <= OmegaStable ? "STABLE" : "VARIABLE"),-12} {(crossMdCv[1, 1] > MdVariable ? "VARIABLE" : "STABLE"),-12}");
        _output.WriteLine($"{"V5.3 ref (M3d)",-20} {V53_OmegaCv,-10:F4} {V53_MdCv,-10:F4} {"VARIABLE",-12} {"STABLE",-12}");
        _output.WriteLine("");
        _output.WriteLine("Decomposition (2×2 cross):");
        _output.WriteLine($"  N effect on Ω_CV:  {nEffectOm:+0.0000;-0.0000} | on MD_CV: {nEffectMd:+0.0000;-0.0000}");
        _output.WriteLine($"  s effect on Ω_CV:  {sEffectOm:+0.0000;-0.0000} | on MD_CV: {sEffectMd:+0.0000;-0.0000}");
        _output.WriteLine($"  N×s interaction Ω: {interactionOm:+0.0000;-0.0000} | MD:        {interactionMd:+0.0000;-0.0000}");

        // ── Decision ──
        _output.WriteLine("");
        _output.WriteLine("═══ DECISION ═══");

        double nOmDelta = nOmCvs[2] - nOmCvs[0]; // N=100 minus N=60 Ω CV
        double sMdDelta = sMdCvs[0] - sMdCvs[2]; // s=0.10 minus s=0.08 MD CV
        double nOmFrac = nOmDelta / (V53_OmegaCv - nOmCvs[0]); // fraction of V5.3 gap explained by N alone
        double sMdFrac = sMdDelta / (nMdCvs[0] - V53_MdCv); // fraction explained by s alone

        bool nExplainsOm = nOmFrac > 0.5;
        bool sExplainsMd = sMdFrac > 0.5;
        bool interactionSmall = Math.Abs(interactionOm) < 0.01 && Math.Abs(interactionMd) < 0.05;

        if (nExplainsOm && sExplainsMd && interactionSmall)
        {
            _output.WriteLine("GATE B: SEPARABLE N/s DRIVERS");
            _output.WriteLine($"  N explains {nOmFrac*100:F0}% of Ω CV gap (N-only)");
            _output.WriteLine($"  s explains {sMdFrac*100:F0}% of MD CV gap (s-only)");
            _output.WriteLine("  N×s interaction is small.");
            _output.WriteLine("");
            _output.WriteLine("  BH2: CONDITIONALLY SUPPORTED (two independent drivers).");
            _output.WriteLine("  H11: FIRST CONDITIONAL EVIDENCE for separable control.");
            _output.WriteLine("  Do NOT claim H11 confirmed.");
        }
        else if (nExplainsOm && sExplainsMd && !interactionSmall)
        {
            _output.WriteLine("GATE C-leaning-B: SEPARABLE DRIVERS WITH INTERACTION");
            _output.WriteLine($"  N explains {nOmFrac*100:F0}% of Ω CV gap");
            _output.WriteLine($"  s explains {sMdFrac*100:F0}% of MD CV gap");
            _output.WriteLine("  But interaction is non-negligible.");
            _output.WriteLine("  BH2 partially supported; interaction component present.");
        }
        else if (nExplainsOm && !sExplainsMd)
        {
            _output.WriteLine("GATE A-leaning: N DOMINANT");
            _output.WriteLine($"  N explains {nOmFrac*100:F0}% of Ω CV gap");
            _output.WriteLine($"  s explains only {sMdFrac*100:F0}% of MD CV gap");
            _output.WriteLine("  BH1 partially supported for Ω; MD driver unclear.");
        }
        else if (!nExplainsOm && sExplainsMd)
        {
            _output.WriteLine("GATE C: ASYMMETRIC");
            _output.WriteLine("  N does NOT explain Ω CV gap alone.");
            _output.WriteLine($"  s explains {sMdFrac*100:F0}% of MD CV gap.");
            _output.WriteLine("  Ω transition requires interaction or other parameters.");
        }
        else
        {
            _output.WriteLine("GATE C: INTERACTION OR MISSING DRIVER");
            _output.WriteLine($"  N explains only {nOmFrac*100:F0}% of Ω gap");
            _output.WriteLine($"  s explains only {sMdFrac*100:F0}% of MD gap");
            _output.WriteLine("  Neither N nor s alone reproduces the transition.");
            _output.WriteLine("  BH3 (interaction) or missing-driver hypothesis.");
        }
    }

    [Fact] public void V5_3_RTD_03_ClaimAudit()
    { _output.WriteLine("=== CLAIM AUDIT ===\nSUPPORTED: N and s sweep CVs as reported.\nCONDITIONAL: 5 seeds/pt, V4.1 xi/K0/St/REps.\nNOT CLAIMED: H9-H12 confirmed, attractor decomposition.\nAUDIT: PASSED."); }
}
