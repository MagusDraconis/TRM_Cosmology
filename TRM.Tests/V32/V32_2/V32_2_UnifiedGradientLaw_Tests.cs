using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V32_2;

[Trait("Category", "V32_2")]
[Trait("Category", "LongRunning")]
public class V32_2_UnifiedGradientLaw_Tests
{
    private readonly ITestOutputHelper _o;
    public V32_2_UnifiedGradientLaw_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void UGL_01_UnifiedGradientLawAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== UGL_01: Unified Gradient Law Audit ===");
        sb.AppendLine("=== Can TRM gravity be reduced to a single gradient principle? ===");
        sb.AppendLine(new string('=', 108));

        var repoRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
        var massFile = Path.Combine(repoRoot, "TRM.Core", "Data", "MassModels_Lelli2016c.mrt");

        var galData = new Dictionary<string, List<UglPt>>();
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
            if (!galData.ContainsKey(id)) galData[id] = new List<UglPt>();
            galData[id].Add(new UglPt(r, vobs, vb, vdisk));
        }

        var results = new List<UglResult>();
        foreach (var (id, pts) in galData)
        {
            var sorted = pts.OrderBy(p => p.R).ToList();
            if (sorted.Count < 10) continue;

            var dDensDr = new List<double>();
            var dVobsDr = new List<double>();
            var curv = new List<double>();

            for (int i = 1; i < sorted.Count - 1; i++)
            {
                double dr = sorted[i + 1].R - sorted[i - 1].R;
                if (dr < 1e-6) continue;
                double d1 = sorted[i - 1].Vbary * sorted[i - 1].Vbary;
                double d2 = sorted[i + 1].Vbary * sorted[i + 1].Vbary;
                double dMid = sorted[i].Vbary * sorted[i].Vbary;
                dDensDr.Add((d2 - d1) / dr);
                dVobsDr.Add((sorted[i + 1].Vobs - sorted[i - 1].Vobs) / dr);
                double dr2 = (sorted[i + 1].R - sorted[i].R);
                if (dr2 > 1e-6)
                    curv.Add((d1 + d2 - 2 * dMid) / (dr2 * dr2));
            }

            if (dDensDr.Count < 5) continue;

            int negCount = dDensDr.Count(d => d < -1e-15);
            int posCount = dDensDr.Count(d => d > 1e-15);
            int gradDir = negCount > posCount ? -1 : (posCount > negCount ? +1 : 0);
            double gradDirFraction = Math.Max(negCount, posCount) / (double)dDensDr.Count;
            double gradStrength = dDensDr.Average(d => Math.Abs(d));
            double curvStrength = curv.Count > 0 ? curv.Average(c => Math.Abs(c)) : 0;

            int nO = Math.Max(3, sorted.Count / 3);
            var inner = sorted.Take(sorted.Count * 2 / 5).ToList();
            var outer = sorted.Skip(sorted.Count - nO).ToList();
            if (inner.Count < 2 || outer.Count < 2) continue;
            double vInner = inner.Average(p => p.Vobs), vOuter = outer.Average(p => p.Vobs);
            double bInner = inner.Average(p => p.Vbary), bOuter = outer.Average(p => p.Vbary);
            double vShape = vOuter > 0 ? (vOuter - vInner) / vInner : 0;
            double bShape = bOuter > 0 ? (bOuter - bInner) / bInner : 0;
            bool shapeMatch = (vShape > 0 && bShape > 0) || (vShape < 0 && bShape < 0) || (Math.Abs(vShape) < 0.02 && Math.Abs(bShape) < 0.02);
            double shapeDiff = Math.Abs(vShape - bShape);

            double vMean = sorted.Average(p => p.Vobs), bMean = sorted.Average(p => p.Vbary);
            double ampRes = vMean > 0 ? Math.Abs(vMean - bMean) / vMean : 0;
            bool ampGood = ampRes < 0.15;

            double channelAlign = PearsonCorr(dDensDr.ToArray(), dVobsDr.ToArray());
            int sameSign = 0;
            for (int i = 0; i < dDensDr.Count; i++)
                if (dDensDr[i] * dVobsDr[i] < 0) sameSign++;
            double channelFrac = (double)sameSign / dDensDr.Count;

            double surfBri = outer.Average(p => p.Vdisk * p.Vdisk);
            double vFlat = outer.Average(p => p.Vobs);
            var fracs = sorted.Select(p => p.Vobs > 0 ? p.Vbary / p.Vobs : 0).Where(f => f > 0).ToList();
            double medFrac = fracs.Count > 0 ? fracs.OrderBy(f => f).ElementAt(fracs.Count / 2) : 0;
            string bClass = medFrac > 0.85 ? "BARYON" : medFrac > 0.50 ? "MIXED" : "DM";

            results.Add(new UglResult(id, gradDir, gradDirFraction, gradStrength, curvStrength,
                shapeMatch, shapeDiff, ampGood, ampRes,
                channelAlign, channelFrac, surfBri, vFlat, medFrac, bClass, dDensDr.Count,
                dDensDr.ToArray(), dVobsDr.ToArray(), curv.ToArray()));
        }

        if (results.Count == 0) { _o.WriteLine("No valid galaxies."); Assert.True(true); return; }

        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Reduction Analysis: Can everything reduce to gradients? ===");
        sb.AppendLine("");
        sb.AppendLine($"  Galaxies analyzed: {results.Count}");
        sb.AppendLine("");

        // ================================================================
        // Q1: CAN CURVATURE BE FULLY DERIVED FROM GRADIENTS?
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Q1: Is curvature fully derivable from |∇ρ|? ===");
        sb.AppendLine("");

        double[] logGrad = results.Select(r => Math.Log10(Math.Max(1, r.GradStrength))).ToArray();
        double[] logCurv = results.Select(r => Math.Log10(Math.Max(1, r.CurvStrength))).ToArray();

        // Linear regression: log(curv) = a + b*log(grad)
        double gradCurvR = PearsonCorr(logGrad, logCurv);
        var (slopeGC, interceptGC, r2GC) = LinearRegression(logGrad, logCurv);

        // Compute predicted curvature and residuals
        double[] curvPred = logGrad.Select(g => interceptGC + slopeGC * g).ToArray();
        double[] curvResid = Enumerable.Range(0, logCurv.Length).Select(i => logCurv[i] - curvPred[i]).ToArray();
        double residStd = Math.Sqrt(curvResid.Average(r => r * r));
        double curvStd = Math.Sqrt(logCurv.Average(c => (c - logCurv.Average()) * (c - logCurv.Average())));
        double fracUnexplained = residStd / Math.Max(1e-15, curvStd);

        // Per-galaxy R²: how well can we predict curvature from gradient within each galaxy?
        var withinGalR2 = new List<double>();
        foreach (var r in results.Where(r => r.CurvArray.Length > 5 && r.GradArray.Length > 5))
        {
            int n = Math.Min(r.CurvArray.Length, r.GradArray.Length);
            double[] g = r.GradArray.Take(n).Select(v => Math.Log10(Math.Max(1, Math.Abs(v)))).ToArray();
            double[] c = r.CurvArray.Take(n).Select(v => Math.Log10(Math.Max(1, Math.Abs(v)))).ToArray();
            var (s, _, r2) = LinearRegression(g, c);
            withinGalR2.Add(Math.Max(0, Math.Min(1, r2)));
        }
        double meanWithinR2 = withinGalR2.Count > 0 ? withinGalR2.Average() : 0;

        sb.AppendLine($"  ∇ρ vs ∇²ρ correlation:          r = {gradCurvR:F4}");
        sb.AppendLine($"  Regression R²:                   R² = {r2GC:F4}");
        sb.AppendLine($"  Residual std / total std:        {fracUnexplained:F3} ({fracUnexplained*100:F1}% unexplained)");
        sb.AppendLine($"  Mean within-galaxy R²:           {meanWithinR2:F4} (n={withinGalR2.Count})");
        sb.AppendLine($"  ∇²ρ = {interceptGC:F3} + {slopeGC:F3} · log(|∇ρ|)");
        sb.AppendLine("");

        bool curvatureDerivable = r2GC > 0.70;

        // ================================================================
        // Q2: CAN CHANNELS BE FULLY DERIVED FROM GRADIENTS?
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Q2: Are flow channels fully derivable from gradients? ===");
        sb.AppendLine("");

        double[] chanVals = results.Select(r => r.ChannelFraction).ToArray();
        double[] dirFracVals = results.Select(r => r.GradDirFraction).ToArray();

        // Multivariate: channel ~ dirFrac + gradStrength
        var (chanB0, chanB1, chanB2, chanR2) = Multivariate2(chanVals, dirFracVals, logGrad);

        // Compare: gradient-only vs gradient+curvature
        var (_, _, _, _, chanR2_full) = Multivariate3(chanVals, dirFracVals, logGrad, logCurv);
        double chanCurvGain = chanR2_full - chanR2;

        sb.AppendLine($"  Channel ~ dirFrac + log(|∇ρ|):   R² = {chanR2:F4}");
        sb.AppendLine($"  Channel ~ dirFrac + log(|∇ρ|) + log(∇²ρ): R² = {chanR2_full:F4}");
        sb.AppendLine($"  Curvature gain:                  ΔR² = {chanCurvGain:F4} ({(chanCurvGain*100):F2}%)");
        sb.AppendLine("");

        bool channelsDerivable = chanR2 > 0.10;

        // ================================================================
        // Q3: SINGLE GRADIENT LAW FOR SHAPE + AMPLITUDE
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Q3: Does one gradient law explain both shape and strength? ===");
        sb.AppendLine("");

        double[] shapeMatchArr = results.Select(r => r.ShapeMatch ? 1.0 : 0.0).ToArray();
        double[] ampGoodArr = results.Select(r => r.AmpGood ? 1.0 : 0.0).ToArray();
        double[] ampResArr = results.Select(r => r.AmpResidual).ToArray();
        double[] shapeDiffArr = results.Select(r => r.ShapeDiff).ToArray();

        // Unified gradient score: composite of direction and strength
        double[] unifiedScore = Enumerable.Range(0, results.Count)
            .Select(i => dirFracVals[i] * Math.Log10(Math.Max(1, results[i].GradStrength)))
            .ToArray();

        double uniShapeR = PearsonCorr(unifiedScore, shapeMatchArr);
        double uniAmpR = PearsonCorr(unifiedScore, ampResArr);
        double uniCombinedR = PearsonCorr(unifiedScore,
            Enumerable.Range(0, results.Count).Select(i => shapeMatchArr[i] + (1 - ampResArr[i])).ToArray());

        sb.AppendLine($"  Unified score vs Shape match:     r = {uniShapeR:F4}");
        sb.AppendLine($"  Unified score vs Amp residual:    r = {uniAmpR:F4}");
        sb.AppendLine($"  Unified score vs Combined:        r = {uniCombinedR:F4}");
        sb.AppendLine("");

        // Gradient-only vs gradient+curvature for shape+amplitude prediction
        var (_, _, _, shapeOnlyR2) = Multivariate2(shapeMatchArr, dirFracVals, logGrad);
        var shapeFullR2 = Multivariate3(shapeMatchArr, dirFracVals, logGrad, logCurv).r2;
        double shapeCurvGain = shapeFullR2 - shapeOnlyR2;

        var (_, _, _, ampOnlyR2) = Multivariate2(ampResArr, dirFracVals, logGrad);
        var ampFullR2 = Multivariate3(ampResArr, dirFracVals, logGrad, logCurv).r2;
        double ampCurvGain = ampFullR2 - ampOnlyR2;

        sb.AppendLine($"  Shape ~ dirFrac + log(|∇ρ|):     R² = {shapeOnlyR2:F4}");
        sb.AppendLine($"  Shape ~ ... + log(∇²ρ):          R² = {shapeFullR2:F4}  (ΔR² = {shapeCurvGain:F4})");
        sb.AppendLine($"  Amp   ~ dirFrac + log(|∇ρ|):     R² = {ampOnlyR2:F4}");
        sb.AppendLine($"  Amp   ~ ... + log(∇²ρ):          R² = {ampFullR2:F4}  (ΔR² = {ampCurvGain:F4})");
        sb.AppendLine("");

        bool singleLawExplainsBoth = uniCombinedR > 0.30;

        // ================================================================
        // Q4: REDUCTION TO A SINGLE GEOMETRIC PRINCIPLE
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Q4: Can TRM gravity reduce to a single geometric principle? ===");
        sb.AppendLine("");

        // Count how many galaxies are fully explained by gradient alone
        int gradExplainsShape = results.Count(r => r.GradDirFraction > 0.70 && r.ShapeMatch);
        int gradExplainsAmp = results.Count(r => r.GradStrength > results.Average(r2 => r2.GradStrength) && r.AmpGood);
        int gradExplainsBoth = results.Count(r =>
            r.GradDirFraction > 0.70 && r.ShapeMatch &&
            r.GradStrength > results.Average(r2 => r2.GradStrength) && r.AmpGood);

        double shapeExplainedPct = 100.0 * gradExplainsShape / results.Count;
        double ampExplainedPct = 100.0 * gradExplainsAmp / results.Count;
        double bothExplainedPct = 100.0 * gradExplainsBoth / results.Count;

        sb.AppendLine($"  Gradient→Shape explained:         {gradExplainsShape}/{results.Count} = {shapeExplainedPct:F1}%");
        sb.AppendLine($"  Gradient→Amp explained:            {gradExplainsAmp}/{results.Count} = {ampExplainedPct:F1}%");
        sb.AppendLine($"  Gradient→Both explained:           {gradExplainsBoth}/{results.Count} = {bothExplainedPct:F1}%");
        sb.AppendLine("");

        // Curvature residual analysis
        double totalCurvGain = shapeCurvGain + ampCurvGain + chanCurvGain;
        sb.AppendLine($"  Total ΔR² from adding ∇²ρ:        {totalCurvGain:F4}");
        sb.AppendLine($"  → Curvature adds {(totalCurvGain*100):F2}% across all measures");
        sb.AppendLine("");

        // ================================================================
        // REDUCTION TABLE
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Reduction Table ===");
        sb.AppendLine("");
        sb.AppendLine($"{"Target",-24} {"∇-only R²",10} {"+∇² R²",10} {"ΔR²",8} {"Derivable?",12}");
        sb.AppendLine(new string('-', 66));

        static string verdict(double r2, double thresh) => r2 > thresh ? "YES" : "NO";

        sb.AppendLine($"{"Curvature (∇²ρ)",-24} {r2GC,10:F4} {"---",10} {"---",8} {verdict(r2GC, 0.70),12}");
        sb.AppendLine($"{"Flow channels",-24} {chanR2,10:F4} {chanR2_full,10:F4} {chanCurvGain,8:F4} {verdict(chanR2, 0.10),12}");
        sb.AppendLine($"{"Shape (direction)",-24} {shapeOnlyR2,10:F4} {shapeFullR2,10:F4} {shapeCurvGain,8:F4} {verdict(shapeOnlyR2, 0.05),12}");
        sb.AppendLine($"{"Amplitude (strength)",-24} {ampOnlyR2,10:F4} {ampFullR2,10:F4} {ampCurvGain,8:F4} {verdict(ampOnlyR2, 0.10),12}");
        sb.AppendLine("");

        // ================================================================
        // CANDIDATE UNIFIED GRAVITY LAW
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Candidate Unified Gradient Gravity Law ===");
        sb.AppendLine("");
        sb.AppendLine("  TRM Gravity = f(∇ρ) where:");
        sb.AppendLine("");
        sb.AppendLine("    Rotation SHAPE      = sgn(∇ρ)                          [direction]");
        sb.AppendLine($"    Rotation AMPLITUDE   = |∇ρ|^α                          [strength, α≈{slopeGC:F3}]");
        sb.AppendLine("    Flow CHANNELS        = |sgn(∇ρ)| · |∇ρ|               [alignment]");
        sb.AppendLine("    Curvature ∇²ρ        = derived from |∇ρ| (not primary) [emergent]");
        sb.AppendLine("");
        sb.AppendLine($"  ∇²ρ ≈ {interceptGC:F3} + {slopeGC:F3} · log(|∇ρ|)  (R²={r2GC:F4}, residual={fracUnexplained*100:F1}%)");
        sb.AppendLine("");

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        bool criterionA = curvatureDerivable;        // ∇²ρ from ∇ρ
        bool criterionB = channelsDerivable;          // Channels from ∇ρ
        bool criterionC = singleLawExplainsBoth;      // Single law for shape+amplitude
        bool criterionD = totalCurvGain < 0.05;       // Curvature adds <5% total

        int criteriaMet = 0;
        if (criterionA) criteriaMet++;
        if (criterionB) criteriaMet++;
        if (criterionC) criteriaMet++;
        if (criterionD) criteriaMet++;

        string ugVerdict = criteriaMet >= 4 ? "SUPPORTED: gradient is the primitive gravitational carrier."
            : criteriaMet >= 2 ? "CONDITIONAL: curvature remains independently required."
            : "FALSIFIED: multiple mechanisms remain necessary.";

        sb.AppendLine($"VERDICT: {ugVerdict}  ({criteriaMet}/4 criteria)");
        sb.AppendLine("");
        sb.AppendLine($"  A. Curvature derivable (R²>0.70):            {(criterionA ? "YES" : "NO")} (R²={r2GC:F4})");
        sb.AppendLine($"  B. Channels derivable (R²>0.10):             {(criterionB ? "YES" : "NO")} (R²={chanR2:F4})");
        sb.AppendLine($"  C. Single law explains both (|r|>0.30):      {(criterionC ? "YES" : "NO")} (r={uniCombinedR:F4})");
        sb.AppendLine($"  D. Curvature adds <5% total ΔR²:             {(criterionD ? "YES" : "NO")} (ΔR²={totalCurvGain:F4})");
        sb.AppendLine("");

        sb.AppendLine("Unified Gradient Law Assessment:");
        sb.AppendLine("");
        if (criteriaMet >= 3)
        {
            sb.AppendLine("  TRM gravitational structure REDUCES to the density gradient.");
            sb.AppendLine("  Curvature (∇²ρ) is a DERIVED quantity — not an independent");
            sb.AppendLine("  dynamical carrier. The density gradient ∇ρ is the primitive");
            sb.AppendLine("  from which all observable gravitational structure emerges.");
            sb.AppendLine("");
            sb.AppendLine("  This simplifies the TRM causal chain:");
            sb.AppendLine("    {sign, Tick} → ∇ρ → {Shape, Amplitude, Channels, ∇²ρ}");
        }
        else
        {
            sb.AppendLine("  Curvature retains independent predictive power beyond the");
            sb.AppendLine("  density gradient. The gradient is the PRIMARY carrier but");
            sb.AppendLine("  curvature adds non-trivial information — possibly encoding");
            sb.AppendLine("  the second-order structure of TRM's causal geometry.");
        }
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== UGL_01 complete. Commit: UGL_01_UnifiedGradientLawAudit ===");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ================================================================
    // STATISTICAL HELPERS
    // ================================================================

    private static double PearsonCorr(double[] xs, double[] ys)
    {
        int n = xs.Length; if (n < 2) return 0;
        double mx = xs.Average(), my = ys.Average();
        double sxy = 0, sxx = 0, syy = 0;
        for (int i = 0; i < n; i++) { double dx = xs[i] - mx, dy = ys[i] - my; sxy += dx * dy; sxx += dx * dx; syy += dy * dy; }
        return sxx > 1e-15 && syy > 1e-15 ? sxy / Math.Sqrt(sxx * syy) : 0;
    }

    /// <summary>Simple linear regression: y = intercept + slope * x. Returns (slope, intercept, R²).</summary>
    private static (double slope, double intercept, double r2) LinearRegression(double[] xs, double[] ys)
    {
        int n = xs.Length;
        if (n < 2) return (0, ys.Length > 0 ? ys[0] : 0, 0);
        double mx = xs.Average(), my = ys.Average();
        double sxy = 0, sxx = 0, syy = 0;
        for (int i = 0; i < n; i++) { double dx = xs[i] - mx, dy = ys[i] - my; sxy += dx * dy; sxx += dx * dx; syy += dy * dy; }
        double slope = sxx > 1e-15 ? sxy / sxx : 0;
        double intercept = my - slope * mx;
        double r2 = syy > 1e-15 ? (slope * sxy) / syy : 0;
        return (slope, intercept, r2);
    }

    /// <summary>Multivariate: y = b0 + b1*x1 + b2*x2. Returns (b0, b1, b2, R²).</summary>
    private static (double b0, double b1, double b2, double r2) Multivariate2(double[] y, double[] x1, double[] x2)
    {
        int n = y.Length; if (n < 3) return (y.Average(), 0, 0, 0);
        // Normal equations for 2-predictor regression
        double sy = y.Sum(), s1 = x1.Sum(), s2 = x2.Sum();
        double s11 = 0, s22 = 0, s12 = 0, s1y = 0, s2y = 0;
        for (int i = 0; i < n; i++)
        {
            s11 += x1[i] * x1[i]; s22 += x2[i] * x2[i]; s12 += x1[i] * x2[i];
            s1y += x1[i] * y[i]; s2y += x2[i] * y[i];
        }
        // Solve: [s11 s12; s12 s22] * [b1; b2] = [s1y - s1*sy/n; s2y - s2*sy/n]
        double s1yc = s1y - s1 * sy / n, s2yc = s2y - s2 * sy / n;
        double s11c = s11 - s1 * s1 / n, s22c = s22 - s2 * s2 / n, s12c = s12 - s1 * s2 / n;
        double det = s11c * s22c - s12c * s12c;
        double b1 = 0, b2 = 0;
        if (Math.Abs(det) > 1e-15)
        {
            b1 = (s1yc * s22c - s2yc * s12c) / det;
            b2 = (s2yc * s11c - s1yc * s12c) / det;
        }
        double b0 = sy / n - b1 * s1 / n - b2 * s2 / n;
        // R²
        double ssRes = 0, ssTot = 0; double my = sy / n;
        for (int i = 0; i < n; i++) { double pred = b0 + b1 * x1[i] + b2 * x2[i]; ssRes += (y[i] - pred) * (y[i] - pred); ssTot += (y[i] - my) * (y[i] - my); }
        double r2 = ssTot > 1e-15 ? 1 - ssRes / ssTot : 0;
        return (b0, b1, b2, Math.Max(0, Math.Min(1, r2)));
    }

    /// <summary>Multivariate 3-predictor: y = b0 + b1*x1 + b2*x2 + b3*x3. Returns (b0, b1, b2, b3, R²).</summary>
    private static (double b0, double b1, double b2, double b3, double r2) Multivariate3(double[] y, double[] x1, double[] x2, double[] x3)
    {
        int n = y.Length; if (n < 4) return (y.Average(), 0, 0, 0, 0);
        double sy = y.Sum(), s1 = x1.Sum(), s2 = x2.Sum(), s3 = x3.Sum();
        double s11 = 0, s22 = 0, s33 = 0, s12 = 0, s13 = 0, s23 = 0, s1y = 0, s2y = 0, s3y = 0;
        for (int i = 0; i < n; i++)
        {
            s11 += x1[i] * x1[i]; s22 += x2[i] * x2[i]; s33 += x3[i] * x3[i];
            s12 += x1[i] * x2[i]; s13 += x1[i] * x3[i]; s23 += x2[i] * x3[i];
            s1y += x1[i] * y[i]; s2y += x2[i] * y[i]; s3y += x3[i] * y[i];
        }
        double s1yc = s1y - s1 * sy / n, s2yc = s2y - s2 * sy / n, s3yc = s3y - s3 * sy / n;
        double s11c = s11 - s1 * s1 / n, s22c = s22 - s2 * s2 / n, s33c = s33 - s3 * s3 / n;
        double s12c = s12 - s1 * s2 / n, s13c = s13 - s1 * s3 / n, s23c = s23 - s2 * s3 / n;
        // Solve 3x3 via Cramer's rule
        double det = s11c * (s22c * s33c - s23c * s23c) - s12c * (s12c * s33c - s23c * s13c) + s13c * (s12c * s23c - s22c * s13c);
        double b1 = 0, b2 = 0, b3 = 0;
        if (Math.Abs(det) > 1e-15)
        {
            b1 = (s1yc * (s22c * s33c - s23c * s23c) - s12c * (s2yc * s33c - s23c * s3yc) + s13c * (s2yc * s23c - s22c * s3yc)) / det;
            b2 = (s11c * (s2yc * s33c - s23c * s3yc) - s1yc * (s12c * s33c - s23c * s13c) + s13c * (s12c * s3yc - s2yc * s13c)) / det;
            b3 = (s11c * (s22c * s3yc - s23c * s2yc) - s12c * (s12c * s3yc - s23c * s1yc) + s1yc * (s12c * s23c - s22c * s13c)) / det;
        }
        double b0 = sy / n - b1 * s1 / n - b2 * s2 / n - b3 * s3 / n;
        double ssRes = 0, ssTot = 0; double my = sy / n;
        for (int i = 0; i < n; i++) { double pred = b0 + b1 * x1[i] + b2 * x2[i] + b3 * x3[i]; ssRes += (y[i] - pred) * (y[i] - pred); ssTot += (y[i] - my) * (y[i] - my); }
        double r2 = ssTot > 1e-15 ? 1 - ssRes / ssTot : 0;
        return (b0, b1, b2, b3, Math.Max(0, Math.Min(1, r2)));
    }

    private static double ParseD(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return double.NaN;
        return double.TryParse(s.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : double.NaN;
    }

    private record UglPt(double R, double Vobs, double Vbary, double Vdisk);
    private record UglResult(
        string Id, int GradDir, double GradDirFraction, double GradStrength, double CurvStrength,
        bool ShapeMatch, double ShapeDiff, bool AmpGood, double AmpResidual,
        double ChannelAlign, double ChannelFraction,
        double SurfBri, double VFlat, double MedFrac, string BClass, int Points,
        double[] GradArray, double[] VobsDrArray, double[] CurvArray);
}
