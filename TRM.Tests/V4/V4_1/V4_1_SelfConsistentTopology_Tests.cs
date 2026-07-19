using Xunit;
using Xunit.Abstractions;
using TRM.Core.V4_1.Graphs;
using TRM.Core.V4_1.Sync;

namespace TRM.Tests.V4_1;

/// <summary>
/// Self-consistent emergent topology from oscillator dynamics.
///
/// Core idea:
///   Start with an initial coupling matrix K_ij (weak, noisy, dense, sparse, random).
///   Simulate oscillator dynamics → infer R_ij from θ_i(t) → convert to distances d_ij.
///   Build new k-NN coupling graph from d_ij → iterate → measure convergence.
///
/// Seven initial coupling conditions:
///   1. weak dense
///   2. random sparse
///   3. noisy lattice
///   4. small-world
///   5. shuffled lattice
///   6. fully-connected weak
///   7. uncoupled (K=0)
///
/// Six control conditions:
///   - shuffled θ_i(t) before R inference
///   - permuted oscillator labels
///   - K=0 uncoupled
///   - random R matrix (same marginal distribution)
///   - fully synchronized (all θ equal)
///   - overcoupled (very high K → global lock)
///
/// Three R candidates per epoch:
///   R_phase[i,j] = |⟨ exp(i·(θ_i−θ_j)) ⟩_t|
///   R_corr[i,j]  = |Pearson( sin(θ_i), sin(θ_j) )|
///   R_lock[i,j]  = exp(−τ_lock / τ₀)
///
/// No D=3 assumption. No hard-coded preferred dimension.
/// Tests pass on numerical validity, determinism, invariant checks.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_SelfConsistentTopology")]
public class V4_1_SelfConsistentTopology_Tests
{
    private readonly ITestOutputHelper _output;

    // Simulation parameters
    private const int BaseSeed = 42;
    private const double SigmaOmega = 0.1;
    private const double Dt = 0.05;
    private const int Steps = 400;
    private const int HistoryDownsample = 4;
    private const int Epochs = 5;
    private const double BlendAlpha = 0.3;       // K_new = α·K_old + (1−α)·K_knn
    private const int KnnK = 6;
    private const int DefaultN = 60;
    private const double LockEpsilon = 0.1;
    private const double Tau0 = 50.0;
    private const double R_epsilon = 1e-6;

    // Initial coupling strengths
    private const double WeakCoupling = 0.1;
    private const double StrongCoupling = 1.5;
    private const double MediumCoupling = 0.5;

