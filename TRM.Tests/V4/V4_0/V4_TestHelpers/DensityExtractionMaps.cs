namespace TRM.Tests.V4.V4_TestHelpers;

/// <summary>
/// Density extraction maps F: δK(r) → δρ_eff(r).
///
/// Each map defines how the coupling perturbation translates into an
/// effective energy density perturbation.
///
/// F1 — Direct:         δρ_eff ∝ δK(r)            (simplest)
/// F2 — Gradient:       δρ_eff ∝ |∇(δK(r))|       (spatial variation)
/// F3 — Sync energy:    δρ_eff ∝ δK(r) · R²       (reduces to F1 when R constant)
/// F4 — Action density: δρ_eff ∝ δK(r)            (reduces to F1 in sync state)
/// </summary>
public static class DensityExtractionMaps
{
    private const double OrderParameterR = 0.889;   // from CML synchronization tests

    /// <summary>
    /// F1 — Direct coupling density.
    /// δρ_eff(r) = C · δK(r)
    /// Asymptotic α is inherited from δK.
    /// </summary>
    public static double F1_Direct(double deltaK, double scale = 1.0)
        => scale * deltaK;

    /// <summary>
    /// F2 — Coupling gradient density.
    /// δρ_eff(r) = C · |∇(δK(r))|
    /// Asymptotic α = α_K − 1 (one power steeper than δK).
    /// </summary>
    public static double F2_Gradient(double gradDeltaK, double scale = 1.0)
        => scale * Math.Abs(gradDeltaK);

    /// <summary>
    /// F3 — Synchronization energy proxy.
    /// δρ_eff(r) = C · δK(r) · R²
    /// In the synchronized state (R constant ≈ 0.889), this reduces to F1.
    /// </summary>
    public static double F3_SyncEnergy(double deltaK, double scale = 1.0)
        => scale * deltaK * OrderParameterR * OrderParameterR;

    /// <summary>
    /// F4 — Action density proxy.
    /// δρ_eff(r) = C · δK(r)
    /// From the coupling term in the Kuramoto Lagrangian: L_c ∝ K·cos(Δθ).
    /// In full sync (cos(0) = 1), reduces to F1.
    /// </summary>
    public static double F4_ActionDensity(double deltaK, double scale = 1.0)
        => scale * deltaK;   // cos(0) = 1 in full sync

    /// <summary>
    /// Compute the known analytic α for δρ_eff given a (K-profile, F-map) pair.
    ///
    /// F1/F3/F4: α = α_K           (inherit K's exponent)
    /// F2:       α = α_K − 1       (gradient adds one power)
    /// Where K1→−1, K2→−2, K3→NaN.
    /// </summary>
    public static double ExpectedRhoAlpha(string kProfile, string fMap)
    {
        double alphaK = CouplingProfiles.ExpectedAlpha(kProfile);
        if (double.IsNaN(alphaK)) return double.NaN;

        return fMap switch
        {
            "F1" or "F3" or "F4" => alphaK,
            "F2" => alphaK - 1.0,
            _ => throw new ArgumentException($"Unknown F-map: {fMap}")
        };
    }

    /// <summary>
    /// Compute the known analytic β (acceleration exponent) from α.
    /// a(r) ∝ ∇(δρ_eff) ∝ r^(α−1) → β = α − 1.
    /// </summary>
    public static double ExpectedBeta(double alpha)
    {
        if (double.IsNaN(alpha)) return double.NaN;
        return alpha - 1.0;
    }

    /// <summary>
    /// Compute gradient of δK numerically at radius r.
    /// Uses central finite difference for accuracy.
    /// </summary>
    public static double ComputeGradient(Func<double, double> deltaK, double r, double dr = 0.001)
    {
        double forward = deltaK(r + dr);
        double backward = deltaK(r - dr);
        return (forward - backward) / (2.0 * dr);
    }

    /// <summary>
    /// All F-map identifiers for matrix iteration.
    /// </summary>
    public static readonly string[] AllFMaps = { "F1", "F2", "F3", "F4" };
}
