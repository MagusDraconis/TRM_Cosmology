namespace TRM.Core.Data;

/// <summary>
/// Lightweight MRT (Machine-Readable Table) parser for astronomical datasets.
/// Handles CDS-style fixed-width tables with byte-by-byte column descriptions.
/// Falls back to whitespace parsing when byte descriptions are absent.
/// </summary>
public static class MrtParser
{
    public static MrtDataSet Parse(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        var ds = new MrtDataSet { FilePath = filePath, RawLines = lines };
        var warnings = new List<string>();

        int lineIdx = 0;
        // Parse metadata header
        while (lineIdx < lines.Length)
        {
            var line = lines[lineIdx];
            if (line.StartsWith("Title:")) ds.Title = line[7..].Trim();
            else if (line.StartsWith("Authors:")) ds.Authors = line[9..].Trim();
            else if (line.StartsWith("Table:")) ds.TableName = line[7..].Trim();
            else if (line.StartsWith("===")) { lineIdx++; break; }
            lineIdx++;
        }
        // Parse byte-by-byte description
        while (lineIdx < lines.Length && lines[lineIdx].Contains("Byte"))
        {
            lineIdx++;
            // Skip lines until we reach the actual column definitions
            // Pattern: header "   Bytes Format..." then "---" then column lines
            while (lineIdx < lines.Length && !lines[lineIdx].StartsWith("   Bytes"))
                lineIdx++;
            // Skip the "   Bytes..." header line
            if (lineIdx < lines.Length && lines[lineIdx].StartsWith("   Bytes"))
                lineIdx++;
            // Skip separator "---"
            while (lineIdx < lines.Length && (lines[lineIdx].StartsWith("---") || lines[lineIdx].StartsWith("===")))
                lineIdx++;
            // Now parse column definition lines (digit-prefixed)
            while (lineIdx < lines.Length && !string.IsNullOrWhiteSpace(lines[lineIdx]))
            {
                var colLine = lines[lineIdx].Trim();
                if (colLine.Length >= 2 && (char.IsDigit(colLine[0]) || (colLine[0] == ' ' && colLine.Length > 2 && char.IsDigit(colLine[1]))))
                {
                    var col = ParseColumnDef(lines[lineIdx]);
                    if (col != null) ds.Columns.Add(col);
                }
                else if (colLine.StartsWith("-") || colLine.StartsWith("Note"))
                    break;
                lineIdx++;
            }
            break;
        }
        // Skip separator lines and notes between columns and data
        while (lineIdx < lines.Length && (lines[lineIdx].TrimStart().StartsWith("-") || lines[lineIdx].TrimStart().StartsWith("=") || lines[lineIdx].TrimStart().StartsWith("Note")))
            lineIdx++;
        // Parse data rows
        int consecEmpty = 0;
        while (lineIdx < lines.Length)
        {
            var line = lines[lineIdx];
            if (string.IsNullOrWhiteSpace(line)) { consecEmpty++; lineIdx++; if (consecEmpty >= 3) break; continue; }
            consecEmpty = 0;
            var trimmed = line.TrimEnd();
            // Footer detection: only break on Note/See/Ref after data
            if (ds.Rows.Count > 0 && (trimmed.StartsWith("Note") || trimmed.StartsWith("See") || trimmed.StartsWith("Ref"))) break;
            try
            {
                var values = ds.Columns.Count > 0 && ds.Columns.Any(c => c.StartByte > 0)
                    ? ParseFixedWidth(trimmed, ds.Columns)
                    : ParseWhitespace(trimmed);
                if (values.Count > 0 && values.Any(v => v.Length > 0))
                    ds.Rows.Add(new MrtRow { Values = values, RawLine = trimmed });
            }
            catch (Exception ex) { warnings.Add($"Row {lineIdx + 1}: {ex.Message}"); }
            lineIdx++;
        }
        ds.Warnings = warnings;
        return ds;
    }

    private static MrtColumn? ParseColumnDef(string line)
    {
        try
        {
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) return null;
            // Parse byte range: "1-11" or space-padded "1- 11"
            string byteStr;
            if (parts[0].EndsWith("-") && parts.Length > 1 && char.IsDigit(parts[1][0]))
            {
                byteStr = parts[0] + parts[1]; // Combine "1-" + "11" → "1-11"
                parts = parts.Skip(1).ToArray(); // Shift remaining parts
            }
            else byteStr = parts[0];
            var byteRange = byteStr.Split('-');
            if (byteRange.Length < 2) return null;
            if (!int.TryParse(byteRange[0], out int start) || !int.TryParse(byteRange[1], out int end)) return null;
            // After fixing, parts[0] is now the first non-byte part (Format)
            var col = new MrtColumn { StartByte = start, EndByte = end, Format = parts.Length > 1 ? parts[1] : "", Units = parts.Length > 2 ? parts[2] : "" };
            int labelIdx = 3;
            if (col.Units == "---") labelIdx = 2;
            if (parts.Length > labelIdx) col.Label = parts[labelIdx];
            return col;
        }
        catch { return null; }
    }

    private static List<string> ParseFixedWidth(string line, List<MrtColumn> columns)
    {
        var values = new List<string>();
        foreach (var col in columns.Where(c => c.StartByte > 0 && c.EndByte > 0))
        {
            int start = Math.Min(col.StartByte - 1, line.Length - 1);
            int len = Math.Min(col.EndByte - col.StartByte + 1, line.Length - start);
            if (start >= line.Length) values.Add("");
            else values.Add(line.Substring(start, len).Trim());
        }
        return values;
    }

    private static List<string> ParseWhitespace(string line)
    {
        return line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).ToList();
    }
}

public class MrtDataSet
{
    public string FilePath { get; set; } = "";
    public string Title { get; set; } = "";
    public string Authors { get; set; } = "";
    public string TableName { get; set; } = "";
    public List<MrtColumn> Columns { get; set; } = new();
    public List<MrtRow> Rows { get; set; } = new();
    public string[] RawLines { get; set; } = Array.Empty<string>();
    public List<string> Warnings { get; set; } = new();
    public int DataRowCount => Rows.Count;
    public int ColumnCount => Columns.Count;
}

public class MrtColumn
{
    public int StartByte { get; set; }
    public int EndByte { get; set; }
    public string Format { get; set; } = "";
    public string Units { get; set; } = "";
    public string Label { get; set; } = "";
}

public class MrtRow
{
    public List<string> Values { get; set; } = new();
    public string RawLine { get; set; } = "";
}
