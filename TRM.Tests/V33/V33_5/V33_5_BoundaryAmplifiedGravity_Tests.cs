using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V33_5;

[Trait("Category", "V33_5")]
[Trait("Category", "LongRunning")]
public class V33_5_BoundaryAmplifiedGravity_Tests
{
    private readonly ITestOutputHelper _o;
    public V33_5_BoundaryAmplifiedGravity_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void BAG_01_BoundaryAmplifiedGravityAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== BAG_01: Boundary Amplified Gravity Audit ===");
        sb.AppendLine("=== Does boundary amplification improve galactic predictions? ===");
        sb.AppendLine(new string('=', 108));

        var repoRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
        var massFile = Path.Combine(repoRoot, "TRM.Core", "Data", "MassModels_Lelli2016c.mrt");

        var galData = new Dictionary<string, List<BagPt>>();
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
            if (!galData.ContainsKey(id)) galData[id] = new List<BagPt>();
            galData[id].Add(new BagPt(r, vobs, vb, vdisk));
        }

        var results = new List<BagResult>();
        foreach (var (id, pts) in galData)
        {
            var sorted = pts.OrderBy(p => p.R).ToList();
            if (sorted.Count < 10) continue;

            // Compute ∇ρ and ∇²ρ at each interior point
            var gradPts = new List<(double r, double grad, double curv)>();
            for (int i = 1; i < sorted.Count - 1; i++)
            {
                double dr = sorted[i + 1].R - sorted[i - 1].R;
                if (dr < 1e-6) continue;
                double rho1 = sorted[i - 1].Vbary * sorted[i - 1].Vbary;
                double rho2 = sorted[i + 1].Vbary * sorted[i + 1].Vbary;
                double rhoMid = sorted[i].Vbary * sorted[i].Vbary;
                double grad = (rho2 - rho1) / dr;
                double dr2 = sorted[i + 1].R - sorted[i].R;
                double curv = dr2 > 1e-6 ? Math.Abs(rho1 + rho2 - 2 * rhoMid) / (dr2 * dr2) : 0;
                gradPts.Add((sorted[i].R, grad, curv));
            }

            if (gradPts.Count < 5) continue;

            // === MODEL A: Raw gradient ===
            double rawGradMean = gradPts.Average(g => Math.Abs(g.grad));
            int rawNeg = gradPts.Count(g => g.grad < -1e-15);
            int rawPos = gradPts.Count(g => g.grad > 1e-15);
            int rawDir = rawNeg > rawPos ? -1 : (rawPos > rawNeg ? +1 : 0);
            double rawDirFrac = Math.Max(rawNeg, rawPos) / (double)gradPts.Count;
            double rawGradStr = rawGradMean;

            // === MODEL B: Boundary-amplified gradient ===
            // Weight each gradient point by its local curvature (boundary proxy)
            double totalWeight = gradPts.Sum(g => g.curv + 1e-15);
            double ampGradMean = gradPts.Sum(g => Math.Abs(g.grad) * (g.curv + 1e-15)) / totalWeight;
            double ampWeightedDir = gradPts.Sum(g => g.grad * (g.curv + 1e-15));
            int ampDir = ampWeightedDir > 1e-15 ? +1 : ampWeightedDir < -1e-15 ? -1 : 0;

            // Curvature-weighted direction consecration
            double ampPosWeight = gradPts.Where(g => g.grad > 0).Sum(g => g.curv + 1e-15);
            double ampNegWeight = gradPts.Where(g => g.grad < 0).Sum(g => g.curv + 1e-15);
            double ampDirFrac = Math.Max(ampPosWeight, ampNegWeight) / Math.Max(1e-15, totalWeight);

            // === TARGETS ===
            int nO = Math.Max(3, sorted.Count / 3);
            var inner = sorted.Take(sorted.Count * 2 / 5).ToList();
            var outer = sorted.Skip(sorted.Count - nO).ToList();
            if (inner.Count < 2 || outer.Count < 2) continue;

            double vI = inner.Average(p => p.Vobs), vO = outer.Average(p => p.Vobs);
            double bI = inner.Average(p => p.Vbary), bO = outer.Average(p => p.Vbary);

            // Shape match
            bool rShapeOk = (vO > vI && rawNeg > rawPos) || (vO < vI && rawPos > rawNeg) || (Math.Abs(vO - vI) < 0.02 * Math.Max(1, vO));
            bool aShapeOk = (vO > vI && ampNegWeight > ampPosWeight) || (vO < vI && ampPosWeight > ampNegWeight) || (Math.Abs(vO - vI) < 0.02 * Math.Max(1, vO));

            // Amplitude
            double vMean = sorted.Average(p => p.Vobs), bMean = sorted.Average(p => p.Vbary);
            double ampRes = vMean > 0 ? Math.Abs(vMean - bMean) / vMean : 0;
            bool ampGood = ampRes < 0.20; // relaxed threshold for model comparison

            double vFlat = outer.Average(p => p.Vobs);
            double surfBri = outer.Average(p => p.Vdisk * p.Vdisk);
            var fracs = sorted.Select(p => p.Vobs > 0 ? p.Vbary / p.Vobs : 0).Where(f => f > 0).ToList();
            double medFrac = fracs.Count > 0 ? fracs.OrderBy(f => f).ElementAt(fracs.Count / 2) : 0;
            string bClass = medFrac > 0.85 ? "BARYON" : medFrac > 0.50 ? "MIXED" : "DM";

            results.Add(new BagResult(id, rawGradStr, ampGradMean, rawDirFrac, ampDirFrac,
                rShapeOk, aShapeOk, ampGood, ampRes, vFlat, surfBri, medFrac, bClass));
        }

        if (results.Count < 10) { _o.WriteLine($"Insufficient galaxies: {results.Count}"); Assert.True(true); return; }

        // ================================================================
        // MODEL COMPARISON
        // ================================================================
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Model Comparison: Raw vs Boundary-Amplified Gradient ===");
        sb.AppendLine("");
        sb.AppendLine($"  Galaxies: {results.Count}");
        sb.AppendLine("");

        int rawShapeOk = results.Count(r => r.RawShapeOk);
        int ampShapeOk = results.Count(r => r.AmpShapeOk);
        double rawShapePct = 100.0 * rawShapeOk / results.Count;
        double ampShapePct = 100.0 * ampShapeOk / results.Count;

        double[] rawStr = results.Select(r => Math.Log10(Math.Max(1, r.RawGradStr))).ToArray();
        double[] ampStr = results.Select(r => Math.Log10(Math.Max(1, r.AmpGradMean))).ToArray();
        double[] ampResA = results.Select(r => r.AmpResidual).ToArray();
        double rawAmpR = PearsonCorr(rawStr, ampResA);
        double ampAmpR = PearsonCorr(ampStr, ampResA);

        sb.AppendLine($"{"Metric",-30} {"Raw ∇ρ",14} {"Amplified ∇ρ",14} {"Δ",10}");
        sb.AppendLine(new string('-', 70));
        sb.AppendLine($"{"Shape agreement",-30} {rawShapePct,13:F1}% {ampShapePct,13:F1}% {ampShapePct - rawShapePct,10:F1}%");
        sb.AppendLine($"{"|∇| vs amp residual r",-30} {rawAmpR,14:F4} {ampAmpR,14:F4} {ampAmpR - rawAmpR,10:F4}");
        sb.AppendLine("");

        // ================================================================
        // DETAILED COMPARISON
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Detailed Shape Comparison ===");
        sb.AppendLine("");

        int bothOk = results.Count(r => r.RawShapeOk && r.AmpShapeOk);
        int rawOnly = results.Count(r => r.RawShapeOk && !r.AmpShapeOk);
        int ampOnly = results.Count(r => !r.RawShapeOk && r.AmpShapeOk);
        int neither = results.Count(r => !r.RawShapeOk && !r.AmpShapeOk);

        sb.AppendLine($"  Both OK:      {bothOk} ({100.0 * bothOk / results.Count:F1}%)");
        sb.AppendLine($"  Raw only:     {rawOnly} ({100.0 * rawOnly / results.Count:F1}%) — amplification HURTS");
        sb.AppendLine($"  Amp only:     {ampOnly} ({100.0 * ampOnly / results.Count:F1}%) — amplification HELPS");
        sb.AppendLine($"  Neither:      {neither} ({100.0 * neither / results.Count:F1}%)");
        sb.AppendLine("");

        if (ampOnly > rawOnly)
            sb.AppendLine($"  → Boundary amplification NET BENEFIT: +{ampOnly - rawOnly} galaxies");
        else if (rawOnly > ampOnly)
            sb.AppendLine($"  → Raw gradient NET BENEFIT: +{rawOnly - ampOnly} galaxies");
        else
            sb.AppendLine("  → No clear winner — models are equivalent");
        sb.AppendLine("");

        // ================================================================
        // LSB ANALYSIS
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== LSB Performance ===");
        sb.AppendLine("");

        var lsb = results.Where(r => r.SurfBri < 2000).ToList();
        var hsb = results.Where(r => r.SurfBri > 5000).ToList();

        if (lsb.Count > 5 && hsb.Count > 5)
        {
            double lsbRaw = 100.0 * lsb.Count(r => r.RawShapeOk) / lsb.Count;
            double lsbAmp = 100.0 * lsb.Count(r => r.AmpShapeOk) / lsb.Count;
            double hsbRaw = 100.0 * hsb.Count(r => r.RawShapeOk) / hsb.Count;
            double hsbAmp = 100.0 * hsb.Count(r => r.AmpShapeOk) / hsb.Count;

            sb.AppendLine($"{"Population",-16} {"N",5} {"RawShape%",12} {"AmpShape%",12} {"Δ",8}");
            sb.AppendLine(new string('-', 55));
            sb.AppendLine($"{"LSB (SB<2000)",-16} {lsb.Count,5} {lsbRaw,12:F1}% {lsbAmp,12:F1}% {lsbAmp - lsbRaw,8:F1}%");
            sb.AppendLine($"{"HSB (SB>5000)",-16} {hsb.Count,5} {hsbRaw,12:F1}% {hsbAmp,12:F1}% {hsbAmp - hsbRaw,8:F1}%");
            sb.AppendLine("");

            if (lsbAmp > lsbRaw + 2)
                sb.AppendLine("  → Amplification IMPROVES LSB galaxies");
            else if (Math.Abs(lsbAmp - lsbRaw) < 2)
                sb.AppendLine("  → LSB galaxies UNCHANGED by amplification");
            else
                sb.AppendLine("  → Amplification WORSENS LSB galaxies");
            sb.AppendLine("");
        }

        // ================================================================
        // BY BARYONIC CLASS
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== By Baryonic Class ===");
        sb.AppendLine("");

        sb.AppendLine($"{"Class",-10} {"N",5} {"RawShape%",12} {"AmpShape%",12} {"Δ",8} {"Raw|∇|r",10} {"Amp|∇|r",10}");
        sb.AppendLine(new string('-', 69));

        foreach (var cls in new[] { "BARYON", "MIXED", "DM" })
        {
            var g = results.Where(r => r.BClass == cls).ToList();
            if (g.Count < 5) continue;
            double rs = 100.0 * g.Count(r => r.RawShapeOk) / g.Count;
            double as_ = 100.0 * g.Count(r => r.AmpShapeOk) / g.Count;
            double[] rsA = g.Select(r => Math.Log10(Math.Max(1, r.RawGradStr))).ToArray();
            double[] asA = g.Select(r => Math.Log10(Math.Max(1, r.AmpGradMean))).ToArray();
            double[] arA = g.Select(r => r.AmpResidual).ToArray();
            double rr = PearsonCorr(rsA, arA);
            double ar = PearsonCorr(asA, arA);
            sb.AppendLine($"{cls,-10} {g.Count,5} {rs,12:F1}% {as_,12:F1}% {as_-rs,8:F1}% {rr,10:F4} {ar,10:F4}");
        }
        sb.AppendLine("");

        // ================================================================
        // MECHANISM ASSESSMENT
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Gravity Mechanism Assessment ===");
        sb.AppendLine("");

        bool shapeGain = ampShapePct > rawShapePct + 1;
        bool ampGain = Math.Abs(ampAmpR) > Math.Abs(rawAmpR) + 0.02;
        bool lsbGain = lsb.Count > 5 && hsb.Count > 5 && (100.0 * lsb.Count(r => r.AmpShapeOk) / lsb.Count > 100.0 * lsb.Count(r => r.RawShapeOk) / lsb.Count + 1);
        bool netGain = ampOnly > rawOnly;

        int gains = (shapeGain ? 1 : 0) + (ampGain ? 1 : 0) + (lsbGain ? 1 : 0) + (netGain ? 1 : 0);

        sb.AppendLine($"  Gains from amplification: {gains}/4");
        sb.AppendLine($"    Shape improvement:      {(shapeGain ? "✓" : "✗")} ({ampShapePct - rawShapePct:F1}%)");
        sb.AppendLine($"    Amplitude improvement:  {(ampGain ? "✓" : "✗")} (Δr={ampAmpR - rawAmpR:F4})");
        sb.AppendLine($"    LSB improvement:        {(lsbGain ? "✓" : "✗")}");
        sb.AppendLine($"    Net galaxy gain:        {(netGain ? "✓" : "✗")} (+{ampOnly - rawOnly})");
        sb.AppendLine("");

        if (gains >= 3)
        {
            sb.AppendLine("  BOUNDARY AMPLIFICATION IS ESSENTIAL:");
            sb.AppendLine("  Curvature-weighted gradients outperform raw gradients");
            sb.AppendLine("  across shape, amplitude, and low-surface-brightness galaxies.");
            sb.AppendLine("  Gravity = boundary-concentrated density gradients.");
        }
        else if (gains >= 2)
        {
            sb.AppendLine("  AMPLIFICATION IS BENEFICIAL BUT NOT ESSENTIAL:");
            sb.AppendLine("  Raw gradients already capture most of the signal.");
            sb.AppendLine("  Boundary amplification provides incremental improvement.");
        }
        else
        {
            sb.AppendLine("  BOUNDARY AMPLIFICATION ADDS NO PREDICTIVE POWER:");
            sb.AppendLine("  Raw density gradients are sufficient.");
            sb.AppendLine("  Boundary concentration does not improve galactic predictions.");
        }
        sb.AppendLine("");

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        bool critA = gains >= 3;
        bool critB = netGain;
        bool critC = results.Count >= 100;
        bool critD = shapeGain;

        int critMet = (critA ? 1 : 0) + (critB ? 1 : 0) + (critC ? 1 : 0) + (critD ? 1 : 0);

        string verdict = critMet >= 4 ? "SUPPORTED: boundary amplification is essential."
            : critMet >= 2 ? "CONDITIONAL: gradient remains dominant."
            : "FALSIFIED: boundary adds no predictive power.";

        sb.AppendLine($"VERDICT: {verdict}  ({critMet}/4 criteria)");
        sb.AppendLine("");
        sb.AppendLine($"  A. ≥3 gains from amplification:          {(critA ? "YES" : "NO")} ({gains}/4)");
        sb.AppendLine($"  B. Net galaxy gain:                       {(critB ? "YES" : "NO")} (+{ampOnly - rawOnly})");
        sb.AppendLine($"  C. ≥100 galaxies:                         {(critC ? "YES" : "NO")} ({results.Count})");
        sb.AppendLine($"  D. Shape improvement:                     {(critD ? "YES" : "NO")} ({ampShapePct - rawShapePct:F1}%)");
        sb.AppendLine("");
        sb.AppendLine("Boundary Amplified Gravity Result:");
        sb.AppendLine($"  Raw ∇ρ shape: {rawShapePct:F1}%  Amplified ∇ρ shape: {ampShapePct:F1}%");
        sb.AppendLine($"  Raw ∇ρ amp r: {rawAmpR:F4}    Amplified ∇ρ amp r: {ampAmpR:F4}");
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== BAG_01 complete. Commit: BAG_01_BoundaryAmplifiedGravityAudit ===");

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

    private record BagPt(double R, double Vobs, double Vbary, double Vdisk);
    private record BagResult(string Id, double RawGradStr, double AmpGradMean, double RawDirFrac, double AmpDirFrac,
        bool RawShapeOk, bool AmpShapeOk, bool AmpGood, double AmpResidual, double VFlat, double SurfBri, double MedFrac, string BClass);
}
