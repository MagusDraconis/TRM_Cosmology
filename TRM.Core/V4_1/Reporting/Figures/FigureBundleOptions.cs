namespace TRM.Core.V4_1.Reporting.Figures;

/// <summary>Bundle options. Classification: FRAMEWORK.</summary>
public sealed class FigureBundleOptions
{
    public string OutputPrefix { get; init; } = "v4_1_fig";
    public int SvgWidth { get; init; } = 600;
    public int SvgHeight { get; init; } = 400;
    public bool IncludeJsonManifest { get; init; } = true;
}
