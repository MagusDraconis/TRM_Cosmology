using System;
using System.Collections.Generic;
using System.Text;

namespace TRM.Simulations.Experiments.WaveOptics;

public struct Vec2
{
    public double X;
    public double Y;

    public Vec2(double x, double y)
    {
        X = x;
        Y = y;
    }

    public double Length() => Math.Sqrt(X * X + Y * Y);

    public Vec2 Normalize()
    {
        double l = Length();
        return new Vec2(X / l, Y / l);
    }

    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vec2 operator *(Vec2 a, double s) => new(a.X * s, a.Y * s);
}
