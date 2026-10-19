using System.Collections.Generic;
using System.Globalization;
using QuickMenu.Core.Input;
using QuickMenu.Core.Layout;
using QuickMenu.Core.Settings;
using QuickMenu.Menu;
using UnityEngine;
using Verse;

namespace QuickMenu.Settings;

/// <summary>
/// The mod settings window (design.md S7), v1: binding row, pin modifier, clear pins, rings and button size.
/// Changes are applied at once (capacity, effective modifier, layout revision); the file is written by the game when the window closes.
/// </summary>
internal static class SettingsPage
{
    private const float RowHeight = 30f;
    private const float ColumnGap = 40f;

    private static string? _headOpening;
    private static string? _headPinning;
    private static string? _headLayout;
    private static string? _pinWith;
    private static string? _pinSuffix;
    private static string? _mintActive;
    private static string? _clearAll;
    private static string? _clearConfirm;
    private static string? _rings;
    private static string? _buttonSize;
    private static string? _reset;
    private static string? _resetTip;

    private static int _slotsFor = -1;
    private static string _slotsText = string.Empty;

    /// <summary>Draws the page in the settings window's content rect.</summary>
    internal static void Draw(Rect inRect)
    {
        EnsureStrings();
        QuickMenuSettings settings = QuickMenuMod.Settings;
        float columnWidth = (inRect.width - ColumnGap) / 2f;
        var left = new Rect(inRect.x, inRect.y, columnWidth, inRect.height - RowHeight - 12f);
        var right = new Rect(inRect.x + columnWidth + ColumnGap, inRect.y, columnWidth, left.height);

        var listing = new Listing_Standard();
        listing.Begin(left);
        Heading(listing, _headOpening!);
        BindingRow.Draw(listing);
        listing.Gap(18f);
        Heading(listing, _headPinning!);
        DrawPinModifier(listing, settings);
        listing.Gap(6f);
        if (Widgets.ButtonText(listing.GetRect(RowHeight).LeftPartPixels(200f), _clearAll))
        {
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(_clearConfirm, ClearAllPins, destructive: true));
        }

        listing.End();

        listing = new Listing_Standard();
        listing.Begin(right);
        Heading(listing, _headLayout!);
        DrawRings(listing, settings);
        listing.Gap(6f);
        DrawButtonSize(listing, settings);
        listing.End();

        var resetRect = new Rect(inRect.x, inRect.yMax - RowHeight, 200f, RowHeight);
        if (Widgets.ButtonText(resetRect, _reset))
        {
            settings.ResetOptions();
            QuickMenuMod.RefreshEffectiveModifier();
            QuickMenuMod.SaveSettings();
        }

