namespace TRM.Core.V4_1.Reduction;

/// <summary>
/// Maps effective equation terms to explicit mechanism channels.
/// Classification: FRAMEWORK — attribution analysis, no claims.
/// </summary>
public static class MechanismEquationEngine
{
    /// <summary>Build the attribution matrix.</summary>
    public static MechanismEquationResult Analyze()
    {
        var mappings = new List<EquationTermMechanismMap>
        {
            new("S_spectral", "spectral-balance", 0.90, "primary"),
            new("S_defect", "defect-1/r-consistency", 0.85, "primary"),
            new("S_sync", "sync-stability", 0.70, "primary"),
            new("S_sync", "isotropy", 0.30, "mixed"),
            new("S_outer", "outer-exponent", 0.80, "correction"),
            new("S_outer", "radial-law", 0.20, "mixed"),
        };

        var primary = new List<string> { "S_spectral", "S_defect" };
        var correction = new List<string> { "S_outer" };
        var mixed = new List<string> { "S_sync" };

        // Unexplained residual: 1.0 − sum of max attribution per term.
        double maxAttrSum = mappings.GroupBy(m => m.EquationTerm)
            .Sum(g => g.Max(m => m.AttributionStrength));
        double unexplained = Math.Max(0, mappings.Select(m => m.EquationTerm).Distinct().Count() - maxAttrSum);

        // Redundancy: average overlap where two mechanisms map to the same term.
        double redundancy = 0;
        var byTerm = mappings.GroupBy(m => m.EquationTerm).Where(g => g.Count() > 1).ToList();
        if (byTerm.Count > 0)
            redundancy = byTerm.Average(g => g.Skip(1).Sum(m => m.AttributionStrength));

        return new MechanismEquationResult(mappings, primary, correction, mixed, unexplained, redundancy);
    }
}
