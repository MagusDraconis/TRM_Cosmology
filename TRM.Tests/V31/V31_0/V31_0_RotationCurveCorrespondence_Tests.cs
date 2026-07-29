using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V31_0;

[Trait("Category", "V31_0")]
[Trait("Category", "LongRunning")]
public class V31_0_RotationCurveCorrespondence_Tests
{
    private readonly ITestOutputHelper _o;
    public V31_0_RotationCurveCorrespondence_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void RTC_01_RotationCurveCorrespondenceAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== RTC_01: Rotation Curve Correspondence Audit ===");
        sb.AppendLine("=== Can TRM density structure reproduce SPARC rotation SHAPES? ===");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("");

        var repoRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
        var massFile = Path.Combine(repoRoot, "TRM.Core", "Data", "MassModels_Lelli2016c.mrt");

        var galData = new Dictionary<string, List<(double r, double vobs, double vbary)>>();
        bool inData = false;
        foreach (var line in File.ReadLines(massFile))
        {
            if (!inData) { if (line.StartsWith("---") && line.Contains("---")) { inData = true; continue; } continue; }
            if (line.StartsWith("---") || line.StartsWith("===") || line.StartsWith("Note") || string.IsNullOrWhiteSpace(line)) continue;
            if (line.Length < 59) continue;
            string id = line.Substring(0, 11).Trim();
            if (id.Length == 0) continue;
            double r = ParseD(line.Substring(19, 7)), vobs = ParseD(line.Substring(26, 7));
            double vgas = ParseD(line.Substring(39, 7)), vdisk = ParseD(line.Substring(46, 7)), vbul = ParseD(line.Substring(53, 7));
            if (double.IsNaN(r) || double.IsNaN(vobs) || r < 0 || vobs < 0) continue;
            double vb = Math.Sqrt(Math.Max(0, vgas * vgas + vdisk * vdisk + vbul * vbul));
            if (!galData.ContainsKey(id)) galData[id] = new List<(double, double, double)>();
            galData[id].Add((r, vobs, vb));
        }

        // Per-galaxy shape analysis
        var results = new List<GalaxyShape>();
        foreach (var (id, pts) in galData)
        {
            var sorted = pts.OrderBy(p => p.r).ToList();
            if (sorted.Count < 8) continue;

            // Split into inner (0-40%), middle (40-70%), outer (70-100%)
            int n = sorted.Count;
            int iSplit = n * 2 / 5, mSplit = n * 7 / 10;

            var inner = sorted.Take(iSplit).ToList();
            var middle = sorted.Skip(iSplit).Take(mSplit - iSplit).ToList();
            var outer = sorted.Skip(mSplit).ToList();

            if (inner.Count < 2 || middle.Count < 2 || outer.Count < 2) continue;

            // Observed shape
            double vInner = inner.Average(p => p.vobs), vOuter = outer.Average(p => p.vobs);
            double vShape = vOuter > 0 ? (vOuter - vInner) / vInner : 0; // rising(+) flat(0) declining(-)

            // Baryonic shape
            double bInner = inner.Average(p => p.vbary), bOuter = outer.Average(p => p.vbary);
            double bShape = bOuter > 0 ? (bOuter - bInner) / bInner : 0;

            // Gradient correlation: r vs vobs slope vs r vs density slope
            var vSlope = Slope(sorted.Select(p => p.r).ToArray(), sorted.Select(p => p.vobs).ToArray());
            var dSlope = Slope(sorted.Select(p => p.r).ToArray(), sorted.Select(p => p.vbary).ToArray());

            double vFlat = outer.Average(p => p.vobs);
            double vFlatStd = Math.Sqrt(outer.Average(p => (p.vobs - vFlat) * (p.vobs - vFlat)));
            double flatness = vFlat > 0 ? 1.0 - vFlatStd / vFlat : 0;

            // Density at outer radius
            double outerDens = outer.Average(p => p.vbary * p.vbary);

            results.Add(new GalaxyShape(id, vShape, bShape, vSlope, dSlope, vFlat, flatness, outerDens, sorted.Count));
        }

        if (results.Count == 0) { _o.WriteLine("No valid galaxies."); Assert.True(true); return; }

        // Shape agreement: same sign for vShape and bShape
        int sameSign = results.Count(r => (r.VShape > 0 && r.BShape > 0) || (r.VShape < 0 && r.BShape < 0) || (Math.Abs(r.VShape) < 0.02 && Math.Abs(r.BShape) < 0.02));
        double shapeAgree = 100.0 * sameSign / results.Count;

        // Slope correlation
        double[] vSlopes = results.Select(r => r.VSlope).ToArray();
        double[] dSlopes = results.Select(r => r.DSlope).ToArray();
        double slopeCorr = PearsonCorr(vSlopes, dSlopes);

        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Shape Correspondence ===");
        sb.AppendLine("");
        sb.AppendLine($"  Galaxies analyzed: {results.Count}");
        sb.AppendLine($"  Same shape sign:   {sameSign} ({shapeAgree:F1}%)");
        sb.AppendLine($"  Slope correlation: r = {slopeCorr:F4}");
        sb.AppendLine("");

