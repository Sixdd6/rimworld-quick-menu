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
/// Point to typed hit. Priority: HubEdit, Button, HubClose, Slot, Plate, Inside, Outside. The hub is never a slot.
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

        var b = layout.ButtonSize;
        var editCentre = new Vec2(layout.Center.X, layout.Center.Y + LayoutConstants.HubSubOffset(b));
        if (editCentre.Distance(point) <= LayoutConstants.HubSubDiameter(b) / 2f)
        {
            return QuickMenu.Core.Layout.Hit.HubEdit;
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

        if (layout.Center.Distance(point) <= LayoutConstants.Pitch(b) / 2f)
        {
            return QuickMenu.Core.Layout.Hit.HubClose;
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
}
