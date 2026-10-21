using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;
using Xunit;

namespace QuickMenu.Core.Tests.Layout;

public class HitTesterGridTests
{
    private static readonly Vec2 _c = new(500f, 400f);

    private static GridHitLayout Make(Rect2? plate = null, HitButton[]? buttons = null) =>
        new(_c, 56f, 5, 3, plate, buttons, buttons?.Length ?? 0);

    [Fact]
    public void Slot_centres_hit_their_index()
    {
        var l = Make();
        var slots = GridLayout.Compute(new GridParams(56f, 5, 3, _c));
        foreach (var s in slots)
        {
            Assert.Equal(Hit.Slot(s.Index), HitTester.HitGrid(s.Center, l));
        }
    }

    [Fact]
    public void Slot_is_hit_in_the_whole_pitch_square_including_the_gap()
    {
        var l = Make();
        // slot 0 centre (372, 336); its square spans 340..404 x 304..368
        Assert.Equal(Hit.Slot(0), HitTester.HitGrid(new Vec2(341f, 305f), l));
        Assert.Equal(Hit.Slot(0), HitTester.HitGrid(new Vec2(403f, 367f), l));
        Assert.Equal(Hit.Slot(0), HitTester.HitGrid(new Vec2(372f + 31f, 336f), l));
    }

    [Fact]
    public void Exact_cell_boundary_picks_the_lower_index()
    {
        var l = Make();
        Assert.Equal(Hit.Slot(0), HitTester.HitGrid(new Vec2(404f, 336f), l));
        Assert.Equal(Hit.Slot(0), HitTester.HitGrid(new Vec2(372f, 368f), l));
    }

    [Fact]
    public void Whole_centre_cell_is_HubEdit()
    {
        var l = Make();
        Assert.Equal(Hit.HubEdit, HitTester.HitGrid(_c, l));
        Assert.Equal(Hit.HubEdit, HitTester.HitGrid(new Vec2(469f, 369f), l));
        Assert.Equal(Hit.HubEdit, HitTester.HitGrid(new Vec2(531f, 431f), l));
        Assert.Equal(Hit.HubEdit, HitTester.HitGrid(new Vec2(468f, 400f), l));
    }

    [Fact]
    public void Slot_indices_after_the_hub_are_shifted_by_one()
    {
        var l = Make();
        Assert.Equal(Hit.Slot(6), HitTester.HitGrid(new Vec2(436f, 400f), l));
        Assert.Equal(Hit.Slot(7), HitTester.HitGrid(new Vec2(564f, 400f), l));
    }

    [Fact]
    public void Panel_padding_is_inside_and_beyond_is_outside()
    {
        var l = Make();
        var panel = GridLayout.Panel(new GridParams(56f, 5, 3, _c));
        Assert.Equal(Hit.Inside, HitTester.HitGrid(new Vec2(panel.X + 1f, panel.Y + 1f), l));
        Assert.Equal(Hit.Inside, HitTester.HitGrid(new Vec2(panel.Right - 0.5f, _c.Y), l));
        Assert.Equal(Hit.Outside, HitTester.HitGrid(new Vec2(panel.X - 1f, _c.Y), l));
        Assert.Equal(Hit.Outside, HitTester.HitGrid(new Vec2(_c.X, panel.Bottom + 1f), l));
    }

    [Fact]
    public void Plate_counts_as_inside_menu_kind_Plate()
    {
        var panel = GridLayout.Panel(new GridParams(56f, 5, 3, _c));
        var plate = new Rect2(_c.X - 50f, panel.Bottom + 8f, 100f, 30f);
        var l = Make(plate);
        Assert.Equal(Hit.Plate, HitTester.HitGrid(new Vec2(_c.X, panel.Bottom + 20f), l));
        Assert.Equal(Hit.Outside, HitTester.HitGrid(new Vec2(_c.X + 60f, panel.Bottom + 20f), l));
    }

    [Fact]
    public void Button_wins_over_everything()
    {
        var b = new[] { new HitButton(new Rect2(490f, 390f, 20f, 20f), 7) };
        Assert.Equal(Hit.Button(7), HitTester.HitGrid(_c, Make(null, b)));
    }

    [Fact]
    public void Non_finite_point_is_outside()
    {
        Assert.Equal(Hit.Outside, HitTester.HitGrid(new Vec2(float.NaN, 0f), Make()));
        Assert.Equal(Hit.Outside, HitTester.HitGrid(new Vec2(0f, float.PositiveInfinity), Make()));
    }

    [Fact]
    public void One_by_three_grid_hits()
    {
        var l = new GridHitLayout(_c, 56f, 3, 1);
        Assert.Equal(Hit.Slot(0), HitTester.HitGrid(new Vec2(436f, 400f), l));
        Assert.Equal(Hit.Slot(1), HitTester.HitGrid(new Vec2(564f, 400f), l));
        Assert.Equal(Hit.HubEdit, HitTester.HitGrid(_c, l));
    }
}
