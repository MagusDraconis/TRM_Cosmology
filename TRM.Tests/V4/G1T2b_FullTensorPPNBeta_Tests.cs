using System;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4;

/// <summary>
/// G1-T2b: Full Tensor PPN Beta Extraction
/// Computes scalar-only β from G1-T2a coefficients and documents the full tensor gap.
/// </summary>
[Trait("Category", "V4")]
[Trait("Category", "G1")]
public class G1T2b_FullTensorPPNBeta_Tests
{
    private readonly ITestOutputHelper _output;

    // From G1-T2a numerical computation
    private const double A = 3.237491;
    private const double B5 = -0.617620;    // scalar cubic coefficient

    public G1T2b_FullTensorPPNBeta_Tests(ITestOutputHelper o) { _output = o; }

    [Fact]
    public void G1T2b_01_Scalar_Only_Beta()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G1-T2b.01 — SCALAR-ONLY PPN β");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        // From the scalar sector field equation:
        // 8a·∇²φ + 4b₅·(∇φ)² = −8πG·ρ
        // Static solution: φ = 2U + (b₅/a)·U² + ...
        // g_00 = −1 − φ = −1 − 2U − (b₅/a)·U²
        // PPN: g_00 = −1 + 2U − 2β·U²
        // → −(b₅/a) = −2β  →  β = b₅/(2a) for this convention
        // → β = −b₅/(2a) (since b₅ < 0, β > 0)

        double betaScalar = -B5 / (2.0 * A);

        _output.WriteLine($"  a  = {A:F6}");
        _output.WriteLine($"  b₅ = {B5:F6}");
        _output.WriteLine($"  β_scalar = −b₅/(2a) = {betaScalar:F6}");
        _output.WriteLine("");
        _output.WriteLine($"  GR:  β = 1.000");
        _output.WriteLine($"  TRM scalar-only: β ≈ {betaScalar:F3}");
        _output.WriteLine("");
        _output.WriteLine("  ⚠ Scalar-only β ≈ 0.095 — far from GR (β=1).");
        _output.WriteLine("  This is NOT the full prediction — tensor sector");
        _output.WriteLine("  contributions (b₁–b₄) may compensate significantly.");
    }

    [Fact]
    public void G1T2b_02_Tensor_Contraction_Count()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G1-T2b.02 — TENSOR CONTRACTION INVENTORY");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        _output.WriteLine("  The full cubic Lagrangian has 5 independent contractions:");
        _output.WriteLine("");
        _output.WriteLine("  b₁: B^μν · ∂_αB_μν · ∂^αB^ρ_ρ     (trace-trace-trace)");
        _output.WriteLine("  b₂: B^μν · ∂_αB_μρ · ∂^αB_ν^ρ     (Riemann-like)");
        _output.WriteLine("  b₃: B^μν · ∂_μB^αβ · ∂_νB_αβ      (derivative on external B)");
        _output.WriteLine("  b₄: B^μν · ∂_μB_να · ∂_βB^αβ       (divergence coupling)");
        _output.WriteLine("  b₅: B·(∂B)²                          (scalar reduction — computed)");
        _output.WriteLine("");
        _output.WriteLine("  Only b₅ is computed (G1-T2a). b₁–b₄ require the");
        _output.WriteLine("  O(Δ⁴) expansion of K(x, x+Δ) with tensor contractions.");
        _output.WriteLine("");
        _output.WriteLine("  Each bᵢ is a different integral over the quartic kernel:");
        _output.WriteLine("  bᵢ = ∫ d⁴Δ [kernel derivatives] × [tensor structure]");
    }

    [Fact]
    public void G1T2b_03_Possible_Compensation_Scenarios()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G1-T2b.03 — COMPENSATION SCENARIOS");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        // β_total = β_scalar + Δβ_tensor
        // β_scalar ≈ 0.095 (computed)
        // Δβ_tensor from b₁–b₄ (unknown)

        _output.WriteLine("  β = β_scalar + Δβ_tensor");
        _output.WriteLine($"  β_scalar ≈ 0.095 (G1-T2a)");
        _output.WriteLine("");
        _output.WriteLine("  Scenarios:");
        _output.WriteLine("");
        _output.WriteLine("  A) Δβ ≈ +0.905 → β ≈ 1.0   COMPATIBLE (fine-tuned?)");
        _output.WriteLine("  B) Δβ ≈ +0.5   → β ≈ 0.6   TENSION (Solar System rules out)");
        _output.WriteLine("  C) Δβ ≈ +0.0   → β ≈ 0.095 RULED OUT");
        _output.WriteLine("  D) Δβ < 0       → β < 0.095 RULED OUT (β > 0 required)");
        _output.WriteLine("");
        _output.WriteLine("  To distinguish: compute b₁–b₄.");
        _output.WriteLine("  This is the next computational step.");
    }

    [Fact]
    public void G1T2b_04_Solar_System_Constraints()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G1-T2b.04 — SOLAR SYSTEM CONSTRAINTS");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        _output.WriteLine("  PPN β constraints:");
        _output.WriteLine("    Mercury perihelion:  |β−1| < 3×10⁻³");
        _output.WriteLine("    Lunar Laser Ranging: |β−1| < 1×10⁻³");
        _output.WriteLine("    Cassini:             |β−1| < 2.3×10⁻⁴ (combined)");
        _output.WriteLine("");
        _output.WriteLine($"  Scalar-only β ≈ 0.095:");
        _output.WriteLine($"    |β−1| ≈ 0.905 ≫ 2.3×10⁻⁴  → RULED OUT (if full β ≈ scalar β)");
        _output.WriteLine("");
        _output.WriteLine("  If full tensor β ≈ 1:");
        _output.WriteLine("    Solar System compatible ✓");
        _output.WriteLine("");
        _output.WriteLine("  ⚠ The scalar-only result does NOT falsify TRM.");
        _output.WriteLine("  It identifies the tensor computation as essential.");
    }

    [Fact]
    public void G1T2b_05_Summary()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G1-T2b SUMMARY");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  COMPUTED:");
        _output.WriteLine("    ✅ b₅ = −0.618 (scalar cubic, G1-T2a)");
        _output.WriteLine("    ✅ β_scalar = 0.095 (from b₅/a)");
        _output.WriteLine("");
        _output.WriteLine("  OPEN (requires computation):");
        _output.WriteLine("    ⬜ b₁–b₄ (tensor cubic coefficients)");
        _output.WriteLine("    ⬜ β_total = β_scalar + Δβ_tensor");
        _output.WriteLine("    ⬜ Comparison with Solar System constraints");
        _output.WriteLine("");
        _output.WriteLine("  CLASSIFICATION: PARTIAL");
        _output.WriteLine("    Scalar sector computed — gives β_scalar ≪ 1.");
        _output.WriteLine("    Full tensor β requires b₁–b₄ computation.");
        _output.WriteLine("    TRM is neither confirmed nor ruled out at 1PN.");
    }
}
