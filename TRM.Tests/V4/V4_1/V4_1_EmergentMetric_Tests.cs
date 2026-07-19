using Xunit;
using Xunit.Abstractions;
using TRM.Core.V4_1.Graphs;

namespace TRM.Tests.V4_1;

/// <summary>
/// Investigates whether spatial geometry emerges from the TRM Temporal Rate Matrix R[i,j].
///
/// Two distance candidates are evaluated:
///   d_inv(i,j) = 1 / R(i,j)
///   d_ln(i,j)  = -ln(R(i,j))
///
/// The rate matrix is constructed from graph topology:
///   R[i,j] = exp(-d_graph(i,j) / xi)
/// where d_graph is the shortest-path distance on a regular lattice in D dimensions,
/// and xi is a correlation length (xi = 1.0 by default).
///
/// Each candidate is tested against the four metric axioms for D ∈ {1, 2, 3, 4, 5, 6}.
/// Weighted graphs are built from the distance candidates, and diagnostics
/// (shortest-path distribution, spectral dimension, clustering coefficient, diameter)
/// are reported without assuming D = 3.
///
/// Classification: EXPLORATORY — investigates emergent metric candidates.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_EmergentMetric")]
public class V4_1_EmergentMetric_Tests
{
    private readonly ITestOutputHelper _output;

    public V4_1_EmergentMetric_Tests(ITestOutputHelper output)
    {
        _output = output;
    }

    // ── Configuration ──────────────────────────────────────────────────────

    /// <summary>
    /// Correlation length for the exponential rate model.
    /// R = exp(-d / xi). xi = 1 gives R ∈ (0, 1] with R=1 for self-pairs.
    /// </summary>
    private const double Xi = 1.0;

    /// <summary>
    /// Graph size per dimension. Balanced for tractability up to D=6.
    /// </summary>
    private static readonly int[] GraphSizes = [20, 10, 7, 4, 3, 3]; // per-dimension node count

    /// <summary>
    /// Deterministic seed for reproducibility.
    /// </summary>
    private const int BaseSeed = 42;

    // ── Graph generation for D ∈ {1, 2, 3, 4, 5, 6} ────────────────────────

    private static GraphTopology BuildRegularGraph(int D, int n)
    {
        return D switch
        {
            1 => GraphFactory.Chain(n),
            2 => GraphFactory.SquareGrid(n),
            3 => GraphFactory.CubicLattice(n),
            4 => GraphFactory.Hypercubic4D(n),
            5 => BuildHypercubic(5, n),
            6 => BuildHypercubic(6, n),
            _ => throw new ArgumentOutOfRangeException(nameof(D))
        };
    }

    /// <summary>Builds a D-dimensional hypercubic lattice of side length n with open boundaries.</summary>
    private static GraphTopology BuildHypercubic(int D, int n)
    {
        if (D < 1 || n < 2)
            throw new ArgumentOutOfRangeException();

        int N = (int)Math.Pow(n, D);
        var adj = new int[N][];

        for (int idx = 0; idx < N; idx++)
        {
            var nbrs = new List<int>(2 * D);

            // Decode linear index → multi-index.
            int rem = idx;
            for (int dim = 0; dim < D; dim++)
            {
                int coord = rem % n;
                rem /= n;

                // Step backwards
                if (coord > 0)
                {
                    int backIdx = idx - PowN(n, dim);
                    nbrs.Add(backIdx);
                }
                // Step forwards
                if (coord < n - 1)
                {
                    int fwdIdx = idx + PowN(n, dim);
                    nbrs.Add(fwdIdx);
                }
            }

            adj[idx] = nbrs.ToArray();
        }

        return new GraphTopology(adj);
    }

    private static int PowN(int n, int exponent)
    {
        int result = 1;
        for (int i = 0; i < exponent; i++)
            result *= n;
        return result;
    }

    // ── Rate matrix R[i,j] ─────────────────────────────────────────────────

    /// <summary>
    /// Builds the Temporal Rate Matrix from graph topology.
    /// R[i,j] = exp(-d(i,j) / xi)  where d(i,j) is the unweighted graph distance.
    /// R[i,i] = 1.0 (perfect self-resonance).
    /// </summary>
    private static double[,] BuildRateMatrix(GraphTopology g, double xi)
    {
        int N = g.NodeCount;
        var R = new double[N, N];

        for (int i = 0; i < N; i++)
        {
            // BFS from node i to all others
            var dist = new int[N];
            Array.Fill(dist, -1);
            dist[i] = 0;
            var queue = new Queue<int>();
            queue.Enqueue(i);

            while (queue.Count > 0)
            {
                int u = queue.Dequeue();
                foreach (int v in g.Neighbours(u))
                {
                    if (dist[v] == -1)
                    {
                        dist[v] = dist[u] + 1;
                        queue.Enqueue(v);
                    }
                }
            }

            for (int j = 0; j < N; j++)
            {
                if (dist[j] >= 0)
                    R[i, j] = Math.Exp(-dist[j] / xi);
                else
                    R[i, j] = 0.0; // disconnected
            }
        }
        return R;
    }

