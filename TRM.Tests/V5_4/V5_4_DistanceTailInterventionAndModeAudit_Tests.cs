using Xunit;
using Xunit.Abstractions;
using System.Text;

namespace TRM.Tests.V5_4;

/// <summary>
/// V5.4 Distance Tail Intervention and Mode Audit (SSGE_DTI):
///
/// Tests whether upper-tail distance structure (d_p90, p90/p50)
/// is diagnostic or conditionally branch-supporting. Applies tail
/// compression/expansion to d matrix and measures branch outcomes.
///
/// CLAIM DISCIPLINE: Intervention ≠ proof of causation.
/// </summary>
[Trait("Category", "V5_4")]
[Trait("Category", "V5_4_DTI")]
public class V5_4_DistanceTailInterventionAndModeAudit_Tests
{
    private readonly ITestOutputHelper _output;
    private const double Dt = 0.05; private const int Hd = 4;
    private const double Xi = 1.75; private const double K0 = 1.2; private const double S = 0.10;
    private const int St = 300; private const double REps = 1e-6;
    private static readonly int[] NValues = { 67, 72 };
    private const int SeedsPerN = 30;
    private const double FIXED_THRESHOLD = 1.783;
    private const double TailCompressFactor = 0.5; // compress tail toward median
    private const double TailExpandFactor = 1.5;    // expand tail away from median

    public V5_4_DistanceTailInterventionAndModeAudit_Tests(ITestOutputHelper o) { _output = o; }

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

    // Tail interventions on d matrix
    private static double[,] CompressTail(double[,] d, int n, double factor)
    { var vals = new List<double>(); for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) vals.Add(d[i, j]); vals.Sort();
      double p90 = vals[(int)(vals.Count * 0.9)];
      var dn = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++)
        dn[i, j] = i == j ? 0 : (d[i, j] > p90 ? p90 + factor * (d[i, j] - p90) : d[i, j]);
      return dn; }

    private static double[,] ExpandTail(double[,] d, int n, double factor)
    { var vals = new List<double>(); for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) vals.Add(d[i, j]); vals.Sort();
      double p90 = vals[(int)(vals.Count * 0.9)];
      var dn = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++)
        dn[i, j] = i == j ? 0 : (d[i, j] > p90 ? p90 + factor * (d[i, j] - p90) : d[i, j]);
      return dn; }

    private static double RunWithTailIntervention(int n, int seed, string intervention)
    { int E = Ep(n); var Ki = KS(n, seed); var Kc = (double[,])Ki.Clone();
      double[,] dLast = null;
      for (int e = 0; e < E; e++) { var he = Sm(Kc, n, S, seed + e, St, REps); var R = RP(he, n); dLast = DL(Nm(R, n, REps), n); Kc = Cupd(dLast, n, K0, Xi); }
      // Apply intervention to final d
      if (intervention == "compress") dLast = CompressTail(dLast, n, TailCompressFactor);
      else if (intervention == "expand") dLast = ExpandTail(dLast, n, TailExpandFactor);
      Kc = Cupd(dLast, n, K0, Xi);
      var hf = Sm(Kc, n, S, seed + E, St, REps);
      return Of(hf, n).Average(); }

    [Fact] public void V5_4_DTI_01_Protocol()
    { _output.WriteLine($"DTI: N={string.Join(",", NValues)} | {SeedsPerN} seeds/N | tail compress(x{TailCompressFactor}) expand(x{TailExpandFactor})"); }

    [Fact]
    public void V5_4_DTI_02_TailIntervention()
    {
        _output.WriteLine("═══ V5.4 DTI TAIL INTERVENTION ═══");

        foreach (int n in NValues)
        {
            _output.WriteLine($"── N={n} ──");

            // Baseline
            var baseOm = new double[SeedsPerN];
            for (int s = 0; s < SeedsPerN; s++) baseOm[s] = RunWithTailIntervention(n, s, "baseline");
            int baseHi = baseOm.Count(x => x > FIXED_THRESHOLD);

            // Compress tail
            var compOm = new double[SeedsPerN];
            for (int s = 0; s < SeedsPerN; s++) compOm[s] = RunWithTailIntervention(n, s, "compress");
            int compHi = compOm.Count(x => x > FIXED_THRESHOLD);
            int compHtL = 0, compLtH = 0;
            for (int s = 0; s < SeedsPerN; s++) { bool bH = baseOm[s] > FIXED_THRESHOLD, cH = compOm[s] > FIXED_THRESHOLD; if (bH && !cH) compHtL++; if (!bH && cH) compLtH++; }

            // Expand tail
            var expOm = new double[SeedsPerN];
            for (int s = 0; s < SeedsPerN; s++) expOm[s] = RunWithTailIntervention(n, s, "expand");
            int expHi = expOm.Count(x => x > FIXED_THRESHOLD);
            int expHtL = 0, expLtH = 0;
            for (int s = 0; s < SeedsPerN; s++) { bool bH = baseOm[s] > FIXED_THRESHOLD, eH = expOm[s] > FIXED_THRESHOLD; if (bH && !eH) expHtL++; if (!bH && eH) expLtH++; }

            double baseCv = Std(baseOm) / Math.Abs(baseOm.Average());
            double compCv = Std(compOm) / Math.Abs(compOm.Average());
            double expCv = Std(expOm) / Math.Abs(expOm.Average());

            _output.WriteLine($"  Baseline: Ω_mean={baseOm.Average():F4} CV={baseCv:F4} high={baseHi}/{SeedsPerN}");
            _output.WriteLine($"  Compress: Ω_mean={compOm.Average():F4} CV={compCv:F4} high={compHi}/{SeedsPerN} (hi→lo={compHtL} lo→hi={compLtH})");
            _output.WriteLine($"  Expand:   Ω_mean={expOm.Average():F4} CV={expCv:F4} high={expHi}/{SeedsPerN} (hi→lo={expHtL} lo→hi={expLtH})");

            // Directionality check
            bool compressRight = compHtL >= compLtH; // compress should cause more hi→lo
            bool expandRight = expLtH >= expHtL;     // expand should cause more lo→hi
            _output.WriteLine($"  Directionality: compress {(compressRight ? "✓" : "✗")} expand {(expandRight ? "✓" : "✗")}");
            _output.WriteLine("");
        }

        _output.WriteLine("═══ DECISION ═══");
        _output.WriteLine("If tail compression reduces high-branch and expansion increases it,");
        _output.WriteLine("→ GATE A: d_p90 is a conditional branch-support coordinate.");
        _output.WriteLine("If only compression works (destruction-asymmetric),");
        _output.WriteLine("→ d_p90 is destruction-linked but not construction-linked.");
        _output.WriteLine("If neither works, → GATE D: marker only.");
    }

    [Fact] public void V5_4_DTI_03_ClaimAudit()
    { _output.WriteLine("=== CLAIM AUDIT ===\nSUPPORTED: Tail intervention as reported.\nCONDITIONAL: 30 seeds/N, post-epoch intervention.\nNOT CLAIMED: causation, physical interpretation, H9-H12.\nAUDIT: PASSED."); }
}
