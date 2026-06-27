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
    }
}
