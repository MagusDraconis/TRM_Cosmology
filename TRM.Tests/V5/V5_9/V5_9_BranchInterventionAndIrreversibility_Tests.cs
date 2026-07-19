using Xunit;
using Xunit.Abstractions;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace TRM.Tests.V5_9;

/// <summary>
/// V5.9 Branch Intervention and Irreversibility (BCI):
///
/// BCE/BCA showed branches are NOT committed at epochs 1-4.
/// BCI tests whether they become committed at epoch 5.
///
/// Key question: Can epoch 5 d_mean intervention still flip the branch?
///
/// CLAIM DISCIPLINE: Irreversibility ≠ causality. No physical interpretation.
/// </summary>
[Trait("Category", "V5_9")]
[Trait("Category", "V5_9_BCI")]
public class V5_9_BranchInterventionAndIrreversibility_Tests
{
    private readonly ITestOutputHelper _output;
    private const double Dt = 0.05; private const int Hd = 4;
    private const double Xi = 1.75; private const double K0 = 1.2; private const double S = 0.10;
    private const int St = 300; private const double REps = 1e-6;
    private const double FIXED_THRESHOLD = 1.783;
    private const int NEpochs = 5;
    private static readonly int[] AllN = { 66, 67, 69, 70, 71, 72, 75, 80 };

    public V5_9_BranchInterventionAndIrreversibility_Tests(ITestOutputHelper o) { _output = o; }

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
    private static double DMean(double[,] d, int n) { double s=0; int c=0;
        for (int i = 0; i < n; i++) for (int j = i+1; j < n; j++) { s+=d[i,j]; c++; } return c>0?s/c:0; }

    private static double[,] DShiftUp(double[,] d, int n) {
        double dm = DMean(d, n); double shift = dm * 0.5;
        var r = new double[n, n];
        for (int i = 0; i < n; i++) { r[i,i]=0;
            for (int j=i+1; j<n; j++) { r[i,j]=Math.Max(REps, d[i,j]+shift); r[j,i]=r[i,j]; } }
        return r;
    }

    // Run baseline (no intervention)
    private (bool hi, double om) RunBaseline(int n, int seed) {
        var K = KS(n, seed);
        for (int e = 0; e < NEpochs; e++) {
            var h = Sm(K, n, S, seed+e, St, REps);
            K = Cupd(DL(Nm(RP(h, n), n, REps), n), n, K0, Xi);
        }
        double om = Of(Sm(K, n, S, seed+NEpochs, St, REps), n).Average();
        return (om > FIXED_THRESHOLD, om);
    }

    // Intervention at last epoch (epoch 4 = 5th epoch, 0-indexed)
    private (bool hi, double om, bool inv) RunEpoch5Intervention(int n, int seed) {
        try {
            var K = KS(n, seed);
            for (int e = 0; e < NEpochs; e++) {
                var h = Sm(K, n, S, seed+e, St, REps);
                var d = DL(Nm(RP(h, n), n, REps), n);
                if (e == 4) { // intervention at epoch 5
                    d = DShiftUp(d, n);
                    for (int i=0;i<n;i++){if(double.IsNaN(d[i,i])||double.IsInfinity(d[i,i]))return(false,0,true);
                        for(int j=i+1;j<n;j++)if(double.IsNaN(d[i,j])||double.IsInfinity(d[i,j])||d[i,j]<0)return(false,0,true);}
                }
                K = Cupd(d, n, K0, Xi);
            }
            double om = Of(Sm(K, n, S, seed+NEpochs, St, REps), n).Average();
            return (om > FIXED_THRESHOLD, om, false);
        } catch { return (false, 0, true); }
    }

    // ═══════════════════════════════════════════════
    //  CP5 (Epoch 5) commitment vs BCE CP4
    // ═══════════════════════════════════════════════

