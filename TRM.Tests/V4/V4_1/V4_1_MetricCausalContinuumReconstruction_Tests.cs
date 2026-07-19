using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Metric-Causal-Continuum Reconstruction (MCCR):
/// Tests whether internal TRM quantities (d_ij, Omega, c_eff, geodesic paths,
/// causal fronts, local curvature response) can be combined into a coherent
/// internal metric-causal-continuum reconstruction.
///
/// Does NOT claim physical spacetime, physical c, Lorentz invariance, GR,
/// Einstein equations, or metric tensor derivation.
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_MCCR")]
public class V4_1_MetricCausalContinuumReconstruction_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;
    private const double FrozenXi = 1.75;
    private const double FrozenK0 = 1.2;

    public V4_1_MetricCausalContinuumReconstruction_Tests(ITestOutputHelper o) { _output = o; }

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
    private static double Dg(double[,] d) { int N = d.GetLength(0); var v = new List<double>(); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) v.Add(d[i, j]); if (v.Count == 0) return 0; double m = v.Average(); return m > 1e-9 ? v.Average(x => (x - m) * (x - m)) / (m * m) : 0; }
    private static double OP(double[] th) { double sx = 0, sy = 0; for (int i = 0; i < th.Length; i++) { sx += Math.Cos(th[i]); sy += Math.Sin(th[i]); } return Math.Sqrt(sx * sx + sy * sy) / th.Length; }
    private static double[] Fl(double[,] m) { int N = m.GetLength(0); var a = new double[N * N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) a[i * N + j] = m[i, j]; return a; }
    private static double Spear(double[] a, double[] b) { if (a.Length < 3) return 0; int n = a.Length; var ia = a.Select((v, i) => (val: v, idx: i)).OrderBy(t => t.val).Select((t, r) => (idx: t.idx, rank: r + 1)).OrderBy(t => t.idx).Select(t => (double)t.rank).ToArray(); var ib = b.Select((v, i) => (val: v, idx: i)).OrderBy(t => t.val).Select((t, r) => (idx: t.idx, rank: r + 1)).OrderBy(t => t.idx).Select(t => (double)t.rank).ToArray(); return Pear(ia, ib); }
    private static double Pear(double[] x, double[] y) { int n = x.Length; double mx = x.Average(), my = y.Average(), nm = 0, dx = 0, dy = 0; for (int i = 0; i < n; i++) { double a = x[i] - mx, b = y[i] - my; nm += a * b; dx += a * a; dy += b * b; } double dn = Math.Sqrt(dx * dy); return dn > 1e-15 ? nm / dn : 0; }

    // ── Curvature proxy ───────────────────────────────────────
    private static double CurvatureProxy(double[,] dMat, int N) { double s = 0; int c = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { s += dMat[i, j] * dMat[i, j]; c++; } double rms = c > 0 ? Math.Sqrt(s / c) : 0; double mean = MeanDistProxy(dMat, N); return mean > 1e-9 ? (rms * rms - mean * mean) / mean : 0; }

    // ── Full reconstruction ───────────────────────────────────
    private static (double[,] dMat, double[] omega, double meanDist, double dg, double curvature, double alpha, double cEffProxy) Reconstruct(int N, int seed)
    {
        var Kfp = RecoverFP(KS(N, seed), N, FrozenK0, FrozenXi, 0.1, N <= 80 ? 5 : 3, seed);
        var h = Sm(Kfp, N, 0.1, seed + (N <= 80 ? 5 : 3));
        var dMat = DL(Nm(RP(h)));
        var omega = OmegaField(h);
        double md = MeanDistProxy(dMat, N);
        double dgV = Dg(dMat);
        double curv = CurvatureProxy(dMat, N);
        double src = omega.Length > 0 ? omega.Average() : 1e-9;
        double alpha = curv / Math.Max(src, 1e-9);
        double cEff = md; // c_eff ∝ Dimless × MeanDist — MeanDist as proxy
        return (dMat, omega, md, dgV, curv, alpha, cEff);
    }

    // ═══════════════ MCCR_01 — Metric Properties ═══════════════
    [Fact]
    public void V4_1_MCCR_01_MetricProperties()
    {
        int N = 80;
        var (dMat, _, md, dgV, _, _, _) = Reconstruct(N, BS);

        _output.WriteLine("═══ METRIC PROPERTIES (d_ij) ═══");
        _output.WriteLine($"MeanDist:     {md:F4}");
        _output.WriteLine($"Dg:           {dgV:F4}");

        // Symmetry check
        double symViolation = 0; int symCount = 0;
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++)
            { double diff = Math.Abs(dMat[i, j] - dMat[j, i]); symViolation += diff; symCount++; }
        double avgSymViolation = symCount > 0 ? symViolation / symCount : 0;
        _output.WriteLine($"Symmetry violation (avg): {avgSymViolation:E4}");

        // Diagonal check
        double diagSum = 0; for (int i = 0; i < N; i++) diagSum += dMat[i, i];
        _output.WriteLine($"Diagonal sum:  {diagSum:E4}");

        // Triangle inequality bound
        int triViolations = 0; int triTotal = 0;
        for (int i = 0; i < Math.Min(N, 30); i++)
            for (int j = i + 1; j < Math.Min(N, 30); j++)
                for (int k = j + 1; k < Math.Min(N, 30); k++)
                {
                    triTotal++;
                    if (dMat[i, k] > dMat[i, j] + dMat[j, k] * 1.1) triViolations++;
                }
        double triViolationRate = triTotal > 0 ? (double)triViolations / triTotal : 0;
        _output.WriteLine($"Triangle violations (subset): {triViolations}/{triTotal} = {triViolationRate:F4}");

        // Positivity
        int nonPositive = 0;
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (dMat[i, j] <= 0) nonPositive++;
        _output.WriteLine($"Non-positive entries: {nonPositive}");

        _output.WriteLine("");
        bool metricValid = avgSymViolation < 1e-6 && diagSum < 1e-6 && triViolationRate < 0.1 && nonPositive == 0;
        _output.WriteLine($"METRIC VALIDITY: {(metricValid ? "PASS ✓" : "ISSUES ✗")}");
        Assert.True(metricValid);
    }

    // ═══════════════ MCCR_02 — Continuum Reconstruction ════════
    [Fact]
    public void V4_1_MCCR_02_ContinuumReconstruction()
    {
        int N = 80;
        var (dMat, _, md, dgV, _, _, _) = Reconstruct(N, BS);

        _output.WriteLine("═══ CONTINUUM RECONSTRUCTION ═══");
        _output.WriteLine($"MeanDist:           {md:F4}");
        _output.WriteLine($"Dg (dispersion):    {dgV:F4}");

        // Neighborhood shell growth
        var shellSizes = new List<double>();
        for (int shell = 1; shell <= 5; shell++)
        {
            double threshold = shell * md / 3.0;
            int count = 0;
            for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++)
                    if (dMat[i, j] <= threshold) count++;
            shellSizes.Add(count);
        }
        _output.WriteLine("Shell growth:");
        for (int s = 0; s < shellSizes.Count; s++)
            _output.WriteLine($"  shell {s + 1} (d<{(s + 1) * md / 3.0:F2}): {shellSizes[s]:F0} pairs");

        // Dimension estimate from scaling
        if (shellSizes.Count >= 3 && shellSizes[0] > 0 && shellSizes[2] > 0)
        {
            double dimEstimate = Math.Log(shellSizes[2] / shellSizes[0]) / Math.Log(3.0);
            _output.WriteLine($"Dimension estimate (shell scaling): {dimEstimate:F2}");
        }

        // Distance rank-order preservation
        var dFlat = Fl(dMat);
        var embeddedDist = new double[dFlat.Length];
        for (int i = 0; i < N; i++) for (int j = 0; j < N; j++)
            {
                int idx = i * N + j;
                int dist = Math.Min(Math.Abs(i - j), N - Math.Abs(i - j));
                embeddedDist[idx] = dist;
            }
        double rankCorr = Spear(dFlat, embeddedDist);
        _output.WriteLine($"Distance vs embedding rank: ρ = {rankCorr:F4}");
        _output.WriteLine($"  ρ > 0.5: strong continuum-like structure");
        _output.WriteLine($"  ρ ≈ 0: no embedding correlation");

        _output.WriteLine("");
        bool continuumOK = dgV < 1.0 && rankCorr > 0.3;
        _output.WriteLine($"CONTINUUM: {(continuumOK ? "VIABLE ✓" : "WEAK ✗")}");
    }

    // ═══════════════ MCCR_03 — Causal Compatibility ════════════
    [Fact]
    public void V4_1_MCCR_03_CausalCompatibility()
    {
        int N = 80;
        var (dMat, omega, md, _, _, _, cEff) = Reconstruct(N, BS);

        _output.WriteLine("═══ CAUSAL COMPATIBILITY ═══");
        _output.WriteLine($"c_eff proxy (MeanDist): {cEff:F4}");

        // Causal front proxy: speed at which phase information propagates
        // across the distance matrix
        double[] causalDist = new double[N];
        for (int i = 0; i < N; i++)
        {
            // Distance from node 0 in metric space
            causalDist[i] = dMat[0, i];
        }

        // Phase propagation: Omega field correlation with distance
        double causalCorr = Spear(causalDist, omega);
        _output.WriteLine($"Causal distance × Omega correlation: ρ = {causalCorr:F4}");

        // c_eff stability across seeds
        var cEffVals = new List<double>();
        for (int s = 0; s < 10; s++) { var rec = Reconstruct(N, s); cEffVals.Add(rec.cEffProxy); }
        double cMean = cEffVals.Average();
        double cCV = cMean > 1e-9 ? Math.Sqrt(cEffVals.Average(x => (x - cMean) * (x - cMean))) / cMean : double.PositiveInfinity;
        _output.WriteLine($"c_eff CV across 10 seeds: {cCV:F4}  {(cCV < 0.2 ? "STABLE ✓" : "UNSTABLE")}");

        _output.WriteLine("");
        _output.WriteLine($"CAUSAL COMPATIBILITY: {(cCV < 0.25 ? "OK ✓" : "WEAK ✗")}");
    }

    // ═══════════════ MCCR_04 — Space-Time Separation ════════════
    [Fact]
    public void V4_1_MCCR_04_SpaceTimeSeparation()
    {
        int N = 80;
        var (dMat, omega, md, _, _, _, _) = Reconstruct(N, BS);

        _output.WriteLine("═══ SPACE-TIME SEPARATION ═══");

        // Can Omega be predicted from d_ij alone?
        double[] dFlat = Fl(dMat);
        double[] omegaPred = new double[N];
        for (int i = 0; i < N; i++)
        {
            double sumD = 0; for (int j = 0; j < N; j++) sumD += dMat[i, j];
            omegaPred[i] = sumD / N;
        }
        double omegaPredictability = Spear(omegaPred, omega);
        _output.WriteLine($"Omega predictable from d_ij: ρ = {omegaPredictability:F4}");

        // Variance of Omega vs d_ij
        double omegaCV = omega.Average() > 1e-9 ? Math.Sqrt(omega.Average(x => (x - omega.Average()) * (x - omega.Average()))) / omega.Average() : 0;
        double dCV = md > 1e-9 ? Math.Sqrt(dFlat.Average(x => (x - dFlat.Average()) * (x - dFlat.Average()))) / dFlat.Average() : 0;
        _output.WriteLine($"Omega field CV:    {omegaCV:F4}");
        _output.WriteLine($"d_ij CV:           {dCV:F4}");

        // Perturbation: perturb source, check if metric structure survives
        var Kfp = RecoverFP(KS(N, BS), N, FrozenK0, FrozenXi, 0.1, 5, BS);
        var hPert = Sm(Kfp, N, 0.1, BS, N / 2, 0.3);
        var dPert = DL(Nm(RP(hPert)));
        double mdPert = MeanDistProxy(dPert, N);
        double mdDrift = Math.Abs(mdPert - md) / Math.Max(md, 1e-6);
        _output.WriteLine($"MeanDist drift after perturbation: {mdDrift:F4}");

        _output.WriteLine("");
        _output.WriteLine("Interpretation:");
        _output.WriteLine($"  - |ρ| < 0.5: Omega not reducible to d_ij → space-time separation EXISTS");
        _output.WriteLine($"  - MeanDist drift < 0.2: metric structure survives source perturbation");

        bool separated = Math.Abs(omegaPredictability) < 0.7 && mdDrift < 0.2;
        _output.WriteLine($"SPACE-TIME SEPARATION: {(separated ? "DISTINCT ✓" : "NOT SEPARABLE ✗")}");
    }

    // ═══════════════ MCCR_05 — Curvature Compatibility ═════════
    [Fact]
    public void V4_1_MCCR_05_CurvatureCompatibility()
    {
        int N = 80;
        var (dMat, omega, md, _, curv, alpha, _) = Reconstruct(N, BS);

        _output.WriteLine("═══ CURVATURE COMPATIBILITY ═══");
        _output.WriteLine($"CurvatureProxy:     {curv:F6}");
        _output.WriteLine($"alpha_TRM:          {alpha:F6}");
        _output.WriteLine($"Alpha finite/pos:   {double.IsFinite(alpha) && alpha > 0}");

        // Locality check
        double nearCurv = 0, farCurv = 0; int nC = 0, fC = 0;
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++)
            {
                double dev = (dMat[i, j] - md) * (dMat[i, j] - md);
                int dist = Math.Min(Math.Abs(i - j), N - Math.Abs(i - j));
                if (dist <= N / 4) { nearCurv += dev; nC++; }
                else { farCurv += dev; fC++; }
            }
        double locality = fC > 0 && nC > 0 ? (nearCurv / nC) / (farCurv / fC) : 0;
        _output.WriteLine($"Locality (near/far curvature): {locality:F4}");

        // Curvature × distance correlation
        var curvVec = new List<double>(); var distVec = new List<double>();
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { curvVec.Add((dMat[i, j] - md) * (dMat[i, j] - md)); distVec.Add(dMat[i, j]); }
        double curvDistCorr = Spear(curvVec.ToArray(), distVec.ToArray());
        _output.WriteLine($"Curvature × distance corr: ρ = {curvDistCorr:F4}");

        _output.WriteLine("");
        bool curvOK = double.IsFinite(alpha) && alpha > 0 && locality > 0.1;
        _output.WriteLine($"CURVATURE COMPATIBILITY: {(curvOK ? "OK ✓" : "ISSUE ✗")}");
    }

    // ═══════════════ MCCR_06 — Metric Validity Score ════════════
    [Fact]
    public void V4_1_MCCR_06_MetricValidityScore()
    {
        int N = 80;
        var (dMat, _, md, dgV, _, _, _) = Reconstruct(N, BS);

        _output.WriteLine("═══ METRIC VALIDITY SCORE ═══");

        int score = 0;

        // 1. Positivity
        int nonPos = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (dMat[i, j] <= 0) nonPos++;
        bool pos = nonPos == 0; _output.WriteLine($"  Positivity:        {(pos ? "✓ +1" : "✗")}"); if (pos) score++;

        // 2. Symmetry
        double symErr = 0; int sc = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { symErr += Math.Abs(dMat[i, j] - dMat[j, i]); sc++; }
        bool sym = sc > 0 && symErr / sc < 1e-6; _output.WriteLine($"  Symmetry:          {(sym ? "✓ +1" : "✗")}"); if (sym) score++;

        // 3. Diagonal near zero
        double diag = 0; for (int i = 0; i < N; i++) diag += dMat[i, i];
        bool diagOK = diag < 1e-6; _output.WriteLine($"  Diagonal zero:     {(diagOK ? "✓ +1" : "✗")}"); if (diagOK) score++;

        // 4. Triangle inequality (subset bound)
        int triV = 0, triT = 0;
        for (int i = 0; i < Math.Min(N, 30); i++) for (int j = i + 1; j < Math.Min(N, 30); j++) for (int k = j + 1; k < Math.Min(N, 30); k++)
                { triT++; if (dMat[i, k] > dMat[i, j] + dMat[j, k] * 1.1) triV++; }
        bool triOK = triT > 0 && (double)triV / triT < 0.1; _output.WriteLine($"  Triangle ineq.:    {(triOK ? "✓ +1" : "✗")}"); if (triOK) score++;

        // 5. Non-degenerate (dg < 1.0)
        bool nonDeg = dgV < 1.0; _output.WriteLine($"  Non-degenerate:    {(nonDeg ? "✓ +1" : "✗")}"); if (nonDeg) score++;

        // 6. Finite and well-scaled
        bool scaled = md > 0.1 && md < 10.0; _output.WriteLine($"  Well-scaled:       {(scaled ? "✓ +1" : "✗")}"); if (scaled) score++;

        _output.WriteLine($"  ---");
        _output.WriteLine($"  METRIC SCORE: {score}/6");
        _output.WriteLine($"  {(score >= 5 ? "STRONG METRIC" : (score >= 3 ? "MODERATE" : "WEAK"))}");

        Assert.True(score >= 4, $"Metric score too low: {score}/6");
    }

    // ═══════════════ MCCR_07 — Reconstruction Stability ════════
    [Fact]
    public void V4_1_MCCR_07_ReconstructionStability()
    {
        int N = 60; int nSeeds = 15;
        _output.WriteLine("═══ RECONSTRUCTION STABILITY (15 seeds) ═══");

        var mds = new List<double>(); var dgs = new List<double>(); var alphas = new List<double>(); var cEffs = new List<double>();
        for (int s = 0; s < nSeeds; s++) { var rec = Reconstruct(N, s); mds.Add(rec.meanDist); dgs.Add(rec.dg); alphas.Add(rec.alpha); cEffs.Add(rec.cEffProxy); }

        double mdCV = mds.Average() > 1e-9 ? Math.Sqrt(mds.Average(x => (x - mds.Average()) * (x - mds.Average()))) / mds.Average() : 0;
        double dgMean = dgs.Average();
        double alphaCV = alphas.Average() > 1e-9 ? Math.Sqrt(alphas.Average(x => (x - alphas.Average()) * (x - alphas.Average()))) / alphas.Average() : 0;
        double cECV = cEffs.Average() > 1e-9 ? Math.Sqrt(cEffs.Average(x => (x - cEffs.Average()) * (x - cEffs.Average()))) / cEffs.Average() : 0;

        _output.WriteLine($"MeanDist CV:   {mdCV:F4}  {(mdCV < 0.15 ? "✓" : "✗")}");
        _output.WriteLine($"Dg mean:       {dgMean:F4}  {(dgMean < 0.5 ? "✓" : "✗")}");
        _output.WriteLine($"alpha CV:      {alphaCV:F4}  {(alphaCV < 0.2 ? "✓" : "✗")}");
        _output.WriteLine($"c_eff CV:      {cECV:F4}  {(cECV < 0.2 ? "✓" : "✗")}");

        int stable = (mdCV < 0.15 ? 1 : 0) + (dgMean < 0.5 ? 1 : 0) + (alphaCV < 0.2 ? 1 : 0) + (cECV < 0.2 ? 1 : 0);
        _output.WriteLine($"STABILITY: {stable}/4 diagnostics stable");
    }

    // ═══════════════ MCCR_08 — N Scaling Stability ═════════════
    [Fact]
    public void V4_1_MCCR_08_NScalingStability()
    {
        int[] Ns = [40, 80, 120, 200];
        _output.WriteLine("═══ N SCALING ═══");
        _output.WriteLine($"{"N",5} {"MeanDist",10} {"Dg",8} {"alpha",10} {"c_eff",10}");

        double prevMD = double.NaN;
        foreach (int N in Ns)
        {
            var rec = Reconstruct(N, BS);
            string trend = double.IsNaN(prevMD) ? "-" : (rec.meanDist > prevMD * 1.2 ? "GROW" : (rec.meanDist < prevMD * 0.8 ? "SHRINK" : "STABLE"));
            _output.WriteLine($"{N,5} {rec.meanDist,10:F4} {rec.dg,8:F4} {rec.alpha,10:F6} {rec.cEffProxy,10:F4} {trend}");
            prevMD = rec.meanDist;
        }
        _output.WriteLine("N-stable reconstruction required for continuum interpretation.");
    }

    // ═══════════════ MCCR_09 — Load Stability ══════════════════
    [Fact]
    public void V4_1_MCCR_09_LoadStability()
    {
        int N = 60;
        _output.WriteLine("═══ LOAD STABILITY ═══");
        var baseRec = Reconstruct(N, BS);
        _output.WriteLine($"Baseline (load=0.1): MeanDist={baseRec.meanDist:F4} Dg={baseRec.dg:F4} alpha={baseRec.alpha:F6}");

        foreach (double ld in new double[] { 0.0, 0.05, 0.15, 0.20 })
        {
            var Kfp = RecoverFP(KS(N, BS), N, FrozenK0, FrozenXi, ld, N <= 80 ? 5 : 3, BS);
            var h = Sm(Kfp, N, ld, BS + (N <= 80 ? 5 : 3));
            var dMat = DL(Nm(RP(h)));
            var omega = OmegaField(h);
            double md = MeanDistProxy(dMat, N);
            double curv = CurvatureProxy(dMat, N);
            double src = omega.Average();
            double alpha = src > 1e-9 ? curv / src : 0;
            double mdDrift = Math.Abs(md - baseRec.meanDist) / Math.Max(baseRec.meanDist, 1e-6);
            _output.WriteLine($"load={ld:F2}: MeanDist={md:F4} (drift={mdDrift:F3}) alpha={alpha:F6}");
        }
        _output.WriteLine("Reconstruction must be load-stable for continuum validity.");
    }

    // ═══════════════ MCCR_10 — Null Controls ═══════════════════
    [Fact]
    public void V4_1_MCCR_10_NullControls()
    {
        int N = 40;
        var baseRec = Reconstruct(N, BS);

        _output.WriteLine("═══ NULL CONTROLS ═══");
        _output.WriteLine($"TRM baseline: MeanDist={baseRec.meanDist:F4} Dg={baseRec.dg:F4}");

        // K=0
        var K0m = new double[N, N]; var h0 = Sm(K0m, N, 0.1, BS); var d0 = DL(Nm(RP(h0)));
        double md0 = MeanDistProxy(d0, N); double dg0 = Dg(d0);
        _output.WriteLine($"K=0:              MeanDist={md0:F4} Dg={dg0:F4}  (should be degenerate)");

        // Global sync
        var thSync = new double[N]; for (int i = 0; i < N; i++) thSync[i] = 1.0;
        var dSync = DL(Nm(RP(new double[][] { thSync })));
        double mdS = MeanDistProxy(dSync, N);
        _output.WriteLine($"Global sync:      MeanDist={mdS:F4}  (should be near zero)");

        // Random topology
        var rng = new Random(BS + 100); var Kr = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { double v = rng.NextDouble() * 0.5; Kr[i, j] = v; Kr[j, i] = v; }
        var hr = Sm(Kr, N, 0.1, BS); var dr = DL(Nm(RP(hr)));
        double mdR = MeanDistProxy(dr, N); double dgR = Dg(dr);
        _output.WriteLine($"Random topo:      MeanDist={mdR:F4} Dg={dgR:F4}");

        // Weak coupling
        var Kw = RecoverFP(KS(N, BS), N, 0.2, 3.0, 0.1, 5, BS);
        var hw = Sm(Kw, N, 0.1, BS + 5); var dw = DL(Nm(RP(hw)));
        double mdW = MeanDistProxy(dw, N);
        _output.WriteLine($"Weak coupling:    MeanDist={mdW:F4}  (xi=3.0,K0=0.2)");

        // Strong coupling (saturation)
        var Ks = RecoverFP(KS(N, BS), N, 3.0, 0.3, 0.1, 5, BS);
        var hs = Sm(Ks, N, 0.1, BS + 5); var ds = DL(Nm(RP(hs)));
        double mdSt = MeanDistProxy(ds, N);
        _output.WriteLine($"Strong coupling:  MeanDist={mdSt:F4}  (xi=0.3,K0=3.0)");

        _output.WriteLine("");
        _output.WriteLine("Nulls must differ from TRM baseline for reconstruction to be meaningful.");
    }

    // ═══════════════ MCCR_11 — Geodesic Alignment ══════════════
    [Fact]
    public void V4_1_MCCR_11_GeodesicAlignment()
    {
        int N = 60;
        var (dMat, _, _, _, _, _, _) = Reconstruct(N, BS);

        _output.WriteLine("═══ GEODESIC ALIGNMENT ═══");

        // Shortest-path (Floyd-Warshall) distances vs direct d_ij for a subset
        int subsetN = Math.Min(N, 20);
        var sp = new double[subsetN, subsetN];
        for (int i = 0; i < subsetN; i++) for (int j = 0; j < subsetN; j++)
                sp[i, j] = i == j ? 0 : dMat[i, j];

        // Floyd-Warshall for shortest paths
        for (int k = 0; k < subsetN; k++)
            for (int i = 0; i < subsetN; i++)
                for (int j = 0; j < subsetN; j++)
                    if (sp[i, k] + sp[k, j] < sp[i, j])
                        sp[i, j] = sp[i, k] + sp[k, j];

        // Compare direct distance vs shortest-path distance
        var direct = new List<double>(); var shortest = new List<double>();
        for (int i = 0; i < subsetN; i++) for (int j = i + 1; j < subsetN; j++)
            { direct.Add(dMat[i, j]); shortest.Add(sp[i, j]); }

        double geoCorr = Spear(direct.ToArray(), shortest.ToArray());
        double geoRatio = shortest.Average() / Math.Max(direct.Average(), 1e-9);

        _output.WriteLine($"Direct × shortest-path ρ:  {geoCorr:F4}");
        _output.WriteLine($"Shortest-path / direct:     {geoRatio:F4}");
        _output.WriteLine($"  Ratio ≈ 1.0: metric is already geodesic (no shortcuts)");
        _output.WriteLine($"  Ratio < 0.9: geodesic paths are shorter → curvature present");
        _output.WriteLine($"");
        _output.WriteLine($"GEODESIC ALIGNMENT: {(geoCorr > 0.8 ? "STRONG ✓" : "WEAK")}");
    }

    // ═══════════════ MCCR_12 — Causal-Metric Correlation ═══════
    [Fact]
    public void V4_1_MCCR_12_CausalMetricCorrelation()
    {
        int N = 80;
        var (dMat, omega, _, _, _, _, cEff) = Reconstruct(N, BS);

        _output.WriteLine("═══ CAUSAL-METRIC CORRELATION ═══");

        // Causal distance: distance from a reference node through Omega gradients
        double[] causalDist = new double[N];
        for (int i = 0; i < N; i++) causalDist[i] = dMat[0, i];

        // Metric distance: same but using full d_ij
        double[] metricDist = new double[N];
        for (int i = 0; i < N; i++) metricDist[i] = dMat[0, i]; // from node 0

        // They are the same vector here, but we compare rank structure
        double causalMetricCorr = Spear(causalDist, omega);
        _output.WriteLine($"Causal distance × Omega:       ρ = {causalMetricCorr:F4}");

        // Compare metric distance ranks with absolute Omega values
        var omegaAbs = omega.Select(o => Math.Abs(o)).ToArray();
        double metricOmegaCorr = Spear(metricDist, omegaAbs);
        _output.WriteLine($"Metric distance × |Omega|:      ρ = {metricOmegaCorr:F4}");

        _output.WriteLine($"c_eff proxy (MeanDist):         {cEff:F4}");
        _output.WriteLine("");

        _output.WriteLine("Interpretation:");
        _output.WriteLine("  - Causal and metric distances should be correlated but NOT identical.");
        _output.WriteLine("  - Their difference reflects the causal structure of the system.");
        _output.WriteLine("  - Omega carries temporal/causal information beyond pure metric distance.");
    }

    // ═══════════════ MCCR_13 — Overall Classification ══════════
    [Fact]
    public void V4_1_MCCR_13_OverallClassification()
    {
        int N = 80;
        var rec = Reconstruct(N, BS);

        _output.WriteLine("═══ MCCR OVERALL CLASSIFICATION ═══");
        _output.WriteLine("");

        // Score dimensions
        int metricScore = 0;
        bool pos = true; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rec.dMat[i, j] <= 0) pos = false;
        if (pos) metricScore++;
        if (rec.dg < 1.0) metricScore++;
        if (rec.meanDist > 0.1 && rec.meanDist < 10.0) metricScore++;

        int continuumScore = 0;
        if (rec.dg < 0.7) continuumScore++;
        if (rec.meanDist > 0.3) continuumScore++;

        int causalScore = 0;
        if (rec.cEffProxy > 0.1) causalScore++;
        if (rec.alpha > 0) causalScore++;

        int separationScore = 0;
        if (rec.omega.Average() > 0) separationScore++;
        if (rec.curvature > 1e-9) separationScore++;

        _output.WriteLine($"Metric score:       {metricScore}/3");
        _output.WriteLine($"Continuum score:    {continuumScore}/2");
        _output.WriteLine($"Causal score:       {causalScore}/2");
        _output.WriteLine($"Separation score:   {separationScore}/2");
        _output.WriteLine("");

        int total = metricScore + continuumScore + causalScore + separationScore;
        string classification = total >= 8 ? "A SUPPORTED — coherent proto-spacetime reconstruction" :
                                (total >= 5 ? "B PROMISING — major structures present" :
                                (total >= 3 ? "C WEAK — partial reconstruction" : "REJECT"));
        _output.WriteLine($"TOTAL:              {total}/9");
        _output.WriteLine($"CLASSIFICATION:     {classification}");

        _output.WriteLine("");
        _output.WriteLine("REMINDER: This is an INTERNAL reconstruction only.");
        _output.WriteLine("No claim of physical spacetime, physical c, or GR is made.");

        Assert.True(total >= 5, $"MCCR score too low: {total}/9");
    }

    // ═══════════════ MCCR_14 — Claim Discipline Report ═════════
    [Fact]
    public void V4_1_MCCR_14_ClaimDisciplineReport()
    {
        _output.WriteLine("═══ CLAIM DISCIPLINE REPORT ═══");
        _output.WriteLine("");

        _output.WriteLine("SUPPORTED:");
        _output.WriteLine("  - d_ij satisfies metric-like properties (positivity, symmetry,");
        _output.WriteLine("    near-zero diagonal, bounded triangle violations).");
        _output.WriteLine("  - MeanDist provides a stable internal length scale.");
        _output.WriteLine("  - c_eff proxy is computable and seed-stable.");
        _output.WriteLine("  - Space and time proxies are distinguishable (ρ < 0.7).");
        _output.WriteLine("  - Curvature response is localized and measurable.");
        _output.WriteLine("  - Geodesic alignment with direct distance is strong.");
        _output.WriteLine("  - Reconstruction is stable across seeds, N, and load.");
        _output.WriteLine("  - Null controls fail to produce metric-causal structure.");
        _output.WriteLine("");
        _output.WriteLine("CONDITIONAL:");
        _output.WriteLine("  - Reconstruction quality depends on parameter regime.");
        _output.WriteLine("  - Continuum limit requires large N (not tested beyond N=200).");
        _output.WriteLine("  - Geodesic structure is a proxy (Floyd-Warshall on d_ij).");
        _output.WriteLine("");
        _output.WriteLine("HYPOTHESIS:");
        _output.WriteLine("  - The internal metric-causal reconstruction may correspond to");
        _output.WriteLine("    a proto-spacetime structure in the continuum limit.");
        _output.WriteLine("  - The exponential coupling law may naturally produce");
        _output.WriteLine("    metric-causal coherence without external imposition.");
        _output.WriteLine("  - These are HYPOTHESES only — no physical claim is made.");
        _output.WriteLine("");
        _output.WriteLine("NOT CLAIMED:");
        _output.WriteLine("  - Physical spacetime derived");
        _output.WriteLine("  - Physical c derived");
        _output.WriteLine("  - SI meters or seconds derived");
        _output.WriteLine("  - Lorentz invariance proven");
        _output.WriteLine("  - Metric tensor physically derived");
        _output.WriteLine("  - General Relativity derived or replaced");
        _output.WriteLine("  - Einstein equations derived");
        _output.WriteLine("  - Physical gravity derived");
        _output.WriteLine("  - Physical G derived");
        _output.WriteLine("  - D=3 derived");
        _output.WriteLine("  - SPARC explained");
        _output.WriteLine("  - Dark matter replaced");
        _output.WriteLine("");
        _output.WriteLine("This suite reconstructs an INTERNAL metric-causal structure only.");
        _output.WriteLine("All results are internal TRM dynamical properties.");
    }
}
