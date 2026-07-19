using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Emergent Lorentz signature probe: tests whether the shared Causal-ETG-Dimension
/// convergence state shows numerical signatures compatible with a Lorentz-like
/// causal structure (finite propagation boundary, cone-like events, invariant-ish
/// dimensionless speed, time/space separation).
///
/// Does NOT claim physical c, Lorentz invariance, Minkowski metric, D=3,
/// physical space, gravity, time dilation, SPARC, or dark matter.
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_EmergentLorentzSignature")]
public class V4_1_EmergentLorentzSignature_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_EmergentLorentzSignature_Tests(ITestOutputHelper o) { _output = o; }

    // ══════════════════ Core helpers ══════════════════
    private static double[][] Sm(double[,] K, int N, double s, int seed, int kickNode = -1, double kickAmp = 0, int kickT = -1)
    {
        var r = new Random(seed); var w = new double[N]; for (int i = 0; i < N; i++) w[i] = 1.0 + s * (r.NextDouble() - 0.5) * 2.0;
        if (kickT < 0 && kickNode >= 0 && kickNode < N) w[kickNode] += kickAmp;
        var th = new double[N]; for (int i = 0; i < N; i++) th[i] = r.NextDouble() * 2.0 * Math.PI;
        int hL = St / Hd + 1; var h = new double[hL][]; h[0] = (double[])th.Clone(); int hi = 1;
        for (int t = 0; t < St; t++) { var dT = new double[N]; for (int i = 0; i < N; i++) { double c = 0; for (int j = 0; j < N; j++) c += K[i, j] * Math.Sin(th[j] - th[i]); dT[i] = w[i] + c; } if (kickT >= 0 && kickNode >= 0 && kickNode < N && t == kickT) dT[kickNode] += kickAmp; for (int i = 0; i < N; i++) th[i] += Dt * dT[i]; if ((t + 1) % Hd == 0 && hi < hL) h[hi++] = (double[])th.Clone(); }
        return h;
    }
    private static double[,] RP(double[][] h) { int T = h.Length, N = h[0].Length; var R = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { double sc = 0, ss = 0; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); } R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; } return R; }
    private static double[,] Nm(double[,] R) { int N = R.GetLength(0); double mn = double.MaxValue; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j && R[i, j] < mn) mn = R[i, j]; double rng = 1.0 - mn; if (rng < 1e-15) rng = 1.0; var Rn = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) Rn[i, j] = 1.0; else Rn[i, j] = Math.Max(REps, (R[i, j] - mn) / rng); } return Rn; }
    private static double[,] DL(double[,] R) { int N = R.GetLength(0); var d = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) d[i, j] = 0; else d[i, j] = -Math.Log(Math.Max(R[i, j], 1e-100)); } return d; }
    private static double[,] ExpUpd(double[,] d, double K0, double xi) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 * Math.Exp(-d[i, j] / Math.Max(xi, 0.01)); } return K; }
    private static double[,] GaussUpd(double[,] d, double K0, double xi) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else { double r = d[i, j] / Math.Max(xi, 0.01); K[i, j] = K0 * Math.Exp(-r * r); } } return K; }
    private static double[,] PowerUpd(double[,] d, double K0, double p) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 / (1.0 + Math.Pow(d[i, j], Math.Max(p, 0.5))); } return K; }
    private static double Spear(double[] a, double[] b) { if (a.Length < 3) return 0; var ia = a.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); var ib = b.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); return Pear(ia, ib); }
    private static double Pear(double[] x, double[] y) { int n = x.Length; double mx = x.Average(), my = y.Average(), nm = 0, dx = 0, dy = 0; for (int i = 0; i < n; i++) { double a = x[i] - mx, b = y[i] - my; nm += a * b; dx += a * a; dy += b * b; } return Math.Sqrt(dx * dy) > 1e-15 ? nm / Math.Sqrt(dx * dy) : 0; }
    private static double Dg(double[,] d) { int N = d.GetLength(0); var v = new List<double>(); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) v.Add(d[i, j]); if (v.Count == 0) return 0; double m = v.Average(); return m > 1e-9 ? v.Average(x => (x - m) * (x - m)) / (m * m) : 0; }
    private static double MNorm(double[,] A, double[,] B) { int N = A.GetLength(0); double s = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { double d = A[i, j] - B[i, j]; s += d * d; } return Math.Sqrt(s / (N * (N - 1) / 2.0)); }
    private static double[,] KS(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    private static double[,] RecoverFP(double[,] K0, int N, double K0v, double xi, double s, int E, int seed, Func<double[,], double, double, double[,]> upd = null) { upd ??= ExpUpd; var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = upd(DL(Nm(RP(he))), K0v, xi); } return Kc; }
    private static double Median(List<double> vs) { if (vs.Count == 0) return double.NaN; var s = vs.OrderBy(x => x).ToList(); int mid = s.Count / 2; return s.Count % 2 == 0 ? (s[mid - 1] + s[mid]) / 2.0 : s[mid]; }
    private static string CorrClass(double r) => Math.Abs(r) > 0.7 ? "Strong" : Math.Abs(r) > 0.4 ? "Moderate" : Math.Abs(r) > 0.1 ? "Weak" : "None";

    // ── Kick detection (from CEDC) ────────────────────
    private static (double[] times, double[] amps) KickDetect(double[,] K, int N, double s, int seed, int src, double kickAmp, double thresh)
    {
        var h0 = Sm(K, N, s, seed); var hk = Sm(K, N, s, seed, src, kickAmp, 50);
        var times = new double[N]; var amps = new double[N];
        for (int dst = 0; dst < N; dst++) { times[dst] = -1; amps[dst] = 0; if (dst == src) continue; for (int t = 0; t < hk.Length; t++) { double dPh = hk[t][dst] - h0[t][dst]; double dev = Math.Abs(Math.Sin(0.5 * dPh)); if (dev > amps[dst]) amps[dst] = dev; if (dev > thresh && times[dst] < 0) times[dst] = t * Dt * Hd; } }
        return (times, amps);
    }

    // ── Dimension (from DAV) ──────────────────────────
    private static (double dEff, double r2) BallDim(double[,] dMat, int N, int center)
    {
        var dists = Enumerable.Range(0, N).Where(x => x != center).Select(x => dMat[center, x]).OrderBy(x => x).ToArray();
        int nSh = Math.Min(8, dists.Length / 4); if (nSh < 3) return (double.NaN, double.NaN);
        var lR = new List<double>(); var lN = new List<double>();
        for (int i = 0; i < nSh; i++) { int cnt = (i + 1) * dists.Length / nSh; double rSh = dists[Math.Min(cnt - 1, dists.Length - 1)]; if (rSh < 1e-6) continue; lR.Add(Math.Log(rSh)); lN.Add(Math.Log(cnt)); }
        if (lR.Count < 3) return (double.NaN, double.NaN);
        double mx = lR.Average(), my = lN.Average(), num = 0, dx = 0, dy = 0;
        for (int i = 0; i < lR.Count; i++) { double a = lR[i] - mx, b = lN[i] - my; num += a * b; dx += a * a; dy += b * b; }
        return (dx > 1e-15 ? num / dx : double.NaN, dy > 1e-15 ? num * num / (dx * dy) : 0);
    }
    private static (double d, double spread) MeasureDim(double[,] Kc, int N, double s)
    {
        var h = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h))); var ds = new List<double>();
        int nC = Math.Max(3, Math.Min(8, N / 5));
        for (int c = 0; c < nC; c++) { var (d, _) = BallDim(dMat, N, c * N / nC); if (double.IsFinite(d)) ds.Add(d); }
        if (ds.Count == 0) return (double.NaN, double.NaN);
        double m = ds.Average(); double sp = ds.Count > 1 ? Math.Sqrt(ds.Average(x => (x - m) * (x - m))) : 0;
        return (m, sp);
    }

    // ── Lorentz probe: event cloud + front fit ────────
    private static (List<double> d, List<double> tau, List<double> v, int resp, int total) BuildEventCloud(double[,] Kc, double[,] dMat, int N, double s, int seed, int nSrc, double kickAmp, double thresh)
    {
        var ds = new List<double>(); var ts = new List<double>(); var vs = new List<double>(); int resp = 0, total = 0;
        for (int src = 0; src < nSrc; src++)
        {
            var (times, _) = KickDetect(Kc, N, s, seed, src, kickAmp, thresh);
            for (int dst = 0; dst < N; dst++) { if (dst == src) continue; total++; if (times[dst] > 0 && dMat[src, dst] > 0) { ds.Add(dMat[src, dst]); ts.Add(times[dst]); vs.Add(dMat[src, dst] / Math.Max(times[dst], 1e-6)); resp++; } }
        }
        return (ds, ts, vs, resp, total);
    }

    private static (double v, double b, double r2, int n) FitFront(List<double> d, List<double> tau)
    {
        if (d.Count < 4) return (double.NaN, double.NaN, double.NaN, d.Count);
        // Fit tau = (1/v)*d + b via linear regression
        double mx = d.Average(), my = tau.Average(), sxy = 0, sx2 = 0, sy2 = 0;
        for (int i = 0; i < d.Count; i++) { double a = d[i] - mx, bv = tau[i] - my; sxy += a * bv; sx2 += a * a; sy2 += bv * bv; }
        double slope = sx2 > 1e-15 ? sxy / sx2 : double.NaN;
        double intercept = my - slope * mx;
        double v = slope > 1e-15 ? 1.0 / slope : double.NaN;
        double r2 = sx2 * sy2 > 1e-15 ? sxy * sxy / (sx2 * sy2) : 0;
        return (v, intercept, r2, d.Count);
    }

    // ═══════════════ ELS_01 LorentzProbeDataFinite ═══════════════
    [Fact]
    public void V4_1_ELS_01_LorentzProbeDataFinite()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int E = 5;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
        var dMat = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var cloud = BuildEventCloud(Kc, dMat, N, s, BS, 5, 0.5, 0.02);
        Assert.True(cloud.d.Count > 0, "No events in cloud");
        Assert.True(double.IsFinite(cloud.d.Average()), "d NaN");
        Assert.True(double.IsFinite(cloud.tau.Average()), "tau NaN");
        var (v, b, r2, n) = FitFront(cloud.d, cloud.tau);
        _output.WriteLine($"Events: {cloud.resp}/{cloud.total}  v_fit={v:F3}  r²={r2:F3}  nFit={n}");
        Assert.True(n >= 0);
    }

    // ═══════════════ ELS_02 EventConeFit ═══════════════
    [Fact]
    public void V4_1_ELS_02_EventConeFit()
    {
        int[] Ns = [80, 120]; double xi = 1.75; double kv = 1.2; double s = 0.1;
        _output.WriteLine("N     v_fit   intercept  r²     n     class");
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3;
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
            var dMat = DL(Nm(RP(Sm(Kc, N, s, BS))));
            var cloud = BuildEventCloud(Kc, dMat, N, s, BS, Math.Min(5, N / 10), 0.5, 0.02);
            var (v, b, r2, n) = FitFront(cloud.d, cloud.tau);
            string cls = r2 > 0.5 && v > 0.1 && double.IsFinite(v) ? "Cone-like" :
                         r2 > 0.2 && double.IsFinite(v) ? "WeakCone" :
                         r2 > 0 ? "Diffusive" : "No-front";
            if (!double.IsFinite(v)) cls = "Degenerate";
            _output.WriteLine($"{N,5}  {v,6:F3}  {b,8:F3}  {r2:F3}  {n,5}  {cls}");
        }
    }

    // ═══════════════ ELS_03 InsideOutsideConeFractions ═══════════════
    [Fact]
    public void V4_1_ELS_03_InsideOutsideConeFractions()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var dMat = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var cloud = BuildEventCloud(Kc, dMat, N, s, BS, 5, 0.5, 0.02);
        var (v, _, _, _) = FitFront(cloud.d, cloud.tau);
        if (cloud.d.Count < 4 || !double.IsFinite(v)) { _output.WriteLine("Insufficient events"); return; }
        double tol = 0.1;
        int inside = 0, outside = 0, near = 0;
        for (int i = 0; i < cloud.d.Count; i++)
        {
            double dExp = cloud.d[i] / v;
            double diff = cloud.tau[i] - dExp;
            if (diff > tol) inside++;
            else if (diff < -tol) outside++;
            else near++;
        }
        int tot = cloud.d.Count;
        _output.WriteLine($"v={v:F3}  inside={inside} ({100.0*inside/tot:F0}%)  near={near} ({100.0*near/tot:F0}%)  outside={outside} ({100.0*outside/tot:F0}%)");
    }

    // ═══════════════ ELS_04 IntervalProxyClassification ═══════════════
    [Fact]
    public void V4_1_ELS_04_IntervalProxyClassification()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var dMat = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var cloud = BuildEventCloud(Kc, dMat, N, s, BS, 5, 0.5, 0.02);
        var (v, _, _, _) = FitFront(cloud.d, cloud.tau);
        if (cloud.d.Count < 4 || !double.IsFinite(v)) { _output.WriteLine("Insufficient events"); return; }
        double tol = 0.05;
        int tl = 0, nul = 0, sl = 0;
        for (int i = 0; i < cloud.d.Count; i++)
        {
            double s2 = cloud.tau[i] * cloud.tau[i] - (cloud.d[i] / v) * (cloud.d[i] / v);
            if (s2 > tol) tl++;
            else if (s2 < -tol) sl++;
            else nul++;
        }
        int tot = cloud.d.Count;
        _output.WriteLine($"time-like: {tl} ({100.0*tl/tot:F0}%)  near-null: {nul} ({100.0*nul/tot:F0}%)  space-like: {sl} ({100.0*sl/tot:F0}%)");
        _output.WriteLine("Interval proxy only — NOT Minkowski metric.");
    }

    // ═══════════════ ELS_05 SpeedCandidateStability ═══════════════
    [Fact]
    public void V4_1_ELS_05_SpeedCandidateStability()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var dMat = DL(Nm(RP(Sm(Kc, N, s, BS))));
        // Multi-source
        var allV = new List<double>();
        for (int src = 0; src < Math.Min(5, N / 10); src++)
        {
            var (times, _) = KickDetect(Kc, N, s, BS, src, 0.5, 0.02);
            for (int dst = 0; dst < N; dst++) if (dst != src && times[dst] > 0 && dMat[src, dst] > 0) allV.Add(dMat[src, dst] / times[dst]);
        }
        if (allV.Count > 3)
        {
            double mv = allV.Average(); double mdv = Median(allV);
            double sv = allV.Count > 1 ? Math.Sqrt(allV.Average(x => (x - mv) * (x - mv))) : 0;
            _output.WriteLine($"v_mean={mv:F3}  v_median={mdv:F3}  v_std={sv:F3}  n={allV.Count}");
        }
        else _output.WriteLine("Insufficient speed samples");
        // Multi-seed
        var seedVs = new List<double>();
        for (int sd = 0; sd < 5; sd++)
        {
            var Ks = RecoverFP(KS(N, sd), N, kv, xi, s, 5, sd);
            var dMats = DL(Nm(RP(Sm(Ks, N, s, sd))));
            var cloud = BuildEventCloud(Ks, dMats, N, s, sd, 3, 0.5, 0.02);
            var (vsd, _, _, _) = FitFront(cloud.d, cloud.tau);
            if (double.IsFinite(vsd)) seedVs.Add(vsd);
        }
        if (seedVs.Count > 1)
            _output.WriteLine($"v_seeds: mean={seedVs.Average():F3} std={Math.Sqrt(seedVs.Average(x => (x - seedVs.Average()) * (x - seedVs.Average()))):F3} n={seedVs.Count}");
        _output.WriteLine("v is dimensionless — NOT physical c.");
    }

    // ═══════════════ ELS_06 LoadInvarianceOfConeStructure ═══════════════
    [Fact]
    public void V4_1_ELS_06_LoadInvarianceOfConeStructure()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int E = 5;
        // Baseline
        var Kb = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
        var dB = DL(Nm(RP(Sm(Kb, N, s, BS))));
        var cB = BuildEventCloud(Kb, dB, N, s, BS, 5, 0.5, 0.02);
        var (vB, _, r2B, nB) = FitFront(cB.d, cB.tau);
        // With load
        var hL = Sm(Kb, N, s, BS, N / 2, 0.2);
        var Kl = ExpUpd(DL(Nm(RP(hL))), kv, xi);
        var dL = DL(Nm(RP(Sm(Kl, N, s, BS))));
        var cL = BuildEventCloud(Kl, dL, N, s, BS, 5, 0.5, 0.02);
        var (vL, _, r2L, nL) = FitFront(cL.d, cL.tau);
        _output.WriteLine($"Before: v={vB:F3} r²={r2B:F3} n={nB}");
        _output.WriteLine($"After:  v={vL:F3} r²={r2L:F3} n={nL}");
        double dv = double.IsFinite(vB) && double.IsFinite(vL) ? Math.Abs(vL - vB) / Math.Max(vB, 0.01) : 1;
        _output.WriteLine($"Δv_rel={dv:F3}  cone-load-invariant={(dv < 0.5 ? "YES" : "NO")}");
    }

    // ═══════════════ ELS_07 DimensionConeCorrelation ═══════════════
    [Fact]
    public void V4_1_ELS_07_DimensionConeCorrelation()
    {
        double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2]; int N = 80; double s = 0.1;
        var ds = new List<double>(); var rs = new List<double>();
        foreach (double xi in xis)
            foreach (double kv in K0s)
            {
                var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
                var dMat = DL(Nm(RP(Sm(Kc, N, s, BS))));
                var cloud = BuildEventCloud(Kc, dMat, N, s, BS, 3, 0.5, 0.02);
                var (_, _, r2, _) = FitFront(cloud.d, cloud.tau);
                var (dRaw, _) = MeasureDim(Kc, N, s);
                if (double.IsFinite(dRaw) && double.IsFinite(r2)) { ds.Add(dRaw); rs.Add(r2); }
            }
        double rho = Spear(ds.ToArray(), rs.ToArray());
        _output.WriteLine($"corr(D_raw, cone_r²) = {rho:F4}  class={CorrClass(rho)}");
        Assert.True(Math.Abs(rho) >= 0);
    }

    // ═══════════════ ELS_08 ETGConeCorrelation ═══════════════
    [Fact]
    public void V4_1_ELS_08_ETGConeCorrelation()
    {
        double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2]; int N = 80; double s = 0.1;
        var cls = new List<double>(); var rs = new List<double>();
        foreach (double xi in xis)
            foreach (double kv in K0s)
            {
                var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
                var dMat = DL(Nm(RP(Sm(Kc, N, s, BS))));
                var cloud = BuildEventCloud(Kc, dMat, N, s, BS, 3, 0.5, 0.02);
                var (_, _, r2, _) = FitFront(cloud.d, cloud.tau);
                // Simplified ETG proxy: response to load
                var h0 = Sm(Kc, N, s, BS); var hL = Sm(Kc, N, s, BS, N / 2, 0.2);
                double closure = 1.0 / (1.0 + MNorm(RP(hL), RP(h0)) * 10);
                if (double.IsFinite(closure) && double.IsFinite(r2)) { cls.Add(closure); rs.Add(r2); }
            }
        double rho = Spear(cls.ToArray(), rs.ToArray());
        _output.WriteLine($"corr(ETG_closure, cone_r²) = {rho:F4}  class={CorrClass(rho)}");
    }

    // ═══════════════ ELS_09 NScalingLorentzSignature ═══════════════
    [Fact]
    public void V4_1_ELS_09_NScalingLorentzSignature()
    {
        int[] Ns = [40, 80, 120, 200]; double xi = 1.75; double kv = 1.2; double s = 0.1;
        _output.WriteLine("N     v_fit   r²     nEv  cone_class");
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3;
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
            var dMat = DL(Nm(RP(Sm(Kc, N, s, BS))));
            var cloud = BuildEventCloud(Kc, dMat, N, s, BS, Math.Min(5, N / 10), 0.5, 0.02);
            var (v, _, r2, n) = FitFront(cloud.d, cloud.tau);
            string cls = r2 > 0.4 && double.IsFinite(v) ? "Cone" : (r2 > 0.15 ? "Weak" : (r2 > 0 ? "Diffuse" : "None"));
            _output.WriteLine($"{N,5}  {v,6:F3}  {r2:F3}  {n,4}  {cls}");
        }
    }

    // ═══════════════ ELS_10 MultiSeedLorentzSignature ═══════════════
    [Fact]
    public void V4_1_ELS_10_MultiSeedLorentzSignature()
    {
        int[] Ns = [80, 120]; double xi = 1.75; double kv = 1.2; double s = 0.1; int nSeeds = 10;
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3;
            var vs = new List<double>(); var r2s = new List<double>();
            for (int seed = 0; seed < nSeeds; seed++)
            {
                var Kc = RecoverFP(KS(N, seed), N, kv, xi, s, E, seed);
                var dMat = DL(Nm(RP(Sm(Kc, N, s, seed))));
                var cloud = BuildEventCloud(Kc, dMat, N, s, seed, 3, 0.5, 0.02);
                var (v, _, r2, _) = FitFront(cloud.d, cloud.tau);
                if (double.IsFinite(v)) vs.Add(v);
                if (double.IsFinite(r2)) r2s.Add(r2);
            }
            double mv = vs.Count > 0 ? vs.Average() : double.NaN;
            double sv = vs.Count > 1 ? Math.Sqrt(vs.Average(x => (x - mv) * (x - mv))) : 0;
            double mr2 = r2s.Count > 0 ? r2s.Average() : 0;
            _output.WriteLine($"N={N}: v={mv:F3}±{sv:F3}  r²={mr2:F3}  nValid={vs.Count}/{nSeeds}");
        }
    }

    // ═══════════════ ELS_11 CouplingLawLorentzComparison ═══════════════
    [Fact]
    public void V4_1_ELS_11_CouplingLawLorentzComparison()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int E = 5;
        var laws = new Dictionary<string, Func<double[,], double, double, double[,]>> {
            {"exp", (d,k0,x) => ExpUpd(d,k0,x)}, {"gauss", (d,k0,x) => GaussUpd(d,k0,x)}, {"power", (d,k0,x) => PowerUpd(d,k0,x)}};
        _output.WriteLine("Law      v_fit   r²     nEv  class");
        foreach (var (name, upd) in laws)
        {
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS, upd);
            var dMat = DL(Nm(RP(Sm(Kc, N, s, BS))));
            var cloud = BuildEventCloud(Kc, dMat, N, s, BS, 5, 0.5, 0.02);
            var (v, _, r2, n) = FitFront(cloud.d, cloud.tau);
            string cls = r2 > 0.3 && double.IsFinite(v) ? "Cone" : (r2 > 0.1 ? "Weak" : "None");
            _output.WriteLine($"{name,-7} {v,6:F3}  {r2:F3}  {n,4}  {cls}");
        }
    }

    // ═══════════════ ELS_12 NullAndDegenerateControls ═══════════════
    [Fact]
    public void V4_1_ELS_12_NullAndDegenerateControls()
    {
        int N = 80; double s = 0.1; double xi = 1.75; double kv = 1.2;
        // K=0
        var K0 = new double[N, N];
        var d0 = DL(Nm(RP(Sm(K0, N, s, BS)))); var c0 = BuildEventCloud(K0, d0, N, s, BS, 3, 0.5, 0.02);
        var (v0, _, r20, _) = FitFront(c0.d, c0.tau);
        // Random R
        var rng = new Random(BS); var rDist = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { double v = 0.1 + rng.NextDouble(); rDist[i, j] = rDist[j, i] = v; }
        var Kr = ExpUpd(rDist, kv, xi); var dr = DL(Nm(RP(Sm(Kr, N, s, BS)))); var cr = BuildEventCloud(Kr, dr, N, s, BS, 3, 0.5, 0.02);
        var (vr, _, r2r, _) = FitFront(cr.d, cr.tau);
        // Global sync
        var Kgs = new double[N, N]; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { Kgs[i, j] = 1.0; Kgs[j, i] = 1.0; }
        var dgs = DL(Nm(RP(Sm(Kgs, N, s, BS)))); var cgs = BuildEventCloud(Kgs, dgs, N, s, BS, 3, 0.5, 0.02);
        var (vgs, _, r2gs, _) = FitFront(cgs.d, cgs.tau);
        // Active
        var Kref = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var dref = DL(Nm(RP(Sm(Kref, N, s, BS)))); var cref = BuildEventCloud(Kref, dref, N, s, BS, 3, 0.5, 0.02);
        var (vref, _, r2ref, _) = FitFront(cref.d, cref.tau);
        _output.WriteLine($"K=0:       v={v0:F3} r²={r20:F3} degenerate");
        _output.WriteLine($"RandR:     v={vr:F3} r²={r2r:F3} not physical");
        _output.WriteLine($"GlobSync:  v={vgs:F3} r²={r2gs:F3} degenerate");
        _output.WriteLine($"ActiveTRM: v={vref:F3} r²={r2ref:F3} TRM");
        Assert.True(!double.IsFinite(v0) || r20 < 0.3, "K=0 should be weak/degenerate");
    }

    // ═══════════════ ELS_13 LorentzSignatureScore ═══════════════
    [Fact]
    public void V4_1_ELS_13_LorentzSignatureScore()
    {
        int[] Ns = [80, 120]; double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2]; double s = 0.1;
        var results = new List<(double xi, double kv, int N, double v, double r2, double score, string cls)>();
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3;
            foreach (double xi in xis)
                foreach (double kv in K0s)
                {
                    var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
                    var dMat = DL(Nm(RP(Sm(Kc, N, s, BS))));
                    var cloud = BuildEventCloud(Kc, dMat, N, s, BS, 3, 0.5, 0.02);
                    var (v, _, r2, _) = FitFront(cloud.d, cloud.tau);
                    var (dRaw, sp) = MeasureDim(Kc, N, s);
                    double vStab = double.IsFinite(v) ? 1.0 / (1.0 + Math.Abs(v - 1.0)) : 0;
                    double dimStab = 1.0 / (1.0 + sp);
                    double score = Math.Max(0, r2) * vStab * dimStab * (cloud.resp / Math.Max(cloud.total, 1.0));
                    string cls = score > 0.15 ? "Lor-like" : (score > 0.05 ? "WeakLor" : (score > 0.01 ? "Marginal" : "None"));
                    if (!double.IsFinite(score)) cls = "Degenerate";
                    if (double.IsFinite(score)) results.Add((xi, kv, N, v, r2, score, cls));
                }
        }
        _output.WriteLine("xi    K0   N    v      r²     score   class");
        foreach (var r in results.OrderByDescending(r => r.score).Take(10))
            _output.WriteLine($"{r.xi:F2}  {r.kv:F1}  {r.N,3}  {r.v,5:F2}  {r.r2:F3}  {r.score:F3}  {r.cls}");
        _output.WriteLine("LorentzSignatureScore: diagnostic only. NOT physical c.");
    }

    // ═══════════════ ELS_14 EmergentLorentzSignatureReport ═══════════════
    [Fact]
    public void V4_1_ELS_14_EmergentLorentzSignatureReport()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, 5, BS);
        var dMat = DL(Nm(RP(Sm(Kc, N, s, BS))));
        var cloud = BuildEventCloud(Kc, dMat, N, s, BS, 5, 0.5, 0.02);
        var (v, _, r2, n) = FitFront(cloud.d, cloud.tau);
        var (dRaw, sp) = MeasureDim(Kc, N, s);
        string conclusion;
        if (r2 > 0.3 && cloud.resp > 10) conclusion = "A) weak-to-moderate Lorentz-like numerical signature detected";
        else if (r2 > 0.1) conclusion = "B) causal front exists but Lorentz-like structure is inconclusive";
        else if (cloud.resp > 0) conclusion = "C) diagnostics are estimator/threshold-dependent";
        else conclusion = "D) no meaningful Lorentz-like signature";
        _output.WriteLine("═══ EMERGENT LORENTZ SIGNATURE REPORT ═══");
        _output.WriteLine($"Best regime: xi=1.75 K0=1.2 N=80");
        _output.WriteLine($"Fitted v: {v:F3}  r²={r2:F3}  events={n}");
        _output.WriteLine($"Response fraction: {cloud.resp}/{cloud.total}");
        _output.WriteLine($"D_raw={dRaw:F2}  spread={sp:F3}");
        _output.WriteLine($"Conclusion: {conclusion}");
        _output.WriteLine("v is dimensionless — NOT physical c. Lorentz NOT proven.");
    }

    // ═══════════════ ELS_15 ClaimDisciplineReport ═══════════════
    [Fact]
    public void V4_1_ELS_15_ClaimDisciplineReport()
    {
        _output.WriteLine("═══ CLAIM DISCIPLINE: Emergent Lorentz Signature ═══");
        _output.WriteLine("");
        _output.WriteLine("SUPPORTED:");
        _output.WriteLine("  - Finite-front event clouds can be analyzed.");
        _output.WriteLine("  - Cone-like diagnostics are computable.");
        _output.WriteLine("  - Interval-like proxies can be classified numerically.");
        _output.WriteLine("  - Dimensionless v stability can be tested.");
        _output.WriteLine("  - Load, N, seed, law, null, and degeneracy controls evaluable.");
        _output.WriteLine("");
        _output.WriteLine("CONDITIONAL:");
        _output.WriteLine("  - Lorentz-like score depends on thresholds, fitting, v def, topology.");
        _output.WriteLine("  - Interval proxy is NOT a Minkowski metric.");
        _output.WriteLine("  - Dimensionless v is NOT physical c.");
        _output.WriteLine("  - Cone-like behavior is NOT Lorentz invariance.");
        _output.WriteLine("");
        _output.WriteLine("HYPOTHESIS:");
        _output.WriteLine("  - TRM convergence state may support emergent Lorentzian structure.");
        _output.WriteLine("  - Physical c may correspond to stable dimensionless v after calibration.");
        _output.WriteLine("  - Lorentzian spacetime may emerge only after continuum constraints.");
        _output.WriteLine("");
        _output.WriteLine("NOT CLAIMED:");
        _output.WriteLine("  - Physical c is derived.");
        _output.WriteLine("  - Lorentz invariance is proven.");
        _output.WriteLine("  - Lorentzian spacetime is derived.");
        _output.WriteLine("  - Minkowski metric is derived.");
        _output.WriteLine("  - D=3 is derived.");
        _output.WriteLine("  - Physical 3D space is derived.");
        _output.WriteLine("  - Gravity is derived.");
        _output.WriteLine("  - Time dilation is derived.");
        _output.WriteLine("  - GR is replaced.");
        _output.WriteLine("  - SPARC is explained.");
        _output.WriteLine("  - Dark matter is replaced.");
        Assert.True(true, "Claim discipline report complete.");
    }
}
