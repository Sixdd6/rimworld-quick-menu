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

    /// <summary>
    /// The slot <see cref="MenuCommand.Index"/> is disabled: play the reject sound and show the reason.
    /// The menu stays open.
    /// </summary>
    Reject,
}

/// <summary>The level a slot belongs to.</summary>
public enum MenuLevel
{
    Main = 0,
    Satellite,
}

/// <summary>
/// A command with its optional slot index (-1 when unused). <see cref="Level"/> is set for Activate and
/// OpenSub: the level the slot was on. <see cref="WasEditing"/> is true when a Close ended edit mode,
/// so the controller must save the pins (ExitEdit already implies it).
/// </summary>
public readonly record struct MenuCommand(
    MenuCommandKind Kind,
    int Index = -1,
    MenuLevel Level = MenuLevel.Main,
    bool WasEditing = false)
{
    public static readonly MenuCommand None = new(MenuCommandKind.None);

    public static readonly MenuCommand Open = new(MenuCommandKind.Open);

    public static readonly MenuCommand Close = new(MenuCommandKind.Close);

    public static readonly MenuCommand Back = new(MenuCommandKind.Back);

    public static readonly MenuCommand EnterEdit = new(MenuCommandKind.EnterEdit);

    public static readonly MenuCommand ExitEdit = new(MenuCommandKind.ExitEdit);

    /// <summary>Close that ended edit mode (trigger press or map lost while editing).</summary>
    public static readonly MenuCommand CloseFromEdit = new(MenuCommandKind.Close, -1, MenuLevel.Main, true);

    public static MenuCommand Activate(int slot, MenuLevel level = MenuLevel.Main) =>
        new(MenuCommandKind.Activate, slot, level);

    public static MenuCommand OpenSub(int slot) => new(MenuCommandKind.OpenSub, slot);

    public static MenuCommand Reject(int slot) => new(MenuCommandKind.Reject, slot);
}
