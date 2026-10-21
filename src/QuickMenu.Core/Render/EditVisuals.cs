using QuickMenu.Core.Geometry;
using QuickMenu.Core.Input;
using QuickMenu.Core.Layout;

namespace QuickMenu.Core.Render;

/// <summary>
/// What the renderer needs to draw a drag in edit mode (design.md S4 states 9 and 11, S5). The edit-mode controller
/// (T-57) owns the drag and fills this in each frame; the renderer only reads it. A value type: passing it allocates
/// nothing.
/// </summary>
/// <param name="Source">Slot index being dragged (occupied or reserved), or -1 when no drag is in progress.</param>
/// <param name="Target">Slot index under the mouse that a release would swap with (an empty Fixed slot included), or -1.</param>
/// <param name="Mouse">The mouse, in the same coordinates as the model's layout (the menu window is the UI screen).</param>
public readonly record struct DragVisual(int Source, int Target, Vec2 Mouse)
{
    /// <summary>No drag in progress.</summary>
    public static readonly DragVisual None = new(-1, -1, default);

    /// <summary>True while a slot is being dragged (the press has moved at least 6 px).</summary>
    public bool Active => Source >= 0;
}

/// <summary>
/// Pure rules and geometry of the edit-mode visuals (T-55): the drag source ghost and drop target (S4 states 9 and
/// 11), the dragged icon, the help panel at the top centre of the screen and the empty-menu hint plate (S5).
/// Allocation-free, safe to call from OnGUI. There is no state 7 ("+") and no state 10 (insert cursor): both were
/// removed (user decision 2026-10-19).
/// </summary>
public static class EditVisuals
{
    /// <summary>The dragged icon is drawn in a square of 0.9 B (S4 state 9).</summary>
    public const float DragIconScale = 0.9f;

    /// <summary>
    /// The dragged icon's top-left corner is this far right of and below the mouse (S4 state 9), so the mouse and
    /// the drop target under it stay visible.
    /// </summary>
    public const float DragIconOffset = 16f;

    /// <summary>Opacity of the dragged pin (S4 state 9: 80 %).</summary>
    public const float DragIconAlpha = 0.8f;

    /// <summary>The drop target's <c>accent</c> tint over <c>slotBgHover</c> (S4 state 11: 22 %).</summary>
    public const float DropTargetTintAlpha = 0.22f;

    /// <summary>Help panel top edge, UI px from the top of the screen (S5).</summary>
    public const float HelpPanelTop = 60f;

    /// <summary>Help panel maximum width in UI px (S5); the text wraps inside it.</summary>
    public const float HelpPanelMaxWidth = 520f;

    /// <summary>Help panel inner padding on every side (design.md §2 spacing 12).</summary>
    public const float HelpPanelPadding = 12f;

    /// <summary>Gap between the help text and the capacity line (design.md §2 spacing 6).</summary>
    public const float HelpPanelLineGap = 6f;

    /// <summary>The help panel keeps at least this much space to the screen edges on small UI screens (spacing 16).</summary>
    public const float ScreenMargin = 16f;

    /// <summary>Empty-menu hint plate maximum width in UI px (S5, FR-16); the hint wraps inside it.</summary>
    public const float HintPlateMaxWidth = 360f;

    /// <summary>Vertical padding of a plate: its height is the text height + 8, like the label plate (§1.1).</summary>
    public const float PlateVerticalPadding = 8f;

    /// <summary>
    /// Slot state with a drag in progress: the source is the ghost (9), the target is highlighted (11), every other
    /// slot is Normal or Empty (no hover and no press while dragging). Without a drag it is <see cref="WheelVisuals.Slot"/>.
    /// </summary>
    public static SlotVisualState Slot(int index, SlotKind kind, Hit hover, Hit pressed, in DragVisual drag)
    {
        if (!drag.Active)
        {
            return WheelVisuals.Slot(index, kind, hover, pressed);
        }

        if (index == drag.Source)
        {
            return SlotVisualState.DragSource;
        }

        if (index == drag.Target)
        {
            return SlotVisualState.DropTarget;
        }

        return kind is SlotKind.Empty or SlotKind.Reserved ? SlotVisualState.Empty : SlotVisualState.Normal;
    }

    /// <summary>Hub state with a drag in progress: never highlighted (dropping on the hub cancels; S5).</summary>
    public static HubPartState Hub(Hit hover, Hit pressed, in DragVisual drag) =>
        drag.Active ? HubPartState.Normal : WheelVisuals.Hub(hover, pressed);

