using ScottPlot;

namespace TRM.Core.V4_1.Reporting.Plots;

/// <summary>Plot export service — SVG/PNG. Classification: FRAMEWORK.</summary>
public static class PlotExportService
{
    public static byte[] ExportSvg(Plot plot, int width = 600, int height = 400)
    {
        return System.Text.Encoding.UTF8.GetBytes(plot.GetSvgXml(width, height));
    }

    public static string ExportSvgString(Plot plot, int width = 600, int height = 400)
    {
        return plot.GetSvgXml(width, height);
    }
}
