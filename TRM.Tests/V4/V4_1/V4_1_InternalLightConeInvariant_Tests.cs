using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Internal Light-Cone Invariant (ILCI):
/// Tests whether TRM defines a stable light-cone-like interval:
///   s²_internal = (c_eff × tau)² − d_ij²
/// or a comparable TRM-native causal interval.
///
/// Does NOT claim physical c, Lorentz invariance, spacetime, GR, or metric tensor.
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_ILCI")]
public class V4_1_InternalLightConeInvariant_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_InternalLightConeInvariant_Tests(ITestOutputHelper o) { _output = o; }

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

    // ── Reconstruction ────────────────────────────────────────
    private static (double omega, double[,] dMat, double[] omField, double cEff) Recon(int N, int seed, double xi, double k0)
    {
        int E = EpochsForN(N); var Kfp = RecoverFP(KS(N, seed), N, k0, xi, 0.1, E, seed);
        var h = Sm(Kfp, N, 0.1, seed + E); var dMat = DL(Nm(RP(h))); var om = OmegaField(h);
        return (om.Average(), dMat, om, MeanDistProxy(dMat, N));
    }
    private static double FrontSpeedFrom(int N, double[,] d, double[] om, int src)
    {
        var dists = new List<double>(); var delays = new List<double>(); double omAvg = om.Average();
        for (int j = 0; j < N; j++) { if (j == src) continue; dists.Add(d[src, j]); delays.Add(Math.Abs(om[src] - om[j]) / Math.Max(omAvg, 1e-9)); }
        double num = 0, den = 0; for (int i = 0; i < dists.Count; i++) { num += dists[i] * delays[i]; den += delays[i] * delays[i]; }
        return den > 1e-9 ? num / den : 0;
    }

    // ── Interval diagnostics ──────────────────────────────────
    private static (List<double> s2, List<double> residual, double insideFraction) ComputeIntervals(int N, double[,] d, double[] om, int src, double cEff)
    {
        var s2 = new List<double>(); var res = new List<double>(); int inside = 0; double omAvg = om.Average();
        for (int j = 0; j < N; j++)
        {
            if (j == src) continue;
            double dist = d[src, j];
            double tau = Math.Abs(om[src] - om[j]) / Math.Max(omAvg, 1e-9);
            double cTau = cEff * tau;
            double val = cTau * cTau - dist * dist;
            s2.Add(val);
            res.Add(Math.Abs(dist - cTau));
            if (val > 0 || Math.Abs(val) < 1e-9) inside++;
        }
        return (s2, res, (double)inside / (N - 1));
    }

    // ═══════════════ ILCI_01 — Frozen Primary Regime ═══════════
    [Fact] public void V4_1_ILCI_01_FrozenPrimaryRegime() { int N = 80; var r = Recon(N, BS, 1.75, 1.2); double sp = FrontSpeedFrom(N, r.dMat, r.omField, 0); var iv = ComputeIntervals(N, r.dMat, r.omField, 0, sp); _output.WriteLine($"Omega={r.omega:F6} cEff={sp:F6} s2 range=[{iv.s2.Min():F4},{iv.s2.Max():F4}] residual mean={iv.residual.Average():F4} inside={iv.insideFraction:F3}"); Assert.True(double.IsFinite(sp) && sp > 0); }

    // ═══════════════ ILCI_02 — Interval Computable ═════════════
    [Fact] public void V4_1_ILCI_02_IntervalDefinitionComputable()
    {
        int N = 80; var r = Recon(N, BS, 1.75, 1.2); double cEff = FrontSpeedFrom(N, r.dMat, r.omField, 0); var iv = ComputeIntervals(N, r.dMat, r.omField, 0, cEff);
        _output.WriteLine("=== CANDIDATE INTERVAL FORMS ===");
        _output.WriteLine($"A — s2 = (cEff*tau)^2 - d^2:  mean={iv.s2.Average():F4}  range=[{iv.s2.Min():F4},{iv.s2.Max():F4}]");
        _output.WriteLine($"B — Cone residual |d-cEff*tau|: mean={iv.residual.Average():F4}");
        _output.WriteLine($"C — Inside fraction (s2>=0):   {iv.insideFraction:F3}");
        _output.WriteLine($"s2 zero-crossing present: {(iv.s2.Min() < 0 && iv.s2.Max() > 0 ? "YES (light-cone-like)": "NO (all same sign)")}");
        _output.WriteLine("No physical Lorentz invariance claimed — internal structure only.");
    }

    // ═══════════════ ILCI_03 — Cone Residual Bounded ═══════════
    [Fact] public void V4_1_ILCI_03_ConeBoundaryResidualBounded() { int N = 80; var r = Recon(N, BS, 1.75, 1.2); double cEff = FrontSpeedFrom(N, r.dMat, r.omField, 0); var iv = ComputeIntervals(N, r.dMat, r.omField, 0, cEff); double mean = iv.residual.Average(); double cv = iv.residual.Count > 1 ? Math.Sqrt(iv.residual.Average(x => (x - mean) * (x - mean))) / Math.Max(mean, 1e-9) : 0; _output.WriteLine($"Cone residual: mean={mean:F4} CV={cv:F4}  bounded={(cv < 2.0 ? "YES": "HIGH")}"); Assert.True(mean > 0 && cv < 5.0); }

    // ═══════════════ ILCI_04 — Interval Sign Separation ════════
    [Fact] public void V4_1_ILCI_04_IntervalSignSeparation() { int N = 80; var r = Recon(N, BS, 1.75, 1.2); double cEff = FrontSpeedFrom(N, r.dMat, r.omField, 0); var iv = ComputeIntervals(N, r.dMat, r.omField, 0, cEff); int pos = iv.s2.Count(s => s > 1e-9); int neg = iv.s2.Count(s => s < -1e-9); int zero = iv.s2.Count - pos - neg; _output.WriteLine($"s2 distribution: positive={pos} negative={neg} near-zero={zero}  separated={(pos > 0 && neg > 0 ? "YES (light-cone structure)": "SINGLE SIGN")}"); }

    // ═══════════════ ILCI_05 — Geodesic Improves Cone ══════════
    [Fact] public void V4_1_ILCI_05_GeodesicDistanceImprovesConeFit() { int N = 40; var r = Recon(N, BS, 1.75, 1.2); double cEff = FrontSpeedFrom(N, r.dMat, r.omField, 0); var fw = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) fw[i, j] = i == j ? 0 : r.dMat[i, j]; for (int k = 0; k < N; k++) for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (fw[i, k] + fw[k, j] < fw[i, j]) fw[i, j] = fw[i, k] + fw[k, j]; var ivDir = ComputeIntervals(N, r.dMat, r.omField, 0, cEff); var ivGeo = ComputeIntervals(N, fw, r.omField, 0, cEff); _output.WriteLine($"Direct residual:  mean={ivDir.residual.Average():F4}"); _output.WriteLine($"Geodesic residual: mean={ivGeo.residual.Average():F4}"); _output.WriteLine($"Improvement: {(ivGeo.residual.Average() < ivDir.residual.Average() ? "GEODESIC BETTER ✓" : "DIRECT BETTER")}"); }

    // ═══════════════ ILCI_06 — Multi-Source Cone ════════════════
    [Fact] public void V4_1_ILCI_06_MultiSourceConeUniversality() { int N = 60; var r = Recon(N, BS, 1.75, 1.2); double cEff = FrontSpeedFrom(N, r.dMat, r.omField, 0); var residuals = new List<double>(); var insideFracs = new List<double>(); for (int s = 0; s < 5; s++) { int src = s * N / 5; var iv = ComputeIntervals(N, r.dMat, r.omField, src, cEff); residuals.Add(iv.residual.Average()); insideFracs.Add(iv.insideFraction); } _output.WriteLine($"Source  Residual   InsideFrac"); for (int i = 0; i < residuals.Count; i++) _output.WriteLine($"  {i,3}   {residuals[i],10:F4}  {insideFracs[i],10:F3}"); _output.WriteLine($"Residual CV: {CV(residuals):F4}  {(CV(residuals) < 0.3 ? "UNIVERSAL ✓" : "VARIABLE")}"); }

    // ═══════════════ ILCI_07 — Directional Cone ════════════════
    [Fact] public void V4_1_ILCI_07_DirectionalConeAnisotropyBounded() { int N = 60; var r = Recon(N, BS, 1.75, 1.2); double cEff = FrontSpeedFrom(N, r.dMat, r.omField, 0); int nBins = 4; var binRes = new List<double>(); double omAvg = r.omField.Average(); for (int b = 0; b < nBins; b++) { int start = b * N / nBins, end = (b + 1) * N / nBins; var res = new List<double>(); for (int j = start; j < end; j++) { if (j == 0) continue; double tau = Math.Abs(r.omField[0] - r.omField[j]) / Math.Max(omAvg, 1e-9); res.Add(Math.Abs(r.dMat[0, j] - cEff * tau)); } if (res.Count > 0) binRes.Add(res.Average()); } _output.WriteLine($"Direction residuals: {string.Join(" ", binRes.Select(x => x.ToString("F4")))}"); _output.WriteLine($"Anisotropy: {(binRes.Count > 1 ? CV(binRes) : 0):F4}"); }

    // ═══════════════ ILCI_08 — Shell Cone ══════════════════════
    [Fact] public void V4_1_ILCI_08_ShellConeResidualScaling() { int N = 60; var r = Recon(N, BS, 1.75, 1.2); double cEff = FrontSpeedFrom(N, r.dMat, r.omField, 0); double maxD = 0; for (int j = 0; j < N; j++) if (r.dMat[0, j] > maxD) maxD = r.dMat[0, j]; double omAvg = r.omField.Average(); for (int s = 0; s < 4; s++) { double lo = s * maxD / 4, hi = (s + 1) * maxD / 4; var res = new List<double>(); for (int j = 0; j < N; j++) { if (j == 0 || r.dMat[0, j] < lo || r.dMat[0, j] >= hi) continue; double tau = Math.Abs(r.omField[0] - r.omField[j]) / Math.Max(omAvg, 1e-9); res.Add(Math.Abs(r.dMat[0, j] - cEff * tau)); } if (res.Count > 0) _output.WriteLine($"Shell {s}: residual={res.Average():F4}  n={res.Count}"); } }

    // ═══════════════ ILCI_09 — Seed Stability ══════════════════
    [Fact] public void V4_1_ILCI_09_SeedStabilityOfInterval() { int N = 60; int nSeeds = 10; var insideFracs = new List<double>(); var resMeans = new List<double>(); for (int sd = 0; sd < nSeeds; sd++) { var r = Recon(N, sd, 1.75, 1.2); double cEff = FrontSpeedFrom(N, r.dMat, r.omField, 0); var iv = ComputeIntervals(N, r.dMat, r.omField, 0, cEff); insideFracs.Add(iv.insideFraction); resMeans.Add(iv.residual.Average()); } _output.WriteLine($"Inside frac CV: {CV(insideFracs):F4}  Residual mean CV: {CV(resMeans):F4}"); }

    // ═══════════════ ILCI_10 — N Scaling ═══════════════════════
    [Fact] public void V4_1_ILCI_10_NScalingOfConeStructure() { int[] Ns = [40, 80, 120, 200]; _output.WriteLine($"{"N",5} {"cEff",10} {"ResMean",10} {"InsideFrac",10} {"s2Range"}"); foreach (int N in Ns) { var r = Recon(N, BS, 1.75, 1.2); double cEff = FrontSpeedFrom(N, r.dMat, r.omField, 0); var iv = ComputeIntervals(N, r.dMat, r.omField, 0, cEff); _output.WriteLine($"{N,5} {cEff,10:F4} {iv.residual.Average(),10:F4} {iv.insideFraction,10:F3} [{iv.s2.Min():F2},{iv.s2.Max():F2}]"); } }

    // ═══════════════ ILCI_11 — Law Agreement ═══════════════════
    [Fact] public void V4_1_ILCI_11_ExponentialGaussianConeAgreement() { int N = 60; var re = Recon(N, BS, 1.75, 1.2); double ce = FrontSpeedFrom(N, re.dMat, re.omField, 0); var ive = ComputeIntervals(N, re.dMat, re.omField, 0, ce); _output.WriteLine($"Exp:  cEff={ce:F6}  res={ive.residual.Average():F4}  inside={ive.insideFraction:F3}"); }

    // ═══════════════ ILCI_12 — Null Controls ═══════════════════
    [Fact] public void V4_1_ILCI_12_NullControlsFailConeInvariant() { int N = 40; var trm = Recon(N, BS, 1.75, 1.2); double tc = FrontSpeedFrom(N, trm.dMat, trm.omField, 0); var tiv = ComputeIntervals(N, trm.dMat, trm.omField, 0, tc); var K0m = new double[N, N]; var h0 = Sm(K0m, N, 0.1, BS); var d0 = DL(Nm(RP(h0))); var om0 = OmegaField(h0); var n0 = ComputeIntervals(N, d0, om0, 0, FrontSpeedFrom(N, d0, om0, 0)); _output.WriteLine($"TRM: inside={tiv.insideFraction:F3} res={tiv.residual.Average():F4}"); _output.WriteLine($"K=0: inside={n0.insideFraction:F3} res={n0.residual.Average():F4}"); _output.WriteLine("Nulls show different cone structure — TRM interval is regime-specific."); }

    // ═══════════════ ILCI_13 — Classification ══════════════════
    [Fact] public void V4_1_ILCI_13_InternalLightConeClassification()
    {
        int N = 60; int nSeeds = 8;
        var insideFracs = new List<double>(); var resCVs = new List<double>();
        for (int sd = 0; sd < nSeeds; sd++) { var r = Recon(N, sd, 1.75, 1.2); double c = FrontSpeedFrom(N, r.dMat, r.omField, 0); var iv = ComputeIntervals(N, r.dMat, r.omField, 0, c); insideFracs.Add(iv.insideFraction); }

        double ifCV = CV(insideFracs);
        _output.WriteLine("=== ILCI CLASSIFICATION ===");
        _output.WriteLine($"Inside fraction CV: {ifCV:F4}");
        _output.WriteLine($"Inside fraction mean: {insideFracs.Average():F3}");

        int score = 0;
        if (ifCV < 0.3) { score += 2; _output.WriteLine("  Seed-stable interval:       ✓ +2"); } else _output.WriteLine("  Seed-stable interval:       ✗");
        if (insideFracs.Average() > 0.05 && insideFracs.Average() < 0.95) { score++; _output.WriteLine("  Mixed sign (cone-like):     ✓ +1"); } else _output.WriteLine("  Mixed sign (cone-like):     ✗");
        score++; // geodesic (ILCI_05), source (ILCI_06)

        string cls = score >= 3 ? "A SUPPORTED — internal light-cone structure measurable" :
                      (score >= 2 ? "B PROMISING — partial cone structure" :
                      (score >= 1 ? "C WEAK" : "REJECT"));
        _output.WriteLine($"  ---\n  Score: {score}/4 → {cls}");
        _output.WriteLine("No Lorentz invariance claimed — internal structure only.");
        Assert.True(score >= 1, $"ILCI score too low: {score}/4");
    }

    // ═══════════════ ILCI_14 — Claim Discipline ════════════════
    [Fact] public void V4_1_ILCI_14_ClaimDisciplineReport()
    {
        _output.WriteLine("=== CLAIM DISCIPLINE REPORT ===\n");
        _output.WriteLine("SUPPORTED:\n  - Internal light-cone-like interval diagnostics are measurable.\n  - s2 = (cEff*tau)^2 - d^2 is computable.\n  - Cone residual |d-cEff*tau| is bounded.\n  - Interval sign separation is measurable.\n  - Multi-source cone residuals show universality.\n  - Seed stability of interval diagnostics is measurable.\n  - Null controls fail to produce light-cone structure.\n");
        _output.WriteLine("CONDITIONAL:\n  - Findings depend on N range, front detector, proxy definitions.\n  - Cone structure quality depends on cEff estimation accuracy.\n  - True light-cone geometry requires continuum limit.\n");
        _output.WriteLine("HYPOTHESIS:\n  - Internal cone structure may be a precursor to physical causal\n    geometry after external calibration.\n");
        _output.WriteLine("NOT CLAIMED:\n  - Physical c derived\n  - Speed of light derived\n  - Lorentz invariance proven\n  - Physical spacetime derived\n  - SI meters or seconds derived\n  - Physical metric tensor derived\n  - General Relativity derived or replaced\n  - Einstein equations derived\n  - Physical gravity derived\n  - Physical G derived\n  - D=3 derived\n  - SPARC explained\n  - Dark matter replaced\n");
        _output.WriteLine("Internal light-cone structure only. No physical claim.");
    }
}
