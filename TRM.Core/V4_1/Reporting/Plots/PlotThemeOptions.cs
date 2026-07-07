namespace TRM.Core.V4_1.Reporting.Plots;

/// <summary>Deterministic plot theme configuration. Classification: FRAMEWORK.</summary>
public sealed class PlotThemeOptions
{
    public int Width { get; init; } = 600;
    public int Height { get; init; } = 400;
    public string FontFamily { get; init; } = "Arial";
    public float TitleSize { get; init; } = 14;
    public float AxisLabelSize { get; init; } = 12;
    public float TickLabelSize { get; init; } = 10;
}
