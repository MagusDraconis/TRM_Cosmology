namespace TRM.Core.V4_1.Graphs;

/// <summary>
/// Deterministic generators for regular lattice graphs in D = 1..4.
/// All graphs are undirected, nearest-neighbour only, with periodic boundary conditions
/// where specified or open boundaries by default.
/// </summary>
public static class GraphFactory
{
    /// <summary>1D chain of length n with open boundaries.</summary>
    public static GraphTopology Chain(int n)
    {
        if (n < 2) throw new ArgumentOutOfRangeException(nameof(n));
        var adj = new int[n][];
        for (int i = 0; i < n; i++)
        {
            var nbr = new List<int>();
            if (i > 0) nbr.Add(i - 1);
            if (i < n - 1) nbr.Add(i + 1);
            adj[i] = nbr.ToArray();
        }
        return new GraphTopology(adj);
    }

    /// <summary>2D n×n square grid, open boundaries.</summary>
    public static GraphTopology SquareGrid(int n)
    {
        if (n < 2) throw new ArgumentOutOfRangeException(nameof(n));
        int N = n * n;
        var adj = new int[N][];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            int idx = y * n + x;
            var nbr = new List<int>(4);
            if (x > 0)     nbr.Add(y * n + (x - 1));
            if (x < n - 1) nbr.Add(y * n + (x + 1));
            if (y > 0)     nbr.Add((y - 1) * n + x);
            if (y < n - 1) nbr.Add((y + 1) * n + x);
            adj[idx] = nbr.ToArray();
        }
        return new GraphTopology(adj);
    }

    /// <summary>3D n×n×n cubic lattice, open boundaries.</summary>
    public static GraphTopology CubicLattice(int n)
    {
        if (n < 2) throw new ArgumentOutOfRangeException(nameof(n));
        int N = n * n * n;
        var adj = new int[N][];
        for (int z = 0; z < n; z++)
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            int idx = (z * n + y) * n + x;
            var nbr = new List<int>(6);
            if (x > 0)     nbr.Add((z * n + y) * n + (x - 1));
            if (x < n - 1) nbr.Add((z * n + y) * n + (x + 1));
            if (y > 0)     nbr.Add((z * n + (y - 1)) * n + x);
            if (y < n - 1) nbr.Add((z * n + (y + 1)) * n + x);
            if (z > 0)     nbr.Add(((z - 1) * n + y) * n + x);
            if (z < n - 1) nbr.Add(((z + 1) * n + y) * n + x);
            adj[idx] = nbr.ToArray();
        }
        return new GraphTopology(adj);
    }

    /// <summary>4D n⁴ hypercubic lattice, open boundaries.</summary>
    public static GraphTopology Hypercubic4D(int n)
    {
        if (n < 2) throw new ArgumentOutOfRangeException(nameof(n));
        int N = n * n * n * n;
        var adj = new int[N][];
        for (int w = 0; w < n; w++)
        for (int z = 0; z < n; z++)
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            int idx = ((w * n + z) * n + y) * n + x;
            var nbr = new List<int>(8);
            if (x > 0)     nbr.Add(((w * n + z) * n + y) * n + (x - 1));
            if (x < n - 1) nbr.Add(((w * n + z) * n + y) * n + (x + 1));
            if (y > 0)     nbr.Add(((w * n + z) * n + (y - 1)) * n + x);
            if (y < n - 1) nbr.Add(((w * n + z) * n + (y + 1)) * n + x);
            if (z > 0)     nbr.Add(((w * n + (z - 1)) * n + y) * n + x);
            if (z < n - 1) nbr.Add(((w * n + (z + 1)) * n + y) * n + x);
            if (w > 0)     nbr.Add((((w - 1) * n + z) * n + y) * n + x);
            if (w < n - 1) nbr.Add((((w + 1) * n + z) * n + y) * n + x);
            adj[idx] = nbr.ToArray();
        }
        return new GraphTopology(adj);
    }
}
