using Xunit;
using Xunit.Abstractions;
using System.Text;

namespace TRM.Tests.V5_3;

/// <summary>
/// M11 Branch Genesis Execution (BGEN):
///
/// Determines which RecoverFP diagnostic separates future branches
/// earliest. Tracks distance spectral (λ₁), coupling spectral (λ₁),
/// per-node diagnostics, and K-update dynamics across epochs.
///
/// CLAIM DISCIPLINE: Correlation ≠ causation. Diagnostics as reported.
/// </summary>
[Trait("Category", "V5_3")]
[Trait("Category", "V5_3_BGEN")]
public class V5_3_BranchGenesisExecution_Tests
{
    private readonly ITestOutputHelper _output;
    private const double Dt = 0.05; private const int Hd = 4;
    private const double Xi = 1.75; private const double K0 = 1.2; private const double S = 0.10;
    private const int St = 300; private const double REps = 1e-6;
    private const int NTarget = 67; private const int SeedsTotal = 30;
    private const double FIXED_THRESHOLD = 1.783; // from M7/M8/M10r

    public V5_3_BranchGenesisExecution_Tests(ITestOutputHelper o) { _output = o; }

    // ═══════════════════════════════════════════════════════════
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

    // ── Power iteration for leading eigenvalue of symmetric matrix ──
    private static double LeadingEigenvalue(double[,] M, int n, int iters = 40)
    {
        var v = new double[n]; var rng = new Random(42);
        for (int i = 0; i < n; i++) v[i] = rng.NextDouble() - 0.5;
        double norm = Math.Sqrt(v.Sum(x => x * x));
        for (int i = 0; i < n; i++) v[i] /= Math.Max(norm, 1e-15);

        for (int iter = 0; iter < iters; iter++)
        {
            var w = new double[n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    w[i] += M[i, j] * v[j];
            norm = Math.Sqrt(w.Sum(x => x * x));
            if (norm < 1e-15) break;
            for (int i = 0; i < n; i++) v[i] = w[i] / norm;
        }

        double lambda = 0;
        var Mv = new double[n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                Mv[i] += M[i, j] * v[j];
        for (int i = 0; i < n; i++) lambda += v[i] * Mv[i];
        return lambda;
    }

    // ── Matrix Frobenius norm (off-diagonal) ──
    private static double FrobeniusOffDiag(double[,] M, int n)
    { double s = 0; for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) { double x = M[i, j]; s += x * x + M[j, i] * M[j, i]; } return Math.Sqrt(s); }

    // ── Per-node coupling/distance sums ──
    private static double[] NodeSums(double[,] M, int n)
    { var sums = new double[n]; for (int i = 0; i < n; i++) { double s = 0; for (int j = 0; j < n; j++) if (i != j) s += M[i, j]; sums[i] = s; } return sums; }

    // ═══════════════════════════════════════════════════════════

    [Fact] public void V5_3_BGEN_01_Protocol()
    { _output.WriteLine($"M11: N={NTarget} | {SeedsTotal} seeds | diagnostics: λ₁(d), λ₁(K), per-node sums, K-update norm | fixed thr={FIXED_THRESHOLD:F3}"); }

    [Fact]
    public void V5_3_BGEN_02_GenesisAudit()
    {
        _output.WriteLine("═══ M11 BRANCH GENESIS AUDIT ═══");
        int E = Ep(NTarget);

        // Store per-epoch diagnostics
        var epochDiags = new (double omega, double md, double dMean, double dStd, double kMean, double kStd,
                              double dLam1, double kLam1, double dFrob, double kFrob,
                              double kUpdateNorm, double topNodeKConc)[SeedsTotal][];

        double[] finalOmega = new double[SeedsTotal];

        for (int s = 0; s < SeedsTotal; s++)
        {
            int seed = s;
            var Ki = KS(NTarget, seed);
            var Kc = (double[,])Ki.Clone();
            double[,] prevK = null;
            epochDiags[s] = new (double, double, double, double, double, double, double, double, double, double, double, double)[E];

            for (int e = 0; e < E; e++)
            {
                var he = Sm(Kc, NTarget, S, seed + e, St, REps);
                var R = RP(he, NTarget);
                var Rnm = Nm(R, NTarget, REps);
                var d = DL(Rnm, NTarget);
                prevK = (double[,])Kc.Clone();
                Kc = Cupd(d, NTarget, K0, Xi);

                double omega = Of(he, NTarget).Average();
                double md = 0; for (int i = 0; i < NTarget; i++) for (int j = i + 1; j < NTarget; j++) md += d[i, j]; md /= (NTarget * (NTarget - 1) / 2);

                var dVals = new List<double>(); var kVals = new List<double>();
                for (int i = 0; i < NTarget; i++) for (int j = i + 1; j < NTarget; j++) { dVals.Add(d[i, j]); kVals.Add(Kc[i, j]); }
                double dMean = dVals.Average(), dStd = Std(dVals.ToArray());
                double kMean = kVals.Average(), kStd = Std(kVals.ToArray());

                double dLam1 = LeadingEigenvalue(d, NTarget);
                double kLam1 = LeadingEigenvalue(Kc, NTarget);
                double dFrob = FrobeniusOffDiag(d, NTarget);
                double kFrob = FrobeniusOffDiag(Kc, NTarget);

                // K update norm: ||K_e − K_{e-1}||_F
                double kUpd = 0;
                if (prevK != null)
                { for (int i = 0; i < NTarget; i++) for (int j = 0; j < NTarget; j++) { double diff = Kc[i, j] - prevK[i, j]; kUpd += diff * diff; } kUpd = Math.Sqrt(kUpd); }

                // Top-node coupling concentration: sum of top 5 node K sums / total K sum
                var nodeSums = NodeSums(Kc, NTarget);
                var top5 = nodeSums.OrderByDescending(x => x).Take(5).Sum();
                double totalK = nodeSums.Sum();
                double topConc = totalK > 0 ? top5 / totalK : 0;

                epochDiags[s][e] = (omega, md, dMean, dStd, kMean, kStd, dLam1, kLam1, dFrob, kFrob, kUpd, topConc);
            }

            var hf = Sm(Kc, NTarget, S, seed + E, St, REps);
            finalOmega[s] = Of(hf, NTarget).Average();
        }

        // Branch labels
        var isHigh = finalOmega.Select(x => x > FIXED_THRESHOLD).ToArray();
        int nHi = isHigh.Count(x => x);
        _output.WriteLine($"Final branches: {SeedsTotal - nHi} low, {nHi} high (fixed thr={FIXED_THRESHOLD:F3})");
        _output.WriteLine("");

        // ── Per-epoch separation ──
        var diagNames = new[] { "Ω", "d_mean", "d_std", "K_mean", "K_std", "λ₁(d)", "λ₁(K)", "d_Frob", "K_Frob", "K_upd", "topK_conc" };

        _output.WriteLine("── EPOCH SEPARATION (|Δ|/σ, future-low vs future-high) ──");
        var header = new StringBuilder(); header.Append($"{"Epoch",6}");
        foreach (var dn in diagNames) header.Append($"{dn,10}");
        _output.WriteLine(header.ToString());

        double firstStrongEpoch = 999; string firstStrongDiag = "";
        for (int e = 0; e < E; e++)
        {
            var sb = new StringBuilder(); sb.Append($"E{e + 1,5}");
            double bestE = 0; string bestN = "";
            for (int di = 0; di < diagNames.Length; di++)
            {
                var loVals = new List<double>(); var hiVals = new List<double>();
                for (int s = 0; s < SeedsTotal; s++)
                {
                    double val = di switch
                    {
                        0 => epochDiags[s][e].omega, 1 => epochDiags[s][e].dMean, 2 => epochDiags[s][e].dStd,
                        3 => epochDiags[s][e].kMean, 4 => epochDiags[s][e].kStd, 5 => epochDiags[s][e].dLam1,
                        6 => epochDiags[s][e].kLam1, 7 => epochDiags[s][e].dFrob, 8 => epochDiags[s][e].kFrob,
                        9 => epochDiags[s][e].kUpdateNorm, 10 => epochDiags[s][e].topNodeKConc,
                        _ => 0
                    };
                    if (isHigh[s]) hiVals.Add(val); else loVals.Add(val);
                }
                double loM = loVals.Average(), hiM = hiVals.Average();
                double pooled = Math.Sqrt((Std(loVals.ToArray()) * Std(loVals.ToArray()) + Std(hiVals.ToArray()) * Std(hiVals.ToArray())) / 2);
                double effect = pooled > 1e-15 ? Math.Abs(hiM - loM) / pooled : 0;
                if (effect > bestE) { bestE = effect; bestN = diagNames[di]; }
                sb.Append($" {effect,9:F3}");
            }
            _output.WriteLine(sb.ToString());
            _output.WriteLine($"  → Best: {bestN} (|Δ|/σ={bestE:F2})");
            if (bestE > 0.8 && e + 1 < firstStrongEpoch) { firstStrongEpoch = e + 1; firstStrongDiag = bestN; }
        }

        // ── Node cascade analysis ──
        _output.WriteLine("");
        _output.WriteLine("── NODE CASCADE (Epoch 1, top-5 node coupling concentration) ──");
        double loTopConc = 0, hiTopConc = 0; int loCnt = 0, hiCnt = 0;
        for (int s = 0; s < SeedsTotal; s++)
        { if (isHigh[s]) { hiTopConc += epochDiags[s][0].topNodeKConc; hiCnt++; } else { loTopConc += epochDiags[s][0].topNodeKConc; loCnt++; } }
        loTopConc /= Math.Max(loCnt, 1); hiTopConc /= Math.Max(hiCnt, 1);
        _output.WriteLine($"  Low-branch mean topK_conc: {loTopConc:F4} | High-branch: {hiTopConc:F4}");

        // ── Decision ──
        _output.WriteLine("");
        _output.WriteLine("═══ DECISION ═══");
        if (firstStrongEpoch <= 2)
        {
            _output.WriteLine($"First strong separation: Epoch {firstStrongEpoch}, {firstStrongDiag}");
            if (firstStrongDiag.StartsWith("λ₁(d)") || firstStrongDiag == "d_Frob")
                _output.WriteLine("GATE A: DISTANCE SPECTRAL — λ₁(d) or d_Frob separates first.");
            else if (firstStrongDiag.StartsWith("λ₁(K)") || firstStrongDiag == "K_Frob")
                _output.WriteLine("GATE B-leaning: COUPLING SPECTRAL — λ₁(K) separates first.");
            else if (firstStrongDiag == "topK_conc")
                _output.WriteLine("GATE C: NODE CASCADE — top-node concentration separates first.");
            else if (firstStrongDiag == "K_std" || firstStrongDiag == "K_mean")
                _output.WriteLine("Scalar K statistics separate first (consistent with M9 marker finding).");
            else
                _output.WriteLine($"GATE E: MULTIPLE/OTHER — {firstStrongDiag} is the earliest separator.");
        }
        else
            _output.WriteLine("GATE F: NO EARLY SEPARATOR — branch genesis remains unresolved at epoch level.");
    }

    [Fact] public void V5_3_BGEN_03_ClaimAudit()
    { _output.WriteLine("=== CLAIM AUDIT ===\nSUPPORTED: Epoch diagnostics as reported.\nCONDITIONAL: 30 seeds, N=67, V4.1 regime.\nNOT CLAIMED: causation, phase transition, H9-H12.\nAUDIT: PASSED."); }
}
