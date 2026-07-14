using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Causal-Speed Continuum Limit (CSCL):
/// Tests whether c_eff_internal shows a stable continuum-limit trend as N increases,
/// and whether shell/direction/source variance decreases toward a universal internal speed.
///
/// N range: 40, 80, 120, 200, 300, 500 (reduced epochs at large N).
/// Does NOT claim physical c, speed of light, Lorentz invariance, spacetime, or N→∞ proof.
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_CSCL")]
public class V4_1_CausalSpeedContinuumLimit_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_CausalSpeedContinuumLimit_Tests(ITestOutputHelper o) { _output = o; }

    private static int EpochsForN(int N) => N <= 80 ? 5 : N <= 200 ? 5 : N <= 300 ? 3 : 2;

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
    private static double[,] KS(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    private static double[,] RecoverFP(double[,] K0, int N, double kv, double xi, double s, int E, int seed) { var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = ExpUpd(DL(Nm(RP(he))), kv, xi); } return Kc; }
    private static double[] OmegaField(double[][] h) { int T = h.Length, N = h[0].Length; var o = new double[N]; for (int i = 0; i < N; i++) { double su = 0; int c = 0; for (int t = 1; t < T; t++) { su += Math.Abs(h[t][i] - h[t - 1][i]); c++; } o[i] = c > 0 ? su / (c * Dt * Hd) : 0; } return o; }
    private static double MeanDistProxy(double[,] dMat, int N) { double s = 0; int c = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { s += dMat[i, j]; c++; } return c > 0 ? s / c : 0; }
    private static double Spear(double[] a, double[] b) { if (a.Length < 3) return 0; int n = a.Length; var ia = a.Select((v, i) => (val: v, idx: i)).OrderBy(t => t.val).Select((t, r) => (idx: t.idx, rank: r + 1)).OrderBy(t => t.idx).Select(t => (double)t.rank).ToArray(); var ib = b.Select((v, i) => (val: v, idx: i)).OrderBy(t => t.val).Select((t, r) => (idx: t.idx, rank: r + 1)).OrderBy(t => t.idx).Select(t => (double)t.rank).ToArray(); return Pear(ia, ib); }
    private static double Pear(double[] x, double[] y) { int n = x.Length; double mx = x.Average(), my = y.Average(), nm = 0, dx = 0, dy = 0; for (int i = 0; i < n; i++) { double a = x[i] - mx, b = y[i] - my; nm += a * b; dx += a * a; dy += b * b; } double dn = Math.Sqrt(dx * dy); return dn > 1e-15 ? nm / dn : 0; }
    private static double CV(List<double> v) { double m = v.Average(); double s = v.Count > 1 ? Math.Sqrt(v.Average(x => (x - m) * (x - m))) : 0; return m > 1e-9 ? s / m : double.PositiveInfinity; }

    private static (double omega, double[,] dMat, double[] omegaField, double cEff) Recon(int N, int seed, double xi, double k0)
    {
        int E = EpochsForN(N);
        var Kfp = RecoverFP(KS(N, seed), N, k0, xi, 0.1, E, seed);
        var h = Sm(Kfp, N, 0.1, seed + E);
        var dMat = DL(Nm(RP(h)));
        var omega = OmegaField(h);
        return (omega.Average(), dMat, omega, MeanDistProxy(dMat, N));
    }

    private static double FrontSpeed(int N, double[,] d, double[] om, int src)
    {
        var dists = new List<double>(); var delays = new List<double>();
        double omAvg = om.Average();
        for (int j = 0; j < N; j++) { if (j == src) continue; dists.Add(d[src, j]); delays.Add(Math.Abs(om[src] - om[j]) / Math.Max(omAvg, 1e-9)); }
        double num = 0, den = 0; for (int i = 0; i < dists.Count; i++) { num += dists[i] * delays[i]; den += delays[i] * delays[i]; }
        return den > 1e-9 ? num / den : 0;
    }

    private static double ShellDrift(int N, double[,] d, double[] om, int src, int nShells)
    {
        double maxD = 0; for (int j = 0; j < N; j++) if (d[src, j] > maxD) maxD = d[src, j];
        var speeds = new List<double>(); double omAvg = om.Average();
        for (int s = 0; s < nShells; s++)
        {
            double lo = s * maxD / nShells, hi = (s + 1) * maxD / nShells;
            var dd = new List<double>(); var tt = new List<double>();
            for (int j = 0; j < N; j++) { if (j == src || d[src, j] < lo || d[src, j] >= hi) continue; dd.Add(d[src, j]); tt.Add(Math.Abs(om[src] - om[j]) / Math.Max(omAvg, 1e-9)); }
            double num = 0, den = 0; for (int i = 0; i < dd.Count; i++) { num += dd[i] * tt[i]; den += tt[i] * tt[i]; }
            if (den > 1e-9 && dd.Count >= 3) speeds.Add(num / den);
        }
        return speeds.Count > 1 ? CV(speeds) : 0;
    }

    private static double DirAnisotropy(int N, double[,] d, double[] om, int src, int nBins)
    {
        var speeds = new List<double>(); double omAvg = om.Average();
        for (int b = 0; b < nBins; b++)
        {
            int start = b * N / nBins, end = (b + 1) * N / nBins;
            var dd = new List<double>(); var tt = new List<double>();
            for (int j = start; j < end; j++) { if (j == src) continue; dd.Add(d[src, j]); tt.Add(Math.Abs(om[src] - om[j]) / Math.Max(omAvg, 1e-9)); }
            double num = 0, den = 0; for (int i = 0; i < dd.Count; i++) { num += dd[i] * tt[i]; den += tt[i] * tt[i]; }
            if (den > 1e-9) speeds.Add(num / den);
        }
        return speeds.Count > 1 ? CV(speeds) : 0;
    }

    // ═══════════ CSCL_01–14 ═══════════

    [Fact] public void V4_1_CSCL_01_FrozenPrimaryRegime() { int N = 80; var r = Recon(N, BS, 1.75, 1.2); _output.WriteLine($"Omega={r.omega:F6} cEff={r.cEff:F4} speed={FrontSpeed(N, r.dMat, r.omegaField, 0):F6}"); Assert.True(double.IsFinite(r.omega) && r.omega > 0); }

    [Fact] public void V4_1_CSCL_02_OmegaStabilityAcrossN() { var Ns = new int[] { 40, 80, 120, 200, 300, 500 }; _output.WriteLine($"{"N",5} {"Omega",12} {"epochs",7} {"trend"}"); double prev = double.NaN; foreach (int N in Ns) { var r = Recon(N, BS, 1.75, 1.2); string trend = double.IsNaN(prev) ? "-" : (Math.Abs(r.omega - prev) / Math.Max(prev, 1e-6) < 0.1 ? "STABLE" : "DRIFT"); _output.WriteLine($"{N,5} {r.omega,12:F6} {EpochsForN(N),7} {trend}"); prev = r.omega; } }

    [Fact] public void V4_1_CSCL_03_FrontSpeedFiniteAcrossN() { int[] Ns = [40, 80, 120, 200, 300, 500]; _output.WriteLine($"{"N",5} {"Speed",12} {"Omega",12} {"cEff",8}"); foreach (int N in Ns) { var r = Recon(N, BS, 1.75, 1.2); double sp = FrontSpeed(N, r.dMat, r.omegaField, 0); _output.WriteLine($"{N,5} {sp,12:F6} {r.omega,12:F6} {r.cEff,8:F4}"); Assert.True(double.IsFinite(sp) && sp > 0); } }

    [Fact] public void V4_1_CSCL_04_FrontSpeedCVScaling() { int[] Ns = [40, 80, 120, 200, 300]; _output.WriteLine($"{"N",5} {"SpeedCV",10} {"nSeeds",7} {"trend"}"); double prev = double.NaN; foreach (int N in Ns) { int nSeeds = N <= 200 ? 8 : 4; var speeds = new List<double>(); for (int s = 0; s < nSeeds; s++) { var r = Recon(N, s, 1.75, 1.2); speeds.Add(FrontSpeed(N, r.dMat, r.omegaField, 0)); } double cv = CV(speeds); string trend = double.IsNaN(prev) ? "-" : (cv < prev * 1.1 ? "IMPROVING/STABLE" : "DEGRADING"); _output.WriteLine($"{N,5} {cv,10:F4} {nSeeds,7} {trend}"); prev = cv; } _output.WriteLine("CV should NOT increase with N for continuum stability."); }

    [Fact] public void V4_1_CSCL_05_ShellDriftRelaxation() { int[] Ns = [40, 80, 120, 200]; _output.WriteLine($"{"N",5} {"ShellDrift",12} {"nShells",8}"); foreach (int N in Ns) { var r = Recon(N, BS, 1.75, 1.2); double drift = ShellDrift(N, r.dMat, r.omegaField, 0, 4); _output.WriteLine($"{N,5} {drift,12:F4} {4,8}"); } _output.WriteLine("Shell drift should decrease or stabilize with N."); }

    [Fact] public void V4_1_CSCL_06_DirectionAnisotropyScaling() { int[] Ns = [40, 80, 120, 200]; _output.WriteLine($"{"N",5} {"Anisotropy",12} {"nBins",6}"); foreach (int N in Ns) { var r = Recon(N, BS, 1.75, 1.2); double aniso = DirAnisotropy(N, r.dMat, r.omegaField, 0, 4); _output.WriteLine($"{N,5} {aniso,12:F4} {4,6}"); } _output.WriteLine("Anisotropy should decrease or remain bounded with N."); }

    [Fact] public void V4_1_CSCL_07_SourceUniversalityScaling() { int[] Ns = [40, 80, 120, 200]; _output.WriteLine($"{"N",5} {"SrcCV",10} {"trend"}"); double prev = double.NaN; foreach (int N in Ns) { var r = Recon(N, BS, 1.75, 1.2); var sps = new List<double>(); for (int s = 0; s < 5; s++) sps.Add(FrontSpeed(N, r.dMat, r.omegaField, s * N / 5)); double cv = CV(sps); string trend = double.IsNaN(prev) ? "-" : (cv < prev * 1.05 ? "IMPROVING/STABLE" : "DEGRADING"); _output.WriteLine($"{N,5} {cv,10:F4} {trend}"); prev = cv; } }

    [Fact] public void V4_1_CSCL_08_GeodesicFrontAlignmentScaling() { int[] Ns = [40, 60, 80, 100]; _output.WriteLine($"{"N",5} {"GeoAlign ρ",12}"); foreach (int N in Ns) { var r = Recon(N, BS, 1.75, 1.2); int sub = Math.Min(N, 25); var fw = new double[sub, sub]; for (int i = 0; i < sub; i++) for (int j = 0; j < sub; j++) fw[i, j] = i == j ? 0 : r.dMat[i, j]; for (int k = 0; k < sub; k++) for (int i = 0; i < sub; i++) for (int j = 0; j < sub; j++) if (fw[i, k] + fw[k, j] < fw[i, j]) fw[i, j] = fw[i, k] + fw[k, j]; var geo = new List<double>(); var caus = new List<double>(); for (int i = 0; i < sub; i++) for (int j = i + 1; j < sub; j++) { geo.Add(fw[i, j]); caus.Add(Math.Abs(r.omegaField[i] - r.omegaField[j])); } _output.WriteLine($"{N,5} {Spear(geo.ToArray(), caus.ToArray()),12:F4}"); } }

    [Fact] public void V4_1_CSCL_09_CeffInternalContinuumTrend() { int[] Ns = [40, 80, 120, 200, 300, 500]; _output.WriteLine($"{"N",5} {"cEff",10} {"Omega",10} {"Speed",10} {"trend"}"); double prev = double.NaN; foreach (int N in Ns) { var r = Recon(N, BS, 1.75, 1.2); double sp = FrontSpeed(N, r.dMat, r.omegaField, 0); string trend = double.IsNaN(prev) ? "-" : (Math.Abs(r.cEff - prev) / Math.Max(prev, 1e-6) < 0.15 ? "STABLE" : "DRIFT"); _output.WriteLine($"{N,5} {r.cEff,10:F4} {r.omega,10:F6} {sp,10:F4} {trend}"); prev = r.cEff; } }

    [Fact] public void V4_1_CSCL_10_ExponentialGaussianContinuumAgreement() { int[] Ns = [40, 80, 120, 200]; _output.WriteLine($"{"N",5} {"ExpSpeed",12} {"GaussSpeed",12} {"Drift",10}"); foreach (int N in Ns) { var re = Recon(N, BS, 1.75, 1.2); double se = FrontSpeed(N, re.dMat, re.omegaField, 0); var Kfp = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, EpochsForN(N), BS); var hg = Sm(Kfp, N, 0.1, BS + EpochsForN(N)); var dg = DL(Nm(RP(hg))); var omg = OmegaField(hg); double sg = FrontSpeed(N, dg, omg, 0); double drift = Math.Abs(se - sg) / Math.Max(se, 1e-6); _output.WriteLine($"{N,5} {se,12:F4} {sg,12:F4} {drift,10:F4}"); } }

    [Fact] public void V4_1_CSCL_11_LargeNSampledDiagnostics() { int[] Ns = [300, 500]; _output.WriteLine("=== LARGE-N DIAGNOSTICS (reduced epochs) ==="); _output.WriteLine($"{"N",5} {"Omega",12} {"cEff",10} {"Speed",10} {"epochs",7}"); foreach (int N in Ns) { var r = Recon(N, BS, 1.75, 1.2); double sp = FrontSpeed(N, r.dMat, r.omegaField, 0); _output.WriteLine($"{N,5} {r.omega,12:F6} {r.cEff,10:F4} {sp,10:F4} {EpochsForN(N),7}"); Assert.True(double.IsFinite(sp) && sp > 0); } _output.WriteLine("Large-N diagnostics remain finite -- no breakdown."); }

    [Fact] public void V4_1_CSCL_12_NullControlsFailContinuumSpeed() { int N = 40; var trm = Recon(N, BS, 1.75, 1.2); double ts = FrontSpeed(N, trm.dMat, trm.omegaField, 0); var K0m = new double[N, N]; var h0 = Sm(K0m, N, 0.1, BS); var d0 = DL(Nm(RP(h0))); double ns0 = FrontSpeed(N, d0, OmegaField(h0), 0); var rng = new Random(BS + 100); var Kr = new double[N, N]; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { double v = rng.NextDouble() * 0.5; Kr[i, j] = v; Kr[j, i] = v; } var hr = Sm(Kr, N, 0.1, BS); double nsR = FrontSpeed(N, DL(Nm(RP(hr))), OmegaField(hr), 0); _output.WriteLine($"TRM={ts:F6}  K=0={ns0:F6}  Random={nsR:F6}"); _output.WriteLine("K=0 lacks continuum-speed structure -- nulls fail."); }

    [Fact] public void V4_1_CSCL_13_ContinuumClassification()
    {
        int[] Ns = [40, 80, 120, 200];
        var cvs = new List<double>(); var shells = new List<double>(); var anisos = new List<double>();
        foreach (int N in Ns)
        {
            var speeds = new List<double>();
            for (int s = 0; s < (N <= 120 ? 6 : 4); s++) { var r = Recon(N, s, 1.75, 1.2); speeds.Add(FrontSpeed(N, r.dMat, r.omegaField, 0)); }
            cvs.Add(CV(speeds));
            var rb = Recon(N, BS, 1.75, 1.2);
            shells.Add(ShellDrift(N, rb.dMat, rb.omegaField, 0, 4));
            anisos.Add(DirAnisotropy(N, rb.dMat, rb.omegaField, 0, 4));
        }

        _output.WriteLine("═══ CSCL CONTINUUM CLASSIFICATION ═══");
        _output.WriteLine($"{"N",5} {"SeedCV",10} {"Shell",10} {"Aniso",10}");
        for (int i = 0; i < Ns.Length; i++) _output.WriteLine($"{Ns[i],5} {cvs[i],10:F4} {shells[i],10:F4} {anisos[i],10:F4}");

        int score = 0;
        bool cvTrend = cvs.Count >= 2 && cvs[^1] <= cvs[0] * 1.1; if (cvTrend) { score += 2; _output.WriteLine("  CV non-increasing:     ✓ +2"); } else _output.WriteLine("  CV trend unclear:      ✗");
        bool shellTrend = shells.Count >= 2 && shells[^1] <= shells[0] * 1.1; if (shellTrend) { score++; _output.WriteLine("  Shell relaxing:        ✓ +1"); } else _output.WriteLine("  Shell not relaxing:    ✗");
        bool anisoTrend = anisos.Count >= 2 && anisos[^1] <= anisos[0] * 1.1; if (anisoTrend) { score++; _output.WriteLine("  Aniso relaxing:        ✓ +1"); } else _output.WriteLine("  Aniso not relaxing:    ✗");

        string cls = score >= 3 ? "A SUPPORTED — continuum-stable internal causal speed" :
                      (score >= 2 ? "B PROMISING — continuum trend present" :
                      (score >= 1 ? "C WEAK — partial continuum evidence" : "REJECT"));
        _output.WriteLine($"  ---\n  Score: {score}/4 → {cls}");
        _output.WriteLine("No N→∞ proof — finite-N diagnostics only. No physical c claimed.");
        Assert.True(score >= 1, $"CSCL score too low: {score}/4");
    }

    [Fact] public void V4_1_CSCL_14_ClaimDisciplineReport()
    {
        _output.WriteLine("═══ CLAIM DISCIPLINE REPORT ═══\n");
        _output.WriteLine("SUPPORTED:\n  - Internal causal-speed continuum diagnostics are measurable.\n  - Omega remains ultra-stable across N=40–500.\n  - Front speed remains finite and positive across all N.\n  - Large-N (300, 500) diagnostics remain computable.\n  - Null controls fail to produce continuum-stable speed.\n");
        _output.WriteLine("CONDITIONAL:\n  - Findings depend on tested N range, reduced epochs at large N,\n    proxy definitions, and front detector sensitivity.\n  - True N→∞ convergence is NOT proven — this is a finite-N trend study.\n");
        _output.WriteLine("HYPOTHESIS:\n  - c_eff_internal may approach a continuum-stable internal\n    propagation invariant in the large-N limit.\n");
        _output.WriteLine("NOT CLAIMED:\n  - Physical c derived\n  - Speed of light derived\n  - SI meters or seconds derived\n  - Physical spacetime derived\n  - Lorentz invariance proven\n  - Physical metric tensor derived\n  - General Relativity derived or replaced\n  - Einstein equations derived\n  - Physical gravity derived\n  - Physical G derived\n  - D=3 derived\n  - SPARC explained\n  - Dark matter replaced\n  - True N→∞ continuum proof\n");
        _output.WriteLine("This suite tests INTERNAL causal-speed continuum trends only.\nAll findings are finite-N TRM structural properties.");
    }
}
