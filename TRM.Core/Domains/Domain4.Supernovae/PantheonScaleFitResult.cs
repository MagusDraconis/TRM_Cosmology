namespace TRM.Core.Domains.Domain4.Supernovae;


public record PantheonScaleFitResult(
    int AnalyzedPoints,
    double RmsError,
    double MeanResidual,
    double MeanAbsResidual,
    double MaxAbsResidual,
    double CenteredRmsError
);
