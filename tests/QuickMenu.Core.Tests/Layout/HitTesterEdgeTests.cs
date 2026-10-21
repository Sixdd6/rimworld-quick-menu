using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;
using Xunit;

namespace QuickMenu.Core.Tests.Layout;

public class HitTesterEdgeTests
{
    private static readonly Vec2 _c = new(300f, 300f);

    private static HitLayout Make(float b, int rings, Rect2? plate = null, HitButton[]? buttons = null)
    {
        var slots = WheelLayout.Compute(new WheelParams(b, rings, _c));
        return new HitLayout(_c, b, LayoutConstants.DiscRadius(b, rings), slots, slots.Length, plate, buttons, buttons?.Length ?? 0);
    }

    [Theory]
    [InlineData(36, 1)]
    [InlineData(36, 5)]
    [InlineData(96, 1)]
    [InlineData(96, 3)]
    public void Extremes_every_slot_centre_hits_itself_and_hub_is_edit(int b, int rings)
    {
        var l = Make(b, rings);
        foreach (var s in l.Slots)
        {
            Assert.Equal(Hit.Slot(s.Index), HitTester.Hit(s.Center, l));
        }

        Assert.Equal(Hit.HubEdit, HitTester.Hit(new Vec2(_c.X, _c.Y - 3f), l));
        Assert.Equal(Hit.HubEdit, HitTester.Hit(new Vec2(_c.X, _c.Y + 3f), l));
    }

    [Theory]
    [InlineData(36)]
    [InlineData(96)]
    public void Disc_rim_is_inside_just_past_it_is_outside(int b)
    {
        var l = Make(b, 2);
        float d = l.DiscRadius;
        Assert.Equal(HitKind.Outside, HitTester.Hit(new Vec2(_c.X + d + 0.01f, _c.Y), l).Kind);
        Assert.NotEqual(HitKind.Outside, HitTester.Hit(new Vec2(_c.X + d - 0.5f, _c.Y), l).Kind);
        Assert.Equal(HitKind.Outside, HitTester.Hit(new Vec2(_c.X - d - 0.01f, _c.Y), l).Kind);
    }

    [Theory]
    [InlineData(36)]
    [InlineData(56)]
    [InlineData(96)]
    public void Plate_and_extra_buttons_together_keep_their_priorities(int b)
    {
        float d = LayoutConstants.DiscRadius(b, 2);
        var plate = new Rect2(_c.X - 60f, _c.Y + d + LayoutConstants.PlateGap, 120f, LayoutConstants.PlateHeight);
        var buttons = new[]
        {
            new HitButton(new Rect2(_c.X + d - 10f, _c.Y - 10f, 20f, 20f), 4),
            new HitButton(new Rect2(plate.X + 100f, plate.Y, 20f, 30f), 5),
        };
        var l = Make(b, 2, plate, buttons);

        Assert.Equal(Hit.Button(4), HitTester.Hit(new Vec2(_c.X + d + 5f, _c.Y), l));
        Assert.Equal(Hit.Button(5), HitTester.Hit(new Vec2(plate.X + 110f, plate.Y + 15f), l));
        Assert.Equal(Hit.Plate, HitTester.Hit(new Vec2(plate.X + 10f, plate.Y + 15f), l));
        Assert.Equal(Hit.Outside, HitTester.Hit(new Vec2(plate.X - 20f, plate.Y + 15f), l));
    }

    [Fact]
    public void Button_count_beyond_array_and_null_array_are_safe()
    {
        var slots = WheelLayout.Compute(new WheelParams(56f, 1, _c));
        var one = new[] { new HitButton(new Rect2(0f, 0f, 10f, 10f), 1) };
        var l = new HitLayout(_c, 56f, 100f, slots, slots.Length, null, one, 99);
        Assert.Equal(Hit.Button(1), HitTester.Hit(new Vec2(5f, 5f), l));
        var l2 = new HitLayout(_c, 56f, 100f, slots, slots.Length, null, null, 5);
        Assert.Equal(Hit.Outside, HitTester.Hit(new Vec2(5f, 5f), l2));
    }

    [Fact]
    public void Infinite_points_are_outside()
    {
        var l = Make(56f, 2);
        Assert.Equal(Hit.Outside, HitTester.Hit(new Vec2(float.PositiveInfinity, 0f), l));
        Assert.Equal(Hit.Outside, HitTester.Hit(new Vec2(0f, float.NaN), l));
    }

    [Fact]
    public void Zero_slots_gives_only_hub_inside_and_outside()
    {
        var l = Make(56f, 1);
        l = l with { SlotCount = 0 };
        Assert.Equal(Hit.HubEdit, HitTester.Hit(new Vec2(_c.X, _c.Y - 5f), l));
        Assert.Equal(HitKind.Inside, HitTester.Hit(new Vec2(_c.X + 64f, _c.Y), l).Kind);
    }
}
