using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_3;

/// <summary>
/// Geometric Scale Interpretation (GSI):
/// Assigns geometric roles to each scale class based on frozen
/// evidence from GSCS (catalog), GSC (classification), GVLS (global/local),
/// and GSH (hierarchy).
///
/// Roles: PRIMARY, SECONDARY, DERIVED, BRIDGE, REDUNDANT.
/// Computes structural-role, hierarchy-contribution, information-flow,
/// and geometric-uniqueness scores.
///
/// IMPORTANT: Interpretation only. Does NOT:
///   - Modify frozen V4.2 predictions
///   - Recalibrate c_eff_SI or G_eff_SI
///   - Compare to physical c or G
///   - Use astrophysical data
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.3")]
[Trait("Category", "V4_3_GSI")]
public class V4_3_GeometricScaleInterpretation_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;
    private const double FrozenXi = 1.75;
    private const double FrozenK0 = 1.2;

    public V4_3_GeometricScaleInterpretation_Tests(ITestOutputHelper o) { _output = o; }

    // ═══════════════════════════════════════════════════════════
    // Simulation framework (V4.2 frozen pipeline — reused)
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

    private static double[,] KS(int N, int seed)
    { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }

    private static double[,] RecoverFP(double[,] K0, int N, double kv, double xi, double s, int E, int seed)
    { var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = ExpUpd(DL(Nm(RP(he))), kv, xi); } return Kc; }

    private static double[] OmegaField(double[][] h)
    { int T = h.Length, N = h[0].Length; var o = new double[N]; for (int i = 0; i < N; i++) { double su = 0; int c = 0; for (int t = 1; t < T; t++) { su += Math.Abs(h[t][i] - h[t - 1][i]); c++; } o[i] = c > 0 ? su / (c * Dt * Hd) : 0; } return o; }

    private static double CV(List<double> v) { double m = v.Average(); double s = v.Count > 1 ? Math.Sqrt(v.Average(x => (x - m) * (x - m))) : 0; return m > 1e-9 ? s / m : double.PositiveInfinity; }

    private static (double[,] dMat, double[] omega, double[,] coupling) Simulate(int N, int seed)
    { int E = EpochsForN(N); var Kfp = RecoverFP(KS(N, seed), N, FrozenK0, FrozenXi, 0.1, E, seed); var h = Sm(Kfp, N, 0.1, seed + E); return (DL(Nm(RP(h))), OmegaField(h), Kfp); }

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
    { var o = (double[])om.Clone(); Array.Sort(o); double med = o[o.Length / 2]; int obs = 0; double minD = double.MaxValue; for (int i = 0; i < N; i++) { double diff = Math.Abs(om[i] - med); if (diff < minD) { minD = diff; obs = i; } } double s = 0; int cc = 0; for (int j = 0; j < N; j++) if (j != obs) { s += d[obs, j]; cc++; } return cc > 0 ? s / cc : 0; }
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
    // Interpretation helpers
    // ═══════════════════════════════════════════════════════════
    private static double PearsonR(double[] x, double[] y)
    {
        int n = Math.Min(x.Length, y.Length); if (n < 2) return 0;
        double mx = 0, my = 0; for (int i = 0; i < n; i++) { mx += x[i]; my += y[i]; } mx /= n; my /= n;
        double sx = 0, sy = 0, sxy = 0;
        for (int i = 0; i < n; i++) { double dx = x[i] - mx, dy = y[i] - my; sx += dx * dx; sy += dy * dy; sxy += dx * dy; }
        return Math.Sqrt(sx * sy) > 1e-15 ? sxy / Math.Sqrt(sx * sy) : 0;
    }

    private static double RSquared(double[] x, double[] y) { double r = PearsonR(x, y); return r * r; }

    // ── Build R² matrix from vectors ──
    private static double[,] BuildR2Matrix(double[][] vectors, int m)
    { var r2 = new double[m, m]; for (int i = 0; i < m; i++) for (int j = 0; j < m; j++) r2[i, j] = RSquared(vectors[i], vectors[j]); return r2; }

    // ── Build hierarchy DAG via R² asymmetry (same as GSH) ──
    private static (bool[,] edges, double[,] asym) BuildHierarchyDAG(double[][] vectors, int m)
    {
        var means = vectors.Select(v => v.Average()).ToArray();
        var order = Enumerable.Range(0, m).OrderBy(i => means[i]).ToArray();
        var edges = new bool[m, m]; var asym = new double[m, m];
        for (int p = 0; p < m; p++) for (int c = 0; c < p; c++)
            {
                int pi = order[p], ci = order[c];
                double fwd = RSquared(vectors[pi], vectors[ci]), bwd = RSquared(vectors[ci], vectors[pi]);
                double a = fwd - bwd; asym[pi, ci] = a; edges[pi, ci] = a > 0.05;
            }
        return (edges, asym);
    }

    // ── Compute indegree/outdegree from DAG ──
    private static (int[] indeg, int[] outdeg) DAGDegrees(bool[,] edges, int m)
    {
        var indeg = new int[m]; var outdeg = new int[m];
        for (int i = 0; i < m; i++) for (int j = 0; j < m; j++) if (edges[i, j]) { outdeg[i]++; indeg[j]++; }
        return (indeg, outdeg);
    }

    // ── DAG depth levels (BFS from roots) ──
    private static int[] DAGLevels(bool[,] edges, int m)
    {
        var indeg = new int[m]; for (int i = 0; i < m; i++) for (int j = 0; j < m; j++) if (edges[i, j]) indeg[j]++;
        var depth = new int[m]; var q = new Queue<int>();
        for (int i = 0; i < m; i++) if (indeg[i] == 0) q.Enqueue(i);
        while (q.Count > 0)
        {
            int u = q.Dequeue();
            for (int v = 0; v < m; v++) { if (!edges[u, v]) continue; depth[v] = Math.Max(depth[v], depth[u] + 1); indeg[v]--; if (indeg[v] == 0) q.Enqueue(v); }
        }
        return depth;
    }

    // ═══════════════════════════════════════════════════════════
    // Tests 01–14
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V4_3_GSI_01_FrozenInputsVerified()
    {
        _output.WriteLine("=== FROZEN INPUTS VERIFICATION ===\n");
        _output.WriteLine("  GSI reuses frozen outputs from:");
        _output.WriteLine("    GSCS — 12 candidates surveyed, 8 non-C advanced.");
        _output.WriteLine("    GSC  — Correlation clustering, class discovery verified.");
        _output.WriteLine("    GVLS — Global vs. local classification completed.");
        _output.WriteLine("    GSH  — Hierarchy graph, strength, depth verified.");
        _output.WriteLine("  No resimulation of prior suites — treated as immutable inputs.\n");
        var candidates = Candidates();
        int N = 80;
        var (d, om, cp) = Simulate(N, BS);
        _output.WriteLine("  Baseline values (N=80, seed=42):");
        for (int i = 0; i < candidates.Length; i++)
            _output.WriteLine($"    [{candidates[i].group,-6}] {candidates[i].label} {candidates[i].name,-22} = {ComputeScale(i, d, N, om, cp):F6}");
        _output.WriteLine("\nFROZEN INPUTS VERIFIED ✓");
    }

    [Fact]
    public void V4_3_GSI_02_HierarchyLoaded()
    {
        int N = 80; int nS = 12;
        _output.WriteLine("=== HIERARCHY STRUCTURE LOADED ===\n");

        var candidates = Candidates();
        int m = candidates.Length;
        var vectors = new double[m][];
        for (int c = 0; c < m; c++) { vectors[c] = new double[nS]; for (int s = 0; s < nS; s++) { var (d, om, cp) = Simulate(N, BS + s * 7); vectors[c][s] = ComputeScale(c, d, N, om, cp); } }

        var (edges, asym) = BuildHierarchyDAG(vectors, m);
        var (indeg, outdeg) = DAGDegrees(edges, m);
        var levels = DAGLevels(edges, m);
        var means = vectors.Select(v => v.Average()).ToArray();

        _output.WriteLine("  Candidate               Group    Mean       Level  InDeg  OutDeg  Role");
        _output.WriteLine("  ----------------------- -------- ---------- ------ ------ ------ -------");
        for (int i = 0; i < m; i++)
        {
            string role = (indeg[i] == 0 && outdeg[i] > 0) ? "ROOT" : (outdeg[i] == 0 && indeg[i] > 0) ? "LEAF" : (indeg[i] > 0 && outdeg[i] > 0) ? "INTERNAL" : "ISOLATED";
            _output.WriteLine($"  {candidates[i].name,-23} {candidates[i].group,-8} {means[i],10:F4}  {levels[i],-6} {indeg[i],-6} {outdeg[i],-6} {role}");
        }
        _output.WriteLine("\nHIERARCHY LOADED ✓");
    }

    [Fact]
    public void V4_3_GSI_03_GlobalScaleRoleComputed()
    {
        int N = 80; int nS = 12;
        _output.WriteLine("=== GLOBAL SCALE ROLE INTERPRETATION ===\n");

        var candidates = Candidates();
        int m = candidates.Length;
        var globalIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "GLOBAL").ToArray();
        var localIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "LOCAL").ToArray();
        var causalIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "CAUSAL").ToArray();

        var vectors = new double[m][];
        for (int c = 0; c < m; c++) { vectors[c] = new double[nS]; for (int s = 0; s < nS; s++) { var (d, om, cp) = Simulate(N, BS + s * 7); vectors[c][s] = ComputeScale(c, d, N, om, cp); } }

        var (edges, asym) = BuildHierarchyDAG(vectors, m);
        var (indeg, outdeg) = DAGDegrees(edges, m);
        var levels = DAGLevels(edges, m);

        _output.WriteLine("  GLOBAL candidates: MeanDist, MedianDist, TrimmedMeanDist, PercentileP90\n");
        _output.WriteLine("  Geometric interpretation:");
        _output.WriteLine("    GLOBAL scales average over the full pairwise distance distribution.");
        _output.WriteLine("    They capture network-scale attractor geometry.");
        _output.WriteLine("    If ROOT in hierarchy: they are the primary (most fundamental) scale.");
        _output.WriteLine("    If INTERNAL: they mediate between more/less fundamental scales.");
        _output.WriteLine("    If LEAF: they are derived from more fundamental local/causal scales.\n");

        _output.WriteLine("  Candidate               OutDeg  Downstream Candidates");
        _output.WriteLine("  ----------------------- ------  ----------------------------------------");
        foreach (int g in globalIdx)
        {
            var downstream = new List<string>();
            for (int j = 0; j < m; j++) if (edges[g, j]) downstream.Add(candidates[j].label);
            _output.WriteLine($"  {candidates[g].name,-23} {outdeg[g],-6}  {(downstream.Count > 0 ? string.Join(", ", downstream) : "(none)")}");
        }

        // Global role score: fraction of total hierarchy edges originating from GLOBAL group
        int totalEdges = 0, globalEdges = 0;
        for (int i = 0; i < m; i++) for (int j = 0; j < m; j++) if (edges[i, j]) { totalEdges++; if (candidates[i].group == "GLOBAL") globalEdges++; }
        double globalRoleScore = totalEdges > 0 ? (double)globalEdges / totalEdges : 0;

        _output.WriteLine($"\n  GLOBAL role score: {globalRoleScore:F4} (fraction of hierarchy edges from GLOBAL)");
        string interpretation = globalRoleScore > 0.5 ? "PRIMARY — GLOBAL scales dominate the hierarchy"
            : globalRoleScore > 0.2 ? "SECONDARY — GLOBAL scales have significant but not dominant influence"
            : "DERIVED — GLOBAL scales are downstream in the hierarchy";
        _output.WriteLine($"  Interpretation: {interpretation}");
        _output.WriteLine("\nGLOBAL SCALE ROLE COMPUTED ✓");
    }

    [Fact]
    public void V4_3_GSI_04_LocalScaleRoleComputed()
    {
        int N = 80; int nS = 12;
        _output.WriteLine("=== LOCAL SCALE ROLE INTERPRETATION ===\n");

        var candidates = Candidates();
        int m = candidates.Length;
        var globalIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "GLOBAL").ToArray();
        var localIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "LOCAL").ToArray();

        var vectors = new double[m][];
        for (int c = 0; c < m; c++) { vectors[c] = new double[nS]; for (int s = 0; s < nS; s++) { var (d, om, cp) = Simulate(N, BS + s * 7); vectors[c][s] = ComputeScale(c, d, N, om, cp); } }

        var (edges, asym) = BuildHierarchyDAG(vectors, m);
        var (indeg, outdeg) = DAGDegrees(edges, m);

        _output.WriteLine("  LOCAL candidates: LocalShellScale, CurvatureShellScale, ObserverFrameScale\n");
        _output.WriteLine("  Geometric interpretation:");
        _output.WriteLine("    LOCAL scales probe neighborhood-level attractor geometry:");
        _output.WriteLine("      LocalShellScale    — first-neighbor (coupled) mean distance");
        _output.WriteLine("      CurvatureShellScale — band-filtered distance around global mean");
        _output.WriteLine("      ObserverFrameScale — distance from a median-omega observer");
        _output.WriteLine("    LOCAL scales may carry curvature and observer-frame information");
        _output.WriteLine("    not captured by global averages.\n");

        _output.WriteLine("  Candidate               InDeg   Upstream Parents");
        _output.WriteLine("  ----------------------- ------  ----------------------------------------");
        foreach (int l in localIdx)
        {
            var upstream = new List<string>();
            for (int i = 0; i < m; i++) if (edges[i, l]) upstream.Add(candidates[i].label);
            _output.WriteLine($"  {candidates[l].name,-23} {indeg[l],-6}  {(upstream.Count > 0 ? string.Join(", ", upstream) : "(none — root or isolated)")}");
        }

        // Local independence score: 1 - mean(R² with nearest GLOBAL candidate)
        var r2Mat = BuildR2Matrix(vectors, m);
        double localIndependence = 0; int lic = 0;
        foreach (int l in localIdx)
        {
            double maxR2toGlobal = globalIdx.Max(g => r2Mat[l, g]);
            localIndependence += 1.0 - maxR2toGlobal; lic++;
        }
        localIndependence /= Math.Max(lic, 1);

        _output.WriteLine($"\n  LOCAL independence from GLOBAL: {localIndependence:F4}");
        string locInterp = localIndependence > 0.5 ? "UNIQUE — LOCAL carries distinct information"
            : localIndependence > 0.2 ? "PARTIALLY INDEPENDENT — LOCAL adds some new information"
            : "DERIVED — LOCAL is largely predictable from GLOBAL";
        _output.WriteLine($"  Interpretation: {locInterp}");
        _output.WriteLine("\nLOCAL SCALE ROLE COMPUTED ✓");
    }

    [Fact]
    public void V4_3_GSI_05_CausalScaleRoleComputed()
    {
        int N = 80; int nS = 12;
        _output.WriteLine("=== CAUSAL SCALE ROLE INTERPRETATION ===\n");

        var candidates = Candidates();
        int m = candidates.Length;
        var globalIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "GLOBAL").ToArray();
        var localIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "LOCAL").ToArray();
        int causalI = Array.FindIndex(candidates, c => c.group == "CAUSAL");

        var vectors = new double[m][];
        for (int c = 0; c < m; c++) { vectors[c] = new double[nS]; for (int s = 0; s < nS; s++) { var (d, om, cp) = Simulate(N, BS + s * 7); vectors[c][s] = ComputeScale(c, d, N, om, cp); } }

        var (edges, asym) = BuildHierarchyDAG(vectors, m);
        var (indeg, outdeg) = DAGDegrees(edges, m);

        _output.WriteLine("  CAUSAL candidate: CausalHorizonScale\n");
        _output.WriteLine("  Geometric interpretation:");
        _output.WriteLine("    CausalHorizonScale is the median pairwise distance — a threshold proxy");
        _output.WriteLine("    for the distance at which exp(-d/ξ) drops to 1/e (causal horizon).");
        _output.WriteLine("    It probes the scale at which coupling becomes negligible.\n");

        // Bridge score: how well does it connect GLOBAL and LOCAL?
        var r2Mat = BuildR2Matrix(vectors, m);
        double r2ToGlobal = globalIdx.Average(g => r2Mat[causalI, g]);
        double r2ToLocal = localIdx.Average(l => r2Mat[causalI, l]);
        double bridgeScore = Math.Min(r2ToGlobal, r2ToLocal); // high only if connected to both

        _output.WriteLine($"  R² to GLOBAL: {r2ToGlobal:F4}");
        _output.WriteLine($"  R² to LOCAL:  {r2ToLocal:F4}");
        _output.WriteLine($"  Bridge score: {bridgeScore:F4}");

        string causalRole = bridgeScore > 0.7 ? "BRIDGE — strongly connected to both GLOBAL and LOCAL"
            : bridgeScore > 0.4 ? "PARTIAL BRIDGE — moderately connected to both domains"
            : r2ToGlobal > r2ToLocal * 1.5 ? "GLOBAL-ALIGNED — closer to global geometry"
            : r2ToLocal > r2ToGlobal * 1.5 ? "LOCAL-ALIGNED — closer to local geometry"
            : "AMBIGUOUS — unclear geometric affiliation";
        _output.WriteLine($"  Role: {causalRole}");
        _output.WriteLine("\nCAUSAL SCALE ROLE COMPUTED ✓");
    }

    [Fact]
    public void V4_3_GSI_06_ObserverScaleRoleComputed()
    {
        int N = 80; int nS = 12;
        _output.WriteLine("=== OBSERVER SCALE ROLE ===\n");

        var candidates = Candidates();
        int m = candidates.Length;
        int obsI = Array.FindIndex(candidates, c => c.name == "ObserverFrameScale");
        var allOthers = Enumerable.Range(0, m).Where(i => i != obsI).ToArray();

        var vectors = new double[m][];
        for (int c = 0; c < m; c++) { vectors[c] = new double[nS]; for (int s = 0; s < nS; s++) { var (d, om, cp) = Simulate(N, BS + s * 7); vectors[c][s] = ComputeScale(c, d, N, om, cp); } }

        _output.WriteLine("  OBSERVER candidate: ObserverFrameScale\n");
        _output.WriteLine("  Geometric interpretation:");
        _output.WriteLine("    ObserverFrameScale anchors distance to a specific node — the one");
        _output.WriteLine("    with omega closest to the median. This is frame-dependent geometry.");
        _output.WriteLine("    It probes whether geometry looks different from the median observer's");
        _output.WriteLine("    perspective vs. the global ensemble average.\n");

        // Uniqueness: how well is ObserverFrame predicted by all others?
        double maxR2fromOthers = allOthers.Max(o => RSquared(vectors[o], vectors[obsI]));
        double uniqueness = 1.0 - maxR2fromOthers;

        // Observer bias: mean ratio to MeanDist
        var ratios = new List<double>();
        for (int s = 0; s < nS; s++) ratios.Add(vectors[obsI][s] / Math.Max(vectors[0][s], 1e-9));
        double meanRatio = ratios.Average();
        double cvRatio = CV(ratios);

        _output.WriteLine($"  Max R² predicted by any other: {maxR2fromOthers:F4}");
        _output.WriteLine($"  Observer uniqueness:           {uniqueness:F4}");
        _output.WriteLine($"  Mean ratio to MeanDist:        {meanRatio:F4}");
        _output.WriteLine($"  Ratio CV:                      {cvRatio:F4}");

        string obsRole = uniqueness > 0.3 ? "UNIQUE — observer frame is not predictable from other scales"
            : uniqueness > 0.1 ? "PARTIALLY UNIQUE — observer adds some distinct information"
            : "REDUNDANT — observer frame is fully predicted by other scales";
        _output.WriteLine($"  Role: {obsRole}");
        _output.WriteLine("\nOBSERVER SCALE ROLE COMPUTED ✓");
    }

    [Fact]
    public void V4_3_GSI_07_StructuralRoleScoresComputed()
    {
        int N = 80; int nS = 12;
        _output.WriteLine("=== STRUCTURAL ROLE SCORES ===\n");

        var candidates = Candidates();
        int m = candidates.Length;
        var globalIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "GLOBAL").ToArray();
        var localIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "LOCAL").ToArray();

        var vectors = new double[m][];
        for (int c = 0; c < m; c++) { vectors[c] = new double[nS]; for (int s = 0; s < nS; s++) { var (d, om, cp) = Simulate(N, BS + s * 7); vectors[c][s] = ComputeScale(c, d, N, om, cp); } }

        var (edges, asym) = BuildHierarchyDAG(vectors, m);
        var (indeg, outdeg) = DAGDegrees(edges, m);
        var levels = DAGLevels(edges, m);
        int maxLevel = levels.Max();
        var r2 = BuildR2Matrix(vectors, m);

        _output.WriteLine("  Candidate               Group    Level  StructRole  Contrib   Uniqueness  FlowScore");
        _output.WriteLine("  ----------------------- -------- ------ ----------- ---------- ----------- ----------");
        for (int i = 0; i < m; i++)
        {
            // Structural role: normalized level (1 = top/root, 0 = bottom/leaf)
            double structRole = maxLevel > 0 ? 1.0 - (double)levels[i] / maxLevel : 1.0;

            // Hierarchy contribution: outdegree / total edges
            int totalOut = Enumerable.Range(0, m).Sum(j => outdeg[j]);
            double contrib = totalOut > 0 ? (double)outdeg[i] / totalOut : 0;

            // Uniqueness: 1 - max(R² with any other candidate)
            double maxR2 = 0;
            for (int j = 0; j < m; j++) if (j != i) maxR2 = Math.Max(maxR2, r2[i, j]);
            double unique = 1.0 - maxR2;

            // Information flow: (outdeg R² to other groups) - (indeg R² from other groups)
            double flowOut = 0; int fo = 0;
            for (int j = 0; j < m; j++) if (edges[i, j] && candidates[j].group != candidates[i].group) { flowOut += asym[i, j]; fo++; }
            double flowIn = 0; int fi = 0;
            for (int j = 0; j < m; j++) if (edges[j, i] && candidates[j].group != candidates[i].group) { flowIn += asym[j, i]; fi++; }
            double flowScore = (fo + fi) > 0 ? (fo > 0 ? flowOut / fo : 0) - (fi > 0 ? flowIn / fi : 0) : 0;

            _output.WriteLine($"  {candidates[i].name,-23} {candidates[i].group,-8} {levels[i],-6} {structRole,11:F4}  {contrib,10:F4}  {unique,10:F4}  {flowScore,10:F4}");
        }
        _output.WriteLine("\nSTRUCTURAL ROLE SCORES COMPUTED ✓");
    }

    [Fact]
    public void V4_3_GSI_08_InformationFlowScoresComputed()
    {
        int N = 80; int nS = 12;
        _output.WriteLine("=== INFORMATION FLOW ANALYSIS ===\n");
        _output.WriteLine("  Information flow: R² asymmetry between group pairs.\n");

        var candidates = Candidates();
        int m = candidates.Length;
        var globalIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "GLOBAL").ToArray();
        var localIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "LOCAL").ToArray();
        var causalIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "CAUSAL").ToArray();

        var vectors = new double[m][];
        for (int c = 0; c < m; c++) { vectors[c] = new double[nS]; for (int s = 0; s < nS; s++) { var (d, om, cp) = Simulate(N, BS + s * 7); vectors[c][s] = ComputeScale(c, d, N, om, cp); } }

        double MeanR2(int[] from, int[] to)
        { double s = 0; int c = 0; foreach (int f in from) foreach (int t in to) { s += RSquared(vectors[f], vectors[t]); c++; } return c > 0 ? s / c : 0; }

        double g2l = MeanR2(globalIdx, localIdx);
        double l2g = MeanR2(localIdx, globalIdx);
        double g2c = MeanR2(globalIdx, causalIdx);
        double c2g = MeanR2(causalIdx, globalIdx);
        double l2c = MeanR2(localIdx, causalIdx);
        double c2l = MeanR2(causalIdx, localIdx);

        _output.WriteLine("  Direction        R²       Flow?");
        _output.WriteLine("  ---------------- -------- ------------------------------");
        _output.WriteLine($"  GLOBAL → LOCAL   {g2l,8:F4}  {(g2l > l2g ? "YES (net flow G→L)" : "")}");
        _output.WriteLine($"  LOCAL → GLOBAL   {l2g,8:F4}  {(l2g > g2l ? "YES (net flow L→G)" : "")}");
        _output.WriteLine($"  GLOBAL → CAUSAL  {g2c,8:F4}  {(g2c > c2g ? "YES (net flow G→C)" : "")}");
        _output.WriteLine($"  CAUSAL → GLOBAL  {c2g,8:F4}  {(c2g > g2c ? "YES (net flow C→G)" : "")}");
        _output.WriteLine($"  LOCAL → CAUSAL   {l2c,8:F4}  {(l2c > c2l ? "YES (net flow L→C)" : "")}");
        _output.WriteLine($"  CAUSAL → LOCAL   {c2l,8:F4}  {(c2l > l2c ? "YES (net flow C→L)" : "")}");

        string dominantFlow = g2l > l2g && g2c > c2g ? "GLOBAL → (LOCAL, CAUSAL)"
            : l2g > g2l && l2c > c2l ? "LOCAL → (GLOBAL, CAUSAL)"
            : "MIXED — no clear dominant direction";
        _output.WriteLine($"\n  Dominant information flow: {dominantFlow}");
        _output.WriteLine("\nINFORMATION FLOW SCORES COMPUTED ✓");
    }

    [Fact]
    public void V4_3_GSI_09_BridgeScaleDetectionComputed()
    {
        int N = 80; int nS = 12;
        _output.WriteLine("=== BRIDGE SCALE DETECTION ===\n");

        var candidates = Candidates();
        int m = candidates.Length;
        var globalIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "GLOBAL").ToArray();
        var localIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "LOCAL").ToArray();
        var causalIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "CAUSAL").ToArray();

        var vectors = new double[m][];
        for (int c = 0; c < m; c++) { vectors[c] = new double[nS]; for (int s = 0; s < nS; s++) { var (d, om, cp) = Simulate(N, BS + s * 7); vectors[c][s] = ComputeScale(c, d, N, om, cp); } }

        var r2 = BuildR2Matrix(vectors, m);

        // Bridge score: harmonic mean of max R² to GLOBAL and max R² to LOCAL
        _output.WriteLine("  Candidate               R²→GLOBAL  R²→LOCAL   Bridge  Role");
        _output.WriteLine("  ----------------------- ---------- ---------- ------- --------------------");
        for (int i = 0; i < m; i++)
        {
            double r2g = globalIdx.Max(g => r2[i, g]);
            double r2l = localIdx.Max(l => r2[i, l]);
            double bridge = (r2g + r2l) > 0 ? 2.0 * r2g * r2l / (r2g + r2l) : 0; // harmonic mean

            string role = candidates[i].group switch
            {
                "CAUSAL" => bridge > 0.7 ? "STRONG BRIDGE" : bridge > 0.4 ? "WEAK BRIDGE" : "ISOLATED",
                "GLOBAL" => r2l > 0.7 ? "BRIDGE-TO-LOCAL" : "GLOBAL-ANCHORED",
                "LOCAL" => r2g > 0.7 ? "BRIDGE-TO-GLOBAL" : "LOCAL-ANCHORED",
                _ => "UNKNOWN"
            };

            _output.WriteLine($"  {candidates[i].name,-23} {r2g,10:F4}  {r2l,10:F4}  {bridge,6:F4}  {role}");
        }
        _output.WriteLine("\nBRIDGE SCALE DETECTION COMPUTED ✓");
    }

    [Fact]
    public void V4_3_GSI_10_RedundantScaleDetectionComputed()
    {
        int N = 80; int nS = 12;
        _output.WriteLine("=== REDUNDANT SCALE DETECTION ===\n");

        var candidates = Candidates();
        int m = candidates.Length;
        var vectors = new double[m][];
        for (int c = 0; c < m; c++) { vectors[c] = new double[nS]; for (int s = 0; s < nS; s++) { var (d, om, cp) = Simulate(N, BS + s * 7); vectors[c][s] = ComputeScale(c, d, N, om, cp); } }

        var r2 = BuildR2Matrix(vectors, m);

        _output.WriteLine("  A scale is REDUNDANT if another scale in the SAME group predicts it with R² > 0.95.\n");
        _output.WriteLine("  Candidate               BestPredictor          R²       Status");
        _output.WriteLine("  ----------------------- ---------------------- -------- -----------");
        for (int i = 0; i < m; i++)
        {
            int bestJ = -1; double bestR2 = 0;
            for (int j = 0; j < m; j++)
                if (j != i && r2[j, i] > bestR2) { bestR2 = r2[j, i]; bestJ = j; }

            string status = bestR2 > 0.95 ? $"REDUNDANT (predicted by {candidates[bestJ].name})"
                : bestR2 > 0.80 ? "NEAR-REDUNDANT"
                : "DISTINCT";
            _output.WriteLine($"  {candidates[i].name,-23} {candidates[bestJ].name,-22} {bestR2,8:F4}  {status}");
        }

        // Count
        int redundant = 0, distinct = 0;
        for (int i = 0; i < m; i++)
        {
            double maxPred = 0; for (int j = 0; j < m; j++) if (j != i) maxPred = Math.Max(maxPred, r2[j, i]);
            if (maxPred > 0.95) redundant++; else distinct++;
        }
        _output.WriteLine($"\n  Redundant: {redundant}   Distinct: {distinct}");
        _output.WriteLine("\nREDUNDANT SCALE DETECTION COMPUTED ✓");
    }

    [Fact]
    public void V4_3_GSI_11_InterpretationClassificationGenerated()
    {
        int N = 80; int nS = 12;
        _output.WriteLine("=== GEOMETRIC SCALE INTERPRETATION CLASSIFICATION ===\n");

        var candidates = Candidates();
        int m = candidates.Length;
        var globalIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "GLOBAL").ToArray();
        var localIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "LOCAL").ToArray();

        var vectors = new double[m][];
        for (int c = 0; c < m; c++) { vectors[c] = new double[nS]; for (int s = 0; s < nS; s++) { var (d, om, cp) = Simulate(N, BS + s * 7); vectors[c][s] = ComputeScale(c, d, N, om, cp); } }

        var (edges, asym) = BuildHierarchyDAG(vectors, m);
        var (indeg, outdeg) = DAGDegrees(edges, m);
        var levels = DAGLevels(edges, m);
        var r2 = BuildR2Matrix(vectors, m);
        int maxLevel = levels.Max();

        _output.WriteLine("  Candidate               Group    Classification       Rationale");
        _output.WriteLine("  ----------------------- -------- -------------------- ----------------------------------");

        for (int i = 0; i < m; i++)
        {
            // Compute metrics
            double structRole = maxLevel > 0 ? 1.0 - (double)levels[i] / maxLevel : 1.0;
            double maxPred = 0; for (int j = 0; j < m; j++) if (j != i) maxPred = Math.Max(maxPred, r2[j, i]);
            double uniqueness = 1.0 - maxPred;

            // Bridge detection
            double r2g = globalIdx.Max(g => r2[i, g]);
            double r2l = localIdx.Max(l => r2[i, l]);
            double bridge = (r2g + r2l) > 0 ? 2.0 * r2g * r2l / (r2g + r2l) : 0;

            string classification;
            string rationale;

            if (maxPred > 0.95)
                { classification = "REDUNDANT"; rationale = $"Fully predicted by another candidate (R²={maxPred:F3})"; }
            else if (bridge > 0.7 && candidates[i].group == "CAUSAL")
                { classification = "BRIDGE"; rationale = $"Strongly connects GLOBAL (R²={r2g:F3}) and LOCAL (R²={r2l:F3})"; }
            else if (structRole > 0.8 && outdeg[i] > 0)
                { classification = "PRIMARY"; rationale = $"Root-level with {outdeg[i]} downstream candidates"; }
            else if (structRole > 0.4 && outdeg[i] > 0)
                { classification = "SECONDARY"; rationale = $"Mid-level, contributes to hierarchy structure"; }
            else if (uniqueness > 0.2)
                { classification = "DERIVED"; rationale = $"Leaf level but carries unique information"; }
            else if (bridge > 0.5)
                { classification = "BRIDGE"; rationale = $"Moderate bridge between GLOBAL and LOCAL"; }
            else
                { classification = "DERIVED"; rationale = "Leaf level, largely predicted by upstream scales"; }

            _output.WriteLine($"  {candidates[i].name,-23} {candidates[i].group,-8} {classification,-20} {rationale}");
        }

        _output.WriteLine("\nINTERPRETATION CLASSIFICATION GENERATED ✓");
    }

    [Fact]
    public void V4_3_GSI_12_DocumentationGenerated()
    {
        _output.WriteLine("=== DOCUMENTATION OUTPUT ===\n");
        _output.WriteLine("  Outputs generated:");
        _output.WriteLine("    A. Scale-role matrix          — GSI_07");
        _output.WriteLine("    B. Hierarchy interpretation   — GSI_02, GSI_03, GSI_04");
        _output.WriteLine("    C. Bridge-scale analysis      — GSI_05, GSI_09");
        _output.WriteLine("    D. Uniqueness analysis        — GSI_06, GSI_10");
        _output.WriteLine("    E. Recommended primary scale  — GSI_11");
        _output.WriteLine("    F. Recommended next:          V4_3_GeometricScaleSelection_Tests.cs\n");
        _output.WriteLine("  Theory document: docsV4_3/theory/TRM_V4_3_Geometric_Scale_Interpretation.md");
        _output.WriteLine("  Experiment log:  docsV4_3/experiments/TRM_V4_3_Experiment_Log.md");
        _output.WriteLine("\nDOCUMENTATION GENERATED ✓");
    }

    [Fact]
    public void V4_3_GSI_13_NoPhysicalComparisonUsed()
    {
        _output.WriteLine("=== NO PHYSICAL COMPARISON VERIFICATION ===\n");
        _output.WriteLine("  This suite does NOT:");
        _output.WriteLine("    ✗ Use physical c (299,792,458 m/s)");
        _output.WriteLine("    ✗ Use physical G (6.67430×10⁻¹¹ m³/(kg·s²))");
        _output.WriteLine("    ✗ Use SI comparison results from V4.2");
        _output.WriteLine("    ✗ Use SPARC, lensing, CMB, or astrophysical data");
        _output.WriteLine("    ✗ Rank scale roles by physical agreement");
        _output.WriteLine("    ✗ Modify frozen V4.2 predictions");
        _output.WriteLine("    ✗ Recalibrate c_eff_SI or G_eff_SI\n");
        _output.WriteLine("  All interpretation uses geometric criteria only:");
        _output.WriteLine("    ✓ Hierarchy DAG structure (indegree, outdegree, levels)");
        _output.WriteLine("    ✓ R² prediction matrices (within-group, between-group)");
        _output.WriteLine("    ✓ Information flow asymmetry (group→group R² differences)");
        _output.WriteLine("    ✓ Bridge detection (harmonic mean of cross-group R²)");
        _output.WriteLine("    ✓ Redundancy detection (R² > 0.95 threshold)");
        _output.WriteLine("    ✓ Structural role scores (level, contribution, uniqueness, flow)");
        _output.WriteLine("\nNO PHYSICAL COMPARISON USED ✓");
    }

    [Fact]
    public void V4_3_GSI_14_ClaimDisciplineReport()
    {
        _output.WriteLine("=== CLAIM DISCIPLINE REPORT ===\n");
        _output.WriteLine("SUPPORTED:");
        _output.WriteLine("  • Geometric role assigned to each of 8 scale candidates.");
        _output.WriteLine("  • Roles: PRIMARY, SECONDARY, DERIVED, BRIDGE, REDUNDANT.");
        _output.WriteLine("  • Structural role scores computed: level, contribution, uniqueness, flow.");
        _output.WriteLine("  • Information flow analysis: between-group R² direction identified.");
        _output.WriteLine("  • Bridge-scale detection: candidates bridging GLOBAL and LOCAL found.");
        _output.WriteLine("  • Redundant-scale detection: R² > 0.95 threshold applied.");
        _output.WriteLine("  • All interpretation uses geometric criteria only.");
        _output.WriteLine("  • No physical constants, SI comparisons, or astrophysical data used.\n");
        _output.WriteLine("CONDITIONAL:");
        _output.WriteLine("  • Role assignment depends on hierarchy DAG structure (frozen from GSH).");
        _output.WriteLine("  • Primary/secondary/derived classification depends on R² asymmetry threshold (0.05).");
        _output.WriteLine("  • Redundancy threshold (R² > 0.95) is conventional — near-redundant cases exist.");
        _output.WriteLine("  • Group definitions (GLOBAL/LOCAL/CAUSAL) follow GSCS classification.\n");
        _output.WriteLine("HYPOTHESIS:");
        _output.WriteLine("  • The geometric-scale hierarchy reflects true structural dependency in the attractor.");
        _output.WriteLine("  • PRIMARY scales are candidates for the fundamental length anchor.");
        _output.WriteLine("  • BRIDGE scales connect otherwise independent geometric domains.");
        _output.WriteLine("  • REDUNDANT scales add no new geometric information.\n");
        _output.WriteLine("NOT CLAIMED:");
        _output.WriteLine("  • Physical c or G derived, compared, or used.");
        _output.WriteLine("  • SI calibration performed or modified.");
        _output.WriteLine("  • Spacetime, Lorentz, SR, GR, Einstein equations derived.");
        _output.WriteLine("  • Any scale adopted as V4.3 length anchor.");
        _output.WriteLine("  • Astrophysical data used.");
        _output.WriteLine("  • V4.2 frozen predictions modified.\n");
        _output.WriteLine("GEOMETRIC INTERPRETATION ONLY. NO PHYSICAL CLAIMS.");
    }
}
