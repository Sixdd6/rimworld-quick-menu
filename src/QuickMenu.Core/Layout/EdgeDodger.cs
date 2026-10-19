using QuickMenu.Core.Geometry;

namespace QuickMenu.Core.Layout;

/// <summary>Edge dodging (design.md section 1.1). Allocation-free.</summary>
public static class EdgeDodger
{
    /// <summary>
    /// Offset to add to <paramref name="bounds"/> so it lies inside [margin, screen - margin] on each axis, using the
    /// minimum shift. On an axis where the box is larger than the allowed area it is centred on the screen instead.
    /// </summary>
    public static Vec2 Fit(Rect2 bounds, Vec2 screen, float margin = LayoutConstants.Margin) =>
        new(FitAxis(bounds.X, bounds.Width, screen.X, margin), FitAxis(bounds.Y, bounds.Height, screen.Y, margin));

    private static float FitAxis(float start, float size, float screen, float margin)
    {
        var lo = margin;
        var hi = screen - margin;
        if (size >= hi - lo)
        {
            return ((screen - size) / 2f) - start;
        }

        if (start < lo)
        {
            return lo - start;
        }

        var end = start + size;
        return end > hi ? hi - end : 0f;
    }
}
