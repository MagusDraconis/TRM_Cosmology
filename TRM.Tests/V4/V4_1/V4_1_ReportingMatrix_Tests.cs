using Xunit;
using TRM.Core.V4_1.Reporting;
using TRM.Core.V4_1.Sync;

namespace TRM.Tests.V4_1;

[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_Reporting")]
public class V4_1_ReportingMatrix_Tests
{
    private static List<SyncParameterScanResult> SampleResults()
    {
        var cfg = new SyncParameterScanConfig
        {
            Dimensions = [3],
            CouplingStrengths = [0.5, 1.0, 2.0],
            FrequencySpreads = [0.0, 0.1, 0.5],
            Dt = 0.05, Steps = 200, SeedsPerPoint = 2, GraphSizePerDim = 3
        };
        return SyncParameterScanner.Scan(cfg);
    }

    [Fact]
    public void V4_1_65_MatrixBuilder_ProducesExpectedGridShape()
    {
        var results = SampleResults();
        var (matrix, kLabels, spreadLabels) = ScanReportBuilder.BuildQualityHeatmap(results, 3);
        Assert.Equal(3, matrix.Length);          // 3 K values
        Assert.Equal(3, matrix[0].Length);        // 3 spread values
        Assert.Equal(3, kLabels.Length);
        Assert.Equal(3, spreadLabels.Length);
    }

    [Fact]
    public void V4_1_66_MatrixBuilder_MissingCells_AreHandledDeterministically()
    {
        var results = SampleResults();
        // Query a dimension not in the scan — should produce empty matrix.
        var (matrix, kLabels, spreadLabels) = ScanReportBuilder.BuildQualityHeatmap(results, 99);
        Assert.Empty(matrix);
        Assert.Empty(kLabels);
        Assert.Empty(spreadLabels);
    }

    [Fact]
    public void V4_1_68_MatrixBuilder_RegimeGrid_AllCellsPopulated()
    {
        var results = SampleResults();
        var (grid, kLabels, spreadLabels) = ScanReportBuilder.BuildRegimeGrid(results, 3);
        for (int i = 0; i < grid.Length; i++)
        for (int j = 0; j < grid[i].Length; j++)
            Assert.NotEqual("missing", grid[i][j]);

        // All regimes should be valid.
        var validRegimes = new[] { "synced", "marginal", "unsynced" };
        for (int i = 0; i < grid.Length; i++)
        for (int j = 0; j < grid[i].Length; j++)
            Assert.Contains(grid[i][j], validRegimes);
    }
}
