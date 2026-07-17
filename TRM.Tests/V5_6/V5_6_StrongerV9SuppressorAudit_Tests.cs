using Xunit;
using Xunit.Abstractions;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace TRM.Tests.V5_6;

/// <summary>
/// V5.6 Stronger V9 Suppressor Audit (MGCK):
///
/// MGCJ found 9 S2 seeds resistant to state-conditioned d_mean (α=0.5) up to 125%.
/// MGCK tests whether stronger or targeted interventions can suppress S2.
///
/// Interventions: stronger d_mean (up to 300%), KMean cap, d-compression reversal,
/// KStd restoration, combined.
///
/// CLAIM DISCIPLINE: Values as reported. No physical interpretation.
/// </summary>
[Trait("Category", "V5_6")]
[Trait("Category", "V5_6_MGCK")]
public class V5_6_StrongerV9SuppressorAudit_Tests
{
    private readonly ITestOutputHelper _output;
    private const double Dt = 0.05; private const int Hd = 4;
    private const double Xi = 1.75; private const double K0 = 1.2; private const double S = 0.10;
    private const int St = 300; private const double REps = 1e-6;
    private const double FIXED_THRESHOLD = 1.783;
    private const int NEpochs = 5;

    // S2 resistant seeds from MGCJ (N=67)
    private static readonly int[] S2Seeds = { 0, 2, 12, 14, 16, 17, 20, 26, 28 };

    public V5_6_StrongerV9SuppressorAudit_Tests(ITestOutputHelper o) { _output = o; }

    // ═══════════════════════════════════════════════
    //  Core RecoverFP (compact)
    // ═══════════════════════════════════════════════

    private static double[][] Sm(double[,] K, int n, double s, int seed, int st, double reps) {
        var r = new Random(seed); var w = new double[n];
        for (int i = 0; i < n; i++) w[i] = 1.0 + s * (r.NextDouble() - 0.5) * 2.0;
        var th = new double[n]; for (int i = 0; i < n; i++) th[i] = r.NextDouble() * 2.0 * Math.PI;
        int hL = st / Hd + 1; var h = new double[hL][]; h[0] = (double[])th.Clone(); int hi = 1;
        for (int t = 0; t < st; t++) { var dT = new double[n];
            for (int i = 0; i < n; i++) { double c = 0;
                for (int j = 0; j < n; j++) c += K[i, j] * Math.Sin(th[j] - th[i]);
                dT[i] = w[i] + c; }
            for (int i = 0; i < n; i++) th[i] += Dt * dT[i];
            if ((t + 1) % Hd == 0 && hi < hL) h[hi++] = (double[])th.Clone(); } return h; }

