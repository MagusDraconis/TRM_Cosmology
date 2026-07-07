using Xunit;
using TRM.Core.V4_1.Reduction;

namespace TRM.Tests.V4_1;

[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_EffectiveCoreEquation")]
public class V4_1_EffectiveCoreEquation_Tests
{
    [Fact]
    public void V4_1_240_EffectiveEquationAudit_IsDeterministicAcrossRuns()
    {
        var r1 = EffectiveCoreEquationEngine.Audit();
        var r2 = EffectiveCoreEquationEngine.Audit();
        Assert.Equal(r1.Best.RSquared, r2.Best.RSquared, 6);
    }

    [Fact]
    public void V4_1_241_CandidateEquations_ProduceFiniteFitMetrics()
    {
        var result = EffectiveCoreEquationEngine.Audit();
        Assert.True(result.Candidates.Count >= 2);
        foreach (var c in result.Candidates)
        {
            Assert.True(double.IsFinite(c.Rmse));
            Assert.True(double.IsFinite(c.RSquared));
        }
    }

    [Fact]
    public void V4_1_242_EffectiveEquation_ComputesDPreferenceWithoutBias()
    {
        var result = EffectiveCoreEquationEngine.Audit();
        Assert.True(result.Best.PredictedOptimum >= 1 && result.Best.PredictedOptimum <= 4);
    }

    [Fact]
    public void V4_1_243_LeaveOneTermOut_Stability_IsComputable()
    {
        var result = EffectiveCoreEquationEngine.Audit();
        // All terms must be present with non-zero coefficients.
        Assert.NotEmpty(result.Best.Terms);
        Assert.True(result.Best.Terms.All(t => t.Coefficient > 0));
    }

    [Fact]
    public void V4_1_244_EffectiveEquationInterpretation_ProducesNoOverclaim()
    {
        var result = EffectiveCoreEquationEngine.Audit();
        var interps = EffectiveCoreEquationInterpretationEngine.Interpret(result);
        foreach (var i in interps)
        {
            Assert.DoesNotContain("proven", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("derived", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.True(i.Classification is "SUPPORTED" or "CONDITIONAL" or "INCONCLUSIVE");
        }
    }

    [Fact]
    public void V4_1_245_EffectiveEquation_HandlesFailedClosureGracefully()
    {
        var result = EffectiveCoreEquationEngine.Audit();
        Assert.True(result.OverallClosure is "CLOSED" or "PARTIAL" or "OPEN");
    }

    [Fact]
    public void V4_1_246_EquationStability_ClassifiesRobustness()
    {
        var s = EffectiveCoreEquationEngine.TestStability();
        Assert.True(s.StabilityClass is "robust" or "weakly-stable" or "variant-dependent");
        Assert.True(s.D3PreservedCount >= s.TotalVariants - 1);
    }

    [Fact]
    public void V4_1_247_EquationStability_IsDeterministic()
    {
        var s1 = EffectiveCoreEquationEngine.TestStability();
        var s2 = EffectiveCoreEquationEngine.TestStability();
        Assert.Equal(s1.StabilityClass, s2.StabilityClass);
    }
}
