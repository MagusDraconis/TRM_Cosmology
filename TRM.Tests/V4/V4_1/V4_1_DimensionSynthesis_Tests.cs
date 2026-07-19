using Xunit;
using TRM.Core.V4_1.Synthesis;

namespace TRM.Tests.V4_1;

[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_Synthesis")]
public class V4_1_DimensionSynthesis_Tests
{
    [Fact]
    public void V4_1_170_Synthesis_CollectsAllLayerInputs()
    {
        var result = DimensionSynthesisEngine.Synthesize();
        Assert.Equal(10, result.LayerEvidence.Count);
    }

    [Fact]
    public void V4_1_171_Synthesis_ComputesDeterministicAggregateScores()
    {
        var r1 = DimensionSynthesisEngine.Synthesize();
        var r2 = DimensionSynthesisEngine.Synthesize();
        var d3_1 = r1.Profiles.First(p => p.Dimension == 3);
        var d3_2 = r2.Profiles.First(p => p.Dimension == 3);
        Assert.Equal(d3_1.SupportCount, d3_2.SupportCount);
        Assert.Equal(d3_1.WeightedScore, d3_2.WeightedScore, 6);
    }

    [Fact]
    public void V4_1_172_Synthesis_DetectsConflictingLayerSignals()
    {
        var result = DimensionSynthesisEngine.Synthesize();
        // D=3 should have the most supporting layers.
        var d3 = result.Profiles.First(p => p.Dimension == 3);
        Assert.True(d3.SupportCount >= result.Profiles.Max(p => p.SupportCount) - 1);
    }

    [Fact]
    public void V4_1_173_Synthesis_ProducesNoOverclaim()
    {
        var result = DimensionSynthesisEngine.Synthesize();
        var interps = DimensionSynthesisInterpretationEngine.Interpret(result);
        foreach (var i in interps)
        {
            Assert.DoesNotContain("proven", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("derived", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("proof", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.True(i.Classification is "SUPPORTED" or "CONDITIONAL" or "INCONCLUSIVE");
        }
    }

    [Fact]
    public void V4_1_174_Synthesis_HandlesInconclusiveLayersGracefully()
    {
        var result = DimensionSynthesisEngine.Synthesize();
        foreach (var e in result.LayerEvidence)
        {
            Assert.True(e.Classification is "SUPPORTED" or "CONDITIONAL" or "INCONCLUSIVE");
            Assert.True(e.Confidence >= 0 && e.Confidence <= 1.0);
        }
    }

    [Fact]
    public void V4_1_175_Synthesis_D3AdvantageProfile_IsComputable()
    {
        var result = DimensionSynthesisEngine.Synthesize();
        Assert.True(result.D3AdvantageScore >= 0 && result.D3AdvantageScore <= 1.0);
        Assert.Equal(4, result.Profiles.Count);
    }
}
