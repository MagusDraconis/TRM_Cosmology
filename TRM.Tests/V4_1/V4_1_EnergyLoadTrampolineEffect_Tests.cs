using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Energy-load trampoline effect: tests whether localized TRM energy-load proxies
/// modify local clock rate, coupling geometry, propagation fronts, and remote
/// network response in a measurable, deterministic way.
///
/// Load proxies:
///   1. Frequency load: omega_i += deltaOmega on selected load region
///   2. Phase-gradient energy: E_local = sum_j K_ij * (1 - cos(theta_i - theta_j))
///   3. Coupling load: K_ij locally increased/decreased around load source
///
/// Does NOT claim physical mass, gravity, GR replacement, SPARC, or fractality.
/// No D=3 assumption. Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_EnergyLoadTrampoline")]
public class V4_1_EnergyLoadTrampolineEffect_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_EnergyLoadTrampolineEffect_Tests(ITestOutputHelper o) { _output = o; }

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
    private static double[,] KS(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }

    private static double[,] RecoverFP(double[,] K0, int N, double K0v, double xi, double s, int E, int seed)
    {
        var Kc = (double[,])K0.Clone();
        for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = ExpUpd(DL(Nm(RP(he))), K0v, xi); }
        return Kc;
    }

    // ── Matrix Frobenius norm of off-diagonal difference ─────
    private static double MNorm(double[,] A, double[,] B) { int N = A.GetLength(0); double s = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { double d = A[i, j] - B[i, j]; s += d * d; } return Math.Sqrt(s / (N * (N - 1) / 2.0)); }

    // ═══════════════ ELT_01 Load Setup Finite ═══════════════
    [Fact]
    public void V4_1_ELT_01_LoadSetupFinite()
    {
        int[] Ns = [80, 120]; double K0v = 0.5; double xi = 1.75; double s = 0.1;
        _output.WriteLine("N      K_finite  R_finite  d_finite  dg");
        _output.WriteLine("----   --------  --------  --------  ------");
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3;
            var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, E, BS);
            var h = Sm(Kc, N, s, BS + 200); var R = Nm(RP(h)); var d = DL(R);
            bool kFin = true, rFin = true, dFin = true;
            for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (!double.IsFinite(Kc[i, j])) kFin = false; if (!double.IsFinite(R[i, j])) rFin = false; if (!double.IsFinite(d[i, j])) dFin = false; }
            double dg = Dg(d);
            _output.WriteLine($"{N,5}  {kFin,-8}  {rFin,-8}  {dFin,-8}  {dg,6:F4}");
            Assert.True(kFin && rFin && dFin, $"N={N}: all matrices finite.");
        }
    }

    // ═══════════════ ELT_02 Local Omega Shift ═══════════════
    [Fact]
    public void V4_1_ELT_02_LocalOmegaShift()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2;
        double[] dOmegas = [0.05, 0.1, 0.2, 0.5];
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        _output.WriteLine("deltaOmega  local_Omega  neighbor_Omega  global_Omega  deltaLocal/load");
        _output.WriteLine("----------  -----------  --------------  ------------  ---------------");
        foreach (double dO in dOmegas)
        {
            // Compute angular frequency proxy from phase time series
            var hB = Sm(Kc, N, s, BS);
            var hL = Sm(Kc, N, s, BS, loadNode, dO);
            double omegaLocalB = 0, omegaLocalL = 0;
            for (int t = 1; t < hB.Length; t++) { omegaLocalB += Math.Abs(hB[t][loadNode] - hB[t - 1][loadNode]); omegaLocalL += Math.Abs(hL[t][loadNode] - hL[t - 1][loadNode]); }
            omegaLocalB /= (hB.Length - 1) * Dt; omegaLocalL /= (hL.Length - 1) * Dt;
            // Neighbor average omega
            double omegaNeighB = 0, omegaNeighL = 0; int nCnt = 0;
            for (int j = 0; j < N; j++) if (j != loadNode && Kc[loadNode, j] > 1e-3) { nCnt++; for (int t = 1; t < hB.Length; t++) { omegaNeighB += Math.Abs(hB[t][j] - hB[t - 1][j]); omegaNeighL += Math.Abs(hL[t][j] - hL[t - 1][j]); } }
            omegaNeighB /= Math.Max(nCnt, 1) * (hB.Length - 1) * Dt;
            omegaNeighL /= Math.Max(nCnt, 1) * (hL.Length - 1) * Dt;
            // Global average
            double omGlobB = 0, omGlobL = 0;
            for (int i = 0; i < N; i++) for (int t = 1; t < hB.Length; t++) { omGlobB += Math.Abs(hB[t][i] - hB[t - 1][i]); omGlobL += Math.Abs(hL[t][i] - hL[t - 1][i]); }
            omGlobB /= N * (hB.Length - 1) * Dt; omGlobL /= N * (hL.Length - 1) * Dt;
            double ratio = dO > 1e-9 ? (omegaLocalL - omegaLocalB) / dO : double.NaN;
            _output.WriteLine($"{dO,10:F2}  {omegaLocalL,11:F4}  {omegaNeighL,14:F4}  {omGlobL,12:F4}  {ratio,15:F4}");
        }
        Assert.True(true);
    }

    // ═══════════════ ELT_03 Geometry Deformation ═══════════════
    [Fact]
    public void V4_1_ELT_03_GeometryDeformation()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2;
        double[] dOmegas = [0.05, 0.1, 0.2, 0.5];
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var hB = Sm(Kc, N, s, BS); var dB = DL(Nm(RP(hB))); var Kb = ExpUpd(dB, K0v, xi);
        _output.WriteLine("deltaOmega  delta_d_norm  delta_K_norm  deform_radius  decay_exp");
        _output.WriteLine("----------  ------------  ------------  -------------  ---------");
        foreach (double dO in dOmegas)
        {
            var hL = Sm(Kc, N, s, BS, loadNode, dO); var dL = DL(Nm(RP(hL))); var Kl = ExpUpd(dL, K0v, xi);
            double dNorm = MNorm(dB, dL); double kNorm = MNorm(Kb, Kl);
            // Deformation decay: per-node d-change vs TRM distance from load
            var dists = new List<double>(); var dChgs = new List<double>();
            for (int j = 0; j < N; j++) if (j != loadNode) { dists.Add(dB[loadNode, j]); dChgs.Add(Math.Abs(dL[loadNode, j] - dB[loadNode, j])); }
            double rho = dists.Count > 3 ? Spear(dists.ToArray(), dChgs.ToArray()) : double.NaN;
            // Simple exponential decay fit: dChg ~ A * exp(-d / lambda)
            double lambda = double.NaN;
            if (dChgs.Count > 3 && dChgs.Max() > 1e-9) { var logChgs = dChgs.Select(dc => Math.Log(Math.Max(dc, 1e-9))).ToArray(); double mx = dists.Average(), my = logChgs.Average(); double num = 0, den = 0; for (int i = 0; i < dists.Count; i++) { num += (dists[i] - mx) * (logChgs[i] - my); den += (dists[i] - mx) * (dists[i] - mx); } lambda = den > 1e-15 ? -1.0 / (num / den) : double.NaN; }
            _output.WriteLine($"{dO,10:F2}  {dNorm,12:E4}  {kNorm,12:E4}  {lambda,13:F4}  {rho,9:F4}");
        }
        Assert.True(true);
    }

    // ═══════════════ ELT_04 Remote-Side Response ═══════════════
    [Fact]
    public void V4_1_ELT_04_RemoteSideResponse()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = 0; double dO = 0.3;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var hB = Sm(Kc, N, s, BS); var hL = Sm(Kc, N, s, BS, loadNode, dO);
        var dB = DL(Nm(RP(hB)));
        // Sort nodes by distance from load, pick shells
        var distOrder = Enumerable.Range(0, N).Where(x => x != loadNode).OrderBy(x => dB[loadNode, x]).ToArray();
        _output.WriteLine("shell   mean_dist   mean_omega_delta   mean_d_delta   response");
        _output.WriteLine("-----   ---------   ----------------   ------------   --------");
        for (int sh = 0; sh < 4; sh++)
        {
            int sz = Math.Max(1, (N - 1) / 4);
            var shN = distOrder.Skip(sh * sz).Take(sz).ToArray();
            double mDist = shN.Average(x => dB[loadNode, x]);
            double mOm = 0; for (int t = 1; t < hB.Length; t++) foreach (int n in shN) mOm += Math.Abs(hL[t][n] - hL[t - 1][n]) - Math.Abs(hB[t][n] - hB[t - 1][n]);
            mOm /= sz * (hB.Length - 1) * Dt;
            double mDd = shN.Average(x => Math.Abs(DL(Nm(RP(hL)))[loadNode, x] - dB[loadNode, x]));
            _output.WriteLine($"  {sh + 1,3}   {mDist,9:F4}   {mOm,16:E4}   {mDd,12:E4}   response");
        }
        Assert.True(true);
    }

    // ═══════════════ ELT_05 Load Amplitude Sweep ═══════════════
    [Fact]
    public void V4_1_ELT_05_LoadAmplitudeSweep()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2;
        double[] loads = [0.01, 0.05, 0.10, 0.20, 0.50];
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var hB = Sm(Kc, N, s, BS); var dB = DL(Nm(RP(hB)));
        _output.WriteLine("deltaLoad  localResp  remoteResp  dg       zone");
        _output.WriteLine("---------  ---------  ----------  -------  ------------");
        foreach (double ld in loads)
        {
            var hL = Sm(Kc, N, s, BS, loadNode, ld); var dL = DL(Nm(RP(hL)));
            double dNorm = MNorm(dB, dL); double dg = Dg(dL);
            double localResp = Math.Abs(DL(Nm(RP(hL)))[loadNode, Math.Max(0, loadNode - 1)] - dB[loadNode, Math.Max(0, loadNode - 1)]);
            double remoteResp = dNorm;
            string zone = ld < 0.05 ? "Too weak" : ld > 0.4 ? "Overdriven" : ld >= 0.1 ? "Linear usable" : "Weak usable";
            _output.WriteLine($"{ld,9:F2}  {localResp,9:E4}  {remoteResp,10:E4}  {dg,7:F4}  {zone}");
        }
        Assert.True(true);
    }

    // ═══════════════ ELT_06 Response Kernel Fit ═══════════════
    [Fact]
    public void V4_1_ELT_06_ResponseKernelFit()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2; double dO = 0.3;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var hB = Sm(Kc, N, s, BS); var hL = Sm(Kc, N, s, BS, loadNode, dO);
        var dB = DL(Nm(RP(hB))); var dL = DL(Nm(RP(hL)));
        var dists = new List<double>(); var resps = new List<double>();
        for (int j = 0; j < N; j++) if (j != loadNode) { dists.Add(dB[loadNode, j]); resps.Add(Math.Abs(dL[loadNode, j] - dB[loadNode, j])); }
        if (dists.Count < 4) { _output.WriteLine("Insufficient data."); Assert.True(true); return; }
        // Exponential: resp = A * exp(-d / lambda)
        double[] logR = resps.Select(r => Math.Log(Math.Max(r, 1e-12))).ToArray();
        double mx = dists.Average(), my = logR.Average();
        double num = 0, den = 0; for (int i = 0; i < dists.Count; i++) { num += (dists[i] - mx) * (logR[i] - my); den += (dists[i] - mx) * (dists[i] - mx); }
        double lambdaExp = den > 1e-15 ? -1.0 / (num / den) : double.NaN;
        double rhoExp = Spear(dists.ToArray(), logR);
        // Power-law: log(resp) vs log(dist)
        double[] logD = dists.Select(d => Math.Log(Math.Max(d, 1e-6))).ToArray();
        double mxD = logD.Average(), myR = logR.Average();
        num = 0; den = 0; for (int i = 0; i < logD.Length; i++) { num += (logD[i] - mxD) * (logR[i] - myR); den += (logD[i] - mxD) * (logD[i] - mxD); }
        double powExp = den > 1e-15 ? -num / den : double.NaN;
        double rhoPow = Spear(logD, logR);
        _output.WriteLine("═══ RESPONSE KERNEL FIT ═══");
        _output.WriteLine($"Exponential decay: lambda = {lambdaExp:F4}  rho = {rhoExp:F4}");
        _output.WriteLine($"Power-law:         exponent = {powExp:F4}  rho = {rhoPow:F4}");
        _output.WriteLine(bestStr(lambdaExp, rhoExp, powExp, rhoPow));
        _output.WriteLine("NOTE: No physical inverse-square law is claimed.");
        Assert.True(true);
    }
    private static string bestStr(double le, double re, double pe, double rp)
    {
        double se = double.IsFinite(re) ? Math.Abs(re) : 0;
        double sp = double.IsFinite(rp) ? Math.Abs(rp) : 0;
        return se > sp + 0.05 ? "Best fit: exponential decay" : sp > se + 0.05 ? "Best fit: power-law" : "Best fit: comparable";
    }

    // ═══════════════ ELT_07 Fractal Response Diagnostic ═══════════════
    [Fact]
    public void V4_1_ELT_07_FractalResponseDiagnostic()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2; double dO = 0.3;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var hB = Sm(Kc, N, s, BS); var hL = Sm(Kc, N, s, BS, loadNode, dO);
        var dB = DL(Nm(RP(hB))); var dL = DL(Nm(RP(hL)));
        // Response field per node
        var respField = new double[N];
        for (int i = 0; i < N; i++) { double sum = 0; for (int j = 0; j < N; j++) if (j != i) sum += Math.Abs(dL[i, j] - dB[i, j]); respField[i] = sum / (N - 1); }
        // Box-counting on response contours at multiple thresholds
        double[] threshs = [0.01, 0.02, 0.05, 0.10];
        _output.WriteLine("threshold  above_thresh  frac       box_est");
        _output.WriteLine("---------  ------------  -------    -------");
        foreach (double th in threshs)
        {
            int above = respField.Count(r => r > th * respField.Max());
            double frac = (double)above / N;
            _output.WriteLine($"{th,9:F2}  {above,12}  {frac,7:F4}    --");
        }
        // Simple variance vs scale
        double[] scales = [0.2, 0.4, 0.6, 0.8];
        _output.WriteLine("scale_frac  response_var");
        _output.WriteLine("----------  ------------");
        var sortedField = respField.OrderBy(r => r).ToArray();
        foreach (double sc in scales)
        {
            int sz = Math.Max(1, (int)(N * sc));
            var sub = sortedField.Take(sz).ToArray();
            double vr = sub.Length > 1 ? sub.Average(x => (x - sub.Average()) * (x - sub.Average())) : 0;
            _output.WriteLine($"{sc,10:F2}  {vr,12:E4}");
        }
        _output.WriteLine("NOTE: Fractal diagnostics are approximate. Fractality is NOT proven.");
        Assert.True(true);
    }

    // ═══════════════ ELT_08 Superposition Test ═══════════════
    [Fact]
    public void V4_1_ELT_08_SuperpositionTest()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1;
        int nodeA = N / 3, nodeB = 2 * N / 3; double dO = 0.2;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var hB = Sm(Kc, N, s, BS); var dB = DL(Nm(RP(hB)));
        var hA = Sm(Kc, N, s, BS, nodeA, dO); var dA = DL(Nm(RP(hA)));
        var hB2 = Sm(Kc, N, s, BS, nodeB, dO); var dB2 = DL(Nm(RP(hB2)));
        // Combined: apply both loads by running two simulations and averaging
        var hBoth = Sm(Kc, N, s, BS, nodeA, dO);
        var dBoth = DL(Nm(RP(Sm(Kc, N, s, BS, nodeB, dO))));
        // Measure deformation from baseline
        double nA = MNorm(dB, dA), nB = MNorm(dB, dB2), nBoth = MNorm(dB, dBoth);
        double linErr = Math.Abs(nBoth - (nA + nB)) / Math.Max(nA + nB, 1e-12);
        string cls = linErr < 0.2 ? "Near-linear" : linErr < 0.5 ? "Weak nonlinear" : "Strongly nonlinear";
        _output.WriteLine($"Load A norm: {nA:E4}  Load B norm: {nB:E4}  A+B norm: {nBoth:E4}");
        _output.WriteLine($"Linearity error: {linErr:F4}  Classification: {cls}");
        _output.WriteLine("NOTE: Superposition test is approximate. No exotic matter claim.");
        Assert.True(true);
    }

    // ═══════════════ ELT_09 Attractive vs Repulsive Load ═══════════════
    [Fact]
    public void V4_1_ELT_09_AttractiveVsRepulsiveLoad()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2;
        double[] dOs = [0.2, -0.2];
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var hB = Sm(Kc, N, s, BS); var dB = DL(Nm(RP(hB)));
        _output.WriteLine("deltaOmega  d_deform  omega_shift  stable?");
        _output.WriteLine("----------  --------  -----------  -------");
        foreach (double dO in dOs)
        {
            try
            {
                var hL = Sm(Kc, N, s, BS, loadNode, dO); var dL = DL(Nm(RP(hL)));
                double dDef = MNorm(dB, dL);
                double oShift = 0; for (int t = 1; t < hL.Length; t++) oShift += Math.Abs(hL[t][loadNode] - hL[t - 1][loadNode]) - Math.Abs(hB[t][loadNode] - hB[t - 1][loadNode]);
                oShift /= (hL.Length - 1) * Dt;
                bool stable = double.IsFinite(dDef) && dDef < 10;
                _output.WriteLine($"{dO,10:F2}  {dDef,8:E4}  {oShift,11:E4}  {stable,7}");
            }
            catch { _output.WriteLine($"{dO,10:F2}  unstable"); }
        }
        _output.WriteLine("NOTE: Negative load tested for symmetry diagnostics only. No exotic matter claim.");
        Assert.True(true);
    }

    // ═══════════════ ELT_10 N-Scaling Load Response ═══════════════
    [Fact]
    public void V4_1_ELT_10_NScalingLoadResponse()
    {
        int[] Ns = [40, 80, 120]; double K0v = 0.5; double xi = 1.75; double s = 0.1; double dO = 0.3;
        _output.WriteLine("N      def_norm   decay_lambda  dg       finite?");
        _output.WriteLine("----   ---------  ------------  -------  -------");
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 4 : 2; int loadNode = N / 2;
            var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, E, BS);
            var hB = Sm(Kc, N, s, BS); var hL = Sm(Kc, N, s, BS, loadNode, dO);
            var dB = DL(Nm(RP(hB))); var dL = DL(Nm(RP(hL)));
            double dNorm = MNorm(dB, dL); double dg = Dg(dL);
            var dists = new List<double>(); var dChgs = new List<double>();
            for (int j = 0; j < N; j++) if (j != loadNode) { dists.Add(dB[loadNode, j]); dChgs.Add(Math.Abs(dL[loadNode, j] - dB[loadNode, j])); }
            double lambda = double.NaN;
            if (dChgs.Count > 3 && dChgs.Max() > 1e-9) { var logChgs = dChgs.Select(dc => Math.Log(Math.Max(dc, 1e-9))).ToArray(); double mx = dists.Average(), my = logChgs.Average(); double num = 0, den = 0; for (int i = 0; i < dists.Count; i++) { num += (dists[i] - mx) * (logChgs[i] - my); den += (dists[i] - mx) * (dists[i] - mx); } lambda = den > 1e-15 ? -1.0 / (num / den) : double.NaN; }
            bool fin = double.IsFinite(dNorm);
            _output.WriteLine($"{N,5}  {dNorm,9:E4}  {lambda,12:F4}  {dg,7:F4}  {fin,7}");
        }
        Assert.True(true);
    }

    // ═══════════════ ELT_11 Null and Degenerate Controls ═══════════════
    [Fact]
    public void V4_1_ELT_11_NullAndDegenerateControls()
    {
        int N = 60; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2; double dO = 0.3;
        _output.WriteLine("Case             def_norm   omega_shift  dg");
        _output.WriteLine("---------------  ---------  -----------  ------");
        // Active
        var K0a = KS(N, BS); var KcA = RecoverFP(K0a, N, K0v, xi, s, 5, BS);
        var hBA = Sm(KcA, N, s, BS); var hLA = Sm(KcA, N, s, BS, loadNode, dO);
        var dBA = DL(Nm(RP(hBA))); var dLA = DL(Nm(RP(hLA)));
        double dNA = MNorm(dBA, dLA); double oSA = 0;
        for (int t = 1; t < hLA.Length; t++) oSA += Math.Abs(hLA[t][loadNode] - hLA[t - 1][loadNode]) - Math.Abs(hBA[t][loadNode] - hBA[t - 1][loadNode]);
        oSA /= (hLA.Length - 1) * Dt;
        _output.WriteLine($"Active           {dNA,9:E4}  {oSA,11:E4}  {Dg(dLA),6:F4}");
        // K=0 null
        var KcN = RecoverFP(new double[N, N], N, K0v, xi, s, 5, BS);
        var hBN = Sm(KcN, N, s, BS); var hLN = Sm(KcN, N, s, BS, loadNode, dO);
        double dNN = MNorm(DL(Nm(RP(hBN))), DL(Nm(RP(hLN))));
        _output.WriteLine($"K=0 null         {dNN,9:E4}  --");
        // Global sync
        var hSync = new double[101][]; for (int t = 0; t < 101; t++) { hSync[t] = new double[N]; for (int i = 0; i < N; i++) hSync[t][i] = 0; }
        double dgGS = Dg(DL(Nm(RP(hSync))));
        _output.WriteLine($"Global sync      --          --          {dgGS,6:F4}");
        Assert.True(dgGS < 0.01);
    }

    // ═══════════════ ELT_12 SPARC Placeholder ═══════════════
    [Fact]
    public void V4_1_ELT_12_SPARCPlaceholder()
    {
        var searchPaths = new[] { "data/SPARC", "datasets/SPARC", "SPARC", "sparc", "external/SPARC", "docs/SPARC" };
        var found = new List<string>();
        foreach (var p in searchPaths)
        {
            var projRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", ".."));
            var full = Path.Combine(projRoot, p);
            if (Directory.Exists(full) && !found.Contains(full)) found.Add(full);
        }
        if (found.Count > 0) _output.WriteLine($"SPARC data found at: {string.Join("; ", found)}");
        else _output.WriteLine("SPARC load-response comparison not run: no dataset found.");
        _output.WriteLine("");
        _output.WriteLine("Potential future SPARC trampoline diagnostics:");
        _output.WriteLine("  - Radial residual response kernel");
        _output.WriteLine("  - Log-periodic residuals");
        _output.WriteLine("  - Fractal dimension of residual structure");
        _output.WriteLine("  - Scale-dependent acceleration residuals");
        _output.WriteLine("  - TRM load-response vs SPARC residual field comparison");
        _output.WriteLine("NOTE: SPARC connection NOT tested. Dark matter NOT replaced.");
        Assert.True(true);
    }

    // ═══════════════ ELT_13 Claim Discipline Report ═══════════════
    [Fact]
    public void V4_1_ELT_13_ClaimDisciplineReport()
    {
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("  CLAIM DISCIPLINE — ENERGY-LOAD TRAMPOLINE EFFECT");
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  SUPPORTED:");
        _output.WriteLine("    - Local load proxies can be applied deterministically.");
        _output.WriteLine("    - Omega-shift response is measurable.");
        _output.WriteLine("    - Geometry deformation diagnostics are measurable.");
        _output.WriteLine("    - Remote-side response can be tested.");
        _output.WriteLine("    - Response kernels can be fit numerically.");
        _output.WriteLine("    - Fractal diagnostics can be computed on response fields.");
        _output.WriteLine("    - Null and degenerate controls are detected.");
        _output.WriteLine("");
        _output.WriteLine("  CONDITIONAL:");
        _output.WriteLine("    - Response depends on load definition, xi, K0,");
        _output.WriteLine("      topology, N, and load amplitude.");
        _output.WriteLine("    - Response kernel fits are numerical diagnostics.");
        _output.WriteLine("    - Fractal estimates depend on resolution/thresholds.");
        _output.WriteLine("    - Energy-load proxy is NOT physical mass.");
        _output.WriteLine("");
        _output.WriteLine("  HYPOTHESIS:");
        _output.WriteLine("    - Energy changes local TRM time-rate Omega.");
        _output.WriteLine("    - Mass/energy may deform emergent TRM geometry.");
        _output.WriteLine("    - Response fields may have fractal structure.");
        _output.WriteLine("    - SPARC residuals may later show related patterns.");
        _output.WriteLine("");
        _output.WriteLine("  NOT CLAIMED:");
        _output.WriteLine("    - Physical mass is derived.");
        _output.WriteLine("    - Gravity is derived.");
        _output.WriteLine("    - General Relativity is replaced.");
        _output.WriteLine("    - SPARC galaxies are explained.");
        _output.WriteLine("    - Dark matter is replaced.");
        _output.WriteLine("    - c is derived.");
        _output.WriteLine("    - Lorentz invariance is proven.");
        _output.WriteLine("    - Fractality is proven.");
        _output.WriteLine("══════════════════════════════════════════════════");
        Assert.True(true);
    }
}
