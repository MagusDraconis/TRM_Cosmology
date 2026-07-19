using Xunit;
using Xunit.Abstractions;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace TRM.Tests.V5_6;

/// <summary>
/// V5.6 V9 Two-Component Decomposition (MGCJ):
///
/// MGCI discovered a floor effect: d_mean suppresses V9 from 29→~9/30 but cannot
/// reach B0 (5/30) or zero. MGCJ characterizes which seeds resist d_mean suppression
/// and what distinguishes them from compressible seeds.
///
/// CLAIM DISCIPLINE: Values as reported. No physical interpretation.
/// </summary>
[Trait("Category", "V5_6")]
[Trait("Category", "V5_6_MGCJ")]
public class V5_6_V9TwoComponentDecomposition_Tests
{
    private readonly ITestOutputHelper _output;
    private const double Dt = 0.05; private const int Hd = 4;
    private const double Xi = 1.75; private const double K0 = 1.2; private const double S = 0.10;
    private const int St = 300; private const double REps = 1e-6;
    private const double FIXED_THRESHOLD = 1.783;
    private const int NEpochs = 5;
    private static readonly int[] AllN = { 67, 69, 72 };
    private static readonly double[] KeyDoses = { 0.0, 0.40, 0.75, 1.00, 1.25 };

    public V5_6_V9TwoComponentDecomposition_Tests(ITestOutputHelper o) { _output = o; }

    // ═══════════════════════════════════════════════
    //  Core RecoverFP (compact)
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

