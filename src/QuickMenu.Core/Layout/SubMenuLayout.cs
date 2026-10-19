using System;

using QuickMenu.Core.Geometry;

namespace QuickMenu.Core.Layout;

/// <summary>What to do for a sub-menu or material list of <c>n</c> options (FR-12).</summary>
public enum SubMenuOutcome
{
    /// <summary>n = 0: nothing to show; the parent is greyed.</summary>
    None,

    /// <summary>n = 1: no ring; activate the single option directly.</summary>
    Skip,

    /// <summary>n &gt;= 2: open a satellite or sub-grid.</summary>
    Open,
}

/// <summary>
/// A wheel satellite on screen. <see cref="Hub"/> is the (edge-dodged) satellite centre: the parent slot sits there,
/// redrawn as the hub, so if dodging moved the disc the hub moved with it. <see cref="Slots"/> are in option order
/// (ring 1 first, from 12 o'clock clockwise). <see cref="Bounds"/> is the disc plus plate box used for dodging.
/// </summary>
public readonly record struct PlacedSatellite(Vec2 Hub, int Rings, float DiscRadius, SlotPlacement[] Slots, Rect2 Bounds);

/// <summary>
/// A sub-grid on screen. <see cref="Cells"/> are in reading order; <see cref="SlotPlacement.Ring"/> holds the 1-based
/// row, <see cref="SlotPlacement.Radius"/> and angle are 0, and hit testing is the P x P square around the centre.
/// <see cref="OnRight"/> is the side finally used.
/// </summary>
public readonly record struct PlacedSubGrid(Rect2 Panel, int Columns, int Rows, bool OnRight, SlotPlacement[] Cells, Rect2 Bounds);

/// <summary>
/// Sub-menu placement (design.md section 1.1 "Sub-menu and material placement"): wheel satellites and grid sub-grids.
/// A material ring opened from a sub-menu option replaces the sub-menu at the same anchor, so callers simply call
/// the same method again with the original parent position. Pure; no game types.
/// </summary>
public static class SubMenuLayout
{
    /// <summary>Largest grid-column count of a sub-grid.</summary>
    public const int MaxSubGridColumns = 6;

    public static SubMenuOutcome Classify(int count) => count switch
    {
        <= 0 => SubMenuOutcome.None,
        1 => SubMenuOutcome.Skip,
        _ => SubMenuOutcome.Open,
    };

    /// <summary>Smallest m with the sum of cap(1..m) at least <paramref name="count"/> (count &gt;= 1).</summary>
    public static int RingsFor(int count)
    {
        if (count < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Count must be at least 1.");
        }

        var m = 0;
        var sum = 0;
        while (sum < count)
        {
            m++;
            sum += RingCapacity.Of(m);
        }

        return m;
    }

    /// <summary>
    /// Writes the <paramref name="count"/> satellite slots around <paramref name="center"/> into
    /// <paramref name="destination"/>. Inner rings are full; the outer ring holds the rest spread evenly from 12 o'clock.
    /// Allocation-free.
    /// </summary>
    public static void ComputeSatelliteSlots(int count, float buttonSize, Vec2 center, Span<SlotPlacement> destination)
    {
        ValidateSize(buttonSize);
        RingsFor(count);
        WheelLayout.ComputeSpread(count, buttonSize, center, destination);
    }

    /// <summary>
    /// Places a satellite for <paramref name="count"/> (&gt;= 1) options around <paramref name="parent"/>, edge-dodged
    /// on its own (disc plus label plate). The hub moves with the disc.
    /// </summary>
    public static PlacedSatellite PlaceSatellite(
        int count,
        float buttonSize,
        Vec2 parent,
        Vec2 screen,
        float plateHeight = LayoutConstants.PlateHeight)
    {
        ValidateSize(buttonSize);
        var rings = RingsFor(count);
        var bounds = MenuBounds.ForWheel(parent, buttonSize, rings, plateHeight);
        var hub = parent + EdgeDodger.Fit(bounds, screen);
        var slots = new SlotPlacement[count];
        ComputeSatelliteSlots(count, buttonSize, hub, slots);
        return new PlacedSatellite(
            hub,
            rings,
            LayoutConstants.DiscRadius(buttonSize, rings),
            slots,
            MenuBounds.ForWheel(hub, buttonSize, rings, plateHeight));
    }

