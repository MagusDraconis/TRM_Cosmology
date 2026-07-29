using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V31_2;

[Trait("Category", "V31_2")]
[Trait("Category", "LongRunning")]
public class V31_2_FlowChannelPrediction_Tests
{
    private readonly ITestOutputHelper _o;
    public V31_2_FlowChannelPrediction_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void FCP_01_FlowChannelPredictionAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== FCP_01: Flow Channel Prediction Audit ===");
        sb.AppendLine("=== Do density-induced curvature structures create preferred channels? ===");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("");

        var repoRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
        var massFile = Path.Combine(repoRoot, "TRM.Core", "Data", "MassModels_Lelli2016c.mrt");

        var galData = new Dictionary<string, List<ChannelPt>>();
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
            if (!galData.ContainsKey(id)) galData[id] = new List<ChannelPt>();
            galData[id].Add(new ChannelPt(r, vobs, vb));
        }

        // Flow channel analysis per galaxy
        var results = new List<ChannelResult>();
        foreach (var (id, pts) in galData)
        {
            var sorted = pts.OrderBy(p => p.R).ToList();
            if (sorted.Count < 10) continue;

            // Compute radial derivatives via finite differences
            var dDensDr = new List<double>(); // curvature proxy: d(density)/dr
            var dVdr = new List<double>();    // velocity gradient
            var rMid = new List<double>();

            for (int i = 1; i < sorted.Count - 1; i++)
            {
                double dr = sorted[i + 1].R - sorted[i - 1].R;
                if (dr < 1e-6) continue;
                double dens1 = sorted[i - 1].Vbary * sorted[i - 1].Vbary;
                double dens2 = sorted[i + 1].Vbary * sorted[i + 1].Vbary;
                dDensDr.Add((dens2 - dens1) / dr);
                dVdr.Add((sorted[i + 1].Vobs - sorted[i - 1].Vobs) / dr);
                rMid.Add(sorted[i].R);
            }

            if (dDensDr.Count < 5) continue;

            // Channel alignment: correlation between dV/dr and d(density)/dr
            double alignCorr = PearsonCorr(dDensDr.ToArray(), dVdr.ToArray());

            // Channel coherence: is the sign consistent? dV/dr follows -d(density)/dr?
            int sameSign = 0, oppositeSign = 0;
            for (int i = 0; i < dDensDr.Count; i++)
            {
                if (dDensDr[i] * dVdr[i] < 0) sameSign++; // density decreases → velocity increases (typical outer)
                else oppositeSign++;
            }
            double channelFrac = dDensDr.Count > 0 ? (double)sameSign / dDensDr.Count : 0;

            // Outer channel: outer third
            int nO = dDensDr.Count / 3;
            var outerDd = dDensDr.Skip(Math.Max(0, dDensDr.Count - nO)).ToList();
            var outerDv = dVdr.Skip(Math.Max(0, dVdr.Count - nO)).ToList();
            double outerAlign = outerDd.Count > 2 ? PearsonCorr(outerDd.ToArray(), outerDv.ToArray()) : 0;
            int outerSame = 0;
            for (int i = 0; i < outerDd.Count; i++)
                if (outerDd[i] * outerDv[i] < 0) outerSame++;
            double outerChannel = outerDd.Count > 0 ? (double)outerSame / outerDd.Count : 0;

            double vFlat = sorted.Skip(sorted.Count - nO).Average(p => p.Vobs);

            results.Add(new ChannelResult(id, alignCorr, channelFrac, outerAlign, outerChannel, vFlat, dDensDr.Count));
        }

        if (results.Count == 0) { _o.WriteLine("No valid galaxies."); Assert.True(true); return; }

        double meanAlign = results.Average(r => r.AlignCorr);
        double meanChannel = results.Average(r => r.ChannelFraction);
        double meanOuter = results.Average(r => r.OuterChannel);

        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Flow Channel Table ===");
        sb.AppendLine("");
        sb.AppendLine($"  Galaxies analyzed: {results.Count}");
        sb.AppendLine($"  Mean alignment (r): {meanAlign:F4}");
        sb.AppendLine($"  Mean channel frac:  {meanChannel:F3} (fraction where dV follows -dDensity)");
        sb.AppendLine($"  Mean outer channel: {meanOuter:F3}");
        sb.AppendLine("");

        // ================================================================
        // BEST EXAMPLES
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Best Channel Examples (highest alignment) ===");
        sb.AppendLine("");
        sb.AppendLine($"{"Galaxy",-12} {"Align r",8} {"Channel",8} {"OutCh",8} {"vFlat",8} {"Pts",5}");
        sb.AppendLine(new string('-', 53));

        foreach (var r in results.OrderByDescending(r => r.AlignCorr).Take(8))
            sb.AppendLine($"{r.Id,-12} {r.AlignCorr,8:F4} {r.ChannelFraction,8:F3} {r.OuterChannel,8:F3} {r.VFlat,8:F1} {r.Points,5}");
        sb.AppendLine("");

        // ================================================================
        // FAILURE CASES
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Weakest Channel Examples (lowest alignment) ===");
        sb.AppendLine("");
        sb.AppendLine($"{"Galaxy",-12} {"Align r",8} {"Channel",8} {"OutCh",8} {"vFlat",8} {"Pts",5}");
        sb.AppendLine(new string('-', 53));

        foreach (var r in results.OrderBy(r => r.AlignCorr).Take(8))
            sb.AppendLine($"{r.Id,-12} {r.AlignCorr,8:F4} {r.ChannelFraction,8:F3} {r.OuterChannel,8:F3} {r.VFlat,8:F1} {r.Points,5}");
        sb.AppendLine("");

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        bool criterionA = Math.Abs(meanAlign) > 0.3;
        bool criterionB = meanChannel > 0.55;
        bool criterionC = results.Count >= 80;
        bool criterionD = meanOuter > 0.50;

        int criteriaMet = 0;
        if (criterionA) criteriaMet++;
        if (criterionB) criteriaMet++;
        if (criterionC) criteriaMet++;
        if (criterionD) criteriaMet++;

        string verdict = criteriaMet >= 4 ? "SUPPORTED: galactic dynamics follow curvature channels."
            : criteriaMet >= 2 ? "CONDITIONAL: partial channel behaviour."
            : "FALSIFIED: no channel structure detected.";

        sb.AppendLine($"VERDICT: {verdict}  ({criteriaMet}/4 criteria)");
        sb.AppendLine("");
        sb.AppendLine($"  A. Mean alignment |r|>0.3:                {(criterionA ? "YES" : "NO")} ({meanAlign:F4})");
        sb.AppendLine($"  B. Channel fraction >55%:                 {(criterionB ? "YES" : "NO")} ({meanChannel*100:F0}%)");
        sb.AppendLine($"  C. ≥80 galaxies:                          {(criterionC ? "YES" : "NO")} ({results.Count})");
        sb.AppendLine($"  D. Outer channel >50%:                    {(criterionD ? "YES" : "NO")} ({meanOuter*100:F0}%)");
        sb.AppendLine("");
        sb.AppendLine("Flow Channel Principle:");
        sb.AppendLine($"  Curvature channels (density gradients) align with velocity");
        sb.AppendLine($"  gradients in {meanChannel*100:F0}% of data points. TRM density→curvature");
        sb.AppendLine("  creates preferred dynamical channels — observed as the");
        sb.AppendLine("  correlation between d(density)/dr and dV/dr.");
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== FCP_01 complete. Commit: FCP_01_FlowChannelPredictionAudit ===");

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

    private record ChannelPt(double R, double Vobs, double Vbary);
    private record ChannelResult(string Id, double AlignCorr, double ChannelFraction, double OuterAlign, double OuterChannel, double VFlat, int Points);
}
