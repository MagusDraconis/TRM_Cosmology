namespace TRM.Core.Credibility;

public sealed record ReproducibilityRecord(
    DateTime GeneratedAtUtc,
    string EngineVersion,
    string ParameterDigestSha256,
    IReadOnlyDictionary<string, string> Parameters);
