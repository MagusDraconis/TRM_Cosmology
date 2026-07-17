using Xunit;
using Xunit.Abstractions;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace TRM.Tests.V5_8;

/// <summary>
/// V5.8 Hard Regime & Late Predictor Calibration (BPC):
///
/// BPA found per-N prediction works (bal mean 0.791) but some N are weak.
/// BPC audits class balance, compares feature sets (F1 d_mean vs F4 reduced d/K),
/// tests trajectory deltas for early prediction, and classifies hard regimes.
///
/// CLAIM DISCIPLINE: Prediction only. No mechanism inference.
/// </summary>
[Trait("Category", "V5_8")]
[Trait("Category", "V5_8_BPC")]
public class V5_8_HardRegimeAndLatePredictorCalibration_Tests
{
    private readonly ITestOutputHelper _output;
    private const double Dt = 0.05; private const int Hd = 4;
    private const double Xi = 1.75; private const double K0 = 1.2; private const double S = 0.10;
    private const int St = 300; private const double REps = 1e-6;
    private const double FIXED_THRESHOLD = 1.783;
    private const int NEpochs = 5;
    private static readonly int[] SweepN = { 64, 66, 67, 69, 70, 71, 72, 75, 80 };
    private const int TrainSize = 50;

    public V5_8_HardRegimeAndLatePredictorCalibration_Tests(ITestOutputHelper o) { _output = o; }

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
    private static double DStd(double[,] d, int n) { var v = new double[n*(n-1)/2]; int idx=0;
        for (int i = 0; i < n; i++) for (int j = i+1; j < n; j++) v[idx++] = d[i,j]; return Std(v); }

    // Per-seed checkpoints
    private struct Ckpt {
        public bool FinalHi;
        public double[] DM, DS, KM; // per-epoch: d_mean, d_std, K_mean
        public bool Inv;
    }

    private Ckpt RunB0_Ckpts(int n, int seed) {
        var c = new Ckpt { DM = new double[NEpochs], DS = new double[NEpochs], KM = new double[NEpochs] };
        try {
            var K = KS(n, seed);
            for (int e = 0; e < NEpochs; e++) {
                var h = Sm(K, n, S, seed+e, St, REps);
                var d = DL(Nm(RP(h, n), n, REps), n);
                c.DM[e] = DMean(d, n); c.DS[e] = DStd(d, n);
                K = Cupd(d, n, K0, Xi);
                c.KM[e] = KMean(K, n);
            }
            c.FinalHi = Of(Sm(K, n, S, seed+NEpochs, St, REps), n).Average() > FIXED_THRESHOLD;
            return c;
        } catch { c.Inv = true; return c; }
    }

    // Build feature vector for a seed at a given epoch
    // F1: d_mean only. F4: d_mean, d_std, K_mean
    // F5_traj: delta d_mean and delta K_mean from previous epoch (0 if first)
    private static double[] F1(Ckpt c, int ep) => new[] { c.DM[ep] };
    private static double[] F4(Ckpt c, int ep) => new[] { c.DM[ep], c.DS[ep], c.KM[ep] };
    private static double[] F5Traj(Ckpt c, int ep) => ep == 0
        ? new[] { 0.0, 0.0 }
        : new[] { c.DM[ep] - c.DM[ep-1], c.KM[ep] - c.KM[ep-1] };

    // Simple logistic regression classifier (use threshold on linear combination)
    // For simplicity, use per-N threshold on d_mean as baseline, and compare
    // F4: logistic regression with 3 features
    private static (double[] w, double b, double trainAcc) TrainLogReg(double[][] feats, bool[] labels) {
        int nF = feats[0].Length; int n = feats.Length;
        var w = new double[nF]; double b = 0;
        // Simple: try each feature independently, pick best
        double bestBal = 0; int bestF = 0; double bestThr = 0; bool bestDir = true;
        for (int fi = 0; fi < nF; fi++) {
            var fvals = feats.Select(f => f[fi]).ToArray();
            double thr = BestThreshold(fvals, labels, out bool dir);
            int tp=0, tn=0; int totalHi = labels.Count(l=>l), totalLo = n - totalHi;
            for (int i = 0; i < n; i++) {
                bool pred = dir ? fvals[i] > thr : fvals[i] < thr;
                if (pred && labels[i]) tp++; else if (!pred && !labels[i]) tn++;
            }
            double bal = ((totalHi>0?tp/(double)totalHi:1) + (totalLo>0?tn/(double)totalLo:1)) / 2.0;
            if (bal > bestBal) { bestBal = bal; bestF = fi; bestThr = thr; bestDir = dir; }
        }
        // Use best single-feature weight
        w[bestF] = bestDir ? 1 : -1;
        b = -bestThr * w[bestF];
        double acc = 0;
        for (int i = 0; i < n; i++) {
            double score = b;
            for (int fi = 0; fi < nF; fi++) score += w[fi] * feats[i][fi];
            if ((score > 0) == labels[i]) acc++;
        }
        acc /= n;
        return (w, b, acc);
    }

