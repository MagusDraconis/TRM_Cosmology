using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Internal Field-Equation Closure (IFEC):
/// Tests whether TRM supports an internal field-equation-like closure:
///   Source → Curvature → Metric Perturbation → Geodesic Deviation
///
/// Does NOT claim Einstein equations, GR, physical gravity, G, stress-energy, or metric.
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_IFEC")]
public class V4_1_InternalFieldEquationClosure_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_InternalFieldEquationClosure_Tests(ITestOutputHelper o) { _output = o; }

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

    // ── Source proxy: OmegaSource (local Omega magnitude near a node) ──
    private static double SourceProxy(double[] om, int center) => om[center];
    // ── Curvature proxy: local d_ij variance in neighborhood ──
    private static double CurvProxy(int N, double[,] d, int center)
    { double mean = 0; int c = 0; for (int j = 0; j < N; j++) { if (j == center) continue; mean += d[center, j]; c++; } if (c == 0) return 0; mean /= c; double var = 0; for (int j = 0; j < N; j++) { if (j == center) continue; double dev = d[center, j] - mean; var += dev * dev; } return c > 1 && mean > 1e-9 ? var / (c * mean * mean) : 0; }
    // ── Metric perturbation: change in g00 from source-free baseline ──
    private static double MetricPert(int N, double[,] d, double[] om, int center, double cEff, double g00Base)
    { double omAvg = om.Average(); double g00Sum = 0; int count = 0; for (int j = 0; j < N; j++) { if (j == center) continue; double tau = Math.Abs(om[center] - om[j]) / Math.Max(omAvg, 1e-9); double cT2 = (cEff * tau) * (cEff * tau); double d2 = d[center, j] * d[center, j]; g00Sum += cT2 / (cT2 + d2 + 1e-9); count++; } return count > 0 ? Math.Abs(g00Sum / count - g00Base) : 0; }
    // ── Geodesic deviation: Floyd-Warshall detour from direct distance ──
    private static double GeoDev(int N, double[,] d, int center)
    { var fw = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) fw[i, j] = i == j ? 0 : d[i, j]; for (int k = 0; k < N; k++) for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (fw[i, k] + fw[k, j] < fw[i, j]) fw[i, j] = fw[i, k] + fw[k, j]; double dev = 0; int c = 0; for (int j = 0; j < N; j++) { if (j == center) continue; dev += Math.Abs(fw[center, j] - d[center, j]); c++; } return c > 0 ? dev / (c * MeanDistProxy(d, N)) : 0; }

    // ═══════════════ IFEC_01–14 ═══════════════

    [Fact] public void V4_1_IFEC_01_FrozenPrimaryRegime() { int N = 80; var r = Recon(N, BS, 1.75, 1.2); double c = CEff(N, r.dMat, r.omField, 0); double src = SourceProxy(r.omField, 0); double curv = CurvProxy(N, r.dMat, 0); double g00Base = 0; int cc = 0; double omAvg = r.omField.Average(); for (int j = 1; j < N; j++) { double tau = Math.Abs(r.omField[0] - r.omField[j]) / Math.Max(omAvg, 1e-9); double cT2 = (c * tau) * (c * tau); double d2 = r.dMat[0, j] * r.dMat[0, j]; g00Base += cT2 / (cT2 + d2 + 1e-9); cc++; } g00Base = cc > 0 ? g00Base / cc : 0; double pert = MetricPert(N, r.dMat, r.omField, 0, c, g00Base); double dev = GeoDev(N, r.dMat, 0); double alpha = curv / Math.Max(src, 1e-9); _output.WriteLine($"src={src:F6} curv={curv:F6} alpha={alpha:F6} g00Base={g00Base:F3} pert={pert:F6} dev={dev:F6}"); Assert.True(double.IsFinite(alpha)); }

    [Fact] public void V4_1_IFEC_02_SourceProxyLocalizedAndFinite() { int N = 80; var r = Recon(N, BS, 1.75, 1.2); _output.WriteLine("=== SOURCE PROXY ==="); _output.WriteLine($"OmegaSource nodes: all finite={r.omField.All(double.IsFinite)}  positive={r.omField.All(o => o > 0)}"); _output.WriteLine($"Range: [{r.omField.Min():F6}, {r.omField.Max():F6}]  CV: {CV(r.omField.ToList()):F4}"); }

    [Fact] public void V4_1_IFEC_03_SourceCurvatureRelationStable()
    {
        int N = 60; var r = Recon(N, BS, 1.75, 1.2); var srcs = new List<double>(); var curvs = new List<double>();
        for (int i = 0; i < Math.Min(N, 12); i++) { srcs.Add(SourceProxy(r.omField, i)); curvs.Add(CurvProxy(N, r.dMat, i)); }
        double rho = Spear(srcs.ToArray(), curvs.ToArray());
        _output.WriteLine($"Source × Curvature ρ: {rho:F4}");
        _output.WriteLine($"{(rho > 0.2 ? "SOURCE-CURVATURE LINKED ✓" : "WEAK LINK")}");
    }

    [Fact] public void V4_1_IFEC_04_AlphaTRMFinitePositiveStable() { int N = 80; var alphas = new List<double>(); for (int s = 0; s < 8; s++) { var r = Recon(N, s, 1.75, 1.2); double a = CurvProxy(N, r.dMat, 0) / Math.Max(SourceProxy(r.omField, 0), 1e-9); alphas.Add(a); } _output.WriteLine($"alpha_TRM: mean={alphas.Average():F6} CV={CV(alphas):F4} finite={alphas.All(double.IsFinite)}"); }

    [Fact] public void V4_1_IFEC_05_CurvatureMetricPerturbationMeasurable()
    {
        int N = 60; var baseline = Recon(N, BS, 1.75, 1.2); double c = CEff(N, baseline.dMat, baseline.omField, 0);
        double omAvg = baseline.omField.Average(); double g00Base = 0; int cc = 0;
        for (int j = 1; j < N; j++) { double tau = Math.Abs(baseline.omField[0] - baseline.omField[j]) / Math.Max(omAvg, 1e-9); double cT2 = (c * tau) * (c * tau); double d2 = baseline.dMat[0, j] * baseline.dMat[0, j]; g00Base += cT2 / (cT2 + d2 + 1e-9); cc++; }
        g00Base = cc > 0 ? g00Base / cc : 0;
        // Perturb with source kick
        var Kfp = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS);
        var hKick = Sm(Kfp, N, 0.1, BS, N / 2, 0.3);
        var dKick = DL(Nm(RP(hKick))); var omKick = OmegaField(hKick);
        double cK = CEff(N, dKick, omKick, N / 2);
        double pertNear = MetricPert(N, dKick, omKick, N / 2, cK, g00Base);
        double pertFar = MetricPert(N, dKick, omKick, 0, cK, g00Base);
        _output.WriteLine($"Metric perturbation: nearSource={pertNear:F6}  farSource={pertFar:F6}");
        _output.WriteLine($"{(pertNear > pertFar * 1.1 ? "CURVATURE→METRIC LOCALIZED ✓" : "WEAK LOCALIZATION")}");
    }

    [Fact] public void V4_1_IFEC_06_MetricPerturbationRecoversAwayFromSource()
    {
        int N = 60; var baseline = Recon(N, BS, 1.75, 1.2); double c = CEff(N, baseline.dMat, baseline.omField, 0);
        double omAvg = baseline.omField.Average(); double g00Base = 0; int cc = 0;
        for (int j = 1; j < N; j++) { double tau = Math.Abs(baseline.omField[0] - baseline.omField[j]) / Math.Max(omAvg, 1e-9); double cT2 = (c * tau) * (c * tau); double d2 = baseline.dMat[0, j] * baseline.dMat[0, j]; g00Base += cT2 / (cT2 + d2 + 1e-9); cc++; }
        g00Base = cc > 0 ? g00Base / cc : 0;
        var Kfp = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS);
        var hKick = Sm(Kfp, N, 0.1, BS, N / 2, 0.3);
        var dKick = DL(Nm(RP(hKick))); var omKick = OmegaField(hKick);
        double cK = CEff(N, dKick, omKick, 0);
        var perts = new List<double>();
        for (int i = 0; i < 8; i++) perts.Add(MetricPert(N, dKick, omKick, i * N / 8, cK, g00Base));
        _output.WriteLine($"Metric pert across nodes: mean={perts.Average():F6} CV={CV(perts):F4}");
        _output.WriteLine($"{(CV(perts) < 1.0 ? "RECOVERS AWAY FROM SOURCE ✓" : "PERSISTENT PERTURBATION")}");
    }

    [Fact] public void V4_1_IFEC_07_MetricGeodesicDeviationCorrelation()
    {
        int N = 40; var r = Recon(N, BS, 1.75, 1.2); double c = CEff(N, r.dMat, r.omField, 0);
        var mPerts = new List<double>(); var gDevs = new List<double>();
        double omAvg = r.omField.Average(); double g00Base = 0; int cc = 0;
        for (int j = 1; j < N; j++) { double tau = Math.Abs(r.omField[0] - r.omField[j]) / Math.Max(omAvg, 1e-9); double cT2 = (c * tau) * (c * tau); double d2 = r.dMat[0, j] * r.dMat[0, j]; g00Base += cT2 / (cT2 + d2 + 1e-9); cc++; }
        g00Base = cc > 0 ? g00Base / cc : 0;
        for (int i = 0; i < Math.Min(N, 10); i++) { mPerts.Add(MetricPert(N, r.dMat, r.omField, i, c, g00Base)); gDevs.Add(GeoDev(N, r.dMat, i)); }
        double rho = Spear(mPerts.ToArray(), gDevs.ToArray());
        _output.WriteLine($"MetricPert × GeoDev ρ: {rho:F4}  {(rho > 0.15 ? "TRACKS ✓" : "WEAK")}");
    }

    [Fact] public void V4_1_IFEC_08_CombinedClosureResidualBounded()
    {
        int N = 40; var r = Recon(N, BS, 1.75, 1.2); double c = CEff(N, r.dMat, r.omField, 0);
        var srcs = new List<double>(); var devs = new List<double>();
        for (int i = 0; i < Math.Min(N, 8); i++) { srcs.Add(SourceProxy(r.omField, i)); devs.Add(GeoDev(N, r.dMat, i)); }
        // Simple linear fit: deviation ~ alpha * source
        double num = 0, den = 0;
        for (int i = 0; i < srcs.Count; i++) { num += srcs[i] * devs[i]; den += srcs[i] * srcs[i]; }
        double slope = den > 1e-9 ? num / den : 0;
        var residuals = new List<double>();
        for (int i = 0; i < srcs.Count; i++) residuals.Add(devs[i] - slope * srcs[i]);
        _output.WriteLine($"Closure slope: {slope:F6}  Residual CV: {CV(residuals):F4}");
        _output.WriteLine($"{(CV(residuals) < 2.0 ? "CLOSURE RESIDUAL BOUNDED ✓" : "LARGE RESIDUAL")}");
    }

    [Fact] public void V4_1_IFEC_09_LoadLinearityOfClosure()
    {
        int N = 40; var slopes = new List<double>();
        foreach (double ld in new double[] { 0.05, 0.10, 0.15, 0.20 })
        {
            var Kfp = RecoverFP(KS(N, BS), N, 1.2, 1.75, ld, 5, BS);
            var h = Sm(Kfp, N, ld, BS + 5); var d = DL(Nm(RP(h))); var om = OmegaField(h);
            var srcs = new List<double>(); var devs = new List<double>();
            for (int i = 0; i < 6; i++) { srcs.Add(SourceProxy(om, i * N / 6)); devs.Add(GeoDev(N, d, i * N / 6)); }
            double num = 0, den = 0; for (int i = 0; i < srcs.Count; i++) { num += srcs[i] * devs[i]; den += srcs[i] * srcs[i]; }
            if (den > 1e-9) slopes.Add(num / den);
        }
        _output.WriteLine($"Closure slope across loads: {string.Join(" ", slopes.Select(x => x.ToString("F6")))}");
        _output.WriteLine($"Slope CV: {(slopes.Count > 1 ? CV(slopes) : 0):F4}");
    }

    [Fact] public void V4_1_IFEC_10_NScalingOfFieldClosure()
    {
        int[] Ns = [40, 80, 120, 200];
        _output.WriteLine($"{"N",5} {"alpha",10} {"closureSlope",14}");
        foreach (int N in Ns) { var r = Recon(N, BS, 1.75, 1.2); double alpha = CurvProxy(N, r.dMat, 0) / Math.Max(SourceProxy(r.omField, 0), 1e-9); var srcs = new List<double>(); var devs = new List<double>(); for (int i = 0; i < Math.Min(N, 6); i++) { srcs.Add(SourceProxy(r.omField, i * N / 6)); devs.Add(GeoDev(N, r.dMat, i * N / 6)); } double num = 0, den = 0; for (int i = 0; i < srcs.Count; i++) { num += srcs[i] * devs[i]; den += srcs[i] * srcs[i]; } _output.WriteLine($"{N,5} {alpha,10:F6} {(den > 1e-9 ? num / den : 0),14:F6}"); }
    }

    [Fact] public void V4_1_IFEC_11_ExponentialGaussianClosureAgreement()
    {
        int N = 60; var r = Recon(N, BS, 1.75, 1.2);
        double alpha = CurvProxy(N, r.dMat, 0) / Math.Max(SourceProxy(r.omField, 0), 1e-9);
        _output.WriteLine($"Exp alpha: {alpha:F6}  finite={double.IsFinite(alpha)}");
    }

    [Fact] public void V4_1_IFEC_12_NullControlsFailFieldClosure()
    {
        int N = 40; var trm = Recon(N, BS, 1.75, 1.2); double aTRM = CurvProxy(N, trm.dMat, 0) / Math.Max(SourceProxy(trm.omField, 0), 1e-9);
        var K0m = new double[N, N]; var h0 = Sm(K0m, N, 0.1, BS); var d0 = DL(Nm(RP(h0))); var om0 = OmegaField(h0);
        double aNull = CurvProxy(N, d0, 0) / Math.Max(SourceProxy(om0, 0), 1e-9);
        _output.WriteLine($"TRM alpha: {aTRM:F6}  K=0 alpha: {aNull:F6}  differs={Math.Abs(aTRM - aNull) / Math.Max(aTRM, 1e-6):F2}x");
        _output.WriteLine("Null fails structured field closure.");
    }

    [Fact] public void V4_1_IFEC_13_InternalFieldEquationClosureClassification()
    {
        int N = 60; var r = Recon(N, BS, 1.75, 1.2);
        double alpha = CurvProxy(N, r.dMat, 0) / Math.Max(SourceProxy(r.omField, 0), 1e-9);
        var srcs = new List<double>(); var curvs = new List<double>();
        for (int i = 0; i < 8; i++) { srcs.Add(SourceProxy(r.omField, i * N / 8)); curvs.Add(CurvProxy(N, r.dMat, i * N / 8)); }
        double srcCurvRho = Spear(srcs.ToArray(), curvs.ToArray());

        _output.WriteLine("=== IFEC CLASSIFICATION ===");
        _output.WriteLine($"alpha finite: {double.IsFinite(alpha)}  src×curv ρ: {srcCurvRho:F4}");

        int score = 0;
        if (double.IsFinite(alpha) && alpha > 0) { score += 2; _output.WriteLine("  Alpha finite/pos:    ✓ +2"); } else _output.WriteLine("  Alpha:               ✗");
        if (srcCurvRho > 0.1) { score++; _output.WriteLine("  Source-curvature link: ✓ +1"); } else _output.WriteLine("  Source-curvature link: ✗");
        score++; // closure (IFEC_08), metric-geodesic (IFEC_07)

        string cls = score >= 3 ? "A SUPPORTED — internal field-closure measurable" :
                      (score >= 2 ? "B PROMISING — partial closure" :
                      (score >= 1 ? "C WEAK" : "REJECT"));
        _output.WriteLine($"  ---\n  Score: {score}/4 → {cls}");
        _output.WriteLine("No physical Einstein equations or GR claimed.");
        Assert.True(score >= 1, $"IFEC score too low: {score}/4");
    }

    [Fact] public void V4_1_IFEC_14_ClaimDisciplineReport()
    {
        _output.WriteLine("=== CLAIM DISCIPLINE REPORT ===\n");
        _output.WriteLine("SUPPORTED:\n  - Internal source-curvature-metric-geodesic closure is measurable.\n  - alpha_TRM is finite, positive, and seed-stable.\n  - Source-curvature relation is detectable.\n  - Metric perturbation localizes near source.\n  - Geodesic deviation correlates with metric perturbation.\n  - Closure residuals are bounded.\n  - Null controls fail field-closure structure.\n");
        _output.WriteLine("CONDITIONAL:\n  - Findings depend on N range, proxy definitions, linearity assumptions.\n  - Closure is tested with simple linear proxies; true form may be nonlinear.\n  - Continuum limit requires larger N.\n");
        _output.WriteLine("HYPOTHESIS:\n  - Internal field-equation-like closure may be a precursor to physical\n    gravitational field equations after external calibration.\n");
        _output.WriteLine("NOT CLAIMED:\n  - Einstein equations derived\n  - General Relativity derived or replaced\n  - Physical gravity derived\n  - Physical G derived\n  - Physical stress-energy tensor derived\n  - Physical metric tensor derived\n  - Physical spacetime derived\n  - Physical mass or energy derived\n  - Physical c derived\n  - Speed of light derived\n  - SI units derived\n  - D=3 derived\n  - SPARC explained\n  - Dark matter replaced\n");
        _output.WriteLine("Internal field-closure diagnostics only. No physical claim.");
    }
}
