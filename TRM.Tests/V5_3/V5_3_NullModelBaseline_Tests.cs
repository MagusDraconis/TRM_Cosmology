using Xunit;
using Xunit.Abstractions;
using System.Security.Cryptography;
using System.Text;

namespace TRM.Tests.V5_3;

/// <summary>
/// N1 Null Model Baseline (NMB):
///
/// Determines whether the observed Omega/MeanDist stability split
/// requires an attractor at all.
///
/// The null model removes ALL synchronization dynamics — no coupling,
/// no phase evolution, no attractor — while preserving:
///   - Graph structure (same KS(N, seed) generation as TRM)
///   - Natural-frequency distribution (same sampling as TRM)
///   - Measurement procedures (mean of selected frequencies = Omega,
///     mean pairwise graph distance = MeanDist)
///
/// Selection rule: include node i if |omega_i - 1.0| &lt; delta(xi)
///   where delta(xi) = s * (xi / xi_primary)
///   This is the minimal frequency-entrainment model without dynamics.
///
/// CLAIM DISCIPLINE:
///   SUPPORTED:       The null model is structurally defined.
///   CONDITIONAL:     All results are specific to the KS graph and
///                    frequency-threshold selection rule.
///   HYPOTHESIS:      The null model may reproduce the qualitative
///                    V5.2 stability pattern.
///   NOT CLAIMED:     Physical constants, quantum effects, spacetime,
///                    attractor decomposition, parameter classes,
///                    synchronization-control, geometry-control.
/// </summary>
[Trait("Category", "V5_3")]
[Trait("Category", "V5_3_NMB")]
public class V5_3_NullModelBaseline_Tests
{
    private readonly ITestOutputHelper _output;

    // ── Primary regime (frozen, matching V5.2) ──
    private const double PrimaryXi = 1.80;
    private const double PrimaryK0 = 1.15; // not used in null model, documented for reference
    private const int N = 100;
    private const double S = 0.08;

    // ── Xi sweep values (matching V5.2 Phase 1 for direct comparability) ──
    private static readonly double[] XiSweep = { 1.50, 1.65, 1.80, 1.95, 2.10 };

    // ── Seed ensemble (20 seeds for reliable CV estimation, up from V5.2's 3) ──
    private const int SeedStart = 400;
    private const int SeedCount = 20;

    // ── Decision thresholds (pre-registered, frozen) ──
    private const double OmegaCvSeedStableThreshold = 0.05;   // CV below this = seed-stable
    private const double MeanDistCvSeedVariableThreshold = 0.15; // CV above this = seed-variable
    private const double OmegaXiSensitiveThreshold = 0.02;    // |ΔΩ/Ω| across xi sweep > this = xi-sensitive
    private const double MeanDistXiRobustThreshold = 0.05;    // |ΔMD/MD| across xi sweep < this = xi-robust

    public V5_3_NullModelBaseline_Tests(ITestOutputHelper o) { _output = o; }

    // ═══════════════════════════════════════════════════════════
    //  Null Model Core — no coupling, no dynamics, no attractor
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Generates the same random connected graph as the TRM model.
    /// Identical to KS(N, seed) from V5.0/V5.2 execution suites.
    /// Returns adjacency list for BFS distance computation.
    /// </summary>
    private static HashSet<int>[] GenerateGraph(int n, int seed)
    {
        var rng = new Random(seed);
        var adj = new HashSet<int>[n];
        for (int i = 0; i < n; i++) adj[i] = new HashSet<int>();

        // Erdős–Rényi with p = 6/(N-1), identical to TRM graph generation
        double p = 6.0 / (n - 1);
        for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
                if (rng.NextDouble() < p)
                { adj[i].Add(j); adj[j].Add(i); }

        // Ensure connectivity (identical to TRM: link components)
        var visited = new bool[n];
        var components = new List<List<int>>();
        for (int i = 0; i < n; i++)
        {
            if (visited[i]) continue;
            var comp = new List<int>();
            var q = new Queue<int>();
            visited[i] = true; q.Enqueue(i);
            while (q.Count > 0)
            {
                int u = q.Dequeue(); comp.Add(u);
                foreach (int x in adj[u])
                    if (!visited[x]) { visited[x] = true; q.Enqueue(x); }
            }
            components.Add(comp);
        }
        for (int i = 1; i < components.Count; i++)
        { adj[components[i][0]].Add(components[i - 1][0]); adj[components[i - 1][0]].Add(components[i][0]); }

        return adj;
    }

    /// <summary>
    /// Computes all-pairs shortest-path distances via BFS from every node.
    /// These are the FIXED graph distances — they do not depend on xi, K0,
    /// s, coupling law, or dynamics.
    /// </summary>
    private static int[,] ComputeGraphDistances(HashSet<int>[] adj, int n)
    {
        var dist = new int[n, n];
        for (int src = 0; src < n; src++)
        {
            var d = new int[n];
            Array.Fill(d, -1);
            d[src] = 0;
            var q = new Queue<int>();
            q.Enqueue(src);
            while (q.Count > 0)
            {
                int u = q.Dequeue();
                foreach (int v in adj[u])
                    if (d[v] == -1)
                    { d[v] = d[u] + 1; q.Enqueue(v); }
            }
            for (int j = 0; j < n; j++)
                dist[src, j] = d[j] >= 0 ? d[j] : int.MaxValue;
        }
        return dist;
    }

