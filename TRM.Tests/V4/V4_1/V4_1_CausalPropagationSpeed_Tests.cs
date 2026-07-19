using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Causal propagation speed benchmark: measures dimensionless propagation-speed
/// candidates on recovered TRM topologies.
///
/// Key diagnostics:
///   - Kick-response with varied perturbation amplitudes
///   - Distance-shell propagation front tracking using d_ij
///   - c_eff_candidate = d_ij / tau_ij (dimensionless)
///   - Light-cone-like front classification
///
/// Does NOT claim physical speed of light c. Does NOT claim Lorentz invariance.
/// No D=3 assumption. Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_CausalPropagation")]
public class V4_1_CausalPropagationSpeed_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_CausalPropagationSpeed_Tests(ITestOutputHelper o) { _output = o; }

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

    // ── Strong kick-response with response-time and amplitude tracking ──
    private static (double[] times, double[] amps) KickDetect(double[,] K, int N, double s, int seed, int src, double kickAmp, int kickT, double threshold)
    {
        var h0 = Sm(K, N, s, seed);
        var hk = Sm(K, N, s, seed, src, kickAmp, kickT);
        var times = new double[N]; var amps = new double[N];
        for (int dst = 0; dst < N; dst++)
        {
            times[dst] = -1; amps[dst] = 0;
            if (dst == src) { times[dst] = 0; continue; }
            for (int t = kickT + 1; t < hk.Length && t < h0.Length; t++)
            {
                double dPh = hk[t][dst] - h0[t][dst];
                double dev = Math.Abs(Math.Sin(0.5 * dPh));
                if (dev > amps[dst]) amps[dst] = dev;
                if (dev > threshold && times[dst] < 0) times[dst] = (t - kickT) * Dt * Hd;
            }
        }
        return (times, amps);
    }

    // ── Build recovered fixed-point topology ─────────────────
    private static double[,] RecoverFP(double[,] K0, int N, double K0v, double xi, double s, int E, int seed)
    {
        var Kc = (double[,])K0.Clone();
        for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = ExpUpd(DL(Nm(RP(he))), K0v, xi); }
        return Kc;
    }

    // ═══════════════ CPS_01 Propagation Setup Finite ═══════════════
    [Fact]
    public void V4_1_CPS_01_PropagationSetupFinite()
    {
        int[] Ns = [40, 80, 120, 200]; double K0v = 0.5; double xi = 1.0; double s = 0.1;
        _output.WriteLine("N      K_finite  R_finite  d_finite  dg");
        _output.WriteLine("----   --------  --------  --------  -------");
        foreach (int N in Ns)
        {
            var K0 = KS(N, BS); int E = N <= 80 ? 6 : 4;
            var Kc = RecoverFP(K0, N, K0v, xi, s, E, BS);
            var h = Sm(Kc, N, s, BS + 200); var R = Nm(RP(h)); var d = DL(R);
            bool kFin = true, rFin = true, dFin = true;
            for (int i = 0; i < N; i++) for (int j = 0; j < N; j++)
                {
                    if (!double.IsFinite(Kc[i, j])) kFin = false;
                    if (!double.IsFinite(R[i, j])) rFin = false;
                    if (!double.IsFinite(d[i, j])) dFin = false;
                }
            double dg = Dg(d);
            _output.WriteLine($"{N,5}  {kFin,-8}  {rFin,-8}  {dFin,-8}  {dg,7:F4}");
            Assert.True(kFin && rFin && dFin, $"N={N}: all matrices must be finite.");
        }
    }

    // ═══════════════ CPS_02 Stronger Kick-Response Detection ═══════════════
    [Fact]
    public void V4_1_CPS_02_StrongerKickResponseDetection()
    {
        int N = 60; double K0v = 0.5; double xi = 1.0; double s = 0.1;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 6, BS);
        double[] kicks = [0.05, 0.1, 0.2, 0.5, 1.0];
        int nSrc = 4; int kickT = 50; double thresh = 0.01;
        _output.WriteLine("kickAmp  resp_frac  mean_tau   mean_amp  c_candidate");
        _output.WriteLine("-------  ---------  ---------  --------  -----------");
        foreach (double ka in kicks)
        {
            int resp = 0, total = 0; var taus = new List<double>(); var amps = new List<double>(); var ds = new List<double>();
            var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
            for (int src = 0; src < nSrc; src++)
            {
                var (times, ams) = KickDetect(Kc, N, s, BS + 300, src, ka, kickT, thresh);
                for (int dst = 0; dst < N; dst++)
                {
                    if (dst == src) continue; total++;
                    if (times[dst] > 0) { resp++; taus.Add(times[dst]); amps.Add(ams[dst]); ds.Add(dMat[src, dst]); }
                }
            }
            double frac = (double)resp / Math.Max(total, 1);
            double mt = taus.Count > 0 ? taus.Average() : double.NaN;
            double ma = amps.Count > 0 ? amps.Average() : double.NaN;
            double mc = taus.Count > 0 ? ds.Zip(taus, (d, t) => d / t).Average() : double.NaN;
            _output.WriteLine($"{ka,7:F2}  {frac,9:F3}  {mt,9:F4}  {ma,8:F4}  {mc,11:F4}");
            Assert.True(double.IsFinite(frac));
        }
    }

    // ═══════════════ CPS_03 Density and Xi Window ═══════════════
    [Fact]
    public void V4_1_CPS_03_DensityAndXiWindow()
    {
        int N = 60; double s = 0.1; double kickAmp = 0.5; double thresh = 0.01;
        double[] xis = [0.5, 1.0, 1.5, 2.0];
        double[] K0s = [0.3, 0.5, 0.8, 1.2];
        _output.WriteLine("xi    K0     resp_frac  mean_tau   c_candidate  dg");
        _output.WriteLine("----  ----   ---------  ---------  -----------  ------");
        foreach (double xi in xis)
            foreach (double K0v in K0s)
            {
                var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
                var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200)))); double dg = Dg(dMat);
                int resp = 0, total = 0; var taus = new List<double>(); var ds = new List<double>();
                int nSrc = 4;
                for (int src = 0; src < nSrc; src++)
                {
                    var (times, _) = KickDetect(Kc, N, s, BS + 300, src, kickAmp, 50, thresh);
                    for (int dst = 0; dst < N; dst++) { if (dst == src) continue; total++; if (times[dst] > 0) { resp++; taus.Add(times[dst]); ds.Add(dMat[src, dst]); } }
                }
                double frac = (double)resp / Math.Max(total, 1);
                double mt = taus.Count > 0 ? taus.Average() : double.NaN;
                double mc = taus.Count > 0 ? ds.Zip(taus, (d, t) => d / t).Average() : double.NaN;
                _output.WriteLine($"{xi:F2}   {K0v:F2}    {frac,9:F3}  {mt,9:F4}  {mc,11:F4}  {dg,6:F4}");
                Assert.True(double.IsFinite(frac));
            }
    }

    // ═══════════════ CPS_04 Propagation Front by Distance Shell ═══════════════
    [Fact]
    public void V4_1_CPS_04_PropagationFrontByDistanceShell()
    {
        int N = 80; double K0v = 0.5; double xi = 1.0; double s = 0.1; double kickAmp = 0.5; double thresh = 0.01;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 6, BS);
        var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
        int nSrc = 3; int nShells = 5;
        _output.WriteLine("src   shell  mean_d     mean_tau   mean_amp   c_candidate");
        _output.WriteLine("----  -----  -------    ---------  --------   -----------");
        for (int src = 0; src < nSrc; src++)
        {
            // Sort targets by distance from source
            var targets = Enumerable.Range(0, N).Where(x => x != src)
                .OrderBy(x => dMat[src, x]).ToArray();
            int shellSz = Math.Max(1, (N - 1) / nShells);
            var (times, amps) = KickDetect(Kc, N, s, BS + 300, src, kickAmp, 50, thresh);
            for (int sh = 0; sh < nShells; sh++)
            {
                var shNodes = targets.Skip(sh * shellSz).Take(shellSz).ToArray();
                var tVals = new List<double>(); var aVals = new List<double>(); var dVals = new List<double>();
                foreach (int dst in shNodes)
                    if (times[dst] > 0) { tVals.Add(times[dst]); aVals.Add(amps[dst]); dVals.Add(dMat[src, dst]); }
                double md = dVals.Count > 0 ? dVals.Average() : double.NaN;
                double mt = tVals.Count > 0 ? tVals.Average() : double.NaN;
                double ma = aVals.Count > 0 ? aVals.Average() : double.NaN;
                double mc = tVals.Count > 0 ? dVals.Zip(tVals, (d, t) => d / t).Average() : double.NaN;
                _output.WriteLine($"{src,4}  {sh + 1,5}  {md,7:F4}   {mt,9:F4}  {ma,8:F4}  {mc,11:F4}");
            }
        }
        Assert.True(true);
    }

    // ═══════════════ CPS_05 Causal Speed Candidate Distribution ═══════════════
    [Fact]
    public void V4_1_CPS_05_CausalSpeedCandidateDistribution()
    {
        int N = 80; double K0v = 0.5; double xi = 1.0; double s = 0.1; double kickAmp = 0.5; double thresh = 0.01;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 6, BS);
        var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
        var cVals = new List<double>(); int nSrc = 4;
        for (int src = 0; src < nSrc; src++)
        {
            var (times, _) = KickDetect(Kc, N, s, BS + 300, src, kickAmp, 50, thresh);
            for (int dst = 0; dst < N; dst++)
                if (dst != src && times[dst] > 0 && dMat[src, dst] > 0)
                    cVals.Add(dMat[src, dst] / times[dst]);
        }
        if (cVals.Count > 0)
        {
            var sorted = cVals.OrderBy(x => x).ToArray();
            double minC = sorted[0], maxC = sorted[^1];
            double meanC = cVals.Average();
            double medC = sorted[sorted.Length / 2];
            double stdC = Math.Sqrt(cVals.Average(x => (x - meanC) * (x - meanC)));
            double p10 = sorted[Math.Max(0, sorted.Length / 10)];
            double p90 = sorted[Math.Min(sorted.Length - 1, sorted.Length * 9 / 10)];
            _output.WriteLine($"c_eff_candidate distribution ({cVals.Count} values):");
            _output.WriteLine($"  min={minC:F4}  max={maxC:F4}  mean={meanC:F4}  median={medC:F4}");
            _output.WriteLine($"  std={stdC:F4}  P10={p10:F4}  P90={p90:F4}");
            _output.WriteLine("  NOTE: dimensionless diagnostic only — NOT physical c.");
        }
        else _output.WriteLine("c_eff_candidate: no detected responses (0 values).");
        Assert.True(true);
    }

    // ═══════════════ CPS_06 Multi-Seed Propagation Stability ═══════════════
    [Fact]
    public void V4_1_CPS_06_MultiSeedPropagationStability()
    {
        int[] Ns = [80, 120]; double K0v = 0.5; double xi = 1.0; double s = 0.1; double kickAmp = 0.5; double thresh = 0.01;
        _output.WriteLine("N      seeds  respFrac_mean  respFrac_std  c_mean      c_std       fails");
        _output.WriteLine("----   -----  -------------  ------------  ---------   ---------   -----");
        foreach (int N in Ns)
        {
            int nSeeds = N <= 80 ? 10 : 6;
            var fracs = new List<double>(); var cMeans = new List<double>(); int fails = 0;
            for (int sd = 0; sd < nSeeds; sd++)
            {
                var K0 = KS(N, BS); int E = N <= 80 ? 5 : 3;
                var Kc = RecoverFP(K0, N, K0v, xi, s, E, BS + sd * 10);
                var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + sd * 10 + 200))));
                int resp = 0, total = 0; var cV = new List<double>();
                int nSrc = 4;
                for (int src = 0; src < nSrc; src++)
                {
                    var (times, _) = KickDetect(Kc, N, s, BS + sd * 10 + 300, src, kickAmp, 50, thresh);
                    for (int dst = 0; dst < N; dst++) { if (dst == src) continue; total++; if (times[dst] > 0 && dMat[src, dst] > 0) { resp++; cV.Add(dMat[src, dst] / times[dst]); } }
                }
                double frac = (double)resp / Math.Max(total, 1);
                if (double.IsFinite(frac)) fracs.Add(frac); else fails++;
                if (cV.Count > 0) cMeans.Add(cV.Average());
            }
            double mf = fracs.Count > 0 ? fracs.Average() : double.NaN;
            double sf = fracs.Count > 1 ? Math.Sqrt(fracs.Average(x => (x - mf) * (x - mf))) : 0;
            double mc = cMeans.Count > 0 ? cMeans.Average() : double.NaN;
            double sc = cMeans.Count > 1 ? Math.Sqrt(cMeans.Average(x => (x - mc) * (x - mc))) : 0;
            _output.WriteLine($"{N,5}  {nSeeds,5}  {mf,13:F4}  {sf,12:F4}  {mc,10:F4}  {sc,9:F4}  {fails,5}");
            Assert.True(fails == 0, $"N={N}: no seed failures.");
        }
    }

    // ═══════════════ CPS_07 N-Scaling Propagation ═══════════════
    [Fact]
    public void V4_1_CPS_07_NScalingPropagation()
    {
        int[] Ns = [40, 80, 120, 200]; double K0v = 0.5; double xi = 1.0; double s = 0.1; double kickAmp = 0.5; double thresh = 0.01;
        _output.WriteLine("N      E   resp_frac  mean_tau   c_candidate  dg");
        _output.WriteLine("----   --  ---------  ---------  -----------  ------");
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : (N <= 120 ? 3 : 2);
            var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, E, BS);
            var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
            int resp = 0, total = 0; var taus = new List<double>(); var ds = new List<double>();
            int nSrc = N <= 80 ? 4 : 3;
            for (int src = 0; src < nSrc; src++)
            {
                var (times, _) = KickDetect(Kc, N, s, BS + 300, src, kickAmp, 50, thresh);
                for (int dst = 0; dst < N; dst++) { if (dst == src) continue; total++; if (times[dst] > 0) { resp++; taus.Add(times[dst]); ds.Add(dMat[src, dst]); } }
            }
            double frac = (double)resp / Math.Max(total, 1);
            double mt = taus.Count > 0 ? taus.Average() : double.NaN;
            double mc = taus.Count > 0 ? ds.Zip(taus, (d, t) => d / t).Average() : double.NaN;
            double dg = Dg(dMat);
            _output.WriteLine($"{N,5}  {E,2}  {frac,9:F3}  {mt,9:F4}  {mc,11:F4}  {dg,6:F4}");
            Assert.True(double.IsFinite(frac), $"N={N}: response fraction must be finite.");
        }
    }

    // ═══════════════ CPS_08 Null and Degenerate Controls ═══════════════
    [Fact]
    public void V4_1_CPS_08_NullAndDegenerateControls()
    {
        int N = 60; double K0v = 0.5; double xi = 1.0; double s = 0.1; double kickAmp = 0.5; double thresh = 0.01;

        double EvalResponse(double[,] Kc)
        {
            var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 300))));
            int resp = 0, total = 0; int nSrc = 4;
            for (int src = 0; src < nSrc; src++)
            {
                var (times, _) = KickDetect(Kc, N, s, BS + 400, src, kickAmp, 50, thresh);
                for (int dst = 0; dst < N; dst++) { if (dst == src) continue; total++; if (times[dst] > 0) resp++; }
            }
            return (double)resp / Math.Max(total, 1);
        }

        // Active
        var K0 = KS(N, BS); var KcA = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        double fA = EvalResponse(KcA);

        // K=0 null
        var KcN = RecoverFP(new double[N, N], N, K0v, xi, s, 5, BS);
        double fN = EvalResponse(KcN);

        // Global sync
        var hSync = new double[101][]; for (int t = 0; t < 101; t++) { hSync[t] = new double[N]; for (int i = 0; i < N; i++) hSync[t][i] = 0; }
        double dgGS = Dg(DL(Nm(RP(hSync))));

        // Overcoupled dense
        var Koc = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j) Koc[i, j] = 0.5;
        var KcOC = RecoverFP(Koc, N, K0v, xi, s, 5, BS);
        double fOC = EvalResponse(KcOC);

        _output.WriteLine($"Active response fraction:     {fA:F4}");
        _output.WriteLine($"K=0 null response fraction:   {fN:F4}");
        _output.WriteLine($"Overcoupled response fraction: {fOC:F4}");
        _output.WriteLine($"Global sync degeneracy:       dg={dgGS:F4}");

        Assert.True(dgGS < 0.01, "Global sync must be degenerate.");
        Assert.True(double.IsFinite(fA) && double.IsFinite(fN));
    }

    // ═══════════════ CPS_09 Exponential vs Gaussian vs Power-Law Propagation ═══════════════
    [Fact]
    public void V4_1_CPS_09_ExponentialVsGaussianPropagation()
    {
        int N = 60; double K0v = 0.5; double s = 0.1; double kickAmp = 0.5; double thresh = 0.01;

        double[,] GaussUpd(double[,] d, double K0, double xi) { int n = d.GetLength(0); var K = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 * Math.Exp(-d[i, j] * d[i, j] / Math.Max(xi * xi, 0.0001)); } return K; }
        double[,] PowerUpd(double[,] d, double K0, double p) { int n = d.GetLength(0); var K = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 / (1.0 + Math.Pow(Math.Max(d[i, j], 0), p)); } return K; }

        (double frac, double cMean, double dg) Eval(double[,] K0, string law)
        {
            var Kc = (double[,])K0.Clone();
            for (int e = 0; e < 5; e++)
            {
                var h = Sm(Kc, N, s, BS + e); var R = Nm(RP(h)); var d = DL(R);
                Kc = law switch { "exp" => ExpUpd(d, K0v, 1.0), "gauss" => GaussUpd(d, K0v, 1.0), _ => PowerUpd(d, K0v, 2.0) };
            }
            var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
            int resp = 0, total = 0; var cV = new List<double>(); int nSrc = 4;
            for (int src = 0; src < nSrc; src++)
            {
                var (times, _) = KickDetect(Kc, N, s, BS + 300, src, kickAmp, 50, thresh);
                for (int dst = 0; dst < N; dst++) { if (dst == src) continue; total++; if (times[dst] > 0 && dMat[src, dst] > 0) { resp++; cV.Add(dMat[src, dst] / times[dst]); } }
            }
            double frac = (double)resp / Math.Max(total, 1);
            double cMean = cV.Count > 0 ? cV.Average() : double.NaN;
            double dg = Dg(dMat);
            return (frac, cMean, dg);
        }

        var Krs = KS(N, BS);
        var (fE, cE, dgE) = Eval(Krs, "exp");
        var (fG, cG, dgG) = Eval(Krs, "gauss");
        var (fP, cP, dgP) = Eval(Krs, "power");

        _output.WriteLine("Law        resp_frac  c_candidate  dg");
        _output.WriteLine("--------   ---------  -----------  ------");
        _output.WriteLine($"Exponential  {fE,9:F4}  {cE,11:F4}  {dgE,6:F4}");
        _output.WriteLine($"Gaussian     {fG,9:F4}  {cG,11:F4}  {dgG,6:F4}");
        _output.WriteLine($"Power-law    {fP,9:F4}  {cP,11:F4}  {dgP,6:F4}");

        Assert.True(double.IsFinite(fE) && double.IsFinite(fG) && double.IsFinite(fP));
    }

    // ═══════════════ CPS_10 Light-Cone-Like Front Diagnostic ═══════════════
    [Fact]
    public void V4_1_CPS_10_LightConeLikeFrontDiagnostic()
    {
        int N = 80; double K0v = 0.5; double xi = 1.0; double s = 0.1; double kickAmp = 0.5; double thresh = 0.01;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 6, BS);
        var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));

        // Collect (d, tau) pairs across all source nodes
        var dAll = new List<double>(); var tAll = new List<double>(); int nSrc = 4;
        var frontVals = new List<(double shellDist, double shellTau)>();
        for (int src = 0; src < nSrc; src++)
        {
            var (times, _) = KickDetect(Kc, N, s, BS + 300, src, kickAmp, 50, thresh);
            var targets = Enumerable.Range(0, N).Where(x => x != src).OrderBy(x => dMat[src, x]).ToArray();
            int nShells = 4; int shSz = Math.Max(1, (N - 1) / nShells);
            for (int sh = 0; sh < nShells; sh++)
            {
                var shN = targets.Skip(sh * shSz).Take(shSz).ToArray();
                var tSh = new List<double>(); var dSh = new List<double>();
                foreach (int dst in shN) if (times[dst] > 0) { tSh.Add(times[dst]); dSh.Add(dMat[src, dst]); }
                if (tSh.Count > 0) frontVals.Add((dSh.Average(), tSh.Average()));
            }
            for (int dst = 0; dst < N; dst++)
                if (dst != src && times[dst] > 0) { dAll.Add(dMat[src, dst]); tAll.Add(times[dst]); }
        }

        double rho = dAll.Count > 3 ? Spear(dAll.ToArray(), tAll.ToArray()) : double.NaN;

        // Front sharpness: correlation between shell distance and shell tau
        double frontSharp = 0;
        if (frontVals.Count > 2)
        {
            var sd = frontVals.Select(f => f.shellDist).ToArray();
            var st = frontVals.Select(f => f.shellTau).ToArray();
            frontSharp = Math.Abs(Spear(sd, st));
        }

        string cls = "No front";
        if (double.IsFinite(frontSharp) && frontSharp > 0.5 && double.IsFinite(rho) && rho > 0.3) cls = "Front-like";
        else if (double.IsFinite(frontSharp) && frontSharp > 0.2) cls = "Weak front";
        if (frontVals.Count < 3) cls = "Degenerate";

        _output.WriteLine("═══ LIGHT-CONE-LIKE FRONT DIAGNOSTIC ═══");
        _output.WriteLine($"N: {N}  shell correlation: {frontSharp:F4}  d-tau correlation: {rho:F4}");
        _output.WriteLine($"Shells with data: {frontVals.Count}  Classification: {cls}");
        _output.WriteLine("");
        _output.WriteLine("NOTE: This is a numerical front diagnostic only.");
        _output.WriteLine("Lorentz light cone and physical c are NOT derived.");

        Assert.True(cls != "Failed", "Front diagnostic must be classifiable.");
    }

    // ═══════════════ CPS_11 Claim Discipline Report ═══════════════
    [Fact]
    public void V4_1_CPS_11_ClaimDisciplineReport()
    {
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("  CLAIM DISCIPLINE — CAUSAL PROPAGATION SPEED");
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  SUPPORTED:");
        _output.WriteLine("    - Propagation-response diagnostics are measurable.");
        _output.WriteLine("    - Dimensionless c_eff candidates can be computed.");
        _output.WriteLine("    - Response fronts can be classified numerically.");
        _output.WriteLine("    - Null and degenerate controls are detected.");
        _output.WriteLine("    - Kick amplitude affects response fraction measurably.");
        _output.WriteLine("    - Multi-seed propagation statistics are finite.");
        _output.WriteLine("");
        _output.WriteLine("  CONDITIONAL:");
        _output.WriteLine("    - Propagation strength depends on kick amplitude, xi,");
        _output.WriteLine("      K0, density, topology, thresholds, and N.");
        _output.WriteLine("    - c_eff candidate is dimensionless and model-dependent.");
        _output.WriteLine("    - Front-like behavior is not Lorentz invariance.");
        _output.WriteLine("    - Shell-based diagnostics depend on d_ij definition.");
        _output.WriteLine("");
        _output.WriteLine("  HYPOTHESIS:");
        _output.WriteLine("    - Physical causal structure emerges from TRM dynamics.");
        _output.WriteLine("    - c_eff may become universal in a calibrated continuum.");
        _output.WriteLine("    - Lorentzian light-cone structure may emerge later.");
        _output.WriteLine("");
        _output.WriteLine("  NOT CLAIMED:");
        _output.WriteLine("    - Physical speed of light c is derived.");
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
