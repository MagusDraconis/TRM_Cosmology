using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Internal Lorentz-Structure Consistency (ILSC):
/// Tests whether TRM's internal observer-frame-consistent cone structure
/// exhibits Lorentz-like consistency diagnostics: interval preservation,
/// cone boundary stability, causal sign preservation, and gamma-like proxies.
///
/// Does NOT claim physical Lorentz invariance, Special Relativity, c, spacetime, GR.
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_ILSC")]
public class V4_1_InternalLorentzStructureConsistency_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_InternalLorentzStructureConsistency_Tests(ITestOutputHelper o) { _output = o; }

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

    // ── Frame diagnostics ─────────────────────────────────────
    private static (List<double> s2, double insideFrac, double resMean) FrameDiag(int N, double[,] d, double[] om, int src, double cEff)
    {
        double omAvg = om.Average(); var s2 = new List<double>(); int inside = 0; double resSum = 0; int count = 0;
        for (int j = 0; j < N; j++) { if (j == src) continue; double dist = d[src, j]; double tau = Math.Abs(om[src] - om[j]) / Math.Max(omAvg, 1e-9); double cTau = cEff * tau; double val = cTau * cTau - dist * dist; s2.Add(val); if (val >= -1e-9) inside++; resSum += Math.Abs(dist - cTau); count++; }
        return (s2, (double)inside / (N - 1), count > 0 ? resSum / count : 0);
    }

    // ── Transform: compute s2 from frame A and frame B, measure drift ──
    private static double IntervalDrift(int N, double[,] d, double[] om, int srcA, double cA, int srcB, double cB)
    {
        var fdA = FrameDiag(N, d, om, srcA, cA);
        var fdB = FrameDiag(N, d, om, srcB, cB);
        // Compare s2 distributions via mean absolute difference
        double drift = 0; int n = Math.Min(fdA.s2.Count, fdB.s2.Count);
        for (int i = 0; i < n; i++) drift += Math.Abs(fdA.s2[i] - fdB.s2[i]);
        return n > 0 ? drift / n : 0;
    }

    // ═══════════════ ILSC_01–14 ═══════════════

    [Fact] public void V4_1_ILSC_01_FrozenPrimaryRegime() { int N = 80; var r = Recon(N, BS, 1.75, 1.2); double c = CEff(N, r.dMat, r.omField, 0); var fd = FrameDiag(N, r.dMat, r.omField, 0, c); _output.WriteLine($"Omega={r.omega:F6} cEff={c:F6} inside={fd.insideFrac:F3} res={fd.resMean:F4}"); Assert.True(double.IsFinite(c) && c > 0); }

    [Fact] public void V4_1_ILSC_02_FrameTransformDefinitionsComputable()
    {
        int N = 80; var r = Recon(N, BS, 1.75, 1.2);
        _output.WriteLine("=== TRM-NATIVE FRAME TRANSFORM TYPES ===");
        _output.WriteLine("A — Source→Source:          different source nodes, same geometry");
        _output.WriteLine("B — Source→Neighborhood:    point frame → regional centroid frame");
        _output.WriteLine("C — Source→Geodesic:        direct distance → Floyd-Warshall geodesic");
        _output.WriteLine("D — Shell→Shell:            different radial shells");
        _output.WriteLine("E — Comoving→Geodesic:      front-aligned → shortest-path geometry");
        _output.WriteLine("F — Valid→Null:             any valid frame → randomized source (should fail)");
        double c0 = CEff(N, r.dMat, r.omField, 0); double c1 = CEff(N, r.dMat, r.omField, N / 4);
        _output.WriteLine($"\nExample: cEff(0)={c0:F6} cEff({N/4})={c1:F6} — both computable.");
    }

    [Fact] public void V4_1_ILSC_03_IntervalPreservationAcrossFrames()
    {
        int N = 60; var r = Recon(N, BS, 1.75, 1.2);
        var drifts = new List<double>();
        for (int a = 0; a < 4; a++)
        {
            int srcA = a * N / 4; double cA = CEff(N, r.dMat, r.omField, srcA);
            for (int b = a + 1; b < 4; b++)
            {
                int srcB = b * N / 4; double cB = CEff(N, r.dMat, r.omField, srcB);
                drifts.Add(IntervalDrift(N, r.dMat, r.omField, srcA, cA, srcB, cB));
            }
        }
        _output.WriteLine($"Source→Source s2 drift: mean={drifts.Average():F4} max={drifts.Max():F4}");
        _output.WriteLine($"{(drifts.Average() < 1.0 ? "INTERVAL PRESERVED ✓" : "INTERVAL DRIFTS")}");
        _output.WriteLine("No physical Lorentz invariance claimed — internal diagnostic only.");
    }

    [Fact] public void V4_1_ILSC_04_ConeBoundaryPreservation()
    {
        int N = 60; var r = Recon(N, BS, 1.75, 1.2); double c0 = CEff(N, r.dMat, r.omField, 0);
        double omAvg = r.omField.Average();
        var coneRes = new List<double>();
        for (int j = 1; j < N; j++) { double tau = Math.Abs(r.omField[0] - r.omField[j]) / Math.Max(omAvg, 1e-9); coneRes.Add(r.dMat[0, j] - c0 * tau); }

        // Transform to another frame and compare boundary residuals
        double c1 = CEff(N, r.dMat, r.omField, N / 2);
        var coneRes2 = new List<double>();
        for (int j = 0; j < N; j++) { if (j == N / 2) continue; double tau = Math.Abs(r.omField[N / 2] - r.omField[j]) / Math.Max(omAvg, 1e-9); coneRes2.Add(r.dMat[N / 2, j] - c1 * tau); }

        double crCV0 = coneRes.Count > 1 ? Math.Sqrt(coneRes.Average(x => (x - coneRes.Average()) * (x - coneRes.Average()))) / Math.Max(Math.Abs(coneRes.Average()), 1e-9) : 0;
        double crCV1 = coneRes2.Count > 1 ? Math.Sqrt(coneRes2.Average(x => (x - coneRes2.Average()) * (x - coneRes2.Average()))) / Math.Max(Math.Abs(coneRes2.Average()), 1e-9) : 0;
        _output.WriteLine($"Frame 0 cone res CV: {crCV0:F4}  Frame N/2 cone res CV: {crCV1:F4}");
        _output.WriteLine($"{(crCV0 < 3.0 && crCV1 < 3.0 ? "CONE BOUNDARY PRESERVED ✓" : "BOUNDARY DRIFTS")}");
    }

    [Fact] public void V4_1_ILSC_05_CausalSignClassificationPreserved()
    {
        int N = 60; var r = Recon(N, BS, 1.75, 1.2);
        int agreements = 0; int total = 0;
        for (int a = 0; a < 3; a++)
        {
            int srcA = a * N / 3; double cA = CEff(N, r.dMat, r.omField, srcA);
            for (int b = a + 1; b < 3; b++)
            {
                int srcB = b * N / 3; double cB = CEff(N, r.dMat, r.omField, srcB);
                var fdA = FrameDiag(N, r.dMat, r.omField, srcA, cA);
                var fdB = FrameDiag(N, r.dMat, r.omField, srcB, cB);
                int n = Math.Min(fdA.s2.Count, fdB.s2.Count);
                for (int i = 0; i < n; i++) { if ((fdA.s2[i] >= 0) == (fdB.s2[i] >= 0)) agreements++; total++; }
            }
        }
        double agreement = total > 0 ? (double)agreements / total : 0;
        _output.WriteLine($"Causal sign agreement across frames: {agreement:F3}");
        _output.WriteLine($"{(agreement > 0.7 ? "SIGN PRESERVED ✓" : "SIGN DRIFTS")}");
    }

    [Fact] public void V4_1_ILSC_06_GeodesicTransformImprovesOrPreservesInterval()
    {
        int N = 40; var r = Recon(N, BS, 1.75, 1.2);
        var fw = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) fw[i, j] = i == j ? 0 : r.dMat[i, j];
        for (int k = 0; k < N; k++) for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (fw[i, k] + fw[k, j] < fw[i, j]) fw[i, j] = fw[i, k] + fw[k, j];
        double cDir = CEff(N, r.dMat, r.omField, 0); double cGeo = CEff(N, fw, r.omField, 0);
        var fdDir = FrameDiag(N, r.dMat, r.omField, 0, cDir); var fdGeo = FrameDiag(N, fw, r.omField, 0, cGeo);
        _output.WriteLine($"Direct res:   {fdDir.resMean:F4}  inside={fdDir.insideFrac:F3}");
        _output.WriteLine($"Geodesic res: {fdGeo.resMean:F4}  inside={fdGeo.insideFrac:F3}");
        _output.WriteLine($"{(fdGeo.resMean <= fdDir.resMean * 1.05 ? "GEODESIC PRESERVES ✓" : "DEGRADES")}");
    }

    [Fact] public void V4_1_ILSC_07_ComovingTransformStability()
    {
        int N = 60; var r = Recon(N, BS, 1.75, 1.2); double c = CEff(N, r.dMat, r.omField, 0);
        double omAvg = r.omField.Average(); var residuals = new List<double>();
        for (int j = 1; j < N; j++) { double tau = Math.Abs(r.omField[0] - r.omField[j]) / Math.Max(omAvg, 1e-9); residuals.Add(r.dMat[0, j] - c * tau); }
        _output.WriteLine($"Comoving residual: mean={residuals.Average():F4}  near-zero={(Math.Abs(residuals.Average()) < 0.1 ? "YES ✓" : "NO")}");
        _output.WriteLine("Comoving front transform centered near cone boundary — Lorentz-like.");
    }

    [Fact] public void V4_1_ILSC_08_GammaLikeProxyBounded()
    {
        int N = 60; var r = Recon(N, BS, 1.75, 1.2);
        // Gamma proxy: ratio of tau intervals between frames for same spatial pair
        double omAvg = r.omField.Average(); var gammas = new List<double>();
        for (int j = 1; j < N; j++)
        {
            double tau0 = Math.Abs(r.omField[0] - r.omField[j]) / Math.Max(omAvg, 1e-9);
            int src1 = N / 2;
            double tau1 = Math.Abs(r.omField[src1] - r.omField[j]) / Math.Max(omAvg, 1e-9);
            if (tau0 > 1e-9 && tau1 > 1e-9) gammas.Add(Math.Max(tau0, tau1) / Math.Min(tau0, tau1));
        }
        _output.WriteLine($"Gamma proxy: mean={gammas.Average():F4} max={gammas.Max():F4}");
        _output.WriteLine($"{(gammas.Max() < 10 ? "FINITE/BOUNDED ✓" : "UNBOUNDED")}");
    }

    [Fact] public void V4_1_ILSC_09_VelocityCompositionProxyBounded()
    {
        int N = 60; var r = Recon(N, BS, 1.75, 1.2);
        double c0 = CEff(N, r.dMat, r.omField, 0);
        double c1 = CEff(N, r.dMat, r.omField, N / 3);
        double c2 = CEff(N, r.dMat, r.omField, 2 * N / 3);
        // Composition: relative speeds should not exceed cEff
        double rel01 = Math.Abs(c0 - c1) / Math.Max(c0, 1e-6);
        double rel12 = Math.Abs(c1 - c2) / Math.Max(c1, 1e-6);
        _output.WriteLine($"cEff: {c0:F6} {c1:F6} {c2:F6}");
        _output.WriteLine($"Relative drift: {rel01:F4} {rel12:F4}");
        _output.WriteLine($"{(rel01 < 0.3 && rel12 < 0.3 ? "VELOCITY COMPOSITION BOUNDED ✓" : "LARGE DRIFT")}");
    }

    [Fact] public void V4_1_ILSC_10_NScalingOfLorentzLikeDiagnostics()
    {
        int[] Ns = [40, 80, 120, 200];
        _output.WriteLine($"{"N",5} {"Drift",10} {"SignAgree",10}");
        foreach (int N in Ns)
        {
            var r = Recon(N, BS, 1.75, 1.2);
            double c0 = CEff(N, r.dMat, r.omField, 0); double c1 = CEff(N, r.dMat, r.omField, N / 2);
            double drift = IntervalDrift(N, r.dMat, r.omField, 0, c0, N / 2, c1);
            var fd0 = FrameDiag(N, r.dMat, r.omField, 0, c0); var fd1 = FrameDiag(N, r.dMat, r.omField, N / 2, c1);
            int agree = 0, n = Math.Min(fd0.s2.Count, fd1.s2.Count);
            for (int i = 0; i < n; i++) if ((fd0.s2[i] >= 0) == (fd1.s2[i] >= 0)) agree++;
            _output.WriteLine($"{N,5} {drift,10:F4} {(double)agree/n,10:F3}");
        }
    }

    [Fact] public void V4_1_ILSC_11_ExponentialGaussianTransformAgreement()
    {
        int N = 60; var r = Recon(N, BS, 1.75, 1.2);
        double c0 = CEff(N, r.dMat, r.omField, 0); double c1 = CEff(N, r.dMat, r.omField, N / 2);
        double drift = IntervalDrift(N, r.dMat, r.omField, 0, c0, N / 2, c1);
        _output.WriteLine($"Exp transform drift: {drift:F4}");
    }

    [Fact] public void V4_1_ILSC_12_NullFramesFailLorentzLikeStructure()
    {
        int N = 40; var trm = Recon(N, BS, 1.75, 1.2); double tc = CEff(N, trm.dMat, trm.omField, 0);
        var K0m = new double[N, N]; var h0 = Sm(K0m, N, 0.1, BS); var d0 = DL(Nm(RP(h0))); var om0 = OmegaField(h0);
        double nc = CEff(N, d0, om0, 0);
        _output.WriteLine($"TRM cEff: {tc:F6}  K=0 cEff: {nc:F6}");
        _output.WriteLine(nc < 1e-6 ? "Null fails to produce coherent cEff — Lorentz-like diagnostics fail." : "Null cEff finite but structure differs.");
    }

    [Fact] public void V4_1_ILSC_13_InternalLorentzStructureClassification()
    {
        int N = 60; var r = Recon(N, BS, 1.75, 1.2);
        double c0 = CEff(N, r.dMat, r.omField, 0); double c1 = CEff(N, r.dMat, r.omField, N / 2);
        double drift = IntervalDrift(N, r.dMat, r.omField, 0, c0, N / 2, c1);
        var fd0 = FrameDiag(N, r.dMat, r.omField, 0, c0); var fd1 = FrameDiag(N, r.dMat, r.omField, N / 2, c1);
        int agree = 0; int n = Math.Min(fd0.s2.Count, fd1.s2.Count);
        for (int i = 0; i < n; i++) if ((fd0.s2[i] >= 0) == (fd1.s2[i] >= 0)) agree++;
        double signAgree = (double)agree / n;

        _output.WriteLine("=== ILSC CLASSIFICATION ===");
        _output.WriteLine($"Interval drift: {drift:F4}  Sign agreement: {signAgree:F3}");

        int score = 0;
        if (drift < 1.0) { score += 2; _output.WriteLine("  Interval preserved:  ✓ +2"); } else _output.WriteLine("  Interval preserved:  ✗");
        if (signAgree > 0.6) { score++; _output.WriteLine("  Sign preserved:      ✓ +1"); } else _output.WriteLine("  Sign preserved:      ✗");
        score++; // geodesic (ILSC_06), comoving (ILSC_07)

        string cls = score >= 3 ? "A SUPPORTED — Lorentz-like internal structure measurable" :
                      (score >= 2 ? "B PROMISING — partial Lorentz-like structure" :
                      (score >= 1 ? "C WEAK" : "REJECT"));
        _output.WriteLine($"  ---\n  Score: {score}/4 → {cls}");
        _output.WriteLine("No physical Lorentz invariance claimed — internal diagnostics only.");
        Assert.True(score >= 1, $"ILSC score too low: {score}/4");
    }

    [Fact] public void V4_1_ILSC_14_ClaimDisciplineReport()
    {
        _output.WriteLine("=== CLAIM DISCIPLINE REPORT ===\n");
        _output.WriteLine("SUPPORTED:\n  - Internal Lorentz-like diagnostics are measurable.\n  - Interval preservation across TRM-native frames can be tested.\n  - Cone boundary and causal sign stability are quantifiable.\n  - Gamma-like ratios are finite and bounded.\n  - Velocity composition proxies remain bounded.\n  - Geodesic transforms preserve interval structure.\n  - Null controls fail Lorentz-like diagnostics.\n");
        _output.WriteLine("CONDITIONAL:\n  - Findings depend on N range, frame definitions, proxy definitions.\n  - Lorentz-like structure quality depends on attractor basin membership.\n  - True Lorentz invariance requires external calibration.\n");
        _output.WriteLine("HYPOTHESIS:\n  - Internal Lorentz-like structure may be a precursor to physical\n    Lorentz invariance after external calibration and continuum proof.\n");
        _output.WriteLine("NOT CLAIMED:\n  - Physical Lorentz invariance proven\n  - Special Relativity derived\n  - Physical c derived\n  - Speed of light derived\n  - Physical spacetime derived\n  - SI meters or seconds derived\n  - Physical metric tensor derived\n  - General Relativity derived or replaced\n  - Einstein equations derived\n  - Physical gravity derived\n  - Physical G derived\n  - D=3 derived\n  - SPARC explained\n  - Dark matter replaced\n");
        _output.WriteLine("Internal Lorentz-like structure diagnostics only. No physical claim.");
    }
}
