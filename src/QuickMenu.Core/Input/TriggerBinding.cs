using System;
using System.Globalization;

namespace QuickMenu.Core.Input;

/// <summary>
/// The input that opens the quick menu (ADR 0003). A plain value: a key code (the integer value of the
/// game's KeyCode) or a mouse button index 2..4. Left and right buttons are rejected.
/// </summary>
public readonly struct TriggerBinding : IEquatable<TriggerBinding>
{
    public const int MinMouseButton = 2;
    public const int MaxMouseButton = 4;

    /// <summary>Unity KeyCode.Mouse0..Mouse6: mouse buttons are bound with <see cref="Mouse"/>, never as keys.</summary>
    public const int FirstMouseKeyCode = 323;
    public const int LastMouseKeyCode = 329;

    private const string NoneText = "none";
    private const string KeyPrefix = "key:";
    private const string MousePrefix = "mouse:";

    private TriggerBinding(TriggerBindingKind kind, int code)
    {
        Kind = kind;
        Code = code;
    }

    public TriggerBindingKind Kind { get; }

    /// <summary>Key code for <see cref="TriggerBindingKind.Key"/>, button index for Mouse, else 0.</summary>
    public int Code { get; }

    public static TriggerBinding None => default;

    /// <summary>A keyboard key. <paramref name="keyCode"/> must be positive and outside 323..329 (mouse key codes).</summary>
    public static TriggerBinding Key(int keyCode)
    {
        if (!IsValidKeyCode(keyCode))
        {
            throw new ArgumentOutOfRangeException(nameof(keyCode), "Key code must be positive and not a mouse key code (323..329).");
        }

        return new TriggerBinding(TriggerBindingKind.Key, keyCode);
    }

    /// <summary>A mouse button 2..4 (middle, Mouse 4, Mouse 5). 0 and 1 are rejected.</summary>
    public static TriggerBinding Mouse(int button)
    {
        if (!IsValidMouseButton(button))
        {
            throw new ArgumentOutOfRangeException(nameof(button), "Only mouse buttons 2 to 4 can be triggers.");
        }

        return new TriggerBinding(TriggerBindingKind.Mouse, button);
    }

    public static bool IsValidKeyCode(int keyCode) =>
        keyCode > 0 && (keyCode < FirstMouseKeyCode || keyCode > LastMouseKeyCode);

    public static bool IsValidMouseButton(int button) => button >= MinMouseButton && button <= MaxMouseButton;

    /// <summary>Keyed label id (adapter translates). Null for a key: the adapter shows the key name.</summary>
    public string? LabelKey => Kind switch
    {
        TriggerBindingKind.None => "QuickMenu.Binding.Unbound",
        TriggerBindingKind.Mouse => Code switch
        {
            2 => "QuickMenu.Binding.MiddleMouse",
            3 => "QuickMenu.Binding.Mouse4",
            _ => "QuickMenu.Binding.Mouse5",
        },
        _ => null,
    };

    /// <summary>Serialised form: <c>none</c>, <c>key:N</c> or <c>mouse:N</c>.</summary>
    public string Serialize() => Kind switch
    {
        TriggerBindingKind.Key => KeyPrefix + Code.ToString(CultureInfo.InvariantCulture),
        TriggerBindingKind.Mouse => MousePrefix + Code.ToString(CultureInfo.InvariantCulture),
        _ => NoneText,
    };

    /// <summary>Parses <see cref="Serialize"/> output. Returns false (and None) on any invalid text.</summary>
    public static bool TryParse(string? text, out TriggerBinding binding)
    {
        binding = default;
        if (text is null)
        {
            return false;
        }

        if (text == NoneText)
        {
            return true;
        }

        if (text.StartsWith(KeyPrefix, StringComparison.Ordinal)
            && TryInt(text.AsSpan(KeyPrefix.Length), out int key) && IsValidKeyCode(key))
        {
            binding = new TriggerBinding(TriggerBindingKind.Key, key);
            return true;
        }

        if (text.StartsWith(MousePrefix, StringComparison.Ordinal)
            && TryInt(text.AsSpan(MousePrefix.Length), out int button) && IsValidMouseButton(button))
        {
            binding = new TriggerBinding(TriggerBindingKind.Mouse, button);
            return true;
        }

        return false;
    }

    private static bool TryInt(ReadOnlySpan<char> s, out int value) =>
        int.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out value);

    public bool Equals(TriggerBinding other) => Kind == other.Kind && Code == other.Code;

    public override bool Equals(object? obj) => obj is TriggerBinding o && Equals(o);

    public override int GetHashCode() => HashCode.Combine((int)Kind, Code);

    public override string ToString() => Serialize();

    public static bool operator ==(TriggerBinding left, TriggerBinding right) => left.Equals(right);

    public static bool operator !=(TriggerBinding left, TriggerBinding right) => !left.Equals(right);
}