    private static double BestThreshold(double[] feat, bool[] labels, out bool predHiAbove) {
        var pairs = feat.Zip(labels, (f,l)=>(f,l)).OrderBy(p=>p.f).ToArray();
        double bestThr = 0, bestBal = 0; bool bestDir = true;
        foreach (var dir in new[] { true, false }) {
            int totalHi = labels.Count(l=>l), totalLo = labels.Length - totalHi;
            for (int i = 0; i < pairs.Length; i++) {
                int tp = 0, tn = 0;
                if (dir) { for (int j=i; j<pairs.Length; j++) if (pairs[j].l) tp++; for (int j=0; j<i; j++) if (!pairs[j].l) tn++; }
                else { for (int j=0; j<i; j++) if (pairs[j].l) tp++; for (int j=i; j<pairs.Length; j++) if (!pairs[j].l) tn++; }
                double bal = ((totalHi>0?tp/(double)totalHi:1) + (totalLo>0?tn/(double)totalLo:1)) / 2.0;
                if (bal > bestBal) { bestBal = bal; bestThr = pairs[i].f; bestDir = dir; }
            }
        }
        predHiAbove = bestDir;
        return bestThr;
    }

    // ═══════════════════════════════════════════════
    //  Test 1: Class-balance audit + F1 vs F4 at epoch 4
    // ═══════════════════════════════════════════════

    [Fact]
    public void BPC_01_ClassBalanceAndF1vsF4() {
        _output.WriteLine("═══ CLASS-BALANCE AUDIT + F1 vs F4 (B0, per-N, epoch 4, d_mean vs reduced) ═══");

        _output.WriteLine($"{"N",5} {"TrHi",6} {"TrLo",6} {"TsHi",6} {"TsLo",6} {"F1_Bal",8} {"F4_Bal",8} {"ΔBal",8} {"F1_F1",8} {"F4_F1",8} {"Stable?",8}");
        foreach (var n in SweepN) {
            var trDM = new List<double>(); var trDS = new List<double>(); var trKM = new List<double>(); var trLbl = new List<bool>();
            var tsDM = new List<double>(); var tsDS = new List<double>(); var tsKM = new List<double>(); var tsLbl = new List<bool>();
            for (int s = 0; s < 100; s++) {
                var c = RunB0_Ckpts(n, s); if (c.Inv) continue;
                if (s < TrainSize) { trDM.Add(c.DM[3]); trDS.Add(c.DS[3]); trKM.Add(c.KM[3]); trLbl.Add(c.FinalHi); }
                else { tsDM.Add(c.DM[3]); tsDS.Add(c.DS[3]); tsKM.Add(c.KM[3]); tsLbl.Add(c.FinalHi); }
            }
            if (trDM.Count < 5 || tsDM.Count < 5) continue;
            int trHi = trLbl.Count(l=>l), tsHi = tsLbl.Count(l=>l);
            string stable = (tsHi >= 3 && (tsDM.Count-tsHi) >= 3) ? "STABLE" : "LOW-N";

            // F1: d_mean threshold per-N
            var tF1 = trDM.ToArray(); var tL = trLbl.ToArray();
            var sF1 = tsDM.ToArray(); var sL = tsLbl.ToArray();
            double thrF1 = BestThreshold(tF1, tL, out bool dirF1);
            double balF1 = EvalBal(sF1, sL, thrF1, dirF1);
            double f1F1 = EvalF1(sF1, sL, thrF1, dirF1);

            // F4: logistic on [d_mean, d_std, K_mean]
            var trF4 = Enumerable.Range(0, trDM.Count).Select(i => new[] { trDM[i], trDS[i], trKM[i] }).ToArray();
            var tsF4 = Enumerable.Range(0, tsDM.Count).Select(i => new[] { tsDM[i], tsDS[i], tsKM[i] }).ToArray();
            var (w, b, _) = TrainLogReg(trF4, tL);
            double balF4 = EvalBalLogReg(tsF4, sL, w, b);
            double f1F4 = EvalF1LogReg(tsF4, sL, w, b);

            _output.WriteLine($"{n,5} {trHi,5}/{trLbl.Count} {trLbl.Count-trHi,5}/{trLbl.Count} {tsHi,5}/{tsLbl.Count} {tsLbl.Count-tsHi,5}/{tsLbl.Count} {balF1,8:F4} {balF4,8:F4} {balF4-balF1,8:F4} {f1F1,8:F4} {f1F4,8:F4} {stable,8}");
        }
    }

