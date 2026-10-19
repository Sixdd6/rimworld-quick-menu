using System;
using QuickMenu.Core.Layout;

namespace QuickMenu.Core.Settings;

/// <summary>Defaults, clamping and derived capacity for the settings (architecture §4). Pure; the adapter stores the values.</summary>
public static class SettingsRules
{
    public const int SchemaVersion = 1;

    public const int DefaultRings = 2;
    public const int DefaultColumns = 5;
    public const int DefaultRows = 3;
    public const int DefaultButtonSize = 56;
    public const string DefaultTrigger = "mouse:3";

    public const int MinColumns = 3;
    public const int MaxColumns = 11;
    public const int MinRows = 1;
    public const int MaxRows = 7;

    /// <summary>Auto growth limits (design.md §1.1).</summary>
    public const int AutoMaxRings = 8;
    public const int AutoMaxRows = 9;

    public static int ClampRings(int value) => Math.Clamp(value, LayoutConstants.MinRings, LayoutConstants.MaxRings);

    public static int ClampColumns(int value) => ClampOdd(value, MinColumns, MaxColumns);

    public static int ClampRows(int value) => ClampOdd(value, MinRows, MaxRows);

    /// <summary>Clamps to 36..96 and snaps to the 4-unit step. NaN gives the default.</summary>
    public static float ClampButtonSize(float value)
    {
        if (float.IsNaN(value))
        {
            return DefaultButtonSize;
        }

        float clamped = Math.Clamp(value, LayoutConstants.MinButtonSize, LayoutConstants.MaxButtonSize);
        int steps = (int)MathF.Round((clamped - LayoutConstants.MinButtonSize) / LayoutConstants.ButtonSizeStep);
        return LayoutConstants.MinButtonSize + (steps * LayoutConstants.ButtonSizeStep);
    }

    public static bool IsDefined<TEnum>(TEnum value)
        where TEnum : struct, Enum => Enum.IsDefined(value);

    /// <summary>Pin slots the board can show: wheel ring total, or grid cells minus the hub cell. Auto uses the growth limits.</summary>
    public static int PinCapacity(MenuLayout layout, SizingMode sizing, int rings, int columns, int rows)
    {
        bool auto = sizing == SizingMode.Auto;
        if (layout == MenuLayout.Wheel)
        {
            return RingCapacity.Total(auto ? AutoMaxRings : ClampRings(rings));
        }

        return (ClampColumns(columns) * (auto ? AutoMaxRows : ClampRows(rows))) - 1;
    }

    private static int ClampOdd(int value, int min, int max)
    {
        int clamped = Math.Clamp(value, min, max);
        return (clamped & 1) == 0 ? Math.Min(clamped + 1, max) : clamped;
    }
}
