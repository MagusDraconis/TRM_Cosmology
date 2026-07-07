namespace TRM.Core.V4_1;

/// <summary>
/// Immutable container for the three diagnostic observables
/// entering the dimensional selection functional F(D).
/// Classification: FRAMEWORK — defines measurable quantities;
/// does not assert that any particular D is selected.
/// </summary>
public sealed class DimensionObservables
{
    /// <summary>Spectral balance: lambda_2 / lambda_max.</summary>
    public double SpectralBalance { get; }

    /// <summary>Propagation isotropy: 1 - sigma_theta / mean_v_theta.</summary>
    public double PropagationIsotropy { get; }

    /// <summary>Bridge-band sharpness: 1 / Delta_Omega.</summary>
    public double BridgeBandSharpness { get; }

    /// <summary>Effective dimension D of the graph ensemble.</summary>
    public int Dimension { get; }

    public DimensionObservables(
        int dimension,
        double spectralBalance,
        double propagationIsotropy,
        double bridgeBandSharpness)
    {
        if (dimension < 1)
            throw new ArgumentOutOfRangeException(nameof(dimension), "Dimension must be positive.");
        if (!double.IsFinite(spectralBalance) || spectralBalance <= 0)
            throw new ArgumentOutOfRangeException(nameof(spectralBalance), "Must be finite and positive.");
        if (!double.IsFinite(propagationIsotropy))
            throw new ArgumentOutOfRangeException(nameof(propagationIsotropy), "Must be finite.");
        if (!double.IsFinite(bridgeBandSharpness) || bridgeBandSharpness <= 0)
            throw new ArgumentOutOfRangeException(nameof(bridgeBandSharpness), "Must be finite and positive.");

        Dimension = dimension;
        SpectralBalance = spectralBalance;
        PropagationIsotropy = propagationIsotropy;
        BridgeBandSharpness = bridgeBandSharpness;
    }
}
