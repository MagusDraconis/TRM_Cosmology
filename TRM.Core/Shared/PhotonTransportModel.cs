using System;

namespace TRM.Tests.RealityTests
{
    /// <summary>
    /// TRM photon transport model for RK4-based weak-field deflection and timing diagnostics.
    /// Implements the effective transport index in the current code path:
    /// n_eff = 2 + lambda_t * phi + lambda_s * phi^2 * |dmu/dt|.
    /// Theory link: docs/theory/TRM_Geodesic_Derivation.md.
    /// Review links: docs/review/TRM_Service_Test_Consolidation.md, docs/review/TRM_Real_Physics_Test_Coverage.md.
    /// Status: derived (structural transport form), tested (fixation and reality suites), calibrated (lambda terms), limitation (no standalone production Euler-Lagrange solver).
    /// Related tests: TRM.Tests/RealityTests/PhotonTransportModel_FixationTests.cs, TRM.Tests/RealityTests/TRM_Realtiy_Tests.cs.
    /// </summary>
    public static class PhotonTransportModel
    {
        public struct PhotonState
        {
            public double x;
            public double y;
            public double vx;
            public double vy;
        }

        public struct PhotonMemoryState
        {
            public double x;
            public double y;
            public double vx;
            public double vy;

            /// <summary>
            /// Transport memory:
            /// memory = integral phi^m * |dmu/dt| dt
            /// </summary>
            public double memory;

            /// <summary>
            /// Optical travel time accumulation:
            /// TimeAccum = integral n_eff ds
            /// </summary>
            public double TimeAccum;

            // DEBUG (optional!)
            public double LastR;
            public double LastLocalContribution;

        }

        public sealed class Parameters
        {
            /// <summary>
            /// Local nonlinear photon fingerprint coefficient from TRM43.
            /// </summary>
            public double A { get; init; } = -0.1701452243330672;

            /// <summary>
            /// Local nonlinear photon fingerprint coefficient from TRM45.
            /// </summary>
            public double B { get; init; } = -8.484408441898648;

            /// <summary>
            /// Global weighted-memory coupling from TRM64.
            /// </summary>
            public double Lambda { get; init; } = 30.79445857638716;

            /// <summary>
            /// Best weighted memory exponent from TRM62.
            /// </summary>
            public double MemoryPower { get; init; } = 2.0;

            /// <summary>
            /// True: n_eff = kBase + lambda * memory, false: n_eff = kBase.
            /// </summary>
            public bool UseMemory { get; init; } = true;
            public double RadialPower { get; init; } = 0.0;
            public double MuPower { get; init; } = 1.0;
            public bool UseGeometricMemory { get; init; } = false;
            public bool UseLocalMemory { get; init; } = false;
            public double LambdaTime { get; init; } = 1.0;     // φ-Term
            public double LambdaSpace { get; init; } = 30.0;   // φ² μ̇-Term
            public double EulerBridgeScale { get; init; } = 0.85;
        }

        public struct TimeComparison
        {
            public double Base;
            public double Full;
        }

        public struct Diagnostics
        {
            public double Deflection;
            public double FinalMemory;
            public double MinR;
            public double MaxPhi;
            public double MaxAbsMu;
            public double AvgAbsMu;
            public double MaxAbsDmuDt;
            public double AvgAbsDmuDt;
            public double TotalTime;
            public double FlatTime;
            public double ShapiroDelay;
            public double TotalTime_Base;
            public double TotalTime_Full;
            public double ShapiroDelay_Base;
            public double ShapiroDelay_Full;
            public TimeComparison TotalTimeComparison;
            public TimeComparison ShapiroDelayComparison;
        }

        public static double Phi(double G, double M, double c, double r)
        {
            return G * M / (c * c * r);
        }

        public static double KBase(double phi, double a, double b)
        {
            return 2.0 + 2.0 * a * phi + 3.0 * b * phi * phi;
        }

        public static double ComputeEffectiveIndex(
            double phi,
            double memory,
            Parameters parameters)
        {
            double kBase = KBase(phi, parameters.A, parameters.B);

            if (!parameters.UseMemory)
                return kBase;

            

            return kBase + parameters.Lambda * memory;
        }

