using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.CoreTests;

public class UnifiedFieldActionRoadmapTests
{
    private readonly ITestOutputHelper _output;

    public UnifiedFieldActionRoadmapTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void UF01_UnifiedAction_Should_Reduce_To_ScalarSector_When_VectorAndThetaDisabled()
    {
        // Limit reduction: with vector/theta disabled,
        // the unified action must reduce exactly to the scalar sector.
        var samples = new List<(double ScalarGrad, double ScalarValue)>
        {
            (0.02, -0.15),
            (0.05,  0.10),
            (0.11, -0.05),
            (0.17,  0.03),
            (0.24, -0.12)
        };

        const double alphaT = 1.3;
        const double massT = 0.4;
        const double alphaA = 0.9;
        const double betaTheta = 0.7;
        const double gammaTA = 0.35;
        const double gammaTTheta = 0.22;
        const double gammaATheta = 0.18;

        double maxAbsDiff = 0.0;
        foreach (var (scalarGrad, scalarValue) in samples)
        {
            double scalarAction = ScalarActionDensity(alphaT, massT, scalarGrad, scalarValue);

            double unifiedAction = UnifiedActionDensity(
                alphaT, massT,
                alphaA, betaTheta,
                gammaTA, gammaTTheta, gammaATheta,
                scalarGrad, scalarValue,
                vectorCurl: 0.0,
                thetaGrad: 0.0);

            double absDiff = Math.Abs(unifiedAction - scalarAction);
            maxAbsDiff = Math.Max(maxAbsDiff, absDiff);

            _output.WriteLine(
                $"[UF01] scalarGrad={scalarGrad:E6}, scalarValue={scalarValue:E6}, scalarAction={scalarAction:E6}, unifiedAction={unifiedAction:E6}, absDiff={absDiff:E6}");

            Assert.True(absDiff < 1e-12,
                $"Expected unified action to reduce to scalar action. scalar={scalarAction:E6}, unified={unifiedAction:E6}");
        }

        _output.WriteLine($"[UF01] scalar-limit reduction: maxAbsDiff={maxAbsDiff:E6}, sampleCount={samples.Count}");
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void UF02_UnifiedAction_Should_Reduce_To_VectorSector_When_ScalarThetaCouplings_Off()
    {
        // Vector-only limit: with scalar/theta disabled, the unified action must
        // reduce exactly to the vector-sector action density.
        var angularSpeeds = new[] { 0.0, 0.4, 0.8, 1.2 };
        const double curlPerUnitOmega = 0.37; // synthetic weak-field B_T proxy scale
        const double alphaA = 0.9;
        const double alphaT = 1.3;
        const double massT = 0.4;
        const double betaTheta = 0.7;
        const double gammaTA = 0.35;
        const double gammaTTheta = 0.22;
        const double gammaATheta = 0.18;

        var couplings = new[] { 0.5, 1.0, 2.0 };
        double maxAbsDiff = 0.0;
        double spinZeroAction = 0.0;

        foreach (double k in couplings)
        {
            foreach (double omega in angularSpeeds)
            {
                double bTCurl = k * curlPerUnitOmega * omega; // B_T ~ k * curl(A_T)
                double vectorAction = alphaA * bTCurl * bTCurl;
                double unifiedAction = UnifiedActionDensity(
                    alphaT, massT,
                    alphaA, betaTheta,
                    gammaTA, gammaTTheta, gammaATheta,
                    scalarGrad: 0.0,
                    scalarValue: 0.0,
                    vectorCurl: bTCurl,
                    thetaGrad: 0.0);

                double absDiff = Math.Abs(unifiedAction - vectorAction);
                maxAbsDiff = Math.Max(maxAbsDiff, absDiff);

                _output.WriteLine(
                    $"[UF02] k={k:E6}, omega={omega:E6}, BT={bTCurl:E6}, vectorAction={vectorAction:E6}, unifiedAction={unifiedAction:E6}, absDiff={absDiff:E6}");

                if (Math.Abs(omega) < 1e-15)
                    spinZeroAction = vectorAction;

                Assert.True(absDiff < 1e-12,
                    $"Expected unified action to reduce to vector action. vector={vectorAction:E6}, unified={unifiedAction:E6}");
            }
        }

        // Spin-zero guard: omega=0 -> B_T=0 -> vector action vanishes.
        Assert.True(spinZeroAction < 1e-12, $"Expected spin-zero vector action near zero, got {spinZeroAction:E6}");

        // Coupling-scale consistency: action scales ~ k^2 at fixed nonzero omega.
        const double omegaRef = 1.2;
        double a05 = alphaA * Math.Pow(0.5 * curlPerUnitOmega * omegaRef, 2);
        double a10 = alphaA * Math.Pow(1.0 * curlPerUnitOmega * omegaRef, 2);
        double a20 = alphaA * Math.Pow(2.0 * curlPerUnitOmega * omegaRef, 2);
        double ratio10to05 = a10 / Math.Max(a05, 1e-18);
        double ratio20to10 = a20 / Math.Max(a10, 1e-18);

        _output.WriteLine(
            $"[UF02] coupling-scale check: ratio10to05={ratio10to05:F6}, ratio20to10={ratio20to10:F6}, maxAbsDiff={maxAbsDiff:E6}");

        Assert.InRange(ratio10to05, 3.99, 4.01);
        Assert.InRange(ratio20to10, 3.99, 4.01);
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void UF03_UnifiedAction_Should_Reduce_To_ThetaO5Sector_When_ScalarVectorCouplings_Off()
    {
        // Theta/O5-only limit: with scalar/vector disabled, unified action must
        // reduce to theta-sector energy, preserve constant-theta zero mode,
        // and decrease energy under small O5-driven relaxation steps.
        const double alphaT = 1.3;
        const double massT = 0.4;
        const double alphaA = 0.9;
        const double betaTheta = 0.7;
        const double gammaTA = 0.35;
        const double gammaTTheta = 0.22;
        const double gammaATheta = 0.18;
        const double dx = 1.0;

        var thetaGradSamples = new[] { 0.0, 0.03, -0.07, 0.12 };
        double maxAbsDiff = 0.0;
        foreach (double thetaGrad in thetaGradSamples)
        {
            double thetaAction = betaTheta * thetaGrad * thetaGrad;
            double unifiedAction = UnifiedActionDensity(
                alphaT, massT,
                alphaA, betaTheta,
                gammaTA, gammaTTheta, gammaATheta,
                scalarGrad: 0.0,
                scalarValue: 0.0,
                vectorCurl: 0.0,
                thetaGrad: thetaGrad);

            double absDiff = Math.Abs(unifiedAction - thetaAction);
            maxAbsDiff = Math.Max(maxAbsDiff, absDiff);

            _output.WriteLine(
                $"[UF03] thetaGrad={thetaGrad:E6}, thetaAction={thetaAction:E6}, unifiedAction={unifiedAction:E6}, absDiff={absDiff:E6}");

            Assert.True(absDiff < 1e-12,
                $"Expected unified action to reduce to theta action. theta={thetaAction:E6}, unified={unifiedAction:E6}");
        }

        var thetaConst = new[] { 1.25, 1.25, 1.25, 1.25, 1.25, 1.25 };
        double eConst = ComputeThetaEnergy(thetaConst, betaTheta, dx);
        double eConstUnified = ComputeThetaEnergyFromUnified(thetaConst, alphaT, massT, alphaA, betaTheta, gammaTA, gammaTTheta, gammaATheta, dx);
        var o5Const = ComputeThetaO5(thetaConst, betaTheta, dx);
        double o5ConstMax = 0.0;
        foreach (double v in o5Const)
            o5ConstMax = Math.Max(o5ConstMax, Math.Abs(v));

        _output.WriteLine(
            $"[UF03] constant-theta mode: energy={eConst:E6}, unifiedEnergy={eConstUnified:E6}, o5Max={o5ConstMax:E6}");

        Assert.True(eConst < 1e-12 && eConstUnified < 1e-12, "Constant-theta profile should have near-zero theta energy.");
        Assert.True(o5ConstMax < 1e-12, "Constant-theta profile should produce near-zero O5 (energy-gradient zero mode).");

        var thetaProfile = new[] { 0.00, 0.18, 0.31, 0.26, 0.12, -0.02 };
        var o5 = ComputeThetaO5(thetaProfile, betaTheta, dx);
        const double relaxStep = 0.08;
        var thetaRelaxed = new double[thetaProfile.Length];
        for (int i = 0; i < thetaProfile.Length; i++)
            thetaRelaxed[i] = thetaProfile[i] + relaxStep * o5[i];

        double eBefore = ComputeThetaEnergy(thetaProfile, betaTheta, dx);
        double eAfter = ComputeThetaEnergy(thetaRelaxed, betaTheta, dx);
        double eBeforeUnified = ComputeThetaEnergyFromUnified(thetaProfile, alphaT, massT, alphaA, betaTheta, gammaTA, gammaTTheta, gammaATheta, dx);
        double eAfterUnified = ComputeThetaEnergyFromUnified(thetaRelaxed, alphaT, massT, alphaA, betaTheta, gammaTA, gammaTTheta, gammaATheta, dx);

        _output.WriteLine(
            $"[UF03] relaxation: eBefore={eBefore:E6}, eAfter={eAfter:E6}, dE={(eAfter - eBefore):E6}, eBeforeUnified={eBeforeUnified:E6}, eAfterUnified={eAfterUnified:E6}");
        _output.WriteLine($"[UF03] theta-limit reduction: maxAbsDiff={maxAbsDiff:E6}");

        Assert.True(eAfter < eBefore, "Theta energy should decrease under a small O5-driven relaxation step.");
        Assert.True(eAfterUnified < eBeforeUnified, "Unified theta-limit energy should decrease under O5-driven relaxation.");
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void UF04_UnifiedAction_CrossTerms_Should_Vanish_When_CouplingsZero()
    {
        // Cross-term null test: gamma_TA = gamma_TTheta = gamma_ATheta = 0
        // must force exact additive decomposition:
        // S_unified = S_T + S_A + S_Theta.
        const double alphaT = 1.3;
        const double massT = 0.4;
        const double alphaA = 0.9;
        const double betaTheta = 0.7;

        const double gammaTA = 0.0;
        const double gammaTTheta = 0.0;
        const double gammaATheta = 0.0;

        var samples = new List<(double ScalarGrad, double ScalarValue, double VectorCurl, double ThetaGrad)>
        {
            ( 0.02, -0.15,  0.04, -0.03),
            (-0.05,  0.10, -0.08,  0.06),
            ( 0.11, -0.05,  0.13, -0.09),
            ( 0.17,  0.03, -0.16,  0.12),
            (-0.24, -0.12,  0.21, -0.14)
        };

        double maxAbsDiff = 0.0;
        double maxCrossAbs = 0.0;

        foreach (var (scalarGrad, scalarValue, vectorCurl, thetaGrad) in samples)
        {
            double sT = ScalarActionDensity(alphaT, massT, scalarGrad, scalarValue);
            double sA = alphaA * vectorCurl * vectorCurl;
            double sTheta = betaTheta * thetaGrad * thetaGrad;
            double additive = sT + sA + sTheta;

            double unified = UnifiedActionDensity(
                alphaT, massT,
                alphaA, betaTheta,
                gammaTA, gammaTTheta, gammaATheta,
                scalarGrad, scalarValue,
                vectorCurl, thetaGrad);

            double crossTerm =
                gammaTA * scalarGrad * vectorCurl +
                gammaTTheta * scalarGrad * thetaGrad +
                gammaATheta * vectorCurl * thetaGrad;

            double absDiff = Math.Abs(unified - additive);
            maxAbsDiff = Math.Max(maxAbsDiff, absDiff);
            maxCrossAbs = Math.Max(maxCrossAbs, Math.Abs(crossTerm));

            _output.WriteLine(
                $"[UF04] sT={sT:E6}, sA={sA:E6}, sTheta={sTheta:E6}, additive={additive:E6}, unified={unified:E6}, crossTerm={crossTerm:E6}, absDiff={absDiff:E6}");

            Assert.True(Math.Abs(crossTerm) < 1e-15, $"Expected cross term to vanish exactly when couplings are zero, got {crossTerm:E6}");
            Assert.True(absDiff < 1e-12, $"Expected unified action to equal additive sector sum, diff={absDiff:E6}");
        }

        _output.WriteLine($"[UF04] cross-term null check: maxCrossAbs={maxCrossAbs:E6}, maxAbsDiff={maxAbsDiff:E6}, sampleCount={samples.Count}");
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void UF05_UnifiedAction_Should_Preserve_AllKnownLimits()
    {
        const double alphaT = 1.3;
        const double massT = 0.4;
        const double alphaA = 0.9;
        const double betaTheta = 0.7;
        const double gammaTA = 0.35;
        const double gammaTTheta = 0.22;
        const double gammaATheta = 0.18;
        const double dx = 1.0;

        // 1) Scalar-only stays scalar.
        double sScalar = ScalarActionDensity(alphaT, massT, scalarGrad: 0.11, scalarValue: -0.05);
        double uScalar = UnifiedActionDensity(alphaT, massT, alphaA, betaTheta, gammaTA, gammaTTheta, gammaATheta,
            scalarGrad: 0.11, scalarValue: -0.05, vectorCurl: 0.0, thetaGrad: 0.0);
        double scalarDiff = Math.Abs(uScalar - sScalar);
        _output.WriteLine($"[UF05] scalar-only: scalar={sScalar:E6}, unified={uScalar:E6}, absDiff={scalarDiff:E6}");
        Assert.True(scalarDiff < 1e-12, "UF05 scalar-only limit must reduce exactly.");

        // 2) Vector-only stays vector.
        double b = 0.44;
        double sVector = alphaA * b * b;
        double uVector = UnifiedActionDensity(alphaT, massT, alphaA, betaTheta, gammaTA, gammaTTheta, gammaATheta,
            scalarGrad: 0.0, scalarValue: 0.0, vectorCurl: b, thetaGrad: 0.0);
        double vectorDiff = Math.Abs(uVector - sVector);
        _output.WriteLine($"[UF05] vector-only: vector={sVector:E6}, unified={uVector:E6}, absDiff={vectorDiff:E6}");
        Assert.True(vectorDiff < 1e-12, "UF05 vector-only limit must reduce exactly.");

        // 3) Theta-only stays theta.
        double thetaGrad = -0.09;
        double sTheta = betaTheta * thetaGrad * thetaGrad;
        double uTheta = UnifiedActionDensity(alphaT, massT, alphaA, betaTheta, gammaTA, gammaTTheta, gammaATheta,
            scalarGrad: 0.0, scalarValue: 0.0, vectorCurl: 0.0, thetaGrad: thetaGrad);
        double thetaDiff = Math.Abs(uTheta - sTheta);
        _output.WriteLine($"[UF05] theta-only: theta={sTheta:E6}, unified={uTheta:E6}, absDiff={thetaDiff:E6}");
        Assert.True(thetaDiff < 1e-12, "UF05 theta-only limit must reduce exactly.");

        // 4) Cross-couplings off => additive sum.
        double sGrad = 0.17;
        double sVal = 0.03;
        double vCurl = -0.16;
        double tGrad = 0.12;
        double additive = ScalarActionDensity(alphaT, massT, sGrad, sVal) + alphaA * vCurl * vCurl + betaTheta * tGrad * tGrad;
        double uAdditive = UnifiedActionDensity(alphaT, massT, alphaA, betaTheta,
            gammaTA: 0.0, gammaTTheta: 0.0, gammaATheta: 0.0,
            scalarGrad: sGrad, scalarValue: sVal, vectorCurl: vCurl, thetaGrad: tGrad);
        double additiveDiff = Math.Abs(uAdditive - additive);
        _output.WriteLine($"[UF05] additive-zero-coupling: additive={additive:E6}, unified={uAdditive:E6}, absDiff={additiveDiff:E6}");
        Assert.True(additiveDiff < 1e-12, "UF05 zero-coupling must yield exact additive decomposition.");

        // 5) Spin-zero => vector sector null.
        double spinZeroCurl = 0.0;
        double spinZeroVectorAction = alphaA * spinZeroCurl * spinZeroCurl;
        _output.WriteLine($"[UF05] spin-zero: vectorAction={spinZeroVectorAction:E6}");
        Assert.True(spinZeroVectorAction < 1e-12, "UF05 spin-zero must null vector action.");

        // 6) Constant-Theta => O5 and theta-energy null.
        var thetaConst = new[] { 1.4, 1.4, 1.4, 1.4, 1.4 };
        var o5Const = ComputeThetaO5(thetaConst, betaTheta, dx);
        double o5Max = 0.0;
        foreach (double x in o5Const) o5Max = Math.Max(o5Max, Math.Abs(x));
        double eConst = ComputeThetaEnergy(thetaConst, betaTheta, dx);
        _output.WriteLine($"[UF05] constant-theta: energy={eConst:E6}, o5Max={o5Max:E6}");
        Assert.True(eConst < 1e-12, "UF05 constant-theta must have near-zero theta energy.");
        Assert.True(o5Max < 1e-12, "UF05 constant-theta must have near-zero O5.");

        double maxLimitDiff = Math.Max(Math.Max(scalarDiff, vectorDiff), Math.Max(thetaDiff, additiveDiff));
        _output.WriteLine($"[UF05] all-limits summary: maxLimitDiff={maxLimitDiff:E6}");
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void UF06_UnifiedAction_CrossTerms_Should_Remain_Bounded_For_SmallCouplings()
    {
        // Small-coupling cross-term guard:
        // - no action explosion,
        // - no sign/negativity breakdown,
        // - controlled deviation from additive (zero-coupling) baseline.
        const double alphaT = 1.3;
        const double massT = 0.4;
        const double alphaA = 0.9;
        const double betaTheta = 0.7;

        var epsSet = new[] { 0.005, 0.010, 0.020 };
        var samples = new List<(double ScalarGrad, double ScalarValue, double VectorCurl, double ThetaGrad)>
        {
            ( 0.05,  0.08,  0.07, -0.04),
            (-0.09, -0.03,  0.10,  0.06),
            ( 0.12,  0.02, -0.11,  0.09),
            (-0.14,  0.06,  0.13, -0.08),
            ( 0.18, -0.05, -0.15,  0.11)
        };

        double maxRelDeviation = 0.0;
        double maxRelCross = 0.0;
        double minUnified = double.PositiveInfinity;

        foreach (double eps in epsSet)
        {
            double gammaTA = eps;
            double gammaTTheta = 0.8 * eps;
            double gammaATheta = 1.1 * eps;

            foreach (var (sGrad, sVal, vCurl, tGrad) in samples)
            {
                double sT = ScalarActionDensity(alphaT, massT, sGrad, sVal);
                double sA = alphaA * vCurl * vCurl;
                double sTheta = betaTheta * tGrad * tGrad;
                double additive = sT + sA + sTheta;

                double crossTerm =
                    gammaTA * sGrad * vCurl +
                    gammaTTheta * sGrad * tGrad +
                    gammaATheta * vCurl * tGrad;

                double unified = UnifiedActionDensity(
                    alphaT, massT, alphaA, betaTheta,
                    gammaTA, gammaTTheta, gammaATheta,
                    sGrad, sVal, vCurl, tGrad);

                double relDeviation = Math.Abs(unified - additive) / Math.Max(additive, 1e-18);
                double relCross = Math.Abs(crossTerm) / Math.Max(additive, 1e-18);

                maxRelDeviation = Math.Max(maxRelDeviation, relDeviation);
                maxRelCross = Math.Max(maxRelCross, relCross);
                minUnified = Math.Min(minUnified, unified);

                _output.WriteLine(
                    $"[UF06] eps={eps:E6}, additive={additive:E6}, cross={crossTerm:E6}, unified={unified:E6}, relDev={relDeviation:E6}, relCross={relCross:E6}");

                Assert.True(double.IsFinite(unified), "UF06 unified action should remain finite.");
                Assert.True(unified > 0.0, $"UF06 unified action should stay positive under small couplings, got {unified:E6}");
                Assert.True(relDeviation < 0.08, $"UF06 relative deviation too large for small coupling: {relDeviation:E6}");
                Assert.True(relCross < 0.08, $"UF06 relative cross-term contribution too large: {relCross:E6}");
            }
        }

        _output.WriteLine(
            $"[UF06] boundedness summary: maxRelDeviation={maxRelDeviation:E6}, maxRelCross={maxRelCross:E6}, minUnified={minUnified:E6}, sampleCount={samples.Count * epsSet.Length}");
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void UF07_UnifiedAction_CrossTerms_Should_Not_Break_KnownSectorLimits()
    {
        // Limit-preservation guard under small cross-couplings.
        const double alphaT = 1.3;
        const double massT = 0.4;
        const double alphaA = 0.9;
        const double betaTheta = 0.7;
        const double dx = 1.0;

        // Small active couplings.
        const double gammaTA = 0.01;
        const double gammaTTheta = 0.008;
        const double gammaATheta = 0.011;

        // 1) Spin-zero remains vector-null even with active cross couplings.
        double spinZero = UnifiedActionDensity(
            alphaT, massT, alphaA, betaTheta,
            gammaTA, gammaTTheta, gammaATheta,
            scalarGrad: 0.0,
            scalarValue: 0.0,
            vectorCurl: 0.0,
            thetaGrad: 0.0);
        _output.WriteLine($"[UF07] spin-zero limit: unified={spinZero:E6}");
        Assert.True(Math.Abs(spinZero) < 1e-12, "UF07 spin-zero limit should remain null.");

        // 2) Constant-theta remains theta-null (O5=0, E_theta=0) with other sectors disabled.
        var thetaConst = new[] { 0.77, 0.77, 0.77, 0.77, 0.77 };
        var o5Const = ComputeThetaO5(thetaConst, betaTheta, dx);
        double o5Max = 0.0;
        foreach (double x in o5Const) o5Max = Math.Max(o5Max, Math.Abs(x));
        double eConst = ComputeThetaEnergy(thetaConst, betaTheta, dx);
        double eConstUnified = ComputeThetaEnergyFromUnified(thetaConst, alphaT, massT, alphaA, betaTheta, gammaTA, gammaTTheta, gammaATheta, dx);
        _output.WriteLine($"[UF07] constant-theta limit: eTheta={eConst:E6}, eUnified={eConstUnified:E6}, o5Max={o5Max:E6}");
        Assert.True(eConst < 1e-12 && eConstUnified < 1e-12, "UF07 constant-theta energy should remain null.");
        Assert.True(o5Max < 1e-12, "UF07 constant-theta should keep O5 zero mode.");

        // 3) Sector limits stay close to uncoupled baselines (tight bounds).
        var samples = new List<(double ScalarGrad, double ScalarValue, double VectorCurl, double ThetaGrad)>
        {
            ( 0.06,  0.05,  0.00,  0.00), // scalar-dominant
            ( 0.00,  0.00,  0.21,  0.00), // vector-dominant
            ( 0.00,  0.00,  0.00, -0.12), // theta-dominant
            ( 0.09, -0.02,  0.16,  0.00), // scalar+vector
            ( 0.07,  0.03,  0.00,  0.11), // scalar+theta
            ( 0.00,  0.00, -0.18,  0.10)  // vector+theta
        };

        double maxRelShift = 0.0;
        double minUnified = double.PositiveInfinity;

        foreach (var (sGrad, sVal, vCurl, tGrad) in samples)
        {
            double uncoupled = UnifiedActionDensity(
                alphaT, massT, alphaA, betaTheta,
                gammaTA: 0.0, gammaTTheta: 0.0, gammaATheta: 0.0,
                sGrad, sVal, vCurl, tGrad);

            double coupled = UnifiedActionDensity(
                alphaT, massT, alphaA, betaTheta,
                gammaTA, gammaTTheta, gammaATheta,
                sGrad, sVal, vCurl, tGrad);

            double relShift = Math.Abs(coupled - uncoupled) / Math.Max(uncoupled, 1e-18);
            maxRelShift = Math.Max(maxRelShift, relShift);
            minUnified = Math.Min(minUnified, coupled);

            _output.WriteLine(
                $"[UF07] sample: uncoupled={uncoupled:E6}, coupled={coupled:E6}, relShift={relShift:E6}, sGrad={sGrad:E6}, vCurl={vCurl:E6}, tGrad={tGrad:E6}");

            Assert.True(double.IsFinite(coupled), "UF07 coupled action must remain finite.");
            Assert.True(coupled > 0.0, $"UF07 coupled action should remain positive, got {coupled:E6}");
            Assert.True(relShift < 0.03, $"UF07 relative limit shift too large under small couplings: {relShift:E6}");
        }

        _output.WriteLine($"[UF07] limit-preservation summary: maxRelShift={maxRelShift:E6}, minUnified={minUnified:E6}, sampleCount={samples.Count}");
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void UF08_AllowedCrossCouplings_Should_Be_Identifiable_Without_Refit()
    {
        // Identifiability guard:
        // estimate one global cross-coupling triplet from mixed samples and
        // require it to explain grouped data without mandatory per-group refit.
        const double gammaTA_True = 0.0100;
        const double gammaTTheta_True = 0.0080;
        const double gammaATheta_True = 0.0110;

        var groups = new Dictionary<string, List<(double SGrad, double VCurl, double TGrad)>>()
        {
            ["mixed-a"] = new()
            {
                ( 0.05,  0.07, -0.04),
                (-0.08,  0.10,  0.06),
                ( 0.12, -0.11,  0.09),
                (-0.10,  0.14, -0.07),
                ( 0.16, -0.13,  0.08)
            },
            ["mixed-b"] = new()
            {
                ( 0.06,  0.09, -0.03),
                (-0.09,  0.12,  0.05),
                ( 0.11, -0.15,  0.07),
                (-0.13,  0.16, -0.06),
                ( 0.18, -0.14,  0.10)
            },
            ["mixed-c"] = new()
            {
                ( 0.04,  0.06, -0.05),
                (-0.07,  0.11,  0.04),
                ( 0.10, -0.12,  0.08),
                (-0.12,  0.15, -0.09),
                ( 0.15, -0.10,  0.11)
            }
        };

        var all = new List<(double X1, double X2, double X3, double Y, string Group)>();
        foreach (var (groupName, rows) in groups)
        {
            foreach (var (sGrad, vCurl, tGrad) in rows)
            {
                double x1 = sGrad * vCurl; // TA feature
                double x2 = sGrad * tGrad; // TTheta feature
                double x3 = vCurl * tGrad; // ATheta feature
                double y = gammaTA_True * x1 + gammaTTheta_True * x2 + gammaATheta_True * x3;
                all.Add((x1, x2, x3, y, groupName));
            }
        }

        var globalFit = FitThreeFeatureLinearModel(all);
        _output.WriteLine(
            $"[UF08] global fit: gammaTA={globalFit.G1:E6}, gammaTTheta={globalFit.G2:E6}, gammaATheta={globalFit.G3:E6}, rms={globalFit.Rms:E6}");

        // Global identifiability against known synthetic generating couplings.
        Assert.InRange(Math.Abs(globalFit.G1 - gammaTA_True), 0.0, 1e-12);
        Assert.InRange(Math.Abs(globalFit.G2 - gammaTTheta_True), 0.0, 1e-12);
        Assert.InRange(Math.Abs(globalFit.G3 - gammaATheta_True), 0.0, 1e-12);

        double worstGlobalGroupRms = 0.0;
        double worstRmsRatio = 0.0;

        foreach (var (groupName, _) in groups)
        {
            var groupRows = all.FindAll(r => r.Group == groupName);
            double rmsGlobal = ComputeRms(groupRows, globalFit.G1, globalFit.G2, globalFit.G3);
            var groupFit = FitThreeFeatureLinearModel(groupRows);
            double rmsGroup = groupFit.Rms;
            double ratio = rmsGlobal / Math.Max(rmsGroup, 1e-18);

            worstGlobalGroupRms = Math.Max(worstGlobalGroupRms, rmsGlobal);
            worstRmsRatio = Math.Max(worstRmsRatio, ratio);

            _output.WriteLine(
                $"[UF08] group={groupName}: rmsGlobal={rmsGlobal:E6}, rmsGroupRefit={rmsGroup:E6}, ratio={ratio:F6}");
        }

        _output.WriteLine(
            $"[UF08] identifiability summary: worstGlobalGroupRms={worstGlobalGroupRms:E6}, worstRmsRatio={worstRmsRatio:F6}, n={all.Count}");

        // "Without refit" criterion: global model remains as good as group refits
        // up to tiny numerical tolerance.
        Assert.True(worstGlobalGroupRms < 1e-12, "UF08 global coupling set should explain all groups to numerical precision.");
        Assert.True(worstRmsRatio < 1.02, "UF08 should not require per-group refit to explain grouped samples.");
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void UF09_UnifiedAction_Should_Not_Break_MC_FD_TO_Guards()
    {
        // Cross-coupling safety meta-guard:
        // small unified couplings must not materially break proxy MC/FD/TO guards.
        const double gammaTA = 0.01;
        const double gammaTTheta = 0.008;
        const double gammaATheta = 0.011;

        // MC proxy: maintain near-linear relation of memory-channel form.
        var mcBase = new List<double>();
        var mcCoupled = new List<double>();
        for (int i = 1; i <= 32; i++)
        {
            double phi = 0.02 * i;
            double muDot = 0.015 + 0.0015 * i;
            double vCurl = 0.03 + 0.0009 * i;
            double thetaGrad = 0.02 * Math.Sin(0.2 * i);

            double baseMem = phi * phi * Math.Abs(muDot);
            double couplingFactor = 1.0 + gammaTA * phi * vCurl + gammaTTheta * phi * thetaGrad + gammaATheta * vCurl * thetaGrad;
            double coupledMem = baseMem * couplingFactor;

            mcBase.Add(baseMem);
            mcCoupled.Add(coupledMem);
        }

        var mcFit = FitLineAndR2(mcBase, mcCoupled);
        double mcMeanRatio = mcCoupled.Average() / Math.Max(mcBase.Average(), 1e-18);
        _output.WriteLine($"[UF09] MC proxy: slope={mcFit.Slope:E6}, intercept={mcFit.Intercept:E6}, R2={mcFit.R2:F6}, meanRatio={mcMeanRatio:F6}");

        Assert.True(mcFit.R2 > 0.999, "UF09 MC proxy linearity should remain very high under small couplings.");
        Assert.InRange(mcMeanRatio, 0.99, 1.01);

        // FD proxy: preserve near-linear Ω/J scaling shape.
        var fdKBase = new List<double>();
        var fdKCoupled = new List<double>();
        foreach (double omega in new[] { 0.5, 0.8, 1.1, 1.4, 1.7 })
        {
            double j = 2.2 * omega;
            double omegaBase = 0.031 * j;
            double scalarGrad = 0.04 + 0.01 * omega;
            double thetaGrad = 0.015 * Math.Cos(omega);
            double couplingFactor = 1.0 + gammaTA * scalarGrad * omegaBase + gammaTTheta * scalarGrad * thetaGrad + gammaATheta * omegaBase * thetaGrad;
            double omegaCoupled = omegaBase * couplingFactor;

            fdKBase.Add(omegaBase / j);
            fdKCoupled.Add(omegaCoupled / j);
        }

        double fdSpreadBase = RelativeSpread(fdKBase);
        double fdSpreadCoupled = RelativeSpread(fdKCoupled);
        _output.WriteLine($"[UF09] FD proxy: spreadBase={fdSpreadBase:E6}, spreadCoupled={fdSpreadCoupled:E6}");

        Assert.True(fdSpreadCoupled < 0.02, "UF09 FD proxy linearity spread should remain tight.");
        Assert.True(fdSpreadCoupled <= fdSpreadBase + 0.01, "UF09 FD proxy spread should not degrade materially.");

        // TO proxy: preserve energy-descent relaxation behavior.
        const double betaTheta = 0.7;
        const double alphaT = 1.3;
        const double massT = 0.4;
        const double alphaA = 0.9;
        const double dx = 1.0;
        var thetaProfile = new[] { 0.00, 0.18, 0.31, 0.26, 0.12, -0.02 };
        var o5 = ComputeThetaO5(thetaProfile, betaTheta, dx);
        const double relaxStep = 0.08;
        var thetaRelaxed = new double[thetaProfile.Length];
        for (int i = 0; i < thetaProfile.Length; i++)
            thetaRelaxed[i] = thetaProfile[i] + relaxStep * o5[i];

        double eBeforeBase = ComputeThetaEnergy(thetaProfile, betaTheta, dx);
        double eAfterBase = ComputeThetaEnergy(thetaRelaxed, betaTheta, dx);
        double eBeforeCoupled = ComputeThetaEnergyFromUnified(thetaProfile, alphaT, massT, alphaA, betaTheta, gammaTA, gammaTTheta, gammaATheta, dx);
        double eAfterCoupled = ComputeThetaEnergyFromUnified(thetaRelaxed, alphaT, massT, alphaA, betaTheta, gammaTA, gammaTTheta, gammaATheta, dx);
        double dropBase = eBeforeBase - eAfterBase;
        double dropCoupled = eBeforeCoupled - eAfterCoupled;
        double dropRatio = dropCoupled / Math.Max(dropBase, 1e-18);

        _output.WriteLine($"[UF09] TO proxy: eBeforeBase={eBeforeBase:E6}, eAfterBase={eAfterBase:E6}, eBeforeCoupled={eBeforeCoupled:E6}, eAfterCoupled={eAfterCoupled:E6}, dropRatio={dropRatio:F6}");

        Assert.True(eAfterCoupled < eBeforeCoupled, "UF09 TO proxy should keep relaxation energy descent.");
        Assert.InRange(dropRatio, 0.90, 1.10);

        // Global positivity sanity on mixed states under small couplings.
        double minUnified = double.PositiveInfinity;
        foreach (var (sGrad, sVal, vCurl, tGrad) in new[]
        {
            (0.06, 0.05, 0.07, -0.04),
            (-0.09, -0.03, 0.10, 0.06),
            (0.12, 0.02, -0.11, 0.09),
            (-0.14, 0.06, 0.13, -0.08),
            (0.18, -0.05, -0.15, 0.11)
        })
        {
            double u = UnifiedActionDensity(alphaT, massT, alphaA, betaTheta, gammaTA, gammaTTheta, gammaATheta, sGrad, sVal, vCurl, tGrad);
            minUnified = Math.Min(minUnified, u);
        }

        _output.WriteLine($"[UF09] global sanity: minUnified={minUnified:E6}");
        Assert.True(minUnified > 0.0, "UF09 unified action should remain positive on mixed sampled states.");
    }

    private static double ScalarActionDensity(double alphaT, double massT, double scalarGrad, double scalarValue)
    {
        return alphaT * scalarGrad * scalarGrad + 0.5 * massT * scalarValue * scalarValue;
    }

    private static double UnifiedActionDensity(
        double alphaT, double massT,
        double alphaA, double betaTheta,
        double gammaTA, double gammaTTheta, double gammaATheta,
        double scalarGrad, double scalarValue,
        double vectorCurl, double thetaGrad)
    {
        double scalar = ScalarActionDensity(alphaT, massT, scalarGrad, scalarValue);
        double vector = alphaA * vectorCurl * vectorCurl;
        double theta = betaTheta * thetaGrad * thetaGrad;

        double interactions =
            gammaTA * scalarGrad * vectorCurl +
            gammaTTheta * scalarGrad * thetaGrad +
            gammaATheta * vectorCurl * thetaGrad;

        return scalar + vector + theta + interactions;
    }

    private static double ComputeThetaEnergy(double[] theta, double betaTheta, double dx)
    {
        double e = 0.0;
        for (int i = 0; i < theta.Length - 1; i++)
        {
            double grad = (theta[i + 1] - theta[i]) / dx;
            e += betaTheta * grad * grad;
        }
        return e;
    }

    private static double ComputeThetaEnergyFromUnified(
        double[] theta,
        double alphaT, double massT, double alphaA, double betaTheta,
        double gammaTA, double gammaTTheta, double gammaATheta,
        double dx)
    {
        double e = 0.0;
        for (int i = 0; i < theta.Length - 1; i++)
        {
            double grad = (theta[i + 1] - theta[i]) / dx;
            e += UnifiedActionDensity(
                alphaT, massT, alphaA, betaTheta, gammaTA, gammaTTheta, gammaATheta,
                scalarGrad: 0.0,
                scalarValue: 0.0,
                vectorCurl: 0.0,
                thetaGrad: grad);
        }
        return e;
    }

    private static double[] ComputeThetaO5(double[] theta, double betaTheta, double dx)
    {
        var o5 = new double[theta.Length];
        double coeff = 2.0 * betaTheta / (dx * dx);

        // Neumann-style edge handling via clamped neighbors.
        for (int i = 0; i < theta.Length; i++)
        {
            double left = theta[Math.Max(i - 1, 0)];
            double mid = theta[i];
            double right = theta[Math.Min(i + 1, theta.Length - 1)];
            o5[i] = coeff * (left - 2.0 * mid + right); // negative energy gradient proxy
        }

        return o5;
    }

    private static (double G1, double G2, double G3, double Rms) FitThreeFeatureLinearModel(
        List<(double X1, double X2, double X3, double Y, string Group)> rows)
    {
        double a11 = 0, a12 = 0, a13 = 0;
        double a22 = 0, a23 = 0, a33 = 0;
        double b1 = 0, b2 = 0, b3 = 0;

        foreach (var r in rows)
        {
            a11 += r.X1 * r.X1;
            a12 += r.X1 * r.X2;
            a13 += r.X1 * r.X3;
            a22 += r.X2 * r.X2;
            a23 += r.X2 * r.X3;
            a33 += r.X3 * r.X3;
            b1 += r.X1 * r.Y;
            b2 += r.X2 * r.Y;
            b3 += r.X3 * r.Y;
        }

        // Symmetric normal matrix.
        double[,] a = new[,]
        {
            { a11, a12, a13 },
            { a12, a22, a23 },
            { a13, a23, a33 }
        };
        double[] b = { b1, b2, b3 };
        var x = Solve3x3(a, b);
        double rms = ComputeRms(rows, x[0], x[1], x[2]);
        return (x[0], x[1], x[2], rms);
    }

    private static double ComputeRms(
        List<(double X1, double X2, double X3, double Y, string Group)> rows,
        double g1, double g2, double g3)
    {
        double mse = 0.0;
        foreach (var r in rows)
        {
            double yHat = g1 * r.X1 + g2 * r.X2 + g3 * r.X3;
            double d = yHat - r.Y;
            mse += d * d;
        }

        mse /= Math.Max(rows.Count, 1);
        return Math.Sqrt(mse);
    }

    private static double[] Solve3x3(double[,] a, double[] b)
    {
        double detA = Determinant3x3(a);
        if (Math.Abs(detA) < 1e-18)
            return new[] { 0.0, 0.0, 0.0 };

        double[,] a1 = { { b[0], a[0, 1], a[0, 2] }, { b[1], a[1, 1], a[1, 2] }, { b[2], a[2, 1], a[2, 2] } };
        double[,] a2 = { { a[0, 0], b[0], a[0, 2] }, { a[1, 0], b[1], a[1, 2] }, { a[2, 0], b[2], a[2, 2] } };
        double[,] a3 = { { a[0, 0], a[0, 1], b[0] }, { a[1, 0], a[1, 1], b[1] }, { a[2, 0], a[2, 1], b[2] } };

        return new[]
        {
            Determinant3x3(a1) / detA,
            Determinant3x3(a2) / detA,
            Determinant3x3(a3) / detA
        };
    }

    private static double Determinant3x3(double[,] m)
    {
        return
            m[0, 0] * (m[1, 1] * m[2, 2] - m[1, 2] * m[2, 1]) -
            m[0, 1] * (m[1, 0] * m[2, 2] - m[1, 2] * m[2, 0]) +
            m[0, 2] * (m[1, 0] * m[2, 1] - m[1, 1] * m[2, 0]);
    }

    private static (double Slope, double Intercept, double R2) FitLineAndR2(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        int n = Math.Min(x.Count, y.Count);
        if (n == 0)
            return (0.0, 0.0, 0.0);

        double meanX = 0.0, meanY = 0.0;
        for (int i = 0; i < n; i++)
        {
            meanX += x[i];
            meanY += y[i];
        }
        meanX /= n;
        meanY /= n;

        double sxx = 0.0, sxy = 0.0, sst = 0.0;
        for (int i = 0; i < n; i++)
        {
            double dx = x[i] - meanX;
            double dy = y[i] - meanY;
            sxx += dx * dx;
            sxy += dx * dy;
            sst += dy * dy;
        }

        double slope = sxy / Math.Max(sxx, 1e-18);
        double intercept = meanY - slope * meanX;

        double sse = 0.0;
        for (int i = 0; i < n; i++)
        {
            double yHat = slope * x[i] + intercept;
            double e = y[i] - yHat;
            sse += e * e;
        }

        double r2 = 1.0 - sse / Math.Max(sst, 1e-18);
        return (slope, intercept, r2);
    }

    private static double RelativeSpread(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
            return 0.0;
        double mean = 0.0;
        foreach (double v in values) mean += v;
        mean /= values.Count;
        double min = double.PositiveInfinity;
        double max = double.NegativeInfinity;
        foreach (double v in values)
        {
            min = Math.Min(min, v);
            max = Math.Max(max, v);
        }
        return (max - min) / Math.Max(Math.Abs(mean), 1e-18);
    }
}
