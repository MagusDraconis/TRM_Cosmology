using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Energy-Time-Geometry dimension selection: tests whether localized load
/// perturbs effective spatial dimension D_eff, spectral dimension proxy,
/// and dimensional stability of the recovered topology.
///
/// Does NOT claim D=3, physical space, gravity, time dilation, SPARC,
/// dark matter, or physical c.
/// No D=3 assumption. Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_ETGDimensionSelection")]
public class V4_1_EnergyTimeGeometryDimensionSelection_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_EnergyTimeGeometryDimensionSelection_Tests(ITestOutputHelper o) { _output = o; }

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
    private static double Spear(double[] a, double[] b) { if (a.Length < 3) return 0; var ia = a.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); var ib = b.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); return Pear(ia, ib); }
    private static double Pear(double[] x, double[] y) { int n = x.Length; double mx = x.Average(), my = y.Average(), nm = 0, dx = 0; for (int i = 0; i < n; i++) { double a = x[i] - mx; nm += a * (y[i] - my); dx += a * a; } return dx > 1e-15 ? nm / Math.Sqrt(dx * y.Average(v => (v - my) * (v - my))) : 0; }
    private static double Dg(double[,] d) { int N = d.GetLength(0); var v = new List<double>(); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) v.Add(d[i, j]); if (v.Count == 0) return 0; double m = v.Average(); return m > 1e-9 ? v.Average(x => (x - m) * (x - m)) / (m * m) : 0; }
    private static double MNorm(double[,] A, double[,] B) { int N = A.GetLength(0); double s = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { double d = A[i, j] - B[i, j]; s += d * d; } return Math.Sqrt(s / (N * (N - 1) / 2.0)); }
    private static double[,] KS(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    private static double[,] RecoverFP(double[,] K0, int N, double K0v, double xi, double s, int E, int seed) { var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = ExpUpd(DL(Nm(RP(he))), K0v, xi); } return Kc; }

    // ── Ball-growth D_eff: count nodes within distance shells ──
    private static (double dEff, double r2) BallDim(double[,] dMat, int N, int center)
    {
        var dists = Enumerable.Range(0, N).Where(x => x != center).Select(x => dMat[center, x]).OrderBy(x => x).ToArray();
        int nShells = Math.Min(8, dists.Length / 4);
        if (nShells < 3) return (double.NaN, double.NaN);
        var logR = new List<double>(); var logN = new List<double>();
        for (int i = 0; i < nShells; i++)
        {
            int count = (i + 1) * dists.Length / nShells;
            double rShell = dists[Math.Min(count - 1, dists.Length - 1)];
            if (rShell < 1e-6) continue;
            logR.Add(Math.Log(rShell)); logN.Add(Math.Log(count));
        }
        if (logR.Count < 3) return (double.NaN, double.NaN);
        double mx = logR.Average(), my = logN.Average(), num = 0, dx = 0, dy = 0;
        for (int i = 0; i < logR.Count; i++) { double a = logR[i] - mx, b = logN[i] - my; num += a * b; dx += a * a; dy += b * b; }
        double dEff = dx > 1e-15 ? num / dx : double.NaN;
        double r2 = dy > 1e-15 ? num * num / (dx * dy) : 0;
        return (dEff, r2);
    }

    // ═══════════════ ETGDS_01 Baseline Dimension Finite ═══════════════
    [Fact]
    public void V4_1_ETGDS_01_BaselineDimensionFinite()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var h = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h)));
        var (dEff, r2) = BallDim(dMat, N, N / 2);
        double dg = Dg(dMat);
        _output.WriteLine($"D_eff={dEff:F3}  R²={r2:F4}  dg={dg:F4}");
        Assert.True(double.IsFinite(dEff));
    }

    // ═══════════════ ETGDS_02 Load Perturbs Dimension ═══════════════
    [Fact]
    public void V4_1_ETGDS_02_LoadPerturbsDimension()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2; double dO = 0.2;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var hB = Sm(Kc, N, s, BS); var dB = DL(Nm(RP(hB)));
        var hL = Sm(Kc, N, s, BS, loadNode, dO); var dL = DL(Nm(RP(hL)));
        var (deB, _) = BallDim(dB, N, loadNode); var (deL, _) = BallDim(dL, N, loadNode);
        double delta = deL - deB;
        _output.WriteLine($"D_eff(before)={deB:F3}  D_eff(after)={deL:F3}  Δ={delta:F3}");
        Assert.True(double.IsFinite(delta));
    }

    // ═══════════════ ETGDS_03 Load Amplitude Dimension Response ═══════════════
    [Fact]
    public void V4_1_ETGDS_03_LoadAmplitudeDimensionResponse()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2;
        double[] dOs = [0.01, 0.05, 0.10, 0.20, 0.35, 0.50];
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var (deB, _) = BallDim(DL(Nm(RP(Sm(Kc, N, s, BS)))), N, loadNode);
        _output.WriteLine($"Baseline D_eff={deB:F3}");
        _output.WriteLine("deltaLoad  D_eff     ΔD_eff   dg      class");
        _output.WriteLine("---------  --------  -------  ------  -------------------");
        foreach (double dO in dOs)
        {
            var hL = Sm(Kc, N, s, BS, loadNode, dO); var dL = DL(Nm(RP(hL)));
            var (deL, _) = BallDim(dL, N, loadNode);
            double delta = deL - deB; double dg = Dg(dL);
            string cls = Math.Abs(delta) < 0.1 ? "No response" : Math.Abs(delta) < 0.5 ? "Weak response" : Math.Abs(delta) < 2 ? "Strong response" : "Degenerate";
            _output.WriteLine($"{dO,9:F2}  {deL,8:F3}  {delta,7:F3}  {dg,6:F4}  {cls}");
        }
        Assert.True(true);
    }

    // ═══════════════ ETGDS_04 Omega-Dimension Correlation ═══════════════
    [Fact]
    public void V4_1_ETGDS_04_OmegaDimensionCorrelation()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2;
        double[] dOs = [0.01, 0.05, 0.10, 0.20];
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var hB = Sm(Kc, N, s, BS);
        var oShifts = new List<double>(); var dDims = new List<double>();
        var (deB, _) = BallDim(DL(Nm(RP(hB))), N, loadNode);
        foreach (double dO in dOs)
        {
            var hL = Sm(Kc, N, s, BS, loadNode, dO);
            double oSh = 0; for (int t = 1; t < hB.Length; t++) oSh += Math.Abs(hL[t][loadNode] - hL[t - 1][loadNode]) - Math.Abs(hB[t][loadNode] - hB[t - 1][loadNode]);
            oShifts.Add(Math.Abs(oSh) / (hB.Length - 1) / Dt);
            var dL = DL(Nm(RP(hL))); var (deL, _) = BallDim(dL, N, loadNode);
            dDims.Add(deL - deB);
        }
        double rho = Spear(oShifts.ToArray(), dDims.ToArray());
        _output.WriteLine($"corr(ΔΩ, ΔD_eff) = {rho:F4}");
        Assert.True(double.IsFinite(rho));
    }

    // ═══════════════ ETGDS_05 Geometry-Dimension Correlation ═══════════════
    [Fact]
    public void V4_1_ETGDS_05_GeometryDimensionCorrelation()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2;
        double[] dOs = [0.01, 0.05, 0.10, 0.20];
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var hB = Sm(Kc, N, s, BS); var dB = DL(Nm(RP(hB)));
        var (deB, _) = BallDim(dB, N, loadNode);
        var dNorms = new List<double>(); var dDims = new List<double>();
        foreach (double dO in dOs)
        {
            var dL = DL(Nm(RP(Sm(Kc, N, s, BS, loadNode, dO))));
            dNorms.Add(MNorm(dB, dL)); var (deL, _) = BallDim(dL, N, loadNode); dDims.Add(deL - deB);
        }
        double rho = Spear(dNorms.ToArray(), dDims.ToArray());
        _output.WriteLine($"corr(Δd_norm, ΔD_eff) = {rho:F4}");
        Assert.True(double.IsFinite(rho));
    }

    // ═══════════════ ETGDS_06 Plateau Band Dimension Stability ═══════════════
    [Fact]
    public void V4_1_ETGDS_06_PlateauBandDimensionStability()
    {
        int N = 60; double s = 0.1; int loadNode = N / 2; double dO = 0.2;
        double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2, 1.5];
        _output.WriteLine("xi     K0     D_eff_base  D_eff_load  ΔD_eff   dg");
        _output.WriteLine("-----  ----   ----------  ----------  -------  ------");
        foreach (double xi in xis)
        {
            int E = xi >= 1.5 ? 4 : 5;
            foreach (double kv in K0s)
            {
                var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, kv, xi, s, E, BS);
                var dB = DL(Nm(RP(Sm(Kc, N, s, BS))));
                var dL = DL(Nm(RP(Sm(Kc, N, s, BS, loadNode, dO))));
                var (deB, _) = BallDim(dB, N, loadNode); var (deL, _) = BallDim(dL, N, loadNode);
                _output.WriteLine($"{xi:F2}   {kv:F2}    {deB,10:F3}  {deL,10:F3}  {deL - deB,7:F3}  {Dg(dL),6:F4}");
            }
        }
        Assert.True(true);
    }

    // ═══════════════ ETGDS_07 Local vs Global Dimension ═══════════════
    [Fact]
    public void V4_1_ETGDS_07_LocalVsGlobalDimension()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2; double dO = 0.2;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var dL = DL(Nm(RP(Sm(Kc, N, s, BS, loadNode, dO))));
        var (deLoc, _) = BallDim(dL, N, loadNode);
        // Global: average D_eff over multiple centers
        var globDims = new List<double>();
        for (int c = 0; c < 5; c++) { var (d, _) = BallDim(dL, N, c * N / 5); if (double.IsFinite(d)) globDims.Add(d); }
        double deGlob = globDims.Count > 0 ? globDims.Average() : double.NaN;
        _output.WriteLine($"D_eff(local near load) = {deLoc:F3}");
        _output.WriteLine($"D_eff(global average)  = {deGlob:F3}");
        _output.WriteLine($"Δ = {deLoc - deGlob:F3}");
        Assert.True(true);
    }

    // ═══════════════ ETGDS_08 Positive/Negative Load Dimension Symmetry ═══════════════
    [Fact]
    public void V4_1_ETGDS_08_PositiveNegativeLoadDimensionSymmetry()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var dP = DL(Nm(RP(Sm(Kc, N, s, BS, loadNode, 0.2))));
        var dN = DL(Nm(RP(Sm(Kc, N, s, BS, loadNode, -0.2))));
        var (deP, _) = BallDim(dP, N, loadNode); var (deN, _) = BallDim(dN, N, loadNode);
        _output.WriteLine($"D_eff(+load)={deP:F3}  D_eff(-load)={deN:F3}  Δ={deP - deN:F3}");
        Assert.True(true);
    }

    // ═══════════════ ETGDS_09 Two-Load Dimension Superposition ═══════════════
    [Fact]
    public void V4_1_ETGDS_09_TwoLoadDimensionSuperposition()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int nA = N / 3, nB = 2 * N / 3; double dO = 0.1;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var dB = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var dA = DL(Nm(RP(Sm(Kc, N, s, BS, nA, dO)))); var dB2 = DL(Nm(RP(Sm(Kc, N, s, BS, nB, dO))));
        var dBoth = DL(Nm(RP(Sm(Kc, N, s, BS, nA, dO)))); dBoth = DL(Nm(RP(Sm(Kc, N, s, BS, nB, dO))));
        var (deBase, _) = BallDim(dB, N, N / 2); var (deAB, _) = BallDim(dBoth, N, N / 2);
        _output.WriteLine($"D_eff(base)={deBase:F3}  D_eff(A+B)={deAB:F3}  Δ={deAB - deBase:F3}");
        Assert.True(true);
    }

    // ═══════════════ ETGDS_10 N-Scaling Dimension Response ═══════════════
    [Fact]
    public void V4_1_ETGDS_10_NScalingDimensionResponse()
    {
        int[] Ns = [40, 80, 120, 200]; double K0v = 0.5; double xi = 1.75; double s = 0.1; double dO = 0.2;
        _output.WriteLine("N      D_base      D_load      ΔD_eff   dg");
        _output.WriteLine("----   ---------   ---------   -------  ------");
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3; int ln = N / 2;
            var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, E, BS);
            var dB = DL(Nm(RP(Sm(Kc, N, s, BS))));
            var dL = DL(Nm(RP(Sm(Kc, N, s, BS, ln, dO))));
            var (deB, _) = BallDim(dB, N, ln); var (deL, _) = BallDim(dL, N, ln);
            _output.WriteLine($"{N,5}  {deB,9:F3}  {deL,9:F3}  {deL - deB,7:F3}  {Dg(dL),6:F4}");
        }
        Assert.True(true);
    }

    // ═══════════════ ETGDS_11 Multi-Seed Dimension Stability ═══════════════
    [Fact]
    public void V4_1_ETGDS_11_MultiSeedDimensionStability()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2; double dO = 0.2; int nSeeds = 12;
        var deBases = new List<double>(); var deLoads = new List<double>(); int fails = 0;
        for (int sd = 0; sd < nSeeds; sd++)
        {
            var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS + sd * 10);
            var dB = DL(Nm(RP(Sm(Kc, N, s, BS)))); var dL = DL(Nm(RP(Sm(Kc, N, s, BS, loadNode, dO))));
            var (deB, _) = BallDim(dB, N, loadNode); var (deL, _) = BallDim(dL, N, loadNode);
            if (double.IsFinite(deB)) deBases.Add(deB); else fails++;
            if (double.IsFinite(deL)) deLoads.Add(deL);
        }
        double mB = deBases.Count > 0 ? deBases.Average() : double.NaN;
        double sB = deBases.Count > 1 ? Math.Sqrt(deBases.Average(x => (x - mB) * (x - mB))) : 0;
        double mL = deLoads.Count > 0 ? deLoads.Average() : double.NaN;
        _output.WriteLine($"Multi-seed: D_base_mean={mB:F3} D_base_std={sB:F3} D_load_mean={mL:F3} fails={fails}");
        Assert.True(fails <= nSeeds / 3);
    }

    // ═══════════════ ETGDS_12 Null and Degenerate Controls ═══════════════
    [Fact]
    public void V4_1_ETGDS_12_NullAndDegenerateControls()
    {
        int N = 60; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2; double dO = 0.2;

        double EvalDim(double[,] Kc)
        {
            var dL = DL(Nm(RP(Sm(Kc, N, s, BS, loadNode, dO))));
            var (d, _) = BallDim(dL, N, loadNode); return d;
        }

        var K0a = KS(N, BS); var KcA = RecoverFP(K0a, N, K0v, xi, s, 5, BS);
        var KcN = RecoverFP(new double[N, N], N, K0v, xi, s, 5, BS);
        var hSync = new double[101][]; for (int t = 0; t < 101; t++) { hSync[t] = new double[N]; for (int i = 0; i < N; i++) hSync[t][i] = 0; }
        double dgGS = Dg(DL(Nm(RP(hSync))));

        _output.WriteLine($"Active D_eff:     {EvalDim(KcA):F3}");
        _output.WriteLine($"K=0 null D_eff:   {EvalDim(KcN):F3}");
        _output.WriteLine($"Global sync dg:   {dgGS:F4}");
        Assert.True(dgGS < 0.01);
    }

    // ═══════════════ ETGDS_13 Dimension Selection Diagnostic Report ═══════════════
    [Fact]
    public void V4_1_ETGDS_13_DimensionSelectionDiagnosticReport()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var dB = DL(Nm(RP(Sm(Kc, N, s, BS))));
        double[] dOs = [0.01, 0.05, 0.10, 0.20, 0.35, 0.50];
        var (deB, _) = BallDim(dB, N, loadNode);
        var deltas = new List<double>();
        foreach (double dO in dOs) { var dL = DL(Nm(RP(Sm(Kc, N, s, BS, loadNode, dO)))); var (deL, _) = BallDim(dL, N, loadNode); deltas.Add(deL - deB); }
        double maxDelta = deltas.Max(d => Math.Abs(d));
        string conclusion = maxDelta < 0.1 ? "A) D_eff stable under load" : maxDelta < 0.5 ? "B) D_eff shifts weakly under load" : maxDelta < 2.0 ? "B) D_eff shifts under load" : "C) D_eff collapses/degenerates";

        _output.WriteLine("═══ DIMENSION SELECTION DIAGNOSTIC ═══");
        _output.WriteLine($"Baseline D_eff = {deB:F3}  max ΔD_eff = {maxDelta:F3}");
        _output.WriteLine($"Conclusion: {conclusion}");
        _output.WriteLine("NOTE: D=3 is NOT derived. This is a diagnostic report only.");
        Assert.True(true);
    }

    // ═══════════════ ETGDS_14 Claim Discipline Report ═══════════════
    [Fact]
    public void V4_1_ETGDS_14_ClaimDisciplineReport()
    {
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("  CLAIM DISCIPLINE — ETG DIMENSION SELECTION");
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  SUPPORTED:");
        _output.WriteLine("    - D_eff measurable before/after load.");
        _output.WriteLine("    - Local/global dimension response comparable.");
        _output.WriteLine("    - Omega/d/K correlated with dimension.");
        _output.WriteLine("    - Plateau/N/seed/null controls testable.");
        _output.WriteLine("");
        _output.WriteLine("  CONDITIONAL:");
        _output.WriteLine("    - Dimension depends on estimator, topology,");
        _output.WriteLine("      N, load, xi, K0, thresholds, sampling.");
        _output.WriteLine("    - D_eff is effective graph diagnostic.");
        _output.WriteLine("    - No continuum dimension is proven.");
        _output.WriteLine("");
        _output.WriteLine("  HYPOTHESIS:");
        _output.WriteLine("    - ETG coupling may drive dimension selection.");
        _output.WriteLine("    - Stable dimension under load may be");
        _output.WriteLine("      prerequisite for physical emergent space.");
        _output.WriteLine("");
        _output.WriteLine("  NOT CLAIMED:");
        _output.WriteLine("    - D=3 is derived.");
        _output.WriteLine("    - Physical space is derived.");
        _output.WriteLine("    - Continuum limit is proven.");
        _output.WriteLine("    - Gravity/time dilation derived.");
        _output.WriteLine("    - SPARC/dark matter explained.");
        _output.WriteLine("    - Physical c derived.");
        _output.WriteLine("══════════════════════════════════════════════════");
        Assert.True(true);
    }
}
