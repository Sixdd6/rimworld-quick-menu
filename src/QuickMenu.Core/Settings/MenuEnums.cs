namespace QuickMenu.Core.Settings;

/// <summary>How the trigger opens the menu (FR-2).</summary>
public enum TriggerMode
{
    Toggle = 0,
    Hold = 1,
}

/// <summary>Menu layout (FR-4).</summary>
public enum MenuLayout
{
    Wheel = 0,
    Grid = 1,
}

/// <summary>Fixed capacity from the size settings, or Auto growth (FR-5).</summary>
public enum SizingMode
{
    Fixed = 0,
    Auto = 1,
}
