using System;
using System.Linq;
using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;
using Xunit;

namespace QuickMenu.Tests.Layout;

public class WheelLayoutTests
{
    private const float Tol = 1e-3f;

    private static void AssertAt(SlotPlacement s, Vec2 c, float radius, double degrees)
    {
        var rad = degrees * Math.PI / 180.0;
        Assert.Equal(c.X + (radius * Math.Cos(rad)), s.Center.X, Tol);
        Assert.Equal(c.Y + (radius * Math.Sin(rad)), s.Center.Y, Tol);
        Assert.Equal(radius, s.Radius, Tol);
    }

    [Fact]
    public void WorkedDefaults_B56_TwoRings()
    {
        var c = new Vec2(500f, 400f);
        var slots = WheelLayout.Compute(new WheelParams(56f, 2, c));

        Assert.Equal(18, slots.Length);
        var angles1 = new[] { -90.0, -30.0, 30.0, 90.0, 150.0, 210.0 };
        for (var i = 0; i < 6; i++)
        {
            Assert.Equal(i, slots[i].Index);
            Assert.Equal(1, slots[i].Ring);
            AssertAt(slots[i], c, 64f, angles1[i]);
        }

        for (var i = 0; i < 12; i++)
        {
            Assert.Equal(6 + i, slots[6 + i].Index);
            Assert.Equal(2, slots[6 + i].Ring);
            AssertAt(slots[6 + i], c, 128f, -90.0 + (i * 30.0));
        }

        Assert.Equal(168f, LayoutConstants.DiscRadius(56f, 2));
    }

    [Fact]
    public void FirstSlotIsAtTwelveOClock()
    {
        var slots = WheelLayout.Compute(new WheelParams(56f, 1, new Vec2(0f, 0f)));
        Assert.Equal(0f, slots[0].Center.X, Tol);
        Assert.Equal(-64f, slots[0].Center.Y, Tol);
    }

    [Fact]
    public void AllButtonSizesAndRings_KeepPitchAndCapacity()
    {
        var expectedCapacity = new[] { 6, 18, 36, 61, 92 };
        for (var b = 36; b <= 96; b += 4)
        {
            var pitch = b + 8f;
            for (var rings = 1; rings <= 5; rings++)
            {
                var slots = WheelLayout.Compute(new WheelParams(b, rings, new Vec2(100f, 100f)));
                Assert.Equal(expectedCapacity[rings - 1], slots.Length);

                foreach (var ring in slots.GroupBy(s => s.Ring))
                {
                    var list = ring.ToArray();
                    for (var i = 0; i < list.Length; i++)
                    {
                        var next = list[(i + 1) % list.Length];
                        Assert.True(list[i].Center.Distance(next.Center) >= pitch - Tol, $"B={b} rings={rings} ring={ring.Key} i={i}");
                    }
                }

                for (var i = 0; i < slots.Length; i++)
                {
                    for (var j = i + 1; j < slots.Length; j++)
                    {
                        Assert.True(slots[i].Center.Distance(slots[j].Center) >= b - Tol);
                    }
                }
            }
        }
    }

    [Theory]
    [InlineData(36, 23, 12)]
    [InlineData(56, 36, 18)]
    [InlineData(96, 61, 31)]
    public void Hub_diameter_and_glyph_round_half_away_from_zero(int b, float h, float hg)
    {
        Assert.Equal(h, LayoutConstants.HubDiameter(b));
        Assert.Equal(hg, LayoutConstants.HubGlyphSize(b));
    }

    [Fact]
    public void Hub_is_smaller_than_a_slot_and_its_glyph_is_half_of_it_at_every_step()
    {
        for (var b = 36; b <= 96; b += 4)
        {
            var h = LayoutConstants.HubDiameter(b);
            Assert.True(h < b);
            Assert.True(h <= LayoutConstants.Pitch(b));
            Assert.Equal(MathF.Round(0.5f * h, MidpointRounding.AwayFromZero), LayoutConstants.HubGlyphSize(b));
        }
    }

    [Fact]
    public void Compute_RejectsInvalidParams()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WheelLayout.Compute(new WheelParams(56f, 0, default)));
        Assert.Throws<ArgumentOutOfRangeException>(() => WheelLayout.Compute(new WheelParams(0f, 2, default)));
    }

    [Fact]
    public void Vec2AndRect2_Basics()
    {
        Assert.Equal(new Vec2(4f, 6f), new Vec2(1f, 2f) + new Vec2(3f, 4f));
        Assert.Equal(new Vec2(-2f, -2f), new Vec2(1f, 2f) - new Vec2(3f, 4f));
        var r = new Rect2(10f, 20f, 30f, 40f);
        Assert.Equal(40f, r.Right);
        Assert.Equal(60f, r.Bottom);
        Assert.True(r.Contains(new Vec2(10f, 60f)));
        Assert.False(r.Contains(new Vec2(9f, 30f)));
    }
}
