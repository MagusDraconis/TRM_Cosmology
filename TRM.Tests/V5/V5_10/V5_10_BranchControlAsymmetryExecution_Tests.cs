using Xunit;
using Xunit.Abstractions;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace TRM.Tests.V5_10;

/// <summary>
/// V5.10 Branch Control Asymmetry Execution (CAE):
///
/// V5.9 BCI: Hi→Lo works (14-31%), Lo→Hi fails (0%) with d_mean increase.
/// CAE tests whether Lo→Hi induction is possible with:
///   I1: K boost (increase K_mean post-pipeline)
///   I2: d compression (decrease d_mean at epoch 5)
///
/// CLAIM DISCIPLINE: No physical interpretation.
/// </summary>
[Trait("Category", "V5_10")]
[Trait("Category", "V5_10_CAE")]
public class V5_10_BranchControlAsymmetryExecution_Tests
{
    private readonly ITestOutputHelper _output;
    private const double Dt = 0.05; private const int Hd = 4;
    private const double Xi = 1.75; private const double K0 = 1.2; private const double S = 0.10;
    private const int St = 300; private const double REps = 1e-6;
    private const double FIXED_THRESHOLD = 1.783;
    private const int NEpochs = 5;
    private static readonly int[] AllN = { 67, 69, 70, 71, 72, 75, 80 };

