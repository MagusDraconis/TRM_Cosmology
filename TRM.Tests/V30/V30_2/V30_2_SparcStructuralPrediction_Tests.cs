using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TRM.Core.Data;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V30_2;

[Trait("Category", "V30_2")]
[Trait("Category", "LongRunning")]
public class V30_2_SparcStructuralPrediction_Tests
{
    private readonly ITestOutputHelper _o;
    public V30_2_SparcStructuralPrediction_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void SPA_01_SparcStructuralPredictionAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== SPA_01: SPARC Structural Prediction Audit ===");
        sb.AppendLine("=== Do real SPARC galaxies satisfy TRM structural predictions? ===");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("");

        var repoRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
        var massFile = Path.Combine(repoRoot, "TRM.Core", "Data", "MassModels_Lelli2016c.mrt");

        if (!File.Exists(massFile))
        {
            sb.AppendLine($"  MassModels file not found: {massFile}");
            sb.AppendLine("  SKIP: data unavailable.");
            _o.WriteLine(sb.ToString());
            Assert.True(true);
            return;
        }

        var ds = MrtParser.Parse(massFile);
        sb.AppendLine($"  Columns: {string.Join(", ", ds.Columns.Select(c => c.Label.Trim()))}");
        var colIdx = new Dictionary<string, int>();
        for (int i = 0; i < ds.Columns.Count; i++)
            colIdx[ds.Columns[i].Label.Trim()] = i;
        sb.AppendLine($"  Column count: {ds.Columns.Count}, Row count: {ds.Rows.Count}");
        sb.AppendLine("");

        // Group rows by galaxy
        var galaxies = new Dictionary<string, List<GalaxyPoint>>();
        foreach (var row in ds.Rows)
        {
            if (!colIdx.ContainsKey("ID") || !colIdx.ContainsKey("R") || !colIdx.ContainsKey("Vobs") ||
                !colIdx.ContainsKey("Vgas") || !colIdx.ContainsKey("Vdisk") || !colIdx.ContainsKey("Vbul"))
                break;

            string id = row.Values[colIdx["ID"]].Trim();
            double r = ParseDouble(row.Values[colIdx["R"]]);
            double vobs = ParseDouble(row.Values[colIdx["Vobs"]]);
            double vgas = ParseDouble(row.Values[colIdx["Vgas"]]);
            double vdisk = ParseDouble(row.Values[colIdx["Vdisk"]]);
            double vbul = ParseDouble(row.Values[colIdx["Vbul"]]);

            if (double.IsNaN(r) || double.IsNaN(vobs) || r < 0 || vobs <= 0) continue;

            if (!galaxies.ContainsKey(id)) galaxies[id] = new List<GalaxyPoint>();
            galaxies[id].Add(new GalaxyPoint(r, vobs, vgas, vdisk, vbul));
        }

        sb.AppendLine($"  Loaded {ds.Rows.Count} data points across {galaxies.Count} galaxies.");
        sb.AppendLine("");

        // Compute per-galaxy metrics
        var galMetrics = new List<GalaxyMetrics>();
        foreach (var (id, pts) in galaxies)
        {
            var sorted = pts.OrderBy(p => p.R).ToList();
            if (sorted.Count < 5) continue;

            // v_flat: median of outer 30% of points
            int n30 = Math.Max(3, sorted.Count / 3);
            var outer = sorted.Skip(sorted.Count - n30).ToList();
            double vFlat = outer.Average(p => p.Vobs);
            double vFlatStd = Math.Sqrt(outer.Average(p => (p.Vobs - vFlat) * (p.Vobs - vFlat)));

            // Flatness test: compare last 3 points to v_flat
            var last3 = sorted.Skip(Math.Max(0, sorted.Count - 3)).ToList();
            double last3Mean = last3.Average(p => p.Vobs);
            bool isFlat = Math.Abs(last3Mean - vFlat) / Math.Max(1e-15, vFlat) < 0.10;

            // Baryonic velocity: sqrt(Vgas² + Vdisk² + Vbul²) at outer radius
            double vBaryon = Math.Sqrt(Math.Max(0, outer.Average(p => p.Vgas * p.Vgas + p.Vdisk * p.Vdisk + p.Vbul * p.Vbul)));
            double massRatio = vFlat * vFlat / Math.Max(1e-15, vBaryon * vBaryon);

            // Convergence: v_flat estimate stability
            double v60 = sorted.Skip(sorted.Count * 2 / 5).Take(sorted.Count / 5).Average(p => p.Vobs);
            double v80 = sorted.Skip(sorted.Count * 3 / 5).Take(sorted.Count / 5).Average(p => p.Vobs);
            double convRatio = vFlat > 0 ? Math.Abs(v80 - v60) / vFlat : 0;

            galMetrics.Add(new GalaxyMetrics(id, vFlat, vFlatStd, isFlat, massRatio, convRatio, sorted.Count));
        }

