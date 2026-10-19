using System;

using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;
using QuickMenu.Core.Settings;
using Xunit;

namespace QuickMenu.Core.Tests.Layout;

public class AutoAndSubGridPlacementTests
{
    private static readonly Vec2 _screen = new(1920f, 1080f);
    private static readonly Vec2 _mouse = new(960f, 540f);

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(6, 1)]
    [InlineData(7, 2)]
    [InlineData(18, 2)]
    [InlineData(19, 3)]
    public void PlaceAuto_uses_the_smallest_ring_count(int count, int rings)
    {
        var placed = WheelLayout.PlaceAuto(count, 56f, _mouse, _screen);
        Assert.Equal(rings, placed.Rings);
        Assert.Equal(count, placed.Slots.Length);
        Assert.Equal(_mouse, placed.Center);
    }

    [Fact]
    public void PlaceAuto_matches_ComputeSpread_around_the_centre()
    {
        var placed = WheelLayout.PlaceAuto(10, 56f, _mouse, _screen);
        var expected = new SlotPlacement[10];
        WheelLayout.ComputeSpread(10, 56f, placed.Center, expected);
        Assert.Equal(expected, placed.Slots);
    }

    [Fact]
    public void PlaceAuto_dodges_the_screen_edge()
    {
        var placed = WheelLayout.PlaceAuto(18, 56f, new Vec2(5f, 5f), _screen);
        var bounds = MenuBounds.ForWheel(placed.Center, 56f, placed.Rings);
        Assert.True(bounds.X >= LayoutConstants.Margin - 0.01f);
        Assert.True(bounds.Y >= LayoutConstants.Margin - 0.01f);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10_000)]
    public void PlaceAuto_rejects_a_count_out_of_range(int count)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WheelLayout.PlaceAuto(count, 56f, _mouse, _screen));
    }

    [Fact]
    public void HitSubGrid_hits_cells_by_pitch_square_then_panel_then_outside()
    {
        var placed = SubMenuLayout.PlaceSubGrid(5, 56f, new Rect2(100f, 100f, 300f, 200f), 250f, new Vec2(300f, 150f), _screen, 200f);
        foreach (var c in placed.Cells)
        {
            Assert.Equal(Hit.Slot(c.Index), HitTester.HitSubGrid(c.Center, placed.Panel, placed.Cells, placed.Cells.Length, 56f));
        }

        var corner = new Vec2(placed.Panel.X + 1f, placed.Panel.Y + 1f);
        Assert.Equal(Hit.Inside, HitTester.HitSubGrid(corner, placed.Panel, placed.Cells, placed.Cells.Length, 56f));
        Assert.Equal(Hit.Outside, HitTester.HitSubGrid(new Vec2(placed.Panel.X - 50f, placed.Panel.Y), placed.Panel, placed.Cells, placed.Cells.Length, 56f));
        Assert.Equal(Hit.Outside, HitTester.HitSubGrid(new Vec2(float.NaN, 0f), placed.Panel, placed.Cells, placed.Cells.Length, 56f));
    }

    [Fact]
    public void HitSubGrid_ignores_cells_beyond_the_count()
    {
        var placed = SubMenuLayout.PlaceSubGrid(4, 56f, new Rect2(100f, 100f, 300f, 200f), 250f, new Vec2(300f, 150f), _screen, 200f);
        Assert.Equal(Hit.Inside, HitTester.HitSubGrid(placed.Cells[3].Center, placed.Panel, placed.Cells, 3, 56f));
    }

    [Fact]
    public void InCell_is_the_pitch_square()
    {
        var c = new Vec2(100f, 100f);
        Assert.True(HitTester.InCell(new Vec2(131f, 69f), c, 56f));
        Assert.False(HitTester.InCell(new Vec2(133f, 100f), c, 56f));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(10)]
    [InlineData(30)]
    public void SubGridWidth_is_the_placed_panel_width(int count)
    {
        var placed = SubMenuLayout.PlaceSubGrid(count, 56f, new Rect2(100f, 100f, 300f, 200f), 250f, new Vec2(300f, 150f), _screen, 200f);
        Assert.Equal(placed.Panel.Width, SubMenuLayout.SubGridWidth(count, 56f), 3);
    }

    [Fact]
    public void IsAutoFull_is_false_for_fixed_menus()
    {
        Assert.False(SettingsRules.IsAutoFull(MenuLayout.Wheel, SizingMode.Fixed, 500, 5, 56f, _screen, 30f));
        Assert.False(SettingsRules.IsAutoFull(MenuLayout.Grid, SizingMode.Fixed, 500, 5, 56f, _screen, 30f));
    }

    [Fact]
    public void IsAutoFull_wheel_follows_Capacity()
    {
        Assert.False(SettingsRules.IsAutoFull(MenuLayout.Wheel, SizingMode.Auto, 5, 5, 56f, _screen, 30f));
        Assert.Equal(
            Capacity.IsAutoWheelFull(18, 56f, new Vec2(500f, 300f), 30f),
            SettingsRules.IsAutoFull(MenuLayout.Wheel, SizingMode.Auto, 18, 5, 56f, new Vec2(500f, 300f), 30f));
        Assert.True(SettingsRules.IsAutoFull(MenuLayout.Wheel, SizingMode.Auto, 18, 5, 56f, new Vec2(500f, 300f), 30f));
    }

    [Fact]
    public void IsAutoFull_grid_follows_Capacity_with_the_panel_width_as_plate()
    {
        Assert.False(SettingsRules.IsAutoFull(MenuLayout.Grid, SizingMode.Auto, 3, 5, 56f, _screen, 30f));
        Assert.True(SettingsRules.IsAutoFull(MenuLayout.Grid, SizingMode.Auto, 14, 5, 56f, new Vec2(500f, 300f), 30f));
        Assert.False(SettingsRules.IsAutoFull(MenuLayout.Grid, SizingMode.Auto, 14, 5, 56f, _screen, 30f));
        Assert.True(SettingsRules.IsAutoFull(MenuLayout.Grid, SizingMode.Auto, 5 * 9 - 1, 5, 56f, new Vec2(8000f, 8000f), 30f));
    }
}
