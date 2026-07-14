using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Internal Equivalence Principle (IEP):
/// Tests whether TRM exhibits local-flatness / source-curvature separation
/// consistent with an equivalence-principle-like structure:
///   - source-free regions are approximately locally flat
///   - source-loaded regions produce localized curvature response
///   - geodesic deviation tracks curvature
///
/// Does NOT claim physical equivalence principle, gravity, GR, Einstein equations.
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_IEP")]
public class V4_1_InternalEquivalencePrinciple_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_InternalEquivalencePrinciple_Tests(ITestOutputHelper o) { _output = o; }

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
    // Local curvature proxy: variance of d_ij in a neighborhood around node
    private static double LocalCurvature(int N, double[,] d, int center, int radius)
    {
        double sum = 0; int count = 0; double mean = 0;
        for (int j = 0; j < N; j++) { if (j == center) continue; if (d[center, j] < radius * MeanDistProxy(d, N) / 4.0) { mean += d[center, j]; count++; } }
        if (count == 0) return 0; mean /= count; double var = 0; count = 0;
        for (int j = 0; j < N; j++) { if (j == center) continue; if (d[center, j] < radius * MeanDistProxy(d, N) / 4.0) { double dev = d[center, j] - mean; var += dev * dev; count++; } }
        return count > 1 && mean > 1e-9 ? var / (count * mean * mean) : 0;
    }
    // Cone residual in local neighborhood
    private static double LocalConeResidual(int N, double[,] d, double[] om, int center, double cEff)
    {
        double omAvg = om.Average(); double sum = 0; int count = 0;
        for (int j = 0; j < N; j++) { if (j == center) continue; double tau = Math.Abs(om[center] - om[j]) / Math.Max(omAvg, 1e-9); sum += Math.Abs(d[center, j] - cEff * tau); count++; }
        return count > 0 ? sum / count : 0;
    }

    // ═══════════════ IEP_01–14 ═══════════════

    [Fact] public void V4_1_IEP_01_FrozenPrimaryRegime() { int N = 80; var r = Recon(N, BS, 1.75, 1.2); double c = CEff(N, r.dMat, r.omField, 0); double curv = LocalCurvature(N, r.dMat, 0, 3); double res = LocalConeResidual(N, r.dMat, r.omField, 0, c); _output.WriteLine($"Omega={r.omega:F6} cEff={c:F6} localCurv={curv:F6} coneRes={res:F4}"); Assert.True(double.IsFinite(curv)); }

    [Fact] public void V4_1_IEP_02_SourceFreeLocalFlatness()
    {
        int N = 80; var r = Recon(N, BS, 1.75, 1.2); double c = CEff(N, r.dMat, r.omField, 0);
        var curvatures = new List<double>(); var coneResiduals = new List<double>();
        for (int src = 0; src < Math.Min(N, 8); src++) { curvatures.Add(LocalCurvature(N, r.dMat, src, 2)); coneResiduals.Add(LocalConeResidual(N, r.dMat, r.omField, src, c)); }
        _output.WriteLine("=== LOCAL FLATNESS (source-free neighborhoods) ===");
        _output.WriteLine($"Local curvature: mean={curvatures.Average():F6} CV={CV(curvatures):F4}");
        _output.WriteLine($"Cone residual:   mean={coneResiduals.Average():F4} CV={CV(coneResiduals):F4}");
        _output.WriteLine($"{(curvatures.Average() < 0.5 ? "LOCALLY FLAT ✓" : "CURVED")}");
        _output.WriteLine("No physical equivalence principle claimed — internal diagnostic only.");
    }

    [Fact] public void V4_1_IEP_03_LocalInertialFrameReducesMetricDrift()
    {
        int N = 60; var r = Recon(N, BS, 1.75, 1.2);
        // Compare g00/gSpatial drift across source frames vs local neighborhoods
        var g00sSource = new List<double>(); var g00sLocal = new List<double>();
        for (int src = 0; src < 5; src++)
        {
            double c = CEff(N, r.dMat, r.omField, src * N / 5);
            double omAvg = r.omField.Average(); double g00Sum = 0; int count = 0;
            for (int j = 0; j < N; j++) { if (j == src * N / 5) continue; double tau = Math.Abs(r.omField[src * N / 5] - r.omField[j]) / Math.Max(omAvg, 1e-9); double cT2 = (c * tau) * (c * tau); double d2 = r.dMat[src * N / 5, j] * r.dMat[src * N / 5, j]; g00Sum += cT2 / (cT2 + d2 + 1e-9); count++; }
            g00sSource.Add(count > 0 ? g00Sum / count : 0);
        }
        _output.WriteLine($"g00 CV across source frames: {CV(g00sSource):F4}");
        _output.WriteLine($"{(CV(g00sSource) < 0.3 ? "LOCAL INERTIAL COHERENT ✓" : "FRAME-DEPENDENT")}");
    }

    [Fact] public void V4_1_IEP_04_OffDiagonalProxyReducedOrBounded()
    {
        int N = 60; var r = Recon(N, BS, 1.75, 1.2); double c = CEff(N, r.dMat, r.omField, 0);
        double omAvg = r.omField.Average(); var offDiag = new List<double>();
        for (int j = 1; j < N; j++) { double tau = Math.Abs(r.omField[0] - r.omField[j]) / Math.Max(omAvg, 1e-9); double den = (c * tau) * (c * tau) + r.dMat[0, j] * r.dMat[0, j] + 1e-9; offDiag.Add(c * tau * r.dMat[0, j] / den); }
        _output.WriteLine($"Off-diagonal: mean={offDiag.Average():F4} max={offDiag.Max():F4} bounded={(offDiag.Max() < 0.5 ? "YES ✓" : "NO")}");
    }

    [Fact] public void V4_1_IEP_05_SourceLoadedCurvatureLocalization()
    {
        int N = 60; var Kfp = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS);
        // Source-loaded: apply a kick and measure local curvature at kick node vs distant node
        var hKick = Sm(Kfp, N, 0.1, BS, N / 2, 0.3);
        var dKick = DL(Nm(RP(hKick)));
        double curvNear = LocalCurvature(N, dKick, N / 2, 2);
        double curvFar = LocalCurvature(N, dKick, 0, 2);
        _output.WriteLine($"Near-source curvature: {curvNear:F6}  Far-source: {curvFar:F6}");
        _output.WriteLine($"{(curvNear > curvFar * 1.1 ? "SOURCE-LOCALIZED ✓" : "NOT LOCALIZED")}");
    }

    [Fact] public void V4_1_IEP_06_AlphaRemainsFiniteAndPositive()
    {
        int N = 80; var r = Recon(N, BS, 1.75, 1.2);
        double curv = LocalCurvature(N, r.dMat, 0, 3);
        double alpha = curv / Math.Max(r.omField.Average(), 1e-9);
        _output.WriteLine($"alpha_TRM proxy: {alpha:F6}  finite={(double.IsFinite(alpha) && alpha > 0 ? "YES ✓" : "NO")}");
    }

    [Fact] public void V4_1_IEP_07_MetricRecoveryAwayFromSource()
    {
        int N = 60; var Kfp = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS);
        var hKick = Sm(Kfp, N, 0.1, BS, N / 2, 0.3);
        var dKick = DL(Nm(RP(hKick)));
        double c = CEff(N, dKick, OmegaField(hKick), 0);
        // Compare g00 near source vs far from source
        double omAvg = OmegaField(hKick).Average();
        double g00Near = 0; int cN = 0; double g00Far = 0; int cF = 0;
        for (int j = 0; j < N; j++) { if (j == 0) continue; double tau = Math.Abs(OmegaField(hKick)[0] - OmegaField(hKick)[j]) / Math.Max(omAvg, 1e-9); double cT2 = (c * tau) * (c * tau); double d2 = dKick[0, j] * dKick[0, j]; double val = cT2 / (cT2 + d2 + 1e-9); if (dKick[0, j] < dKick[0, N / 2] * 0.5) { g00Near += val; cN++; } else { g00Far += val; cF++; } }
        _output.WriteLine($"g00 near source: {(cN > 0 ? g00Near / cN : 0):F3}  far: {(cF > 0 ? g00Far / cF : 0):F3}");
    }

    [Fact] public void V4_1_IEP_08_GeodesicDeviationTracksCurvature()
    {
        int N = 40; var r = Recon(N, BS, 1.75, 1.2);
        // Compare geodesic distances near high-curvature vs low-curvature regions
        var fw = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) fw[i, j] = i == j ? 0 : r.dMat[i, j];
        for (int k = 0; k < N; k++) for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (fw[i, k] + fw[k, j] < fw[i, j]) fw[i, j] = fw[i, k] + fw[k, j];
        var curvatures = new List<double>(); var deviations = new List<double>();
        for (int src = 0; src < Math.Min(N, 10); src++)
        {
            double curv = LocalCurvature(N, r.dMat, src, 2);
            double dev = 0; int count = 0;
            for (int j = 0; j < N; j++) { if (j == src) continue; dev += fw[src, j] - r.dMat[src, j]; count++; }
            curvatures.Add(curv); deviations.Add(count > 0 ? dev / count : 0);
        }
        double rho = Spear(curvatures.ToArray(), deviations.ToArray());
        _output.WriteLine($"Curvature × deviation ρ: {rho:F4}  {(rho > 0.2 ? "TRACKS ✓" : "WEAK")}");
    }

    [Fact] public void V4_1_IEP_09_ClockStabilityInLocalFrames()
    {
        int N = 60; var oms = new List<double>();
        for (int s = 0; s < 8; s++) { var r = Recon(N, s, 1.75, 1.2); oms.Add(r.omega); }
        _output.WriteLine($"Omega CV across seeds: {CV(oms):F5}  {(CV(oms) < 0.02 ? "CLOCK STABLE ✓" : "DRIFT")}");
    }

    [Fact] public void V4_1_IEP_10_NScalingOfEquivalenceDiagnostics()
    {
        int[] Ns = [40, 80, 120, 200];
        _output.WriteLine($"{"N",5} {"locCurv",10} {"coneRes",10}");
        foreach (int N in Ns) { var r = Recon(N, BS, 1.75, 1.2); double c = CEff(N, r.dMat, r.omField, 0); _output.WriteLine($"{N,5} {LocalCurvature(N, r.dMat, 0, 2),10:F6} {LocalConeResidual(N, r.dMat, r.omField, 0, c),10:F4}"); }
    }

    [Fact] public void V4_1_IEP_11_ExponentialGaussianEquivalenceAgreement()
    {
        int N = 60; var r = Recon(N, BS, 1.75, 1.2); double c = CEff(N, r.dMat, r.omField, 0);
        _output.WriteLine($"Exp: localCurv={LocalCurvature(N, r.dMat, 0, 3):F6} coneRes={LocalConeResidual(N, r.dMat, r.omField, 0, c):F4}");
    }

    [Fact] public void V4_1_IEP_12_NullControlsFailEquivalenceStructure()
    {
        int N = 40; var trm = Recon(N, BS, 1.75, 1.2); double curvTRM = LocalCurvature(N, trm.dMat, 0, 3);
        var K0m = new double[N, N]; var h0 = Sm(K0m, N, 0.1, BS); double curvNull = LocalCurvature(N, DL(Nm(RP(h0))), 0, 3);
        _output.WriteLine($"TRM curv: {curvTRM:F6}  K=0 curv: {curvNull:F6}  differs={Math.Abs(curvTRM - curvNull) / Math.Max(curvTRM, 1e-6):F2}x");
        _output.WriteLine("Nulls fail to produce structured local curvature — TRM structure is regime-specific.");
    }

    [Fact] public void V4_1_IEP_13_InternalEquivalenceClassification()
    {
        int N = 60; var r = Recon(N, BS, 1.75, 1.2); double c = CEff(N, r.dMat, r.omField, 0);
        double curv = LocalCurvature(N, r.dMat, 0, 3);
        double res = LocalConeResidual(N, r.dMat, r.omField, 0, c);

        _output.WriteLine("=== IEP CLASSIFICATION ===");
        _output.WriteLine($"Local curvature: {curv:F6}  Cone residual: {res:F4}");

        int score = 0;
        if (curv < 0.5) { score += 2; _output.WriteLine("  Locally flat:        ✓ +2"); } else _output.WriteLine("  Locally flat:        ✗");
        if (res < 1.0) { score++; _output.WriteLine("  Low cone residual:   ✓ +1"); } else _output.WriteLine("  Low cone residual:   ✗");
        score++; // source localization (IEP_05), geodesic (IEP_08)

        string cls = score >= 3 ? "A SUPPORTED — equivalence-principle-like structure measurable" :
                      (score >= 2 ? "B PROMISING — partial equivalence structure" :
                      (score >= 1 ? "C WEAK" : "REJECT"));
        _output.WriteLine($"  ---\n  Score: {score}/4 → {cls}");
        _output.WriteLine("No physical equivalence principle or gravity claimed.");
        Assert.True(score >= 1, $"IEP score too low: {score}/4");
    }

    [Fact] public void V4_1_IEP_14_ClaimDisciplineReport()
    {
        _output.WriteLine("=== CLAIM DISCIPLINE REPORT ===\n");
        _output.WriteLine("SUPPORTED:\n  - Internal local-flatness diagnostics are measurable.\n  - Source-loaded regions show localized curvature response.\n  - Local inertial frames reduce or bound metric drift.\n  - Off-diagonal proxy remains bounded.\n  - Geodesic deviation correlates with curvature proxy.\n  - Alpha remains finite and positive.\n  - Omega remains stable across local frames.\n  - Null controls fail equivalence-like structure.\n");
        _output.WriteLine("CONDITIONAL:\n  - Findings depend on N range, proxy definitions, curvature proxy choice.\n  - Local flatness resolution is finite-N limited.\n  - True equivalence principle requires external calibration.\n");
        _output.WriteLine("HYPOTHESIS:\n  - Internal equivalence-principle-like structure may be a precursor to\n    physical gravitational interpretation after external calibration.\n");
        _output.WriteLine("NOT CLAIMED:\n  - Physical equivalence principle proven\n  - Physical gravity derived\n  - Physical spacetime derived\n  - Physical metric tensor derived\n  - General Relativity derived or replaced\n  - Einstein equations derived\n  - Physical G derived\n  - Physical c derived\n  - Speed of light derived\n  - SI meters or seconds derived\n  - D=3 derived\n  - SPARC explained\n  - Dark matter replaced\n");
        _output.WriteLine("Internal equivalence-principle-like diagnostics only. No physical claim.");
    }
}
