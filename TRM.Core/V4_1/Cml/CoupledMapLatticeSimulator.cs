using TRM.Core.V4_1.Graphs;

namespace TRM.Core.V4_1.Cml;

/// <summary>
/// Deterministic Coupled Map Lattice (CML) simulator on graph topologies.
/// Uses a simple sine-coupled map: x_i(t+1) = (1 - K) f(x_i) + K/deg_i * sum_j f(x_j)
/// where f(x) = sin(x) for bounded deterministic dynamics.
/// Classification: FRAMEWORK — computational infrastructure.
/// </summary>
public static class CoupledMapLatticeSimulator
{
    public static CmlExperimentResult Run(
        GraphTopology graph, CmlExperimentConfig config, Random rng)
    {
        int N = graph.NodeCount;
        double K = config.CouplingStrength;
        int steps = config.TimeSteps;

        // Deterministic initial conditions from seeded RNG.
        var x = new double[N];
        for (int i = 0; i < N; i++)
            x[i] = rng.NextDouble() * 2.0 * Math.PI;

        // Evolve.
        for (int t = 0; t < steps; t++)
        {
            var fx = x.Select(Map).ToArray();
            var next = new double[N];
            for (int i = 0; i < N; i++)
            {
                int deg = graph.Degree(i);
                double neighborSum = 0;
                foreach (int j in graph.Neighbours(i))
                    neighborSum += fx[j];
                double coupling = deg > 0 ? K * neighborSum / deg : 0;
                next[i] = (1.0 - K) * fx[i] + coupling;
            }
            x = next;
        }

        // Order parameter: phase coherence of final state.
        double sx = 0, sy = 0;
        for (int i = 0; i < N; i++) { sx += Math.Cos(x[i]); sy += Math.Sin(x[i]); }
        double R = Math.Sqrt(sx * sx + sy * sy) / N;

        // Collective frequency: mean state value (proxy for phase velocity).
        double omegaStar = x.Average() / (2.0 * Math.PI);

        // Perturbation recovery: re-run with kick at node 0.
        var xKicked = (double[])x.Clone();
        xKicked[0] += Math.PI / 2.0;
        for (int t = 0; t < steps / 4; t++)
        {
            var fx = xKicked.Select(Map).ToArray();
            var next = new double[N];
            for (int i = 0; i < N; i++)
            {
                int deg = graph.Degree(i);
                double neighborSum = 0;
                foreach (int j in graph.Neighbours(i))
                    neighborSum += fx[j];
                double coupling = deg > 0 ? K * neighborSum / deg : 0;
                next[i] = (1.0 - K) * fx[i] + coupling;
            }
            xKicked = next;
        }
        double sxK = 0, syK = 0;
        for (int i = 0; i < N; i++) { sxK += Math.Cos(xKicked[i]); syK += Math.Sin(xKicked[i]); }
        double recovery = Math.Sqrt(sxK * sxK + syK * syK) / N;

        return new CmlExperimentResult
        {
            Dimension = config.Dimension,
            OrderParameter = R,
            CollectiveFrequency = omegaStar,
            PerturbationRecovery = recovery,
            IsSynchronized = R > 0.9
        };
    }

    private static double Map(double x) => Math.Sin(x);
}
