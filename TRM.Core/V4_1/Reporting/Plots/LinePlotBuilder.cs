using ScottPlot;

namespace TRM.Core.V4_1.Reporting.Plots;

/// <summary>Builds line plots from summary data. Classification: FRAMEWORK.</summary>
public static class LinePlotBuilder
{
    public static Plot BuildDimensionComparison(
        List<double> dimensions, List<double> meanQualities, PlotRequest request)
    {
        var plot = new Plot();
        plot.Title(request.Title);
        plot.XLabel(request.XLabel);
        plot.YLabel(request.YLabel);

        var scatter = plot.Add.Scatter(dimensions.ToArray(), meanQualities.ToArray());
        scatter.MarkerSize = 8;
        scatter.LineWidth = 2;

        plot.Axes.SetLimits(
            dimensions.Min() - 0.5, dimensions.Max() + 0.5,
            0, 1.05);
        return plot;
    }
}
