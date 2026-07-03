using System;
using System.Numerics;

namespace TRM.FormalProofs;

/// <summary>
/// Represents an exact rational number using arbitrary-precision integers (BigInteger).
/// </summary>
public readonly struct Rational : IComparable<Rational>, IEquatable<Rational>
{
    public BigInteger Numerator { get; }
    public BigInteger Denominator { get; }

    public static Rational Zero { get; } = new Rational(BigInteger.Zero, BigInteger.One);
    public static Rational One { get; } = new Rational(BigInteger.One, BigInteger.One);

    public Rational(BigInteger numerator)
    {
        Numerator = numerator;
        Denominator = BigInteger.One;
    }

    public Rational(BigInteger numerator, BigInteger denominator)
    {
        if (denominator == BigInteger.Zero)
            throw new DivideByZeroException("Denominator cannot be zero.");

        if (denominator < 0)
        {
            numerator = -numerator;
            denominator = -denominator;
        }

        BigInteger gcd = BigInteger.GreatestCommonDivisor(BigInteger.Abs(numerator), denominator);
        if (gcd > 1)
        {
            numerator /= gcd;
            denominator /= gcd;
        }

        Numerator = numerator;
        Denominator = denominator;
    }

    public static Rational FromLong(long value) => new Rational(value);
    public static Rational FromFraction(long numerator, long denominator) => new Rational(numerator, denominator);

    public static Rational operator +(Rational r) => r;
    public static Rational operator -(Rational r) => new Rational(-r.Numerator, r.Denominator);

    public static Rational operator +(Rational a, Rational b) =>
        new Rational(a.Numerator * b.Denominator + b.Numerator * a.Denominator, a.Denominator * b.Denominator);

    public static Rational operator -(Rational a, Rational b) =>
        new Rational(a.Numerator * b.Denominator - b.Numerator * a.Denominator, a.Denominator * b.Denominator);

    public static Rational operator *(Rational a, Rational b) =>
        new Rational(a.Numerator * b.Numerator, a.Denominator * b.Denominator);

    public static Rational operator /(Rational a, Rational b) =>
        new Rational(a.Numerator * b.Denominator, a.Denominator * b.Numerator);

    public static bool operator ==(Rational a, Rational b) => a.Equals(b);
    public static bool operator !=(Rational a, Rational b) => !a.Equals(b);
    public static bool operator <(Rational a, Rational b) => a.CompareTo(b) < 0;
    public static bool operator <=(Rational a, Rational b) => a.CompareTo(b) <= 0;
    public static bool operator >(Rational a, Rational b) => a.CompareTo(b) > 0;
    public static bool operator >=(Rational a, Rational b) => a.CompareTo(b) >= 0;

    public static implicit operator Rational(long value) => new Rational(value);
    public static implicit operator Rational(BigInteger value) => new Rational(value);

    public Rational Abs() => new Rational(BigInteger.Abs(Numerator), Denominator);

    public int CompareTo(Rational other)
    {
        return (Numerator * other.Denominator).CompareTo(other.Numerator * Denominator);
    }

    public bool Equals(Rational other)
    {
        return Numerator == other.Numerator && Denominator == other.Denominator;
    }

    public override bool Equals(object? obj) => obj is Rational other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Numerator, Denominator);

    public override string ToString()
    {
        return Denominator == BigInteger.One ? Numerator.ToString() : $"{Numerator}/{Denominator}";
    }

    public double ToDouble() => (double)Numerator / (double)Denominator;
}
