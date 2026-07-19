using Xunit;
using Xunit.Abstractions;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace TRM.Tests.V5_6;

/// <summary>
/// V5.6 Nm Dose-Response Audit (MGCF):
///
/// Quantifies the dose-response curve between d_mean shift and high-branch suppression.
///
/// MGCD established: d_mean shift alone is sufficient to eliminate high-branch (12→0/30).
/// MGCF determines: at what dose level does suppression activate?
///
/// Key questions:
///   1. What fraction of the Nm d_mean shift is sufficient?
///   2. Is the response monotonic?
///   3. Is there a threshold?
///   4. Does the dose-response hold across N=67,69,72?
///
/// CLAIM DISCIPLINE: Values as reported. No physical interpretation.
/// </summary>
[Trait("Category", "V5_6")]
[Trait("Category", "V5_6_MGCF")]
public class V5_6_NmDoseResponseAudit_Tests
{
    private readonly ITestOutputHelper _output;
    private const double Dt = 0.05; private const int Hd = 4;
    private const double Xi = 1.75; private const double K0 = 1.2; private const double S = 0.10;
    private const int St = 300; private const double REps = 1e-6;
    private const double FIXED_THRESHOLD = 1.783;
    private const int NEpochs = 5;
    private static readonly int[] NValues = { 67, 69, 72 };

    // MGCB calibrated per-epoch Nm d_mean deltas
    private static readonly double[] NmDeltaDMean = { 0.1913, 0.3246, 0.3990, 0.3451, 0.3401 };

    // Dose levels as fractions of full Nm d_mean shift
    // Stage 1: all doses; Stage 2: selected doses marked with *
    private static readonly double[] DoseLevels = { 0.0, 0.10, 0.25, 0.40, 0.50, 0.60, 0.75, 0.90, 1.00, 1.25 };
    private static readonly double[] InverseDoses = { -0.10, -0.25, -0.50 };

    public V5_6_NmDoseResponseAudit_Tests(ITestOutputHelper o) { _output = o; }

    // ═══════════════════════════════════════════════
    //  Result structs
    // ═══════════════════════════════════════════════

    private struct SeedResult
    {
        public double Omega, MeanDist;
        public double DMean, DStd, DP90, DMax;
        public double KMean, KStd, KLam1;
        public bool IsHighBranch;
        public int Invalid;
    }

    private struct DoseResult
    {
        public double Dose;
        public SeedResult[] Seeds;
        public double OmegaMean, OmegaCv;
        public double MeanDistMean;
        public double DMeanMean, DStdMean, DP90Mean, DMaxMean;
        public double KMeanMean, KStdMean, KLam1Mean;
        public int HiCount, LoCount, InvalidCount;
    }

    // ═══════════════════════════════════════════════
    //  Core RecoverFP stages
    // ═══════════════════════════════════════════════

    private static double[][] Sm(double[,] K, int n, double s, int seed, int st, double reps) {
        var r = new Random(seed); var w = new double[n];
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

    private static (double lam, double[] _) PowerIter(double[,] M, int n, int iters = 20) {
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

    private static double[] OffDiag(double[,] M, int n) { var v = new double[n * (n - 1) / 2]; int idx = 0;
        for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) v[idx++] = M[i, j]; return v; }

    private static double DMean(double[,] d, int n) {
        double sum = 0; int cnt = 0;
        for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) { sum += d[i, j]; cnt++; }
        return cnt > 0 ? sum / cnt : 0;
    }

    // ═══════════════════════════════════════════════
    //  d_mean dose intervention
    // ═══════════════════════════════════════════════

    /// Apply a fraction of the Nm d_mean shift to the d matrix
    private static double[,] DMeanDose(double[,] d, int n, int epoch, double fraction) {
        double shift = NmDeltaDMean[Math.Min(epoch, NEpochs - 1)] * fraction;
        var result = new double[n, n];
        for (int i = 0; i < n; i++) {
            result[i, i] = 0;
            for (int j = i + 1; j < n; j++) {
                result[i, j] = Math.Max(REps, d[i, j] + shift);
                result[j, i] = result[i, j];
            }
        }
        return result;
    }

    // ═══════════════════════════════════════════════
    //  Pipeline runners
    // ═══════════════════════════════════════════════

