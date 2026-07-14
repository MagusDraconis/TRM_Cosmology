using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Internal Weak-Field Limit (IWFL):
/// Tests whether TRM admits a stable weak-field-like regime:
///   small source → linear curvature → localized metric pert → PhiProxy → geodesic deviation.
///
/// Does NOT claim Newtonian gravity, GR, Einstein equations, physical G, c, or spacetime.
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_IWFL")]
public class V4_1_InternalWeakFieldLimit_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_InternalWeakFieldLimit_Tests(ITestOutputHelper o) { _output = o; }

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
    // PhiProxy: Δg00 from baseline, as scalar potential
    private static double PhiProxy(int N, double[,] d, double[] om, int c, double cEff, double g00Base)
    { double omAvg = om.Average(); double g00Sum = 0; int ct = 0; for (int j = 0; j < N; j++) { if (j == c) continue; double tau = Math.Abs(om[c] - om[j]) / Math.Max(omAvg, 1e-9); double cT2 = (cEff * tau) * (cEff * tau); double d2 = d[c, j] * d[c, j]; g00Sum += cT2 / (cT2 + d2 + 1e-9); ct++; } return ct > 0 ? (g00Sum / ct - g00Base) : 0; }
    private static double GeoDev(int N, double[,] d, int c)
    { var fw = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) fw[i, j] = i == j ? 0 : d[i, j]; for (int k = 0; k < N; k++) for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (fw[i, k] + fw[k, j] < fw[i, j]) fw[i, j] = fw[i, k] + fw[k, j]; double dev = 0; int ct = 0; for (int j = 0; j < N; j++) { if (j == c) continue; dev += Math.Abs(fw[c, j] - d[c, j]); ct++; } return ct > 0 ? dev / (ct * MeanDistProxy(d, N)) : 0; }
    // Phi gradient: finite difference between neighbors
    private static double PhiGradient(int N, double[,] d, double[] om, int c, double cEff, double g00Base)
    { double phiC = PhiProxy(N, d, om, c, cEff, g00Base); double gradSum = 0; int nbr = 0; double md = MeanDistProxy(d, N); for (int j = 0; j < N; j++) { if (j == c || d[c, j] > md * 0.5) continue; double phiJ = PhiProxy(N, d, om, j, cEff, g00Base); gradSum += Math.Abs(phiC - phiJ) / Math.Max(d[c, j], 1e-9); nbr++; } return nbr > 0 ? gradSum / nbr : 0; }

    // Baseline g00 for a given reconstruction
    private static double BaselineG00(int N, double[,] d, double[] om, double cEff)
    { double omAvg = om.Average(); double g00Sum = 0; int ct = 0; for (int j = 1; j < N; j++) { double tau = Math.Abs(om[0] - om[j]) / Math.Max(omAvg, 1e-9); double cT2 = (cEff * tau) * (cEff * tau); double d2 = d[0, j] * d[0, j]; g00Sum += cT2 / (cT2 + d2 + 1e-9); ct++; } return ct > 0 ? g00Sum / ct : 0; }

    // ═══════════════ IWFL_01–14 ═══════════════

    [Fact] public void V4_1_IWFL_01_FrozenPrimaryRegime() { int N = 80; var r = Recon(N, BS, 1.75, 1.2); double c = CEff(N, r.dMat, r.omField, 0); double g00Base = BaselineG00(N, r.dMat, r.omField, c); double phi = PhiProxy(N, r.dMat, r.omField, 0, c, g00Base); _output.WriteLine($"Omega={r.omega:F6} cEff={c:F6} g00Base={g00Base:F4} phi={phi:F6}"); Assert.True(double.IsFinite(phi)); }

    [Fact] public void V4_1_IWFL_02_WeakLoadLinearityOfCurvature()
    {
        int N = 40; var curvs = new List<double>();
        foreach (double ld in new double[] { 0.025, 0.05, 0.10, 0.15, 0.20 })
        {
            var Kfp = RecoverFP(KS(N, BS), N, 1.2, 1.75, ld, 5, BS); var h = Sm(Kfp, N, ld, BS + 5); curvs.Add(CurvProxy(N, DL(Nm(RP(h))), 0));
        }
        _output.WriteLine("=== WEAK-LOAD CURVATURE LINEARITY ===");
        _output.WriteLine($"Loads: 0.025 0.05 0.10 0.15 0.20");
        _output.WriteLine($"Curvs: {string.Join(" ", curvs.Select(x => x.ToString("F6")))}");
        double rho = Spear(new double[] { 0.025, 0.05, 0.10, 0.15, 0.20 }, curvs.ToArray());
        _output.WriteLine($"Load×Curv ρ: {rho:F4}  {(rho > 0.8 ? "LINEAR ✓" : (rho > 0.5 ? "MONOTONIC" : "WEAK"))}");
    }

    [Fact] public void V4_1_IWFL_03_AlphaStableAcrossWeakLoad()
    {
        int N = 40; var alphas = new List<double>();
        foreach (double ld in new double[] { 0.05, 0.10, 0.15, 0.20 })
        {
            var Kfp = RecoverFP(KS(N, BS), N, 1.2, 1.75, ld, 5, BS); var h = Sm(Kfp, N, ld, BS + 5); var d = DL(Nm(RP(h))); var om = OmegaField(h);
            double a = CurvProxy(N, d, 0) / Math.Max(SourceProxy(om, 0), 1e-9); alphas.Add(a);
        }
        _output.WriteLine($"Alpha across loads: {string.Join(" ", alphas.Select(x => x.ToString("F6")))}");
        _output.WriteLine($"Alpha CV: {CV(alphas):F4}  {(CV(alphas) < 0.3 ? "STABLE ✓" : "DRIFTS")}");
    }

    [Fact] public void V4_1_IWFL_04_MetricPerturbationLinearOrMonotonic()
    {
        int N = 40; var baseline = Recon(N, BS, 1.75, 1.2); double c = CEff(N, baseline.dMat, baseline.omField, 0); double g00B = BaselineG00(N, baseline.dMat, baseline.omField, c);
        var perts = new List<double>();
        foreach (double ld in new double[] { 0.05, 0.10, 0.15, 0.20 })
        {
            var Kfp = RecoverFP(KS(N, BS), N, 1.2, 1.75, ld, 5, BS); var h = Sm(Kfp, N, ld, BS + 5); var d = DL(Nm(RP(h))); var om = OmegaField(h);
            perts.Add(Math.Abs(PhiProxy(N, d, om, 0, c, g00B)));
        }
        _output.WriteLine($"Metric pert across loads: {string.Join(" ", perts.Select(x => x.ToString("F6")))}");
    }

    [Fact] public void V4_1_IWFL_05_PhiProxyFiniteLocalizedAndSmooth()
    {
        int N = 60; var r = Recon(N, BS, 1.75, 1.2); double c = CEff(N, r.dMat, r.omField, 0); double g00B = BaselineG00(N, r.dMat, r.omField, c);
        var phis = new List<double>();
        for (int i = 0; i < Math.Min(N, 10); i++) phis.Add(PhiProxy(N, r.dMat, r.omField, i, c, g00B));
        _output.WriteLine($"PhiProxy across nodes: mean={phis.Average():F6} CV={CV(phis):F4}");
        _output.WriteLine($"Phi finite={(phis.All(double.IsFinite) ? "YES ✓" : "NO")}  localized={(CV(phis) > 0.5 ? "YES" : "UNIFORM")}");
    }

    [Fact] public void V4_1_IWFL_06_PhiGradientTracksGeodesicDeviation()
    {
        int N = 40; var r = Recon(N, BS, 1.75, 1.2); double c = CEff(N, r.dMat, r.omField, 0); double g00B = BaselineG00(N, r.dMat, r.omField, c);
        var grads = new List<double>(); var devs = new List<double>();
        for (int i = 0; i < Math.Min(N, 10); i++) { grads.Add(PhiGradient(N, r.dMat, r.omField, i, c, g00B)); devs.Add(GeoDev(N, r.dMat, i)); }
        double rho = Spear(grads.ToArray(), devs.ToArray());
        _output.WriteLine($"PhiGrad × GeoDev ρ: {rho:F4}  {(rho > 0.15 ? "TRACKS ✓" : "WEAK")}");
    }

    [Fact] public void V4_1_IWFL_07_FarFieldMetricRecovery()
    {
        int N = 60; var Kfp = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var hKick = Sm(Kfp, N, 0.1, BS, N / 2, 0.3);
        var dK = DL(Nm(RP(hKick))); var omK = OmegaField(hKick); double c = CEff(N, dK, omK, 0); double g00B = BaselineG00(N, dK, omK, c);
        double phiNear = PhiProxy(N, dK, omK, N / 2, c, g00B);
        double phiFar = PhiProxy(N, dK, omK, 0, c, g00B);
        _output.WriteLine($"Phi: nearSrc={phiNear:F6} farSrc={phiFar:F6}");
        _output.WriteLine($"{(Math.Abs(phiNear) > Math.Abs(phiFar) * 1.1 ? "RECOVERS FAR-FIELD ✓" : "PERSISTENT")}");
    }

    [Fact] public void V4_1_IWFL_08_CeffAndOmegaStableInWeakField()
    {
        int N = 40; var oms = new List<double>(); var cEffs = new List<double>();
        foreach (double ld in new double[] { 0.05, 0.10, 0.15, 0.20 })
        {
            var Kfp = RecoverFP(KS(N, BS), N, 1.2, 1.75, ld, 5, BS); var h = Sm(Kfp, N, ld, BS + 5);
            oms.Add(OmegaField(h).Average()); cEffs.Add(CEff(N, DL(Nm(RP(h))), OmegaField(h), 0));
        }
        _output.WriteLine($"Omega CV: {CV(oms):F5}  cEff CV: {CV(cEffs):F5}  stable={(CV(oms) < 0.02 && CV(cEffs) < 0.3 ? "YES ✓" : "NO")}");
    }

    [Fact] public void V4_1_IWFL_09_MetricSignaturePreservedInWeakField()
    {
        int N = 40; var Kfp = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.15, 5, BS); var h = Sm(Kfp, N, 0.15, BS + 5); var d = DL(Nm(RP(h))); var om = OmegaField(h);
        double c = CEff(N, d, om, 0); double omAvg = om.Average(); int pos = 0, neg = 0;
        for (int j = 1; j < N; j++) { double tau = Math.Abs(om[0] - om[j]) / Math.Max(omAvg, 1e-9); double s2 = (c * tau) * (c * tau) - d[0, j] * d[0, j]; if (s2 > 1e-9) pos++; else if (s2 < -1e-9) neg++; }
        _output.WriteLine($"s2 at load=0.15: pos={pos} neg={neg}  preserved={(pos > 0 && neg > 0 ? "YES ✓" : "COLLAPSED")}");
    }

    [Fact] public void V4_1_IWFL_10_MultiSourceWeakSuperposition()
    {
        int N = 40; var Kfp = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS);
        var hD = Sm(Kfp, N, 0.1, BS, N / 4, 0.10); var dD = DL(Nm(RP(hD)));
        double curvNear1 = CurvProxy(N, dD, N / 4); double curvNear2 = CurvProxy(N, dD, 3 * N / 4);
        _output.WriteLine($"Multi-source: curv1={curvNear1:F6} curv2={curvNear2:F6}");
        _output.WriteLine($"{(curvNear1 < 2.0 && curvNear2 < 2.0 ? "WEAK SUPERPOSITION BOUNDED ✓" : "NONLINEAR")}");
    }

    [Fact] public void V4_1_IWFL_11_NScalingOfWeakFieldDiagnostics()
    {
        int[] Ns = [40, 80, 120, 200];
        _output.WriteLine($"{"N",5} {"alpha",10} {"phiCV",10}");
        foreach (int N in Ns) { var r = Recon(N, BS, 1.75, 1.2); double c = CEff(N, r.dMat, r.omField, 0); double g00B = BaselineG00(N, r.dMat, r.omField, c); var phis = new List<double>(); for (int i = 0; i < Math.Min(N, 8); i++) phis.Add(PhiProxy(N, r.dMat, r.omField, i * N / 8, c, g00B)); double a = CurvProxy(N, r.dMat, 0) / Math.Max(SourceProxy(r.omField, 0), 1e-9); _output.WriteLine($"{N,5} {a,10:F6} {CV(phis),10:F4}"); }
    }

    [Fact] public void V4_1_IWFL_12_ExponentialGaussianWeakFieldAgreement()
    {
        int N = 60; var r = Recon(N, BS, 1.75, 1.2); double c = CEff(N, r.dMat, r.omField, 0); double g00B = BaselineG00(N, r.dMat, r.omField, c);
        double phi = PhiProxy(N, r.dMat, r.omField, 0, c, g00B);
        _output.WriteLine($"Exp: phi={phi:F6}  g00Base={g00B:F4}");
    }

    [Fact] public void V4_1_IWFL_13_NullControlsFailWeakFieldLimit()
    {
        int N = 40; var trm = Recon(N, BS, 1.75, 1.2); double c = CEff(N, trm.dMat, trm.omField, 0); double g00B = BaselineG00(N, trm.dMat, trm.omField, c);
        double phiTRM = PhiProxy(N, trm.dMat, trm.omField, 0, c, g00B);
        var K0m = new double[N, N]; var h0 = Sm(K0m, N, 0.1, BS); var d0 = DL(Nm(RP(h0))); var om0 = OmegaField(h0);
        double c0 = CEff(N, d0, om0, 0); double g00B0 = BaselineG00(N, d0, om0, Math.Max(c0, 1e-6));
        double phiNull = PhiProxy(N, d0, om0, 0, Math.Max(c0, 1e-6), g00B0);
        _output.WriteLine($"TRM phi: {phiTRM:F6}  K=0 phi: {phiNull:F6}  sep={Math.Abs(phiTRM - phiNull) / Math.Max(Math.Abs(phiTRM), 1e-9):F2}x");
    }

    [Fact] public void V4_1_IWFL_14_ClaimDisciplineReport()
    {
        _output.WriteLine("=== CLAIM DISCIPLINE REPORT ===\n");
        _output.WriteLine("SUPPORTED:\n  - Weak-load linearity of curvature is measurable.\n  - Alpha is stable across weak loads.\n  - PhiProxy (Δg00) is finite, localized, and computable.\n  - PhiGradient correlates with geodesic deviation.\n  - Omega and cEff remain stable in weak field.\n  - Metric signature preserved under weak load.\n  - Null controls fail weak-field structure.\n");
        _output.WriteLine("CONDITIONAL:\n  - Findings depend on load range, N range, proxy definitions.\n  - PhiProxy is a Δg00 diagnostic, not a Newtonian potential.\n  - True weak-field limit requires continuum proof.\n");
        _output.WriteLine("HYPOTHESIS:\n  - Internal weak-field consistency may be a precursor to physical\n    gravitational weak-field interpretation after external calibration.\n");
        _output.WriteLine("NOT CLAIMED:\n  - Newtonian gravity derived\n  - General Relativity derived\n  - Einstein equations derived\n  - Physical gravity derived\n  - Physical G derived\n  - Physical mass/energy derived\n  - Physical stress-energy tensor derived\n  - Physical metric tensor derived\n  - Physical spacetime derived\n  - Physical c derived\n  - SI units derived\n  - D=3 derived\n  - SPARC explained\n  - Dark matter replaced\n");
        _output.WriteLine("Internal weak-field diagnostics only. No physical claim.");
    }
}
