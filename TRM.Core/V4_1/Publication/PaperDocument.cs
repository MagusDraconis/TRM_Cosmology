namespace TRM.Core.V4_1.Publication;

public sealed class PaperDocument
{
    public string Title { get; init; } = "";
    public List<PaperSection> Sections { get; init; } = [];
    public List<ClaimEvidenceMap> ClaimEvidence { get; init; } = [];
}
