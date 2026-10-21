using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;
using Xunit;

namespace QuickMenu.Core.Tests.Layout;

public class WheelLayoutCacheTests
{
    private static WheelLayoutKey Key(int revision = 1, int w = 1920, int h = 1080, float scale = 1f, float b = 56f, int rings = 2) =>
        new(revision, w, h, scale, b, rings);

    [Fact]
    public void Same_key_computes_the_geometry_once()
    {
        var cache = new WheelLayoutCache();
        cache.Place(Key(), new Vec2(900f, 500f), 30f);
        cache.Place(Key(), new Vec2(100f, 600f), 30f);
        Assert.Equal(1, cache.Rebuilds);
    }

    [Theory]
    [InlineData(2, 1920, 1080, 1f, 56f, 2)]
    [InlineData(1, 1280, 1080, 1f, 56f, 2)]
    [InlineData(1, 1920, 720, 1f, 56f, 2)]
    [InlineData(1, 1920, 1080, 2f, 56f, 2)]
    [InlineData(1, 1920, 1080, 1f, 64f, 2)]
    [InlineData(1, 1920, 1080, 1f, 56f, 3)]
    public void Any_changed_key_part_rebuilds(int revision, int w, int h, float scale, float b, int rings)
    {
        var cache = new WheelLayoutCache();
        cache.Place(Key(), new Vec2(900f, 500f), 30f);
        cache.Place(Key(revision, w, h, scale, b, rings), new Vec2(900f, 500f), 30f);
        Assert.Equal(2, cache.Rebuilds);
    }

    [Fact]
    public void Away_from_edges_the_hub_is_on_the_pointer_and_slots_match_a_direct_layout()
    {
        var cache = new WheelLayoutCache();
        var pointer = new Vec2(900f, 500f);
        PlacedWheel placed = cache.Place(Key(), pointer, 30f);
        SlotPlacement[] direct = WheelLayout.Compute(new WheelParams(56f, 2, pointer));
        Assert.Equal(pointer, placed.Center);
        Assert.Equal(direct.Length, placed.Slots.Length);
        for (int i = 0; i < direct.Length; i++)
        {
            Assert.Equal(direct[i].Index, placed.Slots[i].Index);
            Assert.Equal(direct[i].Center.X, placed.Slots[i].Center.X, 3);
            Assert.Equal(direct[i].Center.Y, placed.Slots[i].Center.Y, 3);
        }
    }

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(1919f, 0f)]
    [InlineData(0f, 1079f)]
    [InlineData(1919f, 1079f)]
    public void Near_each_corner_the_whole_menu_is_on_screen(float x, float y)
    {
        var cache = new WheelLayoutCache();
        PlacedWheel placed = cache.Place(Key(), new Vec2(x, y), 30f);
        Rect2 bounds = MenuBounds.ForWheel(placed.Center, 56f, 2, 30f);
        Assert.True(bounds.X >= 0f && bounds.Y >= 0f);
        Assert.True(bounds.Right <= 1920f && bounds.Bottom <= 1080f);
    }

    [Fact]
    public void Each_open_gets_its_own_slot_array()
    {
        var cache = new WheelLayoutCache();
        PlacedWheel a = cache.Place(Key(), new Vec2(900f, 500f), 30f);
        PlacedWheel b = cache.Place(Key(), new Vec2(300f, 400f), 30f);
        Assert.NotSame(a.Slots, b.Slots);
        Assert.NotEqual(a.Slots[0].Center, b.Slots[0].Center);
    }
}
