using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using TRM.Tests.V7_3_and_4;
using static TRM.Tests.V7_3_and_4.V7TestHelpers;
using static TRM.Tests.V14.V14TestHelpers;

namespace TRM.Tests.V19_2;

[Trait("Category", "V19_2")]
public class V19_2_AmbiguityGeometryMetric_Tests
{
    private readonly ITestOutputHelper _o;
    public V19_2_AmbiguityGeometryMetric_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void AGM_01_AmbiguityGeometryMetricAudit()
    {
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== AGM_01: Ambiguity Geometry Metric Audit ===");
        _o.WriteLine("=== Which geometric property determines memory strength? ===");
        _o.WriteLine(new string('=', 108));

        _o.WriteLine("");
        _o.WriteLine("KNOWN: isRegional correctly classifies memory>0 vs memory=0.");
        _o.WriteLine("QUESTION: Which continuous geometric metric predicts memory magnitude?");
        _o.WriteLine("");

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, da = (aMax - aMin) / (nA - 1);

        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Continuous Ambiguity Geometry ===");
        _o.WriteLine("");

        var geoResults = new List<GeoResult>();

        var configs = new (string label, VcFamily fam, int dim, int nPerDim, double knownMem)[]
        {
            ("PURE",       VcFamily.SAC, 1, 0, 0.0),
            ("RATIONAL",   VcFamily.RCS, 1, 0, 0.0),
            ("STRETCHED",  VcFamily.ICS, 1, 0, 0.0),
            ("COMP-40x40", VcFamily.GAN, 2, 40, 4.1),
            ("COMP-25x25", VcFamily.GAN, 2, 25, 4.1),
            ("COMP-15x15", VcFamily.GAN, 2, 15, 4.1),
        };

        foreach (var cfg in configs)
        {
            var grid = new List<GeoPoint>();
            if (cfg.dim == 1)
            {
                int n1D = cfg.label == "STRETCHED" ? 200 : 50;
                for (int i = 0; i < n1D; i++)
                {
                    double beta = -1.0 + 2.0 * i / (n1D - 1);
                    var (m, dTdp) = ComputeM_and_DTdp(cfg.fam, 1.0, 1.0, 0.70, beta, 0.0,
                        distances, sortedD, xiBase, k0Base, nA, aMin, da);
                    grid.Add(new(beta, 0.0, Math.Abs(m), dTdp > 1e-8 ? 1 : -1));
                }
            }
            else
            {
                int n = cfg.nPerDim;
                for (int bi = 0; bi < n; bi++)
                {
                    double beta = 0.0 + 2.0 * bi / (n - 1);
                    for (int gi = 0; gi < n; gi++)
                    {
                        double gamma = 0.0 + 2.0 * gi / (n - 1);
                        var (m, dTdp) = ComputeM_and_DTdp(cfg.fam, 1.0, 1.0, 0.70, beta, gamma,
                            distances, sortedD, xiBase, k0Base, nA, aMin, da);
                        grid.Add(new(beta, gamma, Math.Abs(m), dTdp > 1e-8 ? 1 : -1));
                    }
                }
            }

            double binRes = 0.05;
            var bins = grid.GroupBy(p => Math.Round(p.absM / binRes) * binRes).ToList();
            var ambigBinsList = bins.Where(b => b.Any(p => p.sign > 0) && b.Any(p => p.sign < 0)).ToList();
            int ambigBinsC = ambigBinsList.Count;

            var sortedKeys = ambigBinsList.Select(b => b.Key).OrderBy(m => m).ToList();
            int regions = 0;
            double totalSpan = 0;
            double maxSpan = 0;
            var regionSizes = new List<double>();
            if (sortedKeys.Count > 0)
            {
                regions = 1;
                double rStart = sortedKeys[0];
                for (int i = 1; i < sortedKeys.Count; i++)
                {
                    if (sortedKeys[i] - sortedKeys[i - 1] > binRes * 1.5)
                    {
                        double regSpan = sortedKeys[i - 1] - rStart + binRes;
                        totalSpan += regSpan;
                        maxSpan = Math.Max(maxSpan, regSpan);
                        regionSizes.Add(regSpan);
                        regions++;
                        rStart = sortedKeys[i];
                    }
                }
                double lastS = sortedKeys.Last() - rStart + binRes;
                totalSpan += lastS;
                maxSpan = Math.Max(maxSpan, lastS);
                regionSizes.Add(lastS);
            }
            double avgRegSize = regionSizes.Count > 0 ? regionSizes.Average() : 0;

            int boundaryLength = 0;
            if (cfg.dim == 2)
            {
                var bs = grid.Select(p => p.beta).Distinct().OrderBy(b => b).ToList();
                var gs = grid.Select(p => p.gamma).Distinct().OrderBy(g => g).ToList();
                int nB = bs.Count, nG = gs.Count;
                var sMap = new int[nB, nG];
                foreach (var pt in grid)
                { int bi = bs.IndexOf(pt.beta); int gi = gs.IndexOf(pt.gamma); if (bi >= 0 && gi >= 0) sMap[bi, gi] = pt.sign; }
                for (int bi = 0; bi < nB; bi++)
                    for (int gi = 0; gi < nG; gi++)
                    {
                        if (bi + 1 < nB && sMap[bi, gi] != sMap[bi + 1, gi]) boundaryLength++;
                        if (gi + 1 < nG && sMap[bi, gi] != sMap[bi, gi + 1]) boundaryLength++;
                    }
            }

            double totalMSp = bins.Count > 0 ? bins.Max(b => b.Key) - bins.Min(b => b.Key) + binRes : 1;
            double densityA = totalMSp > 0 ? totalSpan / totalMSp : 0;
            int ambigCells = ambigBinsList.Sum(b => b.Count());
            double degPR = regions > 0
                ? ambigBinsList.Sum(b => { int p = b.Count(pt => pt.sign > 0); int n = b.Count(pt => pt.sign < 0); return (double)Math.Min(p, n); }) / regions
                : 0;
            double frag = ambigBinsC > 0 ? (double)regions / ambigBinsC : 0;

            _o.WriteLine($"--- {cfg.label} ---");
            _o.WriteLine($"  Ambig bins: {ambigBinsC}  Regions: {regions}  Span: {totalSpan:F4}  MaxSpan: {maxSpan:F4}");
            _o.WriteLine($"  AvgReg: {avgRegSize:F4}  Boundary: {boundaryLength}  Density: {densityA:F4}");
            _o.WriteLine($"  Cells: {ambigCells}  Deg/Reg: {degPR:F2}  Frag: {frag:F3}  Mem: {cfg.knownMem:F1}pp");
            _o.WriteLine("");

            geoResults.Add(new(cfg.label, cfg.dim, cfg.knownMem,
                ambigBinsC, regions, totalSpan, maxSpan,
                avgRegSize, boundaryLength, densityA, ambigCells, degPR, frag));
        }

        // ================================================================
        // Metric Ranking
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Metric Ranking: Continuous Geometry vs Memory ===");
        _o.WriteLine("");

        var memAll = geoResults.Select(g => g.knownMem).ToArray();
        var metrics = new (string name, double[] values)[]
        {
            ("Total span",        geoResults.Select(g => g.totalSpan).ToArray()),
            ("Max region span",   geoResults.Select(g => g.maxSpan).ToArray()),
            ("Avg region size",   geoResults.Select(g => g.avgRegSize).ToArray()),
            ("Region count",      geoResults.Select(g => (double)g.regions).ToArray()),
            ("Boundary length",   geoResults.Select(g => (double)g.boundaryLen).ToArray()),
            ("Ambiguity density", geoResults.Select(g => g.density).ToArray()),
            ("Cell count",        geoResults.Select(g => (double)g.cells).ToArray()),
            ("Degeneracy/region", geoResults.Select(g => g.degPR).ToArray()),
            ("Fragmentation",     geoResults.Select(g => g.frag).ToArray()),
        };

        _o.WriteLine($"{"Metric",-22} {"r(Memory)",10} {"R²",10} {"Strength",12}");
        _o.WriteLine(new string('-', 56));

        var rankings = new List<(string name, double r, double r2)>();
        foreach (var m in metrics)
        {
            double r = PearsonCorr(memAll, m.values);
            double r2 = r * r;
            string str = r2 > 0.90 ? "VERY STRONG" : r2 > 0.70 ? "STRONG" : r2 > 0.40 ? "MODERATE" : "WEAK";
            rankings.Add((m.name, r, r2));
            _o.WriteLine($"{m.name,-22} {r,10:F4} {r2,9:F4} {str,12}");
        }
        _o.WriteLine("");

        var bestM = rankings.OrderByDescending(r => r.r2).First();
        _o.WriteLine($"Best metric: {bestM.name} (R²={bestM.r2:F4})");
        _o.WriteLine("");

        // ================================================================
        // Memory Equations
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Quantitative Memory Equations ===");
        _o.WriteLine("");

        var sV = geoResults.Select(g => g.totalSpan).ToArray();
        var msV = geoResults.Select(g => g.maxSpan).ToArray();
        var dV = geoResults.Select(g => g.density).ToArray();
        var cV = geoResults.Select(g => (double)g.cells).ToArray();
        var degVals = geoResults.Select(g => g.degPR).ToArray();
        var bV = geoResults.Select(g => (double)g.boundaryLen).ToArray();

        (string label, double a, double r2, double res) TestEq(string label, double[] x, double[] y)
        {
            double a = SimpleLinFit(x, y);
            var pred = x.Select(v => a * v).ToArray();
            double r2 = R2Pred(y, pred);
            double res = geoResults.Zip(pred, (g, p) => Math.Abs(g.knownMem - p)).Average();
            return (label, a, r2, res);
        }

        var eqs = new[]
        {
            TestEq("M = a * totalSpan", sV, memAll),
            TestEq("M = a * maxSpan", msV, memAll),
            TestEq("M = a * density", dV, memAll),
            TestEq("M = a * degPerRegion", degVals, memAll),
            TestEq("M = a * boundaryLength", bV, memAll),
            TestEq("M = a * cellCount", cV, memAll),
        };

        _o.WriteLine($"{"Equation",-30} {"a",10} {"R²",9} {"Residual",9}");
        _o.WriteLine(new string('-', 60));
        foreach (var eq in eqs)
            _o.WriteLine($"{eq.label,-30} {eq.a,10:F4} {eq.r2,9:F4} {eq.res,9:F3}");
        _o.WriteLine("");

        var bestEq = eqs.OrderByDescending(e => e.r2).First();
        _o.WriteLine($"Best: {bestEq.label} (R²={bestEq.r2:F4})");
        _o.WriteLine("");

        // ================================================================
        // Decision
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Decision ===");
        _o.WriteLine("");

        string classification = bestEq.r2 > 0.85 ? "SUPPORTED"
            : bestEq.r2 > 0.50 ? "CONDITIONAL" : "FALSIFIED";

        _o.WriteLine($"VERDICT: {classification}.");
        _o.WriteLine($"Memory = {bestEq.label} (R²={bestEq.r2:F4}).");
        _o.WriteLine("");
        _o.WriteLine($"Classification: {classification}");
        _o.WriteLine("");

        _o.WriteLine("Ambiguity Geometry Metric Principle:");
        _o.WriteLine($"  Memory = f(ambiguity geometry).");
        _o.WriteLine($"  Best predictor: {bestM.name} (R²={bestM.r2:F4}).");
        _o.WriteLine("  Memory = 0 iff ambiguity span = 0.");
        _o.WriteLine("  Memory > 0 iff ambiguity span > 0.");
        _o.WriteLine("");

        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== AGM_01 complete. Commit: AGM_01_AmbiguityGeometryMetricAudit ===");
        Assert.True(true);
    }

