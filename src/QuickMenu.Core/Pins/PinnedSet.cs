using System;
using System.Collections.Generic;

namespace QuickMenu.Core.Pins;

/// <summary>
/// Reference-identity cache of what the pin marker draws (FR-19, ADR 0009): object to <see cref="PinMark"/>, plus a
/// def-to-mark map so copies of a build designator are marked too. Lookups are O(1) and allocation-free. Rebuilt off
/// the hot path; storage is reused, and <see cref="Version"/> increments on every rebuild. Only pins the menu shows count:
/// unresolved, opaque, missing-mod and hidden-beyond-capacity pins get no mark. Contains no game types.
/// </summary>
/// <typeparam name="TObj">The designator type (compared by reference).</typeparam>
/// <typeparam name="TDef">The buildable def type (compared by reference).</typeparam>
public sealed class PinnedSet<TObj, TDef>
    where TObj : class
    where TDef : class
{
    private readonly Dictionary<TObj, PinMark> _objects = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<TDef, PinMark> _defs = new(ReferenceEqualityComparer.Instance);
    private readonly List<PinTarget<TObj, TDef>> _scratch = new();

    /// <summary>Increments on every rebuild, so the adapter can detect a stale set with one compare.</summary>
    public int Version { get; private set; }

    /// <summary>True when nothing is marked (a cheap early-out for the draw hook).</summary>
    public bool IsEmpty => _objects.Count == 0 && _defs.Count == 0;

    /// <summary>The mark of <paramref name="instance"/> by identity (<see cref="PinMark.None"/> for null or unknown).</summary>
    public PinMark Lookup(TObj? instance) =>
        instance is not null && _objects.TryGetValue(instance, out PinMark m) ? m : PinMark.None;

    /// <summary>The mark of a build def (<see cref="PinMark.Full"/> or <see cref="PinMark.None"/>).</summary>
    public PinMark LookupDef(TDef? def) =>
        def is not null && _defs.TryGetValue(def, out PinMark m) ? m : PinMark.None;

    /// <summary>Identity first; when that finds nothing, the def map (BAM edit-mode copies of a build designator).</summary>
    public PinMark Lookup(TObj? instance, TDef? def)
    {
        PinMark m = Lookup(instance);
        return m != PinMark.None ? m : LookupDef(def);
    }

    /// <summary>True when <paramref name="instance"/> has any mark.</summary>
    public bool Contains(TObj? instance) => Lookup(instance) != PinMark.None;

    /// <summary>Rebuilds from the board's shown pins through the adapter's resolver, reusing storage.</summary>
    public void Rebuild(PinBoard board, IPinTargetResolver<TObj, TDef> resolver)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(resolver);
        _scratch.Clear();
        PinnedSetBuilder.Collect(board, resolver, _scratch);
        Rebuild(_scratch);
    }

    /// <summary>Rebuilds from resolved targets given as plain data.</summary>
    public void Rebuild(IReadOnlyList<PinTarget<TObj, TDef>> targets)
    {
        ArgumentNullException.ThrowIfNull(targets);
        _objects.Clear();
        _defs.Clear();
        for (int i = 0; i < targets.Count; i++)
        {
            PinTarget<TObj, TDef> t = targets[i];
            if (t.Instance is null)
            {
                continue;
            }

            Mark(_objects, t.Instance, PinMark.Full);
            if (t.DropdownParent is not null)
            {
                Mark(_objects, t.DropdownParent, PinMark.Partial);
            }

            if (t.Def is not null)
            {
                Mark(_defs, t.Def, PinMark.Full);
            }
        }

        Version++;
    }

    private static void Mark<TKey>(Dictionary<TKey, PinMark> map, TKey key, PinMark mark)
        where TKey : notnull
    {
        if (!map.TryGetValue(key, out PinMark existing) || existing != PinMark.Full)
        {
            map[key] = mark;
        }
    }
}
