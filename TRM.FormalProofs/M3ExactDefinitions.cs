using System.Numerics;

namespace TRM.FormalProofs;

/// <summary>
/// Contains the exact mathematical definitions for TRM/TQM mode locking and phase closure.
/// </summary>
public static class M3ExactDefinitions
{
    /// <summary>
    /// Exact rational frequency ratio: Omega(q, m) = (q + m) / q.
    /// </summary>
    public static Rational Omega(BigInteger q, BigInteger m) => new Rational(q + m, q);

    /// <summary>
    /// Exact rational phase lock factor: Gamma(q, m) = q / (q + m).
    /// </summary>
    public static Rational Gamma(BigInteger q, BigInteger m) => new Rational(q, q + m);

    /// <summary>
    /// Target winding number: p = q + targetShift.
    /// </summary>
    public static BigInteger PCompatible(BigInteger q, BigInteger targetShift) => q + targetShift;

    /// <summary>
    /// Exact phase closure defect: |q * Omega - p|.
    /// </summary>
    public static Rational PhaseDefect(BigInteger q, BigInteger m, BigInteger p)
    {
        var omega = Omega(q, m);
        var qRat = new Rational(q);
        var pRat = new Rational(p);
        return (qRat * omega - pRat).Abs();
    }

    /// <summary>
    /// Normalized phase closure defect: |q * Omega - p| / targetShift.
    /// </summary>
    public static Rational PhaseDefectNormalized(BigInteger q, BigInteger m, BigInteger targetShift)
    {
        var p = PCompatible(q, targetShift);
        var defect = PhaseDefect(q, m, p);
        var shiftRat = new Rational(targetShift);
        return defect / shiftRat;
    }
}
