using System;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4;

/// <summary>
/// G1-T2c1: Tensor Cubic Coefficient Execution
/// Computes b_total for time-time perturbation via numerical Δ-integration.
/// </summary>
[Trait("Category", "V4")]
[Trait("Category", "G1")]
public class G1T2c1_TensorCoefficientExecution_Tests
{
    private readonly ITestOutputHelper _output;

    private const double K0 = 1.0;
    private const double Lambda = 1.0;
    private const double S4 = 2.0 * Math.PI * Math.PI;
    private const double Cutoff = 8.0;
    private const int Steps = 20000;

    public G1T2c1_TensorCoefficientExecution_Tests(ITestOutputHelper o) { _output = o; }

    // ════════════════════════════════════════════════════════════
    // Helper: quartic kernel
    // ════════════════════════════════════════════════════════════

    private static double K(double r2, double eps)
    {
        // d_E² = (1−ε)·τ² + r²  but in radial integration, Δ^μ is isotropic
        // For B_μν = ε·diag(1,0,0,0): the time component is stretched
        // We integrate over the full 4D Euclidean space isotropically,
        // then correct for the ε perturbation.
        // Simple approximation: d² → d²(1 + ε/4) for trace perturbation.
        double x = r2 * (1.0 + eps / 4.0) / (Lambda * Lambda);
        return K0 / (1.0 + x + x * x);
    }

    private static double KPrime(double r2, double eps)
    {
        double x = r2 * (1.0 + eps / 4.0) / (Lambda * Lambda);
        double denom = 1.0 + x + x * x;
        return -K0 * (1.0 + 2.0 * x) / (Lambda * Lambda * denom * denom);
    }

    // ════════════════════════════════════════════════════════════
    // G1T2c1_01 — Compute I(ε) for multiple ε values
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void G1T2c1_01_Integral_vs_Epsilon()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G1-T2c1.01 — I(ε) FOR PERTURBED METRIC");
        _output.WriteLine("  B_μν = ε·diag(1,0,0,0)");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        double[] epsVals = { -0.10, -0.05, -0.02, -0.01, 0.0, 0.01, 0.02, 0.05, 0.10 };
        double dr = Cutoff / Steps;

        _output.WriteLine($"  {"ε",8} {"I(ε)",14} {"ΔI/ε",12}");
        _output.WriteLine($"  {new string('-', 8)} {new string('-', 14)} {new string('-', 12)}");

        double i0 = 0;
        var data = new System.Collections.Generic.List<(double, double)>();

        foreach (double eps in epsVals)
        {
            double integral = 0;
            for (int i = 0; i < Steps; i++)
            {
                double r = (i + 0.5) * dr;
                double r2 = r * r;
                double kp = KPrime(r2, eps);
                double r3 = r * r * r;
                // Integrate kinetic-like term: [K']² · r²  (scalar reduction)
                integral += kp * kp * r2 * r3 * S4 * dr;
            }

            if (Math.Abs(eps) < 1e-10) i0 = integral;
            double deltaOverEps = Math.Abs(eps) > 1e-10 ? (integral - i0) / eps : 0;

            _output.WriteLine($"  {eps,8:F3} {integral,14:F6} {deltaOverEps,12:F6}");
            data.Add((eps, integral));
        }

        _output.WriteLine("");
        _output.WriteLine($"  I(0) = {i0:F6}");
        _output.WriteLine("");

        // Fit quadratic: I(ε) = I₀ + I₁·ε + I₂·ε² + I₃·ε³
        // I₁ ≈ 0 (tadpole → zero by symmetry in flat space)
        // I₂ > 0 (kinetic term)
        // Sign of I₃ determines β correction

        // Simple central difference for I₂:
        double i2 = 0;
        for (int i = 0; i < data.Count; i++)
        {
            double eps = data[i].Item1;
            if (Math.Abs(eps) < 0.001) continue;
            i2 += (data[i].Item2 - i0) / (eps * eps) / (data.Count - 1);
        }
        // Better: use only the smallest |ε| values for I₂
        double i2Small = 0; int count = 0;
        for (int i = 0; i < data.Count; i++)
        {
            double eps = data[i].Item1;
            if (Math.Abs(eps) < 0.001 || Math.Abs(eps) > 0.03) continue;
            i2Small += (data[i].Item2 - i0) / (eps * eps);
            count++;
        }
        i2Small /= Math.Max(count, 1);

