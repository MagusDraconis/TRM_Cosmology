using System.Globalization;
using System.Text;
using System.Text.Json;
using TRM.Core.V4_1.Sync;

namespace TRM.Core.V4_1.Reporting;

/// <summary>
/// Builds export rows, summaries, and figure-ready matrices from scan results.
/// Classification: FRAMEWORK — deterministic reporting, no claims.
/// </summary>
public static class ScanReportBuilder
{
    // ── Export rows ──────────────────────────────────────────────

    public static List<ScanExportRow> BuildExportRows(List<SyncParameterScanResult> results)
    {
        return results.Select(r => new ScanExportRow
        {
            Dimension = r.Point.Dimension,
            CouplingK = r.Point.CouplingK,
            FrequencySpread = r.Point.FrequencySpread,
            Dt = r.Point.Dt,
            Steps = r.Point.Steps,
            Seed = r.Point.Seed,
            R_Final = r.FinalOrderParameter,
            PerturbationRecovery = r.PerturbationRecovery,
            BridgeBandSpread = r.BridgeBandSpread,
            SyncQualityScore = r.SyncQualityScore,
            Regime = r.Regime
        }).ToList();
    }

    // ── CSV ──────────────────────────────────────────────────────

    public static string ToCsv(List<ScanExportRow> rows, ScanExportOptions? options = null)
    {
        var opt = options ?? new ScanExportOptions();
        var sb = new StringBuilder();

        if (opt.IncludeHeader)
            sb.AppendLine(ScanExportRow.CsvHeader);

        foreach (var row in rows)
            sb.AppendLine(row.CsvRow);

        return sb.ToString();
    }

    // ── JSON ─────────────────────────────────────────────────────

    public static string ToJson(List<ScanExportRow> rows)
    {
        return JsonSerializer.Serialize(rows, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    // ── Summaries ────────────────────────────────────────────────

    public static List<ScanSummaryRow> BuildDimensionSummaries(List<SyncParameterScanResult> results)
    {
        return results.GroupBy(r => r.Point.Dimension).Select(g =>
        {
            var list = g.ToList();
            return BuildSummary(list, $"D={g.Key}");
        }).ToList();
    }

    public static List<ScanSummaryRow> BuildCouplingSummaries(List<SyncParameterScanResult> results)
    {
        return results.GroupBy(r => r.Point.CouplingK).Select(g =>
        {
            var list = g.ToList();
            return BuildSummary(list, $"K={g.Key:F2}");
        }).ToList();
    }

    public static List<ScanSummaryRow> BuildSpreadSummaries(List<SyncParameterScanResult> results)
    {
        return results.GroupBy(r => r.Point.FrequencySpread).Select(g =>
        {
            var list = g.ToList();
            return BuildSummary(list, $"spread={g.Key:F2}");
        }).ToList();
    }

    private static ScanSummaryRow BuildSummary(List<SyncParameterScanResult> group, string key)
    {
        int synced = group.Count(r => r.Regime == "synced");
        int marginal = group.Count(r => r.Regime == "marginal");
        int unsynced = group.Count(r => r.Regime == "unsynced");
        double[] q = group.Select(r => r.SyncQualityScore).ToArray();

        return new ScanSummaryRow
        {
            GroupKey = key,
            PointCount = group.Count,
            SyncedCount = synced,
            MarginalCount = marginal,
            UnsyncedCount = unsynced,
            MeanQuality = q.Average(),
            MinQuality = q.Min(),
            MaxQuality = q.Max(),
            MeanBridgeBand = group.Average(r => r.BridgeBandSpread),
            MeanRecovery = group.Average(r => r.PerturbationRecovery)
        };
    }

    // ── Figure-ready matrices ────────────────────────────────────

    public static (double[][] matrix, string[] kLabels, string[] spreadLabels)
        BuildQualityHeatmap(List<SyncParameterScanResult> results, int dimension)
    {
        var dimResults = results.Where(r => r.Point.Dimension == dimension).ToList();
        var kValues = dimResults.Select(r => r.Point.CouplingK).Distinct().OrderBy(x => x).ToArray();
        var spreadValues = dimResults.Select(r => r.Point.FrequencySpread).Distinct().OrderBy(x => x).ToArray();

        var matrix = new double[kValues.Length][];
        for (int i = 0; i < kValues.Length; i++)
        {
            matrix[i] = new double[spreadValues.Length];
            for (int j = 0; j < spreadValues.Length; j++)
            {
                var match = dimResults.FirstOrDefault(r =>
                    Math.Abs(r.Point.CouplingK - kValues[i]) < 1e-10 &&
                    Math.Abs(r.Point.FrequencySpread - spreadValues[j]) < 1e-10);
                matrix[i][j] = match?.SyncQualityScore ?? double.NaN;
            }
        }

        return (matrix,
            kValues.Select(k => k.ToString("F2", CultureInfo.InvariantCulture)).ToArray(),
            spreadValues.Select(s => s.ToString("F2", CultureInfo.InvariantCulture)).ToArray());
    }

    public static (string[][] matrix, string[] kLabels, string[] spreadLabels)
        BuildRegimeGrid(List<SyncParameterScanResult> results, int dimension)
    {
        var dimResults = results.Where(r => r.Point.Dimension == dimension).ToList();
        var kValues = dimResults.Select(r => r.Point.CouplingK).Distinct().OrderBy(x => x).ToArray();
        var spreadValues = dimResults.Select(r => r.Point.FrequencySpread).Distinct().OrderBy(x => x).ToArray();

        var matrix = new string[kValues.Length][];
        for (int i = 0; i < kValues.Length; i++)
        {
            matrix[i] = new string[spreadValues.Length];
            for (int j = 0; j < spreadValues.Length; j++)
            {
                var match = dimResults.FirstOrDefault(r =>
                    Math.Abs(r.Point.CouplingK - kValues[i]) < 1e-10 &&
                    Math.Abs(r.Point.FrequencySpread - spreadValues[j]) < 1e-10);
                matrix[i][j] = match?.Regime ?? "missing";
            }
        }

        return (matrix,
            kValues.Select(k => k.ToString("F2", CultureInfo.InvariantCulture)).ToArray(),
            spreadValues.Select(s => s.ToString("F2", CultureInfo.InvariantCulture)).ToArray());
    }
}
