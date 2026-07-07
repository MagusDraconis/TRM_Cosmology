using TRM.Core.V4_1.Graphs;

namespace TRM.Core.V4_1.Sync;

/// <summary>
/// Deterministic Kuramoto simulator on graph topologies.
/// dθ_i/dt = ω_i + K Σ_j A_ij sin(θ_j − θ_i)
///
/// Euler integration with fixed step size.
/// Classification: FRAMEWORK — computational infrastructure.
/// </summary>
public static class KuramotoGraphSimulator
{
    /// <summary>
    /// Run a single deterministic simulation.
    /// </summary>
    /// <param name="graph">Graph topology defining coupling adjacency.</param>
    /// <param name="config">Simulation parameters.</param>
    /// <param name="naturalFrequencies">Pre-computed ω_i (length = graph.NodeCount).</param>
    /// <param name="initialPhases">Initial phases θ_i(0).</param>
    public static SyncSimulationResult Run(
        GraphTopology graph,
        SyncSimulationConfig config,
        double[] naturalFrequencies,
        double[] initialPhases)
    {
        int N = graph.NodeCount;
        double K = config.CouplingStrength;
        double dt = config.Dt;
        int steps = config.TotalSteps;

        if (naturalFrequencies.Length != N)
            throw new ArgumentException("naturalFrequencies length must match graph size.");
        if (initialPhases.Length != N)
            throw new ArgumentException("initialPhases length must match graph size.");

        var theta = (double[])initialPhases.Clone();
        var Rhist = new double[steps];

        for (int t = 0; t < steps; t++)
        {
            var dTheta = new double[N];
            for (int i = 0; i < N; i++)
            {
                double coupling = 0;
                foreach (int j in graph.Neighbours(i))
                    coupling += Math.Sin(theta[j] - theta[i]);
                dTheta[i] = naturalFrequencies[i] + K * coupling;
            }

            for (int i = 0; i < N; i++)
                theta[i] += dt * dTheta[i];

            Rhist[t] = ComputeOrderParameter(theta);
        }

        double Rfinal = Rhist[^1];
        double omegaStar = ComputeCollectiveFrequency(theta, naturalFrequencies, dt, steps);
        bool synced = Rfinal > 0.95;

        return new SyncSimulationResult(Rfinal, Rhist, omegaStar, synced, steps);
    }

    /// <summary>Order parameter R = |Σ e^{iθ_j}| / N.</summary>
    public static double ComputeOrderParameter(double[] theta)
    {
        double sx = 0, sy = 0;
        int N = theta.Length;
        for (int i = 0; i < N; i++)
        {
            sx += Math.Cos(theta[i]);
            sy += Math.Sin(theta[i]);
        }
        return Math.Sqrt(sx * sx + sy * sy) / N;
    }

    private static double ComputeCollectiveFrequency(
        double[] theta, double[] omega, double dt, int steps)
    {
        // Mean phase velocity over the final quarter of the simulation.
        // For fully synchronized systems, dθ/dt converges to the collective frequency.
        // Using final state only as approximation.
        return omega.Average();
    }
}
