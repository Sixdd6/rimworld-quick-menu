using System.Collections.Generic;
using System.Linq;
using QuickMenu.Core.Pins;
using QuickMenu.Core.Settings;
using Xunit;

namespace QuickMenu.Core.Tests.Pins;

public class PinBoardAutoTests
{
    private sealed class Resolver(params PinKey[] missing) : IPinResolver
    {
        private readonly HashSet<PinKey> _missing = new(missing);

        public bool IsResolved(PinKey key) => !_missing.Contains(key);
    }

    private static PinKey K(string n) => PinKey.Build(PinKeyKind.BuildThing, n);

    private static string[] Names(PinBoard b) => b.Entries.Select(e => e?.Key.Value ?? "-").ToArray();

    private static PinBoard Auto(int capacity, params string[] names)
    {
        var b = new PinBoard(capacity) { Mode = SizingMode.Auto };
        foreach (string n in names)
        {
            b.Toggle(K(n));
        }

        return b;
    }

    [Fact]
    public void New_pin_is_appended_at_the_end()
    {
        PinBoard b = Auto(10, "A", "B");
        Assert.Equal(PinResult.Pinned, b.Toggle(K("C")));
        Assert.Equal(new[] { "build:thing/A", "build:thing/B", "build:thing/C" }, Names(b));
    }

    [Fact]
    public void Toggle_is_Full_at_capacity_and_unpin_still_works()
    {
        PinBoard b = Auto(2, "A", "B");
        Assert.Equal(PinResult.Full, b.Toggle(K("C")));
        Assert.Equal(PinResult.Unpinned, b.Toggle(K("A")));
        Assert.Equal(new[] { "build:thing/B" }, Names(b));
    }

    [Fact]
    public void Clear_moves_later_pins_up()
    {
        PinBoard b = Auto(10, "A", "B", "C");
        Assert.True(b.Clear(0));
        Assert.Equal(new[] { "build:thing/B", "build:thing/C" }, Names(b));
        Assert.False(b.Clear(2));
        Assert.False(b.Clear(-1));
    }

    [Fact]
    public void Unpin_by_toggle_moves_later_pins_up()
    {
        PinBoard b = Auto(10, "A", "B", "C");
        b.Toggle(K("B"));
        Assert.Equal(new[] { "build:thing/A", "build:thing/C" }, Names(b));
    }

    [Fact]
    public void Unresolved_pins_are_hidden_but_keep_their_order()
    {
        PinBoard b = Auto(10, "A", "B", "C");
        IReadOnlyList<PinSlot> v = b.VisibleSlots(new Resolver(K("B")));
        Assert.Equal(new[] { 0, 2 }, v.Select(s => s.Index).ToArray());
        Assert.All(v, s => Assert.Equal(PinSlotState.Resolved, s.State));
        Assert.Equal(new[] { "build:thing/A", "build:thing/B", "build:thing/C" }, Names(b));
        b.Toggle(K("D"));
        Assert.Equal("build:thing/D", b.Entries[3]!.Key.Value);
    }

    [Fact]
    public void Opaque_pins_are_hidden_in_Auto()
    {
        PinBoard b = PinSerializer.FromLines(new[] { "0|build:thing/A||A", "1|future:thing/X|some.mod|Label" }).Board;
        b.Mode = SizingMode.Auto;
        b.Capacity = 10;
        Assert.Single(b.VisibleSlots(new Resolver()));
        Assert.Equal(1, b.CountUnresolved(new Resolver()));
    }

    [Fact]
    public void Swap_exchanges_two_pins_and_keeps_the_others()
    {
        PinBoard b = Auto(10, "A", "B", "C", "D");
        Assert.True(b.Swap(0, 2));
        Assert.Equal(new[] { "build:thing/C", "build:thing/B", "build:thing/A", "build:thing/D" }, Names(b));
        Assert.False(b.Swap(0, 4));
        Assert.False(b.Swap(-1, 1));
        Assert.Equal(new[] { "build:thing/C", "build:thing/B", "build:thing/A", "build:thing/D" }, Names(b));
    }

    [Fact]
    public void Fixed_to_Auto_closes_gaps_in_order_and_loses_nothing()
    {
        var b = new PinBoard(5);
        b.SetSlot(0, new PinEntry(K("A")));
        b.SetSlot(3, new PinEntry(K("B")));
        b.SetSlot(7, new PinEntry(K("C"))); // hidden beyond capacity
        b.Mode = SizingMode.Auto;
        Assert.Equal(new[] { "build:thing/A", "build:thing/B", "build:thing/C" }, Names(b));
    }

    [Fact]
    public void Auto_to_Fixed_keeps_order_and_every_pin()
    {
        PinBoard b = Auto(2, "A", "B");
        b.SetSlot(2, new PinEntry(K("C")));
        b.Mode = SizingMode.Fixed;
        Assert.Equal(SizingMode.Fixed, b.Mode);
        Assert.Equal(new[] { "build:thing/A", "build:thing/B", "build:thing/C" }, Names(b));
        Assert.Equal(2, b.VisibleSlots(new Resolver()).Count);
        b.Clear(0);
        Assert.Equal(new[] { "-", "build:thing/B", "build:thing/C" }, Names(b));
    }

    [Fact]
    public void Count_and_forget_unresolved_remove_only_unresolved_and_close_gaps()
    {
        PinBoard b = Auto(10, "A", "B", "C", "D");
        var r = new Resolver(K("B"), K("D"));
        Assert.Equal(2, b.CountUnresolved(r));
        Assert.Equal(2, b.ForgetUnresolved(r));
        Assert.Equal(new[] { "build:thing/A", "build:thing/C" }, Names(b));
        Assert.Equal(0, b.CountUnresolved(r));
        Assert.Equal(0, b.ForgetUnresolved(r));
    }

    [Fact]
    public void Fixed_forget_keeps_other_slots_in_place()
    {
        var b = new PinBoard(4);
        b.Toggle(K("A"));
        b.Toggle(K("B"));
        b.Toggle(K("C"));
        Assert.Equal(1, b.ForgetUnresolved(new Resolver(K("B"))));
        Assert.Equal(new[] { "build:thing/A", "-", "build:thing/C" }, Names(b));
    }

    [Fact]
    public void Fixed_swap_into_empty_slot_moves_the_pin()
    {
        var b = new PinBoard(4);
        b.Toggle(K("A"));
        Assert.True(b.Swap(0, 3));
        Assert.Equal(new[] { "-", "-", "-", "build:thing/A" }, Names(b));
    }

    [Fact]
    public void SetSlot_in_Auto_keeps_the_list_compact()
    {
        PinBoard b = Auto(10, "A");
        b.SetSlot(4, new PinEntry(K("B")));
        Assert.Equal(new[] { "build:thing/A", "build:thing/B" }, Names(b));
    }
}
