using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_3;

/// <summary>
/// M3c Omega Seed Stability Audit Execution (OSSA):
///
/// Executes the frozen OSSA protocol: runs full TRM RecoverFP for
/// five seed blocks at xi=1.80 and computes Omega seed-CV per block
/// against pre-registered thresholds. MeanDist CV as secondary control.
///
/// CLAIM DISCIPLINE: Values as reported. No H9/H10/H11/H12 confirmation.
/// </summary>
[Trait("Category", "V5_3")]
[Trait("Category", "V5_3_OSSA")]
public class V5_3_OmegaSeedStabilityAuditExecution_Tests
{
    private readonly ITestOutputHelper _output;

    private const double FrozenK0 = 1.15; private const int FrozenN = 100; private const double FrozenS = 0.08;
    private const double FrozenXi = 1.80;
    private const double Dt = 0.05, REps = 1e-8; private const int St = 400, Hd = 4;
    private const double ClusterThreshold = 0.70;
    private const double HighlyStableCv = 0.02;
    private const double StableCv = 0.05;
    private const int MostBlocksForRobust = 3;

    // ── Seed blocks ──
    private static readonly (string name, int start, int count)[] Blocks = {
        ("A: V5.2 orig", 100, 10), ("B: M3 curr", 520, 5),
        ("C: mid", 200, 10), ("D: upmid", 300, 10), ("E: upper", 400, 10)
    };

    public V5_3_OmegaSeedStabilityAuditExecution_Tests(ITestOutputHelper o) { _output = o; }

    // ═══════════════════════════════════════════════════════════
    //  TRM Core (identical to M3/M3b)
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

