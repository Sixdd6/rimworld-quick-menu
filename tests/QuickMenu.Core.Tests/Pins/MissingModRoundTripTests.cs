using System.Collections.Generic;
using System.Linq;
using QuickMenu.Core.Pins;
using QuickMenu.Core.Settings;
using Xunit;

namespace QuickMenu.Core.Tests.Pins;

/// <summary>T-66: a pin from a removed mod survives save/load unchanged, is reserved or hidden, counted, and returns in place.</summary>
public class MissingModRoundTripTests
{
    private sealed class Resolver(params PinKey[] missing) : IPinResolver
    {
        private readonly HashSet<PinKey> _missing = new(missing);

        public bool IsResolved(PinKey key) => !_missing.Contains(key);
    }

    private static PinKey K(string n) => PinKey.Build(PinKeyKind.BuildThing, n);

    private static PinBoard Reload(PinBoard board, SizingMode mode)
    {
        PinBoard loaded = PinSerializer.FromLines(PinSerializer.ToLines(board)).Board;
        loaded.Capacity = 4;
        loaded.Mode = mode;
        return loaded;
    }

    [Theory]
    [InlineData(SizingMode.Fixed)]
    [InlineData(SizingMode.Auto)]
    public void Missing_pin_survives_save_load_and_returns_in_place(SizingMode mode)
    {
        var board = new PinBoard(4) { Mode = mode };
        board.Toggle(K("A"), "A", "mod.a");
        board.Toggle(K("Gone"), "Gone", "mod.gone");
        board.Toggle(K("C"), "C", "mod.c");

        PinBoard loaded = Reload(Reload(board, mode), mode);
        var removed = new Resolver(K("Gone"));

        Assert.Equal(1, loaded.CountUnresolved(removed));
        Assert.Equal(3, loaded.Entries.Count(e => e != null));
        IReadOnlyList<PinSlot> slots = loaded.VisibleSlots(removed);
        if (mode == SizingMode.Fixed)
        {
            Assert.Equal(PinSlotState.Unresolved, slots[1].State);
            Assert.Equal("mod.gone", slots[1].Entry!.PackageId);
        }
        else
        {
            Assert.Equal(new[] { 0, 2 }, slots.Select(s => s.Index));
        }

        IReadOnlyList<PinSlot> back = loaded.VisibleSlots(new Resolver());
        Assert.Equal(new[] { 0, 1, 2 }, back.Where(s => s.State == PinSlotState.Resolved).Select(s => s.Index));
        Assert.Equal("build:thing/Gone", back.First(s => s.Index == 1).Entry!.Key.Value);
    }

    [Fact]
    public void Forget_removes_only_the_missing_pin()
    {
        var board = new PinBoard(4);
        board.Toggle(K("A"));
        board.Toggle(K("Gone"));
        Assert.Equal(1, board.ForgetUnresolved(new Resolver(K("Gone"))));
        Assert.Single(board.Entries, e => e != null);
    }
}
