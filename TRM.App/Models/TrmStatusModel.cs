using System.Text.Json.Serialization;

namespace TRM.App.Models;

public sealed class TrmStatusModel
{
    [JsonPropertyName("releaseStatus")]
    public string ReleaseStatus { get; init; } = "EXPLORATORY";

    [JsonPropertyName("date")]
    public string Date { get; init; } = "";

    [JsonPropertyName("testSummary")]
    public TestSummary TestSummary { get; init; } = new();

    [JsonPropertyName("suites")]
    public List<TestSuiteStatus> Suites { get; init; } = [];

    [JsonPropertyName("supportedClaims")]
    public List<string> SupportedClaims { get; init; } = [];

    [JsonPropertyName("conditionalClaims")]
    public List<string> ConditionalClaims { get; init; } = [];

    [JsonPropertyName("hypotheses")]
    public List<string> Hypotheses { get; init; } = [];

    [JsonPropertyName("notClaimed")]
    public List<string> NotClaimed { get; init; } = [];

    [JsonPropertyName("openProblems")]
    public List<RoadmapItem> OpenProblems { get; init; } = [];

    [JsonPropertyName("documents")]
    public List<DocumentLink> Documents { get; init; } = [];

    [JsonPropertyName("roadmap")]
    public Roadmap Roadmap { get; init; } = new();

    [JsonPropertyName("releaseIntegrity")]
    public ReleaseIntegrity ReleaseIntegrity { get; init; } = new();
}

public sealed class TestSummary
{
    [JsonPropertyName("total")]
    public int Total { get; init; }
    [JsonPropertyName("passed")]
    public int Passed { get; init; }
    [JsonPropertyName("failed")]
    public int Failed { get; init; }
    [JsonPropertyName("skipped")]
    public int Skipped { get; init; }
    [JsonPropertyName("verification")]
    public string Verification { get; init; } = "";
    [JsonPropertyName("command")]
    public string Command { get; init; } = "";
    [JsonPropertyName("date")]
    public string Date { get; init; } = "";
}

public sealed class TestSuiteStatus
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";
    [JsonPropertyName("tests")]
    public int Tests { get; init; }
    [JsonPropertyName("status")]
    public string Status { get; init; } = "PASS";
    [JsonPropertyName("claimCategory")]
    public string ClaimCategory { get; init; } = "";
}

public sealed class DocumentLink
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = "";
    [JsonPropertyName("filename")]
    public string Filename { get; init; } = "";
    [JsonPropertyName("url")]
    public string? Url { get; init; }
    [JsonPropertyName("available")]
    public bool Available { get; init; }
    [JsonPropertyName("description")]
    public string Description { get; init; } = "";
}

public sealed class RoadmapItem
{
    [JsonPropertyName("priority")]
    public string Priority { get; init; } = "";
    [JsonPropertyName("title")]
    public string Title { get; init; } = "";
    [JsonPropertyName("description")]
    public string Description { get; init; } = "";
}

public sealed class Roadmap
{
    [JsonPropertyName("recommendedTestFile")]
    public string RecommendedTestFile { get; init; } = "";
    [JsonPropertyName("candidateUpdateLaws")]
    public List<string> CandidateUpdateLaws { get; init; } = [];
}

public sealed class ReleaseIntegrity
{
    [JsonPropertyName("totalTestsVerified")]
    public int TotalTestsVerified { get; init; }
    [JsonPropertyName("passedVerified")]
    public int PassedVerified { get; init; }
    [JsonPropertyName("verificationMethod")]
    public string VerificationMethod { get; init; } = "";
    [JsonPropertyName("mismatch")]
    public string? Mismatch { get; init; }
}
