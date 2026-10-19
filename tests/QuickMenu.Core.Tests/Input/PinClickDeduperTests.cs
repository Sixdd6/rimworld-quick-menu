using QuickMenu.Core.Input;
using Xunit;

namespace QuickMenu.Core.Tests.Input;

public class PinClickDeduperTests
{
    private static PinClickInput Down() => new(false, RawEventType.Used, 0, false, false, false, true, false, false, true);

    private static PinClickInput Up() => new(false, RawEventType.Used, 0, false, false, false, true, false, false, false, true);

    private const int None = PinClickDeduper.NoPress;

    [Fact]
    public void Down_then_up_of_same_press_accepts_once()
    {
        var d = new PinClickDeduper();
        Assert.True(d.Accept(10, 10, Down()));
        Assert.False(d.Accept(14, 10, Up()));
        Assert.False(d.Accept(15, 10, Up()));
    }

    [Fact]
    public void Same_frame_repeat_is_rejected()
    {
        var d = new PinClickDeduper();
        Assert.True(d.Accept(10, 10, Down()));
        Assert.False(d.Accept(10, 10, Down()));
    }

    [Fact]
    public void Later_separate_up_only_click_is_accepted()
    {
        var d = new PinClickDeduper();
        Assert.True(d.Accept(20, 20, Down()));
        Assert.True(d.Accept(5000, 4990, Up()));
        Assert.False(d.Accept(5003, 4990, Up()));
    }

    [Fact]
    public void Up_only_is_accepted_and_next_click_works()
    {
        var d = new PinClickDeduper();
        Assert.True(d.Accept(5, None, Up()));
        Assert.False(d.Accept(5, None, Up()));
        Assert.True(d.Accept(20, 20, Down()));
        Assert.False(d.Accept(22, 20, Up()));
        Assert.True(d.Accept(30, 30, Down()));
    }

    [Fact]
    public void Up_with_no_known_press_is_always_accepted_on_new_frames()
    {
        var d = new PinClickDeduper();
        Assert.True(d.Accept(5, None, Up()));
        Assert.True(d.Accept(9, None, Up()));
    }

    [Fact]
    public void Stale_older_down_frame_never_rejects_and_does_not_move_the_accepted_press_back()
    {
        var d = new PinClickDeduper();
        Assert.True(d.Accept(100, 100, Down()));
        Assert.True(d.Accept(103, 50, Up()));
        Assert.False(d.Accept(105, 100, Up()));
    }

    [Fact]
    public void Stale_down_frame_before_any_accepted_press_is_accepted()
    {
        var d = new PinClickDeduper();
        Assert.True(d.Accept(200, 150, Up()));
        Assert.False(d.Accept(203, 150, Up()));
        Assert.True(d.Accept(300, 100, Up()));
    }
}
