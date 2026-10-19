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
        Sets = new StuffOptionSet[count];
        Count = count;
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

    /// <summary>Number of options in the ring.</summary>
    internal int Count { get; }

    /// <summary>Sub-ring: the materials of each option, kept so opening its material ring does not recompute them.</summary>
    internal StuffOptionSet[] Sets { get; }

    /// <summary>Sub-ring: the designator behind each option. Material ring: unused.</summary>
    internal Designator?[] Designators { get; }

    /// <summary>
    /// Material ring: the stuff of each option. Sub-ring: the only stuff of an option whose ring was skipped (one material),
    /// otherwise null.
    /// </summary>
    internal ThingDef?[] Stuffs { get; }

    /// <summary>
    /// Grid layout only: the centre of the parent cell in the main grid. A sub-grid or material grid has no hub, so a click on
    /// the open parent cell counts as the hub (it goes back, like the hub of a ring).
    /// </summary>
    internal Vec2? ParentCenter { get; init; }

    /// <summary>The hit at <paramref name="point"/> on this satellite (a ring's hub is its centre; a grid's is the parent cell).</summary>
    internal Hit HitTest(Vec2 point)
    {
        Hit hit = Model.HitTest(point);
        return ParentCenter is { } parent && hit.Kind == HitKind.Outside && HitTester.InCell(point, parent, Model.ButtonSize) ? Hit.HubEdit : hit;
    }

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

        if (_main.Targets[slot] is { } target)
        {
            _material = BuildMaterial(slot, target, _main.Dropdowns[slot], _main.StuffSets[slot], _main.Model, slot);
            return _material != null;
        }

        if (d is Designator_Dropdown dropdown)
        {
            _sub = BuildSub(slot, dropdown);
            return _sub != null;
        }

        _material = BuildMaterial(slot, d, _main.Dropdowns[slot], _main.StuffSets[slot], _main.Model, slot);
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

        _material = BuildMaterial(sub.ParentSlot, d, sub.Dropdown, sub.Sets[index], sub.Model, index);
        return _material != null;
    }

    /// <summary>Leaves the top ring. True when a sub-ring is still open below it (the caller resumes the satellite level).</summary>
    internal bool Back()
    {
        if (_material != null)
        {
            _material = null;
            _sub?.Model.RefreshSatelliteRim();
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
        if (ring == null || index < 0 || index >= ring.Count)
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
        int last = set.LastUsedIndex;
        return last >= 0 && last < set.Options.Count ? set.Options[last].Color : null;
    }

    private static Vec2 Screen => new(UI.screenWidth, UI.screenHeight);

    private RingContent? BuildSub(int slot, Designator_Dropdown dropdown)
    {
        List<Designator> elements = MenuContent.VisibleElements(dropdown);
        if (SubMenuLayout.Classify(elements.Count) != SubMenuOutcome.Open)
        {
            return null;
        }

        WheelModel main = _main.Model;
        Vec2 parent = main.Slots[slot].Center;
        WheelModel model = PlaceModel(main, elements.Count, parent);
        var ring = new RingContent(model, main, slot, slot, elements.Count) { Dropdown = dropdown, ParentCenter = main.Panel != null ? parent : null };
        List<ThingDef> present = StuffOptions.Present();
        ThingDef?[]? materials = MaterialVariantIcons.For(elements);
        for (int i = 0; i < elements.Count; i++)
        {
            Designator d = elements[i];
            try
            {
                BuildSubOption(ring, model, i, d, present, materials);
            }
            catch (Exception ex)
            {
                Type type = d.GetType();
                Log.LogOnce("Menu.Classify." + type.FullName, $"Building sub-ring option {type.FullName} failed; it is greyed: {ex}");
                ring.Designators[i] = d;
                model.Kinds[i] = SlotKind.Disabled;
            }
        }

        model.SetHubLabels(main.Label(slot), main.Label(slot));
        Prepare(model);
        return ring;
    }

    private static void BuildSubOption(RingContent ring, WheelModel model, int i, Designator d, List<ThingDef> present, ThingDef?[]? materials)
    {
        ring.Designators[i] = d;
        bool usable = MenuContent.IsUsable(d);
        StuffOptionSet set = StuffOptions.For(d, present);
        ring.Sets[i] = set;
        if (!set.IsStuffBuild)
        {
            model.SetSlot(i, d, usable ? SlotKind.Normal : SlotKind.Disabled);
            if (materials?[i] is { } material)
            {
                model.SetMaterialIcon(i, material);
            }

            return;
        }

        model.SetSlot(i, d, StuffSlotPolicy.KindFor(set.Outcome, usable));
        if (model.Kinds[i] == SlotKind.Disabled)
        {
            if (StuffSlotPolicy.IsNoMaterials(set.Outcome, usable))
            {
                ring.SetReason(i, "NoStuffsToBuildWith".Translate().Resolve());
            }
        }
        else if (set.Outcome == StuffRingOutcome.Ring)
        {
            model.SetDecor(i, SlotDecor.Material, LastUsedColor(d, set));
        }
        else if (set.Outcome == StuffRingOutcome.Skip)
        {
            ring.Stuffs[i] = set.Options[0].Def;
        }
    }

    private RingContent? BuildMaterial(int parentSlot, Designator target, Designator_Dropdown? dropdown, in StuffOptionSet set, WheelModel hubSource, int hubIndex)
    {
        if (set.Outcome != StuffRingOutcome.Ring)
        {
            return null;
        }

        WheelModel main = _main.Model;
        int n = set.Options.Count;
        Vec2 parent = main.Slots[parentSlot].Center;
        WheelModel model = PlaceModel(main, n, parent);
        var ring = new RingContent(model, hubSource, hubIndex, parentSlot, n) { IsMaterial = true, Target = target, Dropdown = dropdown, ParentCenter = main.Panel != null ? parent : null };
        for (int i = 0; i < n; i++)
        {
            StuffOption option = set.Options[i];
            ring.Stuffs[i] = option.Def;
            model.SetStuffOption(i, option.Def, SlotKind.Normal, i == set.LastUsedIndex, option.Label);
        }

        string parentLabel = hubSource.Label(hubIndex);
        model.SetHubLabels(parentLabel, parentLabel);
        Prepare(model);
        return ring;
    }

    /// <summary>
    /// Places a satellite of <paramref name="count"/> options for the parent cell at <paramref name="parent"/>: a ring around
    /// the parent for a wheel menu, a sub-grid beside the main panel for a grid menu (edge-dodged either way).
    /// </summary>
    private static WheelModel PlaceModel(WheelModel main, int count, Vec2 parent)
    {
        float plateHeight = WheelModel.DefaultPlateHeight;
        if (main.Panel is not { } mainPanel)
        {
            PlacedSatellite placed = SubMenuLayout.PlaceSatellite(count, main.ButtonSize, parent, Screen, plateHeight);
            return new WheelModel(placed.Hub, main.ButtonSize, placed.Rings, placed.Slots, placed.Slots.Length, plateHeight);
        }

        float plateWidth = SubMenuLayout.SubGridWidth(count, main.ButtonSize);
        PlacedSubGrid grid = SubMenuLayout.PlaceSubGrid(count, main.ButtonSize, mainPanel, main.Center.X, parent, Screen, plateWidth, plateHeight);
        var center = new Vec2(grid.Panel.X + (grid.Panel.Width / 2f), grid.Panel.Y + (grid.Panel.Height / 2f));
        var model = new WheelModel(center, main.ButtonSize, 1, grid.Cells, grid.Cells.Length, plateHeight);
        model.SetPanel(grid.Panel);
        return model;
    }

    /// <summary>Labels (and the satellite rim of a ring) once when the satellite opens; a sub-grid shares the main textures.</summary>
    private static void Prepare(WheelModel model)
    {
        if (model.Panel != null)
        {
            model.Prepare(ensureTextures: false);
        }
        else
        {
            model.PrepareSatellite();
        }
    }
}
