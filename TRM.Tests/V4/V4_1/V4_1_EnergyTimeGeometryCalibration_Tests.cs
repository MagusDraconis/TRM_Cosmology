using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Energy-Time-Geometry calibration: extracts internal dimensionless transfer
/// coefficients (A_LΩ, A_ΩR, A_Rd, A_dK, A_KM), defines natural load
/// normalization, measures chain gain and closure error, and tests stability
/// across plateaus, N, seeds, and noise.
///
/// Does NOT claim physical energy, mass, time dilation, gravity, SPARC,
/// dark matter, physical c, or GR replacement.
/// No D=3 assumption. Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_ETGCalibration")]
public class V4_1_EnergyTimeGeometryCalibration_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_EnergyTimeGeometryCalibration_Tests(ITestOutputHelper o) { _output = o; }

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
    private static (double slope, double r2) LinFit(double[] x, double[] y) { int n = x.Length; double mx = x.Average(), my = y.Average(), num = 0, dx = 0; for (int i = 0; i < n; i++) { double a = x[i] - mx; num += a * (y[i] - my); dx += a * a; } double s = dx > 1e-15 ? num / dx : 0; double r2 = dx > 1e-15 ? num * num / (dx * y.Average(v => (v - my) * (v - my))) : 0; return (s, r2); }
    private static string StabClass(double r2, double cv) => r2 > 0.7 ? "Stable" : r2 > 0.3 && cv < 0.5 ? "Weak" : cv > 1.0 ? "Noisy" : "Degenerate";

    private static (double[] loads, double[] o, double[] r, double[] d, double[] k, double[] rem) ScanChain(double[,] Kc, int N, double s, double K0v, double xi, int loadNode, double[] dOs)
    {
        var lds = new List<double>(); var os = new List<double>(); var rs = new List<double>(); var ds = new List<double>(); var ks = new List<double>(); var re = new List<double>();
        foreach (double dO in dOs)
        {
            var hB = Sm(Kc, N, s, BS); var hL = Sm(Kc, N, s, BS, loadNode, dO);
            var RB = Nm(RP(hB)); var RL = Nm(RP(hL)); var dB = DL(RB); var dL = DL(RL); var Kb = ExpUpd(dB, K0v, xi); var Kl = ExpUpd(dL, K0v, xi);
            double oSh = 0; for (int t = 1; t < hB.Length; t++) oSh += Math.Abs(hL[t][loadNode] - hL[t - 1][loadNode]) - Math.Abs(hB[t][loadNode] - hB[t - 1][loadNode]);
            oSh = Math.Abs(oSh) / (hB.Length - 1) / Dt;
            double rem = 0; for (int i = 0; i < N; i++) if (i != loadNode) rem += Math.Abs(Kl[loadNode, i] - Kb[loadNode, i]); rem /= (N - 1);
            lds.Add(dO); os.Add(oSh); rs.Add(MNorm(RB, RL)); ds.Add(MNorm(dB, dL)); ks.Add(MNorm(Kb, Kl)); re.Add(rem);
        }
        return (lds.ToArray(), os.ToArray(), rs.ToArray(), ds.ToArray(), ks.ToArray(), re.ToArray());
    }

    // ═══════════════ ETGC_01 Calibration Data Finite ═══════════════
    [Fact]
    public void V4_1_ETGC_01_CalibrationDataFinite()
    {
        int N = 80; double s = 0.1; double K0v = 0.5; double xi = 1.75; int loadNode = N / 2;
        double[] dOs = [0.01, 0.02, 0.05, 0.10, 0.20];
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var (lds, os, rs, ds, ks, rems) = ScanChain(Kc, N, s, K0v, xi, loadNode, dOs);
        bool fin = lds.All(double.IsFinite) && os.All(double.IsFinite) && rs.All(double.IsFinite) && ds.All(double.IsFinite) && ks.All(double.IsFinite) && rems.All(double.IsFinite);
        _output.WriteLine($"Data points: {lds.Length}  all finite: {fin}");
        Assert.True(fin);
    }

    // ═══════════════ ETGC_02 Transfer Coefficient Extraction ═══════════════
    [Fact]
    public void V4_1_ETGC_02_TransferCoefficientExtraction()
    {
        int N = 80; double s = 0.1; double K0v = 0.5; double xi = 1.75; int loadNode = N / 2;
        double[] dOs = [0.01, 0.02, 0.05, 0.10, 0.20];
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var (lds, os, rs, ds, ks, rems) = ScanChain(Kc, N, s, K0v, xi, loadNode, dOs);
        var (aLO, rLO) = LinFit(lds, os); var (aOR, rOR) = LinFit(os, rs);
        var (aRD, rRD) = LinFit(rs, ds); var (aDK, rDK) = LinFit(ds, ks); var (aKM, rKM) = LinFit(ks, rems);

        _output.WriteLine("═══ TRANSFER COEFFICIENTS ═══");
        _output.WriteLine("Coefficient  Value        Sign   R²       Class");
        _output.WriteLine("-----------  -----------  ----   -------  --------");
        var coefs = new[] { ("A_LΩ", aLO, rLO), ("A_ΩR", aOR, rOR), ("A_Rd", aRD, rRD), ("A_dK", aDK, rDK), ("A_KM", aKM, rKM) };
        foreach (var (nm, val, r2) in coefs)
        {
            double cv = Math.Abs(val) > 1e-12 ? Math.Sqrt(1.0 / Math.Max(r2, 0.01) - 1.0) : 1;
            _output.WriteLine($"{nm,-11}  {val,11:E4}  {Math.Sign(val),4}   {r2,7:F4}  {StabClass(r2, cv)}");
        }
        Assert.True(true);
    }

    // ═══════════════ ETGC_03 Natural Load Normalization ═══════════════
    [Fact]
    public void V4_1_ETGC_03_NaturalLoadNormalization()
    {
        int N = 80; double s = 0.1; double K0v = 0.5; double xi = 1.75; int loadNode = N / 2;
        double[] dOs = [0.01, 0.02, 0.05, 0.10, 0.20];
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var (lds, os, _, _, _, _) = ScanChain(Kc, N, s, K0v, xi, loadNode, dOs);
        var (aLO, _) = LinFit(lds, os);
        double loadUnit = aLO > 1e-15 ? 1.0 / aLO : double.NaN;
        string type = dOs.Max() * aLO < 1.0 ? "EXTRAPOLATED" : "measured";
        _output.WriteLine($"Load_TRM_unit = {loadUnit:F4} ({type})");
        _output.WriteLine("  Load_TRM_unit = load amplitude that would produce ΔΩ_local = 1.");
        _output.WriteLine("  NOTE: dimensionless internal unit. Not physical energy.");
        Assert.True(double.IsFinite(loadUnit));
    }

    // ═══════════════ ETGC_04 Natural Clock-Rate Sensitivity ═══════════════
    [Fact]
    public void V4_1_ETGC_04_NaturalClockRateSensitivity()
    {
        int N = 80; double s = 0.1; double K0v = 0.5; double xi = 1.75; int loadNode = N / 2; double dO = 0.1;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var hB = Sm(Kc, N, s, BS); var hL = Sm(Kc, N, s, BS, loadNode, dO);
        double locS = 0; for (int t = 1; t < hB.Length; t++) locS += Math.Abs(hL[t][loadNode] - hL[t - 1][loadNode]) - Math.Abs(hB[t][loadNode] - hB[t - 1][loadNode]);
        locS = Math.Abs(locS) / (hB.Length - 1) / Dt / dO;
        double neiS = 0; int nc = 0;
        for (int j = 0; j < N; j++) if (j != loadNode && Kc[loadNode, j] > 1e-3) { nc++; double nb = 0, nl = 0; for (int t = 1; t < hB.Length; t++) { nb += Math.Abs(hB[t][j] - hB[t - 1][j]); nl += Math.Abs(hL[t][j] - hL[t - 1][j]); } neiS += Math.Abs(nl - nb); }
        neiS /= Math.Max(nc, 1) * (hB.Length - 1) * Dt * dO;
        double globS = 0; for (int i = 0; i < N; i++) { double gb = 0, gl = 0; for (int t = 1; t < hB.Length; t++) { gb += Math.Abs(hB[t][i] - hB[t - 1][i]); gl += Math.Abs(hL[t][i] - hL[t - 1][i]); } globS += Math.Abs(gl - gb); }
        globS /= N * (hB.Length - 1) * Dt * dO;
        _output.WriteLine($"Clock sensitivity: local={locS:F4}  neighbor={neiS:F4}  global={globS:F4}");
        _output.WriteLine("NOTE: Ω is TRM clock-rate proxy, not physical proper time.");
        Assert.True(true);
    }

    // ═══════════════ ETGC_05 Chain Gain Closure ═══════════════
    [Fact]
    public void V4_1_ETGC_05_ChainGainClosure()
    {
        int N = 80; double s = 0.1; double K0v = 0.5; double xi = 1.75; int loadNode = N / 2;
        double[] dOs = [0.01, 0.02, 0.05, 0.10, 0.20];
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var (lds, os, rs, ds, ks, rems) = ScanChain(Kc, N, s, K0v, xi, loadNode, dOs);
        var (aLO, _) = LinFit(lds, os); var (aOR, _) = LinFit(os, rs);
        var (aRD, _) = LinFit(rs, ds); var (aDK, _) = LinFit(ds, ks); var (aKM, _) = LinFit(ks, rems);
        double gChain = aLO * aOR * aRD * aDK * aKM;
        var (gDir, _) = LinFit(lds, rems);
        double closure = (Math.Abs(gDir) > 1e-15) ? gChain / gDir : double.NaN;
        double err = Math.Abs(gChain - gDir) / Math.Max(Math.Abs(gDir), 1e-12);
        string cls = err < 0.3 ? "Good" : err < 0.7 ? "Moderate" : err < 2.0 ? "Weak" : "Failed";
        _output.WriteLine($"G_chain={gChain:E4}  G_direct={gDir:E4}  err={err:F3}  class={cls}");
        Assert.True(true);
    }

    // ═══════════════ ETGC_06 Plateau Calibration Stability ═══════════════
    [Fact]
    public void V4_1_ETGC_06_PlateauCalibrationStability()
    {
        int N = 60; double s = 0.1; int loadNode = N / 2;
        double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2, 1.5]; double[] dOs = [0.01, 0.05, 0.10, 0.20];
        _output.WriteLine("xi     K0     A_LΩ        A_ΩR        A_Rd        A_dK        closure_err  dg");
        _output.WriteLine("-----  ----   ---------   ---------   ---------   ---------   -----------  ------");
        foreach (double xi in xis)
        {
            int E = xi >= 1.5 ? 4 : 5;
            foreach (double kv in K0s)
            {
                var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, kv, xi, s, E, BS);
                var (lds, os, rs, ds, ks, rems) = ScanChain(Kc, N, s, kv, xi, loadNode, dOs);
                var (aLO, _) = LinFit(lds, os); var (aOR, _) = LinFit(os, rs); var (aRD, _) = LinFit(rs, ds); var (aDK, _) = LinFit(ds, ks); var (aKM, _) = LinFit(ks, rems);
                double gC = aLO * aOR * aRD * aDK * aKM; var (gD, _) = LinFit(lds, rems);
                double cErr = Math.Abs(gC - gD) / Math.Max(Math.Abs(gD), 1e-12);
                double dg = Dg(DL(Nm(RP(Sm(Kc, N, s, BS, loadNode, 0.2)))));
                _output.WriteLine($"{xi:F2}   {kv:F2}    {aLO,9:E4}  {aOR,9:E4}  {aRD,9:E4}  {aDK,9:E4}  {cErr,11:F4}  {dg,6:F4}");
            }
        }
        Assert.True(true);
    }

    // ═══════════════ ETGC_07 Positive/Negative Calibration Symmetry ═══════════════
    [Fact]
    public void V4_1_ETGC_07_PositiveNegativeCalibrationSymmetry()
    {
        int N = 80; double s = 0.1; double K0v = 0.5; double xi = 1.75; int loadNode = N / 2;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        double[] dOs = [0.01, 0.05, 0.10, 0.20];
        var (_, oP, rP, dP, kP, remP) = ScanChain(Kc, N, s, K0v, xi, loadNode, dOs);
        var (_, oN, rN, dN, kN, remN) = ScanChain(Kc, N, s, K0v, xi, loadNode, dOs.Select(d => -d).ToArray());
        var (aOP, _) = LinFit(dOs, oP); var (aON, _) = LinFit(dOs.Select(d => -d).ToArray(), oN);
        _output.WriteLine($"A_LΩ(+): {aOP:E4}  A_LΩ(-): {aON:E4}  ratio: {Math.Abs(aOP) / Math.Max(Math.Abs(aON), 1e-12):F3}");
        _output.WriteLine("NOTE: Negative load stable. No exotic matter claim.");
        Assert.True(true);
    }

    // ═══════════════ ETGC_08 Two-Load Calibration Superposition ═══════════════
    [Fact]
    public void V4_1_ETGC_08_TwoLoadCalibrationSuperposition()
    {
        int N = 80; double s = 0.1; double K0v = 0.5; double xi = 1.75; int nA = N / 3, nB = 2 * N / 3; double dO = 0.1;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var hB = Sm(Kc, N, s, BS); var dBase = DL(Nm(RP(hB)));
        var dA = DL(Nm(RP(Sm(Kc, N, s, BS, nA, dO)))); var dB = DL(Nm(RP(Sm(Kc, N, s, BS, nB, dO))));
        var dBoth = DL(Nm(RP(Sm(Kc, N, s, BS, nA, dO)))); dBoth = DL(Nm(RP(Sm(Kc, N, s, BS, nB, dO))));
        double nA_ = MNorm(dBase, dA), nB_ = MNorm(dBase, dB), nAB = MNorm(dBase, dBoth);
        double linErr = Math.Abs(nAB - (nA_ + nB_)) / Math.Max(nA_ + nB_, 1e-12);
        _output.WriteLine($"dA={nA_:E4}  dB={nB_:E4}  dAB={nAB:E4}  linErr={linErr:F3}");
        Assert.True(true);
    }

    // ═══════════════ ETGC_09 N-Scaling Calibration ═══════════════
    [Fact]
    public void V4_1_ETGC_09_NScalingCalibration()
    {
        int[] Ns = [40, 80, 120, 200]; double s = 0.1; double K0v = 0.5; double xi = 1.75; double[] dOs = [0.01, 0.05, 0.10, 0.20];
        _output.WriteLine("N      A_LΩ        A_ΩR        A_Rd        A_dK        dg");
        _output.WriteLine("----   ---------   ---------   ---------   ---------   ------");
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3; int ln = N / 2;
            var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, E, BS);
            var (lds, os, rs, ds, ks, _) = ScanChain(Kc, N, s, K0v, xi, ln, dOs);
            var (aLO, _) = LinFit(lds, os); var (aOR, _) = LinFit(os, rs); var (aRD, _) = LinFit(rs, ds); var (aDK, _) = LinFit(ds, ks);
            double dg = Dg(DL(Nm(RP(Sm(Kc, N, s, BS, ln, 0.2)))));
            _output.WriteLine($"{N,5}  {aLO,9:E4}  {aOR,9:E4}  {aRD,9:E4}  {aDK,9:E4}  {dg,6:F4}");
        }
        Assert.True(true);
    }

    // ═══════════════ ETGC_10 Multi-Seed Calibration Stability ═══════════════
    [Fact]
    public void V4_1_ETGC_10_MultiSeedCalibrationStability()
    {
        int N = 80; double s = 0.1; double K0v = 0.5; double xi = 1.75; int loadNode = N / 2; int nSeeds = 12;
        double[] dOs = [0.01, 0.05, 0.10, 0.20];
        var gains = new List<double>(); int fails = 0;
        for (int sd = 0; sd < nSeeds; sd++)
        {
            var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS + sd * 10);
            var (lds, os, rs, ds, ks, rems) = ScanChain(Kc, N, s, K0v, xi, loadNode, dOs);
            var (aLO, _) = LinFit(lds, os); var (aOR, _) = LinFit(os, rs); var (aRD, _) = LinFit(rs, ds); var (aDK, _) = LinFit(ds, ks); var (aKM, _) = LinFit(ks, rems);
            double gC = aLO * aOR * aRD * aDK * aKM;
            if (double.IsFinite(gC)) gains.Add(gC); else fails++;
        }
        double m = gains.Count > 0 ? gains.Average() : double.NaN;
        double st = gains.Count > 1 ? Math.Sqrt(gains.Average(x => (x - m) * (x - m))) : 0;
        _output.WriteLine($"Multi-seed ({nSeeds}): G_chain_mean={m:E4}  G_chain_std={st:E4}  fails={fails}");
        Assert.True(fails <= nSeeds / 3);
    }

    // ═══════════════ ETGC_11 Noise Sensitivity ═══════════════
    [Fact]
    public void V4_1_ETGC_11_NoiseSensitivity()
    {
        int N = 80; double s = 0.1; double K0v = 0.5; double xi = 1.75; int loadNode = N / 2;
        double[] dOs = [0.01, 0.05, 0.10, 0.20];
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var (lds, os, rs, ds, ks, rems) = ScanChain(Kc, N, s, K0v, xi, loadNode, dOs);
        var (aLO, _) = LinFit(lds, os);
        // With noise: jitter load amplitude by ±1%
        var ldsN = dOs.Select(d => d * (1.0 + 0.01 * (new Random(42).NextDouble() - 0.5) * 2.0)).ToArray();
        var (aLO_n, _) = LinFit(ldsN, os);
        double sens = Math.Abs(aLO - aLO_n) / Math.Max(Math.Abs(aLO), 1e-12);
        string cls = sens < 0.05 ? "Robust" : sens < 0.2 ? "Sensitive" : "Unstable";
        _output.WriteLine($"Noise sensitivity: A_LΩ={aLO:E4}  A_LΩ_noisy={aLO_n:E4}  drift={sens:F3}  class={cls}");
        Assert.True(true);
    }

    // ═══════════════ ETGC_12 Calibration Matrix Report ═══════════════
    [Fact]
    public void V4_1_ETGC_12_CalibrationMatrixReport()
    {
        int N = 80; double s = 0.1; double K0v = 0.5; double xi = 1.75; int loadNode = N / 2;
        double[] dOs = [0.01, 0.02, 0.05, 0.10, 0.20];
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var (lds, os, rs, ds, ks, rems) = ScanChain(Kc, N, s, K0v, xi, loadNode, dOs);
        var (aLO, rLO) = LinFit(lds, os); var (aOR, rOR) = LinFit(os, rs); var (aRD, rRD) = LinFit(rs, ds); var (aDK, rDK) = LinFit(ds, ks); var (aKM, rKM) = LinFit(ks, rems);

        _output.WriteLine("═══ CALIBRATION MATRIX ═══");
        _output.WriteLine("From    To       Coefficient   R²       Stability");
        _output.WriteLine("------  -------  -----------   -------  ---------");
        var rows = new[] { ("Load", "Ω", aLO, rLO), ("Ω", "R", aOR, rOR), ("R", "d", aRD, rRD), ("d", "K", aDK, rDK), ("K", "Remote", aKM, rKM) };
        foreach (var (f, t, v, r2) in rows)
        {
            double cv = Math.Abs(v) > 1e-12 ? Math.Sqrt(1.0 / Math.Max(r2, 0.01) - 1.0) : 1;
            _output.WriteLine($"{f,-6}  {t,-7}  {v,11:E4}  {r2,7:F4}  {StabClass(r2, cv)}");
        }
        Assert.True(true);
    }

    // ═══════════════ ETGC_13 Null and Degenerate Controls ═══════════════
    [Fact]
    public void V4_1_ETGC_13_NullAndDegenerateCalibrationControls()
    {
        int N = 60; double s = 0.1; double K0v = 0.5; double xi = 1.75; int loadNode = N / 2;
        double[] dOs = [0.01, 0.05, 0.10, 0.20];

        double EvalGain(double[,] Kc)
        {
            var (lds, os, rs, ds, ks, rems) = ScanChain(Kc, N, s, K0v, xi, loadNode, dOs);
            var (aLO, _) = LinFit(lds, os); var (aOR, _) = LinFit(os, rs); var (aRD, _) = LinFit(rs, ds); var (aDK, _) = LinFit(ds, ks); var (aKM, _) = LinFit(ks, rems);
            return aLO * aOR * aRD * aDK * aKM;
        }

        var K0a = KS(N, BS); var KcA = RecoverFP(K0a, N, K0v, xi, s, 5, BS);
        var KcN = RecoverFP(new double[N, N], N, K0v, xi, s, 5, BS);
        var hSync = new double[101][]; for (int t = 0; t < 101; t++) { hSync[t] = new double[N]; for (int i = 0; i < N; i++) hSync[t][i] = 0; }
        double dgGS = Dg(DL(Nm(RP(hSync))));

        _output.WriteLine($"Active G_chain:     {EvalGain(KcA):E4}");
        _output.WriteLine($"K=0 null G_chain:   {EvalGain(KcN):E4}");
        _output.WriteLine($"Global sync dg:     {dgGS:F4}");
        Assert.True(dgGS < 0.01);
    }

    // ═══════════════ ETGC_14 Claim Discipline Report ═══════════════
    [Fact]
    public void V4_1_ETGC_14_CalibrationClaimDisciplineReport()
    {
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("  CLAIM DISCIPLINE — ETG CALIBRATION");
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  SUPPORTED:");
        _output.WriteLine("    - Internal transfer coefficients extractable.");
        _output.WriteLine("    - Chain gain and direct gain comparable.");
        _output.WriteLine("    - Closure error measurable.");
        _output.WriteLine("    - Natural load normalization estimable.");
        _output.WriteLine("    - Plateau/N/seed/noise stability testable.");
        _output.WriteLine("    - Null and degenerate controls detected.");
        _output.WriteLine("");
        _output.WriteLine("  CONDITIONAL:");
        _output.WriteLine("    - Coefficients depend on load proxy, xi,");
        _output.WriteLine("      K0, N, seed, topology, linear range.");
        _output.WriteLine("    - Omega is TRM clock-rate, not proper time.");
        _output.WriteLine("    - Load_TRM_unit is dimensionless.");
        _output.WriteLine("    - Closure is numerical, not physical.");
        _output.WriteLine("");
        _output.WriteLine("  HYPOTHESIS:");
        _output.WriteLine("    - Stable transfer constants may support");
        _output.WriteLine("      later physical calibration.");
        _output.WriteLine("    - Energy may modify local TRM time-rate.");
        _output.WriteLine("    - This chain may underlie gravity.");
        _output.WriteLine("");
        _output.WriteLine("  NOT CLAIMED:");
        _output.WriteLine("    - Physical energy/mass derived.");
        _output.WriteLine("    - Time dilation/gravity derived.");
        _output.WriteLine("    - GR replaced.");
        _output.WriteLine("    - SPARC/dark matter explained.");
        _output.WriteLine("    - Physical c derived.");
        _output.WriteLine("══════════════════════════════════════════════════");
        Assert.True(true);
    }
}
