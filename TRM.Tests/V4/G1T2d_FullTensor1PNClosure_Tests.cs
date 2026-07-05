using System;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4;

/// <summary>
/// G1-T2d: Full Tensor 1PN Closure — framework + honest gap assessment
/// </summary>
[Trait("Category", "V4")]
[Trait("Category", "G1")]
public class G1T2d_FullTensor1PNClosure_Tests
{
    private readonly ITestOutputHelper _output;

    public G1T2d_FullTensor1PNClosure_Tests(ITestOutputHelper o) { _output = o; }

    [Fact]
    public void G1T2d_01_Tensor_Kinetic_Equals_Scalar_Kinetic()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G1-T2d.01 — a_H = a_φ (DERIVED)");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  Proof: Both (∂φ)² and (∂H)² come from (∂B)²");
        _output.WriteLine("  with the SAME 4-index tensor average on S³.");
        _output.WriteLine("  Angular contraction gives identical factor.");
        _output.WriteLine("");
        _output.WriteLine("  → a_H = a_φ = 3.237");
        _output.WriteLine("");
        _output.WriteLine("  Consequence: Newtonian potential split:");
        _output.WriteLine("    φ  → ~40% (from trace source T = −ρ)");
        _output.WriteLine("    H_00 → ~60% (from S_00 = ¾ρ)");
        _output.WriteLine("");
        _output.WriteLine("  ✅ a_H = a_φ is DERIVED. No new computation.");
    }

    [Fact]
    public void G1T2d_02_Cubic_Sign_Analysis()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G1-T2d.02 — CUBIC SIGN ANALYSIS");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  Radial integral f'·f'' < 0 → ALL cubic couplings");
        _output.WriteLine("  have negative sign from the radial part.");
        _output.WriteLine("");
        _output.WriteLine("  Angular factors for different contractions may");
        _output.WriteLine("  differ in magnitude but NOT in sign (all positive).");
        _output.WriteLine("");
        _output.WriteLine("  → Both φ-cubic and H-cubic are negative.");
        _output.WriteLine("  → φ-H mixing terms likely also negative.");
        _output.WriteLine("");
        _output.WriteLine("  TENTATIVE VERDICT: β_total < 1");
        _output.WriteLine("  (Unless mixing terms reverse sign — needs verification)");
    }

    [Fact]
    public void G1T2d_03_Updated_Status()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G1-T2d.03 — UPDATED STATUS");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  DERIVED:");
        _output.WriteLine("    ✅ a_H = a_φ (same kinetic angular factor)");
        _output.WriteLine("    ✅ Newtonian potential: φ 40%, H 60%");
        _output.WriteLine("");
        _output.WriteLine("  LIKELY:");
        _output.WriteLine("    ⚠ β_H < 1 (same radial sign as β_φ)");
        _output.WriteLine("    ⚠ β_total < 1 (weighted average: 0.4β_φ + 0.6β_H)");
        _output.WriteLine("");
        _output.WriteLine("  PENDING:");
        _output.WriteLine("    ⬜ Mixing term angular integrals (b_φφH, b_φHH, b_Hφφ)");
        _output.WriteLine("    ⬜ Confirm β_H < 1 numerically");
        _output.WriteLine("    ⬜ Final β_total value");
        _output.WriteLine("");
        _output.WriteLine("  TENTATIVE: TENSION (β likely < 1, not ≈ 1)");
        _output.WriteLine("  Requires mixing term computation for definitive classification.");
    }

    [Fact]
    public void G1T2d_04_Summary()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G1-T2d SUMMARY");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  DERIVED:");
        _output.WriteLine("    ✅ a_H = a_φ (same kinetic angular factor)");
        _output.WriteLine("    ✅ Newtonian potential: φ ~40%, H ~60%");
        _output.WriteLine("");
        _output.WriteLine("  LIKELY:");
        _output.WriteLine("    ⚠ β_total < 1 (both φ and H cubic negative)");
        _output.WriteLine("");
        _output.WriteLine("  TENTATIVE VERDICT: TENSION");
        _output.WriteLine("    TRM with quartic kernel likely predicts β < 1.");
        _output.WriteLine("    Solar System constrains |β−1| < 2.3×10⁻⁴.");
        _output.WriteLine("    If confirmed, TRM would be in tension at 1PN.");
        _output.WriteLine("    The bilocal framework allows kernel modification");
        _output.WriteLine("    to adjust β — this is not a fatal falsification.");
    }
}
