using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;
using Xunit;

namespace QuickMenu.Core.Tests.Layout;

public class EdgeDodgerTinyScreenTests
{
    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(10f, 10f)]
    [InlineData(15f, 15f)]
    [InlineData(16f, 16f)]
    [InlineData(100f, 100f)]
    [InlineData(336f, 374f)]
    public void Tiny_screens_centre_the_box_and_stay_finite(float sw, float sh)
    {
        var box = MenuBounds.ForWheel(new Vec2(sw / 2f, sh / 2f), 56f, 2);
        Vec2 o = EdgeDodger.Fit(box, new Vec2(sw, sh));
        Assert.True(float.IsFinite(o.X) && float.IsFinite(o.Y));
        Assert.Equal((sw - box.Width) / 2f, box.X + o.X, 3);
        Assert.Equal((sh - box.Height) / 2f, box.Y + o.Y, 3);
    }

    [Fact]
    public void One_pixel_short_of_fitting_centres_and_one_pixel_more_fits_flush()
    {
        var box = new Rect2(0f, 0f, 100f, 100f);
        // allowed area is 100 wide at screen 116 with margin 8: fits exactly, size >= area means centred (= flush).
        Vec2 exact = EdgeDodger.Fit(box, new Vec2(116f, 116f));
        Assert.Equal(new Vec2(8f, 8f), exact);
        Vec2 roomy = EdgeDodger.Fit(box, new Vec2(117f, 117f));
        Assert.Equal(new Vec2(8f, 8f), roomy);
        Vec2 tight = EdgeDodger.Fit(box, new Vec2(115f, 115f));
        Assert.Equal(new Vec2(7.5f, 7.5f), tight);
    }

    [Fact]
    public void Result_always_keeps_a_fitting_box_inside_after_shifting()
    {
        var screen = new Vec2(200f, 150f);
        foreach (float x in new[] { -500f, -1f, 0f, 7f, 8f, 100f, 150f, 500f })
        {
            foreach (float y in new[] { -500f, 0f, 60f, 75f, 500f })
            {
                var box = new Rect2(x, y, 100f, 60f);
                Vec2 o = EdgeDodger.Fit(box, screen);
                Assert.InRange(box.X + o.X, 8f, 92f);
                Assert.InRange(box.Y + o.Y, 8f, 82f);
            }
        }
    }

    [Fact]
    public void Mouse_off_screen_is_pulled_back()
    {
        var box = MenuBounds.ForWheel(new Vec2(-2000f, 5000f), 56f, 2);
        Vec2 o = EdgeDodger.Fit(box, new Vec2(1920f, 1080f));
        Assert.Equal(8f, box.X + o.X, 3);
        Assert.Equal(1080f - 8f, box.Bottom + o.Y, 3);
    }
}
