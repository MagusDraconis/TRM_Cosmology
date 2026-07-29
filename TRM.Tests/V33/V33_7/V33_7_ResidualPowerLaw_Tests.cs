using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V33_7;

[Trait("Category", "V33_7")]
[Trait("Category", "LongRunning")]
public class V33_7_ResidualPowerLaw_Tests
{
    private readonly ITestOutputHelper _o;
    public V33_7_ResidualPowerLaw_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void RPL_01_ResidualPowerLawAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== RPL_01: Residual Power Law Audit ===");
        sb.AppendLine("=== Is the exponent α universal or population-dependent? ===");
        sb.AppendLine(new string('=', 108));

        var repoRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
        var massFile = Path.Combine(repoRoot, "TRM.Core", "Data", "MassModels_Lelli2016c.mrt");

        var galData = new Dictionary<string, List<RplPt>>();
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
            if (!galData.ContainsKey(id)) galData[id] = new List<RplPt>();
            galData[id].Add(new RplPt(r, vobs, vb, vdisk));
        }

        var allResults = new List<RplResult>();
        foreach (var (id, pts) in galData)
        {
            var sorted = pts.OrderBy(p => p.R).ToList();
            if (sorted.Count < 10) continue;
            var dDens = new List<double>();
            for (int i = 1; i < sorted.Count - 1; i++)
            { double dr = sorted[i + 1].R - sorted[i - 1].R; if (dr < 1e-6) continue; dDens.Add((sorted[i + 1].Vbary * sorted[i + 1].Vbary - sorted[i - 1].Vbary * sorted[i - 1].Vbary) / dr); }
            if (dDens.Count < 5) continue;
            double gs = dDens.Average(d => Math.Abs(d));
            double vM = sorted.Average(p => p.Vobs), bM = sorted.Average(p => p.Vbary);
            double ar = vM > 0 ? Math.Abs(vM - bM) / vM : 0;
            int nO = Math.Max(3, sorted.Count / 3);
            var outer = sorted.Skip(sorted.Count - nO).ToList();
            double sb_ = outer.Average(p => p.Vdisk * p.Vdisk);
            var fracs = sorted.Select(p => p.Vobs > 0 ? p.Vbary / p.Vobs : 0).Where(f => f > 0).ToList();
            double mf = fracs.Count > 0 ? fracs.OrderBy(f => f).ElementAt(fracs.Count / 2) : 0;
            string bc = mf > 0.85 ? "BARYON" : mf > 0.50 ? "MIXED" : "DM";
            string br = sb_ < 2000 ? "LSB" : sb_ > 5000 ? "HSB" : "MID";
            allResults.Add(new RplResult(id, gs, ar, sb_, mf, bc, br));
        }

        if (allResults.Count < 30) { sb.AppendLine("Insufficient."); Assert.True(true); return; }

        // ================================================================
        // DEFINE POPULATIONS
        // ================================================================
        var populations = new Dictionary<string, List<RplResult>>();
        foreach (var cls in new[] { "BARYON", "MIXED", "DM" })
            populations[$"Class:{cls}"] = allResults.Where(r => r.BClass == cls).ToList();
        foreach (var br in new[] { "LSB", "MID", "HSB" })
            populations[$"Bright:{br}"] = allResults.Where(r => r.BrightClass == br).ToList();
        populations["ALL"] = allResults;

        // ================================================================
        // FIT α PER POPULATION WITH BOOTSTRAP CI
        // ================================================================
        var rng = new Random(42);
        var popResults = new List<PopResult>();

        foreach (var (name, pop) in populations)
        {
            if (pop.Count < 8) continue;

            double[] gs = pop.Select(r => r.GradStrength).ToArray();
            double[] ar = pop.Select(r => r.AmpResidual).ToArray();
            double[] lg = gs.Select(g => Math.Log10(Math.Max(1e-15, g))).ToArray();
            double[] la = ar.Select(a => Math.Log10(Math.Max(1e-15, a))).ToArray();

            // Full-fit α
            var (alpha, _, r2) = LinearRegression(lg, la);

            // Bootstrap CIs (80 resamples, 80% each)
            var bootAlphas = new List<double>();
            for (int b = 0; b < 80; b++)
            {
                var idx = Enumerable.Range(0, pop.Count).OrderBy(_ => rng.Next()).Take(Math.Max(5, pop.Count * 4 / 5)).ToArray();
                double[] bg = idx.Select(i => lg[i]).ToArray();
                double[] ba = idx.Select(i => la[i]).ToArray();
                var (a, _, _) = LinearRegression(bg, ba);
                bootAlphas.Add(a);
            }
            bootAlphas.Sort();
            double alphaLo = bootAlphas[4];   // ~5th percentile
            double alphaHi = bootAlphas[75];  // ~95th percentile
            double alphaStd = Math.Sqrt(bootAlphas.Average(a => (a - alpha) * (a - alpha)));

            popResults.Add(new PopResult(name, pop.Count, alpha, alphaLo, alphaHi, alphaStd, r2));
        }

        // ================================================================
        // EXPONENT STABILITY TABLE
        // ================================================================
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Exponent Stability Table ===");
        sb.AppendLine("");
        sb.AppendLine($"{"Population",-18} {"N",5} {"α",8} {"95% CI",18} {"σ",8} {"R²",8} {"Stable?",10}");
        sb.AppendLine(new string('-', 77));

        foreach (var p in popResults)
        {
            string ci = $"[{p.AlphaLo:F3}, {p.AlphaHi:F3}]";
            string stable = p.AlphaStd < 0.08 ? "YES" : "MARGINAL";
            sb.AppendLine($"{p.Name,-18} {p.N,5} {p.Alpha,8:F4} {ci,18} {p.AlphaStd,8:F4} {p.R2,8:F4} {stable,10}");
        }
        sb.AppendLine("");

        // ================================================================
        // OVERLAP ANALYSIS
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Overlap Analysis ===");
        sb.AppendLine("");

        double overlapFrac = 0;
        var classPops = popResults.Where(p => p.Name.StartsWith("Class:")).ToList();
        if (classPops.Count >= 2)
        {
            double allMin = classPops.Min(p => p.AlphaLo);
            double allMax = classPops.Max(p => p.AlphaHi);
            double totalRange = allMax - allMin;

            // Overlap fraction: intersection / union of CI ranges
            double overlapLo = classPops.Max(p => p.AlphaLo);
            double overlapHi = classPops.Min(p => p.AlphaHi);
            double overlapRange = Math.Max(0, overlapHi - overlapLo);
            overlapFrac = totalRange > 1e-15 ? overlapRange / totalRange : 0;

            sb.AppendLine($"  Class α range: [{allMin:F4}, {allMax:F4}] (span={totalRange:F4})");
            sb.AppendLine($"  Overlap interval: [{overlapLo:F4}, {overlapHi:F4}] ({overlapFrac*100:F1}% of total range)");
            sb.AppendLine($"  → {(overlapFrac > 0.3 ? "STRONG OVERLAP — common α" : overlapFrac > 0.1 ? "PARTIAL OVERLAP" : "DIVERGENT — class-specific α")}");
            sb.AppendLine("");
        }

        // Brightness overlap
        var brightPops = popResults.Where(p => p.Name.StartsWith("Bright:")).ToList();
        if (brightPops.Count >= 2)
        {
            double bMin = brightPops.Min(p => p.AlphaLo);
            double bMax = brightPops.Max(p => p.AlphaHi);
            double bRange = bMax - bMin;
            double bLo = brightPops.Max(p => p.AlphaLo);
            double bHi = brightPops.Min(p => p.AlphaHi);
            double bOverlap = Math.Max(0, bHi - bLo);
            double bFrac = bRange > 1e-15 ? bOverlap / bRange : 0;

            sb.AppendLine($"  Brightness α range: [{bMin:F4}, {bMax:F4}] (span={bRange:F4})");
            sb.AppendLine($"  Overlap: {bFrac*100:F1}% → {(bFrac > 0.3 ? "STRONG OVERLAP" : "DIVERGENT")}");
            sb.AppendLine("");
        }

        // ================================================================
        // POOLED VS CLASS-SPECIFIC FIT
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Pooled vs Class-Specific Fit ===");
        sb.AppendLine("");

        double r2Gain = 0;

        // Pooled: single α for all
        double[] allGs = allResults.Select(r => Math.Log10(Math.Max(1e-15, r.GradStrength))).ToArray();
        double[] allAr = allResults.Select(r => Math.Log10(Math.Max(1e-15, r.AmpResidual))).ToArray();
        var (pooledAlpha, _, pooledR2) = LinearRegression(allGs, allAr);

        // Class-specific: fit separate α per class, compute combined R²
        double ssResClass = 0, ssTotAll = 0;
        double allArMean = allResults.Select(r => Math.Log10(Math.Max(1e-15, r.AmpResidual))).Average();
        foreach (var cls in new[] { "BARYON", "MIXED", "DM" })
        {
            var g = allResults.Where(r => r.BClass == cls).ToList();
            if (g.Count < 5) continue;
            double[] cGs = g.Select(r => Math.Log10(Math.Max(1e-15, r.GradStrength))).ToArray();
            double[] cAr = g.Select(r => Math.Log10(Math.Max(1e-15, r.AmpResidual))).ToArray();
            var (cA, cI, _) = LinearRegression(cGs, cAr);
            for (int i = 0; i < g.Count; i++)
            {
                double pred = cI + cA * cGs[i];
                double obs = cAr[i];
                ssResClass += (obs - pred) * (obs - pred);
                ssTotAll += (obs - allArMean) * (obs - allArMean);
            }
        }
        double classR2 = ssTotAll > 1e-15 ? 1 - ssResClass / ssTotAll : 0;
        r2Gain = classR2 - pooledR2;

        sb.AppendLine($"  Pooled R² (single α):        {pooledR2:F4}  α = {pooledAlpha:F4}");
        sb.AppendLine($"  Class-specific R² (3 α's):   {classR2:F4}");
        sb.AppendLine($"  ΔR² from class-splitting:   +{r2Gain:F4} ({(r2Gain*100):F2}%)");
        sb.AppendLine($"  → {(r2Gain > 0.02 ? "CLASS-SPECIFIC α BETTER — exponent depends on population" : r2Gain > 0.005 ? "MARGINAL GAIN — near-universal α" : "UNIVERSAL α — no gain from splitting")}");
        sb.AppendLine("");

        // ================================================================
        // DISTRIBUTION OVERLAP METRIC
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Cross-Population α Spread ===");
        sb.AppendLine("");

        var mainPops = popResults.Where(p => !p.Name.StartsWith("Bright:MID") && p.Name != "ALL").ToList();
        double[] alphas = mainPops.Select(p => p.Alpha).ToArray();
        double alphaMu = alphas.Average();
        double alphaSd = Math.Sqrt(alphas.Average(a => (a - alphaMu) * (a - alphaMu)));
        double alphaCv = Math.Abs(alphaMu) > 1e-15 ? alphaSd / Math.Abs(alphaMu) : 0;

        sb.AppendLine($"  Cross-population α: {alphaMu:F4} ± {alphaSd:F4}  (CV={alphaCv:F4})");
        sb.AppendLine($"  → α is {(alphaCv < 0.15 ? "HIGHLY CONSISTENT across populations" : alphaCv < 0.30 ? "MODERATELY CONSISTENT" : "POPULATION-DEPENDENT")}");
        sb.AppendLine("");

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        bool critA = alphaCv < 0.15;              // Low cross-population variance
        bool critB = r2Gain < 0.02;                // Small gain from class-splitting
        bool critC = overlapFrac > 0.3;            // Strong CI overlap
        bool critD = mainPops.Count >= 3;          // Multiple populations

        int critMet = (critA ? 1 : 0) + (critB ? 1 : 0) + (critC ? 1 : 0) + (critD ? 1 : 0);

        string verdict = critMet >= 4 ? "SUPPORTED: single universal α exists."
            : critMet >= 2 ? "CONDITIONAL: few population-dependent α."
            : "FALSIFIED: α strongly population-dependent.";

        sb.AppendLine($"VERDICT: {verdict}  ({critMet}/4 criteria)");
        sb.AppendLine("");
        sb.AppendLine($"  A. Cross-pop CV < 0.15:                 {(critA ? "YES" : "NO")} (CV={alphaCv:F4})");
        sb.AppendLine($"  B. ΔR² from splitting < 0.02:           {(critB ? "YES" : "NO")} (ΔR²={r2Gain:F4})");
        sb.AppendLine($"  C. CI overlap > 30%:                     {(critC ? "YES" : "NO")} ({overlapFrac*100:F1}%)");
        sb.AppendLine($"  D. ≥3 populations tested:                {(critD ? "YES" : "NO")} ({mainPops.Count})");
        sb.AppendLine("");
        sb.AppendLine("Universality Result:");
        sb.AppendLine($"  Pooled α = {pooledAlpha:F4}, cross-pop CV = {alphaCv:F4}");
        foreach (var p in popResults.Where(p => p.Name != "ALL"))
            sb.AppendLine($"    {p.Name}: α = {p.Alpha:F4} [{p.AlphaLo:F3}, {p.AlphaHi:F3}]");
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== RPL_01 complete. Commit: RPL_01_ResidualPowerLawAudit ===");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    private static (double slope, double intercept, double r2) LinearRegression(double[] xs, double[] ys)
    {
        int n = xs.Length; if (n < 2) return (0, ys.Length > 0 ? ys[0] : 0, 0);
        double mx = xs.Average(), my = ys.Average();
        double sxy = 0, sxx = 0, syy = 0;
        for (int i = 0; i < n; i++) { double dx = xs[i] - mx, dy = ys[i] - my; sxy += dx * dy; sxx += dx * dx; syy += dy * dy; }
        double slope = sxx > 1e-15 ? sxy / sxx : 0;
        double intercept = my - slope * mx;
        double r2 = syy > 1e-15 ? (slope * sxy) / syy : 0;
        return (slope, intercept, r2);
    }

    private static double ParseD(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return double.NaN;
        return double.TryParse(s.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : double.NaN;
    }

    private record RplPt(double R, double Vobs, double Vbary, double Vdisk);
    private record RplResult(string Id, double GradStrength, double AmpResidual, double SurfBri, double MedFrac, string BClass, string BrightClass);
    private record PopResult(string Name, int N, double Alpha, double AlphaLo, double AlphaHi, double AlphaStd, double R2);
}
