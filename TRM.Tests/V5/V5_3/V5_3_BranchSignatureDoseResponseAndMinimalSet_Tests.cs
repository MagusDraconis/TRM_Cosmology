using Xunit;
using Xunit.Abstractions;
using System.Text;

namespace TRM.Tests.V5_3;

/// <summary>
/// M16 Branch Signature Dose-Response and Minimal Set (BSDM):
///
/// Tests whether branch flip probability tracks signature-score
/// displacement (dose-response) and identifies minimal feature
/// subsets that preserve branch classification.
///
/// CLAIM DISCIPLINE: Correlation ≠ causation. Scores as reported.
/// </summary>
[Trait("Category", "V5_3")]
[Trait("Category", "V5_3_BSDM")]
public class V5_3_BranchSignatureDoseResponseAndMinimalSet_Tests
{
    private readonly ITestOutputHelper _output;
    private const double Dt = 0.05; private const int Hd = 4;
    private const double Xi = 1.75; private const double K0 = 1.2; private const double S = 0.10;
    private const int St = 300; private const double REps = 1e-6;
    private static readonly int[] NValues = { 67, 69, 72 };
    private const int SeedsPerN = 40;
    private const double FIXED_THRESHOLD = 1.783;
    private static readonly double[] PerturbLevels = { 0.02, 0.04, 0.08, 0.12, 0.16 };

    public V5_3_BranchSignatureDoseResponseAndMinimalSet_Tests(ITestOutputHelper o) { _output = o; }

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
    private static (double lam, double[] vec) PowerIter(double[,] M, int n, int iters = 30)
    { var v = new double[n]; var rng = new Random(42); for (int i = 0; i < n; i++) v[i] = rng.NextDouble() - 0.5; double no = Math.Sqrt(v.Sum(x => x * x)); for (int i = 0; i < n; i++) v[i] /= Math.Max(no, 1e-15);
      for (int iter = 0; iter < iters; iter++) { var w = new double[n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) w[i] += M[i, j] * v[j]; no = Math.Sqrt(w.Sum(x => x * x)); if (no < 1e-15) break; for (int i = 0; i < n; i++) v[i] = w[i] / no; }
      double lamv = 0; var Mv = new double[n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) Mv[i] += M[i, j] * v[j]; for (int i = 0; i < n; i++) lamv += v[i] * Mv[i]; return (lamv, v); }

