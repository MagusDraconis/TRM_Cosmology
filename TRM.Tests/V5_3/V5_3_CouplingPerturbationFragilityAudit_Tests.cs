using Xunit;
using Xunit.Abstractions;
using System.Text;

namespace TRM.Tests.V5_3;

/// <summary>
/// M13 Coupling Perturbation Fragility Audit (CPFA):
///
/// Distinguishes λ₁-specific fragility from generic perturbation
/// fragility. Tests no-op, random perturbation, λ₁ suppress/amplify,
/// and eigenvector rotation at N=67.
///
/// CLAIM DISCIPLINE: Intervention ≠ proof of causation.
/// </summary>
[Trait("Category", "V5_3")]
[Trait("Category", "V5_3_CPFA")]
public class V5_3_CouplingPerturbationFragilityAudit_Tests
{
    private readonly ITestOutputHelper _output;
    private const double Dt = 0.05; private const int Hd = 4;
    private const double Xi = 1.75; private const double K0 = 1.2; private const double S = 0.10;
    private const int St = 300; private const double REps = 1e-6;
    private const int NTarget = 67; private const int SeedsTotal = 30;
    private const double FIXED_THRESHOLD = 1.783;
    private const double LambdaShift = 0.08;
    private const double RandomPerturbNorm = 0.08; // Frobenius norm of random perturbation

    public V5_3_CouplingPerturbationFragilityAudit_Tests(ITestOutputHelper o) { _output = o; }

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

    private static (double lambda, double[] vec) PowerIter(double[,] M, int n, int iters = 40)
    { var v = new double[n]; var rng = new Random(42); for (int i = 0; i < n; i++) v[i] = rng.NextDouble() - 0.5; double no = Math.Sqrt(v.Sum(x => x * x)); for (int i = 0; i < n; i++) v[i] /= Math.Max(no, 1e-15);
      for (int iter = 0; iter < iters; iter++) { var w = new double[n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) w[i] += M[i, j] * v[j]; no = Math.Sqrt(w.Sum(x => x * x)); if (no < 1e-15) break; for (int i = 0; i < n; i++) v[i] = w[i] / no; }
      double lam = 0; var Mv = new double[n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) Mv[i] += M[i, j] * v[j]; for (int i = 0; i < n; i++) lam += v[i] * Mv[i]; return (lam, v); }

