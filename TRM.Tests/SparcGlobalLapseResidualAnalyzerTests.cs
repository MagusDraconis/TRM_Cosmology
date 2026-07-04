using TRM.CMD;

namespace TRM.Tests;

#region SPARC Global Lapse Tests

public class SparcGlobalLapseResidualAnalyzerTests
{
    // WARNING: These tests require SPARC data files.
    // If data is not available, tests will throw InvalidOperationException
    // with a clear message about missing files.

    [Fact]
    public void SPARC01_Should_Not_Require_UniversalOffset()
    {
        var result = SparcGlobalLapseResidualAnalyzer.Run();

        // eta0 should be statistically indistinguishable from zero
        Assert.True(Math.Abs(result.Eta0) < result.Eta0Uncertainty);

        // no meaningful RMS or chi-square improvement
        Assert.False(result.OffsetImprovesFitSignificantly);
    }

    [Fact]
    public void SPARC02_Should_Not_Show_Significant_PhiCorrelation_Linear()
    {
        var result = SparcGlobalLapseResidualAnalyzer.Run();

        // linear phi_proxy correlation must not be significant
        Assert.True(Math.Abs(result.Eta1Linear) < result.Eta1LinearUncertainty);
        Assert.True(result.PhiCorrelationLinearPValue > 0.05);
    }

    [Fact]
    public void SPARC03_Should_Not_Show_Significant_PhiCorrelation_Log()
    {
        var result = SparcGlobalLapseResidualAnalyzer.Run();

        // log(phi_proxy) correlation must also be insignificant
        Assert.True(Math.Abs(result.Eta1Log) < result.Eta1LogUncertainty);
        Assert.True(result.PhiCorrelationLogPValue > 0.05);
    }

    [Fact]
    public void SPARC04_Should_Not_Require_GlobalLambda()
    {
        var result = SparcGlobalLapseResidualAnalyzer.Run();

        // lambda should be consistent with 1
        Assert.True(Math.Abs(result.Lambda - 1.0) < result.LambdaUncertainty);

        // no meaningful improvement in RMS
        Assert.False(result.LambdaImprovesFitSignificantly);
    }

    [Fact]
    public void SPARC05_Effect_Should_Not_Be_Driven_By_Subsets()
    {
        var result = SparcGlobalLapseResidualAnalyzer.Run();

        // must be robust across galaxy subsets
        Assert.True(result.IsRobustAcrossSubsamples);

        // must not be dominated by a single galaxy
        Assert.False(result.DrivenBySingleGalaxy);
    }

    [Fact]
    public void SPARC06_Should_Not_Show_Universal_BLike_Signal()
    {
        var result = SparcGlobalLapseResidualAnalyzer.Run();

        // final verdict: no evidence for global B-like term
        Assert.False(result.HasEvidenceForGlobalTerm);
    }
}

#endregion
