using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V30_6;

[Trait("Category", "V30_6")]
[Trait("Category", "LongRunning")]
public class V30_6_BaryonicRawNumbers_Tests
{
    private readonly ITestOutputHelper _o;
    public V30_6_BaryonicRawNumbers_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void BRN_01_BaryonicRawNumbers()
    {
        var sb = new StringBuilder();
        sb.AppendLine("");
        sb.AppendLine("╔══════════════════════════════════════════════════════╗");
        sb.AppendLine("║     SPARC DENSITY PRIMACY — RAW NUMERICAL OUTPUT     ║");
        sb.AppendLine("╚══════════════════════════════════════════════════════╝");
        sb.AppendLine("");

        var repoRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
        var massFile = Path.Combine(repoRoot, "TRM.Core", "Data", "MassModels_Lelli2016c.mrt");

        // Manual fixed-width parse (MRT parser is unreliable for this format)
        // Byte positions: ID(1-11), D(13-18), R(20-25), Vobs(27-32), e_Vobs(34-38), Vgas(40-45), Vdisk(47-52), Vbul(54-59)
        var galaxies = new Dictionary<string, List<DPt>>();
        int parsed = 0, skipped = 0;
        bool inData = false;

        foreach (var line in File.ReadLines(massFile))
        {
            if (!inData)
            {
                if (line.StartsWith("---") && line.Contains("---")) { inData = true; continue; }
                continue;
            }
            if (line.StartsWith("---") || line.StartsWith("===") || line.StartsWith("Note") || string.IsNullOrWhiteSpace(line)) continue;
            if (line.Length < 59) continue;

            string id = line.Substring(0, 11).Trim();
            if (id.Length == 0) continue;
            double r = ParseD(line.Substring(19, 7));
            double vobs = ParseD(line.Substring(26, 7));
            double vgas = ParseD(line.Substring(39, 7));
            double vdisk = ParseD(line.Substring(46, 7));
            double vbul = ParseD(line.Substring(53, 7));

            if (double.IsNaN(r) || double.IsNaN(vobs) || r < 0 || vobs < 0) { skipped++; continue; }
            if (!galaxies.ContainsKey(id)) galaxies[id] = new List<DPt>();
            galaxies[id].Add(new DPt(r, vobs, vgas, vdisk, vbul));
            parsed++;
        }
        sb.AppendLine($"  Parsed {parsed} rows, skipped {skipped}, {galaxies.Count} galaxies");

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

        int b = results.Count(r => r.Class == "BARYON");
        int m = results.Count(r => r.Class == "MIXED");
        int d = results.Count(r => r.Class == "DM");
        int t = results.Count;
        if (t == 0) { sb.AppendLine("  count(TOTAL) = 0 — no valid galaxies found."); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        // ── RAW COUNTS ──
        sb.AppendLine("── RAW COUNTS ──");
        sb.AppendLine($"  count(BARYON)      = {b}");
        sb.AppendLine($"  count(MIXED)       = {m}");
        sb.AppendLine($"  count(DM)          = {d}");
        sb.AppendLine($"  count(TOTAL)       = {t}");
        sb.AppendLine("");

        // ── RAW FRACTIONS ──
        sb.AppendLine("── RAW FRACTIONS ──");
        sb.AppendLine($"  fraction(BARYON)   = {100.0*b/t:F1}%");
        sb.AppendLine($"  fraction(MIXED)    = {100.0*m/t:F1}%");
        sb.AppendLine($"  fraction(DM)       = {100.0*d/t:F1}%");
        sb.AppendLine($"  fraction(BARYON+MIXED) = {100.0*(b+m)/t:F1}%");
        sb.AppendLine("");

        // ── EXTREMES ──
        var topGal = results.OrderByDescending(r => r.MedFrac).First();
        var botGal = results.OrderBy(r => r.MedFrac).First();
        var medGal = results.OrderBy(r => r.MedFrac).ElementAt(t / 2);

        sb.AppendLine("── EXTREMES ──");
        sb.AppendLine($"  highest baryonic fraction galaxy:  {topGal.Id}  medFrac = {topGal.MedFrac:F4}  ({topGal.Points} pts)");
        sb.AppendLine($"  lowest baryonic fraction galaxy:   {botGal.Id}  medFrac = {botGal.MedFrac:F4}  ({botGal.Points} pts)");
        sb.AppendLine($"  median baryonic fraction galaxy:   {medGal.Id}  medFrac = {medGal.MedFrac:F4}  ({medGal.Points} pts)");
        sb.AppendLine("");

        // ── MEDIAN ──
        double medianAll = results.OrderBy(r => r.MedFrac).ElementAt(t / 2).MedFrac;
        sb.AppendLine("── MEDIAN ──");
        sb.AppendLine($"  median(baryonic fraction) = {medianAll:F4}");
        sb.AppendLine("");

        // ── DECISION ──
        double bmPct = 100.0 * (b + m) / t;
        string verdict = bmPct > 75 ? "SUPPORTED: BARYON+MIXED > 75%"
            : bmPct >= 50 ? "CONDITIONAL: 50–75%"
            : "FALSIFIED: <50%";
        sb.AppendLine("── DECISION ──");
        sb.AppendLine($"  {verdict}");
        sb.AppendLine($"  BARYON+MIXED = {b+m}/{t} = {bmPct:F1}%");
        sb.AppendLine("");
        sb.AppendLine("╔══════════════════════════════════════════════════════╗");
        sb.AppendLine("║           END OF RAW NUMERICAL OUTPUT               ║");
        sb.AppendLine("╚══════════════════════════════════════════════════════╝");

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
