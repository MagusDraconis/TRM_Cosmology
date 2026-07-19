using Xunit;
using TRM.Core.V4_1.Reduction;

namespace TRM.Tests.V4_1;

[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_Reduction")]
public class V4_1_ReductionAudit_Tests
{
    [Fact]
    public void V4_1_220_Reduction_EvaluatesAllSubsetsDeterministically()
    {
        var r1 = ReductionEngine.Evaluate();
        var r2 = ReductionEngine.Evaluate();
        Assert.Equal(r1.AllEvaluations.Count, r2.AllEvaluations.Count);
    }

    [Fact]
    public void V4_1_221_Reduction_DetectsMinimalCores()
    {
        var result = ReductionEngine.Evaluate();
        Assert.NotEmpty(result.MinimalCores);
    }

    [Fact]
    public void V4_1_222_Reduction_IdentifiesCriticalMechanisms()
    {
        var result = ReductionEngine.Evaluate();
        Assert.NotEmpty(result.CriticalMechanisms);
    }

    [Fact]
    public void V4_1_223_Reduction_SingleMechanismsEvaluated()
    {
        var result = ReductionEngine.Evaluate();
        var singles = result.AllEvaluations.Where(e => e.Subset.Mechanisms.Count == 1).ToList();
        Assert.True(singles.Count >= 3);
    }

    [Fact]
    public void V4_1_224_ReductionInterpretation_ProducesNoOverclaim()
    {
        var result = ReductionEngine.Evaluate();
        var interps = ReductionInterpretationEngine.Interpret(result);
        foreach (var i in interps)
        {
            Assert.DoesNotContain("proven", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("derived", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.True(i.Classification is "SUPPORTED" or "CONDITIONAL" or "INCONCLUSIVE");
        }
    }

    [Fact]
    public void V4_1_225_Reduction_HasNoBuiltInBiasForD3()
    {
        var result = ReductionEngine.Evaluate();
        // Some subsets should NOT prefer D=3.
        Assert.Contains(result.AllEvaluations, e => e.PreferredDim != 3);
    }
}
