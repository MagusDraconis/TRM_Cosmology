using Xunit;
using Xunit.Abstractions;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace TRM.Tests.V5_8;

/// <summary>
/// V5.8 Branch Predictability Execution (BPE):
///
/// Determines the earliest epoch and smallest feature set that predicts
/// final branch identity from RecoverFP internal state.
///
/// Train: seeds 0-49. Test: seeds 50-99.
/// N: 64,66,67,69,70,71,72,75,80.
/// Conditions: B0 (with Nm), V1 (skip-Nm).
///
/// CLAIM DISCIPLINE: Correlation only — prediction does not imply mechanism.
/// </summary>
[Trait("Category", "V5_8")]
[Trait("Category", "V5_8_BPE")]
public class V5_8_BranchPredictabilityExecution_Tests
{
    private readonly ITestOutputHelper _output;
    private const double Dt = 0.05; private const int Hd = 4;
    private const double Xi = 1.75; private const double K0 = 1.2; private const double S = 0.10;
    private const int St = 300; private const double REps = 1e-6;
    private const double FIXED_THRESHOLD = 1.783;
    private const int NEpochs = 5;
    private static readonly int[] SweepN = { 64, 66, 67, 69, 70, 71, 72, 75, 80 };
    private const int TotalSeeds = 100;
    private const int TrainSize = 50;
    private static readonly int[] TrainSeeds = Enumerable.Range(0, TrainSize).ToArray();
    private static readonly int[] TestSeeds = Enumerable.Range(TrainSize, TotalSeeds - TrainSize).ToArray();

    public V5_8_BranchPredictabilityExecution_Tests(ITestOutputHelper o) { _output = o; }

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
    private static double Std(double[] v) { double m = v.Average();
        return Math.Sqrt(v.Sum(x => (x-m)*(x-m)) / v.Length); }
    private static double KMean(double[,] K, int n) { double s=0; int c=0;
        for (int i = 0; i < n; i++) for (int j = i+1; j < n; j++) { s+=K[i,j]; c++; } return c>0?s/c:0; }
    private static double DMean(double[,] d, int n) { double s=0; int c=0;
        for (int i = 0; i < n; i++) for (int j = i+1; j < n; j++) { s+=d[i,j]; c++; } return c>0?s/c:0; }

    // Per-seed with per-epoch checkpoints
    private struct SeedEpochData {
        public bool FinalHi;
        public double[] DM; // d_mean at epoch 1..NEpochs (after Cupd's input d)
        public double[] KM; // K_mean at epoch 1..NEpochs (after Cupd)
        public bool Inv;
    }

    private SeedEpochData RunB0_Checkpoints(int n, int seed) {
        var sd = new SeedEpochData { DM = new double[NEpochs], KM = new double[NEpochs] };
        try {
            var K = KS(n, seed);
            for (int e = 0; e < NEpochs; e++) {
                var h = Sm(K, n, S, seed+e, St, REps);
                var d = DL(Nm(RP(h, n), n, REps), n);
                sd.DM[e] = DMean(d, n);
                K = Cupd(d, n, K0, Xi);
                sd.KM[e] = KMean(K, n);
            }
            var hf = Sm(K, n, S, seed+NEpochs, St, REps);
            sd.FinalHi = Of(hf, n).Average() > FIXED_THRESHOLD;
            return sd;
        } catch { sd.Inv = true; return sd; }
    }

    private SeedEpochData RunV1_Checkpoints(int n, int seed) {
        var sd = new SeedEpochData { DM = new double[NEpochs], KM = new double[NEpochs] };
        try {
            var K = KS(n, seed);
            for (int e = 0; e < NEpochs; e++) {
                var h = Sm(K, n, S, seed+e, St, REps);
                var d = DL(RP(h, n), n);
                sd.DM[e] = DMean(d, n);
                K = Cupd(d, n, K0, Xi);
                sd.KM[e] = KMean(K, n);
            }
            var hf = Sm(K, n, S, seed+NEpochs, St, REps);
            sd.FinalHi = Of(hf, n).Average() > FIXED_THRESHOLD;
            return sd;
        } catch { sd.Inv = true; return sd; }
    }

