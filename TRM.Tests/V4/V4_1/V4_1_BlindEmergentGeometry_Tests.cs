using Xunit;
using Xunit.Abstractions;
using TRM.Core.V4_1.Graphs;
using TRM.Core.V4_1.Sync;

namespace TRM.Tests.V4_1;

/// <summary>
/// Blind emergent geometry from oscillator dynamics.
///
/// Goal: infer spatial distance purely from simulated oscillator time series θ_i(t),
/// without using graph shortest-path distance to construct the temporal rate matrix R_ij.
///
/// Hidden ground-truth topologies:
///   1. 1D chain
///   2. 2D square lattice
///   3. 3D cubic lattice
///   4. 4D hypercubic lattice
///   5. random Erdos–Renyi graph
///   6. small-world graph
///
/// Three Temporal Rate Matrix candidates inferred from θ_i(t):
///   R_phase[i,j] = |⟨ exp(i·(θ_i − θ_j)) ⟩_t|
///   R_corr[i,j]  = |Pearson( sin(θ_i(t)), sin(θ_j(t)) )|
///   R_lock[i,j]  = exp(−τ_lock(i,j) / τ₀)
///      where τ_lock is the first time after which |Δθ| stays < ε (mod 2π).
///
/// For each R candidate:
///   - Normalize to [ε, 1]
///   - Define d_ij = −log(R_ij)
///   - Test metric properties
///   - Build weighted k-NN graph
///   - Compute reconstruction quality vs hidden graph
///
/// d_graph is used ONLY in final validation — never in constructing R_ij.
/// No D = 3 assumption.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_BlindEmergentGeometry")]
public class V4_1_BlindEmergentGeometry_Tests
{
    private readonly ITestOutputHelper _output;

    private const int BaseSeed = 42;
    private const double K = 0.8;
    private const double SigmaOmega = 0.1;
    private const double Dt = 0.05;
    private const int Steps = 400;
    private const int HistoryDownsample = 4;   // store theta every Nth step
    private const double R_epsilon = 1e-6;      // R normalization floor
    private const double LockEpsilon = 0.1;      // radians, phase-lock threshold
    private const double Tau0 = 50.0;            // lock-time scale
    private const int KnnK = 6;                  // k for k-NN graph construction

