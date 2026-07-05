using Xunit;
using Xunit.Abstractions;
using TRM.Tests.V4.V4_TestHelpers;

namespace TRM.Tests.V4;

/// <summary>
/// TRM V4 — B1 Coupling Modulation Asymptotic Scaling Tests
///
/// Tests whether any coupling perturbation model δK(r) combined with
/// any density extraction map F produces the Newtonian-gravity limit:
///
///   δρ_eff(r) ~ 1/r   →   a(r) ~ 1/r²
///
/// Evaluates all 16 (δK, F) pairs, classifies each as VALID / PARTIAL / INVALID,
/// and outputs a full matrix report.
///
/// Reference: docsV4/experiments/TRM_V4_MappingTests.md
///            docsV4/experiments/TRM_V4_B1_Results.md
///            docsV4/review/TRM_V4_B1_CriticalReview.md
/// </summary>
[Trait("Category", "V4")]
[Trait("Category", "B1")]
[Trait("Category", "CriticalGate")]
public class B1_CouplingModulation_AsymptoticScaling_Tests
{
    private readonly ITestOutputHelper _output;

    public B1_CouplingModulation_AsymptoticScaling_Tests(ITestOutputHelper output)
    {
        _output = output;
    }

    // ────────────────────────────────────────────────────────────
    // Test parameters  (shared with analytic docs)
    // ────────────────────────────────────────────────────────────

    private const double kTest = 1.0;       // coupling perturbation constant
    private const double MTest = 1.0;       // test mass
    private const double R0Test = 100.0;     // screening length for K3
    private const double RNear = 10.0;       // near-field radius for α fitting
    private const double RFar = 1000.0;      // far-field radius for α fitting
    private const double Dr = 0.001;         // step for numerical gradient

    // ────────────────────────────────────────────────────────────
    // [Fact] K1×F1 must be VALID — the decisive gate
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// B1 Gate — K1 × F1 must produce exact Newtonian asymptotics.
    ///
    /// δK(r) = k·M / r    (3D Laplace Green's function)
    /// δρ ∝ δK            (direct density extraction)
    ///
    /// Expected: α = −1.000, β = −2.000 → VALID.
    ///
    /// This is THE decisive test. If it fails, the mechanism is falsified.
    /// </summary>
    [Fact]
    public void B1CM01_K1xF1_DecisiveGate_NewtonianAsymptotics()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B1 GATE — K1 × F1 NEWTONIAN ASYMPTOTICS");
        _output.WriteLine("  δK(r) = k·M/r   (3D Laplace Green's function)");
        _output.WriteLine("  ρ_eff ∝ δK       (direct density extraction)");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        // --- Compute α from δK ---
        double alphaK = CouplingProfiles.ComputeAsymptoticAlpha(
            r => CouplingProfiles.K1(r, kTest, MTest), RNear, RFar);

        _output.WriteLine($"  δK asymptotic α = {alphaK:F6}  (expected: −1.000)");

        // --- Compute δρ_eff via F1 ---
        double rhoNear = DensityExtractionMaps.F1_Direct(
            CouplingProfiles.K1(RNear, kTest, MTest));
        double rhoFar = DensityExtractionMaps.F1_Direct(
            CouplingProfiles.K1(RFar, kTest, MTest));
        double alphaRho = Math.Log(rhoFar / rhoNear) / Math.Log(RFar / RNear);

        _output.WriteLine($"  δρ_eff asymptotic α = {alphaRho:F6}  (expected: −1.000)");

        // --- Compute β from gradient ---
        double gradNear = DensityExtractionMaps.ComputeGradient(
            r => DensityExtractionMaps.F1_Direct(
                CouplingProfiles.K1(r, kTest, MTest)), RNear, Dr);
        double gradFar = DensityExtractionMaps.ComputeGradient(
            r => DensityExtractionMaps.F1_Direct(
                CouplingProfiles.K1(r, kTest, MTest)), RFar, Dr);
        double beta = Math.Log(Math.Abs(gradFar) / Math.Abs(gradNear)) / Math.Log(RFar / RNear);

        _output.WriteLine($"  a(r) asymptotic β = {beta:F6}  (expected: −2.000)");
        _output.WriteLine("");

        // --- Classify ---
        var result = V4GateClassifier.Evaluate("K1", "F1", alphaRho, beta);
        _output.WriteLine($"  Classification: {result.Classification}");
        _output.WriteLine($"  |α + 1| = {Math.Abs(alphaRho + 1.0):F4}  (threshold: {V4GateClassifier.ValidThreshold})");
        _output.WriteLine($"  |β + 2| = {Math.Abs(beta + 2.0):F4}  (threshold: {V4GateClassifier.ValidThreshold})");

        // --- Assert ---
        Assert.InRange(alphaRho, -1.0 - V4GateClassifier.ValidThreshold, -1.0 + V4GateClassifier.ValidThreshold);
        Assert.InRange(beta, -2.0 - V4GateClassifier.ValidThreshold, -2.0 + V4GateClassifier.ValidThreshold);
        Assert.Equal(V4Classification.Valid, result.Classification);

