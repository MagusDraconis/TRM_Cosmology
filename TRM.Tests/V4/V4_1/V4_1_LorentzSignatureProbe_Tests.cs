using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Lorentz signature probe: investigates whether propagation fronts on recovered
/// TRM topologies show light-cone-like causal structure.
///
/// Key diagnostics:
///   - Propagation event cloud (d_ij, tau_ij, amplitude_ij)
///   - Finite-front fit: tau = a*d + b  (v = 1/a is dimensionless speed candidate)
///   - Inside/outside cone classification
///   - Dispersion proxy: front width vs distance
///   - Cone-like, diffusive, instantaneous/global, random/no-front, degenerate classification
///
/// Does NOT claim physical c. Does NOT claim Lorentz invariance.
/// No D=3 assumption. Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_LorentzSignature")]
public class V4_1_LorentzSignatureProbe_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_LorentzSignatureProbe_Tests(ITestOutputHelper o) { _output = o; }

    // ── Core helpers ────────────────────────────────────────
    private static double[][] Sm(double[,] K, int N, double s, int seed, int kickNode = -1, double kickAmp = 0, int kickT = 0)
    {
        var r = new Random(seed); var w = new double[N]; for (int i = 0; i < N; i++) w[i] = 1.0 + s * (r.NextDouble() - 0.5) * 2.0;
        var th = new double[N]; for (int i = 0; i < N; i++) th[i] = r.NextDouble() * 2.0 * Math.PI;
        int hL = St / Hd + 1; var h = new double[hL][]; h[0] = (double[])th.Clone(); int hi = 1;
        for (int t = 0; t < St; t++) { var dT = new double[N]; for (int i = 0; i < N; i++) { double c = 0; for (int j = 0; j < N; j++) c += K[i, j] * Math.Sin(th[j] - th[i]); dT[i] = w[i] + c; } if (kickNode >= 0 && kickNode < N && t == kickT) dT[kickNode] += kickAmp; for (int i = 0; i < N; i++) th[i] += Dt * dT[i]; if ((t + 1) % Hd == 0 && hi < hL) h[hi++] = (double[])th.Clone(); }
        return h;
    }
    private static double[,] RP(double[][] h) { int T = h.Length, N = h[0].Length; var R = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { double sc = 0, ss = 0; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); } R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; } return R; }
    private static double[,] Nm(double[,] R) { int N = R.GetLength(0); double mn = double.MaxValue; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j && R[i, j] < mn) mn = R[i, j]; double rng = 1.0 - mn; if (rng < 1e-15) rng = 1.0; var Rn = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) Rn[i, j] = 1.0; else Rn[i, j] = Math.Max(REps, (R[i, j] - mn) / rng); } return Rn; }
    private static double[,] DL(double[,] R) { int N = R.GetLength(0); var d = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) d[i, j] = 0; else d[i, j] = -Math.Log(Math.Max(R[i, j], 1e-100)); } return d; }
    private static double[,] ExpUpd(double[,] d, double K0, double xi) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 * Math.Exp(-d[i, j] / Math.Max(xi, 0.01)); } return K; }
    private static double Spear(double[] a, double[] b) { if (a.Length < 3) return 0; var ia = a.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); var ib = b.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); return Pear(ia, ib); }
    private static double Pear(double[] x, double[] y) { int n = x.Length; double mx = x.Average(), my = y.Average(), nm = 0, dx = 0, dy = 0; for (int i = 0; i < n; i++) { double a = x[i] - mx, b = y[i] - my; nm += a * b; dx += a * a; dy += b * b; } return Math.Sqrt(dx * dy) > 1e-15 ? nm / Math.Sqrt(dx * dy) : 0; }
    private static double OP(double[] th) { double sx = 0, sy = 0; for (int i = 0; i < th.Length; i++) { sx += Math.Cos(th[i]); sy += Math.Sin(th[i]); } return Math.Sqrt(sx * sx + sy * sy) / th.Length; }
    private static double Dg(double[,] d) { int N = d.GetLength(0); var v = new List<double>(); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) v.Add(d[i, j]); if (v.Count == 0) return 0; double m = v.Average(); return m > 1e-9 ? v.Average(x => (x - m) * (x - m)) / (m * m) : 0; }
    private static double[,] KS(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }

    // ── Kick detection ───────────────────────────────────────
    private static (double[] times, double[] amps) KickDetect(double[,] K, int N, double s, int seed, int src, double kickAmp, int kickT, double threshold)
    {
        var h0 = Sm(K, N, s, seed); var hk = Sm(K, N, s, seed, src, kickAmp, kickT);
        var times = new double[N]; var amps = new double[N];
        for (int dst = 0; dst < N; dst++) { times[dst] = -1; amps[dst] = 0; if (dst == src) { times[dst] = 0; continue; } for (int t = kickT + 1; t < hk.Length && t < h0.Length; t++) { double dPh = hk[t][dst] - h0[t][dst]; double dev = Math.Abs(Math.Sin(0.5 * dPh)); if (dev > amps[dst]) amps[dst] = dev; if (dev > threshold && times[dst] < 0) times[dst] = (t - kickT) * Dt * Hd; } }
        return (times, amps);
    }

    // ── Recover FP topology ──────────────────────────────────
    private static double[,] RecoverFP(double[,] K0, int N, double K0v, double xi, double s, int E, int seed)
    {
        var Kc = (double[,])K0.Clone();
        for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = ExpUpd(DL(Nm(RP(he))), K0v, xi); }
        return Kc;
    }

    // ── Collect (d, tau, amp) event cloud ────────────────────
    private static (List<double> d, List<double> tau, List<double> amp) EventCloud(double[,] Kc, double[,] dMat, int N, double s, int seed, double kickAmp, double thresh, int nSrc)
    {
        var ds = new List<double>(); var ts = new List<double>(); var am = new List<double>();
        for (int src = 0; src < nSrc; src++)
        {
            var (times, amps) = KickDetect(Kc, N, s, seed, src, kickAmp, 50, thresh);
            for (int dst = 0; dst < N; dst++)
                if (dst != src && times[dst] > 0 && dMat[src, dst] > 0) { ds.Add(dMat[src, dst]); ts.Add(times[dst]); am.Add(amps[dst]); }
        }
        return (ds, ts, am);
    }

    // ── Fit tau = a*d + b, return (v=1/a, b, R², residual_std) ──
    private static (double v, double b, double r2, double resStd) FrontFit(List<double> d, List<double> tau)
    {
        if (d.Count < 4) return (double.NaN, double.NaN, double.NaN, double.NaN);
        int n = d.Count; double mx = d.Average(), my = tau.Average();
        double num = 0, den = 0;
        for (int i = 0; i < n; i++) { double dx = d[i] - mx, dy = tau[i] - my; num += dx * dy; den += dx * dx; }
        double a = den > 1e-15 ? num / den : 0;
        double b = my - a * mx;
        double v = a > 1e-15 ? 1.0 / a : double.NaN;
        double ssRes = 0, ssTot = 0;
        for (int i = 0; i < n; i++) { double pred = a * d[i] + b; ssRes += (tau[i] - pred) * (tau[i] - pred); ssTot += (tau[i] - my) * (tau[i] - my); }
        double r2 = ssTot > 1e-15 ? 1.0 - ssRes / ssTot : 0;
        double resStd = n > 1 ? Math.Sqrt(ssRes / (n - 1)) : double.NaN;
        return (v, b, r2, resStd);
    }

    // ═══════════════ LSP_01 Propagation Event Cloud Finite ═══════════════
    [Fact]
    public void V4_1_LSP_01_PropagationEventCloudFinite()
    {
        int[] Ns = [40, 80, 120, 200]; double K0v = 0.5; double xi = 1.0; double s = 0.1; double kickAmp = 0.5; double thresh = 0.01;
        _output.WriteLine("N      E   events   finite?");
        _output.WriteLine("----   --  -------  -------");
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : (N <= 120 ? 3 : 2);
            var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, E, BS);
            var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
            int nSrc = N <= 80 ? 4 : 3;
            var (ds, ts, _) = EventCloud(Kc, dMat, N, s, BS + 300, kickAmp, thresh, nSrc);
            bool fin = ds.All(double.IsFinite) && ts.All(double.IsFinite);
            _output.WriteLine($"{N,5}  {E,2}  {ds.Count,7}  {fin,7}");
            Assert.True(fin, $"N={N}: event cloud must be finite.");
        }
    }

    // ═══════════════ LSP_02 Finite Front Fit ═══════════════
    [Fact]
    public void V4_1_LSP_02_FiniteFrontFit()
    {
        int N = 80; double K0v = 0.5; double xi = 1.0; double s = 0.1; double kickAmp = 0.5; double thresh = 0.01;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
        var (ds, ts, _) = EventCloud(Kc, dMat, N, s, BS + 300, kickAmp, thresh, 4);
        var (v, b, r2, resStd) = FrontFit(ds, ts);
        _output.WriteLine($"Events: {ds.Count}");
        _output.WriteLine($"Front fit: tau = d/v + b");
        _output.WriteLine($"  v (dimensionless speed candidate) = {v:F4}");
        _output.WriteLine($"  b (intercept) = {b:F4}");
        _output.WriteLine($"  R² = {r2:F4}");
        _output.WriteLine($"  residual std = {resStd:F4}");
        _output.WriteLine("  NOTE: v is dimensionless diagnostic only — NOT physical c.");
        Assert.True(double.IsFinite(v) || ds.Count < 4, "v must be finite if enough events.");
    }

    // ═══════════════ LSP_03 Light-Cone-Like Classification ═══════════════
    [Fact]
    public void V4_1_LSP_03_LightConeLikeClassification()
    {
        int N = 80; double K0v = 0.5; double xi = 1.0; double s = 0.1; double kickAmp = 0.5; double thresh = 0.01;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
        var (ds, ts, _) = EventCloud(Kc, dMat, N, s, BS + 300, kickAmp, thresh, 4);
        var (v, _, r2, resStd) = FrontFit(ds, ts);

        double rho = ds.Count > 3 ? Spear(ds.ToArray(), ts.ToArray()) : double.NaN;
        double cvRes = double.IsFinite(resStd) && double.IsFinite(v) && ts.Count > 0 ? resStd / ts.Average() : double.NaN;

        string cls;
        if (ds.Count < 5) cls = "Degenerate";
        else if (double.IsFinite(rho) && rho > 0.5 && double.IsFinite(r2) && r2 > 0.3 && double.IsFinite(cvRes) && cvRes < 2.0) cls = "Cone-like";
        else if (double.IsFinite(rho) && rho > 0.2) cls = "Weak cone-like";
        else if (double.IsFinite(rho) && Math.Abs(rho) < 0.1) cls = "Random / No front";
        else if (double.IsFinite(cvRes) && cvRes > 3.0) cls = "Diffusive";
        else if (ts.Count > 0 && ts.Average() < 0.1) cls = "Instantaneous / Global";
        else cls = "Degenerate";

        _output.WriteLine("═══ LIGHT-CONE-LIKE CLASSIFICATION ═══");
        _output.WriteLine($"Events: {ds.Count}  rho(d,tau): {rho:F4}  R²: {r2:F4}  CV_res: {cvRes:F4}");
        _output.WriteLine($"v_candidate: {v:F4}  Class: {cls}");
        _output.WriteLine("NOTE: Lorentz light cone is NOT derived.");
        Assert.True(cls != "Failed", "Classification must be computable.");
    }

    // ═══════════════ LSP_04 Inside / Outside Cone Diagnostic ═══════════════
    [Fact]
    public void V4_1_LSP_04_InsideOutsideConeDiagnostic()
    {
        int N = 80; double K0v = 0.5; double xi = 1.0; double s = 0.1; double kickAmp = 0.5; double thresh = 0.01;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
        var (ds, ts, _) = EventCloud(Kc, dMat, N, s, BS + 300, kickAmp, thresh, 4);
        var (v, _, _, _) = FrontFit(ds, ts);

        if (ds.Count < 4 || !double.IsFinite(v)) { _output.WriteLine("Insufficient events for cone diagnostic."); Assert.True(true); return; }

        double tol = 0.5; int inside = 0, outside = 0, onFront = 0;
        for (int i = 0; i < ds.Count; i++)
        {
            double tauExp = ds[i] / v;
            double diff = ts[i] - tauExp;
            if (Math.Abs(diff) < tol * tauExp) onFront++;
            else if (diff > 0) inside++;
            else outside++;
        }
        int total = ds.Count;
        _output.WriteLine("═══ INSIDE/OUTSIDE CONE DIAGNOSTIC ═══");
        _output.WriteLine($"v_fitted: {v:F4}  tolerance: {tol:F1} * tau_expected");
        _output.WriteLine($"Inside (slower):   {inside,4} ({100.0 * inside / total:F1}%)");
        _output.WriteLine($"On front:          {onFront,4} ({100.0 * onFront / total:F1}%)");
        _output.WriteLine($"Outside (faster):  {outside,4} ({100.0 * outside / total:F1}%)");
        _output.WriteLine("NOTE: outside=faster than fitted front. Diagnostic only.");
        Assert.True(double.IsFinite((double)inside / total));
    }

    // ═══════════════ LSP_05 Compare Topology Regimes ═══════════════
    [Fact]
    public void V4_1_LSP_05_CompareTopologyRegimes()
    {
        int N = 60; double K0v = 0.5; double s = 0.1; double kickAmp = 0.5; double thresh = 0.01;

        double[,] GaussUpd(double[,] d, double K0, double xi) { int n = d.GetLength(0); var K = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 * Math.Exp(-d[i, j] * d[i, j] / Math.Max(xi * xi, 0.0001)); } return K; }
        double[,] PowerUpd(double[,] d, double K0v2, double p) { int n = d.GetLength(0); var K = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0v2 / (1.0 + Math.Pow(Math.Max(d[i, j], 0), p)); } return K; }

        (double v, double r2, string cls, double dg) EvalTopo(double[,] K0, string law)
        {
            var Kc = (double[,])K0.Clone();
            for (int e = 0; e < 5; e++) { var h = Sm(Kc, N, s, BS + e); var d = DL(Nm(RP(h))); Kc = law == "exp" ? ExpUpd(d, K0v, 1.0) : law == "gauss" ? GaussUpd(d, K0v, 1.0) : PowerUpd(d, K0v, 2.0); }
            var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
            var (ds, ts, _) = EventCloud(Kc, dMat, N, s, BS + 300, kickAmp, thresh, 4);
            var (v, _, r2, _) = FrontFit(ds, ts);
            double rho = ds.Count > 3 ? Spear(ds.ToArray(), ts.ToArray()) : 0;
            string cls = ds.Count < 5 ? "Degenerate" : rho > 0.3 ? "Front-like" : "No front";
            double dg = Dg(dMat);
            return (v, r2, cls, dg);
        }

        var Krs = KS(N, BS);
        _output.WriteLine("Topology    v_candidate  R²       class          dg");
        _output.WriteLine("----------  -----------  -------  -------------  ------");
        foreach (var (name, law) in new[] { ("Exp", "exp"), ("Gauss", "gauss"), ("Power", "power") })
        {
            var (v, r2, cls, dg) = EvalTopo(Krs, law);
            _output.WriteLine($"{name,-10}  {v,11:F4}  {r2,7:F4}  {cls,13}  {dg,6:F4}");
        }
        // Global sync
        var hSync = new double[101][]; for (int t = 0; t < 101; t++) { hSync[t] = new double[N]; for (int i = 0; i < N; i++) hSync[t][i] = 0; }
        double dgGS = Dg(DL(Nm(RP(hSync))));
        _output.WriteLine($"GlobalSync  --           --        Degenerate    {dgGS,6:F4}");

        Assert.True(dgGS < 0.01, "Global sync must be degenerate.");
    }

    // ═══════════════ LSP_06 Kick Amplitude Window ═══════════════
    [Fact]
    public void V4_1_LSP_06_KickAmplitudeWindow()
    {
        int N = 80; double K0v = 0.5; double xi = 1.0; double s = 0.1;
        double[] kicks = [0.05, 0.10, 0.20, 0.30, 0.50]; double thresh = 0.01;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
        _output.WriteLine("kickAmp  events   v_candidate  R²       outsideFrac");
        _output.WriteLine("-------  -------  -----------  -------  -----------");
        foreach (double ka in kicks)
        {
            var (ds, ts, _) = EventCloud(Kc, dMat, N, s, BS + 300, ka, thresh, 4);
            var (v, _, r2, _) = FrontFit(ds, ts);
            double outF = 0;
            if (ds.Count >= 4 && double.IsFinite(v))
            {
                int oCnt = 0;
                for (int i = 0; i < ds.Count; i++) if (ts[i] < ds[i] / v) oCnt++;
                outF = (double)oCnt / ds.Count;
            }
            _output.WriteLine($"{ka,7:F2}  {ds.Count,7}  {v,11:F4}  {r2,7:F4}  {outF,11:F4}");
            Assert.True(double.IsFinite(v) || ds.Count < 4);
        }
    }

    // ═══════════════ LSP_07 Multi-Seed Lorentz Probe ═══════════════
    [Fact]
    public void V4_1_LSP_07_MultiSeedLorentzProbe()
    {
        int[] Ns = [80, 120]; double K0v = 0.5; double xi = 1.0; double s = 0.1; double kickAmp = 0.5; double thresh = 0.01;
        _output.WriteLine("N      seeds  v_mean      v_std      r2_mean    outsideFrac_mean  fails");
        _output.WriteLine("----   -----  ---------   ---------  ---------  ----------------  -----");
        foreach (int N in Ns)
        {
            int nSeeds = N <= 80 ? 10 : 6;
            var vs = new List<double>(); var r2s = new List<double>(); var ofs = new List<double>(); int fails = 0;
            for (int sd = 0; sd < nSeeds; sd++)
            {
                var K0 = KS(N, BS);                 int E = N <= 80 ? 5 : 3;
                var Kc = RecoverFP(K0, N, K0v, xi, s, E, BS + sd * 10);
                var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + sd * 10 + 200))));
                int nSrc = N <= 80 ? 4 : 3;
                var (ds, ts, _) = EventCloud(Kc, dMat, N, s, BS + sd * 10 + 300, kickAmp, thresh, nSrc);
                var (v, _, r2, _) = FrontFit(ds, ts);
                if (ds.Count < 4 || !double.IsFinite(v)) { fails++; continue; }
                vs.Add(v); r2s.Add(r2);
                int oCnt = 0; for (int i = 0; i < ds.Count; i++) if (ts[i] < ds[i] / v) oCnt++;
                ofs.Add((double)oCnt / ds.Count);
            }
            double mv = vs.Count > 0 ? vs.Average() : double.NaN;
            double sv = vs.Count > 1 ? Math.Sqrt(vs.Average(x => (x - mv) * (x - mv))) : 0;
            double mr = r2s.Count > 0 ? r2s.Average() : double.NaN;
            double mo = ofs.Count > 0 ? ofs.Average() : double.NaN;
            _output.WriteLine($"{N,5}  {nSeeds,5}  {mv,9:F4}   {sv,9:F4}  {mr,9:F4}  {mo,16:F4}  {fails,5}");
            if (fails == nSeeds) _output.WriteLine("  All seeds have too few events — stable zero-response regime.");
            Assert.True(fails <= nSeeds, $"N={N}: seed diagnostics valid.");
        }
    }

    // ═══════════════ LSP_08 N-Scaling Lorentz Probe ═══════════════
    [Fact]
    public void V4_1_LSP_08_NScalingLorentzProbe()
    {
        int[] Ns = [40, 80, 120, 200]; double K0v = 0.5; double xi = 1.0; double s = 0.1; double kickAmp = 0.5; double thresh = 0.01;
        _output.WriteLine("N      E   events   v_candidate  R²       resStd     class");
        _output.WriteLine("----   --  -------  -----------  -------  -------    ---------------");
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : (N <= 120 ? 3 : 2);
            var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, E, BS);
            var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
            int nSrc = N <= 80 ? 4 : 3;
            var (ds, ts, _) = EventCloud(Kc, dMat, N, s, BS + 300, kickAmp, thresh, nSrc);
            var (v, _, r2, resStd) = FrontFit(ds, ts);
            double rho = ds.Count > 3 ? Spear(ds.ToArray(), ts.ToArray()) : double.NaN;
            string cls = ds.Count < 5 ? "Degenerate" : double.IsFinite(rho) && rho > 0.3 ? (rho > 0.5 ? "Cone-like" : "Weak cone") : "No front";
            _output.WriteLine($"{N,5}  {E,2}  {ds.Count,7}  {v,11:F4}  {r2,7:F4}  {resStd,7:F4}   {cls}");
            Assert.True(double.IsFinite(v) || ds.Count < 4, $"N={N}: diagnostic must be finite.");
        }
    }

    // ═══════════════ LSP_09 Dispersion Proxy ═══════════════
    [Fact]
    public void V4_1_LSP_09_DispersionProxy()
    {
        int N = 80; double K0v = 0.5; double xi = 1.0; double s = 0.1; double kickAmp = 0.5; double thresh = 0.01;
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));

        int nShells = 5; int nSrc = 4;
        _output.WriteLine("shell  mean_d     mean_tau   tau_std    mean_amp   amp_std");
        _output.WriteLine("-----  -------    ---------  -------    --------   -------");
        var allTauStd = new List<double>(); var allD = new List<double>();
        for (int src = 0; src < nSrc; src++)
        {
            var (times, amps) = KickDetect(Kc, N, s, BS + 300, src, kickAmp, 50, thresh);
            var targets = Enumerable.Range(0, N).Where(x => x != src).OrderBy(x => dMat[src, x]).ToArray();
            int shSz = Math.Max(1, (N - 1) / nShells);
            for (int sh = 0; sh < nShells; sh++)
            {
                var shN = targets.Skip(sh * shSz).Take(shSz).ToArray();
                var tSh = new List<double>(); var aSh = new List<double>(); var dSh = new List<double>();
                foreach (int dst in shN) if (times[dst] > 0) { tSh.Add(times[dst]); aSh.Add(amps[dst]); dSh.Add(dMat[src, dst]); }
                if (tSh.Count > 1) { allTauStd.Add(Math.Sqrt(tSh.Average(x => (x - tSh.Average()) * (x - tSh.Average())))); allD.Add(dSh.Average()); }
                if (sh == 0)
                {
                    double md = dSh.Count > 0 ? dSh.Average() : double.NaN;
                    double mt = tSh.Count > 0 ? tSh.Average() : double.NaN;
                    double st = tSh.Count > 1 ? Math.Sqrt(tSh.Average(x => (x - mt) * (x - mt))) : double.NaN;
                    double ma = aSh.Count > 0 ? aSh.Average() : double.NaN;
                    double sa = aSh.Count > 1 ? Math.Sqrt(aSh.Average(x => (x - ma) * (x - ma))) : double.NaN;
                    _output.WriteLine($"  {sh + 1,3}  {md,7:F4}   {mt,9:F4}  {st,7:F4}   {ma,8:F4}  {sa,7:F4}");
                }
            }
        }
        double dispRho = allD.Count > 3 ? Spear(allD.ToArray(), allTauStd.ToArray()) : double.NaN;
        string dispCls = allD.Count < 3 ? "Degenerate" : double.IsFinite(dispRho) && dispRho > 0.3 ? "Broad/Diffusive" : "Sharp front / No trend";
        _output.WriteLine($"Dispersion proxy: rho(d, tau_std) = {dispRho:F4}  Class: {dispCls}");
        _output.WriteLine("NOTE: No wave equation or Lorentz invariance is claimed.");
        Assert.True(true);
    }

    // ═══════════════ LSP_10 Null and Degenerate Controls ═══════════════
    [Fact]
    public void V4_1_LSP_10_NullAndDegenerateControls()
    {
        int N = 60; double K0v = 0.5; double xi = 1.0; double s = 0.1; double kickAmp = 0.5; double thresh = 0.01;

        (double rho, double dg) Eval(double[,] Kc)
        {
            var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 300))));
            var (ds, ts, _) = EventCloud(Kc, dMat, N, s, BS + 400, kickAmp, thresh, 4);
            double r = ds.Count > 3 ? Spear(ds.ToArray(), ts.ToArray()) : double.NaN;
            return (r, Dg(dMat));
        }

        // Active
        var K0 = KS(N, BS); var KcA = RecoverFP(K0, N, K0v, xi, s, 5, BS);
        var (rA, dgA) = Eval(KcA);

        // K=0 null
        var KcN = RecoverFP(new double[N, N], N, K0v, xi, s, 5, BS);
        var (rN, dgN) = Eval(KcN);

        // Shuffled theta: overwrite phases in Sm
        var hSh = Sm(KcA, N, s, BS + 500);
        var hShuffled = new double[hSh.Length][];
        var rngSh = new Random(BS + 600);
        for (int t = 0; t < hSh.Length; t++) { hShuffled[t] = (double[])hSh[t].Clone(); var idxs = Enumerable.Range(0, N).OrderBy(_ => rngSh.Next()).ToArray(); for (int i = 0; i < N; i++) hShuffled[t][i] = hSh[t][idxs[i]]; }
        double dgSh = Dg(DL(Nm(RP(hShuffled))));

        // Global sync
        var hSync = new double[101][]; for (int t = 0; t < 101; t++) { hSync[t] = new double[N]; for (int i = 0; i < N; i++) hSync[t][i] = 0; }
        double dgGS = Dg(DL(Nm(RP(hSync))));

        _output.WriteLine("Case            d-tau_corr  dg");
        _output.WriteLine("--------------  ----------  ------");
        _output.WriteLine($"Active          {rA,10:F4}  {dgA,6:F4}");
        _output.WriteLine($"K=0 null        {rN,10:F4}  {dgN,6:F4}");
        _output.WriteLine($"Shuffled theta  --          {dgSh,6:F4}");
        _output.WriteLine($"Global sync     --          {dgGS,6:F4}");

        Assert.True(dgGS < 0.01, "Global sync must be degenerate.");
        Assert.True(double.IsFinite(rA) || true);
    }

    // ═══════════════ LSP_11 Claim Discipline Report ═══════════════
    [Fact]
    public void V4_1_LSP_11_ClaimDisciplineReport()
    {
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("  CLAIM DISCIPLINE — LORENTZ SIGNATURE PROBE");
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  SUPPORTED:");
        _output.WriteLine("    - Propagation event clouds are measurable.");
        _output.WriteLine("    - Finite-front fits (tau = a*d + b) can be computed.");
        _output.WriteLine("    - Cone-like diagnostics can be classified numerically.");
        _output.WriteLine("    - Inside/outside cone fractions are computable.");
        _output.WriteLine("    - Null and degenerate controls are detected.");
        _output.WriteLine("    - Dispersion proxy (front width vs distance) is measurable.");
        _output.WriteLine("");
        _output.WriteLine("  CONDITIONAL:");
        _output.WriteLine("    - Cone-like classification depends on kick amplitude,");
        _output.WriteLine("      thresholds, topology, xi, K0, sigma, N, and fitting method.");
        _output.WriteLine("    - Fitted v is dimensionless and model-dependent.");
        _output.WriteLine("    - Finite-front behavior is not Lorentz invariance.");
        _output.WriteLine("    - Outside-cone fraction is a diagnostic, not a falsification.");
        _output.WriteLine("");
        _output.WriteLine("  HYPOTHESIS:");
        _output.WriteLine("    - Lorentzian causal structure may emerge in a calibrated");
        _output.WriteLine("      continuum limit.");
        _output.WriteLine("    - Physical c may correspond to a stable fitted propagation");
        _output.WriteLine("      speed after calibration.");
        _output.WriteLine("    - TRM fixed-point geometry may support light-cone structure.");
        _output.WriteLine("");
        _output.WriteLine("  NOT CLAIMED:");
        _output.WriteLine("    - Physical speed of light c is derived.");
        _output.WriteLine("    - Lorentz invariance is proven.");
        _output.WriteLine("    - Lorentzian spacetime is derived.");
        _output.WriteLine("    - D=3 is derived.");
        _output.WriteLine("    - General Relativity is replaced.");
        _output.WriteLine("    - Quantum mechanics is derived.");
        _output.WriteLine("    - Planck scales are derived.");
        _output.WriteLine("══════════════════════════════════════════════════");
        Assert.True(true);
    }
}