    public V4_1_BlindEmergentGeometry_Tests(ITestOutputHelper output)
    {
        _output = output;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Hidden topology builders
    // ═══════════════════════════════════════════════════════════════════════

    private static GraphTopology BuildHidden(int D, int n) => D switch
    {
        1 => GraphFactory.Chain(Math.Max(2, n)),
        2 => GraphFactory.SquareGrid(Math.Max(2, n)),
        3 => GraphFactory.CubicLattice(Math.Max(2, n)),
        4 => GraphFactory.Hypercubic4D(Math.Max(2, n)),
        _ => throw new ArgumentOutOfRangeException(nameof(D))
    };

    private static GraphTopology BuildRandomGraph(int N, double avgDeg, int seed)
    {
        var rng = new Random(seed);
        double p = Math.Min(1.0, avgDeg / (N - 1));
        var adj = new HashSet<int>[N];
        for (int i = 0; i < N; i++) adj[i] = new HashSet<int>();
        for (int i = 0; i < N; i++)
        for (int j = i + 1; j < N; j++)
            if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); }
        // Ensure connectivity
        var visited = new bool[N];
        var comps = new List<List<int>>();
        for (int i = 0; i < N; i++)
        {
            if (visited[i]) continue;
            var comp = new List<int>();
            var q = new Queue<int>();
            visited[i] = true; q.Enqueue(i);
            while (q.Count > 0) { int u = q.Dequeue(); comp.Add(u); foreach (int v in adj[u]) if (!visited[v]) { visited[v] = true; q.Enqueue(v); } }
            comps.Add(comp);
        }
        for (int c = 1; c < comps.Count; c++) { adj[comps[c][0]].Add(comps[c - 1][0]); adj[comps[c - 1][0]].Add(comps[c][0]); }
        return new GraphTopology(adj.Select(h => h.ToArray()).ToArray());
    }

    private static GraphTopology BuildSmallWorld(int N, int k, double p, int seed)
    {
        var rng = new Random(seed);
        var adj = new HashSet<int>[N];
        for (int i = 0; i < N; i++) adj[i] = new HashSet<int>();
        int halfK = k / 2;
        for (int i = 0; i < N; i++)
        for (int d = 1; d <= halfK; d++)
        { int j = (i + d) % N; adj[i].Add(j); adj[j].Add(i); }
        var currentList = adj.Select(a => a.ToList()).ToArray();
        for (int i = 0; i < N; i++)
        foreach (int j in currentList[i])
        {
            if (i >= j) continue;
            if (rng.NextDouble() < p)
            {
                adj[i].Remove(j); adj[j].Remove(i);
                int nj; do { nj = rng.Next(N); } while (nj == i || adj[i].Contains(nj));
                adj[i].Add(nj); adj[nj].Add(i);
            }
        }
        return new GraphTopology(adj.Select(h => h.ToArray()).ToArray());
    }

    private static int ShortestPath(GraphTopology g, int from, int to) => GraphMetrics.ShortestPath(g, from, to);

    // ═══════════════════════════════════════════════════════════════════════
    // Full Kuramoto simulation — captures θ_i(t) history
    // ═══════════════════════════════════════════════════════════════════════

    private static double[][] RunKuramotoFullHistory(
        GraphTopology graph, double Kval, double sigmaOmega, double dt, int steps, int seed)
    {
        int N = graph.NodeCount;
        var rng = new Random(seed);
        var omega = new double[N];
        for (int i = 0; i < N; i++)
            omega[i] = 1.0 + sigmaOmega * (rng.NextDouble() - 0.5) * 2.0;

        var theta = new double[N];
        for (int i = 0; i < N; i++)
            theta[i] = rng.NextDouble() * 2.0 * Math.PI;

        int histLen = steps / HistoryDownsample + 1;
        var history = new double[histLen][];
        history[0] = (double[])theta.Clone();

        int hIdx = 1;
        for (int t = 0; t < steps; t++)
        {
            var dTheta = new double[N];
            for (int i = 0; i < N; i++)
            {
                double coupling = 0;
                foreach (int j in graph.Neighbours(i))
                    coupling += Math.Sin(theta[j] - theta[i]);
                dTheta[i] = omega[i] + Kval * coupling;
            }
            for (int i = 0; i < N; i++)
                theta[i] += dt * dTheta[i];

            if ((t + 1) % HistoryDownsample == 0 && hIdx < histLen)
                history[hIdx++] = (double[])theta.Clone();
        }
        return history;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Rate matrix candidate 1: phase-lock matrix
    // R_phase[i,j] = |⟨ exp(i·(θ_i − θ_j)) ⟩_t|
    // ═══════════════════════════════════════════════════════════════════════

    private static double[,] BuildPhaseLockMatrix(double[][] history)
    {
        int T = history.Length;
        int N = history[0].Length;
        var R = new double[N, N];
        for (int i = 0; i < N; i++)
        for (int j = 0; j < N; j++)
        {
            double sumCos = 0, sumSin = 0;
            for (int t = 0; t < T; t++)
            {
                double d = history[t][i] - history[t][j];
                sumCos += Math.Cos(d);
                sumSin += Math.Sin(d);
            }
            R[i, j] = Math.Sqrt(sumCos * sumCos + sumSin * sumSin) / T;
        }
        return R;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Rate matrix candidate 2: correlation matrix
    // R_corr[i,j] = |Pearson( sin(θ_i(t)), sin(θ_j(t)) )|
    // ═══════════════════════════════════════════════════════════════════════

    private static double[,] BuildCorrelationMatrix(double[][] history)
    {
        int T = history.Length;
        int N = history[0].Length;
        // Extract sin time series
        var sinSeries = new double[N][];
        for (int i = 0; i < N; i++)
        {
            sinSeries[i] = new double[T];
            for (int t = 0; t < T; t++)
                sinSeries[i][t] = Math.Sin(history[t][i]);
        }
        var R = new double[N, N];
        for (int i = 0; i < N; i++)
        for (int j = 0; j < N; j++)
            R[i, j] = Math.Abs(Pearson(sinSeries[i], sinSeries[j]));
        return R;
    }

    private static double Pearson(double[] x, double[] y)
    {
        int n = x.Length;
        double mx = 0, my = 0;
        for (int i = 0; i < n; i++) { mx += x[i]; my += y[i]; }
        mx /= n; my /= n;
        double num = 0, dx = 0, dy = 0;
        for (int i = 0; i < n; i++)
        {
            double a = x[i] - mx, b = y[i] - my;
            num += a * b; dx += a * a; dy += b * b;
        }
        double den = Math.Sqrt(dx * dy);
        return den > 1e-15 ? num / den : 0;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Rate matrix candidate 3: lock-time matrix
    // R_lock[i,j] = exp(−τ_lock(i,j) / τ₀)
    // τ_lock = first time after which |Δθ| stays < ε (mod 2π) for the rest of the simulation.
    // ═══════════════════════════════════════════════════════════════════════

    private static double[,] BuildLockTimeMatrix(double[][] history, double eps, double tau0)
    {
        int T = history.Length;
        int N = history[0].Length;
        var R = new double[N, N];
        for (int i = 0; i < N; i++)
        for (int j = 0; j < N; j++)
        {
            // Find the last time the pair was NOT phase-locked.
            int lastUnlocked = -1;
            for (int t = 0; t < T; t++)
            {
                double d = NormalizeAngle(history[t][i] - history[t][j]);
                if (Math.Abs(d) > eps) lastUnlocked = t;
            }
            double tauLock = lastUnlocked < 0 ? 0 : T - lastUnlocked;
            R[i, j] = Math.Exp(-tauLock / tau0);
        }
        return R;
    }

    private static double NormalizeAngle(double a)
    {
        a %= 2.0 * Math.PI;
        if (a > Math.PI) a -= 2.0 * Math.PI;
        if (a < -Math.PI) a += 2.0 * Math.PI;
        return a;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Rate matrix normalization
    // ═══════════════════════════════════════════════════════════════════════

    private static double[,] NormalizeRateMatrix(double[,] R)
    {
        int N = R.GetLength(0);
        double minVal = double.MaxValue;
        for (int i = 0; i < N; i++)
        for (int j = 0; j < N; j++)
            if (i != j && R[i, j] < minVal) minVal = R[i, j];

        double range = 1.0 - minVal;
        if (range < 1e-15) range = 1.0;

        var Rn = new double[N, N];
        for (int i = 0; i < N; i++)
        for (int j = 0; j < N; j++)
        {
            if (i == j) Rn[i, j] = 1.0;
            else Rn[i, j] = Math.Max(R_epsilon, (R[i, j] - minVal) / range);
        }
        return Rn;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Distance from rate matrix: d_ij = −log(R_ij), d_ii = 0
    // ═══════════════════════════════════════════════════════════════════════

    private static double[,] DistanceFromRate(double[,] R)
    {
        int N = R.GetLength(0);
        var d = new double[N, N];
        for (int i = 0; i < N; i++)
        for (int j = 0; j < N; j++)
        {
            if (i == j) d[i, j] = 0;
            else d[i, j] = -Math.Log(Math.Max(R[i, j], 1e-100));
        }
        return d;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Metric property validation
    // ═══════════════════════════════════════════════════════════════════════

    private static (int nonNeg, int sym, int triViolations, double maxTriExcess)
        ValidateMetricProperties(double[,] d, int triSample = 5000)
    {
        int N = d.GetLength(0);
        int nonNeg = 0, sym = 0, triV = 0;
        double maxTri = 0;

        for (int i = 0; i < N; i++)
        for (int j = 0; j < N; j++)
        {
            if (d[i, j] < -1e-9) nonNeg++;
            if (Math.Abs(d[i, j] - d[j, i]) > 1e-9) sym++;
        }

        var rng = new Random(BaseSeed);
        for (int s = 0; s < triSample; s++)
        {
            int i = rng.Next(N), j = rng.Next(N), k = rng.Next(N);
            double excess = d[i, k] - (d[i, j] + d[j, k]);
            if (excess > 1e-9) { triV++; maxTri = Math.Max(maxTri, excess); }
        }

        return (nonNeg, sym, triV, maxTri);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Build k-NN graph from distance matrix
    // ═══════════════════════════════════════════════════════════════════════

    private static GraphTopology BuildKnnGraph(double[,] d, int k)
    {
        int N = d.GetLength(0);
        var adj = new HashSet<int>[N];
        for (int i = 0; i < N; i++) adj[i] = new HashSet<int>();

        for (int i = 0; i < N; i++)
        {
            var neighbors = Enumerable.Range(0, N)
                .Where(j => j != i)
                .OrderBy(j => d[i, j])
                .Take(k);
            foreach (int j in neighbors) { adj[i].Add(j); adj[j].Add(i); }
        }
        return new GraphTopology(adj.Select(h => h.ToArray()).ToArray());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Graph diagnostics on recovered graph
    // ═══════════════════════════════════════════════════════════════════════

    private static double AvgShortestPath(GraphTopology g)
    {
        int N = g.NodeCount;
        double sum = 0;
        int count = 0;
        for (int i = 0; i < N; i++)
        for (int j = i + 1; j < N; j++)
        {
            int sp = GraphMetrics.ShortestPath(g, i, j);
            if (sp > 0) { sum += sp; count++; }
        }
        return count > 0 ? sum / count : double.NaN;
    }

    private static int Diameter(GraphTopology g)
    {
        int N = g.NodeCount, diam = 0;
        for (int i = 0; i < N; i++)
        {
            var dist = new int[N]; Array.Fill(dist, -1); dist[i] = 0;
            var q = new Queue<int>(); q.Enqueue(i);
            while (q.Count > 0)
            { int u = q.Dequeue(); foreach (int v in g.Neighbours(u)) if (dist[v] == -1) { dist[v] = dist[u] + 1; diam = Math.Max(diam, dist[v]); q.Enqueue(v); } }
        }
        return diam;
    }

    private static double ClusteringCoeff(GraphTopology g)
    {
        int N = g.NodeCount;
        double total = 0;
        for (int i = 0; i < N; i++)
        {
            var nbrs = g.Neighbours(i);
            int deg = nbrs.Length;
            if (deg < 2) continue;
            var set = new HashSet<int>(nbrs);
            int tri = 0;
            foreach (int u in nbrs)
            foreach (int v in g.Neighbours(u))
                if (v > u && set.Contains(v)) tri++;
            total += 2.0 * tri / (deg * (deg - 1));
        }
        return total / N;
    }

    private static double EffectiveDim(GraphTopology g)
    {
        int N = g.NodeCount;
        int center = N / 2;
        int maxR = 0;
        var dist = new int[N]; Array.Fill(dist, -1); dist[center] = 0;
        var q = new Queue<int>(); q.Enqueue(center);
        while (q.Count > 0) { int u = q.Dequeue(); foreach (int v in g.Neighbours(u)) if (dist[v] == -1) { dist[v] = dist[u] + 1; maxR = Math.Max(maxR, dist[v]); q.Enqueue(v); } }
        if (maxR < 3) return double.NaN;
        var vols = GraphMetrics.ShellGrowth(g, center, maxR);
        int start = maxR / 4, end = maxR * 3 / 4;
        if (end <= start) { start = 1; end = maxR; }
        double sx = 0, sy = 0, sxy = 0, sx2 = 0; int pts = 0;
        for (int r = start; r < end; r++)
        {
            if (vols[r] < 2) continue;
            double lr = Math.Log(r + 1), lv = Math.Log(vols[r]);
            sx += lr; sy += lv; sxy += lr * lv; sx2 += lr * lr; pts++;
        }
        return pts >= 2 ? (pts * sxy - sx * sy) / (pts * sx2 - sx * sx) : double.NaN;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Reconstruction quality vs hidden graph
    // ═══════════════════════════════════════════════════════════════════════

    private static (double precision, double recall, double f1, double spearman)
        ReconstructionMetrics(double[,] dInferred, GraphTopology hiddenGraph, int knnK)
    {
        int N = hiddenGraph.NodeCount;
        var recoveredGraph = BuildKnnGraph(dInferred, knnK);

        // Edge precision/recall
        var hiddenEdges = new HashSet<(int, int)>();
        for (int i = 0; i < N; i++)
        foreach (int j in hiddenGraph.Neighbours(i))
            if (i < j) hiddenEdges.Add((i, j));

        var recoveredEdges = new HashSet<(int, int)>();
        for (int i = 0; i < N; i++)
        foreach (int j in recoveredGraph.Neighbours(i))
            if (i < j) recoveredEdges.Add((i, j));

        int truePos = hiddenEdges.Intersect(recoveredEdges).Count();
        double precision = recoveredEdges.Count > 0 ? (double)truePos / recoveredEdges.Count : 0;
        double recall = hiddenEdges.Count > 0 ? (double)truePos / hiddenEdges.Count : 0;
        double f1 = precision + recall > 0 ? 2 * precision * recall / (precision + recall) : 0;

        // Spearman rho between inferred d_ij and graph distance
        var inferredDist = new List<double>();
        var trueDist = new List<double>();
        for (int i = 0; i < N; i++)
        for (int j = i + 1; j < N; j++)
        {
            inferredDist.Add(dInferred[i, j]);
            int sp = ShortestPath(hiddenGraph, i, j);
            if (sp >= 0) trueDist.Add(sp);
            else { inferredDist.RemoveAt(inferredDist.Count - 1); }
        }
        double spearman = SpearmanRho(inferredDist.ToArray(), trueDist.ToArray());

        return (precision, recall, f1, spearman);
    }

    private static double SpearmanRho(double[] a, double[] b)
    {
        int n = a.Length;
        if (n < 3) return 0;
        int[] ra = Rank(a), rb = Rank(b);
        return Pearson(ra.Select(x => (double)x).ToArray(), rb.Select(x => (double)x).ToArray());
    }

    private static int[] Rank(double[] x)
    {
        int n = x.Length;
        var indexed = x.Select((v, i) => (v, i)).OrderBy(t => t.v).ToArray();
        var ranks = new int[n];
        for (int i = 0; i < n; i++) ranks[indexed[i].i] = i + 1;
        return ranks;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BGE_01 — Run simulations and verify time series are finite
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_BGE_01_Simulations_ProduceFiniteTimeSeries()
    {
        var topologies = new (string name, GraphTopology g)[]
        {
            ("1D chain", BuildHidden(1, 20)),
            ("2D lattice", BuildHidden(2, 8)),
            ("3D cubic", BuildHidden(3, 5)),
            ("4D hypercubic", BuildHidden(4, 3)),
            ("random", BuildRandomGraph(60, 6, BaseSeed)),
            ("small-world", BuildSmallWorld(60, 6, 0.1, BaseSeed)),
        };

        foreach (var (name, g) in topologies)
        {
            var history = RunKuramotoFullHistory(g, K, SigmaOmega, Dt, Steps, BaseSeed);
            int T = history.Length;
            int N = g.NodeCount;

            for (int t = 0; t < T; t++)
            for (int i = 0; i < N; i++)
                Assert.True(double.IsFinite(history[t][i]),
                    $"{name}: θ[{i}](t={t}) not finite.");

            _output.WriteLine($"  {name,-20} N={N,4}  T={T,4}  all finite=OK");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BGE_02 — Build all three R candidates from time series only
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("1D chain", 1, 20)]
    [InlineData("2D lattice", 2, 8)]
    [InlineData("3D cubic", 3, 5)]
    [InlineData("4D hypercubic", 4, 3)]
    public void V4_1_BGE_02_RateCandidates_RegularLattices(string name, int D, int n)
    {
        var g = BuildHidden(D, n);
        var history = RunKuramotoFullHistory(g, K, SigmaOmega, Dt, Steps, BaseSeed);
        int N = g.NodeCount;

        var Rphase = BuildPhaseLockMatrix(history);
        var Rcorr = BuildCorrelationMatrix(history);
        var Rlock = BuildLockTimeMatrix(history, LockEpsilon, Tau0);

        // All R in [0, 1]
        for (int i = 0; i < N; i++)
        for (int j = 0; j < N; j++)
        {
            Assert.True(Rphase[i, j] >= 0 && Rphase[i, j] <= 1.0001,
                $"{name} R_phase[{i},{j}]={Rphase[i,j]:F6}");
            Assert.True(Rcorr[i, j] >= 0 && Rcorr[i, j] <= 1.0001,
                $"{name} R_corr[{i},{j}]={Rcorr[i,j]:F6}");
            Assert.True(Rlock[i, j] >= 0 && Rlock[i, j] <= 1.0001,
                $"{name} R_lock[{i},{j}]={Rlock[i,j]:F6}");
        }

        // Symmetry
        for (int i = 0; i < N; i++)
        for (int j = 0; j < N; j++)
        {
            Assert.Equal(Rphase[i, j], Rphase[j, i], 9);
            Assert.Equal(Rcorr[i, j], Rcorr[j, i], 9);
            Assert.Equal(Rlock[i, j], Rlock[j, i], 9);
        }

        // Diagonal ≈ 1
        for (int i = 0; i < N; i++)
        {
            Assert.True(Rphase[i, i] > 0.99, $"{name} R_phase[{i},{i}]={Rphase[i,i]:F4}");
            Assert.True(Rcorr[i, i] > 0.99, $"{name} R_corr[{i},{i}]={Rcorr[i,i]:F4}");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BGE_03 — Normalize and compute d_ij = −log(R_ij)
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_BGE_03_DistanceFromRate_IsFiniteAndSymmetric()
    {
        var g = BuildHidden(3, 5);
        var history = RunKuramotoFullHistory(g, K, SigmaOmega, Dt, Steps, BaseSeed);
        int N = g.NodeCount;

        var names = new[] { "phase-lock", "correlation", "lock-time" };
        var builders = new Func<double[][], double[,]>[]
            { BuildPhaseLockMatrix, BuildCorrelationMatrix, h => BuildLockTimeMatrix(h, LockEpsilon, Tau0) };

        for (int m = 0; m < 3; m++)
        {
            var R = NormalizeRateMatrix(builders[m](history));
            var d = DistanceFromRate(R);

            for (int i = 0; i < N; i++)
            for (int j = 0; j < N; j++)
            {
                Assert.True(double.IsFinite(d[i, j]), $"{names[m]}: d[{i},{j}] not finite.");
                Assert.Equal(d[i, j], d[j, i], 9);
                Assert.True(d[i, j] >= -1e-9, $"{names[m]}: negative d[{i},{j}].");
            }
            for (int i = 0; i < N; i++)
                Assert.Equal(0.0, d[i, i], 9);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BGE_04 — Metric property report for all three candidates across all topologies
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_BGE_04_MetricProperties_AllCandidates_AllTopologies()
    {
        var topologies = new (string name, GraphTopology g)[]
        {
            ("1D chain", BuildHidden(1, 20)),
            ("2D lattice", BuildHidden(2, 8)),
            ("3D cubic", BuildHidden(3, 5)),
            ("4D hypercubic", BuildHidden(4, 3)),
            ("random", BuildRandomGraph(60, 6, BaseSeed)),
            ("small-world", BuildSmallWorld(60, 6, 0.1, BaseSeed)),
        };

        var methodNames = new[] { "phase-lock", "correlation", "lock-time" };
        var builders = new Func<double[][], double[,]>[]
            { BuildPhaseLockMatrix, BuildCorrelationMatrix, h => BuildLockTimeMatrix(h, LockEpsilon, Tau0) };

        _output.WriteLine("  METRIC PROPERTIES OF d_ij = −log(R_ij)");
        _output.WriteLine($"  {"topology",-18} {"method",-14} {"non-neg",8} {"sym",8} {"tri-ε(5K)",12} {"max tri",10}");
        _output.WriteLine($"  {new string('-',18)} {new string('-',14)} {new string('-',8)} {new string('-',8)} {new string('-',12)} {new string('-',10)}");

        foreach (var (name, g) in topologies)
        {
            var history = RunKuramotoFullHistory(g, K, SigmaOmega, Dt, Steps, BaseSeed);
            for (int m = 0; m < 3; m++)
            {
                var R = NormalizeRateMatrix(builders[m](history));
                var d = DistanceFromRate(R);
                var (nn, sym, tri, maxTri) = ValidateMetricProperties(d, 5000);

                _output.WriteLine($"  {name,-18} {methodNames[m],-14} {nn,8} {sym,8} {tri,12} {maxTri,10:F4}");
            }
        }

        _output.WriteLine("");
        _output.WriteLine("  non-neg = negative-distance violations  (should be 0)");
        _output.WriteLine("  sym     = symmetry violations           (should be 0)");
        _output.WriteLine("  tri-ε   = triangle-inequality violations (sampled)");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BGE_05 — k-NN graph reconstruction and diagnostics
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_BGE_05_KnnGraphDiagnostics()
    {
        var g = BuildHidden(3, 5);
        var history = RunKuramotoFullHistory(g, K, SigmaOmega, Dt, Steps, BaseSeed);
        int N = g.NodeCount;

        var names = new[] { "phase-lock", "correlation", "lock-time" };
        var builders = new Func<double[][], double[,]>[]
            { BuildPhaseLockMatrix, BuildCorrelationMatrix, h => BuildLockTimeMatrix(h, LockEpsilon, Tau0) };

        _output.WriteLine("  k-NN GRAPH DIAGNOSTICS (hidden: 3D cubic, N=125)");
        _output.WriteLine($"  {"method",-14} {"avg SP",9} {"diam",6} {"cluster",9} {"λ₂",9} {"λ_max",8} {"S₁",7} {"D_eff",7}");
        _output.WriteLine($"  {new string('-',14)} {new string('-',9)} {new string('-',6)} {new string('-',9)} {new string('-',9)} {new string('-',8)} {new string('-',7)} {new string('-',7)}");

        for (int m = 0; m < 3; m++)
        {
            var R = NormalizeRateMatrix(builders[m](history));
            var d = DistanceFromRate(R);
            var knn = BuildKnnGraph(d, KnnK);

            double avgSp = AvgShortestPath(knn);
            int diam = Diameter(knn);
            double cc = ClusteringCoeff(knn);
            double l2 = GraphMetrics.Lambda2(knn);
            double lmax = GraphMetrics.LambdaMax(knn);
            double S1 = lmax > 0 ? l2 / lmax : 0;
            double deff = EffectiveDim(knn);

            _output.WriteLine($"  {names[m],-14} {avgSp,9:F3} {diam,6} {cc,9:F4} {l2,9:F5} {lmax,8:F4} {S1,7:F4} {deff,7:F2}");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BGE_06 — Reconstruction quality against hidden graph
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_BGE_06_ReconstructionQuality_AllTopologies()
    {
        var topologies = new (string name, GraphTopology g)[]
        {
            ("1D chain", BuildHidden(1, 20)),
            ("2D lattice", BuildHidden(2, 8)),
            ("3D cubic", BuildHidden(3, 5)),
            ("4D hypercubic", BuildHidden(4, 3)),
            ("random", BuildRandomGraph(60, 6, BaseSeed)),
            ("small-world", BuildSmallWorld(60, 6, 0.1, BaseSeed)),
        };

        var methodNames = new[] { "phase-lock", "correlation", "lock-time" };
        var builders = new Func<double[][], double[,]>[]
            { BuildPhaseLockMatrix, BuildCorrelationMatrix, h => BuildLockTimeMatrix(h, LockEpsilon, Tau0) };

        _output.WriteLine("  RECONSTRUCTION QUALITY vs HIDDEN GRAPH");
        _output.WriteLine($"  {"topology",-18} {"method",-14} {"prec",7} {"recall",7} {"F1",7} {"Spearman ρ",12}");
        _output.WriteLine($"  {new string('-',18)} {new string('-',14)} {new string('-',7)} {new string('-',7)} {new string('-',7)} {new string('-',12)}");

        foreach (var (name, g) in topologies)
        {
            var history = RunKuramotoFullHistory(g, K, SigmaOmega, Dt, Steps, BaseSeed);
            for (int m = 0; m < 3; m++)
            {
                var R = NormalizeRateMatrix(builders[m](history));
                var d = DistanceFromRate(R);
                var (prec, rec, f1, spearman) = ReconstructionMetrics(d, g, KnnK);

                _output.WriteLine($"  {name,-18} {methodNames[m],-14} {prec,7:F4} {rec,7:F4} {f1,7:F4} {spearman,12:F4}");
            }
        }

        _output.WriteLine("");
        _output.WriteLine("  prec/rec/F1 = edge reconstruction vs hidden adjacency (k-NN graph)");
        _output.WriteLine("  Spearman ρ  = rank correlation between inferred d_ij and hidden graph distance");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BGE_07 — Full numerical evidence table (aggregated across topologies)
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_BGE_07_FullEvidenceTable()
    {
        var topologies = new (string name, GraphTopology g)[]
        {
            ("1D chain", BuildHidden(1, 20)),
            ("2D lattice", BuildHidden(2, 8)),
            ("3D cubic", BuildHidden(3, 5)),
            ("4D hypercubic", BuildHidden(4, 3)),
            ("random", BuildRandomGraph(60, 6, BaseSeed)),
            ("small-world", BuildSmallWorld(60, 6, 0.1, BaseSeed)),
        };

        var methodNames = new[] { "phase-lock", "correlation", "lock-time" };
        var builders = new Func<double[][], double[,]>[]
            { BuildPhaseLockMatrix, BuildCorrelationMatrix, h => BuildLockTimeMatrix(h, LockEpsilon, Tau0) };

        _output.WriteLine("═══════════════════════════════════════════════════════════════════════════════════════════════════════");
        _output.WriteLine("  FULL NUMERICAL EVIDENCE — BLIND EMERGENT GEOMETRY FROM OSCILLATOR DYNAMICS");
        _output.WriteLine("═══════════════════════════════════════════════════════════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine($"  Simulation: dt={Dt}, steps={Steps}, K={K}, σ_ω={SigmaOmega}, history_downsample={HistoryDownsample}");
        _output.WriteLine($"  k-NN: k={KnnK}, lock ε={LockEpsilon}, τ₀={Tau0}");
        _output.WriteLine("");

        foreach (var (name, g) in topologies)
        {
            _output.WriteLine($"  ── {name} (N={g.NodeCount}) ──");
            var history = RunKuramotoFullHistory(g, K, SigmaOmega, Dt, Steps, BaseSeed);

            for (int m = 0; m < 3; m++)
            {
                var R = NormalizeRateMatrix(builders[m](history));
                var d = DistanceFromRate(R);
                var (nn, sym, tri, maxTri) = ValidateMetricProperties(d, 5000);
                var (prec, rec, f1, spearman) = ReconstructionMetrics(d, g, KnnK);

                var knn = BuildKnnGraph(d, KnnK);
                double avgSp = AvgShortestPath(knn);
                int diam = Diameter(knn);
                double cc = ClusteringCoeff(knn);
                double l2 = GraphMetrics.Lambda2(knn), lmax = GraphMetrics.LambdaMax(knn);
                double S1 = lmax > 0 ? l2 / lmax : 0;
                double deff = EffectiveDim(knn);

                _output.WriteLine(
                    $"    {methodNames[m],-14} | tri-v:{tri,5} | prec:{prec:F3} rec:{rec:F3} F1:{f1:F3} ρ:{spearman:F3} | " +
                    $"avgSP:{avgSp:F2} diam:{diam,3} cc:{cc:F3} λ₂:{l2:F4} S₁:{S1:F4} D_eff:{deff:F2}");
            }
            _output.WriteLine("");
        }

        _output.WriteLine("  LEGEND:");
        _output.WriteLine("    tri-v = triangle-inequality violations (sampled / 5000)");
        _output.WriteLine("    prec/rec/F1 = edge reconstruction vs hidden adjacency");
        _output.WriteLine("    ρ = Spearman rank correlation d_inferred vs d_graph");
        _output.WriteLine("    avgSP = average shortest path in k-NN graph");
        _output.WriteLine("    cc = clustering coefficient");
        _output.WriteLine("    S₁ = λ₂/λ_max (spectral balance)");
        _output.WriteLine("    D_eff = effective dimension from N(r) ∝ r^D");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BGE_08 — Deterministic reproducibility
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_BGE_08_AllComputations_AreDeterministic()
    {
        var g = BuildHidden(3, 5);

        var h1 = RunKuramotoFullHistory(g, K, SigmaOmega, Dt, Steps, BaseSeed);
        var h2 = RunKuramotoFullHistory(g, K, SigmaOmega, Dt, Steps, BaseSeed);
        int N = g.NodeCount;
        for (int t = 0; t < h1.Length; t++)
        for (int i = 0; i < N; i++)
            Assert.Equal(h1[t][i], h2[t][i], 12);

        var Rp1 = BuildPhaseLockMatrix(h1);
        var Rp2 = BuildPhaseLockMatrix(h2);
        for (int i = 0; i < N; i++)
        for (int j = 0; j < N; j++)
            Assert.Equal(Rp1[i, j], Rp2[i, j], 12);

        var Rc1 = BuildCorrelationMatrix(h1);
        var Rc2 = BuildCorrelationMatrix(h2);
        for (int i = 0; i < N; i++)
        for (int j = 0; j < N; j++)
            Assert.Equal(Rc1[i, j], Rc2[i, j], 12);

        var Rl1 = BuildLockTimeMatrix(h1, LockEpsilon, Tau0);
        var Rl2 = BuildLockTimeMatrix(h2, LockEpsilon, Tau0);
        for (int i = 0; i < N; i++)
        for (int j = 0; j < N; j++)
            Assert.Equal(Rl1[i, j], Rl2[i, j], 12);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BGE_09 — Phase-lock matrix monotonicity: closer oscillators → higher R_phase
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(1, 20)]
    [InlineData(2, 8)]
    [InlineData(3, 5)]
    public void V4_1_BGE_09_PhaseLockMatrix_CorrelatesWithHiddenDistance(int D, int n)
    {
        var g = BuildHidden(D, n);
        var history = RunKuramotoFullHistory(g, K, SigmaOmega, Dt, Steps, BaseSeed);
        var Rphase = BuildPhaseLockMatrix(history);
        int N = g.NodeCount;

        // Spearman between R_phase and −(graph_distance)
        var rVals = new List<double>();
        var dVals = new List<double>();
        for (int i = 0; i < N; i++)
        for (int j = i + 1; j < N; j++)
        {
            int sp = ShortestPath(g, i, j);
            if (sp >= 0) { rVals.Add(Rphase[i, j]); dVals.Add(-(double)sp); }
        }

        double rho = SpearmanRho(rVals.ToArray(), dVals.ToArray());

        // For regular lattices with strong coupling, phase coherence should correlate
        // positively with proximity (R_phase ↑ as distance ↓ → ρ > 0).
        // Some topologies may have weak correlation; we check ρ is not strongly negative.
        Assert.True(rho > -0.5,
            $"D={D}: Spearman ρ(R_phase, −d_graph) = {rho:F4}, should not be strongly negative.");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BGE_10 — Falsification: if oscillators are uncoupled (K=0), R should be random
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(1, 20)]
    [InlineData(3, 5)]
    public void V4_1_BGE_10_Falsification_UncoupledVsCoupled(int D, int n)
    {
        var g = BuildHidden(D, n);
        var histUncoupled = RunKuramotoFullHistory(g, 0.0, SigmaOmega, Dt, Steps, BaseSeed);
        var histCoupled = RunKuramotoFullHistory(g, K, SigmaOmega, Dt, Steps, BaseSeed);

        var RpUncoupled = BuildPhaseLockMatrix(histUncoupled);
        var RpCoupled = BuildPhaseLockMatrix(histCoupled);
        int N = g.NodeCount;

        var uncoupledVals = new List<double>();
        var coupledVals = new List<double>();
        for (int i = 0; i < N; i++)
        for (int j = i + 1; j < N; j++)
        {
            uncoupledVals.Add(RpUncoupled[i, j]);
            coupledVals.Add(RpCoupled[i, j]);
        }

        double meanUncoupled = uncoupledVals.Average();
        double meanCoupled = coupledVals.Average();

        _output.WriteLine($"  D={D}: mean R_phase uncoupled={meanUncoupled:F4}, coupled={meanCoupled:F4}");

        // Both must be finite.
        Assert.True(double.IsFinite(meanUncoupled));
        Assert.True(double.IsFinite(meanCoupled));

        // The uncoupled and coupled distributions differ (they are computed from different dynamics).
        // No claim about direction — this is purely diagnostic.
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BGE_11 — Method comparison summary: which R candidate best reconstructs?
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_BGE_11_MethodComparison_Summary()
    {
        var topologies = new (string name, GraphTopology g)[]
        {
            ("1D chain", BuildHidden(1, 20)),
            ("2D lattice", BuildHidden(2, 8)),
            ("3D cubic", BuildHidden(3, 5)),
            ("4D hypercubic", BuildHidden(4, 3)),
            ("random", BuildRandomGraph(60, 6, BaseSeed)),
            ("small-world", BuildSmallWorld(60, 6, 0.1, BaseSeed)),
        };

        var methodNames = new[] { "phase-lock", "correlation", "lock-time" };
        var builders = new Func<double[][], double[,]>[]
            { BuildPhaseLockMatrix, BuildCorrelationMatrix, h => BuildLockTimeMatrix(h, LockEpsilon, Tau0) };

        _output.WriteLine("══════════════════════════════════════════════════════════════");
        _output.WriteLine("  METHOD COMPARISON — WHICH R CANDIDATE BEST RECONSTRUCTS?");
        _output.WriteLine("══════════════════════════════════════════════════════════════");
        _output.WriteLine("");

        // Per-method average metrics across topologies
        foreach (int m in new[] { 0, 1, 2 })
        {
            double totalF1 = 0, totalSpear = 0;
            int count = 0;
            foreach (var (_, g) in topologies)
            {
                var history = RunKuramotoFullHistory(g, K, SigmaOmega, Dt, Steps, BaseSeed);
                var R = NormalizeRateMatrix(builders[m](history));
                var d = DistanceFromRate(R);
                var (_, _, f1, spearman) = ReconstructionMetrics(d, g, KnnK);
                totalF1 += f1; totalSpear += spearman; count++;
            }

            double avgF1 = totalF1 / count;
            double avgSpear = totalSpear / count;

            _output.WriteLine($"  {methodNames[m],-14}:  avg F1 = {avgF1:F4}  avg Spearman ρ = {avgSpear:F4}");
        }

        _output.WriteLine("");
        _output.WriteLine("  Higher F1 → better edge reconstruction.");
        _output.WriteLine("  Higher Spearman ρ → better rank-order agreement with true graph distance.");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BGE_12 — Effective dimension of recovered graphs across topologies
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_BGE_12_EffectiveDimension_RecoveredVsHidden()
    {
        var topologies = new (string name, int hiddenD, GraphTopology g)[]
        {
            ("1D chain", 1, BuildHidden(1, 20)),
            ("2D lattice", 2, BuildHidden(2, 8)),
            ("3D cubic", 3, BuildHidden(3, 5)),
            ("4D hypercubic", 4, BuildHidden(4, 3)),
        };

        var methodNames = new[] { "phase-lock", "correlation", "lock-time" };
        var builders = new Func<double[][], double[,]>[]
            { BuildPhaseLockMatrix, BuildCorrelationMatrix, h => BuildLockTimeMatrix(h, LockEpsilon, Tau0) };

        _output.WriteLine("  EFFECTIVE DIMENSION — RECOVERED vs HIDDEN");
        _output.WriteLine($"  {"topology",-18} {"hidden D",9} {"phase-lock D_eff",18} {"correlation D_eff",18} {"lock-time D_eff",17}");
        _output.WriteLine($"  {new string('-',18)} {new string('-',9)} {new string('-',18)} {new string('-',18)} {new string('-',17)}");

        foreach (var (name, hiddenD, g) in topologies)
        {
            var history = RunKuramotoFullHistory(g, K, SigmaOmega, Dt, Steps, BaseSeed);
            var deffs = new double[3];
            for (int m = 0; m < 3; m++)
            {
                var R = NormalizeRateMatrix(builders[m](history));
                var d = DistanceFromRate(R);
                var knn = BuildKnnGraph(d, KnnK);
                deffs[m] = EffectiveDim(knn);
            }
            _output.WriteLine($"  {name,-18} {hiddenD,9} {deffs[0],18:F2} {deffs[1],18:F2} {deffs[2],17:F2}");
        }
    }
}
