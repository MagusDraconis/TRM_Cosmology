using System;
using System.Collections.Generic;
using System.Text;
using TRM.QuantumCore.Planck;
using TRM.Simulations.Experiments;
using Xunit.Abstractions;

namespace TRM.Tests.QuantumTests;


public class UncertaintyTests1
{
    private readonly ITestOutputHelper _output;
    public UncertaintyTests1(ITestOutputHelper output)
    {
        _output = output;
    }


    [Fact]
    public void Run_Uncertainty_With_LogSpace_And_Analyze()
    {
        var planck = PlanckConstants.FromPhysicalConstants();

        var experiment = new UncertaintyExperiment(planck);

        // 🔥 Log sampling (important!)
        var deltaTValues = LogSpace(1e-43, 5e-38, 30);

        var results = experiment.Run(deltaTValues, samplesPerStep: 10000);

        // extract values
        var deltaT = results.Select(r => r.DeltaT).ToList();
        var deltaE = results.Select(r => r.DeltaE).ToList();
        var products = results.Select(r => r.Product).ToList();

        // basic sanity checks
        Assert.NotEmpty(results);
        Assert.All(products, p => Assert.True(p > 0));

        // 🔥 statistics
        double mean = products.Average();
        double std = Math.Sqrt(products.Select(p => Math.Pow(p - mean, 2)).Average());

        // print debug
        _output.WriteLine($"Mean ΔE·Δt: {mean:E6}");
        _output.WriteLine($"StdDev     : {std:E6}");
        _output.WriteLine($"Min        : {products.Min():E6}");
        _output.WriteLine($"Max        : {products.Max():E6}");

        // 🔥 expected: ~ ħ
        double hbar = PhysicalConstantsSI.hbar;

        double relError = Math.Abs(mean - hbar) / hbar;

        _output.WriteLine($"Relative error: {relError:P4}");

        // ✅ tolerance (you can tighten later)
        Assert.InRange(mean, 1.0e-34, 1.1e-34);
        Assert.True(relError < 0.05); // <5%
    }
    [Fact]
    public void Run_Uncertainty_Experiment_And_Export_Csv_LogSpace()
    {
        var planck = PlanckConstants.FromPhysicalConstants();
        var experiment = new UncertaintyExperiment(planck);

        var deltaTValues = LogSpace(1e-43, 5e-38, 25);

        var results = experiment.Run(deltaTValues, samplesPerStep: 10000);

        UncertaintyCsvExporter.Export("uncertainty_results.csv", results);

        Assert.NotEmpty(results);
    }

    private static List<double> LogSpace(double min, double max, int points)
    {
        var result = new List<double>(points);

        double logMin = Math.Log10(min);
        double logMax = Math.Log10(max);

        for (int i = 0; i < points; i++)
        {
            double t = logMin + (logMax - logMin) * i / (points - 1);
            result.Add(Math.Pow(10, t));
        }

        return result;
    }
    [Fact]
    public void Should_Produce_Constant_Without_Scale()
    {
        var planck = PlanckConstants.FromPhysicalConstants();
        var exp = new UncertaintyExperiment(planck);

        exp.UseDimensionlessScale();

        var deltaT = LogSpace(1e-43, 1e-38, 30);

        var results = exp.Run(deltaT, 10000);

        var mean = results.Average(r => r.Product);

        double tP = planck.tP;

        _output.WriteLine($"Mean: {mean:E6}");
        _output.WriteLine($"tP  : {tP:E6}");

        double relError = Math.Abs(mean - tP) / tP;

        _output.WriteLine($"Rel error: {relError:P6}");

        // ✅ now correct
        Assert.True(relError < 0.02); // <2%
    }

    [Fact]
    public void Should_Recover_Hbar_From_Planck_Scale()
    {
        var p = PlanckConstants.FromPhysicalConstants();

        var exp = new UncertaintyExperiment(p, seed: 42);
        exp.UseEmergentPlanckEnergyScale();
        

        var deltaT = LogSpace(1e-43, 1e-38, 30);

        var results = exp.Run(deltaT, 10000);

        var mean = results.Average(r => r.Product);

        double hbar = PhysicalConstantsSI.hbar;

        double rel = Math.Abs(mean - hbar) / hbar;

        _output.WriteLine($"Mean: {mean:E}");
        _output.WriteLine($"Rel error: {rel}");

        Assert.True(rel < 0.05);
    }

