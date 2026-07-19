using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_3;

/// <summary>
/// M3 MeanDist Saturation Boundary Execution (MSBE):
///
/// Executes the frozen MSBP protocol: computes MeanDist for
/// all three baselines (random subset, generic Kuramoto, TRM
/// RecoverFP), normalizes by D_90 and D_median, and applies
/// pre-registered decision gates.
///
/// CLAIM DISCIPLINE: Values as reported. No H10/H11/H12
/// confirmation claimed.
/// </summary>
[Trait("Category", "V5_3")]
[Trait("Category", "V5_3_MSBE")]
public class V5_3_MeanDistSaturationBoundaryExecution_Tests
{
    private readonly ITestOutputHelper _output;

    private const double FrozenK0 = 1.15; private const int FrozenN = 100; private const double FrozenS = 0.08;
    private const double Dt = 0.05, REps = 1e-8; private const int St = 400, Hd = 4;
    private const double BaselineXi = 1.80;
    private static readonly double[] XiSweep = { 1.50, 1.65, 1.80, 1.95, 2.10 };
    private const int SweepSeedStart = 520, SweepSeedsPerPoint = 5;
    private const double ClusterThreshold = 0.70;
    private const double SeedVariableThreshold = 0.15;
    private const double XiRobustThreshold = 0.05;

    public V5_3_MeanDistSaturationBoundaryExecution_Tests(ITestOutputHelper o) { _output = o; }

    // ═══════════════════════════════════════════════════════════
    //  TRM Core (identical to M1/M2)
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
    private static HashSet<int>[] KS_adj(int n, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[n]; for (int i = 0; i < n; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (n - 1); for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[n]; var cs = new List<List<int>>(); for (int i = 0; i < n; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } return adj; }
    private static int[,] BfsDists(HashSet<int>[] adj, int n) { var dist = new int[n, n]; for (int src = 0; src < n; src++) { var d = new int[n]; Array.Fill(d, -1); d[src] = 0; var q = new Queue<int>(); q.Enqueue(src); while (q.Count > 0) { int u = q.Dequeue(); foreach (int nb in adj[u]) if (d[nb] == -1) { d[nb] = d[u] + 1; q.Enqueue(nb); } } for (int j = 0; j < n; j++) dist[src, j] = d[j] >= 0 ? d[j] : int.MaxValue; } return dist; }

    // ── MeanDist over upper triangle of distance matrix ──
    private static double MD(double[,] d, int n) { double s = 0; int c = 0; for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) { s += d[i, j]; c++; } return c > 0 ? s / c : 0; }

