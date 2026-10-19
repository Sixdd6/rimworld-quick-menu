using QuickMenu.Core.Geometry;
using QuickMenu.Core.Input;
using QuickMenu.Core.Layout;

namespace QuickMenu.Core.Render;

/// <summary>The base visual state of a slot (design.md S4 states 1, 2, 3 and 6).</summary>
public enum SlotVisualState
{
    /// <summary>State 1: <c>slotBg</c> + 1 px <c>slotBorder</c> ring.</summary>
    Normal = 0,

    /// <summary>State 2: <c>slotBgHover</c> + 2 px <c>accent</c> ring + 8 % white overlay; the plate shows the label.</summary>
    Hover,

    /// <summary>State 3: <c>slotBgPressed</c> while the button is held on the slot.</summary>
    Pressed,

    /// <summary>State 6 (Fixed, use mode; also reserved-missing): <c>slotBg</c> at 50 % + 1 px <c>slotBorder</c> at 70 %.</summary>
    Empty,
}

/// <summary>State of the hub, the Edit button (design.md S4a).</summary>
public enum HubPartState
{
    /// <summary>Not highlighted.</summary>
    Normal = 0,

    /// <summary><c>slotBgHover</c> + 2 px <c>accent</c> ring; the plate shows the target's label.</summary>
    Hover,

    /// <summary><c>slotBgPressed</c>.</summary>
    Pressed,
}

/// <summary>What the label plate shows (design.md S1 step 4, S4a).</summary>
public enum PlateContent
{
    /// <summary>No plate.</summary>
    None = 0,

    /// <summary>The hovered slot's label; the slot index is the hover hit's index.</summary>
    Slot,

    /// <summary>The hub's label ("Edit"; "Done" in edit mode).</summary>
    Hub,
}

/// <summary>
/// Maps model coordinates (the layout, centred on <see cref="ModelCenter"/>) to screen coordinates: the model centre
/// lands on <see cref="Origin"/> and every length is multiplied by <see cref="Scale"/>. The open menu uses
/// <see cref="Identity"/>; the settings preview (T-59) draws the same model at 50 % (design.md §6 technical need 7).
/// </summary>
public readonly record struct RenderTransform(Vec2 ModelCenter, Vec2 Origin, float Scale)
{
    /// <summary>Draw the model where its layout put it, at full size.</summary>
    public static RenderTransform Identity(Vec2 modelCenter) => new(modelCenter, modelCenter, 1f);

    public Vec2 Point(Vec2 p) =>
        new(Origin.X + ((p.X - ModelCenter.X) * Scale), Origin.Y + ((p.Y - ModelCenter.Y) * Scale));

    public float Length(float length) => length * Scale;

    public Rect2 Rect(Rect2 r)
    {
        Vec2 topLeft = Point(new Vec2(r.X, r.Y));
        return new Rect2(topLeft.X, topLeft.Y, r.Width * Scale, r.Height * Scale);
    }
}

/// <summary>
/// Pure decisions and geometry of the wheel renderer (T-29): slot and hub states from the hover and press hits, what
/// the label plate shows and where it goes. Allocation-free, safe to call from OnGUI.
/// </summary>
public static class WheelVisuals
{
    /// <summary>The designator icon is drawn in a square of 0.75 B centred on the slot (S4).</summary>
    public const float IconScale = 0.75f;

    /// <summary>Plate width = text width + 12 (§1.1).</summary>
    public const float PlatePadding = 12f;

    /// <summary>
    /// Slot state. <paramref name="pressed"/> is where the current press began (<see cref="Hit.Outside"/> when no
    /// button is held). Pressed shows only while the pointer is still over the slot, because activation happens on
    /// release inside it. Empty slots never react.
    /// </summary>
    public static SlotVisualState Slot(int index, SlotKind kind, Hit hover, Hit pressed)
    {
        if (kind == SlotKind.Empty)
        {
            return SlotVisualState.Empty;
        }

        if (hover.Kind != HitKind.Slot || hover.Index != index)
        {
            return SlotVisualState.Normal;
        }

        return pressed.Kind == HitKind.Slot && pressed.Index == index ? SlotVisualState.Pressed : SlotVisualState.Hover;
    }

    /// <summary>
    /// Hub state. The hover highlight waits for <paramref name="hoverEnabled"/> (the pointer moved
    /// ≥ <see cref="TriggerStateMachine.HoverMoveThreshold"/> px since opening), so the menu does not open with
    /// "Edit" lit; a deliberate press shows at once.
    /// </summary>
    public static HubPartState Hub(Hit hover, Hit pressed, bool hoverEnabled)
    {
        if (hover.Kind != HitKind.HubEdit)
        {
            return HubPartState.Normal;
        }

        if (pressed.Kind == HitKind.HubEdit)
        {
            return HubPartState.Pressed;
        }

        return hoverEnabled ? HubPartState.Hover : HubPartState.Normal;
    }

    /// <summary>
    /// What the label plate shows: the hovered slot's label (not for empty slots), or a hub label once hover is
    /// enabled. <paramref name="kinds"/> holds the slot kinds; only the first <paramref name="count"/> are used.
    /// </summary>
    public static PlateContent Plate(Hit hover, bool hoverEnabled, SlotKind[] kinds, int count)
    {
        switch (hover.Kind)
        {
            case HitKind.Slot:
                int n = count < kinds.Length ? count : kinds.Length;
                return hover.Index >= 0 && hover.Index < n && kinds[hover.Index] != SlotKind.Empty
                    ? PlateContent.Slot
                    : PlateContent.None;
            case HitKind.HubEdit:
                return hoverEnabled ? PlateContent.Hub : PlateContent.None;
            default:
                return PlateContent.None;
        }
    }

    /// <summary>
    /// Label plate (§1.1): centred on cx, top = <c>cy + D + plateGap</c>, width = text width + 12, at most 2 D.
    /// </summary>
    public static Rect2 PlateRect(Vec2 center, float discRadius, float textWidth, float plateHeight)
    {
        float max = discRadius + discRadius;
        float width = textWidth + PlatePadding;
        if (width > max)
        {
            width = max;
        }

        return new Rect2(center.X - (width / 2f), center.Y + discRadius + LayoutConstants.PlateGap, width, plateHeight);
    }

    /// <summary>The widest label that fits the plate; longer labels are truncated to this width once per open.</summary>
    public static float PlateMaxTextWidth(float discRadius) => discRadius + discRadius - PlatePadding;

    /// <summary>The icon square of a slot: 0.75 B, centred.</summary>
    public static Rect2 IconRect(Vec2 slotCenter, float buttonSize)
    {
        float size = buttonSize * IconScale;
        return new Rect2(slotCenter.X - (size / 2f), slotCenter.Y - (size / 2f), size, size);
    }
}
