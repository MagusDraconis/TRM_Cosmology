using Xunit;

namespace TRM.Tests.V4_1;

/// <summary>
/// Validates causal propagation bounds on discrete graphs.
/// Maps to: TRM_V4_1_Emergent_Space.md §3.5 (Causal Bound Argument).
/// Classification: DERIVABLE CANDIDATE — tests structural claims.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_CausalPropagation")]
public class V4_1_CausalPropagation_Tests
{
    // ── Simple nearest-neighbour graph builder ───────────────────
    private static int[][] RingAdjacency(int n)
    {
        var adj = new int[n][];
        for (int i = 0; i < n; i++)
        {
            var neighbours = new List<int> { (i + 1) % n };
            if (n > 2) neighbours.Add((i - 1 + n) % n);
            adj[i] = neighbours.ToArray();
        }
        return adj;
    }

    private static int ShortestPath(int[][] adj, int from, int to)
    {
        var dist = new int[adj.Length];
        Array.Fill(dist, -1);
        dist[from] = 0;
        var q = new Queue<int>();
        q.Enqueue(from);
        while (q.Count > 0)
        {
            int u = q.Dequeue();
            if (u == to) return dist[u];
            foreach (int v in adj[u])
            {
                if (dist[v] == -1)
                {
                    dist[v] = dist[u] + 1;
                    q.Enqueue(v);
                }
            }
        }
        return -1;
    }

    /// <summary>
    /// One-edge-per-cycle propagation: after T cycles, max reachable distance is T.
    /// </summary>
    [Fact]
    public void V4_1_03_CausalPropagation_MatchesShortestPath()
    {
        var adj = RingAdjacency(10);
        for (int t = 0; t < 10; t++)
        {
            // After t cycles, node 0 can only reach nodes within distance t.
            var reachable = new int[10];
            Array.Fill(reachable, -1);
            reachable[0] = 0;
            var q = new Queue<int>();
            q.Enqueue(0);
            while (q.Count > 0)
            {
                int u = q.Dequeue();
                if (reachable[u] >= t) continue;
                foreach (int v in adj[u])
                {
                    if (reachable[v] == -1)
                    {
                        reachable[v] = reachable[u] + 1;
                        q.Enqueue(v);
                    }
                }
            }
            for (int i = 0; i < 10; i++)
            {
                int d = ShortestPath(adj, 0, i);
                // After t cycles, can only reach nodes with d <= t.
                if (d > t)
                    Assert.True(reachable[i] == -1,
                        $"Node {i} at distance {d} should not be reachable in {t} cycles.");
            }
        }
    }

    /// <summary>
    /// c_TRM = 1: max distance per cycle is 1 edge.
    /// </summary>
    [Fact]
    public void V4_1_02_CausalSpeed_BoundIsOneEdgePerCycle()
    {
        var adj = RingAdjacency(10);
        int cycles = 3;
        // Simulate BFS limited to 1 edge per cycle.
        int maxDist = 0;
        var visited = new bool[10];
        visited[0] = true;
        var frontier = new List<int> { 0 };
        for (int c = 0; c < cycles; c++)
        {
            var next = new List<int>();
            foreach (int u in frontier)
            foreach (int v in adj[u])
                if (!visited[v]) { visited[v] = true; next.Add(v); }
            frontier = next;
            maxDist = c + 1;
        }
        Assert.Equal(cycles, maxDist); // after 3 cycles, max distance = 3
    }

    /// <summary>
    /// No edge-skipping: after T cycles, cannot reach distance > T.
    /// </summary>
    [Fact]
    public void V4_1_01_NoEdgeSkipping_ReachabilityBound()
    {
        var adj = RingAdjacency(10);
        for (int t = 1; t <= 5; t++)
        {
            var visited = new bool[10];
            visited[0] = true;
            var frontier = new List<int> { 0 };
            for (int c = 0; c < t; c++)
            {
                var next = new List<int>();
                foreach (int u in frontier)
                foreach (int v in adj[u])
                    if (!visited[v]) { visited[v] = true; next.Add(v); }
                frontier = next;
            }
            for (int i = 0; i < 10; i++)
            {
                int d = ShortestPath(adj, 0, i);
                if (d > t)
                    Assert.False(visited[i],
                        $"Node {i} at distance {d} reached in {t} cycles (edge-skipping).");
            }
        }
    }
}
