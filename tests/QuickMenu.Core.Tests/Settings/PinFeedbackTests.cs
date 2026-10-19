using QuickMenu.Core.Pins;
using QuickMenu.Core.Settings;
using Xunit;

namespace QuickMenu.Core.Tests.Settings;

public class PinFeedbackTests
{
    [Fact]
    public void Pinned_ticks_high_and_persists()
    {
        PinFeedback f = PinFeedback.For(PinResult.Pinned);
        Assert.Equal(PinSound.TickHigh, f.Sound);
        Assert.True(f.Persist);
        Assert.True(f.UsesLabel);
        Assert.False(f.IsReject);
        Assert.StartsWith("QuickMenu.", f.MessageKey);
    }

    [Fact]
    public void Unpinned_ticks_low_and_persists()
    {
        PinFeedback f = PinFeedback.For(PinResult.Unpinned);
        Assert.Equal(PinSound.TickLow, f.Sound);
        Assert.True(f.Persist);
        Assert.False(f.IsReject);
    }

    [Fact]
    public void Full_rejects_with_full_text_and_does_not_persist()
    {
        PinFeedback f = PinFeedback.For(PinResult.Full);
        Assert.Equal(PinSound.ClickReject, f.Sound);
        Assert.True(f.IsReject);
        Assert.False(f.Persist);
        Assert.False(f.UsesLabel);
        Assert.Equal(PinFeedback.FullKey, f.MessageKey);
    }

    [Fact]
    public void Rejected_names_the_item_and_does_not_persist()
    {
        PinFeedback f = PinFeedback.For(PinResult.Rejected);
        Assert.Equal(PinSound.ClickReject, f.Sound);
        Assert.True(f.IsReject);
        Assert.False(f.Persist);
        Assert.True(f.UsesLabel);
        Assert.Equal(PinFeedback.RejectedKey, f.MessageKey);
    }
}