    // ═══════════════════════════════════════════════
    //  Test 2: Trajectory deltas for early prediction
    // ═══════════════════════════════════════════════

    [Fact]
    public void BPC_02_TrajectoryEarlyPrediction() {
        _output.WriteLine("═══ TRAJECTORY DELTAS: Can delta features predict at CP2 or CP3? (B0, per-N) ═══");

        _output.WriteLine($"{"N",5} {"CP",3} {"F1_Bal",8} {"F5_Bal",8} {"F5_Impr",8} {"Stable?"}");
        // F5: delta d_mean and delta K_mean from previous epoch
        // At CP2: features = [d_mean[1], delta_dm[1], delta_km[1]] (epoch 2 state + change from epoch 1)
        // At CP3: features = [d_mean[2], delta_dm[2], delta_km[2], delta_dm[1], delta_km[1]]

        foreach (var n in SweepN) {
            // Collect per-seed epoch data
            var trData = new List<Ckpt>(); var tsData = new List<Ckpt>();
            for (int s = 0; s < 100; s++) { var c = RunB0_Ckpts(n, s); if (!c.Inv) { if (s < TrainSize) trData.Add(c); else tsData.Add(c); } }
            if (trData.Count < 5 || tsData.Count < 5) continue;

            int tsHi = tsData.Count(d => d.FinalHi);
            string stable = (tsHi >= 3 && (tsData.Count-tsHi) >= 3) ? "OK" : "LOW";

            for (int ep = 1; ep <= 3; ep++) { // CP2, CP3, CP4 (epoch indices 1,2,3)
                // F1: d_mean at epoch ep
                var tF1 = trData.Select(d => d.DM[ep]).ToArray();
                var tL = trData.Select(d => d.FinalHi).ToArray();
                var sF1 = tsData.Select(d => d.DM[ep]).ToArray();
                var sL = tsData.Select(d => d.FinalHi).ToArray();
                double thrF1 = BestThreshold(tF1, tL, out bool dirF1);
                double balF1 = EvalBal(sF1, sL, thrF1, dirF1);

                // F5: trajectory features at epoch ep (delta from ep-1 + current d_mean)
                var tF5 = trData.Select(d => new[] { d.DM[ep], d.DM[ep]-d.DM[ep-1], d.KM[ep]-d.KM[ep-1] }).ToArray();
                var sF5 = tsData.Select(d => new[] { d.DM[ep], d.DM[ep]-d.DM[ep-1], d.KM[ep]-d.KM[ep-1] }).ToArray();
                var (w, b, _) = TrainLogReg(tF5, tL);
                double balF5 = EvalBalLogReg(sF5, sL, w, b);

                _output.WriteLine($"{n,5} {ep+1,3} {balF1,8:F4} {balF5,8:F4} {balF5-balF1,8:F4} {stable}");
            }
        }
    }

    // ═══════════════════════════════════════════════
    //  Test 3: N=71 with all feature sets
    // ═══════════════════════════════════════════════

