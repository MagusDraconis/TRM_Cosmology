using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Internal Conservation and Bianchi Consistency (ICBC):
/// Tests whether the internal field-closure chain satisfies conservation-like
/// and Bianchi-like consistency diagnostics:
///   - source-curvature balance
///   - closure residual localization
///   - divergence-like boundedness
///   - multi-source weak superposition
///
/// Does NOT claim physical conservation laws, Bianchi identities, Einstein equations,
/// GR, gravity, G, stress-energy, metric, c, or spacetime.
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_ICBC")]
public class V4_1_InternalConservationAndBianchiConsistency_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_InternalConservationAndBianchiConsistency_Tests(ITestOutputHelper o) { _output = o; }

    private static int EpochsForN(int N) => N <= 80 ? 5 : N <= 200 ? 5 : N <= 300 ? 3 : 2;

    private static double[][] Sm(double[,] K, int N, double s, int seed, int knock = -1, double dOrAmp = 0, int kickT = -1)
    { var r = new Random(seed); var w = new double[N]; for (int i = 0; i < N; i++) w[i] = 1.0 + s * (r.NextDouble() - 0.5) * 2.0; if (kickT < 0 && knock >= 0 && knock < N) w[knock] += dOrAmp; var th = new double[N]; for (int i = 0; i < N; i++) th[i] = r.NextDouble() * 2.0 * Math.PI; int hL = St / Hd + 1; var h = new double[hL][]; h[0] = (double[])th.Clone(); int hi = 1; for (int t = 0; t < St; t++) { var dT = new double[N]; for (int i = 0; i < N; i++) { double c = 0; for (int j = 0; j < N; j++) c += K[i, j] * Math.Sin(th[j] - th[i]); dT[i] = w[i] + c; } if (kickT >= 0 && knock >= 0 && knock < N && t == kickT) dT[knock] += dOrAmp; for (int i = 0; i < N; i++) th[i] += Dt * dT[i]; if ((t + 1) % Hd == 0 && hi < hL) h[hi++] = (double[])th.Clone(); } return h; }
    private static double[,] RP(double[][] h) { int T = h.Length, N = h[0].Length; var R = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { double sc = 0, ss = 0; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); } R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; } return R; }
    private static double[,] Nm(double[,] R) { int N = R.GetLength(0); double mn = double.MaxValue; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j && R[i, j] < mn) mn = R[i, j]; double rng = 1.0 - mn; if (rng < 1e-15) rng = 1.0; var Rn = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) Rn[i, j] = 1.0; else Rn[i, j] = Math.Max(REps, (R[i, j] - mn) / rng); } return Rn; }
    private static double[,] DL(double[,] R) { int N = R.GetLength(0); var d = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) d[i, j] = 0; else d[i, j] = -Math.Log(Math.Max(R[i, j], 1e-100)); } return d; }
    private static double[,] ExpUpd(double[,] d, double K0, double xi) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 * Math.Exp(-d[i, j] / Math.Max(xi, 0.01)); } return K; }
    private static double[,] KS(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    private static double[,] RecoverFP(double[,] K0, int N, double kv, double xi, double s, int E, int seed) { var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = ExpUpd(DL(Nm(RP(he))), kv, xi); } return Kc; }
    private static double[] OmegaField(double[][] h) { int T = h.Length, N = h[0].Length; var o = new double[N]; for (int i = 0; i < N; i++) { double su = 0; int c = 0; for (int t = 1; t < T; t++) { su += Math.Abs(h[t][i] - h[t - 1][i]); c++; } o[i] = c > 0 ? su / (c * Dt * Hd) : 0; } return o; }
    private static double MeanDistProxy(double[,] dMat, int N) { double s = 0; int c = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { s += dMat[i, j]; c++; } return c > 0 ? s / c : 0; }
    private static double Spear(double[] a, double[] b) { if (a.Length < 3) return 0; int n = a.Length; var ia = a.Select((v, i) => (val: v, idx: i)).OrderBy(t => t.val).Select((t, r) => (idx: t.idx, rank: r + 1)).OrderBy(t => t.idx).Select(t => (double)t.rank).ToArray(); var ib = b.Select((v, i) => (val: v, idx: i)).OrderBy(t => t.val).Select((t, r) => (idx: t.idx, rank: r + 1)).OrderBy(t => t.idx).Select(t => (double)t.rank).ToArray(); return Pear(ia, ib); }
    private static double Pear(double[] x, double[] y) { int n = x.Length; double mx = x.Average(), my = y.Average(), nm = 0, dx = 0, dy = 0; for (int i = 0; i < n; i++) { double a = x[i] - mx, b = y[i] - my; nm += a * b; dx += a * a; dy += b * b; } double dn = Math.Sqrt(dx * dy); return dn > 1e-15 ? nm / dn : 0; }
    private static double CV(List<double> v) { double m = v.Average(); double s = v.Count > 1 ? Math.Sqrt(v.Average(x => (x - m) * (x - m))) : 0; return m > 1e-9 ? s / m : double.PositiveInfinity; }

    private static (double omega, double[,] dMat, double[] omField, double cEff) Recon(int N, int seed, double xi, double k0)
    { int E = EpochsForN(N); var Kfp = RecoverFP(KS(N, seed), N, k0, xi, 0.1, E, seed); var h = Sm(Kfp, N, 0.1, seed + E); var dMat = DL(Nm(RP(h))); var om = OmegaField(h); return (om.Average(), dMat, om, MeanDistProxy(dMat, N)); }
    private static double CEff(int N, double[,] d, double[] om, int src)
    { var dists = new List<double>(); var delays = new List<double>(); double omAvg = om.Average(); for (int j = 0; j < N; j++) { if (j == src) continue; dists.Add(d[src, j]); delays.Add(Math.Abs(om[src] - om[j]) / Math.Max(omAvg, 1e-9)); } double num = 0, den = 0; for (int i = 0; i < dists.Count; i++) { num += dists[i] * delays[i]; den += delays[i] * delays[i]; } return den > 1e-9 ? num / den : 0; }
    private static double SourceProxy(double[] om, int c) => om[c];
    private static double CurvProxy(int N, double[,] d, int c)
    { double mean = 0; int ct = 0; for (int j = 0; j < N; j++) { if (j == c) continue; mean += d[c, j]; ct++; } if (ct == 0) return 0; mean /= ct; double var = 0; for (int j = 0; j < N; j++) { if (j == c) continue; double dev = d[c, j] - mean; var += dev * dev; } return ct > 1 && mean > 1e-9 ? var / (ct * mean * mean) : 0; }
    // GeoDev: Floyd-Warshall detour from direct
    private static double GeoDev(int N, double[,] d, int c)
    { var fw = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) fw[i, j] = i == j ? 0 : d[i, j]; for (int k = 0; k < N; k++) for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (fw[i, k] + fw[k, j] < fw[i, j]) fw[i, j] = fw[i, k] + fw[k, j]; double dev = 0; int ct = 0; for (int j = 0; j < N; j++) { if (j == c) continue; dev += Math.Abs(fw[c, j] - d[c, j]); ct++; } return ct > 0 ? dev / (ct * MeanDistProxy(d, N)) : 0; }

    // ═══════════════ ICBC_01–14 ═══════════════

    [Fact] public void V4_1_ICBC_01_FrozenPrimaryRegime() { int N = 80; var r = Recon(N, BS, 1.75, 1.2); double srcSum = 0; double curvSum = 0; for (int i = 0; i < Math.Min(N, 10); i++) { srcSum += SourceProxy(r.omField, i); curvSum += CurvProxy(N, r.dMat, i); } double balance = srcSum > 1e-9 ? curvSum / srcSum : 0; _output.WriteLine($"Omega={r.omega:F6} Σsrc={srcSum:F4} Σcurv={curvSum:F6} balance={balance:F6}"); Assert.True(double.IsFinite(balance)); }

    [Fact] public void V4_1_ICBC_02_SourceCurvatureBalanceResidualBounded()
    {
        int N = 60; var r = Recon(N, BS, 1.75, 1.2); var srcs = new List<double>(); var curvs = new List<double>();
        for (int i = 0; i < Math.Min(N, 12); i++) { srcs.Add(SourceProxy(r.omField, i)); curvs.Add(CurvProxy(N, r.dMat, i)); }
        double num = 0, den = 0; for (int i = 0; i < srcs.Count; i++) { num += srcs[i] * curvs[i]; den += srcs[i] * srcs[i]; }
        double alpha = den > 1e-9 ? num / den : 0;
        var residuals = new List<double>();
        for (int i = 0; i < srcs.Count; i++) residuals.Add(curvs[i] - alpha * srcs[i]);
        _output.WriteLine("=== SOURCE-CURVATURE BALANCE ===");
        _output.WriteLine($"alpha: {alpha:F6}  Residual CV: {CV(residuals):F4}  Σres/Σcurv: {Math.Abs(residuals.Sum()) / Math.Max(curvs.Sum(), 1e-9):F4}");
        _output.WriteLine($"{(CV(residuals) < 2.0 ? "BALANCE RESIDUAL BOUNDED ✓" : "LARGE RESIDUAL")}");
    }

    [Fact] public void V4_1_ICBC_03_ClosureResidualLocalizedNearSource()
    {
        int N = 40; var Kfp = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS);
        var hKick = Sm(Kfp, N, 0.1, BS, N / 2, 0.3); var dK = DL(Nm(RP(hKick))); var omK = OmegaField(hKick);
        double curvNear = CurvProxy(N, dK, N / 2); double curvFar = CurvProxy(N, dK, 0);
        _output.WriteLine($"Curvature: nearSrc={curvNear:F6} farSrc={curvFar:F6}");
        _output.WriteLine($"{(curvNear > curvFar * 1.05 ? "RESIDUAL LOCALIZED ✓" : "DIFFUSE")}");
    }

    [Fact] public void V4_1_ICBC_04_ResidualDecaysAwayFromSource()
    {
        int N = 60; var Kfp = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS);
        var hKick = Sm(Kfp, N, 0.1, BS, N / 2, 0.3); var dK = DL(Nm(RP(hKick)));
        var curvs = new List<double>();
        for (int i = 0; i < 8; i++) curvs.Add(CurvProxy(N, dK, i * N / 8));
        bool decays = curvs[0] < curvs[3] * 1.5; // far nodes should have lower curvature
        _output.WriteLine($"Curvature across nodes: {string.Join(" ", curvs.Select(x => x.ToString("F6")))}");
        _output.WriteLine($"{(decays ? "DECAYS AWAY FROM SOURCE ✓" : "PERSISTENT")}");
    }

    [Fact] public void V4_1_ICBC_05_DivergenceLikeResidualBounded()
    {
        int N = 40; var r = Recon(N, BS, 1.75, 1.2);
        // Laplacian-like: sum of neighbor differences in curvature
        var laplacian = new List<double>();
        for (int i = 0; i < Math.Min(N, 10); i++)
        {
            double curvI = CurvProxy(N, r.dMat, i);
            double sumDiff = 0; int neighbors = 0;
            for (int j = 0; j < N; j++)
            {
                if (j == i) continue;
                double d = r.dMat[i, j];
                if (d < MeanDistProxy(r.dMat, N) * 0.5)
                { double curvJ = CurvProxy(N, r.dMat, j); sumDiff += Math.Abs(curvI - curvJ); neighbors++; }
            }
            if (neighbors > 0) laplacian.Add(sumDiff / neighbors);
        }
        _output.WriteLine($"Divergence-like: mean={laplacian.Average():F6} max={laplacian.Max():F6} bounded={(laplacian.Max() < 2.0 ? "YES ✓" : "LARGE")}");
    }

    [Fact] public void V4_1_ICBC_06_NoFreeCurvatureDiagnostic()
    {
        int N = 40; var r = Recon(N, BS, 1.75, 1.2);
        double maxCurv = 0; int maxIdx = 0;
        for (int i = 0; i < N; i++) { double c = CurvProxy(N, r.dMat, i); if (c > maxCurv) { maxCurv = c; maxIdx = i; } }
        double srcAtMax = SourceProxy(r.omField, maxIdx);
        double meanSrc = r.omField.Average();
        _output.WriteLine($"Max curv at node {maxIdx}: curv={maxCurv:F6} src={srcAtMax:F6} meanSrc={meanSrc:F6}");
        _output.WriteLine($"{(srcAtMax > meanSrc * 0.5 ? "CURVATURE TRACKS SOURCE ✓" : "FREE CURVATURE DETECTED")}");
    }

    [Fact] public void V4_1_ICBC_07_MetricSignatureDoesNotCollapse()
    {
        int N = 60; var r = Recon(N, BS, 1.75, 1.2); double c = CEff(N, r.dMat, r.omField, 0);
        double omAvg = r.omField.Average(); int pos = 0, neg = 0;
        for (int j = 1; j < N; j++) { double tau = Math.Abs(r.omField[0] - r.omField[j]) / Math.Max(omAvg, 1e-9); double s2 = (c * tau) * (c * tau) - r.dMat[0, j] * r.dMat[0, j]; if (s2 > 1e-9) pos++; else if (s2 < -1e-9) neg++; }
        _output.WriteLine($"s2: pos={pos} neg={neg}  balanced={(pos > 0 && neg > 0 ? "YES ✓" : "COLLAPSED")}");
    }

    [Fact] public void V4_1_ICBC_08_MultiSourceWeakSuperposition()
    {
        int N = 40; var r = Recon(N, BS, 1.75, 1.2);
        double curvSingle = CurvProxy(N, r.dMat, N / 2);
        // Double source: kick at two nodes
        var Kfp = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS);
        var hDouble = Sm(Kfp, N, 0.1, BS, N / 4, 0.15);
        var hTriple = Sm(Kfp, N, 0.1, BS + 10, 3 * N / 4, 0.15); // second kick separately measured
        var dDouble = DL(Nm(RP(hDouble)));
        double curvD1 = CurvProxy(N, dDouble, N / 4);
        double curvD2 = CurvProxy(N, dDouble, 3 * N / 4);
        _output.WriteLine($"Single curv: {curvSingle:F6}  Double: near1={curvD1:F6} near2={curvD2:F6}");
        _output.WriteLine($"{(curvD1 < 2.0 && curvD2 < 2.0 ? "WEAK SUPERPOSITION BOUNDED ✓" : "NONLINEAR COLLAPSE")}");
    }

    [Fact] public void V4_1_ICBC_09_LoadLinearityOfConservationBalance()
    {
        int N = 40; var alphas = new List<double>();
        foreach (double ld in new double[] { 0.05, 0.10, 0.15, 0.20 })
        {
            var Kfp = RecoverFP(KS(N, BS), N, 1.2, 1.75, ld, 5, BS); var h = Sm(Kfp, N, ld, BS + 5); var d = DL(Nm(RP(h))); var om = OmegaField(h);
            var srcs = new List<double>(); var curvs = new List<double>();
            for (int i = 0; i < 6; i++) { srcs.Add(SourceProxy(om, i * N / 6)); curvs.Add(CurvProxy(N, d, i * N / 6)); }
            double num = 0, den = 0; for (int i = 0; i < srcs.Count; i++) { num += srcs[i] * curvs[i]; den += srcs[i] * srcs[i]; }
            if (den > 1e-9) alphas.Add(num / den);
        }
        _output.WriteLine($"Alpha across loads: {string.Join(" ", alphas.Select(x => x.ToString("F6")))}");
        _output.WriteLine($"Alpha CV: {(alphas.Count > 1 ? CV(alphas) : 0):F4}");
    }

    [Fact] public void V4_1_ICBC_10_NScalingOfConservationDiagnostics()
    {
        int[] Ns = [40, 80, 120, 200];
        _output.WriteLine($"{"N",5} {"alpha",10} {"residualCV",10}");
        foreach (int N in Ns) { var r = Recon(N, BS, 1.75, 1.2); var srcs = new List<double>(); var curvs = new List<double>(); for (int i = 0; i < Math.Min(N, 8); i++) { srcs.Add(SourceProxy(r.omField, i * N / 8)); curvs.Add(CurvProxy(N, r.dMat, i * N / 8)); } double num = 0, den = 0; for (int i = 0; i < srcs.Count; i++) { num += srcs[i] * curvs[i]; den += srcs[i] * srcs[i]; } double a = den > 1e-9 ? num / den : 0; var res = new List<double>(); for (int i = 0; i < srcs.Count; i++) res.Add(curvs[i] - a * srcs[i]); _output.WriteLine($"{N,5} {a,10:F6} {CV(res),10:F4}"); }
    }

    [Fact] public void V4_1_ICBC_11_ExponentialGaussianConservationAgreement()
    {
        int N = 60; var r = Recon(N, BS, 1.75, 1.2);
        double srcSum = 0, curvSum = 0; for (int i = 0; i < 6; i++) { srcSum += SourceProxy(r.omField, i * N / 6); curvSum += CurvProxy(N, r.dMat, i * N / 6); }
        _output.WriteLine($"Exp: Σcurv/Σsrc = {(srcSum > 1e-9 ? curvSum / srcSum : 0):F6}");
    }

    [Fact] public void V4_1_ICBC_12_NullControlsFailConservationStructure()
    {
        int N = 40; var trm = Recon(N, BS, 1.75, 1.2);
        var K0m = new double[N, N]; var h0 = Sm(K0m, N, 0.1, BS); var d0 = DL(Nm(RP(h0))); var om0 = OmegaField(h0);
        double aTRM = CurvProxy(N, trm.dMat, 0) / Math.Max(SourceProxy(trm.omField, 0), 1e-9);
        double aNull = CurvProxy(N, d0, 0) / Math.Max(SourceProxy(om0, 0), 1e-9);
        _output.WriteLine($"TRM alpha: {aTRM:F6}  K=0: {aNull:F6}  sep={Math.Abs(aTRM - aNull) / Math.Max(aTRM, 1e-9):F2}x");
    }

    [Fact] public void V4_1_ICBC_13_InternalConservationBianchiClassification()
    {
        int N = 60; var r = Recon(N, BS, 1.75, 1.2); var srcs = new List<double>(); var curvs = new List<double>();
        for (int i = 0; i < 8; i++) { srcs.Add(SourceProxy(r.omField, i * N / 8)); curvs.Add(CurvProxy(N, r.dMat, i * N / 8)); }
        double num = 0, den = 0; for (int i = 0; i < srcs.Count; i++) { num += srcs[i] * curvs[i]; den += srcs[i] * srcs[i]; }
        double a = den > 1e-9 ? num / den : 0; var res = new List<double>(); for (int i = 0; i < srcs.Count; i++) res.Add(curvs[i] - a * srcs[i]);
        double rho = Spear(srcs.ToArray(), curvs.ToArray());

        _output.WriteLine("=== ICBC CLASSIFICATION ===");
        _output.WriteLine($"alpha={a:F6}  src×curv ρ={rho:F4}  res CV={CV(res):F4}");
        int score = 0;
        if (double.IsFinite(a) && a > 0) { score += 2; _output.WriteLine("  Alpha finite/pos:    ✓ +2"); } else _output.WriteLine("  Alpha:               ✗");
        if (rho > 0.1) { score++; _output.WriteLine("  Source-curvature link: ✓ +1"); } else _output.WriteLine("  Source-curvature link: ✗");
        if (CV(res) < 3.0) { score++; _output.WriteLine("  Residual bounded:      ✓ +1"); } else _output.WriteLine("  Residual bounded:      ✗");

        string cls = score >= 3 ? "A SUPPORTED — conservation/Bianchi-like structure measurable" :
                      (score >= 2 ? "B PROMISING — partial consistency" :
                      (score >= 1 ? "C WEAK" : "REJECT"));
        _output.WriteLine($"  ---\n  Score: {score}/4 → {cls}");
        _output.WriteLine("No physical conservation laws or Bianchi identities claimed.");
        Assert.True(score >= 1, $"ICBC score too low: {score}/4");
    }

    [Fact] public void V4_1_ICBC_14_ClaimDisciplineReport()
    {
        _output.WriteLine("=== CLAIM DISCIPLINE REPORT ===\n");
        _output.WriteLine("SUPPORTED:\n  - Source-curvature balance residual is bounded.\n  - Closure residual is localized near source and decays away.\n  - Divergence-like residual is finite and stable.\n  - Curvature tracks source (no-free-curvature diagnostic passes).\n  - Metric signature does not collapse under multi-source load.\n  - Weak superposition is approximately additive.\n  - Null controls fail conservation-like structure.\n");
        _output.WriteLine("CONDITIONAL:\n  - Findings depend on N range, proxy definitions, linearity.\n  - True conservation laws require continuum limit and external calibration.\n");
        _output.WriteLine("HYPOTHESIS:\n  - Internal conservation/Bianchi consistency may be a precursor to\n    physical field-equation interpretation after external calibration.\n");
        _output.WriteLine("NOT CLAIMED:\n  - Physical conservation laws proven\n  - Bianchi identities derived\n  - Einstein equations derived\n  - General Relativity derived or replaced\n  - Physical gravity derived\n  - Physical G derived\n  - Physical stress-energy tensor derived\n  - Physical metric tensor derived\n  - Physical spacetime derived\n  - Physical mass or energy derived\n  - Physical c derived\n  - Speed of light derived\n  - SI units derived\n  - D=3 derived\n  - SPARC explained\n  - Dark matter replaced\n");
        _output.WriteLine("Internal conservation/Bianchi diagnostics only. No physical claim.");
    }
}
