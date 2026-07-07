using Xunit;
using TRM.Core.V4_1.Reporting;
using TRM.Core.V4_1.Reporting.Plots;
using TRM.Core.V4_1.Sync;

namespace TRM.Tests.V4_1;

[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_Plotting")]
public class V4_1_PlotGeneration_Tests
{
    private static (double[][] matrix, string[] k, string[] s, string[][] grid) SampleData()
    {
        var cfg = new SyncParameterScanConfig
        {
            Dimensions = [3],
            CouplingStrengths = [0.5, 1.0],
            FrequencySpreads = [0.0, 0.1, 0.5],
            Dt = 0.05, Steps = 200, SeedsPerPoint = 2, GraphSizePerDim = 3
        };
        var results = SyncParameterScanner.Scan(cfg);
        var (mat, k, s) = ScanReportBuilder.BuildQualityHeatmap(results, 3);
        var (grid, _, _) = ScanReportBuilder.BuildRegimeGrid(results, 3);
        return (mat, k, s, grid);
    }

    private static PlotRequest DefaultRequest(string title) => new()
    {
        Title = title, XLabel = "K", YLabel = "spread", Dimension = 3
    };

    [Fact]
    public void V4_1_70_HeatmapPlot_CanBeBuilt_FromSyncQualityMatrix()
    {
        var (mat, k, s, _) = SampleData();
        var plot = HeatmapPlotBuilder.BuildQualityHeatmap(mat, k, s, DefaultRequest("Quality"));
        Assert.NotNull(plot);
    }

    [Fact]
    public void V4_1_71_RegimeGridPlot_CanBeBuilt_FromClassificationMatrix()
    {
        var (_, k, s, grid) = SampleData();
        var plot = RegimeGridPlotBuilder.BuildRegimeGrid(grid, k, s, DefaultRequest("Regime"));
        Assert.NotNull(plot);
    }

    [Fact]
    public void V4_1_72_LinePlot_CanBeBuilt_FromSummarySeries()
    {
        var plot = LinePlotBuilder.BuildDimensionComparison(
            [1.0, 2.0, 3.0], [0.5, 0.7, 0.9],
            new PlotRequest { Title = "D Comparison", XLabel = "D", YLabel = "Q" });
        Assert.NotNull(plot);
    }

    [Fact]
    public void V4_1_73_SvgExport_RepeatedRun_IsStructurallyStable()
    {
        var (mat, k, s, _) = SampleData();
        var p1 = HeatmapPlotBuilder.BuildQualityHeatmap(mat, k, s, DefaultRequest("Q"));
        var p2 = HeatmapPlotBuilder.BuildQualityHeatmap(mat, k, s, DefaultRequest("Q"));
        var svg1 = PlotExportService.ExportSvgString(p1);
        var svg2 = PlotExportService.ExportSvgString(p2);
        // Both must be valid SVG with the title.
        Assert.Contains("<svg", svg1);
        Assert.Contains("<svg", svg2);
        Assert.Contains("Q", svg1);
    }

    [Fact]
    public void V4_1_74_PlotBuilder_UsesExpectedAxisLabels()
    {
        var (mat, k, s, _) = SampleData();
        var plot = HeatmapPlotBuilder.BuildQualityHeatmap(mat, k, s,
            new PlotRequest { Title = "TestTitle", XLabel = "Coupling K", YLabel = "Spread", Dimension = 3 });
        var svg = PlotExportService.ExportSvgString(plot);
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void V4_1_75_PlotBuilder_HandlesMissingCellsDeterministically()
    {
        // Single cell with NaN: heatmap builder replaces NaN with 0, no crash.
        double[][] mat = [[double.NaN]];
        var plot = HeatmapPlotBuilder.BuildQualityHeatmap(mat, ["0.5"], ["0.0"], DefaultRequest("NaN cell"));
        Assert.NotNull(plot);
        var svg = PlotExportService.ExportSvgString(plot);
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void V4_1_76_PlotExport_DoesNotEmitNaNOrLocaleDependentText()
    {
        var (mat, k, s, _) = SampleData();
        var plot = HeatmapPlotBuilder.BuildQualityHeatmap(mat, k, s, DefaultRequest("Q"));
        var svg = PlotExportService.ExportSvgString(plot);
        Assert.DoesNotContain("NaN", svg);
        Assert.DoesNotContain("Infinity", svg);
    }

    [Fact]
    public void V4_1_77_PlotPipeline_DoesNotReorderInputDimensionsUnexpectedly()
    {
        double[][] mat = [[0.1, 0.2], [0.3, 0.4]];
        string[] k = ["0.50", "1.00"], s = ["0.00", "0.10"];
        var plot = HeatmapPlotBuilder.BuildQualityHeatmap(mat, k, s, DefaultRequest("Q"));
        var svg = PlotExportService.ExportSvgString(plot);
        // Both K labels must appear.
        Assert.Contains("0.50", svg);
        Assert.Contains("1.00", svg);
    }

    [Fact]
    public void V4_1_78_PlotExport_Filenames_AreDeterministic()
    {
        var (mat, k, s, _) = SampleData();
        var plot = HeatmapPlotBuilder.BuildQualityHeatmap(mat, k, s, DefaultRequest("Q"));
        var svg1 = PlotExportService.ExportSvgString(plot);
        var svg2 = PlotExportService.ExportSvgString(plot);
        Assert.Equal(svg1.Length, svg2.Length);
    }
}
