using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace TRM.Core.Credibility;

public sealed class CredibilityService : ICredibilityService
{
    public ReproducibilityRecord BuildReproducibilityRecord(IReadOnlyDictionary<string, string> parameters)
    {
        var normalized = parameters
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);

        var joined = string.Join(";", normalized.Select(kv => $"{kv.Key}={kv.Value}"));
        var digest = ComputeSha256(joined);

        var version = typeof(CredibilityService).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? typeof(CredibilityService).Assembly.GetName().Version?.ToString()
            ?? "unknown";

        return new ReproducibilityRecord(
            GeneratedAtUtc: DateTime.UtcNow,
            EngineVersion: version,
            ParameterDigestSha256: digest,
            Parameters: normalized);
    }

    public string BuildCsv(IEnumerable<ValidationExportRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Metric,Theory,Reference,Status");

        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",",
                Escape(row.Metric),
                Escape(row.Theory),
                Escape(row.Reference),
                Escape(row.Status)));
        }

        return sb.ToString();
    }

    public string BuildLatexTable(string caption, IEnumerable<ValidationExportRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("\\begin{table}[h]");
        sb.AppendLine("\\centering");
        sb.AppendLine("\\begin{tabular}{lccc}");
        sb.AppendLine("\\hline");
        sb.AppendLine("Metric & Theory & Reference & Status \\\\ ");
        sb.AppendLine("\\hline");

        foreach (var row in rows)
        {
            sb.AppendLine($"{EscapeLatex(row.Metric)} & {EscapeLatex(row.Theory)} & {EscapeLatex(row.Reference)} & {EscapeLatex(row.Status)} \\\\");
        }

        sb.AppendLine("\\hline");
        sb.AppendLine("\\end{tabular}");
        sb.AppendLine($"\\caption{{{EscapeLatex(caption)}}}");
        sb.AppendLine("\\end{table}");

        return sb.ToString();
    }

    private static string Escape(string value)
    {
        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    private static string EscapeLatex(string value)
    {
        return value
            .Replace("\\", "\\textbackslash{}")
            .Replace("_", "\\_")
            .Replace("%", "\\%")
            .Replace("&", "\\&")
            .Replace("#", "\\#");
    }

    private static string ComputeSha256(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
