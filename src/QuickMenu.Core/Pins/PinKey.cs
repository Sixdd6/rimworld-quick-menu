using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace QuickMenu.Core.Pins;

/// <summary>
/// Stable identity string of a pinnable designator (ADR 0004). Equality is by the canonical string.
/// Inside a name the characters <c>\ , ~ @ # |</c> are escaped with a backslash, so any def or type
/// name round-trips. Mod-dropdown children are complete keys, kept with their own escapes.
/// </summary>
public sealed class PinKey : IEquatable<PinKey>
{
    private const char Escape = '\\';
    private const int MaxOrdinal = 999_999_999;
    private static readonly SearchValues<char> _specials = SearchValues.Create("\\,~@#|");

    private PinKey(
        PinKeyKind kind,
        string name,
        int? ordinal,
        string? category,
        IReadOnlyList<PinKey>? children,
        string value)
    {
        Kind = kind;
        Name = name;
        Ordinal = ordinal;
        Category = category;
        Children = children;
        Value = value;
    }

    public PinKeyKind Kind { get; }

    /// <summary>Def name (build, dropdown) or type full name (order, mod dropdown), unescaped.</summary>
    public string Name { get; }

    /// <summary>Position among same-typed order designators; null when the type is unique.</summary>
    public int? Ordinal { get; }

    /// <summary>Category def name of a mod dropdown, unescaped; otherwise null.</summary>
    public string? Category { get; }

    /// <summary>Sorted, unique child keys of a mod dropdown (a set); read-only; otherwise null.</summary>
    public IReadOnlyList<PinKey>? Children { get; }

    /// <summary>The canonical string form.</summary>
    public string Value { get; }

    public static PinKey Build(PinKeyKind kind, string defName)
    {
        if (kind != PinKeyKind.BuildThing && kind != PinKeyKind.BuildTerrain)
        {
            throw new ArgumentException("Kind must be BuildThing or BuildTerrain.", nameof(kind));
        }

        string prefix = kind == PinKeyKind.BuildThing ? "build:thing/" : "build:terrain/";
        return new PinKey(kind, defName, null, null, null, prefix + Esc(defName, nameof(defName)));
    }

    public static PinKey Dropdown(string groupDefName) =>
        new(PinKeyKind.Dropdown, groupDefName, null, null, null, "dropdown:" + Esc(groupDefName, nameof(groupDefName)));

    public static PinKey Order(string typeFullName, int? ordinal = null)
    {
        if (ordinal is < 0 or > MaxOrdinal)
        {
            throw new ArgumentOutOfRangeException(nameof(ordinal), ordinal, "Ordinal must be 0 to 999999999.");
        }

        string value = "order:" + Esc(typeFullName, nameof(typeFullName));
        if (ordinal.HasValue)
        {
            value += "#" + ordinal.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return new PinKey(PinKeyKind.Order, typeFullName, ordinal, null, null, value);
    }

    public static PinKey ModDropdown(string typeFullName, string categoryDefName, IEnumerable<PinKey> children)
    {
        ArgumentNullException.ThrowIfNull(children);
        var sorted = new List<PinKey>();
        foreach (PinKey child in children)
        {
            ArgumentNullException.ThrowIfNull(child);
            if (child.Kind == PinKeyKind.ModDropdown)
            {
                throw new ArgumentException("A mod dropdown cannot contain another mod dropdown.", nameof(children));
            }

            sorted.Add(child);
        }

        if (sorted.Count == 0)
        {
            throw new ArgumentException("A mod dropdown needs at least one child.", nameof(children));
        }

        sorted.Sort(static (a, b) => string.CompareOrdinal(a.Value, b.Value));
        for (int i = sorted.Count - 1; i > 0; i--)
        {
            if (sorted[i].Equals(sorted[i - 1]))
            {
                sorted.RemoveAt(i);
            }
        }

        var sb = new StringBuilder("moddrop:");
        sb.Append(Esc(typeFullName, nameof(typeFullName))).Append('@').Append(Esc(categoryDefName, nameof(categoryDefName))).Append('~');
        for (int i = 0; i < sorted.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }

            sb.Append(sorted[i].Value);
        }

        return new PinKey(PinKeyKind.ModDropdown, typeFullName, null, categoryDefName, Array.AsReadOnly(sorted.ToArray()), sb.ToString());
    }

