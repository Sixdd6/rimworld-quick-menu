using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;
using QuickMenu.Core.Pins;
using QuickMenu.Core.Settings;
using Xunit;

namespace QuickMenu.Core.Tests.Settings;

public class SettingsPreviewTests
{
    private static readonly Vec2 _screen1080 = new(1920f, 1080f);
    private static readonly Vec2 _screenScale2 = new(960f, 540f);

    [Fact]
    public void Default_wheel_bounds_are_336_by_the_disc_plus_plate()
    {
        Rect2 b = SettingsPreview.Bounds(MenuLayout.Wheel, SizingMode.Fixed, 2, 5, 3, 56f, 0, 30f);
        Assert.Equal(336f, b.Width, 0.5f);
        Assert.Equal(374f, b.Height, 0.5f);
        Assert.Equal(-168f, b.Y, 0.5f);
    }

    [Fact]
    public void Auto_wheel_bounds_follow_the_pin_count_and_ignore_the_rings_setting()
    {
        Rect2 few = SettingsPreview.Bounds(MenuLayout.Wheel, SizingMode.Auto, 5, 5, 3, 56f, 3, 30f);
        Rect2 one = SettingsPreview.Bounds(MenuLayout.Wheel, SizingMode.Fixed, 1, 5, 3, 56f, 0, 30f);
        Assert.Equal(one, few);
    }

    [Fact]
    public void Grid_bounds_use_the_panel_and_a_plate_no_wider_than_the_panel()
    {
        Rect2 b = SettingsPreview.Bounds(MenuLayout.Grid, SizingMode.Fixed, 2, 5, 3, 56f, 0, 30f);
        Rect2 panel = GridLayout.Panel(new GridParams(56f, 5, 3, default));
        Assert.Equal(panel.Width, b.Width, 0.5f);
        Assert.Equal(panel.Height + LayoutConstants.PlateGap + 30f, b.Height, 0.5f);
    }

    [Fact]
    public void Auto_grid_rows_follow_the_pin_count()
    {
        Rect2 a = SettingsPreview.Bounds(MenuLayout.Grid, SizingMode.Auto, 2, 5, 7, 56f, 15, 30f);
        Rect2 f = SettingsPreview.Bounds(MenuLayout.Grid, SizingMode.Fixed, 2, 5, 5, 56f, 0, 30f);
        Assert.Equal(f, a);
    }

    [Fact]
    public void Default_wheel_fits_at_scale_1_and_at_scale_2()
    {
        Rect2 b = SettingsPreview.Bounds(MenuLayout.Wheel, SizingMode.Fixed, 2, 5, 3, 56f, 0, 30f);
        Assert.True(SettingsPreview.FitsScreen(b, _screen1080));
        Assert.True(SettingsPreview.FitsScreen(b, _screenScale2));
    }

    [Fact]
    public void Four_ring_wheel_does_not_fit_at_ui_scale_2()
    {
        Rect2 b = SettingsPreview.Bounds(MenuLayout.Wheel, SizingMode.Fixed, 4, 5, 3, 56f, 0, 30f);
        Assert.False(SettingsPreview.FitsScreen(b, _screenScale2));
        Assert.True(SettingsPreview.FitsScreen(b, _screen1080));
    }

    [Fact]
    public void Plan_scale_is_half_for_the_default_and_shrinks_to_fit_the_area()
    {
        Rect2 small = SettingsPreview.Bounds(MenuLayout.Wheel, SizingMode.Fixed, 2, 5, 3, 56f, 0, 30f);
        Assert.Equal(0.5f, SettingsPreview.Scale(small));
        Rect2 big = SettingsPreview.Bounds(MenuLayout.Wheel, SizingMode.Fixed, 5, 5, 3, 96f, 0, 30f);
        float scale = SettingsPreview.Scale(big);
        Assert.True(scale < 0.5f);
        Assert.True(big.Width * scale <= SettingsPreview.Width + 0.01f);
        Assert.True(big.Height * scale <= SettingsPreview.Height + 0.01f);
    }

    [Fact]
    public void Hub_offset_centres_the_bounds_in_the_area()
    {
        var b = new Rect2(-168f, -168f, 336f, 374f);
        Vec2 hub = SettingsPreview.HubOffset(b, 0.5f);
        Assert.Equal(0f, hub.X, 0.01f);
        Assert.Equal(-(b.Y + (b.Height / 2f)) * 0.5f, hub.Y, 0.01f);
    }

    [Fact]
    public void Hidden_pins_are_the_stored_pins_beyond_the_capacity()
    {
        var board = new PinBoard(18);
        board.SetSlot(2, new PinEntry(PinKey.Build(PinKeyKind.BuildThing, "A"), "A", null));
        board.SetSlot(20, new PinEntry(PinKey.Build(PinKeyKind.BuildThing, "B"), "B", null));
        board.SetSlot(25, new PinEntry(PinKey.Build(PinKeyKind.BuildThing, "C"), "C", null));
        Assert.Equal(2, SettingsPreview.HiddenPins(board));
        board.Capacity = 30;
        Assert.Equal(0, SettingsPreview.HiddenPins(board));
    }

    [Theory]
    [InlineData(2, 1, 3)]
    [InlineData(5, 1, 5)]
    [InlineData(1, -1, 1)]
    public void Rings_step_by_one_and_clamp(int value, int delta, int expected) =>
        Assert.Equal(expected, SettingsPreview.StepRings(value, delta));

    [Theory]
    [InlineData(5, 1, 7)]
    [InlineData(5, -1, 3)]
    [InlineData(3, -1, 3)]
    [InlineData(11, 1, 11)]
    [InlineData(4, 1, 7)]
    public void Columns_step_by_two_and_stay_odd(int value, int delta, int expected) =>
        Assert.Equal(expected, SettingsPreview.StepColumns(value, delta));

    [Theory]
    [InlineData(3, 1, 5)]
    [InlineData(1, -1, 1)]
    [InlineData(7, 1, 7)]
    public void Rows_step_by_two_and_stay_odd(int value, int delta, int expected) =>
        Assert.Equal(expected, SettingsPreview.StepRows(value, delta));

    [Theory]
    [InlineData(MenuLayout.Wheel, SizingMode.Fixed, true, false, false)]
    [InlineData(MenuLayout.Wheel, SizingMode.Auto, false, false, false)]
    [InlineData(MenuLayout.Grid, SizingMode.Fixed, false, true, true)]
    [InlineData(MenuLayout.Grid, SizingMode.Auto, false, true, false)]
    public void Steppers_shown_per_shape_and_sizing(MenuLayout layout, SizingMode sizing, bool rings, bool columns, bool rows)
    {
        Assert.Equal(rings, SettingsPreview.ShowsRings(layout, sizing));
        Assert.Equal(columns, SettingsPreview.ShowsColumns(layout, sizing));
        Assert.Equal(rows, SettingsPreview.ShowsRows(layout, sizing));
    }
}
