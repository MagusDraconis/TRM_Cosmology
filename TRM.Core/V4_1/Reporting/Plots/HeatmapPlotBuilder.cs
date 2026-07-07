using System.Globalization;
using ScottPlot;

namespace TRM.Core.V4_1.Reporting.Plots;

/// <summary>
/// Builds deterministic ScottPlot figures from V4.1 reporting matrices.
/// Classification: FRAMEWORK — plotting infrastructure, no claims.
/// </summary>
public static class HeatmapPlotBuilder
{
    public static Plot BuildQualityHeatmap(
        double[][] matrix, string[] kLabels, string[] spreadLabels, PlotRequest request)
    {
        var theme = request.Theme ?? new PlotThemeOptions();
        var plot = new Plot();
        plot.Title(request.Title);
        plot.XLabel(request.XLabel);
        plot.YLabel(request.YLabel);

        // Convert to double[,] for ScottPlot
        int rows = matrix.Length, cols = matrix.Length > 0 ? matrix[0].Length : 0;
        var data = new double[rows, cols];
        for (int i = 0; i < rows; i++)
        for (int j = 0; j < cols; j++)
            data[i, j] = double.IsNaN(matrix[i][j]) ? 0 : matrix[i][j];

        var hm = plot.Add.Heatmap(data);
        hm.CellWidth = 1;
        hm.CellHeight = 1;

        // Tick labels from K and spread values.
        double[] kPositions = Enumerable.Range(0, kLabels.Length).Select(i => (double)i).ToArray();
        double[] spreadPositions = Enumerable.Range(0, spreadLabels.Length).Select(i => (double)i).ToArray();
        plot.Axes.Bottom.SetTicks(kPositions, kLabels.Select(s => s).ToArray());
        plot.Axes.Left.SetTicks(spreadPositions, spreadLabels.Select(s => s).ToArray());

        plot.Axes.SetLimits(-0.5, cols - 0.5, -0.5, rows - 0.5);
        return plot;
    }
}
