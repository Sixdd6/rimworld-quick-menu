using System;
using System.Collections.Generic;

namespace QuickMenu.Core.Pins;

/// <summary>
/// Ordered pin slots with Fixed sizing semantics (FR-5, FR-9, FR-14, FR-15). A slot holds a pin or is
/// empty. Slots at or beyond <see cref="Capacity"/> are hidden but kept, and return when it grows.
/// Unresolved pins (including opaque lines from a newer version) are never altered except by <see cref="Clear"/>, <see cref="Toggle"/> on their key
/// and <see cref="ForgetUnresolved"/>. Contains no game types.
/// </summary>
public sealed class PinBoard
{
    private readonly List<PinEntry?> _slots = new();
    private readonly IReadOnlyList<PinEntry?> _view;
    private int _capacity;
    private int? _cursor;

    public PinBoard(int capacity)
    {
        _view = _slots.AsReadOnly();
        Capacity = capacity;
    }

    /// <summary>Number of visible slots. Shrinking keeps hidden pins.</summary>
    public int Capacity
    {
        get => _capacity;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _capacity = value;
            if (_cursor >= value)
            {
                _cursor = null;
            }
        }
    }

    /// <summary>Insert cursor slot, or null.</summary>
    public int? Cursor => _cursor is { } c && c < _capacity ? c : null;

    /// <summary>All stored slots including hidden ones; trailing empties are trimmed.</summary>
    public IReadOnlyList<PinEntry?> Entries => _view;

    /// <summary>Puts <paramref name="entry"/> into <paramref name="index"/>, replacing the content; used when loading.</summary>
    public void SetSlot(int index, PinEntry? entry)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        while (_slots.Count <= index)
        {
            _slots.Add(null);
        }

        _slots[index] = entry;
        Trim();
    }

    /// <summary>Sets the insert cursor (a visible slot index) or clears it with null. Out of range clears it.</summary>
    public void SetCursor(int? index) => _cursor = index is >= 0 && index < _capacity ? index : null;

    /// <summary>
    /// Unpins <paramref name="key"/> in place if pinned (also when hidden or unresolved). Otherwise pins
    /// it into the cursor slot if that is free, else the first free visible slot, else returns Full.
    /// </summary>
    public PinResult Toggle(PinKey key, string label = "", string? packageId = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i]?.Key.Equals(key) == true)
            {
                _slots[i] = null;
                Trim();
                return PinResult.Unpinned;
            }
        }

        int target = _cursor is { } c && c < _capacity && IsFree(c) ? c : FirstFree();
        if (target < 0)
        {
            return PinResult.Full;
        }

        if (!PinSerializer.TryFit(target, key, packageId, label ?? string.Empty, out string? fitPackage, out string fitLabel))
        {
            return PinResult.Rejected;
        }

        SetSlot(target, new PinEntry(key, fitLabel, fitPackage));
        return PinResult.Pinned;
    }

    /// <summary>Exchanges two visible slots. Returns false (no change) if either is out of range.</summary>
    public bool Swap(int a, int b)
    {
        if (!Visible(a) || !Visible(b))
        {
            return false;
        }

        PinEntry? ea = At(a);
        PinEntry? eb = At(b);
        SetSlot(a, eb);
        SetSlot(b, ea);
        return true;
    }

    /// <summary>Empties a visible slot in place. Returns false if out of range.</summary>
    public bool Clear(int index)
    {
        if (!Visible(index))
        {
            return false;
        }

        if (index < _slots.Count)
        {
            _slots[index] = null;
            Trim();
        }

        return true;
    }

    /// <summary>The visible slots, <see cref="Capacity"/> of them; unresolved ones are reserved.</summary>
    public IReadOnlyList<PinSlot> VisibleSlots(IPinResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        var result = new PinSlot[_capacity];
        for (int i = 0; i < result.Length; i++)
        {
            PinEntry? e = At(i);
            PinSlotState state = e is null
                ? PinSlotState.Empty
                : IsResolved(resolver, e.Key) ? PinSlotState.Resolved : PinSlotState.Unresolved;
            result[i] = new PinSlot(i, state, e);
        }

        return result;
    }

    /// <summary>Counts unresolved pins, hidden ones included.</summary>
    public int CountUnresolved(IPinResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        int n = 0;
        foreach (PinEntry? e in _slots)
        {
            if (e is not null && !IsResolved(resolver, e.Key))
            {
                n++;
            }
        }

        return n;
    }

    /// <summary>Removes unresolved pins (slots become empty in place); returns how many.</summary>
    public int ForgetUnresolved(IPinResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        int n = 0;
        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i] is { } e && !IsResolved(resolver, e.Key))
            {
                _slots[i] = null;
                n++;
            }
        }

        Trim();
        return n;
    }

    private static bool IsResolved(IPinResolver resolver, PinKey key) =>
        key.Kind != PinKeyKind.Opaque && resolver.IsResolved(key);

    private PinEntry? At(int i) => i < _slots.Count ? _slots[i] : null;

    private bool Visible(int i) => i >= 0 && i < _capacity;

    private bool IsFree(int i) => At(i) is null;

    private int FirstFree()
    {
        for (int i = 0; i < _capacity; i++)
        {
            if (IsFree(i))
            {
                return i;
            }
        }

        return -1;
    }

    private void Trim()
    {
        int n = _slots.Count;
        while (n > 0 && _slots[n - 1] is null)
        {
            n--;
        }

        _slots.RemoveRange(n, _slots.Count - n);
    }
}
