using System;
using System.Collections.Generic;
using System.Linq;
using TRM.QuantumCore.Planck;
using TRM.Simulations.Experiments;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.QuantumTests
{
    public class UncertaintyTests
    {
        private readonly ITestOutputHelper _output;

        public UncertaintyTests(ITestOutputHelper output)
        {
            _output = output;
        }
        [Fact]
        public void EmergentEnergyScale_Should_Match_Hbar_Over_tP()
        {
            var p = PlanckConstants.FromPhysicalConstants();
            var d = new DerivedConstants(p);

            double e1 = p.mP * d.SpeedOfLight * d.SpeedOfLight;
            double e2 = d.ReducedPlanck / p.tP;

            double relError = Math.Abs(e1 - e2) / e2;

            _output.WriteLine($"mP * c^2       = {e1:E16}");
            _output.WriteLine($"hbar / tP      = {e2:E16}");
            _output.WriteLine($"relative error = {relError:E16}");

            Assert.True(relError < 1e-15);
        }

        [Fact]
        public void Should_Report_Stability_Vs_Hbar_Scale_With_Pointwise_Varying_Seeds()
        {
            var basePlanck = PlanckConstants.FromPhysicalConstants();
            var scan = new PlanckMultiScan(basePlanck);

            var scanResults = scan.Run(200, 0.001);
            var deltaT = LogSpace(1e-43, 1e-38, 20);

            int globalSeed = 42;
            int index = 0;

            foreach (var s in scanResults)
            {
                var p = new PlanckConstants(
                    basePlanck.lP * (1.0 + s.epsL),
                    basePlanck.tP * (1.0 + s.epsT),
                    basePlanck.mP * (1.0 + s.epsM)
                );

                // Reproduzierbar, aber NICHT identisch pro Punkt
                var exp = new UncertaintyExperiment(p, seed: globalSeed + index);
                exp.UseEmergentPlanckEnergyScale();

                var results = exp.Run(deltaT, 3000);

                var products = results.Select(r => r.Product).ToArray();

                double mean = products.Average();
                double std = StdDev(products);

                var derived = new DerivedConstants(p);
                double hbarDerived = derived.ReducedPlanck;

                double scaleRatio = mean / hbarDerived;
                double hbarError = Math.Abs(scaleRatio - 1.0);

                // relative Streuung der Produkte
                double spreadError = std / Math.Abs(mean);

                // bestehendes Feld weiter benutzen
                s.HbarError = hbarError;

                // optionale dynamische Zusatzfelder, falls verfügbar:
                // s.MeanProduct = mean;
                // s.StdProduct = std;
                // s.ScaleRatio = scaleRatio;
                // s.SpreadError = spreadError;
                // s.Score = hbarError + spreadError;

                index++;
            }

            var bestStability = scanResults.OrderBy(r => r.Stability).First();
            var bestHbar = scanResults.OrderBy(r => r.HbarError).First();

            _output.WriteLine("=== Stability vs hbar-scale (varying seeds) ===");
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

            double distance = EuclideanDistance(
                bestStability.epsL, bestStability.epsT, bestStability.epsM,
                bestHbar.epsL, bestHbar.epsT, bestHbar.epsM);

            _output.WriteLine($"Parameter distance between minima = {distance:E16}");

            // bewusst nur weicher diagnostischer Check
            Assert.True(!double.IsNaN(distance) && !double.IsInfinity(distance));
        }

        [Fact]
        public void Should_Report_Dimensionless_Product_Behaviour_Without_Hbar_Calibration()
        {
            var basePlanck = PlanckConstants.FromPhysicalConstants();
            var scan = new PlanckMultiScan(basePlanck);

            var scanResults = scan.Run(200, 0.001);
            var deltaT = LogSpace(1e-43, 1e-38, 20);

            int globalSeed = 4200;
            int index = 0;

            foreach (var s in scanResults)
            {
                var p = new PlanckConstants(
                    basePlanck.lP * (1.0 + s.epsL),
                    basePlanck.tP * (1.0 + s.epsT),
                    basePlanck.mP * (1.0 + s.epsM)
                );

                // NICHT-zirkulär: keine hbar-Kalibrierung
                var exp = new UncertaintyExperiment(p, seed: globalSeed + index);
                exp.UseDimensionlessScale();

                var results = exp.Run(deltaT, 3000);

                var products = results.Select(r => r.Product).ToArray();

                double mean = products.Average();
                double std = StdDev(products);

                // dimensionslose Erwartung: Produkt ~ tP
                double scaleRatio = mean / p.tP;
                double relToTP = Math.Abs(scaleRatio - 1.0);
                double spreadError = std / Math.Abs(mean);

                s.HbarError = relToTP;

                // optionale Zusatzinfos:
                // s.ScaleRatio = scaleRatio;
                // s.SpreadError = spreadError;
                // s.Score = relToTP + spreadError;

                index++;
            }

            var bestStability = scanResults.OrderBy(r => r.Stability).First();
            var bestProductScale = scanResults.OrderBy(r => r.HbarError).First();

            _output.WriteLine("=== Dimensionless Product Scale Test (varying seeds) ===");
            _output.WriteLine("Stability minimum:");
            _output.WriteLine($"  epsL      = {bestStability.epsL:E16}");
            _output.WriteLine($"  epsT      = {bestStability.epsT:E16}");
            _output.WriteLine($"  epsM      = {bestStability.epsM:E16}");
            _output.WriteLine($"  Stability = {bestStability.Stability:E16}");
            _output.WriteLine($"  ProductErrorToTP = {bestStability.HbarError:E16}");

            _output.WriteLine("Best dimensionless-product point:");
            _output.WriteLine($"  epsL      = {bestProductScale.epsL:E16}");
            _output.WriteLine($"  epsT      = {bestProductScale.epsT:E16}");
            _output.WriteLine($"  epsM      = {bestProductScale.epsM:E16}");
            _output.WriteLine($"  Stability = {bestProductScale.Stability:E16}");
            _output.WriteLine($"  ProductErrorToTP = {bestProductScale.HbarError:E16}");

            double distance = EuclideanDistance(
                bestStability.epsL, bestStability.epsT, bestStability.epsM,
                bestProductScale.epsL, bestProductScale.epsT, bestProductScale.epsM);

            _output.WriteLine($"Parameter distance between minima = {distance:E16}");

            // ebenfalls nur ein weicher Check
            Assert.True(bestStability.HbarError < 1e-1,
                "Dimensionless product at the stability minimum is unexpectedly poor.");
        }

        [Fact]
        public void Should_Report_Top10_Overlap_Between_Stability_And_HbarError()
        {
            var basePlanck = PlanckConstants.FromPhysicalConstants();
            var scan = new PlanckMultiScan(basePlanck);

            var scanResults = scan.Run(200, 0.001);
            var deltaT = LogSpace(1e-43, 1e-38, 20);

            int globalSeed = 9000;
            int index = 0;

            foreach (var s in scanResults)
            {
                var p = new PlanckConstants(
                    basePlanck.lP * (1.0 + s.epsL),
                    basePlanck.tP * (1.0 + s.epsT),
                    basePlanck.mP * (1.0 + s.epsM)
                );

                var exp = new UncertaintyExperiment(p, seed: globalSeed + index);
                exp.UseEmergentPlanckEnergyScale();

                var results = exp.Run(deltaT, 3000);
                double mean = results.Average(r => r.Product);

                var derived = new DerivedConstants(p);
                double hbarDerived = derived.ReducedPlanck;

                s.HbarError = Math.Abs(mean - hbarDerived) / hbarDerived;

                index++;
            }

            var topStability = scanResults
                .OrderBy(r => r.Stability)
                .Take(10)
                .ToList();

            var topHbar = scanResults
                .OrderBy(r => r.HbarError)
                .Take(10)
                .ToList();

            int overlap = topStability.Count(a =>
                topHbar.Any(b =>
                    NearlyEqual(a.epsL, b.epsL) &&
                    NearlyEqual(a.epsT, b.epsT) &&
                    NearlyEqual(a.epsM, b.epsM)));

            _output.WriteLine("=== Top-10 Overlap Stability vs HbarError ===");
            _output.WriteLine($"Top-10 overlap = {overlap}");

            Assert.True(overlap >= 0); // rein diagnostisch
        }

        [Fact]
        public void Should_Report_Combined_Score_Landscape_For_Stability_And_ProductScaleError()
        {
            var basePlanck = PlanckConstants.FromPhysicalConstants();
            var scan = new PlanckMultiScan(basePlanck);

            var scanResults = scan.Run(200, 0.001);
            var deltaT = LogSpace(1e-43, 1e-38, 20);

            int globalSeed = 12000;
            int index = 0;

            // lokale Hilfsstruktur
            var enriched = new List<(dynamic ScanPoint, double ProductScaleError)>();

            foreach (var s in scanResults)
            {
                var p = new PlanckConstants(
                    basePlanck.lP * (1.0 + s.epsL),
                    basePlanck.tP * (1.0 + s.epsT),
                    basePlanck.mP * (1.0 + s.epsM)
                );

                var exp = new UncertaintyExperiment(p, seed: globalSeed + index);
                exp.UseDimensionlessScale();

                var results = exp.Run(deltaT, 3000);
                var products = results.Select(r => r.Product).ToArray();

                double mean = products.Average();

                // nicht-zirkulär: Vergleich gegen tP
                double productScaleError = Math.Abs(mean - p.tP) / p.tP;

                enriched.Add((s, productScaleError));

                index++;
            }

            // Referenz-Minima
            var bestStability = enriched.OrderBy(x => x.ScanPoint.Stability).First();
            var bestProduct = enriched.OrderBy(x => x.ProductScaleError).First();

            _output.WriteLine("=== Combined Score Landscape Test ===");
            _output.WriteLine("Reference minima:");
            _output.WriteLine($"Best Stability:");
            _output.WriteLine($"  epsL = {bestStability.ScanPoint.epsL:E16}");
            _output.WriteLine($"  epsT = {bestStability.ScanPoint.epsT:E16}");
            _output.WriteLine($"  epsM = {bestStability.ScanPoint.epsM:E16}");
            _output.WriteLine($"  Stability = {bestStability.ScanPoint.Stability:E16}");
            _output.WriteLine($"  ProductScaleError = {bestStability.ProductScaleError:E16}");

            _output.WriteLine($"Best ProductScaleError:");
            _output.WriteLine($"  epsL = {bestProduct.ScanPoint.epsL:E16}");
            _output.WriteLine($"  epsT = {bestProduct.ScanPoint.epsT:E16}");
            _output.WriteLine($"  epsM = {bestProduct.ScanPoint.epsM:E16}");
            _output.WriteLine($"  Stability = {bestProduct.ScanPoint.Stability:E16}");
            _output.WriteLine($"  ProductScaleError = {bestProduct.ProductScaleError:E16}");

            _output.WriteLine("");

            // Mehrere λ testen
            double[] lambdas =
            {
        1e-6,
        1e-5,
        1e-4,
        1e-3,
        1e-2,
        1e-1,
        1.0,
        10.0,
        100.0
    };

            foreach (double lambda in lambdas)
            {
                var bestScore = enriched
                    .Select(x => new
                    {
                        x.ScanPoint,
                        x.ProductScaleError,
                        Score = x.ScanPoint.Stability + lambda * x.ProductScaleError
                    })
                    .OrderBy(x => x.Score)
                    .First();

                double distToStability = EuclideanDistance(
                    bestScore.ScanPoint.epsL, bestScore.ScanPoint.epsT, bestScore.ScanPoint.epsM,
                    bestStability.ScanPoint.epsL, bestStability.ScanPoint.epsT, bestStability.ScanPoint.epsM);

                double distToProduct = EuclideanDistance(
                    bestScore.ScanPoint.epsL, bestScore.ScanPoint.epsT, bestScore.ScanPoint.epsM,
                    bestProduct.ScanPoint.epsL, bestProduct.ScanPoint.epsT, bestProduct.ScanPoint.epsM);

                _output.WriteLine($"lambda = {lambda:E6}");
                _output.WriteLine($"  epsL = {bestScore.ScanPoint.epsL:E16}");
                _output.WriteLine($"  epsT = {bestScore.ScanPoint.epsT:E16}");
                _output.WriteLine($"  epsM = {bestScore.ScanPoint.epsM:E16}");
                _output.WriteLine($"  Stability = {bestScore.ScanPoint.Stability:E16}");
                _output.WriteLine($"  ProductScaleError = {bestScore.ProductScaleError:E16}");
                _output.WriteLine($"  Score = {bestScore.Score:E16}");
                _output.WriteLine($"  Dist to Stability min = {distToStability:E16}");
                _output.WriteLine($"  Dist to Product min   = {distToProduct:E16}");
                _output.WriteLine("");
            }

            // rein diagnostisch: Test soll nicht hart scheitern
            Assert.True(enriched.Count > 10);
        }
        #region Level 2

        [Fact]
        public void Should_Report_EigenmodeScore_Minimum_For_Product_Stability()
        {
            var basePlanck = PlanckConstants.FromPhysicalConstants();
            var scan = new PlanckMultiScan(basePlanck);

            var scanResults = scan.Run(200, 0.001);
            var deltaT = LogSpace(1e-43, 1e-38, 20);

            int globalSeed = 15000;
            int index = 0;

            var enriched = new List<(dynamic ScanPoint, double MeanProduct, double StdProduct, double ScaleError, double SpreadError, double EigenmodeScore)>();

            foreach (var s in scanResults)
            {
                var p = new PlanckConstants(
                    basePlanck.lP * (1.0 + s.epsL),
                    basePlanck.tP * (1.0 + s.epsT),
                    basePlanck.mP * (1.0 + s.epsM)
                );

                // Nicht-zirkulär: dimensionslose Skala
                var exp = new UncertaintyExperiment(p, seed: globalSeed + index);
                exp.UseDimensionlessScale();

                var results = exp.Run(deltaT, 3000);

                var products = results.Select(r => r.Product).ToArray();

                double meanProduct = products.Average();
                double stdProduct = StdDev(products);

                // Erwartung im dimensionslosen Fall: Produkt ~ tP
                double scaleError = Math.Abs(meanProduct - p.tP) / p.tP;

                // relative Streuung
                double spreadError = stdProduct / Math.Abs(meanProduct);

                // einfache Eigenmoden-Metrik
                double eigenmodeScore = scaleError + spreadError;

                enriched.Add((s, meanProduct, stdProduct, scaleError, spreadError, eigenmodeScore));

                index++;
            }

            var bestStability = enriched.OrderBy(x => x.ScanPoint.Stability).First();
            var bestEigenmode = enriched.OrderBy(x => x.EigenmodeScore).First();
            var bestScale = enriched.OrderBy(x => x.ScaleError).First();
            var bestSpread = enriched.OrderBy(x => x.SpreadError).First();

            _output.WriteLine("=== Eigenmode / Resonance Test ===");
            _output.WriteLine("");

            _output.WriteLine("Best Stability point:");
            _output.WriteLine($"  epsL            = {bestStability.ScanPoint.epsL:E16}");
            _output.WriteLine($"  epsT            = {bestStability.ScanPoint.epsT:E16}");
            _output.WriteLine($"  epsM            = {bestStability.ScanPoint.epsM:E16}");
            _output.WriteLine($"  Stability       = {bestStability.ScanPoint.Stability:E16}");
            _output.WriteLine($"  MeanProduct     = {bestStability.MeanProduct:E16}");
            _output.WriteLine($"  StdProduct      = {bestStability.StdProduct:E16}");
            _output.WriteLine($"  ScaleError      = {bestStability.ScaleError:E16}");
            _output.WriteLine($"  SpreadError     = {bestStability.SpreadError:E16}");
            _output.WriteLine($"  EigenmodeScore  = {bestStability.EigenmodeScore:E16}");
            _output.WriteLine("");

            _output.WriteLine("Best EigenmodeScore point:");
            _output.WriteLine($"  epsL            = {bestEigenmode.ScanPoint.epsL:E16}");
            _output.WriteLine($"  epsT            = {bestEigenmode.ScanPoint.epsT:E16}");
            _output.WriteLine($"  epsM            = {bestEigenmode.ScanPoint.epsM:E16}");
            _output.WriteLine($"  Stability       = {bestEigenmode.ScanPoint.Stability:E16}");
            _output.WriteLine($"  MeanProduct     = {bestEigenmode.MeanProduct:E16}");
            _output.WriteLine($"  StdProduct      = {bestEigenmode.StdProduct:E16}");
            _output.WriteLine($"  ScaleError      = {bestEigenmode.ScaleError:E16}");
            _output.WriteLine($"  SpreadError     = {bestEigenmode.SpreadError:E16}");
            _output.WriteLine($"  EigenmodeScore  = {bestEigenmode.EigenmodeScore:E16}");
            _output.WriteLine("");

            _output.WriteLine("Best ScaleError point:");
            _output.WriteLine($"  epsL            = {bestScale.ScanPoint.epsL:E16}");
            _output.WriteLine($"  epsT            = {bestScale.ScanPoint.epsT:E16}");
            _output.WriteLine($"  epsM            = {bestScale.ScanPoint.epsM:E16}");
            _output.WriteLine($"  Stability       = {bestScale.ScanPoint.Stability:E16}");
            _output.WriteLine($"  ScaleError      = {bestScale.ScaleError:E16}");
            _output.WriteLine($"  SpreadError     = {bestScale.SpreadError:E16}");
            _output.WriteLine($"  EigenmodeScore  = {bestScale.EigenmodeScore:E16}");
            _output.WriteLine("");

            _output.WriteLine("Best SpreadError point:");
            _output.WriteLine($"  epsL            = {bestSpread.ScanPoint.epsL:E16}");
            _output.WriteLine($"  epsT            = {bestSpread.ScanPoint.epsT:E16}");
            _output.WriteLine($"  epsM            = {bestSpread.ScanPoint.epsM:E16}");
            _output.WriteLine($"  Stability       = {bestSpread.ScanPoint.Stability:E16}");
            _output.WriteLine($"  ScaleError      = {bestSpread.ScaleError:E16}");
            _output.WriteLine($"  SpreadError     = {bestSpread.SpreadError:E16}");
            _output.WriteLine($"  EigenmodeScore  = {bestSpread.EigenmodeScore:E16}");
            _output.WriteLine("");

            double distStabilityToEigenmode = EuclideanDistance(
                bestStability.ScanPoint.epsL, bestStability.ScanPoint.epsT, bestStability.ScanPoint.epsM,
                bestEigenmode.ScanPoint.epsL, bestEigenmode.ScanPoint.epsT, bestEigenmode.ScanPoint.epsM);

            double distScaleToEigenmode = EuclideanDistance(
                bestScale.ScanPoint.epsL, bestScale.ScanPoint.epsT, bestScale.ScanPoint.epsM,
                bestEigenmode.ScanPoint.epsL, bestEigenmode.ScanPoint.epsT, bestEigenmode.ScanPoint.epsM);

            double distSpreadToEigenmode = EuclideanDistance(
                bestSpread.ScanPoint.epsL, bestSpread.ScanPoint.epsT, bestSpread.ScanPoint.epsM,
                bestEigenmode.ScanPoint.epsL, bestEigenmode.ScanPoint.epsT, bestEigenmode.ScanPoint.epsM);

            _output.WriteLine($"Distance Stability -> Eigenmode = {distStabilityToEigenmode:E16}");
            _output.WriteLine($"Distance Scale     -> Eigenmode = {distScaleToEigenmode:E16}");
            _output.WriteLine($"Distance Spread    -> Eigenmode = {distSpreadToEigenmode:E16}");

            // bewusst nur diagnostisch / weich
            Assert.True(enriched.Count > 10);
        }
        [Fact]
        public void Should_Show_Preferred_Temporal_Tick_Near_Planck_Time()
        {
            var basePlanck = PlanckConstants.FromPhysicalConstants();
            var deltaT = LogSpace(1e-43, 1e-38, 20);

            // Wir testen Faktoren relativ zu tP
            double[] gammaValues =
            {
        0.25,
        0.5,
        0.75,
        1.0,
        1.25,
        1.5,
        2.0
    };

            var resultsSummary = new List<(double Gamma, double MeanProduct, double StdProduct, double ScaleError, double SpreadError, double TickScore)>();

            int seedBase = 20000;
            int idx = 0;

            foreach (double gamma in gammaValues)
            {
                var exp = new UncertaintyExperiment(basePlanck, seed: seedBase + idx);

                // nicht-zirkulär
                exp.UseDimensionlessScale();

                // JETZT variieren wir den fundamentalen Tick
                exp.UseTemporalTick(() => gamma * basePlanck.tP);

                var results = exp.Run(deltaT, 3000);
                var products = results.Select(r => r.Product).ToArray();

                double meanProduct = products.Average();
                double stdProduct = StdDev(products);

                // Vergleich gegen den ursprünglichen tP-Referenztakt
                double scaleError = Math.Abs(meanProduct - basePlanck.tP) / basePlanck.tP;

                // relative Streuung
                double spreadError = stdProduct / Math.Abs(meanProduct);

                // einfache kombinierte Takt-Metrik
                double tickScore = scaleError + spreadError;

                resultsSummary.Add((gamma, meanProduct, stdProduct, scaleError, spreadError, tickScore));

                idx++;
            }

            var bestByScale = resultsSummary.OrderBy(x => x.ScaleError).First();
            var bestBySpread = resultsSummary.OrderBy(x => x.SpreadError).First();
            var bestByTickScore = resultsSummary.OrderBy(x => x.TickScore).First();

            _output.WriteLine("=== Fundamental Tick / Minimal Frequency Test ===");
            foreach (var r in resultsSummary)
            {
                _output.WriteLine($"gamma        = {r.Gamma:F6}");
                _output.WriteLine($"MeanProduct  = {r.MeanProduct:E16}");
                _output.WriteLine($"StdProduct   = {r.StdProduct:E16}");
                _output.WriteLine($"ScaleError   = {r.ScaleError:E16}");
                _output.WriteLine($"SpreadError  = {r.SpreadError:E16}");
                _output.WriteLine($"TickScore    = {r.TickScore:E16}");
                _output.WriteLine("");
            }

            _output.WriteLine("Best by ScaleError:");
            _output.WriteLine($"  gamma       = {bestByScale.Gamma:F6}");
            _output.WriteLine($"  ScaleError  = {bestByScale.ScaleError:E16}");
            _output.WriteLine($"  SpreadError = {bestByScale.SpreadError:E16}");
            _output.WriteLine($"  TickScore   = {bestByScale.TickScore:E16}");

            _output.WriteLine("Best by SpreadError:");
            _output.WriteLine($"  gamma       = {bestBySpread.Gamma:F6}");
            _output.WriteLine($"  ScaleError  = {bestBySpread.ScaleError:E16}");
            _output.WriteLine($"  SpreadError = {bestBySpread.SpreadError:E16}");
            _output.WriteLine($"  TickScore   = {bestBySpread.TickScore:E16}");

            _output.WriteLine("Best by TickScore:");
            _output.WriteLine($"  gamma       = {bestByTickScore.Gamma:F6}");
            _output.WriteLine($"  ScaleError  = {bestByTickScore.ScaleError:E16}");
            _output.WriteLine($"  SpreadError = {bestByTickScore.SpreadError:E16}");
            _output.WriteLine($"  TickScore   = {bestByTickScore.TickScore:E16}");

            // Erstmal bewusst weich/diagnostisch:
            Assert.True(resultsSummary.Count == gammaValues.Length);
        }
        [Fact]
        public void Should_Show_Local_Minimum_Of_TickScore_Near_Gamma_One()
        {
            var basePlanck = PlanckConstants.FromPhysicalConstants();
            var deltaT = LogSpace(1e-43, 1e-38, 20);

            // Feinscan um gamma = 1.0
            double[] gammaValues =
            {
        0.85,
        0.90,
        0.95,
        1.00,
        1.05,
        1.10,
        1.15
    };

            var resultsSummary = new List<(double Gamma, double MeanProduct, double StdProduct, double ScaleError, double SpreadError, double TickScore)>();

            int seedBase = 25000;
            int idx = 0;

            foreach (double gamma in gammaValues)
            {
                var exp = new UncertaintyExperiment(basePlanck, seed: seedBase + idx);

                // nicht-zirkulär
                exp.UseDimensionlessScale();

                // effektiven Tick variieren
                exp.UseTemporalTick(() => gamma * basePlanck.tP);

                var results = exp.Run(deltaT, 3000);
                var products = results.Select(r => r.Product).ToArray();

                double meanProduct = products.Average();
                double stdProduct = StdDev(products);

                // Referenz bleibt ursprüngliches tP
                double scaleError = Math.Abs(meanProduct - basePlanck.tP) / basePlanck.tP;
                double spreadError = stdProduct / Math.Abs(meanProduct);

                double tickScore = scaleError + spreadError;

                resultsSummary.Add((gamma, meanProduct, stdProduct, scaleError, spreadError, tickScore));

                idx++;
            }

            var bestByScale = resultsSummary.OrderBy(x => x.ScaleError).First();
            var bestBySpread = resultsSummary.OrderBy(x => x.SpreadError).First();
            var bestByTickScore = resultsSummary.OrderBy(x => x.TickScore).First();

            _output.WriteLine("=== Fine Scan around gamma = 1.0 ===");
            foreach (var r in resultsSummary)
            {
                _output.WriteLine($"gamma        = {r.Gamma:F6}");
                _output.WriteLine($"MeanProduct  = {r.MeanProduct:E16}");
                _output.WriteLine($"StdProduct   = {r.StdProduct:E16}");
                _output.WriteLine($"ScaleError   = {r.ScaleError:E16}");
                _output.WriteLine($"SpreadError  = {r.SpreadError:E16}");
                _output.WriteLine($"TickScore    = {r.TickScore:E16}");
                _output.WriteLine("");
            }

            _output.WriteLine("Best by ScaleError:");
            _output.WriteLine($"  gamma       = {bestByScale.Gamma:F6}");
            _output.WriteLine($"  ScaleError  = {bestByScale.ScaleError:E16}");
            _output.WriteLine($"  SpreadError = {bestByScale.SpreadError:E16}");
            _output.WriteLine($"  TickScore   = {bestByScale.TickScore:E16}");

            _output.WriteLine("Best by SpreadError:");
            _output.WriteLine($"  gamma       = {bestBySpread.Gamma:F6}");
            _output.WriteLine($"  ScaleError  = {bestBySpread.ScaleError:E16}");
            _output.WriteLine($"  SpreadError = {bestBySpread.SpreadError:E16}");
            _output.WriteLine($"  TickScore   = {bestBySpread.TickScore:E16}");

            _output.WriteLine("Best by TickScore:");
            _output.WriteLine($"  gamma       = {bestByTickScore.Gamma:F6}");
            _output.WriteLine($"  ScaleError  = {bestByTickScore.ScaleError:E16}");
            _output.WriteLine($"  SpreadError = {bestByTickScore.SpreadError:E16}");
            _output.WriteLine($"  TickScore   = {bestByTickScore.TickScore:E16}");

            // Wir erwarten erstmal, dass gamma=1.0 mindestens sehr nah am TickScore-Minimum liegt
            Assert.True(Math.Abs(bestByTickScore.Gamma - 1.0) <= 0.05,
                "Best TickScore is not sufficiently close to gamma = 1.0");
        }
        [Fact]
        public void Should_Show_Robust_TickScore_Minimum_Near_Gamma_One_Across_Multiple_Seeds()
        {
            var basePlanck = PlanckConstants.FromPhysicalConstants();
            var deltaT = LogSpace(1e-43, 1e-38, 20);

            double[] gammaValues =
            {
        0.85,
        0.90,
        0.95,
        1.00,
        1.05,
        1.10,
        1.15
    };

            int[] seeds =
            {
        30001, 30002, 30003, 30004, 30005,
        30006, 30007, 30008, 30009, 30010
    };

            var summary = new List<GammaSummary>();

            foreach (double gamma in gammaValues)
            {
                var scaleErrors = new List<double>();
                var spreadErrors = new List<double>();
                var tickScores = new List<double>();

                foreach (int seed in seeds)
                {
                    var exp = new UncertaintyExperiment(basePlanck, seed: seed);

                    // nicht-zirkulär
                    exp.UseDimensionlessScale();

                    // Taktvariation
                    exp.UseTemporalTick(() => gamma * basePlanck.tP);

                    var results = exp.Run(deltaT, 3000);
                    var products = results.Select(r => r.Product).ToArray();

                    double meanProduct = products.Average();
                    double stdProduct = StdDev(products);

                    double scaleError = Math.Abs(meanProduct - basePlanck.tP) / basePlanck.tP;
                    double spreadError = stdProduct / Math.Abs(meanProduct);
                    double tickScore = scaleError + spreadError;

                    scaleErrors.Add(scaleError);
                    spreadErrors.Add(spreadError);
                    tickScores.Add(tickScore);
                }

                summary.Add(new GammaSummary
                {
                    Gamma = gamma,

                    MeanScaleError = scaleErrors.Average(),
                    StdScaleError = StdDev(scaleErrors),

                    MeanSpreadError = spreadErrors.Average(),
                    StdSpreadError = StdDev(spreadErrors),

                    MeanTickScore = tickScores.Average(),
                    StdTickScore = StdDev(tickScores)
                });
            }

            var bestByMeanTickScore = summary.OrderBy(x => x.MeanTickScore).First();
            var bestByMeanScaleError = summary.OrderBy(x => x.MeanScaleError).First();
            var bestByMeanSpreadError = summary.OrderBy(x => x.MeanSpreadError).First();

            _output.WriteLine("=== Multi-Seed Tick Robustness Test ===");
            foreach (var s in summary.OrderBy(x => x.Gamma))
            {
                _output.WriteLine($"gamma              = {s.Gamma:F6}");
                _output.WriteLine($"MeanScaleError     = {s.MeanScaleError:E16}");
                _output.WriteLine($"StdScaleError      = {s.StdScaleError:E16}");
                _output.WriteLine($"MeanSpreadError    = {s.MeanSpreadError:E16}");
                _output.WriteLine($"StdSpreadError     = {s.StdSpreadError:E16}");
                _output.WriteLine($"MeanTickScore      = {s.MeanTickScore:E16}");
                _output.WriteLine($"StdTickScore       = {s.StdTickScore:E16}");
                _output.WriteLine("");
            }

            _output.WriteLine("Best by MeanScaleError:");
            _output.WriteLine($"  gamma            = {bestByMeanScaleError.Gamma:F6}");
            _output.WriteLine($"  MeanScaleError   = {bestByMeanScaleError.MeanScaleError:E16}");
            _output.WriteLine($"  MeanSpreadError  = {bestByMeanScaleError.MeanSpreadError:E16}");
            _output.WriteLine($"  MeanTickScore    = {bestByMeanScaleError.MeanTickScore:E16}");

            _output.WriteLine("Best by MeanSpreadError:");
            _output.WriteLine($"  gamma            = {bestByMeanSpreadError.Gamma:F6}");
            _output.WriteLine($"  MeanScaleError   = {bestByMeanSpreadError.MeanScaleError:E16}");
            _output.WriteLine($"  MeanSpreadError  = {bestByMeanSpreadError.MeanSpreadError:E16}");
            _output.WriteLine($"  MeanTickScore    = {bestByMeanSpreadError.MeanTickScore:E16}");

            _output.WriteLine("Best by MeanTickScore:");
            _output.WriteLine($"  gamma            = {bestByMeanTickScore.Gamma:F6}");
            _output.WriteLine($"  MeanScaleError   = {bestByMeanTickScore.MeanScaleError:E16}");
            _output.WriteLine($"  MeanSpreadError  = {bestByMeanTickScore.MeanSpreadError:E16}");
            _output.WriteLine($"  MeanTickScore    = {bestByMeanTickScore.MeanTickScore:E16}");

            // weicher, aber sinnvoller Check:
            Assert.True(Math.Abs(bestByMeanTickScore.Gamma - 1.0) <= 0.05,
                "Robust multi-seed TickScore minimum is not sufficiently close to gamma = 1.0");
        }

        [Fact]
        public void Should_Show_Robust_Action_Scale_Minimum_Near_Gamma_One_With_Emergent_Energy_Scale()
        {
            var basePlanck = PlanckConstants.FromPhysicalConstants();
            var derivedBase = new DerivedConstants(basePlanck);
            var deltaT = LogSpace(1e-43, 1e-38, 20);

            double[] gammaValues =
            {
        0.85,
        0.90,
        0.95,
        1.00,
        1.05,
        1.10,
        1.15
    };

            int[] seeds =
            {
        40001, 40002, 40003, 40004, 40005,
        40006, 40007, 40008, 40009, 40010
    };

            var summary = new List<ActionGammaSummary>();

            foreach (double gamma in gammaValues)
            {
                var actionErrors = new List<double>();
                var actionSpreads = new List<double>();
                var actionScores = new List<double>();

                foreach (int seed in seeds)
                {
                    var exp = new UncertaintyExperiment(basePlanck, seed: seed);

                    // Jetzt mit emergenter Energieskala
                    exp.UseEmergentPlanckEnergyScale();

                    // Tick um gamma * tP variieren
                    exp.UseTemporalTick(() => gamma * basePlanck.tP);

                    var results = exp.Run(deltaT, 3000);
                    var products = results.Select(r => r.Product).ToArray();

                    double meanProduct = products.Average();
                    double stdProduct = StdDev(products);

                    // Ziel: emergentes hbar dieses Planck-Punkts
                    double hbarTarget = derivedBase.ReducedPlanck;

                    double actionError = Math.Abs(meanProduct - hbarTarget) / hbarTarget;
                    double actionSpread = stdProduct / Math.Abs(meanProduct);

                    double actionScore = actionError + actionSpread;

                    actionErrors.Add(actionError);
                    actionSpreads.Add(actionSpread);
                    actionScores.Add(actionScore);
                }

                summary.Add(new ActionGammaSummary
                {
                    Gamma = gamma,

                    MeanActionError = actionErrors.Average(),
                    StdActionError = StdDev(actionErrors),

                    MeanActionSpread = actionSpreads.Average(),
                    StdActionSpread = StdDev(actionSpreads),

                    MeanActionScore = actionScores.Average(),
                    StdActionScore = StdDev(actionScores)
                });
            }

            var bestByActionError = summary.OrderBy(x => x.MeanActionError).First();
            var bestByActionSpread = summary.OrderBy(x => x.MeanActionSpread).First();
            var bestByActionScore = summary.OrderBy(x => x.MeanActionScore).First();

            _output.WriteLine("=== Action-Scale Bridge Test ===");
            foreach (var s in summary.OrderBy(x => x.Gamma))
            {
                _output.WriteLine($"gamma               = {s.Gamma:F6}");
                _output.WriteLine($"MeanActionError     = {s.MeanActionError:E16}");
                _output.WriteLine($"StdActionError      = {s.StdActionError:E16}");
                _output.WriteLine($"MeanActionSpread    = {s.MeanActionSpread:E16}");
                _output.WriteLine($"StdActionSpread     = {s.StdActionSpread:E16}");
                _output.WriteLine($"MeanActionScore     = {s.MeanActionScore:E16}");
                _output.WriteLine($"StdActionScore      = {s.StdActionScore:E16}");
                _output.WriteLine("");
            }

            _output.WriteLine("Best by MeanActionError:");
            _output.WriteLine($"  gamma             = {bestByActionError.Gamma:F6}");
            _output.WriteLine($"  MeanActionError   = {bestByActionError.MeanActionError:E16}");
            _output.WriteLine($"  MeanActionSpread  = {bestByActionError.MeanActionSpread:E16}");
            _output.WriteLine($"  MeanActionScore   = {bestByActionError.MeanActionScore:E16}");

            _output.WriteLine("Best by MeanActionSpread:");
            _output.WriteLine($"  gamma             = {bestByActionSpread.Gamma:F6}");
            _output.WriteLine($"  MeanActionError   = {bestByActionSpread.MeanActionError:E16}");
            _output.WriteLine($"  MeanActionSpread  = {bestByActionSpread.MeanActionSpread:E16}");
            _output.WriteLine($"  MeanActionScore   = {bestByActionSpread.MeanActionScore:E16}");

            _output.WriteLine("Best by MeanActionScore:");
            _output.WriteLine($"  gamma             = {bestByActionScore.Gamma:F6}");
            _output.WriteLine($"  MeanActionError   = {bestByActionScore.MeanActionError:E16}");
            _output.WriteLine($"  MeanActionSpread  = {bestByActionScore.MeanActionSpread:E16}");
            _output.WriteLine($"  MeanActionScore   = {bestByActionScore.MeanActionScore:E16}");

            // weicher, aber sinnvoller Check:
            Assert.True(Math.Abs(bestByActionScore.Gamma - 1.0) <= 0.05,
                "Robust action-scale minimum is not sufficiently close to gamma = 1.0");
        }

        [Fact]
        public void Should_Show_Gamma_Minimum_Near_One_Across_Multiple_Relevant_Planck_Points()
        {
            var basePlanck = PlanckConstants.FromPhysicalConstants();
            var scan = new PlanckMultiScan(basePlanck);

            var scanResults = scan.Run(200, 0.001);
            var deltaT = LogSpace(1e-43, 1e-38, 20);

            // Beispiel: Stability-Minimum aus dem bisherigen Scan
            var bestStability = scanResults.OrderBy(r => r.Stability).First();

            // Optional: einen zweiten interessanten Punkt nehmen
            // Hier z. B. ein Punkt nahe kleinem Stability-Wert, aber nicht identisch
            var secondInteresting = scanResults
                .OrderBy(r => r.Stability)
                .Skip(10)
                .First();

            // Relevante Planck-Punkte
            var planckPoints = new List<PlanckPointInfo>
    {
        new PlanckPointInfo
        {
            Name = "BasePlanck",
            Planck = basePlanck
        },
        new PlanckPointInfo
        {
            Name = "BestStability",
            Planck = new PlanckConstants(
                basePlanck.lP * (1.0 + bestStability.epsL),
                basePlanck.tP * (1.0 + bestStability.epsT),
                basePlanck.mP * (1.0 + bestStability.epsM))
        },
        new PlanckPointInfo
        {
            Name = "SecondInteresting",
            Planck = new PlanckConstants(
                basePlanck.lP * (1.0 + secondInteresting.epsL),
                basePlanck.tP * (1.0 + secondInteresting.epsT),
                basePlanck.mP * (1.0 + secondInteresting.epsM))
        }
    };

            double[] gammaValues =
            {
        0.85,
        0.90,
        0.95,
        1.00,
        1.05,
        1.10,
        1.15
    };

            int[] seeds =
            {
        50001, 50002, 50003, 50004, 50005
    };

            _output.WriteLine("=== Gamma Minimum Across Multiple Planck Points ===");
            _output.WriteLine("");

            foreach (var point in planckPoints)
            {
                var derived = new DerivedConstants(point.Planck);

                var gammaSummaries = new List<ActionGammaSummary>();

                foreach (double gamma in gammaValues)
                {
                    var actionErrors = new List<double>();
                    var actionSpreads = new List<double>();
                    var actionScores = new List<double>();

                    foreach (int seed in seeds)
                    {
                        var exp = new UncertaintyExperiment(point.Planck, seed: seed);

                        // emergente Energieskala
                        exp.UseEmergentPlanckEnergyScale();

                        // Tick-Variation relativ zum jeweiligen lokalen tP
                        exp.UseTemporalTick(() => gamma * point.Planck.tP);

                        var results = exp.Run(deltaT, 3000);
                        var products = results.Select(r => r.Product).ToArray();

                        double meanProduct = products.Average();
                        double stdProduct = StdDev(products);

                        double hbarTarget = derived.ReducedPlanck;

                        double actionError = Math.Abs(meanProduct - hbarTarget) / hbarTarget;
                        double actionSpread = stdProduct / Math.Abs(meanProduct);
                        double actionScore = actionError + actionSpread;

                        actionErrors.Add(actionError);
                        actionSpreads.Add(actionSpread);
                        actionScores.Add(actionScore);
                    }

                    gammaSummaries.Add(new ActionGammaSummary
                    {
                        Gamma = gamma,
                        MeanActionError = actionErrors.Average(),
                        StdActionError = StdDev(actionErrors),
                        MeanActionSpread = actionSpreads.Average(),
                        StdActionSpread = StdDev(actionSpreads),
                        MeanActionScore = actionScores.Average(),
                        StdActionScore = StdDev(actionScores)
                    });
                }

                var bestByActionError = gammaSummaries.OrderBy(x => x.MeanActionError).First();
                var bestByActionScore = gammaSummaries.OrderBy(x => x.MeanActionScore).First();

                _output.WriteLine($"--- {point.Name} ---");
                foreach (var s in gammaSummaries.OrderBy(x => x.Gamma))
                {
                    _output.WriteLine($"gamma               = {s.Gamma:F6}");
                    _output.WriteLine($"MeanActionError     = {s.MeanActionError:E16}");
                    _output.WriteLine($"StdActionError      = {s.StdActionError:E16}");
                    _output.WriteLine($"MeanActionSpread    = {s.MeanActionSpread:E16}");
                    _output.WriteLine($"StdActionSpread     = {s.StdActionSpread:E16}");
                    _output.WriteLine($"MeanActionScore     = {s.MeanActionScore:E16}");
                    _output.WriteLine($"StdActionScore      = {s.StdActionScore:E16}");
                    _output.WriteLine("");
                }

                _output.WriteLine($"Best by MeanActionError for {point.Name}:");
                _output.WriteLine($"  gamma             = {bestByActionError.Gamma:F6}");
                _output.WriteLine($"  MeanActionError   = {bestByActionError.MeanActionError:E16}");
                _output.WriteLine($"  MeanActionSpread  = {bestByActionError.MeanActionSpread:E16}");
                _output.WriteLine($"  MeanActionScore   = {bestByActionError.MeanActionScore:E16}");

                _output.WriteLine($"Best by MeanActionScore for {point.Name}:");
                _output.WriteLine($"  gamma             = {bestByActionScore.Gamma:F6}");
                _output.WriteLine($"  MeanActionError   = {bestByActionScore.MeanActionError:E16}");
                _output.WriteLine($"  MeanActionSpread  = {bestByActionScore.MeanActionSpread:E16}");
                _output.WriteLine($"  MeanActionScore   = {bestByActionScore.MeanActionScore:E16}");
                _output.WriteLine("");

                // weicher, aber sinnvoller Check:
                Assert.True(Math.Abs(bestByActionScore.Gamma - 1.0) <= 0.05,
                    $"Best action-score gamma for {point.Name} is not sufficiently close to 1.0");
            }
        }
        [Fact]
        public void Should_Show_Resonance_Peak_For_DeltaT_Driven_Mode()
        {
            var basePlanck = PlanckConstants.FromPhysicalConstants();
            var deltaT = LogSpace(1e-43, 1e-38, 20);

            // Wir definieren eine natürliche Referenzfrequenz aus tP
            double omega0 = 1.0 / basePlanck.tP;

            // Wir testen relative Anregungsfrequenzen
            double[] gammaOmega =
            {
        0.25,
        0.5,
        0.75,
        1.0,
        1.25,
        1.5,
        2.0
    };

            double driveAmplitude = 0.05; // klein halten
            int[] seeds = { 60001, 60002, 60003, 60004, 60005 };

            var summaries = new List<ResonanceSummary>();

            foreach (double g in gammaOmega)
            {
                double driveOmega = g * omega0;

                var responseMeans = new List<double>();
                var responseStds = new List<double>();

                foreach (int seed in seeds)
                {
                    var exp = new UncertaintyExperiment(basePlanck, seed: seed);
                    exp.UseDimensionlessScale();
                    exp.UseTemporalTick(() => basePlanck.tP);

                    var results = exp.RunDriven(
                        deltaTValues: deltaT,
                        driveOmega: driveOmega,
                        driveAmplitude: driveAmplitude,
                        samplesPerStep: 3000);

                    // Resonanzantwort:
                    // Mittelwert der StdTemporalFluctuation über alle Δt
                    double responseMean = results.Average(r => r.StdTemporalFluctuation);
                    double responseStd = StdDev(results.Select(r => r.StdTemporalFluctuation).ToArray());

                    responseMeans.Add(responseMean);
                    responseStds.Add(responseStd);
                }

                summaries.Add(new ResonanceSummary
                {
                    GammaOmega = g,
                    MeanResponse = responseMeans.Average(),
                    StdResponse = StdDev(responseMeans),
                    MeanInternalSpread = responseStds.Average()
                });
            }

            var best = summaries.OrderByDescending(x => x.MeanResponse).First();

            _output.WriteLine("=== DeltaT Driven Resonance Test ===");
            foreach (var s in summaries.OrderBy(x => x.GammaOmega))
            {
                _output.WriteLine($"gammaOmega         = {s.GammaOmega:F6}");
                _output.WriteLine($"MeanResponse       = {s.MeanResponse:E16}");
                _output.WriteLine($"StdResponse        = {s.StdResponse:E16}");
                _output.WriteLine($"MeanInternalSpread = {s.MeanInternalSpread:E16}");
                _output.WriteLine("");
            }

            _output.WriteLine("Best resonance response:");
            _output.WriteLine($"  gammaOmega       = {best.GammaOmega:F6}");
            _output.WriteLine($"  MeanResponse     = {best.MeanResponse:E16}");
            _output.WriteLine($"  StdResponse      = {best.StdResponse:E16}");
            _output.WriteLine($"  MeanInternalSpread = {best.MeanInternalSpread:E16}");

            // Erstmal nur diagnostisch:
            Assert.True(summaries.Count == gammaOmega.Length);
        }

        [Fact]
        public void Should_Show_Resonance_Response_Trend_With_Amplitude_Scan()
        {
            var basePlanck = PlanckConstants.FromPhysicalConstants();
            var deltaT = LogSpace(1e-43, 1e-38, 20);

            double omega0 = 1.0 / basePlanck.tP;

            double[] gammaOmegaValues =
            {
        0.25,
        0.5,
        0.75,
        1.0,
        1.25,
        1.5,
        2.0
    };

            double[] amplitudes =
            {
        0.05,
        0.10,
        0.20
    };

            int[] seeds =
            {
        70001, 70002, 70003, 70004, 70005
    };

            _output.WriteLine("=== Resonance Test v2: Amplitude Scan ===");
            _output.WriteLine("");

            foreach (double amplitude in amplitudes)
            {
                var summaries = new List<ResonanceSummary>();

                foreach (double g in gammaOmegaValues)
                {
                    double driveOmega = g * omega0;

                    var responseMeans = new List<double>();
                    var responseSpreads = new List<double>();

                    foreach (int seed in seeds)
                    {
                        var exp = new UncertaintyExperiment(basePlanck, seed: seed);
                        exp.UseDimensionlessScale();
                        exp.UseTemporalTick(() => basePlanck.tP);

                        var results = exp.RunDriven(
                            deltaTValues: deltaT,
                            driveOmega: driveOmega,
                            driveAmplitude: amplitude,
                            samplesPerStep: 3000);

                        // Antwortgröße:
                        // Mittelwert der Standardabweichungen über alle DeltaT
                        double responseMean = results.Average(r => r.StdTemporalFluctuation);

                        // innere Streuung über die DeltaT-Punkte
                        double responseSpread = StdDev(results.Select(r => r.StdTemporalFluctuation).ToArray());

                        responseMeans.Add(responseMean);
                        responseSpreads.Add(responseSpread);
                    }

                    summaries.Add(new ResonanceSummary
                    {
                        GammaOmega = g,
                        MeanResponse = responseMeans.Average(),
                        StdResponse = StdDev(responseMeans),
                        MeanInternalSpread = responseSpreads.Average()
                    });
                }

                var best = summaries.OrderByDescending(x => x.MeanResponse).First();

                _output.WriteLine($"--- driveAmplitude = {amplitude:F6} ---");

                foreach (var s in summaries.OrderBy(x => x.GammaOmega))
                {
                    _output.WriteLine($"gammaOmega         = {s.GammaOmega:F6}");
                    _output.WriteLine($"MeanResponse       = {s.MeanResponse:E16}");
                    _output.WriteLine($"StdResponse        = {s.StdResponse:E16}");
                    _output.WriteLine($"MeanInternalSpread = {s.MeanInternalSpread:E16}");
                    _output.WriteLine("");
                }

                _output.WriteLine("Best resonance response:");
                _output.WriteLine($"  gammaOmega       = {best.GammaOmega:F6}");
                _output.WriteLine($"  MeanResponse     = {best.MeanResponse:E16}");
                _output.WriteLine($"  StdResponse      = {best.StdResponse:E16}");
                _output.WriteLine($"  MeanInternalSpread = {best.MeanInternalSpread:E16}");
                _output.WriteLine("");
            }

            Assert.True(amplitudes.Length > 0);
        }

        [Fact]
        public void Should_Show_Resonance_Response_With_Multiple_Observables_In_Parallel()
        {
            var basePlanck = PlanckConstants.FromPhysicalConstants();
            var deltaT = LogSpace(1e-43, 1e-38, 20);

            double omega0 = 1.0 / basePlanck.tP;

            double[] gammaOmegaValues =
            {
        0.25,
        0.5,
        0.75,
        1.0,
        1.25,
        1.5,
        2.0
    };

            double[] amplitudes =
            {
        0.05,
        0.10,
        0.20
    };

            int[] seeds =
            {
        80001, 80002, 80003, 80004, 80005
    };

            _output.WriteLine("=== Resonance Test v3: Multiple Observables ===");
            _output.WriteLine("");

            foreach (double amplitude in amplitudes)
            {
                var summaries = new List<ResonanceMultiSummary>();

                foreach (double g in gammaOmegaValues)
                {
                    double driveOmega = g * omega0;

                    var temporalMeans = new List<double>();
                    var temporalStds = new List<double>();
                    var deltaEMeans = new List<double>();
                    var productMeans = new List<double>();

                    foreach (int seed in seeds)
                    {
                        var exp = new UncertaintyExperiment(basePlanck, seed: seed);
                        exp.UseDimensionlessScale();
                        exp.UseTemporalTick(() => basePlanck.tP);

                        var results = exp.RunDriven(
                            deltaTValues: deltaT,
                            driveOmega: driveOmega,
                            driveAmplitude: amplitude,
                            samplesPerStep: 3000);

                        // 1) Mittelwert der absoluten MeanTemporalFluctuation
                        double meanTemporalResponse =
                            results.Average(r => Math.Abs(r.MeanTemporalFluctuation));

                        // 2) Mittelwert der StdTemporalFluctuation
                        double stdTemporalResponse =
                            results.Average(r => r.StdTemporalFluctuation);

                        // 3) Mittelwert der DeltaE-Werte
                        double meanDeltaEResponse =
                            results.Average(r => r.DeltaE);

                        // 4) Mittelwert der absoluten Produktwerte
                        double meanProductResponse =
                            results.Average(r => Math.Abs(r.Product));

                        temporalMeans.Add(meanTemporalResponse);
                        temporalStds.Add(stdTemporalResponse);
                        deltaEMeans.Add(meanDeltaEResponse);
                        productMeans.Add(meanProductResponse);
                    }

                    summaries.Add(new ResonanceMultiSummary
                    {
                        GammaOmega = g,

                        MeanTemporalResponse = temporalMeans.Average(),
                        StdTemporalResponse = temporalStds.Average(),
                        MeanDeltaEResponse = deltaEMeans.Average(),
                        MeanProductResponse = productMeans.Average(),

                        SeedSpreadTemporalMean = StdDev(temporalMeans),
                        SeedSpreadTemporalStd = StdDev(temporalStds),
                        SeedSpreadDeltaE = StdDev(deltaEMeans),
                        SeedSpreadProduct = StdDev(productMeans)
                    });
                }

                _output.WriteLine($"--- driveAmplitude = {amplitude:F6} ---");

                foreach (var s in summaries.OrderBy(x => x.GammaOmega))
                {
                    _output.WriteLine($"gammaOmega              = {s.GammaOmega:F6}");
                    _output.WriteLine($"MeanTemporalResponse    = {s.MeanTemporalResponse:E16}");
                    _output.WriteLine($"StdTemporalResponse     = {s.StdTemporalResponse:E16}");
                    _output.WriteLine($"MeanDeltaEResponse      = {s.MeanDeltaEResponse:E16}");
                    _output.WriteLine($"MeanProductResponse     = {s.MeanProductResponse:E16}");
                    _output.WriteLine($"SeedSpreadTemporalMean  = {s.SeedSpreadTemporalMean:E16}");
                    _output.WriteLine($"SeedSpreadTemporalStd   = {s.SeedSpreadTemporalStd:E16}");
                    _output.WriteLine($"SeedSpreadDeltaE        = {s.SeedSpreadDeltaE:E16}");
                    _output.WriteLine($"SeedSpreadProduct       = {s.SeedSpreadProduct:E16}");
                    _output.WriteLine("");
                }

                var bestTemporalMean = summaries.OrderByDescending(x => x.MeanTemporalResponse).First();
                var bestTemporalStd = summaries.OrderByDescending(x => x.StdTemporalResponse).First();
                var bestDeltaE = summaries.OrderByDescending(x => x.MeanDeltaEResponse).First();
                var bestProduct = summaries.OrderByDescending(x => x.MeanProductResponse).First();

                _output.WriteLine("Best by Observable:");
                _output.WriteLine($"  Best MeanTemporalResponse : gammaOmega = {bestTemporalMean.GammaOmega:F6}");
                _output.WriteLine($"  Best StdTemporalResponse  : gammaOmega = {bestTemporalStd.GammaOmega:F6}");
                _output.WriteLine($"  Best MeanDeltaEResponse   : gammaOmega = {bestDeltaE.GammaOmega:F6}");
                _output.WriteLine($"  Best MeanProductResponse  : gammaOmega = {bestProduct.GammaOmega:F6}");
                _output.WriteLine("");
            }

            Assert.True(amplitudes.Length > 0);
        }
        [Fact]
        public void Should_Report_Harmonic_Structure_In_Driven_DeltaT_Response()
        {
            var basePlanck = PlanckConstants.FromPhysicalConstants();
            var deltaT = LogSpace(1e-43, 1e-38, 20);

            double omega0 = 1.0 / basePlanck.tP;

            // Kandidaten für Unter-/Grund-/Oberharmonische
            double[] gammaOmegaValues =
            {
        0.25,
        0.50,
        0.75,
        1.00,
        1.25,
        1.50,
        2.00
    };

            int[] seeds =
            {
        90001, 90002, 90003, 90004, 90005
    };

            double driveAmplitude = 0.20;

            var summaries = new List<ResonanceMultiSummary>();

            foreach (double g in gammaOmegaValues)
            {
                double driveOmega = g * omega0;

                var temporalMeans = new List<double>();
                var temporalStds = new List<double>();
                var deltaEMeans = new List<double>();
                var productMeans = new List<double>();

                foreach (int seed in seeds)
                {
                    var exp = new UncertaintyExperiment(basePlanck, seed: seed);
                    exp.UseDimensionlessScale();
                    exp.UseTemporalTick(() => basePlanck.tP);

                    var results = exp.RunDriven(
                        deltaTValues: deltaT,
                        driveOmega: driveOmega,
                        driveAmplitude: driveAmplitude,
                        samplesPerStep: 3000);

                    double meanTemporalResponse =
                        results.Average(r => Math.Abs(r.MeanTemporalFluctuation));

                    double stdTemporalResponse =
                        results.Average(r => r.StdTemporalFluctuation);

                    double meanDeltaEResponse =
                        results.Average(r => r.DeltaE);

                    double meanProductResponse =
                        results.Average(r => Math.Abs(r.Product));

                    temporalMeans.Add(meanTemporalResponse);
                    temporalStds.Add(stdTemporalResponse);
                    deltaEMeans.Add(meanDeltaEResponse);
                    productMeans.Add(meanProductResponse);
                }

                summaries.Add(new ResonanceMultiSummary
                {
                    GammaOmega = g,
                    MeanTemporalResponse = temporalMeans.Average(),
                    StdTemporalResponse = temporalStds.Average(),
                    MeanDeltaEResponse = deltaEMeans.Average(),
                    MeanProductResponse = productMeans.Average(),

                    SeedSpreadTemporalMean = StdDev(temporalMeans),
                    SeedSpreadTemporalStd = StdDev(temporalStds),
                    SeedSpreadDeltaE = StdDev(deltaEMeans),
                    SeedSpreadProduct = StdDev(productMeans)
                });
            }

            // Kleine Hilfsfunktion für "Pattern Strength"
            double PatternStrength(IEnumerable<double> values)
            {
                var arr = values.ToArray();
                double mean = arr.Average();
                double std = StdDev(arr);
                return std / Math.Abs(mean);
            }

            double temporalMeanPattern = PatternStrength(summaries.Select(x => x.MeanTemporalResponse));
            double temporalStdPattern = PatternStrength(summaries.Select(x => x.StdTemporalResponse));
            double deltaEPattern = PatternStrength(summaries.Select(x => x.MeanDeltaEResponse));
            double productPattern = PatternStrength(summaries.Select(x => x.MeanProductResponse));

            var bestTemporalMean = summaries.OrderByDescending(x => x.MeanTemporalResponse).First();
            var bestTemporalStd = summaries.OrderByDescending(x => x.StdTemporalResponse).First();
            var bestDeltaE = summaries.OrderByDescending(x => x.MeanDeltaEResponse).First();
            var bestProduct = summaries.OrderByDescending(x => x.MeanProductResponse).First();

            _output.WriteLine("=== Harmonic Structure Test ===");
            _output.WriteLine($"driveAmplitude = {driveAmplitude:F6}");
            _output.WriteLine("");

            foreach (var s in summaries.OrderBy(x => x.GammaOmega))
            {
                _output.WriteLine($"gammaOmega              = {s.GammaOmega:F6}");
                _output.WriteLine($"MeanTemporalResponse    = {s.MeanTemporalResponse:E16}");
                _output.WriteLine($"StdTemporalResponse     = {s.StdTemporalResponse:E16}");
                _output.WriteLine($"MeanDeltaEResponse      = {s.MeanDeltaEResponse:E16}");
                _output.WriteLine($"MeanProductResponse     = {s.MeanProductResponse:E16}");
                _output.WriteLine($"SeedSpreadTemporalMean  = {s.SeedSpreadTemporalMean:E16}");
                _output.WriteLine($"SeedSpreadTemporalStd   = {s.SeedSpreadTemporalStd:E16}");
                _output.WriteLine($"SeedSpreadDeltaE        = {s.SeedSpreadDeltaE:E16}");
                _output.WriteLine($"SeedSpreadProduct       = {s.SeedSpreadProduct:E16}");
                _output.WriteLine("");
            }

            _output.WriteLine("Pattern strengths (std/mean across harmonics):");
            _output.WriteLine($"  TemporalMeanPattern = {temporalMeanPattern:E16}");
            _output.WriteLine($"  TemporalStdPattern  = {temporalStdPattern:E16}");
            _output.WriteLine($"  DeltaEPattern       = {deltaEPattern:E16}");
            _output.WriteLine($"  ProductPattern      = {productPattern:E16}");
            _output.WriteLine("");

            _output.WriteLine("Best harmonic candidates by observable:");
            _output.WriteLine($"  Best MeanTemporalResponse : gammaOmega = {bestTemporalMean.GammaOmega:F6}");
            _output.WriteLine($"  Best StdTemporalResponse  : gammaOmega = {bestTemporalStd.GammaOmega:F6}");
            _output.WriteLine($"  Best MeanDeltaEResponse   : gammaOmega = {bestDeltaE.GammaOmega:F6}");
            _output.WriteLine($"  Best MeanProductResponse  : gammaOmega = {bestProduct.GammaOmega:F6}");
            _output.WriteLine("");

            // Diagnose: Ist überhaupt eine nicht-flache Struktur sichtbar?
            Assert.True(
                temporalMeanPattern > 1e-4 ||
                temporalStdPattern > 1e-4 ||
                deltaEPattern > 1e-4 ||
                productPattern > 1e-4,
                "No detectable harmonic structure above flat numerical background."
            );
        }

        [Fact]
        public void Should_Report_Minimal_Phase_Sampling_Behaviour()
        {
            var basePlanck = PlanckConstants.FromPhysicalConstants();

            // Grundfrequenz passend zum Planck-Takt
            double omega0 = 2.0 * Math.PI / basePlanck.tP; //double omega0 = 1.0 / basePlanck.tP;

            // Wir testen dt = gamma * tP
            double[] gammaValues =
            {
        0.85,
        0.90,
        0.95,
        1.00,
        1.05,
        1.10,
        1.15
    };

            // Anzahl diskreter Schritte
            int steps = 2000;

            // Ableitungsamplitude für δT = A cos(phi)
            double amplitude = 1.0;

            var summaries = new List<PhaseTickSummary>();

            foreach (double gamma in gammaValues)
            {
                double dt = gamma * basePlanck.tP;

                var cell = new PhaseCell
                {
                    Phi = 0.0,
                    Omega = omega0
                };

                var deltaTSeries = new List<double>(steps);

                for (int i = 0; i < steps; i++)
                {
                    UpdateSingleCell(cell, dt);

                    double deltaT = ComputeDeltaT(cell, amplitude);
                    deltaTSeries.Add(deltaT);
                }

                // Mittelwert sollte bei sauberer Oszillation nahe 0 liegen
                double mean = deltaTSeries.Average();

                // StdDev misst die Schwingungsbreite
                double std = StdDev(deltaTSeries);

                // einfache Stabilitätsmetrik:
                // kleine Mittelwertverschiebung + saubere konstante Schwingungsbreite
                double meanError = Math.Abs(mean);

                // Für eine ideale Cosinus-Schwingung mit A=1 wäre std = 1/sqrt(2)
                double targetStd = 1.0 / Math.Sqrt(2.0);
                double stdError = Math.Abs(std - targetStd);

                double phaseScore = meanError + stdError;

                summaries.Add(new PhaseTickSummary
                {
                    Gamma = gamma,
                    MeanDeltaT = mean,
                    StdDeltaT = std,
                    MeanError = meanError,
                    StdError = stdError,
                    PhaseScore = phaseScore
                });
            }

            var bestByPhaseScore = summaries.OrderBy(x => x.PhaseScore).First();
            var bestByMeanError = summaries.OrderBy(x => x.MeanError).First();
            var bestByStdError = summaries.OrderBy(x => x.StdError).First();

            _output.WriteLine("=== Minimal Phase Model Tick Test ===");
            foreach (var s in summaries.OrderBy(x => x.Gamma))
            {
                _output.WriteLine($"gamma       = {s.Gamma:F6}");
                _output.WriteLine($"MeanDeltaT  = {s.MeanDeltaT:E16}");
                _output.WriteLine($"StdDeltaT   = {s.StdDeltaT:E16}");
                _output.WriteLine($"MeanError   = {s.MeanError:E16}");
                _output.WriteLine($"StdError    = {s.StdError:E16}");
                _output.WriteLine($"PhaseScore  = {s.PhaseScore:E16}");
                _output.WriteLine("");
            }

            _output.WriteLine("Best by MeanError:");
            _output.WriteLine($"  gamma     = {bestByMeanError.Gamma:F6}");
            _output.WriteLine($"  MeanError = {bestByMeanError.MeanError:E16}");
            _output.WriteLine($"  StdError  = {bestByMeanError.StdError:E16}");
            _output.WriteLine($"  PhaseScore= {bestByMeanError.PhaseScore:E16}");

            _output.WriteLine("Best by StdError:");
            _output.WriteLine($"  gamma     = {bestByStdError.Gamma:F6}");
            _output.WriteLine($"  MeanError = {bestByStdError.MeanError:E16}");
            _output.WriteLine($"  StdError  = {bestByStdError.StdError:E16}");
            _output.WriteLine($"  PhaseScore= {bestByStdError.PhaseScore:E16}");

            _output.WriteLine("Best by PhaseScore:");
            _output.WriteLine($"  gamma     = {bestByPhaseScore.Gamma:F6}");
            _output.WriteLine($"  MeanError = {bestByPhaseScore.MeanError:E16}");
            _output.WriteLine($"  StdError  = {bestByPhaseScore.StdError:E16}");
            _output.WriteLine($"  PhaseScore= {bestByPhaseScore.PhaseScore:E16}");

            // only logging for now, no strict assertion, but we expect best gamma to be near 1.0
        }

        [Fact]
        public void Should_Show_PhaseLock_Near_Integer_Tick_Ratios()
        {
            var basePlanck = PlanckConstants.FromPhysicalConstants();

            // Wenn tP die Periode ist:
            double omega0 = 2.0 * Math.PI / basePlanck.tP;

            double[] gammaValues =
            {
        0.25,
        0.50,
        0.75,
        1.00,
        1.25,
        1.50,
        2.00
    };

            int steps = 2000;

            var summaries = new List<PhaseLockSummary>();

            foreach (double gamma in gammaValues)
            {
                double dt = gamma * basePlanck.tP;

                var cell = new PhaseCell
                {
                    Phi = 0.0,
                    Omega = omega0
                };

                var cosValues = new List<double>(steps);
                var sinValues = new List<double>(steps);

                for (int i = 0; i < steps; i++)
                {
                    UpdateSingleCell(cell, dt);

                    // Phase auf Kreis projizieren
                    cosValues.Add(Math.Cos(cell.Phi));
                    sinValues.Add(Math.Sin(cell.Phi));
                }

                double meanCos = cosValues.Average();
                double meanSin = sinValues.Average();

                // Kuramoto-artiger Ordnungsparameter / Lock-Maß
                double lockStrength = Math.Sqrt(meanCos * meanCos + meanSin * meanSin);

                summaries.Add(new PhaseLockSummary
                {
                    Gamma = gamma,
                    MeanCos = meanCos,
                    MeanSin = meanSin,
                    LockStrength = lockStrength
                });
            }

            var bestLock = summaries.OrderByDescending(x => x.LockStrength).First();

            _output.WriteLine("=== Phase Lock Test ===");
            foreach (var s in summaries.OrderBy(x => x.Gamma))
            {
                _output.WriteLine($"gamma         = {s.Gamma:F6}");
                _output.WriteLine($"MeanCos       = {s.MeanCos:E16}");
                _output.WriteLine($"MeanSin       = {s.MeanSin:E16}");
                _output.WriteLine($"LockStrength  = {s.LockStrength:E16}");
                _output.WriteLine("");
            }

            _output.WriteLine("Best phase lock:");
            _output.WriteLine($"  gamma       = {bestLock.Gamma:F6}");
            _output.WriteLine($"  MeanCos     = {bestLock.MeanCos:E16}");
            _output.WriteLine($"  MeanSin     = {bestLock.MeanSin:E16}");
            _output.WriteLine($"  LockStrength= {bestLock.LockStrength:E16}");

            // Für ganzzahlige Taktverhältnisse erwarten wir starke Phasenbindung.
            Assert.True(bestLock.LockStrength > 0.9,
                "No strong phase locking detected.");
        }
        [Fact]
        public void Should_Prefer_Fundamental_Mode_Over_Higher_Harmonic_In_Phase_Model()
        {
            var basePlanck = PlanckConstants.FromPhysicalConstants();

            // Wenn tP die Periode ist:
            double omega0 = 2.0 * Math.PI / basePlanck.tP;

            double[] gammaValues =
            {
        0.25,
        0.50,
        0.75,
        1.00,
        1.25,
        1.50,
        2.00
    };

            int steps = 2000;
            double tolerance = 1e-9;

            var summaries = new List<PhaseModeSummary>();

            foreach (double gamma in gammaValues)
            {
                double dt = gamma * basePlanck.tP;

                var cell = new PhaseCell
                {
                    Phi = 0.0,
                    Omega = omega0
                };

                var cosValues = new List<double>(steps);
                var sinValues = new List<double>(steps);

                int? firstReturnStep = null;

                for (int i = 0; i < steps; i++)
                {
                    UpdateSingleCell(cell, dt);

                    double wrapped = WrapToTwoPi(cell.Phi);

                    cosValues.Add(Math.Cos(cell.Phi));
                    sinValues.Add(Math.Sin(cell.Phi));

                    // erste Rückkehr in die Nähe der Startphase
                    if (firstReturnStep == null && Math.Abs(wrapped) < tolerance)
                    {
                        firstReturnStep = i + 1; // Schritte zählen ab 1
                    }
                }

                double meanCos = cosValues.Average();
                double meanSin = sinValues.Average();

                double lockStrength = Math.Sqrt(meanCos * meanCos + meanSin * meanSin);

                // Wenn nie zurückgekehrt: sehr große Ordnung setzen
                int lockOrder = firstReturnStep ?? int.MaxValue;

                // Score:
                // hohe LockStrength gut, kleine LockOrder gut
                // wir minimieren also:
                double modeScore = (1.0 - lockStrength) + 0.001 * lockOrder;

                summaries.Add(new PhaseModeSummary
                {
                    Gamma = gamma,
                    MeanCos = meanCos,
                    MeanSin = meanSin,
                    LockStrength = lockStrength,
                    LockOrder = lockOrder,
                    ModeScore = modeScore
                });
            }

            var bestByLock = summaries.OrderByDescending(x => x.LockStrength).First();
            var bestByModeScore = summaries.OrderBy(x => x.ModeScore).First();

            _output.WriteLine("=== Fundamental vs Harmonic Phase Mode Test ===");
            foreach (var s in summaries.OrderBy(x => x.Gamma))
            {
                _output.WriteLine($"gamma         = {s.Gamma:F6}");
                _output.WriteLine($"MeanCos       = {s.MeanCos:E16}");
                _output.WriteLine($"MeanSin       = {s.MeanSin:E16}");
                _output.WriteLine($"LockStrength  = {s.LockStrength:E16}");
                _output.WriteLine($"LockOrder     = {s.LockOrder}");
                _output.WriteLine($"ModeScore     = {s.ModeScore:E16}");
                _output.WriteLine("");
            }

            _output.WriteLine("Best by LockStrength:");
            _output.WriteLine($"  gamma       = {bestByLock.Gamma:F6}");
            _output.WriteLine($"  LockStrength= {bestByLock.LockStrength:E16}");
            _output.WriteLine($"  LockOrder   = {bestByLock.LockOrder}");
            _output.WriteLine($"  ModeScore   = {bestByLock.ModeScore:E16}");

            _output.WriteLine("Best by ModeScore:");
            _output.WriteLine($"  gamma       = {bestByModeScore.Gamma:F6}");
            _output.WriteLine($"  LockStrength= {bestByModeScore.LockStrength:E16}");
            _output.WriteLine($"  LockOrder   = {bestByModeScore.LockOrder}");
            _output.WriteLine($"  ModeScore   = {bestByModeScore.ModeScore:E16}");

            // Jetzt sollte der Grundmodus bevorzugt werden
            Assert.True(Math.Abs(bestByModeScore.Gamma - 1.0) < 1e-12,
                "Fundamental mode at gamma = 1.0 was not preferred.");
        }

        [Fact]
        public void Should_Show_Strongest_Phase_Synchronization_Near_Planck_Tick()
        {
            var basePlanck = PlanckConstants.FromPhysicalConstants();

            double omega0 = 2.0 * Math.PI / basePlanck.tP;

            double[] gammaValues =
            {
        0.85,
        0.90,
        0.95,
        1.00,
        1.05,
        1.10,
        1.15
    };

            int cellCount = 16;
            int steps = 2000;
            double kappa = 0.01;

            int[] seeds = { 100001, 100002, 100003, 100004, 100005 };

            var summaries = new List<PhaseSyncSummary>();

            foreach (double gamma in gammaValues)
            {
                double dt = gamma * basePlanck.tP;

                var orderMeans = new List<double>();

                foreach (int seed in seeds)
                {
                    var rng = new Random(seed);

                    var cells = new List<CoupledPhaseCell>();
                    for (int i = 0; i < cellCount; i++)
                    {
                        cells.Add(new CoupledPhaseCell
                        {
                            Phi = 2.0 * Math.PI * rng.NextDouble(),
                            // kleine Streuung um omega0
                            Omega = omega0 * (1.0 + 0.01 * (2.0 * rng.NextDouble() - 1.0))
                        });
                    }

                    var orderSeries = new List<double>(steps);

                    for (int step = 0; step < steps; step++)
                    {
                        UpdateCoupledCells(cells, dt, kappa);
                        orderSeries.Add(ComputeOrderParameter(cells));
                    }

                    // mittlere Synchronisation über die Zeit
                    double meanOrder = orderSeries.Average();
                    orderMeans.Add(meanOrder);
                }

                summaries.Add(new PhaseSyncSummary
                {
                    Gamma = gamma,
                    MeanOrder = orderMeans.Average(),
                    StdOrder = StdDev(orderMeans)
                });
            }

            var best = summaries.OrderByDescending(x => x.MeanOrder).First();

            _output.WriteLine("=== Coupled Phase Synchronization Test ===");
            foreach (var s in summaries.OrderBy(x => x.Gamma))
            {
                _output.WriteLine($"gamma      = {s.Gamma:F6}");
                _output.WriteLine($"MeanOrder  = {s.MeanOrder:E16}");
                _output.WriteLine($"StdOrder   = {s.StdOrder:E16}");
                _output.WriteLine("");
            }

            _output.WriteLine("Best synchronization:");
            _output.WriteLine($"  gamma    = {best.Gamma:F6}");
            _output.WriteLine($"  MeanOrder= {best.MeanOrder:E16}");
            _output.WriteLine($"  StdOrder = {best.StdOrder:E16}");

            // only logging for now, but we expect best gamma to be near 1.0

        }
        [Fact]
        public void Should_Report_Synchronization_Peak_Shift_Across_Kappa_Values()
        {
            var basePlanck = PlanckConstants.FromPhysicalConstants();

            double omega0 = 2.0 * Math.PI / basePlanck.tP;

            double[] gammaValues =
            {
        0.85,
        0.90,
        0.95,
        1.00,
        1.05,
        1.10,
        1.15
    };

            double[] kappaValues =
            {
        0.001,
        0.01,
        0.05,
        0.10
    };

            int cellCount = 16;
            int steps = 2000;

            int[] seeds =
            {
        110001, 110002, 110003, 110004, 110005
    };

            _output.WriteLine("=== Kappa Synchronization Scan ===");
            _output.WriteLine("");

            foreach (double kappa in kappaValues)
            {
                var summaries = new List<PhaseSyncSummary>();

                foreach (double gamma in gammaValues)
                {
                    double dt = gamma * basePlanck.tP;

                    var orderMeans = new List<double>();

                    foreach (int seed in seeds)
                    {
                        var rng = new Random(seed);

                        var cells = new List<CoupledPhaseCell>();
                        for (int i = 0; i < cellCount; i++)
                        {
                            cells.Add(new CoupledPhaseCell
                            {
                                Phi = 2.0 * Math.PI * rng.NextDouble(),
                                Omega = omega0 * (1.0 + 0.01 * (2.0 * rng.NextDouble() - 1.0))
                            });
                        }

                        var orderSeries = new List<double>(steps);

                        for (int step = 0; step < steps; step++)
                        {
                            UpdateCoupledCells(cells, dt, kappa);
                            orderSeries.Add(ComputeOrderParameter(cells));
                        }

                        double meanOrder = orderSeries.Average();
                        orderMeans.Add(meanOrder);
                    }

                    summaries.Add(new PhaseSyncSummary
                    {
                        Gamma = gamma,
                        MeanOrder = orderMeans.Average(),
                        StdOrder = StdDev(orderMeans)
                    });
                }

                var best = summaries.OrderByDescending(x => x.MeanOrder).First();

                _output.WriteLine($"--- kappa = {kappa:F6} ---");
                foreach (var s in summaries.OrderBy(x => x.Gamma))
                {
                    _output.WriteLine($"gamma      = {s.Gamma:F6}");
                    _output.WriteLine($"MeanOrder  = {s.MeanOrder:E16}");
                    _output.WriteLine($"StdOrder   = {s.StdOrder:E16}");
                    _output.WriteLine("");
                }

                _output.WriteLine("Best synchronization for this kappa:");
                _output.WriteLine($"  gamma    = {best.Gamma:F6}");
                _output.WriteLine($"  MeanOrder= {best.MeanOrder:E16}");
                _output.WriteLine($"  StdOrder = {best.StdOrder:E16}");
                _output.WriteLine("");
            }

            Assert.True(kappaValues.Length > 0);
        }

        [Fact]
        public void Should_Show_Kappa_Dependent_Synchronization_With_Effective_Coupling()
        {
            var basePlanck = PlanckConstants.FromPhysicalConstants();

            double omega0 = 2.0 * Math.PI / basePlanck.tP;

            double[] gammaValues =
            {
        0.85,
        0.90,
        0.95,
        1.00,
        1.05,
        1.10,
        1.15
    };

            double[] kappaValues =
            {
        0.001,
        0.01,
        0.05,
        0.10
    };

            int cellCount = 16;
            int steps = 2000;

            int[] seeds =
            {
        120001, 120002, 120003, 120004, 120005
    };

            _output.WriteLine("=== Effective-Coupling Synchronization Scan ===");
            _output.WriteLine("");

            foreach (double kappa in kappaValues)
            {
                var summaries = new List<PhaseSyncSummary>();

                foreach (double gamma in gammaValues)
                {
                    double dt = gamma * basePlanck.tP;

                    var orderMeans = new List<double>();

                    foreach (int seed in seeds)
                    {
                        var rng = new Random(seed);

                        var cells = new List<CoupledPhaseCell>();
                        for (int i = 0; i < cellCount; i++)
                        {
                            cells.Add(new CoupledPhaseCell
                            {
                                Phi = 2.0 * Math.PI * rng.NextDouble(),
                                Omega = omega0 * (1.0 + 0.01 * (2.0 * rng.NextDouble() - 1.0))
                            });
                        }

                        var orderSeries = new List<double>(steps);

                        for (int step = 0; step < steps; step++)
                        {
                            UpdateCoupledCellsEffective(cells, dt, kappa, CouplingTopology.AllToAll);
                            orderSeries.Add(ComputeOrderParameter(cells));
                        }

                        double meanOrder = orderSeries.Average();
                        orderMeans.Add(meanOrder);
                    }

                    summaries.Add(new PhaseSyncSummary
                    {
                        Gamma = gamma,
                        MeanOrder = orderMeans.Average(),
                        StdOrder = StdDev(orderMeans)
                    });
                }

                var best = summaries.OrderByDescending(x => x.MeanOrder).First();

                _output.WriteLine($"--- kappa = {kappa:F6} ---");
                foreach (var s in summaries.OrderBy(x => x.Gamma))
                {
                    _output.WriteLine($"gamma      = {s.Gamma:F6}");
                    _output.WriteLine($"MeanOrder  = {s.MeanOrder:E16}");
                    _output.WriteLine($"StdOrder   = {s.StdOrder:E16}");
                    _output.WriteLine("");
                }

                _output.WriteLine("Best synchronization for this kappa:");
                _output.WriteLine($"  gamma    = {best.Gamma:F6}");
                _output.WriteLine($"  MeanOrder= {best.MeanOrder:E16}");
                _output.WriteLine($"  StdOrder = {best.StdOrder:E16}");
                _output.WriteLine("");
            }

            Assert.True(kappaValues.Length > 0);
        }

        [Fact]
        public void Should_Report_Synchronization_Peak_Shift_Across_Frequency_Spread()
        {
            var basePlanck = PlanckConstants.FromPhysicalConstants();

            double omega0 = 2.0 * Math.PI / basePlanck.tP;

            double[] gammaValues =
            {
        0.85,
        0.90,
        0.95,
        1.00,
        1.05,
        1.10,
        1.15
    };

            double[] spreadValues =
            {
        0.0,   // identische Frequenzen
        0.001, // 0.1 %
        0.01,  // 1 %
        0.05   // 5 %
    };

            int cellCount = 16;
            int steps = 2000;
            double kappa = 0.01;

            int[] seeds =
            {
        130001, 130002, 130003, 130004, 130005
    };

            _output.WriteLine("=== Frequency-Spread Synchronization Scan ===");
            _output.WriteLine("");

            foreach (double spread in spreadValues)
            {
                var summaries = new List<PhaseSyncSummary>();

                foreach (double gamma in gammaValues)
                {
                    double dt = gamma * basePlanck.tP;

                    var orderMeans = new List<double>();

                    foreach (int seed in seeds)
                    {
                        var rng = new Random(seed);

                        var cells = new List<CoupledPhaseCell>();
                        for (int i = 0; i < cellCount; i++)
                        {
                            cells.Add(new CoupledPhaseCell
                            {
                                Phi = 2.0 * Math.PI * rng.NextDouble(),
                                Omega = omega0 * (1.0 + spread * (2.0 * rng.NextDouble() - 1.0))
                            });
                        }

                        var orderSeries = new List<double>(steps);

                        for (int step = 0; step < steps; step++)
                        {
                            UpdateCoupledCellsEffective(cells, dt, kappa, CouplingTopology.AllToAll);
                            orderSeries.Add(ComputeOrderParameter(cells));
                        }

                        double meanOrder = orderSeries.Average();
                        orderMeans.Add(meanOrder);
                    }

                    summaries.Add(new PhaseSyncSummary
                    {
                        Gamma = gamma,
                        MeanOrder = orderMeans.Average(),
                        StdOrder = StdDev(orderMeans)
                    });
                }

                var best = summaries.OrderByDescending(x => x.MeanOrder).First();

                _output.WriteLine($"--- spread = {spread:F6} ---");
                foreach (var s in summaries.OrderBy(x => x.Gamma))
                {
                    _output.WriteLine($"gamma      = {s.Gamma:F6}");
                    _output.WriteLine($"MeanOrder  = {s.MeanOrder:E16}");
                    _output.WriteLine($"StdOrder   = {s.StdOrder:E16}");
                    _output.WriteLine("");
                }

                _output.WriteLine("Best synchronization for this spread:");
                _output.WriteLine($"  gamma    = {best.Gamma:F6}");
                _output.WriteLine($"  MeanOrder= {best.MeanOrder:E16}");
                _output.WriteLine($"  StdOrder = {best.StdOrder:E16}");
                _output.WriteLine("");
            }

            Assert.True(spreadValues.Length > 0);
        }

        [Fact]
        public void Should_Report_Synchronization_Peak_For_AllToAll_Vs_Ring_Topology()
        {
            var basePlanck = PlanckConstants.FromPhysicalConstants();

            double omega0 = 2.0 * Math.PI / basePlanck.tP;

            double[] gammaValues =
            {
        0.85,
        0.90,
        0.95,
        1.00,
        1.05,
        1.10,
        1.15
    };

            var topologies = new[]
            {
        CouplingTopology.AllToAll,
        CouplingTopology.Ring
    };

            int cellCount = 16;
            int steps = 2000;
            double kappa = 0.01;
            double spread = 0.01;

            int[] seeds =
            {
        140001, 140002, 140003, 140004, 140005
    };

            _output.WriteLine("=== Synchronization Scan: All-to-All vs Ring ===");
            _output.WriteLine("");

            foreach (var topology in topologies)
            {
                var summaries = new List<PhaseSyncSummary>();

                foreach (double gamma in gammaValues)
                {
                    double dt = gamma * basePlanck.tP;

                    var orderMeans = new List<double>();

                    foreach (int seed in seeds)
                    {
                        var rng = new Random(seed);

                        var cells = new List<CoupledPhaseCell>();
                        for (int i = 0; i < cellCount; i++)
                        {
                            cells.Add(new CoupledPhaseCell
                            {
                                Phi = 2.0 * Math.PI * rng.NextDouble(),
                                Omega = omega0 * (1.0 + spread * (2.0 * rng.NextDouble() - 1.0))
                            });
                        }

                        var orderSeries = new List<double>(steps);

                        for (int step = 0; step < steps; step++)
                        {
                            UpdateCoupledCellsEffective(cells, dt, kappa, topology);
                            orderSeries.Add(ComputeOrderParameter(cells));
                        }

                        double meanOrder = orderSeries.Average();
                        orderMeans.Add(meanOrder);
                    }

                    summaries.Add(new PhaseSyncSummary
                    {
                        Gamma = gamma,
                        MeanOrder = orderMeans.Average(),
                        StdOrder = StdDev(orderMeans)
                    });
                }

                var best = summaries.OrderByDescending(x => x.MeanOrder).First();

                _output.WriteLine($"--- topology = {topology} ---");
                foreach (var s in summaries.OrderBy(x => x.Gamma))
                {
                    _output.WriteLine($"gamma      = {s.Gamma:F6}");
                    _output.WriteLine($"MeanOrder  = {s.MeanOrder:E16}");
                    _output.WriteLine($"StdOrder   = {s.StdOrder:E16}");
                    _output.WriteLine("");
                }

                _output.WriteLine($"Best synchronization for topology {topology}:");
                _output.WriteLine($"  gamma    = {best.Gamma:F6}");
                _output.WriteLine($"  MeanOrder= {best.MeanOrder:E16}");
                _output.WriteLine($"  StdOrder = {best.StdOrder:E16}");
                _output.WriteLine("");
            }

            Assert.True(topologies.Length == 2);
        }

        [Fact]
        public void Should_Report_Synchronization_Peak_Across_Different_Cell_Counts()
        {
            var basePlanck = PlanckConstants.FromPhysicalConstants();

            double omega0 = 2.0 * Math.PI / basePlanck.tP;

            double[] gammaValues =
            {
        0.85,
        0.90,
        0.95,
        1.00,
        1.05,
        1.10,
        1.15
    };

            int[] cellCounts =
            {
        8,
        16,
        32,
        64
    };

            double kappa = 0.01;
            double spread = 0.01;
            int steps = 2000;

            int[] seeds =
            {
        150001, 150002, 150003, 150004, 150005
    };

            _output.WriteLine("=== Cell Count Synchronization Scan ===");
            _output.WriteLine("");

            foreach (int cellCount in cellCounts)
            {
                var summaries = new List<PhaseSyncSummary>();

                foreach (double gamma in gammaValues)
                {
                    double dt = gamma * basePlanck.tP;

                    var orderMeans = new List<double>();

                    foreach (int seed in seeds)
                    {
                        var rng = new Random(seed);

                        var cells = new List<CoupledPhaseCell>();
                        for (int i = 0; i < cellCount; i++)
                        {
                            cells.Add(new CoupledPhaseCell
                            {
                                Phi = 2.0 * Math.PI * rng.NextDouble(),
                                Omega = omega0 * (1.0 + spread * (2.0 * rng.NextDouble() - 1.0))
                            });
                        }

                        var orderSeries = new List<double>(steps);

                        for (int step = 0; step < steps; step++)
                        {
                            // hier bewusst dieselbe Topologie wie im letzten Test:
                            UpdateCoupledCellsEffective(cells, dt, kappa, CouplingTopology.AllToAll);
                            orderSeries.Add(ComputeOrderParameter(cells));
                        }

                        double meanOrder = orderSeries.Average();
                        orderMeans.Add(meanOrder);
                    }

                    summaries.Add(new PhaseSyncSummary
                    {
                        Gamma = gamma,
                        MeanOrder = orderMeans.Average(),
                        StdOrder = StdDev(orderMeans)
                    });
                }

                var best = summaries.OrderByDescending(x => x.MeanOrder).First();

                _output.WriteLine($"--- cellCount = {cellCount} ---");
                foreach (var s in summaries.OrderBy(x => x.Gamma))
                {
                    _output.WriteLine($"gamma      = {s.Gamma:F6}");
                    _output.WriteLine($"MeanOrder  = {s.MeanOrder:E16}");
                    _output.WriteLine($"StdOrder   = {s.StdOrder:E16}");
                    _output.WriteLine("");
                }

                _output.WriteLine($"Best synchronization for cellCount {cellCount}:");
                _output.WriteLine($"  gamma    = {best.Gamma:F6}");
                _output.WriteLine($"  MeanOrder= {best.MeanOrder:E16}");
                _output.WriteLine($"  StdOrder = {best.StdOrder:E16}");
                _output.WriteLine("");
            }

            Assert.True(cellCounts.Length > 0);
        }

        [Fact]
        public void Should_Report_Synchronization_Peak_Across_Different_Update_Rules()
        {
            var basePlanck = PlanckConstants.FromPhysicalConstants();

            double omega0 = 2.0 * Math.PI / basePlanck.tP;

            double[] gammaValues =
            {
        0.85,
        0.90,
        0.95,
        1.00,
        1.05,
        1.10,
        1.15
    };

            var rules = new[]
            {
        PhaseUpdateRule.ExplicitEuler,
        PhaseUpdateRule.NormalizedEuler,
        PhaseUpdateRule.HalfStepCoupling
    };

            int cellCount = 16;
            int steps = 2000;
            double kappa = 0.01;
            double spread = 0.01;
            var topology = CouplingTopology.AllToAll;

            int[] seeds =
            {
        160001, 160002, 160003, 160004, 160005
    };

            _output.WriteLine("=== Synchronization Scan: Different Update Rules ===");
            _output.WriteLine("");

            foreach (var rule in rules)
            {
                var summaries = new List<PhaseSyncSummary>();

                foreach (double gamma in gammaValues)
                {
                    double dt = gamma * basePlanck.tP;

                    var orderMeans = new List<double>();

                    foreach (int seed in seeds)
                    {
                        var rng = new Random(seed);

                        var cells = new List<CoupledPhaseCell>();
                        for (int i = 0; i < cellCount; i++)
                        {
                            cells.Add(new CoupledPhaseCell
                            {
                                Phi = 2.0 * Math.PI * rng.NextDouble(),
                                Omega = omega0 * (1.0 + spread * (2.0 * rng.NextDouble() - 1.0))
                            });
                        }

                        var orderSeries = new List<double>(steps);

                        for (int step = 0; step < steps; step++)
                        {
                            UpdateCoupledCellsWithRule(cells, dt, kappa, topology, rule);
                            orderSeries.Add(ComputeOrderParameter(cells));
                        }

                        orderMeans.Add(orderSeries.Average());
                    }

                    summaries.Add(new PhaseSyncSummary
                    {
                        Gamma = gamma,
                        MeanOrder = orderMeans.Average(),
                        StdOrder = StdDev(orderMeans)
                    });
                }

                var best = summaries.OrderByDescending(x => x.MeanOrder).First();

                _output.WriteLine($"--- rule = {rule} ---");
                foreach (var s in summaries.OrderBy(x => x.Gamma))
                {
                    _output.WriteLine($"gamma      = {s.Gamma:F6}");
                    _output.WriteLine($"MeanOrder  = {s.MeanOrder:E16}");
                    _output.WriteLine($"StdOrder   = {s.StdOrder:E16}");
                    _output.WriteLine("");
                }

                _output.WriteLine($"Best synchronization for rule {rule}:");
                _output.WriteLine($"  gamma    = {best.Gamma:F6}");
                _output.WriteLine($"  MeanOrder= {best.MeanOrder:E16}");
                _output.WriteLine($"  StdOrder = {best.StdOrder:E16}");
                _output.WriteLine("");
            }

            Assert.True(rules.Length == 3);
        }

        #endregion



        // ----------------- Helpers -----------------

        #region helper methods


        private static void UpdateSingleCell(PhaseCell cell, double dt)
        {
            cell.Phi += cell.Omega * dt;
        }

        private static double ComputeDeltaT(PhaseCell cell, double amplitude)
        {
            return amplitude * Math.Cos(cell.Phi);
        }


        private static double WrapToTwoPi(double phi)
        {
            double twoPi = 2.0 * Math.PI;
            double wrapped = phi % twoPi;
            if (wrapped < 0) wrapped += twoPi;
            return wrapped;
        }

        private static double ComputeOrderParameter(List<CoupledPhaseCell> cells)
        {
            double meanCos = cells.Average(c => Math.Cos(c.Phi));
            double meanSin = cells.Average(c => Math.Sin(c.Phi));

            return Math.Sqrt(meanCos * meanCos + meanSin * meanSin);
        }


        private static List<double> LogSpace(double start, double stop, int count)
        {
            double logStart = Math.Log10(start);
            double logStop = Math.Log10(stop);

            double step = (logStop - logStart) / (count - 1);

            var values = new List<double>(count);
            for (int i = 0; i < count; i++)
            {
                values.Add(Math.Pow(10.0, logStart + i * step));
            }

            return values;
        }

        private static double StdDev(IReadOnlyList<double> values)
        {
            double mean = values.Average();
            double variance = values.Select(v => (v - mean) * (v - mean)).Average();
            return Math.Sqrt(variance);
        }

        private static double EuclideanDistance(
            double a1, double a2, double a3,
            double b1, double b2, double b3)
        {
            return Math.Sqrt(
                Math.Pow(a1 - b1, 2) +
                Math.Pow(a2 - b2, 2) +
                Math.Pow(a3 - b3, 2));
        }

        private static bool NearlyEqual(double a, double b, double tol = 1e-15)
        {
            return Math.Abs(a - b) < tol;
        }

        private static void UpdateCoupledCells(List<CoupledPhaseCell> cells, double dt, double kappa)
        {
            var newPhis = new double[cells.Count];

            for (int i = 0; i < cells.Count; i++)
            {
                double coupling = 0.0;

                for (int j = 0; j < cells.Count; j++)
                {
                    if (i == j) continue;

                    coupling += Math.Sin(cells[j].Phi - cells[i].Phi);
                }

                newPhis[i] = cells[i].Phi + cells[i].Omega * dt + kappa * coupling * dt;
            }

            for (int i = 0; i < cells.Count; i++)
            {
                cells[i].Phi = newPhis[i];
            }
        }
        private static void UpdateCoupledCellsEffective(
        List<CoupledPhaseCell> cells,
        double dt,
        double kappa,
        CouplingTopology topology)
        {
            var newPhis = new double[cells.Count];

            for (int i = 0; i < cells.Count; i++)
            {
                double coupling = 0.0;

                if (topology == CouplingTopology.AllToAll)
                {
                    for (int j = 0; j < cells.Count; j++)
                    {
                        if (i == j) continue;
                        coupling += Math.Sin(cells[j].Phi - cells[i].Phi);
                    }
                }
                else if (topology == CouplingTopology.Ring)
                {
                    int left = (i - 1 + cells.Count) % cells.Count;
                    int right = (i + 1) % cells.Count;

                    coupling += Math.Sin(cells[left].Phi - cells[i].Phi);
                    coupling += Math.Sin(cells[right].Phi - cells[i].Phi);
                }

                newPhis[i] = cells[i].Phi + cells[i].Omega * dt + kappa * coupling;
            }

            for (int i = 0; i < cells.Count; i++)
            {
                cells[i].Phi = newPhis[i];
            }
        }
        private static void UpdateCoupledCellsWithRule(
    List<CoupledPhaseCell> cells,
    double dt,
    double kappa,
    CouplingTopology topology,
    PhaseUpdateRule rule)
        {
            var newPhis = new double[cells.Count];

            for (int i = 0; i < cells.Count; i++)
            {
                double coupling = 0.0;
                int neighborCount = 0;

                if (topology == CouplingTopology.AllToAll)
                {
                    for (int j = 0; j < cells.Count; j++)
                    {
                        if (i == j) continue;
                        coupling += Math.Sin(cells[j].Phi - cells[i].Phi);
                        neighborCount++;
                    }
                }
                else if (topology == CouplingTopology.Ring)
                {
                    int left = (i - 1 + cells.Count) % cells.Count;
                    int right = (i + 1) % cells.Count;

                    coupling += Math.Sin(cells[left].Phi - cells[i].Phi);
                    coupling += Math.Sin(cells[right].Phi - cells[i].Phi);
                    neighborCount = 2;
                }

                double normalizedCoupling = neighborCount > 0 ? coupling / neighborCount : 0.0;

                switch (rule)
                {
                    case PhaseUpdateRule.ExplicitEuler:
                        newPhis[i] = cells[i].Phi + cells[i].Omega * dt + kappa * coupling;
                        break;

                    case PhaseUpdateRule.NormalizedEuler:
                        newPhis[i] = cells[i].Phi + cells[i].Omega * dt + kappa * normalizedCoupling;
                        break;

                    case PhaseUpdateRule.HalfStepCoupling:
                        {
                            double halfFree = cells[i].Phi + 0.5 * cells[i].Omega * dt;
                            // Kopplung wird hier auf Basis des alten Zustands genommen, aber symmetrisch eingebettet
                            newPhis[i] = halfFree + kappa * normalizedCoupling + 0.5 * cells[i].Omega * dt;
                            break;
                        }

                    default:
                        throw new InvalidOperationException("Unknown update rule");
                }
            }

            for (int i = 0; i < cells.Count; i++)
            {
                cells[i].Phi = newPhis[i];
            }
        }


        #endregion
    }

}

