namespace TRM.Core.WeakField;

public sealed record PpnResult(
    double BetaPpn,
    double GammaPpn,
    double BetaDelta,
    double GammaDelta,
    double CassiniGammaBound,
    double CombinedBetaBound,
    bool IsWithinBounds,
    bool IsOptimizedZone);
