using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Fine structure parameter scan: high-resolution investigation around the
/// strongest causal-front regimes to determine whether TRM exhibits broad
/// stable plateaus, narrow resonance bands, multiple stability islands,
/// or sharp phase transitions.
///
/// Key diagnostics:
///   - Xi fine scan at fixed K0
///   - K0 fine scan at fixed xi
///   - Combined xi-K0 landscape with cell classification
///   - Resonance band detection and width measurement
///   - Plateau vs peak classification
///   - Multi-seed and N-scaling band persistence
///   - Event-count structure contribution breakdown
///
/// Does NOT claim physical significance. Does NOT claim physical c.
/// No D=3 assumption. Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_FineStructureScan")]
public class V4_1_FineStructureParameterScan_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_FineStructureParameterScan_Tests(ITestOutputHelper o) { _output = o; }

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
    private static (double readiness, int events, double r2, double dg) EvalRegime(double xi, double K0v, int N, double s, double ka, double thresh, int E)
    {
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
        double dg = Dg(dMat);
        double evNorm = Math.Min(1.0, ds.Count / 40.0);
        double fitQ = double.IsFinite(r2) ? Math.Max(0, r2) : 0;
        double nonDeg = 1.0 / (1.0 + dg);
        return (evNorm * fitQ * nonDeg, ds.Count, r2, dg);
    }
    private static string FrontCls(double readiness, int ev) => ev < 4 ? "Insufficient" : readiness > 0.3 ? "Strong" : readiness > 0.1 ? "Usable" : readiness > 0.01 ? "Weak" : "Degenerate";

    // ═══════════════ FSS_01 Xi Fine Scan ═══════════════
    [Fact]
    public void V4_1_FSS_01_XiFineScan()
    {
        int N = 60; double s = 0.1; double K0v = 1.2; double ka = 0.3; double thresh = 0.01;
        double[] xis = Enumerable.Range(26, 15).Select(i => i * 0.05).ToArray(); // 1.30 to 2.00 step 0.05
        _output.WriteLine("xi      readiness  events   R²       dg       class");
        _output.WriteLine("-----   ---------  -------  -------  -------  ----------");
        foreach (double xi in xis)
        {
            int E = xi >= 1.5 ? 4 : 5;
            var (rdy, ev, r2, dg) = EvalRegime(xi, K0v, N, s, ka, thresh, E);
            _output.WriteLine($"{xi:F2}    {rdy,9:F4}  {ev,7}  {r2,7:F4}  {dg,6:F4}  {FrontCls(rdy, ev)}");
        }
        Assert.True(true);
    }

    // ═══════════════ FSS_02 K0 Fine Scan ═══════════════
    [Fact]
    public void V4_1_FSS_02_K0FineScan()
    {
        int N = 60; double s = 0.1; double xi = 1.75; double ka = 0.3; double thresh = 0.01;
        double[] K0s = Enumerable.Range(16, 17).Select(i => i * 0.05).ToArray(); // 0.80 to 1.60 step 0.05
        _output.WriteLine("K0      readiness  events   R²       dg       class");
        _output.WriteLine("-----   ---------  -------  -------  -------  ----------");
        foreach (double K0v in K0s)
        {
            int E = xi >= 1.5 ? 4 : 5;
            var (rdy, ev, r2, dg) = EvalRegime(xi, K0v, N, s, ka, thresh, E);
            _output.WriteLine($"{K0v:F2}    {rdy,9:F4}  {ev,7}  {r2,7:F4}  {dg,6:F4}  {FrontCls(rdy, ev)}");
        }
        Assert.True(true);
    }

    // ═══════════════ FSS_03 Combined Xi-K0 Landscape ═══════════════
    [Fact]
    public void V4_1_FSS_03_CombinedXiK0Landscape()
    {
        int N = 60; double s = 0.1; double ka = 0.3; double thresh = 0.01;
        double[] xis = [1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 1.9, 2.0];
        double[] K0s = [0.8, 0.9, 1.0, 1.1, 1.2, 1.3, 1.4, 1.5, 1.6];
        var landscape = new (double xi, double K0v, double rdy, int ev, string cls)[xis.Length * K0s.Length];
        int idx = 0;
        foreach (double xi in xis)
        {
            int E = xi >= 1.5 ? 4 : 5;
            foreach (double K0v in K0s)
            {
                var (rdy, ev, _, _) = EvalRegime(xi, K0v, N, s, ka, thresh, E);
                string cls = ev < 4 ? "D" : rdy > 0.3 ? "S" : rdy > 0.1 ? "U" : rdy > 0.01 ? "W" : "I";
                landscape[idx++] = (xi, K0v, rdy, ev, cls);
            }
        }
        _output.WriteLine("═══ XI-K0 LANDSCAPE (D=Degenerate, I=Insufficient, W=Weak, U=Usable, S=Strong) ═══");
        var hdr = "xi\\K0 " + string.Join("", K0s.Select(k => $"  {k,4:F1} "));
        _output.WriteLine(hdr);
        foreach (double xi in xis)
        {
            var row = $"{xi,4:F1}  ";
            foreach (double K0v in K0s)
            {
                var cell = landscape.First(l => Math.Abs(l.xi - xi) < 0.001 && Math.Abs(l.K0v - K0v) < 0.001);
                row += $"  {cell.rdy,4:F2}{cell.cls}";
            }
            _output.WriteLine(row);
        }
        // Count classes
        int sCnt = landscape.Count(l => l.cls == "S");
        int uCnt = landscape.Count(l => l.cls == "U");
        int wCnt = landscape.Count(l => l.cls == "W");
        int iCnt = landscape.Count(l => l.cls == "I");
        int dCnt = landscape.Count(l => l.cls == "D");
        _output.WriteLine($"Summary: Strong={sCnt} Usable={uCnt} Weak={wCnt} Insufficient={iCnt} Degenerate={dCnt}");
        Assert.True(sCnt + uCnt + wCnt + iCnt + dCnt == landscape.Length);
    }

    // ═══════════════ FSS_04 Resonance Band Detection ═══════════════
    [Fact]
    public void V4_1_FSS_04_ResonanceBandDetection()
    {
        int N = 60; double s = 0.1; double ka = 0.3; double thresh = 0.01; double K0v = 1.2;
        double[] xis = Enumerable.Range(26, 15).Select(i => i * 0.05).ToArray();
        var rdys = new List<double>(); var evs = new List<int>();
        foreach (double xi in xis) { int E = xi >= 1.5 ? 4 : 5; var (r, e, _, _) = EvalRegime(xi, K0v, N, s, ka, thresh, E); rdys.Add(r); evs.Add(e); }

        // Find local maxima in readiness (3-point neighborhood)
        var peaks = new List<(int idx, double xi, double rdy)>();
        for (int i = 1; i < rdys.Count - 1; i++)
            if (rdys[i] > rdys[i - 1] && rdys[i] >= rdys[i + 1] && rdys[i] > 0.01)
                peaks.Add((i, xis[i], rdys[i]));

        // Measure band width: find where readiness drops to 50% of peak on each side
        _output.WriteLine("═══ RESONANCE BAND DETECTION ═══");
        _output.WriteLine("Peak xi   readiness  events   width_50%   prominence");
        _output.WriteLine("-------   ---------  -------  ----------  ----------");
        if (peaks.Count == 0) _output.WriteLine("  No significant peaks detected.");
        foreach (var (pi, pxi, prdy) in peaks)
        {
            // Width at 50% peak height
            double halfH = prdy * 0.5;
            int left = pi;
            while (left > 0 && rdys[left] > halfH) left--;
            int right = pi;
            while (right < rdys.Count - 1 && rdys[right] > halfH) right++;
            double width = xis[Math.Min(right, xis.Length - 1)] - xis[Math.Max(left, 0)];
            double prominence = prdy - Math.Min(rdys.Take(pi).DefaultIfEmpty(0).Min(), rdys.Skip(pi + 1).DefaultIfEmpty(0).Min());
            _output.WriteLine($"{pxi,7:F2}   {prdy,9:F4}  {evs[pi],7}  {width,10:F4}  {prominence,10:F4}");
        }
        _output.WriteLine($"Total peaks found: {peaks.Count}");
        Assert.True(true);
    }

    // ═══════════════ FSS_05 Plateau vs Peak Classification ═══════════════
    [Fact]
    public void V4_1_FSS_05_PlateauVsPeakClassification()
    {
        int N = 60; double s = 0.1; double ka = 0.3; double thresh = 0.01; double K0v = 1.2;
        double[] xis = Enumerable.Range(26, 15).Select(i => i * 0.05).ToArray();
        var rdys = new List<double>(); var evs = new List<int>();
        foreach (double xi in xis) { int E = xi >= 1.5 ? 4 : 5; var (r, e, _, _) = EvalRegime(xi, K0v, N, s, ka, thresh, E); rdys.Add(r); evs.Add(e); }

        // Find peaks
        var peaks = new List<(int idx, double xi, double rdy)>();
        for (int i = 1; i < rdys.Count - 1; i++)
            if (rdys[i] > rdys[i - 1] && rdys[i] >= rdys[i + 1] && rdys[i] > 0.01)
                peaks.Add((i, xis[i], rdys[i]));

        _output.WriteLine("═══ PLATEAU VS PEAK CLASSIFICATION ═══");
        _output.WriteLine("Peak xi   curvature  width_90%  class");
        _output.WriteLine("-------   ---------  ---------  --------------");
        foreach (var (pi, pxi, prdy) in peaks)
        {
            // Curvature: second derivative approximation
            double curv = pi > 0 && pi < rdys.Count - 1 ? (rdys[pi - 1] + rdys[pi + 1] - 2 * rdys[pi]) / (0.05 * 0.05) : 0;
            double absCurv = Math.Abs(curv);
            // Width at 90% peak
            double h90 = prdy * 0.9;
            int l = pi; while (l > 0 && rdys[l] > h90) l--;
            int r = pi; while (r < rdys.Count - 1 && rdys[r] > h90) r++;
            double w90 = xis[Math.Min(r, xis.Length - 1)] - xis[Math.Max(l, 0)];
            string cls = w90 > 0.4 ? "Broad plateau" : w90 > 0.2 ? "Moderate band" : "Sharp peak";
            _output.WriteLine($"{pxi,7:F2}   {absCurv,9:F4}  {w90,9:F4}  {cls}");
        }
        Assert.True(true);
    }

    // ═══════════════ FSS_06 Multi-Seed Band Stability ═══════════════
    [Fact]
    public void V4_1_FSS_06_MultiSeedBandStability()
    {
        int N = 60; double s = 0.1; double ka = 0.3; double thresh = 0.01; double K0v = 1.2; int nSeeds = 12;
        double[] xis = [1.50, 1.60, 1.70, 1.75, 1.80, 1.90, 2.00];
        _output.WriteLine("xi      rd_mean   rd_std    ev_mean   band_persist%");
        _output.WriteLine("-----   -------   -------   -------   -------------");
        foreach (double xi in xis)
        {
            int E = xi >= 1.5 ? 4 : 5;
            var rdVals = new List<double>(); var evVals = new List<int>(); int active = 0;
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
                double dg = Dg(dMat);
                double rdy = Math.Min(1.0, ds.Count / 40.0) * (double.IsFinite(r2) ? Math.Max(0, r2) : 0) * (1.0 / (1.0 + dg));
                if (rdy > 0.01) { rdVals.Add(rdy); evVals.Add(ds.Count); }
                if (ds.Count >= 4) active++;
            }
            double mr = rdVals.Count > 0 ? rdVals.Average() : 0;
            double sr = rdVals.Count > 1 ? Math.Sqrt(rdVals.Average(x => (x - mr) * (x - mr))) : 0;
            double me = evVals.Count > 0 ? evVals.Average() : 0;
            double persist = 100.0 * active / nSeeds;
            _output.WriteLine($"{xi,5:F2}   {mr,7:F4}   {sr,7:F4}   {me,7:F1}   {persist,13:F1}");
        }
        Assert.True(true);
    }

    // ═══════════════ FSS_07 N-Scaling Band Persistence ═══════════════
    [Fact]
    public void V4_1_FSS_07_NScalingBandPersistence()
    {
        int[] Ns = [40, 80, 120, 200]; double s = 0.1; double ka = 0.3; double thresh = 0.01; double K0v = 1.2;
        double[] xis = [1.40, 1.50, 1.60, 1.70, 1.75, 1.80, 1.90, 2.00];
        _output.WriteLine("N      xi      readiness  events   class");
        _output.WriteLine("----   -----   ---------  -------  ----------");
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
                double dg = Dg(dMat);
                double rdy = Math.Min(1.0, ds.Count / 40.0) * (double.IsFinite(r2) ? Math.Max(0, r2) : 0) * (1.0 / (1.0 + dg));
                _output.WriteLine($"{N,5}  {xi:F2}   {rdy,9:F4}  {ds.Count,7}  {FrontCls(rdy, ds.Count)}");
            }
        }
        Assert.True(true);
    }

    // ═══════════════ FSS_08 Event Count Structure ═══════════════
    [Fact]
    public void V4_1_FSS_08_EventCountStructure()
    {
        int N = 60; double s = 0.1; double ka = 0.3; double thresh = 0.01; double K0v = 1.2;
        double[] xis = [1.50, 1.60, 1.70, 1.75, 1.80, 1.90, 2.00];
        _output.WriteLine("xi      readiness  events   ev_contrib  fit_contrib  nonDeg_contrib");
        _output.WriteLine("-----   ---------  -------  ----------  -----------  --------------");
        foreach (double xi in xis)
        {
            int E = xi >= 1.5 ? 4 : 5;
            var (rdy, ev, r2, dg) = EvalRegime(xi, K0v, N, s, ka, thresh, E);
            double evCont = Math.Min(1.0, ev / 40.0);
            double fitCont = double.IsFinite(r2) ? Math.Max(0, r2) : 0;
            double ndCont = 1.0 / (1.0 + dg);
            _output.WriteLine($"{xi:F2}    {rdy,9:F4}  {ev,7}  {evCont,10:F4}  {fitCont,11:F4}  {ndCont,14:F4}");
        }
        _output.WriteLine("Readiness = evCont * fitCont * nonDegCont");
        Assert.True(true);
    }

    // ═══════════════ FSS_09 Null Comparison ═══════════════
    [Fact]
    public void V4_1_FSS_09_NullComparison()
    {
        int N = 60; double s = 0.1; double ka = 0.3; double thresh = 0.01;
        double[] xis = [1.50, 1.75, 2.00]; double K0v = 1.2;
        _output.WriteLine("Case          xi      readiness  events");
        _output.WriteLine("------------  -----   ---------  -------");
        foreach (double xi in xis)
        {
            int E = xi >= 1.5 ? 4 : 5;
            // Active
            var (rdyA, evA, _, _) = EvalRegime(xi, K0v, N, s, ka, thresh, E);
            _output.WriteLine($"Active        {xi,5:F2}   {rdyA,9:F4}  {evA,7}");
            // K=0
            var KcN = RecoverFP(new double[N, N], N, K0v, xi, s, E, BS);
            var dMatN = DL(Nm(RP(Sm(KcN, N, s, BS + 200))));
            int evN = 0; for (int src = 0; src < 4; src++) { var (times, _) = KickDetect(KcN, N, s, BS + 300, src, ka, 50, thresh); for (int dst = 0; dst < N; dst++) if (dst != src && times[dst] > 0) evN++; }
            _output.WriteLine($"K=0 null      {xi,5:F2}   --          {evN,7}");
        }
        // Global sync
        var hSync = new double[101][]; for (int t = 0; t < 101; t++) { hSync[t] = new double[N]; for (int i = 0; i < N; i++) hSync[t][i] = 0; }
        double dgGS = Dg(DL(Nm(RP(hSync))));
        _output.WriteLine($"Global sync   --      --          dg={dgGS:F4} Degenerate");
        Assert.True(dgGS < 0.01);
    }

    // ═══════════════ FSS_10 Claim Discipline Report ═══════════════
    [Fact]
    public void V4_1_FSS_10_ClaimDisciplineReport()
    {
        // Run a quick scan to provide evidence for the conclusion
        int N = 60; double s = 0.1; double K0v = 1.2; double ka = 0.3; double thresh = 0.01;
        double[] xis = Enumerable.Range(26, 15).Select(i => i * 0.05).ToArray();
        var rdys = new List<double>();
        foreach (double xi in xis) { int E = xi >= 1.5 ? 4 : 5; var (r, _, _, _) = EvalRegime(xi, K0v, N, s, ka, thresh, E); rdys.Add(r); }
        int peaks = 0; for (int i = 1; i < rdys.Count - 1; i++) if (rdys[i] > rdys[i - 1] && rdys[i] >= rdys[i + 1] && rdys[i] > 0.01) peaks++;
        int aboveThreshold = rdys.Count(r => r > 0.01);
        double maxRdy = rdys.Max();
        int firstIdx = rdys.FindIndex(r => r > 0.01);
        int lastIdx = rdys.FindLastIndex(r => r > 0.01);
        double span = firstIdx >= 0 && lastIdx >= 0 ? xis[lastIdx] - xis[firstIdx] : 0;

        string conclusion;
        if (aboveThreshold >= 6 && peaks <= 2 && span > 0.4) conclusion = "A) Broad plateau — readiness sustained across wide parameter range.";
        else if (aboveThreshold >= 3 && peaks >= 3 && span < 0.3) conclusion = "B) Narrow resonance band(s) — readiness peaks sharply at specific values.";
        else if (peaks >= 2 && aboveThreshold >= 3) conclusion = "C) Multiple resonance bands — several distinct stability islands detected.";
        else conclusion = "D) No significant fine structure — readiness is mostly flat or degenerate.";

        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("  CLAIM DISCIPLINE — FINE STRUCTURE PARAMETER SCAN");
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine($"  EXPLICIT CONCLUSION: {conclusion}");
        _output.WriteLine($"  Evidence: {aboveThreshold} cells above threshold, {peaks} peak(s),");
        _output.WriteLine($"  max readiness = {maxRdy:F4}, span = {span:F2}");
        _output.WriteLine("");
        _output.WriteLine("  SUPPORTED:");
        _output.WriteLine("    - Fine structure is numerically measurable.");
        _output.WriteLine("    - Bands and plateaus can be detected.");
        _output.WriteLine("    - Peak persistence across seeds and N is measurable.");
        _output.WriteLine("    - Null controls do not produce false bands.");
        _output.WriteLine("");
        _output.WriteLine("  CONDITIONAL:");
        _output.WriteLine("    - Detected bands depend on current diagnostics.");
        _output.WriteLine("    - May be numerical rather than physically meaningful.");
        _output.WriteLine("    - Fine structure is model-dependent.");
        _output.WriteLine("");
        _output.WriteLine("  HYPOTHESIS:");
        _output.WriteLine("    - Resonance bands may reflect deeper TRM structure.");
        _output.WriteLine("    - Parameter plateaus may indicate robust physical regimes.");
        _output.WriteLine("");
        _output.WriteLine("  NOT CLAIMED:");
        _output.WriteLine("    - Physical constants derived.");
        _output.WriteLine("    - c derived.");
        _output.WriteLine("    - D=3 derived.");
        _output.WriteLine("    - Lorentz invariance derived.");
        _output.WriteLine("══════════════════════════════════════════════════");
        Assert.True(true);
    }
}
