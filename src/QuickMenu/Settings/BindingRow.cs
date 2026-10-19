using System.Text;
using LudeonTK;
using QuickMenu.Core.Input;
using QuickMenu.Menu;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuickMenu.Settings;

/// <summary>
/// The "Open menu with" row of the settings page (design.md S7): binding button, [Clear], [Use middle mouse instead],
/// the key-conflict warning and the note. T-31 places it with <see cref="Draw"/>. Labels are cached per binding change.
/// </summary>
internal static class BindingRow
{
    private const float RowHeight = 30f;

    private static TriggerBinding _cachedFor;
    private static bool _cacheValid;
    private static string _label = string.Empty;
    private static string? _conflict;
    private static string? _openWith;
    private static string? _clear;
    private static string? _useMiddle;
    private static string? _note;

    /// <summary>Draws the row and note in the listing's column.</summary>
    internal static void Draw(Listing_Standard listing)
    {
        _openWith ??= "QuickMenu.Settings.OpenWith".Translate();
        _clear ??= "QuickMenu.Settings.Clear".Translate();
        _useMiddle ??= "QuickMenu.Settings.UseMiddleMouse".Translate();
        _note ??= "QuickMenu.Settings.BindingNote".Translate();

        QuickMenuSettings settings = QuickMenuMod.Settings;
        Refresh(settings.Trigger);

        Rect row = listing.GetRect(RowHeight);
        float labelWidth = row.width * 0.3f;
        Widgets.Label(new Rect(row.x, row.y, labelWidth, row.height), _openWith);
        var buttonRect = new Rect(row.x + labelWidth, row.y, 150f, row.height);
        if (settings.Trigger.Kind == TriggerBindingKind.None)
        {
            GUI.color = QMStyle.TextMuted;
        }

        bool open = Widgets.ButtonText(buttonRect, _label);
        GUI.color = Color.white;
        if (open)
        {
            Find.WindowStack.Add(new BindingCaptureWindow(Set));
        }

        if (Widgets.ButtonText(new Rect(buttonRect.xMax + 8f, row.y, 80f, row.height), _clear))
        {
            Set(TriggerBinding.None);
        }

        if (settings.Trigger != TriggerBinding.Mouse(2) && Widgets.ButtonText(listing.GetRect(RowHeight).LeftPartPixels(260f), _useMiddle))
        {
            Set(TriggerBinding.Mouse(2));
        }

        if (_conflict != null)
        {
            GUI.color = QMStyle.Danger;
            listing.Label(_conflict);
            GUI.color = Color.white;
        }

        Text.Font = GameFont.Tiny;
        GUI.color = QMStyle.TextMuted;
        listing.Label(_note);
        GUI.color = Color.white;
        Text.Font = GameFont.Small;
    }

    /// <summary>Stores the binding and writes the settings (once, on the user's action).</summary>
    internal static void Set(TriggerBinding binding)
    {
        QuickMenuMod.Settings.Trigger = binding;
        QuickMenuMod.SaveSettings();
    }

    // Temporary, until T-31 places the row: lets UC-2 open the capture dialog from dev mode.
    [DebugAction("Quick Menu", "set menu button", allowedGameStates = AllowedGameStates.Playing)]
    private static void OpenCaptureDebug() => Find.WindowStack.Add(new BindingCaptureWindow(Set));

    private static void Refresh(TriggerBinding binding)
    {
        if (_cacheValid && _cachedFor == binding)
        {
            return;
        }

        _cachedFor = binding;
        _cacheValid = true;
        _conflict = null;
        string? key = binding.LabelKey;
        if (key != null)
        {
            _label = key.Translate();
            return;
        }

        var code = (KeyCode)binding.Code;
        _label = code.ToString();
        _conflict = FindConflict(code, _label);
    }

    private static string? FindConflict(KeyCode code, string keyName)
    {
        StringBuilder? names = null;
        foreach (KeyBindingDef def in DefDatabase<KeyBindingDef>.AllDefsListForReading)
        {
            if (def.MainKey == code || KeyPrefs.KeyPrefsData.GetBoundKeyCode(def, KeyPrefs.BindingSlot.B) == code)
            {
                names ??= new StringBuilder();
                if (names.Length > 0)
                {
                    names.Append(", ");
                }

                names.Append(def.LabelCap.ToString());
            }
        }

        return names == null ? null : "QuickMenu.Settings.KeyConflict".Translate(keyName, names.ToString());
    }
}
