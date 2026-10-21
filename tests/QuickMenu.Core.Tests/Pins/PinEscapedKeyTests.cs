using System.Collections.Generic;
using QuickMenu.Core.Pins;
using Xunit;

namespace QuickMenu.Core.Tests.Pins;

/// <summary>M2 review: mod dropdown keys whose type or category names hold the key's own separators.</summary>
public class PinEscapedKeyTests
{
    private static PinKey Drop(string type, string cat, int children)
    {
        var list = new List<PinKey>();
        for (int i = 0; i < children; i++)
        {
            list.Add(PinKey.Build(PinKeyKind.BuildThing, "Thing" + i));
        }

        return PinKey.ModDropdown(type, cat, list);
    }

    [Theory]
    [InlineData("Ns.A~B", "Cat,1")]
    [InlineData("Ns.A,B", "Cat~1")]
    [InlineData("Ns.A|B", "C\\at")]
    public void Separators_inside_names_round_trip_through_key_and_board(string type, string cat)
    {
        PinKey key = Drop(type, cat, 3);

        Assert.True(PinKey.TryParse(key.Value, out PinKey? parsed));
        Assert.Equal(key, parsed);
        Assert.True(PinKeyRoundTrip.TryRoundTrip(key, out PinKey? again));
        Assert.Equal(key, again);

        var board = new PinBoard(4);
        board.SetSlot(1, new PinEntry(key, "label", "pkg"));
        PinLoadResult r = PinSerializer.FromLines(PinSerializer.ToLines(board));
        Assert.Equal(0, r.SkippedCount);
        Assert.Equal(key, r.Board.Entries[1]!.Key);
    }

    [Fact]
    public void Escaped_separators_in_the_type_name_still_round_trip_within_the_line_limit()
    {
        // 120 children plus a type name full of escaped '~' and ',' stays under the line-length limit.
        PinKey key = Drop("A~~,,B", "C", 120);
        var board = new PinBoard(2);
        board.SetSlot(0, new PinEntry(key, "l", ""));

        PinLoadResult r = PinSerializer.FromLines(PinSerializer.ToLines(board));

        Assert.Equal(0, r.SkippedCount);
        Assert.Equal(key, r.Board.Entries[0]!.Key);
    }
}