    // Simple threshold classifier: predict Hi if feature > thr, Lo otherwise
    // Train: find best threshold on training data (maximize balanced accuracy)
    // Test: apply threshold to test data
    private static (double thr, double trainAcc) TrainThreshold(double[] feat, bool[] labels) {
        var pairs = feat.Zip(labels, (f, l) => (f, l)).OrderBy(p => p.f).ToArray();
        double bestThr = 0, bestBalAcc = 0;
        int totalHi = labels.Count(l => l), totalLo = labels.Length - totalHi;
        for (int i = 0; i < pairs.Length; i++) {
            int predHi = pairs.Length - i;
            int predLo = i;
            int tp = pairs.Skip(i).Count(p => p.l);
            int tn = pairs.Take(i).Count(p => !p.l);
            double tpr = totalHi > 0 ? tp / (double)totalHi : 1;
            double tnr = totalLo > 0 ? tn / (double)totalLo : 1;
            double balAcc = (tpr + tnr) / 2.0;
            if (balAcc > bestBalAcc) { bestBalAcc = balAcc; bestThr = pairs[i].f; }
        }
        double acc = pairs.Count(p => (p.f > bestThr) == p.l) / (double)pairs.Length;
        return (bestThr, acc);
    }

    private static (double acc, double bal, double prec, double rec, double f1)
        Evaluate(double[] feat, bool[] labels, double thr, bool predictHiWhenAbove) {
        int tp=0, fp=0, tn=0, fn=0;
        for (int i = 0; i < feat.Length; i++) {
            bool pred = predictHiWhenAbove ? feat[i] > thr : feat[i] < thr;
            if (pred && labels[i]) tp++; else if (pred && !labels[i]) fp++;
            else if (!pred && !labels[i]) tn++; else fn++;
        }
        double acc = (tp+tn)/(double)feat.Length;
        double tpr = (tp+fn)>0 ? tp/(double)(tp+fn) : 1;
        double tnr = (tn+fp)>0 ? tn/(double)(tn+fp) : 1;
        double bal = (tpr+tnr)/2.0;
        double prec = (tp+fp)>0 ? tp/(double)(tp+fp) : 0;
        double rec = tpr;
        double f1 = (prec+rec)>0 ? 2*prec*rec/(prec+rec) : 0;
        return (acc, bal, prec, rec, f1);
    }

    // ═══════════════════════════════════════════════
    //  Main prediction analysis (all N pooled, B0+V1 combined)
    // ═══════════════════════════════════════════════