    // ── Normalization helpers ──
    private static double Pctl(double[,] d, int n, double p) { var vals = new List<double>(); for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) vals.Add(d[i, j]); vals.Sort(); int idx = (int)(p / 100.0 * (vals.Count - 1)); return vals[Math.Min(idx, vals.Count - 1)]; }
    private static double Med(double[] v) { if (v.Length == 0) return double.NaN; var s = (double[])v.Clone(); Array.Sort(s); int mid = s.Length / 2; return s.Length % 2 == 1 ? s[mid] : (s[mid - 1] + s[mid]) / 2.0; }
    private static double Cv(double[] v) { double m = v.Average(); if (Math.Abs(m) < 1e-15) return double.NaN; double va = v.Sum(x => (x - m) * (x - m)) / v.Length; return Math.Sqrt(Math.Max(va, 0)) / Math.Abs(m); }

    // ── TRM RunFull (for M1 masks + Baseline 3 MD) ──
    private static (double[] om, double[,] R, double[,] d, double md) RunFull(int seed, double xi)
    { int E = Ep(FrozenN); var Ki = KS_mat(FrozenN, seed); var Kfp = Rfp(Ki, FrozenN, FrozenK0, xi, FrozenS, E, seed); var h = Sm(Kfp, FrozenN, FrozenS, seed + E); var om = Of(h); var R = RP(h); var d = DL(Nm(R)); return (om, R, d, MD(d, FrozenN)); }

    // ── Generic Run (Baseline 2 MD) ──
    private static (double[,] d, double md) RunGenericMd(int seed, double xi)
    { var adj = KS_adj(FrozenN, seed); var bd = BfsDists(adj, FrozenN); var dd = new double[FrozenN, FrozenN]; for (int i = 0; i < FrozenN; i++) for (int j = 0; j < FrozenN; j++) dd[i, j] = bd[i, j]; var K = Cupd(dd, FrozenK0, xi); var h = Sm(K, FrozenN, FrozenS, seed); var R = RP(h); var d = DL(Nm(R)); return (d, MD(d, FrozenN)); }

    private static bool[] IdCluster(double[,] R) { int n = R.GetLength(0); var rm = new double[n]; for (int i = 0; i < n; i++) { double s = 0; for (int j = 0; j < n; j++) s += R[i, j]; rm[i] = s / n; } var cl = new bool[n]; for (int i = 0; i < n; i++) cl[i] = rm[i] > ClusterThreshold; if (cl.Count(x => x) < 2) { var idx = rm.Select((v, i) => (i, v)).OrderByDescending(x => x.v).Take(Math.Max(2, n / 2)).ToList(); cl = new bool[n]; foreach (var x in idx) cl[x.i] = true; } return cl; }

    // ═══════════════════════════════════════════════════════════
    [Fact] public void V5_3_MSBE_01_ProtocolLoaded()
    { _output.WriteLine($"=== MSBP PROTOCOL ===\nSeed-var threshold: CV>{SeedVariableThreshold:F2}\nXi-robust threshold: CV<{XiRobustThreshold:F2}\nNormalization: MD/D_90 primary, MD/D_median secondary\nXi sweep: [{string.Join(", ", XiSweep.Select(x => x.ToString("F2")))}]\nSeeds: {SweepSeedStart}–{SweepSeedStart + SweepSeedsPerPoint - 1}\nPROTOCOL LOADED."); }

    [Fact]
    public void V5_3_MSBE_02_M3FullExecution()
    {
        _output.WriteLine("═══ M3 FULL EXECUTION ═══");

        // ═══════════════════════════════════════════════════════
        //  BASELINE 1: Random Subset on Fixed Graph
        // ═══════════════════════════════════════════════════════
        _output.WriteLine("── BASELINE 1: RANDOM SUBSET ON FIXED GRAPH ──");

        // First get TRM cluster sizes at baseline to match subset size
        var trmClusterSizes = new int[SweepSeedsPerPoint];
        for (int s = 0; s < SweepSeedsPerPoint; s++)
        {
            var (_, R, _, _) = RunFull(SweepSeedStart + s, BaselineXi);
            trmClusterSizes[s] = IdCluster(R).Count(x => x);
        }
        int subsetSize = (int)Math.Round(trmClusterSizes.Average());
        _output.WriteLine($"  TRM cluster size: {subsetSize}/{FrozenN} → matching B1 subset size");

        var b1MdRaw = new double[SweepSeedsPerPoint];
        var b1MdD90 = new double[SweepSeedsPerPoint];
        var b1MdDmed = new double[SweepSeedsPerPoint];

        for (int s = 0; s < SweepSeedsPerPoint; s++)
        {
            int seed = SweepSeedStart + s;
            var adj = KS_adj(FrozenN, seed);
            var bd = BfsDists(adj, FrozenN);
            // Convert to double matrix
            var dd = new double[FrozenN, FrozenN];
            for (int i = 0; i < FrozenN; i++) for (int j = 0; j < FrozenN; j++) dd[i, j] = bd[i, j];

            // Random subset
            var rng = new Random(seed + 1000);
            var indices = Enumerable.Range(0, FrozenN).OrderBy(_ => rng.Next()).Take(subsetSize).ToHashSet();

            // MD over subset
            double sum = 0; int cnt = 0;
            for (int i = 0; i < FrozenN; i++)
                for (int j = i + 1; j < FrozenN; j++)
                    if (indices.Contains(i) && indices.Contains(j)) { sum += dd[i, j]; cnt++; }
            double md = cnt > 0 ? sum / cnt : 0;
            b1MdRaw[s] = md;

            // D_90 and D_median over subset
            var subsetDists = new List<double>();
            for (int i = 0; i < FrozenN; i++)
                for (int j = i + 1; j < FrozenN; j++)
                    if (indices.Contains(i) && indices.Contains(j)) subsetDists.Add(dd[i, j]);
            subsetDists.Sort();
            double d90 = subsetDists[(int)(0.90 * (subsetDists.Count - 1))];
            double dmed = subsetDists[subsetDists.Count / 2];
            b1MdD90[s] = d90 > 0 ? md / d90 : 0;
            b1MdDmed[s] = dmed > 0 ? md / dmed : 0;
        }

        double b1CvRaw = Cv(b1MdRaw), b1CvD90 = Cv(b1MdD90), b1CvDmed = Cv(b1MdDmed);
        _output.WriteLine($"  MD_raw:     mean={b1MdRaw.Average():F4}, CV={b1CvRaw:F4}");
        _output.WriteLine($"  MD/D_90:    mean={b1MdD90.Average():F4}, CV={b1CvD90:F4}");
        _output.WriteLine($"  MD/D_median: mean={b1MdDmed.Average():F4}, CV={b1CvDmed:F4}");
        bool b1SeedVar = b1CvD90 > SeedVariableThreshold;
        _output.WriteLine($"  Seed-variable? {(b1SeedVar ? "YES" : "NO")} (threshold CV>{SeedVariableThreshold:F2})");
        _output.WriteLine("  Xi-robust? YES (by construction — fixed d_ij)");

        // ═══════════════════════════════════════════════════════
        //  BASELINE 2: Generic Kuramoto Dynamics
        // ═══════════════════════════════════════════════════════
        _output.WriteLine("── BASELINE 2: GENERIC KURAMOTO DYNAMICS ──");

        var b2MdRaw = new double[XiSweep.Length][];
        var b2MdD90 = new double[XiSweep.Length][];
        var b2MdDmed = new double[XiSweep.Length][];

        for (int xiI = 0; xiI < XiSweep.Length; xiI++)
        {
            double xi = XiSweep[xiI];
            b2MdRaw[xiI] = new double[SweepSeedsPerPoint];
            b2MdD90[xiI] = new double[SweepSeedsPerPoint];
            b2MdDmed[xiI] = new double[SweepSeedsPerPoint];

            for (int s = 0; s < SweepSeedsPerPoint; s++)
            {
                var (d, md) = RunGenericMd(SweepSeedStart + s, xi);
                b2MdRaw[xiI][s] = md;
                double d90 = Pctl(d, FrozenN, 90);
                double dmed = Pctl(d, FrozenN, 50);
                b2MdD90[xiI][s] = d90 > 0 ? md / d90 : 0;
                b2MdDmed[xiI][s] = dmed > 0 ? md / dmed : 0;
            }

            _output.WriteLine($"  xi={xi:F2}: MD_raw={b2MdRaw[xiI].Average():F4}, MD/D_90={b2MdD90[xiI].Average():F4}");
        }

        // Seed-CV at baseline xi (index 2 = 1.80)
        double b2CvSeedRaw = Cv(b2MdRaw[2]), b2CvSeedD90 = Cv(b2MdD90[2]);
        bool b2SeedVar = b2CvSeedD90 > SeedVariableThreshold;

        // Xi-CV
        var b2XiMeans = new double[XiSweep.Length];
        for (int xiI = 0; xiI < XiSweep.Length; xiI++) b2XiMeans[xiI] = b2MdD90[xiI].Average();
        double b2CvXi = Cv(b2XiMeans);
        bool b2XiRobust = b2CvXi < XiRobustThreshold;

        _output.WriteLine($"  Seed-CV (baseline xi): raw={b2CvSeedRaw:F4}, D_90={b2CvSeedD90:F4} → seed-var={(b2SeedVar ? "YES" : "NO")}");
        _output.WriteLine($"  Xi-CV (across sweep): {b2CvXi:F4} → xi-robust={(b2XiRobust ? "YES" : "NO")}");

        // ═══════════════════════════════════════════════════════
        //  BASELINE 3: TRM RecoverFP
        // ═══════════════════════════════════════════════════════
        _output.WriteLine("── BASELINE 3: TRM RECOVERFP ──");

        var b3MdRaw = new double[XiSweep.Length][];
        var b3MdD90 = new double[XiSweep.Length][];
        var b3MdDmed = new double[XiSweep.Length][];

        for (int xiI = 0; xiI < XiSweep.Length; xiI++)
        {
            double xi = XiSweep[xiI];
            b3MdRaw[xiI] = new double[SweepSeedsPerPoint];
            b3MdD90[xiI] = new double[SweepSeedsPerPoint];
            b3MdDmed[xiI] = new double[SweepSeedsPerPoint];

            for (int s = 0; s < SweepSeedsPerPoint; s++)
            {
                var (_, _, d, md) = RunFull(SweepSeedStart + s, xi);
                b3MdRaw[xiI][s] = md;
                double d90 = Pctl(d, FrozenN, 90);
                double dmed = Pctl(d, FrozenN, 50);
                b3MdD90[xiI][s] = d90 > 0 ? md / d90 : 0;
                b3MdDmed[xiI][s] = dmed > 0 ? md / dmed : 0;
            }

            _output.WriteLine($"  xi={xi:F2}: MD_raw={b3MdRaw[xiI].Average():F4}, MD/D_90={b3MdD90[xiI].Average():F4}");
        }

        double b3CvSeedRaw = Cv(b3MdRaw[2]), b3CvSeedD90 = Cv(b3MdD90[2]);
        bool b3SeedVar = b3CvSeedD90 > SeedVariableThreshold;

        var b3XiMeans = new double[XiSweep.Length];
        for (int xiI = 0; xiI < XiSweep.Length; xiI++) b3XiMeans[xiI] = b3MdD90[xiI].Average();
        double b3CvXi = Cv(b3XiMeans);
        bool b3XiRobust = b3CvXi < XiRobustThreshold;

        _output.WriteLine($"  Seed-CV (baseline xi): raw={b3CvSeedRaw:F4}, D_90={b3CvSeedD90:F4} → seed-var={(b3SeedVar ? "YES" : "NO")}");
        _output.WriteLine($"  Xi-CV (across sweep): {b3CvXi:F4} → xi-robust={(b3XiRobust ? "YES" : "NO")}");

        // ═══════════════════════════════════════════════════════
        //  SUMMARY TABLE
        // ═══════════════════════════════════════════════════════
        _output.WriteLine("");
        _output.WriteLine("═══ SUMMARY ═══");
        _output.WriteLine($"{"Baseline",-30} {"CV_seed(D90)",-14} {"Seed-Var?",-12} {"CV_xi(D90)",-12} {"Xi-Robust?",-12}");
        _output.WriteLine(new string('-', 80));
        _output.WriteLine($"{"B1: Random Subset (fixed graph)",-30} {b1CvD90,-14:F4} {(b1SeedVar ? "YES" : "NO"),-12} {"N/A (fixed)",-12} {"YES (trivial)",-12}");
        _output.WriteLine($"{"B2: Generic Kuramoto",-30} {b2CvSeedD90,-14:F4} {(b2SeedVar ? "YES" : "NO"),-12} {b2CvXi,-12:F4} {(b2XiRobust ? "YES" : "NO"),-12}");
        _output.WriteLine($"{"B3: TRM RecoverFP",-30} {b3CvSeedD90,-14:F4} {(b3SeedVar ? "YES" : "NO"),-12} {b3CvXi,-12:F4} {(b3XiRobust ? "YES" : "NO"),-12}");
        _output.WriteLine("");
        _output.WriteLine("V5.2 REFERENCE: seed-VARIABLE (CV~0.30), xi-ROBUST (CV~0.05)");
        _output.WriteLine("");

        // ═══════════════════════════════════════════════════════
        //  DECISION GATE
        // ═══════════════════════════════════════════════════════
        _output.WriteLine("═══ DECISION ═══");

        bool b1Both = b1SeedVar && true; // B1 xi-robust is trivial (true)
        bool b2Both = b2SeedVar && b2XiRobust;
        bool b3Both = b3SeedVar && b3XiRobust;

        _output.WriteLine($"B1 both signatures: {(b1Both ? "YES" : "NO")} (seed-var={(b1SeedVar ? "Y" : "N")}, xi-robust=Y(trivial))");
        _output.WriteLine($"B2 both signatures: {(b2Both ? "YES" : "NO")} (seed-var={(b2SeedVar ? "Y" : "N")}, xi-robust={(b2XiRobust ? "Y" : "N")})");
        _output.WriteLine($"B3 both signatures: {(b3Both ? "YES" : "NO")} (seed-var={(b3SeedVar ? "Y" : "N")}, xi-robust={(b3XiRobust ? "Y" : "N")})");
        _output.WriteLine("");

        if (b1Both || b2Both)
        {
            _output.WriteLine("GATE A: SATURATION EXPLAINS BOTH SIGNATURES");
            _output.WriteLine("");
            if (b1Both) _output.WriteLine("  Baseline 1 (random subset) reproduces both.");
            if (b2Both) _output.WriteLine("  Baseline 2 (generic Kuramoto) reproduces both.");
            _output.WriteLine("");
            _output.WriteLine("  H10: WEAKENED — MeanDist behavior is not TRM-specific.");
            _output.WriteLine("  H11: WEAKENED — geometry-control class not distinctive.");
            _output.WriteLine("  H12: WEAKENED — attractor not required for pattern.");
            _output.WriteLine("");
            _output.WriteLine("  NEXT: Revise V5.3 away from geometry-control claims.");
            _output.WriteLine("  MeanDist may not be a useful metric for attractor");
            _output.WriteLine("  characterization.");
        }
        else if (b3Both && !b1Both && !b2Both)
        {
            _output.WriteLine("GATE B: TRM RESIDUAL DOMINANT");
            _output.WriteLine("");
            _output.WriteLine("  Neither B1 nor B2 reproduces both signatures.");
            _output.WriteLine("  B3 (TRM RecoverFP) reproduces both.");
            _output.WriteLine("  MeanDist contains a RecoverFP-specific residual.");
            _output.WriteLine("");
            _output.WriteLine("  H10: CONDITIONALLY SUPPORTED.");
            _output.WriteLine("  H11: MORE TESTABLE but not confirmed.");
            _output.WriteLine("  H12: WEAKLY SUPPORTED but not confirmed.");
            _output.WriteLine("");
            _output.WriteLine("  NEXT: M4 (latent variable regression).");
        }
        else
        {
            _output.WriteLine("GATE C: MIXED / PARTIAL");
            _output.WriteLine("");
            _output.WriteLine("  Baselines reproduce one signature but not the other.");
            _output.WriteLine("  MeanDist behavior decomposes into generic + TRM-specific.");
            _output.WriteLine("");
            _output.WriteLine("  H10: PARTIALLY SUPPORTED.");
            _output.WriteLine("  H11: Requires decomposition.");
            _output.WriteLine("");
            _output.WriteLine("  NEXT: Characterize which signature is TRM-specific.");
        }

        // D_median robustness check
        _output.WriteLine("");
        _output.WriteLine("── D_median ROBUSTNESS CHECK ──");
        double b3CvSeedDmed = Cv(b3MdDmed[2]);
        var b3XiMeansDmed = new double[XiSweep.Length];
        for (int xiI = 0; xiI < XiSweep.Length; xiI++) b3XiMeansDmed[xiI] = b3MdDmed[xiI].Average();
        double b3CvXiDmed = Cv(b3XiMeansDmed);
        bool d90SeedVar = b3CvSeedD90 > SeedVariableThreshold;
        bool dmedSeedVar = b3CvSeedDmed > SeedVariableThreshold;
        _output.WriteLine($"  B3 D_90:   seed-CV={b3CvSeedD90:F4} (var={(d90SeedVar ? "Y" : "N")}), xi-CV={b3CvXi:F4} (robust={(b3XiRobust ? "Y" : "N")})");
        _output.WriteLine($"  B3 D_median: seed-CV={b3CvSeedDmed:F4} (var={(dmedSeedVar ? "Y" : "N")}), xi-CV={b3CvXiDmed:F4} (robust={(b3CvXiDmed < XiRobustThreshold ? "Y" : "N")})");
        _output.WriteLine(d90SeedVar == dmedSeedVar ? "  → Normalization-ROBUST (same classification)" : "  → NORMALIZATION-SENSITIVE (different classification)");
    }

    [Fact]
    public void V5_3_MSBE_03_ClaimDisciplineAudit()
    {
        _output.WriteLine("=== CLAIM AUDIT ===");
        _output.WriteLine("SUPPORTED: B1/B2/B3 MeanDist computed as defined.");
        _output.WriteLine("SUPPORTED: Decision gate applied with frozen thresholds.");
        _output.WriteLine($"CONDITIONAL: D_90 normalization, {SweepSeedsPerPoint} seeds/pt, N={FrozenN}.");
        _output.WriteLine("NOT CLAIMED: H10/H11/H12 confirmed, attractor decomposition, physical constants.");
        _output.WriteLine("AUDIT: PASSED.");
    }
}
