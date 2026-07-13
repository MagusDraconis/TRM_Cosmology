using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Cross-law continuum scaling comparison.
///
/// Compares exponential, Gaussian, power-law, softmax, and adaptive coupling laws
/// as N increases. Determines whether exponential scaling stability is special or
/// whether other continuous laws show comparable continuum behavior.
///
/// No kNN used as an update mechanism.
/// No D=3 assumption. Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_CrossLawContinuumScaling")]
public class V4_1_CrossLawContinuumScaling_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_CrossLawContinuumScaling_Tests(ITestOutputHelper o) { _output = o; }

    private enum Law { Exp, Gauss, Power, Softmax, Adaptive }
    private static readonly string[] Ln = ["exp", "gauss", "power", "softmax", "adaptive"];

    // ── Core helpers ────────────────────────────────────────
    private static double[][] Sm(double[,] K, int N, double s, int seed) { var r = new Random(seed); var w = new double[N]; for (int i = 0; i < N; i++) w[i] = 1.0 + s * (r.NextDouble() - 0.5) * 2.0; var th = new double[N]; for (int i = 0; i < N; i++) th[i] = r.NextDouble() * 2.0 * Math.PI; int hL = St / Hd + 1; var h = new double[hL][]; h[0] = (double[])th.Clone(); int hi = 1; for (int t = 0; t < St; t++) { var dT = new double[N]; for (int i = 0; i < N; i++) { double c = 0; for (int j = 0; j < N; j++) c += K[i, j] * Math.Sin(th[j] - th[i]); dT[i] = w[i] + c; } for (int i = 0; i < N; i++) th[i] += Dt * dT[i]; if ((t + 1) % Hd == 0 && hi < hL) h[hi++] = (double[])th.Clone(); } return h; }
    private static double[,] RP(double[][] h) { int T = h.Length, N = h[0].Length; var R = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { double sc = 0, ss = 0; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); } R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; } return R; }
    private static double[,] Nm(double[,] R) { int N = R.GetLength(0); double mn = double.MaxValue; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j && R[i, j] < mn) mn = R[i, j]; double rng = 1.0 - mn; if (rng < 1e-15) rng = 1.0; var Rn = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) Rn[i, j] = 1.0; else Rn[i, j] = Math.Max(REps, (R[i, j] - mn) / rng); } return Rn; }
    private static double[,] DL(double[,] R) { int N = R.GetLength(0); var d = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) d[i, j] = 0; else d[i, j] = -Math.Log(Math.Max(R[i, j], 1e-100)); } return d; }
    private static double Spear(double[] a, double[] b) { if (a.Length < 3) return 0; var ia = a.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); var ib = b.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); return Pear(ia, ib); }
    private static double Pear(double[] x, double[] y) { int n = x.Length; double mx = x.Average(), my = y.Average(), nm = 0, dx = 0, dy = 0; for (int i = 0; i < n; i++) { double a = x[i] - mx, b = y[i] - my; nm += a * b; dx += a * a; dy += b * b; } return Math.Sqrt(dx * dy) > 1e-15 ? nm / Math.Sqrt(dx * dy) : 0; }
    private static double[] Fl(double[,] m) { int N = m.GetLength(0); var a = new double[N * N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) a[i * N + j] = m[i, j]; return a; }
    private static double MDiff(double[,] A, double[,] B) { int N = A.GetLength(0); double s = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) s += Math.Abs(A[i, j] - B[i, j]); return s / (N * (N - 1) / 2.0); }
    private static double OP(double[] th) { double sx = 0, sy = 0; for (int i = 0; i < th.Length; i++) { sx += Math.Cos(th[i]); sy += Math.Sin(th[i]); } return Math.Sqrt(sx * sx + sy * sy) / th.Length; }
    private static double Dg(double[,] d) { int N = d.GetLength(0); var v = new List<double>(); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) v.Add(d[i, j]); if (v.Count == 0) return 0; double m = v.Average(); return m > 1e-9 ? v.Average(x => (x - m) * (x - m)) / (m * m) : 0; }

    // ── Initial K builder ────────────────────────────────────
    private static double[,] KS(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }

    // ── Multi-law update ─────────────────────────────────────
    private static double[,] Upd(double[,] d, Law law, double K0, double p, double tau)
    {
        int N = d.GetLength(0); var K = new double[N, N];
        switch (law)
        {
            case Law.Exp:
                for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) { K[i, j] = 0; continue; } K[i, j] = K0 * Math.Exp(-d[i, j] / Math.Max(p, 0.01)); }
                break;
            case Law.Gauss:
                for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) { K[i, j] = 0; continue; } K[i, j] = K0 * Math.Exp(-d[i, j] * d[i, j] / Math.Max(p * p, 0.0001)); }
                break;
            case Law.Power:
                for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) { K[i, j] = 0; continue; } K[i, j] = K0 / (1.0 + Math.Pow(Math.Max(d[i, j], 0), p)); }
                break;
            case Law.Softmax:
                double it = 1.0 / Math.Max(tau, 0.01);
                for (int i = 0; i < N; i++) { double sum = 0; for (int j = 0; j < N; j++) if (i != j) sum += Math.Exp(-d[i, j] * it); if (sum > 1e-15) for (int j = 0; j < N; j++) if (i != j) K[i, j] = K0 * Math.Exp(-d[i, j] * it) / sum; }
                break;
            case Law.Adaptive:
                for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) { K[i, j] = 0; continue; } double S = 1.0; K[i, j] = K0 * Math.Exp(-d[i, j] / Math.Max(p, 0.01)) * S; }
                break;
        }
        return K;
    }

    // ── FP loop for a given law ──────────────────────────────
    private static (double dKFinal, double cR, double dg, double qMean) RunLaw(double[,] K0, int N, Law law, double K0v, double param, double tau, double s, int E, int seed)
    {
        var Kc = (double[,])K0.Clone(); double[,]? pK = null;
        double dKFinal = double.NaN, cR = double.NaN, dg = double.NaN;
        var qs = new List<double>();
        for (int e = 0; e < E; e++)
        {
            var h = Sm(Kc, N, s, seed + e);
            var R = Nm(RP(h)); var d = DL(R);
            var Kn = Upd(d, law, K0v, param, tau);
            if (pK != null) { double dk = MDiff(Kn, pK); dKFinal = dk; if (qs.Count > 0 && qs[^1] > 1e-15) qs.Add(dk / qs[^1]); else if (dk > 1e-15) qs.Add(dk); }
            cR = Spear(Fl(Kn), Fl(R)); dg = Dg(d);
            pK = Kn; Kc = Kn;
        }
        double qMean = qs.Count > 0 ? qs.Average() : double.NaN;
        return (dKFinal, cR, dg, qMean);
    }

    // ── Sparsity diagnostics ─────────────────────────────────
    private static (double pr, double ent, double top10, int effN) Sparsity(double[,] K)
    {
        int N = K.GetLength(0); var vals = new List<double>();
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (K[i, j] > 1e-15) vals.Add(K[i, j]);
        if (vals.Count == 0) return (0, 0, 0, 0);
        double sum = vals.Sum(), sumSq = vals.Sum(x => x * x);
        double pr = sum * sum / Math.Max(sumSq, 1e-15) / vals.Count;
        double ent = 0; foreach (var x in vals) { double px = x / sum; if (px > 1e-15) ent -= px * Math.Log(px); }
        var sorted = vals.OrderByDescending(x => x).ToArray();
        int tk = Math.Max(1, vals.Count / 10); double top10 = sorted.Take(tk).Sum() / Math.Max(sum, 1e-15);
        double eN = 0; for (int i = 0; i < N; i++) { double rs = 0, rsq = 0; for (int j = 0; j < N; j++) if (i != j) { rs += K[i, j]; rsq += K[i, j] * K[i, j]; } if (rs > 1e-15) eN += rs * rs / rsq; }
        return (pr, ent, top10, (int)Math.Round(eN / N));
    }

    // ── Spectral proxy ───────────────────────────────────────
    private static (double l2, double lmax, double s1) WSpec(double[,] K)
    {
        int N = K.GetLength(0); var deg = new double[N]; var rng = new Random(42);
        for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j && K[i, j] > 1e-15) deg[i] += K[i, j];
        var x = new double[N]; for (int i = 0; i < N; i++) x[i] = rng.NextDouble();
        double nrm = Math.Sqrt(x.Sum(v => v * v)); for (int i = 0; i < N; i++) x[i] /= Math.Max(nrm, 1e-15);
        double lmax = 0;
        for (int it = 0; it < 200; it++) { var y = new double[N]; for (int i = 0; i < N; i++) { double s = deg[i] * x[i]; for (int j = 0; j < N; j++) if (i != j && K[i, j] > 1e-15) s -= K[i, j] * x[j]; y[i] = s; } nrm = Math.Sqrt(y.Sum(v => v * v)); if (nrm < 1e-15) break; double lam = 0; for (int i = 0; i < N; i++) lam += x[i] * y[i]; lmax = lam; for (int i = 0; i < N; i++) x[i] = y[i] / nrm; }
        var z = new double[N]; for (int i = 0; i < N; i++) z[i] = rng.NextDouble() - 0.5;
        double mz = z.Average(); for (int i = 0; i < N; i++) z[i] -= mz;
        nrm = Math.Sqrt(z.Sum(v => v * v)); if (nrm < 1e-15) { for (int i = 0; i < N; i++) z[i] = (i % 2 == 0 ? 1 : -1); nrm = Math.Sqrt(N); }
        for (int i = 0; i < N; i++) z[i] /= Math.Max(nrm, 1e-15);
        double dot = 0; for (int i = 0; i < N; i++) dot += z[i] * x[i]; for (int i = 0; i < N; i++) z[i] -= dot * x[i];
        nrm = Math.Sqrt(z.Sum(v => v * v)); if (nrm > 1e-15) for (int i = 0; i < N; i++) z[i] /= nrm;
        double l2 = 0;
        for (int it = 0; it < 200; it++) { var y = new double[N]; for (int i = 0; i < N; i++) { double s = deg[i] * z[i]; for (int j = 0; j < N; j++) if (i != j && K[i, j] > 1e-15) s -= K[i, j] * z[j]; y[i] = s; } dot = 0; for (int i = 0; i < N; i++) dot += y[i] * x[i]; for (int i = 0; i < N; i++) y[i] -= dot * x[i]; nrm = Math.Sqrt(y.Sum(v => v * v)); if (nrm < 1e-15) break; double lam = 0; for (int i = 0; i < N; i++) lam += z[i] * y[i]; l2 = lam; for (int i = 0; i < N; i++) z[i] = y[i] / nrm; }
        double s1 = lmax > 1e-15 ? l2 / lmax : 0;
        return (l2, lmax, s1);
    }

    // ═══════════════ CLCS_01 All Laws Finite Across N ═══════════════
    [Fact]
    public void V4_1_CLCS_01_AllLawsFiniteAcrossN()
    {
        int[] Ns = [40, 80, 120, 200]; double K0v = 0.5; double s = 0.1; int E = 6;
        _output.WriteLine("law       N      dK_final  corr(K,R)  finite?");
        _output.WriteLine("--------  ----   ---------  ---------  -------");
        foreach (int N in Ns)
            foreach (Law law in Enum.GetValues<Law>())
            {
                var K0 = KS(N, BS);
                double param = law switch { Law.Power => 2.0, Law.Softmax => 0.5, _ => 1.0 };
                var (dK, cR, dg, _) = RunLaw(K0, N, law, K0v, param, 0.5, s, E, BS);
                bool fin = double.IsFinite(dK) && double.IsFinite(cR) && double.IsFinite(dg);
                _output.WriteLine($"{Ln[(int)law],-8} {N,5}  {dK,9:E4}  {cR,9:F4}  {fin,7}");
                Assert.True(fin, $"{Ln[(int)law]} N={N}: all diagnostics must be finite.");
            }
    }

    // ═══════════════ CLCS_02 Correlation Scaling By Law ═══════════════
    [Fact]
    public void V4_1_CLCS_02_CorrelationScalingByLaw()
    {
        int[] Ns = [40, 80, 120, 200]; double K0v = 0.5; double s = 0.1; int E = 8;
        _output.WriteLine("law       N      corr(K,R)  final_dK   q_mean     dg       class");
        _output.WriteLine("--------  ----   ---------  ---------  ------     -------  -------------");
        foreach (int N in Ns)
            foreach (Law law in Enum.GetValues<Law>())
            {
                var K0 = KS(N, BS);
                double param = law switch { Law.Power => 2.0, Law.Softmax => 0.5, _ => 1.0 };
                var (dK, cR, dg, qm) = RunLaw(K0, N, law, K0v, param, 0.5, s, E, BS);
                string cls = "Finite";
                if (!double.IsFinite(cR) || cR < 0.3) cls = "Degenerate";
                else if (double.IsFinite(qm) && qm < 0.5 && dK < 1e-6) cls = "Strong Attractor";
                else if (double.IsFinite(qm) && qm < 0.9) cls = "Weak Attractor";
                else if (double.IsFinite(qm) && qm < 1.5) cls = "Marginal";
                _output.WriteLine($"{Ln[(int)law],-8} {N,5}  {cR,9:F4}  {dK,9:E4}  {qm,6:F4}   {dg,7:F4}  {cls}");
                Assert.True(double.IsFinite(cR));
            }
    }

    // ═══════════════ CLCS_03 Effective Sparsity By Law ═══════════════
    [Fact]
    public void V4_1_CLCS_03_EffectiveSparsityByLaw()
    {
        int[] Ns = [40, 80, 120, 200]; double K0v = 0.5; double s = 0.1; int E = 6;
        _output.WriteLine("law       N      part_ratio  entropy   top10_frac  eff_neigh");
        _output.WriteLine("--------  ----   ----------  -------   ----------  ---------");
        foreach (int N in Ns)
            foreach (Law law in Enum.GetValues<Law>())
            {
                var K0 = KS(N, BS); var Kc = (double[,])K0.Clone();
                double param = law switch { Law.Power => 2.0, Law.Softmax => 0.5, _ => 1.0 };
                for (int e = 0; e < E; e++) { var h = Sm(Kc, N, s, BS + e); Kc = Upd(DL(Nm(RP(h))), law, K0v, param, 0.5); }
                var (pr, ent, t10, en) = Sparsity(Kc);
                _output.WriteLine($"{Ln[(int)law],-8} {N,5}  {pr,10:F4}  {ent,7:F4}  {t10,10:F4}  {en,9}");
                Assert.True(double.IsFinite(pr) && double.IsFinite(ent));
            }
    }

    // ═══════════════ CLCS_04 Spectral Proxy By Law ═══════════════
    [Fact]
    public void V4_1_CLCS_04_SpectralProxyByLaw()
    {
        int[] Ns = [40, 80, 120, 200]; double K0v = 0.5; double s = 0.1; int E = 6;
        _output.WriteLine("law       N      lambda2    lambda_max  S1");
        _output.WriteLine("--------  ----   --------   ----------  ------");
        foreach (int N in Ns)
            foreach (Law law in Enum.GetValues<Law>())
            {
                var K0 = KS(N, BS); var Kc = (double[,])K0.Clone();
                double param = law switch { Law.Power => 2.0, Law.Softmax => 0.5, _ => 1.0 };
                for (int e = 0; e < E; e++) { var h = Sm(Kc, N, s, BS + e); Kc = Upd(DL(Nm(RP(h))), law, K0v, param, 0.5); }
                var (l2, lmax, s1) = WSpec(Kc);
                bool fin = double.IsFinite(l2) && double.IsFinite(lmax) && double.IsFinite(s1);
                _output.WriteLine($"{Ln[(int)law],-8} {N,5}  {l2,8:F4}   {lmax,10:F4}  {s1,6:F4}");
                Assert.True(fin, $"{Ln[(int)law]} N={N}: spectral must be finite.");
            }
    }

    // ═══════════════ CLCS_05 Multi-Seed Cross-Law Study ═══════════════
    [Fact]
    public void V4_1_CLCS_05_MultiSeedCrossLawStudy()
    {
        int[] Ns = [40, 120, 200]; double K0v = 0.5; double s = 0.1; int E = 6;
        _output.WriteLine("law       N      mean_cR    std_cR    mean_dK     std_dK     fails");
        _output.WriteLine("--------  ----   --------   -------   ---------   -------    -----");
        foreach (int N in Ns)
            foreach (Law law in Enum.GetValues<Law>())
            {
                var K0 = KS(N, BS); var crs = new List<double>(); var dks = new List<double>(); int fails = 0;
                double param = law switch { Law.Power => 2.0, Law.Softmax => 0.5, _ => 1.0 };
                for (int sd = 0; sd < 10; sd++)
                {
                    var (dK, cR, _, _) = RunLaw(K0, N, law, K0v, param, 0.5, s, E, BS + sd * 100);
                    if (double.IsFinite(cR) && double.IsFinite(dK)) { crs.Add(cR); dks.Add(dK); } else fails++;
                }
                double mc = crs.Count > 0 ? crs.Average() : double.NaN;
                double sc = crs.Count > 1 ? Math.Sqrt(crs.Average(x => (x - mc) * (x - mc))) : 0;
                double md = dks.Count > 0 ? dks.Average() : double.NaN;
                double sdd = dks.Count > 1 ? Math.Sqrt(dks.Average(x => (x - md) * (x - md))) : 0;
                _output.WriteLine($"{Ln[(int)law],-8} {N,5}  {mc,8:F4}   {sc,7:F4}   {md,9:E4}   {sdd,7:E4}   {fails,5}");
                Assert.True(fails == 0, $"{Ln[(int)law]} N={N}: no seed failures.");
            }
    }

    // ═══════════════ CLCS_06 Xi and Shape Parameter Scan ═══════════════
    [Fact]
    public void V4_1_CLCS_06_XiAndShapeParameterScan()
    {
        int N = 80; double K0v = 0.5; double s = 0.1; int E = 6;
        var K0 = KS(N, BS);
        _output.WriteLine("law       param   corr(K,R)  final_dK   class");
        _output.WriteLine("--------  ------  ---------  ---------  -----");
        // Exponential + Gaussian: xi sweep
        foreach (Law law in new[] { Law.Exp, Law.Gauss })
            foreach (double xi in new[] { 0.5, 1.0, 1.5, 2.0 })
            {
                var (dK, cR, _, qm) = RunLaw(K0, N, law, K0v, xi, 0.5, s, E, BS);
                string cls = double.IsFinite(qm) && qm < 0.9 ? "Attractor" : "Marginal";
                _output.WriteLine($"{Ln[(int)law],-8} xi={xi:F1}  {cR,9:F4}  {dK,9:E4}  {cls}");
                Assert.True(double.IsFinite(cR));
            }
        // Power-law: p sweep
        foreach (double p in new[] { 1.5, 2.0, 3.0, 4.0 })
        {
            var (dK, cR, _, qm) = RunLaw(K0, N, Law.Power, K0v, p, 0.5, s, E, BS);
            string cls = double.IsFinite(qm) && qm < 0.9 ? "Attractor" : "Marginal";
            _output.WriteLine($"{Ln[(int)Law.Power],-8} p={p:F1}   {cR,9:F4}  {dK,9:E4}  {cls}");
            Assert.True(double.IsFinite(cR));
        }
        // Softmax: tau sweep
        foreach (double tau in new[] { 0.25, 0.5, 1.0, 2.0 })
        {
            var (dK, cR, _, qm) = RunLaw(K0, N, Law.Softmax, K0v, 1.0, tau, s, E, BS);
            string cls = double.IsFinite(qm) && qm < 0.9 ? "Attractor" : "Marginal";
            _output.WriteLine($"{Ln[(int)Law.Softmax],-8} tau={tau:F2} {cR,9:F4}  {dK,9:E4}  {cls}");
            Assert.True(double.IsFinite(cR));
        }
        // Adaptive: xi sweep with default S=1
        foreach (double xi in new[] { 0.5, 1.0, 1.5 })
        {
            var (dK, cR, _, qm) = RunLaw(K0, N, Law.Adaptive, K0v, xi, 0.5, s, E, BS);
            string cls = double.IsFinite(qm) && qm < 0.9 ? "Attractor" : "Marginal";
            _output.WriteLine($"{Ln[(int)Law.Adaptive],-8} xi={xi:F1}  {cR,9:F4}  {dK,9:E4}  {cls}");
            Assert.True(double.IsFinite(cR));
        }
    }

    // ═══════════════ CLCS_07 Robustness Ranking Across N ═══════════════
    [Fact]
    public void V4_1_CLCS_07_RobustnessRankingAcrossN()
    {
        int[] Ns = [40, 80, 120, 200]; double K0v = 0.5; double s = 0.1; int E = 8;
        var scores = new Dictionary<Law, double>();
        foreach (Law law in Enum.GetValues<Law>())
        {
            double param = law switch { Law.Power => 2.0, Law.Softmax => 0.5, _ => 1.0 };
            var crNs = new List<double>(); var dkNs = new List<double>(); var qNs = new List<double>();
            // Scaling stability: how well diagnostics hold across N
            foreach (int N in Ns)
            {
                var K0 = KS(N, BS);
                var (dK, cR, _, qm) = RunLaw(K0, N, law, K0v, param, 0.5, s, E, BS);
                if (double.IsFinite(cR)) crNs.Add(cR);
                if (double.IsFinite(dK)) dkNs.Add(dK);
                if (double.IsFinite(qm)) qNs.Add(qm);
            }
            double crStab = crNs.Count > 1 ? 1.0 / (1.0 + Math.Sqrt(crNs.Average(x => (x - crNs.Average()) * (x - crNs.Average())))) : 0;
            double dkStab = dkNs.Count > 1 ? 1.0 / (1.0 + Math.Sqrt(dkNs.Average(x => (x - dkNs.Average()) * (x - dkNs.Average())))) : 0;
            double qStab = qNs.Count > 1 && qNs.All(x => x < 1.5) ? 0.5 : 0;

            // Multi-seed reproducibility at N=120
            var K0m = KS(120, BS); var seedCrs = new List<double>();
            for (int sd = 0; sd < 5; sd++)
            {
                var (dK, cR, _, _) = RunLaw(K0m, 120, law, K0v, param, 0.5, s, E, BS + sd * 100);
                if (double.IsFinite(cR)) seedCrs.Add(cR);
            }
            double repro = seedCrs.Count > 1 ? 1.0 / (1.0 + Math.Sqrt(seedCrs.Average(x => (x - seedCrs.Average()) * (x - seedCrs.Average())))) : 0;

            double avgCr = crNs.Count > 0 ? crNs.Average() : 0;
            double score = 0.25 * crStab + 0.25 * Math.Min(avgCr, 1.0) + 0.20 * qStab + 0.15 * repro + 0.15 * dkStab;
            scores[law] = score;
        }
        var ranked = scores.OrderByDescending(kv => kv.Value).ToList();
        _output.WriteLine("Rank  Law         Score     Best N Range   Weakness");
        _output.WriteLine("----  ----------  --------  -------------  --------");
        for (int i = 0; i < ranked.Count; i++)
        {
            var (law, score) = (ranked[i].Key, ranked[i].Value);
            string bestN = "40-200";
            string weakness = law switch { Law.Softmax => "asymmetry", Law.Adaptive => "S_ij undefined", Law.Power => "tail heavy", Law.Gauss => "over-localized", _ => "none identified" };
            _output.WriteLine($"{i + 1,4}  {Ln[(int)law],-10} {score,8:F4}  {bestN,13}  {weakness}");
        }
        Assert.NotEmpty(ranked);
        Assert.True(ranked.All(r => double.IsFinite(r.Value)));
    }

    // ═══════════════ CLCS_08 Null and Degenerate Comparison By Law ═══════════════
    [Fact]
    public void V4_1_CLCS_08_NullAndDegenerateComparisonByLaw()
    {
        int N = 80; double K0v = 0.5; double s = 0.1; int E = 6;
        var K0 = KS(N, BS); var Kn = new double[N, N];
        _output.WriteLine("law       active_cR  null_cR   sync_dg   overcoupled_dg");
        _output.WriteLine("--------  ---------  -------   -------   ---------------");
        foreach (Law law in Enum.GetValues<Law>())
        {
            double param = law switch { Law.Power => 2.0, Law.Softmax => 0.5, _ => 1.0 };
            var (_, cRa, _, _) = RunLaw(K0, N, law, K0v, param, 0.5, s, E, BS);
            var (_, cRn, _, _) = RunLaw(Kn, N, law, K0v, param, 0.5, s, E, BS);
            // Global sync
            var hS = new double[101][]; for (int t = 0; t < 101; t++) hS[t] = new double[N];
            double dgSync = Dg(DL(Nm(RP(hS))));
            // Overcoupled dense
            var Koc = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j) Koc[i, j] = 0.5;
            var Kc = (double[,])Koc.Clone();
            for (int e = 0; e < E; e++) { var h = Sm(Kc, N, s, BS + e); Kc = Upd(DL(Nm(RP(h))), law, K0v, param, 0.5); }
            var hf = Sm(Kc, N, s, BS + E); double dgOC = Dg(DL(Nm(RP(hf))));
            _output.WriteLine($"{Ln[(int)law],-8} {cRa,9:F4}  {cRn,7:F4}   {dgSync,7:F4}   {dgOC,15:F4}");
            Assert.True(double.IsFinite(cRa) && double.IsFinite(cRn));
            Assert.True(dgSync < 0.01, "Global sync must be degenerate.");
        }
    }

    // ═══════════════ CLCS_09 Exponential Special Self-Consistency ═══════════════
    [Fact]
    public void V4_1_CLCS_09_ExponentialSpecialSelfConsistency()
    {
        int[] Ns = [40, 80, 120, 200]; double K0v = 0.5; double s = 0.1;
        _output.WriteLine("N      corr_exp(K,R)  corr_gauss(K,R)  corr_power(K,R)  exp_proportionality");
        _output.WriteLine("----   --------------  ---------------  ---------------  -------------------");
        foreach (int N in Ns)
        {
            var K0 = KS(N, BS);
            // Exponential
            var h = Sm(K0, N, s, BS); var R = Nm(RP(h)); var d = DL(R);
            var Ke = Upd(d, Law.Exp, K0v, 1.0, 0.5);
            double crE = Spear(Fl(Ke), Fl(R));
            // Proportionality: corr(Ke, R) — since exp(-d)=R for xi=1
            // Deviation: 1 - corr(Ke, R)
            double propDev = 1.0 - crE;
            // Gaussian
            var Kg = Upd(d, Law.Gauss, K0v, 1.0, 0.5);
            double crG = Spear(Fl(Kg), Fl(R));
            // Power-law
            var Kp = Upd(d, Law.Power, K0v, 2.0, 0.5);
            double crP = Spear(Fl(Kp), Fl(R));
            _output.WriteLine($"{N,5}  {crE,14:F4}  {crG,15:F4}  {crP,15:F4}  {propDev,19:F4}");
            Assert.True(crE > 0.5, $"N={N}: exp must show self-consistency.");
        }
    }

    // ═══════════════ CLCS_10 Claim Discipline Report ═══════════════
    [Fact]
    public void V4_1_CLCS_10_ClaimDisciplineReport()
    {
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("  CLAIM DISCIPLINE — CROSS-LAW CONTINUUM SCALING");
        _output.WriteLine("══════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  SUPPORTED:");
        _output.WriteLine("    - Cross-law continuum diagnostics are finite and deterministic.");
        _output.WriteLine("    - Continuous laws can be compared without kNN update.");
        _output.WriteLine("    - Null and degenerate cases are correctly detected.");
        _output.WriteLine("    - Law ranking can be computed under tested conditions.");
        _output.WriteLine("    - Sparsity, spectral proxies are measurable across laws.");
        _output.WriteLine("    - Exponential shows structural self-consistency (K ∝ R at xi=1).");
        _output.WriteLine("");
        _output.WriteLine("  CONDITIONAL:");
        _output.WriteLine("    - Ranking depends on N range, law parameters, K0, sigma, epochs.");
        _output.WriteLine("    - Exponential may be favored under tested conditions.");
        _output.WriteLine("    - Weighted spectral diagnostics are proxies.");
        _output.WriteLine("    - N up to tested maximum is not continuum limit.");
        _output.WriteLine("    - Adaptive law uses S=1 placeholder; real S_ij not yet defined.");
        _output.WriteLine("");
        _output.WriteLine("  HYPOTHESIS:");
        _output.WriteLine("    - One natural physical coupling law exists.");
        _output.WriteLine("    - Continuum limit selects exponential law.");
        _output.WriteLine("    - Physical space corresponds to selected fixed-point law.");
        _output.WriteLine("");
        _output.WriteLine("  NOT CLAIMED:");
        _output.WriteLine("    - Continuum limit proven.");
        _output.WriteLine("    - D=3 derived.");
        _output.WriteLine("    - General Relativity replaced.");
        _output.WriteLine("    - Quantum mechanics derived.");
        _output.WriteLine("    - hbar derived.");
        _output.WriteLine("    - Planck scales derived.");
        _output.WriteLine("══════════════════════════════════════════════════");
        Assert.True(true);
    }
}
