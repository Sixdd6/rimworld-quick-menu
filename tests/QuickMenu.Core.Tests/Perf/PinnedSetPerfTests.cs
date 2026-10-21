using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using QuickMenu.Core.Pins;
using Xunit;

namespace QuickMenu.Core.Tests.Perf;

/// <summary>P-19 (Core part): the marker lookup costs at most 100 ns and 0 bytes per call; the gate fails above 5 times that.</summary>
[Trait("Category", "Perf")]
public class PinnedSetPerfTests
{
    private const int Warmup = 2_000;
    private const int Calls = 10_000;
    private const double BudgetNs = 100.0;
    private const double GateFactor = 5.0;

    private sealed class Item
    {
    }

    private sealed class Def
    {
    }

    private static (PinnedSet<Item, Def> Set, Item[] Members, Item[] Others, Def[] Defs) Fixture(int pins = 200)
    {
        var members = new Item[pins];
        var defs = new Def[pins];
        var targets = new List<PinTarget<Item, Def>>(pins);
        for (int i = 0; i < pins; i++)
        {
            members[i] = new Item();
            defs[i] = new Def();
            targets.Add(new PinTarget<Item, Def>(members[i], i % 5 == 0 ? new Item() : null, defs[i]));
        }

        var set = new PinnedSet<Item, Def>();
        set.Rebuild(targets);
        var others = new Item[pins];
        for (int i = 0; i < pins; i++)
        {
            others[i] = new Item();
        }

        return (set, members, others, defs);
    }

    private static int Sweep(PinnedSet<Item, Def> set, Item[] members, Item[] others, Def[] defs, int n)
    {
        int hits = 0;
        for (int i = 0; i < n; i++)
        {
            int k = i % members.Length;
            hits += (int)set.Lookup(members[k]);
            hits += (int)set.Lookup(others[k], defs[k]);
            hits += (int)set.Lookup(others[k]);
            hits += (int)set.Lookup(null);
            if (set.Contains(members[k]))
            {
                hits++;
            }
        }

        return hits;
    }

    [Fact]
    public void Lookups_allocate_zero_bytes()
    {
        var (set, m, o, d) = Fixture();
        int sink = Sweep(set, m, o, d, Warmup);
        long before = GC.GetAllocatedBytesForCurrentThread();
        sink += Sweep(set, m, o, d, Calls);
        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
        Assert.NotEqual(-1, sink);
    }

    [Fact]
    public void IsEmpty_and_empty_set_lookup_allocate_zero_bytes()
    {
        var empty = new PinnedSet<Item, Def>();
        var probe = new Item();
        var def = new Def();
        int sink = 0;
        for (int i = 0; i < Warmup; i++)
        {
            sink += empty.IsEmpty ? 1 : 0;
            sink += (int)empty.Lookup(probe, def);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < Calls; i++)
        {
            sink += empty.IsEmpty ? 1 : 0;
            sink += (int)empty.Lookup(probe, def);
            sink += (int)empty.Lookup(probe);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
        Assert.NotEqual(-1, sink);
    }

    [Fact]
    public void Lookups_stay_under_five_times_the_100ns_budget()
    {
        var (set, m, o, d) = Fixture();
        int sink = Sweep(set, m, o, d, Warmup);
        double[] ns = new double[5];
        for (int run = 0; run < ns.Length; run++)
        {
            long start = Stopwatch.GetTimestamp();
            sink += Sweep(set, m, o, d, Calls);
            ns[run] = Stopwatch.GetElapsedTime(start).TotalMilliseconds * 1_000_000.0 / (Calls * 5.0);
        }

        Array.Sort(ns);
        Assert.True(ns[2] <= BudgetNs * GateFactor, $"median {ns[2]:F1} ns per lookup, limit {BudgetNs * GateFactor} ns");
        Assert.NotEqual(-1, sink);
    }

    [Fact]
    public void Rebuild_of_200_pins_reuses_its_storage()
    {
        var (set, _, _, _) = Fixture();
        var targets = new List<PinTarget<Item, Def>>();
        for (int i = 0; i < 200; i++)
        {
            targets.Add(new PinTarget<Item, Def>(new Item(), null, new Def()));
        }

        // Warm past tiered JIT promotion (30 calls plus the background tier-1 compile): with only 20 warm-up rebuilds CI
        // occasionally counted ~7 KB of runtime allocations inside the measured loop.
        for (int i = 0; i < 300; i++)
        {
            set.Rebuild(targets);
        }

        Thread.Sleep(200);
        set.Rebuild(targets);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++)
        {
            set.Rebuild(targets);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
