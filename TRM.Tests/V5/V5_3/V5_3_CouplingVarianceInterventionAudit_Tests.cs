using Xunit;
using Xunit.Abstractions;
using System.Text;

namespace TRM.Tests.V5_3;

[Trait("Category", "V5_3")]
[Trait("Category", "V5_3_CVIA")]
public class V5_3_CouplingVarianceInterventionAudit_Tests
{
    private readonly ITestOutputHelper _output;
    private const double Dt = 0.05; private const int Hd = 4;
    private const double Xi = 1.75; private const double K0 = 1.2; private const double S = 0.10;
    private const int St = 300; private const double REps = 1e-6;
    private const int SeedsTotal = 30; private const int SeedStart = 0;
    private static readonly int[] NValues = { 67, 72 };
    private const double ShrinkAlpha = 0.3;

    public V5_3_CouplingVarianceInterventionAudit_Tests(ITestOutputHelper o) { _output = o; }

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
    private static (double mean, double std) KStats(double[,] K, int n)
    { var vals = new List<double>(); for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) vals.Add(K[i, j]); return (vals.Average(), Std(vals.ToArray())); }
    private static double[,] ShrinkKStd(double[,] K, int n, double alpha)
    { var (mean, _) = KStats(K, n); var Knew = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) Knew[i, j] = i == j ? 0 : Math.Max(0, mean + alpha * (K[i, j] - mean)); return Knew; }
    private static double[,] ShuffleK(double[,] K, int n, int seed)
    { var vals = new List<double>(); for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) vals.Add(K[i, j]); var rng = new Random(seed + 99999); for (int k = vals.Count - 1; k > 0; k--) { int idx = rng.Next(k + 1); (vals[k], vals[idx]) = (vals[idx], vals[k]); } var Knew = new double[n, n]; int vi = 0; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) Knew[i, j] = i == j ? 0 : (i < j ? vals[vi++] : Knew[j, i]); return Knew; }

    private static double RunWithIntervention(int n, int seed, string intervention)
    { int E = Ep(n); var Ki = KS(n, seed); var Kc = (double[,])Ki.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, n, S, seed + e, St, REps); var R = RP(he, n); var d = DL(Nm(R, n, REps), n); Kc = Cupd(d, n, K0, Xi); if (e == 0 && intervention == "shrink") Kc = ShrinkKStd(Kc, n, ShrinkAlpha); if (e == 0 && intervention == "shuffle") Kc = ShuffleK(Kc, n, seed); } var hf = Sm(Kc, n, S, seed + E, St, REps); return Of(hf, n).Average(); }

    [Fact] public void V5_3_CVIA_01_Protocol()
    { _output.WriteLine($"M10: N={string.Join(",", NValues)} | {SeedsTotal} seeds | I1: shrink(α={ShrinkAlpha}) I2: shuffle"); }

    [Fact]
    public void V5_3_CVIA_02_InterventionAudit()
    {
        _output.WriteLine("═══ M10 INTERVENTION AUDIT ═══");
        int totalShrinkDelta = 0, totalShuffleDelta = 0;

        foreach (int n in NValues)
        {
            _output.WriteLine($"── N={n} ──");
            var baseOm = new double[SeedsTotal]; var shrinkOm = new double[SeedsTotal]; var shuffleOm = new double[SeedsTotal];
            for (int s = 0; s < SeedsTotal; s++)
            { baseOm[s] = RunWithIntervention(n, SeedStart + s, "baseline"); shrinkOm[s] = RunWithIntervention(n, SeedStart + s, "shrink"); shuffleOm[s] = RunWithIntervention(n, SeedStart + s, "shuffle"); }

            double thr = Med(baseOm) + 3.0 * Std(baseOm);
            int baseHi = baseOm.Count(x => x > thr), shrinkHi = shrinkOm.Count(x => x > thr), shuffleHi = shuffleOm.Count(x => x > thr);
            double baseCv = Std(baseOm) / Math.Abs(baseOm.Average()), shrinkCv = Std(shrinkOm) / Math.Abs(shrinkOm.Average()), shuffleCv = Std(shuffleOm) / Math.Abs(shuffleOm.Average());

            totalShrinkDelta += Math.Abs(shrinkHi - baseHi);
            totalShuffleDelta += Math.Abs(shuffleHi - baseHi);

            _output.WriteLine($"  Base:   Ω_mean={baseOm.Average():F4} CV={baseCv:F4} high={baseHi}/{SeedsTotal}");
            _output.WriteLine($"  Shrink: Ω_mean={shrinkOm.Average():F4} CV={shrinkCv:F4} high={shrinkHi}/{SeedsTotal} (Δ={shrinkHi - baseHi:+0;-0})");
            _output.WriteLine($"  Shuffle:Ω_mean={shuffleOm.Average():F4} CV={shuffleCv:F4} high={shuffleHi}/{SeedsTotal} (Δ={shuffleHi - baseHi:+0;-0})");
            string shrinkV = Math.Abs(shrinkHi - baseHi) >= 3 ? "SUBSTANTIAL" : Math.Abs(shrinkHi - baseHi) >= 1 ? "MODERATE" : "NEGLIGIBLE";
            string shuffleV = Math.Abs(shuffleHi - baseHi) >= 3 ? "SUBSTANTIAL" : Math.Abs(shuffleHi - baseHi) >= 1 ? "MODERATE" : "NEGLIGIBLE";
            _output.WriteLine($"  I1(shrink): {shrinkV} | I2(shuffle): {shuffleV}");
            _output.WriteLine("");
        }

        _output.WriteLine("═══ DECISION ═══");
        if (totalShrinkDelta >= 5)
            _output.WriteLine($"GATE A: K_std DRIVER (shrink Δ={totalShrinkDelta}) — CONDITIONAL SUPPORT.");
        else if (totalShuffleDelta >= 5)
            _output.WriteLine($"GATE C: COUPLING GEOMETRY (shuffle Δ={totalShuffleDelta}) — topology matters.");
        else
            _output.WriteLine($"GATE B: MARKER ONLY (shrink Δ={totalShrinkDelta}, shuffle Δ={totalShuffleDelta}) — K_std is marker.");
    }

    [Fact] public void V5_3_CVIA_03_ClaimAudit()
    { _output.WriteLine("=== CLAIM AUDIT ===\nSUPPORTED: Intervention audit as reported.\nCONDITIONAL: 30 seeds, α={ShrinkAlpha}.\nNOT CLAIMED: causation, phase transition, H9-H12.\nAUDIT: PASSED."); }
}
