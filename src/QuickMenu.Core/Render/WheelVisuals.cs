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
/// Decorations of a slot that combine with its base state (design.md S4 states 4, 12, 13, 14). Set once per open;
/// the greyed state (5) comes from <see cref="SlotKind.Disabled"/>.
/// </summary>
[System.Flags]
public enum SlotDecor
{
    /// <summary>No decoration.</summary>
    None = 0,

    /// <summary>State 12: the slot opens a sub-menu; dropdown corner badge at 1:30.</summary>
    SubMenu = 1,

    /// <summary>State 13: the slot opens the material ring; dot at 4:30 in the last-used stuff colour.</summary>
    Material = 2,

    /// <summary>State 4: the last-used material in a material ring; 3 px <c>accent</c> ring.</summary>
    LastUsed = 4,

    /// <summary>State 14: the parent of the open satellite; 2 px <c>accent</c> ring at full opacity.</summary>
    OpenParent = 8,
}

/// <summary>Which ring a slot draws (design.md §2 ring widths).</summary>
public enum SlotRing
{
    /// <summary>1 px <c>slotBorder</c>: normal.</summary>
    Border1 = 0,

    /// <summary>1 px <c>slotBorder</c> at 70 %: empty and reserved (use mode).</summary>
    EmptyBorder1,

    /// <summary>2 px <c>accent</c>: hover, pressed, open parent.</summary>
    Accent2,

    /// <summary>3 px <c>accent</c>: last-used material.</summary>
    Accent3,

    /// <summary>Dashed 1 px <c>emptyBorder</c>: a reserved-missing pin in edit mode (state 8).</summary>
    EmptyDashed,
}

/// <summary>Slot badges, each on its own 45° diagonal at radius B/2 (design.md S4).</summary>
public enum SlotBadge
{
    /// <summary>1:30, <c>SubMenuBadge</c> glyph, 0.22 B.</summary>
    SubMenu = 0,

    /// <summary>4:30, material dot, 0.18 B.</summary>
    Material,

    /// <summary>7:30, <c>Lock</c> glyph, 0.25 B.</summary>
    Lock,
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

    /// <summary>Material dot diameter relative to B (design.md §2 badges).</summary>
    public const float MaterialDotFraction = 0.18f;

    private const float Diagonal = 0.70710678f;

    /// <summary>
    /// The ring of a slot. Empty slots keep their faint ring whatever they are decorated with. The last-used material
    /// keeps its 3 px ring when hovered or pressed (the fill shows the hover); the open parent always has 2 px.
    /// </summary>
    public static SlotRing Ring(SlotVisualState state, SlotDecor decor)
    {
        if (state == SlotVisualState.Empty)
        {
            return SlotRing.EmptyBorder1;
        }

        if ((decor & SlotDecor.LastUsed) != 0)
        {
            return SlotRing.Accent3;
        }

        if ((decor & SlotDecor.OpenParent) != 0 || state is SlotVisualState.Hover or SlotVisualState.Pressed)
        {
            return SlotRing.Accent2;
        }

        return SlotRing.Border1;
    }

    /// <summary>
    /// The ring with the slot kind and mode: a reserved-missing pin (state 8) shows a dashed <c>emptyBorder</c> ring in
    /// edit mode and looks empty in use mode; otherwise as <see cref="Ring(SlotVisualState, SlotDecor)"/>.
    /// </summary>
    public static SlotRing Ring(SlotVisualState state, SlotDecor decor, SlotKind kind, bool editing) =>
        ShowsReservedMarker(kind, editing) ? SlotRing.EmptyDashed : Ring(state, decor);

    /// <summary>Whether a slot shows the reserved-missing marker (S4 state 8: dashed ring and "?"): edit mode only.</summary>
    public static bool ShowsReservedMarker(SlotKind kind, bool editing) => editing && kind == SlotKind.Reserved;

    /// <summary>
    /// Whether <paramref name="badge"/> is drawn. Badges 12 and 13 combine with states 1–5 (also greyed); the lock
    /// marks greyed slots (state 5). Empty and reserved slots draw no badge.
    /// </summary>
    public static bool ShowsBadge(SlotBadge badge, SlotKind kind, SlotDecor decor)
    {
        if (kind is SlotKind.Empty or SlotKind.Reserved)
        {
            return false;
        }

        return badge switch
        {
            SlotBadge.SubMenu => (decor & SlotDecor.SubMenu) != 0,
            SlotBadge.Material => (decor & SlotDecor.Material) != 0,
            _ => kind == SlotKind.Disabled,
        };
    }