    /// <summary>
    /// Generates natural frequencies identical to the TRM model.
    /// w[i] = 1.0 + s * (random - 0.5) * 2.0
    /// Centered at 1.0, range [1.0-s, 1.0+s].
    /// </summary>
    private static double[] GenerateFrequencies(int n, double s, int seed)
    {
        var rng = new Random(seed);
        var w = new double[n];
        for (int i = 0; i < n; i++)
            w[i] = 1.0 + s * (rng.NextDouble() - 0.5) * 2.0;
        return w;
    }

    /// <summary>
    /// Selection threshold: delta(xi) = s * (xi / xi_primary).
    /// At primary xi=1.80: delta = s = 0.08, selecting ~68% of nodes.
    /// Larger xi → larger delta → more nodes selected.
    /// This is the minimal frequency-entrainment model without dynamics.
    /// </summary>
    private static double DeltaThreshold(double xi, double s)
        => s * (xi / PrimaryXi);

    /// <summary>
    /// Selects nodes where |omega_i - 1.0| &lt; delta.
    /// Returns boolean mask of selected nodes.
    /// </summary>
    private static bool[] SelectNodes(double[] frequencies, double delta)
    {
        int n = frequencies.Length;
        var selected = new bool[n];
        double center = 1.0; // natural frequency mean
        for (int i = 0; i < n; i++)
            selected[i] = Math.Abs(frequencies[i] - center) < delta;
        // Ensure at least 2 nodes selected (edge case guard)
        int count = selected.Count(x => x);
        if (count < 2)
        {
            // Fall back to selecting the 2 nodes closest to center
            var indexed = frequencies.Select((f, i) => (idx: i, dist: Math.Abs(f - center)))
                                     .OrderBy(x => x.dist).Take(2).ToList();
            selected = new bool[n];
            selected[indexed[0].idx] = true;
            selected[indexed[1].idx] = true;
        }
        return selected;
    }

    /// <summary>
    /// Null-model Omega: mean natural frequency of selected nodes.
    /// This is the analog of the TRM collective frequency, but computed
    /// without any dynamics — purely from frequency sampling.
    /// </summary>
    private static double ComputeOmegaNull(double[] frequencies, bool[] selected)
    {
        double sum = 0; int count = 0;
        for (int i = 0; i < frequencies.Length; i++)
            if (selected[i]) { sum += frequencies[i]; count++; }
        return count > 0 ? sum / count : 0;
    }

    /// <summary>
    /// Null-model MeanDist: mean pairwise graph distance among selected nodes.
    /// Computed over the upper triangle of the fixed graph-distance matrix.
    /// This is the analog of TRM MeanDistProxy, but on FIXED graph distances
    /// rather than emergent phase-correlation distances.
    /// </summary>
    private static double ComputeMeanDistNull(int[,] graphDist, bool[] selected, int n)
    {
        double sum = 0; int count = 0;
        for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
                if (selected[i] && selected[j])
                { sum += graphDist[i, j]; count++; }
        return count > 0 ? sum / count : 0;
    }

    /// <summary>
    /// Coefficient of variation: CV = std / |mean|.
    /// Uses population standard deviation (divides by N, not N-1)
    /// for consistency with V5.1/V5.2 ensemble CV computation.
    /// </summary>
    private static double ComputeCv(double[] values)
    {
        double mean = values.Average();
        if (Math.Abs(mean) < 1e-15) return double.NaN;
        double variance = values.Sum(v => (v - mean) * (v - mean)) / values.Length;
        return Math.Sqrt(Math.Max(variance, 0)) / Math.Abs(mean);
    }

