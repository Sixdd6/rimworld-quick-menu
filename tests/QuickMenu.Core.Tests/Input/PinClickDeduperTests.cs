using QuickMenu.Core.Input;
using Xunit;

namespace QuickMenu.Core.Tests.Input;

public class PinClickDeduperTests
{
    private static PinClickInput Down() => new(false, RawEventType.Used, 0, false, false, false, true, false, false, true, false, true);

    private static PinClickInput Up() => new(false, RawEventType.Used, 0, false, false, false, true, false, false, false, true, false);

    [Fact]
    public void Down_then_up_accepts_once()
    {
        var d = new PinClickDeduper();
        Assert.True(d.Accept(10, Down()));
        Assert.False(d.Accept(14, Up()));
    }

    [Fact]
    public void Same_frame_repeat_is_rejected()
    {
        var d = new PinClickDeduper();
        Assert.True(d.Accept(10, Down()));
        Assert.False(d.Accept(10, Down()));
    }

    [Fact]
    public void Up_only_is_accepted_and_next_click_works()
    {
        var d = new PinClickDeduper();
        Assert.True(d.Accept(5, Up()));
        Assert.False(d.Accept(5, Up()));
        Assert.True(d.Accept(20, Down()));
        Assert.False(d.Accept(22, Up()));
        Assert.True(d.Accept(30, Down()));
    }
}
