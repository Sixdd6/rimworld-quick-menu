namespace QuickMenu.Core.Input;

/// <summary>Which level of the menu is showing.</summary>
public enum MenuState
{
    /// <summary>Not open.</summary>
    Closed = 0,

    /// <summary>The main wheel or grid.</summary>
    Main,

    /// <summary>A sub-menu or material satellite is open on top of the main level.</summary>
    Satellite,

    /// <summary>Edit mode (pin editing); the hub shows Done.</summary>
    Edit,
}
