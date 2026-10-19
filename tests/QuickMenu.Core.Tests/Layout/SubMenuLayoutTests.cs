using System;
using System.Linq;

using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;
using Xunit;

namespace QuickMenu.Core.Tests.Layout;

public class SubMenuLayoutTests
{
    private const float B = 56f;
    private static readonly float _p = LayoutConstants.Pitch(B);
    private static readonly Vec2 _screen = new(1920f, 1080f);

    [Theory]
    [InlineData(0, SubMenuOutcome.None)]
    [InlineData(-3, SubMenuOutcome.None)]
    [InlineData(1, SubMenuOutcome.Skip)]
    [InlineData(2, SubMenuOutcome.Open)]
    [InlineData(60, SubMenuOutcome.Open)]
    public void Classify_maps_counts(int n, SubMenuOutcome expected) => Assert.Equal(expected, SubMenuLayout.Classify(n));

    [Fact]
    public void RingsFor_is_smallest_m_covering_n()
    {
        for (var n = 1; n <= 60; n++)
        {
            var m = SubMenuLayout.RingsFor(n);
            Assert.True(RingCapacity.Total(m) >= n);
            Assert.True(m == 1 || RingCapacity.Total(m - 1) < n);
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => SubMenuLayout.RingsFor(0));
    }

    [Fact]
    public void Satellite_counts_1_to_60_fill_inner_rings_and_spread_outer()
    {
        var c = new Vec2(900f, 500f);
        for (var n = 1; n <= 60; n++)
        {
            var s = SubMenuLayout.PlaceSatellite(n, B, c, _screen);
            Assert.Equal(n, s.Slots.Length);
            Assert.Equal(SubMenuLayout.RingsFor(n), s.Rings);
            Assert.Equal(LayoutConstants.DiscRadius(B, s.Rings), s.DiscRadius);
            for (var k = 1; k < s.Rings; k++)
            {
                Assert.Equal(RingCapacity.Of(k), s.Slots.Count(x => x.Ring == k));
            }

            var outer = s.Slots.Where(x => x.Ring == s.Rings).ToArray();
            Assert.Equal(n - (s.Rings > 1 ? RingCapacity.Total(s.Rings - 1) : 0), outer.Length);
            Assert.Equal(-90f, outer[0].AngleDegrees, 3);
            for (var i = 0; i < outer.Length; i++)
            {
                Assert.Equal(-90f + (i * 360f / outer.Length), outer[i].AngleDegrees, 3);
                Assert.Equal(outer[i].Radius, outer[i].Center.Distance(s.Hub), 1);
            }

            for (var i = 0; i < n; i++)
            {
                Assert.Equal(i, s.Slots[i].Index);
            }
        }
    }

    [Fact]
    public void Satellite_slots_keep_one_pitch_apart()
    {
        var s = SubMenuLayout.PlaceSatellite(60, B, new Vec2(960f, 540f), _screen);
        for (var i = 0; i < s.Slots.Length; i++)
        {
            for (var j = i + 1; j < s.Slots.Length; j++)
            {
                Assert.True(s.Slots[i].Center.Distance(s.Slots[j].Center) >= _p - 0.01f);
            }
        }
    }

    [Fact]
    public void Satellite_uncramped_is_centred_on_parent()
    {
        var parent = new Vec2(900f, 500f);
        var s = SubMenuLayout.PlaceSatellite(8, B, parent, _screen);
        Assert.Equal(parent, s.Hub);
    }

    [Theory]
    [InlineData(2f, 540f)]
    [InlineData(1918f, 540f)]
    [InlineData(960f, 2f)]
    [InlineData(960f, 1078f)]
    [InlineData(1f, 1f)]
    [InlineData(1919f, 1079f)]
    public void Satellite_at_screen_edges_fits_and_hub_moves_with_it(float x, float y)
    {
        foreach (var n in new[] { 2, 7, 20, 60 })
        {
            var parent = new Vec2(x, y);
            var s = SubMenuLayout.PlaceSatellite(n, B, parent, _screen);
            var b = s.Bounds;
            Assert.True(b.X >= LayoutConstants.Margin - 0.01f, $"left n={n}");
            Assert.True(b.Y >= LayoutConstants.Margin - 0.01f, $"top n={n}");
            Assert.True(b.Right <= _screen.X - LayoutConstants.Margin + 0.01f, $"right n={n}");
            Assert.True(b.Bottom <= _screen.Y - LayoutConstants.Margin + 0.01f, $"bottom n={n}");
            Assert.Equal(s.Hub.X, b.X + (b.Width / 2f), 2);
            foreach (var slot in s.Slots)
            {
                Assert.Equal(slot.Radius, slot.Center.Distance(s.Hub), 1);
            }
        }
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(4, 2)]
    [InlineData(5, 3)]
    [InlineData(9, 3)]
    [InlineData(10, 4)]
    [InlineData(16, 4)]
    [InlineData(17, 5)]
    [InlineData(25, 5)]
    [InlineData(26, 6)]
    [InlineData(36, 6)]
    [InlineData(60, 6)]
    public void SubGridColumns_is_min_ceil_sqrt_6(int n, int cols) => Assert.Equal(cols, SubMenuLayout.SubGridColumns(n));