    // ── Distance candidates ─────────────────────────────────────────────────

    private static double[,] DistanceInv(double[,] R)
    {
        int N = R.GetLength(0);
        var d = new double[N, N];
        for (int i = 0; i < N; i++)
        for (int j = 0; j < N; j++)
        {
            if (R[i, j] > 1e-100)
                d[i, j] = 1.0 / R[i, j];
            else
                d[i, j] = double.PositiveInfinity;
        }
        return d;
    }

    private static double[,] DistanceLn(double[,] R)
    {
        int N = R.GetLength(0);
        var d = new double[N, N];
        for (int i = 0; i < N; i++)
        for (int j = 0; j < N; j++)
        {
            if (R[i, j] > 1e-100)
                d[i, j] = -Math.Log(R[i, j]);
            else
                d[i, j] = double.PositiveInfinity;
        }
        return d;
    }

    // ── Metric validation ───────────────────────────────────────────────────

    private enum MetricViolation { None, NonNegativity, IdentityOfIndiscernibles, Symmetry, TriangleInequality }

    private static List<(int i, int j, int k, MetricViolation violation, double value)>
        ValidateMetric(double[,] d, double tol = 1e-9, int triangleSample = -1)
    {
        int N = d.GetLength(0);
        var violations = new List<(int, int, int, MetricViolation, double)>();

        // Non-negativity, identity, symmetry — O(N²), always exhaustive
        for (int i = 0; i < N; i++)
        for (int j = 0; j < N; j++)
        {
            if (d[i, j] < -tol)
                violations.Add((i, j, -1, MetricViolation.NonNegativity, d[i, j]));

            if (i == j)
            {
                if (Math.Abs(d[i, j]) > tol)
                    violations.Add((i, j, -1, MetricViolation.IdentityOfIndiscernibles, d[i, j]));
            }
            else
            {
                if (Math.Abs(d[i, j]) <= tol)
                    violations.Add((i, j, -1, MetricViolation.IdentityOfIndiscernibles, d[i, j]));
            }

            if (Math.Abs(d[i, j] - d[j, i]) > tol)
                violations.Add((i, j, -1, MetricViolation.Symmetry, Math.Abs(d[i, j] - d[j, i])));
        }

        // Triangle inequality — sample if requested, else exhaustive O(N³)
        if (triangleSample > 0 && N > 20)
        {
            var rng = new Random(BaseSeed);
            for (int s = 0; s < triangleSample; s++)
            {
                int i = rng.Next(N), j = rng.Next(N), k = rng.Next(N);
                double sum = d[i, j] + d[j, k];
                if (d[i, k] > sum + tol)
                    violations.Add((i, j, k, MetricViolation.TriangleInequality, d[i, k] - sum));
            }
        }
        else
        {
            for (int i = 0; i < N; i++)
            for (int j = 0; j < N; j++)
            for (int k = 0; k < N; k++)
            {
                double sum = d[i, j] + d[j, k];
                if (d[i, k] > sum + tol)
                    violations.Add((i, j, k, MetricViolation.TriangleInequality, d[i, k] - sum));
            }
        }

        return violations;
    }

    // ── Weighted graph construction ─────────────────────────────────────────

    /// <summary>
    /// Builds a weighted adjacency matrix from the distance candidate.
    /// Only edges corresponding to nearest-neighbour graph edges receive weight = distance.
    /// </summary>
    private static double[,] BuildWeightedGraph(GraphTopology g, double[,] dist)
    {
        int N = g.NodeCount;
        var W = new double[N, N];
        for (int i = 0; i < N; i++)
        for (int j = 0; j < N; j++)
            W[i, j] = double.PositiveInfinity;

        for (int i = 0; i < N; i++)
        {
            W[i, i] = 0;
            foreach (int j in g.Neighbours(i))
                W[i, j] = dist[i, j];
        }
        return W;
    }

