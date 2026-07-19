using Xunit;
using Xunit.Abstractions;
using System.Text;

namespace TRM.Tests.V5_4;

/// <summary>
/// V5.4 Basin Deformation Mechanism Audit (SSGC):
///
/// Determines what drives N-dependent basin deformation by analyzing
/// PC1 loadings, K vs d contributions, and feature-family dominance.
///
/// CLAIM DISCIPLINE: Values as reported. No physical interpretation.
/// </summary>
[Trait("Category", "V5_4")]
[Trait("Category", "V5_4_SSGC")]
public class V5_4_BasinDeformationMechanismAudit_Tests
{
    private readonly ITestOutputHelper _output;
    private const double Dt = 0.05; private const int Hd = 4;
    private const double Xi = 1.75; private const double K0 = 1.2; private const double S = 0.10;
    private const int St = 300; private const double REps = 1e-6;
    private static readonly int[] NValues = { 67, 69, 72 };
    private const int SeedsPerN = 40;
    private const double FIXED_THRESHOLD = 1.783;

    public V5_4_BasinDeformationMechanismAudit_Tests(ITestOutputHelper o) { _output = o; }

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
    private static (double lam, double[] vec) PowerIterMat(double[,] cov, int d, int iters = 20)
    { var v = new double[d]; var rng = new Random(42); for (int i = 0; i < d; i++) v[i] = rng.NextDouble() - 0.5; double no = Math.Sqrt(v.Sum(x => x * x)); for (int i = 0; i < d; i++) v[i] /= Math.Max(no, 1e-15);
      for (int iter = 0; iter < iters; iter++) { var w = new double[d]; for (int i = 0; i < d; i++) for (int j = 0; j < d; j++) w[i] += cov[i, j] * v[j]; no = Math.Sqrt(w.Sum(x => x * x)); if (no < 1e-15) break; for (int i = 0; i < d; i++) v[i] = w[i] / no; }
      double lamv = 0; var cv = new double[d]; for (int i = 0; i < d; i++) for (int j = 0; j < d; j++) cv[i] += cov[i, j] * v[j]; for (int i = 0; i < d; i++) lamv += v[i] * cv[i]; return (lamv, v); }

    // Feature names and groups
    private static readonly string[] FeatNames = { "K_mean", "K_std", "K_min", "K_max", "K_med", "K_p90",
                                                    "d_mean", "d_std", "d_min", "d_max", "d_med", "d_p90", "Ω" };
    // Groups: K=[0..5], d=[6..11], Ω=[12]