    private static readonly Rect2 _main = new(560f, 300f, 336f, 208f);
    private const float Cx = 728f;

    [Fact]
    public void SubGrid_right_side_first_row_at_parent_y_reading_order()
    {
        var parent = new Vec2(800f, 400f);
        var g = SubMenuLayout.PlaceSubGrid(7, B, _main, Cx, parent, _screen, 120f);
        Assert.True(g.OnRight);
        Assert.Equal(3, g.Columns);
        Assert.Equal(3, g.Rows);
        Assert.Equal(_main.Right + LayoutConstants.Gap, g.Panel.X);
        Assert.Equal(parent.Y, g.Cells[0].Center.Y, 3);
        Assert.Equal(parent.Y, g.Cells[2].Center.Y, 3);
        Assert.Equal(parent.Y + _p, g.Cells[3].Center.Y, 3);
        Assert.Equal(g.Cells[0].Center.X + _p, g.Cells[1].Center.X, 3);
        Assert.Equal(g.Cells[0].Center.X, g.Cells[3].Center.X, 3);
        Assert.Equal((3 * _p) - LayoutConstants.Gap + 24f, g.Panel.Width, 3);
        Assert.Equal(7, g.Cells.Length);
    }

    [Fact]
    public void SubGrid_centre_column_counts_as_right_and_left_otherwise()
    {
        Assert.True(SubMenuLayout.PlaceSubGrid(4, B, _main, Cx, new Vec2(Cx, 400f), _screen, 120f).OnRight);
        var left = SubMenuLayout.PlaceSubGrid(4, B, _main, Cx, new Vec2(Cx - 1f, 400f), _screen, 120f);
        Assert.False(left.OnRight);
        Assert.Equal(_main.X - LayoutConstants.Gap, left.Panel.Right, 3);
    }

    [Fact]
    public void SubGrid_switches_side_when_preferred_overflows()
    {
        var main = new Rect2(1500f, 300f, 336f, 208f);
        var g = SubMenuLayout.PlaceSubGrid(16, B, main, 1668f, new Vec2(1700f, 400f), _screen, 120f);
        Assert.False(g.OnRight);

        var mainL = new Rect2(40f, 300f, 336f, 208f);
        var g2 = SubMenuLayout.PlaceSubGrid(16, B, mainL, 208f, new Vec2(100f, 400f), _screen, 120f);
        Assert.True(g2.OnRight);
    }

    [Fact]
    public void SubGrid_dodges_vertical_edges_and_keeps_cells_in_panel()
    {
        foreach (var py in new[] { 4f, 540f, 1076f })
        {
            foreach (var n in new[] { 2, 9, 36, 60 })
            {
                var g = SubMenuLayout.PlaceSubGrid(n, B, _main, Cx, new Vec2(800f, py), _screen, 120f);
                Assert.True(g.Bounds.Y >= LayoutConstants.Margin - 0.01f);
                Assert.True(g.Bounds.Bottom <= _screen.Y - LayoutConstants.Margin + 0.01f);
                Assert.True(g.Bounds.X >= LayoutConstants.Margin - 0.01f);
                Assert.True(g.Bounds.Right <= _screen.X - LayoutConstants.Margin + 0.01f);
                foreach (var c in g.Cells)
                {
                    Assert.True(g.Panel.Contains(c.Center));
                }
            }
        }
    }

    [Fact]
    public void Material_ring_replaces_sub_menu_at_same_anchor()
    {
        var parent = new Vec2(900f, 500f);
        var sub = SubMenuLayout.PlaceSatellite(8, B, parent, _screen);
        var mat = SubMenuLayout.PlaceSatellite(3, B, parent, _screen);
        Assert.Equal(sub.Hub, mat.Hub);
        Assert.Equal(SubMenuOutcome.Skip, SubMenuLayout.Classify(1));
        Assert.Equal(SubMenuOutcome.None, SubMenuLayout.Classify(0));
    }

    [Fact]
    public void Invalid_inputs_throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SubMenuLayout.PlaceSatellite(3, 0f, default, _screen));
        Assert.Throws<ArgumentOutOfRangeException>(() => SubMenuLayout.PlaceSatellite(0, B, default, _screen));
        Assert.Throws<ArgumentException>(() => SubMenuLayout.ComputeSatelliteSlots(5, B, default, new SlotPlacement[2]));
        Assert.Throws<ArgumentOutOfRangeException>(() => SubMenuLayout.SubGridColumns(0));
    }
}