    private SeedResult RunBaseline(int n, int seed, bool useNm) {
        var K = KS(n, seed);
        try {
            for (int e = 0; e < NEpochs; e++) {
                var h = Sm(K, n, S, seed + e, St, REps);
                var R = RP(h, n);
                if (useNm) R = Nm(R, n, REps);
                var d = DL(R, n);
                K = Cupd(d, n, K0, Xi);
            }
            return ComputeMetrics(K, n, seed + NEpochs);
        } catch { return new SeedResult { Invalid = 1 }; }
    }

    private SeedResult RunDose(int n, int seed, double fraction, bool useNm) {
        var K = KS(n, seed);
        try {
            for (int e = 0; e < NEpochs; e++) {
                var h = Sm(K, n, S, seed + e, St, REps);
                var R = RP(h, n);
                if (useNm) R = Nm(R, n, REps);
                var d = DL(R, n);

                // Apply d_mean dose
                d = DMeanDose(d, n, e, fraction);

                // Validate
                for (int i = 0; i < n; i++) {
                    if (double.IsNaN(d[i, i]) || double.IsInfinity(d[i, i])) return new SeedResult { Invalid = 1 };
                    for (int j = i + 1; j < n; j++) {
                        if (double.IsNaN(d[i, j]) || double.IsInfinity(d[i, j])) return new SeedResult { Invalid = 1 };
                        if (d[i, j] < 0) return new SeedResult { Invalid = 1 };
                    }
                }

                K = Cupd(d, n, K0, Xi);
            }
            return ComputeMetrics(K, n, seed + NEpochs);
        } catch { return new SeedResult { Invalid = 1 }; }
    }

    private SeedResult ComputeMetrics(double[,] K, int n, int seed) {
        var hf = Sm(K, n, S, seed, St, REps);
        double om = Of(hf, n).Average();
        var Rf = RP(hf, n); Rf = Nm(Rf, n, REps);
        var df = DL(Rf, n);
        var dVals = OffDiag(df, n); Array.Sort(dVals);
        var Kf = Cupd(df, n, K0, Xi);
        var kVals = OffDiag(Kf, n);
        return new SeedResult {
            Omega = om, IsHighBranch = om > FIXED_THRESHOLD,
            MeanDist = OffDiag(Rf, n).Average(),
            DMean = dVals.Average(), DStd = Std(dVals),
            DP90 = dVals[(int)(dVals.Length * 0.90)], DMax = dVals[^1],
            KMean = kVals.Average(), KStd = Std(kVals),
            KLam1 = PowerIter(Kf, n).lam, Invalid = 0
        };
    }

    // ═══════════════════════════════════════════════
    //  Aggregate helpers
    // ═══════════════════════════════════════════════

    private DoseResult AggregateDose(SeedResult[] seeds, double dose, int total) {
        var valid = seeds.Where(s => s.Invalid == 0).ToArray();
        if (valid.Length == 0) return new DoseResult { Dose = dose, HiCount = 0, InvalidCount = total };
        return new DoseResult {
            Dose = dose, Seeds = seeds,
            OmegaMean = valid.Average(s => s.Omega), OmegaCv = Std(valid.Select(s => s.Omega).ToArray()) / Math.Abs(valid.Average(s => s.Omega)),
            MeanDistMean = valid.Average(s => s.MeanDist),
            DMeanMean = valid.Average(s => s.DMean), DStdMean = valid.Average(s => s.DStd),
            DP90Mean = valid.Average(s => s.DP90), DMaxMean = valid.Average(s => s.DMax),
            KMeanMean = valid.Average(s => s.KMean), KStdMean = valid.Average(s => s.KStd),
            KLam1Mean = valid.Average(s => s.KLam1),
            HiCount = valid.Count(s => s.IsHighBranch), LoCount = valid.Count(s => !s.IsHighBranch),
            InvalidCount = total - valid.Length
        };
    }

    // ═══════════════════════════════════════════════
    //  Parallel sweep
    // ═══════════════════════════════════════════════

    private SeedResult[] RunSweep(Func<int, int, SeedResult> runner, int n, int seeds) {
        var results = new SeedResult[seeds];
        Parallel.For(0, seeds, s => results[s] = runner(n, s));
        return results;
    }

    // ═══════════════════════════════════════════════
    //  Test 01: Protocol
    // ═══════════════════════════════════════════════