        public static double ComputeMu(PhotonMemoryState state)
        {
            double r = Math.Sqrt(state.x * state.x + state.y * state.y);

            double ex = state.x / r;
            double ey = state.y / r;

            double v = Math.Sqrt(state.vx * state.vx + state.vy * state.vy);

            double vxHat = state.vx / v;
            double vyHat = state.vy / v;

            return vxHat * ex + vyHat * ey;
        }

        public static double ComputeAbsDmuDtBase(
            PhotonMemoryState state,
            double G,
            double M,
            double c,
            Parameters parameters)
        {
            double r = Math.Sqrt(state.x * state.x + state.y * state.y);

            double ex = state.x / r;
            double ey = state.y / r;

            double phi = Phi(G, M, c, r);
            double kBase = KBase(phi, parameters.A, parameters.B);

            double v = Math.Sqrt(state.vx * state.vx + state.vy * state.vy);

            double vxHat = state.vx / v;
            double vyHat = state.vy / v;

            double arBase = -kBase * G * M / (r * r);

            double axBase = arBase * ex;
            double ayBase = arBase * ey;

            double v2 = state.vx * state.vx + state.vy * state.vy;
            double dotBase = axBase * state.vx + ayBase * state.vy;

            double axBaseProj = axBase - dotBase / v2 * state.vx;
            double ayBaseProj = ayBase - dotBase / v2 * state.vy;

            // d(v_hat)/dt = a_perp / |v|
            double dvhxDt = axBaseProj / v;
            double dvhyDt = ayBaseProj / v;

            // d(e_r)/dt = (v - (v·e_r)e_r) / r
            double vRad = state.vx * ex + state.vy * ey;

            double derxDt = (state.vx - vRad * ex) / r;
            double deryDt = (state.vy - vRad * ey) / r;

            // dmu/dt = d(v_hat)/dt · e_r + v_hat · d(e_r)/dt
            double dmuDt =
                dvhxDt * ex + dvhyDt * ey
                + vxHat * derxDt + vyHat * deryDt;

            return Math.Abs(dmuDt);
        }

        public static PhotonMemoryState Derivatives(
            PhotonMemoryState state,
            double G,
            double M,
            double c,
            Parameters parameters)
        {
            double r = Math.Sqrt(state.x * state.x + state.y * state.y);
            double ex = state.x / r;
            double ey = state.y / r;

            double phi = Phi(G, M, c, r);

            double v = Math.Sqrt(state.vx * state.vx + state.vy * state.vy);

            double absDmuDt = ComputeAbsDmuDtBase(state, G, M, c, parameters);

            // ✅ Deflection term
            double localMemory =
                Math.Pow(phi, 2.0) *
                absDmuDt;

            // ✅ Unified propagation model
            double nEff =
                2.0
                + parameters.LambdaTime * phi
                + parameters.LambdaSpace * localMemory;

            // ✅ Time evolution (NO amplification!)
            double timeAccumDerivative = (nEff - 2.0) * v;

            // ✅ Acceleration
            double ar = -nEff * G * M / (r * r);

            double ax = ar * ex;
            double ay = ar * ey;

            // Photon constraint
            double v2 = state.vx * state.vx + state.vy * state.vy;
            double dot = ax * state.vx + ay * state.vy;

            double axProj = ax - dot / v2 * state.vx;
            double ayProj = ay - dot / v2 * state.vy;

            return new PhotonMemoryState
            {
                x = state.vx,
                y = state.vy,
                vx = axProj,
                vy = ayProj,

                memory = 0.0, // ✅ no global memory anymore
                TimeAccum = timeAccumDerivative,

                LastR = r,
                LastLocalContribution = (nEff - 2.0) * v
            };
        }

