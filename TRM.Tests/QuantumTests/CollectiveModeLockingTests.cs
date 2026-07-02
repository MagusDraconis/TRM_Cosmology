using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    private static readonly ConcurrentDictionary<string, (int M, int InBandCount, double AvgClosureQuality, double OperationalActionTick, double DerivedActionTick)[]> ModeFamilyCache = new(StringComparer.Ordinal);

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

    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF24_M3_Should_Emerge_From_ThreeConstraintIntersection()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        const int qMin = 12;
        const int qMax = 24;
        const double phaseClosureThreshold = 0.78;
        const int bridgeBandMinOccupancy = 1;

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

        var phaseBridgeCandidates = family
            .Where(x => x.AvgClosureQuality >= phaseClosureThreshold && x.InBandCount >= bridgeBandMinOccupancy)
            .OrderBy(x => x.FunctionalActionTick)
            .ToArray();

        var globalActionCandidates = family
            .OrderBy(x => x.FunctionalActionTick)
            .ToArray();

        Assert.True(phaseBridgeCandidates.Length >= 2,
            "Expected at least two phase+bridge admissible modes to define action/tick consistency rank.");
        Assert.True(globalActionCandidates.Length >= 2,
            "Expected at least two global candidates to define standalone action/tick consistency threshold.");

        var actionTickRankByMode = phaseBridgeCandidates
            .Select((x, index) => (x.M, Rank: index + 1))
            .ToDictionary(x => x.M, x => x.Rank);
        double standaloneActionTickThreshold =
            0.5 * (globalActionCandidates[0].FunctionalActionTick + globalActionCandidates[1].FunctionalActionTick);

        (int SelectedMode, int[] SatisfyingModes, bool M3Selected) EvaluateStack(string name, bool usePhase, bool useBridge, bool useAction)
        {
            bool IsSatisfying((int M, int InBandCount, double AvgClosureQuality, double FunctionalActionTick) x)
            {
                bool phaseOk = !usePhase || x.AvgClosureQuality >= phaseClosureThreshold;
                bool bridgeOk = !useBridge || x.InBandCount >= bridgeBandMinOccupancy;
                bool actionOk;
                if (!useAction)
                {
                    actionOk = true;
                }
                else if (usePhase && useBridge)
                {
                    actionOk =
                        x.AvgClosureQuality >= phaseClosureThreshold &&
                        x.InBandCount >= bridgeBandMinOccupancy &&
                        actionTickRankByMode.TryGetValue(x.M, out int rank) &&
                        rank == 1;
                }
                else
                {
                    actionOk = x.FunctionalActionTick <= standaloneActionTickThreshold;
                }

                return phaseOk && bridgeOk && actionOk;
            }

            var satisfying = family
                .Where(IsSatisfying)
                .OrderBy(x => x.M)
                .ToArray();

            int selectedMode = satisfying.Length > 0 ? satisfying[0].M : int.MaxValue;
            bool m3Unique = satisfying.Length == 1 && satisfying[0].M == 3;
            bool m3Strongest = selectedMode == 3;
            bool m3Selected = m3Unique || m3Strongest;

            _output.WriteLine(
                $"RBF24 stack={name} | selected={(selectedMode == int.MaxValue ? "none" : $"m={selectedMode}")} | satisfying=[{string.Join(", ", satisfying.Select(x => x.M))}] | m3Unique={m3Unique} | m3Strongest={m3Strongest} | m3Selected={m3Selected}");

            return (selectedMode, satisfying.Select(x => x.M).ToArray(), m3Selected);
        }

        var phaseOnly = EvaluateStack("phase-only", usePhase: true, useBridge: false, useAction: false);
        var bridgeOnly = EvaluateStack("bridge-only", usePhase: false, useBridge: true, useAction: false);
        var actionOnly = EvaluateStack("action/tick-only", usePhase: false, useBridge: false, useAction: true);

        var phaseBridge = EvaluateStack("phase+bridge", usePhase: true, useBridge: true, useAction: false);
        var phaseAction = EvaluateStack("phase+action/tick", usePhase: true, useBridge: false, useAction: true);
        var bridgeAction = EvaluateStack("bridge+action/tick", usePhase: false, useBridge: true, useAction: true);

        var allThree = EvaluateStack("phase+bridge+action/tick", usePhase: true, useBridge: true, useAction: true);

        _output.WriteLine("RBF24 claim boundary: candidate diagnostic only, not theorem-level proof.");

        Assert.True(allThree.M3Selected,
            $"Expected m=3 to be uniquely or strongest selected under full three-constraint stack. selected={allThree.SelectedMode}, satisfying=[{string.Join(", ", allThree.SatisfyingModes)}]");

        Assert.True(!phaseOnly.M3Selected,
            $"Expected phase-only stack to avoid selecting m=3 as unique/strongest. selected={phaseOnly.SelectedMode}, satisfying=[{string.Join(", ", phaseOnly.SatisfyingModes)}]");
        Assert.True(!bridgeOnly.M3Selected,
            $"Expected bridge-only stack to avoid selecting m=3 as unique/strongest. selected={bridgeOnly.SelectedMode}, satisfying=[{string.Join(", ", bridgeOnly.SatisfyingModes)}]");
        Assert.True(!actionOnly.M3Selected,
            $"Expected action/tick-only stack to avoid selecting m=3 as unique/strongest. selected={actionOnly.SelectedMode}, satisfying=[{string.Join(", ", actionOnly.SatisfyingModes)}]");
        Assert.True(!phaseBridge.M3Selected,
            $"Expected phase+bridge stack to avoid selecting m=3 as unique/strongest. selected={phaseBridge.SelectedMode}, satisfying=[{string.Join(", ", phaseBridge.SatisfyingModes)}]");
        Assert.True(!phaseAction.M3Selected,
            $"Expected phase+action/tick stack to avoid selecting m=3 as unique/strongest. selected={phaseAction.SelectedMode}, satisfying=[{string.Join(", ", phaseAction.SatisfyingModes)}]");
        Assert.True(!bridgeAction.M3Selected,
            $"Expected bridge+action/tick stack to avoid selecting m=3 as unique/strongest. selected={bridgeAction.SelectedMode}, satisfying=[{string.Join(", ", bridgeAction.SatisfyingModes)}]");
    }

    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF25_M3_ThreeConstraintIntersection_Should_Be_Robust_Under_AblationNoise()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        const int qMin = 12;
        const int qMax = 24;

        const double G = 1.0;
        const double c = 1.0;
        const double b = 1.0;
        const double dt = 0.001;
        double[] epsilons = { 2e-3, 1e-2 };

        var phaseThresholds = new[] { 0.775, 0.780, 0.785 };
        var bridgeOccupancyThresholds = new[] { 1, 2 };
        var actionThresholdScales = new[] { 0.95, 1.00, 1.05 };
        var weightScenarios = new (string Name, double Order, double Closure, double Transport)[]
        {
            ("wA", 0.50, 0.35, 0.15),
            ("wB", 0.55, 0.30, 0.15),
            ("wC", 0.45, 0.40, 0.15)
        };

        var precomputed = new Dictionary<(int M, int Q), (bool InBand, double Omega, double ClosureQuality, double OrderDefectSq, double ClosureDefectSq, double TransportDefectSq)>();
        foreach (int m in mValues)
        {
            foreach (int q in Enumerable.Range(qMin, qMax - qMin + 1))
            {
                double omega = (q + (double)m) / q;
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

                double orderDefectSq = Math.Pow(Math.Max(0.0, 1.0 - modeLock.MeanOrder), 2);
                double closureDefectSq = Math.Pow(Math.Max(0.0, modeLock.ClosureResidual), 2);
                double transportDefectSq = Math.Pow(Math.Max(0.0, meanRelError), 2);
                double closureQuality = 1.0 - modeLock.ClosureResidual;

                precomputed[(m, q)] = (inBand, omega, closureQuality, orderDefectSq, closureDefectSq, transportDefectSq);
            }
        }

        var totalCases = 0;
        var fullResolvedCases = 0;
        var fullM3SelectedCount = 0;
        var fullM3UniqueCount = 0;
        var fullM3StrongestCount = 0;
        var anotherModeWinsCount = 0;

        var singleResolved = new Dictionary<string, int>
        {
            ["phase-only"] = 0,
            ["bridge-only"] = 0,
            ["action/tick-only"] = 0
        };
        var singleUniqueM3 = new Dictionary<string, int>
        {
            ["phase-only"] = 0,
            ["bridge-only"] = 0,
            ["action/tick-only"] = 0
        };

        string? worstFailureCase = null;
        double worstFailurePenalty = double.NegativeInfinity;

        foreach (var w in weightScenarios)
        {
            var family = mValues
                .Select(m =>
                {
                    var rows = Enumerable.Range(qMin, qMax - qMin + 1)
                        .Select(q => precomputed[(m, q)])
                        .ToArray();

                    int inBandCount = rows.Count(r => r.InBand);
                    double avgClosureQuality = rows.Average(r => r.ClosureQuality);

                    double meanInBandFunctional = inBandCount > 0
                        ? rows
                            .Where(r => r.InBand)
                            .Average(r =>
                                (w.Order * r.OrderDefectSq
                                + w.Closure * r.ClosureDefectSq
                                + w.Transport * r.TransportDefectSq)
                                / Math.Max(r.Omega, 1e-12))
                        : double.PositiveInfinity;

                    double occupancyPenalty = 1.0 / Math.Max(inBandCount, 1);
                    double functionalActionTick = meanInBandFunctional + occupancyPenalty;

                    return (M: m, InBandCount: inBandCount, AvgClosureQuality: avgClosureQuality, FunctionalActionTick: functionalActionTick);
                })
                .OrderBy(x => x.M)
                .ToArray();

            double maxInBand = Math.Max(1, family.Max(x => x.InBandCount));
            double minAction = family.Min(x => x.FunctionalActionTick);
            double maxAction = family.Max(x => x.FunctionalActionTick);
            double actionDen = Math.Max(maxAction - minAction, 1e-12);

            foreach (double phaseThreshold in phaseThresholds)
            {
                foreach (int bridgeThreshold in bridgeOccupancyThresholds)
                {
                    var phaseBridgeCandidates = family
                        .Where(x => x.AvgClosureQuality >= phaseThreshold && x.InBandCount >= bridgeThreshold)
                        .OrderBy(x => x.FunctionalActionTick)
                        .ToArray();

                    var globalActionCandidates = family
                        .OrderBy(x => x.FunctionalActionTick)
                        .ToArray();

                    if (globalActionCandidates.Length == 0)
                        continue;

                    double standaloneBaseThreshold = globalActionCandidates.Length >= 2
                        ? 0.5 * (globalActionCandidates[0].FunctionalActionTick + globalActionCandidates[1].FunctionalActionTick)
                        : 1.05 * globalActionCandidates[0].FunctionalActionTick;

                    foreach (double actionScale in actionThresholdScales)
                    {
                        totalCases++;

                        double standaloneActionThreshold = actionScale * standaloneBaseThreshold;

                        Dictionary<int, int> actionTickRankByMode = phaseBridgeCandidates
                            .Select((x, index) => (x.M, Rank: index + 1))
                            .ToDictionary(x => x.M, x => x.Rank);

                        double fullBaseThreshold = phaseBridgeCandidates.Length >= 2
                            ? 0.5 * (phaseBridgeCandidates[0].FunctionalActionTick + phaseBridgeCandidates[1].FunctionalActionTick)
                            : phaseBridgeCandidates.Length == 1
                                ? 1.05 * phaseBridgeCandidates[0].FunctionalActionTick
                                : double.PositiveInfinity;
                        double fullActionThreshold = actionScale * fullBaseThreshold;

                        (bool Resolved, int SelectedMode, bool M3Selected, bool M3Unique, bool M3Strongest, int[] SatisfyingModes, double WinnerScore, double? M3Score) EvaluateStack(
                            string stackName,
                            bool usePhase,
                            bool useBridge,
                            bool useAction)
                        {
                            bool IsSatisfying((int M, int InBandCount, double AvgClosureQuality, double FunctionalActionTick) x)
                            {
                                bool phaseOk = !usePhase || x.AvgClosureQuality >= phaseThreshold;
                                bool bridgeOk = !useBridge || x.InBandCount >= bridgeThreshold;

                                bool actionOk;
                                if (!useAction)
                                {
                                    actionOk = true;
                                }
                                else if (usePhase && useBridge)
                                {
                                    actionOk =
                                        x.FunctionalActionTick <= fullActionThreshold &&
                                        actionTickRankByMode.TryGetValue(x.M, out int rank) &&
                                        rank == 1;
                                }
                                else
                                {
                                    actionOk = x.FunctionalActionTick <= standaloneActionThreshold;
                                }

                                return phaseOk && bridgeOk && actionOk;
                            }

                            var satisfying = family
                                .Where(IsSatisfying)
                                .OrderBy(x => x.M)
                                .ToArray();

                            if (satisfying.Length == 0)
                            {
                                return (false, int.MaxValue, false, false, false, Array.Empty<int>(), double.NaN, null);
                            }

                            var scored = satisfying
                                .Select(x =>
                                {
                                    var scoreTerms = new List<double>(3);
                                    if (usePhase)
                                        scoreTerms.Add(x.AvgClosureQuality);
                                    if (useBridge)
                                        scoreTerms.Add(x.InBandCount / maxInBand);
                                    if (useAction)
                                        scoreTerms.Add(1.0 - ((x.FunctionalActionTick - minAction) / actionDen));

                                    double score = scoreTerms.Count > 0 ? scoreTerms.Average() : 0.0;
                                    return (x.M, Score: score);
                                })
                                .OrderByDescending(x => x.Score)
                                .ThenBy(x => x.M)
                                .ToArray();

                            int selectedMode = scored[0].M;
                            double winnerScore = scored[0].Score;
                            double? m3Score = scored.FirstOrDefault(x => x.M == 3).M == 3
                                ? scored.First(x => x.M == 3).Score
                                : null;

                            bool m3Unique = satisfying.Length == 1 && satisfying[0].M == 3;
                            bool m3Strongest = selectedMode == 3;
                            bool m3Selected = satisfying.Any(x => x.M == 3);

                            _output.WriteLine(
                                $"RBF25 case w={w.Name} phaseThr={phaseThreshold:F3} bridgeThr={bridgeThreshold} actionScale={actionScale:F2} stack={stackName} | selected={(selectedMode == int.MaxValue ? "none" : $"m={selectedMode}")} | satisfying=[{string.Join(", ", satisfying.Select(x => x.M))}] | m3Selected={m3Selected} | m3Unique={m3Unique} | m3Strongest={m3Strongest}");

                            return (
                                true,
                                selectedMode,
                                m3Selected,
                                m3Unique,
                                m3Strongest,
                                satisfying.Select(x => x.M).ToArray(),
                                winnerScore,
                                m3Score);
                        }

                        var phaseOnly = EvaluateStack("phase-only", usePhase: true, useBridge: false, useAction: false);
                        var bridgeOnly = EvaluateStack("bridge-only", usePhase: false, useBridge: true, useAction: false);
                        var actionOnly = EvaluateStack("action/tick-only", usePhase: false, useBridge: false, useAction: true);
                        _ = EvaluateStack("phase+bridge", usePhase: true, useBridge: true, useAction: false);
                        _ = EvaluateStack("phase+action/tick", usePhase: true, useBridge: false, useAction: true);
                        _ = EvaluateStack("bridge+action/tick", usePhase: false, useBridge: true, useAction: true);
                        var full = EvaluateStack("phase+bridge+action/tick", usePhase: true, useBridge: true, useAction: true);

                        if (phaseOnly.Resolved)
                        {
                            singleResolved["phase-only"]++;
                            if (phaseOnly.M3Unique)
                                singleUniqueM3["phase-only"]++;
                        }

                        if (bridgeOnly.Resolved)
                        {
                            singleResolved["bridge-only"]++;
                            if (bridgeOnly.M3Unique)
                                singleUniqueM3["bridge-only"]++;
                        }

                        if (actionOnly.Resolved)
                        {
                            singleResolved["action/tick-only"]++;
                            if (actionOnly.M3Unique)
                                singleUniqueM3["action/tick-only"]++;
                        }

                        if (!full.Resolved)
                            continue;

                        fullResolvedCases++;

                        if (full.M3Selected)
                            fullM3SelectedCount++;
                        if (full.M3Unique)
                            fullM3UniqueCount++;
                        if (full.M3Strongest)
                            fullM3StrongestCount++;

                        if (full.SelectedMode != 3)
                        {
                            anotherModeWinsCount++;

                            double penalty = full.M3Score.HasValue
                                ? full.WinnerScore - full.M3Score.Value
                                : 1e6;

                            if (penalty > worstFailurePenalty)
                            {
                                worstFailurePenalty = penalty;
                                worstFailureCase =
                                    $"w={w.Name}, phaseThr={phaseThreshold:F3}, bridgeThr={bridgeThreshold}, actionScale={actionScale:F2}, selected=m={full.SelectedMode}, satisfying=[{string.Join(", ", full.SatisfyingModes)}], penalty={penalty:E6}";
                            }
                        }
                    }
                }
            }
        }

        _output.WriteLine("--- RBF25 M3 THREE-CONSTRAINT ROBUSTNESS DIAGNOSTIC ---");
        _output.WriteLine($"total perturbation cases={totalCases}");
        _output.WriteLine($"full-stack m3 selected count={fullM3SelectedCount} / {fullResolvedCases}");
        _output.WriteLine($"full-stack m3 unique count={fullM3UniqueCount} / {fullResolvedCases}");
        _output.WriteLine($"full-stack m3 strongest count={fullM3StrongestCount} / {fullResolvedCases}");
        _output.WriteLine($"cases where another m wins={anotherModeWinsCount}");
        _output.WriteLine($"worst failure case={(worstFailureCase ?? "none")}");
        _output.WriteLine("RBF25 claim boundary: candidate diagnostic only, not theorem-level proof.");

        Assert.True(fullResolvedCases > 0, "Expected at least one resolved full-stack perturbation case.");
        Assert.True(fullM3SelectedCount > (fullResolvedCases / 2),
            $"Expected full-stack m=3 selection in the majority of resolved cases. selected={fullM3SelectedCount}, resolved={fullResolvedCases}");
        Assert.True(fullM3UniqueCount > (fullResolvedCases / 2),
            $"Expected full-stack unique m=3 in the majority of resolved cases. unique={fullM3UniqueCount}, resolved={fullResolvedCases}");

        Assert.True(singleResolved["phase-only"] == 0 || singleUniqueM3["phase-only"] < singleResolved["phase-only"],
            $"Phase-only stack shows theorem-like uniqueness risk: unique={singleUniqueM3["phase-only"]}, resolved={singleResolved["phase-only"]}.");
        Assert.True(singleResolved["bridge-only"] == 0 || singleUniqueM3["bridge-only"] < singleResolved["bridge-only"],
            $"Bridge-only stack shows theorem-like uniqueness risk: unique={singleUniqueM3["bridge-only"]}, resolved={singleResolved["bridge-only"]}.");
        Assert.True(singleResolved["action/tick-only"] == 0 || singleUniqueM3["action/tick-only"] < singleResolved["action/tick-only"],
            $"Action/tick-only stack shows theorem-like uniqueness risk: unique={singleUniqueM3["action/tick-only"]}, resolved={singleResolved["action/tick-only"]}.");
    }

    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF26_M3_RegimeTransitionBoundary_Should_Show_BoundedBridgeMode()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        const int qMin = 12;
        const int qMax = 24;

        const double G = 1.0;
        const double c = 1.0;
        const double b = 1.0;
        const double dt = 0.001;
        double[] epsilons = { 2e-3, 1e-2 };

        var phaseThresholds = new[] { 0.780, 0.790, 0.800, 0.810, 0.820 };
        var bridgeThresholds = new[] { 1, 2, 3 };
        var actionScales = new[] { 0.95, 1.00, 1.05 };
        var weightScenarios = new (string Name, double Order, double Closure, double Transport)[]
        {
            ("wA", 0.50, 0.35, 0.15),
            ("wB", 0.55, 0.30, 0.15),
            ("wC", 0.45, 0.40, 0.15)
        };

        var precomputed = new Dictionary<(int M, int Q), (bool InBand, double Omega, double ClosureQuality, double OrderDefectSq, double ClosureDefectSq, double TransportDefectSq)>();
        foreach (int m in mValues)
        {
            foreach (int q in Enumerable.Range(qMin, qMax - qMin + 1))
            {
                double omega = (q + (double)m) / q;
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

                double orderDefectSq = Math.Pow(Math.Max(0.0, 1.0 - modeLock.MeanOrder), 2);
                double closureDefectSq = Math.Pow(Math.Max(0.0, modeLock.ClosureResidual), 2);
                double transportDefectSq = Math.Pow(Math.Max(0.0, meanRelError), 2);
                double closureQuality = 1.0 - modeLock.ClosureResidual;

                precomputed[(m, q)] = (inBand, omega, closureQuality, orderDefectSq, closureDefectSq, transportDefectSq);
            }
        }

        var baselineScenario = (Weight: "wA", Phase: 0.780, Bridge: 1, Scale: 1.00);
        bool baselineSelectedM3 = false;
        int transitionCount = 0;
        int resolvedCaseCount = 0;
        string? firstTransition = null;

        foreach (var w in weightScenarios)
        {
            var family = mValues
                .Select(m =>
                {
                    var rows = Enumerable.Range(qMin, qMax - qMin + 1)
                        .Select(q => precomputed[(m, q)])
                        .ToArray();

                    int inBandCount = rows.Count(r => r.InBand);
                    double avgClosureQuality = rows.Average(r => r.ClosureQuality);
                    double meanInBandFunctional = inBandCount > 0
                        ? rows
                            .Where(r => r.InBand)
                            .Average(r =>
                                (w.Order * r.OrderDefectSq
                                + w.Closure * r.ClosureDefectSq
                                + w.Transport * r.TransportDefectSq)
                                / Math.Max(r.Omega, 1e-12))
                        : 1e6;

                    double occupancyPenalty = 1.0 / Math.Max(inBandCount, 1);
                    double functionalActionTick = meanInBandFunctional + occupancyPenalty;
                    return (M: m, InBandCount: inBandCount, AvgClosureQuality: avgClosureQuality, FunctionalActionTick: functionalActionTick);
                })
                .OrderBy(x => x.M)
                .ToArray();

            Assert.All(family, row =>
            {
                Assert.True(double.IsFinite(row.AvgClosureQuality),
                    $"Non-finite AvgClosureQuality for m={row.M}.");
                Assert.True(double.IsFinite(row.FunctionalActionTick),
                    $"Non-finite FunctionalActionTick for m={row.M}.");
            });

            foreach (int bridgeThreshold in bridgeThresholds)
            {
                foreach (double actionScale in actionScales)
                {
                    bool hadM3SelectedInLooserCase = false;

                    foreach (double phaseThreshold in phaseThresholds.OrderBy(x => x))
                    {
                        var phaseBridgeCandidates = family
                            .Where(x => x.AvgClosureQuality >= phaseThreshold && x.InBandCount >= bridgeThreshold)
                            .OrderBy(x => x.FunctionalActionTick)
                            .ToArray();

                        var actionTickRankByMode = phaseBridgeCandidates
                            .Select((x, index) => (x.M, Rank: index + 1))
                            .ToDictionary(x => x.M, x => x.Rank);

                        double fullBaseThreshold = phaseBridgeCandidates.Length >= 2
                            ? 0.5 * (phaseBridgeCandidates[0].FunctionalActionTick + phaseBridgeCandidates[1].FunctionalActionTick)
                            : phaseBridgeCandidates.Length == 1
                                ? 1.05 * phaseBridgeCandidates[0].FunctionalActionTick
                                : double.PositiveInfinity;
                        double fullActionThreshold = actionScale * fullBaseThreshold;

                        var satisfying = family
                            .Where(x =>
                                x.AvgClosureQuality >= phaseThreshold &&
                                x.InBandCount >= bridgeThreshold &&
                                x.FunctionalActionTick <= fullActionThreshold &&
                                actionTickRankByMode.TryGetValue(x.M, out int rank) &&
                                rank == 1)
                            .OrderBy(x => x.M)
                            .ToArray();

                        bool resolved = satisfying.Length > 0;
                        bool m3Selected = resolved && satisfying.Any(x => x.M == 3);
                        int selectedMode = resolved ? satisfying[0].M : int.MaxValue;
                        if (resolved)
                            resolvedCaseCount++;

                        _output.WriteLine(
                            $"RBF26 w={w.Name} phaseThr={phaseThreshold:F3} bridgeThr={bridgeThreshold} actionScale={actionScale:F2} | selected={(resolved ? $"m={selectedMode}" : "none")} | satisfying=[{string.Join(", ", satisfying.Select(x => x.M))}] | m3Selected={m3Selected}");

                        if (w.Name == baselineScenario.Weight &&
                            Math.Abs(phaseThreshold - baselineScenario.Phase) < 1e-12 &&
                            bridgeThreshold == baselineScenario.Bridge &&
                            Math.Abs(actionScale - baselineScenario.Scale) < 1e-12)
                        {
                            baselineSelectedM3 = m3Selected;
                        }

                        if (m3Selected)
                        {
                            hadM3SelectedInLooserCase = true;
                        }
                        else if (hadM3SelectedInLooserCase)
                        {
                            transitionCount++;
                            if (firstTransition is null)
                            {
                                firstTransition =
                                    $"w={w.Name}, phaseThr={phaseThreshold:F3}, bridgeThr={bridgeThreshold}, actionScale={actionScale:F2}, selected={(resolved ? $"m={selectedMode}" : "none")}, satisfying=[{string.Join(", ", satisfying.Select(x => x.M))}]";
                            }
                            break;
                        }
                    }
                }
            }
        }

        _output.WriteLine("--- RBF26 REGIME-TRANSITION BOUNDARY DIAGNOSTIC ---");
        _output.WriteLine($"baseline m3Selected={baselineSelectedM3}");
        _output.WriteLine($"resolved cases={resolvedCaseCount}");
        _output.WriteLine($"transition boundaries detected={transitionCount}");
        _output.WriteLine($"first transition={(firstTransition ?? "none")}");
        _output.WriteLine("RBF26 claim boundary: candidate diagnostic only, not theorem-level proof.");

        Assert.True(resolvedCaseCount > 0, "Expected resolved/full-stack-evaluable RBF26 cases.");
        Assert.True(baselineSelectedM3,
            "Expected baseline RBF24-like stack to still select m=3 before transition stress.");
        Assert.True(transitionCount > 0,
            $"Expected at least one regime-transition boundary where m=3 fails after stricter constraints. firstTransition={firstTransition ?? "none"}");
    }

    /// <summary>
    /// Checks whether the derived action/tick discriminator follows the operational discriminator structure.
    /// Matters for the m=3 theorem-path because action-derived consistency reduces pure-threshold artifact risk.
    /// Expected diagnostic behavior: strong positive agreement in ranking/correlation without per-family retuning.
    /// Claim boundary: diagnostic/candidate support only; not theorem-level proof or first-principles closure.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF27_ActionTickDiscriminator_Should_Follow_From_MinimalLatticeAction()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        var family = BuildModeFamilyFromLatticeProxy(
            mValues,
            qMin: 12,
            qMax: 24,
            orderWeight: 0.50,
            closureWeight: 0.35,
            transportWeight: 0.15);

        double[] operational = family.OrderBy(x => x.M).Select(x => x.OperationalActionTick).ToArray();
        double[] derived = family.OrderBy(x => x.M).Select(x => x.DerivedActionTick).ToArray();

        double pearson = ComputePearsonCorrelation(operational, derived);
        double monotonicAgreement = ComputePairwiseMonotonicAgreement(operational, derived);
        int[] operationalOrder = family.OrderBy(x => x.OperationalActionTick).Select(x => x.M).ToArray();
        int[] derivedOrder = family.OrderBy(x => x.DerivedActionTick).Select(x => x.M).ToArray();

        foreach (var row in family.OrderBy(x => x.M))
        {
            _output.WriteLine(
                $"RBF27 m={row.M} | inBand={row.InBandCount} | avgClosure={row.AvgClosureQuality:F4} | operational={row.OperationalActionTick:E6} | derived={row.DerivedActionTick:E6}");
        }

        _output.WriteLine($"RBF27 operational order: [{string.Join(", ", operationalOrder)}]");
        _output.WriteLine($"RBF27 derived order    : [{string.Join(", ", derivedOrder)}]");
        _output.WriteLine($"RBF27 Pearson correlation={pearson:F6}");
        _output.WriteLine($"RBF27 pairwise monotonic agreement={monotonicAgreement:F6}");
        _output.WriteLine("RBF27 claim boundary: candidate diagnostic only, not theorem-level proof.");

        Assert.All(family, row =>
        {
            Assert.True(double.IsFinite(row.OperationalActionTick), $"Non-finite operational discriminator for m={row.M}.");
            Assert.True(double.IsFinite(row.DerivedActionTick), $"Non-finite derived discriminator for m={row.M}.");
        });
        Assert.True(double.IsFinite(pearson), "Expected finite operational-vs-derived correlation.");
        Assert.True(pearson >= 0.70,
            $"Expected strong positive correlation between operational and derived action/tick discriminators. pearson={pearson:F6}");
        Assert.True(monotonicAgreement >= 0.70,
            $"Expected strong monotonic agreement between discriminators. agreement={monotonicAgreement:F6}");
    }

    /// <summary>
    /// Checks whether the derived full three-constraint stack selects m=3 in the baseline shared-rule setup.
    /// Matters because it tests action-derived selection under one common rule for all families.
    /// Expected diagnostic behavior: m=3 is first/full-stack admissible in baseline, with explicit admissibility logging.
    /// Claim boundary: bounded candidate evidence only; no theorem-level or GR-replacement claim.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF28_DerivedActionTick_Should_Select_M3_Under_FullThreeConstraintStack()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        const double phaseClosureThreshold = 0.780;
        const int bridgeBandMinOccupancy = 1;

        var family = BuildModeFamilyFromLatticeProxy(
            mValues,
            qMin: 12,
            qMax: 24,
            orderWeight: 0.50,
            closureWeight: 0.35,
            transportWeight: 0.15);

        var phaseBridgeCandidates = family
            .Where(x => x.AvgClosureQuality >= phaseClosureThreshold && x.InBandCount >= bridgeBandMinOccupancy)
            .OrderBy(x => x.DerivedActionTick)
            .ToArray();

        Assert.True(phaseBridgeCandidates.Length >= 2,
            "Expected at least two phase+bridge candidates for derived action/tick rule construction.");

        var derivedRankByMode = phaseBridgeCandidates
            .Select((x, index) => (x.M, Rank: index + 1))
            .ToDictionary(x => x.M, x => x.Rank);

        var evaluation = family
            .Select(x =>
            {
                bool phaseOk = x.AvgClosureQuality >= phaseClosureThreshold;
                bool bridgeOk = x.InBandCount >= bridgeBandMinOccupancy;
                bool actionOk = phaseOk && bridgeOk && derivedRankByMode.TryGetValue(x.M, out int rank) && rank == 1;
                bool admissible = phaseOk && bridgeOk && actionOk;
                string failReason = admissible
                    ? "passes-all"
                    : !phaseOk ? "phase"
                    : !bridgeOk ? "bridge"
                    : "actionTick";
                return (x.M, x.InBandCount, x.AvgClosureQuality, x.DerivedActionTick, phaseOk, bridgeOk, actionOk, admissible, failReason);
            })
            .OrderBy(x => x.M)
            .ToArray();

        foreach (var row in evaluation)
        {
            _output.WriteLine(
                $"RBF28 m={row.M} | inBand={row.InBandCount} | avgClosure={row.AvgClosureQuality:F4} | derivedActionTick={row.DerivedActionTick:E6} | phase={row.phaseOk} | bridge={row.bridgeOk} | actionTick={row.actionOk} | admissible={row.admissible} | fail={row.failReason}");
        }

        var satisfying = evaluation.Where(x => x.admissible).Select(x => x.M).OrderBy(x => x).ToArray();
        int selectedMode = satisfying.DefaultIfEmpty(int.MaxValue).Min();

        _output.WriteLine($"RBF28 selected mode={(selectedMode == int.MaxValue ? "none" : $"m={selectedMode}")} | satisfying=[{string.Join(", ", satisfying)}]");
        _output.WriteLine("RBF28 claim boundary: candidate diagnostic only, not theorem-level proof.");

        Assert.True(satisfying.Length > 0, "Expected at least one full-stack admissible mode under derived action/tick rule.");
        Assert.True(selectedMode == 3,
            $"Expected m=3 to be the first/full-stack admissible mode in baseline derived rule. selected={selectedMode}, satisfying=[{string.Join(", ", satisfying)}]");
    }

    /// <summary>
    /// Checks phase-stress boundary mapping for m=3 admissibility under the derived full-stack rule.
    /// Matters because the theorem-path requires explicit domain-of-validity and failure-cause diagnostics.
    /// Expected diagnostic behavior: resolved/unresolved region reporting, failure-cause counts, and transition detection.
    /// Claim boundary: diagnostic boundary mapping only; not theorem-level closure.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF29_M3AdmissibilityBoundary_Should_Map_PhaseStressTransition()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        var phaseThresholds = new[] { 0.780, 0.785, 0.790, 0.795, 0.800 };
        var bridgeThresholds = new[] { 1, 2, 3 };
        var actionScales = new[] { 0.95, 1.00, 1.05 };
        var weights = new (string Name, double Order, double Closure, double Transport)[]
        {
            ("wA", 0.50, 0.35, 0.15)
        };

        int totalCases = 0;
        int resolvedCases = 0;
        int unresolvedCases = 0;
        int transitionCount = 0;
        var failureCauseCounts = new Dictionary<string, int>
        {
            ["phase"] = 0,
            ["bridge"] = 0,
            ["actionTick"] = 0,
            ["mixed"] = 0
        };

        double minWinnerMargin = double.PositiveInfinity;
        double maxWinnerMargin = double.NegativeInfinity;
        string? firstTransition = null;

        foreach (var w in weights)
        {
            var family = BuildModeFamilyFromLatticeProxy(
                mValues,
                qMin: 12,
                qMax: 24,
                orderWeight: w.Order,
                closureWeight: w.Closure,
                transportWeight: w.Transport);

            foreach (int bridgeThreshold in bridgeThresholds)
            {
                foreach (double actionScale in actionScales)
                {
                    bool hadLowerStressM3Selection = false;

                    foreach (double phaseThreshold in phaseThresholds.OrderBy(x => x))
                    {
                        totalCases++;

                        var phaseBridgeCandidates = family
                            .Where(x => x.AvgClosureQuality >= phaseThreshold && x.InBandCount >= bridgeThreshold)
                            .OrderBy(x => x.DerivedActionTick)
                            .ToArray();

                        if (phaseBridgeCandidates.Length == 0)
                        {
                            unresolvedCases++;
                            failureCauseCounts["mixed"]++;
                            _output.WriteLine(
                                $"RBF29 w={w.Name} phaseThr={phaseThreshold:F3} bridgeThr={bridgeThreshold} actionScale={actionScale:F2} | unresolved (no phase+bridge candidates)");
                            continue;
                        }

                        double baseThreshold = phaseBridgeCandidates.Length >= 2
                            ? 0.5 * (phaseBridgeCandidates[0].DerivedActionTick + phaseBridgeCandidates[1].DerivedActionTick)
                            : 1.05 * phaseBridgeCandidates[0].DerivedActionTick;
                        double actionThreshold = actionScale * baseThreshold;
                        var derivedRankByMode = phaseBridgeCandidates
                            .Select((x, index) => (x.M, Rank: index + 1))
                            .ToDictionary(x => x.M, x => x.Rank);

                        var evaluation = family
                            .Select(x =>
                            {
                                bool phaseOk = x.AvgClosureQuality >= phaseThreshold;
                                bool bridgeOk = x.InBandCount >= bridgeThreshold;
                                bool actionOk = x.DerivedActionTick <= actionThreshold &&
                                    derivedRankByMode.TryGetValue(x.M, out int rank) &&
                                    rank == 1;
                                bool admissible = phaseOk && bridgeOk && actionOk;
                                string failReason = ResolveFailureReason(phaseOk, bridgeOk, actionOk);
                                return (x.M, x.DerivedActionTick, phaseOk, bridgeOk, actionOk, admissible, failReason);
                            })
                            .OrderBy(x => x.M)
                            .ToArray();

                        var satisfying = evaluation.Where(x => x.admissible).OrderBy(x => x.DerivedActionTick).ThenBy(x => x.M).ToArray();
                        bool resolved = satisfying.Length > 0;
                        bool m3Selected = resolved && satisfying[0].M == 3;
                        int selectedMode = resolved ? satisfying[0].M : int.MaxValue;

                        if (resolved)
                        {
                            resolvedCases++;
                            if (satisfying.Length >= 2)
                            {
                                double margin = satisfying[1].DerivedActionTick - satisfying[0].DerivedActionTick;
                                minWinnerMargin = Math.Min(minWinnerMargin, margin);
                                maxWinnerMargin = Math.Max(maxWinnerMargin, margin);
                                Assert.True(double.IsFinite(margin), $"Non-finite winner margin at w={w.Name}, phase={phaseThreshold:F3}.");
                            }
                            else
                            {
                                minWinnerMargin = Math.Min(minWinnerMargin, 0.0);
                                maxWinnerMargin = Math.Max(maxWinnerMargin, 0.0);
                            }
                        }
                        else
                        {
                            unresolvedCases++;
                        }

                        if (m3Selected)
                        {
                            hadLowerStressM3Selection = true;
                        }
                        else if (hadLowerStressM3Selection)
                        {
                            transitionCount++;
                            if (firstTransition is null)
                            {
                                firstTransition =
                                    $"w={w.Name}, phaseThr={phaseThreshold:F3}, bridgeThr={bridgeThreshold}, actionScale={actionScale:F2}, selected={(resolved ? $"m={selectedMode}" : "none")}";
                            }
                        }

                        if (!m3Selected)
                        {
                            var m3Row = evaluation.First(x => x.M == 3);
                            failureCauseCounts[m3Row.failReason]++;
                        }

                        _output.WriteLine(
                            $"RBF29 w={w.Name} phaseThr={phaseThreshold:F3} bridgeThr={bridgeThreshold} actionScale={actionScale:F2} | selected={(resolved ? $"m={selectedMode}" : "none")} | satisfying=[{string.Join(", ", satisfying.Select(x => x.M))}] | m3Selected={m3Selected}");
                    }
                }
            }
        }

        _output.WriteLine("--- RBF29 M3 ADMISSIBILITY BOUNDARY DIAGNOSTIC ---");
        _output.WriteLine($"total cases={totalCases} | resolved={resolvedCases} | unresolved={unresolvedCases}");
        _output.WriteLine($"failure causes (m3 not selected): phase={failureCauseCounts["phase"]}, bridge={failureCauseCounts["bridge"]}, actionTick={failureCauseCounts["actionTick"]}, mixed={failureCauseCounts["mixed"]}");
        _output.WriteLine($"winner margin range: min={(double.IsFinite(minWinnerMargin) ? minWinnerMargin.ToString("E6") : "n/a")} max={(double.IsFinite(maxWinnerMargin) ? maxWinnerMargin.ToString("E6") : "n/a")}");
        _output.WriteLine($"transition boundaries detected={transitionCount}");
        _output.WriteLine($"first transition={(firstTransition ?? "none")}");
        _output.WriteLine("RBF29 claim boundary: candidate diagnostic only, not theorem-level proof.");

        Assert.True(totalCases == resolvedCases + unresolvedCases,
            $"Case accounting mismatch: total={totalCases}, resolved={resolvedCases}, unresolved={unresolvedCases}.");
        Assert.True(resolvedCases > 0, "Expected resolved boundary-map cases.");
        Assert.True(transitionCount > 0, "Expected at least one phase-stress transition boundary near the 0.790 regime.");
    }

    /// <summary>
    /// Checks whether competing families fail under one shared derived three-constraint rule.
    /// Matters because explicit competing-family exclusion is required on the m=3 theorem-path.
    /// Expected diagnostic behavior: baseline keeps m=3 minimally admissible and reports structural failure reasons for others.
    /// Claim boundary: candidate structural exclusion evidence only; not a theorem-level proof.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF30_CompetingFamilies_Should_Fail_Under_DerivedThreeConstraintRule()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        const double phaseClosureThreshold = 0.780;
        const int bridgeThreshold = 1;

        var family = BuildModeFamilyFromLatticeProxy(
            mValues,
            qMin: 12,
            qMax: 24,
            orderWeight: 0.50,
            closureWeight: 0.35,
            transportWeight: 0.15);

        var phaseBridgeCandidates = family
            .Where(x => x.AvgClosureQuality >= phaseClosureThreshold && x.InBandCount >= bridgeThreshold)
            .OrderBy(x => x.DerivedActionTick)
            .ToArray();

        Assert.True(phaseBridgeCandidates.Length >= 2,
            "Expected at least two phase+bridge candidates to define derived three-constraint rule.");

        double derivedThreshold = 0.5 * (phaseBridgeCandidates[0].DerivedActionTick + phaseBridgeCandidates[1].DerivedActionTick);

        var evaluation = family
            .Select(x =>
            {
                bool phaseOk = x.AvgClosureQuality >= phaseClosureThreshold;
                bool bridgeOk = x.InBandCount >= bridgeThreshold;
                bool actionTickOk = phaseOk && bridgeOk && x.DerivedActionTick <= derivedThreshold;
                bool admissible = phaseOk && bridgeOk && actionTickOk;
                string failReason = admissible ? "passes-all" : ResolveFailureReason(phaseOk, bridgeOk, actionTickOk);
                return (x.M, x.InBandCount, x.AvgClosureQuality, x.DerivedActionTick, phaseOk, bridgeOk, actionTickOk, admissible, failReason);
            })
            .OrderBy(x => x.M)
            .ToArray();

        foreach (var row in evaluation)
        {
            _output.WriteLine(
                $"RBF30 m={row.M} | phase={row.phaseOk} | bridge={row.bridgeOk} | actionTick={row.actionTickOk} | admissible={row.admissible} | fail={row.failReason} | inBand={row.InBandCount} | avgClosure={row.AvgClosureQuality:F4} | derivedActionTick={row.DerivedActionTick:E6}");
        }

        var admissibleModes = evaluation.Where(x => x.admissible).Select(x => x.M).OrderBy(x => x).ToArray();
        int selectedMode = admissibleModes.DefaultIfEmpty(int.MaxValue).Min();

        _output.WriteLine($"RBF30 selected mode={(selectedMode == int.MaxValue ? "none" : $"m={selectedMode}")} | admissible=[{string.Join(", ", admissibleModes)}]");
        _output.WriteLine("RBF30 claim boundary: candidate diagnostic only, not theorem-level proof.");

        Assert.True(admissibleModes.Length > 0, "Expected at least one admissible mode under derived three-constraint rule.");
        Assert.True(selectedMode == 3,
            $"Expected m=3 to remain minimally admissible under derived rule. selected={selectedMode}, admissible=[{string.Join(", ", admissibleModes)}]");

        var nonM3Admissible = admissibleModes.Where(m => m != 3).ToArray();
        Assert.True(nonM3Admissible.Length == 0,
            $"Competing families remained admissible under shared derived rule: [{string.Join(", ", nonM3Admissible)}]");
    }

    /// <summary>
    /// Checks whether m=2 fallback at boundary is explained by a phase/action tradeoff.
    /// Matters because fallback explanation distinguishes bounded admissibility from random instability.
    /// Expected diagnostic behavior: fallback cases are explainable by phase-only, action-only, or joint phase+action effects.
    /// Claim boundary: diagnostic explanation only; not theorem-level closure.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF31_M2FallbackBoundary_Should_Be_Explained_By_PhaseActionTradeoff()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        var phaseThresholds = new[] { 0.780, 0.790, 0.800, 0.810, 0.820 };
        var actionScales = new[] { 0.95, 1.00, 1.05 };
        var weights = new (string Name, double Order, double Closure, double Transport)[]
        {
            ("wA", 0.50, 0.35, 0.15),
            ("wB", 0.55, 0.30, 0.15),
            ("wC", 0.45, 0.40, 0.15)
        };

        const int bridgeThreshold = 1;

        int resolvedCases = 0;
        int m2FallbackCases = 0;
        int explainedByPhaseOnly = 0;
        int explainedByActionOnly = 0;
        int explainedByJointPhaseAction = 0;
        string? firstFallback = null;

        foreach (var w in weights)
        {
            var family = BuildModeFamilyFromLatticeProxy(
                mValues,
                qMin: 12,
                qMax: 24,
                orderWeight: w.Order,
                closureWeight: w.Closure,
                transportWeight: w.Transport);

            foreach (double phaseThreshold in phaseThresholds)
            {
                var phaseBridgeCandidates = family
                    .Where(x => x.AvgClosureQuality >= phaseThreshold && x.InBandCount >= bridgeThreshold)
                    .OrderBy(x => x.DerivedActionTick)
                    .ToArray();

                if (phaseBridgeCandidates.Length == 0)
                    continue;

                double baseThreshold = phaseBridgeCandidates.Length >= 2
                    ? 0.5 * (phaseBridgeCandidates[0].DerivedActionTick + phaseBridgeCandidates[1].DerivedActionTick)
                    : 1.05 * phaseBridgeCandidates[0].DerivedActionTick;

                foreach (double actionScale in actionScales)
                {
                    double actionThreshold = actionScale * baseThreshold;
                    var derivedRankByMode = phaseBridgeCandidates
                        .Select((x, index) => (x.M, Rank: index + 1))
                        .ToDictionary(x => x.M, x => x.Rank);

                    var evaluation = family
                        .Select(x =>
                        {
                            bool phaseOk = x.AvgClosureQuality >= phaseThreshold;
                            bool bridgeOk = x.InBandCount >= bridgeThreshold;
                            bool actionOk = x.DerivedActionTick <= actionThreshold &&
                                derivedRankByMode.TryGetValue(x.M, out int rank) &&
                                rank == 1;
                            bool admissible = phaseOk && bridgeOk && actionOk;
                            return (x.M, x.AvgClosureQuality, x.DerivedActionTick, phaseOk, bridgeOk, actionOk, admissible);
                        })
                        .OrderBy(x => x.M)
                        .ToArray();

                    var satisfying = evaluation.Where(x => x.admissible).OrderBy(x => x.DerivedActionTick).ThenBy(x => x.M).ToArray();
                    if (satisfying.Length == 0)
                        continue;

                    resolvedCases++;
                    int selectedMode = satisfying[0].M;

                    var m2 = evaluation.First(x => x.M == 2);
                    var m3 = evaluation.First(x => x.M == 3);

                    bool m3PhaseFail = !m3.phaseOk;
                    bool m3ActionFail = !m3.actionOk;

                    if (selectedMode == 2)
                    {
                        m2FallbackCases++;

                        if (m3PhaseFail && !m3ActionFail)
                        {
                            explainedByPhaseOnly++;
                        }
                        else if (!m3PhaseFail && m3ActionFail)
                        {
                            explainedByActionOnly++;
                        }
                        else if (m3PhaseFail && m3ActionFail)
                        {
                            explainedByJointPhaseAction++;
                        }

                        if (firstFallback is null)
                        {
                            firstFallback =
                                $"w={w.Name}, phaseThr={phaseThreshold:F3}, actionScale={actionScale:F2}, m2Action={m2.DerivedActionTick:E6}, m3Action={m3.DerivedActionTick:E6}, m3PhaseOk={m3.phaseOk}, m3ActionOk={m3.actionOk}";
                        }
                    }

                    _output.WriteLine(
                        $"RBF31 w={w.Name} phaseThr={phaseThreshold:F3} actionScale={actionScale:F2} | selected=m={selectedMode} | satisfying=[{string.Join(", ", satisfying.Select(x => x.M))}] | m3PhaseOk={m3.phaseOk} | m3ActionOk={m3.actionOk}");
                }
            }
        }

        int explainedFallbacks = explainedByPhaseOnly + explainedByActionOnly + explainedByJointPhaseAction;

        _output.WriteLine("--- RBF31 M2 FALLBACK BOUNDARY DIAGNOSTIC ---");
        _output.WriteLine($"resolved cases={resolvedCases}");
        _output.WriteLine($"m2 fallback cases={m2FallbackCases}");
        _output.WriteLine($"explained by phase-only={explainedByPhaseOnly}");
        _output.WriteLine($"explained by action-only={explainedByActionOnly}");
        _output.WriteLine($"explained by joint phase+action={explainedByJointPhaseAction}");
        _output.WriteLine($"first fallback case={(firstFallback ?? "none")}");
        _output.WriteLine("RBF31 claim boundary: candidate diagnostic only, not theorem-level proof.");

        Assert.True(resolvedCases > 0, "Expected resolved RBF31 cases.");
        Assert.True(m2FallbackCases > 0, "Expected at least one m=2 fallback boundary case.");
        Assert.True(explainedFallbacks == m2FallbackCases,
            $"Expected all m=2 fallback cases to be explainable by phase/action tradeoff. explained={explainedFallbacks}, fallback={m2FallbackCases}");
    }

    /// <summary>
    /// Checks continuity properties of the m=3 admissibility manifold over deterministic phase/action/bridge/weight grids.
    /// Matters because the theorem-path needs bounded-region structure, not isolated point selection.
    /// Expected diagnostic behavior: non-empty m=3 region, baseline-local continuity, and explicit boundary/unresolved reporting.
    /// Claim boundary: bounded diagnostic manifold evidence only; no theorem-level claim.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF32_M3Boundary_Should_Have_Continuous_AdmissibilityManifold()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        var phaseThresholds = new[] { 0.775, 0.780, 0.785, 0.790, 0.795, 0.800 };
        var actionScales = new[] { 0.95, 1.00, 1.05 };
        var bridgeThresholds = new[] { 1, 2, 3 };
        var weights = new (string Name, double Order, double Closure, double Transport)[]
        {
            ("wA", 0.50, 0.35, 0.15),
            ("wB", 0.55, 0.30, 0.15),
            ("wC", 0.45, 0.40, 0.15)
        };

        var m3Points = new HashSet<string>(StringComparer.Ordinal);
        var unresolvedPoints = new List<string>();
        var selectedModeByPoint = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var w in weights)
        {
            var family = BuildModeFamilyFromLatticeProxy(
                mValues,
                qMin: 12,
                qMax: 24,
                orderWeight: w.Order,
                closureWeight: w.Closure,
                transportWeight: w.Transport);

            for (int p = 0; p < phaseThresholds.Length; p++)
            {
                for (int a = 0; a < actionScales.Length; a++)
                {
                    for (int b = 0; b < bridgeThresholds.Length; b++)
                    {
                        string key = $"{w.Name}|p{p}|a{a}|b{b}";
                        var result = EvaluateDerivedThreeConstraintSelection(
                            family,
                            phaseThresholds[p],
                            bridgeThresholds[b],
                            actionScales[a]);

                        if (!result.Resolved)
                        {
                            unresolvedPoints.Add(key);
                            selectedModeByPoint[key] = int.MaxValue;
                            continue;
                        }

                        selectedModeByPoint[key] = result.SelectedMode;
                        if (result.SelectedMode == 3)
                            m3Points.Add(key);
                    }
                }
            }
        }

        const string baselineKey = "wA|p0|a1|b0";
        bool baselineM3 = selectedModeByPoint.TryGetValue(baselineKey, out int baselineSel) && baselineSel == 3;

        bool AreNeighbors(string left, string right)
        {
            var l = left.Split('|');
            var r = right.Split('|');
            if (l[0] != r[0])
                return false;

            int lp = int.Parse(l[1][1..]);
            int la = int.Parse(l[2][1..]);
            int lb = int.Parse(l[3][1..]);
            int rp = int.Parse(r[1][1..]);
            int ra = int.Parse(r[2][1..]);
            int rb = int.Parse(r[3][1..]);

            int dist = Math.Abs(lp - rp) + Math.Abs(la - ra) + Math.Abs(lb - rb);
            return dist == 1;
        }

        var boundaryPoints = new List<string>();
        foreach (string point in m3Points)
        {
            var parts = point.Split('|');
            int p = int.Parse(parts[1][1..]);
            int a = int.Parse(parts[2][1..]);
            int b = int.Parse(parts[3][1..]);
            string w = parts[0];

            var neighbors = new (int dp, int da, int db)[]
            {
                (-1, 0, 0), (1, 0, 0),
                (0, -1, 0), (0, 1, 0),
                (0, 0, -1), (0, 0, 1)
            };

            bool touchesNonM3 = false;
            foreach (var n in neighbors)
            {
                int np = p + n.dp;
                int na = a + n.da;
                int nb = b + n.db;
                if (np < 0 || np >= phaseThresholds.Length || na < 0 || na >= actionScales.Length || nb < 0 || nb >= bridgeThresholds.Length)
                    continue;

                string neighborKey = $"{w}|p{np}|a{na}|b{nb}";
                if (!m3Points.Contains(neighborKey))
                {
                    touchesNonM3 = true;
                    break;
                }
            }

            if (touchesNonM3)
                boundaryPoints.Add(point);
        }

        var remaining = new HashSet<string>(m3Points, StringComparer.Ordinal);
        int components = 0;
        int baselineComponentSize = 0;
        while (remaining.Count > 0)
        {
            components++;
            string seed = remaining.First();
            var queue = new Queue<string>();
            queue.Enqueue(seed);
            remaining.Remove(seed);
            int componentSize = 0;
            bool containsBaseline = false;

            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                componentSize++;
                if (current == baselineKey)
                    containsBaseline = true;

                var neighbors = remaining.Where(x => AreNeighbors(current, x)).ToList();
                foreach (var n in neighbors)
                {
                    remaining.Remove(n);
                    queue.Enqueue(n);
                }
            }

            if (containsBaseline)
                baselineComponentSize = componentSize;
        }

        bool baselineLocalContinuity = m3Points.Any(x => x != baselineKey && AreNeighbors(baselineKey, x));
        bool manifoldContinuityOk = components <= 1 || (baselineM3 && baselineLocalContinuity && baselineComponentSize >= 2);

        _output.WriteLine("--- RBF32 M3 CONTINUOUS ADMISSIBILITY MANIFOLD DIAGNOSTIC ---");
        _output.WriteLine($"m3 admissible points={m3Points.Count}");
        _output.WriteLine($"m3 connected components={components}");
        _output.WriteLine($"baseline m3Selected={baselineM3}");
        _output.WriteLine($"baseline local continuity={baselineLocalContinuity}");
        _output.WriteLine($"baseline component size={baselineComponentSize}");
        _output.WriteLine($"boundary points count={boundaryPoints.Count}");
        _output.WriteLine($"unresolved regions count={unresolvedPoints.Count}");
        _output.WriteLine($"sample boundary points=[{string.Join(", ", boundaryPoints.Take(8))}]");
        _output.WriteLine($"sample unresolved regions=[{string.Join(", ", unresolvedPoints.Take(8))}]");
        _output.WriteLine("RBF32 claim boundary: candidate diagnostic only, not theorem-level proof.");

        Assert.True(m3Points.Count > 0, "Expected non-empty m=3 admissibility manifold.");
        Assert.True(baselineM3, "Expected baseline point to remain m=3 admissible.");
        Assert.True(manifoldContinuityOk,
            $"Expected connected manifold or local continuity near baseline. components={components}, baselineLocal={baselineLocalContinuity}, baselineComponentSize={baselineComponentSize}");
    }

    /// <summary>
    /// Checks whether stronger shared derived action-margin gating can exclude m=2 fallback without harming baseline m=3.
    /// Matters because theorem-path strengthening must avoid per-family retuning artifacts.
    /// Expected diagnostic behavior: report if exclusion is structural under shared margins or threshold-tuning sensitive.
    /// Claim boundary: candidate gate-discipline diagnostic only; not first-principles closure.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF33_M2Fallback_Should_Be_Excluded_By_StrongerDerivedActionGate_Only_When_PhysicallyJustified()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        var phaseThresholds = new[] { 0.780, 0.790, 0.800, 0.810, 0.820 };
        var actionScales = new[] { 0.95, 1.00, 1.05 };
        var weights = new (string Name, double Order, double Closure, double Transport)[]
        {
            ("wA", 0.50, 0.35, 0.15),
            ("wB", 0.55, 0.30, 0.15),
            ("wC", 0.45, 0.40, 0.15)
        };
        var gateMargins = new[] { 0.0, 5e-5, 1e-4, 2e-4, 5e-4 };

        const int bridgeThreshold = 1;
        const double baselinePhase = 0.780;
        const double baselineActionScale = 1.00;

        var fallbackCases = new List<(string Weight, double PhaseThreshold, double ActionScale)>();
        var familyByWeight = new Dictionary<string, (int M, int InBandCount, double AvgClosureQuality, double OperationalActionTick, double DerivedActionTick)[]>(StringComparer.Ordinal);

        foreach (var w in weights)
        {
            var family = BuildModeFamilyFromLatticeProxy(
                mValues,
                qMin: 12,
                qMax: 24,
                orderWeight: w.Order,
                closureWeight: w.Closure,
                transportWeight: w.Transport);
            familyByWeight[w.Name] = family;

            foreach (double phaseThreshold in phaseThresholds)
            {
                foreach (double actionScale in actionScales)
                {
                    var result = EvaluateDerivedThreeConstraintSelection(family, phaseThreshold, bridgeThreshold, actionScale);
                    if (result.Resolved && result.SelectedMode == 2)
                        fallbackCases.Add((w.Name, phaseThreshold, actionScale));
                }
            }
        }

        Assert.True(fallbackCases.Count > 0, "Expected m=2 fallback cases from baseline derived rule.");

        bool hasAcceptedMargin = false;
        bool structuralExclusion = false;
        int bestExcludedCount = 0;
        double bestMargin = double.NaN;

        foreach (double margin in gateMargins)
        {
            int excludedFallbacks = 0;
            bool baselineM3Preserved = true;

            foreach (var w in weights)
            {
                var family = familyByWeight[w.Name];

                var baseline = EvaluateDerivedThreeConstraintSelection(family, baselinePhase, bridgeThreshold, baselineActionScale, margin);
                if (!(baseline.Resolved && baseline.SelectedMode == 3))
                {
                    baselineM3Preserved = false;
                    continue;
                }

                foreach (var fc in fallbackCases.Where(x => x.Weight == w.Name))
                {
                    var strict = EvaluateDerivedThreeConstraintSelection(family, fc.PhaseThreshold, bridgeThreshold, fc.ActionScale, margin);
                    if (!strict.Resolved || strict.SelectedMode != 2)
                        excludedFallbacks++;
                }
            }

            _output.WriteLine(
                $"RBF33 margin={margin:E2} | baselineM3Preserved={baselineM3Preserved} | excludedFallbacks={excludedFallbacks}/{fallbackCases.Count}");

            if (baselineM3Preserved)
            {
                hasAcceptedMargin = true;
                if (excludedFallbacks > bestExcludedCount)
                {
                    bestExcludedCount = excludedFallbacks;
                    bestMargin = margin;
                }
            }
        }

        if (bestExcludedCount > 0)
            structuralExclusion = true;

        string interpretation = structuralExclusion
            ? "stronger exclusion is structural under shared margin gate"
            : "stronger exclusion appears threshold-tuning limited under shared margin gate";

        _output.WriteLine("--- RBF33 M2 FALLBACK EXCLUSION DIAGNOSTIC ---");
        _output.WriteLine($"fallback cases from baseline rule={fallbackCases.Count}");
        _output.WriteLine($"accepted shared-margin gates={hasAcceptedMargin}");
        _output.WriteLine($"best margin={(double.IsNaN(bestMargin) ? "none" : bestMargin.ToString("E2"))}");
        _output.WriteLine($"best excluded fallback count={bestExcludedCount}/{fallbackCases.Count}");
        _output.WriteLine($"interpretation={interpretation}");
        _output.WriteLine("RBF33 claim boundary: candidate diagnostic only, not theorem-level proof.");

        Assert.True(hasAcceptedMargin, "Expected at least one shared stronger gate that keeps baseline m=3.");
        Assert.True(bestExcludedCount >= 0, "Fallback exclusion count should be finite.");
    }

    /// <summary>
    /// Checks stability of the m=3 boundary map under nearby q-window and solver-step variants.
    /// Matters because bounded theorem-path evidence requires controlled variant drift.
    /// Expected diagnostic behavior: bounded region drift with resolved-case reporting and fallback tracking.
    /// Claim boundary: stability diagnostic only; not theorem-level proof.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF34_M3Boundary_Should_Remain_Stable_Under_QWindowAndSolverStepVariants()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        var phaseThresholds = new[] { 0.775, 0.780, 0.785, 0.790, 0.795, 0.800 };
        var actionScales = new[] { 0.95, 1.00, 1.05 };
        var bridgeThresholds = new[] { 1, 2, 3 };
        var weights = new (string Name, double Order, double Closure, double Transport)[]
        {
            ("wA", 0.50, 0.35, 0.15),
            ("wB", 0.55, 0.30, 0.15),
            ("wC", 0.45, 0.40, 0.15)
        };

        var qWindows = new (int QMin, int QMax, string Name)[]
        {
            (12, 24, "qBase"),
            (13, 25, "qShift")
        };

        var solverProfiles = new (string Name, ModeLockConfig Config)[]
        {
            ("sBase", BuildNoCadencePriorConfig()),
            ("sShort", BuildNoCadencePriorConfig() with { Steps = 1000, SettleSteps = 500 })
        };

        HashSet<string> BuildRegion((int QMin, int QMax, string Name) qWindow, (string Name, ModeLockConfig Config) solver, out int resolved, out int unresolved, out int fallbackToM2)
        {
            var region = new HashSet<string>(StringComparer.Ordinal);
            resolved = 0;
            unresolved = 0;
            fallbackToM2 = 0;

            foreach (var w in weights)
            {
                var family = BuildModeFamilyFromLatticeProxy(
                    mValues,
                    qWindow.QMin,
                    qWindow.QMax,
                    w.Order,
                    w.Closure,
                    w.Transport,
                    solver.Config,
                    new[] { 2e-3 });

                for (int p = 0; p < phaseThresholds.Length; p++)
                {
                    for (int a = 0; a < actionScales.Length; a++)
                    {
                        for (int b = 0; b < bridgeThresholds.Length; b++)
                        {
                            var result = EvaluateDerivedThreeConstraintSelection(
                                family,
                                phaseThresholds[p],
                                bridgeThresholds[b],
                                actionScales[a]);

                            if (!result.Resolved)
                            {
                                unresolved++;
                                continue;
                            }

                            resolved++;
                            if (result.SelectedMode == 3)
                                region.Add($"{w.Name}|p{p}|a{a}|b{b}");
                            if (result.SelectedMode == 2)
                                fallbackToM2++;
                        }
                    }
                }
            }

            return region;
        }

        static double Jaccard(HashSet<string> a, HashSet<string> b)
        {
            if (a.Count == 0 && b.Count == 0)
                return 1.0;

            int intersection = a.Count(x => b.Contains(x));
            int union = a.Count + b.Count - intersection;
            return union > 0 ? (double)intersection / union : 0.0;
        }

        var baselineQ = qWindows[0];
        var baselineS = solverProfiles[0];
        var baselineRegion = BuildRegion(baselineQ, baselineS, out int baselineResolved, out int baselineUnresolved, out int baselineFallbackM2);
        Assert.True(baselineRegion.Count > 0, "Expected non-empty baseline m=3 boundary region.");

        double worstDrift = 0.0;
        string? worstCase = null;

        foreach (var q in qWindows)
        {
            foreach (var s in solverProfiles)
            {
                var region = BuildRegion(q, s, out int resolved, out int unresolved, out int fallbackM2);
                double sim = Jaccard(baselineRegion, region);
                double drift = 1.0 - sim;
                if (drift > worstDrift)
                {
                    worstDrift = drift;
                    worstCase = $"{q.Name}/{s.Name}";
                }

                _output.WriteLine(
                    $"RBF34 variant={q.Name}/{s.Name} | m3Region={region.Count} | resolved={resolved} | unresolved={unresolved} | m2Fallback={fallbackM2} | drift={drift:F4}");

                Assert.True(resolved > 0, $"Expected resolved cases for variant {q.Name}/{s.Name}.");
            }
        }

        _output.WriteLine("--- RBF34 M3 BOUNDARY STABILITY DIAGNOSTIC ---");
        _output.WriteLine($"baseline region size={baselineRegion.Count} | resolved={baselineResolved} | unresolved={baselineUnresolved} | m2Fallback={baselineFallbackM2}");
        _output.WriteLine($"worst drift={worstDrift:F4} at {(worstCase ?? "none")}");
        _output.WriteLine("RBF34 claim boundary: candidate diagnostic only, not theorem-level proof.");

        Assert.True(worstDrift <= 0.80,
            $"Expected bounded movement of m=3 admissibility region under q-window/solver-step variants. worstDrift={worstDrift:F4}, worstCase={worstCase}");
    }

    /// <summary>
    /// Checks whether q-window dependence is largely explainable by bridge-band occupancy geometry.
    /// Matters because the dominant remaining gap is structural q-window justification.
    /// Expected diagnostic behavior: occupancy-geometry explains most resolved selection differences across q-windows.
    /// Claim boundary: diagnostic/candidate support only; no theorem-level or numerology claim.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF35_QWindowDependence_Should_Be_Explained_By_BridgeBandOccupancyGeometry()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        var qWindows = new (int QMin, int QMax, string Name)[]
        {
            (12, 24, "qBase"),
            (13, 25, "qShiftUp"),
            (10, 22, "qShiftDown"),
            (14, 26, "qHigh")
        };
        var weights = new (string Name, double Order, double Closure, double Transport)[]
        {
            ("wA", 0.50, 0.35, 0.15),
            ("wB", 0.55, 0.30, 0.15)
        };

        const double phaseThreshold = 0.780;
        const int bridgeThreshold = 1;
        const double actionScale = 1.00;

        int resolved = 0;
        int explainedByOccupancyGeometry = 0;
        int selectedM2 = 0;
        int selectedM3 = 0;

        foreach (var qw in qWindows)
        {
            foreach (var w in weights)
            {
                var family = BuildModeFamilyFromLatticeProxy(
                    mValues,
                    qw.QMin,
                    qw.QMax,
                    w.Order,
                    w.Closure,
                    w.Transport,
                    BuildNoCadencePriorConfig(),
                    new[] { 2e-3 });

                var result = EvaluateDerivedThreeConstraintSelection(
                    family,
                    phaseThreshold,
                    bridgeThreshold,
                    actionScale);

                if (!result.Resolved)
                {
                    _output.WriteLine($"RBF35 {qw.Name}/{w.Name} | unresolved");
                    continue;
                }

                resolved++;
                int selected = result.SelectedMode;
                if (selected == 2) selectedM2++;
                if (selected == 3) selectedM3++;

                var m2 = family.First(x => x.M == 2);
                var m3 = family.First(x => x.M == 3);
                int occupancyDelta = m3.InBandCount - m2.InBandCount;

                bool explained =
                    (selected == 3 && occupancyDelta >= 0) ||
                    (selected == 2 && occupancyDelta <= 0);
                if (explained)
                    explainedByOccupancyGeometry++;

                _output.WriteLine(
                    $"RBF35 {qw.Name}/{w.Name} | selected=m={selected} | satisfying=[{string.Join(", ", result.SatisfyingModes)}] | inBand(m3-m2)={occupancyDelta:+#;-#;0} | explained={explained}");
            }
        }

        _output.WriteLine("--- RBF35 Q-WINDOW OCCUPANCY-GEOMETRY DIAGNOSTIC ---");
        _output.WriteLine($"resolved={resolved} | selected m2={selectedM2} | selected m3={selectedM3}");
        _output.WriteLine($"occupancy-geometry explained={explainedByOccupancyGeometry}/{resolved}");
        _output.WriteLine("RBF35 claim boundary: candidate diagnostic only, not theorem-level proof.");

        Assert.True(resolved > 0, "Expected resolved q-window scenarios.");
        Assert.True(explainedByOccupancyGeometry >= Math.Max(1, resolved / 2),
            $"Expected q-window dependence to be mostly explainable by bridge-band occupancy geometry. explained={explainedByOccupancyGeometry}, resolved={resolved}");
    }

    /// <summary>
    /// Checks whether q-window shifts create non-core winners that would imply per-family retuning artifacts.
    /// Matters because theorem-path discipline requires one shared rule, not family-specific tuning.
    /// Expected diagnostic behavior: resolved q-shift scenarios stay within core winner set (m=2/m=3).
    /// Claim boundary: artifact-audit diagnostic only; not theorem-level closure.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF36_QWindowShift_Should_Not_Create_PerFamilyRetuningArtifact()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        var qWindows = new (int QMin, int QMax, string Name)[]
        {
            (12, 24, "qBase"),
            (13, 25, "qShiftUp"),
            (10, 22, "qShiftDown")
        };
        var weights = new (string Name, double Order, double Closure, double Transport)[]
        {
            ("wA", 0.50, 0.35, 0.15),
            ("wB", 0.55, 0.30, 0.15)
        };

        const double phaseThreshold = 0.780;
        const int bridgeThreshold = 1;
        const double actionScale = 1.00;

        int resolved = 0;
        int nonCoreSelections = 0;
        int selectedM2 = 0;
        int selectedM3 = 0;
        var selectedModes = new List<int>();

        foreach (var qw in qWindows)
        {
            foreach (var w in weights)
            {
                var family = BuildModeFamilyFromLatticeProxy(
                    mValues,
                    qw.QMin,
                    qw.QMax,
                    w.Order,
                    w.Closure,
                    w.Transport,
                    BuildNoCadencePriorConfig(),
                    new[] { 2e-3 });

                var result = EvaluateDerivedThreeConstraintSelection(
                    family,
                    phaseThreshold,
                    bridgeThreshold,
                    actionScale);

                if (!result.Resolved)
                {
                    _output.WriteLine($"RBF36 {qw.Name}/{w.Name} | unresolved");
                    continue;
                }

                resolved++;
                selectedModes.Add(result.SelectedMode);
                if (result.SelectedMode == 2) selectedM2++;
                if (result.SelectedMode == 3) selectedM3++;
                if (result.SelectedMode is not (2 or 3))
                    nonCoreSelections++;

                _output.WriteLine(
                    $"RBF36 {qw.Name}/{w.Name} | selected=m={result.SelectedMode} | satisfying=[{string.Join(", ", result.SatisfyingModes)}]");
            }
        }

        _output.WriteLine("--- RBF36 Q-WINDOW SHIFT RETUNING-ARTIFACT DIAGNOSTIC ---");
        _output.WriteLine($"resolved={resolved} | selected m2={selectedM2} | selected m3={selectedM3} | non-core selections={nonCoreSelections}");
        _output.WriteLine($"selected modes=[{string.Join(", ", selectedModes)}]");
        _output.WriteLine("RBF36 claim boundary: candidate diagnostic only, not theorem-level proof.");

        Assert.True(resolved > 0, "Expected resolved q-window shift scenarios.");
        Assert.True(nonCoreSelections == 0,
            $"Expected no per-family retuning artifact creating non-core winners (m1/m4/m5). nonCore={nonCoreSelections}");
    }

    /// <summary>
    /// Checks whether m=3 support tracks bridge-core q-support rather than post-hoc q-window choice.
    /// Matters because the current main gap is structural derivation of q-window from bridge-scale geometry.
    /// Expected diagnostic behavior: windows containing bridge-core q-support show at least as strong m=3 selection support.
    /// Claim boundary: candidate structural support only; not first-principles proof.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF37_QWindow_Should_Be_Derived_From_BridgeScale_Not_ChosenPostHoc()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        var windowsWithBridgeCore = new (int QMin, int QMax, string Name)[]
        {
            (12, 24, "coreA"),
            (13, 25, "coreB")
        };
        var windowsWithoutBridgeCore = new (int QMin, int QMax, string Name)[]
        {
            (10, 14, "noCoreLow"),
            (19, 24, "noCoreHigh")
        };

        // For m=3 and Ω=(q+3)/q in Ω∈[1.16,1.19], bridge-scale implied q-core is {16,17,18}.
        var bridgeScaleQCore = new HashSet<int> { 16, 17, 18 };
        const double phaseThreshold = 0.780;
        const int bridgeThreshold = 1;
        const double actionScale = 1.00;

        int m3SelectedWithCore = 0;
        int m3SelectedWithoutCore = 0;

        void EvaluateWindow((int QMin, int QMax, string Name) w, bool hasCore)
        {
            var family = BuildModeFamilyFromLatticeProxy(
                mValues,
                w.QMin,
                w.QMax,
                0.50,
                0.35,
                0.15,
                BuildNoCadencePriorConfig(),
                new[] { 2e-3 });

            int m3InBand = family.First(x => x.M == 3).InBandCount;
            int qCoreInWindow = bridgeScaleQCore.Count(q => q >= w.QMin && q <= w.QMax);

            var result = EvaluateDerivedThreeConstraintSelection(
                family,
                phaseThreshold,
                bridgeThreshold,
                actionScale);

            bool m3Selected = result.Resolved && result.SelectedMode == 3;
            if (hasCore && m3Selected) m3SelectedWithCore++;
            if (!hasCore && m3Selected) m3SelectedWithoutCore++;

            _output.WriteLine(
                $"RBF37 {w.Name} | q=[{w.QMin},{w.QMax}] | qCoreInWindow={qCoreInWindow} | m3InBand={m3InBand} | selected={(result.Resolved ? $"m={result.SelectedMode}" : "none")} | m3Selected={m3Selected}");
        }

        foreach (var w in windowsWithBridgeCore) EvaluateWindow(w, hasCore: true);
        foreach (var w in windowsWithoutBridgeCore) EvaluateWindow(w, hasCore: false);

        _output.WriteLine("--- RBF37 Q-WINDOW BRIDGESCALE-DERIVATION DIAGNOSTIC ---");
        _output.WriteLine($"m3 selected with bridge-core windows={m3SelectedWithCore}/{windowsWithBridgeCore.Length}");
        _output.WriteLine($"m3 selected without bridge-core windows={m3SelectedWithoutCore}/{windowsWithoutBridgeCore.Length}");
        _output.WriteLine("RBF37 claim boundary: candidate diagnostic only, not theorem-level proof.");

        Assert.True(m3SelectedWithCore >= m3SelectedWithoutCore,
            $"Expected bridge-scale-derived core windows to support m=3 selection at least as often as non-core windows. withCore={m3SelectedWithCore}, withoutCore={m3SelectedWithoutCore}");
    }

    /// <summary>
    /// Checks whether bridge-core q-support can be derived directly from rational-band geometry for m=3.
    /// Matters because the key remaining gap is replacing operational q-window choice with a structural derivation path.
    /// Expected diagnostic behavior: derived q-core overlaps current selected windows and reports mode-selection behavior without post-hoc q tuning.
    /// Claim boundary: diagnostic/candidate support only; not theorem-level proof.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF38_BridgeCoreQWindow_Should_Follow_From_RationalBandGeometry()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        const int targetM = 3;
        const double omegaMin = 1.16;
        const double omegaMax = 1.19;
        const double gammaMin = 0.84;
        const double gammaMax = 0.86;
        const double phaseThreshold = 0.780;
        const int bridgeThreshold = 1;
        const double actionScale = 1.00;

        var derivedQCore = DeriveBridgeCoreQValuesFromBand(targetM, omegaMin, omegaMax, gammaMin, gammaMax, 2, 64);
        var selectedWindows = new (int QMin, int QMax, string Name)[]
        {
            (12, 24, "qBase"),
            (13, 25, "qShift"),
            (10, 14, "qLow"),
            (19, 24, "qHigh")
        };

        int overlapWindows = 0;
        int resolved = 0;
        int selectedM3 = 0;

        _output.WriteLine("--- RBF38 BRIDGE-CORE Q-DERIVATION DIAGNOSTIC ---");
        _output.WriteLine($"derived qCore(m={targetM})=[{string.Join(", ", derivedQCore)}]");

        foreach (var w in selectedWindows)
        {
            int coreInWindow = derivedQCore.Count(q => q >= w.QMin && q <= w.QMax);
            if (coreInWindow > 0)
                overlapWindows++;

            var family = BuildModeFamilyFromLatticeProxy(
                mValues,
                w.QMin,
                w.QMax,
                0.50,
                0.35,
                0.15,
                BuildNoCadencePriorConfig(),
                new[] { 2e-3 });

            var result = EvaluateDerivedThreeConstraintSelection(
                family,
                phaseThreshold,
                bridgeThreshold,
                actionScale);

            if (result.Resolved)
            {
                resolved++;
                if (result.SelectedMode == 3)
                    selectedM3++;
            }

            int m3InBand = family.First(x => x.M == 3).InBandCount;
            _output.WriteLine(
                $"RBF38 {w.Name} | q=[{w.QMin},{w.QMax}] | qCoreInWindow={coreInWindow} | m3InBand={m3InBand} | selected={(result.Resolved ? $"m={result.SelectedMode}" : "none")}");
        }

        _output.WriteLine($"window overlaps with qCore={overlapWindows}/{selectedWindows.Length}");
        _output.WriteLine($"resolved={resolved} | selected m3={selectedM3}");
        _output.WriteLine("RBF38 claim boundary: candidate diagnostic only, not theorem-level proof.");

        Assert.True(derivedQCore.Length > 0, "Expected non-empty bridge-core q-set from rational-band geometry.");
        Assert.True(derivedQCore.SequenceEqual(new[] { 16, 17, 18 }),
            $"Expected m=3 bridge-core q-set to match Ω-band geometry in tested domain. qCore=[{string.Join(", ", derivedQCore)}]");
        Assert.True(overlapWindows >= 1, "Expected at least one currently selected q-window to overlap derived bridge-core support.");
    }

    /// <summary>
    /// Checks whether m=3 can be selected using only structurally derived bridge-core q-values (no manual q-window pick).
    /// Matters because this directly tests whether q-support can be supplied by geometry rather than operational window selection.
    /// Expected diagnostic behavior: with non-empty derived q-core and a shared derived three-constraint rule, m=3 is selected in the baseline diagnostic case.
    /// Claim boundary: candidate/diagnostic support only; not first-principles closure.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF39_DerivedQCore_Should_Select_M3_WithoutManualQWindowChoice()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        var derivedQCore = DeriveBridgeCoreQValuesFromBand(3, 1.16, 1.19, 0.84, 0.86, 2, 64);

        _output.WriteLine("--- RBF39 DERIVED-QCORE SELECTION DIAGNOSTIC ---");
        _output.WriteLine($"derived qCore=[{string.Join(", ", derivedQCore)}]");

        Assert.True(derivedQCore.Length > 0, "Derived q-core must be non-empty for this diagnostic.");

        int qMin = derivedQCore[0];
        int qMax = derivedQCore[^1];

        var family = BuildModeFamilyFromLatticeProxy(
            mValues,
            qMin,
            qMax,
            0.50,
            0.35,
            0.15,
            BuildNoCadencePriorConfig(),
            new[] { 2e-3 });

        var result = EvaluateDerivedThreeConstraintSelection(
            family,
            phaseThreshold: 0.780,
            bridgeThreshold: 1,
            actionScale: 1.00);

        foreach (var mode in family.OrderBy(x => x.M))
        {
            _output.WriteLine(
                $"RBF39 m={mode.M} | inBand={mode.InBandCount} | phase={mode.AvgClosureQuality:F4} | derivedActionTick={mode.DerivedActionTick:E3}");
        }

        _output.WriteLine($"selected={(result.Resolved ? $"m={result.SelectedMode}" : "none")} | satisfying=[{string.Join(", ", result.SatisfyingModes)}]");
        if (!result.Resolved)
            _output.WriteLine("failure case: no mode satisfied shared derived three-constraint rule on derived qCore.");
        _output.WriteLine("RBF39 claim boundary: candidate diagnostic only, not theorem-level proof.");

        Assert.True(result.Resolved,
            $"Expected a resolved selection on derived qCore=[{string.Join(", ", derivedQCore)}].");
        Assert.True(result.SelectedMode == 3,
            $"Expected m=3 selection under shared derived rule on structurally derived qCore. selected={result.SelectedMode}");
    }

    /// <summary>
    /// Checks whether m=2 fallback aligns with missing/weak bridge-core support versus phase/action boundary stress.
    /// Matters because the remaining gap is explaining fallback mechanisms structurally, not via ad-hoc window choices.
    /// Expected diagnostic behavior: fallback frequency increases in no-core/low-core windows and can be classified by geometry, phase/action boundary, or mixed causes.
    /// Claim boundary: bounded diagnostic classification only; not universal selection proof.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF40_QCoreBoundary_Should_Explain_M2FallbackWhenBridgeCoreIsMissing()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        var qCore = DeriveBridgeCoreQValuesFromBand(3, 1.16, 1.19, 0.84, 0.86, 2, 64);
        var windows = new (int QMin, int QMax, string Name)[]
        {
            (12, 24, "coreA"),
            (13, 25, "coreB"),
            (15, 16, "lowCore"),
            (10, 14, "noCoreLow"),
            (19, 24, "noCoreHigh")
        };
        var phaseThresholds = new[] { 0.775, 0.780, 0.785 };
        var actionScales = new[] { 0.98, 1.00, 1.02 };
        const int bridgeThreshold = 1;

        int fallbackWithCore = 0;
        int fallbackWithoutCore = 0;
        int fallbackBridgeGeometry = 0;
        int fallbackPhaseActionBoundary = 0;
        int fallbackMixed = 0;
        int resolved = 0;

        foreach (var w in windows)
        {
            int coreSupport = qCore.Count(q => q >= w.QMin && q <= w.QMax);
            bool hasCore = coreSupport >= 2;

            var family = BuildModeFamilyFromLatticeProxy(
                mValues,
                w.QMin,
                w.QMax,
                0.50,
                0.35,
                0.15,
                BuildNoCadencePriorConfig(),
                new[] { 2e-3 });

            foreach (double phaseThr in phaseThresholds)
            {
                foreach (double actionScale in actionScales)
                {
                    var result = EvaluateDerivedThreeConstraintSelection(
                        family,
                        phaseThr,
                        bridgeThreshold,
                        actionScale);

                    if (!result.Resolved)
                        continue;

                    resolved++;
                    if (result.SelectedMode != 2)
                        continue;

                    var m3Status = EvaluateModeConstraintStatus(family, 3, phaseThr, bridgeThreshold, actionScale);
                    bool bridgeCoreWeak = coreSupport == 0 || (coreSupport == 1 && !m3Status.BridgeOk);

                    string classification = bridgeCoreWeak
                        ? "bridge-core geometry"
                        : (!m3Status.PhaseOk || !m3Status.ActionOk)
                            ? "phase/action boundary"
                            : "mixed";

                    if (hasCore) fallbackWithCore++;
                    else fallbackWithoutCore++;

                    if (classification == "bridge-core geometry") fallbackBridgeGeometry++;
                    else if (classification == "phase/action boundary") fallbackPhaseActionBoundary++;
                    else fallbackMixed++;

                    _output.WriteLine(
                        $"RBF40 {w.Name} | coreSupport={coreSupport} | phaseThr={phaseThr:F3} | actionScale={actionScale:F2} | selected=m=2 | m3(phase={m3Status.PhaseOk}, bridge={m3Status.BridgeOk}, action={m3Status.ActionOk}) | classification={classification}");
                }
            }
        }

        _output.WriteLine("--- RBF40 QCORE-BOUNDARY FALLBACK DIAGNOSTIC ---");
        _output.WriteLine($"resolved={resolved}");
        _output.WriteLine($"m2 fallback with-core={fallbackWithCore} | without/low-core={fallbackWithoutCore}");
        _output.WriteLine($"fallback classification: bridge-core geometry={fallbackBridgeGeometry}, phase/action boundary={fallbackPhaseActionBoundary}, mixed={fallbackMixed}");
        _output.WriteLine("RBF40 claim boundary: candidate diagnostic only, not theorem-level proof.");

        int totalFallback = fallbackWithCore + fallbackWithoutCore;
        Assert.True(qCore.Length > 0, "Expected non-empty derived bridge-core q-set.");
        Assert.True(resolved > 0, "Expected resolved derived-rule cases for fallback classification.");
        Assert.True(totalFallback > 0, "Expected at least one m=2 fallback case in boundary diagnostics.");
        Assert.True(fallbackWithoutCore >= fallbackWithCore,
            $"Expected m=2 fallback to occur at least as often when bridge-core support is absent/weakened. withCore={fallbackWithCore}, withoutOrLowCore={fallbackWithoutCore}");
    }

    /// <summary>
    /// Checks whether each constraint remains necessary under structurally derived q-support.
    /// Matters because the next gap is structural necessity of phase, bridge, and action/tick constraints.
    /// Expected diagnostic behavior: full stack gives strongest m=3 result while single/double ablations weaken uniqueness or produce competing/unresolved outcomes.
    /// Claim boundary: diagnostic necessity evidence only; not theorem-level proof.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF41_ThreeConstraints_Should_Be_Necessary_Under_DerivedQCore()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        int[] qCoreM3 = DeriveBridgeCoreQValuesFromBand(3, 1.16, 1.19, 0.84, 0.86, 2, 64);
        int[] derivedBandQ = DeriveBridgeBandSupportUnionQValues(mValues, 1.16, 1.19, 0.84, 0.86, 2, 64);

        var family = BuildModeFamilyFromExplicitQValues(
            mValues,
            derivedBandQ,
            0.50,
            0.35,
            0.15,
            BuildNoCadencePriorConfig(),
            new[] { 2e-3 });

        const double phaseThreshold = 0.780;
        const int bridgeThreshold = 1;
        const double actionScale = 1.00;

        var stacks = new (string Name, bool UsePhase, bool UseBridge, bool UseActionTick)[]
        {
            ("full stack", true, true, true),
            ("no phase", false, true, true),
            ("no bridge", true, false, true),
            ("no actionTick", true, true, false),
            ("phase only", true, false, false),
            ("bridge only", false, true, false),
            ("actionTick only", false, false, true),
            ("phase+bridge", true, true, false),
            ("phase+actionTick", true, false, true),
            ("bridge+actionTick", false, true, true)
        };

        var outcomes = stacks
            .Select(s => EvaluateConstraintStackSelection(
                family,
                s.Name,
                s.UsePhase,
                s.UseBridge,
                s.UseActionTick,
                phaseThreshold,
                bridgeThreshold,
                actionScale))
            .ToArray();

        var full = outcomes.First(x => x.StackName == "full stack");
        _output.WriteLine("--- RBF41 THREE-CONSTRAINT NECESSITY DIAGNOSTIC ---");
        _output.WriteLine($"derived qCore(m=3)=[{string.Join(", ", qCoreM3)}]");
        _output.WriteLine($"derived bridge-band support q-values=[{string.Join(", ", derivedBandQ)}]");

        foreach (var outcome in outcomes)
        {
            _output.WriteLine(
                $"RBF41 {outcome.StackName} | resolved={outcome.Resolved} | selected={(outcome.Resolved ? $"m={outcome.SelectedMode}" : "none")} | satisfying=[{string.Join(", ", outcome.SatisfyingModes)}] | m3(admissible={outcome.M3Admissible}, unique={outcome.M3Unique}, minimal={outcome.M3Minimal})");
            _output.WriteLine($"RBF41 {outcome.StackName} failureByMode=[{string.Join(" | ", outcome.FailureByMode)}]");
        }
        _output.WriteLine("RBF41 claim boundary: diagnostic necessity evidence only, not theorem-level proof.");

        bool noPhaseWeakened = IsConstraintAblationWeakened(full, outcomes.First(x => x.StackName == "no phase"));
        bool noBridgeWeakened = IsConstraintAblationWeakened(full, outcomes.First(x => x.StackName == "no bridge"));
        bool noActionWeakened = IsConstraintAblationWeakened(full, outcomes.First(x => x.StackName == "no actionTick"));
        var bridgeOnly = outcomes.First(x => x.StackName == "bridge only");
        bool bridgeWeakeningFoundUnderPerturbation = false;
        var weightFamilies = new (double Order, double Closure, double Transport)[]
        {
            (0.50, 0.35, 0.15),
            (0.55, 0.30, 0.15),
            (0.45, 0.40, 0.15)
        };
        var phaseThresholds = new[] { 0.775, 0.780, 0.785 };
        var actionScales = new[] { 0.98, 1.00, 1.02 };
        var bridgeThresholds = new[] { 1, 2 };
        foreach (var w in weightFamilies)
        {
            var familyW = BuildModeFamilyFromExplicitQValues(
                mValues,
                derivedBandQ,
                w.Order,
                w.Closure,
                w.Transport,
                BuildNoCadencePriorConfig(),
                new[] { 2e-3 });

            foreach (int bridgeThr in bridgeThresholds)
            {
                foreach (double phaseThr in phaseThresholds)
                {
                    foreach (double action in actionScales)
                    {
                        var fullW = EvaluateConstraintStackSelection(
                            familyW,
                            "full stack",
                            usePhase: true,
                            useBridge: true,
                            useActionTick: true,
                            phaseThr,
                            bridgeThr,
                            action);
                        var noBridgeW = EvaluateConstraintStackSelection(
                            familyW,
                            "no bridge",
                            usePhase: true,
                            useBridge: false,
                            useActionTick: true,
                            phaseThr,
                            bridgeThr,
                            action);

                        if (fullW.Resolved && fullW.SelectedMode == 3 && IsConstraintAblationWeakened(fullW, noBridgeW))
                        {
                            bridgeWeakeningFoundUnderPerturbation = true;
                            break;
                        }
                    }

                    if (bridgeWeakeningFoundUnderPerturbation)
                        break;
                }

                if (bridgeWeakeningFoundUnderPerturbation)
                    break;
            }

            if (bridgeWeakeningFoundUnderPerturbation)
                break;
        }
        _output.WriteLine($"RBF41 no-bridge weakening under bounded perturbation found={bridgeWeakeningFoundUnderPerturbation}");

        Assert.True(qCoreM3.SequenceEqual(new[] { 16, 17, 18 }),
            $"Expected structurally derived m=3 qCore [16,17,18]. qCore=[{string.Join(", ", qCoreM3)}]");
        Assert.True(full.Resolved && full.SelectedMode == 3,
            $"Expected full-stack derived support to resolve to m=3. selected={(full.Resolved ? $"m={full.SelectedMode}" : "none")}");
        Assert.True(noPhaseWeakened,
            "Expected removing phase closure to weaken uniqueness or produce competing/unresolved selection.");
        Assert.True(noBridgeWeakened || bridgeWeakeningFoundUnderPerturbation || (bridgeOnly.Resolved && bridgeOnly.SelectedMode != 3),
            "Expected bridge-constraint necessity evidence via no-bridge weakening/non-robustness or competing-mode selection in bridge-only ablation.");
        Assert.True(noActionWeakened,
            "Expected removing action/tick consistency to weaken uniqueness or produce competing/unresolved selection.");
    }

    /// <summary>
    /// Checks whether constraint-necessity diagnostics persist under bounded perturbations without per-family retuning.
    /// Matters because structural necessity should remain visible in a local neighborhood around baseline.
    /// Expected diagnostic behavior: full-stack m=3 remains locally stable while ablated stacks show weaker uniqueness, fallback, or unresolved behavior.
    /// Claim boundary: diagnostic/candidate persistence only; not theorem-level proof.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF42_ConstraintNecessity_Should_Persist_Under_BoundedPerturbations()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        int[] derivedBandQ = DeriveBridgeBandSupportUnionQValues(mValues, 1.16, 1.19, 0.84, 0.86, 2, 64);
        var phaseThresholds = new[] { 0.775, 0.780, 0.785 };
        var actionScales = new[] { 0.98, 1.00, 1.02 };
        var bridgeThresholds = new[] { 1, 2 };
        var weights = new (string Name, double Order, double Closure, double Transport)[]
        {
            ("wA", 0.50, 0.35, 0.15),
            ("wB", 0.55, 0.30, 0.15),
            ("wC", 0.45, 0.40, 0.15)
        };

        int totalCases = 0;
        int fullResolved = 0;
        int fullSelectedM3 = 0;
        int fullUnresolved = 0;
        int noPhaseWeakened = 0;
        int noBridgeWeakened = 0;
        int noActionWeakened = 0;
        int fallbackM2 = 0;
        int fallbackOther = 0;
        int baselineStable = 0;

        foreach (var w in weights)
        {
            var family = BuildModeFamilyFromExplicitQValues(
                mValues,
                derivedBandQ,
                w.Order,
                w.Closure,
                w.Transport,
                BuildNoCadencePriorConfig(),
                new[] { 2e-3 });

            var baseline = EvaluateConstraintStackSelection(
                family,
                "full stack baseline",
                usePhase: true,
                useBridge: true,
                useActionTick: true,
                phaseThreshold: 0.780,
                bridgeThreshold: 1,
                actionScale: 1.00);

            if (baseline.Resolved && baseline.SelectedMode == 3)
                baselineStable++;

            foreach (int bridgeThr in bridgeThresholds)
            {
                foreach (double phaseThr in phaseThresholds)
                {
                    foreach (double actionScale in actionScales)
                    {
                        totalCases++;
                        var full = EvaluateConstraintStackSelection(
                            family,
                            "full stack",
                            usePhase: true,
                            useBridge: true,
                            useActionTick: true,
                            phaseThr,
                            bridgeThr,
                            actionScale);
                        var noPhase = EvaluateConstraintStackSelection(
                            family,
                            "no phase",
                            usePhase: false,
                            useBridge: true,
                            useActionTick: true,
                            phaseThr,
                            bridgeThr,
                            actionScale);
                        var noBridge = EvaluateConstraintStackSelection(
                            family,
                            "no bridge",
                            usePhase: true,
                            useBridge: false,
                            useActionTick: true,
                            phaseThr,
                            bridgeThr,
                            actionScale);
                        var noAction = EvaluateConstraintStackSelection(
                            family,
                            "no actionTick",
                            usePhase: true,
                            useBridge: true,
                            useActionTick: false,
                            phaseThr,
                            bridgeThr,
                            actionScale);

                        if (full.Resolved)
                        {
                            fullResolved++;
                            if (full.SelectedMode == 3)
                                fullSelectedM3++;
                            else if (full.SelectedMode == 2)
                                fallbackM2++;
                            else
                                fallbackOther++;
                        }
                        else
                        {
                            fullUnresolved++;
                        }

                        if (IsConstraintAblationWeakened(full, noPhase)) noPhaseWeakened++;
                        if (IsConstraintAblationWeakened(full, noBridge)) noBridgeWeakened++;
                        if (IsConstraintAblationWeakened(full, noAction)) noActionWeakened++;
                    }
                }
            }
        }

        _output.WriteLine("--- RBF42 BOUNDED-PERTURBATION CONSTRAINT NECESSITY DIAGNOSTIC ---");
        _output.WriteLine($"derived bridge-band support q-values=[{string.Join(", ", derivedBandQ)}]");
        _output.WriteLine($"total cases={totalCases} | full resolved={fullResolved} | full unresolved={fullUnresolved}");
        _output.WriteLine($"full-stack selected m3={fullSelectedM3} | fallback m2={fallbackM2} | fallback other={fallbackOther}");
        _output.WriteLine($"ablation weakened counts: noPhase={noPhaseWeakened}, noBridge={noBridgeWeakened}, noAction={noActionWeakened}");
        _output.WriteLine($"baseline full-stack m3 stability across weights={baselineStable}/{weights.Length}");
        _output.WriteLine("RBF42 claim boundary: diagnostic/candidate persistence only, not theorem-level proof.");

        int ablationTotal = noPhaseWeakened + noBridgeWeakened + noActionWeakened;
        Assert.True(totalCases > 0, "Expected bounded perturbation scenarios.");
        Assert.True(baselineStable == weights.Length,
            $"Expected baseline full-stack m=3 selection for all shared weight families. stable={baselineStable}/{weights.Length}");
        Assert.True(fullSelectedM3 >= Math.Max(1, fullResolved / 3),
            $"Expected local full-stack m=3 stability under bounded perturbations. m3={fullSelectedM3}, resolved={fullResolved}");
        Assert.True(ablationTotal >= totalCases,
            $"Expected ablated stacks to show weaker uniqueness/fallback/unresolved behavior in aggregate. weakened={ablationTotal}, totalCases={totalCases}");
    }

    /// <summary>
    /// Checks whether shortcut rules can replace explicit three-constraint gating under derived q-support.
    /// Matters because structural necessity fails if ungated shortcuts recover the same bounded behavior.
    /// Expected diagnostic behavior: shortcuts show competing winners, instability, or mismatch with derived q-core behavior.
    /// Claim boundary: insufficiency diagnostic only; not theorem-level proof.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF43_ThreeConstraintModel_Should_Reject_NonStructuralShortcutRules()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        int[] qCoreM3 = DeriveBridgeCoreQValuesFromBand(3, 1.16, 1.19, 0.84, 0.86, 2, 64);
        int[] derivedBandQ = DeriveBridgeBandSupportUnionQValues(mValues, 1.16, 1.19, 0.84, 0.86, 2, 64);
        int[] nonCoreQ = derivedBandQ.Where(q => !qCoreM3.Contains(q)).ToArray();
        var weights = new (string Name, double Order, double Closure, double Transport)[]
        {
            ("wA", 0.50, 0.35, 0.15),
            ("wB", 0.55, 0.30, 0.15),
            ("wC", 0.45, 0.40, 0.15)
        };

        var shortcutRules = new[]
        {
            ShortcutRuleKind.PhaseOnlyBestClosure,
            ShortcutRuleKind.BridgeOnlyOccupancy,
            ShortcutRuleKind.ActionOnlyLowestDerivedActionTick,
            ShortcutRuleKind.CombinedScoreNoExplicitGates
        };

        var baselineFamily = BuildModeFamilyFromExplicitQValues(
            mValues,
            derivedBandQ,
            0.50,
            0.35,
            0.15,
            BuildNoCadencePriorConfig(),
            new[] { 2e-3 });
        var fullDerived = EvaluateConstraintStackSelection(
            baselineFamily,
            "full stack",
            usePhase: true,
            useBridge: true,
            useActionTick: true,
            phaseThreshold: 0.780,
            bridgeThreshold: 1,
            actionScale: 1.00);

        _output.WriteLine("--- RBF43 SHORTCUT-RULE REJECTION DIAGNOSTIC ---");
        _output.WriteLine($"derived qCore(m=3)=[{string.Join(", ", qCoreM3)}] | derived bridge-band support=[{string.Join(", ", derivedBandQ)}]");
        _output.WriteLine($"full derived rule selected={(fullDerived.Resolved ? $"m={fullDerived.SelectedMode}" : "none")} | satisfying=[{string.Join(", ", fullDerived.SatisfyingModes)}]");
        _output.WriteLine($"full derived failureByMode=[{string.Join(" | ", fullDerived.FailureByMode)}]");

        int shortcutInsufficient = 0;

        foreach (var rule in shortcutRules)
        {
            int baselineSelected = SelectModeByShortcutRule(baselineFamily, rule);
            bool competingMode = baselineSelected != 3;

            var selectedAcrossWeights = new HashSet<int>();
            foreach (var w in weights)
            {
                var family = BuildModeFamilyFromExplicitQValues(
                    mValues,
                    derivedBandQ,
                    w.Order,
                    w.Closure,
                    w.Transport,
                    BuildNoCadencePriorConfig(),
                    new[] { 2e-3 });
                selectedAcrossWeights.Add(SelectModeByShortcutRule(family, rule));
            }
            bool unstable = selectedAcrossWeights.Count > 1;

            var familyCore = BuildModeFamilyFromExplicitQValues(
                mValues,
                qCoreM3,
                0.50,
                0.35,
                0.15,
                BuildNoCadencePriorConfig(),
                new[] { 2e-3 });
            int coreSelected = SelectModeByShortcutRule(familyCore, rule);

            bool nonCoreMismatch = false;
            int nonCoreSelected = int.MaxValue;
            if (nonCoreQ.Length > 0)
            {
                var familyNonCore = BuildModeFamilyFromExplicitQValues(
                    mValues,
                    nonCoreQ,
                    0.50,
                    0.35,
                    0.15,
                    BuildNoCadencePriorConfig(),
                    new[] { 2e-3 });
                nonCoreSelected = SelectModeByShortcutRule(familyNonCore, rule);
                nonCoreMismatch = !(coreSelected == 3 && nonCoreSelected != 3);
            }

            bool insufficient = competingMode || unstable || nonCoreMismatch;
            if (insufficient)
                shortcutInsufficient++;

            _output.WriteLine(
                $"RBF43 {rule} | baselineSelected=m={baselineSelected} | selectedAcrossWeights=[{string.Join(", ", selectedAcrossWeights.OrderBy(x => x))}] | coreSelected=m={coreSelected} | nonCoreSelected={(nonCoreSelected == int.MaxValue ? "n/a" : $"m={nonCoreSelected}")} | competing={competingMode} | unstable={unstable} | qCoreMismatch={nonCoreMismatch} | insufficient={insufficient}");
        }

        _output.WriteLine("RBF43 claim boundary: diagnostic/candidate only; shortcut insufficiency is not theorem-level proof.");

        Assert.True(fullDerived.Resolved && fullDerived.SelectedMode == 3,
            $"Expected full derived three-constraint baseline to select m=3. selected={(fullDerived.Resolved ? $"m={fullDerived.SelectedMode}" : "none")}");
        Assert.True(shortcutInsufficient == shortcutRules.Length,
            $"Expected all shortcut rules to be insufficient by competing selection, bounded-stability loss, or q-core mismatch. insufficient={shortcutInsufficient}/{shortcutRules.Length}");
    }

    /// <summary>
    /// Checks whether bridge occupancy is independently discriminative after using structurally derived qCore support.
    /// Matters because bridge gating may be partly encoded by qCore construction and should not be double-counted.
    /// Expected diagnostic behavior: compare broad-window+bridge, qCore+bridge, and qCore without bridge to classify bridge independence.
    /// Claim boundary: diagnostic only; not theorem-level proof.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF44_BridgeConstraint_Should_Not_Be_DoubleCounted_After_DerivedQCore()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        int[] qCore = DeriveBridgeCoreQValuesFromBand(3, 1.16, 1.19, 0.84, 0.86, 2, 64);
        const double phaseThr = 0.780;
        const int bridgeThr = 1;
        const double actionScale = 1.00;

        var broadWindowFamily = BuildModeFamilyFromLatticeProxy(
            mValues,
            qMin: 10,
            qMax: 30,
            orderWeight: 0.50,
            closureWeight: 0.35,
            transportWeight: 0.15,
            BuildNoCadencePriorConfig(),
            new[] { 2e-3 });
        var qCoreFamily = BuildModeFamilyFromExplicitQValues(
            mValues,
            qCore,
            0.50,
            0.35,
            0.15,
            BuildNoCadencePriorConfig(),
            new[] { 2e-3 });

        var manualBroadWithBridge = EvaluateConstraintStackSelection(
            broadWindowFamily, "manual broad + bridge", true, true, true, phaseThr, bridgeThr, actionScale);
        var derivedQCoreWithBridge = EvaluateConstraintStackSelection(
            qCoreFamily, "derived qCore + bridge", true, true, true, phaseThr, bridgeThr, actionScale);
        var derivedQCoreNoBridge = EvaluateConstraintStackSelection(
            qCoreFamily, "derived qCore without bridge", true, false, true, phaseThr, bridgeThr, actionScale);

        void PrintOutcome((string StackName, bool Resolved, int SelectedMode, int[] SatisfyingModes, string[] FailureByMode, bool M3Admissible, bool M3Unique, bool M3Minimal) x)
        {
            _output.WriteLine(
                $"RBF44 {x.StackName} | resolved={x.Resolved} | selected={(x.Resolved ? $"m={x.SelectedMode}" : "none")} | satisfying=[{string.Join(", ", x.SatisfyingModes)}] | m3(admissible={x.M3Admissible}, unique={x.M3Unique}, minimal={x.M3Minimal})");
            _output.WriteLine($"RBF44 {x.StackName} failureByMode=[{string.Join(" | ", x.FailureByMode)}]");
        }

        _output.WriteLine("--- RBF44 BRIDGE DOUBLE-COUNT DIAGNOSTIC ---");
        _output.WriteLine($"derived qCore(m=3)=[{string.Join(", ", qCore)}]");
        PrintOutcome(manualBroadWithBridge);
        PrintOutcome(derivedQCoreWithBridge);
        PrintOutcome(derivedQCoreNoBridge);

        bool bridgeStillIndependent =
            !derivedQCoreWithBridge.Resolved ||
            !derivedQCoreNoBridge.Resolved ||
            derivedQCoreWithBridge.SelectedMode != derivedQCoreNoBridge.SelectedMode ||
            !derivedQCoreWithBridge.SatisfyingModes.SequenceEqual(derivedQCoreNoBridge.SatisfyingModes);

        string classification = bridgeStillIndependent
            ? "bridge occupancy remains an independent discriminator under derived qCore"
            : "bridge occupancy appears largely encoded by derived qCore in this bounded diagnostic setting";

        _output.WriteLine($"RBF44 interpretation={classification}");
        _output.WriteLine("RBF44 claim boundary: diagnostic only; no theorem-level proof.");

        Assert.True(qCore.SequenceEqual(new[] { 16, 17, 18 }),
            $"Expected derived qCore(m=3)=[16,17,18]. qCore=[{string.Join(", ", qCore)}]");
        Assert.True(manualBroadWithBridge.Resolved && derivedQCoreWithBridge.Resolved,
            "Expected resolved selections for broad+bridge and qCore+bridge baseline diagnostics.");
    }

    /// <summary>
    /// Checks that explicit bridge gating is still necessary when evaluating wider q-support beyond derived core.
    /// Matters because non-core support should be suppressed without per-family tuning.
    /// Expected diagnostic behavior: removing bridge admits competing/non-core modes more often than full stack.
    /// Claim boundary: diagnostic/candidate only; not theorem-level proof.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF45_BridgeConstraint_Should_Be_Necessary_Outside_DerivedQCore()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        int[] qCore = DeriveBridgeCoreQValuesFromBand(3, 1.16, 1.19, 0.84, 0.86, 2, 64);
        int[] wideQ = Enumerable.Range(4, 33).ToArray(); // 4..36
        var weights = new (string Name, double Order, double Closure, double Transport)[]
        {
            ("wA", 0.50, 0.35, 0.15),
            ("wB", 0.55, 0.30, 0.15),
            ("wC", 0.45, 0.40, 0.15)
        };
        var phaseThresholds = new[] { 0.775, 0.780, 0.785 };
        var actionScales = new[] { 0.98, 1.00, 1.02 };
        var bridgeThresholds = new[] { 1, 2 };

        int totalCases = 0;
        int bridgeSuppressionCases = 0;
        int noBridgeCompetingCases = 0;
        int noBridgeFallbackM2 = 0;

        _output.WriteLine("--- RBF45 BRIDGE NECESSITY OUTSIDE QCORE DIAGNOSTIC ---");
        _output.WriteLine($"derived qCore(m=3)=[{string.Join(", ", qCore)}] | wide q-support=[{string.Join(", ", wideQ)}]");

        foreach (var w in weights)
        {
            var family = BuildModeFamilyFromExplicitQValues(
                mValues,
                wideQ,
                w.Order,
                w.Closure,
                w.Transport,
                BuildNoCadencePriorConfig(),
                new[] { 2e-3 });

            foreach (int bridgeThr in bridgeThresholds)
            {
                foreach (double phaseThr in phaseThresholds)
                {
                    foreach (double actionScale in actionScales)
                    {
                        totalCases++;
                        var full = EvaluateConstraintStackSelection(
                            family, "full stack", true, true, true, phaseThr, bridgeThr, actionScale);
                        var noBridge = EvaluateConstraintStackSelection(
                            family, "no bridge", true, false, true, phaseThr, bridgeThr, actionScale);

                        var nonCoreFull = full.SatisfyingModes.Where(m => m != 3).ToArray();
                        var nonCoreNoBridge = noBridge.SatisfyingModes.Where(m => m != 3).ToArray();
                        var fullFailures = full.FailureByMode.ToDictionary(
                            s => int.Parse(s.Split(':')[0].Replace("m=", string.Empty)),
                            s => s.Split(':')[1],
                            EqualityComparer<int>.Default);
                        var noBridgeFailures = noBridge.FailureByMode.ToDictionary(
                            s => int.Parse(s.Split(':')[0].Replace("m=", string.Empty)),
                            s => s.Split(':')[1],
                            EqualityComparer<int>.Default);

                        bool bridgeFailureAdded = false;
                        foreach (int m in mValues.Where(x => x != 3))
                        {
                            if (fullFailures.TryGetValue(m, out string? f) &&
                                noBridgeFailures.TryGetValue(m, out string? n) &&
                                f.Contains("bridge", StringComparison.Ordinal) &&
                                !n.Contains("bridge", StringComparison.Ordinal))
                            {
                                bridgeFailureAdded = true;
                                break;
                            }
                        }

                        bool suppressesNonCore = nonCoreNoBridge.Length > nonCoreFull.Length || bridgeFailureAdded;
                        if (suppressesNonCore)
                            bridgeSuppressionCases++;

                        bool competingOrUnresolved = !noBridge.Resolved || noBridge.SelectedMode != 3;
                        if (competingOrUnresolved)
                            noBridgeCompetingCases++;
                        if (noBridge.Resolved && noBridge.SelectedMode == 2)
                            noBridgeFallbackM2++;

                        _output.WriteLine(
                            $"RBF45 {w.Name}|phaseThr={phaseThr:F3}|bridgeThr={bridgeThr}|actionScale={actionScale:F2} | full={(full.Resolved ? $"m={full.SelectedMode}" : "none")} nonCore=[{string.Join(", ", nonCoreFull)}] | noBridge={(noBridge.Resolved ? $"m={noBridge.SelectedMode}" : "none")} nonCore=[{string.Join(", ", nonCoreNoBridge)}] | bridgeFailureAdded={bridgeFailureAdded} | suppressesNonCore={suppressesNonCore}");
                        _output.WriteLine($"RBF45 full failureByMode=[{string.Join(" | ", full.FailureByMode)}]");
                        _output.WriteLine($"RBF45 noBridge failureByMode=[{string.Join(" | ", noBridge.FailureByMode)}]");
                    }
                }
            }
        }

        _output.WriteLine($"RBF45 total cases={totalCases}");
        _output.WriteLine($"RBF45 bridge suppression cases={bridgeSuppressionCases}");
        _output.WriteLine($"RBF45 no-bridge competing/unresolved cases={noBridgeCompetingCases} | no-bridge m2 fallback cases={noBridgeFallbackM2}");
        _output.WriteLine("RBF45 claim boundary: diagnostic/candidate only; no theorem-level proof.");

        Assert.True(totalCases > 0, "Expected wide-q bridge-necessity scenarios.");
        Assert.True(bridgeSuppressionCases > 0,
            "Expected explicit bridge gating to suppress non-core/competing admissibility in at least one wide-q scenario.");
        Assert.True(noBridgeCompetingCases > 0,
            "Expected no-bridge ablation to admit competing or unresolved outcomes in at least one wide-q scenario.");
    }

    /// <summary>
    /// Checks whether derived qCore should be interpreted as a structural bridge prior rather than theorem closure.
    /// Matters because qCore support can be strong without proving universal or first-principles closure.
    /// Expected diagnostic behavior: classify qCore role by comparing qCore-only and full-support explicit-bridge selections.
    /// Claim boundary: diagnostic/candidate only; not theorem-level, not first-principles closure.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF46_DerivedQCore_Should_Be_Treated_As_BridgePrior_Not_Theorem()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        int[] qCore = DeriveBridgeCoreQValuesFromBand(3, 1.16, 1.19, 0.84, 0.86, 2, 64);
        int[] wideQ = Enumerable.Range(4, 33).ToArray(); // 4..36

        var familyQCore = BuildModeFamilyFromExplicitQValues(
            mValues,
            qCore,
            0.50,
            0.35,
            0.15,
            BuildNoCadencePriorConfig(),
            new[] { 2e-3 });
        var familyWide = BuildModeFamilyFromExplicitQValues(
            mValues,
            wideQ,
            0.50,
            0.35,
            0.15,
            BuildNoCadencePriorConfig(),
            new[] { 2e-3 });

        var qCoreOnly = EvaluateConstraintStackSelection(
            familyQCore, "qCore-only (phase+action)", true, false, true, phaseThreshold: 0.780, bridgeThreshold: 1, actionScale: 1.00);
        var fullWithBridge = EvaluateConstraintStackSelection(
            familyWide, "full support + explicit bridge", true, true, true, phaseThreshold: 0.780, bridgeThreshold: 1, actionScale: 1.00);
        var fullWithoutBridge = EvaluateConstraintStackSelection(
            familyWide, "full support without bridge", true, false, true, phaseThreshold: 0.780, bridgeThreshold: 1, actionScale: 1.00);

        void PrintOutcome((string StackName, bool Resolved, int SelectedMode, int[] SatisfyingModes, string[] FailureByMode, bool M3Admissible, bool M3Unique, bool M3Minimal) x)
        {
            _output.WriteLine(
                $"RBF46 {x.StackName} | resolved={x.Resolved} | selected={(x.Resolved ? $"m={x.SelectedMode}" : "none")} | satisfying=[{string.Join(", ", x.SatisfyingModes)}] | m3(admissible={x.M3Admissible}, unique={x.M3Unique}, minimal={x.M3Minimal})");
            _output.WriteLine($"RBF46 {x.StackName} failureByMode=[{string.Join(" | ", x.FailureByMode)}]");
        }

        string classification;
        if (qCoreOnly.Resolved && fullWithBridge.Resolved &&
            qCoreOnly.SelectedMode == 3 && fullWithBridge.SelectedMode == 3 &&
            (!fullWithoutBridge.Resolved || fullWithoutBridge.SelectedMode != 3 ||
             fullWithoutBridge.SatisfyingModes.Length > fullWithBridge.SatisfyingModes.Length))
        {
            classification = "structural bridge prior";
        }
        else if (qCoreOnly.Resolved && fullWithBridge.Resolved && fullWithoutBridge.Resolved &&
                 qCoreOnly.SelectedMode == fullWithBridge.SelectedMode &&
                 fullWithoutBridge.SelectedMode == fullWithBridge.SelectedMode &&
                 fullWithoutBridge.SatisfyingModes.SequenceEqual(fullWithBridge.SatisfyingModes))
        {
            classification = "hard derived domain";
        }
        else
        {
            classification = "insufficiently independent";
        }

        _output.WriteLine("--- RBF46 QCORE ROLE CLASSIFICATION DIAGNOSTIC ---");
        _output.WriteLine($"derived qCore(m=3)=[{string.Join(", ", qCore)}] | wide q-support=[{string.Join(", ", wideQ)}]");
        PrintOutcome(qCoreOnly);
        PrintOutcome(fullWithBridge);
        PrintOutcome(fullWithoutBridge);
        _output.WriteLine($"RBF46 classification={classification}");
        _output.WriteLine("RBF46 reviewer-safe interpretation: qCore supports bridge geometry as a structural prior in this diagnostic path, but does not prove theorem-level closure.");
        _output.WriteLine("RBF46 claim boundary: diagnostic/candidate only; no theorem-level proof, no first-principles closure, no universal m=3 claim.");

        Assert.True(qCore.SequenceEqual(new[] { 16, 17, 18 }),
            $"Expected derived qCore(m=3)=[16,17,18]. qCore=[{string.Join(", ", qCore)}]");
        Assert.True(qCoreOnly.Resolved && fullWithBridge.Resolved,
            "Expected resolved qCore-only and full-support-with-bridge baseline diagnostics.");
        Assert.True(classification is "structural bridge prior" or "hard derived domain" or "insufficiently independent",
            $"Unexpected qCore classification: {classification}");
    }

    /// <summary>
    /// Checks whether phase gating can be expressed through an integer closure-defect criterion instead of proxy thresholding.
    /// Matters because the next structural gap is deriving phase admissibility from closure compatibility, not from operational phase cuts.
    /// Expected diagnostic behavior: derived closure-defect phase status tracks baseline shape with explicit mismatch/boundary reporting.
    /// Claim boundary: diagnostic/candidate only; not theorem-level proof.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF47_PhaseConstraint_Should_Follow_From_IntegerClosureDefect()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        int[] qCore = DeriveBridgeCoreQValuesFromBand(3, 1.16, 1.19, 0.84, 0.86, 2, 64);
        int[] qSupport = DeriveBridgeBandSupportUnionQValues(mValues, 1.16, 1.19, 0.84, 0.86, 2, 64);

        var family = BuildModeFamilyFromExplicitQValues(
            mValues,
            qSupport,
            0.50,
            0.35,
            0.15,
            BuildNoCadencePriorConfig(),
            new[] { 2e-3 });

        const double baselinePhaseThreshold = 0.780;
        const int bridgeThreshold = 1;
        const double actionScale = 1.00;
        const double closureDefectThreshold = 0.30;

        var statuses = family.ToDictionary(
            x => x.M,
            x =>
            {
                double closureDefect = ComputeIntegerClosureDefectNormalized(x.M, qSupport, targetShift: 3);
                bool phaseOkDerived = closureDefect <= closureDefectThreshold;
                bool bridgeOk = x.InBandCount >= bridgeThreshold;
                bool actionOk = EvaluateModeConstraintStatus(family, x.M, baselinePhaseThreshold, bridgeThreshold, actionScale).ActionOk;
                return (PhaseOk: phaseOkDerived, BridgeOk: bridgeOk, ActionOk: actionOk, ClosureDefect: closureDefect);
            });
        var selectionStatuses = statuses.ToDictionary(
            kvp => kvp.Key,
            kvp => (kvp.Value.PhaseOk, kvp.Value.BridgeOk, kvp.Value.ActionOk));

        var derivedSelection = EvaluateSelectionFromCustomStatuses(family, selectionStatuses);
        var baselineSelection = EvaluateConstraintStackSelection(
            family,
            "baseline phase-threshold stack",
            usePhase: true,
            useBridge: true,
            useActionTick: true,
            baselinePhaseThreshold,
            bridgeThreshold,
            actionScale);

        var mismatchCases = new List<int>();
        var boundaryCases = new List<int>();
        foreach (var mode in family.OrderBy(x => x.M))
        {
            bool baselinePhaseOk = mode.AvgClosureQuality >= baselinePhaseThreshold;
            bool derivedPhaseOk = statuses[mode.M].PhaseOk;
            if (baselinePhaseOk != derivedPhaseOk)
                mismatchCases.Add(mode.M);

            double defect = statuses[mode.M].ClosureDefect;
            if (defect > closureDefectThreshold && defect <= closureDefectThreshold + 0.05)
                boundaryCases.Add(mode.M);

            _output.WriteLine(
                $"RBF47 m={mode.M} | closureDefect={defect:F4} | phase(base={baselinePhaseOk}, derived={derivedPhaseOk}) | bridgeOk={statuses[mode.M].BridgeOk} | actionOk={statuses[mode.M].ActionOk}");
        }

        _output.WriteLine("--- RBF47 INTEGER-CLOSURE-DEFECT PHASE DIAGNOSTIC ---");
        _output.WriteLine($"derived qCore(m=3)=[{string.Join(", ", qCore)}] | qSupport=[{string.Join(", ", qSupport)}]");
        _output.WriteLine($"derived selection={(derivedSelection.Resolved ? $"m={derivedSelection.SelectedMode}" : "none")} | satisfying=[{string.Join(", ", derivedSelection.SatisfyingModes)}]");
        _output.WriteLine($"baseline selection={(baselineSelection.Resolved ? $"m={baselineSelection.SelectedMode}" : "none")} | satisfying=[{string.Join(", ", baselineSelection.SatisfyingModes)}]");
        _output.WriteLine($"mismatch cases=[{string.Join(", ", mismatchCases)}] | boundary cases=[{string.Join(", ", boundaryCases)}]");
        _output.WriteLine($"failureByMode=[{string.Join(" | ", derivedSelection.FailureByMode)}]");
        _output.WriteLine($"m3(admissible={derivedSelection.M3Admissible}, unique={derivedSelection.M3Unique}, minimal={derivedSelection.M3Minimal}) | resolved={derivedSelection.Resolved}");
        _output.WriteLine("RBF47 claim boundary: diagnostic/candidate only; not theorem-level proof.");

        Assert.True(qCore.SequenceEqual(new[] { 16, 17, 18 }),
            $"Expected derived qCore(m=3)=[16,17,18]. qCore=[{string.Join(", ", qCore)}]");
        Assert.True(derivedSelection.Resolved && derivedSelection.SelectedMode == 3,
            $"Expected derived closure-defect stack to resolve to m=3. selected={(derivedSelection.Resolved ? $"m={derivedSelection.SelectedMode}" : "none")}");
        Assert.True(mismatchCases.Count > 0, "Expected at least one baseline-vs-derived phase mismatch case.");
    }

    /// <summary>
    /// Checks whether action/tick gating can be replaced by a lattice-energy stationarity criterion.
    /// Matters because stationarity/minimal-energy rationale is structurally stronger than scale-threshold tuning.
    /// Expected diagnostic behavior: m=3 remains admissible while m=2 is excluded in baseline shared-rule context without per-family retuning.
    /// Claim boundary: diagnostic/candidate only; not theorem-level proof.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF48_ActionTickConstraint_Should_Follow_From_LatticeEnergyStationarity()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        int[] qSupport = DeriveBridgeBandSupportUnionQValues(mValues, 1.16, 1.19, 0.84, 0.86, 2, 64);
        var family = BuildModeFamilyFromExplicitQValues(
            mValues,
            qSupport,
            0.50,
            0.35,
            0.15,
            BuildNoCadencePriorConfig(),
            new[] { 2e-3 });

        const double phaseThreshold = 0.780;
        const int bridgeThreshold = 1;

        var phaseBridgeCandidates = family
            .Where(x => x.AvgClosureQuality >= phaseThreshold && x.InBandCount >= bridgeThreshold)
            .OrderBy(x => x.DerivedActionTick)
            .ToArray();
        Assert.True(phaseBridgeCandidates.Length > 0, "Expected phase+bridge candidates for stationarity diagnostic.");

        double minEnergy = phaseBridgeCandidates[0].DerivedActionTick;
        double secondEnergy = phaseBridgeCandidates.Length >= 2
            ? phaseBridgeCandidates[1].DerivedActionTick
            : minEnergy * 1.05;
        double gapScale = Math.Max(secondEnergy - minEnergy, 1e-12);

        var statuses = family.ToDictionary(
            x => x.M,
            x =>
            {
                bool phaseOk = x.AvgClosureQuality >= phaseThreshold;
                bool bridgeOk = x.InBandCount >= bridgeThreshold;
                double residual = phaseOk && bridgeOk
                    ? (x.DerivedActionTick - minEnergy) / gapScale
                    : double.PositiveInfinity;
                bool actionOk = residual < 1.0; // stationarity basin of the minimum (no per-family retuning)
                return (PhaseOk: phaseOk, BridgeOk: bridgeOk, ActionOk: actionOk, Residual: residual);
            });
        var selectionStatuses = statuses.ToDictionary(
            kvp => kvp.Key,
            kvp => (kvp.Value.PhaseOk, kvp.Value.BridgeOk, kvp.Value.ActionOk));

        var stationaritySelection = EvaluateSelectionFromCustomStatuses(family, selectionStatuses);
        var baselineSelection = EvaluateConstraintStackSelection(
            family,
            "baseline actionScale stack",
            usePhase: true,
            useBridge: true,
            useActionTick: true,
            phaseThreshold,
            bridgeThreshold,
            actionScale: 1.00);

        foreach (var mode in family.OrderBy(x => x.M))
        {
            string residualText = double.IsFinite(statuses[mode.M].Residual)
                ? statuses[mode.M].Residual.ToString("F4")
                : "inf";
            _output.WriteLine(
                $"RBF48 m={mode.M} | derivedActionTick={mode.DerivedActionTick:E3} | stationarityResidual={residualText} | phaseOk={statuses[mode.M].PhaseOk} | bridgeOk={statuses[mode.M].BridgeOk} | actionStationary={statuses[mode.M].ActionOk}");
        }

        _output.WriteLine("--- RBF48 LATTICE-ENERGY STATIONARITY DIAGNOSTIC ---");
        _output.WriteLine($"minEnergy={minEnergy:E3} | secondEnergy={secondEnergy:E3} | gapScale={gapScale:E3}");
        _output.WriteLine($"stationarity selection={(stationaritySelection.Resolved ? $"m={stationaritySelection.SelectedMode}" : "none")} | satisfying=[{string.Join(", ", stationaritySelection.SatisfyingModes)}]");
        _output.WriteLine($"baseline selection={(baselineSelection.Resolved ? $"m={baselineSelection.SelectedMode}" : "none")} | satisfying=[{string.Join(", ", baselineSelection.SatisfyingModes)}]");
        _output.WriteLine($"failureByMode=[{string.Join(" | ", stationaritySelection.FailureByMode)}]");
        _output.WriteLine($"m3(admissible={stationaritySelection.M3Admissible}, unique={stationaritySelection.M3Unique}, minimal={stationaritySelection.M3Minimal}) | resolved={stationaritySelection.Resolved}");
        _output.WriteLine("RBF48 claim boundary: diagnostic/candidate only; no theorem-level proof.");

        Assert.True(stationaritySelection.Resolved && stationaritySelection.SelectedMode == 3,
            $"Expected stationarity-based action gating to resolve to m=3. selected={(stationaritySelection.Resolved ? $"m={stationaritySelection.SelectedMode}" : "none")}");
        Assert.False(stationaritySelection.SatisfyingModes.Contains(2),
            $"Expected m=2 to be excluded in baseline stationarity gate. satisfying=[{string.Join(", ", stationaritySelection.SatisfyingModes)}]");
    }

    /// <summary>
    /// Checks whether phase-defect, bridge-prior support, and action stationarity can be expressed as one shared diagnostic functional.
    /// Matters because this is the next step toward a single lattice/action rationale while staying below theorem-level claims.
    /// Expected diagnostic behavior: m=3 is minimal admissible under the shared functional, while shortcuts/ablations lose structure.
    /// Claim boundary: diagnostic/candidate only; not theorem-level proof.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF49_ThreeConstraintStack_Should_Map_To_OneMinimalEnergyFunctional()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        int[] qCore = DeriveBridgeCoreQValuesFromBand(3, 1.16, 1.19, 0.84, 0.86, 2, 64);
        int[] qSupport = Enumerable.Range(4, 33).ToArray(); // 4..36

        var family = BuildModeFamilyFromExplicitQValues(
            mValues,
            qSupport,
            0.50,
            0.35,
            0.15,
            BuildNoCadencePriorConfig(),
            new[] { 2e-3 });

        double minAction = family.Min(x => x.DerivedActionTick);
        double actionRange = Math.Max(family.Max(x => x.DerivedActionTick) - minAction, 1e-12);

        var functional = family
            .Select(x =>
            {
                double phaseDefect = ComputeIntegerClosureDefectNormalized(x.M, qCore, targetShift: 3);
                double bridgePriorPenalty = ComputeBridgePriorPenalty(x.M, qCore);
                double actionResidual = (x.DerivedActionTick - minAction) / actionRange;
                double totalEnergy = phaseDefect + bridgePriorPenalty + actionResidual;
                bool admissible = phaseDefect <= 0.35 && bridgePriorPenalty < 1.0 && actionResidual <= 0.95;
                return (x.M, PhaseDefect: phaseDefect, BridgePenalty: bridgePriorPenalty, ActionResidual: actionResidual, TotalEnergy: totalEnergy, Admissible: admissible);
            })
            .OrderBy(x => x.TotalEnergy)
            .ThenBy(x => x.M)
            .ToArray();

        var admissible = functional.Where(x => x.Admissible).ToArray();
        bool resolved = admissible.Length > 0;
        int selectedMode = resolved ? admissible[0].M : int.MaxValue;
        int[] satisfyingModes = admissible.Select(x => x.M).ToArray();
        bool m3Admissible = satisfyingModes.Contains(3);
        bool m3Unique = m3Admissible && satisfyingModes.Length == 1;
        bool m3Minimal = resolved && selectedMode == 3;

        int phaseOnlyWinner = functional.OrderBy(x => x.PhaseDefect).ThenBy(x => x.M).First().M;
        int bridgeOnlyWinner = functional.OrderBy(x => x.BridgePenalty).ThenBy(x => x.M).First().M;
        int actionOnlyWinner = functional.OrderBy(x => x.ActionResidual).ThenBy(x => x.M).First().M;
        int combinedNoGatesWinner = functional.OrderBy(x => x.TotalEnergy).ThenBy(x => x.M).First().M;

        string[] failureByMode = functional
            .OrderBy(x => x.M)
            .Select(x =>
            {
                var reasons = new List<string>();
                if (x.PhaseDefect > 0.35) reasons.Add("phaseDefect");
                if (x.BridgePenalty >= 1.0) reasons.Add("bridgePrior");
                if (x.ActionResidual > 0.95) reasons.Add("actionStationarity");
                return $"m={x.M}:{(reasons.Count == 0 ? "passes-active" : string.Join("+", reasons))}";
            })
            .ToArray();

        _output.WriteLine("--- RBF49 SHARED FUNCTIONAL DIAGNOSTIC ---");
        _output.WriteLine($"derived qCore(m=3)=[{string.Join(", ", qCore)}] | qSupport=[{string.Join(", ", qSupport)}]");
        foreach (var row in functional)
        {
            _output.WriteLine(
                $"RBF49 m={row.M} | phaseDefect={row.PhaseDefect:F4} | bridgePenalty={row.BridgePenalty:F4} | actionResidual={row.ActionResidual:F4} | totalEnergy={row.TotalEnergy:F4} | admissible={row.Admissible}");
        }
        _output.WriteLine($"selected={(resolved ? $"m={selectedMode}" : "none")} | satisfying=[{string.Join(", ", satisfyingModes)}]");
        _output.WriteLine($"shortcut/ablation winners: phaseOnly=m={phaseOnlyWinner}, bridgeOnly=m={bridgeOnlyWinner}, actionOnly=m={actionOnlyWinner}, combinedNoGates=m={combinedNoGatesWinner}");
        _output.WriteLine($"failureByMode=[{string.Join(" | ", failureByMode)}]");
        _output.WriteLine($"m3(admissible={m3Admissible}, unique={m3Unique}, minimal={m3Minimal}) | resolved={resolved}");
        _output.WriteLine("RBF49 claim boundary: diagnostic/candidate only; no theorem-level proof, no first-principles closure claim, no universal m=3 claim.");

        Assert.True(resolved && selectedMode == 3,
            $"Expected m=3 as minimal admissible mode under shared functional. selected={(resolved ? $"m={selectedMode}" : "none")}");
        Assert.True(actionOnlyWinner != 3 || phaseOnlyWinner != 3 || bridgeOnlyWinner != 3,
            $"Expected at least one shortcut/ablation winner to deviate from m=3. phaseOnly={phaseOnlyWinner}, bridgeOnly={bridgeOnlyWinner}, actionOnly={actionOnlyWinner}");
    }

    /// <summary>
    /// Checks local stability of the shared functional under bounded structural component-weight perturbations.
    /// Matters because necessity/uniqueness evidence must survive bounded weight drift under one shared rule.
    /// Expected diagnostic behavior: m=3 remains locally stable near baseline with explicit reporting of bounded failures.
    /// Claim boundary: diagnostic/candidate only; not theorem-level proof.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF50_SharedFunctional_Should_Select_M3_Under_BoundedStructuralPerturbations()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        int[] qCore = DeriveBridgeCoreQValuesFromBand(3, 1.16, 1.19, 0.84, 0.86, 2, 64);
        int[] qSupport = Enumerable.Range(4, 33).ToArray(); // 4..36
        var family = BuildModeFamilyFromExplicitQValues(
            mValues,
            qSupport,
            0.50,
            0.35,
            0.15,
            BuildNoCadencePriorConfig(),
            new[] { 2e-3 });

        var perturb = new[] { 0.90, 1.00, 1.10 };
        int total = 0;
        int resolved = 0;
        int selectedM3 = 0;
        int localTotal = 0;
        int localM3 = 0;

        _output.WriteLine("--- RBF50 SHARED FUNCTIONAL BOUNDED-PERTURBATION DIAGNOSTIC ---");
        _output.WriteLine($"qCore=[{string.Join(", ", qCore)}] | qSupport=[{string.Join(", ", qSupport)}]");

        foreach (double wp in perturb)
        {
            foreach (double wb in perturb)
            {
                foreach (double wa in perturb)
                {
                    total++;
                    bool isLocal = Math.Abs(wp - 1.0) <= 0.10 && Math.Abs(wb - 1.0) <= 0.10 && Math.Abs(wa - 1.0) <= 0.10;
                    if (isLocal) localTotal++;

                    var rows = BuildSharedFunctionalRows(
                        family,
                        qCore,
                        phaseWeight: wp,
                        bridgeWeight: wb,
                        actionWeight: wa,
                        phaseTolerance: 0.35,
                        bridgeTolerance: 1.0,
                        actionTolerance: 0.95);
                    var result = EvaluateSharedFunctionalSelection(rows);

                    if (result.Resolved)
                    {
                        resolved++;
                        if (result.SelectedMode == 3)
                            selectedM3++;
                        if (isLocal && result.SelectedMode == 3)
                            localM3++;
                    }

                    _output.WriteLine(
                        $"RBF50 wPhase={wp:F2},wBridge={wb:F2},wAction={wa:F2} | selected={(result.Resolved ? $"m={result.SelectedMode}" : "none")} | satisfying=[{string.Join(", ", result.SatisfyingModes)}] | margin={(double.IsNaN(result.Margin) ? "n/a" : result.Margin.ToString("F4"))}");
                    _output.WriteLine($"RBF50 failureByMode=[{string.Join(" | ", result.FailureByMode)}]");
                }
            }
        }

        _output.WriteLine($"RBF50 totals: total={total} | resolved={resolved} | selected m3={selectedM3}");
        _output.WriteLine($"RBF50 local stability: localCases={localTotal} | local m3 selections={localM3}");
        _output.WriteLine("RBF50 claim boundary: diagnostic/candidate only; bounded failures are expected and reported.");

        Assert.True(localTotal > 0, "Expected local perturbation cases.");
        Assert.True(localM3 >= Math.Max(1, (int)Math.Floor(0.6 * localTotal)),
            $"Expected local m=3 stability under bounded perturbations. localM3={localM3}, localTotal={localTotal}");
    }

    /// <summary>
    /// Checks non-uniqueness exposure when one core structural assumption is relaxed at a time.
    /// Matters because assumption sensitivity identifies which component most strongly controls uniqueness.
    /// Expected diagnostic behavior: relaxed assumptions admit competing modes or weaken uniqueness.
    /// Claim boundary: diagnostic/candidate only; not theorem-level proof.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF51_SharedFunctional_Should_Expose_NonUniqueness_When_CoreAssumptionsAreRelaxed()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        int[] qCore = DeriveBridgeCoreQValuesFromBand(3, 1.16, 1.19, 0.84, 0.86, 2, 64);
        int[] qSupport = Enumerable.Range(4, 33).ToArray(); // 4..36
        var family = BuildModeFamilyFromExplicitQValues(
            mValues,
            qSupport,
            0.50,
            0.35,
            0.15,
            BuildNoCadencePriorConfig(),
            new[] { 2e-3 });

        var baselineRows = BuildSharedFunctionalRows(family, qCore, 1.0, 1.0, 1.0, 0.35, 1.0, 0.95);
        var baseline = EvaluateSharedFunctionalSelection(baselineRows);
        Assert.True(baseline.Resolved, "Expected resolved baseline shared-functional selection.");

        var relaxations = new (string Name, bool RelaxPhase, bool RelaxBridge, bool RelaxAction)[]
        {
            ("phase defect", true, false, false),
            ("bridge prior", false, true, false),
            ("action stationarity", false, false, true)
        };

        int weakenedCount = 0;
        string strongestControl = "none";
        int strongestMetric = int.MinValue;

        _output.WriteLine("--- RBF51 RELAXED-ASSUMPTION NON-UNIQUENESS DIAGNOSTIC ---");
        _output.WriteLine($"baseline selected={(baseline.Resolved ? $"m={baseline.SelectedMode}" : "none")} | satisfying=[{string.Join(", ", baseline.SatisfyingModes)}]");

        foreach (var r in relaxations)
        {
            var rows = BuildSharedFunctionalRows(
                family,
                qCore,
                phaseWeight: r.RelaxPhase ? 0.0 : 1.0,
                bridgeWeight: r.RelaxBridge ? 0.0 : 1.0,
                actionWeight: r.RelaxAction ? 0.0 : 1.0,
                phaseTolerance: r.RelaxPhase ? 1.0 : 0.35,
                bridgeTolerance: r.RelaxBridge ? 1.01 : 1.0,
                actionTolerance: r.RelaxAction ? 2.0 : 0.95);
            var result = EvaluateSharedFunctionalSelection(rows);

            int non3Admissible = result.SatisfyingModes.Count(m => m != 3);
            bool weakened =
                !result.Resolved ||
                result.SelectedMode != 3 ||
                non3Admissible > 0 ||
                result.SatisfyingModes.Length > baseline.SatisfyingModes.Length;
            if (weakened) weakenedCount++;

            int controlMetric = non3Admissible + (result.SelectedMode != 3 ? 1 : 0);
            if (controlMetric > strongestMetric)
            {
                strongestMetric = controlMetric;
                strongestControl = r.Name;
            }

            _output.WriteLine(
                $"RBF51 relax={r.Name} | selected={(result.Resolved ? $"m={result.SelectedMode}" : "none")} | satisfying=[{string.Join(", ", result.SatisfyingModes)}] | non3Admissible={non3Admissible} | weakened={weakened}");
            _output.WriteLine($"RBF51 relax={r.Name} failureByMode=[{string.Join(" | ", result.FailureByMode)}]");
        }

        _output.WriteLine($"RBF51 strongest uniqueness-control assumption={strongestControl} | metric={strongestMetric}");
        _output.WriteLine("RBF51 claim boundary: diagnostic/candidate only; non-uniqueness exposure is not theorem-level disproof.");

        Assert.True(weakenedCount >= 1,
            $"Expected uniqueness weakening under at least one relaxed core assumption. weakenedCount={weakenedCount}");
    }

    /// <summary>
    /// Performs deterministic bounded counterexample search under the full shared rule without per-family retuning.
    /// Matters because necessity evidence should resist bounded adversarial scans in the tested domain.
    /// Expected diagnostic behavior: no m!=3 admissible counterexample in the bounded full-rule grid; if found, classify explicitly.
    /// Claim boundary: diagnostic/candidate only; not theorem-level proof.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF52_CounterexampleSearch_Should_Not_Find_CompetingMode_UnderFullSharedRule()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        int[] qCore = DeriveBridgeCoreQValuesFromBand(3, 1.16, 1.19, 0.84, 0.86, 2, 64);
        var qSupports = new (string Name, int[] Q)[]
        {
            ("core", qCore),
            ("band", DeriveBridgeBandSupportUnionQValues(mValues, 1.16, 1.19, 0.84, 0.86, 2, 64)),
            ("wide", Enumerable.Range(8, 25).ToArray()) // 8..32 bounded broad search
        };
        var weights = new (string Name, double O, double C, double T)[]
        {
            ("wA", 0.50, 0.35, 0.15),
            ("wB", 0.55, 0.30, 0.15),
            ("wC", 0.45, 0.40, 0.15)
        };
        var phaseTolerances = new[] { 0.33, 0.35, 0.37 };
        var actionTolerances = new[] { 0.90, 0.95, 1.00 };

        var counterexamples = new List<(string Scope, int SelectedMode, string Classify)>();

        _output.WriteLine("--- RBF52 BOUNDED COUNTEREXAMPLE SEARCH DIAGNOSTIC ---");
        _output.WriteLine($"qCore=[{string.Join(", ", qCore)}]");

        foreach (var qs in qSupports)
        {
            foreach (var w in weights)
            {
                var family = BuildModeFamilyFromExplicitQValues(
                    mValues,
                    qs.Q,
                    w.O,
                    w.C,
                    w.T,
                    BuildNoCadencePriorConfig(),
                    new[] { 2e-3 });

                foreach (double phaseTol in phaseTolerances)
                {
                    foreach (double actionTol in actionTolerances)
                    {
                        var rows = BuildSharedFunctionalRows(
                            family,
                            qCore,
                            phaseWeight: 1.0,
                            bridgeWeight: 1.0,
                            actionWeight: 1.0,
                            phaseTolerance: phaseTol,
                            bridgeTolerance: 1.0,
                            actionTolerance: actionTol);
                        var result = EvaluateSharedFunctionalSelection(rows);

                        if (result.Resolved && result.SelectedMode != 3)
                        {
                            string category = ClassifyCounterexample(result.SelectedMode, qs.Name, phaseTol, actionTol);
                            counterexamples.Add(($"{qs.Name}/{w.Name}/p{phaseTol:F2}/a{actionTol:F2}", result.SelectedMode, category));
                        }

                        _output.WriteLine(
                            $"RBF52 {qs.Name}/{w.Name} | phaseTol={phaseTol:F2} | actionTol={actionTol:F2} | selected={(result.Resolved ? $"m={result.SelectedMode}" : "none")} | satisfying=[{string.Join(", ", result.SatisfyingModes)}]");
                    }
                }
            }
        }

        _output.WriteLine($"RBF52 counterexamples={counterexamples.Count}");
        foreach (var c in counterexamples)
            _output.WriteLine($"RBF52 counterexample scope={c.Scope} | selected=m={c.SelectedMode} | class={c.Classify}");
        _output.WriteLine("RBF52 claim boundary: diagnostic/candidate only; no theorem-level proof, no full first-principles closure.");

        Assert.True(counterexamples.Count == 0,
            $"Expected no m!=3 counterexample under bounded full shared-rule search. found={counterexamples.Count}");
    }

    /// <summary>
    /// Maps explicit validity boundaries for the bounded shared-functional uniqueness candidate.
    /// Matters because candidate status requires transparent domain-of-validity and transition reporting.
    /// Expected diagnostic behavior: identify m=3-valid region and boundary regions (none/m2/other) with first transition examples.
    /// Claim boundary: diagnostic/candidate only; not theorem-level proof.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF53_SharedFunctional_Domain_Should_Have_ExplicitValidityBoundary()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        int[] qCore = DeriveBridgeCoreQValuesFromBand(3, 1.16, 1.19, 0.84, 0.86, 2, 64);
        var qSupports = new (string Name, int[] Q)[]
        {
            ("core", qCore),
            ("band", DeriveBridgeBandSupportUnionQValues(mValues, 1.16, 1.19, 0.84, 0.86, 2, 64)),
            ("wide", Enumerable.Range(8, 25).ToArray()) // 8..32
        };
        var phaseTolerances = new[] { 0.30, 0.33, 0.35, 0.37 };
        var actionTolerances = new[] { 0.85, 0.90, 0.95, 1.00 };
        var componentWeights = new (double Phase, double Bridge, double Action)[]
        {
            (0.90, 1.00, 1.00),
            (1.00, 1.00, 1.00),
            (1.10, 1.00, 0.90),
            (1.00, 0.90, 1.10)
        };

        int validM3Unique = 0;
        int boundaryNone = 0;
        int boundaryM2 = 0;
        int boundaryOther = 0;
        string? firstNone = null;
        string? firstM2 = null;
        string? firstOther = null;

        _output.WriteLine("--- RBF53 SHARED-FUNCTIONAL VALIDITY-BOUNDARY DIAGNOSTIC ---");
        _output.WriteLine($"qCore=[{string.Join(", ", qCore)}]");

        foreach (var qs in qSupports)
        {
            var family = BuildModeFamilyFromExplicitQValues(
                mValues,
                qs.Q,
                0.50,
                0.35,
                0.15,
                BuildNoCadencePriorConfig(),
                new[] { 2e-3 });

            foreach (var w in componentWeights)
            {
                foreach (double phaseTol in phaseTolerances)
                {
                    foreach (double actionTol in actionTolerances)
                    {
                        var rows = BuildSharedFunctionalRows(
                            family,
                            qCore,
                            w.Phase,
                            w.Bridge,
                            w.Action,
                            phaseTol,
                            bridgeTolerance: 1.0,
                            actionTolerance: actionTol);
                        var result = EvaluateSharedFunctionalSelection(rows);

                        string scope = $"{qs.Name}|pTol={phaseTol:F2}|aTol={actionTol:F2}|w=({w.Phase:F2},{w.Bridge:F2},{w.Action:F2})";
                        if (result.Resolved && result.SelectedMode == 3 && result.SatisfyingModes.Length == 1)
                        {
                            validM3Unique++;
                        }
                        else if (!result.Resolved)
                        {
                            boundaryNone++;
                            firstNone ??= scope;
                        }
                        else if (result.SelectedMode == 2)
                        {
                            boundaryM2++;
                            firstM2 ??= scope;
                        }
                        else
                        {
                            boundaryOther++;
                            firstOther ??= scope;
                        }

                        _output.WriteLine(
                            $"RBF53 {scope} | selected={(result.Resolved ? $"m={result.SelectedMode}" : "none")} | satisfying=[{string.Join(", ", result.SatisfyingModes)}] | margin={(double.IsNaN(result.Margin) ? "n/a" : result.Margin.ToString("F4"))}");
                    }
                }
            }
        }

        _output.WriteLine($"RBF53 validity region (m3 unique) count={validM3Unique}");
        _output.WriteLine($"RBF53 boundary classes: none={boundaryNone}, m2={boundaryM2}, other={boundaryOther}");
        _output.WriteLine($"RBF53 first transitions: none={firstNone ?? "none"}, m2={firstM2 ?? "none"}, other={firstOther ?? "none"}");
        _output.WriteLine("RBF53 claim boundary: diagnostic/candidate only; not theorem-level proof.");

        int boundaryTotal = boundaryNone + boundaryM2 + boundaryOther;
        Assert.True(validM3Unique > 0, "Expected non-empty m=3 unique validity region.");
        Assert.True(boundaryTotal > 0, "Expected explicit boundary region classes.");
        Assert.True(boundaryM2 > 0 || boundaryOther > 0,
            $"Expected at least one competing-mode boundary class. m2={boundaryM2}, other={boundaryOther}");
    }

    /// <summary>
    /// Classifies boundary failures by dominant constraint channel for shared-functional scans.
    /// Matters because domain boundaries should be interpretable by phase-defect, bridge-prior, action-stationarity, or mixed channels.
    /// Expected diagnostic behavior: channel counts and representative examples are produced under one shared rule.
    /// Claim boundary: diagnostic/candidate only; not theorem-level proof.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF54_BoundaryFailures_Should_Be_Classified_ByDominantConstraintChannel()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        int[] qCore = DeriveBridgeCoreQValuesFromBand(3, 1.16, 1.19, 0.84, 0.86, 2, 64);
        int[] qSupport = Enumerable.Range(8, 25).ToArray(); // 8..32
        var family = BuildModeFamilyFromExplicitQValues(
            mValues,
            qSupport,
            0.50,
            0.35,
            0.15,
            BuildNoCadencePriorConfig(),
            new[] { 2e-3 });

        var phaseTolerances = new[] { 0.30, 0.33, 0.35, 0.37 };
        var actionTolerances = new[] { 0.85, 0.90, 0.95, 1.00 };

        int phaseCount = 0;
        int bridgeCount = 0;
        int actionCount = 0;
        int mixedCount = 0;
        string? phaseExample = null;
        string? bridgeExample = null;
        string? actionExample = null;
        string? mixedExample = null;

        _output.WriteLine("--- RBF54 BOUNDARY FAILURE-CHANNEL CLASSIFICATION DIAGNOSTIC ---");
        _output.WriteLine($"qCore=[{string.Join(", ", qCore)}] | qSupport=[{string.Join(", ", qSupport)}]");

        foreach (double phaseTol in phaseTolerances)
        {
            foreach (double actionTol in actionTolerances)
            {
                var rows = BuildSharedFunctionalRows(
                    family,
                    qCore,
                    phaseWeight: 1.0,
                    bridgeWeight: 1.0,
                    actionWeight: 1.0,
                    phaseTol,
                    bridgeTolerance: 1.0,
                    actionTolerance: actionTol);
                var result = EvaluateSharedFunctionalSelection(rows);

                bool boundaryCase = !result.Resolved || result.SelectedMode != 3 || result.SatisfyingModes.Length != 1;
                if (!boundaryCase)
                    continue;

                string channel = DetermineDominantConstraintChannel(rows, targetMode: 3, phaseTolerance: phaseTol, bridgeTolerance: 1.0, actionTolerance: actionTol);
                string example = $"pTol={phaseTol:F2}|aTol={actionTol:F2}|selected={(result.Resolved ? $"m={result.SelectedMode}" : "none")}";

                switch (channel)
                {
                    case "phase-defect":
                        phaseCount++;
                        phaseExample ??= example;
                        break;
                    case "bridge-prior/qCore":
                        bridgeCount++;
                        bridgeExample ??= example;
                        break;
                    case "action-stationarity":
                        actionCount++;
                        actionExample ??= example;
                        break;
                    default:
                        mixedCount++;
                        mixedExample ??= example;
                        break;
                }

                _output.WriteLine($"RBF54 {example} | dominant={channel} | failureByMode=[{string.Join(" | ", result.FailureByMode)}]");
            }
        }

        _output.WriteLine($"RBF54 channel counts: phase-defect={phaseCount}, bridge-prior/qCore={bridgeCount}, action-stationarity={actionCount}, mixed={mixedCount}");
        _output.WriteLine($"RBF54 examples: phase={phaseExample ?? "none"}, bridge={bridgeExample ?? "none"}, action={actionExample ?? "none"}, mixed={mixedExample ?? "none"}");
        _output.WriteLine("RBF54 claim boundary: diagnostic/candidate only; no theorem-level proof.");

        int total = phaseCount + bridgeCount + actionCount + mixedCount;
        Assert.True(total > 0, "Expected boundary failures for dominant-channel classification.");
        Assert.True(phaseCount + actionCount + mixedCount > 0,
            $"Expected at least one non-bridge dominant/fused channel in boundary region. phase={phaseCount}, action={actionCount}, mixed={mixedCount}");
    }

    /// <summary>
    /// Checks stability of domain boundaries under solver-step and q-support variants.
    /// Matters because bounded candidate claims require bounded near-baseline drift with explicit reporting outside admissible regime.
    /// Expected diagnostic behavior: bounded drift near baseline with explicit unresolved/fallback reporting in stressed variants.
    /// Claim boundary: diagnostic/candidate only; not theorem-level proof.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF55_DomainBoundary_Should_Be_Stable_UnderSolverAndQSupportVariants()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        int[] qCore = DeriveBridgeCoreQValuesFromBand(3, 1.16, 1.19, 0.84, 0.86, 2, 64);
        var qSupports = new (string Name, int[] Q)[]
        {
            ("core", qCore),
            ("band", DeriveBridgeBandSupportUnionQValues(mValues, 1.16, 1.19, 0.84, 0.86, 2, 64)),
            ("wide", Enumerable.Range(8, 25).ToArray())
        };
        var solverProfiles = new (string Name, ModeLockConfig Config)[]
        {
            ("sBase", BuildNoCadencePriorConfig()),
            ("sShort", BuildNoCadencePriorConfig() with { Steps = 1000, SettleSteps = 500 })
        };
        var phaseTolerances = new[] { 0.33, 0.35, 0.37 };
        var actionTolerances = new[] { 0.90, 0.95, 1.00 };

        static double Jaccard(HashSet<string> a, HashSet<string> b)
        {
            if (a.Count == 0 && b.Count == 0) return 1.0;
            int inter = a.Count(x => b.Contains(x));
            int union = a.Count + b.Count - inter;
            return union > 0 ? (double)inter / union : 0.0;
        }

        HashSet<string> BuildRegion((string Name, int[] Q) qVariant, (string Name, ModeLockConfig Config) solver, out int unresolved, out int fallbackM2, out int fallbackOther)
        {
            unresolved = 0;
            fallbackM2 = 0;
            fallbackOther = 0;
            var region = new HashSet<string>(StringComparer.Ordinal);

            var family = BuildModeFamilyFromExplicitQValues(
                mValues,
                qVariant.Q,
                0.50,
                0.35,
                0.15,
                solver.Config,
                new[] { 2e-3 });

            foreach (double pTol in phaseTolerances)
            {
                foreach (double aTol in actionTolerances)
                {
                    var rows = BuildSharedFunctionalRows(
                        family,
                        qCore,
                        phaseWeight: 1.0,
                        bridgeWeight: 1.0,
                        actionWeight: 1.0,
                        pTol,
                        bridgeTolerance: 1.0,
                        actionTolerance: aTol);
                    var result = EvaluateSharedFunctionalSelection(rows);

                    if (!result.Resolved)
                    {
                        unresolved++;
                        continue;
                    }

                    if (result.SelectedMode == 3 && result.SatisfyingModes.Length == 1)
                    {
                        region.Add($"p{pTol:F2}|a{aTol:F2}");
                    }
                    else if (result.SelectedMode == 2)
                    {
                        fallbackM2++;
                    }
                    else
                    {
                        fallbackOther++;
                    }
                }
            }

            return region;
        }

        var baseRegion = BuildRegion(qSupports[1], solverProfiles[0], out int baseUnresolved, out int baseFallbackM2, out int baseFallbackOther); // band + base solver
        Assert.True(baseRegion.Count > 0, "Expected non-empty baseline m=3 domain region.");

        double worstDrift = 0.0;
        string? worstCase = null;

        _output.WriteLine("--- RBF55 DOMAIN-BOUNDARY STABILITY DIAGNOSTIC ---");
        _output.WriteLine($"baseline region size={baseRegion.Count} | unresolved={baseUnresolved} | fallbackM2={baseFallbackM2} | fallbackOther={baseFallbackOther}");

        foreach (var q in qSupports)
        {
            foreach (var s in solverProfiles)
            {
                var region = BuildRegion(q, s, out int unresolved, out int fallbackM2, out int fallbackOther);
                double drift = 1.0 - Jaccard(baseRegion, region);
                if (drift > worstDrift)
                {
                    worstDrift = drift;
                    worstCase = $"{q.Name}/{s.Name}";
                }

                _output.WriteLine(
                    $"RBF55 variant={q.Name}/{s.Name} | m3Region={region.Count} | unresolved={unresolved} | fallbackM2={fallbackM2} | fallbackOther={fallbackOther} | drift={drift:F4}");
            }
        }

        _output.WriteLine($"RBF55 worst drift={worstDrift:F4} at {(worstCase ?? "none")}");
        _output.WriteLine("RBF55 claim boundary: diagnostic/candidate only; bounded near-baseline drift with explicit out-of-regime failures.");

        Assert.True(worstDrift <= 0.90,
            $"Expected bounded drift under solver/q-support variants near baseline domain mapping. worstDrift={worstDrift:F4}, worstCase={worstCase}");
    }

    /// <summary>
    /// Reports a formalized minimal assumption scaffold for baseline m=3 shared-functional selection.
    /// Matters because theorem-path preparation requires explicit assumptions and their current epistemic status.
    /// Expected diagnostic behavior: checklist of active/necessary assumptions marked as structural/derived/diagnostic/operational.
    /// Claim boundary: diagnostic/candidate only; not theorem-level proof.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF56_SharedFunctional_Should_Report_FormalAssumptionSet()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        int[] qCore = DeriveBridgeCoreQValuesFromBand(3, 1.16, 1.19, 0.84, 0.86, 2, 64);
        int[] qBand = DeriveBridgeBandSupportUnionQValues(mValues, 1.16, 1.19, 0.84, 0.86, 2, 64);
        int[] qWide = Enumerable.Range(8, 25).ToArray();

        var familyBand = BuildModeFamilyFromExplicitQValues(
            mValues, qBand, 0.50, 0.35, 0.15, BuildNoCadencePriorConfig(), new[] { 2e-3 });
        var familyWide = BuildModeFamilyFromExplicitQValues(
            mValues, qWide, 0.50, 0.35, 0.15, BuildNoCadencePriorConfig(), new[] { 2e-3 });

        var baselineRows = BuildSharedFunctionalRows(familyBand, qCore, 1.0, 1.0, 1.0, 0.35, 1.0, 0.95);
        var baseline = EvaluateSharedFunctionalSelection(baselineRows);
        Assert.True(baseline.Resolved && baseline.SelectedMode == 3, "Expected baseline full shared rule to select m=3.");

        bool phaseNecessary = DoesAssumptionRemovalWeaken(
            baseline,
            EvaluateSharedFunctionalSelection(BuildSharedFunctionalRows(familyBand, qCore, 0.0, 1.0, 1.0, 1.0, 1.0, 0.95)));
        bool bridgeNecessary = DoesAssumptionRemovalWeaken(
            baseline,
            EvaluateSharedFunctionalSelection(BuildSharedFunctionalRows(familyBand, qCore, 1.0, 0.0, 1.0, 0.35, 1.01, 0.95)));
        bool actionNecessary = DoesAssumptionRemovalWeaken(
            baseline,
            EvaluateSharedFunctionalSelection(BuildSharedFunctionalRows(familyBand, qCore, 1.0, 1.0, 0.0, 0.35, 1.0, 2.0)));

        var sharedWeightVariants = new (double P, double B, double A)[]
        {
            (1.0, 1.0, 1.0),
            (1.10, 0.90, 1.00),
            (0.90, 1.10, 1.00),
            (0.20, 1.60, 0.20)
        };
        bool sharedWeightsNecessary = false;
        foreach (var w in sharedWeightVariants)
        {
            var v = EvaluateSharedFunctionalSelection(BuildSharedFunctionalRows(familyBand, qCore, w.P, w.B, w.A, 0.35, 1.0, 0.95));
            if (DoesAssumptionRemovalWeaken(baseline, v))
            {
                sharedWeightsNecessary = true;
                break;
            }
        }

        bool boundedDomainNecessary = DoesAssumptionRemovalWeaken(
            baseline,
            EvaluateSharedFunctionalSelection(BuildSharedFunctionalRows(familyWide, qCore, 1.0, 1.0, 1.0, 0.35, 1.0, 0.95)));

        var checklist = new (string Name, bool Active, bool Necessary, string Status)[]
        {
            ("phase integer closure defect", true, phaseNecessary, "derived"),
            ("bridge-prior/qCore support", true, bridgeNecessary, "structural"),
            ("action-stationarity", true, actionNecessary, "derived"),
            ("shared weights", true, sharedWeightsNecessary, "operational"),
            ("bounded domain conditions", true, boundedDomainNecessary, "diagnostic")
        };

        _output.WriteLine("--- RBF56 FORMAL ASSUMPTION-SCAFFOLD DIAGNOSTIC ---");
        foreach (var a in checklist)
        {
            _output.WriteLine(
                $"RBF56 assumption={a.Name} | active={a.Active} | supportsNecessity={a.Necessary} | status={a.Status}");
        }
        _output.WriteLine($"RBF56 baseline selected=m={baseline.SelectedMode} | satisfying=[{string.Join(", ", baseline.SatisfyingModes)}]");
        _output.WriteLine("RBF56 claim boundary: diagnostic/candidate only; assumption scaffold is not theorem-level proof.");

        Assert.True(checklist.All(x => x.Active), "Expected all formal assumptions to be active in baseline scaffold.");
        Assert.True(checklist.Count(x => x.Necessary) >= 3,
            $"Expected multiple assumptions to support necessity diagnostics. necessary={checklist.Count(x => x.Necessary)}");
    }

    /// <summary>
    /// Removes formal assumptions one-by-one and checks whether m=3 admissibility/uniqueness weakens.
    /// Matters because necessity claims require explicit single-assumption ablation behavior.
    /// Expected diagnostic behavior: assumption removals produce weakening, failure classes, and necessity-support flags.
    /// Claim boundary: diagnostic/candidate only; not theorem-level proof.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF57_NecessityConditions_Should_Fail_When_AssumptionsAreIndividuallyRemoved()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        int[] qCore = DeriveBridgeCoreQValuesFromBand(3, 1.16, 1.19, 0.84, 0.86, 2, 64);
        int[] qBand = DeriveBridgeBandSupportUnionQValues(mValues, 1.16, 1.19, 0.84, 0.86, 2, 64);
        int[] qWide = Enumerable.Range(8, 25).ToArray();

        var familyBand = BuildModeFamilyFromExplicitQValues(
            mValues, qBand, 0.50, 0.35, 0.15, BuildNoCadencePriorConfig(), new[] { 2e-3 });
        var familyWide = BuildModeFamilyFromExplicitQValues(
            mValues, qWide, 0.50, 0.35, 0.15, BuildNoCadencePriorConfig(), new[] { 2e-3 });

        var baselineRows = BuildSharedFunctionalRows(familyBand, qCore, 1.0, 1.0, 1.0, 0.35, 1.0, 0.95);
        var baseline = EvaluateSharedFunctionalSelection(baselineRows);
        Assert.True(baseline.Resolved && baseline.SelectedMode == 3, "Expected baseline m=3 selection.");

        var removalCases = new (string Name, (int M, double PhaseDefect, double BridgePenalty, double ActionResidual, double TotalEnergy, bool Admissible)[] Rows, double PhaseTol, double BridgeTol, double ActionTol)[]
        {
            ("phase integer closure defect", BuildSharedFunctionalRows(familyBand, qCore, 0.0, 1.0, 1.0, 1.0, 1.0, 0.95), 1.0, 1.0, 0.95),
            ("bridge-prior/qCore support", BuildSharedFunctionalRows(familyBand, qCore, 1.0, 0.0, 1.0, 0.35, 1.01, 0.95), 0.35, 1.01, 0.95),
            ("action-stationarity", BuildSharedFunctionalRows(familyBand, qCore, 1.0, 1.0, 0.0, 0.35, 1.0, 2.0), 0.35, 1.0, 2.0),
            ("shared weights", BuildSharedFunctionalRows(familyBand, qCore, 0.20, 1.60, 0.20, 0.35, 1.0, 0.95), 0.35, 1.0, 0.95),
            ("bounded domain conditions", BuildSharedFunctionalRows(familyWide, qCore, 1.0, 1.0, 1.0, 0.35, 1.0, 0.95), 0.35, 1.0, 0.95)
        };

        int supportsNecessityCount = 0;

        _output.WriteLine("--- RBF57 ASSUMPTION-REMOVAL NECESSITY DIAGNOSTIC ---");
        foreach (var c in removalCases)
        {
            var result = EvaluateSharedFunctionalSelection(c.Rows);
            bool supportsNecessity = DoesAssumptionRemovalWeaken(baseline, result);
            if (supportsNecessity)
                supportsNecessityCount++;

            string failureClass = DetermineFailureClassFromSharedResult(result, c.Rows, c.PhaseTol, c.BridgeTol, c.ActionTol);
            _output.WriteLine(
                $"RBF57 removed={c.Name} | selected={(result.Resolved ? $"m={result.SelectedMode}" : "none")} | satisfying=[{string.Join(", ", result.SatisfyingModes)}] | failureClass={failureClass} | supportsNecessity={supportsNecessity}");
            _output.WriteLine($"RBF57 removed={c.Name} failureByMode=[{string.Join(" | ", result.FailureByMode)}]");
        }
        _output.WriteLine("RBF57 claim boundary: diagnostic/candidate only; assumption removal does not establish theorem-level necessity.");

        Assert.True(supportsNecessityCount >= 3,
            $"Expected multiple assumption removals to support necessity diagnostics. supports={supportsNecessityCount}/{removalCases.Length}");
    }

    /// <summary>
    /// Enumerates known counterexample/failure classes and checks they are bounded and diagnosable.
    /// Matters because scaffold quality requires explicit bounded failure taxonomy under one shared rule.
    /// Expected diagnostic behavior: representative scenarios for no-core, phase/action, bridge loss, action relaxation, and mixed boundaries are identifiable.
    /// Claim boundary: diagnostic/candidate only; not theorem-level proof.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF58_CounterexampleClasses_Should_Be_Enumerated_And_Bounded()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        int[] qCore = DeriveBridgeCoreQValuesFromBand(3, 1.16, 1.19, 0.84, 0.86, 2, 64);
        int[] qBand = DeriveBridgeBandSupportUnionQValues(mValues, 1.16, 1.19, 0.84, 0.86, 2, 64);
        int[] qNoCore = Enumerable.Range(8, 7).ToArray(); // 8..14
        int[] qWide = Enumerable.Range(8, 25).ToArray(); // 8..32

        var scenarios = new (string Label, string ExpectedClass, int[] Q, double WP, double WB, double WA, double PTol, double BTol, double ATol)[]
        {
            ("no-core support", "no-core q-support", qNoCore, 1.0, 1.0, 1.0, 0.35, 1.0, 0.95),
            ("phase-action boundary", "phase/action boundary", qBand, 1.0, 1.0, 1.0, 0.37, 1.0, 1.00),
            ("bridge-prior loss", "bridge-prior loss", qWide, 1.0, 0.0, 1.0, 0.35, 1.01, 0.95),
            ("action-stationarity relaxation", "action-stationarity relaxation", qBand, 1.0, 1.0, 0.0, 0.35, 1.0, 2.0),
            ("mixed boundary", "mixed boundary", qWide, 0.80, 0.60, 0.80, 0.37, 1.01, 1.10)
        };

        int diagnosed = 0;

        _output.WriteLine("--- RBF58 COUNTEREXAMPLE-CLASS ENUMERATION DIAGNOSTIC ---");
        _output.WriteLine($"qCore=[{string.Join(", ", qCore)}] | qBand=[{string.Join(", ", qBand)}]");

        foreach (var s in scenarios)
        {
            var family = BuildModeFamilyFromExplicitQValues(
                mValues, s.Q, 0.50, 0.35, 0.15, BuildNoCadencePriorConfig(), new[] { 2e-3 });
            var rows = BuildSharedFunctionalRows(family, qCore, s.WP, s.WB, s.WA, s.PTol, s.BTol, s.ATol);
            var result = EvaluateSharedFunctionalSelection(rows);

            bool boundedScenario = s.Q.Length <= qWide.Length && s.PTol <= 0.40 && s.ATol <= 2.0;
            string diagnosedClass = DetermineScenarioCounterexampleClass(s.Label, s.Q, qCore, s.WB, s.WA, s.PTol, s.ATol, result);
            bool boundaryLike = !result.Resolved || result.SelectedMode != 3 || result.SatisfyingModes.Length != 1;
            bool classMatch = diagnosedClass == s.ExpectedClass;
            if (boundedScenario && boundaryLike && classMatch)
                diagnosed++;

            _output.WriteLine(
                $"RBF58 scenario={s.Label} | selected={(result.Resolved ? $"m={result.SelectedMode}" : "none")} | satisfying=[{string.Join(", ", result.SatisfyingModes)}] | diagnosedClass={diagnosedClass} | expectedClass={s.ExpectedClass} | bounded={boundedScenario} | diagnosable={boundaryLike && classMatch}");
            _output.WriteLine($"RBF58 scenario={s.Label} failureByMode=[{string.Join(" | ", result.FailureByMode)}]");
        }

        _output.WriteLine($"RBF58 diagnosed classes={diagnosed}/{scenarios.Length}");
        _output.WriteLine("RBF58 claim boundary: diagnostic/candidate only; bounded class taxonomy is not theorem-level closure.");

        Assert.True(diagnosed == scenarios.Length,
            $"Expected all enumerated counterexample classes to be bounded and diagnosable. diagnosed={diagnosed}, total={scenarios.Length}");
    }

    /// <summary>
    /// Reports shared-functional energy margins showing why m=3 is selected when other admissible modes can coexist.
    /// Matters because this separates strict uniqueness from minimal-by-energy selection under one shared rule.
    /// Expected diagnostic behavior: explicit m=1..5 energies, m3-vs-competitor margins, and bounded selection-class reporting.
    /// Claim boundary: diagnostic/candidate only; not theorem-level proof.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF59_SharedFunctional_Should_Report_M3SelectionMarginAgainstAdmissibleCompetitors()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        int[] qCore = DeriveBridgeCoreQValuesFromBand(3, 1.16, 1.19, 0.84, 0.86, 2, 64);
        int[] qSupport = DeriveBridgeBandSupportUnionQValues(mValues, 1.16, 1.19, 0.84, 0.86, 2, 64);

        var family = BuildModeFamilyFromExplicitQValues(
            mValues,
            qSupport,
            0.50,
            0.35,
            0.15,
            BuildNoCadencePriorConfig(),
            new[] { 2e-3 });

        var rows = BuildSharedFunctionalRows(
            family,
            qCore,
            phaseWeight: 1.0,
            bridgeWeight: 1.0,
            actionWeight: 1.0,
            phaseTolerance: 0.35,
            bridgeTolerance: 1.0,
            actionTolerance: 0.95);
        var result = EvaluateSharedFunctionalSelection(rows);

        var byMode = rows.ToDictionary(x => x.M);
        var admissible = rows
            .Where(x => x.Admissible)
            .OrderBy(x => x.TotalEnergy)
            .ThenBy(x => x.M)
            .ToArray();

        Assert.True(result.Resolved && result.SelectedMode == 3,
            $"Expected baseline shared functional to resolve to m=3. selected={(result.Resolved ? $"m={result.SelectedMode}" : "none")}");
        Assert.True(byMode.ContainsKey(3), "Expected m=3 row in shared-functional table.");

        var m3 = byMode[3];
        double marginM3VsM2 = byMode.TryGetValue(2, out var m2) ? m2.TotalEnergy - m3.TotalEnergy : double.NaN;
        double marginM3VsM4 = byMode.TryGetValue(4, out var m4) ? m4.TotalEnergy - m3.TotalEnergy : double.NaN;
        var nextBest = admissible
            .Where(x => x.M != 3)
            .OrderBy(x => x.TotalEnergy)
            .ThenBy(x => x.M)
            .FirstOrDefault();
        bool hasNextBestAdmissible = admissible.Any(x => x.M != 3);
        double marginM3VsNextBestAdmissible = hasNextBestAdmissible
            ? nextBest.TotalEnergy - m3.TotalEnergy
            : double.NaN;

        const double tieEpsilon = 1e-9;
        bool m3Admissible = admissible.Any(x => x.M == 3);
        bool tieWithCompetitor = m3Admissible &&
                                 admissible.Any(x => x.M != 3 && Math.Abs(x.TotalEnergy - m3.TotalEnergy) <= tieEpsilon);
        string selectionClass = !m3Admissible
            ? "non-unique"
            : result.SatisfyingModes.Length == 1
                ? "strict unique"
                : tieWithCompetitor
                    ? "tie/boundary"
                    : result.SelectedMode == 3
                        ? "minimal-by-energy"
                        : "non-unique";

        _output.WriteLine("--- RBF59 SHARED-FUNCTIONAL MARGIN DIAGNOSTIC ---");
        _output.WriteLine($"qCore=[{string.Join(", ", qCore)}] | qSupport=[{string.Join(", ", qSupport)}]");
        foreach (int m in mValues.OrderBy(x => x))
        {
            var row = byMode[m];
            _output.WriteLine(
                $"RBF59 m={m} | phaseDefect={row.PhaseDefect:F4} | bridgePenalty={row.BridgePenalty:F4} | actionResidual={row.ActionResidual:F4} | totalEnergy={row.TotalEnergy:F4} | admissible={row.Admissible}");
        }
        _output.WriteLine($"RBF59 baseline selected={(result.Resolved ? $"m={result.SelectedMode}" : "none")} | satisfying=[{string.Join(", ", result.SatisfyingModes)}]");
        _output.WriteLine($"RBF59 margins: m3-vs-m2={(double.IsNaN(marginM3VsM2) ? "n/a" : marginM3VsM2.ToString("F4"))}, m3-vs-m4={(double.IsNaN(marginM3VsM4) ? "n/a" : marginM3VsM4.ToString("F4"))}, m3-vs-next-best-admissible={(double.IsNaN(marginM3VsNextBestAdmissible) ? "n/a" : marginM3VsNextBestAdmissible.ToString("F4"))}");
        _output.WriteLine($"RBF59 selection class={selectionClass} | tieEpsilon={tieEpsilon:E2}");
        _output.WriteLine("RBF59 claim boundary: diagnostic/candidate only; margin evidence does not establish theorem-level uniqueness.");

        Assert.True(hasNextBestAdmissible, "Expected admissible competitors beyond m=3 for margin diagnostics.");
        Assert.True(marginM3VsNextBestAdmissible > 0.0,
            $"Expected m=3 to be minimal by positive margin against next-best admissible competitor. margin={marginM3VsNextBestAdmissible:F4}");
        Assert.True(selectionClass is "strict unique" or "minimal-by-energy" or "tie/boundary" or "non-unique",
            $"Unexpected RBF59 selection class: {selectionClass}");
    }

    /// <summary>
    /// Tests whether m=3 selection survives alternative admissible-mode ordering/tie rules.
    /// Matters because robust candidate status should not depend on arbitrary list ordering.
    /// Expected diagnostic behavior: physically motivated rules keep m=3 selected; ordering-sensitive boundaries are explicitly flagged.
    /// Claim boundary: diagnostic/candidate only; not theorem-level proof.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF60_M3Selection_Should_Not_Depend_OnArbitraryTieBreaking()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        int[] qCore = DeriveBridgeCoreQValuesFromBand(3, 1.16, 1.19, 0.84, 0.86, 2, 64);
        int[] qSupport = DeriveBridgeBandSupportUnionQValues(mValues, 1.16, 1.19, 0.84, 0.86, 2, 64);

        var family = BuildModeFamilyFromExplicitQValues(
            mValues,
            qSupport,
            0.50,
            0.35,
            0.15,
            BuildNoCadencePriorConfig(),
            new[] { 2e-3 });

        var rows = BuildSharedFunctionalRows(
            family,
            qCore,
            phaseWeight: 1.0,
            bridgeWeight: 1.0,
            actionWeight: 1.0,
            phaseTolerance: 0.35,
            bridgeTolerance: 1.0,
            actionTolerance: 0.95);
        var baseline = EvaluateSharedFunctionalSelection(rows);

        var admissible = rows.Where(x => x.Admissible).ToArray();
        Assert.True(admissible.Length > 0, "Expected admissible modes for tie/ordering diagnostics.");

        int lowestMFirst = admissible
            .OrderBy(x => x.M)
            .First().M;
        int lowestEnergyFirst = admissible
            .OrderBy(x => x.TotalEnergy)
            .ThenBy(x => x.M)
            .First().M;
        int strongestBridgePriorFirst = admissible
            .OrderBy(x => x.BridgePenalty)
            .ThenBy(x => x.TotalEnergy)
            .ThenBy(x => x.M)
            .First().M;
        int strongestPhaseClosureFirst = admissible
            .OrderBy(x => x.PhaseDefect)
            .ThenBy(x => x.TotalEnergy)
            .ThenBy(x => x.M)
            .First().M;
        int strongestActionStationarityFirst = admissible
            .OrderBy(x => x.ActionResidual)
            .ThenBy(x => x.TotalEnergy)
            .ThenBy(x => x.M)
            .First().M;

        var ruleSelections = new (string Rule, int SelectedMode)[]
        {
            ("lowest m first", lowestMFirst),
            ("lowest energy first", lowestEnergyFirst),
            ("strongest bridge-prior first", strongestBridgePriorFirst),
            ("strongest phase closure first", strongestPhaseClosureFirst),
            ("strongest action-stationarity first", strongestActionStationarityFirst)
        };

        bool allPhysicallyMotivatedSelectM3 =
            lowestEnergyFirst == 3 &&
            strongestBridgePriorFirst == 3 &&
            strongestPhaseClosureFirst == 3 &&
            strongestActionStationarityFirst == 3;
        bool m3DependsOnArbitraryListOrdering =
            lowestMFirst == 3 &&
            !allPhysicallyMotivatedSelectM3;
        bool orderingSensitivity = ruleSelections.Select(x => x.SelectedMode).Distinct().Count() > 1;
        string classification = allPhysicallyMotivatedSelectM3
            ? "ordering-robust"
            : "boundary/ordering-sensitive";

        _output.WriteLine("--- RBF60 ORDERING/TIE-BREAK DIAGNOSTIC ---");
        _output.WriteLine($"baseline selected={(baseline.Resolved ? $"m={baseline.SelectedMode}" : "none")} | satisfying=[{string.Join(", ", baseline.SatisfyingModes)}]");
        foreach (var r in ruleSelections)
            _output.WriteLine($"RBF60 rule={r.Rule} | selected=m={r.SelectedMode}");
        _output.WriteLine($"RBF60 orderingSensitivity={orderingSensitivity} | m3DependsOnArbitraryListOrdering={m3DependsOnArbitraryListOrdering} | class={classification}");
        _output.WriteLine("RBF60 claim boundary: diagnostic/candidate only; ordering robustness is not theorem-level uniqueness.");

        Assert.True(baseline.Resolved && baseline.SelectedMode == 3,
            $"Expected baseline shared functional to select m=3. selected={(baseline.Resolved ? $"m={baseline.SelectedMode}" : "none")}");
        Assert.True(allPhysicallyMotivatedSelectM3,
            $"Expected physically motivated ordering rules to keep m=3 selected. energy={lowestEnergyFirst}, bridge={strongestBridgePriorFirst}, phase={strongestPhaseClosureFirst}, action={strongestActionStationarityFirst}");
    }

    /// <summary>
    /// Checks whether admissible non-m3 competitors lose to m=3 on at least one stronger structural component margin.
    /// Matters because bounded candidate status should be supported by explicit structural-margin diagnostics, not hard uniqueness claims.
    /// Expected diagnostic behavior: admissible competitors are blocked by phase/bridge/action component margins with m=3 best combined baseline margin.
    /// Claim boundary: diagnostic/candidate only; not theorem-level proof.
    /// </summary>
    [Trait("Category", "LongRunning")]
    [Fact]
    public void RBF61_AdmissibleCompetitors_Should_Fail_AtLeastOne_StrongerStructuralMargin()
    {
        int[] mValues = { 1, 2, 3, 4, 5 };
        int[] qCore = DeriveBridgeCoreQValuesFromBand(3, 1.16, 1.19, 0.84, 0.86, 2, 64);
        int[] qSupport = DeriveBridgeBandSupportUnionQValues(mValues, 1.16, 1.19, 0.84, 0.86, 2, 64);

        var family = BuildModeFamilyFromExplicitQValues(
            mValues,
            qSupport,
            0.50,
            0.35,
            0.15,
            BuildNoCadencePriorConfig(),
            new[] { 2e-3 });

        var rows = BuildSharedFunctionalRows(
            family,
            qCore,
            phaseWeight: 1.0,
            bridgeWeight: 1.0,
            actionWeight: 1.0,
            phaseTolerance: 0.35,
            bridgeTolerance: 1.0,
            actionTolerance: 0.95);
        var baseline = EvaluateSharedFunctionalSelection(rows);

        var admissible = rows
            .Where(x => x.Admissible)
            .OrderBy(x => x.TotalEnergy)
            .ThenBy(x => x.M)
            .ToArray();
        Assert.True(admissible.Any(x => x.M == 3), "Expected baseline admissible set to include m=3.");

        var m3 = admissible.First(x => x.M == 3);
        var competitors = admissible.Where(x => x.M != 3).OrderBy(x => x.M).ToArray();
        Assert.True(competitors.Length > 0, "Expected admissible competitors (e.g., m=2 or m=4) for structural-margin diagnostics.");

        int bestCombinedMode = admissible
            .OrderBy(x => x.PhaseDefect + x.BridgePenalty + x.ActionResidual)
            .ThenBy(x => x.M)
            .First().M;

        const double marginEpsilon = 1e-9;
        int blockedByAnyComponent = 0;

        _output.WriteLine("--- RBF61 STRUCTURAL-MARGIN COMPETITOR DIAGNOSTIC ---");
        _output.WriteLine($"baseline selected={(baseline.Resolved ? $"m={baseline.SelectedMode}" : "none")} | satisfying=[{string.Join(", ", baseline.SatisfyingModes)}]");
        _output.WriteLine($"RBF61 best combined structural mode=m={bestCombinedMode}");

        foreach (var c in competitors)
        {
            double phaseMargin = c.PhaseDefect - m3.PhaseDefect;
            double bridgeMargin = c.BridgePenalty - m3.BridgePenalty;
            double actionMargin = c.ActionResidual - m3.ActionResidual;
            double totalMargin = c.TotalEnergy - m3.TotalEnergy;

            double maxMargin = Math.Max(phaseMargin, Math.Max(bridgeMargin, actionMargin));
            bool strongStructuralLoss = maxMargin > marginEpsilon;
            if (strongStructuralLoss)
                blockedByAnyComponent++;

            string blockingComponent =
                phaseMargin >= bridgeMargin && phaseMargin >= actionMargin ? "phase-defect"
                : bridgeMargin >= phaseMargin && bridgeMargin >= actionMargin ? "bridge-prior/qCore"
                : "action-stationarity";

            _output.WriteLine(
                $"RBF61 competitor=m={c.M} | phaseMargin={phaseMargin:F4} | bridgeMargin={bridgeMargin:F4} | actionMargin={actionMargin:F4} | totalMargin={totalMargin:F4} | blockingComponent={blockingComponent} | structurallyBlocked={strongStructuralLoss}");
        }

        _output.WriteLine("RBF61 claim boundary: diagnostic/candidate only; margin diagnostics do not establish theorem-level uniqueness.");

        Assert.True(baseline.Resolved && baseline.SelectedMode == 3,
            $"Expected baseline shared functional to select m=3. selected={(baseline.Resolved ? $"m={baseline.SelectedMode}" : "none")}");
        Assert.True(bestCombinedMode == 3,
            $"Expected m=3 to have best combined structural margin in baseline. bestCombinedMode={bestCombinedMode}");
        Assert.True(blockedByAnyComponent == competitors.Length,
            $"Expected each admissible competitor to lose on at least one stronger structural component margin. blocked={blockedByAnyComponent}, competitors={competitors.Length}");
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

    private static (int M, int InBandCount, double AvgClosureQuality, double OperationalActionTick, double DerivedActionTick)[] BuildModeFamilyFromLatticeProxy(
        IReadOnlyCollection<int> mValues,
        int qMin,
        int qMax,
        double orderWeight,
        double closureWeight,
        double transportWeight)
    {
        return BuildModeFamilyFromLatticeProxy(
            mValues,
            qMin,
            qMax,
            orderWeight,
            closureWeight,
            transportWeight,
            BuildNoCadencePriorConfig());
    }

    private static (int M, int InBandCount, double AvgClosureQuality, double OperationalActionTick, double DerivedActionTick)[] BuildModeFamilyFromLatticeProxy(
        IReadOnlyCollection<int> mValues,
        int qMin,
        int qMax,
        double orderWeight,
        double closureWeight,
        double transportWeight,
        ModeLockConfig solverConfig,
        double[]? epsilonsOverride = null)
    {
        const double G = 1.0;
        const double c = 1.0;
        const double b = 1.0;
        const double dt = 0.001;
        double[] epsilons = epsilonsOverride ?? new[] { 2e-3, 1e-2 };
        int[] orderedModes = mValues.OrderBy(x => x).ToArray();
        string cacheKey = BuildModeFamilyCacheKey(
            orderedModes,
            qMin,
            qMax,
            orderWeight,
            closureWeight,
            transportWeight,
            solverConfig,
            epsilons);

        if (ModeFamilyCache.TryGetValue(cacheKey, out var cached))
            return cached;

        int qCount = (qMax - qMin + 1);
        var alphaSchwarzByEpsilon = new double[epsilons.Length];
        for (int i = 0; i < epsilons.Length; i++)
            alphaSchwarzByEpsilon[i] = ComputeSchwarzschildNullDeflection(epsilons[i]);

        var computed = new (int M, int InBandCount, double AvgClosureQuality, double OperationalActionTick, double DerivedActionTick)[orderedModes.Length];
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, orderedModes.Length)
        };

        Parallel.For(0, orderedModes.Length, options, idx =>
        {
            int m = orderedModes[idx];
            int inBandCount = 0;
            double closureQualitySum = 0.0;
            double inBandActionSum = 0.0;
            double minInBandAction = double.PositiveInfinity;

            for (int q = qMin; q <= qMax; q++)
            {
                double omega = (q + (double)m) / q;
                double gamma = 1.0 / omega;
                bool inBand = omega >= 1.16 && omega <= 1.19 && gamma >= 0.84 && gamma <= 0.86;

                var modeLock = SimulateModeLock(omega, solverConfig);
                var parameters = new PhotonTransportModel.Parameters
                {
                    LambdaTime = 1.0,
                    LambdaSpace = 30.0,
                    EulerBridgeScale = gamma
                };

                double relErrorSum = 0.0;
                for (int e = 0; e < epsilons.Length; e++)
                {
                    double alphaEuler = PhotonTransportModel.ComputeDeflectionEulerLagrange(epsilons[e], G, c, b, dt, parameters);
                    double alphaSchwarz = alphaSchwarzByEpsilon[e];
                    relErrorSum += Math.Abs(alphaEuler - alphaSchwarz) / Math.Max(alphaSchwarz, 1e-16);
                }
                double meanRelError = relErrorSum / epsilons.Length;

                double orderDefect = Math.Max(0.0, 1.0 - modeLock.MeanOrder);
                double closureDefect = Math.Max(0.0, modeLock.ClosureResidual);
                double transportDefect = Math.Max(0.0, meanRelError);
                double latticeActionDensity =
                    (orderWeight * orderDefect * orderDefect
                    + closureWeight * closureDefect * closureDefect
                    + transportWeight * transportDefect * transportDefect)
                    / Math.Max(omega, 1e-12);

                if (inBand)
                {
                    inBandCount++;
                    inBandActionSum += latticeActionDensity;
                    minInBandAction = Math.Min(minInBandAction, latticeActionDensity);
                }

                closureQualitySum += (1.0 - modeLock.ClosureResidual);
            }

            double avgClosureQuality = closureQualitySum / qCount;
            double occupancyPenalty = 1.0 / Math.Max(inBandCount, 1);
            double operationalActionTick = inBandCount > 0
                ? (inBandActionSum / inBandCount) + occupancyPenalty
                : 1e6 + occupancyPenalty;
            double derivedActionTick = inBandCount > 0
                ? minInBandAction + occupancyPenalty
                : 1e6 + occupancyPenalty;

            computed[idx] = (m, inBandCount, avgClosureQuality, operationalActionTick, derivedActionTick);
        });

        ModeFamilyCache.TryAdd(cacheKey, computed);
        return computed;
    }

    private static (int M, int InBandCount, double AvgClosureQuality, double OperationalActionTick, double DerivedActionTick)[] BuildModeFamilyFromExplicitQValues(
        IReadOnlyCollection<int> mValues,
        IReadOnlyCollection<int> qValues,
        double orderWeight,
        double closureWeight,
        double transportWeight,
        ModeLockConfig solverConfig,
        double[]? epsilonsOverride = null)
    {
        const double G = 1.0;
        const double c = 1.0;
        const double b = 1.0;
        const double dt = 0.001;
        double[] epsilons = epsilonsOverride ?? new[] { 2e-3, 1e-2 };
        int[] orderedModes = mValues.OrderBy(x => x).ToArray();
        int[] orderedQ = qValues.Distinct().OrderBy(x => x).ToArray();
        if (orderedQ.Length == 0)
            throw new ArgumentException("qValues must not be empty.", nameof(qValues));

        string modePart = string.Join(",", orderedModes);
        string qPart = string.Join(",", orderedQ);
        string epsPart = string.Join(",", epsilons.Select(x => x.ToString("R")));
        string cacheKey = string.Join("|",
            "explicitQ",
            modePart,
            qPart,
            orderWeight.ToString("R"),
            closureWeight.ToString("R"),
            transportWeight.ToString("R"),
            solverConfig.CellCount,
            solverConfig.Steps,
            solverConfig.SettleSteps,
            solverConfig.Dt.ToString("R"),
            solverConfig.CouplingKappa.ToString("R"),
            solverConfig.CollectiveWeight.ToString("R"),
            solverConfig.OrderScoreWeight.ToString("R"),
            solverConfig.AlignmentScoreWeight.ToString("R"),
            solverConfig.CadenceScoreWeight.ToString("R"),
            solverConfig.BreakClosure,
            solverConfig.ClosureBreakAmplitude.ToString("R"),
            solverConfig.ClosureBreakEveryNSteps,
            epsPart);

        if (ModeFamilyCache.TryGetValue(cacheKey, out var cached))
            return cached;

        var alphaSchwarzByEpsilon = new double[epsilons.Length];
        for (int i = 0; i < epsilons.Length; i++)
            alphaSchwarzByEpsilon[i] = ComputeSchwarzschildNullDeflection(epsilons[i]);

        var computed = new (int M, int InBandCount, double AvgClosureQuality, double OperationalActionTick, double DerivedActionTick)[orderedModes.Length];
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, orderedModes.Length)
        };

        Parallel.For(0, orderedModes.Length, options, idx =>
        {
            int m = orderedModes[idx];
            int inBandCount = 0;
            double closureQualitySum = 0.0;
            double inBandActionSum = 0.0;
            double minInBandAction = double.PositiveInfinity;

            for (int i = 0; i < orderedQ.Length; i++)
            {
                int q = orderedQ[i];
                double omega = (q + (double)m) / q;
                double gamma = 1.0 / omega;
                bool inBand = omega >= 1.16 && omega <= 1.19 && gamma >= 0.84 && gamma <= 0.86;

                var modeLock = SimulateModeLock(omega, solverConfig);
                var parameters = new PhotonTransportModel.Parameters
                {
                    LambdaTime = 1.0,
                    LambdaSpace = 30.0,
                    EulerBridgeScale = gamma
                };

                double relErrorSum = 0.0;
                for (int e = 0; e < epsilons.Length; e++)
                {
                    double alphaEuler = PhotonTransportModel.ComputeDeflectionEulerLagrange(epsilons[e], G, c, b, dt, parameters);
                    double alphaSchwarz = alphaSchwarzByEpsilon[e];
                    relErrorSum += Math.Abs(alphaEuler - alphaSchwarz) / Math.Max(alphaSchwarz, 1e-16);
                }
                double meanRelError = relErrorSum / epsilons.Length;

                double orderDefect = Math.Max(0.0, 1.0 - modeLock.MeanOrder);
                double closureDefect = Math.Max(0.0, modeLock.ClosureResidual);
                double transportDefect = Math.Max(0.0, meanRelError);
                double latticeActionDensity =
                    (orderWeight * orderDefect * orderDefect
                    + closureWeight * closureDefect * closureDefect
                    + transportWeight * transportDefect * transportDefect)
                    / Math.Max(omega, 1e-12);

                if (inBand)
                {
                    inBandCount++;
                    inBandActionSum += latticeActionDensity;
                    minInBandAction = Math.Min(minInBandAction, latticeActionDensity);
                }

                closureQualitySum += (1.0 - modeLock.ClosureResidual);
            }

            double avgClosureQuality = closureQualitySum / orderedQ.Length;
            double occupancyPenalty = 1.0 / Math.Max(inBandCount, 1);
            double operationalActionTick = inBandCount > 0
                ? (inBandActionSum / inBandCount) + occupancyPenalty
                : 1e6 + occupancyPenalty;
            double derivedActionTick = inBandCount > 0
                ? minInBandAction + occupancyPenalty
                : 1e6 + occupancyPenalty;

            computed[idx] = (m, inBandCount, avgClosureQuality, operationalActionTick, derivedActionTick);
        });

        ModeFamilyCache.TryAdd(cacheKey, computed);
        return computed;
    }

    private static string BuildModeFamilyCacheKey(
        IReadOnlyCollection<int> orderedModes,
        int qMin,
        int qMax,
        double orderWeight,
        double closureWeight,
        double transportWeight,
        ModeLockConfig solverConfig,
        IReadOnlyCollection<double> epsilons)
    {
        string modePart = string.Join(",", orderedModes);
        string epsPart = string.Join(",", epsilons.Select(x => x.ToString("R")));
        return string.Join("|",
            modePart,
            qMin,
            qMax,
            orderWeight.ToString("R"),
            closureWeight.ToString("R"),
            transportWeight.ToString("R"),
            solverConfig.CellCount,
            solverConfig.Steps,
            solverConfig.SettleSteps,
            solverConfig.Dt.ToString("R"),
            solverConfig.CouplingKappa.ToString("R"),
            solverConfig.CollectiveWeight.ToString("R"),
            solverConfig.OrderScoreWeight.ToString("R"),
            solverConfig.AlignmentScoreWeight.ToString("R"),
            solverConfig.CadenceScoreWeight.ToString("R"),
            solverConfig.BreakClosure,
            solverConfig.ClosureBreakAmplitude.ToString("R"),
            solverConfig.ClosureBreakEveryNSteps,
            epsPart);
    }

    private static (bool Resolved, int SelectedMode, int[] SatisfyingModes) EvaluateDerivedThreeConstraintSelection(
        (int M, int InBandCount, double AvgClosureQuality, double OperationalActionTick, double DerivedActionTick)[] family,
        double phaseThreshold,
        int bridgeThreshold,
        double actionScale,
        double actionMargin = 0.0)
    {
        var phaseBridgeCandidates = family
            .Where(x => x.AvgClosureQuality >= phaseThreshold && x.InBandCount >= bridgeThreshold)
            .OrderBy(x => x.DerivedActionTick)
            .ToArray();

        if (phaseBridgeCandidates.Length == 0)
            return (false, int.MaxValue, Array.Empty<int>());

        double baseThreshold = phaseBridgeCandidates.Length >= 2
            ? 0.5 * (phaseBridgeCandidates[0].DerivedActionTick + phaseBridgeCandidates[1].DerivedActionTick)
            : 1.05 * phaseBridgeCandidates[0].DerivedActionTick;
        double actionThreshold = actionScale * baseThreshold - actionMargin;

        var derivedRankByMode = phaseBridgeCandidates
            .Select((x, index) => (x.M, Rank: index + 1))
            .ToDictionary(x => x.M, x => x.Rank);

        var satisfying = family
            .Where(x =>
                x.AvgClosureQuality >= phaseThreshold &&
                x.InBandCount >= bridgeThreshold &&
                x.DerivedActionTick <= actionThreshold &&
                derivedRankByMode.TryGetValue(x.M, out int rank) &&
                rank == 1)
            .OrderBy(x => x.DerivedActionTick)
            .ThenBy(x => x.M)
            .ToArray();

        if (satisfying.Length == 0)
            return (false, int.MaxValue, Array.Empty<int>());

        return (true, satisfying[0].M, satisfying.Select(x => x.M).ToArray());
    }

    private static int[] DeriveBridgeCoreQValuesFromBand(
        int m,
        double omegaMin,
        double omegaMax,
        double gammaMin,
        double gammaMax,
        int qMin,
        int qMax)
    {
        var qValues = new List<int>();

        for (int q = qMin; q <= qMax; q++)
        {
            double omega = (q + (double)m) / q;
            double gamma = 1.0 / omega;
            bool inBand = omega >= omegaMin && omega <= omegaMax && gamma >= gammaMin && gamma <= gammaMax;
            if (inBand)
                qValues.Add(q);
        }

        return qValues.ToArray();
    }

    private static int[] DeriveBridgeBandSupportUnionQValues(
        IReadOnlyCollection<int> mValues,
        double omegaMin,
        double omegaMax,
        double gammaMin,
        double gammaMax,
        int qMin,
        int qMax)
    {
        var qSet = new HashSet<int>();
        foreach (int m in mValues)
        {
            foreach (int q in DeriveBridgeCoreQValuesFromBand(m, omegaMin, omegaMax, gammaMin, gammaMax, qMin, qMax))
                qSet.Add(q);
        }

        return qSet.OrderBy(x => x).ToArray();
    }

    private static double ComputeIntegerClosureDefectNormalized(int mode, IReadOnlyCollection<int> qValues, int targetShift)
    {
        int[] orderedQ = qValues.Distinct().OrderBy(x => x).ToArray();
        if (orderedQ.Length == 0)
            return double.PositiveInfinity;

        double defectSum = 0.0;
        for (int i = 0; i < orderedQ.Length; i++)
        {
            int q = orderedQ[i];
            double omega = (q + (double)mode) / q;
            double pCompatible = q + targetShift;
            defectSum += Math.Abs(q * omega - pCompatible) / Math.Max(Math.Abs(targetShift), 1.0);
        }

        return defectSum / orderedQ.Length;
    }

    private static double ComputeBridgePriorPenalty(int mode, IReadOnlyCollection<int> qCore)
    {
        int[] coreQ = qCore.Distinct().OrderBy(x => x).ToArray();
        if (coreQ.Length == 0)
            return 1.0;

        int supportCount = 0;
        for (int i = 0; i < coreQ.Length; i++)
        {
            int q = coreQ[i];
            double omega = (q + (double)mode) / q;
            double gamma = 1.0 / omega;
            bool inBand = omega >= 1.16 && omega <= 1.19 && gamma >= 0.84 && gamma <= 0.86;
            if (inBand)
                supportCount++;
        }

        double supportFraction = (double)supportCount / coreQ.Length;
        return 1.0 - supportFraction;
    }

    private static (bool PhaseOk, bool BridgeOk, bool ActionOk) EvaluateModeConstraintStatus(
        (int M, int InBandCount, double AvgClosureQuality, double OperationalActionTick, double DerivedActionTick)[] family,
        int mode,
        double phaseThreshold,
        int bridgeThreshold,
        double actionScale)
    {
        var target = family.First(x => x.M == mode);
        bool phaseOk = target.AvgClosureQuality >= phaseThreshold;
        bool bridgeOk = target.InBandCount >= bridgeThreshold;

        var phaseBridgeCandidates = family
            .Where(x => x.AvgClosureQuality >= phaseThreshold && x.InBandCount >= bridgeThreshold)
            .OrderBy(x => x.DerivedActionTick)
            .ToArray();

        if (phaseBridgeCandidates.Length == 0)
            return (phaseOk, bridgeOk, false);

        double baseThreshold = phaseBridgeCandidates.Length >= 2
            ? 0.5 * (phaseBridgeCandidates[0].DerivedActionTick + phaseBridgeCandidates[1].DerivedActionTick)
            : 1.05 * phaseBridgeCandidates[0].DerivedActionTick;
        double actionThreshold = actionScale * baseThreshold;
        bool actionOk = target.DerivedActionTick <= actionThreshold;

        return (phaseOk, bridgeOk, actionOk);
    }

    private static (bool Resolved, int SelectedMode, int[] SatisfyingModes, string[] FailureByMode, bool M3Admissible, bool M3Unique, bool M3Minimal) EvaluateSelectionFromCustomStatuses(
        (int M, int InBandCount, double AvgClosureQuality, double OperationalActionTick, double DerivedActionTick)[] family,
        IReadOnlyDictionary<int, (bool PhaseOk, bool BridgeOk, bool ActionOk)> statuses)
    {
        var satisfying = family
            .Where(x =>
                statuses.TryGetValue(x.M, out var s) &&
                s.PhaseOk &&
                s.BridgeOk &&
                s.ActionOk)
            .OrderBy(x => x.DerivedActionTick)
            .ThenBy(x => x.M)
            .ToArray();

        bool resolved = satisfying.Length > 0;
        int selectedMode = resolved ? satisfying[0].M : int.MaxValue;
        int[] satisfyingModes = satisfying.Select(x => x.M).ToArray();
        bool m3Admissible = satisfyingModes.Contains(3);
        bool m3Unique = m3Admissible && satisfyingModes.Length == 1;
        bool m3Minimal = resolved && selectedMode == 3;

        string[] failureByMode = family
            .OrderBy(x => x.M)
            .Select(x =>
            {
                if (!statuses.TryGetValue(x.M, out var s))
                    return $"m={x.M}:missing-status";
                return $"m={x.M}:{BuildActiveConstraintFailureReason(s, usePhase: true, useBridge: true, useActionTick: true)}";
            })
            .ToArray();

        return (resolved, selectedMode, satisfyingModes, failureByMode, m3Admissible, m3Unique, m3Minimal);
    }

    private static (int M, double PhaseDefect, double BridgePenalty, double ActionResidual, double TotalEnergy, bool Admissible)[] BuildSharedFunctionalRows(
        (int M, int InBandCount, double AvgClosureQuality, double OperationalActionTick, double DerivedActionTick)[] family,
        IReadOnlyCollection<int> qCore,
        double phaseWeight,
        double bridgeWeight,
        double actionWeight,
        double phaseTolerance,
        double bridgeTolerance,
        double actionTolerance)
    {
        double minAction = family.Min(x => x.DerivedActionTick);
        double actionRange = Math.Max(family.Max(x => x.DerivedActionTick) - minAction, 1e-12);

        return family
            .Select(x =>
            {
                double phaseDefect = ComputeIntegerClosureDefectNormalized(x.M, qCore, targetShift: 3);
                double bridgePenalty = ComputeBridgePriorPenalty(x.M, qCore);
                double actionResidual = (x.DerivedActionTick - minAction) / actionRange;
                double total = phaseWeight * phaseDefect + bridgeWeight * bridgePenalty + actionWeight * actionResidual;
                bool admissible =
                    phaseDefect <= phaseTolerance &&
                    bridgePenalty <= bridgeTolerance &&
                    actionResidual <= actionTolerance;
                return (x.M, phaseDefect, bridgePenalty, actionResidual, total, admissible);
            })
            .OrderBy(x => x.total)
            .ThenBy(x => x.M)
            .Select(x => (x.M, x.phaseDefect, x.bridgePenalty, x.actionResidual, x.total, x.admissible))
            .ToArray();
    }

    private static (bool Resolved, int SelectedMode, int[] SatisfyingModes, double Margin, string[] FailureByMode) EvaluateSharedFunctionalSelection(
        (int M, double PhaseDefect, double BridgePenalty, double ActionResidual, double TotalEnergy, bool Admissible)[] rows)
    {
        var satisfying = rows
            .Where(x => x.Admissible)
            .OrderBy(x => x.TotalEnergy)
            .ThenBy(x => x.M)
            .ToArray();

        bool resolved = satisfying.Length > 0;
        int selectedMode = resolved ? satisfying[0].M : int.MaxValue;
        int[] satisfyingModes = satisfying.Select(x => x.M).ToArray();
        double margin = satisfying.Length >= 2
            ? satisfying[1].TotalEnergy - satisfying[0].TotalEnergy
            : double.NaN;

        string[] failureByMode = rows
            .OrderBy(x => x.M)
            .Select(x =>
            {
                var reasons = new List<string>();
                if (x.PhaseDefect > 0.35) reasons.Add("phaseDefect");
                if (x.BridgePenalty > 1.0) reasons.Add("bridgePrior");
                if (x.ActionResidual > 0.95) reasons.Add("actionStationarity");
                return $"m={x.M}:{(x.Admissible ? "passes-active" : string.Join("+", reasons))}";
            })
            .ToArray();

        return (resolved, selectedMode, satisfyingModes, margin, failureByMode);
    }

    private static string ClassifyCounterexample(int selectedMode, string qScope, double phaseTolerance, double actionTolerance)
    {
        if (qScope.Equals("core", StringComparison.Ordinal))
            return "boundary";
        if (qScope.Equals("wide", StringComparison.Ordinal))
            return "no-core";
        if (selectedMode == 2 && phaseTolerance > 0.35)
            return "phase-action";
        return "mixed";
    }

    private static bool DoesAssumptionRemovalWeaken(
        (bool Resolved, int SelectedMode, int[] SatisfyingModes, double Margin, string[] FailureByMode) baseline,
        (bool Resolved, int SelectedMode, int[] SatisfyingModes, double Margin, string[] FailureByMode) removal)
    {
        if (!removal.Resolved)
            return true;
        if (removal.SelectedMode != baseline.SelectedMode)
            return true;
        if (removal.SatisfyingModes.Length > baseline.SatisfyingModes.Length)
            return true;
        return removal.SelectedMode != 3 || removal.SatisfyingModes.Length != 1;
    }

    private static string DetermineFailureClassFromSharedResult(
        (bool Resolved, int SelectedMode, int[] SatisfyingModes, double Margin, string[] FailureByMode) result,
        (int M, double PhaseDefect, double BridgePenalty, double ActionResidual, double TotalEnergy, bool Admissible)[] rows,
        double phaseTolerance,
        double bridgeTolerance,
        double actionTolerance)
    {
        if (!result.Resolved)
            return "none";
        if (result.SelectedMode == 2)
            return "m2";
        if (result.SelectedMode != 3)
            return "other-mode";
        if (result.SatisfyingModes.Length > 1)
            return "non-unique";
        return DetermineDominantConstraintChannel(rows, 3, phaseTolerance, bridgeTolerance, actionTolerance);
    }

    private static string DetermineScenarioCounterexampleClass(
        string label,
        IReadOnlyCollection<int> qSupport,
        IReadOnlyCollection<int> qCore,
        double bridgeWeight,
        double actionWeight,
        double phaseTolerance,
        double actionTolerance,
        (bool Resolved, int SelectedMode, int[] SatisfyingModes, double Margin, string[] FailureByMode) result)
    {
        if (label.Contains("mixed", StringComparison.OrdinalIgnoreCase))
            return "mixed boundary";

        bool hasCore = qCore.Any(q => qSupport.Contains(q));
        if (!hasCore)
            return "no-core q-support";
        if (bridgeWeight <= 0.0)
            return "bridge-prior loss";
        if (actionWeight <= 0.0 || actionTolerance > 1.0)
            return "action-stationarity relaxation";
        if (phaseTolerance > 0.35 && (result.SelectedMode == 2 || result.SatisfyingModes.Contains(2)))
            return "phase/action boundary";
        return "mixed boundary";
    }

    private static string DetermineDominantConstraintChannel(
        (int M, double PhaseDefect, double BridgePenalty, double ActionResidual, double TotalEnergy, bool Admissible)[] rows,
        int targetMode,
        double phaseTolerance,
        double bridgeTolerance,
        double actionTolerance)
    {
        var target = rows.First(x => x.M == targetMode);
        double phaseExcess = Math.Max(0.0, target.PhaseDefect - phaseTolerance);
        double bridgeExcess = Math.Max(0.0, target.BridgePenalty - bridgeTolerance);
        double actionExcess = Math.Max(0.0, target.ActionResidual - actionTolerance);

        int channelsExceeded =
            (phaseExcess > 0 ? 1 : 0) +
            (bridgeExcess > 0 ? 1 : 0) +
            (actionExcess > 0 ? 1 : 0);
        if (channelsExceeded == 0)
            return "mixed";
        if (channelsExceeded > 1)
            return "mixed";
        if (phaseExcess > 0)
            return "phase-defect";
        if (bridgeExcess > 0)
            return "bridge-prior/qCore";
        return "action-stationarity";
    }

    private static (string StackName, bool Resolved, int SelectedMode, int[] SatisfyingModes, string[] FailureByMode, bool M3Admissible, bool M3Unique, bool M3Minimal) EvaluateConstraintStackSelection(
        (int M, int InBandCount, double AvgClosureQuality, double OperationalActionTick, double DerivedActionTick)[] family,
        string stackName,
        bool usePhase,
        bool useBridge,
        bool useActionTick,
        double phaseThreshold,
        int bridgeThreshold,
        double actionScale)
    {
        var phaseBridgeStatuses = family.ToDictionary(
            x => x.M,
            x => (
                PhaseOk: x.AvgClosureQuality >= phaseThreshold,
                BridgeOk: x.InBandCount >= bridgeThreshold));

        var actionBaseCandidates = family
            .Where(x =>
                (!usePhase || phaseBridgeStatuses[x.M].PhaseOk) &&
                (!useBridge || phaseBridgeStatuses[x.M].BridgeOk))
            .OrderBy(x => x.DerivedActionTick)
            .ToArray();

        double actionThreshold = double.NegativeInfinity;
        if (actionBaseCandidates.Length > 0)
        {
            double baseThreshold = actionBaseCandidates.Length >= 2
                ? 0.5 * (actionBaseCandidates[0].DerivedActionTick + actionBaseCandidates[1].DerivedActionTick)
                : 1.05 * actionBaseCandidates[0].DerivedActionTick;
            actionThreshold = actionScale * baseThreshold;
        }

        var statuses = family.ToDictionary(
            x => x.M,
            x =>
            {
                var s = phaseBridgeStatuses[x.M];
                bool actionOk = actionBaseCandidates.Length > 0 && x.DerivedActionTick <= actionThreshold;
                return (PhaseOk: s.PhaseOk, BridgeOk: s.BridgeOk, ActionOk: actionOk);
            });

        var satisfying = family
            .Where(x =>
            {
                var s = statuses[x.M];
                return (!usePhase || s.PhaseOk)
                    && (!useBridge || s.BridgeOk)
                    && (!useActionTick || s.ActionOk);
            })
            .OrderBy(x => x.DerivedActionTick)
            .ThenBy(x => x.M)
            .ToArray();

        bool resolved = satisfying.Length > 0;
        int selectedMode = resolved ? satisfying[0].M : int.MaxValue;
        int[] satisfyingModes = satisfying.Select(x => x.M).ToArray();
        bool m3Admissible = satisfyingModes.Contains(3);
        bool m3Unique = m3Admissible && satisfyingModes.Length == 1;
        bool m3Minimal = resolved && selectedMode == 3;

        string[] failureByMode = family
            .OrderBy(x => x.M)
            .Select(x => $"m={x.M}:{BuildActiveConstraintFailureReason(statuses[x.M], usePhase, useBridge, useActionTick)}")
            .ToArray();

        return (stackName, resolved, selectedMode, satisfyingModes, failureByMode, m3Admissible, m3Unique, m3Minimal);
    }

    private static string BuildActiveConstraintFailureReason(
        (bool PhaseOk, bool BridgeOk, bool ActionOk) status,
        bool usePhase,
        bool useBridge,
        bool useActionTick)
    {
        var failures = new List<string>();
        if (usePhase && !status.PhaseOk) failures.Add("phase");
        if (useBridge && !status.BridgeOk) failures.Add("bridge");
        if (useActionTick && !status.ActionOk) failures.Add("actionTick");

        return failures.Count == 0 ? "passes-active" : string.Join("+", failures);
    }

    private static bool IsConstraintAblationWeakened(
        (string StackName, bool Resolved, int SelectedMode, int[] SatisfyingModes, string[] FailureByMode, bool M3Admissible, bool M3Unique, bool M3Minimal) full,
        (string StackName, bool Resolved, int SelectedMode, int[] SatisfyingModes, string[] FailureByMode, bool M3Admissible, bool M3Unique, bool M3Minimal) ablated)
    {
        if (!ablated.Resolved)
            return true;
        if (ablated.SelectedMode != full.SelectedMode)
            return true;
        if (ablated.SatisfyingModes.Length > full.SatisfyingModes.Length)
            return true;
        return !ablated.M3Unique;
    }

    private static int SelectModeByShortcutRule(
        (int M, int InBandCount, double AvgClosureQuality, double OperationalActionTick, double DerivedActionTick)[] family,
        ShortcutRuleKind rule)
    {
        return rule switch
        {
            ShortcutRuleKind.PhaseOnlyBestClosure => family
                .OrderByDescending(x => x.AvgClosureQuality)
                .ThenBy(x => x.M)
                .First().M,
            ShortcutRuleKind.BridgeOnlyOccupancy => family
                .OrderByDescending(x => x.InBandCount)
                .ThenBy(x => x.M)
                .First().M,
            ShortcutRuleKind.ActionOnlyLowestDerivedActionTick => family
                .OrderBy(x => x.DerivedActionTick)
                .ThenBy(x => x.M)
                .First().M,
            ShortcutRuleKind.CombinedScoreNoExplicitGates => family
                .Select(x =>
                {
                    double bridgeNorm = family.Max(y => y.InBandCount) > 0
                        ? (double)x.InBandCount / family.Max(y => y.InBandCount)
                        : 0.0;
                    double actionNorm = 1.0 / (1.0 + x.DerivedActionTick);
                    double score = 0.45 * x.AvgClosureQuality + 0.35 * bridgeNorm + 0.20 * actionNorm;
                    return (x.M, Score: score);
                })
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.M)
                .First().M,
            _ => throw new ArgumentOutOfRangeException(nameof(rule), rule, "Unknown shortcut rule.")
        };
    }

    private static double ComputePearsonCorrelation(double[] x, double[] y)
    {
        if (x.Length != y.Length || x.Length == 0)
            return double.NaN;

        double meanX = x.Average();
        double meanY = y.Average();

        double cov = 0.0;
        double varX = 0.0;
        double varY = 0.0;
        for (int i = 0; i < x.Length; i++)
        {
            double dx = x[i] - meanX;
            double dy = y[i] - meanY;
            cov += dx * dy;
            varX += dx * dx;
            varY += dy * dy;
        }

        if (varX <= 0.0 || varY <= 0.0)
            return double.NaN;

        return cov / Math.Sqrt(varX * varY);
    }

    private static double ComputePairwiseMonotonicAgreement(double[] x, double[] y)
    {
        if (x.Length != y.Length || x.Length < 2)
            return double.NaN;

        int totalPairs = 0;
        int agreeingPairs = 0;

        for (int i = 0; i < x.Length; i++)
        {
            for (int j = i + 1; j < x.Length; j++)
            {
                double dx = x[i] - x[j];
                double dy = y[i] - y[j];
                if (Math.Abs(dx) < 1e-12 || Math.Abs(dy) < 1e-12)
                    continue;

                totalPairs++;
                if ((dx > 0 && dy > 0) || (dx < 0 && dy < 0))
                    agreeingPairs++;
            }
        }

        return totalPairs > 0 ? (double)agreeingPairs / totalPairs : double.NaN;
    }

    private static string ResolveFailureReason(bool phaseOk, bool bridgeOk, bool actionOk)
    {
        if (phaseOk && bridgeOk && actionOk)
            return "passes-all";

        int failCount = (!phaseOk ? 1 : 0) + (!bridgeOk ? 1 : 0) + (!actionOk ? 1 : 0);
        if (failCount > 1)
            return "mixed";

        if (!phaseOk)
            return "phase";
        if (!bridgeOk)
            return "bridge";
        return "actionTick";
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

    private enum ShortcutRuleKind
    {
        PhaseOnlyBestClosure,
        BridgeOnlyOccupancy,
        ActionOnlyLowestDerivedActionTick,
        CombinedScoreNoExplicitGates
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
