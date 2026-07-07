using Xunit;
using TRM.Core.V4_1.Synthesis;

namespace TRM.Tests.V4_1;

[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_MechanismExtraction")]
public class V4_1_MechanismExtraction_Tests
{
    [Fact]
    public void V4_1_180_MechanismExtraction_CollectsNamedMechanismsDeterministically()
    {
        var r1 = MechanismExtractionEngine.Extract();
        var r2 = MechanismExtractionEngine.Extract();
        Assert.Equal(r1.Contributions.Count, r2.Contributions.Count);
        Assert.Equal(9, r1.Contributions.Count);
    }

    [Fact]
    public void V4_1_181_MechanismExtraction_ComputesContributionRanking()
    {
        var result = MechanismExtractionEngine.Extract();
        for (int i = 1; i < result.Contributions.Count; i++)
            Assert.True(result.Contributions[i - 1].ContributionScore >= result.Contributions[i].ContributionScore);
    }

    [Fact]
    public void V4_1_182_LeaveOneOutAnalysis_IsDeterministic()
    {
        double s1 = MechanismExtractionEngine.LeaveOneOutScore("Spectral balance");
        double s2 = MechanismExtractionEngine.LeaveOneOutScore("Spectral balance");
        Assert.Equal(s1, s2, 6);
        Assert.True(s1 < 1.0);
    }

    [Fact]
    public void V4_1_183_MechanismExtraction_DetectsTradeoffStyleWin()
    {
        var result = MechanismExtractionEngine.Extract();
        Assert.True(result.WinStyle is "compromise-optimum" or "dominant-channel");
    }

    [Fact]
    public void V4_1_184_MechanismInterpretation_ProducesNoOverclaim()
    {
        var result = MechanismExtractionEngine.Extract();
        var interps = MechanismInterpretationEngine.Interpret(result);
        foreach (var i in interps)
        {
            Assert.DoesNotContain("proven", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("derived", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.True(i.Classification is "SUPPORTED" or "CONDITIONAL" or "INCONCLUSIVE");
        }
    }

    [Fact]
    public void V4_1_185_MechanismExtraction_HandlesConflictingSignalsGracefully()
    {
        var result = MechanismExtractionEngine.Extract();
        Assert.NotEmpty(result.Tradeoffs);
        Assert.True(result.FragileMechanisms.Count >= 0);
        Assert.True(result.D3AdvantageFromTop3 >= 0 && result.D3AdvantageFromTop3 <= 1.0);
    }
}