        public static void RK4Step(
            ref PhotonMemoryState state,
            double dt,
            double G,
            double M,
            double c,
            Parameters parameters)
        {
            PhotonMemoryState k1 = Derivatives(state, G, M, c, parameters);

            PhotonMemoryState s2 = new PhotonMemoryState
            {
                x = state.x + 0.5 * dt * k1.x,
                y = state.y + 0.5 * dt * k1.y,
                vx = state.vx + 0.5 * dt * k1.vx,
                vy = state.vy + 0.5 * dt * k1.vy,
                memory = state.memory + 0.5 * dt * k1.memory,
                TimeAccum = state.TimeAccum + 0.5 * dt * k1.TimeAccum
            };

            PhotonMemoryState k2 = Derivatives(s2, G, M, c, parameters);

            PhotonMemoryState s3 = new PhotonMemoryState
            {
                x = state.x + 0.5 * dt * k2.x,
                y = state.y + 0.5 * dt * k2.y,
                vx = state.vx + 0.5 * dt * k2.vx,
                vy = state.vy + 0.5 * dt * k2.vy,
                memory = state.memory + 0.5 * dt * k2.memory,
                TimeAccum = state.TimeAccum + 0.5 * dt * k2.TimeAccum
            };

            PhotonMemoryState k3 = Derivatives(s3, G, M, c, parameters);

            PhotonMemoryState s4 = new PhotonMemoryState
            {
                x = state.x + dt * k3.x,
                y = state.y + dt * k3.y,
                vx = state.vx + dt * k3.vx,
                vy = state.vy + dt * k3.vy,
                memory = state.memory + dt * k3.memory,
                TimeAccum = state.TimeAccum + dt * k3.TimeAccum
            };

            PhotonMemoryState k4 = Derivatives(s4, G, M, c, parameters);

            state.x += dt / 6.0 * (k1.x + 2.0 * k2.x + 2.0 * k3.x + k4.x);
            state.y += dt / 6.0 * (k1.y + 2.0 * k2.y + 2.0 * k3.y + k4.y);
            state.vx += dt / 6.0 * (k1.vx + 2.0 * k2.vx + 2.0 * k3.vx + k4.vx);
            state.vy += dt / 6.0 * (k1.vy + 2.0 * k2.vy + 2.0 * k3.vy + k4.vy);
            state.memory += dt / 6.0 * (k1.memory + 2.0 * k2.memory + 2.0 * k3.memory + k4.memory);
            state.TimeAccum += dt / 6.0 * (k1.TimeAccum + 2.0 * k2.TimeAccum + 2.0 * k3.TimeAccum + k4.TimeAccum);

            // Enforce |v| = c.
            double v = Math.Sqrt(state.vx * state.vx + state.vy * state.vy);
            state.vx = state.vx / v * c;
            state.vy = state.vy / v * c;
        }

        private static double ComputeEffectiveIndexLocal(
            PhotonMemoryState state,
            double G,
            double M,
            double c,
            Parameters parameters)
        {
            double r = Math.Sqrt(state.x * state.x + state.y * state.y);
            double phi = Phi(G, M, c, r);
            double absDmuDt = ComputeAbsDmuDtBase(state, G, M, c, parameters);
            double localMemory = phi * phi * absDmuDt;

            return 2.0
                + parameters.LambdaTime * phi
                + parameters.LambdaSpace * localMemory;
        }

        private static (double dNx, double dNy) ComputeEffectiveIndexGradient(
            PhotonMemoryState state,
            double G,
            double M,
            double c,
            Parameters parameters)
        {
            double r = Math.Sqrt(state.x * state.x + state.y * state.y);
            double h = Math.Max(1e-6, 1e-5 * Math.Max(1.0, r));

            PhotonMemoryState sxp = state;
            PhotonMemoryState sxm = state;
            PhotonMemoryState syp = state;
            PhotonMemoryState sym = state;

            sxp.x += h;
            sxm.x -= h;
            syp.y += h;
            sym.y -= h;

            double nXp = ComputeEffectiveIndexLocal(sxp, G, M, c, parameters);
            double nXm = ComputeEffectiveIndexLocal(sxm, G, M, c, parameters);
            double nYp = ComputeEffectiveIndexLocal(syp, G, M, c, parameters);
            double nYm = ComputeEffectiveIndexLocal(sym, G, M, c, parameters);

            double dNx = (nXp - nXm) / (2.0 * h);
            double dNy = (nYp - nYm) / (2.0 * h);

            return (dNx, dNy);
        }

