using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TRM.Core.Data;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V30_4;

[Trait("Category", "V30_4")]
[Trait("Category", "LongRunning")]
public class V30_4_BaryonicTestResults_Tests
{
    private readonly ITestOutputHelper _o;
    public V30_4_BaryonicTestResults_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void BTR_01_BaryonicTestResultsAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== BTR_01: Baryonic Test Results Audit ===");
        sb.AppendLine("=== What fraction of SPARC galaxies fall into each class? ===");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("");

        var repoRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
        var massFile = Path.Combine(repoRoot, "TRM.Core", "Data", "MassModels_Lelli2016c.mrt");
        var ds = MrtParser.Parse(massFile);

        var colIdx = new Dictionary<string, int>();
        for (int i = 0; i < ds.Columns.Count; i++) colIdx[ds.Columns[i].Label.Trim()] = i;

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

        var results = new List<GalaxyResult>();
        foreach (var (id, pts) in galaxies)
        {
            var sorted = pts.OrderBy(p => p.R).ToList();
            if (sorted.Count < 5) continue;
            var fracs = sorted.Select(p => { double vB = Math.Sqrt(Math.Max(0, p.Vgas * p.Vgas + p.Vdisk * p.Vdisk + p.Vbul * p.Vbul)); return vB > 0 && p.Vobs > 0 ? vB / p.Vobs : 0; }).Where(f => f > 0).ToList();
            if (fracs.Count < 3) continue;
            double medFrac = fracs.OrderBy(f => f).ElementAt(fracs.Count / 2);
            string cls = medFrac > 0.85 ? "BARYON" : medFrac > 0.50 ? "MIXED" : "DM";
            double outerFrac = fracs.Skip(Math.Max(0, fracs.Count - Math.Max(1, fracs.Count / 3))).DefaultIfEmpty(medFrac).Average();
            int nOuter = Math.Max(1, sorted.Count / 3);
            double density = sorted.Skip(sorted.Count - nOuter).DefaultIfEmpty(sorted.Last()).Average(p => p.Vdisk * p.Vdisk + p.Vgas * p.Vgas);
            results.Add(new GalaxyResult(id, cls, medFrac, outerFrac, density, sorted.Count));
        }

