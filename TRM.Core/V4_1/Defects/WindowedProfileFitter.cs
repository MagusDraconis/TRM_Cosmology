namespace TRM.Core.V4_1.Defects;

/// <summary>
/// Fits power-law profiles in separate radial windows.
/// Classification: FRAMEWORK — curve fitting, no claims.
/// </summary>
public static class WindowedProfileFitter
{
    public static List<WindowedProfileFitResult> FitWindows(
        List<RadialProfileSample> profile, RadialRegimeConfig config)
    {
        var results = new List<WindowedProfileFitResult>();

        // Inner window: first config.InnerShells shells.
        var inner = profile.Take(config.InnerShells).ToList();
        results.Add(FitPowerLaw(inner, "inner"));

        // Outer window: shells from OuterShellStart onward.
        var outer = profile.Skip(config.OuterShellStart).ToList();
        results.Add(FitPowerLaw(outer, "outer"));

        // Full profile.
        results.Add(FitPowerLaw(profile, "full"));

        return results;
    }

    private static WindowedProfileFitResult FitPowerLaw(
        List<RadialProfileSample> window, string label)
    {
        if (window.Count < 2)
            return new WindowedProfileFitResult(label, 0, double.MaxValue, 0, window.Count);

        // Log-log linear fit: log A = log C - p * log r.
        double sx = 0, sy = 0, sxy = 0, sx2 = 0;
        int n = window.Count;
        foreach (var p in window)
        {
            double lr = Math.Log(Math.Max(p.Distance, 1));
            double la = Math.Log(Math.Max(p.MeanAmplitude, 1e-15));
            sx += lr; sy += la; sxy += lr * la; sx2 += lr * lr;
        }
        double pExp = -(n * sxy - sx * sy) / Math.Max(n * sx2 - sx * sx, 1e-15);
        double logC = (sy + pExp * sx) / n;
        double C = Math.Exp(logC);

        double ssRes = 0, ssTot = 0;
        double meanA = window.Average(w => w.MeanAmplitude);
        foreach (var s in window)
        {
            double pred = C / Math.Pow(Math.Max(s.Distance, 1), pExp);
            ssRes += (s.MeanAmplitude - pred) * (s.MeanAmplitude - pred);
            ssTot += (s.MeanAmplitude - meanA) * (s.MeanAmplitude - meanA);
        }
        double rmse = Math.Sqrt(ssRes / n);
        double r2 = ssTot > 0 ? 1.0 - ssRes / ssTot : 0;

        return new WindowedProfileFitResult(label, pExp, rmse, r2, n);
    }
}
