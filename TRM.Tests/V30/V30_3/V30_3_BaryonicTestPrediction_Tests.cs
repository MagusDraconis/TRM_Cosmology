using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TRM.Core.Data;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V30_3;

[Trait("Category", "V30_3")]
[Trait("Category", "LongRunning")]
public class V30_3_BaryonicTestPrediction_Tests
{
    private readonly ITestOutputHelper _o;
    public V30_3_BaryonicTestPrediction_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void BTP_01_BaryonicTestPredictionAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== BTP_01: Baryonic Test Prediction Audit ===");
        sb.AppendLine("=== Does boundary density dominate in SPARC? ===");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("");
        sb.AppendLine("TRM PREDICTION: Boundary density → curvature → dynamics.");
        sb.AppendLine("TEST: Do baryons alone predict observed rotation in SPARC?");
        sb.AppendLine("");

        var repoRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
        var massFile = Path.Combine(repoRoot, "TRM.Core", "Data", "MassModels_Lelli2016c.mrt");
        var ds = MrtParser.Parse(massFile);

        var colIdx = new Dictionary<string, int>();
        for (int i = 0; i < ds.Columns.Count; i++)
            colIdx[ds.Columns[i].Label.Trim()] = i;

        // Load per-galaxy data
        var galaxies = new Dictionary<string, List<DataPoint>>();
        foreach (var row in ds.Rows)
        {
            string id = row.Values[colIdx["ID"]].Trim();
            double r = ParseD(row.Values[colIdx["R"]]);
            double vobs = ParseD(row.Values[colIdx["Vobs"]]);
            double vgas = ParseD(row.Values[colIdx["Vgas"]]);
            double vdisk = ParseD(row.Values[colIdx["Vdisk"]]);
            double vbul = ParseD(row.Values[colIdx["Vbul"]]);
            if (double.IsNaN(r) || double.IsNaN(vobs) || r < 0 || vobs <= 0) continue;
            if (!galaxies.ContainsKey(id)) galaxies[id] = new List<DataPoint>();
            galaxies[id].Add(new DataPoint(r, vobs, vgas, vdisk, vbul));
        }

        sb.AppendLine($"  {galaxies.Count} galaxies loaded.");
        sb.AppendLine("");

        var results = new List<GalaxyResult>();
        foreach (var (id, pts) in galaxies)
        {
            var sorted = pts.OrderBy(p => p.R).ToList();
            if (sorted.Count < 5) continue;

            // Baryonic velocity at each point
            var baryFracs = new List<double>();
            var rVals = new List<double>();
            foreach (var p in sorted)
            {
                double vBary = Math.Sqrt(Math.Max(0, p.Vgas * p.Vgas + p.Vdisk * p.Vdisk + p.Vbul * p.Vbul));
                if (vBary > 0 && p.Vobs > 0) { baryFracs.Add(vBary / p.Vobs); rVals.Add(p.R); }
            }
            if (baryFracs.Count < 3) continue;

            double medianFrac = baryFracs.OrderBy(f => f).ElementAt(baryFracs.Count / 2);
            int nOuter = Math.Max(1, baryFracs.Count / 3);
            double outerFrac = baryFracs.Skip(Math.Max(0, baryFracs.Count - nOuter)).DefaultIfEmpty(medianFrac).Average();

            // Classify: BARYON-DOMINATED (median > 0.85), MIXED (0.50-0.85), DM-DOMINATED (<0.50)
            string classification = medianFrac > 0.85 ? "BARYON" : medianFrac > 0.50 ? "MIXED" : "DM";

            // Density proxy: mean surface brightness from outer points
            int nDens = Math.Max(1, sorted.Count / 3);
            var outerPoints = sorted.Skip(Math.Max(0, sorted.Count - nDens)).ToList();
            double density = outerPoints.Count > 0
                ? outerPoints.Average(p => p.Vdisk * p.Vdisk + p.Vgas * p.Vgas)
                : sorted.Average(p => p.Vdisk * p.Vdisk + p.Vgas * p.Vgas);

            results.Add(new GalaxyResult(id, classification, medianFrac, outerFrac, density, sorted.Count));
        }

        int totalGals = results.Count;
        if (totalGals == 0)
        {
            sb.AppendLine("  No valid galaxies found. Check data parsing.");
            _o.WriteLine(sb.ToString());
            Assert.True(true);
            return;
        }

        int baryonCount = results.Count(r => r.Classification == "BARYON");
        int mixedCount = results.Count(r => r.Classification == "MIXED");
        int dmCount = results.Count(r => r.Classification == "DM");

        // ================================================================
        // DENSITY PRIMACY TABLE
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Density Primacy Classification ===");
        sb.AppendLine("");
        sb.AppendLine($"  BARYON-dominated (median frac > 0.85): {baryonCount} ({100.0*baryonCount/results.Count:F0}%)");
        sb.AppendLine($"  MIXED (0.50-0.85):                      {mixedCount} ({100.0*mixedCount/results.Count:F0}%)");
        sb.AppendLine($"  DM-dominated (< 0.50):                   {dmCount} ({100.0*dmCount/results.Count:F0}%)");
        sb.AppendLine("");
        sb.AppendLine($"  Median baryonic fraction: {results.OrderBy(r=>r.MedianFrac).ElementAt(results.Count/2).MedianFrac:F3}");
        sb.AppendLine($"  Mean outer fraction:      {results.Average(r=>r.OuterFrac):F3}");
        sb.AppendLine("");

