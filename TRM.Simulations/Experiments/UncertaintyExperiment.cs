using System;
using System.Collections.Generic;
using System.Linq;
using TRM.QuantumCore.Planck;

namespace TRM.Simulations.Experiments;

public class UncertaintyExperiment
{
    private readonly PlanckConstants _planck;
    private readonly DerivedConstants _derived;
    private readonly Random _rng;

    // Energie-Skala wird jetzt bewusst injizierbar gehalten
    private Func<double> _energyScale;
    private Func<double> _temporalTick;

    public UncertaintyExperiment(PlanckConstants planck, int? seed = null)
    {
        _planck = planck;
        _derived = new DerivedConstants(planck);
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();

        // Default: vollständig emergente Skala
        // E = ħ_emergent / tP
        _energyScale = () => _derived.ReducedPlanck / _planck.tP;

        // Standard: ursprünglicher Planck-Takt
        _temporalTick = () => _planck.tP;

    }

    public List<UncertaintyResult> Run(List<double> deltaTValues, int samplesPerStep = 5000)
    {
        var results = new List<UncertaintyResult>();

        double eScale = _energyScale();

        foreach (var deltaT in deltaTValues)
        {
            var fluctuations = new List<double>(samplesPerStep);

            for (int i = 0; i < samplesPerStep; i++)
            {
                double xi = NextGaussian();


                double effectiveTick = _temporalTick();
                double deltaTemporal = (effectiveTick / deltaT) * xi;


                fluctuations.Add(deltaTemporal);
            }

            double mean = fluctuations.Average();

            double variance = fluctuations
                .Select(x => Math.Pow(x - mean, 2))
                .Average();

            double std = Math.Sqrt(variance);

            double deltaE = eScale * std;

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
    public List<UncertaintyResult> RunDriven(
        List<double> deltaTValues,
        double driveOmega,
        double driveAmplitude,
        int samplesPerStep = 5000)
    {
        var results = new List<UncertaintyResult>();

        double eScale = _energyScale();
        double effectiveTick = _temporalTick();

        foreach (var deltaT in deltaTValues)
        {
            var fluctuations = new List<double>(samplesPerStep);

            for (int i = 0; i < samplesPerStep; i++)
            {
                double xi = NextGaussian();

                // innere Sample-Zeit
                double tau = i * effectiveTick;

                // JETZT variiert der Drive pro Sample
                double drive = driveAmplitude * Math.Sin(driveOmega * tau);

                double deltaTemporal = (effectiveTick / deltaT) * (xi + drive);

                fluctuations.Add(deltaTemporal);
            }

            double mean = fluctuations.Average();
            double variance = fluctuations
                .Select(x => Math.Pow(x - mean, 2))
                .Average();
            double std = Math.Sqrt(variance);

            double deltaE = eScale * std;

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
        // Box-Muller
        double u1 = 1.0 - _rng.NextDouble();
        double u2 = 1.0 - _rng.NextDouble();

        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }

    /// <summary>
    /// Rein dimensionslose Referenzskala.
    /// Dann ist DeltaE * DeltaT typischerweise ~ tP.
    /// </summary>
    public void UseDimensionlessScale()
    {
        _energyScale = () => 1.0;
    }

    /// <summary>
    /// Voll emergente Planck-Energieskala aus dem aktuell gescannten Planck-Punkt.
    /// KEIN festes SI-c mehr.
    /// E = mP * c_emergent^2
    /// </summary>
    public void UseEmergentPlanckEnergyScale()
    {
        _energyScale = () => _planck.mP * _derived.SpeedOfLight * _derived.SpeedOfLight;
    }

    /// <summary>
    /// Alternative emergente Darstellung:
    /// E = ħ_emergent / tP
    /// Sollte konsistent zur obigen Form sein.
    /// </summary>
    public void UseEmergentReducedPlanckOverTimeScale()
    {
        _energyScale = () => _derived.ReducedPlanck / _planck.tP;
    }

    /// <summary>
    /// Frei injizierbare Skala für spätere Experimente.
    /// </summary>
    public void UseEnergyScale(Func<double> energyScale)
    {
        _energyScale = energyScale ?? throw new ArgumentNullException(nameof(energyScale));
    }
    public void UseTemporalTick(Func<double> temporalTick)
    {
        _temporalTick = temporalTick ?? throw new ArgumentNullException(nameof(temporalTick));
    }

    public double GetCurrentTemporalTick()
    {
        return _temporalTick();
    }
    /// <summary>
    /// Für Debug / Tests sichtbar machen.
    /// </summary>
    public double GetCurrentEnergyScale()
    {
        return _energyScale();
    }
}