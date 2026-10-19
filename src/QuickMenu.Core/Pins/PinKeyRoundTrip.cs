using System;

namespace QuickMenu.Core.Pins;

/// <summary>Pure self-test checks on pin keys (ADR 0004).</summary>
public static class PinKeyRoundTrip
{
    /// <summary>True when parsing the canonical string of <paramref name="key"/> gives an equal key.</summary>
    public static bool Parses(PinKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return PinKey.TryParse(key.Value, out PinKey? parsed) && parsed == key;
    }
}
