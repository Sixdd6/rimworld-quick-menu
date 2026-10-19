namespace QuickMenu.Core.Input;

/// <summary>What the caller must do in response to an event.</summary>
public enum MenuCommandKind
{
    /// <summary>Nothing.</summary>
    None = 0,

    /// <summary>Show the menu.</summary>
    Open,

    /// <summary>Hide the menu.</summary>
    Close,

    /// <summary>Close the satellite and return to the main level.</summary>
    Back,

    /// <summary>Activate slot <see cref="MenuCommand.Index"/>; the menu is closed afterwards.</summary>
    Activate,

    /// <summary>Open the satellite of slot <see cref="MenuCommand.Index"/>.</summary>
    OpenSub,

    /// <summary>Enter edit mode.</summary>
    EnterEdit,

    /// <summary>Leave edit mode (the caller writes settings once).</summary>
    ExitEdit,
}

/// <summary>A command with its optional slot index (-1 when unused).</summary>
public readonly record struct MenuCommand(MenuCommandKind Kind, int Index = -1)
{
    public static readonly MenuCommand None = new(MenuCommandKind.None);

    public static readonly MenuCommand Open = new(MenuCommandKind.Open);

    public static readonly MenuCommand Close = new(MenuCommandKind.Close);

    public static readonly MenuCommand Back = new(MenuCommandKind.Back);

    public static readonly MenuCommand EnterEdit = new(MenuCommandKind.EnterEdit);

    public static readonly MenuCommand ExitEdit = new(MenuCommandKind.ExitEdit);

    public static MenuCommand Activate(int slot) => new(MenuCommandKind.Activate, slot);

    public static MenuCommand OpenSub(int slot) => new(MenuCommandKind.OpenSub, slot);
}
