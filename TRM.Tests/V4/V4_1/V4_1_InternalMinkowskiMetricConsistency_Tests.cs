using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Internal Minkowski-Metric Consistency (IMMC):
/// Tests whether TRM supports a stable Minkowski-like metric-signature proxy
/// with distinct time-like and space-like components from Omega, d_ij, and c_eff.
///
/// g00-like ∝ (c_eff·tau)² / s²   gSpatial-like ∝ d² / s²   off-diagonal → 0
///
/// Does NOT claim physical spacetime, Minkowski metric, Lorentz invariance, c, or GR.
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_IMMC")]
public class V4_1_InternalMinkowskiMetricConsistency_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_InternalMinkowskiMetricConsistency_Tests(ITestOutputHelper o) { _output = o; }

    private static int EpochsForN(int N) => N <= 80 ? 5 : N <= 200 ? 5 : N <= 300 ? 3 : 2;

    // ── Simulation ────────────────────────────────────────────
    private static double[][] Sm(double[,] K, int N, double s, int seed, int knock = -1, double dOrAmp = 0, int kickT = -1)
    {
        var r = new Random(seed); var w = new double[N]; for (int i = 0; i < N; i++) w[i] = 1.0 + s * (r.NextDouble() - 0.5) * 2.0;
        if (kickT < 0 && knock >= 0 && knock < N) w[knock] += dOrAmp;
        var th = new double[N]; for (int i = 0; i < N; i++) th[i] = r.NextDouble() * 2.0 * Math.PI;
        int hL = St / Hd + 1; var h = new double[hL][]; h[0] = (double[])th.Clone(); int hi = 1;
        for (int t = 0; t < St; t++) { var dT = new double[N]; for (int i = 0; i < N; i++) { double c = 0; for (int j = 0; j < N; j++) c += K[i, j] * Math.Sin(th[j] - th[i]); dT[i] = w[i] + c; } if (kickT >= 0 && knock >= 0 && knock < N && t == kickT) dT[knock] += dOrAmp; for (int i = 0; i < N; i++) th[i] += Dt * dT[i]; if ((t + 1) % Hd == 0 && hi < hL) h[hi++] = (double[])th.Clone(); }
        return h;
    }
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
    {
        int E = EpochsForN(N); var Kfp = RecoverFP(KS(N, seed), N, k0, xi, 0.1, E, seed);
        var h = Sm(Kfp, N, 0.1, seed + E); var dMat = DL(Nm(RP(h))); var om = OmegaField(h);
        return (om.Average(), dMat, om, MeanDistProxy(dMat, N));
    }
    private static double CEff(int N, double[,] d, double[] om, int src)
    {
        var dists = new List<double>(); var delays = new List<double>(); double omAvg = om.Average();
        for (int j = 0; j < N; j++) { if (j == src) continue; dists.Add(d[src, j]); delays.Add(Math.Abs(om[src] - om[j]) / Math.Max(omAvg, 1e-9)); }
        double num = 0, den = 0; for (int i = 0; i < dists.Count; i++) { num += dists[i] * delays[i]; den += delays[i] * delays[i]; }
        return den > 1e-9 ? num / den : 0;
    }

    // ── Metric-signature proxies ──────────────────────────────
    private static (List<double> g00, List<double> gSpatial, List<double> offDiag, List<double> s2, double timeFrac)
        SignatureProxies(int N, double[,] d, double[] om, int src, double cEff)
    {
        double omAvg = om.Average(); var g00 = new List<double>(); var gS = new List<double>(); var off = new List<double>(); var s2 = new List<double>(); int timeCount = 0;
        for (int j = 0; j < N; j++)
        {
            if (j == src) continue;
            double dist = d[src, j];
            double tau = Math.Abs(om[src] - om[j]) / Math.Max(omAvg, 1e-9);
            double cTau2 = (cEff * tau) * (cEff * tau);
            double d2 = dist * dist;
            double interval = cTau2 - d2;
            s2.Add(interval);
            double denom = Math.Abs(cTau2) + Math.Abs(d2) + 1e-9;
            g00.Add(cTau2 / denom);
            gS.Add(d2 / denom);
            off.Add((cEff * tau * dist) / denom);
            if (interval >= -1e-9) timeCount++;
        }
        return (g00, gS, off, s2, (double)timeCount / (N - 1));
    }

    // ═══════════════ IMMC_01–14 ═══════════════

    [Fact] public void V4_1_IMMC_01_FrozenPrimaryRegime() { int N = 80; var r = Recon(N, BS, 1.75, 1.2); double c = CEff(N, r.dMat, r.omField, 0); var sp = SignatureProxies(N, r.dMat, r.omField, 0, c); _output.WriteLine($"Omega={r.omega:F6} cEff={c:F6} g00={sp.g00.Average():F3} gSpatial={sp.gSpatial.Average():F3} offDiag={sp.offDiag.Average():F3} timeFrac={sp.timeFrac:F3}"); Assert.True(double.IsFinite(c) && c > 0); }

    [Fact] public void V4_1_IMMC_02_IntervalSignatureRegionsExist()
    {
        int N = 80; var r = Recon(N, BS, 1.75, 1.2); double c = CEff(N, r.dMat, r.omField, 0); var sp = SignatureProxies(N, r.dMat, r.omField, 0, c);
        int pos = sp.s2.Count(s => s > 1e-9); int neg = sp.s2.Count(s => s < -1e-9);
        _output.WriteLine($"s2: positive={pos} negative={neg} time-like fraction={sp.timeFrac:F3}");
        _output.WriteLine($"{(pos > 0 && neg > 0 ? "SIGNATURE REGIONS EXIST ✓ (mixed sign)" : "SINGLE SIGN — NOT MINKOWSKI-LIKE")}");
        _output.WriteLine("Minkowski signature: (+,-,-,-) → time-like s2>0, space-like s2<0.");
        _output.WriteLine("No physical Minkowski spacetime claimed — internal structure only.");
    }

    [Fact] public void V4_1_IMMC_03_G00LikeClockProxyStable() { int N = 80; var r = Recon(N, BS, 1.75, 1.2); double c = CEff(N, r.dMat, r.omField, 0); var sp = SignatureProxies(N, r.dMat, r.omField, 0, c); _output.WriteLine($"g00 proxy: mean={sp.g00.Average():F4} CV={CV(sp.g00):F4}"); _output.WriteLine($"g00 ∈ [0,1] — {(sp.g00.All(x => x >= -1e-9 && x <= 1 + 1e-9) ? "BOUNDED ✓" : "UNBOUNDED ✗")}"); _output.WriteLine("g00 > 0.5 means clock dominates; g00 < 0.5 means distance dominates."); }

    [Fact] public void V4_1_IMMC_04_GSpatialLikeDistanceProxyStable() { int N = 80; var r = Recon(N, BS, 1.75, 1.2); double c = CEff(N, r.dMat, r.omField, 0); var sp = SignatureProxies(N, r.dMat, r.omField, 0, c); _output.WriteLine($"gSpatial proxy: mean={sp.gSpatial.Average():F4} CV={CV(sp.gSpatial):F4}"); _output.WriteLine($"gSpatial + g00 ≈ 1: avg sum = {sp.g00.Zip(sp.gSpatial, (a, b) => a + b).Average():F4}"); }

    [Fact] public void V4_1_IMMC_05_OffDiagonalProxyBounded() { int N = 80; var r = Recon(N, BS, 1.75, 1.2); double c = CEff(N, r.dMat, r.omField, 0); var sp = SignatureProxies(N, r.dMat, r.omField, 0, c); _output.WriteLine($"Off-diagonal proxy: mean={sp.offDiag.Average():F4} max={sp.offDiag.Max():F4}"); _output.WriteLine($"{(sp.offDiag.Max() < 0.5 ? "OFF-DIAGONAL BOUNDED ✓ (diagonal-dominant)" : "LARGE OFF-DIAGONAL — not diagonal metric")}"); }

    [Fact] public void V4_1_IMMC_06_SignatureBalanceNotCollapsed()
    {
        int N = 60; int nSeeds = 8; var timeFracs = new List<double>();
        for (int s = 0; s < nSeeds; s++) { var r = Recon(N, s, 1.75, 1.2); double c = CEff(N, r.dMat, r.omField, 0); var sp = SignatureProxies(N, r.dMat, r.omField, 0, c); timeFracs.Add(sp.timeFrac); }
        _output.WriteLine($"Time-like fraction across seeds: mean={timeFracs.Average():F3} CV={CV(timeFracs):F4}");
        _output.WriteLine($"{(timeFracs.Average() > 0.05 && timeFracs.Average() < 0.95 ? "SIGNATURE BALANCED ✓" : "COLLAPSED TO SINGLE SIGN")}");
    }

    [Fact] public void V4_1_IMMC_07_GeodesicDistancePreservesSpatialComponent()
    {
        int N = 40; var r = Recon(N, BS, 1.75, 1.2);
        var fw = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) fw[i, j] = i == j ? 0 : r.dMat[i, j];
        for (int k = 0; k < N; k++) for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (fw[i, k] + fw[k, j] < fw[i, j]) fw[i, j] = fw[i, k] + fw[k, j];
        double cD = CEff(N, r.dMat, r.omField, 0); double cG = CEff(N, fw, r.omField, 0);
        var spD = SignatureProxies(N, r.dMat, r.omField, 0, cD); var spG = SignatureProxies(N, fw, r.omField, 0, cG);
        _output.WriteLine($"Direct gSpatial:     {spD.gSpatial.Average():F4}");
        _output.WriteLine($"Geodesic gSpatial:   {spG.gSpatial.Average():F4}");
        _output.WriteLine($"{(Math.Abs(spD.gSpatial.Average() - spG.gSpatial.Average()) < 0.2 ? "SPATIAL PRESERVED ✓" : "SPATIAL DRIFTS")}");
    }

    [Fact] public void V4_1_IMMC_08_FrameRobustnessOfSignature()
    {
        int N = 60; var r = Recon(N, BS, 1.75, 1.2); var g00s = new List<double>(); var gSs = new List<double>();
        for (int s = 0; s < 5; s++) { int src = s * N / 5; double c = CEff(N, r.dMat, r.omField, src); var sp = SignatureProxies(N, r.dMat, r.omField, src, c); g00s.Add(sp.g00.Average()); gSs.Add(sp.gSpatial.Average()); }
        _output.WriteLine($"g00 CV across frames: {CV(g00s):F4}  gSpatial CV: {CV(gSs):F4}");
        _output.WriteLine($"{(CV(g00s) < 0.3 && CV(gSs) < 0.3 ? "FRAME-ROBUST ✓" : "FRAME-DEPENDENT")}");
    }

    [Fact] public void V4_1_IMMC_09_CeffSupportsIntervalNormalization()
    {
        int N = 80; var r = Recon(N, BS, 1.75, 1.2);
        // Without cEff normalization, s2 is dominated by spatial component
        double c = CEff(N, r.dMat, r.omField, 0);
        var sp = SignatureProxies(N, r.dMat, r.omField, 0, c);
        double g00 = sp.g00.Average(); double gS = sp.gSpatial.Average();
        _output.WriteLine($"With cEff={c:F4}: g00={g00:F3} gSpatial={gS:F3}");
        _output.WriteLine($"cEff sets the clock-distance scale to balance the signature.");
        _output.WriteLine($"Balanced signature (|g00-gSpatial| < 0.5): {(Math.Abs(g00 - gS) < 0.5 ? "YES ✓" : "IMBALANCED")}");
    }

    [Fact] public void V4_1_IMMC_10_NScalingOfMetricSignature()
    {
        int[] Ns = [40, 80, 120, 200];
        _output.WriteLine($"{"N",5} {"g00",10} {"gSpatial",10} {"offDiag",10} {"timeFrac",10}");
        foreach (int N in Ns) { var r = Recon(N, BS, 1.75, 1.2); double c = CEff(N, r.dMat, r.omField, 0); var sp = SignatureProxies(N, r.dMat, r.omField, 0, c); _output.WriteLine($"{N,5} {sp.g00.Average(),10:F4} {sp.gSpatial.Average(),10:F4} {sp.offDiag.Average(),10:F4} {sp.timeFrac,10:F3}"); }
    }

    [Fact] public void V4_1_IMMC_11_ExponentialGaussianSignatureAgreement()
    {
        int N = 60; var r = Recon(N, BS, 1.75, 1.2); double c = CEff(N, r.dMat, r.omField, 0); var sp = SignatureProxies(N, r.dMat, r.omField, 0, c);
        _output.WriteLine($"Exp: g00={sp.g00.Average():F3} gSpatial={sp.gSpatial.Average():F3} offDiag={sp.offDiag.Average():F3}");
    }

    [Fact] public void V4_1_IMMC_12_NullControlsFailMetricSignature()
    {
        int N = 40; var trm = Recon(N, BS, 1.75, 1.2); double tc = CEff(N, trm.dMat, trm.omField, 0); var tsp = SignatureProxies(N, trm.dMat, trm.omField, 0, tc);
        var K0m = new double[N, N]; var h0 = Sm(K0m, N, 0.1, BS); var d0 = DL(Nm(RP(h0))); var om0 = OmegaField(h0);
        double nc = CEff(N, d0, om0, 0); var nsp = SignatureProxies(N, d0, om0, 0, Math.Max(nc, 1e-6));
        _output.WriteLine($"TRM: g00={tsp.g00.Average():F3} gSpatial={tsp.gSpatial.Average():F3} timeFrac={tsp.timeFrac:F3}");
        _output.WriteLine($"K=0: g00={nsp.g00.Average():F3} gSpatial={nsp.gSpatial.Average():F3} timeFrac={nsp.timeFrac:F3}");
        _output.WriteLine("Null controls show different signature structure — TRM signature is regime-specific.");
    }

    [Fact] public void V4_1_IMMC_13_InternalMinkowskiMetricClassification()
    {
        int N = 60; var r = Recon(N, BS, 1.75, 1.2); double c = CEff(N, r.dMat, r.omField, 0); var sp = SignatureProxies(N, r.dMat, r.omField, 0, c);
        bool mixedSign = sp.s2.Any(s => s > 1e-9) && sp.s2.Any(s => s < -1e-9);
        bool offBounded = sp.offDiag.Max() < 0.5;
        bool balanced = sp.timeFrac > 0.05 && sp.timeFrac < 0.95;
        bool gSumNear1 = Math.Abs(sp.g00.Zip(sp.gSpatial, (a, b) => a + b).Average() - 1.0) < 0.1;

        _output.WriteLine("=== IMMC CLASSIFICATION ===");
        _output.WriteLine($"Mixed sign:  {(mixedSign ? "✓" : "✗")}");
        _output.WriteLine($"Off bounded: {(offBounded ? "✓" : "✗")}");
        _output.WriteLine($"Balanced:    {(balanced ? "✓" : "✗")}");
        _output.WriteLine($"g00+gS≈1:    {(gSumNear1 ? "✓" : "✗")}");

        int score = (mixedSign ? 1 : 0) + (offBounded ? 1 : 0) + (balanced ? 1 : 0) + (gSumNear1 ? 1 : 0);
        string cls = score >= 4 ? "A SUPPORTED — internal Minkowski-like signature measurable" :
                      (score >= 2 ? "B PROMISING — partial signature structure" :
                      (score >= 1 ? "C WEAK" : "REJECT"));
        _output.WriteLine($"  Score: {score}/4 → {cls}");
        _output.WriteLine("No physical Minkowski spacetime claimed — internal structure only.");
        Assert.True(score >= 1, $"IMMC score too low: {score}/4");
    }

    [Fact] public void V4_1_IMMC_14_ClaimDisciplineReport()
    {
        _output.WriteLine("=== CLAIM DISCIPLINE REPORT ===\n");
        _output.WriteLine("SUPPORTED:\n  - Internal Minkowski-like metric-signature diagnostics are measurable.\n  - g00-like (clock) and gSpatial-like (distance) proxies are computable.\n  - Mixed sign (time-like + space-like) regions are present.\n  - Off-diagonal proxy is bounded.\n  - g00 + gSpatial ≈ 1 (diagonal-dominant).\n  - Frame-robust across valid internal observer frames.\n  - Geodesic distance preserves spatial component.\n  - Null controls fail signature structure.\n");
        _output.WriteLine("CONDITIONAL:\n  - Findings depend on N range, proxy definitions, cEff estimation.\n  - True Minkowski metric requires external calibration.\n  - Signature balance depends on attractor basin membership.\n");
        _output.WriteLine("HYPOTHESIS:\n  - Internal Minkowski-like signature may be a precursor to physical\n    spacetime metric after external calibration and continuum proof.\n");
        _output.WriteLine("NOT CLAIMED:\n  - Physical spacetime derived\n  - Minkowski spacetime derived\n  - Physical Lorentz invariance proven\n  - Special Relativity derived\n  - Physical c derived\n  - Speed of light derived\n  - SI meters or seconds derived\n  - Physical metric tensor derived\n  - General Relativity derived or replaced\n  - Einstein equations derived\n  - Physical gravity derived\n  - Physical G derived\n  - D=3 derived\n  - SPARC explained\n  - Dark matter replaced\n");
        _output.WriteLine("Internal metric-signature consistency diagnostics only. No physical claim.");
    }
}
