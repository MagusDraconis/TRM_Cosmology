using Xunit;
using TRM.Core.V4_1.Phase;

namespace TRM.Tests.V4_1;

[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_Phase")]
public class V4_1_PhaseStructure_Tests
{
    [Fact]
    public void V4_1_190_PhaseStructure_IsDeterministic()
    {
        var r1 = PhaseStructureEngine.BuildPhaseStructure();
        var r2 = PhaseStructureEngine.BuildPhaseStructure();
        Assert.Equal(r1.D3CoverageFraction, r2.D3CoverageFraction, 6);
        Assert.Equal(r1.Winners.Count, r2.Winners.Count);
    }

    [Fact]
    public void V4_1_191_PhaseStructure_HasNoBuiltInBiasForD3()
    {
        var result = PhaseStructureEngine.BuildPhaseStructure();
        // Multiple dimensions appear as winners.
        var winnerDims = result.Winners.Select(w => w.WinnerDimension).Distinct().ToList();
        Assert.True(winnerDims.Count >= 2, "At least two dimensions should appear as winners.");
    }

    [Fact]
    public void V4_1_192_WinnerDetection_ProducesValidConfidenceLabels()
    {
        var result = PhaseStructureEngine.BuildPhaseStructure();
        foreach (var w in result.Winners)
            Assert.True(w.ConfidenceLabel is "SUPPORTED" or "CONDITIONAL" or "INCONCLUSIVE");
    }

    [Fact]
    public void V4_1_193_PhaseRegions_AreComputable()
    {
        var result = PhaseStructureEngine.BuildPhaseStructure();
        Assert.NotEmpty(result.Regions);
        double totalFraction = result.Regions.Sum(r => r.FractionOfTotal);
        Assert.True(totalFraction > 0.5);
    }

    [Fact]
    public void V4_1_194_PhaseInterpretation_ProducesNoOverclaim()
    {
        var result = PhaseStructureEngine.BuildPhaseStructure();
        var interps = PhaseInterpretationEngine.Interpret(result);
        foreach (var i in interps)
        {
            Assert.DoesNotContain("proven", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("derived", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.True(i.Classification is "SUPPORTED" or "CONDITIONAL" or "INCONCLUSIVE");
        }
    }
}
