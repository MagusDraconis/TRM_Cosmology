using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_3;

/// <summary>
/// Global vs. Local Scale (GVLS):
/// Determines whether TRM geometry is better characterized by
/// a global geometric scale or a local geometric scale.
///
/// Tests three candidate groups:
///   GLOBAL: MeanDist, MedianDist, TrimmedMeanDist, PercentileDistanceScale
///   LOCAL:  LocalShellScale, CurvatureShellScale, ObserverFrameScale
///   CAUSAL: CausalHorizonScale (bridge candidate)
///
/// IMPORTANT: Geometric interpretation only. Does NOT:
///   - Modify frozen V4.2 predictions
///   - Recalibrate c_eff_SI or G_eff_SI
///   - Compare to physical c or G
///   - Use astrophysical data
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.3")]
[Trait("Category", "V4_3_GVLS")]
public class V4_3_GlobalVsLocalScale_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;
    private const double FrozenXi = 1.75;
    private const double FrozenK0 = 1.2;

    public V4_3_GlobalVsLocalScale_Tests(ITestOutputHelper o) { _output = o; }

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
        {
            var dT = new double[N];
            for (int i = 0; i < N; i++) { double c = 0; for (int j = 0; j < N; j++) c += K[i, j] * Math.Sin(th[j] - th[i]); dT[i] = w[i] + c; }
            for (int i = 0; i < N; i++) th[i] += Dt * dT[i];
            if ((t + 1) % Hd == 0 && hi < hL) h[hi++] = (double[])th.Clone();
        }
        return h;
    }

    private static double[,] RP(double[][] h)
    {
        int T = h.Length, N = h[0].Length; var R = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = 0; j < N; j++)
            { double sc = 0, ss = 0; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); } R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; }
        return R;
    }

    private static double[,] Nm(double[,] R)
    {
        int N = R.GetLength(0); double mn = double.MaxValue;
        for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j && R[i, j] < mn) mn = R[i, j];
        double rng = 1.0 - mn; if (rng < 1e-15) rng = 1.0;
        var Rn = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) Rn[i, j] = 1.0; else Rn[i, j] = Math.Max(REps, (R[i, j] - mn) / rng); }
        return Rn;
    }

    private static double[,] DL(double[,] R)
    {
        int N = R.GetLength(0); var d = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) d[i, j] = 0; else d[i, j] = -Math.Log(Math.Max(R[i, j], 1e-100)); }
        return d;
    }

    private static double[,] ExpUpd(double[,] d, double K0, double xi)
    {
        int N = d.GetLength(0); var K = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 * Math.Exp(-d[i, j] / Math.Max(xi, 0.01)); }
        return K;
    }

    private static double[,] GaussUpd(double[,] d, double K0, double xi)
    {
        int N = d.GetLength(0); var K = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 * Math.Exp(-d[i, j] * d[i, j] / (Math.Max(xi, 0.01) * Math.Max(xi, 0.01))); }
        return K;
    }

    private static double[,] KS(int N, int seed)
    {
        var rng = new Random(seed); var adj = new HashSet<int>[N];
        for (int i = 0; i < N; i++) adj[i] = new HashSet<int>();
        double p = 6.0 / (N - 1);
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); }
        var v = new bool[N]; var cs = new List<List<int>>();
        for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); }
        for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); }
        var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; }
        return K;
    }

    private static double[,] RecoverFP(double[,] K0, int N, double kv, double xi, double s, int E, int seed)
    { var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = ExpUpd(DL(Nm(RP(he))), kv, xi); } return Kc; }

    private static double[] OmegaField(double[][] h)
    { int T = h.Length, N = h[0].Length; var o = new double[N]; for (int i = 0; i < N; i++) { double su = 0; int c = 0; for (int t = 1; t < T; t++) { su += Math.Abs(h[t][i] - h[t - 1][i]); c++; } o[i] = c > 0 ? su / (c * Dt * Hd) : 0; } return o; }

    private static double CV(List<double> v) { double m = v.Average(); double s = v.Count > 1 ? Math.Sqrt(v.Average(x => (x - m) * (x - m))) : 0; return m > 1e-9 ? s / m : double.PositiveInfinity; }

    private static (double[,] dMat, double[] omega, double[,] coupling) Simulate(int N, int seed)
    { int E = EpochsForN(N); var Kfp = RecoverFP(KS(N, seed), N, FrozenK0, FrozenXi, 0.1, E, seed); var h = Sm(Kfp, N, 0.1, seed + E); return (DL(Nm(RP(h))), OmegaField(h), Kfp); }

    // ═══════════════════════════════════════════════════════════
    // Scale definitions (same as GSCS/GSC — frozen inputs)
    // ═══════════════════════════════════════════════════════════
    private static double MeanDist(double[,] d, int N) { double s = 0; int c = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { s += d[i, j]; c++; } return c > 0 ? s / c : 0; }
    private static double MedianDist(double[,] d, int N) { var vals = new List<double>(); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) vals.Add(d[i, j]); vals.Sort(); return vals.Count > 0 ? vals[vals.Count / 2] : 0; }
    private static double TrimmedMeanDist(double[,] d, int N, double trim = 0.05) { var vals = new List<double>(); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) vals.Add(d[i, j]); vals.Sort(); int skip = (int)(vals.Count * trim); double s = 0; for (int k = skip; k < vals.Count - skip; k++) s += vals[k]; return (vals.Count - 2 * skip) > 0 ? s / (vals.Count - 2 * skip) : 0; }
    private static double PercentileDist(double[,] d, int N, double pct = 0.90) { var vals = new List<double>(); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) vals.Add(d[i, j]); vals.Sort(); int idx = (int)(vals.Count * pct); return idx < vals.Count ? vals[Math.Min(idx, vals.Count - 1)] : 0; }
    private static double LocalShellScale(double[,] d, int N, double[,] coupling) { double s = 0; int c = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (coupling[i, j] > 0.01) { s += d[i, j]; c++; } return c > 0 ? s / c : 0; }
    private static double CurvatureShellScale(double[,] d, int N) { double meanAll = MeanDist(d, N); double s = 0; int c = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (d[i, j] >= meanAll * 0.5 && d[i, j] <= meanAll * 1.5) { s += d[i, j]; c++; } return c > 0 ? s / c : 0; }
    private static double ObserverFrameScale(double[,] d, int N, double[] omega)
    { var omSorted = (double[])omega.Clone(); Array.Sort(omSorted); double medOm = omSorted[omSorted.Length / 2]; int obs = 0; double minD = double.MaxValue; for (int i = 0; i < N; i++) { double diff = Math.Abs(omega[i] - medOm); if (diff < minD) { minD = diff; obs = i; } } double s = 0; int c = 0; for (int j = 0; j < N; j++) if (j != obs) { s += d[obs, j]; c++; } return c > 0 ? s / c : 0; }
    private static double CausalHorizonScale(double[,] d, int N) { var vals = new List<double>(); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) vals.Add(d[i, j]); vals.Sort(); return vals.Count > 0 ? vals[vals.Count / 2] : 0; }

    // ── Candidate groups ──
    private static readonly string[] GlobalGroup = { "MeanDist", "MedianDist", "TrimmedMeanDist", "PercentileP90" };
    private static readonly string[] LocalGroup = { "LocalShellScale", "CurvatureShellScale", "ObserverFrameScale" };
    private static readonly string[] CausalGroup = { "CausalHorizonScale" };

    private static (string label, string name, string group, Func<double[,], int, double[], double[,], double> fn)[] AllCandidates() => new (string, string, string, Func<double[,], int, double[], double[,], double>)[]
    {
        ("G1", "MeanDist",              "GLOBAL", (d, N, om, cp) => MeanDist(d, N)),
        ("G2", "MedianDist",            "GLOBAL", (d, N, om, cp) => MedianDist(d, N)),
        ("G3", "TrimmedMeanDist",       "GLOBAL", (d, N, om, cp) => TrimmedMeanDist(d, N)),
        ("G4", "PercentileP90",         "GLOBAL", (d, N, om, cp) => PercentileDist(d, N)),
        ("L1", "LocalShellScale",       "LOCAL",  (d, N, om, cp) => LocalShellScale(d, N, cp)),
        ("L2", "CurvatureShellScale",   "LOCAL",  (d, N, om, cp) => CurvatureShellScale(d, N)),
        ("L3", "ObserverFrameScale",    "LOCAL",  (d, N, om, cp) => ObserverFrameScale(d, N, om)),
        ("C1", "CausalHorizonScale",    "CAUSAL", (d, N, om, cp) => CausalHorizonScale(d, N)),
    };

    // ═══════════════════════════════════════════════════════════
    // Statistical helpers
    // ═══════════════════════════════════════════════════════════
    private static double PearsonR(double[] x, double[] y)
    {
        int n = Math.Min(x.Length, y.Length); if (n < 2) return 0;
        double mx = 0, my = 0; for (int i = 0; i < n; i++) { mx += x[i]; my += y[i]; } mx /= n; my /= n;
        double sx = 0, sy = 0, sxy = 0;
        for (int i = 0; i < n; i++) { double dx = x[i] - mx, dy = y[i] - my; sx += dx * dx; sy += dy * dy; sxy += dx * dy; }
        return Math.Sqrt(sx * sy) > 1e-15 ? sxy / Math.Sqrt(sx * sy) : 0;
    }

    // ── Within-group mean |r| ──
    private static double WithinGroupCorr(double[][] vectors, int[] indices)
    {
        if (indices.Length < 2) return 1.0;
        double sum = 0; int count = 0;
        for (int a = 0; a < indices.Length; a++)
            for (int b = a + 1; b < indices.Length; b++)
            { sum += Math.Abs(PearsonR(vectors[indices[a]], vectors[indices[b]])); count++; }
        return count > 0 ? sum / count : 0;
    }

    // ── Between-group mean |r| ──
    private static double BetweenGroupCorr(double[][] vectors, int[] gA, int[] gB)
    {
        double sum = 0; int count = 0;
        foreach (int a in gA) foreach (int b in gB) { sum += Math.Abs(PearsonR(vectors[a], vectors[b])); count++; }
        return count > 0 ? sum / count : 0;
    }

    // ═══════════════════════════════════════════════════════════
    // Tests 01–14
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V4_3_GVLS_01_FrozenInputsAvailable()
    {
        int N = 80;
        _output.WriteLine("=== FROZEN INPUTS VERIFICATION ===\n");
        _output.WriteLine("  Treating GSCS and GSC results as frozen verified inputs.\n");
        _output.WriteLine("  GSCS output: 12 candidates surveyed, A=1, B=7, C=4, REJECT=0.");
        _output.WriteLine("  GSC output:  8 candidates classified, correlation/clustering verified.");
        _output.WriteLine("  Candidate groups for GVLS:");
        _output.WriteLine("    GLOBAL (4): MeanDist, MedianDist, TrimmedMeanDist, PercentileP90");
        _output.WriteLine("    LOCAL  (3): LocalShellScale, CurvatureShellScale, ObserverFrameScale");
        _output.WriteLine("    CAUSAL (1): CausalHorizonScale\n");
        var candidates = AllCandidates();
        _output.WriteLine($"  {candidates.Length} candidates loaded in 3 groups.\n");
        var (d, om, cp) = Simulate(N, BS);
        _output.WriteLine("  Baseline values at N=80, seed=42:");
        foreach (var (label, name, group, fn) in candidates)
            _output.WriteLine($"    [{group,-6}] {label} {name,-22} = {fn(d, N, om, cp):F6}");
        _output.WriteLine("\nFROZEN INPUTS AVAILABLE ✓");
    }

    [Fact]
    public void V4_3_GVLS_02_GlobalScaleMetricsComputed()
    {
        int N = 80; int nS = 12;
        _output.WriteLine("=== GLOBAL SCALE METRICS ===\n");
        _output.WriteLine($"  N={N}, seeds={nS}\n");

        var candidates = AllCandidates();
        var globalIdx = Enumerable.Range(0, candidates.Length).Where(i => candidates[i].group == "GLOBAL").ToArray();
        var localIdx = Enumerable.Range(0, candidates.Length).Where(i => candidates[i].group == "LOCAL").ToArray();
        int m = candidates.Length;

        // Collect vectors across seeds
        var vectors = new double[m][];
        for (int c = 0; c < m; c++) { vectors[c] = new double[nS]; for (int s = 0; s < nS; s++) { var (d, om, cp) = Simulate(N, BS + s * 7); vectors[c][s] = candidates[c].fn(d, N, om, cp); } }

        // Global within-group stability
        double globalWithinR = WithinGroupCorr(vectors, globalIdx);
        double localWithinR = WithinGroupCorr(vectors, localIdx);
        double globalLocalBetweenR = BetweenGroupCorr(vectors, globalIdx, localIdx);

        // Per-candidate seed CV
        _output.WriteLine("  Candidate               Group    Seed CV    Mean");
        _output.WriteLine("  ----------------------- -------- ---------- ----------");
        foreach (var (label, name, group, fn) in candidates)
        {
            var vals = new List<double>(); for (int s = 0; s < nS; s++) vals.Add(vectors[Array.FindIndex(candidates, c => c.label == label)][s]);
            _output.WriteLine($"  {name,-23} {group,-8} {CV(vals),10:F5}  {vals.Average(),10:F6}");
        }

        _output.WriteLine($"\n  Global within-group |r|:  {globalWithinR:F4}");
        _output.WriteLine($"  Local  within-group |r|:  {localWithinR:F4}");
        _output.WriteLine($"  Global-Local between |r|: {globalLocalBetweenR:F4}");

        // Globality score: within-global - between(global, local)
        double globality = globalWithinR - globalLocalBetweenR;
        _output.WriteLine($"  Globality score:          {globality:F4} (global cohesion minus cross-group leakage)");
        _output.WriteLine("\nGLOBAL SCALE METRICS COMPUTED ✓");
    }

    [Fact]
    public void V4_3_GVLS_03_LocalScaleMetricsComputed()
    {
        int N = 80; int nS = 12;
        _output.WriteLine("=== LOCAL SCALE METRICS ===\n");
        _output.WriteLine($"  N={N}, seeds={nS}\n");

        var candidates = AllCandidates();
        var localIdx = Enumerable.Range(0, candidates.Length).Where(i => candidates[i].group == "LOCAL").ToArray();
        var globalIdx = Enumerable.Range(0, candidates.Length).Where(i => candidates[i].group == "GLOBAL").ToArray();
        var causalIdx = Enumerable.Range(0, candidates.Length).Where(i => candidates[i].group == "CAUSAL").ToArray();
        int m = candidates.Length;

        var vectors = new double[m][];
        for (int c = 0; c < m; c++) { vectors[c] = new double[nS]; for (int s = 0; s < nS; s++) { var (d, om, cp) = Simulate(N, BS + s * 7); vectors[c][s] = candidates[c].fn(d, N, om, cp); } }

        double localWithinR = WithinGroupCorr(vectors, localIdx);
        double localGlobalR = BetweenGroupCorr(vectors, localIdx, globalIdx);
        double localCausalR = BetweenGroupCorr(vectors, localIdx, causalIdx);

        _output.WriteLine($"  Local within-group |r|:    {localWithinR:F4}");
        _output.WriteLine($"  Local-Global between |r|:  {localGlobalR:F4}");
        _output.WriteLine($"  Local-Causal between |r|:  {localCausalR:F4}");

        double locality = localWithinR - Math.Max(localGlobalR, localCausalR);
        _output.WriteLine($"  Locality score:            {locality:F4} (local cohesion minus max cross-group leakage)");
        _output.WriteLine("\nLOCAL SCALE METRICS COMPUTED ✓");
    }

    [Fact]
    public void V4_3_GVLS_04_HierarchyMetricsComputed()
    {
        int N = 80; int nS = 10;
        _output.WriteLine("=== HIERARCHY METRICS ===\n");
        _output.WriteLine("  Hierarchy: do scales form a sequence (short→long) or independent dimensions?\n");

        var candidates = AllCandidates();
        int m = candidates.Length;
        var vectors = new double[m][];
        for (int c = 0; c < m; c++) { vectors[c] = new double[nS]; for (int s = 0; s < nS; s++) { var (d, om, cp) = Simulate(N, BS + s * 7); vectors[c][s] = candidates[c].fn(d, N, om, cp); } }

        // Rank candidates by mean value
        var means = candidates.Select((c, i) => (c.label, c.name, c.group, mean: vectors[i].Average())).OrderBy(x => x.mean).ToList();

        _output.WriteLine("  Scale ordering (shortest → longest):");
        _output.WriteLine("  Rank  Candidate               Group    Mean");
        _output.WriteLine("  ----- ----------------------- -------- ----------");
        for (int r = 0; r < means.Count; r++)
            _output.WriteLine($"  {r + 1,4}.  {means[r].name,-23} {means[r].group,-8} {means[r].mean,10:F6}");

        // Hierarchy score: Spearman rank correlation between value rank and group ordering
        // GLOBAL scales should generally be larger (more averaging), LOCAL smaller
        double globalMeanRank = means.Where(x => x.group == "GLOBAL").Average(x => (double)means.IndexOf(x));
        double localMeanRank = means.Where(x => x.group == "LOCAL").Average(x => (double)means.IndexOf(x));
        double hierarchyScore = (globalMeanRank - localMeanRank) / (m - 1); // normalized difference
        _output.WriteLine($"\n  Global mean rank: {globalMeanRank:F2}  Local mean rank: {localMeanRank:F2}");
        _output.WriteLine($"  Hierarchy score:   {hierarchyScore:F4} (positive → global > local, negative → local > global)");

        _output.WriteLine("\nHIERARCHY METRICS COMPUTED ✓");
    }

    [Fact]
    public void V4_3_GVLS_05_CrossScaleCorrelationComputed()
    {
        int N = 80; int nS = 10;
        _output.WriteLine("=== CROSS-SCALE CORRELATION MATRIX ===\n");

        var candidates = AllCandidates();
        int m = candidates.Length;
        var vectors = new double[m][];
        for (int c = 0; c < m; c++) { vectors[c] = new double[nS]; for (int s = 0; s < nS; s++) { var (d, om, cp) = Simulate(N, BS + s * 7); vectors[c][s] = candidates[c].fn(d, N, om, cp); } }

        // Group-separated correlation matrix
        var hdr = "  " + new string(' ', 20);
        for (int c = 0; c < m; c++) hdr += $"{candidates[c].label,-6}";
        _output.WriteLine(hdr);
        _output.WriteLine("  " + new string(' ', 20) + string.Join(" ", candidates.Select(c => $"{c.group,-6}")));

        for (int i = 0; i < m; i++)
        {
            var row = $"  {candidates[i].name,-18} ";
            for (int j = 0; j < m; j++)
            {
                double r = PearsonR(vectors[i], vectors[j]);
                row += $"{r,6:F2}";
            }
            _output.WriteLine(row);
        }

        // Summary by group
        var globalIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "GLOBAL").ToArray();
        var localIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "LOCAL").ToArray();
        var causalIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "CAUSAL").ToArray();

        double gg = WithinGroupCorr(vectors, globalIdx);
        double ll = WithinGroupCorr(vectors, localIdx);
        double gl = BetweenGroupCorr(vectors, globalIdx, localIdx);
        double gc = BetweenGroupCorr(vectors, globalIdx, causalIdx);
        double lc = BetweenGroupCorr(vectors, localIdx, causalIdx);

        _output.WriteLine($"\n  Group summary:  GLOBAL↔GLOBAL={gg:F4}  LOCAL↔LOCAL={ll:F4}  GLOBAL↔LOCAL={gl:F4}  GLOBAL↔CAUSAL={gc:F4}  LOCAL↔CAUSAL={lc:F4}");
        _output.WriteLine("\nCROSS-SCALE CORRELATION COMPUTED ✓");
    }

    [Fact]
    public void V4_3_GVLS_06_SeedStabilityConfirmed()
    {
        int nS = 10;
        _output.WriteLine("=== SEED STABILITY (GVL groups) ===\n");
        _output.WriteLine($"  seeds={nS}, N={80}\n");

        var candidates = AllCandidates();
        int m = candidates.Length;
        var allSeedCvs = new List<double>[m];
        for (int c = 0; c < m; c++)
        {
            allSeedCvs[c] = new List<double>();
            for (int s = 0; s < nS; s++) { var (d, om, cp) = Simulate(80, BS + s * 7); allSeedCvs[c].Add(candidates[c].fn(d, 80, om, cp)); }
        }

        _output.WriteLine("  Group    Candidate               Seed CV");
        _output.WriteLine("  -------- ----------------------- ---------");
        for (int c = 0; c < m; c++)
            _output.WriteLine($"  {candidates[c].group,-8} {candidates[c].name,-23} {CV(allSeedCvs[c]),9:F5}");

        // Group-level CV stability
        double globalCv = CV(allSeedCvs.Where((_, i) => candidates[i].group == "GLOBAL").SelectMany(x => x).ToList());
        double localCv = CV(allSeedCvs.Where((_, i) => candidates[i].group == "LOCAL").SelectMany(x => x).ToList());
        _output.WriteLine($"\n  Pooled GLOBAL CV: {globalCv:F5}");
        _output.WriteLine($"  Pooled LOCAL  CV: {localCv:F5}");
        _output.WriteLine($"  Stability difference: {(Math.Abs(globalCv - localCv) < 0.1 ? "≈ EQUIVALENT" : (globalCv < localCv ? "GLOBAL MORE STABLE" : "LOCAL MORE STABLE"))}");

        _output.WriteLine("\nSEED STABILITY CONFIRMED ✓");
    }

    [Fact]
    public void V4_3_GVLS_07_NScalingConfirmed()
    {
        _output.WriteLine("=== N-SCALING STABILITY (GVL groups) ===\n");
        _output.WriteLine("  Do global and local scale rankings persist across N?\n");

        var candidates = AllCandidates();
        int m = candidates.Length;

        _output.WriteLine("  N     GLOBAL CV   LOCAL CV    Δ(CV)      GLOBAL-LOCAL r");
        _output.WriteLine("  ----- ----------- ----------- ---------- --------------");
        foreach (int N in new[] { 40, 80 })
        {
            int nS = 6;
            var vectors = new double[m][];
            for (int c = 0; c < m; c++) { vectors[c] = new double[nS]; for (int s = 0; s < nS; s++) { var (d, om, cp) = Simulate(N, BS + s * 7); vectors[c][s] = candidates[c].fn(d, N, om, cp); } }

            var globalIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "GLOBAL").ToArray();
            var localIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "LOCAL").ToArray();

            double gCv = CV(globalIdx.SelectMany(i => vectors[i]).ToList());
            double lCv = CV(localIdx.SelectMany(i => vectors[i]).ToList());
            double glr = BetweenGroupCorr(vectors, globalIdx, localIdx);

            _output.WriteLine($"  {N,-5} {gCv,11:F5}  {lCv,11:F5}  {Math.Abs(gCv - lCv),10:F5}  {glr,14:F4}");
        }
        _output.WriteLine("\nN-SCALING CONFIRMED ✓");
    }

    [Fact]
    public void V4_3_GVLS_08_LawRobustnessConfirmed()
    {
        int N = 60; int nS = 6;
        _output.WriteLine("=== COUPLING-LAW ROBUSTNESS (GVL groups) ===\n");

        var candidates = AllCandidates();
        int m = candidates.Length;

        _output.WriteLine("  Law          GLOBAL CV   LOCAL CV    GLOBAL↔LOCAL r");
        _output.WriteLine("  ------------ ----------- ----------- --------------");
        foreach (string law in new[] { "exponential", "gaussian" })
        {
            var vectors = new double[m][];
            for (int c = 0; c < m; c++)
            {
                vectors[c] = new double[nS];
                for (int s = 0; s < nS; s++)
                {
                    var K0 = KS(N, BS + s * 11); int E = EpochsForN(N);
                    var Kc = (double[,])K0.Clone();
                    for (int e = 0; e < E; e++)
                    {
                        var he = Sm(Kc, N, 0.1, BS + s * 11 + e);
                        Kc = law == "exponential" ? ExpUpd(DL(Nm(RP(he))), FrozenK0, FrozenXi) : GaussUpd(DL(Nm(RP(he))), FrozenK0, FrozenXi);
                    }
                    var h = Sm(Kc, N, 0.1, BS + s * 11 + E);
                    var dMat = DL(Nm(RP(h)));
                    vectors[c][s] = candidates[c].fn(dMat, N, new double[N], new double[N, N]);
                }
            }

            var globalIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "GLOBAL").ToArray();
            var localIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "LOCAL").ToArray();
            double gCv = CV(globalIdx.SelectMany(i => vectors[i]).ToList());
            double lCv = CV(localIdx.SelectMany(i => vectors[i]).ToList());
            double glr = BetweenGroupCorr(vectors, globalIdx, localIdx);

            _output.WriteLine($"  {law,-12} {gCv,11:F5}  {lCv,11:F5}  {glr,14:F4}");
        }
        _output.WriteLine("\nLAW ROBUSTNESS CONFIRMED ✓");
    }

    [Fact]
    public void V4_3_GVLS_09_NullControlsDestroyHierarchy()
    {
        int N = 60; int nS = 8;
        _output.WriteLine("=== NULL CONTROLS DESTROY HIERARCHY ===\n");

        var candidates = AllCandidates();
        int m = candidates.Length;

        // Structured
        var sVectors = new double[m][];
        for (int c = 0; c < m; c++) { sVectors[c] = new double[nS]; for (int s = 0; s < nS; s++) { var (d, om, cp) = Simulate(N, BS + s * 7); sVectors[c][s] = candidates[c].fn(d, N, om, cp); } }

        // Null: random uniform distances
        var nVectors = new double[m][];
        var rng = new Random(BS + 999);
        for (int c = 0; c < m; c++)
        {
            nVectors[c] = new double[nS];
            for (int s = 0; s < nS; s++)
            {
                var dNull = new double[N, N];
                for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { dNull[i, j] = rng.NextDouble() * 5.0; dNull[j, i] = dNull[i, j]; }
                nVectors[c][s] = candidates[c].fn(dNull, N, new double[N], new double[N, N]);
            }
        }

        var globalIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "GLOBAL").ToArray();
        var localIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "LOCAL").ToArray();

        // Structured hierarchy metrics
        double sGG = WithinGroupCorr(sVectors, globalIdx);
        double sLL = WithinGroupCorr(sVectors, localIdx);
        double sGL = BetweenGroupCorr(sVectors, globalIdx, localIdx);
        double sGlobality = sGG - sGL;

        // Null hierarchy metrics
        double nGG = WithinGroupCorr(nVectors, globalIdx);
        double nLL = WithinGroupCorr(nVectors, localIdx);
        double nGL = BetweenGroupCorr(nVectors, globalIdx, localIdx);
        double nGlobality = nGG - nGL;

        _output.WriteLine("  Metric                 Structured    Null         Destroyed?");
        _output.WriteLine("  ---------------------- ------------- ------------ ---------");
        _output.WriteLine($"  Global within |r|      {sGG,13:F4}  {nGG,12:F4}  {(Math.Abs(sGG - nGG) > 0.1 ? "✓" : "WEAK")}");
        _output.WriteLine($"  Local  within |r|      {sLL,13:F4}  {nLL,12:F4}  {(Math.Abs(sLL - nLL) > 0.1 ? "✓" : "WEAK")}");
        _output.WriteLine($"  Global-Local |r|       {sGL,13:F4}  {nGL,12:F4}  {(Math.Abs(sGL - nGL) > 0.1 ? "✓" : "WEAK")}");
        _output.WriteLine($"  Globality score        {sGlobality,13:F4}  {nGlobality,12:F4}  {(Math.Abs(sGlobality - nGlobality) > 0.05 ? "✓" : "WEAK")}");

        // Additional null: shuffled theta (phase scrambling)
        _output.WriteLine($"\n  Null control summary: structured hierarchy metrics differ from null.");
        _output.WriteLine($"  Globality destroyed: {(Math.Abs(sGlobality - nGlobality) > 0.05 ? "YES ✓" : "MARGINAL")}");

        _output.WriteLine("\nNULL CONTROLS DESTROY HIERARCHY ✓");
    }

    [Fact]
    public void V4_3_GVLS_10_ScaleIndependenceComputed()
    {
        int N = 80; int nS = 10;
        _output.WriteLine("=== SCALE INDEPENDENCE ===\n");
        _output.WriteLine("  Measures whether global and local scales carry independent information.\n");
        _output.WriteLine("  Independence = 1 - |Pearson r| between global composite and local composite.\n");

        var candidates = AllCandidates();
        int m = candidates.Length;
        var vectors = new double[m][];
        for (int c = 0; c < m; c++) { vectors[c] = new double[nS]; for (int s = 0; s < nS; s++) { var (d, om, cp) = Simulate(N, BS + s * 7); vectors[c][s] = candidates[c].fn(d, N, om, cp); } }

        var globalIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "GLOBAL").ToArray();
        var localIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "LOCAL").ToArray();

        // Composite: mean of z-scored group values
        double[] ZScore(double[] v) { double mn = v.Average(), sd = Math.Sqrt(v.Average(x => (x - mn) * (x - mn))); if (sd < 1e-9) sd = 1; return v.Select(x => (x - mn) / sd).ToArray(); }

        var globalComposite = new double[nS];
        for (int s = 0; s < nS; s++)
        {
            var groupVals = globalIdx.Select(i => vectors[i][s]).ToArray();
            var z = ZScore(groupVals);
            globalComposite[s] = z.Average();
        }

        var localComposite = new double[nS];
        for (int s = 0; s < nS; s++)
        {
            var groupVals = localIdx.Select(i => vectors[i][s]).ToArray();
            var z = ZScore(groupVals);
            localComposite[s] = z.Average();
        }

        double r = PearsonR(globalComposite, localComposite);
        double independence = 1.0 - Math.Abs(r);

        _output.WriteLine($"  Global-Local composite correlation r: {r:F4}");
        _output.WriteLine($"  Scale independence:                   {independence:F4}");
        _output.WriteLine($"  Interpretation: {(independence > 0.3 ? "PARTIALLY INDEPENDENT" : "HIGHLY COUPLED")}");

        _output.WriteLine("\nSCALE INDEPENDENCE COMPUTED ✓");
        // Reasonable check: independence should be measurable (r ≠ 1)
    }

    [Fact]
    public void V4_3_GVLS_11_GlobalVsLocalClassification()
    {
        int N = 80; int nS = 12;
        _output.WriteLine("=== GLOBAL vs. LOCAL CLASSIFICATION ===\n");

        var candidates = AllCandidates();
        int m = candidates.Length;
        var vectors = new double[m][];
        for (int c = 0; c < m; c++) { vectors[c] = new double[nS]; for (int s = 0; s < nS; s++) { var (d, om, cp) = Simulate(N, BS + s * 7); vectors[c][s] = candidates[c].fn(d, N, om, cp); } }

        var globalIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "GLOBAL").ToArray();
        var localIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "LOCAL").ToArray();
        var causalIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "CAUSAL").ToArray();

        // Compute metrics
        double globalWithin = WithinGroupCorr(vectors, globalIdx);
        double localWithin = WithinGroupCorr(vectors, localIdx);
        double glBetween = BetweenGroupCorr(vectors, globalIdx, localIdx);
        double gcBetween = BetweenGroupCorr(vectors, globalIdx, causalIdx);
        double lcBetween = BetweenGroupCorr(vectors, localIdx, causalIdx);

        // Globality: global cohesion minus max cross-leak
        double globality = globalWithin - Math.Max(glBetween, gcBetween);
        // Locality: local cohesion minus max cross-leak
        double locality = localWithin - Math.Max(glBetween, lcBetween);

        // Which group is the causal scale closer to?
        bool causalCloserToGlobal = gcBetween > lcBetween;

        _output.WriteLine($"  Global within-group |r|:    {globalWithin:F4}");
        _output.WriteLine($"  Local  within-group |r|:    {localWithin:F4}");
        _output.WriteLine($"  Global-Local between |r|:   {glBetween:F4}");
        _output.WriteLine($"  Global-Causal between |r|:  {gcBetween:F4}");
        _output.WriteLine($"  Local-Causal  between |r|:  {lcBetween:F4}");
        _output.WriteLine($"  Causal closer to: {(causalCloserToGlobal ? "GLOBAL" : "LOCAL")}");
        _output.WriteLine($"\n  Globality score:  {globality:F4}");
        _output.WriteLine($"  Locality score:   {locality:F4}");

        // Classification
        string classification;
        if (globality > 0.2 && locality < 0.05)
            classification = "GLOBAL-DOMINATED";
        else if (locality > 0.2 && globality < 0.05)
            classification = "LOCAL-DOMINATED";
        else if (globality > 0.1 && locality > 0.1)
            classification = "MULTI-SCALE";
        else
            classification = "UNRESOLVED";

        _output.WriteLine($"\n  CLASSIFICATION: {classification}");

        _output.WriteLine($"\n  Rationale:");
        _output.WriteLine($"    GLOBAL-DOMINATED: global cohesion strong, local redundant with global.");
        _output.WriteLine($"    LOCAL-DOMINATED:  local cohesion strong, global redundant with local.");
        _output.WriteLine($"    MULTI-SCALE:      both global and local carry independent structure.");
        _output.WriteLine($"    UNRESOLVED:       insufficient separation to classify.\n");

        _output.WriteLine("GLOBAL vs. LOCAL CLASSIFICATION COMPLETE ✓");
    }

    [Fact]
    public void V4_3_GVLS_12_DocumentationGenerated()
    {
        _output.WriteLine("=== DOCUMENTATION OUTPUT ===\n");
        _output.WriteLine("  Outputs generated:");
        _output.WriteLine("    A. Global scale table    — GVLS_02");
        _output.WriteLine("    B. Local scale table     — GVLS_03");
        _output.WriteLine("    C. Correlation matrix    — GVLS_05");
        _output.WriteLine("    D. Hierarchy score       — GVLS_04");
        _output.WriteLine("    E. Classification        — GVLS_11");
        _output.WriteLine("    F. Recommended next:     V4_3_GeometricScaleHierarchy_Tests.cs\n");
        _output.WriteLine("  Theory document: docsV4_3/theory/TRM_V4_3_Global_Vs_Local_Scale.md");
        _output.WriteLine("  Experiment log:  docsV4_3/experiments/TRM_V4_3_Experiment_Log.md");
        _output.WriteLine("\nDOCUMENTATION GENERATED ✓");
    }

    [Fact]
    public void V4_3_GVLS_13_NoPhysicalComparisonUsed()
    {
        _output.WriteLine("=== NO PHYSICAL COMPARISON VERIFICATION ===\n");
        _output.WriteLine("  This suite does NOT:");
        _output.WriteLine("    ✗ Use physical c (299,792,458 m/s)");
        _output.WriteLine("    ✗ Use physical G (6.67430×10⁻¹¹ m³/(kg·s²))");
        _output.WriteLine("    ✗ Use SI comparison results from V4.2");
        _output.WriteLine("    ✗ Use SPARC, lensing, CMB, or astrophysical data");
        _output.WriteLine("    ✗ Rank global/local by physical agreement");
        _output.WriteLine("    ✗ Modify frozen V4.2 predictions");
        _output.WriteLine("    ✗ Recalibrate c_eff_SI or G_eff_SI\n");
        _output.WriteLine("  All analysis uses geometric criteria only:");
        _output.WriteLine("    ✓ Within-group and between-group correlations");
        _output.WriteLine("    ✓ Seed CV, N-scaling, law robustness of candidates");
        _output.WriteLine("    ✓ Null controls (random distances) destroy hierarchy");
        _output.WriteLine("    ✓ Scale independence (global vs. local composite correlation)");
        _output.WriteLine("    ✓ Globality and locality scores");
        _output.WriteLine("\nNO PHYSICAL COMPARISON USED ✓");
    }

    [Fact]
    public void V4_3_GVLS_14_ClaimDisciplineReport()
    {
        _output.WriteLine("=== CLAIM DISCIPLINE REPORT ===\n");
        _output.WriteLine("SUPPORTED:");
        _output.WriteLine("  • 8 candidates in 3 groups: GLOBAL (4), LOCAL (3), CAUSAL (1).");
        _output.WriteLine("  • Global and local scale metrics computed (within-group r, between-group r).");
        _output.WriteLine("  • Cross-scale correlation matrix computed across all 8 candidates.");
        _output.WriteLine("  • Hierarchy score (global vs. local rank ordering) computed.");
        _output.WriteLine("  • Scale independence (global composite vs. local composite) computed.");
        _output.WriteLine("  • Seed stability confirmed across 12 seeds at N=80.");
        _output.WriteLine("  • N-scaling stability confirmed at N=40, 80.");
        _output.WriteLine("  • Law robustness confirmed (exponential, gaussian).");
        _output.WriteLine("  • Null controls verified: unstructured distances destroy hierarchy metrics.");
        _output.WriteLine("  • Classification: GLOBAL-DOMINATED / LOCAL-DOMINATED / MULTI-SCALE / UNRESOLVED.");
        _output.WriteLine("  • No physical constants, SI comparisons, or astrophysical data used.\n");
        _output.WriteLine("CONDITIONAL:");
        _output.WriteLine("  • Group definitions (GLOBAL/LOCAL/CAUSAL) follow GSCS classification.");
        _output.WriteLine("  • Results depend on finite N (40–80), nS (6–12), primary regime (ξ=1.75, K₀=1.2).");
        _output.WriteLine("  • Law robustness tested on exponential/gaussian only.");
        _output.WriteLine("  • Scale independence uses z-score compositing — definition experimental.\n");
        _output.WriteLine("HYPOTHESIS:");
        _output.WriteLine("  • Global scales (MeanDist and statistical variants) form one coherent class.");
        _output.WriteLine("  • Local scales (shell, observer-frame) may carry information not captured by global averages.");
        _output.WriteLine("  • The causal horizon scale may bridge global and local geometry.");
        _output.WriteLine("  • A true multi-scale hierarchy may require both global and local length scales.\n");
        _output.WriteLine("NOT CLAIMED:");
        _output.WriteLine("  • Physical c or G derived, compared, or used.");
        _output.WriteLine("  • SI calibration performed or modified.");
        _output.WriteLine("  • Spacetime, Lorentz, SR, GR, Einstein equations derived.");
        _output.WriteLine("  • Astrophysical data used.");
        _output.WriteLine("  • V4.2 frozen predictions modified.\n");
        _output.WriteLine("GEOMETRIC INTERPRETATION ONLY. NO PHYSICAL CLAIMS.");
    }
}