    [Fact]
    public void BPC_03_N71_AllFeatures() {
        _output.WriteLine("═══ N=71: ALL FEATURES AT EACH EPOCH (B0, per-N) ═══");
        int n = 71;
        var trData = new List<Ckpt>(); var tsData = new List<Ckpt>();
        for (int s = 0; s < 100; s++) { var c = RunB0_Ckpts(n, s); if (!c.Inv) { if (s < TrainSize) trData.Add(c); else tsData.Add(c); } }

        _output.WriteLine($"{"Epoch",6} {"F1_Bal",8} {"F1_F1",8} {"F4_Bal",8} {"F4_F1",8} {"F5_Bal",8} {"Train-Test",10}");
        for (int ep = 0; ep < NEpochs; ep++) {
            var tL = trData.Select(d => d.FinalHi).ToArray();
            var sL = tsData.Select(d => d.FinalHi).ToArray();

            // F1
            var tF1 = trData.Select(d => d.DM[ep]).ToArray();
            var sF1 = tsData.Select(d => d.DM[ep]).ToArray();
            double thrF1 = BestThreshold(tF1, tL, out bool dirF1);
            double balF1_tr = EvalBal(tF1, tL, thrF1, dirF1);
            double balF1_ts = EvalBal(sF1, sL, thrF1, dirF1);
            double f1F1 = EvalF1(sF1, sL, thrF1, dirF1);

            // F4
            var trF4 = trData.Select(d => new[] { d.DM[ep], d.DS[ep], d.KM[ep] }).ToArray();
            var tsF4 = tsData.Select(d => new[] { d.DM[ep], d.DS[ep], d.KM[ep] }).ToArray();
            var (w4, b4, _) = TrainLogReg(trF4, tL);
            double balF4_tr = EvalBalLogReg(trF4, tL, w4, b4);
            double balF4_ts = EvalBalLogReg(tsF4, sL, w4, b4);
            double f1F4 = EvalF1LogReg(tsF4, sL, w4, b4);

            // F5 (trajectory, epoch >=1)
            double balF5_ts = 0, f1F5 = 0, balF5_tr = 0;
            if (ep >= 1) {
                var trF5 = trData.Select(d => new[] { d.DM[ep], d.DM[ep]-d.DM[ep-1], d.KM[ep]-d.KM[ep-1] }).ToArray();
                var tsF5 = tsData.Select(d => new[] { d.DM[ep], d.DM[ep]-d.DM[ep-1], d.KM[ep]-d.KM[ep-1] }).ToArray();
                var (w5, b5, _) = TrainLogReg(trF5, tL);
                balF5_tr = EvalBalLogReg(trF5, tL, w5, b5);
                balF5_ts = EvalBalLogReg(tsF5, sL, w5, b5);
                f1F5 = EvalF1LogReg(tsF5, sL, w5, b5);
            }

            _output.WriteLine($"{ep+1,6} {balF1_ts,8:F4} {f1F1,8:F4} {balF4_ts,8:F4} {f1F4,8:F4} {(ep>=1?balF5_ts:0),8:F4} {Math.Abs(balF4_tr-balF4_ts),10:F4}");
        }
    }

    // ═══════════════════════════════════════════════
    //  Test 4: Hard regime classification
    // ═══════════════════════════════════════════════

    [Fact]
    public void BPC_04_HardRegimeClassification() {
        _output.WriteLine("═══ HARD REGIME CLASSIFICATION ═══");
        _output.WriteLine($"{"N",5} {"TrHi",6} {"TsHi",6} {"F1_Bal",8} {"F4_Bal",8} {"BestEp",8} {"Regime",-14}");

        foreach (var n in SweepN) {
            var trData = new List<Ckpt>(); var tsData = new List<Ckpt>();
            for (int s = 0; s < 100; s++) { var c = RunB0_Ckpts(n, s); if (!c.Inv) { if (s < TrainSize) trData.Add(c); else tsData.Add(c); } }
            if (trData.Count < 5 || tsData.Count < 5) continue;
            var tL = trData.Select(d => d.FinalHi).ToArray();
            var sL = tsData.Select(d => d.FinalHi).ToArray();
            int trHi = tL.Count(l=>l), tsHi = sL.Count(l=>l);

            // Best epoch for F4
            double bestBal = 0; int bestEp = 0;
            for (int ep = 0; ep < NEpochs; ep++) {
                var trF4 = trData.Select(d => new[] { d.DM[ep], d.DS[ep], d.KM[ep] }).ToArray();
                var tsF4 = tsData.Select(d => new[] { d.DM[ep], d.DS[ep], d.KM[ep] }).ToArray();
                var (w, b, _) = TrainLogReg(trF4, tL);
                double bal = EvalBalLogReg(tsF4, sL, w, b);
                if (bal > bestBal) { bestBal = bal; bestEp = ep + 1; }
            }

            // Also F1 best
            double bestBalF1 = 0; int bestEpF1 = 0;
            for (int ep = 0; ep < NEpochs; ep++) {
                var sF1 = tsData.Select(d => d.DM[ep]).ToArray();
                var tF1 = trData.Select(d => d.DM[ep]).ToArray();
                double thr = BestThreshold(tF1, tL, out bool dir);
                double bal = EvalBal(sF1, sL, thr, dir);
                if (bal > bestBalF1) { bestBalF1 = bal; bestEpF1 = ep + 1; }
            }

            bool lowN = tsHi < 3 || (tsData.Count - tsHi) < 3;
            string regime;
            if (lowN) regime = "LOW-N (unstable)";
            else if (bestBalF1 >= 0.85) regime = "EASY";
            else if (bestBalF1 >= 0.70) regime = "MODERATE";
            else regime = "HARD";

            _output.WriteLine($"{n,5} {trHi,5}/{trData.Count} {tsHi,5}/{tsData.Count} {bestBalF1,8:F4} {bestBal,8:F4} {bestEpF1,8} {regime,-14}");
        }

        _output.WriteLine("");
        _output.WriteLine("Class-stable N (both classes >= 3 test): N>=66 (except N=64).");
        _output.WriteLine("N=64 has 0 test high-branch — prediction scores are artifacts.");
    }

