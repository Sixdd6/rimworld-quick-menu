using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;

namespace QuickMenu.Core.Input;

/// <summary>Kind of input snapshot fed to the state machine.</summary>
public enum TriggerEventKind
{
    /// <summary>The bound trigger went down.</summary>
    TriggerPressed = 0,

    /// <summary>A mouse button went down.</summary>
    MouseDown,

    /// <summary>A mouse button went up.</summary>
    MouseUp,

    /// <summary>The pointer moved.</summary>
    MouseMove,

    /// <summary>Esc was pressed.</summary>
    Escape,

    /// <summary>The colony map is no longer shown.</summary>
    MapLost,

    /// <summary>The pressed mouse button is no longer held and its MouseUp never arrived (released outside the window): drop the pending press and any drag.</summary>
    PressLost,
}

/// <summary>Plain input snapshot. Build with the factory methods.</summary>
public readonly record struct TriggerEvent(
    TriggerEventKind Kind,
    Vec2 Position,
    Hit Hit,
    int Button,
    SlotKind SlotKind,
    bool Modifier = false)
{
    public static TriggerEvent TriggerPressed(Vec2 position) =>
        new(TriggerEventKind.TriggerPressed, position, Hit.Outside, 0, SlotKind.Normal);

    /// <param name="button">0 = left, 1 = right.</param>
    /// <param name="slotKind">When <paramref name="hit"/> is a slot: what the slot is (edit mode starts a drag from an occupied or reserved slot).</param>
    public static TriggerEvent MouseDown(Vec2 position, Hit hit, int button, SlotKind slotKind = SlotKind.Normal) =>
        new(TriggerEventKind.MouseDown, position, hit, button, slotKind);

    /// <param name="slotKind">When <paramref name="hit"/> is a slot: what the slot is (empty, normal, sub-menu, disabled).</param>
    /// <param name="modifier">The effective pin modifier was held for this click (read by the window from its own click event).</param>
    public static TriggerEvent MouseUp(Vec2 position, Hit hit, SlotKind slotKind, int button, bool modifier = false) =>
        new(TriggerEventKind.MouseUp, position, hit, button, slotKind, modifier);

    public static TriggerEvent MouseMove(Vec2 position) =>
        new(TriggerEventKind.MouseMove, position, Hit.Outside, 0, SlotKind.Normal);

    public static TriggerEvent Escape(Vec2 position) =>
        new(TriggerEventKind.Escape, position, Hit.Outside, 0, SlotKind.Normal);

    public static TriggerEvent PressLost() =>
        new(TriggerEventKind.PressLost, default, Hit.Outside, 0, SlotKind.Normal);

    public static TriggerEvent MapLost() =>
        new(TriggerEventKind.MapLost, default, Hit.Outside, 0, SlotKind.Normal);
}
