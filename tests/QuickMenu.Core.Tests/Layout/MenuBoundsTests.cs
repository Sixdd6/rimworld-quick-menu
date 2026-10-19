using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;
using Xunit;

namespace QuickMenu.Core.Tests.Layout;

public class MenuBoundsTests
{
    [Fact]
    public void Default_wheel_is_336_wide_top_minus_168_bottom_plus_206()
    {
        var r = MenuBounds.ForWheel(new Vec2(0f, 0f), 56f, 2);
        Assert.Equal(336f, r.Width);
        Assert.Equal(-168f, r.Y);
        Assert.Equal(206f, r.Bottom);
        Assert.Equal(-168f, r.X);
    }

    [Fact]
    public void Wheel_bounds_follow_the_centre()
    {
        var r = MenuBounds.ForWheel(new Vec2(500f, 300f), 56f, 2);
        Assert.Equal(new Rect2(332f, 132f, 336f, 374f), r);
    }

    [Theory]
    [InlineData(36f, 1)]
    [InlineData(96f, 5)]
    public void Extremes_use_disc_radius(float b, int rings)
    {
        var d = LayoutConstants.DiscRadius(b, rings);
        var r = MenuBounds.ForWheel(new Vec2(0f, 0f), b, rings);
        Assert.Equal(2 * d, r.Width);
        Assert.Equal(d + 8f + 30f, r.Bottom);
    }

    [Fact]
    public void Custom_plate_height_is_used()
    {
        Assert.Equal(168f + 8f + 40f, MenuBounds.ForWheel(new Vec2(0f, 0f), 56f, 2, 40f).Bottom);
    }

    [Fact]
    public void Panel_bounds_include_plate_below()
    {
        var r = MenuBounds.ForPanel(new Rect2(100f, 100f, 336f, 208f), 200f);
        Assert.Equal(new Rect2(100f, 100f, 336f, 246f), r);
    }

    [Fact]
    public void Wide_plate_widens_panel_bounds_symmetrically()
    {
        var r = MenuBounds.ForPanel(new Rect2(100f, 100f, 100f, 50f), 200f);
        Assert.Equal(50f, r.X);
        Assert.Equal(200f, r.Width);
    }
}
