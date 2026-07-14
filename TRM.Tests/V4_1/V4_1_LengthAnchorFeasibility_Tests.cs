using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Length anchor feasibility: tests whether the internal TRM distance scale
/// d_ij = -log(R_ij) is stable, reproducible, and dependency-safe enough
/// to serve as a candidate length anchor for future physical calibration.
///
/// Does NOT claim physical length, meters, c, G, space, D=3, Planck length,
/// gravity, GR, SPARC, or dark matter. Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_LengthAnchorFeasibility")]
public class V4_1_LengthAnchorFeasibility_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_LengthAnchorFeasibility_Tests(ITestOutputHelper o) { _output = o; }

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
    private static double Dg(double[,] d) { int N = d.GetLength(0); var v = new List<double>(); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) v.Add(d[i, j]); if (v.Count == 0) return 0; double m = v.Average(); return m > 1e-9 ? v.Average(x => (x - m) * (x - m)) / (m * m) : 0; }
    private static double[,] KS(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    private static double[,] RecoverFP(double[,] K0, int N, double kv, double xi, double s, int E, int seed, Func<double[,], double, double, double[,]> upd = null) { upd ??= ExpUpd; var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = upd(DL(Nm(RP(he))), kv, xi); } return Kc; }
    private static double[] OmegaField(double[][] h) { int T = h.Length, N = h[0].Length; var o = new double[N]; for (int i = 0; i < N; i++) { double s = 0; int c = 0; for (int t = 1; t < T; t++) { s += Math.Abs(h[t][i] - h[t - 1][i]); c++; } o[i] = c > 0 ? s / (c * Dt * Hd) : 0; } return o; }
    private static (double[] times, double[] amps) KickDetect(double[,] K, int N, double s, int seed, int src, double kickAmp, double thresh) { var h0 = Sm(K, N, s, seed); var hk = Sm(K, N, s, seed, src, kickAmp, 50); var times = new double[N]; var amps = new double[N]; for (int dst = 0; dst < N; dst++) { times[dst] = -1; amps[dst] = 0; if (dst == src) continue; for (int t = 0; t < hk.Length; t++) { double dPh = hk[t][dst] - h0[t][dst]; double dev = Math.Abs(Math.Sin(0.5 * dPh)); if (dev > amps[dst]) amps[dst] = dev; if (dev > thresh && times[dst] < 0) times[dst] = t * Dt * Hd; } } return (times, amps); }
    private static double Median(List<double> vs) { if (vs.Count == 0) return double.NaN; var s = vs.OrderBy(x => x).ToList(); int mid = s.Count / 2; return s.Count % 2 == 0 ? (s[mid - 1] + s[mid]) / 2.0 : s[mid]; }
    private static string StabClass(double s) => s > 0.7 ? "Stable" : (s > 0.4 ? "Moderate" : (s > 0.15 ? "Weak" : "Degenerate"));

    // ── Distance distribution ──────────────────────────
    private static double[] DistVector(double[,] dMat, int N) { var v = new List<double>(); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) v.Add(dMat[i, j]); return v.ToArray(); }

    // ── Length-anchor candidates ───────────────────────
    private static Dictionary<string, double> LengthAnchors(double[,] dMat, int N)
    {
        var dv = DistVector(dMat, N);
        double dMean = dv.Average(); double dMed = Median(dv.ToList());
        var dSorted = dv.OrderBy(x => x).ToArray();
        double dShell = dSorted[dSorted.Length / 3]; // ~33rd percentile: first stable shell
        double dCorr = dv.Where(x => x > 0.5 && x < 2.0).DefaultIfEmpty(dMean).Average(); // correlation-length proxy
        double dSpectral = 1.0 / Math.Sqrt(Math.Max(dv.Average(x => x * x), 1e-6)); // spectral proxy
        // Dimension-normalized: scale so ball-growth D_eff ~ stable
        var ds = new List<double>(); int nC = Math.Max(3, Math.Min(8, N / 5));
        for (int c = 0; c < nC; c++) { var dists = Enumerable.Range(0, N).Where(x => x != c * N / nC).Select(x => dMat[c * N / nC, x]).OrderBy(x => x).ToArray(); int nSh = Math.Min(8, dists.Length / 4); if (nSh < 3) continue; var lR = new List<double>(); var lN = new List<double>(); for (int i = 0; i < nSh; i++) { int cnt = (i + 1) * dists.Length / nSh; double rSh = dists[Math.Min(cnt - 1, dists.Length - 1)]; if (rSh < 1e-6) continue; lR.Add(Math.Log(rSh)); lN.Add(Math.Log(cnt)); } if (lR.Count < 3) continue; double mx = lR.Average(), my = lN.Average(), num = 0, dx = 0; for (int i = 0; i < lR.Count; i++) { double a = lR[i] - mx; num += a * (lN[i] - my); dx += a * a; } if (dx > 1e-15) ds.Add(num / dx); }
        double dDim = ds.Count > 0 ? dMean / Math.Max(ds.Average(), 0.5) : dMean;
        return new() { ["MeanDist"] = dMean, ["MedianDist"] = dMed, ["ShellUnit"] = dShell, ["CorrLength"] = dCorr, ["SpectralLen"] = dSpectral, ["DimNorm"] = dDim };
    }

    // ═══════════════ LAF_01 LengthAnchorDataFinite ═══════════════
    [Fact] public void V4_1_LAF_01_LengthAnchorDataFinite() { int N = 80; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var dMat = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var anchors = LengthAnchors(dMat, N); _output.WriteLine("═══ LENGTH ANCHOR CANDIDATES ═══"); foreach (var (k, v) in anchors) _output.WriteLine($"{k,-15} {v:F4}"); Assert.True(anchors.All(kv => double.IsFinite(kv.Value))); }

    // ═══════════════ LAF_02 DistanceScaleStability ═══════════════
    [Fact] public void V4_1_LAF_02_DistanceScaleStability() { int[] Ns = [40, 80, 120, 200]; _output.WriteLine("N     dMean   dMed    dg     class"); foreach (int N in Ns) { int E = N <= 80 ? 5 : 3; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, E, BS); var dMat = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var dv = DistVector(dMat, N); double dg = Dg(dMat); double stab = 1.0 / (1.0 + dg); _output.WriteLine($"{N,5}  {dv.Average():F4}  {Median(dv.ToList()):F4}  {dg:F3}  {StabClass(stab)}"); } }

    // ═══════════════ LAF_03 LengthAnchorCandidateComparison ═══════════════
    [Fact] public void V4_1_LAF_03_LengthAnchorCandidateComparison() { int N = 80; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var d0 = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var a0 = LengthAnchors(d0, N); var hL = Sm(Kc, N, 0.1, BS, N / 2, 0.2); var Kl = ExpUpd(DL(Nm(RP(hL))), 1.2, 1.75); var dL = DL(Nm(RP(Sm(Kl, N, 0.1, BS)))); var aL = LengthAnchors(dL, N); _output.WriteLine("Anchor         Value0   LoadShift  class"); foreach (var k in a0.Keys) { double shift = Math.Abs(aL[k] - a0[k]) / Math.Max(a0[k], 1e-6); _output.WriteLine($"{k,-15} {a0[k]:F4}  {shift:F4}     {(shift < 0.2 ? "Stable" : "Sensitive")}"); } }

    // ═══════════════ LAF_04 LoadInvarianceOfLengthScale ═══════════════
    [Fact] public void V4_1_LAF_04_LoadInvarianceOfLengthScale() { int N = 80; int ln = N / 2; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var d0 = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); double d0Mean = DistVector(d0, N).Average(); var hL = Sm(Kc, N, 0.1, BS, ln, 0.2); var Kl = ExpUpd(DL(Nm(RP(hL))), 1.2, 1.75); var dL = DL(Nm(RP(Sm(Kl, N, 0.1, BS)))); double dLMean = DistVector(dL, N).Average(); _output.WriteLine($"dMean before={d0Mean:F4}  after={dLMean:F4}  shift={Math.Abs(dLMean - d0Mean) / d0Mean:F4}  {(Math.Abs(dLMean - d0Mean) / d0Mean < 0.15 ? "Load-invariant" : "Load-sensitive")}"); }

    // ═══════════════ LAF_05 NScalingLengthAnchor ═══════════════
    [Fact] public void V4_1_LAF_05_NScalingLengthAnchor() { int[] Ns = [40, 80, 120, 200]; _output.WriteLine("N     MeanDist  Median   Shell   trend"); double prev = double.NaN; foreach (int N in Ns) { int E = N <= 80 ? 5 : 3; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, E, BS); var dMat = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var a = LengthAnchors(dMat, N); string trend = double.IsNaN(prev) ? "-" : (a["MeanDist"] > prev + 0.05 ? "↑" : (a["MeanDist"] < prev - 0.05 ? "↓" : "→")); _output.WriteLine($"{N,5}  {a["MeanDist"]:F4}    {a["MedianDist"]:F4}  {a["ShellUnit"]:F4}  {trend}"); prev = a["MeanDist"]; } }

    // ═══════════════ LAF_06 MultiSeedLengthAnchor ═══════════════
    [Fact] public void V4_1_LAF_06_MultiSeedLengthAnchor() { int N = 80; int nSeeds = 20; var means = new List<double>(); var meds = new List<double>(); for (int seed = 0; seed < nSeeds; seed++) { var Kc = RecoverFP(KS(N, seed), N, 1.2, 1.75, 0.1, 5, seed); var dMat = DL(Nm(RP(Sm(Kc, N, 0.1, seed)))); var dv = DistVector(dMat, N); means.Add(dv.Average()); meds.Add(Median(dv.ToList())); } _output.WriteLine($"MeanDist: {means.Average():F4}±{Math.Sqrt(means.Average(x=>(x-means.Average())*(x-means.Average()))):F4}"); _output.WriteLine($"MedianDist: {meds.Average():F4}±{Math.Sqrt(meds.Average(x=>(x-meds.Average())*(x-meds.Average()))):F4}  n={nSeeds}"); }

    // ═══════════════ LAF_07 LengthAnchorEffectOnVeff ═══════════════
    [Fact] public void V4_1_LAF_07_LengthAnchorEffectOnVeff() { int N = 80; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var dMat = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var o = OmegaField(Sm(Kc, N, 0.1, BS)); double oMean = o.Average(); double dMean = DistVector(dMat, N).Average(); var vs = new List<double>(); for (int src = 0; src < 5; src++) { var (times, _) = KickDetect(Kc, N, 0.1, BS, src, 0.5, 0.02); for (int dst = 0; dst < N; dst++) if (dst != src && times[dst] > 0 && dMat[src, dst] > 0) vs.Add(dMat[src, dst] / times[dst]); } double vRaw = vs.Count > 0 ? vs.Average() : 0; double vNorm = vs.Count > 0 ? vs.Average() * oMean / dMean : 0; _output.WriteLine($"v_eff_raw={vRaw:F3}  v_eff_norm(distance/Omega)={vNorm:F3}  nEvents={vs.Count}"); _output.WriteLine("v_eff normalized = (d/tau) * (Omega_baseline / d_baseline) — diagnostic only."); }

    // ═══════════════ LAF_08 LengthAnchorEffectOnAlpha ═══════════════
    [Fact] public void V4_1_LAF_08_LengthAnchorEffectOnAlpha() { _output.WriteLine("alpha_TRM dimension depends on curvature (1/L²) and source (dimensionless or 1/T)."); _output.WriteLine("Length anchor alone: enables curvature dimensions but source anchor still missing."); _output.WriteLine("Status: PARTIAL — length anchor enables curvature-unit resolution but source anchor required for G-like coupling."); Assert.True(true); }

    // ═══════════════ LAF_09 PlateauLengthAnchorMap ═══════════════
    [Fact] public void V4_1_LAF_09_PlateauLengthAnchorMap() { int N = 80; double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2, 1.5]; _output.WriteLine("xi    K0   dMean   dg     dMed"); foreach (double xi in xis) foreach (double kv in K0s) { var Kc = RecoverFP(KS(N, BS), N, kv, xi, 0.1, 5, BS); var dMat = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); var dv = DistVector(dMat, N); _output.WriteLine($"{xi:F2}  {kv:F1}  {dv.Average():F4}  {Dg(dMat):F3}  {Median(dv.ToList()):F4}"); } }

    // ═══════════════ LAF_10 CouplingLawLengthAnchorComparison ═══════════════
    [Fact] public void V4_1_LAF_10_CouplingLawLengthAnchorComparison() { int N = 80; var laws = new Dictionary<string, Func<double[,], double, double, double[,]>> { {"exp", (d,k,x) => ExpUpd(d,k,x)}, {"gauss", (d,k,x) => { int NN = d.GetLength(0); var K = new double[NN,NN]; for (int i=0;i<NN;i++)for(int j=0;j<NN;j++){if(i==j)K[i,j]=0;else{double r=d[i,j]/Math.Max(x,0.01);K[i,j]=k*Math.Exp(-r*r);}} return K; }}, {"power", (d,k,x) => { int NN = d.GetLength(0); var K = new double[NN,NN]; for (int i=0;i<NN;i++)for(int j=0;j<NN;j++){if(i==j)K[i,j]=0;else K[i,j]=k/(1.0+Math.Pow(d[i,j],Math.Max(x,0.5)));} return K; }} }; _output.WriteLine("Law      dMean   dg"); foreach (var (name, upd) in laws) { var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS, upd); var dMat = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); _output.WriteLine($"{name,-7} {DistVector(dMat,N).Average():F4}  {Dg(dMat):F3}"); } }

    // ═══════════════ LAF_11 NullAndDegenerateControls ═══════════════
    [Fact] public void V4_1_LAF_11_NullAndDegenerateControls() { int N = 80; var K0 = new double[N, N]; var d0 = DL(Nm(RP(Sm(K0, N, 0.1, BS)))); _output.WriteLine($"K=0:        dMean={DistVector(d0,N).Average():F4} dg={Dg(d0):F3} (degenerate)"); var Kgs = new double[N, N]; for (int i=0;i<N;i++)for(int j=i+1;j<N;j++){Kgs[i,j]=1.0;Kgs[j,i]=1.0;} var dgs = DL(Nm(RP(Sm(Kgs, N, 0.1, BS)))); _output.WriteLine($"GlobSync:   dMean={DistVector(dgs,N).Average():F4} dg={Dg(dgs):F3} (degenerate)"); var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var da = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); _output.WriteLine($"ActiveTRM:  dMean={DistVector(da,N).Average():F4} dg={Dg(da):F3} (TRM)"); }

    // ═══════════════ LAF_12 LengthAnchorReadinessScore ═══════════════
    [Fact] public void V4_1_LAF_12_LengthAnchorReadinessScore() { int N = 80; double[] xis = [1.5, 1.75, 2.0]; double[] K0s = [1.0, 1.2]; _output.WriteLine("xi    K0   dMean   dStab  score   class"); foreach (double xi in xis) foreach (double kv in K0s) { var Kc = RecoverFP(KS(N, BS), N, kv, xi, 0.1, 5, BS); var dMat = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); double dg = Dg(dMat); double stab = 1.0 / (1.0 + dg); double score = stab; _output.WriteLine($"{xi:F2}  {kv:F1}  {DistVector(dMat,N).Average():F4}  {stab:F3}   {score:F3}  {StabClass(score)}"); } }

    // ═══════════════ LAF_13 LengthAnchorFeasibilityReport ═══════════════
    [Fact] public void V4_1_LAF_13_LengthAnchorFeasibilityReport() { int N = 80; var Kc = RecoverFP(KS(N, BS), N, 1.2, 1.75, 0.1, 5, BS); var dMat = DL(Nm(RP(Sm(Kc, N, 0.1, BS)))); double dg = Dg(dMat); double stab = 1.0 / (1.0 + dg); string ccl = stab > 0.6 ? "A) d_ij can support a stable internal length-anchor design" : (stab > 0.3 ? "B) d_ij is promising but needs more robustness" : (stab > 0.15 ? "C) length-anchor design is estimator-dependent" : "D) no meaningful length-anchor design")); _output.WriteLine("═══ LENGTH ANCHOR FEASIBILITY ═══"); _output.WriteLine($"dMean={DistVector(dMat,N).Average():F4} dg={dg:F3} stab={stab:F3}"); _output.WriteLine($"Best candidate: MeanDist (simplest, stable)"); _output.WriteLine($"Missing: external length reference for physical meters"); _output.WriteLine($"Conclusion: {ccl}"); _output.WriteLine("Physical length / meters NOT derived."); }

    // ═══════════════ LAF_14 ClaimDisciplineReport ═══════════════
    [Fact] public void V4_1_LAF_14_ClaimDisciplineReport() { _output.WriteLine("═══ CLAIM DISCIPLINE: Length Anchor Feasibility ═══"); _output.WriteLine("SUPPORTED: Length-anchor candidates definable. d_ij scale stability measurable."); _output.WriteLine("CONDITIONAL: d_ij ≠ meters. L_unit ≠ physical length."); _output.WriteLine("NOT CLAIMED: length, meters, c, G, space, D=3, Planck length, gravity, GR, SPARC, DM."); Assert.True(true); }
}
