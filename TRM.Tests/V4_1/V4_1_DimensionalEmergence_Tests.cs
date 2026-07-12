using Xunit;
using Xunit.Abstractions;
using TRM.Core.V4_1.Graphs;
using TRM.Core.V4_1.Sync;

namespace TRM.Tests.V4_1;

/// <summary>
/// Deep analysis: does effective spatial dimension emerge from oscillator dynamics
/// rather than being imposed?
///
/// Six graph topologies are analyzed:
///   1. 1D chain
///   2. 2D square lattice
///   3. 3D cubic lattice
///   4. 4D hypercubic lattice
///   5. Random graph (Erdos–Renyi, fixed average degree ≈ 2D_ref)
///   6. Small-world graph (Watts–Strogatz, rewiring p = 0.1)
///
/// For each topology the following numerical indicators are computed:
///   Spectral:  lambda_2, lambda_max, S1 = lambda_2 / lambda_max
///   Synchronization:  R_final, basin convergence fraction
///   Propagation:  isotropy S2 (directional front-speed variance)
///   Bridge band:  Delta_Omega, S3 = 1 / Delta_Omega
///   Effective dimension:  D_eff = d log N(r) / d log r
///   Correlations:  sync-stability vs D_eff vs bridge-band sharpness
///
/// No D = 3 assumption. Purely numerical evidence.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_DimensionalEmergence")]
public class V4_1_DimensionalEmergence_Tests
{
    private readonly ITestOutputHelper _output;

    private const int BaseSeed = 42;
    private const int GraphNodes = 10;          // nodes per dimension for regular lattices (D≤2)
    private const int RandomSmallWorldNodes = 200; // target node count for random and small-world
    private const double CouplingK = 0.8;        // coupling strength
    private const double FreqSpread = 0.1;       // natural frequency spread
    private const double Dt = 0.05;
    private const int TimeSteps = 300;
    private const int BasinRuns = 20;            // runs for basin fraction
    private const int BridgeRuns = 10;           // runs for bridge-band estimate
    private const int PropagationSamples = 12;   // directions sampled for isotropy

