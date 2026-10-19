using QuickMenu.Core.Pins;

namespace QuickMenu.Core.Settings;

/// <summary>Which sound plays for a pin result (design.md S9).</summary>
public enum PinSound
{
    TickHigh,
    TickLow,
    ClickReject,
}

/// <summary>What the player is told after a pin toggle: a Keyed string, its message kind and its sound.</summary>
public readonly record struct PinFeedback(string MessageKey, bool UsesLabel, bool IsReject, PinSound Sound, bool Persist)
{
    public const string PinnedKey = "QuickMenu.Pin.Pinned";
    public const string UnpinnedKey = "QuickMenu.Pin.Unpinned";
    public const string FullKey = "QuickMenu.Pin.Full";
    public const string RejectedKey = "QuickMenu.Pin.Rejected";

    /// <summary>Settings are written only when the board changed (once per pin, never on a rejected or full result).</summary>
    public static PinFeedback For(PinResult result) => result switch
    {
        PinResult.Pinned => new PinFeedback(PinnedKey, true, false, PinSound.TickHigh, true),
        PinResult.Unpinned => new PinFeedback(UnpinnedKey, true, false, PinSound.TickLow, true),
        PinResult.Full => new PinFeedback(FullKey, false, true, PinSound.ClickReject, false),
        _ => new PinFeedback(RejectedKey, true, true, PinSound.ClickReject, false),
    };
}