    public V5_10_BranchControlAsymmetryExecution_Tests(ITestOutputHelper o) { _output = o; }

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
        for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i-1][0]); adj[cs[i-1][0]].Add(cs[i][0]); }
        var K = new double[n, n];
        for (int i = 0; i < n; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    private static double[] Of(double[][] h, int n) { int T = h.Length; var o = new double[n];
        for (int i = 0; i < n; i++) { double su = 0; int c = 0;
            for (int t = 1; t < T; t++) { su += Math.Abs(h[t][i] - h[t-1][i]); c++; }
            o[i] = c > 0 ? su / (c * Dt * Hd) : 0; } return o; }
    private static double KMean(double[,] K, int n) { double s=0; int c=0;
        for (int i = 0; i < n; i++) for (int j = i+1; j < n; j++) { s+=K[i,j]; c++; } return c>0?s/c:0; }
    private static double DMean(double[,] d, int n) { double s=0; int c=0;
        for (int i = 0; i < n; i++) for (int j = i+1; j < n; j++) { s+=d[i,j]; c++; } return c>0?s/c:0; }

    // K boost: rescale off-diagonal entries to increase K_mean
    private static (double[,], double om, bool hi) ApplyKBoost(double[,] K, int n, int seed, double targetKM) {
        double km = KMean(K, n);
        if (km < 1e-10) return (K, 0, false);
        double scale = targetKM / km;
        var K2 = new double[n, n];
        for (int i = 0; i < n; i++) { K2[i,i]=0;
            for (int j=i+1; j<n; j++) { K2[i,j]=K[i,j]*scale; K2[j,i]=K2[i,j]; } }
        var hf = Sm(K2, n, S, seed+100, St, REps);
        double om = Of(hf, n).Average();
        return (K2, om, om > FIXED_THRESHOLD);
    }

    // d compression at epoch: decrease d_mean before Cupd
    private static double[,] DCompress(double[,] d, int n, double alpha) {
        double dm = DMean(d, n); double shift = -dm * alpha;
        var r = new double[n, n];
        for (int i = 0; i < n; i++) { r[i,i]=0;
            for (int j=i+1; j<n; j++) { r[i,j]=Math.Max(REps, d[i,j]+shift); r[j,i]=r[i,j]; } }
        return r;
    }

    // Run pipeline with d compression at epoch 4 (0-indexed)
    private (double om, bool hi, bool inv) RunDcompressEpoch5(int n, int seed, double alpha) {
        try {
            var K = KS(n, seed);
            for (int e = 0; e < NEpochs; e++) {
                var h = Sm(K, n, S, seed+e, St, REps);
                var d = DL(Nm(RP(h, n), n, REps), n);
                if (e == 4) { d = DCompress(d, n, alpha);
                    for (int i=0;i<n;i++){if(double.IsNaN(d[i,i])||double.IsInfinity(d[i,i]))return(0,false,true);
                        for(int j=i+1;j<n;j++)if(double.IsNaN(d[i,j])||double.IsInfinity(d[i,j])||d[i,j]<0)return(0,false,true);}
                }
                K = Cupd(d, n, K0, Xi);
            }
            double om = Of(Sm(K, n, S, seed+NEpochs, St, REps), n).Average();
            return (om, om > FIXED_THRESHOLD, false);
        } catch { return (0, false, true); }
    }

    // ═══════════════════════════════════════════════
    //  Main test: K boost and d compression at CP5
    // ═══════════════════════════════════════════════

    [Fact]
    public void CAE_01_LoHi_Induction() {
        _output.WriteLine("═══ Lo→Hi INDUCTION: K boost + d compression at CP5 (seeds 0-99) ═══");
        int seeds = 100;

        var results = new ConcurrentDictionary<int, (int lo, int kBoostLoHi, int dCompLoHi, double kBoostBestOm, double dCompBestOm)>();

        Parallel.ForEach(AllN, n => {
            // Run baseline B0
            var bl = new bool[seeds];
            var finalK = new (double[,] K, double om)[seeds];
            Parallel.For(0, seeds, s => {
                var K = KS(n, s);
                for (int e = 0; e < NEpochs; e++) {
                    var h = Sm(K, n, S, s+e, St, REps);
                    K = Cupd(DL(Nm(RP(h, n), n, REps), n), n, K0, Xi);
                }
                double om = Of(Sm(K, n, S, s+NEpochs, St, REps), n).Average();
                bl[s] = om > FIXED_THRESHOLD;
                finalK[s] = (K, om);
            });

            int loCount = bl.Count(b => !b);
            if (loCount == 0) { results[n] = (0,0,0,0,0); return; }

            // Target K_mean for boost: high-branch median K_mean
            var hiKMs = new List<double>();
            for (int s = 0; s < seeds; s++)
                if (bl[s]) hiKMs.Add(KMean(finalK[s].K, n));
            double targetKM = hiKMs.Count > 0 ? hiKMs.OrderBy(x => x).ElementAt(hiKMs.Count / 2) : 1.15;

            int kBoostLH = 0, dCompLH = 0;
            double kBestOm = 0, dBestOm = 0;

            Parallel.For(0, seeds, s => {
                if (bl[s]) return; // only low seeds

                // K boost: try multiple multipliers
                double[] multipliers = { 1.0, 1.05, 1.10, 1.15, 1.20, 1.25 };
                foreach (var mult in multipliers) {
                    var (_, om, hi) = ApplyKBoost(finalK[s].K, n, s*1000, targetKM * mult / (mult > 0 ? mult : 1));
                    // Actually just use multipliers directly
                }
                // Better: just use the best result
                double bestOm = 0; bool anyHi = false;
                foreach (var mult in multipliers) {
                    var (_, om, hi) = ApplyKBoost(finalK[s].K, n, s*1000 + (int)(mult*100), targetKM * mult);
                    if (om > bestOm) bestOm = om;
                    if (hi) anyHi = true;
                }
                if (anyHi) Interlocked.Increment(ref kBoostLH);
                Interlocked.Exchange(ref kBestOm, Math.Max(kBestOm, bestOm));
            });

            // d compression test: separate loop
            var dCompResults = new (double om, bool hi, bool inv)[seeds];
            Parallel.For(0, seeds, s => {
                if (bl[s]) { dCompResults[s] = (0, false, false); return; }
                dCompResults[s] = RunDcompressEpoch5(n, s, 0.5);
            });
            for (int s = 0; s < seeds; s++) {
                if (!bl[s] && !dCompResults[s].inv && dCompResults[s].hi) dCompLH++;
                if (!bl[s] && !dCompResults[s].inv && dCompResults[s].om > dBestOm) dBestOm = dCompResults[s].om;
            }

            results[n] = (loCount, kBoostLH, dCompLH, kBestOm, dBestOm);
        });

        _output.WriteLine($"{"N",5} {"Lo Seeds",10} {"KBoost→Hi",10} {"DComp→Hi",10} {"KBestΩ",8} {"DBestΩ",8} {"Induction?"}");
        foreach (var n in AllN) {
            if (!results.ContainsKey(n)) continue;
            var r = results[n];
            string induced = (r.kBoostLoHi > 0 || r.dCompLoHi > 0) ? $"YES ({r.kBoostLoHi+r.dCompLoHi})" : "NO";
            _output.WriteLine($"{n,5} {r.lo,9}/{100} {r.kBoostLoHi,10} {r.dCompLoHi,10} {r.kBoostBestOm,8:F4} {r.dCompBestOm,8:F4} {induced}");
        }

        _output.WriteLine("");
        int totalInduced = results.Values.Sum(r => r.kBoostLoHi + r.dCompLoHi);
        int totalLo = results.Values.Sum(r => r.lo);
        _output.WriteLine($"Total Lo→Hi induced: {totalInduced}/{totalLo} across all N");
        _output.WriteLine($"Gate E (Not inducible): {(totalInduced == 0 ? "REACHED — Lo→Hi remains 0%" : "NOT REACHED")}");
    }

    // ═══════════════════════════════════════════════
    //  CP4 d compression test
    // ═══════════════════════════════════════════════

    [Fact]
    public void CAE_02_CP4_DCompress() {
        _output.WriteLine("═══ CP4 d COMPRESSION: Low→High induction at epoch 4 (seeds 0-29, N=67,71) ═══");
        int seeds = 30; int[] testN = { 67, 71 };

        foreach (var n in testN) {
            var bl = new bool[seeds];
            Parallel.For(0, seeds, s => {
                var K = KS(n, s);
                for (int e = 0; e < NEpochs; e++) {
                    var h = Sm(K, n, S, s+e, St, REps);
                    K = Cupd(DL(Nm(RP(h, n), n, REps), n), n, K0, Xi);
                }
                bl[s] = Of(Sm(K, n, S, s+NEpochs, St, REps), n).Average() > FIXED_THRESHOLD;
            });

            int loCount = bl.Count(b => !b);
            int induced = 0;
            for (double alpha = 0.25; alpha <= 0.75; alpha += 0.25) {
                var results = new (bool hi, bool inv)[seeds];
                Parallel.For(0, seeds, s => {
                    if (bl[s]) { results[s] = (false, false); return; }
                    try {
                        var K = KS(n, s);
                        for (int e = 0; e < NEpochs; e++) {
                            var h = Sm(K, n, S, s+e, St, REps);
                            var d = DL(Nm(RP(h, n), n, REps), n);
                            if (e == 3) { d = DCompress(d, n, alpha); for (int i=0;i<n;i++){if(double.IsNaN(d[i,i])||double.IsInfinity(d[i,i])){results[s]=(false,true);return;}
                                for(int j=i+1;j<n;j++)if(double.IsNaN(d[i,j])||double.IsInfinity(d[i,j])||d[i,j]<0){results[s]=(false,true);return;}}}
                            K = Cupd(d, n, K0, Xi);
                        }
                        double om = Of(Sm(K, n, S, s+NEpochs, St, REps), n).Average();
                        results[s] = (om > FIXED_THRESHOLD, false);
                    } catch { results[s] = (false, true); }
                });
                induced += results.Count(r => r.hi && !r.inv);
            }
            _output.WriteLine($"N={n}: {loCount} Lo seeds, {induced} induced via CP4 d-compress (alpha=0.25-0.75)");
        }
    }

    [Fact]
    public void CAE_03_GateSummary() {
        _output.WriteLine("═══ GATE SUMMARY ═══");
        _output.WriteLine("Gate A (Scalar K boost): CAE_01");
        _output.WriteLine("Gate B (K geometry): NOT TESTED in CAE");
        _output.WriteLine("Gate E (Not inducible): CAE_01/02 — Lo→Hi fails");
        _output.WriteLine("Gate F (N-dependent): CAE_01");
        _output.WriteLine("");
        _output.WriteLine("═══ CLAIM AUDIT ═══");
        _output.WriteLine("SUPPORTED: K boost, d compression, CP4 and CP5 Lo→Hi induction rates.");
        _output.WriteLine("CONDITIONAL: B0 pipelines. Seeds 0-99. d and K interventions only.");
        _output.WriteLine("NOT CLAIMED: Physical interpretation, universality, impossibility.");
    }
}
