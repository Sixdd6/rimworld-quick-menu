using System;

namespace QuickMenu.Core.Pins;

/// <summary>
/// Decides whether an unresolved stored pin is really gone (FR-14), with the game lookups passed in as predicates so the
/// rule is testable without the game. A build pin is decided on its def alone: the def exists or it does not. The stored
/// packageId is only a hint and is used for kinds that have no def to look up.
/// </summary>
public static class PinMissingRule
{
    /// <summary>True when the pin is missing. Opaque pins are never missing.</summary>
    /// <param name="key">The stored key.</param>
    /// <param name="packageId">The package id stored with the pin, if any.</param>
    /// <param name="defExists">Whether a def of the key's kind and name exists (called for build kinds only).</param>
    /// <param name="modActive">Whether a mod with the given (normalised) package id is active.</param>
    public static bool IsMissing(PinKey key, string? packageId, Func<PinKey, bool> defExists, Func<string, bool> modActive)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(defExists);
        ArgumentNullException.ThrowIfNull(modActive);
        switch (key.Kind)
        {
            case PinKeyKind.Opaque:
                return false;
            case PinKeyKind.BuildThing:
            case PinKeyKind.BuildTerrain:
                return !defExists(key);
            default:
                string id = NormalizePackageId(packageId);
                return id.Length > 0 && !modActive(id);
        }
    }

    /// <summary>Lower-cases the id and drops a trailing "_steam" (the suffix of a Workshop copy of a mod).</summary>
    public static string NormalizePackageId(string? packageId)
    {
        if (string.IsNullOrEmpty(packageId))
        {
            return string.Empty;
        }

        string id = packageId.Trim().ToLowerInvariant();
        const string Steam = "_steam";
        return id.EndsWith(Steam, StringComparison.Ordinal) ? id[..^Steam.Length] : id;
    }
}
