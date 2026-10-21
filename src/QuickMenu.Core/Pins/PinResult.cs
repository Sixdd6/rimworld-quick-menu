namespace QuickMenu.Core.Pins;

/// <summary>Outcome of <see cref="PinBoard.Toggle"/>.</summary>
public enum PinResult
{
    Pinned,
    Unpinned,
    Full,

    /// <summary>The key cannot be stored within the load limits (too many children or too long); nothing changed.</summary>
    Rejected,
}
