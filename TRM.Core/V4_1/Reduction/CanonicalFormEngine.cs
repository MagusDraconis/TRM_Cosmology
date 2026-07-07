namespace TRM.Core.V4_1.Reduction;

/// <summary>
/// Compares canonical block-structured forms of the effective D-score equation.
/// Classification: FRAMEWORK — form comparison, no claims.
/// </summary>
public static class CanonicalFormEngine
{
    private static readonly Dictionary<int, double> Target = new()
    {
        [1] = 0.18, [2] = 0.52, [3] = 0.83, [4] = 0.42
    };

    // Per-term per-D scores.
    private static readonly Dictionary<string, double[]> TermScores = new()
    {
        ["spectral"] = [0.2, 0.5, 0.8, 0.4],
        ["defect"] = [0.1, 0.4, 0.9, 0.5],
        ["sync"] = [0.3, 0.6, 0.7, 0.4],
        ["outer"] = [0.2, 0.5, 0.7, 0.3],
    };

    private static double TermVal(string name, int d) => TermScores[name][d - 1];

    public static CanonicalFormAuditResult Audit()
    {
        // Candidate forms with block definitions.
        var forms = new List<CanonicalEquationForm>
        {
            new("Core + Correction", "Core(D) + Correction(D)",
                [new("Core", ["spectral","defect","sync"], 0.75), new("Correction", ["outer"], 0.10)]),
            new("Benefit - Penalty", "Benefit(D) - Penalty(D)",
                [new("Benefit", ["spectral","defect"], 0.65), new("Penalty", ["sync"], 0.20)]),
            new("Benefit - Penalty + Correction", "Benefit(D) - Penalty(D) + Correction(D)",
                [new("Benefit", ["spectral","defect"], 0.55), new("Penalty", ["sync"], 0.15), new("Correction", ["outer"], 0.10)]),
            new("Core + GeometricCorrection", "Core(D) + GeoCorrection(D)",
                [new("Core", ["spectral","defect","sync"], 0.75), new("GeoCorrection", ["outer"], 0.10)]),
        };

        var fits = new List<CanonicalFormFitResult>();
        foreach (var form in forms)
            fits.Add(Evaluate(form));

        var bestFit = fits.MinBy(f => f.Rmse)!;
        var preferred = fits.OrderByDescending(f => f.InterpretabilityScore * (1.0 - f.Rmse)).First();

        return new CanonicalFormAuditResult(fits, preferred, bestFit);
    }

    private static CanonicalFormFitResult Evaluate(CanonicalEquationForm form)
    {
        var pred = new Dictionary<int, double>();
        double[] scores = [0, 0, 0, 0];
        int sign = 1;
        foreach (var block in form.Blocks)
        {
            sign = block.Name.Contains("Penalty") ? -1 : 1;
            foreach (var term in block.Terms)
                for (int d = 1; d <= 4; d++)
                    scores[d - 1] += sign * block.Weight * TermVal(term, d) / block.Terms.Count;
        }
        for (int d = 1; d <= 4; d++) pred[d] = scores[d - 1] + 0.15;

        int opt = pred.MaxBy(kv => kv.Value).Key;
        double rmse = Math.Sqrt(Target.Keys.Average(d => Math.Pow(Target[d] - pred[d], 2)));
        double ssTot = Target.Values.Sum(v => Math.Pow(v - Target.Values.Average(), 2));
        double r2 = ssTot > 0 ? 1.0 - rmse * rmse * 4 / ssTot : 0;

        // Interpretability: fewer blocks + low redundancy → higher score.
        double interp = 1.0 / (form.Blocks.Count * 0.5 + 0.5);
        if (form.Blocks.Count <= 2) interp += 0.1;

        string status = r2 > 0.85 ? "PREFERRED" : r2 > 0.7 ? "VIABLE" : r2 > 0.5 ? "OVER-COMPLICATED" : "INSUFFICIENT";
        if (r2 > 0.85 && form.Blocks.Count <= 2) status = "PREFERRED";
        else if (r2 > 0.7) status = "VIABLE";

        return new CanonicalFormFitResult(form, pred, rmse, r2, interp, opt, status);
    }
}
