namespace TRM.Core.V4_1.Reporting.Plots;

/// <summary>Describes a plot request. Classification: FRAMEWORK.</summary>
public sealed class PlotRequest
{
    public string Title { get; init; } = "";
    public string XLabel { get; init; } = "";
    public string YLabel { get; init; } = "";
    public int Dimension { get; init; }
    public PlotThemeOptions? Theme { get; init; }
}
