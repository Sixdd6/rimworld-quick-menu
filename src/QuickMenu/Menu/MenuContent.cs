using System;
using QuickMenu.Catalog;
using QuickMenu.Core.Input;
using QuickMenu.Core.Layout;
using QuickMenu.Core.Materials;
using QuickMenu.Core.Pins;
using QuickMenu.Core.Render;
using QuickMenu.Pinning;
using RimWorld;
using Verse;
using Log = QuickMenu.Diagnostics.Log;

namespace QuickMenu.Menu;

/// <summary>
/// Everything one open of the menu shows, resolved once at open from the pin board (the cached resolution in
/// <see cref="PinService"/>): the render model, the designator behind each slot and lazily built tooltips.
/// Resolved pins fill <see cref="SlotKind.Normal"/> slots, or <see cref="SlotKind.Disabled"/> when the designator is not
/// usable right now. Dropdowns are Normal too: until T-41 activating one opens the vanilla dropdown menu. A pin whose def exists is never Empty: when the game offers no
/// designator it is Disabled (FR-14). Only unresolved (reserved, mod missing) pins stay empty in use mode.
/// </summary>
internal sealed class MenuContent
{
    private readonly string?[] _tips;
    private readonly string?[] _reasons;
    private string? _editTip;
    private string? _doneTip;

    private MenuContent(WheelModel model, Designator?[] designators, PinEntry?[] pins)
    {
        Model = model;
        Designators = designators;
        Pins = pins;
        Targets = new Designator?[designators.Length];
        Dropdowns = new Designator_Dropdown?[designators.Length];
        StuffSets = new StuffOptionSet[designators.Length];
        _tips = new string?[designators.Length];
        _reasons = new string?[designators.Length];
    }

    /// <summary>
    /// The designator a slot activates when it is not the pin itself: the only element of a one-element dropdown (FR-12,
    /// no ring for one option). Null when the pin is activated as it is.
    /// </summary>
    internal Designator?[] Targets { get; }

    /// <summary>The dropdown a slot's <see cref="Targets"/> element belongs to (so the architect remembers it); null otherwise.</summary>
    internal Designator_Dropdown?[] Dropdowns { get; }

    /// <summary>The materials of each stuff build slot, read once at open (<see cref="StuffOptionSet.IsStuffBuild"/> is false for the rest).</summary>
    internal StuffOptionSet[] StuffSets { get; }

    internal WheelModel Model { get; }

    /// <summary>Tooltip of the hub: what Edit does in use mode, how to finish in edit mode (built on first hover).</summary>
    internal string HubTip(bool editing) => editing
        ? _doneTip ??= "QuickMenu.Menu.DoneTip".Translate().Resolve()
        : _editTip ??= "QuickMenu.Menu.EditTip".Translate().Resolve();

    internal Designator?[] Designators { get; }

    /// <summary>The saved pin behind each slot (null for empty slots; reserved ones included); what modifier+click unpins.</summary>
    internal PinEntry?[] Pins { get; }

    internal static MenuContent Build(PlacedWheel placed, float buttonSize, int rings)
    {
        SlotPlacement[] slots = placed.Slots;
        var model = new WheelModel(placed.Center, buttonSize, rings, slots, slots.Length, WheelModel.DefaultPlateHeight);
        var designators = new Designator?[slots.Length];
        var pinsBySlot = new PinEntry?[slots.Length];
        var materialPending = new System.Collections.Generic.List<(int Slot, Designator Target)>();
        System.Collections.Generic.IReadOnlyList<PinSlot> pins = PinService.Board.VisibleSlots(PinService.Resolver);
        for (int i = 0; i < slots.Length && i < pins.Count; i++)
        {
            PinEntry? pin = pins[i].Entry;
            if (pin == null)
            {
                continue;
            }

            pinsBySlot[i] = pin;
            CatalogEntry? entry = pins[i].State == PinSlotState.Resolved ? PinService.Entry(pin.Key) : null;
            if (entry == null)
            {
                if (pins[i].State == PinSlotState.Unresolved)
                {
                    model.SetSlot(i, null, SlotKind.Reserved);
                }

                continue;
            }

            Designator d = entry.Designator;
            designators[i] = d;
            bool usable = !entry.ForcedUnavailable && IsUsable(d);
            model.SetSlot(i, d, usable ? SlotKind.Normal : SlotKind.Disabled, entry.Label);
            materialPending.Add((i, d));
        }

        model.SetHubLabels("QuickMenu.Menu.Edit".Translate(), "QuickMenu.Menu.Done".Translate());
        model.Prepare();
        var content = new MenuContent(model, designators, pinsBySlot);
        System.Collections.Generic.List<ThingDef>? present = materialPending.Count > 0 ? StuffOptions.Present() : null; // once per open
        foreach ((int slot, Designator d) in materialPending)
        {
            content.Classify(slot, d, present);
        }

        return content;
    }

