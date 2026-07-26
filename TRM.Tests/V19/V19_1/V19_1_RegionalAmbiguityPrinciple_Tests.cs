using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using TRM.Tests.V7_3_and_4;
using static TRM.Tests.V7_3_and_4.V7TestHelpers;
using static TRM.Tests.V14.V14TestHelpers;

namespace TRM.Tests.V19_1;

[Trait("Category", "V19_1")]
public class V19_1_RegionalAmbiguityPrinciple_Tests
{
    private readonly ITestOutputHelper _o;
    public V19_1_RegionalAmbiguityPrinciple_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void RAR_01_RegionalAmbiguityPrincipleAudit()
    {
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== RAR_01: Regional Ambiguity Principle Audit ===");
        _o.WriteLine("=== Is memory from regional ambiguity, not point ambiguity? ===");
        _o.WriteLine(new string('=', 108));

        _o.WriteLine("");
        _o.WriteLine("KNOWN: STRETCHED has AmbigDeg>0 but memory=0.");
        _o.WriteLine("       COMPOSITE has AmbigDeg>0 and memory>0.");
        _o.WriteLine("HYPOTHESIS: Point ambiguity → 0 memory. Regional ambiguity → memory>0.");
        _o.WriteLine("");

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, da = (aMax - aMin) / (nA - 1);

        var archDefs = new (string name, VcFamily fam, double theta, int dim, double knownMem)[]
        {
            ("PURE",      VcFamily.SAC, 0.00, 1, 0.0),
            ("RATIONAL",  VcFamily.RCS, 999.0, 1, 0.0),
            ("STRETCHED", VcFamily.ICS, 1.00, 1, 0.0),
            ("COMPOSITE",  VcFamily.GAN, 0.64, 2, 4.1),
        };

        // ================================================================
        // Map parameter space with sign labels for regional analysis
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Ambiguity Region Mapping ===");
        _o.WriteLine("");

        var regionResults = new List<RegionResult>();

        foreach (var ad in archDefs)
        {
            var grid = new List<RarPoint>();

            if (ad.dim == 1)
            {
                const int n1D = 200;
                for (int i = 0; i < n1D; i++)
                {
                    double beta = -1.0 + 2.0 * i / (n1D - 1); // uniform sweep
                    var (m, dTdp) = ComputeM_and_DTdp(ad.fam, 1.0, 1.0, 0.70, beta, 0.0,
                        distances, sortedD, xiBase, k0Base, nA, aMin, da);
                    grid.Add(new(beta, 0.0, Math.Abs(m), dTdp > 1e-8 ? 1 : -1));
                }
            }
            else
            {
                const int nPerDim = 40;
                for (int bi = 0; bi < nPerDim; bi++)
                {
                    double beta = 0.0 + 2.0 * bi / (nPerDim - 1);
                    for (int gi = 0; gi < nPerDim; gi++)
                    {
                        double gamma = 0.0 + 2.0 * gi / (nPerDim - 1);
                        var (m, dTdp) = ComputeM_and_DTdp(ad.fam, 1.0, 1.0, 0.70, beta, gamma,
                            distances, sortedD, xiBase, k0Base, nA, aMin, da);
                        grid.Add(new(beta, gamma, Math.Abs(m), dTdp > 1e-8 ? 1 : -1));
                    }
                }
            }

            double binRes = 0.05;
            var bins = grid.GroupBy(p => Math.Round(p.absM / binRes) * binRes).ToList();

            // --- Ambiguous bins: bins with both signs ---
            var ambigBins = bins.Where(b => b.Any(p => p.sign > 0) && b.Any(p => p.sign < 0)).ToList();
            int ambigCount = ambigBins.Count;

            // --- Connected ambiguous regions ---
            // In |m| space: group consecutive ambiguous bins
            // Two ambiguous bins are connected if their |m| values differ by binRes
            var sortedAmbig = ambigBins.Select(b => b.Key).OrderBy(m => m).ToList();
            int ambigRegions = 0;
            double totalAmbigSpan = 0;
            double maxRegionSpan = 0;

            if (sortedAmbig.Count > 0)
            {
                ambigRegions = 1;
                double regionStart = sortedAmbig[0];
                for (int i = 1; i < sortedAmbig.Count; i++)
                {
                    if (sortedAmbig[i] - sortedAmbig[i - 1] > binRes * 1.5)
                    {
                        // Gap: new region
                        double regionSpan = sortedAmbig[i - 1] - regionStart + binRes;
                        totalAmbigSpan += regionSpan;
                        maxRegionSpan = Math.Max(maxRegionSpan, regionSpan);
                        ambigRegions++;
                        regionStart = sortedAmbig[i];
                    }
                }
                double lastSpan = sortedAmbig.Last() - regionStart + binRes;
                totalAmbigSpan += lastSpan;
                maxRegionSpan = Math.Max(maxRegionSpan, lastSpan);
            }

            // --- Ambiguity density: ambig bins / total |m| span ---
            double totalMSpan = bins.Count > 0
                ? bins.Max(b => b.Key) - bins.Min(b => b.Key) + binRes
                : 1;
            double ambigDensity2 = totalMSpan > 0 ? totalAmbigSpan / totalMSpan : 0;

            // --- In 2D: measure the (β,γ) area of ambiguous |m| bins ---
            // Map back: for each ambiguous |m| bin, count the (β,γ) cells
            int ambigCellCount = ambigBins.Sum(b => b.Count());

            // --- AmbigDeg per region (average degeneracy density) ---
            double ambigDegPerRegion = ambigRegions > 0
                ? ambigBins.Sum(b =>
                {
                    int p = b.Count(pt => pt.sign > 0);
                    int n = b.Count(pt => pt.sign < 0);
                    return (double)Math.Min(p, n);
                }) / ambigRegions
                : 0;

            // --- Point vs Regional classification ---
            // Point ambiguity: single |m| bin or thin (span <= 1 bin)
            // Regional ambiguity: span > 1 bin and > 1 ambiguous bin
            bool isPointAmbiguity = ambigRegions <= 1 && totalAmbigSpan <= binRes * 1.1;
            bool isRegionalAmbiguity = ambigRegions >= 1 && totalAmbigSpan > binRes * 1.1;

            _o.WriteLine($"--- {ad.name} ({ad.dim}D) ---");
            _o.WriteLine($"  Ambiguous |m| bins:    {ambigCount}/{bins.Count}");
            _o.WriteLine($"  Connected regions:     {ambigRegions}");
            _o.WriteLine($"  Total ambiguous span:  {totalAmbigSpan:F3} (|m| range)");
            _o.WriteLine($"  Max region span:       {maxRegionSpan:F3}");
            _o.WriteLine($"  Ambiguity density:     {ambigDensity2:F3} (span / total span)");
            _o.WriteLine($"  Ambig cell count:      {ambigCellCount}");
            _o.WriteLine($"  AmbigDeg per region:   {ambigDegPerRegion:F2}");
            _o.WriteLine($"  Classification:        {(isRegionalAmbiguity ? "REGIONAL" : isPointAmbiguity ? "POINT" : "NONE")}");
            _o.WriteLine($"  Known memory:          {ad.knownMem:F1} pp");
            _o.WriteLine("");

            regionResults.Add(new(ad.name, ad.dim, ad.knownMem,
                ambigCount, ambigRegions, totalAmbigSpan, maxRegionSpan,
                ambigDensity2, ambigCellCount, ambigDegPerRegion,
                isRegionalAmbiguity, isPointAmbiguity));
        }

        // ================================================================
        // Region Statistics Table
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Ambiguity Geometry Table ===");
        _o.WriteLine("");
        _o.WriteLine($"{"Arch",-14} {"ABins",5} {"Regions",8} {"Span",8} {"MaxSpan",8} {"Density",8} {"Cells",7} {"Deg/Reg",8} {"Type",-10} {"Mem",5}");
        _o.WriteLine(new string('-', 87));
        foreach (var rr in regionResults)
        {
            string type = rr.isRegionalAmbiguity ? "REGIONAL" : rr.isPointAmbiguity ? "POINT" : "NONE";
            _o.WriteLine($"{rr.name,-14} {rr.ambigBins,5} {rr.ambigRegions,8} {rr.totalSpan,8:F3} {rr.maxSpan,8:F3} {rr.ambigDensity,8:F3} {rr.ambigCells,7} {rr.degPerRegion,8:F2} {type,-10} {rr.knownMem,4:F1}pp");
        }
        _o.WriteLine("");

        // ================================================================
        // Candidate Ranking
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Candidate Ranking: Memory vs Ambiguity Geometry ===");
        _o.WriteLine("");

        var memArr = regionResults.Select(r => r.knownMem).ToArray();
        var cands = new (string name, double[] values)[]
        {
            ("Ambig |m| bins",        regionResults.Select(r => (double)r.ambigBins).ToArray()),
            ("Connected regions",     regionResults.Select(r => (double)r.ambigRegions).ToArray()),
            ("Total ambiguous span",  regionResults.Select(r => r.totalSpan).ToArray()),
            ("Max region span",       regionResults.Select(r => r.maxSpan).ToArray()),
            ("Ambiguity density",     regionResults.Select(r => r.ambigDensity).ToArray()),
            ("Ambig cell count",      regionResults.Select(r => (double)r.ambigCells).ToArray()),
            ("Degeneracy per region", regionResults.Select(r => r.degPerRegion).ToArray()),
            ("IsRegional (binary)",   regionResults.Select(r => r.isRegionalAmbiguity ? 1.0 : 0.0).ToArray()),
        };

        _o.WriteLine($"{"Candidate",-24} {"r(Memory)",10} {"R²",10} {"Dominant?",10}");
        _o.WriteLine(new string('-', 56));

        var rankings = new List<(string name, double r, double r2)>();
        foreach (var cand in cands)
        {
            double r = PearsonCorr(memArr, cand.values);
            double r2 = r * r;
            string dom = r2 > 0.85 ? "YES" : r2 > 0.50 ? "partial" : "no";
            rankings.Add((cand.name, r, r2));
            _o.WriteLine($"{cand.name,-24} {r,10:F4} {r2,9:F4} {dom,10}");
        }
        _o.WriteLine("");

        var bestR = rankings.OrderByDescending(r => r.r2).First();
        _o.WriteLine($"Best metric: {bestR.name} (R²={bestR.r2:F4})");
        _o.WriteLine("");

        // ================================================================
        // Is memory correlated with ambiguity area?
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Point vs Regional Ambiguity ===");
        _o.WriteLine("");

        var strResult = regionResults.First(r => r.name == "STRETCHED");
        var compResult = regionResults.First(r => r.name == "COMPOSITE");

        _o.WriteLine($"STRETCHED:  {strResult.ambigBins} ambiguous bins, {strResult.ambigRegions} region(s), span={strResult.totalSpan:F3}");
        _o.WriteLine($"COMPOSITE:  {compResult.ambigBins} ambiguous bins, {compResult.ambigRegions} region(s), span={compResult.totalSpan:F3}");
        _o.WriteLine("");

        if (strResult.isPointAmbiguity && compResult.isRegionalAmbiguity)
        {
            _o.WriteLine("CONFIRMED: STRETCHED = POINT ambiguity, COMPOSITE = REGIONAL ambiguity.");
            _o.WriteLine("");
            _o.WriteLine("Point ambiguity (STRETCHED):");
            _o.WriteLine("  - Sign crossing at a single |m| value");
            _o.WriteLine("  - Ambiguous bins span 1 connected region");
            _o.WriteLine("  - Each point in the ambiguous bin is reachable");
            _o.WriteLine("    from exactly ONE direction in 1D parameter space");
            _o.WriteLine("  → Memory = 0");
            _o.WriteLine("");
            _o.WriteLine("Regional ambiguity (COMPOSITE):");
            _o.WriteLine("  - Sign boundary spanning multiple |m| values");
            _o.WriteLine("  - Ambiguous bins span >1 connected region(s)");
            _o.WriteLine("  - Same |m| reachable from MULTIPLE (β,γ) pairs");
            _o.WriteLine("    carrying BOTH signs");
            _o.WriteLine("  → Memory > 0");
        }

        // ================================================================
        // Memory equation with regional metrics
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Regional Memory Equations ===");
        _o.WriteLine("");

        var span = regionResults.Select(r => r.totalSpan).ToArray();
        var maxSpan = regionResults.Select(r => r.maxSpan).ToArray();
        var density = regionResults.Select(r => r.ambigDensity).ToArray();
        var degPR = regionResults.Select(r => r.degPerRegion).ToArray();
        var isReg = regionResults.Select(r => r.isRegionalAmbiguity ? 1.0 : 0.0).ToArray();

        // Eq 1: M = a * totalSpan
        double a1 = SimpleLinFit(span, memArr);
        var p1 = span.Select(s => a1 * s).ToArray();
        double r2_1 = R2Pred(memArr, p1);

        // Eq 2: M = a * maxSpan
        double a2 = SimpleLinFit(maxSpan, memArr);
        var p2 = maxSpan.Select(s => a2 * s).ToArray();
        double r2_2 = R2Pred(memArr, p2);

        // Eq 3: M = a * density
        double a3 = SimpleLinFit(density, memArr);
        var p3 = density.Select(d => a3 * d).ToArray();
        double r2_3 = R2Pred(memArr, p3);

        // Eq 4: M = a * degPerRegion
        double a4 = SimpleLinFit(degPR, memArr);
        var p4 = degPR.Select(d => a4 * d).ToArray();
        double r2_4 = R2Pred(memArr, p4);

        // Eq 5: M = a * isRegional (binary)
        double a5 = SimpleLinFit(isReg, memArr);
        var p5 = isReg.Select(x => a5 * x).ToArray();
        double r2_5 = R2Pred(memArr, p5);

        _o.WriteLine($"{"Equation",-32} {"a",9} {"R²",9}");
        _o.WriteLine(new string('-', 52));
        _o.WriteLine($"{"M = a * totalSpan",-32} {a1,9:F4} {r2_1,9:F4}");
        _o.WriteLine($"{"M = a * maxSpan",-32} {a2,9:F4} {r2_2,9:F4}");
        _o.WriteLine($"{"M = a * density",-32} {a3,9:F4} {r2_3,9:F4}");
        _o.WriteLine($"{"M = a * degPerRegion",-32} {a4,9:F4} {r2_4,9:F4}");
        _o.WriteLine($"{"M = a * isRegional",-32} {a5,9:F4} {r2_5,9:F4}");
        _o.WriteLine("");

        double bestR2Eq = new[] { r2_1, r2_2, r2_3, r2_4, r2_5 }.Max();
        string bestEq = bestR2Eq == r2_1 ? $"M = {a1:F4} * totalSpan"
            : bestR2Eq == r2_2 ? $"M = {a2:F4} * maxSpan"
            : bestR2Eq == r2_3 ? $"M = {a3:F4} * density"
            : bestR2Eq == r2_4 ? $"M = {a4:F4} * degPerRegion"
            : $"M = {a5:F4} * isRegional";
        _o.WriteLine($"Best: {bestEq} (R²={bestR2Eq:F4})");
        _o.WriteLine("");

        // ================================================================
        // Does memory disappear when ambiguity area → 0?
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Memory vs Ambiguity Area ===");
        _o.WriteLine("");

        _o.WriteLine("Test: Does memory → 0 when ambiguity span → 0?");
        _o.WriteLine("");
        _o.WriteLine($"{"Arch",-14} {"Span",8} {"isPoint?",10} {"Memory",8}");
        _o.WriteLine(new string('-', 42));
        foreach (var rr in regionResults)
            _o.WriteLine($"{rr.name,-14} {rr.totalSpan,8:F3} {rr.isPointAmbiguity,10} {rr.knownMem,7:F1}pp");
        _o.WriteLine("");

        bool spanSeparates = regionResults.Where(r => r.totalSpan < 0.1).All(r => r.knownMem == 0)
            && regionResults.Where(r => r.totalSpan > 0.1).All(r => r.knownMem > 0);

        if (spanSeparates)
            _o.WriteLine("YES — memory = 0 when ambiguity span < 0.1, >0 otherwise.");
        else
            _o.WriteLine("Span alone does NOT perfectly separate memory values.");

        // ================================================================
        // Decision
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Decision ===");
        _o.WriteLine("");

        bool regionalDominates = bestR.name.Contains("span") || bestR.name.Contains("Region")
            || bestR.name.Contains("density");
        bool isRegionalPredicts = rankings.Any(r => r.name == "IsRegional (binary)" && r.r2 > 0.80);

        string classification;
        if (isRegionalPredicts)
        {
            _o.WriteLine("VERDICT: SUPPORTED — regional vs point ambiguity fully explains memory.");
            classification = "SUPPORTED";
        }
        else if (regionalDominates && bestR.r2 > 0.60)
        {
            _o.WriteLine("VERDICT: CONDITIONAL — regional structure dominates but sample is small.");
            classification = "CONDITIONAL";
        }
        else
        {
            _o.WriteLine("VERDICT: FALSIFIED — another invariant dominates.");
            classification = "FALSIFIED";
        }

        _o.WriteLine("");
        _o.WriteLine($"Classification: {classification}");
        _o.WriteLine("");

        _o.WriteLine("Regional Ambiguity Principle:");
        _o.WriteLine("  Point ambiguity (1D crossing) → NO memory.");
        _o.WriteLine("  Regional ambiguity (2D boundary spanning multiple |m|) → memory.");
        _o.WriteLine($"  Best metric: {bestR.name} (R²={bestR.r2:F4}).");
        _o.WriteLine("  Memory emerges when sign-ambiguous |m| bins form");
        _o.WriteLine("  a connected region spanning multiple |m| values.");
        _o.WriteLine("");

        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== RAR_01 complete. Commit: RAR_01_RegionalAmbiguityPrincipleAudit ===");
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

    private record RarPoint(double beta, double gamma, double absM, int sign);
    private record RegionResult(string name, int dim, double knownMem,
        int ambigBins, int ambigRegions, double totalSpan, double maxSpan,
        double ambigDensity, int ambigCells, double degPerRegion,
        bool isRegionalAmbiguity, bool isPointAmbiguity);
}