        /// <summary>
        /// Euler-Lagrange/Fermat ray derivative for isotropic effective transport index.
        /// Implements d/ds(n * t_hat) = grad(n) in projected acceleration form.
        /// </summary>
        public static PhotonMemoryState DerivativesEulerLagrange(
            PhotonMemoryState state,
            double G,
            double M,
            double c,
            Parameters parameters)
        {
            double v = Math.Sqrt(state.vx * state.vx + state.vy * state.vy);
            double vHatX = state.vx / v;
            double vHatY = state.vy / v;

            double nEff = ComputeEffectiveIndexLocal(state, G, M, c, parameters);
            var (dNx, dNy) = ComputeEffectiveIndexGradient(state, G, M, c, parameters);

            double gradDotT = dNx * vHatX + dNy * vHatY;

            // In this codebase transport dynamics are evolved with |v| constrained to c.
            // EulerBridgeScale makes this executable EL/Fermat path comparable to the
            // established transport RK4 branch in weak-field deflection diagnostics.
            double accelScale = c * c * nEff * parameters.EulerBridgeScale;
            double ax = accelScale * (dNx - gradDotT * vHatX);
            double ay = accelScale * (dNy - gradDotT * vHatY);

            double timeAccumDerivative = (nEff - 2.0) * v;

            return new PhotonMemoryState
            {
                x = state.vx,
                y = state.vy,
                vx = ax,
                vy = ay,
                memory = 0.0,
                TimeAccum = timeAccumDerivative,
                LastR = Math.Sqrt(state.x * state.x + state.y * state.y),
                LastLocalContribution = timeAccumDerivative
            };
        }

        public static void RK4StepEulerLagrange(
            ref PhotonMemoryState state,
            double dt,
            double G,
            double M,
            double c,
            Parameters parameters)
        {
            PhotonMemoryState k1 = DerivativesEulerLagrange(state, G, M, c, parameters);

            PhotonMemoryState s2 = new PhotonMemoryState
            {
                x = state.x + 0.5 * dt * k1.x,
                y = state.y + 0.5 * dt * k1.y,
                vx = state.vx + 0.5 * dt * k1.vx,
                vy = state.vy + 0.5 * dt * k1.vy,
                memory = state.memory + 0.5 * dt * k1.memory,
                TimeAccum = state.TimeAccum + 0.5 * dt * k1.TimeAccum
            };

            PhotonMemoryState k2 = DerivativesEulerLagrange(s2, G, M, c, parameters);

            PhotonMemoryState s3 = new PhotonMemoryState
            {
                x = state.x + 0.5 * dt * k2.x,
                y = state.y + 0.5 * dt * k2.y,
                vx = state.vx + 0.5 * dt * k2.vx,
                vy = state.vy + 0.5 * dt * k2.vy,
                memory = state.memory + 0.5 * dt * k2.memory,
                TimeAccum = state.TimeAccum + 0.5 * dt * k2.TimeAccum
            };

            PhotonMemoryState k3 = DerivativesEulerLagrange(s3, G, M, c, parameters);

            PhotonMemoryState s4 = new PhotonMemoryState
            {
                x = state.x + dt * k3.x,
                y = state.y + dt * k3.y,
                vx = state.vx + dt * k3.vx,
                vy = state.vy + dt * k3.vy,
                memory = state.memory + dt * k3.memory,
                TimeAccum = state.TimeAccum + dt * k3.TimeAccum
            };

            PhotonMemoryState k4 = DerivativesEulerLagrange(s4, G, M, c, parameters);

            state.x += dt / 6.0 * (k1.x + 2.0 * k2.x + 2.0 * k3.x + k4.x);
            state.y += dt / 6.0 * (k1.y + 2.0 * k2.y + 2.0 * k3.y + k4.y);
            state.vx += dt / 6.0 * (k1.vx + 2.0 * k2.vx + 2.0 * k3.vx + k4.vx);
            state.vy += dt / 6.0 * (k1.vy + 2.0 * k2.vy + 2.0 * k3.vy + k4.vy);
            state.memory += dt / 6.0 * (k1.memory + 2.0 * k2.memory + 2.0 * k3.memory + k4.memory);
            state.TimeAccum += dt / 6.0 * (k1.TimeAccum + 2.0 * k2.TimeAccum + 2.0 * k3.TimeAccum + k4.TimeAccum);

            double vNorm = Math.Sqrt(state.vx * state.vx + state.vy * state.vy);
            state.vx = state.vx / vNorm * c;
            state.vy = state.vy / vNorm * c;
        }

