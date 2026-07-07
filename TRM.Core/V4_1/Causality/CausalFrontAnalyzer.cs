using TRM.Core.V4_1.Graphs;

namespace TRM.Core.V4_1.Causality;

/// <summary>
/// Measures front propagation times from a source perturbation on graph topologies.
/// Classification: FRAMEWORK — measurement infrastructure, no claims.
/// </summary>
public static class CausalFrontAnalyzer
{
    /// <summary>
    /// Simulate a perturbation spreading from source node and record
    /// first-arrival step at each reachable node.
    /// Uses deterministic diffusion on the graph: x_i(t+1) = x_i + K * sum_j (x_j - x_i) / deg_i.
    /// Arrival detected when |x_i| exceeds threshold.
    /// </summary>
    public static FrontPropagationResult MeasureFront(
        GraphTopology graph, int dimension, int sourceNode, double K, int maxSteps, int seed)
    {
        int N = graph.NodeCount;
        var x = new double[N];
        x[sourceNode] = 1.0; // perturbation at source

        var arrivalStep = new int[N];
        Array.Fill(arrivalStep, -1);
        arrivalStep[sourceNode] = 0;
        int arrived = 1;
        double threshold = 0.01;

        for (int t = 0; t < maxSteps && arrived < N; t++)
        {
            var next = (double[])x.Clone();
            for (int i = 0; i < N; i++)
            {
                if (arrivalStep[i] >= 0) continue;
                int deg = graph.Degree(i);
                if (deg == 0) continue;
                double neighborSum = 0;
                foreach (int j in graph.Neighbours(i))
                    neighborSum += x[j];
                next[i] = x[i] + K * (neighborSum / deg - x[i]);
                if (Math.Abs(next[i]) > threshold && arrivalStep[i] < 0)
                {
                    arrivalStep[i] = t + 1;
                    arrived++;
                }
            }
            x = next;
        }

        // Compute graph distances from source via BFS.
        var graphDist = new int[N];
        Array.Fill(graphDist, -1);
        graphDist[sourceNode] = 0;
        var q = new Queue<int>();
        q.Enqueue(sourceNode);
        while (q.Count > 0)
        {
            int u = q.Dequeue();
            foreach (int v in graph.Neighbours(u))
                if (graphDist[v] == -1) { graphDist[v] = graphDist[u] + 1; q.Enqueue(v); }
        }

        var arrivals = new List<FrontArrivalSample>();
        var speeds = new List<double>();
        for (int i = 0; i < N; i++)
        {
            if (arrivalStep[i] > 0 && graphDist[i] > 0)
            {
                arrivals.Add(new FrontArrivalSample(i, graphDist[i], arrivalStep[i]));
                speeds.Add((double)graphDist[i] / arrivalStep[i]);
            }
        }

        double meanSpeed = speeds.Count > 0 ? speeds.Average() : 0;
        double stdDev = speeds.Count > 1
            ? Math.Sqrt(speeds.Average(s => (s - meanSpeed) * (s - meanSpeed)))
            : 0;

        // Anisotropy: spread of speeds grouped by graph distance shell.
        double anisotropy = 0;
        var byDist = arrivals.GroupBy(a => a.GraphDistance).ToList();
        if (byDist.Count > 1)
        {
            double[] shellMeans = byDist.Select(g => g.Average(a => (double)a.GraphDistance / a.ArrivalStep)).ToArray();
            anisotropy = shellMeans.Max() - shellMeans.Min();
        }

        return new FrontPropagationResult(dimension, arrivals, meanSpeed, stdDev, anisotropy);
    }
}
