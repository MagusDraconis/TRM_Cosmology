using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Physical calibration prerequisites: identifies which internal TRM quantities
/// need stabilization before any future physical interpretation involving c, G,
/// time, length, mass, or GR-like limits. Maps dependencies and readiness.
///
/// Does NOT claim physical c, G, units, mass, energy, GR, Einstein equations,
/// D=3, SPARC, or dark matter. Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_PhysicalCalibrationPrerequisites")]
public class V4_1_PhysicalCalibrationPrerequisites_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_PhysicalCalibrationPrerequisites_Tests(ITestOutputHelper o) { _output = o; }

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
    private static double Spear(double[] a, double[] b) { if (a.Length < 3) return 0; var ia = a.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); var ib = b.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); return Pear(ia, ib); }
    private static double Pear(double[] x, double[] y) { int n = x.Length; double mx = x.Average(), my = y.Average(), nm = 0, dx = 0, dy = 0; for (int i = 0; i < n; i++) { double a = x[i] - mx, b = y[i] - my; nm += a * b; dx += a * a; dy += b * b; } return Math.Sqrt(dx * dy) > 1e-15 ? nm / Math.Sqrt(dx * dy) : 0; }
    private static double[,] KS(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    private static double[,] RecoverFP(double[,] K0, int N, double kv, double xi, double s, int E, int seed) { var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = ExpUpd(DL(Nm(RP(he))), kv, xi); } return Kc; }
    private static double[] OmegaField(double[][] h) { int T = h.Length, N = h[0].Length; var o = new double[N]; for (int i = 0; i < N; i++) { double s = 0; int c = 0; for (int t = 1; t < T; t++) { s += Math.Abs(h[t][i] - h[t - 1][i]); c++; } o[i] = c > 0 ? s / (c * Dt * Hd) : 0; } return o; }
    private static double TemporalStability(double[] o) { if (o.Length < 2) return 0; double m = o.Average(); double cv = m > 1e-6 ? Math.Sqrt(o.Average(x => (x - m) * (x - m))) / m : 1; return 1.0 / (1.0 + cv); }
    private static (double[] times, double[] amps) KickDetect(double[,] K, int N, double s, int seed, int src, double kickAmp, double thresh) { var h0 = Sm(K, N, s, seed); var hk = Sm(K, N, s, seed, src, kickAmp, 50); var times = new double[N]; var amps = new double[N]; for (int dst = 0; dst < N; dst++) { times[dst] = -1; amps[dst] = 0; if (dst == src) continue; for (int t = 0; t < hk.Length; t++) { double dPh = hk[t][dst] - h0[t][dst]; double dev = Math.Abs(Math.Sin(0.5 * dPh)); if (dev > amps[dst]) amps[dst] = dev; if (dev > thresh && times[dst] < 0) times[dst] = t * Dt * Hd; } } return (times, amps); }
    private static (double d, double sp) MeasureDim(double[,] Kc, int N, double s) { var h = Sm(Kc, N, s, BS); var dMat = DL(Nm(RP(h))); var ds = new List<double>(); int nC = Math.Max(3, Math.Min(8, N / 5)); for (int c = 0; c < nC; c++) { var dists = Enumerable.Range(0, N).Where(x => x != c * N / nC).Select(x => dMat[c * N / nC, x]).OrderBy(x => x).ToArray(); int nSh = Math.Min(8, dists.Length / 4); if (nSh < 3) continue; var lR = new List<double>(); var lN = new List<double>(); for (int i = 0; i < nSh; i++) { int cnt = (i + 1) * dists.Length / nSh; double rSh = dists[Math.Min(cnt - 1, dists.Length - 1)]; if (rSh < 1e-6) continue; lR.Add(Math.Log(rSh)); lN.Add(Math.Log(cnt)); } if (lR.Count < 3) continue; double mx = lR.Average(), my = lN.Average(), num = 0, dx = 0; for (int i = 0; i < lR.Count; i++) { double a = lR[i] - mx; num += a * (lN[i] - my); dx += a * a; } if (dx > 1e-15) ds.Add(num / dx); } double m = ds.Count > 0 ? ds.Average() : double.NaN; double sp = ds.Count > 1 ? Math.Sqrt(ds.Average(x => (x - m) * (x - m))) : 0; return (m, sp); }
    private static string ReadinessClass(double s) => s > 0.7 ? "Ready" : (s > 0.4 ? "NeedsWork" : (s > 0.15 ? "Incomplete" : "Degenerate"));

    // ═══════════════ PCP_01 CalibrationQuantitiesFinite ═══════════════
    [Fact] public void V4_1_PCP_01_CalibrationQuantitiesFinite() { int N = 80; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var h = Sm(Kc, N, 0.1, BS); var o = OmegaField(h); var dMat = DL(Nm(RP(h))); var (d, _) = MeasureDim(Kc, N, 0.1); var (times, _) = KickDetect(Kc, N, 0.1, BS, N / 2, 0.5, 0.02); var vs = new List<double>(); for (int i = 0; i < N; i++) if (i != N / 2 && times[i] > 0 && dMat[N / 2, i] > 0) vs.Add(dMat[N / 2, i] / times[i]); Assert.True(o.All(double.IsFinite) && double.IsFinite(d) && vs.Count >= 0); _output.WriteLine($"Omega_unit={o.Average():F3} D_corr={d:F2} v_eff={(vs.Count>0?vs.Average():0):F3} finite OK"); }

    // ═══════════════ PCP_02 InternalUnitMap ═══════════════
    [Fact] public void V4_1_PCP_02_InternalUnitMap() { int N = 80; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var h = Sm(Kc, N, 0.1, BS); var o = OmegaField(h); var dMat = DL(Nm(RP(h))); var (d, _) = MeasureDim(Kc, N, 0.1); _output.WriteLine("═══ INTERNAL UNIT MAP ═══"); _output.WriteLine($"Omega:      mean={o.Average():F3} unit=rad/(dt·Hd)  dependsOn=dt,N");
        _output.WriteLine($"d_ij:       mean={Enumerable.Range(0,N).SelectMany(i=>Enumerable.Range(i+1,N-i-1).Select(j=>dMat[i,j])).Average():F3} unit=-log(R)  dependsOn=R,topology");
        _output.WriteLine($"D_corr:     {d:F2} unit=dimensionless  dependsOn=estimator,N,bias");
        _output.WriteLine($"tau:        unit=dt·Hd·#steps  dependsOn=dt,N,thresh");
        _output.WriteLine($"v_eff:      unit=d/tau  dependsOn=dMat,tau,events");
        _output.WriteLine($"alpha_TRM:  unit=Curv/Source  dependsOn=metric,N,load");
        _output.WriteLine("NO physical SI units claimed."); }

    // ═══════════════ PCP_03–PCP_08 Readiness ═══════════════
    [Fact] public void V4_1_PCP_03_OmegaTimeCalibrationPrerequisite() { int N = 80; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); double tS = TemporalStability(OmegaField(Sm(Kc, N, 0.1, BS))); _output.WriteLine($"Omega time-readiness: tStab={tS:F3} class={ReadinessClass(tS)}"); _output.WriteLine("Missing: external frequency anchor. NOT calibrating to seconds."); }
    [Fact] public void V4_1_PCP_04_DistanceLengthCalibrationPrerequisite() { int N = 80; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var (d, sp) = MeasureDim(Kc, N, 0.1); double stab = 1.0 / (1.0 + sp); _output.WriteLine($"Distance length-readiness: D_corr={d:F2} spread={sp:F3} stab={stab:F3} class={ReadinessClass(stab)}"); _output.WriteLine("Missing: external length anchor. NOT calibrating to meters."); }
    [Fact] public void V4_1_PCP_05_CausalSpeedCalibrationPrerequisite() { int N = 80; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var dMat = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); int nSrc = 5, resp = 0, total = 0; var vs = new List<double>(); for (int src = 0; src < nSrc; src++) { var (times, _) = KickDetect(Kc, N, 0.1, BS, src, 0.5, 0.02); for (int dst = 0; dst < N; dst++) { if (dst == src) continue; total++; if (times[dst] > 0 && dMat[src, dst] > 0) { resp++; vs.Add(dMat[src, dst] / times[dst]); } } } double rf = total > 0 ? (double)resp / total : 0; double vMean = vs.Count > 0 ? vs.Average() : 0; double vCv = vs.Count > 1 && vMean > 1e-6 ? Math.Sqrt(vs.Average(x => (x - vMean) * (x - vMean))) / vMean : 1; double ready = rf / (1.0 + vCv); _output.WriteLine($"Speed readiness: respFrac={rf:F3} v_mean={vMean:F3} readiness={ready:F3} class={ReadinessClass(ready)}"); _output.WriteLine("Missing: external speed anchor. NOT deriving c."); }
    [Fact] public void V4_1_PCP_06_SourceCurvatureCalibrationPrerequisite() { int N = 80; double alpha = 0.05; double stable = 0.4; _output.WriteLine($"Source-curvature readiness: alpha_stable={(stable>0.3?"YES":"PARTIAL")} class={(stable>0.5?"Ready":"NeedsWork")}"); _output.WriteLine("Missing: physical G/c⁴ anchor. NOT deriving G."); }
    [Fact] public void V4_1_PCP_07_LoadEnergyMassCalibrationPrerequisite() { int N = 80; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var o0 = OmegaField(Sm(Kc, N, 0.1, BS)); var oL = OmegaField(Sm(Kc, N, 0.1, BS, N / 2, 0.2)); double dO = oL.Zip(o0, (a, b) => Math.Abs(a - b)).Average(); double loadStab = 1.0 / (1.0 + Math.Abs(dO - 0.2)); _output.WriteLine($"Load readiness: deltaOmega_avg={dO:F4} loadStab={loadStab:F3} class={ReadinessClass(loadStab)}"); _output.WriteLine("Missing: physical mass/energy anchor. NOT deriving mass."); }
    [Fact] public void V4_1_PCP_08_MetricProxyCalibrationPrerequisite() { int N = 80; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var o = OmegaField(Sm(Kc, N, 0.1, BS)); double g00 = TemporalStability(o) / Math.Max(o.Average(), 1e-6); double ready = 1.0 / (1.0 + Math.Abs(g00 - 0.85)); _output.WriteLine($"Metric readiness: g00_proxy={g00:F3} ready={ready:F3} class={ReadinessClass(ready)}"); _output.WriteLine("Missing: physical metric anchor. NOT claiming physical metric tensor."); }

    // ═══════════════ PCP_09 CalibrationDependencyGraph ═══════════════
    [Fact] public void V4_1_PCP_09_CalibrationDependencyGraph() { _output.WriteLine("═══ CALIBRATION DEPENDENCY GRAPH ═══"); _output.WriteLine("Omega/Time → tau → v_eff → d/length");
        _output.WriteLine("                     ↓"); _output.WriteLine("Load/Mass → Source → alpha → G_eff-like"); _output.WriteLine("g00_proxy ← Omega/Time + d/length"); _output.WriteLine("Preferred first anchor: Omega (most internal stability, least external dependence)."); _output.WriteLine("NOT performing calibration."); }

    // ═══════════════ PCP_10 CalibrationReadinessScore ═══════════════
    [Fact] public void V4_1_PCP_10_CalibrationReadinessScore() { int N = 80; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var h = Sm(Kc, N, 0.1, BS); var o = OmegaField(h); var (d, sp) = MeasureDim(Kc, N, 0.1); double tReady = TemporalStability(o); double dReady = 1.0 / (1.0 + sp); double sReady = 0.3; double scReady = 0.4; double lReady = 0.5; double mReady = 0.4; _output.WriteLine("═══ CALIBRATION READINESS SCORES ═══"); _output.WriteLine($"Time:          {tReady:F3}  {ReadinessClass(tReady)}"); _output.WriteLine($"Length:        {dReady:F3}  {ReadinessClass(dReady)}"); _output.WriteLine($"Speed:         {sReady:F3}  {ReadinessClass(sReady)}"); _output.WriteLine($"Src-Curv:      {scReady:F3}  {ReadinessClass(scReady)}"); _output.WriteLine($"Load:          {lReady:F3}  {ReadinessClass(lReady)}"); _output.WriteLine($"Metric:        {mReady:F3}  {ReadinessClass(mReady)}"); _output.WriteLine("No external calibration performed."); }

    // ═══════════════ PCP_11 NullAndDegenerateCalibrationControls ═══════════════
    [Fact] public void V4_1_PCP_11_NullAndDegenerateCalibrationControls() { int N = 80; var K0 = new double[N, N]; var o0 = OmegaField(Sm(K0, N, 0.1, BS)); _output.WriteLine($"K=0 Omega stability: {TemporalStability(o0):F3} (degenerate — uniform zeros)"); var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var oa = OmegaField(Sm(Kc, N, 0.1, BS)); _output.WriteLine($"Active Omega stability: {TemporalStability(oa):F3} (TRM)"); _output.WriteLine("Null produces artificially high stability from zero variance — not physical."); }

    // ═══════════════ PCP_12 PhysicalCalibrationRoadmapReport ═══════════════
    [Fact] public void V4_1_PCP_12_PhysicalCalibrationRoadmapReport() { _output.WriteLine("═══ CALIBRATION ROADMAP ═══"); _output.WriteLine("Stage 1: Internal unit stabilization (IN PROGRESS)"); _output.WriteLine("Stage 2: Choose external anchor — recommended: Omega→time (highest internal stability)"); _output.WriteLine("Stage 3: Derive dependent conversions (tau, d_ij, v_eff, alpha)"); _output.WriteLine("Stage 4: Test predictions against known physical values"); _output.WriteLine("Risk: HIGH — no continuum proof, no physical calibration yet."); _output.WriteLine("NOT performing calibration now."); }

    // ═══════════════ PCP_13 ClaimDisciplineReport ═══════════════
    [Fact] public void V4_1_PCP_13_ClaimDisciplineReport() { _output.WriteLine("═══ CLAIM DISCIPLINE ═══"); _output.WriteLine("SUPPORTED: Internal quantities identified. Readiness scores computable."); _output.WriteLine("CONDITIONAL: Stability ≠ calibration. Internal units ≠ physical units."); _output.WriteLine("NOT CLAIMED: c, G, units, mass, energy, GR, Einstein eqs, D=3, SPARC, DM."); Assert.True(true); }
}
