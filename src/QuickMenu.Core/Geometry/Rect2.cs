namespace QuickMenu.Core.Geometry;

/// <summary>Axis-aligned rectangle in UI pixels: top-left corner plus size.</summary>
public readonly record struct Rect2(float X, float Y, float Width, float Height)
{
    public float Right => X + Width;

    public float Bottom => Y + Height;

    public bool Contains(Vec2 p) => p.X >= X && p.X <= Right && p.Y >= Y && p.Y <= Bottom;
}