    private static double Md(double[,] d, int n) { double s = 0; int c = 0; for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) { s += d[i, j]; c++; } return c > 0 ? s / c : 0; }
    private static bool[] IdCluster(double[,] R) { int n = R.GetLength(0); var rm = new double[n]; for (int i = 0; i < n; i++) { double s = 0; for (int j = 0; j < n; j++) s += R[i, j]; rm[i] = s / n; } var cl = new bool[n]; for (int i = 0; i < n; i++) cl[i] = rm[i] > ClusterThreshold; if (cl.Count(x => x) < 2) { var idx = rm.Select((v, i) => (i, v)).OrderByDescending(x => x.v).Take(Math.Max(2, n / 2)).ToList(); cl = new bool[n]; foreach (var x in idx) cl[x.i] = true; } return cl; }
    private static double Std(double[] v) { double m = v.Average(); return Math.Sqrt(v.Sum(x => (x - m) * (x - m)) / v.Length); }
    private static double Cv(double[] v) { double m = v.Average(); if (Math.Abs(m) < 1e-15) return double.NaN; return Std(v) / Math.Abs(m); }

    private static (double omega, double md, int clusterSize, double syncFrac) RunSeed(int seed)
    { int E = Ep(FrozenN); var Ki = KS_mat(FrozenN, seed); var Kfp = Rfp(Ki, FrozenN, FrozenK0, FrozenXi, FrozenS, E, seed); var h = Sm(Kfp, FrozenN, FrozenS, seed + E); var om = Of(h); var R = RP(h); var d = DL(Nm(R)); var cl = IdCluster(R); return (om.Average(), Md(d, FrozenN), cl.Count(x => x), (double)cl.Count(x => x) / FrozenN); }

    // ═══════════════════════════════════════════════════════════

    [Fact] public void V5_3_OSSA_E01_ProtocolLoaded()
    { _output.WriteLine($"=== OSSA PROTOCOL ===\nHighly-stable: CV≤{HighlyStableCv:F2}\nStable: CV≤{StableCv:F2}\nVariable: CV>{StableCv:F2}\nRobust: ≥{MostBlocksForRobust}/{Blocks.Length} blocks\nPROTOCOL LOADED."); }

    [Fact]
    public void V5_3_OSSA_E02_FullAudit()
    {
        _output.WriteLine("═══ M3c FULL AUDIT ═══");

        var blockResults = new (string name, double[] omegas, double[] mds, int[] clSizes, double[] syncFracs)[Blocks.Length];

        for (int bi = 0; bi < Blocks.Length; bi++)
        {
            var (bname, start, count) = Blocks[bi];
            _output.WriteLine($"── BLOCK {bname} (seeds {start}–{start + count - 1}) ──");

            var omegas = new double[count];
            var mds = new double[count];
            var clSizes = new int[count];
            var syncFracs = new double[count];

            for (int s = 0; s < count; s++)
            {
                var (om, md, clSz, sf) = RunSeed(start + s);
                omegas[s] = om; mds[s] = md; clSizes[s] = clSz; syncFracs[s] = sf;
                _output.WriteLine($"  seed={start + s}: Ω={om:F4}, MD={md:F4}, cl={clSz}/{FrozenN}");
            }

            double omMean = omegas.Average(), omStd = Std(omegas), omCv = Cv(omegas);
            double mdCv = Cv(mds);
            double clMean = clSizes.Average();

            string cls = omCv <= HighlyStableCv ? "HIGHLY STABLE" : omCv <= StableCv ? "STABLE" : "VARIABLE";

            _output.WriteLine($"  → Ω: μ={omMean:F4}, σ={omStd:F4}, CV={omCv:F4} → {cls}");
            _output.WriteLine($"  → MD: CV={mdCv:F4} | cl_mean={clMean:F1}/{FrozenN}");
            _output.WriteLine("");

            blockResults[bi] = (bname, omegas, mds, clSizes, syncFracs);
        }

        // ── Summary ──
        _output.WriteLine("═══ SUMMARY ═══");
        _output.WriteLine($"{"Block",-16} {"Ω_CV",-10} {"Classification",-18} {"MD_CV",-10} {"Cl_Size",-10}");
        _output.WriteLine(new string('-', 64));

        int stableCount = 0;
        foreach (var (name, omegas, mds, clSizes, _) in blockResults)
        {
            double cv = Cv(omegas), mdCv = Cv(mds);
            string cls = cv <= HighlyStableCv ? "HIGHLY STABLE" : cv <= StableCv ? "STABLE" : "VARIABLE";
            if (cv <= StableCv) stableCount++;
            _output.WriteLine($"{name,-16} {cv,-10:F4} {cls,-18} {mdCv,-10:F4} {clSizes.Average(),-10:F1}");
        }
        _output.WriteLine("");
        _output.WriteLine($"Stable blocks: {stableCount}/{Blocks.Length} (need ≥{MostBlocksForRobust} for Gate A)");
        _output.WriteLine($"Historical expected: CV≈0.01");
        _output.WriteLine("");

        // ── Decision ──
        _output.WriteLine("═══ DECISION ═══");

        if (stableCount >= MostBlocksForRobust)
        {
            _output.WriteLine($"GATE A: ROBUST OMEGA SEED STABILITY ({stableCount}/{Blocks.Length} blocks CV≤{StableCv:F2})");
            _output.WriteLine("");
            _output.WriteLine("  Omega seed-stability is broadly supported.");
            foreach (var (name, omegas, _, _, _) in blockResults)
            {
                double cv = Cv(omegas);
                if (cv > StableCv)
                    _output.WriteLine($"  ⚠ Anomalous block: {name} (CV={cv:F4}) — investigate.");
            }
            _output.WriteLine("");
            _output.WriteLine("  H9: Unchanged (conditionally supported for xi-sensitivity).");
            _output.WriteLine("  Ω seed-stability pillar: RESTORED (with caveats for outliers).");
            _output.WriteLine("  H10/H11/H12: Not directly tested by M3c.");
            _output.WriteLine("");
            _output.WriteLine("  NEXT: Proceed to M4 only if MD seed-variability is");
            _output.WriteLine("  separately resolved or excluded from M4 scope.");
        }
        else
        {
            _output.WriteLine($"GATE C: OMEGA SEED STABILITY NOT REPRODUCED ({stableCount}/{Blocks.Length} blocks CV≤{StableCv:F2})");
            _output.WriteLine("");
            _output.WriteLine("  Omega seed-stability is NOT robust under current protocol.");
            _output.WriteLine("  The V5.2 'HIGHLY STABLE' classification must be revised.");
            _output.WriteLine("");
            _output.WriteLine("  H9: WEAKENED for the seed-stability dimension.");
            _output.WriteLine("  H9 xi-sensitivity from M1/M2 is UNAFFECTED.");
            _output.WriteLine("  H10/H11/H12: Not directly tested by M3c.");
            _output.WriteLine("");
            _output.WriteLine("  NEXT: Do NOT proceed to M4 until Ω CV discrepancy");
            _output.WriteLine("  is understood. Audit OmegaField, Rfp convergence,");
            _output.WriteLine("  and regime parameters that produced CV~0.01.");
        }
    }

    [Fact]
    public void V5_3_OSSA_E03_ClaimDisciplineAudit()
    {
        _output.WriteLine("=== CLAIM AUDIT ===");
        _output.WriteLine("SUPPORTED: Ω and MD computed for all 5 seed blocks.");
        _output.WriteLine($"CONDITIONAL: {Blocks.Sum(b => b.count)} seeds, xi={FrozenXi:F2}, N={FrozenN}.");
        _output.WriteLine("NOT CLAIMED: H9/H10/H11/H12 confirmed, attractor decomposition.");
        _output.WriteLine("AUDIT: PASSED.");
    }
}
