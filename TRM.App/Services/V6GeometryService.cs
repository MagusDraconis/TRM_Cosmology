using TRM.App.Models;
using TRM.Core.Geometry.V6;

namespace TRM.App.Services;

/// <summary>
/// Service that computes V6 geometry from SAC simulation parameters.
/// Uses TRM.Core V6Pipeline for computation.
/// </summary>
public class V6GeometryService
{
    const int N = 72;
    const double Xi = 1.75;
    const double Dt = 0.05;
    const double K0 = 1.2;
    const int St = 300;
    const int Hd = 4;

    /// <summary>
    /// Runs a SAC simulation and returns V6 geometry for the given seed.
    /// </summary>
    public V6TrajectoryModel ComputeForSeed(int seed, int epochs = 20)
    {
        var K = GenerateGraph(N, seed);
        var km = new double[epochs];
        var dm = new double[epochs];
        var om = new double[epochs];

        for (int e = 1; e <= epochs; e++)
        {
            var h = Simulate(K, N, 0.10, seed + e - 1);
            var R = PhaseCoherence(h, N);
            var Rn = Normalize(R, N);
            var d = DistanceLog(Rn, N);
            K = CouplingUpdate(d, N);
            km[e - 1] = MeanCoupling(K, N);
            dm[e - 1] = MeanDistance(d, N);
            om[e - 1] = CollectiveFrequency(h, N);
        }

        var traj = V6Pipeline.ComputeTrajectory(km, dm, om);

        return new V6TrajectoryModel
        {
            Epochs = epochs,
            I1 = traj.I1,
            I2 = traj.I2,
            ArcLength = traj.ArcLength,
            G22 = traj.G22,
            I1Mean = traj.I1Mean,
            I1CV = traj.I1CV,
            I2Mean = traj.I2Mean,
            I2CV = traj.I2CV,
            TotalArcLength = traj.TotalArcLength,
            G22Mean = traj.G22Mean,
            G22Median = traj.G22Median,
            Eccentricity = traj.Eccentricity,
            AxisRatio = traj.AxisRatio,
            IsArcMonotonic = traj.IsArcMonotonic(),
            IsI1Invariant = traj.IsI1Invariant(),
            IsI2Invariant = traj.IsI2Invariant(),
            IsMetricEuclidean = traj.IsMetricEuclidean()
        };
    }

    // === SAC simulation helpers (from V6 test suite) ===

    static double[,] GenerateGraph(int n, int seed)
    {
        var rng = new Random(seed);
        var adj = new HashSet<int>[n];
        for (int i = 0; i < n; i++) adj[i] = [];
        double p = 6.0 / (n - 1);
        for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
                if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); }
        var visited = new bool[n];
        var components = new List<List<int>>();
        for (int i = 0; i < n; i++)
        {
            if (visited[i]) continue;
            var comp = new List<int>();
            var q = new Queue<int>();
            visited[i] = true; q.Enqueue(i);
            while (q.Count > 0) { int u = q.Dequeue(); comp.Add(u); foreach (int x in adj[u]) if (!visited[x]) { visited[x] = true; q.Enqueue(x); } }
            components.Add(comp);
        }
        for (int i = 1; i < components.Count; i++) { adj[components[i][0]].Add(components[i - 1][0]); adj[components[i - 1][0]].Add(components[i][0]); }
        var K = new double[n, n];
        for (int i = 0; i < n; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; }
        return K;
    }

    static double[][] Simulate(double[,] K, int n, double s, int seed)
    {
        var rng = new Random(seed); var w = new double[n];
        for (int i = 0; i < n; i++) w[i] = 1.0 + s * (rng.NextDouble() - 0.5) * 2.0;
        var th = new double[n];
        for (int i = 0; i < n; i++) th[i] = rng.NextDouble() * 2 * Math.PI;
        int hL = St / Hd + 1; var h = new double[hL][]; h[0] = (double[])th.Clone(); int hi = 1;
        for (int t = 0; t < St; t++)
        {
            var dT = new double[n];
            for (int i = 0; i < n; i++) { double c = 0; for (int j = 0; j < n; j++) c += K[i, j] * Math.Sin(th[j] - th[i]); dT[i] = w[i] + c; }
            for (int i = 0; i < n; i++) th[i] += Dt * dT[i];
            if ((t + 1) % Hd == 0 && hi < hL) h[hi++] = (double[])th.Clone();
        }
        return h;
    }

    static double[,] PhaseCoherence(double[][] h, int n)
    {
        int T = h.Length; var R = new double[n, n];
        for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) { double sc = 0, ss = 0; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); } R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; }
        return R;
    }

    static double[,] Normalize(double[,] R, int n)
    {
        double mn = double.MaxValue; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) if (i != j && R[i, j] < mn) mn = R[i, j];
        double r = 1.0 - mn; if (r < 1e-15) r = 1.0;
        var Rn = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) Rn[i, j] = i == j ? 1.0 : Math.Max(1e-9, (R[i, j] - mn) / r);
        return Rn;
    }

    static double[,] DistanceLog(double[,] R, int n) { var d = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) d[i, j] = i == j ? 0 : -Math.Log(Math.Max(R[i, j], 1e-100)); return d; }

    static double[,] CouplingUpdate(double[,] d, int n) { var K = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) K[i, j] = i == j ? 0 : K0 * Math.Exp(-d[i, j] / Math.Max(Xi, 0.01)); return K; }

    static double MeanCoupling(double[,] K, int n) { double s = 0; int c = 0; for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) { s += K[i, j]; c++; } return c > 0 ? s / c : 0; }

    static double MeanDistance(double[,] d, int n) { double s = 0; int c = 0; for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) { s += d[i, j]; c++; } return c > 0 ? s / c : 0; }

    static double CollectiveFrequency(double[][] h, int n) { int T = h.Length; double s = 0; for (int i = 0; i < n; i++) { for (int t = 1; t < T; t++) s += Math.Abs(h[t][i] - h[t - 1][i]); } return n > 0 ? s / (n * (T - 1) * Dt * Hd) : 0; }
}
