using System;

using QuickMenu.Core.Geometry;

namespace QuickMenu.Core.Layout;

/// <summary>
/// Main grid layout (design.md section 1.1 "Grid (main)"). Columns and rows are odd so the centre cell, the hub (the
/// Edit button, never a slot), sits under the mouse. Slots are in reading order skipping the hub; each slot is a
/// P x P cell, <see cref="SlotPlacement.Ring"/> holds the 1-based row, radius and angle are 0 (as for sub-grids).
/// </summary>
public static class GridLayout
{
    public const int MinColumns = 3;
    public const int MaxColumns = 11;
    public const int MinRows = 1;

    /// <summary>Largest Fixed row count (the rows setting).</summary>
    public const int MaxFixedRows = 7;

    /// <summary>Largest row count the Auto grid can grow to.</summary>
    public const int MaxAutoRows = 9;

    public static bool IsValidColumns(int columns) => columns is >= MinColumns and <= MaxColumns && (columns & 1) == 1;

    public static bool IsValidFixedRows(int rows) => rows is >= MinRows and <= MaxFixedRows && (rows & 1) == 1;

    public static bool IsValidRows(int rows) => rows is >= MinRows and <= MaxAutoRows && (rows & 1) == 1;

    /// <summary>Row of the hub cell, 0-based.</summary>
    public static int HubRow(int rows) => (rows - 1) / 2;

    /// <summary>Column of the hub cell, 0-based.</summary>
    public static int HubColumn(int columns) => (columns - 1) / 2;

    /// <summary>Number of slots: rows x columns minus the hub.</summary>
    public static int SlotCount(in GridParams p) => (p.Rows * p.Columns) - 1;

    /// <summary>Panel rect: cols x P - G + 2 rimPad wide, rows x P - G + 2 rimPad tall, centred on the hub.</summary>
    public static Rect2 Panel(in GridParams p)
    {
        Validate(p);
        return PanelCore(p.ButtonSize, p.Columns, p.Rows, p.Center);
    }

    /// <summary>The hub cell: the P x P square around the centre, which is the Edit button's whole hit area.</summary>
    public static Rect2 HubCell(in GridParams p)
    {
        Validate(p);
        var pitch = LayoutConstants.Pitch(p.ButtonSize);
        return new Rect2(p.Center.X - (pitch / 2f), p.Center.Y - (pitch / 2f), pitch, pitch);
    }

    public static SlotPlacement[] Compute(GridParams p)
    {
        Validate(p);
        var slots = new SlotPlacement[SlotCount(p)];
        var pitch = LayoutConstants.Pitch(p.ButtonSize);
        var hubRow = HubRow(p.Rows);
        var hubCol = HubColumn(p.Columns);
        var index = 0;
        for (var r = 0; r < p.Rows; r++)
        {
            for (var c = 0; c < p.Columns; c++)
            {
                if (r == hubRow && c == hubCol)
                {
                    continue;
                }

                var center = new Vec2(p.Center.X + ((c - hubCol) * pitch), p.Center.Y + ((r - hubRow) * pitch));
                slots[index] = new SlotPlacement(index, r + 1, center, 0f, 0f, pitch / 2f);
                index++;
            }
        }

        return slots;
    }

    /// <summary>
    /// Places the grid with its hub on <paramref name="mouse"/>, shifted so the panel and its label plate (of
    /// <paramref name="plateWidth"/>) fit the screen.
    /// </summary>
    public static PlacedGrid Place(
        float buttonSize,
        int columns,
        int rows,
        Vec2 mouse,
        Vec2 screen,
        float plateWidth,
        float plateHeight = LayoutConstants.PlateHeight)
    {
        var at = new GridParams(buttonSize, columns, rows, mouse);
        Validate(at);
        var bounds = MenuBounds.ForPanel(PanelCore(buttonSize, columns, rows, mouse), plateWidth, plateHeight);
        var center = mouse + EdgeDodger.Fit(bounds, screen);
        var p = at with { Center = center };
        var panel = PanelCore(buttonSize, columns, rows, center);
        return new PlacedGrid(center, panel, Compute(p), MenuBounds.ForPanel(panel, plateWidth, plateHeight));
    }

    internal static Rect2 PanelCore(float buttonSize, int columns, int rows, Vec2 center)
    {
        var pitch = LayoutConstants.Pitch(buttonSize);
        var width = (columns * pitch) - LayoutConstants.Gap + (2f * LayoutConstants.RimPad);
        var height = (rows * pitch) - LayoutConstants.Gap + (2f * LayoutConstants.RimPad);
        return new Rect2(center.X - (width / 2f), center.Y - (height / 2f), width, height);
    }

    private static void Validate(in GridParams p)
    {
        if (!IsValidColumns(p.Columns))
        {
            throw new ArgumentOutOfRangeException(nameof(p), p.Columns, "Columns must be odd, 3 to 11.");
        }

        if (!IsValidRows(p.Rows))
        {
            throw new ArgumentOutOfRangeException(nameof(p), p.Rows, "Rows must be odd, 1 to 9.");
        }

        if (!(p.ButtonSize > 0f) || !float.IsFinite(p.ButtonSize))
        {
            throw new ArgumentOutOfRangeException(nameof(p), p.ButtonSize, "Button size must be positive and finite.");
        }

        if (!float.IsFinite(p.Center.X) || !float.IsFinite(p.Center.Y))
        {
            throw new ArgumentOutOfRangeException(nameof(p), p.Center, "Center must be finite.");
        }
    }
}
