using Xunit;
using TRM.Core.V4_1.Reporting;
using TRM.Core.V4_1.Sync;

namespace TRM.Tests.V4_1;

[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_Reporting")]
public class V4_1_ReportingExport_Tests
{
    private static List<SyncParameterScanResult> SampleResults()
    {
        var cfg = new SyncParameterScanConfig
        {
            Dimensions = [3], CouplingStrengths = [0.5], FrequencySpreads = [0.1],
            Dt = 0.05, Steps = 200, SeedsPerPoint = 2, GraphSizePerDim = 3
        };
        return SyncParameterScanner.Scan(cfg);
    }

    [Fact]
    public void V4_1_60_CsvExport_HasStableColumnOrder()
    {
        var results = SampleResults();
        var rows = ScanReportBuilder.BuildExportRows(results);
        var csv = ScanReportBuilder.ToCsv(rows);
        var lines = csv.TrimEnd().Split('\n');
        Assert.Equal(ScanExportRow.CsvHeader, lines[0].Trim());
        Assert.Equal(results.Count + 1, lines.Length);
    }

    [Fact]
    public void V4_1_61_CsvExport_RepeatedRun_IsByteStable()
    {
        var r1 = SampleResults();
        var r2 = SampleResults();
        var csv1 = ScanReportBuilder.ToCsv(ScanReportBuilder.BuildExportRows(r1));
        var csv2 = ScanReportBuilder.ToCsv(ScanReportBuilder.BuildExportRows(r2));
        Assert.Equal(csv1, csv2);
    }

    [Fact]
    public void V4_1_62_JsonExport_RepeatedRun_IsStructurallyStable()
    {
        var r1 = SampleResults();
        var r2 = SampleResults();
        var json1 = ScanReportBuilder.ToJson(ScanReportBuilder.BuildExportRows(r1));
        var json2 = ScanReportBuilder.ToJson(ScanReportBuilder.BuildExportRows(r2));
        Assert.Equal(json1, json2);
    }

    [Fact]
    public void V4_1_67_ExportPipeline_DoesNotProduceNaNStringsOrLocaleArtifacts()
    {
        var results = SampleResults();
        var rows = ScanReportBuilder.BuildExportRows(results);
        var csv = ScanReportBuilder.ToCsv(rows);
        Assert.DoesNotContain("NaN", csv);
        Assert.DoesNotContain("Infinity", csv);
        // Invariant culture: decimal separator is '.', all float fields formatted with F6.
        var firstDataLine = csv.Split('\n')[1];
        Assert.Contains("0.05", firstDataLine); // Dt
    }
}
