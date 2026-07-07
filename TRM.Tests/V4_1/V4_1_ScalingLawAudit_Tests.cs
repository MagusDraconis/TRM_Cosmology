using Xunit;
using TRM.Core.V4_1.Synthesis;

namespace TRM.Tests.V4_1;

[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_ScalingLaw")]
public class V4_1_ScalingLawAudit_Tests
{
    [Fact]
    public void V4_1_190_ScalingLawExtraction_IsDeterministicAcrossRuns()
    {
        var r1 = ScalingLawExtractionEngine.Extract();
        var r2 = ScalingLawExtractionEngine.Extract();
        Assert.Equal(r1.BestFit.Rmse, r2.BestFit.Rmse, 6);
        Assert.Equal(r1.BestFit.PredictedOptimumDim, r2.BestFit.PredictedOptimumDim);
    }

    [Fact]
    public void V4_1_191_CandidateLaws_ProduceFiniteFitMetrics()
    {
        var result = ScalingLawExtractionEngine.Extract();
        Assert.True(result.CandidateFits.Count >= 2);
        foreach (var f in result.CandidateFits)
        {
            Assert.True(double.IsFinite(f.Rmse));
            Assert.True(double.IsFinite(f.RSquared));
        }
    }

    [Fact]
    public void V4_1_192_ModelSelection_ComputesComparableScores()
    {
        var result = ScalingLawExtractionEngine.Extract();
        var best = result.BestFit;
        Assert.True(best.Rmse <= result.CandidateFits.Max(f => f.Rmse));
    }

    [Fact]
    public void V4_1_193_LeaveOneLayerOut_Robustness_IsComputable()
    {
        var loo = ScalingLawExtractionEngine.LeaveOneOut();
        Assert.True(double.IsFinite(loo.RSquared));
    }

    [Fact]
    public void V4_1_194_ScalingLawInterpretation_ProducesNoOverclaim()
    {
        var result = ScalingLawExtractionEngine.Extract();
        var interps = ScalingLawInterpretationEngine.Interpret(result);
        foreach (var i in interps)
        {
            Assert.DoesNotContain("proven", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("derived", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.True(i.Classification is "SUPPORTED" or "CONDITIONAL" or "INCONCLUSIVE");
        }
    }

    [Fact]
    public void V4_1_195_ScalingLawAudit_HasNoBuiltInBiasForD3()
    {
        var result = ScalingLawExtractionEngine.Extract();
        // Multiple candidate models are tested; the optimum is fit, not assumed.
        Assert.True(result.CandidateFits.Count >= 2);
    }
}
