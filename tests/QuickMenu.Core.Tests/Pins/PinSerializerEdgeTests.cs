using System.Collections.Generic;
using QuickMenu.Core.Pins;
using Xunit;

namespace QuickMenu.Core.Tests.Pins;

public class PinSerializerEdgeTests
{
    private static PinKey Thing(string n) => PinKey.Build(PinKeyKind.BuildThing, n);

    [Theory]
    [InlineData("a|b")]
    [InlineData("a\\b")]
    [InlineData("\\")]
    [InlineData("\\\\")]
    [InlineData("|")]
    [InlineData("||")]
    [InlineData("\\|\\")]
    [InlineData("trailing\\")]
    [InlineData("é中文 \U0001F600")]
    public void Label_and_package_round_trip_with_special_characters(string text)
    {
        var board = new PinBoard(4);
        board.SetSlot(1, new PinEntry(Thing("Wall"), text, text));

        PinLoadResult r = PinSerializer.FromLines(PinSerializer.ToLines(board));

        Assert.Equal(0, r.SkippedCount);
        Assert.Equal(text, r.Board.Entries[1]!.Label);
        Assert.Equal(text, r.Board.Entries[1]!.PackageId);
    }

    [Theory]
    [InlineData("A|B")]
    [InlineData("A\\B")]
    [InlineData("A.B|C\\")]
    public void Order_key_with_pipe_and_backslash_round_trips(string typeName)
    {
        var key = PinKey.Order(typeName);
        var board = new PinBoard(2);
        board.SetSlot(0, new PinEntry(key, "l|\\", null));

        PinLoadResult r = PinSerializer.FromLines(PinSerializer.ToLines(board));

        Assert.Equal(0, r.SkippedCount);
        Assert.Equal(key, r.Board.Entries[0]!.Key);
    }

    [Fact]
    public void Preserved_lines_keep_escapes_and_follow_the_board_lines_unchanged()
    {
        string[] lines = { "0|future:a\\|b|pk|la\\\\bel", "1|build:thing/Wall||W" };
        PinLoadResult r = PinSerializer.FromLines(lines);

        Assert.Equal(0, r.SkippedCount);
        Assert.Equal(new[] { lines[0] }, r.Preserved);

        List<string> back = PinSerializer.ToLines(r.Board, r.Preserved);
        Assert.Equal(new[] { lines[1], lines[0] }, back);

        // Second generation is stable.
        PinLoadResult r2 = PinSerializer.FromLines(back);
        Assert.Equal(back, PinSerializer.ToLines(r2.Board, r2.Preserved));
    }

    [Fact]
    public void Preserved_slot_is_reserved_against_later_duplicates()
    {
        PinLoadResult r = PinSerializer.FromLines(new[] { "2|future:x||", "2|build:thing/Wall||W" });
        Assert.Single(r.Preserved);
        Assert.Equal(PinLineIssue.BadSlot, Assert.Single(r.Skipped).Issue);
    }

    [Theory]
    [InlineData("0|Future:x||")]
    [InlineData("0|1x:x||")]
    [InlineData("0|:x||")]
    [InlineData("0|nocolon||")]
    [InlineData("0|build:thing/||")]
    public void Not_a_valid_future_prefix_and_not_a_known_key_is_skipped(string line)
    {
        PinLoadResult r = PinSerializer.FromLines(new[] { line });
        Assert.Equal(1, r.SkippedCount);
        Assert.Empty(r.Preserved);
    }

    [Theory]
    [InlineData("0|build:thing/Wall||x\\")]
    [InlineData("0|build:thing/Wall||x\\n")]
    [InlineData("0|build:thing/Wall|||extra")]
    [InlineData("-1|build:thing/Wall||x")]
    [InlineData("01|build:thing/Wall||x")]
    [InlineData("99999999999|build:thing/Wall||x")]
    [InlineData("4096|build:thing/Wall||x")]
    [InlineData("")]
    public void Bad_lines_are_skipped_without_throwing(string line)
    {
        PinLoadResult r = PinSerializer.FromLines(new[] { line });
        Assert.Equal(1, r.SkippedCount);
        Assert.Empty(r.Board.Entries);
    }

    [Fact]
    public void Max_slot_loads_and_round_trips()
    {
        PinLoadResult r = PinSerializer.FromLines(new[] { "4095|build:thing/Wall||x" });
        Assert.Equal(0, r.SkippedCount);
        Assert.Equal(4096, r.Board.Entries.Count);
        Assert.Equal(new[] { "4095|build:thing/Wall||x" }, PinSerializer.ToLines(r.Board));
    }

    [Fact]
    public void Skipped_report_carries_index_and_no_content()
    {
        PinLoadResult r = PinSerializer.FromLines(new string?[] { "0|build:thing/Wall||x", "junk", null });
        Assert.Equal(2, r.SkippedCount);
        Assert.Equal(1, r.Skipped[0].LineIndex);
        Assert.Equal(2, r.Skipped[1].LineIndex);
    }

    [Fact]
    public void Long_future_line_is_dropped_not_preserved_and_does_not_block_others()
    {
        string huge = "0|future:x||" + new string('a', 5000);
        PinLoadResult r = PinSerializer.FromLines(new[] { huge, "1|build:thing/Wall||x" });
        Assert.Equal(PinLineIssue.TooLong, Assert.Single(r.Skipped).Issue);
        Assert.Empty(r.Preserved);
        Assert.NotNull(r.Board.Entries[1]);
    }

    [Fact]
    public void Hidden_slots_beyond_capacity_are_written()
    {
        var b = new PinBoard(5);
        b.SetSlot(4, new PinEntry(Thing("Wall")));
        b.Capacity = 2;
        Assert.Single(PinSerializer.ToLines(b));
    }
}
