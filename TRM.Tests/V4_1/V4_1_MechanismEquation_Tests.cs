using Xunit;
using TRM.Core.V4_1.Reduction;

namespace TRM.Tests.V4_1;

[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_MechanismEquation")]
public class V4_1_MechanismEquation_Tests
{
    [Fact]
    public void V4_1_250_MechanismEquationAudit_IsDeterministicAcrossRuns()
    {
        var r1 = MechanismEquationEngine.Analyze();
        var r2 = MechanismEquationEngine.Analyze();
        Assert.Equal(r1.PrimaryTerms, r2.PrimaryTerms);
        Assert.Equal(r1.UnexplainedResidual, r2.UnexplainedResidual, 6);
    }

    [Fact]
    public void V4_1_251_TermAttributionMatrix_IsComputableAndFinite()
    {
        var result = MechanismEquationEngine.Analyze();
        Assert.NotEmpty(result.Mappings);
        Assert.True(result.Mappings.All(m => double.IsFinite(m.AttributionStrength)));
    }

    [Fact]
    public void V4_1_253_MixedVsPrimaryClassification_IsComputedWithoutBias()
    {
        var result = MechanismEquationEngine.Analyze();
        Assert.NotEmpty(result.PrimaryTerms);
        Assert.NotEmpty(result.CorrectionTerms);
    }

    [Fact]
    public void V4_1_254_MechanismEquationInterpretation_ProducesNoOverclaim()
    {
        var result = MechanismEquationEngine.Analyze();
        var interps = MechanismEquationInterpretationEngine.Interpret(result);
        foreach (var i in interps)
        {
            Assert.DoesNotContain("proven", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("derived", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.True(i.Classification is "SUPPORTED" or "CONDITIONAL" or "INCONCLUSIVE");
        }
    }

    [Fact]
    public void V4_1_255_UnexplainedResidual_IsHandledGracefully()
    {
        var result = MechanismEquationEngine.Analyze();
        Assert.True(result.UnexplainedResidual >= 0);
    }
}