    public V4_1_DimensionalEmergence_Tests(ITestOutputHelper output)
    {
        _output = output;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Graph topology builders
    // ═══════════════════════════════════════════════════════════════════════

    private static GraphTopology BuildRegular(int D, int n) => D switch
    {
        1 => GraphFactory.Chain(Math.Max(2, n)),
        2 => GraphFactory.SquareGrid(Math.Max(2, n)),
        3 => GraphFactory.CubicLattice(Math.Max(2, n)),
        4 => GraphFactory.Hypercubic4D(Math.Max(2, n / 3)),
        _ => throw new ArgumentOutOfRangeException(nameof(D))
    };

    /// <summary>
    /// Erdos–Renyi random graph with target average degree k.
    /// If the graph is disconnected, additional edges are added until connected.
    /// Deterministic via seeded RNG.
    /// </summary>
    private static GraphTopology BuildRandomGraph(int N, double targetAvgDegree, int seed)
    {
        var rng = new Random(seed);
        double p = Math.Min(1.0, targetAvgDegree / (N - 1));
        var adjList = new HashSet<int>[N];
        for (int i = 0; i < N; i++) adjList[i] = new HashSet<int>();

        for (int i = 0; i < N; i++)
        for (int j = i + 1; j < N; j++)
            if (rng.NextDouble() < p)
            {
                adjList[i].Add(j);
                adjList[j].Add(i);
            }

        // Ensure connectivity: add edges between disconnected components.
        var visited = new bool[N];
        var components = new List<List<int>>();
        for (int i = 0; i < N; i++)
        {
            if (visited[i]) continue;
            var comp = new List<int>();
            var q = new Queue<int>();
            visited[i] = true;
            q.Enqueue(i);
            while (q.Count > 0)
            {
                int u = q.Dequeue();
                comp.Add(u);
                foreach (int v in adjList[u])
                    if (!visited[v]) { visited[v] = true; q.Enqueue(v); }
            }
            components.Add(comp);
        }

        // Connect components sequentially.
        for (int c = 1; c < components.Count; c++)
        {
            int a = components[c][0];
            int b = components[c - 1][0];
            adjList[a].Add(b);
            adjList[b].Add(a);
        }

        return new GraphTopology(adjList.Select(h => h.ToArray()).ToArray());
    }

    /// <summary>
    /// Watts–Strogatz small-world graph.
    /// Start with a ring lattice (k neighbours), rewire each edge with probability p.
    /// Deterministic via seeded RNG.
    /// </summary>
    private static GraphTopology BuildSmallWorld(int N, int k, double p, int seed)
    {
        var rng = new Random(seed);
        var adjList = new HashSet<int>[N];
        for (int i = 0; i < N; i++) adjList[i] = new HashSet<int>();

        // Ring lattice: each node connects to k/2 neighbours on each side.
        int halfK = k / 2;
        for (int i = 0; i < N; i++)
        for (int d = 1; d <= halfK; d++)
        {
            int j = (i + d) % N;
            adjList[i].Add(j);
            adjList[j].Add(i);
        }

        // Rewire
        for (int i = 0; i < N; i++)
        {
            var currentNeighbors = adjList[i].ToList();
            foreach (int j in currentNeighbors)
            {
                if (i >= j) continue; // process each edge once
                if (rng.NextDouble() < p)
                {
                    // Remove old edge
                    adjList[i].Remove(j);
                    adjList[j].Remove(i);

                    // Add new edge to random node (avoid self-loops and duplicates)
                    int newJ;
                    do { newJ = rng.Next(N); }
                    while (newJ == i || adjList[i].Contains(newJ));

                    adjList[i].Add(newJ);
                    adjList[newJ].Add(i);
                }
            }
        }

        return new GraphTopology(adjList.Select(h => h.ToArray()).ToArray());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Spectral diagnostics
    // ═══════════════════════════════════════════════════════════════════════

    private static (double lambda2, double lambdaMax, double S1) SpectralDiagnostics(GraphTopology g)
    {
        double l2 = GraphMetrics.Lambda2(g);
        double lMax = GraphMetrics.LambdaMax(g);
        double S1 = lMax > 0 ? l2 / lMax : 0;
        return (l2, lMax, S1);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Synchronization diagnostics
    // ═══════════════════════════════════════════════════════════════════════

    private static (double Rfinal, double basinFraction) RunSyncDiagnostics(GraphTopology g, int seed)
    {
        int N = g.NodeCount;
        var omega = SyncDiagnostics.GenerateNaturalFrequencies(N, FreqSpread, new Random(seed));
        var simCfg = new SyncSimulationConfig(N, CouplingK, FreqSpread, Dt, TimeSteps)
            { SeededRuns = BasinRuns, RandomSeed = seed };

        // Single run for R_final
        var init = new double[N];
        var rngInit = new Random(seed + 1);
        for (int i = 0; i < N; i++)
            init[i] = rngInit.NextDouble() * 2.0 * Math.PI;

        var result = KuramotoGraphSimulator.Run(g, simCfg, omega, init);

        // Basin fraction
        var rngBasin = new Random(seed + 2);
        double basin = SyncDiagnostics.SyncBasinFraction(g, simCfg, omega, rngBasin);

        return (result.FinalOrderParameter, basin);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Propagation isotropy S2
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Estimates propagation isotropy by measuring BFS distances from a center
    /// node in multiple directions and computing S2 = 1 - sigma / mean.
    /// For regular lattices, S2 ≈ 0.95+ (high isotropy).
    /// For 1D chains, S2 is lower due to built-in axis.
    /// </summary>
    private static double PropagationIsotropy(GraphTopology g)
    {
        int N = g.NodeCount;
        if (N < 10) return 1.0; // too small to measure

        // Find a central node.
        int center = FindCentralNode(g);

        // BFS to get distances from center.
        var dist = new int[N];
        Array.Fill(dist, -1);
        dist[center] = 0;
        var q = new Queue<int>();
        q.Enqueue(center);
        while (q.Count > 0)
        {
            int u = q.Dequeue();
            foreach (int v in g.Neighbours(u))
                if (dist[v] == -1) { dist[v] = dist[u] + 1; q.Enqueue(v); }
        }

        // Sample nodes at a fixed distance shell and measure angular variance
        // of their positions in the BFS tree.  Higher variance → less isotropic.
        int targetDist = dist.Max() / 2;
        if (targetDist < 2) targetDist = 2;

        var shellNodes = Enumerable.Range(0, N).Where(i => dist[i] == targetDist).ToList();
        if (shellNodes.Count < 4) return 1.0;

        // For each shell node, measure the shortest path to every other shell node.
        // The variance of these distances across the shell indicates anisotropy.
        var shellDistances = new List<double>();
        int sampleCount = Math.Min(shellNodes.Count, PropagationSamples);
        var rng = new Random(BaseSeed);
        var sampled = shellNodes.OrderBy(_ => rng.Next()).Take(sampleCount).ToList();

        for (int a = 0; a < sampled.Count; a++)
        for (int b = a + 1; b < sampled.Count; b++)
        {
            int d = GraphMetrics.ShortestPath(g, sampled[a], sampled[b]);
            if (d > 0) shellDistances.Add(d);
        }

        if (shellDistances.Count < 2) return 1.0;

        double mean = shellDistances.Average();
        double variance = shellDistances.Sum(d => (d - mean) * (d - mean)) / shellDistances.Count;
        double sigma = Math.Sqrt(variance);

        if (mean < 1e-9) return 1.0;
        double S2 = 1.0 - sigma / mean;
        return Math.Max(0, Math.Min(1, S2));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Bridge band width S3
    // ═══════════════════════════════════════════════════════════════════════

    private static (double deltaOmega, double S3) BridgeBandDiagnostics(GraphTopology g, int seed)
    {
        int N = g.NodeCount;
        var omega = SyncDiagnostics.GenerateNaturalFrequencies(N, FreqSpread, new Random(seed));
        var simCfg = new SyncSimulationConfig(N, CouplingK, FreqSpread, Dt, TimeSteps)
            { SeededRuns = BridgeRuns, RandomSeed = seed };

        var rng = new Random(seed + 3);
        double deltaOmega = SyncDiagnostics.BridgeBandProxy(g, simCfg, omega, rng);
        double S3 = deltaOmega > 0 ? 1.0 / deltaOmega : double.PositiveInfinity;

        return (deltaOmega, S3);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Effective graph dimension D_eff = d log N(r) / d log r
    // ═══════════════════════════════════════════════════════════════════════

    private static double EffectiveDimension(GraphTopology g)
    {
        int N = g.NodeCount;
        int center = FindCentralNode(g);
        int maxR = Math.Min(FindMaxDistance(g, center), 12);
        if (maxR < 3) return double.NaN;

        var volumes = GraphMetrics.ShellGrowth(g, center, maxR);

        // Log-log linear fit over central shells.
        int start = maxR / 4;
        int end = maxR * 3 / 4;
        if (end <= start) { start = 1; end = maxR; }

        double sx = 0, sy = 0, sxy = 0, sx2 = 0;
        int pts = 0;
        for (int r = start; r < end; r++)
        {
            if (volumes[r] < 2) continue;
            double lr = Math.Log(r + 1);
            double lv = Math.Log(volumes[r]);
            sx += lr; sy += lv; sxy += lr * lv; sx2 += lr * lr;
            pts++;
        }
        if (pts < 2) return double.NaN;
        return (pts * sxy - sx * sy) / (pts * sx2 - sx * sx);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════════════

    private static int FindCentralNode(GraphTopology g)
    {
        int N = g.NodeCount;
        int best = N / 2, bestMax = 0;
        int candidates = Math.Min(N, 10);
        for (int i = 0; i < candidates; i++)
        {
            int node = i * N / candidates;
            int maxD = FindMaxDistance(g, node);
            if (maxD > bestMax) { bestMax = maxD; best = node; }
        }
        return best;
    }

    private static int FindMaxDistance(GraphTopology g, int origin)
    {
        int N = g.NodeCount;
        var dist = new int[N];
        Array.Fill(dist, -1);
        dist[origin] = 0;
        var q = new Queue<int>();
        q.Enqueue(origin);
        int maxD = 0;
        while (q.Count > 0)
        {
            int u = q.Dequeue();
            foreach (int v in g.Neighbours(u))
                if (dist[v] == -1)
                {
                    dist[v] = dist[u] + 1;
                    maxD = Math.Max(maxD, dist[v]);
                    q.Enqueue(v);
                }
        }
        return maxD;
    }

    private static bool IsConnected(GraphTopology g)
    {
        int N = g.NodeCount;
        var visited = new bool[N];
        var q = new Queue<int>();
        visited[0] = true;
        q.Enqueue(0);
        while (q.Count > 0)
        {
            int u = q.Dequeue();
            foreach (int v in g.Neighbours(u))
                if (!visited[v]) { visited[v] = true; q.Enqueue(v); }
        }
        return visited.All(v => v);
    }

    /// <summary>Pearson correlation coefficient.</summary>
    private static double Pearson(double[] x, double[] y)
    {
        int n = x.Length;
        double mx = x.Average(), my = y.Average();
        double num = 0, denX = 0, denY = 0;
        for (int i = 0; i < n; i++)
        {
            double dx = x[i] - mx, dy = y[i] - my;
            num += dx * dy;
            denX += dx * dx;
            denY += dy * dy;
        }
        double den = Math.Sqrt(denX * denY);
        return den > 1e-15 ? num / den : 0;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DE_01 — Graph construction and validation
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_DE_01_AllGraphTopologies_AreConstructedAndConnected()
    {
        var graphs = new Dictionary<string, GraphTopology>
        {
            ["1D chain"]      = BuildRegular(1, GraphNodes),
            ["2D lattice"]    = BuildRegular(2, GraphNodes),
            ["3D cubic"]      = BuildRegular(3, GraphNodes),
            ["4D hypercubic"] = BuildRegular(4, GraphNodes),
            ["random"]        = BuildRandomGraph(RandomSmallWorldNodes, 6.0, BaseSeed),
            ["small-world"]   = BuildSmallWorld(RandomSmallWorldNodes, 6, 0.1, BaseSeed),
        };

        foreach (var (name, g) in graphs)
        {
            Assert.True(IsConnected(g), $"{name} must be connected.");
            Assert.True(g.NodeCount >= 2, $"{name} must have at least 2 nodes.");
            _output.WriteLine($"  {name,-18} N={g.NodeCount,5}  connected=OK");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DE_02 — Spectral diagnostics across all topologies
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_DE_02_SpectralDiagnostics_AllTopologies()
    {
        _output.WriteLine("  SPECTRAL DIAGNOSTICS");
        _output.WriteLine($"  {"topology",-18} {"N",6} {"λ₂",10} {"λ_max",10} {"S₁=λ₂/λ_max",14}");
        _output.WriteLine($"  {new string('-',18)} {new string('-',6)} {new string('-',10)} {new string('-',10)} {new string('-',14)}");

        var data = new Dictionary<string, (double l2, double lMax, double S1)>();

        foreach (var (name, g) in GetTopologies())
        {
            var (l2, lMax, S1) = SpectralDiagnostics(g);
            data[name] = (l2, lMax, S1);
            _output.WriteLine($"  {name,-18} {g.NodeCount,6} {l2,10:F5} {lMax,10:F4} {S1,14:F6}");
        }

        // λ₂ > 0 for all connected graphs.
        foreach (var kv in data)
            Assert.True(kv.Value.l2 > 0, $"λ₂ must be positive for connected {kv.Key}.");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DE_03 — Synchronization order parameter R(t) for each topology
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_DE_03_SyncOrderParameter_AllTopologies()
    {
        _output.WriteLine("  SYNCHRONIZATION ORDER PARAMETER");
        _output.WriteLine($"  {"topology",-18} {"R_final",9} {"basin_frac",10} {"synced?",9}");
        _output.WriteLine($"  {new string('-',18)} {new string('-',9)} {new string('-',10)} {new string('-',9)}");

        foreach (var (name, g) in GetTopologies())
        {
            var (Rfinal, basin) = RunSyncDiagnostics(g, BaseSeed);
            _output.WriteLine($"  {name,-18} {Rfinal,9:F4} {basin,10:F3} {Rfinal > 0.9,9}");
            Assert.True(Rfinal >= 0 && Rfinal <= 1.0, "R_final ∈ [0,1]");
            Assert.True(basin >= 0 && basin <= 1.0, "basin ∈ [0,1]");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DE_04 — Propagation isotropy S2
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_DE_04_PropagationIsotropy_AllTopologies()
    {
        _output.WriteLine("  PROPAGATION ISOTROPY S₂ = 1 − σ/μ");
        _output.WriteLine($"  {"topology",-18} {"S₂",8}");
        _output.WriteLine($"  {new string('-',18)} {new string('-',8)}");

        foreach (var (name, g) in GetTopologies())
        {
            double S2 = PropagationIsotropy(g);
            _output.WriteLine($"  {name,-18} {S2,8:F4}");
            Assert.True(S2 >= 0 && S2 <= 1.0, "S₂ ∈ [0,1]");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DE_05 — Bridge band width and S3
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_DE_05_BridgeBand_AllTopologies()
    {
        _output.WriteLine("  BRIDGE-BAND WIDTH ΔΩ AND SHARPNESS S₃");
        _output.WriteLine($"  {"topology",-18} {"ΔΩ",10} {"S₃=1/ΔΩ",10}");
        _output.WriteLine($"  {new string('-',18)} {new string('-',10)} {new string('-',10)}");

        foreach (var (name, g) in GetTopologies())
        {
            var (deltaOmega, S3) = BridgeBandDiagnostics(g, BaseSeed);
            _output.WriteLine($"  {name,-18} {deltaOmega,10:F6} {S3,10:F3}");
            Assert.True(deltaOmega >= 0, "ΔΩ must be non-negative.");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DE_06 — Effective graph dimension D_eff
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_DE_06_EffectiveDimension_AllTopologies()
    {
        _output.WriteLine("  EFFECTIVE GRAPH DIMENSION D_eff = d log N(r) / d log r");
        _output.WriteLine($"  {"topology",-18} {"D_eff",8}");
        _output.WriteLine($"  {new string('-',18)} {new string('-',8)}");

        foreach (var (name, g) in GetTopologies())
        {
            double D_eff = EffectiveDimension(g);
            _output.WriteLine($"  {name,-18} {D_eff,8:F2}");
            Assert.True(double.IsNaN(D_eff) || D_eff > 0, "D_eff must be positive or NaN.");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DE_07 — Full numerical evidence table (combined indicators)
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_DE_07_FullEvidenceTable()
    {
        _output.WriteLine("══════════════════════════════════════════════════════════════════════════════════════════");
        _output.WriteLine("  FULL NUMERICAL EVIDENCE — DIMENSIONAL EMERGENCE FROM OSCILLATOR DYNAMICS");
        _output.WriteLine("══════════════════════════════════════════════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine($"  Coupling K={CouplingK}, freq spread={FreqSpread}, dt={Dt}, steps={TimeSteps}");
        _output.WriteLine($"  Basin runs={BasinRuns}, bridge runs={BridgeRuns}");
        _output.WriteLine("");
        _output.WriteLine($"  {"topology",-18} {"N",6} {"λ₂",9} {"λ_max",8} {"S₁",7} {"R_final",9} {"basin",7} {"S₂",7} {"ΔΩ",9} {"S₃",8} {"D_eff",7}");
        _output.WriteLine($"  {new string('-',18)} {new string('-',6)} {new string('-',9)} {new string('-',8)} {new string('-',7)} {new string('-',9)} {new string('-',7)} {new string('-',7)} {new string('-',9)} {new string('-',8)} {new string('-',7)}");

        var records = new List<(string name, int N, double l2, double lMax, double S1,
            double Rfinal, double basin, double S2, double dOmega, double S3, double Deff)>();

        foreach (var (name, g) in GetTopologies())
        {
            var (l2, lMax, S1) = SpectralDiagnostics(g);
            var (Rfinal, basin) = RunSyncDiagnostics(g, BaseSeed);
            double S2 = PropagationIsotropy(g);
            var (dOmega, S3) = BridgeBandDiagnostics(g, BaseSeed);
            double Deff = EffectiveDimension(g);

            records.Add((name, g.NodeCount, l2, lMax, S1, Rfinal, basin, S2, dOmega, S3, Deff));

            _output.WriteLine(
                $"  {name,-18} {g.NodeCount,6} {l2,9:F5} {lMax,8:F4} {S1,7:F4} {Rfinal,9:F4} {basin,7:F3} {S2,7:F4} {dOmega,9:F6} {S3,8:F2} {Deff,7:F2}");
        }

        _output.WriteLine("");
        _output.WriteLine("  NOTES:");
        _output.WriteLine("  - S₁ = λ₂/λ_max  (spectral balance: higher → better sync structure)");
        _output.WriteLine("  - S₂ = 1 − σ/μ   (propagation isotropy: 1 = perfect)");
        _output.WriteLine("  - S₃ = 1/ΔΩ       (bridge-band sharpness: higher → narrower)");
        _output.WriteLine("  - D_eff            (effective dimension from N(r) ∝ r^D)");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DE_08 — Combined indicator table (aggregated, with ranking)
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_DE_08_CombinedIndicatorTable()
    {
        _output.WriteLine("══════════════════════════════════════════════════════════════════");
        _output.WriteLine("  COMBINED INDICATOR TABLE — WHICH TOPOLOGY MAXIMIZES?");
        _output.WriteLine("══════════════════════════════════════════════════════════════════");
        _output.WriteLine("");

        var names = new List<string>();
        var S1vals = new List<double>();
        var S2vals = new List<double>();
        var S3vals = new List<double>();
        var Rvals = new List<double>();
        var Bvals = new List<double>();
        var Dvals = new List<double>();

        foreach (var (name, g) in GetTopologies())
        {
            var (l2, lMax, S1) = SpectralDiagnostics(g);
            var (Rfinal, basin) = RunSyncDiagnostics(g, BaseSeed);
            double S2 = PropagationIsotropy(g);
            var (dOmega, S3) = BridgeBandDiagnostics(g, BaseSeed);
            double Deff = EffectiveDimension(g);

            names.Add(name);
            S1vals.Add(S1);
            S2vals.Add(S2);
            S3vals.Add(double.IsInfinity(S3) ? 0 : S3);
            Rvals.Add(Rfinal);
            Bvals.Add(basin);
            Dvals.Add(double.IsNaN(Deff) ? 0 : Deff);
        }

        // Normalize each indicator to [0, 1] for fair combination.
        var indicators = new (string label, double[] values)[]
        {
            ("S₁ (spectral)",        S1vals.ToArray()),
            ("S₂ (isotropy)",        S2vals.ToArray()),
            ("S₃ (bridge-band)",     S3vals.ToArray()),
            ("R_final (sync)",       Rvals.ToArray()),
            ("basin (convergence)",  Bvals.ToArray()),
        };

        _output.WriteLine($"  {"topology",-18} {"S₁(λ₂/λ_max)",13} {"S₂(iso)",8} {"S₃(1/ΔΩ)",10} {"R_final",9} {"basin",7} {"D_eff",7} {"combined",10}");
        _output.WriteLine($"  {new string('-',18)} {new string('-',13)} {new string('-',8)} {new string('-',10)} {new string('-',9)} {new string('-',7)} {new string('-',7)} {new string('-',10)}");

        // Compute combined score: average of normalized indicators.
        var combinedScores = new double[names.Count];
        double bestScore = 0;
        string bestName = "";

        for (int i = 0; i < names.Count; i++)
        {
            double sum = 0;
            int count = 0;
            foreach (var (_, values) in indicators)
            {
                double max = values.Max();
                if (max > 1e-15)
                {
                    sum += values[i] / max;
                    count++;
                }
            }
            combinedScores[i] = count > 0 ? sum / count : 0;
            if (combinedScores[i] > bestScore)
            {
                bestScore = combinedScores[i];
                bestName = names[i];
            }

            _output.WriteLine(
                $"  {names[i],-18} {S1vals[i],13:F6} {S2vals[i],8:F4} {S3vals[i],10:F2} {Rvals[i],9:F4} {Bvals[i],7:F3} {Dvals[i],7:F2} {combinedScores[i],10:F4}");
        }

        // Rank
        var ranked = Enumerable.Range(0, names.Count)
            .OrderByDescending(i => combinedScores[i])
            .ToList();

        _output.WriteLine("");
        _output.WriteLine("  RANKING (combined indicator):");
        for (int r = 0; r < ranked.Count; r++)
        {
            int i = ranked[r];
            _output.WriteLine($"    {r + 1}. {names[i],-18}  score = {combinedScores[i]:F4}");
        }

        _output.WriteLine("");
        _output.WriteLine($"  TOPOLOGY MAXIMIZING COMBINED INDICATORS: {bestName}  (score = {bestScore:F4})");

        Assert.True(bestScore > 0, "At least one topology must have positive combined score.");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DE_09 — Correlation analysis: sync-stability vs D_eff vs bridge-band
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_DE_09_CorrelationAnalysis()
    {
        _output.WriteLine("══════════════════════════════════════════════════════════════════");
        _output.WriteLine("  CORRELATION ANALYSIS");
        _output.WriteLine("══════════════════════════════════════════════════════════════════");
        _output.WriteLine("");

        var names = new List<string>();
        var D_eff = new List<double>();
        var Rfinal = new List<double>();
        var basin = new List<double>();
        var S1 = new List<double>();
        var S2 = new List<double>();
        var S3 = new List<double>();

        foreach (var (name, g) in GetTopologies())
        {
            var (l2, lMax, _S1) = SpectralDiagnostics(g);
            var (Rf, b) = RunSyncDiagnostics(g, BaseSeed);
            double _S2 = PropagationIsotropy(g);
            var (dOmega, _S3) = BridgeBandDiagnostics(g, BaseSeed);
            double d = EffectiveDimension(g);

            names.Add(name);
            D_eff.Add(double.IsNaN(d) ? 0 : d);
            Rfinal.Add(Rf);
            basin.Add(b);
            S1.Add(_S1);
            S2.Add(_S2);
            S3.Add(double.IsInfinity(_S3) ? 0 : _S3);
        }

        double[] dArr = D_eff.ToArray();
        double[] rArr = Rfinal.ToArray();
        double[] bArr = basin.ToArray();
        double[] s1Arr = S1.ToArray();
        double[] s2Arr = S2.ToArray();
        double[] s3Arr = S3.ToArray();

        _output.WriteLine("  Pearson correlation coefficients:");
        _output.WriteLine("");
        _output.WriteLine("                 D_eff    R_final   basin     S₁        S₂        S₃");
        _output.WriteLine("    D_eff        1.000");
        _output.WriteLine($"    R_final      {Pearson(dArr, rArr),6:F3}    1.000");
        _output.WriteLine($"    basin        {Pearson(dArr, bArr),6:F3}    {Pearson(rArr, bArr),6:F3}    1.000");
        _output.WriteLine($"    S₁(spectral) {Pearson(dArr, s1Arr),6:F3}    {Pearson(rArr, s1Arr),6:F3}    {Pearson(bArr, s1Arr),6:F3}    1.000");
        _output.WriteLine($"    S₂(iso)      {Pearson(dArr, s2Arr),6:F3}    {Pearson(rArr, s2Arr),6:F3}    {Pearson(bArr, s2Arr),6:F3}    {Pearson(s1Arr, s2Arr),6:F3}    1.000");
        _output.WriteLine($"    S₃(bridge)   {Pearson(dArr, s3Arr),6:F3}    {Pearson(rArr, s3Arr),6:F3}    {Pearson(bArr, s3Arr),6:F3}    {Pearson(s1Arr, s3Arr),6:F3}    {Pearson(s2Arr, s3Arr),6:F3}    1.000");

        _output.WriteLine("");
        _output.WriteLine("  KEY CORRELATIONS:");
        _output.WriteLine($"    sync_stability vs D_eff:           r = {Pearson(rArr, dArr):+.3f}");
        _output.WriteLine($"    sync_stability vs bridge_band:     r = {Pearson(rArr, s3Arr):+.3f}");
        _output.WriteLine($"    D_eff vs bridge_band:              r = {Pearson(dArr, s3Arr):+.3f}");
        _output.WriteLine($"    spectral_balance vs sync:          r = {Pearson(s1Arr, rArr):+.3f}");
        _output.WriteLine($"    isotropy vs sync:                  r = {Pearson(s2Arr, rArr):+.3f}");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DE_10 — Dimension sweep: sync metrics vs D for regular lattices D=1..4
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void V4_1_DE_10_DimensionSweep_RegularLattices(int D)
    {
        int n = D switch { 1 => 20, 2 => 12, 3 => 7, 4 => 3, _ => 5 };
        var g = BuildRegular(D, n);
        Assert.True(IsConnected(g));

        var (l2, lMax, S1) = SpectralDiagnostics(g);
        var (Rf, basin) = RunSyncDiagnostics(g, BaseSeed + D);
        double S2 = PropagationIsotropy(g);
        var (dOmega, S3) = BridgeBandDiagnostics(g, BaseSeed + D);
        double Deff = EffectiveDimension(g);

        // Assert all metrics are finite and in valid ranges.
        Assert.True(l2 > 0, $"λ₂ must be > 0 for D={D}.");
        Assert.True(lMax >= l2, $"λ_max ≥ λ₂ for D={D}.");
        Assert.True(Rf >= 0 && Rf <= 1, $"R_final ∈ [0,1] for D={D}.");
        Assert.True(basin >= 0 && basin <= 1, $"basin ∈ [0,1] for D={D}.");
        Assert.True(S2 >= 0, $"S₂ ≥ 0 for D={D}.");
        Assert.True(dOmega >= 0, $"ΔΩ ≥ 0 for D={D}.");
        Assert.True(double.IsNaN(Deff) || Deff > 0, $"D_eff > 0 or NaN for D={D}.");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DE_11 — Dimension sweep summary table
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_DE_11_DimensionSweep_SummaryTable()
    {
        _output.WriteLine("══════════════════════════════════════════════════════════════════════════════════");
        _output.WriteLine("  DIMENSION SWEEP — REGULAR LATTICES D = 1..4");
        _output.WriteLine("══════════════════════════════════════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine($"  {"D",3} {"n",4} {"N",6} {"λ₂",9} {"λ_max",8} {"S₁",7} {"R_final",9} {"basin",7} {"S₂",7} {"ΔΩ",9} {"S₃",8} {"D_eff",7}");
        _output.WriteLine($"  {new string('-',3)} {new string('-',4)} {new string('-',6)} {new string('-',9)} {new string('-',8)} {new string('-',7)} {new string('-',9)} {new string('-',7)} {new string('-',7)} {new string('-',9)} {new string('-',8)} {new string('-',7)}");

        for (int D = 1; D <= 4; D++)
        {
            int n = D switch { 1 => 20, 2 => 12, 3 => 7, 4 => 3, _ => 5 };
            var g = BuildRegular(D, n);
            var (l2, lMax, S1) = SpectralDiagnostics(g);
            var (Rf, basin) = RunSyncDiagnostics(g, BaseSeed + D);
            double S2 = PropagationIsotropy(g);
            var (dOmega, S3) = BridgeBandDiagnostics(g, BaseSeed + D);
            double Deff = EffectiveDimension(g);

            _output.WriteLine(
                $"  {D,3} {n,4} {g.NodeCount,6} {l2,9:F5} {lMax,8:F4} {S1,7:F4} {Rf,9:F4} {basin,7:F3} {S2,7:F4} {dOmega,9:F6} {S3,8:F2} {Deff,7:F2}");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DE_12 — Deterministic reproducibility
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_DE_12_AllDiagnostics_AreDeterministic()
    {
        foreach (var (name, g) in GetTopologies())
        {
            var r1 = SpectralDiagnostics(g);
            var r2 = SpectralDiagnostics(g);
            Assert.Equal(r1.S1, r2.S1, 12);

            var s1 = RunSyncDiagnostics(g, BaseSeed);
            var s2 = RunSyncDiagnostics(g, BaseSeed);
            Assert.Equal(s1.Rfinal, s2.Rfinal, 12);
            Assert.Equal(s1.basinFraction, s2.basinFraction, 12);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Topology provider
    // ═══════════════════════════════════════════════════════════════════════

    private IEnumerable<(string name, GraphTopology g)> GetTopologies()
    {
        yield return ("1D chain", BuildRegular(1, 20));
        yield return ("2D lattice", BuildRegular(2, 12));
        yield return ("3D cubic", BuildRegular(3, 7));
        yield return ("4D hypercubic", BuildRegular(4, 4));

        var rg = BuildRandomGraph(RandomSmallWorldNodes, 8.0, BaseSeed);
        if (IsConnected(rg))
            yield return ("random", rg);

        var sw = BuildSmallWorld(RandomSmallWorldNodes, 6, 0.1, BaseSeed);
        if (IsConnected(sw))
            yield return ("small-world", sw);
    }
}

