using System;
using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;
using QuickMenu.Core.Pins;

namespace QuickMenu.Core.Settings;

/// <summary>
/// Pure rules of the settings page (design.md S7): which size controls show, how they step, the menu's bounding box for
/// the live preview, the preview scale and whether the menu fits the screen at the current UI scale. No game types.
/// </summary>
public static class SettingsPreview
{
    /// <summary>Preview area (design.md S7), UI px.</summary>
    public const float Width = 410f;

    public const float Height = 270f;

    /// <summary>The preview is drawn at half size, or smaller when the menu would not fit the area.</summary>
    public const float MaxScale = 0.5f;

    public static bool ShowsRings(MenuLayout layout, SizingMode sizing) => layout == MenuLayout.Wheel && sizing == SizingMode.Fixed;

    /// <summary>Columns stay in Auto: they set the width of an Auto grid.</summary>
    public static bool ShowsColumns(MenuLayout layout, SizingMode sizing) => layout == MenuLayout.Grid;

    public static bool ShowsRows(MenuLayout layout, SizingMode sizing) => layout == MenuLayout.Grid && sizing == SizingMode.Fixed;

    public static int StepRings(int value, int delta) => SettingsRules.ClampRings(value + delta);

    /// <summary>Columns move by 2 so they stay odd (the hub is the centre cell).</summary>
    public static int StepColumns(int value, int delta) => SettingsRules.ClampColumns(SettingsRules.ClampColumns(value) + (2 * Math.Sign(delta)));

    public static int StepRows(int value, int delta) => SettingsRules.ClampRows(SettingsRules.ClampRows(value) + (2 * Math.Sign(delta)));

    /// <summary>
    /// The menu's bounding box (disc or panel plus label plate) with the hub at the origin, as the menu would be drawn.
    /// Auto sizes follow <paramref name="autoPinCount"/>; Fixed ones follow the settings.
    /// </summary>
    public static Rect2 Bounds(MenuLayout layout, SizingMode sizing, int rings, int columns, int rows, float buttonSize, int autoPinCount, float plateHeight)
    {
        bool auto = sizing == SizingMode.Auto;
        if (layout == MenuLayout.Wheel)
        {
            int m = auto ? Capacity.AutoWheelRings(autoPinCount) : SettingsRules.ClampRings(rings);
            return MenuBounds.ForWheel(default, buttonSize, m, plateHeight);
        }

        int cols = SettingsRules.ClampColumns(columns);
        int r = auto ? Capacity.AutoGridRows(autoPinCount, cols) : SettingsRules.ClampRows(rows);
        Rect2 panel = GridLayout.Panel(new GridParams(buttonSize, cols, r, default));
        return MenuBounds.ForPanel(panel, panel.Width, plateHeight);
    }

    /// <summary>True when the box fits inside the screen minus the margin on every side; otherwise the menu is centred and overlaps the edge.</summary>
    public static bool FitsScreen(Rect2 bounds, Vec2 screen) =>
        bounds.Width <= screen.X - (2f * LayoutConstants.Margin) && bounds.Height <= screen.Y - (2f * LayoutConstants.Margin);

    /// <summary>Preview scale: <see cref="MaxScale"/>, reduced so the box fits <see cref="Width"/> x <see cref="Height"/>.</summary>
    public static float Scale(Rect2 bounds)
    {
        float scale = MaxScale;
        if (bounds.Width > 0f)
        {
            scale = Math.Min(scale, Width / bounds.Width);
        }

        if (bounds.Height > 0f)
        {
            scale = Math.Min(scale, Height / bounds.Height);
        }

        return scale;
    }

    /// <summary>Where the hub goes, relative to the centre of the preview area, so the scaled box is centred in it.</summary>
    public static Vec2 HubOffset(Rect2 bounds, float scale) =>
        new(-(bounds.X + (bounds.Width / 2f)) * scale, -(bounds.Y + (bounds.Height / 2f)) * scale);

    /// <summary>Stored pins beyond the board's capacity: kept, but not shown until the menu is bigger again.</summary>
    public static int HiddenPins(PinBoard board)
    {
        int n = 0;
        for (int i = board.Capacity; i < board.Entries.Count; i++)
        {
            if (board.Entries[i] != null)
            {
                n++;
            }
        }

        return n;
    }
}
