namespace TRM.Core.V4_1.Reduction;

public static class CanonicalFormInterpretationEngine
{
    public static List<CanonicalFormInterpretation> Interpret(CanonicalFormAuditResult result)
    {
        var interps = new List<CanonicalFormInterpretation>();

        interps.Add(new CanonicalFormInterpretation(
            $"Preferred form: {result.Preferred.Form.Name} " +
            $"(R²={result.Preferred.RSquared:F2}, blocks={result.Preferred.Form.Blocks.Count}, " +
            $"status={result.Preferred.Status}).",
            result.Preferred.Status == "PREFERRED" ? "SUPPORTED" : "CONDITIONAL",
            new() { ["r2"] = result.Preferred.RSquared }));

        interps.Add(new CanonicalFormInterpretation(
            result.Preferred.Form.Blocks.Count <= 2
                ? "The effective equation is best represented by a minimal block structure."
                : "A multi-block structure is required for adequate closure.",
            result.Preferred.Form.Blocks.Count <= 2 ? "SUPPORTED" : "CONDITIONAL",
            new() { ["n_blocks"] = result.Preferred.Form.Blocks.Count }));

        var correctionBlocks = result.Preferred.Form.Blocks
            .Where(b => b.Name.Contains("Correction") || b.Name.Contains("GeoCorrection")).ToList();
        interps.Add(new CanonicalFormInterpretation(
            correctionBlocks.Count > 0
                ? $"The correction block ({string.Join(", ", correctionBlocks.Select(b => b.Name))}) " +
                  "remains subleading and does not act as a primary driver."
                : "No separate correction block identified — all terms appear in core structure.",
            correctionBlocks.Count > 0 ? "SUPPORTED" : "CONDITIONAL",
            new() { ["n_corrections"] = correctionBlocks.Count }));

        int nCandidates = result.Candidates.Count;
        int viable = result.Candidates.Count(c => c.Status is "PREFERRED" or "VIABLE");
        interps.Add(new CanonicalFormInterpretation(
            $"{viable}/{nCandidates} canonical forms achieve viable closure.",
            viable >= 3 ? "SUPPORTED" : "CONDITIONAL",
            new() { ["viable"] = viable }));

        return interps;
    }
}
