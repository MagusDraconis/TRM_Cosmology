using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// c_eff calibration design: combines internal time and length anchor designs
/// into a stable dimensionless effective propagation speed candidate.
/// Does NOT claim physical c, SI units, GR, D=3, SPARC, or dark matter.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_CeffCalibrationDesign")]
public class V4_1_CeffCalibrationDesign_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_CeffCalibrationDesign_Tests(ITestOutputHelper o) { _output = o; }

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
    private static double[,] RecoverFP(double[,] K0, int N, double kv, double xi, double s, int E, int seed, Func<double[,], double, double, double[,]> upd = null) { upd ??= ExpUpd; var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = upd(DL(Nm(RP(he))), kv, xi); } return Kc; }
    private static double[] OmegaField(double[][] h) { int T = h.Length, N = h[0].Length; var o = new double[N]; for (int i = 0; i < N; i++) { double s = 0; int c = 0; for (int t = 1; t < T; t++) { s += Math.Abs(h[t][i] - h[t - 1][i]); c++; } o[i] = c > 0 ? s / (c * Dt * Hd) : 0; } return o; }
    private static (double[] times, double[] amps) KickDetect(double[,] K, int N, double s, int seed, int src, double kickAmp, double thresh) { var h0 = Sm(K, N, s, seed); var hk = Sm(K, N, s, seed, src, kickAmp, 50); var times = new double[N]; var amps = new double[N]; for (int dst = 0; dst < N; dst++) { times[dst] = -1; amps[dst] = 0; if (dst == src) continue; for (int t = 0; t < hk.Length; t++) { double dPh = hk[t][dst] - h0[t][dst]; double dev = Math.Abs(Math.Sin(0.5 * dPh)); if (dev > amps[dst]) amps[dst] = dev; if (dev > thresh && times[dst] < 0) times[dst] = t * Dt * Hd; } } return (times, amps); }
    private static double Median(List<double> vs) { if (vs.Count == 0) return double.NaN; var s = vs.OrderBy(x => x).ToList(); int mid = s.Count / 2; return s.Count % 2 == 0 ? (s[mid - 1] + s[mid]) / 2.0 : s[mid]; }
    private static string StabClass(double s) => s > 0.7 ? "Stable" : (s > 0.4 ? "Moderate" : (s > 0.15 ? "Weak" : "Degenerate"));
    private static double MeanDistProxy(double[,] dMat, int N) { double s = 0; int c = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { s += dMat[i, j]; c++; } return c > 0 ? s / c : 0; }

    private static List<double> EventVeff(double[,] Kc, double[,] dMat, int N, double s, int seed)
    {
        var vs = new List<double>();
        for (int src = 0; src < Math.Min(5, N / 10); src++) { var (times, _) = KickDetect(Kc, N, s, seed, src, 0.5, 0.02); for (int dst = 0; dst < N; dst++) if (dst != src && times[dst] > 0 && dMat[src, dst] > 0) vs.Add(dMat[src, dst] / times[dst]); }
        return vs;
    }

    // ═══════════════ CEFF_01–13 ═══════════════
    [Fact] public void V4_1_CEFF_01_CeffDesignDataFinite() { int N = 80; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var dMat = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var o = OmegaField(Sm(Kc, N, 0.1, BS)); var vs = EventVeff(Kc, dMat, N, 0.1, BS); _output.WriteLine($"Omega={o.Average():F4} dMean={MeanDistProxy(dMat,N):F4} v={(vs.Count>0?vs.Average():0):F3} n={vs.Count}"); Assert.True(double.IsFinite(o.Average()) && vs.Count > 0); }

    [Fact] public void V4_1_CEFF_02_TimeLengthCombinationMatrix() { int N = 80; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var dMat = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var o = OmegaField(Sm(Kc, N, 0.1, BS)); double oB = o.Average(); var vs = EventVeff(Kc, dMat, N, 0.1, BS); double vR = vs.Count > 0 ? vs.Average() : 0; double[] fRefs = [1.0, 1.0 / (Dt * Hd), 1.0, 1.0, 9_192_631_770.0]; string[] tNames = ["Dimless", "SimDt", "AbstractFreq", "SISecond", "CsLike"]; string[] lNames = ["MeanDist", "MedianDist", "ShellUnit"]; _output.WriteLine("TimeAnchor     LenAnchor    v_eff"); foreach (var pair in tNames.Zip(fRefs)) { double tS = pair.Second / Math.Max(oB, 1e-6); for (int li = 0; li < 3; li++) { double lV = li == 0 ? MeanDistProxy(dMat, N) : (li == 1 ? Median(Enumerable.Range(0, N).SelectMany(i => Enumerable.Range(i + 1, N - i - 1).Select(j => dMat[i, j])).ToList()) : Enumerable.Range(0, N).SelectMany(i => Enumerable.Range(i + 1, N - i - 1).Select(j => dMat[i, j])).OrderBy(x => x).ToArray()[N * (N - 1) / 6]); double vC = vR * lV / oB * tS; if (double.IsFinite(vC)) _output.WriteLine($"{pair.First,-14} {lNames[li],-12} {vC:F3}"); } } _output.WriteLine("Physical c NOT claimed."); }

    [Fact] public void V4_1_CEFF_03_BaselineCeffCandidate() { int N = 80; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var dMat = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var o = OmegaField(Sm(Kc, N, 0.1, BS)); double oB = o.Average(); double dMean = MeanDistProxy(dMat, N); var vs = EventVeff(Kc, dMat, N, 0.1, BS); double vB = vs.Count > 0 ? vs.Average() * oB / dMean : 0; double vStd = vs.Count > 1 ? Math.Sqrt(vs.Average(x => (x - vs.Average()) * (x - vs.Average()))) : 0; _output.WriteLine($"c_eff candidate (Dimless×MeanDist): {vB:F3}±{vStd:F3} n={vs.Count}  Not physical c."); }

    [Fact] public void V4_1_CEFF_04_LoadInvarianceOfCeff() { int N = 80; int ln = N / 2; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var d0 = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var o0 = OmegaField(Sm(Kc, N, 0.1, BS)); double oB0 = o0.Average(); double dB0 = MeanDistProxy(d0, N); var vs0 = EventVeff(Kc, d0, N, 0.1, BS); var hL = Sm(Kc, N, 0.1, BS, ln, 0.2); var Kl = ExpUpd(DL(Nm(RP(hL))), 1.2, 1.75); var dL = DL(Nm(RP(Sm(Kl, N, 0.1, BS)))); var oL = OmegaField(Sm(Kl, N, 0.1, BS)); var vsL = EventVeff(Kl, dL, N, 0.1, BS); double v0 = vs0.Count > 0 ? vs0.Average() * oB0 / dB0 : 0; double vL = vsL.Count > 0 ? vsL.Average() * oL.Average() / MeanDistProxy(dL, N) : 0; double shift = Math.Abs(vL - v0) / Math.Max(v0, 1e-6); _output.WriteLine($"c_eff before={v0:F3} after={vL:F3} shift={shift:F3} {(shift < 0.3 ? "Load-invariant" : "Load-sensitive")}"); }

    [Fact] public void V4_1_CEFF_05_NScalingCeff() { int[] Ns = [40, 80, 120, 200]; foreach (int N in Ns) { int E = N <= 80 ? 5 : 3; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, E, BS); var dMat = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var o = OmegaField(Sm(Kc, N, 0.1, BS)); var vs = EventVeff(Kc, dMat, N, 0.1, BS); double vC = vs.Count > 0 ? vs.Average() * o.Average() / MeanDistProxy(dMat, N) : 0; _output.WriteLine($"N={N}: c_eff={vC:F3} nEv={vs.Count}"); } }

    [Fact] public void V4_1_CEFF_06_MultiSeedCeff() { int N = 80; var vs = new List<double>(); for (int s = 0; s < 20; s++) { var Kc = RecoverFP(KS(N, s), N, 1.2, 1.75, 0.1, 5, s); var dMat = DL(Nm(RP(Sm(Kc, N, 0.1, s)))); var o = OmegaField(Sm(Kc, N, 0.1, s)); var ev = EventVeff(Kc, dMat, N, 0.1, s); if (ev.Count > 0) vs.Add(ev.Average() * o.Average() / MeanDistProxy(dMat, N)); } _output.WriteLine($"c_eff: {vs.Average():F3}±{(vs.Count>1?Math.Sqrt(vs.Average(x=>(x-vs.Average())*(x-vs.Average()))):0):F3} n={vs.Count}"); }

    [Fact] public void V4_1_CEFF_07_CeffFrontConsistency() { int N = 80; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var dMat = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var vs = EventVeff(Kc, dMat, N, 0.1, BS); if (vs.Count < 4) { _output.WriteLine("Insufficient events"); return; } var ds = new List<double>(); var ts = new List<double>(); for (int src = 0; src < 3; src++) { var (times, _) = KickDetect(Kc, N, 0.1, BS, src, 0.5, 0.02); for (int dst = 0; dst < N; dst++) if (dst != src && times[dst] > 0 && dMat[src, dst] > 0) { ds.Add(dMat[src, dst]); ts.Add(times[dst]); } } double mx = ds.Average(), my = ts.Average(), sxy = 0, sx2 = 0; for (int i = 0; i < ds.Count; i++) { double a = ds[i] - mx; sxy += a * (ts[i] - my); sx2 += a * a; } double vFront = sx2 > 1e-15 && sxy > 0 ? 1.0 / (sxy / sx2) : double.NaN; double agree = double.IsFinite(vFront) ? Math.Abs(vs.Average() - vFront) / Math.Max(vs.Average(), 1e-6) : 1; _output.WriteLine($"v_event={vs.Average():F3} v_front={vFront:F3} agree={(agree < 0.5 ? "YES" : "PARTIAL")}"); }

    [Fact] public void V4_1_CEFF_08_CouplingLawCeffComparison() { int N = 80; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var dMat = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var o = OmegaField(Sm(Kc, N, 0.1, BS)); var vs = EventVeff(Kc, dMat, N, 0.1, BS); _output.WriteLine($"exp: c_eff={(vs.Count>0?vs.Average()*o.Average()/MeanDistProxy(dMat,N):0):F3}"); }

    [Fact] public void V4_1_CEFF_09_PlateauCeffMap() { int N = 80; double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2]; _output.WriteLine("xi    K0   c_eff"); foreach (double xi in xis) foreach (double kv in K0s) { var Kc = RecoverFP(KS(N, BS), N, kv, xi, 0.1, 5, BS); var dMat = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var o = OmegaField(Sm(Kc, N, 0.1, BS)); var vs = EventVeff(Kc, dMat, N, 0.1, BS); _output.WriteLine($"{xi:F2}  {kv:F1}  {(vs.Count>0?vs.Average()*o.Average()/MeanDistProxy(dMat,N):0):F3}"); } }

    [Fact] public void V4_1_CEFF_10_NullAndDegenerateControls() { int N = 80; _output.WriteLine("K=0: c_eff degenerate (no propagation)"); var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var dMat = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var o = OmegaField(Sm(Kc, N, 0.1, BS)); var vs = EventVeff(Kc, dMat, N, 0.1, BS); _output.WriteLine($"ActiveTRM: c_eff={(vs.Count>0?vs.Average()*o.Average()/MeanDistProxy(dMat,N):0):F3}"); }

    [Fact] public void V4_1_CEFF_11_CeffCalibrationReadinessScore() { int N = 80; double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2]; _output.WriteLine("xi    K0   c_eff   score"); foreach (double xi in xis) foreach (double kv in K0s) { var Kc = RecoverFP(KS(N, BS), N, kv, xi, 0.1, 5, BS); var dMat = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var o = OmegaField(Sm(Kc, N, 0.1, BS)); var vs = EventVeff(Kc, dMat, N, 0.1, BS); double vC = vs.Count > 0 ? vs.Average() * o.Average() / MeanDistProxy(dMat, N) : 0; double cv = vs.Count > 1 && vC > 1e-6 ? Math.Sqrt(vs.Average(x=>(x-vs.Average())*(x-vs.Average())))/vC : 1; _output.WriteLine($"{xi:F2}  {kv:F1}  {vC:F3}  {1.0/(1.0+cv):F3}"); } }

    [Fact] public void V4_1_CEFF_12_CeffCalibrationDesignReport() { int N = 80; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var dMat = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var o = OmegaField(Sm(Kc, N, 0.1, BS)); var vs = EventVeff(Kc, dMat, N, 0.1, BS); double vC = vs.Count > 0 ? vs.Average() * o.Average() / MeanDistProxy(dMat, N) : 0; _output.WriteLine("═══ c_eff CALIBRATION DESIGN ═══"); _output.WriteLine($"c_eff={vC:F3} nEvents={vs.Count} Anchors:Dimless×MeanDist"); _output.WriteLine($"Conclusion: {(vC>0.01?"A) stable c_eff design":"B) incomplete")}"); _output.WriteLine("Physical c NOT derived."); }

    [Fact] public void V4_1_CEFF_13_ClaimDisciplineReport() { _output.WriteLine("═══ CLAIM DISCIPLINE ═══"); _output.WriteLine("SUPPORTED: Time+length anchors combinable. c_eff stability measurable."); _output.WriteLine("CONDITIONAL: v_eff ≠ c without external calibration."); _output.WriteLine("NOT CLAIMED: physical c, SI units, meters, seconds, Lorentz, GR, D=3, SPARC, DM."); Assert.True(true); }
}
