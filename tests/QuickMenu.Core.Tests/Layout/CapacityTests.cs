using System;

using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;
using Xunit;

namespace QuickMenu.Core.Tests.Layout;

public class CapacityTests
{
    private static readonly Vec2 _big = new(1920f, 1080f);
    private static readonly Vec2 _small = new(960f, 540f);

    [Fact]
    public void Fixed_capacity_is_ring_total_or_cells_minus_hub()
    {
        Assert.Equal(RingCapacity.Total(2), Capacity.FixedWheel(2));
        Assert.Equal(14, Capacity.FixedGrid(5, 3));
        Assert.Equal(2, Capacity.FixedGrid(3, 1));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    public void Auto_wheel_with_few_pins_uses_ring_1(int count, int rings)
    {
        Assert.Equal(rings, Capacity.AutoWheelRings(count));
    }

    [Fact]
    public void Auto_wheel_rings_are_the_smallest_that_hold_the_count()
    {
        for (var m = 1; m <= LayoutConstants.AutoMaxRings; m++)
        {
            Assert.Equal(m, Capacity.AutoWheelRings(RingCapacity.Total(m)));
            if (m > 1)
            {
                Assert.Equal(m, Capacity.AutoWheelRings(RingCapacity.Total(m - 1) + 1));
            }
        }
    }

    [Fact]
    public void Auto_wheel_rings_stop_at_the_maximum()
    {
        Assert.Equal(LayoutConstants.AutoMaxRings, Capacity.AutoWheelRings(RingCapacity.Total(8) + 50));
    }

    [Fact]
    public void Auto_wheel_is_not_full_below_ring_capacity()
    {
        Assert.False(Capacity.IsAutoWheelFull(0, 56f, _small));
        Assert.False(Capacity.IsAutoWheelFull(RingCapacity.Total(3) - 1, 56f, _small));
    }

    [Fact]
    public void Auto_wheel_at_540_is_full_at_three_rings_because_a_fourth_does_not_fit()
    {
        Assert.False(Capacity.IsAutoWheelFull(RingCapacity.Total(2), 56f, _small));
        Assert.True(Capacity.IsAutoWheelFull(RingCapacity.Total(3), 56f, _small));
    }

    [Fact]
    public void Auto_wheel_at_1080_is_full_at_seven_rings()
    {
        Assert.False(Capacity.IsAutoWheelFull(RingCapacity.Total(6), 56f, _big));
        Assert.True(Capacity.IsAutoWheelFull(RingCapacity.Total(7), 56f, _big));
    }

    [Fact]
    public void Auto_wheel_is_full_at_eight_rings_even_on_a_huge_screen()
    {
        var huge = new Vec2(8000f, 8000f);
        Assert.False(Capacity.IsAutoWheelFull(RingCapacity.Total(8) - 1, 56f, huge));
        Assert.True(Capacity.IsAutoWheelFull(RingCapacity.Total(8), 56f, huge));
        Assert.True(Capacity.IsAutoWheelFull(RingCapacity.Total(8) + 5, 56f, huge));
    }

    [Fact]
    public void Auto_wheel_full_depends_on_width_too()
    {
        var narrow = new Vec2(300f, 8000f);
        Assert.True(Capacity.IsAutoWheelFull(RingCapacity.Total(1), 56f, narrow));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(4, 1)]
    [InlineData(5, 3)]
    [InlineData(14, 3)]
    [InlineData(15, 5)]
    [InlineData(24, 5)]
    [InlineData(25, 7)]
    [InlineData(34, 7)]
    [InlineData(35, 9)]
    [InlineData(44, 9)]
    [InlineData(500, 9)]
    public void Auto_grid_rows_with_five_columns(int count, int rows)
    {
        // 5 columns: rows 1,3,5,7,9 hold 4,14,24,34,44.
        Assert.Equal(rows, Capacity.AutoGridRows(count, 5));
    }

    [Fact]
    public void Auto_grid_rows_reject_even_columns()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Capacity.AutoGridRows(3, 4));
    }

    [Fact]
    public void Auto_grid_at_1080_is_full_only_at_nine_rows()
    {
        Assert.False(Capacity.IsAutoGridFull(34, 5, 56f, _big, 200f));
        Assert.False(Capacity.IsAutoGridFull(43, 5, 56f, _big, 200f));
        Assert.True(Capacity.IsAutoGridFull(44, 5, 56f, _big, 200f));
        Assert.True(Capacity.IsAutoGridFull(60, 5, 56f, _big, 200f));
    }

    [Fact]
    public void Auto_grid_at_540_is_full_at_seven_rows()
    {
        Assert.False(Capacity.IsAutoGridFull(24, 5, 56f, _small, 200f));
        Assert.False(Capacity.IsAutoGridFull(33, 5, 56f, _small, 200f));
        Assert.True(Capacity.IsAutoGridFull(34, 5, 56f, _small, 200f));
    }

    [Fact]
    public void Auto_grid_is_full_when_a_wide_plate_makes_the_next_rows_too_wide()
    {
        Assert.True(Capacity.IsAutoGridFull(8, 3, 56f, new Vec2(400f, 4000f), 500f));
    }

    [Fact]
    public void Compute_spread_matches_fixed_layout_when_rings_are_full()
    {
        var total = RingCapacity.Total(2);
        var spread = new SlotPlacement[total];
        WheelLayout.ComputeSpread(total, 56f, new Vec2(10f, 20f), spread);
        var fixedSlots = WheelLayout.Compute(new WheelParams(56f, 2, new Vec2(10f, 20f)));
        Assert.Equal(fixedSlots, spread);
    }

    [Fact]
    public void Compute_spread_spreads_the_outer_ring_evenly_and_zero_writes_nothing()
    {
        var slots = new SlotPlacement[RingCapacity.Of(1) + 3];
        WheelLayout.ComputeSpread(slots.Length, 56f, default, slots);
        Assert.Equal(2, slots[^1].Ring);
        Assert.Equal(-90f, slots[RingCapacity.Of(1)].AngleDegrees);
        Assert.Equal(30f, slots[RingCapacity.Of(1) + 1].AngleDegrees, 3);
        WheelLayout.ComputeSpread(0, 56f, default, Span<SlotPlacement>.Empty);
    }

    [Fact]
    public void Compute_spread_validates_input()
    {
        var one = new SlotPlacement[1];
        Assert.Throws<ArgumentOutOfRangeException>(() => WheelLayout.ComputeSpread(-1, 56f, default, one));
        Assert.Throws<ArgumentOutOfRangeException>(() => WheelLayout.ComputeSpread(1, 0f, default, one));
        Assert.Throws<ArgumentException>(() => WheelLayout.ComputeSpread(2, 56f, default, one));
    }
}
