using ScottPlot;

namespace TRM.Core.V4_1.Reporting.Plots;

/// <summary>Builds regime classification grid plots. Classification: FRAMEWORK.</summary>
public static class RegimeGridPlotBuilder
{
    public static Plot BuildRegimeGrid(
        string[][] grid, string[] kLabels, string[] spreadLabels, PlotRequest request)
    {
        var theme = request.Theme ?? new PlotThemeOptions();
        var plot = new Plot();
        plot.Title(request.Title);
        plot.XLabel(request.XLabel);
        plot.YLabel(request.YLabel);

        int rows = grid.Length, cols = rows > 0 ? grid[0].Length : 0;
        var data = new double[rows, cols];
        var palette = new Dictionary<string, double>
        {
            ["synced"] = 2.0, ["marginal"] = 1.0, ["unsynced"] = 0.0
        };
        for (int i = 0; i < rows; i++)
        for (int j = 0; j < cols; j++)
            data[i, j] = palette.GetValueOrDefault(grid[i][j], -1.0);

        var hm = plot.Add.Heatmap(data);
        hm.CellWidth = 1;
        hm.CellHeight = 1;

        double[] kPos = Enumerable.Range(0, kLabels.Length).Select(i => (double)i).ToArray();
        double[] sPos = Enumerable.Range(0, spreadLabels.Length).Select(i => (double)i).ToArray();
        plot.Axes.Bottom.SetTicks(kPos, kLabels);
        plot.Axes.Left.SetTicks(sPos, spreadLabels);
        plot.Axes.SetLimits(-0.5, cols - 0.5, -0.5, rows - 0.5);
        return plot;
    }
}
