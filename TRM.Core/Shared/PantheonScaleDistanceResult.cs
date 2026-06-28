namespace TRM.Core;

public record PantheonScaleDistanceResult(
    int Count,
    double HT,
    double BetaEta,
    double Alpha,
    double RmsError,
    double MeanResidual,
    double MeanAbsResidual,
    double MaxAbsResidual,
    double CenteredRmsError
);
