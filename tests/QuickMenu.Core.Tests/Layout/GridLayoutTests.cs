using System;

using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;
using Xunit;

namespace QuickMenu.Core.Tests.Layout;

public class GridLayoutTests
{
    private static readonly Vec2 _c = new(500f, 400f);

    [Fact]
    public void Five_by_three_at_56_has_336_by_208_panel_centred_on_the_hub()
    {
        var p = new GridParams(56f, 5, 3, _c);
        var panel = GridLayout.Panel(p);
        Assert.Equal(336f, panel.Width);
        Assert.Equal(208f, panel.Height);
        Assert.Equal(_c.X, panel.X + (panel.Width / 2f));
        Assert.Equal(_c.Y, panel.Y + (panel.Height / 2f));
    }

    [Fact]
    public void Hub_is_cell_1_2_and_slots_are_14_in_reading_order_skipping_it()
    {
        var p = new GridParams(56f, 5, 3, _c);
        Assert.Equal(1, GridLayout.HubRow(p.Rows));
        Assert.Equal(2, GridLayout.HubColumn(p.Columns));
        Assert.Equal(14, GridLayout.SlotCount(p));
        var slots = GridLayout.Compute(p);
        Assert.Equal(14, slots.Length);
        Assert.Equal(new Vec2(500f - 128f, 400f - 64f), slots[0].Center);
        Assert.Equal(new Vec2(500f + 128f, 400f - 64f), slots[4].Center);
        Assert.Equal(new Vec2(500f - 128f, 400f), slots[5].Center);
        Assert.Equal(new Vec2(500f - 64f, 400f), slots[6].Center);
        Assert.Equal(new Vec2(500f + 64f, 400f), slots[7].Center);
        Assert.Equal(new Vec2(500f + 128f, 400f + 64f), slots[13].Center);
        for (var i = 0; i < slots.Length; i++)
        {
            Assert.Equal(i, slots[i].Index);
            Assert.Equal(32f, slots[i].HitRadius);
            Assert.NotEqual(_c, slots[i].Center);
        }
    }

    [Fact]
    public void Hub_cell_is_the_pitch_square_at_the_centre()
    {
        var r = GridLayout.HubCell(new GridParams(56f, 5, 3, _c));
        Assert.Equal(new Rect2(468f, 368f, 64f, 64f), r);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(9)]
    [InlineData(11)]
    public void Valid_columns_are_odd_3_to_11(int cols) => Assert.True(GridLayout.IsValidColumns(cols));

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(13)]
    [InlineData(-3)]
    public void Invalid_columns(int cols) => Assert.False(GridLayout.IsValidColumns(cols));

    [Theory]
    [InlineData(1, true, true)]
    [InlineData(3, true, true)]
    [InlineData(7, true, true)]
    [InlineData(9, false, true)]
    [InlineData(2, false, false)]
    [InlineData(11, false, false)]
    public void Rows_fixed_1_to_7_auto_up_to_9(int rows, bool fixedOk, bool autoOk)
    {
        Assert.Equal(fixedOk, GridLayout.IsValidFixedRows(rows));
        Assert.Equal(autoOk, GridLayout.IsValidRows(rows));
    }

    [Fact]
    public void One_row_grid_has_hub_and_cols_minus_one_slots()
    {
        var p = new GridParams(56f, 3, 1, _c);
        var slots = GridLayout.Compute(p);
        Assert.Equal(2, slots.Length);
        Assert.Equal(new Vec2(436f, 400f), slots[0].Center);
        Assert.Equal(new Vec2(564f, 400f), slots[1].Center);
    }

    [Theory]
    [InlineData(0, 3, 56f)]
    [InlineData(4, 3, 56f)]
    [InlineData(5, 2, 56f)]
    [InlineData(5, 11, 56f)]
    [InlineData(5, 3, 0f)]
    [InlineData(5, 3, float.NaN)]
    public void Invalid_params_throw(int cols, int rows, float b)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GridLayout.Compute(new GridParams(b, cols, rows, _c)));
    }

    [Fact]
    public void Non_finite_centre_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            GridLayout.Compute(new GridParams(56f, 5, 3, new Vec2(float.NaN, 0f))));
    }

    [Fact]
    public void Place_edge_dodges_panel_and_plate_and_moves_the_hub()
    {
        var placed = GridLayout.Place(56f, 5, 3, new Vec2(10f, 10f), new Vec2(1920f, 1080f), 100f);
        Assert.True(placed.Panel.X >= LayoutConstants.Margin);
        Assert.True(placed.Panel.Y >= LayoutConstants.Margin);
        Assert.Equal(placed.Center.X, placed.Panel.X + (placed.Panel.Width / 2f), 3);
        Assert.Equal(14, placed.Slots.Length);
        Assert.Equal(placed.Bounds, MenuBounds.ForPanel(placed.Panel, 100f));
    }

    [Fact]
    public void Place_keeps_position_when_it_fits()
    {
        var placed = GridLayout.Place(56f, 5, 3, new Vec2(960f, 540f), new Vec2(1920f, 1080f), 100f);
        Assert.Equal(new Vec2(960f, 540f), placed.Center);
    }
}
