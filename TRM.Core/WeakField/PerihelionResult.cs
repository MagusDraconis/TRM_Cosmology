namespace TRM.Core.WeakField;

public sealed record PerihelionResult(
    string Planet,
    double GrArcSecPerCentury,
    double TrmArcSecPerCentury,
    double EinsteinArcSecPerCentury,
    double EphemerisArcSecPerCentury,
    double AbsoluteDeltaTrmToEphemeris,
    double PpnFactor,
    double BetaPpn,
    double GammaPpn,
    bool IsWithinTolerance);
