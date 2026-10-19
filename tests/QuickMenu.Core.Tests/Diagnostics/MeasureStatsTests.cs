using QuickMenu.Core.Diagnostics;
using Xunit;

namespace QuickMenu.Core.Tests.Diagnostics;

public class MeasureStatsTests
{
    [Fact]
    public void Calls_in_one_frame_form_one_bucket()
    {
        var s = new MeasureStats("draw", 8);
        s.Record(10, 100, 5); // Layout
        s.Record(20, 0, 5); // Repaint
        s.Record(30, 40, 6);
        s.Flush();

        Assert.Equal(3, s.Calls);
        Assert.Equal(2, s.Frames);
        Assert.Equal(60, s.TotalTicks);
        Assert.Equal(140, s.TotalBytes);
        Assert.Equal(new SampleSummary(2, 30, 30, 30), s.FrameTicks.Summarize());
        Assert.Equal(new SampleSummary(2, 40, 100, 100), s.FrameBytes.Summarize());
    }

    [Fact]
    public void Open_frame_is_kept_out_of_the_steady_state_and_max_over_opens_is_tracked()
    {
        var s = new MeasureStats("window", 8);
        s.MarkOpen();
        s.Record(5, 8000, 10);
        s.Record(5, 100, 10);
        s.Record(1, 10, 11);
        s.Record(1, 20, 12);
        s.MarkOpen();
        s.Record(9, 9000, 20);
        s.Record(1, 30, 21);
        s.Flush();

        Assert.Equal(2, s.Opens);
        Assert.Equal(9000, s.FirstFrameBytesMax);
        Assert.Equal(10, s.FirstFrameTicksMax);
        Assert.Equal(3, s.Frames);
        Assert.Equal(new SampleSummary(3, 20, 30, 30), s.FrameBytes.Summarize());
    }

    [Fact]
    public void Flush_completes_a_pending_first_frame_and_is_idempotent()
    {
        var s = new MeasureStats("w", 4);
        s.MarkOpen();
        s.Record(2, 50, 1);
        s.Flush();
        s.Flush();
        Assert.Equal(50, s.FirstFrameBytesMax);
        Assert.Equal(0, s.Frames);
    }

    [Fact]
    public void Frame_rings_wrap_keeping_the_latest_frames()
    {
        var s = new MeasureStats("w", 4);
        for (int f = 1; f <= 10; f++)
        {
            s.Record(f, f * 10, f);
            s.Record(f, 0, f);
        }

        s.Flush();
        Assert.Equal(10, s.Frames);
        Assert.Equal(4, s.FrameBytes.Count);
        Assert.Equal(new SampleSummary(4, 80, 100, 100), s.FrameBytes.Summarize());
    }

    [Fact]
    public void First_call_ticks_and_bytes_are_kept_until_reset()
    {
        var s = new MeasureStats("open", 4);
        s.Record(777, 12, 1);
        s.Record(5, 1, 2);
        Assert.Equal(777, s.FirstCallTicks);
        Assert.Equal(12, s.FirstCallBytes);
        s.Reset();
        s.Record(9, 3, 7);
        Assert.Equal(9, s.FirstCallTicks);
        Assert.Equal(3, s.FirstCallBytes);
    }

    [Fact]
    public void Reset_clears_everything_including_frame_tracking()
    {
        var s = new MeasureStats("x", 4);
        s.MarkOpen();
        s.Record(1, 9, 3);
        s.Flush();
        s.Reset();
        s.Record(2, 4, 3);
        s.Flush();

        Assert.Equal(1, s.Calls);
        Assert.Equal(1, s.Frames);
        Assert.Equal(0, s.Opens);
        Assert.Equal(0, s.FirstFrameBytesMax);
        Assert.Equal(2, s.TotalTicks);
        Assert.Equal(1, s.FrameTicks.Count);
    }
}
