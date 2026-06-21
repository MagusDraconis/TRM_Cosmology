using System;
using System.Collections.Generic;
using System.Linq;
using TRM.QuantumCore.Planck;

namespace TRM.Simulations.Experiments;

public class UncertaintyResult
{
    public double DeltaT { get; set; }
    public double MeanTemporalFluctuation { get; set; }
    public double StdTemporalFluctuation { get; set; }
    public double DeltaE { get; set; }
    public double Product { get; set; }
}

public class UncertaintyExperiment
{
    private readonly PlanckConstants _planck;
    private readonly DerivedConstants _derived;
    private readonly Random _rng = new();

    public UncertaintyExperiment(PlanckConstants planck)
    {
        _planck = planck;
        _derived = new DerivedConstants(planck);
    }

    public List<UncertaintyResult> Run(List<double> deltaTValues, int samplesPerStep = 5000)
    {
        var results = new List<UncertaintyResult>();

        // Planck-Energieskala
        double ePlanck = _derived.ReducedPlanck / _planck.tP;

        foreach (var deltaT in deltaTValues)
        {
            var fluctuations = new List<double>();

            for (int i = 0; i < samplesPerStep; i++)
            {
                // Gaussian noise
                double xi = NextGaussian();

                // TQF Ansatz
                double deltaTemporal = (_planck.tP / deltaT) * xi;
                fluctuations.Add(deltaTemporal);
            }

            double mean = fluctuations.Average();
            double variance = fluctuations.Select(x => Math.Pow(x - mean, 2)).Average();
            double std = Math.Sqrt(variance);

            // Energieunschärfe ~ Planck-Energieskala * zeitliche Fluktuation
            double deltaE = ePlanck * std;

            results.Add(new UncertaintyResult
            {
                DeltaT = deltaT,
                MeanTemporalFluctuation = mean,
                StdTemporalFluctuation = std,
                DeltaE = deltaE,
                Product = deltaE * deltaT
            });
        }

        return results;
    }

    private double NextGaussian()
    {
        double u1 = 1.0 - _rng.NextDouble();
        double u2 = 1.0 - _rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }

}
