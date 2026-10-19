using QuickMenu.Core.Diagnostics;
using Xunit;

namespace QuickMenu.Core.Tests.Diagnostics;

public class MeasureStatsTests
{
    [Fact]
    public void Record_aggregates_calls_frames_ticks_and_bytes()
    {
        var s = new MeasureStats("draw", 8);
        s.Record(10, 100, 5);
        s.Record(20, 0, 5);
        s.Record(30, 40, 6);

        Assert.Equal("draw", s.Name);
        Assert.Equal(3, s.Calls);
        Assert.Equal(2, s.Frames);
        Assert.Equal(60, s.TotalTicks);
        Assert.Equal(140, s.TotalBytes);
        Assert.Equal(100, s.FirstCallBytes);
        Assert.Equal(100, s.MaxCallBytes);
        Assert.Equal(20, s.Ticks.Summarize().Median);
    }

    [Fact]
    public void Reset_clears_everything_including_frame_tracking()
    {
        var s = new MeasureStats("x", 4);
        s.Record(1, 9, 3);
        s.Reset();
        s.Record(2, 4, 3);

        Assert.Equal(1, s.Calls);
        Assert.Equal(1, s.Frames);
        Assert.Equal(4, s.FirstCallBytes);
        Assert.Equal(2, s.TotalTicks);
        Assert.Equal(1, s.Ticks.Count);
    }
}