        /// <summary>
        /// Computes deflection using the explicit Euler-Lagrange/Fermat derivative path.
        /// This is intended as the executable derivation track to compare against the legacy RK4 transport path.
        /// </summary>
        public static double ComputeDeflectionEulerLagrange(
            double epsilon,
            double G,
            double c,
            double bImpact,
            double dt,
            Parameters parameters)
        {
            double M = epsilon * c * c * bImpact / G;
            double X = 100.0;

            PhotonMemoryState state = new PhotonMemoryState
            {
                x = -X,
                y = bImpact,
                vx = c,
                vy = 0.0,
                memory = 0.0,
                TimeAccum = 0.0
            };

            double initialAngle = Math.Atan2(state.vy, state.vx);
            int steps = (int)(2.0 * X / (c * dt));

            for (int i = 0; i < steps; i++)
            {
                RK4StepEulerLagrange(ref state, dt, G, M, c, parameters);
            }

            double finalAngle = Math.Atan2(state.vy, state.vx);

            double deflection = finalAngle - initialAngle;
            deflection = Math.IEEERemainder(deflection, 2.0 * Math.PI);

            return Math.Abs(deflection);
        }

        /// <summary>
        /// Computes absolute photon deflection for a normalized impact-parameter setup.
        /// Status: tested (deflection scaling checks), diagnostic (used in parameter studies), calibrated (depends on active parameter set).
        /// Related tests: PhotonTransportModel_FixationTests TRM81 and reality-suite photon blocks.
        /// Relevant docs: docs/review/TRM_Real_Physics_Test_Coverage.md.
        /// </summary>
        public static double ComputeDeflection(
            double epsilon,
            double G,
            double c,
            double bImpact,
            double dt,
            Parameters parameters)
        {
            double M = epsilon * c * c * bImpact / G;

            double X = 100.0;

            PhotonMemoryState state = new PhotonMemoryState
            {
                x = -X,
                y = bImpact,
                vx = c,
                vy = 0.0,
                memory = 0.0,
                TimeAccum = 0.0
            };

            double initialAngle = Math.Atan2(state.vy, state.vx);
            int steps = (int)(2.0 * X / (c * dt));

            for (int i = 0; i < steps; i++)
            {
                RK4Step(ref state, dt, G, M, c, parameters);
            }

            double finalAngle = Math.Atan2(state.vy, state.vx);

            double deflection = finalAngle - initialAngle;
            deflection = Math.IEEERemainder(deflection, 2.0 * Math.PI);

            return Math.Abs(deflection);
        }

        private struct DiagnosticRunResult
        {
            public double Deflection;
            public double FinalMemory;
            public double MinR;
            public double MaxPhi;
            public double MaxAbsMu;
            public double AvgAbsMu;
            public double MaxAbsDmuDt;
            public double AvgAbsDmuDt;
            public double TotalTime;
            public double FlatTime;
            public double ShapiroDelay;
        }

        private static DiagnosticRunResult RunDiagnosticsSingleMode(
            double epsilon,
            double G,
            double c,
            double bImpact,
            double dt,
            Parameters parameters,
            Action<double, double>? logger = null)
        {
            //double M = epsilon * c * c * bImpact / G;
            double M = epsilon * c * c / G;
            double X = 100.0 * bImpact;

            PhotonMemoryState state = new PhotonMemoryState
            {
                x = -X,
                y = bImpact,
                vx = c,
                vy = 0.0,
                memory = 0.0,
                TimeAccum = 0.0
            };

            double initialAngle = Math.Atan2(state.vy, state.vx);
            int steps = (int)(2.0 * X / (c * dt));

            double minR = double.MaxValue;
            double maxPhi = 0.0;
            double maxAbsMu = 0.0;
            double sumAbsMu = 0.0;
            double maxAbsDmuDt = 0.0;
            double sumAbsDmuDt = 0.0;
            double flatTime = 0.0;

            // ✅ NEU: echter geometrischer Shapiro-Term
            double shapiroAccum = 0.0;

            for (int i = 0; i < steps; i++)
            {
                double speed = Math.Sqrt(state.vx * state.vx + state.vy * state.vy);
                flatTime += speed * dt;

                double r = Math.Sqrt(state.x * state.x + state.y * state.y);

                double phi = G * M / (c * c * r);
                //double phi = Phi(G, M, c, r);

                if (r < minR)
                    minR = r;

                if (phi > maxPhi)
                    maxPhi = phi;

                double absMu = Math.Abs(ComputeMu(state));
                double absDmuDt = ComputeAbsDmuDtBase(state, G, M, c, parameters);

                if (absMu > maxAbsMu)
                    maxAbsMu = absMu;

                if (absDmuDt > maxAbsDmuDt)
                    maxAbsDmuDt = absDmuDt;

                sumAbsMu += absMu;
                sumAbsDmuDt += absDmuDt;


                double ds = speed * dt;
                

                // ✅ DER richtige Shapiro-Term
                shapiroAccum += phi * ds;

                



                // optional Logging
                logger?.Invoke(r, 1.0 / r);

                RK4Step(ref state, dt, G, M, c, parameters);
            }

            double finalAngle = Math.Atan2(state.vy, state.vx);

            double deflection = finalAngle - initialAngle;
            deflection = Math.IEEERemainder(deflection, 2.0 * Math.PI);

            double totalTime = state.TimeAccum;

            // ✅ FINAL: Shapiro kommt NICHT aus TimeAccum!
            //double shapiroDelay = parameters.Lambda * shapiroAccum;
            double shapiroDelay = shapiroAccum;

            return new DiagnosticRunResult

            {
                Deflection = Math.Abs(deflection),
                FinalMemory = state.memory,
                MinR = minR,
                MaxPhi = maxPhi,
                MaxAbsMu = maxAbsMu,
                AvgAbsMu = sumAbsMu / steps,
                MaxAbsDmuDt = maxAbsDmuDt,
                AvgAbsDmuDt = sumAbsDmuDt / steps,
                TotalTime = totalTime,
                FlatTime = flatTime,
                ShapiroDelay = shapiroDelay
            };
        }



