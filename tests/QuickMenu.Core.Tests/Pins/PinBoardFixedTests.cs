using System.Collections.Generic;
using QuickMenu.Core.Pins;
using QuickMenu.Core.Settings;
using Xunit;

namespace QuickMenu.Core.Tests.Pins;

public class PinBoardFixedTests
{
    private sealed class Resolver(params PinKey[] missing) : IPinResolver
    {
        private readonly HashSet<PinKey> _missing = new(missing);

        public bool IsResolved(PinKey key) => !_missing.Contains(key);
    }

    private static PinKey K(string n) => PinKey.Build(PinKeyKind.BuildThing, n);

    private static string?[] Keys(PinBoard b, int n)
    {
        var r = new string?[n];
        for (int i = 0; i < n; i++)
        {
            r[i] = i < b.Entries.Count ? b.Entries[i]?.Key.Value : null;
        }

        return r;
    }

    [Fact]
    public void Toggle_pins_into_first_free_slot()
    {
        var b = new PinBoard(3);
        Assert.Equal(PinResult.Pinned, b.Toggle(K("A")));
        Assert.Equal(PinResult.Pinned, b.Toggle(K("B")));
        Assert.Equal("build:thing/B", b.Entries[1]!.Key.Value);
    }

    [Fact]
    public void Toggle_stores_label_and_package()
    {
        var b = new PinBoard(2);
        b.Toggle(K("A"), "Wall", "mod.x");
        Assert.Equal("Wall", b.Entries[0]!.Label);
        Assert.Equal("mod.x", b.Entries[0]!.PackageId);
    }

    [Fact]
    public void Toggle_unpins_in_place_leaving_a_hole_that_is_reused()
    {
        var b = new PinBoard(3);
        b.Toggle(K("A"));
        b.Toggle(K("B"));
        b.Toggle(K("C"));
        Assert.Equal(PinResult.Unpinned, b.Toggle(K("B")));
        Assert.Equal(new string?[] { "build:thing/A", null, "build:thing/C" }, Keys(b, 3));
        Assert.Equal(PinResult.Pinned, b.Toggle(K("D")));
        Assert.Equal("build:thing/D", b.Entries[1]!.Key.Value);
    }

    [Fact]
    public void Toggle_never_duplicates()
    {
        var b = new PinBoard(3);
        b.Toggle(K("A"));
        b.Toggle(K("A"));
        Assert.Empty(b.Entries);
    }

    [Fact]
    public void Toggle_when_all_slots_occupied_is_Full_and_changes_nothing()
    {
        var b = new PinBoard(2);
        b.Toggle(K("A"));
        b.Toggle(K("B"));
        Assert.Equal(PinResult.Full, b.Toggle(K("C")));
        Assert.Equal(new string?[] { "build:thing/A", "build:thing/B" }, Keys(b, 2));
    }

    [Fact]
    public void Zero_capacity_is_Full()
    {
        Assert.Equal(PinResult.Full, new PinBoard(0).Toggle(K("A")));
    }

    [Fact]
    public void Swap_exchanges_and_keeps_others()
    {
        var b = new PinBoard(4);
        b.Toggle(K("A"));
        b.Toggle(K("B"));
        b.Toggle(K("C"));
        Assert.True(b.Swap(0, 2));
        Assert.Equal(new string?[] { "build:thing/C", "build:thing/B", "build:thing/A", null }, Keys(b, 4));
        Assert.True(b.Swap(0, 3)); // into an empty slot
        Assert.Equal(new string?[] { null, "build:thing/B", "build:thing/A", "build:thing/C" }, Keys(b, 4));
    }

    [Fact]
    public void Swap_out_of_range_changes_nothing()
    {
        var b = new PinBoard(2);
        b.Toggle(K("A"));
        Assert.False(b.Swap(0, 2));
        Assert.False(b.Swap(-1, 0));
        Assert.Equal("build:thing/A", b.Entries[0]!.Key.Value);
    }

    [Fact]
    public void Clear_empties_in_place()
    {
        var b = new PinBoard(3);
        b.Toggle(K("A"));
        b.Toggle(K("B"));
        b.Toggle(K("C"));
        Assert.True(b.Clear(1));
        Assert.Equal(new string?[] { "build:thing/A", null, "build:thing/C" }, Keys(b, 3));
        Assert.True(b.Clear(2)); // empty already is fine
        Assert.False(b.Clear(3));
    }

    [Fact]
    public void Unresolved_slots_are_reserved_and_skipped_by_Toggle()
    {
        var b = new PinBoard(3);
        b.Toggle(K("Gone"));
        var r = new Resolver(K("Gone"));
        Assert.Equal(PinResult.Pinned, b.Toggle(K("B")));
        Assert.Equal("build:thing/B", b.Entries[1]!.Key.Value);
        IReadOnlyList<PinSlot> v = b.VisibleSlots(r);
        Assert.Equal(PinSlotState.Unresolved, v[0].State);
        Assert.Equal(PinSlotState.Resolved, v[1].State);
        Assert.Equal(PinSlotState.Empty, v[2].State);
        b.Toggle(K("C"));
        Assert.Equal(PinResult.Full, b.Toggle(K("D")));
    }