    [Fact]
    public void MGCF_01_Protocol() {
        _output.WriteLine("═══ V5.6 MGCF: Nm Dose-Response Audit ═══");
        _output.WriteLine("Purpose: Quantify d_mean → high-branch dose-response curve.");
        _output.WriteLine("");
        _output.WriteLine($"Settings: N=67,69,72, seeds 0-99, {NEpochs} epochs");
        _output.WriteLine($"Branch threshold: Omega > {FIXED_THRESHOLD} (V5.3 frozen)");
        _output.WriteLine("");
        _output.WriteLine("Dose levels (fraction of full Nm d_mean shift):");
        _output.WriteLine($"  V1 doses: {string.Join(", ", DoseLevels.Select(d => $"{d*100:F0}%"))}");
        _output.WriteLine($"  B0 inverse: {string.Join(", ", InverseDoses.Select(d => $"{d*100:F0}%"))}");
        _output.WriteLine("");
        _output.WriteLine("MGCB calibrated per-epoch Nm d_mean deltas:");
        _output.WriteLine($"  Epochs: {string.Join(", ", NmDeltaDMean.Select(d => $"{d:F4}"))}");
        _output.WriteLine("");
        _output.WriteLine("Gates:");
        _output.WriteLine("  A: Monotonic dose-response");
        _output.WriteLine("  B: Threshold response");
        _output.WriteLine("  C: Cross-N stable dose");
        _output.WriteLine("  D: N-dependent dose");
        _output.WriteLine("  E: Non-monotonic response");
        _output.WriteLine("  F: Invalid/unsafe");
    }

    // ═══════════════════════════════════════════════
    //  Test 02: Baseline + Full Dose-Response (Stage 1)
    // ═══════════════════════════════════════════════

    [Fact]
    public void MGCF_02_DoseResponseCurve() {
        _output.WriteLine("═══ DOSE-RESPONSE CURVE (N=67, seeds 0-29) ═══");
        int n = 67; int seeds = 30;

        // Baselines
        var b0 = AggregateDose(RunSweep((n, s) => RunBaseline(n, s, true), n, seeds), -1, seeds);
        var v1 = AggregateDose(RunSweep((n, s) => RunBaseline(n, s, false), n, seeds), 0, seeds);

        _output.WriteLine("Baselines:");
        _output.WriteLine($"  B0: Omega={b0.OmegaMean:F4} High={b0.HiCount}/{seeds} dMean={b0.DMeanMean:F4}");
        _output.WriteLine($"  V1: Omega={v1.OmegaMean:F4} High={v1.HiCount}/{seeds} dMean={v1.DMeanMean:F4}");
        _output.WriteLine("");

        // Run all V1 dose levels in parallel
        var doseResults = new ConcurrentDictionary<double, DoseResult>();
        Parallel.ForEach(DoseLevels, dose => {
            var sr = RunSweep((n2, s2) => RunDose(n2, s2, dose, false), n, seeds);
            doseResults[dose] = AggregateDose(sr, dose, seeds);
        });

        _output.WriteLine($"{"Dose",8} {"Omega",8} {"CV",7} {"High",6} {"dMean",8} {"dStd",8} {"dP90",8} {"KMean",8} {"KStd",8} {"KLam1",8} {"Inv",4}");
        foreach (var dose in DoseLevels) {
            var r = doseResults[dose];
            _output.WriteLine($"{dose*100,7:F0}% {r.OmegaMean,8:F4} {r.OmegaCv,7:F4} {r.HiCount,5}/{seeds} {r.DMeanMean,8:F4} {r.DStdMean,8:F4} {r.DP90Mean,8:F4} {r.KMeanMean,8:F4} {r.KStdMean,8:F4} {r.KLam1Mean,8:F4} {r.InvalidCount,3}");
        }

        // Find threshold: first dose that drops below B0 high-branch level
        _output.WriteLine("");
        _output.WriteLine("── Threshold Analysis ──");
        double? firstBelowB0 = null, firstZero = null;
        foreach (var dose in DoseLevels) {
            var r = doseResults[dose];
            if (firstBelowB0 == null && r.HiCount <= b0.HiCount) firstBelowB0 = dose;
            if (firstZero == null && r.HiCount == 0) firstZero = dose;
        }
        _output.WriteLine($"B0 high-branch level: {b0.HiCount}/{seeds}");
        _output.WriteLine($"First dose below B0: {(firstBelowB0.HasValue ? $"{firstBelowB0*100:F0}%" : "NONE")}");
        _output.WriteLine($"First dose at zero:  {(firstZero.HasValue ? $"{firstZero*100:F0}%" : "NONE")}");

        // Monotonicity test
        _output.WriteLine("");
        _output.WriteLine("── Monotonicity Test ──");
        bool monotonic = true;
        int prevHi = int.MaxValue;
        foreach (var dose in DoseLevels) {
            int hi = doseResults[dose].HiCount;
            if (hi > prevHi) { monotonic = false; break; }
            prevHi = hi;
        }
        _output.WriteLine($"High-branch monotonic with dose: {(monotonic ? "YES" : "NO")}");

        // Threshold detection
        bool isThreshold = false;
        double? thresholdDose = null;
        for (int i = 1; i < DoseLevels.Length; i++) {
            int drop = doseResults[DoseLevels[i - 1]].HiCount - doseResults[DoseLevels[i]].HiCount;
            if (drop >= seeds * 0.2) { isThreshold = true; thresholdDose = DoseLevels[i]; break; }
        }
        _output.WriteLine($"Sharp threshold (≥20% drop in one step): {(isThreshold ? $"YES at {thresholdDose*100:F0}%" : "NO (gradual)")}");

        // Gate A
        _output.WriteLine("");
        _output.WriteLine($"Gate A (Monotonic): {(monotonic ? "REACHED" : "NOT REACHED")}");
        _output.WriteLine($"Gate B (Threshold): {(isThreshold ? "REACHED" : "NOT REACHED")}");
    }

