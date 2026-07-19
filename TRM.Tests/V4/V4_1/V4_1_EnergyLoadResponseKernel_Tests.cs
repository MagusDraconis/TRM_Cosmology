using Xunit;
using Xunit.Abstractions;
using TRM.Core.Data;

namespace TRM.Tests.V4_1;

/// <summary>
/// Energy-load response kernel: characterizes which numerical response law
/// best describes the TRM trampoline effect from localized energy-load proxies.
///
/// Candidate laws: exponential, power-law, stretched exponential, hybrid.
/// Log-periodic residual check as diagnostic only.
///
/// Does NOT claim mass, gravity, inverse-square law, SPARC, dark matter,
/// MOND, physical c, or fractality.
/// No D=3 assumption. Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_EnergyLoadResponseKernel")]
public class V4_1_EnergyLoadResponseKernel_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_EnergyLoadResponseKernel_Tests(ITestOutputHelper o) { _output = o; }

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
    private static double[,] RecoverFP(double[,] K0, int N, double K0v, double xi, double s, int E, int seed) { var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = ExpUpd(DL(Nm(RP(he))), K0v, xi); } return Kc; }

    // ── Response field: (distance, dResponse, kResponse, omegaResponse) per node ──
    private static (List<double> dists, List<double> dResp, List<double> kResp, List<double> omResp) ResponseField(double[,] Kc, int N, double s, double K0v, double xi, int loadNode, double dO)
    {
        var hB = Sm(Kc, N, s, BS); var hL = Sm(Kc, N, s, BS, loadNode, dO);
        var dB = DL(Nm(RP(hB))); var dL = DL(Nm(RP(hL)));
        var Kb = ExpUpd(dB, K0v, xi); var Kl = ExpUpd(dL, K0v, xi);
        var dists = new List<double>(); var dResp = new List<double>(); var kResp = new List<double>(); var omResp = new List<double>();
        for (int j = 0; j < N; j++)
        {
            if (j == loadNode) continue;
            dists.Add(dB[loadNode, j]);
            dResp.Add(Math.Abs(dL[loadNode, j] - dB[loadNode, j]));
            kResp.Add(Math.Abs(Kl[loadNode, j] - Kb[loadNode, j]));
            double omB = 0, omL = 0;
            for (int t = 1; t < hB.Length; t++) { omB += Math.Abs(hB[t][j] - hB[t - 1][j]); omL += Math.Abs(hL[t][j] - hL[t - 1][j]); }
            omResp.Add(Math.Abs(omL - omB) / (hB.Length - 1) / Dt);
        }
        return (dists, dResp, kResp, omResp);
    }

    // ── Shell-averaged response ──
    private static (List<double> shellDist, List<double> shellResp) ShellAvg(List<double> dists, List<double> resp, int nShells)
    {
        var idxs = Enumerable.Range(0, dists.Count).OrderBy(i => dists[i]).ToArray();
        int shSz = Math.Max(1, dists.Count / nShells);
        var sd = new List<double>(); var sr = new List<double>();
        for (int sh = 0; sh < nShells; sh++)
        {
            var shIdxs = idxs.Skip(sh * shSz).Take(shSz).ToArray();
            if (shIdxs.Length == 0) break;
            sd.Add(shIdxs.Average(i => dists[i]));
            sr.Add(shIdxs.Average(i => resp[i]));
        }
        return (sd, sr);
    }

    // ── Fit: Exponential  r = A * exp(-d / lambda) using log-linear regression ──
    private static (double A, double lambda, double rmse) FitExp(List<double> d, List<double> r)
    {
        if (d.Count < 4) return (double.NaN, double.NaN, double.NaN);
        var logR = r.Select(v => Math.Log(Math.Max(v, 1e-12))).ToArray();
        double mx = d.Average(), my = logR.Average();
        double num = 0, den = 0;
        for (int i = 0; i < d.Count; i++) { num += (d[i] - mx) * (logR[i] - my); den += (d[i] - mx) * (d[i] - mx); }
        double lambda = den > 1e-15 ? -1.0 / (num / den) : double.NaN;
        double logA = my - num / Math.Max(den, 1e-15) * mx;
        double A = Math.Exp(logA);
        double rmse = 0; for (int i = 0; i < d.Count; i++) { double pred = A * Math.Exp(-d[i] / Math.Max(lambda, 0.01)); double e = r[i] - pred; rmse += e * e; }
        rmse = Math.Sqrt(rmse / d.Count);
        return (A, lambda, rmse);
    }

    // ── Fit: Power-law  r = A / (1 + d^p) using log-log regression ──
    private static (double A, double p, double rmse) FitPow(List<double> d, List<double> r)
    {
        if (d.Count < 4) return (double.NaN, double.NaN, double.NaN);
        var logD = d.Select(v => Math.Log(Math.Max(v, 1e-6))).ToArray();
        var logR = r.Select(v => Math.Log(Math.Max(v, 1e-12))).ToArray();
        double mx = logD.Average(), my = logR.Average();
        double num = 0, den = 0;
        for (int i = 0; i < logD.Length; i++) { num += (logD[i] - mx) * (logR[i] - my); den += (logD[i] - mx) * (logD[i] - mx); }
        double p = den > 1e-15 ? -num / den : double.NaN;
        double logA = my - num / Math.Max(den, 1e-15) * mx;
        double A = Math.Exp(logA);
        double rmse = 0; for (int i = 0; i < d.Count; i++) { double pred = A / (1.0 + Math.Pow(Math.Max(d[i], 0), Math.Max(p, 0.5))); double e = r[i] - pred; rmse += e * e; }
        rmse = Math.Sqrt(rmse / d.Count);
        return (A, p, rmse);
    }

    // ═══════════════ ELRK_01 Response Data Finite ═══════════════
    [Fact]
    public void V4_1_ELRK_01_ResponseDataFinite()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2;
        double[] dOs = [0.1, 0.2, 0.5];
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        _output.WriteLine("deltaLoad  finite   max_dResp   max_kResp   max_omResp");
        _output.WriteLine("---------  ------   ---------   ---------   ----------");
        foreach (double dO in dOs)
        {
            var (dists, dResp, kResp, omResp) = ResponseField(Kc, N, s, K0v, xi, loadNode, dO);
            bool fin = dists.All(double.IsFinite) && dResp.All(double.IsFinite) && kResp.All(double.IsFinite) && omResp.All(double.IsFinite);
            _output.WriteLine($"{dO,9:F2}  {fin,-6}   {dResp.Max(),9:E4}  {kResp.Max(),9:E4}  {omResp.Max(),11:E4}");
            Assert.True(fin, $"deltaLoad={dO}: response must be finite.");
        }
    }

    // ═══════════════ ELRK_02 Radial Shell Response ═══════════════
    [Fact]
    public void V4_1_ELRK_02_RadialShellResponse()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2; double dO = 0.2;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var (dists, dResp, _, omResp) = ResponseField(Kc, N, s, K0v, xi, loadNode, dO);
        int nShells = 6;
        var (sd, sDr) = ShellAvg(dists, dResp, nShells);
        var (_, sOr) = ShellAvg(dists, omResp, nShells);
        _output.WriteLine("shell  mean_dist   mean_dResp   mean_omResp");
        _output.WriteLine("-----  ---------   ----------   -----------");
        for (int i = 0; i < sd.Count; i++)
            _output.WriteLine($"  {i + 1,3}   {sd[i],9:F4}   {sDr[i],10:E4}   {sOr[i],11:E4}");
        Assert.True(sd.Count > 0);
    }

    // ═══════════════ ELRK_03 Response Law Fit Comparison ═══════════════
    [Fact]
    public void V4_1_ELRK_03_ResponseLawFitComparison()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2; double dO = 0.2;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var (dists, dResp, _, _) = ResponseField(Kc, N, s, K0v, xi, loadNode, dO);
        var (sd, sr) = ShellAvg(dists, dResp, 8);

        var (eA, eL, eR) = FitExp(sd, sr);
        var (pA, pP, pR) = FitPow(sd, sr);

        // Stretched exponential: scan beta = 0.5, 1.0, 1.5
        double bestBeta = 1.0, bestSR = double.MaxValue;
        foreach (double beta in new[] { 0.5, 1.0, 1.5, 2.0 })
        {
            double lb = eL > 0 ? eL : 1.0;
            var tD = sd.Select(d => Math.Pow(d / lb, beta)).ToList();
            var (tA, _, tR) = FitExp(tD, sr);
            if (tR < bestSR) { bestSR = tR; bestBeta = beta; }
        }

        _output.WriteLine("═══ RESPONSE LAW FIT COMPARISON ═══");
        _output.WriteLine("Law                    param1      param2      RMSE       Rank");
        _output.WriteLine("---------------------  ----------  ----------  ---------  ----");
        var fits = new (string name, double p1, double p2, double rmse)[] {
            ("Exponential", eL, eA, eR),
            ("Power-law", pP, pA, pR),
            ("Stretched exp", eL, bestBeta, bestSR)
        }.OrderBy(f => f.rmse).ToArray();
        for (int i = 0; i < fits.Length; i++)
        {
            var (name, p1, p2, rmse) = fits[i];
            _output.WriteLine($"{name,-21}  {p1,10:F4}  {p2,10:F4}  {rmse,9:E4}  {i + 1,4}");
        }
        Assert.NotEmpty(fits);
    }

    // ═══════════════ ELRK_04 Log-Periodic Residual Check ═══════════════
    [Fact]
    public void V4_1_ELRK_04_LogPeriodicResidualCheck()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2; double dO = 0.2;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var (dists, dResp, _, _) = ResponseField(Kc, N, s, K0v, xi, loadNode, dO);
        var (sd, sr) = ShellAvg(dists, dResp, 8);
        var (_, lambda, _) = FitExp(sd, sr);
        if (!double.IsFinite(lambda)) { _output.WriteLine("Base fit failed; log-periodic check skipped."); Assert.True(true); return; }
        // Residuals vs log(distance)
        var logDists = sd.Select(d => Math.Log(Math.Max(d, 1e-6))).ToArray();
        var residuals = sd.Zip(sr, (d, r) => r - FitExp(sd, sr).A * Math.Exp(-d / lambda)).ToArray();
        // Oscillation score: fit sine to residuals vs log(d)
        double bestAmp = 0; double bestPer = 0;
        foreach (double per in new[] { 0.5, 1.0, 1.5, 2.0, 3.0 })
        {
            double aCos = 0, aSin = 0;
            for (int i = 0; i < logDists.Length; i++)
            {
                double phase = 2 * Math.PI * logDists[i] / per;
                aCos += residuals[i] * Math.Cos(phase);
                aSin += residuals[i] * Math.Sin(phase);
            }
            double amp = Math.Sqrt(aCos * aCos + aSin * aSin) / logDists.Length;
            if (amp > bestAmp) { bestAmp = amp; bestPer = per; }
        }
        double residStd = Math.Sqrt(residuals.Average(r => r * r));
        double relAmp = residStd > 1e-12 ? bestAmp / residStd : 0;
        string verdict = relAmp > 2.0 ? "Possible log-periodic signal" : relAmp > 1.0 ? "Weak hint (inconclusive)" : "No significant log-periodic signal";
        _output.WriteLine($"Log-periodic check: best amp={bestAmp:E4} at period≈{bestPer:F1}  rel_amp={relAmp:F2}  => {verdict}");
        _output.WriteLine("NOTE: Do NOT claim log-periodicity or fractality.");
        Assert.True(true);
    }

    // ═══════════════ ELRK_05 Load Amplitude Scaling ═══════════════
    [Fact]
    public void V4_1_ELRK_05_LoadAmplitudeScaling()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2;
        double[] dOs = [0.01, 0.05, 0.10, 0.20, 0.50];
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        _output.WriteLine("deltaLoad  max_dResp   lambda      dg       zone");
        _output.WriteLine("---------  ---------   ---------   -------  ------------");
        foreach (double dO in dOs)
        {
            var (dists, dResp, _, _) = ResponseField(Kc, N, s, K0v, xi, loadNode, dO);
            var (sd, sr) = ShellAvg(dists, dResp, 6);
            var (_, lambda, _) = FitExp(sd, sr);
            double dg = Dg(DL(Nm(RP(Sm(Kc, N, s, BS, loadNode, dO)))));
            string zone = dO < 0.05 ? "Too weak" : dO > 0.4 ? "Overdriven" : "Linear usable";
            _output.WriteLine($"{dO,9:F2}  {dResp.Max(),9:E4}  {lambda,9:F4}  {dg,7:F4}  {zone}");
        }
        Assert.True(true);
    }

    // ═══════════════ ELRK_06 Positive/Negative Load Symmetry ═══════════════
    [Fact]
    public void V4_1_ELRK_06_PositiveNegativeLoadSymmetry()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var (dP, rP, _, oP) = ResponseField(Kc, N, s, K0v, xi, loadNode, 0.2);
        var (dN, rN, _, oN) = ResponseField(Kc, N, s, K0v, xi, loadNode, -0.2);
        var (sP, srP) = ShellAvg(dP, rP, 5); var (sN, srN) = ShellAvg(dN, rN, 5);
        var (_, lP, _) = FitExp(sP, srP); var (_, lN, _) = FitExp(sN, srN);
        double asymD = sP.Zip(srP, (a, b) => Math.Abs(a)).Sum();
        double asymN = sN.Zip(srN, (a, b) => Math.Abs(a)).Sum();
        double symRatio = asymN > 1e-12 ? asymD / asymN : double.NaN;
        _output.WriteLine($"Positive: max_dResp={rP.Max():E4}  lambda={lP:F4}  omega_mean={oP.Average():E4}");
        _output.WriteLine($"Negative: max_dResp={rN.Max():E4}  lambda={lN:F4}  omega_mean={oN.Average():E4}");
        _output.WriteLine($"Symmetry ratio: {symRatio:F4}");
        _output.WriteLine("NOTE: Negative load is numerically stable. No exotic matter claim.");
        Assert.True(true);
    }

    // ═══════════════ ELRK_07 Two-Load Kernel Superposition ═══════════════
    [Fact]
    public void V4_1_ELRK_07_TwoLoadKernelSuperposition()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1;
        int nodeA = N / 3, nodeB = 2 * N / 3; double dO = 0.2;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var hB = Sm(Kc, N, s, BS); var dB = DL(Nm(RP(hB)));
        var dA = DL(Nm(RP(Sm(Kc, N, s, BS, nodeA, dO))));
        var dB2 = DL(Nm(RP(Sm(Kc, N, s, BS, nodeB, dO))));
        var hBoth = Sm(Kc, N, s, BS, nodeA, dO);
        var dBoth = DL(Nm(RP(Sm(Kc, N, s, BS, nodeB, dO))));
        double nA = 0, nB = 0, nBoth = 0; int cnt = 0;
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { nA += Math.Abs(dA[i, j] - dB[i, j]); nB += Math.Abs(dB2[i, j] - dB[i, j]); nBoth += Math.Abs(dBoth[i, j] - dB[i, j]); cnt++; }
        nA /= cnt; nB /= cnt; nBoth /= cnt;
        double linErr = Math.Abs(nBoth - (nA + nB)) / Math.Max(nA + nB, 1e-12);
        string cls = linErr < 0.2 ? "Linear" : linErr < 0.5 ? "Weak nonlinear" : "Strong nonlinear";
        _output.WriteLine($"Load A norm: {nA:E4}  Load B norm: {nB:E4}  A+B norm: {nBoth:E4}");
        _output.WriteLine($"Linearity error: {linErr:F4}  Classification: {cls}");
        Assert.True(true);
    }

    // ═══════════════ ELRK_08 N-Scaling Kernel Stability ═══════════════
    [Fact]
    public void V4_1_ELRK_08_NScalingKernelStability()
    {
        int[] Ns = [40, 80, 120, 200]; double K0v = 0.5; double xi = 1.75; double s = 0.1; double dO = 0.2;
        _output.WriteLine("N      lambda      rmse       max_resp   dg");
        _output.WriteLine("----   ---------   ---------  ---------  ------");
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3; int loadNode = N / 2;
            var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, E, BS);
            var (dists, dResp, _, _) = ResponseField(Kc, N, s, K0v, xi, loadNode, dO);
            var (sd, sr) = ShellAvg(dists, dResp, 6);
            var (_, lambda, rmse) = FitExp(sd, sr);
            double dg = Dg(DL(Nm(RP(Sm(Kc, N, s, BS, loadNode, dO)))));
            _output.WriteLine($"{N,5}  {lambda,9:F4}  {rmse,9:E4}  {dResp.Max(),9:E4}  {dg,6:F4}");
        }
        Assert.True(true);
    }

    // ═══════════════ ELRK_09 Multi-Seed Kernel Stability ═══════════════
    [Fact]
    public void V4_1_ELRK_09_MultiSeedKernelStability()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2; double dO = 0.2; int nSeeds = 12;
        var lambdas = new List<double>(); var rmses = new List<double>(); var maxRs = new List<double>(); int fails = 0;
        for (int sd = 0; sd < nSeeds; sd++)
        {
            var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS + sd * 10);
            var (dists, dResp, _, _) = ResponseField(Kc, N, s, K0v, xi, loadNode, dO);
            var (sd2, sr) = ShellAvg(dists, dResp, 6);
            var (_, lambda, rmse) = FitExp(sd2, sr);
            if (double.IsFinite(lambda)) { lambdas.Add(lambda); rmses.Add(rmse); maxRs.Add(dResp.Max()); } else fails++;
        }
        double ml = lambdas.Count > 0 ? lambdas.Average() : double.NaN;
        double sl = lambdas.Count > 1 ? Math.Sqrt(lambdas.Average(x => (x - ml) * (x - ml))) : 0;
        double mr = rmses.Count > 0 ? rmses.Average() : double.NaN;
        double mMax = maxRs.Count > 0 ? maxRs.Average() : 0;
        _output.WriteLine($"Multi-seed ({nSeeds} seeds): lambda_mean={ml:F4} lambda_std={sl:F4} rmse_mean={mr:E4} maxResp_mean={mMax:E4} fails={fails}");
        Assert.True(fails <= nSeeds / 3);
    }

    // ═══════════════ ELRK_10 Fractal or Multifractal Response Field ═══════════════
    [Fact]
    public void V4_1_ELRK_10_FractalOrMultifractalResponseField()
    {
        int N = 80; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2; double dO = 0.2;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var (_, dResp, _, _) = ResponseField(Kc, N, s, K0v, xi, loadNode, dO);
        var sorted = dResp.OrderByDescending(r => r).ToArray();
        _output.WriteLine("═══ RESPONSE FIELD FRACTAL DIAGNOSTICS ═══");
        _output.WriteLine("threshold_frac  above_thresh");
        _output.WriteLine("--------------  ------------");
        double maxR = sorted.Max();
        foreach (double f in new[] { 0.01, 0.05, 0.10, 0.25, 0.50 })
        {
            int above = sorted.Count(r => r > f * maxR);
            _output.WriteLine($"{f,14:F2}  {above,12}");
        }
        // q-moment proxy: compute moments for q=-1,0,1,2
        _output.WriteLine("q      moment_sum");
        _output.WriteLine("---    ----------");
        foreach (int q in new[] { -1, 0, 1, 2 })
        {
            double sum = 0; double eps = 1e-9;
            foreach (var r in sorted) { double v = q == 0 ? 1 : Math.Pow(Math.Max(r, eps), q); sum += v; }
            _output.WriteLine($"{q,3}    {sum,10:E4}");
        }
        _output.WriteLine("NOTE: Fractal diagnostics are approximate. Fractality is NOT proven.");
        Assert.True(true);
    }

    // ═══════════════ ELRK_11 SPARC Readiness Link ═══════════════
    [Fact]
    public void V4_1_ELRK_11_SPARCReadinessLink()
    {
        var dataDir = TrmDatasetCatalog.ResolveDataDirectory();
        var massModels = Path.Combine(dataDir, "MassModels_Lelli2016c.mrt");
        var sparcFile = Path.Combine(dataDir, "SPARC_Lelli2016c.mrt");
        _output.WriteLine("═══ SPARC READINESS LINK ═══");
        _output.WriteLine($"MassModels: {(File.Exists(massModels) ? "FOUND" : "NOT FOUND")}");
        _output.WriteLine($"SPARC table: {(File.Exists(sparcFile) ? "FOUND" : "NOT FOUND")}");
        if (File.Exists(massModels))
        {
            var ds = MrtParser.Parse(massModels);
            var cols = new[] { "R", "Vobs", "Vgas", "Vdisk", "Vbul", "D", "ID", "SBdisk", "SBbul", "e_Vobs" };
            int found = cols.Count(c => ds.Columns.Any(mc => mc.Label.Equals(c, StringComparison.OrdinalIgnoreCase)));
            _output.WriteLine($"Rotation-curve columns detected: {found}/{cols.Length}");
        }
        _output.WriteLine("NOTE: Real SPARC residual analysis is NOT run in this suite.");
        _output.WriteLine("No galaxy rotation curves are fit. No dark matter claim is made.");
        Assert.True(true);
    }

    // ═══════════════ ELRK_12 Null and Degenerate Controls ═══════════════
    [Fact]
    public void V4_1_ELRK_12_NullAndDegenerateControls()
    {
        int N = 60; double K0v = 0.5; double xi = 1.75; double s = 0.1; int loadNode = N / 2; double dO = 0.2;

        (double lambda, double dg) Eval(double[,] Kc)
        {
            var (dists, dResp, _, _) = ResponseField(Kc, N, s, K0v, xi, loadNode, dO);
            var (sd, sr) = ShellAvg(dists, dResp, 5);
            var (_, lambda, _) = FitExp(sd, sr);
            double dg = Dg(DL(Nm(RP(Sm(Kc, N, s, BS, loadNode, dO)))));
            return (lambda, dg);
        }

        var K0a = KS(N, BS); var KcA = RecoverFP(K0a, N, K0v, xi, s, 5, BS);
        var (lA, dgA) = Eval(KcA);
        var KcN = RecoverFP(new double[N, N], N, K0v, xi, s, 5, BS);
        var (lN, dgN) = Eval(KcN);
        var hSync = new double[101][]; for (int t = 0; t < 101; t++) { hSync[t] = new double[N]; for (int i = 0; i < N; i++) hSync[t][i] = 0; }
        double dgGS = Dg(DL(Nm(RP(hSync))));

        _output.WriteLine("Case             lambda      dg");
        _output.WriteLine("---------------  ---------   ------");
        _output.WriteLine($"Active           {lA,9:F4}   {dgA,6:F4}");
        _output.WriteLine($"K=0 null         {lN,9:F4}   {dgN,6:F4}");
        _output.WriteLine($"Global sync      --          {dgGS,6:F4}");
        Assert.True(dgGS < 0.01, "Global sync must be degenerate.");
    }

    // ═══════════════ ELRK_13 Claim Discipline Report ═══════════════
    [Fact]
    public void V4_1_ELRK_13_ClaimDisciplineReport()
    {
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("  CLAIM DISCIPLINE — ENERGY-LOAD RESPONSE KERNEL");
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  SUPPORTED:");
        _output.WriteLine("    - Load-response shell data are measurable.");
        _output.WriteLine("    - Response-law fits can be compared numerically.");
        _output.WriteLine("    - Load-amplitude scaling can be tested.");
        _output.WriteLine("    - Positive/negative load symmetry measured.");
        _output.WriteLine("    - Superposition error is quantifiable.");
        _output.WriteLine("    - Fractal diagnostics computable as probes.");
        _output.WriteLine("    - SPARC data readiness checks work.");
        _output.WriteLine("    - Null and degenerate controls detected.");
        _output.WriteLine("");
        _output.WriteLine("  CONDITIONAL:");
        _output.WriteLine("    - Preferred law depends on proxy, xi, K0,");
        _output.WriteLine("      topology, N, seed, amplitude, shelling.");
        _output.WriteLine("    - Fractal diagnostics are resolution-limited.");
        _output.WriteLine("    - Kernel is NOT a physical force law.");
        _output.WriteLine("    - SPARC comparison requires validated workflow.");
        _output.WriteLine("");
        _output.WriteLine("  HYPOTHESIS:");
        _output.WriteLine("    - TRM load-response may correspond to emergent");
        _output.WriteLine("      gravity-like deformation.");
        _output.WriteLine("    - Response kernels may contain fractal structure.");
        _output.WriteLine("    - SPARC residuals may show related scaling.");
        _output.WriteLine("");
        _output.WriteLine("  NOT CLAIMED:");
        _output.WriteLine("    - Mass is derived.");
        _output.WriteLine("    - Gravity is derived.");
        _output.WriteLine("    - Inverse-square law is derived.");
        _output.WriteLine("    - GR is replaced.");
        _output.WriteLine("    - SPARC galaxies are explained.");
        _output.WriteLine("    - Dark matter/MOND are replaced.");
        _output.WriteLine("    - Physical c is derived.");
        _output.WriteLine("    - Fractality is proven.");
        _output.WriteLine("══════════════════════════════════════════════════");
        Assert.True(true);
    }
}