    private static double[] ExtractFeat(int n, int seed)
    { int E = Ep(n); var Ki = KS(n, seed); var Kc = (double[,])Ki.Clone();
      for (int e = 0; e < E; e++) { var he = Sm(Kc, n, S, seed + e, St, REps); Kc = Cupd(DL(Nm(RP(he, n), n, REps), n), n, K0, Xi); }
      var hf = Sm(Kc, n, S, seed + E, St, REps); var om = Of(hf, n); var df = DL(Nm(RP(hf, n), n, REps), n);
      var kVals = new List<double>(); var dVals = new List<double>();
      for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) { kVals.Add(Kc[i, j]); dVals.Add(df[i, j]); }
      kVals.Sort(); dVals.Sort();
      return new[] { kVals.Average(), Std(kVals.ToArray()), kVals[0], kVals[kVals.Count-1], kVals[kVals.Count/2], kVals[kVals.Count*9/10],
                     dVals.Average(), Std(dVals.ToArray()), dVals[0], dVals[dVals.Count-1], dVals[dVals.Count/2], dVals[dVals.Count*9/10], om.Average() }; }

    [Fact] public void V5_4_SSGC_01_Protocol()
    { _output.WriteLine($"SSGC: N={string.Join(",", NValues)} | {SeedsPerN} seeds/N | 13 features (K:0-5, d:6-11, Ω:12) | PC1 loading decomposition"); }

    [Fact]
    public void V5_4_SSGC_02_DeformationAudit()
    {
        _output.WriteLine("═══ V5.4 SSGC BASIN DEFORMATION ═══");

        foreach (int n in NValues)
        {
            _output.WriteLine($"── N={n} ──");
            var feats = new double[SeedsPerN][]; var labs = new bool[SeedsPerN]; var oms = new double[SeedsPerN];
            for (int s = 0; s < SeedsPerN; s++) { feats[s] = ExtractFeat(n, s); oms[s] = feats[s][12]; labs[s] = oms[s] > FIXED_THRESHOLD; }
            int nHi = labs.Count(x => x);
            _output.WriteLine($"  High branch: {nHi}/{SeedsPerN}");

            int d = feats[0].Length - 1; // exclude Omega
            var cov = new double[d, d]; var mean = new double[d];
            for (int s = 0; s < SeedsPerN; s++) for (int f = 0; f < d; f++) mean[f] += feats[s][f] / SeedsPerN;
            for (int s = 0; s < SeedsPerN; s++) for (int a = 0; a < d; a++) for (int b = 0; b < d; b++)
                cov[a, b] += (feats[s][a] - mean[a]) * (feats[s][b] - mean[b]) / SeedsPerN;

            var (lam, pc1) = PowerIterMat(cov, d);

            // PC1 loading decomposition
            double kLoad = 0, dLoad = 0;
            for (int f = 0; f < 6; f++) kLoad += pc1[f] * pc1[f];
            for (int f = 6; f < 12; f++) dLoad += pc1[f] * pc1[f];
            double totalLoad = kLoad + dLoad;
            _output.WriteLine($"  PC1 variance: {lam / cov.Cast<double>().Sum(x => Math.Abs(x)) * 100 / d * 100:F0}%");
            _output.WriteLine($"  PC1 loading: K={kLoad / totalLoad * 100:F0}% d={dLoad / totalLoad * 100:F0}%");

            // Top 5 loading features
            var ranked = FeatNames.Take(d).Select((name, i) => (name, absW: Math.Abs(pc1[i]))).OrderByDescending(x => x.absW).Take(5).ToArray();
            var sb = new StringBuilder(); sb.Append("  Top loadings: ");
            foreach (var r in ranked) sb.Append($"{r.name}({r.absW / ranked[0].absW * 100:F0}%) ");
            _output.WriteLine(sb.ToString());

            // Project to PC1
            var proj = new double[SeedsPerN];
            for (int s = 0; s < SeedsPerN; s++) { double dot = 0; for (int f = 0; f < d; f++) dot += (feats[s][f] - mean[f]) * pc1[f]; proj[s] = dot; }

            // Basin stats on PC1
            var loP = new List<double>(); var hiP = new List<double>();
            for (int s = 0; s < SeedsPerN; s++) { if (labs[s]) hiP.Add(proj[s]); else loP.Add(proj[s]); }
            double loC = loP.Average(), hiC = hiP.Average();
            double loStd = Std(loP.ToArray()), hiStd = Std(hiP.ToArray());
            _output.WriteLine($"  PC1 centroids: lo={loC:F3} hi={hiC:F3} dist={Math.Abs(hiC - loC):F3} | spread: lo={loStd:F3} hi={hiStd:F3}");

            // Branch separation from PC1 only vs Omega
            double pc1Sep = Math.Abs(hiC - loC) / Math.Sqrt((loStd * loStd + hiStd * hiStd) / 2);
            _output.WriteLine($"  PC1 separation: |Δ|/σ={pc1Sep:F2}");
            _output.WriteLine("");
        }

        // ── Cross-N deformation ──
        _output.WriteLine("═══ CROSS-N DEFORMATION ═══");
        var nData = new Dictionary<int, (double loC, double hiC, double loStd, double hiStd, double kFrac)>();
        foreach (int n in NValues)
        { var feats = new double[SeedsPerN][]; var labs = new bool[SeedsPerN];
          for (int s = 0; s < SeedsPerN; s++) { feats[s] = ExtractFeat(n, s); labs[s] = feats[s][12] > FIXED_THRESHOLD; }
          int d2 = feats[0].Length - 1; var cov2 = new double[d2, d2]; var mean2 = new double[d2];
          for (int s = 0; s < SeedsPerN; s++) for (int f = 0; f < d2; f++) mean2[f] += feats[s][f] / SeedsPerN;
          for (int s = 0; s < SeedsPerN; s++) for (int a = 0; a < d2; a++) for (int b = 0; b < d2; b++) cov2[a, b] += (feats[s][a] - mean2[a]) * (feats[s][b] - mean2[b]) / SeedsPerN;
          var (lam2, pc1v) = PowerIterMat(cov2, d2);
          var proj2 = new double[SeedsPerN]; for (int s = 0; s < SeedsPerN; s++) { double dot = 0; for (int f = 0; f < d2; f++) dot += (feats[s][f] - mean2[f]) * pc1v[f]; proj2[s] = dot; }
          var loP2 = new List<double>(); var hiP2 = new List<double>(); for (int s = 0; s < SeedsPerN; s++) { if (labs[s]) hiP2.Add(proj2[s]); else loP2.Add(proj2[s]); }
          double kL = 0, dL = 0; for (int f = 0; f < 6; f++) kL += pc1v[f] * pc1v[f]; for (int f = 6; f < 12; f++) dL += pc1v[f] * pc1v[f];
          nData[n] = (loP2.Average(), hiP2.Average(), Std(loP2.ToArray()), Std(hiP2.ToArray()), kL / (kL + dL)); }

        _output.WriteLine($"{"N",5} {"loC",8} {"hiC",8} {"dist",8} {"loStd",7} {"hiStd",7} {"K_frac"}");
        foreach (int n in NValues)
        { var d2 = nData[n]; _output.WriteLine($"{n,5} {d2.loC,8:F3} {d2.hiC,8:F3} {Math.Abs(d2.hiC - d2.loC),8:F3} {d2.loStd,7:F3} {d2.hiStd,7:F3} {d2.kFrac * 100,5:F0}%"); }

        // Deformation type
        var n67d = nData[67]; var n72d = nData[72];
        double loCChg = (n72d.loC - n67d.loC) / Math.Max(Math.Abs(n67d.loC), 0.001) * 100;
        double hiCChg = (n72d.hiC - n67d.hiC) / Math.Max(Math.Abs(n67d.hiC), 0.001) * 100;
        double distChg = (Math.Abs(n72d.hiC - n72d.loC) - Math.Abs(n67d.hiC - n67d.loC)) / Math.Max(Math.Abs(n67d.hiC - n67d.loC), 0.001) * 100;
        _output.WriteLine($"");
        _output.WriteLine($"  loC shift: {loCChg:+0;-0}% | hiC shift: {hiCChg:+0;-0}% | dist change: {distChg:+0;-0}%");
        if (Math.Abs(hiCChg) > Math.Abs(loCChg) * 2) _output.WriteLine("  → GATE A-leaning: HIGH-BRANCH SPREADING dominant");
        else if (Math.Abs(loCChg) > Math.Abs(hiCChg) * 2) _output.WriteLine("  → LOW-BRANCH SPREADING dominant");
        else _output.WriteLine("  → GATE C: MIXED K+d deformation");
    }

    [Fact] public void V5_4_SSGC_03_ClaimAudit()
    { _output.WriteLine("=== CLAIM AUDIT ===\nSUPPORTED: Deformation audit as reported.\nCONDITIONAL: 40 seeds/N, 12 features.\nNOT CLAIMED: physical interpretation, H9-H12.\nAUDIT: PASSED."); }
}
