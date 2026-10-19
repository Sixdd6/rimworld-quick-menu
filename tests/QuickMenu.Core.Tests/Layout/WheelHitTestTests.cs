using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;
using Xunit;

namespace QuickMenu.Core.Tests.Layout;

public class WheelHitTestTests
{
    private static SlotPlacement[] Wheel() => WheelLayout.Compute(new WheelParams(56f, 1, new Vec2(500f, 400f)));

    [Fact]
    public void Point_on_slot_centre_hits_that_slot()
    {
        var slots = Wheel();
        for (var i = 0; i < slots.Length; i++)
        {
            Assert.Equal(i, WheelHitTest.SlotAt(slots, slots.Length, slots[i].Center));
        }
    }

    [Fact]
    public void Hub_centre_and_far_points_hit_nothing()
    {
        var slots = Wheel();
        Assert.Equal(-1, WheelHitTest.SlotAt(slots, slots.Length, new Vec2(500f, 400f)));
        Assert.Equal(-1, WheelHitTest.SlotAt(slots, slots.Length, new Vec2(5000f, -5000f)));
    }

    [Fact]
    public void Hit_radius_is_inclusive_and_exclusive_beyond()
    {
        var slots = Wheel();
        var s = slots[0];
        Assert.Equal(0, WheelHitTest.SlotAt(slots, slots.Length, new Vec2(s.Center.X + s.HitRadius, s.Center.Y)));
        Assert.Equal(-1, WheelHitTest.SlotAt(slots, slots.Length, new Vec2(s.Center.X + s.HitRadius + 0.5f, s.Center.Y)));
    }

    [Fact]
    public void Count_limits_which_slots_are_tested()
    {
        var slots = Wheel();
        Assert.Equal(-1, WheelHitTest.SlotAt(slots, 2, slots[3].Center));
        Assert.Equal(1, WheelHitTest.SlotAt(slots, 2, slots[1].Center));
        Assert.Equal(-1, WheelHitTest.SlotAt(slots, 0, slots[0].Center));
    }

    [Fact]
    public void Overlapping_slots_choose_the_nearest()
    {
        var slots = new[]
        {
            new SlotPlacement(0, 1, new Vec2(0f, 0f), 0f, 0f, 10f),
            new SlotPlacement(1, 1, new Vec2(8f, 0f), 0f, 0f, 10f),
        };
        Assert.Equal(1, WheelHitTest.SlotAt(slots, 2, new Vec2(6f, 0f)));
        Assert.Equal(0, WheelHitTest.SlotAt(slots, 2, new Vec2(3f, 0f)));
    }
}
