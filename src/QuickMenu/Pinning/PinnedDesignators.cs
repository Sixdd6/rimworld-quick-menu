using System;
using System.Collections.Generic;
using QuickMenu.Catalog;
using QuickMenu.Core.Pins;
using QuickMenu.Diagnostics;
using RimWorld;
using Verse;
using Log = QuickMenu.Diagnostics.Log;
using TargetResolver = QuickMenu.Core.Pins.IPinTargetResolver<Verse.Designator, object>;

namespace QuickMenu.Pinning;

/// <summary>
/// What the pin marker draws (FR-19, ADR 0009): the Core <see cref="PinnedSet{TObj, TDef}"/> over designators, filled from the
/// pin board through the pin service's resolved cache. Rebuilt off the draw path: by <see cref="PinService"/> after every
/// toggle, clear, forget and re-resolve (which the catalog triggers after its build and after an Ideology refresh), and by the
/// settings when the capacity changes. The marker hook only reads it. The set's def map holds the <see cref="BuildableDef"/> of
/// a build pin, the <see cref="Type"/> of an order pin and the <see cref="DesignatorDropdownGroupDef"/> of a dropdown pin, so
/// copies another mod draws (Better Architect Menu) are marked too.
/// </summary>
internal static class PinnedDesignators
{
    private static readonly PinnedSet<Designator, object> _set = new();
    private static readonly Resolver _resolver = new();

    /// <summary>True when nothing is marked (the hook's cheapest early-out).</summary>
    internal static bool IsEmpty => _set.IsEmpty;

    /// <summary>Increments on every rebuild.</summary>
    internal static int Version => _set.Version;

    /// <summary>
    /// The mark of a designator drawn as a button: identity first, then the def of a build designator, then for a designator
    /// the catalog does not hold (Better Architect Menu's copies) the type of an order or the group and elements of a dropdown.
    /// A catalogued instance is decided by its own pin only, so an unpinned <c>#n</c> of a type is never marked. Allocation-free.
    /// </summary>
    internal static PinMark Lookup(Designator designator)
    {
        PinMark mark = _set.Lookup(designator);
        if (mark != PinMark.None || !_set.HasDefs)
        {
            return mark;
        }

        return designator switch
        {
            Designator_Build build => _set.LookupDef(build.PlacingDef),
            _ when DesignatorCatalog.Find(designator) != null => PinMark.None,
            Designator_Dropdown dropdown => LookupDropdownCopy(dropdown),
            _ => _set.LookupDef(designator.GetType()),
        };
    }

    /// <summary>A dropdown copy: Full when its def group is pinned, Partial when one of its build elements is. Allocation-free.</summary>
    private static PinMark LookupDropdownCopy(Designator_Dropdown dropdown)
    {
        List<Designator> elements = dropdown.Elements;
        PinMark mark = PinMark.None;
        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i] is Designator_Build { PlacingDef: { } def })
            {
                if (_set.LookupDef(def.designatorDropdown) == PinMark.Full)
                {
                    return PinMark.Full;
                }

                if (_set.LookupDef(def) == PinMark.Full)
                {
                    mark = PinMark.Partial;
                }
            }
        }

        return mark;
    }

    /// <summary>
    /// The mark of a single entry inside an open dropdown list: its own identity only (never the def map, never its parent), so a
    /// whole-dropdown pin marks no entries. A build entry the catalog does not hold (an element of a Better Architect Menu copy)
    /// falls back to its def. Allocation-free.
    /// </summary>
    internal static PinMark LookupEntry(Designator entry)
    {
        PinMark mark = _set.Lookup(entry);
        return mark == PinMark.None && _set.HasDefs && entry is Designator_Build { PlacingDef: { } def } && DesignatorCatalog.Find(entry) == null
            ? _set.LookupDef(def)
            : mark;
    }

    /// <summary>Rebuilds from the board and the resolved cache. Never throws: a failure is logged once and the old set stays.</summary>
    internal static void Rebuild()
    {
        try
        {
            _set.Rebuild(PinService.Board, _resolver);
        }
        catch (Exception ex)
        {
            Log.LogOnce("Pinning.MarkerSet", $"Rebuilding the pin marker set failed, markers may be out of date: {ex}");
        }
    }

    private sealed class Resolver : TargetResolver
    {
        public void Resolve(PinKey key, List<PinTarget<Designator, object>> into)
        {
            if (!PinService.TryGetResolved(key, out CatalogEntry? entry))
            {
                return;
            }

            Designator designator = entry.Designator;
            switch (key.Kind)
            {
                case PinKeyKind.BuildThing:
                case PinKeyKind.BuildTerrain:
                    into.Add(new PinTarget<Designator, object>(designator, ParentOf(entry), (designator as Designator_Build)?.PlacingDef));
                    break;
                case PinKeyKind.Order:
                    // Only the type's first entry stands for its copies: a click on a copy pins that entry (DesignatorCatalog.Resolve).
                    into.Add(new PinTarget<Designator, object>(designator, ParentOf(entry), key.Ordinal is null or 1 ? designator.GetType() : null));
                    break;
                case PinKeyKind.Dropdown:
                    into.Add(new PinTarget<Designator, object>(designator, Def: DefDatabase<DesignatorDropdownGroupDef>.GetNamedSilentFail(key.Name)));
                    break;
                case PinKeyKind.ModDropdown:
                    into.Add(new PinTarget<Designator, object>(designator));
                    break;
            }
        }

        private static Designator? ParentOf(CatalogEntry entry)
        {
            IReadOnlyList<CatalogEntry> entries = DesignatorCatalog.Entries;
            return entry.ParentId >= 0 && entry.ParentId < entries.Count ? entries[entry.ParentId].Designator : null;
        }
    }
}
