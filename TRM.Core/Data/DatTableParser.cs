using System.Globalization;

namespace TRM.Core.Data;

/// <summary>
/// Generic whitespace/comma/tab-separated table parser with comment handling.
/// Supports #, %, ;, // comment prefixes and optional header row.
/// </summary>
public static class DatTableParser
{
    public static DatTable Parse(string filePath, bool hasHeader = true, string[]? commentPrefixes = null)
    {
        var lines = File.ReadAllLines(filePath);
        commentPrefixes ??= new[] { "#", "%", ";", "//" };
        var table = new DatTable { FilePath = filePath, RawLines = lines };
        var warnings = new List<string>();

        int lineIdx = 0;
        // Skip comment/header detection lines
        while (lineIdx < lines.Length && (string.IsNullOrWhiteSpace(lines[lineIdx]) || commentPrefixes.Any(p => lines[lineIdx].TrimStart().StartsWith(p))))
        {
            if (!string.IsNullOrWhiteSpace(lines[lineIdx]))
                table.Comments.Add(lines[lineIdx]);
            lineIdx++;
        }
        // Header row
        if (hasHeader && lineIdx < lines.Length)
        {
            var header = lines[lineIdx];
            // Try tab, comma, then whitespace
            char sep = '\t';
            if (!header.Contains('\t') && header.Contains(',')) sep = ',';
            var hdrParts = header.Split(sep, StringSplitOptions.RemoveEmptyEntries);
            if (sep == ',' || hdrParts.Length > 1)
            {
                table.Headers = hdrParts.Select(h => h.Trim()).ToList();
                lineIdx++;
            }
            else
            {
                // Space-separated
                table.Headers = header.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                lineIdx++;
            }
        }
        // Data rows
        char dataSep = ' ';
        if (lineIdx < lines.Length)
        {
            var fl = lines[lineIdx];
            if (fl.Contains('\t')) dataSep = '\t';
            else if (fl.Contains(',') && !fl.Contains('.')) dataSep = ',';
        }
        for (; lineIdx < lines.Length; lineIdx++)
        {
            var line = lines[lineIdx].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (commentPrefixes.Any(p => line.StartsWith(p))) { table.Comments.Add(line); continue; }
            try
            {
                var parts = dataSep == ' '
                    ? line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    : line.Split(dataSep);
                var row = new DatRow { Values = parts.ToList() };
                // Try parse numeric values
                row.NumericValues = parts.Select(p =>
                {
                    if (double.TryParse(p, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) return d;
                    if (p == "?" || p == "<" || p == "=" || p == "-999" || p == "-9" || p == "--") return double.NaN;
                    return double.NaN;
                }).ToList();
                table.Rows.Add(row);
            }
            catch (Exception ex) { warnings.Add($"Row {lineIdx + 1}: {ex.Message}"); }
        }
        table.Warnings = warnings;
        return table;
    }
}

public class DatTable
{
    public string FilePath { get; set; } = "";
    public List<string> Comments { get; set; } = new();
    public List<string> Headers { get; set; } = new();
    public List<DatRow> Rows { get; set; } = new();
    public string[] RawLines { get; set; } = Array.Empty<string>();
    public List<string> Warnings { get; set; } = new();
    public int DataRowCount => Rows.Count;
    public int ColumnCount => Headers.Count > 0 ? Headers.Count : (Rows.Count > 0 ? Rows[0].Values.Count : 0);
}

public class DatRow
{
    public List<string> Values { get; set; } = new();
    public List<double> NumericValues { get; set; } = new();
}
