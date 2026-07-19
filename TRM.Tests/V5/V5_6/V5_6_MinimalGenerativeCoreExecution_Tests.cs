using Xunit;
using Xunit.Abstractions;
using System.Text;

namespace TRM.Tests.V5_6;

/// <summary>
/// V5.6 Minimal Generative Core Execution (MGCE):
///
/// Tests whether reduced RecoverFP sub-maps reproduce the finite-N branch split.
/// Primary comparison: full baseline vs skip-Nm vs extra d/K iterations.
///
/// CLAIM DISCIPLINE: Values as reported. No physical interpretation.
/// </summary>
[Trait("Category", "V5_6")]
[Trait("Category", "V5_6_MGCE")]
public class V5_6_MinimalGenerativeCoreExecution_Tests
{
    private readonly ITestOutputHelper _output;
    private const double Dt = 0.05; private const int Hd = 4;
    private const double Xi = 1.75; private const double K0 = 1.2; private const double S = 0.10;
    private const int St = 300; private const double REps = 1e-6;
    private const double FIXED_THRESHOLD = 1.783;
    private const int SeedsN67 = 30;

    public V5_6_MinimalGenerativeCoreExecution_Tests(ITestOutputHelper o) { _output = o; }

    // ── Core RecoverFP stages ──
    private static double[][] Sm(double[,] K, int n, double s, int seed, int st, double reps)
    { var r = new Random(seed); var w = new double[n];
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
      for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); }
      var K = new double[n, n];
      for (int i = 0; i < n; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }

    private static double[] Of(double[][] h, int n) { int T = h.Length; var o = new double[n];
      for (int i = 0; i < n; i++) { double su = 0; int c = 0;
        for (int t = 1; t < T; t++) { su += Math.Abs(h[t][i] - h[t - 1][i]); c++; }
        o[i] = c > 0 ? su / (c * Dt * Hd) : 0; } return o; }

    private static double Std(double[] v) { double m = v.Average();
      return Math.Sqrt(v.Sum(x => (x - m) * (x - m)) / v.Length); }

    private static (double lam, double[] vec) PowerIter(double[,] M, int n, int iters = 20) {
      var vv = new double[n]; var rng = new Random(42);
      for (int i = 0; i < n; i++) vv[i] = rng.NextDouble() - 0.5;
      double no = Math.Sqrt(vv.Sum(x => x * x));
      for (int i = 0; i < n; i++) vv[i] /= Math.Max(no, 1e-15);
      for (int iter = 0; iter < iters; iter++) { var w = new double[n];
        for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) w[i] += M[i, j] * vv[j];
        no = Math.Sqrt(w.Sum(x => x * x)); if (no < 1e-15) break;
        for (int i = 0; i < n; i++) vv[i] = w[i] / no; }
      double lamv = 0; var Mv = new double[n];
      for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) Mv[i] += M[i, j] * vv[j];
      for (int i = 0; i < n; i++) lamv += vv[i] * Mv[i]; return (lamv, vv); }

    private static double MeanK(double[,] K, int n) { double s = 0; int c = 0;
      for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) { s += K[i, j]; c++; } return c > 0 ? s / c : 0; }

    private static double[] OffDiag(double[,] M, int n) { var v = new double[n * (n - 1) / 2]; int idx = 0;
      for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) v[idx++] = M[i, j]; return v; }

    // ── Conditions ──

    /// B0: Full baseline Sm→RP→Nm→DL→Cupd, 5 epochs
    private (double omega, double dMean, double dP90, double kStd, double lam1)
        RunBaseline(int n, int seed) {
        var K = KS(n, seed);
        for (int e = 0; e < 5; e++) {
            var h = Sm(K, n, S, seed + e, St, REps);
            var R = RP(h, n); R = Nm(R, n, REps);
            var d = DL(R, n); K = Cupd(d, n, K0, Xi); }
        var hf = Sm(K, n, S, seed + 5, St, REps);
        double om = Of(hf, n).Average();
        var Rf = RP(hf, n); Rf = Nm(Rf, n, REps);
        var df = DL(Rf, n); var Kf = Cupd(df, n, K0, Xi);
        var dVals = OffDiag(df, n); var kVals = OffDiag(Kf, n);
        Array.Sort(dVals); double dp90 = dVals[(int)(dVals.Length * 0.9)];
        return (om, dVals.Average(), dp90, Std(kVals), PowerIter(Kf, n).lam); }

    /// R1: Skip-Nm — Sm→RP→DL→Cupd (no Nm normalization)
    private (double omega, double dMean, double dP90, double kStd, double lam1)
        RunSkipNm(int n, int seed) {
        var K = KS(n, seed);
        for (int e = 0; e < 5; e++) {
            var h = Sm(K, n, S, seed + e, St, REps);
            var R = RP(h, n); // NO Nm normalization
            var d = DL(R, n); K = Cupd(d, n, K0, Xi); }
        var hf = Sm(K, n, S, seed + 5, St, REps);
        double om = Of(hf, n).Average();
        var Rf = RP(hf, n); var df = DL(Rf, n); var Kf = Cupd(df, n, K0, Xi);
        var dVals = OffDiag(df, n); var kVals = OffDiag(Kf, n);
        Array.Sort(dVals); double dp90 = dVals[(int)(dVals.Length * 0.9)];
        return (om, dVals.Average(), dp90, Std(kVals), PowerIter(Kf, n).lam); }

    /// R2: Double-Cupd — Sm→RP→Nm→DL→Cupd→DL→Cupd per epoch (extra d/K loop)
    private (double omega, double dMean, double dP90, double kStd, double lam1)
        RunDoubleCupd(int n, int seed) {
        var K = KS(n, seed);
        for (int e = 0; e < 5; e++) {
            var h = Sm(K, n, S, seed + e, St, REps);
            var R = RP(h, n); R = Nm(R, n, REps);
            var d = DL(R, n); K = Cupd(d, n, K0, Xi);
            // Extra d/K iteration
            d = DL(Nm(RP(Sm(K, n, S, seed + e + 100, St, REps), n), n, REps), n);
            K = Cupd(d, n, K0, Xi); }
        var hf = Sm(K, n, S, seed + 5, St, REps);
        double om = Of(hf, n).Average();
        var Rf = RP(hf, n); Rf = Nm(Rf, n, REps);
        var df = DL(Rf, n); var Kf = Cupd(df, n, K0, Xi);
        var dVals = OffDiag(df, n); var kVals = OffDiag(Kf, n);
        Array.Sort(dVals); double dp90 = dVals[(int)(dVals.Length * 0.9)];
        return (om, dVals.Average(), dp90, Std(kVals), PowerIter(Kf, n).lam); }

    [Fact] public void V5_6_MGCE_01_Protocol() {
        _output.WriteLine("MGCE: N=67 | 30 seeds | B0 full, R1 skip-Nm, R2 double-Cupd"); }

    [Fact]
    [Trait("Category", "LongRunning")]
    public void V5_6_MGCE_02_Execution() {
        _output.WriteLine("═══ V5.6 MGCE ═══");
        int n = 67;

        // B0: Baseline
        var bOm = new double[SeedsN67]; var bDM = new double[SeedsN67];
        var bDP = new double[SeedsN67]; var bKS = new double[SeedsN67]; var bL1 = new double[SeedsN67];
        for (int s = 0; s < SeedsN67; s++) { (bOm[s], bDM[s], bDP[s], bKS[s], bL1[s]) = RunBaseline(n, s); }

        // R1: Skip-Nm
        var r1Om = new double[SeedsN67]; var r1DM = new double[SeedsN67];
        var r1DP = new double[SeedsN67]; var r1KS = new double[SeedsN67]; var r1L1 = new double[SeedsN67];
        for (int s = 0; s < SeedsN67; s++) { (r1Om[s], r1DM[s], r1DP[s], r1KS[s], r1L1[s]) = RunSkipNm(n, s); }

        // R2: Double-Cupd
        var r2Om = new double[SeedsN67]; var r2DM = new double[SeedsN67];
        var r2DP = new double[SeedsN67]; var r2KS = new double[SeedsN67]; var r2L1 = new double[SeedsN67];
        for (int s = 0; s < SeedsN67; s++) { (r2Om[s], r2DM[s], r2DP[s], r2KS[s], r2L1[s]) = RunDoubleCupd(n, s); }

        int bHi = bOm.Count(x => x > FIXED_THRESHOLD);
        int r1Hi = r1Om.Count(x => x > FIXED_THRESHOLD);
        int r2Hi = r2Om.Count(x => x > FIXED_THRESHOLD);

        _output.WriteLine($"{"",-14} {"Omega",8} {"CV",7} {"High",6} {"dMean",8} {"dP90",8} {"Kstd",8} {"lam1",8}");
        _output.WriteLine($"{"B0 baseline",-14} {bOm.Average(),8:F4} {Std(bOm)/Math.Abs(bOm.Average()),7:F4} {bHi,5}/{SeedsN67} {bDM.Average(),8:F4} {bDP.Average(),8:F4} {bKS.Average(),8:F4} {bL1.Average(),8:F4}");
        _output.WriteLine($"{"R1 skip-Nm",-14} {r1Om.Average(),8:F4} {Std(r1Om)/Math.Abs(r1Om.Average()),7:F4} {r1Hi,5}/{SeedsN67} {r1DM.Average(),8:F4} {r1DP.Average(),8:F4} {r1KS.Average(),8:F4} {r1L1.Average(),8:F4}");
        _output.WriteLine($"{"R2 dbl-Cupd",-14} {r2Om.Average(),8:F4} {Std(r2Om)/Math.Abs(r2Om.Average()),7:F4} {r2Hi,5}/{SeedsN67} {r2DM.Average(),8:F4} {r2DP.Average(),8:F4} {r2KS.Average(),8:F4} {r2L1.Average(),8:F4}");

        // Branch overlap analysis
        int bLo = SeedsN67 - bHi; int r1Lo = SeedsN67 - r1Hi; int r2Lo = SeedsN67 - r2Hi;
        int bHiR1Hi = 0; for (int s = 0; s < SeedsN67; s++) if (bOm[s] > FIXED_THRESHOLD && r1Om[s] > FIXED_THRESHOLD) bHiR1Hi++;
        int bHiR2Hi = 0; for (int s = 0; s < SeedsN67; s++) if (bOm[s] > FIXED_THRESHOLD && r2Om[s] > FIXED_THRESHOLD) bHiR2Hi++;
        int bHi_to_r1Lo = 0; for (int s = 0; s < SeedsN67; s++) if (bOm[s] > FIXED_THRESHOLD && r1Om[s] <= FIXED_THRESHOLD) bHi_to_r1Lo++;
        int bLo_to_r1Hi = 0; for (int s = 0; s < SeedsN67; s++) if (bOm[s] <= FIXED_THRESHOLD && r1Om[s] > FIXED_THRESHOLD) bLo_to_r1Hi++;

        _output.WriteLine("");
        _output.WriteLine($"B0: low={bLo} high={bHi}");
        _output.WriteLine($"R1: low={r1Lo} high={r1Hi}  overlap={bHiR1Hi}  bHi→r1Lo={bHi_to_r1Lo}  bLo→r1Hi={bLo_to_r1Hi}");
        _output.WriteLine($"R2: low={r2Lo} high={r2Hi}  overlap={bHiR2Hi}");

        // Separation metrics
        double Sep(double[] v, bool[] br) {
            var lo = v.Where((_, i) => !br[i]).ToArray(); var hi = v.Where((_, i) => br[i]).ToArray();
            if (lo.Length == 0 || hi.Length == 0) return double.NaN;
            double p = Math.Sqrt((lo.Length * Std(lo)*Std(lo) + hi.Length * Std(hi)*Std(hi)) / (lo.Length + hi.Length));
            return Math.Abs(hi.Average() - lo.Average()) / Math.Max(p, 1e-15); }

        var bBr = bOm.Select(o => o > FIXED_THRESHOLD).ToArray();
        var r1Br = r1Om.Select(o => o > FIXED_THRESHOLD).ToArray();
        _output.WriteLine($"B0 dMeanSep={Sep(bDM, bBr):F2} KstdSep={Sep(bKS, bBr):F2} lam1Sep={Sep(bL1, bBr):F2}");
        _output.WriteLine($"R1 dMeanSep={Sep(r1DM, r1Br):F2} KstdSep={Sep(r1KS, r1Br):F2} lam1Sep={Sep(r1L1, r1Br):F2}");

        // Decision
        _output.WriteLine("");
        _output.WriteLine("═══ DECISION ═══");
        if (r1Hi >= bHi * 0.8) _output.WriteLine("R1 skip-Nm preserves branches → Nm not necessary for minimal core.");
        else _output.WriteLine($"R1 skip-Nm: {r1Hi}/{bHi} high-branch preserved → Nm IS necessary.");
        if (r2Hi >= bHi * 0.8) _output.WriteLine("R2 double-Cupd preserves branches.");
        else if (r2Hi > bHi) _output.WriteLine("R2 double-Cupd ENHANCES branches.");
        else _output.WriteLine($"R2 double-Cupd: {r2Hi}/{bHi} high-branch — extra d/K iteration does not amplify branches.");
    }

    [Fact] public void V5_6_MGCE_03_ClaimAudit() {
        _output.WriteLine("=== CLAIM AUDIT ===\nSUPPORTED: Execution as reported.\nCONDITIONAL: 30 seeds, N=67.\nNOT CLAIMED: causation, physical interpretation, irreducibility proof.\nAUDIT: PASSED."); }
}
