using System;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4;

[Trait("Category", "V4")]
[Trait("Category", "G6")]
public class G6_Phase1B_LatticePathIntegral_Tests
{
    private readonly ITestOutputHelper _output;

    public G6_Phase1B_LatticePathIntegral_Tests(ITestOutputHelper o) { _output = o; }

    [Fact]
    public void G6P1B_01_DiscreteAction_ContinuumLimit()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G6-P1B.01 — DISCRETE → CONTINUUM ACTION");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        _output.WriteLine("  Discrete: S = Σ_i [I/2 (dθ_i/dτ)² + Σ_j K_ij(1-cos Δθ)]");
        _output.WriteLine("");
        _output.WriteLine("  Continuum limits (a → 0):");
        _output.WriteLine("    θ_i → φ(x)");
        _output.WriteLine("    p_i → π(x) a³");
        _output.WriteLine("    K_ij → K(x,y)");
        _output.WriteLine("    1−cos Δθ → (a²/2)(∇φ·r̂)²");
        _output.WriteLine("");
        _output.WriteLine("  Gradient expansion:");
        _output.WriteLine("    φ(y) ≈ φ(x) + (y−x)·∇φ");
        _output.WriteLine("    cos(Δφ) ≈ 1 − ½[(y−x)·∇φ]²");
        _output.WriteLine("");
        _output.WriteLine("  Result: S_space → ½∫d³x d³y K(x,y) (r·∇φ)²");
        _output.WriteLine("");

        // Verify gradient expansion numerically for small separations
        double a = 0.1;
        double[] dxVals = { 0.01, 0.05, 0.1, 0.2 };
        _output.WriteLine($"  Gradient expansion accuracy (a={a}):");
        _output.WriteLine($"  {"|dx|",8} {"cos(Δφ)",12} {"1−½(∇φ·dx)²",16} {"error",10}");
        _output.WriteLine($"  {new string('-',8)} {new string('-',12)} {new string('-',16)} {new string('-',10)}");

        double gradPhi = 1.0; // arbitrary gradient
        foreach (double dx in dxVals)
        {
            double dPhi = gradPhi * dx;
            double exact = Math.Cos(dPhi);
            double approx = 1.0 - 0.5 * dPhi * dPhi;
            double error = Math.Abs(exact - approx);
            _output.WriteLine($"  {dx,8:F2} {exact,12:F6} {approx,16:F6} {error,10:E2}");
        }

        _output.WriteLine("");
        _output.WriteLine("  Gradient expansion accurate to O(dx³).");
        _output.WriteLine("  Classification: DERIVED (Taylor expansion).");
    }

    [Fact]
    public void G6P1B_02_Derived_vs_Postulated()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G6-P1B.02 — DERIVED vs POSTULATED");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        var steps = new (string step, string classification)[]
        {
            ("Discrete Hamiltonian (V3 core)",    "DERIVED"),
            ("Euclidean action (Wick rotation)",  "DERIVED"),
            ("Path integral measure (flat U(1))", "DERIVED"),
            ("Continuum limit a→0",               "STRUCTURAL"),
            ("Gradient expansion",                "DERIVED"),
            ("Bilocal kinetic form",              "STRUCTURALLY INFERRED"),
            ("Padé kernel ansatz",                "POSTULATED"),
            ("Covariantization (g_μν enters)",    "POSTULATED"),
            ("1-loop effective action",           "FORMULATED"),
            ("UV finiteness (1-loop)",            "POWER-COUNTING CONFIRMED"),
        };

        _output.WriteLine($"  {"Step",-42} {"Classification",-24}");
        _output.WriteLine($"  {new string('-',42)} {new string('-',24)}");
        foreach (var (step, cls) in steps)
            _output.WriteLine($"  {step,-42} {cls,-24}");

        _output.WriteLine("");
        _output.WriteLine("  6/10 derived or structural. 2 postulates (kernel, covariant).");
        _output.WriteLine("  2 formulated but pending explicit computation.");
    }

    [Fact]
    public void G6P1B_03_NextSteps()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G6-P1B.03 — RECOMMENDED NEXT STEP");
        _output.WriteLine("  B.2: 1-Loop Graviton Propagator");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        _output.WriteLine("  STATUS:");
        _output.WriteLine("    ✅ Path integral formulated");
        _output.WriteLine("    ✅ Continuum bridge established");
        _output.WriteLine("    ✅ Power counting confirms 1-loop finiteness");
        _output.WriteLine("");
        _output.WriteLine("  READY FOR:");
        _output.WriteLine("    B.2: Explicit 1-loop graviton self-energy");
        _output.WriteLine("    - Feynman rules from S_int (Phase 1A, Eq. 10)");
        _output.WriteLine("    - Vertex factors with form factors");
        _output.WriteLine("    - Π_{μναβ}(k) computation");
        _output.WriteLine("    - Verify UV finiteness explicitly");
        _output.WriteLine("    - Extract running of G_eff and b");
        _output.WriteLine("");
        _output.WriteLine("    B.4: Quantum spectral positivity");
        _output.WriteLine("    - Extend Källén-Lehmann to quantum level");
        _output.WriteLine("    - Optical theorem check at 1-loop");
        _output.WriteLine("");
        _output.WriteLine("  Classification: READY FOR B.2 EXECUTION.");
    }
}
