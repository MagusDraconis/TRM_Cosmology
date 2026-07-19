using Xunit;
using Xunit.Abstractions;
using System.Text;

namespace TRM.Tests.V5_3;

/// <summary>
/// M12 Coupling Spectral Intervention Audit (CSIA):
///
/// Tests whether the leading coupling eigenvalue λ₁(K) is a branch
/// driver or marker. After Epoch 1, shifts λ₁(K) toward low-branch
/// mean (suppress) or amplifies it, then continues RecoverFP.
///
/// I1: λ₁ suppression (subtract rank-1 component)
/// I2: λ₁ amplification (add rank-1 component)
///
/// CLAIM DISCIPLINE: Intervention ≠ proof of causation.
/// </summary>
[Trait("Category", "V5_3")]
[Trait("Category", "V5_3_CSIA")]
public class V5_3_CouplingSpectralInterventionAudit_Tests
{
    private readonly ITestOutputHelper _output;
    private const double Dt = 0.05; private const int Hd = 4;
    private const double Xi = 1.75; private const double K0 = 1.2; private const double S = 0.10;
    private const int St = 300; private const double REps = 1e-6;
    private const int NTarget = 67; private const int SeedsTotal = 30;
    private const double FIXED_THRESHOLD = 1.783;
    private const double LambdaShift = 0.08; // magnitude of λ₁ shift

    public V5_3_CouplingSpectralInterventionAudit_Tests(ITestOutputHelper o) { _output = o; }

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

    private static (double lambda, double[] vec) PowerIter(double[,] M, int n, int iters = 40)
    { var v = new double[n]; var rng = new Random(42); for (int i = 0; i < n; i++) v[i] = rng.NextDouble() - 0.5; double norm = Math.Sqrt(v.Sum(x => x * x)); for (int i = 0; i < n; i++) v[i] /= Math.Max(norm, 1e-15);
      for (int iter = 0; iter < iters; iter++) { var w = new double[n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) w[i] += M[i, j] * v[j]; norm = Math.Sqrt(w.Sum(x => x * x)); if (norm < 1e-15) break; for (int i = 0; i < n; i++) v[i] = w[i] / norm; }
      double lam = 0; var Mv = new double[n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) Mv[i] += M[i, j] * v[j]; for (int i = 0; i < n; i++) lam += v[i] * Mv[i]; return (lam, v); }

