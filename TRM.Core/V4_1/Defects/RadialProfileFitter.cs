namespace TRM.Core.V4_1.Defects;

/// <summary>
/// Fits radial profile data against candidate decay laws.
/// Classification: FRAMEWORK — curve fitting, no physical claims.
/// </summary>
public static class RadialProfileFitter
{
    public static ProfileFitResult FitPowerLaw(List<RadialProfileSample> profile, double exponent)
    {
        if (profile.Count < 3)
            return new ProfileFitResult { Model = $"1/r^{exponent}", Rmse = double.MaxValue, RSquared = 0 };

        // Fit: A(r) ≈ C / r^exp  →  log A ≈ log C - exp * log r
        // For fixed exponent, find best C by least squares.
        double sumY = 0, sumX = 0;
        int n = profile.Count;
        foreach (var p in profile)
        {
            double r = Math.Max(p.Distance, 1);
            sumY += Math.Log(Math.Max(p.MeanAmplitude, 1e-15));
            sumX += Math.Log(r);
        }
        double logC = (sumY + exponent * sumX) / n;
        double C = Math.Exp(logC);

        double ssRes = 0, ssTot = 0;
        double meanA = profile.Average(p => p.MeanAmplitude);
        foreach (var p in profile)
        {
            double pred = C / Math.Pow(Math.Max(p.Distance, 1), exponent);
            ssRes += (p.MeanAmplitude - pred) * (p.MeanAmplitude - pred);
            ssTot += (p.MeanAmplitude - meanA) * (p.MeanAmplitude - meanA);
        }
        double rmse = Math.Sqrt(ssRes / n);
        double r2 = ssTot > 0 ? 1.0 - ssRes / ssTot : 0;

        return new ProfileFitResult { Model = $"1/r^{exponent}", Rmse = rmse, RSquared = r2, Parameters = [C, exponent] };
    }

    public static ProfileFitResult FitExponentialCutoff(List<RadialProfileSample> profile)
    {
        // A(r) ≈ C * exp(-r/xi) / r  → simplified to A(r)·r ≈ C * exp(-r/xi)
        // Log-linear fit: log(A*r) ≈ log C - r/xi.
        if (profile.Count < 3)
            return new ProfileFitResult { Model = "exp(-r/xi)/r", Rmse = double.MaxValue, RSquared = 0 };

        double sx = 0, sy = 0, sxy = 0, sx2 = 0;
        int n = profile.Count;
        foreach (var p in profile)
        {
            double r = Math.Max(p.Distance, 1);
            double y = Math.Log(Math.Max(p.MeanAmplitude * r, 1e-15));
            sx += r; sy += y; sxy += r * y; sx2 += r * r;
        }
        double xiInv = (n * sxy - sx * sy) / Math.Max(n * sx2 - sx * sx, 1e-15);
        double logC = (sy + xiInv * sx) / n;

        double ssRes = 0, ssTot = 0;
        double meanA = profile.Average(p => p.MeanAmplitude);
        foreach (var p in profile)
        {
            double pred = Math.Exp(logC) * Math.Exp(-Math.Abs(xiInv) * p.Distance) / Math.Max(p.Distance, 1);
            ssRes += (p.MeanAmplitude - pred) * (p.MeanAmplitude - pred);
            ssTot += (p.MeanAmplitude - meanA) * (p.MeanAmplitude - meanA);
        }
        return new ProfileFitResult
        {
            Model = "exp(-r/xi)/r", Rmse = Math.Sqrt(ssRes / n),
            RSquared = ssTot > 0 ? 1.0 - ssRes / ssTot : 0,
            Parameters = [Math.Exp(logC), Math.Abs(xiInv) > 0 ? 1.0 / Math.Abs(xiInv) : double.MaxValue]
        };
    }
}
