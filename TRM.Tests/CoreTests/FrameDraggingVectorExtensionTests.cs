using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.CoreTests;

/// <summary>
/// Structural guard tests for a candidate TRM vector frame-dragging sector.
/// Uses normalized units (G=c=1, k_T=1) and synthetic rotating sources only.
/// No quantitative GR fitting is performed here.
/// </summary>
public class FrameDraggingVectorExtensionTests
{
    private readonly ITestOutputHelper _output;

    public FrameDraggingVectorExtensionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void FD01_RotatingSource_Should_Generate_Nonzero_VectorPotential()
    {
        var rotating = CreateRigidRotatingSource(angularSpeed: 1.0);
        var staticSource = CreateRigidRotatingSource(angularSpeed: 0.0);

        var point = new Vec3(2.8, 0.2, 0.1);
        double aRot = ComputeVectorPotential(point, rotating).Norm();
        double aStatic = ComputeVectorPotential(point, staticSource).Norm();

        _output.WriteLine($"[FD01] |A_rot|    = {aRot:E6}");
        _output.WriteLine($"[FD01] |A_static| = {aStatic:E6}");

        Assert.True(aRot > 1e-6, $"Rotating source should produce nonzero |A_T|, got {aRot:E6}");
        Assert.True(aRot > 1000.0 * (aStatic + 1e-18), "Rotating-source vector potential should dominate static baseline.");
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void FD02_NonRotatingSource_Should_Generate_Zero_FrameDraggingField()
    {
        var staticSource = CreateRigidRotatingSource(angularSpeed: 0.0);

        var probes = new[]
        {
            new Vec3(2.0, 0.0, 0.0),
            new Vec3(2.5, 0.4, 0.1),
            new Vec3(3.0, -0.3, 0.2)
        };

        foreach (var p in probes)
        {
            Vec3 b = ComputeCurlField(p, staticSource, step: 0.12);
            double norm = b.Norm();
            _output.WriteLine($"[FD02] p=({p.X:F2},{p.Y:F2},{p.Z:F2}) |B_static|={norm:E6}");
            Assert.True(norm < 1e-12, $"Non-rotating source should give near-zero B_T, got {norm:E6}");
        }
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void FD03_FrameDraggingField_Should_FlipSign_When_SpinReverses()
    {
        var spinPlus = CreateRigidRotatingSource(angularSpeed: +1.0);
        var spinMinus = CreateRigidRotatingSource(angularSpeed: -1.0);
        var point = new Vec3(3.0, 0.0, 0.0);

        Vec3 bPlus = ComputeCurlField(point, spinPlus, step: 0.12);
        Vec3 bMinus = ComputeCurlField(point, spinMinus, step: 0.12);

        _output.WriteLine($"[FD03] B_plus  = ({bPlus.X:E6}, {bPlus.Y:E6}, {bPlus.Z:E6})");
        _output.WriteLine($"[FD03] B_minus = ({bMinus.X:E6}, {bMinus.Y:E6}, {bMinus.Z:E6})");

        Assert.True(bPlus.Z * bMinus.Z < 0.0, "Spin reversal should flip B_T orientation (z-sign).");

        double magRatio = bPlus.Norm() / Math.Max(bMinus.Norm(), 1e-18);
        Assert.InRange(magRatio, 0.8, 1.25);
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void FD04_FrameDraggingField_Should_Decay_With_Radius()
    {
        var rotating = CreateRigidRotatingSource(angularSpeed: 1.0);

        double b2 = ComputeCurlField(new Vec3(2.0, 0.0, 0.0), rotating, step: 0.12).Norm();
        double b3 = ComputeCurlField(new Vec3(3.0, 0.0, 0.0), rotating, step: 0.12).Norm();
        double b4 = ComputeCurlField(new Vec3(4.0, 0.0, 0.0), rotating, step: 0.12).Norm();

        _output.WriteLine($"[FD04] |B|(r=2) = {b2:E6}");
        _output.WriteLine($"[FD04] |B|(r=3) = {b3:E6}");
        _output.WriteLine($"[FD04] |B|(r=4) = {b4:E6}");

        Assert.True(b2 > b3 && b3 > b4, "Frame-dragging candidate field should decay with radius in far zone.");
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void FD05_GyroPrecession_Should_Scale_With_SourceAngularMomentum()
    {
        var slow = CreateRigidRotatingSource(angularSpeed: 0.5);
        var mid = CreateRigidRotatingSource(angularSpeed: 1.0);
        var fast = CreateRigidRotatingSource(angularSpeed: 1.5);
        var probe = new Vec3(3.2, 0.2, 0.0);

        double lSlow = ComputeAngularMomentumNorm(slow);
        double lMid = ComputeAngularMomentumNorm(mid);
        double lFast = ComputeAngularMomentumNorm(fast);

        // Precession proxy convention: Omega_FD ∝ |B_T|
        double oSlow = ComputeCurlField(probe, slow, step: 0.12).Norm();
        double oMid = ComputeCurlField(probe, mid, step: 0.12).Norm();
        double oFast = ComputeCurlField(probe, fast, step: 0.12).Norm();

        _output.WriteLine($"[FD05] L: slow={lSlow:E6}, mid={lMid:E6}, fast={lFast:E6}");
        _output.WriteLine($"[FD05] OmegaProxy: slow={oSlow:E6}, mid={oMid:E6}, fast={oFast:E6}");

        Assert.True(lSlow < lMid && lMid < lFast, "Source angular momentum should scale with angular speed.");
        Assert.True(oSlow < oMid && oMid < oFast, "Precession proxy should scale with source angular momentum.");
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void FD06_VectorPotential_Should_Be_Dimensionally_Consistent()
    {
        var baseSource = CreateRigidRotatingSource(angularSpeed: 1.0);
        var scaledSource = ScaleSourceCurrent(baseSource, currentScale: 2.75);
        var probe = new Vec3(3.1, 0.3, 0.2);

        double aBase = ComputeVectorPotential(probe, baseSource).Norm();
        double aScaled = ComputeVectorPotential(probe, scaledSource).Norm();
        double ratio = aScaled / Math.Max(aBase, 1e-18);

        _output.WriteLine($"[FD06] |A_base|={aBase:E6}, |A_scaled|={aScaled:E6}, ratio={ratio:F6}");

        Assert.InRange(ratio, 2.72, 2.78);
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void FD07_FrameDraggingField_Should_Scale_With_CouplingConstant()
    {
        var source = CreateRigidRotatingSource(angularSpeed: 1.0);
        var probe = new Vec3(3.0, 0.0, 0.0);

        double b05 = ComputeCurlField(probe, source, step: 0.12, coupling: 0.5).Norm();
        double b10 = ComputeCurlField(probe, source, step: 0.12, coupling: 1.0).Norm();
        double b20 = ComputeCurlField(probe, source, step: 0.12, coupling: 2.0).Norm();

        double r1 = b10 / Math.Max(b05, 1e-18);
        double r2 = b20 / Math.Max(b10, 1e-18);

        _output.WriteLine($"[FD07] |B|(k=0.5)={b05:E6}, |B|(k=1.0)={b10:E6}, |B|(k=2.0)={b20:E6}");
        _output.WriteLine($"[FD07] ratios: r1={r1:F6}, r2={r2:F6}");

        Assert.InRange(r1, 1.95, 2.05);
        Assert.InRange(r2, 1.95, 2.05);
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void FD08_PrecessionProxy_Should_Be_Zero_For_RadialSpinlessMotion()
    {
        var staticSource = CreateRigidRotatingSource(angularSpeed: 0.0);
        var probe = new Vec3(2.6, -0.4, 0.3);
        var radialVelocity = probe.Normalized();
        Vec3 bField = ComputeCurlField(probe, staticSource, step: 0.12);
        double omegaProxy = ComputePrecessionProxy(radialVelocity, bField);

        _output.WriteLine($"[FD08] |B_static|={bField.Norm():E6}, OmegaProxy(radial,spinless)={omegaProxy:E6}");

        Assert.True(bField.Norm() < 1e-12, "Spinless/non-rotating setup should give near-zero B_T.");
        Assert.True(omegaProxy < 1e-12, "Radial spinless precession proxy should be near zero.");
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void FD09_FarField_Should_Approximate_AngularMomentum_DipoleShape()
    {
        var source = CreateRigidRotatingSource(angularSpeed: 1.0);

        var rs = new[] { 4.0, 5.0, 6.0 };
        var invariants = rs
            .Select(r =>
            {
                Vec3 b = ComputeCurlField(new Vec3(r, 0.0, 0.0), source, step: 0.15);
                double inv = Math.Abs(b.Z) * r * r * r; // dipole-shape proxy on equatorial line
                _output.WriteLine($"[FD09] r={r:F1}, Bz={b.Z:E6}, |Bz|*r^3={inv:E6}");
                return inv;
            })
            .ToList();

        double mean = invariants.Average();
        double spread = (invariants.Max() - invariants.Min()) / Math.Max(mean, 1e-18);
        _output.WriteLine($"[FD09] dipole-invariant mean={mean:E6}, relativeSpread={spread:F6}");

        Assert.True(spread < 0.30, "Far-field dipole-shape proxy should be approximately radius-invariant.");
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void FD10_LenseThirring_WeakFieldScaling_Should_Match_GRShape()
    {
        // Shape-only guard (no absolute-amplitude fit):
        // Omega_FD ∝ J / r^3 with spin-sign flip and zero baseline at no rotation.

        var slow = CreateRigidRotatingSource(angularSpeed: 0.5);
        var mid = CreateRigidRotatingSource(angularSpeed: 1.0);
        var fast = CreateRigidRotatingSource(angularSpeed: 1.5);
        var spinMinus = CreateRigidRotatingSource(angularSpeed: -1.0);
        var staticSource = CreateRigidRotatingSource(angularSpeed: 0.0);

        // 1) Linear in angular momentum J (shape).
        var fixedProbe = new Vec3(4.5, 0.0, 0.0);
        double jSlow = ComputeAngularMomentumNorm(slow);
        double jMid = ComputeAngularMomentumNorm(mid);
        double jFast = ComputeAngularMomentumNorm(fast);

        double oSlow = Math.Abs(ComputeCurlField(fixedProbe, slow, step: 0.15).Z);
        double oMid = Math.Abs(ComputeCurlField(fixedProbe, mid, step: 0.15).Z);
        double oFast = Math.Abs(ComputeCurlField(fixedProbe, fast, step: 0.15).Z);

        double kSlow = oSlow / Math.Max(jSlow, 1e-18);
        double kMid = oMid / Math.Max(jMid, 1e-18);
        double kFast = oFast / Math.Max(jFast, 1e-18);
        double kMean = (kSlow + kMid + kFast) / 3.0;
        double kSpread = new[] { kSlow, kMid, kFast }.Select(k => Math.Abs(k - kMean) / Math.Max(kMean, 1e-18)).Max();

        _output.WriteLine($"[FD10] J-linearity: J=({jSlow:E6},{jMid:E6},{jFast:E6}), Omega=({oSlow:E6},{oMid:E6},{oFast:E6})");
        _output.WriteLine($"[FD10] J-linearity: K=Omega/J -> ({kSlow:E6},{kMid:E6},{kFast:E6}), relSpread={kSpread:F6}");

        Assert.True(kSpread < 0.10, "Weak-field shape should be approximately linear in angular momentum (Omega/J ~ const).");

        // 2) Radial decay ~ r^-3 (shape).
        var radii = new[] { 4.0, 5.0, 6.0 };
        var r3Invariants = radii
            .Select(r =>
            {
                double omega = Math.Abs(ComputeCurlField(new Vec3(r, 0.0, 0.0), mid, step: 0.15).Z);
                double inv = omega * r * r * r;
                _output.WriteLine($"[FD10] r^-3: r={r:F1}, Omega={omega:E6}, Omega*r^3={inv:E6}");
                return inv;
            })
            .ToList();

        double invMean = r3Invariants.Average();
        double invSpread = (r3Invariants.Max() - r3Invariants.Min()) / Math.Max(invMean, 1e-18);
        _output.WriteLine($"[FD10] r^-3: invariantMean={invMean:E6}, relSpread={invSpread:F6}");

        Assert.True(invSpread < 0.15, "Weak-field shape should follow Omega ∝ r^-3 in far field.");

        // 3) Spin reversal changes sign.
        double bPlusZ = ComputeCurlField(fixedProbe, mid, step: 0.15).Z;
        double bMinusZ = ComputeCurlField(fixedProbe, spinMinus, step: 0.15).Z;
        _output.WriteLine($"[FD10] spin-sign: Bz_plus={bPlusZ:E6}, Bz_minus={bMinusZ:E6}");

        Assert.True(bPlusZ * bMinusZ < 0.0, "Spin reversal should flip sign of frame-dragging orientation.");

        // 4) No rotation gives null.
        Vec3 bStatic = ComputeCurlField(fixedProbe, staticSource, step: 0.15);
        _output.WriteLine($"[FD10] static-null: |B_static|={bStatic.Norm():E6}");
        Assert.True(bStatic.Norm() < 1e-12, "Non-rotating source should produce near-zero frame-dragging field.");
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void FD11_VectorCoupling_Should_Normalize_To_GR_LenseThirring_WeakFieldWindow()
    {
        // Effective-coupling calibration only (not a fundamental derivation):
        // fit one global k_T against a weak-field LT reference shape Ω_ref = C_LT * J / r^3.
        const double cLt = 1.0; // normalized weak-field reference coefficient

        var trainSamples = BuildCouplingSamples(
            angularSpeeds: new[] { 0.8, 1.0, 1.2 },
            radii: new[] { 4.0, 5.0, 6.0 },
            step: 0.15,
            cLt: cLt);

        double kEff = FitEffectiveCoupling(trainSamples);
        _output.WriteLine($"[FD11] fitted k_T(effective) = {kEff:E6}");
        Assert.True(double.IsFinite(kEff) && kEff > 0.0, "Effective coupling k_T must be finite and positive.");

        var validation = BuildCouplingSamples(
            angularSpeeds: new[] { 0.6, 1.4 },
            radii: new[] { 4.5, 5.5, 6.5 },
            step: 0.15,
            cLt: cLt);

        var ratios = validation
            .Select(s =>
            {
                double omegaCal = kEff * s.OmegaRaw;
                double ratio = omegaCal / Math.Max(s.OmegaRef, 1e-18);
                _output.WriteLine($"[FD11] val: omega={s.AngularSpeed:F2}, r={s.Radius:F1}, Ω_cal={omegaCal:E6}, Ω_ref={s.OmegaRef:E6}, ratio={ratio:F6}");
                return ratio;
            })
            .ToList();

        double meanRatio = ratios.Average();
        double spread = ratios.Max() - ratios.Min();

        _output.WriteLine($"[FD11] validation meanRatio={meanRatio:F6}, spread={spread:F6}");

        // Weak-field window check for a single global effective coupling.
        Assert.InRange(meanRatio, 0.90, 1.10);
        Assert.True(spread < 0.15, "Single effective k_T should remain inside a tight weak-field normalization window.");
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void FD12_VectorCoupling_Should_Generalize_Under_Holdout_Normalization()
    {
        // No-overfit guard:
        // Fit one global effective k_T on training (J,r) points, then validate on disjoint holdout points.
        const double cLt = 1.0;
        var all = BuildCouplingSamples(
            angularSpeeds: new[] { 0.55, 0.75, 0.95, 1.15, 1.35, 1.55 },
            radii: new[] { 3.8, 4.4, 5.0, 5.6, 6.2, 6.8 },
            step: 0.15,
            cLt: cLt);

        var indexed = all.Select((s, idx) => (Sample: s, Index: idx)).ToList();
        var train = indexed.Where(x => x.Index % 3 != 0).Select(x => x.Sample).ToList();
        var holdout = indexed.Where(x => x.Index % 3 == 0).Select(x => x.Sample).ToList();

        Assert.True(train.Count >= 16, $"FD12 train set too small: {train.Count}");
        Assert.True(holdout.Count >= 8, $"FD12 holdout set too small: {holdout.Count}");

        double kEff = FitEffectiveCoupling(train);
        _output.WriteLine($"[FD12] fitted k_T(train) = {kEff:E6}, trainN={train.Count}, holdoutN={holdout.Count}");
        Assert.True(double.IsFinite(kEff) && kEff > 0.0, "FD12 fitted k_T must be finite and positive.");

        var holdoutRatios = holdout
            .Select(s =>
            {
                double omegaCal = kEff * s.OmegaRaw;
                double ratio = omegaCal / Math.Max(s.OmegaRef, 1e-18);
                _output.WriteLine($"[FD12] holdout: omega={s.AngularSpeed:F2}, r={s.Radius:F1}, Ω_cal={omegaCal:E6}, Ω_ref={s.OmegaRef:E6}, ratio={ratio:F6}");
                return ratio;
            })
            .ToList();

        double meanRatio = holdoutRatios.Average();
        double minRatio = holdoutRatios.Min();
        double maxRatio = holdoutRatios.Max();
        double spread = maxRatio - minRatio;

        _output.WriteLine($"[FD12] holdout meanRatio={meanRatio:F6}, minRatio={minRatio:F6}, maxRatio={maxRatio:F6}, spread={spread:F6}");

        Assert.InRange(meanRatio, 0.95, 1.05);
        Assert.True(minRatio >= 0.95 && maxRatio <= 1.05, "Holdout normalization ratios should stay inside 0.95..1.05.");
        Assert.True(spread < 0.08, "Holdout ratio spread is too wide for a stable single-coupling normalization.");
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void FD13_VectorCoupling_Should_Remain_Stable_Under_SourceDiscretization()
    {
        const double cLt = 1.0;

        var discretizations = new[]
        {
            new { Name = "coarse", Spacing = 0.40, HalfCount = 4, DensityRadius = 0.95 },
            new { Name = "medium", Spacing = 0.30, HalfCount = 5, DensityRadius = 0.95 },
            new { Name = "fine",   Spacing = 0.24, HalfCount = 6, DensityRadius = 0.95 }
        };

        var results = new List<(string Name, double KEff, double HoldoutMean, double HoldoutSpread, double JLinSpread, double R3Spread, double R3InvariantMean)>();

        foreach (var cfg in discretizations)
        {
            var all = BuildCouplingSamplesForDiscretization(
                angularSpeeds: new[] { 0.55, 0.75, 0.95, 1.15, 1.35, 1.55 },
                radii: new[] { 3.8, 4.4, 5.0, 5.6, 6.2, 6.8 },
                step: 0.15,
                cLt: cLt,
                spacing: cfg.Spacing,
                halfCount: cfg.HalfCount,
                densityRadius: cfg.DensityRadius);

            var indexed = all.Select((s, idx) => (Sample: s, Index: idx)).ToList();
            var train = indexed.Where(x => x.Index % 3 != 0).Select(x => x.Sample).ToList();
            var holdout = indexed.Where(x => x.Index % 3 == 0).Select(x => x.Sample).ToList();

            double kEff = FitEffectiveCoupling(train);

            var holdoutRatios = holdout
                .Select(s => (kEff * s.OmegaRaw) / Math.Max(s.OmegaRef, 1e-18))
                .ToList();
            double holdoutMean = holdoutRatios.Average();
            double holdoutSpread = holdoutRatios.Max() - holdoutRatios.Min();

            var omegaSet = new[] { 0.6, 1.0, 1.4 };
            var jLinearK = omegaSet
                .Select(omega =>
                {
                    var src = CreateRigidRotatingSource(omega, cfg.Spacing, cfg.HalfCount, cfg.DensityRadius);
                    double j = ComputeAngularMomentumNorm(src);
                    double omegaVal = Math.Abs(ComputeCurlField(new Vec3(4.5, 0.0, 0.0), src, step: 0.15).Z);
                    return omegaVal / Math.Max(j, 1e-18);
                })
                .ToList();
            double jLinMean = jLinearK.Average();
            double jLinSpread = (jLinearK.Max() - jLinearK.Min()) / Math.Max(jLinMean, 1e-18);

            var sourceMid = CreateRigidRotatingSource(1.0, cfg.Spacing, cfg.HalfCount, cfg.DensityRadius);
            var r3Invariants = new[] { 4.0, 5.0, 6.0 }
                .Select(r =>
                {
                    double omegaVal = Math.Abs(ComputeCurlField(new Vec3(r, 0.0, 0.0), sourceMid, step: 0.15).Z);
                    return omegaVal * r * r * r;
                })
                .ToList();
            double r3Mean = r3Invariants.Average();
            double r3Spread = (r3Invariants.Max() - r3Invariants.Min()) / Math.Max(r3Mean, 1e-18);

            _output.WriteLine($"[FD13] {cfg.Name}: k_T={kEff:E6}, holdoutMean={holdoutMean:F6}, holdoutSpread={holdoutSpread:F6}, JlinSpread={jLinSpread:F6}, R3Spread={r3Spread:F6}, R3Mean={r3Mean:E6}");

            Assert.InRange(holdoutMean, 0.95, 1.05);
            Assert.True(holdoutSpread < 0.10, $"{cfg.Name}: holdout spread too large.");
            Assert.True(jLinSpread < 0.12, $"{cfg.Name}: J-linearity shape too unstable.");
            Assert.True(r3Spread < 0.20, $"{cfg.Name}: r^-3 shape too unstable.");

            results.Add((cfg.Name, kEff, holdoutMean, holdoutSpread, jLinSpread, r3Spread, r3Mean));
        }

        double kMean = results.Average(r => r.KEff);
        double kRelSpread = (results.Max(r => r.KEff) - results.Min(r => r.KEff)) / Math.Max(kMean, 1e-18);
        double r3GlobalMean = results.Average(r => r.R3InvariantMean);
        double r3GlobalRelSpread = (results.Max(r => r.R3InvariantMean) - results.Min(r => r.R3InvariantMean)) / Math.Max(r3GlobalMean, 1e-18);

        _output.WriteLine($"[FD13] cross-config: kRelSpread={kRelSpread:F6}, R3InvariantRelSpread={r3GlobalRelSpread:F6}");

        Assert.True(kRelSpread < 0.12, "Effective coupling k_T should remain stable across source discretizations.");
        Assert.True(r3GlobalRelSpread < 0.20, "Far-field dipole-shape level should remain controlled across source discretizations.");
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void FD14_VectorSector_Should_Preserve_ScalarTRM_Limits_When_SpinZero()
    {
        // Compatibility guard:
        // for J=0 (spinless source), the vector sector must collapse to zero and not alter scalar-path limits.
        var configs = new[]
        {
            new { Name = "coarse", Spacing = 0.40, HalfCount = 4, DensityRadius = 0.95 },
            new { Name = "medium", Spacing = 0.30, HalfCount = 5, DensityRadius = 0.95 },
            new { Name = "fine",   Spacing = 0.24, HalfCount = 6, DensityRadius = 0.95 }
        };

        var couplings = new[] { 0.5, 1.0, 2.0, 3.0 };
        var probes = new[]
        {
            new Vec3(2.8, 0.2, 0.1),
            new Vec3(3.6, -0.4, 0.3),
            new Vec3(4.8, 0.5, -0.2)
        };

        foreach (var cfg in configs)
        {
            var spinless = CreateRigidRotatingSource(0.0, cfg.Spacing, cfg.HalfCount, cfg.DensityRadius);
            double j = ComputeAngularMomentumNorm(spinless);
            _output.WriteLine($"[FD14] {cfg.Name}: J_spinless={j:E6}");
            Assert.True(j < 1e-14, $"{cfg.Name}: spinless source should have near-zero angular momentum.");

            foreach (double k in couplings)
            {
                foreach (var p in probes)
                {
                    Vec3 a = ComputeVectorPotential(p, spinless, coupling: k);
                    Vec3 b = ComputeCurlField(p, spinless, step: 0.15, coupling: k);
                    double aNorm = a.Norm();
                    double bNorm = b.Norm();

                    // No vector-induced precession proxy for any direction when B=0.
                    double omegaRadial = ComputePrecessionProxy(p.Normalized(), b);
                    double omegaTangential = ComputePrecessionProxy(new Vec3(-p.Y, p.X, 0.0).Normalized(), b);

                    _output.WriteLine($"[FD14] {cfg.Name} k={k:F1} p=({p.X:F2},{p.Y:F2},{p.Z:F2}) |A|={aNorm:E6} |B|={bNorm:E6} Ωr={omegaRadial:E6} Ωt={omegaTangential:E6}");

                    Assert.True(aNorm < 1e-12, $"{cfg.Name}: spin-zero should yield near-zero vector potential.");
                    Assert.True(bNorm < 1e-12, $"{cfg.Name}: spin-zero should yield near-zero curl field.");
                    Assert.True(omegaRadial < 1e-12 && omegaTangential < 1e-12, $"{cfg.Name}: spin-zero should yield near-zero vector-sector precession proxy.");
                }
            }
        }
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void FD15_RotatingSource_Should_Produce_ProgradeRetrograde_LightPathAsymmetry()
    {
        // Structural light-path asymmetry guard (no absolute GR fit):
        // compare signed prograde vs retrograde transport proxies at fixed radius.
        var spinPlus = CreateRigidRotatingSource(angularSpeed: +1.0);
        var spinMinus = CreateRigidRotatingSource(angularSpeed: -1.0);
        var staticSource = CreateRigidRotatingSource(angularSpeed: 0.0);

        var probe = new Vec3(4.2, 0.8, 0.0); // off-axis point in equatorial plane
        var rHat = probe.Normalized();
        var ePhi = new Vec3(-probe.Y, probe.X, 0.0).Normalized();
        var ePhiRetro = -1.0 * ePhi;

        double SignedTransportProxy(IReadOnlyList<SourceCell> source, Vec3 direction)
        {
            Vec3 b = ComputeCurlField(probe, source, step: 0.15);
            return direction.Cross(rHat).Dot(b);
        }

        double proPlus = SignedTransportProxy(spinPlus, ePhi);
        double retroPlus = SignedTransportProxy(spinPlus, ePhiRetro);
        double deltaPlus = proPlus - retroPlus;

        double proMinus = SignedTransportProxy(spinMinus, ePhi);
        double retroMinus = SignedTransportProxy(spinMinus, ePhiRetro);
        double deltaMinus = proMinus - retroMinus;

        double proStatic = SignedTransportProxy(staticSource, ePhi);
        double retroStatic = SignedTransportProxy(staticSource, ePhiRetro);
        double deltaStatic = proStatic - retroStatic;

        _output.WriteLine($"[FD15] spin+ : pro={proPlus:E6}, retro={retroPlus:E6}, delta={deltaPlus:E6}");
        _output.WriteLine($"[FD15] spin- : pro={proMinus:E6}, retro={retroMinus:E6}, delta={deltaMinus:E6}");
        _output.WriteLine($"[FD15] static: pro={proStatic:E6}, retro={retroStatic:E6}, delta={deltaStatic:E6}");

        Assert.True(Math.Abs(deltaPlus) > 1e-6, "Rotating source should produce nonzero prograde/retrograde asymmetry.");
        Assert.True(Math.Abs(deltaStatic) < 1e-12, "Non-rotating source should produce near-zero prograde/retrograde asymmetry.");
        Assert.True(deltaPlus * deltaMinus < 0.0, "Spin reversal should flip asymmetry sign.");
    }

    private static List<SourceCell> CreateRigidRotatingSource(
        double angularSpeed,
        double spacing = 0.30,
        int halfCount = 5,
        double densityRadius = 0.95)
    {
        double dv = spacing * spacing * spacing;

        var cells = new List<SourceCell>((2 * halfCount + 1) * (2 * halfCount + 1) * (2 * halfCount + 1));

        for (int ix = -halfCount; ix <= halfCount; ix++)
        {
            for (int iy = -halfCount; iy <= halfCount; iy++)
            {
                for (int iz = -halfCount; iz <= halfCount; iz++)
                {
                    double x = ix * spacing;
                    double y = iy * spacing;
                    double z = iz * spacing;

                    double r2 = x * x + y * y + z * z;
                    double rho = Math.Exp(-r2 / (densityRadius * densityRadius));
                    if (rho < 1e-8)
                        continue;

                    // Rigid rotation around z-axis: v = omega x r = (-omega*y, omega*x, 0)
                    var velocity = new Vec3(-angularSpeed * y, angularSpeed * x, 0.0);
                    var currentDensity = rho * velocity;
                    var weightedCurrent = dv * currentDensity;

                    cells.Add(new SourceCell(new Vec3(x, y, z), weightedCurrent));
                }
            }
        }

        return cells;
    }

    private static Vec3 ComputeVectorPotential(Vec3 point, IReadOnlyList<SourceCell> source, double coupling = 1.0)
    {
        var a = Vec3.Zero;
        const double softening = 1e-8;

        foreach (var cell in source)
        {
            double dist = (point - cell.Position).Norm();
            double inv = coupling / Math.Max(dist, softening);
            a += inv * cell.WeightedCurrent;
        }

        return a;
    }

    private static Vec3 ComputeCurlField(Vec3 point, IReadOnlyList<SourceCell> source, double step, double coupling = 1.0)
    {
        var ex = new Vec3(step, 0.0, 0.0);
        var ey = new Vec3(0.0, step, 0.0);
        var ez = new Vec3(0.0, 0.0, step);

        Vec3 aXp = ComputeVectorPotential(point + ex, source, coupling);
        Vec3 aXm = ComputeVectorPotential(point - ex, source, coupling);
        Vec3 aYp = ComputeVectorPotential(point + ey, source, coupling);
        Vec3 aYm = ComputeVectorPotential(point - ey, source, coupling);
        Vec3 aZp = ComputeVectorPotential(point + ez, source, coupling);
        Vec3 aZm = ComputeVectorPotential(point - ez, source, coupling);

        double dAzDy = (aYp.Z - aYm.Z) / (2.0 * step);
        double dAyDz = (aZp.Y - aZm.Y) / (2.0 * step);
        double dAxDz = (aZp.X - aZm.X) / (2.0 * step);
        double dAzDx = (aXp.Z - aXm.Z) / (2.0 * step);
        double dAyDx = (aXp.Y - aXm.Y) / (2.0 * step);
        double dAxDy = (aYp.X - aYm.X) / (2.0 * step);

        return new Vec3(
            dAzDy - dAyDz,
            dAxDz - dAzDx,
            dAyDx - dAxDy);
    }

    private static List<SourceCell> ScaleSourceCurrent(IReadOnlyList<SourceCell> source, double currentScale)
    {
        return source
            .Select(c => new SourceCell(c.Position, currentScale * c.WeightedCurrent))
            .ToList();
    }

    private static double ComputePrecessionProxy(Vec3 velocity, Vec3 bField)
    {
        return velocity.Cross(bField).Norm();
    }

    private static List<CouplingSample> BuildCouplingSamples(
        IReadOnlyList<double> angularSpeeds,
        IReadOnlyList<double> radii,
        double step,
        double cLt)
    {
        var samples = new List<CouplingSample>();

        foreach (double omega in angularSpeeds)
        {
            var source = CreateRigidRotatingSource(angularSpeed: omega);
            double j = ComputeAngularMomentumNorm(source);

            foreach (double r in radii)
            {
                double omegaRaw = Math.Abs(ComputeCurlField(new Vec3(r, 0.0, 0.0), source, step).Z);
                double omegaRef = cLt * j / (r * r * r);
                samples.Add(new CouplingSample(omega, r, j, omegaRaw, omegaRef));
            }
        }

        return samples;
    }

    private static List<CouplingSample> BuildCouplingSamplesForDiscretization(
        IReadOnlyList<double> angularSpeeds,
        IReadOnlyList<double> radii,
        double step,
        double cLt,
        double spacing,
        int halfCount,
        double densityRadius)
    {
        var samples = new List<CouplingSample>();

        foreach (double omega in angularSpeeds)
        {
            var source = CreateRigidRotatingSource(omega, spacing, halfCount, densityRadius);
            double j = ComputeAngularMomentumNorm(source);

            foreach (double r in radii)
            {
                double omegaRaw = Math.Abs(ComputeCurlField(new Vec3(r, 0.0, 0.0), source, step).Z);
                double omegaRef = cLt * j / (r * r * r);
                samples.Add(new CouplingSample(omega, r, j, omegaRaw, omegaRef));
            }
        }

        return samples;
    }

    private static double FitEffectiveCoupling(IReadOnlyList<CouplingSample> samples)
    {
        // Least-squares fit for Ω_cal = k_T * Ω_raw against Ω_ref.
        double num = samples.Sum(s => s.OmegaRaw * s.OmegaRef);
        double den = samples.Sum(s => s.OmegaRaw * s.OmegaRaw);
        return den > 0.0 ? num / den : 0.0;
    }

    private static double ComputeAngularMomentumNorm(IReadOnlyList<SourceCell> source)
    {
        var l = Vec3.Zero;
        foreach (var cell in source)
        {
            l += cell.Position.Cross(cell.WeightedCurrent);
        }
        return l.Norm();
    }

    private readonly record struct SourceCell(Vec3 Position, Vec3 WeightedCurrent);
    private readonly record struct CouplingSample(double AngularSpeed, double Radius, double AngularMomentum, double OmegaRaw, double OmegaRef);

    private readonly record struct Vec3(double X, double Y, double Z)
    {
        public static Vec3 Zero => new(0.0, 0.0, 0.0);

        public double Norm() => Math.Sqrt(X * X + Y * Y + Z * Z);
        public double Dot(Vec3 other) => X * other.X + Y * other.Y + Z * other.Z;

        public Vec3 Normalized()
        {
            double n = Norm();
            return n > 0.0 ? (1.0 / n) * this : Zero;
        }

        public Vec3 Cross(Vec3 other)
            => new(
                Y * other.Z - Z * other.Y,
                Z * other.X - X * other.Z,
                X * other.Y - Y * other.X);

        public static Vec3 operator +(Vec3 a, Vec3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 operator *(double s, Vec3 v) => new(s * v.X, s * v.Y, s * v.Z);
        public static Vec3 operator *(Vec3 v, double s) => s * v;
    }
}
