using System;

namespace QuickMenu.Core.Geometry;

/// <summary>2D point or vector in UI pixels (y grows downwards).</summary>
public readonly record struct Vec2(float X, float Y)
{
    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y);

    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y);

    public float Distance(Vec2 other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return MathF.Sqrt((dx * dx) + (dy * dy));
    }
}
