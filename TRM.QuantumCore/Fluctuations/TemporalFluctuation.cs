using System;
using System.Collections.Generic;
using System.Text;

namespace TRM.QuantumCore.Fluctuations;

public class TemporalFluctuation
{
    private readonly double tP;
    private readonly Random rng = new();

    public TemporalFluctuation(double planckTime)
    {
        tP = planckTime;
    }

    public double Sample(double deltaT)
    {
        double baseVal = tP / deltaT;

        // Gaussian approximation
        double u1 = 1.0 - rng.NextDouble();
        double u2 = 1.0 - rng.NextDouble();
        double randStdNormal =
            Math.Sqrt(-2.0 * Math.Log(u1)) *
            Math.Sin(2.0 * Math.PI * u2);

        return baseVal * (1 + 0.1 * randStdNormal);
    }
}

