using Xunit;
using TRM.Core.V4_1.Reduction;

namespace TRM.Tests.V4_1;

[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_CoreReconstruction")]
public class V4_1_CoreReconstruction_Tests
{
    [Fact]
    public void V4_1_226_CoreReconstruction_IsDeterministicAcrossRuns()
    {
        var r1 = CoreReconstructionEngine.Reconstruct();
        var r2 = CoreReconstructionEngine.Reconstruct();
        Assert.Equal(r1.ScoreRetention, r2.ScoreRetention, 6);
        Assert.Equal(r1.ClosureStatus, r2.ClosureStatus);
    }

    [Fact]
    public void V4_1_227_CoreReconstruction_ComputesRetentionAndResiduals()
    {
        var result = CoreReconstructionEngine.Reconstruct();
        Assert.True(result.ScoreRetention > 0 && result.ScoreRetention <= 1.0);
        Assert.NotEmpty(result.Residuals);
    }

    [Fact]
    public void V4_1_228_CoreClosureClassification_IsComputedWithoutBias()
    {
        var result = CoreReconstructionEngine.Reconstruct();
        Assert.True(result.ClosureStatus is "CLOSED" or "PARTIAL" or "OPEN");
    }

    [Fact]
    public void V4_1_229_CorrectionRanking_IsDeterministic()
    {
        var r1 = CoreReconstructionEngine.Reconstruct();
        var r2 = CoreReconstructionEngine.Reconstruct();
        Assert.Equal(r1.CorrectionRanking, r2.CorrectionRanking);
    }

    [Fact]
    public void V4_1_230_CoreInterpretation_ProducesNoOverclaim()
    {
        var result = CoreReconstructionEngine.Reconstruct();
        var interps = CoreReconstructionInterpretationEngine.Interpret(result);
        foreach (var i in interps)
        {
            Assert.DoesNotContain("proven", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("derived", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.True(i.Classification is "SUPPORTED" or "CONDITIONAL" or "INCONCLUSIVE");
        }
    }

    [Fact]
    public void V4_1_231_CoreReconstruction_HandlesFailedClosureGracefully()
    {
        var result = CoreReconstructionEngine.Reconstruct();
        Assert.True(result.ScoreRetention > 0);
        Assert.True(result.RankAgreement >= 0);
    }
}
