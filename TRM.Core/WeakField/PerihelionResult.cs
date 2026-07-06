namespace TRM.Core.WeakField;

public sealed record PerihelionResult(
    string Planet,
    double TheoryArcSecPerCentury,
    double EinsteinArcSecPerCentury,
    double EphemerisArcSecPerCentury,
    double AbsoluteDeltaToEphemeris,
    bool IsWithinTolerance);
