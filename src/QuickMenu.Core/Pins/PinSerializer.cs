using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace QuickMenu.Core.Pins;

/// <summary>Why a stored pin line was not loaded.</summary>
public enum PinLineIssue
{
    /// <summary>The line is null or does not follow <c>slot|key|packageId|label</c>.</summary>
    Malformed,

    /// <summary>The line is longer than <see cref="PinSerializer.MaxLineLength"/>.</summary>
    TooLong,

    /// <summary>A mod-dropdown key lists more than <see cref="PinSerializer.MaxModDropdownChildren"/> children.</summary>
    TooManyChildren,

    /// <summary>The slot repeats an earlier line, or is beyond <see cref="PinSerializer.MaxSlot"/>.</summary>
    BadSlot,
}

/// <summary>One skipped line: its position in the input and the reason. Contains no pin content.</summary>
public readonly record struct SkippedPinLine(int LineIndex, PinLineIssue Issue);

/// <summary>Outcome of <see cref="PinSerializer.FromLines"/>.</summary>
public sealed class PinLoadResult
{
    internal PinLoadResult(PinBoard board, IReadOnlyList<SkippedPinLine> skipped)
    {
        Board = board;
        Skipped = skipped;
    }

    /// <summary>
    /// The loaded pins (capacity 0; the caller sets the capacity from settings). Lines this version cannot read
    /// are held as <see cref="PinKeyKind.Opaque"/> entries in their slots and written back unchanged.
    /// </summary>
    public PinBoard Board { get; }

    /// <summary>Lines that were dropped, for the caller to log once.</summary>
    public IReadOnlyList<SkippedPinLine> Skipped { get; }

    public int SkippedCount => Skipped.Count;
}

/// <summary>
/// Pin line format v1: <c>slot|key|packageId|label</c>. In each field <c>^</c>, <c>|</c> and <c>\</c> are written as
/// <c>^^</c>, <c>^|</c> and <c>^/</c>, so a backslash never appears in a line: the game's string loader rewrites the two
/// characters backslash and n to a newline (ADR 0004). Loading never throws: bad lines are skipped and reported as data.
/// </summary>
public static class PinSerializer
{
    public const int MaxLineLength = 4096;
    public const int MaxModDropdownChildren = 256;
    public const int MaxSlot = 4095;

    private const char Escape = '^';
    private const char Backslash = '\\';
    private const char BackslashCode = '/';
    private const char KeyEscape = '\\';
    private const char Sep = '|';
    private static readonly string[] _knownPrefixes = { "build:thing/", "build:terrain/", "dropdown:", "order:", "moddrop:" };

