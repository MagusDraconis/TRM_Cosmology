using Xunit;
using TRM.Core.V4_1.Analysis;
using TRM.Core.V4_1.Sync;

namespace TRM.Tests.V4_1;

[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_Analysis")]
public class V4_1_Analysis_Tests
{
    private static List<SyncParameterScanResult> SampleResults()
    {
        var cfg = new SyncParameterScanConfig
        {
            Dimensions = [1, 2, 3],
            CouplingStrengths = [0.5, 1.0],
            FrequencySpreads = [0.0, 0.1],
            Dt = 0.05, Steps = 200, SeedsPerPoint = 2, GraphSizePerDim = 3
        };
        return SyncParameterScanner.Scan(cfg);
    }

    [Fact]
    public void V4_1_90_Analysis_ComputesPerDimensionAverages()
    {
        var results = SampleResults();
        var analysis = DimensionAnalysisEngine.Analyze(results);
        Assert.Equal(3, analysis.Count);
        foreach (var a in analysis)
        {
            Assert.True(a.MeanQuality >= 0 && a.MeanQuality <= 1.0);
            Assert.True(a.TotalPoints > 0);
            Assert.Equal(a.TotalPoints, a.SyncedCount + a.MarginalCount + a.UnsyncedCount);
        }
    }

    [Fact]
    public void V4_1_91_Analysis_DetectsBestDimensionInBaselineScenario()
    {
        var results = SampleResults();
        var analysis = DimensionAnalysisEngine.Analyze(results);
        var conclusions = DimensionAnalysisEngine.GenerateConclusions(analysis);
        Assert.NotEmpty(conclusions);
        // At least one statement must identify a best dimension.
        Assert.Contains(conclusions, s => s.Metrics.ContainsKey("best_D"));
    }

    [Fact]
    public void V4_1_92_Analysis_ProducesClassificationLabels()
    {
        var results = SampleResults();
        var analysis = DimensionAnalysisEngine.Analyze(results);
        var conclusions = DimensionAnalysisEngine.GenerateConclusions(analysis);
        foreach (var s in conclusions)
            Assert.True(s.Confidence is AnalysisConfidence.Supported
                or AnalysisConfidence.Conditional
                or AnalysisConfidence.Inconclusive);
    }

    [Fact]
    public void V4_1_93_Analysis_DoesNotOverclaimResults()
    {
        var results = SampleResults();
        var analysis = DimensionAnalysisEngine.Analyze(results);
        var conclusions = DimensionAnalysisEngine.GenerateConclusions(analysis);
        foreach (var s in conclusions)
        {
            Assert.DoesNotContain("proven", s.Text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("derived", s.Text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("established", s.Text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void V4_1_94_Analysis_HandlesFlatOrAmbiguousData()
    {
        // Synthetic flat data: all dimensions identical.
        var flat = new List<SyncParameterScanResult>();
        for (int D = 1; D <= 3; D++)
        {
            var pt = new SyncParameterPoint(D, 0.5, 0.1, 0.05, 200, 42);
            flat.AddRange(Enumerable.Range(0, 4).Select(_ => new SyncParameterScanResult(
                pt, 0.5, 0.5, 0.1, 0.5, false, "marginal")));
        }
        var analysis = DimensionAnalysisEngine.Analyze(flat);
        var conclusions = DimensionAnalysisEngine.GenerateConclusions(analysis);
        Assert.Contains(conclusions, s => s.Confidence == AnalysisConfidence.Inconclusive
            && s.Text.Contains("flat"));
    }

    [Fact]
    public void V4_1_95_Analysis_IsDeterministicAcrossRuns()
    {
        var r1 = SampleResults();
        var r2 = SampleResults();
        var a1 = DimensionAnalysisEngine.Analyze(r1);
        var a2 = DimensionAnalysisEngine.Analyze(r2);
        Assert.Equal(a1.Count, a2.Count);
        for (int i = 0; i < a1.Count; i++)
        {
            Assert.Equal(a1[i].MeanQuality, a2[i].MeanQuality, 6);
            Assert.Equal(a1[i].Dimension, a2[i].Dimension);
        }
    }
}
