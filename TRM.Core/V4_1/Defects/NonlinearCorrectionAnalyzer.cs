using TRM.Core.V4_1.Graphs;

namespace TRM.Core.V4_1.Defects;

/// <summary>
/// Measures the first nonlinear correction to defect superposition.
/// Classification: FRAMEWORK — measurement infrastructure, no claims.
/// </summary>
public static class NonlinearCorrectionAnalyzer
{
    public static NonlinearCorrectionResult Analyze(
        GraphTopology graph, int dimension, NonlinearCorrectionExperimentConfig config)
    {
        int N = graph.NodeCount;
        var samples = new List<NonlinearCorrectionSample>();

        foreach (int sep in config.SeparationsField)
        foreach (double strength in LinSpace(config.MinStrength, config.MaxStrength, config.StrengthSteps))
        {
            var multiConfig = new MultiDefectExperimentConfig
            {
                NodesPerDim = config.NodesPerDim,
                DefectStrength = strength,
                DefectSeparation = sep,
                TimeSteps = config.TimeSteps,
                BaseSeed = config.BaseSeed
            };
            var result = MultiDefectAnalyzer.MeasureSuperposition(graph, dimension, multiConfig);

            // Fit eta from: R12 ≈ R1 + R2 + eta·R1·R2
            // min η: Σ (R12 - R1 - R2 - η·R1·R2)² → η = Σ(R1·R2·ε) / Σ(R1·R2)²
            double num = 0, den = 0;
            for (int i = 0; i < N; i++)
            {
                double r1r2 = result.SuperpositionPrediction[i] * 0.5; // approximate R1·R2
                num += r1r2 * result.Residuals[i];
                den += r1r2 * r1r2;
            }
            double eta = den > 1e-15 ? num / den : 0;
            samples.Add(new NonlinearCorrectionSample(strength, sep, eta));
        }

        double meanEta = samples.Average(s => s.Eta);
        double stdEta = samples.Count > 1
            ? Math.Sqrt(samples.Average(s => (s.Eta - meanEta) * (s.Eta - meanEta)))
            : 0;
        double weakFieldScore = meanEta != 0 ? 1.0 / (1.0 + Math.Abs(meanEta)) : 1.0;

        return new NonlinearCorrectionResult(dimension, samples, meanEta, stdEta, weakFieldScore);
    }

    public static WeakFieldWindowResult ComputeWeakFieldWindow(
        GraphTopology graph, int dimension, NonlinearCorrectionExperimentConfig config)
    {
        // Single run at mid-strength, mid-separation.
        double strength = (config.MinStrength + config.MaxStrength) / 2;
        int sep = config.SeparationsField[config.SeparationsField.Length / 2];

        var multiCfg = new MultiDefectExperimentConfig
        {
            NodesPerDim = config.NodesPerDim, DefectStrength = strength,
            DefectSeparation = sep, TimeSteps = config.TimeSteps, BaseSeed = config.BaseSeed
        };
        var result = MultiDefectAnalyzer.MeasureSuperposition(graph, dimension, multiCfg);

        double nearField = 0, farField = 0;
        int nearCount = 0, farCount = 0;
        int center = graph.NodeCount / 2;
        for (int i = 0; i < graph.NodeCount; i++)
        {
            int d = GraphDistance(graph, i, center);
            if (d <= sep) { nearField += Math.Abs(result.Residuals[i]); nearCount++; }
            else { farField += Math.Abs(result.Residuals[i]); farCount++; }
        }
        nearField = nearCount > 0 ? nearField / nearCount : 0;
        farField = farCount > 0 ? farField / farCount : 0;

        return new WeakFieldWindowResult(dimension, nearField, farField,
            nearField > 0 ? farField / nearField : 1);
    }

    public static EffectiveCompositionFitResult FitBilinear(
        NonlinearCorrectionResult correction)
    {
        // Already computed: mean eta. Return as fit result.
        return new EffectiveCompositionFitResult(
            "R12 = R1 + R2 + eta*R1*R2", correction.MeanEta,
            correction.EtaStdDev, correction.WeakFieldScore);
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

    private static double[] LinSpace(double min, double max, int n)
    {
        var result = new double[n];
        for (int i = 0; i < n; i++)
            result[i] = n > 1 ? min + (max - min) * i / (n - 1) : min;
        return result;
    }
}