    /// <summary>
    /// Floyd-Warshall all-pairs shortest path on weighted adjacency.
    /// </summary>
    private static double[,] AllPairsShortestPath(double[,] W)
    {
        int N = W.GetLength(0);
        var sp = (double[,])W.Clone();
        for (int k = 0; k < N; k++)
        for (int i = 0; i < N; i++)
        for (int j = 0; j < N; j++)
            if (sp[i, k] + sp[k, j] < sp[i, j])
                sp[i, j] = sp[i, k] + sp[k, j];
        return sp;
    }

    // ── Graph diagnostics ───────────────────────────────────────────────────

    private static double SpectralDimensionEstimate(GraphTopology g, int origin, int maxR)
    {
        // N(r) ∝ r^D  →  fit log N(r) = D * log r + C
        var volumes = GraphMetrics.ShellGrowth(g, origin, maxR);

        // Use interior shells: skip r=0 (handled by ShellGrowth starting at 1),
        // and avoid the outer boundary where finite-size effects dominate.
        int start = Math.Max(0, maxR / 4);
        int end = Math.Min(maxR * 3 / 4, maxR);
        if (end <= start) { start = 0; end = maxR; }

        double sx = 0, sy = 0, sxy = 0, sx2 = 0;
        int pts = 0;
        for (int r = start; r < end; r++)
        {
            if (volumes[r] < 1) continue;
            double lr = Math.Log(r + 1);
            double lv = Math.Log(volumes[r]);
            sx += lr; sy += lv; sxy += lr * lv; sx2 += lr * lr;
            pts++;
        }
        if (pts < 2) return double.NaN; // not enough data
        return (pts * sxy - sx * sy) / (pts * sx2 - sx * sx);
    }

    private static double ClusteringCoefficient(GraphTopology g)
    {
        int N = g.NodeCount;
        double total = 0;
        for (int i = 0; i < N; i++)
        {
            var nbrs = g.Neighbours(i);
            int deg = nbrs.Length;
            if (deg < 2) continue;

            var nbrSet = new HashSet<int>(nbrs);
            int triangles = 0;
            foreach (int u in nbrs)
            foreach (int v in g.Neighbours(u))
                if (v > u && nbrSet.Contains(v))
                    triangles++;

            total += (double)triangles / (deg * (deg - 1) / 2.0);
        }
        return total / N;
    }

    private static int GraphDiameter(GraphTopology g)
    {
        int N = g.NodeCount;
        int diameter = 0;
        for (int i = 0; i < N; i++)
        {
            var dist = new int[N];
            Array.Fill(dist, -1);
            dist[i] = 0;
            var q = new Queue<int>();
            q.Enqueue(i);
            while (q.Count > 0)
            {
                int u = q.Dequeue();
                foreach (int v in g.Neighbours(u))
                    if (dist[v] == -1)
                    {
                        dist[v] = dist[u] + 1;
                        diameter = Math.Max(diameter, dist[v]);
                        q.Enqueue(v);
                    }
            }
        }
        return diameter;
    }

    // ── Tests ───────────────────────────────────────────────────────────────

    /// <summary>
    /// EM_01 — Rate matrix symmetry and bounds.
    /// R[i,j] must be symmetric and in [0, 1].
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void V4_1_EM_01_RateMatrix_IsSymmetricAndBoundedInZeroOne(int D)
    {
        int n = GraphSizes[D - 1];
        var g = BuildRegularGraph(D, n);
        var R = BuildRateMatrix(g, Xi);
        int N = g.NodeCount;

        for (int i = 0; i < N; i++)
        for (int j = 0; j < N; j++)
        {
            // Symmetry
            Assert.Equal(R[i, j], R[j, i], 12);

            // Bounds
            Assert.True(R[i, j] >= 0.0 && R[i, j] <= 1.0,
                $"R[{i},{j}]={R[i, j]:E3} outside [0,1] at D={D}.");
        }

        // Diagonal must be 1 (self-resonance)
        for (int i = 0; i < N; i++)
            Assert.Equal(1.0, R[i, i], 12);
    }

    /// <summary>
    /// EM_02 — Rate matrix decays with graph distance.
    /// Monotonic: if d(i,j) > d(i,k) then R[i,j] < R[i,k].
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void V4_1_EM_02_RateMatrix_DecaysMonotonicallyWithDistance(int D)
    {
        int n = GraphSizes[D - 1];
        var g = BuildRegularGraph(D, n);
        var R = BuildRateMatrix(g, Xi);
        int N = g.NodeCount;

        // Sample pairs and verify monotonicity
        for (int i = 0; i < Math.Min(N, 100); i++)
        {
            int d1 = GraphMetrics.ShortestPath(g, i, (i + 1) % N);
            int d2 = GraphMetrics.ShortestPath(g, i, (i + 2) % N);
            if (d1 >= 0 && d2 >= 0 && d1 < d2)
                Assert.True(R[i, (i + 1) % N] > R[i, (i + 2) % N],
                    $"R should decay with distance at D={D}, i={i}.");
        }
    }

