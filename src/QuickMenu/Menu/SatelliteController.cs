using System;
using System.Collections.Generic;
using QuickMenu.Catalog;
using QuickMenu.Core.Geometry;
using QuickMenu.Core.Input;
using QuickMenu.Core.Layout;
using QuickMenu.Core.Materials;
using QuickMenu.Core.Render;
using RimWorld;
using UnityEngine;
using Verse;
using Log = QuickMenu.Diagnostics.Log;

namespace QuickMenu.Menu;

/// <summary>
/// One satellite ring on screen (design.md S3): a dropdown's sub-ring (each option a designator) or the material ring of a
/// stuff build. Built once when it opens; everything the window needs per frame is cached here (model, tips, reasons).
/// </summary>
internal sealed class RingContent
{
    private readonly string?[] _tips;
    private readonly string?[] _reasons;

    internal RingContent(WheelModel model, WheelModel hubSource, int hubIndex, int parentSlot, int count)
    {
        Model = model;
        HubSource = hubSource;
        HubIndex = hubIndex;
        ParentSlot = parentSlot;
        Designators = new Designator?[count];
        Stuffs = new ThingDef?[count];
        _tips = new string?[count];
        _reasons = new string?[count];
    }

    /// <summary>The satellite model: centre = the satellite hub, slots = the options.</summary>
    internal WheelModel Model { get; }

    /// <summary>The model holding the parent slot the hub redraws (the main menu, or the sub-ring for a material ring).</summary>
    internal WheelModel HubSource { get; }

    internal int HubIndex { get; }

    /// <summary>The main menu slot this satellite opened from (it stays lit while the rest dims).</summary>
    internal int ParentSlot { get; }

    internal bool IsMaterial { get; init; }

    /// <summary>Sub-ring: the designator behind each option. Material ring: unused.</summary>
    internal Designator?[] Designators { get; }

    /// <summary>
    /// Material ring: the stuff of each option. Sub-ring: the only stuff of an option whose ring was skipped (one material),
    /// otherwise null.
    /// </summary>
    internal ThingDef?[] Stuffs { get; }

    /// <summary>Material ring: the stuff build to activate.</summary>
    internal Designator? Target { get; init; }

    /// <summary>The dropdown the options belong to, so the architect remembers the chosen element.</summary>
    internal Designator_Dropdown? Dropdown { get; init; }

    /// <summary>Preset reasons for options greyed for no materials; other reasons are composed on first use.</summary>
    internal void SetReason(int slot, string reason) => _reasons[slot] = reason;

    internal string? Reason(int slot)
    {
        Designator? d = Designators[slot];
        if (Model.Kinds[slot] != SlotKind.Disabled)
        {
            return null;
        }

        return _reasons[slot] ??= d == null ? "QuickMenu.Menu.Unavailable".Translate().Resolve() : UnavailableReasons.For(d);
    }

    /// <summary>Tooltip of a sub-ring option (the designator's own text); null for material options (the plate names them).</summary>
    internal string? Tip(int slot)
    {
        Designator? d = Designators[slot];
        if (d == null)
        {
            return null;
        }

        string? tip = _tips[slot];
        if (tip == null)
        {
            try
            {
                string desc = d.Desc ?? string.Empty;
                tip = desc.Length == 0 ? d.LabelCap : d.LabelCap + "\n\n" + desc;
                if (Model.Kinds[slot] == SlotKind.Disabled)
                {
                    tip += "\n\n<color=#" + ColorUtility.ToHtmlStringRGB(QMStyle.Danger) + ">" + Reason(slot) + "</color>";
                }
            }
            catch (Exception ex)
            {
                Log.LogOnce("Menu.Tooltip", $"Reading a designator's tooltip failed: {ex.GetType().Name}");
                tip = string.Empty;
            }

            _tips[slot] = tip;
        }

        return tip.Length == 0 ? null : tip;
    }
}

/// <summary>
/// The satellite state of one open menu (FR-10..13): a sub-ring or material ring over the dimmed main menu, plus the
/// material ring a sub-ring option can open on top of it. Pure placement and decisions are Core
/// (<see cref="SubMenuLayout"/>, <see cref="StuffSlotPolicy"/>); this class turns game objects into rings once per open.
/// </summary>
internal sealed class SatelliteController
{
    private readonly MenuContent _main;
    private RingContent? _sub;
    private RingContent? _material;

    internal SatelliteController(MenuContent main)
    {
        _main = main;
    }

    /// <summary>The ring on screen: the material ring if one is open, else the sub-ring.</summary>
    internal RingContent? Active => _material ?? _sub;

    /// <summary>Opens the satellite of main slot <paramref name="slot"/>. Returns false when it has nothing to show.</summary>
    internal bool OpenFromMain(int slot)
    {
        Designator? d = _main.Designators[slot];
        if (d == null)
        {
            return false;
        }

        if (d is Designator_Dropdown dropdown)
        {
            _sub = BuildSub(slot, dropdown);
            return _sub != null;
        }

        Designator target = _main.Targets[slot] ?? d;
        _material = BuildMaterial(slot, target, _main.Dropdowns[slot], _main.StuffSets[slot], _main.Model, slot);
        return _material != null;
    }

