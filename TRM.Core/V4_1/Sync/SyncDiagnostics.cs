using TRM.Core.V4_1.Graphs;

namespace TRM.Core.V4_1.Sync;

/// <summary>
/// Diagnostic utilities for synchronization simulations.
/// Classification: FRAMEWORK — measurement infrastructure.
/// </summary>
public static class SyncDiagnostics
{
    /// <summary>
    /// Run multiple seeded simulations and return the fraction that synchronise.
    /// </summary>
    public static double SyncBasinFraction(
        GraphTopology graph,
        SyncSimulationConfig config,
        double[] naturalFrequencies,
        Random rng)
    {
        int synced = 0;
        for (int run = 0; run < config.SeededRuns; run++)
        {
            var init = new double[graph.NodeCount];
            for (int i = 0; i < init.Length; i++)
                init[i] = rng.NextDouble() * 2.0 * Math.PI;

            var result = KuramotoGraphSimulator.Run(graph, config, naturalFrequencies, init);
            if (result.IsSynchronized) synced++;
        }
        return (double)synced / config.SeededRuns;
    }

    /// <summary>
    /// Measure recovery after a local phase kick at the given node.
    /// Returns final order parameter after kick simulation.
    /// </summary>
    public static double PerturbationRecovery(
        GraphTopology graph,
        SyncSimulationConfig config,
        double[] naturalFrequencies,
        int kickNode)
    {
        // Start from near-synchronised state.
        var init = new double[graph.NodeCount];
        for (int i = 0; i < init.Length; i++)
            init[i] = 0.0;

        // Apply kick: shift the phase of kickNode.
        init[kickNode] = Math.PI / 2.0;

        var result = KuramotoGraphSimulator.Run(graph, config, naturalFrequencies, init);
        return result.FinalOrderParameter;
    }

    /// <summary>
    /// Bridge-band proxy: spread of collective frequencies across seeded runs.
    /// Returns the standard deviation of Omega* estimates.
    /// </summary>
    public static double BridgeBandProxy(
        GraphTopology graph,
        SyncSimulationConfig config,
        double[] naturalFrequencies,
        Random rng)
    {
        var omegas = new List<double>();
        for (int run = 0; run < config.SeededRuns; run++)
        {
            var init = new double[graph.NodeCount];
            for (int i = 0; i < init.Length; i++)
                init[i] = rng.NextDouble() * 2.0 * Math.PI;

            var result = KuramotoGraphSimulator.Run(graph, config, naturalFrequencies, init);
            omegas.Add(result.CollectiveFrequency);
        }

        double mean = omegas.Average();
        double variance = omegas.Sum(o => (o - mean) * (o - mean)) / omegas.Count;
        return Math.Sqrt(variance);
    }

    /// <summary>Generate deterministic natural frequencies from a seeded RNG.</summary>
    public static double[] GenerateNaturalFrequencies(int count, double spread, Random rng)
    {
        var omega = new double[count];
        for (int i = 0; i < count; i++)
            omega[i] = 1.0 + spread * (rng.NextDouble() - 0.5) * 2.0;
        return omega;
    }
}
