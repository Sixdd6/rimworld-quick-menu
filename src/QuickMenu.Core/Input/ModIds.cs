using System;

namespace QuickMenu.Core.Input;

/// <summary>Matching of mod package ids, which the game lower-cases and may suffix with <c>_steam</c> or <c>_copy</c>.</summary>
public static class ModIds
{
    public const string Mint = "Dubwise.DubsMintMenus";
    public const string BetterArchitect = "ferny.BetterArchitect";
    public const string Multiplayer = "rwmt.Multiplayer";

    /// <summary>True when <paramref name="actual"/> is <paramref name="expected"/>, ignoring case and one <c>_steam</c> or <c>_copy</c> suffix.</summary>
    public static bool Matches(string? actual, string expected)
    {
        if (string.IsNullOrEmpty(actual))
        {
            return false;
        }

        if (actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (string suffix in new[] { "_steam", "_copy" })
        {
            if (actual.Length == expected.Length + suffix.Length
                && actual.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
                && actual.StartsWith(expected, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
