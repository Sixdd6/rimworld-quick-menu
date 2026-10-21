using System;
using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;
using Xunit;

namespace QuickMenu.Tests.Layout;

/// <summary>Edge and parameter-extreme checks for design.md section 1.1 beyond the worked defaults.</summary>
public class WheelLayoutExtremesTests
{
    private const float Tol = 1e-3f;

    [Fact]
    public void RingCapacity_IsMaximalAndSafe_ForManyRings()
    {
        // cap(k) is the largest n with chord 2 k sin(pi/n) >= 1 (in pitch units); cap(k)+1 must break it.
        for (var k = 1; k <= 2000; k++)
        {
            var cap = RingCapacity.Of(k);
            Assert.True(2.0 * k * Math.Sin(Math.PI / cap) >= 1.0 - 1e-9, $"k={k} cap={cap} too many");
            Assert.True(2.0 * k * Math.Sin(Math.PI / (cap + 1)) < 1.0, $"k={k} cap={cap} not maximal");
        }
    }

    [Fact]
    public void RingCapacity_IsStrictlyIncreasing()
    {
        var prev = 0;
        for (var k = 1; k <= 200; k++)
        {
            var cap = RingCapacity.Of(k);
            Assert.True(cap > prev, $"k={k}");
            prev = cap;
        }
    }

    [Fact]
    public void RingCapacity_Total_ZeroRingsIsZero_AndSumsTable()
    {
        Assert.Equal(0, RingCapacity.Total(0));
        Assert.Equal(6 + 12 + 18 + 25 + 31 + 37 + 43 + 50, RingCapacity.Total(8));
    }

    [Theory]
    [InlineData(36)]
    [InlineData(56)]
    [InlineData(96)]
    public void Compute_RingsBeyondSettingRange_StillKeepsPitchAndEvenSpacing(int b)
    {
        var center = new Vec2(-250f, 1500f);
        var slots = WheelLayout.Compute(new WheelParams(b, 8, center));
        Assert.Equal(222, slots.Length);
        var pitch = b + 8f;
        var start = 0;
        for (var k = 1; k <= 8; k++)
        {
            var cap = RingCapacity.Of(k);
            for (var i = 0; i < cap; i++)
            {
                var s = slots[start + i];
                Assert.Equal(start + i, s.Index);
                Assert.Equal(k, s.Ring);
                Assert.Equal(k * pitch, s.Center.Distance(center), 0.05f);
                Assert.Equal(-90f + (i * 360f / cap), s.AngleDegrees, Tol);
                var next = slots[start + ((i + 1) % cap)];
                Assert.True(s.Center.Distance(next.Center) >= pitch - 0.05f, $"B={b} k={k} i={i}");
            }

            start += cap;
        }
    }

    [Fact]
    public void Compute_SlotZeroIsAlwaysTwelveOClock_AtEveryButtonSize()
    {
        for (var b = 36; b <= 96; b += 4)
        {
            var c = new Vec2(10f, 20f);
            var slots = WheelLayout.Compute(new WheelParams(b, 1, c));
            Assert.Equal(c.X, slots[0].Center.X, Tol);
            Assert.Equal(20f - (b + 8f), slots[0].Center.Y, Tol);
        }
    }

    [Fact]
    public void Compute_IsDeterministic_AndTranslatesWithCentre()
    {
        var a = WheelLayout.Compute(new WheelParams(56f, 3, new Vec2(0f, 0f)));
        var b = WheelLayout.Compute(new WheelParams(56f, 3, new Vec2(0f, 0f)));
        Assert.Equal(a, b);
        var moved = WheelLayout.Compute(new WheelParams(56f, 3, new Vec2(300f, -40f)));
        for (var i = 0; i < a.Length; i++)
        {
            Assert.Equal(a[i].Center.X + 300f, moved[i].Center.X, Tol);
            Assert.Equal(a[i].Center.Y - 40f, moved[i].Center.Y, Tol);
        }
    }

    [Fact]
    public void Compute_RejectsNaNAndNegativeButtonSize_AndNegativeRings()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WheelLayout.Compute(new WheelParams(float.NaN, 2, default)));
        Assert.Throws<ArgumentOutOfRangeException>(() => WheelLayout.Compute(new WheelParams(-56f, 2, default)));
        Assert.Throws<ArgumentOutOfRangeException>(() => WheelLayout.Compute(new WheelParams(56f, -1, default)));
    }

    [Fact]
    public void DiscRadius_MatchesFormula_ForEveryButtonSizeAndRing()
    {
        Assert.Equal(168f, LayoutConstants.DiscRadius(56f, 2));
        for (var b = 36; b <= 96; b += 4)
        {
            for (var m = 1; m <= 5; m++)
            {
                Assert.Equal((m * (b + 8f)) + (b / 2f) + 12f, LayoutConstants.DiscRadius(b, m));
                // The outermost slot edge plus rimPad is exactly the disc radius.
                Assert.Equal(LayoutConstants.DiscRadius(b, m), (m * LayoutConstants.Pitch(b)) + (b / 2f) + LayoutConstants.RimPad);
            }
        }
    }

    [Fact]
    public void Compute_NonFiniteCenter_Throws()
    {
        foreach (float bad in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => WheelLayout.Compute(new WheelParams(56f, 2, new Vec2(bad, 0f))));
            Assert.Throws<ArgumentOutOfRangeException>(() => WheelLayout.Compute(new WheelParams(56f, 2, new Vec2(0f, bad))));
        }
    }

    [Theory]
    [InlineData(36f)]
    [InlineData(56f)]
    [InlineData(96f)]
    public void Compute_HitRadiusIsHalfPitch_ForEverySlot(float size)
    {
        foreach (SlotPlacement slot in WheelLayout.Compute(new WheelParams(size, 3, default)))
        {
            Assert.Equal((size + 8f) / 2f, slot.HitRadius);
        }
    }
}
