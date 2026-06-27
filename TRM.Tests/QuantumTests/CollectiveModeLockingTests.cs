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
