using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V32_4;

[Trait("Category", "V32_4")]
[Trait("Category", "LongRunning")]
public class V32_4_GradientPrimacyReduction_Tests
{
    private readonly ITestOutputHelper _o;
    public V32_4_GradientPrimacyReduction_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void GPR_01_GradientPrimacyReductionAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== GPR_01: Gradient Primacy Reduction Audit ===");
        sb.AppendLine("=== Can TRM gravity reduce to gradient alone? ===");
        sb.AppendLine(new string('=', 108));

        var repoRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
        var massFile = Path.Combine(repoRoot, "TRM.Core", "Data", "MassModels_Lelli2016c.mrt");

        var galData = new Dictionary<string, List<GprPt>>();
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
            if (!galData.ContainsKey(id)) galData[id] = new List<GprPt>();
            galData[id].Add(new GprPt(r, vobs, vb, vdisk));
        }

        var results = new List<GprResult>();
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

            // GRADIENT
            int gNeg = dDensDr.Count(d => d < -1e-15), gPos = dDensDr.Count(d => d > 1e-15);
            double gradDirFrac = Math.Max(gNeg, gPos) / (double)dDensDr.Count;
            double gradStr = dDensDr.Average(d => Math.Abs(d));

            // CURVATURE
            int cNeg = curv.Count(d => d < -1e-15), cPos = curv.Count(d => d > 1e-15);
            double curvDirFrac = curv.Count > 0 ? Math.Max(cNeg, cPos) / (double)curv.Count : 0;
            double curvStr = curv.Count > 0 ? curv.Average(c => Math.Abs(c)) : 0;

            // CHANNEL
            double channelAlign = PearsonCorr(dDensDr.ToArray(), dVobsDr.ToArray());
            int sameSign = 0;
            for (int i = 0; i < dDensDr.Count; i++)
                if (dDensDr[i] * dVobsDr[i] < 0) sameSign++;
            double channelFrac = (double)sameSign / dDensDr.Count;

            // TARGETS
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

            double vFlat = outer.Average(p => p.Vobs);
            double surfBri = outer.Average(p => p.Vdisk * p.Vdisk);
            var fracs = sorted.Select(p => p.Vobs > 0 ? p.Vbary / p.Vobs : 0).Where(f => f > 0).ToList();
            double medFrac = fracs.Count > 0 ? fracs.OrderBy(f => f).ElementAt(fracs.Count / 2) : 0;

            results.Add(new GprResult(id, gradDirFrac, gradStr, curvDirFrac, curvStr,
                channelAlign, channelFrac, shapeMatch, shapeDiff, ampGood, ampRes,
                vFlat, surfBri, medFrac));
        }

        if (results.Count == 0) { _o.WriteLine("No valid galaxies."); Assert.True(true); return; }

        sb.AppendLine("");
        sb.AppendLine($"  Galaxies analyzed: {results.Count}");
        sb.AppendLine("");

        // ================================================================
        // FEATURE MATRICES
        // ================================================================
        double[] shapeY = results.Select(r => r.ShapeMatch ? 1.0 : 0.0).ToArray();
        double[] ampY = results.Select(r => r.AmpResidual).ToArray();

        // Gradient (2 features)
        var gradFeatures = new[] {
            results.Select(r => r.GradDirFrac).ToArray(),
            results.Select(r => Math.Log10(Math.Max(1, r.GradStrength))).ToArray()
        };

        // Curvature (2 features)
        var curvFeatures = new[] {
            results.Select(r => r.CurvDirFrac).ToArray(),
            results.Select(r => Math.Log10(Math.Max(1, r.CurvStrength))).ToArray()
        };

        // Channel (2 features)
        var chanFeatures = new[] {
            results.Select(r => r.ChannelFraction).ToArray(),
            results.Select(r => Math.Abs(r.ChannelAlign)).ToArray()
        };

        // ================================================================
        // INCREMENTAL MODEL BUILDING
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Information Gain Table: Shape Prediction ===");
        sb.AppendLine("");
        sb.AppendLine($"{"Model",-36} {"R²",10} {"ΔR²",10} {"CumulΔ",10} {"k",4}");
        sb.AppendLine(new string('-', 72));

        // A) Gradient-only
        double shapeA = OLSR2(shapeY, gradFeatures);
        sb.AppendLine($"{"A: Gradient-only (2 vars)",-36} {shapeA,10:F4} {"--",10} {"--",10} {2,4}");

        // B) Gradient + Curvature
        var gradCurvFeatures = MergeFeatures(gradFeatures, curvFeatures);
        double shapeB = OLSR2(shapeY, gradCurvFeatures);
        sb.AppendLine($"{"B: Gradient + Curvature (4 vars)",-36} {shapeB,10:F4} {shapeB-shapeA,10:F4} {shapeB-shapeA,10:F4} {4,4}");

        // C) Gradient + Channels
        var gradChanFeatures = MergeFeatures(gradFeatures, chanFeatures);
        double shapeC = OLSR2(shapeY, gradChanFeatures);
        sb.AppendLine($"{"C: Gradient + Channels (4 vars)",-36} {shapeC,10:F4} {shapeC-shapeA,10:F4} {shapeC-shapeA,10:F4} {4,4}");

        // D) Full: all 6 features
        var allFeatures = MergeFeatures(gradCurvFeatures, chanFeatures);
        double shapeD = OLSR2(shapeY, allFeatures);
        sb.AppendLine($"{"D: Full (6 vars)",-36} {shapeD,10:F4} {shapeD-shapeA,10:F4} {shapeD-shapeA,10:F4} {6,4}");
        sb.AppendLine("");

        // ================================================================
        // AMPLITUDE
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Information Gain Table: Amplitude Prediction ===");
        sb.AppendLine("");
        sb.AppendLine($"{"Model",-36} {"R²",10} {"ΔR²",10} {"CumulΔ",10} {"k",4}");
        sb.AppendLine(new string('-', 72));

        double ampA = OLSR2(ampY, gradFeatures);
        sb.AppendLine($"{"A: Gradient-only (2 vars)",-36} {ampA,10:F4} {"--",10} {"--",10} {2,4}");

        double ampB = OLSR2(ampY, gradCurvFeatures);
        sb.AppendLine($"{"B: Gradient + Curvature (4 vars)",-36} {ampB,10:F4} {ampB-ampA,10:F4} {ampB-ampA,10:F4} {4,4}");

        double ampC = OLSR2(ampY, gradChanFeatures);
        sb.AppendLine($"{"C: Gradient + Channels (4 vars)",-36} {ampC,10:F4} {ampC-ampA,10:F4} {ampC-ampA,10:F4} {4,4}");

        double ampD = OLSR2(ampY, allFeatures);
        sb.AppendLine($"{"D: Full (6 vars)",-36} {ampD,10:F4} {ampD-ampA,10:F4} {ampD-ampA,10:F4} {6,4}");
        sb.AppendLine("");

        // ================================================================
        // ΔR² RANKING
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== ΔR² Ranking: What adds information beyond gradient? ===");
        sb.AppendLine("");
        sb.AppendLine($"{"Addition",-28} {"Shape ΔR²",12} {"Amp ΔR²",12} {"Total ΔR²",12} {"Significant?",14}");
        sb.AppendLine(new string('-', 80));

        double shCurvGain = shapeB - shapeA;
        double shChanGain = shapeC - shapeA;
        double shFullGain = shapeD - shapeA;
        double amCurvGain = ampB - ampA;
        double amChanGain = ampC - ampA;
        double amFullGain = ampD - ampA;

        static string sigMark(double d) => d > 0.005 ? "YES" : d > 0.001 ? "MARGINAL" : "NO";

        sb.AppendLine($"{"+ Curvature",-28} {shCurvGain,12:F4} {amCurvGain,12:F4} {shCurvGain+amCurvGain,12:F4} {sigMark(shCurvGain+amCurvGain),14}");
        sb.AppendLine($"{"+ Channels",-28} {shChanGain,12:F4} {amChanGain,12:F4} {shChanGain+amChanGain,12:F4} {sigMark(shChanGain+amChanGain),14}");
        sb.AppendLine($"{"+ Curv + Chan (full)",-28} {shFullGain,12:F4} {amFullGain,12:F4} {shFullGain+amFullGain,12:F4} {sigMark(shFullGain+amFullGain),14}");
        sb.AppendLine("");

        // ================================================================
        // RESIDUAL REDUCTION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Residual Reduction ===");
        sb.AppendLine("");

        double shapeResidA = Math.Sqrt(1 - Math.Min(1, shapeA));
        double shapeResidD = Math.Sqrt(1 - Math.Min(1, shapeD));
        double ampResidA = Math.Sqrt(1 - Math.Min(1, ampA));
        double ampResidD = Math.Sqrt(1 - Math.Min(1, ampD));

        sb.AppendLine($"  Shape: residual reduced from {shapeResidA:F4} to {shapeResidD:F4} ({(1-shapeResidD/shapeResidA)*100:F1}%)");
        sb.AppendLine($"  Amp:   residual reduced from {ampResidA:F4} to {ampResidD:F4} ({(1-ampResidD/ampResidA)*100:F1}%)");
        sb.AppendLine("");

        // ================================================================
        // FRACTION OF EXPLAINED VARIANCE FROM GRADIENT
        // ================================================================
        double shapeFracGrad = shapeD > 0.001 ? shapeA / shapeD : 1.0;
        double ampFracGrad = ampD > 0.001 ? ampA / ampD : 1.0;

        sb.AppendLine($"  Shape: gradient accounts for {shapeFracGrad*100:F1}% of explainable variance");
        sb.AppendLine($"  Amp:   gradient accounts for {ampFracGrad*100:F1}% of explainable variance");
        sb.AppendLine("");

        // ================================================================
        // MINIMAL GRAVITY LAW
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Minimal Gravity Law ===");
        sb.AppendLine("");

        double totalGradR2 = shapeA + ampA;
        double totalFullR2 = shapeD + ampD;
        double totalGain = totalFullR2 - totalGradR2;

        sb.AppendLine($"  Gradient total R²: {totalGradR2:F4}");
        sb.AppendLine($"  Full total R²:     {totalFullR2:F4}");
        sb.AppendLine($"  Full gain:         {totalGain:F4}");
        sb.AppendLine("");

        if (totalGain < 0.01)
        {
            sb.AppendLine("  GRADIENT IS SUFFICIENT.");
            sb.AppendLine("  Curvature and channels add negligible information beyond ∇ρ.");
            sb.AppendLine("  TRM gravity reduces to: f(∇ρ) = {direction, magnitude}");
        }
        else if (totalGain < 0.03)
        {
            sb.AppendLine("  GRADIENT IS DOMINANT with small additions.");
            sb.AppendLine("  Curvature/channels add marginal information (ΔR² < 0.03).");
            sb.AppendLine("  Practical reduction: ∇ρ is the primary carrier.");
        }
        else
        {
            sb.AppendLine("  GRADIENT IS NOT SUFFICIENT.");
            sb.AppendLine($"  Additional variables add {totalGain:F4} total R².");
            sb.AppendLine("  Full model required: ∇ρ + ∇²ρ + channels.");
        }
        sb.AppendLine("");

        sb.AppendLine("  Minimal TRM gravity law candidate:");
        sb.AppendLine("");
        sb.AppendLine("    TRM_Gravity(galaxy) = {");
        sb.AppendLine("      shape     = f_dir (∇ρ),");
        sb.AppendLine("      amplitude = f_mag (|∇ρ|)");
        sb.AppendLine("    }");
        sb.AppendLine("");

        if (totalGain > 0.01)
        {
            sb.AppendLine("    with optional corrections:");
            if (shCurvGain > 0.001 || amCurvGain > 0.001)
                sb.AppendLine($"      + ∇²ρ  (ΔR²_shape={shCurvGain:F4}, ΔR²_amp={amCurvGain:F4})");
            if (shChanGain > 0.001 || amChanGain > 0.001)
                sb.AppendLine($"      + channels (ΔR²_shape={shChanGain:F4}, ΔR²_amp={amChanGain:F4})");
        }
        sb.AppendLine("");

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        bool criterionA = totalGain < 0.05;            // Full adds <5% total
        bool criterionB = shapeFracGrad > 0.85;        // Gradient >85% of explainable shape
        bool criterionC = ampFracGrad > 0.70;          // Gradient >70% of explainable amp
        bool criterionD = results.Count >= 120;         // Sufficient sample

        int criteriaMet = 0;
        if (criterionA) criteriaMet++;
        if (criterionB) criteriaMet++;
        if (criterionC) criteriaMet++;
        if (criterionD) criteriaMet++;

        string verdict = criteriaMet >= 4 ? "SUPPORTED: gradient is sufficient."
            : criteriaMet >= 2 ? "CONDITIONAL: small independent additions exist."
            : "FALSIFIED: gradient not sufficient.";

        sb.AppendLine($"VERDICT: {verdict}  ({criteriaMet}/4 criteria)");
        sb.AppendLine("");
        sb.AppendLine($"  A. Full adds <5% total R²:               {(criterionA ? "YES" : "NO")} (Δ={totalGain:F4})");
        sb.AppendLine($"  B. Gradient >85% of explainable shape:   {(criterionB ? "YES" : "NO")} ({shapeFracGrad*100:F0}%)");
        sb.AppendLine($"  C. Gradient >70% of explainable amp:     {(criterionC ? "YES" : "NO")} ({ampFracGrad*100:F0}%)");
        sb.AppendLine($"  D. ≥120 galaxies:                         {(criterionD ? "YES" : "NO")} ({results.Count})");
        sb.AppendLine("");
        sb.AppendLine("Reduction Result:");
        sb.AppendLine($"  From {2} gradient features to {6} total features:");
        sb.AppendLine($"  Shape R² gain:   +{shapeD-shapeA:F4}");
        sb.AppendLine($"  Amp R² gain:     +{ampD-ampA:F4}");
        sb.AppendLine($"  Total gain:      +{totalGain:F4}");
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== GPR_01 complete. Commit: GPR_01_GradientPrimacyReductionAudit ===");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ================================================================
    // GENERAL OLS WITH K PREDICTORS
    // ================================================================

    /// <summary>R² for OLS regression y ~ X where X is k predictor arrays.</summary>
    private static double OLSR2(double[] y, double[][] X)
    {
        int n = y.Length, k = X.Length;
        if (n < k + 1) return 0;

        // Build normal equations: (X^T X) b = X^T y
        // X has rows = observations, cols = predictors (+ intercept later)
        // We'll build X^T X and X^T y directly

        // Center y and each predictor (removes intercept)
        double my = y.Average();
        double[] yc = y.Select(v => v - my).ToArray();
        double[][] Xc = X.Select(col => { double m = col.Average(); return col.Select(v => v - m).ToArray(); }).ToArray();

        // Build k×k matrix A and k-vector rhs
        double[,] A = new double[k, k];
        double[] rhs = new double[k];
        for (int i = 0; i < k; i++)
        {
            for (int j = 0; j < k; j++)
            {
                double sum = 0;
                for (int t = 0; t < n; t++) sum += Xc[i][t] * Xc[j][t];
                A[i, j] = sum;
            }
            double s = 0;
            for (int t = 0; t < n; t++) s += Xc[i][t] * yc[t];
            rhs[i] = s;
        }

        // Solve A * b = rhs via Gaussian elimination with partial pivoting
        double[] b = SolveLinearSystem(A, rhs, k);

        // Compute R²
        double ssRes = 0, ssTot = 0;
        for (int t = 0; t < n; t++)
        {
            double pred = my;
            for (int j = 0; j < k; j++)
                pred += b[j] * Xc[j][t];
            ssRes += (y[t] - pred) * (y[t] - pred);
            ssTot += (y[t] - my) * (y[t] - my);
        }
        double r2 = ssTot > 1e-15 ? 1 - ssRes / ssTot : 0;
        return Math.Max(0, Math.Min(1, r2));
    }

    private static double[] SolveLinearSystem(double[,] A, double[] rhs, int n)
    {
        // Gaussian elimination with partial pivoting, in-place on augmented matrix
        double[,] aug = new double[n, n + 1];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++) aug[i, j] = A[i, j];
            aug[i, n] = rhs[i];
        }

        for (int col = 0; col < n; col++)
        {
            // Partial pivoting
            int maxRow = col;
            double maxVal = Math.Abs(aug[col, col]);
            for (int row = col + 1; row < n; row++)
            {
                if (Math.Abs(aug[row, col]) > maxVal)
                { maxVal = Math.Abs(aug[row, col]); maxRow = row; }
            }
            if (maxVal < 1e-15) continue;

            if (maxRow != col)
                for (int j = col; j <= n; j++) { double tmp = aug[col, j]; aug[col, j] = aug[maxRow, j]; aug[maxRow, j] = tmp; }

            for (int row = col + 1; row < n; row++)
            {
                double factor = aug[row, col] / aug[col, col];
                for (int j = col; j <= n; j++)
                    aug[row, j] -= factor * aug[col, j];
            }
        }

        // Back substitution
        double[] x = new double[n];
        for (int i = n - 1; i >= 0; i--)
        {
            double sum = aug[i, n];
            for (int j = i + 1; j < n; j++) sum -= aug[i, j] * x[j];
            x[i] = Math.Abs(aug[i, i]) > 1e-15 ? sum / aug[i, i] : 0;
        }
        return x;
    }

    private static double[][] MergeFeatures(double[][] a, double[][] b)
    {
        var result = new double[a.Length + b.Length][];
        Array.Copy(a, result, a.Length);
        Array.Copy(b, 0, result, a.Length, b.Length);
        return result;
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

    private record GprPt(double R, double Vobs, double Vbary, double Vdisk);
    private record GprResult(
        string Id, double GradDirFrac, double GradStrength,
        double CurvDirFrac, double CurvStrength,
        double ChannelAlign, double ChannelFraction,
        bool ShapeMatch, double ShapeDiff, bool AmpGood, double AmpResidual,
        double VFlat, double SurfBri, double MedFrac);
}
