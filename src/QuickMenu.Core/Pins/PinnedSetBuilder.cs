using System.Collections.Generic;
using QuickMenu.Core.Settings;

namespace QuickMenu.Core.Pins;

/// <summary>How a pinned object is marked on an architect button or dropdown entry (FR-19).</summary>
public enum PinMark
{
    None,

    /// <summary>The object itself is pinned: the normal marker.</summary>
    Full,

    /// <summary>Only some entries of this dropdown are pinned: a smaller, dimmer marker.</summary>
    Partial,
}

/// <summary>
/// One resolved pin as plain data for <see cref="PinnedSet{TObj, TDef}"/>. <see cref="Instance"/> is the object the pin
/// resolves to; <see cref="DropdownParent"/> is set only when the pin is a single entry of a dropdown (the dropdown then
/// gets <see cref="PinMark.Partial"/>); <see cref="Def"/> is the buildable def of a build pin, if any.
/// </summary>
public readonly record struct PinTarget<TObj, TDef>(TObj? Instance, TObj? DropdownParent = null, TDef? Def = null)
    where TObj : class
    where TDef : class;

/// <summary>
/// Supplied by the adapter: maps a pin key to the objects it resolves to. Append nothing for an unresolved or missing
/// pin; append more than one target when the key matches several instances (duplicate keys).
/// </summary>
public interface IPinTargetResolver<TObj, TDef>
    where TObj : class
    where TDef : class
{
    void Resolve(PinKey key, List<PinTarget<TObj, TDef>> into);
}

internal static class PinnedSetBuilder
{
    /// <summary>
    /// Collects the targets of the pins the menu shows (ADR 0009, decision 13): Fixed, slots below
    /// <see cref="PinBoard.Capacity"/>; Auto, every pin. Opaque and unresolved pins add nothing. Does not allocate beyond growth of <paramref name="into"/>.
    /// </summary>
    internal static void Collect<TObj, TDef>(PinBoard board, IPinTargetResolver<TObj, TDef> resolver, List<PinTarget<TObj, TDef>> into)
        where TObj : class
        where TDef : class
    {
        IReadOnlyList<PinEntry?> entries = board.Entries;
        int limit = board.Mode == SizingMode.Auto ? entries.Count : System.Math.Min(entries.Count, board.Capacity);
        for (int i = 0; i < limit; i++)
        {
            if (entries[i] is { } pin && pin.Key.Kind != PinKeyKind.Opaque)
            {
                resolver.Resolve(pin.Key, into);
            }
        }
    }
}
