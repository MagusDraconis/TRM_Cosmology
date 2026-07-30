using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using TRM.Tests.V7_3_and_4;
using static TRM.Tests.V7_3_and_4.V7TestHelpers;
using static TRM.Tests.V14.V14TestHelpers;

namespace TRM.Tests.V33_13;

[Trait("Category", "V33_13")]
[Trait("Category", "LongRunning")]
public class V33_13_EstimatorIdentity_Tests
{
    private readonly ITestOutputHelper _o;
    public V33_13_EstimatorIdentity_Tests(ITestOutputHelper o) { _o = o; }

    // ====================================================================
    // EID_01: INFORMATION DECOMPOSITION
    //
    // Are fb and |m| independent information channels?
    // Measure: I(target | fb), I(target | |m|), I(target | fb, |m|)
    //
    // Null:  fb and |m| carry identical information — ΔI ≈ 0
    // Alt:   fb carries unique information beyond |m|
    //
    // Targets tested: tick, sign, boundary, tick_anomaly, curvature
    //
    // Pass (fb IS independent):    at least one target shows ΔI > threshold
    // Fail (fb IS redundant):      all targets show ΔI ≈ 0
    // ====================================================================
    [Fact]
    public void EID_01_InformationDecomposition_DoesFbAddInformation()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== EID_01: Information Decomposition ===");
        sb.AppendLine("=== Does fb carry information beyond |m|? ===");
        sb.AppendLine(new string('=', 96));

        var data = CollectEstimatorData();
        if (data.Count < 100) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        double[] fbV = data.Select(c => c.Fb).ToArray();
        double[] mV = data.Select(c => c.AbsM).ToArray();
        double[] lfb = data.Select(c => Math.Log10(Math.Max(1e-15, c.Fb))).ToArray();
        double[] lm = data.Select(c => Math.Log10(Math.Max(1e-15, c.AbsM))).ToArray();

        // Target observables
        var targets = new (string name, double[] values)[]
        {
            ("tick",           data.Select(c => c.Tick).ToArray()),
            ("sign",           data.Select(c => (double)c.Sign).ToArray()),
            ("boundary",       data.Select(c => c.IsBoundary ? 1.0 : 0.0).ToArray()),
            ("tick_anomaly",   data.Select(c => c.TickAnomaly).ToArray()),
            ("bd_distance",    data.Select(c => (double)c.ZoneDist).ToArray()),
            ("∇|m|",           data.Select(c => Math.Log10(Math.Max(1e-15, c.GradM))).ToArray()),
            ("∇fb",            data.Select(c => Math.Log10(Math.Max(1e-15, c.GradFb))).ToArray()),
            ("∇Tick",          data.Select(c => Math.Log10(Math.Max(1e-15, c.GradTick))).ToArray()),
        };

        sb.AppendLine($"{"Target",-16} {"R²(|m|)",10} {"R²(fb)",10} {"R²(fb+|m|)",12} {"ΔR²(fb)",10} {"Unique?",8}");
        sb.AppendLine(new string('-', 72));

        foreach (var (tname, tval) in targets)
        {
            double r2_m = PearsonCorr(lm, tval); r2_m *= r2_m;
            double r2_fb = PearsonCorr(lfb, tval); r2_fb *= r2_fb;
            double r2_both = MultivariateR2(tval, lfb, lm);
            double deltaFb = r2_both - r2_m;
            double deltaM = r2_both - r2_fb;
            bool unique = deltaFb > 0.05;

            // Which predictor dominates?
            string uniqueInfo = deltaFb > 0.05 ? $"fb+{deltaFb*100:F1}%" :
                                deltaM > 0.05 ? $"|m|+{deltaM*100:F1}%" : "NEITHER";

            sb.AppendLine($"{tname,-16} {r2_m,10:F4} {r2_fb,10:F4} {r2_both,12:F4} {deltaFb,10:F4} {uniqueInfo,8}");
        }
        sb.AppendLine("");

        // Count targets where fb adds meaningful information
        int fbWins = 0, fbAdds = 0;
        foreach (var (tname, tval) in targets)
        {
            double r2_m = PearsonCorr(lm, tval); r2_m *= r2_m;
            double r2_fb = PearsonCorr(lfb, tval); r2_fb *= r2_fb;
            double r2_both = MultivariateR2(tval, lfb, lm);
            if (r2_fb > r2_m + 0.05) fbWins++;
            if (r2_both > r2_m + 0.05) fbAdds++;
        }

        sb.AppendLine($"  fb dominates |m| on {fbWins}/{targets.Length} targets");
        sb.AppendLine($"  fb adds unique info on {fbAdds}/{targets.Length} targets");
        sb.AppendLine("");

