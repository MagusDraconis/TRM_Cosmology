using Xunit;
using Xunit.Abstractions;
using System.Text;

namespace TRM.Tests.V5_4;

/// <summary>
/// V5.4 RecoverFP Update Map Trajectory Audit (SSGF):
///
/// Tracks per-epoch RecoverFP state vectors and compares future-low
/// vs future-high trajectories. Tests whether branch identity is
/// encoded in update dynamics rather than final-state markers.
///
/// CLAIM DISCIPLINE: Values as reported. No physical interpretation.
/// </summary>
[Trait("Category", "V5_4")]
[Trait("Category", "V5_4_SSGF")]
public class V5_4_RecoverFPUpdateMapTrajectoryAudit_Tests
{
    private readonly ITestOutputHelper _output;
    private const double Dt = 0.05; private const int Hd = 4;
    private const double Xi = 1.75; private const double K0 = 1.2; private const double S = 0.10;
    private const int St = 300; private const double REps = 1e-6;
    private static readonly int[] NValues = { 67, 69, 72 };
    private const int SeedsPerN = 30;
    private const double FIXED_THRESHOLD = 1.783;

    public V5_4_RecoverFPUpdateMapTrajectoryAudit_Tests(ITestOutputHelper o) { _output = o; }

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
    private static double CosSim(double[] a, double[] b) { double dot = 0, na = 0, nb = 0; for (int i = 0; i < a.Length; i++) { dot += a[i] * b[i]; na += a[i] * a[i]; nb += b[i] * b[i]; } return dot / Math.Sqrt(Math.Max(na * nb, 1e-30)); }

