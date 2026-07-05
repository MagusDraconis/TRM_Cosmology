namespace TRM.Tests.V4.V4_TestHelpers;

/// <summary>
/// Coupling perturbation profiles δK(r) for B1 mechanism tests.
///
/// Each profile models how a point mass M perturbs the oscillator coupling K
/// as a function of radial distance r.
///
/// K1: δK(r) = k·M / r          — 3D Laplace Green's function (natural isotropic)
/// K2: δK(r) = k·M / r²         — inverse-square (dipole / flux-conservation)
/// K3: δK(r) = k·M·exp(−r/r₀)   — Yukawa-like screening
/// </summary>
public static class CouplingProfiles
{
    /// <summary>
    /// K1 — Inverse-distance (3D Laplace Green's function).
    /// δK(r) ∝ 1/r. Natural isotropic response of a second-order coupling field.
    /// Asymptotic: δK(r) ~ r^(−1).
    /// </summary>
    public static double K1(double r, double k, double M)
    {
        if (r <= 0) throw new ArgumentOutOfRangeException(nameof(r), "r must be positive");
        return k * M / r;
    }

    /// <summary>
    /// K2 — Inverse-square.
    /// δK(r) ∝ 1/r². Dipole-like or flux-conserving behavior.
    /// Asymptotic: δK(r) ~ r^(−2).
    /// </summary>
    public static double K2(double r, double k, double M)
    {
        if (r <= 0) throw new ArgumentOutOfRangeException(nameof(r), "r must be positive");
        return k * M / (r * r);
    }

    /// <summary>
    /// K3 — Exponential (Yukawa-like screening).
    /// δK(r) ∝ exp(−r/r₀). Massive coupling-field behavior.
    /// Asymptotic: δK(r) ~ exp(−r/r₀) — not a power law.
    /// </summary>
    public static double K3(double r, double k, double M, double r0)
    {
        if (r <= 0) throw new ArgumentOutOfRangeException(nameof(r), "r must be positive");
        return k * M * Math.Exp(-r / r0);
    }

    /// <summary>
    /// Compute the asymptotic power-law exponent for δK(r) ~ r^α
    /// by evaluating at two widely separated radial points and taking
    /// α = log(δK(r₂)/δK(r₁)) / log(r₂/r₁).
    /// </summary>
    public static double ComputeAsymptoticAlpha(
        Func<double, double> deltaK,
        double rNear = 10.0,
        double rFar = 1000.0)
    {
        double kNear = deltaK(rNear);
        double kFar = deltaK(rFar);

        if (kNear <= 0 || kFar <= 0)
            return double.NaN;

        return Math.Log(kFar / kNear) / Math.Log(rFar / rNear);
    }

    /// <summary>
    /// Compute the known analytic α for each K-profile (for validation).
    /// K1 = −1.0, K2 = −2.0, K3 = undefined (returns NaN).
    /// </summary>
    public static double ExpectedAlpha(string kProfile)
    {
        return kProfile switch
        {
            "K1" => -1.0,
            "K2" => -2.0,
            "K3" => double.NaN,   // exponential — no power-law asymptote
            _ => throw new ArgumentException($"Unknown K-profile: {kProfile}")
        };
    }

    /// <summary>
    /// All K-profile identifiers for matrix iteration.
    /// </summary>
    public static readonly string[] AllKProfiles = { "K1", "K2", "K3" };
}