    /// <summary>Writes the board in ascending slot order; opaque entries are written back unchanged.</summary>
    public static List<string> ToLines(PinBoard board)
    {
        ArgumentNullException.ThrowIfNull(board);
        var lines = new List<string>();
        IReadOnlyList<PinEntry?> entries = board.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] is not { } e)
            {
                continue;
            }

            var sb = new StringBuilder();
            sb.Append(i.ToString(CultureInfo.InvariantCulture)).Append(Sep);
            if (e.Key.Kind == PinKeyKind.Opaque)
            {
                sb.Append(e.Key.Value);
            }
            else
            {
                Append(sb, e.Key.Value);
                sb.Append(Sep);
                Append(sb, Sanitize(e.PackageId ?? string.Empty));
                sb.Append(Sep);
                Append(sb, Sanitize(e.Label));
            }

            lines.Add(sb.ToString());
        }

        return lines;
    }

    /// <summary>Parses stored lines. Never throws; a null list gives an empty result.</summary>
    public static PinLoadResult FromLines(IReadOnlyList<string?>? lines)
    {
        var board = new PinBoard(0);
        var skipped = new List<SkippedPinLine>();
        var usedSlots = new HashSet<int>();
        for (int i = 0; lines != null && i < lines.Count; i++)
        {
            PinLineIssue? issue = TryLoad(lines[i], board, usedSlots);
            if (issue is { } why)
            {
                skipped.Add(new SkippedPinLine(i, why));
            }
        }

        return new PinLoadResult(board, skipped);
    }

    private static PinLineIssue? TryLoad(string? line, PinBoard board, HashSet<int> usedSlots)
    {
        if (line == null)
        {
            return PinLineIssue.Malformed;
        }

        if (line.Length > MaxLineLength)
        {
            return PinLineIssue.TooLong;
        }

        if (!TrySplit(line, out string[]? f) || !TryParseSlot(f[0], out int slot))
        {
            return PinLineIssue.Malformed;
        }

        if (slot > MaxSlot || usedSlots.Contains(slot))
        {
            return PinLineIssue.BadSlot;
        }

        string keyText = f[1];
        if (keyText.StartsWith("moddrop:", StringComparison.Ordinal) && CountChildren(keyText) > MaxModDropdownChildren)
        {
            return PinLineIssue.TooManyChildren;
        }

        if (PinKey.TryParse(keyText, out PinKey? key))
        {
            board.SetSlot(slot, new PinEntry(key, Sanitize(f[3]), f[2].Length == 0 ? null : Sanitize(f[2])));
            usedSlots.Add(slot);
            return null;
        }

        if (IsUnknownPrefix(keyText) || IsKnownPrefix(keyText))
        {
            // Written by a newer version (or a key form this one cannot read): keep the slot and the text.
            string raw = line[(line.IndexOf(Sep, StringComparison.Ordinal) + 1)..];
            board.SetSlot(slot, new PinEntry(PinKey.Opaque(raw), Sanitize(f[3]), f[2].Length == 0 ? null : Sanitize(f[2])));
            usedSlots.Add(slot);
            return null;
        }

        return PinLineIssue.Malformed;
    }

    private static bool IsKnownPrefix(string key)
    {
        foreach (string p in _knownPrefixes)
        {
            if (key.StartsWith(p, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsUnknownPrefix(string key)
    {
        int colon = key.IndexOf(':', StringComparison.Ordinal);
        if (colon <= 0 || IsKnownPrefix(key))
        {
            return false;
        }

        for (int i = 0; i < colon; i++)
        {
            char c = key[i];
            if (!(c is >= 'a' and <= 'z' || (i > 0 && c is >= '0' and <= '9')))
            {
                return false;
            }
        }

        return true;
    }

    private static int CountChildren(string key)
    {
        int tilde = -1;
        for (int i = 0; i < key.Length; i++)
        {
            if (key[i] == KeyEscape)
            {
                i++;
            }
            else if (key[i] == '~')
            {
                tilde = i;
                break;
            }
        }

        if (tilde < 0)
        {
            return 0;
        }

        int n = 1;
        for (int i = tilde + 1; i < key.Length; i++)
        {
            if (key[i] == KeyEscape)
            {
                i++;
            }
            else if (key[i] == ',')
            {
                n++;
            }
        }

        return n;
    }

    private static bool TryParseSlot(string text, out int slot)
    {
        slot = 0;
        if (text.Length is 0 or > 9 || (text.Length > 1 && text[0] == '0'))
        {
            return false;
        }

        foreach (char c in text)
        {
            if (c is < '0' or > '9')
            {
                return false;
            }

            slot = (slot * 10) + (c - '0');
        }

        return true;
    }

    /// <summary>
    /// Checks that a pin written to <paramref name="slot"/> loads back (line length, child count) and shortens the
    /// label, or drops the package id, to fit. The key is never altered. False when the key alone cannot be stored.
    /// </summary>
    internal static bool TryFit(int slot, PinKey key, string? packageId, string label, out string? fitPackage, out string fitLabel)
    {
        packageId = packageId == null ? null : Sanitize(packageId);
        label = Sanitize(label);
        fitPackage = packageId;
        fitLabel = label;
        if (key.Kind == PinKeyKind.ModDropdown && key.Children!.Count > MaxModDropdownChildren)
        {
            return false;
        }

        int budget = MaxLineLength - Digits(slot) - 3 - EscapedLength(key.Value);
        if (budget < 0)
        {
            return false;
        }

        int pkgLength = EscapedLength(packageId ?? string.Empty);
        if (pkgLength > budget)
        {
            fitPackage = null;
            pkgLength = 0;
        }

        budget -= pkgLength;
        int used = 0;
        int keep = 0;
        while (keep < label.Length)
        {
            int cost = label[keep] is Escape or Sep or Backslash ? 2 : 1;
            if (used + cost > budget)
            {
                break;
            }

            used += cost;
            keep++;
        }

        if (keep < label.Length)
        {
            if (keep > 0 && char.IsHighSurrogate(label[keep - 1]))
            {
                keep--;
            }

            fitLabel = label[..keep];
        }

        return true;
    }

    /// <summary>
    /// Drops '<' and '>' (so a label can never read as rich text or markup) and characters XML 1.0 cannot store (control characters, unpaired surrogates, U+FFFE/U+FFFF), so a pin label or
    /// package id can never make the settings file unreadable. Returns the same instance when nothing needs dropping.
    /// </summary>
    internal static string Sanitize(string text)
    {
        StringBuilder? sb = null;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            bool ok;
            int width = 1;
            if (char.IsHighSurrogate(c))
            {
                ok = i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]);
                width = ok ? 2 : 1;
            }
            else
            {
                ok = c is not ('<' or '>') && !char.IsLowSurrogate(c) && System.Xml.XmlConvert.IsXmlChar(c);
            }

            if (ok)
            {
                if (sb != null)
                {
                    sb.Append(text, i, width);
                }

                i += width - 1;
                continue;
            }

            sb ??= new StringBuilder(text.Length).Append(text, 0, i);
        }

        return sb?.ToString() ?? text;
    }

    private static int Digits(int n) => n < 10 ? 1 : n < 100 ? 2 : n < 1000 ? 3 : 4;

    private static int EscapedLength(string s)
    {
        int n = s.Length;
        foreach (char c in s)
        {
            if (c is Escape or Sep or Backslash)
            {
                n++;
            }
        }

        return n;
    }

    private static void Append(StringBuilder sb, string field)
    {
        foreach (char c in field)
        {
            if (c is Escape or Sep)
            {
                sb.Append(Escape).Append(c);
            }
            else if (c == Backslash)
            {
                sb.Append(Escape).Append(BackslashCode);
            }
            else
            {
                sb.Append(c);
            }
        }
    }

    /// <summary>Splits on unescaped separators into exactly four unescaped fields.</summary>
    private static bool TrySplit(string line, [NotNullWhen(true)] out string[]? fields)
    {
        fields = null;
        var result = new string[4];
        var sb = new StringBuilder();
        int n = 0;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == Escape)
            {
                if (++i >= line.Length || line[i] is not (Escape or Sep or BackslashCode))
                {
                    return false;
                }

                sb.Append(line[i] == BackslashCode ? Backslash : line[i]);
            }
            else if (c == Sep)
            {
                if (n >= 3)
                {
                    return false;
                }

                result[n++] = sb.ToString();
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }

        if (n != 3)
        {
            return false;
        }

        result[3] = sb.ToString();
        fields = result;
        return true;
    }
}
