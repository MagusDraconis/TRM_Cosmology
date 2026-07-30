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
public class V33_13_SharedCoreDecomposition_Tests
{
    private readonly ITestOutputHelper _o;
    public V33_13_SharedCoreDecomposition_Tests(ITestOutputHelper o) { _o = o; }

    // ====================================================================
    // Data structures for decomposed observables
    // ====================================================================
    private record DecomposedCell(
        string Arch,
        double AbsM, double Fb, double Tick, int Sign,
        double GradM, double GradFb, double GradTick, double TickAnomaly,
        int ZoneDist, bool IsBoundary,
        // Shared core + residuals (computed after collection)
        double Core, double FbResidual, double MResidual);

    // ====================================================================
    // SCD_01: CONSTRUCT SHARED CORE
    //
    // Extract C = first principal component of [standardize(fb), standardize(|m|)]
    // Compute residuals: fb_res = fb - E[fb|C], m_res = |m| - E[|m||C]
    //
    // Report:
    //   - Variance explained by C
    //   - Residual standard deviations
    //   - Signal-to-noise ratio
    // ====================================================================
    [Fact]
    public void SCD_01_ConstructSharedCore_VarianceDecomposition()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== SCD_01: Construct Shared Core ===");
        sb.AppendLine("=== Extract C = PC1(fb, |m|), decompose into core + residuals ===");
        sb.AppendLine(new string('=', 96));

