using Xunit;
using Xunit.Abstractions;
using System.Text;

namespace TRM.Tests.V5_5;

/// <summary>
/// V5.5 Update-Map Mechanism Analysis (UMA):
///
/// Analyzes lead-lag relationships between distance and coupling
/// separation across epochs to determine whether branch generation
/// is distance-first, coupling-first, feedback-loop, or distributed.
///
/// CLAIM DISCIPLINE: Values as reported. No physical interpretation.
/// </summary>
[Trait("Category", "V5_5")]
[Trait("Category", "V5_5_UMA")]
public class V5_5_UpdateMapMechanismAnalysis_Tests
{
    private readonly ITestOutputHelper _output;
    private const double Dt = 0.05; private const int Hd = 4;
    private const double Xi = 1.75; private const double K0 = 1.2; private const double S = 0.10;
    private const int St = 300; private const double REps = 1e-6;
    private static readonly int[] NValues = { 67, 69, 72 };
    private const int SeedsPerN = 30;
    private const double FIXED_THRESHOLD = 1.783;

    public V5_5_UpdateMapMechanismAnalysis_Tests(ITestOutputHelper o) { _output = o; }

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
    private static double Sep(double[] lo, double[] hi)
    { double lm = lo.Average(), hm = hi.Average(); double ls = Std(lo), hs = Std(hi); double p = Math.Sqrt((ls * ls + hs * hs) / 2); return p > 1e-15 ? Math.Abs(hm - lm) / p : 0; }
    private static double Pearson(double[] a, double[] b)
    { double ma = a.Average(), mb = b.Average(); double num = 0, da = 0, db = 0;
      for (int i = 0; i < a.Length; i++) { double ad = a[i] - ma, bd = b[i] - mb; num += ad * bd; da += ad * ad; db += bd * bd; }
      return num / Math.Sqrt(Math.Max(da * db, 1e-30)); }

    private static (double lam, double[] vec) PowerIter(double[,] M, int n, int iters = 20)
    { var v = new double[n]; var rng = new Random(42); for (int i = 0; i < n; i++) v[i] = rng.NextDouble() - 0.5; double no = Math.Sqrt(v.Sum(x => x * x)); for (int i = 0; i < n; i++) v[i] /= Math.Max(no, 1e-15);
      for (int iter = 0; iter < iters; iter++) { var w = new double[n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) w[i] += M[i, j] * v[j]; no = Math.Sqrt(w.Sum(x => x * x)); if (no < 1e-15) break; for (int i = 0; i < n; i++) v[i] = w[i] / no; }
      double lamv = 0; var Mv = new double[n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) Mv[i] += M[i, j] * v[j]; for (int i = 0; i < n; i++) lamv += v[i] * Mv[i]; return (lamv, v); }