    /// <summary>Opens the material ring of sub-ring option <paramref name="index"/> in place of the sub-ring.</summary>
    internal bool OpenMaterialFromSub(int index)
    {
        RingContent? sub = _sub;
        if (sub == null || sub.Designators[index] is not { } d)
        {
            return false;
        }

        _material = BuildMaterial(sub.ParentSlot, d, sub.Dropdown, StuffOptions.For(d), sub.Model, index);
        return _material != null;
    }

    /// <summary>Leaves the top ring. True when a sub-ring is still open below it (the caller resumes the satellite level).</summary>
    internal bool Back()
    {
        if (_material != null)
        {
            _material = null;
            return _sub != null;
        }

        _sub = null;
        return false;
    }

    /// <summary>What activating option <paramref name="index"/> of the active ring selects.</summary>
    internal bool TryResolve(int index, out Designator designator, out ThingDef? stuff, out Designator_Dropdown? dropdown)
    {
        RingContent? ring = Active;
        stuff = null;
        dropdown = null;
        designator = null!;
        if (ring == null || index < 0 || index >= ring.Stuffs.Length)
        {
            return false;
        }

        Designator? d = ring.IsMaterial ? ring.Target : ring.Designators[index];
        if (d == null)
        {
            return false;
        }

        designator = d;
        stuff = ring.Stuffs[index];
        dropdown = ring.Dropdown;
        return true;
    }

    /// <summary>The stuff colour shown as the material dot: the last-used stuff when the build remembers one among its options, else null.</summary>
    internal static Color? LastUsedColor(Designator target, in StuffOptionSet set)
    {
        if (set.Options.Count > 0 && target is Designator_Build build && build.StuffDefRaw is { } last && last == set.Options[0].Def)
        {
            return set.Options[0].Color;
        }

        return null;
    }

    private static Vec2 Screen => new(UI.screenWidth, UI.screenHeight);

    private RingContent? BuildSub(int slot, Designator_Dropdown dropdown)
    {
        var elements = new List<Designator>();
        foreach (Designator element in dropdown.Elements)
        {
            if (element != null)
            {
                elements.Add(element);
            }
        }

        if (SubMenuLayout.Classify(elements.Count) != SubMenuOutcome.Open)
        {
            return null;
        }

        WheelModel main = _main.Model;
        PlacedSatellite placed = SubMenuLayout.PlaceSatellite(elements.Count, main.ButtonSize, main.Slots[slot].Center, Screen, WheelModel.DefaultPlateHeight);
        var model = new WheelModel(placed.Hub, main.ButtonSize, placed.Rings, placed.Slots, placed.Slots.Length, WheelModel.DefaultPlateHeight);
        var ring = new RingContent(model, main, slot, slot, elements.Count) { Dropdown = dropdown };
        for (int i = 0; i < elements.Count; i++)
        {
            Designator d = elements[i];
            ring.Designators[i] = d;
            bool usable = MenuContent.IsUsable(d);
            StuffOptionSet set = StuffOptions.For(d);
            if (!set.IsStuffBuild)
            {
                model.SetSlot(i, d, usable ? SlotKind.Normal : SlotKind.Disabled);
                continue;
            }

            model.SetSlot(i, d, StuffSlotPolicy.KindFor(set.Outcome, usable));
            if (set.Outcome == StuffRingOutcome.Ring)
            {
                model.SetDecor(i, SlotDecor.Material, LastUsedColor(d, set));
            }
            else if (set.Outcome == StuffRingOutcome.Skip)
            {
                ring.Stuffs[i] = set.Options[0].Def;
            }
            else if (StuffSlotPolicy.IsNoMaterials(set.Outcome, usable))
            {
                ring.SetReason(i, "NoStuffsToBuildWith".Translate().Resolve());
            }
        }

        model.SetHubLabels(main.Label(slot), main.Label(slot));
        model.PrepareSatellite();
        return ring;
    }

    private RingContent? BuildMaterial(int parentSlot, Designator target, Designator_Dropdown? dropdown, in StuffOptionSet set, WheelModel hubSource, int hubIndex)
    {
        if (set.Outcome != StuffRingOutcome.Ring)
        {
            return null;
        }

        WheelModel main = _main.Model;
        int n = set.Options.Count;
        PlacedSatellite placed = SubMenuLayout.PlaceSatellite(n, main.ButtonSize, main.Slots[parentSlot].Center, Screen, WheelModel.DefaultPlateHeight);
        var model = new WheelModel(placed.Hub, main.ButtonSize, placed.Rings, placed.Slots, placed.Slots.Length, WheelModel.DefaultPlateHeight);
        var ring = new RingContent(model, hubSource, hubIndex, parentSlot, n) { IsMaterial = true, Target = target, Dropdown = dropdown };
        bool lastUsed = LastUsedColor(target, set) != null;
        for (int i = 0; i < n; i++)
        {
            StuffOption option = set.Options[i];
            ring.Stuffs[i] = option.Def;
            model.SetStuffOption(i, option.Def, SlotKind.Normal, lastUsed && i == 0, option.Label);
        }

        string parentLabel = hubSource.Label(hubIndex);
        model.SetHubLabels(parentLabel, parentLabel);
        model.PrepareSatellite();
        return ring;
    }
}