        TooltipHandler.TipRegion(resetRect, _resetTip);
    }

    private static void EnsureStrings()
    {
        if (_headOpening != null)
        {
            return;
        }

        _headOpening = "QuickMenu.Settings.HeadOpening".Translate();
        _headPinning = "QuickMenu.Settings.HeadPinning".Translate();
        _headLayout = "QuickMenu.Settings.HeadLayout".Translate();
        _pinWith = "QuickMenu.Settings.PinWith".Translate();
        _pinSuffix = "QuickMenu.Settings.PinWithSuffix".Translate();
        _mintActive = "QuickMenu.Settings.MintActive".Translate();
        _clearAll = "QuickMenu.Settings.ClearAllPins".Translate();
        _clearConfirm = "QuickMenu.Settings.ClearAllPinsConfirm".Translate();
        _rings = "QuickMenu.Settings.Rings".Translate();
        _buttonSize = "QuickMenu.Settings.ButtonSize".Translate();
        _reset = "QuickMenu.Settings.ResetDefaults".Translate();
        _resetTip = "QuickMenu.Settings.ResetDefaultsTip".Translate();
    }

    private static void Heading(Listing_Standard listing, string text)
    {
        Text.Font = GameFont.Medium;
        listing.Label(text);
        Text.Font = GameFont.Small;
    }

    private static void DrawPinModifier(Listing_Standard listing, QuickMenuSettings settings)
    {
        Rect row = listing.GetRect(RowHeight);
        Widgets.Label(new Rect(row.x, row.y, 90f, row.height), _pinWith);
        var button = new Rect(row.x + 90f, row.y, 90f, row.height);
        if (Widgets.ButtonText(button, ModifierLabel(QuickMenuMod.EffectiveModifier)))
        {
            Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
            {
                ModifierOption(PinModifier.Ctrl),
                ModifierOption(PinModifier.Alt),
                ModifierOption(PinModifier.Shift),
            }));
        }

        Widgets.Label(new Rect(button.xMax + 8f, row.y, row.width - button.xMax - 8f, row.height), _pinSuffix);

        if (!settings.PinModifierUserSet && QuickMenuMod.EffectiveModifier != settings.PinModifier)
        {
            Text.Font = GameFont.Tiny;
            GUI.color = QMStyle.TextMuted;
            listing.Label(_mintActive);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }
    }

    private static string ModifierLabel(PinModifier modifier) => modifier switch
    {
        PinModifier.Alt => "QuickMenu.Settings.Modifier.Alt".Translate(),
        PinModifier.Shift => "QuickMenu.Settings.Modifier.Shift".Translate(),
        _ => "QuickMenu.Settings.Modifier.Ctrl".Translate(),
    };

    private static FloatMenuOption ModifierOption(PinModifier modifier) => new(ModifierLabel(modifier), () =>
    {
        QuickMenuSettings settings = QuickMenuMod.Settings;
        settings.PinModifier = modifier;
        settings.PinModifierUserSet = true;
        QuickMenuMod.RefreshEffectiveModifier();
        settings.NotifyChanged();
    });

    private static void ClearAllPins()
    {
        QuickMenuMod.Settings.ClearAllPins();
        QuickMenuMod.Settings.NotifyChanged();
        QuickMenuMod.SaveSettings();
    }

    private static void DrawRings(Listing_Standard listing, QuickMenuSettings settings)
    {
        Rect row = listing.GetRect(RowHeight);
        Widgets.Label(new Rect(row.x, row.y, 90f, row.height), _rings);
        float x = row.x + 90f;
        int rings = settings.Rings;
        if (Widgets.ButtonText(new Rect(x, row.y, 30f, row.height), "-") && rings > LayoutConstants.MinRings)
        {
            rings--;
        }

        Widgets.Label(new Rect(x + 30f, row.y, 30f, row.height), rings.ToString(CultureInfo.CurrentCulture));
        if (Widgets.ButtonText(new Rect(x + 60f, row.y, 30f, row.height), "+") && rings < LayoutConstants.MaxRings)
        {
            rings++;
        }

        int slots = SettingsRules.PinCapacity(settings.Layout, settings.Sizing, rings, settings.GridColumns, settings.GridRows);
        if (slots != _slotsFor)
        {
            _slotsFor = slots;
            _slotsText = "QuickMenu.Settings.Slots".Translate(slots.ToString(CultureInfo.CurrentCulture));
        }

        Widgets.Label(new Rect(x + 100f, row.y, row.width - x - 100f, row.height), _slotsText);
        if (rings != settings.Rings)
        {
            settings.Rings = SettingsRules.ClampRings(rings);
            settings.NotifyChanged();
        }
    }

    private static void DrawButtonSize(Listing_Standard listing, QuickMenuSettings settings)
    {
        Rect row = listing.GetRect(RowHeight);
        Widgets.Label(new Rect(row.x, row.y, 110f, row.height), $"{_buttonSize} {settings.ButtonSize.ToString("0", CultureInfo.CurrentCulture)}");
        float value = Widgets.HorizontalSlider(
            new Rect(row.x + 120f, row.y + 6f, row.width - 120f, row.height - 6f),
            settings.ButtonSize,
            LayoutConstants.MinButtonSize,
            LayoutConstants.MaxButtonSize,
            roundTo: LayoutConstants.ButtonSizeStep);
        float snapped = SettingsRules.ClampButtonSize(value);
        if (snapped != settings.ButtonSize)
        {
            settings.ButtonSize = snapped;
            settings.NotifyChanged();
        }
    }
}