    /// <summary>
    /// EM_03 — d_ln = -ln(R) recovers graph distance exactly.
    /// Because R = exp(-d/xi), we have -ln(R) = d/xi.
    /// This must hold for all D.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void V4_1_EM_03_DistanceLn_RecoversGraphDistance(int D)
    {
        int n = GraphSizes[D - 1];
        var g = BuildRegularGraph(D, n);
        var R = BuildRateMatrix(g, Xi);
        var dLn = DistanceLn(R);
        int N = g.NodeCount;

        for (int i = 0; i < N; i++)
        for (int j = 0; j < N; j++)
        {
            int graphDist = GraphMetrics.ShortestPath(g, i, j);
            if (graphDist < 0) continue; // disconnected (should not happen)
            Assert.Equal(graphDist / Xi, dLn[i, j], 9);
        }
    }

    /// <summary>
    /// EM_04 — d_ln = -ln(R) satisfies all metric axioms for all D.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void V4_1_EM_04_DistanceLn_SatisfiesAllMetricAxioms(int D)
    {
        int n = GraphSizes[D - 1];
        var g = BuildRegularGraph(D, n);
        var R = BuildRateMatrix(g, Xi);
        var dLn = DistanceLn(R);

        // For large N, sample triangle inequality (graph distance is known to be a metric)
        int N = g.NodeCount;
        var violations = ValidateMetric(dLn, triangleSample: N > 100 ? 20000 : -1);

        Assert.Empty(violations);
    }

    /// <summary>
    /// EM_05 — d_inv = 1/R violates both identity and triangle inequality for all D.
    ///
    /// Identity violation: R[i,i] = 1, so d_inv[i,i] = 1/1 = 1 ≠ 0.
    /// Self-distance must be 0 for a metric — this is a direct violation.
    ///
    /// Triangle inequality: since d_inv = exp(d/xi), it grows exponentially with distance.
    /// exp(a+b) = exp(a)·exp(b) > exp(a) + exp(b) for positive a,b.
    /// Therefore d_inv(i,k) = exp(d_ik) > exp(d_ij) + exp(d_jk) in general,
    /// which is a triangle inequality VIOLATION (distance is too large).
    ///
    /// This test verifies both violations exist for all D.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void V4_1_EM_05_DistanceInv_ViolatesTriangleInequality(int D)
    {
        int n = GraphSizes[D - 1];
        var g = BuildRegularGraph(D, n);
        var R = BuildRateMatrix(g, Xi);
        var dInv = DistanceInv(R);

        // Sample triangle inequality for large N (d_inv violations are abundant)
        int N = g.NodeCount;
        var violations = ValidateMetric(dInv, triangleSample: N > 100 ? 10000 : -1);

        // Non-negativity and symmetry should hold
        var nonNegativityViolations = violations.Where(v => v.violation == MetricViolation.NonNegativity).ToList();
        var symmetryViolations = violations.Where(v => v.violation == MetricViolation.Symmetry).ToList();
        var identityViolations = violations.Where(v => v.violation == MetricViolation.IdentityOfIndiscernibles).ToList();
        var triangleViolations = violations.Where(v => v.violation == MetricViolation.TriangleInequality).ToList();

        Assert.Empty(nonNegativityViolations);
        Assert.Empty(symmetryViolations);

        // Identity violations: R[i,i] = 1 → d_inv[i,i] = 1 ≠ 0
        // There should be exactly N identity violations (one per diagonal element)
        Assert.Equal(N, identityViolations.Count);

        // Triangle inequality should have violations (abundant for d_inv = exp(d/xi))
        Assert.NotEmpty(triangleViolations);

        _output.WriteLine($"  D={D}: d_inv identity violations: {identityViolations.Count}, triangle violations: {triangleViolations.Count}");
    }

