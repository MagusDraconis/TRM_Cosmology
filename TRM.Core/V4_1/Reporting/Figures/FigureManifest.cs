namespace TRM.Core.V4_1.Reporting.Figures;

/// <summary>Manifest listing all panels in a figure bundle. Classification: FRAMEWORK.</summary>
public sealed class FigureManifest
{
    public string BundleId { get; init; } = "";
    public List<FigurePanelSpec> Panels { get; init; } = [];
}
