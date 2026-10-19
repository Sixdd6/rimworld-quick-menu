using QuickMenu.Core.Geometry;

namespace QuickMenu.Core.Layout;

/// <summary>Bounding boxes used for edge dodging (design.md section 1.1).</summary>
public static class MenuBounds
{
    /// <summary>
    /// Wheel disc plus the label plate below it: x = cx +/- D, top = cy - D, bottom = cy + D + plateGap + plateHeight.
    /// With B = 56, 2 rings and plate 30 relative to the centre: 336 wide, top -168, bottom +206.
    /// </summary>
    public static Rect2 ForWheel(Vec2 center, float buttonSize, int rings, float plateHeight = LayoutConstants.PlateHeight)
    {
        var d = LayoutConstants.DiscRadius(buttonSize, rings);
        var height = d + d + LayoutConstants.PlateGap + plateHeight;
        return new Rect2(center.X - d, center.Y - d, d + d, height);
    }

    /// <summary>A panel (grid) plus the label plate below it, centred on the panel.</summary>
    public static Rect2 ForPanel(Rect2 panel, float plateWidth, float plateHeight = LayoutConstants.PlateHeight)
    {
        var left = System.Math.Min(panel.X, panel.X + (panel.Width / 2f) - (plateWidth / 2f));
        var right = System.Math.Max(panel.Right, panel.X + (panel.Width / 2f) + (plateWidth / 2f));
        var bottom = panel.Bottom + LayoutConstants.PlateGap + plateHeight;
        return new Rect2(left, panel.Y, right - left, bottom - panel.Y);
    }
}
