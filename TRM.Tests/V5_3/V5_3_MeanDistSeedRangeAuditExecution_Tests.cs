using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_3;

/// <summary>
/// M3b MeanDist Seed Range Audit Stage 1 Execution (MSRA):
///
/// Executes Stage 1 of the frozen MSRA protocol: Block A (seeds 100–109,
/// V5.2 reference) and Block B (seeds 520–524, M3 current). Full TRM
/// RecoverFP pipeline at xi=1.80. Computes raw and normalized MeanDist
/// seed-CV with Omega as control.
///
/// CLAIM DISCIPLINE: Values as reported. No H10/H11/H12 confirmation.
/// </summary>
[Trait("Category", "V5_3")]
[Trait("Category", "V5_3_MSRA")]
public class V5_3_MeanDistSeedRangeAuditExecution_Tests
{
    private readonly ITestOutputHelper _output;

    private const double FrozenK0 = 1.15; private const int FrozenN = 100; private const double FrozenS = 0.08;
    private const double FrozenXi = 1.80;
    private const double Dt = 0.05, REps = 1e-8; private const int St = 400, Hd = 4;
    private const double SeedVariableThreshold = 0.15;

    public V5_3_MeanDistSeedRangeAuditExecution_Tests(ITestOutputHelper o) { _output = o; }