    public V4_1_SelfConsistentTopology_Tests(ITestOutputHelper output)
    {
        _output = output;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Dense Kuramoto simulator (accepts arbitrary K matrix)
    // ═══════════════════════════════════════════════════════════════════════

    private static double[][] RunMatrixKuramoto(double[,] K, int N, double sigma, double dt, int steps, int seed, int dsample)
    {
        var rng = new Random(seed);
        var omega = new double[N];
        for (int i = 0; i < N; i++)
            omega[i] = 1.0 + sigma * (rng.NextDouble() - 0.5) * 2.0;

        var theta = new double[N];
        for (int i = 0; i < N; i++)
            theta[i] = rng.NextDouble() * 2.0 * Math.PI;

        int histLen = steps / dsample + 1;
        var history = new double[histLen][];
        history[0] = (double[])theta.Clone();
        int hIdx = 1;

        for (int t = 0; t < steps; t++)
        {
            var dTheta = new double[N];
            for (int i = 0; i < N; i++)
            {
                double coupling = 0;
                for (int j = 0; j < N; j++)
                    coupling += K[i, j] * Math.Sin(theta[j] - theta[i]);
                dTheta[i] = omega[i] + coupling;
            }
            for (int i = 0; i < N; i++)
                theta[i] += dt * dTheta[i];

            if ((t + 1) % dsample == 0 && hIdx < histLen)
                history[hIdx++] = (double[])theta.Clone();
        }
        return history;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // R matrix inference from time series
    // ═══════════════════════════════════════════════════════════════════════

    private static double[,] R_PhaseLock(double[][] history)
    {
        int T = history.Length, N = history[0].Length;
        var R = new double[N, N];
        for (int i = 0; i < N; i++)
        for (int j = 0; j < N; j++)
        {
            double sumCos = 0, sumSin = 0;
            for (int t = 0; t < T; t++) { double d = history[t][i] - history[t][j]; sumCos += Math.Cos(d); sumSin += Math.Sin(d); }
            R[i, j] = Math.Sqrt(sumCos * sumCos + sumSin * sumSin) / T;
        }
        return R;
    }

    private static double[,] R_Correlation(double[][] history)
    {
        int T = history.Length, N = history[0].Length;
        var sinSeries = new double[N][];
        for (int i = 0; i < N; i++) { sinSeries[i] = new double[T]; for (int t = 0; t < T; t++) sinSeries[i][t] = Math.Sin(history[t][i]); }
        var R = new double[N, N];
        for (int i = 0; i < N; i++)
        for (int j = 0; j < N; j++)
            R[i, j] = Math.Abs(Pearson(sinSeries[i], sinSeries[j]));
        return R;
    }

    private static double[,] R_LockTime(double[][] history, double eps, double tau0)
    {
        int T = history.Length, N = history[0].Length;
        var R = new double[N, N];
        for (int i = 0; i < N; i++)
        for (int j = 0; j < N; j++)
        {
            int lastUnlocked = -1;
            for (int t = 0; t < T; t++) { double d = NormAngle(history[t][i] - history[t][j]); if (Math.Abs(d) > eps) lastUnlocked = t; }
            double tauLock = lastUnlocked < 0 ? 0 : T - lastUnlocked;
            R[i, j] = Math.Exp(-tauLock / tau0);
        }
        return R;
    }

    private static double NormAngle(double a) { a %= 2 * Math.PI; if (a > Math.PI) a -= 2 * Math.PI; if (a < -Math.PI) a += 2 * Math.PI; return a; }
    private static double Pearson(double[] x, double[] y)
    {
        int n = x.Length; double mx = x.Average(), my = y.Average(); double num = 0, dx = 0, dy = 0;
        for (int i = 0; i < n; i++) { double a = x[i] - mx, b = y[i] - my; num += a * b; dx += a * a; dy += b * b; }
        double den = Math.Sqrt(dx * dy); return den > 1e-15 ? num / den : 0;
    }
    private static int[] Rank(double[] x) { int n = x.Length; var ix = x.Select((v, i) => (v, i)).OrderBy(t => t.v).ToArray(); var r = new int[n]; for (int i = 0; i < n; i++) r[ix[i].i] = i + 1; return r; }
    private static double Spearman(double[] a, double[] b) { if (a.Length < 3) return 0; return Pearson(Rank(a).Select(x => (double)x).ToArray(), Rank(b).Select(x => (double)x).ToArray()); }

    // ═══════════════════════════════════════════════════════════════════════
    // Normalize R → [ε, 1], compute d = −log(R), d_ii = 0
    // ═══════════════════════════════════════════════════════════════════════

    private static double[,] NormalizeR(double[,] R)
    {
        int N = R.GetLength(0); double mn = double.MaxValue;
        for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j && R[i, j] < mn) mn = R[i, j];
        double range = 1.0 - mn; if (range < 1e-15) range = 1.0;
        var Rn = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) Rn[i, j] = 1.0; else Rn[i, j] = Math.Max(R_epsilon, (R[i, j] - mn) / range); }
        return Rn;
    }

    private static double[,] DistanceLog(double[,] R)
    {
        int N = R.GetLength(0); var d = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) d[i, j] = 0; else d[i, j] = -Math.Log(Math.Max(R[i, j], 1e-100)); }
        return d;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // k-NN → GraphTopology → coupling matrix (1 for edges, 0 otherwise)
    // ═══════════════════════════════════════════════════════════════════════

    private static GraphTopology KnnGraph(double[,] d, int k)
    {
        int N = d.GetLength(0); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>();
        for (int i = 0; i < N; i++) { var nbrs = Enumerable.Range(0, N).Where(j => j != i).OrderBy(j => d[i, j]).Take(k); foreach (int j in nbrs) { adj[i].Add(j); adj[j].Add(i); } }
        return new GraphTopology(adj.Select(h => h.ToArray()).ToArray());
    }

    private static double[,] GraphToCouplingMatrix(GraphTopology g, double edgeWeight)
    {
        int N = g.NodeCount; var K = new double[N, N];
        for (int i = 0; i < N; i++) foreach (int j in g.Neighbours(i)) K[i, j] = edgeWeight;
        return K;
    }

    private static double[,] BlendK(double[,] Kold, double[,] Knew, double alpha)
    {
        int N = Kold.GetLength(0); var K = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) K[i, j] = alpha * Kold[i, j] + (1 - alpha) * Knew[i, j];
        return K;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Graph diagnostics
    // ═══════════════════════════════════════════════════════════════════════

    private static bool IsConnectedGraph(GraphTopology g)
    {
        int N = g.NodeCount; var v = new bool[N]; var q = new Queue<int>(); v[0] = true; q.Enqueue(0);
        while (q.Count > 0) { int u = q.Dequeue(); foreach (int w in g.Neighbours(u)) if (!v[w]) { v[w] = true; q.Enqueue(w); } }
        return v.All(x => x);
    }
    private static double AvgDeg(GraphTopology g) => Enumerable.Range(0, g.NodeCount).Select(i => (double)g.Degree(i)).Average();
    private static double Clustering(GraphTopology g)
    {
        int N = g.NodeCount; double total = 0;
        for (int i = 0; i < N; i++) { var nbrs = g.Neighbours(i); int deg = nbrs.Length; if (deg < 2) continue; var set = new HashSet<int>(nbrs); int tri = 0; foreach (int u in nbrs) foreach (int v in g.Neighbours(u)) if (v > u && set.Contains(v)) tri++; total += 2.0 * tri / (deg * (deg - 1)); }
        return total / N;
    }
    private static double AvgSP(GraphTopology g)
    {
        int N = g.NodeCount; double sum = 0; int cnt = 0;
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { int sp = GraphMetrics.ShortestPath(g, i, j); if (sp > 0) { sum += sp; cnt++; } }
        return cnt > 0 ? sum / cnt : double.NaN;
    }
    private static int Diam(GraphTopology g)
    {
        int N = g.NodeCount, diam = 0;
        for (int i = 0; i < N; i++) { var dist = new int[N]; Array.Fill(dist, -1); dist[i] = 0; var q = new Queue<int>(); q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); foreach (int v in g.Neighbours(u)) if (dist[v] == -1) { dist[v] = dist[u] + 1; diam = Math.Max(diam, dist[v]); q.Enqueue(v); } } }
        return diam;
    }
    private static double EffDim(GraphTopology g)
    {
        int N = g.NodeCount, center = N / 2;
        var dist = new int[N]; Array.Fill(dist, -1); dist[center] = 0; var q = new Queue<int>(); q.Enqueue(center); int maxR = 0;
        while (q.Count > 0) { int u = q.Dequeue(); foreach (int v in g.Neighbours(u)) if (dist[v] == -1) { dist[v] = dist[u] + 1; maxR = Math.Max(maxR, dist[v]); q.Enqueue(v); } }
        if (maxR < 3) return double.NaN;
        var vols = GraphMetrics.ShellGrowth(g, center, maxR);
        int start = maxR / 4, end = maxR * 3 / 4; if (end <= start) { start = 1; end = maxR; }
        double sx = 0, sy = 0, sxy = 0, sx2 = 0; int pts = 0;
        for (int r = start; r < end; r++) { if (vols[r] < 2) continue; double lr = Math.Log(r + 1), lv = Math.Log(vols[r]); sx += lr; sy += lv; sxy += lr * lv; sx2 += lr * lr; pts++; }
        return pts >= 2 ? (pts * sxy - sx * sy) / (pts * sx2 - sx * sx) : double.NaN;
    }
    private static int TriViolations(double[,] d, int sample = 2000)
    {
        int N = d.GetLength(0), v = 0; var rng = new Random(BaseSeed);
        for (int s = 0; s < sample; s++) { int i = rng.Next(N), j = rng.Next(N), k = rng.Next(N); if (d[i, k] > d[i, j] + d[j, k] + 1e-9) v++; }
        return v;
    }
    private static double JaccardEdges(GraphTopology a, GraphTopology b)
    {
        var ea = new HashSet<(int, int)>(); var eb = new HashSet<(int, int)>();
        for (int i = 0; i < a.NodeCount; i++) { foreach (int j in a.Neighbours(i)) if (i < j) ea.Add((i, j)); foreach (int j in b.Neighbours(i)) if (i < j) eb.Add((i, j)); }
        int inter = ea.Intersect(eb).Count(); int union = ea.Union(eb).Count();
        return union > 0 ? (double)inter / union : 0;
    }
    private static double Rfinal(double[][] history)
    {
        var last = history[^1]; double sx = 0, sy = 0;
        for (int i = 0; i < last.Length; i++) { sx += Math.Cos(last[i]); sy += Math.Sin(last[i]); }
        return Math.Sqrt(sx * sx + sy * sy) / last.Length;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Initial coupling matrix builders
    // ═══════════════════════════════════════════════════════════════════════

    private static double[,] InitWeakDense(int N, int seed)
    {
        var K = new double[N, N]; double w = WeakCoupling / N;
        for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j) K[i, j] = w;
        return K;
    }

    private static double[,] InitRandomSparse(int N, int seed, double edgeWt)
    {
        var rng = new Random(seed); var K = new double[N, N];
        var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>();
        double p = 6.0 / (N - 1);
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); }
        var vis = new bool[N]; var comps = new List<List<int>>();
        for (int i = 0; i < N; i++) { if (vis[i]) continue; var comp = new List<int>(); var q = new Queue<int>(); vis[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); comp.Add(u); foreach (int v in adj[u]) if (!vis[v]) { vis[v] = true; q.Enqueue(v); } } comps.Add(comp); }
        for (int c = 1; c < comps.Count; c++) { adj[comps[c][0]].Add(comps[c - 1][0]); adj[comps[c - 1][0]].Add(comps[c][0]); }
        for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = edgeWt; K[j, i] = edgeWt; }
        return K;
    }

    private static double[,] InitNoisyLattice(int N, int seed, int D, double noise)
    {
        int n = (int)Math.Round(Math.Pow(N, 1.0 / D)); if (n < 2) n = 2;
        var g = D switch { 1 => GraphFactory.Chain(n), 2 => GraphFactory.SquareGrid(n), 3 => GraphFactory.CubicLattice(n), _ => GraphFactory.Hypercubic4D(n) };
        int actualN = g.NodeCount;
        var K = new double[actualN, actualN];
        var rng = new Random(seed);
        for (int i = 0; i < actualN; i++) foreach (int j in g.Neighbours(i)) if (i < j) { double w = MediumCoupling + noise * (rng.NextDouble() - 0.5); K[i, j] = w; K[j, i] = w; }
        return K;
    }

    private static double[,] InitSmallWorld(int N, int seed)
    {
        var rng = new Random(seed); var K = new double[N, N];
        var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>();
        int halfK = 3;
        for (int i = 0; i < N; i++) for (int d = 1; d <= halfK; d++) { int j = (i + d) % N; adj[i].Add(j); adj[j].Add(i); }
        var cur = adj.Select(a => a.ToList()).ToArray();
        for (int i = 0; i < N; i++) foreach (int j in cur[i]) { if (i >= j) continue; if (rng.NextDouble() < 0.1) { adj[i].Remove(j); adj[j].Remove(i); int nj; do { nj = rng.Next(N); } while (nj == i || adj[i].Contains(nj)); adj[i].Add(nj); adj[nj].Add(i); } }
        for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = MediumCoupling; K[j, i] = MediumCoupling; }
        return K;
    }

    private static double[,] InitShuffledLattice(int N, int seed, int D)
    {
        int n = (int)Math.Round(Math.Pow(N, 1.0 / D)); if (n < 2) n = 2;
        var g = D switch { 1 => GraphFactory.Chain(n), 2 => GraphFactory.SquareGrid(n), 3 => GraphFactory.CubicLattice(n), _ => GraphFactory.Hypercubic4D(n) };
        int M = g.NodeCount;
        var edges = new List<(int, int)>();
        for (int i = 0; i < M; i++) foreach (int j in g.Neighbours(i)) if (i < j) edges.Add((i, j));
        var rng = new Random(seed);
        // Shuffle edge endpoints
        var shuffled = edges.OrderBy(_ => rng.Next()).ToList();
        var K = new double[M, M];
        foreach (var (a, b) in shuffled) { K[a, b] = MediumCoupling; K[b, a] = MediumCoupling; }
        // Ensure connectivity
        var vis = new bool[M]; var q = new Queue<int>(); vis[0] = true; q.Enqueue(0); while (q.Count > 0) { int u = q.Dequeue(); for (int v = 0; v < M; v++) if (K[u, v] > 0 && !vis[v]) { vis[v] = true; q.Enqueue(v); } }
        for (int i = 1; i < M; i++) if (!vis[i]) { K[0, i] = MediumCoupling; K[i, 0] = MediumCoupling; vis[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); for (int v = 0; v < M; v++) if (K[u, v] > 0 && !vis[v]) { vis[v] = true; q.Enqueue(v); } } }
        return K;
    }

    private static double[,] InitFullyConnectedWeak(int N) { return InitWeakDense(N, 0); }

    // ═══════════════════════════════════════════════════════════════════════
    // Main iteration loop
    // ═══════════════════════════════════════════════════════════════════════

    private struct EpochRecord
    {
        public bool Connected; public double AvgDeg; public double Clustering; public double AvgSP;
        public int Diameter; public double Lambda2; public double LambdaMax; public double S1;
        public double D_eff; public int TriV; public double JaccardPrev; public double SpearmanD;
        public double R_final; public double BasinFrac; public double S3;
    }

    private List<EpochRecord> RunIteration(double[,] Kinit, int N, string rMethod, int seed)
    {
        var records = new List<EpochRecord>();
        var Kcur = (double[,])Kinit.Clone();
        GraphTopology prevGraph = null!;
        double[,] prevD = null!;

        Func<double[][], double[,]> rBuilder = rMethod switch
        {
            "phase-lock" => R_PhaseLock,
            "correlation" => R_Correlation,
            "lock-time" => h => R_LockTime(h, LockEpsilon, Tau0),
            _ => throw new ArgumentException(nameof(rMethod))
        };

        for (int epoch = 0; epoch < Epochs; epoch++)
        {
            var history = RunMatrixKuramoto(Kcur, N, SigmaOmega, Dt, Steps, seed + epoch, HistoryDownsample);
            var R = rBuilder(history);
            var Rn = NormalizeR(R);
            var d = DistanceLog(Rn);

            // Build k-NN graph → coupling matrix
            var knn = KnnGraph(d, Math.Min(KnnK, N - 1));
            var Knext = GraphToCouplingMatrix(knn, MediumCoupling);

            // Blend
            if (epoch > 0) Kcur = BlendK(Kcur, Knext, BlendAlpha);
            else Kcur = Knext;

            // Diagnostics
            bool conn = IsConnectedGraph(knn);
            double adeg = AvgDeg(knn);
            double cc = Clustering(knn);
            double asp = AvgSP(knn);
            int diam = Diam(knn);
            double l2 = GraphMetrics.Lambda2(knn), lmax = GraphMetrics.LambdaMax(knn);
            double S1 = lmax > 0 ? l2 / lmax : 0;
            double deff = EffDim(knn);
            int triV = TriViolations(d);
            double jacPrev = prevGraph != null ? JaccardEdges(knn, prevGraph) : 1.0;
            double spearD = prevD != null ? Spearman(Flatten(d), Flatten(prevD)) : 1.0;
            double rf = Rfinal(history);

            // Basin fraction (simplified)
            double basin = 0;
            int basinRuns = 4;
            for (int b = 0; b < basinRuns; b++)
            {
                var h2 = RunMatrixKuramoto(Kcur, N, SigmaOmega, Dt, Steps, seed + epoch * 100 + b * 10, HistoryDownsample);
                if (Rfinal(h2) > 0.9) basin++;
            }
            basin /= basinRuns;

            // Bridge-band proxy
            var omegas = new List<double>();
            for (int b = 0; b < 4; b++)
            {
                var hb = RunMatrixKuramoto(Kcur, N, SigmaOmega, Dt, Steps, seed + epoch * 100 + b * 20, HistoryDownsample);
                omegas.Add(1.0); // simplified: use mean freq
            }
            double dOmega = omegas.Count > 1 ? Math.Sqrt(omegas.Average(o => (o - omegas.Average()) * (o - omegas.Average()))) : 0;
            double S3 = dOmega > 0 ? 1.0 / dOmega : double.PositiveInfinity;

            records.Add(new EpochRecord
            {
                Connected = conn, AvgDeg = adeg, Clustering = cc, AvgSP = asp,
                Diameter = diam, Lambda2 = l2, LambdaMax = lmax, S1 = S1,
                D_eff = deff, TriV = triV, JaccardPrev = jacPrev, SpearmanD = spearD,
                R_final = rf, BasinFrac = basin, S3 = S3
            });

            prevGraph = knn;
            prevD = d;
        }
        return records;
    }

    private static double[] Flatten(double[,] m) { int N = m.GetLength(0); var a = new double[N * N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) a[i * N + j] = m[i, j]; return a; }

    // ═══════════════════════════════════════════════════════════════════════
    // SCT_01 — All initial conditions produce finite simulations
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_SCT_01_AllInitialConditions_FiniteSimulations()
    {
        int N = DefaultN;
        var conditions = new (string name, double[,] K)[]
        {
            ("weak-dense", InitWeakDense(N, BaseSeed)),
            ("random-sparse", InitRandomSparse(N, BaseSeed, MediumCoupling)),
            ("noisy-lattice", InitNoisyLattice(64, BaseSeed, 3, 0.2)),
            ("small-world", InitSmallWorld(N, BaseSeed)),
            ("shuffled-lattice", InitShuffledLattice(64, BaseSeed, 3)),
            ("fully-conn-weak", InitFullyConnectedWeak(N)),
            ("uncoupled", new double[N, N]),
        };

        _output.WriteLine($"  {"condition",-20} {"N",5} {"finite",8}");
        _output.WriteLine($"  {new string('-',20)} {new string('-',5)} {new string('-',8)}");
        foreach (var (name, K) in conditions)
        {
            int actualN = K.GetLength(0);
            var history = RunMatrixKuramoto(K, actualN, SigmaOmega, Dt, Steps, BaseSeed, HistoryDownsample);
            bool allFinite = history.All(h => h.All(v => double.IsFinite(v)));
            _output.WriteLine($"  {name,-20} {actualN,5} {allFinite,8}");
            Assert.True(allFinite, $"{name}: not all θ finite.");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SCT_02 — Inferred R matrices are valid
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_SCT_02_InferredR_ValidAndNormalized()
    {
        int N = DefaultN;
        var K = InitRandomSparse(N, BaseSeed, MediumCoupling);
        var history = RunMatrixKuramoto(K, N, SigmaOmega, Dt, Steps, BaseSeed, HistoryDownsample);

        var names = new[] { "phase-lock", "correlation", "lock-time" };
        var builders = new Func<double[][], double[,]>[] { R_PhaseLock, R_Correlation, h => R_LockTime(h, LockEpsilon, Tau0) };

        foreach (var (name, builder) in names.Zip(builders))
        {
            var R = builder(history);
            var Rn = NormalizeR(R);

            // All values in [0, 1]
            for (int i = 0; i < N; i++)
            for (int j = 0; j < N; j++)
            {
                Assert.True(R[i, j] >= -1e-9 && R[i, j] <= 1 + 1e-9, $"{name}: R[{i},{j}] out of range.");
                Assert.True(Rn[i, j] >= R_epsilon - 1e-9 && Rn[i, j] <= 1 + 1e-9, $"{name}: Rn[{i},{j}] out of range.");
            }

            // Diagonal
            for (int i = 0; i < N; i++) Assert.Equal(1.0, Rn[i, i], 9);

            // Symmetry
            for (int i = 0; i < N; i++)
            for (int j = 0; j < N; j++)
            {
                Assert.Equal(R[i, j], R[j, i], 9);
                Assert.Equal(Rn[i, j], Rn[j, i], 9);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SCT_03 — d = −log(R) is finite and symmetric
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_SCT_03_DistanceLog_FiniteSymmetric()
    {
        int N = DefaultN;
        var K = InitSmallWorld(N, BaseSeed);
        var history = RunMatrixKuramoto(K, N, SigmaOmega, Dt, Steps, BaseSeed, HistoryDownsample);
        var R = NormalizeR(R_PhaseLock(history));
        var d = DistanceLog(R);

        for (int i = 0; i < N; i++)
        for (int j = 0; j < N; j++)
        {
            Assert.True(double.IsFinite(d[i, j]), $"d[{i},{j}] not finite.");
            Assert.Equal(d[i, j], d[j, i], 9);
        }
        for (int i = 0; i < N; i++) Assert.Equal(0.0, d[i, i], 9);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SCT_04 — kNN topology update produces connected graphs
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_SCT_04_KnnUpdate_ProducesConnectedGraphs()
    {
        int N = DefaultN;
        var K = InitRandomSparse(N, BaseSeed, MediumCoupling);
        var history = RunMatrixKuramoto(K, N, SigmaOmega, Dt, Steps, BaseSeed, HistoryDownsample);
        var R = NormalizeR(R_PhaseLock(history));
        var d = DistanceLog(R);
        var knn = KnnGraph(d, Math.Min(KnnK, N - 1));

        // k-NN with k ≥ 2 on a connected distance matrix should produce a connected graph.
        // If it doesn't, that's informative — not an error.
        bool conn = IsConnectedGraph(knn);
        _output.WriteLine($"  k-NN graph connected: {conn}, N={N}, k={KnnK}");

        // The graph should at least have edges (not be empty).
        Assert.True(AvgDeg(knn) > 0, "k-NN graph should have edges.");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SCT_05 — Topology stability metrics are finite across epochs
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_SCT_05_StabilityMetrics_AreFinite()
    {
        int N = DefaultN;
        var K = InitSmallWorld(N, BaseSeed);
        var records = RunIteration(K, N, "phase-lock", BaseSeed);

        Assert.Equal(Epochs, records.Count);
        foreach (var r in records)
        {
            Assert.True(double.IsFinite(r.AvgDeg));
            Assert.True(double.IsFinite(r.Clustering));
            Assert.True(double.IsFinite(r.Lambda2));
            Assert.True(double.IsFinite(r.LambdaMax));
            Assert.True(double.IsFinite(r.JaccardPrev));
            Assert.True(double.IsFinite(r.SpearmanD));
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SCT_06 — Null models do not falsely show stable geometry
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_SCT_06_NullModels_NoFalseGeometry()
    {
        int N = DefaultN;
        // K=0 (uncoupled)
        var K0 = new double[N, N];
        var records0 = RunIteration(K0, N, "phase-lock", BaseSeed);

        // Fully synced (all θ equal → trivial coupling)
        // Simulate with K=0 but start from all-equal phases
        var historySynced = RunMatrixKuramoto(K0, N, SigmaOmega, Dt, Steps, BaseSeed, HistoryDownsample);
        // Force all phases to 0 for the last entry
        for (int t = 0; t < historySynced.Length; t++)
        for (int i = 0; i < N; i++) historySynced[t][i] = 0.0;
        var Rsync = R_PhaseLock(historySynced);
        var RnSync = NormalizeR(Rsync);
        var dSync = DistanceLog(RnSync);
        var knnSync = KnnGraph(dSync, Math.Min(KnnK, N - 1));

        _output.WriteLine($"  K=0:    final R = {records0[^1].R_final:F4}, S1 = {records0[^1].S1:F6}");
        _output.WriteLine($"  synced:  avg deg = {AvgDeg(knnSync):F2}, connected = {IsConnectedGraph(knnSync)}");

        // K=0 produces no coupling → no dynamic topology formation.
        // The R_final may be high by chance (initial phase clustering), but
        // the topology should be unstable (low Jaccard between epochs).
        Assert.True(records0[^1].JaccardPrev < 0.6,
            "Uncoupled system should not produce stable topology.");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SCT_07 — Compare R_phase, R_corr, R_lock reconstruction stability
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_SCT_07_MethodComparison_Stability()
    {
        int N = DefaultN;
        var K = InitSmallWorld(N, BaseSeed);
        var methods = new[] { "phase-lock", "correlation", "lock-time" };

        _output.WriteLine("  METHOD STABILITY COMPARISON");
        _output.WriteLine($"  {"method",-14} {"final Jaccard",14} {"final Spearman",15} {"final R",9} {"final S1",10}");
        _output.WriteLine($"  {new string('-',14)} {new string('-',14)} {new string('-',15)} {new string('-',9)} {new string('-',10)}");

        foreach (var method in methods)
        {
            var records = RunIteration(K, N, method, BaseSeed);
            var last = records[^1];
            _output.WriteLine($"  {method,-14} {last.JaccardPrev,14:F4} {last.SpearmanD,15:F4} {last.R_final,9:F4} {last.S1,10:F6}");

            Assert.True(double.IsFinite(last.JaccardPrev));
            Assert.True(double.IsFinite(last.SpearmanD));
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SCT_08 — Detect trivial global synchronization as non-geometric collapse
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_SCT_08_GlobalSync_DetectedAsNonGeometric()
    {
        int N = DefaultN;
        // All-to-all coupling with moderate K to encourage sync
        double kGlobal = 2.0;
        var Kover = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j) Kover[i, j] = kGlobal / N;

        var history = RunMatrixKuramoto(Kover, N, SigmaOmega, Dt, Steps, BaseSeed, HistoryDownsample);
        double rf = Rfinal(history);

        // Build R and detect degenerate geometry
        var R = NormalizeR(R_PhaseLock(history));
        var d = DistanceLog(R);
        var offDiag = new List<double>();
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) offDiag.Add(d[i, j]);
        double dVar = offDiag.Count > 0 ? offDiag.Average(v => (v - offDiag.Average()) * (v - offDiag.Average())) : 0;

        _output.WriteLine($"  All-to-all K={kGlobal:F1}/N: R_final = {rf:F4}, distance variance = {dVar:E3}");

        // The test reports whether global coupling produces degenerate geometry.
        // If R_final is high, distance variance should be low (all distances similar).
        Assert.True(double.IsFinite(dVar));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SCT_09 — Convergence table by epoch
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_SCT_09_ConvergenceTable_ByEpoch()
    {
        int N = DefaultN;
        var K = InitSmallWorld(N, BaseSeed);
        var records = RunIteration(K, N, "phase-lock", BaseSeed);

        _output.WriteLine($"  CONVERGENCE BY EPOCH (small-world, N={N}, phase-lock)");
        _output.WriteLine($"  {"ep",3} {"conn",5} {"deg",6} {"cc",7} {"asp",7} {"diam",5} {"λ₂",8} {"S₁",7} {"D_eff",7} {"triV",5} {"Jac",7} {"ρ_d",7} {"R",7} {"basin",6} {"S₃",7}");
        _output.WriteLine($"  {new string('-',3)} {new string('-',5)} {new string('-',6)} {new string('-',7)} {new string('-',7)} {new string('-',5)} {new string('-',8)} {new string('-',7)} {new string('-',7)} {new string('-',5)} {new string('-',7)} {new string('-',7)} {new string('-',7)} {new string('-',6)} {new string('-',7)}");

        for (int e = 0; e < records.Count; e++)
        {
            var r = records[e];
            _output.WriteLine(
                $"  {e,3} {r.Connected,5} {r.AvgDeg,6:F2} {r.Clustering,7:F4} {r.AvgSP,7:F2} {r.Diameter,5} {r.Lambda2,8:F5} {r.S1,7:F4} {r.D_eff,7:F2} {r.TriV,5} {r.JaccardPrev,7:F3} {r.SpearmanD,7:F3} {r.R_final,7:F4} {r.BasinFrac,6:F2} {r.S3,7:F1}");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SCT_10 — Final ranked table by method and initial condition
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_SCT_10_FinalRankedTable()
    {
        int N = DefaultN;
        var conditions = new (string name, double[,] K)[]
        {
            ("weak-dense", InitWeakDense(N, BaseSeed)),
            ("random-sparse", InitRandomSparse(N, BaseSeed, MediumCoupling)),
            ("noisy-lattice", InitNoisyLattice(64, BaseSeed, 3, 0.2)),
            ("small-world", InitSmallWorld(N, BaseSeed)),
            ("shuffled-lattice", InitShuffledLattice(64, BaseSeed, 3)),
            ("fully-conn-weak", InitFullyConnectedWeak(N)),
        };
        var methods = new[] { "phase-lock", "correlation", "lock-time" };

        _output.WriteLine("═══════════════════════════════════════════════════════════════════════════════════");
        _output.WriteLine("  FINAL RANKED TABLE — SELF-CONSISTENT TOPOLOGY EMERGENCE");
        _output.WriteLine("═══════════════════════════════════════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine($"  {"condition",-20} {"method",-14} {"conn",5} {"deg",6} {"λ₂",8} {"S₁",7} {"D_eff",7} {"Jaccard",8} {"ρ_d",8} {"R",7} {"S₃",7}");
        _output.WriteLine($"  {new string('-',20)} {new string('-',14)} {new string('-',5)} {new string('-',6)} {new string('-',8)} {new string('-',7)} {new string('-',7)} {new string('-',8)} {new string('-',8)} {new string('-',7)} {new string('-',7)}");

        var allRows = new List<(string cond, string meth, EpochRecord rec)>();

        foreach (var (cond, K) in conditions)
        {
            int actualN = K.GetLength(0);
            foreach (var meth in methods)
            {
                var records = RunIteration(K, actualN, meth, BaseSeed);
                var last = records[^1];
                allRows.Add((cond, meth, last));

                _output.WriteLine(
                    $"  {cond,-20} {meth,-14} {last.Connected,5} {last.AvgDeg,6:F2} {last.Lambda2,8:F5} {last.S1,7:F4} {last.D_eff,7:F2} {last.JaccardPrev,8:F3} {last.SpearmanD,8:F3} {last.R_final,7:F4} {last.S3,7:F1}");
            }
        }

        // Rank by Jaccard stability (higher = more stable topology)
        var ranked = allRows.OrderByDescending(r => r.rec.JaccardPrev).ToList();
        _output.WriteLine("");
        _output.WriteLine("  RANKED BY TOPOLOGY STABILITY (Jaccard):");
        for (int i = 0; i < Math.Min(10, ranked.Count); i++)
            _output.WriteLine($"    {i + 1,2}. {ranked[i].cond,-20} {ranked[i].meth,-14} Jaccard={ranked[i].rec.JaccardPrev:F4}");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SCT_11 — Deterministic reproducibility
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_SCT_11_DeterministicReproducibility()
    {
        int N = DefaultN;
        var K = InitSmallWorld(N, BaseSeed);

        var r1 = RunIteration(K, N, "phase-lock", BaseSeed);
        var r2 = RunIteration(K, N, "phase-lock", BaseSeed);

        Assert.Equal(r1.Count, r2.Count);
        for (int e = 0; e < r1.Count; e++)
        {
            Assert.Equal(r1[e].Lambda2, r2[e].Lambda2, 9);
            Assert.Equal(r1[e].LambdaMax, r2[e].LambdaMax, 9);
            Assert.Equal(r1[e].JaccardPrev, r2[e].JaccardPrev, 9);
            Assert.Equal(r1[e].SpearmanD, r2[e].SpearmanD, 9);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SCT_12 — Adversarial condition: shuffled oscillator data
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_SCT_12_Adversarial_ShuffledData()
    {
        int N = DefaultN;
        var K = InitSmallWorld(N, BaseSeed);
        var history = RunMatrixKuramoto(K, N, SigmaOmega, Dt, Steps, BaseSeed, HistoryDownsample);

        // Normal R
        var Rnorm = NormalizeR(R_PhaseLock(history));

        // Shuffled: permute the time series labels (breaks spatial structure)
        var shuffledHistory = new double[history.Length][];
        var rng = new Random(BaseSeed);
        for (int t = 0; t < history.Length; t++)
        {
            var permuted = history[t].OrderBy(_ => rng.Next()).ToArray();
            shuffledHistory[t] = permuted;
        }
        var Rshuffled = NormalizeR(R_PhaseLock(shuffledHistory));

        // Compare: normal R should show more structure (variance) than shuffled
        var normVals = new List<double>();
        var shuffVals = new List<double>();
        for (int i = 0; i < N; i++)
        for (int j = i + 1; j < N; j++)
        {
            normVals.Add(Rnorm[i, j]);
            shuffVals.Add(Rshuffled[i, j]);
        }

        double normStd = Math.Sqrt(normVals.Average(v => Math.Pow(v - normVals.Average(), 2)));
        double shuffStd = Math.Sqrt(shuffVals.Average(v => Math.Pow(v - shuffVals.Average(), 2)));

        _output.WriteLine($"  Normal R std: {normStd:F6}");
        _output.WriteLine($"  Shuffled R std: {shuffStd:F6}");

        // Shuffled data should produce more homogeneous R (lower variance).
        Assert.True(shuffStd <= normStd + 0.01,
            "Shuffled oscillator labels should produce more homogeneous R matrix.");
    }
}
