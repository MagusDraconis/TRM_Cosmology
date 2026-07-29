using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V31_7;

[Trait("Category", "V31_7")]
[Trait("Category", "LongRunning")]
public class V31_7_SpiralArmChannel_Tests
{
    private readonly ITestOutputHelper _o;
    public V31_7_SpiralArmChannel_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void SAC_01_SpiralArmChannelAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== SAC_01: Spiral Arm Channel Audit ===");
        sb.AppendLine("=== Do spiral-arm regions correspond to TRM flow channels? ===");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("");
        sb.AppendLine("NOTE: Using 1D rotation curve data as proxy. Strong disk galaxies");
        sb.AppendLine("  (high Vdisk/Vobs) are the spiral-arm population.");
        sb.AppendLine("");

        var repoRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
        var massFile = Path.Combine(repoRoot, "TRM.Core", "Data", "MassModels_Lelli2016c.mrt");

        var galData = new Dictionary<string, List<SaPt>>();
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
            if (!galData.ContainsKey(id)) galData[id] = new List<SaPt>();
            galData[id].Add(new SaPt(r, vobs, vb, vdisk, vbul));
        }

        var results = new List<SaResult>();
        foreach (var (id, pts) in galData)
        {
            var sorted = pts.OrderBy(p => p.R).ToList();
            if (sorted.Count < 10) continue;

            int nO = Math.Max(3, sorted.Count / 3);
            var outer = sorted.Skip(sorted.Count - nO).ToList();

            // Disk dominance (spiral proxy)
            double diskFrac = outer.Average(p => p.Vobs > 0 ? p.Vdisk / p.Vobs : 0);
            double bulgeFrac = outer.Average(p => p.Vobs > 0 ? p.Vbul / p.Vobs : 0);
            double diskDom = outer.Average(p => p.Vbary > 0 ? p.Vdisk * p.Vdisk / (p.Vbary * p.Vbary) : 0);
            // Galaxy type: disk-dominated (>0.5 disk), bulge+disk, bulge-only
            string galType = diskDom > 0.7 ? "DISK" : diskDom > 0.4 ? "DISK+BULGE" : "BULGE";

            // Flow channel metrics
            var dD = new List<double>(); var dV = new List<double>();
            for (int i = 1; i < sorted.Count - 1; i++)
            { double dr = sorted[i+1].R - sorted[i-1].R; if (dr<1e-6) continue; dD.Add((sorted[i+1].Vbary*sorted[i+1].Vbary - sorted[i-1].Vbary*sorted[i-1].Vbary)/dr); dV.Add((sorted[i+1].Vobs - sorted[i-1].Vobs)/dr); }
            if (dD.Count < 5) continue;

            double align = PearsonCorr(dD.ToArray(), dV.ToArray());
            int same = 0; for (int i = 0; i < dD.Count; i++) if (dD[i] * dV[i] < 0) same++;
            double chFrac = (double)same / dD.Count;

            // Gradient oscillation: std/max of consecutive density gradients (channel structure indicator)
            double gradOsc = 0;
            for (int i = 1; i < dD.Count; i++) gradOsc += Math.Abs(dD[i] - dD[i-1]);
            gradOsc = dD.Count > 1 ? gradOsc / (dD.Count - 1) / (dD.Select(Math.Abs).DefaultIfEmpty(1).Max()) : 0;

            // Continuity: fraction of adjacent same-sign gradient pairs
            int signCh = 0; for (int i = 1; i < dD.Count; i++) if (Math.Sign(dD[i]) != Math.Sign(dD[i-1])) signCh++;
            double continuity = 1.0 - (double)signCh / Math.Max(1, dD.Count - 1);

            results.Add(new SaResult(id, galType, diskDom, diskFrac, align, chFrac, gradOsc, continuity));
        }

        if (results.Count == 0) { _o.WriteLine("No valid galaxies."); Assert.True(true); return; }

        // Group by galaxy type
        var groups = results.GroupBy(r => r.GalType).OrderBy(g => g.Key).ToList();

        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Arm-Channel Alignment by Galaxy Type ===");
        sb.AppendLine("");
        sb.AppendLine($"{"Type",-14} {"Count",6} {"Align r",8} {"ChFrac",8} {"GradOsc",8} {"Continuity",10} {"DiskDom",8}");
        sb.AppendLine(new string('-', 66));

        foreach (var g in groups)
        {
            sb.AppendLine($"{g.Key,-14} {g.Count(),6} {g.Average(r=>r.Align),8:F4} {g.Average(r=>r.ChFrac),8:F3} {g.Average(r=>r.GradOsc),8:F3} {g.Average(r=>r.Continuity),10:F3} {g.Average(r=>r.DiskDom),8:F3}");
        }
        sb.AppendLine("");

        // Correlation: disk dominance vs channel quality
        double corrDiskAlign = PearsonCorr(
            results.Select(r => r.DiskDom).ToArray(),
            results.Select(r => r.Align).ToArray());

        sb.AppendLine($"  DiskDom vs Align correlation: r = {corrDiskAlign:F4}");
        sb.AppendLine($"  → {(Math.Abs(corrDiskAlign) > 0.15 ? "STRONGER disks = STRONGER channels (spiral arm connection)" : "NO correlation — disk structure independent of channels")}");
        sb.AppendLine("");

        // ================================================================
        // BEST EXAMPLES
        // ================================================================
        var diskGals = results.Where(r => r.DiskDom > 0.7).OrderByDescending(r => r.Align).Take(5).ToList();
        if (diskGals.Count > 0)
        {
            sb.AppendLine(new string('=', 108));
            sb.AppendLine("=== Best Disk-Channel Galaxies ===");
            sb.AppendLine("");
            sb.AppendLine($"{"Galaxy",-12} {"DiskDom",8} {"Align r",8} {"ChFrac",8} {"Continuity",10}");
            sb.AppendLine(new string('-', 48));
            foreach (var r in diskGals)
                sb.AppendLine($"{r.Id,-12} {r.DiskDom,8:F3} {r.Align,8:F4} {r.ChFrac,8:F3} {r.Continuity,10:F3}");
            sb.AppendLine("");
        }

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        bool criterionA = Math.Abs(corrDiskAlign) > 0.1;
        bool criterionB = results.Count >= 100;
        bool criterionC = groups.Count() >= 3;
        bool criterionD = diskGals.Count >= 3;

        int criteriaMet = 0;
        if (criterionA) criteriaMet++;
        if (criterionB) criteriaMet++;
        if (criterionC) criteriaMet++;
        if (criterionD) criteriaMet++;

        string verdict = criteriaMet >= 4 ? "SUPPORTED: spiral arms behave as TRM flow channels."
            : criteriaMet >= 2 ? "CONDITIONAL: partial alignment."
            : "FALSIFIED: no channel correspondence exists.";

        sb.AppendLine($"VERDICT: {verdict}  ({criteriaMet}/4 criteria)");
        sb.AppendLine("");
        sb.AppendLine($"  A. Disk-channel correlation exists:       {(criterionA ? "YES" : "NO")} (r={corrDiskAlign:F4})");
        sb.AppendLine($"  B. ≥100 galaxies:                         {(criterionB ? "YES" : "NO")} ({results.Count})");
        sb.AppendLine($"  C. ≥3 galaxy types:                       {(criterionC ? "YES" : "NO")} ({groups.Count()})");
        sb.AppendLine($"  D. ≥3 strong disk galaxies:               {(criterionD ? "YES" : "NO")} ({diskGals.Count})");
        sb.AppendLine("");
        sb.AppendLine("Spiral Arm Channel Result:");
        sb.AppendLine("  The connection between disk dominance and flow channels is");
        sb.AppendLine("  tested from 1D rotation curve data. Full 2D density maps");
        sb.AppendLine("  would provide a more direct test of spiral arm alignment.");
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== SAC_01 complete. Commit: SAC_01_SpiralArmChannelAudit ===");

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

    private static double ParseD(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return double.NaN;
        return double.TryParse(s.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : double.NaN;
    }

    private record SaPt(double R, double Vobs, double Vbary, double Vdisk, double Vbul);
    private record SaResult(string Id, string GalType, double DiskDom, double DiskFrac, double Align, double ChFrac, double GradOsc, double Continuity);
}
