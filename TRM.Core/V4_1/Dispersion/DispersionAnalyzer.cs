namespace TRM.Core.V4_1.Dispersion;

/// <summary>
/// Determines wavelength-dependent mode frequencies by exciting sine modes
/// on regular lattice graphs and estimating the oscillation frequency from
/// time-series zero crossings.
/// Classification: FRAMEWORK — numerical measurement, no claims.
/// </summary>
public static class DispersionAnalyzer
{
    /// <summary>
    /// Estimate omega for a sine mode of wave-number k on a 1D chain.
    /// Uses deterministic sine-coupled map propagation and zero-crossing analysis.
    /// </summary>
    public static double EstimateFrequency1D(int chainLength, double k, double coupling, int steps, int seed)
    {
        int N = chainLength;
        var x = new double[N];
        var v = new double[N];
        for (int i = 0; i < N; i++)
        {
            x[i] = Math.Sin(k * i);
            v[i] = coupling * Math.Cos(k * i);
        }

        double dt = 0.1;
        var history = new List<double>();
        int center = N / 2;

        for (int t = 0; t < steps; t++)
        {
            for (int i = 0; i < N; i++)
            {
                double lap = -2.0 * x[i];
                if (i > 0) lap += x[i - 1];
                if (i < N - 1) lap += x[i + 1];
                v[i] += dt * coupling * coupling * lap;
            }
            for (int i = 0; i < N; i++)
                x[i] += dt * v[i];

            if (t > steps - 100)
                history.Add(x[center]);
        }

        int crossings = 0;
        for (int i = 1; i < history.Count; i++)
            if (history[i - 1] * history[i] < 0) crossings++;
        if (crossings < 2) return 0;
        double period = 2.0 * history.Count / Math.Max(crossings, 1);
        return 2.0 * Math.PI / period;
    }

    /// <summary>Build dispersion samples for a 1D chain across wave-numbers.</summary>
    public static DispersionCurve SampleChainDispersion(int chainLength, double coupling, int steps, int seed)
    {
        int numKs = Math.Min(chainLength / 2, 5);
        var samples = new List<DispersionSample>();
        for (int m = 1; m <= numKs; m++)
        {
            double k = 2.0 * Math.PI * m / chainLength;
            double omega = EstimateFrequency1D(chainLength, k, coupling, steps, seed + m);
            if (omega > 0) samples.Add(new DispersionSample(k, omega));
        }
        var fit = FitLinear(samples);
        return new DispersionCurve(1, "x", samples, fit);
    }

    /// <summary>Linear least-squares fit omega = slope * k + intercept.</summary>
    public static DispersionFitResult FitLinear(List<DispersionSample> samples)
    {
        if (samples.Count < 2)
            return new DispersionFitResult(0, 0, double.MaxValue, samples.Count);

        double sx = 0, sy = 0, sxy = 0, sx2 = 0;
        int n = samples.Count;
        foreach (var s in samples)
        {
            sx += s.K; sy += s.Omega; sxy += s.K * s.Omega; sx2 += s.K * s.K;
        }
        double slope = (n * sxy - sx * sy) / Math.Max(n * sx2 - sx * sx, 1e-15);
        double intercept = (sy - slope * sx) / n;

        double error = 0;
        foreach (var s in samples)
        {
            double pred = slope * s.K + intercept;
            error += (s.Omega - pred) * (s.Omega - pred);
        }
        error = Math.Sqrt(error / n);

        return new DispersionFitResult(slope, intercept, error, n);
    }

    private static double Map(double x) => Math.Sin(x);
}
