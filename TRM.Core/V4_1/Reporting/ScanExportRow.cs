namespace TRM.Core.V4_1.Reporting;

/// <summary>Immutable export row for a single scan point. Classification: FRAMEWORK.</summary>
public sealed class ScanExportRow
{
    public int Dimension { get; init; }
    public double CouplingK { get; init; }
    public double FrequencySpread { get; init; }
    public double Dt { get; init; }
    public int Steps { get; init; }
    public int Seed { get; init; }
    public double R_Final { get; init; }
    public double PerturbationRecovery { get; init; }
    public double BridgeBandSpread { get; init; }
    public double SyncQualityScore { get; init; }
    public string Regime { get; init; } = "";

    public static string CsvHeader => "Dimension,K,Spread,Dt,Steps,Seed,R_Final,Recovery,BridgeBand,Quality,Regime";
    public string CsvRow
    {
        get
        {
            var ic = System.Globalization.CultureInfo.InvariantCulture;
            return $"{Dimension}," +
                $"{CouplingK.ToString("F6", ic)}," +
                $"{FrequencySpread.ToString("F6", ic)}," +
                $"{Dt.ToString("F4", ic)}," +
                $"{Steps},{Seed}," +
                $"{R_Final.ToString("F6", ic)}," +
                $"{PerturbationRecovery.ToString("F6", ic)}," +
                $"{BridgeBandSpread.ToString("F6", ic)}," +
                $"{SyncQualityScore.ToString("F6", ic)}," +
                $"{Regime}";
        }
    }
}