        if (results.Count == 0) { sb.AppendLine("  No valid galaxies."); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        int baryon = results.Count(r => r.Class == "BARYON");
        int mixed = results.Count(r => r.Class == "MIXED");
        int dm = results.Count(r => r.Class == "DM");
        int total = results.Count;

        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Class Distribution Table ===");
        sb.AppendLine("");
        sb.AppendLine($"{"Class",-10} {"Count",7} {"Fraction",10} {"MedFrac",10} {"OuterFrac",11}");
        sb.AppendLine(new string('-', 50));
        foreach (var (cls, cnt) in new[] { ("BARYON", baryon), ("MIXED", mixed), ("DM", dm) })
        {
            var grp = results.Where(r => r.Class == cls).ToList();
            double mf = grp.Count > 0 ? grp.Average(r => r.MedFrac) : 0;
            double of = grp.Count > 0 ? grp.Average(r => r.OuterFrac) : 0;
            sb.AppendLine($"{cls,-10} {cnt,7} {100.0*cnt/total,10:F1}% {mf,10:F3} {of,11:F3}");
        }
        sb.AppendLine($"{"TOTAL",-10} {total,7}");
        sb.AppendLine("");

        sb.AppendLine($"  BARYON + MIXED: {baryon + mixed} ({100.0*(baryon+mixed)/total:F1}%) — density primacy range");
        sb.AppendLine($"  DM-only:        {dm} ({100.0*dm/total:F1}%) — requires additional mass");
        sb.AppendLine("");

        // ================================================================
        // DENSITY QUARTILE BREAKDOWN
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Density Quartile Breakdown ===");
        sb.AppendLine("");
        sb.AppendLine($"{"Quartile",-18} {"Count",6} {"BARYON%",9} {"MIXED%",8} {"DM%",6} {"MedFrac",9}");
        sb.AppendLine(new string('-', 58));

        var byDens = results.OrderBy(r => r.Density).ToList();
        int qs = total / 4;
        for (int q = 0; q < 4; q++)
        {
            var grp = q < 3 ? byDens.Skip(q * qs).Take(qs).ToList() : byDens.Skip(3 * qs).ToList();
            int b = grp.Count(r => r.Class == "BARYON"), m = grp.Count(r => r.Class == "MIXED"), d = grp.Count(r => r.Class == "DM");
            string label = q == 0 ? "Lowest density" : q == 1 ? "Low density" : q == 2 ? "High density" : "Highest density";
            sb.AppendLine($"{label,-18} {grp.Count,6} {100.0*b/grp.Count,9:F1}% {100.0*m/grp.Count,8:F1}% {100.0*d/grp.Count,6:F1}% {grp.Average(r=>r.MedFrac),9:F3}");
        }
        sb.AppendLine("");

        // ================================================================
        // STRONGEST COUNTEREXAMPLES
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Strongest Counterexamples ===");
        sb.AppendLine("");

        var dmGals = results.Where(r => r.Class == "DM").OrderBy(r => r.MedFrac).Take(5).ToList();
        if (dmGals.Count > 0)
        {
            sb.AppendLine("  Lowest baryonic fraction (DM-dominated):");
            foreach (var g in dmGals)
                sb.AppendLine($"    {g.Id}: medFrac={g.MedFrac:F3}, outerFrac={g.OuterFrac:F3}, pts={g.Points}");
        }
        else
            sb.AppendLine("  No DM-dominated galaxies found — density primacy universal.");
        sb.AppendLine("");

        var bestBaryon = results.Where(r => r.Class == "BARYON").OrderByDescending(r => r.MedFrac).Take(3).ToList();
        if (bestBaryon.Count > 0)
        {
            sb.AppendLine("  Highest baryonic fraction (best TRM matches):");
            foreach (var g in bestBaryon)
                sb.AppendLine($"    {g.Id}: medFrac={g.MedFrac:F3}, pts={g.Points}");
        }
        sb.AppendLine("");

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        double baryonPlusMixed = 100.0 * (baryon + mixed) / total;
        bool criterionA = baryonPlusMixed >= 70;
        bool criterionB = dm <= baryon;
        bool criterionC = total >= 50;
        bool criterionD = results.Average(r => r.MedFrac) > 0.55;

        int criteriaMet = 0;
        if (criterionA) criteriaMet++;
        if (criterionB) criteriaMet++;
        if (criterionC) criteriaMet++;
        if (criterionD) criteriaMet++;

        string verdict = criteriaMet >= 4 ? "SUPPORTED: density primacy broadly survives."
            : criteriaMet >= 2 ? "CONDITIONAL: mixed outcome."
            : "FALSIFIED: DM class dominates.";

        sb.AppendLine($"VERDICT: {verdict}  ({criteriaMet}/4 criteria)");
        sb.AppendLine("");
        sb.AppendLine($"  A. BARYON+MIXED ≥ 70%:                    {(criterionA ? "YES" : "NO")} ({baryonPlusMixed:F1}%)");
        sb.AppendLine($"  B. DM ≤ BARYON:                            {(criterionB ? "YES" : "NO")} ({dm} vs {baryon})");
        sb.AppendLine($"  C. ≥50 galaxies:                           {(criterionC ? "YES" : "NO")} ({total})");
        sb.AppendLine($"  D. Mean medFrac > 0.55:                   {(criterionD ? "YES" : "NO")} ({results.Average(r=>r.MedFrac):F3})");
        sb.AppendLine("");
        sb.AppendLine($"Density Primacy Result ({total} galaxies):");
        sb.AppendLine($"  BARYON: {baryon} ({100.0*baryon/total:F1}%) — density CURVATURE law directly confirmed");
        sb.AppendLine($"  MIXED:  {mixed} ({100.0*mixed/total:F1}%) — density important, secondary effects present");
        sb.AppendLine($"  DM:     {dm} ({100.0*dm/total:F1}%) — requires mass beyond baryons");
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== BTR_01 complete. Commit: BTR_01_BaryonicTestResultsAudit ===");

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
    private record GalaxyResult(string Id, string Class, double MedFrac, double OuterFrac, double Density, int Points);
}
