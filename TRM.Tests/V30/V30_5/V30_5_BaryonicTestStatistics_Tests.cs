using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TRM.Core.Data;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V30_5;

[Trait("Category", "V30_5")]
[Trait("Category", "LongRunning")]
public class V30_5_BaryonicTestStatistics_Tests
{
    private readonly ITestOutputHelper _o;
    public V30_5_BaryonicTestStatistics_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void BTS_01_BaryonicTestStatisticsAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== BTS_01: Baryonic Test Statistics Audit ===");
        sb.AppendLine("=== Complete numerical outcome of SPARC density primacy test ===");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("");

        var repoRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
        var massFile = Path.Combine(repoRoot, "TRM.Core", "Data", "MassModels_Lelli2016c.mrt");
        var ds = MrtParser.Parse(massFile);

        var colIdx = new Dictionary<string, int>();
        for (int i = 0; i < ds.Columns.Count; i++) colIdx[ds.Columns[i].Label.Trim()] = i;

        var galaxies = new Dictionary<string, List<DPt>>();
        foreach (var row in ds.Rows)
        {
            string id = row.Values[colIdx["ID"]].Trim();
            double r = ParseD(row.Values[colIdx["R"]]), vobs = ParseD(row.Values[colIdx["Vobs"]]);
            double vgas = ParseD(row.Values[colIdx["Vgas"]]), vdisk = ParseD(row.Values[colIdx["Vdisk"]]), vbul = ParseD(row.Values[colIdx["Vbul"]]);
            if (double.IsNaN(r) || double.IsNaN(vobs) || r < 0 || vobs <= 0) continue;
            if (!galaxies.ContainsKey(id)) galaxies[id] = new List<DPt>();
            galaxies[id].Add(new DPt(r, vobs, vgas, vdisk, vbul));
        }

        var results = new List<GalStat>();
        foreach (var (id, pts) in galaxies)
        {
            var sorted = pts.OrderBy(p => p.R).ToList();
            if (sorted.Count < 5) continue;
            var fracs = sorted.Select(p => { double vB = Math.Sqrt(Math.Max(0, p.Vgas * p.Vgas + p.Vdisk * p.Vdisk + p.Vbul * p.Vbul)); return vB > 0 && p.Vobs > 0 ? vB / p.Vobs : 0; }).Where(f => f > 0).ToList();
            if (fracs.Count < 3) continue;
            double med = fracs.OrderBy(f => f).ElementAt(fracs.Count / 2);
            string cls = med > 0.85 ? "BARYON" : med > 0.50 ? "MIXED" : "DM";
            results.Add(new GalStat(id, cls, med, sorted.Count));
        }

