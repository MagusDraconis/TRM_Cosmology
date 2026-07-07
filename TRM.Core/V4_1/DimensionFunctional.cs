namespace TRM.Core.V4_1;

/// <summary>
/// Computes the dimensional selection functional F(D).
///
/// F(D) = a1 * S1 + a2 * S2 + a3 * S3
///
/// S1 = lambda_2 / lambda_max   (spectral balance)
/// S2 = 1 - sigma_theta / v_theta_mean   (propagation isotropy)
/// S3 = 1 / Delta_Omega          (bridge-band sharpness)
///
/// Classification: FRAMEWORK — defines a scalar objective function.
/// Weights a1, a2, a3 are free parameters to be calibrated.
/// No claim is made that F(D) peaks at D = 3.
/// </summary>
public static class DimensionFunctional
{
    /// <summary>
    /// Computes F(D) from observables with given weights.
    /// All weights must be non-negative.
    /// </summary>
    public static double Compute(
        DimensionObservables obs,
        double a1,
        double a2,
        double a3)
    {
        if (a1 < 0 || a2 < 0 || a3 < 0)
            throw new ArgumentOutOfRangeException("Weights must be non-negative.");

        return a1 * obs.SpectralBalance
             + a2 * obs.PropagationIsotropy
             + a3 * obs.BridgeBandSharpness;
    }

    /// <summary>
    /// Computes F(D) with default equal weights (a1 = a2 = a3 = 1).
    /// </summary>
    public static double Compute(DimensionObservables obs) => Compute(obs, 1.0, 1.0, 1.0);

    /// <summary>
    /// Returns the individual terms for diagnostic inspection.
    /// </summary>
    public static (double spectralTerm, double isotropyTerm, double sharpnessTerm)
        Decompose(DimensionObservables obs, double a1, double a2, double a3)
    {
        if (a1 < 0 || a2 < 0 || a3 < 0)
            throw new ArgumentOutOfRangeException("Weights must be non-negative.");

        return (a1 * obs.SpectralBalance,
                a2 * obs.PropagationIsotropy,
                a3 * obs.BridgeBandSharpness);
    }
}
