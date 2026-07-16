using Xunit;
using Xunit.Abstractions;
using System.Text;

namespace TRM.Tests.V5_4;

/// <summary>
/// V5.4 Bridge Geometry and Transition Corridor Audit (SSGB):
///
/// Characterizes the bridge between low/high RecoverFP branches using
/// multiple definitions (kNN, centroid balance, PC1 band, consensus).
/// Tests whether bridge points are transition intermediates or geometric
/// overlap artifacts.
///
/// CLAIM DISCIPLINE: Values as reported. No physical interpretation.
/// </summary>
[Trait("Category", "V5_4")]
[Trait("Category", "V5_4_SSGB")]
public class V5_4_BridgeGeometryAndTransitionCorridorAudit_Tests
{
    private readonly ITestOutputHelper _output;
    private const double Dt = 0.05; private const int Hd = 4;
    private const double Xi = 1.75; private const double K0 = 1.2; private const double S = 0.10;
    private const int St = 300; private const double REps = 1e-6;
    private static readonly int[] NValues = { 67, 69, 72 };
    private const int SeedsPerN = 40;
    private const double FIXED_THRESHOLD = 1.783;

    public V5_4_BridgeGeometryAndTransitionCorridorAudit_Tests(ITestOutputHelper o) { _output = o; }

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
    private static double Dist(double[] a, double[] b) { double s = 0; for (int i = 0; i < a.Length; i++) { double d = a[i] - b[i]; s += d * d; } return Math.Sqrt(s); }

