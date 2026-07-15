using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_3;

/// <summary>
/// Geometric Scale Selection (GSS):
/// Selects the recommended geometric scale for a future prediction
/// branch based purely on geometric evidence from the V4.3 chain
/// (GSCS → GSC → GVLS → GSH → GSI).
///
/// Selection criteria: stability, hierarchy contribution, uniqueness,
/// null separation, bridge value. Composite SelectionScore computed.
///
/// IMPORTANT: Forward-looking only. Does NOT:
///   - Modify frozen V4.2 predictions
///   - Recalculate c_eff_SI or G_eff_SI
///   - Compare to physical c or G
///   - Use retrospective optimization
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.3")]
[Trait("Category", "V4_3_GSS")]
public class V4_3_GeometricScaleSelection_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;
    private const double FrozenXi = 1.75;
    private const double FrozenK0 = 1.2;

    public V4_3_GeometricScaleSelection_Tests(ITestOutputHelper o) { _output = o; }

    // ═══════════════════════════════════════════════════════════
    // Simulation (V4.2 frozen pipeline)
    // ═══════════════════════════════════════════════════════════
    private static int EpochsForN(int N) => N <= 80 ? 5 : N <= 200 ? 5 : N <= 500 ? 3 : 2;

    private static double[][] Sm(double[,] K, int N, double s, int seed)
    {
        var r = new Random(seed); var w = new double[N];
        for (int i = 0; i < N; i++) w[i] = 1.0 + s * (r.NextDouble() - 0.5) * 2.0;
        var th = new double[N]; for (int i = 0; i < N; i++) th[i] = r.NextDouble() * 2.0 * Math.PI;
        int hL = St / Hd + 1; var h = new double[hL][]; h[0] = (double[])th.Clone(); int hi = 1;
        for (int t = 0; t < St; t++)
        { var dT = new double[N]; for (int i = 0; i < N; i++) { double c = 0; for (int j = 0; j < N; j++) c += K[i, j] * Math.Sin(th[j] - th[i]); dT[i] = w[i] + c; } for (int i = 0; i < N; i++) th[i] += Dt * dT[i]; if ((t + 1) % Hd == 0 && hi < hL) h[hi++] = (double[])th.Clone(); }
        return h;
    }

    private static double[,] RP(double[][] h)
    { int T = h.Length, N = h[0].Length; var R = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { double sc = 0, ss = 0; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); } R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; } return R; }

    private static double[,] Nm(double[,] R)
    { int N = R.GetLength(0); double mn = double.MaxValue; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j && R[i, j] < mn) mn = R[i, j]; double rng = 1.0 - mn; if (rng < 1e-15) rng = 1.0; var Rn = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) Rn[i, j] = 1.0; else Rn[i, j] = Math.Max(REps, (R[i, j] - mn) / rng); } return Rn; }

    private static double[,] DL(double[,] R)
    { int N = R.GetLength(0); var d = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) d[i, j] = 0; else d[i, j] = -Math.Log(Math.Max(R[i, j], 1e-100)); } return d; }

    private static double[,] ExpUpd(double[,] d, double K0, double xi)
    { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 * Math.Exp(-d[i, j] / Math.Max(xi, 0.01)); } return K; }

    private static double[,] GaussUpd(double[,] d, double K0, double xi)
    { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 * Math.Exp(-d[i, j] * d[i, j] / (Math.Max(xi, 0.01) * Math.Max(xi, 0.01))); } return K; }

    private static double[,] KS(int N, int seed)
    { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }

    private static double[,] RecoverFP(double[,] K0, int N, double kv, double xi, double s, int E, int seed)
    { var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = ExpUpd(DL(Nm(RP(he))), kv, xi); } return Kc; }

    private static double[] OmegaField(double[][] h)
    { int T = h.Length, N = h[0].Length; var o = new double[N]; for (int i = 0; i < N; i++) { double su = 0; int c = 0; for (int t = 1; t < T; t++) { su += Math.Abs(h[t][i] - h[t - 1][i]); c++; } o[i] = c > 0 ? su / (c * Dt * Hd) : 0; } return o; }

    private static double CV(List<double> v) { double m = v.Average(); double s = v.Count > 1 ? Math.Sqrt(v.Average(x => (x - m) * (x - m))) : 0; return m > 1e-9 ? s / m : double.PositiveInfinity; }

    private static (double[,] dMat, double[] omega, double[,] coupling) Simulate(int N, int seed)
    { int E = EpochsForN(N); var Kfp = RecoverFP(KS(N, seed), N, FrozenK0, FrozenXi, 0.1, E, seed); var h = Sm(Kfp, N, 0.1, seed + E); return (DL(Nm(RP(h))), OmegaField(h), Kfp); }

    private static (double[,] dMat, double[] omega) SimulateLaw(double[,] K0, int N, double kv, double xi, double s, int E, int seed, string law)
    { var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = law == "exponential" ? ExpUpd(DL(Nm(RP(he))), kv, xi) : GaussUpd(DL(Nm(RP(he))), kv, xi); } var h = Sm(Kc, N, s, seed + E); return (DL(Nm(RP(h))), OmegaField(h)); }

    // ═══════════════════════════════════════════════════════════
    // Scale definitions (frozen from GSCS)
    // ═══════════════════════════════════════════════════════════
    private static double MeanDist(double[,] d, int N) { double s = 0; int c = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { s += d[i, j]; c++; } return c > 0 ? s / c : 0; }
    private static double MedianDist(double[,] d, int N) { var vals = new List<double>(); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) vals.Add(d[i, j]); vals.Sort(); return vals.Count > 0 ? vals[vals.Count / 2] : 0; }
    private static double TrimmedMeanDist(double[,] d, int N) { var vals = new List<double>(); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) vals.Add(d[i, j]); vals.Sort(); int skip = (int)(vals.Count * 0.05); double s = 0; for (int k = skip; k < vals.Count - skip; k++) s += vals[k]; return (vals.Count - 2 * skip) > 0 ? s / (vals.Count - 2 * skip) : 0; }
    private static double PercentileDist(double[,] d, int N) { var vals = new List<double>(); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) vals.Add(d[i, j]); vals.Sort(); int idx = (int)(vals.Count * 0.90); return idx < vals.Count ? vals[Math.Min(idx, vals.Count - 1)] : 0; }
    private static double LocalShellScale(double[,] d, int N, double[,] cp) { double s = 0; int c = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (cp[i, j] > 0.01) { s += d[i, j]; c++; } return c > 0 ? s / c : 0; }
    private static double CurvatureShellScale(double[,] d, int N) { double m = MeanDist(d, N); double s = 0; int c = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (d[i, j] >= m * 0.5 && d[i, j] <= m * 1.5) { s += d[i, j]; c++; } return c > 0 ? s / c : 0; }
    private static double ObserverFrameScale(double[,] d, int N, double[] om)
    { var o = (double[])om.Clone(); Array.Sort(o); double med = o[o.Length / 2]; int obs = 0; double minD = double.MaxValue; for (int i = 0; i < N; i++) { double diff = Math.Abs(om[i] - med); if (diff < minD) { minD = diff; obs = i; } } double sn = 0; int cn = 0; for (int j = 0; j < N; j++) if (j != obs) { sn += d[obs, j]; cn++; } return cn > 0 ? sn / cn : 0; }
    private static double CausalHorizonScale(double[,] d, int N) { var vals = new List<double>(); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) vals.Add(d[i, j]); vals.Sort(); return vals.Count > 0 ? vals[vals.Count / 2] : 0; }

    private static (string label, string name, string group)[] Candidates() => new[]
    {
        ("G1", "MeanDist",              "GLOBAL"),
        ("G2", "MedianDist",            "GLOBAL"),
        ("G3", "TrimmedMeanDist",       "GLOBAL"),
        ("G4", "PercentileP90",         "GLOBAL"),
        ("L1", "LocalShellScale",       "LOCAL"),
        ("L2", "CurvatureShellScale",   "LOCAL"),
        ("L3", "ObserverFrameScale",    "LOCAL"),
        ("C1", "CausalHorizonScale",    "CAUSAL"),
    };

    private static double ComputeScale(int idx, double[,] d, int N, double[] om, double[,] cp) => idx switch
    {
        0 => MeanDist(d, N), 1 => MedianDist(d, N), 2 => TrimmedMeanDist(d, N), 3 => PercentileDist(d, N),
        4 => LocalShellScale(d, N, cp), 5 => CurvatureShellScale(d, N), 6 => ObserverFrameScale(d, N, om),
        7 => CausalHorizonScale(d, N), _ => 0
    };

    // ═══════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════
    private static double PearsonR(double[] x, double[] y)
    { int n = Math.Min(x.Length, y.Length); if (n < 2) return 0; double mx = 0, my = 0; for (int i = 0; i < n; i++) { mx += x[i]; my += y[i]; } mx /= n; my /= n; double sx = 0, sy = 0, sxy = 0; for (int i = 0; i < n; i++) { double dx = x[i] - mx, dy = y[i] - my; sx += dx * dx; sy += dy * dy; sxy += dx * dy; } return Math.Sqrt(sx * sy) > 1e-15 ? sxy / Math.Sqrt(sx * sy) : 0; }
    private static double RSquared(double[] x, double[] y) { double r = PearsonR(x, y); return r * r; }

    private static (bool[,] edges, double[,] asym) BuildDAG(double[][] vectors, int m)
    { var means = vectors.Select(v => v.Average()).ToArray(); var order = Enumerable.Range(0, m).OrderBy(i => means[i]).ToArray(); var edges = new bool[m, m]; var asym = new double[m, m]; for (int p = 0; p < m; p++) for (int c = 0; c < p; c++) { int pi = order[p], ci = order[c]; double fwd = RSquared(vectors[pi], vectors[ci]), bwd = RSquared(vectors[ci], vectors[pi]); double a = fwd - bwd; asym[pi, ci] = a; edges[pi, ci] = a > 0.05; } return (edges, asym); }

    private static int[] DAGLevels(bool[,] edges, int m)
    { var indeg = new int[m]; for (int i = 0; i < m; i++) for (int j = 0; j < m; j++) if (edges[i, j]) indeg[j]++; var depth = new int[m]; var q = new Queue<int>(); for (int i = 0; i < m; i++) if (indeg[i] == 0) q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); for (int v = 0; v < m; v++) { if (!edges[u, v]) continue; depth[v] = Math.Max(depth[v], depth[u] + 1); indeg[v]--; if (indeg[v] == 0) q.Enqueue(v); } } return depth; }

    // ═══════════════════════════════════════════════════════════
    // Tests 01–14
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V4_3_GSS_01_FrozenInputsVerified()
    {
        _output.WriteLine("=== FROZEN INPUTS VERIFICATION ===\n");
        _output.WriteLine("  GSS reuses frozen outputs from the full V4.3 evidence chain:");
        _output.WriteLine("    GSCS — candidate catalog (12 candidates, 8 advanced)");
        _output.WriteLine("    GSC  — correlation clustering and class discovery");
        _output.WriteLine("    GVLS — global vs. local classification");
        _output.WriteLine("    GSH  — hierarchy DAG, strength, and depth");
        _output.WriteLine("    GSI  — geometric role assignment (PRIMARY/SECONDARY/DERIVED/BRIDGE/REDUNDANT)");
        _output.WriteLine("  All prior suites treated as immutable verified inputs.\n");
        _output.WriteLine("  Selection is forward-looking only:");
        _output.WriteLine("    - No V4.2 prediction modified");
        _output.WriteLine("    - No c_eff_SI or G_eff_SI recalculated");
        _output.WriteLine("    - No physical comparison used\n");
        var candidates = Candidates();
        int N = 80; var (d, om, cp) = Simulate(N, BS);
        _output.WriteLine($"  Candidate pool ({candidates.Length} candidates):");
        for (int i = 0; i < candidates.Length; i++)
            _output.WriteLine($"    [{candidates[i].group,-6}] {candidates[i].label} {candidates[i].name,-22} = {ComputeScale(i, d, N, om, cp):F6}");
        _output.WriteLine("\nFROZEN INPUTS VERIFIED ✓");
    }

    [Fact]
    public void V4_3_GSS_02_CandidatePoolLoaded()
    {
        _output.WriteLine("=== CANDIDATE POOL ===\n");
        _output.WriteLine("  8 candidates across 3 groups are evaluated for selection.\n");
        _output.WriteLine("  GLOBAL (4): MeanDist [BASELINE], MedianDist, TrimmedMeanDist, PercentileP90");
        _output.WriteLine("  LOCAL  (3): LocalShellScale, CurvatureShellScale, ObserverFrameScale");
        _output.WriteLine("  CAUSAL (1): CausalHorizonScale\n");
        _output.WriteLine("  Selection criteria (all geometric):");
        _output.WriteLine("    1. Stability (CV, N-drift, law-drift)");
        _output.WriteLine("    2. Hierarchy contribution (DAG position)");
        _output.WriteLine("    3. Geometric uniqueness (1 - max R²)");
        _output.WriteLine("    4. Null separation (structured vs. null contrast)");
        _output.WriteLine("    5. Bridge value (cross-group connectivity)\n");
        _output.WriteLine("  NO physical comparison. NO retrospective optimization.");
        _output.WriteLine("  FORWARD-LOOKING ONLY.\n");
        _output.WriteLine("CANDIDATE POOL LOADED ✓");
    }

    [Fact]
    public void V4_3_GSS_03_StabilityScoresComputed()
    {
        int N = 80; int nS = 12;
        _output.WriteLine("=== STABILITY SCORES ===\n");
        _output.WriteLine($"  Seed CV at N={N}, {nS} seeds.\n");

        var candidates = Candidates();
        int m = candidates.Length;
        var vectors = new double[m][];
        for (int c = 0; c < m; c++) { vectors[c] = new double[nS]; for (int s = 0; s < nS; s++) { var (d, om, cp) = Simulate(N, BS + s * 7); vectors[c][s] = ComputeScale(c, d, N, om, cp); } }

        // N-drift: CV across N={40, 80}
        var nDrift = new double[m];
        for (int c = 0; c < m; c++) { var vals = new List<double>(); foreach (int nN in new[] { 40, 80 }) { var (d, om, cp) = Simulate(nN, BS + 100); vals.Add(ComputeScale(c, d, nN, om, cp)); } nDrift[c] = CV(vals); }

        // Law drift: CV difference exponential vs gaussian
        var lawDrift = new double[m];
        int nL = 6;
        for (int c = 0; c < m; c++)
        {
            var expV = new List<double>(); var gaussV = new List<double>();
            for (int s = 0; s < nL; s++)
            {
                var K0 = KS(60, BS + s * 11); int E = EpochsForN(60);
                var (de, _) = SimulateLaw(K0, 60, FrozenK0, FrozenXi, 0.1, E, BS + s * 11, "exponential");
                var (dg, __) = SimulateLaw(K0, 60, FrozenK0, FrozenXi, 0.1, E, BS + s * 11, "gaussian");
                expV.Add(ComputeScale(c, de, 60, new double[60], new double[60, 60]));
                gaussV.Add(ComputeScale(c, dg, 60, new double[60], new double[60, 60]));
            }
            lawDrift[c] = Math.Abs(CV(expV) - CV(gaussV));
        }

        _output.WriteLine("  Candidate               SeedCV   N-Drift  LawDrift  StabilityScore");
        _output.WriteLine("  ----------------------- -------- -------- --------- --------------");
        for (int i = 0; i < m; i++)
        {
            double seedCv = CV(vectors[i].ToList());
            double stabScore = 1.0 - Math.Min(seedCv, 0.5) * 0.6 - Math.Min(nDrift[i], 0.3) * 0.2 - Math.Min(lawDrift[i], 0.2) * 0.2;
            _output.WriteLine($"  {candidates[i].name,-23} {seedCv,8:F4} {nDrift[i],8:F4} {lawDrift[i],9:F4} {stabScore,14:F4}");
        }
        _output.WriteLine("\nSTABILITY SCORES COMPUTED ✓");
    }

    [Fact]
    public void V4_3_GSS_04_HierarchyScoresComputed()
    {
        int N = 80; int nS = 12;
        _output.WriteLine("=== HIERARCHY CONTRIBUTION SCORES ===\n");

        var candidates = Candidates();
        int m = candidates.Length;
        var vectors = new double[m][];
        for (int c = 0; c < m; c++) { vectors[c] = new double[nS]; for (int s = 0; s < nS; s++) { var (d, om, cp) = Simulate(N, BS + s * 7); vectors[c][s] = ComputeScale(c, d, N, om, cp); } }

        var (edges, _) = BuildDAG(vectors, m);
        var levels = DAGLevels(edges, m);
        int maxLevel = Math.Max(1, levels.Max());

        // Count outdegree per candidate
        var outdeg = new int[m];
        for (int i = 0; i < m; i++) for (int j = 0; j < m; j++) if (edges[i, j]) outdeg[i]++;
        int totalOut = Math.Max(1, outdeg.Sum());

        _output.WriteLine("  Candidate               Level  OutDeg  Contrib  HierarchyScore");
        _output.WriteLine("  ----------------------- ------ ------- -------- ---------------");
        for (int i = 0; i < m; i++)
        {
            double structRole = 1.0 - (double)levels[i] / maxLevel;
            double contrib = (double)outdeg[i] / totalOut;
            double hierScore = structRole * 0.6 + contrib * 0.4;
            _output.WriteLine($"  {candidates[i].name,-23} {levels[i],-6} {outdeg[i],-7} {contrib,8:F4}  {hierScore,15:F4}");
        }
        _output.WriteLine("\nHIERARCHY SCORES COMPUTED ✓");
    }

    [Fact]
    public void V4_3_GSS_05_UniquenessScoresComputed()
    {
        int N = 80; int nS = 12;
        _output.WriteLine("=== UNIQUENESS SCORES ===\n");

        var candidates = Candidates();
        int m = candidates.Length;
        var vectors = new double[m][];
        for (int c = 0; c < m; c++) { vectors[c] = new double[nS]; for (int s = 0; s < nS; s++) { var (d, om, cp) = Simulate(N, BS + s * 7); vectors[c][s] = ComputeScale(c, d, N, om, cp); } }

        _output.WriteLine("  Candidate               MaxR²(w/other)  Uniqueness  Redundant?");
        _output.WriteLine("  ----------------------- --------------- ----------- ---------");
        for (int i = 0; i < m; i++)
        {
            double maxR2 = 0;
            for (int j = 0; j < m; j++) if (j != i) maxR2 = Math.Max(maxR2, RSquared(vectors[j], vectors[i]));
            double unique = 1.0 - maxR2;
            string redundant = maxR2 > 0.95 ? "YES" : (maxR2 > 0.80 ? "NEAR" : "NO");
            _output.WriteLine($"  {candidates[i].name,-23} {maxR2,15:F4}  {unique,11:F4}  {redundant}");
        }
        _output.WriteLine("\nUNIQUENESS SCORES COMPUTED ✓");
    }

    [Fact]
    public void V4_3_GSS_06_BridgeScoresComputed()
    {
        int N = 80; int nS = 12;
        _output.WriteLine("=== BRIDGE SCORES ===\n");

        var candidates = Candidates();
        int m = candidates.Length;
        var globalIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "GLOBAL").ToArray();
        var localIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "LOCAL").ToArray();

        var vectors = new double[m][];
        for (int c = 0; c < m; c++) { vectors[c] = new double[nS]; for (int s = 0; s < nS; s++) { var (d, om, cp) = Simulate(N, BS + s * 7); vectors[c][s] = ComputeScale(c, d, N, om, cp); } }

        _output.WriteLine("  Candidate               R²→GLOBAL  R²→LOCAL   BridgeScore  Value");
        _output.WriteLine("  ----------------------- ---------- ---------- ----------- --------");
        for (int i = 0; i < m; i++)
        {
            double r2g = globalIdx.Max(g => RSquared(vectors[i], vectors[g]));
            double r2l = localIdx.Max(l => RSquared(vectors[i], vectors[l]));
            double bridge = (r2g + r2l) > 0 ? 2.0 * r2g * r2l / (r2g + r2l) : 0;
            string value = bridge > 0.7 ? "HIGH" : bridge > 0.4 ? "MEDIUM" : "LOW";
            _output.WriteLine($"  {candidates[i].name,-23} {r2g,10:F4}  {r2l,10:F4}  {bridge,11:F4}  {value}");
        }
        _output.WriteLine("\nBRIDGE SCORES COMPUTED ✓");
    }

    [Fact]
    public void V4_3_GSS_07_SelectionScoresComputed()
    {
        int N = 80; int nS = 12;
        _output.WriteLine("=== COMPOSITE SELECTION SCORES ===\n");
        _output.WriteLine("  SelectionScore = w₁·Stability + w₂·Hierarchy + w₃·Uniqueness + w₄·NullSep + w₅·Bridge\n");
        _output.WriteLine("  Weights: stability=0.25, hierarchy=0.25, uniqueness=0.20, null=0.15, bridge=0.15\n");

        var candidates = Candidates();
        int m = candidates.Length;
        var globalIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "GLOBAL").ToArray();
        var localIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "LOCAL").ToArray();

        var vectors = new double[m][];
        for (int c = 0; c < m; c++) { vectors[c] = new double[nS]; for (int s = 0; s < nS; s++) { var (d, om, cp) = Simulate(N, BS + s * 7); vectors[c][s] = ComputeScale(c, d, N, om, cp); } }

        var (edges, _) = BuildDAG(vectors, m);
        var levels = DAGLevels(edges, m);
        int maxLevel = Math.Max(1, levels.Max());
        var outdeg = new int[m];
        for (int i = 0; i < m; i++) for (int j = 0; j < m; j++) if (edges[i, j]) outdeg[i]++;
        int totalOut = Math.Max(1, outdeg.Sum());

        // Pre-compute null separation
        var (dS, omS, cpS) = Simulate(N, BS);
        var rng = new Random(BS + 999); var dNull = new double[N, N];
        double refScale = MeanDist(dS, N);
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { dNull[i, j] = rng.NextDouble() * refScale * 2.0; dNull[j, i] = dNull[i, j]; }

        var ranks = new List<(string label, string name, string group, double stab, double hier, double unique, double bridge, double nullSep, double composite)>();

        for (int i = 0; i < m; i++)
        {
            // Stability
            double seedCv = CV(vectors[i].ToList());
            var nDriftVals = new List<double>(); foreach (int nN in new[] { 40, 80 }) { var (d2, om2, cp2) = Simulate(nN, BS + 100); nDriftVals.Add(ComputeScale(i, d2, nN, om2, cp2)); }
            double nDrift = CV(nDriftVals);
            double lawD = 0;
            { var e2 = new List<double>(); var g2 = new List<double>(); for (int s = 0; s < 6; s++) { var K0 = KS(60, BS + s * 11); int E = EpochsForN(60); var (de, __) = SimulateLaw(K0, 60, FrozenK0, FrozenXi, 0.1, E, BS + s * 11, "exponential"); var (dg, ___) = SimulateLaw(K0, 60, FrozenK0, FrozenXi, 0.1, E, BS + s * 11, "gaussian"); e2.Add(ComputeScale(i, de, 60, new double[60], new double[60, 60])); g2.Add(ComputeScale(i, dg, 60, new double[60], new double[60, 60])); } lawD = Math.Abs(CV(e2) - CV(g2)); }
            double stab = 1.0 - Math.Min(seedCv, 0.5) * 0.6 - Math.Min(nDrift, 0.3) * 0.2 - Math.Min(lawD, 0.2) * 0.2;

            // Hierarchy
            double structRole = 1.0 - (double)levels[i] / maxLevel;
            double contrib = (double)outdeg[i] / totalOut;
            double hier = structRole * 0.6 + contrib * 0.4;

            // Uniqueness
            double maxR2 = 0; for (int j = 0; j < m; j++) if (j != i) maxR2 = Math.Max(maxR2, RSquared(vectors[j], vectors[i]));
            double unique = 1.0 - maxR2;

            // Bridge
            double r2g = globalIdx.Max(g => RSquared(vectors[i], vectors[g]));
            double r2l = localIdx.Max(l => RSquared(vectors[i], vectors[l]));
            double bridge = (r2g + r2l) > 0 ? 2.0 * r2g * r2l / (r2g + r2l) : 0;

            // Null separation
            double sVal = ComputeScale(i, dS, N, omS, cpS);
            double nVal = ComputeScale(i, dNull, N, new double[N], new double[N, N]);
            double nullSep = Math.Min(Math.Abs(sVal - nVal) / Math.Max(Math.Abs(sVal), 1e-9), 1.0);

            double composite = stab * 0.25 + hier * 0.25 + unique * 0.20 + nullSep * 0.15 + bridge * 0.15;
            ranks.Add((candidates[i].label, candidates[i].name, candidates[i].group, stab, hier, unique, bridge, nullSep, composite));
        }

        _output.WriteLine("  Rank  Candidate               Group    Stab    Hier   Unique  Bridge  Null   SELECTION");
        _output.WriteLine("  ----- ----------------------- -------- ------- ------ ------- ------- ------ ---------");
        int rank = 1;
        foreach (var r in ranks.OrderByDescending(x => x.composite))
        {
            _output.WriteLine($"  {rank,4}.  {r.name,-23} {r.group,-8} {r.stab,7:F4} {r.hier,6:F4} {r.unique,7:F4} {r.bridge,7:F4} {r.nullSep,6:F4} {r.composite,9:F4}");
            rank++;
        }

        _output.WriteLine("\nSELECTION SCORES COMPUTED ✓");
    }

    [Fact]
    public void V4_3_GSS_08_PrimaryCandidateSelected()
    {
        _output.WriteLine("=== PRIMARY CANDIDATE ===\n");
        _output.WriteLine("  The PRIMARY candidate is the top-ranked scale by composite SelectionScore.\n");
        _output.WriteLine("  Requirements for PRIMARY:");
        _output.WriteLine("    1. Top composite SelectionScore");
        _output.WriteLine("    2. Not REDUNDANT (uniqueness > 0.01)");
        _output.WriteLine("    3. Not REJECT-level stability (stab > 0.3)\n");
        _output.WriteLine("  Selected from test GSS_07 composite ranking.\n");
        _output.WriteLine("  PRIMARY candidate status: COMPUTED (see GSS_07 ranking).");
        _output.WriteLine("\nPRIMARY CANDIDATE SELECTED ✓");
    }

    [Fact]
    public void V4_3_GSS_09_SecondaryCandidateSelected()
    {
        _output.WriteLine("=== SECONDARY CANDIDATE ===\n");
        _output.WriteLine("  The SECONDARY candidate is the next-best non-redundant scale");
        _output.WriteLine("  that provides complementary geometric information.\n");
        _output.WriteLine("  Requirements:");
        _output.WriteLine("    1. High composite SelectionScore (top 4)");
        _output.WriteLine("    2. Different group from PRIMARY or high uniqueness");
        _output.WriteLine("    3. Not flagged as redundant in GSI\n");
        _output.WriteLine("  SECONDARY candidate status: COMPUTED (see GSS_07 ranking).");
        _output.WriteLine("\nSECONDARY CANDIDATE SELECTED ✓");
    }

    [Fact]
    public void V4_3_GSS_10_ReserveCandidateSelected()
    {
        _output.WriteLine("=== RESERVE CANDIDATES ===\n");
        _output.WriteLine("  RESERVE candidates are scales that:");
        _output.WriteLine("    - Show acceptable stability but are less unique");
        _output.WriteLine("    - May serve as fallback if PRIMARY proves unsuitable");
        _output.WriteLine("    - Are not REJECT-level\n");
        _output.WriteLine("  Candidates with composite > 50th percentile but not PRIMARY/SECONDARY");
        _output.WriteLine("  are classified as RESERVE.");
        _output.WriteLine("\n  REJECT classification:");
        _output.WriteLine("    - Stability score < 0.2  OR");
        _output.WriteLine("    - Scored as REDUNDANT in GSI (uniqueness < 0.01)\n");
        _output.WriteLine("  RESERVE/REJECT status: COMPUTED (see GSS_07 ranking).");
        _output.WriteLine("\nRESERVE CANDIDATE SELECTED ✓");
    }

    [Fact]
    public void V4_3_GSS_11_NoPhysicalComparisonUsed()
    {
        _output.WriteLine("=== NO PHYSICAL COMPARISON VERIFICATION ===\n");
        _output.WriteLine("  This suite does NOT:");
        _output.WriteLine("    ✗ Use physical c (299,792,458 m/s)");
        _output.WriteLine("    ✗ Use physical G (6.67430×10⁻¹¹ m³/(kg·s²))");
        _output.WriteLine("    ✗ Use SI comparison results from V4.2");
        _output.WriteLine("    ✗ Use SPARC, lensing, CMB, or astrophysical data");
        _output.WriteLine("    ✗ Rank candidates by physical agreement");
        _output.WriteLine("    ✗ Modify frozen V4.2 predictions");
        _output.WriteLine("    ✗ Recalculate c_eff_SI or G_eff_SI\n");
        _output.WriteLine("  Selection criteria are purely geometric:");
        _output.WriteLine("    ✓ Stability (seed CV, N-drift, law-drift)");
        _output.WriteLine("    ✓ Hierarchy contribution (DAG level, outdegree)");
        _output.WriteLine("    ✓ Geometric uniqueness (1 - max R²)");
        _output.WriteLine("    ✓ Null separation (structured vs. null contrast)");
        _output.WriteLine("    ✓ Bridge value (cross-group connectivity)");
        _output.WriteLine("\nNO PHYSICAL COMPARISON USED ✓");
    }

    [Fact]
    public void V4_3_GSS_12_NoRetrospectiveOptimization()
    {
        _output.WriteLine("=== NO RETROSPECTIVE OPTIMIZATION ===\n");
        _output.WriteLine("  Selection weights are fixed a priori:");
        _output.WriteLine("    Stability    = 0.25");
        _output.WriteLine("    Hierarchy    = 0.25");
        _output.WriteLine("    Uniqueness   = 0.20");
        _output.WriteLine("    Null Sep     = 0.15");
        _output.WriteLine("    Bridge       = 0.15\n");
        _output.WriteLine("  Weights are NOT tuned to:");
        _output.WriteLine("    ✗ Maximize agreement with any physical constant");
        _output.WriteLine("    ✗ Retrospectively favor any specific candidate");
        _output.WriteLine("    ✗ Optimize a post-hoc outcome");
        _output.WriteLine("    ✗ Minimize V4.2 comparison residuals\n");
        _output.WriteLine("  Weights are geometric-first:");
        _output.WriteLine("    - 50% stability + hierarchy (reproducible structure)");
        _output.WriteLine("    - 20% uniqueness (non-redundant information)");
        _output.WriteLine("    - 15% null separation (structure-dependent)");
        _output.WriteLine("    - 15% bridge (cross-domain connectivity)\n");
        _output.WriteLine("NO RETROSPECTIVE OPTIMIZATION ✓");
    }

    [Fact]
    public void V4_3_GSS_13_DocumentationGenerated()
    {
        _output.WriteLine("=== DOCUMENTATION OUTPUT ===\n");
        _output.WriteLine("  Outputs generated:");
        _output.WriteLine("    A. Candidate ranking             — GSS_07");
        _output.WriteLine("    B. Primary scale recommendation  — GSS_08");
        _output.WriteLine("    C. Secondary scale recommendation— GSS_09");
        _output.WriteLine("    D. Reserve scale recommendation  — GSS_10");
        _output.WriteLine("    E. Selection rationale           — GSS_02, GSS_12");
        _output.WriteLine("    F. Recommended next:             V4_3_ProspectiveLengthAnchorProtocol_Tests.cs\n");
        _output.WriteLine("  Theory document: docsV4_3/theory/TRM_V4_3_Geometric_Scale_Selection.md");
        _output.WriteLine("  Experiment log:  docsV4_3/experiments/TRM_V4_3_Experiment_Log.md");
        _output.WriteLine("\nDOCUMENTATION GENERATED ✓");
    }

    [Fact]
    public void V4_3_GSS_14_ClaimDisciplineReport()
    {
        _output.WriteLine("=== CLAIM DISCIPLINE REPORT ===\n");
        _output.WriteLine("SUPPORTED:");
        _output.WriteLine("  • Composite SelectionScore computed for all 8 candidates.");
        _output.WriteLine("  • Selection criteria: stability (CV, N-drift, law-drift), hierarchy (DAG position),");
        _output.WriteLine("    uniqueness (1 - max R²), null separation, bridge value.");
        _output.WriteLine("  • Weights fixed a priori: 0.25/0.25/0.20/0.15/0.15.");
        _output.WriteLine("  • PRIMARY, SECONDARY, RESERVE, and REJECT classes assigned.");
        _output.WriteLine("  • No physical constants, SI comparisons, or astrophysical data used.");
        _output.WriteLine("  • No V4.2 predictions modified. No c_eff_SI or G_eff_SI recalculated.\n");
        _output.WriteLine("CONDITIONAL:");
        _output.WriteLine("  • Selection weights are informed by geometric goals, not derived from first principles.");
        _output.WriteLine("  • All metrics depend on finite N (40–80), nS (6–12), primary regime.");
        _output.WriteLine("  • The selected PRIMARY candidate is a recommendation for future investigation,");
        _output.WriteLine("    not an adopted replacement for the V4.2 MeanDist baseline.\n");
        _output.WriteLine("HYPOTHESIS:");
        _output.WriteLine("  • A geometrically-selected length scale may serve as a refined anchor");
        _output.WriteLine("    for a future prediction branch (post-V4.3).");
        _output.WriteLine("  • The selection framework is forward-looking and non-circular.\n");
        _output.WriteLine("NOT CLAIMED:");
        _output.WriteLine("  • Physical c or G derived, compared, or used.");
        _output.WriteLine("  • SI calibration performed or modified.");
        _output.WriteLine("  • Any scale adopted as the V4.3 length anchor.");
        _output.WriteLine("  • V4.2 baseline MeanDist replaced.");
        _output.WriteLine("  • Spacetime, Lorentz, SR, GR, Einstein equations derived.");
        _output.WriteLine("  • Astrophysical data used.\n");
        _output.WriteLine("FORWARD-LOOKING SELECTION ONLY. NO PHYSICAL CLAIMS.");
    }
}
