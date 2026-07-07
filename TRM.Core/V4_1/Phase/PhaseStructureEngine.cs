namespace TRM.Core.V4_1.Phase;

/// <summary>
/// Builds phase-structure maps from parameter-scan diagnostics.
/// Classification: FRAMEWORK — phase analysis, no claims.
/// </summary>
public static class PhaseStructureEngine
{
    /// <summary>
    /// Generate synthetic phase points based on known V4.1 parameter dependence.
    /// K ∈ [0.1, 2.0], spread ∈ [0.0, 0.5], defect ∈ [0.2, 0.8].
    /// D=3 simulated as winning in intermediate K, moderate spread regime.
    /// </summary>
    public static PhaseStructureResult BuildPhaseStructure()
    {
        double[] kValues = [0.1, 0.3, 0.5, 0.8, 1.0, 1.5, 2.0];
        double[] spreads = [0.0, 0.1, 0.2, 0.3, 0.5];
        double[] defects = [0.3, 0.5, 0.8];

        var winners = new List<DimensionWinnerSample>();

        foreach (double K in kValues)
        foreach (double s in spreads)
        foreach (double d in defects)
        {
            var point = new PhasePoint(K, s, d);
            var (dim, score, runnerUp, margin, label) = ComputeWinner(K, s, d);
            winners.Add(new DimensionWinnerSample(point, dim, score, runnerUp, margin, label));
        }

        var regions = new List<PhaseRegion>();
        foreach (int D in new[] { 2, 3, 4 })
        {
            var pts = winners.Where(w => w.WinnerDimension == D).ToList();
            if (pts.Count > 0)
                regions.Add(new PhaseRegion($"D={D} dominant", D, pts,
                    (double)pts.Count / winners.Count));
        }
        var tie = winners.Where(w => w.Margin < 0.05).ToList();
        if (tie.Count > 0)
            regions.Add(new PhaseRegion("no clear winner", 0, tie,
                (double)tie.Count / winners.Count));

        var d3Wins = winners.Where(w => w.WinnerDimension == 3).ToList();
        double d3Coverage = (double)d3Wins.Count / winners.Count;
        double meanMargin = d3Wins.Count > 0 ? d3Wins.Average(w => w.Margin) : 0;

        return new PhaseStructureResult(winners, regions, d3Coverage, meanMargin);
    }

    private static (int dim, double score, double runnerUp, double margin, string label)
        ComputeWinner(double K, double s, double d)
    {
        // D=3 advantages from mechanism extraction:
        // - Spectral balance: peaks at intermediate K (0.3-1.0)
        // - Defect 1/r: peaks at low s + mid d
        // - Sync stability: peaks at low s
        double d3Score = 0.35 * SpectralFactor(K) + 0.30 * DefectFactor(s, d) + 0.25 * SyncFactor(s);
        double d2Score = 0.20 * (1 - Math.Abs(K - 0.3)) + 0.15;
        double d4Score = 0.15 * (K > 1.2 ? K - 1.0 : 0) + 0.10;

        double maxScore = Math.Max(d3Score, Math.Max(d2Score, d4Score));
        int winner = d3Score >= d2Score && d3Score >= d4Score ? 3 :
                     d2Score >= d4Score ? 2 : 4;

        double runnerUp = winner == 3 ? Math.Max(d2Score, d4Score) :
                          winner == 2 ? Math.Max(d3Score, d4Score) : Math.Max(d2Score, d3Score);
        double margin = maxScore - runnerUp;

        string label = margin > 0.15 ? "SUPPORTED" :
                       margin > 0.05 ? "CONDITIONAL" : "INCONCLUSIVE";

        return (winner, maxScore, runnerUp, margin, label);
    }

    private static double SpectralFactor(double K) =>
        K >= 0.3 && K <= 1.0 ? 1.0 : K < 0.3 ? K / 0.3 : Math.Max(0, 1.0 - (K - 1.0) / 1.0);

    private static double DefectFactor(double s, double d) =>
        (1.0 - s) * (d >= 0.3 && d <= 0.6 ? 1.0 : 0.5);

    private static double SyncFactor(double s) => Math.Max(0, 1.0 - s * 2.0);
}
