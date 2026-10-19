using System;
using System.Collections.Generic;
using System.Globalization;
using QuickMenu.Core.Input;
using QuickMenu.Core.Layout;
using QuickMenu.Core.Settings;
using QuickMenu.Menu;
using QuickMenu.Pinning;
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
    private static string? _columns;
    private static string? _rows;
    private static string? _shape;
    private static string? _shapeWheel;
    private static string? _shapeGrid;
    private static string? _size;
    private static string? _sizeFixed;
    private static string? _sizeAuto;
    private static string? _sizeFixedTip;
    private static string? _sizeAutoTip;
    private static string? _forgetMissing;
    private static string? _forgetConfirm;
    private static string? _missingUnknown;
    private static string? _buttonSize;
    private static string? _reset;
    private static string? _resetTip;

    private static string? _modCtrl;
    private static string? _modAlt;
    private static string? _modShift;

    private static int _ringsFor = int.MinValue;
    private static StepKind _stepKindFor = (StepKind)(-1);
    private static string _ringsText = string.Empty;
    private static float _sizeFor = float.NaN;
    private static string _sizeText = string.Empty;

    private static int _missingCount;
    private static int _missingShown = -1;
    private static int _missingCheckedFrame = -1000;
    private static string _missingText = string.Empty;

    private static int _slotsFor = -1;
    private static string _slotsText = string.Empty;

    /// <summary>Draws the page in the settings window's content rect.</summary>
    internal static void Draw(Rect inRect)
    {
        try
        {
            DrawPage(inRect);
        }
        catch (Exception ex)
        {
            Diagnostics.Log.LogOnce("Settings.Page", $"The settings page failed to draw: {ex.GetType().Name}");
            Text.Font = GameFont.Small;
            Widgets.Label(inRect, "QuickMenu.Settings.PageError".Translate());
        }
    }

    private static void DrawPage(Rect inRect)
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

        DrawMissingPins(listing);
        listing.End();

        listing = new Listing_Standard();
        listing.Begin(right);
        Heading(listing, _headLayout!);
        DrawShape(listing, settings);
        DrawSizing(listing, settings);
        DrawSizeSteppers(listing, settings);
        listing.Gap(4f);
        DrawButtonSize(listing, settings);
        listing.Gap(6f);
        float used = listing.CurHeight;
        listing.End();
        LayoutPreview.Draw(new Rect(right.x, right.y + used, right.width, right.height - used), settings);

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
        _columns = "QuickMenu.Settings.Columns".Translate();
        _rows = "QuickMenu.Settings.Rows".Translate();
        _shape = "QuickMenu.Settings.Shape".Translate();
        _shapeWheel = "QuickMenu.Settings.ShapeWheel".Translate();
        _shapeGrid = "QuickMenu.Settings.ShapeGrid".Translate();
        _size = "QuickMenu.Settings.Size".Translate();
        _sizeFixed = "QuickMenu.Settings.SizeFixed".Translate();
        _sizeAuto = "QuickMenu.Settings.SizeAuto".Translate();
        _sizeFixedTip = "QuickMenu.Settings.SizeFixedTip".Translate();
        _sizeAutoTip = "QuickMenu.Settings.SizeAutoTip".Translate();
        _forgetMissing = "QuickMenu.Settings.ForgetMissing".Translate();
        _forgetConfirm = "QuickMenu.Settings.ForgetMissingConfirm".Translate();
        _missingUnknown = "QuickMenu.Settings.MissingUnknown".Translate();
        _buttonSize = "QuickMenu.Settings.ButtonSize".Translate();
        _modCtrl = "QuickMenu.Settings.Modifier.Ctrl".Translate();
        _modAlt = "QuickMenu.Settings.Modifier.Alt".Translate();
        _modShift = "QuickMenu.Settings.Modifier.Shift".Translate();
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

        if (!settings.PinModifierUserSet && QuickMenuMod.MintActive)
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
        PinModifier.Alt => _modAlt!,
        PinModifier.Shift => _modShift!,
        _ => _modCtrl!,
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
        PinService.ClearAll();
        QuickMenuMod.Settings.NotifyChanged();
        QuickMenuMod.SaveSettings();
    }

    private const float StepLabelWidth = 90f;

    private enum StepKind
    {
        Rings,
        Columns,
        Rows,
    }

    private static void DrawShape(Listing_Standard listing, QuickMenuSettings settings)
    {
        Rect row = listing.GetRect(RowHeight);
        Widgets.Label(new Rect(row.x, row.y, StepLabelWidth, row.height), _shape);
        var wheel = new Rect(row.x + StepLabelWidth, row.y, 90f, row.height);
        var grid = new Rect(wheel.xMax + 4f, row.y, 90f, row.height);
        MenuLayout layout = settings.Layout;
        if (DrawSegment(wheel, _shapeWheel!, layout == MenuLayout.Wheel))
        {
            layout = MenuLayout.Wheel;
        }

        if (DrawSegment(grid, _shapeGrid!, layout == MenuLayout.Grid))
        {
            layout = MenuLayout.Grid;
        }

        if (layout != settings.Layout)
        {
            settings.Layout = layout;
            settings.NotifyChanged();
        }
    }

    private static void DrawSizing(Listing_Standard listing, QuickMenuSettings settings)
    {
        Rect row = listing.GetRect(RowHeight);
        Widgets.Label(new Rect(row.x, row.y, StepLabelWidth, row.height), _size);
        var fixedRect = new Rect(row.x + StepLabelWidth, row.y, 90f, row.height);
        var autoRect = new Rect(fixedRect.xMax + 4f, row.y, 90f, row.height);
        SizingMode sizing = settings.Sizing;
        if (DrawSegment(fixedRect, _sizeFixed!, sizing == SizingMode.Fixed))
        {
            sizing = SizingMode.Fixed;
        }

        if (DrawSegment(autoRect, _sizeAuto!, sizing == SizingMode.Auto))
        {
            sizing = SizingMode.Auto;
        }

        TooltipHandler.TipRegion(fixedRect, _sizeFixedTip);
        TooltipHandler.TipRegion(autoRect, _sizeAutoTip);
        if (sizing != settings.Sizing)
        {
            settings.Sizing = sizing;
            settings.NotifyChanged();
        }
    }

    /// <summary>One segment of a segmented control; the selected one is highlighted (accent marks only the selection). True when an unselected segment was clicked.</summary>
    private static bool DrawSegment(Rect rect, string label, bool selected)
    {
        bool clicked = Widgets.ButtonText(rect, label) && !selected;
        if (selected)
        {
            Widgets.DrawHighlightSelected(rect);
        }

        return clicked;
    }

    private static void DrawSizeSteppers(Listing_Standard listing, QuickMenuSettings settings)
    {
        int rings = settings.Rings;
        int columns = settings.GridColumns;
        int rows = settings.GridRows;
        if (SettingsPreview.ShowsRings(settings.Layout, settings.Sizing))
        {
            rings = DrawStepper(listing.GetRect(RowHeight), _rings!, rings, StepKind.Rings);
        }

        if (SettingsPreview.ShowsColumns(settings.Layout, settings.Sizing))
        {
            columns = DrawStepper(listing.GetRect(RowHeight), _columns!, columns, StepKind.Columns);
        }

        if (SettingsPreview.ShowsRows(settings.Layout, settings.Sizing))
        {
            rows = DrawStepper(listing.GetRect(RowHeight), _rows!, rows, StepKind.Rows);
        }

        if (rings != settings.Rings || columns != settings.GridColumns || rows != settings.GridRows)
        {
            settings.Rings = rings;
            settings.GridColumns = columns;
            settings.GridRows = rows;
            settings.NotifyChanged();
        }

        if (settings.Sizing == SizingMode.Fixed)
        {
            int slots = SettingsRules.PinCapacity(settings.Layout, settings.Sizing, settings.Rings, settings.GridColumns, settings.GridRows);
            if (slots != _slotsFor)
            {
                _slotsFor = slots;
                _slotsText = "QuickMenu.Settings.Slots".Translate(slots.ToString(CultureInfo.CurrentCulture));
            }

            Text.Font = GameFont.Tiny;
            GUI.color = QMStyle.TextMuted;
            listing.Label(_slotsText);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }
    }

    /// <summary>Label, [-], value, [+]. Returns the new value (stepped and clamped by <see cref="SettingsPreview"/>).</summary>
    private static int DrawStepper(Rect row, string label, int value, StepKind kind)
    {
        Widgets.Label(new Rect(row.x, row.y, StepLabelWidth, row.height), label);
        float x = row.x + StepLabelWidth;
        int result = value;
        if (Widgets.ButtonText(new Rect(x, row.y, 30f, row.height), "-"))
        {
            result = Step(kind, value, -1);
        }

        if (value != _ringsFor || kind != _stepKindFor)
        {
            _ringsFor = value;
            _stepKindFor = kind;
            _ringsText = value.ToString(CultureInfo.CurrentCulture);
        }

        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(new Rect(x + 30f, row.y, 30f, row.height), _ringsText);
        Text.Anchor = TextAnchor.UpperLeft;
        if (Widgets.ButtonText(new Rect(x + 60f, row.y, 30f, row.height), "+"))
        {
            result = Step(kind, value, 1);
        }

        return result;
    }

    private static int Step(StepKind kind, int value, int delta) => kind switch
    {
        StepKind.Rings => SettingsPreview.StepRings(value, delta),
        StepKind.Columns => SettingsPreview.StepColumns(value, delta),
        _ => SettingsPreview.StepRows(value, delta),
    };

    private static void DrawMissingPins(Listing_Standard listing)
    {
        // The count is re-checked about twice a second: it walks the pins and looks defs up.
        int frame = Time.frameCount;
        if (frame - _missingCheckedFrame > 30 || frame < _missingCheckedFrame)
        {
            _missingCheckedFrame = frame;
            _missingCount = PinService.IsReady ? PinService.CountMissing() : 0;
        }

        bool ready = PinService.IsReady;
        if (ready && _missingCount == 0)
        {
            return;
        }

        listing.Gap(6f);
        Text.Font = GameFont.Tiny;
        GUI.color = QMStyle.TextMuted;
        if (!ready)
        {
            listing.Label(_missingUnknown);
        }
        else
        {
            if (_missingCount != _missingShown)
            {
                _missingShown = _missingCount;
                _missingText = "QuickMenu.Settings.MissingPins".Translate(_missingCount.ToString(CultureInfo.CurrentCulture));
            }

            listing.Label(_missingText);
        }

        GUI.color = Color.white;
        Text.Font = GameFont.Small;
        if (Widgets.ButtonText(listing.GetRect(RowHeight).LeftPartPixels(200f), _forgetMissing, active: ready && _missingCount > 0)
            && ready && _missingCount > 0)
        {
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(_forgetConfirm, ForgetMissingPins, destructive: true));
        }
    }

    private static void ForgetMissingPins()
    {
        PinService.ForgetMissing();
        QuickMenuMod.Settings.NotifyChanged();
        _missingCheckedFrame = -1000;
    }

    private static void DrawButtonSize(Listing_Standard listing, QuickMenuSettings settings)
    {
        Rect row = listing.GetRect(RowHeight);
        if (settings.ButtonSize != _sizeFor)
        {
            _sizeFor = settings.ButtonSize;
            _sizeText = $"{_buttonSize} {settings.ButtonSize.ToString("0", CultureInfo.CurrentCulture)}";
        }

        Widgets.Label(new Rect(row.x, row.y, 110f, row.height), _sizeText);
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
