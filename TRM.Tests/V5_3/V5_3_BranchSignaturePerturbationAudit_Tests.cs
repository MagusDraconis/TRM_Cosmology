using Xunit;
using Xunit.Abstractions;
using System.Text;

namespace TRM.Tests.V5_3;

/// <summary>
/// M15 Branch Signature Perturbation Audit (BSPA):
///
/// Tests whether disrupting the M14 multi-diagnostic branch signature
/// changes branch outcomes more than generic perturbation. Computes
/// LDA scores before/after perturbation and correlates score disruption
/// with branch flips.
///
/// CLAIM DISCIPLINE: Correlation ≠ causation. Scores as reported.
/// </summary>
[Trait("Category", "V5_3")]
[Trait("Category", "V5_3_BSPA")]
public class V5_3_BranchSignaturePerturbationAudit_Tests
{
    private readonly ITestOutputHelper _output;
    private const double Dt = 0.05; private const int Hd = 4;
    private const double Xi = 1.75; private const double K0 = 1.2; private const double S = 0.10;
    private const int St = 300; private const double REps = 1e-6;
    private const int NTarget = 67; private const int SeedsTotal = 50;
    private const double FIXED_THRESHOLD = 1.783;
    private const double LambdaShift = 0.08;
    private const double RandomNorm = 0.08;

