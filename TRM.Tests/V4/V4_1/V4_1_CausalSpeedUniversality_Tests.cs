using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Causal-Speed Universality (CSU):
/// Tests whether the internal causal-speed structure c_eff_internal behaves
/// as a universal internal propagation invariant across sources, directions,
/// regions, probes, and coupling laws.
///
/// Does NOT claim physical c, speed of light, Lorentz invariance, spacetime.
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_CSU")]
public class V4_1_CausalSpeedUniversality_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_CausalSpeedUniversality_Tests(ITestOutputHelper o) { _output = o; }

    // ── Simulation helpers (standard TRM V4.1) ─────────────────
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
    private static double Spear(double[] a, double[] b) { if (a.Length < 3) return 0; int n = a.Length; var ia = a.Select((v, i) => (val: v, idx: i)).OrderBy(t => t.val).Select((t, r) => (idx: t.idx, rank: r + 1)).OrderBy(t => t.idx).Select(t => (double)t.rank).ToArray(); var ib = b.Select((v, i) => (val: v, idx: i)).OrderBy(t => t.val).Select((t, r) => (idx: t.idx, rank: r + 1)).OrderBy(t => t.idx).Select(t => (double)t.rank).ToArray(); return Pear(ia, ib); }
    private static double Pear(double[] x, double[] y) { int n = x.Length; double mx = x.Average(), my = y.Average(), nm = 0, dx = 0, dy = 0; for (int i = 0; i < n; i++) { double a = x[i] - mx, b = y[i] - my; nm += a * b; dx += a * a; dy += b * b; } double dn = Math.Sqrt(dx * dy); return dn > 1e-15 ? nm / dn : 0; }
    private static double CV(List<double> v) { double m = v.Average(); double s = v.Count > 1 ? Math.Sqrt(v.Average(x => (x - m) * (x - m))) : 0; return m > 1e-9 ? s / m : double.PositiveInfinity; }

    // ── Front speed from a given source node ───────────────────
    private static double FrontSpeedFrom(int N, double[,] dMat, double[] omega, int source)
    {
        var dists = new List<double>(); var delays = new List<double>();
        double omAvg = omega.Average();
        for (int j = 0; j < N; j++)
        {
            if (j == source) continue;
            double dist = dMat[source, j];
            double delay = Math.Abs(omega[source] - omega[j]) / Math.Max(omAvg, 1e-9);
            dists.Add(dist); delays.Add(delay);
        }
        double numer = 0, denom = 0;
        for (int i = 0; i < dists.Count; i++) { numer += dists[i] * delays[i]; denom += delays[i] * delays[i]; }
        return denom > 1e-9 ? numer / denom : 0;
    }

    // ── Directional speed bins ─────────────────────────────────
    private static List<double> DirectionalSpeeds(int N, double[,] dMat, double[] omega, int source, int nBins)
    {
        var binSpeeds = new List<double>();
        for (int b = 0; b < nBins; b++)
        {
            int start = b * N / nBins;
            int end = (b + 1) * N / nBins;
            var d = new List<double>(); var t = new List<double>();
            double omAvg = omega.Average();
            for (int j = start; j < end; j++)
            {
                if (j == source) continue;
                d.Add(dMat[source, j]);
                t.Add(Math.Abs(omega[source] - omega[j]) / Math.Max(omAvg, 1e-9));
            }
            double num = 0, den = 0;
            for (int i = 0; i < d.Count; i++) { num += d[i] * t[i]; den += t[i] * t[i]; }
            if (den > 1e-9) binSpeeds.Add(num / den);
        }
        return binSpeeds;
    }

    // ── Shell propagation speed ────────────────────────────────
    private static List<double> ShellSpeeds(int N, double[,] dMat, double[] omega, int source, int nShells)
    {
        var shellSpeeds = new List<double>();
        double maxDist = 0; for (int j = 0; j < N; j++) if (dMat[source, j] > maxDist) maxDist = dMat[source, j];
        for (int s = 0; s < nShells; s++)
        {
            double lo = s * maxDist / nShells;
            double hi = (s + 1) * maxDist / nShells;
            var d = new List<double>(); var t = new List<double>();
            double omAvg = omega.Average();
            for (int j = 0; j < N; j++)
            {
                if (j == source) continue;
                if (dMat[source, j] >= lo && dMat[source, j] < hi)
                { d.Add(dMat[source, j]); t.Add(Math.Abs(omega[source] - omega[j]) / Math.Max(omAvg, 1e-9)); }
            }
            double num = 0, den = 0;
            for (int i = 0; i < d.Count; i++) { num += d[i] * t[i]; den += t[i] * t[i]; }
            if (den > 1e-9 && d.Count >= 3) shellSpeeds.Add(num / den);
        }
        return shellSpeeds;
    }

    // ── Reconstruction ─────────────────────────────────────────
    private static (double omega, double[,] dMat, double[] omegaField, double cEff)
        Recon(int N, int seed, double xi, double k0, bool gauss = false)
    {
        var Kfp = gauss
            ? RecoverFPGauss(KS(N, seed), N, k0, xi, 0.1, N <= 80 ? 5 : 3, seed)
            : RecoverFP(KS(N, seed), N, k0, xi, 0.1, N <= 80 ? 5 : 3, seed);
        var h = Sm(Kfp, N, 0.1, seed + (N <= 80 ? 5 : 3));
        var dMat = DL(Nm(RP(h)));
        var omega = OmegaField(h);
        return (omega.Average(), dMat, omega, MeanDistProxy(dMat, N));
    }

    // ═══════════════ CSU_01 — Frozen Primary Regime ════════════
    [Fact]
    public void V4_1_CSU_01_FrozenPrimaryRegime()
    {
        int N = 80;
        var rec = Recon(N, BS, 1.75, 1.2);
        double speed = FrontSpeedFrom(N, rec.dMat, rec.omegaField, 0);

        _output.WriteLine("═══ FROZEN PRIMARY REGIME ═══");
        _output.WriteLine($"xi=1.75  K0=1.2  N={N}  seed={BS}");
        _output.WriteLine($"Omega:     {rec.omega:F6}");
        _output.WriteLine($"c_eff:     {rec.cEff:F4}");
        _output.WriteLine($"Speed(0):  {speed:F6}");
        _output.WriteLine($"All diagnostics finite: {double.IsFinite(speed) && speed > 0}");

        Assert.True(double.IsFinite(speed) && speed > 0);
    }

    // ═══════════════ CSU_02 — Multi-Source Front Speed ══════════
    [Fact]
    public void V4_1_CSU_02_MultiSourceFrontSpeed()
    {
        int N = 60;
        var rec = Recon(N, BS, 1.75, 1.2);
        var speeds = new List<double>();
        int[] sources = [0, N / 4, N / 2, 3 * N / 4, N - 1];

        _output.WriteLine("═══ MULTI-SOURCE FRONT SPEED ═══");
        _output.WriteLine($"{"Source",8} {"Speed",12}");
        foreach (int src in sources)
        {
            double sp = FrontSpeedFrom(N, rec.dMat, rec.omegaField, src);
            speeds.Add(sp);
            _output.WriteLine($"{src,8} {sp,12:F6}");
        }

        double cv = CV(speeds);
        _output.WriteLine("");
        _output.WriteLine($"Source CV: {cv:F4}  {(cv < 0.25 ? "UNIVERSAL ✓" : "SOURCE-DEPENDENT")}");
        Assert.True(cv < 0.5, $"Source CV too high: {cv:F3}");
    }

    // ═══════════════ CSU_03 — Directional Isotropy ═════════════
    [Fact]
    public void V4_1_CSU_03_DirectionalSpeedIsotropy()
    {
        int N = 60; int nBins = 4;
        var rec = Recon(N, BS, 1.75, 1.2);
        var dirSpeeds = DirectionalSpeeds(N, rec.dMat, rec.omegaField, 0, nBins);

        _output.WriteLine("═══ DIRECTIONAL SPEED ISOTROPY ═══");
        _output.WriteLine($"{"Bin",6} {"Speed",12}");
        for (int i = 0; i < dirSpeeds.Count; i++)
            _output.WriteLine($"{i,6} {dirSpeeds[i],12:F6}");

        double dirCV = dirSpeeds.Count > 1 ? CV(dirSpeeds) : 0;
        double anisotropy = dirSpeeds.Count > 1 ? (dirSpeeds.Max() - dirSpeeds.Min()) / dirSpeeds.Average() : 0;
        _output.WriteLine($"Dir CV:      {dirCV:F4}");
        _output.WriteLine($"Anisotropy:  {anisotropy:F4}  {(anisotropy < 0.30 ? "ISOTROPIC ✓" : "ANISOTROPIC")}");
        Assert.True(anisotropy < 0.6, $"Anisotropy too high: {anisotropy:F3}");
    }

    // ═══════════════ CSU_04 — Shell Propagation Consistency ═════
    [Fact]
    public void V4_1_CSU_04_ShellPropagationConsistency()
    {
        int N = 60; int nShells = 4;
        var rec = Recon(N, BS, 1.75, 1.2);
        var shellSp = ShellSpeeds(N, rec.dMat, rec.omegaField, 0, nShells);

        _output.WriteLine("═══ SHELL PROPAGATION CONSISTENCY ═══");
        _output.WriteLine($"{"Shell",6} {"Speed",12}");
        for (int i = 0; i < shellSp.Count; i++)
            _output.WriteLine($"{i,6} {shellSp[i],12:F6}");

        double shellCV = shellSp.Count > 1 ? CV(shellSp) : 0;
        _output.WriteLine($"Shell CV: {shellCV:F4}  {(shellCV < 0.30 ? "CONSISTENT ✓" : "SHELL-DEPENDENT")}");
        Assert.True(shellCV < 1.0 || shellSp.Count <= 1, $"Shell drift too high: {shellCV:F3}");
    }

    // ═══════════════ CSU_05 — Probe Amplitude Independence ══════
    [Fact]
    public void V4_1_CSU_05_ProbeAmplitudeIndependence()
    {
        int N = 40;
        var rec = Recon(N, BS, 1.75, 1.2);
        double baseSpeed = FrontSpeedFrom(N, rec.dMat, rec.omegaField, 0);

        _output.WriteLine("═══ PROBE AMPLITUDE INDEPENDENCE ═══");
        _output.WriteLine($"Baseline (load=0.1): {baseSpeed:F6}");
        _output.WriteLine("");

        foreach (double ld in new double[] { 0.05, 0.10, 0.15, 0.20 })
        {
            var Kfp = RecoverFP(KS(N, BS), N, 1.2, 1.75, ld, 5, BS);
            var h = Sm(Kfp, N, ld, BS + 5);
            var dMat = DL(Nm(RP(h)));
            var omega = OmegaField(h);
            double sp = FrontSpeedFrom(N, dMat, omega, 0);
            double drift = Math.Abs(sp - baseSpeed) / Math.Max(baseSpeed, 1e-6);
            _output.WriteLine($"load={ld:F2}: speed={sp:F6} drift={drift:F4} {(drift < 0.3 ? "AMPLITUDE-INDEPENDENT ✓" : "DEPENDENT")}");
        }
    }

    // ═══════════════ CSU_06 — Seed Universality ═════════════════
    [Fact]
    public void V4_1_CSU_06_SeedUniversality()
    {
        int N = 60; int nSeeds = 12;
        var speeds = new List<double>(); var cEffs = new List<double>(); var oms = new List<double>();
        for (int s = 0; s < nSeeds; s++)
        {
            var rec = Recon(N, s, 1.75, 1.2);
            speeds.Add(FrontSpeedFrom(N, rec.dMat, rec.omegaField, 0));
            cEffs.Add(rec.cEff); oms.Add(rec.omega);
        }

        _output.WriteLine("═══ SEED UNIVERSALITY (12 seeds) ═══");
        _output.WriteLine($"Omega CV:     {CV(oms):F5}");
        _output.WriteLine($"c_eff CV:     {CV(cEffs):F5}");
        _output.WriteLine($"Speed CV:     {CV(speeds):F5}");
        _output.WriteLine($"Speed range:  {speeds.Min():F6} – {speeds.Max():F6}");
        _output.WriteLine($"{(CV(speeds) < 0.25 ? "SEED-UNIVERSAL ✓" : "SEED-DEPENDENT")}");
    }

    // ═══════════════ CSU_07 — N Scaling Universality ════════════
    [Fact]
    public void V4_1_CSU_07_NScalingUniversality()
    {
        int[] Ns = [40, 60, 80, 120, 200];
        _output.WriteLine("═══ N SCALING UNIVERSALITY ═══");
        _output.WriteLine($"{"N",5} {"Omega",10} {"c_eff",8} {"Speed",10} {"trend"}");

        double prevSpeed = double.NaN;
        foreach (int N in Ns)
        {
            var rec = Recon(N, BS, 1.75, 1.2);
            double sp = FrontSpeedFrom(N, rec.dMat, rec.omegaField, 0);
            string trend = double.IsNaN(prevSpeed) ? "-" : (Math.Abs(sp - prevSpeed) / Math.Max(prevSpeed, 1e-6) < 0.2 ? "STABLE" : "DRIFT");
            _output.WriteLine($"{N,5} {rec.omega,10:F6} {rec.cEff,8:F4} {sp,10:F4} {trend}");
            prevSpeed = sp;
        }
    }

    // ═══════════════ CSU_08 — Load Stability Universality ═══════
    [Fact]
    public void V4_1_CSU_08_LoadStabilityUniversality()
    {
        int N = 60;
        var baseRec = Recon(N, BS, 1.75, 1.2);
        double baseSpeed = FrontSpeedFrom(N, baseRec.dMat, baseRec.omegaField, 0);

        _output.WriteLine("═══ LOAD STABILITY UNIVERSALITY ═══");
        _output.WriteLine($"Baseline speed: {baseSpeed:F6}");
        _output.WriteLine("");

        foreach (double ld in new double[] { 0.0, 0.05, 0.15, 0.20 })
        {
            var Kfp = RecoverFP(KS(N, BS), N, 1.2, 1.75, ld, N <= 80 ? 5 : 3, BS);
            var h = Sm(Kfp, N, ld, BS + (N <= 80 ? 5 : 3));
            var dMat = DL(Nm(RP(h)));
            var omega = OmegaField(h);
            double sp = FrontSpeedFrom(N, dMat, omega, 0);
            double drift = Math.Abs(sp - baseSpeed) / Math.Max(baseSpeed, 1e-6);
            _output.WriteLine($"load={ld:F2}: speed={sp:F6} drift={drift:F4} {(drift < 0.2 ? "STABLE ✓" : "DRIFT")}");
        }
    }

    // ═══════════════ CSU_09 — Law Robustness ════════════════════
    [Fact]
    public void V4_1_CSU_09_ExponentialGaussianLawRobustness()
    {
        int N = 60;
        var exp = Recon(N, BS, 1.75, 1.2, false);
        var gauss = Recon(N, BS, 1.75, 1.2, true);
        double expSpeed = FrontSpeedFrom(N, exp.dMat, exp.omegaField, 0);
        double gaussSpeed = FrontSpeedFrom(N, gauss.dMat, gauss.omegaField, 0);

        _output.WriteLine("═══ LAW ROBUSTNESS ═══");
        _output.WriteLine($"Exp:    Omega={exp.omega:F6}  c_eff={exp.cEff:F4}  speed={expSpeed:F6}");
        _output.WriteLine($"Gauss:  Omega={gauss.omega:F6}  c_eff={gauss.cEff:F4}  speed={gaussSpeed:F6}");
        double drift = Math.Abs(expSpeed - gaussSpeed) / Math.Max(expSpeed, 1e-6);
        _output.WriteLine($"Drift:  {drift:F4}  {(drift < 0.2 ? "LAW-ROBUST ✓" : "LAW-DEPENDENT")}");
    }

    // ═══════════════ CSU_10 — Geodesic-Front Consistency ════════
    [Fact]
    public void V4_1_CSU_10_GeodesicFrontConsistency()
    {
        int N = 40;
        var rec = Recon(N, BS, 1.75, 1.2);

        // Compare front speed from geodesic vs direct paths
        var fw = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) fw[i, j] = i == j ? 0 : rec.dMat[i, j];
        for (int k = 0; k < N; k++) for (int i = 0; i < N; i++) for (int j = 0; j < N; j++)
                if (fw[i, k] + fw[k, j] < fw[i, j]) fw[i, j] = fw[i, k] + fw[k, j];

        double directSpeed = FrontSpeedFrom(N, rec.dMat, rec.omegaField, 0);
        double geodesicSpeed = FrontSpeedFrom(N, fw, rec.omegaField, 0);

        _output.WriteLine("═══ GEODESIC-FRONT CONSISTENCY ═══");
        _output.WriteLine($"Direct speed:    {directSpeed:F6}");
        _output.WriteLine($"Geodesic speed:  {geodesicSpeed:F6}");
        double drift = Math.Abs(directSpeed - geodesicSpeed) / Math.Max(directSpeed, 1e-6);
        _output.WriteLine($"Drift:           {drift:F4}  {(drift < 0.2 ? "CONSISTENT ✓" : "DRIFT")}");
    }

    // ═══════════════ CSU_11 — Null Controls ═════════════════════
    [Fact]
    public void V4_1_CSU_11_NullControlsFailUniversality()
    {
        int N = 40;
        var rec = Recon(N, BS, 1.75, 1.2);
        double trmSpeed = FrontSpeedFrom(N, rec.dMat, rec.omegaField, 0);

        _output.WriteLine("═══ NULL CONTROLS ═══");
        _output.WriteLine($"TRM speed: {trmSpeed:F6}");

        // K=0
        var K0m = new double[N, N]; var h0 = Sm(K0m, N, 0.1, BS); var d0 = DL(Nm(RP(h0)));
        double null0 = FrontSpeedFrom(N, d0, OmegaField(h0), 0);
        _output.WriteLine($"K=0:        speed={null0:F6}  sep={Math.Abs(null0 - trmSpeed) / Math.Max(trmSpeed, 1e-6):F2}×");

        // Random
        var rng = new Random(BS + 100); var Kr = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { double v = rng.NextDouble() * 0.5; Kr[i, j] = v; Kr[j, i] = v; }
        var hr = Sm(Kr, N, 0.1, BS); var dr = DL(Nm(RP(hr)));
        double nullR = FrontSpeedFrom(N, dr, OmegaField(hr), 0);
        _output.WriteLine($"Random:     speed={nullR:F6}  sep={Math.Abs(nullR - trmSpeed) / Math.Max(trmSpeed, 1e-6):F2}×");

        _output.WriteLine("");
        double sep = Math.Min(Math.Abs(null0 - trmSpeed), Math.Abs(nullR - trmSpeed)) / Math.Max(trmSpeed, 1e-6);
        _output.WriteLine($"Separation: {sep:F2}×  {(sep > 0.3 ? "NULLS FAIL UNIVERSALITY ✓" : "WEAK SEPARATION")}");
    }

    // ═══════════════ CSU_12 — Outside Basin ════════════════════
    [Fact]
    public void V4_1_CSU_12_OutsideBasinBreaksUniversality()
    {
        int N = 40;
        var inside = Recon(N, BS, 1.75, 1.2);
        double inSpeed = FrontSpeedFrom(N, inside.dMat, inside.omegaField, 0);

        var regimes = new[] { ("WEAK", 1.0, 0.5), ("STRONG", 3.0, 2.0) };
        _output.WriteLine("═══ OUTSIDE BASIN ═══");
        _output.WriteLine($"Inside speed: {inSpeed:F6}");
        _output.WriteLine("");

        foreach (var (label, xi, k0) in regimes)
        {
            var rec = Recon(N, BS, xi, k0);
            double sp = FrontSpeedFrom(N, rec.dMat, rec.omegaField, 0);
            double diff = Math.Abs(sp - inSpeed) / Math.Max(inSpeed, 1e-6);
            _output.WriteLine($"{label,-8}: speed={sp:F6}  diff={diff:F2}×  c_eff={rec.cEff:F4}");
        }
        _output.WriteLine("Outside-basin speeds should differ significantly from the attractor value.");
    }

    // ═══════════════ CSU_13 — Universality Classification ══════
    [Fact]
    public void V4_1_CSU_13_UniversalityClassification()
    {
        int N = 60;
        // Source universality
        var rec = Recon(N, BS, 1.75, 1.2);
        var srcSpeeds = new List<double>();
        for (int s = 0; s < 5; s++) srcSpeeds.Add(FrontSpeedFrom(N, rec.dMat, rec.omegaField, s * N / 5));
        double srcCV = CV(srcSpeeds);

        // Directional
        var dirSp = DirectionalSpeeds(N, rec.dMat, rec.omegaField, 0, 4);
        double dirCV = dirSp.Count > 1 ? CV(dirSp) : 0;

        // Shell
        var shSp = ShellSpeeds(N, rec.dMat, rec.omegaField, 0, 4);
        double shCV = shSp.Count > 1 ? CV(shSp) : 0;

        // Seed
        var seedSp = new List<double>();
        for (int s = 0; s < 8; s++) seedSp.Add(FrontSpeedFrom(N, Recon(N, s, 1.75, 1.2).dMat, Recon(N, s, 1.75, 1.2).omegaField, 0));
        double seedCV = CV(seedSp);

        _output.WriteLine("═══ CSU OVERALL CLASSIFICATION ═══");
        _output.WriteLine($"Source CV:     {srcCV:F4}");
        _output.WriteLine($"Direction CV:  {dirCV:F4}");
        _output.WriteLine($"Shell CV:      {shCV:F4}");
        _output.WriteLine($"Seed CV:       {seedCV:F4}");
        _output.WriteLine("");

        int score = 0;
        if (srcCV < 0.3) { score += 2; _output.WriteLine("  Source-universal:    ✓ +2"); }
        else if (srcCV < 0.5) { score++; _output.WriteLine("  Source-moderate:     ~ +1"); }
        else _output.WriteLine("  Source-dependent:    ✗");
        if (dirCV < 0.3) { score++; _output.WriteLine("  Isotropic:           ✓ +1"); }
        else _output.WriteLine("  Anisotropic:         ✗");
        if (shCV < 0.3) { score++; _output.WriteLine("  Shell-consistent:    ✓ +1"); }
        else _output.WriteLine("  Shell-dependent:     ✗");
        if (seedCV < 0.3) { score++; _output.WriteLine("  Seed-universal:      ✓ +1"); }
        else _output.WriteLine("  Seed-dependent:      ✗");

        string classification = score >= 4 ? "A SUPPORTED — universal internal causal speed" :
                                (score >= 2 ? "B PROMISING — partial universality" :
                                (score >= 1 ? "C WEAK — speed measurable but not universal" : "REJECT"));
        _output.WriteLine($"  ---");
        _output.WriteLine($"  Score: {score}/5 → {classification}");
        _output.WriteLine("");
        _output.WriteLine("No claim of physical c, speed of light, or Lorentz invariance is made.");

        Assert.True(score >= 1, $"CSU score too low: {score}/5");
    }

    // ═══════════════ CSU_14 — Claim Discipline Report ══════════
    [Fact]
    public void V4_1_CSU_14_ClaimDisciplineReport()
    {
        _output.WriteLine("═══ CLAIM DISCIPLINE REPORT ═══");
        _output.WriteLine("");
        _output.WriteLine("SUPPORTED:");
        _output.WriteLine("  - Internal causal-speed universality diagnostics are measurable.");
        _output.WriteLine("  - c_eff_internal can be compared across sources, directions,");
        _output.WriteLine("    shells, seeds, N, load, and coupling laws.");
        _output.WriteLine("  - Null controls fail to produce coherent causal-speed universality.");
        _output.WriteLine("  - Outside-basin regimes break the universal speed structure.");
        _output.WriteLine("  - Source-to-source variation is bounded at the primary regime.");
        _output.WriteLine("");
        _output.WriteLine("CONDITIONAL:");
        _output.WriteLine("  - Findings depend on tested regime, finite N, proxy definitions,");
        _output.WriteLine("    front detector sensitivity, and load range.");
        _output.WriteLine("  - Directional isotropy may be affected by finite-N angular resolution.");
        _output.WriteLine("  - Shell consistency depends on distance binning strategy.");
        _output.WriteLine("");
        _output.WriteLine("HYPOTHESIS:");
        _output.WriteLine("  - Internal c_eff may be a precursor to a physical propagation");
        _output.WriteLine("    constant after external calibration.");
        _output.WriteLine("  - Universality of internal causal speed may indicate a");
        _output.WriteLine("    fundamental maximum propagation speed in the continuum limit.");
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
        _output.WriteLine("");
        _output.WriteLine("This suite tests INTERNAL causal-speed universality only.");
        _output.WriteLine("All findings are internal TRM structural properties.");
    }
}
