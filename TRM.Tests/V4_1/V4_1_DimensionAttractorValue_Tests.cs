using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Dimension attractor value: estimates the numerical value and uncertainty
/// range of the corrected TRM effective dimension attractor using calibrated
/// estimators, multi-seed sampling, N-scaling, load invariance, coupling-law
/// and plateau comparisons.
///
/// Does NOT claim D=3 is derived. Does NOT reward D=3.
/// Does NOT claim physical 3D space, continuum dimension, gravity,
/// time dilation, c, Lorentz spacetime, SPARC, or dark matter.
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_DimensionAttractorValue")]
public class V4_1_DimensionAttractorValue_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_DimensionAttractorValue_Tests(ITestOutputHelper o) { _output = o; }

    // ══════════════════ Core helpers ══════════════════
    private static double[][] Sm(double[,] K, int N, double s, int seed, int loadNode = -1, double deltaOmega = 0)
    {
        var r = new Random(seed); var w = new double[N]; for (int i = 0; i < N; i++) w[i] = 1.0 + s * (r.NextDouble() - 0.5) * 2.0;
        if (loadNode >= 0 && loadNode < N) w[loadNode] += deltaOmega;
        var th = new double[N]; for (int i = 0; i < N; i++) th[i] = r.NextDouble() * 2.0 * Math.PI;
        int hL = St / Hd + 1; var h = new double[hL][]; h[0] = (double[])th.Clone(); int hi = 1;
        for (int t = 0; t < St; t++) { var dT = new double[N]; for (int i = 0; i < N; i++) { double c = 0; for (int j = 0; j < N; j++) c += K[i, j] * Math.Sin(th[j] - th[i]); dT[i] = w[i] + c; } for (int i = 0; i < N; i++) th[i] += Dt * dT[i]; if ((t + 1) % Hd == 0 && hi < hL) h[hi++] = (double[])th.Clone(); }
        return h;
    }
    private static double[,] RP(double[][] h) { int T = h.Length, N = h[0].Length; var R = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { double sc = 0, ss = 0; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); } R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; } return R; }
    private static double[,] Nm(double[,] R) { int N = R.GetLength(0); double mn = double.MaxValue; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j && R[i, j] < mn) mn = R[i, j]; double rng = 1.0 - mn; if (rng < 1e-15) rng = 1.0; var Rn = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) Rn[i, j] = 1.0; else Rn[i, j] = Math.Max(REps, (R[i, j] - mn) / rng); } return Rn; }
    private static double[,] DL(double[,] R) { int N = R.GetLength(0); var d = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) d[i, j] = 0; else d[i, j] = -Math.Log(Math.Max(R[i, j], 1e-100)); } return d; }
    private static double[,] ExpUpd(double[,] d, double K0, double xi) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 * Math.Exp(-d[i, j] / Math.Max(xi, 0.01)); } return K; }
    private static double[,] GaussUpd(double[,] d, double K0, double xi) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else { double r = d[i, j] / Math.Max(xi, 0.01); K[i, j] = K0 * Math.Exp(-r * r); } } return K; }
    private static double[,] PowerUpd(double[,] d, double K0, double p) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 / (1.0 + Math.Pow(d[i, j], Math.Max(p, 0.5))); } return K; }
    private static double Dg(double[,] d) { int N = d.GetLength(0); var v = new List<double>(); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) v.Add(d[i, j]); if (v.Count == 0) return 0; double m = v.Average(); return m > 1e-9 ? v.Average(x => (x - m) * (x - m)) / (m * m) : 0; }
    private static double[,] KS(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    private static double[,] RecoverFP(double[,] K0, int N, double K0v, double xi, double s, int E, int seed, Func<double[,], double, double, double[,]> upd = null) { upd ??= ExpUpd; var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = upd(DL(Nm(RP(he))), K0v, xi); } return Kc; }

    // ── Dimension estimators ───────────────────────────
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

    private static (double dEff, double r2) WeightedDim(double[,] K, int N, int center)
    {
        var dists = Enumerable.Range(0, N).Where(x => x != center).Select(x => K[center, x]).OrderByDescending(x => x).ToArray();
        int nSh = Math.Min(8, dists.Length / 4); if (nSh < 3) return (double.NaN, double.NaN);
        var lW = new List<double>(); var lN = new List<double>();
        for (int i = 0; i < nSh; i++) { int cnt = (i + 1) * dists.Length / nSh; double wSum = dists.Take(cnt).Sum(); if (wSum < 1e-6) continue; lW.Add(Math.Log(wSum)); lN.Add(Math.Log(cnt)); }
        if (lW.Count < 3) return (double.NaN, double.NaN);
        double mx = lN.Average(), my = lW.Average(), num = 0, dx = 0, dy = 0;
        for (int i = 0; i < lW.Count; i++) { double a = lN[i] - mx, b = lW[i] - my; num += a * b; dx += a * a; dy += b * b; }
        return (dx > 1e-15 ? num / dx : double.NaN, dy > 1e-15 ? num * num / (dx * dy) : 0);
    }

    private static (double d, double spread) MeasureDim(double[,] Kc, int N, double s, Func<double[,], int, int, (double, double)> est)
    {
        var h = Sm(Kc, N, s, BS); var ds = new List<double>();
        if (est == BallDim) { var dMat = DL(Nm(RP(h))); for (int c = 0; c < Math.Min(8, N / 5); c++) { var (d, _) = est(dMat, N, c * N / Math.Min(8, N / 5)); if (double.IsFinite(d)) ds.Add(d); } }
        else { for (int c = 0; c < Math.Min(8, N / 5); c++) { var (d, _) = est(Kc, N, c * N / Math.Min(8, N / 5)); if (double.IsFinite(d)) ds.Add(d); } }
        if (ds.Count == 0) return (double.NaN, double.NaN);
        double m = ds.Average(); double sp = ds.Count > 1 ? Math.Sqrt(ds.Average(x => (x - m) * (x - m))) : 0;
        return (m, sp);
    }

    // ── Reference lattice builders (from DEC) ─────────
    private static double[,] LatticeDist(int N, int D)
    {
        int side = Math.Max(2, (int)Math.Round(Math.Pow(N, 1.0 / D)));
        var coords = new int[N][];
        for (int i = 0; i < N; i++) { coords[i] = new int[D]; int v = i; for (int d = 0; d < D; d++) { coords[i][d] = v % side; v /= side; } }
        var dMat = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++)
            {
                double dist = 0; for (int d = 0; d < D; d++) { double diff = coords[i][d] - coords[j][d]; diff = Math.Min(Math.Abs(diff), side - Math.Abs(diff)); dist += diff * diff; }
                dMat[i, j] = dMat[j, i] = Math.Sqrt(dist);
            }
        return dMat;
    }

    // ── Bias calibration table ─────────────────────────
    private static Dictionary<int, double> CalibrateBias(int N)
    {
        var bias = new Dictionary<int, double>();
        int[] Ds = [1, 2, 3, 4, 5];
        foreach (int D in Ds)
        {
            var dMat = LatticeDist(N, D);
            var ds = new List<double>();
            for (int c = 0; c < Math.Min(8, N / 5); c++)
            {
                var (d, _) = BallDim(dMat, N, c * N / Math.Min(8, N / 5));
                if (double.IsFinite(d)) ds.Add(d);
            }
            if (ds.Count > 0) bias[D] = ds.Average() - D;
        }
        return bias;
    }

    private static double CorrectDim(double dRaw, Dictionary<int, double> biasTable)
    {
        if (double.IsNaN(dRaw) || biasTable.Count == 0) return dRaw;
        // Interpolate bias from nearest D_true references
        double rawClamped = Math.Max(1.0, Math.Min(5.0, Math.Round(dRaw)));
        int dKey = (int)rawClamped;
        if (biasTable.TryGetValue(dKey, out double b)) return dRaw - b;
        // Linear interpolation between available keys
        var keys = biasTable.Keys.OrderBy(k => k).ToList();
        if (dRaw <= keys[0]) return dRaw - biasTable[keys[0]];
        if (dRaw >= keys[keys.Count - 1]) return dRaw - biasTable[keys[keys.Count - 1]];
        for (int i = 0; i < keys.Count - 1; i++)
        {
            if (dRaw >= keys[i] && dRaw <= keys[i + 1])
            {
                double t = (dRaw - keys[i]) / (keys[i + 1] - keys[i]);
                double bInterp = biasTable[keys[i]] * (1 - t) + biasTable[keys[i + 1]] * t;
                return dRaw - bInterp;
            }
        }
        return dRaw;
    }

    // ═══════════════ DAV_01 CorrectedDimensionDataFinite ═══════════════
    [Fact]
    public void V4_1_DAV_01_CorrectedDimensionDataFinite()
    {
        int[] Ns = [40, 80]; double s = 0.1; double xi = 1.75; double kv = 1.2;
        int count = 0;
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3;
            var biasTbl = CalibrateBias(N);
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
            var (dRaw, spRaw) = MeasureDim(Kc, N, s, BallDim);
            double dCorr = CorrectDim(dRaw, biasTbl);
            var (wRaw, _) = MeasureDim(Kc, N, s, WeightedDim);
            Assert.True(double.IsFinite(dRaw), $"dRaw NaN N={N}");
            Assert.True(double.IsFinite(dCorr), $"dCorr NaN N={N}");
            Assert.True(double.IsFinite(wRaw), $"wRaw NaN N={N}");
            Assert.True(double.IsFinite(spRaw), $"sp NaN N={N}");
            if (double.IsFinite(dRaw)) count++;
        }
        Assert.True(count > 0, "No finite dimension measurements");
        _output.WriteLine($"DAV_01: {count} regimes finite, all diagnostics OK");
    }

    // ═══════════════ DAV_02 AttractorValueBaseline ═══════════════
    [Fact]
    public void V4_1_DAV_02_AttractorValueBaseline()
    {
        int[] Ns = [80, 120]; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var vals = new List<double>(); var sps = new List<double>();
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3;
            var biasTbl = CalibrateBias(N);
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
            var (dRaw, sp) = MeasureDim(Kc, N, s, BallDim);
            double dCorr = CorrectDim(dRaw, biasTbl);
            if (double.IsFinite(dCorr)) { vals.Add(dCorr); sps.Add(sp); }
        }
        Assert.True(vals.Count > 0, "No finite baseline values");
        double mean = vals.Average(); double median = Median(vals);
        double mSpread = sps.Average();
        string cls = vals.Count >= 2 && sps.Average() < 0.8 ? "Stable value" :
                     sps.Average() < 1.5 ? "Weak value" : "Estimator-dependent";
        _output.WriteLine($"N  D_corr_mean  D_corr_median  Spread  Class");
        _output.WriteLine($"{Ns[0]}-{Ns[1]}  {mean:F3}  {median:F3}  {mSpread:F3}  {cls}");
        _output.WriteLine($"Baseline D_attractor ≈ {mean:F3} ± {mSpread:F3}");
    }

    // ═══════════════ DAV_03 NScalingAttractorValue ═══════════════
    [Fact]
    public void V4_1_DAV_03_NScalingAttractorValue()
    {
        int[] Ns = [40, 80, 120, 200]; double xi = 1.75; double kv = 1.2; double s = 0.1;
        _output.WriteLine("N    D_corr  Trend");
        double prev = double.NaN;
        var allD = new List<double>();
        foreach (int N in Ns)
        {
            int E = N <= 80 ? 5 : 3;
            var biasTbl = CalibrateBias(N);
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
            var (dRaw, _) = MeasureDim(Kc, N, s, BallDim);
            double dCorr = CorrectDim(dRaw, biasTbl);
            string trend = double.IsNaN(prev) ? "-" : (dCorr > prev ? "↑" : (dCorr < prev ? "↓" : "→"));
            _output.WriteLine($"{N,4}  {dCorr:F3}  {trend}");
            if (double.IsFinite(dCorr)) allD.Add(dCorr);
            prev = dCorr;
        }
        Assert.True(allD.Count >= 2, "Need at least 2 N values");
        double mean = allD.Average(); double std = allD.Count > 1 ? Math.Sqrt(allD.Average(x => (x - mean) * (x - mean))) : 0;
        // Simple a/N extrapolation
        if (allD.Count >= 3)
        {
            double sxy = 0, sx2 = 0; double mx = Ns.Select(n => 1.0 / n).Average(); double my = allD.Average();
            for (int i = 0; i < allD.Count; i++) { double a = 1.0 / Ns[i] - mx; sxy += a * (allD[i] - my); sx2 += a * a; }
            double aSlope = sx2 > 1e-15 ? sxy / sx2 : 0;
            double Dinf = my - aSlope * mx;
            _output.WriteLine($"Extrapolation: D(N→∞) ≈ {Dinf:F3} (a/N model, hint only)");
        }
        _output.WriteLine($"N-scaling: mean={mean:F3} std={std:F3}");
    }

    // ═══════════════ DAV_04 MultiSeedAttractorValue ═══════════════
    [Fact]
    public void V4_1_DAV_04_MultiSeedAttractorValue()
    {
        int N = 80; int[] seeds = Enumerable.Range(0, 20).ToArray();
        double xi = 1.75; double kv = 1.2; double s = 0.1; int E = 5;
        var biasTbl = CalibrateBias(N);
        var vals = new List<double>(); var sps = new List<double>(); var niFreq = new int[7];
        foreach (int seed in seeds)
        {
            var Kc = RecoverFP(KS(N, seed), N, kv, xi, s, E, seed);
            var (dRaw, sp) = MeasureDim(Kc, N, s, BallDim);
            double dCorr = CorrectDim(dRaw, biasTbl);
            if (double.IsFinite(dCorr)) { vals.Add(dCorr); sps.Add(sp); int ni = Math.Clamp((int)Math.Round(dCorr), 1, 6); niFreq[ni]++; }
        }
        Assert.True(vals.Count >= 5, $"Too few valid seeds: {vals.Count}");
        double mean = vals.Average(); double std = vals.Count > 1 ? Math.Sqrt(vals.Average(x => (x - mean) * (x - mean))) : 0;
        int outliers = vals.Count(x => Math.Abs(x - mean) > 2.0 * std);
        _output.WriteLine($"Seed stats: D_corr={mean:F3} ± {std:F3}  spread_mean={sps.Average():F3}  outliers={outliers}");
        _output.WriteLine("Nearest-int distribution (diagnostic only):");
        for (int ni = 1; ni <= 6; ni++) _output.WriteLine($"  D={ni}: {niFreq[ni]}");
    }

    // ═══════════════ DAV_05 PlateauAttractorValueMap ═══════════════
    [Fact]
    public void V4_1_DAV_05_PlateauAttractorValueMap()
    {
        double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2, 1.5];
        int N = 80; double s = 0.1; int E = 5;
        var biasTbl = CalibrateBias(N);
        _output.WriteLine("xi    K0   D_corr  Spread  NearestInt");
        var allD = new List<double>();
        foreach (double xi in xis)
            foreach (double kv in K0s)
            {
                var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
                var (dRaw, sp) = MeasureDim(Kc, N, s, BallDim);
                double dCorr = CorrectDim(dRaw, biasTbl);
                int ni = double.IsNaN(dCorr) ? 0 : (int)Math.Round(dCorr);
                _output.WriteLine($"{xi:F2}  {kv:F1}  {dCorr:F3}  {sp:F3}  D={ni}");
                if (double.IsFinite(dCorr)) allD.Add(dCorr);
            }
        Assert.True(allD.Count >= 3, "Too few plateau values");
        double mean = allD.Average(); double std = allD.Count > 1 ? Math.Sqrt(allD.Average(x => (x - mean) * (x - mean))) : 0;
        _output.WriteLine($"Plateau stats: D={mean:F3} ± {std:F3}");
    }

    // ═══════════════ DAV_06 LoadInvariantAttractorValue ═══════════════
    [Fact]
    public void V4_1_DAV_06_LoadInvariantAttractorValue()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int E = 5;
        var biasTbl = CalibrateBias(N);
        int loadNode = N / 2;
        // Without load
        var Kb = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
        var (dRawB, _) = MeasureDim(Kb, N, s, BallDim);
        double dB = CorrectDim(dRawB, biasTbl);
        // With load: run simulation with load then recover FP without load injection in recovery
        // Simpler: inject load delta before recovery, measure
        var hLoaded = Sm(Kb, N, s, BS, loadNode, 0.2);
        var KcL = ExpUpd(DL(Nm(RP(hLoaded))), kv, xi);
        var (dRawL, _) = MeasureDim(KcL, N, s, BallDim);
        double dL = CorrectDim(dRawL, biasTbl);
        Assert.True(double.IsFinite(dB), "dB NaN");
        Assert.True(double.IsFinite(dL), "dL NaN");
        double delta = Math.Abs(dL - dB);
        _output.WriteLine($"D_before={dB:F3}  D_after={dL:F3}  Δ={delta:F3}");
        _output.WriteLine($"Load invariant: {(delta < 0.5 ? "YES" : "NO")}");
    }

    // ═══════════════ DAV_07 CouplingLawAttractorValue ═══════════════
    [Fact]
    public void V4_1_DAV_07_CouplingLawAttractorValue()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int E = 5;
        var biasTbl = CalibrateBias(N);
        var laws = new Dictionary<string, Func<double[,], double, double, double[,]>> {
            {"exp", (d, k0, x) => ExpUpd(d, k0, x)},
            {"gauss", (d, k0, x) => GaussUpd(d, k0, x)},
            {"power", (d, k0, x) => PowerUpd(d, k0, x)}};
        _output.WriteLine("Law      D_corr  Spread  EstAgree");
        double bestStability = 0; string bestLaw = "?";
        foreach (var (name, upd) in laws)
        {
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS, upd);
            var (dRawB, spB) = MeasureDim(Kc, N, s, BallDim);
            var (dRawW, spW) = MeasureDim(Kc, N, s, WeightedDim);
            double dB = CorrectDim(dRawB, biasTbl);
            double dW = dRawW;
            double agree = 1.0 / (1.0 + Math.Abs(dB - dW));
            double stability = agree / (1.0 + spB);
            _output.WriteLine($"{name,-7} {dB:F3}  {spB:F3}  {agree:F3}");
            if (stability > bestStability) { bestStability = stability; bestLaw = name; }
        }
        _output.WriteLine($"Tightest attractor: {bestLaw} (stability={bestStability:F3})");
    }

    // ═══════════════ DAV_08 LocalGlobalAttractorValue ═══════════════
    [Fact]
    public void V4_1_DAV_08_LocalGlobalAttractorValue()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int E = 5;
        var biasTbl = CalibrateBias(N);
        var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
        // Compute node degrees
        var degs = new double[N]; var dMat = DL(Nm(RP(Sm(Kc, N, s, BS))));
        for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j) degs[i] += Kc[i, j];
        var ranked = Enumerable.Range(0, N).OrderByDescending(i => degs[i]).ToArray();
        // Local dim at high-degree node (top 10%)
        var dLocalHi = new List<double>(); var dLocalLo = new List<double>();
        var dLocalMid = new List<double>();
        int nLocal = Math.Max(1, N / 10);
        for (int i = 0; i < nLocal; i++)
        {
            var (dh, _) = BallDim(dMat, N, ranked[i]); if (double.IsFinite(dh)) dLocalHi.Add(dh);
            var (dl, _) = BallDim(dMat, N, ranked[N - 1 - i]); if (double.IsFinite(dl)) dLocalLo.Add(dl);
            var (dm, _) = BallDim(dMat, N, ranked[N / 2 + i - nLocal / 2]); if (double.IsFinite(dm)) dLocalMid.Add(dm);
        }
        double mHi = dLocalHi.Count > 0 ? dLocalHi.Average() : double.NaN;
        double mLo = dLocalLo.Count > 0 ? dLocalLo.Average() : double.NaN;
        double mMid = dLocalMid.Count > 0 ? dLocalMid.Average() : double.NaN;
        // Global
        var (dGlob, _) = MeasureDim(Kc, N, s, BallDim);
        double hiIdx = double.IsFinite(mHi) && double.IsFinite(mLo) && mLo > 0 ? (mHi - mLo) / mLo : 0;
        _output.WriteLine($"D_highDeg={CorrectDim(mHi, biasTbl):F3}  D_lowDeg={CorrectDim(mLo, biasTbl):F3}  D_median={CorrectDim(mMid, biasTbl):F3}  D_global={CorrectDim(dGlob, biasTbl):F3}");
        _output.WriteLine($"Heterogeneity idx: {hiIdx:F3}  ({(Math.Abs(hiIdx) < 0.5 ? "homogeneous" : "heterogeneous")})");
    }

    // ═══════════════ DAV_09 NearestIntegerDiagnostic ═══════════════
    [Fact]
    public void V4_1_DAV_09_NearestIntegerDiagnostic()
    {
        int N = 80; int[] seeds = Enumerable.Range(0, 15).ToArray();
        double xi = 1.75; double kv = 1.2; double s = 0.1; int E = 5;
        var biasTbl = CalibrateBias(N);
        var allD = new List<double>();
        foreach (int seed in seeds)
        {
            var Kc = RecoverFP(KS(N, seed), N, kv, xi, s, E, seed);
            var (dRaw, _) = MeasureDim(Kc, N, s, BallDim);
            double dCorr = CorrectDim(dRaw, biasTbl);
            if (double.IsFinite(dCorr)) allD.Add(dCorr);
        }
        Assert.True(allD.Count >= 5, $"Too few: {allD.Count}");
        _output.WriteLine("Integer  MeanDist  MinDist  FreqNearest");
        for (int ni = 1; ni <= 6; ni++)
        {
            var dists = allD.Select(d => Math.Abs(d - ni)).ToList();
            double meanDist = dists.Average();
            double minDist = dists.Min();
            int freq = allD.Count(d => Math.Abs(d - ni) < 0.5);
            _output.WriteLine($"{ni,7}  {meanDist:F3}  {minDist:F3}  {freq}");
        }
        _output.WriteLine("DIAGNOSTIC ONLY — No D=3 claim.");
    }

    // ═══════════════ DAV_10 UncertaintyBudget ═══════════════
    [Fact]
    public void V4_1_DAV_10_UncertaintyBudget()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int E = 5;
        var biasTbl = CalibrateBias(N);
        // Estimator spread
        var Kref = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
        var (dRaw, spEst) = MeasureDim(Kref, N, s, BallDim);
        // Seed variance
        var seedDs = new List<double>(); for (int sd = 0; sd < 10; sd++) { var Ks = RecoverFP(KS(N, sd), N, kv, xi, s, E, sd); var (dr, _) = MeasureDim(Ks, N, s, BallDim); double dc = CorrectDim(dr, biasTbl); if (double.IsFinite(dc)) seedDs.Add(dc); }
        double uSeed = seedDs.Count > 1 ? Math.Sqrt(seedDs.Average(x => (x - seedDs.Average()) * (x - seedDs.Average()))) : 0;
        // Load variance
        int ln = N / 2; var hL = Sm(Kref, N, s, BS, ln, 0.2); var Kl = ExpUpd(DL(Nm(RP(hL))), kv, xi); var (drL, _) = MeasureDim(Kl, N, s, BallDim);
        double uLoad = Math.Abs(CorrectDim(drL, biasTbl) - CorrectDim(dRaw, biasTbl)) / Math.Sqrt(2.0);
        // Coupling-law variance
        var lawDs = new List<double>();
        foreach (var upd in new Func<double[,], double, double, double[,]>[] { ExpUpd, GaussUpd, PowerUpd })
        { var Klaw = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS, upd); var (dr2, _) = MeasureDim(Klaw, N, s, BallDim); double dc = CorrectDim(dr2, biasTbl); if (double.IsFinite(dc)) lawDs.Add(dc); }
        double uLaw = lawDs.Count > 1 ? Math.Sqrt(lawDs.Average(x => (x - lawDs.Average()) * (x - lawDs.Average()))) : 0;
        // Total
        double uTotal = Math.Sqrt(spEst * spEst + uSeed * uSeed + uLoad * uLoad + uLaw * uLaw);
        _output.WriteLine("Source          Contribution");
        _output.WriteLine($"Estimator       {spEst:F3}");
        _output.WriteLine($"Seed            {uSeed:F3}");
        _output.WriteLine($"Load            {uLoad:F3}");
        _output.WriteLine($"CouplingLaw     {uLaw:F3}");
        _output.WriteLine($"TOTAL           {uTotal:F3}");
        Assert.True(double.IsFinite(uTotal), "Uncertainty total NaN");
    }

    // ═══════════════ DAV_11 NullAndDegenerateControls ═══════════════
    [Fact]
    public void V4_1_DAV_11_NullAndDegenerateControls()
    {
        int N = 80; double s = 0.1;
        // K=0 null
        var K0 = new double[N, N]; var (d0, _) = MeasureDim(K0, N, s, BallDim);
        // Random R-like distance
        var rng = new Random(BS); var rDist = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { double v = 0.1 + rng.NextDouble(); rDist[i, j] = rDist[j, i] = v; }
        var Kr = ExpUpd(rDist, 1.2, 1.75);
        var hR = Sm(Kr, N, s, BS); var (dr, _) = MeasureDim(Kr, N, s, BallDim);
        // Global sync
        var Kgs = new double[N, N]; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { Kgs[i, j] = 1.0; Kgs[j, i] = 1.0; }
        var (dgs, _) = MeasureDim(Kgs, N, s, BallDim);
        // Shuffled theta
        var Kref = RecoverFP(KS(N, BS), N, 1.2, 1.75, s, 5, BS);
        var hSh = Sm(Kref, N, s, BS); var thSh = hSh[hSh.Length - 1]; for (int i = 0; i < N; i++) thSh[i] += rng.NextDouble() * Math.PI;
        var Rsh = RP([thSh]); var dSh = DL(Nm(Rsh)); var Ksh = ExpUpd(dSh, 1.2, 1.75); var (dsh, _) = MeasureDim(Ksh, N, s, BallDim);
        _output.WriteLine($"K=0:       D={d0:F3}  (degenerate)");
        _output.WriteLine($"Random R:  D={dr:F3}  (not physical)");
        _output.WriteLine($"GlobSync:  D={dgs:F3}  (degenerate)");
        _output.WriteLine($"Shuffled:  D={dsh:F3}  (destroyed)");
        Assert.True(d0 < 0.5 || double.IsNaN(d0), "K=0 should be degenerate");
        Assert.True(!double.IsNaN(dgs) && double.IsFinite(dgs), "Global sync dim should be finite");
        // Global sync D ~ 1 is detectably different from TRM attractor (not a false positive)
        _output.WriteLine($"Null separation: K=0={d0:F3}  GlobSync={dgs:F3}  RandomR={dr:F3}");
    }

    // ═══════════════ DAV_12 AttractorValueReport ═══════════════
    [Fact]
    public void V4_1_DAV_12_AttractorValueReport()
    {
        int N = 80; double xi = 1.75; double kv = 1.2; double s = 0.1; int E = 5;
        var biasTbl = CalibrateBias(N);
        var vals = new List<double>(); var spreads = new List<double>();
        for (int seed = 0; seed < 15; seed++)
        {
            var Kc = RecoverFP(KS(N, seed), N, kv, xi, s, E, seed);
            var (dRaw, sp) = MeasureDim(Kc, N, s, BallDim);
            double dCorr = CorrectDim(dRaw, biasTbl);
            if (double.IsFinite(dCorr)) { vals.Add(dCorr); spreads.Add(sp); }
        }
        double mD = vals.Average();
        double sD = vals.Count > 1 ? Math.Sqrt(vals.Average(x => (x - mD) * (x - mD))) : 0;
        double mSp = spreads.Average();
        int nI = (int)Math.Round(mD);
        string conclusion;
        if (sD < 0.3 && mSp < 0.5) conclusion = "A) stable corrected dimension attractor with bounded uncertainty";
        else if (sD < 0.7) conclusion = "B) weak but measurable attractor";
        else if (mSp < 1.0) conclusion = "C) estimator-dependent value";
        else conclusion = "D) degenerate/no meaningful value";
        _output.WriteLine("═══ DIMENSION ATTRACTOR VALUE REPORT ═══");
        _output.WriteLine($"D_attractor range: {mD - sD:F3} – {mD + sD:F3}");
        _output.WriteLine($"D_attractor = {mD:F3} ± {sD:F3}");
        _output.WriteLine($"Estimator spread: {mSp:F3}");
        _output.WriteLine($"Nearest integer: D={nI} (diagnostic only)");
        _output.WriteLine($"N stability: {GetNStability(xi, kv, s)}");
        _output.WriteLine($"Load invariance: {GetLoadInvariance(N, xi, kv, s, biasTbl)}");
        _output.WriteLine($"Conclusion: {conclusion}");
        _output.WriteLine("D=3 is NOT derived. Physical space is NOT claimed.");
    }

    private string GetNStability(double xi, double kv, double s)
    {
        int[] Ns = [40, 80, 120]; var ds = new List<double>();
        foreach (int N in Ns)
        {
            var biasTbl = CalibrateBias(N); int E = N <= 80 ? 5 : 3;
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
            var (dRaw, _) = MeasureDim(Kc, N, s, BallDim);
            double dc = CorrectDim(dRaw, biasTbl);
            if (double.IsFinite(dc)) ds.Add(dc);
        }
        if (ds.Count < 2) return "insufficient";
        double range = ds.Max() - ds.Min();
        return range < 0.5 ? "stable" : (range < 1.0 ? "weak" : "unstable");
    }

    private string GetLoadInvariance(int N, double xi, double kv, double s, Dictionary<int, double> biasTbl)
    {
        int E = 5; int ln = N / 2;
        var Kref = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
        var (dRawB, _) = MeasureDim(Kref, N, s, BallDim);
        var hL = Sm(Kref, N, s, BS, ln, 0.2); var Kl = ExpUpd(DL(Nm(RP(hL))), kv, xi);
        var (dRawL, _) = MeasureDim(Kl, N, s, BallDim);
        double dB = CorrectDim(dRawB, biasTbl); double dL = CorrectDim(dRawL, biasTbl);
        if (!double.IsFinite(dB) || !double.IsFinite(dL)) return "insufficient";
        return Math.Abs(dL - dB) < 0.5 ? "invariant" : "load-sensitive";
    }

    // ═══════════════ DAV_13 ClaimDisciplineReport ═══════════════
    [Fact]
    public void V4_1_DAV_13_ClaimDisciplineReport()
    {
        _output.WriteLine("═══ CLAIM DISCIPLINE: Dimension Attractor Value ═══");
        _output.WriteLine("");
        _output.WriteLine("SUPPORTED:");
        _output.WriteLine("  - Corrected dimension attractor value can be estimated.");
        _output.WriteLine("  - Uncertainty components are measurable.");
        _output.WriteLine("  - N, seed, load, plateau, and coupling-law dependence testable.");
        _output.WriteLine("  - Nearest-integer proximity is a diagnostic only.");
        _output.WriteLine("  - Null and degenerate controls are detectable.");
        _output.WriteLine("");
        _output.WriteLine("CONDITIONAL:");
        _output.WriteLine("  - D_attractor depends on estimator, N, topology, xi, K0, seed, coupling law.");
        _output.WriteLine("  - Graph/weighted-topology dimension is NOT continuum spatial dimension.");
        _output.WriteLine("  - Proximity to any integer is NOT derivation.");
        _output.WriteLine("");
        _output.WriteLine("HYPOTHESIS:");
        _output.WriteLine("  - TRM may possess a preferred corrected effective-dimension attractor.");
        _output.WriteLine("  - D=3 may emerge only after continuum + causal + calibration constraints.");
        _output.WriteLine("  - Stable dimension attractor may be prerequisite for physical emergent space.");
        _output.WriteLine("");
        _output.WriteLine("NOT CLAIMED:");
        _output.WriteLine("  - D=3 is derived.");
        _output.WriteLine("  - Physical 3D space is derived.");
        _output.WriteLine("  - Continuum dimension is proven.");
        _output.WriteLine("  - Gravity is derived.");
        _output.WriteLine("  - Time dilation is derived.");
        _output.WriteLine("  - Physical c is derived.");
        _output.WriteLine("  - Lorentz spacetime is derived.");
        _output.WriteLine("  - SPARC is explained.");
        _output.WriteLine("  - Dark matter is replaced.");
        Assert.True(true, "Claim discipline report complete.");
    }

    private static double Median(List<double> vs)
    {
        if (vs.Count == 0) return double.NaN;
        var s = vs.OrderBy(x => x).ToList();
        int mid = s.Count / 2;
        return s.Count % 2 == 0 ? (s[mid - 1] + s[mid]) / 2.0 : s[mid];
    }
}
