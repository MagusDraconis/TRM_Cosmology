namespace TRM.Core.V4_1.Graphs;

/// <summary>
/// Deterministic dense symmetric eigensolver using the classical Jacobi method.
/// Suitable for small matrices (N ≤ ~100) in unit tests.
/// Classification: FRAMEWORK — numerical infrastructure.
/// </summary>
public static class DenseSymmetricEigenSolver
{
    /// <summary>
    /// Compute all eigenvalues and eigenvectors of a symmetric matrix.
    /// Returns eigenvalues sorted ascending.
    /// </summary>
    public static (double[] eigenvalues, double[,] eigenvectors) Decompose(double[,] A, int maxSweeps = 50)
    {
        int N = A.GetLength(0);
        if (A.GetLength(1) != N)
            throw new ArgumentException("Matrix must be square.");

        // Initialise eigenvectors to identity.
        var V = new double[N, N];
        var D = new double[N, N];
        for (int i = 0; i < N; i++)
        {
            V[i, i] = 1.0;
            for (int j = 0; j < N; j++)
                D[i, j] = A[i, j];
        }

        double tol = 1e-12;
        for (int sweep = 0; sweep < maxSweeps; sweep++)
        {
            double maxOff = 0;
            for (int i = 0; i < N; i++)
            for (int j = i + 1; j < N; j++)
                maxOff = Math.Max(maxOff, Math.Abs(D[i, j]));

            if (maxOff < tol) break;

            for (int p = 0; p < N; p++)
            for (int q = p + 1; q < N; q++)
            {
                double app = D[p, p], aqq = D[q, q], apq = D[p, q];
                if (Math.Abs(apq) < tol) continue;

                double theta = 0.5 * Math.Atan2(2.0 * apq, aqq - app);
                double c = Math.Cos(theta), s = Math.Sin(theta);

                // Rotate D
                D[p, p] = c * c * app + s * s * aqq - 2.0 * s * c * apq;
                D[q, q] = s * s * app + c * c * aqq + 2.0 * s * c * apq;
                D[p, q] = 0;
                D[q, p] = 0;

                for (int r = 0; r < N; r++)
                {
                    if (r == p || r == q) continue;
                    double arp = D[r, p], arq = D[r, q];
                    D[r, p] = c * arp - s * arq;
                    D[p, r] = D[r, p];
                    D[r, q] = s * arp + c * arq;
                    D[q, r] = D[r, q];
                }

                // Accumulate eigenvectors
                for (int r = 0; r < N; r++)
                {
                    double vrp = V[r, p], vrq = V[r, q];
                    V[r, p] = c * vrp - s * vrq;
                    V[r, q] = s * vrp + c * vrq;
                }
            }
        }

        var evals = new double[N];
        for (int i = 0; i < N; i++)
            evals[i] = Math.Max(0, D[i, i]); // Laplacian eigenvalues are ≥ 0

        Array.Sort(evals);
        return (evals, V);
    }

    /// <summary>Convenience: eigenvalues only.</summary>
    public static double[] Eigenvalues(double[,] A, int maxSweeps = 50)
    {
        var (evals, _) = Decompose(A, maxSweeps);
        return evals;
    }
}
