using Xunit;
using TRM.Core.V4_1.Reduction;

namespace TRM.Tests.V4_1;

[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_CanonicalForm")]
public class V4_1_CanonicalFormAudit_Tests
{
    [Fact]
    public void V4_1_260_CanonicalFormAudit_IsDeterministicAcrossRuns()
    {
        var r1 = CanonicalFormEngine.Audit();
        var r2 = CanonicalFormEngine.Audit();
        Assert.Equal(r1.Preferred.Form.Name, r2.Preferred.Form.Name);
    }

    [Fact]
    public void V4_1_261_CandidateCanonicalForms_ProduceFiniteFitMetrics()
    {
        var result = CanonicalFormEngine.Audit();
        Assert.True(result.Candidates.Count >= 3);
        foreach (var c in result.Candidates)
        {
            Assert.True(double.IsFinite(c.Rmse));
            Assert.True(double.IsFinite(c.RSquared));
        }
    }

    [Fact]
    public void V4_1_262_BlockAssignment_IsComputedWithoutBias()
    {
        var result = CanonicalFormEngine.Audit();
        foreach (var c in result.Candidates)
            Assert.NotEmpty(c.Form.Blocks);
    }

    [Fact]
    public void V4_1_263_PreferredFormSelection_IsDeterministic()
    {
        var r1 = CanonicalFormEngine.Audit();
        var r2 = CanonicalFormEngine.Audit();
        Assert.Equal(r1.Preferred.Status, r2.Preferred.Status);
    }

    [Fact]
    public void V4_1_264_CanonicalFormInterpretation_ProducesNoOverclaim()
    {
        var result = CanonicalFormEngine.Audit();
        var interps = CanonicalFormInterpretationEngine.Interpret(result);
        foreach (var i in interps)
        {
            Assert.DoesNotContain("proven", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("derived", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.True(i.Classification is "SUPPORTED" or "CONDITIONAL" or "INCONCLUSIVE");
        }
    }

    [Fact]
    public void V4_1_265_CanonicalFormAudit_HandlesPoorCandidateGracefully()
    {
        var result = CanonicalFormEngine.Audit();
        Assert.True(result.Candidates.All(c => c.Status is "PREFERRED" or "VIABLE" or "OVER-COMPLICATED" or "INSUFFICIENT"));
    }
}
