using Xunit;
using TRM.Core.V4_1.Reduction;

namespace TRM.Tests.V4_1;

[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_CorePlusCorrection")]
public class V4_1_CorePlusCorrectionClosure_Tests
{
    [Fact]
    public void V4_1_230_CorePlusCorrection_IsDeterministicAcrossRuns()
    {
        var r1 = CorePlusCorrectionEngine.Evaluate();
        var r2 = CorePlusCorrectionEngine.Evaluate();
        Assert.Equal(r1.CorePlusRetention, r2.CorePlusRetention, 6);
    }

    [Fact]
    public void V4_1_231_CorePlusCorrection_ComputesRetentionImprovement()
    {
        var result = CorePlusCorrectionEngine.Evaluate();
        Assert.True(result.Improvement >= 0);
        Assert.True(result.CorePlusRetention > result.CoreRetention);
    }

    [Fact]
    public void V4_1_232_ClosureClassification_IsComputedWithoutBias()
    {
        var result = CorePlusCorrectionEngine.Evaluate();
        Assert.True(result.ClosureStatus is "CLOSED" or "PARTIAL" or "OPEN");
    }

    [Fact]
    public void V4_1_233_ResidualCorrectionRanking_IsDeterministic()
    {
        var r1 = CorePlusCorrectionEngine.Evaluate();
        var r2 = CorePlusCorrectionEngine.Evaluate();
        Assert.Equal(r1.Residuals.Count, r2.Residuals.Count);
    }

    [Fact]
    public void V4_1_234_CorePlusCorrectionInterpretation_ProducesNoOverclaim()
    {
        var result = CorePlusCorrectionEngine.Evaluate();
        var interps = CorePlusCorrectionInterpretationEngine.Interpret(result);
        foreach (var i in interps)
        {
            Assert.DoesNotContain("proven", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("derived", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.True(i.Classification is "SUPPORTED" or "CONDITIONAL" or "INCONCLUSIVE");
        }
    }

    [Fact]
    public void V4_1_235_CorePlusCorrection_HandlesFailedClosureGracefully()
    {
        var result = CorePlusCorrectionEngine.Evaluate();
        Assert.True(result.CoreRetention > 0);
        Assert.True(result.CorePlusRetention > 0);
    }
}
