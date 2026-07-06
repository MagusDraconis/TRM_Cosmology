using TRM.Core.Credibility;

namespace TRM.Tests.CoreTests;

public class CredibilityServiceTests
{
    [Fact]
    public void BuildReproducibilityRecord_Should_Be_Deterministic_For_ParameterDigest()
    {
        var service = new CredibilityService();
        var parameters = new Dictionary<string, string>
        {
            ["kappa"] = "0.3",
            ["b"] = "1.248"
        };

        var recordA = service.BuildReproducibilityRecord(parameters);
        var recordB = service.BuildReproducibilityRecord(new Dictionary<string, string>
        {
            ["b"] = "1.248",
            ["kappa"] = "0.3"
        });

        Assert.Equal(recordA.ParameterDigestSha256, recordB.ParameterDigestSha256);
    }

    [Fact]
    public void BuildCsv_Should_Contain_Header_And_RowValues()
    {
        var service = new CredibilityService();
        var csv = service.BuildCsv(
        [
            new ValidationExportRow("beta", "1.0", "1.0", "Pass")
        ]);

        Assert.Contains("Metric,Theory,Reference,Status", csv);
        Assert.Contains("\"beta\"", csv);
        Assert.Contains("\"Pass\"", csv);
    }

    [Fact]
    public void BuildLatexTable_Should_Contain_TableStructure()
    {
        var service = new CredibilityService();
        var latex = service.BuildLatexTable("Caption",
        [
            new ValidationExportRow("gamma", "1.0", "1.0", "Pass")
        ]);

        Assert.Contains("\\begin{table}", latex);
        Assert.Contains("\\caption{Caption}", latex);
        Assert.Contains("gamma", latex);
    }
}
