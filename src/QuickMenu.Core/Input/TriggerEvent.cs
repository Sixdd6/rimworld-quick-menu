using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;

namespace QuickMenu.Core.Input;

/// <summary>Kind of input snapshot fed to the state machine.</summary>
public enum TriggerEventKind
{
    /// <summary>The bound trigger went down.</summary>
    TriggerPressed = 0,

    /// <summary>The bound trigger went up (used by hold mode; ignored in toggle mode).</summary>
    TriggerReleased,

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

    public static TriggerEvent TriggerReleased(Vec2 position) =>
        new(TriggerEventKind.TriggerReleased, position, Hit.Outside, 0, SlotKind.Normal);

    /// <param name="hit">What the pointer is over at the release (hold mode).</param>
    public static TriggerEvent TriggerReleased(Vec2 position, Hit hit) =>
        new(TriggerEventKind.TriggerReleased, position, hit, 0, SlotKind.Normal);

    /// <param name="button">0 = left, 1 = right.</param>
    public static TriggerEvent MouseDown(Vec2 position, Hit hit, int button) =>
        new(TriggerEventKind.MouseDown, position, hit, button, SlotKind.Normal);

    /// <param name="slotKind">When <paramref name="hit"/> is a slot: what the slot is (empty, normal, sub-menu, disabled).</param>
    /// <param name="modifier">The effective pin modifier was held for this click (read by the window from its own click event).</param>
    public static TriggerEvent MouseUp(Vec2 position, Hit hit, SlotKind slotKind, int button, bool modifier = false) =>
        new(TriggerEventKind.MouseUp, position, hit, button, slotKind, modifier);

    public static TriggerEvent MouseMove(Vec2 position) =>
        new(TriggerEventKind.MouseMove, position, Hit.Outside, 0, SlotKind.Normal);

    public static TriggerEvent Escape(Vec2 position) =>
        new(TriggerEventKind.Escape, position, Hit.Outside, 0, SlotKind.Normal);

    public static TriggerEvent MapLost() =>
        new(TriggerEventKind.MapLost, default, Hit.Outside, 0, SlotKind.Normal);
}
