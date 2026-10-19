using System.Collections.Generic;
using QuickMenu.Core.Input;
using QuickMenu.Core.Pins;
using QuickMenu.Core.Settings;
using QuickMenu.Diagnostics;
using Verse;

namespace QuickMenu.Settings;

/// <summary>
/// All persistent state of the mod (architecture §4, FR-17). Nothing is written to save files: no game component of any kind,
/// and no def exists. Written only by <see cref="QuickMenuMod.WriteSettings"/>, which callers
/// invoke once per pin, at the end of an edit, and when the menu closes, never per frame or per drag step.
/// </summary>
internal sealed class QuickMenuSettings : ModSettings
{
    internal int SchemaVersion = SettingsRules.SchemaVersion;
    internal string TriggerText = SettingsRules.DefaultTrigger;
    internal TriggerMode TriggerMode = TriggerMode.Toggle;
    internal MenuLayout Layout = MenuLayout.Wheel;
    internal SizingMode Sizing = SizingMode.Fixed;
    internal int Rings = SettingsRules.DefaultRings;
    internal int GridColumns = SettingsRules.DefaultColumns;
    internal int GridRows = SettingsRules.DefaultRows;
    internal float ButtonSize = SettingsRules.DefaultButtonSize;
    internal PinModifier PinModifier = PinModifier.Ctrl;
    internal bool PinModifierUserSet;
    internal bool FirstRunNoticeShown;
    internal bool MintNoticeShown;

    private List<string>? _pinLines;

    /// <summary>The pins as a live board. Opaque entries (unknown or newer lines) stay on it and are written back unchanged.</summary>
    internal PinBoard Board { get; private set; } = new(SettingsRules.PinCapacity(MenuLayout.Wheel, SizingMode.Fixed, SettingsRules.DefaultRings, SettingsRules.DefaultColumns, SettingsRules.DefaultRows));

    internal TriggerBinding Trigger
    {
        get => TriggerBinding.TryParse(TriggerText, out TriggerBinding b) ? b : TriggerBinding.Mouse(3);
        set => TriggerText = value.Serialize();
    }

    /// <summary>Recomputes the board capacity from the size settings. Pins beyond it are kept, just not shown.</summary>
    internal void ApplyCapacity() => Board.Capacity = SettingsRules.PinCapacity(Layout, Sizing, Rings, GridColumns, GridRows);

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref SchemaVersion, "schemaVersion", SettingsRules.SchemaVersion, forceSave: true);
        Scribe_Values.Look(ref TriggerText, "trigger", SettingsRules.DefaultTrigger, forceSave: true);
        Scribe_Values.Look(ref TriggerMode, "triggerMode", TriggerMode.Toggle, forceSave: true);
        Scribe_Values.Look(ref Layout, "layout", MenuLayout.Wheel, forceSave: true);
        Scribe_Values.Look(ref Sizing, "sizing", SizingMode.Fixed, forceSave: true);
        Scribe_Values.Look(ref Rings, "rings", SettingsRules.DefaultRings, forceSave: true);
        Scribe_Values.Look(ref GridColumns, "gridColumns", SettingsRules.DefaultColumns, forceSave: true);
        Scribe_Values.Look(ref GridRows, "gridRows", SettingsRules.DefaultRows, forceSave: true);
        Scribe_Values.Look(ref ButtonSize, "buttonSize", SettingsRules.DefaultButtonSize, forceSave: true);
        Scribe_Values.Look(ref PinModifier, "pinModifier", PinModifier.Ctrl, forceSave: true);
        Scribe_Values.Look(ref PinModifierUserSet, "pinModifierUserSet", false, forceSave: true);
        Scribe_Values.Look(ref FirstRunNoticeShown, "firstRunNoticeShown", false, forceSave: true);
        Scribe_Values.Look(ref MintNoticeShown, "mintNoticeShown", false, forceSave: true);

        if (Scribe.mode == LoadSaveMode.Saving)
        {
            _pinLines = PinSerializer.ToLines(Board);
        }

        Scribe_Collections.Look(ref _pinLines, "pins", LookMode.Value);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            PostLoad();
        }
    }

    private void PostLoad()
    {
        bool repaired = Normalize();
        PinLoadResult loaded = PinSerializer.FromLines(_pinLines);
        Board = loaded.Board;
        ApplyCapacity();
        if (loaded.SkippedCount > 0)
        {
            // Counts and reasons only: pin lines can contain mod names and are never logged.
            Diagnostics.Log.WarnOnce("Settings.PinLines", $"{loaded.SkippedCount} pin line(s) in the settings file could not be read and were dropped (first reason: {loaded.Skipped[0].Issue}).");
        }

        if (repaired)
        {
            Diagnostics.Log.WarnOnce("Settings.Repaired", "Some settings were missing or out of range and were reset to valid values.");
        }
    }

    /// <summary>Resets invalid values to valid ones. Returns true when anything changed.</summary>
    internal bool Normalize()
    {
        bool changed = false;
        changed |= Reset(ref SchemaVersion, SettingsRules.SchemaVersion, SchemaVersion >= 1);
        changed |= Reset(ref TriggerText, SettingsRules.DefaultTrigger, TriggerBinding.TryParse(TriggerText, out _));
        changed |= Reset(ref TriggerMode, TriggerMode.Toggle, SettingsRules.IsDefined(TriggerMode));
        changed |= Reset(ref Layout, MenuLayout.Wheel, SettingsRules.IsDefined(Layout));
        changed |= Reset(ref Sizing, SizingMode.Fixed, SettingsRules.IsDefined(Sizing));
        changed |= Reset(ref PinModifier, PinModifier.Ctrl, SettingsRules.IsDefined(PinModifier));
        changed |= Reset(ref Rings, SettingsRules.ClampRings(Rings), Rings == SettingsRules.ClampRings(Rings));
        changed |= Reset(ref GridColumns, SettingsRules.ClampColumns(GridColumns), GridColumns == SettingsRules.ClampColumns(GridColumns));
        changed |= Reset(ref GridRows, SettingsRules.ClampRows(GridRows), GridRows == SettingsRules.ClampRows(GridRows));
        changed |= Reset(ref ButtonSize, SettingsRules.ClampButtonSize(ButtonSize), ButtonSize == SettingsRules.ClampButtonSize(ButtonSize));
        return changed;
    }

    private static bool Reset<T>(ref T field, T replacement, bool valid)
    {
        if (valid)
        {
            return false;
        }

        field = replacement;
        return true;
    }
}
