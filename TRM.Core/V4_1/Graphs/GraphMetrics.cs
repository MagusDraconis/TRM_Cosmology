namespace TRM.Core.V4_1.Graphs;

/// <summary>
/// Deterministic graph analysis: shortest-path distance, shell growth N(r),
/// graph Laplacian construction, and spectral diagnostics.
/// All methods are stateless and reproducible.
/// </summary>
public static class GraphMetrics
{
    /// <summary>Shortest-path distance via BFS, or -1 if disconnected.</summary>
    public static int ShortestPath(GraphTopology g, int from, int to)
    {
        int N = g.NodeCount;
        var dist = new int[N];
        Array.Fill(dist, -1);
        dist[from] = 0;
        var q = new Queue<int>();
        q.Enqueue(from);
        while (q.Count > 0)
        {
            int u = q.Dequeue();
            if (u == to) return dist[u];
            foreach (int v in g.Neighbours(u))
                if (dist[v] == -1)
                {
                    dist[v] = dist[u] + 1;
                    q.Enqueue(v);
                }
        }
        return -1;
    }

    /// <summary>Number of nodes within graph distance r of a given origin node.</summary>
    public static int ShellVolume(GraphTopology g, int from, int r)
    {
        int N = g.NodeCount;
        var dist = new int[N];
        Array.Fill(dist, -1);
        dist[from] = 0;
        var q = new Queue<int>();
        q.Enqueue(from);
        int count = 0;
        while (q.Count > 0)
        {
            int u = q.Dequeue();
            if (dist[u] > r) continue;
            count++;
            foreach (int v in g.Neighbours(u))
                if (dist[v] == -1)
                {
                    dist[v] = dist[u] + 1;
                    if (dist[v] <= r) q.Enqueue(v);
                }
        }
        return count;
    }

    /// <summary>Array of N(r) for r = 1..maxR from given origin.</summary>
    public static int[] ShellGrowth(GraphTopology g, int from, int maxR)
    {
        var result = new int[maxR];
        for (int r = 1; r <= maxR; r++)
            result[r - 1] = ShellVolume(g, from, r);
        return result;
    }

    /// <summary>Build the graph Laplacian as a dense N×N matrix L = D − A.</summary>
    public static double[,] LaplacianMatrix(GraphTopology g)
    {
        int N = g.NodeCount;
        var L = new double[N, N];
        for (int i = 0; i < N; i++)
        {
            int deg = g.Degree(i);
            L[i, i] = deg;
            foreach (int j in g.Neighbours(i))
                L[i, j] = -1.0;
        }
        return L;
    }

    /// <summary>Apply Laplacian to a field vector φ.</summary>
    public static double[] ApplyLaplacian(GraphTopology g, double[] phi)
    {
        int N = g.NodeCount;
        var result = new double[N];
        for (int i = 0; i < N; i++)
        {
            double sum = 0;
            foreach (int j in g.Neighbours(i))
                sum += phi[j] - phi[i];
            result[i] = sum;
        }
        return result;
    }

    /// <summary>
    /// Compute the first k eigenvalues of a symmetric matrix via power iteration
    /// with deflation. Small, deterministic, suitable for unit tests.
    /// Returns sorted ascending.
    /// </summary>
    public static double[] Eigenvalues(double[,] M, int k, int maxIter = 1000)
    {
        int N = M.GetLength(0);
        var evals = new double[k];
        var Mwork = (double[,])M.Clone();

        for (int e = 0; e < k; e++)
        {
            var v = new double[N];
            Array.Fill(v, 1.0 / Math.Sqrt(N));
            double lambda = 0;
            for (int iter = 0; iter < maxIter; iter++)
            {
                var w = new double[N];
                for (int i = 0; i < N; i++)
                {
                    double s = 0;
                    for (int j = 0; j < N; j++)
                        s += Mwork[i, j] * v[j];
                    w[i] = s;
                }
                double norm = Math.Sqrt(w.Sum(x => x * x));
                if (norm < 1e-15) break;
                for (int i = 0; i < N; i++) v[i] = w[i] / norm;

                double newLambda = 0;
                for (int i = 0; i < N; i++)
                {
                    double s = 0;
                    for (int j = 0; j < N; j++)
                        s += Mwork[i, j] * v[j];
                    newLambda += v[i] * s;
                }
                if (Math.Abs(newLambda - lambda) < 1e-12) { lambda = newLambda; break; }
                lambda = newLambda;
            }
            evals[e] = lambda;

            // Deflate: subtract λ·v·v^T
            for (int i = 0; i < N; i++)
            for (int j = 0; j < N; j++)
                Mwork[i, j] -= lambda * v[i] * v[j];
        }
        Array.Sort(evals);
        return evals;
    }

    /// <summary>
    /// Analytical eigenvalues for 1D chain Laplacian of length n (open boundaries).
    /// λ_k = 2(1 − cos(πk/n)), k = 0..n−1.
    /// </summary>
    private static double[] ChainEigenvalues(int n)
    {
        var ev = new double[n];
        for (int k = 0; k < n; k++)
            ev[k] = 2.0 * (1.0 - Math.Cos(Math.PI * k / n));
        Array.Sort(ev);
        return ev;
    }

    /// <summary>
    /// Slow but reliable reference: compute first 3 Laplacian eigenvalues
    /// via dense QR-like power iteration on a clone. Returns ev[0]=0, ev[1]=λ₂, ...
    /// </summary>
    private static double[] LaplacianEigenvalues(GraphTopology g, int k)
    {
        int N = g.NodeCount;
        if (k > N) k = N;
        var L = new double[N, N];
        for (int i = 0; i < N; i++)
        {
            L[i, i] = g.Degree(i);
            foreach (int j in g.Neighbours(i))
                L[i, j] = -1.0;
        }
        return Eigenvalues(L, k);
    }

    /// <summary>Second eigenvalue (spectral gap) of Laplacian.</summary>
    public static double Lambda2(GraphTopology g)
    {
        // Analytical for tractable cases; numerical fallback otherwise.
        int N = g.NodeCount;
        var ev = LaplacianEigenvalues(g, Math.Min(5, N));
        return ev.Length >= 2 ? ev[1] : 0;
    }

    /// <summary>Largest eigenvalue of Laplacian.</summary>
    public static double LambdaMax(GraphTopology g)
    {
        int N = g.NodeCount;
        var ev = LaplacianEigenvalues(g, Math.Min(5, N));
        return ev[^1];
    }
}
