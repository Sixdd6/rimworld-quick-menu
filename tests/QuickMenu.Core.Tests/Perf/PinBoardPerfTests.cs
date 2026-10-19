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

    private sealed class Marked
    {
    }

    private sealed class NoDef
    {
    }

    private sealed class Resolver : IPinTargetResolver<Marked, NoDef>
    {
        private readonly Marked _shared = new();

        public void Resolve(PinKey key, System.Collections.Generic.List<PinTarget<Marked, NoDef>> into) =>
            into.Add(new PinTarget<Marked, NoDef>(_shared));
    }

    [Fact]
    public void Toggle_followed_by_a_marker_set_rebuild_allocates_at_most_2KB_per_click()
    {
        PinBoard board = SyntheticCatalog.Pins(SyntheticCatalog.PinCount - 1);
        board.Capacity = SyntheticCatalog.PinCount;
        PinKey key = PinKey.Build(PinKeyKind.BuildThing, "Perf_Toggle_Target");
        var set = new PinnedSet<Marked, NoDef>();
        var resolver = new Resolver();
        for (int i = 0; i < 100; i++)
        {
            board.Toggle(key, "Perf label", "author.mod");
            set.Rebuild(board, resolver);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < Calls; i++)
        {
            board.Toggle(key, "Perf label", "author.mod");
            set.Rebuild(board, resolver);
        }

        long perClick = (GC.GetAllocatedBytesForCurrentThread() - before) / Calls;
        Assert.True(perClick <= MaxBytesPerCall, $"{perClick} bytes per toggle plus rebuild");
    }
}
