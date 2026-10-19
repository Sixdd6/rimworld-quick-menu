using System;
using System.Collections.Generic;
using QuickMenu.Core.Materials;
using RimWorld;
using UnityEngine;
using Verse;
using Log = QuickMenu.Diagnostics.Log;

namespace QuickMenu.Catalog;

/// <summary>One material the material ring can offer (FR-12).</summary>
internal readonly struct StuffOption
{
    internal StuffOption(ThingDef def, string label, Texture? icon, Color color)
    {
        Def = def;
        Label = label;
        Icon = icon;
        Color = color;
    }

    internal ThingDef Def { get; }

    /// <summary>The material's defName, which is also the id <see cref="StuffOrdering"/> sorts by.</summary>
    internal string Id => Def.defName;

    internal string Label { get; }

    internal Texture? Icon { get; }

    /// <summary>The stuff's own colour (the ring tints the icon and the slot's material dot with it).</summary>
    internal Color Color { get; }
}

/// <summary>The materials offered for one stuff build and what to do with them.</summary>
internal readonly struct StuffOptionSet
{
    internal static readonly StuffOptionSet NotStuffBuild = new(StuffRingOutcome.None, Array.Empty<StuffOption>(), isStuffBuild: false);

    internal StuffOptionSet(StuffRingOutcome outcome, IReadOnlyList<StuffOption> options, bool isStuffBuild = true)
    {
        Outcome = outcome;
        Options = options;
        IsStuffBuild = isStuffBuild;
    }

    /// <summary>False for orders, plain builds and foreign designators: they activate without a material.</summary>
    internal bool IsStuffBuild { get; }

    /// <summary>None: grey the slot ("no materials"). Skip: use the only option. Ring: open the material ring.</summary>
    internal StuffRingOutcome Outcome { get; }

    /// <summary>Ordered, last used first.</summary>
    internal IReadOnlyList<StuffOption> Options { get; }
}

/// <summary>
/// The materials the architect would offer for a stuff build (behaviour of <c>Designator_Build.ProcessInput</c>, 1.7): the
/// counted stuffs on the current map that <c>IsStuff</c> and <c>CanMake</c> the def, or every such stuff present in the
/// counted list in god mode; vanilla order is commonality descending then market value ascending. The designator's current
/// stuff (<c>StuffDefRaw</c>) goes first, so the order is shared with the architect.
/// </summary>
internal static class StuffOptions
{
    /// <summary>
    /// The stuffs present on the current map, in vanilla order (commonality descending, then market value ascending, ties
    /// keeping their counted order). Collected once per menu open and shared by every stuff build in it, so the per-slot
    /// cost is only the <c>CanMake</c> filter.
    /// </summary>
    internal static List<ThingDef> Present()
    {
        var present = new List<ThingDef>();
        Map? map = Find.CurrentMap;
        if (map == null)
        {
            return present;
        }

        bool god = DebugSettings.godMode;
        foreach (ThingDef stuff in map.resourceCounter.AllCountedAmounts.Keys)
        {
            if (stuff.IsStuff && (god || map.listerThings.ThingsOfDef(stuff).Count > 0))
            {
                present.Add(stuff);
            }
        }

        StableSort(present);
        return present;
    }

    /// <summary>Builds the option set. Never throws: a fault is logged once per designator type and reads as "no materials".</summary>
    internal static StuffOptionSet For(Designator designator) => For(designator, null);

    /// <summary>As <see cref="For(Designator)"/> with the map's stuffs collected once by <see cref="Present"/>; null collects them here.</summary>
    internal static StuffOptionSet For(Designator designator, List<ThingDef>? present)
    {
        if (designator is not Designator_Build build || build.PlacingDef is not ThingDef def || !def.MadeFromStuff)
        {
            return StuffOptionSet.NotStuffBuild;
        }

        try
        {
            return Build(build, def, present ?? Present());
        }
        catch (Exception ex)
        {
            Type type = designator.GetType();
            Log.LogOnce($"Stuff.Options.{type.FullName}", $"Reading the materials of {type.FullName} failed: {ex}");
            return new StuffOptionSet(StuffRingOutcome.None, Array.Empty<StuffOption>());
        }
    }

    private static StuffOptionSet Build(Designator_Build build, ThingDef def, List<ThingDef> present)
    {
        var stuffs = new List<ThingDef>(present.Count);
        foreach (ThingDef stuff in present)
        {
            if (stuff.stuffProps.CanMake(def))
            {
                stuffs.Add(stuff);
            }
        }

        var ids = new string[stuffs.Count];
        for (int i = 0; i < ids.Length; i++)
        {
            ids[i] = stuffs[i].defName;
        }

        ThingDef? last = build.StuffDefRaw;
        StuffOrder order = StuffOrdering.Order(ids, last?.defName);
        var options = new StuffOption[order.Ids.Count];
        for (int i = 0; i < options.Length; i++)
        {
            ThingDef stuff = FindById(stuffs, order.Ids[i]);
            options[i] = new StuffOption(stuff, stuff.LabelCap.ToString(), stuff.uiIcon, stuff.stuffProps.color);
        }

        return new StuffOptionSet(order.Outcome, options);
    }

    /// <summary>Insertion sort: stable (<c>List.Sort</c> is not), and the list is a few dozen stuffs at most.</summary>
    private static void StableSort(List<ThingDef> list)
    {
        for (int i = 1; i < list.Count; i++)
        {
            ThingDef item = list[i];
            int j = i - 1;
            while (j >= 0 && VanillaOrder(list[j], item) > 0)
            {
                list[j + 1] = list[j];
                j--;
            }

            list[j + 1] = item;
        }
    }

    private static ThingDef FindById(List<ThingDef> stuffs, string id)
    {
        foreach (ThingDef stuff in stuffs)
        {
            if (string.Equals(stuff.defName, id, StringComparison.Ordinal))
            {
                return stuff;
            }
        }

        throw new InvalidOperationException($"Stuff {id} vanished from its own list.");
    }

    private static int VanillaOrder(ThingDef a, ThingDef b)
    {
        int byCommonality = b.stuffProps.commonality.CompareTo(a.stuffProps.commonality);
        return byCommonality != 0 ? byCommonality : a.BaseMarketValue.CompareTo(b.BaseMarketValue);
    }
}
