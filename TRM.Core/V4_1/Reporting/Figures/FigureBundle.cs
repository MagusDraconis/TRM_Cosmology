namespace TRM.Core.V4_1.Reporting.Figures;

/// <summary>
/// Immutable figure bundle: panels + manifest + captions.
/// Classification: FRAMEWORK — reproducible artifact, no claims.
/// </summary>
public sealed class FigureBundle
{
    public string BundleId { get; init; } = "";
    public List<FigurePanelSpec> Panels { get; init; } = [];
    public FigureManifest Manifest { get; init; } = new();
    public Dictionary<string, byte[]> SvgArtifacts { get; init; } = [];
    public Dictionary<string, FigureCaption> Captions { get; init; } = [];
}