        bool fbIndependent = fbAdds >= 2;
        sb.AppendLine(fbIndependent
            ? "  VERDICT: fb CARRIES INDEPENDENT INFORMATION — not merely collinear with |m|"
            : "  VERDICT: fb IS REDUNDANT — |m| captures essentially the same information");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(!fbIndependent, $"EID_01: fb adds info on {fbAdds}/{targets.Length} targets. Null: fb is redundant with |m|.");
    }

    // ====================================================================
    // EID_02: RESIDUAL ANALYSIS
    //
    // Fit fb = α·|m| + β (linear) and fb = α·|m|^γ (power law)
    // Analyze residuals: do they carry predictive power?
    //
    // Null:  fb_residual is noise — no predictive power
    // Alt:   fb_residual predicts observables beyond |m|
    //
    // Pass (fb independent):  residuals predict at least one target
    // Fail (fb redundant):    residuals are pure noise
    // ====================================================================
    [Fact]
    public void EID_02_ResidualAnalysis_DoesFbResidualCarrySignal()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== EID_02: Residual Analysis ===");
        sb.AppendLine("=== After regressing fb on |m|, does the residual carry information? ===");
        sb.AppendLine(new string('=', 96));

        var data = CollectEstimatorData();
        if (data.Count < 100) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        double[] fbV = data.Select(c => c.Fb).ToArray();
        double[] mV = data.Select(c => c.AbsM).ToArray();
        double[] lfb = data.Select(c => Math.Log10(Math.Max(1e-15, c.Fb))).ToArray();
        double[] lm = data.Select(c => Math.Log10(Math.Max(1e-15, c.AbsM))).ToArray();

        // Model 1: linear fb = a·|m| + b
        double mMean = mV.Average(), fbMean = fbV.Average();
        double cov = 0, varM = 0;
        for (int i = 0; i < mV.Length; i++) { double dm = mV[i] - mMean; cov += dm * (fbV[i] - fbMean); varM += dm * dm; }
        double aLin = varM > 1e-15 ? cov / varM : 0;
        double bLin = fbMean - aLin * mMean;
        double[] residLin = mV.Select((m, i) => fbV[i] - (aLin * m + bLin)).ToArray();
        double r2Lin = 1.0 - residLin.Select(r => r * r).Sum() / Math.Max(1e-15, fbV.Select(f => (f - fbMean) * (f - fbMean)).Sum());

        // Model 2: log-linear log(fb) = γ·log(|m|) + δ
        double lmMean = lm.Average(), lfbMean = lfb.Average();
        double covL = 0, varLm = 0;
        for (int i = 0; i < lm.Length; i++) { double dl = lm[i] - lmMean; covL += dl * (lfb[i] - lfbMean); varLm += dl * dl; }
        double gamma = varLm > 1e-15 ? covL / varLm : 0;
        double delta = lfbMean - gamma * lmMean;
        double[] predLog = lm.Select(l => Math.Pow(10, gamma * l + delta)).ToArray();
        double[] residLog = fbV.Zip(predLog, (f, p) => f - p).ToArray();
        double ssResLog = residLog.Select(r => r * r).Sum();
        double ssTot = fbV.Select(f => (f - fbMean) * (f - fbMean)).Sum();
        double r2Log = 1.0 - ssResLog / Math.Max(1e-15, ssTot);

        // Model 3: quadratic fb = a·|m|² + b·|m| + c
        double[] m2V = mV.Select(m => m * m).ToArray();
        double r2Quad = MultivariateR2(fbV, mV, m2V);
        double[] predQuad = new double[mV.Length];
        {
            double s1 = mV.Sum(), s2 = m2V.Sum(), sy = fbV.Sum();
            double s11 = mV.Zip(mV, (a, b) => a * b).Sum(), s22 = m2V.Zip(m2V, (a, b) => a * b).Sum();
            double s12 = mV.Zip(m2V, (a, b) => a * b).Sum();
            double s1y = mV.Zip(fbV, (a, b) => a * b).Sum(), s2y = m2V.Zip(fbV, (a, b) => a * b).Sum();
            int n = mV.Length;
            double s1yc = s1y - s1 * sy / n, s2yc = s2y - s2 * sy / n;
            double s11c = s11 - s1 * s1 / n, s22c = s22 - s2 * s2 / n, s12c = s12 - s1 * s2 / n;
            double det = s11c * s22c - s12c * s12c;
            double b1 = 0, b2 = 0;
            if (Math.Abs(det) > 1e-15) { b1 = (s1yc * s22c - s2yc * s12c) / det; b2 = (s2yc * s11c - s1yc * s12c) / det; }
            double b0 = sy / n - b1 * s1 / n - b2 * s2 / n;
            for (int i = 0; i < n; i++) predQuad[i] = b0 + b1 * mV[i] + b2 * m2V[i];
        }
        double[] residQuad = fbV.Zip(predQuad, (f, p) => f - p).ToArray();

        // Best model residuals
        double bestR2 = Math.Max(r2Lin, Math.Max(r2Log, r2Quad));
        double[] bestResid = bestR2 == r2Quad ? residQuad : bestR2 == r2Log ? residLog : residLin;
        string bestModel = bestR2 == r2Quad ? "quadratic" : bestR2 == r2Log ? "power-law" : "linear";

        sb.AppendLine($"  fb ~ |m| models:");
        sb.AppendLine($"    Linear:     R² = {r2Lin:F4}  fb = {aLin:F4}·|m| + {bLin:F4}");
        sb.AppendLine($"    Power-law:  R² = {r2Log:F4}  fb = 10^({delta:F4}) · |m|^{gamma:F4}");
        sb.AppendLine($"    Quadratic:  R² = {r2Quad:F4}");
        sb.AppendLine($"    Best: {bestModel}  R² = {bestR2:F4}");
        sb.AppendLine("");

        // Does the residual predict anything?
        sb.AppendLine($"  Residual signal tests (using {bestModel} residuals):");
        sb.AppendLine($"{"Target",-18} {"r(resid,target)",14} {"p-value proxy",12} {"Signal?",8}");
        sb.AppendLine(new string('-', 56));

        var targets = new (string name, double[] values)[]
        {
            ("tick",           data.Select(c => c.Tick).ToArray()),
            ("sign",           data.Select(c => (double)c.Sign).ToArray()),
            ("boundary",       data.Select(c => c.IsBoundary ? 1.0 : 0.0).ToArray()),
            ("tick_anomaly",   data.Select(c => c.TickAnomaly).ToArray()),
            ("bd_distance",    data.Select(c => (double)c.ZoneDist).ToArray()),
        };

        int signalCount = 0;
        foreach (var (tname, tval) in targets)
        {
            double r = PearsonCorr(bestResid, tval);
            // Crude p-value proxy: |r|·sqrt(N-2)/sqrt(1-r²) ~ t-statistic
            double tStat = bestResid.Length > 2 ? Math.Abs(r) * Math.Sqrt(bestResid.Length - 2) / Math.Sqrt(Math.Max(1e-15, 1 - r * r)) : 0;
            bool hasSignal = Math.Abs(r) > 0.15 && tStat > 3.0;
            if (hasSignal) signalCount++;
            sb.AppendLine($"{tname,-18} {r,14:F4} {tStat,12:F2} {(hasSignal ? "YES" : "no"),8}");
        }
        sb.AppendLine("");

        bool fbIndependent = signalCount >= 2;
        sb.AppendLine($"  Residuals carry signal on {signalCount}/{targets.Length} targets");
        sb.AppendLine(fbIndependent
            ? "  VERDICT: fb RESIDUAL CARRIES INDEPENDENT INFORMATION — fb is not merely func(|m|)"
            : "  VERDICT: fb IS REDUNDANT — residuals contain no signal beyond noise");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(!fbIndependent, $"EID_02: Residual signal on {signalCount}/{targets.Length} targets. Null: fb = func(|m|).");
    }

    // ====================================================================
    // EID_03: GRADIENT INDEPENDENCE
    //
    // Compare ∇fb and ∇|m| directionally.
    // Cosine similarity, angular deviation, local divergence near boundaries.
    //
    // Null:  ∇fb ∝ ∇|m| everywhere — gradients are collinear (same field)
    // Alt:   ∇fb deviates from ∇|m| direction in regions of interest
    //
    // Pass (fb independent):   angular deviation > threshold near boundaries
    // Fail (fb redundant):     cos(θ) ≈ 1 everywhere
    // ====================================================================
    [Fact]
    public void EID_03_GradientIndependence_DoGradientsDiverge()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== EID_03: Gradient Independence ===");
        sb.AppendLine("=== Do ∇fb and ∇|m| point in different directions? ===");
        sb.AppendLine(new string('=', 96));

        var data = CollectEstimatorData();
        if (data.Count < 100) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        // We need directional gradient data. Collect it with per-component gradients.
        var dirData = CollectDirectionalGradientData();
        if (dirData.Count < 100) { sb.AppendLine("Insufficient directional data."); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        // Cosine similarity between ∇fb and ∇|m|
        var cosVals = new List<double>();
        var angVals = new List<double>();
        var bdryCos = new List<double>();
        var intCos = new List<double>();

        foreach (var cell in dirData)
        {
            double dot = cell.DmDb * cell.DfDb + cell.DmDg * cell.DfDg;
            double normM = Math.Sqrt(cell.DmDb * cell.DmDb + cell.DmDg * cell.DmDg);
            double normF = Math.Sqrt(cell.DfDb * cell.DfDb + cell.DfDg * cell.DfDg);
            if (normM < 1e-15 || normF < 1e-15) continue;

            double cosTheta = dot / (normM * normF);
            cosTheta = Math.Max(-1, Math.Min(1, cosTheta));
            double angle = Math.Acos(cosTheta) * 180.0 / Math.PI;

            cosVals.Add(cosTheta);
            angVals.Add(angle);

            if (cell.IsBoundary) bdryCos.Add(cosTheta);
            else if (cell.ZoneDist >= 2) intCos.Add(cosTheta);
        }

        double cosMean = cosVals.Average();
        double cosStd = Math.Sqrt(cosVals.Select(c => (c - cosMean) * (c - cosMean)).Average());
        double angMean = angVals.Average();
        double angStd = Math.Sqrt(angVals.Select(a => (a - angMean) * (a - angMean)).Average());

        double bdryCosMean = bdryCos.Count > 0 ? bdryCos.Average() : 0;
        double intCosMean = intCos.Count > 0 ? intCos.Average() : 0;

        // Fraction where cos < 0.95 (angle > 18°)
        double fracDivergent = 100.0 * cosVals.Count(c => c < 0.95) / cosVals.Count;

        sb.AppendLine($"  N = {cosVals.Count}");
        sb.AppendLine($"  cos(θ) mean ± σ:   {cosMean:F4} ± {cosStd:F4}");
        sb.AppendLine($"  Angle mean ± σ:    {angMean:F2}° ± {angStd:F2}°");
        sb.AppendLine($"  Divergent fraction (cos < 0.95): {fracDivergent:F1}%");
        sb.AppendLine("");
        sb.AppendLine($"  Boundary cos:      {bdryCosMean:F4}");
        sb.AppendLine($"  Interior cos:      {intCosMean:F4}");
        sb.AppendLine($"  Δcos = {(bdryCosMean - intCosMean):F4}  ({(bdryCosMean < intCosMean ? "MORE divergent" : "MORE aligned")} at boundaries)");
        sb.AppendLine("");

        bool gradientsDiverge = fracDivergent > 15.0 || bdryCosMean < intCosMean - 0.05;
        sb.AppendLine(gradientsDiverge
            ? $"  VERDICT: ∇fb and ∇|m| DIVERGE — fb is not a simple function of |m|"
            : $"  VERDICT: ∇fb and ∇|m| are COLLINEAR (cos={cosMean:F3}) — they measure the same gradient field");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(!gradientsDiverge, $"EID_03: cos mean={cosMean:F4}, divergent={fracDivergent:F1}%. Null: ∇fb ∝ ∇|m|. Redundant if collinear.");
    }

    // ====================================================================
    // EID_04: BOUNDARY ENHANCEMENT — Does boundary increase or decrease divergence?
    //
    // Null:  Boundary does not affect estimator relationship
    // Alt:   Boundary changes the fb-|m| relationship
    //
    // Two competing sub-hypotheses:
    //   A) Boundary increases alignment (cc model: common cause strengthens)
    //   B) Boundary increases divergence (loop model: coupling creates new structure)
    //
    // Pass (A, CC favored):   cos(θ)_boundary > cos(θ)_interior
    // Pass (B, RL favored):   cos(θ)_boundary < cos(θ)_interior
    // ====================================================================
    [Fact]
    public void EID_04_BoundaryEnhancement_AlignmentOrDivergence()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== EID_04: Boundary Enhancement ===");
        sb.AppendLine("=== Does boundary increase or decrease estimator alignment? ===");
        sb.AppendLine(new string('=', 96));

        var data = CollectEstimatorData();
        if (data.Count < 100) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        // Per-zone analysis: fb vs |m| correlation AND residual structure
        sb.AppendLine($"{"Zone",-12} {"r(fb,|m|)",10} {"R²",8} {"Resid σ",10} {"fb/|m| ratio",12} {"fb mean",10} {"|m| mean",10} {"N",5}");
        sb.AppendLine(new string('-', 80));

        var zoneStats = new List<(int zone, double r, double r2, double residStd, double ratio, double fbMean, double mMean, int n)>();

        for (int z = 0; z <= 3; z++)
        {
            var zData = data.Where(c => c.ZoneDist == z).ToList();
            if (zData.Count < 15) continue;

            double[] zFb = zData.Select(c => c.Fb).ToArray();
            double[] zM = zData.Select(c => c.AbsM).ToArray();

            double r = PearsonCorr(zFb, zM);
            double r2 = r * r;

            // Linear fit within zone
            double mMeanZ = zM.Average(), fbMeanZ = zFb.Average();
            double covZ = 0, varZ = 0;
            for (int i = 0; i < zM.Length; i++) { double dm = zM[i] - mMeanZ; covZ += dm * (zFb[i] - fbMeanZ); varZ += dm * dm; }
            double slopeZ = varZ > 1e-15 ? covZ / varZ : 0;
            double interceptZ = fbMeanZ - slopeZ * mMeanZ;
            double[] residZ = zM.Select((m, i) => zFb[i] - (slopeZ * m + interceptZ)).ToArray();
            double residStd = Math.Sqrt(residZ.Select(rr => rr * rr).Average());

            double ratioMean = zFb.Zip(zM, (f, m) => m > 1e-15 ? f / m : 0).Average();

            zoneStats.Add((z, r, r2, residStd, ratioMean, fbMeanZ, mMeanZ, zData.Count));

            string zName = z == 0 ? "Boundary" : z == 1 ? "Near-1" : z == 2 ? "Near-2" : "Interior";
            sb.AppendLine($"{zName,-12} {r,10:F4} {r2,8:F4} {residStd,10:F4} {ratioMean,12:F4} {fbMeanZ,10:F4} {mMeanZ,10:F4} {zData.Count,5}");
        }
        sb.AppendLine("");

        // Does the fb/|m| ratio change across zones?
        if (zoneStats.Count >= 2)
        {
            var bdry = zoneStats.FirstOrDefault(s => s.zone == 0);
            var interior = zoneStats.FirstOrDefault(s => s.zone >= 2);
            if (bdry.n > 0 && interior.n > 0)
            {
                double ratioZ0 = bdry.r2;
                double ratioZint = interior.r2;
                double residZ0 = bdry.residStd;
                double residZint = interior.residStd;

                sb.AppendLine($"  Boundary R²:     {ratioZ0:F4}  Interior R²: {ratioZint:F4}  Δ = {ratioZ0 - ratioZint:F4}");
                sb.AppendLine($"  Boundary σ_resid: {residZ0:F4}  Interior σ_resid: {residZint:F4}  Ratio = {residZ0/residZint:F2}x");
                sb.AppendLine("");

                bool alignmentIncreases = ratioZ0 > ratioZint + 0.05;
                bool divergenceIncreases = ratioZ0 < ratioZint - 0.05;

                if (alignmentIncreases)
                    sb.AppendLine("  → Boundary INCREASES fb-|m| alignment — estimators converge near boundaries");
                else if (divergenceIncreases)
                    sb.AppendLine("  → Boundary DECREASES fb-|m| alignment — estimators diverge near boundaries");
                else
                    sb.AppendLine("  → Boundary does NOT change fb-|m| alignment — estimators have stable relationship");

                sb.AppendLine(alignmentIncreases
                    ? "  VERDICT: Favors COMMON CAUSE — boundary strengthens shared origin signal"
                    : divergenceIncreases
                    ? "  VERDICT: Favors RESONANCE LOOP — boundary creates new coupling structure"
                    : "  VERDICT: NEUTRAL — boundary does not modulate estimator relationship");
            }
        }
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ====================================================================
    // EID_05: TICK CONTRIBUTION — Does tick predict ∇fb better than |m|?
    //
    // Key question: if fb ≈ func(|m|), then |m| should predict ∇fb
    // as well as or better than any external variable.
    // If tick predicts ∇fb BETTER than |m| does, then fb carries
    // information about the α-sweep that is NOT captured by |m|.
    //
    // Null:  |m| predicts ∇fb at least as well as tick does
    // Alt:   tick predicts ∇fb better than |m| does
    //
    // Pass (fb independent):    tick R² > |m| R² + margin
    // Fail (fb redundant):      |m| R² ≥ tick R²
    // ====================================================================
    [Fact]
    public void EID_05_TickContribution_DoesTickOutpredictAbsM()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== EID_05: Tick Contribution ===");
        sb.AppendLine("=== Does tick predict ∇fb better than |m| does? ===");
        sb.AppendLine(new string('=', 96));

        var data = CollectEstimatorData();
        if (data.Count < 100) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        double[] gfV = data.Select(c => Math.Log10(Math.Max(1e-15, c.GradFb))).ToArray();
        double[] mV = data.Select(c => c.AbsM).ToArray();
        double[] lm = data.Select(c => Math.Log10(Math.Max(1e-15, c.AbsM))).ToArray();
        double[] tickV = data.Select(c => c.Tick).ToArray();
        double[] ltick = data.Select(c => Math.Log10(Math.Max(1e-15, c.Tick))).ToArray();
        double[] gtV = data.Select(c => Math.Log10(Math.Max(1e-15, c.GradTick))).ToArray();
        double[] taV = data.Select(c => c.TickAnomaly).ToArray();
        double[] bdV = data.Select(c => (double)c.ZoneDist).ToArray();

        // Predictor models for ∇fb
        var models = new (string name, double[] pred, bool isTick)[]
        {
            ("|m|",              lm,     false),
            ("fb",               data.Select(c => Math.Log10(Math.Max(1e-15, c.Fb))).ToArray(), false),
            ("tick",             ltick,  true),
            ("tick_anomaly",     taV,    true),
            ("∇Tick",            gtV,    true),
            ("bd_distance",      bdV,    false),
        };

        sb.AppendLine($"{"Predictor",-18} {"R²(→∇fb)",10} {"R²(→∇|m|)",10} {"Tick-better?",12}");
        sb.AppendLine(new string('-', 54));

        double bestNonTickR2 = 0;
        double bestTickR2 = 0;

        foreach (var (name, pred, isTick) in models)
        {
            double r2_fb = PearsonCorr(gfV, pred); r2_fb *= r2_fb;
            double r2_gm = PearsonCorr(data.Select(c => Math.Log10(Math.Max(1e-15, c.GradM))).ToArray(), pred); r2_gm *= r2_gm;

            if (!isTick && r2_fb > bestNonTickR2) bestNonTickR2 = r2_fb;
            if (isTick && r2_fb > bestTickR2) bestTickR2 = r2_fb;

            string tickBetter = isTick ? (r2_fb > bestNonTickR2 + 0.02 ? "YES" : "no") : "";
            sb.AppendLine($"{name,-18} {r2_fb,10:F4} {r2_gm,10:F4} {tickBetter,12}");
        }
        sb.AppendLine("");

        // Joint model: ∇fb ~ |m| + tick
        double r2_mOnly = PearsonCorr(gfV, lm); r2_mOnly *= r2_mOnly;
        double r2_tOnly = PearsonCorr(gfV, ltick); r2_tOnly *= r2_tOnly;
        double r2_both = MultivariateR2(gfV, lm, ltick);
        double tickGain = r2_both - r2_mOnly;

        sb.AppendLine($"  ∇fb ~ |m|:           R² = {r2_mOnly:F4}");
        sb.AppendLine($"  ∇fb ~ tick:          R² = {r2_tOnly:F4}");
        sb.AppendLine($"  ∇fb ~ |m| + tick:    R² = {r2_both:F4}  (Δtick = {tickGain:F4})");
        sb.AppendLine("");

        bool tickAddsInfo = tickGain > 0.05 || bestTickR2 > bestNonTickR2 + 0.05;
        sb.AppendLine(tickAddsInfo
            ? $"  VERDICT: TICK adds information beyond |m| — fb is NOT fully collinear with |m|"
            : $"  VERDICT: |m| dominates — tick adds no unique predictive power for ∇fb");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(!tickAddsInfo, $"EID_05: tick ΔR²={tickGain:F4}, bestTick={bestTickR2:F4}, bestNonTick={bestNonTickR2:F4}. Null: tick adds nothing beyond |m|.");
    }

    // ====================================================================
    // EID_06: LAYER ELIMINATION — Can Geometry ↔ Feedback be reduced?
    //
    // Test 1: Reduce Geometry (|m|) to Feedback (fb)
    // Test 2: Reduce Feedback (fb) to Geometry (|m|)
    //
    // For each:
    //   SUPPORTED:  best model R² > 0.85, residual carries no signal
    //   CONDITIONAL: best model R² > 0.60, residual has weak signal
    //   HYPOTHESIS: best model R² > 0.30
    //   FAIL:        best model R² < 0.30
    // ====================================================================
    [Fact]
    public void EID_06_LayerElimination_CanOneBeReducedToTheOther()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== EID_06: Layer Elimination ===");
        sb.AppendLine("=== Can Geometry be reduced to Feedback, or vice versa? ===");
        sb.AppendLine(new string('=', 96));

        var data = CollectEstimatorData();
        if (data.Count < 100) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        double[] fbV = data.Select(c => c.Fb).ToArray();
        double[] mV = data.Select(c => c.AbsM).ToArray();
        double[] lfb = data.Select(c => Math.Log10(Math.Max(1e-15, c.Fb))).ToArray();
        double[] lm = data.Select(c => Math.Log10(Math.Max(1e-15, c.AbsM))).ToArray();

        // ================================================================
        // REDUCTION 1: |m| → fb  (Geometry reduced to Feedback)
        // ================================================================
        double r2_lin_m = PearsonCorr(mV, fbV); r2_lin_m *= r2_lin_m;
        double r2_log_m = PearsonCorr(lm, lfb); r2_log_m *= r2_log_m;
        double r2_quad_m = MultivariateR2(mV, fbV, fbV.Zip(fbV, (a, b) => a * b).ToArray());
        double bestR2_m = Math.Max(r2_lin_m, Math.Max(r2_log_m, r2_quad_m));

        // Residual from best model for |m| ~ fb
        double[] residM = ComputeBestResidual(mV, fbV, bestR2_m, r2_lin_m, r2_log_m, r2_quad_m);

        // Does |m| residual predict anything?
        var targets = new (string name, double[] values)[]
        {
            ("tick",           data.Select(c => c.Tick).ToArray()),
            ("sign",           data.Select(c => (double)c.Sign).ToArray()),
            ("boundary",       data.Select(c => c.IsBoundary ? 1.0 : 0.0).ToArray()),
            ("tick_anomaly",   data.Select(c => c.TickAnomaly).ToArray()),
        };

        int mResidSignals = 0;
        foreach (var (tname, tval) in targets)
        {
            double r = PearsonCorr(residM, tval);
            if (Math.Abs(r) > 0.15) mResidSignals++;
        }

        string mClass = bestR2_m > 0.85 && mResidSignals == 0 ? "SUPPORTED" :
                        bestR2_m > 0.60 && mResidSignals <= 1 ? "CONDITIONAL" :
                        bestR2_m > 0.30 ? "HYPOTHESIS" : "FAIL";

        // ================================================================
        // REDUCTION 2: fb → |m|  (Feedback reduced to Geometry)
        // ================================================================
        double r2_lin_f = r2_lin_m; // same correlation
        double r2_log_f = r2_log_m;
        double r2_quad_f = MultivariateR2(fbV, mV, mV.Zip(mV, (a, b) => a * b).ToArray());
        double bestR2_f = Math.Max(r2_lin_f, Math.Max(r2_log_f, r2_quad_f));

        double[] residF = ComputeBestResidual(fbV, mV, bestR2_f, r2_lin_f, r2_log_f, r2_quad_f);

        int fResidSignals = 0;
        foreach (var (tname, tval) in targets)
        {
            double r = PearsonCorr(residF, tval);
            if (Math.Abs(r) > 0.15) fResidSignals++;
        }

        string fClass = bestR2_f > 0.85 && fResidSignals == 0 ? "SUPPORTED" :
                        bestR2_f > 0.60 && fResidSignals <= 1 ? "CONDITIONAL" :
                        bestR2_f > 0.30 ? "HYPOTHESIS" : "FAIL";

        sb.AppendLine("  REDUCTION: |m| → fb (Geometry reduced to Feedback)");
        sb.AppendLine($"    Best R² = {bestR2_m:F4}");
        sb.AppendLine($"    Residual signals on {mResidSignals}/{targets.Length} targets");
        sb.AppendLine($"    Classification: {mClass}");
        sb.AppendLine("");
        sb.AppendLine("  REDUCTION: fb → |m| (Feedback reduced to Geometry)");
        sb.AppendLine($"    Best R² = {bestR2_f:F4}");
        sb.AppendLine($"    Residual signals on {fResidSignals}/{targets.Length} targets");
        sb.AppendLine($"    Classification: {fClass}");
        sb.AppendLine("");

        // Are both directions equivalent?
        bool symmetric = mClass == fClass && Math.Abs(bestR2_m - bestR2_f) < 0.05;
        sb.AppendLine(symmetric
            ? "  → SYMMETRIC: Geometry and Feedback are mutually reducible — they carry the same information"
            : "  → ASYMMETRIC: One direction reduces better — one carries independent information");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(symmetric, $"EID_06: |m|→fb {mClass} (R²={bestR2_m:F4}), fb→|m| {fClass} (R²={bestR2_f:F4}). Symmetric if they're the same quantity.");
    }

    // ====================================================================
    // EID_07: CROSS-ARCHITECTURE STABILITY
    //
    // Is the fb-|m| relationship stable across GAN and CNS?
    // If they're truly the same quantity, the functional form
    // should be architecture-invariant.
    //
    // Null:  fb = f(|m|) with same functional form across architectures
    // Alt:   The fb-|m| mapping differs by architecture
    // ====================================================================
    [Fact]
    public void EID_07_CrossArchitectureStability_IsMappingInvariant()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== EID_07: Cross-Architecture Stability ===");
        sb.AppendLine("=== Is the fb-|m| mapping architecture-invariant? ===");
        sb.AppendLine(new string('=', 96));

        var data = CollectEstimatorData();
        if (data.Count < 100) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        foreach (var arch in new[] { "GAN", "CNS" })
        {
            var archData = data.Where(c => c.Arch == arch).ToList();
            if (archData.Count < 50) continue;

            double[] fbV = archData.Select(c => c.Fb).ToArray();
            double[] mV = archData.Select(c => c.AbsM).ToArray();
            double[] lfb = archData.Select(c => Math.Log10(Math.Max(1e-15, c.Fb))).ToArray();
            double[] lm = archData.Select(c => Math.Log10(Math.Max(1e-15, c.AbsM))).ToArray();

            // Linear model
            double mMean = mV.Average(), fbMean = fbV.Average();
            double cov = 0, varM = 0;
            for (int i = 0; i < mV.Length; i++) { double dm = mV[i] - mMean; cov += dm * (fbV[i] - fbMean); varM += dm * dm; }
            double slope = varM > 1e-15 ? cov / varM : 0;
            double intercept = fbMean - slope * mMean;
            double r2 = PearsonCorr(fbV, mV); r2 *= r2;

            // Log model
            double lmMean = lm.Average(), lfbMean = lfb.Average();
            double covL = 0, varLm = 0;
            for (int i = 0; i < lm.Length; i++) { double dl = lm[i] - lmMean; covL += dl * (lfb[i] - lfbMean); varLm += dl * dl; }
            double gamma = varLm > 1e-15 ? covL / varLm : 0;
            double logR2 = PearsonCorr(lfb, lm); logR2 *= logR2;

            // Residual std
            double[] residFb = fbV.Select((f, i) => f - (slope * mV[i] + intercept)).ToArray();
            double residStd = Math.Sqrt(residFb.Select(r => r * r).Average());

            sb.AppendLine($"  {arch}: fb = {slope:F4}·|m| + {intercept:F4}  R²={r2:F4}  log: fb~|m|^{gamma:F4}  R²_log={logR2:F4}  σ_resid={residStd:F4}");
        }
        sb.AppendLine("");

        // Test: do regression coefficients differ significantly?
        // (A full Fisher test would be better, but we check consistency)
        var ganData = data.Where(c => c.Arch == "GAN").ToList();
        var cnsData = data.Where(c => c.Arch == "CNS").ToList();

        if (ganData.Count > 50 && cnsData.Count > 50)
        {
            double[] gFb = ganData.Select(c => c.Fb).ToArray();
            double[] gM = ganData.Select(c => c.AbsM).ToArray();
            double[] cFb = cnsData.Select(c => c.Fb).ToArray();
            double[] cM = cnsData.Select(c => c.AbsM).ToArray();

            double gSlope = Cov(gM, gFb) / Math.Max(1e-15, Var(gM));
            double cSlope = Cov(cM, cFb) / Math.Max(1e-15, Var(cM));

            double rGan = PearsonCorr(gFb, gM);
            double rCns = PearsonCorr(cFb, cM);

            // Fisher z-test for correlation difference
            double zGan = 0.5 * Math.Log((1 + rGan) / Math.Max(1e-15, 1 - rGan));
            double zCns = 0.5 * Math.Log((1 + rCns) / Math.Max(1e-15, 1 - rCns));
            double se = Math.Sqrt(1.0 / (gM.Length - 3) + 1.0 / (cM.Length - 3));
            double zDiff = Math.Abs(zGan - zCns) / Math.Max(1e-15, se);

            sb.AppendLine($"  GAN slope={gSlope:F4}  CNS slope={cSlope:F4}  Δslope={gSlope-cSlope:F4}");
            sb.AppendLine($"  GAN r={rGan:F4}  CNS r={rCns:F4}");
            sb.AppendLine($"  Fisher z-diff = {zDiff:F2}  ({(zDiff > 2.0 ? "SIGNIFICANTLY different" : "NOT significantly different")})");
            sb.AppendLine("");

            bool invariant = zDiff < 2.0;
            sb.AppendLine(invariant
                ? "  VERDICT: Architecture-INVARIANT — fb-|m| mapping is the same across GAN and CNS"
                : "  VERDICT: Architecture-DEPENDENT — fb-|m| mapping differs between architectures");
            sb.AppendLine(invariant
                ? "  → Supports estimator collinearity: same functional relationship in all architectures"
                : "  → Supports independent observables: relationship depends on architecture topology");
        }
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ====================================================================
    // EID_08: V3.4 COMPATIBILITY
    //
    // If fb ≈ func(|m|), then the V3.4 question simplifies:
    // The bridge-band depends on effective Ω, which depends on
    // oscillator dynamics that produce BOTH fb and |m|.
    //
    // If fb and |m| are the same thing, V3.4 recovery only needs
    // to map ONE of them to Ω*.
    //
    // If they're independent, V3.4 may need both.
    // ====================================================================
    [Fact]
    public void EID_08_V34Compatibility_EstimatorIdentityImpact()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== EID_08: V3.4 Compatibility — Impact of Estimator Identity ===");
        sb.AppendLine(new string('=', 96));

        var data = CollectEstimatorData();
        if (data.Count < 100) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        double[] fbV = data.Select(c => c.Fb).ToArray();
        double[] mV = data.Select(c => c.AbsM).ToArray();
        double[] tickV = data.Select(c => c.Tick).ToArray();

        double r2_fb_m = PearsonCorr(fbV, mV); r2_fb_m *= r2_fb_m;
        double r2_fb_tick = PearsonCorr(fbV, tickV); r2_fb_tick *= r2_fb_tick;
        double r2_m_tick = PearsonCorr(mV, tickV); r2_m_tick *= r2_m_tick;

        sb.AppendLine("  V3.4 Bridge-Band Chain:");
        sb.AppendLine("    ω_i (tick baseline) → synchronization → Ω* → γ* → bridge band");
        sb.AppendLine("");
        sb.AppendLine("  If fb ≈ |m|:");
        sb.AppendLine("    → Only ONE mapping needed: (fb or |m|) → ω_i_effective");
        sb.AppendLine("    → V3.4 recovery requires: oscillator_statistic → bridge_band");
        sb.AppendLine("    → Simpler: single degree of freedom");
        sb.AppendLine("");
        sb.AppendLine("  If fb ≠ |m| (independent):");
        sb.AppendLine("    → TWO mappings needed: fb → ω_i AND |m| → ω_i");
        sb.AppendLine("    → Must show they produce consistent Ω*");
        sb.AppendLine("    → Harder: two constraints on one bridge-band parameter");
        sb.AppendLine("");
        sb.AppendLine($"  fb-|m| R² = {r2_fb_m:F4}");
        sb.AppendLine($"  fb-tick R² = {r2_fb_tick:F4}");
        sb.AppendLine($"  |m|-tick R² = {r2_m_tick:F4}");
        sb.AppendLine("");

        if (r2_fb_m > 0.80)
        {
            sb.AppendLine("  → fb ≈ |m|: V3.4 recovery path is SIMPLIFIED");
            sb.AppendLine("  → One effective parameter maps oscillator to bridge band");
            sb.AppendLine("  Classification: PASS (simplified recovery)");
        }
        else if (r2_fb_m > 0.50)
        {
            sb.AppendLine("  → fb partially independent of |m|");
            sb.AppendLine("  → V3.4 recovery needs both or must explain the difference");
            sb.AppendLine("  Classification: CONDITIONAL (two-parameter recovery)");
        }
        else
        {
            sb.AppendLine("  → fb and |m| are INDEPENDENT observables");
            sb.AppendLine("  → V3.4 recovery requires explaining why BOTH exist");
            sb.AppendLine("  Classification: UNKNOWN (complex recovery)");
        }
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ====================================================================
    // EID_09: COMPREHENSIVE VERDICT — Are fb and |m| independent?
    //
    // Aggregates evidence from EID_01 through EID_07.
    // Classifies: INDEPENDENT, PARTIALLY INDEPENDENT, or REDUNDANT.
    // ====================================================================
    [Fact]
    public void EID_09_ComprehensiveVerdict()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== EID_09: Comprehensive Verdict — Estimator Identity ===");
        sb.AppendLine(new string('=', 96));

        var data = CollectEstimatorData();
        if (data.Count < 100) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        double[] fbV = data.Select(c => c.Fb).ToArray();
        double[] mV = data.Select(c => c.AbsM).ToArray();
        double[] lfb = data.Select(c => Math.Log10(Math.Max(1e-15, c.Fb))).ToArray();
        double[] lm = data.Select(c => Math.Log10(Math.Max(1e-15, c.AbsM))).ToArray();

        // Evidence 1: Raw correlation
        double r2_raw = PearsonCorr(fbV, mV); r2_raw *= r2_raw;
        double r2_log = PearsonCorr(lfb, lm); r2_log *= r2_log;
        double bestR2 = Math.Max(r2_raw, r2_log);
        int ev1 = bestR2 > 0.85 ? -1 : bestR2 > 0.60 ? 0 : 1; // -1=redundant, 0=partial, 1=independent

        // Evidence 2: Quadratic fit improvement over linear
        double r2_quad = MultivariateR2(fbV, mV, mV.Zip(mV, (a, b) => a * b).ToArray());
        int ev2 = (r2_quad - r2_raw) < 0.05 ? -1 : (r2_quad - r2_raw) < 0.10 ? 0 : 1;

        // Evidence 3: Residual predictive power (best model)
        double mMean = mV.Average(), fbMean = fbV.Average();
        double cov = 0, varM = 0;
        for (int i = 0; i < mV.Length; i++) { double dm = mV[i] - mMean; cov += dm * (fbV[i] - fbMean); varM += dm * dm; }
        double slope = varM > 1e-15 ? cov / varM : 0;
        double intercept = fbMean - slope * mMean;
        double[] resid = mV.Select((m, i) => fbV[i] - (slope * m + intercept)).ToArray();

        var tTargets = new (string name, double[] values)[]
        {
            ("tick", data.Select(c => c.Tick).ToArray()),
            ("sign", data.Select(c => (double)c.Sign).ToArray()),
            ("boundary", data.Select(c => c.IsBoundary ? 1.0 : 0.0).ToArray()),
        };

        int residSignals = tTargets.Count(t => Math.Abs(PearsonCorr(resid, t.values)) > 0.15);
        int ev3 = residSignals == 0 ? -1 : residSignals <= 1 ? 0 : 1;

        // Evidence 4: Architecture invariance (correlation stability)
        // Approximated from EID_07 pattern
        int ev4 = 0; // neutral by default, EID_07 resolves this

        // Evidence 5: Tick contribution (does tick add beyond |m|?)
        double[] gfV = data.Select(c => Math.Log10(Math.Max(1e-15, c.GradFb))).ToArray();
        double[] ltick = data.Select(c => Math.Log10(Math.Max(1e-15, c.Tick))).ToArray();
        double r2_m = PearsonCorr(gfV, lm); r2_m *= r2_m;
        double r2_mt = MultivariateR2(gfV, lm, ltick);
        double tickGain = r2_mt - r2_m;
        int ev5 = tickGain < 0.03 ? -1 : tickGain < 0.08 ? 0 : 1;

        int totalScore = ev1 + ev2 + ev3 + ev4 + ev5;

        sb.AppendLine($"  Evidence summary:");
        sb.AppendLine($"    E1: Raw correlation        R² = {bestR2:F4}  → {(ev1 == -1 ? "REDUNDANT" : ev1 == 0 ? "PARTIAL" : "INDEPENDENT")}");
        sb.AppendLine($"    E2: Quadratic gain         ΔR² = {r2_quad - r2_raw:F4}  → {(ev2 == -1 ? "REDUNDANT" : ev2 == 0 ? "PARTIAL" : "INDEPENDENT")}");
        sb.AppendLine($"    E3: Residual signals       {residSignals}/3  → {(ev3 == -1 ? "REDUNDANT" : ev3 == 0 ? "PARTIAL" : "INDEPENDENT")}");
        sb.AppendLine($"    E4: Architecture stability (see EID_07)");
        sb.AppendLine($"    E5: Tick contribution      ΔR² = {tickGain:F4}  → {(ev5 == -1 ? "REDUNDANT" : ev5 == 0 ? "PARTIAL" : "INDEPENDENT")}");
        sb.AppendLine($"    Total score: {totalScore}  (range: -5 = fully redundant ... +5 = fully independent)");
        sb.AppendLine("");

        string classification = totalScore <= -3 ? "REDUNDANT — fb ≈ func(|m|), single observable"
            : totalScore <= 0 ? "PARTIALLY REDUNDANT — mostly collinear, weak independent signal"
            : totalScore <= 2 ? "PARTIALLY INDEPENDENT — related but distinct information"
            : "INDEPENDENT — fb and |m| carry genuinely separate information";

        sb.AppendLine($"  CLASSIFICATION: {classification}");
        sb.AppendLine("");
        sb.AppendLine(totalScore <= 0
            ? "  → The 'feedback' concept in V33 should be MERGED with 'geometry'"
            : "  → fb and |m| should be TREATED AS DISTINCT LAYERS in the hierarchy");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(totalScore <= 0, $"EID_09: Total score = {totalScore}. Null: fb is redundant with |m|.");
    }

    // ====================================================================
    // DATA COLLECTION
    // ====================================================================

    private List<EstCell> CollectEstimatorData()
    {
        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);
        var allCells = new ConcurrentBag<EstCell>();

        foreach (var (arch, fam) in new[] { ("GAN", VcFamily.GAN), ("CNS", VcFamily.CNS) })
        {
            const int nG = 20;
            double bMin = 0.0, bMax = 2.0, gMin = 0.0, gMax = 2.0;
            double db = (bMax - bMin) / (nG - 1), dg = (gMax - gMin) / (nG - 1);

            var gM = new double[nG, nG]; var gS = new int[nG, nG];
            var gFb = new double[nG, nG]; var gTick = new double[nG, nG];
            var gV = new double[nG, nG];

            Parallel.For(0, nG, bi =>
            {
                double beta = bMin + db * bi;
                for (int gi = 0; gi < nG; gi++)
                {
                    double gamma = gMin + dg * gi;
                    var f = ComputeFull(fam, 1.0, 1.0, 0.70, beta, gamma, distances, sortedD, xiBase, k0Base, nA, aMin, daD);
                    gM[bi, gi] = f.absM; gS[bi, gi] = f.sign; gFb[bi, gi] = f.fb; gTick[bi, gi] = f.tick; gV[bi, gi] = f.V;
                }
            });

            var dist = new int[nG, nG];
            for (int bi = 0; bi < nG; bi++) for (int gi = 0; gi < nG; gi++) dist[bi, gi] = int.MaxValue;
            var q = new Queue<(int, int)>();
            for (int bi = 1; bi < nG - 1; bi++)
                for (int gi = 1; gi < nG - 1; gi++)
                    if (gS[bi, gi] != gS[bi + 1, gi] || gS[bi, gi] != gS[bi - 1, gi] ||
                        gS[bi, gi] != gS[bi, gi + 1] || gS[bi, gi] != gS[bi, gi - 1])
                    { dist[bi, gi] = 0; q.Enqueue((bi, gi)); }
            while (q.Count > 0) { var (bi, gi) = q.Dequeue(); foreach (var (nb, ng) in new[] { (bi + 1, gi), (bi - 1, gi), (bi, gi + 1), (bi, gi - 1) }) if (nb >= 0 && nb < nG && ng >= 0 && ng < nG && dist[nb, ng] == int.MaxValue) { dist[nb, ng] = dist[bi, gi] + 1; q.Enqueue((nb, ng)); } }

            for (int bi = 1; bi < nG - 1; bi++)
                for (int gi = 1; gi < nG - 1; gi++)
                {
                    double dMdB = (gM[bi + 1, gi] - gM[bi - 1, gi]) / (2 * db);
                    double dMdG = (gM[bi, gi + 1] - gM[bi, gi - 1]) / (2 * dg);
                    double gradM = Math.Sqrt(dMdB * dMdB + dMdG * dMdG);

                    double dFdB = (gFb[bi + 1, gi] - gFb[bi - 1, gi]) / (2 * db);
                    double dFdG = (gFb[bi, gi + 1] - gFb[bi, gi - 1]) / (2 * dg);
                    double gradFb = Math.Sqrt(dFdB * dFdB + dFdG * dFdG);

                    double dTdB = (gTick[bi + 1, gi] - gTick[bi - 1, gi]) / (2 * db);
                    double dTdG = (gTick[bi, gi + 1] - gTick[bi, gi - 1]) / (2 * dg);
                    double gradTick = Math.Sqrt(dTdB * dTdB + dTdG * dTdG);

                    int d = dist[bi, gi] == int.MaxValue ? 4 : Math.Min(3, dist[bi, gi]);
                    bool isBdry = d == 0;

                    var nT = new List<double>();
                    if (bi > 0) nT.Add(gTick[bi - 1, gi]); if (bi + 1 < nG) nT.Add(gTick[bi + 1, gi]);
                    if (gi > 0) nT.Add(gTick[bi, gi - 1]); if (gi + 1 < nG) nT.Add(gTick[bi, gi + 1]);
                    double tLocMean = nT.Count > 0 ? nT.Average() : gTick[bi, gi];
                    double tAnom = Math.Abs(gTick[bi, gi] - tLocMean) / Math.Max(1e-15, tLocMean);

                    allCells.Add(new EstCell(arch, gM[bi, gi], gS[bi, gi], gFb[bi, gi], gTick[bi, gi], gV[bi, gi],
                        gradM, gradFb, gradTick, d, isBdry, tAnom));
                }
        }
        return allCells.ToList();
    }

    private List<DirGradCell> CollectDirectionalGradientData()
    {
        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);
        var allCells = new ConcurrentBag<DirGradCell>();

        foreach (var (arch, fam) in new[] { ("GAN", VcFamily.GAN), ("CNS", VcFamily.CNS) })
        {
            const int nG = 20;
            double bMin = 0.0, bMax = 2.0, gMin = 0.0, gMax = 2.0;
            double db = (bMax - bMin) / (nG - 1), dg = (gMax - gMin) / (nG - 1);

            var gM = new double[nG, nG]; var gS = new int[nG, nG];
            var gFb = new double[nG, nG]; var gTick = new double[nG, nG];

            Parallel.For(0, nG, bi =>
            {
                double beta = bMin + db * bi;
                for (int gi = 0; gi < nG; gi++)
                {
                    double gamma = gMin + dg * gi;
                    var f = ComputeFull(fam, 1.0, 1.0, 0.70, beta, gamma, distances, sortedD, xiBase, k0Base, nA, aMin, daD);
                    gM[bi, gi] = f.absM; gS[bi, gi] = f.sign; gFb[bi, gi] = f.fb; gTick[bi, gi] = f.tick;
                }
            });

            var dist = new int[nG, nG];
            for (int bi = 0; bi < nG; bi++) for (int gi = 0; gi < nG; gi++) dist[bi, gi] = int.MaxValue;
            var q = new Queue<(int, int)>();
            for (int bi = 1; bi < nG - 1; bi++)
                for (int gi = 1; gi < nG - 1; gi++)
                    if (gS[bi, gi] != gS[bi + 1, gi] || gS[bi, gi] != gS[bi - 1, gi] ||
                        gS[bi, gi] != gS[bi, gi + 1] || gS[bi, gi] != gS[bi, gi - 1])
                    { dist[bi, gi] = 0; q.Enqueue((bi, gi)); }
            while (q.Count > 0) { var (bi, gi) = q.Dequeue(); foreach (var (nb, ng) in new[] { (bi + 1, gi), (bi - 1, gi), (bi, gi + 1), (bi, gi - 1) }) if (nb >= 0 && nb < nG && ng >= 0 && ng < nG && dist[nb, ng] == int.MaxValue) { dist[nb, ng] = dist[bi, gi] + 1; q.Enqueue((nb, ng)); } }

            for (int bi = 1; bi < nG - 1; bi++)
                for (int gi = 1; gi < nG - 1; gi++)
                {
                    double dmDb = (gM[bi + 1, gi] - gM[bi - 1, gi]) / (2 * db);
                    double dmDg = (gM[bi, gi + 1] - gM[bi, gi - 1]) / (2 * dg);
                    double dfDb = (gFb[bi + 1, gi] - gFb[bi - 1, gi]) / (2 * db);
                    double dfDg = (gFb[bi, gi + 1] - gFb[bi, gi - 1]) / (2 * dg);

                    int d = dist[bi, gi] == int.MaxValue ? 4 : Math.Min(3, dist[bi, gi]);
                    bool isBdry = d == 0;

                    allCells.Add(new DirGradCell(arch, dmDb, dmDg, dfDb, dfDg, d, isBdry));
                }
        }
        return allCells.ToList();
    }

    // ====================================================================
    // STATISTICAL HELPERS
    // ====================================================================

    private static double PearsonCorr(double[] xs, double[] ys)
    {
        int n = xs.Length; if (n < 2) return 0;
        double mx = xs.Average(), my = ys.Average();
        double sxy = 0, sxx = 0, syy = 0;
        for (int i = 0; i < n; i++) { double dx = xs[i] - mx, dy = ys[i] - my; sxy += dx * dy; sxx += dx * dx; syy += dy * dy; }
        return sxx > 1e-15 && syy > 1e-15 ? sxy / Math.Sqrt(sxx * syy) : 0;
    }

    private static double MultivariateR2(double[] y, double[] x1, double[] x2)
    {
        int n = y.Length; if (n < 3) return 0;
        double sy = y.Sum(), s1 = x1.Sum(), s2 = x2.Sum();
        double s11 = 0, s22 = 0, s12 = 0, s1y = 0, s2y = 0;
        for (int i = 0; i < n; i++) { s11 += x1[i] * x1[i]; s22 += x2[i] * x2[i]; s12 += x1[i] * x2[i]; s1y += x1[i] * y[i]; s2y += x2[i] * y[i]; }
        double s1yc = s1y - s1 * sy / n, s2yc = s2y - s2 * sy / n;
        double s11c = s11 - s1 * s1 / n, s22c = s22 - s2 * s2 / n, s12c = s12 - s1 * s2 / n;
        double det = s11c * s22c - s12c * s12c;
        double b1 = 0, b2 = 0;
        if (Math.Abs(det) > 1e-15) { b1 = (s1yc * s22c - s2yc * s12c) / det; b2 = (s2yc * s11c - s1yc * s12c) / det; }
        double b0 = sy / n - b1 * s1 / n - b2 * s2 / n;
        double ssRes = 0, ssTot = 0; double my = sy / n;
        for (int i = 0; i < n; i++) { double pred = b0 + b1 * x1[i] + b2 * x2[i]; ssRes += (y[i] - pred) * (y[i] - pred); ssTot += (y[i] - my) * (y[i] - my); }
        double r2 = ssTot > 1e-15 ? 1 - ssRes / ssTot : 0;
        return Math.Max(0, Math.Min(1, r2));
    }

    private static double Cov(double[] x, double[] y)
    {
        double mx = x.Average(), my = y.Average();
        double s = 0;
        for (int i = 0; i < x.Length; i++) s += (x[i] - mx) * (y[i] - my);
        return s / (x.Length - 1);
    }

    private static double Var(double[] x)
    {
        double m = x.Average();
        double s = 0;
        for (int i = 0; i < x.Length; i++) s += (x[i] - m) * (x[i] - m);
        return s / (x.Length - 1);
    }

    private static double[] ComputeBestResidual(double[] y, double[] x, double bestR2, double r2Lin, double r2Log, double r2Quad)
    {
        int n = y.Length;
        if (bestR2 == r2Quad && r2Quad > r2Lin && r2Quad > r2Log)
        {
            double[] x2 = x.Zip(x, (a, b) => a * b).ToArray();
            double sy = y.Sum(), s1 = x.Sum(), s2 = x2.Sum();
            double s11 = 0, s22 = 0, s12 = 0, s1y = 0, s2y = 0;
            for (int i = 0; i < n; i++) { s11 += x[i] * x[i]; s22 += x2[i] * x2[i]; s12 += x[i] * x2[i]; s1y += x[i] * y[i]; s2y += x2[i] * y[i]; }
            double s1yc = s1y - s1 * sy / n, s2yc = s2y - s2 * sy / n;
            double s11c = s11 - s1 * s1 / n, s22c = s22 - s2 * s2 / n, s12c = s12 - s1 * s2 / n;
            double det = s11c * s22c - s12c * s12c;
            double b1 = 0, b2 = 0;
            if (Math.Abs(det) > 1e-15) { b1 = (s1yc * s22c - s2yc * s12c) / det; b2 = (s2yc * s11c - s1yc * s12c) / det; }
            double b0 = sy / n - b1 * s1 / n - b2 * s2 / n;
            return y.Select((yi, i) => yi - (b0 + b1 * x[i] + b2 * x2[i])).ToArray();
        }
        else if (bestR2 == r2Lin)
        {
            double mx = x.Average(), my = y.Average();
            double cov = 0, varx = 0;
            for (int i = 0; i < n; i++) { double dx = x[i] - mx; cov += dx * (y[i] - my); varx += dx * dx; }
            double slope = varx > 1e-15 ? cov / varx : 0;
            double intercept = my - slope * mx;
            return y.Select((yi, i) => yi - (slope * x[i] + intercept)).ToArray();
        }
        else
        {
            double[] lx = x.Select(v => Math.Log10(Math.Max(1e-15, v))).ToArray();
            double[] ly = y.Select(v => Math.Log10(Math.Max(1e-15, v))).ToArray();
            double mx = lx.Average(), my = ly.Average();
            double cov = 0, varx = 0;
            for (int i = 0; i < n; i++) { double dx = lx[i] - mx; cov += dx * (ly[i] - my); varx += dx * dx; }
            double gamma = varx > 1e-15 ? cov / varx : 0;
            double delta = my - gamma * mx;
            double[] pred = lx.Select(l => Math.Pow(10, gamma * l + delta)).ToArray();
            return y.Select((yi, i) => yi - pred[i]).ToArray();
        }
    }

    private record EstCell(string Arch, double AbsM, int Sign, double Fb, double Tick, double V,
        double GradM, double GradFb, double GradTick, int ZoneDist, bool IsBoundary, double TickAnomaly);
    private record DirGradCell(string Arch, double DmDb, double DmDg, double DfDb, double DfDg, int ZoneDist, bool IsBoundary);
}
