using System;
using System.Collections.Generic;
using Xunit;

namespace TRM.Tests.CoreTests;

public class UnifiedFieldActionRoadmapTests
{
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

            Assert.True(Math.Abs(unifiedAction - scalarAction) < 1e-12,
                $"Expected unified action to reduce to scalar action. scalar={scalarAction:E6}, unified={unifiedAction:E6}");
        }
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
}
