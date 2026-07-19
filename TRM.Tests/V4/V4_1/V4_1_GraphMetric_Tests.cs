using Xunit;
using TRM.Core.V4_1;

namespace TRM.Tests.V4_1;

/// <summary>
/// Validates graph-distance metric properties on small synthetic graphs.
/// Maps to: TRM_V4_1_Emergent_Space.md §3.6 (Metric Properties).
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_GraphMetric")]
public class V4_1_GraphMetric_Tests
{
    // ── Helper: build adjacency for a simple path graph ──────────
    private static int[][] PathAdjacency(int n)
    {
        var adj = new int[n][];
        for (int i = 0; i < n; i++)
        {
            var neighbours = new List<int>();
            if (i > 0) neighbours.Add(i - 1);
            if (i < n - 1) neighbours.Add(i + 1);
            adj[i] = neighbours.ToArray();
        }
        return adj;
    }

    private static int GraphDistance(int[][] adj, int from, int to)
    {
        var dist = new int[adj.Length];
        Array.Fill(dist, int.MaxValue);
        dist[from] = 0;
        var q = new Queue<int>();
        q.Enqueue(from);
        while (q.Count > 0)
        {
            int u = q.Dequeue();
            if (u == to) return dist[u];
            foreach (int v in adj[u])
            {
                if (dist[v] > dist[u] + 1)
                {
                    dist[v] = dist[u] + 1;
                    q.Enqueue(v);
                }
            }
        }
        return int.MaxValue; // disconnected
    }

    [Fact]
    public void V4_1_01_GraphDistance_IsSymmetric()
    {
        var adj = PathAdjacency(5);
        for (int i = 0; i < 5; i++)
        for (int j = 0; j < 5; j++)
            Assert.Equal(GraphDistance(adj, i, j), GraphDistance(adj, j, i));
    }

    [Fact]
    public void V4_1_02_GraphDistance_SatisfiesTriangleInequality()
    {
        var adj = PathAdjacency(6);
        for (int i = 0; i < 6; i++)
        for (int j = 0; j < 6; j++)
        for (int k = 0; k < 6; k++)
            Assert.True(GraphDistance(adj, i, k) <= GraphDistance(adj, i, j) + GraphDistance(adj, j, k));
    }

    [Fact]
    public void V4_1_03_GraphDistance_IsPositiveDefinite()
    {
        var adj = PathAdjacency(4);
        for (int i = 0; i < 4; i++)
        for (int j = 0; j < 4; j++)
        {
            int d = GraphDistance(adj, i, j);
            Assert.True(d >= 0);
            Assert.True((d == 0) == (i == j));
        }
    }

    [Fact]
    public void V4_1_04_GraphDistance_ConnectedGraph_FiniteAllPairs()
    {
        var adj = PathAdjacency(5);
        for (int i = 0; i < 5; i++)
        for (int j = 0; j < 5; j++)
            Assert.True(GraphDistance(adj, i, j) < int.MaxValue);
    }
}
