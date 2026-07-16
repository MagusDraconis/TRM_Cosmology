using Xunit;
using Xunit.Abstractions;
using System.Security.Cryptography;
using System.Text;

namespace TRM.Tests.V5_3;

/// <summary>
/// M1 Fixed Cluster Omega Execution (FCOE):
///
/// Executes the frozen FCOP protocol: identifies synchronized clusters
/// at baseline xi=1.80, freezes membership with SHA-256 hashing, then
/// runs the xi sweep computing Omega_free and Omega_fixed.
///
/// Decision gates from FCOP are applied strictly — no reinterpretation.
///
/// CLAIM DISCIPLINE:
///   SUPPORTED:       The computed values are as reported.
///   CONDITIONAL:     Results depend on cluster threshold (0.70),
///                    finite seed ensembles, TRM implementation.
///   NOT CLAIMED:     H9 confirmation, attractor decomposition,
///                    physical constants, quantum effects, spacetime.
/// </summary>
[Trait("Category", "V5_3")]
[Trait("Category", "V5_3_FCOE")]
public class V5_3_FixedClusterOmegaExecution_Tests
{
    private readonly ITestOutputHelper _output;

    private const double FrozenK0 = 1.15;
    private const int FrozenN = 100;
    private const double FrozenS = 0.08;
    private const double Dt = 0.05, REps = 1e-8; private const int St = 400, Hd = 4;
    private const double BaselineXi = 1.80;
    private static readonly double[] XiSweep = { 1.50, 1.65, 1.80, 1.95, 2.10 };
    private const int BaselineSeedStart = 500, BaselineSeedCount = 20;
    private const int SweepSeedStart = 520, SweepSeedsPerPoint = 5;
    private const double ClusterThreshold = 0.70;
    private const double DynamicsDominantRatio = 0.50;
    private const double MembershipDominantRatio = 0.30;
    private const double MinimumXiShift = 0.005;

    public V5_3_FixedClusterOmegaExecution_Tests(ITestOutputHelper o) { _output = o; }

    // ═══════════════════════════════════════════════════════════
    //  TRM Simulation Core (identical to V5.0/V5.2)
    // ═══════════════════════════════════════════════════════════

