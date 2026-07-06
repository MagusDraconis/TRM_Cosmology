namespace TRM.Core.QuantumLoops;

public sealed class QuantumLoopService : IQuantumLoopService
{
    public IReadOnlyList<UvLoopPoint> ComputeUvLoopSeries(UvLoopInput input)
    {
        var lambdaSquared = input.LambdaSquared > 0 ? input.LambdaSquared : 1.0;
        var pMin = Math.Max(0.0, input.PMin);
        var pMax = Math.Max(pMin + 1.0, input.PMax);
        var samples = Math.Clamp(input.Samples, 20, 300);
        var safeKappa = Math.Max(0.001, input.Kappa);

        var points = new List<UvLoopPoint>(samples);
        var step = (pMax - pMin) / (samples - 1);

        for (var i = 0; i < samples; i++)
        {
            var momentumSquared = pMin + (i * step);
            var x = -momentumSquared / lambdaSquared;
            var kernel = 1.0 / (1.0 + x + input.B * x * x + Math.Pow(x, 4));

            var damping = Math.Exp(-safeKappa * momentumSquared / lambdaSquared);
            var oneLoop = Math.Abs(kernel) * damping;
            var twoLoop = oneLoop * oneLoop * (1.0 / (1.0 + momentumSquared / lambdaSquared));

            points.Add(new UvLoopPoint(momentumSquared, kernel, oneLoop, twoLoop));
        }

        return points;
    }
}
