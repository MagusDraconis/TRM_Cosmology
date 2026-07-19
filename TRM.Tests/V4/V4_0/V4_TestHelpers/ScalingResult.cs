namespace TRM.Tests.V4;

/// <summary>
/// Result of asymptotic power-law fitting for a (δK, F) pair.
///
/// δρ_eff(r) ~ r^α  →  α = Alpha
/// a(r) ~ r^β        →  β = Beta
/// </summary>
public sealed record ScalingResult(
    string KProfile,
    string FMap,
    double Alpha,
    double Beta,
    V4Classification Classification,
    string Note = "");
