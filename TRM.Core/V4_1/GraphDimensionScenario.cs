namespace TRM.Core.V4_1;

/// <summary>
/// Immutable representation of raw diagnostic observables for a graph scenario
/// at effective dimension D.
///
/// Classification: FRAMEWORK — provides structured input for F(D) evaluation.
/// No physical claims are embedded in this type.
/// </summary>
public sealed class GraphDimensionScenario
{
    /// <summary>Effective dimension D of the graph (1, 2, 3, …).</summary>
    public int Dimension { get; }

    /// <summary>Second eigenvalue of the graph Laplacian (spectral gap).</summary>
    public double Lambda2 { get; }

    /// <summary>Largest eigenvalue of the graph Laplacian.</summary>
    public double LambdaMax { get; }

    /// <summary>Angular standard deviation of propagation front speed.</summary>
    public double SigmaTheta { get; }

    /// <summary>Mean propagation front speed across directions.</summary>
    public double MeanVTheta { get; }

    /// <summary>Width of the bridge band (spread of Omega*).</summary>
    public double DeltaOmega { get; }

    public GraphDimensionScenario(
        int dimension,
        double lambda2,
        double lambdaMax,
        double sigmaTheta,
        double meanVTheta,
        double deltaOmega)
    {
        if (dimension < 1)
            throw new ArgumentOutOfRangeException(nameof(dimension));
        if (!double.IsFinite(lambda2) || lambda2 <= 0)
            throw new ArgumentOutOfRangeException(nameof(lambda2));
        if (!double.IsFinite(lambdaMax) || lambdaMax <= 0)
            throw new ArgumentOutOfRangeException(nameof(lambdaMax));
        if (lambda2 > lambdaMax)
            throw new ArgumentException("lambda2 must not exceed lambdaMax.");
        if (!double.IsFinite(sigmaTheta) || sigmaTheta < 0)
            throw new ArgumentOutOfRangeException(nameof(sigmaTheta));
        if (!double.IsFinite(meanVTheta) || meanVTheta <= 0)
            throw new ArgumentOutOfRangeException(nameof(meanVTheta));
        if (!double.IsFinite(deltaOmega) || deltaOmega <= 0)
            throw new ArgumentOutOfRangeException(nameof(deltaOmega));

        Dimension = dimension;
        Lambda2 = lambda2;
        LambdaMax = lambdaMax;
        SigmaTheta = sigmaTheta;
        MeanVTheta = meanVTheta;
        DeltaOmega = deltaOmega;
    }
}