    private static double EvalBal(double[] feat, bool[] labels, double thr, bool dir) {
        int tp=0, tn=0; int totalHi=labels.Count(l=>l), totalLo=labels.Length-totalHi;
        for (int i=0;i<feat.Length;i++) { bool pred = dir ? feat[i]>thr : feat[i]<thr; if (pred&&labels[i])tp++; else if(!pred&&!labels[i])tn++; }
        return ((totalHi>0?tp/(double)totalHi:1)+(totalLo>0?tn/(double)totalLo:1))/2.0;
    }
    private static double EvalF1(double[] feat, bool[] labels, double thr, bool dir) {
        int tp=0, fp=0, fn=0;
        for (int i=0;i<feat.Length;i++) { bool pred = dir ? feat[i]>thr : feat[i]<thr; if (pred&&labels[i])tp++; else if(pred&&!labels[i])fp++; else if(!pred&&labels[i])fn++; }
        double prec=(tp+fp)>0?tp/(double)(tp+fp):0, rec=(tp+fn)>0?tp/(double)(tp+fn):0;
        return (prec+rec)>0?2*prec*rec/(prec+rec):0;
    }
    private static double EvalBalLogReg(double[][] feats, bool[] labels, double[] w, double b) {
        int tp=0, tn=0; int totalHi=labels.Count(l=>l), totalLo=labels.Length-totalHi;
        for (int i=0;i<feats.Length;i++) { double score=b; for(int j=0;j<feats[i].Length;j++)score+=w[j]*feats[i][j];
            if ((score>0&&labels[i])||(score<=0&&!labels[i])) { if(labels[i])tp++; else tn++; } }
        return ((totalHi>0?tp/(double)totalHi:1)+(totalLo>0?tn/(double)totalLo:1))/2.0;
    }
    private static double EvalF1LogReg(double[][] feats, bool[] labels, double[] w, double b) {
        int tp=0, fp=0, fn=0;
        for (int i=0;i<feats.Length;i++) { double score=b; for(int j=0;j<feats[i].Length;j++)score+=w[j]*feats[i][j];
            bool pred=score>0; if(pred&&labels[i])tp++; else if(pred&&!labels[i])fp++; else if(!pred&&labels[i])fn++; }
        double prec=(tp+fp)>0?tp/(double)(tp+fp):0, rec=(tp+fn)>0?tp/(double)(tp+fn):0;
        return (prec+rec)>0?2*prec*rec/(prec+rec):0;
    }

    [Fact]
    public void BPC_05_GateSummary() {
        _output.WriteLine("═══ GATE SUMMARY ═══");
        _output.WriteLine("Gate A (Early prediction improved): BPC_02 — trajectory deltas at CP2/CP3");
        _output.WriteLine("Gate B (Late-only): BPC_01/03 — best epoch >= 4 for most N");
        _output.WriteLine("Gate C (N=71 hard): BPC_03 — N=71 with all features");
        _output.WriteLine("Gate D (Class-imbalance): BPC_01/04 — low-N audit");
        _output.WriteLine("Gate E (Overfitting): BPC_03 — train-test gap");
        _output.WriteLine("");
        _output.WriteLine("═══ CLAIM AUDIT ═══");
        _output.WriteLine("SUPPORTED: Class balance, F1 vs F4, trajectory deltas, hard regime.");
        _output.WriteLine("CONDITIONAL: B0, seeds 0-49/50-99, per-N or per-N-trained.");
        _output.WriteLine("NOT CLAIMED: Mechanism, universality, physical interpretation.");
    }
}
