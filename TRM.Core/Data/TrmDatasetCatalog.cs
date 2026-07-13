namespace TRM.Core.Data;

/// <summary>
/// Central registry of discovered datasets in TRM.Core/Data/.
/// Provides catalog metadata, parse status, and analysis readiness.
/// </summary>
public static class TrmDatasetCatalog
{
    public static List<DatasetEntry> Discover(string? dataDir = null)
    {
        dataDir ??= ResolveDataDirectory();
        var entries = new List<DatasetEntry>();
        if (!Directory.Exists(dataDir)) return entries;

        foreach (var file in Directory.GetFiles(dataDir))
        {
            var fi = new FileInfo(file);
            var entry = new DatasetEntry
            {
                Name = fi.Name,
                Path = fi.FullName,
                Format = DetectFormat(fi.Extension),
                Category = Categorize(fi.Name),
                FileSizeBytes = fi.Length,
                LastModified = fi.LastWriteTime
            };
            // Quick parse attempt
            try
            {
                if (entry.Format == "MRT")
                {
                    var ds = MrtParser.Parse(fi.FullName);
                    entry.ParseStatus = ds.Warnings.Count == 0 ? "OK" : $"Warnings: {ds.Warnings.Count}";
                    entry.Rows = ds.DataRowCount;
                    entry.Columns = ds.ColumnCount;
                    entry.Title = ds.Title;
                }
                else if (fi.Extension is ".dat" or ".tab")
                {
                    var dt = DatTableParser.Parse(fi.FullName, hasHeader: fi.Extension == ".tab" || fi.Name.Contains("Pantheon"));
                    entry.ParseStatus = dt.Warnings.Count == 0 ? "OK" : $"Warnings: {dt.Warnings.Count}";
                    entry.Rows = dt.DataRowCount;
                    entry.Columns = dt.ColumnCount;
                }
                else if (fi.Extension == ".zip")
                {
                    entry.ParseStatus = "ZIP archive — not expanded";
                }
                else
                {
                    entry.ParseStatus = "Unknown format";
                }
            }
            catch (Exception ex)
            {
                entry.ParseStatus = $"Parse error: {ex.Message}";
            }
            entries.Add(entry);
        }
        return entries;
    }

    public static string ResolveDataDirectory()
    {
        // Try relative to TRM.Core project
        var candidates = new[] {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "TRM.Core", "Data"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data"),
            Path.Combine(Directory.GetCurrentDirectory(), "TRM.Core", "Data"),
            Path.Combine(Directory.GetCurrentDirectory(), "Data")
        };
        foreach (var c in candidates)
        {
            var full = Path.GetFullPath(c);
            if (Directory.Exists(full)) return full;
        }
        return candidates[0]; // return first candidate even if not found
    }

    private static string DetectFormat(string ext) => ext.ToLowerInvariant() switch
    {
        ".mrt" => "MRT",
        ".dat" => "DAT",
        ".tab" => "TAB",
        ".csv" => "CSV",
        ".txt" => "TXT",
        ".zip" => "ZIP",
        _ => "Unknown"
    };

    private static string Categorize(string name) => name.ToLowerInvariant() switch
    {
        var n when n.Contains("sparc") || n.Contains("lelli") || n.Contains("rotmod") || n.Contains("massmodel") => "SPARC",
        var n when n.Contains("coma") => "Cluster",
        var n when n.Contains("pantheon") || n.Contains("sh0es") => "Cosmology",
        _ => "Unknown"
    };
}

public class DatasetEntry
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public string Format { get; set; } = "";
    public string Category { get; set; } = "";
    public long FileSizeBytes { get; set; }
    public DateTime LastModified { get; set; }
    public string ParseStatus { get; set; } = "";
    public string Title { get; set; } = "";
    public int Rows { get; set; }
    public int Columns { get; set; }
}
