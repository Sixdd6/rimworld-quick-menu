using System;

using QuickMenu.Core.Geometry;

namespace QuickMenu.Core.Layout;

/// <summary>A rectangular extra button and the caller-defined kind reported in <see cref="Hit.Button(int)"/>.</summary>
public readonly record struct HitButton(Rect2 Bounds, int Kind);

/// <summary>
/// Everything <see cref="HitTester"/> needs. Arrays are owned by the caller and only the first
/// <see cref="SlotCount"/> / <see cref="ButtonCount"/> entries are used. <see cref="Plate"/> is null when no plate is shown.
/// </summary>
public readonly record struct HitLayout(
    Vec2 Center,
    float ButtonSize,
    float DiscRadius,
    SlotPlacement[] Slots,
    int SlotCount,
    Rect2? Plate = null,
    HitButton[]? Buttons = null,
    int ButtonCount = 0);

/// <summary>
/// Everything <see cref="HitTester.HitGrid"/> needs for a main grid: the hub centre, B, odd columns and rows, and the
/// optional plate and buttons (as in <see cref="HitLayout"/>). The panel and cells are derived, so nothing is allocated.
/// </summary>
public readonly record struct GridHitLayout(
    Vec2 Center,
    float ButtonSize,
    int Columns,
    int Rows,
    Rect2? Plate = null,
    HitButton[]? Buttons = null,
    int ButtonCount = 0);

/// <summary>
/// Point to typed hit. Priority: Button, HubEdit (distance ≤ P/2, the one hub target), Slot, Plate, Inside, Outside. The hub is never a slot.
/// Allocation-free, safe to call from OnGUI.
/// </summary>
public static class HitTester
{
    public static Hit Hit(Vec2 point, in HitLayout layout)
    {
        if (!float.IsFinite(point.X) || !float.IsFinite(point.Y))
        {
            return QuickMenu.Core.Layout.Hit.Outside;
        }

        var buttons = layout.Buttons;
        if (buttons is not null)
        {
            var n = layout.ButtonCount < buttons.Length ? layout.ButtonCount : buttons.Length;
            for (var i = 0; i < n; i++)
            {
                if (buttons[i].Bounds.Contains(point))
                {
                    return QuickMenu.Core.Layout.Hit.Button(buttons[i].Kind);
                }
            }
        }

        if (layout.Center.Distance(point) <= LayoutConstants.Pitch(layout.ButtonSize) / 2f)
        {
            return QuickMenu.Core.Layout.Hit.HubEdit;
        }

        var slot = WheelHitTest.SlotAt(layout.Slots, layout.SlotCount, point);
        if (slot >= 0)
        {
            return QuickMenu.Core.Layout.Hit.Slot(slot);
        }

        if (layout.Plate is { } plate && plate.Contains(point))
        {
            return QuickMenu.Core.Layout.Hit.Plate;
        }

        return layout.Center.Distance(point) <= layout.DiscRadius
            ? QuickMenu.Core.Layout.Hit.Inside
            : QuickMenu.Core.Layout.Hit.Outside;
    }

    /// <summary>
    /// Grid hit test. Priority: Button, HubEdit (the whole centre cell, edges included), Slot (inside its P x P cell; on an exact shared
    /// edge the lower index wins), Plate, Inside (panel padding), Outside. Allocation-free.
    /// </summary>
    public static Hit HitGrid(Vec2 point, in GridHitLayout layout)
    {
        if (!float.IsFinite(point.X) || !float.IsFinite(point.Y))
        {
            return QuickMenu.Core.Layout.Hit.Outside;
        }

        var buttons = layout.Buttons;
        if (buttons is not null)
        {
            var n = layout.ButtonCount < buttons.Length ? layout.ButtonCount : buttons.Length;
            for (var i = 0; i < n; i++)
            {
                if (buttons[i].Bounds.Contains(point))
                {
                    return QuickMenu.Core.Layout.Hit.Button(buttons[i].Kind);
                }
            }
        }

        var cols = layout.Columns;
        var rows = layout.Rows;
        var pitch = LayoutConstants.Pitch(layout.ButtonSize);
        var half = pitch / 2f;
        if (MathF.Abs(point.X - layout.Center.X) <= half && MathF.Abs(point.Y - layout.Center.Y) <= half)
        {
            return QuickMenu.Core.Layout.Hit.HubEdit;
        }

        var fx = (point.X - (layout.Center.X - (cols * pitch / 2f))) / pitch;
        var fy = (point.Y - (layout.Center.Y - (rows * pitch / 2f))) / pitch;
        if (fx >= 0f && fx <= cols && fy >= 0f && fy <= rows)
        {
            var col = LowerCell(fx, cols);
            var row = LowerCell(fy, rows);
            var hubRow = GridLayout.HubRow(rows);
            var hubCol = GridLayout.HubColumn(cols);

            var index = (row * cols) + col;
            return QuickMenu.Core.Layout.Hit.Slot(row > hubRow || (row == hubRow && col > hubCol) ? index - 1 : index);
        }

        if (layout.Plate is { } plate && plate.Contains(point))
        {
            return QuickMenu.Core.Layout.Hit.Plate;
        }

        return GridLayout.PanelCore(layout.ButtonSize, cols, rows, layout.Center).Contains(point)
            ? QuickMenu.Core.Layout.Hit.Inside
            : QuickMenu.Core.Layout.Hit.Outside;
    }

    /// <summary>
    /// Hit test of a sub-grid or material grid (no hub): cell <c>i</c> of <paramref name="cells"/> owns the pitch-sized
    /// square around its centre, gaps included. Then the panel is <see cref="Hit.Inside"/>, anything else
    /// <see cref="Hit.Outside"/>.
    /// </summary>
    public static Hit HitSubGrid(Vec2 point, Rect2 panel, SlotPlacement[] cells, int count, float buttonSize)
    {
        if (!float.IsFinite(point.X) || !float.IsFinite(point.Y))
        {
            return QuickMenu.Core.Layout.Hit.Outside;
        }

        var half = LayoutConstants.Pitch(buttonSize) / 2f;
        var n = count < cells.Length ? count : cells.Length;
        for (var i = 0; i < n; i++)
        {
            if (MathF.Abs(point.X - cells[i].Center.X) <= half && MathF.Abs(point.Y - cells[i].Center.Y) <= half)
            {
                return QuickMenu.Core.Layout.Hit.Slot(i);
            }
        }

        return panel.Contains(point) ? QuickMenu.Core.Layout.Hit.Inside : QuickMenu.Core.Layout.Hit.Outside;
    }

    /// <summary>True when <paramref name="point"/> is inside the pitch-sized cell around <paramref name="cellCenter"/>.</summary>
    public static bool InCell(Vec2 point, Vec2 cellCenter, float buttonSize)
    {
        var half = LayoutConstants.Pitch(buttonSize) / 2f;
        return MathF.Abs(point.X - cellCenter.X) <= half && MathF.Abs(point.Y - cellCenter.Y) <= half;
    }

    private static int LowerCell(float f, int count)
    {
        var cell = (int)MathF.Ceiling(f) - 1;
        return cell < 0 ? 0 : (cell >= count ? count - 1 : cell);
    }
}