    /// <summary>
    /// Decides what a click on pin slot <paramref name="slot"/> does (FR-10..13): a dropdown with two or more elements opens
    /// the sub-ring (badge); a one-element dropdown acts as its element; a stuff build opens the material ring (dot in the
    /// last-used stuff colour), activates directly with its only material, or is greyed when no material exists.
    /// </summary>
    private void Classify(int slot, Designator d, System.Collections.Generic.List<ThingDef>? present)
    {
        if (Model.Kinds[slot] == SlotKind.Disabled)
        {
            return;
        }

        Designator target = d;
        if (d is Designator_Dropdown dropdown)
        {
            var elements = new System.Collections.Generic.List<Designator>();
            foreach (Designator element in dropdown.Elements)
            {
                if (element != null)
                {
                    elements.Add(element);
                }
            }

            switch (SubMenuLayout.Classify(elements.Count))
            {
                case SubMenuOutcome.None:
                    Model.Kinds[slot] = SlotKind.Disabled;
                    return;
                case SubMenuOutcome.Open:
                    Model.Kinds[slot] = SlotKind.Submenu;
                    Model.SetDecor(slot, SlotDecor.SubMenu);
                    return;
                default:
                    target = elements[0];
                    Targets[slot] = target;
                    Dropdowns[slot] = dropdown;
                    break;
            }
        }

        StuffOptionSet set = StuffOptions.For(target, present);
        StuffSets[slot] = set;
        if (!set.IsStuffBuild)
        {
            return;
        }

        bool usable = Model.Kinds[slot] != SlotKind.Disabled && IsUsable(target);
        SlotKind kind = StuffSlotPolicy.KindFor(set.Outcome, usable);
        Model.Kinds[slot] = kind;
        if (set.Outcome == StuffRingOutcome.Ring && usable)
        {
            Model.SetDecor(slot, SlotDecor.Material, SatelliteController.LastUsedColor(target, set));
        }
        else if (StuffSlotPolicy.IsNoMaterials(set.Outcome, usable))
        {
            _reasons[slot] = "NoStuffsToBuildWith".Translate().Resolve();
        }
    }

    /// <summary>
    /// Edit-mode tooltip of a reserved-missing pin (design.md S4 state 8): its stored label, the mod it came from and
    /// how to remove it. Built on first hover and kept for this open. Null for other slots.
    /// </summary>
    internal string? ReservedTip(int slot)
    {
        PinEntry? pin = Pins[slot];
        if (pin == null || Model.Kinds[slot] != SlotKind.Reserved)
        {
            return null;
        }

        string? tip = _tips[slot];
        if (tip == null)
        {
            string modifierKey = QuickMenuMod.EffectiveModifier switch
            {
                PinModifier.Alt => "QuickMenu.Settings.Modifier.Alt",
                PinModifier.Shift => "QuickMenu.Settings.Modifier.Shift",
                _ => "QuickMenu.Settings.Modifier.Ctrl",
            };
            string mod = string.IsNullOrEmpty(pin.PackageId) ? "QuickMenu.Menu.UnknownMod".Translate().Resolve() : pin.PackageId!;
            tip = "QuickMenu.Menu.ReservedTip".Translate(pin.Label, mod, modifierKey.Translate()).Resolve();
            _tips[slot] = tip;
        }

        return tip.Length == 0 ? null : tip;
    }

    /// <summary>The designator's own tooltip text (label and description), built on first hover and kept for this open.</summary>
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
                    tip += "\n\n<color=#" + UnityEngine.ColorUtility.ToHtmlStringRGB(QMStyle.Danger) + ">" + Reason(slot) + "</color>";
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

    /// <summary>Why a Disabled slot cannot be used (FR-14); composed on first use and kept for this open. Null for other slots.</summary>
    internal string? Reason(int slot)
    {
        Designator? d = Designators[slot];
        if (d == null || Model.Kinds[slot] != SlotKind.Disabled)
        {
            return null;
        }

        return _reasons[slot] ??= UnavailableReasons.For(d);
    }

    internal static bool IsUsable(Designator d)
    {
        try
        {
            return d.Visible && !d.Disabled;
        }
        catch (Exception ex)
        {
            Log.LogOnce($"Menu.Usable.{d.GetType().FullName}", $"Checking whether {d.GetType().FullName} is usable failed, treating it as unavailable: {ex.GetType().Name}");
            return false;
        }
    }
}
