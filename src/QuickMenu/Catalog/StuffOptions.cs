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
    /// <summary>Builds the option set. Never throws: a fault is logged once per designator type and reads as "no materials".</summary>
    internal static StuffOptionSet For(Designator designator)
    {
        if (designator is not Designator_Build build || build.PlacingDef is not ThingDef def || !def.MadeFromStuff)
        {
            return StuffOptionSet.NotStuffBuild;
        }

        try
        {
            return Build(build, def);
        }
        catch (Exception ex)
        {
            Type type = designator.GetType();
            Log.LogOnce($"Stuff.Options.{type.FullName}", $"Reading the materials of {type.FullName} failed: {ex}");
            return new StuffOptionSet(StuffRingOutcome.None, Array.Empty<StuffOption>());
        }
    }

    private static StuffOptionSet Build(Designator_Build build, ThingDef def)
    {
        Map? map = Find.CurrentMap;
        if (map == null)
        {
            return new StuffOptionSet(StuffRingOutcome.None, Array.Empty<StuffOption>());
        }

        var stuffs = new List<ThingDef>();
        bool god = DebugSettings.godMode;
        foreach (ThingDef stuff in map.resourceCounter.AllCountedAmounts.Keys)
        {
            if (stuff.IsStuff && stuff.stuffProps.CanMake(def) && (god || map.listerThings.ThingsOfDef(stuff).Count > 0))
            {
                stuffs.Add(stuff);
            }
        }

        stuffs.Sort(VanillaOrder);

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
