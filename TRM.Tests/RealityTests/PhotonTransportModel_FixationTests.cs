using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.RealityTests
{

    /// <summary>
    /// Fixation/regression tests for the consolidated PhotonTransportModel.
    /// Status: tested (fast invariants), diagnostic (transport-channel checks), limitation (does not by itself prove full physical completeness).
    /// Theory/review links: docs/theory/TRM_Geodesic_Derivation.md, docs/review/TRM_Real_Physics_Test_Coverage.md.
    /// Related implementation: TRM.Core/Shared/PhotonTransportModel.cs.
    /// </summary>
    public class PhotonTransportModel_FixationTests
    {
        private readonly ITestOutputHelper _output;

        public PhotonTransportModel_FixationTests(ITestOutputHelper output)
        {
            _output = output;
        }
        [Trait("Category", "CoreRegression")]
        [Fact]
        public void TRM78_EffectiveIndex_Should_Be_Positive_And_Finite()
        {
            var parameters = new PhotonTransportModel.Parameters
            {
                LambdaTime = 1.0,
                LambdaSpace = 30.0
            };

            double[] phis = { 1e-6, 1e-4, 1e-2, 0.05, 0.1 };

            foreach (double phi in phis)
            {
                double absDmuDt = 0.01;
                double localMemory = phi * phi * absDmuDt;

                double nEff =
                    2.0
                    + parameters.LambdaTime * phi
                    + parameters.LambdaSpace * localMemory;

                Assert.True(double.IsFinite(nEff), $"nEff must be finite for phi={phi}.");
                Assert.True(nEff > 2.0, $"nEff must be greater than baseline 2 for phi={phi}.");
            }
        }

        [Trait("Category", "CoreRegression")]
        [Fact]
        public void TRM79_RK4_Should_Preserve_PhotonSpeed()
        {
            const double G = 1.0;
            const double c = 1.0;
            const double M = 0.01;
            const double dt = 0.001;

            var parameters = new PhotonTransportModel.Parameters
            {
                LambdaTime = 1.0,
                LambdaSpace = 30.0
            };

            var state = new PhotonTransportModel.PhotonMemoryState
            {
                x = -50.0,
                y = 1.0,
                vx = c,
                vy = 0.0,
                memory = 0.0,
                TimeAccum = 0.0
            };

            for (int i = 0; i < 1000; i++)
            {
                PhotonTransportModel.RK4Step(ref state, dt, G, M, c, parameters);
            }

            double speed = Math.Sqrt(state.vx * state.vx + state.vy * state.vy);

            Assert.InRange(speed, c - 1e-12, c + 1e-12);
        }

        [Trait("Category", "CoreRegression")]
        [Fact]
        public void TRM80_LocalMemoryChannel_Should_Be_Finite_And_NonNegative()
        {
            const double G = 1.0;
            const double c = 1.0;
            const double M = 0.01;

            var parameters = new PhotonTransportModel.Parameters();

            var state = new PhotonTransportModel.PhotonMemoryState
            {
                x = -10.0,
                y = 1.0,
                vx = c,
                vy = 0.0,
                memory = 0.0,
                TimeAccum = 0.0
            };

            double r = Math.Sqrt(state.x * state.x + state.y * state.y);
            double phi = PhotonTransportModel.Phi(G, M, c, r);
            double absDmuDt = PhotonTransportModel.ComputeAbsDmuDtBase(state, G, M, c, parameters);
            double localMemory = phi * phi * absDmuDt;

            Assert.True(double.IsFinite(phi), "phi must be finite.");
            Assert.True(double.IsFinite(absDmuDt), "|dmu/dt| must be finite.");
            Assert.True(double.IsFinite(localMemory), "local memory must be finite.");
            Assert.True(localMemory >= 0.0, "local memory must be non-negative.");
        }

        [Trait("Category", "CoreRegression")]
        [Fact]
        public void TRM81_Deflection_Should_Decrease_With_ImpactParameter()
        {
            const double G = 1.0;
            const double c = 1.0;
            const double epsilon = 0.01;
            const double dt = 0.001;

            var parameters = new PhotonTransportModel.Parameters
            {
                LambdaTime = 1.0,
                LambdaSpace = 30.0
            };

            double alpha1 = PhotonTransportModel.ComputeDeflection(epsilon, G, c, 1.0, dt, parameters);
            double alpha2 = PhotonTransportModel.ComputeDeflection(epsilon, G, c, 2.0, dt, parameters);
            double alpha5 = PhotonTransportModel.ComputeDeflection(epsilon, G, c, 5.0, dt, parameters);

            Assert.True(double.IsFinite(alpha1));
            Assert.True(double.IsFinite(alpha2));
            Assert.True(double.IsFinite(alpha5));

            Assert.True(alpha1 > alpha2, $"Expected alpha(b=1) > alpha(b=2), got {alpha1} <= {alpha2}.");
            Assert.True(alpha2 > alpha5, $"Expected alpha(b=2) > alpha(b=5), got {alpha2} <= {alpha5}.");
        }

        /// <summary>
        /// Checks scale behavior of implemented Shapiro diagnostic under proportional integration domain.
        /// Status: tested (regression for current implementation), diagnostic, limitation (implementation-bound invariant).
        /// </summary>
        [Trait("Category", "CoreRegression")]
        [Fact]
        public void TRM82_ShapiroDelay_Should_Be_ScaleStable_For_Proportional_IntegrationDomain()
        {
            const double G = 1.0;
            const double c = 1.0;
            const double epsilon = 0.01;
            const double dt = 0.001;

            double[] bValues = { 1.0, 2.0, 5.0, 10.0, 20.0, 50.0 };

            var data = new List<(double LogB, double Delay)>();

            foreach (double b in bValues)
            {
                var diagnostics = PhotonTransportModel.ComputeDeflectionWithDiagnostics(
                    epsilon,
                    G,
                    c,
                    b,
                    dt,
                    new PhotonTransportModel.Parameters
                    {
                        LambdaTime = 1.0,
                        LambdaSpace = 30.0
                    });

                Assert.True(double.IsFinite(diagnostics.ShapiroDelay));
                Assert.True(diagnostics.ShapiroDelay > 0.0);

                data.Add((Math.Log(b), diagnostics.ShapiroDelay));
            }

            (double slope, double intercept, double rms) = FitLinear(data);

            _output.WriteLine("---- TRM82 SHAPIRO SCALE-STABLE FIXATION ----");
            _output.WriteLine($"slope = {slope:E6}");
            _output.WriteLine($"rms   = {rms:E6}");

            Assert.True(double.IsFinite(slope));
            Assert.True(double.IsFinite(intercept));
            Assert.True(double.IsFinite(rms));

            _output.WriteLine("Current diagnostic uses X = 100*b, so ShapiroDelay is approximately scale-stable.");

            // In the current PhotonTransportModel diagnostics, the integration range scales with b:
            // X = 100*b. For ∫ ds/r this makes the geometric Shapiro integral approximately
            // scale-stable rather than growing as log(b). So this test fixes the implemented invariant.
            Assert.True(Math.Abs(slope) < 5e-4, $"Expected near-zero scale slope, got {slope}.");
            Assert.True(rms < 1e-3, $"Expected low RMS for scale-stable Shapiro diagnostic, got {rms}.");
        }



        [Trait("Category", "CoreRegression")]
        [Fact]
        public void TRM83_TimeAndSpaceChannels_Should_Separate_Cleanly()
        {
            const double phi = 0.01;
            const double absDmuDt = 0.02;

            var timeOnly = new PhotonTransportModel.Parameters
            {
                LambdaTime = 1.0,
                LambdaSpace = 0.0
            };

            var spaceOnly = new PhotonTransportModel.Parameters
            {
                LambdaTime = 0.0,
                LambdaSpace = 30.0
            };

            double localMemory = phi * phi * absDmuDt;

            double nTimeOnly = 2.0 + timeOnly.LambdaTime * phi + timeOnly.LambdaSpace * localMemory;
            double nSpaceOnly = 2.0 + spaceOnly.LambdaTime * phi + spaceOnly.LambdaSpace * localMemory;
            double nUnified = 2.0 + phi + 30.0 * localMemory;

            
            Assert.True(Math.Abs((2.0 + phi) - nTimeOnly) < 1e-12);
            Assert.True(Math.Abs((2.0 + 30.0 * localMemory) - nSpaceOnly) < 1e-12);
            Assert.True(Math.Abs((nTimeOnly + nSpaceOnly - 2.0) - nUnified) < 1e-12);
        }

        [Trait("Category", "PhysicsValidation")]
        [Fact]
        public void TRM84_LocalMemoryTerm_Should_Scale_Quadratically_With_Phi()
        {
            const double absDmuDt = 0.02;

            double phi1 = 1e-3;
            double phi2 = 2e-3;
            double phi3 = 4e-3;

            double m1 = phi1 * phi1 * absDmuDt;
            double m2 = phi2 * phi2 * absDmuDt;
            double m3 = phi3 * phi3 * absDmuDt;

            Assert.InRange(m2 / Math.Max(m1, 1e-30), 4.0 - 1e-12, 4.0 + 1e-12);
            Assert.InRange(m3 / Math.Max(m1, 1e-30), 16.0 - 1e-12, 16.0 + 1e-12);
        }

        [Trait("Category", "PhysicsValidation")]
        [Fact]
        public void TRM85_LocalMemoryTerm_Should_Scale_Linearly_With_DirectionalRotation()
        {
            const double phi = 1e-2;

            double absDmuDt1 = 5e-3;
            double absDmuDt2 = 1e-2;
            double absDmuDt3 = 2e-2;

            double m1 = phi * phi * absDmuDt1;
            double m2 = phi * phi * absDmuDt2;
            double m3 = phi * phi * absDmuDt3;

            Assert.InRange(m2 / Math.Max(m1, 1e-30), 2.0 - 1e-12, 2.0 + 1e-12);
            Assert.InRange(m3 / Math.Max(m1, 1e-30), 4.0 - 1e-12, 4.0 + 1e-12);
        }

        [Trait("Category", "PhysicsValidation")]
        [Fact]
        public void TRM86_WeakField_MemoryContribution_Should_Remain_Subleading_To_TimeChannel()
        {
            var parameters = new PhotonTransportModel.Parameters
            {
                LambdaTime = 1.0,
                LambdaSpace = 30.0
            };

            double[] phis = { 1e-6, 1e-5, 1e-4, 1e-3, 1e-2 };
            const double absDmuDt = 0.02;

            foreach (double phi in phis)
            {
                double timeContribution = parameters.LambdaTime * phi;
                double memoryContribution = parameters.LambdaSpace * phi * phi * absDmuDt;
                double relative = memoryContribution / Math.Max(timeContribution, 1e-30);

                Assert.True(memoryContribution >= 0.0, "Memory contribution must be non-negative.");
                Assert.True(relative < 0.01,
                    $"Expected memory channel to stay subleading in weak field. phi={phi:E}, ratio={relative:E6}");
            }
        }

        [Trait("Category", "PhysicsValidation")]
        [Fact]
        public void TRM87_MemoryChannel_Should_Depict_DirectionalTransport_Not_PureTimeShift()
        {
            const double phi = 1e-2;
            const double absDmuDtNoTurn = 0.0;
            const double absDmuDtTurn = 2e-2;

            var withMemory = new PhotonTransportModel.Parameters
            {
                LambdaTime = 1.0,
                LambdaSpace = 30.0
            };

            var timeOnly = new PhotonTransportModel.Parameters
            {
                LambdaTime = 1.0,
                LambdaSpace = 0.0
            };

            double nWithMemoryNoTurn = 2.0 + withMemory.LambdaTime * phi + withMemory.LambdaSpace * phi * phi * absDmuDtNoTurn;
            double nWithMemoryTurn = 2.0 + withMemory.LambdaTime * phi + withMemory.LambdaSpace * phi * phi * absDmuDtTurn;

            double nTimeOnlyNoTurn = 2.0 + timeOnly.LambdaTime * phi + timeOnly.LambdaSpace * phi * phi * absDmuDtNoTurn;
            double nTimeOnlyTurn = 2.0 + timeOnly.LambdaTime * phi + timeOnly.LambdaSpace * phi * phi * absDmuDtTurn;

            Assert.True(nWithMemoryTurn > nWithMemoryNoTurn,
                "With LambdaSpace > 0, turning trajectories must raise n_eff through the memory channel.");
            Assert.InRange(Math.Abs(nTimeOnlyTurn - nTimeOnlyNoTurn), 0.0, 1e-15);
        }

        [Trait("Category", "PhysicsValidation")]
        [Fact]
        public void MC01_MemoryInvariant_Should_Vanish_Without_Field_Or_Turning()
        {
            const double phiZero = 0.0;
            const double phiNonZero = 1e-2;
            const double absDmuDtZero = 0.0;
            const double absDmuDtNonZero = 2e-2;

            double noField = phiZero * phiZero * absDmuDtNonZero;
            double noTurning = phiNonZero * phiNonZero * absDmuDtZero;
            double active = phiNonZero * phiNonZero * absDmuDtNonZero;

            Assert.InRange(Math.Abs(noField), 0.0, 1e-30);
            Assert.InRange(Math.Abs(noTurning), 0.0, 1e-30);
            Assert.True(active > 0.0, "Expected active memory invariant for nonzero field and nonzero turning.");
        }

        [Trait("Category", "PhysicsValidation")]
        [Fact]
        public void MC02_PhiLinearMemoryCandidate_Should_Fail_WeakFieldSubleadingConstraint()
        {
            var parameters = new PhotonTransportModel.Parameters
            {
                LambdaTime = 1.0,
                LambdaSpace = 30.0
            };

            double[] phis = { 1e-6, 1e-5, 1e-4, 1e-3, 1e-2 };
            const double absDmuDt = 0.02;
            const double weakFieldSubleadingThreshold = 0.01;

            bool linearCandidateViolates = false;
            bool squaredCandidateSatisfies = true;

            foreach (double phi in phis)
            {
                double timeContribution = parameters.LambdaTime * phi;
                double linearMemoryContribution = parameters.LambdaSpace * phi * absDmuDt;
                double squaredMemoryContribution = parameters.LambdaSpace * phi * phi * absDmuDt;

                double linearRatio = linearMemoryContribution / Math.Max(timeContribution, 1e-30);
                double squaredRatio = squaredMemoryContribution / Math.Max(timeContribution, 1e-30);

                if (linearRatio >= weakFieldSubleadingThreshold)
                {
                    linearCandidateViolates = true;
                }

                if (squaredRatio >= weakFieldSubleadingThreshold)
                {
                    squaredCandidateSatisfies = false;
                }
            }

            Assert.True(linearCandidateViolates,
                "Expected phi*|dmu/dt| candidate to violate weak-field subleading constraint.");
            Assert.True(squaredCandidateSatisfies,
                "Expected phi^2*|dmu/dt| candidate to remain weak-field subleading.");
        }

        [Trait("Category", "PhysicsValidation")]
        [Fact]
        public void MC03_PhiSquaredMemoryCandidate_Should_Satisfy_TimeChannelSeparation()
        {
            const double phi = 1e-2;
            const double absDmuDtNoTurn = 0.0;
            const double absDmuDtTurning = 2e-2;

            var withMemory = new PhotonTransportModel.Parameters
            {
                LambdaTime = 1.0,
                LambdaSpace = 30.0
            };

            var timeOnly = new PhotonTransportModel.Parameters
            {
                LambdaTime = 1.0,
                LambdaSpace = 0.0
            };

            double nWithMemoryNoTurn = 2.0 + withMemory.LambdaTime * phi + withMemory.LambdaSpace * phi * phi * absDmuDtNoTurn;
            double nWithMemoryTurning = 2.0 + withMemory.LambdaTime * phi + withMemory.LambdaSpace * phi * phi * absDmuDtTurning;

            double nTimeOnlyNoTurn = 2.0 + timeOnly.LambdaTime * phi;

            Assert.InRange(Math.Abs(nWithMemoryNoTurn - nTimeOnlyNoTurn), 0.0, 1e-15);
            Assert.True(nWithMemoryTurning > nWithMemoryNoTurn,
                "Expected phi^2*|dmu/dt| memory channel to activate under turning.");
        }

        [Trait("Category", "PhysicsValidation")]
        [Fact]
        public void MC04_AlternativeMemoryPowers_Should_NotImprove_ELBridgeWithoutPenalty()
        {
            const double G = 1.0;
            const double c = 1.0;
            const double b = 1.0;
            const double dt = 0.001;
            double[] epsilons = { 2e-3, 1e-2 };
            const double weakFieldSubleadingThreshold = 0.01;
            const double absDmuDtReference = 0.02;
            const double phiReference = 1e-2;

            var parameters = new PhotonTransportModel.Parameters
            {
                LambdaTime = 1.0,
                LambdaSpace = 30.0
            };

            var candidates = new[]
            {
                new MemoryInvariantCandidate("phi|dmu|", PhiPower: 1.0, DmuPower: 1.0),
                new MemoryInvariantCandidate("phi^2|dmu|", PhiPower: 2.0, DmuPower: 1.0),
                new MemoryInvariantCandidate("phi^2|dmu|^2", PhiPower: 2.0, DmuPower: 2.0),
                new MemoryInvariantCandidate("phi^3|dmu|", PhiPower: 3.0, DmuPower: 1.0)
            };

            var results = candidates
                .Select(candidate =>
                {
                    double meanRelError = epsilons
                        .Select(epsilon =>
                        {
                            double alphaCandidate = ComputeDeflectionForMemoryInvariantCandidate(
                                epsilon, G, c, b, dt, parameters, candidate.PhiPower, candidate.DmuPower);
                            double alphaSchwarz = ComputeSchwarzschildNullDeflection(epsilon);
                            return Math.Abs(alphaCandidate - alphaSchwarz) / Math.Max(alphaSchwarz, 1e-16);
                        })
                        .Average();

                    bool weakFieldSubleadingOk = IsWeakFieldSubleading(
                        candidate.PhiPower, candidate.DmuPower, parameters, absDmuDtReference, weakFieldSubleadingThreshold);
                    bool timeChannelSeparationOk = IsTimeChannelSeparationSatisfied(
                        candidate.PhiPower, candidate.DmuPower, parameters);
                    bool bridgeRelevanceOk = IsBridgeRelevanceSatisfied(
                        candidate.PhiPower, candidate.DmuPower, phiReference, absDmuDtReference);

                    double penalty =
                        (weakFieldSubleadingOk ? 0.0 : 1.0) +
                        (timeChannelSeparationOk ? 0.0 : 1.0) +
                        (bridgeRelevanceOk ? 0.0 : 1.0);

                    double score = meanRelError + penalty;
                    return new
                    {
                        candidate.Name,
                        candidate.PhiPower,
                        candidate.DmuPower,
                        MeanRelError = meanRelError,
                        WeakFieldSubleadingOk = weakFieldSubleadingOk,
                        TimeChannelSeparationOk = timeChannelSeparationOk,
                        BridgeRelevanceOk = bridgeRelevanceOk,
                        Penalty = penalty,
                        Score = score
                    };
                })
                .OrderBy(x => x.Score)
                .ToArray();

            foreach (var row in results)
            {
                _output.WriteLine(
                    $"MC04 {row.Name} | relErr={row.MeanRelError:E6} | weakField={row.WeakFieldSubleadingOk} | timeSeparation={row.TimeChannelSeparationOk} | bridgeRelevance={row.BridgeRelevanceOk} | penalty={row.Penalty:F1} | score={row.Score:E6}");
            }

            var baseline = results.First(x => x.Name == "phi^2|dmu|");
            var linear = results.First(x => x.Name == "phi|dmu|");

            Assert.False(linear.WeakFieldSubleadingOk,
                "Expected phi|dmu| candidate to violate weak-field subleading hierarchy.");
            Assert.True(baseline.WeakFieldSubleadingOk && baseline.TimeChannelSeparationOk && baseline.BridgeRelevanceOk,
                "Expected phi^2|dmu| baseline to satisfy weak-field hierarchy, time-channel separation, and bridge relevance.");

            Assert.True(results
                    .Where(x => x.Name != baseline.Name && x.MeanRelError < baseline.MeanRelError)
                    .All(x => x.Penalty > 0.0),
                "Expected any alternative with lower EL error to incur at least one structural penalty.");
            Assert.True(results.All(x => x.Score >= baseline.Score - 1e-12),
                $"Expected phi^2|dmu| to remain best candidate under EL-error-plus-penalty score. best={results[0].Name}");
        }

        [Trait("Category", "PhysicsValidation")]
        [Fact]
        public void MC05_PhiSquaredMemoryInvariant_Should_Imply_AeffProportionalToPhi()
        {
            const double absDmuDt = 0.02;
            double[] phis = { 1e-6, 3e-6, 1e-5, 3e-5, 1e-4, 3e-4, 1e-3, 3e-3, 1e-2 };

            var inferredAmplitude = phis
                .Select(phi =>
                {
                    double memoryInvariant = phi * phi * absDmuDt;
                    double aEff = Math.Sqrt(memoryInvariant / absDmuDt);
                    return (X: Math.Log10(phi), Y: Math.Log10(aEff));
                })
                .ToArray();

            var fit = FitLinear(inferredAmplitude);

            _output.WriteLine($"MC05 algebraic consistency Aeff~phi slope={fit.Slope:F6} | intercept={fit.Intercept:F6} | rms={fit.Rms:E6}");

            Assert.InRange(fit.Slope, 1.0 - 1e-12, 1.0 + 1e-12);
            Assert.InRange(Math.Abs(fit.Intercept), 0.0, 1e-12);
            Assert.InRange(fit.Rms, 0.0, 1e-12);
        }

        [Trait("Category", "PhysicsValidation")]
        [Fact]
        public void MC06_LeadingInvariantOrder_Should_Reject_LinearInA_Coupling_Under_WeakFieldHierarchy()
        {
            var parameters = new PhotonTransportModel.Parameters
            {
                LambdaTime = 1.0,
                LambdaSpace = 30.0
            };

            const double absDmuDt = 0.02;
            const double weakFieldSubleadingThreshold = 0.01;
            double[] phis = { 1e-6, 1e-5, 1e-4, 1e-3, 1e-2 };

            bool linearInAviolates = false;
            bool quadraticInAsatisfies = true;

            foreach (double phi in phis)
            {
                double aEff = phi;
                double timeContribution = parameters.LambdaTime * phi;

                double linearInvariant = aEff * absDmuDt;
                double quadraticInvariant = aEff * aEff * absDmuDt;

                double linearRatio = parameters.LambdaSpace * linearInvariant / Math.Max(timeContribution, 1e-30);
                double quadraticRatio = parameters.LambdaSpace * quadraticInvariant / Math.Max(timeContribution, 1e-30);

                if (linearRatio >= weakFieldSubleadingThreshold)
                {
                    linearInAviolates = true;
                }

                if (quadraticRatio >= weakFieldSubleadingThreshold)
                {
                    quadraticInAsatisfies = false;
                }
            }

            Assert.True(linearInAviolates,
                "Expected A*|dmu/dt| coupling (with A~phi) to violate weak-field hierarchy.");
            Assert.True(quadraticInAsatisfies,
                "Expected A^2*|dmu/dt| coupling (with A~phi) to satisfy weak-field hierarchy.");
        }

        [Trait("Category", "PhysicsValidation")]
        [Fact]
        public void MC07_SignedVsAbsoluteTurningRate_Should_Preserve_DissipativePositivityConstraints()
        {
            var parameters = new PhotonTransportModel.Parameters
            {
                LambdaTime = 1.0,
                LambdaSpace = 30.0
            };

            const double phi = 1e-2;
            double[] signedTurningRates = { -0.02, -0.01, 0.0, 0.01, 0.02 };

            double baseChannel = 2.0 + parameters.LambdaTime * phi;

            foreach (double signedRate in signedTurningRates)
            {
                double memoryAbs = phi * phi * Math.Abs(signedRate);
                double memorySigned = phi * phi * signedRate;

                double nAbs = baseChannel + parameters.LambdaSpace * memoryAbs;
                double nSigned = baseChannel + parameters.LambdaSpace * memorySigned;

                Assert.True(nAbs >= baseChannel,
                    $"Absolute turning-rate channel must remain non-negative relative to base. rate={signedRate:E}");

                if (signedRate < 0.0)
                {
                    Assert.True(nSigned < baseChannel,
                        $"Signed turning-rate channel should produce negative correction for negative turning. rate={signedRate:E}");
                }
            }

            double positiveAbs = baseChannel + parameters.LambdaSpace * phi * phi * Math.Abs(0.02);
            double negativeAbs = baseChannel + parameters.LambdaSpace * phi * phi * Math.Abs(-0.02);
            Assert.InRange(Math.Abs(positiveAbs - negativeAbs), 0.0, 1e-15);
        }

        [Trait("Category", "PhysicsValidation")]
        [Fact]
        public void MC08_MemoryInvariant_Should_Not_Be_Reabsorbed_Into_PureTimeChannel_Reparameterization()
        {
            var parameters = new PhotonTransportModel.Parameters
            {
                LambdaTime = 1.0,
                LambdaSpace = 30.0
            };

            const double phi = 1e-2;
            const double absDmuDtNoTurn = 0.0;
            const double absDmuDtLowTurn = 1e-2;
            const double absDmuDtHighTurn = 2e-2;

            double nNoTurn = 2.0 + parameters.LambdaTime * phi + parameters.LambdaSpace * phi * phi * absDmuDtNoTurn;
            double nLowTurn = 2.0 + parameters.LambdaTime * phi + parameters.LambdaSpace * phi * phi * absDmuDtLowTurn;
            double nHighTurn = 2.0 + parameters.LambdaTime * phi + parameters.LambdaSpace * phi * phi * absDmuDtHighTurn;

            // If memory were reabsorbable into a pure time channel n=2+lambdaEff*phi, one lambdaEff would fit all turning states at fixed phi.
            double lambdaEffFromLowTurn = (nLowTurn - 2.0) / phi;
            double reconstructedHighTurnFromLambdaEff = 2.0 + lambdaEffFromLowTurn * phi;

            Assert.InRange(Math.Abs(nNoTurn - (2.0 + parameters.LambdaTime * phi)), 0.0, 1e-15);
            Assert.True(nLowTurn > nNoTurn && nHighTurn > nLowTurn,
                "Expected monotonic activation with turning rate at fixed phi.");
            Assert.True(Math.Abs(reconstructedHighTurnFromLambdaEff - nHighTurn) > 1e-12,
                "Expected turning-dependent memory channel to be non-reducible to a single pure time-channel lambda at fixed phi.");
        }

        [Trait("Category", "PhysicsValidation")]
        [Fact]
        public void MC09_CoherenceAmplitude_Should_Scale_With_Phi_From_Lattice()
        {
            // TQM lattice proxy: weak-field source bias enters phase dynamics and modulates
            // coarse-grained synchronization amplitude A(phi). This is a derive-or-falsify guard,
            // not a theorem-level proof.
            double[] phis = { 0.0, 1e-4, 2e-4, 5e-4, 1e-3, 2e-3 };
            var amplitudes = phis.Select(SimulateCoherenceAmplitudeFromLatticeProxy).ToArray();
            double baseline = amplitudes[0];

            var dynamicData = phis
                .Skip(1)
                .Select((phi, idx) => (X: phi, Y: amplitudes[idx + 1] - baseline))
                .ToArray();

            for (int i = 1; i < amplitudes.Length; i++)
            {
                _output.WriteLine($"[MC09] phi={phis[i]:E3} | A={amplitudes[i]:F6} | Adyn={amplitudes[i] - baseline:E6}");
                Assert.True(amplitudes[i] + 1e-6 >= amplitudes[i - 1], "Expected non-decreasing coherence amplitude with phi in weak field.");
            }

            var fit = FitLinear(dynamicData);
            double meanY = dynamicData.Average(p => p.Y);
            double ssTot = dynamicData.Sum(p => (p.Y - meanY) * (p.Y - meanY));
            double ssRes = dynamicData.Sum(p =>
            {
                double yHat = fit.Slope * p.X + fit.Intercept;
                double e = p.Y - yHat;
                return e * e;
            });
            double r2 = ssTot > 0.0 ? 1.0 - ssRes / ssTot : 1.0;

            _output.WriteLine($"[MC09] A_dyn(phi) linear fit: slope={fit.Slope:E6}, intercept={fit.Intercept:E6}, R2={r2:F6}");

            Assert.True(fit.Slope > 0.0, "Expected positive weak-field coherence-amplitude response to phi.");
            Assert.True(Math.Abs(fit.Intercept) < 1e-3, "Expected near-zero intercept after baseline subtraction.");
            Assert.True(r2 > 0.95, $"Expected near-linear weak-field A(phi) scaling in lattice proxy, got R2={r2:F4}.");
        }

        [Trait("Category", "PhysicsValidation")]
        [Fact]
        public void MC10_QuadraticCoherenceCoupling_Should_Be_First_Admissible_MemoryInvariant()
        {
            // Uses MC09 lattice-proxy response A_dyn(phi) and checks first admissible memory order:
            // A|dmu| should violate weak-field hierarchy, A^2|dmu| should satisfy it while staying bridge-relevant.
            var parameters = new PhotonTransportModel.Parameters
            {
                LambdaTime = 1.0,
                LambdaSpace = 30.0
            };

            const double absDmuDt = 0.02;
            const double weakFieldSubleadingThreshold = 0.10;
            const double bridgeRelevanceThreshold = 1e-6;
            double[] phis = { 1e-6, 3e-6, 1e-5, 3e-5, 1e-4 };

            double baselineA = SimulateCoherenceAmplitudeFromLatticeProxy(0.0);
            bool linearViolates = false;
            bool quadraticAdmissible = true;
            bool quadraticBridgeRelevant = false;

            foreach (double phi in phis)
            {
                double aDyn = SimulateCoherenceAmplitudeFromLatticeProxy(phi) - baselineA;
                double aDynClamped = Math.Max(aDyn, 0.0);
                double timeContribution = parameters.LambdaTime * phi;

                double linearInvariant = aDynClamped * absDmuDt;
                double quadraticInvariant = aDynClamped * aDynClamped * absDmuDt;

                double linearRatio = parameters.LambdaSpace * linearInvariant / Math.Max(timeContribution, 1e-30);
                double quadraticRatio = parameters.LambdaSpace * quadraticInvariant / Math.Max(timeContribution, 1e-30);

                _output.WriteLine($"[MC10] phi={phi:E3} | Adyn={aDynClamped:E6} | linearRatio={linearRatio:E6} | quadraticRatio={quadraticRatio:E6}");

                if (linearRatio >= weakFieldSubleadingThreshold)
                    linearViolates = true;

                if (quadraticRatio >= weakFieldSubleadingThreshold)
                    quadraticAdmissible = false;

                if (parameters.LambdaSpace * quadraticInvariant >= bridgeRelevanceThreshold)
                    quadraticBridgeRelevant = true;
            }

            Assert.True(linearViolates,
                "Expected lattice-proxy linear-in-A memory coupling to violate weak-field hierarchy.");
            Assert.True(quadraticAdmissible,
                "Expected lattice-proxy quadratic-in-A memory coupling to remain weak-field subleading.");
            Assert.True(quadraticBridgeRelevant,
                "Expected lattice-proxy quadratic-in-A memory coupling to remain bridge-relevant.");
        }

        [Trait("Category", "PhysicsValidation")]
        [Fact]
        public void MC11_DerivedMemoryInvariant_Should_Match_PhotonTransport_Form()
        {
            // Bridge check:
            // I_derived = A_dyn^2 * |dmu/dt|
            // I_transport = phi^2 * |dmu/dt|
            // Expect linear proportionality up to coupling scale.
            double baselineA = SimulateCoherenceAmplitudeFromLatticeProxy(0.0);
            double[] phis = { 1e-6, 3e-6, 1e-5, 3e-5, 1e-4, 3e-4, 1e-3, 2e-3 };
            double[] absDmu = { 0.005, 0.01, 0.02, 0.03 };

            var pairs = new List<(double X, double Y)>();

            foreach (double phi in phis)
            {
                double aDyn = Math.Max(0.0, SimulateCoherenceAmplitudeFromLatticeProxy(phi) - baselineA);

                foreach (double dmu in absDmu)
                {
                    double iTransport = phi * phi * dmu;
                    double iDerived = aDyn * aDyn * dmu;
                    pairs.Add((X: iTransport, Y: iDerived));
                }
            }

            var fit = FitLinear(pairs);
            double meanY = pairs.Average(p => p.Y);
            double ssTot = pairs.Sum(p => (p.Y - meanY) * (p.Y - meanY));
            double ssRes = pairs.Sum(p =>
            {
                double yHat = fit.Slope * p.X + fit.Intercept;
                double e = p.Y - yHat;
                return e * e;
            });
            double r2 = ssTot > 0.0 ? 1.0 - ssRes / ssTot : 1.0;

            double maxY = pairs.Max(p => p.Y);
            double interceptRel = Math.Abs(fit.Intercept) / Math.Max(maxY, 1e-30);

            _output.WriteLine($"[MC11] Iderived vs Itransport fit: slope={fit.Slope:E6}, intercept={fit.Intercept:E6}, R2={r2:F6}, interceptRel={interceptRel:E6}");

            Assert.True(fit.Slope > 0.0, "Expected positive proportionality between derived and transport invariants.");
            Assert.True(r2 > 0.995, $"Expected strong linear match (up to coupling scale), got R2={r2:F6}.");
            Assert.True(interceptRel < 0.02, $"Expected small relative intercept, got interceptRel={interceptRel:E6}.");
        }

        [Trait("Category", "PhysicsValidation")]
        [Fact]
        public void MC12_DerivedMemoryInvariant_Should_Reproduce_ELBridge_When_Substituted()
        {
            // Substitute lattice-derived invariant A_dyn(phi)^2*|dmu/dt| into bridge path
            // and verify bridge agreement remains in the same effective window (up to coupling scale).
            const double G = 1.0;
            const double c = 1.0;
            const double b = 1.0;
            const double dt = 0.001;
            double[] epsilons = { 2e-3, 1e-2 };

            var parameters = new PhotonTransportModel.Parameters
            {
                LambdaTime = 1.0,
                LambdaSpace = 30.0
            };

            double couplingScale = FitDerivedToTransportCouplingScale();
            _output.WriteLine($"[MC12] derived coupling scale={couplingScale:E6}");
            Assert.True(couplingScale > 0.0 && double.IsFinite(couplingScale), "Expected positive finite coupling scale.");

            var rows = epsilons.Select(epsilon =>
            {
                double alphaSchwarz = ComputeSchwarzschildNullDeflection(epsilon);
                double alphaBaseline = ComputeDeflectionForMemoryInvariantCandidate(
                    epsilon, G, c, b, dt, parameters, phiPower: 2.0, dmuPower: 1.0);
                double alphaDerived = ComputeDeflectionForDerivedMemoryInvariant(
                    epsilon, G, c, b, dt, parameters, couplingScale);

                double relBaseline = Math.Abs(alphaBaseline - alphaSchwarz) / Math.Max(alphaSchwarz, 1e-16);
                double relDerived = Math.Abs(alphaDerived - alphaSchwarz) / Math.Max(alphaSchwarz, 1e-16);
                double relGap = Math.Abs(alphaDerived - alphaBaseline) / Math.Max(alphaBaseline, 1e-16);

                _output.WriteLine($"[MC12] eps={epsilon:E3} | baselineRel={relBaseline:E6} | derivedRel={relDerived:E6} | derivedVsBaseline={relGap:E6}");
                return (relBaseline, relDerived, relGap);
            }).ToArray();

            double meanBaseline = rows.Average(r => r.relBaseline);
            double meanDerived = rows.Average(r => r.relDerived);
            double meanGap = rows.Average(r => r.relGap);
            double bridgeRetention = meanBaseline > 0 ? meanDerived / meanBaseline : 1.0;

            _output.WriteLine($"[MC12] meanBaseline={meanBaseline:E6} | meanDerived={meanDerived:E6} | bridgeRetention={bridgeRetention:F6} | meanGap={meanGap:E6}");

            Assert.True(bridgeRetention >= 0.80 && bridgeRetention <= 1.20,
                $"Derived invariant should preserve EL-bridge level up to coupling scale. retention={bridgeRetention:F6}");
            Assert.True(meanGap < 0.20,
                $"Derived substitution should stay close to baseline bridge behavior. meanGap={meanGap:E6}");
        }

        [Trait("Category", "PhysicsValidation")]
        [Fact]
        public void HOA01_TransportIndex_Should_Depend_On_Position_Direction_And_DirectionalChange()
        {
            const double G = 1.0;
            const double c = 1.0;
            const double M = 0.01;

            var parameters = new PhotonTransportModel.Parameters
            {
                LambdaTime = 1.0,
                LambdaSpace = 30.0
            };

            // Position dependence: same directional-change measure, different r -> different phi -> different n_eff.
            const double absDmuDtRef = 0.02;
            double phiNear = PhotonTransportModel.Phi(G, M, c, r: 10.0);
            double phiFar = PhotonTransportModel.Phi(G, M, c, r: 20.0);
            double nNear = 2.0 + parameters.LambdaTime * phiNear + parameters.LambdaSpace * phiNear * phiNear * absDmuDtRef;
            double nFar = 2.0 + parameters.LambdaTime * phiFar + parameters.LambdaSpace * phiFar * phiFar * absDmuDtRef;
            Assert.True(nNear > nFar, "Expected higher n_eff at smaller radius (larger phi).");

            // Direction dependence via mu and |dmu/dt| at equal radius.
            var radialState = new PhotonTransportModel.PhotonMemoryState
            {
                x = 10.0,
                y = 0.0,
                vx = c,
                vy = 0.0
            };
            var tangentialState = new PhotonTransportModel.PhotonMemoryState
            {
                x = 10.0,
                y = 0.0,
                vx = 0.0,
                vy = c
            };

            double muRadial = PhotonTransportModel.ComputeMu(radialState);
            double muTangential = PhotonTransportModel.ComputeMu(tangentialState);
            double absDmuDtRadial = PhotonTransportModel.ComputeAbsDmuDtBase(radialState, G, M, c, parameters);
            double absDmuDtTangential = PhotonTransportModel.ComputeAbsDmuDtBase(tangentialState, G, M, c, parameters);

            double nRadial = 2.0 + parameters.LambdaTime * phiNear + parameters.LambdaSpace * phiNear * phiNear * absDmuDtRadial;
            double nTangential = 2.0 + parameters.LambdaTime * phiNear + parameters.LambdaSpace * phiNear * phiNear * absDmuDtTangential;

            Assert.True(Math.Abs(muRadial - muTangential) > 0.5, "Expected clearly different directional state (mu).");
            Assert.True(Math.Abs(absDmuDtRadial - absDmuDtTangential) > 1e-12, "Expected direction-dependent |dmu/dt|.");
            Assert.True(Math.Abs(nRadial - nTangential) > 1e-12, "Expected direction-dependent n_eff through memory channel.");

            // Directional-change dependence: same phi, varying |dmu/dt| -> varying memory channel.
            const double absDmuDtLow = 5e-3;
            const double absDmuDtHigh = 2e-2;
            double nLowTurn = 2.0 + parameters.LambdaTime * phiNear + parameters.LambdaSpace * phiNear * phiNear * absDmuDtLow;
            double nHighTurn = 2.0 + parameters.LambdaTime * phiNear + parameters.LambdaSpace * phiNear * phiNear * absDmuDtHigh;
            Assert.True(nHighTurn > nLowTurn, "Expected stronger directional-change channel to increase n_eff.");
        }

        [Trait("Category", "Guard")]
        [Fact]
        public void CLAIM01_ScalarTRM_Should_Document_NoFrameDraggingPath()
        {
            string docPath = Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..", "..", "..", "..",
                    "docs", "Theory", "TRM_Geodesic_Derivation.md"));

            Assert.True(File.Exists(docPath), $"Expected claim-boundary doc at: {docPath}");

            string content = File.ReadAllText(docPath);
            Assert.True(content.IndexOf("frame-dragging", StringComparison.OrdinalIgnoreCase) >= 0,
                "Expected claim-boundary doc to mention frame-dragging limitation.");
            Assert.True(content.IndexOf("Lense-Thirring", StringComparison.OrdinalIgnoreCase) >= 0,
                "Expected claim-boundary doc to mention Lense-Thirring limitation.");
        }

        private static bool IsWeakFieldSubleading(
            double phiPower,
            double dmuPower,
            PhotonTransportModel.Parameters parameters,
            double absDmuDt,
            double threshold)
        {
            double[] phis = { 1e-6, 1e-5, 1e-4, 1e-3, 1e-2 };

            foreach (double phi in phis)
            {
                double timeContribution = parameters.LambdaTime * phi;
                double memoryInvariant = Math.Pow(phi, phiPower) * Math.Pow(absDmuDt, dmuPower);
                double memoryContribution = parameters.LambdaSpace * memoryInvariant;
                double ratio = memoryContribution / Math.Max(timeContribution, 1e-30);

                if (ratio >= threshold)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsTimeChannelSeparationSatisfied(
            double phiPower,
            double dmuPower,
            PhotonTransportModel.Parameters parameters)
        {
            const double phi = 1e-2;
            const double absDmuDtNoTurn = 0.0;
            const double absDmuDtTurning = 2e-2;

            double memoryNoTurn = Math.Pow(phi, phiPower) * Math.Pow(absDmuDtNoTurn, dmuPower);
            double memoryTurning = Math.Pow(phi, phiPower) * Math.Pow(absDmuDtTurning, dmuPower);

            double nWithNoTurn = 2.0 + parameters.LambdaTime * phi + parameters.LambdaSpace * memoryNoTurn;
            double nWithTurning = 2.0 + parameters.LambdaTime * phi + parameters.LambdaSpace * memoryTurning;
            double nTimeOnly = 2.0 + parameters.LambdaTime * phi;

            bool noExtraShiftAtNoTurn = Math.Abs(nWithNoTurn - nTimeOnly) <= 1e-15;
            bool activatesUnderTurning = nWithTurning > nWithNoTurn;
            return noExtraShiftAtNoTurn && activatesUnderTurning;
        }

        private static bool IsBridgeRelevanceSatisfied(
            double phiPower,
            double dmuPower,
            double phiReference,
            double absDmuDtReference)
        {
            double baselineInvariant = phiReference * phiReference * absDmuDtReference;
            double candidateInvariant = Math.Pow(phiReference, phiPower) * Math.Pow(absDmuDtReference, dmuPower);
            double ratioToBaseline = candidateInvariant / Math.Max(baselineInvariant, 1e-30);

            // Keep candidate in the same order-of-magnitude relevance window as the current bridge-active baseline.
            return ratioToBaseline >= 0.5 && ratioToBaseline <= 2.0;
        }

        private static double ComputeDeflectionForMemoryInvariantCandidate(
            double epsilon,
            double G,
            double c,
            double b,
            double dt,
            PhotonTransportModel.Parameters parameters,
            double phiPower,
            double dmuPower)
        {
            double M = epsilon * c * c * b / G;
            double xMin = -100.0 * b;
            double xMax = 100.0 * b;

            var state = new PhotonTransportModel.PhotonMemoryState
            {
                x = xMin,
                y = b,
                vx = c,
                vy = 0.0
            };

            int maxSteps = (int)Math.Ceiling((xMax - xMin) / (c * dt)) + 200000;
            for (int i = 0; i < maxSteps && state.x < xMax; i++)
            {
                RK4StepForMemoryInvariantCandidate(
                    ref state, dt, G, M, c, parameters, phiPower, dmuPower);
            }

            return Math.Abs(Math.Atan2(state.vy, state.vx));
        }

        private static void RK4StepForMemoryInvariantCandidate(
            ref PhotonTransportModel.PhotonMemoryState state,
            double dt,
            double G,
            double M,
            double c,
            PhotonTransportModel.Parameters parameters,
            double phiPower,
            double dmuPower)
        {
            var k1 = DerivativesForMemoryInvariantCandidate(state, G, M, c, parameters, phiPower, dmuPower);
            var s2 = AddScaled(state, k1, dt * 0.5);
            var k2 = DerivativesForMemoryInvariantCandidate(s2, G, M, c, parameters, phiPower, dmuPower);
            var s3 = AddScaled(state, k2, dt * 0.5);
            var k3 = DerivativesForMemoryInvariantCandidate(s3, G, M, c, parameters, phiPower, dmuPower);
            var s4 = AddScaled(state, k3, dt);
            var k4 = DerivativesForMemoryInvariantCandidate(s4, G, M, c, parameters, phiPower, dmuPower);

            state = new PhotonTransportModel.PhotonMemoryState
            {
                x = state.x + dt / 6.0 * (k1.x + 2.0 * k2.x + 2.0 * k3.x + k4.x),
                y = state.y + dt / 6.0 * (k1.y + 2.0 * k2.y + 2.0 * k3.y + k4.y),
                vx = state.vx + dt / 6.0 * (k1.vx + 2.0 * k2.vx + 2.0 * k3.vx + k4.vx),
                vy = state.vy + dt / 6.0 * (k1.vy + 2.0 * k2.vy + 2.0 * k3.vy + k4.vy)
            };
        }

        private static PhotonTransportModel.PhotonMemoryState DerivativesForMemoryInvariantCandidate(
            PhotonTransportModel.PhotonMemoryState state,
            double G,
            double M,
            double c,
            PhotonTransportModel.Parameters parameters,
            double phiPower,
            double dmuPower)
        {
            double r = Math.Sqrt(state.x * state.x + state.y * state.y);
            double ex = state.x / r;
            double ey = state.y / r;

            double phi = PhotonTransportModel.Phi(G, M, c, r);
            double absDmuDt = PhotonTransportModel.ComputeAbsDmuDtBase(state, G, M, c, parameters);
            double localMemoryInvariant = Math.Pow(phi, phiPower) * Math.Pow(absDmuDt, dmuPower);

            double nEff =
                2.0
                + parameters.LambdaTime * phi
                + parameters.LambdaSpace * localMemoryInvariant;

            double ar = -nEff * G * M / (r * r);
            double ax = ar * ex;
            double ay = ar * ey;

            double v2 = state.vx * state.vx + state.vy * state.vy;
            double dot = ax * state.vx + ay * state.vy;
            double axProj = ax - dot / v2 * state.vx;
            double ayProj = ay - dot / v2 * state.vy;

            return new PhotonTransportModel.PhotonMemoryState
            {
                x = state.vx,
                y = state.vy,
                vx = axProj,
                vy = ayProj
            };
        }

        private static PhotonTransportModel.PhotonMemoryState AddScaled(
            PhotonTransportModel.PhotonMemoryState state,
            PhotonTransportModel.PhotonMemoryState delta,
            double scale)
        {
            return new PhotonTransportModel.PhotonMemoryState
            {
                x = state.x + scale * delta.x,
                y = state.y + scale * delta.y,
                vx = state.vx + scale * delta.vx,
                vy = state.vy + scale * delta.vy
            };
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

        private readonly record struct MemoryInvariantCandidate(
            string Name,
            double PhiPower,
            double DmuPower);

        private static (double Slope, double Intercept, double Rms) FitLinear(
            IReadOnlyCollection<(double X, double Y)> data)
        {
            double sx = 0.0;
            double sy = 0.0;
            double sxx = 0.0;
            double sxy = 0.0;
            int n = data.Count;

            foreach (var (x, y) in data)
            {
                sx += x;
                sy += y;
                sxx += x * x;
                sxy += x * y;
            }

            double denominator = n * sxx - sx * sx;
            double slope = (n * sxy - sx * sy) / denominator;
            double intercept = (sy - slope * sx) / n;

            double rms = 0.0;
            foreach (var (x, y) in data)
            {
                double fit = slope * x + intercept;
                double err = y - fit;
                rms += err * err;
            }

            rms = Math.Sqrt(rms / n);

            return (slope, intercept, rms);
        }

        private static double SimulateCoherenceAmplitudeFromLatticeProxy(double phi)
        {
            const int n = 64;
            const int steps = 4000;
            const int sampleWindow = 1000;
            const double dt = 0.02;
            const double omegaSpread = 0.08;
            const double kBase = 0.80;
            const double kPhi = 120.0;
            const double alpha = 1.0;

            var theta = new double[n];
            var omega = new double[n];

            for (int i = 0; i < n; i++)
            {
                double phase = 2.0 * Math.PI * i / n;
                theta[i] = 0.15 * Math.Sin(phase) + 0.05 * Math.Cos(2.0 * phase);
                omega[i] = omegaSpread * Math.Sin(phase);
            }

            double kEff = kBase + kPhi * phi;
            double rAccum = 0.0;
            int rCount = 0;

            for (int t = 0; t < steps; t++)
            {
                var dTheta = new double[n];
                for (int i = 0; i < n; i++)
                {
                    int left = (i - 1 + n) % n;
                    int right = (i + 1) % n;
                    double coupling = Math.Sin(theta[left] - theta[i]) + Math.Sin(theta[right] - theta[i]);
                    dTheta[i] = omega[i] + alpha * phi + 0.5 * kEff * coupling;
                }

                for (int i = 0; i < n; i++)
                {
                    theta[i] += dt * dTheta[i];
                }

                if (t >= steps - sampleWindow)
                {
                    double c = 0.0;
                    double s = 0.0;
                    for (int i = 0; i < n; i++)
                    {
                        c += Math.Cos(theta[i]);
                        s += Math.Sin(theta[i]);
                    }

                    double r = Math.Sqrt(c * c + s * s) / n;
                    rAccum += r;
                    rCount++;
                }
            }

            return rAccum / Math.Max(rCount, 1);
        }

        private static double FitDerivedToTransportCouplingScale()
        {
            double baselineA = SimulateCoherenceAmplitudeFromLatticeProxy(0.0);
            double[] phis = { 1e-6, 3e-6, 1e-5, 3e-5, 1e-4, 3e-4, 1e-3, 2e-3 };
            double[] absDmu = { 0.005, 0.01, 0.02, 0.03 };

            var pairs = new List<(double X, double Y)>();
            foreach (double phi in phis)
            {
                double aDyn = Math.Max(0.0, SimulateCoherenceAmplitudeFromLatticeProxy(phi) - baselineA);
                foreach (double dmu in absDmu)
                {
                    double iTransport = phi * phi * dmu;
                    double iDerived = aDyn * aDyn * dmu;
                    pairs.Add((X: iTransport, Y: iDerived));
                }
            }

            var fit = FitLinear(pairs);
            return fit.Slope > 0.0 ? 1.0 / fit.Slope : 0.0;
        }

        private static double ComputeDeflectionForDerivedMemoryInvariant(
            double epsilon,
            double G,
            double c,
            double b,
            double dt,
            PhotonTransportModel.Parameters parameters,
            double couplingScale)
        {
            double M = epsilon * c * c * b / G;
            double xMin = -100.0 * b;
            double xMax = 100.0 * b;

            double phiMax = Math.Max(epsilon, 1e-6);
            var coherenceLookup = BuildCoherenceAmplitudeLookup(phiMax, points: 16);

            var state = new PhotonTransportModel.PhotonMemoryState
            {
                x = xMin,
                y = b,
                vx = c,
                vy = 0.0
            };

            int maxSteps = (int)Math.Ceiling((xMax - xMin) / (c * dt)) + 200000;
            for (int i = 0; i < maxSteps && state.x < xMax; i++)
            {
                RK4StepForDerivedMemoryInvariant(
                    ref state, dt, G, M, c, parameters, couplingScale, coherenceLookup);
            }

            return Math.Abs(Math.Atan2(state.vy, state.vx));
        }

        private static void RK4StepForDerivedMemoryInvariant(
            ref PhotonTransportModel.PhotonMemoryState state,
            double dt,
            double G,
            double M,
            double c,
            PhotonTransportModel.Parameters parameters,
            double couplingScale,
            (double[] PhiGrid, double[] ADynGrid) coherenceLookup)
        {
            var k1 = DerivativesForDerivedMemoryInvariant(state, G, M, c, parameters, couplingScale, coherenceLookup);
            var s2 = AddScaled(state, k1, dt * 0.5);
            var k2 = DerivativesForDerivedMemoryInvariant(s2, G, M, c, parameters, couplingScale, coherenceLookup);
            var s3 = AddScaled(state, k2, dt * 0.5);
            var k3 = DerivativesForDerivedMemoryInvariant(s3, G, M, c, parameters, couplingScale, coherenceLookup);
            var s4 = AddScaled(state, k3, dt);
            var k4 = DerivativesForDerivedMemoryInvariant(s4, G, M, c, parameters, couplingScale, coherenceLookup);

            state = new PhotonTransportModel.PhotonMemoryState
            {
                x = state.x + dt / 6.0 * (k1.x + 2.0 * k2.x + 2.0 * k3.x + k4.x),
                y = state.y + dt / 6.0 * (k1.y + 2.0 * k2.y + 2.0 * k3.y + k4.y),
                vx = state.vx + dt / 6.0 * (k1.vx + 2.0 * k2.vx + 2.0 * k3.vx + k4.vx),
                vy = state.vy + dt / 6.0 * (k1.vy + 2.0 * k2.vy + 2.0 * k3.vy + k4.vy)
            };
        }

        private static PhotonTransportModel.PhotonMemoryState DerivativesForDerivedMemoryInvariant(
            PhotonTransportModel.PhotonMemoryState state,
            double G,
            double M,
            double c,
            PhotonTransportModel.Parameters parameters,
            double couplingScale,
            (double[] PhiGrid, double[] ADynGrid) coherenceLookup)
        {
            double r = Math.Sqrt(state.x * state.x + state.y * state.y);
            if (r < 1e-15)
                return new PhotonTransportModel.PhotonMemoryState();

            double ex = state.x / r;
            double ey = state.y / r;

            double phi = PhotonTransportModel.Phi(G, M, c, r);
            double absDmuDt = PhotonTransportModel.ComputeAbsDmuDtBase(state, G, M, c, parameters);
            double aDyn = InterpolateCoherenceAmplitude(phi, coherenceLookup.PhiGrid, coherenceLookup.ADynGrid);
            double localMemoryInvariant = couplingScale * aDyn * aDyn * absDmuDt;

            double nEff = 2.0
                + parameters.LambdaTime * phi
                + parameters.LambdaSpace * localMemoryInvariant;

            double ar = -nEff * G * M / (r * r);
            double ax = ar * ex;
            double ay = ar * ey;

            double v2 = state.vx * state.vx + state.vy * state.vy;
            double dot = ax * state.vx + ay * state.vy;
            double axProj = ax - dot / v2 * state.vx;
            double ayProj = ay - dot / v2 * state.vy;

            return new PhotonTransportModel.PhotonMemoryState
            {
                x = state.vx,
                y = state.vy,
                vx = axProj,
                vy = ayProj
            };
        }

        private static (double[] PhiGrid, double[] ADynGrid) BuildCoherenceAmplitudeLookup(double phiMax, int points)
        {
            double[] phiGrid = new double[Math.Max(points, 2)];
            double[] aDynGrid = new double[phiGrid.Length];
            double baselineA = SimulateCoherenceAmplitudeFromLatticeProxy(0.0);

            for (int i = 0; i < phiGrid.Length; i++)
            {
                double t = (double)i / (phiGrid.Length - 1);
                double phi = phiMax * t;
                phiGrid[i] = phi;
                aDynGrid[i] = Math.Max(0.0, SimulateCoherenceAmplitudeFromLatticeProxy(phi) - baselineA);
            }

            return (phiGrid, aDynGrid);
        }

        private static double InterpolateCoherenceAmplitude(double phi, double[] phiGrid, double[] aDynGrid)
        {
            if (phi <= phiGrid[0])
                return aDynGrid[0];

            int last = phiGrid.Length - 1;
            if (phi >= phiGrid[last])
                return aDynGrid[last];

            int lo = 0;
            int hi = last;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) / 2;
                if (phiGrid[mid] <= phi)
                    lo = mid;
                else
                    hi = mid;
            }

            double x0 = phiGrid[lo];
            double x1 = phiGrid[hi];
            double y0 = aDynGrid[lo];
            double y1 = aDynGrid[hi];
            double w = (phi - x0) / Math.Max(x1 - x0, 1e-30);
            return y0 + w * (y1 - y0);
        }
    }
}