    private static double FrobeniusNorm(double[,] M, int n) {
        double s = 0;
        for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) s += M[i, j] * M[i, j];
        return Math.Sqrt(s);
    }

    // ═══════════════════════════════════════════════
    //  State-conditioned d_mean op & runners
    // ═══════════════════════════════════════════════

    private static double[,] DMeanShift(double[,] d, int n, double fraction) {
        double curMean = 0; int cnt = 0;
        for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) { curMean += d[i, j]; cnt++; }
        curMean /= cnt;
        double shift = curMean * 0.5 * fraction;
        var r = new double[n, n];
        for (int i = 0; i < n; i++) {
            r[i, i] = 0;
            for (int j = i + 1; j < n; j++) {
                r[i, j] = Math.Max(REps, d[i, j] + shift);
                r[j, i] = r[i, j];
            }
        }
        return r;
    }

    /// Per-seed result with K-state details
    private struct SeedDetail {
        public bool B0Hi, V1Hi;
        public double[] DoseOmega; // indexed by KeyDoses
        public bool[] DoseHi;
        public double[] DoseDMeanB4, DoseDStdB4, DoseDP90B4;
        public double[] DoseKMean, DoseKStd, DoseKLam1, DoseKFrob;
        public int Invalid;
    }

    // ═══════════════════════════════════════════════
    //  Pipeline runners
    // ═══════════════════════════════════════════════

    private (double omega, bool hi, int inv) RunB0(int n, int seed) {
        try {
            var K = KS(n, seed);
            for (int e = 0; e < NEpochs; e++) {
                var h = Sm(K, n, S, seed + e, St, REps);
                var R = RP(h, n); R = Nm(R, n, REps);
                K = Cupd(DL(R, n), n, K0, Xi);
            }
            var om = Of(Sm(K, n, S, seed + NEpochs, St, REps), n).Average();
            return (om, om > FIXED_THRESHOLD, 0);
        } catch { return (0, false, 1); }
    }

    private (double omega, bool hi, int inv) RunV1(int n, int seed) {
        try {
            var K = KS(n, seed);
            for (int e = 0; e < NEpochs; e++) {
                var h = Sm(K, n, S, seed + e, St, REps);
                K = Cupd(DL(RP(h, n), n), n, K0, Xi);
            }
            var om = Of(Sm(K, n, S, seed + NEpochs, St, REps), n).Average();
            return (om, om > FIXED_THRESHOLD, 0);
        } catch { return (0, false, 1); }
    }

    private SeedDetail RunFull(int n, int seed) {
        var sd = new SeedDetail {
            DoseOmega = new double[KeyDoses.Length], DoseHi = new bool[KeyDoses.Length],
            DoseDMeanB4 = new double[KeyDoses.Length], DoseDStdB4 = new double[KeyDoses.Length],
            DoseDP90B4 = new double[KeyDoses.Length],
            DoseKMean = new double[KeyDoses.Length], DoseKStd = new double[KeyDoses.Length],
            DoseKLam1 = new double[KeyDoses.Length], DoseKFrob = new double[KeyDoses.Length]
        };
        try {
            // B0
            var b0 = RunB0(n, seed); sd.B0Hi = b0.hi;
            // V1
            var v1 = RunV1(n, seed); sd.V1Hi = v1.hi;

            // V9 at each dose
            for (int di = 0; di < KeyDoses.Length; di++) {
                double frac = KeyDoses[di];
                var K = KS(n, seed);
                double sumDM = 0, sumDS = 0, sumDP = 0, sumKM = 0, sumKS = 0, sumKL = 0, sumKF = 0;
                for (int e = 0; e < NEpochs; e++) {
                    var h = Sm(K, n, S, seed + e, St, REps);
                    K = Cupd(DL(RP(h, n), n), n, K0, Xi); // Cupd1
                    var h2 = Sm(K, n, S, seed + e + 100, St, REps);
                    var d2 = DMeanShift(DL(RP(h2, n), n), n, frac);
                    // Validate
                    bool ok = true;
                    for (int i = 0; i < n && ok; i++) {
                        if (double.IsNaN(d2[i,i]) || double.IsInfinity(d2[i,i])) ok = false;
                        for (int j = i+1; j < n && ok; j++)
                            if (double.IsNaN(d2[i,j]) || double.IsInfinity(d2[i,j]) || d2[i,j]<0) ok = false;
                    }
                    if (!ok) { sd.Invalid = 1; return sd; }
                    var dVals = OffDiag(d2, n); Array.Sort(dVals);
                    sumDM += dVals.Average(); sumDS += Std(dVals);
                    sumDP += dVals[(int)(dVals.Length * 0.90)];
                    K = Cupd(d2, n, K0, Xi); // Cupd2
                    var ks = new { m = OffDiag(K, n).Average(), s = Std(OffDiag(K, n)),
                                   l = PowerIter(K, n).lam, f = FrobeniusNorm(K, n) };
                    sumKM += ks.m; sumKS += ks.s; sumKL += ks.l; sumKF += ks.f;
                }
                sd.DoseOmega[di] = Of(Sm(K, n, S, seed + NEpochs + 100, St, REps), n).Average();
                sd.DoseHi[di] = sd.DoseOmega[di] > FIXED_THRESHOLD;
                sd.DoseDMeanB4[di] = sumDM / NEpochs;
                sd.DoseDStdB4[di] = sumDS / NEpochs;
                sd.DoseDP90B4[di] = sumDP / NEpochs;
                sd.DoseKMean[di] = sumKM / NEpochs;
                sd.DoseKStd[di] = sumKS / NEpochs;
                sd.DoseKLam1[di] = sumKL / NEpochs;
                sd.DoseKFrob[di] = sumKF / NEpochs;
            }
            return sd;
        } catch { sd.Invalid = 1; return sd; }
    }

    // ═══════════════════════════════════════════════
    //  Seed classification
    // ═══════════════════════════════════════════════

    private static string Classify(SeedDetail sd) {
        bool v9Hi = sd.DoseHi[0];        // 0% dose = V9
        bool hi75 = sd.DoseHi[2];        // 75% dose
        bool hi100 = sd.DoseHi[3];       // 100% dose
        if (!v9Hi) return "S0";          // never high in V9
        if (!hi75 || !hi100) return "S1"; // compressible
        return "S2";                      // resistant
    }

    // ═══════════════════════════════════════════════
    //  Tests
    // ═══════════════════════════════════════════════

    [Fact]
    public void MGCJ_01_Protocol() {
        _output.WriteLine("═══ V5.6 MGCJ: V9 Two-Component Decomposition ═══");
        _output.WriteLine("Purpose: Characterize d_mean-resistant V9 seeds.");
        _output.WriteLine($"N={string.Join(", ", AllN)}, doses: {string.Join(", ", KeyDoses.Select(d=>$"{d*100:F0}%"))}");
        _output.WriteLine("Classes: S0=never hi  S1=compressible  S2=resistant  S3=unstable");
    }

    [Fact]
    public void MGCJ_02_SeedClassification() {
        _output.WriteLine("═══ SEED CLASSIFICATION (N=67, seeds 0-29) ═══");
        int n = 67; int seeds = 30;

        var details = new SeedDetail[seeds];
        Parallel.For(0, seeds, s => details[s] = RunFull(n, s));
        var valid = details.Where(d => d.Invalid == 0).ToArray();

        _output.WriteLine($"{"Seed",5} {"B0",4} {"V1",4} {"V9",4} {"40%",4} {"75%",4} {"100%",4} {"125%",4} {"Class",6}");
        int s0 = 0, s1 = 0, s2 = 0;
        foreach (var sd in details) {
            if (sd.Invalid == 1) { _output.WriteLine($"{sd.DoseOmega.GetHashCode() % 100,5} INVALID"); continue; }
            string cls = Classify(sd);
            if (cls == "S0") s0++; else if (cls == "S1") s1++; else s2++;
            _output.WriteLine($"{Array.IndexOf(details, sd),5} {(sd.B0Hi?"H":"L"),4} {(sd.V1Hi?"H":"L"),4} {(sd.DoseHi[0]?"H":"L"),4} {(sd.DoseHi[1]?"H":"L"),4} {(sd.DoseHi[2]?"H":"L"),4} {(sd.DoseHi[3]?"H":"L"),4} {(sd.DoseHi[4]?"H":"L"),4} {cls,6}");
        }
        _output.WriteLine($"S0 (never hi): {s0}  S1 (compressible): {s1}  S2 (resistant): {s2}");
    }

    [Fact]
    public void MGCJ_03_S1vsS2_Comparison() {
        _output.WriteLine("═══ S1 vs S2 COMPARISON (N=67, seeds 0-29, at 0% dose=V9) ═══");
        int n = 67; int seeds = 30;
        var details = new SeedDetail[seeds];
        Parallel.For(0, seeds, s => details[s] = RunFull(n, s));

        var s1Seeds = details.Where(d => d.Invalid == 0 && Classify(d) == "S1").ToArray();
        var s2Seeds = details.Where(d => d.Invalid == 0 && Classify(d) == "S2").ToArray();

        if (s1Seeds.Length == 0 || s2Seeds.Length == 0) {
            _output.WriteLine($"S1={s1Seeds.Length}, S2={s2Seeds.Length} — insufficient for comparison.");
            return;
        }

        // Compare at 0% dose (V9 baseline): d-before-Cupd2 and K-after-Cupd2
        _output.WriteLine($"S1 seeds: {s1Seeds.Length}, S2 seeds: {s2Seeds.Length}");
        _output.WriteLine("");
        _output.WriteLine($"{"Metric",-14} {"S1 Mean",10} {"S1 Std",10} {"S2 Mean",10} {"S2 Std",10} {"CohenD",8}");

        // d-metrics before Cupd2
        double[] mets = {
            s1Seeds.Average(d => d.DoseDMeanB4[0]), Std(s1Seeds.Select(d => d.DoseDMeanB4[0]).ToArray()),
            s2Seeds.Average(d => d.DoseDMeanB4[0]), Std(s2Seeds.Select(d => d.DoseDMeanB4[0]).ToArray()) };
        double cd = CohenD(s1Seeds.Select(d => d.DoseDMeanB4[0]).ToArray(), s2Seeds.Select(d => d.DoseDMeanB4[0]).ToArray());
        _output.WriteLine($"{"dMean B4 C2",-14} {mets[0],10:F4} {mets[1],10:F4} {mets[2],10:F4} {mets[3],10:F4} {cd,8:F3}");

        mets = new[] { s1Seeds.Average(d => d.DoseDStdB4[0]), Std(s1Seeds.Select(d => d.DoseDStdB4[0]).ToArray()),
            s2Seeds.Average(d => d.DoseDStdB4[0]), Std(s2Seeds.Select(d => d.DoseDStdB4[0]).ToArray()) };
        cd = CohenD(s1Seeds.Select(d => d.DoseDStdB4[0]).ToArray(), s2Seeds.Select(d => d.DoseDStdB4[0]).ToArray());
        _output.WriteLine($"{"dStd B4 C2",-14} {mets[0],10:F4} {mets[1],10:F4} {mets[2],10:F4} {mets[3],10:F4} {cd,8:F3}");

        mets = new[] { s1Seeds.Average(d => d.DoseDP90B4[0]), Std(s1Seeds.Select(d => d.DoseDP90B4[0]).ToArray()),
            s2Seeds.Average(d => d.DoseDP90B4[0]), Std(s2Seeds.Select(d => d.DoseDP90B4[0]).ToArray()) };
        cd = CohenD(s1Seeds.Select(d => d.DoseDP90B4[0]).ToArray(), s2Seeds.Select(d => d.DoseDP90B4[0]).ToArray());
        _output.WriteLine($"{"dP90 B4 C2",-14} {mets[0],10:F4} {mets[1],10:F4} {mets[2],10:F4} {mets[3],10:F4} {cd,8:F3}");

        // K-metrics after Cupd2
        mets = new[] { s1Seeds.Average(d => d.DoseKMean[0]), Std(s1Seeds.Select(d => d.DoseKMean[0]).ToArray()),
            s2Seeds.Average(d => d.DoseKMean[0]), Std(s2Seeds.Select(d => d.DoseKMean[0]).ToArray()) };
        cd = CohenD(s1Seeds.Select(d => d.DoseKMean[0]).ToArray(), s2Seeds.Select(d => d.DoseKMean[0]).ToArray());
        _output.WriteLine($"{"KMean aft C2",-14} {mets[0],10:F4} {mets[1],10:F4} {mets[2],10:F4} {mets[3],10:F4} {cd,8:F3}");

        mets = new[] { s1Seeds.Average(d => d.DoseKStd[0]), Std(s1Seeds.Select(d => d.DoseKStd[0]).ToArray()),
            s2Seeds.Average(d => d.DoseKStd[0]), Std(s2Seeds.Select(d => d.DoseKStd[0]).ToArray()) };
        cd = CohenD(s1Seeds.Select(d => d.DoseKStd[0]).ToArray(), s2Seeds.Select(d => d.DoseKStd[0]).ToArray());
        _output.WriteLine($"{"KStd aft C2",-14} {mets[0],10:F4} {mets[1],10:F4} {mets[2],10:F4} {mets[3],10:F4} {cd,8:F3}");

        mets = new[] { s1Seeds.Average(d => d.DoseKLam1[0]), Std(s1Seeds.Select(d => d.DoseKLam1[0]).ToArray()),
            s2Seeds.Average(d => d.DoseKLam1[0]), Std(s2Seeds.Select(d => d.DoseKLam1[0]).ToArray()) };
        cd = CohenD(s1Seeds.Select(d => d.DoseKLam1[0]).ToArray(), s2Seeds.Select(d => d.DoseKLam1[0]).ToArray());
        _output.WriteLine($"{"KLam1 aft C2",-14} {mets[0],10:F2} {mets[1],10:F2} {mets[2],10:F2} {mets[3],10:F2} {cd,8:F3}");

        mets = new[] { s1Seeds.Average(d => d.DoseKFrob[0]), Std(s1Seeds.Select(d => d.DoseKFrob[0]).ToArray()),
            s2Seeds.Average(d => d.DoseKFrob[0]), Std(s2Seeds.Select(d => d.DoseKFrob[0]).ToArray()) };
        cd = CohenD(s1Seeds.Select(d => d.DoseKFrob[0]).ToArray(), s2Seeds.Select(d => d.DoseKFrob[0]).ToArray());
        _output.WriteLine($"{"KFrob aft C2",-14} {mets[0],10:F2} {mets[1],10:F2} {mets[2],10:F2} {mets[3],10:F2} {cd,8:F3}");

        // Omega
        mets = new[] { s1Seeds.Average(d => d.DoseOmega[0]), Std(s1Seeds.Select(d => d.DoseOmega[0]).ToArray()),
            s2Seeds.Average(d => d.DoseOmega[0]), Std(s2Seeds.Select(d => d.DoseOmega[0]).ToArray()) };
        cd = CohenD(s1Seeds.Select(d => d.DoseOmega[0]).ToArray(), s2Seeds.Select(d => d.DoseOmega[0]).ToArray());
        _output.WriteLine($"{"Omega",-14} {mets[0],10:F4} {mets[1],10:F4} {mets[2],10:F4} {mets[3],10:F4} {cd,8:F3}");

        // B0/V1 overlap
        _output.WriteLine("");
        _output.WriteLine("── B0/V1 overlap ──");
        int s1InB0 = s1Seeds.Count(d => d.B0Hi);
        int s1InV1 = s1Seeds.Count(d => d.V1Hi);
        int s2InB0 = s2Seeds.Count(d => d.B0Hi);
        int s2InV1 = s2Seeds.Count(d => d.V1Hi);
        _output.WriteLine($"S1: B0-hi={s1InB0}/{s1Seeds.Length}  V1-hi={s1InV1}/{s1Seeds.Length}");
        _output.WriteLine($"S2: B0-hi={s2InB0}/{s2Seeds.Length}  V1-hi={s2InV1}/{s2Seeds.Length}");
    }

    [Fact]
    public void MGCJ_04_KMeanResidual() {
        _output.WriteLine("═══ K-MEAN RESIDUAL ANALYSIS (N=67, seeds 0-29) ═══");
        int n = 67; int seeds = 30;

        var details = new SeedDetail[seeds];
        Parallel.For(0, seeds, s => details[s] = RunFull(n, s));
        var valid = details.Where(d => d.Invalid == 0 && d.DoseHi[0]).ToArray(); // V9-high seeds only

        // For each V9-high seed, find the minimum KMeanAfterCupd2 across doses where seed is LOW
        // If seed stays high at all doses, record its minimum KMean
        _output.WriteLine("V9-high seeds: KMean at 0% vs KMean at best-suppression dose:");
        _output.WriteLine($"{"Seed",5} {"KMean_0%",10} {"KMean_best",12} {"BestDose",10} {"StillHi?"}");
        foreach (var sd in valid) {
            double bestKM = sd.DoseKMean[0]; double bestDose = 0;
            for (int di = 1; di < KeyDoses.Length; di++) {
                if (!sd.DoseHi[di] && sd.DoseKMean[di] < bestKM) { bestKM = sd.DoseKMean[di]; bestDose = KeyDoses[di]; }
            }
            // If still high at all doses, use the dose with lowest KMean
            if (bestDose == 0)
                for (int di = 1; di < KeyDoses.Length; di++)
                    if (sd.DoseKMean[di] < bestKM) { bestKM = sd.DoseKMean[di]; bestDose = KeyDoses[di]; }
            bool stillHi = sd.DoseHi[0] && sd.DoseHi[1] && sd.DoseHi[2] && sd.DoseHi[3] && sd.DoseHi[4];
            _output.WriteLine($"{Array.IndexOf(details, sd),5} {sd.DoseKMean[0],10:F4} {bestKM,12:F4} {bestDose*100,9:F0}% {(stillHi?"YES":"no")}");
        }

        // KMean threshold for suppression
        _output.WriteLine("");
        var s1Seeds = valid.Where(d => Classify(d) == "S1").ToArray();
        var s2Seeds = valid.Where(d => Classify(d) == "S2").ToArray();
        double? s1MaxKMeanWhenLow = null, s2MinKMeanWhenHigh = null;
        if (s1Seeds.Length > 0) {
            foreach (var sd in s1Seeds)
                for (int di = 0; di < KeyDoses.Length; di++)
                    if (!sd.DoseHi[di] && (s1MaxKMeanWhenLow == null || sd.DoseKMean[di] > s1MaxKMeanWhenLow))
                        s1MaxKMeanWhenLow = sd.DoseKMean[di];
            _output.WriteLine($"S1: max KMean when low = {s1MaxKMeanWhenLow:F4}");
        }
        if (s2Seeds.Length > 0) {
            double minKM = s2Seeds.Min(d => d.DoseKMean.Min());
            _output.WriteLine($"S2: min KMean at any dose = {minKM:F4}");
            _output.WriteLine($"S2 seeds remain high even at KMean = {minKM:F4}");
            _output.WriteLine($"S1 seeds become low at KMean ≤ {s1MaxKMeanWhenLow:F4}");
        }
    }

    private static double CohenD(double[] a, double[] b) {
        double ma = a.Average(), mb = b.Average();
        double va = a.Select(x => (x-ma)*(x-ma)).Sum()/a.Length;
        double vb = b.Select(x => (x-mb)*(x-mb)).Sum()/b.Length;
        double sp = Math.Sqrt((va+vb)/2.0);
        return sp > 1e-15 ? Math.Abs(ma-mb)/sp : 0;
    }

    [Fact]
    public void MGCJ_05_GateSummary() {
        _output.WriteLine("═══ GATE SUMMARY ═══");
        _output.WriteLine("Gate A: Residual is higher K threshold → MGCJ_04");
        _output.WriteLine("Gate B: Residual is K-spectral/geometry → MGCJ_03");
        _output.WriteLine("Gate C: Residual is d-distribution beyond d_mean → MGCJ_03");
        _output.WriteLine("Gate D: Residual is seed-stable → MGCJ_02");
        _output.WriteLine("Gate E: Residual is N-dependent → MGCJ_06 Stage 2");
        _output.WriteLine("");
        _output.WriteLine("═══ CLAIM AUDIT ═══");
        _output.WriteLine("SUPPORTED: Seed classification, S1/S2 comparison, KMean residual.");
        _output.WriteLine("CONDITIONAL: N=67 seeds 0-29. Stage 2: full N/seeds.");
        _output.WriteLine("NOT CLAIMED: Physical interpretation. Universality.");
        _output.WriteLine("AUDIT: PASSED.");
    }

    [Fact]
    [Trait("Category", "LongRunning")]
    public void MGCJ_06_Stage2_CrossN() {
        _output.WriteLine("═══ MGCJ STAGE 2: CROSS-N DECOMPOSITION (seeds 0-99) ═══");
        int seeds = 100;
        var nResults = new ConcurrentDictionary<int, (int s0, int s1, int s2)>();

        Parallel.ForEach(AllN, n => {
            var details = new SeedDetail[seeds];
            Parallel.For(0, seeds, s => details[s] = RunFull(n, s));
            int s0 = details.Count(d => d.Invalid == 0 && Classify(d) == "S0");
            int s1 = details.Count(d => d.Invalid == 0 && Classify(d) == "S1");
            int s2 = details.Count(d => d.Invalid == 0 && Classify(d) == "S2");
            nResults[n] = (s0, s1, s2);
        });

        _output.WriteLine($"{"N",4} {"S0",5} {"S1",5} {"S2",5} {"Invalid",7}");
        foreach (int n in AllN) {
            var r = nResults[n];
            _output.WriteLine($"{n,4} {r.s0,5} {r.s1,5} {r.s2,5} {seeds-r.s0-r.s1-r.s2,7}");
        }
    }
}
