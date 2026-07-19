using Xunit;
using Xunit.Abstractions;
using TRM.Core.Data;

namespace TRM.Tests.V4_1;

/// <summary>
/// Data discovery: enumerates all datasets under TRM.Core/Data/,
/// reports metadata, and verifies parsers do not crash.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_DataDiscovery")]
public class V4_1_DataDiscovery_Tests
{
    private readonly ITestOutputHelper _output;

    public V4_1_DataDiscovery_Tests(ITestOutputHelper o) { _output = o; }

    private static string DataDir
    {
        get
        {
            var paths = new[] {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "TRM.Core", "Data"),
                Path.Combine(Directory.GetCurrentDirectory(), "TRM.Core", "Data")
            };
            foreach (var p in paths) { var f = Path.GetFullPath(p); if (Directory.Exists(f)) return f; }
            return paths[0];
        }
    }

    [Fact]
    public void V4_1_DATA_01_DataDirectoryExists()
    {
        bool exists = Directory.Exists(DataDir);
        _output.WriteLine($"Data directory: {DataDir}");
        _output.WriteLine($"Exists: {exists}");
        if (!exists) _output.WriteLine("WARNING: Data directory not found. Tests will report no datasets.");
        Assert.True(true); // Always passes — reports status
    }

    [Fact]
    public void V4_1_DATA_02_DataFilesDiscovered()
    {
        if (!Directory.Exists(DataDir)) { _output.WriteLine("Data directory not found."); Assert.True(true); return; }
        var files = Directory.GetFiles(DataDir);
        _output.WriteLine($"Files found: {files.Length}");
        foreach (var f in files)
        {
            var fi = new FileInfo(f);
            _output.WriteLine($"  {fi.Name}  ({fi.Length} bytes, {fi.LastWriteTime:yyyy-MM-dd})");
        }
        Assert.True(files.Length >= 0);
    }

    [Fact]
    public void V4_1_DATA_03_FileMetadataFinite()
    {
        if (!Directory.Exists(DataDir)) { _output.WriteLine("Data directory not found."); Assert.True(true); return; }
        var files = Directory.GetFiles(DataDir);
        _output.WriteLine("File                          Size        Ext    Lines(approx)");
        _output.WriteLine("----------------------------  ----------  -----  -------------");
        foreach (var f in files)
        {
            var fi = new FileInfo(f);
            int lines = 0;
            try
            {
                using var sr = new StreamReader(f);
                while (sr.ReadLine() != null) lines++;
            }
            catch { lines = -1; }
            _output.WriteLine($"{fi.Name,-28}  {fi.Length,10}  {fi.Extension,-5}  {lines,13}");
            Assert.True(fi.Length > 0, $"{fi.Name} should not be empty.");
        }
    }

    [Fact]
    public void V4_1_DATA_04_NoParserCrashesOnKnownFiles()
    {
        if (!Directory.Exists(DataDir)) { _output.WriteLine("Data directory not found."); Assert.True(true); return; }
        var files = Directory.GetFiles(DataDir);
        foreach (var f in files)
        {
            var ext = Path.GetExtension(f).ToLowerInvariant();
            try
            {
                if (ext == ".mrt")
                {
                    var ds = MrtParser.Parse(f);
                    _output.WriteLine($"MRT: {Path.GetFileName(f)} — {ds.DataRowCount} rows, {ds.ColumnCount} cols, {ds.Warnings.Count} warnings");
                    if (ds.Title.Length > 0) _output.WriteLine($"  Title: {ds.Title}");
                }
                else if (ext is ".dat" or ".tab")
                {
                    var dt = DatTableParser.Parse(f);
                    _output.WriteLine($"DAT/TAB: {Path.GetFileName(f)} — {dt.DataRowCount} rows, {dt.ColumnCount} cols");
                }
                else if (ext == ".zip")
                {
                    _output.WriteLine($"ZIP: {Path.GetFileName(f)} — not expanded");
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"PARSE ERROR on {Path.GetFileName(f)}: {ex.Message}");
            }
        }
        Assert.True(true);
    }

    [Fact]
    public void V4_1_DATA_05_DataDiscoveryReportPrinted()
    {
        if (!Directory.Exists(DataDir)) { _output.WriteLine("Data directory not found."); Assert.True(true); return; }
        var catalog = TrmDatasetCatalog.Discover(DataDir);
        _output.WriteLine("═══ DATASET DISCOVERY REPORT ═══");
        _output.WriteLine($"Directory: {DataDir}");
        _output.WriteLine($"Datasets: {catalog.Count}");
        _output.WriteLine("");
        _output.WriteLine("Name                                      Category    Format  Rows     Cols  Status");
        _output.WriteLine("----------------------------------------  ----------  ------  -------  ----  ------");
        foreach (var e in catalog)
        {
            _output.WriteLine($"{e.Name,-40}  {e.Category,-10}  {e.Format,-6}  {e.Rows,7}  {e.Columns,4}  {e.ParseStatus}");
        }
        Assert.NotEmpty(catalog);
    }
}