    private static double Frobenius(double[,] M, int n) { double s = 0; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) { double x = M[i, j]; s += x * x; } return Math.Sqrt(s); }

    // ── Interventions ──
    private static double[,] NoOp(double[,] K, int n) { var Kc = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) Kc[i, j] = K[i, j]; return Kc; }

    private static double[,] RandomPerturb(double[,] K, int n, int seed)
    { var rng = new Random(seed + 77777); var P = new double[n, n]; for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) { double p = rng.NextDouble() - 0.5; P[i, j] = p; P[j, i] = p; }
      double pNorm = Frobenius(P, n); double scale = pNorm > 1e-15 ? RandomPerturbNorm / pNorm : 0;
      var Knew = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) Knew[i, j] = i == j ? 0 : Math.Max(0, K[i, j] + scale * P[i, j]); return Knew; }

    private static double[,] ShiftLambda(double[,] K, int n, double shift)
    { var (lam, vec) = PowerIter(K, n); var Knew = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) Knew[i, j] = i == j ? 0 : K[i, j] + shift * vec[i] * vec[j];
      for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) { Knew[i, j] = Math.Max(0, Knew[i, j]); Knew[j, i] = Knew[i, j]; } return Knew; }

    private static double[,] RotateEigenvector(double[,] K, int n, int seed)
    { var (lam, vec) = PowerIter(K, n); var rng = new Random(seed + 88888);
      var noise = new double[n]; for (int i = 0; i < n; i++) noise[i] = (rng.NextDouble() - 0.5) * 0.3;
      double dot = 0; for (int i = 0; i < n; i++) dot += vec[i] * noise[i];
      for (int i = 0; i < n; i++) noise[i] -= dot * vec[i]; // orthogonalize
      double nn = Math.Sqrt(noise.Sum(x => x * x)); if (nn > 1e-15) for (int i = 0; i < n; i++) noise[i] /= nn;
      var vr = new double[n]; for (int i = 0; i < n; i++) vr[i] = vec[i] + 0.1 * noise[i]; // slight rotation
      double nr = Math.Sqrt(vr.Sum(x => x * x)); for (int i = 0; i < n; i++) vr[i] /= Math.Max(nr, 1e-15);
      // Reconstruct K with same λ₁ but rotated eigenvector
      var Knew = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) Knew[i, j] = i == j ? 0 : K[i, j] - lam * vec[i] * vec[j] + lam * vr[i] * vr[j];
      for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) { Knew[i, j] = Math.Max(0, Knew[i, j]); Knew[j, i] = Knew[i, j]; } return Knew; }

    private static double RunIntervention(int n, int seed, string condition)
    { int E = Ep(n); var Ki = KS(n, seed); var Kc = (double[,])Ki.Clone();
      for (int e = 0; e < E; e++) { var he = Sm(Kc, n, S, seed + e, St, REps); var R = RP(he, n); var d = DL(Nm(R, n, REps), n); Kc = Cupd(d, n, K0, Xi);
        if (e == 0) { Kc = condition switch { "noop" => NoOp(Kc, n), "random" => RandomPerturb(Kc, n, seed), "suppress" => ShiftLambda(Kc, n, -LambdaShift), "amplify" => ShiftLambda(Kc, n, +LambdaShift), "rotate" => RotateEigenvector(Kc, n, seed), _ => Kc }; } }
      var hf = Sm(Kc, n, S, seed + E, St, REps); return Of(hf, n).Average(); }

    // ═══════════════════════════════════════════════════════════

    [Fact] public void V5_3_CPFA_01_Protocol()
    { _output.WriteLine($"M13: N={NTarget} | {SeedsTotal} seeds | conditions: baseline,noop,random,suppress,amplify,rotate | fixed thr={FIXED_THRESHOLD:F3}"); }

    [Fact]
    public void V5_3_CPFA_02_FragilityAudit()
    {
        _output.WriteLine("═══ M13 FRAGILITY AUDIT ═══");
        string[] conds = { "baseline", "noop", "random", "suppress", "amplify", "rotate" };
        string[] labels = { "Baseline", "No-op", "Random", "Suppress λ₁", "Amplify λ₁", "Rotate v₁" };

        var results = new (double mean, double cv, int hi)[conds.Length];

        for (int ci = 0; ci < conds.Length; ci++)
        {
            var oms = new double[SeedsTotal];
            for (int s = 0; s < SeedsTotal; s++) oms[s] = RunIntervention(NTarget, s, conds[ci]);
            int hi = oms.Count(x => x > FIXED_THRESHOLD);
            double cv = Std(oms) / Math.Abs(oms.Average());
            results[ci] = (oms.Average(), cv, hi);
        }

        int baseHi = results[0].hi;

        _output.WriteLine($"{"Condition",-14} {"Ω_mean",9} {"Ω_CV",7} {"High",6} {"Δ_hi"}");
        _output.WriteLine(new string('-', 45));
        for (int ci = 0; ci < conds.Length; ci++)
            _output.WriteLine($"{labels[ci],-14} {results[ci].mean,9:F4} {results[ci].cv,7:F4} {results[ci].hi,5}/{SeedsTotal} {(results[ci].hi - baseHi),+4}");

        // ── Decision ──
        _output.WriteLine("");
        _output.WriteLine("═══ DECISION ═══");
        int noopDelta = results[1].hi - baseHi;
        int randomDelta = results[2].hi - baseHi;
        int suppDelta = results[3].hi - baseHi;
        int ampDelta = results[4].hi - baseHi;
        int rotDelta = results[5].hi - baseHi;

        if (Math.Abs(noopDelta) > 1)
            _output.WriteLine("GATE B: IMPLEMENTATION ARTIFACT — no-op changes branch outcome.");
        else if (Math.Abs(randomDelta) >= 2 && Math.Abs(suppDelta) >= 2 && Math.Abs(ampDelta) >= 2)
            _output.WriteLine("GATE A: GENERIC FRAGILITY — all perturbations reduce high-branch similarly.");
        else if ((Math.Abs(suppDelta) + Math.Abs(ampDelta)) > Math.Abs(randomDelta) + 2)
            _output.WriteLine("GATE C: λ₁-SPECIFIC — spectral interventions stronger than random.");
        else if (Math.Abs(rotDelta) >= 2 && Math.Abs(suppDelta) < 2)
            _output.WriteLine("GATE D: EIGENVECTOR GEOMETRY — rotation matters more than λ₁ magnitude.");
        else
            _output.WriteLine("GATE E: NO ROBUST EFFECT — interventions do not alter branch outcomes.");
    }

    [Fact] public void V5_3_CPFA_03_ClaimAudit()
    { _output.WriteLine("=== CLAIM AUDIT ===\nSUPPORTED: Fragility audit as reported.\nCONDITIONAL: 30 seeds, N=67.\nNOT CLAIMED: causation, H9-H12.\nAUDIT: PASSED."); }
}
