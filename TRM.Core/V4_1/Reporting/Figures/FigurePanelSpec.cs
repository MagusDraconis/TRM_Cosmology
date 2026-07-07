namespace TRM.Core.V4_1.Reporting.Figures;

/// <summary>Describes a single figure panel. Classification: FRAMEWORK.</summary>
public sealed class FigurePanelSpec
{
    public string PanelId { get; init; } = "";
    public string Filename { get; init; } = "";
    public int Dimension { get; init; }
    public string ShortCaption { get; init; } = "";
    public string LongCaption { get; init; } = "";
}
