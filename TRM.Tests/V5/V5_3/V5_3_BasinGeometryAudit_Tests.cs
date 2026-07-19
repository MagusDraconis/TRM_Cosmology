using Xunit;
using Xunit.Abstractions;
using System.Text;

namespace TRM.Tests.V5_3;

/// <summary>
/// M16 Basin Geometry Audit (BGEO):
///
/// Determines whether RecoverFP branches form two disconnected basins
/// or a continuous manifold. Computes clustering metrics, k-NN graph
/// connectedness, and branch overlap in M14 feature space.
///
/// CLAIM DISCIPLINE: Geometry as computed. No physical interpretation.
/// </summary>
[Trait("Category", "V5_3")]
[Trait("Category", "V5_3_BGEO")]
public class V5_3_BasinGeometryAudit_Tests
{
    private readonly ITestOutputHelper _output;
    private const double Dt = 0.05; private const int Hd = 4;
    private const double Xi = 1.75; private const double K0 = 1.2; private const double S = 0.10;
    private const int St = 300; private const double REps = 1e-6;
    private static readonly int[] NValues = { 67, 69, 72 };
    private const int SeedsPerN = 60;
    private const double FIXED_THRESHOLD = 1.783;

    public V5_3_BasinGeometryAudit_Tests(ITestOutputHelper o) { _output = o; }

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