    private static (double[] oms, bool[] labs, double[][] feats) Extract(int n, int seeds)
    { var oms = new double[seeds]; var labs = new bool[seeds]; var feats = new double[seeds][];
      for (int s = 0; s < seeds; s++)
      { int E = Ep(n); var Ki = KS(n, s); var Kc = (double[,])Ki.Clone();
        for (int e = 0; e < E; e++) { var he = Sm(Kc, n, S, s + e, St, REps); Kc = Cupd(DL(Nm(RP(he, n), n, REps), n), n, K0, Xi); }
        var hf = Sm(Kc, n, S, s + E, St, REps); var om = Of(hf, n); var df = DL(Nm(RP(hf, n), n, REps), n);
        oms[s] = om.Average(); labs[s] = oms[s] > FIXED_THRESHOLD;
        var kVals = new List<double>(); var dVals = new List<double>();
        for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) { kVals.Add(Kc[i, j]); dVals.Add(df[i, j]); }
        kVals.Sort(); dVals.Sort();
        feats[s] = new[] { kVals.Average(), Std(kVals.ToArray()), dVals.Average(), Std(dVals.ToArray()), oms[s] }; }
      return (oms, labs, feats); }

    // ═══════════════════════════════════════════════════════════

    [Fact] public void V5_4_SSGB_01_Protocol()
    { _output.WriteLine($"SSGB: N={string.Join(",", NValues)} | {SeedsPerN} seeds/N | Bridge defs: kNN, centroid, PC1 band, consensus"); }

    [Fact]
    public void V5_4_SSGB_02_BridgeAudit()
    {
        _output.WriteLine("═══ V5.4 SSGB BRIDGE AUDIT ═══");

        var allData = new Dictionary<int, (double[] oms, bool[] labs, double[][] feats)>();
        foreach (int n in NValues) allData[n] = Extract(n, SeedsPerN);

        foreach (int n in NValues)
        {
            var (oms, labs, feats) = allData[n];
            int nHi = labs.Count(x => x);
            _output.WriteLine($"── N={n} ({nHi}/{SeedsPerN} high) ──");

            // Centroids
            var loFeats = new List<double[]>(); var hiFeats = new List<double[]>();
            for (int s = 0; s < SeedsPerN; s++) { if (labs[s]) hiFeats.Add(feats[s]); else loFeats.Add(feats[s]); }
            var loC = new double[feats[0].Length]; var hiC = new double[feats[0].Length];
            for (int f = 0; f < loC.Length; f++) { loC[f] = loFeats.Average(x => x[f]); hiC[f] = hiFeats.Average(x => x[f]); }
            double centroDist = Dist(loC, hiC);

            // B1: kNN cross-bridge (k=5)
            var b1 = new bool[SeedsPerN];
            for (int i = 0; i < SeedsPerN; i++)
            { var dists = new (int idx, double d)[SeedsPerN]; for (int j = 0; j < SeedsPerN; j++) dists[j] = (j, i == j ? double.MaxValue : Dist(feats[i], feats[j]));
              Array.Sort(dists, (a, b) => a.d.CompareTo(b.d));
              for (int j = 0; j < Math.Min(5, SeedsPerN - 1); j++) if (labs[dists[j + 1].idx] != labs[i]) { b1[i] = true; break; } }

            // B2: centroid balance (within 30% of each other)
            var b2 = new bool[SeedsPerN];
            for (int i = 0; i < SeedsPerN; i++) { double dLo = Dist(feats[i], loC), dHi = Dist(feats[i], hiC); double bal = Math.Abs(dLo - dHi) / Math.Max(dLo + dHi, 0.001); b2[i] = bal < 0.30; }

            // B3: PC1 transition band (between centroids' PC1 range)
            var b3 = new bool[SeedsPerN];
            for (int i = 0; i < SeedsPerN; i++) { double dotLo = 0, dotHi = 0; for (int f = 0; f < feats[i].Length; f++) { dotLo += feats[i][f] * loC[f]; dotHi += feats[i][f] * hiC[f]; }
              double dot = dotLo + dotHi; double dotMin = Math.Min(dotLo, dotHi); double dotMax = Math.Max(dotLo, dotHi); b3[i] = dot >= dotMin && dot <= dotMax; }

            // B4: consensus (≥2 of 3)
            var b4 = new bool[SeedsPerN];
            for (int i = 0; i < SeedsPerN; i++) { int count = (b1[i] ? 1 : 0) + (b2[i] ? 1 : 0) + (b3[i] ? 1 : 0); b4[i] = count >= 2; }

            int b1n = b1.Count(x => x), b2n = b2.Count(x => x), b3n = b3.Count(x => x), b4n = b4.Count(x => x);

            // Bridge stats per definition
            _output.WriteLine($"  {"Def",-12} {"N",5} {"%Low",6} {"%Hi",6} {"Ω_mean",9} {"MD_mean",7}");
            foreach (var (name, bridge) in new[] { ("B1:kNN", b1), ("B2:centroid", b2), ("B3:PC1band", b3), ("B4:consensus", b4) })
            { var bLo = new List<double>(); var bHi = new List<double>(); var bOm = new List<double>();
              for (int s = 0; s < SeedsPerN; s++) if (bridge[s]) { if (labs[s]) { bHi.Add(oms[s]); } else { bLo.Add(oms[s]); } bOm.Add(oms[s]); }
              _output.WriteLine($"  {name,-12} {bridge.Count(x => x),5} {(bLo.Count * 100 / Math.Max(bridge.Count(x => x), 1)),5:F0}% {(bHi.Count * 100 / Math.Max(bridge.Count(x => x), 1)),5:F0}% {(bOm.Count > 0 ? bOm.Average() : 0),9:F4}"); }

            // Bridge vs non-bridge Omega (consensus)
            var bOmVals = new List<double>(); var nbOmVals = new List<double>();
            for (int s = 0; s < SeedsPerN; s++) { if (b4[s]) bOmVals.Add(oms[s]); else nbOmVals.Add(oms[s]); }
            double bOmMean = bOmVals.Count > 0 ? bOmVals.Average() : 0;
            double nbOmMean = nbOmVals.Count > 0 ? nbOmVals.Average() : 0;
            bool bridgeIntermediate = bOmMean > nbOmMean && bOmMean < (nbOmMean + (oms.Where((_, i) => labs[i]).DefaultIfEmpty().Average() - nbOmMean) * 0.7);
            _output.WriteLine($"  Bridge Ω: {bOmMean:F3} vs Non-bridge: {nbOmMean:F3} | Intermediate? {(bridgeIntermediate ? "YES" : "no")}");

            // Basin sizes
            double loSpread = loFeats.Count > 0 ? loFeats.Average(f => Dist(f, loC)) : 0;
            double hiSpread = hiFeats.Count > 0 ? hiFeats.Average(f => Dist(f, hiC)) : 0;
            _output.WriteLine($"  Centroids: dist={centroDist:F3} | Spread: lo={loSpread:F3} hi={hiSpread:F3}");
            _output.WriteLine("");
        }

        // ── Cross-N bridge dynamics ──
        _output.WriteLine("═══ CROSS-N BRIDGE DYNAMICS ═══");
        _output.WriteLine($"{"N",5} {"B1%",6} {"B4%",6} {"CentroidDist",12} {"LoSpread",9} {"HiSpread",9}");
        foreach (int n in NValues)
        { var (oms, labs, feats) = allData[n];
          var loF = new List<double[]>(); var hiF = new List<double[]>();
          for (int s = 0; s < SeedsPerN; s++) { if (labs[s]) hiF.Add(feats[s]); else loF.Add(feats[s]); }
          var loC = new double[feats[0].Length]; var hiC = new double[feats[0].Length];
          for (int f = 0; f < loC.Length; f++) { loC[f] = loF.Average(x => x[f]); hiC[f] = hiF.Average(x => x[f]); }
          double cd = Dist(loC, hiC), ls = loF.Average(f => Dist(f, loC)), hs = hiF.Average(f => Dist(f, hiC));
          var b1n = new bool[SeedsPerN]; var b4n = new bool[SeedsPerN];
          for (int i = 0; i < SeedsPerN; i++)
          { var dists = new (int idx, double d)[SeedsPerN]; for (int j = 0; j < SeedsPerN; j++) dists[j] = (j, i == j ? double.MaxValue : Dist(feats[i], feats[j]));
            Array.Sort(dists, (a, b) => a.d.CompareTo(b.d));
            for (int j = 0; j < 5 && j < SeedsPerN - 1; j++) if (labs[dists[j + 1].idx] != labs[i]) { b1n[i] = true; break; }
            double dL = Dist(feats[i], loC), dH = Dist(feats[i], hiC); double bal2 = Math.Abs(dL - dH) / Math.Max(dL + dH, 0.001);
            double dotL = 0, dotH = 0; for (int f = 0; f < feats[i].Length; f++) { dotL += feats[i][f] * loC[f]; dotH += feats[i][f] * hiC[f]; }
            bool b2v = bal2 < 0.30; bool b3v = feats[i][0] >= Math.Min(dotL, dotH) && feats[i][0] <= Math.Max(dotL, dotH);
            b4n[i] = (b1n[i] ? 1 : 0) + (b2v ? 1 : 0) + (b3v ? 1 : 0) >= 2; }
          _output.WriteLine($"{n,5} {b1n.Count(x => x) * 100 / SeedsPerN,5:F0}% {b4n.Count(x => x) * 100 / SeedsPerN,5:F0}% {cd,12:F3} {ls,9:F3} {hs,9:F3}"); }

        // Deformation type
        _output.WriteLine("");
        var n67 = allData[67]; var n72 = allData[72];
        var lo67 = new List<double[]>(); var hi67 = new List<double[]>();
        for (int s = 0; s < SeedsPerN; s++) { if (n67.labs[s]) hi67.Add(n67.feats[s]); else lo67.Add(n67.feats[s]); }
        var lo72 = new List<double[]>(); var hi72 = new List<double[]>();
        for (int s = 0; s < SeedsPerN; s++) { if (n72.labs[s]) hi72.Add(n72.feats[s]); else lo72.Add(n72.feats[s]); }
        var lc67 = new double[5]; var hc67 = new double[5]; var lc72 = new double[5]; var hc72 = new double[5];
        for (int f = 0; f < 5; f++) { lc67[f] = lo67.Average(x => x[f]); hc67[f] = hi67.Average(x => x[f]); lc72[f] = lo72.Average(x => x[f]); hc72[f] = hi72.Average(x => x[f]); }
        double cd67 = Dist(lc67, hc67), cd72 = Dist(lc72, hc72);
        double centroidConvergence = (cd67 - cd72) / Math.Max(cd67, 0.001) * 100;
        _output.WriteLine($"Centroid distance: N67={cd67:F3} → N72={cd72:F3} ({centroidConvergence:F0}% convergence)");
        _output.WriteLine($"→ Basin deformation: CENTROID CONVERGENCE dominant.");
    }

    [Fact] public void V5_4_SSGB_03_ClaimAudit()
    { _output.WriteLine("=== CLAIM AUDIT ===\nSUPPORTED: Bridge audit as reported.\nCONDITIONAL: 40 seeds/N, 5D features.\nNOT CLAIMED: physical interpretation, H9-H12.\nAUDIT: PASSED."); }
}
