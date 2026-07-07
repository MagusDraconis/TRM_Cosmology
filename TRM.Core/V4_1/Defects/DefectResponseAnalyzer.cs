using TRM.Core.V4_1.Graphs;

namespace TRM.Core.V4_1.Defects;

/// <summary>
/// Measures the static/late-time response to a localized coupling defect.
/// Classification: FRAMEWORK — measurement infrastructure, no claims.
/// </summary>
public static class DefectResponseAnalyzer
{
    /// <summary>
    /// Evolve a diffusion-like field on the graph with a localized defect
    /// (reduced coupling at defect node). Record the steady-state response amplitude
    /// at each node relative to the unperturbed baseline.
    /// </summary>
    public static DefectResponseResult MeasureDefectResponse(
        GraphTopology graph, int dimension, DefectExperimentConfig config)
    {
        int N = graph.NodeCount;
        int defectNode = N / 2; // center node
        double defectFactor = 1.0 - config.DefectStrength;

        // Initialize field: unit source at defect, zero elsewhere.
        var x = new double[N];
        x[defectNode] = 1.0;

        // Evolve diffusion: x_i(t+1) = x_i + K * sum_j A'_ij (x_j - x_i) / deg_i
        // where A' has reduced coupling at defect.
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

        // Compute graph distances from defect.
        var dist = new int[N];
        Array.Fill(dist, -1);
        dist[defectNode] = 0;
        var q = new Queue<int>();
        q.Enqueue(defectNode);
        while (q.Count > 0)
        {
            int u = q.Dequeue();
            foreach (int v in graph.Neighbours(u))
                if (dist[v] == -1) { dist[v] = dist[u] + 1; q.Enqueue(v); }
        }

        // Shell-average the absolute response.
        var byDist = new Dictionary<int, List<double>>();
        for (int i = 0; i < N; i++)
        {
            if (dist[i] <= 0) continue;
            if (!byDist.ContainsKey(dist[i])) byDist[dist[i]] = new List<double>();
            byDist[dist[i]].Add(Math.Abs(x[i]));
        }

        var profile = byDist.OrderBy(kv => kv.Key).Select(kv =>
            new RadialProfileSample(kv.Key, kv.Value.Average(),
                kv.Value.Count > 1 ? Math.Sqrt(kv.Value.Average(v => (v - kv.Value.Average()) * (v - kv.Value.Average()))) : 0,
                kv.Value.Count)).ToList();

        double meanResp = profile.Average(p => p.MeanAmplitude);
        double range = profile.Max(p => p.MeanAmplitude) - profile.Min(p => p.MeanAmplitude);

        return new DefectResponseResult(dimension, profile, meanResp, range);
    }
}