    [Fact]
    public void Unresolved_pin_is_kept_unchanged_through_swap()
    {
        var b = new PinBoard(2);
        b.Toggle(K("Gone"), "Old", "mod.gone");
        b.Swap(0, 1);
        Assert.Null(b.Entries[0]);
        Assert.Equal("Old", b.Entries[1]!.Label);
        Assert.Equal("mod.gone", b.Entries[1]!.PackageId);
    }

    [Fact]
    public void Shrinking_hides_pins_and_growing_restores_them()
    {
        var b = new PinBoard(4);
        for (char c = 'A'; c <= 'D'; c++)
        {
            b.Toggle(K(c.ToString()));
        }

        b.Capacity = 2;
        Assert.Equal(2, b.VisibleSlots(new Resolver()).Count);
        Assert.Equal(PinResult.Full, b.Toggle(K("E")));
        Assert.Equal(4, b.Entries.Count);
        b.Capacity = 4;
        Assert.Equal(new string?[] { "build:thing/A", "build:thing/B", "build:thing/C", "build:thing/D" }, Keys(b, 4));
    }

    [Fact]
    public void Hidden_pin_cannot_be_duplicated_and_can_be_unpinned()
    {
        var b = new PinBoard(3);
        b.Toggle(K("A"));
        b.Toggle(K("B"));
        b.Toggle(K("C"));
        b.Capacity = 1;
        Assert.Equal(PinResult.Unpinned, b.Toggle(K("C")));
        Assert.Equal(2, b.Entries.Count);
    }

    [Fact]
    public void CountUnresolved_and_ForgetUnresolved()
    {
        var b = new PinBoard(3);
        b.Toggle(K("A"));
        b.Toggle(K("Gone1"));
        b.Toggle(K("Gone2"));
        b.Capacity = 2; // Gone2 hidden
        var r = new Resolver(K("Gone1"), K("Gone2"));
        Assert.Equal(2, b.CountUnresolved(r));
        Assert.Equal(2, b.ForgetUnresolved(r));
        Assert.Equal(0, b.CountUnresolved(r));
        Assert.Equal(new string?[] { "build:thing/A" }, Keys(b, 1));
        Assert.Equal(0, b.ForgetUnresolved(r));
    }

    [Fact]
    public void Forget_predicate_removes_only_matching_and_keeps_opaque_pins()
    {
        PinBoard b = PinSerializer.FromLines(new[] { "0|future:x||L", "1|build:thing/Gone||", "2|build:thing/B||" }).Board;
        b.Capacity = 3;
        Assert.Equal(1, b.Forget(e => e.Key.Value == "build:thing/Gone"));
        Assert.Equal(new[] { "0|future:x||L", "2|build:thing/B||" }, PinSerializer.ToLines(b));
    }

    [Fact]
    public void VisibleCount_fixed_counts_pins_below_capacity_only()
    {
        var b = new PinBoard(3);
        b.Toggle(K("A"));
        b.Toggle(K("Gone"));
        b.Toggle(K("C"));
        b.Capacity = 2;
        Assert.Equal(2, b.VisibleCount(new Resolver(K("Gone"))));
    }

    [Fact]
    public void VisibleCount_auto_counts_resolved_pins_only()
    {
        var b = new PinBoard(10);
        b.Toggle(K("A"));
        b.Toggle(K("Gone"));
        b.Toggle(K("C"));
        b.Mode = SizingMode.Auto;
        Assert.Equal(2, b.VisibleCount(new Resolver(K("Gone"))));
    }

    [Fact]
    public void Switching_to_Auto_and_back_without_edits_restores_the_fixed_positions()
    {
        var b = new PinBoard(5);
        b.SetSlot(1, new PinEntry(K("A"), "", null));
        b.SetSlot(4, new PinEntry(K("B"), "", null));
        b.Mode = SizingMode.Auto;
        Assert.Equal(new string?[] { "build:thing/A", "build:thing/B" }, Keys(b, 2));
        b.Mode = SizingMode.Fixed;
        Assert.Equal(new string?[] { null, "build:thing/A", null, null, "build:thing/B" }, Keys(b, 5));
    }

    [Fact]
    public void Switching_back_to_Fixed_after_an_edit_in_Auto_keeps_the_compact_order()
    {
        var b = new PinBoard(5);
        b.SetSlot(1, new PinEntry(K("A"), "", null));
        b.SetSlot(4, new PinEntry(K("B"), "", null));
        b.Mode = SizingMode.Auto;
        b.Toggle(K("C"));
        b.Mode = SizingMode.Fixed;
        Assert.Equal(new string?[] { "build:thing/A", "build:thing/B", "build:thing/C", null, null }, Keys(b, 5));
    }

    [Fact]
    public void ForgetUnresolved_keeps_resolved_in_place()
    {
        var b = new PinBoard(3);
        b.Toggle(K("Gone"));
        b.Toggle(K("B"));
        b.ForgetUnresolved(new Resolver(K("Gone")));
        Assert.Equal(new string?[] { null, "build:thing/B", null }, Keys(b, 3));
    }

    [Fact]
    public void Negative_capacity_throws()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new PinBoard(-1));
    }
}
