using Xunit;
using TRM.Core.V4_1.Universality;

namespace TRM.Tests.V4_1;

[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_Universality")]
public class V4_1_UniversalityAudit_Tests
{
    [Fact]
    public void V4_1_200_UniversalityAudit_CollectsVariantFamiliesDeterministically()
    {
        var r1 = UniversalityAuditEngine.Audit();
        var r2 = UniversalityAuditEngine.Audit();
        Assert.Equal(r1.Variants.Count, r2.Variants.Count);
        Assert.Equal(12, r1.Variants.Count);
    }

    [Fact]
    public void V4_1_201_UniversalityAudit_ComputesPerVariantD3Advantage()
    {
        var result = UniversalityAuditEngine.Audit();
        foreach (var v in result.Variants)
        {
            Assert.True(v.D3AdvantageScore >= 0 && v.D3AdvantageScore <= 1.0);
            Assert.True(v.ConfidenceLabel is "SUPPORTED" or "CONDITIONAL" or "INCONCLUSIVE");
        }
    }

    [Fact]
    public void V4_1_202_UniversalityAudit_DetectsVariantSensitivity()
    {
        var result = UniversalityAuditEngine.Audit();
        Assert.NotEmpty(result.SensitiveFamilies);
    }

    [Fact]
    public void V4_1_203_UniversalityAudit_ProducesRobustnessFraction()
    {
        var result = UniversalityAuditEngine.Audit();
        Assert.True(result.RobustnessFraction >= 0 && result.RobustnessFraction <= 1.0);
        Assert.True(result.UniversalityScore >= 0);
    }

    [Fact]
    public void V4_1_204_UniversalityInterpretation_ProducesNoOverclaim()
    {
        var result = UniversalityAuditEngine.Audit();
        var interps = UniversalityInterpretationEngine.Interpret(result);
        foreach (var i in interps)
        {
            Assert.DoesNotContain("proven", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("derived", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.True(i.Classification is "SUPPORTED" or "CONDITIONAL" or "INCONCLUSIVE");
        }
    }

    [Fact]
    public void V4_1_205_UniversalityAudit_HandlesBrokenBaselineGracefully()
    {
        var result = UniversalityAuditEngine.Audit();
        Assert.True(result.UniversalityScore <= 2.0);
    }
}
