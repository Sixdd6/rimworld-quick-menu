namespace QuickMenu.Core.Diagnostics;

/// <summary>
/// Aggregates dev-mode measurements of one code path: elapsed ticks per call (ring, for median/p99/max), total and
/// first-call allocated bytes, and the number of distinct frames. Allocates nothing after construction.
/// </summary>
public sealed class MeasureStats
{
    private int _lastFrame = int.MinValue;

    public MeasureStats(string name, int ringCapacity)
    {
        Name = name;
        Ticks = new SampleRing(ringCapacity);
    }

    public string Name { get; }

    public SampleRing Ticks { get; }

    public long Calls { get; private set; }

    /// <summary>Number of distinct <c>frame</c> values seen (frames in which the path ran at least once).</summary>
    public long Frames { get; private set; }

    public long TotalTicks { get; private set; }

    public long TotalBytes { get; private set; }

    /// <summary>Bytes allocated by the first call since the last <see cref="Reset"/> (P-9 "first frame after open").</summary>
    public long FirstCallBytes { get; private set; }

    public long MaxCallBytes { get; private set; }

    public void Record(long ticks, long bytes, int frame)
    {
        if (Calls == 0)
        {
            FirstCallBytes = bytes;
        }

        Calls++;
        if (frame != _lastFrame)
        {
            _lastFrame = frame;
            Frames++;
        }

        TotalTicks += ticks;
        TotalBytes += bytes;
        if (bytes > MaxCallBytes)
        {
            MaxCallBytes = bytes;
        }

        Ticks.Add(ticks);
    }

    public void Reset()
    {
        Ticks.Clear();
        Calls = 0;
        Frames = 0;
        TotalTicks = 0;
        TotalBytes = 0;
        FirstCallBytes = 0;
        MaxCallBytes = 0;
        _lastFrame = int.MinValue;
    }
}