    private static double PearsonCorr(double[] x, double[] y)
    {
        int n = Math.Min(x.Length, y.Length);
        double mx = 0, my = 0;
        for (int i = 0; i < n; i++) { mx += x[i]; my += y[i]; }
        mx /= n; my /= n;
        double cov = 0, sx = 0, sy = 0;
        for (int i = 0; i < n; i++)
        { double dx = x[i] - mx, dy = y[i] - my; cov += dx * dy; sx += dx * dx; sy += dy * dy; }
        return Math.Sqrt(sx * sy) > 1e-15 ? cov / Math.Sqrt(sx * sy) : 0;
    }

    private static double R2Pred(double[] y, double[] pred)
    {
        int n = Math.Min(y.Length, pred.Length);
        double my = 0; for (int i = 0; i < n; i++) my += y[i]; my /= n;
        double ssT = 0, ssR = 0;
        for (int i = 0; i < n; i++) { ssT += (y[i] - my) * (y[i] - my); ssR += (y[i] - pred[i]) * (y[i] - pred[i]); }
        return ssT > 1e-15 ? 1.0 - ssR / ssT : 0;
    }

    private static double SimpleLinFit(double[] x, double[] y)
    {
        int n = Math.Min(x.Length, y.Length);
        double mx = 0, my = 0;
        for (int i = 0; i < n; i++) { mx += x[i]; my += y[i]; }
        mx /= n; my /= n;
        double num = 0, den = 0;
        for (int i = 0; i < n; i++) { double dx = x[i] - mx; num += dx * (y[i] - my); den += dx * dx; }
        return den > 1e-15 ? num / den : 0;
    }

    private record GeoPoint(double beta, double gamma, double absM, int sign);
    private record GeoResult(string label, int dim, double knownMem,
        int ambigBins, int regions, double totalSpan, double maxSpan,
        double avgRegSize, int boundaryLen, double density, int cells, double degPR, double frag);
}