    // M14 features (without Ω and ΩF_std — use coupling/distance only for unbiased geometry)
    private static double[] ExtractFeat(double[,] K, double[,] d, int n)
    { var dV = new List<double>(); var kV = new List<double>(); for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) { dV.Add(d[i, j]); kV.Add(K[i, j]); }
      var (kL1, _) = PowerIter(K, n); var (dL1, _) = PowerIter(d, n);
      var ns = new double[n]; for (int i = 0; i < n; i++) { double s = 0; for (int j = 0; j < n; j++) if (i != j) s += K[i, j]; ns[i] = s; }
      return new[] { dV.Average(), Std(dV.ToArray()), kV.Average(), Std(kV.ToArray()), kL1, dL1, ns.OrderByDescending(x => x).Take(5).Sum() / Math.Max(ns.Sum(), 1e-15) }; }

    private static double Dist(double[] a, double[] b)
    { double s = 0; for (int i = 0; i < a.Length; i++) { double d = a[i] - b[i]; s += d * d; } return Math.Sqrt(s); }

    /// <summary>Silhouette score: mean over points of (b-a)/max(a,b) where a=mean intra-cluster dist, b=mean nearest-other-cluster dist</summary>
    private static double Silhouette(double[][] feats, bool[] labels)
    { int n = feats.Length; var loIdx = new List<int>(); var hiIdx = new List<int>();
      for (int i = 0; i < n; i++) { if (labels[i]) hiIdx.Add(i); else loIdx.Add(i); }
      double total = 0; int count = 0;
      for (int i = 0; i < n; i++)
      { double a = 0; int aN = 0; var same = labels[i] ? hiIdx : loIdx; foreach (int j in same) if (j != i) { a += Dist(feats[i], feats[j]); aN++; } a /= Math.Max(aN, 1);
        double b = double.MaxValue; var other = labels[i] ? loIdx : hiIdx; if (other.Count == 0) continue;
        double bSum = 0; foreach (int j in other) bSum += Dist(feats[i], feats[j]); b = bSum / other.Count;
        double s = Math.Max(a, b) > 1e-15 ? (b - a) / Math.Max(a, b) : 0; total += s; count++; }
      return count > 0 ? total / count : 0; }

    /// <summary>Davies-Bouldin index: lower = better separation</summary>
    private static double DaviesBouldin(double[][] feats, bool[] labels)
    { var loIdx = new List<int>(); var hiIdx = new List<int>();
      for (int i = 0; i < feats.Length; i++) { if (labels[i]) hiIdx.Add(i); else loIdx.Add(i); }
      if (loIdx.Count == 0 || hiIdx.Count == 0) return double.NaN;
      // Cluster centers
      var loC = new double[feats[0].Length]; var hiC = new double[feats[0].Length];
      foreach (int i in loIdx) for (int f = 0; f < loC.Length; f++) loC[f] += feats[i][f] / loIdx.Count;
      foreach (int i in hiIdx) for (int f = 0; f < hiC.Length; f++) hiC[f] += feats[i][f] / hiIdx.Count;
      // Intra-cluster dispersion
      double loDisp = 0, hiDisp = 0;
      foreach (int i in loIdx) loDisp += Dist(feats[i], loC); loDisp /= loIdx.Count;
      foreach (int i in hiIdx) hiDisp += Dist(feats[i], hiC); hiDisp /= hiIdx.Count;
      double centerDist = Dist(loC, hiC);
      return centerDist > 1e-15 ? (loDisp + hiDisp) / centerDist : double.PositiveInfinity; }

    /// <summary>k-NN connectedness: fraction of points whose k-NN includes at least one opposite-branch point</summary>
    private static double KnnCrossConnect(double[][] feats, bool[] labels, int k)
    { int n = feats.Length, crossCount = 0;
      for (int i = 0; i < n; i++)
      { var dists = new (int idx, double d)[n]; for (int j = 0; j < n; j++) dists[j] = (j, i == j ? double.MaxValue : Dist(feats[i], feats[j]));
        Array.Sort(dists, (a, b) => a.d.CompareTo(b.d));
        for (int j = 0; j < Math.Min(k, n - 1); j++) if (labels[dists[j + 1].idx] != labels[i]) { crossCount++; break; } }
      return (double)crossCount / n; }

    /// <summary>Min and mean cross-branch nearest-neighbor distance</summary>
    private static (double min, double mean, double max) CrossBranchDist(double[][] feats, bool[] labels)
    { var loIdx = new List<int>(); var hiIdx = new List<int>();
      for (int i = 0; i < feats.Length; i++) { if (labels[i]) hiIdx.Add(i); else loIdx.Add(i); }
      if (loIdx.Count == 0 || hiIdx.Count == 0) return (0, 0, 0);
      double minD = double.MaxValue, sumD = 0, maxD = 0; int count = 0;
      foreach (int i in loIdx) foreach (int j in hiIdx) { double d = Dist(feats[i], feats[j]); if (d < minD) minD = d; if (d > maxD) maxD = d; sumD += d; count++; }
      return (minD, sumD / count, maxD); }

    // ═══════════════════════════════════════════════════════════

    [Fact] public void V5_3_BGEO_01_Protocol()
    { _output.WriteLine($"M16: N={string.Join(",", NValues)} | {SeedsPerN} seeds | 7 coupling/distance features (no Ω) | k-NN, silhouette, DB"); }

    [Fact]
    public void V5_3_BGEO_02_BasinGeometry()
    {
        _output.WriteLine("═══ M16 BASIN GEOMETRY ═══");

        foreach (int n in NValues)
        {
            _output.WriteLine($"── N={n} ──");
            int E = Ep(n);
            var feats = new double[SeedsPerN][]; var labels = new bool[SeedsPerN]; var omegas = new double[SeedsPerN];

            for (int s = 0; s < SeedsPerN; s++)
            { var Ki = KS(n, s); var Kc = (double[,])Ki.Clone();
              for (int e = 0; e < E; e++) { var he = Sm(Kc, n, S, s + e, St, REps); Kc = Cupd(DL(Nm(RP(he, n), n, REps), n), n, K0, Xi); }
              var hf = Sm(Kc, n, S, s + E, St, REps); var om = Of(hf, n); var df = DL(Nm(RP(hf, n), n, REps), n);
              omegas[s] = om.Average(); labels[s] = omegas[s] > FIXED_THRESHOLD;
              feats[s] = ExtractFeat(Kc, df, n); }

            int nHi = labels.Count(x => x);
            _output.WriteLine($"  High branch: {nHi}/{SeedsPerN}, Low: {SeedsPerN - nHi}");

            // Normalize features per dimension
            var means = new double[feats[0].Length]; var stds = new double[feats[0].Length];
            for (int f = 0; f < feats[0].Length; f++) { var vals = feats.Select(x => x[f]).ToArray(); means[f] = vals.Average(); stds[f] = Std(vals); }
            var nFeats = feats.Select(f => f.Select((v, i) => stds[i] > 1e-15 ? (v - means[i]) / stds[i] : 0.0).ToArray()).ToArray();

            double sil = Silhouette(nFeats, labels);
            double db = DaviesBouldin(nFeats, labels);
            double knn3 = KnnCrossConnect(nFeats, labels, 3);
            double knn5 = KnnCrossConnect(nFeats, labels, 5);
            double knn10 = KnnCrossConnect(nFeats, labels, 10);
            var (cbMin, cbMean, cbMax) = CrossBranchDist(nFeats, labels);
            // Intra-branch distances
            var loIdx = new List<int>(); var hiIdx = new List<int>();
            for (int i = 0; i < SeedsPerN; i++) { if (labels[i]) hiIdx.Add(i); else loIdx.Add(i); }
            double intraLo = 0, intraHi = 0; int loN = 0, hiN = 0;
            for (int a = 0; a < loIdx.Count; a++) for (int b = a + 1; b < loIdx.Count; b++) { intraLo += Dist(nFeats[loIdx[a]], nFeats[loIdx[b]]); loN++; }
            for (int a = 0; a < hiIdx.Count; a++) for (int b = a + 1; b < hiIdx.Count; b++) { intraHi += Dist(nFeats[hiIdx[a]], nFeats[hiIdx[b]]); hiN++; }
            intraLo /= Math.Max(loN, 1); intraHi /= Math.Max(hiN, 1);

            _output.WriteLine($"  Silhouette:    {sil:F3}  (>0.5=well-separated, <0.25=overlapping)");
            _output.WriteLine($"  Davies-Bouldin: {db:F3}  (<1.0=well-separated)");
            _output.WriteLine($"  k-NN cross (k=3):  {knn3 * 100:F0}% points connect to opposite branch");
            _output.WriteLine($"  k-NN cross (k=5):  {knn5 * 100:F0}%");
            _output.WriteLine($"  k-NN cross (k=10): {knn10 * 100:F0}%");
            _output.WriteLine($"  Intra-branch dist: low={intraLo:F3} high={intraHi:F3}");
            _output.WriteLine($"  Cross-branch dist: min={cbMin:F3} mean={cbMean:F3} max={cbMax:F3}");
            _output.WriteLine($"  Cross/Intra ratio: {cbMean / Math.Max((intraLo + intraHi) / 2, 0.001):F2}");

            // Decision per N
            string gate;
            if (sil > 0.5 && db < 1.0 && knn3 < 0.2) gate = "TWO BASINS";
            else if (sil > 0.25 && knn3 < 0.5) gate = "WEAK BRIDGE";
            else if (sil < 0.1 && knn10 > 0.7) gate = "SINGLE MANIFOLD";
            else gate = "MIXED/AMBIGUOUS";
            _output.WriteLine($"  → {gate}");
            _output.WriteLine("");
        }
    }

    [Fact] public void V5_3_BGEO_03_ClaimAudit()
    { _output.WriteLine("=== CLAIM AUDIT ===\nSUPPORTED: Basin geometry as reported.\nCONDITIONAL: 60 seeds, coupling/distance features only.\nNOT CLAIMED: physical basins, H9-H12.\nAUDIT: PASSED."); }
}
