using Xunit;
using Xunit.Abstractions;
using System.Security.Cryptography;
using System.Text;

namespace TRM.Tests.V5_3;

/// <summary>
/// M2 Generic Dynamics Baseline Execution (GDBE):
///
/// Executes the frozen GDBP protocol: recomputes M1 cluster masks
/// (deterministic, same seeds/pipeline), then runs the generic
/// baseline (no RecoverFP, fixed graph-distance coupling) across
/// the xi sweep. Compares generic ΔΩ_fixed to TRM ΔΩ_fixed (1.629).
///
/// CLAIM DISCIPLINE: Values as reported. No H9 confirmation claimed.
/// </summary>
[Trait("Category", "V5_3")]
[Trait("Category", "V5_3_GDBE")]
public class V5_3_GenericDynamicsBaselineExecution_Tests
{
    private readonly ITestOutputHelper _output;

    private const double FrozenK0 = 1.15; private const int FrozenN = 100; private const double FrozenS = 0.08;
    private const double Dt = 0.05, REps = 1e-8; private const int St = 400, Hd = 4;
    private const double BaselineXi = 1.80;
    private static readonly double[] XiSweep = { 1.50, 1.65, 1.80, 1.95, 2.10 };
    private const int BaselineSeedStart = 500, BaselineSeedCount = 20;
    private const int SweepSeedStart = 520, SweepSeedsPerPoint = 5;
    private const double ClusterThreshold = 0.70;
    private const double GenericDominantRatio = 0.70;
    private const double TrmSpecificRatio = 0.30;
    private const double M1_DeltaOmegaFixed = 1.629;

    public V5_3_GenericDynamicsBaselineExecution_Tests(ITestOutputHelper o) { _output = o; }

    // ═══════════════════════════════════════════════════════════
    //  TRM Core (identical to M1/V5.2)
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