    [Fact]
    public void Should_Report_Correlation_Between_Stability_And_Hbar_Error_Without_Modifying_Stability()
    {
        var basePlanck = PlanckConstants.FromPhysicalConstants();
        var scan = new PlanckMultiScan(basePlanck);

        var scanResults = scan.Run(200, 0.001);
        var deltaT = LogSpace(1e-43, 1e-38, 20);

        foreach (var s in scanResults)
        {
            var p = new PlanckConstants(
                basePlanck.lP * (1.0 + s.epsL),
                basePlanck.tP * (1.0 + s.epsT),
                basePlanck.mP * (1.0 + s.epsM)
            );

            var exp = new UncertaintyExperiment(p, seed: 42);
            exp.UseEmergentPlanckEnergyScale();

            var results = exp.Run(deltaT, 3000);

            double mean = results.Average(r => r.Product);

            var derived = new DerivedConstants(p);
            double hbarDerived = derived.ReducedPlanck;

            double relError = Math.Abs(mean - hbarDerived) / hbarDerived;
            s.HbarError = relError;
        }

        var valid = scanResults
            .Where(r => r.Stability > 0 && r.HbarError > 0)
            .ToList();

        Assert.True(valid.Count > 10, "Not enough valid scan points for correlation analysis.");

        // Log-Werte, damit Größenordnungen besser vergleichbar werden
        var x = valid.Select(r => Math.Log10(r.Stability)).ToArray();
        var y = valid.Select(r => Math.Log10(r.HbarError)).ToArray();

        double corr = PearsonCorrelation(x, y);

        var bestStability = valid.OrderBy(r => r.Stability).First();
        var bestHbar = valid.OrderBy(r => r.HbarError).First();

        _output.WriteLine("=== Stability vs HbarError Correlation ===");
        _output.WriteLine($"Pearson(log Stability, log HbarError) = {corr:F9}");
        _output.WriteLine("");

        _output.WriteLine("Best Stability point:");
        _output.WriteLine($"  epsL      = {bestStability.epsL:E16}");
        _output.WriteLine($"  epsT      = {bestStability.epsT:E16}");
        _output.WriteLine($"  epsM      = {bestStability.epsM:E16}");
        _output.WriteLine($"  Stability = {bestStability.Stability:E16}");
        _output.WriteLine($"  HbarError = {bestStability.HbarError:E16}");

        _output.WriteLine("Best HbarError point:");
        _output.WriteLine($"  epsL      = {bestHbar.epsL:E16}");
        _output.WriteLine($"  epsT      = {bestHbar.epsT:E16}");
        _output.WriteLine($"  epsM      = {bestHbar.epsM:E16}");
        _output.WriteLine($"  Stability = {bestHbar.Stability:E16}");
        _output.WriteLine($"  HbarError = {bestHbar.HbarError:E16}");

        // Nur ein weicher Check:
        // Der Test soll erstmal diagnostisch sein, nicht künstlich hart.
        Assert.True(!double.IsNaN(corr) && !double.IsInfinity(corr),
            "Correlation could not be computed.");
    }





