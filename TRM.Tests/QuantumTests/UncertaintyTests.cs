using System;
using System.Collections.Generic;
using System.Text;
using TRM.QuantumCore.Planck;
using TRM.Simulations.Experiments;

namespace TRM.Tests.QuantumTests;


public class UncertaintyTests
{
    [Fact]
    public void Run_Uncertainty_Experiment_And_Export_Csv()
    {
        var planck = PlanckConstants.FromPhysicalConstants();

        var experiment = new UncertaintyExperiment(planck);

        var deltaTValues = new List<double>
            {
                1e-43, 2e-43, 5e-43,
                1e-42, 2e-42, 5e-42,
                1e-41, 2e-41, 5e-41,
                1e-40, 2e-40, 5e-40,
                1e-39, 2e-39, 5e-39,
                1e-38, 2e-38, 5e-38
            };

        var results = experiment.Run(deltaTValues, samplesPerStep: 10000);

        UncertaintyCsvExporter.Export("uncertainty_results.csv", results);

        Assert.NotEmpty(results);
    }
}