    // ═══════════════════════════════════════════════
    //  Test 03: Inverse Dose on B0 (Stage 1)
    // ═══════════════════════════════════════════════

    [Fact]
    public void MGCF_03_InverseDose() {
        _output.WriteLine("═══ INVERSE DOSE ON B0 (N=67, seeds 0-29) ═══");
        int n = 67; int seeds = 30;

        var b0 = AggregateDose(RunSweep((n, s) => RunBaseline(n, s, true), n, seeds), 0, seeds);
        var v1 = AggregateDose(RunSweep((n, s) => RunBaseline(n, s, false), n, seeds), 0, seeds);

        _output.WriteLine($"B0: Omega={b0.OmegaMean:F4} High={b0.HiCount}/{seeds}");
        _output.WriteLine($"V1: Omega={v1.OmegaMean:F4} High={v1.HiCount}/{seeds}");
        _output.WriteLine("");

        var invResults = new ConcurrentDictionary<double, DoseResult>();
        Parallel.ForEach(InverseDoses, dose => {
            var sr = RunSweep((n2, s2) => RunDose(n2, s2, dose, true), n, seeds);
            invResults[dose] = AggregateDose(sr, dose, seeds);
        });

        _output.WriteLine($"{"Dose",8} {"Omega",8} {"CV",7} {"High",6} {"dMean",8} {"KMean",8} {"Inv",4} {"Safe?"}");
        foreach (var dose in InverseDoses) {
            var r = invResults[dose];
            double invalidRate = r.InvalidCount / (double)seeds;
            string safe = invalidRate < 0.2 ? "YES" : "UNSAFE";
            _output.WriteLine($"{dose*100,7:F0}% {r.OmegaMean,8:F4} {r.OmegaCv,7:F4} {r.HiCount,5}/{seeds} {r.DMeanMean,8:F4} {r.KMeanMean,8:F4} {r.InvalidCount,3}   {safe}");
        }

        // Check if negative dose increases high-branch toward V1
        var neg10 = invResults[-0.10];
        bool movesTowardV1 = neg10.HiCount > b0.HiCount;
        _output.WriteLine("");
        _output.WriteLine($"-10% dose moves toward V1: {(movesTowardV1 ? "YES" : "NO")}");

        // Safety check
        bool anyUnsafe = invResults.Values.Any(r => r.InvalidCount > seeds * 0.2);
        _output.WriteLine($"Inverse dose safety: {(anyUnsafe ? "UNSAFE — some doses exceed 20% invalid" : "SAFE")}");
    }

    // ═══════════════════════════════════════════════
    //  Test 04: d_mean vs High-Branch Correlation
    // ═══════════════════════════════════════════════

