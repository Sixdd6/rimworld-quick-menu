using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    private string _triggerText = SettingsRules.DefaultTrigger;
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
            _triggerText = value.Serialize();
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

    /// <summary>Ticks spent inside the settings scribe pass over all load phases (our parsing plus the game's Scribe calls), for P-16 attribution.</summary>
    internal static long ScribeTicks { get; private set; }

    /// <summary>Ticks spent in <see cref="PostLoad"/>: purely our work (normalize, trigger parse, pin lines, capacity).</summary>
    internal static long PostLoadTicks { get; private set; }

    /// <summary>
    /// Numbers, enums and flags are scribed as text and parsed here, so one corrupt value is one repaired setting and
    /// one warning in total, not a red error per field from the game's own parser. The file format is the same XML
    /// elements; enums hold their names. The schema version written is always the current one.
    /// </summary>
    public override void ExposeData()
    {
        long start = Stopwatch.GetTimestamp();
        base.ExposeData();
        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            _unreadable = false;
        }

        if (Scribe.mode == LoadSaveMode.Saving)
        {
            SchemaVersion = SettingsRules.SchemaVersion;
        }

        SchemaVersion = LookInt(SchemaVersion, "schemaVersion", SettingsRules.SchemaVersion);
        Scribe_Values.Look(ref _triggerText, "trigger", SettingsRules.DefaultTrigger, forceSave: true);
        TriggerMode = (TriggerMode)LookEnum(typeof(TriggerMode), TriggerMode, "triggerMode", TriggerMode.Toggle);
        Layout = (MenuLayout)LookEnum(typeof(MenuLayout), Layout, "layout", MenuLayout.Wheel);
        Sizing = (SizingMode)LookEnum(typeof(SizingMode), Sizing, "sizing", SizingMode.Fixed);
        Rings = LookInt(Rings, "rings", SettingsRules.DefaultRings);
        GridColumns = LookInt(GridColumns, "gridColumns", SettingsRules.DefaultColumns);
        GridRows = LookInt(GridRows, "gridRows", SettingsRules.DefaultRows);
        ButtonSize = LookFloat(ButtonSize, "buttonSize", SettingsRules.DefaultButtonSize);
        PinModifier = (PinModifier)LookEnum(typeof(PinModifier), PinModifier, "pinModifier", PinModifier.Ctrl);
        PinModifierUserSet = LookBool(PinModifierUserSet, "pinModifierUserSet");
        FirstRunNoticeShown = LookBool(FirstRunNoticeShown, "firstRunNoticeShown");
        MintNoticeShown = LookBool(MintNoticeShown, "mintNoticeShown");

        if (Scribe.mode == LoadSaveMode.Saving)
        {
            _pinLines = PinSerializer.ToLines(Board);
        }

        Scribe_Collections.Look(ref _pinLines, "pins", LookMode.Value);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            long postStart = Stopwatch.GetTimestamp();
            PostLoad();
            PostLoadTicks += Stopwatch.GetTimestamp() - postStart;
        }

        ScribeTicks += Stopwatch.GetTimestamp() - start;
    }

    private void PostLoad()
    {
        bool repaired = Normalize() | _unreadable;
        _trigger = TriggerBinding.TryParse(_triggerText, out TriggerBinding parsed) ? parsed : TriggerBinding.Mouse(3);
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
    /// <remarks>Plain statements, no generic helper: each value-type instantiation of a generic would be JIT-compiled separately at startup (P-16).</remarks>
    internal bool Normalize()
    {
        bool changed = false;
        if (SchemaVersion < 1)
        {
            SchemaVersion = SettingsRules.SchemaVersion;
            changed = true;
        }

        if (!TriggerBinding.TryParse(_triggerText, out _))
        {
            _triggerText = SettingsRules.DefaultTrigger;
            changed = true;
        }

        if (!IsDefined(typeof(TriggerMode), TriggerMode))
        {
            TriggerMode = TriggerMode.Toggle;
            changed = true;
        }

        if (!IsDefined(typeof(MenuLayout), Layout))
        {
            Layout = MenuLayout.Wheel;
            changed = true;
        }

        if (!IsDefined(typeof(SizingMode), Sizing))
        {
            Sizing = SizingMode.Fixed;
            changed = true;
        }

        if (!IsDefined(typeof(PinModifier), PinModifier))
        {
            PinModifier = PinModifier.Ctrl;
            changed = true;
        }

        int rings = SettingsRules.ClampRings(Rings);
        int columns = SettingsRules.ClampColumns(GridColumns);
        int rows = SettingsRules.ClampRows(GridRows);
        float size = SettingsRules.ClampButtonSize(ButtonSize);
        changed |= Rings != rings || GridColumns != columns || GridRows != rows || ButtonSize != size;
        Rings = rings;
        GridColumns = columns;
        GridRows = rows;
        ButtonSize = size;
        return changed;
    }

    private static bool IsDefined(Type enumType, object value) => Enum.IsDefined(enumType, value);

    // The Look helpers are non-generic on purpose: one compiled body each instead of one JIT compilation per value type (P-16).
    private int LookInt(int value, string label, int fallback)
    {
        string? text = LookText(label, Scribe.mode == LoadSaveMode.Saving ? value.ToString(CultureInfo.InvariantCulture) : null, fallback.ToString(CultureInfo.InvariantCulture));
        if (Scribe.mode != LoadSaveMode.LoadingVars)
        {
            return value;
        }

        if (text != null && int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
        {
            return parsed;
        }

        _unreadable = true;
        return fallback;
    }

    private float LookFloat(float value, string label, float fallback)
    {
        string? text = LookText(label, Scribe.mode == LoadSaveMode.Saving ? value.ToString("R", CultureInfo.InvariantCulture) : null, fallback.ToString("R", CultureInfo.InvariantCulture));
        if (Scribe.mode != LoadSaveMode.LoadingVars)
        {
            return value;
        }

        if (text != null && float.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
        {
            return parsed;
        }

        _unreadable = true;
        return fallback;
    }

    private bool LookBool(bool value, string label)
    {
        string? text = LookText(label, Scribe.mode == LoadSaveMode.Saving ? (value ? "true" : "false") : null, "false");
        if (Scribe.mode != LoadSaveMode.LoadingVars)
        {
            return value;
        }

        if (text != null && bool.TryParse(text.Trim(), out bool parsed))
        {
            return parsed;
        }

        _unreadable = true;
        return false;
    }

    private object LookEnum(Type enumType, object value, string label, object fallback)
    {
        string? text = LookText(label, Scribe.mode == LoadSaveMode.Saving ? value.ToString() : null, fallback.ToString()!);
        if (Scribe.mode != LoadSaveMode.LoadingVars)
        {
            return value;
        }

        if (text != null && Enum.TryParse(enumType, text.Trim(), ignoreCase: false, out object? parsed) && Enum.IsDefined(enumType, parsed))
        {
            return parsed;
        }

        _unreadable = true;
        return fallback;
    }

    private static string? LookText(string label, string? current, string fallbackText)
    {
        string? text = current;
        Scribe_Values.Look(ref text, label, fallbackText, forceSave: true);
        return text;
    }
}