    [Fact]
    public void Should_Report_Dimensionless_Product_Scale_At_Stability_And_Product_Minima()
    {
        var basePlanck = PlanckConstants.FromPhysicalConstants();
        var scan = new PlanckMultiScan(basePlanck);

        var scanResults = scan.Run(200, 0.001);
        var deltaT = LogSpace(1e-43, 1e-38, 20);

        foreach (var s in scanResults)
        {
            var p = new PlanckConstants(
                basePlanck.lP * (1.0 + s.epsL),
                basePlanck.tP * (1.0 + s.epsT),
                basePlanck.mP * (1.0 + s.epsM)
            );

            var exp = new UncertaintyExperiment(p, seed: 42);
            exp.UseDimensionlessScale();

            var results = exp.Run(deltaT, 3000);

            double mean = results.Average(r => r.Product);

            // Jetzt NICHT gegen hbar prüfen,
            // sondern gegen die natürliche dimensionslose Erwartung ~ tP
            double relToTP = Math.Abs(mean - p.tP) / p.tP;

            // Wir speichern das bewusst in HbarError um die bestehende Struktur weiter zu verwenden,
            // semantisch ist es jetzt aber ein "dimensionless product error".
            s.HbarError = relToTP;
        }

        var stabilityMin = scanResults.OrderBy(r => r.Stability).First();
        var bestProductScale = scanResults.OrderBy(r => r.HbarError).First();

        _output.WriteLine("=== Dimensionless Product Scale Test ===");
        _output.WriteLine("Stability minimum:");
        _output.WriteLine($"  epsL      = {stabilityMin.epsL:E16}");
        _output.WriteLine($"  epsT      = {stabilityMin.epsT:E16}");
        _output.WriteLine($"  epsM      = {stabilityMin.epsM:E16}");
        _output.WriteLine($"  Stability = {stabilityMin.Stability:E16}");
        _output.WriteLine($"  ProductErrorToTP = {stabilityMin.HbarError:E16}");

        _output.WriteLine("Best dimensionless-product point:");
        _output.WriteLine($"  epsL      = {bestProductScale.epsL:E16}");
        _output.WriteLine($"  epsT      = {bestProductScale.epsT:E16}");
        _output.WriteLine($"  epsM      = {bestProductScale.epsM:E16}");
        _output.WriteLine($"  Stability = {bestProductScale.Stability:E16}");
        _output.WriteLine($"  ProductErrorToTP = {bestProductScale.HbarError:E16}");

        double distance = Math.Sqrt(
            Math.Pow(stabilityMin.epsL - bestProductScale.epsL, 2) +
            Math.Pow(stabilityMin.epsT - bestProductScale.epsT, 2) +
            Math.Pow(stabilityMin.epsM - bestProductScale.epsM, 2));

        _output.WriteLine($"Parameter distance between minima = {distance:E16}");

        // Wieder bewusst weich:
        // Erstmal diagnostisch, ohne harte physikalische Behauptung.
        Assert.True(stabilityMin.HbarError < 1e-2,
            "Dimensionless product at the stability minimum is unexpectedly far from tP.");
    }







    private static double PearsonCorrelation(double[] x, double[] y)
    {
        if (x.Length != y.Length || x.Length == 0)
            throw new ArgumentException("Arrays must have same non-zero length.");

        double meanX = x.Average();
        double meanY = y.Average();

        double num = 0.0;
        double denX = 0.0;
        double denY = 0.0;

        for (int i = 0; i < x.Length; i++)
        {
            double dx = x[i] - meanX;
            double dy = y[i] - meanY;

            num += dx * dy;
            denX += dx * dx;
            denY += dy * dy;
        }

        double den = Math.Sqrt(denX * denY);
        if (den == 0.0)
            return double.NaN;

        return num / den;
    }


    //[Fact]
    //public void Should_Stability_Minimum_Select_Physical_Scale()
    //{
    //    var basePlanck = PlanckConstants.FromPhysicalConstants();
    //    var scan = new PlanckMultiScan(basePlanck);

    //    var scanResults = scan.Run(200, 0.001);

    //    var deltaT = LogSpace(1e-43, 1e-38, 20);

    //    foreach (var s in scanResults)
    //    {
    //        var p = new PlanckConstants(
    //            basePlanck.lP * (1.0 + s.epsL),
    //            basePlanck.tP * (1.0 + s.epsT),
    //            basePlanck.mP * (1.0 + s.epsM)
    //        );

    //        var exp = new UncertaintyExperiment(p, seed: 42);
    //        exp.UseEmergentPlanckEnergyScale();


    //        var results = exp.Run(deltaT, 3000);

    //        double mean = results.Average(r => r.Product);

    //        var derived = new DerivedConstants(p);
    //        double hbarDerived = derived.ReducedPlanck;
    //        double relError = Math.Abs(mean - hbarDerived) / hbarDerived;
    //        s.HbarError = relError;
    //    }

    //    var stabilityMin = scanResults.OrderBy(r => r.Stability).First();
    //    var hbarMin = scanResults.OrderBy(r => r.HbarError).First();

    //    _output.WriteLine("=== Stability vs hbar-scale selection ===");
    //    _output.WriteLine($"Stability minimum:");
    //    _output.WriteLine($"  epsL      = {stabilityMin.epsL:E}");
    //    _output.WriteLine($"  epsT      = {stabilityMin.epsT:E}");
    //    _output.WriteLine($"  epsM      = {stabilityMin.epsM:E}");
    //    _output.WriteLine($"  Stability = {stabilityMin.Stability:E}");
    //    _output.WriteLine($"  HbarError = {stabilityMin.HbarError:E}");

