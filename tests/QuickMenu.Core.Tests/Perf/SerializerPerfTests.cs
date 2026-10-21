using System;
using System.Collections.Generic;
using System.Diagnostics;

using QuickMenu.Core.Pins;
using QuickMenu.TestData;
using Xunit;

namespace QuickMenu.Core.Tests.Perf;

/// <summary>P-16 (parse) and P-17 (save, pure part): 200 pins, gate at 5 x 5 ms (performance.md section 4).</summary>
[Trait("Category", "Perf")]
public class SerializerPerfTests
{
    private const double BudgetMilliseconds = 5.0;
    private const double GateFactor = 5.0;

    private static double MedianMilliseconds(Action body)
    {
        for (int i = 0; i < 10; i++)
        {
            body();
        }

        double[] ms = new double[5];
        for (int run = 0; run < ms.Length; run++)
        {
            long start = Stopwatch.GetTimestamp();
            body();
            ms[run] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }

        Array.Sort(ms);
        return ms[2];
    }

    [Fact]
    public void FromLines_200_pins_stays_under_five_times_the_budget()
    {
        List<string> lines = SyntheticCatalog.PinLines();
        Assert.Equal(SyntheticCatalog.PinCount, lines.Count);
        PinLoadResult? result = null;
        double median = MedianMilliseconds(() => result = PinSerializer.FromLines(lines));

        Assert.Equal(0, result!.SkippedCount);
        Assert.True(median <= BudgetMilliseconds * GateFactor, $"median {median:F3} ms, limit {BudgetMilliseconds * GateFactor} ms");
    }

    [Fact]
    public void ToLines_200_pins_stays_under_five_times_the_budget()
    {
        PinBoard board = SyntheticCatalog.Pins();
        List<string>? lines = null;
        double median = MedianMilliseconds(() => lines = PinSerializer.ToLines(board));

        Assert.Equal(SyntheticCatalog.PinCount, lines!.Count);
        Assert.True(median <= BudgetMilliseconds * GateFactor, $"median {median:F3} ms, limit {BudgetMilliseconds * GateFactor} ms");
    }
}
