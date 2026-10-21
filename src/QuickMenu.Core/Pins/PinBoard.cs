using System;
using System.Collections.Generic;
using QuickMenu.Core.Settings;

namespace QuickMenu.Core.Pins;

/// <summary>
/// Ordered pins in Fixed or Auto sizing mode (FR-5, FR-9, FR-14, FR-15). Fixed: a slot holds a pin or is
/// empty, new pins take the first free slot, and slots at or beyond <see cref="Capacity"/> are hidden but kept.
/// Auto: a compact ordered list (no gaps), new pins go to the end, clearing moves later pins up, and unresolved
/// pins are hidden from <see cref="VisibleSlots"/> but keep their place. In Auto, <see cref="Capacity"/> is the
/// pin limit and indexes (<see cref="PinSlot.Index"/>, <see cref="Clear"/>, <see cref="Swap"/>) are positions in <see cref="Entries"/>.
/// Unresolved pins (including opaque lines from a newer version) are never altered except by <see cref="Clear"/>, <see cref="Toggle"/> on their key
/// and <see cref="ForgetUnresolved"/>. Contains no game types.
/// </summary>
public sealed class PinBoard
{
    private readonly List<PinEntry?> _slots = new();
    private readonly IReadOnlyList<PinEntry?> _view;
    private int _capacity;
    private SizingMode _mode = SizingMode.Fixed;
    private PinEntry?[]? _fixedSnapshot;

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
        }
    }

    /// <summary>
    /// Sizing mode. Switching to Auto closes the gaps, keeping order; switching to Fixed keeps every pin in its position.
    /// Switching back to Fixed with no pin changed in between restores the Fixed positions from before Auto, so
    /// trying Auto in the settings does not compact a Fixed layout for good. No pin is lost either way (pins beyond
    /// <see cref="Capacity"/> stay hidden).
    /// </summary>
    public SizingMode Mode
    {
        get => _mode;
        set
        {
            if (value == _mode)
            {
                return;
            }

            _mode = value;
            if (value == SizingMode.Auto)
            {
                _fixedSnapshot = _slots.ToArray();
                Compact();
            }
            else if (_fixedSnapshot is { } snapshot)
            {
                _fixedSnapshot = null;
                _slots.Clear();
                _slots.AddRange(snapshot);
                Trim();
            }
        }
    }

    /// <summary>All stored slots including hidden ones; trailing empties are trimmed.</summary>
    public IReadOnlyList<PinEntry?> Entries => _view;

    /// <summary>Puts <paramref name="entry"/> into <paramref name="index"/>, replacing the content; used when loading.</summary>
    public void SetSlot(int index, PinEntry? entry)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        _fixedSnapshot = null;
        while (_slots.Count <= index)
        {
            _slots.Add(null);
        }

        _slots[index] = entry;
        Trim();
        if (_mode == SizingMode.Auto)
        {
            Compact();
        }
    }

    /// <summary>
    /// Unpins <paramref name="key"/> in place if pinned (also when hidden or unresolved). Otherwise pins
    /// it into the first free visible slot (Fixed) or at the end (Auto), else returns Full.
    /// </summary>
    public PinResult Toggle(PinKey key, string label = "", string? packageId = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i]?.Key.Equals(key) == true)
            {
                RemoveAt(i);
                return PinResult.Unpinned;
            }
        }

        int target = _mode == SizingMode.Auto ? (_slots.Count < _capacity ? _slots.Count : -1) : FirstFree();
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

    /// <summary>
    /// Exchanges two slots (Fixed: either may be empty, so the pin moves; Auto: two pins exchange places in the list).
    /// Returns false (no change) if either is out of range.
    /// </summary>
    public bool Swap(int a, int b)
    {
        if (!InRange(a) || !InRange(b))
        {
            return false;
        }

        PinEntry? ea = At(a);
        PinEntry? eb = At(b);
        SetSlot(a, eb);
        SetSlot(b, ea);
        return true;
    }

    /// <summary>Empties a slot in place (Fixed) or removes it so later pins move up (Auto). Returns false if out of range.</summary>
    public bool Clear(int index)
    {
        if (!InRange(index))
        {
            return false;
        }

        if (index < _slots.Count)
        {
            RemoveAt(index);
        }

        return true;
    }

    /// <summary>
    /// Fixed: the <see cref="Capacity"/> visible slots, unresolved ones reserved. Auto: only the resolved pins in order
    /// (unresolved are hidden), each with its list position as <see cref="PinSlot.Index"/>.
    /// </summary>
    public IReadOnlyList<PinSlot> VisibleSlots(IPinResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        if (_mode == SizingMode.Auto)
        {
            var list = new List<PinSlot>(_slots.Count);
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i] is { } pin && IsResolved(resolver, pin.Key))
                {
                    list.Add(new PinSlot(i, PinSlotState.Resolved, pin));
                }
            }

            return list;
        }

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

    /// <summary>
    /// How many pins the menu shows and counts toward its size: Auto, the resolved pins (hidden unresolved ones reserve no
    /// space); Fixed, the pins in slots below <see cref="Capacity"/> (unresolved ones reserve their slot, hidden ones are not counted).
    /// </summary>
    public int VisibleCount(IPinResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        int n = 0;
        int limit = _mode == SizingMode.Auto ? _slots.Count : Math.Min(_slots.Count, _capacity);
        for (int i = 0; i < limit; i++)
        {
            if (_slots[i] is { } e && (_mode != SizingMode.Auto || IsResolved(resolver, e.Key)))
            {
                n++;
            }
        }

        return n;
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

    /// <summary>Removes unresolved pins (Fixed: slots become empty in place; Auto: later pins move up); returns how many.</summary>
    public int ForgetUnresolved(IPinResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        return Forget(e => !IsResolved(resolver, e.Key));
    }

    /// <summary>Removes exactly the pins for which <paramref name="shouldForget"/> is true (opaque ones too, if it says so); returns how many.</summary>
    public int Forget(Func<PinEntry, bool> shouldForget)
    {
        ArgumentNullException.ThrowIfNull(shouldForget);
        int n = 0;
        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i] is { } e && shouldForget(e))
            {
                _slots[i] = null;
                n++;
            }
        }

        if (n > 0)
        {
            _fixedSnapshot = null;
        }

        Trim();
        if (_mode == SizingMode.Auto)
        {
            Compact();
        }

        return n;
    }

    private static bool IsResolved(IPinResolver resolver, PinKey key) =>
        key.Kind != PinKeyKind.Opaque && resolver.IsResolved(key);

    private PinEntry? At(int i) => i < _slots.Count ? _slots[i] : null;

    private bool InRange(int i) => i >= 0 && i < (_mode == SizingMode.Auto ? _slots.Count : _capacity);

    private bool IsFree(int i) => At(i) is null;

    private void RemoveAt(int i)
    {
        _fixedSnapshot = null;
        if (_mode == SizingMode.Auto)
        {
            _slots.RemoveAt(i);
        }
        else
        {
            _slots[i] = null;
            Trim();
        }
    }

    private void Compact() => _slots.RemoveAll(static e => e is null);

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
