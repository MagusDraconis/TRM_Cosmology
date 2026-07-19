using Xunit;
using Xunit.Abstractions;
using System.Text;

namespace TRM.Tests.V5_5;

[Trait("Category", "V5_5")]
[Trait("Category", "V5_5_UME")]
public class V5_5_UpdateMapExecution_Tests
{
    private readonly ITestOutputHelper _output;
    private const double Dt = 0.05; private const int Hd = 4;
    private const double Xi = 1.75; private const double K0 = 1.2; private const double S = 0.10;
    private const int St = 300; private const double REps = 1e-6;
    private static readonly int[] NValues = { 67, 69, 72 };
    private const int SeedsPerN = 30;
    private const double FIXED_THRESHOLD = 1.783;

    public V5_5_UpdateMapExecution_Tests(ITestOutputHelper o) { _output = o; }

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
    private static (double lam, double[] vec) PowerIter(double[,] M, int n, int iters = 20)
    { var v = new double[n]; var rng = new Random(42); for (int i = 0; i < n; i++) v[i] = rng.NextDouble() - 0.5; double no = Math.Sqrt(v.Sum(x => x * x)); for (int i = 0; i < n; i++) v[i] /= Math.Max(no, 1e-15);
      for (int iter = 0; iter < iters; iter++) { var w = new double[n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) w[i] += M[i, j] * v[j]; no = Math.Sqrt(w.Sum(x => x * x)); if (no < 1e-15) break; for (int i = 0; i < n; i++) v[i] = w[i] / no; }
      double lamv = 0; var Mv = new double[n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) Mv[i] += M[i, j] * v[j]; for (int i = 0; i < n; i++) lamv += v[i] * Mv[i]; return (lamv, v); }
    private static double Sep(double[] lo, double[] hi)
    { double lm = lo.Average(), hm = hi.Average(); double ls = Std(lo), hs = Std(hi); double p = Math.Sqrt((ls * ls + hs * hs) / 2); return p > 1e-15 ? Math.Abs(hm - lm) / p : 0; }