    /// <summary>Parses a key; never throws. Child order in a mod dropdown is normalised to sorted.</summary>
    public static bool TryParse(string? text, [NotNullWhen(true)] out PinKey? key)
    {
        key = null;
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        if (text.StartsWith("build:thing/", StringComparison.Ordinal))
        {
            return TryUnescape(text.AsSpan(12), out string? n) && (key = Build(PinKeyKind.BuildThing, n)) != null;
        }

        if (text.StartsWith("build:terrain/", StringComparison.Ordinal))
        {
            return TryUnescape(text.AsSpan(14), out string? n) && (key = Build(PinKeyKind.BuildTerrain, n)) != null;
        }

        if (text.StartsWith("dropdown:", StringComparison.Ordinal))
        {
            return TryUnescape(text.AsSpan(9), out string? n) && (key = Dropdown(n)) != null;
        }

        if (text.StartsWith("order:", StringComparison.Ordinal))
        {
            return TryParseOrder(text.AsSpan(6), out key);
        }

        if (text.StartsWith("moddrop:", StringComparison.Ordinal))
        {
            return TryParseModDropdown(text, out key);
        }

        return false;
    }

    public static bool operator ==(PinKey? left, PinKey? right) => Equals(left, right);

    public static bool operator !=(PinKey? left, PinKey? right) => !Equals(left, right);

    public bool Equals(PinKey? other) => other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is PinKey other && Equals(other);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    public override string ToString() => Value;

    private static bool TryParseOrder(ReadOnlySpan<char> rest, out PinKey? key)
    {
        key = null;
        int hash = IndexOfUnescaped(rest, '#');
        int? ordinal = null;
        ReadOnlySpan<char> typePart = rest;
        if (hash >= 0)
        {
            typePart = rest[..hash];
            ReadOnlySpan<char> digits = rest[(hash + 1)..];
            if (digits.Length == 0 || digits.Length > 9 || (digits.Length > 1 && digits[0] == '0'))
            {
                return false;
            }

            int n = 0;
            foreach (char c in digits)
            {
                if (c is < '0' or > '9')
                {
                    return false;
                }

                n = (n * 10) + (c - '0');
            }

            ordinal = n;
        }

        if (!TryUnescape(typePart, out string? type))
        {
            return false;
        }

        key = Order(type, ordinal);
        return true;
    }

    private static bool TryParseModDropdown(string text, out PinKey? key)
    {
        key = null;
        ReadOnlySpan<char> rest = text.AsSpan(8);
        int at = IndexOfUnescaped(rest, '@');
        if (at < 0)
        {
            return false;
        }

        ReadOnlySpan<char> afterAt = rest[(at + 1)..];
        int tilde = IndexOfUnescaped(afterAt, '~');
        if (tilde < 0 ||
            !TryUnescape(rest[..at], out string? type) ||
            !TryUnescape(afterAt[..tilde], out string? category))
        {
            return false;
        }

        var children = new List<PinKey>();
        ReadOnlySpan<char> list = afterAt[(tilde + 1)..];
        while (true)
        {
            int comma = IndexOfUnescaped(list, ',');
            ReadOnlySpan<char> item = comma < 0 ? list : list[..comma];
            if (item.StartsWith("moddrop:", StringComparison.Ordinal) ||
                !TryParse(item.ToString(), out PinKey? child))
            {
                return false;
            }

            children.Add(child);
            if (comma < 0)
            {
                break;
            }

            list = list[(comma + 1)..];
        }

        key = ModDropdown(type, category, children);
        return true;
    }

    /// <summary>Index of the first unescaped occurrence of <paramref name="c"/>, or -1.</summary>
    private static int IndexOfUnescaped(ReadOnlySpan<char> s, char c)
    {
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == Escape)
            {
                i++;
            }
            else if (s[i] == c)
            {
                return i;
            }
        }

        return -1;
    }

    private static string Esc(string? name, string paramName)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("Name must not be empty.", paramName);
        }

        if (name.AsSpan().IndexOfAny(_specials) < 0)
        {
            return name;
        }

        var sb = new StringBuilder(name.Length + 4);
        foreach (char c in name)
        {
            if (_specials.Contains(c))
            {
                sb.Append(Escape);
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>Strict unescape: rejects empty text, unescaped specials, unknown escapes and a trailing backslash.</summary>
    private static bool TryUnescape(ReadOnlySpan<char> s, [NotNullWhen(true)] out string? result)
    {
        result = null;
        if (s.IsEmpty)
        {
            return false;
        }

        var sb = new StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == Escape)
            {
                if (++i >= s.Length || !_specials.Contains(s[i]))
                {
                    return false;
                }

                sb.Append(s[i]);
            }
            else if (_specials.Contains(c))
            {
                return false;
            }
            else
            {
                sb.Append(c);
            }
        }

        result = sb.ToString();
        return true;
    }
}
