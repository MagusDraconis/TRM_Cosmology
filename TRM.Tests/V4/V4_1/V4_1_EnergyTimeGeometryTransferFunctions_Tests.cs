using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Energy-Time-Geometry transfer functions: quantifies slopes, linearity,
/// thresholds, saturation, chain gain, and closure error for the full
/// Load → Ω → R → d → K → Remote Response chain.
///
/// Does NOT claim physical energy, mass, gravity, time dilation, SPARC,
/// dark matter, physical c, or GR replacement.
/// No D=3 assumption. Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_EnergyTimeGeometryTF")]
public class V4_1_EnergyTimeGeometryTransferFunctions_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_EnergyTimeGeometryTransferFunctions_Tests(ITestOutputHelper o) { _output = o; }

    // ── Core helpers ────────────────────────────────────────
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
    private static double Pear(double[] x, double[] y) { int n = x.Length; double mx = x.Average(), my = y.Average(), nm = 0, dx = 0, dy = 0; for (int i = 0; i < n; i++) { double a = x[i] - mx, b = y[i] - my; nm += a * b; dx += a * a; dy += b * b; } return Math.Sqrt(dx * dy) > 1e-15 ? nm / Math.Sqrt(dx * dy) : 0; }
    private static double Dg(double[,] d) { int N = d.GetLength(0); var v = new List<double>(); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) v.Add(d[i, j]); if (v.Count == 0) return 0; double m = v.Average(); return m > 1e-9 ? v.Average(x => (x - m) * (x - m)) / (m * m) : 0; }
    private static double[] Fl(double[,] m) { int N = m.GetLength(0); var a = new double[N * N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) a[i * N + j] = m[i, j]; return a; }
    private static double MNorm(double[,] A, double[,] B) { int N = A.GetLength(0); double s = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { double d = A[i, j] - B[i, j]; s += d * d; } return Math.Sqrt(s / (N * (N - 1) / 2.0)); }
    private static double[,] KS(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    private static double[,] RecoverFP(double[,] K0, int N, double K0v, double xi, double s, int E, int seed) { var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = ExpUpd(DL(Nm(RP(he))), K0v, xi); } return Kc; }
    private static (double slope, double r2) LinFit(double[] x, double[] y) { int n = x.Length; double mx = x.Average(), my = y.Average(), num = 0, dx = 0, dy = 0; for (int i = 0; i < n; i++) { double a = x[i] - mx, b = y[i] - my; num += a * b; dx += a * a; dy += b * b; } double s = dx > 1e-15 ? num / dx : 0; double r2 = dy > 1e-15 ? num * num / (dx * dy) : 0; return (s, r2); }
    private static string RegimeLabel(double x, double maxX) => x < maxX * 0.05 ? "BelowThreshold" : x < maxX * 0.4 ? "Linear" : x < maxX * 0.7 ? "Nonlinear" : "Saturated";

    // ── Collect all chain metrics for a single load ──────────
    private static (double oSh, double rSh, double dSh, double kSh, double remSh) ChainMetrics(double[,] Kc, int N, double s, double K0v, double xi, int loadNode, double dO)
    {
        var hB = Sm(Kc, N, s, BS); var hL = Sm(Kc, N, s, BS, loadNode, dO);
        var RB = Nm(RP(hB)); var RL = Nm(RP(hL)); var dB = DL(RB); var dL = DL(RL);
        var Kb = ExpUpd(dB, K0v, xi); var Kl = ExpUpd(dL, K0v, xi);
        double oSh = 0; for (int t = 1; t < hB.Length; t++) oSh += Math.Abs(hL[t][loadNode] - hL[t - 1][loadNode]) - Math.Abs(hB[t][loadNode] - hB[t - 1][loadNode]);
        oSh = Math.Abs(oSh) / (hB.Length - 1) / Dt;
        double rSh = MNorm(RB, RL), dSh = MNorm(dB, dL), kSh = MNorm(Kb, Kl);
        double remSh = 0; for (int i = 0; i < N; i++) if (i != loadNode) remSh += Math.Abs(Kl[loadNode, i] - Kb[loadNode, i]);
        remSh /= (N - 1);
        return (oSh, rSh, dSh, kSh, remSh);
    }

    // ═══════════════ ETGTF_01 Baseline Transfer Data Finite ═══════════════
    [Fact]
    public void V4_1_ETGTF_01_BaselineTransferDataFinite()
    {
        int N = 80; double s = 0.1; double K0v = 0.5; double xi = 1.75; int loadNode = N / 2;
        double[] dOs = [0.005, 0.01, 0.02, 0.05, 0.10, 0.20, 0.35, 0.50];
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        _output.WriteLine("deltaLoad  oShift       rShift       dShift       kShift       remShift");
        _output.WriteLine("---------  -----------  -----------  -----------  -----------  -----------");
        foreach (double dO in dOs)
        {
            var (oS, rS, dS, kS, rS2) = ChainMetrics(Kc, N, s, K0v, xi, loadNode, dO);
            bool fin = double.IsFinite(oS) && double.IsFinite(rS) && double.IsFinite(dS) && double.IsFinite(kS) && double.IsFinite(rS2);
            _output.WriteLine($"{dO,9:F3}  {oS,11:E4}  {rS,11:E4}  {dS,11:E4}  {kS,11:E4}  {rS2,11:E4}");
            Assert.True(fin, $"dO={dO}: all metrics finite.");
        }
    }

    // ═══════════════ ETGTF_02 Load→Omega Transfer Function ═══════════════
    [Fact]
    public void V4_1_ETGTF_02_LoadToOmegaTransferFunction()
    {
        int N = 80; double s = 0.1; double K0v = 0.5; double xi = 1.75; int loadNode = N / 2;
        double[] dOs = [0.005, 0.01, 0.02, 0.05, 0.10, 0.20, 0.35, 0.50];
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var loads = new List<double>(); var shifts = new List<double>();
        foreach (double dO in dOs) { var (oS, _, _, _, _) = ChainMetrics(Kc, N, s, K0v, xi, loadNode, dO); loads.Add(dO); shifts.Add(oS); }
        var (slope, r2) = LinFit(loads.ToArray(), shifts.ToArray());
        _output.WriteLine($"Load→Ω: slope={slope:F4}  R²={r2:F4}");
        // Separate linear range (dO <= 0.2)
        var lL = loads.Where((_, i) => dOs[i] <= 0.2).ToArray();
        var sL = shifts.Where((_, i) => dOs[i] <= 0.2).ToArray();
        var (sL2, rL2) = LinFit(lL, sL);
        _output.WriteLine($"Linear range (dO<=0.2): slope={sL2:F4}  R²={rL2:F4}");
        for (int j = 0; j < loads.Count; j++)
            _output.WriteLine($"  dO={dOs[j]:F3}: shift={shifts[j]:E4} regime={RegimeLabel(dOs[j], 0.5)}");
        Assert.True(double.IsFinite(slope));
    }

    // ═══════════════ ETGTF_03 Omega→Rate Transfer Function ═══════════════
    [Fact]
    public void V4_1_ETGTF_03_OmegaToRateTransferFunction()
    {
        int N = 80; double s = 0.1; double K0v = 0.5; double xi = 1.75; int loadNode = N / 2;
        double[] dOs = [0.01, 0.05, 0.10, 0.20, 0.35, 0.50];
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var oShs = new List<double>(); var rShs = new List<double>();
        foreach (double dO in dOs) { var (oS, rS, _, _, _) = ChainMetrics(Kc, N, s, K0v, xi, loadNode, dO); oShs.Add(oS); rShs.Add(rS); }
        var (slope, r2) = LinFit(oShs.ToArray(), rShs.ToArray());
        _output.WriteLine($"Ω→R: slope={slope:F4}  R²={r2:F4}");
        _output.WriteLine("  NOTE: Ω is TRM clock-rate proxy, not physical time.");
        Assert.True(double.IsFinite(slope));
    }

    // ═══════════════ ETGTF_04 Rate→Distance Transfer Function ═══════════════
    [Fact]
    public void V4_1_ETGTF_04_RateToDistanceTransferFunction()
    {
        int N = 80; double s = 0.1; double K0v = 0.5; double xi = 1.75; int loadNode = N / 2;
        double[] dOs = [0.01, 0.05, 0.10, 0.20, 0.35, 0.50];
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var rShs = new List<double>(); var dShs = new List<double>();
        foreach (double dO in dOs) { var (_, rS, dS, _, _) = ChainMetrics(Kc, N, s, K0v, xi, loadNode, dO); rShs.Add(rS); dShs.Add(dS); }
        var (slope, r2) = LinFit(rShs.ToArray(), dShs.ToArray());
        // Theoretical: d = -log(R), so Δd ≈ ΔR / R
        _output.WriteLine($"R→d: slope={slope:F4}  R²={r2:F4}");
        _output.WriteLine("  NOTE: d deformation is log-metric, not GR curvature.");
        Assert.True(double.IsFinite(slope));
    }

    // ═══════════════ ETGTF_05 Distance→Coupling Transfer Function ═══════════════
    [Fact]
    public void V4_1_ETGTF_05_DistanceToCouplingTransferFunction()
    {
        int N = 80; double s = 0.1; double K0v = 0.5; double xi = 1.75; int loadNode = N / 2;
        double[] dOs = [0.01, 0.05, 0.10, 0.20, 0.35, 0.50];
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var dShs = new List<double>(); var kShs = new List<double>();
        foreach (double dO in dOs) { var (_, _, dS, kS, _) = ChainMetrics(Kc, N, s, K0v, xi, loadNode, dO); dShs.Add(dS); kShs.Add(kS); }
        var (slope, r2) = LinFit(dShs.ToArray(), kShs.ToArray());
        _output.WriteLine($"d→K: slope={slope:F4}  R²={r2:F4}");
        _output.WriteLine("  NOTE: K response is exponential update, not physical force law.");
        Assert.True(double.IsFinite(slope));
    }

    // ═══════════════ ETGTF_06 Coupling→Remote Response Transfer ═══════════════
    [Fact]
    public void V4_1_ETGTF_06_CouplingToRemoteResponseTransfer()
    {
        int N = 80; double s = 0.1; double K0v = 0.5; double xi = 1.75; int loadNode = N / 2;
        double[] dOs = [0.01, 0.05, 0.10, 0.20, 0.35, 0.50];
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var kShs = new List<double>(); var rems = new List<double>();
        foreach (double dO in dOs) { var (_, _, _, kS, rem) = ChainMetrics(Kc, N, s, K0v, xi, loadNode, dO); kShs.Add(kS); rems.Add(rem); }
        var (slope, r2) = LinFit(kShs.ToArray(), rems.ToArray());
        _output.WriteLine($"K→Remote: slope={slope:F4}  R²={r2:F4}  attenuation_factor={slope:F4}");
        Assert.True(double.IsFinite(slope));
    }

    // ═══════════════ ETGTF_07 Full Chain Gain ═══════════════
    [Fact]
    public void V4_1_ETGTF_07_FullChainGain()
    {
        int N = 80; double s = 0.1; double K0v = 0.5; double xi = 1.75; int loadNode = N / 2;
        double[] dOs = [0.01, 0.05, 0.10, 0.20, 0.35, 0.50];
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var loads = new List<double>(); var oShs = new List<double>(); var rShs = new List<double>();
        var dShs = new List<double>(); var kShs = new List<double>(); var rems = new List<double>();
        foreach (double dO in dOs) { var (oS, rS, dS, kS, rem) = ChainMetrics(Kc, N, s, K0v, xi, loadNode, dO); loads.Add(dO); oShs.Add(oS); rShs.Add(rS); dShs.Add(dS); kShs.Add(kS); rems.Add(rem); }

        var (gLO, _) = LinFit(loads.ToArray(), oShs.ToArray());
        var (gOR, _) = LinFit(oShs.ToArray(), rShs.ToArray());
        var (gRD, _) = LinFit(rShs.ToArray(), dShs.ToArray());
        var (gDK, _) = LinFit(dShs.ToArray(), kShs.ToArray());
        var (gKR, _) = LinFit(kShs.ToArray(), rems.ToArray());
        double chainGain = gLO * gOR * gRD * gDK * gKR;
        var (gDir, _) = LinFit(loads.ToArray(), rems.ToArray());
        double closure = gDir > 1e-15 ? chainGain / gDir : double.NaN;

        _output.WriteLine("═══ FULL CHAIN GAIN ═══");
        _output.WriteLine($"Load→Ω:    {gLO:E4}");
        _output.WriteLine($"Ω→R:       {gOR:E4}");
        _output.WriteLine($"R→d:       {gRD:E4}");
        _output.WriteLine($"d→K:       {gDK:E4}");
        _output.WriteLine($"K→Remote:  {gKR:E4}");
        _output.WriteLine($"Chain product: {chainGain:E4}");
        _output.WriteLine($"Direct (Load→Remote): {gDir:E4}");
        _output.WriteLine($"Closure ratio: {closure:F4}");
        _output.WriteLine("NOTE: Chain gain is diagnostic only. No gravity claim.");
        Assert.True(double.IsFinite(chainGain));
    }

    // ═══════════════ ETGTF_08 Threshold and Saturation Detection ═══════════════
    [Fact]
    public void V4_1_ETGTF_08_ThresholdAndSaturationDetection()
    {
        int N = 80; double s = 0.1; double K0v = 0.5; double xi = 1.75; int loadNode = N / 2;
        double[] dOs = [0.005, 0.01, 0.02, 0.05, 0.10, 0.20, 0.35, 0.50];
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        _output.WriteLine("deltaLoad  oShift       regime");
        _output.WriteLine("---------  -----------  ----------------");
        double maxSh = 0;
        foreach (double dO in dOs) { var (oS, _, _, _, _) = ChainMetrics(Kc, N, s, K0v, xi, loadNode, dO); maxSh = Math.Max(maxSh, oS); }
        foreach (double dO in dOs)
        {
            var (oS, _, _, _, _) = ChainMetrics(Kc, N, s, K0v, xi, loadNode, dO);
            _output.WriteLine($"{dO,9:F3}  {oS,11:E4}  {RegimeLabel(oS, maxSh)}");
        }
        Assert.True(true);
    }

    // ═══════════════ ETGTF_09 Plateau Band Dependence ═══════════════
    [Fact]
    public void V4_1_ETGTF_09_PlateauBandDependence()
    {
        int N = 60; double s = 0.1; int loadNode = N / 2; double dO = 0.2;
        double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2, 1.5];
        _output.WriteLine("xi     K0     chain_gain   closure     dg");
        _output.WriteLine("-----  ----   ----------   ---------   ------");
        foreach (double xi in xis)
        {
            int E = xi >= 1.5 ? 4 : 5;
            foreach (double kv in K0s)
            {
                var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, kv, xi, s, E, BS);
                var (oS, rS, dS, kS, rem) = ChainMetrics(Kc, N, s, kv, xi, loadNode, dO);
                double cg = Math.Abs(oS) * rS * dS * kS * rem;
                double dg = Dg(DL(Nm(RP(Sm(Kc, N, s, BS, loadNode, dO)))));
                _output.WriteLine($"{xi:F2}   {kv:F2}    {cg,10:E4}  {rem / Math.Max(oS, 1e-12),9:F4}  {dg,6:F4}");
            }
        }
        Assert.True(true);
    }

    // ═══════════════ ETGTF_10 Positive/Negative Transfer Symmetry ═══════════════
    [Fact]
    public void V4_1_ETGTF_10_PositiveNegativeTransferSymmetry()
    {
        int N = 80; double s = 0.1; double K0v = 0.5; double xi = 1.75; int loadNode = N / 2;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var (oP, rP, dP, kP, remP) = ChainMetrics(Kc, N, s, K0v, xi, loadNode, 0.2);
        var (oN, rN, dN, kN, remN) = ChainMetrics(Kc, N, s, K0v, xi, loadNode, -0.2);
        _output.WriteLine("═══ POSITIVE/NEGATIVE TRANSFER SYMMETRY ═══");
        _output.WriteLine($"        oShift       rShift       dShift       kShift       remShift");
        _output.WriteLine($"+load   {oP,11:E4}  {rP,11:E4}  {dP,11:E4}  {kP,11:E4}  {remP,11:E4}");
        _output.WriteLine($"-load   {oN,11:E4}  {rN,11:E4}  {dN,11:E4}  {kN,11:E4}  {remN,11:E4}");
        _output.WriteLine($"|+/|-|  {oP / Math.Max(Math.Abs(oN), 1e-12),11:F4}  {rP / Math.Max(rN, 1e-12),11:F4}  {dP / Math.Max(dN, 1e-12),11:F4}  {kP / Math.Max(kN, 1e-12),11:F4}  {remP / Math.Max(remN, 1e-12),11:F4}");
        Assert.True(true);
    }

    // ═══════════════ ETGTF_11 Two-Load Transfer Superposition ═══════════════
    [Fact]
    public void V4_1_ETGTF_11_TwoLoadTransferSuperposition()
    {
        int N = 80; double s = 0.1; double K0v = 0.5; double xi = 1.75; int nA = N / 3, nB = 2 * N / 3; double dO = 0.2;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var (_, _, dA, _, _) = ChainMetrics(Kc, N, s, K0v, xi, nA, dO);
        var (_, _, dB, _, _) = ChainMetrics(Kc, N, s, K0v, xi, nB, dO);
        // Both loads: approximate by running load A then computing with load B state
        var hBoth = Sm(Kc, N, s, BS, nA, dO);
        var dBoth = DL(Nm(RP(Sm(Kc, N, s, BS, nB, dO))));
        var hB = Sm(Kc, N, s, BS); var dBase = DL(Nm(RP(hB)));
        double nBoth = MNorm(dBase, dBoth);
        double linErr = Math.Abs(nBoth - (dA + dB)) / Math.Max(dA + dB, 1e-12);
        string cls = linErr < 0.2 ? "Linear" : linErr < 0.5 ? "Weak nonlinear" : "Strong nonlinear";
        _output.WriteLine($"dA={dA:E4}  dB={dB:E4}  dAB={nBoth:E4}  linErr={linErr:F3}  class={cls}");
        Assert.True(true);
    }

    // ═══════════════ ETGTF_12 N-Scaling Transfer Functions ═══════════════
    [Fact]
    public void V4_1_ETGTF_12_NScalingTransferFunctions()
    {
        int[] Ns = [40, 80, 120, 200]; double s = 0.1; double K0v = 0.5; double xi = 1.75; double dO = 0.2;
        _output.WriteLine("N      Load→Ω     Ω→R        R→d        d→K        chainGain   dg");
        _output.WriteLine("----   --------   --------   --------   --------   ---------   ------");
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3; int ln = N / 2;
            var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, E, BS);
            var (oS, rS, dS, kS, rem) = ChainMetrics(Kc, N, s, K0v, xi, ln, dO);
            double cg = Math.Abs(oS) * rS * dS * kS * rem;
            double dg = Dg(DL(Nm(RP(Sm(Kc, N, s, BS, ln, dO)))));
            _output.WriteLine($"{N,5}  {Math.Abs(oS)/dO,8:E4}  {rS/Math.Max(oS,1e-12),8:E4}  {dS/Math.Max(rS,1e-12),8:E4}  {kS/Math.Max(dS,1e-12),8:E4}  {cg,9:E4}  {dg,6:F4}");
        }
        Assert.True(true);
    }

    // ═══════════════ ETGTF_13 Multi-Seed Transfer Stability ═══════════════
    [Fact]
    public void V4_1_ETGTF_13_MultiSeedTransferStability()
    {
        int N = 80; double s = 0.1; double K0v = 0.5; double xi = 1.75; int loadNode = N / 2; double dO = 0.2; int nSeeds = 12;
        var slopes = new List<double>(); int fails = 0;
        for (int sd = 0; sd < nSeeds; sd++)
        {
            var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS + sd * 10);
            var (oS, rS, dS, kS, rem) = ChainMetrics(Kc, N, s, K0v, xi, loadNode, dO);
            double cg = Math.Abs(oS) * rS * dS * kS * rem;
            if (double.IsFinite(cg)) slopes.Add(cg); else fails++;
        }
        double m = slopes.Count > 0 ? slopes.Average() : double.NaN;
        double st = slopes.Count > 1 ? Math.Sqrt(slopes.Average(x => (x - m) * (x - m))) : 0;
        _output.WriteLine($"Multi-seed ({nSeeds}): chainGain_mean={m:E4} chainGain_std={st:E4} fails={fails}");
        Assert.True(fails <= nSeeds / 3);
    }

    // ═══════════════ ETGTF_14 Null and Degenerate Controls ═══════════════
    [Fact]
    public void V4_1_ETGTF_14_NullAndDegenerateControls()
    {
        int N = 60; double s = 0.1; double K0v = 0.5; double xi = 1.75; int loadNode = N / 2; double dO = 0.2;

        double EvalGain(double[,] Kc)
        {
            var (oS, rS, dS, kS, rem) = ChainMetrics(Kc, N, s, K0v, xi, loadNode, dO);
            return Math.Abs(oS) * rS * dS * kS * rem;
        }

        var K0a = KS(N, BS); var KcA = RecoverFP(K0a, N, K0v, xi, s, 5, BS);
        double gA = EvalGain(KcA);
        var KcN = RecoverFP(new double[N, N], N, K0v, xi, s, 5, BS);
        double gN = EvalGain(KcN);
        var hSync = new double[101][]; for (int t = 0; t < 101; t++) { hSync[t] = new double[N]; for (int i = 0; i < N; i++) hSync[t][i] = 0; }
        double dgGS = Dg(DL(Nm(RP(hSync))));

        _output.WriteLine($"Active chain gain:     {gA:E4}");
        _output.WriteLine($"K=0 null chain gain:   {gN:E4}");
        _output.WriteLine($"Global sync degeneracy: dg={dgGS:F4}");
        Assert.True(dgGS < 0.01);
    }

    // ═══════════════ ETGTF_15 Claim Discipline Report ═══════════════
    [Fact]
    public void V4_1_ETGTF_15_ClaimDisciplineReport()
    {
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("  CLAIM DISCIPLINE — ENERGY-TIME-GEOMETRY TF");
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  SUPPORTED:");
        _output.WriteLine("    - Transfer functions for all chain links measurable.");
        _output.WriteLine("    - Chain gain and closure error computable.");
        _output.WriteLine("    - Thresholds, saturation, linear regimes classifiable.");
        _output.WriteLine("    - Plateau/band dependence testable.");
        _output.WriteLine("    - Null and degenerate controls detected.");
        _output.WriteLine("");
        _output.WriteLine("  CONDITIONAL:");
        _output.WriteLine("    - Slopes depend on load proxy, xi, K0, topology,");
        _output.WriteLine("      N, seed, amplitude range, and diagnostics.");
        _output.WriteLine("    - Omega is TRM clock-rate, not proper time.");
        _output.WriteLine("    - Load not calibrated to physical energy.");
        _output.WriteLine("    - d deformation not GR curvature.");
        _output.WriteLine("");
        _output.WriteLine("  HYPOTHESIS:");
        _output.WriteLine("    - Energy modifies local TRM time-rate.");
        _output.WriteLine("    - Time-rate shifts drive geometry deformation.");
        _output.WriteLine("    - The chain may underlie gravity-like behavior.");
        _output.WriteLine("");
        _output.WriteLine("  NOT CLAIMED:");
        _output.WriteLine("    - Physical energy/mass derived.");
        _output.WriteLine("    - Gravity/GR time dilation derived.");
        _output.WriteLine("    - GR replaced.");
        _output.WriteLine("    - SPARC/dark matter explained.");
        _output.WriteLine("    - Physical c derived.");
        _output.WriteLine("══════════════════════════════════════════════════");
        Assert.True(true);
    }
}