    //    _output.WriteLine($"hbar-error minimum:");
    //    _output.WriteLine($"  epsL      = {hbarMin.epsL:E}");
    //    _output.WriteLine($"  epsT      = {hbarMin.epsT:E}");
    //    _output.WriteLine($"  epsM      = {hbarMin.epsM:E}");
    //    _output.WriteLine($"  Stability = {hbarMin.Stability:E}");
    //    _output.WriteLine($"  HbarError = {hbarMin.HbarError:E}");

    //    // Abstand der Punkte im Parameterraum
    //    double distance = Math.Sqrt(
    //        Math.Pow(stabilityMin.epsL - hbarMin.epsL, 2) +
    //        Math.Pow(stabilityMin.epsT - hbarMin.epsT, 2) +
    //        Math.Pow(stabilityMin.epsM - hbarMin.epsM, 2));

    //    _output.WriteLine($"Parameter distance between minima = {distance:E}");

    //    // Erstmal keine harte Identität fordern, sondern Nähe
    //    Assert.True(distance < 5e-4,
    //        "Stability minimum does not lie near the hbar-error minimum.");
    //}

    //[Fact]
    //public void Should_Stability_Metric_Correlate_With_Hbar_Error()
    //{
    //    var basePlanck = PlanckConstants.FromPhysicalConstants();

    //    var scan = new PlanckMultiScan(basePlanck);

    //    var scanResults = scan.Run(200, 0.001); // fein genug

    //    var deltaT = LogSpace(1e-43, 1e-38, 20);
    //    double alpha = 1.0;

    //    foreach (var s in scanResults)
    //    {
    //        var p = new PlanckConstants(
    //            basePlanck.lP * (1 + s.epsL),
    //            basePlanck.tP * (1 + s.epsT),
    //            basePlanck.mP * (1 + s.epsM)
    //        );


    //        var exp = new UncertaintyExperiment(p, seed: 42);
    //        exp.UseEmergentPlanckEnergyScale();


    //        var results = exp.Run(deltaT, 3000);

    //        double mean = results.Average(r => r.Product);

    //        var derived = new DerivedConstants(p);
    //        double hbarDerived = derived.ReducedPlanck;
    //        double relError = Math.Abs(mean - hbarDerived) / hbarDerived;

    //        s.HbarError = relError;

    //        // 🔥 neue gekoppelte Stability
    //        s.Stability = s.Stability + alpha * relError * relError;

    //    }

    //    // 👉 Minimum bestimmen
    //    var bestStability = scanResults
    //        .OrderBy(r => r.Stability)
    //        .First();

    //    var bestHbar = scanResults
    //        .OrderBy(r => r.HbarError)
    //        .First();

    //    _output.WriteLine("Best Stability:");
    //    _output.WriteLine($"epsL={bestStability.epsL}, epsT={bestStability.epsT}, epsM={bestStability.epsM}");

    //    _output.WriteLine("Best Hbar match:");
    //    _output.WriteLine($"epsL={bestHbar.epsL}, epsT={bestHbar.epsT}, epsM={bestHbar.epsM}");

    //    // 👉 Vergleich (grob, reicht erstmal)
    //    double dist =
    //        Math.Abs(bestStability.epsL - bestHbar.epsL) +
    //        Math.Abs(bestStability.epsT - bestHbar.epsT) +
    //        Math.Abs(bestStability.epsM - bestHbar.epsM);

    //    _output.WriteLine($"Distance: {dist}");

    //    Assert.True(dist < 2e-3);
    //}
    //[Fact]
    //public void Run_Uncertainty_Experiment_And_Export_Csv()
    //{
    //    var planck = PlanckConstants.FromPhysicalConstants();

    //    var experiment = new UncertaintyExperiment(planck);

    //    var deltaTValues = new List<double>
    //        {
    //            1e-43, 2e-43, 5e-43,
    //            1e-42, 2e-42, 5e-42,
    //            1e-41, 2e-41, 5e-41,
    //            1e-40, 2e-40, 5e-40,
    //            1e-39, 2e-39, 5e-39,
    //            1e-38, 2e-38, 5e-38
    //        };

    //    var results = experiment.Run(deltaTValues, samplesPerStep: 10000);

    //    UncertaintyCsvExporter.Export("uncertainty_results.csv", results);

    //    Assert.NotEmpty(results);
    //}

}