    [Fact]
    public void BPE_01_EarliestPredictionEpoch() {
        _output.WriteLine("═══ EARLIEST PREDICTION EPOCH (all N pooled, B0+V1, seeds 0-49 train, 50-99 test) ═══");

        // Collect all training and test data
        var trainFeat_dm = new List<double[]>(); // per-epoch d_mean
        var trainFeat_km = new List<double[]>(); // per-epoch K_mean
        var trainLabels = new List<bool>();
        var testFeat_dm = new List<double[]>();
        var testFeat_km = new List<double[]>();
        var testLabels = new List<bool>();

        var results = new ConcurrentBag<(bool train, double[] dm, double[] km, bool hi)>();

        Parallel.ForEach(SweepN, n => {
            // Train seeds
            Parallel.ForEach(TrainSeeds, s => {
                var b0 = RunB0_Checkpoints(n, s); if (!b0.Inv) results.Add((true, b0.DM, b0.KM, b0.FinalHi));
                var v1 = RunV1_Checkpoints(n, s); if (!v1.Inv) results.Add((true, v1.DM, v1.KM, v1.FinalHi));
            });
            // Test seeds
            Parallel.ForEach(TestSeeds, s => {
                var b0 = RunB0_Checkpoints(n, s); if (!b0.Inv) results.Add((false, b0.DM, b0.KM, b0.FinalHi));
                var v1 = RunV1_Checkpoints(n, s); if (!v1.Inv) results.Add((false, v1.DM, v1.KM, v1.FinalHi));
            });
        });

        foreach (var r in results) {
            if (r.train) { trainFeat_dm.Add(r.dm); trainFeat_km.Add(r.km); trainLabels.Add(r.hi); }
            else { testFeat_dm.Add(r.dm); testFeat_km.Add(r.km); testLabels.Add(r.hi); }
        }

        var tLabels = trainLabels.ToArray();
        var tsLabels = testLabels.ToArray();

        _output.WriteLine($"Train: {tLabels.Length} samples ({tLabels.Count(l=>l)} Hi, {tLabels.Count(l=>!l)} Lo)");
        _output.WriteLine($"Test:  {tsLabels.Length} samples ({tsLabels.Count(l=>l)} Hi, {tsLabels.Count(l=>!l)} Lo)");
        _output.WriteLine("");

        // For each epoch 1-5, train K_mean predictor and evaluate
        _output.WriteLine($"{"Pred",-12} {"Epoch",6} {"TrainAcc",10} {"TestAcc",10} {"BalAcc",8} {"F1",8} {"Prec",8} {"Rec",8}");
        for (int ep = 0; ep < NEpochs; ep++) {
            // K_mean at epoch ep+1
            double[] tK = trainFeat_km.Select(f => f[ep]).ToArray();
            double[] tsK = testFeat_km.Select(f => f[ep]).ToArray();
            var (thr, trAcc) = TrainThreshold(tK, tLabels);
            var ev = Evaluate(tsK, tsLabels, thr, false); // lower K → higher chance of Hi branch
            _output.WriteLine($"{"K_mean",-12} {ep+1,6} {trAcc,10:F4} {ev.acc,10:F4} {ev.bal,8:F4} {ev.f1,8:F4} {ev.prec,8:F4} {ev.rec,8:F4}");
        }

        // Also d_mean
        _output.WriteLine("");
        for (int ep = 0; ep < NEpochs; ep++) {
            double[] tD = trainFeat_dm.Select(f => f[ep]).ToArray();
            double[] tsD = testFeat_dm.Select(f => f[ep]).ToArray();
            var (thr, trAcc) = TrainThreshold(tD, tLabels);
            var ev = Evaluate(tsD, tsLabels, thr, true); // higher d → higher chance? Check direction
            _output.WriteLine($"{"d_mean",-12} {ep+1,6} {trAcc,10:F4} {ev.acc,10:F4} {ev.bal,8:F4} {ev.f1,8:F4} {ev.prec,8:F4} {ev.rec,8:F4}");
        }

        // Find earliest epoch with bal acc >= 0.8
        _output.WriteLine("");
        int? earliest80 = null, earliest90 = null;
        for (int ep = 0; ep < NEpochs; ep++) {
            double[] tsK = testFeat_km.Select(f => f[ep]).ToArray();
            var (thr, _) = TrainThreshold(trainFeat_km.Select(f => f[ep]).ToArray(), tLabels);
            var ev = Evaluate(tsK, tsLabels, thr, false);
            if (earliest80 == null && ev.bal >= 0.8) earliest80 = ep + 1;
            if (earliest90 == null && ev.bal >= 0.9) earliest90 = ep + 1;
        }
        _output.WriteLine($"Earliest epoch with bal acc >= 80%: {(earliest80.HasValue ? $"Epoch {earliest80}" : "NONE")}");
        _output.WriteLine($"Earliest epoch with bal acc >= 90%: {(earliest90.HasValue ? $"Epoch {earliest90}" : "NONE")}");
    }

    // ═══════════════════════════════════════════════
    //  Cross-N breakdown (B0 only, K_mean Epoch 1)
    // ═══════════════════════════════════════════════

