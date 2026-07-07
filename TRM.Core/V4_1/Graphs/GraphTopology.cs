namespace TRM.Core.V4_1.Graphs;

/// <summary>
/// Immutable undirected graph with adjacency list representation.
/// Nodes are indexed 0..N-1.
/// </summary>
public sealed class GraphTopology
{
    public int NodeCount { get; }
    private readonly int[][] _adj;

    public GraphTopology(int[][] adjacency)
    {
        NodeCount = adjacency.Length;
        _adj = adjacency;
    }

    /// <summary>Neighbours of node i.</summary>
    public int[] Neighbours(int i) => _adj[i];

    /// <summary>Degree of node i.</summary>
    public int Degree(int i) => _adj[i].Length;
}
