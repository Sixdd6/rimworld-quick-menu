using System;

using QuickMenu.Core.Pins;
using QuickMenu.TestData;
using Xunit;

namespace QuickMenu.Core.Tests.Perf;

/// <summary>P-5: pin click allocates a bounded amount (at most 2 KB per call), strict.</summary>
[Trait("Category", "Perf")]
public class PinBoardPerfTests
{
    private const int MaxBytesPerCall = 2048;
    private const int Calls = 1_000;

    [Fact]
    public void Toggle_pin_and_unpin_allocate_at_most_2KB_per_call()
    {
        PinBoard board = SyntheticCatalog.Pins(SyntheticCatalog.PinCount - 1);
        board.Capacity = SyntheticCatalog.PinCount; // one free slot, so Toggle alternates pin and unpin
        PinKey key = PinKey.Build(PinKeyKind.BuildThing, "Perf_Toggle_Target");

        for (int i = 0; i < 100; i++)
        {
            board.Toggle(key, "Perf label", "author.mod");
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        PinResult last = PinResult.Rejected;
        for (int i = 0; i < Calls; i++)
        {
            last = board.Toggle(key, "Perf label", "author.mod");
        }

        long perCall = (GC.GetAllocatedBytesForCurrentThread() - before) / Calls;
        Assert.True(perCall <= MaxBytesPerCall, $"{perCall} bytes per Toggle");
        Assert.True(last is PinResult.Pinned or PinResult.Unpinned);
    }

    [Fact]
    public void Toggle_on_a_full_board_allocates_at_most_2KB_per_call()
    {
        PinBoard board = SyntheticCatalog.Pins();
        PinKey key = PinKey.Build(PinKeyKind.BuildThing, "Perf_Full_Target");
        for (int i = 0; i < 100; i++)
        {
            board.Toggle(key);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < Calls; i++)
        {
            Assert.Equal(PinResult.Full, board.Toggle(key));
        }

        Assert.True((GC.GetAllocatedBytesForCurrentThread() - before) / Calls <= MaxBytesPerCall);
    }
}