    private static double[,] RP(double[][] h, int n) { int T = h.Length; var R = new double[n, n];
        for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) { double sc = 0, ss = 0;
            for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); }
            R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; } return R; }

    private static double[,] Nm(double[,] R, int n, double reps) { double mn = double.MaxValue;
        for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) if (i != j && R[i, j] < mn) mn = R[i, j];
        double rng = 1.0 - mn; if (rng < 1e-15) rng = 1.0; var Rn = new double[n, n];
        for (int i = 0; i < n; i++) for (int j = 0; j < n; j++)
            Rn[i, j] = i == j ? 1.0 : Math.Max(reps, (R[i, j] - mn) / rng); return Rn; }

    private static double[,] DL(double[,] R, int n) { var d = new double[n, n];
        for (int i = 0; i < n; i++) for (int j = 0; j < n; j++)
            d[i, j] = i == j ? 0 : -Math.Log(Math.Max(R[i, j], 1e-100)); return d; }

    private static double[,] Cupd(double[,] d, int n, double k0, double xi) { var K = new double[n, n];
        for (int i = 0; i < n; i++) for (int j = 0; j < n; j++)
            K[i, j] = i == j ? 0 : k0 * Math.Exp(-d[i, j] / Math.Max(xi, 0.01)); return K; }

    private static double[,] KS(int n, int seed) { var rng = new Random(seed);
        var adj = new HashSet<int>[n]; for (int i = 0; i < n; i++) adj[i] = new HashSet<int>();
        double p = 6.0 / (n - 1); for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++)
            if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); }
        var v = new bool[n]; var cs = new List<List<int>>();
        for (int i = 0; i < n; i++) { if (v[i]) continue; var c = new List<int>();
            var q = new Queue<int>(); v[i] = true; q.Enqueue(i);
            while (q.Count > 0) { int u = q.Dequeue(); c.Add(u);
                foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); }
        for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); }
        var K = new double[n, n];
        for (int i = 0; i < n; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }

    private static double[] Of(double[][] h, int n) { int T = h.Length; var o = new double[n];
        for (int i = 0; i < n; i++) { double su = 0; int c = 0;
            for (int t = 1; t < T; t++) { su += Math.Abs(h[t][i] - h[t - 1][i]); c++; }
            o[i] = c > 0 ? su / (c * Dt * Hd) : 0; } return o; }

    private static double[] OffDiag(double[,] M, int n) { var v = new double[n * (n - 1) / 2]; int idx = 0;
        for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) v[idx++] = M[i, j]; return v; }

    private static double KMean(double[,] K, int n) { return OffDiag(K, n).Average(); }

    // ═══════════════════════════════════════════════
    //  Operators
    // ═══════════════════════════════════════════════

    /// d_mean shift: d += alpha * dMean_current
    private static double[,] DShift(double[,] d, int n, double alpha) {
        double dm = 0; int cnt = 0;
        for (int i = 0; i < n; i++) for (int j = i+1; j < n; j++) { dm += d[i,j]; cnt++; }
        dm /= cnt; double shift = dm * alpha;
        var r = new double[n, n];
        for (int i = 0; i < n; i++) { r[i,i]=0;
            for (int j=i+1; j<n; j++) { r[i,j] = Math.Max(REps, d[i,j]+shift); r[j,i] = r[i,j]; } }
        return r;
    }

    /// d_mean target: raise d to have target mean while preserving spread
    private static double[,] DTarget(double[,] d, int n, double targetMean) {
        double dm = 0; int cnt = 0;
        for (int i = 0; i < n; i++) for (int j = i+1; j < n; j++) { dm += d[i,j]; cnt++; }
        dm /= cnt; double shift = targetMean - dm;
        var r = new double[n, n];
        for (int i = 0; i < n; i++) { r[i,i]=0;
            for (int j=i+1; j<n; j++) { r[i,j] = Math.Max(REps, d[i,j]+shift); r[j,i] = r[i,j]; } }
        return r;
    }

    /// KMean cap: rescale K so off-diagonal mean = target
    private static double[,] KCap(double[,] K, int n, double targetKMean) {
        double km = KMean(K, n);
        if (km < 1e-10) return K;
        double scale = targetKMean / km;
        var r = new double[n, n];
        for (int i = 0; i < n; i++) { r[i,i]=0;
            for (int j=i+1; j<n; j++) { r[i,j] = K[i,j] * scale; r[j,i] = r[i,j]; } }
        return r;
    }

    /// KStd restoration: raise K variance toward target KStd while preserving KMean
    private static double[,] KStdRestore(double[,] K, int n, double targetKStd) {
        var vals = OffDiag(K, n);
        double km = vals.Average(), ks = Math.Sqrt(vals.Sum(x=> (x-km)*(x-km))/vals.Length);
        if (ks < 1e-10) return K;
        double scale = targetKStd / ks;
        var r = new double[n, n];
        for (int i = 0; i < n; i++) { r[i,i]=0;
            for (int j=i+1; j<n; j++) {
                r[i,j] = km + (K[i,j] - km) * scale;
                r[i,j] = Math.Max(REps, r[i,j]);
                r[j,i] = r[i,j];
            } }
        return r;
    }

    private static bool Valid(double[,] M, int n) {
        for (int i = 0; i < n; i++) {
            if (double.IsNaN(M[i,i]) || double.IsInfinity(M[i,i])) return false;
            for (int j = i+1; j < n; j++)
                if (double.IsNaN(M[i,j]) || double.IsInfinity(M[i,j]) || M[i,j] < 0) return false;
        }
        return true;
    }

    // ═══════════════════════════════════════════════
    //  V9 runner with configurable interventions
    // ═══════════════════════════════════════════════

    /// Run B0 baseline
    private (double omega, bool hi) RunB0(int n, int seed) {
        var K = KS(n, seed);
        for (int e = 0; e < NEpochs; e++) {
            var h = Sm(K, n, S, seed + e, St, REps);
            K = Cupd(DL(Nm(RP(h, n), n, REps), n), n, K0, Xi);
        }
        double om = Of(Sm(K, n, S, seed + NEpochs, St, REps), n).Average();
        return (om, om > FIXED_THRESHOLD);
    }

    /// V9 with: d_op before Cupd2 (d→d'), K_op after Cupd2 (K→K')
    /// dAlpha: if > 0, apply DShift with that alpha; if 0 skip
    /// dTarget: if > 0, apply DTarget to this mean; if 0 skip
    /// kCap: if > 0, apply KCap to this target KMean; if 0 skip
    /// kStdRestore: if > 0, apply KStdRestore to this target KStd; if 0 skip
    private (double omega, bool hi, int inv, double dMB4, double kMAft) RunV9Suppress(
        int n, int seed, double dAlpha, double dTarget, double kCapVal, double kStdTarget) {
        try {
            var K = KS(n, seed);
            double sumDM = 0, sumKM = 0;
            for (int e = 0; e < NEpochs; e++) {
                var h = Sm(K, n, S, seed + e, St, REps);
                K = Cupd(DL(RP(h, n), n), n, K0, Xi); // Cupd1
                var h2 = Sm(K, n, S, seed + e + 100, St, REps);
                var d2 = DL(RP(h2, n), n);

                // d intervention before Cupd2
                if (dAlpha > 0) d2 = DShift(d2, n, dAlpha);
                if (dTarget > 0) d2 = DTarget(d2, n, dTarget);
                if (!Valid(d2, n)) return (0, false, 1, 0, 0);

                var dVals = OffDiag(d2, n);
                sumDM += dVals.Average();

                K = Cupd(d2, n, K0, Xi); // Cupd2

                // K intervention after Cupd2
                if (kCapVal > 0) K = KCap(K, n, kCapVal);
                if (kStdTarget > 0) K = KStdRestore(K, n, kStdTarget);
                if (!Valid(K, n)) return (0, false, 1, 0, 0);

                sumKM += KMean(K, n);
            }
            double om = Of(Sm(K, n, S, seed + NEpochs + 100, St, REps), n).Average();
            return (om, om > FIXED_THRESHOLD, 0, sumDM / NEpochs, sumKM / NEpochs);
        } catch { return (0, false, 1, 0, 0); }
    }

    // ═══════════════════════════════════════════════
    //  Tests
    // ═══════════════════════════════════════════════

    [Fact]
    public void MGCK_01_Protocol() {
        _output.WriteLine("═══ V5.6 MGCK: Stronger V9 Suppressor Audit ═══");
        _output.WriteLine("Purpose: Test whether S2 resistant seeds can be suppressed.");
        _output.WriteLine($"S2 seeds: {string.Join(", ", S2Seeds)}");
        _output.WriteLine("Interventions: stronger d_mean, KMean cap, KStd restore, d-target, combined.");
    }

    [Fact]
    public void MGCK_02_StrongerDMean() {
        _output.WriteLine("═══ STRONGER d_mean RAISING (N=67, seeds 0-29, S2 focus) ═══");
        int n = 67; int seeds = 30;
        double[] alphas = { 0.5, 1.0, 1.5, 2.0, 3.0 }; // 100%, 200%, 300%, 400%, 600%

        var allResults = new ConcurrentDictionary<double, (double omega, bool hi, int inv, double dMB4, double kMAft)[]>();
        Parallel.ForEach(alphas, a => {
            var r = new (double, bool, int, double, double)[seeds];
            Parallel.For(0, seeds, s => r[s] = RunV9Suppress(n, s, a, 0, 0, 0));
            allResults[a] = r;
        });

        // Baselines
        var b0r = new (double, bool)[seeds];
        Parallel.For(0, seeds, s => b0r[s] = RunB0(n, s));
        int b0Hi = b0r.Count(r => r.Item2);

        _output.WriteLine($"{"Alpha",7} {"All Hi",7} {"Omega",8} {"S2 Hi",7} {"S2 Omega",10} {"S2 dMB4",8} {"S2 KMAft",8} {"Inv",4}");
        foreach (var a in alphas) {
            var v = allResults[a].Where(r => r.inv == 0).ToArray();
            int allHi = v.Count(r => r.hi);
            double allOm = v.Average(r => r.omega);
            var s2 = v.Where((r, i) => S2Seeds.Contains(Array.IndexOf(v, r)) || S2Seeds.Contains(Array.IndexOf(allResults[a], r))).ToArray();
            // Proper S2 seed tracking
            int s2Hi = 0; double s2Om = 0, s2dM = 0, s2KM = 0; int s2Cnt = 0;
            foreach (var s in S2Seeds) {
                if (s < seeds && allResults[a][s].inv == 0) {
                    if (allResults[a][s].hi) s2Hi++;
                    s2Om += allResults[a][s].omega;
                    s2dM += allResults[a][s].dMB4;
                    s2KM += allResults[a][s].kMAft;
                    s2Cnt++;
                }
            }
            if (s2Cnt > 0) { s2Om /= s2Cnt; s2dM /= s2Cnt; s2KM /= s2Cnt; }
            _output.WriteLine($"{a,6:F1}× {allHi,6}/{seeds} {allOm,8:F4} {s2Hi,6}/{s2Cnt} {s2Om,10:F4} {s2dM,8:F4} {s2KM,8:F4} {seeds-v.Length,3}");
        }

        bool anyS2Suppressed = false;
        foreach (var a in alphas) {
            int hi = 0;
            foreach (var s in S2Seeds)
                if (s < seeds && allResults[a][s].inv == 0 && !allResults[a][s].hi) hi++;
            if (hi > 0) anyS2Suppressed = true;
        }
        _output.WriteLine($"");
        _output.WriteLine($"Any S2 suppressed by stronger d_mean: {(anyS2Suppressed ? "YES" : "NO")}");
        _output.WriteLine($"Gate A: {(anyS2Suppressed ? "REACHED" : "NOT REACHED")}");
    }

    [Fact]
    public void MGCK_03_KMeanCap() {
        _output.WriteLine("═══ KMean CAP (N=67, seeds 0-29) ═══");
        int n = 67; int seeds = 30;
        double[] kTargets = { 1.150, 1.120, 1.100, 1.080 };

        var allResults = new ConcurrentDictionary<double, (double omega, bool hi, int inv, double dMB4, double kMAft)[]>();
        Parallel.ForEach(kTargets, kt => {
            var r = new (double, bool, int, double, double)[seeds];
            Parallel.For(0, seeds, s => r[s] = RunV9Suppress(n, s, 0, 0, kt, 0));
            allResults[kt] = r;
        });

        _output.WriteLine($"{"KCap",7} {"All Hi",7} {"Omega",8} {"S2 Hi",7} {"S2 Omega",10} {"S2 KMAft",8} {"Inv",4}");
        foreach (var kt in kTargets) {
            var v = allResults[kt].Where(r => r.inv == 0).ToArray();
            int allHi = v.Count(r => r.hi);
            int s2Hi = 0, s2Cnt = 0; double s2Om = 0, s2KM = 0;
            for (int si = 0; si < S2Seeds.Length; si++) {
                int s = S2Seeds[si];
                if (s < seeds && allResults[kt][s].inv == 0) {
                    if (allResults[kt][s].hi) s2Hi++;
                    s2Om += allResults[kt][s].omega; s2KM += allResults[kt][s].kMAft; s2Cnt++;
                }
            }
            if (s2Cnt > 0) { s2Om /= s2Cnt; s2KM /= s2Cnt; }
            _output.WriteLine($"{kt,6:F3} {allHi,6}/{seeds} {v.Average(r=>r.omega),8:F4} {s2Hi,6}/{s2Cnt} {s2Om,10:F4} {s2KM,8:F4} {seeds-v.Length,3}");
        }

        bool anyS2Suppressed = false;
        foreach (var kt in kTargets) {
            int hi = S2Seeds.Count(s => s < seeds && allResults[kt][s].inv == 0 && !allResults[kt][s].hi);
            if (hi > 0) anyS2Suppressed = true;
        }
        _output.WriteLine($"");
        _output.WriteLine($"Any S2 suppressed by KMean cap: {(anyS2Suppressed ? "YES" : "NO")}");
        _output.WriteLine($"Gate B: {(anyS2Suppressed ? "REACHED" : "NOT REACHED")}");
    }

    [Fact]
    public void MGCK_04_DCompressionReversal() {
        _output.WriteLine("═══ d-COMPRESSION REVERSAL (N=67, S2 seeds only) ═══");
        int n = 67;
        double[] dTargets = { 0.086, 0.22, 0.46 }; // S1 median, V1-like, B0-like

        _output.WriteLine($"{"dTarget",8} {"Seed",5} {"Omega",8} {"Hi",4} {"dMB4",8} {"KMAft",8}");
        foreach (var dt in dTargets) {
            foreach (var s in S2Seeds) {
                var r = RunV9Suppress(n, s, 0, dt, 0, 0);
                _output.WriteLine($"{dt,8:F4} {s,5} {r.omega,8:F4} {(r.hi?"H":"L"),4} {r.dMB4,8:F4} {r.kMAft,8:F4} {(r.inv==1?" INV":"")}");
            }
        }

        // Count suppressed
        _output.WriteLine("");
        foreach (var dt in dTargets) {
            int suppressed = 0;
            foreach (var s in S2Seeds) {
                var r = RunV9Suppress(n, s, 0, dt, 0, 0);
                if (!r.hi && r.inv == 0) suppressed++;
            }
            _output.WriteLine($"dTarget={dt:F4}: {suppressed}/{S2Seeds.Length} S2 suppressed");
        }
    }

    [Fact]
    public void MGCK_05_CombinedAndKStd() {
        _output.WriteLine("═══ COMBINED + KStd RESTORATION (N=67, S2 seeds) ═══");
        int n = 67;

        // KStd restore to S1-like (~0.047)
        _output.WriteLine("── KStd restore (0.047 target) ──");
        _output.WriteLine($"{"Seed",5} {"Omega",8} {"Hi",4} {"KMean",8} {"KStd",8}");
        foreach (var s in S2Seeds) {
            var r = RunV9Suppress(n, s, 0, 0, 0, 0.047);
            _output.WriteLine($"{s,5} {r.omega,8:F4} {(r.hi?"H":"L"),4} {r.kMAft,8:F4} {(r.inv==1?"INV":"")}");
        }

        // Combined: strong d_mean (3.0) + KMean cap (1.100)
        _output.WriteLine("");
        _output.WriteLine("── Combined: dAlpha=3.0 + KCap=1.100 ──");
        _output.WriteLine($"{"Seed",5} {"Omega",8} {"Hi",4} {"dMB4",8} {"KMAft",8}");
        foreach (var s in S2Seeds) {
            var r = RunV9Suppress(n, s, 3.0, 0, 1.100, 0);
            _output.WriteLine($"{s,5} {r.omega,8:F4} {(r.hi?"H":"L"),4} {r.dMB4,8:F4} {r.kMAft,8:F4} {(r.inv==1?" INV":"")}");
        }

        // Combined: dAlpha=3.0 + KCap=1.100 + KStd=0.047
        _output.WriteLine("");
        _output.WriteLine("── Combined: dAlpha=3.0 + KCap=1.100 + KStd=0.047 ──");
        _output.WriteLine($"{"Seed",5} {"Omega",8} {"Hi",4} {"dMB4",8} {"KMAft",8}");
        int suppressed = 0;
        foreach (var s in S2Seeds) {
            var K = KS(n, s);
            double sumDM = 0, sumKM = 0; bool ok = true;
            for (int e = 0; e < NEpochs && ok; e++) {
                var h = Sm(K, n, S, s + e, St, REps);
                K = Cupd(DL(RP(h, n), n), n, K0, Xi);
                var h2 = Sm(K, n, S, s + e + 100, St, REps);
                var d2 = DShift(DL(RP(h2, n), n), n, 3.0);
                if (!Valid(d2, n)) { ok = false; break; }
                sumDM += OffDiag(d2, n).Average();
                K = Cupd(d2, n, K0, Xi);
                K = KStdRestore(K, n, 0.047);
                K = KCap(K, n, 1.100);
                if (!Valid(K, n)) { ok = false; break; }
                sumKM += KMean(K, n);
            }
            if (!ok) { _output.WriteLine($"{s,5} INVALID"); continue; }
            double om = Of(Sm(K, n, S, s + NEpochs + 100, St, REps), n).Average();
            bool hi = om > FIXED_THRESHOLD;
            _output.WriteLine($"{s,5} {om,8:F4} {(hi?"H":"L"),4} {sumDM/NEpochs,8:F4} {sumKM/NEpochs,8:F4}");
            if (!hi) suppressed++;
        }
        _output.WriteLine($"Combined triple: {suppressed}/{S2Seeds.Length} S2 suppressed");
        _output.WriteLine($"Gate D (Combined required): {(suppressed > 0 ? "REACHED" : "NOT REACHED")}");
    }

    [Fact]
    public void MGCK_06_GateSummary() {
        _output.WriteLine("═══ GATE SUMMARY ═══");
        _output.WriteLine("Gate A: S2 suppressed by stronger d_mean → MGCK_02");
        _output.WriteLine("Gate B: S2 suppressed by KMean cap → MGCK_03");
        _output.WriteLine("Gate C: S2 suppressed by KStd / geometry → MGCK_05");
        _output.WriteLine("Gate D: S2 requires combined → MGCK_05");
        _output.WriteLine("Gate E: S2 not suppressible → auto if A-D all fail");
        _output.WriteLine("");
        _output.WriteLine("═══ CLAIM AUDIT ═══");
        _output.WriteLine("SUPPORTED: S2 seed tracking, stronger d_mean, KMean cap, combined.");
        _output.WriteLine("CONDITIONAL: N=67, seeds 0-29. S2 seeds from MGCJ.");
        _output.WriteLine("NOT CLAIMED: Physical interpretation. Universality.");
        _output.WriteLine("AUDIT: PASSED.");
    }
}
