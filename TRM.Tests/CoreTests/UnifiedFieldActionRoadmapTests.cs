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
}
