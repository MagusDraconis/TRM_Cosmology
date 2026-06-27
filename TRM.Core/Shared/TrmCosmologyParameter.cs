using System;
using System.Collections.Generic;
using System.Text;

namespace TRM.Core;

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
        return 0.005;
    }

    /// <summary>
    /// Current placeholder for solver-time to TRM-time scaling.
    /// TODO: Replace with a derivation from the TQM tick/action bridge or recombination condition.
    /// </summary>
    public static double GetAlpha()
    {
        return 6.8;
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
        return 70.30;
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
