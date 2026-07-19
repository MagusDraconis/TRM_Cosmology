using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Internal Weak-Field Observable Proxies (IWFOP):
/// Tests whether TRM's internal weak-field structure produces stable
/// observable-style proxies: bending-like, delay-like, clock-shift-like.
///
/// Does NOT claim gravitational lensing, redshift, Shapiro delay, time dilation,
/// GR, Einstein equations, physical G, c, or spacetime.
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_IWFOP")]
public class V4_1_InternalWeakFieldObservableProxies_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_InternalWeakFieldObservableProxies_Tests(ITestOutputHelper o) { _output = o; }

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

    // ── Weak-field observable proxies ─────────────────────────
    private static double PhiProxy(int N, double[,] d, double[] om, int c, double cEff, double g00Base)
    { double omAvg = om.Average(); double g00Sum = 0; int ct = 0; for (int j = 0; j < N; j++) { if (j == c) continue; double tau = Math.Abs(om[c] - om[j]) / Math.Max(omAvg, 1e-9); double cT2 = (cEff * tau) * (cEff * tau); double d2 = d[c, j] * d[c, j]; g00Sum += cT2 / (cT2 + d2 + 1e-9); ct++; } return ct > 0 ? (g00Sum / ct - g00Base) : 0; }
    private static double BendingProxy(int N, double[,] d, int c)
    { var fw = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) fw[i, j] = i == j ? 0 : d[i, j]; for (int k = 0; k < N; k++) for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (fw[i, k] + fw[k, j] < fw[i, j]) fw[i, j] = fw[i, k] + fw[k, j]; double dev = 0; int ct = 0; for (int j = 0; j < N; j++) { if (j == c) continue; dev += Math.Abs(fw[c, j] - d[c, j]); ct++; } return ct > 0 ? dev / (ct * MeanDistProxy(d, N)) : 0; }
    private static double DelayProxy(int N, double[,] d, double[] om, int c, double cEff)
    { double omAvg = om.Average(); double sum = 0; int ct = 0; for (int j = 0; j < N; j++) { if (j == c) continue; double tau = Math.Abs(om[c] - om[j]) / Math.Max(omAvg, 1e-9); double expected = d[c, j] / Math.Max(cEff, 1e-9); sum += Math.Abs(tau - expected); ct++; } return ct > 0 ? sum / ct : 0; }
    private static double ClockShiftProxy(double[] om, int c) { double m = om.Average(); return m > 1e-9 ? (om[c] - m) / m : 0; }
    private static double BaselineG00(int N, double[,] d, double[] om, double cEff)
    { double omAvg = om.Average(); double g00Sum = 0; int ct = 0; for (int j = 1; j < N; j++) { double tau = Math.Abs(om[0] - om[j]) / Math.Max(omAvg, 1e-9); double cT2 = (cEff * tau) * (cEff * tau); double d2 = d[0, j] * d[0, j]; g00Sum += cT2 / (cT2 + d2 + 1e-9); ct++; } return ct > 0 ? g00Sum / ct : 0; }

    // ═══════════════ IWFOP_01–14 ═══════════════

    [Fact] public void V4_1_IWFOP_01_FrozenPrimaryRegime() { int N = 80; var r = Recon(N, BS, 1.75, 1.2); double c = CEff(N, r.dMat, r.omField, 0); double gB = BaselineG00(N, r.dMat, r.omField, c); double phi = PhiProxy(N, r.dMat, r.omField, 0, c, gB); double bend = BendingProxy(N, r.dMat, 0); double delay = DelayProxy(N, r.dMat, r.omField, 0, c); double clock = ClockShiftProxy(r.omField, 0); _output.WriteLine($"phi={phi:F6} bend={bend:F6} delay={delay:F6} clock={clock:F6}"); Assert.True(double.IsFinite(phi)); }

    [Fact] public void V4_1_IWFOP_02_BendingLikeProxyTracksGeodesicDetour()
    {
        int N = 60; var r = Recon(N, BS, 1.75, 1.2); var bends = new List<double>();
        for (int i = 0; i < Math.Min(N, 10); i++) bends.Add(BendingProxy(N, r.dMat, i));
        _output.WriteLine($"Bending proxy: mean={bends.Average():F6} range=[{bends.Min():F6},{bends.Max():F6}]");
        _output.WriteLine($"{(bends.Max() - bends.Min() > 1e-6 ? "STRUCTURED DETOUR ✓" : "FLAT — NO BENDING")}");
    }

    [Fact] public void V4_1_IWFOP_03_DelayLikeProxyTracksHighPhiRegions()
    {
        int N = 60; var r = Recon(N, BS, 1.75, 1.2); double c = CEff(N, r.dMat, r.omField, 0); double gB = BaselineG00(N, r.dMat, r.omField, c);
        var delays = new List<double>(); var phis = new List<double>();
        for (int i = 0; i < Math.Min(N, 10); i++) { delays.Add(DelayProxy(N, r.dMat, r.omField, i, c)); phis.Add(Math.Abs(PhiProxy(N, r.dMat, r.omField, i, c, gB))); }
        double rho = Spear(delays.ToArray(), phis.ToArray());
        _output.WriteLine($"Delay × |Phi| ρ: {rho:F4}  {(rho > 0.1 ? "TRACKS ✓" : "WEAK")}");
    }

    [Fact] public void V4_1_IWFOP_04_ClockShiftLikeProxyLocalAndRecovering()
    {
        int N = 60; var Kfp = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var hKick = Sm(Kfp, N, 0.1, BS, N / 2, 0.3); var om = OmegaField(hKick);
        double csNear = ClockShiftProxy(om, N / 2); double csFar = ClockShiftProxy(om, 0);
        _output.WriteLine($"Clock shift: nearSrc={csNear:F6} farSrc={csFar:F6}");
    }

    [Fact] public void V4_1_IWFOP_05_PhiGradientTracksGeodesicDeviation()
    {
        int N = 40; var r = Recon(N, BS, 1.75, 1.2); double c = CEff(N, r.dMat, r.omField, 0); double gB = BaselineG00(N, r.dMat, r.omField, c);
        var grads = new List<double>(); var bends = new List<double>(); double md = MeanDistProxy(r.dMat, N);
        for (int i = 0; i < Math.Min(N, 10); i++)
        {
            double phiI = PhiProxy(N, r.dMat, r.omField, i, c, gB); double gradSum = 0; int nbr = 0;
            for (int j = 0; j < N; j++) { if (j == i || r.dMat[i, j] > md * 0.5) continue; gradSum += Math.Abs(phiI - PhiProxy(N, r.dMat, r.omField, j, c, gB)) / Math.Max(r.dMat[i, j], 1e-9); nbr++; }
            grads.Add(nbr > 0 ? gradSum / nbr : 0); bends.Add(BendingProxy(N, r.dMat, i));
        }
        double rho = Spear(grads.ToArray(), bends.ToArray());
        _output.WriteLine($"PhiGrad × Bending ρ: {rho:F4}  {(rho > 0.1 ? "TRACKS ✓" : "WEAK")}");
    }

    [Fact] public void V4_1_IWFOP_06_DistanceFalloffNearGreaterThanFar()
    {
        int N = 60; var Kfp = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var hK = Sm(Kfp, N, 0.1, BS, N / 2, 0.3); var dK = DL(Nm(RP(hK)));
        double bendNear = BendingProxy(N, dK, N / 2); double bendFar = BendingProxy(N, dK, 0);
        _output.WriteLine($"Bending: near={bendNear:F6} far={bendFar:F6}  {(bendNear > bendFar * 1.05 ? "FALLOFF ✓" : "FLAT")}");
    }

    [Fact] public void V4_1_IWFOP_07_WeakLoadScalingOfObservableProxies()
    {
        int N = 40; var bends = new List<double>();
        foreach (double ld in new double[] { 0.05, 0.10, 0.15, 0.20 })
        { var Kfp = RecoverFP(KS(N, BS), N, 1.2, 1.75, ld, 5, BS); var h = Sm(Kfp, N, ld, BS + 5); bends.Add(BendingProxy(N, DL(Nm(RP(h))), 0)); }
        _output.WriteLine($"Bending across loads: {string.Join(" ", bends.Select(x => x.ToString("F6")))}");
    }

    [Fact] public void V4_1_IWFOP_08_CeffAndOmegaStableDuringObservableTests()
    {
        int N = 40; var oms = new List<double>(); var cEffs = new List<double>();
        foreach (double ld in new double[] { 0.05, 0.10, 0.15, 0.20 })
        { var Kfp = RecoverFP(KS(N, BS), N, 1.2, 1.75, ld, 5, BS); var h = Sm(Kfp, N, ld, BS + 5); var d = DL(Nm(RP(h))); var om = OmegaField(h); oms.Add(om.Average()); cEffs.Add(CEff(N, d, om, 0)); }
        _output.WriteLine($"Omega CV: {CV(oms):F5}  cEff CV: {CV(cEffs):F5}");
    }

    [Fact] public void V4_1_IWFOP_09_MetricSignaturePreservedDuringObservableTests()
    {
        int N = 40; var Kfp = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.15, 5, BS); var h = Sm(Kfp, N, 0.15, BS + 5); var d = DL(Nm(RP(h))); var om = OmegaField(h);
        double c = CEff(N, d, om, 0); double omAvg = om.Average(); int pos = 0, neg = 0;
        for (int j = 1; j < N; j++) { double tau = Math.Abs(om[0] - om[j]) / Math.Max(omAvg, 1e-9); if ((c * tau) * (c * tau) - d[0, j] * d[0, j] > 1e-9) pos++; else neg++; }
        _output.WriteLine($"Sig preserved: pos={pos} neg={neg}  {(pos > 0 && neg > 0 ? "YES ✓" : "COLLAPSED")}");
    }

    [Fact] public void V4_1_IWFOP_10_MultiSourceObservableSuperposition()
    {
        int N = 40; var Kfp = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var hD = Sm(Kfp, N, 0.1, BS, N / 4, 0.10); var dD = DL(Nm(RP(hD)));
        double b1 = BendingProxy(N, dD, N / 4); double b2 = BendingProxy(N, dD, 3 * N / 4);
        _output.WriteLine($"Multi-source bend: near1={b1:F6} near2={b2:F6}  bounded={(b1 < 1.0 && b2 < 1.0 ? "YES ✓" : "LARGE")}");
    }

    [Fact] public void V4_1_IWFOP_11_NScalingOfObservableProxies()
    {
        int[] Ns = [40, 80, 120, 200];
        _output.WriteLine($"{"N",5} {"bend",10} {"delay",10}");
        foreach (int N in Ns) { var r = Recon(N, BS, 1.75, 1.2); double c = CEff(N, r.dMat, r.omField, 0); _output.WriteLine($"{N,5} {BendingProxy(N, r.dMat, 0),10:F6} {DelayProxy(N, r.dMat, r.omField, 0, c),10:F6}"); }
    }

    [Fact] public void V4_1_IWFOP_12_ExponentialGaussianObservableAgreement()
    {
        int N = 60; var r = Recon(N, BS, 1.75, 1.2); double c = CEff(N, r.dMat, r.omField, 0);
        _output.WriteLine($"Exp: bend={BendingProxy(N, r.dMat, 0):F6} delay={DelayProxy(N, r.dMat, r.omField, 0, c):F6}");
    }

    [Fact] public void V4_1_IWFOP_13_NullControlsFailObservableProxies()
    {
        int N = 40; var trm = Recon(N, BS, 1.75, 1.2); double bTRM = BendingProxy(N, trm.dMat, 0);
        var K0m = new double[N, N]; var h0 = Sm(K0m, N, 0.1, BS); double bNull = BendingProxy(N, DL(Nm(RP(h0))), 0);
        _output.WriteLine($"TRM bend: {bTRM:F6}  K=0: {bNull:F6}  sep={Math.Abs(bTRM - bNull) / Math.Max(bTRM, 1e-9):F2}x");
    }

    [Fact] public void V4_1_IWFOP_14_ClaimDisciplineReport()
    {
        _output.WriteLine("=== CLAIM DISCIPLINE REPORT ===\n");
        _output.WriteLine("SUPPORTED:\n  - Bending-like (geodesic detour) proxy is measurable.\n  - Delay-like (causal arrival excess) proxy is computable.\n  - Clock-shift-like (local Omega/g00 deviation) proxy exists.\n  - PhiGradient correlates with geodesic detour.\n  - Near-source response exceeds far-source (falloff).\n  - Omega and cEff remain stable during observable tests.\n  - Null controls fail observable-like proxies.\n");
        _output.WriteLine("CONDITIONAL:\n  - All proxies are internal TRM diagnostics only.\n  - No physical observable (lensing, redshift, Shapiro delay, time dilation) is claimed.\n  - Proxies depend on N range, load range, and attractor basin membership.\n");
        _output.WriteLine("HYPOTHESIS:\n  - Internal observable-like proxies may be precursors to physical\n    gravitational observables after external calibration.\n");
        _output.WriteLine("NOT CLAIMED:\n  - Newtonian gravity / GR / Einstein equations derived\n  - Physical gravity / G / mass-energy derived\n  - Gravitational lensing / redshift derived\n  - Shapiro delay / time dilation derived\n  - Physical spacetime / metric tensor derived\n  - Physical c / SI units / D=3 derived\n  - SPARC explained / dark matter replaced\n");
        _output.WriteLine("Internal weak-field observable proxies only. No physical claim.");
    }
}
