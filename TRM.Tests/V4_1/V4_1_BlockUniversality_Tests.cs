using Xunit;
using TRM.Core.V4_1.Reduction;

namespace TRM.Tests.V4_1;

[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_BlockUniversality")]
public class V4_1_BlockUniversality_Tests
{
    [Fact]
    public void V4_1_270_BlockUniversality_IsDeterministicAcrossRuns()
    {
        var r1 = BlockUniversalityEngine.Audit();
        var r2 = BlockUniversalityEngine.Audit();
        Assert.Equal(r1.PreferredFormOverall, r2.PreferredFormOverall);
        Assert.Equal(r1.RobustnessClass, r2.RobustnessClass);
    }

    [Fact]
    public void V4_1_271_CanonicalForms_AreComparedAcrossVariants()
    {
        var result = BlockUniversalityEngine.Audit();
        Assert.Equal(9, result.Variants.Count);
    }

    [Fact]
    public void V4_1_272_BlockWeightStability_IsComputable()
    {
        var result = BlockUniversalityEngine.Audit();
        Assert.True(double.IsFinite(result.Stability.CoreWeightVariance));
        Assert.True(double.IsFinite(result.Stability.CorrectionWeightVariance));
    }

    [Fact]
    public void V4_1_273_PreferredFormFrequency_IsDeterministic()
    {
        var r1 = BlockUniversalityEngine.Audit();
        var r2 = BlockUniversalityEngine.Audit();
        int cc1 = r1.FormFrequency.GetValueOrDefault("Core+Correction");
        int cc2 = r2.FormFrequency.GetValueOrDefault("Core+Correction");
        Assert.Equal(cc1, cc2);
    }

    [Fact]
    public void V4_1_274_BlockUniversalityInterpretation_ProducesNoOverclaim()
    {
        var result = BlockUniversalityEngine.Audit();
        var interps = BlockUniversalityInterpretationEngine.Interpret(result);
        foreach (var i in interps)
        {
            Assert.DoesNotContain("proven", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("derived", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.True(i.Classification is "SUPPORTED" or "CONDITIONAL" or "INCONCLUSIVE");
        }
    }

    [Fact]
    public void V4_1_275_BlockUniversality_HandlesCompetingFormsGracefully()
    {
        var result = BlockUniversalityEngine.Audit();
        Assert.True(result.RobustnessClass is "ROBUST" or "WEAKLY-STABLE" or "VARIANT-DEPENDENT");
    }
}