        _output.WriteLine("");
        _output.WriteLine("  ✅ K1×F1 GATE PASSED — Newtonian asymptotics confirmed.");
    }

    // ────────────────────────────────────────────────────────────
    // [Fact] K2 × F1 must be INVALID
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void B1CM02_K2xF1_MustBeInvalid()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B1 — K2 × F1  (δK ~ 1/r², direct density)");
        _output.WriteLine("══════════════════════════════════════════════");

        double alphaRho = CouplingProfiles.ExpectedAlpha("K2");        // −2.0
        double beta = DensityExtractionMaps.ExpectedBeta(alphaRho);     // −3.0

        _output.WriteLine($"  α = {alphaRho:F4}  (target: −1.0, error: {Math.Abs(alphaRho + 1.0):F4})");
        _output.WriteLine($"  β = {beta:F4}  (target: −2.0, error: {Math.Abs(beta + 2.0):F4})");

        var result = V4GateClassifier.Evaluate("K2", "F1", alphaRho, beta);
        _output.WriteLine($"  Classification: {result.Classification}");

        Assert.Equal(V4Classification.Invalid, result.Classification);
        _output.WriteLine("  ✅ K2×F1 correctly classified as INVALID.");
    }

    // ────────────────────────────────────────────────────────────
    // [Fact] K1 × F2 must be INVALID (gradient adds one power)
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void B1CM03_K1xF2_MustBeInvalid()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B1 — K1 × F2  (δK ~ 1/r, gradient density)");
        _output.WriteLine("══════════════════════════════════════════════");

        double alphaRho = DensityExtractionMaps.ExpectedRhoAlpha("K1", "F2");  // −2.0
        double beta = DensityExtractionMaps.ExpectedBeta(alphaRho);             // −3.0

        _output.WriteLine($"  α = {alphaRho:F4}  (target: −1.0, error: {Math.Abs(alphaRho + 1.0):F4})");
        _output.WriteLine($"  β = {beta:F4}  (target: −2.0, error: {Math.Abs(beta + 2.0):F4})");

        var result = V4GateClassifier.Evaluate("K1", "F2", alphaRho, beta);
        _output.WriteLine($"  Classification: {result.Classification}");

        Assert.Equal(V4Classification.Invalid, result.Classification);
        _output.WriteLine("  ✅ K1×F2 correctly classified as INVALID.");
    }

    // ────────────────────────────────────────────────────────────
    // [Theory] Full 12-pair matrix (K1-K3 × F1-F4)  — excludes K4
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// Full B1 classification matrix: every (K-profile, F-map) pair
    /// is evaluated and checked against its expected classification.
    ///
    /// Expected results (from TRM_V4_B1_Results.md):
    ///   K1×F1, K1×F3, K1×F4 → VALID   (all reduce to same mechanism)
    ///   K1×F2                 → INVALID (gradient adds one power)
    ///   K2×all                → INVALID (δK ~ 1/r² → too steep)
    ///   K3×all                → INVALID (exponential → no power law)
    /// </summary>
    [Theory]
    [InlineData("K1", "F1", V4Classification.Valid,    "δK~1/r, direct")]
    [InlineData("K1", "F2", V4Classification.Invalid,   "δK~1/r, gradient → too steep")]
    [InlineData("K1", "F3", V4Classification.Valid,     "δK~1/r, sync energy ≡ F1")]
    [InlineData("K1", "F4", V4Classification.Valid,     "δK~1/r, action ≡ F1")]
    [InlineData("K2", "F1", V4Classification.Invalid,   "δK~1/r², direct → too steep")]
    [InlineData("K2", "F2", V4Classification.Invalid,   "δK~1/r², gradient → even steeper")]
    [InlineData("K2", "F3", V4Classification.Invalid,   "δK~1/r², sync energy ≡ F1")]
    [InlineData("K2", "F4", V4Classification.Invalid,   "δK~1/r², action ≡ F1")]
    [InlineData("K3", "F1", V4Classification.Invalid,   "δK~exp, direct → no power law")]
    [InlineData("K3", "F2", V4Classification.Invalid,   "δK~exp, gradient → no power law")]
    [InlineData("K3", "F3", V4Classification.Invalid,   "δK~exp, sync energy ≡ F1")]
    [InlineData("K3", "F4", V4Classification.Invalid,   "δK~exp, action ≡ F1")]
    public void B1CM04_FullMatrix_ExpectedClassification(
        string kProfile, string fMap, V4Classification expected, string reason)
    {
        double alphaRho = DensityExtractionMaps.ExpectedRhoAlpha(kProfile, fMap);
        double beta = DensityExtractionMaps.ExpectedBeta(alphaRho);

        // For K3 (exponential), α and β are NaN → treat as INVALID.
        if (double.IsNaN(alphaRho) || double.IsNaN(beta))
        {
            _output.WriteLine($"  {kProfile}×{fMap}: {reason} → expected {expected} (NaN exponents)");
            Assert.Equal(V4Classification.Invalid, expected);
            return;
        }

        var result = V4GateClassifier.Evaluate(kProfile, fMap, alphaRho, beta);

        _output.WriteLine($"  {kProfile}×{fMap}: α={alphaRho:F3} β={beta:F3} → {result.Classification} ({reason})");
        Assert.Equal(expected, result.Classification);
    }

    // ────────────────────────────────────────────────────────────
    // [Fact] Matrix report — prints full 16-pair table
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a full human-readable matrix report of all 12 pairs
    /// (K1–K3 × F1–F4, excluding K4 which requires baryonic data).
    /// </summary>
    [Fact]
    public void B1CM05_MatrixReport()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B1 COMPLETE CLASSIFICATION MATRIX");
        _output.WriteLine("  δK profiles × F extraction maps");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        // Header
        _output.WriteLine($"  {"Pair",-10} {"α (rρ)",8} {"β (a)",8} {"|α+1|",8} {"|β+2|",8} {"Classification",-14} {"Note"}");
        _output.WriteLine($"  {new string('─', 10)} {new string('─', 8)} {new string('─', 8)} {new string('─', 8)} {new string('─', 8)} {new string('─', 14)} {new string('─', 30)}");
        _output.WriteLine("");

        foreach (var kp in CouplingProfiles.AllKProfiles)
        {
            foreach (var fm in DensityExtractionMaps.AllFMaps)
            {
                double alpha = DensityExtractionMaps.ExpectedRhoAlpha(kp, fm);
                double beta = DensityExtractionMaps.ExpectedBeta(alpha);

                if (double.IsNaN(alpha) || double.IsNaN(beta))
                {
                    _output.WriteLine($"  {$"{kp}×{fm}",-10} {"N/A",8} {"N/A",8} {"N/A",8} {"N/A",8} {"INVALID",-14} {"exponential — no power law"}");
                    continue;
                }

                var result = V4GateClassifier.Evaluate(kp, fm, alpha, beta);
                double alphaErr = Math.Abs(alpha + 1.0);
                double betaErr = Math.Abs(beta + 2.0);

                string note = (kp, fm) switch
                {
                    ("K1", "F1") => "⭐ BEST — unique Newtonian match",
                    ("K1", "F3") or ("K1", "F4") => "≡ F1 (reduces in sync state)",
                    ("K1", "F2") => "gradient adds one power",
                    (_, "F2") => "gradient makes steeper",
                    _ => ""
                };

                _output.WriteLine($"  {$"{kp}×{fm}",-10} {alpha,8:F3} {beta,8:F3} {alphaErr,8:F3} {betaErr,8:F3} {result.Classification,-14} {note}");
            }
            _output.WriteLine("");
        }

        // Summary
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  SUMMARY");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  VALID:    K1×F1, K1×F3, K1×F4  (all reduce to same mechanism)");
        _output.WriteLine("  INVALID:  K2×all (8), K3×all (4), K1×F2 (1)");
        _output.WriteLine("  PENDING:  K4×all (4) — requires baryonic data (B2)");
        _output.WriteLine("");
        _output.WriteLine("  Best pair: K1×F1");
        _output.WriteLine("  Mechanism: δK(r) = k·M/r  →  ρ_eff ∝ δK  →  a(r) ∝ 1/r²");
        _output.WriteLine("  Calibration: k = G·K₀/c² (post-hoc)");
        _output.WriteLine("");
        _output.WriteLine("  Classification after critical review: PARTIAL");
        _output.WriteLine("  (k is post-hoc calibrated, K1 is chosen, not derived)");
        _output.WriteLine("  See: docsV4/review/TRM_V4_B1_CriticalReview.md");
    }

    // ────────────────────────────────────────────────────────────
    // [Fact] Gate classifier threshold edge cases
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void B1CM06_ClassifierThresholdBoundaries()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B1 — CLASSIFIER THRESHOLD EDGE CASES");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        // Exact targets
        Assert.Equal(V4Classification.Valid, V4GateClassifier.Classify(-1.0, -2.0));

        // Just inside VALID
        Assert.Equal(V4Classification.Valid, V4GateClassifier.Classify(-1.09, -2.09));

        // Boundary: exactly at threshold (strict <, so 0.10 is NOT valid)
        Assert.Equal(V4Classification.Partial, V4GateClassifier.Classify(-1.10, -2.0));
        Assert.Equal(V4Classification.Partial, V4GateClassifier.Classify(-1.0, -2.10));

        // Just inside PARTIAL (one error at boundary, one perfect)
        Assert.Equal(V4Classification.Partial, V4GateClassifier.Classify(-1.20, -2.20));

        // Boundary: alpha at 0.30, beta perfect → still PARTIAL (beta < 0.30)
        Assert.Equal(V4Classification.Partial, V4GateClassifier.Classify(-1.30, -2.0));

        // Both clearly above 0.30 → INVALID
        Assert.Equal(V4Classification.Invalid, V4GateClassifier.Classify(-1.31, -2.31));

        // Way outside
        Assert.Equal(V4Classification.Invalid, V4GateClassifier.Classify(-3.0, -4.0));

        _output.WriteLine("  ✅ All threshold edge cases passed.");
    }
}
