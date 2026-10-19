using System;
using QuickMenu.Core.Diagnostics;
using Xunit;

namespace QuickMenu.Core.Tests.Diagnostics;

public class SampleRingTests
{
    [Fact]
    public void Empty_ring_summarizes_to_zero()
    {
        var ring = new SampleRing(4);
        Assert.Equal(default, ring.Summarize());
    }

    [Fact]
    public void Single_sample_is_median_p99_and_max()
    {
        var ring = new SampleRing(4);
        ring.Add(7);
        Assert.Equal(new SampleSummary(1, 7, 7, 7), ring.Summarize());
    }

    [Fact]
    public void Odd_and_even_counts_use_nearest_rank()
    {
        var ring = new SampleRing(8);
        foreach (long v in new long[] { 5, 1, 3 })
        {
            ring.Add(v);
        }

        Assert.Equal(3, ring.Summarize().Median);
        ring.Add(9);
        Assert.Equal(3, ring.Summarize().Median); // lower of 3 and 5
    }

    [Fact]
    public void Hundred_samples_give_known_percentiles()
    {
        var ring = new SampleRing(128);
        for (int i = 100; i >= 1; i--)
        {
            ring.Add(i);
        }

        Assert.Equal(new SampleSummary(100, 50, 99, 100), ring.Summarize());
    }

    [Fact]
    public void Outlier_shows_in_max_and_p99_not_median()
    {
        var ring = new SampleRing(1000);
        for (int i = 0; i < 999; i++)
        {
            ring.Add(2);
        }

        ring.Add(5000);
        SampleSummary s = ring.Summarize();
        Assert.Equal(2, s.Median);
        Assert.Equal(2, s.P99);
        Assert.Equal(5000, s.Max);
    }

    [Fact]
    public void Full_ring_overwrites_oldest()
    {
        var ring = new SampleRing(3);
        for (long v = 1; v <= 5; v++)
        {
            ring.Add(v);
        }

        Assert.Equal(3, ring.Count);
        Assert.Equal(new SampleSummary(3, 4, 5, 5), ring.Summarize());
    }

    [Fact]
    public void Clear_empties_the_ring()
    {
        var ring = new SampleRing(3);
        ring.Add(1);
        ring.Clear();
        Assert.Equal(0, ring.Count);
        Assert.Equal(default, ring.Summarize());
    }

    [Fact]
    public void Zero_capacity_is_rejected() => Assert.Throws<ArgumentOutOfRangeException>(() => new SampleRing(0));

    [Fact]
    [Trait("Category", "Perf")]
    public void Add_and_Summarize_allocate_zero_bytes()
    {
        var ring = new SampleRing(16384);
        var stats = new MeasureStats("x", 16384);
        for (int i = 0; i < 20000; i++) // warm up and wrap
        {
            ring.Add(i);
        }

        ring.Summarize();
        stats.Record(1, 1, 1);
        stats.MarkOpen();
        stats.Flush();

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10000; i++)
        {
            ring.Add((i * 7919L) % 1000);
            if (i % 500 == 0)
            {
                stats.MarkOpen();
            }

            stats.Record(i, i & 3, i / 3);
        }

        stats.Flush();
        SampleSummary s = ring.Summarize();
        SampleSummary f = stats.FrameBytes.Summarize();
        long after = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(0, after - before);
        Assert.True(s.Max >= s.P99 && s.P99 >= s.Median);
        Assert.True(f.Count > 0);
    }
}
