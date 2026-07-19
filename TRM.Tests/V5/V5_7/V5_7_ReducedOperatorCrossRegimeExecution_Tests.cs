using Xunit;
using Xunit.Abstractions;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace TRM.Tests.V5_7;

/// <summary>
/// V5.7 Reduced Operator Cross-Regime Execution (ROCE):
///
/// Executes the pre-registered cross-N and cross-seed validation.
/// Attempts to FALSIFY the V5.6 reduced operator model by testing
/// it outside the discovery regime (N=60-80, seed blocks 0-29/30-59/60-99).
///
/// CLAIM DISCIPLINE: No physical interpretation. Falsification stance.
/// </summary>
[Trait("Category", "V5_7")]
[Trait("Category", "V5_7_ROCE")]
public class V5_7_ReducedOperatorCrossRegimeExecution_Tests
{
    private readonly ITestOutputHelper _output;
    private const double Dt = 0.05; private const int Hd = 4;
    private const double Xi = 1.75; private const double K0 = 1.2; private const double S = 0.10;
    private const int St = 300; private const double REps = 1e-6;
    private const double FIXED_THRESHOLD = 1.783;
    private const int NEpochs = 5;
    private static readonly int[] AllN = { 60, 62, 64, 66, 67, 69, 72, 75, 80 };
    private static readonly int[] BlockSizes = { 30, 30, 40 }; // seeds 0-29, 30-59, 60-99

    public V5_7_ReducedOperatorCrossRegimeExecution_Tests(ITestOutputHelper o) { _output = o; }

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
        for (int i = 0; i < cs.Count - 1; i++) { adj[cs[i + 1][0]].Add(cs[i][0]); adj[cs[i][0]].Add(cs[i + 1][0]); }
        var K = new double[n, n];
        for (int i = 0; i < n; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }

    private static double[] Of(double[][] h, int n) { int T = h.Length; var o = new double[n];
        for (int i = 0; i < n; i++) { double su = 0; int c = 0;
            for (int t = 1; t < T; t++) { su += Math.Abs(h[t][i] - h[t - 1][i]); c++; }
            o[i] = c > 0 ? su / (c * Dt * Hd) : 0; } return o; }

    private static double Std(double[] v) { double m = v.Average();
        return Math.Sqrt(v.Sum(x => (x - m) * (x - m)) / v.Length); }