    [Fact]
    public void BCI_01_CP5_Commitment() {
        _output.WriteLine("═══ CP5 (EPOCH 5) COMMITMENT: d_mean increase at epoch 5 (seeds 0-99) ═══");
        int seeds = 100;

        var results = new ConcurrentDictionary<int, (int baseHi, int flipHiLo, int flipLoHi, int inv)>();

        Parallel.ForEach(AllN, n => {
            var bl = new (bool hi, double om)[seeds];
            var iv = new (bool hi, double om, bool inv)[seeds];
            Parallel.Invoke(
                () => Parallel.For(0, seeds, s => bl[s] = RunBaseline(n, s)),
                () => Parallel.For(0, seeds, s => iv[s] = RunEpoch5Intervention(n, s))
            );
            int baseHi = bl.Count(b => b.hi);
            int flipHiLo=0, flipLoHi=0, invalids=0;
            for (int s = 0; s < seeds; s++) {
                if (iv[s].inv) { invalids++; continue; }
                if (bl[s].hi && !iv[s].hi) flipHiLo++;
                else if (!bl[s].hi && iv[s].hi) flipLoHi++;
            }
            results[n] = (baseHi, flipHiLo, flipLoHi, invalids);
        });

        // BCE CP4 flip rates for comparison (from BCE_01 output)
        var bce4 = new Dictionary<int, double> {
            [66]=2, [67]=15, [69]=28, [70]=27, [71]=45, [72]=46, [75]=29, [80]=7
        };

        _output.WriteLine($"{"N",5} {"BaseHi",8} {"CP4 Flip%",10} {"CP5 Flip%",10} {"Δ(5-4)",10} {"CP5 Commit%",12} {"Soft?",6} {"Direction"}");
        foreach (var n in AllN) {
            if (!results.ContainsKey(n)) continue;
            var r = results[n];
            int totalFlips = r.flipHiLo + r.flipLoHi;
            int valid = seeds - r.inv;
            double flip5 = valid > 0 ? totalFlips * 100.0 / valid : 0;
            double flip4 = bce4.GetValueOrDefault(n, 0);
            double delta = flip5 - flip4;
            string soft = flip5 < 10 ? "YES ✓" : "NO";
            string dir = r.flipHiLo > r.flipLoHi ? "H→L" : r.flipLoHi > r.flipHiLo ? "L→H" : "equal";
            _output.WriteLine($"{n,5} {r.baseHi,7}/{seeds} {flip4,9:F0}% {flip5,9:F0}% {delta,10:F0}% {100-flip5,11:F0}% {soft,6} {dir}");
        }

        // Gate check
        _output.WriteLine("");
        int softAt5 = results.Keys.Count(n => {
            var r = results[n]; int v = seeds - r.inv;
            return v > 0 && (r.flipHiLo + r.flipLoHi) * 100.0 / v < 10;
        });
        _output.WriteLine($"Soft commitment at CP5: {softAt5}/{AllN.Length} N values.");
        _output.WriteLine($"Gate A (Epoch 5 commitment): {(softAt5 >= AllN.Length * 0.6 ? "REACHED" : "NOT REACHED — most N still plastic")}");
    }

    // ═══════════════════════════════════════════════
    //  N=71 CP5 special
    // ═══════════════════════════════════════════════

    [Fact]
    public void BCI_02_N71_CP5() {
        _output.WriteLine("═══ N=71 CP5 DETAILED (seeds 0-99) ═══");
        int n = 71; int seeds = 100;

        var bl = new (bool hi, double om)[seeds];
        Parallel.For(0, seeds, s => bl[s] = RunBaseline(n, s));

        var iv = new (bool hi, double om, bool inv)[seeds];
        Parallel.For(0, seeds, s => iv[s] = RunEpoch5Intervention(n, s));

        _output.WriteLine($"N=71 baseline: {bl.Count(b=>b.hi)} Hi, {bl.Count(b=>!b.hi)} Lo");
        int flipHiLo=0, flipLoHi=0, inv=0;
        var flipped = new List<int>();
        for (int s=0; s<seeds; s++) {
            if (iv[s].inv) { inv++; continue; }
            if (bl[s].hi && !iv[s].hi) { flipHiLo++; flipped.Add(s); }
            else if (!bl[s].hi && iv[s].hi) { flipLoHi++; flipped.Add(s); }
        }
        double flipPct = (flipHiLo+flipLoHi)*100.0/(seeds-inv);
        _output.WriteLine($"CP5 intervention: {flipHiLo} H→L, {flipLoHi} L→H, flip={flipPct:F1}%");
        _output.WriteLine($"Flipped seeds: [{string.Join(",", flipped.Take(20))}{(flipped.Count>20?"...":"")}]");

        // Compare with BCE epoch 4 for N=71 (45%)
        _output.WriteLine($"BCE CP4 flip: 45% → BCI CP5 flip: {flipPct:F1}% — {(flipPct<45?"↓ DECREASED":"↑ INCREASED or equal")}");
    }

    [Fact]
    public void BCI_03_GateSummary() {
        _output.WriteLine("═══ GATE SUMMARY ═══");
        _output.WriteLine("Gate A (Epoch 5 commitment): BCI_01 — soft commit at CP5?");
        _output.WriteLine("Gate B (Post-final reversible): NOT TESTED (protocol limitation)");
        _output.WriteLine("Gate C (Directional commitment): BCI_01 — H→L vs L→H");
        _output.WriteLine("Gate D (N=71 persistent plasticity): BCI_02");
        _output.WriteLine("Gate E (Intervention-specific): single intervention tested");
        _output.WriteLine("Gate F (No irreversibility): auto if no N committed");
        _output.WriteLine("");
        _output.WriteLine("═══ CLAIM AUDIT ═══");
        _output.WriteLine("SUPPORTED: CP5 flip rates, CP4 vs CP5 comparison, N=71 CP5.");
        _output.WriteLine("CONDITIONAL: d_mean increase only. Seeds 0-99.");
        _output.WriteLine("NOT CLAIMED: Physical interpretation, universality, causality.");
    }
}