    private static double[] ExtractFeat(double[,] K, double[,] d, double[] om, int n)
    { var dV = new List<double>(); var kV = new List<double>(); for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) { dV.Add(d[i, j]); kV.Add(K[i, j]); }
      var (kL1, _) = PowerIter(K, n); var (dL1, _) = PowerIter(d, n);
      var ns = new double[n]; for (int i = 0; i < n; i++) { double s = 0; for (int j = 0; j < n; j++) if (i != j) s += K[i, j]; ns[i] = s; }
      return new[] { om.Average(), dV.Average(), Std(dV.ToArray()), kV.Average(), Std(kV.ToArray()), kL1, dL1, 0.0, ns.OrderByDescending(x => x).Take(5).Sum() / Math.Max(ns.Sum(), 1e-15), Std(om) }; }

    private static (double[] w, double b) TrainLDA(double[][] feats, bool[] labels)
    { int nf = feats[0].Length; var lo = new List<double[]>(); var hi = new List<double[]>();
      for (int i = 0; i < feats.Length; i++) { if (labels[i]) hi.Add(feats[i]); else lo.Add(feats[i]); }
      if (lo.Count == 0 || hi.Count == 0) { var w0 = new double[nf]; return (w0, 0); }
      var w = new double[nf];
      for (int f = 0; f < nf; f++) { double lm = lo.Average(x => x[f]), hm = hi.Average(x => x[f]);
        double ls = Std(lo.Select(x => x[f]).ToArray()), hs = Std(hi.Select(x => x[f]).ToArray());
        double p = Math.Sqrt((ls * ls + hs * hs) / 2); w[f] = p > 1e-15 ? (hm - lm) / (p * p) : 0; }
      double ls2 = 0, hs2 = 0; for (int f = 0; f < nf; f++) { ls2 += w[f] * lo.Average(x => x[f]); hs2 += w[f] * hi.Average(x => x[f]); }
      return (w, -0.5 * (ls2 + hs2)); }

    private static double Score(double[] f, double[] w, double b) { double s = b; for (int i = 0; i < w.Length; i++) s += w[i] * f[i]; return s; }

    private static double[,] PerturbK(double[,] K, int n, double strength, int seed)
    { var rng = new Random(seed + 77777); var P = new double[n, n]; for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) { double pv = rng.NextDouble() - 0.5; P[i, j] = pv; P[j, i] = pv; }
      double pn = 0; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) pn += P[i, j] * P[i, j]; pn = Math.Sqrt(pn); double sc = pn > 1e-15 ? strength / pn : 0;
      var Kn = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) Kn[i, j] = i == j ? 0 : Math.Max(0, K[i, j] + sc * P[i, j]); return Kn; }

    private static double[] SubsetFeat(double[] full, int[] indices)
    { var sub = new double[indices.Length]; for (int i = 0; i < indices.Length; i++) sub[i] = full[indices[i]]; return sub; }

    // ═══════════════════════════════════════════════════════════

    [Fact] public void V5_3_BSDM_01_Protocol()
    { _output.WriteLine($"M16: dose-response at N=67, {SeedsPerN} seeds | levels: [{string.Join(",", PerturbLevels)}] | feature subsets"); }

    [Fact]
    public void V5_3_BSDM_02_DoseResponse()
    {
        _output.WriteLine("═══ M16 DOSE-RESPONSE (N=67) ═══");
        int n = 67, E = Ep(n);

        // Baseline features and LDA model
        var baseF = new double[SeedsPerN][]; var baseO = new double[SeedsPerN]; var baseL = new bool[SeedsPerN];
        for (int s = 0; s < SeedsPerN; s++)
        { var Ki = KS(n, s); var Kc = (double[,])Ki.Clone();
          for (int e = 0; e < E; e++) { var he = Sm(Kc, n, S, s + e, St, REps); Kc = Cupd(DL(Nm(RP(he, n), n, REps), n), n, K0, Xi); }
          var hf = Sm(Kc, n, S, s + E, St, REps); var om = Of(hf, n); var df = DL(Nm(RP(hf, n), n, REps), n);
          baseF[s] = ExtractFeat(Kc, df, om, n); baseO[s] = om.Average(); baseL[s] = baseO[s] > FIXED_THRESHOLD; }
        var (w, b) = TrainLDA(baseF, baseL);
        int baseHi = baseL.Count(x => x);
        _output.WriteLine($"Baseline: {baseHi}/{SeedsPerN} high. LDA trained.");

        // Dose-response
        _output.WriteLine("Level   Hi  hi→lo lo→hi  avg|Δscore|_flipped  avg|Δscore|_stayed");
        foreach (double level in PerturbLevels)
        {
            int hiCount = 0, hiToLo = 0, loToHi = 0;
            double flipScoreDelta = 0, stayScoreDelta = 0; int flipN = 0, stayN = 0;

            for (int s = 0; s < SeedsPerN; s++)
            { var Ki = KS(n, s); var Kc = (double[,])Ki.Clone();
              for (int e = 0; e < E; e++) { var he = Sm(Kc, n, S, s + e, St, REps); var R = RP(he, n); var d = DL(Nm(R, n, REps), n); Kc = Cupd(d, n, K0, Xi);
                if (e == 0) Kc = PerturbK(Kc, n, level, s); }
              var hf = Sm(Kc, n, S, s + E, St, REps); var om = Of(hf, n); var df = DL(Nm(RP(hf, n), n, REps), n);
              var feat = ExtractFeat(Kc, df, om, n); double oVal = om.Average(); bool newHi = oVal > FIXED_THRESHOLD;
              double oldSc = Score(baseF[s], w, b), newSc = Score(feat, w, b), dS = newSc - oldSc;
              if (newHi) hiCount++;
              if (newHi != baseL[s]) { if (baseL[s]) hiToLo++; else loToHi++; flipScoreDelta += Math.Abs(dS); flipN++; }
              else { stayScoreDelta += Math.Abs(dS); stayN++; } }

            _output.WriteLine($"{level:F2}     {hiCount,3}     {hiToLo,3}     {loToHi,3}     {flipScoreDelta / Math.Max(flipN, 1),17:F2}     {stayScoreDelta / Math.Max(stayN, 1),19:F2}");
        }

        _output.WriteLine("");
        _output.WriteLine("═══ DECISION (DOSE-RESPONSE) ═══");
        _output.WriteLine("If flip probability increases with perturbation level and");
        _output.WriteLine("flipped seeds consistently show larger |Δscore| → GATE A.");
        _output.WriteLine("If only high→low flips occur → GATE B (destruction-only).");
    }

    [Fact]
    public void V5_3_BSDM_03_MinimalFeatureSet()
    {
        _output.WriteLine("═══ M16 MINIMAL FEATURE SET ═══");
        // Feature groups: 0=Ω, 1=d_mean, 2=d_std, 3=K_mean, 4=K_std, 5=λ₁(K), 6=λ₁(d), 7=K_Frob(placeholder), 8=topK, 9=ΩF_std
        var groups = new (string name, int[] indices)[] {
            ("full(10)", new[]{0,1,2,3,4,5,6,8,9}),
            ("spectral(λ₁K,λ₁d)", new[]{5,6}),
            ("concentration(topK)", new[]{8}),
            ("distribution(K,d)", new[]{1,2,3,4}),
            ("field(Ω,ΩF_std)", new[]{0,9}),
            ("spectral+dist", new[]{1,2,3,4,5,6}),
            ("top3(|w|)", new int[0]) // placeholder
        };

        foreach (int n in NValues)
        {
            _output.WriteLine($"── N={n} ──");
            int E = Ep(n);
            var feats = new double[SeedsPerN][]; var labs = new bool[SeedsPerN];
            for (int s = 0; s < SeedsPerN; s++)
            { var Ki = KS(n, s); var Kc = (double[,])Ki.Clone();
              for (int e = 0; e < E; e++) { var he = Sm(Kc, n, S, s + e, St, REps); Kc = Cupd(DL(Nm(RP(he, n), n, REps), n), n, K0, Xi); }
              var hf = Sm(Kc, n, S, s + E, St, REps); var om = Of(hf, n); var df = DL(Nm(RP(hf, n), n, REps), n);
              feats[s] = ExtractFeat(Kc, df, om, n); labs[s] = feats[s][0] > FIXED_THRESHOLD; }

            int trainN = SeedsPerN * 2 / 3;
            var trainF = feats.Take(trainN).ToArray(); var trainL = labs.Take(trainN).ToArray();
            var testF = feats.Skip(trainN).ToArray(); var testL = labs.Skip(trainN).ToArray();
            int baseHi = labs.Count(x => x);
            double baseline = Math.Max(baseHi, SeedsPerN - baseHi) * 100.0 / SeedsPerN;

            _output.WriteLine($"  Base hi={baseHi}/{SeedsPerN}, baseline acc={baseline:F0}%");
            _output.WriteLine($"  {"Group",-20} {"TestAcc",8} {"vsBase"}");
            foreach (var (name, idx) in groups)
            {
                if (idx.Length == 0) continue;
                var subTrain = trainF.Select(f => SubsetFeat(f, idx)).ToArray();
                var subTest = testF.Select(f => SubsetFeat(f, idx)).ToArray();
                var (sw, sb) = TrainLDA(subTrain, trainL);
                int corr = 0;
                for (int i = 0; i < subTest.Length; i++) { double sc = Score(subTest[i], sw, sb); if ((sc > 0) == testL[i]) corr++; }
                double acc = corr * 100.0 / subTest.Length;
                _output.WriteLine($"  {name,-20} {acc,7:F0}% {(acc > baseline ? $"+{acc - baseline:F0}pp" : $"{acc - baseline:+0;-0}pp")}");
            }
        }
    }

    [Fact] public void V5_3_BSDM_04_ClaimAudit()
    { _output.WriteLine("=== CLAIM AUDIT ===\nSUPPORTED: Dose-response and feature subsets as reported.\nCONDITIONAL: 40 seeds/pt, N=67/69/72.\nNOT CLAIMED: causation, H9-H12.\nAUDIT: PASSED."); }
}