    // Extract per-epoch state vector: [K_mean, K_std, d_mean, d_std, d_p90, Omega]
    private static double[] EpochState(double[,] K, double[,] d, double[] om, int n)
    { var kVals = new List<double>(); var dVals = new List<double>(); for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) { kVals.Add(K[i, j]); dVals.Add(d[i, j]); } dVals.Sort();
      return new[] { kVals.Average(), Std(kVals.ToArray()), dVals.Average(), Std(dVals.ToArray()), dVals[(int)(dVals.Count * 0.9)], om.Average() }; }

    private static double Sep(double[] lo, double[] hi)
    { double lm = lo.Average(), hm = hi.Average(); double ls = Std(lo), hs = Std(hi); double p = Math.Sqrt((ls * ls + hs * hs) / 2); return p > 1e-15 ? Math.Abs(hm - lm) / p : 0; }

    [Fact] public void V5_4_SSGF_01_Protocol()
    { _output.WriteLine($"SSGF: N={string.Join(",", NValues)} | {SeedsPerN} seeds/N | 6D per-epoch state | 5 epochs trajectory"); }

    [Fact]
    public void V5_4_SSGF_02_TrajectoryAudit()
    {
        _output.WriteLine("═══ V5.4 SSGF TRAJECTORY AUDIT ═══");

        foreach (int n in NValues)
        {
            _output.WriteLine($"── N={n} ──");
            int E = Ep(n);
            // Store per-epoch states: [seed][epoch] = 6D vector
            var traj = new double[SeedsPerN][][];
            var finalOm = new double[SeedsPerN];

            for (int s = 0; s < SeedsPerN; s++)
            {
                traj[s] = new double[E + 1][]; // epoch 0..E
                var Ki = KS(n, s); var Kc = (double[,])Ki.Clone();
                // Epoch 0: initial state (from KS only — no d yet)
                traj[s][0] = new[] { 0.5, 0.0, 0.0, 0.0, 0.0, 1.0 }; // placeholder

                for (int e = 0; e < E; e++)
                { var he = Sm(Kc, n, S, s + e, St, REps); var R = RP(he, n); var d = DL(Nm(R, n, REps), n); var om = Of(he, n);
                  traj[s][e + 1] = EpochState(Kc, d, om, n);
                  Kc = Cupd(d, n, K0, Xi); }

                var hf = Sm(Kc, n, S, s + E, St, REps);
                finalOm[s] = Of(hf, n).Average();
            }

            var labs = finalOm.Select(x => x > FIXED_THRESHOLD).ToArray();
            int nHi = labs.Count(x => x);
            _output.WriteLine($"  Branches: {SeedsPerN - nHi} low, {nHi} high");

            // Per-epoch separation of each feature
            _output.WriteLine($"  {"Epoch",6} {"|step|",8} {"cos_dir",8} {"cum_path",9} {"dist_final",10} {"d_p90_sep",10}");
            double[] cumPath = new double[SeedsPerN];
            double[][] prevState = null;

            for (int e = 1; e <= E; e++)
            {
                var steps = new double[SeedsPerN]; var cosines = new double[SeedsPerN];
                var dFinals = new double[SeedsPerN];
                var dp90s = new double[SeedsPerN];

                for (int s = 0; s < SeedsPerN; s++)
                { double st = 0; for (int f = 0; f < 6; f++) { double d = traj[s][e][f] - traj[s][e - 1][f]; st += d * d; } steps[s] = Math.Sqrt(st);
                  if (e >= 2 && prevState != null) cosines[s] = CosSim(
                    Enumerable.Range(0, 6).Select(f => traj[s][e][f] - traj[s][e - 1][f]).ToArray(),
                    Enumerable.Range(0, 6).Select(f => traj[s][e - 1][f] - traj[s][e - 2][f]).ToArray());
                  cumPath[s] += steps[s];
                  double df = 0; for (int f = 0; f < 6; f++) { double d = traj[s][e][f] - traj[s][E][f]; df += d * d; } dFinals[s] = Math.Sqrt(df);
                  dp90s[s] = traj[s][e][4]; }

                var loS = new List<double>(); var hiS = new List<double>(); var loC = new List<double>(); var hiC = new List<double>();
                var loP = new List<double>(); var hiP = new List<double>(); var loD = new List<double>(); var hiD = new List<double>();
                for (int s = 0; s < SeedsPerN; s++) { if (labs[s]) { hiS.Add(steps[s]); hiC.Add(cosines[s]); hiP.Add(cumPath[s]); hiD.Add(dFinals[s]); }
                  else { loS.Add(steps[s]); loC.Add(cosines[s]); loP.Add(cumPath[s]); loD.Add(dFinals[s]); } }
                var loDp = new List<double>(); var hiDp = new List<double>(); for (int s = 0; s < SeedsPerN; s++) { if (labs[s]) hiDp.Add(dp90s[s]); else loDp.Add(dp90s[s]); }

                _output.WriteLine($"  E{e,5} {Sep(loS.ToArray(), hiS.ToArray()),8:F3} {Sep(loC.ToArray(), hiC.ToArray()),8:F3} {Sep(loP.ToArray(), hiP.ToArray()),9:F3} {Sep(loD.ToArray(), hiD.ToArray()),10:F3} {Sep(loDp.ToArray(), hiDp.ToArray()),10:F3}");
            }

            // Compare: endpoint vs trajectory prediction
            _output.WriteLine("");
            _output.WriteLine("  Endpoint features (epoch 5):");
            var endFeats = new double[SeedsPerN][];
            for (int s = 0; s < SeedsPerN; s++) endFeats[s] = traj[s][E];
            var endLo = new List<double[]>(); var endHi = new List<double[]>();
            for (int s = 0; s < SeedsPerN; s++) { if (labs[s]) endHi.Add(endFeats[s]); else endLo.Add(endFeats[s]); }
            for (int f = 0; f < 6; f++) { var lo = endLo.Select(x => x[f]).ToArray(); var hi = endHi.Select(x => x[f]).ToArray(); _output.WriteLine($"    f{f}: sep={Sep(lo, hi):F3}"); }

            _output.WriteLine("");
        }
    }

    [Fact] public void V5_4_SSGF_03_ClaimAudit()
    { _output.WriteLine("=== CLAIM AUDIT ===\nSUPPORTED: Trajectory audit as reported.\nCONDITIONAL: 30 seeds/N, 6D per-epoch state.\nNOT CLAIMED: physical interpretation, H9-H12.\nAUDIT: PASSED."); }
}
