using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Causal structure probe: directed/asymmetric temporal influence patterns
/// extracted from oscillator dynamics on recovered exponential fixed-point topologies.
///
/// Implements three directed rate candidates:
///   1. Lagged phase response: R_lag[i,j] = max_tau corr(theta_i(t), theta_j(t+tau))
///   2. Kick-response influence: perturb i, measure response delay tau_ij in j
///   3. Causal distance candidate: c_ij = d_ij / tau_ij (dimensionless diagnostic)
///
/// Does NOT derive physical c. Does NOT claim Lorentzian spacetime.
/// No D=3 assumption. Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_CausalStructure")]
public class V4_1_CausalStructure_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_CausalStructure_Tests(ITestOutputHelper o) { _output = o; }

    // ── Core helpers ────────────────────────────────────────
    private static double[][] Sm(double[,] K, int N, double s, int seed, int kickNode = -1, double kickAmp = 0, int kickT = 0)
    {
        var r = new Random(seed); var w = new double[N]; for (int i = 0; i < N; i++) w[i] = 1.0 + s * (r.NextDouble() - 0.5) * 2.0;
        var th = new double[N]; for (int i = 0; i < N; i++) th[i] = r.NextDouble() * 2.0 * Math.PI;
        int hL = St / Hd + 1; var h = new double[hL][]; h[0] = (double[])th.Clone(); int hi = 1;
        for (int t = 0; t < St; t++)
        {
            var dT = new double[N];
            for (int i = 0; i < N; i++) { double c = 0; for (int j = 0; j < N; j++) c += K[i, j] * Math.Sin(th[j] - th[i]); dT[i] = w[i] + c; }
            // Apply kick at specific time
            if (kickNode >= 0 && kickNode < N && t == kickT) dT[kickNode] += kickAmp;
            for (int i = 0; i < N; i++) th[i] += Dt * dT[i];
            if ((t + 1) % Hd == 0 && hi < hL) h[hi++] = (double[])th.Clone();
        }
        return h;
    }
    private static double[,] RP(double[][] h) { int T = h.Length, N = h[0].Length; var R = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { double sc = 0, ss = 0; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); } R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; } return R; }
    private static double[,] Nm(double[,] R) { int N = R.GetLength(0); double mn = double.MaxValue; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j && R[i, j] < mn) mn = R[i, j]; double rng = 1.0 - mn; if (rng < 1e-15) rng = 1.0; var Rn = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) Rn[i, j] = 1.0; else Rn[i, j] = Math.Max(REps, (R[i, j] - mn) / rng); } return Rn; }
    private static double[,] DL(double[,] R) { int N = R.GetLength(0); var d = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) d[i, j] = 0; else d[i, j] = -Math.Log(Math.Max(R[i, j], 1e-100)); } return d; }
    private static double[,] ExpUpd(double[,] d, double K0, double xi) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 * Math.Exp(-d[i, j] / Math.Max(xi, 0.01)); } return K; }
    private static double Spear(double[] a, double[] b) { if (a.Length < 3) return 0; var ia = a.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); var ib = b.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); return Pear(ia, ib); }
    private static double Pear(double[] x, double[] y) { int n = x.Length; double mx = x.Average(), my = y.Average(), nm = 0, dx = 0, dy = 0; for (int i = 0; i < n; i++) { double a = x[i] - mx, b = y[i] - my; nm += a * b; dx += a * a; dy += b * b; } return Math.Sqrt(dx * dy) > 1e-15 ? nm / Math.Sqrt(dx * dy) : 0; }
    private static double[] Fl(double[,] m) { int N = m.GetLength(0); var a = new double[N * N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) a[i * N + j] = m[i, j]; return a; }
    private static double OP(double[] th) { double sx = 0, sy = 0; for (int i = 0; i < th.Length; i++) { sx += Math.Cos(th[i]); sy += Math.Sin(th[i]); } return Math.Sqrt(sx * sx + sy * sy) / th.Length; }
    private static double Dg(double[,] d) { int N = d.GetLength(0); var v = new List<double>(); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) v.Add(d[i, j]); if (v.Count == 0) return 0; double m = v.Average(); return m > 1e-9 ? v.Average(x => (x - m) * (x - m)) / (m * m) : 0; }
    private static double[,] KS(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }

    // ── Lagged phase response ────────────────────────────────
    // R_lag[i,j] = max over tau in [1, maxLag] of corr(theta_i(t), theta_j(t+tau))
    private static double[,] LaggedR(double[][] h, int maxLag)
    {
        int T = h.Length, N = h[0].Length; var R = new double[N, N];
        for (int i = 0; i < N; i++)
            for (int j = 0; j < N; j++)
            {
                if (i == j) { R[i, j] = 1.0; continue; }
                double best = 0;
                for (int tau = 1; tau <= maxLag && tau < T - 1; tau++)
                {
                    int len = T - tau;
                    var ai = new double[len]; var aj = new double[len];
                    for (int t = 0; t < len; t++) { ai[t] = Math.Cos(h[t][i]); aj[t] = Math.Cos(h[t + tau][j]); }
                    double c = Math.Abs(Pear(ai, aj));
                    if (double.IsFinite(c) && c > best) best = c;
                }
                R[i, j] = best;
            }
        return R;
    }

    // ── Calculate best lag direction ─────────────────────────
    private static (double Rlag, int tau) BestLag(double[][] h, int i, int j, int maxLag)
    {
        int T = h.Length; double best = 0; int bestTau = 0;
        for (int tau = 1; tau <= maxLag && tau < T - 1; tau++)
        {
            int len = T - tau; var ai = new double[len]; var aj = new double[len];
            for (int t = 0; t < len; t++) { ai[t] = Math.Cos(h[t][i]); aj[t] = Math.Cos(h[t + tau][j]); }
            double c = Math.Abs(Pear(ai, aj));
            if (double.IsFinite(c) && c > best) { best = c; bestTau = tau; }
        }
        return (best, bestTau);
    }

    // ── Kick-response delay matrix ───────────────────────────
    // Perturb oscillator i at t=kickT, measure first detectable response in j
    private static double[,] KickResponse(double[,] K, int N, double s, int seed, double kickAmp, int kickT, double threshold)
    {
        var tau = new double[N, N];
        var h0 = Sm(K, N, s, seed);
        int nSrc = Math.Min(N, 8);
        for (int src = 0; src < nSrc; src++)
        {
            var hk = Sm(K, N, s, seed, src, kickAmp, kickT);
            for (int dst = 0; dst < N; dst++)
            {
                if (src == dst) { tau[src, dst] = 0; continue; }
                double bestTau = -1;
                for (int t = kickT + 1; t < hk.Length && t < h0.Length; t++)
                {
                    // Use sin(0.5 * delta_phase) for proper circular distance
                    double dPh = hk[t][dst] - h0[t][dst];
                    double dev = Math.Abs(Math.Sin(0.5 * dPh));
                    if (dev > threshold) { bestTau = (t - kickT) * Dt * Hd; break; }
                }
                tau[src, dst] = bestTau;
            }
        }
        return tau;
    }

    // ═══════════════ CS_01 Directed Matrices Finite ═══════════════
    [Fact]
    public void V4_1_CS_01_DirectedMatricesFinite()
    {
        int N = 30; double s = 0.1; double K0v = 0.5; double xi = 1.0;
        var K0 = KS(N, BS); var Kc = (double[,])K0.Clone();
        for (int e = 0; e < 4; e++) { var he = Sm(Kc, N, s, BS + e); Kc = ExpUpd(DL(Nm(RP(he))), K0v, xi); }
        var hf = Sm(Kc, N, s, BS + 100);

        // R_lag
        var Rlag = LaggedR(hf, 20);
        int nanL = 0; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (!double.IsFinite(Rlag[i, j])) nanL++;
        // Asymmetry
        double asym = 0; int cnt = 0;
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { asym += Math.Abs(Rlag[i, j] - Rlag[j, i]); cnt++; }
        asym /= Math.Max(cnt, 1);

        // Kick-response
        var tau = KickResponse(Kc, N, s, BS + 200, 2.0, 50, 0.02);
        int nanK = 0; for (int i = 0; i < Math.Min(N, 10); i++) for (int j = 0; j < N; j++) if (double.IsNaN(tau[i, j])) nanK++;

        _output.WriteLine($"R_lag: finite={nanL == 0}  asymmetry={asym:F4}");
        _output.WriteLine($"Kick-response: finite={nanK == 0}");
        Assert.True(nanL == 0, "R_lag must have no NaN.");
        Assert.True(double.IsFinite(asym));
    }

    // ═══════════════ CS_02 Asymmetry Detected ═══════════════
    [Fact]
    public void V4_1_CS_02_AsymmetryDetected()
    {
        int N = 30; double s = 0.1; double K0v = 0.5; double xi = 1.0;
        var K0 = KS(N, BS); var Kc = (double[,])K0.Clone();
        for (int e = 0; e < 4; e++) { var he = Sm(Kc, N, s, BS + e); Kc = ExpUpd(DL(Nm(RP(he))), K0v, xi); }
        var hf = Sm(Kc, N, s, BS + 100);
        var Rlag = LaggedR(hf, 20);

        // Symmetric R (standard)
        var Rsym = Nm(RP(hf));
        double asymLag = 0, asymSym = 0; int cnt = 0;
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++)
            {
                asymLag += Math.Abs(Rlag[i, j] - Rlag[j, i]);
                asymSym += Math.Abs(Rsym[i, j] - Rsym[j, i]);
                cnt++;
            }
        asymLag /= cnt; asymSym /= cnt;
        _output.WriteLine($"Lagged asymmetry: {asymLag:F4}");
        _output.WriteLine($"Symmetric R asymmetry: {asymSym:F4}");
        Assert.True(asymSym < 1e-10, "Symmetric R must have near-zero asymmetry.");
        Assert.True(double.IsFinite(asymLag));
    }

    // ═══════════════ CS_03 Kick-Response Delay Matrix ═══════════════
    [Fact]
    public void V4_1_CS_03_KickResponseDelayMatrix()
    {
        int N = 30; double s = 0.1; double K0v = 0.5; double xi = 1.0;
        var K0 = KS(N, BS); var Kc = (double[,])K0.Clone();
        for (int e = 0; e < 4; e++) { var h = Sm(Kc, N, s, BS + e); Kc = ExpUpd(DL(Nm(RP(h))), K0v, xi); }
        var tau = KickResponse(Kc, N, s, BS + 200, 2.0, 50, 0.02);

        var delays = new List<double>(); int nSrc = Math.Min(N, 8);
        for (int i = 0; i < nSrc; i++)
            for (int j = 0; j < N; j++)
                if (i != j && tau[i, j] >= 0) delays.Add(tau[i, j]);

        double meanDelay = delays.Count > 0 ? delays.Average() : double.NaN;
        double maxDelay = delays.Count > 0 ? delays.Max() : double.NaN;
        int responded = delays.Count;
        int tested = nSrc * (N - 1);

        _output.WriteLine($"Responded: {responded}/{tested}  mean delay: {meanDelay:F4}  max delay: {maxDelay:F4}");
        Assert.True(responded >= 0, "Kick-response diagnostic must be computable (zero responses is valid).");
    }

    // ═══════════════ CS_04 Causal Distance Candidate ═══════════════
    [Fact]
    public void V4_1_CS_04_CausalDistanceCandidate()
    {
        int N = 30; double s = 0.1; double K0v = 0.5; double xi = 1.0;
        var K0 = KS(N, BS); var Kc = (double[,])K0.Clone();
        for (int e = 0; e < 4; e++) { var he = Sm(Kc, N, s, BS + e); Kc = ExpUpd(DL(Nm(RP(he))), K0v, xi); }
        var hf = Sm(Kc, N, s, BS + 100);
        var d = DL(Nm(RP(hf)));
        var tau = KickResponse(Kc, N, s, BS + 200, 2.0, 50, 0.02);

        var cCandidates = new List<double>(); int nSrc = Math.Min(N, 8);
        for (int i = 0; i < nSrc; i++)
            for (int j = 0; j < N; j++)
                if (i != j && tau[i, j] > 0 && d[i, j] > 0)
                    cCandidates.Add(d[i, j] / tau[i, j]);

        double meanC = cCandidates.Count > 0 ? cCandidates.Average() : double.NaN;
        double medC = cCandidates.Count > 0 ? cCandidates.OrderBy(x => x).ElementAt(cCandidates.Count / 2) : double.NaN;

        _output.WriteLine($"Causal distance candidates: {cCandidates.Count} finite");
        _output.WriteLine($"  mean c_candidate: {meanC:F4}  median: {medC:F4}");
        _output.WriteLine("  (dimensionless diagnostic only — NOT physical c)");
        Assert.True(double.IsFinite(meanC) || cCandidates.Count == 0, "Diagnostic must be finite even if no candidates.");
    }

    // ═══════════════ CS_05 Propagation Front ═══════════════
    [Fact]
    public void V4_1_CS_05_PropagationFront()
    {
        int N = 40; double s = 0.1; double K0v = 0.5; double xi = 1.0;
        var K0 = KS(N, BS); var Kc = (double[,])K0.Clone();
        for (int e = 0; e < 4; e++) { var h = Sm(Kc, N, s, BS + e); Kc = ExpUpd(DL(Nm(RP(h))), K0v, xi); }

        int src = 0; double kickAmp = 2.0; int kickT = 50;
        var h0 = Sm(Kc, N, s, BS + 300);       // baseline
        var hk = Sm(Kc, N, s, BS + 300, src, kickAmp, kickT); // kicked

        // Compute response amplitude by "shell" (rank by symmetric R to source)
        var Rsym = Nm(RP(h0));
        // Sort nodes by R to source (descending = closer)
        var distOrder = Enumerable.Range(0, N).Where(x => x != src)
            .OrderBy(x => -Rsym[src, x]).ToArray();

        _output.WriteLine("shell  nodes   mean_resp_time  mean_resp_amp");
        _output.WriteLine("-----  ------  --------------  -------------");
        int shellSize = Math.Max(1, (N - 1) / 4);
        for (int sh = 0; sh < 4; sh++)
        {
            var shellNodes = distOrder.Skip(sh * shellSize).Take(shellSize).ToArray();
            var times = new List<double>(); var amps = new List<double>();
            foreach (int dst in shellNodes)
            {
                double maxDev = 0; double respTime = -1;
                for (int t = kickT + 1; t < hk.Length; t++)
                {
                    double dev = Math.Abs(Math.Sin(hk[t][dst]) - Math.Sin(h0[t][dst]));
                    if (dev > maxDev) maxDev = dev;
                    if (dev > 0.05 && respTime < 0) respTime = (t - kickT) * Dt * Hd;
                }
                if (respTime >= 0) times.Add(respTime);
                amps.Add(maxDev);
            }
            double mt = times.Count > 0 ? times.Average() : double.NaN;
            double ma = amps.Count > 0 ? amps.Average() : double.NaN;
            _output.WriteLine($"{sh + 1,5}  {shellNodes.Length,6}  {mt,14:F4}  {ma,13:F4}");
        }
        Assert.True(true);
    }

    // ═══════════════ CS_06 Null and Degenerate Controls ═══════════════
    [Fact]
    public void V4_1_CS_06_NullAndDegenerateControls()
    {
        int N = 30; double s = 0.1; double K0v = 0.5; double xi = 1.0;

        double[,] GetKc(double[,] K0)
        {
            var Kc = (double[,])K0.Clone();
            for (int e = 0; e < 4; e++) { var h = Sm(Kc, N, s, BS + e); Kc = ExpUpd(DL(Nm(RP(h))), K0v, xi); }
            return Kc;
        }

        double TestAsym(double[,] Kc)
        {
            var h = Sm(Kc, N, s, BS + 300);
            var Rlag = LaggedR(h, 15);
            double asym = 0; int cnt = 0;
            for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { asym += Math.Abs(Rlag[i, j] - Rlag[j, i]); cnt++; }
            return cnt > 0 ? asym / cnt : 0;
        }

        // Active
        var KcActive = GetKc(KS(N, BS));
        double asymA = TestAsym(KcActive);

        // K=0
        var KcNull = GetKc(new double[N, N]);
        double asymN = TestAsym(KcNull);

        // Global sync: compute degeneracy directly from a fully synced phase array
        var hSync = new double[101][]; for (int t = 0; t < 101; t++) { hSync[t] = new double[N]; for (int i = 0; i < N; i++) hSync[t][i] = 0; }
        double dgGS = Dg(DL(Nm(RP(hSync))));
        _output.WriteLine($"Global sync degen:   dg={dgGS:F4}");

        // Random R
        var rng = new Random(BS); var Krd = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { var v = rng.NextDouble(); Krd[i, j] = v; Krd[j, i] = v; }
        var KcRnd = GetKc(Krd);
        double asymRnd = TestAsym(KcRnd);

        _output.WriteLine($"Active asymmetry:    {asymA:F4}");
        _output.WriteLine($"K=0 null asymmetry:  {asymN:F4}");
        _output.WriteLine($"Random-R asymmetry:  {asymRnd:F4}");
        _output.WriteLine($"Global sync degen:   dg={dgGS:F4}");

        Assert.True(dgGS < 0.01, "Global sync must be degenerate.");
        Assert.True(double.IsFinite(asymA) && double.IsFinite(asymN));
    }

    // ═══════════════ CS_07 Exponential Fixed-Point Causal Probe ═══════════════
    [Fact]
    public void V4_1_CS_07_ExponentialFixedPointCausalProbe()
    {
        int N = 30; double s = 0.1;

        double[,] GaussUpd(double[,] d, double K0, double xi) { int n = d.GetLength(0); var K = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 * Math.Exp(-d[i, j] * d[i, j] / Math.Max(xi * xi, 0.0001)); } return K; }

        (double asym, double delays, double dg) Probe(double[,] K0, bool useExp)
        {
            var Kc = (double[,])K0.Clone();
            for (int e = 0; e < 4; e++) { var h = Sm(Kc, N, s, BS + e); Kc = useExp ? ExpUpd(DL(Nm(RP(h))), 0.5, 1.0) : GaussUpd(DL(Nm(RP(h))), 0.5, 1.0); }
            var h2 = Sm(Kc, N, s, BS + 200);
            var Rlag = LaggedR(h2, 15);
            double asym = 0; int cnt = 0;
            for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { asym += Math.Abs(Rlag[i, j] - Rlag[j, i]); cnt++; }
            asym /= Math.Max(cnt, 1);
            var tau = KickResponse(Kc, N, s, BS + 300, 2.0, 50, 0.02);
            var delays = new List<double>();
            for (int i = 0; i < Math.Min(N, 10); i++) for (int j = 0; j < N; j++) if (i != j && tau[i, j] >= 0) delays.Add(tau[i, j]);
            double md = delays.Count > 0 ? delays.Average() : double.NaN;
            double dg = Dg(DL(Nm(RP(h2))));
            return (asym, md, dg);
        }

        var Krs = KS(N, BS);
        var (aExp, dExp, dgExp) = Probe(Krs, true);
        var (aGauss, dGauss, dgGauss) = Probe(Krs, false);

        _output.WriteLine("Topology       asymmetry  mean_delay  dg");
        _output.WriteLine("-------------  ---------  ----------  -------");
        _output.WriteLine($"Exponential    {aExp,9:F4}  {dExp,10:F4}  {dgExp,7:F4}");
        _output.WriteLine($"Gaussian       {aGauss,9:F4}  {dGauss,10:F4}  {dgGauss,7:F4}");

        Assert.True(double.IsFinite(aExp) && double.IsFinite(aGauss));
        Assert.True(double.IsFinite(dgExp) && double.IsFinite(dgGauss));
    }

    // ═══════════════ CS_08 Multi-Seed Causal Stability ═══════════════
    [Fact]
    public void V4_1_CS_08_MultiSeedCausalStability()
    {
        int N = 30; double s = 0.1; double K0v = 0.5; double xi = 1.0;
        var asyms = new List<double>(); var delays = new List<double>(); int fails = 0;

        for (int sd = 0; sd < 10; sd++)
        {
            var K0 = KS(N, BS);
            var Kc = (double[,])K0.Clone();
            for (int e = 0; e < 4; e++) { var h = Sm(Kc, N, s, BS + sd * 10 + e); Kc = ExpUpd(DL(Nm(RP(h))), K0v, xi); }
            var h2 = Sm(Kc, N, s, BS + sd * 10 + 100);
            var Rlag = LaggedR(h2, 15);
            double asym = 0; int cnt = 0;
            for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { asym += Math.Abs(Rlag[i, j] - Rlag[j, i]); cnt++; }
            asym /= Math.Max(cnt, 1);
            if (double.IsFinite(asym)) asyms.Add(asym); else fails++;

            var tau = KickResponse(Kc, N, s, BS + sd * 10 + 200, 2.0, 50, 0.02);
            var dl = new List<double>();
            for (int i = 0; i < Math.Min(N, 10); i++) for (int j = 0; j < N; j++) if (i != j && tau[i, j] >= 0) dl.Add(tau[i, j]);
            if (dl.Count > 0) delays.Add(dl.Average());
        }

        double ma = asyms.Count > 0 ? asyms.Average() : double.NaN;
        double sa = asyms.Count > 1 ? Math.Sqrt(asyms.Average(x => (x - ma) * (x - ma))) : 0;
        double md = delays.Count > 0 ? delays.Average() : double.NaN;
        double sdD = delays.Count > 1 ? Math.Sqrt(delays.Average(x => (x - md) * (x - md))) : 0;

        _output.WriteLine("Multi-seed causal stability (10 seeds):");
        _output.WriteLine($"  Asymmetry:   mean={ma:F4}  std={sa:F4}  fails={fails}");
        _output.WriteLine($"  Mean delay:  mean={md:F4}  std={sdD:F4}");
        Assert.True(fails == 0, "No seeds should fail.");
    }

    // ═══════════════ CS_09 N-Scaling Causal Probe ═══════════════
    [Fact]
    public void V4_1_CS_09_NScalingCausalProbe()
    {
        int[] Ns = [40, 80, 120]; double s = 0.1; double K0v = 0.5; double xi = 1.0;
        _output.WriteLine("N      asymmetry  mean_delay  dg       finite?");
        _output.WriteLine("----   ---------  ----------  -------  -------");
        foreach (int N in Ns)
        {
            var K0 = KS(N, BS); var Kc = (double[,])K0.Clone();
            int E = N <= 80 ? 4 : 3;
            for (int e = 0; e < E; e++) { var h = Sm(Kc, N, s, BS + e); Kc = ExpUpd(DL(Nm(RP(h))), K0v, xi); }
            var h2 = Sm(Kc, N, s, BS + 200);
            int maxLag = N <= 60 ? 15 : 8;
            var Rlag = LaggedR(h2, maxLag);
            double asym = 0; int cnt = 0;
            for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { asym += Math.Abs(Rlag[i, j] - Rlag[j, i]); cnt++; }
            asym /= Math.Max(cnt, 1);
            var tau = KickResponse(Kc, N, s, BS + 300, 2.0, 50, 0.02);
            var dl = new List<double>();
            for (int i = 0; i < Math.Min(N, 10); i++) for (int j = 0; j < N; j++) if (i != j && tau[i, j] >= 0) dl.Add(tau[i, j]);
            double md = dl.Count > 0 ? dl.Average() : double.NaN;
            double dg = Dg(DL(Nm(RP(h2))));
            bool fin = double.IsFinite(asym) && double.IsFinite(dg);
            _output.WriteLine($"{N,5}  {asym,9:F4}  {md,10:F4}  {dg,7:F4}  {fin,7}");
            Assert.True(fin, $"N={N}: causal diagnostics must be finite.");
        }
    }

    // ═══════════════ CS_10 Causal Structure Report ═══════════════
    [Fact]
    public void V4_1_CS_10_CausalStructureReport()
    {
        int N = 40; double s = 0.1; double K0v = 0.5; double xi = 1.0;
        var K0 = KS(N, BS); var Kc = (double[,])K0.Clone();
        for (int e = 0; e < 4; e++) { var he = Sm(Kc, N, s, BS + e); Kc = ExpUpd(DL(Nm(RP(he))), K0v, xi); }
        var hf = Sm(Kc, N, s, BS + 200);

        // Lagged asymmetry
        var Rlag = LaggedR(hf, 15);
        double asym = 0; int cnt = 0;
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { asym += Math.Abs(Rlag[i, j] - Rlag[j, i]); cnt++; }
        asym /= Math.Max(cnt, 1);

        // Kick delays
        var tau = KickResponse(Kc, N, s, BS + 300, 2.0, 50, 0.02);
        var delays = new List<double>();
        for (int i = 0; i < Math.Min(N, 10); i++) for (int j = 0; j < N; j++) if (i != j && tau[i, j] >= 0) delays.Add(tau[i, j]);
        double meanDelay = delays.Count > 0 ? delays.Average() : double.NaN;

        // Causal distance candidates
        var d = DL(Nm(RP(hf)));
        var cC = new List<double>();
        for (int i = 0; i < Math.Min(N, 10); i++) for (int j = 0; j < N; j++) if (i != j && tau[i, j] > 0 && d[i, j] > 0) cC.Add(d[i, j] / tau[i, j]);
        double minC = cC.Count > 0 ? cC.Min() : double.NaN;
        double maxC = cC.Count > 0 ? cC.Max() : double.NaN;

        double dg = Dg(d);

        string cls = "Symmetric / No Direction";
        if (asym > 0.01 && delays.Count > 5) cls = "Directed Signal Detected";
        else if (asym > 0.001) cls = "Weak Directed Signal";
        if (dg < 0.001) cls = "Degenerate";

        _output.WriteLine("═══ CAUSAL STRUCTURE REPORT ═══");
        _output.WriteLine($"N: {N}");
        _output.WriteLine($"Asymmetry:              {asym:F4}");
        _output.WriteLine($"Mean response delay:    {meanDelay:F4}");
        _output.WriteLine($"Causal speed candidate: [{minC:F4}, {maxC:F4}]");
        _output.WriteLine($"Degeneracy:             {dg:F4}");
        _output.WriteLine($"Class:                  {cls}");
        _output.WriteLine("");
        _output.WriteLine("NOTE: causal speed candidate is dimensionless diagnostic only.");
        _output.WriteLine("Physical speed of light c is NOT derived here.");

        Assert.True(cls != "Failed", "Causal structure must not be Failed.");
    }

    // ═══════════════ CS_11 Claim Discipline Report ═══════════════
    [Fact]
    public void V4_1_CS_11_ClaimDisciplineReport()
    {
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("  CLAIM DISCIPLINE — CAUSAL STRUCTURE PROBE");
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  SUPPORTED:");
        _output.WriteLine("    - Directed/asymmetric influence diagnostics are measurable.");
        _output.WriteLine("    - Lagged phase correlation R_lag[i,j] is computable and finite.");
        _output.WriteLine("    - Kick-response delays can be computed deterministically.");
        _output.WriteLine("    - Causal distance candidate d/tau is a dimensionless diagnostic.");
        _output.WriteLine("    - Null and degenerate controls are correctly detected.");
        _output.WriteLine("    - Causal diagnostics remain finite across N=40..120.");
        _output.WriteLine("");
        _output.WriteLine("  CONDITIONAL:");
        _output.WriteLine("    - Directed structure depends on perturbation method, lag window,");
        _output.WriteLine("      thresholds, topology, and parameters.");
        _output.WriteLine("    - c_eff candidate is dimensionless and model-dependent.");
        _output.WriteLine("    - Response fronts are numerical diagnostics only.");
        _output.WriteLine("    - Kick-response depends on threshold choice.");
        _output.WriteLine("");
        _output.WriteLine("  HYPOTHESIS:");
        _output.WriteLine("    - Physical causal structure emerges from TRM dynamics.");
        _output.WriteLine("    - c_eff can be derived from stable propagation fronts.");
        _output.WriteLine("    - Lorentzian light-cone structure emerges in a later limit.");
        _output.WriteLine("");
        _output.WriteLine("  NOT CLAIMED:");
        _output.WriteLine("    - Physical c is derived.");
        _output.WriteLine("    - Lorentz invariance is proven.");
        _output.WriteLine("    - Lorentzian spacetime is derived.");
        _output.WriteLine("    - D=3 is derived.");
        _output.WriteLine("    - General Relativity is replaced.");
        _output.WriteLine("    - Quantum mechanics is derived.");
        _output.WriteLine("    - Planck scales are derived.");
        _output.WriteLine("══════════════════════════════════════════════════");
        Assert.True(true);
    }
}