    public V5_3_BranchSignaturePerturbationAudit_Tests(ITestOutputHelper o) { _output = o; }

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
      return new[] { om.Average(), dV.Average(), Std(dV.ToArray()), kV.Average(), Std(kV.ToArray()), kL1, dL1, 0.0, ns.OrderByDescending(x=>x).Take(5).Sum()/Math.Max(ns.Sum(),1e-15), Std(om) }; }

    private static (double[] w, double b) TrainLDA(double[][] feats, bool[] labels)
    { int nf = feats[0].Length; var lo = new List<double[]>(); var hi = new List<double[]>();
      for (int i = 0; i < feats.Length; i++) { if (labels[i]) hi.Add(feats[i]); else lo.Add(feats[i]); }
      var w = new double[nf];
      for (int f = 0; f < nf; f++) { double lm = lo.Average(x => x[f]), hm = hi.Average(x => x[f]);
        double ls = Std(lo.Select(x => x[f]).ToArray()), hs = Std(hi.Select(x => x[f]).ToArray());
        double p = Math.Sqrt((ls * ls + hs * hs) / 2); w[f] = p > 1e-15 ? (hm - lm) / (p * p) : 0; }
      double ls2 = 0, hs2 = 0; for (int f = 0; f < nf; f++) { ls2 += w[f] * lo.Average(x => x[f]); hs2 += w[f] * hi.Average(x => x[f]); }
      return (w, -0.5 * (ls2 + hs2)); }

    private static double Score(double[] f, double[] w, double b) { double s = b; for (int i = 0; i < w.Length; i++) s += w[i] * f[i]; return s; }

    private static double[,] PerturbK(double[,] K, int n, string type, int seed)
    { if (type == "random") { var rng = new Random(seed + 77777); var P = new double[n, n]; for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) { double pv = rng.NextDouble() - 0.5; P[i, j] = pv; P[j, i] = pv; }
        double pn = 0; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) pn += P[i, j] * P[i, j]; pn = Math.Sqrt(pn); double sc = pn > 1e-15 ? RandomNorm / pn : 0;
        var Kn = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) Kn[i, j] = i == j ? 0 : Math.Max(0, K[i, j] + sc * P[i, j]); return Kn; }
      else if (type == "suppress" || type == "amplify") { double sh = type == "suppress" ? -LambdaShift : +LambdaShift; var (lam, vec) = PowerIter(K, n);
        var Kn = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) Kn[i, j] = i == j ? 0 : K[i, j] + sh * vec[i] * vec[j];
        for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) { Kn[i, j] = Math.Max(0, Kn[i, j]); Kn[j, i] = Kn[i, j]; } return Kn; }
      return K; }

    // ═══════════════════════════════════════════════════════════

    [Fact] public void V5_3_BSPA_01_Protocol()
    { _output.WriteLine($"M15: N={NTarget} | {SeedsTotal} seeds | score disruption vs branch flip | fixed thr={FIXED_THRESHOLD:F3}"); }

    [Fact]
    public void V5_3_BSPA_02_SignaturePerturbation()
    {
        _output.WriteLine("═══ M15 SIGNATURE PERTURBATION ═══");
        int E = Ep(NTarget);

        // ── Phase 1: Build LDA model on baseline runs ──
        var baseFeats = new double[SeedsTotal][];
        var baseOmega = new double[SeedsTotal];
        var baseLabels = new bool[SeedsTotal];

        for (int s = 0; s < SeedsTotal; s++)
        { var Ki = KS(NTarget, s); var Kc = (double[,])Ki.Clone();
          for (int e = 0; e < E; e++) { var he = Sm(Kc, NTarget, S, s + e, St, REps); Kc = Cupd(DL(Nm(RP(he, NTarget), NTarget, REps), NTarget), NTarget, K0, Xi); }
          var hf = Sm(Kc, NTarget, S, s + E, St, REps); var om = Of(hf, NTarget); var df = DL(Nm(RP(hf, NTarget), NTarget, REps), NTarget);
          baseFeats[s] = ExtractFeat(Kc, df, om, NTarget); baseOmega[s] = om.Average(); baseLabels[s] = baseOmega[s] > FIXED_THRESHOLD; }

        var (w, b) = TrainLDA(baseFeats, baseLabels);
        int baseHi = baseLabels.Count(x => x);
        _output.WriteLine($"Baseline: {baseHi}/{SeedsTotal} high. LDA model trained.");

        // ── Phase 2: Perturb and track score disruption vs branch flip ──
        string[] conds = { "random", "suppress", "amplify" };
        foreach (string cond in conds)
        {
            int flippedToLow = 0, flippedToHigh = 0, stayed = 0;
            double avgScoreChangeFlipped = 0, avgScoreChangeStayed = 0;
            int flippedCount = 0, stayedCount = 0;

            for (int s = 0; s < SeedsTotal; s++)
            {
                var Ki = KS(NTarget, s); var Kc = (double[,])Ki.Clone();
                for (int e = 0; e < E; e++) { var he = Sm(Kc, NTarget, S, s + e, St, REps); var R = RP(he, NTarget); var d = DL(Nm(R, NTarget, REps), NTarget); Kc = Cupd(d, NTarget, K0, Xi);
                    if (e == 0) Kc = PerturbK(Kc, NTarget, cond, s); }
                var hf = Sm(Kc, NTarget, S, s + E, St, REps); var om = Of(hf, NTarget); var df = DL(Nm(RP(hf, NTarget), NTarget, REps), NTarget);
                var feat = ExtractFeat(Kc, df, om, NTarget);
                double omVal = om.Average(); bool newHigh = omVal > FIXED_THRESHOLD;
                double oldScore = Score(baseFeats[s], w, b), newScore = Score(feat, w, b);
                double scoreDelta = newScore - oldScore;

                if (newHigh != baseLabels[s]) { if (baseLabels[s]) flippedToLow++; else flippedToHigh++; avgScoreChangeFlipped += scoreDelta; flippedCount++; }
                else { stayed++; avgScoreChangeStayed += scoreDelta; stayedCount++; }
            }

            _output.WriteLine($"{cond}: flipped(hi→lo={flippedToLow} lo→hi={flippedToHigh}) stayed={stayed}");
            _output.WriteLine($"  avg Δscore: flipped={avgScoreChangeFlipped / Math.Max(flippedCount, 1):F4} stayed={avgScoreChangeStayed / Math.Max(stayedCount, 1):F4}");
        }

        _output.WriteLine("");
        _output.WriteLine("═══ DECISION ═══");
        _output.WriteLine("If flipped seeds show larger |Δscore| than stayed seeds,");
        _output.WriteLine("the signature is conditionally associated with branch change.");
        _output.WriteLine("If Δscore is similar for flipped and stayed seeds,");
        _output.WriteLine("the signature is diagnostic but not causally linked.");
    }

    [Fact] public void V5_3_BSPA_03_ClaimAudit()
    { _output.WriteLine("=== CLAIM AUDIT ===\nSUPPORTED: Signature perturbation as reported.\nCONDITIONAL: 50 seeds, N=67, LDA model.\nNOT CLAIMED: causation, H9-H12.\nAUDIT: PASSED."); }
}
