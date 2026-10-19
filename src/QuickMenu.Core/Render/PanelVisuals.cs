using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;

namespace QuickMenu.Core.Render;

/// <summary>
/// Pure geometry of the grid panel (design.md §1.1 "Grid", §2 grid panel component; T-54): the label plate under a
/// panel and the rects of the 9-slice <see cref="MaskId.PanelRim"/> outlines. The main grid, sub-grids and material
/// grids all use it. Allocation-free, safe to call from OnGUI.
/// </summary>
public static class PanelVisuals
{
    /// <summary>The dark outline on each side of the 2 px panel rim (design.md §2 Ring widths: rim 2 + outlines 1 + 1).</summary>
    public const float OutlineWidth = 1f;

    /// <summary>
    /// The smallest side a rect may have for <c>Widgets.DrawAtlas</c> with the <see cref="MaskId.Panel"/> atlas: two
    /// borders of <c>atlas / 4</c>. A smaller rect would overlap its corners.
    /// </summary>
    public const float MinAtlasSide = MaskSet.PanelAtlas / 2f;

    /// <summary>
    /// Label plate under a panel: centred on the panel's centre x (the menu's cx for the main grid), top = panel bottom +
    /// <c>plateGap</c>, width = text + 12, at most the panel width (the grid's analogue of the wheel's 2 D).
    /// </summary>
    public static Rect2 PlateRect(Rect2 panel, float textWidth, float plateHeight)
    {
        float width = textWidth + WheelVisuals.PlatePadding;
        if (width > panel.Width)
        {
            width = panel.Width;
        }

        float cx = panel.X + (panel.Width / 2f);
        return new Rect2(cx - (width / 2f), panel.Bottom + LayoutConstants.PlateGap, width, plateHeight);
    }

    /// <summary>The widest label that fits the plate of <paramref name="panel"/>; longer labels are truncated once per open.</summary>
    public static float PlateMaxTextWidth(Rect2 panel) => panel.Width - WheelVisuals.PlatePadding;

    /// <summary>
    /// <see cref="MaskId.PanelRim"/> draws a 2 px band inside its rect. Tinted <c>rimOutline</c> at this rect (grown by
    /// 1 px), it leaves a 1 px outline outside the accent band.
    /// </summary>
    public static Rect2 OuterOutlineRect(Rect2 panel) =>
        new(panel.X - OutlineWidth, panel.Y - OutlineWidth, panel.Width + (2f * OutlineWidth), panel.Height + (2f * OutlineWidth));

    /// <summary>The rect shrunk by 1 px: the rim mask tinted <c>rimOutline</c> here leaves a 1 px outline inside the accent band.</summary>
    public static Rect2 InnerOutlineRect(Rect2 panel) =>
        new(panel.X + OutlineWidth, panel.Y + OutlineWidth, panel.Width - (2f * OutlineWidth), panel.Height - (2f * OutlineWidth));

    /// <summary>True when <paramref name="rect"/> is large enough for the 9-slice (<see cref="MinAtlasSide"/> on both axes).</summary>
    public static bool FitsAtlas(Rect2 rect) => rect.Width >= MinAtlasSide && rect.Height >= MinAtlasSide;
}
