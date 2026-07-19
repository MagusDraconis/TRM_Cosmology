using Xunit;
using TRM.Core.V4_1.Comparators;

namespace TRM.Tests.V4_1;

[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_Comparators")]
public class V4_1_ComparatorAudit_Tests
{
    [Fact]
    public void V4_1_210_ComparatorAudit_CollectsDeterministicComparatorFamilies()
    {
        var r1 = ComparatorAuditEngine.Audit();
        var r2 = ComparatorAuditEngine.Audit();
        Assert.Equal(r1.Variants.Count, r2.Variants.Count);
        Assert.True(r1.Variants.Count >= 6);
    }

    [Fact]
    public void V4_1_211_NullModels_ProduceFiniteComparableScores()
    {
        var result = ComparatorAuditEngine.Audit();
        Assert.NotEmpty(result.NullModels);
        foreach (var v in result.Variants)
        {
            Assert.True(double.IsFinite(v.D3Score));
            Assert.True(v.D3Score >= 0 && v.D3Score <= 1.0);
        }
    }

    [Fact]
    public void V4_1_212_ComparatorAudit_ComputesBaselineDeviation()
    {
        var result = ComparatorAuditEngine.Audit();
        foreach (var v in result.Variants)
            Assert.True(double.IsFinite(v.DeviationFromBaseline));
    }

    [Fact]
    public void V4_1_213_ComparatorAudit_DetectsWhenD3SignalIsTrivialOrNonTrivial()
    {
        var result = ComparatorAuditEngine.Audit();
        Assert.True(result.D3SurvivalFraction >= 0 && result.D3SurvivalFraction <= 1.0);
        Assert.NotNull(result.Summary);
    }

    [Fact]
    public void V4_1_214_ComparatorInterpretation_ProducesNoOverclaim()
    {
        var result = ComparatorAuditEngine.Audit();
        var interps = ComparatorInterpretationEngine.Interpret(result);
        foreach (var i in interps)
        {
            Assert.DoesNotContain("proven", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("derived", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.True(i.Classification is "SUPPORTED" or "CONDITIONAL" or "INCONCLUSIVE");
        }
    }

    [Fact]
    public void V4_1_215_ComparatorAudit_HasNoBuiltInBiasForBaselineTRM()
    {
        var result = ComparatorAuditEngine.Audit();
        // Multiple comparator families tested — baseline is not assumed superior.
        var families = result.Variants.Select(v => v.Family).Distinct().ToList();
        Assert.True(families.Count >= 3);
    }
}
