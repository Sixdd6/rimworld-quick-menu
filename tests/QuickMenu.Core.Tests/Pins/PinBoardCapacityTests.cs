using System.Collections.Generic;
using QuickMenu.Core.Pins;
using Xunit;

namespace QuickMenu.Core.Tests.Pins;

public class PinBoardCapacityTests
{
    private sealed class Resolver(params PinKey[] missing) : IPinResolver
    {
        private readonly HashSet<PinKey> _missing = new(missing);

        public bool IsResolved(PinKey key) => !_missing.Contains(key);
    }

    private static PinKey K(string n) => PinKey.Build(PinKeyKind.BuildThing, n);

    [Fact]
    public void Shrink_with_visible_full_and_hidden_free_is_Full_not_hidden_pin()
    {
        var b = new PinBoard(4);
        b.Toggle(K("A"));
        b.Toggle(K("B"));
        b.Capacity = 2;

        Assert.Equal(PinResult.Full, b.Toggle(K("C")));
        Assert.Equal(2, b.Entries.Count);
    }

    [Fact]
    public void Hidden_unresolved_pin_survives_shrink_grow_and_stays_reserved()
    {
        var b = new PinBoard(4);
        b.SetSlot(3, new PinEntry(K("Gone"), "Gone", "x.mod"));
        b.Capacity = 2;
        var res = new Resolver(K("Gone"));

        Assert.Equal(1, b.CountUnresolved(res));
        Assert.Equal(2, b.VisibleSlots(res).Count);

        b.Capacity = 4;
        Assert.Equal(PinSlotState.Unresolved, b.VisibleSlots(res)[3].State);
        Assert.Equal("x.mod", b.VisibleSlots(res)[3].Entry?.PackageId);

        // Fill the three free slots; the fourth toggle must be Full because slot 3 is reserved.
        Assert.Equal(PinResult.Pinned, b.Toggle(K("A")));
        Assert.Equal(PinResult.Pinned, b.Toggle(K("B")));
        Assert.Equal(PinResult.Pinned, b.Toggle(K("C")));
        Assert.Equal(PinResult.Full, b.Toggle(K("D")));
        Assert.Equal(K("Gone"), b.Entries[3]?.Key);
    }

    [Fact]
    public void Swap_and_Clear_on_hidden_slots_are_refused_and_change_nothing()
    {
        var b = new PinBoard(4);
        b.SetSlot(0, new PinEntry(K("A")));
        b.SetSlot(3, new PinEntry(K("D")));
        b.Capacity = 2;

        Assert.False(b.Swap(0, 3));
        Assert.False(b.Clear(3));
        Assert.Equal(K("D"), b.Entries[3]?.Key);
        Assert.Equal(K("A"), b.Entries[0]?.Key);
    }

    [Fact]
    public void Capacity_zero_keeps_everything_and_regrow_restores_positions()
    {
        var b = new PinBoard(3);
        b.SetSlot(0, new PinEntry(K("A")));
        b.SetSlot(2, new PinEntry(K("C")));
        b.Capacity = 0;
        Assert.Empty(b.VisibleSlots(new Resolver()));
        b.Capacity = 3;
        Assert.Equal(K("A"), b.Entries[0]?.Key);
        Assert.Null(b.Entries[1]);
        Assert.Equal(K("C"), b.Entries[2]?.Key);
    }

    [Fact]
    public void Cursor_on_a_slot_hidden_by_shrinking_never_receives_a_pin()
    {
        var b = new PinBoard(4);
        b.SetCursor(3);
        b.Capacity = 2;

        Assert.Equal(PinResult.Pinned, b.Toggle(K("A")));
        Assert.Equal(K("A"), b.Entries[0]?.Key);
        Assert.Single(b.Entries);
        b.Capacity = 4;
        Assert.Null(b.Cursor);
    }

    [Fact]
    public void Shrunk_cursor_is_not_reported_as_visible()
    {
        var b = new PinBoard(4);
        b.SetCursor(2);
        b.Capacity = 2;
        Assert.Null(b.Cursor);

        b.SetCursor(1);
        b.Capacity = 2;
        Assert.Equal(1, b.Cursor);
    }

    [Fact]
    public void Entries_is_a_read_only_view()
    {
        var b = new PinBoard(2);
        Assert.False(b.Entries is List<PinEntry?>);
        Assert.False(b.Entries is PinEntry?[]);
    }
}
