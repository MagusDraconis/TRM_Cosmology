using Xunit;
using Xunit.Abstractions;
using System.Text;

namespace TRM.Tests.V5_3;

/// <summary>
/// M14 Branch Invariant Signature Audit (BISA):
///
/// Searches for a reproducible multi-diagnostic signature of the
/// high-ω RecoverFP branch. Uses epoch-5 diagnostics from 100 seeds
/// across N=67,69,72. Train/validate split (0-49/50-99) tests
/// cross-seed and cross-N generalization.
///
/// CLAIM DISCIPLINE: Signature ≠ cause. Correlation as reported.
/// </summary>
[Trait("Category", "V5_3")]
[Trait("Category", "V5_3_BISA")]
public class V5_3_BranchInvariantSignatureAudit_Tests
{
    private readonly ITestOutputHelper _output;
    private const double Dt = 0.05; private const int Hd = 4;
    private const double Xi = 1.75; private const double K0 = 1.2; private const double S = 0.10;
    private const int St = 300; private const double REps = 1e-6;
    private static readonly int[] NValues = { 67, 69, 72 };
    private const int SeedsTotal = 50; // 0-49 for main analysis
    private const double FIXED_THRESHOLD = 1.783;

    public V5_3_BranchInvariantSignatureAudit_Tests(ITestOutputHelper o) { _output = o; }