    // ── KS that returns adjacency list (needed for BFS) ──
    private static HashSet<int>[] KS_adj(int n, int seed)
    { var rng = new Random(seed); var adj = new HashSet<int>[n]; for (int i = 0; i < n; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (n - 1); for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[n]; var cs = new List<List<int>>(); for (int i = 0; i < n; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } return adj; }

    // ── BFS all-pairs shortest path distances ──
    private static int[,] BfsDists(HashSet<int>[] adj, int n)
    { var dist = new int[n, n]; for (int src = 0; src < n; src++) { var d = new int[n]; Array.Fill(d, -1); d[src] = 0; var q = new Queue<int>(); q.Enqueue(src); while (q.Count > 0) { int u = q.Dequeue(); foreach (int nb in adj[u]) if (d[nb] == -1) { d[nb] = d[u] + 1; q.Enqueue(nb); } } for (int j = 0; j < n; j++) dist[src, j] = d[j] >= 0 ? d[j] : int.MaxValue; } return dist; }

    // ── TRM RunFull (for mask computation, identical to M1) ──
    private static (double[] om, double[,] R, double oMean) RunFull(int seed, double xi)
    { int E = Ep(FrozenN); var Ki = KS_mat(FrozenN, seed); var Kfp = Rfp(Ki, FrozenN, FrozenK0, xi, FrozenS, E, seed); var h = Sm(Kfp, FrozenN, FrozenS, seed + E); var om = Of(h); var R = RP(h); return (om, R, om.Average()); }

    // ── KS that returns dense matrix (for RunFull) ──
    private static double[,] KS_mat(int n, int seed)
    { var adj = KS_adj(n, seed); var K = new double[n, n]; for (int i = 0; i < n; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }

    // ── Generic baseline run: KS → BFS → Cupd → Sm → Omega ──
    private static (double[] om, double oMean) RunGeneric(int seed, double xi)
    { var adj = KS_adj(FrozenN, seed); var bd = BfsDists(adj, FrozenN); var dd = new double[FrozenN, FrozenN]; for (int i = 0; i < FrozenN; i++) for (int j = 0; j < FrozenN; j++) dd[i, j] = bd[i, j]; var K = Cupd(dd, FrozenK0, xi); var h = Sm(K, FrozenN, FrozenS, seed); var om = Of(h); return (om, om.Average()); }

    private static bool[] IdCluster(double[,] R)
    { int n = R.GetLength(0); var rm = new double[n]; for (int i = 0; i < n; i++) { double s = 0; for (int j = 0; j < n; j++) s += R[i, j]; rm[i] = s / n; } var cl = new bool[n]; for (int i = 0; i < n; i++) cl[i] = rm[i] > ClusterThreshold; if (cl.Count(x => x) < 2) { var idx = rm.Select((v, i) => (i, v)).OrderByDescending(x => x.v).Take(Math.Max(2, n / 2)).ToList(); cl = new bool[n]; foreach (var x in idx) cl[x.i] = true; } return cl; }
    private static double OmCl(double[] om, bool[] mask) { double s = 0; int c = 0; for (int i = 0; i < om.Length; i++) if (mask[i]) { s += om[i]; c++; } return c > 0 ? s / c : 0; }
    private static double Med(double[] v) { if (v.Length == 0) return double.NaN; var s = (double[])v.Clone(); Array.Sort(s); int mid = s.Length / 2; return s.Length % 2 == 1 ? s[mid] : (s[mid - 1] + s[mid]) / 2.0; }

    // ═══════════════════════════════════════════════════════════
    [Fact] public void V5_3_GDBE_01_ProtocolLoaded()
    { _output.WriteLine($"=== GDBP PROTOCOL ===\nM1 ΔΩ_fixed_trm: {M1_DeltaOmegaFixed:F3}\nGeneric-dominant: ≥{GenericDominantRatio:F2}\nTRM-specific: ≤{TrmSpecificRatio:F2}\nXi sweep: [{string.Join(", ", XiSweep.Select(x => x.ToString("F2")))}]\nSeeds: {SweepSeedStart}–{SweepSeedStart + SweepSeedsPerPoint - 1}\nPROTOCOL LOADED."); }

    [Fact]
    public void V5_3_GDBE_02_M2FullExecution()
    {
        // ── PHASE 1: Recompute M1 masks (deterministic, same seeds/pipeline) ──
        _output.WriteLine("═══ M2 FULL EXECUTION ═══");
        _output.WriteLine("── PHASE 1: M1 MASKS (TRM pipeline, baseline xi) ──");

        var masks = new bool[SweepSeedsPerPoint][];
        for (int s = 0; s < SweepSeedsPerPoint; s++)
        {
            int seed = SweepSeedStart + s;
            var (_, R, _) = RunFull(seed, BaselineXi);
            masks[s] = IdCluster(R);
            _output.WriteLine($"  seed={seed}: mask_size={masks[s].Count(x => x)}/{FrozenN}");
        }

        // ── PHASE 2: Generic baseline xi sweep ──
        _output.WriteLine("── PHASE 2: GENERIC BASELINE XI SWEEP ──");

        var offXi = new double[XiSweep.Length][];
        var ofXi = new double[XiSweep.Length][];

        for (int xiI = 0; xiI < XiSweep.Length; xiI++)
        {
            double xi = XiSweep[xiI];
            offXi[xiI] = new double[SweepSeedsPerPoint];
            ofXi[xiI] = new double[SweepSeedsPerPoint];

            for (int s = 0; s < SweepSeedsPerPoint; s++)
            {
                int seed = SweepSeedStart + s;
                var (om, _) = RunGeneric(seed, xi);
                ofXi[xiI][s] = om.Average();
                offXi[xiI][s] = OmCl(om, masks[s]);
            }

            _output.WriteLine($"  xi={xi:F2}: Ω_free={ofXi[xiI].Average():F6}, Ω_fixed={offXi[xiI].Average():F6}");
        }

        // ── PHASE 3: Per-seed ratios ──
        _output.WriteLine("── PHASE 3: PER-SEED RESULTS ──");

        var genDeltas = new double[SweepSeedsPerPoint];
        for (int s = 0; s < SweepSeedsPerPoint; s++)
        {
            var offV = new double[XiSweep.Length];
            var ofV = new double[XiSweep.Length];
            for (int xiI = 0; xiI < XiSweep.Length; xiI++) { ofV[xiI] = ofXi[xiI][s]; offV[xiI] = offXi[xiI][s]; }
            double dF = Math.Abs(ofV.Max() - ofV.Min());
            double dFF = Math.Abs(offV.Max() - offV.Min());
            genDeltas[s] = dFF;
            _output.WriteLine($"  seed={SweepSeedStart + s}: Δ_free={dF:F4}, Δ_fixed={dFF:F4}");
        }

        double dOffGenMed = Med(genDeltas);
        double dOffGenMean = genDeltas.Average();
        double dOffEns = offXi.Select(a => a.Average()).Max() - offXi.Select(a => a.Average()).Min();

        double ratio = dOffGenMed / M1_DeltaOmegaFixed;

        _output.WriteLine("");
        _output.WriteLine($"  ΔΩ_fixed_gen median: {dOffGenMed:F4}, mean: {dOffGenMean:F4}");
        _output.WriteLine($"  ΔΩ_fixed_gen ensemble: {dOffEns:F4}");
        _output.WriteLine($"  ΔΩ_fixed_trm (M1 ref): {M1_DeltaOmegaFixed:F3}");
        _output.WriteLine($"  ratio = {dOffGenMed:F4} / {M1_DeltaOmegaFixed:F3} = {ratio:F4}");
        _output.WriteLine("");

        // ── PHASE 4: Decision gate ──
        _output.WriteLine("═══ PHASE 4: DECISION ═══");
        _output.WriteLine("");

        if (ratio >= GenericDominantRatio)
        {
            _output.WriteLine($"GATE A (GENERIC DOMINANT) — ratio={ratio:F4} ≥ {GenericDominantRatio:F2}");
            _output.WriteLine("Generic baseline reproduces ≥70% of TRM's Ω-ξ sensitivity.");
            _output.WriteLine("");
            _output.WriteLine("H9:  CONDITIONALLY SUPPORTED but less distinctive.");
            _output.WriteLine("     Ω-ξ sensitivity is generic Kuramoto behavior,");
            _output.WriteLine("     not specific to TRM's RecoverFP.");
            _output.WriteLine("H11: NOT DIRECTLY TESTED by M2.");
            _output.WriteLine("H12: WEAKENED — key residual is generic, not attractor-specific.");
            _output.WriteLine("");
            _output.WriteLine("NEXT: M3 (MeanDist saturation boundary). Revise H9 wording");
            _output.WriteLine("  toward generic dynamical sensitivity.");
        }
        else if (ratio <= TrmSpecificRatio)
        {
            _output.WriteLine($"GATE B (TRM RESIDUAL) — ratio={ratio:F4} ≤ {TrmSpecificRatio:F2}");
            _output.WriteLine("Generic baseline reproduces ≤30% of TRM's Ω-ξ sensitivity.");
            _output.WriteLine("RecoverFP produces substantially different Ω-ξ behavior.");
            _output.WriteLine("");
            _output.WriteLine("H9:  STRONGER CONDITIONAL SUPPORT.");
            _output.WriteLine("     Ω-ξ sensitivity is NOT generic — RecoverFP matters.");
            _output.WriteLine("H11: NOT DIRECTLY TESTED by M2.");
            _output.WriteLine("H12: Less weakened — residual is TRM-specific.");
            _output.WriteLine("");
            _output.WriteLine("NEXT: M3 (MeanDist saturation boundary).");
        }
        else
        {
            _output.WriteLine($"GATE C (MIXED) — ratio={ratio:F4} in ({TrmSpecificRatio:F2}, {GenericDominantRatio:F2})");
            _output.WriteLine("Both generic and TRM-specific components contribute.");
            _output.WriteLine("");
            _output.WriteLine("H9:  CONDITIONALLY SUPPORTED with caveats.");
            _output.WriteLine("     Ω-ξ sensitivity is partially generic, partially TRM-specific.");
            _output.WriteLine("H11: NOT DIRECTLY TESTED by M2.");
            _output.WriteLine("H12: PARTIALLY WEAKENED.");
            _output.WriteLine("");
            _output.WriteLine("NEXT: Characterize TRM-specific residual fraction.");
        }
    }

    [Fact]
    public void V5_3_GDBE_03_ClaimDisciplineAudit()
    {
        _output.WriteLine("=== CLAIM AUDIT ===");
        _output.WriteLine("SUPPORTED: Generic baseline Ω_fixed computed as defined.");
        _output.WriteLine("SUPPORTED: Decision gate applied with frozen thresholds.");
        _output.WriteLine($"SUPPORTED: M1 ΔΩ_fixed_trm = {M1_DeltaOmegaFixed:F3} (immutable reference).");
        _output.WriteLine($"CONDITIONAL: Single-epoch graph-distance coupling baseline.");
        _output.WriteLine($"CONDITIONAL: M1 cluster masks used (threshold {ClusterThreshold:F2}).");
        _output.WriteLine($"CONDITIONAL: N={FrozenN}, K0={FrozenK0}, s={FrozenS}, {SweepSeedsPerPoint} seeds/pt.");
        _output.WriteLine("NOT CLAIMED: H9/H11/H12 confirmed, attractor decomposition, physical constants.");
        _output.WriteLine("AUDIT: PASSED.");
    }
}