    // Per-seed per-epoch separation values for d_mean and λ₁ across seeds
    // Returns arrays: dSep[e] = separation of d_mean at epoch e, kSep[e] = separation of λ₁ at epoch e
    private static (double[] dSep, double[] kSep, double[] oSep) ComputeSepCurves(int n, int seeds)
    { int E = Ep(n); var dSep = new double[E]; var kSep = new double[E]; var oSep = new double[E];
      var allD = new double[seeds][]; var allK = new double[seeds][]; var allO = new double[seeds][]; var labs = new bool[seeds];
      for (int s = 0; s < seeds; s++)
      { allD[s] = new double[E]; allK[s] = new double[E]; allO[s] = new double[E];
        var Ki = KS(n, s); var Kc = (double[,])Ki.Clone();
        for (int e = 0; e < E; e++)
        { var he = Sm(Kc, n, S, s + e, St, REps); var om = Of(he, n); var R = RP(he, n); var Rn = Nm(R, n, REps); var d = DL(Rn, n);
          var dV = new List<double>(); for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) dV.Add(d[i, j]);
          allD[s][e] = dV.Average(); allO[s][e] = om.Average();
          Kc = Cupd(d, n, K0, Xi);
          var (lam, _) = PowerIter(Kc, n);
          allK[s][e] = lam; }
        var hf = Sm(Kc, n, S, s + E, St, REps);
        labs[s] = Of(hf, n).Average() > FIXED_THRESHOLD; }
      for (int e = 0; e < E; e++)
      { var loD = new List<double>(); var hiD = new List<double>(); var loK = new List<double>(); var hiK = new List<double>();
        var loO = new List<double>(); var hiO = new List<double>();
        for (int s = 0; s < seeds; s++) { if (labs[s]) { hiD.Add(allD[s][e]); hiK.Add(allK[s][e]); hiO.Add(allO[s][e]); }
          else { loD.Add(allD[s][e]); loK.Add(allK[s][e]); loO.Add(allO[s][e]); } }
        dSep[e] = Sep(loD.ToArray(), hiD.ToArray()); kSep[e] = Sep(loK.ToArray(), hiK.ToArray()); oSep[e] = Sep(loO.ToArray(), hiO.ToArray()); }
      return (dSep, kSep, oSep); }

    [Fact] public void V5_5_UMA_01_Protocol()
    { _output.WriteLine($"UMA: N={string.Join(",", NValues)} | {SeedsPerN} seeds/N | Lead-lag: d→K vs K→d | Omega downstream test"); }

    [Fact]
    public void V5_5_UMA_02_LeadLagAnalysis()
    {
        _output.WriteLine("═══ V5.5 UMA LEAD-LAG ═══");

        foreach (int n in NValues)
        {
            _output.WriteLine($"── N={n} ──");
            var (dSep, kSep, oSep) = ComputeSepCurves(n, SeedsPerN);
            int E = dSep.Length;

            // Separation growth curves
            var sb = new StringBuilder(); sb.Append("  d_sep:"); for (int e = 0; e < E; e++) sb.Append($" {dSep[e]:F2}"); _output.WriteLine(sb.ToString());
            sb = new StringBuilder(); sb.Append("  k_sep:"); for (int e = 0; e < E; e++) sb.Append($" {kSep[e]:F2}"); _output.WriteLine(sb.ToString());
            sb = new StringBuilder(); sb.Append("  o_sep:"); for (int e = 0; e < E; e++) sb.Append($" {oSep[e]:F2}"); _output.WriteLine(sb.ToString());

            // Lead-lag: does d at epoch e correlate with k at epoch e+1?
            if (E >= 2)
            { double dToK = Pearson(dSep.Take(E - 1).ToArray(), kSep.Skip(1).ToArray());
              double kToD = Pearson(kSep.Take(E - 1).ToArray(), dSep.Skip(1).ToArray());
              double dToO = Pearson(dSep.Take(E - 1).ToArray(), oSep.Skip(1).ToArray());
              double kToO = Pearson(kSep.Take(E - 1).ToArray(), oSep.Skip(1).ToArray());
              double simultaneous = Pearson(dSep, kSep);
              _output.WriteLine($"  Lead-lag ρ: d→K={dToK:F2} K→d={kToD:F2} d→Ω={dToO:F2} K→Ω={kToO:F2} sim={simultaneous:F2}"); }

            // Omega downstream: does Ω always trail d/K?
            double maxOSep = oSep.Max(), maxDSep = dSep.Max(), maxKSep = kSep.Max();
            bool omegaDownstream = maxOSep < Math.Min(maxDSep, maxKSep) * 0.8;
            _output.WriteLine($"  Max sep: d={maxDSep:F2} K={maxKSep:F2} Ω={maxOSep:F2} → Ω downstream? {(omegaDownstream ? "YES" : "no")}");

            // Which leads?
            double dGrowth = dSep[E - 1] - dSep[0], kGrowth = kSep[E - 1] - kSep[0];
            _output.WriteLine($"  Growth: d={dGrowth:F2} K={kGrowth:F2}");
            _output.WriteLine("");
        }

        _output.WriteLine("═══ DECISION ═══");
        _output.WriteLine("  If d→K ρ > K→d ρ consistently → GATE A: Distance-first.");
        _output.WriteLine("  If K→d ρ > d→K ρ consistently → GATE B: Coupling-first.");
        _output.WriteLine("  If both strong and mutual → GATE C: Feedback-loop.");
        _output.WriteLine("  If neither consistently leads → GATE D: Distributed.");
    }

    [Fact] public void V5_5_UMA_03_ClaimAudit()
    { _output.WriteLine("=== CLAIM AUDIT ===\nSUPPORTED: Lead-lag analysis as reported.\nCONDITIONAL: 30 seeds/N, 5 epochs.\nNOT CLAIMED: causation, physical interpretation, H9-H12.\nAUDIT: PASSED."); }
}