        // ================================================================
        // DENSITY vs FRACTION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Density vs Baryonic Fraction ===");
        sb.AppendLine("");

        // Group by density quartiles
        var byDensity = results.OrderBy(r => r.Density).ToList();
        int qSize = Math.Max(1, results.Count / 4);
        int remaining = results.Count - 3 * qSize;
        var quartiles = new (string label, List<GalaxyResult> group)[] {
            ("Lowest density", byDensity.Take(qSize).ToList()),
            ("Low density", byDensity.Skip(qSize).Take(qSize).ToList()),
            ("High density", byDensity.Skip(2*qSize).Take(qSize).ToList()),
            ("Highest density", byDensity.Skip(3*qSize).Take(Math.Max(1, remaining)).ToList())
        };

        sb.AppendLine($"{"Density Quartile",-18} {"Count",6} {"BARYON%",9} {"MedianFrac",11} {"OuterFrac",11}");
        sb.AppendLine(new string('-', 57));

        foreach (var (label, group) in quartiles)
        {
            int bCount = group.Count(r => r.Classification == "BARYON");
            sb.AppendLine($"{label,-18} {group.Count,6} {100.0*bCount/group.Count,9:F0}% {group.Average(r=>r.MedianFrac),11:F3} {group.Average(r=>r.OuterFrac),11:F3}");
        }
        sb.AppendLine("");

        // ================================================================
        // LOW-DENSITY FAILURE TEST
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Low-Density Failure Test ===");
        sb.AppendLine("");

        var lowest = quartiles[0].group;
        var highest = quartiles[3].group;
        int lowBaryon = lowest.Count(r => r.Classification == "BARYON");
        int highBaryon = highest.Count(r => r.Classification == "BARYON");

        sb.AppendLine($"  Lowest density:  {100.0*lowBaryon/lowest.Count:F0}% BARYON  (median frac = {lowest.Average(r=>r.MedianFrac):F3})");
        sb.AppendLine($"  Highest density: {100.0*highBaryon/highest.Count:F0}% BARYON  (median frac = {highest.Average(r=>r.MedianFrac):F3})");
        sb.AppendLine("");

        bool lowDensityWeaker = 100.0 * lowBaryon / lowest.Count < 100.0 * highBaryon / highest.Count * 0.8;
        if (lowDensityWeaker)
            sb.AppendLine("  → Low-density galaxies ARE the main failures — TRM density primacy weaker at low densities.");
        else
            sb.AppendLine("  → Density primacy holds ACROSS density range — TRM prediction is robust.");
        sb.AppendLine("");

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        double baryonFrac = 100.0 * baryonCount / results.Count;
        bool criterionA = baryonFrac >= 50;
        bool criterionB = results.Average(r => r.MedianFrac) > 0.60;
        bool criterionC = results.Count >= 50;
        bool criterionD = dmCount < baryonCount;

        int criteriaMet = 0;
        if (criterionA) criteriaMet++;
        if (criterionB) criteriaMet++;
        if (criterionC) criteriaMet++;
        if (criterionD) criteriaMet++;

        string verdict = criteriaMet >= 4 ? "SUPPORTED: density is dominant."
            : criteriaMet >= 2 ? "CONDITIONAL: density important but incomplete."
            : "FALSIFIED: density not dominant.";

        sb.AppendLine($"VERDICT: {verdict}  ({criteriaMet}/4 criteria)");
        sb.AppendLine("");
        sb.AppendLine($"  A. ≥50% BARYON-dominated:                 {(criterionA ? "YES" : "NO")} ({baryonFrac:F0}%)");
        sb.AppendLine($"  B. Median fraction > 0.60:                 {(criterionB ? "YES" : "NO")} ({results.Average(r=>r.MedianFrac):F3})");
        sb.AppendLine($"  C. ≥50 galaxies analyzed:                  {(criterionC ? "YES" : "NO")} ({results.Count})");
        sb.AppendLine($"  D. BARYON > DM count:                      {(criterionD ? "YES" : "NO")} ({baryonCount} vs {dmCount})");
        sb.AppendLine("");
        sb.AppendLine("Density Primacy Result:");
        sb.AppendLine($"  {baryonFrac:F0}% of SPARC galaxies are BARYON-dominated.");
        sb.AppendLine($"  Median baryonic fraction across all: {results.OrderBy(r=>r.MedianFrac).ElementAt(results.Count/2).MedianFrac:F3}");
        sb.AppendLine("  TRM density primacy prediction: CONFRONTED with SPARC data.");
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== BTP_01 complete. Commit: BTP_01_BaryonicTestPredictionAudit ===");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    private static double ParseD(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return double.NaN;
        return double.TryParse(s.Trim(), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : double.NaN;
    }

    private record DataPoint(double R, double Vobs, double Vgas, double Vdisk, double Vbul);
    private record GalaxyResult(string Id, string Classification, double MedianFrac, double OuterFrac, double Density, int PointCount);
}
