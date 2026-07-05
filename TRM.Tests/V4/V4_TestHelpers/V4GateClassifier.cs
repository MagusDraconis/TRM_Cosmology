namespace TRM.Tests.V4.V4_TestHelpers;

/// <summary>
/// Gate classifier for V4 B1 asymptotic scaling.
///
/// Determines whether a (δK, F) pair reproduces Newtonian gravity
/// based on the asymptotic power-law exponents of δρ_eff(r) and a(r).
///
/// Targets:
///   α_target = −1   (δK ~ 1/r  →  δρ_eff ~ 1/r)
///   β_target = −2   (a ~ 1/r²)
///
/// Thresholds:
///   VALID:   |α + 1| &lt; 0.10  AND  |β + 2| &lt; 0.10
///   PARTIAL: |α + 1| &lt; 0.30  OR   |β + 2| &lt; 0.30
///   INVALID: otherwise
/// </summary>
public static class V4GateClassifier
{
    public const double ValidThreshold = 0.10;
    public const double PartialThreshold = 0.30;
    public const double AlphaTarget = -1.0;
    public const double BetaTarget = -2.0;

    /// <summary>
    /// Classify a (δK, F) pair based on its asymptotic exponents.
    /// </summary>
    public static V4Classification Classify(double alpha, double beta)
    {
        double alphaErr = Math.Abs(alpha - AlphaTarget);
        double betaErr = Math.Abs(beta - BetaTarget);

        if (alphaErr < ValidThreshold && betaErr < ValidThreshold)
            return V4Classification.Valid;

        if (alphaErr < PartialThreshold || betaErr < PartialThreshold)
            return V4Classification.Partial;

        return V4Classification.Invalid;
    }

    /// <summary>
    /// Classify and produce a full ScalingResult.
    /// </summary>
    public static ScalingResult Evaluate(string kProfile, string fMap, double alpha, double beta, string? note = null)
    {
        var classification = Classify(alpha, beta);
        return new ScalingResult(kProfile, fMap, alpha, beta, classification, note ?? string.Empty);
    }
}