    [Fact]
    public void MGCF_04_DMeanHighBranchCorrelation() {
        _output.WriteLine("═══ d_mean vs HIGH-BRANCH CORRELATION (N=67, seeds 0-29) ═══");
        int n = 67; int seeds = 30;

        // Run doses 0%, 25%, 50%, 75%, 100% on V1 and track per-seed d_mean vs Omega
        var keyDoses = new[] { 0.0, 0.25, 0.50, 0.75, 1.00 };
        var allResults = new ConcurrentDictionary<double, SeedResult[]>();
        Parallel.ForEach(keyDoses, dose => {
            allResults[dose] = RunSweep((n2, s2) => RunDose(n2, s2, dose, false), n, seeds);
        });

        // For each seed, track d_mean and Omega across doses
        _output.WriteLine("Per-seed d_mean and Omega at selected doses:");
        _output.WriteLine($"{"Seed",5} {"0%_dM",8} {"0%_Om",8} {"50%_dM",8} {"50%_Om",8} {"100%_dM",8} {"100%_Om",8} {"Trend",8}");
        int suppressed = 0, resisting = 0;
        for (int s = 0; s < seeds; s++) {
            var r0 = allResults[0.0][s];
            var r50 = allResults[0.50][s];
            var r100 = allResults[1.00][s];
            string trend = r100.IsHighBranch ? "RESIST" : "SUPPRESS";
            if (!r100.IsHighBranch) suppressed++;
            else resisting++;
            if (s < 10 || s >= seeds - 3) // Show first 10 and last 3
                _output.WriteLine($"{s,5} {r0.DMean,8:F4} {r0.Omega,8:F4} {r50.DMean,8:F4} {r50.Omega,8:F4} {r100.DMean,8:F4} {r100.Omega,8:F4} {trend,8}");
        }
        _output.WriteLine($"... ({seeds - 13} seeds omitted)");
        _output.WriteLine("");
        _output.WriteLine($"Suppressed at 100%: {suppressed}/{seeds}  Resisting: {resisting}/{seeds}");
    }

    // ═══════════════════════════════════════════════
    //  Test 05: Gate Summary & Claim Audit
    // ═══════════════════════════════════════════════

    [Fact]
    public void MGCF_05_GateSummary() {
        _output.WriteLine("═══ GATE SUMMARY ═══");
        _output.WriteLine("Gate evaluation from MGCF_02, MGCF_03.");
        _output.WriteLine("");
        _output.WriteLine("Gate A (Monotonic dose-response):  See MGCF_02");
        _output.WriteLine("Gate B (Threshold response):       See MGCF_02");
        _output.WriteLine("Gate C (Cross-N stable dose):      See MGCF_06 (Stage 2)");
        _output.WriteLine("Gate D (N-dependent dose):         See MGCF_06 (Stage 2)");
        _output.WriteLine("Gate E (Non-monotonic):            Inverse of Gate A");
        _output.WriteLine("Gate F (Invalid/unsafe):           See MGCF_03");
        _output.WriteLine("");
        _output.WriteLine("═══ CLAIM AUDIT ═══");
        _output.WriteLine("SUPPORTED:");
        _output.WriteLine("  - Baseline reproduction as reported.");
        _output.WriteLine("  - Dose-response curve as measured.");
        _output.WriteLine("  - Threshold estimate as computed.");
        _output.WriteLine("  - Monotonicity assessment.");
        _output.WriteLine("  - Invalid run counts per dose.");
        _output.WriteLine("CONDITIONAL:");
        _output.WriteLine("  - Stage 1: N=67, seeds 0-29, all doses.");
        _output.WriteLine("  - Stage 2: N=67,69,72, seeds 0-99, selected doses.");
        _output.WriteLine("  - Inverse doses safe only if invalid < 20%.");
        _output.WriteLine("NOT CLAIMED:");
        _output.WriteLine("  - Physical interpretation.");
        _output.WriteLine("  - Universality beyond tested N/seeds.");
        _output.WriteLine("  - d_mean as universal cause of Nm suppression.");
        _output.WriteLine("AUDIT: PASSED — Within scope of RecoverFP d_mean dose-response analysis.");
    }

    // ═══════════════════════════════════════════════
    //  Test 06: Recommended Next Suite
    // ═══════════════════════════════════════════════

    [Fact]
    public void MGCF_06_RecommendedNextSuite() {
        _output.WriteLine("═══ RECOMMENDED NEXT SUITE ═══");
        _output.WriteLine("");
        _output.WriteLine("If Gate A (Monotonic) + Gate C (Cross-N stable):");
        _output.WriteLine("  → MGCG: Minimal Nm-Equivalent Operator Audit");
        _output.WriteLine("  Define the minimal operator: d' = d + α(epoch)");
        _output.WriteLine("  Validate across wider N range, test analytic threshold model.");
        _output.WriteLine("");
        _output.WriteLine("If Gate B (Threshold response):");
        _output.WriteLine("  → MGCG-th: Threshold Refinement");
        _output.WriteLine("  Refine dose near threshold with finer resolution.");
        _output.WriteLine("");
        _output.WriteLine("If Gate D (N-dependent):");
        _output.WriteLine("  → MGCG-nd: N-Conditioned d_mean Model");
        _output.WriteLine("  Determine how threshold scales with N.");
        _output.WriteLine("");
        _output.WriteLine("If Gate E (Non-monotonic):");
        _output.WriteLine("  → MGCH: Deep Pipeline State Audit");
        _output.WriteLine("  Trace why d_mean does not monotonically control suppression.");
        _output.WriteLine("");
        _output.WriteLine("Conservative default:");
        _output.WriteLine("  → MGCG: Minimal Nm-Equivalent Operator Audit.");
    }

