namespace TRM.Core.V4_1.Reporting;

/// <summary>Aggregated summary for a group of scan points. Classification: FRAMEWORK.</summary>
public sealed class ScanSummaryRow
{
    public string GroupKey { get; init; } = "";
    public int PointCount { get; init; }
    public int SyncedCount { get; init; }
    public int MarginalCount { get; init; }
    public int UnsyncedCount { get; init; }
    public double MeanQuality { get; init; }
    public double MinQuality { get; init; }
    public double MaxQuality { get; init; }
    public double MeanBridgeBand { get; init; }
    public double MeanRecovery { get; init; }

    public static string CsvHeader => "Group,Points,Synced,Marginal,Unsynced,MeanQ,MinQ,MaxQ,MeanBridge,MeanRecovery";
    public string CsvRow
    {
        get
        {
            var ic = System.Globalization.CultureInfo.InvariantCulture;
            return $"{GroupKey},{PointCount},{SyncedCount},{MarginalCount},{UnsyncedCount}," +
                $"{MeanQuality.ToString("F6", ic)},{MinQuality.ToString("F6", ic)},{MaxQuality.ToString("F6", ic)}," +
                $"{MeanBridgeBand.ToString("F6", ic)},{MeanRecovery.ToString("F6", ic)}";
        }
    }
}
