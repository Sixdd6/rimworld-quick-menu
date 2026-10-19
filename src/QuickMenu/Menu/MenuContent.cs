using System;
using QuickMenu.Catalog;
using QuickMenu.Core.Input;
using QuickMenu.Core.Layout;
using QuickMenu.Core.Pins;
using QuickMenu.Pinning;
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

    private MenuContent(WheelModel model, Designator?[] designators)
    {
        Model = model;
        Designators = designators;
        _tips = new string?[designators.Length];
        _reasons = new string?[designators.Length];
    }

    internal WheelModel Model { get; }

    internal Designator?[] Designators { get; }

    internal static MenuContent Build(PlacedWheel placed, float buttonSize, int rings)
    {
        SlotPlacement[] slots = placed.Slots;
        var model = new WheelModel(placed.Center, buttonSize, rings, slots, slots.Length, hasHubSub: false, WheelModel.DefaultPlateHeight);
        var designators = new Designator?[slots.Length];
        System.Collections.Generic.IReadOnlyList<PinSlot> pins = PinService.Board.VisibleSlots(PinService.Resolver);
        for (int i = 0; i < slots.Length && i < pins.Count; i++)
        {
            PinEntry? pin = pins[i].State == PinSlotState.Resolved ? pins[i].Entry : null;
            CatalogEntry? entry = pin == null ? null : PinService.Entry(pin.Key);
            if (entry == null)
            {
                continue;
            }

            Designator d = entry.Designator;
            designators[i] = d;
            model.SetSlot(i, d, !entry.ForcedUnavailable && IsUsable(d) ? SlotKind.Normal : SlotKind.Disabled, entry.Label);
        }

        model.SetHubLabels("QuickMenu.Menu.Close".Translate(), "QuickMenu.Menu.Edit".Translate());
        model.Prepare();
        return new MenuContent(model, designators);
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

    private static bool IsUsable(Designator d)
    {
        try
        {
            return d.Visible && !d.Disabled;
        }
        catch (Exception ex)
        {
            Log.LogOnce($"Menu.Usable.{d.GetType().FullName}", $"Checking whether {d.GetType().FullName} is usable failed, treating it as usable: {ex.GetType().Name}");
            return true;
        }
    }
}
