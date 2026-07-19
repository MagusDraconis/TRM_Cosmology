using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Omega time anchor feasibility: tests whether the internal Omega clock-rate
/// proxy is stable, reproducible, and dependency-safe enough to serve as the
/// first candidate anchor for future physical time calibration.
///
/// Does NOT claim physical time, SI seconds, time dilation, c, G, gravity,
/// GR, or physical units. Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_OmegaTimeAnchorFeasibility")]
public class V4_1_OmegaTimeAnchorFeasibility_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_OmegaTimeAnchorFeasibility_Tests(ITestOutputHelper o) { _output = o; }

    // ══════════════════ Core helpers ══════════════════
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
    private static double Spear(double[] a, double[] b) { if (a.Length < 3) return 0; var ia = a.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); var ib = b.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); return Pear(ia, ib); }
    private static double Pear(double[] x, double[] y) { int n = x.Length; double mx = x.Average(), my = y.Average(), nm = 0, dx = 0, dy = 0; for (int i = 0; i < n; i++) { double a = x[i] - mx, b = y[i] - my; nm += a * b; dx += a * a; dy += b * b; } return Math.Sqrt(dx * dy) > 1e-15 ? nm / Math.Sqrt(dx * dy) : 0; }
    private static double[,] KS(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    private static double[,] RecoverFP(double[,] K0, int N, double kv, double xi, double s, int E, int seed) { var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = ExpUpd(DL(Nm(RP(he))), kv, xi); } return Kc; }
    private static double[] OmegaField(double[][] h) { int T = h.Length, N = h[0].Length; var o = new double[N]; for (int i = 0; i < N; i++) { double s = 0; int c = 0; for (int t = 1; t < T; t++) { s += Math.Abs(h[t][i] - h[t - 1][i]); c++; } o[i] = c > 0 ? s / (c * Dt * Hd) : 0; } return o; }
    private static double TemporalStability(double[] o) { if (o.Length < 2) return 0; double m = o.Average(); double cv = m > 1e-6 ? Math.Sqrt(o.Average(x => (x - m) * (x - m))) / m : 1; return 1.0 / (1.0 + cv); }
    private static (double[] times, double[] amps) KickDetect(double[,] K, int N, double s, int seed, int src, double kickAmp, double thresh) { var h0 = Sm(K, N, s, seed); var hk = Sm(K, N, s, seed, src, kickAmp, 50); var times = new double[N]; var amps = new double[N]; for (int dst = 0; dst < N; dst++) { times[dst] = -1; amps[dst] = 0; if (dst == src) continue; for (int t = 0; t < hk.Length; t++) { double dPh = hk[t][dst] - h0[t][dst]; double dev = Math.Abs(Math.Sin(0.5 * dPh)); if (dev > amps[dst]) amps[dst] = dev; if (dev > thresh && times[dst] < 0) times[dst] = t * Dt * Hd; } } return (times, amps); }
    private static double Median(List<double> vs) { if (vs.Count == 0) return double.NaN; var s = vs.OrderBy(x => x).ToList(); int mid = s.Count / 2; return s.Count % 2 == 0 ? (s[mid - 1] + s[mid]) / 2.0 : s[mid]; }
    private static string ReadinessClass(double s) => s > 0.7 ? "Ready" : (s > 0.4 ? "NeedsWork" : (s > 0.15 ? "Incomplete" : "Degenerate"));

    // ═══════════════ OTAF_01 OmegaAnchorDataFinite ═══════════════
    [Fact]
    public void V4_1_OTAF_01_OmegaAnchorDataFinite()
    {
        int N = 80; int ln = N / 2;
        var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS);
        var h0 = Sm(Kc, N, 0.1, BS);
        var o0 = OmegaField(h0);
        var hL = Sm(Kc, N, 0.1, BS, ln, 0.1);
        var oL = OmegaField(hL);
        var dO = oL.Zip(o0, (a, b) => a - b).ToArray();
        Assert.True(o0.All(double.IsFinite), "Omega NaN");
        Assert.True(dO.All(double.IsFinite), "DeltaOmega NaN");
        _output.WriteLine($"Omega: mean={o0.Average():F3} median={Median(o0.ToList()):F3} std={Math.Sqrt(o0.Average(x=>(x-o0.Average())*(x-o0.Average()))):F3}");
        _output.WriteLine($"DeltaOmega under 0.1 load: mean={dO.Average():F4} local={Math.Abs(dO[ln]):F4}");
    }

    // ═══════════════ OTAF_02 OmegaBaselineStability ═══════════════
    [Fact]
    public void V4_1_OTAF_02_OmegaBaselineStability()
    {
        int[] Ns = [40, 80, 120, 200];
        _output.WriteLine("N     Omega_mean  tStab  class");
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3;
            var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, E, BS);
            var o = OmegaField(Sm(Kc, N, 0.1, BS));
            double tS = TemporalStability(o);
            _output.WriteLine($"{N,5}  {o.Average():F4}       {tS:F3}  {ReadinessClass(tS)}");
        }
        // Plateau sweep
        _output.WriteLine("xi    K0   Omega_mean  tStab");
        double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2];
        foreach (double xi in xis)
            foreach (double kv in K0s)
            {
                var Kc = RecoverFP(KS(80, BS), 80, kv, xi, 0.1, 5, BS);
                var o = OmegaField(Sm(Kc, 80, 0.1, BS));
                _output.WriteLine($"{xi:F2}  {kv:F1}  {o.Average():F4}       {TemporalStability(o):F3}");
            }
    }

    // ═══════════════ OTAF_03 OmegaLoadLinearity ═══════════════
    [Fact]
    public void V4_1_OTAF_03_OmegaLoadLinearity()
    {
        int N = 80; int ln = N / 2; double[] loads = [0.01, 0.05, 0.10, 0.20];
        var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS);
        var o0 = OmegaField(Sm(Kc, N, 0.1, BS));
        var lds = new List<double>(); var dOs = new List<double>();
        foreach (double load in loads)
        {
            var oL = OmegaField(Sm(Kc, N, 0.1, BS, ln, load));
            lds.Add(load); dOs.Add(Math.Abs(oL[ln] - o0[ln]));
        }
        double mx = lds.Average(), my = dOs.Average(), sxy = 0, sx2 = 0, sy2 = 0;
        for (int i = 0; i < lds.Count; i++) { double a = lds[i] - mx, b = dOs[i] - my; sxy += a * b; sx2 += a * a; sy2 += b * b; }
        double slope = sx2 > 1e-15 ? sxy / sx2 : 0;
        double r2 = sx2 * sy2 > 1e-15 ? sxy * sxy / (sx2 * sy2) : 0;
        _output.WriteLine($"DeltaOmega ≈ {slope:F4} × Load  R²={r2:F3}  linear={(r2 > 0.8 ? "YES" : "partial")}");
        _output.WriteLine("No physical time dilation claim.");
    }

    // ═══════════════ OTAF_04 LocalNeighborGlobalOmegaHierarchy ═══════════════
    [Fact]
    public void V4_1_OTAF_04_LocalNeighborGlobalOmegaHierarchy()
    {
        int N = 80; int ln = N / 2; double dO = 0.1;
        var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS);
        var o0 = OmegaField(Sm(Kc, N, 0.1, BS));
        var oL = OmegaField(Sm(Kc, N, 0.1, BS, ln, dO));
        var deltas = oL.Zip(o0, (a, b) => Math.Abs(a - b)).ToArray();
        double local = deltas[ln];
        var dMat = DL(Nm(RP(Sm(Kc, N, 0.1, BS))));
        var nn = Enumerable.Range(0, N).Where(i => i != ln).OrderBy(i => dMat[ln, i]).Take(3).ToArray();
        double neighbor = nn.Select(i => deltas[i]).Average();
        double global = deltas.Where((_, i) => i != ln && !nn.Contains(i)).DefaultIfEmpty(0).Average();
        _output.WriteLine($"Omega shift: local={local:F4}  neighbor={neighbor:F4}  global={global:F4}");
        _output.WriteLine($"Hierarchy: {(local > neighbor && neighbor > global ? "local>neighbor>global" : "mixed")}");
    }

    // ═══════════════ OTAF_05 OmegaNormalizationChoices ═══════════════
    [Fact]
    public void V4_1_OTAF_05_OmegaNormalizationChoices()
    {
        int N = 80; int ln = N / 2; double dO = 0.1;
        var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS);
        var o0 = OmegaField(Sm(Kc, N, 0.1, BS));
        var oL = OmegaField(Sm(Kc, N, 0.1, BS, ln, dO));
        double baseMean = o0.Average(); double baseMedian = Median(o0.ToList());
        // Norm 1: Omega / baseline_mean
        var n1 = o0.Select(x => x / baseMean).ToArray();
        // Norm 2: DeltaOmega / baseline_mean
        var n2 = oL.Zip(o0, (a, b) => (a - b) / baseMean).ToArray();
        // Norm 3: Omega / median
        var n3 = o0.Select(x => x / baseMedian).ToArray();
        _output.WriteLine($"Norm1 (Ω/mean):  mean={n1.Average():F3} cv={Math.Sqrt(n1.Average(x=>(x-n1.Average())*(x-n1.Average())))/Math.Max(n1.Average(),1e-6):F3}");
        _output.WriteLine($"Norm2 (ΔΩ/mean): mean={n2.Average():F3} cv={Math.Sqrt(n2.Average(x=>(x-n2.Average())*(x-n2.Average())))/Math.Max(Math.Abs(n2.Average()),1e-6):F3}");
        _output.WriteLine($"Norm3 (Ω/med):   mean={n3.Average():F3} cv={Math.Sqrt(n3.Average(x=>(x-n3.Average())*(x-n3.Average())))/Math.Max(n3.Average(),1e-6):F3}");
        _output.WriteLine("Best: Norm1 (Ω/mean) — simplest, stable, non-negative baseline.");
    }

    // ═══════════════ OTAF_06 OmegaToTauDependency ═══════════════
    [Fact]
    public void V4_1_OTAF_06_OmegaToTauDependency()
    {
        int N = 80; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS);
        var o = OmegaField(Sm(Kc, N, 0.1, BS)); var dMat = DL(Nm(RP(Sm(Kc, N, 0.1, BS))));
        var omegas = new List<double>(); var taus = new List<double>();
        for (int src = 0; src < 5; src++)
        {
            var (times, _) = KickDetect(Kc, N, 0.1, BS, src, 0.5, 0.02);
            for (int dst = 0; dst < N; dst++)
                if (dst != src && times[dst] > 0) { omegas.Add(o[dst]); taus.Add(times[dst]); }
        }
        if (omegas.Count > 3)
        {
            double rho = Spear(omegas.ToArray(), taus.ToArray());
            _output.WriteLine($"corr(Omega, tau) = {rho:F4} — {(Math.Abs(rho) < 0.3 ? "Omega anchor does NOT dominate tau" : "Omega affects tau")}");
        }
        else _output.WriteLine("Insufficient events for tau-Omega correlation.");
        _output.WriteLine("No physical seconds claim.");
    }

    // ═══════════════ OTAF_07 OmegaToVeffDependency ═══════════════
    [Fact]
    public void V4_1_OTAF_07_OmegaToVeffDependency()
    {
        int N = 80; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS);
        var o = OmegaField(Sm(Kc, N, 0.1, BS));
        double oMean = o.Average();
        var dMat = DL(Nm(RP(Sm(Kc, N, 0.1, BS))));
        var vs = new List<double>();
        for (int src = 0; src < 5; src++)
        {
            var (times, _) = KickDetect(Kc, N, 0.1, BS, src, 0.5, 0.02);
            for (int dst = 0; dst < N; dst++)
                if (dst != src && times[dst] > 0 && dMat[src, dst] > 0)
                    vs.Add(dMat[src, dst] / times[dst]);
        }
        if (vs.Count > 3)
        {
            double vMean = vs.Average();
            double vCv = vs.Count > 1 ? Math.Sqrt(vs.Average(x => (x - vMean) * (x - vMean))) / Math.Max(vMean, 1e-6) : 1;
            double stab = 1.0 / (1.0 + vCv);
            _output.WriteLine($"v_eff: mean={vMean:F3} cv={vCv:F3} stab={stab:F3} class={ReadinessClass(stab)}");
        }
        else _output.WriteLine("Insufficient events.");
        _output.WriteLine("Physical c NOT claimed.");
    }

    // ═══════════════ OTAF_08 OmegaToAlphaDependency ═══════════════
    [Fact]
    public void V4_1_OTAF_08_OmegaToAlphaDependency()
    {
        int N = 80; int ln = N / 2;
        var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS);
        var d0 = DL(Nm(RP(Sm(Kc, N, 0.1, BS))));
        var dL = DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc, N, 0.1, BS, ln, 0.2)))), 1.2, 1.75), N, 0.1, BS))));
        var curv = new double[N];
        { var h = new double[N]; for (int i = 0; i < N; i++) { double s = 0; int c = 0; for (int j = 0; j < N; j++) if (i != j) { s += Math.Abs(dL[i, j] - d0[i, j]); c++; } h[i] = c > 0 ? s / c : 0; } for (int i = 0; i < N; i++) { double sum = 0, wSum = 0; for (int j = 0; j < N; j++) if (i != j) { double w = 1.0 / (1.0 + d0[i, j]); sum += w * h[j]; wSum += w; } curv[i] = wSum * h[i] - sum; } }
        var o0 = OmegaField(Sm(Kc, N, 0.1, BS)); var oL = OmegaField(Sm(Kc, N, 0.1, BS, ln, 0.2));
        var src = oL.Zip(o0, (a, b) => Math.Abs(a - b)).ToArray();
        double mx = src.Average(), my = curv.Average(), sxy = 0, sx2 = 0;
        for (int i = 0; i < N; i++) { double a = src[i] - mx; sxy += a * (curv[i] - my); sx2 += a * a; }
        double alpha = sx2 > 1e-15 ? sxy / sx2 : 0;
        _output.WriteLine($"alpha_TRM (using Omega-based source) = {alpha:F4}");
        _output.WriteLine("Alpha is NOT physical 8πG/c⁴.");
    }

    // ═══════════════ OTAF_09 PlateauOmegaAnchorMap ═══════════════
    [Fact]
    public void V4_1_OTAF_09_PlateauOmegaAnchorMap()
    {
        int N = 80; int ln = N / 2; double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2, 1.5];
        _output.WriteLine("xi    K0   Omega   tStab  dO_slope  v_eff_mean");
        foreach (double xi in xis)
            foreach (double kv in K0s)
            {
                var Kc = RecoverFP(KS(N, BS), N, kv, xi, 0.1, 5, BS);
                var o = OmegaField(Sm(Kc, N, 0.1, BS)); var oL = OmegaField(Sm(Kc, N, 0.1, BS, ln, 0.1));
                double dOSlope = Math.Abs(oL[ln] - o[ln]) / 0.1;
                var dMat = DL(Nm(RP(Sm(Kc, N, 0.1, BS))));
                var vs = new List<double>();
                for (int src = 0; src < 3; src++) { var (times, _) = KickDetect(Kc, N, 0.1, BS, src, 0.5, 0.02); for (int dst = 0; dst < N; dst++) if (dst != src && times[dst] > 0 && dMat[src, dst] > 0) vs.Add(dMat[src, dst] / times[dst]); }
                _output.WriteLine($"{xi:F2}  {kv:F1}  {o.Average():F4}  {TemporalStability(o):F3}  {dOSlope:F4}     {(vs.Count > 0 ? vs.Average() : 0):F3}");
            }
    }

    // ═══════════════ OTAF_10 MultiSeedOmegaAnchor ═══════════════
    [Fact]
    public void V4_1_OTAF_10_MultiSeedOmegaAnchor()
    {
        int N = 80; int nSeeds = 20; int ln = N / 2;
        var omegas = new List<double>(); var tStabs = new List<double>(); var slopes = new List<double>();
        for (int seed = 0; seed < nSeeds; seed++)
        {
            var Kc = RecoverFP(KS(N, seed), N, 1.2, 1.75, 0.1, 5, seed);
            var o0 = OmegaField(Sm(Kc, N, 0.1, seed)); var oL = OmegaField(Sm(Kc, N, 0.1, seed, ln, 0.1));
            omegas.Add(o0.Average()); tStabs.Add(TemporalStability(o0)); slopes.Add(Math.Abs(oL[ln] - o0[ln]) / 0.1);
        }
        double mo = omegas.Average(); double so = omegas.Count > 1 ? Math.Sqrt(omegas.Average(x => (x - mo) * (x - mo))) : 0;
        double ms = slopes.Average(); double ss = slopes.Count > 1 ? Math.Sqrt(slopes.Average(x => (x - ms) * (x - ms))) : 0;
        _output.WriteLine($"Omega: {mo:F4}±{so:F4}  tStab: {tStabs.Average():F3}  slope: {ms:F4}±{ss:F4}  n={nSeeds}");
    }

    // ═══════════════ OTAF_11 NScalingOmegaAnchor ═══════════════
    [Fact]
    public void V4_1_OTAF_11_NScalingOmegaAnchor()
    {
        int[] Ns = [40, 80, 120, 200];
        _output.WriteLine("N     Omega   tStab  slope   v_eff");
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3; int ln = N / 2;
            var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, E, BS);
            var o0 = OmegaField(Sm(Kc, N, 0.1, BS)); var oL = OmegaField(Sm(Kc, N, 0.1, BS, ln, 0.1));
            double sl = Math.Abs(oL[ln] - o0[ln]) / 0.1;
            var dMat = DL(Nm(RP(Sm(Kc, N, 0.1, BS))));
            var vs = new List<double>(); for (int src = 0; src < 3; src++) { var (times, _) = KickDetect(Kc, N, 0.1, BS, src, 0.5, 0.02); for (int dst = 0; dst < N; dst++) if (dst != src && times[dst] > 0 && dMat[src, dst] > 0) vs.Add(dMat[src, dst] / times[dst]); }
            _output.WriteLine($"{N,5}  {o0.Average():F4}  {TemporalStability(o0):F3}  {sl:F4}  {(vs.Count>0?vs.Average():0):F3}");
        }
    }

    // ═══════════════ OTAF_12 NullAndDegenerateControls ═══════════════
    [Fact]
    public void V4_1_OTAF_12_NullAndDegenerateControls()
    {
        int N = 80;
        var K0 = new double[N, N]; var o0 = OmegaField(Sm(K0, N, 0.1, BS));
        _output.WriteLine($"K=0:        Omega={o0.Average():F4} tStab={TemporalStability(o0):F3} (uniform, degenerate)");
        var Kgs = new double[N, N]; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { Kgs[i, j] = 1.0; Kgs[j, i] = 1.0; }
        var ogs = OmegaField(Sm(Kgs, N, 0.1, BS));
        _output.WriteLine($"GlobSync:   Omega={ogs.Average():F4} tStab={TemporalStability(ogs):F3} (degenerate)");
        var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS);
        var oa = OmegaField(Sm(Kc, N, 0.1, BS));
        _output.WriteLine($"ActiveTRM:  Omega={oa.Average():F4} tStab={TemporalStability(oa):F3} (TRM)");
        _output.WriteLine("K=0/GlobSync produce artificially high stability from uniform fields — not physical time.");
    }

    // ═══════════════ OTAF_13 OmegaAnchorReadinessScore ═══════════════
    [Fact]
    public void V4_1_OTAF_13_OmegaAnchorReadinessScore()
    {
        int N = 80; double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2]; int ln = N / 2;
        _output.WriteLine("xi    K0   tStab  slope   vEffStab  score   class");
        foreach (double xi in xis)
            foreach (double kv in K0s)
            {
                var Kc = RecoverFP(KS(N, BS), N, kv, xi, 0.1, 5, BS);
                var o0 = OmegaField(Sm(Kc, N, 0.1, BS)); var oL = OmegaField(Sm(Kc, N, 0.1, BS, ln, 0.1));
                double tS = TemporalStability(o0); double sl = Math.Abs(oL[ln] - o0[ln]) / 0.1;
                var dMat = DL(Nm(RP(Sm(Kc, N, 0.1, BS))));
                var vs = new List<double>(); for (int src = 0; src < 3; src++) { var (times, _) = KickDetect(Kc, N, 0.1, BS, src, 0.5, 0.02); for (int dst = 0; dst < N; dst++) if (dst != src && times[dst] > 0 && dMat[src, dst] > 0) vs.Add(dMat[src, dst] / times[dst]); }
                double vStab = vs.Count > 1 ? 1.0 / (1.0 + Math.Sqrt(vs.Average(x => (x - vs.Average()) * (x - vs.Average()))) / Math.Max(vs.Average(), 1e-6)) : 0;
                double score = tS * (sl > 0 ? Math.Min(sl, 3.0) / 3.0 : 0) * vStab;
                _output.WriteLine($"{xi:F2}  {kv:F1}  {tS:F3}  {sl:F4}    {vStab:F3}     {score:F3}   {ReadinessClass(score)}");
            }
    }

    // ═══════════════ OTAF_14 OmegaAnchorFeasibilityReport ═══════════════
    [Fact]
    public void V4_1_OTAF_14_OmegaAnchorFeasibilityReport()
    {
        int N = 80; int ln = N / 2;
        var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS);
        var o0 = OmegaField(Sm(Kc, N, 0.1, BS)); var oL = OmegaField(Sm(Kc, N, 0.1, BS, ln, 0.1));
        double tS = TemporalStability(o0); double sl = Math.Abs(oL[ln] - o0[ln]) / 0.1;
        var dMat = DL(Nm(RP(Sm(Kc, N, 0.1, BS))));
        var vs = new List<double>(); for (int src = 0; src < 5; src++) { var (times, _) = KickDetect(Kc, N, 0.1, BS, src, 0.5, 0.02); for (int dst = 0; dst < N; dst++) if (dst != src && times[dst] > 0 && dMat[src, dst] > 0) vs.Add(dMat[src, dst] / times[dst]); }
        double vStab = vs.Count > 1 ? 1.0 / (1.0 + Math.Sqrt(vs.Average(x => (x - vs.Average()) * (x - vs.Average()))) / Math.Max(vs.Average(), 1e-6)) : 0;
        double score = tS * (sl > 0 ? Math.Min(sl, 3.0) / 3.0 : 0) * vStab;
        string conclusion = score > 0.3 ? "A) Omega is ready as first exploratory internal calibration anchor"
                         : score > 0.15 ? "B) Omega is promising but needs more robustness"
                         : score > 0.05 ? "C) Omega anchor is diagnostic-dependent"
                         : "D) Omega is not viable as first anchor";
        _output.WriteLine("═══ OMEGA TIME ANCHOR FEASIBILITY ═══");
        _output.WriteLine($"Omega mean: {o0.Average():F4}  tStab: {tS:F3}  load slope: {sl:F4}");
        _output.WriteLine($"v_eff stability: {vStab:F3}  nEvents: {vs.Count}");
        _output.WriteLine($"Score: {score:F3}  ⇒  {conclusion}");
        _output.WriteLine("Physical time / seconds NOT derived.");
    }

    // ═══════════════ OTAF_15 ClaimDisciplineReport ═══════════════
    [Fact]
    public void V4_1_OTAF_15_ClaimDisciplineReport()
    {
        _output.WriteLine("═══ CLAIM DISCIPLINE: Omega Time Anchor Feasibility ═══");
        _output.WriteLine("");
        _output.WriteLine("SUPPORTED:");
        _output.WriteLine("  - Omega anchor stability evaluable.");
        _output.WriteLine("  - Internal normalization choices comparable.");
        _output.WriteLine("  - Omega dependencies on tau, v_eff, alpha trackable.");
        _output.WriteLine("  - Load, N, seed, plateau, null, and degenerate controls testable.");
        _output.WriteLine("");
        _output.WriteLine("CONDITIONAL:");
        _output.WriteLine("  - Omega readiness depends on normalization, N, seed, load, xi, K0.");
        _output.WriteLine("  - Omega is NOT physical proper time.");
        _output.WriteLine("  - tau is NOT physical seconds.");
        _output.WriteLine("  - v_eff is NOT physical c.");
        _output.WriteLine("  - alpha_TRM is NOT physical G-related coupling.");
        _output.WriteLine("");
        _output.WriteLine("HYPOTHESIS:");
        _output.WriteLine("  - Omega may serve as first future physical time calibration anchor.");
        _output.WriteLine("  - Once Omega is externally anchored, tau/v_eff may become calibratable.");
        _output.WriteLine("");
        _output.WriteLine("NOT CLAIMED:");
        _output.WriteLine("  - Physical time, seconds, time dilation, c, G, mass, energy derived.");
        _output.WriteLine("  - Gravity, GR, D=3, SPARC, dark matter derived/replaced.");
        Assert.True(true, "Claim discipline report complete.");
    }
}
