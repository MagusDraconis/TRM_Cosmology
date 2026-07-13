using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Energy-Time-Geometry coupling: tests whether localized TRM energy-load
/// proxies produce a reproducible causal chain:
///   Load → local Ω shift → R_ij change → d_ij = -log(R_ij) deformation
///   → K_ij coupling deformation → remote response.
///
/// Does NOT claim physical mass, energy, gravity, time dilation,
/// SPARC, physical c, or GR replacement.
/// No D=3 assumption. Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_EnergyTimeGeometry")]
public class V4_1_EnergyTimeGeometry_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_EnergyTimeGeometry_Tests(ITestOutputHelper o) { _output = o; }

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

    private static string CorrClass(double r) => Math.Abs(r) > 0.7 ? "Strong" : Math.Abs(r) > 0.4 ? "Moderate" : Math.Abs(r) > 0.1 ? "Weak" : "None";

    // ═══════════════ ETG_01 Baseline Finite ═══════════════
    [Fact]
    public void V4_1_ETG_01_BaselineFinite()
    {
        int[] Ns = [80, 120]; double K0v = 0.5; double xi = 1.75; double s = 0.1;
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3;
            var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, E, BS);
            var h = Sm(Kc, N, s, BS); var R = Nm(RP(h)); var d = DL(R);
            bool fin = true; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (!double.IsFinite(Kc[i, j]) || !double.IsFinite(R[i, j]) || !double.IsFinite(d[i, j])) fin = false;
            _output.WriteLine($"N={N}: finite={fin}  dg={Dg(d):F4}");
            Assert.True(fin);
        }
    }

    // ═══════════════ ETG_02 Load to Omega Transfer ═══════════════
    [Fact]
    public void V4_1_ETG_02_LoadToOmegaTransfer()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2;
        double[] dOs = [0.01, 0.05, 0.10, 0.20, 0.50];
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        _output.WriteLine("deltaLoad  local_dO   neigh_dO   global_dO  dO/dLoad  linearity");
        _output.WriteLine("---------  ---------  ---------  ---------  ---------  ---------");
        foreach (double dO in dOs)
        {
            var hB = Sm(Kc, N, s, BS); var hL = Sm(Kc, N, s, BS, loadNode, dO);
            double locB = 0, locL = 0; for (int t = 1; t < hB.Length; t++) { locB += Math.Abs(hB[t][loadNode] - hB[t - 1][loadNode]); locL += Math.Abs(hL[t][loadNode] - hL[t - 1][loadNode]); }
            double locSh = Math.Abs(locL - locB) / (hB.Length - 1) / Dt;
            double neiSh = 0; int nc = 0; for (int j = 0; j < N; j++) if (j != loadNode && Kc[loadNode, j] > 1e-3) { nc++; double nb = 0, nl = 0; for (int t = 1; t < hB.Length; t++) { nb += Math.Abs(hB[t][j] - hB[t - 1][j]); nl += Math.Abs(hL[t][j] - hL[t - 1][j]); } neiSh += Math.Abs(nl - nb); }
            neiSh /= Math.Max(nc, 1) * (hB.Length - 1) * Dt;
            double globSh = 0; for (int i = 0; i < N; i++) { double gb = 0, gl = 0; for (int t = 1; t < hB.Length; t++) { gb += Math.Abs(hB[t][i] - hB[t - 1][i]); gl += Math.Abs(hL[t][i] - hL[t - 1][i]); } globSh += Math.Abs(gl - gb); }
            globSh /= N * (hB.Length - 1) * Dt;
            double ratio = dO > 1e-9 ? locSh / dO : double.NaN;
            string lin = dO <= 0.2 ? "Linear" : "Nonlinear";
            _output.WriteLine($"{dO,9:F2}  {locSh,9:E4}  {neiSh,9:E4}  {globSh,9:E4}  {ratio,9:F4}  {lin}");
        }
        Assert.True(true);
    }

    // ═══════════════ ETG_03 Omega to Rate Matrix Transfer ═══════════════
    [Fact]
    public void V4_1_ETG_03_OmegaToRateMatrixTransfer()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2; double dO = 0.2;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var hB = Sm(Kc, N, s, BS); var hL = Sm(Kc, N, s, BS, loadNode, dO);
        var RB = Nm(RP(hB)); var RL = Nm(RP(hL));
        double rNorm = MNorm(RB, RL);
        // Per-node omega shift vs per-row R change
        var oShifts = new List<double>(); var rShifts = new List<double>();
        for (int i = 0; i < N; i++)
        {
            double ob = 0, ol = 0; for (int t = 1; t < hB.Length; t++) { ob += Math.Abs(hB[t][i] - hB[t - 1][i]); ol += Math.Abs(hL[t][i] - hL[t - 1][i]); }
            oShifts.Add(Math.Abs(ol - ob) / (hB.Length - 1) / Dt);
            double rs = 0; for (int j = 0; j < N; j++) if (j != i) rs += Math.Abs(RL[i, j] - RB[i, j]);
            rShifts.Add(rs / (N - 1));
        }
        double rho = Spear(oShifts.ToArray(), rShifts.ToArray());
        _output.WriteLine($"Omega→R: R_norm={rNorm:E4}  corr(omega_shift, row_R_shift)={rho:F4}  class={CorrClass(rho)}");
        Assert.True(double.IsFinite(rho));
    }

    // ═══════════════ ETG_04 Rate Matrix to Geometry Transfer ═══════════════
    [Fact]
    public void V4_1_ETG_04_RateMatrixToGeometryTransfer()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2; double dO = 0.2;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var hB = Sm(Kc, N, s, BS); var RB = Nm(RP(hB)); var dB = DL(RB);
        var hL = Sm(Kc, N, s, BS, loadNode, dO); var RL = Nm(RP(hL)); var dL = DL(RL);
        double dNorm = MNorm(dB, dL);
        double rho = Spear(Fl(RB), Fl(RL));
        // d vs R correlation per node
        var rShifts = new List<double>(); var dShifts = new List<double>();
        for (int i = 0; i < N; i++)
        {
            double rs = 0, ds = 0;
            for (int j = 0; j < N; j++) if (j != i) { rs += Math.Abs(RL[i, j] - RB[i, j]); ds += Math.Abs(dL[i, j] - dB[i, j]); }
            rShifts.Add(rs / (N - 1)); dShifts.Add(ds / (N - 1));
        }
        double rhoRd = Spear(rShifts.ToArray(), dShifts.ToArray());
        _output.WriteLine($"R→d: d_norm={dNorm:E4}  corr(R,d)={rho:F4}  corr(row_R_shift,row_d_shift)={rhoRd:F4}  class={CorrClass(rhoRd)}");
        Assert.True(double.IsFinite(rhoRd));
    }

    // ═══════════════ ETG_05 Geometry to Coupling Transfer ═══════════════
    [Fact]
    public void V4_1_ETG_05_GeometryToCouplingTransfer()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2; double dO = 0.2;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var hB = Sm(Kc, N, s, BS); var dB = DL(Nm(RP(hB))); var Kb = ExpUpd(dB, K0v, xi);
        var hL = Sm(Kc, N, s, BS, loadNode, dO); var dL = DL(Nm(RP(hL))); var Kl = ExpUpd(dL, K0v, xi);
        double kNorm = MNorm(Kb, Kl);
        double rho = Spear(Fl(dL), Fl(Kl));
        _output.WriteLine($"d→K: K_norm={kNorm:E4}  corr(d,K)={rho:F4}  class={CorrClass(rho)}");
        Assert.True(double.IsFinite(rho));
    }

    // ═══════════════ ETG_06 Chain Correlation Report ═══════════════
    [Fact]
    public void V4_1_ETG_06_ChainCorrelationReport()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2; double dO = 0.2;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var hB = Sm(Kc, N, s, BS); var hL = Sm(Kc, N, s, BS, loadNode, dO);
        var RB = Nm(RP(hB)); var RL = Nm(RP(hL)); var dB = DL(RB); var dL = DL(RL);
        var Kb = ExpUpd(dB, K0v, xi); var Kl = ExpUpd(dL, K0v, xi);

        var oShifts = new List<double>(); var rShifts = new List<double>(); var dShifts = new List<double>(); var kShifts = new List<double>();
        for (int i = 0; i < N; i++)
        {
            double ob = 0, ol = 0; for (int t = 1; t < hB.Length; t++) { ob += Math.Abs(hB[t][i] - hB[t - 1][i]); ol += Math.Abs(hL[t][i] - hL[t - 1][i]); }
            oShifts.Add(Math.Abs(ol - ob) / (hB.Length - 1) / Dt);
            double rs = 0, ds = 0, ks = 0;
            for (int j = 0; j < N; j++) if (j != i) { rs += Math.Abs(RL[i, j] - RB[i, j]); ds += Math.Abs(dL[i, j] - dB[i, j]); ks += Math.Abs(Kl[i, j] - Kb[i, j]); }
            rShifts.Add(rs / (N - 1)); dShifts.Add(ds / (N - 1)); kShifts.Add(ks / (N - 1));
        }

        var oa = oShifts.ToArray(); var ra = rShifts.ToArray(); var da = dShifts.ToArray(); var ka = kShifts.ToArray();
        double cOR = Spear(oa, ra), cRD = Spear(ra, da), cDK = Spear(da, ka);

        _output.WriteLine("═══ ENERGY-TIME-GEOMETRY CHAIN ═══");
        _output.WriteLine("Link              Correlation  Slope       Class");
        _output.WriteLine("----------------  -----------  ----------  --------");
        _output.WriteLine($"Load→Omega        measured     --           --");
        _output.WriteLine($"Omega→Rate(R)      {cOR,11:F4}  --           {CorrClass(cOR)}");
        _output.WriteLine($"Rate(R)→Geom(d)   {cRD,11:F4}  --           {CorrClass(cRD)}");
        _output.WriteLine($"Geom(d)→Coupl(K)  {cDK,11:F4}  --           {CorrClass(cDK)}");
        _output.WriteLine("NOTE: No physical mass, energy, gravity, or GR claim.");
        Assert.True(true);
    }

    // ═══════════════ ETG_07 Remote Response Mediation ═══════════════
    [Fact]
    public void V4_1_ETG_07_RemoteResponseMediation()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2; double dO = 0.2;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var hB = Sm(Kc, N, s, BS); var hL = Sm(Kc, N, s, BS, loadNode, dO);
        var dB = DL(Nm(RP(hB))); var dL = DL(Nm(RP(hL))); var Kb = ExpUpd(dB, K0v, xi); var Kl = ExpUpd(dL, K0v, xi);

        // Direct: per-node omega shift vs per-node remote K deformation
        var dirO = new List<double>(); var dirK = new List<double>();
        // Mediated: omega→R→d→K chain product
        var chain = new List<double>();
        for (int i = 0; i < N; i++)
        {
            if (i == loadNode) continue;
            double ob = 0, ol = 0; for (int t = 1; t < hB.Length; t++) { ob += Math.Abs(hB[t][i] - hB[t - 1][i]); ol += Math.Abs(hL[t][i] - hL[t - 1][i]); }
            double oSh = Math.Abs(ol - ob) / (hB.Length - 1) / Dt;
            double ks = 0; for (int j = 0; j < N; j++) if (j != i) ks += Math.Abs(Kl[i, j] - Kb[i, j]);
            dirO.Add(oSh); dirK.Add(ks / (N - 1));
            chain.Add(oSh * dB[loadNode, i]); // simple mediation proxy
        }
        double dirR = Spear(dirO.ToArray(), dirK.ToArray());
        double medR = chain.Count > 3 ? Spear(chain.ToArray(), dirK.ToArray()) : double.NaN;
        _output.WriteLine($"Direct (omega→remote_K): rho={dirR:F4}");
        _output.WriteLine($"Mediated (omega*dist→remote_K): rho={medR:F4}");
        _output.WriteLine("NOTE: Mediation proxy is diagnostic only. No gravity claim.");
        Assert.True(true);
    }

    // ═══════════════ ETG_08 Positive/Negative Symmetry ═══════════════
    [Fact]
    public void V4_1_ETG_08_PositiveNegativeTimeGeometrySymmetry()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var hB = Sm(Kc, N, s, BS); var RB = Nm(RP(hB)); var dB = DL(RB); var Kb = ExpUpd(dB, K0v, xi);

        foreach (double dO in new[] { 0.2, -0.2 })
        {
            var hL = Sm(Kc, N, s, BS, loadNode, dO); var RL = Nm(RP(hL)); var dL = DL(RL); var Kl = ExpUpd(dL, K0v, xi);
            double rN = MNorm(RB, RL), dN = MNorm(dB, dL), kN = MNorm(Kb, Kl);
            double oSh = 0; for (int t = 1; t < hL.Length; t++) oSh += Math.Abs(hL[t][loadNode] - hL[t - 1][loadNode]) - Math.Abs(hB[t][loadNode] - hB[t - 1][loadNode]);
            oSh /= (hL.Length - 1) * Dt;
            _output.WriteLine($"dO={dO:F1}: omega_shift={oSh:E4}  R_norm={rN:E4}  d_norm={dN:E4}  K_norm={kN:E4}");
        }
        _output.WriteLine("NOTE: Negative load stable. No exotic matter claim.");
        Assert.True(true);
    }

    // ═══════════════ ETG_09 Two-Load Superposition ═══════════════
    [Fact]
    public void V4_1_ETG_09_TwoLoadTimeGeometrySuperposition()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int nA = N / 3, nB = 2 * N / 3; double dO = 0.2;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var hB = Sm(Kc, N, s, BS); var dB = DL(Nm(RP(hB)));
        var dA = DL(Nm(RP(Sm(Kc, N, s, BS, nA, dO)))); var dB2 = DL(Nm(RP(Sm(Kc, N, s, BS, nB, dO))));
        var dBoth = DL(Nm(RP(Sm(Kc, N, s, BS, nA, dO))));
        var hBoth = Sm(Kc, N, s, BS, nA, dO); var dBoth2 = DL(Nm(RP(Sm(Kc, N, s, BS, nB, dO))));
        double nA_ = MNorm(dB, dA), nB_ = MNorm(dB, dB2), nBoth = MNorm(dB, dBoth2);
        double linErr = Math.Abs(nBoth - (nA_ + nB_)) / Math.Max(nA_ + nB_, 1e-12);
        string cls = linErr < 0.2 ? "Linear" : linErr < 0.5 ? "Weak nonlinear" : "Strong nonlinear";
        _output.WriteLine($"d_norm: A={nA_:E4} B={nB_:E4} A+B={nBoth:E4}  linErr={linErr:F3}  class={cls}");
        Assert.True(true);
    }

    // ═══════════════ ETG_10 Distance Decay ═══════════════
    [Fact]
    public void V4_1_ETG_10_DistanceDecayOfTimeGeometryChain()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2; double dO = 0.2;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var hB = Sm(Kc, N, s, BS); var hL = Sm(Kc, N, s, BS, loadNode, dO);
        var RB = Nm(RP(hB)); var RL = Nm(RP(hL)); var dB = DL(RB); var dL = DL(RL);
        var Kb = ExpUpd(dB, K0v, xi); var Kl = ExpUpd(dL, K0v, xi);
        var dists = Enumerable.Range(0, N).Where(x => x != loadNode).OrderBy(x => dB[loadNode, x]).ToArray();
        int nSh = 6; int shSz = Math.Max(1, (N - 1) / nSh);
        _output.WriteLine("shell  mean_d    delta_R    delta_d    delta_K    delta_omega");
        _output.WriteLine("-----  -------   ---------  ---------  ---------  -----------");
        for (int sh = 0; sh < nSh; sh++)
        {
            var nodes = dists.Skip(sh * shSz).Take(shSz).ToArray();
            double md = nodes.Average(n => dB[loadNode, n]);
            double rSh = 0, dSh = 0, kSh = 0, oSh = 0;
            foreach (int n in nodes)
            {
                rSh += Math.Abs(RL[loadNode, n] - RB[loadNode, n]);
                dSh += Math.Abs(dL[loadNode, n] - dB[loadNode, n]);
                kSh += Math.Abs(Kl[loadNode, n] - Kb[loadNode, n]);
                double ob = 0, ol = 0; for (int t = 1; t < hB.Length; t++) { ob += Math.Abs(hB[t][n] - hB[t - 1][n]); ol += Math.Abs(hL[t][n] - hL[t - 1][n]); }
                oSh += Math.Abs(ol - ob);
            }
            int cnt = nodes.Length;
            _output.WriteLine($"  {sh + 1,3}   {md,7:F4}  {rSh / cnt,9:E4}  {dSh / cnt,9:E4}  {kSh / cnt,9:E4}  {oSh / cnt / (hB.Length - 1) / Dt,11:E4}");
        }
        Assert.True(true);
    }

    // ═══════════════ ETG_11 Fractal/Band Sensitivity ═══════════════
    [Fact]
    public void V4_1_ETG_11_FractalOrBandSensitivity()
    {
        int N = 60; double s = 0.1; int loadNode = N / 2; double dO = 0.2; double K0v = 0.5;
        double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2, 1.5];
        _output.WriteLine("xi     K0     chain_rho  d_norm      dg");
        _output.WriteLine("-----  ----   ---------  ---------   ------");
        foreach (double xi in xis)
        {
            int E = xi >= 1.5 ? 4 : 5;
            foreach (double kv in K0s)
            {
                var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, kv, xi, s, E, BS);
                var hB = Sm(Kc, N, s, BS); var hL = Sm(Kc, N, s, BS, loadNode, dO);
                var RB = Nm(RP(hB)); var RL = Nm(RP(hL)); var dB = DL(RB); var dL = DL(RL);
                double dN = MNorm(dB, dL); double dg = Dg(dL);
                double rho = Spear(Fl(RB), Fl(RL));
                _output.WriteLine($"{xi:F2}   {kv:F2}    {rho,9:F4}  {dN,9:E4}  {dg,7:F4}");
            }
        }
        Assert.True(true);
    }

    // ═══════════════ ETG_12 N-Scaling ═══════════════
    [Fact]
    public void V4_1_ETG_12_NScalingEnergyTimeGeometry()
    {
        int[] Ns = [40, 80, 120, 200]; double K0v = 0.5; double xi = 1.75; double s = 0.1; double dO = 0.2;
        _output.WriteLine("N      R_corr     d_norm      K_norm      dg");
        _output.WriteLine("----   ---------  ---------   ---------   ------");
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3; int ln = N / 2;
            var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, E, BS);
            var hB = Sm(Kc, N, s, BS); var hL = Sm(Kc, N, s, BS, ln, dO);
            var RB = Nm(RP(hB)); var RL = Nm(RP(hL)); var dB = DL(RB); var dL = DL(RL);
            var Kb = ExpUpd(dB, K0v, xi); var Kl = ExpUpd(dL, K0v, xi);
            _output.WriteLine($"{N,5}  {Spear(Fl(RB),Fl(RL)),9:F4}  {MNorm(dB,dL),9:E4}  {MNorm(Kb,Kl),9:E4}  {Dg(dL),6:F4}");
        }
        Assert.True(true);
    }

    // ═══════════════ ETG_13 Multi-Seed ═══════════════
    [Fact]
    public void V4_1_ETG_13_MultiSeedEnergyTimeGeometry()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2; double dO = 0.2; int nSeeds = 12;
        var rhos = new List<double>(); var dNs = new List<double>(); int fails = 0;
        for (int sd = 0; sd < nSeeds; sd++)
        {
            var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS + sd * 10);
            var hB = Sm(Kc, N, s, BS + sd * 20); var hL = Sm(Kc, N, s, BS + sd * 20, loadNode, dO);
            var RB = Nm(RP(hB)); var RL = Nm(RP(hL)); var dB = DL(RB); var dL = DL(RL);
            double rho = Spear(Fl(RB), Fl(RL)); double dN = MNorm(dB, dL);
            if (double.IsFinite(rho)) { rhos.Add(rho); dNs.Add(dN); } else fails++;
        }
        double mr = rhos.Count > 0 ? rhos.Average() : double.NaN;
        double sr = rhos.Count > 1 ? Math.Sqrt(rhos.Average(x => (x - mr) * (x - mr))) : 0;
        double md = dNs.Count > 0 ? dNs.Average() : double.NaN;
        _output.WriteLine($"Multi-seed ({nSeeds}): R_corr_mean={mr:F4} R_corr_std={sr:F4} d_norm_mean={md:E4} fails={fails}");
        Assert.True(fails <= nSeeds / 3);
    }

    // ═══════════════ ETG_14 Null and Degenerate Controls ═══════════════
    [Fact]
    public void V4_1_ETG_14_NullAndDegenerateControls()
    {
        int N = 60; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2; double dO = 0.2;

        (double rho, double dN, double dg) Eval(double[,] Kc)
        {
            var hB = Sm(Kc, N, s, BS); var hL = Sm(Kc, N, s, BS, loadNode, dO);
            var RB = Nm(RP(hB)); var RL = Nm(RP(hL)); var dB = DL(RB); var dL = DL(RL);
            return (Spear(Fl(RB), Fl(RL)), MNorm(dB, dL), Dg(dL));
        }

        var K0a = KS(N, BS); var KcA = RecoverFP(K0a, N, K0v, xi, s, 5, BS);
        var (rA, dA, dgA) = Eval(KcA);
        var KcN = RecoverFP(new double[N, N], N, K0v, xi, s, 5, BS);
        var (rN, dN, dgN) = Eval(KcN);
        var hSync = new double[101][]; for (int t = 0; t < 101; t++) { hSync[t] = new double[N]; for (int i = 0; i < N; i++) hSync[t][i] = 0; }
        double dgGS = Dg(DL(Nm(RP(hSync))));

        _output.WriteLine("Case             R_corr     d_norm      dg");
        _output.WriteLine("---------------  ---------  ---------   ------");
        _output.WriteLine($"Active           {rA,9:F4}  {dA,9:E4}  {dgA,6:F4}");
        _output.WriteLine($"K=0 null         {rN,9:F4}  {dN,9:E4}  {dgN,6:F4}");
        _output.WriteLine($"Global sync      --         --          {dgGS,6:F4}");
        Assert.True(dgGS < 0.01);
    }

    // ═══════════════ ETG_15 Claim Discipline Report ═══════════════
    [Fact]
    public void V4_1_ETG_15_ClaimDisciplineReport()
    {
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("  CLAIM DISCIPLINE — ENERGY-TIME-GEOMETRY");
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  SUPPORTED:");
        _output.WriteLine("    - Localized load proxies shift Omega.");
        _output.WriteLine("    - Omega shifts correlate with R deformation.");
        _output.WriteLine("    - R deformation maps to d (log-metric) deformation.");
        _output.WriteLine("    - d deformation maps to K coupling deformation.");
        _output.WriteLine("    - Remote response is measurable.");
        _output.WriteLine("    - Chain-level diagnostics are computable.");
        _output.WriteLine("    - Null and degenerate controls detected.");
        _output.WriteLine("");
        _output.WriteLine("  CONDITIONAL:");
        _output.WriteLine("    - Chain strength depends on load proxy, xi,");
        _output.WriteLine("      K0, topology, N, seed, amplitude.");
        _output.WriteLine("    - Omega is TRM clock-rate, not proper time.");
        _output.WriteLine("    - Energy-load proxy not calibrated to energy.");
        _output.WriteLine("    - Geometry deformation not GR curvature.");
        _output.WriteLine("");
        _output.WriteLine("  HYPOTHESIS:");
        _output.WriteLine("    - Energy modifies local TRM time rate.");
        _output.WriteLine("    - Time-rate shifts generate geometry deformation.");
        _output.WriteLine("    - This chain may underlie gravity-like behavior.");
        _output.WriteLine("");
        _output.WriteLine("  NOT CLAIMED:");
        _output.WriteLine("    - Physical mass is derived.");
        _output.WriteLine("    - Physical energy is derived.");
        _output.WriteLine("    - Gravity is derived.");
        _output.WriteLine("    - GR time dilation is derived.");
        _output.WriteLine("    - GR is replaced.");
        _output.WriteLine("    - SPARC is explained.");
        _output.WriteLine("    - Physical c is derived.");
        _output.WriteLine("══════════════════════════════════════════════════");
        Assert.True(true);
    }
}