        /// <summary>
        /// Computes deflection plus diagnostic channels (memory/time/shapiro comparisons) for baseline vs full mode.
        /// Status: tested (regression invariants), diagnostic (primary analysis output), calibrated (parameter-sensitive), limitation (implementation-centric shapiro diagnostics).
        /// Related tests: TRM.Tests/RealityTests/PhotonTransportModel_FixationTests.cs (TRM82/TRM83) and TRM67-TRM77 in TRM_Realtiy_Tests.
        /// Relevant docs: docs/review/TRM_Service_Test_Consolidation.md, docs/review/TRM_Real_Physics_Test_Coverage.md.
        /// </summary>
        public static Diagnostics ComputeDeflectionWithDiagnostics(
            double epsilon,
            double G,
            double c,
            double bImpact,
            double dt,
            Parameters parameters, 
            Action<double, double>? logger = null)
        {
            Parameters baseParameters = new Parameters
            {
                A = parameters.A,
                B = parameters.B,
                Lambda = parameters.Lambda,
                MemoryPower = parameters.MemoryPower,
                UseMemory = false,
                RadialPower = parameters.RadialPower
            };

            Parameters fullParameters = new Parameters
            {
                A = parameters.A,
                B = parameters.B,
                Lambda = parameters.Lambda,
                MemoryPower = parameters.MemoryPower,
                UseMemory = true,
                RadialPower = parameters.RadialPower
            };

            DiagnosticRunResult baseRun = RunDiagnosticsSingleMode(
                epsilon,
                G,
                c,
                bImpact,
                dt,
                baseParameters,
                null);

            DiagnosticRunResult fullRun = RunDiagnosticsSingleMode(
                epsilon,
                G,
                c,
                bImpact,
                dt,
                fullParameters,
                logger);

            return new Diagnostics
            {
                Deflection = fullRun.Deflection,
                FinalMemory = fullRun.FinalMemory,
                MinR = fullRun.MinR,
                MaxPhi = fullRun.MaxPhi,
                MaxAbsMu = fullRun.MaxAbsMu,
                AvgAbsMu = fullRun.AvgAbsMu,
                MaxAbsDmuDt = fullRun.MaxAbsDmuDt,
                AvgAbsDmuDt = fullRun.AvgAbsDmuDt,
                TotalTime = fullRun.TotalTime,
                FlatTime = fullRun.FlatTime,
                ShapiroDelay = fullRun.ShapiroDelay,
                TotalTime_Base = baseRun.TotalTime,
                TotalTime_Full = fullRun.TotalTime,
                ShapiroDelay_Base = baseRun.ShapiroDelay,
                ShapiroDelay_Full = fullRun.ShapiroDelay,
                TotalTimeComparison = new TimeComparison
                {
                    Base = baseRun.TotalTime,
                    Full = fullRun.TotalTime
                },
                ShapiroDelayComparison = new TimeComparison
                {
                    Base = baseRun.ShapiroDelay,
                    Full = fullRun.ShapiroDelay
                }
            };
        }
    }
}