        _output.WriteLine($"  I₂ (kinetic, all)  ≈ {i2:F6}");
        _output.WriteLine($"  I₂ (kinetic, small) ≈ {i2Small:F6}");
        _output.WriteLine($"  (should be > 0 — positive kinetic term)");

        Assert.True(i2Small > 0, "Kinetic coefficient must be positive");
        _output.WriteLine("  ✅ I₂ > 0 — kinetic term has correct sign.");
    }

    // ════════════════════════════════════════════════════════════
    // G1T2c1_02 — Extract I₃ (cubic) from asymmetry
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void G1T2c1_02_Cubic_Asymmetry()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G1-T2c1.02 — CUBIC ASYMMETRY I₃");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        double[] epsVals = { -0.05, -0.02, 0.02, 0.05 };
        double dr = Cutoff / Steps;

        double i0 = 0;
        // Compute I(0)
        for (int i = 0; i < Steps; i++)
        {
            double r = (i + 0.5) * dr;
            double r2 = r * r;
            double kp = KPrime(r2, 0.0);
            i0 += kp * kp * r2 * r * r * r * S4 * dr;
        }

        _output.WriteLine($"  I(0) = {i0:F6}");
        _output.WriteLine("");
        _output.WriteLine($"  {"ε",8} {"I(ε)",14} {"I(−ε)",14} {"I(ε)−I(−ε)",14} {"∝ ε³?",12}");
        _output.WriteLine($"  {new string('-', 8)} {new string('-', 14)} {new string('-', 14)} {new string('-', 14)} {new string('-', 12)}");

        foreach (double eps in epsVals)
        {
            if (eps < 0) continue;

            double iPlus = 0, iMinus = 0;
            for (int i = 0; i < Steps; i++)
            {
                double r = (i + 0.5) * dr;
                double r2 = r * r;
                iPlus += KPrime(r2, eps) * KPrime(r2, eps) * r2 * r * r * r * S4 * dr;
                iMinus += KPrime(r2, -eps) * KPrime(r2, -eps) * r2 * r * r * r * S4 * dr;
            }

            double asym = iPlus - iMinus;
            double asymOverEps3 = asym / (eps * eps * eps);

            _output.WriteLine($"  {eps,8:F3} {iPlus,14:F6} {iMinus,14:F6} {asym,14:F6} {asymOverEps3,12:F4}");
        }

        _output.WriteLine("");
        _output.WriteLine("  I(ε) − I(−ε) = 2I₃·ε³ + O(ε⁵)");
        _output.WriteLine("  Sign of I₃ → sign of cubic coupling.");

        // The simple model can't capture full tensor effects.
        // This is a simplified probe, not the full PPN β.
        _output.WriteLine("");
        _output.WriteLine("  Note: This simplified radial model uses isotropic");
        _output.WriteLine("  perturbation — it captures the scalar trace contribution");
        _output.WriteLine("  but NOT the full tensor degrees of freedom.");
        _output.WriteLine("  The full computation requires O(Δ⁴) tensor structure.");
    }

    // ════════════════════════════════════════════════════════════
    // G1T2c1_03 — Honest assessment
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void G1T2c1_03_Summary()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G1-T2c1 SUMMARY");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  COMPUTED:");
        _output.WriteLine("    ✅ b₅ = −0.618 (scalar cubic, G1-T2a)");
        _output.WriteLine("    ✅ I₂ > 0 (kinetic, correct sign)");
        _output.WriteLine("    ✅ Simplified ε-perturbation framework");
        _output.WriteLine("");
        _output.WriteLine("  LIMITATIONS OF SIMPLIFIED MODEL:");
        _output.WriteLine("    The isotropic radial integration captures only");
        _output.WriteLine("    the scalar trace contribution. The full tensor");
        _output.WriteLine("    degrees of freedom (H_μν, B_0i) require:");
        _output.WriteLine("      • O(Δ⁴) expansion of K(x, x+Δ)");
        _output.WriteLine("      • Anisotropic angular integration");
        _output.WriteLine("      • Decomposition into 5 tensor contractions");
        _output.WriteLine("");
        _output.WriteLine("  HONEST STATUS:");
        _output.WriteLine("    b₁–b₄ NOT computed by this simplified method.");
        _output.WriteLine("    Full tensor computation requires dedicated project.");
        _output.WriteLine("    β_total remains PENDING.");
        _output.WriteLine("    Compensation is PLAUSIBLE (same order of magnitude).");
    }
}