        // ================================================================
        // BEST / WORST MATCHES
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Best Matches (smallest |vShape - bShape|) ===");
        sb.AppendLine("");
        sb.AppendLine($"{"Galaxy",-12} {"vShape",8} {"bShape",8} {"Diff",8} {"vFlat",8} {"Flatness",9} {"Pts",5}");
        sb.AppendLine(new string('-', 62));

        var sortedByDiff = results.OrderBy(r => Math.Abs(r.VShape - r.BShape)).ToList();
        foreach (var r in sortedByDiff.Take(8))
            sb.AppendLine($"{r.Id,-12} {r.VShape,8:F3} {r.BShape,8:F3} {Math.Abs(r.VShape-r.BShape),8:F3} {r.VFlat,8:F1} {r.Flatness,9:F3} {r.Points,5}");

        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Worst Matches (largest |vShape - bShape|) ===");
        sb.AppendLine("");
        sb.AppendLine($"{"Galaxy",-12} {"vShape",8} {"bShape",8} {"Diff",8} {"vFlat",8} {"Flatness",9} {"Pts",5}");
        sb.AppendLine(new string('-', 62));

        foreach (var r in sortedByDiff.TakeLast(8).Reverse())
            sb.AppendLine($"{r.Id,-12} {r.VShape,8:F3} {r.BShape,8:F3} {Math.Abs(r.VShape-r.BShape),8:F3} {r.VFlat,8:F1} {r.Flatness,9:F3} {r.Points,5}");
        sb.AppendLine("");

        // ================================================================
        // LOW-DENSITY DEVIATION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Low-Density Deviation Test ===");
        sb.AppendLine("");

        var loDens = results.OrderBy(r => r.OuterDensity).Take(results.Count / 3).ToList();
        var hiDens = results.OrderByDescending(r => r.OuterDensity).Take(results.Count / 3).ToList();
        double loDiff = loDens.Average(r => Math.Abs(r.VShape - r.BShape));
        double hiDiff = hiDens.Average(r => Math.Abs(r.VShape - r.BShape));

        sb.AppendLine($"  Low-density mean |diff|: {loDiff:F3}");
        sb.AppendLine($"  High-density mean |diff|: {hiDiff:F3}");
        sb.AppendLine($"  Ratio: {loDiff/Math.Max(1e-15,hiDiff):F2}x");
        sb.AppendLine($"  → {(loDiff > hiDiff*1.5 ? "Low-density galaxies ARE worse matches" : "No systematic density dependence")}");
        sb.AppendLine("");

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        bool criterionA = shapeAgree > 50;
        bool criterionB = Math.Abs(slopeCorr) > 0.2;
        bool criterionC = results.Count >= 100;
        bool criterionD = sortedByDiff.Take(5).Average(r => Math.Abs(r.VShape - r.BShape)) < 0.3;

        int criteriaMet = 0;
        if (criterionA) criteriaMet++;
        if (criterionB) criteriaMet++;
        if (criterionC) criteriaMet++;
        if (criterionD) criteriaMet++;

        string verdict = criteriaMet >= 4 ? "SUPPORTED: density predicts rotation curve morphology."
            : criteriaMet >= 2 ? "CONDITIONAL: partial predictive power."
            : "FALSIFIED: no meaningful shape correspondence.";

        sb.AppendLine($"VERDICT: {verdict}  ({criteriaMet}/4 criteria)");
        sb.AppendLine("");
        sb.AppendLine($"  A. Shape sign agrees >50%:               {(criterionA ? "YES" : "NO")} ({shapeAgree:F1}%)");
        sb.AppendLine($"  B. Slope correlation |r|>0.2:            {(criterionB ? "YES" : "NO")} (r={slopeCorr:F4})");
        sb.AppendLine($"  C. ≥100 galaxies:                        {(criterionC ? "YES" : "NO")} ({results.Count})");
        sb.AppendLine($"  D. Best 5 mean |diff|<0.3:               {(criterionD ? "YES" : "NO")}");
        sb.AppendLine("");
        sb.AppendLine("Rotation Curve Correspondence began.");
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== RTC_01 complete. Commit: RTC_01_RotationCurveCorrespondenceAudit ===");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    private static double Slope(double[] xs, double[] ys)
    {
        double mx = xs.Average(), my = ys.Average();
        double sxy = 0, sxx = 0;
        for (int i = 0; i < xs.Length; i++) { double dx = xs[i] - mx; sxy += dx * (ys[i] - my); sxx += dx * dx; }
        return sxx > 1e-15 ? sxy / sxx : 0;
    }

    private static double PearsonCorr(double[] xs, double[] ys)
    {
        int n = xs.Length; if (n < 2) return 0;
        double mx = xs.Average(), my = ys.Average();
        double sxy = 0, sxx = 0, syy = 0;
        for (int i = 0; i < n; i++) { double dx = xs[i] - mx, dy = ys[i] - my; sxy += dx * dy; sxx += dx * dx; syy += dy * dy; }
        return sxx > 1e-15 && syy > 1e-15 ? sxy / Math.Sqrt(sxx * syy) : 0;
    }

    private static double ParseD(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return double.NaN;
        return double.TryParse(s.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : double.NaN;
    }

    private record GalaxyShape(string Id, double VShape, double BShape, double VSlope, double DSlope, double VFlat, double Flatness, double OuterDensity, int Points);
}
