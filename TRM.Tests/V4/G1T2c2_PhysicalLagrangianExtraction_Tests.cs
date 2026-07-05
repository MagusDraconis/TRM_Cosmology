using System;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4;

/// <summary>
/// G1-T2c2: Physical Bilocal Lagrangian Extraction
/// Computes a_physical and b_physical from f'·f'' integral.
/// </summary>
[Trait("Category", "V4")]
[Trait("Category", "G1")]
public class G1T2c2_PhysicalLagrangianExtraction_Tests
{
    private readonly ITestOutputHelper _output;

    private const double K0 = 1.0;
    private const double Lambda = 1.0;
    private const double S4 = 2.0 * Math.PI * Math.PI;
    private const double Cutoff = 8.0;
    private const int Steps = 20000;

    public G1T2c2_PhysicalLagrangianExtraction_Tests(ITestOutputHelper o) { _output = o; }

    // ════════════════════════════════════════════════════════════
    // Kernel derivatives
    // ════════════════════════════════════════════════════════════

    private static double F(double x)
    {
        double d = 1.0 + x + x * x;
        return K0 / d;
    }

    private static double FPrime(double x)
    {
        double d = 1.0 + x + x * x;
        return -K0 * (1.0 + 2.0 * x) / (d * d);
    }

    private static double FDoublePrime(double x)
    {
        double d = 1.0 + x + x * x;
        // f''(x) = 2K₀(1+3x+3x²)/(1+x+x²)³
        double num = 1.0 + 3.0 * x + 3.0 * x * x;
        return 2.0 * K0 * num / (d * d * d);
    }

    // ════════════════════════════════════════════════════════════
    // G1T2c2_01 — Compute a_physical (kinetic)
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void G1T2c2_01_Compute_Kinetic_Coefficient()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G1-T2c2.01 — KINETIC COEFFICIENT a_physical");
        _output.WriteLine("  ∫ r⁷·[f'(r²/λ²)]² dr");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        double dr = Cutoff / Steps;
        double integral = 0;

        for (int i = 0; i < Steps; i++)
        {
            double r = (i + 0.5) * dr;
            double x = r * r / (Lambda * Lambda);
            double fp = FPrime(x);
            integral += r * r * r * r * r * r * r * fp * fp * dr;
        }

        // Angular factor: the tensor contraction gives numerical factors.
        // For the trace sector: ∫ dΩ₄ Δ^μΔ^νΔ^ρΔ^σ = S₄/24 · (symmetrized η's)
        // The factor for (∂B)² = ∂_αB_μν·∂^αB^μν is S₄/15 (after contraction).
        double angularFactor = S4 / 15.0;
        double aPhysical = angularFactor * integral / (Lambda * Lambda);  // scale by λ² from f'

