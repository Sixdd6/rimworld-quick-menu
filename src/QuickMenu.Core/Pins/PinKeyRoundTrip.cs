using System;

namespace QuickMenu.Core.Pins;

/// <summary>Pure self-test checks on pin keys (ADR 0004).</summary>
public static class PinKeyRoundTrip
{
    /// <summary>True when parsing the canonical string of <paramref name="key"/> gives an equal key.</summary>
    public static bool Parses(PinKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return TryRoundTrip(key, out _);
    }

    /// <summary>
    /// Parses the canonical string of <paramref name="key"/>. Returns true when that gives an equal key, and
    /// <paramref name="parsed"/> is then the freshly parsed key (so callers can resolve what a save would load).
    /// </summary>
    public static bool TryRoundTrip(PinKey key, out PinKey? parsed)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (PinKey.TryParse(key.Value, out parsed) && parsed == key)
        {
            return true;
        }

        parsed = null;
        return false;
    }
}
