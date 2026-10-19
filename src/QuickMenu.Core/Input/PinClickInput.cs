namespace QuickMenu.Core.Input;

/// <summary>Raw IMGUI event type, as mapped by the adapter from the game's EventType.</summary>
public enum RawEventType
{
    Other = 0,
    MouseDown = 1,
    MouseUp = 2,
    KeyDown = 3,
    KeyUp = 4,

    /// <summary>The event was already consumed (S-3: always the case when the prefix runs).</summary>
    Used = 5,
}

/// <summary>
/// Plain snapshot of one input event, fed by the adapter. Per spike S-3 the event is already used when
/// the prefix runs, so the adapter fills <see cref="RawType"/>/<see cref="Button"/> from raw input state
/// and the modifier flags from both the event and <c>UnityEngine.Input</c>. Click detection uses the raw
/// left-mouse state (<c>GetMouseButtonDown/Up(0)</c>) because the event type is <see cref="RawEventType.Used"/>.
/// </summary>
public readonly record struct PinClickInput(
    bool IsKeyEvent,
    RawEventType RawType,
    int Button,
    bool EventCtrl,
    bool EventAlt,
    bool EventShift,
    bool RawCtrl,
    bool RawAlt,
    bool RawShift,
    bool LeftDownThisFrame = false,
    bool LeftUpThisFrame = false,
    bool LeftHeld = false);
