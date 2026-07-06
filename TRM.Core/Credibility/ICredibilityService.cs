namespace TRM.Core.Credibility;

public interface ICredibilityService
{
    ReproducibilityRecord BuildReproducibilityRecord(IReadOnlyDictionary<string, string> parameters);
    string BuildCsv(IEnumerable<ValidationExportRow> rows);
    string BuildLatexTable(string caption, IEnumerable<ValidationExportRow> rows);
}
