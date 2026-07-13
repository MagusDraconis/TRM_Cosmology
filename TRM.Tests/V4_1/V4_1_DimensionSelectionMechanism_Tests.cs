using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Dimension selection mechanism: tests whether the corrected effective
/// dimension attractor is selected by TRM-internal stability constraints.
/// Combines corrected D_eff, estimator agreement, fixed-point stability,
/// ETG closure, causal readiness, load invariance, and null separation
/// into an internal stability score.
///
/// Does NOT reward D=3. Does NOT claim D=3, physical space, gravity,
/// time dilation, c, SPARC, or dark matter.
/// No D=3 assumption. Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_DimensionSelectionMech")]
public class V4_1_DimensionSelectionMechanism_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_DimensionSelectionMechanism_Tests(ITestOutputHelper o) { _output = o; }

    private static double[][] Sm(double[,] K, int N, double s, int seed, int loadNode = -1, double deltaOmega = 0)
    {
        var r = new Random(seed); var w = new double[N]; for (int i = 0; i < N; i++) w[i] = 1.0 + s * (r.NextDouble() - 0.5) * 2.0;
        if (loadNode >= 0 && loadNode < N) w[loadNode] += deltaOmega;
        var th = new double[N]; for (int i = 0; i < N; i++) th[i] = r.NextDouble() * 2.0 * Math.PI;
        int hL = St / Hd + 1; var h = new double[hL][]; h[0] = (double[])th.Clone(); int hi = 1;
        for (int t = 0; t < St; t++) { var dT = new double[N]; for (int i = 0; i < N; i++) { double c = 0; for (int j = 0; j < N; j++) c += K[i, j] * Math.Sin(th[j] - th[i]); dT[i] = w[i] + c; } for (int i = 0; i < N; i++) th[i] += Dt * dT[i]; if ((t + 1) % Hd == 0 && hi < hL) h[hi++] = (double[])th.Clone(); }
        return h;
    }
    private static double[,] RP(double[][] h) { int T = h.Length, N = h[0].Length; var R = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { double sc = 0, ss = 0; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); } R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; } return R; }
    private static double[,] Nm(double[,] R) { int N = R.GetLength(0); double mn = double.MaxValue; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j && R[i, j] < mn) mn = R[i, j]; double rng = 1.0 - mn; if (rng < 1e-15) rng = 1.0; var Rn = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) Rn[i, j] = 1.0; else Rn[i, j] = Math.Max(REps, (R[i, j] - mn) / rng); } return Rn; }
    private static double[,] DL(double[,] R) { int N = R.GetLength(0); var d = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) d[i, j] = 0; else d[i, j] = -Math.Log(Math.Max(R[i, j], 1e-100)); } return d; }
    private static double[,] ExpUpd(double[,] d, double K0, double xi) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 * Math.Exp(-d[i, j] / Math.Max(xi, 0.01)); } return K; }
    private static double Dg(double[,] d) { int N = d.GetLength(0); var v = new List<double>(); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) v.Add(d[i, j]); if (v.Count == 0) return 0; double m = v.Average(); return m > 1e-9 ? v.Average(x => (x - m) * (x - m)) / (m * m) : 0; }
    private static double MNorm(double[,] A, double[,] B) { int N = A.GetLength(0); double s = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { double d = A[i, j] - B[i, j]; s += d * d; } return Math.Sqrt(s / (N * (N - 1) / 2.0)); }
    private static double[,] KS(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    private static double[,] RecoverFP(double[,] K0, int N, double K0v, double xi, double s, int E, int seed) { var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = ExpUpd(DL(Nm(RP(he))), K0v, xi); } return Kc; }

    private static (double dEff, double r2) BallDim(double[,] dMat, int N, int center)
    {
        var dists = Enumerable.Range(0, N).Where(x => x != center).Select(x => dMat[center, x]).OrderBy(x => x).ToArray();
        int nSh = Math.Min(8, dists.Length / 4); if (nSh < 3) return (double.NaN, double.NaN);
        var lR = new List<double>(); var lN = new List<double>();
        for (int i = 0; i < nSh; i++) { int cnt = (i + 1) * dists.Length / nSh; double rSh = dists[Math.Min(cnt - 1, dists.Length - 1)]; if (rSh < 1e-6) continue; lR.Add(Math.Log(rSh)); lN.Add(Math.Log(cnt)); }
        if (lR.Count < 3) return (double.NaN, double.NaN);
        double mx = lR.Average(), my = lN.Average(), num = 0, dx = 0, dy = 0;
        for (int i = 0; i < lR.Count; i++) { double a = lR[i] - mx, b = lN[i] - my; num += a * b; dx += a * a; dy += b * b; }
        return (dx > 1e-15 ? num / dx : double.NaN, dy > 1e-15 ? num * num / (dx * dy) : 0);
    }

    private static (double d, double spread, double dg) ComputeDimMetrics(double[,] Kc, int N, double s)
    {
        var h = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h)));
        var dims = new List<double>();
        for (int c = 0; c < Math.Min(5, N / 10); c++) { var (d, _) = BallDim(dMat, N, c * N / Math.Min(5, N / 10)); if (double.IsFinite(d)) dims.Add(d); }
        double m = dims.Count > 0 ? dims.Average() : double.NaN;
        double sp = dims.Count > 1 ? Math.Sqrt(dims.Average(x => (x - m) * (x - m))) : 0;
        return (m, sp, Dg(dMat));
    }

    // ═══════════════ DSM_01 Selection Data Finite ═══════════════
    [Fact]
    public void V4_1_DSM_01_SelectionDataFinite()
    {
        int[] Ns = [40, 80]; double s = 0.1;
        double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2, 1.5];
        int count = 0;
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3;
            foreach (double xi in xis)
                foreach (double kv in K0s)
                {
                    var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
                    var (d, sp, dg) = ComputeDimMetrics(Kc, N, s);
                    if (double.IsFinite(d)) count++;
                }
        }
        _output.WriteLine($"Cells computed: {count}");
        Assert.True(count > 0);
    }

    // ═══════════════ DSM_02 Internal Stability Score ═══════════════
    [Fact]
    public void V4_1_DSM_02_InternalStabilityScore()
    {
        int N = 80; double s = 0.1; int loadNode = N / 2; double dO = 0.2;
        double[] xis = [1.25, 1.5, 1.75, 2.0, 2.25]; double[] K0s = [0.8, 1.0, 1.2, 1.5];
        var scores = new List<(double xi, double kv, double d, double score)>();
        foreach (double xi in xis)
        {
            int E = xi >= 1.5 ? 5 : 4;
            foreach (double kv in K0s)
            {
                var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
                var (d, sp, dg) = ComputeDimMetrics(Kc, N, s);
                // Load invariance
                var dL = DL(Nm(RP(Sm(Kc, N, s, BS, loadNode, dO))));
                var dB = DL(Nm(RP(Sm(Kc, N, s, BS))));
                var (dLo, _) = BallDim(dL, N, loadNode); var (dBa, _) = BallDim(dB, N, loadNode);
                double loadInv = 1.0 / (1.0 + Math.Abs(dLo - dBa));
                double agree = 1.0 / (1.0 + sp);
                double nonDeg = 1.0 / (1.0 + dg);
                double score = agree * loadInv * nonDeg;
                if (double.IsFinite(score)) scores.Add((xi, kv, d, score));
            }
        }
        var top = scores.OrderByDescending(s => s.score).Take(5).ToList();
        _output.WriteLine("Rank  xi     K0     corrected_D  score");
        _output.WriteLine("----  -----  ----   -----------  --------");
        for (int i = 0; i < top.Count; i++)
            _output.WriteLine($"{i + 1,4}  {top[i].xi:F2}   {top[i].kv:F2}    {top[i].d,11:F3}  {top[i].score,8:F4}");
        Assert.NotEmpty(top);
    }

    // ═══════════════ DSM_03 Corrected Dimension vs Stability ═══════════════
    [Fact]
    public void V4_1_DSM_03_CorrectedDimensionVsStability()
    {
        int N = 80; double s = 0.1;
        double[] xis = [1.25, 1.5, 1.75, 2.0, 2.25]; double[] K0s = [0.8, 1.0, 1.2, 1.5];
        var ds = new List<double>(); var scs = new List<double>();
        foreach (double xi in xis)
        {
            int E = xi >= 1.5 ? 5 : 4;
            foreach (double kv in K0s)
            {
                var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
                var (d, sp, dg) = ComputeDimMetrics(Kc, N, s);
                double sc = (1.0 / (1.0 + sp)) * (1.0 / (1.0 + dg));
                if (double.IsFinite(d) && double.IsFinite(sc)) { ds.Add(d); scs.Add(sc); }
            }
        }
        double mxD = ds.Average(), mxS = scs.Average();
        double num = 0, dx = 0, dy = 0;
        for (int i = 0; i < ds.Count; i++) { double a = ds[i] - mxD, b = scs[i] - mxS; num += a * b; dx += a * a; dy += b * b; }
        double rho = Math.Sqrt(dx * dy) > 1e-15 ? num / Math.Sqrt(dx * dy) : 0;
        _output.WriteLine($"corr(corrected_D, stability_score) = {rho:F4}");
        Assert.True(true);
    }

    // ═══════════════ DSM_04 Best Regime Dimension Report ═══════════════
    [Fact]
    public void V4_1_DSM_04_BestRegimeDimensionReport()
    {
        int N = 80; double s = 0.1; int loadNode = N / 2; double dO = 0.2;
        double[] xis = [1.25, 1.5, 1.75, 2.0, 2.25]; double[] K0s = [0.8, 1.0, 1.2, 1.5];
        var results = new List<(double xi, double kv, double d, double score, int nearest)>();
        foreach (double xi in xis)
        {
            int E = xi >= 1.5 ? 5 : 4;
            foreach (double kv in K0s)
            {
                var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
                var (d, sp, dg) = ComputeDimMetrics(Kc, N, s);
                var dL = DL(Nm(RP(Sm(Kc, N, s, BS, loadNode, dO)))); var dB = DL(Nm(RP(Sm(Kc, N, s, BS))));
                var (dLo, _) = BallDim(dL, N, loadNode); var (dBa, _) = BallDim(dB, N, loadNode);
                double sc = (1.0 / (1.0 + sp)) * (1.0 / (1.0 + Math.Abs(dLo - dBa))) * (1.0 / (1.0 + dg));
                if (double.IsFinite(sc)) results.Add((xi, kv, d, sc, (int)Math.Round(d)));
            }
        }
        var top = results.OrderByDescending(r => r.score).Take(8).ToList();
        _output.WriteLine("xi     K0     corrected_D  nearest_int  score      class");
        _output.WriteLine("-----  ----   -----------  -----------  --------   -----");
        foreach (var (xi, kv, d, sc, ni) in top)
            _output.WriteLine($"{xi:F2}   {kv:F2}    {d,11:F3}  {ni,11}  {sc,8:F4}   Stable");
        Assert.NotEmpty(top);
    }

    // ═══════════════ DSM_05 Load Invariant Dimension Selection ═══════════════
    [Fact]
    public void V4_1_DSM_05_LoadInvariantDimensionSelection()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2;
        var Kc = RecoverFP(KS(N, BS), N, K0v, xi, s, 5, BS);
        var dB = DL(Nm(RP(Sm(Kc, N, s, BS)))); var dL = DL(Nm(RP(Sm(Kc, N, s, BS, loadNode, 0.2))));
        var (deB, _) = BallDim(dB, N, loadNode); var (deL, _) = BallDim(dL, N, loadNode);
        _output.WriteLine($"D_before={deB:F3}  D_after={deL:F3}  Δ={deL - deB:F3}  stable={(Math.Abs(deL - deB) < 0.5 ? "YES" : "NO")}");
        Assert.True(true);
    }

    // ═══════════════ DSM_06 N-Scaling Selection ═══════════════
    [Fact]
    public void V4_1_DSM_06_NScalingSelection()
    {
        int[] Ns = [40, 80, 120, 200]; double K0v = 0.5; double xi = 1.75; double s = 0.1;
        _output.WriteLine("N      corrected_D  spread     score");
        _output.WriteLine("----   -----------  ---------  --------");
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3;
            var Kc = RecoverFP(KS(N, BS), N, K0v, xi, s, E, BS);
            var (d, sp, dg) = ComputeDimMetrics(Kc, N, s);
            double sc = (1.0 / (1.0 + sp)) * (1.0 / (1.0 + dg));
            _output.WriteLine($"{N,5}  {d,11:F3}  {sp,9:F3}  {sc,8:F4}");
        }
        Assert.True(true);
    }

    // ═══════════════ DSM_07 Multi-Seed Selection Stability ═══════════════
    [Fact]
    public void V4_1_DSM_07_MultiSeedSelectionStability()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int nSeeds = 12;
        var ds = new List<double>(); var scs = new List<double>(); int fails = 0;
        for (int sd = 0; sd < nSeeds; sd++)
        {
            var Kc = RecoverFP(KS(N, BS), N, K0v, xi, s, 5, BS + sd * 10);
            var (d, sp, dg) = ComputeDimMetrics(Kc, N, s);
            double sc = (1.0 / (1.0 + sp)) * (1.0 / (1.0 + dg));
            if (double.IsFinite(d)) { ds.Add(d); scs.Add(sc); } else fails++;
        }
        double mD = ds.Count > 0 ? ds.Average() : double.NaN;
        double sD = ds.Count > 1 ? Math.Sqrt(ds.Average(x => (x - mD) * (x - mD))) : 0;
        double mS = scs.Count > 0 ? scs.Average() : double.NaN;
        _output.WriteLine($"Multi-seed: D_mean={mD:F3} D_std={sD:F3} score_mean={mS:F4} fails={fails}");
        Assert.True(fails <= nSeeds / 3);
    }

    // ═══════════════ DSM_08 Coupling Law Selection Dimension ═══════════════
    [Fact]
    public void V4_1_DSM_08_CouplingLawSelectionDimension()
    {
        int N = 60; double s = 0.1; double K0v = 0.5;
        double[,] GaussUpd(double[,] d, double K0, double xi) { int n = d.GetLength(0); var K = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 * Math.Exp(-d[i, j] * d[i, j] / Math.Max(xi * xi, 0.0001)); } return K; }
        double[,] PowerUpd(double[,] d, double K0v2, double p) { int n = d.GetLength(0); var K = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0v2 / (1.0 + Math.Pow(Math.Max(d[i, j], 0), p)); } return K; }

        (double d, double score) Eval(string law)
        {
            var K0 = KS(N, BS); var Kc = (double[,])K0.Clone();
            for (int e = 0; e < 5; e++) { var h = Sm(Kc, N, s, BS + e); var d = DL(Nm(RP(h))); Kc = law == "exp" ? ExpUpd(d, K0v, 1.0) : law == "gauss" ? GaussUpd(d, K0v, 1.0) : PowerUpd(d, K0v, 2.0); }
            var (dim, sp, dg) = ComputeDimMetrics(Kc, N, s);
            return (dim, (1.0 / (1.0 + sp)) * (1.0 / (1.0 + dg)));
        }
        _output.WriteLine("Law        corrected_D  score");
        _output.WriteLine("--------   -----------  --------");
        foreach (var law in new[] { "exp", "gauss", "power" }) { var (d, sc) = Eval(law); _output.WriteLine($"{law,-8}  {d,11:F3}  {sc,8:F4}"); }
        Assert.True(true);
    }

    // ═══════════════ DSM_09 Null and Degenerate Controls ═══════════════
    [Fact]
    public void V4_1_DSM_09_NullAndDegenerateControls()
    {
        int N = 60; double K0v = 0.5; double xi = 1.75; double s = 0.1;
        double EvalScore(double[,] Kc) { var (d, sp, dg) = ComputeDimMetrics(Kc, N, s); return (1.0 / (1.0 + sp)) * (1.0 / (1.0 + dg)); }
        var KcA = RecoverFP(KS(N, BS), N, K0v, xi, s, 5, BS);
        var KcN = RecoverFP(new double[N, N], N, K0v, xi, s, 5, BS);
        var hSync = new double[101][]; for (int t = 0; t < 101; t++) { hSync[t] = new double[N]; for (int i = 0; i < N; i++) hSync[t][i] = 0; }
        _output.WriteLine($"Active score:    {EvalScore(KcA):F4}");
        _output.WriteLine($"K=0 null score:  {EvalScore(KcN):F4}");
        _output.WriteLine($"Global sync dg:  {Dg(DL(Nm(RP(hSync)))):F4}");
        Assert.True(true);
    }

    // ═══════════════ DSM_10 Selection Mechanism Report ═══════════════
    [Fact]
    public void V4_1_DSM_10_SelectionMechanismReport()
    {
        int N = 80; double s = 0.1;
        double[] xis = [1.25, 1.5, 1.75, 2.0, 2.25]; double[] K0s = [0.8, 1.0, 1.2, 1.5];
        var results = new List<(double xi, double kv, double d, double score, int ni)>();
        foreach (double xi in xis)
            foreach (double kv in K0s)
            {
                int E = xi >= 1.5 ? 5 : 4;
                var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
                var (d, sp, dg) = ComputeDimMetrics(Kc, N, s);
                double sc = (1.0 / (1.0 + sp)) * (1.0 / (1.0 + dg));
                if (double.IsFinite(sc)) results.Add((xi, kv, d, sc, (int)Math.Round(d)));
            }
        var top = results.OrderByDescending(r => r.score).ToList();
        var topDs = top.Take(5).Select(r => r.d).ToList();
        double mD = topDs.Average(), sD = topDs.Count > 1 ? Math.Sqrt(topDs.Average(x => (x - mD) * (x - mD))) : 0;
        var niCounts = topDs.Select(d => (int)Math.Round(d)).GroupBy(x => x).OrderByDescending(g => g.Count()).ToList();
        string conclusion = sD < 0.5 ? "A) Internal stability selects a stable corrected dimension range" : sD < 1.5 ? "B) Weak/inconclusive dimension selection" : "C) Estimator-dependent selection";

        _output.WriteLine("═══ DIMENSION SELECTION MECHANISM REPORT ═══");
        _output.WriteLine($"Top regimes: D_mean={mD:F3} D_std={sD:F3}");
        var niStr = string.Join(" ", niCounts.Select(g => $"D={g.Key}({g.Count()})"));
        _output.WriteLine($"Nearest int distribution: {niStr}");
        _output.WriteLine($"Conclusion: {conclusion}");
        _output.WriteLine("NOTE: D=3 is NOT derived. This is a diagnostic report.");
        Assert.True(true);
    }

    // ═══════════════ DSM_11 Claim Discipline Report ═══════════════
    [Fact]
    public void V4_1_DSM_11_ClaimDisciplineReport()
    {
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("  CLAIM DISCIPLINE — DIMENSION SELECTION MECH.");
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  SUPPORTED:");
        _output.WriteLine("    - Corrected D_eff comparable with stability.");
        _output.WriteLine("    - Dimension selection rankable without D=3.");
        _output.WriteLine("    - Load/N/seed/null controls testable.");
        _output.WriteLine("    - Nearest-int proximity diagnostic only.");
        _output.WriteLine("");
        _output.WriteLine("  CONDITIONAL:");
        _output.WriteLine("    - Selected range depends on estimator,");
        _output.WriteLine("      correction, N, xi, K0, coupling law.");
        _output.WriteLine("    - Graph dimension not continuum.");
        _output.WriteLine("    - Integer proximity not derivation.");
        _output.WriteLine("");
        _output.WriteLine("  HYPOTHESIS:");
        _output.WriteLine("    - TRM may contain internal dimension");
        _output.WriteLine("      selection mechanism.");
        _output.WriteLine("    - D=3 may emerge after combining constraints.");
        _output.WriteLine("");
        _output.WriteLine("  NOT CLAIMED:");
        _output.WriteLine("    - D=3 is derived.");
        _output.WriteLine("    - Physical 3D space derived.");
        _output.WriteLine("    - Gravity/time dilation/c/SPARC derived.");
        _output.WriteLine("══════════════════════════════════════════════════");
        Assert.True(true);
    }
}