        var raw = CollectRawData();
        if (raw.Count < 100) { sb.AppendLine($"Insufficient: {raw.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        // Standardize fb and |m|
        double[] fbV = raw.Select(c => c.Fb).ToArray();
        double[] mV = raw.Select(c => c.AbsM).ToArray();

        double fbMean = fbV.Average(), fbStd = Math.Sqrt(fbV.Select(v => (v - fbMean) * (v - fbMean)).Average());
        double mMean = mV.Average(), mStd = Math.Sqrt(mV.Select(v => (v - mMean) * (v - mMean)).Average());

        double[] zFb = fbV.Select(v => fbStd > 1e-15 ? (v - fbMean) / fbStd : 0).ToArray();
        double[] zM = mV.Select(v => mStd > 1e-15 ? (v - mMean) / mStd : 0).ToArray();

        // Covariance of standardized variables
        int n = zFb.Length;
        double sFF = 0, sMM = 0, sFM = 0;
        for (int i = 0; i < n; i++) { sFF += zFb[i] * zFb[i]; sMM += zM[i] * zM[i]; sFM += zFb[i] * zM[i]; }
        sFF /= (n - 1); sMM /= (n - 1); sFM /= (n - 1);

        // Eigen-decomposition of 2×2 covariance matrix
        // λ = (tr ± sqrt(tr² - 4·det)) / 2
        double trace = sFF + sMM;
        double det = sFF * sMM - sFM * sFM;
        double disc = Math.Sqrt(Math.Max(0, trace * trace - 4 * det));
        double lambda1 = (trace + disc) / 2.0;
        double lambda2 = (trace - disc) / 2.0;
        double varExplained = 100.0 * lambda1 / (lambda1 + lambda2);

        // First eigenvector (principal component direction)
        // Eigenvector for λ1: [sFM, λ1 - sFF] (unnormalized)
        double ev1_fb = sFM;
        double ev1_m = lambda1 - sFF;
        double evNorm = Math.Sqrt(ev1_fb * ev1_fb + ev1_m * ev1_m);
        if (evNorm < 1e-15) { ev1_fb = 1; ev1_m = 0; evNorm = 1; }
        double wFb = ev1_fb / evNorm;
        double wM = ev1_m / evNorm;

        // C = w_fb * zFb + w_m * zM (first PC — the shared core in z-score space)
        double[] coreZ = zFb.Zip(zM, (f, m) => wFb * f + wM * m).ToArray();

        // Map core back to natural units: C_natural = coreZ * core_std + core_mean
        // But we work in z-space for decomposition; residuals are in z-space too
        // fb_z_residual = zFb - E[zFb | coreZ] = zFb - (cov(zFb, coreZ) / var(coreZ)) * coreZ
        double coreMean = coreZ.Average();
        double cVar = 0, cCovF = 0, cCovM = 0;
        for (int i = 0; i < n; i++) { double dc = coreZ[i] - coreMean; cVar += dc * dc; cCovF += dc * (zFb[i]); cCovM += dc * (zM[i]); }
        cVar /= (n - 1); cCovF /= (n - 1); cCovM /= (n - 1);

        double betaF = cVar > 1e-15 ? cCovF / cVar : 0;
        double betaM = cVar > 1e-15 ? cCovM / cVar : 0;

        // Residuals in z-space
        double[] zFbRes = coreZ.Zip(zFb, (c, f) => f - betaF * c).ToArray();
        double[] zMRes = coreZ.Zip(zM, (c, m) => m - betaM * c).ToArray();

        // Residual variances
        double fbResVar = zFbRes.Select(r => r * r).Average();
        double mResVar = zMRes.Select(r => r * r).Average();

        // R² of each variable explained by core
        double r2Fb = 1.0 - fbResVar / Math.Max(1e-15, zFb.Select(v => v * v).Average());
        double r2M = 1.0 - mResVar / Math.Max(1e-15, zM.Select(v => v * v).Average());

        sb.AppendLine($"  Covariance matrix (standardized):");
        sb.AppendLine($"    var(zFb) = {sFF:F4}  var(zM) = {sMM:F4}  cov(zFb,zM) = {sFM:F4}");
        sb.AppendLine($"    Correlation: r = {sFM / Math.Sqrt(sFF*sMM):F4}  R² = {sFM*sFM/(sFF*sMM):F4}");
        sb.AppendLine("");
        sb.AppendLine($"  PCA eigenvalues: λ₁ = {lambda1:F4}  λ₂ = {lambda2:F4}");
        sb.AppendLine($"  Variance explained by C: {varExplained:F1}%");
        sb.AppendLine($"  PC1 loadings: fb = {wFb:F4}  |m| = {wM:F4}");
        sb.AppendLine("");
        sb.AppendLine($"  Decomposition:");
        sb.AppendLine($"    fb:  R²(C) = {r2Fb:F4}  σ_resid = {Math.Sqrt(fbResVar):F4}");
        sb.AppendLine($"    |m|: R²(C) = {r2M:F4}  σ_resid = {Math.Sqrt(mResVar):F4}");
        sb.AppendLine("");

        bool singleComponent = varExplained > 85;
        sb.AppendLine(singleComponent
            ? "  → ONE DOMINANT COMPONENT: fb and |m| are largely redundant"
            : "  → TWO COMPONENTS: fb and |m| carry independent variance");
        sb.AppendLine(singleComponent
            ? "  VERDICT: SharedCore captures essentially all signal. Residuals are noise-dominated."
            : "  VERDICT: Both components carry signal. fb and |m| are NOT fully collinear.");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(singleComponent, $"SCD_01: λ₁ explains {varExplained:F1}%. Redundant if >85%.");
    }

    // ====================================================================
    // SCD_02: TICK CORRELATION — Does tick correlate with C or residuals?
    //
    // If fb and |m| are redundant, tick should correlate with C,
    // NOT with residuals (which are noise).
    //
    // If residuals carry signal, they might correlate with tick.
    //
    // Null:  r(tick, C) ≫ r(tick, fb_res) and r(tick, m_res)
    // Alt:   Residuals carry tick-relevant information
    // ====================================================================
    [Fact]
    public void SCD_02_TickCorrelation_CoreVsResiduals()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== SCD_02: Tick Correlation — Core vs Residuals ===");
        sb.AppendLine("=== Does tick couple to the shared core or to residuals? ===");
        sb.AppendLine(new string('=', 96));

        var decomp = Decompose();
        if (decomp.Count < 100) { sb.AppendLine($"Insufficient: {decomp.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        double[] coreV = decomp.Select(c => c.Core).ToArray();
        double[] fbResV = decomp.Select(c => c.FbResidual).ToArray();
        double[] mResV = decomp.Select(c => c.MResidual).ToArray();
        double[] tickV = decomp.Select(c => c.Tick).ToArray();
        double[] ltick = decomp.Select(c => Math.Log10(Math.Max(1e-15, c.Tick))).ToArray();
        double[] taV = decomp.Select(c => c.TickAnomaly).ToArray();

        // Correlations with raw tick
        double r_tick_core = PearsonCorr(tickV, coreV);
        double r_tick_fbRes = PearsonCorr(tickV, fbResV);
        double r_tick_mRes = PearsonCorr(tickV, mResV);

        // Correlations with log tick
        double r_ltick_core = PearsonCorr(ltick, coreV);
        double r_ltick_fbRes = PearsonCorr(ltick, fbResV);
        double r_ltick_mRes = PearsonCorr(ltick, mResV);

        // Correlations with tick anomaly
        double r_ta_core = PearsonCorr(taV, coreV);
        double r_ta_fbRes = PearsonCorr(taV, fbResV);
        double r_ta_mRes = PearsonCorr(taV, mResV);

        // Joint: can residuals predict tick beyond core?
        double r2_tick_core = r_tick_core * r_tick_core;
        double r2_tick_joint = MultivariateR2(tickV, coreV, fbResV);
        double r2_tick_joint_m = MultivariateR2(tickV, coreV, mResV);
        double deltaFbRes = r2_tick_joint - r2_tick_core;
        double deltaMRes = r2_tick_joint_m - r2_tick_core;

        sb.AppendLine($"  Tick correlation structure:");
        sb.AppendLine($"{"Target",-20} {"r(Core)",10} {"r(fb_res)",10} {"r(m_res)",10}");
        sb.AppendLine(new string('-', 52));
        sb.AppendLine($"{"Tick (raw)",-20} {r_tick_core,10:F4} {r_tick_fbRes,10:F4} {r_tick_mRes,10:F4}");
        sb.AppendLine($"{"Tick (log)",-20} {r_ltick_core,10:F4} {r_ltick_fbRes,10:F4} {r_ltick_mRes,10:F4}");
        sb.AppendLine($"{"TickAnomaly",-20} {r_ta_core,10:F4} {r_ta_fbRes,10:F4} {r_ta_mRes,10:F4}");
        sb.AppendLine("");
        sb.AppendLine($"  Incremental R² from residuals:");
        sb.AppendLine($"    Core only:          {r2_tick_core:F4}");
        sb.AppendLine($"    Core + fb_res:      {r2_tick_joint:F4}  (Δ = {deltaFbRes:F4})");
        sb.AppendLine($"    Core + m_res:       {r2_tick_joint_m:F4}  (Δ = {deltaMRes:F4})");
        sb.AppendLine("");

        bool residualsSilent = deltaFbRes < 0.03 && deltaMRes < 0.03;
        sb.AppendLine(residualsSilent
            ? "  → Tick couples to SHARED CORE only. Residuals are silent."
            : "  → Residuals carry tick-relevant information beyond the shared core.");
        sb.AppendLine(residualsSilent
            ? "  VERDICT: fb and |m| are REDUNDANT for tick prediction."
            : "  VERDICT: fb and |m| carry DISTINCT tick-relevant information.");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(residualsSilent, $"SCD_02: ΔR²(fb_res)={deltaFbRes:F4}, ΔR²(m_res)={deltaMRes:F4}. Redundant if both < 0.03.");
    }

    // ====================================================================
    // SCD_03: BOUNDARY EFFECTS — Does boundary concentrate C or residuals?
    //
    // If fb and |m| are redundant, boundary should concentrate C
    // (the shared signal), not residuals (which are noise).
    //
    // If boundary concentrates residuals, they carry boundary-relevant structure.
    //
    // Null:  B/I ratio: C_ratio ≫ residual_ratios
    // Alt:   Boundary also concentrates residuals
    // ====================================================================
    [Fact]
    public void SCD_03_BoundaryEffects_ConcentratesCoreOrResiduals()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== SCD_03: Boundary Effects — Core vs Residual Concentration ===");
        sb.AppendLine(new string('=', 96));

        var decomp = Decompose();
        if (decomp.Count < 100) { sb.AppendLine($"Insufficient: {decomp.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        var bdry = decomp.Where(c => c.IsBoundary).ToList();
        var interior = decomp.Where(c => c.ZoneDist >= 2).ToList();

        if (bdry.Count < 15 || interior.Count < 15)
        { sb.AppendLine("Insufficient boundary/interior cells."); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        // Absolute values (sign matters less for concentration)
        double bCore = bdry.Average(c => Math.Abs(c.Core));
        double iCore = interior.Average(c => Math.Abs(c.Core));
        double coreRatio = bCore / Math.Max(1e-15, iCore);

        double bFbR = bdry.Average(c => Math.Abs(c.FbResidual));
        double iFbR = interior.Average(c => Math.Abs(c.FbResidual));
        double fbResRatio = bFbR / Math.Max(1e-15, iFbR);

        double bMR = bdry.Average(c => Math.Abs(c.MResidual));
        double iMR = interior.Average(c => Math.Abs(c.MResidual));
        double mResRatio = bMR / Math.Max(1e-15, iMR);

        // Also check: does residual structure correlate with boundary distance?
        double[] bdV = decomp.Select(c => (double)c.ZoneDist).ToArray();
        double r_bd_core = PearsonCorr(bdV, decomp.Select(c => Math.Abs(c.Core)).ToArray());
        double r_bd_fbRes = PearsonCorr(bdV, decomp.Select(c => Math.Abs(c.FbResidual)).ToArray());
        double r_bd_mRes = PearsonCorr(bdV, decomp.Select(c => Math.Abs(c.MResidual)).ToArray());

        sb.AppendLine($"  Boundary/Interior concentration ratios:");
        sb.AppendLine($"    SharedCore:  {coreRatio:F2}x  (r with bd_dist = {r_bd_core:F4})");
        sb.AppendLine($"    fb_residual: {fbResRatio:F2}x  (r with bd_dist = {r_bd_fbRes:F4})");
        sb.AppendLine($"    m_residual:  {mResRatio:F2}x  (r with bd_dist = {r_bd_mRes:F4})");
        sb.AppendLine("");

        // Check per-zone structure
        sb.AppendLine($"  Per-zone |Core| and |residual| means:");
        sb.AppendLine($"{"Zone",-12} {"|Core|",10} {"|fb_res|",10} {"|m_res|",10} {"N",5}");
        sb.AppendLine(new string('-', 50));
        for (int z = 0; z <= 3; z++)
        {
            var zCells = decomp.Where(c => c.ZoneDist == z).ToList();
            if (zCells.Count < 10) continue;
            double zc = zCells.Average(c => Math.Abs(c.Core));
            double zf = zCells.Average(c => Math.Abs(c.FbResidual));
            double zm = zCells.Average(c => Math.Abs(c.MResidual));
            string zn = z == 0 ? "Boundary" : z == 1 ? "Near-1" : z == 2 ? "Near-2" : "Interior";
            sb.AppendLine($"{zn,-12} {zc,10:F4} {zf,10:F4} {zm,10:F4} {zCells.Count,5}");
        }
        sb.AppendLine("");

        bool coreDominates = coreRatio > Math.Max(fbResRatio, mResRatio) * 1.3;
        bool residualsNoisy = fbResRatio < 1.15 && mResRatio < 1.15;
        sb.AppendLine(coreDominates
            ? "  → Boundary concentrates the SHARED CORE, not residuals."
            : residualsNoisy
            ? "  → Neither core nor residuals are strongly boundary-concentrated."
            : "  → Residuals show boundary structure — they carry independent information.");
        sb.AppendLine(residualsNoisy && coreDominates
            ? "  VERDICT: Boundary acts on the shared signal. Residuals are boundary-neutral."
            : !residualsNoisy
            ? "  VERDICT: Residuals carry boundary-relevant structure. fb and |m| are distinct."
            : "  VERDICT: AMBIGUOUS — neither dominates.");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(residualsNoisy, $"SCD_03: B/I ratios: core={coreRatio:F2}x, fb_res={fbResRatio:F2}x, m_res={mResRatio:F2}x. Redundant if residuals have B/I < 1.15.");
    }

    // ====================================================================
    // SCD_04: GRADIENT EXPLANATION — Is ∇fb explained by C?
    //
    // If fb ≈ func(|m|), then ∇fb should be determined by ∇C
    // (the gradient of the shared core).
    //
    // Null:  ∇fb ≈ β · ∇C  (R² > 0.80)
    // Alt:   ∇fb has structure beyond ∇C
    //
    // Same test for ∇|m|.
    // ====================================================================
    [Fact]
    public void SCD_04_GradientExplanation_IsGradFbExplainedByCore()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== SCD_04: Gradient Explanation — Is ∇fb explained by C? ===");
        sb.AppendLine(new string('=', 96));

        // Need grid-structured data to compute gradients of C
        var gridData = CollectGridDecomposition();
        if (gridData.Count < 100) { sb.AppendLine("Insufficient grid data."); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        // ∇fb vs ∇C, ∇|m| vs ∇C
        double[] gfV = gridData.Select(c => Math.Log10(Math.Max(1e-15, c.GradFb))).ToArray();
        double[] gmV = gridData.Select(c => Math.Log10(Math.Max(1e-15, c.GradM))).ToArray();
        double[] gcV = gridData.Select(c => c.GradCore != 0 ? Math.Log10(Math.Max(1e-15, Math.Abs(c.GradCore))) * Math.Sign(c.GradCore) : 0).ToArray();

        // Clean up: use absolute log for magnitude comparison
        double[] lGf = gridData.Select(c => Math.Log10(Math.Max(1e-15, c.GradFb))).ToArray();
        double[] lGm = gridData.Select(c => Math.Log10(Math.Max(1e-15, c.GradM))).ToArray();
        double[] lGc = gridData.Select(c => Math.Log10(Math.Max(1e-15, Math.Abs(c.GradCore)))).ToArray();

        double r2_gf_gc = PearsonCorr(lGf, lGc); r2_gf_gc *= r2_gf_gc;
        double r2_gm_gc = PearsonCorr(lGm, lGc); r2_gm_gc *= r2_gm_gc;
        double r2_gf_gm = PearsonCorr(lGf, lGm); r2_gf_gm *= r2_gf_gm;

        // Does core gradient explain fb gradient BETTER than |m| gradient does?
        double r2_gf_gc_m = MultivariateR2(lGf, lGc, lGm);

        sb.AppendLine($"  ∇fb explained by ∇Core:      R² = {r2_gf_gc:F4}");
        sb.AppendLine($"  ∇|m| explained by ∇Core:      R² = {r2_gm_gc:F4}");
        sb.AppendLine($"  ∇fb explained by ∇|m|:        R² = {r2_gf_gm:F4}");
        sb.AppendLine($"  ∇fb explained by ∇Core+∇|m|:  R² = {r2_gf_gc_m:F4}");
        sb.AppendLine("");

        bool gfExplainedByCore = r2_gf_gc > 0.70;
        bool gmExplainedByCore = r2_gm_gc > 0.70;

        sb.AppendLine($"  ∇fb: {(gfExplainedByCore ? "EXPLAINED by core" : "NOT fully explained by core")}");
        sb.AppendLine($"  ∇|m|: {(gmExplainedByCore ? "EXPLAINED by core" : "NOT fully explained by core")}");
        sb.AppendLine("");

        if (gfExplainedByCore && gmExplainedByCore)
        {
            sb.AppendLine("  → Both gradients are functions of the shared core gradient.");
            sb.AppendLine("  → ∇fb and ∇|m| carry no independent gradient information.");
            sb.AppendLine("  VERDICT: fb and |m| are REDUNDANT — single gradient field.");
        }
        else if (gfExplainedByCore && !gmExplainedByCore)
        {
            sb.AppendLine("  → ∇fb follows core; ∇|m| has independent gradient structure.");
            sb.AppendLine("  VERDICT: ASYMMETRIC — |m| carries unique gradient information.");
        }
        else if (!gfExplainedByCore && gmExplainedByCore)
        {
            sb.AppendLine("  → ∇|m| follows core; ∇fb has independent gradient structure.");
            sb.AppendLine("  VERDICT: ASYMMETRIC — fb carries unique gradient information.");
        }
        else
        {
            sb.AppendLine("  → Neither gradient is fully explained by the shared core.");
            sb.AppendLine("  VERDICT: fb and |m| carry INDEPENDENT gradient structure.");
        }
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(gfExplainedByCore && gmExplainedByCore,
            $"SCD_04: R²(∇fb|∇Core)={r2_gf_gc:F4}, R²(∇|m||∇Core)={r2_gm_gc:F4}. Redundant if both > 0.70.");
    }

    // ====================================================================
    // SCD_05: RESIDUAL G-T-F INTERACTION
    //
    // Do residuals participate in the Geometry-Tick-Feedback interaction?
    //
    // If residuals are noise, they should show NO correlation with:
    //   - tick anomaly
    //   - boundary proximity
    //   - grad_fb or grad_m
    //
    // Null:  residuals are uncorrelated with all G-T-F observables
    // Alt:   residuals participate in at least one G-T-F coupling
    // ====================================================================
    [Fact]
    public void SCD_05_ResidualGTFInteraction_DoResidualsParticipate()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== SCD_05: Residual G-T-F Interaction ===");
        sb.AppendLine("=== Do residuals participate in Geometry-Tick-Feedback coupling? ===");
        sb.AppendLine(new string('=', 96));

        var gridData = CollectGridDecomposition();
        if (gridData.Count < 100) { sb.AppendLine("Insufficient grid data."); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        double[] fbResV = gridData.Select(c => c.FbResidual).ToArray();
        double[] mResV = gridData.Select(c => c.MResidual).ToArray();
        double[] taV = gridData.Select(c => c.TickAnomaly).ToArray();
        double[] bdV = gridData.Select(c => (double)c.ZoneDist).ToArray();
        double[] bBin = gridData.Select(c => c.IsBoundary ? 1.0 : 0.0).ToArray();
        double[] gfV = gridData.Select(c => Math.Log10(Math.Max(1e-15, c.GradFb))).ToArray();
        double[] gmV = gridData.Select(c => Math.Log10(Math.Max(1e-15, c.GradM))).ToArray();
        double[] gtV = gridData.Select(c => Math.Log10(Math.Max(1e-15, c.GradTick))).ToArray();

        sb.AppendLine($"{"Coupling",-24} {"r(fb_res)",10} {"r(m_res)",10} {"Signal?",8}");
        sb.AppendLine(new string('-', 56));

        var tests = new (string name, double[] target)[]
        {
            ("Tick anomaly",        taV),
            ("Boundary distance",   bdV),
            ("Boundary binary",     bBin),
            ("∇fb",                 gfV),
            ("∇|m|",                gmV),
            ("∇Tick",               gtV),
        };

        int fbSigCount = 0, mSigCount = 0;
        foreach (var (tname, tval) in tests)
        {
            double rFb = PearsonCorr(fbResV, tval);
            double rM = PearsonCorr(mResV, tval);
            bool fbSig = Math.Abs(rFb) > 0.15;
            bool mSig = Math.Abs(rM) > 0.15;
            if (fbSig) fbSigCount++;
            if (mSig) mSigCount++;
            sb.AppendLine($"{tname,-24} {rFb,10:F4} {rM,10:F4} {(fbSig || mSig ? "YES" : "no"),8}");
        }
        sb.AppendLine("");

        // Cross-residual correlation (should be near 0 if both are noise)
        double r_fbRes_mRes = PearsonCorr(fbResV, mResV);
        sb.AppendLine($"  Corr(fb_res, m_res) = {r_fbRes_mRes:F4}");
        sb.AppendLine("");

        bool residualsSilent = fbSigCount == 0 && mSigCount == 0;
        sb.AppendLine(residualsSilent
            ? $"  → Residuals carry NO G-T-F interaction signal ({fbSigCount}+{mSigCount}=0/{tests.Length * 2})"
            : $"  → Residuals participate in {fbSigCount + mSigCount}/{tests.Length * 2} G-T-F couplings");
        sb.AppendLine(residualsSilent
            ? "  VERDICT: SharedCore captures ALL G-T-F interaction. fb and |m| are redundant."
            : "  VERDICT: Residuals carry coupling information. fb and |m| are distinct.");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(residualsSilent,
            $"SCD_05: fb_res signals={fbSigCount}, m_res signals={mSigCount}. Redundant if both=0.");
    }

    // ====================================================================
    // SCD_06: REDUCTION CLASSIFICATION
    //
    // Can V33 hierarchy be reduced: fb → SharedCore, |m| → SharedCore?
    //
    // Classification based on aggregate evidence from SCD_01-05.
    // ====================================================================
    [Fact]
    public void SCD_06_ReductionClassification_FbAndMToCore()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== SCD_06: Reduction Classification ===");
        sb.AppendLine("=== Can fb → SharedCore and |m| → SharedCore? ===");
        sb.AppendLine(new string('=', 96));

        var decomp = Decompose();
        if (decomp.Count < 100) { sb.AppendLine($"Insufficient: {decomp.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        double[] fbV = decomp.Select(c => c.Fb).ToArray();
        double[] mV = decomp.Select(c => c.AbsM).ToArray();
        double[] coreV = decomp.Select(c => c.Core).ToArray();
        double[] fbResV = decomp.Select(c => c.FbResidual).ToArray();
        double[] mResV = decomp.Select(c => c.MResidual).ToArray();

        // Evidence 1: Variance explained
        double r2_fb = PearsonCorr(fbV, coreV); r2_fb *= r2_fb;
        double r2_m = PearsonCorr(mV, coreV); r2_m *= r2_m;
        int ev1 = (r2_fb > 0.85 && r2_m > 0.85) ? 2 : (r2_fb > 0.60 && r2_m > 0.60) ? 1 : 0;

        // Evidence 2: Residual correlation (should be near 0)
        double r_res = PearsonCorr(fbResV, mResV);
        int ev2 = Math.Abs(r_res) < 0.15 ? 2 : Math.Abs(r_res) < 0.30 ? 1 : 0;

        // Evidence 3: Residual predictive power (SCD_02 proxy)
        double[] tickV = decomp.Select(c => c.Tick).ToArray();
        double r2_tick_core = PearsonCorr(tickV, coreV); r2_tick_core *= r2_tick_core;
        double r2_tick_joint = MultivariateR2(tickV, coreV, fbResV);
        double deltaTick = r2_tick_joint - r2_tick_core;
        int ev3 = deltaTick < 0.03 ? 2 : deltaTick < 0.08 ? 1 : 0;

        // Evidence 4: Boundary concentration (SCD_03 proxy)
        var bdry = decomp.Where(c => c.IsBoundary).ToList();
        var interior = decomp.Where(c => c.ZoneDist >= 2).ToList();
        double coreRatio = bdry.Average(c => Math.Abs(c.Core)) / Math.Max(1e-15, interior.Average(c => Math.Abs(c.Core)));
        double fbResRatio = bdry.Average(c => Math.Abs(c.FbResidual)) / Math.Max(1e-15, interior.Average(c => Math.Abs(c.FbResidual)));
        int ev4 = (coreRatio > fbResRatio * 1.5) ? 2 : (coreRatio > fbResRatio) ? 1 : 0;

        int totalScore = ev1 + ev2 + ev3 + ev4;

        string classification = totalScore >= 7 ? "SUPPORTED — fb and |m| reduce to SharedCore"
            : totalScore >= 4 ? "CONDITIONAL — partial reduction, weak residual signal"
            : totalScore >= 2 ? "HYPOTHESIS — significant residual structure"
            : "FAIL — fb and |m| are genuinely distinct";

        sb.AppendLine($"  Evidence summary:");
        sb.AppendLine($"    E1: Variance explained   R²(fb)={r2_fb:F4}  R²(|m|)={r2_m:F4}  → score {ev1}/2");
        sb.AppendLine($"    E2: Residual correlation r={r_res:F4}  → score {ev2}/2");
        sb.AppendLine($"    E3: Residual tick info   ΔR²={deltaTick:F4}  → score {ev3}/2");
        sb.AppendLine($"    E4: Boundary preference   Core={coreRatio:F2}x vs fb_res={fbResRatio:F2}x  → score {ev4}/2");
        sb.AppendLine($"    Total: {totalScore}/8");
        sb.AppendLine("");
        sb.AppendLine($"  CLASSIFICATION: {classification}");
        sb.AppendLine("");
        sb.AppendLine(totalScore >= 7
            ? "  → V33 hierarchy simplifies: SharedCore replaces {fb, |m|} as single layer."
            : totalScore >= 4
            ? "  → SharedCore captures most signal; weak independent residual remains."
            : "  → fb and |m| must remain as separate layers in the V33 hierarchy.");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(totalScore >= 7, $"SCD_06: Reduction score = {totalScore}/8. SUPPORTED if ≥7.");
    }

    // ====================================================================
    // SCD_07: V3.4 COMPATIBILITY — SharedCore and Bridge Band
    //
    // If fb and |m| reduce to SharedCore, V3.4 recovery simplifies:
    //   SharedCore → effective Ω → bridge band
    //
    // Test: does SharedCore correlate with V3.4 proxy parameters?
    //
    // Note: V3.4 parameters (γ=0.85, φ=0.17) are in CML space.
    // V33 parameters are in CCI kernel space.
    // Direct mapping requires CML-CCI bridge (not available here).
    // This test identifies the recovery path and failure modes.
    // ====================================================================
    [Fact]
    public void SCD_07_V34Compatibility_SharedCoreRecovery()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== SCD_07: V3.4 Compatibility — SharedCore Recovery ===");
        sb.AppendLine(new string('=', 96));

        var decomp = Decompose();
        if (decomp.Count < 100) { sb.AppendLine($"Insufficient: {decomp.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        // V3.4 chain: ω_i → CML sync → Ω* = ⟨ω_i⟩ + α_CLOCK·φ → γ* = 1/Ω*
        //
        // SharedCore measures: the common signal between fb and |m|
        //   = the shared α-sensitivity of VarTerms/VarI1 relationship
        //
        // V3.4 ω_i = 1.0 (normalization) — the tick baseline
        //
        // Potential mapping:
        //   SharedCore (V33) ↔ effective ω_i (V3.4)
        //   If C(β,γ) can be mapped to ω_i_eff, then:
        //     C → ω_i_eff → Ω* → γ* → bridge band
        //
        // Recovery test (proxy): does SharedCore vary with kernel parameters
        // in a way that could produce Ω* variation?

        double[] coreV = decomp.Select(c => c.Core).ToArray();
        double[] tickV = decomp.Select(c => c.Tick).ToArray();

        double r_core_tick = PearsonCorr(coreV, tickV);

        sb.AppendLine("  V3.4 bridge-band recovery path with SharedCore:");
        sb.AppendLine("");
        sb.AppendLine("    V33 CCI space              V3.4 CML space");
        sb.AppendLine("    ─────────────               ─────────────");
        sb.AppendLine("    (β,γ_kernel)  ──→  Core   ──?──→  ω_i_eff");
        sb.AppendLine("                                          ↓");
        sb.AppendLine("    tick (α-sense)                  CML sync → Ω*");
        sb.AppendLine("                                          ↓");
        sb.AppendLine("    sign = sgn(dTdp)                γ* = 1/Ω*");
        sb.AppendLine("                                          ↓");
        sb.AppendLine("    boundary                         bridge band [1.16,1.19]");
        sb.AppendLine("");
        sb.AppendLine($"  SharedCore ↔ tick correlation: r = {r_core_tick:F4}  R² = {r_core_tick * r_core_tick:F4}");
        sb.AppendLine("");

        if (Math.Abs(r_core_tick) > 0.60)
        {
            sb.AppendLine("  → Core couples to tick — feasible mapping to ω_i exists");
            sb.AppendLine("  → Recovery path: C(β,γ) → effective_ω_i → Ω*(C) → bridge band");
            sb.AppendLine("  → Missing link: explicit CCI → CML parameter correspondence");
        }
        else
        {
            sb.AppendLine("  → Core weakly couples to tick — mapping to ω_i is unclear");
            sb.AppendLine("  → Recovery path: REQUIRES additional bridge between frameworks");
        }
        sb.AppendLine("");

        sb.AppendLine("  Recovery failure modes:");
        sb.AppendLine("    1. If C(β,γ) produces ω_i_eff outside sync-stable range");
        sb.AppendLine("       → synchronization fails → no bridge band");
        sb.AppendLine("    2. If the Core→tick relationship is architecture-dependent");
        sb.AppendLine("       → different architectures would predict different Ω*");
        sb.AppendLine("    3. If fb_res or m_res contain the V3.4-critical information");
        sb.AppendLine("       → SharedCore alone is insufficient for recovery");
        sb.AppendLine("");
        sb.AppendLine("  Classification: CONDITIONAL — path exists, CML bridge needed");
        sb.AppendLine("  Gate: OPEN — requires explicit V33↔V3.4 integration test");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ====================================================================
    // SCD_08: FINAL VERDICT — One signal or two?
    //
    // Aggregates all SCD evidence. Decides:
    //   ONE SIGNAL (fb and |m| measure the same thing)
    //   or
    //   TWO SIGNALS (fb and |m| are distinct observables)
    // ====================================================================
    [Fact]
    public void SCD_08_FinalVerdict_OneSignalOrTwo()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== SCD_08: Final Verdict — One Signal or Two? ===");
        sb.AppendLine(new string('=', 96));

        var decomp = Decompose();
        if (decomp.Count < 100) { sb.AppendLine($"Insufficient: {decomp.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        double[] fbV = decomp.Select(c => c.Fb).ToArray();
        double[] mV = decomp.Select(c => c.AbsM).ToArray();
        double[] coreV = decomp.Select(c => c.Core).ToArray();
        double[] fbResV = decomp.Select(c => c.FbResidual).ToArray();
        double[] mResV = decomp.Select(c => c.MResidual).ToArray();
        double[] tickV = decomp.Select(c => c.Tick).ToArray();

        // Criterion 1: Core explains > 85% of variance in BOTH
        double r2_fb = PearsonCorr(fbV, coreV); r2_fb *= r2_fb;
        double r2_m = PearsonCorr(mV, coreV); r2_m *= r2_m;
        bool c1 = r2_fb > 0.85 && r2_m > 0.85;

        // Criterion 2: Residuals are uncorrelated (ρ < 0.15)
        double r_res = PearsonCorr(fbResV, mResV);
        bool c2 = Math.Abs(r_res) < 0.15;

        // Criterion 3: Residuals add < 3% to tick prediction beyond core
        double r2_tick_core = PearsonCorr(tickV, coreV); r2_tick_core *= r2_tick_core;
        double r2_tick_joint = MultivariateR2(tickV, coreV, fbResV);
        double deltaTick = r2_tick_joint - r2_tick_core;
        bool c3 = deltaTick < 0.03;

        // Criterion 4: Residual variance < 20% of core variance
        double varCore = coreV.Select(v => v * v).Average();
        double varFbRes = fbResV.Select(v => v * v).Average();
        double varMRes = mResV.Select(v => v * v).Average();
        bool c4 = varFbRes / Math.Max(1e-15, varCore) < 0.20 && varMRes / Math.Max(1e-15, varCore) < 0.20;

        // Criterion 5: Residuals show no boundary concentration
        var bdry = decomp.Where(c => c.IsBoundary).ToList();
        var interior = decomp.Where(c => c.ZoneDist >= 2).ToList();
        double fbResBdry = bdry.Average(c => Math.Abs(c.FbResidual));
        double fbResInt = interior.Average(c => Math.Abs(c.FbResidual));
        bool c5 = fbResBdry / Math.Max(1e-15, fbResInt) < 1.15;

        int criteriaMet = (c1 ? 1 : 0) + (c2 ? 1 : 0) + (c3 ? 1 : 0) + (c4 ? 1 : 0) + (c5 ? 1 : 0);

        string c1Label = c1 ? "PASS" : $"FAIL ({Math.Min(r2_fb, r2_m):F3})";
        string c2Label = c2 ? $"PASS ({Math.Abs(r_res):F3})" : $"FAIL ({Math.Abs(r_res):F3})";
        string c3Label = c3 ? $"PASS ({deltaTick:F4})" : $"FAIL ({deltaTick:F4})";
        string c4Label = c4 ? "PASS" : "FAIL";
        string c5Label = c5 ? "PASS" : "FAIL";

        sb.AppendLine($"{"Criterion",-52} {"Status",8}");
        sb.AppendLine(new string('-', 62));
        sb.AppendLine($"{"C1: Core explains >85% of fb AND |m| variance",-52} {c1Label,8}");
        sb.AppendLine($"{"C2: Residuals uncorrelated (|\u03c1| < 0.15)",-52} {c2Label,8}");
        sb.AppendLine($"{"C3: Residuals add <3% to tick prediction",-52} {c3Label,8}");
        sb.AppendLine($"{"C4: Residual variance <20% of core variance",-52} {c4Label,8}");
        sb.AppendLine($"{"C5: No boundary concentration of residuals",-52} {c5Label,8}");
        sb.AppendLine($"{"TOTAL",-52} {criteriaMet,8}/5");
        sb.AppendLine("");

        string verdict = criteriaMet >= 5 ? "ONE SIGNAL — fb and |m| measure the same underlying quantity"
            : criteriaMet >= 3 ? "MOSTLY ONE SIGNAL — weak independent residual, SharedCore captures nearly everything"
            : criteriaMet >= 2 ? "PARTIALLY DISTINCT — significant independent information in one or both residuals"
            : "TWO SIGNALS — fb and |m| are genuinely distinct observables";

        sb.AppendLine($"  FINAL VERDICT: {verdict}");
        sb.AppendLine("");
        sb.AppendLine(criteriaMet >= 5
            ? "  → V33 should replace {fb, |m|} with SharedCore as single layer."
            : criteriaMet >= 3
            ? "  → SharedCore is the primary layer; residuals may carry weak secondary signals."
            : "  → fb and |m| must remain as separate layers with SharedCore as derived combination.");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(criteriaMet >= 5, $"SCD_08: {criteriaMet}/5 criteria for single-signal. One signal if all 5 pass.");
    }

    // ====================================================================
    // DATA COLLECTION AND DECOMPOSITION PIPELINE
    // ====================================================================

    /// <summary>
    /// Collect raw grid data (fb, |m|, tick, sign, gradients, boundary).
    /// </summary>
    private List<(string Arch, double AbsM, double Fb, double Tick, int Sign,
        double GradM, double GradFb, double GradTick, double TickAnomaly,
        int ZoneDist, bool IsBoundary)> CollectRawData()
    {
        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);
        var allCells = new ConcurrentBag<(string, double, double, double, int, double, double, double, double, int, bool)>();

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

                    allCells.Add((arch, gM[bi, gi], gFb[bi, gi], gTick[bi, gi], gS[bi, gi],
                        gradM, gradFb, gradTick, tAnom, d, isBdry));
                }
        }
        return allCells.ToList();
    }

    /// <summary>
    /// Full decomposition: raw data → standardized → PCA → core + residuals.
    /// Returns DecomposedCell list.
    /// </summary>
    private List<DecomposedCell> Decompose()
    {
        var raw = CollectRawData();
        int n = raw.Count;
        if (n < 50) return new List<DecomposedCell>();

        double[] fbV = raw.Select(c => c.Fb).ToArray();
        double[] mV = raw.Select(c => c.AbsM).ToArray();

        // Standardize
        double fbMean = fbV.Average(), fbStd = Math.Sqrt(fbV.Select(v => (v - fbMean) * (v - fbMean)).Average());
        double mMean = mV.Average(), mStd = Math.Sqrt(mV.Select(v => (v - mMean) * (v - mMean)).Average());
        double[] zFb = fbV.Select(v => fbStd > 1e-15 ? (v - fbMean) / fbStd : 0).ToArray();
        double[] zM = mV.Select(v => mStd > 1e-15 ? (v - mMean) / mStd : 0).ToArray();

        // Covariance
        double sFM = 0, sFF = 0, sMM = 0;
        for (int i = 0; i < n; i++) { sFF += zFb[i] * zFb[i]; sMM += zM[i] * zM[i]; sFM += zFb[i] * zM[i]; }
        sFF /= (n - 1); sMM /= (n - 1); sFM /= (n - 1);

        // PCA
        double trace = sFF + sMM;
        double disc = Math.Sqrt(Math.Max(0, trace * trace - 4 * (sFF * sMM - sFM * sFM)));
        double lambda1 = (trace + disc) / 2.0;
        double evFb = sFM, evM = lambda1 - sFF;
        double evNorm = Math.Sqrt(evFb * evFb + evM * evM);
        if (evNorm < 1e-15) { evFb = 1; evM = 0; evNorm = 1; }
        double wFb = evFb / evNorm, wM = evM / evNorm;

        // Core
        double[] coreZ = zFb.Zip(zM, (f, m) => wFb * f + wM * m).ToArray();

        // Regression of each variable onto core
        double coreMeanZ = coreZ.Average();
        double cVar = 0, cCovF = 0, cCovM = 0;
        for (int i = 0; i < n; i++) { double dc = coreZ[i] - coreMeanZ; cVar += dc * dc; cCovF += dc * zFb[i]; cCovM += dc * zM[i]; }
        cVar /= (n - 1); cCovF /= (n - 1); cCovM /= (n - 1);
        double betaF = cVar > 1e-15 ? cCovF / cVar : 0;
        double betaM = cVar > 1e-15 ? cCovM / cVar : 0;

        // Residuals in z-space
        double[] zFbRes = coreZ.Zip(zFb, (c, f) => f - betaF * c).ToArray();
        double[] zMRes = coreZ.Zip(zM, (c, m) => m - betaM * c).ToArray();

        // Build result
        var result = new List<DecomposedCell>(n);
        for (int i = 0; i < n; i++)
        {
            var r = raw[i];
            result.Add(new DecomposedCell(
                r.Arch, r.AbsM, r.Fb, r.Tick, r.Sign,
                r.GradM, r.GradFb, r.GradTick, r.TickAnomaly,
                r.ZoneDist, r.IsBoundary,
                coreZ[i], zFbRes[i], zMRes[i]));
        }
        return result;
    }

    /// <summary>
    /// Grid decomposition: raw → decompose → compute ∇Core per cell.
    /// Requires grid-structured data (nG × nG).
    /// </summary>
    private List<GridDecompCell> CollectGridDecomposition()
    {
        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);
        var allCells = new ConcurrentBag<GridDecompCell>();

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

            // Compute shared core on the full grid
            var flatFb = new List<double>(); var flatM = new List<double>();
            for (int bi = 0; bi < nG; bi++) for (int gi = 0; gi < nG; gi++) { flatFb.Add(gFb[bi, gi]); flatM.Add(gM[bi, gi]); }
            double[] fArr = flatFb.ToArray(), mArr = flatM.ToArray();
            int n = fArr.Length;
            double fbMean = fArr.Average(), fbStd = Math.Sqrt(fArr.Select(v => (v - fbMean) * (v - fbMean)).Average());
            double mMean = mArr.Average(), mStd = Math.Sqrt(mArr.Select(v => (v - mMean) * (v - mMean)).Average());
            double[] zFb = fArr.Select(v => fbStd > 1e-15 ? (v - fbMean) / fbStd : 0).ToArray();
            double[] zM = mArr.Select(v => mStd > 1e-15 ? (v - mMean) / mStd : 0).ToArray();

            double sFM = 0, sFF = 0, sMM = 0;
            for (int i = 0; i < n; i++) { sFF += zFb[i] * zFb[i]; sMM += zM[i] * zM[i]; sFM += zFb[i] * zM[i]; }
            sFF /= (n - 1); sMM /= (n - 1); sFM /= (n - 1);

            double trace = sFF + sMM;
            double disc = Math.Sqrt(Math.Max(0, trace * trace - 4 * (sFF * sMM - sFM * sFM)));
            double lambda1 = (trace + disc) / 2.0;
            double evFb = sFM, evM = lambda1 - sFF;
            double evNorm = Math.Sqrt(evFb * evFb + evM * evM);
            double wFb = evNorm > 1e-15 ? evFb / evNorm : 1;
            double wM = evNorm > 1e-15 ? evM / evNorm : 0;

            // Core and residuals on grid
            var coreGrid = new double[nG, nG];
            var fbResGrid = new double[nG, nG];
            var mResGrid = new double[nG, nG];
            {
                double[] coreZ = zFb.Zip(zM, (f, m) => wFb * f + wM * m).ToArray();
                double coreMeanZ = coreZ.Average();
                double cVar = 0, cCovF = 0, cCovM = 0;
                for (int i = 0; i < n; i++) { double dc = coreZ[i] - coreMeanZ; cVar += dc * dc; cCovF += dc * zFb[i]; cCovM += dc * zM[i]; }
                cVar /= (n - 1); cCovF /= (n - 1); cCovM /= (n - 1);
                double betaF = cVar > 1e-15 ? cCovF / cVar : 0;
                double betaM = cVar > 1e-15 ? cCovM / cVar : 0;
                int idx = 0;
                for (int bi = 0; bi < nG; bi++)
                    for (int gi = 0; gi < nG; gi++)
                    {
                        coreGrid[bi, gi] = coreZ[idx];
                        fbResGrid[bi, gi] = zFb[idx] - betaF * coreZ[idx];
                        mResGrid[bi, gi] = zM[idx] - betaM * coreZ[idx];
                        idx++;
                    }
            }

            // BFS distance
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

                    // ∇Core
                    double dCdB = (coreGrid[bi + 1, gi] - coreGrid[bi - 1, gi]) / (2 * db);
                    double dCdG = (coreGrid[bi, gi + 1] - coreGrid[bi, gi - 1]) / (2 * dg);
                    double gradCore = Math.Sqrt(dCdB * dCdB + dCdG * dCdG);

                    int d = dist[bi, gi] == int.MaxValue ? 4 : Math.Min(3, dist[bi, gi]);
                    bool isBdry = d == 0;

                    var nT = new List<double>();
                    if (bi > 0) nT.Add(gTick[bi - 1, gi]); if (bi + 1 < nG) nT.Add(gTick[bi + 1, gi]);
                    if (gi > 0) nT.Add(gTick[bi, gi - 1]); if (gi + 1 < nG) nT.Add(gTick[bi, gi + 1]);
                    double tLocMean = nT.Count > 0 ? nT.Average() : gTick[bi, gi];
                    double tAnom = Math.Abs(gTick[bi, gi] - tLocMean) / Math.Max(1e-15, tLocMean);

                    allCells.Add(new GridDecompCell(
                        gradM, gradFb, gradTick, tAnom, d, isBdry,
                        coreGrid[bi, gi], fbResGrid[bi, gi], mResGrid[bi, gi], gradCore));
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

    private record GridDecompCell(double GradM, double GradFb, double GradTick, double TickAnomaly,
        int ZoneDist, bool IsBoundary, double Core, double FbResidual, double MResidual, double GradCore);
}