    /// <summary>Columns for a sub-grid of <paramref name="count"/> options: min(ceil(sqrt(n)), 6).</summary>
    public static int SubGridColumns(int count)
    {
        if (count < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Count must be at least 1.");
        }

        var cols = (int)Math.Ceiling(Math.Sqrt(count));
        while ((cols * cols) < count)
        {
            cols++;
        }

        while (cols > 1 && ((cols - 1) * (cols - 1)) >= count)
        {
            cols--;
        }

        return Math.Min(cols, MaxSubGridColumns);
    }

    /// <summary>Panel width of a sub-grid of <paramref name="count"/> options: what to pass as the plate width to <see cref="PlaceSubGrid"/>.</summary>
    public static float SubGridWidth(int count, float buttonSize)
    {
        ValidateSize(buttonSize);
        return (SubGridColumns(count) * LayoutConstants.Pitch(buttonSize)) - LayoutConstants.Gap + (2f * LayoutConstants.RimPad);
    }

    /// <summary>
    /// Places a sub-grid beside the main panel. The side is right when <c>parent.X &gt;= mainCenterX</c>, else left,
    /// and switches if the chosen side leaves the screen margin horizontally and the other does not. The first row's
    /// cell centres sit at <c>parent.Y</c>. The result is then edge-dodged (panel plus a plate of
    /// <paramref name="plateWidth"/>).
    /// </summary>
    public static PlacedSubGrid PlaceSubGrid(
        int count,
        float buttonSize,
        Rect2 mainPanel,
        float mainCenterX,
        Vec2 parent,
        Vec2 screen,
        float plateWidth,
        float plateHeight = LayoutConstants.PlateHeight)
    {
        ValidateSize(buttonSize);
        var cols = SubGridColumns(count);
        var rows = (count + cols - 1) / cols;
        var pitch = LayoutConstants.Pitch(buttonSize);
        var pad = LayoutConstants.RimPad;
        var width = (cols * pitch) - LayoutConstants.Gap + (2f * pad);
        var height = (rows * pitch) - LayoutConstants.Gap + (2f * pad);
        var top = parent.Y - (buttonSize / 2f) - pad;

        var right = parent.X >= mainCenterX;
        var rightX = mainPanel.Right + LayoutConstants.Gap;
        var leftX = mainPanel.X - LayoutConstants.Gap - width;
        if (right && !FitsX(rightX, width, screen.X) && FitsX(leftX, width, screen.X))
        {
            right = false;
        }
        else if (!right && !FitsX(leftX, width, screen.X) && FitsX(rightX, width, screen.X))
        {
            right = true;
        }

        var panel = new Rect2(right ? rightX : leftX, top, width, height);
        var shift = EdgeDodger.Fit(MenuBounds.ForPanel(panel, plateWidth, plateHeight), screen);
        panel = new Rect2(panel.X + shift.X, panel.Y + shift.Y, width, height);

        var cells = new SlotPlacement[count];
        for (var i = 0; i < count; i++)
        {
            var row = i / cols;
            var col = i % cols;
            cells[i] = new SlotPlacement(
                i,
                row + 1,
                new Vec2(panel.X + pad + (buttonSize / 2f) + (col * pitch), panel.Y + pad + (buttonSize / 2f) + (row * pitch)),
                0f,
                0f,
                pitch / 2f);
        }

        return new PlacedSubGrid(panel, cols, rows, right, cells, MenuBounds.ForPanel(panel, plateWidth, plateHeight));
    }

    private static bool FitsX(float x, float width, float screenWidth) =>
        x >= LayoutConstants.Margin && x + width <= screenWidth - LayoutConstants.Margin;

    private static void ValidateSize(float buttonSize)
    {
        if (!(buttonSize > 0f))
        {
            throw new ArgumentOutOfRangeException(nameof(buttonSize), buttonSize, "Button size must be positive.");
        }
    }
}