    /// <summary>Badge centre: on its 45° diagonal (1:30, 4:30, 7:30) at radius B/2 from the slot centre (y down).</summary>
    public static Vec2 BadgeCentre(SlotBadge badge, Vec2 slotCenter, float buttonSize)
    {
        float d = buttonSize / 2f * Diagonal;
        return badge switch
        {
            SlotBadge.SubMenu => new Vec2(slotCenter.X + d, slotCenter.Y - d),
            SlotBadge.Material => new Vec2(slotCenter.X + d, slotCenter.Y + d),
            _ => new Vec2(slotCenter.X - d, slotCenter.Y + d),
        };
    }

    /// <summary>Material dot diameter in UI px: 0.18 B.</summary>
    public static float MaterialDotSize(float buttonSize) => MaterialDotFraction * buttonSize;

    /// <summary>
    /// Material dot colour (state 13): the last-used stuff colour, opaque, or <c>textMuted</c> when there is none.
    /// Call once per open, not per frame.
    /// </summary>
    public static Rgba32 MaterialDotColor(Rgba32? lastUsedStuff) =>
        lastUsedStuff is { } c ? new Rgba32(c.R, c.G, c.B, 255) : StyleTokens.TextMuted;

    /// <summary>
    /// Scale for a badge outline (<c>rimOutline</c>, design.md §3): the same mask drawn 1 px larger on each side under
    /// the tinted badge. <paramref name="drawSize"/> is the badge's drawn size in UI px.
    /// </summary>
    public static float OutlineScale(float drawSize) => drawSize > 0f ? (drawSize + 2f) / drawSize : 1f;

    /// <summary>Alpha of the main menu: <c>dimmedMenu</c> (45 %) while a satellite is open, else opaque (S1).</summary>
    public static float MenuAlpha(bool dimmed) => dimmed ? StyleTokens.DimmedMenuAlpha : 1f;

    /// <summary>The dimmed main menu does not react to the mouse (S1): its hover and press hits are ignored.</summary>
    public static Hit Interactive(Hit hit, bool dimmed) => dimmed ? Hit.Outside : hit;

    /// <summary>
    /// Alpha of the <c>slotBg</c> veil drawn over a slot's icon. <c>Command.DrawIcon</c> sets its own colour, so the
    /// icon cannot be tinted or faded through <c>GUI.color</c>: a greyed icon (state 5) is veiled at
    /// <see cref="StyleTokens.UnavailableVeilAlpha"/>, and in a dimmed menu the icon keeps 45 % of its strength.
    /// </summary>
    public static float IconVeilAlpha(SlotKind kind, bool dimmed)
    {
        float keep = kind == SlotKind.Disabled ? 1f - StyleTokens.UnavailableVeilAlpha : 1f;
        if (dimmed)
        {
            keep *= StyleTokens.DimmedMenuAlpha;
        }

        return 1f - keep;
    }

    /// <summary>
    /// State of a satellite's hub, which is the parent slot redrawn on top (S3, state 14): it hovers and presses like
    /// a slot; the hub hit is <see cref="HitKind.HubEdit"/> in the satellite's hit layout.
    /// </summary>
    public static SlotVisualState SatelliteHub(Hit hover, Hit pressed) => Hub(hover, pressed) switch
    {
        HubPartState.Hover => SlotVisualState.Hover,
        HubPartState.Pressed => SlotVisualState.Pressed,
        _ => SlotVisualState.Normal,
    };

    /// <summary>
    /// Slot state. <paramref name="pressed"/> is where the current press began (<see cref="Hit.Outside"/> when no
    /// button is held). Pressed shows only while the pointer is still over the slot, because activation happens on
    /// release inside it. Empty slots never react.
    /// </summary>
    public static SlotVisualState Slot(int index, SlotKind kind, Hit hover, Hit pressed)
    {
        if (kind is SlotKind.Empty or SlotKind.Reserved)
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
    /// Hub state. Hover reacts at once, also when the menu opens with the pointer on the hub (user decision, UC-2b).
    /// </summary>
    public static HubPartState Hub(Hit hover, Hit pressed)
    {
        if (hover.Kind != HitKind.HubEdit)
        {
            return HubPartState.Normal;
        }

        if (pressed.Kind == HitKind.HubEdit)
        {
            return HubPartState.Pressed;
        }

        return HubPartState.Hover;
    }

    /// <summary>
    /// What the label plate shows: the hovered slot's label (not for empty slots), or the hub label when the hub is hovered. <paramref name="kinds"/> holds the slot kinds; only the first <paramref name="count"/> are used.
    /// </summary>
    public static PlateContent Plate(Hit hover, SlotKind[] kinds, int count)
    {
        switch (hover.Kind)
        {
            case HitKind.Slot:
                int n = count < kinds.Length ? count : kinds.Length;
                return hover.Index >= 0 && hover.Index < n && kinds[hover.Index] is not (SlotKind.Empty or SlotKind.Reserved)
                    ? PlateContent.Slot
                    : PlateContent.None;
            case HitKind.HubEdit:
                return PlateContent.Hub;
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
