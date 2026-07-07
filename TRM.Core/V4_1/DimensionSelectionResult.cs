namespace TRM.Core.V4_1;

/// <summary>
/// Immutable container for the three diagnostic terms and the combined
/// dimensional selection functional F(D) computed from a GraphDimensionScenario.
///
/// Classification: FRAMEWORK — holds computed values; no selection claim.
/// </summary>
public sealed class DimensionSelectionResult
{
    public int Dimension { get; }

    /// <summary>S₁ = λ₂ / λ_max  (spectral balance).</summary>
    public double SpectralBalance { get; }

    /// <summary>S₂ = 1 − σ_θ / ⟨v_θ⟩  (propagation isotropy).</summary>
    public double PropagationIsotropy { get; }

    /// <summary>S₃ = 1 / ΔΩ  (bridge-band sharpness).</summary>
    public double BridgeBandSharpness { get; }

    /// <summary>F(D) = a₁·S₁ + a₂·S₂ + a₃·S₃.</summary>
    public double FunctionalValue { get; }

    /// <summary>Weights used.</summary>
    public double A1 { get; }
    public double A2 { get; }
    public double A3 { get; }

    public DimensionSelectionResult(
        int dimension,
        double spectralBalance,
        double propagationIsotropy,
        double bridgeBandSharpness,
        double functionalValue,
        double a1, double a2, double a3)
    {
        Dimension = dimension;
        SpectralBalance = spectralBalance;
        PropagationIsotropy = propagationIsotropy;
        BridgeBandSharpness = bridgeBandSharpness;
        FunctionalValue = functionalValue;
        A1 = a1; A2 = a2; A3 = a3;
    }
}
