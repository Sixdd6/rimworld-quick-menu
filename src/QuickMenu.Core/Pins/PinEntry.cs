using System;

namespace QuickMenu.Core.Pins;

/// <summary>A stored pin: identity plus the label and mod remembered for when the target is missing.</summary>
public sealed record PinEntry
{
    public PinEntry(PinKey key, string label = "", string? packageId = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        Key = key;
        Label = label ?? string.Empty;
        PackageId = packageId;
    }

    public PinKey Key { get; }

    public string Label { get; }

    public string? PackageId { get; }
}

/// <summary>State of a visible slot.</summary>
public enum PinSlotState
{
    Empty,
    Resolved,

    /// <summary>The pin's target is missing; the slot is reserved and shown as "?" in edit mode.</summary>
    Unresolved,
}

/// <summary>One visible slot of the board.</summary>
public readonly record struct PinSlot(int Index, PinSlotState State, PinEntry? Entry);
