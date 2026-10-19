using System;
using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;
using Xunit;

namespace QuickMenu.Core.Tests.Layout;

public class EdgeDodgerTests
{
    private static readonly Vec2 _screen = new(960f, 540f);

    private static Rect2 Box(float cx, float cy) => MenuBounds.ForWheel(new Vec2(cx, cy), 56f, 2);

    [Fact]
    public void Fully_inside_does_not_move()
    {
        Assert.Equal(new Vec2(0f, 0f), EdgeDodger.Fit(Box(480f, 270f), _screen));
    }

    [Fact]
    public void Left_edge_shifts_right_to_margin()
    {
        var b = Box(10f, 270f);
        var o = EdgeDodger.Fit(b, _screen);
        Assert.Equal(8f, b.X + o.X);
        Assert.Equal(0f, o.Y);
    }

    [Fact]
    public void Right_edge_shifts_left_to_margin()
    {
        var b = Box(950f, 270f);
        var o = EdgeDodger.Fit(b, _screen);
        Assert.Equal(952f, b.Right + o.X);
        Assert.Equal(0f, o.Y);
    }

    [Fact]
    public void Top_edge_shifts_down_to_margin()
    {
        var b = Box(480f, 20f);
        var o = EdgeDodger.Fit(b, _screen);
        Assert.Equal(8f, b.Y + o.Y);
        Assert.Equal(0f, o.X);
    }

    [Fact]
    public void Bottom_edge_accounts_for_plate()
    {
        var b = Box(480f, 530f);
        var o = EdgeDodger.Fit(b, _screen);
        Assert.Equal(532f, b.Bottom + o.Y);
        Assert.Equal(0f, o.X);
    }

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(960f, 0f)]
    [InlineData(0f, 540f)]
    [InlineData(960f, 540f)]
    public void Corners_shift_both_axes_to_fit(float cx, float cy)
    {
        var b = Box(cx, cy);
        var o = EdgeDodger.Fit(b, _screen);
        Assert.True(b.X + o.X >= 8f - 1e-3f);
        Assert.True(b.Y + o.Y >= 8f - 1e-3f);
        Assert.True(b.Right + o.X <= 952f + 1e-3f);
        Assert.True(b.Bottom + o.Y <= 532f + 1e-3f);
        // minimum: at least one side is flush on each axis
        Assert.True(Math.Abs(b.X + o.X - 8f) < 1e-3f || Math.Abs(b.Right + o.X - 952f) < 1e-3f);
        Assert.True(Math.Abs(b.Y + o.Y - 8f) < 1e-3f || Math.Abs(b.Bottom + o.Y - 532f) < 1e-3f);
    }

    [Fact]
    public void Wider_than_allowed_centres_horizontally_only()
    {
        var b = new Rect2(-100f, 20f, 1000f, 100f);
        var o = EdgeDodger.Fit(b, _screen);
        Assert.Equal(480f, b.X + o.X + (b.Width / 2f));
        Assert.Equal(0f, o.Y);
    }

    [Fact]
    public void Taller_than_allowed_centres_vertically_only()
    {
        var b = new Rect2(20f, -50f, 100f, 600f);
        var o = EdgeDodger.Fit(b, _screen);
        Assert.Equal(270f, b.Y + o.Y + (b.Height / 2f));
        Assert.Equal(0f, o.X);
    }

    [Fact]
    public void Both_oversize_centres_on_screen()
    {
        var b = new Rect2(0f, 0f, 2000f, 2000f);
        var o = EdgeDodger.Fit(b, _screen);
        Assert.Equal(new Vec2(-520f, -730f), o);
    }

    [Fact]
    public void Exactly_allowed_size_is_centred_and_flush()
    {
        var b = new Rect2(300f, 300f, 944f, 100f);
        var o = EdgeDodger.Fit(b, _screen);
        Assert.Equal(8f, b.X + o.X);
    }

    [Fact]
    public void Custom_margin_is_honoured()
    {
        var o = EdgeDodger.Fit(new Rect2(0f, 100f, 50f, 50f), _screen, 20f);
        Assert.Equal(20f, o.X);
    }

    [Fact]
    public void Default_wheel_at_4_ring_ui_scale_two_is_centred()
    {
        var b = MenuBounds.ForWheel(new Vec2(100f, 100f), 56f, 4);
        var o = EdgeDodger.Fit(b, _screen);
        Assert.Equal(270f, b.Y + o.Y + (b.Height / 2f));
    }
}