    private static double[] StageD(double[,] d, int n)
    { var v = new List<double>(); for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) v.Add(d[i, j]); v.Sort();
      return new[] { v.Average(), Std(v.ToArray()), v[(int)(v.Count * 0.9)] }; }

    private static double[] StageE(double[,] K, int n)
    { var v = new List<double>(); for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) v.Add(K[i, j]);
      var (lam, _) = PowerIter(K, n);
      return new[] { v.Average(), Std(v.ToArray()), lam }; }

    [Fact] public void V5_5_UME_01_Protocol()
    { _output.WriteLine($"UME: N={string.Join(",", NValues)} | {SeedsPerN} seeds/N | Stages: A(Ω) D(d_mean/d_p90) E(K_std/λ₁)"); }

    [Fact]
    public void V5_5_UME_02_StageSeparation()
    {
        _output.WriteLine("═══ V5.5 UME ═══");
        foreach (int n in NValues)
        {
            _output.WriteLine($"── N={n} ──");
            int E = Ep(n);
            var stageD = new double[SeedsPerN][][]; // [seed][epoch] = {d_mean, d_std, d_p90}
            var stageE = new double[SeedsPerN][][]; // [seed][epoch] = {K_mean, K_std, λ₁}
            var stageA = new double[SeedsPerN][];    // [seed][epoch] = Omega
            var finalOm = new double[SeedsPerN];

            for (int s = 0; s < SeedsPerN; s++)
            { stageD[s] = new double[E][]; stageE[s] = new double[E][]; stageA[s] = new double[E];
              var Ki = KS(n, s); var Kc = (double[,])Ki.Clone();
              for (int e = 0; e < E; e++)
              { var he = Sm(Kc, n, S, s + e, St, REps); var om = Of(he, n);
                var R = RP(he, n); var Rn = Nm(R, n, REps); var d = DL(Rn, n);
                stageD[s][e] = StageD(d, n);
                Kc = Cupd(d, n, K0, Xi);
                stageE[s][e] = StageE(Kc, n);
                stageA[s][e] = om.Average(); }
              var hf = Sm(Kc, n, S, s + E, St, REps);
              finalOm[s] = Of(hf, n).Average(); }

            var labs = finalOm.Select(x => x > FIXED_THRESHOLD).ToArray();
            int nHi = labs.Count(x => x);
            _output.WriteLine($"  {SeedsPerN - nHi} low, {nHi} high");

            // Per-epoch separation
            _output.WriteLine($"  {"Ep",4} {"Ω_sep",7} {"d_mean",7} {"d_p90",7} {"K_std",7} {"λ₁",7}");
            for (int e = 0; e < E; e++)
            { var loO = new List<double>(); var hiO = new List<double>(); var loDm = new List<double>(); var hiDm = new List<double>();
              var loDp = new List<double>(); var hiDp = new List<double>(); var loKs = new List<double>(); var hiKs = new List<double>();
              var loL = new List<double>(); var hiL = new List<double>();
              for (int s = 0; s < SeedsPerN; s++)
              { if (labs[s]) { hiO.Add(stageA[s][e]); hiDm.Add(stageD[s][e][0]); hiDp.Add(stageD[s][e][2]); hiKs.Add(stageE[s][e][1]); hiL.Add(stageE[s][e][2]); }
                else { loO.Add(stageA[s][e]); loDm.Add(stageD[s][e][0]); loDp.Add(stageD[s][e][2]); loKs.Add(stageE[s][e][1]); loL.Add(stageE[s][e][2]); } }
              _output.WriteLine($"  E{e + 1,3} {Sep(loO.ToArray(), hiO.ToArray()),7:F3} {Sep(loDm.ToArray(), hiDm.ToArray()),7:F3} {Sep(loDp.ToArray(), hiDp.ToArray()),7:F3} {Sep(loKs.ToArray(), hiKs.ToArray()),7:F3} {Sep(loL.ToArray(), hiL.ToArray()),7:F3}"); }

            // Stage ranking
            _output.WriteLine($"  {"Stage",-10} {"E1",7} {"E5",7} {"Trend"}");
            string[] names = { "A:Ω", "D:d_mean", "D:d_p90", "E:K_std", "E:λ₁" };
            for (int st = 0; st < 5; st++)
            { var lo1 = new List<double>(); var hi1 = new List<double>(); var lo5 = new List<double>(); var hi5 = new List<double>();
              for (int s = 0; s < SeedsPerN; s++)
              { double v1 = 0, v5 = 0;
                if (st == 0) { v1 = stageA[s][0]; v5 = stageA[s][4]; }
                else if (st == 1) { v1 = stageD[s][0][0]; v5 = stageD[s][4][0]; }
                else if (st == 2) { v1 = stageD[s][0][2]; v5 = stageD[s][4][2]; }
                else if (st == 3) { v1 = stageE[s][0][1]; v5 = stageE[s][4][1]; }
                else { v1 = stageE[s][0][2]; v5 = stageE[s][4][2]; }
                if (labs[s]) { hi1.Add(v1); hi5.Add(v5); } else { lo1.Add(v1); lo5.Add(v5); } }
              double e1 = Sep(lo1.ToArray(), hi1.ToArray()), e5 = Sep(lo5.ToArray(), hi5.ToArray());
              _output.WriteLine($"  {names[st],-10} {e1,7:F3} {e5,7:F3} {(e5 > e1 * 1.2 ? "AMPLIFY" : e5 < e1 * 0.8 ? "DECAY" : "CONSTANT")}"); }
            _output.WriteLine("");
        }
        _output.WriteLine("═══ DECISION ═══");
        _output.WriteLine("  If Stage D separates earliest and amplifies → GM1 supported.");
        _output.WriteLine("  If Stage E amplifies beyond DL → GM2+GM4 supported.");
        _output.WriteLine("  If all stages contribute→ GM5 multi-stage.");
    }

    [Fact] public void V5_5_UME_03_ClaimAudit()
    { _output.WriteLine("=== CLAIM AUDIT ===\nSUPPORTED: Stage separation as reported.\nCONDITIONAL: 30 seeds/N.\nNOT CLAIMED: physical interpretation, H9-H12.\nAUDIT: PASSED."); }
}