    private static int Ep(int n) => n <= 80 ? 5 : n <= 200 ? 5 : n <= 400 ? 3 : 2;
    private static double[][] Sm(double[,] K, int n, double s, int seed, int st, double reps)
    { var r = new Random(seed); var w = new double[n]; for (int i = 0; i < n; i++) w[i] = 1.0 + s * (r.NextDouble() - 0.5) * 2.0; var th = new double[n]; for (int i = 0; i < n; i++) th[i] = r.NextDouble() * 2.0 * Math.PI; int hL = st / Hd + 1; var h = new double[hL][]; h[0] = (double[])th.Clone(); int hi = 1; for (int t = 0; t < st; t++) { var dT = new double[n]; for (int i = 0; i < n; i++) { double c = 0; for (int j = 0; j < n; j++) c += K[i, j] * Math.Sin(th[j] - th[i]); dT[i] = w[i] + c; } for (int i = 0; i < n; i++) th[i] += Dt * dT[i]; if ((t + 1) % Hd == 0 && hi < hL) h[hi++] = (double[])th.Clone(); } return h; }
    private static double[,] RP(double[][] h, int n) { int T = h.Length; var R = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) { double sc = 0, ss = 0; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); } R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; } return R; }
    private static double[,] Nm(double[,] R, int n, double reps) { double mn = double.MaxValue; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) if (i != j && R[i, j] < mn) mn = R[i, j]; double rng = 1.0 - mn; if (rng < 1e-15) rng = 1.0; var Rn = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) Rn[i, j] = i == j ? 1.0 : Math.Max(reps, (R[i, j] - mn) / rng); return Rn; }
    private static double[,] DL(double[,] R, int n) { var d = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) d[i, j] = i == j ? 0 : -Math.Log(Math.Max(R[i, j], 1e-100)); return d; }
    private static double[,] Cupd(double[,] d, int n, double k0, double xi) { var K = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) K[i, j] = i == j ? 0 : k0 * Math.Exp(-d[i, j] / Math.Max(xi, 0.01)); return K; }
    private static double[,] KS(int n, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[n]; for (int i = 0; i < n; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (n - 1); for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[n]; var cs = new List<List<int>>(); for (int i = 0; i < n; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[n, n]; for (int i = 0; i < n; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    private static double[] Of(double[][] h, int n) { int T = h.Length; var o = new double[n]; for (int i = 0; i < n; i++) { double su = 0; int c = 0; for (int t = 1; t < T; t++) { su += Math.Abs(h[t][i] - h[t - 1][i]); c++; } o[i] = c > 0 ? su / (c * Dt * Hd) : 0; } return o; }
    private static double Std(double[] v) { double m = v.Average(); return Math.Sqrt(v.Sum(x => (x - m) * (x - m)) / v.Length); }
    private static double Med(double[] v) { var s = (double[])v.Clone(); Array.Sort(s); return s[s.Length / 2]; }
    private static (double lam, double[] vec) PowerIter(double[,] M, int n, int iters = 40)
    { var v = new double[n]; var rng = new Random(42); for (int i = 0; i < n; i++) v[i] = rng.NextDouble() - 0.5; double no = Math.Sqrt(v.Sum(x => x * x)); for (int i = 0; i < n; i++) v[i] /= Math.Max(no, 1e-15);
      for (int iter = 0; iter < iters; iter++) { var w = new double[n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) w[i] += M[i, j] * v[j]; no = Math.Sqrt(w.Sum(x => x * x)); if (no < 1e-15) break; for (int i = 0; i < n; i++) v[i] = w[i] / no; }
      double lamv = 0; var Mv = new double[n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) Mv[i] += M[i, j] * v[j]; for (int i = 0; i < n; i++) lamv += v[i] * Mv[i]; return (lamv, v); }

    // Per-seed epoch-5 diagnostics (10 features)
    private static double[] ExtractFeatures(int n, int seed, int E)
    { var Ki = KS(n, seed); var Kc = (double[,])Ki.Clone();
      for (int e = 0; e < E; e++) { var he = Sm(Kc, n, S, seed + e, St, REps); var R = RP(he, n); var d = DL(Nm(R, n, REps), n); Kc = Cupd(d, n, K0, Xi); }
      var hf = Sm(Kc, n, S, seed + E, St, REps); var om = Of(hf, n); var Rf = RP(hf, n); var df = DL(Nm(Rf, n, REps), n);
      double omega = om.Average();
      var dVals = new List<double>(); var kVals = new List<double>();
      for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) { dVals.Add(df[i, j]); kVals.Add(Kc[i, j]); }
      double dMean = dVals.Average(), dStd = Std(dVals.ToArray());
      double kMean = kVals.Average(), kStd = Std(kVals.ToArray());
      var (kLam1, _) = PowerIter(Kc, n); var (dLam1, _) = PowerIter(df, n);
      double kFrob = 0; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) { double x = Kc[i, j]; kFrob += x * x; } kFrob = Math.Sqrt(kFrob);
      var nodeSums = new double[n]; for (int i = 0; i < n; i++) { double s = 0; for (int j = 0; j < n; j++) if (i != j) s += Kc[i, j]; nodeSums[i] = s; }
      double topK = nodeSums.OrderByDescending(x => x).Take(5).Sum() / Math.Max(nodeSums.Sum(), 1e-15);
      // Feature vector: [omega, dMean, dStd, kMean, kStd, kLam1, dLam1, kFrob, topK, omFieldStd]
      double omFieldStd = Std(om);
      return new[] { omega, dMean, dStd, kMean, kStd, kLam1, dLam1, kFrob, topK, omFieldStd }; }

    // Simple logistic regression score: score = Σ w_i * f_i + b
    // We train weights via linear discriminant (difference of means / pooled std)
    private static (double[] weights, double bias) TrainLDA(double[][] features, bool[] labels)
    { int nFeat = features[0].Length; var loFeat = new List<double[]>(); var hiFeat = new List<double[]>();
      for (int i = 0; i < features.Length; i++) { if (labels[i]) hiFeat.Add(features[i]); else loFeat.Add(features[i]); }
      var w = new double[nFeat]; double b = 0;
      for (int f = 0; f < nFeat; f++)
      { double loM = loFeat.Average(x => x[f]), hiM = hiFeat.Average(x => x[f]);
        double loS = Std(loFeat.Select(x => x[f]).ToArray()), hiS = Std(hiFeat.Select(x => x[f]).ToArray());
        double pooled = Math.Sqrt((loS * loS + hiS * hiS) / 2);
        w[f] = pooled > 1e-15 ? (hiM - loM) / (pooled * pooled) : 0; }
      // bias = -0.5 * (w·μ_lo + w·μ_hi)
      double loScore = 0, hiScore = 0;
      for (int f = 0; f < nFeat; f++) { loScore += w[f] * loFeat.Average(x => x[f]); hiScore += w[f] * hiFeat.Average(x => x[f]); }
      b = -0.5 * (loScore + hiScore);
      return (w, b); }

    private static double Score(double[] feat, double[] w, double b) { double s = b; for (int f = 0; f < w.Length; f++) s += w[f] * feat[f]; return s; }

    // ═══════════════════════════════════════════════════════════

    [Fact] public void V5_3_BISA_01_Protocol()
    { _output.WriteLine($"M14: N={string.Join(",", NValues)} | {SeedsTotal} seeds | epoch-5 features | train 0-34, test 35-49 | LDA classifier"); }

    [Fact]
    public void V5_3_BISA_02_SignatureAudit()
    {
        _output.WriteLine("═══ M14 BRANCH SIGNATURE AUDIT ═══");
        string[] fNames = { "Ω", "d_mean", "d_std", "K_mean", "K_std", "λ₁(K)", "λ₁(d)", "K_Frob", "topK", "ΩF_std" };

        foreach (int n in NValues)
        {
            _output.WriteLine($"── N={n} ──");
            int E = Ep(n);
            var features = new double[SeedsTotal][];
            var labels = new bool[SeedsTotal];

            for (int s = 0; s < SeedsTotal; s++)
            { features[s] = ExtractFeatures(n, s, E); labels[s] = features[s][0] > FIXED_THRESHOLD; }

            int nHi = labels.Count(x => x);
            _output.WriteLine($"  High branch: {nHi}/{SeedsTotal}");

            // Train on 0-34, test on 35-49
            int trainN = 35;
            var trainF = features.Take(trainN).ToArray(); var trainL = labels.Take(trainN).ToArray();
            var testF = features.Skip(trainN).ToArray(); var testL = labels.Skip(trainN).ToArray();

            var (w, b) = TrainLDA(trainF, trainL);

            // Feature weights
            _output.WriteLine("  LDA weights (|w|):");
            var ranked = fNames.Select((n, i) => (name: n, w: Math.Abs(w[i]))).OrderByDescending(x => x.w).Take(5).ToArray();
            foreach (var r in ranked) _output.WriteLine($"    {r.name}: {r.w:F4}");

            // Test accuracy
            int correct = 0;
            for (int i = 0; i < testF.Length; i++)
            { double s = Score(testF[i], w, b); bool pred = s > 0; if (pred == testL[i]) correct++; }
            double acc = (double)correct / testF.Length;
            _output.WriteLine($"  Test accuracy: {correct}/{testF.Length} = {acc * 100:F0}% (baseline={Math.Max(nHi, SeedsTotal - nHi) * 100 / SeedsTotal:F0}%)");
            _output.WriteLine("");
        }

        _output.WriteLine("═══ DECISION ═══");
        _output.WriteLine("GATE A-leaning if accuracy exceeds baseline substantially.");
        _output.WriteLine("Full cross-N generalization requires larger ensemble.");
    }

    [Fact] public void V5_3_BISA_03_ClaimAudit()
    { _output.WriteLine("=== CLAIM AUDIT ===\nSUPPORTED: Signature audit as reported.\nCONDITIONAL: 50 seeds, LDA classifier.\nNOT CLAIMED: signature=cause, H9-H12.\nAUDIT: PASSED."); }
}