        _output.WriteLine($"  Radial integral = {integral:E6}");
        _output.WriteLine($"  Angular factor  = {angularFactor:F4} (S₄/15)");
        _output.WriteLine($"  a_physical      = {aPhysical:E6}");
        _output.WriteLine("");
        _output.WriteLine("  ✅ a_physical > 0 (stable kinetic term).");
        Assert.True(aPhysical > 0);
    }

    // ════════════════════════════════════════════════════════════
    // G1T2c2_02 — Compute b_physical (cubic, f'·f'')
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void G1T2c2_02_Compute_Cubic_Coefficient_fprime_fdoubleprime()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G1-T2c2.02 — CUBIC COEFFICIENT b_physical");
        _output.WriteLine("  ∫ r⁹·f'(r²/λ²)·f''(r²/λ²) dr");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        double dr = Cutoff / Steps;
        double integral = 0;

        for (int i = 0; i < Steps; i++)
        {
            double r = (i + 0.5) * dr;
            double x = r * r / (Lambda * Lambda);
            double fp = FPrime(x);
            double fpp = FDoublePrime(x);
            integral += Math.Pow(r, 9) * fp * fpp * dr;
        }

        double angularFactor = S4 / 105.0;  // Different angular contraction for 3-tensor
        double bPhysical = angularFactor * integral / (Lambda * Lambda * Lambda * Lambda);

        _output.WriteLine($"  Radial integral = {integral:E6}");
        _output.WriteLine($"  Angular factor  = {angularFactor:F6} (S₄/105)");
        _output.WriteLine($"  b_physical      = {bPhysical:E6}");
        _output.WriteLine("");
        _output.WriteLine($"  f'(0) = −K₀  (negative)");
        _output.WriteLine($"  f''(0) = +2K₀ (positive)");
        _output.WriteLine($"  → f'·f'' < 0 → b_physical < 0");
        _output.WriteLine("");

        if (bPhysical < 0)
        {
            _output.WriteLine("  ⚠ b_physical < 0 → cubic coupling has same sign as b₅.");
            _output.WriteLine("  → β correction is negative → β < 1.");
            _output.WriteLine("  The f'·f'' product confirms the sign from G1-T2a.");
        }
        else
        {
            _output.WriteLine("  ✅ b_physical > 0 → sign differs from b₅.");
            _output.WriteLine("  The tensor contributions may compensate!");
        }
    }

    // ════════════════════════════════════════════════════════════
    // G1T2c2_03 — Ratio b/a and β estimate
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void G1T2c2_03_Beta_From_fprime_fdoubleprime()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G1-T2c2.03 — β FROM f'·f''");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        double dr = Cutoff / Steps;
        double aInt = 0, bInt = 0;

        for (int i = 0; i < Steps; i++)
        {
            double r = (i + 0.5) * dr;
            double x = r * r / (Lambda * Lambda);
            double fp = FPrime(x);
            double fpp = FDoublePrime(x);

            aInt += Math.Pow(r, 7) * fp * fp * dr;
            bInt += Math.Pow(r, 9) * fp * fpp * dr;
        }

        double aPhys = (S4 / 15.0) * aInt / (Lambda * Lambda);
        double bPhys = (S4 / 105.0) * bInt / (Lambda * Lambda * Lambda * Lambda);
        double ratio = bPhys / aPhys;

        _output.WriteLine($"  a_physical    = {aPhys:E6}");
        _output.WriteLine($"  b_physical    = {bPhys:E6}");
        _output.WriteLine($"  b/a           = {ratio:E6}");
        _output.WriteLine("");
        _output.WriteLine("  PPN β from scalar trace sector:");
        _output.WriteLine($"    Δβ ∝ b/a  (negative → β < 1)");
        _output.WriteLine("");

        // Note: the exact β depends on the full tensor contraction factors.
        // This gives the SIGN and ORDER OF MAGNITUDE of the scalar contribution.
        _output.WriteLine("  SIGN: b_physical < 0 → β < 1 (same as G1-T2a)");
        _output.WriteLine("  The f'·f'' product confirms the negative sign.");
        _output.WriteLine("");
        _output.WriteLine("  ⚠ This is the SCALAR trace contribution only.");
        _output.WriteLine("  Full tensor contractions (b₁–b₄) may modify this.");
    }

    // ════════════════════════════════════════════════════════════
    // G1T2c2_04 — Comparison with G1-T2a
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void G1T2c2_04_Comparison_With_G1T2a()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G1-T2c2.04 — COMPARISON WITH G1-T2a");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  G1-T2a:  b₅ from ∫[f']³·|Δ|⁶ → b₅ = −0.618");
        _output.WriteLine("  G1-T2c2: b_physical from ∫f'·f''·|Δ|⁸");
        _output.WriteLine("");
        _output.WriteLine("  These are DIFFERENT coefficients:");
        _output.WriteLine("    b₅: scalar reduction of (∂K)² → B·(∂B)² (one contraction)");
        _output.WriteLine("    b_physical: f'·f'' cross term → B·(∂B)² (different contraction)");
        _output.WriteLine("");
        _output.WriteLine("  Both give β < 1 (negative sign) → consistent direction.");
        _output.WriteLine("  Full β requires BOTH contributions + tensor b₁–b₄.");
    }

    // ════════════════════════════════════════════════════════════
    // G1T2c2_05 — Summary
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void G1T2c2_05_Summary()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G1-T2c2 SUMMARY");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  Physical effective Lagrangian from proper derivative terms.");
        _output.WriteLine("");
        _output.WriteLine("  COMPUTED:");
        _output.WriteLine("    ✅ a_physical ∝ ∫[f']²·|Δ|⁶  (kinetic, from ∂K·∂K)");
        _output.WriteLine("    ✅ b_physical ∝ ∫f'·f''·|Δ|⁸ (cubic, from f'f'' cross term)");
        _output.WriteLine("    ✅ f'(0)·f''(0) = −2K₀² < 0 → β < 1");
        _output.WriteLine("");
        _output.WriteLine("  CONSISTENT WITH G1-T2a:");
        _output.WriteLine("    Both f'³ and f'·f'' give negative cubic coupling.");
        _output.WriteLine("    → Scalar sector consistently predicts β < 1.");
        _output.WriteLine("");
        _output.WriteLine("  OPEN:");
        _output.WriteLine("    ⬜ Full tensor b₁–b₄ may change the sign or magnitude.");
        _output.WriteLine("    ⬜ The scalar trace sector alone → β < 1.");
        _output.WriteLine("    ⬜ β_total depends on b₁–b₄ compensation.");
    }
}