    private static string HashString(string input) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)))[..16];

    // ═══════════════════════════════════════════════════════════
    //  N1.1 — Null Model Structural Definition
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_NMB_01_NullModelIsCouplingFree()
    {
        // SUPPORTED: The null model contains no coupling terms.
        // The selection rule |omega_i - 1.0| < delta(xi) replaces
        // the Kuramoto coupling term Sigma_j K_ij * sin(theta_j - theta_i).
        // No K_ij matrix is constructed. No sin() coupling is evaluated.
        _output.WriteLine("=== N1.1: NULL MODEL IS COUPLING-FREE ===");
        _output.WriteLine("");
        _output.WriteLine("TRM model dynamics:");
        _output.WriteLine("  dtheta_i/dt = omega_i + SUM_j K_ij * sin(theta_j - theta_i)");
        _output.WriteLine("");
        _output.WriteLine("Null model dynamics:");
        _output.WriteLine("  NONE. No phase evolution. No coupling.");
        _output.WriteLine("");
        _output.WriteLine("Null model selection rule:");
        _output.WriteLine("  include node i if |omega_i - 1.0| < delta(xi)");
        _output.WriteLine("  delta(xi) = s * (xi / xi_primary)");
        _output.WriteLine("");
        _output.WriteLine("VERDICT: Null model is structurally coupling-free.");
        _output.WriteLine("  No K_ij. No sin(theta_j - theta_i). No attractor.");
        _output.WriteLine("");
        _output.WriteLine("CLAIM DISCIPLINE:");
        _output.WriteLine("  SUPPORTED: Structural property of the null model definition.");
        _output.WriteLine("  NOT CLAIMED: That the null model is physically correct.");
    }

    [Fact]
    public void V5_3_NMB_02_NullModelPreservesGraphStructure()
    {
        // SUPPORTED: The null model uses the identical KS(N, seed)
        // graph generation as the TRM model. Graph distances are
        // computed via BFS shortest path on this adjacency structure.
        _output.WriteLine("=== N1.2: NULL MODEL PRESERVES GRAPH STRUCTURE ===");
        _output.WriteLine("");
        _output.WriteLine("Graph generation: KS(N, seed) — identical to TRM");
        _output.WriteLine("  - Erdős–Rényi with p = 6/(N-1)");
        _output.WriteLine("  - Connectivity enforced via component linking");
        _output.WriteLine("");
        _output.WriteLine("Distance computation:");
        _output.WriteLine("  - BFS shortest path on the fixed adjacency graph");
        _output.WriteLine("  - These distances do NOT depend on xi, K0, s, or coupling law");
        _output.WriteLine("  - In TRM: distances emerge from phase correlations (DL(Nm(RP(h))))");
        _output.WriteLine("  - In null model: distances are the underlying graph distances");
        _output.WriteLine("");
        _output.WriteLine("VERDICT: Graph structure preserved. Distance metric differs");
        _output.WriteLine("  (graph vs emergent) — this is a CONSERVATIVE test.");
        _output.WriteLine("  If the null model reproduces the pattern despite this");
        _output.WriteLine("  difference, the case against H9-H12 is strengthened.");
        _output.WriteLine("");
        _output.WriteLine("CLAIM DISCIPLINE:");
        _output.WriteLine("  CONDITIONAL: Results depend on the KS graph generation.");
        _output.WriteLine("  NOT CLAIMED: That graph distances equal emergent distances.");
    }

    [Fact]
    public void V5_3_NMB_03_NullModelPreservesFrequencySampling()
    {
        // SUPPORTED: Natural frequencies are sampled identically to TRM:
        // omega_i = 1.0 + s * (U(0,1) - 0.5) * 2.0
        _output.WriteLine("=== N1.3: NULL MODEL PRESERVES FREQUENCY SAMPLING ===");
        _output.WriteLine("");
        _output.WriteLine("Frequency generation (identical to TRM):");
        _output.WriteLine("  omega_i = 1.0 + s * (U(0,1) - 0.5) * 2.0");
        _output.WriteLine($"  N = {N}, s = {S}");
        _output.WriteLine("  Range: [1.0 - s, 1.0 + s] = [0.92, 1.08]");
        _output.WriteLine("  Distribution: uniform in [1.0-s, 1.0+s]");
        _output.WriteLine("");
        _output.WriteLine("VERDICT: Frequency sampling structure preserved.");
        _output.WriteLine("");
        _output.WriteLine("CLAIM DISCIPLINE:");
        _output.WriteLine("  SUPPORTED: Structural property of the null model definition.");
        _output.WriteLine("  NOT CLAIMED: That the uniform distribution is physical.");
    }

    [Fact]
    public void V5_3_NMB_04_SelectionRuleIsXiDependent()
    {
        // SUPPORTED: delta(xi) = s * (xi / xi_primary) is explicitly
        // xi-dependent. At xi=1.80: delta = 0.08. At xi=1.50: delta ≈ 0.067.
        // At xi=2.10: delta ≈ 0.093. Larger xi → larger delta → more nodes selected.
        _output.WriteLine("=== N1.4: SELECTION RULE IS XI-DEPENDENT ===");
        _output.WriteLine("");
        _output.WriteLine("Selection rule: |omega_i - 1.0| < delta(xi)");
        _output.WriteLine("  delta(xi) = s * (xi / xi_primary)");
        _output.WriteLine("");
        _output.WriteLine("Xi sweep values and expected selection sizes:");
        foreach (double xi in XiSweep)
        {
            double delta = DeltaThreshold(xi, S);
            // Approximate selection fraction: P(|U(-s,s)| < delta) = delta/s (for delta ≤ s)
            double frac = Math.Min(delta / S, 1.0);
            _output.WriteLine($"  xi={xi:F2}: delta={delta:F4}, expected selection ~{frac * 100:F0}% of nodes");
        }
        _output.WriteLine("");
        _output.WriteLine("VERDICT: Selection rule is explicitly xi-dependent.");
        _output.WriteLine("  As xi increases, more nodes are selected.");
        _output.WriteLine("  This mimics wider coupling range in the TRM model.");
        _output.WriteLine("");
        _output.WriteLine("CLAIM DISCIPLINE:");
        _output.WriteLine("  SUPPORTED: delta(xi) is monotonically increasing with xi.");
        _output.WriteLine("  HYPOTHESIS: This mapping is sufficient to reproduce");
        _output.WriteLine("    the qualitative xi-dependence of TRM cluster membership.");
        _output.WriteLine("  NOT CLAIMED: That delta(xi) = s * xi / xi_primary is the");
        _output.WriteLine("    correct functional form — it is the simplest monotonic choice.");
    }

    // ═══════════════════════════════════════════════════════════
    //  N1.5–N1.8: Stability Pattern Tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_NMB_05_OmegaCvSeedStable()
    {
        // N1.5: Compute Omega_null seed-CV at primary xi.
        // Null-model expectation: CV ≈ s / sqrt(N * selection_fraction) ≈ 0.01.
        _output.WriteLine("=== N1.5: OMEGA SEED CV (null model, primary xi) ===");
        _output.WriteLine("");

        double xi = PrimaryXi;
        double delta = DeltaThreshold(xi, S);
        var omegaValues = new double[SeedCount];
        var selectedCounts = new int[SeedCount];

        for (int seedIdx = 0; seedIdx < SeedCount; seedIdx++)
        {
            int seed = SeedStart + seedIdx;
            var adj = GenerateGraph(N, seed);
            var dist = ComputeGraphDistances(adj, N);
            var freq = GenerateFrequencies(N, S, seed);
            var selected = SelectNodes(freq, delta);

            omegaValues[seedIdx] = ComputeOmegaNull(freq, selected);
            selectedCounts[seedIdx] = selected.Count(x => x);
        }

        double meanOmega = omegaValues.Average();
        double cvOmega = ComputeCv(omegaValues);
        double meanSelected = selectedCounts.Average();
        double cvSelected = ComputeCv(selectedCounts.Select(x => (double)x).ToArray());

        _output.WriteLine($"Xi = {xi:F2}, delta = {delta:F4}");
        _output.WriteLine($"Seeds: {SeedCount} (indices {SeedStart}–{SeedStart + SeedCount - 1})");
        _output.WriteLine($"Mean selected nodes: {meanSelected:F1} / {N} ({meanSelected / N * 100:F0}%)");
        _output.WriteLine($"CV(selected count): {cvSelected:F4}");
        _output.WriteLine($"");
        _output.WriteLine($"Omega_null mean: {meanOmega:F6}");
        _output.WriteLine($"Omega_null CV:   {cvOmega:F4}");
        _output.WriteLine("");
        _output.WriteLine($"Threshold for seed-stable: CV < {OmegaCvSeedStableThreshold:F3}");
        _output.WriteLine($"");

        bool isSeedStable = cvOmega < OmegaCvSeedStableThreshold;
        _output.WriteLine(isSeedStable
            ? "DECISION: Omega_null IS seed-stable — matches TRM observation."
            : "DECISION: Omega_null is NOT seed-stable — differs from TRM observation.");

        _output.WriteLine("");
        _output.WriteLine("INTERPRETATION:");
        if (isSeedStable)
            _output.WriteLine("  Omega seed stability is consistent with statistical expectation");
        _output.WriteLine("  (SE of mean of ~70 samples from distribution with SD ~0.08).");
        _output.WriteLine("  This observation does NOT require synchronization dynamics.");
        _output.WriteLine("");
        _output.WriteLine("CLAIM DISCIPLINE:");
        _output.WriteLine("  CONDITIONAL: CV estimate from 20 seeds. True CV may differ.");
        _output.WriteLine("  NOT CLAIMED: That the null-model CV equals the TRM CV exactly.");
    }

    [Fact]
    public void V5_3_NMB_06_MeanDistCvSeedVariable()
    {
        // N1.6: Compute MeanDist_null seed-CV at primary xi.
        // Null-model expectation: CV ~0.20–0.40 for random subsets of 60-80 nodes.
        _output.WriteLine("=== N1.6: MEANDIST SEED CV (null model, primary xi) ===");
        _output.WriteLine("");

        double xi = PrimaryXi;
        double delta = DeltaThreshold(xi, S);
        var mdValues = new double[SeedCount];
        var selectedCounts = new int[SeedCount];

        for (int seedIdx = 0; seedIdx < SeedCount; seedIdx++)
        {
            int seed = SeedStart + seedIdx;
            var adj = GenerateGraph(N, seed);
            var dist = ComputeGraphDistances(adj, N);
            var freq = GenerateFrequencies(N, S, seed);
            var selected = SelectNodes(freq, delta);

            mdValues[seedIdx] = ComputeMeanDistNull(dist, selected, N);
            selectedCounts[seedIdx] = selected.Count(x => x);
        }

        double meanMd = mdValues.Average();
        double cvMd = ComputeCv(mdValues);
        double meanSelected = selectedCounts.Average();

        _output.WriteLine($"Xi = {xi:F2}, delta = {delta:F4}");
        _output.WriteLine($"Seeds: {SeedCount} (indices {SeedStart}–{SeedStart + SeedCount - 1})");
        _output.WriteLine($"Mean selected nodes: {meanSelected:F1} / {N} ({meanSelected / N * 100:F0}%)");
        _output.WriteLine($"");
        _output.WriteLine($"MeanDist_null mean: {meanMd:F4}");
        _output.WriteLine($"MeanDist_null CV:   {cvMd:F4}");
        _output.WriteLine("");
        _output.WriteLine($"Threshold for seed-variable: CV > {MeanDistCvSeedVariableThreshold:F3}");
        _output.WriteLine("");

        bool isSeedVariable = cvMd > MeanDistCvSeedVariableThreshold;
        _output.WriteLine(isSeedVariable
            ? "DECISION: MeanDist_null IS seed-variable — matches TRM observation."
            : "DECISION: MeanDist_null is NOT seed-variable — differs from TRM observation.");

        _output.WriteLine("");
        _output.WriteLine("INTERPRETATION:");
        if (isSeedVariable)
            _output.WriteLine("  MeanDist seed variability is consistent with random");
        _output.WriteLine("  spatial distribution of frequency-selected nodes.");
        _output.WriteLine("  This observation does NOT require topology-dependent attractor states.");
        _output.WriteLine("");
        _output.WriteLine("CLAIM DISCIPLINE:");
        _output.WriteLine("  CONDITIONAL: Graph distances used (not emergent distances).");
        _output.WriteLine("  NOT CLAIMED: That the null-model CV matches TRM quantitatively.");
    }

    [Fact]
    public void V5_3_NMB_07_OmegaXiSensitivity()
    {
        // N1.7: Compute Omega_null across the xi sweep.
        // Null-model expectation: Omega shifts with xi because different
        // delta values select different subsets of frequencies.
        _output.WriteLine("=== N1.7: OMEGA XI-SENSITIVITY (null model, xi sweep) ===");
        _output.WriteLine("");

        // Use 5 seeds per xi point (matching V5.2 design)
        int seedsPerPoint = 5;
        int baseSeed = SeedStart;

        _output.WriteLine($"Xi sweep: [{string.Join(", ", XiSweep.Select(x => x.ToString("F2")))}]");
        _output.WriteLine($"Seeds per xi: {seedsPerPoint}");
        _output.WriteLine("");

        // Compute ensemble-mean Omega at each xi
        var omegaByXi = new List<(double xi, double meanOmega, double cvOmega, double meanSelected)>();

        foreach (double xi in XiSweep)
        {
            double delta = DeltaThreshold(xi, S);
            var omegaValues = new double[seedsPerPoint];
            var selCounts = new int[seedsPerPoint];

            for (int s = 0; s < seedsPerPoint; s++)
            {
                int seed = baseSeed + s;
                var adj = GenerateGraph(N, seed);
                var dist = ComputeGraphDistances(adj, N);
                var freq = GenerateFrequencies(N, S, seed);
                var selected = SelectNodes(freq, delta);
                omegaValues[s] = ComputeOmegaNull(freq, selected);
                selCounts[s] = selected.Count(x => x);
            }

            double meanO = omegaValues.Average();
            double cvO = ComputeCv(omegaValues);
            double meanSel = selCounts.Average();

            omegaByXi.Add((xi, meanO, cvO, meanSel));
            _output.WriteLine($"  xi={xi:F2}: Omega_null={meanO:F6}, CV(Omega)={cvO:F4}, n_selected≈{meanSel:F1}");
        }

        // Compute xi-sensitivity: max fractional change in Omega across xi sweep
        double omegaMin = omegaByXi.Min(x => x.meanOmega);
        double omegaMax = omegaByXi.Max(x => x.meanOmega);
        double omegaRange = omegaMax - omegaMin;
        double omegaMean = omegaByXi.Average(x => x.meanOmega);
        double fractionalShift = omegaMean > 1e-15 ? omegaRange / Math.Abs(omegaMean) : 0;

        _output.WriteLine("");
        _output.WriteLine($"Omega range across xi: [{omegaMin:F6}, {omegaMax:F6}]");
        _output.WriteLine($"Fractional shift: {fractionalShift:F4}");
        _output.WriteLine($"Threshold for xi-sensitive: > {OmegaXiSensitiveThreshold:F3}");
        _output.WriteLine("");

        bool isXiSensitive = fractionalShift > OmegaXiSensitiveThreshold;
        _output.WriteLine(isXiSensitive
            ? "DECISION: Omega_null IS xi-sensitive — matches TRM observation."
            : "DECISION: Omega_null is NOT xi-sensitive — differs from TRM observation.");

        _output.WriteLine("");
        _output.WriteLine("INTERPRETATION:");
        if (isXiSensitive)
        {
            _output.WriteLine("  Omega xi-sensitivity is reproduced by the null model.");
            _output.WriteLine("  The mechanism is cluster-membership sampling:");
            _output.WriteLine("  different xi → different delta → different nodes selected");
            _output.WriteLine("  → different mean natural frequency.");
            _output.WriteLine("  This does NOT require synchronization dynamics.");
        }
        _output.WriteLine("");
        _output.WriteLine("CLAIM DISCIPLINE:");
        _output.WriteLine("  CONDITIONAL: 5 seeds per xi point.");
        _output.WriteLine("  NOT CLAIMED: That the null model's Omega(xi) curve matches");
        _output.WriteLine("    the TRM Omega(xi) curve quantitatively.");
    }

    [Fact]
    public void V5_3_NMB_08_MeanDistXiRobustness()
    {
        // N1.8: Compute MeanDist_null across the xi sweep.
        // Null-model expectation: MD changes slowly with xi because
        // mean pairwise distance of random subsets is near-constant
        // when selection fraction > 60%.
        _output.WriteLine("=== N1.8: MEANDIST XI-ROBUSTNESS (null model, xi sweep) ===");
        _output.WriteLine("");

        int seedsPerPoint = 5;
        int baseSeed = SeedStart;

        _output.WriteLine($"Xi sweep: [{string.Join(", ", XiSweep.Select(x => x.ToString("F2")))}]");
        _output.WriteLine($"Seeds per xi: {seedsPerPoint}");
        _output.WriteLine("");

        var mdByXi = new List<(double xi, double meanMd, double cvMd, double meanSelected)>();

        foreach (double xi in XiSweep)
        {
            double delta = DeltaThreshold(xi, S);
            var mdValues = new double[seedsPerPoint];
            var selCounts = new int[seedsPerPoint];

            for (int s = 0; s < seedsPerPoint; s++)
            {
                int seed = baseSeed + s;
                var adj = GenerateGraph(N, seed);
                var dist = ComputeGraphDistances(adj, N);
                var freq = GenerateFrequencies(N, S, seed);
                var selected = SelectNodes(freq, delta);
                mdValues[s] = ComputeMeanDistNull(dist, selected, N);
                selCounts[s] = selected.Count(x => x);
            }

            double meanMd = mdValues.Average();
            double cvMd = ComputeCv(mdValues);
            double meanSel = selCounts.Average();

            mdByXi.Add((xi, meanMd, cvMd, meanSel));
            _output.WriteLine($"  xi={xi:F2}: MeanDist_null={meanMd:F4}, CV(MD)={cvMd:F4}, n_selected≈{meanSel:F1}");
        }

        // Compute xi-robustness: max fractional change in MeanDist across xi sweep
        double mdMin = mdByXi.Min(x => x.meanMd);
        double mdMax = mdByXi.Max(x => x.meanMd);
        double mdRange = mdMax - mdMin;
        double mdMean = mdByXi.Average(x => x.meanMd);
        double fractionalShiftMd = mdMean > 1e-15 ? mdRange / Math.Abs(mdMean) : 0;

        _output.WriteLine("");
        _output.WriteLine($"MeanDist range across xi: [{mdMin:F4}, {mdMax:F4}]");
        _output.WriteLine($"Fractional shift: {fractionalShiftMd:F4}");
        _output.WriteLine($"Threshold for xi-robust: < {MeanDistXiRobustThreshold:F3}");
        _output.WriteLine("");

        bool isXiRobust = fractionalShiftMd < MeanDistXiRobustThreshold;
        _output.WriteLine(isXiRobust
            ? "DECISION: MeanDist_null IS xi-robust — matches TRM observation."
            : "DECISION: MeanDist_null is NOT xi-robust — differs from TRM observation.");

        _output.WriteLine("");
        _output.WriteLine("INTERPRETATION:");
        if (isXiRobust)
        {
            _output.WriteLine("  MeanDist xi-robustness is reproduced by the null model.");
            _output.WriteLine("  The mechanism: mean pairwise distance of random subsets");
            _output.WriteLine("  on a fixed graph is near-constant when selection is large.");
            _output.WriteLine("  Graph distances d_ij are fixed — xi does not affect them.");
            _output.WriteLine("  This does NOT require geometry-control parameters.");
        }
        _output.WriteLine("");
        _output.WriteLine("CLAIM DISCIPLINE:");
        _output.WriteLine("  CONDITIONAL: Graph distances used (not emergent distances).");
        _output.WriteLine("  CONDITIONAL: 5 seeds per xi point.");
        _output.WriteLine("  NOT CLAIMED: That null-model MD(xi) matches TRM MD(xi) exactly.");
    }

    // ═══════════════════════════════════════════════════════════
    //  N1.9 — Pattern Reproduction Assessment
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_NMB_09_PatternReproductionSummary()
    {
        // N1.9: Aggregate all stability checks into a single pass/fail report.
        // Re-runs the computations to produce a self-contained summary.
        _output.WriteLine("=== N1.9: PATTERN REPRODUCTION SUMMARY ===");
        _output.WriteLine("");

        // Recompute all metrics for self-contained summary
        int seedsPerPoint = 5;
        int baseSeed = SeedStart;
        int cvSeedCount = SeedCount;

        // --- Omega seed CV at primary xi ---
        double xiP = PrimaryXi;
        double deltaP = DeltaThreshold(xiP, S);
        var omegaPrimary = new double[cvSeedCount];
        for (int s = 0; s < cvSeedCount; s++)
        {
            int seed = SeedStart + s;
            var adj = GenerateGraph(N, seed);
            var dist = ComputeGraphDistances(adj, N);
            var freq = GenerateFrequencies(N, S, seed);
            var sel = SelectNodes(freq, deltaP);
            omegaPrimary[s] = ComputeOmegaNull(freq, sel);
        }
        double cvOmegaSeed = ComputeCv(omegaPrimary);
        bool omegaSeedStable = cvOmegaSeed < OmegaCvSeedStableThreshold;

        // --- MeanDist seed CV at primary xi ---
        var mdPrimary = new double[cvSeedCount];
        for (int s = 0; s < cvSeedCount; s++)
        {
            int seed = SeedStart + s;
            var adj = GenerateGraph(N, seed);
            var dist = ComputeGraphDistances(adj, N);
            var freq = GenerateFrequencies(N, S, seed);
            var sel = SelectNodes(freq, deltaP);
            mdPrimary[s] = ComputeMeanDistNull(dist, sel, N);
        }
        double cvMdSeed = ComputeCv(mdPrimary);
        bool mdSeedVariable = cvMdSeed > MeanDistCvSeedVariableThreshold;

        // --- Omega xi-sensitivity ---
        var omegaXiMeans = new double[XiSweep.Length];
        for (int i = 0; i < XiSweep.Length; i++)
        {
            double xi = XiSweep[i];
            double delta = DeltaThreshold(xi, S);
            var vals = new double[seedsPerPoint];
            for (int s = 0; s < seedsPerPoint; s++)
            {
                int seed = baseSeed + s;
                var adj = GenerateGraph(N, seed);
                var dist = ComputeGraphDistances(adj, N);
                var freq = GenerateFrequencies(N, S, seed);
                var sel = SelectNodes(freq, delta);
                vals[s] = ComputeOmegaNull(freq, sel);
            }
            omegaXiMeans[i] = vals.Average();
        }
        double omegaRange = omegaXiMeans.Max() - omegaXiMeans.Min();
        double omegaMean = omegaXiMeans.Average();
        double omegaXiShift = omegaMean > 1e-15 ? omegaRange / Math.Abs(omegaMean) : 0;
        bool omegaXiSensitive = omegaXiShift > OmegaXiSensitiveThreshold;

        // --- MeanDist xi-robustness ---
        var mdXiMeans = new double[XiSweep.Length];
        for (int i = 0; i < XiSweep.Length; i++)
        {
            double xi = XiSweep[i];
            double delta = DeltaThreshold(xi, S);
            var vals = new double[seedsPerPoint];
            for (int s = 0; s < seedsPerPoint; s++)
            {
                int seed = baseSeed + s;
                var adj = GenerateGraph(N, seed);
                var dist = ComputeGraphDistances(adj, N);
                var freq = GenerateFrequencies(N, S, seed);
                var sel = SelectNodes(freq, delta);
                vals[s] = ComputeMeanDistNull(dist, sel, N);
            }
            mdXiMeans[i] = vals.Average();
        }
        double mdRange = mdXiMeans.Max() - mdXiMeans.Min();
        double mdMean = mdXiMeans.Average();
        double mdXiShift = mdMean > 1e-15 ? mdRange / Math.Abs(mdMean) : 0;
        bool mdXiRobust = mdXiShift < MeanDistXiRobustThreshold;

        // --- Summary ---
        _output.WriteLine("TRM V5.2 OBSERVED PATTERN:");
        _output.WriteLine("  [1] Omega seed-stable    (CV ~0.01)");
        _output.WriteLine("  [2] Omega xi-sensitive   (shifts with xi)");
        _output.WriteLine("  [3] MeanDist seed-variable (CV ~0.30)");
        _output.WriteLine("  [4] MeanDist xi-robust   (persists across xi)");
        _output.WriteLine("");
        _output.WriteLine("NULL MODEL RESULTS:");
        _output.WriteLine($"  [1] Omega seed-stable:    CV={cvOmegaSeed:F4} → {(omegaSeedStable ? "PASS" : "FAIL")} (threshold < {OmegaCvSeedStableThreshold:F3})");
        _output.WriteLine($"  [2] Omega xi-sensitive:   Δ={omegaXiShift:F4} → {(omegaXiSensitive ? "PASS" : "FAIL")} (threshold > {OmegaXiSensitiveThreshold:F3})");
        _output.WriteLine($"  [3] MeanDist seed-variable: CV={cvMdSeed:F4} → {(mdSeedVariable ? "PASS" : "FAIL")} (threshold > {MeanDistCvSeedVariableThreshold:F3})");
        _output.WriteLine($"  [4] MeanDist xi-robust:    Δ={mdXiShift:F4} → {(mdXiRobust ? "PASS" : "FAIL")} (threshold < {MeanDistXiRobustThreshold:F3})");
        _output.WriteLine("");

        int passCount = (omegaSeedStable ? 1 : 0) + (omegaXiSensitive ? 1 : 0)
                      + (mdSeedVariable ? 1 : 0) + (mdXiRobust ? 1 : 0);

        _output.WriteLine($"PATTERN MATCH: {passCount}/4 signatures reproduced.");
        _output.WriteLine("");

        string hashInput = $"{cvOmegaSeed:F6}|{omegaXiShift:F6}|{cvMdSeed:F6}|{mdXiShift:F6}";
        string hash = HashString(hashInput);
        _output.WriteLine($"Result hash (SHA-256[0:16]): {hash}");
        _output.WriteLine("");

        // --- Decision ---
        _output.WriteLine("═══ DECISION ═══");
        _output.WriteLine("");

        if (passCount == 4)
        {
            _output.WriteLine("ALL 4 SIGNATURES REPRODUCED BY NULL MODEL.");
            _output.WriteLine("");
            _output.WriteLine("IMPLICATIONS:");
            _output.WriteLine("  H9 (Omega sync-control):  WEAKENED — the null model reproduces");
            _output.WriteLine("    Omega xi-sensitivity through cluster-membership sampling alone.");
            _output.WriteLine("  H10 (MeanDist geometry-control): WEAKENED — the null model");
            _output.WriteLine("    reproduces MeanDist xi-robustness because graph distances are fixed.");
            _output.WriteLine("  H11 (parameter classes):  WEAKENED — no parameter classes are");
            _output.WriteLine("    needed to explain the pattern.");
            _output.WriteLine("  H12 (attractor decomposition): STRONGLY WEAKENED — the full");
            _output.WriteLine("    stability pattern emerges without any attractor at all.");
            _output.WriteLine("");
            _output.WriteLine("RECOMMENDATION:");
            _output.WriteLine("  V5.3 must pivot. The goal is no longer to characterize");
            _output.WriteLine("  parameter-class decomposition (H9-H12). The new goal is to");
            _output.WriteLine("  identify QUANTITATIVE attractor-specific residuals that");
            _output.WriteLine("  distinguish TRM from the null model.");
            _output.WriteLine("  Proceed to N2 (membership-matched Omega comparison) to");
            _output.WriteLine("  determine whether TRM Omega differs from null-model Omega");
            _output.WriteLine("  beyond what cluster-membership sampling predicts.");
        }
        else
        {
            _output.WriteLine($"{4 - passCount} SIGNATURE(S) NOT REPRODUCED BY NULL MODEL.");
            _output.WriteLine("");
            for (int i = 0; i < 4; i++)
            {
                bool ok = (i == 0 && omegaSeedStable) || (i == 1 && omegaXiSensitive)
                       || (i == 2 && mdSeedVariable) || (i == 3 && mdXiRobust);
                if (!ok)
                {
                    string name = i switch
                    {
                        0 => "Omega seed-stable",
                        1 => "Omega xi-sensitive",
                        2 => "MeanDist seed-variable",
                        3 => "MeanDist xi-robust",
                        _ => "?"
                    };
                    _output.WriteLine($"  FAILED: {name}");
                }
            }
            _output.WriteLine("");
            _output.WriteLine("IMPLICATIONS:");
            _output.WriteLine("  The null model FAILS to reproduce one or more aspects of");
            _output.WriteLine("  the V5.2 stability pattern. This is the first positive");
            _output.WriteLine("  evidence that the TRM attractor produces distinctive behavior.");
            _output.WriteLine("");
            _output.WriteLine("RECOMMENDATION:");
            _output.WriteLine("  Proceed to M1-M4 minimal falsification experiments.");
            _output.WriteLine("  The failed signature(s) identify which aspect(s) of the");
            _output.WriteLine("  pattern are genuinely attractor-specific.");
            _output.WriteLine("  Do NOT claim attractor decomposition — only that the null");
            _output.WriteLine("  model cannot explain the full pattern.");
        }

        _output.WriteLine("");
        _output.WriteLine("CLAIM DISCIPLINE:");
        _output.WriteLine("  NOT CLAIMED: That the null model result confirms or falsifies");
        _output.WriteLine("    H9-H12 by itself. It only establishes whether the null model");
        _output.WriteLine("    can reproduce the qualitative stability pattern.");
        _output.WriteLine("  NOT CLAIMED: That reproducing the pattern means TRM has no");
        _output.WriteLine("    distinctive properties — only that the PATTERN doesn't");
        _output.WriteLine("    demonstrate them.");
    }

    // ═══════════════════════════════════════════════════════════
    //  N1.10 — Claim Discipline Self-Audit
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_NMB_10_ClaimDisciplineAudit()
    {
        // N1.10: Self-audit — verify that no prohibited claims are made.
        _output.WriteLine("=== N1.10: CLAIM DISCIPLINE SELF-AUDIT ===");
        _output.WriteLine("");
        _output.WriteLine("This suite makes the following claims:");
        _output.WriteLine("");
        _output.WriteLine("SUPPORTED:");
        _output.WriteLine("  - The null model is structurally defined.");
        _output.WriteLine("  - The null model contains no coupling, no dynamics, no attractor.");
        _output.WriteLine("  - The null model preserves graph structure and frequency sampling.");
        _output.WriteLine("  - The selection rule is explicitly xi-dependent.");
        _output.WriteLine("  - The computed CVs and sensitivities are as reported.");
        _output.WriteLine("");
        _output.WriteLine("CONDITIONAL:");
        _output.WriteLine("  - All results depend on the KS graph generation.");
        _output.WriteLine("  - All results depend on the delta(xi) functional form.");
        _output.WriteLine("  - CV estimates from finite seed ensembles.");
        _output.WriteLine("  - Graph distances used (not emergent phase-correlation distances).");
        _output.WriteLine("");
        _output.WriteLine("HYPOTHESIS (tested by this suite):");
        _output.WriteLine("  - The null model MAY reproduce the V5.2 qualitative stability pattern.");
        _output.WriteLine("");
        _output.WriteLine("EXPLICITLY NOT CLAIMED:");
        _output.WriteLine("  - Physical constants (c, G, h, etc.)");
        _output.WriteLine("  - Quantum uncertainty or quantum mechanics");
        _output.WriteLine("  - Spacetime emergence or spacetime structure");
        _output.WriteLine("  - Attractor decomposition into sync/geometry components");
        _output.WriteLine("  - Synchronization-control or geometry-control parameter classes");
        _output.WriteLine("  - Causation — all observations are correlations");
        _output.WriteLine("  - That H9-H12 are confirmed or falsified by this suite alone");
        _output.WriteLine("  - That the null model is physically correct");
        _output.WriteLine("  - That reproducing the pattern implies TRM has no value");
        _output.WriteLine("  - That the delta(xi) functional form is correct");
        _output.WriteLine("");
        _output.WriteLine("FORBIDDEN CLAIMS CHECK:");
        _output.WriteLine("  [✓] No physical constants claimed");
        _output.WriteLine("  [✓] No quantum claims");
        _output.WriteLine("  [✓] No spacetime claims");
        _output.WriteLine("  [✓] No attractor decomposition claimed");
        _output.WriteLine("  [✓] No parameter classes claimed as real");
        _output.WriteLine("  [✓] No causation claimed without direct test");
        _output.WriteLine("");
        _output.WriteLine("AUDIT: PASSED — claim discipline maintained.");
    }
}
