namespace QuickMenu.Core.Diagnostics;

/// <summary>
/// Aggregates dev-mode measurements of one code path <b>per frame</b>: every call with the same frame number is summed
/// into one bucket (IMGUI runs <c>DoWindowContents</c> once per event), and each finished bucket goes into rings for
/// ticks and bytes, so median/p99/max are per frame. The first frame after <see cref="MarkOpen"/> is kept out of the
/// steady-state rings and tracked separately (max over opens). Allocates nothing after construction.
/// </summary>
public sealed class MeasureStats
{
    private bool _hasBucket;
    private int _bucketFrame;
    private long _bucketTicks;
    private long _bucketBytes;
    private bool _firstFramePending;

    public MeasureStats(string name, int ringCapacity)
    {
        Name = name;
        FrameTicks = new SampleRing(ringCapacity);
        FrameBytes = new SampleRing(ringCapacity);
    }

    public string Name { get; }

    /// <summary>Per-frame ticks of finished steady-state frames.</summary>
    public SampleRing FrameTicks { get; }

    /// <summary>Per-frame allocated bytes of finished steady-state frames.</summary>
    public SampleRing FrameBytes { get; }

    /// <summary>Number of <see cref="Record"/> calls.</summary>
    public long Calls { get; private set; }

    /// <summary>Finished steady-state frames (excludes first-after-open frames).</summary>
    public long Frames { get; private set; }

    public long Opens { get; private set; }

    public long TotalTicks { get; private set; }

    public long TotalBytes { get; private set; }

    public long FirstCallTicks { get; private set; }

    public long FirstCallBytes { get; private set; }

    /// <summary>Largest per-frame byte total of a first-after-open frame (P-9 "first frame after open").</summary>
    public long FirstFrameBytesMax { get; private set; }

    public long FirstFrameTicksMax { get; private set; }

    public void Record(long ticks, long bytes, int frame)
    {
        if (_hasBucket && frame != _bucketFrame)
        {
            CloseBucket();
        }

        if (!_hasBucket)
        {
            _hasBucket = true;
            _bucketFrame = frame;
            _bucketTicks = 0;
            _bucketBytes = 0;
        }

        if (Calls == 0)
        {
            FirstCallTicks = ticks;
            FirstCallBytes = bytes;
        }

        Calls++;
        _bucketTicks += ticks;
        _bucketBytes += bytes;
        TotalTicks += ticks;
        TotalBytes += bytes;
    }

    /// <summary>The next finished frame is a first-after-open frame (call when the menu opens, before its first draw).</summary>
    public void MarkOpen()
    {
        CloseBucket();
        Opens++;
        _firstFramePending = true;
    }

    /// <summary>Finishes the current frame bucket so reports include it. Safe to call repeatedly.</summary>
    public void Flush() => CloseBucket();

    public void Reset()
    {
        FrameTicks.Clear();
        FrameBytes.Clear();
        Calls = 0;
        Frames = 0;
        Opens = 0;
        TotalTicks = 0;
        TotalBytes = 0;
        FirstCallTicks = 0;
        FirstCallBytes = 0;
        FirstFrameBytesMax = 0;
        FirstFrameTicksMax = 0;
        _hasBucket = false;
        _firstFramePending = false;
    }

    private void CloseBucket()
    {
        if (!_hasBucket)
        {
            return;
        }

        _hasBucket = false;
        if (_firstFramePending)
        {
            _firstFramePending = false;
            if (_bucketBytes > FirstFrameBytesMax)
            {
                FirstFrameBytesMax = _bucketBytes;
            }

            if (_bucketTicks > FirstFrameTicksMax)
            {
                FirstFrameTicksMax = _bucketTicks;
            }

            return;
        }

        FrameTicks.Add(_bucketTicks);
        FrameBytes.Add(_bucketBytes);
        Frames++;
    }
}
