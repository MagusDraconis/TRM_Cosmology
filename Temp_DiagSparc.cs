using System;
using TRM.CMD;
try {
    var r = SparcGlobalLapseResidualAnalyzer.Run();
    Console.WriteLine($"Eta0={r.Eta0:E6} Unc={r.Eta0Uncertainty:E6} |Eta0|<Unc={Math.Abs(r.Eta0) < r.Eta0Uncertainty}");
    Console.WriteLine($"Eta1Lin={r.Eta1Linear:E6} Unc={r.Eta1LinearUncertainty:E6} |E1L|<Unc={Math.Abs(r.Eta1Linear) < r.Eta1LinearUncertainty}");
    Console.WriteLine($"Eta1Log={r.Eta1Log:E6} Unc={r.Eta1LogUncertainty:E6} |E1Lg|<Unc={Math.Abs(r.Eta1Log) < r.Eta1LogUncertainty}");
    Console.WriteLine($"Lambda={r.Lambda:F8} Unc={r.LambdaUncertainty:F8} |L-1|<Unc={Math.Abs(r.Lambda - 1.0) < r.LambdaUncertainty}");
    Console.WriteLine($"OffsetSig={r.OffsetImprovesFitSignificantly} LambdaSig={r.LambdaImprovesFitSignificantly}");
    Console.WriteLine($"Robust={r.IsRobustAcrossSubsamples} DrivenBySingle={r.DrivenBySingleGalaxy} HasEvidence={r.HasEvidenceForGlobalTerm}");
    Console.WriteLine($"Verdict: {r.Verdict}");
    Console.WriteLine($"Pvals: E0={r.Eta0Fit?.PValue:E4} E1L={r.Eta1Fit?.PValue:E4} E1Lg={r.Eta1LogFit?.PValue:E4} Lam={r.LambdaAccelFit?.PValue:E4}");
} catch (Exception ex) { Console.WriteLine($"ERR: {ex.Message}"); }
