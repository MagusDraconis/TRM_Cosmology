namespace TRM.Core.Lattice;

public sealed class LatticeService : ILatticeService
{
    public LatticeSnapshot ComputeSnapshot(LatticeInput input)
    {
        var n = Math.Clamp(input.NodeCount, 8, 128);
        var coupling = Math.Clamp(input.Coupling, 0.0, 2.0);
        var injectedEnergy = Math.Max(0.0, input.InjectedEnergy);
        var injectionIndex = ((input.InjectionIndex % n) + n) % n;

        var energies = new double[n];
        var sigma = Math.Max(1.0, n / 14.0);

        for (var i = 0; i < n; i++)
        {
            var distance = CircularDistance(i, injectionIndex, n);
            var local = injectedEnergy * Math.Exp(-(distance * distance) / (2.0 * sigma * sigma));
            energies[i] = local;
        }

        var smoothed = new double[n];
        for (var i = 0; i < n; i++)
        {
            var left = energies[(i - 1 + n) % n];
            var center = energies[i];
            var right = energies[(i + 1) % n];

            var neighborMean = 0.5 * (left + right);
            smoothed[i] = center + (coupling * (neighborMean - center));
        }

        var maxDistance = Math.Max(1, n / 2);
        var distances = new double[maxDistance + 1];
        var correlation = new double[maxDistance + 1];
        var padeKernel = new double[maxDistance + 1];

        var corr0 = 0.0;
        for (var i = 0; i < n; i++)
        {
            corr0 += smoothed[i] * smoothed[i];
        }

        corr0 /= n;
        corr0 = Math.Max(corr0, 1e-30);

        const double b = 1.248;

        for (var d = 0; d <= maxDistance; d++)
        {
            var c = 0.0;
            for (var i = 0; i < n; i++)
            {
                c += smoothed[i] * smoothed[(i + d) % n];
            }

            c /= n;

            var x = d / Math.Max(1.0, n / 4.0);
            var kernel = 1.0 / (1.0 + x + b * x * x + Math.Pow(x, 4));

            distances[d] = d;
            correlation[d] = c / corr0;
            padeKernel[d] = kernel;
        }

        return new LatticeSnapshot(smoothed, distances, correlation, padeKernel);
    }

    private static int CircularDistance(int i, int j, int n)
    {
        var linear = Math.Abs(i - j);
        return Math.Min(linear, n - linear);
    }
}