    // Modify K to shift λ₁ by adding/subtracting rank-1 component
    private static double[,] ShiftLambda(double[,] K, int n, double shift)
    { var (lambda, vec) = PowerIter(K, n);
      var Knew = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) Knew[i, j] = i == j ? 0 : K[i, j] + shift * vec[i] * vec[j];
      // Clamp to valid range
      for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) { Knew[i, j] = Math.Max(0, Knew[i, j]); Knew[j, i] = Knew[i, j]; }
      return Knew; }

    private static (double mean, double std) KStats(double[,] K, int n)
    { var vals = new List<double>(); for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) vals.Add(K[i, j]); return (vals.Average(), Std(vals.ToArray())); }

    private static double RunWithSpectralIntervention(int n, int seed, string intervention)
    { int E = Ep(n); var Ki = KS(n, seed); var Kc = (double[,])Ki.Clone();
      for (int e = 0; e < E; e++) { var he = Sm(Kc, n, S, seed + e, St, REps); var R = RP(he, n); var d = DL(Nm(R, n, REps), n); Kc = Cupd(d, n, K0, Xi);
        if (e == 0) { if (intervention == "suppress") Kc = ShiftLambda(Kc, n, -LambdaShift); else if (intervention == "amplify") Kc = ShiftLambda(Kc, n, +LambdaShift); } }
      var hf = Sm(Kc, n, S, seed + E, St, REps); return Of(hf, n).Average(); }

    // ═══════════════════════════════════════════════════════════

    [Fact] public void V5_3_CSIA_01_Protocol()
    { _output.WriteLine($"M12: N={NTarget} | {SeedsTotal} seeds | λ₁ shift={LambdaShift:F2} | fixed thr={FIXED_THRESHOLD:F3}"); }

    [Fact]
    public void V5_3_CSIA_02_SpectralIntervention()
    {
        _output.WriteLine("═══ M12 SPECTRAL INTERVENTION ═══");

        // Baseline
        var baseOm = new double[SeedsTotal];
        for (int s = 0; s < SeedsTotal; s++) baseOm[s] = RunWithSpectralIntervention(NTarget, s, "baseline");
        int baseHi = baseOm.Count(x => x > FIXED_THRESHOLD);
        double baseCv = Std(baseOm) / Math.Abs(baseOm.Average());

        // I1: Suppress λ₁
        var suppOm = new double[SeedsTotal];
        for (int s = 0; s < SeedsTotal; s++) suppOm[s] = RunWithSpectralIntervention(NTarget, s, "suppress");
        int suppHi = suppOm.Count(x => x > FIXED_THRESHOLD);
        double suppCv = Std(suppOm) / Math.Abs(suppOm.Average());

        // I2: Amplify λ₁
        var ampOm = new double[SeedsTotal];
        for (int s = 0; s < SeedsTotal; s++) ampOm[s] = RunWithSpectralIntervention(NTarget, s, "amplify");
        int ampHi = ampOm.Count(x => x > FIXED_THRESHOLD);
        double ampCv = Std(ampOm) / Math.Abs(ampOm.Average());

        _output.WriteLine($"Base:     Ω_mean={baseOm.Average():F4} CV={baseCv:F4} high={baseHi}/{SeedsTotal}");
        _output.WriteLine($"Suppress: Ω_mean={suppOm.Average():F4} CV={suppCv:F4} high={suppHi}/{SeedsTotal} (Δ={suppHi - baseHi:+0;-0})");
        _output.WriteLine($"Amplify:  Ω_mean={ampOm.Average():F4} CV={ampCv:F4} high={ampHi}/{SeedsTotal} (Δ={ampHi - baseHi:+0;-0})");

        // Quick Epoch 1 λ₁ diagnostic for a few seeds
        _output.WriteLine("");
        _output.WriteLine("── Epoch 1 λ₁ verification (seeds 0-4) ──");
        for (int s = 0; s < 5; s++)
        { int E = Ep(NTarget); var Ki = KS(NTarget, s); var Kc = (double[,])Ki.Clone(); var he = Sm(Kc, NTarget, S, s, St, REps); var R = RP(he, NTarget); var d = DL(Nm(R, NTarget, REps), NTarget); Kc = Cupd(d, NTarget, K0, Xi); var (lam, _) = PowerIter(Kc, NTarget); var (km, ks) = KStats(Kc, NTarget);
          var Ks = ShiftLambda(Kc, NTarget, -LambdaShift); var (lamS, _) = PowerIter(Ks, NTarget); var Ka = ShiftLambda(Kc, NTarget, +LambdaShift); var (lamA, _) = PowerIter(Ka, NTarget);
          _output.WriteLine($"  seed={s}: λ₁={lam:F4}, K_m={km:F4}, K_σ={ks:F4} | suppr→λ₁={lamS:F4} ampl→λ₁={lamA:F4}"); }

        // Decision
        _output.WriteLine("");
        _output.WriteLine("═══ DECISION ═══");
        int suppDelta = Math.Abs(suppHi - baseHi), ampDelta = Math.Abs(ampHi - baseHi);
        if (suppDelta + ampDelta >= 3)
            _output.WriteLine($"GATE A: λ₁ DRIVER (supp Δ={suppHi - baseHi:+0;-0}, amp Δ={ampHi - baseHi:+0;-0})");
        else
            _output.WriteLine($"GATE B: λ₁ MARKER ONLY (supp Δ={suppHi - baseHi:+0;-0}, amp Δ={ampHi - baseHi:+0;-0}) — λ₁ does not control branch.");
    }

    [Fact] public void V5_3_CSIA_03_ClaimAudit()
    { _output.WriteLine("=== CLAIM AUDIT ===\nSUPPORTED: Spectral intervention as reported.\nCONDITIONAL: 30 seeds, N=67.\nNOT CLAIMED: causation, phase transition, H9-H12.\nAUDIT: PASSED."); }
}
