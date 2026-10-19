using System;

namespace QuickMenu.Core.Diagnostics;

/// <summary>
/// Name-only heuristics for development tooling. These are hints, not measurements: a mod can name a normal
/// designator anything, and a dev-only one anything else.
/// </summary>
public static class DevNameHint
{
    private static readonly string[] _typePrefixes = { "Designator_Dev", "Debug", "God" };

    /// <summary>True when a designator type name starts with <c>Designator_Dev</c>, <c>Debug</c> or <c>God</c>.</summary>
    public static bool IsDevTypeName(string? typeName)
    {
        if (string.IsNullOrEmpty(typeName))
        {
            return false;
        }

        foreach (string prefix in _typePrefixes)
        {
            if (typeName.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// True when <c>Dev</c> or <c>Debug</c> is a whole token of the name. Tokens split on non-letters and at
    /// lower-to-upper case changes, so <c>DevTools</c> matches but <c>Device</c> and <c>Devilstrand</c> do not.
    /// </summary>
    public static bool HasDevToken(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        int start = 0;
        for (int i = 1; i <= name.Length; i++)
        {
            bool boundary = i == name.Length || !char.IsLetter(name[i]) ||
                            (char.IsUpper(name[i]) && char.IsLower(name[i - 1]));
            if (!boundary)
            {
                continue;
            }

            if (IsDevToken(name.AsSpan(start, i - start)))
            {
                return true;
            }

            start = i;
        }

        return false;
    }

    private static bool IsDevToken(ReadOnlySpan<char> token)
    {
        while (token.Length > 0 && !char.IsLetter(token[0]))
        {
            token = token[1..];
        }

        return token.Equals("Dev", StringComparison.OrdinalIgnoreCase) || token.Equals("Debug", StringComparison.OrdinalIgnoreCase);
    }
}
