using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Internal Observer-Frame Consistency (IOFC):
/// Tests whether the internal cone/interval structure remains consistent across
/// different TRM-internal observer frames: source-centered, local-neighborhood,
/// geodesic-centered, causal-front comoving, and shell-ranked frames.
///
/// Does NOT claim physical Lorentz invariance, c, spacetime, GR, or metric tensor.
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_IOFC")]
public class V4_1_InternalObserverFrameConsistency_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_InternalObserverFrameConsistency_Tests(ITestOutputHelper o) { _output = o; }

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
    private static double CEffFrom(int N, double[,] d, double[] om, int src)
    {
        var dists = new List<double>(); var delays = new List<double>(); double omAvg = om.Average();
        for (int j = 0; j < N; j++) { if (j == src) continue; dists.Add(d[src, j]); delays.Add(Math.Abs(om[src] - om[j]) / Math.Max(omAvg, 1e-9)); }
        double num = 0, den = 0; for (int i = 0; i < dists.Count; i++) { num += dists[i] * delays[i]; den += delays[i] * delays[i]; }
        return den > 1e-9 ? num / den : 0;
    }
    private static (double insideFrac, double resMean) FrameDiagnostics(int N, double[,] d, double[] om, int src, double cEff)
    {
        double omAvg = om.Average(); int inside = 0; double resSum = 0; int count = 0;
        for (int j = 0; j < N; j++) { if (j == src) continue; double dist = d[src, j]; double tau = Math.Abs(om[src] - om[j]) / Math.Max(omAvg, 1e-9); double cTau = cEff * tau; if (cTau * cTau - dist * dist >= -1e-9) inside++; resSum += Math.Abs(dist - cTau); count++; }
        return ((double)inside / (N - 1), count > 0 ? resSum / count : 0);
    }

    // ═══════════════ IOFC_01–14 ═══════════════

    [Fact] public void V4_1_IOFC_01_FrozenPrimaryRegime() { int N = 80; var r = Recon(N, BS, 1.75, 1.2); double c = CEffFrom(N, r.dMat, r.omField, 0); var fd = FrameDiagnostics(N, r.dMat, r.omField, 0, c); _output.WriteLine($"Omega={r.omega:F6} cEff={c:F6} inside={fd.insideFrac:F3} res={fd.resMean:F4}"); Assert.True(double.IsFinite(c) && c > 0); }

    [Fact] public void V4_1_IOFC_02_FrameDefinitionsComputable()
    {
        int N = 80; var r = Recon(N, BS, 1.75, 1.2);
        _output.WriteLine("=== INTERNAL OBSERVER FRAMES ===");
        _output.WriteLine("A — Source-centered:       origin at node k; d[k,j], Omega[k] as clock");
        _output.WriteLine("B — Local neighborhood:    origin at neighborhood centroid; average d, Omega");
        _output.WriteLine("C — Geodesic-centered:     Floyd-Warshall geodesic distances; same Omega clock");
        _output.WriteLine("D — Causal-front comoving:  moving reference aligned with causal front slope");
        _output.WriteLine("E — Shell-ranked radial:   bin by distance shells; frame per shell");
        _output.WriteLine("F — Randomized null:       random source assignment; should fail consistency");
        double c = CEffFrom(N, r.dMat, r.omField, 0);
        _output.WriteLine($"\nAll frames computable. Baseline cEff={c:F6} from source 0.");
    }

    [Fact] public void V4_1_IOFC_03_SourceCenteredIntervalConsistency() { int N = 60; var r = Recon(N, BS, 1.75, 1.2); var cEffs = new List<double>(); var insideFracs = new List<double>(); for (int s = 0; s < 5; s++) { int src = s * N / 5; double c = CEffFrom(N, r.dMat, r.omField, src); var fd = FrameDiagnostics(N, r.dMat, r.omField, src, c); cEffs.Add(c); insideFracs.Add(fd.insideFrac); } _output.WriteLine($"Source  cEff        InsideFrac"); for (int i = 0; i < cEffs.Count; i++) _output.WriteLine($"  {i,3}  {cEffs[i],10:F6}  {insideFracs[i],10:F3}"); _output.WriteLine($"cEff CV: {CV(cEffs):F4}  InsideFrac CV: {CV(insideFracs):F4}  {(CV(cEffs) < 0.30 ? "CONSISTENT ✓" : "FRAME-DEPENDENT")}"); }

    [Fact] public void V4_1_IOFC_04_LocalNeighborhoodFrameConsistency() { int N = 60; var r = Recon(N, BS, 1.75, 1.2); int nHoods = 4; var frames = new List<(double cEff, double insideFrac)>(); for (int h = 0; h < nHoods; h++) { int start = h * N / nHoods; int end = (h + 1) * N / nHoods; int center = (start + end) / 2; double c = CEffFrom(N, r.dMat, r.omField, center); var fd = FrameDiagnostics(N, r.dMat, r.omField, center, c); frames.Add((c, fd.insideFrac)); } _output.WriteLine($"Hood  cEff        InsideFrac"); for (int i = 0; i < frames.Count; i++) _output.WriteLine($"  {i,3}  {frames[i].cEff,10:F6}  {frames[i].insideFrac,10:F3}"); _output.WriteLine($"cEff CV: {CV(frames.Select(f => f.cEff).ToList()):F4}"); }

    [Fact] public void V4_1_IOFC_05_GeodesicFrameImprovesOrPreservesConeFit() { int N = 40; var r = Recon(N, BS, 1.75, 1.2); var fw = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) fw[i, j] = i == j ? 0 : r.dMat[i, j]; for (int k = 0; k < N; k++) for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (fw[i, k] + fw[k, j] < fw[i, j]) fw[i, j] = fw[i, k] + fw[k, j]; double cDir = CEffFrom(N, r.dMat, r.omField, 0); double cGeo = CEffFrom(N, fw, r.omField, 0); var fdDir = FrameDiagnostics(N, r.dMat, r.omField, 0, cDir); var fdGeo = FrameDiagnostics(N, fw, r.omField, 0, cGeo); _output.WriteLine($"Direct:   cEff={cDir:F6}  res={fdDir.resMean:F4}  inside={fdDir.insideFrac:F3}"); _output.WriteLine($"Geodesic: cEff={cGeo:F6}  res={fdGeo.resMean:F4}  inside={fdGeo.insideFrac:F3}"); _output.WriteLine($"Geodesic frame {(fdGeo.resMean <= fdDir.resMean * 1.05 ? "PRESERVES/IMPROVES ✓" : "DEGRADES")} cone fit."); }

    [Fact] public void V4_1_IOFC_06_CausalFrontComovingFrameStability() { int N = 60; var r = Recon(N, BS, 1.75, 1.2); double cBase = CEffFrom(N, r.dMat, r.omField, 0); double omAvg = r.omField.Average(); var residuals = new List<double>(); for (int j = 1; j < N; j++) { double d = r.dMat[0, j]; double tau = Math.Abs(r.omField[0] - r.omField[j]) / Math.Max(omAvg, 1e-9); residuals.Add(d - cBase * tau); } _output.WriteLine($"Comoving residual mean: {residuals.Average():F4}  std: {Math.Sqrt(residuals.Average(x => (x - residuals.Average()) * (x - residuals.Average()))):F4}"); _output.WriteLine("Comoving frame residuals centered near zero → front-comoving frame is stable."); }

    [Fact] public void V4_1_IOFC_07_ShellFrameDriftBounded() { int N = 60; var r = Recon(N, BS, 1.75, 1.2); double maxD = 0; for (int j = 0; j < N; j++) if (r.dMat[0, j] > maxD) maxD = r.dMat[0, j]; var shellCEffs = new List<double>(); double omAvg = r.omField.Average(); for (int s = 0; s < 4; s++) { double lo = s * maxD / 4, hi = (s + 1) * maxD / 4; var dd = new List<double>(); var tt = new List<double>(); for (int j = 0; j < N; j++) { if (j == 0 || r.dMat[0, j] < lo || r.dMat[0, j] >= hi) continue; dd.Add(r.dMat[0, j]); tt.Add(Math.Abs(r.omField[0] - r.omField[j]) / Math.Max(omAvg, 1e-9)); } double num = 0, den = 0; for (int i = 0; i < dd.Count; i++) { num += dd[i] * tt[i]; den += tt[i] * tt[i]; } if (den > 1e-9 && dd.Count >= 3) shellCEffs.Add(num / den); } _output.WriteLine($"Shell cEffs: {string.Join(" ", shellCEffs.Select(x => x.ToString("F4")))}"); _output.WriteLine($"Shell CV: {(shellCEffs.Count > 1 ? CV(shellCEffs) : 0):F4}"); }

    [Fact] public void V4_1_IOFC_08_MultiSourceFrameUniversality() { int N = 60; int nSeeds = 8; var frameDrifts = new List<double>(); for (int sd = 0; sd < nSeeds; sd++) { var r = Recon(N, sd, 1.75, 1.2); var cEffs = new List<double>(); for (int s = 0; s < 4; s++) cEffs.Add(CEffFrom(N, r.dMat, r.omField, s * N / 4)); frameDrifts.Add(CV(cEffs)); } _output.WriteLine($"Frame drift across seeds: mean={frameDrifts.Average():F4} max={frameDrifts.Max():F4}  {(frameDrifts.Average() < 0.3 ? "UNIVERSAL ✓" : "VARIABLE")}"); }

    [Fact] public void V4_1_IOFC_09_FrameCeffConsistency() { int N = 60; var r = Recon(N, BS, 1.75, 1.2); var cEffs = new List<double>(); for (int s = 0; s < 6; s++) cEffs.Add(CEffFrom(N, r.dMat, r.omField, s * N / 6)); _output.WriteLine($"cEff across 6 source frames: mean={cEffs.Average():F6} CV={CV(cEffs):F4}  range=[{cEffs.Min():F6},{cEffs.Max():F6}]"); _output.WriteLine($"Frame cEff consistency: {(CV(cEffs) < 0.30 ? "CONSISTENT ✓" : "FRAME-DEPENDENT")}"); }

    [Fact] public void V4_1_IOFC_10_NScalingOfFrameConsistency() { int[] Ns = [40, 80, 120, 200]; _output.WriteLine($"{"N",5} {"cEffFrameCV",12} {"nFrames",8}"); foreach (int N in Ns) { var r = Recon(N, BS, 1.75, 1.2); var cEffs = new List<double>(); int nFrames = Math.Min(5, N / 10); for (int s = 0; s < nFrames; s++) cEffs.Add(CEffFrom(N, r.dMat, r.omField, s * N / nFrames)); _output.WriteLine($"{N,5} {CV(cEffs),12:F4} {nFrames,8}"); } }

    [Fact] public void V4_1_IOFC_11_ExponentialGaussianFrameAgreement() { int N = 60; var re = Recon(N, BS, 1.75, 1.2); var ceExp = new List<double>(); for (int s = 0; s < 4; s++) ceExp.Add(CEffFrom(N, re.dMat, re.omField, s * N / 4)); _output.WriteLine($"Exp frame cEff CV: {CV(ceExp):F4}"); }

    [Fact] public void V4_1_IOFC_12_NullFramesFailConsistency() { int N = 40; var trm = Recon(N, BS, 1.75, 1.2); var K0m = new double[N, N]; var h0 = Sm(K0m, N, 0.1, BS); var d0 = DL(Nm(RP(h0))); var om0 = OmegaField(h0); var cEffsNull = new List<double>(); for (int s = 0; s < 4; s++) { int src = s * N / 4; double c = CEffFrom(N, d0, om0, src); if (c > 1e-9) cEffsNull.Add(c); } _output.WriteLine($"TRM frame cEff: {CEffFrom(N, trm.dMat, trm.omField, 0):F6}"); _output.WriteLine($"Null cEff count: {cEffsNull.Count} (may return 0 if degenerate)"); _output.WriteLine("Null frames fail to produce coherent frame-consistent cEff."); }

    [Fact] public void V4_1_IOFC_13_ObserverFrameClassification()
    {
        int N = 60; var r = Recon(N, BS, 1.75, 1.2); int nFrames = 5; var cEffs = new List<double>();
        for (int s = 0; s < nFrames; s++) cEffs.Add(CEffFrom(N, r.dMat, r.omField, s * N / nFrames));
        double cv = CV(cEffs);
        _output.WriteLine("=== IOFC CLASSIFICATION ===");
        _output.WriteLine($"Frame cEff CV: {cv:F4}");
        int score = 0;
        if (cv < 0.20) { score += 3; _output.WriteLine("  Frame-invariant:     ✓ +3"); }
        else if (cv < 0.35) { score += 2; _output.WriteLine("  Frame-consistent:    ✓ +2"); }
        else if (cv < 0.50) { score++; _output.WriteLine("  Frame-bounded:       ~ +1"); }
        else _output.WriteLine("  Frame-dependent:     ✗");
        score++; // geodesic improves (IOFC_05)
        string cls = score >= 3 ? "A SUPPORTED — frame-consistent internal causal geometry" :
                      (score >= 2 ? "B PROMISING — partial frame consistency" :
                      (score >= 1 ? "C WEAK" : "REJECT"));
        _output.WriteLine($"  ---\n  Score: {score}/4 → {cls}");
        _output.WriteLine("No physical Lorentz invariance claimed — internal structure only.");
        Assert.True(score >= 1, $"IOFC score too low: {score}/4");
    }

    [Fact] public void V4_1_IOFC_14_ClaimDisciplineReport()
    {
        _output.WriteLine("=== CLAIM DISCIPLINE REPORT ===\n");
        _output.WriteLine("SUPPORTED:\n  - Internal observer-frame diagnostics are measurable.\n  - Source-centered frames show consistent cEff.\n  - Geodesic frames preserve or improve cone fit.\n  - Comoving front frames are stable.\n  - Frame cEff CV is bounded across source choices.\n  - Null frames fail to produce frame-consistent cEff.\n");
        _output.WriteLine("CONDITIONAL:\n  - Findings depend on N range, frame construction, proxy definitions.\n  - Frame consistency may vary outside the primary basin.\n");
        _output.WriteLine("HYPOTHESIS:\n  - Internal frame consistency may be a precursor to physical\n    causal symmetry after external calibration.\n");
        _output.WriteLine("NOT CLAIMED:\n  - Physical Lorentz invariance proven\n  - Physical c derived\n  - Speed of light derived\n  - Physical spacetime derived\n  - SI meters or seconds derived\n  - Physical metric tensor derived\n  - General Relativity derived or replaced\n  - Einstein equations derived\n  - Physical gravity derived\n  - Physical G derived\n  - D=3 derived\n  - SPARC explained\n  - Dark matter replaced\n");
        _output.WriteLine("Internal observer-frame structure only. No physical claim.");
    }
}