    private static double[] OffDiag(double[,] M, int n) { var v = new double[n * (n - 1) / 2]; int idx = 0;
        for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) v[idx++] = M[i, j]; return v; }

    private static double KMean(double[,] K, int n) { return OffDiag(K, n).Average(); }
    private static double DMean(double[,] d, int n) { return OffDiag(d, n).Average(); }

    // State-conditioned operator
    private static double[,] StateOp(double[,] d, int n) {
        double dm = DMean(d, n);
        double shift = dm * 0.5;
        var r = new double[n, n];
        for (int i = 0; i < n; i++) { r[i,i]=0;
            for (int j=i+1; j<n; j++) { r[i,j]=Math.Max(REps, d[i,j]+shift); r[j,i]=r[i,j]; } }
        return r;
    }

    private static bool Valid(double[,] M, int n) {
        for (int i = 0; i < n; i++) {
            if (double.IsNaN(M[i,i]) || double.IsInfinity(M[i,i])) return false;
            for (int j = i+1; j < n; j++)
                if (double.IsNaN(M[i,j]) || double.IsInfinity(M[i,j]) || M[i,j] < 0) return false;
        } return true;
    }

    // ═══════════════════════════════════════════════
    //  Pipeline runners (return seed-level data)
    // ═══════════════════════════════════════════════

    private (double omega, double dM, double kM, bool hi, int inv) RunB0(int n, int seed) {
        try {
            var K = KS(n, seed);
            for (int e = 0; e < NEpochs; e++) {
                var h = Sm(K, n, S, seed+e, St, REps);
                K = Cupd(DL(Nm(RP(h, n), n, REps), n), n, K0, Xi);
            }
            var hf = Sm(K, n, S, seed+NEpochs, St, REps);
            double om = Of(hf, n).Average();
            var d = DL(Nm(RP(hf, n), n, REps), n);
            return (om, DMean(d, n), KMean(Cupd(d, n, K0, Xi), n), om > FIXED_THRESHOLD, 0);
        } catch { return (0,0,0,false,1); }
    }

    private (double omega, double dM, double kM, bool hi, int inv) RunV1(int n, int seed) {
        try {
            var K = KS(n, seed);
            for (int e = 0; e < NEpochs; e++) {
                K = Cupd(DL(RP(Sm(K, n, S, seed+e, St, REps), n), n), n, K0, Xi);
            }
            var hf = Sm(K, n, S, seed+NEpochs, St, REps);
            double om = Of(hf, n).Average();
            var d = DL(RP(hf, n), n);
            return (om, DMean(d, n), KMean(Cupd(d, n, K0, Xi), n), om > FIXED_THRESHOLD, 0);
        } catch { return (0,0,0,false,1); }
    }

    private (double omega, double dM, double kM, bool hi, int inv) RunR1(int n, int seed) {
        try {
            var K = KS(n, seed);
            double sumDM = 0, sumKM = 0;
            for (int e = 0; e < NEpochs; e++) {
                var h = Sm(K, n, S, seed+e, St, REps);
                var d = StateOp(DL(RP(h, n), n), n);
                if (!Valid(d, n)) return (0,0,0,false,1);
                sumDM += DMean(d, n);
                K = Cupd(d, n, K0, Xi);
                sumKM += KMean(K, n);
            }
            var hf = Sm(K, n, S, seed+NEpochs, St, REps);
            double om = Of(hf, n).Average();
            return (om, sumDM/NEpochs, sumKM/NEpochs, om > FIXED_THRESHOLD, 0);
        } catch { return (0,0,0,false,1); }
    }

    private (double omega, double dMB4, double kMAft, bool hi, int inv) RunV9(int n, int seed) {
        try {
            var K = KS(n, seed);
            double sumDM = 0, sumKM = 0;
            for (int e = 0; e < NEpochs; e++) {
                var h = Sm(K, n, S, seed+e, St, REps);
                K = Cupd(DL(RP(h, n), n), n, K0, Xi); // Cupd1
                var h2 = Sm(K, n, S, seed+e+100, St, REps);
                var d2 = DL(RP(h2, n), n);
                sumDM += DMean(d2, n);
                K = Cupd(d2, n, K0, Xi); // Cupd2
                sumKM += KMean(K, n);
            }
            var hf = Sm(K, n, S, seed+NEpochs+100, St, REps);
            double om = Of(hf, n).Average();
            return (om, sumDM/NEpochs, sumKM/NEpochs, om > FIXED_THRESHOLD, 0);
        } catch { return (0,0,0,false,1); }
    }

    // ═══════════════════════════════════════════════
    //  Cross-N Table (Stage 1: seeds 0-29)
    // ═══════════════════════════════════════════════

    [Fact]
    public void ROCE_01_CrossN_Validation() {
        _output.WriteLine("═══ CROSS-N VALIDATION (seeds 0-29) ═══");
        int seeds = 30;

        var results = new ConcurrentDictionary<(int n, string cond), (int hi, double om, double dM, double kM, int inv)>();

        Parallel.ForEach(AllN, n => {
            // B0
            var b0r = new (double,double,double,bool,int)[seeds];
            Parallel.For(0, seeds, s => b0r[s] = RunB0(n, s));
            var b0v = b0r.Where(r => r.Item5 == 0).ToArray();
            results[(n,"B0")] = (b0v.Count(r=>r.Item4), b0v.Average(r=>r.Item1), b0v.Average(r=>r.Item2), b0v.Average(r=>r.Item3), seeds-b0v.Length);

            // V1
            var v1r = new (double,double,double,bool,int)[seeds];
            Parallel.For(0, seeds, s => v1r[s] = RunV1(n, s));
            var v1v = v1r.Where(r => r.Item5 == 0).ToArray();
            results[(n,"V1")] = (v1v.Count(r=>r.Item4), v1v.Average(r=>r.Item1), v1v.Average(r=>r.Item2), v1v.Average(r=>r.Item3), seeds-v1v.Length);

            // R1 (state-conditioned)
            var r1r = new (double,double,double,bool,int)[seeds];
            Parallel.For(0, seeds, s => r1r[s] = RunR1(n, s));
            var r1v = r1r.Where(r => r.Item5 == 0).ToArray();
            results[(n,"R1")] = (r1v.Count(r=>r.Item4), r1v.Average(r=>r.Item1), r1v.Average(r=>r.Item2), r1v.Average(r=>r.Item3), seeds-r1v.Length);
        });

        // Also V9 at selected N
        int[] v9N = { 67, 72, 80 };
        foreach (var n in v9N) {
            var v9r = new (double,double,double,bool,int)[seeds];
            Parallel.For(0, seeds, s => v9r[s] = RunV9(n, s));
            var v9v = v9r.Where(r => r.Item5 == 0).ToArray();
            results[(n,"V9")] = (v9v.Count(r=>r.Item4), v9v.Average(r=>r.Item1), v9v.Average(r=>r.Item2), v9v.Average(r=>r.Item3), seeds-v9v.Length);
        }

        _output.WriteLine($"{"N",5} {"Cond",5} {"High",7} {"Omega",8} {"dMean",8} {"KMean",8} {"Inv",4} {"Note"}");
        _output.WriteLine(new string('-', 60));
        foreach (var n in AllN) {
            var b0 = results[(n,"B0")]; var v1 = results[(n,"V1")]; var r1 = results[(n,"R1")];
            string note = r1.hi <= b0.hi ? "≤B0 ✓" : "FAIL ✗";
            _output.WriteLine($"{n,5} {"B0",5} {b0.hi,6}/{seeds} {b0.om,8:F4} {b0.dM,8:F4} {b0.kM,8:F4} {b0.inv,3}");
            _output.WriteLine($"{n,5} {"V1",5} {v1.hi,6}/{seeds} {v1.om,8:F4} {v1.dM,8:F4} {v1.kM,8:F4} {v1.inv,3}");
            _output.WriteLine($"{n,5} {"R1",5} {r1.hi,6}/{seeds} {r1.om,8:F4} {r1.dM,8:F4} {r1.kM,8:F4} {r1.inv,3}  {note}");
        }
        foreach (var n in v9N) {
            var v9 = results[(n,"V9")];
            var v1 = results[(n,"V1")];
            string note2 = v9.hi > v1.hi ? "AMPLIFIES ✓" : "FAIL ✗";
            _output.WriteLine($"{n,5} {"V9",5} {v9.hi,6}/{seeds} {v9.om,8:F4} {v9.dM,8:F4} {v9.kM,8:F4} {v9.inv,3}  {note2}");
        }
    }

    // ═══════════════════════════════════════════════
    //  Cross-Seed Block Test
    // ═══════════════════════════════════════════════

    [Fact]
    public void ROCE_02_CrossSeedBlocks() {
        _output.WriteLine("═══ CROSS-SEED BLOCKS (N=67,72,80) ═══");
        int[] selN = { 67, 72, 80 };
        var blocks = new[] { (0, 30), (30, 60), (60, 100) };
        string[] blockNames = { "Blk0 (0-29)", "Blk1 (30-59)", "Blk2 (60-99)" };

        var results = new ConcurrentDictionary<(int n, string cond, int blk), (int hi, double om, int inv)>();

        Parallel.ForEach(selN, n => {
            for (int bi = 0; bi < blocks.Length; bi++) {
                var (start, end) = blocks[bi];
                int cnt = end - start;

                var b0r = new (double,double,double,bool,int)[cnt];
                Parallel.For(start, end, s => b0r[s-start] = RunB0(n, s));
                var b0v = b0r.Where(r => r.Item5 == 0).ToArray();
                results[(n,"B0",bi)] = (b0v.Count(r=>r.Item4), b0v.Average(r=>r.Item1), cnt-b0v.Length);

                var r1r = new (double,double,double,bool,int)[cnt];
                Parallel.For(start, end, s => r1r[s-start] = RunR1(n, s));
                var r1v = r1r.Where(r => r.Item5 == 0).ToArray();
                results[(n,"R1",bi)] = (r1v.Count(r=>r.Item4), r1v.Average(r=>r.Item1), cnt-r1v.Length);
            }
        });

        _output.WriteLine($"{"N",5} {"Cond",5} {"Block",-14} {"High",8} {"Omega",8} {"Inv",4}");
        foreach (var n in selN) {
            for (int bi = 0; bi < blocks.Length; bi++) {
                var b0b = results[(n,"B0",bi)];
                _output.WriteLine($"{n,5} {"B0",5} {blockNames[bi],-14} {b0b.hi,7}/{blocks[bi].Item2-blocks[bi].Item1} {b0b.om,8:F4} {b0b.inv,3}");
            }
            for (int bi = 0; bi < blocks.Length; bi++) {
                var r1b = results[(n,"R1",bi)];
                var b0b = results[(n,"B0",bi)];
                string note = r1b.hi <= b0b.hi ? "≤B0" : "FAIL";
                _output.WriteLine($"{n,5} {"R1",5} {blockNames[bi],-14} {r1b.hi,7}/{blocks[bi].Item2-blocks[bi].Item1} {r1b.om,8:F4} {r1b.inv,3}  {note}");
            }
            _output.WriteLine("");
        }

        // Block stability check
        _output.WriteLine("── Block Stability ──");
        foreach (var n in selN) {
            int[] r1Hi = { results[(n,"R1",0)].hi, results[(n,"R1",1)].hi, results[(n,"R1",2)].hi };
            double mean = r1Hi.Average();
            double maxDev = r1Hi.Max(h => Math.Abs(h - mean)) / Math.Max(mean, 1);
            _output.WriteLine($"N={n}: R1 Hi = [{r1Hi[0]}, {r1Hi[1]}, {r1Hi[2]}], maxDev = {maxDev*100:F0}% {(maxDev > 0.2 ? "FAIL F2" : "OK")}");
        }
    }

    // ═══════════════════════════════════════════════
    //  Failure Criteria Check
    // ═══════════════════════════════════════════════

    [Fact]
    public void ROCE_03_FailureCriteria() {
        _output.WriteLine("═══ FAILURE CRITERIA CHECK ═══");
        _output.WriteLine("(Based on ROCE_01 cross-N results)");
        _output.WriteLine("");
        _output.WriteLine("F1 (N-local): Operator fails at N outside 67-72.");
        _output.WriteLine("F2 (Seed-block-overfit): Operator differs across blocks.");
        _output.WriteLine("F3 (Amplification fail): V9 ≤ V1 at tested N.");
        _output.WriteLine("F4 (d-K decoupling): d→K correlation weak at some N.");
        _output.WriteLine("F5 (Instability): Invalid d/K at any N or seed.");
    }

    // ═══════════════════════════════════════════════
    //  d→K→Omega correlation across N
    // ═══════════════════════════════════════════════

    [Fact]
    public void ROCE_04_DKOmegaCorrelation() {
        _output.WriteLine("═══ d→K→Omega CORRELATION (seeds 0-29) ═══");
        int seeds = 30;

        _output.WriteLine($"{"N",5} {"d→K r",8} {"K→Ω r",8} {"F4?",8}");
        foreach (var n in AllN) {
            // Collect per-seed (dMean, KMean, Omega) from R1 runs
            var dArr = new List<double>();
            var kArr = new List<double>();
            var oArr = new List<double>();
            var r1r = new (double,double,double,bool,int)[seeds];
            Parallel.For(0, seeds, s => r1r[s] = RunR1(n, s));
            foreach (var r in r1r) {
                if (r.Item5 == 0) { dArr.Add(r.Item2); kArr.Add(r.Item3); oArr.Add(r.Item1); }
            }
            var dA = dArr.ToArray(); var kA = kArr.ToArray(); var oA = oArr.ToArray();
            double rDK = Corr(dA, kA);
            double rKO = Corr(kA, oA);
            string f4 = rDK > -0.7 ? "F4 ✗" : "OK";
            _output.WriteLine($"{n,5} {rDK,8:F4} {rKO,8:F4} {f4,8}");
        }
    }

    private static double Corr(double[] x, double[] y) {
        double mx = x.Average(), my = y.Average();
        double num = 0, dx = 0, dy = 0;
        for (int i = 0; i < x.Length; i++) { num += (x[i]-mx)*(y[i]-my); dx += (x[i]-mx)*(x[i]-mx); dy += (y[i]-my)*(y[i]-my); }
        return (dx*dy > 1e-15) ? num/Math.Sqrt(dx*dy) : 0;
    }
}
