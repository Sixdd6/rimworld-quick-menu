using System;

namespace QuickMenu.Core.Diagnostics;

/// <summary>Median, 99th percentile and maximum of a sample set (nearest-rank percentiles).</summary>
public readonly record struct SampleSummary(int Count, long Median, long P99, long Max);

/// <summary>
/// Fixed-capacity ring of samples. <see cref="Add"/> and <see cref="Summarize"/> allocate nothing: both buffers are
/// created in the constructor. When full, the oldest sample is overwritten. Not thread-safe (main thread only).
/// </summary>
public sealed class SampleRing
{
    private readonly long[] _samples;
    private readonly long[] _scratch;
    private int _next;

    public SampleRing(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _samples = new long[capacity];
        _scratch = new long[capacity];
    }

    public int Capacity => _samples.Length;

    public int Count { get; private set; }

    public void Add(long value)
    {
        _samples[_next] = value;
        _next = _next + 1 == _samples.Length ? 0 : _next + 1;
        if (Count < _samples.Length)
        {
            Count++;
        }
    }

    public void Clear()
    {
        _next = 0;
        Count = 0;
    }

    /// <summary>Sorts a copy of the live samples in the scratch buffer. Percentile rank is <c>ceil(p * n)</c>.</summary>
    public SampleSummary Summarize()
    {
        int n = Count;
        if (n == 0)
        {
            return default;
        }

        // Live samples are always slots 0..n-1: the ring only wraps once it is full, and then all slots are live.
        Array.Copy(_samples, _scratch, n);
        Array.Sort(_scratch, 0, n);
        return new SampleSummary(n, _scratch[((n + 1) / 2) - 1], _scratch[Rank(n, 99)], _scratch[n - 1]);
    }

    private static int Rank(int n, int percent) => Math.Max(0, ((n * percent) + 99) / 100 - 1);
}
