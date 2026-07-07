using Xunit;
using TRM.Core.V4_1.Reporting;
using TRM.Core.V4_1.Sync;

namespace TRM.Tests.V4_1;

[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_Reporting")]
public class V4_1_ReportingSummary_Tests
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
    public void V4_1_63_SummaryBuilder_AggregatesByDimensionCorrectly()
    {
        var results = SampleResults();
        var summaries = ScanReportBuilder.BuildDimensionSummaries(results);
        Assert.Equal(3, summaries.Count);
        foreach (var s in summaries)
        {
            Assert.True(s.PointCount > 0);
            Assert.True(s.MeanQuality >= 0 && s.MeanQuality <= 1.0);
            Assert.True(s.SyncedCount + s.MarginalCount + s.UnsyncedCount == s.PointCount);
        }
    }

    [Fact]
    public void V4_1_64_SummaryBuilder_RegimeCounts_AreConsistent()
    {
        var results = SampleResults();
        var summaries = ScanReportBuilder.BuildDimensionSummaries(results);
        int totalSynced = summaries.Sum(s => s.SyncedCount);
        int totalMarginal = summaries.Sum(s => s.MarginalCount);
        int totalUnsynced = summaries.Sum(s => s.UnsyncedCount);
        Assert.Equal(results.Count, totalSynced + totalMarginal + totalUnsynced);
    }
}