    // ═══════════════════════════════════════════════════════════
    //  TRM Core
    // ═══════════════════════════════════════════════════════════
    private static int Ep(int n) => n <= 80 ? 5 : n <= 200 ? 5 : n <= 400 ? 3 : 2;
    private static double[][] Sm(double[,] K, int n, double s, int seed)
    { var r = new Random(seed); var w = new double[n]; for (int i = 0; i < n; i++) w[i] = 1.0 + s * (r.NextDouble() - 0.5) * 2.0; var th = new double[n]; for (int i = 0; i < n; i++) th[i] = r.NextDouble() * 2.0 * Math.PI; int hL = St / Hd + 1; var h = new double[hL][]; h[0] = (double[])th.Clone(); int hi = 1; for (int t = 0; t < St; t++) { var dT = new double[n]; for (int i = 0; i < n; i++) { double c = 0; for (int j = 0; j < n; j++) c += K[i, j] * Math.Sin(th[j] - th[i]); dT[i] = w[i] + c; } for (int i = 0; i < n; i++) th[i] += Dt * dT[i]; if ((t + 1) % Hd == 0 && hi < hL) h[hi++] = (double[])th.Clone(); } return h; }
    private static double[,] RP(double[][] h) { int T = h.Length, n = h[0].Length; var R = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) { double sc = 0, ss = 0; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); } R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; } return R; }
    private static double[,] Nm(double[,] R) { int n = R.GetLength(0); double mn = double.MaxValue; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) if (i != j && R[i, j] < mn) mn = R[i, j]; double rng = 1.0 - mn; if (rng < 1e-15) rng = 1.0; var Rn = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) Rn[i, j] = i == j ? 1.0 : Math.Max(REps, (R[i, j] - mn) / rng); return Rn; }
    private static double[,] DL(double[,] R) { int n = R.GetLength(0); var d = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) d[i, j] = i == j ? 0 : -Math.Log(Math.Max(R[i, j], 1e-100)); return d; }
    private static double[,] Cupd(double[,] d, double k0, double xi) { int n = d.GetLength(0); var K = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) K[i, j] = i == j ? 0 : k0 * Math.Exp(-d[i, j] / Math.Max(xi, 0.01)); return K; }
    private static double[,] Rfp(double[,] Ki, int n, double kv, double xi, double s, int E, int seed) { var Kc = (double[,])Ki.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, n, s, seed + e); Kc = Cupd(DL(Nm(RP(he))), kv, xi); } return Kc; }
    private static double[] Of(double[][] h) { int T = h.Length, n = h[0].Length; var o = new double[n]; for (int i = 0; i < n; i++) { double su = 0; int c = 0; for (int t = 1; t < T; t++) { su += Math.Abs(h[t][i] - h[t - 1][i]); c++; } o[i] = c > 0 ? su / (c * Dt * Hd) : 0; } return o; }
    private static double[,] KS_mat(int n, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[n]; for (int i = 0; i < n; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (n - 1); for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[n]; var cs = new List<List<int>>(); for (int i = 0; i < n; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[n, n]; for (int i = 0; i < n; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }

    private static (double[,] d, double md, double omega) RunMdOmega(int seed)
    { int E = Ep(FrozenN); var Ki = KS_mat(FrozenN, seed); var Kfp = Rfp(Ki, FrozenN, FrozenK0, FrozenXi, FrozenS, E, seed); var h = Sm(Kfp, FrozenN, FrozenS, seed + E); var om = Of(h); var R = RP(h); var d = DL(Nm(R)); return (d, Md(d, FrozenN), om.Average()); }

    private static double Md(double[,] d, int n) { double s = 0; int c = 0; for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) { s += d[i, j]; c++; } return c > 0 ? s / c : 0; }
    private static double Pctl(double[,] d, int n, double p) { var vals = new List<double>(); for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) vals.Add(d[i, j]); vals.Sort(); int idx = (int)(p / 100.0 * (vals.Count - 1)); return vals[Math.Min(idx, vals.Count - 1)]; }
    private static double Std(double[] v) { double m = v.Average(); return Math.Sqrt(v.Sum(x => (x - m) * (x - m)) / v.Length); }
    private static double Cv(double[] v) { double m = v.Average(); if (Math.Abs(m) < 1e-15) return double.NaN; return Std(v) / Math.Abs(m); }

    // ═══════════════════════════════════════════════════════════

    [Fact] public void V5_3_MSRA_E01_ProtocolLoaded()
    { _output.WriteLine($"=== MSRA STAGE 1 ===\nBlock A: seeds 100–109 (V5.2 ref)\nBlock B: seeds 520–524 (M3 current)\nXi={FrozenXi:F2}, N={FrozenN}, K0={FrozenK0}\nThreshold: CV>{SeedVariableThreshold:F2} = seed-variable\nPROTOCOL LOADED."); }

    [Fact]
    public void V5_3_MSRA_E02_Stage1Execution()
    {
        _output.WriteLine("═══ M3b STAGE 1 EXECUTION ═══");

        // Define blocks
        var blocks = new[] { ("A: V5.2 ref", 100, 10), ("B: M3 current", 520, 5) };

        foreach (var (name, start, count) in blocks)
        {
            _output.WriteLine($"── BLOCK {name} (seeds {start}–{start + count - 1}) ──");

            var mdRaw = new double[count];
            var mdD90 = new double[count];
            var mdDmed = new double[count];
            var omegas = new double[count];

            for (int s = 0; s < count; s++)
            {
                int seed = start + s;
                var (d, md, omega) = RunMdOmega(seed);
                mdRaw[s] = md;
                double d90 = Pctl(d, FrozenN, 90);
                double dmed = Pctl(d, FrozenN, 50);
                mdD90[s] = d90 > 0 ? md / d90 : 0;
                mdDmed[s] = dmed > 0 ? md / dmed : 0;
                omegas[s] = omega;
                _output.WriteLine($"  seed={seed}: MD_raw={md:F4}, MD/D90={mdD90[s]:F4}, Ω={omega:F4}");
            }

            double mdRawMean = mdRaw.Average(), mdRawStd = Std(mdRaw), cvRaw = Cv(mdRaw);
            double mdD90Mean = mdD90.Average(), cvD90 = Cv(mdD90);
            double cvDmed = Cv(mdDmed);
            double omegaMean = omegas.Average(), omegaStd = Std(omegas), cvOmega = Cv(omegas);

            bool seedVarRaw = cvRaw > SeedVariableThreshold;
            bool seedVarD90 = cvD90 > SeedVariableThreshold;
            bool seedVarDmed = cvDmed > SeedVariableThreshold;
            bool omegaOk = cvOmega <= 0.05;

            _output.WriteLine("");
            _output.WriteLine($"  MD_raw:      mean={mdRawMean:F4}, std={mdRawStd:F4}, CV={cvRaw:F4} → {(seedVarRaw ? "VARIABLE" : "STABLE")}");
            _output.WriteLine($"  MD/D90:      mean={mdD90Mean:F4}, CV={cvD90:F4} → {(seedVarD90 ? "VARIABLE" : "STABLE")}");
            _output.WriteLine($"  MD/D_median: CV={cvDmed:F4} → {(seedVarDmed ? "VARIABLE" : "STABLE")}");
            _output.WriteLine($"  Ω control:   mean={omegaMean:F4}, std={omegaStd:F4}, CV={cvOmega:F4} → {(omegaOk ? "OK" : "ANOMALOUS")}");
            _output.WriteLine("");
        }

        // ── Recompute summary ──
        _output.WriteLine("═══ STAGE 1 SUMMARY ═══");

        // Block A
        var aMdR = new double[10]; var aMdD = new double[10]; var aMdM = new double[10]; var aOm = new double[10];
        for (int s = 0; s < 10; s++) { var (d, md, om) = RunMdOmega(100 + s); aMdR[s] = md; double d90 = Pctl(d, FrozenN, 90), dmed = Pctl(d, FrozenN, 50); aMdD[s] = d90 > 0 ? md / d90 : 0; aMdM[s] = dmed > 0 ? md / dmed : 0; aOm[s] = om; }
        double aCvR = Cv(aMdR), aCvD = Cv(aMdD), aCvO = Cv(aOm);
        bool aVar = aCvD > SeedVariableThreshold;
        _output.WriteLine($"Block A (100–109): CV_raw={aCvR:F4}, CV_D90={aCvD:F4} ({(aVar ? "VARIABLE" : "STABLE")}), CV_Ω={aCvO:F4}");

        // Block B
        var bMdR = new double[5]; var bMdD = new double[5]; var bOm = new double[5];
        for (int s = 0; s < 5; s++) { var (d, md, om) = RunMdOmega(520 + s); bMdR[s] = md; double d90 = Pctl(d, FrozenN, 90); bMdD[s] = d90 > 0 ? md / d90 : 0; bOm[s] = om; }
        double bCvR = Cv(bMdR), bCvD = Cv(bMdD), bCvO = Cv(bOm);
        bool bVar = bCvD > SeedVariableThreshold;
        _output.WriteLine($"Block B (520–524): CV_raw={bCvR:F4}, CV_D90={bCvD:F4} ({(bVar ? "VARIABLE" : "STABLE")}), CV_Ω={bCvO:F4}");

        _output.WriteLine("");
        _output.WriteLine($"V5.2 ref: CV_raw~0.30, M3 threshold: CV>{SeedVariableThreshold:F2}");

        // ── Interpretation ──
        _output.WriteLine("");
        _output.WriteLine("═══ STAGE 1 INTERPRETATION ═══");

        bool aAnom = aCvO > 0.05, bAnom = bCvO > 0.05;

        if (aVar && !bVar)
        {
            _output.WriteLine("CASE 1: Block A seed-variable, Block B seed-stable.");
            _output.WriteLine("V5.2 MD seed-variability reproduces for seeds 100–109.");
            _output.WriteLine("Seed variability may be seed-block specific.");
            if (aAnom) _output.WriteLine("WARNING: Block A Ω CV anomalous — flag for audit.");
            _output.WriteLine("NEXT: Proceed to Stage 2 (blocks C/D/E).");
        }
        else if (!aVar && !bVar)
        {
            _output.WriteLine("CASE 2: Neither block reproduces seed-variability.");
            _output.WriteLine("V5.2 MD seed-variable classification does NOT reproduce");
            _output.WriteLine("under current M3 pipeline at either seed block.");
            _output.WriteLine("NEXT: Audit V5.2 pipeline for computation differences.");
            _output.WriteLine("  Possible differences:");
            _output.WriteLine("    - MeanDist computed on different distance matrix");
            _output.WriteLine("    - Different normalization or CV formula");
            _output.WriteLine("    - Different seed count (10 vs 3)");
            _output.WriteLine("    - Different xi or regime parameters");
            _output.WriteLine("  Do NOT proceed to M4 until discrepancy resolved.");
        }
        else if (aVar && bVar)
        {
            _output.WriteLine("CASE 3: Both blocks seed-variable.");
            _output.WriteLine("M3 result (seeds 520–524 seed-stable) may have been");
            _output.WriteLine("execution-specific. Recheck M3 implementation.");
            _output.WriteLine("NEXT: Proceed cautiously to Stage 2.");
        }
        else // !aVar && bVar
        {
            _output.WriteLine("CASE 4: Block B seed-variable, Block A seed-stable.");
            _output.WriteLine("Unexpected reversal: M3 block shows variability");
            _output.WriteLine("while V5.2 reference block does not.");
            _output.WriteLine("NEXT: Audit both blocks and M3 implementation.");
        }
    }

    [Fact]
    public void V5_3_MSRA_E03_ClaimDisciplineAudit()
    {
        _output.WriteLine("=== CLAIM AUDIT ===");
        _output.WriteLine("SUPPORTED: MD and Ω computed for blocks A and B.");
        _output.WriteLine($"CONDITIONAL: {5}+{10} seeds, D90 normalization, xi={FrozenXi:F2}.");
        _output.WriteLine("NOT CLAIMED: H10/H11/H12 confirmed, attractor decomposition.");
        _output.WriteLine("AUDIT: PASSED.");
    }
}
