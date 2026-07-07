using System.Text.Json;
using TRM.Core.V4_1.Reporting.Plots;
using TRM.Core.V4_1.Sync;

namespace TRM.Core.V4_1.Reporting.Figures;

/// <summary>
/// Builds paper-ready figure bundles from validated scan outputs.
/// Classification: FRAMEWORK — deterministic artifact generation, no claims.
/// </summary>
public static class FigureBundleBuilder
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>Build a standard V4.1 figure bundle for a given dimension.</summary>
    public static FigureBundle BuildStandardBundle(
        List<SyncParameterScanResult> results, int dimension, FigureBundleOptions? options = null)
    {
        var opt = options ?? new FigureBundleOptions();
        var bundleId = $"v4_1_{dimension:D2}";
        var prefix = opt.OutputPrefix;

        var panels = new List<FigurePanelSpec>();
        var svgs = new Dictionary<string, byte[]>();
        var captions = new Dictionary<string, FigureCaption>();

        // 1. Sync quality heatmap
        var (qMat, kLabels, sLabels) = ScanReportBuilder.BuildQualityHeatmap(results, dimension);
        var qPlot = HeatmapPlotBuilder.BuildQualityHeatmap(qMat, kLabels, sLabels,
            new PlotRequest { Title = $"Sync Quality D={dimension}", XLabel = "K", YLabel = "spread", Dimension = dimension });
        string qFile = $"{prefix}_sync_quality_D{dimension}.svg";
        panels.Add(new FigurePanelSpec { PanelId = "sync_quality", Filename = qFile, Dimension = dimension,
            ShortCaption = $"Sync-quality heatmap D={dimension}.", LongCaption = $"Deterministic sync-quality heatmap across (K, spread) for D = {dimension}." });
        svgs[qFile] = PlotExportService.ExportSvg(qPlot, opt.SvgWidth, opt.SvgHeight);
        captions["sync_quality"] = new FigureCaption { Short = panels[^1].ShortCaption, Long = panels[^1].LongCaption };

        // 2. Regime grid
        var (rGrid, _, _) = ScanReportBuilder.BuildRegimeGrid(results, dimension);
        var rPlot = RegimeGridPlotBuilder.BuildRegimeGrid(rGrid, kLabels, sLabels,
            new PlotRequest { Title = $"Regime D={dimension}", XLabel = "K", YLabel = "spread", Dimension = dimension });
        string rFile = $"{prefix}_regime_grid_D{dimension}.svg";
        panels.Add(new FigurePanelSpec { PanelId = "regime_grid", Filename = rFile, Dimension = dimension,
            ShortCaption = $"Regime classification D={dimension}.", LongCaption = $"Regime classification grid computed from validated scan outputs; no new claims added." });
        svgs[rFile] = PlotExportService.ExportSvg(rPlot, opt.SvgWidth, opt.SvgHeight);
        captions["regime_grid"] = new FigureCaption { Short = panels[^1].ShortCaption, Long = panels[^1].LongCaption };

        var manifest = new FigureManifest { BundleId = bundleId, Panels = panels };
        return new FigureBundle { BundleId = bundleId, Panels = panels, Manifest = manifest, SvgArtifacts = svgs, Captions = captions };
    }

    /// <summary>Build a dimension-comparison bundle.</summary>
    public static FigureBundle BuildComparisonBundle(
        Dictionary<int, List<SyncParameterScanResult>> resultsByDim, FigureBundleOptions? options = null)
    {
        var opt = options ?? new FigureBundleOptions();
        var dims = resultsByDim.Keys.OrderBy(d => d).ToList();
        var qualities = dims.Select(d =>
        {
            var summaries = ScanReportBuilder.BuildDimensionSummaries(resultsByDim[d]);
            return summaries.FirstOrDefault()?.MeanQuality ?? 0;
        }).ToList();

        var plot = LinePlotBuilder.BuildDimensionComparison(
            dims.Select(d => (double)d).ToList(), qualities,
            new PlotRequest { Title = "Dimension Comparison", XLabel = "D", YLabel = "Mean Quality" });

        string file = $"{opt.OutputPrefix}_dimension_compare.svg";
        var panels = new List<FigurePanelSpec>
        {
            new() { PanelId = "dim_compare", Filename = file, Dimension = 0,
                ShortCaption = "Per-dimension sync-quality comparison.",
                LongCaption = "Mean sync quality across dimensions computed from validated scan outputs." }
        };
        var svgs = new Dictionary<string, byte[]> { [file] = PlotExportService.ExportSvg(plot, opt.SvgWidth, opt.SvgHeight) };
        var captions = new Dictionary<string, FigureCaption>
        {
            ["dim_compare"] = new() { Short = panels[0].ShortCaption, Long = panels[0].LongCaption }
        };
        var manifest = new FigureManifest { BundleId = "v4_1_dim_compare", Panels = panels };
        return new FigureBundle { BundleId = "v4_1_dim_compare", Panels = panels, Manifest = manifest, SvgArtifacts = svgs, Captions = captions };
    }

    /// <summary>Serialize manifest to JSON bytes.</summary>
    public static byte[] SerializeManifest(FigureManifest manifest) =>
        System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(manifest, JsonOpts));
}
