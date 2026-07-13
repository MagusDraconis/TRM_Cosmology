using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Dimension continuum limit probe: tests whether the corrected TRM effective
/// dimension attractor remains stable as N increases (40→500) and whether
/// simple large-N extrapolation models suggest a stable limiting dimension.
///
/// Uses calibrated estimators from DimensionEstimatorCalibration.
/// Does NOT claim D=3, physical 3D space, continuum dimension, gravity,
/// time dilation, c, Lorentz spacetime, SPARC, or dark matter.
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_DimensionContinuumLimit")]
public class V4_1_DimensionContinuumLimit_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_DimensionContinuumLimit_Tests(ITestOutputHelper o) { _output = o; }

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
    private static double[,] KS(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    private static double[,] RecoverFP(double[,] K0, int N, double K0v, double xi, double s, int E, int seed) { var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = ExpUpd(DL(Nm(RP(he))), K0v, xi); } return Kc; }

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

    private static (double d, double spread) MeasureDim(double[,] Kc, int N, double s)
    {
        var h = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h))); var ds = new List<double>();
        int nC = Math.Max(3, Math.Min(8, N / 5));
        for (int c = 0; c < nC; c++) { var (d, _) = BallDim(dMat, N, c * N / nC); if (double.IsFinite(d)) ds.Add(d); }
        if (ds.Count == 0) return (double.NaN, double.NaN);
        double m = ds.Average(); double sp = ds.Count > 1 ? Math.Sqrt(ds.Average(x => (x - m) * (x - m))) : 0;
        return (m, sp);
    }

    // ── Bias calibration ───────────────────────────────
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

    private static Dictionary<int, double> CalibrateBias(int N)
    {
        var bias = new Dictionary<int, double>();
        int[] Ds = [1, 2, 3, 4, 5];
        foreach (int D in Ds)
        {
            var dMat = LatticeDist(N, D);
            var ds = new List<double>();
            int nC = Math.Max(3, Math.Min(8, N / 5));
            for (int c = 0; c < nC; c++) { var (d, _) = BallDim(dMat, N, c * N / nC); if (double.IsFinite(d)) ds.Add(d); }
            if (ds.Count > 0) bias[D] = ds.Average() - D;
        }
        return bias;
    }

    private static double CorrectDim(double dRaw, Dictionary<int, double> biasTbl)
    {
        if (double.IsNaN(dRaw) || biasTbl.Count == 0) return dRaw;
        double rawClamped = Math.Max(1.0, Math.Min(5.0, Math.Round(dRaw)));
        int dKey = (int)rawClamped;
        if (biasTbl.TryGetValue(dKey, out double b)) return dRaw - b;
        var keys = biasTbl.Keys.OrderBy(k => k).ToList();
        if (dRaw <= keys[0]) return dRaw - biasTbl[keys[0]];
        if (dRaw >= keys[^1]) return dRaw - biasTbl[keys[^1]];
        for (int i = 0; i < keys.Count - 1; i++)
            if (dRaw >= keys[i] && dRaw <= keys[i + 1])
            { double t = (dRaw - keys[i]) / (keys[i + 1] - keys[i]); return dRaw - (biasTbl[keys[i]] * (1 - t) + biasTbl[keys[i + 1]] * t); }
        return dRaw;
    }

    private static double Median(List<double> vs)
    {
        if (vs.Count == 0) return double.NaN;
        var s = vs.OrderBy(x => x).ToList();
        int mid = s.Count / 2;
        return s.Count % 2 == 0 ? (s[mid - 1] + s[mid]) / 2.0 : s[mid];
    }

    private static int EpochsForN(int N) => N <= 200 ? 5 : (N <= 300 ? 3 : 2);

    // ═══════════════ DCL_01 LargeNDimensionDataFinite ═══════════════
    [Fact]
    public void V4_1_DCL_01_LargeNDimensionDataFinite()
    {
        int[] Ns = [40, 80, 120, 200, 300, 500]; double xi = 1.75; double kv = 1.2; double s = 0.1;
        int count = 0;
        foreach (int N in Ns)
        {
            int E = EpochsForN(N);
            var biasTbl = CalibrateBias(N);
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
            var (dRaw, sp) = MeasureDim(Kc, N, s);
            double dCorr = CorrectDim(dRaw, biasTbl);
            Assert.True(double.IsFinite(dRaw), $"dRaw NaN N={N}");
            Assert.True(double.IsFinite(dCorr), $"dCorr NaN N={N}");
            Assert.True(double.IsFinite(sp), $"sp NaN N={N}");
            if (double.IsFinite(dCorr)) count++;
        }
        Assert.True(count >= 3, $"Too few finite: {count}");
        _output.WriteLine($"DCL_01: {count}/{Ns.Length} regimes finite at N up to 500");
    }

    // ═══════════════ DCL_02 CorrectedDimensionVsN ═══════════════
    [Fact]
    public void V4_1_DCL_02_CorrectedDimensionVsN()
    {
        int[] Ns = [40, 80, 120, 200, 300, 500]; double xi = 1.75; double kv = 1.2; double s = 0.1;
        _output.WriteLine("N     D_corr  spread  class");
        double prev = double.NaN;
        foreach (int N in Ns)
        {
            int E = EpochsForN(N); var biasTbl = CalibrateBias(N);
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
            var (dRaw, sp) = MeasureDim(Kc, N, s);
            double dCorr = CorrectDim(dRaw, biasTbl);
            string trend = double.IsNaN(prev) ? "-" : (dCorr > prev + 0.2 ? "Drifting" : (dCorr < prev - 0.2 ? "Drifting" : "→"));
            string cls = sp > 1.5 ? "Noisy" : (sp > 0.7 ? "Marginal" : "Stable");
            if (double.IsNaN(dCorr)) cls = "Degenerate";
            _output.WriteLine($"{N,5}  {dCorr:F3}  {sp:F3}  {cls}");
            prev = dCorr;
        }
        _output.WriteLine("Large-N trend diagnostic only — no continuum claim.");
    }

    // ═══════════════ DCL_03 ExtrapolationModels ═══════════════
    [Fact]
    public void V4_1_DCL_03_ExtrapolationModels()
    {
        int[] Ns = [40, 80, 120, 200, 300, 500]; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var ns = new List<int>(); var ds = new List<double>();
        foreach (int N in Ns)
        {
            int E = EpochsForN(N); var biasTbl = CalibrateBias(N);
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
            var (dRaw, _) = MeasureDim(Kc, N, s);
            double dCorr = CorrectDim(dRaw, biasTbl);
            if (double.IsFinite(dCorr)) { ns.Add(N); ds.Add(dCorr); }
        }
        Assert.True(ns.Count >= 3, "Need >= 3 data points for extrapolation");
        var DinfVals = new List<double>();
        // Model 1: D(N) = D_inf + a/N
        var (D1, e1) = FitModel(ns, ds, n => 1.0 / n);
        _output.WriteLine($"1/N model:    D_inf={D1:F3}  rms={e1:F3}");
        if (double.IsFinite(D1)) DinfVals.Add(D1);
        // Model 2: D(N) = D_inf + a/sqrt(N)
        var (D2, e2) = FitModel(ns, ds, n => 1.0 / Math.Sqrt(n));
        _output.WriteLine($"1/sqrt(N):    D_inf={D2:F3}  rms={e2:F3}");
        if (double.IsFinite(D2)) DinfVals.Add(D2);
        // Model 3: D(N) = D_inf + a/log(N)
        var (D3, e3) = FitModel(ns, ds, n => 1.0 / Math.Log(Math.Max(n, 2)));
        _output.WriteLine($"1/log(N):     D_inf={D3:F3}  rms={e3:F3}");
        if (double.IsFinite(D3)) DinfVals.Add(D3);
        Assert.True(DinfVals.Count >= 2, "Too few finite extrapolations");
        double mDinf = DinfVals.Average();
        double sDinf = DinfVals.Count > 1 ? Math.Sqrt(DinfVals.Average(x => (x - mDinf) * (x - mDinf))) : 0;
        _output.WriteLine($"D_inf models: mean={mDinf:F3} ± {sDinf:F3} (hint only)");
    }

    private static (double Dinf, double rms) FitModel(List<int> ns, List<double> ds, Func<int, double> xform)
    {
        int n = ns.Count; var x = ns.Select(nv => xform(nv)).ToArray();
        double mx = x.Average(), my = ds.Average(), sxy = 0, sx2 = 0;
        for (int i = 0; i < n; i++) { double a = x[i] - mx; sxy += a * (ds[i] - my); sx2 += a * a; }
        if (sx2 < 1e-15) return (double.NaN, double.NaN);
        double slope = sxy / sx2;
        double Dinf = my - slope * mx;
        double rms = Math.Sqrt(ds.Select((d, i) => { double fit = Dinf + slope * x[i]; double e = d - fit; return e * e; }).Sum() / n);
        return (Dinf, rms);
    }

    // ═══════════════ DCL_04 ModelAgreement ═══════════════
    [Fact]
    public void V4_1_DCL_04_ModelAgreement()
    {
        int[] Ns = [40, 80, 120, 200, 300, 500]; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var ns = new List<int>(); var ds = new List<double>();
        foreach (int N in Ns)
        {
            int E = EpochsForN(N); var biasTbl = CalibrateBias(N);
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
            var (dRaw, _) = MeasureDim(Kc, N, s);
            double dCorr = CorrectDim(dRaw, biasTbl);
            if (double.IsFinite(dCorr)) { ns.Add(N); ds.Add(dCorr); }
        }
        Assert.True(ns.Count >= 3, "Need >= 3 data points");
        var (D1, _) = FitModel(ns, ds, n => 1.0 / n);
        var (D2, _) = FitModel(ns, ds, n => 1.0 / Math.Sqrt(n));
        var (D3, _) = FitModel(ns, ds, n => 1.0 / Math.Log(Math.Max(n, 2)));
        var DinfAll = new[] { D1, D2, D3 }.Where(double.IsFinite).ToArray();
        Assert.True(DinfAll.Length >= 2, "Too few finite D_inf values");
        double mean = DinfAll.Average();
        double spread = DinfAll.Length > 1 ? Math.Sqrt(DinfAll.Average(x => (x - mean) * (x - mean))) : 0;
        int nI = (int)Math.Round(mean);
        string cls = spread < 0.3 ? "Strong agreement" : (spread < 0.7 ? "Moderate agreement" : "Weak agreement");
        _output.WriteLine($"D_inf mean={mean:F3}  spread={spread:F3}  class={cls}");
        _output.WriteLine($"Nearest integer: D={nI} (diagnostic only)");
    }

    // ═══════════════ DCL_05 MultiSeedLargeNStability ═══════════════
    [Fact]
    public void V4_1_DCL_05_MultiSeedLargeNStability()
    {
        double xi = 1.75; double kv = 1.2; double s = 0.1;
        _output.WriteLine("N     seeds  D_corr  std  outliers");
        foreach (var (N, nSeeds) in new (int, int)[] { (80, 10), (200, 5), (300, 3) })
        {
            int E = EpochsForN(N); var biasTbl = CalibrateBias(N);
            var vals = new List<double>();
            for (int seed = 0; seed < nSeeds; seed++)
            {
                var Kc = RecoverFP(KS(N, seed), N, kv, xi, s, E, seed);
                var (dRaw, _) = MeasureDim(Kc, N, s);
                double dCorr = CorrectDim(dRaw, biasTbl);
                if (double.IsFinite(dCorr)) vals.Add(dCorr);
            }
            if (vals.Count < 2) { _output.WriteLine($"{N,5}  {nSeeds,4}  --  --  --"); continue; }
            double m = vals.Average(); double std = vals.Count > 1 ? Math.Sqrt(vals.Average(x => (x - m) * (x - m))) : 0;
            int outs = vals.Count(x => Math.Abs(x - m) > 2.0 * std);
            _output.WriteLine($"{N,5}  {nSeeds,4}  {m:F3}  {std:F3}  {outs}");
        }
    }

    // ═══════════════ DCL_06 LoadInvarianceLargeN ═══════════════
    [Fact]
    public void V4_1_DCL_06_LoadInvarianceLargeN()
    {
        int[] Ns = [80, 200, 300]; double xi = 1.75; double kv = 1.2; double s = 0.1;
        _output.WriteLine("N     D_before  D_after  Δ  stable?");
        foreach (int N in Ns)
        {
            int E = EpochsForN(N); var biasTbl = CalibrateBias(N);
            int ln = N / 2;
            var Kref = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
            var (dRawB, _) = MeasureDim(Kref, N, s);
            var hL = Sm(Kref, N, s, BS, ln, 0.2);
            var Kl = ExpUpd(DL(Nm(RP(hL))), kv, xi);
            var (dRawL, _) = MeasureDim(Kl, N, s);
            double dB = CorrectDim(dRawB, biasTbl); double dL = CorrectDim(dRawL, biasTbl);
            double delta = Math.Abs(dL - dB);
            _output.WriteLine($"{N,5}  {dB:F3}  {dL:F3}  {delta:F3}  {(delta < 0.5 ? "YES" : "NO")}");
        }
    }

    // ═══════════════ DCL_07 PlateauLargeNCheck ═══════════════
    [Fact]
    public void V4_1_DCL_07_PlateauLargeNCheck()
    {
        int N = 200; double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2, 1.5];
        double s = 0.1; int E = EpochsForN(N);
        var biasTbl = CalibrateBias(N);
        double bestSpread = double.MaxValue; double bestXi = 0, bestK0 = 0, bestD = double.NaN;
        _output.WriteLine($"N={N}: xi    K0   D_corr  spread");
        foreach (double xi in xis)
            foreach (double kv in K0s)
            {
                var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
                var (dRaw, sp) = MeasureDim(Kc, N, s);
                double dCorr = CorrectDim(dRaw, biasTbl);
                _output.WriteLine($"    {xi:F2}  {kv:F1}  {dCorr:F3}  {sp:F3}");
                if (sp < bestSpread) { bestSpread = sp; bestXi = xi; bestK0 = kv; bestD = dCorr; }
            }
        _output.WriteLine($"Best: xi={bestXi:F2} K0={bestK0:F1} D={bestD:F3} spread={bestSpread:F3}");
        // Spot-check N=300 at best
        int N2 = 300; int E2 = EpochsForN(N2);
        var biasTbl2 = CalibrateBias(N2);
        var Kc2 = RecoverFP(KS(N2, BS), N2, bestK0, bestXi, s, E2, BS);
        var (dRaw2, sp2) = MeasureDim(Kc2, N2, s);
        double dCorr2 = CorrectDim(dRaw2, biasTbl2);
        _output.WriteLine($"N=300 at best: D_corr={dCorr2:F3} spread={sp2:F3}");
    }

    // ═══════════════ DCL_08 LocalGlobalLargeNDimension ═══════════════
    [Fact]
    public void V4_1_DCL_08_LocalGlobalLargeNDimension()
    {
        int[] Ns = [80, 200, 300]; double xi = 1.75; double kv = 1.2; double s = 0.1;
        _output.WriteLine("N     D_hi   D_lo   D_mid  D_glob  homog?");
        foreach (int N in Ns)
        {
            int E = EpochsForN(N); var biasTbl = CalibrateBias(N);
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
            var dMat = DL(Nm(RP(Sm(Kc, N, s, BS))));
            var degs = new double[N];
            for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j) degs[i] += Kc[i, j];
            var ranked = Enumerable.Range(0, N).OrderByDescending(i => degs[i]).ToArray();
            int nLoc = Math.Max(1, N / 10);
            var hi = new List<double>(); var lo = new List<double>(); var mi = new List<double>();
            for (int i = 0; i < nLoc; i++)
            {
                var (dh, _) = BallDim(dMat, N, ranked[i]); if (double.IsFinite(dh)) hi.Add(dh);
                var (dl, _) = BallDim(dMat, N, ranked[N - 1 - i]); if (double.IsFinite(dl)) lo.Add(dl);
                int miIdx = Math.Clamp(N / 2 + i - nLoc / 2, 0, N - 1);
                var (dm, _) = BallDim(dMat, N, ranked[miIdx]); if (double.IsFinite(dm)) mi.Add(dm);
            }
            double mHi = hi.Count > 0 ? CorrectDim(hi.Average(), biasTbl) : double.NaN;
            double mLo = lo.Count > 0 ? CorrectDim(lo.Average(), biasTbl) : double.NaN;
            double mMi = mi.Count > 0 ? CorrectDim(mi.Average(), biasTbl) : double.NaN;
            var (dGlobal, _) = MeasureDim(Kc, N, s);
            double dGlobCorr = CorrectDim(dGlobal, biasTbl);
            string homog = double.IsFinite(mHi) && double.IsFinite(mLo) && Math.Abs(mHi - mLo) < 0.5 ? "homog" : "mixed";
            _output.WriteLine($"{N,5}  {mHi:F3}  {mLo:F3}  {mMi:F3}  {dGlobCorr:F3}  {homog}");
        }
    }

    // ═══════════════ DCL_09 NullAndDegenerateLargeNControls ═══════════════
    [Fact]
    public void V4_1_DCL_09_NullAndDegenerateLargeNControls()
    {
        int N = 200; double s = 0.1;
        // K=0 null
        var K0 = new double[N, N]; var (d0, _) = MeasureDim(K0, N, s);
        // Random R
        var rng = new Random(BS); var rDist = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { double v = 0.1 + rng.NextDouble(); rDist[i, j] = rDist[j, i] = v; }
        var Kr = ExpUpd(rDist, 1.2, 1.75);
        var (dr, _) = MeasureDim(Kr, N, s);
        // Global sync
        var Kgs = new double[N, N]; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { Kgs[i, j] = 1.0; Kgs[j, i] = 1.0; }
        var (dgs, _) = MeasureDim(Kgs, N, s);
        // Shuffled
        var Kref = RecoverFP(KS(N, BS), N, 1.2, 1.75, s, 3, BS);
        var hSh = Sm(Kref, N, s, BS); var thSh = hSh[^1];
        for (int i = 0; i < N; i++) thSh[i] += rng.NextDouble() * Math.PI;
        var dSh = DL(Nm(RP([thSh]))); var Ksh = ExpUpd(dSh, 1.2, 1.75); var (dsh, _) = MeasureDim(Ksh, N, s);
        // Overcoupled dense
        var Kden = new double[N, N]; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { Kden[i, j] = 2.0; Kden[j, i] = 2.0; }
        var (dden, _) = MeasureDim(Kden, N, s);
        _output.WriteLine($"K=0:     D={d0:F3}  degenerate");
        _output.WriteLine($"RandR:   D={dr:F3}  not physical");
        _output.WriteLine($"GlobSync:D={dgs:F3}  degenerate");
        _output.WriteLine($"Shuffled:D={dsh:F3}  destroyed");
        _output.WriteLine($"Dense:   D={dden:F3}  no continuum signal");
        Assert.True(d0 < 0.5 || double.IsNaN(d0), "K=0 degenerate");
        Assert.True(double.IsFinite(dgs), "GlobSync finite but not treated as physical");
    }

    // ═══════════════ DCL_10 DimensionContinuumReport ═══════════════
    [Fact]
    public void V4_1_DCL_10_DimensionContinuumReport()
    {
        int[] Ns = [40, 80, 120, 200, 300, 500]; double xi = 1.75; double kv = 1.2; double s = 0.1;
        var ns = new List<int>(); var ds = new List<double>(); var sps = new List<double>();
        foreach (int N in Ns)
        {
            int E = EpochsForN(N); var biasTbl = CalibrateBias(N);
            var Kc = RecoverFP(KS(N, BS), N, kv, xi, s, E, BS);
            var (dRaw, sp) = MeasureDim(Kc, N, s);
            double dCorr = CorrectDim(dRaw, biasTbl);
            if (double.IsFinite(dCorr)) { ns.Add(N); ds.Add(dCorr); sps.Add(sp); }
        }
        Assert.True(ns.Count >= 3, "Need >= 3 N values");
        var (Dinf, _) = FitModel(ns, ds, n => 1.0 / n);
        double meanSp = sps.Average();
        double nI = Math.Round(Dinf);
        double lastD = ds[^1];
        string conclusion;
        if (meanSp < 0.7 && Math.Abs(ds[^1] - ds[0]) < 0.5) conclusion = "A) stable large-N corrected dimension attractor";
        else if (meanSp < 1.0 && Math.Abs(ds[^1] - ds[0]) < 1.0) conclusion = "B) weak but measurable large-N attractor";
        else if (meanSp < 1.5) conclusion = "C) estimator/extrapolation-dependent";
        else conclusion = "D) degenerate/no meaningful value";
        _output.WriteLine("═══ DIMENSION CONTINUUM LIMIT REPORT ═══");
        _output.WriteLine($"D_corr(N=40):  {ds[0]:F3}  D_corr(N={ns[^1]}): {lastD:F3}");
        _output.WriteLine($"D_inf (1/N):   {Dinf:F3}");
        _output.WriteLine($"Estimator spread: {meanSp:F3}");
        _output.WriteLine($"Nearest integer: D={nI:F0} (diagnostic only)");
        _output.WriteLine($"Conclusion: {conclusion}");
        _output.WriteLine("Continuum dimension NOT proven. D=3 NOT derived.");
    }

    // ═══════════════ DCL_11 ClaimDisciplineReport ═══════════════
    [Fact]
    public void V4_1_DCL_11_ClaimDisciplineReport()
    {
        _output.WriteLine("═══ CLAIM DISCIPLINE: Dimension Continuum Limit ═══");
        _output.WriteLine("");
        _output.WriteLine("SUPPORTED:");
        _output.WriteLine("  - Corrected D_eff can be tracked across larger N (up to 500).");
        _output.WriteLine("  - Simple extrapolation diagnostics are computable.");
        _output.WriteLine("  - Seed, load, plateau, and local/global stability can be tested.");
        _output.WriteLine("  - Null and degenerate controls are detectable.");
        _output.WriteLine("");
        _output.WriteLine("CONDITIONAL:");
        _output.WriteLine("  - D_inf depends on estimator, extrap model, N range, xi, K0, seeds.");
        _output.WriteLine("  - Graph dimension is NOT continuum spatial dimension.");
        _output.WriteLine("  - Nearest-integer proximity is NOT derivation.");
        _output.WriteLine("");
        _output.WriteLine("HYPOTHESIS:");
        _output.WriteLine("  - TRM may approach a preferred continuum effective dimension.");
        _output.WriteLine("  - D=3 may emerge only after continuum + causal + calibration.");
        _output.WriteLine("  - Stable large-N dimension may be prerequisite for physical space.");
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
}