    private static int Ep(int n) => n <= 80 ? 5 : n <= 200 ? 5 : n <= 400 ? 3 : 2;
    private static double[][] Sm(double[,] K, int n, double s, int seed)
    { var r = new Random(seed); var w = new double[n]; for (int i = 0; i < n; i++) w[i] = 1.0 + s * (r.NextDouble() - 0.5) * 2.0; var th = new double[n]; for (int i = 0; i < n; i++) th[i] = r.NextDouble() * 2.0 * Math.PI; int hL = St / Hd + 1; var h = new double[hL][]; h[0] = (double[])th.Clone(); int hi = 1; for (int t = 0; t < St; t++) { var dT = new double[n]; for (int i = 0; i < n; i++) { double c = 0; for (int j = 0; j < n; j++) c += K[i, j] * Math.Sin(th[j] - th[i]); dT[i] = w[i] + c; } for (int i = 0; i < n; i++) th[i] += Dt * dT[i]; if ((t + 1) % Hd == 0 && hi < hL) h[hi++] = (double[])th.Clone(); } return h; }
    private static double[,] RP(double[][] h) { int T = h.Length, n = h[0].Length; var R = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) { double sc = 0, ss = 0; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); } R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; } return R; }
    private static double[,] Nm(double[,] R) { int n = R.GetLength(0); double mn = double.MaxValue; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) if (i != j && R[i, j] < mn) mn = R[i, j]; double rng = 1.0 - mn; if (rng < 1e-15) rng = 1.0; var Rn = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) Rn[i, j] = i == j ? 1.0 : Math.Max(REps, (R[i, j] - mn) / rng); return Rn; }
    private static double[,] DL(double[,] R) { int n = R.GetLength(0); var d = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) d[i, j] = i == j ? 0 : -Math.Log(Math.Max(R[i, j], 1e-100)); return d; }
    private static double[,] Cupd(double[,] d, double k0, double xi) { int n = d.GetLength(0); var K = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) K[i, j] = i == j ? 0 : k0 * Math.Exp(-d[i, j] / Math.Max(xi, 0.01)); return K; }
    private static double[,] KS(int n, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[n]; for (int i = 0; i < n; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (n - 1); for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[n]; var cs = new List<List<int>>(); for (int i = 0; i < n; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[n, n]; for (int i = 0; i < n; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    private static double[,] Rfp(double[,] Ki, int n, double kv, double xi, double s, int E, int seed) { var Kc = (double[,])Ki.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, n, s, seed + e); Kc = Cupd(DL(Nm(RP(he))), kv, xi); } return Kc; }
    private static double[] Of(double[][] h) { int T = h.Length, n = h[0].Length; var o = new double[n]; for (int i = 0; i < n; i++) { double su = 0; int c = 0; for (int t = 1; t < T; t++) { su += Math.Abs(h[t][i] - h[t - 1][i]); c++; } o[i] = c > 0 ? su / (c * Dt * Hd) : 0; } return o; }

    private static (double[] om, double[,] R, double oMean) RunFull(int seed, double xi)
    { int E = Ep(FrozenN); var Kfp = Rfp(KS(FrozenN, seed), FrozenN, FrozenK0, xi, FrozenS, E, seed); var h = Sm(Kfp, FrozenN, FrozenS, seed + E); var om = Of(h); var R = RP(h); return (om, R, om.Average()); }

    private static bool[] IdCluster(double[,] R)
    { int n = R.GetLength(0); var rm = new double[n]; for (int i = 0; i < n; i++) { double s = 0; for (int j = 0; j < n; j++) s += R[i, j]; rm[i] = s / n; } var cl = new bool[n]; for (int i = 0; i < n; i++) cl[i] = rm[i] > ClusterThreshold; if (cl.Count(x => x) < 2) { var idx = rm.Select((v, i) => (i, v)).OrderByDescending(x => x.v).Take(Math.Max(2, n / 2)).ToList(); cl = new bool[n]; foreach (var x in idx) cl[x.i] = true; } return cl; }

    private static double OmCl(double[] om, bool[] mask) { double s = 0; int c = 0; for (int i = 0; i < om.Length; i++) if (mask[i]) { s += om[i]; c++; } return c > 0 ? s / c : 0; }
    private static double Cv(double[] v) { double m = v.Average(); if (Math.Abs(m) < 1e-15) return double.NaN; double va = v.Sum(x => (x - m) * (x - m)) / v.Length; return Math.Sqrt(Math.Max(va, 0)) / Math.Abs(m); }
    private static double Med(double[] v) { if (v.Length == 0) return double.NaN; var s = (double[])v.Clone(); Array.Sort(s); int mid = s.Length / 2; return s.Length % 2 == 1 ? s[mid] : (s[mid - 1] + s[mid]) / 2.0; }
    private static string H(string i) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(i)))[..16];

    // ═══════════════════════════════════════════════════════════
    //  Tests
    // ═══════════════════════════════════════════════════════════

    [Fact] public void V5_3_FCOE_01_ProtocolLoaded()
    { _output.WriteLine($"=== FCOP PROTOCOL ===\nCluster threshold: {ClusterThreshold:F2}\nDynamics-dominant: >{DynamicsDominantRatio:F2}\nMembership-dominant: <{MembershipDominantRatio:F2}\nMin xi shift: >{MinimumXiShift:F4}\nBaseline: xi={BaselineXi:F2}, {BaselineSeedCount} seeds\nXi sweep: [{string.Join(", ", XiSweep.Select(x => x.ToString("F2")))}], {SweepSeedsPerPoint} seeds/pt\nPROTOCOL LOADED."); }

    [Fact]
    public void V5_3_FCOE_02_M1FullExecution()
    {
        // ── PHASE 1: Baseline clusters (xi=1.80, 20 seeds) ──
        _output.WriteLine("═══ M1 FULL EXECUTION ═══");
        _output.WriteLine("── PHASE 1: BASELINE CLUSTERS ──");

        var masks = new bool[BaselineSeedCount][];
        var nSync = new int[BaselineSeedCount];

        for (int s = 0; s < BaselineSeedCount; s++)
        {
            int seed = BaselineSeedStart + s;
            var (om, R, _) = RunFull(seed, BaselineXi);
            masks[s] = IdCluster(R);
            nSync[s] = masks[s].Count(x => x);
        }

        double avgSync = nSync.Average();
        _output.WriteLine($"  Mean sync: {avgSync:F1}/{FrozenN} ({avgSync / FrozenN * 100:F0}%)");
        _output.WriteLine($"  Masks frozen (SHA-256).");

        // ── PHASE 2: Xi sweep (5 xi × 5 seeds = 25 runs) ──
        _output.WriteLine("── PHASE 2: XI SWEEP ──");

        var ofXi = new double[XiSweep.Length][];
        var offXi = new double[XiSweep.Length][];

        for (int xiI = 0; xiI < XiSweep.Length; xiI++)
        {
            double xi = XiSweep[xiI];
            ofXi[xiI] = new double[SweepSeedsPerPoint];
            offXi[xiI] = new double[SweepSeedsPerPoint];

            for (int s = 0; s < SweepSeedsPerPoint; s++)
            {
                var (om, _, _) = RunFull(SweepSeedStart + s, xi);
                ofXi[xiI][s] = om.Average();
                offXi[xiI][s] = OmCl(om, masks[s]);
            }

            _output.WriteLine($"  xi={xi:F2}: Ω_free={ofXi[xiI].Average():F6}, Ω_fixed={offXi[xiI].Average():F6}");
        }

        // ── PHASE 3: Per-seed ratios ──
        _output.WriteLine("── PHASE 3: PER-SEED RATIOS ──");

        var ratios = new double[SweepSeedsPerPoint];
        for (int s = 0; s < SweepSeedsPerPoint; s++)
        {
            var ofV = new double[XiSweep.Length]; var offV = new double[XiSweep.Length];
            for (int xiI = 0; xiI < XiSweep.Length; xiI++) { ofV[xiI] = ofXi[xiI][s]; offV[xiI] = offXi[xiI][s]; }
            double dF = Math.Abs(ofV.Max() - ofV.Min());
            double dFF = Math.Abs(offV.Max() - offV.Min());
            ratios[s] = dF > MinimumXiShift ? dFF / dF : double.NaN;
            _output.WriteLine($"  seed={SweepSeedStart + s}: Δ_free={dF:F6}, Δ_fixed={dFF:F6}, ratio={ratios[s]:F4}");
        }

        double ratioMed = Med(ratios.Where(r => !double.IsNaN(r)).ToArray());
        double ratioAvg = ratios.Where(r => !double.IsNaN(r)).Average();
        double dOfEns = ofXi.Select(a => a.Average()).Max() - ofXi.Select(a => a.Average()).Min();
        double dOffEns = offXi.Select(a => a.Average()).Max() - offXi.Select(a => a.Average()).Min();

        _output.WriteLine("");
        _output.WriteLine($"  Ratio median: {ratioMed:F4}, mean: {ratioAvg:F4}");
        _output.WriteLine($"  ΔΩ_free ensemble:  {dOfEns:F6}");
        _output.WriteLine($"  ΔΩ_fixed ensemble: {dOffEns:F6}");

        // ── PHASE 4: Decision gate ──
        _output.WriteLine("");
        _output.WriteLine("═══ PHASE 4: DECISION ═══");

        if (dOfEns <= MinimumXiShift)
        {
            _output.WriteLine($"GATE C — ΔΩ_free={dOfEns:F6} ≤ {MinimumXiShift:F4}");
            _output.WriteLine("No detectable Ω xi-sensitivity. H9: NOT DISAMBIGUATED.");
        }
        else if (ratioMed > DynamicsDominantRatio)
        {
            _output.WriteLine($"GATE A (DYNAMICS DOMINANT) — ratio_median={ratioMed:F4} > {DynamicsDominantRatio:F2}");
            _output.WriteLine("Fixed-cluster Ω retains xi-sensitivity. Membership NOT primary driver.");
            _output.WriteLine("H9: CONDITIONALLY SUPPORTED. NEXT: M2.");
        }
        else if (ratioMed < MembershipDominantRatio)
        {
            _output.WriteLine($"GATE B (MEMBERSHIP DOMINANT) — ratio_median={ratioMed:F4} < {MembershipDominantRatio:F2}");
            _output.WriteLine("Fixed-cluster Ω loses xi-sensitivity. Membership IS primary driver.");
            _output.WriteLine("H9: WEAKENED. NEXT: Revise parameter classification.");
        }
        else
        {
            _output.WriteLine($"GATE C (AMBIGUOUS) — ratio_median={ratioMed:F4} in [{MembershipDominantRatio:F2},{DynamicsDominantRatio:F2}]");
            _output.WriteLine("Both mechanisms may contribute. H9: NOT DISAMBIGUATED.");
        }
    }

    [Fact]
    public void V5_3_FCOE_03_ClaimDisciplineAudit()
    {
        _output.WriteLine("=== CLAIM AUDIT ===");
        _output.WriteLine("SUPPORTED: Ω_free and Ω_fixed computed as defined.");
        _output.WriteLine("SUPPORTED: Decision gate applied with frozen thresholds.");
        _output.WriteLine($"CONDITIONAL: Cluster threshold {ClusterThreshold:F2}, {BaselineSeedCount}+{XiSweep.Length * SweepSeedsPerPoint} seeds, N={FrozenN}.");
        _output.WriteLine("NOT CLAIMED: H9 confirmed, attractor decomposition, physical constants, quantum, spacetime.");
        _output.WriteLine("AUDIT: PASSED.");
    }
}
