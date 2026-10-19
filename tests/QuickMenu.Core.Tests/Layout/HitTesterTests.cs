using System;

using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;
using Xunit;

namespace QuickMenu.Core.Tests.Layout;

public class HitTesterTests
{
    private static readonly Vec2 _c = new(500f, 400f);

    private static HitLayout Make(float b, int rings = 2, Rect2? plate = null, HitButton[]? buttons = null)
    {
        var slots = WheelLayout.Compute(new WheelParams(b, rings, _c));
        return new HitLayout(_c, b, LayoutConstants.DiscRadius(b, rings), slots, slots.Length, plate, buttons, buttons?.Length ?? 0);
    }

    [Fact]
    public void Slot_centres_and_edge_hit_that_slot()
    {
        var l = Make(56f);
        foreach (var s in l.Slots)
        {
            Assert.Equal(Hit.Slot(s.Index), HitTester.Hit(s.Center, l));
            Assert.Equal(Hit.Slot(s.Index), HitTester.Hit(new Vec2(s.Center.X, s.Center.Y - 28f), l));
        }
    }

    [Fact]
    public void Exact_boundary_between_slots_picks_lower_index()
    {
        var slots = new[]
        {
            new SlotPlacement(0, 1, new Vec2(100f, 0f), 100f, 0f, 4f),
            new SlotPlacement(1, 1, new Vec2(108f, 0f), 100f, 0f, 4f),
        };
        var l = new HitLayout(new Vec2(0f, 0f), 56f, 300f, slots, 2);
        Assert.Equal(Hit.Slot(0), HitTester.Hit(new Vec2(104f, 0f), l));
    }

    [Theory]
    [InlineData(36)]
    [InlineData(40)]
    [InlineData(56)]
    [InlineData(72)]
    [InlineData(96)]
    public void Hub_is_one_target_out_to_half_a_pitch(int b)
    {
        var l = Make(b);
        var r = LayoutConstants.Pitch(b) / 2f;
        Assert.Equal(Hit.HubEdit, HitTester.Hit(_c, l));
        Assert.Equal(Hit.HubEdit, HitTester.Hit(new Vec2(_c.X, _c.Y - r), l));
        Assert.Equal(Hit.HubEdit, HitTester.Hit(new Vec2(_c.X, _c.Y + r), l));
        Assert.Equal(Hit.HubEdit, HitTester.Hit(new Vec2(_c.X - r, _c.Y), l));
        Assert.Equal(Hit.HubEdit, HitTester.Hit(new Vec2(_c.X + r, _c.Y), l));
        Assert.NotEqual(HitKind.HubEdit, HitTester.Hit(new Vec2(_c.X, _c.Y + r + 0.5f), l).Kind);
        Assert.NotEqual(HitKind.HubEdit, HitTester.Hit(new Vec2(_c.X + r + 0.5f, _c.Y), l).Kind);
    }

    [Fact]
    public void Defaults_hub_hit_area_is_64_across_and_larger_than_the_drawn_36()
    {
        var l = Make(56f);
        Assert.Equal(Hit.HubEdit, HitTester.Hit(new Vec2(_c.X, _c.Y + 32f), l));
        Assert.NotEqual(HitKind.HubEdit, HitTester.Hit(new Vec2(_c.X, _c.Y + 32.5f), l).Kind);
        Assert.True(32f > LayoutConstants.HubDiameter(56f) / 2f);
    }

    [Theory]
    [InlineData(36)]
    [InlineData(56)]
    [InlineData(96)]
    public void Hub_is_never_a_slot(int b)
    {
        var l = Make(b);
        for (var dx = -60; dx <= 60; dx += 3)
        {
            for (var dy = -60; dy <= 60; dy += 3)
            {
                var h = HitTester.Hit(new Vec2(_c.X + dx, _c.Y + dy), l);
                if (Math.Sqrt((dx * dx) + (dy * dy)) <= LayoutConstants.Pitch(b) / 2f - 0.01f)
                {
                    Assert.NotEqual(HitKind.Slot, h.Kind);
                }
            }
        }
    }

    [Fact]
    public void Buttons_beat_slots_but_the_hub_beats_nothing_it_does_not_overlap()
    {
        var slot0 = WheelLayout.Compute(new WheelParams(56f, 1, _c))[0].Center;
        var buttons = new[]
        {
            new HitButton(new Rect2(slot0.X - 10f, slot0.Y - 10f, 20f, 20f), 7),
            new HitButton(new Rect2(_c.X + 22f, _c.Y - 5f, 8f, 10f), 9),
        };
        var l = Make(56f, 1, null, buttons);
        Assert.Equal(Hit.Button(7), HitTester.Hit(slot0, l));
        Assert.Equal(Hit.HubEdit, HitTester.Hit(new Vec2(_c.X, _c.Y + 15f), l));
        Assert.Equal(Hit.Button(9), HitTester.Hit(new Vec2(_c.X + 26f, _c.Y), l));
    }

    [Fact]
    public void Button_over_the_hub_area_beats_the_hub()
    {
        var buttons = new[] { new HitButton(new Rect2(_c.X - 25f, _c.Y - 25f, 10f, 10f), 3) };
        var l = Make(56f, 1, null, buttons);
        Assert.Equal(Hit.Button(3), HitTester.Hit(new Vec2(_c.X - 20f, _c.Y - 20f), l));
    }

    [Fact]
    public void Plate_inside_and_outside()
    {
        var plate = new Rect2(_c.X - 50f, _c.Y + 200f, 100f, 30f);
        var l = Make(56f, 2, plate);
        Assert.Equal(Hit.Plate, HitTester.Hit(new Vec2(_c.X, _c.Y + 215f), l));
        Assert.Equal(Hit.Outside, HitTester.Hit(new Vec2(_c.X, _c.Y + 240f), l));
        // gap between two ring-1 slots inside the disc
        var gap = new Vec2(_c.X + 90f, _c.Y);
        Assert.Equal(HitKind.Inside, HitTester.Hit(gap, l).Kind);
        Assert.Equal(Hit.Outside, HitTester.Hit(new Vec2(_c.X + 400f, _c.Y), l));
        Assert.Equal(Hit.Outside, HitTester.Hit(new Vec2(float.NaN, 0f), l));
    }

    [Fact]
    public void Min_button_size_keeps_the_six_oclock_slot_area_outside_the_hub()
    {
        const float b = 36f;
        var slots = new[] { new SlotPlacement(0, 1, new Vec2(_c.X, _c.Y + LayoutConstants.Pitch(b)), LayoutConstants.Pitch(b), 90f, LayoutConstants.Pitch(b) / 2f) };
        var l = new HitLayout(_c, b, 100f, slots, 1);
        Assert.Equal(Hit.HubEdit, HitTester.Hit(new Vec2(_c.X, _c.Y + 21f), l));
        Assert.Equal(Hit.Slot(0), HitTester.Hit(new Vec2(_c.X, _c.Y + 23f), l));
    }
}