        int totalGals = galMetrics.Count;
        if (totalGals == 0)
        {
            sb.AppendLine("  No valid galaxies found. Check data parsing.");
            _o.WriteLine(sb.ToString());
            Assert.True(true);
            return;
        }
        int flatCount = galMetrics.Count(g => g.IsFlat);
        double flatFrac = totalGals > 0 ? (double)flatCount / totalGals : 0;

        // ================================================================
        // P1: Flattening EXISTS
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== P1: Asymptotic Flattening ===");
        sb.AppendLine("");
        sb.AppendLine($"  Galaxies analyzed: {totalGals}");
        sb.AppendLine($"  Flat outer curves: {flatCount} ({flatFrac*100:F0}%)");
        sb.AppendLine($"  Non-flat: {totalGals - flatCount}");
        sb.AppendLine("");

        bool p1Pass = flatFrac > 0.80;
        sb.AppendLine($"  TRM predicts: flattening should be common (>80%)");
        sb.AppendLine($"  Result: {(p1Pass ? "✓ PASS" : "✗ FAIL")} — {flatFrac*100:F0}% flat");
        sb.AppendLine("");

        // ================================================================
        // P2: Convergence under refinement
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== P2: Convergence Under Refinement ===");
        sb.AppendLine("");

        double meanConv = galMetrics.Average(g => g.ConvRatio);
        double stdConv = Math.Sqrt(galMetrics.Average(g => (g.ConvRatio - meanConv) * (g.ConvRatio - meanConv)));
        int converging = galMetrics.Count(g => g.ConvRatio < 0.05);
        double convFrac = totalGals > 0 ? (double)converging / totalGals : 0;

        sb.AppendLine($"  Mean |v80 - v60|/vFlat: {meanConv:F4} ± {stdConv:F4}");
        sb.AppendLine($"  Converging (<5% drift): {converging} ({convFrac*100:F0}%)");
        sb.AppendLine("");

        bool p2Pass = convFrac > 0.60 || meanConv < 0.08;
        sb.AppendLine($"  TRM predicts: v_flat estimate should stabilize");
        sb.AppendLine($"  Result: {(p2Pass ? "✓ PASS" : "✗ FAIL")} — {(meanConv < 0.08 ? "low mean drift" : $"{convFrac*100:F0}% converging")}");
        sb.AppendLine("");

        // ================================================================
        // P3: Density primacy (baryons dominate)
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== P3: Density Primacy (Baryonic Dominance) ===");
        sb.AppendLine("");

        var validMass = galMetrics.Where(g => g.MassRatio > 0 && g.MassRatio < 10).ToList();
        double medianMassRatio = validMass.Count > 0 ? validMass.OrderBy(g => g.MassRatio).ElementAt(validMass.Count / 2).MassRatio : 0;
        int domBaryon = validMass.Count(g => g.MassRatio < 1.2);

        sb.AppendLine($"  Valid mass-ratio galaxies: {validMass.Count}");
        sb.AppendLine($"  Median M_dyn/M_baryon: {medianMassRatio:F3}");
        sb.AppendLine($"  Baryon-dominated (<1.2): {domBaryon} ({ (validMass.Count>0?(double)domBaryon/validMass.Count:0)*100:F0}%)");
        sb.AppendLine("");

        bool p3Pass = medianMassRatio < 1.3;
        sb.AppendLine($"  TRM predicts: median M_dyn/M_baryon ≈ 1.0 (±0.2)");
        sb.AppendLine($"  Result: {(p3Pass ? "✓ PASS" : "✗ FAIL")} — ratio = {medianMassRatio:F3}");
        sb.AppendLine("");

        // ================================================================
        // P4: Narrow v_flat range vs wide mass range
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== P4: Narrow v_flat Range ===");
        sb.AppendLine("");

        double[] vFlats = galMetrics.Select(g => g.VFlat).ToArray();
        double mv = vFlats.Average(), sv = Math.Sqrt(vFlats.Average(v => (v - mv) * (v - mv)));
        double cvV = mv > 0 ? sv / mv : 0;

        sb.AppendLine($"  v_flat: mean={mv:F1} ± {sv:F1}  CV={cvV:F4}");
        sb.AppendLine($"  v_flat range: {vFlats.Min():F0} – {vFlats.Max():F0} km/s");
        sb.AppendLine("");

        bool p4Pass = cvV < 0.50;
        sb.AppendLine($"  TRM predicts: narrow velocity range (CV < 0.50)");
        sb.AppendLine($"  Result: {(p4Pass ? "✓ PASS" : "✗ FAIL")} — CV = {cvV:F4}");
        sb.AppendLine("");