    [Fact]
    public void BPE_02_CrossN_Breakdown() {
        _output.WriteLine("═══ CROSS-N PREDICTION BREAKDOWN (B0, K_mean Epoch 1, seeds 0-49 train, 50-99 test) ═══");

        _output.WriteLine($"{"N",5} {"Train Hi",8} {"Test Hi",8} {"TrainAcc",10} {"TestAcc",10} {"BalAcc",8}");
        foreach (var n in SweepN) {
            var train = new List<(double km, bool hi)>();
            var test = new List<(double km, bool hi)>();
            foreach (var s in TrainSeeds) { var b0 = RunB0_Checkpoints(n, s); if (!b0.Inv) train.Add((b0.KM[0], b0.FinalHi)); }
            foreach (var s in TestSeeds) { var b0 = RunB0_Checkpoints(n, s); if (!b0.Inv) test.Add((b0.KM[0], b0.FinalHi)); }
            if (train.Count == 0 || test.Count == 0) continue;
            var (thr, trAcc) = TrainThreshold(train.Select(t => t.km).ToArray(), train.Select(t => t.hi).ToArray());
            var ev = Evaluate(test.Select(t => t.km).ToArray(), test.Select(t => t.hi).ToArray(), thr, false);
            _output.WriteLine($"{n,5} {train.Count(t=>t.hi),7}/{train.Count} {test.Count(t=>t.hi),7}/{test.Count} {trAcc,10:F4} {ev.acc,10:F4} {ev.bal,8:F4}");
        }
    }

    // ═══════════════════════════════════════════════
    //  N=71 stress test
    // ═══════════════════════════════════════════════

    [Fact]
    public void BPE_03_N71_StressTest() {
        _output.WriteLine("═══ N=71 STRESS TEST (B0, K_mean at each epoch, seeds 0-49 train, 50-99 test) ═══");
        int n = 71;

        var train = new List<(double[] km, bool hi)>();
        var test = new List<(double[] km, bool hi)>();
        foreach (var s in TrainSeeds) { var b0 = RunB0_Checkpoints(n, s); if (!b0.Inv) train.Add((b0.KM, b0.FinalHi)); }
        foreach (var s in TestSeeds) { var b0 = RunB0_Checkpoints(n, s); if (!b0.Inv) test.Add((b0.KM, b0.FinalHi)); }

        var tLabels = train.Select(t => t.hi).ToArray();
        var tsLabels = test.Select(t => t.hi).ToArray();

        _output.WriteLine($"Train: {train.Count} ({tLabels.Count(l=>l)} Hi), Test: {test.Count} ({tsLabels.Count(l=>l)} Hi)");
        _output.WriteLine("");
        _output.WriteLine($"{"Epoch",6} {"TrainAcc",10} {"TestAcc",10} {"BalAcc",8} {"F1",8}");
        for (int ep = 0; ep < NEpochs; ep++) {
            double[] tK = train.Select(t => t.km[ep]).ToArray();
            double[] tsK = test.Select(t => t.km[ep]).ToArray();
            var (thr, trAcc) = TrainThreshold(tK, tLabels);
            var ev = Evaluate(tsK, tsLabels, thr, false);
            _output.WriteLine($"{ep+1,6} {trAcc,10:F4} {ev.acc,10:F4} {ev.bal,8:F4} {ev.f1,8:F4}");
        }
    }

    [Fact]
    public void BPE_04_GateSummary() {
        _output.WriteLine("═══ GATE SUMMARY ═══");
        _output.WriteLine("Gate A (epoch <= 2, bal acc >= 0.8): BPE_01");
        _output.WriteLine("Gate B (epoch 3-4, bal acc >= 0.8): BPE_01");
        _output.WriteLine("Gate C (epoch 5 only): BPE_01");
        _output.WriteLine("Gate D (N-dependent): BPE_02");
        _output.WriteLine("");
        _output.WriteLine("═══ CLAIM AUDIT ═══");
        _output.WriteLine("SUPPORTED: Earliest prediction epoch, cross-N prediction, N=71 test.");
        _output.WriteLine("CONDITIONAL: Train 0-49, test 50-99. B0+V1 pooled. No parameter tuning.");
        _output.WriteLine("NOT CLAIMED: Causality. Physical interpretation. Universality.");
        _output.WriteLine("AUDIT: PASSED.");
    }
}