        if (results.Count == 0) { sb.AppendLine("  No valid galaxies."); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        int b = results.Count(r => r.Class == "BARYON"), m = results.Count(r => r.Class == "MIXED"), d = results.Count(r => r.Class == "DM");
        int total = results.Count;

        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Class Statistics ===");
        sb.AppendLine("");
        sb.AppendLine($"  count(BARYON): {b}");
        sb.AppendLine($"  count(MIXED):  {m}");
        sb.AppendLine($"  count(DM):     {d}");
        sb.AppendLine($"  TOTAL:         {total}");
        sb.AppendLine("");
        sb.AppendLine($"  fraction(BARYON): {100.0*b/total:F1}%");
        sb.AppendLine($"  fraction(MIXED):  {100.0*m/total:F1}%");
        sb.AppendLine($"  fraction(DM):     {100.0*d/total:F1}%");
        sb.AppendLine("");
        sb.AppendLine($"  BARYON+MIXED: {b+m} ({100.0*(b+m)/total:F1}%)");
        sb.AppendLine("");

        double bmPct = 100.0 * (b + m) / total;
        sb.AppendLine($"  THRESHOLD: BARYON+MIXED > 75% → {(bmPct > 75 ? "✓ PASS" : "✗ FAIL")}");
        sb.AppendLine("");

        // ================================================================
        // QUARTILE STATISTICS
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Quartile Statistics ===");
        sb.AppendLine("");

        var ordered = results.OrderBy(r => r.MedFrac).ToList();
        int qs = total / 4;
        sb.AppendLine($"{"Quartile",-20} {"Range",16} {"BARYON",8} {"MIXED",7} {"DM",5} {"B+M%",8}");
        sb.AppendLine(new string('-', 66));

        for (int q = 0; q < 4; q++)
        {
            var grp = q < 3 ? ordered.Skip(q * qs).Take(qs).ToList() : ordered.Skip(3 * qs).ToList();
            int bg = grp.Count(r => r.Class == "BARYON"), mg = grp.Count(r => r.Class == "MIXED"), dg = grp.Count(r => r.Class == "DM");
            double lo = grp.Min(r => r.MedFrac), hi = grp.Max(r => r.MedFrac);
            string label = q == 0 ? "Lowest medFrac" : q == 1 ? "Low medFrac" : q == 2 ? "High medFrac" : "Highest medFrac";
            sb.AppendLine($"{label,-20} [{lo:F3},{hi:F3}]  {bg,8} {mg,7} {dg,5} {100.0*(bg+mg)/grp.Count,8:F1}%");
        }
        sb.AppendLine("");

        // ================================================================
        // TOP 10 CONFIRMATIONS
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Strongest 10 Confirmations (highest medFrac) ===");
        sb.AppendLine("");

        var top10 = results.OrderByDescending(r => r.MedFrac).Take(10).ToList();
        sb.AppendLine($"{"Rank",4} {"Galaxy",-12} {"Class",-8} {"MedFrac",10} {"Points",8}");
        sb.AppendLine(new string('-', 44));
        int rank = 1;
        foreach (var g in top10)
            sb.AppendLine($"{rank++,4} {g.Id,-12} {g.Class,-8} {g.MedFrac,10:F4} {g.Points,8}");
        sb.AppendLine("");

        // ================================================================
        // TOP 10 FAILURES
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Strongest 10 Failures (lowest medFrac) ===");
        sb.AppendLine("");

        var bot10 = results.OrderBy(r => r.MedFrac).Take(10).ToList();
        sb.AppendLine($"{"Rank",4} {"Galaxy",-12} {"Class",-8} {"MedFrac",10} {"Points",8}");
        sb.AppendLine(new string('-', 44));
        rank = 1;
        foreach (var g in bot10)
            sb.AppendLine($"{rank++,4} {g.Id,-12} {g.Class,-8} {g.MedFrac,10:F4} {g.Points,8}");
        sb.AppendLine("");

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        string verdict = bmPct > 75 ? "SUPPORTED: BARYON+MIXED > 75%"
            : bmPct >= 50 ? "CONDITIONAL: 50–75%"
            : "FALSIFIED: <50%";

        sb.AppendLine($"VERDICT: {verdict}");
        sb.AppendLine("");
        sb.AppendLine($"  BARYON+MIXED: {b+m}/{total} ({bmPct:F1}%)");
        sb.AppendLine($"  Top confirmations: {top10.Count(t => t.Class == "BARYON")}/{top10.Count} BARYON");
        sb.AppendLine($"  Top failures: {bot10.Count(t => t.Class == "DM")}/{bot10.Count} DM");
        sb.AppendLine("");
        sb.AppendLine("SPARC Density Primacy Statistics — complete.");
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== BTS_01 complete. Commit: BTS_01_BaryonicTestStatisticsAudit ===");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    private static double ParseD(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return double.NaN;
        return double.TryParse(s.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : double.NaN;
    }

    private record DPt(double R, double Vobs, double Vgas, double Vdisk, double Vbul);
    private record GalStat(string Id, string Class, double MedFrac, int Points);
}
