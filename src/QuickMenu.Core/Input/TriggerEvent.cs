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
    bool SlotIsSubmenu)
{
    public static TriggerEvent TriggerPressed(Vec2 position) =>
        new(TriggerEventKind.TriggerPressed, position, Hit.Outside, 0, false);

    public static TriggerEvent TriggerReleased(Vec2 position) =>
        new(TriggerEventKind.TriggerReleased, position, Hit.Outside, 0, false);

    /// <param name="button">0 = left, 1 = right.</param>
    public static TriggerEvent MouseDown(Vec2 position, Hit hit, int button) =>
        new(TriggerEventKind.MouseDown, position, hit, button, false);

    /// <param name="slotIsSubmenu">When <paramref name="hit"/> is a slot: it opens a sub-menu rather than activating.</param>
    public static TriggerEvent MouseUp(Vec2 position, Hit hit, bool slotIsSubmenu, int button) =>
        new(TriggerEventKind.MouseUp, position, hit, button, slotIsSubmenu);

    public static TriggerEvent MouseMove(Vec2 position) =>
        new(TriggerEventKind.MouseMove, position, Hit.Outside, 0, false);

    public static TriggerEvent Escape(Vec2 position) =>
        new(TriggerEventKind.Escape, position, Hit.Outside, 0, false);

    public static TriggerEvent MapLost() =>
        new(TriggerEventKind.MapLost, default, Hit.Outside, 0, false);
}
