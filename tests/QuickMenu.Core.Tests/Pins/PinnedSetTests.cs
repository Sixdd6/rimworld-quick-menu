using System.Collections.Generic;
using QuickMenu.Core.Pins;
using QuickMenu.Core.Settings;
using Xunit;

namespace QuickMenu.Core.Tests.Pins;

public class PinnedSetTests
{
    private sealed class Obj
    {
        public override bool Equals(object? obj) => true; // identity must win over Equals
        public override int GetHashCode() => 1;
    }

    private sealed class Def
    {
    }

    private sealed class Resolver : IPinTargetResolver<Obj, Def>
    {
        public Dictionary<string, List<PinTarget<Obj, Def>>> Map { get; } = new();

        public void Resolve(PinKey key, List<PinTarget<Obj, Def>> into)
        {
            if (Map.TryGetValue(key.Value, out var targets))
            {
                into.AddRange(targets);
            }
        }

        public PinKey Add(PinKey key, params PinTarget<Obj, Def>[] targets)
        {
            Map[key.Value] = new List<PinTarget<Obj, Def>>(targets);
            return key;
        }
    }

    private static PinKey Thing(string n) => PinKey.Build(PinKeyKind.BuildThing, n);

    private static PinBoard Board(int capacity, params PinKey?[] slots)
    {
        var b = new PinBoard(capacity);
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] is { } k)
            {
                b.SetSlot(i, new PinEntry(k));
            }
        }

        return b;
    }

    [Fact]
    public void Whole_dropdown_pin_marks_the_dropdown_Full_and_not_its_elements()
    {
        var drop = new Obj();
        var child = new Obj();
        var r = new Resolver();
        PinKey k = r.Add(PinKey.Dropdown("Floors"), new PinTarget<Obj, Def>(drop));
        var set = new PinnedSet<Obj, Def>();
        set.Rebuild(Board(4, k), r);
        Assert.Equal(PinMark.Full, set.Lookup(drop));
        Assert.Equal(PinMark.None, set.Lookup(child));
    }

    [Fact]
    public void Single_entry_pin_marks_the_entry_Full_and_its_dropdown_Partial()
    {
        var drop = new Obj();
        var entry = new Obj();
        var def = new Def();
        var r = new Resolver();
        PinKey k = r.Add(Thing("Carpet"), new PinTarget<Obj, Def>(entry, drop, def));
        var set = new PinnedSet<Obj, Def>();
        set.Rebuild(Board(4, k), r);
        Assert.Equal(PinMark.Full, set.Lookup(entry));
        Assert.Equal(PinMark.Partial, set.Lookup(drop));
        Assert.Equal(PinMark.Full, set.LookupDef(def));
        Assert.True(set.Contains(drop));
    }

    [Fact]
    public void Whole_pin_wins_over_partial_in_either_order()
    {
        var drop = new Obj();
        var entry = new Obj();
        var r = new Resolver();
        PinKey whole = r.Add(PinKey.Dropdown("Floors"), new PinTarget<Obj, Def>(drop));
        PinKey single = r.Add(Thing("Carpet"), new PinTarget<Obj, Def>(entry, drop));

        var a = new PinnedSet<Obj, Def>();
        a.Rebuild(Board(4, whole, single), r);
        Assert.Equal(PinMark.Full, a.Lookup(drop));

        var b = new PinnedSet<Obj, Def>();
        b.Rebuild(Board(4, single, whole), r);
        Assert.Equal(PinMark.Full, b.Lookup(drop));
    }

    [Fact]
    public void Unresolved_opaque_and_missing_pins_add_nothing()
    {
        var r = new Resolver();
        var set = new PinnedSet<Obj, Def>();
        var board = PinSerializer.FromLines(new[] { "0|future:a|pk|x", "1|build:thing/Gone||G" }).Board;
        board.Capacity = 2;
        Assert.Equal(PinKeyKind.Opaque, board.Entries[0]!.Key.Kind);
        var spy = new Spy();
        set.Rebuild(board, spy);
        Assert.Equal(1, spy.Calls);
        Assert.Equal(PinKeyKind.BuildThing, spy.Keys[0].Kind);
        set.Rebuild(board, r);
        Assert.True(set.IsEmpty);
    }

    [Fact]
    public void HasDefs_is_true_only_when_a_def_entry_exists()
    {
        var set = new PinnedSet<Obj, Def>();
        Assert.False(set.HasDefs);
        set.Rebuild(new[] { new PinTarget<Obj, Def>(new Obj()) });
        Assert.False(set.HasDefs);
        set.Rebuild(new[] { new PinTarget<Obj, Def>(new Obj(), null, new Def()) });
        Assert.True(set.HasDefs);
        set.Rebuild(new List<PinTarget<Obj, Def>>());
        Assert.False(set.HasDefs);
    }

    private sealed class ResolvedSet : IPinResolver
    {
        public HashSet<string> Values { get; } = new();

        public bool IsResolved(PinKey key) => Values.Contains(key.Value);
    }

    [Theory]
    [InlineData(SizingMode.Fixed)]
    [InlineData(SizingMode.Auto)]
    public void Collected_targets_equal_the_visible_resolved_slots(SizingMode mode)
    {
        for (int seed = 1; seed <= 40; seed++)
        {
            var rng = new System.Random(seed);
            var board = new PinBoard(rng.Next(1, 6));
            board.Mode = mode;
            int count = rng.Next(0, 12);
            var resolver = new Resolver();
            var resolved = new ResolvedSet();
            var instances = new Dictionary<string, Obj>();
            for (int i = 0; i < count; i++)
            {
                if (rng.Next(4) == 0)
                {
                    continue; // gap
                }

                PinKey key = Thing("T" + seed + "_" + i);
                board.SetSlot(i, new PinEntry(key));
                if (rng.Next(3) != 0)
                {
                    resolved.Values.Add(key.Value);
                    var obj = new Obj();
                    instances[key.Value] = obj;
                    resolver.Add(key, new PinTarget<Obj, Def>(obj));
                }
            }

            var expected = new HashSet<Obj>(ReferenceEqualityComparer.Instance);
            foreach (PinSlot slot in board.VisibleSlots(resolved))
            {
                if (slot.State == PinSlotState.Resolved)
                {
                    expected.Add(instances[slot.Entry!.Key.Value]);
                }
            }

            var set = new PinnedSet<Obj, Def>();
            set.Rebuild(board, resolver);
            foreach (Obj obj in instances.Values)
            {
                Assert.Equal(expected.Contains(obj) ? PinMark.Full : PinMark.None, set.Lookup(obj));
            }

            Assert.Equal(expected.Count == 0, set.IsEmpty);
        }
    }

    [Fact]
    public void Opaque_key_is_never_passed_to_the_resolver()
    {
        PinBoard board = PinSerializer.FromLines(new[] { "0|future:a|pk|x" }).Board;
        board.Capacity = 2;
        var r = new Spy();
        new PinnedSet<Obj, Def>().Rebuild(board, r);
        Assert.Equal(0, r.Calls);
    }

    private sealed class Spy : IPinTargetResolver<Obj, Def>
    {
        public int Calls { get; private set; }

        public List<PinKey> Keys { get; } = new();

        public void Resolve(PinKey key, List<PinTarget<Obj, Def>> into)
        {
            Calls++;
            Keys.Add(key);
        }
    }

    [Fact]
    public void Pins_hidden_beyond_capacity_are_not_marked_in_Fixed_mode_and_return_when_capacity_grows()
    {
        var shown = new Obj();
        var hidden = new Obj();
        var r = new Resolver();
        PinKey a = r.Add(Thing("A"), new PinTarget<Obj, Def>(shown));
        PinKey b = r.Add(Thing("B"), new PinTarget<Obj, Def>(hidden));
        PinBoard board = Board(2, a, null, null, b);
        board.Capacity = 2;
        var set = new PinnedSet<Obj, Def>();
        set.Rebuild(board, r);
        Assert.Equal(PinMark.Full, set.Lookup(shown));
        Assert.Equal(PinMark.None, set.Lookup(hidden));
        board.Capacity = 4;
        set.Rebuild(board, r);
        Assert.Equal(PinMark.Full, set.Lookup(hidden));
    }

    [Fact]
    public void Auto_mode_marks_every_resolved_pin_and_skips_unresolved()
    {
        var x = new Obj();
        var y = new Obj();
        var r = new Resolver();
        PinKey a = r.Add(Thing("A"), new PinTarget<Obj, Def>(x));
        PinKey b = r.Add(Thing("B"), new PinTarget<Obj, Def>(y));
        var board = new PinBoard(10) { Mode = SizingMode.Auto };
        board.Toggle(a);
        board.Toggle(Thing("Gone"));
        board.Toggle(b);
        var set = new PinnedSet<Obj, Def>();
        set.Rebuild(board, r);
        Assert.Equal(PinMark.Full, set.Lookup(x));
        Assert.Equal(PinMark.Full, set.Lookup(y));
    }

    [Fact]
    public void Membership_follows_pin_and_unpin_and_ignores_swap()
    {
        var x = new Obj();
        var y = new Obj();
        var r = new Resolver();
        PinKey a = r.Add(Thing("A"), new PinTarget<Obj, Def>(x));
        PinKey b = r.Add(Thing("B"), new PinTarget<Obj, Def>(y));
        var board = new PinBoard(4);
        var set = new PinnedSet<Obj, Def>();
        set.Rebuild(board, r);
        Assert.True(set.IsEmpty);
        board.Toggle(a);
        board.Toggle(b);
        set.Rebuild(board, r);
        Assert.True(set.Contains(x) && set.Contains(y));
        board.Swap(0, 1);
        set.Rebuild(board, r);
        Assert.True(set.Contains(x) && set.Contains(y));
        board.Toggle(a);
        set.Rebuild(board, r);
        Assert.False(set.Contains(x));
        Assert.True(set.Contains(y));
    }

    [Fact]
    public void Duplicate_instances_for_one_key_are_all_marked()
    {
        var x1 = new Obj();
        var x2 = new Obj();
        var r = new Resolver();
        PinKey a = r.Add(Thing("A"), new PinTarget<Obj, Def>(x1), new PinTarget<Obj, Def>(x2));
        var set = new PinnedSet<Obj, Def>();
        set.Rebuild(Board(2, a), r);
        Assert.True(set.Contains(x1) && set.Contains(x2));
    }

    [Fact]
    public void Lookup_uses_reference_identity_not_Equals()
    {
        var x = new Obj();
        var set = new PinnedSet<Obj, Def>();
        set.Rebuild(new[] { new PinTarget<Obj, Def>(x) });
        Assert.Equal(PinMark.None, set.Lookup(new Obj()));
        Assert.Equal(PinMark.None, set.Lookup(null));
        Assert.Equal(PinMark.None, set.LookupDef(null));
    }

    [Fact]
    public void Def_map_marks_a_copy_when_the_identity_misses()
    {
        var def = new Def();
        var original = new Obj();
        var copy = new Obj();
        var set = new PinnedSet<Obj, Def>();
        set.Rebuild(new[] { new PinTarget<Obj, Def>(original, null, def) });
        Assert.Equal(PinMark.None, set.Lookup(copy));
        Assert.Equal(PinMark.Full, set.Lookup(copy, def));
        Assert.Equal(PinMark.None, set.Lookup(copy, new Def()));
    }

    [Fact]
    public void A_target_without_an_instance_is_ignored()
    {
        var set = new PinnedSet<Obj, Def>();
        set.Rebuild(new[] { new PinTarget<Obj, Def>(null, new Obj(), new Def()) });
        Assert.True(set.IsEmpty);
    }

    [Fact]
    public void Version_increments_on_each_rebuild()
    {
        var set = new PinnedSet<Obj, Def>();
        Assert.Equal(0, set.Version);
        set.Rebuild(new List<PinTarget<Obj, Def>>());
        set.Rebuild(new Board_NoOp().Board, new Resolver());
        Assert.Equal(2, set.Version);
    }

    private sealed class Board_NoOp
    {
        public PinBoard Board { get; } = new(1);
    }

    [Fact]
    public void Rebuild_rejects_null_arguments()
    {
        var set = new PinnedSet<Obj, Def>();
        Assert.Throws<System.ArgumentNullException>(() => set.Rebuild((PinBoard)null!, new Resolver()));
        Assert.Throws<System.ArgumentNullException>(() => set.Rebuild(new PinBoard(1), null!));
        Assert.Throws<System.ArgumentNullException>(() => set.Rebuild((IReadOnlyList<PinTarget<Obj, Def>>)null!));
    }
}
