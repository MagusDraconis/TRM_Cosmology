using Xunit;
using TRM.Core.V4_1.Graphs;

namespace TRM.Tests.V4_1;

/// <summary>
/// Validates deterministic graph generators for D = 1..4.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_GraphGenerators")]
public class V4_1_GraphGenerator_Tests
{
    [Fact]
    public void V4_1_31_ChainGraph_HasExpectedDegreePattern()
    {
        var g = GraphFactory.Chain(10);
        Assert.Equal(1, g.Degree(0));          // left endpoint
        Assert.Equal(2, g.Degree(5));          // interior
        Assert.Equal(1, g.Degree(9));          // right endpoint
    }

    [Fact]
    public void V4_1_32_SquareGrid_HasExpectedInteriorDegree()
    {
        var g = GraphFactory.SquareGrid(5);
        // corner: (0,0) -> 2 neighbours
        Assert.Equal(2, g.Degree(0));
        // interior: (2,2) -> index 2*5+2=12 -> 4 neighbours
        Assert.Equal(4, g.Degree(2 * 5 + 2));
    }

    [Fact]
    public void V4_1_33_CubicLattice_HasExpectedInteriorDegreeSix()
    {
        var g = GraphFactory.CubicLattice(4);
        // interior node (1,1,1) -> 6 neighbours
        int idx = (1 * 4 + 1) * 4 + 1;
        Assert.Equal(6, g.Degree(idx));
    }

    [Fact]
    public void V4_1_34_Hypercubic4D_HasExpectedInteriorDegreeEight()
    {
        var g = GraphFactory.Hypercubic4D(3);
        // interior node (1,1,1,1) -> 8 neighbours
        int idx = ((1 * 3 + 1) * 3 + 1) * 3 + 1;
        Assert.Equal(8, g.Degree(idx));
    }

    [Fact]
    public void V4_1_35_Generators_CreateUndirectedConnectedGraphs()
    {
        foreach (var g in new[] {
            GraphFactory.Chain(5), GraphFactory.SquareGrid(4),
            GraphFactory.CubicLattice(3), GraphFactory.Hypercubic4D(2) })
        {
            int N = g.NodeCount;
            // Undirected: A[i] contains j iff A[j] contains i.
            for (int i = 0; i < N; i++)
            foreach (int j in g.Neighbours(i))
                Assert.Contains(i, g.Neighbours(j));

            // Connected: every node reachable from node 0.
            for (int i = 1; i < N; i++)
                Assert.True(GraphMetrics.ShortestPath(g, 0, i) >= 0,
                    $"Node {i} unreachable from 0.");
        }
    }
}