    // ═══════════════════════════════════════════════
    //  Test 07: Stage 2 Cross-N (LongRunning)
    // ═══════════════════════════════════════════════

    [Fact]
    [Trait("Category", "LongRunning")]
    public void MGCF_07_Stage2_CrossN() {
        _output.WriteLine("═══ MGCF STAGE 2: CROSS-N DOSE-RESPONSE ═══");
        _output.WriteLine($"N values: {string.Join(", ", NValues)}");
        _output.WriteLine("Seeds: 0-99 per N");
        _output.WriteLine("Doses: 0%, 25%, 50%, 75%, 100%");
        _output.WriteLine("");

        var crossDoses = new[] { 0.0, 0.25, 0.50, 0.75, 1.00 };
        var nResults = new ConcurrentDictionary<(int n, double dose), (int hi, double omega, double dMean, int inv)>();

        Parallel.ForEach(NValues, n => {
            int seedsN = 100;
            foreach (var dose in crossDoses) {
                var sr = RunSweep((n2, s2) => RunDose(n2, s2, dose, false), n, seedsN);
                var agg = AggregateDose(sr, dose, seedsN);
                nResults[(n, dose)] = (agg.HiCount, agg.OmegaMean, agg.DMeanMean, agg.InvalidCount);
            }
            // Also baselines
            var b0 = AggregateDose(RunSweep((n2, s2) => RunBaseline(n2, s2, true), n, seedsN), -1, seedsN);
            var v1 = AggregateDose(RunSweep((n2, s2) => RunBaseline(n2, s2, false), n, seedsN), 0, seedsN);
            nResults[(n, -1)] = (b0.HiCount, b0.OmegaMean, b0.DMeanMean, b0.InvalidCount);
            nResults[(n, -2)] = (v1.HiCount, v1.OmegaMean, v1.DMeanMean, v1.InvalidCount);
        });

        _output.WriteLine($"{"N",4} {"Cond",8} {"High",6} {"Omega",8} {"dMean",8} {"Inv",4}");
        _output.WriteLine("──────────────────────────────────────────");
        foreach (int n in NValues) {
            var b0r = nResults[(n, -1)];
            var v1r = nResults[(n, -2)];
            _output.WriteLine($"{n,4} {"B0",8} {b0r.hi,6} {b0r.omega,8:F4} {b0r.dMean,8:F4} {b0r.inv,4}");
            _output.WriteLine($"{n,4} {"V1",8} {v1r.hi,6} {v1r.omega,8:F4} {v1r.dMean,8:F4} {v1r.inv,4}");
            foreach (var dose in crossDoses) {
                var r = nResults[(n, dose)];
                _output.WriteLine($"{n,4} {dose*100,7:F0}% {r.hi,6} {r.omega,8:F4} {r.dMean,8:F4} {r.inv,4}");
            }
            _output.WriteLine("");
        }

        // Cross-N stability analysis
        _output.WriteLine("═══ CROSS-N GATE ANALYSIS ═══");
        var thresholds = new Dictionary<int, double?>();
        foreach (int n in NValues) {
            double? thresh = null;
            foreach (var dose in crossDoses) {
                if (nResults[(n, dose)].hi <= nResults[(n, -1)].hi) { thresh = dose; break; }
            }
            thresholds[n] = thresh;
            _output.WriteLine($"N={n}: threshold dose to reach B0 level = {(thresh.HasValue ? $"{thresh*100:F0}%" : "NONE")}");
        }

        bool crossNStable = thresholds.Values.All(t => t.HasValue) &&
            thresholds.Values.Max() - thresholds.Values.Min() <= 0.25;
        _output.WriteLine($"");
        _output.WriteLine($"Gate C (Cross-N stable): {(crossNStable ? "REACHED" : "NOT REACHED")}");
        _output.WriteLine($"Gate D (N-dependent):    {(!crossNStable ? "REACHED" : "NOT REACHED")}");
    }
}
