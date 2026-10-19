using System;
using QuickMenu.Core.Input;
using Verse;

namespace QuickMenu.Menu;

/// <summary>
/// The Keyed texts of edit mode and the empty menu that name the pin modifier (design.md S4, S5, Tooltips; T-55),
/// translated once per modifier and kept, so drawing and hovering never build strings.
/// </summary>
/// <remarks>
/// For T-57: append <see cref="EditSlotTip"/> to a slot's tooltip in edit mode (occupied slots; empty slots have no
/// tooltip). For T-58: <see cref="EmptyHint"/> is the text of <see cref="HelpPanelRenderer.DrawHint"/>.
/// </remarks>
internal static class EditTexts
{
    private const int ModifierCount = 3;

    private static readonly string?[] _modifierNames = new string?[ModifierCount];
    private static readonly string?[] _slotTips = new string?[ModifierCount];
    private static readonly string?[] _hints = new string?[ModifierCount];
    private static readonly string?[] _helpFixed = new string?[ModifierCount];
    private static readonly string?[] _helpAuto = new string?[ModifierCount];

    /// <summary>The player-facing name of <paramref name="modifier"/> ("Ctrl", "Alt", "Shift").</summary>
    internal static string ModifierName(PinModifier modifier)
    {
        DropIfLanguageChanged();
        int i = Index(modifier);
        return _modifierNames[i] ??= KeyOf(modifier).Translate().Resolve();
    }

    /// <summary>"Drag to move. {modifier}+click to remove." for the effective pin modifier (edit-mode slot tooltip line).</summary>
    internal static string EditSlotTip => Cached(_slotTips, "QuickMenu.Menu.EditSlotTip");

    /// <summary>The empty-menu hint (S5, FR-16) for the effective pin modifier.</summary>
    internal static string EmptyHint => Cached(_hints, "QuickMenu.Menu.EmptyHint");

    /// <summary>The edit help text (S5, §1.4) for the effective pin modifier: Fixed or Auto.</summary>
    internal static string EditHelp(bool auto) =>
        auto ? Cached(_helpAuto, "QuickMenu.Menu.EditHelpAuto") : Cached(_helpFixed, "QuickMenu.Menu.EditHelpFixed");

    private static string Cached(string?[] cache, string key)
    {
        DropIfLanguageChanged();
        PinModifier modifier = QuickMenuMod.EffectiveModifier;
        int i = Index(modifier);
        return cache[i] ??= key.Translate(ModifierName(modifier)).Resolve();
    }

    private static object? _language;

    private static void DropIfLanguageChanged()
    {
        object? active = LanguageDatabase.activeLanguage;
        if (ReferenceEquals(active, _language))
        {
            return;
        }

        _language = active;
        Array.Clear(_modifierNames);
        Array.Clear(_slotTips);
        Array.Clear(_hints);
        Array.Clear(_helpFixed);
        Array.Clear(_helpAuto);
    }

    private static int Index(PinModifier modifier) => modifier switch
    {
        PinModifier.Alt => 1,
        PinModifier.Shift => 2,
        _ => 0,
    };

    private static string KeyOf(PinModifier modifier) => modifier switch
    {
        PinModifier.Alt => "QuickMenu.Settings.Modifier.Alt",
        PinModifier.Shift => "QuickMenu.Settings.Modifier.Shift",
        _ => "QuickMenu.Settings.Modifier.Ctrl",
    };
}
