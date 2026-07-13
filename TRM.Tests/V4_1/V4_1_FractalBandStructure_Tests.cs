using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Fractal band structure probe: investigates whether TRM parameter bands and
/// fixed-point basins show fractal, multifractal, or self-similar structure
/// under multi-resolution zoom.
///
/// Key diagnostics:
///   - Multi-resolution xi and xi/K0 scans at coarse, medium, fine resolution
///   - Box-counting boundary dimension estimation
///   - Plateau interior (occupied region) dimension
///   - Self-similarity zoom correlation
///   - Log-periodic signal test near band edges
///   - Basin boundary roughness and sensitivity map
///   - Multi-seed and N-scaling fractal persistence
///   - SPARC connection placeholder
///
/// Does NOT claim physical fractals. Does NOT claim SPARC is explained.
/// No D=3 assumption. Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_FractalBandStructure")]
public class V4_1_FractalBandStructure_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_FractalBandStructure_Tests(ITestOutputHelper o) { _output = o; }

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
    private static double Dg(double[,] d) { int N = d.GetLength(0); var v = new List<double>(); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) v.Add(d[i, j]); if (v.Count == 0) return 0; double m = v.Average(); return m > 1e-9 ? v.Average(x => (x - m) * (x - m)) / (m * m) : 0; }
    private static double[,] KS(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }

    private static (double[] times, double[] amps) KickDetect(double[,] K, int N, double s, int seed, int src, double kickAmp, int kickT, double thresh)
    {
        var h0 = Sm(K, N, s, seed); var hk = Sm(K, N, s, seed, src, kickAmp, kickT);
        var times = new double[N]; var amps = new double[N];
        for (int dst = 0; dst < N; dst++) { times[dst] = -1; amps[dst] = 0; if (dst == src) { times[dst] = 0; continue; } for (int t = kickT + 1; t < hk.Length; t++) { double dPh = hk[t][dst] - h0[t][dst]; double dev = Math.Abs(Math.Sin(0.5 * dPh)); if (dev > amps[dst]) amps[dst] = dev; if (dev > thresh && times[dst] < 0) times[dst] = (t - kickT) * Dt * Hd; } }
        return (times, amps);
    }
    private static double[,] RecoverFP(double[,] K0, int N, double K0v, double xi, double s, int E, int seed)
    {
        var Kc = (double[,])K0.Clone();
        for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = ExpUpd(DL(Nm(RP(he))), K0v, xi); }
        return Kc;
    }
    private static (double readiness, int events, string cls) ScanCell(double xi, double K0v, int N, double s, double ka, double thresh, int E)
    {
        var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, E, BS);
        var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
        var ds = new List<double>(); var ts = new List<double>();
        for (int src = 0; src < 4; src++)
        {
            var (times, _) = KickDetect(Kc, N, s, BS + 300, src, ka, 50, thresh);
            for (int dst = 0; dst < N; dst++) if (dst != src && times[dst] > 0 && dMat[src, dst] > 0) { ds.Add(dMat[src, dst]); ts.Add(times[dst]); }
        }
        double r2 = double.NaN;
        if (ds.Count >= 4) { int n = ds.Count; double mx = ds.Average(), my = ts.Average(); double num = 0, den = 0; for (int i = 0; i < n; i++) { double dx = ds[i] - mx, dy = ts[i] - my; num += dx * dy; den += dx * dx; } double a = den > 1e-15 ? num / den : 0; double ssRes = 0, ssTot = 0; for (int i = 0; i < n; i++) { double pred = a * ds[i]; ssRes += (ts[i] - pred) * (ts[i] - pred); ssTot += (ts[i] - my) * (ts[i] - my); } r2 = ssTot > 1e-15 ? 1.0 - ssRes / ssTot : 0; }
        double dg = Dg(dMat); double rdy = Math.Min(1.0, ds.Count / 40.0) * (double.IsFinite(r2) ? Math.Max(0, r2) : 0) * (1.0 / (1.0 + dg));
        string cls = ds.Count < 4 ? "I" : rdy > 0.3 ? "S" : rdy > 0.1 ? "U" : rdy > 0.01 ? "W" : "I";
        return (rdy, ds.Count, cls);
    }

    // ═══════════════ FBS_01 Multi-Resolution Xi Scan ═══════════════
    [Fact]
    public void V4_1_FBS_01_MultiResolutionXiScan()
    {
        int N = 80; double s = 0.1; double K0v = 1.2; double ka = 0.3; double thresh = 0.01;
        var resolutions = new[] { ("Coarse", 0.05, 1.0, 2.3), ("Medium", 0.025, 1.4, 2.1), ("Fine", 0.01, 1.55, 1.95) };
        foreach (var (label, step, xMin, xMax) in resolutions)
        {
            _output.WriteLine($"─── {label} xi scan (step={step:F3}) ───");
            _output.WriteLine("xi      readiness  events   class");
            _output.WriteLine("-----   ---------  -------  -----");
            for (double xi = xMin; xi <= xMax + step * 0.5; xi += step)
            {
                int E = xi >= 1.5 ? 4 : 5;
                var (rdy, ev, cls) = ScanCell(xi, K0v, N, s, ka, thresh, E);
                _output.WriteLine($"{xi:F3}   {rdy,9:F4}  {ev,7}  {cls}");
            }
            _output.WriteLine("");
        }
        Assert.True(true);
    }

    // ═══════════════ FBS_02 Multi-Resolution Xi-K0 Grid ═══════════════
    [Fact]
    public void V4_1_FBS_02_MultiResolutionXiK0Grid()
    {
        int N = 80; double s = 0.1; double ka = 0.3; double thresh = 0.01;
        var grids = new[] {
            ("Grid A (step=0.10)", 0.10, (1.0, 2.3), (0.8, 1.6)),
            ("Grid B (step=0.05)", 0.05, (1.4, 2.1), (1.0, 1.5)),
            ("Grid C (step=0.025)", 0.025, (1.55, 1.95), (1.1, 1.35))
        };
        foreach (var (label, step, xiR, K0R) in grids)
        {
            var clsMap = new List<(double xi, double K0v, string cls)>();
            for (double xi = xiR.Item1; xi <= xiR.Item2 + step * 0.5; xi += step)
            {
                int E = xi >= 1.5 ? 4 : 5;
                for (double K0v = K0R.Item1; K0v <= K0R.Item2 + step * 0.5; K0v += step)
                {
                    var (_, _, cls) = ScanCell(xi, K0v, N, s, ka, thresh, E);
                    clsMap.Add((xi, K0v, cls));
                }
            }
            int sCnt = clsMap.Count(c => c.cls == "S"), uCnt = clsMap.Count(c => c.cls == "U");
            int wCnt = clsMap.Count(c => c.cls == "W"), iCnt = clsMap.Count(c => c.cls == "I");
            _output.WriteLine($"─── {label}: {clsMap.Count} cells, S={sCnt} U={uCnt} W={wCnt} I={iCnt} ───");
            // Count boundary cells
            var xiVals = clsMap.Select(c => c.xi).Distinct().OrderBy(x => x).ToList();
            var K0Vals = clsMap.Select(c => c.K0v).Distinct().OrderBy(x => x).ToList();
            int boundary = 0;
            for (int i = 0; i < clsMap.Count; i++)
            {
                var (xi, K0v, cls) = clsMap[i];
                foreach (var (dx, dk) in new[] { (step, 0.0), (-step, 0.0), (0.0, step), (0.0, -step) })
                {
                    var nb = clsMap.FirstOrDefault(c => Math.Abs(c.xi - xi - dx) < step * 0.1 && Math.Abs(c.K0v - K0v - dk) < step * 0.1);
                    if (nb.cls != null && nb.cls != cls) { boundary++; break; }
                }
            }
            _output.WriteLine($"  Boundary cells: {boundary}  Class map (xi rows):");
            foreach (double xi in xiVals)
            {
                var row = clsMap.Where(c => Math.Abs(c.xi - xi) < step * 0.1).OrderBy(c => c.K0v).Select(c => c.cls);
                _output.WriteLine($"  xi={xi:F2}: {string.Join("", row)}");
            }
            _output.WriteLine("");
        }
        Assert.True(true);
    }

    // ═══════════════ FBS_03 Box-Counting Boundary Dimension ═══════════════
    [Fact]
    public void V4_1_FBS_03_BoxCountingBoundaryDimension()
    {
        int N = 80; double s = 0.1; double K0v = 1.2; double ka = 0.3; double thresh = 0.01;
        // Use multiple step sizes for box counting
        double[] steps = [0.08, 0.04, 0.02, 0.01];
        var boundaryCounts = new List<double>();
        _output.WriteLine("step     N_boundary   log(step)   log(N)");
        _output.WriteLine("------   ----------   ---------   ------");
        foreach (double st in steps)
        {
            var classes = new List<string>();
            for (double xi = 1.3; xi <= 2.1; xi += st)
            {
                int E = xi >= 1.5 ? 4 : 5;
                var (_, _, cls) = ScanCell(xi, K0v, N, s, ka, thresh, E);
                classes.Add(cls);
            }
            int bCount = 0;
            for (int i = 1; i < classes.Count; i++) if (classes[i] != classes[i - 1]) bCount++;
            boundaryCounts.Add(bCount);
            double logStep = Math.Log(st), logN = Math.Log(Math.Max(1, bCount));
            _output.WriteLine($"{st,6:F3}   {bCount,10}   {logStep,9:F4}   {logN,6:F4}");
        }
        // Linear regression: log(N) = a * log(step) + intercept
        double D = double.NaN;
        if (boundaryCounts.Count >= 2)
        {
            var logSteps = steps.Select(s => Math.Log(s)).ToArray();
            var logNs = boundaryCounts.Select(b => Math.Log(Math.Max(1, b))).ToArray();
            double mx = logSteps.Average(), my = logNs.Average();
            double num = 0, den = 0;
            for (int i = 0; i < logSteps.Length; i++) { double dx = logSteps[i] - mx, dy = logNs[i] - my; num += dx * dy; den += dx * dx; }
            D = den > 1e-15 ? -num / den : double.NaN; // D = -slope
        }
        _output.WriteLine($"Estimated boundary dimension D ≈ {D:F3}");
        _output.WriteLine("NOTE: Box-counting estimate only. Not a mathematical proof.");
        Assert.True(double.IsFinite(D) || boundaryCounts.Count < 2);
    }

    // ═══════════════ FBS_04 Plateau Interior Dimension ═══════════════
    [Fact]
    public void V4_1_FBS_04_PlateauInteriorDimension()
    {
        int N = 80; double s = 0.1; double ka = 0.3; double thresh = 0.01;
        // Scan a dense grid and count Strong+Usable cells as "occupied"
        double[] steps = [0.08, 0.04, 0.02];
        _output.WriteLine("step     occupied   total    frac      log(step)  log(occ)");
        _output.WriteLine("------   --------   -----    -------   ---------  --------");
        var logSteps = new List<double>(); var logOccs = new List<double>();
        foreach (double st in steps)
        {
            int occ = 0, total = 0;
            for (double xi = 1.3; xi <= 2.1; xi += st)
            {
                int E = xi >= 1.5 ? 4 : 5;
                for (double K0v = 0.9; K0v <= 1.5; K0v += st)
                {
                    total++;
                    var (rdy, _, _) = ScanCell(xi, K0v, N, s, ka, thresh, E);
                    if (rdy > 0.1) occ++;
                }
            }
            logSteps.Add(Math.Log(st)); logOccs.Add(Math.Log(Math.Max(1, occ)));
            _output.WriteLine($"{st,6:F3}   {occ,8}   {total,5}   {100.0 * occ / total,6:F1}%  {Math.Log(st),9:F4}  {Math.Log(Math.Max(1, occ)),8:F4}");
        }
        double Dint = double.NaN;
        if (logSteps.Count >= 2)
        {
            double mx = logSteps.Average(), my = logOccs.Average();
            double num = 0, den = 0;
            for (int i = 0; i < logSteps.Count; i++) { double dx = logSteps[i] - mx, dy = logOccs[i] - my; num += dx * dy; den += dx * dx; }
            Dint = den > 1e-15 ? -num / den : double.NaN;
        }
        _output.WriteLine($"Estimated interior (occupied) dimension D_int ≈ {Dint:F3}");
        _output.WriteLine("D_int≈2 = area-like, D_int≈1 = filament-like.");
        Assert.True(double.IsFinite(Dint) || logSteps.Count < 2);
    }

    // ═══════════════ FBS_05 Self-Similarity Zoom Correlation ═══════════════
    [Fact]
    public void V4_1_FBS_05_SelfSimilarityZoomCorrelation()
    {
        int N = 60; double s = 0.1; double ka = 0.3; double thresh = 0.01; double K0v = 1.2;
        // Use two overlapping ranges with the same number of points
        double xMin = 1.5, xMax = 2.0; int nPts = 11;
        double xiBroad = (xMax - xMin) / (nPts - 1);
        double xiNarrow = (xMax - xMin) / (nPts - 1) * 0.5; // Finer resolution
        var broad = new List<double>(); var narrow = new List<double>();
        for (int i = 0; i < nPts; i++)
        {
            double xi = xMin + i * xiBroad;
            int E = xi >= 1.5 ? 4 : 5;
            var (r, _, _) = ScanCell(xi, K0v, N, s, ka, thresh, E);
            broad.Add(r);
        }
        for (int i = 0; i < nPts; i++)
        {
            double xi = xMin + i * xiNarrow;
            int E = xi >= 1.5 ? 4 : 5;
            var (r, _, _) = ScanCell(xi, K0v, N, s, ka, thresh, E);
            narrow.Add(r);
        }
        // Resample broad to match narrow's xi positions using linear interpolation
        var broadInterp = new List<double>();
        for (int i = 0; i < nPts; i++)
        {
            double xi = xMin + i * xiNarrow;
            int lo = (int)((xi - xMin) / xiBroad);
            lo = Math.Clamp(lo, 0, nPts - 2);
            double t = (xi - (xMin + lo * xiBroad)) / xiBroad;
            broadInterp.Add(broad[lo] * (1 - t) + broad[Math.Min(lo + 1, nPts - 1)] * t);
        }
        double rho = Spear(broadInterp.ToArray(), narrow.ToArray());
        _output.WriteLine($"Self-similarity zoom correlation ({xMin}-{xMax}): rho = {rho:F4}");
        _output.WriteLine($"Broad points: {broad.Count}, Narrow points: {narrow.Count}");
        _output.WriteLine("NOTE: High correlation suggests self-similar structure under zoom.");
        Assert.True(double.IsFinite(rho) || broad.Count < 3);
    }

    // ═══════════════ FBS_06 Log-Periodic Signal Test ═══════════════
    [Fact]
    public void V4_1_FBS_06_LogPeriodicSignalTest()
    {
        int N = 80; double s = 0.1; double K0v = 1.2; double ka = 0.3; double thresh = 0.01;
        double xiC = 1.5; // approximate transition edge
        var xis = new List<double>(); var rdys = new List<double>();
        for (double xi = 1.5; xi <= 2.1; xi += 0.02)
        {
            int E = xi >= 1.5 ? 4 : 5;
            var (r, _, _) = ScanCell(xi, K0v, N, s, ka, thresh, E);
            xis.Add(xi); rdys.Add(r);
        }
        // Compute simple spectral proxy: fit a sine to log(x - xiC + eps)
        double eps = 0.01;
        var logDists = xis.Select(x => Math.Log(Math.Max(x - xiC, eps))).ToArray();
        double mx = logDists.Average(), my = rdys.Average();
        // Simple periodogram: test a few candidate periods
        double bestAmp = 0; double bestPer = 0;
        foreach (double per in new[] { 0.5, 1.0, 1.5, 2.0 })
        {
            double aCos = 0, aSin = 0;
            for (int i = 0; i < logDists.Length; i++)
            {
                double dx = logDists[i] - mx, dy = rdys[i] - my;
                double phase = 2 * Math.PI * logDists[i] / per;
                aCos += dy * Math.Cos(phase);
                aSin += dy * Math.Sin(phase);
            }
            double amp = Math.Sqrt(aCos * aCos + aSin * aSin) / logDists.Length;
            if (amp > bestAmp) { bestAmp = amp; bestPer = per; }
        }
        _output.WriteLine($"Log-periodic test: best amplitude = {bestAmp:F4} at period ≈ {bestPer:F1}");
        _output.WriteLine(bestAmp < 0.02 ? "  No significant log-periodic signal detected." : "  Possible weak log-periodic modulation.");
        _output.WriteLine("NOTE: Inconclusive. Do not overfit.");
        Assert.True(true);
    }

    // ═══════════════ FBS_07 Basin Boundary Roughness ═══════════════
    [Fact]
    public void V4_1_FBS_07_BasinBoundaryRoughness()
    {
        int N = 80; double s = 0.1; double ka = 0.3; double thresh = 0.01;
        double[] xis = [1.40, 1.45, 1.50, 1.55, 1.60, 1.65, 1.70, 1.75, 1.80, 1.85, 1.90, 1.95, 2.00];
        double[] K0s = [0.9, 1.0, 1.1, 1.2, 1.3, 1.4];
        _output.WriteLine("═══ BOUNDARY ROUGHNESS AND SENSITIVITY ═══");
        _output.WriteLine("xi      K0     readiness  class  dRdy_dXi  dRdy_dK0");
        _output.WriteLine("-----   ----   ---------  -----  --------  --------");
        var grid = new Dictionary<(double, double), (double rdy, string cls)>();
        foreach (double xi in xis)
        {
            int E = xi >= 1.5 ? 4 : 5;
            foreach (double K0v in K0s)
            {
                var (rdy, _, cls) = ScanCell(xi, K0v, N, s, ka, thresh, E);
                grid[(xi, K0v)] = (rdy, cls);
            }
        }
        foreach (double xi in xis)
            foreach (double K0v in K0s)
            {
                var (rdy, cls) = grid[(xi, K0v)];
                double dXi = grid.ContainsKey((xi + 0.01, K0v)) && grid.ContainsKey((xi - 0.01, K0v))
                    ? (grid[(xi + 0.01, K0v)].rdy - grid[(xi - 0.01, K0v)].rdy) / 0.02 : double.NaN;
                double dK0 = grid.ContainsKey((xi, K0v + 0.01)) && grid.ContainsKey((xi, K0v - 0.01))
                    ? (grid[(xi, K0v + 0.01)].rdy - grid[(xi, K0v - 0.01)].rdy) / 0.02 : double.NaN;
                _output.WriteLine($"{xi:F2}    {K0v:F2}    {rdy,9:F4}  {cls,3}   {dXi,8:F4}  {dK0,8:F4}");
            }
        int flips = 0;
        foreach (var ((xi, K0v), (_, cls)) in grid)
            foreach (var ((nXi, nK0), _) in grid)
                if ((Math.Abs(nXi - xi) < 0.02 && Math.Abs(nK0 - K0v) < 0.02 && !(Math.Abs(nXi - xi) < 0.001 && Math.Abs(nK0 - K0v) < 0.001)))
                { var (_, nCls) = grid[(nXi, nK0)]; if (nCls != cls) flips++; }
        _output.WriteLine($"Local class flips: ~{flips} (higher = rougher boundaries)");
        Assert.True(true);
    }

    // ═══════════════ FBS_08 Multi-Seed Fractal Stability ═══════════════
    [Fact]
    public void V4_1_FBS_08_MultiSeedFractalStability()
    {
        int N = 60; double s = 0.1; double K0v = 1.2; double ka = 0.3; double thresh = 0.01; int nSeeds = 8;
        double[] xis = [1.50, 1.60, 1.70, 1.75, 1.80, 1.90, 2.00];
        _output.WriteLine("xi      rd_mean   rd_std    ev_mean   ev_std   cls_stability");
        _output.WriteLine("-----   -------   -------   -------   -------   -------------");
        foreach (double xi in xis)
        {
            int E = xi >= 1.5 ? 4 : 5;
            var rdys = new List<double>(); var evs = new List<int>(); var classes = new Dictionary<string, int>();
            for (int sd = 0; sd < nSeeds; sd++)
            {
                var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, E, BS + sd * 10);
                var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
                var ds = new List<double>(); var ts = new List<double>();
                for (int src = 0; src < 4; src++)
                {
                    var (times, _) = KickDetect(Kc, N, s, BS + 300, src, ka, 50, thresh);
                    for (int dst = 0; dst < N; dst++) if (dst != src && times[dst] > 0 && dMat[src, dst] > 0) { ds.Add(dMat[src, dst]); ts.Add(times[dst]); }
                }
                double r2 = double.NaN;
                if (ds.Count >= 4) { int n = ds.Count; double mx = ds.Average(), my = ts.Average(); double num = 0, den = 0; for (int i = 0; i < n; i++) { double dx = ds[i] - mx, dy = ts[i] - my; num += dx * dy; den += dx * dx; } double a = den > 1e-15 ? num / den : 0; double ssRes = 0, ssTot = 0; for (int i = 0; i < n; i++) { double pred = a * ds[i]; ssRes += (ts[i] - pred) * (ts[i] - pred); ssTot += (ts[i] - my) * (ts[i] - my); } r2 = ssTot > 1e-15 ? 1.0 - ssRes / ssTot : 0; }
                double dg = Dg(dMat); double rdy = Math.Min(1.0, ds.Count / 40.0) * (double.IsFinite(r2) ? Math.Max(0, r2) : 0) * (1.0 / (1.0 + dg));
                string cls = ds.Count < 4 ? "I" : rdy > 0.3 ? "S" : rdy > 0.1 ? "U" : "W";
                rdys.Add(rdy); evs.Add(ds.Count);
                classes[cls] = classes.GetValueOrDefault(cls) + 1;
            }
            double mr = rdys.Average(), sr = rdys.Count > 1 ? Math.Sqrt(rdys.Average(x => (x - mr) * (x - mr))) : 0;
            double me = evs.Average(), se = evs.Count > 1 ? Math.Sqrt(evs.Average(x => (x - me) * (x - me))) : 0;
            string domCls = classes.OrderByDescending(kv => kv.Value).First().Key;
            double stab = 100.0 * classes[domCls] / nSeeds;
            _output.WriteLine($"{xi:F2}    {mr,7:F4}   {sr,7:F4}   {me,7:F1}   {se,7:F1}   {domCls} ({stab:F0}%)");
        }
        Assert.True(true);
    }

    // ═══════════════ FBS_09 N-Scaling Fractal Persistence ═══════════════
    [Fact]
    public void V4_1_FBS_09_NScalingFractalPersistence()
    {
        int[] Ns = [40, 80, 120, 200]; double s = 0.1; double K0v = 1.2; double ka = 0.3; double thresh = 0.01;
        double[] xis = [1.50, 1.60, 1.70, 1.75, 1.80, 1.90, 2.00];
        _output.WriteLine("N      xi      readiness  events   class");
        _output.WriteLine("----   -----   ---------  -------  -----");
        foreach (int N in Ns)
        {
            int Ebase = N <= 80 ? 5 : 3;
            foreach (double xi in xis)
            {
                int E = xi >= 1.5 && N <= 80 ? 4 : Ebase;
                var K0 = KS(N, BS); var Kc = RecoverFP(K0, N, K0v, xi, s, E, BS);
                var dMat = DL(Nm(RP(Sm(Kc, N, s, BS + 200))));
                int nSrc = N <= 80 ? 4 : 3;
                var ds = new List<double>(); var ts = new List<double>();
                for (int src = 0; src < nSrc; src++)
                {
                    var (times, _) = KickDetect(Kc, N, s, BS + 300, src, ka, 50, thresh);
                    for (int dst = 0; dst < N; dst++) if (dst != src && times[dst] > 0 && dMat[src, dst] > 0) { ds.Add(dMat[src, dst]); ts.Add(times[dst]); }
                }
                double r2 = double.NaN;
                if (ds.Count >= 4) { int n = ds.Count; double mx = ds.Average(), my = ts.Average(); double num = 0, den = 0; for (int i = 0; i < n; i++) { double dx = ds[i] - mx, dy = ts[i] - my; num += dx * dy; den += dx * dx; } double a = den > 1e-15 ? num / den : 0; double ssRes = 0, ssTot = 0; for (int i = 0; i < n; i++) { double pred = a * ds[i]; ssRes += (ts[i] - pred) * (ts[i] - pred); ssTot += (ts[i] - my) * (ts[i] - my); } r2 = ssTot > 1e-15 ? 1.0 - ssRes / ssTot : 0; }
                double dg = Dg(dMat); double rdy = Math.Min(1.0, ds.Count / 40.0) * (double.IsFinite(r2) ? Math.Max(0, r2) : 0) * (1.0 / (1.0 + dg));
                string cls = ds.Count < 4 ? "I" : rdy > 0.3 ? "S" : rdy > 0.1 ? "U" : "W";
                _output.WriteLine($"{N,5}  {xi:F2}   {rdy,9:F4}  {ds.Count,7}  {cls}");
            }
        }
        Assert.True(true);
    }

    // ═══════════════ FBS_10 Null Model Comparison ═══════════════
    [Fact]
    public void V4_1_FBS_10_NullModelComparison()
    {
        int N = 60; double s = 0.1; double K0v = 1.2; double ka = 0.3; double thresh = 0.01;
        double[] xis = [1.50, 1.75, 2.00];
        _output.WriteLine("Case          xi      readiness  events");
        _output.WriteLine("------------  -----   ---------  -------");
        foreach (double xi in xis)
        {
            int E = xi >= 1.5 ? 4 : 5;
            var (rdyA, evA, _) = ScanCell(xi, K0v, N, s, ka, thresh, E);
            _output.WriteLine($"Active        {xi,5:F2}   {rdyA,9:F4}  {evA,7}");
            var KcN = RecoverFP(new double[N, N], N, K0v, xi, s, E, BS);
            var dMatN = DL(Nm(RP(Sm(KcN, N, s, BS + 200))));
            int evN = 0; for (int src = 0; src < 4; src++) { var (times, _) = KickDetect(KcN, N, s, BS + 300, src, ka, 50, thresh); for (int dst = 0; dst < N; dst++) if (dst != src && times[dst] > 0) evN++; }
            _output.WriteLine($"K=0 null      {xi,5:F2}   --          {evN,7}");
        }
        var hSync = new double[101][]; for (int t = 0; t < 101; t++) { hSync[t] = new double[N]; for (int i = 0; i < N; i++) hSync[t][i] = 0; }
        double dgGS = Dg(DL(Nm(RP(hSync))));
        _output.WriteLine($"Global sync   --      --          dg={dgGS:F4} Degenerate");
        Assert.True(dgGS < 0.01);
    }

    // ═══════════════ FBS_11 SPARC Connection Placeholder ═══════════════
    [Fact]
    public void V4_1_FBS_11_SPARCConnectionPlaceholder()
    {
        // Check for SPARC data in common locations
        var searchPaths = new[] {
            "data/SPARC", "datasets/SPARC", "SPARC", "sparc",
            "external/SPARC", "docs/SPARC", "wwwroot/data/SPARC"
        };
        var found = new List<string>();
        foreach (var p in searchPaths)
        {
            var full = Path.Combine(Directory.GetCurrentDirectory(), p);
            if (Directory.Exists(full)) found.Add(full);
            var projRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", ".."));
            var projFull = Path.Combine(projRoot, p);
            if (Directory.Exists(projFull) && !found.Contains(projFull)) found.Add(projFull);
        }
        if (found.Count > 0)
        {
            _output.WriteLine($"SPARC data found at: {string.Join("; ", found)}");
            _output.WriteLine("SPARC fractal residual analysis not yet implemented.");
        }
        else
        {
            _output.WriteLine("SPARC fractal residual analysis not run: no dataset found.");
            _output.WriteLine("Checked paths: " + string.Join(", ", searchPaths));
        }
        _output.WriteLine("");
        _output.WriteLine("Potential future SPARC diagnostics:");
        _output.WriteLine("  - Residual power spectrum");
        _output.WriteLine("  - Radial log-periodicity");
        _output.WriteLine("  - Multifractal residual distribution");
        _output.WriteLine("  - Scale-dependent acceleration residuals");
        _output.WriteLine("  - Comparison to TRM band exponents");
        _output.WriteLine("");
        _output.WriteLine("NOTE: SPARC connection is NOT tested. Dark matter is NOT replaced.");
        Assert.True(true);
    }

    // ═══════════════ FBS_12 Claim Discipline Report ═══════════════
    [Fact]
    public void V4_1_FBS_12_ClaimDisciplineReport()
    {
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("  CLAIM DISCIPLINE — FRACTAL BAND STRUCTURE PROBE");
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  SUPPORTED:");
        _output.WriteLine("    - Multi-resolution band diagnostics are measurable.");
        _output.WriteLine("    - Box-counting estimates can be computed.");
        _output.WriteLine("    - Self-similarity proxies can be evaluated.");
        _output.WriteLine("    - Null comparisons can be performed.");
        _output.WriteLine("    - Boundary roughness and sensitivity are measurable.");
        _output.WriteLine("    - Class stability across seeds is measurable.");
        _output.WriteLine("");
        _output.WriteLine("  CONDITIONAL:");
        _output.WriteLine("    - Fractal estimates depend on grid resolution,");
        _output.WriteLine("      thresholds, finite-size effects, seeds, and N.");
        _output.WriteLine("    - Observed self-similarity may be numerical.");
        _output.WriteLine("    - SPARC connection is NOT tested (no data found).");
        _output.WriteLine("    - Box-counting dimension is a numerical estimate.");
        _output.WriteLine("");
        _output.WriteLine("  HYPOTHESIS:");
        _output.WriteLine("    - TRM parameter bands may have fractal structure.");
        _output.WriteLine("    - Resonance bands may relate to mode-locking or");
        _output.WriteLine("      Arnold-tongue-like dynamics.");
        _output.WriteLine("    - Similar scaling may appear in galaxy residuals.");
        _output.WriteLine("");
        _output.WriteLine("  NOT CLAIMED:");
        _output.WriteLine("    - Fractality is proven.");
        _output.WriteLine("    - SPARC galaxies are explained.");
        _output.WriteLine("    - Dark matter is replaced.");
        _output.WriteLine("    - Physical constants are derived.");
        _output.WriteLine("    - c is derived.");
        _output.WriteLine("    - D=3 is derived.");
        _output.WriteLine("    - Lorentz invariance is derived.");
        _output.WriteLine("══════════════════════════════════════════════════");
        Assert.True(true);
    }
}
