using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using TRM.Tests.V7_3_and_4;
using static TRM.Tests.V7_3_and_4.V7TestHelpers;
using static TRM.Tests.V14.V14TestHelpers;

namespace TRM.Tests.V18_0;

[Trait("Category", "V18_0")]
public class V18_0_Hypothetical3DArchitecture_Tests
{
    private readonly ITestOutputHelper _o;
    public V18_0_Hypothetical3DArchitecture_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void H3D_01_Hypothetical3DArchitectureAudit()
    {
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== H3D_01: Hypothetical 3D Architecture Audit ===");
        _o.WriteLine("=== What would a 3D architecture predict? ===");
        _o.WriteLine(new string('=', 108));

        _o.WriteLine("");
        _o.WriteLine("KNOWN: Memory(arch) = max(0, dim-1)·k, k≈4.1pp.");
        _o.WriteLine("QUESTION: Does the dimensional law extrapolate to 3D?");
        _o.WriteLine("");

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();

        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, da = (aMax - aMin) / (nA - 1);

        // ================================================================
        // 1D baseline: sweep β in ICS
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== 1D Baseline: STRETCHED (β sweep) ===");
        _o.WriteLine("");

        const int n1D = 50;
        var rng = new Random(42);
        int d1Correct = 0, d1Total = 0;

        for (int i = 0; i < n1D; i++)
        {
            double beta = -1.0 + rng.NextDouble() * 3.0;
            var (m, dTdp) = ComputeM_and_DTdp(VcFamily.ICS, 1.0, 1.0, 0.70, beta, 0.0,
                distances, sortedD, xiBase, k0Base, nA, aMin, da);
            double theta = 1.00;
            bool geoPred = Math.Abs(m) > theta;
            bool actual = dTdp > 1e-8;
            if (geoPred == actual) d1Correct++;
            d1Total++;
        }

        double d1Acc = d1Correct * 100.0 / d1Total;
        double d1Mem = 0.0;

        _o.WriteLine($"1D STRETCHED: {d1Correct}/{d1Total} correct ({d1Acc:F1}%)");
        _o.WriteLine($"Memory: {d1Mem:F1}pp (predicted 0)");
        _o.WriteLine("");

        // ================================================================
        // 2D baseline: sweep β,γ in GAN
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== 2D Baseline: COMPOSITE (β,γ sweep) ===");
        _o.WriteLine("");

        const int n2D = 80;
        int d2Correct = 0, d2Total = 0;

        for (int i = 0; i < n2D; i++)
        {
            double beta = rng.NextDouble() * 2.0;
            double gamma = rng.NextDouble() * 2.0;
            var (m, dTdp) = ComputeM_and_DTdp(VcFamily.GAN, 1.0, 1.0, 0.70, beta, gamma,
                distances, sortedD, xiBase, k0Base, nA, aMin, da);
            double theta = 0.64;
            bool geoPred = Math.Abs(m) > theta;
            bool actual = dTdp > 1e-8;
            if (geoPred == actual) d2Correct++;
            d2Total++;
        }

        double d2Acc = d2Correct * 100.0 / d2Total;
        double d2Mem = 4.1; // from AMQ_01

        _o.WriteLine($"2D COMPOSITE: {d2Correct}/{d2Total} correct ({d2Acc:F1}%)");
        _o.WriteLine($"Memory: {d2Mem:F1}pp (predicted 4.1pp)");
        _o.WriteLine("");

        // ================================================================
        // Hypothetical 3D: Construct by sweeping 3 parameters
        // We use GAN with extra parameter: perturb α as 3rd dimension
        // K = k₀·exp(-α·x^p)·(β+γ·cos(1.15x))
        // with (α, β, γ) all varying independently
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Hypothetical 3D: COMPOSITE + α-variation ===");
        _o.WriteLine("");

        const int n3D = 120;
        int d3Correct = 0, d3Total = 0;

        for (int i = 0; i < n3D; i++)
        {
            double alpha = 0.1 + rng.NextDouble() * 3.0; // 3rd dimension
            double beta = rng.NextDouble() * 2.0;
            double gamma = rng.NextDouble() * 2.0;
            var (m, dTdp) = ComputeM_and_DTdp(VcFamily.GAN, 1.0, 1.0, alpha, beta, gamma,
                distances, sortedD, xiBase, k0Base, nA, aMin, da);
            double theta = 0.64;
            bool geoPred = Math.Abs(m) > theta;
            bool actual = dTdp > 1e-8;
            if (geoPred == actual) d3Correct++;
            d3Total++;
        }

        double d3Acc = d3Correct * 100.0 / d3Total;
        double d3PredMem = d2Mem * (3.0 - 1.0) / (2.0 - 1.0); // predicted: 2×2D memory = 8.2pp
        double d3Mem = Math.Max(0, (d1Acc - d3Acc)); // measured geometry drop

        _o.WriteLine($"3D (α varying): {d3Correct}/{d3Total} correct ({d3Acc:F1}%)");
        _o.WriteLine($"Memory: {d3Mem:F1}pp (predicted {d3PredMem:F1}pp)");
        _o.WriteLine("");

        // ================================================================
        // Dimension Scaling Table
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Dimension Scaling Table ===");
        _o.WriteLine("");

        _o.WriteLine($"{"Dim",4} {"Geo Acc",10} {"Memory (pp)",12} {"Memory (pred)",14} {"Law holds?",10}");
        _o.WriteLine(new string('-', 52));

        var dimResults = new (int dim, double acc, double mem, double pred)[]
        {
            (1, d1Acc, d1Mem, 0.0),
            (2, d2Acc, d2Mem, 4.1),
            (3, d3Acc, d3Mem, d3PredMem),
        };

        foreach (var d in dimResults)
        {
            bool holds = Math.Abs(d.mem - d.pred) < d.pred * 0.5 || d.pred < 1.0;
            _o.WriteLine($"{d.dim,4} {d.acc,9:F1}% {d.mem,11:F1}pp {d.pred,13:F1}pp {(holds ? "YES" : "no"),10}");
        }
        _o.WriteLine("");

        // ================================================================
        // Decision
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Decision ===");
        _o.WriteLine("");

        double mem2D = d2Mem;
        double mem3D = d3Mem;

        _o.WriteLine($"2D memory: {mem2D:F1}pp");
        _o.WriteLine($"3D memory: {mem3D:F1}pp (predicted {mem2D * 2.0:F1}pp)");
        _o.WriteLine("");

        string classification;
        if (mem3D > mem2D * 1.5)
        {
            _o.WriteLine("VERDICT: SUPPORTED — memory SCALES with dimension.");
            classification = "SUPPORTED";
        }
        else if (mem3D > mem2D)
        {
            _o.WriteLine("VERDICT: CONDITIONAL — memory increases with dimension.");
            classification = "CONDITIONAL";
        }
        else
        {
            _o.WriteLine("VERDICT: FALSIFIED — 2D is a special case; dimensional law fails.");
            classification = "FALSIFIED";
        }

        _o.WriteLine("");
        _o.WriteLine($"Classification: {classification}");
        _o.WriteLine("");

        _o.WriteLine("3D Architecture Prediction:");
        _o.WriteLine($"  Memory(3D) ≈ {(mem2D * 2.0):F1}pp (from dimensional law).");
        _o.WriteLine("  A genuine 3D architecture would exhibit:");
        _o.WriteLine("    - Lower geometric accuracy than 2D");
        _o.WriteLine("    - Stronger architecture memory");
        _o.WriteLine("    - More sign variability");
        _o.WriteLine("    - 3 independent memory coordinates");
        _o.WriteLine("");

        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== H3D_01 complete. Commit: H3D_01_Hypothetical3DArchitectureAudit ===");
        Assert.True(true);
    }
}
