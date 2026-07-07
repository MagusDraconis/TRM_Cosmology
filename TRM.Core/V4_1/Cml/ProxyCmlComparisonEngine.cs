namespace TRM.Core.V4_1.Cml;

/// <summary>
/// Compares CML audit results against proxy-level (sync diagnostic) expectations.
/// Classification: FRAMEWORK — comparison only, no claims.
/// </summary>
public static class ProxyCmlComparisonEngine
{
    public static List<ProxyCmlComparison> Compare(
        List<CmlDimensionAuditResult> cmlResults)
    {
        var comps = new List<ProxyCmlComparison>();
        if (cmlResults.Count == 0) return comps;

        var sorted = cmlResults.OrderByDescending(r => r.MeanOrderParameter).ToList();
        var best = sorted[0];
        double margin = sorted.Count > 1 ? best.MeanOrderParameter - sorted[1].MeanOrderParameter : 0;

        if (margin > 0.05)
            comps.Add(new ProxyCmlComparison
            {
                Dimension = best.Dimension,
                Statement = $"CML results are broadly consistent with proxy-level preference for D={best.Dimension}.",
                Classification = "SUPPORTED"
            });
        else if (margin > 0.01)
            comps.Add(new ProxyCmlComparison
            {
                Dimension = best.Dimension,
                Statement = $"CML shows weak preference for D={best.Dimension} (margin={margin:F3}).",
                Classification = "CONDITIONAL"
            });
        else
            comps.Add(new ProxyCmlComparison
            {
                Dimension = best.Dimension,
                Statement = "CML results show no clear dimensional preference under current parameter range.",
                Classification = "INCONCLUSIVE"
            });

        // Bridge-band selectivity check.
        var narrowest = cmlResults.MinBy(r => r.BridgeBandWidth)!;
        var widest = cmlResults.MaxBy(r => r.BridgeBandWidth)!;
        double bridgeRatio = narrowest.BridgeBandWidth > 0
            ? widest.BridgeBandWidth / narrowest.BridgeBandWidth : 1;
        comps.Add(new ProxyCmlComparison
        {
            Dimension = 0,
            Statement = bridgeRatio > 2
                ? "CML bridge-band widths vary significantly across dimensions."
                : "CML bridge-band selectivity is weak under current parameter range.",
            Classification = bridgeRatio > 3 ? "SUPPORTED" : bridgeRatio > 1.5 ? "CONDITIONAL" : "INCONCLUSIVE"
        });

        return comps;
    }
}