    /// <summary>
    /// Label plate with a drag in progress: the drop target's label (none for an empty or reserved target), never the
    /// hub's. Without a drag it is <see cref="WheelVisuals.Plate"/>.
    /// </summary>
    public static PlateContent Plate(Hit hover, SlotKind[] kinds, int count, in DragVisual drag)
    {
        if (drag.Active && hover.Kind != HitKind.Slot)
        {
            return PlateContent.None;
        }

        return WheelVisuals.Plate(hover, kinds, count);
    }

    /// <summary>Whether the slot draws its icon: not when empty (6) or while it is the drag source (9).</summary>
    public static bool ShowsIcon(SlotVisualState state) => state is not (SlotVisualState.Empty or SlotVisualState.DragSource);

    /// <summary>Whether the slot draws its badges (12, 13, lock): not while it is the drag source (9).</summary>
    public static bool ShowsBadges(SlotVisualState state) => state != SlotVisualState.DragSource;

    /// <summary>Whether the slot gets the 22 % <c>accent</c> tint over its fill (state 11).</summary>
    public static bool ShowsDropTint(SlotVisualState state) => state == SlotVisualState.DropTarget;

    /// <summary>The dragged icon (state 9): a 0.9 B square whose top-left is (+16, +16) from <paramref name="mouse"/>.</summary>
    public static Rect2 DraggedIconRect(Vec2 mouse, float buttonSize)
    {
        float size = DragIconScale * buttonSize;
        return new Rect2(mouse.X + DragIconOffset, mouse.Y + DragIconOffset, size, size);
    }

    /// <summary>The widest the help text may be on a UI screen <paramref name="screenWidth"/> wide: the panel width less its padding.</summary>
    public static float HelpTextMaxWidth(float screenWidth)
    {
        float panel = screenWidth - (2f * ScreenMargin);
        if (panel > HelpPanelMaxWidth)
        {
            panel = HelpPanelMaxWidth;
        }

        float text = panel - (2f * HelpPanelPadding);
        return text > 0f ? text : 0f;
    }

    /// <summary>
    /// The help panel (S5): centred at the top of the screen, <see cref="HelpPanelTop"/> down. Its width fits the wider
    /// of the help text and the capacity line (<paramref name="contentWidth"/>, at most
    /// <see cref="HelpTextMaxWidth"/>) plus padding; its height holds the wrapped help text, the gap and one line.
    /// </summary>
    public static Rect2 HelpPanelRect(float screenWidth, float contentWidth, float helpHeight, float lineHeight)
    {
        float max = HelpTextMaxWidth(screenWidth);
        float content = contentWidth < max ? contentWidth : max;
        if (content < 0f)
        {
            content = 0f;
        }

        float width = content + (2f * HelpPanelPadding);
        float height = HelpPanelPadding + helpHeight + HelpPanelLineGap + lineHeight + HelpPanelPadding;
        return new Rect2((screenWidth - width) / 2f, HelpPanelTop, width, height);
    }

    /// <summary>The help text inside <paramref name="panel"/>.</summary>
    public static Rect2 HelpTextRect(Rect2 panel, float helpHeight) =>
        new(panel.X + HelpPanelPadding, panel.Y + HelpPanelPadding, panel.Width - (2f * HelpPanelPadding), helpHeight);

    /// <summary>The capacity line inside <paramref name="panel"/>, under the help text.</summary>
    public static Rect2 CapacityLineRect(Rect2 panel, float helpHeight, float lineHeight) =>
        new(panel.X + HelpPanelPadding, panel.Y + HelpPanelPadding + helpHeight + HelpPanelLineGap, panel.Width - (2f * HelpPanelPadding), lineHeight);

    /// <summary>The widest the hint text may be: the plate's maximum width less the label plate's padding.</summary>
    public static float HintTextMaxWidth => HintPlateMaxWidth - WheelVisuals.PlatePadding;

    /// <summary>
    /// The empty-menu hint plate (S5, FR-16), in place of the label plate: centred on <paramref name="centreX"/>, top =
    /// the label plate's top, width = text + 12 (at most 360), height = wrapped text height + 8.
    /// </summary>
    public static Rect2 HintPlateRect(float centreX, float top, float textWidth, float textHeight)
    {
        float width = textWidth + WheelVisuals.PlatePadding;
        if (width > HintPlateMaxWidth)
        {
            width = HintPlateMaxWidth;
        }

        return new Rect2(centreX - (width / 2f), top, width, textHeight + PlateVerticalPadding);
    }
}
