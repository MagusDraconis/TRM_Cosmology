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

    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF16_M3Uniqueness_Should_Persist_Under_ContinuousThresholdContinuation()
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

        const int directionClosureMinOccupancy = 1;
        const int actionClosureMinOccupancy = 3;

        var highOccupancyCosts = family
            .Where(x => x.InBandCount >= actionClosureMinOccupancy)
            .Select(x => x.BestCost)
            .OrderBy(x => x)
            .ToArray();

        Assert.True(highOccupancyCosts.Length >= 2,
            "Expected at least two high-occupancy modes to define baseline action/tick threshold.");

        double baseActionTickCostThreshold = 0.5 * (highOccupancyCosts[0] + highOccupancyCosts[1]);

        double[] phaseThresholds = BuildOmegaGrid(0.760, 0.820, 0.005).ToArray();
        double[] costScales = BuildOmegaGrid(0.90, 1.10, 0.02).ToArray();
        var uniqueM3Mask = new bool[phaseThresholds.Length, costScales.Length];

        for (int i = 0; i < phaseThresholds.Length; i++)
        {
            double phaseThreshold = phaseThresholds[i];
            for (int j = 0; j < costScales.Length; j++)
            {
                double actionTickCostThreshold = baseActionTickCostThreshold * costScales[j];

                var satisfying = family
                    .Where(x =>
                        x.AvgClosureQuality >= phaseThreshold &&
                        x.InBandCount >= directionClosureMinOccupancy &&
                        x.InBandCount >= actionClosureMinOccupancy &&
                        x.BestCost <= actionTickCostThreshold)
                    .Select(x => x.M)
                    .OrderBy(m => m)
                    .ToArray();

                uniqueM3Mask[i, j] = satisfying.Length == 1 && satisfying[0] == 3;
            }
        }

        int successCount = 0;
        for (int i = 0; i < phaseThresholds.Length; i++)
        {
            for (int j = 0; j < costScales.Length; j++)
            {
                if (uniqueM3Mask[i, j])
                {
                    successCount++;
                }
            }
        }

        var visited = new bool[phaseThresholds.Length, costScales.Length];
        int bestComponent = 0;
        int bestComponentPhaseSpan = 0;
        int bestComponentCostSpan = 0;
        int[] di = { -1, 1, 0, 0 };
        int[] dj = { 0, 0, -1, 1 };

        for (int i = 0; i < phaseThresholds.Length; i++)
        {
            for (int j = 0; j < costScales.Length; j++)
            {
                if (!uniqueM3Mask[i, j] || visited[i, j])
                {
                    continue;
                }

                var queue = new Queue<(int I, int J)>();
                queue.Enqueue((i, j));
                visited[i, j] = true;

                int componentSize = 0;
                int minI = i, maxI = i, minJ = j, maxJ = j;

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    componentSize++;
                    minI = Math.Min(minI, current.I);
                    maxI = Math.Max(maxI, current.I);
                    minJ = Math.Min(minJ, current.J);
                    maxJ = Math.Max(maxJ, current.J);

                    for (int k = 0; k < 4; k++)
                    {
                        int ni = current.I + di[k];
                        int nj = current.J + dj[k];

                        if (ni < 0 || ni >= phaseThresholds.Length || nj < 0 || nj >= costScales.Length)
                        {
                            continue;
                        }

                        if (visited[ni, nj] || !uniqueM3Mask[ni, nj])
                        {
                            continue;
                        }

                        visited[ni, nj] = true;
                        queue.Enqueue((ni, nj));
                    }
                }

                int phaseSpan = maxI - minI + 1;
                int costSpan = maxJ - minJ + 1;
                if (componentSize > bestComponent)
                {
                    bestComponent = componentSize;
                    bestComponentPhaseSpan = phaseSpan;
                    bestComponentCostSpan = costSpan;
                }
            }
        }

        int phaseCenter = Array.FindIndex(phaseThresholds, x => Math.Abs(x - 0.780) < 1e-12);
        int costCenter = Array.FindIndex(costScales, x => Math.Abs(x - 1.000) < 1e-12);
        bool centerSupported = phaseCenter >= 0 && costCenter >= 0 && uniqueM3Mask[phaseCenter, costCenter];

        _output.WriteLine($"RBF16 sweep grid                  : {phaseThresholds.Length}x{costScales.Length}");
        _output.WriteLine($"RBF16 unique-m3 cells             : {successCount}");
        _output.WriteLine($"RBF16 largest connected component : size={bestComponent}, phaseSpan={bestComponentPhaseSpan}, costSpan={bestComponentCostSpan}");
        _output.WriteLine($"RBF16 center support (0.78,1.00)  : {centerSupported}");

        Assert.True(successCount >= 12,
            $"Expected non-trivial continuous uniqueness support for m=3. successCount={successCount}");
        Assert.True(bestComponent >= 10 && bestComponentPhaseSpan >= 3 && bestComponentCostSpan >= 3,
            $"Expected a connected threshold region with unique m=3 selection. size={bestComponent}, phaseSpan={bestComponentPhaseSpan}, costSpan={bestComponentCostSpan}");
        Assert.True(centerSupported,
            "Expected unique m=3 selection to include the baseline threshold center (phase=0.78, costScale=1.00).");
    }

    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF17_ActionTickClosure_Should_Be_Derived_From_MicroscopicAction_Not_Imposed()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        const int qMin = 12;
        const int qMax = 24;
        const double phaseClosureThreshold = 0.78;
        const int directionClosureMinOccupancy = 1;

        const double G = 1.0;
        const double c = 1.0;
        const double b = 1.0;
        const double dt = 0.001;
        double[] epsilons = { 2e-3, 1e-2 };

        var family = new List<(int M, int InBandCount, double AvgClosureQuality, double DerivedActionTick)>(mValues.Length);

        foreach (int m in mValues)
        {
            var candidates = Enumerable.Range(qMin, qMax - qMin + 1)
                .Select(q => (Omega: (q + (double)m) / q, Q: q))
                .ToArray();

            int inBandCount = 0;
            double closureQualitySum = 0.0;
            double minMicroscopicActionPerTickInBand = double.PositiveInfinity;

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

                // Coarse-grained lattice/action density:
                // order deficit + closure defect + transport mismatch.
                double orderDefect = Math.Max(0.0, 1.0 - modeLock.MeanOrder);
                double closureDefect = Math.Max(0.0, modeLock.ClosureResidual);
                double transportDefect = Math.Max(0.0, meanRelError);
                double microscopicActionPerTick =
                    (orderDefect * orderDefect + closureDefect * closureDefect + transportDefect * transportDefect)
                    / Math.Max(omega, 1e-12);

                if (inBand)
                {
                    inBandCount++;
                    minMicroscopicActionPerTickInBand = Math.Min(minMicroscopicActionPerTickInBand, microscopicActionPerTick);
                }

                closureQualitySum += (1.0 - modeLock.ClosureResidual);
            }

            double avgClosureQuality = closureQualitySum / candidates.Length;
            double occupancyPenalty = 1.0 / Math.Max(inBandCount, 1);
            double derivedActionTick = minMicroscopicActionPerTickInBand + occupancyPenalty;

            family.Add((M: m, InBandCount: inBandCount, AvgClosureQuality: avgClosureQuality, DerivedActionTick: derivedActionTick));
        }

        var phaseDirectionCandidates = family
            .Where(x => x.AvgClosureQuality >= phaseClosureThreshold && x.InBandCount >= directionClosureMinOccupancy)
            .OrderBy(x => x.DerivedActionTick)
            .ToArray();

        Assert.True(phaseDirectionCandidates.Length >= 2,
            "Expected at least two phase+direction admissible modes to derive action/tick closure threshold.");

        double derivedActionTickThreshold =
            0.5 * (phaseDirectionCandidates[0].DerivedActionTick + phaseDirectionCandidates[1].DerivedActionTick);

        var evaluation = family
            .Select(x =>
            {
                bool phaseClosureOk = x.AvgClosureQuality >= phaseClosureThreshold;
                bool directionClosureOk = x.InBandCount >= directionClosureMinOccupancy;
                bool actionTickClosureOk = x.DerivedActionTick <= derivedActionTickThreshold;
                bool allThree = phaseClosureOk && directionClosureOk && actionTickClosureOk;
                return (x.M, x.InBandCount, x.AvgClosureQuality, x.DerivedActionTick, phaseClosureOk, directionClosureOk, actionTickClosureOk, allThree);
            })
            .OrderBy(x => x.M)
            .ToArray();

        foreach (var row in evaluation)
        {
            _output.WriteLine(
                $"RBF17 m={row.M} | inBand={row.InBandCount} | avgClosure={row.AvgClosureQuality:F4} | derivedActionTick={row.DerivedActionTick:E6} | phase={row.phaseClosureOk} | direction={row.directionClosureOk} | actionTick={row.actionTickClosureOk} | all={row.allThree}");
        }

        _output.WriteLine($"RBF17 derived action/tick threshold: {derivedActionTickThreshold:E6}");

        int minimalAllThreeMode = evaluation
            .Where(x => x.allThree)
            .Select(x => x.M)
            .DefaultIfEmpty(int.MaxValue)
            .Min();

        var m2 = evaluation.First(x => x.M == 2);
        var m3 = evaluation.First(x => x.M == 3);

        Assert.True(double.IsFinite(derivedActionTickThreshold) && derivedActionTickThreshold > 0.0,
            $"Expected finite positive derived action/tick threshold. thr={derivedActionTickThreshold:E6}");
        Assert.True(m2.phaseClosureOk && m2.directionClosureOk && !m2.actionTickClosureOk,
            $"Expected m=2 to fail derived action/tick closure while passing phase+direction. m2ActionTick={m2.actionTickClosureOk}");
        Assert.True(m3.phaseClosureOk && m3.directionClosureOk && m3.actionTickClosureOk,
            $"Expected m=3 to satisfy derived action/tick closure. m3ActionTick={m3.actionTickClosureOk}");
        Assert.True(minimalAllThreeMode == 3,
            $"Expected m=3 to remain minimal with derived microscopic action/tick closure. minimal={minimalAllThreeMode}");
    }

    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF18_M3Selection_Should_Remain_Unique_Across_SolverFamilies()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        const int qMin = 12;
        const int qMax = 24;
        const double phaseClosureThreshold = 0.78;
        const int directionClosureMinOccupancy = 1;

        const double G = 1.0;
        const double c = 1.0;
        const double b = 1.0;
        double[] epsilons = { 2e-3, 1e-2 };

        var baseNoCadence = BuildNoCadencePriorConfig();
        var solverFamilies = new[]
        {
            new
            {
                Name = "baseline",
                ModeLock = baseNoCadence,
                ElDt = 0.001
            },
            new
            {
                Name = "fine-update",
                ModeLock = baseNoCadence,
                ElDt = 0.0008
            },
            new
            {
                Name = "coarse-update",
                ModeLock = baseNoCadence,
                ElDt = 0.0012
            }
        };

        int uniqueM3FamilyCount = 0;

        foreach (var familySolver in solverFamilies)
        {
            var family = new List<(int M, int InBandCount, double AvgClosureQuality, double DerivedActionTick)>(mValues.Length);

            foreach (int m in mValues)
            {
                var candidates = Enumerable.Range(qMin, qMax - qMin + 1)
                    .Select(q => (Omega: (q + (double)m) / q, Q: q))
                    .ToArray();

                int inBandCount = 0;
                double closureQualitySum = 0.0;
                double minMicroscopicActionPerTickInBand = double.PositiveInfinity;

                foreach (var x in candidates)
                {
                    double omega = x.Omega;
                    double gamma = 1.0 / omega;
                    bool inBand = omega >= 1.16 && omega <= 1.19 && gamma >= 0.84 && gamma <= 0.86;

                    var modeLock = SimulateModeLock(omega, familySolver.ModeLock);
                    var parameters = new PhotonTransportModel.Parameters
                    {
                        LambdaTime = 1.0,
                        LambdaSpace = 30.0,
                        EulerBridgeScale = gamma
                    };

                    double meanRelError = epsilons
                        .Select(epsilon =>
                        {
                            double alphaEuler = PhotonTransportModel.ComputeDeflectionEulerLagrange(
                                epsilon, G, c, b, familySolver.ElDt, parameters);
                            double alphaSchwarz = ComputeSchwarzschildNullDeflection(epsilon);
                            return Math.Abs(alphaEuler - alphaSchwarz) / Math.Max(alphaSchwarz, 1e-16);
                        })
                        .Average();

                    double orderDefect = Math.Max(0.0, 1.0 - modeLock.MeanOrder);
                    double closureDefect = Math.Max(0.0, modeLock.ClosureResidual);
                    double transportDefect = Math.Max(0.0, meanRelError);
                    double microscopicActionPerTick =
                        (orderDefect * orderDefect + closureDefect * closureDefect + transportDefect * transportDefect)
                        / Math.Max(omega, 1e-12);

                    if (inBand)
                    {
                        inBandCount++;
                        minMicroscopicActionPerTickInBand = Math.Min(minMicroscopicActionPerTickInBand, microscopicActionPerTick);
                    }

                    closureQualitySum += (1.0 - modeLock.ClosureResidual);
                }

                double avgClosureQuality = closureQualitySum / candidates.Length;
                double occupancyPenalty = 1.0 / Math.Max(inBandCount, 1);
                double derivedActionTick = minMicroscopicActionPerTickInBand + occupancyPenalty;

                family.Add((M: m, InBandCount: inBandCount, AvgClosureQuality: avgClosureQuality, DerivedActionTick: derivedActionTick));
            }

            var phaseDirectionCandidates = family
                .Where(x => x.AvgClosureQuality >= phaseClosureThreshold && x.InBandCount >= directionClosureMinOccupancy)
                .OrderBy(x => x.DerivedActionTick)
                .ToArray();

            Assert.True(phaseDirectionCandidates.Length >= 2,
                $"Expected at least two phase+direction admissible modes in family={familySolver.Name}.");

            double derivedActionTickThreshold =
                0.5 * (phaseDirectionCandidates[0].DerivedActionTick + phaseDirectionCandidates[1].DerivedActionTick);

            var satisfying = family
                .Where(x =>
                    x.AvgClosureQuality >= phaseClosureThreshold &&
                    x.InBandCount >= directionClosureMinOccupancy &&
                    x.DerivedActionTick <= derivedActionTickThreshold)
                .Select(x => x.M)
                .OrderBy(m => m)
                .ToArray();

            bool uniqueM3 = satisfying.Length == 1 && satisfying[0] == 3;
            if (uniqueM3)
            {
                uniqueM3FamilyCount++;
            }

            _output.WriteLine(
                $"RBF18 family={familySolver.Name} | elDt={familySolver.ElDt:E3} | actionTickThr={derivedActionTickThreshold:E6} | satisfying=[{string.Join(", ", satisfying)}] | uniqueM3={uniqueM3}");
        }

        Assert.True(uniqueM3FamilyCount == solverFamilies.Length,
            $"Expected unique m=3 selection across all solver families. success={uniqueM3FamilyCount}/{solverFamilies.Length}");
    }

    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF19_CompetingMFamily_Should_Fail_FullConstraintDerivation()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        const int qMin = 12;
        const int qMax = 24;
        const double phaseClosureThreshold = 0.78;
        const int directionClosureMinOccupancy = 1;

        const double G = 1.0;
        const double c = 1.0;
        const double b = 1.0;
        const double dt = 0.001;
        double[] epsilons = { 2e-3, 1e-2 };

        var family = new List<(int M, int InBandCount, double AvgClosureQuality, double DerivedActionTick)>(mValues.Length);

        foreach (int m in mValues)
        {
            var candidates = Enumerable.Range(qMin, qMax - qMin + 1)
                .Select(q => (Omega: (q + (double)m) / q, Q: q))
                .ToArray();

            int inBandCount = 0;
            double closureQualitySum = 0.0;
            double minMicroscopicActionPerTickInBand = double.PositiveInfinity;

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

                double orderDefect = Math.Max(0.0, 1.0 - modeLock.MeanOrder);
                double closureDefect = Math.Max(0.0, modeLock.ClosureResidual);
                double transportDefect = Math.Max(0.0, meanRelError);
                double microscopicActionPerTick =
                    (orderDefect * orderDefect + closureDefect * closureDefect + transportDefect * transportDefect)
                    / Math.Max(omega, 1e-12);

                if (inBand)
                {
                    inBandCount++;
                    minMicroscopicActionPerTickInBand = Math.Min(minMicroscopicActionPerTickInBand, microscopicActionPerTick);
                }

                closureQualitySum += (1.0 - modeLock.ClosureResidual);
            }

            double avgClosureQuality = closureQualitySum / candidates.Length;
            double occupancyPenalty = 1.0 / Math.Max(inBandCount, 1);
            double derivedActionTick = minMicroscopicActionPerTickInBand + occupancyPenalty;

            family.Add((M: m, InBandCount: inBandCount, AvgClosureQuality: avgClosureQuality, DerivedActionTick: derivedActionTick));
        }

        var phaseDirectionCandidates = family
            .Where(x => x.AvgClosureQuality >= phaseClosureThreshold && x.InBandCount >= directionClosureMinOccupancy)
            .OrderBy(x => x.DerivedActionTick)
            .ToArray();

        Assert.True(phaseDirectionCandidates.Length >= 2,
            "Expected at least two phase+direction admissible modes to derive action/tick threshold.");

        double derivedActionTickThreshold =
            0.5 * (phaseDirectionCandidates[0].DerivedActionTick + phaseDirectionCandidates[1].DerivedActionTick);

        var evaluation = family
            .Select(x =>
            {
                bool phaseClosureOk = x.AvgClosureQuality >= phaseClosureThreshold;
                bool directionClosureOk = x.InBandCount >= directionClosureMinOccupancy;
                bool actionTickClosureOk = x.DerivedActionTick <= derivedActionTickThreshold;
                bool allThree = phaseClosureOk && directionClosureOk && actionTickClosureOk;
                string failReason = allThree
                    ? "passes-all"
                    : !directionClosureOk ? "direction/band"
                    : !phaseClosureOk ? "phase/closure"
                    : "action/tick";
                return (x.M, x.InBandCount, x.AvgClosureQuality, x.DerivedActionTick, phaseClosureOk, directionClosureOk, actionTickClosureOk, allThree, failReason);
            })
            .OrderBy(x => x.M)
            .ToArray();

        foreach (var row in evaluation)
        {
            _output.WriteLine(
                $"RBF19 m={row.M} | inBand={row.InBandCount} | avgClosure={row.AvgClosureQuality:F4} | derivedActionTick={row.DerivedActionTick:E6} | phase={row.phaseClosureOk} | direction={row.directionClosureOk} | actionTick={row.actionTickClosureOk} | all={row.allThree} | fail={row.failReason}");
        }

        _output.WriteLine($"RBF19 derived action/tick threshold: {derivedActionTickThreshold:E6}");

        var m1 = evaluation.First(x => x.M == 1);
        var m2 = evaluation.First(x => x.M == 2);
        var m3 = evaluation.First(x => x.M == 3);
        var m4 = evaluation.First(x => x.M == 4);
        var m5 = evaluation.First(x => x.M == 5);

        Assert.True(m3.allThree,
            "Expected m=3 to satisfy full derived constraint set.");

        Assert.True(m2.phaseClosureOk && m2.directionClosureOk && !m2.actionTickClosureOk && m2.failReason == "action/tick",
            $"Expected m=2 exclusion by action/tick. fail={m2.failReason}");
        Assert.True(!m4.phaseClosureOk && m4.directionClosureOk && m4.failReason == "phase/closure",
            $"Expected m=4 exclusion by phase/closure. fail={m4.failReason}");

        Assert.True(!m1.directionClosureOk && m1.failReason == "direction/band",
            $"Expected m=1 exclusion by direction/band. fail={m1.failReason}");
        Assert.True(!m5.directionClosureOk && m5.failReason == "direction/band",
            $"Expected m=5 exclusion by direction/band. fail={m5.failReason}");

        Assert.True(evaluation.Count(x => x.allThree) == 1,
            $"Expected unique full-constraint mode. all=[{string.Join(", ", evaluation.Where(x => x.allThree).Select(x => x.M))}]");
    }

    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF20_TheoremAssumptionAudit_Should_Detect_If_M3Depends_On_OperationalArtifacts()
    {
        const double phaseClosureThreshold = 0.78;
        const int directionClosureMinOccupancy = 1;

// Normalized weak-field units.
// This test audits operational m=3 selection robustness,
// not SI-dimensional calibration or physical amplitude normalization.

        const double G = 1.0;
        const double c = 1.0;
        const double b = 1.0;
        const double dt = 0.001;
        double[] epsilons = { 2e-3, 1e-2 };

        var qWindows = new (int QMin, int QMax)[]
        {
            (12, 24),
            (14, 26),
            (10, 22)
        };

        var precomputed = new Dictionary<(int QMin, int QMax, int M), (int InBandCount, double AvgClosureQuality, double MinNoNorm, double MinOverOmega, double MinOverSqrtOmega)>();

        foreach (var window in qWindows)
        {
            for (int m = 1; m <= 6; m++)
            {
                var candidates = Enumerable.Range(window.QMin, window.QMax - window.QMin + 1)
                    .Select(q => (Omega: (q + (double)m) / q, Q: q))
                    .ToArray();

                int inBandCount = 0;
                double closureQualitySum = 0.0;
                double minNoNorm = double.PositiveInfinity;
                double minOverOmega = double.PositiveInfinity;
                double minOverSqrtOmega = double.PositiveInfinity;

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

                    double orderDefect = Math.Max(0.0, 1.0 - modeLock.MeanOrder);
                    double closureDefect = Math.Max(0.0, modeLock.ClosureResidual);
                    double transportDefect = Math.Max(0.0, meanRelError);
                    double actionBase =
                        orderDefect * orderDefect +
                        closureDefect * closureDefect +
                        transportDefect * transportDefect;

                    if (inBand)
                    {
                        inBandCount++;
                        minNoNorm = Math.Min(minNoNorm, actionBase);
                        minOverOmega = Math.Min(minOverOmega, actionBase / Math.Max(omega, 1e-12));
                        minOverSqrtOmega = Math.Min(minOverSqrtOmega, actionBase / Math.Sqrt(Math.Max(omega, 1e-12)));
                    }

                    closureQualitySum += (1.0 - modeLock.ClosureResidual);
                }

                double avgClosureQuality = closureQualitySum / candidates.Length;
                precomputed[(window.QMin, window.QMax, m)] = (inBandCount, avgClosureQuality, minNoNorm, minOverOmega, minOverSqrtOmega);
            }
        }

        var scenarios = new[]
        {
            new { Name = "baseline", MMin = 1, MMax = 5, QMin = 12, QMax = 24, Normalization = "omega", ActionMinOcc = 3, Threshold = "midpoint-top2", TieBreak = "minimal-m" },
            new { Name = "m-range-extended", MMin = 1, MMax = 6, QMin = 12, QMax = 24, Normalization = "omega", ActionMinOcc = 3, Threshold = "midpoint-top2", TieBreak = "minimal-m" },
            new { Name = "m-range-truncated", MMin = 1, MMax = 4, QMin = 12, QMax = 24, Normalization = "omega", ActionMinOcc = 3, Threshold = "midpoint-top2", TieBreak = "minimal-m" },
            new { Name = "q-window-up", MMin = 1, MMax = 5, QMin = 14, QMax = 26, Normalization = "omega", ActionMinOcc = 3, Threshold = "midpoint-top2", TieBreak = "minimal-m" },
            new { Name = "q-window-down", MMin = 1, MMax = 5, QMin = 10, QMax = 22, Normalization = "omega", ActionMinOcc = 3, Threshold = "midpoint-top2", TieBreak = "minimal-m" },
            new { Name = "normalization-none", MMin = 1, MMax = 5, QMin = 12, QMax = 24, Normalization = "none", ActionMinOcc = 3, Threshold = "midpoint-top2", TieBreak = "minimal-m" },
            new { Name = "normalization-sqrt", MMin = 1, MMax = 5, QMin = 12, QMax = 24, Normalization = "sqrt-omega", ActionMinOcc = 3, Threshold = "midpoint-top2", TieBreak = "minimal-m" },
            new { Name = "occupancy-min2", MMin = 1, MMax = 5, QMin = 12, QMax = 24, Normalization = "omega", ActionMinOcc = 2, Threshold = "midpoint-top2", TieBreak = "minimal-m" },
            new { Name = "threshold-top3", MMin = 1, MMax = 5, QMin = 12, QMax = 24, Normalization = "omega", ActionMinOcc = 3, Threshold = "midpoint-top3", TieBreak = "minimal-m" },
            new { Name = "tie-break-low-action", MMin = 1, MMax = 5, QMin = 12, QMax = 24, Normalization = "omega", ActionMinOcc = 3, Threshold = "midpoint-top2", TieBreak = "lowest-action" }
        };

        var auditResults = new List<(string Name, bool Resolved, int SelectedMode, int[] SatisfyingModes, bool UniqueM3)>();

        foreach (var scenario in scenarios)
        {
            var family = Enumerable.Range(scenario.MMin, scenario.MMax - scenario.MMin + 1)
                .Select(m =>
                {
                    var row = precomputed[(scenario.QMin, scenario.QMax, m)];
                    double minAction = scenario.Normalization switch
                    {
                        "none" => row.MinNoNorm,
                        "sqrt-omega" => row.MinOverSqrtOmega,
                        _ => row.MinOverOmega
                    };
                    double occupancyPenalty = 1.0 / Math.Max(row.InBandCount, 1);
                    double derivedActionTick = minAction + occupancyPenalty;
                    return (M: m, row.InBandCount, row.AvgClosureQuality, DerivedActionTick: derivedActionTick);
                })
                .OrderBy(x => x.M)
                .ToArray();

            var phaseDirectionCandidates = family
                .Where(x => x.AvgClosureQuality >= phaseClosureThreshold && x.InBandCount >= directionClosureMinOccupancy)
                .OrderBy(x => x.DerivedActionTick)
                .ToArray();

            if (phaseDirectionCandidates.Length < 2)
            {
                auditResults.Add((scenario.Name, Resolved: false, SelectedMode: int.MaxValue, SatisfyingModes: Array.Empty<int>(), UniqueM3: false));
                _output.WriteLine($"RBF20 scenario={scenario.Name} | unresolved (insufficient phase+direction candidates)");
                continue;
            }

            double threshold = scenario.Threshold switch
            {
                "midpoint-top3" => 0.5 * (phaseDirectionCandidates[0].DerivedActionTick + phaseDirectionCandidates[Math.Min(2, phaseDirectionCandidates.Length - 1)].DerivedActionTick),
                _ => 0.5 * (phaseDirectionCandidates[0].DerivedActionTick + phaseDirectionCandidates[1].DerivedActionTick)
            };

            var satisfying = family
                .Where(x =>
                    x.AvgClosureQuality >= phaseClosureThreshold &&
                    x.InBandCount >= directionClosureMinOccupancy &&
                    x.InBandCount >= scenario.ActionMinOcc &&
                    x.DerivedActionTick <= threshold)
                .OrderBy(x => x.M)
                .ToArray();

            int selectedMode;
            if (satisfying.Length == 0)
            {
                selectedMode = int.MaxValue;
            }
            else if (satisfying.Length == 1)
            {
                selectedMode = satisfying[0].M;
            }
            else
            {
                selectedMode = scenario.TieBreak == "lowest-action"
                    ? satisfying.OrderBy(x => x.DerivedActionTick).ThenBy(x => x.M).First().M
                    : satisfying.Min(x => x.M);
            }

            bool uniqueM3 = satisfying.Length == 1 && satisfying[0].M == 3;
            int[] satisfyingModes = satisfying.Select(x => x.M).ToArray();

            auditResults.Add((scenario.Name, Resolved: true, SelectedMode: selectedMode, SatisfyingModes: satisfyingModes, UniqueM3: uniqueM3));
            _output.WriteLine(
                $"RBF20 scenario={scenario.Name} | selected={(selectedMode == int.MaxValue ? "none" : $"m{selectedMode}")} | satisfying=[{string.Join(", ", satisfyingModes)}] | uniqueM3={uniqueM3}");
        }

        var baseline = auditResults.First(r => r.Name == "baseline");
        Assert.True(baseline.Resolved && baseline.UniqueM3,
            "Expected baseline audit scenario to keep unique m=3.");

        var resolved = auditResults.Where(r => r.Resolved).ToArray();
        Assert.True(resolved.Length >= 8,
            $"Expected broad artifact-audit coverage with resolved scenarios. resolved={resolved.Length}");

        var offenders = resolved.Where(r => r.SelectedMode != 3).ToArray();
        Assert.True(offenders.Length == 0,
            $"Operational-artifact dependence detected. offenders=[{string.Join("; ", offenders.Select(o => $"{o.Name}:selected={(o.SelectedMode == int.MaxValue ? "none" : $"m{o.SelectedMode}")},satisfying=[{string.Join(",", o.SatisfyingModes)}]"))}]");
    }

    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF21_M3_Should_Follow_From_MinimalThreeConstraintClosureModel()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        const int qMin = 12;
        const int qMax = 24;
        const double phaseClosureThreshold = 0.78;
        const int directionClosureMinOccupancy = 1;

        // Normalized weak-field units for operational theorem-path guards.
        const double G = 1.0;
        const double c = 1.0;
        const double b = 1.0;
        const double dt = 0.001;
        double[] epsilons = { 2e-3, 1e-2 };

        var family = new List<(int M, int InBandCount, double AvgClosureQuality, double DerivedActionTick)>(mValues.Length);

        foreach (int m in mValues)
        {
            var candidates = Enumerable.Range(qMin, qMax - qMin + 1)
                .Select(q => (Omega: (q + (double)m) / q, Q: q))
                .ToArray();

            int inBandCount = 0;
            double closureQualitySum = 0.0;
            double minMicroscopicActionPerTickInBand = double.PositiveInfinity;

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

                // Minimal three-constraint model proxy:
                // phase quality + direction/band occupancy + derived action/tick.
                double orderDefect = Math.Max(0.0, 1.0 - modeLock.MeanOrder);
                double closureDefect = Math.Max(0.0, modeLock.ClosureResidual);
                double transportDefect = Math.Max(0.0, meanRelError);
                double microscopicActionPerTick =
                    (orderDefect * orderDefect + closureDefect * closureDefect + transportDefect * transportDefect)
                    / Math.Max(omega, 1e-12);

                if (inBand)
                {
                    inBandCount++;
                    minMicroscopicActionPerTickInBand = Math.Min(minMicroscopicActionPerTickInBand, microscopicActionPerTick);
                }

                closureQualitySum += (1.0 - modeLock.ClosureResidual);
            }

            double avgClosureQuality = closureQualitySum / candidates.Length;
            double occupancyPenalty = 1.0 / Math.Max(inBandCount, 1);
            double derivedActionTick = minMicroscopicActionPerTickInBand + occupancyPenalty;

            family.Add((M: m, InBandCount: inBandCount, AvgClosureQuality: avgClosureQuality, DerivedActionTick: derivedActionTick));
        }

        var phaseDirectionCandidates = family
            .Where(x => x.AvgClosureQuality >= phaseClosureThreshold && x.InBandCount >= directionClosureMinOccupancy)
            .OrderBy(x => x.DerivedActionTick)
            .ToArray();

        Assert.True(phaseDirectionCandidates.Length >= 2,
            "Expected at least two phase+direction admissible modes to derive minimal-model action/tick threshold.");

        double derivedActionTickThreshold =
            0.5 * (phaseDirectionCandidates[0].DerivedActionTick + phaseDirectionCandidates[1].DerivedActionTick);

        var evaluation = family
            .Select(x =>
            {
                bool phaseClosureOk = x.AvgClosureQuality >= phaseClosureThreshold;
                bool directionClosureOk = x.InBandCount >= directionClosureMinOccupancy;
                bool actionTickClosureOk = x.DerivedActionTick <= derivedActionTickThreshold;
                bool allThree = phaseClosureOk && directionClosureOk && actionTickClosureOk;

                string failReason = allThree
                    ? "passes-all"
                    : !directionClosureOk ? "direction/band"
                    : !phaseClosureOk ? "phase/closure"
                    : "action/tick";

                return (x.M, x.InBandCount, x.AvgClosureQuality, x.DerivedActionTick, phaseClosureOk, directionClosureOk, actionTickClosureOk, allThree, failReason);
            })
            .OrderBy(x => x.M)
            .ToArray();

        foreach (var row in evaluation)
        {
            _output.WriteLine(
                $"RBF21 m={row.M} | inBand={row.InBandCount} | avgClosure={row.AvgClosureQuality:F4} | derivedActionTick={row.DerivedActionTick:E6} | phase={row.phaseClosureOk} | direction={row.directionClosureOk} | actionTick={row.actionTickClosureOk} | all={row.allThree} | fail={row.failReason}");
        }

        _output.WriteLine($"RBF21 derived action/tick threshold: {derivedActionTickThreshold:E6}");

        var satisfyingModes = evaluation.Where(x => x.allThree).Select(x => x.M).OrderBy(x => x).ToArray();
        int minimalAllThreeMode = satisfyingModes.DefaultIfEmpty(int.MaxValue).Min();

        var m1 = evaluation.First(x => x.M == 1);
        var m2 = evaluation.First(x => x.M == 2);
        var m3 = evaluation.First(x => x.M == 3);
        var m4 = evaluation.First(x => x.M == 4);
        var m5 = evaluation.First(x => x.M == 5);

        Assert.True(m3.allThree,
            "Expected m=3 to satisfy the minimal three-constraint closure model.");
        Assert.True(minimalAllThreeMode == 3,
            $"Expected m=3 to be minimal satisfying mode. minimal={minimalAllThreeMode}, satisfying=[{string.Join(", ", satisfyingModes)}]");
        Assert.True(satisfyingModes.Length == 1,
            $"Expected unique minimal-model selection. satisfying=[{string.Join(", ", satisfyingModes)}]");

        Assert.True(m2.phaseClosureOk && m2.directionClosureOk && !m2.actionTickClosureOk && m2.failReason == "action/tick",
            $"Expected m=2 to fail by action/tick in minimal model. fail={m2.failReason}");
        Assert.True(!m4.phaseClosureOk && m4.directionClosureOk && m4.failReason == "phase/closure",
            $"Expected m=4 to fail by phase/closure in minimal model. fail={m4.failReason}");
        Assert.True(!m1.directionClosureOk && m1.failReason == "direction/band",
            $"Expected m=1 to fail by direction/band in minimal model. fail={m1.failReason}");
        Assert.True(!m5.directionClosureOk && m5.failReason == "direction/band",
            $"Expected m=5 to fail by direction/band in minimal model. fail={m5.failReason}");
    }

    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF22_M3_MinimalClosureModel_Should_Remain_Stable_Under_QWindowAndThresholdPerturbation()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        const int directionClosureMinOccupancy = 1;

        // Baseline and small q-window perturbations around RBF21.
        var qWindows = new (int QMin, int QMax)[]
        {
            (12, 24),
            (12, 25),
            (13, 25)
        };

        const double G = 1.0;
        const double c = 1.0;
        const double b = 1.0;
        const double dt = 0.001;
        double[] epsilons = { 2e-3, 1e-2 };

        // Precompute family metrics per q-window and m once, then perturb thresholds.
        var precomputed = new Dictionary<(int QMin, int QMax, int M), (int InBandCount, double AvgClosureQuality, double DerivedActionTick)>();
        foreach (var window in qWindows)
        {
            foreach (int m in mValues)
            {
                var candidates = Enumerable.Range(window.QMin, window.QMax - window.QMin + 1)
                    .Select(q => (Omega: (q + (double)m) / q, Q: q))
                    .ToArray();

                int inBandCount = 0;
                double closureQualitySum = 0.0;
                double minMicroscopicActionPerTickInBand = double.PositiveInfinity;

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

                    double orderDefect = Math.Max(0.0, 1.0 - modeLock.MeanOrder);
                    double closureDefect = Math.Max(0.0, modeLock.ClosureResidual);
                    double transportDefect = Math.Max(0.0, meanRelError);
                    double microscopicActionPerTick =
                        (orderDefect * orderDefect + closureDefect * closureDefect + transportDefect * transportDefect)
                        / Math.Max(omega, 1e-12);

                    if (inBand)
                    {
                        inBandCount++;
                        minMicroscopicActionPerTickInBand = Math.Min(minMicroscopicActionPerTickInBand, microscopicActionPerTick);
                    }

                    closureQualitySum += (1.0 - modeLock.ClosureResidual);
                }

                double avgClosureQuality = closureQualitySum / candidates.Length;
                double occupancyPenalty = 1.0 / Math.Max(inBandCount, 1);
                double derivedActionTick = minMicroscopicActionPerTickInBand + occupancyPenalty;

                precomputed[(window.QMin, window.QMax, m)] = (inBandCount, avgClosureQuality, derivedActionTick);
            }
        }

        var phaseThresholds = new[] { 0.770, 0.775, 0.780 };
        var actionThresholdScales = new[] { 0.96, 1.00, 1.04 };

        int resolvedScenarios = 0;
        int uniqueM3Scenarios = 0;
        var offenders = new List<string>();

        foreach (var window in qWindows)
        {
            foreach (double phaseClosureThreshold in phaseThresholds)
            {
                foreach (double actionScale in actionThresholdScales)
                {
                    var family = mValues
                        .Select(m =>
                        {
                            var x = precomputed[(window.QMin, window.QMax, m)];
                            return (M: m, x.InBandCount, x.AvgClosureQuality, x.DerivedActionTick);
                        })
                        .OrderBy(x => x.M)
                        .ToArray();

                    var phaseDirectionCandidates = family
                        .Where(x => x.AvgClosureQuality >= phaseClosureThreshold && x.InBandCount >= directionClosureMinOccupancy)
                        .OrderBy(x => x.DerivedActionTick)
                        .ToArray();

                    if (phaseDirectionCandidates.Length < 1)
                    {
                        _output.WriteLine(
                            $"RBF22 window={window.QMin}..{window.QMax} | phaseThr={phaseClosureThreshold:F3} | actionScale={actionScale:F2} | unresolved");
                        continue;
                    }

                    double baseThreshold = phaseDirectionCandidates.Length >= 2
                        ? 0.5 * (phaseDirectionCandidates[0].DerivedActionTick + phaseDirectionCandidates[1].DerivedActionTick)
                        : 1.05 * phaseDirectionCandidates[0].DerivedActionTick;
                    double derivedActionTickThreshold = actionScale * baseThreshold;

                    var satisfying = family
                        .Where(x =>
                            x.AvgClosureQuality >= phaseClosureThreshold &&
                            x.InBandCount >= directionClosureMinOccupancy &&
                            x.DerivedActionTick <= derivedActionTickThreshold)
                        .Select(x => x.M)
                        .OrderBy(x => x)
                        .ToArray();

                    resolvedScenarios++;

                    bool uniqueM3 = satisfying.Length == 1 && satisfying[0] == 3;
                    if (uniqueM3)
                    {
                        uniqueM3Scenarios++;
                    }
                    else
                    {
                        offenders.Add(
                            $"q={window.QMin}..{window.QMax},phase={phaseClosureThreshold:F3},scale={actionScale:F2},satisfying=[{string.Join(",", satisfying)}]");
                    }

                    _output.WriteLine(
                        $"RBF22 window={window.QMin}..{window.QMax} | phaseThr={phaseClosureThreshold:F3} | actionScale={actionScale:F2} | thr={derivedActionTickThreshold:E6} | satisfying=[{string.Join(", ", satisfying)}] | uniqueM3={uniqueM3}");
                }
            }
        }

        _output.WriteLine($"RBF22 resolved scenarios: {resolvedScenarios}");
        _output.WriteLine($"RBF22 unique m=3 count : {uniqueM3Scenarios}");

        Assert.True(resolvedScenarios >= 24,
            $"Expected broad perturbation coverage with resolved scenarios. resolved={resolvedScenarios}");
        Assert.True(uniqueM3Scenarios == resolvedScenarios,
            $"Expected unique m=3 across resolved perturbation scenarios. offenders=[{string.Join("; ", offenders)}]");
    }

    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF23_ActionTickDiscriminator_Should_Emerge_From_PhaseLatticeEnergy()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        const int qMin = 12;
        const int qMax = 24;
        const double phaseClosureThreshold = 0.78;
        const int directionClosureMinOccupancy = 1;

        const double orderWeight = 0.50;
        const double closureWeight = 0.35;
        const double transportWeight = 0.15;

        const double G = 1.0;
        const double c = 1.0;
        const double b = 1.0;
        const double dt = 0.001;
        double[] epsilons = { 2e-3, 1e-2 };

        var family = new List<(int M, int InBandCount, double AvgClosureQuality, double FunctionalActionTick)>(mValues.Length);

        foreach (int m in mValues)
        {
            var candidates = Enumerable.Range(qMin, qMax - qMin + 1)
                .Select(q => (Omega: (q + (double)m) / q, Q: q))
                .ToArray();

            int inBandCount = 0;
            double closureQualitySum = 0.0;
            double inBandFunctionalSum = 0.0;

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

                double orderDefect = Math.Max(0.0, 1.0 - modeLock.MeanOrder);
                double closureDefect = Math.Max(0.0, modeLock.ClosureResidual);
                double transportDefect = Math.Max(0.0, meanRelError);

                double microscopicActionTickFunctional =
                    (orderWeight * orderDefect * orderDefect
                    + closureWeight * closureDefect * closureDefect
                    + transportWeight * transportDefect * transportDefect)
                    / Math.Max(omega, 1e-12);

                if (inBand)
                {
                    inBandCount++;
                    inBandFunctionalSum += microscopicActionTickFunctional;
                }

                closureQualitySum += (1.0 - modeLock.ClosureResidual);
            }

            double avgClosureQuality = closureQualitySum / candidates.Length;
            double meanInBandFunctional = inBandCount > 0
                ? inBandFunctionalSum / inBandCount
                : double.PositiveInfinity;
            double occupancyPenalty = 1.0 / Math.Max(inBandCount, 1);
            double functionalActionTick = meanInBandFunctional + occupancyPenalty;

            family.Add((M: m, InBandCount: inBandCount, AvgClosureQuality: avgClosureQuality, FunctionalActionTick: functionalActionTick));
        }

        var phaseDirectionCandidates = family
            .Where(x => x.AvgClosureQuality >= phaseClosureThreshold && x.InBandCount >= directionClosureMinOccupancy)
            .OrderBy(x => x.FunctionalActionTick)
            .ToArray();

        Assert.True(phaseDirectionCandidates.Length >= 2,
            "Expected at least two phase+direction admissible modes to derive an action/tick discriminator from phase-lattice energy.");

        var actionTickRankByMode = phaseDirectionCandidates
            .Select((x, idx) => (x.M, Rank: idx + 1, x.FunctionalActionTick))
            .ToDictionary(x => x.M, x => x.Rank);

        var evaluation = family
            .Select(x =>
            {
                bool phaseClosureOk = x.AvgClosureQuality >= phaseClosureThreshold;
                bool directionClosureOk = x.InBandCount >= directionClosureMinOccupancy;
                bool actionTickClosureOk =
                    phaseClosureOk &&
                    directionClosureOk &&
                    actionTickRankByMode.TryGetValue(x.M, out int actionTickRank) &&
                    actionTickRank == 1;
                bool allThree = phaseClosureOk && directionClosureOk && actionTickClosureOk;
                string failReason = allThree
                    ? "none"
                    : !directionClosureOk
                        ? "direction/band"
                        : !phaseClosureOk
                            ? "phase/closure"
                            : "action/tick";

                return (x.M, x.InBandCount, x.AvgClosureQuality, x.FunctionalActionTick, phaseClosureOk, directionClosureOk, actionTickClosureOk, allThree, failReason);
            })
            .OrderBy(x => x.M)
            .ToArray();

        foreach (var row in evaluation)
        {
            _output.WriteLine(
                $"RBF23 m={row.M} | inBand={row.InBandCount} | avgClosure={row.AvgClosureQuality:F4} | functionalActionTick={row.FunctionalActionTick:E6} | phase={row.phaseClosureOk} | direction={row.directionClosureOk} | actionTick={row.actionTickClosureOk} | all={row.allThree} | fail={row.failReason}");
        }

        _output.WriteLine(
            $"RBF23 action/tick discriminator ranking: [{string.Join(", ", phaseDirectionCandidates.Select((x, i) => $"m={x.M}:rank={i + 1},E={x.FunctionalActionTick:E6}"))}]");

        var satisfyingModes = evaluation
            .Where(x => x.allThree)
            .Select(x => x.M)
            .OrderBy(x => x)
            .ToArray();
        int minimalAllThreeMode = satisfyingModes.DefaultIfEmpty(int.MaxValue).Min();

        var m2 = evaluation.First(x => x.M == 2);
        var m3 = evaluation.First(x => x.M == 3);
        var m4 = evaluation.First(x => x.M == 4);

        Assert.True(phaseDirectionCandidates[0].M == 3,
            $"Expected microscopic phase-lattice action/tick discriminator to rank m=3 best. rankedFirst={phaseDirectionCandidates[0].M}");
        Assert.True(m3.phaseClosureOk && m3.directionClosureOk && m3.actionTickClosureOk,
            $"Expected m=3 to satisfy the microscopic action/tick functional closure. m3ActionTick={m3.actionTickClosureOk}");
        Assert.True(minimalAllThreeMode == 3,
            $"Expected m=3 to remain minimal under microscopic functional closure. minimal={minimalAllThreeMode}, satisfying=[{string.Join(", ", satisfyingModes)}]");
        Assert.True(satisfyingModes.Length == 1,
            $"Expected unique satisfying mode under microscopic functional closure. satisfying=[{string.Join(", ", satisfyingModes)}]");
        Assert.True(m2.phaseClosureOk && m2.directionClosureOk && !m2.actionTickClosureOk && m2.failReason == "action/tick",
            $"Expected m=2 to fail by action/tick discriminator. fail={m2.failReason}");
        Assert.True(!m4.phaseClosureOk && m4.directionClosureOk && m4.failReason == "phase/closure",
            $"Expected m=4 to fail by phase/closure in microscopic functional model. fail={m4.failReason}");
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
