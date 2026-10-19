using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;
using Xunit;

namespace QuickMenu.Core.Tests.Layout;

/// <summary>Boundary, gap, hub, translation and multi-ring checks for <see cref="WheelHitTest"/> (T-7, design.md 1.1).</summary>
public class WheelHitTestBoundaryTests
{
    private static SlotPlacement[] Wheel(int rings, float x = 500f, float y = 400f, float size = 56f) =>
        WheelLayout.Compute(new WheelParams(size, rings, new Vec2(x, y)));

    [Fact]
    public void Hit_radius_is_half_the_pitch()
    {
        foreach (SlotPlacement s in Wheel(3))
        {
            Assert.Equal(LayoutConstants.Pitch(56f) / 2f, s.HitRadius);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Points_in_each_axis_direction_flip_exactly_at_the_hit_radius(int axis)
    {
        // Single isolated slot so no neighbour interferes; exact binary fractions keep the float maths exact.
        var slot = new SlotPlacement(0, 1, new Vec2(100f, 200f), 0f, 0f, 32f);
        var slots = new[] { slot };
        (float dx, float dy) = axis switch { 0 => (1f, 0f), 1 => (-1f, 0f), 2 => (0f, 1f), _ => (0f, -1f) };
        Vec2 At(float r) => new(100f + (dx * r), 200f + (dy * r));

        Assert.Equal(0, WheelHitTest.SlotAt(slots, 1, At(31.9375f)));
        Assert.Equal(0, WheelHitTest.SlotAt(slots, 1, At(32f)));
        Assert.Equal(-1, WheelHitTest.SlotAt(slots, 1, At(32.0625f)));
    }

    [Fact]
    public void Pythagorean_point_exactly_on_the_circle_hits()
    {
        // 3-4-5 triangle scaled to radius 40: distance is exactly 40.
        var slots = new[] { new SlotPlacement(0, 1, new Vec2(0f, 0f), 0f, 0f, 40f) };
        Assert.Equal(0, WheelHitTest.SlotAt(slots, 1, new Vec2(24f, 32f)));
        Assert.Equal(0, WheelHitTest.SlotAt(slots, 1, new Vec2(-24f, -32f)));
        Assert.Equal(-1, WheelHitTest.SlotAt(slots, 1, new Vec2(24.5f, 32.5f)));
    }

    [Fact]
    public void Ring_one_neighbours_touch_so_their_midpoint_is_a_tie_resolved_to_the_lower_index()
    {
        // Ring 1 holds 6 slots exactly one pitch apart, so the midpoint is P/2 from both centres (the boundary).
        SlotPlacement[] slots = Wheel(1);
        Assert.Equal(6, slots.Length);
        for (var i = 0; i < slots.Length; i++)
        {
            SlotPlacement a = slots[i];
            SlotPlacement b = slots[(i + 1) % slots.Length];
            var mid = new Vec2((a.Center.X + b.Center.X) / 2f, (a.Center.Y + b.Center.Y) / 2f);
            int hit = WheelHitTest.SlotAt(slots, slots.Length, mid);
            Assert.True(hit == a.Index || hit == b.Index || hit == -1, "midpoint may only hit a neighbour or sit a float-ulp outside");

            // Nudging toward either neighbour is unambiguous.
            Assert.Equal(a.Index, WheelHitTest.SlotAt(slots, slots.Length, Toward(mid, a.Center, 1f)));
            Assert.Equal(b.Index, WheelHitTest.SlotAt(slots, slots.Length, Toward(mid, b.Center, 1f)));
        }
    }

    [Fact]
    public void Gap_between_widely_spaced_slots_hits_nothing()
    {
        // On outer rings neighbouring centres are further apart than two hit radii, leaving dead space.
        SlotPlacement[] slots = Wheel(5);
        var checkedGaps = 0;
        for (var i = 0; i < slots.Length; i++)
        {
            for (var j = i + 1; j < slots.Length; j++)
            {
                SlotPlacement a = slots[i];
                SlotPlacement b = slots[j];
                if (a.Ring != b.Ring || a.Center.Distance(b.Center) <= (2f * a.HitRadius) + 1f)
                {
                    continue;
                }

                var mid = new Vec2((a.Center.X + b.Center.X) / 2f, (a.Center.Y + b.Center.Y) / 2f);
                if (NearestDistance(slots, mid) > a.HitRadius + 0.5f)
                {
                    Assert.Equal(-1, WheelHitTest.SlotAt(slots, slots.Length, mid));
                    checkedGaps++;
                }
            }
        }

        Assert.True(checkedGaps > 0);
    }

    [Fact]
    public void Hub_centre_and_the_whole_hub_disc_hit_nothing_for_every_ring_count()
    {
        for (var rings = 1; rings <= 5; rings++)
        {
            SlotPlacement[] slots = Wheel(rings);
            Assert.Equal(-1, WheelHitTest.SlotAt(slots, slots.Length, new Vec2(500f, 400f)));
            // The hub is a button wide; slot discs start at pitch - hit radius = pitch / 2 from the centre.
            foreach ((float dx, float dy) in new[] { (28f, 0f), (-28f, 0f), (0f, 28f), (0f, -28f), (19.5f, 19.5f) })
            {
                Assert.Equal(-1, WheelHitTest.SlotAt(slots, slots.Length, new Vec2(500f + dx, 400f + dy)));
            }
        }
    }

    [Fact]
    public void Every_slot_of_a_five_ring_wheel_hits_itself_at_its_centre()
    {
        SlotPlacement[] slots = Wheel(5);
        Assert.Equal(RingCapacity.Total(5), slots.Length);
        foreach (SlotPlacement s in slots)
        {
            Assert.Equal(s.Index, WheelHitTest.SlotAt(slots, slots.Length, s.Center));
        }
    }

    [Fact]
    public void Slots_on_every_ring_keep_their_own_hit_zone_across_rings()
    {
        SlotPlacement[] slots = Wheel(5);
        foreach (SlotPlacement s in slots)
        {
            // Half a hit radius toward the hub stays inside this slot's disc and is nearer to it than to any other.
            var inward = new Vec2(
                s.Center.X + ((500f - s.Center.X) * (s.HitRadius * 0.5f / s.Radius)),
                s.Center.Y + ((400f - s.Center.Y) * (s.HitRadius * 0.5f / s.Radius)));
            Assert.Equal(s.Index, WheelHitTest.SlotAt(slots, slots.Length, inward));
        }
    }

    [Theory]
    [InlineData(-1000f, -250f)]
    [InlineData(0f, 0f)]
    [InlineData(4096.5f, 2160.25f)]
    public void Translating_the_centre_translates_the_hit_zones(float cx, float cy)
    {
        SlotPlacement[] origin = Wheel(3, 0f, 0f);
        SlotPlacement[] moved = Wheel(3, cx, cy);
        for (var i = 0; i < origin.Length; i++)
        {
            Assert.Equal(i, WheelHitTest.SlotAt(moved, moved.Length, moved[i].Center));
            Assert.Equal(-1, WheelHitTest.SlotAt(moved, moved.Length, new Vec2(cx, cy)));
            // Same relative offset gives the same answer as at the origin.
            var rel = new Vec2(origin[i].Center.X + 7f, origin[i].Center.Y - 5f);
            var abs = new Vec2(moved[i].Center.X + 7f, moved[i].Center.Y - 5f);
            Assert.Equal(WheelHitTest.SlotAt(origin, origin.Length, rel), WheelHitTest.SlotAt(moved, moved.Length, abs));
        }

        // A point at the old centre position is not a hit once the wheel has moved far away.
        if (cx != 0f || cy != 0f)
        {
            Assert.Equal(-1, WheelHitTest.SlotAt(moved, moved.Length, new Vec2(origin[0].Center.X, origin[0].Center.Y)));
        }
    }

    [Fact]
    public void Degenerate_inputs_hit_nothing_and_do_not_throw()
    {
        SlotPlacement[] slots = Wheel(1);
        Assert.Equal(-1, WheelHitTest.SlotAt(slots, -3, slots[0].Center));
        Assert.Equal(-1, WheelHitTest.SlotAt(System.Array.Empty<SlotPlacement>(), 5, new Vec2(0f, 0f)));
        Assert.Equal(-1, WheelHitTest.SlotAt(slots, slots.Length, new Vec2(float.NaN, 0f)));
        Assert.Equal(-1, WheelHitTest.SlotAt(slots, slots.Length, new Vec2(float.PositiveInfinity, float.NegativeInfinity)));
        // A count larger than the array is clamped to the array.
        Assert.Equal(5, WheelHitTest.SlotAt(slots, 1000, slots[5].Center));
    }

    private static Vec2 Toward(Vec2 from, Vec2 to, float distance)
    {
        float d = from.Distance(to);
        return new Vec2(from.X + ((to.X - from.X) * distance / d), from.Y + ((to.Y - from.Y) * distance / d));
    }

    private static float NearestDistance(SlotPlacement[] slots, Vec2 p)
    {
        var best = float.MaxValue;
        foreach (SlotPlacement s in slots)
        {
            best = System.MathF.Min(best, s.Center.Distance(p));
        }

        return best;
    }
}