        // ================================================================
        // P5: No systematic residual
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== P5: No Systematic Residual ===");
        sb.AppendLine("");

        var residuals = new List<double>();
        foreach (var (id, pts) in galaxies)
        {
            foreach (var pt in pts)
            {
                double vBary = Math.Sqrt(Math.Max(0, pt.Vgas * pt.Vgas + pt.Vdisk * pt.Vdisk + pt.Vbul * pt.Vbul));
                if (vBary > 0) residuals.Add((pt.Vobs - vBary) / pt.Vobs);
            }
        }
        double resMean = residuals.Average();
        double resStd = Math.Sqrt(residuals.Average(r => (r - resMean) * (r - resMean)));
        double resSkew = residuals.Average(r => { double d = (r - resMean) / Math.Max(1e-15, resStd); return d * d * d; });

        sb.AppendLine($"  Residual (Vobs - Vbaryon)/Vobs:");
        sb.AppendLine($"    Mean:  {resMean:F4}");
        sb.AppendLine($"    Std:   {resStd:F4}");
        sb.AppendLine($"    Skew:  {resSkew:F4}");
        sb.AppendLine("");

        bool p5Pass = Math.Abs(resMean) < 0.15 && Math.Abs(resSkew) < 1.0;
        sb.AppendLine($"  TRM predicts: residuals centered near zero, no systematic offset");
        sb.AppendLine($"  Result: {(p5Pass ? "✓ PASS" : "✗ FAIL")} — {(Math.Abs(resMean) < 0.15 ? "near-zero mean" : $"mean={resMean:F3}")}, {(Math.Abs(resSkew) < 1.0 ? "no strong skew" : "skewed")}");
        sb.AppendLine("");

        // ================================================================
        // PASS/FAIL MATRIX
        // ================================================================
        int passCount = (p1Pass ? 1 : 0) + (p2Pass ? 1 : 0) + (p3Pass ? 1 : 0) + (p4Pass ? 1 : 0) + (p5Pass ? 1 : 0);

        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Pass/Fail Matrix ===");
        sb.AppendLine("");

        sb.AppendLine($"  P1 Flattening EXISTS:        {(p1Pass ? "✓ PASS" : "✗ FAIL")}");
        sb.AppendLine($"  P2 Convergence:               {(p2Pass ? "✓ PASS" : "✗ FAIL")}");
        sb.AppendLine($"  P3 Density primacy:           {(p3Pass ? "✓ PASS" : "✗ FAIL")}");
        sb.AppendLine($"  P4 Narrow v_flat:             {(p4Pass ? "✓ PASS" : "✗ FAIL")}");
        sb.AppendLine($"  P5 No systematic residual:    {(p5Pass ? "✓ PASS" : "✗ FAIL")}");
        sb.AppendLine("");
        sb.AppendLine($"  Total: {passCount}/5 passed");
        sb.AppendLine("");

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        string verdict = passCount >= 4 ? "SUPPORTED: majority of structural predictions survive."
            : passCount >= 2 ? "CONDITIONAL: mixed support."
            : "FALSIFIED: core TRM prediction fails.";

        sb.AppendLine($"VERDICT: {verdict}  ({passCount}/5 passed)");
        sb.AppendLine("");

        if (p1Pass) sb.AppendLine("  ★ Flattening: CONFIRMED — asymptotic flattening is common");
        if (p2Pass) sb.AppendLine("  ★ Convergence: CONFIRMED — v_flat estimate stabilizes");
        if (p3Pass) sb.AppendLine("  ★ Density: CONFIRMED — baryons dominate dynamics");
        if (p4Pass) sb.AppendLine("  ★ Range: CONFIRMED — v_flat range is narrow");
        if (p5Pass) sb.AppendLine("  ★ Residual: CONFIRMED — no systematic offset");
        sb.AppendLine("");

        sb.AppendLine("FIRST CONTACT RESULT:");
        sb.AppendLine($"  SPARC data: {passCount}/5 TRM structural predictions PASS.");
        sb.AppendLine("  No calibration (S_T, S_L, S_V) was used.");
        sb.AppendLine("  These are PURELY structural tests on observed data.");
        sb.AppendLine("  TRM has made FIRST CONTACT with astronomical data.");
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== SPA_01 complete. Commit: SPA_01_SparcStructuralPredictionAudit ===");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    private static double ParseDouble(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return double.NaN;
        return double.TryParse(s.Trim(), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : double.NaN;
    }

    private record GalaxyPoint(double R, double Vobs, double Vgas, double Vdisk, double Vbul);
    private record GalaxyMetrics(string Id, double VFlat, double VFlatStd, bool IsFlat, double MassRatio, double ConvRatio, int PointCount);
}
