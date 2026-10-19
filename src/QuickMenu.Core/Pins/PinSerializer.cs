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
    internal PinLoadResult(PinBoard board, IReadOnlyList<string> preserved, IReadOnlyList<SkippedPinLine> skipped)
    {
        Board = board;
        Preserved = preserved;
        Skipped = skipped;
    }

    /// <summary>The loaded pins (capacity 0; the caller sets the capacity from settings).</summary>
    public PinBoard Board { get; }

    /// <summary>Well-formed lines with a key prefix this version does not know. Pass them back to <see cref="PinSerializer.ToLines"/>.</summary>
    public IReadOnlyList<string> Preserved { get; }

    /// <summary>Lines that were dropped, for the caller to log once.</summary>
    public IReadOnlyList<SkippedPinLine> Skipped { get; }

    public int SkippedCount => Skipped.Count;
}

/// <summary>
/// Pin line format v1: <c>slot|key|packageId|label</c>, with <c>\</c> and <c>|</c> escaped by a backslash in
/// each field (ADR 0004). Loading never throws: bad lines are skipped and reported as data.
/// </summary>
public static class PinSerializer
{
    public const int MaxLineLength = 4096;
    public const int MaxModDropdownChildren = 256;
    public const int MaxSlot = 4095;

    private const char Escape = '\\';
    private const char Sep = '|';
    private static readonly string[] _knownPrefixes = { "build:thing/", "build:terrain/", "dropdown:", "order:", "moddrop:" };

    /// <summary>Writes the board, then the preserved lines unchanged. Slots are written in ascending order.</summary>
    public static List<string> ToLines(PinBoard board, IReadOnlyList<string>? preserved = null)
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
            Append(sb, e.Key.Value);
            sb.Append(Sep);
            Append(sb, e.PackageId ?? string.Empty);
            sb.Append(Sep);
            Append(sb, e.Label);
            lines.Add(sb.ToString());
        }

        if (preserved != null)
        {
            lines.AddRange(preserved);
        }

        return lines;
    }

    /// <summary>Parses stored lines. Never throws; a null list gives an empty result.</summary>
    public static PinLoadResult FromLines(IReadOnlyList<string?>? lines)
    {
        var board = new PinBoard(0);
        var preserved = new List<string>();
        var skipped = new List<SkippedPinLine>();
        var usedSlots = new HashSet<int>();
        for (int i = 0; lines != null && i < lines.Count; i++)
        {
            PinLineIssue? issue = TryLoad(lines[i], board, preserved, usedSlots);
            if (issue is { } why)
            {
                skipped.Add(new SkippedPinLine(i, why));
            }
        }

        return new PinLoadResult(board, preserved, skipped);
    }

    private static PinLineIssue? TryLoad(string? line, PinBoard board, List<string> preserved, HashSet<int> usedSlots)
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
            board.SetSlot(slot, new PinEntry(key, f[3], f[2].Length == 0 ? null : f[2]));
            usedSlots.Add(slot);
            return null;
        }

        if (IsUnknownPrefix(keyText))
        {
            preserved.Add(line);
            usedSlots.Add(slot);
            return null;
        }

        return PinLineIssue.Malformed;
    }

    private static bool IsUnknownPrefix(string key)
    {
        int colon = key.IndexOf(':', StringComparison.Ordinal);
        if (colon <= 0)
        {
            return false;
        }

        foreach (string p in _knownPrefixes)
        {
            if (key.StartsWith(p, StringComparison.Ordinal))
            {
                return false;
            }
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
            if (key[i] == Escape)
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
            if (key[i] == Escape)
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

    private static void Append(StringBuilder sb, string field)
    {
        foreach (char c in field)
        {
            if (c is Escape or Sep)
            {
                sb.Append(Escape);
            }

            sb.Append(c);
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
                if (++i >= line.Length || line[i] is not (Escape or Sep))
                {
                    return false;
                }

                sb.Append(line[i]);
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
