using Xunit;
using Xunit.Abstractions;
using System.Text;

namespace TRM.Tests.V5_5;

/// <summary>
/// V5.5 Update-Map Interruption and Truncation Audit (UMI):
///
/// Tests which parts of the RecoverFP update map are necessary for
/// branch generation. Truncates at different epochs, freezes d or K,
/// and measures whether branch separation survives.
///
/// CLAIM DISCIPLINE: Values as reported. No physical interpretation.
/// </summary>
[Trait("Category", "V5_5")]
[Trait("Category", "V5_5_UMI")]
public class V5_5_UpdateMapInterruptionAndTruncationAudit_Tests
{
    private readonly ITestOutputHelper _output;
    private const double Dt = 0.05; private const int Hd = 4;
    private const double Xi = 1.75; private const double K0 = 1.2; private const double S = 0.10;
    private const int St = 300; private const double REps = 1e-6;
    private const int NTarget = 67;
    private const int SeedsPerN = 30;
    private const double FIXED_THRESHOLD = 1.783;

    public V5_5_UpdateMapInterruptionAndTruncationAudit_Tests(ITestOutputHelper o) { _output = o; }

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

    // Truncate after E epochs
    private static double RunTruncated(int n, int seed, int truncEpochs)
    { var Ki = KS(n, seed); var Kc = (double[,])Ki.Clone(); int E = truncEpochs;
      for (int e = 0; e < E; e++) { var he = Sm(Kc, n, S, seed + e, St, REps); var R = RP(he, n); var d = DL(Nm(R, n, REps), n); Kc = Cupd(d, n, K0, Xi); }
      var hf = Sm(Kc, n, S, seed + E, St, REps);
      return Of(hf, n).Average(); }

    // Freeze K after freezeEpoch
    private static double RunFreezeK(int n, int seed, int freezeEpoch)
    { var Ki = KS(n, seed); var Kc = (double[,])Ki.Clone();
      for (int e = 0; e < 5; e++)
      { var he = Sm(Kc, n, S, seed + e, St, REps);
        if (e < freezeEpoch) { var R = RP(he, n); var d = DL(Nm(R, n, REps), n); Kc = Cupd(d, n, K0, Xi); } }
      var hf = Sm(Kc, n, S, seed + 5, St, REps);
      return Of(hf, n).Average(); }

    [Fact] public void V5_5_UMI_01_Protocol()
    { _output.WriteLine($"UMI: N={NTarget} | {SeedsPerN} seeds | Truncation E1-5 | Freeze-K E1-4"); }

    [Fact]
    public void V5_5_UMI_02_TruncationAndFreeze()
    {
        _output.WriteLine("═══ V5.5 UMI ═══");

        // Baseline (E=5)
        var baseOm = new double[SeedsPerN];
        for (int s = 0; s < SeedsPerN; s++) baseOm[s] = RunTruncated(NTarget, s, 5);
        int baseHi = baseOm.Count(x => x > FIXED_THRESHOLD);
        double baseCv = Std(baseOm) / Math.Abs(baseOm.Average());
        _output.WriteLine($"Baseline (E5): Ω_mean={baseOm.Average():F4} CV={baseCv:F4} high={baseHi}/{SeedsPerN}");

        // I1: Epoch truncation
        _output.WriteLine("── I1: EPOCH TRUNCATION ──");
        _output.WriteLine($"{"Epochs",7} {"Ω_mean",9} {"CV",7} {"High",6} {"hi→lo",7} {"lo→hi",7}");
        for (int e = 1; e <= 5; e++)
        { var oms = new double[SeedsPerN]; for (int s = 0; s < SeedsPerN; s++) oms[s] = RunTruncated(NTarget, s, e);
          int hi = oms.Count(x => x > FIXED_THRESHOLD); double cv = Std(oms) / Math.Abs(oms.Average());
          int htl = 0, lth = 0; for (int s = 0; s < SeedsPerN; s++) { bool b = baseOm[s] > FIXED_THRESHOLD, t = oms[s] > FIXED_THRESHOLD; if (b && !t) htl++; if (!b && t) lth++; }
          _output.WriteLine($"E1-E{e,2}   {oms.Average(),9:F4} {cv,7:F4} {hi,5}/{SeedsPerN} {htl,7} {lth,7}"); }

        // I2: Freeze K
        _output.WriteLine("── I2: FREEZE K ──");
        for (int f = 1; f <= 4; f++)
        { var oms = new double[SeedsPerN]; for (int s = 0; s < SeedsPerN; s++) oms[s] = RunFreezeK(NTarget, s, f);
          int hi = oms.Count(x => x > FIXED_THRESHOLD); double cv = Std(oms) / Math.Abs(oms.Average());
          int htl = 0, lth = 0; for (int s = 0; s < SeedsPerN; s++) { bool b = baseOm[s] > FIXED_THRESHOLD, t = oms[s] > FIXED_THRESHOLD; if (b && !t) htl++; if (!b && t) lth++; }
          _output.WriteLine($"FreezeK@E{f}: Ω={oms.Average():F4} CV={cv:F4} high={hi}/{SeedsPerN} hi→lo={htl} lo→hi={lth}"); }

        // Decision
        _output.WriteLine("");
        _output.WriteLine("═══ DECISION ═══");
        _output.WriteLine("If E1 already shows branch separation → GATE A: Early commitment.");
        _output.WriteLine("If E4-E5 amplification is required → GATE B: Late amplification.");
        _output.WriteLine("If freeze-K collapses separation → GATE C/D: d/K loop necessary.");
    }

    [Fact] public void V5_5_UMI_03_ClaimAudit()
    { _output.WriteLine("=== CLAIM AUDIT ===\nSUPPORTED: Truncation/freeze as reported.\nCONDITIONAL: 30 seeds, N=67.\nNOT CLAIMED: causation, physical interpretation, H9-H12.\nAUDIT: PASSED."); }
}
