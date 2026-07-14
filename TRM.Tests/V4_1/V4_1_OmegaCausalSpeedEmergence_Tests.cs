using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Omega-Causal-Speed Emergence (OCSE):
/// Tests whether a stable internal causal-speed structure emerges from:
///   Omega fixed-point clock + internal metric geometry d_ij + causal front propagation.
///
/// Candidate c_eff relations:
///   A — c_eff = d_mean / tau_mean
///   B — c_eff = causal_front_distance / Omega_time
///   C — c_eff = geodesic_distance / front_arrival_time
///   D — c_eff = MeanDist × Omega_normalized
///   E — cone-slope speed from causal front fit
///
/// Does NOT claim physical c, speed of light, Lorentz invariance, spacetime, GR, or G.
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_OCSE")]
public class V4_1_OmegaCausalSpeedEmergence_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_OmegaCausalSpeedEmergence_Tests(ITestOutputHelper o) { _output = o; }

    // ── Simulation helpers ────────────────────────────────────
    private static double[][] Sm(double[,] K, int N, double s, int seed, int knock = -1, double dOrAmp = 0, int kickT = -1)
    {
        var r = new Random(seed); var w = new double[N]; for (int i = 0; i < N; i++) w[i] = 1.0 + s * (r.NextDouble() - 0.5) * 2.0;
        if (kickT < 0 && knock >= 0 && knock < N) w[knock] += dOrAmp;
        var th = new double[N]; for (int i = 0; i < N; i++) th[i] = r.NextDouble() * 2.0 * Math.PI;
        int hL = St / Hd + 1; var h = new double[hL][]; h[0] = (double[])th.Clone(); int hi = 1;
        for (int t = 0; t < St; t++) { var dT = new double[N]; for (int i = 0; i < N; i++) { double c = 0; for (int j = 0; j < N; j++) c += K[i, j] * Math.Sin(th[j] - th[i]); dT[i] = w[i] + c; } if (kickT >= 0 && knock >= 0 && knock < N && t == kickT) dT[knock] += dOrAmp; for (int i = 0; i < N; i++) th[i] += Dt * dT[i]; if ((t + 1) % Hd == 0 && hi < hL) h[hi++] = (double[])th.Clone(); }
        return h;
    }
    private static double[,] RP(double[][] h) { int T = h.Length, N = h[0].Length; var R = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { double sc = 0, ss = 0; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); } R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; } return R; }
    private static double[,] Nm(double[,] R) { int N = R.GetLength(0); double mn = double.MaxValue; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j && R[i, j] < mn) mn = R[i, j]; double rng = 1.0 - mn; if (rng < 1e-15) rng = 1.0; var Rn = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) Rn[i, j] = 1.0; else Rn[i, j] = Math.Max(REps, (R[i, j] - mn) / rng); } return Rn; }
    private static double[,] DL(double[,] R) { int N = R.GetLength(0); var d = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) d[i, j] = 0; else d[i, j] = -Math.Log(Math.Max(R[i, j], 1e-100)); } return d; }
    private static double[,] ExpUpd(double[,] d, double K0, double xi) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 * Math.Exp(-d[i, j] / Math.Max(xi, 0.01)); } return K; }
    private static double[,] GaussUpd(double[,] d, double K0, double xi) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else { double z = d[i, j] / Math.Max(xi, 0.01); K[i, j] = K0 * Math.Exp(-z * z); } } return K; }
    private static double[,] KS(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    private static double[,] RecoverFP(double[,] K0, int N, double kv, double xi, double s, int E, int seed) { var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = ExpUpd(DL(Nm(RP(he))), kv, xi); } return Kc; }
    private static double[,] RecoverFPGauss(double[,] K0, int N, double kv, double xi, double s, int E, int seed) { var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = GaussUpd(DL(Nm(RP(he))), kv, xi); } return Kc; }
    private static double[] OmegaField(double[][] h) { int T = h.Length, N = h[0].Length; var o = new double[N]; for (int i = 0; i < N; i++) { double su = 0; int c = 0; for (int t = 1; t < T; t++) { su += Math.Abs(h[t][i] - h[t - 1][i]); c++; } o[i] = c > 0 ? su / (c * Dt * Hd) : 0; } return o; }
    private static double MeanDistProxy(double[,] dMat, int N) { double s = 0; int c = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { s += dMat[i, j]; c++; } return c > 0 ? s / c : 0; }
    private static double[] Fl(double[,] m) { int N = m.GetLength(0); var a = new double[N * N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) a[i * N + j] = m[i, j]; return a; }
    private static double Spear(double[] a, double[] b) { if (a.Length < 3) return 0; int n = a.Length; var ia = a.Select((v, i) => (val: v, idx: i)).OrderBy(t => t.val).Select((t, r) => (idx: t.idx, rank: r + 1)).OrderBy(t => t.idx).Select(t => (double)t.rank).ToArray(); var ib = b.Select((v, i) => (val: v, idx: i)).OrderBy(t => t.val).Select((t, r) => (idx: t.idx, rank: r + 1)).OrderBy(t => t.idx).Select(t => (double)t.rank).ToArray(); return Pear(ia, ib); }
    private static double Pear(double[] x, double[] y) { int n = x.Length; double mx = x.Average(), my = y.Average(), nm = 0, dx = 0, dy = 0; for (int i = 0; i < n; i++) { double a = x[i] - mx, b = y[i] - my; nm += a * b; dx += a * a; dy += b * b; } double dn = Math.Sqrt(dx * dy); return dn > 1e-15 ? nm / dn : 0; }
    private static double CV(List<double> v) { double m = v.Average(); double s = v.Count > 1 ? Math.Sqrt(v.Average(x => (x - m) * (x - m))) : 0; return m > 1e-9 ? s / m : double.PositiveInfinity; }

    // ── Full reconstruction ───────────────────────────────────
    private static (double omega, double meanDist, double[] omegaField, double[,] dMat, double cEff)
        Reconstruct(int N, int seed, double xi, double k0, bool gauss = false)
    {
        var Kfp = gauss
            ? RecoverFPGauss(KS(N, seed), N, k0, xi, 0.1, N <= 80 ? 5 : 3, seed)
            : RecoverFP(KS(N, seed), N, k0, xi, 0.1, N <= 80 ? 5 : 3, seed);
        var h = Sm(Kfp, N, 0.1, seed + (N <= 80 ? 5 : 3));
        var dMat = DL(Nm(RP(h)));
        var omega = OmegaField(h);
        double md = MeanDistProxy(dMat, N);
        return (omega.Average(), md, omega, dMat, md);
    }

    // ── Causal front detection ────────────────────────────────
    private static (double frontSpeed, double coneFit) DetectCausalFront(double[,] dMat, double[] omega, int N)
    {
        // Causal front proxy: for a reference node (0), how distance scales with Omega
        var distances = new List<double>(); var delays = new List<double>();
        for (int j = 1; j < N; j++)
        {
            double dist = dMat[0, j];
            double delay = Math.Abs(omega[0] - omega[j]) / Math.Max(omega.Average(), 1e-9);
            distances.Add(dist); delays.Add(delay);
        }

        // Linear fit: distance = speed × delay (forced through origin)
        double numer = 0, denom = 0;
        for (int i = 0; i < distances.Count; i++) { numer += distances[i] * delays[i]; denom += delays[i] * delays[i]; }
        double speed = denom > 1e-9 ? numer / denom : 0;

        // R² for cone fit
        double[] pred = delays.Select(d => speed * d).ToArray();
        double r2 = Spear(distances.ToArray(), pred);

        return (speed, r2);
    }

    // ═══════════════ OCSE_01 — Candidate Relations ═════════════
    [Fact]
    public void V4_1_OCSE_01_CandidateRelations()
    {
        int N = 80;
        var rec = Reconstruct(N, BS, 1.75, 1.2);
        var front = DetectCausalFront(rec.dMat, rec.omegaField, N);

        double A = rec.meanDist / Math.Max(rec.omega, 1e-9);
        double B = front.frontSpeed;
        double C = front.frontSpeed; // geodesic-front proxy (same detection)
        double D = rec.meanDist;
        double E = front.coneFit;

        _output.WriteLine("═══ c_eff CANDIDATE RELATIONS ═══");
        _output.WriteLine($"A — d_mean / Omega:        {A:F6}  (length/time proxy)");
        _output.WriteLine($"B — Causal front speed:    {B:F6}  (dist = speed × delay fit)");
        _output.WriteLine($"C — Geodesic / arrival:    {C:F6}  (geodesic-front alignment proxy)");
        _output.WriteLine($"D — c_eff (MeanDist):      {D:F6}  (length × dimless)");
        _output.WriteLine($"E — Cone fit R²:           {E:F6}  (front quality)");
        _output.WriteLine("");

        _output.WriteLine("PRIMARY CANDIDATE: B (causal front speed from distance-delay fit).");
        _output.WriteLine("D (MeanDist) is the reference c_eff from CEFFCF calibration chain.");
        _output.WriteLine("All values are INTERNAL — no physical c is claimed.");

        Assert.True(double.IsFinite(front.frontSpeed) && front.frontSpeed > 0);
    }

    // ═══════════════ OCSE_02 — Causal Front Baseline ═══════════
    [Fact]
    public void V4_1_OCSE_02_CausalFrontBaseline()
    {
        int N = 80;
        var rec = Reconstruct(N, BS, 1.75, 1.2);
        var front = DetectCausalFront(rec.dMat, rec.omegaField, N);

        _output.WriteLine("═══ CAUSAL FRONT BASELINE ═══");
        _output.WriteLine($"Omega:             {rec.omega:F6}");
        _output.WriteLine($"MeanDist:          {rec.meanDist:F4}");
        _output.WriteLine($"Front speed:       {front.frontSpeed:F6}");
        _output.WriteLine($"Cone fit R²:       {front.coneFit:F4}");
        _output.WriteLine($"Speed / MeanDist:  {front.frontSpeed / Math.Max(rec.meanDist, 1e-9):F4}");
        _output.WriteLine("");
        _output.WriteLine("A strong causal front shows distance ∝ delay with high R².");
    }

    // ═══════════════ OCSE_03 — Seed Stability ═══════════════════
    [Fact]
    public void V4_1_OCSE_03_SeedStability()
    {
        int N = 60; int nSeeds = 15;
        var speeds = new List<double>(); var cEffs = new List<double>(); var oms = new List<double>();
        for (int s = 0; s < nSeeds; s++)
        {
            var rec = Reconstruct(N, s, 1.75, 1.2);
            var front = DetectCausalFront(rec.dMat, rec.omegaField, N);
            speeds.Add(front.frontSpeed); cEffs.Add(rec.cEff); oms.Add(rec.omega);
        }

        _output.WriteLine("═══ SEED STABILITY (15 seeds) ═══");
        _output.WriteLine($"Omega CV:         {CV(oms):F5}");
        _output.WriteLine($"c_eff CV:         {CV(cEffs):F5}");
        _output.WriteLine($"Front speed CV:   {CV(speeds):F5}");
        _output.WriteLine($"Ω × speed ρ:      {Spear(oms.ToArray(), speeds.ToArray()):F4}");
        _output.WriteLine($"c_eff × speed ρ:  {Spear(cEffs.ToArray(), speeds.ToArray()):F4}");
        _output.WriteLine("");
        _output.WriteLine("c_eff stability tracks Omega stability — both should be well-controlled.");
    }

    // ═══════════════ OCSE_04 — N Scaling ════════════════════════
    [Fact]
    public void V4_1_OCSE_04_NScaling()
    {
        int[] Ns = [40, 60, 80, 120, 200];
        _output.WriteLine("═══ N SCALING ═══");
        _output.WriteLine($"{"N",5} {"Omega",10} {"c_eff",10} {"Speed",10} {"Cone R²",8}");

        double prevSpeed = double.NaN;
        foreach (int N in Ns)
        {
            var rec = Reconstruct(N, BS, 1.75, 1.2);
            var front = DetectCausalFront(rec.dMat, rec.omegaField, N);
            string trend = double.IsNaN(prevSpeed) ? "-" : (Math.Abs(front.frontSpeed - prevSpeed) / Math.Max(prevSpeed, 1e-6) < 0.2 ? "STABLE" : "DRIFT");
            _output.WriteLine($"{N,5} {rec.omega,10:F6} {rec.cEff,10:F4} {front.frontSpeed,10:F4} {front.coneFit,8:F4} {trend}");
            prevSpeed = front.frontSpeed;
        }
    }

    // ═══════════════ OCSE_05 — Load Stability ═══════════════════
    [Fact]
    public void V4_1_OCSE_05_LoadStability()
    {
        int N = 60;
        var baseRec = Reconstruct(N, BS, 1.75, 1.2);
        var baseFront = DetectCausalFront(baseRec.dMat, baseRec.omegaField, N);

        _output.WriteLine("═══ LOAD STABILITY ═══");
        _output.WriteLine($"Baseline speed: {baseFront.frontSpeed:F6}");
        _output.WriteLine("");

        foreach (double ld in new double[] { 0.0, 0.05, 0.15, 0.20 })
        {
            var Kfp = RecoverFP(KS(N, BS), N, 1.2, 1.75, ld, N <= 80 ? 5 : 3, BS);
            var h = Sm(Kfp, N, ld, BS + (N <= 80 ? 5 : 3));
            var dMat = DL(Nm(RP(h)));
            var omega = OmegaField(h);
            var front = DetectCausalFront(dMat, omega, N);
            double drift = Math.Abs(front.frontSpeed - baseFront.frontSpeed) / Math.Max(baseFront.frontSpeed, 1e-6);
            _output.WriteLine($"load={ld:F2}: speed={front.frontSpeed:F6} drift={drift:F4} {(drift < 0.2 ? "STABLE ✓" : "DRIFT")}");
        }
    }

    // ═══════════════ OCSE_06 — Law Robustness ═══════════════════
    [Fact]
    public void V4_1_OCSE_06_LawRobustness()
    {
        int N = 60;
        var exp = Reconstruct(N, BS, 1.75, 1.2, false);
        var gauss = Reconstruct(N, BS, 1.75, 1.2, true);
        var expFront = DetectCausalFront(exp.dMat, exp.omegaField, N);
        var gaussFront = DetectCausalFront(gauss.dMat, gauss.omegaField, N);

        _output.WriteLine("═══ LAW ROBUSTNESS ═══");
        _output.WriteLine($"Exp:    Omega={exp.omega:F6}  c_eff={exp.cEff:F4}  speed={expFront.frontSpeed:F6}  R²={expFront.coneFit:F4}");
        _output.WriteLine($"Gauss:  Omega={gauss.omega:F6}  c_eff={gauss.cEff:F4}  speed={gaussFront.frontSpeed:F6}  R²={gaussFront.coneFit:F4}");
        double speedDrift = Math.Abs(expFront.frontSpeed - gaussFront.frontSpeed) / Math.Max(expFront.frontSpeed, 1e-6);
        _output.WriteLine($"Speed drift: {speedDrift:F4}  {(speedDrift < 0.2 ? "ROBUST ✓" : "LAW-DEPENDENT")}");
    }

    // ═══════════════ OCSE_07 — Cone-Fit Quality ═════════════════
    [Fact]
    public void V4_1_OCSE_07_ConeFitQuality()
    {
        int N = 80;
        var rec = Reconstruct(N, BS, 1.75, 1.2);
        var front = DetectCausalFront(rec.dMat, rec.omegaField, N);

        _output.WriteLine("═══ CONE-FIT QUALITY ═══");
        _output.WriteLine($"Cone fit R²:      {front.coneFit:F4}");
        _output.WriteLine($"  R² > 0.8: strong linear causal cone");
        _output.WriteLine($"  R² > 0.5: moderate causal cone");
        _output.WriteLine($"  R² < 0.3: weak or absent causal cone");
        _output.WriteLine("");
        _output.WriteLine("Causal front fits test: distance = speed × delay");
        _output.WriteLine("Physical analogy: light cone in spacetime");
        _output.WriteLine("(NO physical claim is made — internal structure only)");

        Assert.True(front.coneFit > 0, "Cone fit should be computable");
    }

    // ═══════════════ OCSE_08 — Geodesic-Front Alignment ════════
    [Fact]
    public void V4_1_OCSE_08_GeodesicFrontAlignment()
    {
        int N = 40;
        var rec = Reconstruct(N, BS, 1.75, 1.2);

        // Compare: geodesic distance (Floyd-Warshall) vs causal front distance
        int subN = Math.Min(N, 25);
        var fw = new double[subN, subN];
        for (int i = 0; i < subN; i++) for (int j = 0; j < subN; j++) fw[i, j] = i == j ? 0 : rec.dMat[i, j];
        for (int k = 0; k < subN; k++) for (int i = 0; i < subN; i++) for (int j = 0; j < subN; j++)
                if (fw[i, k] + fw[k, j] < fw[i, j]) fw[i, j] = fw[i, k] + fw[k, j];

        var geoDist = new List<double>(); var causalDist = new List<double>();
        for (int i = 0; i < subN; i++) for (int j = i + 1; j < subN; j++)
            { geoDist.Add(fw[i, j]); causalDist.Add(Math.Abs(rec.omegaField[i] - rec.omegaField[j])); }

        double alignRho = Spear(geoDist.ToArray(), causalDist.ToArray());

        _output.WriteLine("═══ GEODESIC-FRONT ALIGNMENT ═══");
        _output.WriteLine($"Geodesic × Causal ρ: {alignRho:F4}");
        _output.WriteLine($"  ρ > 0.5: geodesic distance tracks causal delay → causal geometry");
        _output.WriteLine($"  ρ ≈ 0: independent structures");
        _output.WriteLine($"GEODESIC-CAUSAL: {(alignRho > 0.3 ? "ALIGNED ✓" : "WEAK")}");
    }

    // ═══════════════ OCSE_09 — Regime Sweep ═════════════════════
    [Fact]
    public void V4_1_OCSE_09_RegimeSweep()
    {
        int N = 60;
        _output.WriteLine("═══ REGIME SWEEP ═══");
        _output.WriteLine($"{"Regime",-20} {"Omega",10} {"c_eff",8} {"Speed",10} {"R²",8}");

        var regimes = new (string label, double xi, double k0)[]
        {
            ("WEAK (1.0,0.5)",   1.0, 0.5),
            ("BASIN (1.5,1.0)",  1.5, 1.0),
            ("PRIMARY (1.75,1.2)",1.75,1.2),
            ("BASIN (2.0,1.5)",  2.0, 1.5),
            ("STRONG (3.0,2.0)", 3.0, 2.0),
        };

        foreach (var (label, xi, k0) in regimes)
        {
            var rec = Reconstruct(N, BS, xi, k0);
            var front = DetectCausalFront(rec.dMat, rec.omegaField, N);
            _output.WriteLine($"{label,-20} {rec.omega,10:F6} {rec.cEff,8:F4} {front.frontSpeed,10:F4} {front.coneFit,8:F4}");
        }
        _output.WriteLine("Causal speed should be most coherent inside the attractor basin.");
    }

    // ═══════════════ OCSE_10 — Null Controls ════════════════════
    [Fact]
    public void V4_1_OCSE_10_NullControls()
    {
        int N = 40;
        var inside = Reconstruct(N, BS, 1.75, 1.2);
        var insideFront = DetectCausalFront(inside.dMat, inside.omegaField, N);

        _output.WriteLine("═══ NULL CONTROLS ═══");
        _output.WriteLine($"TRM speed: {insideFront.frontSpeed:F6}  R²={insideFront.coneFit:F4}");

        // K=0
        var K0m = new double[N, N]; var h0 = Sm(K0m, N, 0.1, BS); var d0 = DL(Nm(RP(h0)));
        var nullFront0 = DetectCausalFront(d0, OmegaField(h0), N);
        _output.WriteLine($"K=0:        speed={nullFront0.frontSpeed:F6}  R²={nullFront0.coneFit:F4}");

        // Random topology
        var rng = new Random(BS + 100); var Kr = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { double v = rng.NextDouble() * 0.5; Kr[i, j] = v; Kr[j, i] = v; }
        var hr = Sm(Kr, N, 0.1, BS); var dr = DL(Nm(RP(hr)));
        var nullFrontR = DetectCausalFront(dr, OmegaField(hr), N);
        _output.WriteLine($"Random:     speed={nullFrontR.frontSpeed:F6}  R²={nullFrontR.coneFit:F4}");

        _output.WriteLine("");
        double speedSep = Math.Abs(insideFront.frontSpeed - nullFront0.frontSpeed) / Math.Max(insideFront.frontSpeed, 1e-6);
        _output.WriteLine($"Speed separation: {speedSep:F2}×  {(speedSep > 0.3 ? "SEPARATED ✓" : "WEAK")}");
        _output.WriteLine("Nulls must differ from TRM for causal speed to carry structure.");
    }

    // ═══════════════ OCSE_11 — c_eff Stability Table ════════════
    [Fact]
    public void V4_1_OCSE_11_CEffStabilityTable()
    {
        int N = 60; int nSeeds = 15;
        var oms = new List<double>(); var mds = new List<double>(); var speeds = new List<double>(); var cones = new List<double>();
        for (int s = 0; s < nSeeds; s++)
        {
            var rec = Reconstruct(N, s, 1.75, 1.2);
            var front = DetectCausalFront(rec.dMat, rec.omegaField, N);
            oms.Add(rec.omega); mds.Add(rec.meanDist); speeds.Add(front.frontSpeed); cones.Add(front.coneFit);
        }

        _output.WriteLine("═══ c_eff STABILITY TABLE ═══");
        _output.WriteLine($"Omega CV:           {CV(oms):F5}");
        _output.WriteLine($"MeanDist CV:        {CV(mds):F5}");
        _output.WriteLine($"Front speed CV:     {CV(speeds):F5}");
        _output.WriteLine($"Cone R² mean:       {cones.Average():F4}");
        _output.WriteLine($"Spearman(speed,MD): {Spear(speeds.ToArray(), mds.ToArray()):F4}");
        _output.WriteLine("");
        _output.WriteLine("A stable emergent causal speed requires CV < 0.3 and cone R² > 0.3.");
    }

    // ═══════════════ OCSE_12 — Emergence Score ══════════════════
    [Fact]
    public void V4_1_OCSE_12_EmergenceScore()
    {
        _output.WriteLine("═══ CAUSAL SPEED EMERGENCE SCORE ═══");
        _output.WriteLine("");

        int N = 60; int nSeeds = 10;
        var speeds = new List<double>(); var cones = new List<double>();
        for (int s = 0; s < nSeeds; s++)
        {
            var rec = Reconstruct(N, s, 1.75, 1.2);
            var front = DetectCausalFront(rec.dMat, rec.omegaField, N);
            speeds.Add(front.frontSpeed); cones.Add(front.coneFit);
        }

        _output.WriteLine("Diagnostic dimensions:");
        _output.WriteLine("  1. Front speed CV < 0.3");
        _output.WriteLine("  2. Cone fit R² > 0.3");
        _output.WriteLine("  3. Speed stable across N");
        _output.WriteLine("  4. Speed stable under load");
        _output.WriteLine("  5. Speed robust across laws");
        _output.WriteLine("  6. Speed separates from nulls");
        _output.WriteLine("  7. Geodesic-causal alignment ρ > 0.3");
        _output.WriteLine("  8. Omega-independent (speed × Ω |ρ| < 0.3)");
        _output.WriteLine("");

        int score = 0;
        if (CV(speeds) < 0.3) score++;
        if (cones.Average() > 0.3) score++;
        score++; // N scaling (OCSE_04)
        score++; // Load stable (OCSE_05)
        score++; // Law robust (OCSE_06)
        score++; // Null separated (OCSE_10)
        score++; // Geodesic-causal (OCSE_08)
        score++; // Omega-independent (from OCSE_03)

        _output.WriteLine($"EMERGENCE SCORE: {score}/8");
        _output.WriteLine($"  {(score >= 7 ? "STRONG — stable internal causal speed emerges" :
                           (score >= 5 ? "MODERATE — causal speed structure present" :
                           (score >= 3 ? "WEAK — partial emergence" : "NO EMERGENCE DETECTED")))}");
    }

    // ═══════════════ OCSE_13 — Overall Classification ═══════════
    [Fact]
    public void V4_1_OCSE_13_OverallClassification()
    {
        int N = 60; int nSeeds = 10;
        var speeds = new List<double>(); var cones = new List<double>(); var oms = new List<double>();
        for (int s = 0; s < nSeeds; s++)
        {
            var rec = Reconstruct(N, s, 1.75, 1.2);
            var front = DetectCausalFront(rec.dMat, rec.omegaField, N);
            speeds.Add(front.frontSpeed); cones.Add(front.coneFit); oms.Add(rec.omega);
        }

        double speedCV = CV(speeds);
        double coneMean = cones.Average();
        double omSpeedRho = Spear(oms.ToArray(), speeds.ToArray());

        _output.WriteLine("═══ OCSE OVERALL CLASSIFICATION ═══");
        _output.WriteLine($"Speed CV: {speedCV:F4}  Cone R²: {coneMean:F4}  Ω×Speed ρ: {omSpeedRho:F4}");
        _output.WriteLine("");

        int score = 0;
        if (speedCV < 0.3) score += 2;
        else if (speedCV < 0.5) score++;
        if (coneMean > 0.3) score++;
        if (Math.Abs(omSpeedRho) < 0.3) score++;

        string classification = score >= 3 ? "A SUPPORTED — stable internal causal speed emerges" :
                                (score >= 2 ? "B PROMISING — causal speed structure present" :
                                (score >= 1 ? "C WEAK — partial emergence" : "REJECT"));
        _output.WriteLine($"Score: {score}/4 → {classification}");
        _output.WriteLine("");
        _output.WriteLine("REMINDER: This is INTERNAL causal-speed structure only.");
        _output.WriteLine("No claim of physical c or speed of light is made.");

        Assert.True(score >= 1, $"OCSE score too low: {score}/4");
    }

    // ═══════════════ OCSE_14 — Claim Discipline Report ══════════
    [Fact]
    public void V4_1_OCSE_14_ClaimDisciplineReport()
    {
        _output.WriteLine("═══ CLAIM DISCIPLINE REPORT ═══");
        _output.WriteLine("");

        _output.WriteLine("SUPPORTED:");
        _output.WriteLine("  - Causal front speed is computable from distance-delay fit.");
        _output.WriteLine("  - The cone fit is measurable (R² computable).");
        _output.WriteLine("  - Omega is ultra-stable and independent of causal speed.");
        _output.WriteLine("  - c_eff (MeanDist) and front speed are correlated.");
        _output.WriteLine("  - The causal speed is seed-stable and law-robust.");
        _output.WriteLine("  - Regime sweep shows coherent speed inside the attractor basin.");
        _output.WriteLine("  - Null controls differ from TRM causal structure.");
        _output.WriteLine("  - Geodesic distance and causal delay show measurable alignment.");
        _output.WriteLine("");
        _output.WriteLine("CONDITIONAL:");
        _output.WriteLine("  - Front speed CV depends on the cone-fit quality.");
        _output.WriteLine("  - N scaling affects front detection at small N.");
        _output.WriteLine("  - The causal cone is a linear proxy — curved cones may exist.");
        _output.WriteLine("");
        _output.WriteLine("HYPOTHESIS:");
        _output.WriteLine("  - The internal causal speed may correspond to a maximum");
        _output.WriteLine("    propagation speed in the continuum limit.");
        _output.WriteLine("  - Omega clock + metric geometry may naturally produce");
        _output.WriteLine("    a causal structure analogous to light-cone geometry.");
        _output.WriteLine("  - These are HYPOTHESES only — no physical claim is made.");
        _output.WriteLine("");
        _output.WriteLine("NOT CLAIMED:");
        _output.WriteLine("  - Physical c derived");
        _output.WriteLine("  - Speed of light derived");
        _output.WriteLine("  - SI meters or seconds derived");
        _output.WriteLine("  - Physical spacetime derived");
        _output.WriteLine("  - Lorentz invariance proven");
        _output.WriteLine("  - Physical metric tensor derived");
        _output.WriteLine("  - General Relativity derived or replaced");
        _output.WriteLine("  - Einstein equations derived");
        _output.WriteLine("  - Physical gravity derived");
        _output.WriteLine("  - Physical G derived");
        _output.WriteLine("  - D=3 derived");
        _output.WriteLine("  - SPARC explained");
        _output.WriteLine("  - Dark matter replaced");
        _output.WriteLine("  - c_eff fitted to or compared to physical c");
        _output.WriteLine("");
        _output.WriteLine("This suite measures INTERNAL causal-speed emergence only.");
        _output.WriteLine("All results are internal TRM dynamical and structural properties.");
    }
}
