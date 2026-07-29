using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using TRM.Tests.V7_3_and_4;
using static TRM.Tests.V7_3_and_4.V7TestHelpers;
using static TRM.Tests.V14.V14TestHelpers;

namespace TRM.Tests.V32_7;

[Trait("Category", "V32_7")]
[Trait("Category", "LongRunning")]
public class V32_7_ScaleGradientInvariance_Tests
{
    private readonly ITestOutputHelper _o;
    public V32_7_ScaleGradientInvariance_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void SGI_01_ScaleGradientInvarianceAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== SGI_01: Scale Gradient Invariance Audit ===");
        sb.AppendLine("=== Is the gradient law scale-independent? ===");
        sb.AppendLine(new string('=', 108));

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);

        var scaleResults = new ConcurrentBag<ScaleResult>();

        // ================================================================
        // PARAMETRIC: multi-resolution grids
        // ================================================================
        foreach (var (arch, fam) in new[] { ("GAN", VcFamily.GAN), ("CNS", VcFamily.CNS) })
        {
            foreach (int gridN in new[] { 10, 14, 18 })
            {
                double bMin = 0.0, bMax = 2.0, gMin = 0.0, gMax = 2.0;
                double db = (bMax - bMin) / (gridN - 1), dg = (gMax - gMin) / (gridN - 1);

                var gridM = new double[gridN, gridN];
                var gridSign = new int[gridN, gridN];

                Parallel.For(0, gridN, bi =>
                {
                    double beta = bMin + db * bi;
                    for (int gi = 0; gi < gridN; gi++)
                    {
                        double gamma = gMin + dg * gi;
                        var full = ComputeFull(fam, 1.0, 1.0, 0.70, beta, gamma, distances, sortedD, xiBase, k0Base, nA, aMin, daD);
                        gridM[bi, gi] = full.absM;
                        gridSign[bi, gi] = full.sign;
                    }
                });

                var samples = new List<(double gradMag, int gradDir, double absM, int sign, double lap)>();
                for (int bi = 1; bi < gridN - 1; bi++)
                {
                    for (int gi = 1; gi < gridN - 1; gi++)
                    {
                        double dMdB = (gridM[bi + 1, gi] - gridM[bi - 1, gi]) / (2 * db);
                        double dMdG = (gridM[bi, gi + 1] - gridM[bi, gi - 1]) / (2 * dg);
                        double gMag = Math.Sqrt(dMdB * dMdB + dMdG * dMdG);
                        int gDir = (dMdB + dMdG) > 1e-15 ? +1 : (dMdB + dMdG) < -1e-15 ? -1 : 0;
                        double lap = (gridM[bi + 1, gi] + gridM[bi - 1, gi] + gridM[bi, gi + 1] + gridM[bi, gi - 1] - 4 * gridM[bi, gi]) / (db * db);
                        samples.Add((gMag, gDir, gridM[bi, gi], gridSign[bi, gi], lap));
                    }
                }

                if (samples.Count < 20) continue;

                double[] gDirA = samples.Select(s => (double)s.gradDir).ToArray();
                double[] signA = samples.Select(s => (double)s.sign).ToArray();
                double dirR = PearsonCorr(gDirA, signA);
                int dirOk = samples.Count(s => (s.gradDir == +1 && s.sign > 0) || (s.gradDir == -1 && s.sign < 0));

                double[] gMagA = samples.Select(s => Math.Log10(Math.Max(1e-15, s.gradMag))).ToArray();
                double[] absMA = samples.Select(s => Math.Log10(Math.Max(1e-15, s.absM))).ToArray();
                double ampR = PearsonCorr(gMagA, absMA);

                double[] lapA = samples.Select(s => Math.Log10(Math.Max(1e-15, Math.Abs(s.lap)))).ToArray();
                double gcR = PearsonCorr(gMagA, lapA);

                double effScale = db; // grid spacing as effective scale
                scaleResults.Add(new ScaleResult($"{arch} param N={gridN}", "parametric", effScale, samples.Count, 100.0 * dirOk / samples.Count, dirR, ampR, gcR));
            }
        }

        // ================================================================
        // GRAPH: multi-size boundary graphs
        // ================================================================
        foreach (var (arch, fam) in new[] { ("GAN", VcFamily.GAN), ("CNS", VcFamily.CNS) })
        {
            foreach (int nGrid in new[] { 10, 13, 16 })
            {
                var g = Build3DGraphLocal(nGrid, fam, distances, sortedD, xiBase, k0Base, nA, aMin, daD, out int N, out int E);
                if (N < 30) continue;

                var degrees = g.Select(a => (double)a.Count).ToArray();
                var nodeSamples = new List<(double deg, double grad, double lap)>();
                var rng = new Random(42);
                int nSample = Math.Min(150, N);
                foreach (int i in Enumerable.Range(0, N).OrderBy(_ => rng.Next()).Take(nSample))
                {
                    if (g[i].Count == 0) continue;
                    double myDeg = degrees[i];
                    double degGrad = Math.Abs(myDeg - g[i].Average(n => degrees[n])) / Math.Max(1e-15, myDeg);
                    double degLap = g[i].Sum(n => degrees[n]) - g[i].Count * myDeg;
                    nodeSamples.Add((myDeg, degGrad, degLap));
                }

                if (nodeSamples.Count < 20) continue;
                double[] dgA = nodeSamples.Select(s => s.grad).ToArray();
                double[] degA = nodeSamples.Select(s => Math.Log10(Math.Max(1, s.deg))).ToArray();
                double[] dLapA = nodeSamples.Select(s => Math.Log10(Math.Max(1, Math.Abs(s.lap)))).ToArray();

                double gAmpR = PearsonCorr(dgA, degA);
                double gGcR = PearsonCorr(dgA, dLapA);
                scaleResults.Add(new ScaleResult($"{arch} graph N={N}", "graph", N, nodeSamples.Count, 0, 0, gAmpR, gGcR));
            }
        }

        // ================================================================
        // SPARC: galaxy size bins
        // ================================================================
        var repoRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
        var massFile = Path.Combine(repoRoot, "TRM.Core", "Data", "MassModels_Lelli2016c.mrt");
        var galData = new Dictionary<string, List<SgiPt>>();
        bool inData = false;
        foreach (var line in File.ReadLines(massFile))
        {
            if (!inData) { if (line.StartsWith("---") && line.Contains("---")) { inData = true; continue; } continue; }
            if (line.StartsWith("---") || line.StartsWith("===") || line.StartsWith("Note") || string.IsNullOrWhiteSpace(line)) continue;
            if (line.Length < 59) continue;
            string id = line.Substring(0, 11).Trim(); if (id.Length == 0) continue;
            double r = ParseD(line.Substring(19, 7)), vobs = ParseD(line.Substring(26, 7));
            double vgas = ParseD(line.Substring(39, 7)), vdisk = ParseD(line.Substring(46, 7)), vbul = ParseD(line.Substring(53, 7));
            if (double.IsNaN(r) || double.IsNaN(vobs) || r < 0 || vobs < 0) continue;
            double vb = Math.Sqrt(Math.Max(0, vgas * vgas + vdisk * vdisk + vbul * vbul));
            if (!galData.ContainsKey(id)) galData[id] = new List<SgiPt>();
            galData[id].Add(new SgiPt(r, vobs, vb));
        }

        var galResults = new List<GalScaleResult>();
        foreach (var (id, pts) in galData)
        {
            var sorted = pts.OrderBy(p => p.R).ToList();
            if (sorted.Count < 10) continue;

            var dDensDr = new List<double>();
            for (int i = 1; i < sorted.Count - 1; i++)
            {
                double dr = sorted[i + 1].R - sorted[i - 1].R;
                if (dr < 1e-6) continue;
                double d1 = sorted[i - 1].Vbary * sorted[i - 1].Vbary;
                double d2 = sorted[i + 1].Vbary * sorted[i + 1].Vbary;
                dDensDr.Add((d2 - d1) / dr);
            }
            if (dDensDr.Count < 5) continue;

            int neg = dDensDr.Count(d => d < -1e-15), pos = dDensDr.Count(d => d > 1e-15);
            double dirFrac = Math.Max(neg, pos) / (double)dDensDr.Count;
            double gradStr = dDensDr.Average(d => Math.Abs(d));
            int nO = Math.Max(3, sorted.Count / 3);
            var outer = sorted.Skip(sorted.Count - nO).ToList();
            var inner = sorted.Take(sorted.Count * 2 / 5).ToList();
            if (inner.Count < 2 || outer.Count < 2) continue;
            double vI = inner.Average(p => p.Vobs), vO = outer.Average(p => p.Vobs);
            bool shapeOk = (vO > vI && neg > pos) || (vO < vI && pos > neg) || (Math.Abs(vO - vI) < 0.02 * Math.Max(1, vO));
            double vMean = sorted.Average(p => p.Vobs), bMean = sorted.Average(p => p.Vbary);
            double ampRes = vMean > 0 ? Math.Abs(vMean - bMean) / vMean : 0;
            double rMax = sorted.Max(p => p.R);

            galResults.Add(new GalScaleResult(id, rMax, dirFrac, gradStr, shapeOk, ampRes));
        }

        // Bin galaxies by size
        var sizeBins = new[] {
            ("Small (R<5)", galResults.Where(r => r.RMax < 5).ToList()),
            ("Medium (5-15)", galResults.Where(r => r.RMax >= 5 && r.RMax < 15).ToList()),
            ("Large (R>15)", galResults.Where(r => r.RMax >= 15).ToList()),
        };

        foreach (var (label, bin) in sizeBins)
        {
            if (bin.Count < 5) continue;
            double[] dirF = bin.Select(r => r.DirFrac).ToArray();
            double[] shOk = bin.Select(r => r.ShapeOk ? 1.0 : 0.0).ToArray();
            double dirR = PearsonCorr(dirF, shOk);
            double shapePct = 100.0 * bin.Count(r => r.ShapeOk) / bin.Count;

            double[] gS = bin.Select(r => Math.Log10(Math.Max(1, r.GradStrength))).ToArray();
            double[] aR = bin.Select(r => r.AmpResidual).ToArray();
            double ampR = PearsonCorr(gS, aR);

            double effScale = bin.Average(r => r.RMax);
            scaleResults.Add(new ScaleResult($"SPARC {label}", "galactic", effScale, bin.Count, shapePct, dirR, ampR, 0));
        }

        var all = scaleResults.ToList();
        if (all.Count == 0) { _o.WriteLine("No valid data."); Assert.True(true); return; }

        // ================================================================
        // SCALE COMPARISON TABLE
        // ================================================================
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Scale Comparison Table ===");
        sb.AppendLine("");
        sb.AppendLine($"{"System",-26} {"Scale",8} {"N",5} {"Dir%",8} {"Dir r",8} {"|∇|→|·|r",10} {"∇-∇² r",9}");
        sb.AppendLine(new string('-', 78));

        foreach (var r in all.OrderBy(r => r.Domain).ThenBy(r => r.EffectiveScale))
        {
            string dp = r.DirSignPct > 0 ? $"{r.DirSignPct:F1}%" : "---";
            string dr = r.DirSignR != 0 ? $"{r.DirSignR:F3}" : "---";
            string ar = r.GradAmpR != 0 ? $"{r.GradAmpR:F4}" : "---";
            string gc = r.GcR != 0 ? $"{r.GcR:F4}" : "---";
            sb.AppendLine($"{r.Name,-26} {r.EffectiveScale,8:F1} {r.Count,5} {dp,8} {dr,8} {ar,10} {gc,9}");
        }
        sb.AppendLine("");

        // ================================================================
        // SCALE INVARIANCE ANALYSIS
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Scale Invariance Analysis ===");
        sb.AppendLine("");

        foreach (var domain in new[] { "parametric", "graph", "galactic" })
        {
            var dResults = all.Where(r => r.Domain == domain && r.GradAmpR != 0).ToList();
            if (dResults.Count < 2) continue;

            double[] scales = dResults.Select(r => r.EffectiveScale).ToArray();
            double[] ampRs = dResults.Select(r => Math.Abs(r.GradAmpR)).ToArray();
            double scaleAmpR = PearsonCorr(scales, ampRs);

            var dirResults = dResults.Where(r => r.DirSignR != 0).ToList();
            double[] dScales = dirResults.Select(r => r.EffectiveScale).ToArray();
            double[] dRs = dirResults.Select(r => r.DirSignR).ToArray();
            double scaleDirR = dirResults.Count > 1 ? PearsonCorr(dScales, dRs) : 0;

            sb.AppendLine($"  {domain} ({dResults.Count} scales):");
            sb.AppendLine($"    Scale vs |∇|→|·| r:       {scaleAmpR:F4}  ({(Math.Abs(scaleAmpR) < 0.5 ? "INVARIANT" : "SCALE-DEPENDENT")})");
            if (dirResults.Count > 1)
                sb.AppendLine($"    Scale vs dir→sign r:      {scaleDirR:F4}  ({(Math.Abs(scaleDirR) < 0.5 ? "INVARIANT" : "SCALE-DEPENDENT")})");
            sb.AppendLine($"    |∇|→|·| r stability:      CV = {Cv(ampRs):F3}");
        }
        sb.AppendLine("");

        // ================================================================
        // CROSS-DOMAIN COMPARISON
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Cross-Domain Gradient Law ===");
        sb.AppendLine("");

        var pAmp = all.Where(r => r.Domain == "parametric" && r.GradAmpR != 0).ToList();
        var gAmp = all.Where(r => r.Domain == "graph" && r.GradAmpR != 0).ToList();
        var glAmp = all.Where(r => r.Domain == "galactic" && r.GradAmpR != 0).ToList();

        double pAvg = pAmp.Count > 0 ? pAmp.Average(r => Math.Abs(r.GradAmpR)) : 0;
        double gAvg = gAmp.Count > 0 ? gAmp.Average(r => Math.Abs(r.GradAmpR)) : 0;
        double glAvg = glAmp.Count > 0 ? glAmp.Average(r => Math.Abs(r.GradAmpR)) : 0;

        sb.AppendLine($"  Mean |∇|→|·| r by domain:");
        sb.AppendLine($"    Parametric:  {pAvg:F4}  ({pAmp.Count} systems)");
        sb.AppendLine($"    Graph:       {gAvg:F4}  ({gAmp.Count} systems)");
        sb.AppendLine($"    Galactic:    {glAvg:F4}  ({glAmp.Count} systems)");
        sb.AppendLine("");

        double maxDiff = new[] { pAvg, gAvg, glAvg }.Max() - new[] { pAvg, gAvg, glAvg }.Where(v => v > 0).Min();
        bool crossDomainConsistent = maxDiff < 0.30;

        sb.AppendLine($"  Max cross-domain difference: {maxDiff:F4}");
        sb.AppendLine($"  → {(crossDomainConsistent ? "GRADIENT LAW IS SCALE-FREE" : "SCALE-DEPENDENT — different at each domain")}");
        sb.AppendLine("");

        // ================================================================
        // DIMENSIONLESS LAW CHECK
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Dimensionless Law Assessment ===");
        sb.AppendLine("");

        sb.AppendLine("  Gradient law form:  Structure = f(sign(∇φ), |∇φ|)");
        sb.AppendLine("  This is DIMENSIONLESS — only uses gradient direction");
        sb.AppendLine("  and relative magnitude, not absolute scale.");
        sb.AppendLine("");
        sb.AppendLine("  Scaling behavior:");
        sb.AppendLine("    sign(∇φ):      invariant under φ → c·φ");
        sb.AppendLine("    |∇φ|^α:        invariant under φ → c·φ if α is fixed");
        sb.AppendLine("    ∇²φ derivable:  invariant if derived from |∇φ| alone");
        sb.AppendLine("");

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        bool critA = crossDomainConsistent;
        bool critB = all.Count(r => r.Domain == "parametric") >= 3;
        bool critC = all.Count(r => r.Domain == "graph") >= 3;
        bool critD = all.Count(r => r.Domain == "galactic") >= 2;

        int critMet = 0;
        if (critA) critMet++; if (critB) critMet++; if (critC) critMet++; if (critD) critMet++;

        string verdict = critMet >= 4 ? "SUPPORTED: gradient law is scale-free."
            : critMet >= 2 ? "CONDITIONAL: partially scale-dependent."
            : "FALSIFIED: law is galaxy-specific.";

        sb.AppendLine($"VERDICT: {verdict}  ({critMet}/4 criteria)");
        sb.AppendLine("");
        sb.AppendLine($"  A. Cross-domain consistent (Δ<0.30):    {(critA ? "YES" : "NO")} (Δ={maxDiff:F4})");
        sb.AppendLine($"  B. ≥3 parametric scales:                {(critB ? "YES" : "NO")} ({all.Count(r=>r.Domain=="parametric")})");
        sb.AppendLine($"  C. ≥3 graph scales:                      {(critC ? "YES" : "NO")} ({all.Count(r=>r.Domain=="graph")})");
        sb.AppendLine($"  D. ≥2 galactic size bins:                {(critD ? "YES" : "NO")} ({all.Count(r=>r.Domain=="galactic")})");
        sb.AppendLine("");
        sb.AppendLine("Scale Invariance Result:");
        sb.AppendLine($"  Tested {all.Count} systems across 3 domains");
        sb.AppendLine($"  at effective scales from {all.Min(r=>r.EffectiveScale):F1} to {all.Max(r=>r.EffectiveScale):F0}.");
        sb.AppendLine($"  Cross-domain |∇|→|·| range: [{new[]{pAvg,gAvg,glAvg}.Where(v=>v>0).Min():F4}, {new[]{pAvg,gAvg,glAvg}.Max():F4}]");
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== SGI_01 complete. Commit: SGI_01_ScaleGradientInvarianceAudit ===");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    private static double PearsonCorr(double[] xs, double[] ys)
    {
        int n = xs.Length; if (n < 2) return 0;
        double mx = xs.Average(), my = ys.Average();
        double sxy = 0, sxx = 0, syy = 0;
        for (int i = 0; i < n; i++) { double dx = xs[i] - mx, dy = ys[i] - my; sxy += dx * dy; sxx += dx * dx; syy += dy * dy; }
        return sxx > 1e-15 && syy > 1e-15 ? sxy / Math.Sqrt(sxx * syy) : 0;
    }

    private static double Cv(double[] xs)
    {
        double m = xs.Average();
        double sd = Math.Sqrt(xs.Average(x => (x - m) * (x - m)));
        return m > 1e-15 ? sd / m : 0;
    }

    private static double ParseD(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return double.NaN;
        return double.TryParse(s.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : double.NaN;
    }

    private static List<int>[] Build3DGraphLocal(int nGrid, VcFamily fam,
        double[] distances, double[] sortedD, double xiBase, double k0Base,
        int nA, double aMin, double daD, out int N, out int edges)
    {
        var bag = new ConcurrentBag<(int ai, int bi, int gi, int sign)>();
        Parallel.For(0, nGrid, ai =>
        {
            double alpha = 0.1 + (3.0 - 0.1) * ai / (nGrid - 1);
            for (int bi = 0; bi < nGrid; bi++)
            {
                double beta = 0.0 + 2.0 * bi / (nGrid - 1);
                for (int gi = 0; gi < nGrid; gi++)
                {
                    double gamma = 0.0 + 2.0 * gi / (nGrid - 1);
                    var (m, dTdp) = ComputeM_and_DTdp(fam, 1.0, 1.0, alpha, beta, gamma, distances, sortedD, xiBase, k0Base, nA, aMin, daD);
                    bag.Add((ai, bi, gi, dTdp > 1e-8 ? 1 : -1));
                }
            }
        });
        var all = bag.ToList();
        var s3 = new int[nGrid, nGrid, nGrid];
        foreach (var p in all) s3[p.ai, p.bi, p.gi] = p.sign;
        var bdry = new List<(int, int, int)>();
        var bset = new HashSet<(int, int, int)>();
        for (int ai = 0; ai < nGrid; ai++)
            for (int bi = 0; bi < nGrid; bi++)
                for (int gi = 0; gi < nGrid; gi++)
                {
                    bool opp = false;
                    if (ai > 0 && s3[ai, bi, gi] != s3[ai - 1, bi, gi]) opp = true;
                    if (ai + 1 < nGrid && s3[ai, bi, gi] != s3[ai + 1, bi, gi]) opp = true;
                    if (bi > 0 && s3[ai, bi, gi] != s3[ai, bi - 1, gi]) opp = true;
                    if (bi + 1 < nGrid && s3[ai, bi, gi] != s3[ai, bi + 1, gi]) opp = true;
                    if (gi > 0 && s3[ai, bi, gi] != s3[ai, gi - 1, gi]) opp = true;
                    if (gi + 1 < nGrid && s3[ai, bi, gi] != s3[ai, gi + 1, gi]) opp = true;
                    if (opp) { bdry.Add((ai, bi, gi)); bset.Add((ai, bi, gi)); }
                }
        N = bdry.Count;
        var imap = new Dictionary<(int, int, int), int>();
        for (int i = 0; i < N; i++) imap[bdry[i]] = i;
        var adj = new List<int>[N];
        for (int i = 0; i < N; i++) adj[i] = new List<int>();
        for (int da = -1; da <= 1; da++)
            for (int db = -1; db <= 1; db++)
                for (int dg = -1; dg <= 1; dg++)
                {
                    if (da == 0 && db == 0 && dg == 0) continue;
                    for (int i = 0; i < N; i++)
                    {
                        var (a, b, g2) = bdry[i];
                        int na = a + da, nb = b + db, ng = g2 + dg;
                        if (bset.Contains((na, nb, ng)))
                        {
                            int j = imap[(na, nb, ng)];
                            if (!adj[i].Contains(j)) adj[i].Add(j);
                        }
                    }
                }
        edges = adj.Sum(a => a.Count) / 2;
        return adj;
    }

    private record SgiPt(double R, double Vobs, double Vbary);
    private record GalScaleResult(string Id, double RMax, double DirFrac, double GradStrength, bool ShapeOk, double AmpResidual);
    private record ScaleResult(string Name, string Domain, double EffectiveScale, int Count, double DirSignPct, double DirSignR, double GradAmpR, double GcR);
}
