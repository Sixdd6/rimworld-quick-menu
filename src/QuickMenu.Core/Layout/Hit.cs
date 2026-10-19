namespace QuickMenu.Core.Layout;

/// <summary>What a pointer position is over (design.md 1.1 hit testing, S4a).</summary>
public enum HitKind
{
    /// <summary>Not over the menu.</summary>
    Outside = 0,

    /// <summary>Inside the disc but not on anything interactive.</summary>
    Inside,

    /// <summary>A pin slot; <see cref="Hit.Index"/> is the slot index.</summary>
    Slot,

    /// <summary>The hub main area (Close in use mode, Done in edit mode).</summary>
    HubClose,

    /// <summary>The hub sub-button (Edit in use mode, Add in edit mode).</summary>
    HubEdit,

    /// <summary>An extra button; <see cref="Hit.Index"/> is the caller's button kind.</summary>
    Button,

    /// <summary>The label plate.</summary>
    Plate,
}

/// <summary>Typed hit result. <see cref="Index"/> is the slot index or button kind, otherwise -1.</summary>
public readonly record struct Hit(HitKind Kind, int Index = -1)
{
    public static readonly Hit Outside = new(HitKind.Outside);

    public static readonly Hit Inside = new(HitKind.Inside);

    public static readonly Hit HubClose = new(HitKind.HubClose);

    public static readonly Hit HubEdit = new(HitKind.HubEdit);

    public static readonly Hit Plate = new(HitKind.Plate);

    public static Hit Slot(int index) => new(HitKind.Slot, index);

    public static Hit Button(int kind) => new(HitKind.Button, kind);
}