    /// <summary>
    /// EM_06 — Full metric report: d_ln vs d_inv across D = 1..6.
    /// This is the summary test that reports all findings.
    /// </summary>
    [Fact]
    public void V4_1_EM_06_FullMetricReport_AcrossAllDimensions()
    {
        _output.WriteLine("═══════════════════════════════════════════════════════════");
        _output.WriteLine("  EMERGENT METRIC REPORT — TRM Temporal Rate Matrix R[i,j]");
        _output.WriteLine("  R[i,j] = exp(-d_graph(i,j) / xi), xi = 1.0");
        _output.WriteLine("═══════════════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine($"  {"D",3} {"N",6} {"d_ln metric?",12} {"d_inv metric?",13} {"d_ln tri-ineq err",17} {"d_inv tri-ineq err",18}");
        _output.WriteLine($"  {new string('-',3)} {new string('-',6)} {new string('-',12)} {new string('-',13)} {new string('-',17)} {new string('-',18)}");

        for (int D = 1; D <= 6; D++)
        {
            int n = GraphSizes[D - 1];
            var g = BuildRegularGraph(D, n);
            var R = BuildRateMatrix(g, Xi);
            var dLn = DistanceLn(R);
            var dInv = DistanceInv(R);
            int N = g.NodeCount;

            var violationsLn = ValidateMetric(dLn, triangleSample: N > 100 ? 20000 : -1);
            var violationsInv = ValidateMetric(dInv, triangleSample: N > 100 ? 5000 : -1);

            double maxTriLn = violationsLn
                .Where(v => v.violation == MetricViolation.TriangleInequality)
                .Select(v => v.value).DefaultIfEmpty(0).Max();
            double maxTriInv = violationsInv
                .Where(v => v.violation == MetricViolation.TriangleInequality)
                .Select(v => v.value).DefaultIfEmpty(0).Max();

            bool lnMetric = !violationsLn.Any(v => v.violation != MetricViolation.None);
            bool invMetric = !violationsInv.Any(v => v.violation != MetricViolation.None);

            _output.WriteLine($"  {D,3} {N,6} {lnMetric,12} {invMetric,13} {maxTriLn,17:E3} {maxTriInv,18:E3}");
        }

        _output.WriteLine("");
        _output.WriteLine("  FINDING: d_ln = -ln(R) is a valid metric for all D.");
        _output.WriteLine("  FINDING: d_inv = 1/R violates triangle inequality for all D.");
        _output.WriteLine("  The -ln(R) distance exactly recovers the graph distance / xi.");
    }

    /// <summary>
    /// EM_07 — Weighted graph shortest-path distribution.
    /// Uses d_ln as edge weight and computes all-pairs shortest paths.
    /// Since d_ln ≡ graph_distance / xi on edges, the weighted shortest
    /// path should equal the unweighted distance / xi.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void V4_1_EM_07_WeightedShortestPath_MatchesGraphDistance(int D)
    {
        int n = GraphSizes[D - 1];
        var g = BuildRegularGraph(D, n);
        var R = BuildRateMatrix(g, Xi);
        var dLn = DistanceLn(R);
        var W = BuildWeightedGraph(g, dLn);
        var sp = AllPairsShortestPath(W);
        int N = g.NodeCount;

        int errors = 0;
        for (int i = 0; i < N; i++)
        for (int j = 0; j < N; j++)
        {
            double expected = GraphMetrics.ShortestPath(g, i, j) / Xi;
            if (Math.Abs(sp[i, j] - expected) > 1e-9)
                errors++;
        }

        Assert.Equal(0, errors);
    }

    /// <summary>
    /// EM_08 — Spectral dimension estimates from N(r) ∝ r^D.
    /// For regular lattices, the spectral dimension should approximately match
    /// the embedding dimension. Tolerances are wide to account for small graph sizes
    /// and boundary effects. Returns NaN if insufficient data.
    /// </summary>
    [Theory]
    [InlineData(1, 0.3, 2.5)]
    [InlineData(2, 0.8, 3.5)]
    [InlineData(3, 1.0, 5.0)]
    [InlineData(4, 1.2, 6.0)]
    [InlineData(5, 1.4, 7.5)]
    [InlineData(6, 1.6, 9.0)]
    public void V4_1_EM_08_SpectralDimension_MatchesEmbeddingDimension(
        int D, double minExpected, double maxExpected)
    {
        int n = GraphSizes[D - 1];
        var g = BuildRegularGraph(D, n);

        // Pick a node near the center of the lattice for the best scaling fit
        int center = FindCentralNode(g);

        // Compute max shell radius for the spectral dimension fit
        int maxR = Math.Max(3, n / 2);

        double dimEstimate = SpectralDimensionEstimate(g, center, maxR);

        Assert.False(double.IsNaN(dimEstimate),
            $"D={D}: spectral dimension is NaN — not enough data points (n={n}, maxR={maxR}).");
        Assert.True(dimEstimate > minExpected && dimEstimate < maxExpected,
            $"D={D}: spectral dimension {dimEstimate:F2} outside [{minExpected}, {maxExpected}].");
    }

    /// <summary>
    /// Finds a node near the geometric center of the lattice for best shell-growth statistics.
    /// </summary>
    private static int FindCentralNode(GraphTopology g)
    {
        int N = g.NodeCount;
        // Use node with max BFS tree height ≈ center (avoids boundary early truncation)
        int bestNode = N / 2;
        int bestMaxDist = 0;
        for (int i = 0; i < Math.Min(N, 10); i++)
        {
            int candidate = i * N / Math.Min(N, 10);
            int maxDist = 0;
            var dist = new int[N];
            Array.Fill(dist, -1);
            dist[candidate] = 0;
            var q = new Queue<int>();
            q.Enqueue(candidate);
            while (q.Count > 0)
            {
                int u = q.Dequeue();
                foreach (int v in g.Neighbours(u))
                    if (dist[v] == -1)
                    {
                        dist[v] = dist[u] + 1;
                        maxDist = Math.Max(maxDist, dist[v]);
                        q.Enqueue(v);
                    }
            }
            if (maxDist > bestMaxDist)
            {
                bestMaxDist = maxDist;
                bestNode = candidate;
            }
        }
        return bestNode;
    }

    /// <summary>
    /// EM_09 — Clustering coefficient for regular lattices.
    /// Regular lattices have zero clustering (no triangles) except when
    /// nearest neighbours can form cycles. A 1D chain has C=0.
    /// 2D square grid also has C=0 (no diagonal edges).
    /// All D ≥ 1 regular lattices with only axis-aligned edges have C = 0.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void V4_1_EM_09_ClusteringCoefficient_IsZeroForRegularLattices(int D)
    {
        int n = GraphSizes[D - 1];
        var g = BuildRegularGraph(D, n);
        double C = ClusteringCoefficient(g);

        Assert.Equal(0.0, C, 12);
    }

    /// <summary>
    /// EM_10 — Graph diameter scales as n·D for regular lattices.
    /// For open-boundary lattices, diameter ≈ D·(n−1).
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void V4_1_EM_10_GraphDiameter_ScalesWithDimension(int D)
    {
        int n = GraphSizes[D - 1];
        var g = BuildRegularGraph(D, n);
        int diameter = GraphDiameter(g);

        int expectedMax = D * (n - 1);

        Assert.True(diameter <= expectedMax,
            $"D={D}: diameter={diameter} exceeds D·(n-1)={expectedMax}.");
        Assert.True(diameter > 0,
            $"D={D}: diameter should be positive.");
    }

    /// <summary>
    /// EM_11 — Rate matrix eigenvalue spectrum.
    /// The spectrum of R encodes structural information about
    /// the coupling network.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void V4_1_EM_11_RateMatrixEigenvalues_ArePositiveAndBounded(int D)
    {
        int n = GraphSizes[D - 1];
        var g = BuildRegularGraph(D, n);
        var R = BuildRateMatrix(g, Xi);

        var ev = DenseSymmetricEigenSolver.Eigenvalues(R);

        // All eigenvalues should be >= 0 (R is a Gram-like matrix with positive entries)
        for (int i = 0; i < ev.Length; i++)
            Assert.True(ev[i] >= -1e-10, $"D={D}: eigenvalue {i} = {ev[i]:E3} is negative.");

        // Largest eigenvalue bounded by N (since |R_ij| ≤ 1)
        Assert.True(ev[^1] <= g.NodeCount + 1e-9,
            $"D={D}: λ_max = {ev[^1]:E3} exceeds N = {g.NodeCount}.");
    }

    /// <summary>
    /// EM_12 — Consistent emergent metric conclusion.
    ///
    /// d_ln = -ln(R) is a VALID metric (satisfies all four axioms).
    /// d_inv = 1/R is NOT a metric (violates triangle inequality).
    ///
    /// Because R[i,j] = exp(-d_graph(i,j) / xi), we have:
    ///   d_ln(i,j) = d_graph(i,j) / xi
    /// which is exactly proportional to the graph distance.
    ///
    /// CONCLUSION: The -ln(R) distance candidate defines a consistent metric
    /// for ALL D ∈ {1, 2, 3, 4, 5, 6}. The 1/R candidate does not.
    ///
    /// The -ln(R) metric is equivalent to the graph distance up to a scale
    /// factor xi. This means: IF the TRM rate matrix R[i,j] follows the
    /// exponential form R = exp(-d/xi), THEN the emergent spatial metric
    /// is the graph distance itself, and the spatial dimension D is the
    /// embedding dimension of the oscillator coupling graph.
    ///
    /// The theory does NOT internally select D = 3 via the metric properties
    /// alone — metric consistency holds for all D. The dimensional selection
    /// must come from dynamical constraints (synchronization stability,
    /// bridge-band structure) as explored in V4_1_DimensionalStructure_Tests.
    /// </summary>
    [Fact]
    public void V4_1_EM_12_ConsistentEmergentMetric_Conclusion()
    {
        _output.WriteLine("═══════════════════════════════════════════════════════════");
        _output.WriteLine("  CONCLUSION: CONSISTENT EMERGENT METRIC");
        _output.WriteLine("═══════════════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  Rate model: R[i,j] = exp(-d_graph(i,j) / xi)");
        _output.WriteLine("");
        _output.WriteLine("  ┌─────────────────────┬─────────────────────────────────┐");
        _output.WriteLine("  │ d_ln  = -ln(R)      │ VALID METRIC for D = 1..6       │");
        _output.WriteLine("  │                     │ ≡ d_graph / xi                  │");
        _output.WriteLine("  │                     │ All four axioms satisfied.      │");
        _output.WriteLine("  ├─────────────────────┼─────────────────────────────────┤");
        _output.WriteLine("  │ d_inv = 1/R         │ NOT A METRIC                    │");
        _output.WriteLine("  │                     │ Violates triangle inequality.   │");
        _output.WriteLine("  │                     │ d_inv = exp(d_graph/xi)         │");
        _output.WriteLine("  │                     │ Exponential → super-additive.   │");
        _output.WriteLine("  └─────────────────────┴─────────────────────────────────┘");
        _output.WriteLine("");
        _output.WriteLine("  PHYSICAL INTERPRETATION:");
        _output.WriteLine("  If resonance rates decay exponentially with graph distance,");
        _output.WriteLine("  then -ln(R) IS the emergent spatial distance.");
        _output.WriteLine("  The metric does NOT select D = 3 — it works for all D.");
        _output.WriteLine("  Dimensional selection must come from dynamics.");
        _output.WriteLine("");

        // Verify the conclusion with actual data
        bool allDLnValid = true;
        bool anyDInvValid = false;

        for (int D = 1; D <= 6; D++)
        {
            int n = GraphSizes[D - 1];
            var g = BuildRegularGraph(D, n);
            var R = BuildRateMatrix(g, Xi);
            var dLn = DistanceLn(R);
            var dInv = DistanceInv(R);

            int N = g.NodeCount;
            var vLn = ValidateMetric(dLn, triangleSample: N > 100 ? 20000 : -1);
            var vInv = ValidateMetric(dInv, triangleSample: N > 100 ? 5000 : -1);

            bool lnValid = !vLn.Any(vi => vi.violation != MetricViolation.None);
            bool invValid = !vInv.Any(vi => vi.violation != MetricViolation.None);

            allDLnValid = allDLnValid && lnValid;
            anyDInvValid = anyDInvValid || invValid;

            _output.WriteLine($"  D={D}: d_ln metric = {lnValid}, d_inv metric = {invValid}");
        }

        Assert.True(allDLnValid, "d_ln = -ln(R) must be a valid metric for all D.");
        Assert.False(anyDInvValid, "d_inv = 1/R must NOT be a valid metric for any D.");
    }

    /// <summary>
    /// EM_13 — Deterministic reproducibility.
    /// Two independent builds must produce identical results.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(6)]
    public void V4_1_EM_13_ResultsAreDeterministic(int D)
    {
        int n = GraphSizes[D - 1];
        var g1 = BuildRegularGraph(D, n);
        var g2 = BuildRegularGraph(D, n);

        var R1 = BuildRateMatrix(g1, Xi);
        var R2 = BuildRateMatrix(g2, Xi);

        int N = R1.GetLength(0);
        for (int i = 0; i < N; i++)
        for (int j = 0; j < N; j++)
            Assert.Equal(R1[i, j], R2[i, j], 12);

        var dLn1 = DistanceLn(R1);
        var dLn2 = DistanceLn(R2);

        for (int i = 0; i < N; i++)
        for (int j = 0; j < N; j++)
            Assert.Equal(dLn1[i, j], dLn2[i, j], 12);
    }

    /// <summary>
    /// EM_14 — Falsification: if rate matrix is altered (e.g., multiplicative noise),
    /// the metric properties change accordingly. This test injects small perturbations
    /// and verifies the framework reports the change.
    /// </summary>
    [Theory]
    [InlineData(3)]
    public void V4_1_EM_14_Falsification_PerturbedRateBreaksSymmetry(int D)
    {
        int n = GraphSizes[D - 1];
        var g = BuildRegularGraph(D, n);
        var R = BuildRateMatrix(g, Xi);
        int N = R.GetLength(0);

        // Perturb: break symmetry by adding tiny antisymmetric noise
        var Rpert = (double[,])R.Clone();
        var rng = new Random(BaseSeed);
        for (int i = 0; i < N; i++)
        for (int j = i + 1; j < N; j++)
        {
            double noise = rng.NextDouble() * 1e-6;
            Rpert[i, j] += noise;
            // Do NOT add to Rpert[j,i] — this breaks symmetry
        }

        var dLnPert = DistanceLn(Rpert);
        int Npert = Rpert.GetLength(0);
        var violations = ValidateMetric(dLnPert, tol: 1e-9, triangleSample: Npert > 100 ? 20000 : -1);

        // Symmetry should now be violated
        var symViolations = violations
            .Where(v => v.violation == MetricViolation.Symmetry)
            .ToList();

        Assert.NotEmpty(symViolations);
    }

    /// <summary>
    /// EM_15 — Different xi values produce consistent metrics.
    /// The metric property is independent of xi (scale invariance).
    /// </summary>
    [Theory]
    [InlineData(1, 0.5)]
    [InlineData(1, 2.0)]
    [InlineData(3, 0.5)]
    [InlineData(3, 2.0)]
    [InlineData(6, 0.5)]
    [InlineData(6, 2.0)]
    public void V4_1_EM_15_MetricProperty_IsScaleInvariant(int D, double xi)
    {
        int n = GraphSizes[D - 1];
        var g = BuildRegularGraph(D, n);
        var R = BuildRateMatrix(g, xi);
        var dLn = DistanceLn(R);

        int Nxi = g.NodeCount;
        var violations = ValidateMetric(dLn, triangleSample: Nxi > 100 ? 20000 : -1);

        Assert.Empty(violations);
    }

    /// <summary>
    /// EM_16 — Reporting summary: table of per-dimension diagnostics.
    /// </summary>
    [Fact]
    public void V4_1_EM_16_DiagnosticsTable_AcrossAllDimensions()
    {
        _output.WriteLine("══════════════════════════════════════════════════════════════════════════════════════════");
        _output.WriteLine("  DIAGNOSTICS TABLE — Graph properties by dimension");
        _output.WriteLine("══════════════════════════════════════════════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine($"  {"D",3} {"N",6} {"diam",5} {"avg deg",8} {"λ₂",10} {"λ_max",10} {"S₁(λ₂/λ_max)",14} {"spectral D",11} {"cluster C",10}");
        _output.WriteLine($"  {new string('-',3)} {new string('-',6)} {new string('-',5)} {new string('-',8)} {new string('-',10)} {new string('-',10)} {new string('-',14)} {new string('-',11)} {new string('-',10)}");

        for (int D = 1; D <= 6; D++)
        {
            int n = GraphSizes[D - 1];
            var g = BuildRegularGraph(D, n);
            int N = g.NodeCount;
            int diam = GraphDiameter(g);
            double avgDeg = Enumerable.Range(0, N).Select(i => (double)g.Degree(i)).Average();
            double l2 = GraphMetrics.Lambda2(g);
            double lMax = GraphMetrics.LambdaMax(g);
            double S1 = l2 / lMax;
            int center = FindCentralNode(g);
            int maxR = Math.Max(3, n / 2);
            double specDim = SpectralDimensionEstimate(g, center, maxR);
            double C = ClusteringCoefficient(g);

            _output.WriteLine($"  {D,3} {N,6} {diam,5} {avgDeg,8:F2} {l2,10:F3} {lMax,10:F3} {S1,14:F4} {specDim,11:F2} {C,10:F3}");
        }

        _output.WriteLine("");
        _output.WriteLine("  NOTES:");
        _output.WriteLine("  - avg deg → 2D for interior nodes of regular lattices.");
        _output.WriteLine("  - λ₂ increases with D (more connectivity → faster sync).");
        _output.WriteLine("  - S₁ = λ₂/λ_max may peak at intermediate D (spectral balance hypothesis).");
        _output.WriteLine("  - Clustering C = 0 for all regular axis-aligned lattices (no triangles).");
    }
}
