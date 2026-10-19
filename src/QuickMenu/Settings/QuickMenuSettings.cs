using System;
using System.Collections.Generic;
using System.Globalization;
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
    private TriggerBinding _trigger = TriggerBinding.TryParse(SettingsRules.DefaultTrigger, out TriggerBinding d) ? d : TriggerBinding.Mouse(3);
    private bool _unreadable;

    /// <summary>The pins as a live board. Opaque entries (unknown or newer lines) stay on it and are written back unchanged.</summary>
    internal PinBoard Board { get; private set; } = new(SettingsRules.PinCapacity(MenuLayout.Wheel, SizingMode.Fixed, SettingsRules.DefaultRings, SettingsRules.DefaultColumns, SettingsRules.DefaultRows));

    /// <summary>The parsed trigger, cached: set by the setter and after loading, so reading it never parses.</summary>
    internal TriggerBinding Trigger
    {
        get => _trigger;
        set
        {
            _trigger = value;
            TriggerText = value.Serialize();
        }
    }

    /// <summary>Incremented on every settings change that affects the menu. A cached layout is valid only for the revision it was built at.</summary>
    internal int Revision { get; private set; }

    /// <summary>Marks the settings changed: recomputes the board capacity and invalidates cached layouts.</summary>
    internal void NotifyChanged()
    {
        ApplyCapacity();
        Revision++;
    }

    /// <summary>Removes every pin, including hidden and unresolved ones.</summary>
    internal void ClearAllPins()
    {
        while (Board.Entries.Count > 0)
        {
            Board.SetSlot(Board.Entries.Count - 1, null);
        }
    }

    /// <summary>Restores every option to its default. Pins are not touched.</summary>
    internal void ResetOptions()
    {
        Trigger = TriggerBinding.TryParse(SettingsRules.DefaultTrigger, out TriggerBinding d) ? d : TriggerBinding.Mouse(3);
        TriggerMode = TriggerMode.Toggle;
        Layout = MenuLayout.Wheel;
        Sizing = SizingMode.Fixed;
        Rings = SettingsRules.DefaultRings;
        GridColumns = SettingsRules.DefaultColumns;
        GridRows = SettingsRules.DefaultRows;
        ButtonSize = SettingsRules.DefaultButtonSize;
        PinModifier = PinModifier.Ctrl;
        PinModifierUserSet = false;
        NotifyChanged();
    }

    /// <summary>Recomputes the board capacity from the size settings. Pins beyond it are kept, just not shown.</summary>
    internal void ApplyCapacity() => Board.Capacity = SettingsRules.PinCapacity(Layout, Sizing, Rings, GridColumns, GridRows);

    /// <summary>
    /// Numbers, enums and flags are scribed as text and parsed here, so one corrupt value is one repaired setting and
    /// one warning in total, not a red error per field from the game's own parser. The file format is the same XML
    /// elements; enums hold their names. The schema version written is always the current one.
    /// </summary>
    public override void ExposeData()
    {
        base.ExposeData();
        _unreadable = false;
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            SchemaVersion = SettingsRules.SchemaVersion;
        }

        Look(ref SchemaVersion, "schemaVersion", SettingsRules.SchemaVersion);
        Scribe_Values.Look(ref TriggerText, "trigger", SettingsRules.DefaultTrigger, forceSave: true);
        LookEnum(ref TriggerMode, "triggerMode", TriggerMode.Toggle);
        LookEnum(ref Layout, "layout", MenuLayout.Wheel);
        LookEnum(ref Sizing, "sizing", SizingMode.Fixed);
        Look(ref Rings, "rings", SettingsRules.DefaultRings);
        Look(ref GridColumns, "gridColumns", SettingsRules.DefaultColumns);
        Look(ref GridRows, "gridRows", SettingsRules.DefaultRows);
        Look(ref ButtonSize, "buttonSize", (float)SettingsRules.DefaultButtonSize);
        LookEnum(ref PinModifier, "pinModifier", PinModifier.Ctrl);
        Look(ref PinModifierUserSet, "pinModifierUserSet", false);
        Look(ref FirstRunNoticeShown, "firstRunNoticeShown", false);
        Look(ref MintNoticeShown, "mintNoticeShown", false);

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
        bool repaired = Normalize() | _unreadable;
        _trigger = TriggerBinding.TryParse(TriggerText, out TriggerBinding parsed) ? parsed : TriggerBinding.Mouse(3);
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

    private void Look(ref int value, string label, int fallback) =>
        LookText(ref value, label, fallback, static v => v.ToString(CultureInfo.InvariantCulture), static (string t, out int v) => int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out v));

    private void Look(ref float value, string label, float fallback) =>
        LookText(ref value, label, fallback, static v => v.ToString("R", CultureInfo.InvariantCulture), static (string t, out float v) => float.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out v));

    private void Look(ref bool value, string label, bool fallback) =>
        LookText(ref value, label, fallback, static v => v ? "true" : "false", static (string t, out bool v) => bool.TryParse(t, out v));

    private void LookEnum<T>(ref T value, string label, T fallback)
        where T : struct, Enum =>
        LookText(ref value, label, fallback, static v => v.ToString(), static (string t, out T v) => Enum.TryParse(t, ignoreCase: false, out v) && SettingsRules.IsDefined(v));

    private delegate bool TryParseText<T>(string text, out T value);

    // Called a few times per settings load or save, never per frame: the delegates are static and do not capture.
    private void LookText<T>(ref T value, string label, T fallback, Func<T, string> format, TryParseText<T> tryParse)
    {
        string? text = Scribe.mode == LoadSaveMode.Saving ? format(value) : null;
        Scribe_Values.Look(ref text, label, format(fallback), forceSave: true);
        if (Scribe.mode != LoadSaveMode.LoadingVars)
        {
            return;
        }

        if (text != null && tryParse(text.Trim(), out T parsed))
        {
            value = parsed;
            return;
        }

        value = fallback;
        _unreadable = true;
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
