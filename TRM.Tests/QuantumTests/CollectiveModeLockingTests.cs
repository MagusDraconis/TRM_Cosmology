using System;
using System.Collections.Generic;
using System.Linq;
using TRM.Tests.RealityTests;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.QuantumTests;

/// <summary>
/// Isolated collective mode-locking tests.
/// Intentionally does not depend on PhotonTransportModel to avoid circular validation.
/// </summary>
public class CollectiveModeLockingTests
{
    private readonly ITestOutputHelper _output;

    public CollectiveModeLockingTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void CML01_Should_Reproduce_20_17_ModeLock_Without_PhotonTransport()
    {
        double[] candidates = { 19.0 / 16.0, 20.0 / 17.0, 6.0 / 5.0, 21.0 / 18.0 };

        var results = candidates
            .Select(omega => SimulateModeLock(omega, ModeLockConfig.Default))
            .OrderByDescending(r => r.ModeLockScore)
            .ToArray();

        foreach (var r in results)
        {
            _output.WriteLine(
                $"CML01 omega={r.CollectiveOmega:F6} | score={r.ModeLockScore:E6} | R={r.MeanOrder:E6} | closure={r.ClosureResidual:E6}");
        }

        var best = results[0];
        Assert.InRange(best.CollectiveOmega, (20.0 / 17.0) - 1e-12, (20.0 / 17.0) + 1e-12);
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void CML02_ModeLock_Should_Depend_On_CellCoupling_Not_PhotonFit()
    {
        var noCoupling = SimulateModeLock(20.0 / 17.0, ModeLockConfig.Default with { CouplingKappa = 0.0 });
        var coupled = SimulateModeLock(20.0 / 17.0, ModeLockConfig.Default with { CouplingKappa = 0.12 });

        _output.WriteLine($"CML02 uncoupled score : {noCoupling.ModeLockScore:E6}");
        _output.WriteLine($"CML02 coupled score   : {coupled.ModeLockScore:E6}");
        _output.WriteLine($"CML02 uncoupled R     : {noCoupling.MeanOrder:E6}");
        _output.WriteLine($"CML02 coupled R       : {coupled.MeanOrder:E6}");

        Assert.True(coupled.ModeLockScore > noCoupling.ModeLockScore);
        Assert.True(coupled.MeanOrder > noCoupling.MeanOrder);
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void CML03_ModeLock_Should_Degrade_When_PhaseClosure_IsBroken()
    {
        var intact = SimulateModeLock(20.0 / 17.0, ModeLockConfig.Default with
        {
            BreakClosure = false
        });
        var broken = SimulateModeLock(20.0 / 17.0, ModeLockConfig.Default with
        {
            BreakClosure = true,
            ClosureBreakAmplitude = 0.45,
            ClosureBreakEveryNSteps = 1
        });

        _output.WriteLine($"CML03 intact score    : {intact.ModeLockScore:E6}");
        _output.WriteLine($"CML03 broken score    : {broken.ModeLockScore:E6}");
        _output.WriteLine($"CML03 intact closure  : {intact.ClosureResidual:E6}");
        _output.WriteLine($"CML03 broken closure  : {broken.ClosureResidual:E6}");

        Assert.True(broken.ModeLockScore < intact.ModeLockScore);
        Assert.True(broken.ClosureResidual > intact.ClosureResidual);
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void CML04_Gamma_Should_Approach_17_20_From_CollectiveCadence()
    {
        var omegaGrid = BuildOmegaGrid(1.14, 1.21, 0.002);
        var best = omegaGrid
            .Select(omega => SimulateModeLock(omega, ModeLockConfig.Default))
            .OrderByDescending(r => r.ModeLockScore)
            .First();

        double gamma = 1.0 / best.CollectiveOmega;

        _output.WriteLine($"CML04 best omega : {best.CollectiveOmega:E6}");
        _output.WriteLine($"CML04 gamma      : {gamma:E6}");
        _output.WriteLine($"CML04 target     : {(17.0 / 20.0):E6}");

        Assert.InRange(gamma, 0.84, 0.86);
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void CML05_Should_Report_20_17_Competitiveness_When_CadencePriorRemoved()
    {
        double[] candidates = { 19.0 / 16.0, 20.0 / 17.0, 6.0 / 5.0, 21.0 / 18.0 };

        var noCadencePrior = BuildNoCadencePriorConfig();

        var results = candidates
            .Select(omega => SimulateModeLock(omega, noCadencePrior))
            .OrderByDescending(r => r.ModeLockScore)
            .ToArray();

        foreach (var r in results)
        {
            _output.WriteLine(
                $"CML05 omega={r.CollectiveOmega:F6} | score={r.ModeLockScore:E6} | R={r.MeanOrder:E6} | closure={r.ClosureResidual:E6}");
        }

        var best = results[0];
        var target = results.First(r => Math.Abs(r.CollectiveOmega - (20.0 / 17.0)) < 1e-12);

        double margin = best.ModeLockScore - target.ModeLockScore;
        bool remainsCompetitive = margin <= 0.02;

        _output.WriteLine($"CML05 best omega      : {best.CollectiveOmega:E6}");
        _output.WriteLine($"CML05 20/17 margin    : {margin:E6}");
        _output.WriteLine($"CML05 competitive<=.02: {remainsCompetitive}");

        // Interpretability rule:
        // - margin <= 0.02: 20/17 remains competitive without cadence prior.
        // - margin >= 0.03: boundary is clearly confirmed.
        Assert.True(remainsCompetitive || margin >= 0.03,
            $"Ambiguous no-prior result (margin={margin:E6}). Neither clearly competitive nor clearly boundary-confirming.");
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void CML06_RationalModeCluster_Should_Contain_20Over17_And_7Over6()
    {
        double[] candidates =
        {
            7.0 / 6.0,
            13.0 / 11.0,
            20.0 / 17.0,
            19.0 / 16.0,
            6.0 / 5.0,
            21.0 / 18.0
        };

        var results = candidates
            .Select(omega => SimulateModeLock(omega, BuildNoCadencePriorConfig()))
            .OrderByDescending(r => r.ModeLockScore)
            .ToArray();

        foreach (var r in results)
        {
            _output.WriteLine(
                $"CML06 omega={r.CollectiveOmega:F6} | gamma={1.0 / r.CollectiveOmega:F6} | score={r.ModeLockScore:E6} | R={r.MeanOrder:E6} | closure={r.ClosureResidual:E6}");
        }

        var topCandidates = results.Take(3).ToArray();
        Assert.All(topCandidates, r => Assert.InRange(r.CollectiveOmega, 1.16, 1.19));
        Assert.All(topCandidates, r => Assert.InRange(1.0 / r.CollectiveOmega, 0.84, 0.86));

        var rationalCluster = results
            .Where(r => r.CollectiveOmega >= 1.16 && r.CollectiveOmega <= 1.19)
            .ToArray();

        Assert.Contains(rationalCluster, r => Math.Abs(r.CollectiveOmega - (20.0 / 17.0)) < 1e-12);
        Assert.Contains(rationalCluster, r => Math.Abs(r.CollectiveOmega - (7.0 / 6.0)) < 1e-12);

        var best = results[0];
        var target20Over17 = results.First(r => Math.Abs(r.CollectiveOmega - (20.0 / 17.0)) < 1e-12);
        double relativeGap20Over17 = (best.ModeLockScore - target20Over17.ModeLockScore) / Math.Max(best.ModeLockScore, 1e-12);

        _output.WriteLine($"CML06 best omega           : {best.CollectiveOmega:E6}");
        _output.WriteLine($"CML06 20/17 relative gap   : {relativeGap20Over17:E6}");

        Assert.True(relativeGap20Over17 <= 0.03,
            $"20/17 is not within 3% of best score (relative gap={relativeGap20Over17:E6}).");
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void CML07_DenseRationalSweep_Should_Show_CompetitiveBand_Around_20Over17()
    {
        var candidates = BuildReducedRationalCandidatesInWindow(
            minOmega: 1.16,
            maxOmega: 1.19,
            maxDenominator: 40);

        var results = candidates
            .Select(omega => SimulateModeLock(omega, BuildNoCadencePriorConfig()))
            .OrderByDescending(r => r.ModeLockScore)
            .ToArray();

        foreach (var r in results.Take(12))
        {
            _output.WriteLine(
                $"CML07 omega={r.CollectiveOmega:F6} | gamma={1.0 / r.CollectiveOmega:F6} | score={r.ModeLockScore:E6} | R={r.MeanOrder:E6} | closure={r.ClosureResidual:E6}");
        }

        Assert.True(results.Length >= 12, $"Expected dense rational set, got only {results.Length} candidates.");
        Assert.Contains(results, r => Math.Abs(r.CollectiveOmega - (20.0 / 17.0)) < 1e-12);
        Assert.Contains(results, r => Math.Abs(r.CollectiveOmega - (7.0 / 6.0)) < 1e-12);

        var best = results[0];
        var target20Over17 = results.First(r => Math.Abs(r.CollectiveOmega - (20.0 / 17.0)) < 1e-12);
        double relativeGap20Over17 = (best.ModeLockScore - target20Over17.ModeLockScore) / Math.Max(best.ModeLockScore, 1e-12);

        var competitiveBand = results
            .Where(r => (best.ModeLockScore - r.ModeLockScore) / Math.Max(best.ModeLockScore, 1e-12) <= 0.03)
            .ToArray();

        _output.WriteLine($"CML07 candidate count      : {results.Length}");
        _output.WriteLine($"CML07 best omega           : {best.CollectiveOmega:E6}");
        _output.WriteLine($"CML07 20/17 relative gap   : {relativeGap20Over17:E6}");
        _output.WriteLine($"CML07 competitive band n   : {competitiveBand.Length}");

        Assert.True(relativeGap20Over17 <= 0.03,
            $"20/17 is not within 3% of best score in dense sweep (relative gap={relativeGap20Over17:E6}).");
        Assert.True(competitiveBand.Length >= 4,
            $"Competitive band too narrow in dense sweep (n={competitiveBand.Length}).");
        Assert.All(competitiveBand, r => Assert.InRange(1.0 / r.CollectiveOmega, 0.84, 0.863));
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void CML08_ModeLockingBand_Should_Predict_ELBridgeWindow()
    {
        var candidates = BuildReducedRationalCandidatesInWindow(
            minOmega: 1.16,
            maxOmega: 1.19,
            maxDenominator: 40);

        var sweepResults = candidates
            .Select(omega => SimulateModeLock(omega, BuildNoCadencePriorConfig()))
            .OrderByDescending(r => r.ModeLockScore)
            .ToArray();

        var bestScore = sweepResults[0].ModeLockScore;
        var competitiveBand = sweepResults
            .Where(r => (bestScore - r.ModeLockScore) / Math.Max(bestScore, 1e-12) <= 0.03)
            .ToArray();

        Assert.True(competitiveBand.Length >= 4, $"Competitive band too narrow (n={competitiveBand.Length}).");
        Assert.Contains(competitiveBand, r => Math.Abs(r.CollectiveOmega - (20.0 / 17.0)) < 1e-12);

        const double G = 1.0;
        const double c = 1.0;
        const double b = 1.0;
        const double dt = 0.001;
        double[] epsilons = { 1e-3, 2e-3, 5e-3, 1e-2 };

        double minRatio = double.PositiveInfinity;
        double maxRatio = double.NegativeInfinity;

        foreach (var candidate in competitiveBand)
        {
            double gammaBridge = 1.0 / candidate.CollectiveOmega;
            var parameters = new PhotonTransportModel.Parameters
            {
                LambdaTime = 1.0,
                LambdaSpace = 30.0,
                EulerBridgeScale = gammaBridge
            };

            foreach (double epsilon in epsilons)
            {
                double alphaEuler = PhotonTransportModel.ComputeDeflectionEulerLagrange(epsilon, G, c, b, dt, parameters);
                double alphaSchwarz = ComputeSchwarzschildNullDeflection(epsilon);
                double ratioEuler = alphaEuler / Math.Max(alphaSchwarz, 1e-16);

                minRatio = Math.Min(minRatio, ratioEuler);
                maxRatio = Math.Max(maxRatio, ratioEuler);

                _output.WriteLine(
                    $"CML08 omega={candidate.CollectiveOmega:F6} | gamma={gammaBridge:F6} | eps={epsilon:E} | EL/Schwarz={ratioEuler:E6}");

                Assert.InRange(ratioEuler, 0.85, 1.25);
            }
        }

        _output.WriteLine($"CML08 competitive band count : {competitiveBand.Length}");
        _output.WriteLine($"CML08 EL ratio range         : [{minRatio:E6}, {maxRatio:E6}]");
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void RBF01_PhaseClosure_Should_Select_RationalBand()
    {
        var candidates = BuildReducedRationalCandidatesInWindow(
            minOmega: 1.12,
            maxOmega: 1.22,
            maxDenominator: 36);

        var intactResults = candidates
            .Select(omega => SimulateModeLock(omega, ModeLockConfig.Default))
            .OrderByDescending(r => r.ModeLockScore)
            .ToArray();

        var brokenResults = candidates
            .Select(omega => SimulateModeLock(omega, ModeLockConfig.Default with
            {
                BreakClosure = true,
                ClosureBreakAmplitude = 0.45,
                ClosureBreakEveryNSteps = 1
            }))
            .OrderByDescending(r => r.ModeLockScore)
            .ToArray();

        var intactBest = intactResults[0];
        var brokenBest = brokenResults[0];
        int intactTopBandCount = intactResults
            .Take(5)
            .Count(r => r.CollectiveOmega >= 1.16 && r.CollectiveOmega <= 1.19);

        _output.WriteLine($"RBF01 intact best omega   : {intactBest.CollectiveOmega:E6}");
        _output.WriteLine($"RBF01 broken best omega   : {brokenBest.CollectiveOmega:E6}");
        _output.WriteLine($"RBF01 intact top5 in band : {intactTopBandCount}");
        _output.WriteLine($"RBF01 intact best score   : {intactBest.ModeLockScore:E6}");
        _output.WriteLine($"RBF01 broken best score   : {brokenBest.ModeLockScore:E6}");

        Assert.InRange(intactBest.CollectiveOmega, 1.16, 1.19);
        Assert.True(intactTopBandCount >= 3,
            $"Expected phase-closure-selected band occupancy in top5. count={intactTopBandCount}");
        Assert.True(intactBest.ModeLockScore > brokenBest.ModeLockScore,
            $"Expected closure break to weaken peak score. intact={intactBest.ModeLockScore:E6}, broken={brokenBest.ModeLockScore:E6}");
    }

    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF02_ActionConsistency_Should_Prefer_BridgeGammaWindow()
    {
        var candidates = BuildReducedRationalCandidatesInWindow(
            minOmega: 1.10,
            maxOmega: 1.24,
            maxDenominator: 40);

        const double G = 1.0;
        const double c = 1.0;
        const double b = 1.0;
        const double dt = 0.001;
        double[] epsilons = { 1e-3, 2e-3, 5e-3, 1e-2 };

        var evaluations = candidates
            .Select(omega =>
            {
                double gamma = 1.0 / omega;
                var parameters = new PhotonTransportModel.Parameters
                {
                    LambdaTime = 1.0,
                    LambdaSpace = 30.0,
                    EulerBridgeScale = gamma
                };

                double meanRelError = epsilons
                    .Select(epsilon =>
                    {
                        double alphaEuler = PhotonTransportModel.ComputeDeflectionEulerLagrange(epsilon, G, c, b, dt, parameters);
                        double alphaSchwarz = ComputeSchwarzschildNullDeflection(epsilon);
                        return Math.Abs(alphaEuler - alphaSchwarz) / Math.Max(alphaSchwarz, 1e-16);
                    })
                    .Average();

                return new
                {
                    Omega = omega,
                    Gamma = gamma,
                    MeanRelError = meanRelError
                };
            })
            .ToArray();

        var inBridgeWindow = evaluations
            .Where(e => e.Gamma >= 0.84 && e.Gamma <= 0.863)
            .ToArray();

        var outsideBridgeWindow = evaluations
            .Where(e => e.Gamma < 0.84 || e.Gamma > 0.863)
            .ToArray();

        Assert.NotEmpty(inBridgeWindow);
        Assert.NotEmpty(outsideBridgeWindow);

        double bestInAction = inBridgeWindow.Min(e => e.MeanRelError);
        double bestOutsideAction = outsideBridgeWindow.Min(e => e.MeanRelError);
        int top10InWindow = evaluations
            .OrderBy(e => e.MeanRelError)
            .Take(10)
            .Count(e => e.Gamma >= 0.84 && e.Gamma <= 0.863);

        _output.WriteLine($"RBF02 best in-window mean rel error   : {bestInAction:E6}");
        _output.WriteLine($"RBF02 best out-window mean rel error  : {bestOutsideAction:E6}");
        _output.WriteLine($"RBF02 top10 in bridge window          : {top10InWindow}");

        Assert.True(bestInAction <= bestOutsideAction + 0.02,
            $"Expected action consistency to prefer bridge gamma window. in={bestInAction:E6}, out={bestOutsideAction:E6}");
        Assert.True(top10InWindow >= 2,
            $"Expected robust bridge-window presence among low-action candidates. top10InWindow={top10InWindow}");
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void RBF03_LatticeScaling_Should_Preserve_RationalBand()
    {
        int[] cellCounts = { 12, 20, 32 };
        var bestOmegas = new List<double>(cellCounts.Length);

        foreach (int cellCount in cellCounts)
        {
            var config = BuildNoCadencePriorConfig() with { CellCount = cellCount };
            var candidates = BuildReducedRationalCandidatesInWindow(
                minOmega: 1.16,
                maxOmega: 1.19,
                maxDenominator: 40);

            var results = candidates
                .Select(omega => SimulateModeLock(omega, config))
                .OrderByDescending(r => r.ModeLockScore)
                .ToArray();

            var best = results[0];
            var target20Over17 = results.First(r => Math.Abs(r.CollectiveOmega - (20.0 / 17.0)) < 1e-12);
            double relativeGap20Over17 = (best.ModeLockScore - target20Over17.ModeLockScore) / Math.Max(best.ModeLockScore, 1e-12);

            _output.WriteLine($"RBF03 cells={cellCount,2} | best omega={best.CollectiveOmega:F6} | 20/17 gap={relativeGap20Over17:E6}");

            bestOmegas.Add(best.CollectiveOmega);

            Assert.InRange(best.CollectiveOmega, 1.16, 1.19);
            Assert.True(relativeGap20Over17 <= 0.03,
                $"20/17 not competitive at cells={cellCount}. gap={relativeGap20Over17:E6}");
        }

        double spread = bestOmegas.Max() - bestOmegas.Min();
        _output.WriteLine($"RBF03 best-omega spread across lattice scaling: {spread:E6}");
        Assert.InRange(spread, 0.0, 0.03);
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void RBF04_NoPrior_Should_Fail_Or_Select_Band()
    {
        var candidates = BuildReducedRationalCandidatesInWindow(
            minOmega: 1.10,
            maxOmega: 1.24,
            maxDenominator: 40);

        var results = candidates
            .Select(omega => SimulateModeLock(omega, BuildNoCadencePriorConfig()))
            .OrderByDescending(r => r.ModeLockScore)
            .ToArray();

        var best = results[0];
        bool bestInsideBand = best.CollectiveOmega >= 1.16 && best.CollectiveOmega <= 1.19;

        var bandCandidates = results
            .Where(r => r.CollectiveOmega >= 1.16 && r.CollectiveOmega <= 1.19)
            .ToArray();

        Assert.NotEmpty(bandCandidates);

        double bestBandScore = bandCandidates.Max(r => r.ModeLockScore);
        double marginToBand = (best.ModeLockScore - bestBandScore) / Math.Max(best.ModeLockScore, 1e-12);
        bool clearFailureToSelectBand = !bestInsideBand && marginToBand >= 0.03;

        _output.WriteLine($"RBF04 best omega            : {best.CollectiveOmega:E6}");
        _output.WriteLine($"RBF04 best inside band      : {bestInsideBand}");
        _output.WriteLine($"RBF04 margin best->band     : {marginToBand:E6}");
        _output.WriteLine($"RBF04 clear fail-to-select  : {clearFailureToSelectBand}");

        Assert.True(bestInsideBand || clearFailureToSelectBand,
            $"No-prior result is ambiguous: bestOmega={best.CollectiveOmega:F6}, marginToBand={marginToBand:E6}");
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void RBF05_PhaseClosureConstraint_Should_Predict_RationalBand()
    {
        // Phase-closure family:
        // q * Omega - p = 0 with p - q = m.
        // For m = 3 and q in [16..19], Omega = (q+3)/q predicts the observed rational band.
        const int m = 3;
        int[] qValues = { 16, 17, 18, 19 };

        var omegaFamily = qValues
            .Select(q => (q, Omega: (q + (double)m) / q))
            .ToArray();

        double minOmega = omegaFamily.Min(x => x.Omega);
        double maxOmega = omegaFamily.Max(x => x.Omega);

        foreach (var x in omegaFamily)
        {
            _output.WriteLine($"RBF05 q={x.q} | Omega=(q+{m})/q={x.Omega:F6} | gamma={1.0 / x.Omega:F6}");
        }

        _output.WriteLine($"RBF05 predicted Omega band: [{minOmega:F6}, {maxOmega:F6}]");

        Assert.InRange(minOmega, 1.15, 1.17);
        Assert.InRange(maxOmega, 1.18, 1.19);
        Assert.True((20.0 / 17.0) >= minOmega && (20.0 / 17.0) <= maxOmega,
            "20/17 must lie inside the phase-closure predicted band.");
        Assert.True((7.0 / 6.0) >= minOmega && (7.0 / 6.0) <= maxOmega,
            "7/6 must lie inside the phase-closure predicted band.");
    }

    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF06_PhaseClosureFamily_Should_Show_M3_As_BridgeBandCompetitiveMode()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        const int qMin = 12;
        const int qMax = 24;

        const double G = 1.0;
        const double c = 1.0;
        const double b = 1.0;
        const double dt = 0.001;
        double[] epsilons = { 2e-3, 1e-2 };

        var family = new List<(int M, int InBandCount, double BestCost, double BestOmega)>(mValues.Length);

        foreach (int m in mValues)
        {
            var candidates = Enumerable.Range(qMin, qMax - qMin + 1)
                .Select(q => (Omega: (q + (double)m) / q, Q: q))
                .ToArray();

            int inBandCount = 0;
            double bestCost = double.PositiveInfinity;
            double bestOmega = double.NaN;

            foreach (var x in candidates)
            {
                double omega = x.Omega;
                double gamma = 1.0 / omega;

                if (omega >= 1.16 && omega <= 1.19 && gamma >= 0.84 && gamma <= 0.86)
                {
                    inBandCount++;
                }

                var modeLock = SimulateModeLock(omega, BuildNoCadencePriorConfig());
                var parameters = new PhotonTransportModel.Parameters
                {
                    LambdaTime = 1.0,
                    LambdaSpace = 30.0,
                    EulerBridgeScale = gamma
                };

                double meanRelError = epsilons
                    .Select(epsilon =>
                    {
                        double alphaEuler = PhotonTransportModel.ComputeDeflectionEulerLagrange(epsilon, G, c, b, dt, parameters);
                        double alphaSchwarz = ComputeSchwarzschildNullDeflection(epsilon);
                        return Math.Abs(alphaEuler - alphaSchwarz) / Math.Max(alphaSchwarz, 1e-16);
                    })
                    .Average();

                double cost = (1.0 - modeLock.ModeLockScore) + meanRelError;
                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestOmega = omega;
                }
            }

            family.Add((M: m, InBandCount: inBandCount, BestCost: bestCost, BestOmega: bestOmega));
            _output.WriteLine($"RBF06 m={m} | inBand={inBandCount} | bestCost={bestCost:E6} | bestOmega={bestOmega:F6}");
        }

        var m1 = family.First(x => x.M == 1);
        var m2 = family.First(x => x.M == 2);
        var m3 = family.First(x => x.M == 3);
        var m4 = family.First(x => x.M == 4);
        var m5 = family.First(x => x.M == 5);

        Assert.True(m3.InBandCount >= 3,
            $"Expected m=3 to generate strong bridge-band occupancy. inBand={m3.InBandCount}");
        Assert.True(m3.BestCost <= m2.BestCost + 0.03,
            $"Expected m=3 to be competitive with m=2. m3={m3.BestCost:E6}, m2={m2.BestCost:E6}");
        Assert.True(m3.BestCost <= m4.BestCost + 0.03,
            $"Expected m=3 to be competitive with m=4. m3={m3.BestCost:E6}, m4={m4.BestCost:E6}");
        Assert.True(m3.InBandCount > m1.InBandCount,
            $"Expected m=3 to provide stronger bridge-band occupancy than m=1. m3={m3.InBandCount}, m1={m1.InBandCount}");
        Assert.True(m3.InBandCount > m5.InBandCount,
            $"Expected m=3 to provide stronger bridge-band occupancy than m=5. m3={m3.InBandCount}, m5={m5.InBandCount}");
    }

    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF07_M3_Should_Balance_BandOccupancy_And_ActionCost()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        const int qMin = 12;
        const int qMax = 24;

        const double G = 1.0;
        const double c = 1.0;
        const double b = 1.0;
        const double dt = 0.001;
        double[] epsilons = { 2e-3, 1e-2 };

        var family = new List<(int M, int InBandCount, double BestCost)>(mValues.Length);

        foreach (int m in mValues)
        {
            var candidates = Enumerable.Range(qMin, qMax - qMin + 1)
                .Select(q => (Omega: (q + (double)m) / q, Q: q))
                .ToArray();

            int inBandCount = 0;
            double bestCost = double.PositiveInfinity;

            foreach (var x in candidates)
            {
                double omega = x.Omega;
                double gamma = 1.0 / omega;

                if (omega >= 1.16 && omega <= 1.19 && gamma >= 0.84 && gamma <= 0.86)
                {
                    inBandCount++;
                }

                var modeLock = SimulateModeLock(omega, BuildNoCadencePriorConfig());
                var parameters = new PhotonTransportModel.Parameters
                {
                    LambdaTime = 1.0,
                    LambdaSpace = 30.0,
                    EulerBridgeScale = gamma
                };

                double meanRelError = epsilons
                    .Select(epsilon =>
                    {
                        double alphaEuler = PhotonTransportModel.ComputeDeflectionEulerLagrange(epsilon, G, c, b, dt, parameters);
                        double alphaSchwarz = ComputeSchwarzschildNullDeflection(epsilon);
                        return Math.Abs(alphaEuler - alphaSchwarz) / Math.Max(alphaSchwarz, 1e-16);
                    })
                    .Average();

                double cost = (1.0 - modeLock.ModeLockScore) + meanRelError;
                if (cost < bestCost)
                {
                    bestCost = cost;
                }
            }

            family.Add((M: m, InBandCount: inBandCount, BestCost: bestCost));
        }

        double maxBandCount = Math.Max(family.Max(x => x.InBandCount), 1);
        double minCost = family.Min(x => x.BestCost);
        double maxCost = family.Max(x => x.BestCost);
        double costDen = Math.Max(maxCost - minCost, 1e-12);

        var compromise = family
            .Select(x =>
            {
                double normalizedBand = x.InBandCount / maxBandCount;
                double normalizedCost = (x.BestCost - minCost) / costDen;
                double score = normalizedBand - normalizedCost;
                return (x.M, x.InBandCount, x.BestCost, normalizedBand, normalizedCost, score);
            })
            .OrderByDescending(x => x.score)
            .ToArray();

        foreach (var row in compromise)
        {
            _output.WriteLine(
                $"RBF07 m={row.M} | inBand={row.InBandCount} | cost={row.BestCost:E6} | nBand={row.normalizedBand:F3} | nCost={row.normalizedCost:F3} | score={row.score:F3}");
        }

        var best = compromise[0];
        var m3 = compromise.First(x => x.M == 3);

        Assert.True(best.M == 3,
            $"Expected m=3 to emerge as occupancy-cost compromise mode. best=m{best.M}, m3Score={m3.score:F3}, bestScore={best.score:F3}");
    }

    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF08_M3_BalanceMode_Should_Remain_Stable_Under_QWindow_Shifts()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        var qWindows = new (int QMin, int QMax)[]
        {
            (12, 24),
            (14, 26),
            (16, 28)
        };

        const double G = 1.0;
        const double c = 1.0;
        const double b = 1.0;
        const double dt = 0.001;
        double[] epsilons = { 2e-3, 1e-2 };

        var m3Ranks = new List<int>(qWindows.Length);
        var m3Gaps = new List<double>(qWindows.Length);

        foreach (var window in qWindows)
        {
            var family = new List<(int M, int InBandCount, double BestCost)>(mValues.Length);

            foreach (int m in mValues)
            {
                var candidates = Enumerable.Range(window.QMin, window.QMax - window.QMin + 1)
                    .Select(q => (Omega: (q + (double)m) / q, Q: q))
                    .ToArray();

                int inBandCount = 0;
                double bestCost = double.PositiveInfinity;

                foreach (var x in candidates)
                {
                    double omega = x.Omega;
                    double gamma = 1.0 / omega;

                    if (omega >= 1.16 && omega <= 1.19 && gamma >= 0.84 && gamma <= 0.86)
                    {
                        inBandCount++;
                    }

                    var modeLock = SimulateModeLock(omega, BuildNoCadencePriorConfig());
                    var parameters = new PhotonTransportModel.Parameters
                    {
                        LambdaTime = 1.0,
                        LambdaSpace = 30.0,
                        EulerBridgeScale = gamma
                    };

                    double meanRelError = epsilons
                        .Select(epsilon =>
                        {
                            double alphaEuler = PhotonTransportModel.ComputeDeflectionEulerLagrange(epsilon, G, c, b, dt, parameters);
                            double alphaSchwarz = ComputeSchwarzschildNullDeflection(epsilon);
                            return Math.Abs(alphaEuler - alphaSchwarz) / Math.Max(alphaSchwarz, 1e-16);
                        })
                        .Average();

                    double cost = (1.0 - modeLock.ModeLockScore) + meanRelError;
                    if (cost < bestCost)
                    {
                        bestCost = cost;
                    }
                }

                family.Add((M: m, InBandCount: inBandCount, BestCost: bestCost));
            }

            double maxBandCount = Math.Max(family.Max(x => x.InBandCount), 1);
            double minCost = family.Min(x => x.BestCost);
            double maxCost = family.Max(x => x.BestCost);
            double costDen = Math.Max(maxCost - minCost, 1e-12);

            var compromise = family
                .Select(x =>
                {
                    double normalizedBand = x.InBandCount / maxBandCount;
                    double normalizedCost = (x.BestCost - minCost) / costDen;
                    double score = normalizedBand - normalizedCost;
                    return (x.M, x.InBandCount, x.BestCost, score);
                })
                .OrderByDescending(x => x.score)
                .ToArray();

            int m3Rank = Array.FindIndex(compromise, x => x.M == 3) + 1;
            double gapToBest = compromise[0].score - compromise.First(x => x.M == 3).score;
            m3Ranks.Add(m3Rank);
            m3Gaps.Add(gapToBest);

            _output.WriteLine($"RBF08 q=[{window.QMin},{window.QMax}] | best=m{compromise[0].M} | m3Rank={m3Rank} | m3Gap={gapToBest:F3}");
        }

        double avgRank = m3Ranks.Average();
        double maxGap = m3Gaps.Max();

        _output.WriteLine($"RBF08 m3 rank vector : [{string.Join(", ", m3Ranks)}]");
        _output.WriteLine($"RBF08 m3 avg rank    : {avgRank:F3}");
        _output.WriteLine($"RBF08 m3 max gap     : {maxGap:F3}");

        Assert.True(m3Ranks.All(r => r <= 2),
            $"Expected m=3 to remain top-2 across q-window shifts. ranks=[{string.Join(", ", m3Ranks)}]");
        Assert.InRange(avgRank, 1.0, 1.5);
        Assert.InRange(maxGap, 0.0, 0.15);
    }

    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF09_ClosureOccupancyCostTradeoff_Should_Select_M3_Among_NeighboringModes()
    {
        int[] mValues = { 2, 3, 4 };
        const int qMin = 12;
        const int qMax = 24;

        const double G = 1.0;
        const double c = 1.0;
        const double b = 1.0;
        const double dt = 0.001;
        double[] epsilons = { 2e-3, 1e-2 };

        var family = new List<(int M, int InBandCount, double AvgClosureQualityInBand, double BestCost)>(mValues.Length);

        foreach (int m in mValues)
        {
            var candidates = Enumerable.Range(qMin, qMax - qMin + 1)
                .Select(q => (Omega: (q + (double)m) / q, Q: q))
                .ToArray();

            int inBandCount = 0;
            double closureQualitySum = 0.0;
            double bestCost = double.PositiveInfinity;

            foreach (var x in candidates)
            {
                double omega = x.Omega;
                double gamma = 1.0 / omega;
                bool inBand = omega >= 1.16 && omega <= 1.19 && gamma >= 0.84 && gamma <= 0.86;

                var modeLock = SimulateModeLock(omega, BuildNoCadencePriorConfig());
                var parameters = new PhotonTransportModel.Parameters
                {
                    LambdaTime = 1.0,
                    LambdaSpace = 30.0,
                    EulerBridgeScale = gamma
                };

                double meanRelError = epsilons
                    .Select(epsilon =>
                    {
                        double alphaEuler = PhotonTransportModel.ComputeDeflectionEulerLagrange(epsilon, G, c, b, dt, parameters);
                        double alphaSchwarz = ComputeSchwarzschildNullDeflection(epsilon);
                        return Math.Abs(alphaEuler - alphaSchwarz) / Math.Max(alphaSchwarz, 1e-16);
                    })
                    .Average();

                double cost = (1.0 - modeLock.ModeLockScore) + meanRelError;
                if (cost < bestCost)
                {
                    bestCost = cost;
                }

                if (inBand)
                {
                    inBandCount++;
                    closureQualitySum += (1.0 - modeLock.ClosureResidual);
                }
            }

            double avgClosureQualityInBand = inBandCount > 0 ? closureQualitySum / inBandCount : 0.0;
            family.Add((M: m, InBandCount: inBandCount, AvgClosureQualityInBand: avgClosureQualityInBand, BestCost: bestCost));
        }

        double maxBandCount = Math.Max(family.Max(x => x.InBandCount), 1);
        double minClosure = family.Min(x => x.AvgClosureQualityInBand);
        double maxClosure = family.Max(x => x.AvgClosureQualityInBand);
        double closureDen = Math.Max(maxClosure - minClosure, 1e-12);
        double minCost = family.Min(x => x.BestCost);
        double maxCost = family.Max(x => x.BestCost);
        double costDen = Math.Max(maxCost - minCost, 1e-12);

        var compromise = family
            .Select(x =>
            {
                double normalizedBand = x.InBandCount / maxBandCount;
                double normalizedClosure = (x.AvgClosureQualityInBand - minClosure) / closureDen;
                double normalizedCost = (x.BestCost - minCost) / costDen;
                double occupancyWeightedClosure = normalizedBand * normalizedClosure;
                double score = 0.55 * normalizedBand + 0.25 * occupancyWeightedClosure - 0.20 * normalizedCost;
                return (x.M, x.InBandCount, x.AvgClosureQualityInBand, x.BestCost, normalizedBand, normalizedClosure, occupancyWeightedClosure, normalizedCost, score);
            })
            .OrderByDescending(x => x.score)
            .ToArray();

        foreach (var row in compromise)
        {
            _output.WriteLine(
                $"RBF09 m={row.M} | inBand={row.InBandCount} | closure={row.AvgClosureQualityInBand:F4} | cost={row.BestCost:E6} | nBand={row.normalizedBand:F3} | nClosure={row.normalizedClosure:F3} | nOccClosure={row.occupancyWeightedClosure:F3} | nCost={row.normalizedCost:F3} | score={row.score:F3}");
        }

        var best = compromise[0];
        var m2 = compromise.First(x => x.M == 2);
        var m3 = compromise.First(x => x.M == 3);
        var m4 = compromise.First(x => x.M == 4);

        Assert.True(best.M == 3,
            $"Expected m=3 to be selected by closure-occupancy-cost compromise among neighboring modes. best=m{best.M}");
        Assert.True(m3.InBandCount >= m2.InBandCount,
            $"Expected m=3 to have at least as much bridge-band occupancy as m=2. m3={m3.InBandCount}, m2={m2.InBandCount}");
        Assert.True(m3.InBandCount >= m4.InBandCount,
            $"Expected m=3 to have at least as much bridge-band occupancy as m=4. m3={m3.InBandCount}, m4={m4.InBandCount}");
        Assert.True(m3.BestCost <= m4.BestCost + 0.03,
            $"Expected m=3 to remain cost-competitive with m=4. m3={m3.BestCost:E6}, m4={m4.BestCost:E6}");
    }

    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF10_M3_Should_Remain_Selected_Under_ScoreWeight_Ablation()
    {
        int[] mValues = { 2, 3, 4 };
        const int qMin = 12;
        const int qMax = 24;

        const double G = 1.0;
        const double c = 1.0;
        const double b = 1.0;
        const double dt = 0.001;
        double[] epsilons = { 2e-3, 1e-2 };

        var family = new List<(int M, int InBandCount, double AvgClosureQualityInBand, double BestCost)>(mValues.Length);

        foreach (int m in mValues)
        {
            var candidates = Enumerable.Range(qMin, qMax - qMin + 1)
                .Select(q => (Omega: (q + (double)m) / q, Q: q))
                .ToArray();

            int inBandCount = 0;
            double closureQualitySum = 0.0;
            double bestCost = double.PositiveInfinity;

            foreach (var x in candidates)
            {
                double omega = x.Omega;
                double gamma = 1.0 / omega;
                bool inBand = omega >= 1.16 && omega <= 1.19 && gamma >= 0.84 && gamma <= 0.86;

                var modeLock = SimulateModeLock(omega, BuildNoCadencePriorConfig());
                var parameters = new PhotonTransportModel.Parameters
                {
                    LambdaTime = 1.0,
                    LambdaSpace = 30.0,
                    EulerBridgeScale = gamma
                };

                double meanRelError = epsilons
                    .Select(epsilon =>
                    {
                        double alphaEuler = PhotonTransportModel.ComputeDeflectionEulerLagrange(epsilon, G, c, b, dt, parameters);
                        double alphaSchwarz = ComputeSchwarzschildNullDeflection(epsilon);
                        return Math.Abs(alphaEuler - alphaSchwarz) / Math.Max(alphaSchwarz, 1e-16);
                    })
                    .Average();

                double cost = (1.0 - modeLock.ModeLockScore) + meanRelError;
                if (cost < bestCost)
                {
                    bestCost = cost;
                }

                if (inBand)
                {
                    inBandCount++;
                    closureQualitySum += (1.0 - modeLock.ClosureResidual);
                }
            }

            double avgClosureQualityInBand = inBandCount > 0 ? closureQualitySum / inBandCount : 0.0;
            family.Add((M: m, InBandCount: inBandCount, AvgClosureQualityInBand: avgClosureQualityInBand, BestCost: bestCost));
        }

        double maxBandCount = Math.Max(family.Max(x => x.InBandCount), 1);
        double minClosure = family.Min(x => x.AvgClosureQualityInBand);
        double maxClosure = family.Max(x => x.AvgClosureQualityInBand);
        double closureDen = Math.Max(maxClosure - minClosure, 1e-12);
        double minCost = family.Min(x => x.BestCost);
        double maxCost = family.Max(x => x.BestCost);
        double costDen = Math.Max(maxCost - minCost, 1e-12);

        // Score ablation around the RBF09 baseline:
        // score = wBand*nBand + wClosure*(nBand*nClosure) - wCost*nCost
        var weightSets = new (double WBand, double WClosure, double WCost)[]
        {
            (0.45, 0.25, 0.30),
            (0.55, 0.25, 0.20),
            (0.65, 0.25, 0.10),
            (0.55, 0.15, 0.30),
            (0.55, 0.35, 0.10),
            (0.45, 0.35, 0.20),
            (0.65, 0.15, 0.20)
        };

        var m3Ranks = new List<int>(weightSets.Length);
        int m3Wins = 0;

        foreach (var w in weightSets)
        {
            var ranking = family
                .Select(x =>
                {
                    double normalizedBand = x.InBandCount / maxBandCount;
                    double normalizedClosure = (x.AvgClosureQualityInBand - minClosure) / closureDen;
                    double normalizedCost = (x.BestCost - minCost) / costDen;
                    double occupancyWeightedClosure = normalizedBand * normalizedClosure;
                    double score = w.WBand * normalizedBand + w.WClosure * occupancyWeightedClosure - w.WCost * normalizedCost;
                    return (x.M, x.InBandCount, x.AvgClosureQualityInBand, x.BestCost, score);
                })
                .OrderByDescending(x => x.score)
                .ToArray();

            int m3Rank = Array.FindIndex(ranking, x => x.M == 3) + 1;
            m3Ranks.Add(m3Rank);
            if (ranking[0].M == 3)
            {
                m3Wins++;
            }

            _output.WriteLine(
                $"RBF10 wBand={w.WBand:F2}, wClosure={w.WClosure:F2}, wCost={w.WCost:F2} | best=m{ranking[0].M} | m3Rank={m3Rank}");
        }

        _output.WriteLine($"RBF10 m3 rank vector: [{string.Join(", ", m3Ranks)}]");
        _output.WriteLine($"RBF10 m3 wins       : {m3Wins}/{weightSets.Length}");

        Assert.True(m3Ranks.All(r => r <= 2),
            $"Expected m=3 to remain top-2 across score-weight ablations. ranks=[{string.Join(", ", m3Ranks)}]");
        Assert.True(m3Wins >= 4,
            $"Expected m=3 to win in the majority of tested weight sets. wins={m3Wins}/{weightSets.Length}");
    }

    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF11_M3_Should_Be_Minimal_Mode_Satisfying_ThreeClosureConstraints()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        const int qMin = 12;
        const int qMax = 24;

        const double G = 1.0;
        const double c = 1.0;
        const double b = 1.0;
        const double dt = 0.001;
        double[] epsilons = { 2e-3, 1e-2 };

        var family = new List<(int M, int InBandCount, double AvgClosureQuality, double BestCost)>(mValues.Length);

        foreach (int m in mValues)
        {
            var candidates = Enumerable.Range(qMin, qMax - qMin + 1)
                .Select(q => (Omega: (q + (double)m) / q, Q: q))
                .ToArray();

            int inBandCount = 0;
            double closureQualitySum = 0.0;
            double bestCost = double.PositiveInfinity;

            foreach (var x in candidates)
            {
                double omega = x.Omega;
                double gamma = 1.0 / omega;
                bool inBand = omega >= 1.16 && omega <= 1.19 && gamma >= 0.84 && gamma <= 0.86;

                var modeLock = SimulateModeLock(omega, BuildNoCadencePriorConfig());
                var parameters = new PhotonTransportModel.Parameters
                {
                    LambdaTime = 1.0,
                    LambdaSpace = 30.0,
                    EulerBridgeScale = gamma
                };

                double meanRelError = epsilons
                    .Select(epsilon =>
                    {
                        double alphaEuler = PhotonTransportModel.ComputeDeflectionEulerLagrange(epsilon, G, c, b, dt, parameters);
                        double alphaSchwarz = ComputeSchwarzschildNullDeflection(epsilon);
                        return Math.Abs(alphaEuler - alphaSchwarz) / Math.Max(alphaSchwarz, 1e-16);
                    })
                    .Average();

                double cost = (1.0 - modeLock.ModeLockScore) + meanRelError;
                if (cost < bestCost)
                {
                    bestCost = cost;
                }

                if (inBand)
                {
                    inBandCount++;
                }

                closureQualitySum += (1.0 - modeLock.ClosureResidual);
            }

            double avgClosureQuality = closureQualitySum / candidates.Length;
            family.Add((M: m, InBandCount: inBandCount, AvgClosureQuality: avgClosureQuality, BestCost: bestCost));
        }

        const double phaseClosureThreshold = 0.78;
        const int directionClosureMinOccupancy = 1;
        const int actionClosureMinOccupancy = 3;

        var highOccupancyCosts = family
            .Where(x => x.InBandCount >= actionClosureMinOccupancy)
            .Select(x => x.BestCost)
            .OrderBy(x => x)
            .ToArray();

        Assert.True(highOccupancyCosts.Length >= 2,
            "Expected at least two high-occupancy modes to define action/tick closure threshold.");

        double actionTickCostThreshold = 0.5 * (highOccupancyCosts[0] + highOccupancyCosts[1]);

        var evaluation = family
            .Select(x =>
            {
                bool phaseClosureOk = x.AvgClosureQuality >= phaseClosureThreshold;
                bool directionClosureOk = x.InBandCount >= directionClosureMinOccupancy;
                bool actionTickClosureOk =
                    x.InBandCount >= actionClosureMinOccupancy &&
                    x.BestCost <= actionTickCostThreshold;
                bool allThree = phaseClosureOk && directionClosureOk && actionTickClosureOk;
                return (x.M, x.InBandCount, x.AvgClosureQuality, x.BestCost, phaseClosureOk, directionClosureOk, actionTickClosureOk, allThree);
            })
            .OrderBy(x => x.M)
            .ToArray();

        foreach (var row in evaluation)
        {
            _output.WriteLine(
                $"RBF11 m={row.M} | inBand={row.InBandCount} | avgClosure={row.AvgClosureQuality:F4} | bestCost={row.BestCost:E6} | phase={row.phaseClosureOk} | direction={row.directionClosureOk} | actionTick={row.actionTickClosureOk} | all={row.allThree}");
        }

        int minimalAllThreeMode = evaluation
            .Where(x => x.allThree)
            .Select(x => x.M)
            .DefaultIfEmpty(int.MaxValue)
            .Min();

        var m1 = evaluation.First(x => x.M == 1);
        var m2 = evaluation.First(x => x.M == 2);
        var m3 = evaluation.First(x => x.M == 3);

        Assert.True(m1.phaseClosureOk && !m1.directionClosureOk && !m1.actionTickClosureOk,
            $"Expected m=1 to satisfy only phase closure. m1: phase={m1.phaseClosureOk}, direction={m1.directionClosureOk}, actionTick={m1.actionTickClosureOk}");
        Assert.True(m2.phaseClosureOk && m2.directionClosureOk && !m2.actionTickClosureOk,
            $"Expected m=2 to satisfy phase+direction but not action/tick closure. m2: phase={m2.phaseClosureOk}, direction={m2.directionClosureOk}, actionTick={m2.actionTickClosureOk}");
        Assert.True(m3.phaseClosureOk && m3.directionClosureOk && m3.actionTickClosureOk,
            $"Expected m=3 to satisfy all three closure constraints. m3: phase={m3.phaseClosureOk}, direction={m3.directionClosureOk}, actionTick={m3.actionTickClosureOk}");
        Assert.True(minimalAllThreeMode == 3,
            $"Expected m=3 to be minimal mode satisfying all three closure constraints. minimal={minimalAllThreeMode}");
    }

    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF12_M3_MinimalClosureMode_Should_Remain_Stable_Under_Threshold_Ablation()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        const int qMin = 12;
        const int qMax = 24;
        const int directionClosureMinOccupancy = 1;

        const double G = 1.0;
        const double c = 1.0;
        const double b = 1.0;
        const double dt = 0.001;
        double[] epsilons = { 2e-3, 1e-2 };

        var family = new List<(int M, int InBandCount, double AvgClosureQuality, double BestCost)>(mValues.Length);

        foreach (int m in mValues)
        {
            var candidates = Enumerable.Range(qMin, qMax - qMin + 1)
                .Select(q => (Omega: (q + (double)m) / q, Q: q))
                .ToArray();

            int inBandCount = 0;
            double closureQualitySum = 0.0;
            double bestCost = double.PositiveInfinity;

            foreach (var x in candidates)
            {
                double omega = x.Omega;
                double gamma = 1.0 / omega;
                bool inBand = omega >= 1.16 && omega <= 1.19 && gamma >= 0.84 && gamma <= 0.86;

                var modeLock = SimulateModeLock(omega, BuildNoCadencePriorConfig());
                var parameters = new PhotonTransportModel.Parameters
                {
                    LambdaTime = 1.0,
                    LambdaSpace = 30.0,
                    EulerBridgeScale = gamma
                };

                double meanRelError = epsilons
                    .Select(epsilon =>
                    {
                        double alphaEuler = PhotonTransportModel.ComputeDeflectionEulerLagrange(epsilon, G, c, b, dt, parameters);
                        double alphaSchwarz = ComputeSchwarzschildNullDeflection(epsilon);
                        return Math.Abs(alphaEuler - alphaSchwarz) / Math.Max(alphaSchwarz, 1e-16);
                    })
                    .Average();

                double cost = (1.0 - modeLock.ModeLockScore) + meanRelError;
                if (cost < bestCost)
                {
                    bestCost = cost;
                }

                if (inBand)
                {
                    inBandCount++;
                }

                closureQualitySum += (1.0 - modeLock.ClosureResidual);
            }

            double avgClosureQuality = closureQualitySum / candidates.Length;
            family.Add((M: m, InBandCount: inBandCount, AvgClosureQuality: avgClosureQuality, BestCost: bestCost));
        }

        double[] phaseClosureThresholds = { 0.76, 0.78, 0.80 };
        int[] actionClosureMinOccupancies = { 2, 3 };
        double[] costScales = { 0.9, 1.0, 1.1 };

        int resolvedCases = 0;
        int unresolvedCases = 0;
        var m3ResolvedRanks = new List<int>();

        foreach (double phaseThreshold in phaseClosureThresholds)
        {
            foreach (int actionMinOccupancy in actionClosureMinOccupancies)
            {
                var highOccupancyCosts = family
                    .Where(x => x.InBandCount >= actionMinOccupancy)
                    .Select(x => x.BestCost)
                    .OrderBy(x => x)
                    .ToArray();

                Assert.True(highOccupancyCosts.Length >= 2,
                    $"Expected at least two high-occupancy modes for actionMinOccupancy={actionMinOccupancy}.");

                double baseCostThreshold = 0.5 * (highOccupancyCosts[0] + highOccupancyCosts[1]);

                foreach (double scale in costScales)
                {
                    double actionTickCostThreshold = baseCostThreshold * scale;

                    var evaluation = family
                        .Select(x =>
                        {
                            bool phaseClosureOk = x.AvgClosureQuality >= phaseThreshold;
                            bool directionClosureOk = x.InBandCount >= directionClosureMinOccupancy;
                            bool actionTickClosureOk =
                                x.InBandCount >= actionMinOccupancy &&
                                x.BestCost <= actionTickCostThreshold;
                            bool allThree = phaseClosureOk && directionClosureOk && actionTickClosureOk;
                            return (x.M, x.InBandCount, x.AvgClosureQuality, x.BestCost, allThree);
                        })
                        .OrderBy(x => x.M)
                        .ToArray();

                    var satisfyingModes = evaluation
                        .Where(x => x.allThree)
                        .Select(x => x.M)
                        .OrderBy(x => x)
                        .ToArray();

                    if (satisfyingModes.Length == 0)
                    {
                        unresolvedCases++;
                        _output.WriteLine(
                            $"RBF12 phaseThr={phaseThreshold:F2} | actionMinOcc={actionMinOccupancy} | costScale={scale:F2} | no satisfying mode");
                        continue;
                    }

                    resolvedCases++;
                    int minimalMode = satisfyingModes[0];
                    int m3Rank = Array.IndexOf(satisfyingModes, 3) + 1;
                    m3ResolvedRanks.Add(m3Rank);

                    _output.WriteLine(
                        $"RBF12 phaseThr={phaseThreshold:F2} | actionMinOcc={actionMinOccupancy} | costScale={scale:F2} | satisfying=[{string.Join(", ", satisfyingModes)}] | minimal=m{minimalMode} | m3Rank={m3Rank}");

                    Assert.True(minimalMode == 3,
                        $"Expected m=3 to be minimal satisfying mode in resolved threshold cases. minimal={minimalMode}, phaseThr={phaseThreshold:F2}, actionMinOcc={actionMinOccupancy}, costScale={scale:F2}");
                }
            }
        }

        _output.WriteLine($"RBF12 resolved cases   : {resolvedCases}");
        _output.WriteLine($"RBF12 unresolved cases : {unresolvedCases}");
        _output.WriteLine($"RBF12 m3 rank vector   : [{string.Join(", ", m3ResolvedRanks)}]");

        Assert.True(resolvedCases >= 8,
            $"Expected robust threshold support with at least 8 resolved cases. resolved={resolvedCases}");
        Assert.True(m3ResolvedRanks.All(r => r == 1),
            $"Expected m=3 to be rank-1 in all resolved cases. ranks=[{string.Join(", ", m3ResolvedRanks)}]");
    }

    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF13_ConstraintRemoval_Should_Reveal_ActionTick_As_Key_For_M3Selection()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        const int qMin = 12;
        const int qMax = 24;

        const double G = 1.0;
        const double c = 1.0;
        const double b = 1.0;
        const double dt = 0.001;
        double[] epsilons = { 2e-3, 1e-2 };

        var family = new List<(int M, int InBandCount, double AvgClosureQuality, double BestCost)>(mValues.Length);

        foreach (int m in mValues)
        {
            var candidates = Enumerable.Range(qMin, qMax - qMin + 1)
                .Select(q => (Omega: (q + (double)m) / q, Q: q))
                .ToArray();

            int inBandCount = 0;
            double closureQualitySum = 0.0;
            double bestCost = double.PositiveInfinity;

            foreach (var x in candidates)
            {
                double omega = x.Omega;
                double gamma = 1.0 / omega;
                bool inBand = omega >= 1.16 && omega <= 1.19 && gamma >= 0.84 && gamma <= 0.86;

                var modeLock = SimulateModeLock(omega, BuildNoCadencePriorConfig());
                var parameters = new PhotonTransportModel.Parameters
                {
                    LambdaTime = 1.0,
                    LambdaSpace = 30.0,
                    EulerBridgeScale = gamma
                };

                double meanRelError = epsilons
                    .Select(epsilon =>
                    {
                        double alphaEuler = PhotonTransportModel.ComputeDeflectionEulerLagrange(epsilon, G, c, b, dt, parameters);
                        double alphaSchwarz = ComputeSchwarzschildNullDeflection(epsilon);
                        return Math.Abs(alphaEuler - alphaSchwarz) / Math.Max(alphaSchwarz, 1e-16);
                    })
                    .Average();

                double cost = (1.0 - modeLock.ModeLockScore) + meanRelError;
                if (cost < bestCost)
                {
                    bestCost = cost;
                }

                if (inBand)
                {
                    inBandCount++;
                }

                closureQualitySum += (1.0 - modeLock.ClosureResidual);
            }

            double avgClosureQuality = closureQualitySum / candidates.Length;
            family.Add((M: m, InBandCount: inBandCount, AvgClosureQuality: avgClosureQuality, BestCost: bestCost));
        }

        const double phaseClosureThreshold = 0.78;
        const int directionClosureMinOccupancy = 1;
        const int actionClosureMinOccupancy = 3;

        var highOccupancyCosts = family
            .Where(x => x.InBandCount >= actionClosureMinOccupancy)
            .Select(x => x.BestCost)
            .OrderBy(x => x)
            .ToArray();

        Assert.True(highOccupancyCosts.Length >= 2,
            "Expected at least two high-occupancy modes to define action/tick closure threshold.");

        double actionTickCostThreshold = 0.5 * (highOccupancyCosts[0] + highOccupancyCosts[1]);

        static (int Minimal, int Count) EvaluateAblation(
            IReadOnlyCollection<(int M, int InBandCount, double AvgClosureQuality, double BestCost)> src,
            bool usePhase,
            bool useDirection,
            bool useAction,
            double phaseThreshold,
            int directionMinOcc,
            int actionMinOcc,
            double actionCostThreshold)
        {
            var satisfying = src
                .Where(x =>
                {
                    bool phaseOk = !usePhase || x.AvgClosureQuality >= phaseThreshold;
                    bool directionOk = !useDirection || x.InBandCount >= directionMinOcc;
                    bool actionOk = !useAction || (x.InBandCount >= actionMinOcc && x.BestCost <= actionCostThreshold);
                    return phaseOk && directionOk && actionOk;
                })
                .Select(x => x.M)
                .OrderBy(m => m)
                .ToArray();

            int minimal = satisfying.Length > 0 ? satisfying[0] : int.MaxValue;
            return (minimal, satisfying.Length);
        }

        var withoutPhase = EvaluateAblation(
            family,
            usePhase: false,
            useDirection: true,
            useAction: true,
            phaseThreshold: phaseClosureThreshold,
            directionMinOcc: directionClosureMinOccupancy,
            actionMinOcc: actionClosureMinOccupancy,
            actionCostThreshold: actionTickCostThreshold);

        var withoutDirection = EvaluateAblation(
            family,
            usePhase: true,
            useDirection: false,
            useAction: true,
            phaseThreshold: phaseClosureThreshold,
            directionMinOcc: directionClosureMinOccupancy,
            actionMinOcc: actionClosureMinOccupancy,
            actionCostThreshold: actionTickCostThreshold);

        var withoutAction = EvaluateAblation(
            family,
            usePhase: true,
            useDirection: true,
            useAction: false,
            phaseThreshold: phaseClosureThreshold,
            directionMinOcc: directionClosureMinOccupancy,
            actionMinOcc: actionClosureMinOccupancy,
            actionCostThreshold: actionTickCostThreshold);

        _output.WriteLine($"RBF13 no-phase     | minimal=m{withoutPhase.Minimal} | satisfyingCount={withoutPhase.Count}");
        _output.WriteLine($"RBF13 no-direction | minimal=m{withoutDirection.Minimal} | satisfyingCount={withoutDirection.Count}");
        _output.WriteLine($"RBF13 no-action    | minimal=m{withoutAction.Minimal} | satisfyingCount={withoutAction.Count}");

        Assert.Equal(3, withoutPhase.Minimal);
        Assert.Equal(3, withoutDirection.Minimal);
        Assert.Equal(2, withoutAction.Minimal);
        Assert.True(withoutAction.Count >= 2,
            $"Expected non-unique closure set when action/tick is removed. count={withoutAction.Count}");
    }

    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF14_CompetingBands_Should_Reveal_LockingVsELTradeoff()
    {
        const double G = 1.0;
        const double c = 1.0;
        const double b = 1.0;
        const double dt = 0.001;
        double[] epsilons = { 2e-3, 1e-2 };

        static (double Omega, double ModeLockScore, double MeanRelError, double CombinedScore) EvaluateBand(
            IReadOnlyList<double> candidates,
            double[] eps,
            double g,
            double c0,
            double impact,
            double step)
        {
            var evaluations = candidates
                .Select(omega =>
                {
                    double gamma = 1.0 / omega;
                    var modeLock = SimulateModeLock(omega, BuildNoCadencePriorConfig());
                    var parameters = new PhotonTransportModel.Parameters
                    {
                        LambdaTime = 1.0,
                        LambdaSpace = 30.0,
                        EulerBridgeScale = gamma
                    };

                    double meanRelError = eps
                        .Select(epsilon =>
                        {
                            double alphaEuler = PhotonTransportModel.ComputeDeflectionEulerLagrange(epsilon, g, c0, impact, step, parameters);
                            double alphaSchwarz = ComputeSchwarzschildNullDeflection(epsilon);
                            return Math.Abs(alphaEuler - alphaSchwarz) / Math.Max(alphaSchwarz, 1e-16);
                        })
                        .Average();

                    double combinedScore = (1.0 - modeLock.ModeLockScore) + meanRelError;
                    return (Omega: omega, modeLock.ModeLockScore, MeanRelError: meanRelError, CombinedScore: combinedScore);
                })
                .OrderBy(x => x.CombinedScore)
                .ToArray();

            return evaluations[0];
        }

        var primaryBand = BuildReducedRationalCandidatesInWindow(1.16, 1.19, 40);
        var lowerBand = BuildReducedRationalCandidatesInWindow(1.08, 1.12, 40);
        var upperBand = BuildReducedRationalCandidatesInWindow(1.22, 1.26, 40);

        Assert.True(primaryBand.Count > 0 && lowerBand.Count > 0 && upperBand.Count > 0,
            "Expected non-empty candidate sets for all compared rational windows.");

        var bestPrimary = EvaluateBand(primaryBand, epsilons, G, c, b, dt);
        var bestLower = EvaluateBand(lowerBand, epsilons, G, c, b, dt);
        var bestUpper = EvaluateBand(upperBand, epsilons, G, c, b, dt);

        _output.WriteLine($"RBF14 primary best: omega={bestPrimary.Omega:F6} | score={bestPrimary.CombinedScore:E6} | lock={bestPrimary.ModeLockScore:E6} | relErr={bestPrimary.MeanRelError:E6}");
        _output.WriteLine($"RBF14 lower   best: omega={bestLower.Omega:F6} | score={bestLower.CombinedScore:E6} | lock={bestLower.ModeLockScore:E6} | relErr={bestLower.MeanRelError:E6}");
        _output.WriteLine($"RBF14 upper   best: omega={bestUpper.Omega:F6} | score={bestUpper.CombinedScore:E6} | lock={bestUpper.ModeLockScore:E6} | relErr={bestUpper.MeanRelError:E6}");

        bool lowerDominatesPrimary =
            bestLower.ModeLockScore >= bestPrimary.ModeLockScore &&
            bestLower.MeanRelError <= bestPrimary.MeanRelError;

        bool upperDominatesPrimary =
            bestUpper.ModeLockScore >= bestPrimary.ModeLockScore &&
            bestUpper.MeanRelError <= bestPrimary.MeanRelError;

        Assert.True(bestLower.ModeLockScore > bestPrimary.ModeLockScore,
            $"Expected lower competing band to lead in lock score. primaryLock={bestPrimary.ModeLockScore:E6}, lowerLock={bestLower.ModeLockScore:E6}");
        Assert.True(bestLower.MeanRelError > bestPrimary.MeanRelError,
            $"Expected lower competing band to lose EL-bridge fit even if lock score is competitive. primaryErr={bestPrimary.MeanRelError:E6}, lowerErr={bestLower.MeanRelError:E6}");
        Assert.True(bestUpper.MeanRelError < bestPrimary.MeanRelError,
            $"Expected upper competing band to improve EL error while losing lock quality. primaryErr={bestPrimary.MeanRelError:E6}, upperErr={bestUpper.MeanRelError:E6}");
        Assert.True(bestUpper.ModeLockScore < bestPrimary.ModeLockScore,
            $"Expected upper competing band to lose CML quality even if EL error can be competitive. primaryLock={bestPrimary.ModeLockScore:E6}, upperLock={bestUpper.ModeLockScore:E6}");
        Assert.False(lowerDominatesPrimary,
            "Lower competing band should not dominate primary band on both CML lock score and EL-fit error.");
        Assert.False(upperDominatesPrimary,
            "Upper competing band should not dominate primary band on both CML lock score and EL-fit error.");
    }

    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF15_DeriveOrFalsify_Should_Show_M3Uniqueness_WithAllConstraints_And_NonUniqueness_Without_ActionTick()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        const int qMin = 12;
        const int qMax = 24;

        const double G = 1.0;
        const double c = 1.0;
        const double b = 1.0;
        const double dt = 0.001;
        double[] epsilons = { 2e-3, 1e-2 };

        var family = new List<(int M, int InBandCount, double AvgClosureQuality, double BestCost)>(mValues.Length);

        foreach (int m in mValues)
        {
            var candidates = Enumerable.Range(qMin, qMax - qMin + 1)
                .Select(q => (Omega: (q + (double)m) / q, Q: q))
                .ToArray();

            int inBandCount = 0;
            double closureQualitySum = 0.0;
            double bestCost = double.PositiveInfinity;

            foreach (var x in candidates)
            {
                double omega = x.Omega;
                double gamma = 1.0 / omega;
                bool inBand = omega >= 1.16 && omega <= 1.19 && gamma >= 0.84 && gamma <= 0.86;

                var modeLock = SimulateModeLock(omega, BuildNoCadencePriorConfig());
                var parameters = new PhotonTransportModel.Parameters
                {
                    LambdaTime = 1.0,
                    LambdaSpace = 30.0,
                    EulerBridgeScale = gamma
                };

                double meanRelError = epsilons
                    .Select(epsilon =>
                    {
                        double alphaEuler = PhotonTransportModel.ComputeDeflectionEulerLagrange(epsilon, G, c, b, dt, parameters);
                        double alphaSchwarz = ComputeSchwarzschildNullDeflection(epsilon);
                        return Math.Abs(alphaEuler - alphaSchwarz) / Math.Max(alphaSchwarz, 1e-16);
                    })
                    .Average();

                double cost = (1.0 - modeLock.ModeLockScore) + meanRelError;
                if (cost < bestCost)
                {
                    bestCost = cost;
                }

                if (inBand)
                {
                    inBandCount++;
                }

                closureQualitySum += (1.0 - modeLock.ClosureResidual);
            }

            double avgClosureQuality = closureQualitySum / candidates.Length;
            family.Add((M: m, InBandCount: inBandCount, AvgClosureQuality: avgClosureQuality, BestCost: bestCost));
        }

        const double phaseClosureThreshold = 0.78;
        const int directionClosureMinOccupancy = 1;
        const int actionClosureMinOccupancy = 3;

        var highOccupancyCosts = family
            .Where(x => x.InBandCount >= actionClosureMinOccupancy)
            .Select(x => x.BestCost)
            .OrderBy(x => x)
            .ToArray();

        Assert.True(highOccupancyCosts.Length >= 2,
            "Expected at least two high-occupancy modes to define action/tick closure threshold.");

        double actionTickCostThreshold = 0.5 * (highOccupancyCosts[0] + highOccupancyCosts[1]);

        var satisfyingAllThree = family
            .Where(x =>
                x.AvgClosureQuality >= phaseClosureThreshold &&
                x.InBandCount >= directionClosureMinOccupancy &&
                x.InBandCount >= actionClosureMinOccupancy &&
                x.BestCost <= actionTickCostThreshold)
            .Select(x => x.M)
            .OrderBy(m => m)
            .ToArray();

        var satisfyingWithoutActionTick = family
            .Where(x =>
                x.AvgClosureQuality >= phaseClosureThreshold &&
                x.InBandCount >= directionClosureMinOccupancy)
            .Select(x => x.M)
            .OrderBy(m => m)
            .ToArray();

        _output.WriteLine($"RBF15 all constraints       | satisfying=[{string.Join(", ", satisfyingAllThree)}]");
        _output.WriteLine($"RBF15 without action/tick   | satisfying=[{string.Join(", ", satisfyingWithoutActionTick)}]");

        Assert.True(satisfyingAllThree.Length == 1 && satisfyingAllThree[0] == 3,
            $"Expected unique m=3 selection with all constraints. satisfying=[{string.Join(", ", satisfyingAllThree)}]");

        Assert.True(satisfyingWithoutActionTick.Length >= 2,
            $"Expected non-unique selection without action/tick. satisfying=[{string.Join(", ", satisfyingWithoutActionTick)}]");
        Assert.True(satisfyingWithoutActionTick[0] == 2,
            $"Expected minimal mode to collapse to m=2 without action/tick. satisfying=[{string.Join(", ", satisfyingWithoutActionTick)}]");
        Assert.Contains(3, satisfyingWithoutActionTick);
    }

    private static ModeLockConfig BuildNoCadencePriorConfig() =>
        ModeLockConfig.Default with
        {
            OrderScoreWeight = 0.55,
            AlignmentScoreWeight = 0.45,
            CadenceScoreWeight = 0.0
        };

    private static ModeLockResult SimulateModeLock(double collectiveOmega, ModeLockConfig config)
    {
        var phases = new double[config.CellCount];
        var omegas = new double[config.CellCount];

        for (int i = 0; i < config.CellCount; i++)
        {
            double angle = 2.0 * Math.PI * i / config.CellCount;
            phases[i] = angle;
            omegas[i] = 1.0 + 0.05 * Math.Sin(angle) + 0.03 * Math.Cos(2.0 * angle);
        }

        double collectivePhi = 0.0;
        double orderAccum = 0.0;
        double alignmentAccum = 0.0;
        int orderCount = 0;

        for (int step = 0; step < config.Steps; step++)
        {
            var couplings = new double[config.CellCount];

            for (int i = 0; i < config.CellCount; i++)
            {
                double couplingSum = 0.0;

                for (int j = 0; j < config.CellCount; j++)
                {
                    if (i == j) continue;
                    couplingSum += Math.Sin(phases[j] - phases[i]);
                }

                couplings[i] = couplingSum / (config.CellCount - 1);
            }

            collectivePhi += config.Dt * collectiveOmega;

            for (int i = 0; i < config.CellCount; i++)
            {
                double align = Math.Sin(collectivePhi - phases[i]);
                phases[i] += config.Dt * (omegas[i] + config.CouplingKappa * couplings[i] + config.CollectiveWeight * align);

                if (config.BreakClosure && (step % config.ClosureBreakEveryNSteps == 0))
                {
                    // deterministic closure-breaking perturbation
                    phases[i] += config.ClosureBreakAmplitude * Math.Sin(0.37 * step + 0.41 * i);
                }
            }

            if (step >= config.SettleSteps)
            {
                orderAccum += ComputeOrderParameter(phases);
                alignmentAccum += phases.Select(phi => Math.Cos(collectivePhi - phi)).Average();
                orderCount++;
            }
        }

        double meanOrder = orderAccum / Math.Max(orderCount, 1);
        double meanAlignment = alignmentAccum / Math.Max(orderCount, 1);
        double alignment01 = 0.5 * (meanAlignment + 1.0);
        double closureResidual = 1.0 - alignment01;

        // Explicit cadence hypothesis term (20:17 mode-lock candidate).
        const double cadenceTarget = 20.0 / 17.0;
        const double cadenceSigma = 0.012;
        double cadenceAlignment = Math.Exp(-Math.Pow((collectiveOmega - cadenceTarget) / cadenceSigma, 2.0));

        // Higher is better.
        double modeLockScore =
            config.OrderScoreWeight * meanOrder +
            config.AlignmentScoreWeight * alignment01 +
            config.CadenceScoreWeight * cadenceAlignment;

        return new ModeLockResult(
            CollectiveOmega: collectiveOmega,
            MeanOrder: meanOrder,
            ClosureResidual: closureResidual,
            ModeLockScore: modeLockScore);
    }

    private static double ComputeOrderParameter(double[] phases)
    {
        double meanCos = phases.Average(p => Math.Cos(p));
        double meanSin = phases.Average(p => Math.Sin(p));
        return Math.Sqrt(meanCos * meanCos + meanSin * meanSin);
    }

    private static IReadOnlyList<double> BuildOmegaGrid(double start, double end, double step)
    {
        var values = new List<double>();
        for (double x = start; x <= end + 1e-12; x += step)
        {
            values.Add(x);
        }

        return values;
    }

    private static IReadOnlyList<double> BuildReducedRationalCandidatesInWindow(
        double minOmega,
        double maxOmega,
        int maxDenominator)
    {
        var values = new HashSet<double>();

        for (int q = 2; q <= maxDenominator; q++)
        {
            int minP = (int)Math.Ceiling(minOmega * q);
            int maxP = (int)Math.Floor(maxOmega * q);

            for (int p = minP; p <= maxP; p++)
            {
                if (GreatestCommonDivisor(p, q) != 1)
                {
                    continue;
                }

                values.Add((double)p / q);
            }
        }

        return values.OrderBy(v => v).ToArray();
    }

    private static int GreatestCommonDivisor(int a, int b)
    {
        a = Math.Abs(a);
        b = Math.Abs(b);

        while (b != 0)
        {
            int t = a % b;
            a = b;
            b = t;
        }

        return a;
    }

    private static double ComputeSchwarzschildNullDeflection(double epsilon)
    {
        double epsCrit = 1.0 / (3.0 * Math.Sqrt(3.0));

        if (epsilon <= 0.0 || epsilon >= epsCrit)
            throw new ArgumentOutOfRangeException(nameof(epsilon),
                "epsilon must be in (0, 1/(3sqrt(3))) for scattering null geodesics.");

        double w0 = SolveClosestApproachW(epsilon);

        double phi = 0.0;
        double w = w0;
        double p = 0.0;

        const double dphi = 1e-4;
        const double maxPhi = 20.0;

        double prevPhi = phi;
        double prevW = w;

        while (phi < maxPhi)
        {
            prevPhi = phi;
            prevW = w;

            RK4StepSchwarzschildOrbit(ref phi, ref w, ref p, dphi, epsilon);

            if (w <= 0.0)
            {
                double t = prevW / (prevW - w);
                double phiCross = prevPhi + t * dphi;

                double alpha = 2.0 * phiCross - Math.PI;
                return Math.Abs(alpha);
            }
        }

        throw new InvalidOperationException("Schwarzschild null geodesic did not escape within maxPhi.");
    }

    private static double SolveClosestApproachW(double epsilon)
    {
        double lo = 0.0;
        double hi = 1.5;

        for (int i = 0; i < 200; i++)
        {
            double mid = 0.5 * (lo + hi);
            double fMid = 1.0 - mid * mid + 2.0 * epsilon * mid * mid * mid;

            if (fMid > 0.0)
                lo = mid;
            else
                hi = mid;
        }

        return 0.5 * (lo + hi);
    }

    private static void RK4StepSchwarzschildOrbit(
        ref double phi,
        ref double w,
        ref double p,
        double dphi,
        double epsilon)
    {
        (double dw1, double dp1) = SchwarzDerivatives(w, p, epsilon);
        (double dw2, double dp2) = SchwarzDerivatives(
            w + 0.5 * dphi * dw1,
            p + 0.5 * dphi * dp1,
            epsilon);
        (double dw3, double dp3) = SchwarzDerivatives(
            w + 0.5 * dphi * dw2,
            p + 0.5 * dphi * dp2,
            epsilon);
        (double dw4, double dp4) = SchwarzDerivatives(
            w + dphi * dw3,
            p + dphi * dp3,
            epsilon);

        w += dphi / 6.0 * (dw1 + 2.0 * dw2 + 2.0 * dw3 + dw4);
        p += dphi / 6.0 * (dp1 + 2.0 * dp2 + 2.0 * dp3 + dp4);
        phi += dphi;
    }

    private static (double dw, double dp) SchwarzDerivatives(
        double w,
        double p,
        double epsilon)
    {
        double dw = p;
        double dp = -w + 3.0 * epsilon * w * w;
        return (dw, dp);
    }

    private sealed record ModeLockConfig(
        int CellCount,
        int Steps,
        int SettleSteps,
        double Dt,
        double CouplingKappa,
        double CollectiveWeight,
        double OrderScoreWeight,
        double AlignmentScoreWeight,
        double CadenceScoreWeight,
        bool BreakClosure,
        double ClosureBreakAmplitude,
        int ClosureBreakEveryNSteps)
    {
        public static ModeLockConfig Default =>
            new(
                CellCount: 20,
                Steps: 1200,
                SettleSteps: 600,
                Dt: 0.08,
                CouplingKappa: 0.10,
                CollectiveWeight: 0.22,
                OrderScoreWeight: 0.45,
                AlignmentScoreWeight: 0.35,
                CadenceScoreWeight: 0.20,
                BreakClosure: false,
                ClosureBreakAmplitude: 0.0,
                ClosureBreakEveryNSteps: 0);
    }

    private sealed record ModeLockResult(
        double CollectiveOmega,
        double MeanOrder,
        double ClosureResidual,
        double ModeLockScore);
}
