namespace TRM.Core.V4_1.Reporting;

/// <summary>Export configuration. Classification: FRAMEWORK.</summary>
public sealed class ScanExportOptions
{
    public bool IncludeHeader { get; init; } = true;
    public bool InvariantCulture { get; init; } = true;
    public string Delimiter { get; init; } = ",";
}
