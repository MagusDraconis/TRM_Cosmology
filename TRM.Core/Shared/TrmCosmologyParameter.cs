using System;
using System.Collections.Generic;

namespace TRM.Core;

public enum TrmParameterStatus
{
    Derived,
    Fitted,
    Calibrated
}

public sealed record TrmParameterTrace(
    string Name,
    double Value,
    string Unit,
    TrmParameterStatus Status,
    string DerivationEquation,
    string ReferencePath,
    string Notes);

/// <summary>
/// Central provider for current TRM cosmological scaling parameters used by CMB and Pantheon pipelines.
/// Theory/review links: docs/review/TRM_Service_Test_Consolidation.md and docs/review/TRM_Real_Physics_Test_Coverage.md.
/// Status: calibrated (current operational values), tested (via core cosmology tests), not derived yet (explicit TODO derivation path), limitation (no first-principles closure yet).
/// Related tests: TRM.Tests/CoreTests/ClockworkCosmologyTests.cs.
/// </summary>
public sealed record TrmCosmologyParameters(
    double BetaEta,
    double Alpha,
    double HT)
{
        public const double DefaultBetaEta = 0.005;
        public const double DefaultAlpha = 6.8;
        public const double DefaultHT = 70.30;

        /// <summary>
        /// Returns the active cosmology parameter tuple consumed by TRM distance/CMB services.
        /// Status: calibrated + tested in integration tests; not derived yet as a closed theoretical set.
        /// Related docs: docs/review/TRM_Real_Physics_Test_Coverage.md (Pantheon/HT and CMB sections).
    /// </summary>
    public static TrmCosmologyParameters Current()
    {
        return new TrmCosmologyParameters(
            BetaEta: GetBetaEta(),
            Alpha: GetAlpha(),
            HT: GetHT());
    }

    /// <summary>
    /// Current placeholder for the dimensionless TRM drift per solver-time unit.
    /// TODO: Replace with a derivation from temporal drift dynamics.
    /// </summary>
    public static double GetBetaEta()
    {
        return DefaultBetaEta;
    }

    /// <summary>
    /// Current placeholder for solver-time to TRM-time scaling.
    /// TODO: Replace with a derivation from the TQM tick/action bridge or recombination condition.
    /// </summary>
    public static double GetAlpha()
    {
        return DefaultAlpha;
    }


    /// <summary>
    /// Current Pantheon-calibrated TRM cosmological distance pacing in km/s/Mpc.
    /// 
    /// Calibration status:
    /// - Fine scan over Pantheon+SH0ES scale-distance residuals gives best RMS near HT ≈ 70.30.
    /// - At HT ≈ 70.30 the mean residual is approximately zero.
    /// - This value should still be treated as a calibrated working value, not yet as a fundamental derivation.
    /// 
    /// TODO:
    /// - Replace with result from a unified TRM distance relation.
    /// - Derive HT from TQM/TRM dynamics rather than Pantheon calibration.
    /// </summary>
    public static double GetHT()
    {
        return DefaultHT;
    }

    /// <summary>
    /// Effective temporal-drift product used by the CMB relation:
    /// z_rec = exp((BetaEta * Alpha) * eta_rec) - 1.
    /// </summary>
    public static double GetCmbTemporalDriftProduct()
    {
        return GetBetaEta() * GetAlpha();
    }

    /// <summary>
    /// Derives betaEta from a CMB recombination reference tuple.
    /// Relation: 1+z = exp(betaEta * alpha * etaRec).
    /// </summary>
    public static double DeriveBetaEtaFromCmbReference(
        double zRec,
        double alpha,
        double etaRec)
    {
        if (zRec <= -1.0)
            throw new ArgumentOutOfRangeException(nameof(zRec), "zRec must be greater than -1.");
        if (alpha <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(alpha), "alpha must be positive.");
        if (etaRec <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(etaRec), "etaRec must be positive.");

        return Math.Log(1.0 + zRec) / (alpha * etaRec);
    }

    /// <summary>
    /// Derives alpha from a CMB recombination reference tuple.
    /// Relation: 1+z = exp(betaEta * alpha * etaRec).
    /// </summary>
    public static double DeriveAlphaFromCmbReference(
        double zRec,
        double betaEta,
        double etaRec)
    {
        if (zRec <= -1.0)
            throw new ArgumentOutOfRangeException(nameof(zRec), "zRec must be greater than -1.");
        if (betaEta <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(betaEta), "betaEta must be positive.");
        if (etaRec <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(etaRec), "etaRec must be positive.");

        return Math.Log(1.0 + zRec) / (betaEta * etaRec);
    }

    /// <summary>
    /// Derives HT from a TRM base-distance anchor.
    /// Relation: D_base(z) = (c / HT) * ln(1 + z).
    /// </summary>
    public static double DeriveHTFromDistanceAnchor(
        double z,
        double baseDistanceMpc)
    {
        if (z < 0.0)
            throw new ArgumentOutOfRangeException(nameof(z), "z must be non-negative.");
        if (baseDistanceMpc <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(baseDistanceMpc), "baseDistanceMpc must be positive.");

        return (PhysicalConstants.C_Kms * Math.Log(1.0 + z)) / baseDistanceMpc;
    }

    /// <summary>
    /// Trace metadata for central cosmology parameters.
    /// Values are currently operational calibrated defaults with explicit derivation equations.
    /// </summary>
    public static IReadOnlyList<TrmParameterTrace> GetDerivationTrace()
    {
        return new[]
        {
            new TrmParameterTrace(
                Name: "BetaEta",
                Value: GetBetaEta(),
                Unit: "1/eta-unit",
                Status: TrmParameterStatus.Calibrated,
                DerivationEquation: "betaEta = ln(1 + zRec) / (alpha * etaRec)",
                ReferencePath: "TRM.Core/Domains/Domain3.Cmb/CmbAcousticSolver.cs::CalculateTrmRecombinationRedshift",
                Notes: "Current operational value; equation path is implemented, anchor tuple not first-principles closed yet."),
            new TrmParameterTrace(
                Name: "Alpha",
                Value: GetAlpha(),
                Unit: "dimensionless",
                Status: TrmParameterStatus.Calibrated,
                DerivationEquation: "alpha = ln(1 + zRec) / (betaEta * etaRec)",
                ReferencePath: "TRM.Core/Domains/Domain3.Cmb/CmbAcousticSolver.cs::CalculateTrmRecombinationRedshift",
                Notes: "Current operational value; tied to the same CMB exponential drift relation."),
            new TrmParameterTrace(
                Name: "HT",
                Value: GetHT(),
                Unit: "km/s/Mpc",
                Status: TrmParameterStatus.Calibrated,
                DerivationEquation: "HT = c * ln(1 + z) / D_base(z)",
                ReferencePath: "TRM.Core/Shared/TrmDistanceMapper.cs::CalculateTrmBaseDistance",
                Notes: "Pantheon-calibrated working value; explicit distance-anchor inversion is available.")
        };
    }

    public static TrmParameterTrace GetDerivationTrace(string parameterName)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
            throw new ArgumentException("Parameter name must not be empty.", nameof(parameterName));

        foreach (var trace in GetDerivationTrace())
        {
            if (string.Equals(trace.Name, parameterName, StringComparison.OrdinalIgnoreCase))
                return trace;
        }

        throw new ArgumentOutOfRangeException(
            nameof(parameterName),
            $"Unknown cosmology parameter trace: '{parameterName}'.");
    }


    /// <summary>
    /// Returns the current working TRM cosmological parameter set,
    /// but with an explicitly supplied HT value.
    /// Useful for HT sensitivity tests.
    /// </summary>
    public static TrmCosmologyParameters WithHT(double ht)
    {
        return new TrmCosmologyParameters(
            BetaEta: GetBetaEta(),
            Alpha: GetAlpha(),
            HT: ht);
    }

    /// <summary>
    /// Returns the current working TRM cosmological parameter set
    /// with optional explicit overrides.
    /// Useful for sensitivity tests without changing the central defaults.
    /// </summary>
    public static TrmCosmologyParameters WithOverrides(
        double? betaEta = null,
        double? alpha = null,
        double? ht = null)
    {
        return new TrmCosmologyParameters(
            BetaEta: betaEta ?? GetBetaEta(),
            Alpha: alpha ?? GetAlpha(),
            HT: ht ?? GetHT());
    }
}
