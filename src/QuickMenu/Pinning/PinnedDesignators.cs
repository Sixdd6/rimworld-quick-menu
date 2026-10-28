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
/// a build pin and the <see cref="Type"/> of an order pin, so copies another mod draws (Better Architect Menu) are marked too.
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
    /// The mark of a designator drawn as a button: identity first, then the def of a build designator, then the type of an
    /// order designator the catalog does not hold (Better Architect Menu's copies). A catalogued instance is decided by its own
    /// pin only, so an unpinned <c>#n</c> of a type is never marked. Allocation-free.
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
            Designator_Dropdown => PinMark.None,
            _ => DesignatorCatalog.Find(designator) == null ? _set.LookupDef(designator.GetType()) : PinMark.None,
        };
    }

    /// <summary>
    /// The mark of a single entry inside an open dropdown list: its own identity only (never the def map, never its parent), so a
    /// whole-dropdown pin marks no entries. Allocation-free.
    /// </summary>
    internal static PinMark LookupEntry(Designator entry) => _set.Lookup(entry);

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
