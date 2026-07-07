using TRM.Core.V4_1.Graphs;

namespace TRM.Core.V4_1.Defects;

/// <summary>
/// Measures superposition linearity of multi-defect responses.
/// Classification: FRAMEWORK — measurement infrastructure, no claims.
/// </summary>
public static class MultiDefectAnalyzer
{
    /// <summary>Compute the static response from a single defect at a given node.</summary>
    private static double[] SingleDefectResponse(
        GraphTopology graph, int defectNode, double defectStrength,
        DefectExperimentConfig config)
    {
        int N = graph.NodeCount;
        double defectFactor = 1.0 - defectStrength;
        var x = new double[N];
        x[defectNode] = 1.0;

        double dt = 0.1;
        for (int t = 0; t < config.TimeSteps; t++)
        {
            var next = (double[])x.Clone();
            for (int i = 0; i < N; i++)
            {
                int deg = graph.Degree(i);
                if (deg == 0) continue;
                double sum = 0;
                foreach (int j in graph.Neighbours(i))
                {
                    double weight = (i == defectNode || j == defectNode) ? defectFactor : 1.0;
                    sum += weight * (x[j] - x[i]);
                }
                next[i] = x[i] + dt * sum / deg;
            }
            x = next;
        }
        return x;
    }

    public static MultiDefectResponseResult MeasureSuperposition(
        GraphTopology graph, int dimension, MultiDefectExperimentConfig config)
    {
        int N = graph.NodeCount;
        int center = N / 2;
        // Two defects symmetrically placed around center.
        int d1 = Math.Max(0, center - (int)config.DefectSeparation / 2);
        int d2 = Math.Min(N - 1, center + (int)config.DefectSeparation / 2);
        if (d1 == d2) d2 = Math.Min(N - 1, d1 + 1);

        // Individual responses.
        var r1 = SingleDefectResponse(graph, d1, config.DefectStrength,
            new DefectExperimentConfig { NodesPerDim = config.NodesPerDim, DefectStrength = config.DefectStrength, TimeSteps = config.TimeSteps });
        var r2 = SingleDefectResponse(graph, d2, config.DefectStrength,
            new DefectExperimentConfig { NodesPerDim = config.NodesPerDim, DefectStrength = config.DefectStrength, TimeSteps = config.TimeSteps });

        // Combined response: both defects active simultaneously.
        var combined = SingleDefectResponse(graph, d1, config.DefectStrength,
            new DefectExperimentConfig { NodesPerDim = config.NodesPerDim, DefectStrength = config.DefectStrength, TimeSteps = config.TimeSteps });
        // Actually run with both defects:
        combined = RunTwoDefects(graph, d1, d2, config);

        // Superposition prediction: linear sum.
        var prediction = new double[N];
        for (int i = 0; i < N; i++) prediction[i] = r1[i] + r2[i];

        // Residuals.
        var residuals = new double[N];
        double sumRes = 0, maxRes = 0, farSum = 0;
        int farCount = 0;
        for (int i = 0; i < N; i++)
        {
            residuals[i] = combined[i] - prediction[i];
            double absR = Math.Abs(residuals[i]);
            sumRes += absR;
            if (absR > maxRes) maxRes = absR;

            // Far-field: nodes beyond defect separation.
            int dist1 = GraphDistance(graph, i, d1);
            int dist2 = GraphDistance(graph, i, d2);
            if (dist1 > config.DefectSeparation && dist2 > config.DefectSeparation)
            {
                farSum += absR;
                farCount++;
            }
        }

        return new MultiDefectResponseResult(
            dimension, N, combined, prediction, residuals,
            sumRes / N, maxRes, farCount > 0 ? farSum / farCount : 0);
    }

    private static double[] RunTwoDefects(
        GraphTopology graph, int d1, int d2, MultiDefectExperimentConfig config)
    {
        int N = graph.NodeCount;
        double defectFactor = 1.0 - config.DefectStrength;
        var x = new double[N];
        x[d1] = 1.0; x[d2] = 1.0;

        double dt = 0.1;
        for (int t = 0; t < config.TimeSteps; t++)
        {
            var next = (double[])x.Clone();
            for (int i = 0; i < N; i++)
            {
                int deg = graph.Degree(i);
                if (deg == 0) continue;
                double sum = 0;
                foreach (int j in graph.Neighbours(i))
                {
                    bool nearDefect = (i == d1 || j == d1 || i == d2 || j == d2);
                    double weight = nearDefect ? defectFactor : 1.0;
                    sum += weight * (x[j] - x[i]);
                }
                next[i] = x[i] + dt * sum / deg;
            }
            x = next;
        }
        return x;
    }

    private static int GraphDistance(GraphTopology g, int a, int b)
    {
        if (a == b) return 0;
        int N = g.NodeCount;
        var dist = new int[N]; Array.Fill(dist, -1);
        dist[a] = 0; var q = new Queue<int>(); q.Enqueue(a);
        while (q.Count > 0)
        {
            int u = q.Dequeue();
            foreach (int v in g.Neighbours(u))
                if (dist[v] == -1) { dist[v] = dist[u] + 1; if (v == b) return dist[v]; q.Enqueue(v); }
        }
        return -1;
    }
}
