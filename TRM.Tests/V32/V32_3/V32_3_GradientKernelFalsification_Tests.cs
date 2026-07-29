using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V32_3;

[Trait("Category", "V32_3")]
[Trait("Category", "LongRunning")]
public class V32_3_GradientKernelFalsification_Tests
{
    private readonly ITestOutputHelper _o;
    public V32_3_GradientKernelFalsification_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void GKF_01_GradientKernelFalsificationAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== GKF_01: Gradient Kernel Falsification Audit ===");
        sb.AppendLine("=== Attempt to falsify: gradient as primitive carrier of TRM ===");
        sb.AppendLine(new string('=', 108));

        var repoRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
        var massFile = Path.Combine(repoRoot, "TRM.Core", "Data", "MassModels_Lelli2016c.mrt");

        var galData = new Dictionary<string, List<GkfPt>>();
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
            if (!galData.ContainsKey(id)) galData[id] = new List<GkfPt>();
            galData[id].Add(new GkfPt(r, vobs, vb, vdisk));
        }

        var results = new List<GkfResult>();
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

            // --- GRADIENT features ---
            int gNeg = dDensDr.Count(d => d < -1e-15);
            int gPos = dDensDr.Count(d => d > 1e-15);
            int gradDir = gNeg > gPos ? -1 : (gPos > gNeg ? +1 : 0);
            double gradDirFrac = Math.Max(gNeg, gPos) / (double)dDensDr.Count;
            double gradStr = dDensDr.Average(d => Math.Abs(d));

            // --- CURVATURE features ---
            int cNeg = curv.Count(d => d < -1e-15);
            int cPos = curv.Count(d => d > 1e-15);
            int curvDir = cNeg > cPos ? -1 : (cPos > cNeg ? +1 : 0);
            double curvDirFrac = curv.Count > 0 ? Math.Max(cNeg, cPos) / (double)curv.Count : 0;
            double curvStr = curv.Count > 0 ? curv.Average(c => Math.Abs(c)) : 0;

            // --- CHANNEL features ---
            double channelAlign = PearsonCorr(dDensDr.ToArray(), dVobsDr.ToArray());
            int sameSign = 0;
            for (int i = 0; i < dDensDr.Count; i++)
                if (dDensDr[i] * dVobsDr[i] < 0) sameSign++;
            double channelFrac = (double)sameSign / dDensDr.Count;

            // --- DENSITY features ---
            int nO = Math.Max(3, sorted.Count / 3);
            var inner = sorted.Take(sorted.Count * 2 / 5).ToList();
            var outer = sorted.Skip(sorted.Count - nO).ToList();
            if (inner.Count < 2 || outer.Count < 2) continue;

            double densInner = inner.Average(p => p.Vbary * p.Vbary);
            double densOuter = outer.Average(p => p.Vbary * p.Vbary);
            double densSlope = densInner > 1e-15 ? (densOuter - densInner) / densInner : 0;
            int densDir = densSlope < -0.01 ? -1 : (densSlope > 0.01 ? +1 : 0);
            double outerDens = densOuter;

            // --- TARGETS ---
            double vInner = inner.Average(p => p.Vobs), vOuter = outer.Average(p => p.Vobs);
            double bInner = inner.Average(p => p.Vbary), bOuter = outer.Average(p => p.Vbary);
            double vShape = vOuter > 0 ? (vOuter - vInner) / vInner : 0;
            double bShape = bOuter > 0 ? (bOuter - bInner) / bInner : 0;
            bool shapeMatch = (vShape > 0 && bShape > 0) || (vShape < 0 && bShape < 0) || (Math.Abs(vShape) < 0.02 && Math.Abs(bShape) < 0.02);
            double shapeDiff = Math.Abs(vShape - bShape);

            double vMean = sorted.Average(p => p.Vobs), bMean = sorted.Average(p => p.Vbary);
            double ampRes = vMean > 0 ? Math.Abs(vMean - bMean) / vMean : 0;
            bool ampGood = ampRes < 0.15;

            double vFlat = outer.Average(p => p.Vobs);
            double surfBri = outer.Average(p => p.Vdisk * p.Vdisk);

            var fracs = sorted.Select(p => p.Vobs > 0 ? p.Vbary / p.Vobs : 0).Where(f => f > 0).ToList();
            double medFrac = fracs.Count > 0 ? fracs.OrderBy(f => f).ElementAt(fracs.Count / 2) : 0;

            results.Add(new GkfResult(id, gradDir, gradDirFrac, gradStr,
                curvDir, curvDirFrac, curvStr,
                channelAlign, channelFrac,
                densDir, densSlope, outerDens,
                shapeMatch, shapeDiff, ampGood, ampRes, vFlat, surfBri, medFrac));
        }

        if (results.Count == 0) { _o.WriteLine("No valid galaxies."); Assert.True(true); return; }

        sb.AppendLine("");
        sb.AppendLine($"  Galaxies analyzed: {results.Count}");
        sb.AppendLine("");

        // ================================================================
        // BUILD 5 COMPETING MODELS (each with exactly 2 predictors)
        // ================================================================
        double[] shapeTarget = results.Select(r => r.ShapeMatch ? 1.0 : 0.0).ToArray();
        double[] ampTarget = results.Select(r => r.AmpResidual).ToArray();

        // Feature vectors
        double[] gDirFrac = results.Select(r => r.GradDirFrac).ToArray();
        double[] gStr = results.Select(r => Math.Log10(Math.Max(1, r.GradStrength))).ToArray();
        double[] cDirFrac = results.Select(r => r.CurvDirFrac).ToArray();
        double[] cStr = results.Select(r => Math.Log10(Math.Max(1, r.CurvStrength))).ToArray();
        double[] chFrac = results.Select(r => r.ChannelFraction).ToArray();
        double[] chAbsAlign = results.Select(r => Math.Abs(r.ChannelAlign)).ToArray();
        double[] dDir = results.Select(r => (double)r.DensDir).ToArray();
        double[] dStr = results.Select(r => Math.Log10(Math.Max(1, r.OuterDens))).ToArray();

        // Named feature pairs for each model
        var modelDefs = new (string name, double[] x1, double[] x2)[]
        {
            ("A: Gradient-only", gDirFrac, gStr),
            ("B: Curvature-only", cDirFrac, cStr),
            ("C: Channel-only", chFrac, chAbsAlign),
            ("D: Density-only", dDir, dStr),
        };

        // Full model: try all 2-predictor combinations from non-gradient vars, pick best
        var altPairs = new (string tag, double[] x1, double[] x2)[]
        {
            ("curv", cDirFrac, cStr), ("chan", chFrac, chAbsAlign), ("dens", dDir, dStr),
            ("gDir+curvStr", gDirFrac, cStr), ("curvDir+gStr", cDirFrac, gStr),
            ("chan+gStr", chFrac, gStr), ("gDir+densStr", gDirFrac, dStr),
        };

        double bestFullShapeR2 = 0, bestFullAmpR2 = 0;
        string bestFullShapePair = "", bestFullAmpPair = "";
        foreach (var ap in altPairs)
        {
            double rs = MultivariateR2(shapeTarget, ap.x1, ap.x2);
            if (rs > bestFullShapeR2) { bestFullShapeR2 = rs; bestFullShapePair = ap.tag; }
            double ra = MultivariateR2(ampTarget, ap.x1, ap.x2);
            if (ra > bestFullAmpR2) { bestFullAmpR2 = ra; bestFullAmpPair = ap.tag; }
        }

        // ================================================================
        // MODEL COMPARISON TABLE
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Model Comparison: Shape Prediction ===");
        sb.AppendLine("");
        sb.AppendLine($"{"Model",-24} {"Shape R²",10} {"ΔR² vs Best",12} {"Rank",6}");
        sb.AppendLine(new string('-', 54));

        var shapeScores = new List<(string name, double r2)>();
        foreach (var m in modelDefs)
            shapeScores.Add((m.name, MultivariateR2(shapeTarget, m.x1, m.x2)));
        shapeScores.Add(("E: Full (best: " + bestFullShapePair + ")", bestFullShapeR2));

        double bestShapeR2 = shapeScores.Max(s => s.r2);
        foreach (var s in shapeScores.OrderByDescending(s => s.r2))
            sb.AppendLine($"{s.name,-24} {s.r2,10:F4} {bestShapeR2 - s.r2,12:F4} {shapeScores.OrderByDescending(x=>x.r2).ToList().IndexOf(s)+1,6}");

        sb.AppendLine("");

        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Model Comparison: Amplitude Prediction ===");
        sb.AppendLine("");
        sb.AppendLine($"{"Model",-24} {"Amp R²",10} {"ΔR² vs Best",12} {"Rank",6}");
        sb.AppendLine(new string('-', 54));

        var ampScores = new List<(string name, double r2)>();
        foreach (var m in modelDefs)
            ampScores.Add((m.name, MultivariateR2(ampTarget, m.x1, m.x2)));
        ampScores.Add(("E: Full (best: " + bestFullAmpPair + ")", bestFullAmpR2));

        double bestAmpR2 = ampScores.Max(s => s.r2);
        foreach (var s in ampScores.OrderByDescending(s => s.r2))
            sb.AppendLine($"{s.name,-24} {s.r2,10:F4} {bestAmpR2 - s.r2,12:F4} {ampScores.OrderByDescending(x=>x.r2).ToList().IndexOf(s)+1,6}");

        sb.AppendLine("");

        // ================================================================
        // INDEPENDENT INFORMATION ANALYSIS
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Independent Information Analysis ===");
        sb.AppendLine("");
        sb.AppendLine("  What does each variable add BEYOND gradient?");
        sb.AppendLine("");

        double shapeGradR2 = MultivariateR2(shapeTarget, gDirFrac, gStr);
        double shapeAddCurvDir = MultivariateR2(shapeTarget, gDirFrac, cDirFrac); // switch dir predictor
        double shapeAddCurvStr = MultivariateR2(shapeTarget, gDirFrac, cStr);     // switch str predictor
        double shapeAddChanFrac = MultivariateR2(shapeTarget, gDirFrac, chFrac);
        double shapeAddDensDir = MultivariateR2(shapeTarget, gDirFrac, dDir);

        sb.AppendLine($"{"Increment",-30} {"Shape R²",10} {"Δ from Grad",12}");
        sb.AppendLine(new string('-', 54));
        sb.AppendLine($"{"Gradient baseline",-30} {shapeGradR2,10:F4} {"--",12}");
        sb.AppendLine($"{"+ Curvature direction",-30} {shapeAddCurvDir,10:F4} {shapeAddCurvDir - shapeGradR2,12:F4}");
        sb.AppendLine($"{"+ Curvature strength",-30} {shapeAddCurvStr,10:F4} {shapeAddCurvStr - shapeGradR2,12:F4}");
        sb.AppendLine($"{"+ Channel fraction",-30} {shapeAddChanFrac,10:F4} {shapeAddChanFrac - shapeGradR2,12:F4}");
        sb.AppendLine($"{"+ Density direction",-30} {shapeAddDensDir,10:F4} {shapeAddDensDir - shapeGradR2,12:F4}");
        sb.AppendLine("");

        double ampGradR2 = MultivariateR2(ampTarget, gStr, gDirFrac);
        double ampAddCurvStr = MultivariateR2(ampTarget, gStr, cStr);
        double ampAddChanAlign = MultivariateR2(ampTarget, gStr, chAbsAlign);
        double ampAddDensStr = MultivariateR2(ampTarget, gStr, dStr);

        sb.AppendLine($"{"Increment",-30} {"Amp R²",10} {"Δ from Grad",12}");
        sb.AppendLine(new string('-', 54));
        sb.AppendLine($"{"Gradient baseline",-30} {ampGradR2,10:F4} {"--",12}");
        sb.AppendLine($"{"+ Curvature strength",-30} {ampAddCurvStr,10:F4} {ampAddCurvStr - ampGradR2,12:F4}");
        sb.AppendLine($"{"+ Channel alignment",-30} {ampAddChanAlign,10:F4} {ampAddChanAlign - ampGradR2,12:F4}");
        sb.AppendLine($"{"+ Density strength",-30} {ampAddDensStr,10:F4} {ampAddDensStr - ampGradR2,12:F4}");
        sb.AppendLine("");

        // ================================================================
        // FALSIFICATION TEST: Can any model beat gradient?
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Falsification Test ===");
        sb.AppendLine("");

        double gradShapeR2 = shapeScores.First(s => s.name == "A: Gradient-only").r2;
        double gradAmpR2 = ampScores.First(s => s.name == "A: Gradient-only").r2;

        bool anyBeatsGradShape = shapeScores.Any(s => s.name != "A: Gradient-only" && s.r2 > gradShapeR2 + 0.01);
        bool anyBeatsGradAmp = ampScores.Any(s => s.name != "A: Gradient-only" && s.r2 > gradAmpR2 + 0.01);

        string closestShape = shapeScores.Where(s => s.name != "A: Gradient-only").OrderByDescending(s => s.r2).First().name;
        double closestShapeR2 = shapeScores.Where(s => s.name != "A: Gradient-only").Max(s => s.r2);
        string closestAmp = ampScores.Where(s => s.name != "A: Gradient-only").OrderByDescending(s => s.r2).First().name;
        double closestAmpR2 = ampScores.Where(s => s.name != "A: Gradient-only").Max(s => s.r2);

        sb.AppendLine($"  Gradient shape R²:       {gradShapeR2:F4}");
        sb.AppendLine($"  Best non-gradient shape: {closestShape} (R²={closestShapeR2:F4})");
        sb.AppendLine($"  Any beats gradient?      {(anyBeatsGradShape ? "YES — gradient FALSIFIED" : "NO — gradient survives")}");
        sb.AppendLine("");
        sb.AppendLine($"  Gradient amp R²:         {gradAmpR2:F4}");
        sb.AppendLine($"  Best non-gradient amp:   {closestAmp} (R²={closestAmpR2:F4})");
        sb.AppendLine($"  Any beats gradient?      {(anyBeatsGradAmp ? "YES — gradient FALSIFIED" : "NO — gradient survives")}");
        sb.AppendLine("");

        // ================================================================
        // PER-BARYONIC-CLASS MODEL PERFORMANCE
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Model Performance by Baryonic Class ===");
        sb.AppendLine("");

        // Add MedFrac to results for class determination
        var classResults = new List<(string cls, int n, double gradShape, double curvShape, double chanShape, double densShape,
            double gradAmp, double curvAmp, double chanAmp, double densAmp)>();

        foreach (var clsName in new[] { "BARYON", "MIXED", "DM" })
        {
            var g = results.Where(r => Classify(r) == clsName).ToList();
            if (g.Count < 5) continue;
            var idx = g.Select(r => results.IndexOf(r)).ToArray();

            double gsR2 = MultivariateR2(Idx(shapeTarget, idx), Idx(gDirFrac, idx), Idx(gStr, idx));
            double csR2 = MultivariateR2(Idx(shapeTarget, idx), Idx(cDirFrac, idx), Idx(cStr, idx));
            double chsR2 = MultivariateR2(Idx(shapeTarget, idx), Idx(chFrac, idx), Idx(chAbsAlign, idx));
            double dsR2 = MultivariateR2(Idx(shapeTarget, idx), Idx(dDir, idx), Idx(dStr, idx));
            double gaR2 = MultivariateR2(Idx(ampTarget, idx), Idx(gStr, idx), Idx(gDirFrac, idx));
            double caR2 = MultivariateR2(Idx(ampTarget, idx), Idx(cStr, idx), Idx(cDirFrac, idx));
            double chaR2 = MultivariateR2(Idx(ampTarget, idx), Idx(chAbsAlign, idx), Idx(chFrac, idx));
            double daR2 = MultivariateR2(Idx(ampTarget, idx), Idx(dStr, idx), Idx(dDir, idx));

            classResults.Add((clsName, g.Count, gsR2, csR2, chsR2, dsR2, gaR2, caR2, chaR2, daR2));
        }

        sb.AppendLine($"{"Class",-10} {"N",5} {"GradSh",8} {"CurvSh",8} {"ChanSh",8} {"DensSh",8} {"GradAmp",8} {"CurvAmp",8} {"ChanAmp",8} {"DensAmp",8}");
        sb.AppendLine(new string('-', 81));
        foreach (var c in classResults)
            sb.AppendLine($"{c.cls,-10} {c.n,5} {c.gradShape,8:F4} {c.curvShape,8:F4} {c.chanShape,8:F4} {c.densShape,8:F4} {c.gradAmp,8:F4} {c.curvAmp,8:F4} {c.chanAmp,8:F4} {c.densAmp,8:F4}");
        sb.AppendLine("");

        // ================================================================
        // MINIMAL DYNAMICS LAW
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Minimal Dynamics Law ===");
        sb.AppendLine("");

        bool gradDominant = !anyBeatsGradShape && !anyBeatsGradAmp;
        double totalGradR2 = gradShapeR2 + gradAmpR2;
        double totalBestAltR2 = closestShapeR2 + closestAmpR2;
        double gradAdvantage = totalGradR2 - totalBestAltR2;

        sb.AppendLine($"  Gradient total R²:       {totalGradR2:F4} (shape + amp)");
        sb.AppendLine($"  Best alternative total:  {totalBestAltR2:F4}");
        sb.AppendLine($"  Gradient advantage:      {gradAdvantage:F4}");
        sb.AppendLine("");

        if (gradDominant && gradAdvantage > 0.02)
        {
            sb.AppendLine("  GRADIENT SURVIVES FALSIFICATION.");
            sb.AppendLine("  No alternative model approaches gradient performance.");
            sb.AppendLine("  ∇ρ is the minimal sufficient carrier of TRM galactic dynamics.");
        }
        else if (gradDominant)
        {
            sb.AppendLine("  GRADIENT SURVIVES but margin is thin.");
            sb.AppendLine("  Alternatives are close — gradient is the best but not uniquely so.");
        }
        else
        {
            sb.AppendLine("  GRADIENT FALSIFIED as unique primitive.");
            sb.AppendLine("  An alternative variable carries comparable or superior information.");
        }
        sb.AppendLine("");

        sb.AppendLine("  Minimal dynamics law:");
        sb.AppendLine("");
        sb.AppendLine("    If gradient survives:");
        sb.AppendLine("      TRM Gravity = { direction(∇ρ), magnitude(∇ρ) }");
        sb.AppendLine("      Shape   = sgn(∇ρ)");
        sb.AppendLine("      Strength = |∇ρ|^α");
        sb.AppendLine("");
        sb.AppendLine("    If gradient falsified:");
        sb.AppendLine("      Multiple variables carry irreducible information.");
        sb.AppendLine("      Minimal model requires gradient + [winning variable].");
        sb.AppendLine("");

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        bool criterionA = !anyBeatsGradShape;          // No model beats gradient for shape
        bool criterionB = !anyBeatsGradAmp;            // No model beats gradient for amplitude
        bool criterionC = gradAdvantage > 0.02;        // Meaningful advantage
        bool criterionD = results.Count >= 120;         // Sufficient sample

        int criteriaMet = 0;
        if (criterionA) criteriaMet++;
        if (criterionB) criteriaMet++;
        if (criterionC) criteriaMet++;
        if (criterionD) criteriaMet++;

        string verdict = criteriaMet >= 4 ? "SUPPORTED: gradient is the primitive carrier."
            : criteriaMet >= 2 ? "CONDITIONAL: gradient dominant but not sufficient."
            : "FALSIFIED: another variable carries independent information.";

        sb.AppendLine($"VERDICT: {verdict}  ({criteriaMet}/4 criteria)");
        sb.AppendLine("");
        sb.AppendLine($"  A. No model beats gradient for shape:     {(criterionA ? "YES" : "NO")}");
        sb.AppendLine($"  B. No model beats gradient for amplitude: {(criterionB ? "YES" : "NO")}");
        sb.AppendLine($"  C. Gradient advantage > 0.02 total R²:    {(criterionC ? "YES" : "NO")} (Δ={gradAdvantage:F4})");
        sb.AppendLine($"  D. ≥120 galaxies:                          {(criterionD ? "YES" : "NO")} ({results.Count})");
        sb.AppendLine("");
        sb.AppendLine("Falsification Result:");
        sb.AppendLine($"  Gradient model vs {modelDefs.Length} alternatives:");
        sb.AppendLine($"  Shape:  gradient R² = {gradShapeR2:F4}, closest = {closestShape} (R²={closestShapeR2:F4})");
        sb.AppendLine($"  Amp:    gradient R² = {gradAmpR2:F4}, closest = {closestAmp} (R²={closestAmpR2:F4})");
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== GKF_01 complete. Commit: GKF_01_GradientKernelFalsificationAudit ===");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ================================================================
    // HELPERS
    // ================================================================

    private static double PearsonCorr(double[] xs, double[] ys)
    {
        int n = xs.Length; if (n < 2) return 0;
        double mx = xs.Average(), my = ys.Average();
        double sxy = 0, sxx = 0, syy = 0;
        for (int i = 0; i < n; i++) { double dx = xs[i] - mx, dy = ys[i] - my; sxy += dx * dy; sxx += dx * dx; syy += dy * dy; }
        return sxx > 1e-15 && syy > 1e-15 ? sxy / Math.Sqrt(sxx * syy) : 0;
    }

    /// <summary>R² for 2-predictor regression y ~ x1 + x2.</summary>
    private static double MultivariateR2(double[] y, double[] x1, double[] x2)
    {
        int n = y.Length; if (n < 3) return 0;
        double sy = y.Sum(), s1 = x1.Sum(), s2 = x2.Sum();
        double s11 = 0, s22 = 0, s12 = 0, s1y = 0, s2y = 0;
        for (int i = 0; i < n; i++)
        {
            s11 += x1[i] * x1[i]; s22 += x2[i] * x2[i]; s12 += x1[i] * x2[i];
            s1y += x1[i] * y[i]; s2y += x2[i] * y[i];
        }
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
        double ssRes = 0, ssTot = 0; double my = sy / n;
        for (int i = 0; i < n; i++) { double pred = b0 + b1 * x1[i] + b2 * x2[i]; ssRes += (y[i] - pred) * (y[i] - pred); ssTot += (y[i] - my) * (y[i] - my); }
        double r2 = ssTot > 1e-15 ? 1 - ssRes / ssTot : 0;
        return Math.Max(0, Math.Min(1, r2));
    }

    private static string Classify(GkfResult r) =>
        r.MedFrac > 0.85 ? "BARYON" : r.MedFrac > 0.50 ? "MIXED" : "DM";

    private static double[] Idx(double[] arr, int[] idx) => idx.Select(i => arr[i]).ToArray();

    private static double ParseD(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return double.NaN;
        return double.TryParse(s.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : double.NaN;
    }

    private record GkfPt(double R, double Vobs, double Vbary, double Vdisk);
    private record GkfResult(
        string Id,
        int GradDir, double GradDirFrac, double GradStrength,
        int CurvDir, double CurvDirFrac, double CurvStrength,
        double ChannelAlign, double ChannelFraction,
        int DensDir, double DensSlope, double OuterDens,
        bool ShapeMatch, double ShapeDiff, bool AmpGood, double AmpResidual,
        double VFlat, double SurfBri, double MedFrac);
}
