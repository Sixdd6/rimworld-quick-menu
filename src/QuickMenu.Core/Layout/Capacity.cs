using System;

using QuickMenu.Core.Geometry;

namespace QuickMenu.Core.Layout;

/// <summary>
/// Slot capacity and Auto growth (design.md section 1.1, FR-5) for the wheel and the grid. Fixed capacity is the
/// slots the setting gives; Auto uses the smallest ring or row count that holds the pins and is "full" once it
/// cannot grow (the next size does not fit the screen minus margins, or the growth limit is reached). Pure and
/// allocation-free.
/// </summary>
public static class Capacity
{
    /// <summary>Fixed wheel capacity: the slots on rings 1..<paramref name="rings"/>.</summary>
    public static int FixedWheel(int rings) => RingCapacity.Total(rings);

    /// <summary>Fixed grid capacity: rows x columns minus the hub cell.</summary>
    public static int FixedGrid(int columns, int rows) => (columns * rows) - 1;

    /// <summary>
    /// Rings the Auto wheel draws for <paramref name="count"/> pins: the smallest m with Σ cap(1..m) at least count,
    /// at least 1 (count 0 draws ring-1 geometry) and at most <see cref="LayoutConstants.AutoMaxRings"/>.
    /// </summary>
    public static int AutoWheelRings(int count)
    {
        var m = 1;
        var sum = RingCapacity.Of(1);
        while (sum < count && m < LayoutConstants.AutoMaxRings)
        {
            m++;
            sum += RingCapacity.Of(m);
        }

        return m;
    }

    /// <summary>
    /// True when no more pins fit the Auto wheel: count fills all slots of its rings and one more ring does not
    /// fit the screen (disc plus plate inside the margins), or it already has the maximum rings.
    /// </summary>
    public static bool IsAutoWheelFull(int count, float buttonSize, Vec2 screen, float plateHeight = LayoutConstants.PlateHeight)
    {
        var m = AutoWheelRings(count);
        if (count < RingCapacity.Total(m))
        {
            return false;
        }

        return m >= LayoutConstants.AutoMaxRings || !FitsScreen(MenuBounds.ForWheel(default, buttonSize, m + 1, plateHeight), screen);
    }

    /// <summary>Rows the Auto grid draws: the smallest odd rows (1..9) with rows x columns - 1 at least count.</summary>
    public static int AutoGridRows(int count, int columns)
    {
        ValidateColumns(columns);
        var rows = GridLayout.MinRows;
        while (rows < GridLayout.MaxAutoRows && FixedGrid(columns, rows) < count)
        {
            rows += 2;
        }

        return rows;
    }

    /// <summary>
    /// True when no more pins fit the Auto grid: count fills every cell and rows + 2 do not fit the screen (panel
    /// plus a plate of <paramref name="plateWidth"/> inside the margins), or rows is already 9.
    /// </summary>
    public static bool IsAutoGridFull(
        int count,
        int columns,
        float buttonSize,
        Vec2 screen,
        float plateWidth,
        float plateHeight = LayoutConstants.PlateHeight)
    {
        var rows = AutoGridRows(count, columns);
        if (count < FixedGrid(columns, rows))
        {
            return false;
        }

        if (rows >= GridLayout.MaxAutoRows)
        {
            return true;
        }

        var panel = GridLayout.PanelCore(buttonSize, columns, rows + 2, default);
        return !FitsScreen(MenuBounds.ForPanel(panel, plateWidth, plateHeight), screen);
    }

    /// <summary>True when the box fits inside the screen minus the margin on every side.</summary>
    private static bool FitsScreen(Rect2 box, Vec2 screen) =>
        box.Width <= screen.X - (2f * LayoutConstants.Margin) && box.Height <= screen.Y - (2f * LayoutConstants.Margin);

    private static void ValidateColumns(int columns)
    {
        if (!GridLayout.IsValidColumns(columns))
        {
            throw new ArgumentOutOfRangeException(nameof(columns), columns, "Columns must be odd, 3 to 11.");
        }
    }
}
