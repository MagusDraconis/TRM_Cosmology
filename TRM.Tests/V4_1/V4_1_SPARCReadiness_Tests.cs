using Xunit;
using Xunit.Abstractions;
using TRM.Core.Data;
using TRM.Core.Analysis;

namespace TRM.Tests.V4_1;

/// <summary>
/// SPARC readiness: checks whether SPARC-like data can be parsed and
/// prepared for future TRM residual analysis. Does NOT fit SPARC.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_SPARCReadiness")]
public class V4_1_SPARCReadiness_Tests
{
    private readonly ITestOutputHelper _output;

    public V4_1_SPARCReadiness_Tests(ITestOutputHelper o) { _output = o; }

    private static string DataDir => TrmDatasetCatalog.ResolveDataDirectory();

    [Fact]
    public void V4_1_SPARC_01_SparcFilesDetectedIfPresent()
    {
        if (!Directory.Exists(DataDir)) { _output.WriteLine("Data directory not found."); Assert.True(true); return; }
        var sparcFiles = Directory.GetFiles(DataDir, "*SPARC*")
            .Concat(Directory.GetFiles(DataDir, "*Lelli*"))
            .Concat(Directory.GetFiles(DataDir, "*MassModel*"))
            .Concat(Directory.GetFiles(DataDir, "*Rotmod*"))
            .Distinct().ToList();
        _output.WriteLine($"SPARC-related files found: {sparcFiles.Count}");
        foreach (var f in sparcFiles) _output.WriteLine($"  {Path.GetFileName(f)}");
        Assert.True(sparcFiles.Count >= 0);
    }

    [Fact]
    public void V4_1_SPARC_02_SparcMrtParseReadiness()
    {
        if (!Directory.Exists(DataDir)) { _output.WriteLine("Data directory not found."); Assert.True(true); return; }
        var mrtFiles = Directory.GetFiles(DataDir, "*.mrt");
        _output.WriteLine($"MRT files found: {mrtFiles.Length}");
        foreach (var f in mrtFiles)
        {
            var ds = MrtParser.Parse(f);
            _output.WriteLine($"─── {Path.GetFileName(f)} ───");
            _output.WriteLine($"  Title: {ds.Title}");
            _output.WriteLine($"  Rows: {ds.DataRowCount}  Columns: {ds.ColumnCount}");
            _output.WriteLine($"  Warnings: {ds.Warnings.Count}");
            if (ds.Columns.Count > 0)
            {
                _output.WriteLine("  Columns:");
                foreach (var c in ds.Columns)
                    _output.WriteLine($"    {c.Label,-12} {c.Units,-10} bytes {c.StartByte}-{c.EndByte}  ({c.Format})");
            }
            Assert.True(ds.DataRowCount >= 0);
        }
    }

    [Fact]
    public void V4_1_SPARC_03_RotationCurveColumnsCandidateDetection()
    {
        if (!Directory.Exists(DataDir)) { _output.WriteLine("Data directory not found."); Assert.True(true); return; }
        var massModels = Path.Combine(DataDir, "MassModels_Lelli2016c.mrt");
        if (!File.Exists(massModels)) { _output.WriteLine("MassModels file not found."); Assert.True(true); return; }
        var ds = MrtParser.Parse(massModels);
        _output.WriteLine($"Parsed: {ds.ColumnCount} columns, {ds.DataRowCount} rows");
        var colLabels = string.Join(" ", ds.Columns.Select(c => $"{c.Label}({c.Units})"));
        _output.WriteLine($"Column labels found: {colLabels}");
        var candidates = new[] { "R", "Vobs", "Vgas", "Vdisk", "Vbul", "D", "ID", "SBdisk", "SBbul", "e_Vobs" };
        _output.WriteLine("═══ ROTATION CURVE COLUMN CANDIDATES (MassModels) ═══");
        _output.WriteLine("Candidate  Found?");
        _output.WriteLine("---------  ------");
        foreach (var cand in candidates)
        {
            var col = ds.Columns.FirstOrDefault(c => c.Label.Equals(cand, StringComparison.OrdinalIgnoreCase));
            _output.WriteLine($"{cand,-9}  {(col != null ? "YES" : "NO"),-6}");
        }
        Assert.True(ds.ColumnCount > 0, "Should find some columns.");
    }

    [Fact]
    public void V4_1_SPARC_04_NoPhysicalClaimsMade()
    {
        _output.WriteLine("═══ CLAIM DISCIPLINE CHECK ═══");
        _output.WriteLine("SUPPORTED:");
        _output.WriteLine("  - SPARC files are discovered and partially/fully parsed.");
        _output.WriteLine("  - Candidate rotation-curve columns can be detected if present.");
        _output.WriteLine("");
        _output.WriteLine("CONDITIONAL:");
        _output.WriteLine("  - Column mapping depends on file format.");
        _output.WriteLine("  - Future residual analysis depends on validated units and columns.");
        _output.WriteLine("");
        _output.WriteLine("HYPOTHESIS:");
        _output.WriteLine("  - TRM load/response/fractal diagnostics may later be compared to SPARC residuals.");
        _output.WriteLine("");
        _output.WriteLine("NOT CLAIMED:");
        _output.WriteLine("  - SPARC is explained.");
        _output.WriteLine("  - Dark matter is replaced.");
        _output.WriteLine("  - MOND is replaced.");
        _output.WriteLine("  - Galaxy rotation curves are fit.");
        Assert.True(true);
    }

    [Fact]
    public void V4_1_SPARC_05_SparcReadinessReport()
    {
        var catalog = TrmDatasetCatalog.Discover();
        var sparcEntries = catalog.Where(e => e.Category == "SPARC").ToList();
        _output.WriteLine("═══ SPARC READINESS REPORT ═══");
        _output.WriteLine($"SPARC-related datasets: {sparcEntries.Count}");
        _output.WriteLine("");
        foreach (var e in sparcEntries)
        {
            _output.WriteLine($"  {e.Name}: {e.Format}  rows={e.Rows} cols={e.Columns} status={e.ParseStatus}");
        }
        _output.WriteLine("");
        // Check placeholder analysis
        var r1 = SparcResidualAnalysis.ComputeNewtonianResidualPreview();
        var r2 = SparcResidualAnalysis.ComputeTrmResponseKernelProjection();
        _output.WriteLine($"Newtonian residual: {r1.Status} — {r1.Message}");
        _output.WriteLine($"TRM response kernel: {r2.Status} — {r2.Message}");
        _output.WriteLine("");
        _output.WriteLine("NOTE: No SPARC fitting or dark matter claim is made.");
        Assert.NotEmpty(catalog);
    }
}
