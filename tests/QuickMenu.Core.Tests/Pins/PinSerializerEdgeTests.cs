using System.Collections.Generic;
using QuickMenu.Core.Pins;
using Xunit;

namespace QuickMenu.Core.Tests.Pins;

public class PinSerializerEdgeTests
{
    private static PinKey Thing(string n) => PinKey.Build(PinKeyKind.BuildThing, n);

    [Theory]
    [InlineData("a\\nb")]
    [InlineData("\\n")]
    [InlineData("\\\\n")]
    [InlineData("^\\n^")]
    [InlineData("a\nb")]
    public void Written_lines_hold_no_backslash_so_the_game_string_loader_cannot_rewrite_them(string text)
    {
        var board = new PinBoard(4);
        board.SetSlot(2, new PinEntry(Thing("We\\nird"), text, text));

        List<string> lines = PinSerializer.ToLines(board);
        // The game's ParseHelper.ParseString does Replace("\\n", "\n") on every loaded string.
        List<string> loaded = lines.ConvertAll(l => l.Replace("\\n", "\n"));
        PinLoadResult r = PinSerializer.FromLines(loaded);

        Assert.DoesNotContain(lines, l => l.Contains('\\'));
        Assert.Equal(lines, loaded);
        Assert.Equal(0, r.SkippedCount);
        Assert.Equal(text, r.Board.Entries[2]!.Label);
        Assert.Equal(text, r.Board.Entries[2]!.PackageId);
    }

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
    public void Opaque_lines_keep_escapes_and_their_slot_unchanged()
    {
        string[] lines = { "0|future:a^|b|pk|la^^bel", "1|build:thing/Wall||W" };
        PinLoadResult r = PinSerializer.FromLines(lines);

        Assert.Equal(0, r.SkippedCount);
        Assert.Equal(PinKeyKind.Opaque, r.Board.Entries[0]!.Key.Kind);

        List<string> back = PinSerializer.ToLines(r.Board);
        Assert.Equal(lines, back);

        // Second generation is stable.
        Assert.Equal(back, PinSerializer.ToLines(PinSerializer.FromLines(back).Board));
    }

    [Fact]
    public void Opaque_slot_is_reserved_against_later_duplicates()
    {
        PinLoadResult r = PinSerializer.FromLines(new[] { "2|future:x||", "2|build:thing/Wall||W" });
        Assert.Equal(PinKeyKind.Opaque, r.Board.Entries[2]!.Key.Kind);
        Assert.Equal(PinLineIssue.BadSlot, Assert.Single(r.Skipped).Issue);
    }

    private sealed class AllResolved : IPinResolver
    {
        public bool IsResolved(PinKey key) => true;
    }

    [Fact]
    public void Opaque_slots_are_occupied_so_new_pins_skip_them_and_the_line_survives_save_and_reload()
    {
        PinLoadResult r = PinSerializer.FromLines(new[] { "0|build:thing/Wall||W", "2|future:x/Y||L" });
        PinBoard board = r.Board;
        board.Capacity = 4;

        Assert.Equal(PinResult.Pinned, board.Toggle(PinKey.Build(PinKeyKind.BuildThing, "A")));
        Assert.Equal(PinResult.Pinned, board.Toggle(PinKey.Build(PinKeyKind.BuildThing, "B")));
        Assert.Equal(PinResult.Full, board.Toggle(PinKey.Build(PinKeyKind.BuildThing, "C")));

        Assert.Equal("build:thing/A", board.Entries[1]!.Key.Value);
        Assert.Equal("build:thing/B", board.Entries[3]!.Key.Value);
        Assert.Equal(PinSlotState.Unresolved, board.VisibleSlots(new AllResolved())[2].State);

        List<string> lines = PinSerializer.ToLines(board);
        Assert.Equal("2|future:x/Y||L", lines[2]);
        Assert.Equal(lines, PinSerializer.ToLines(PinSerializer.FromLines(lines).Board));
    }

    [Fact]
    public void Opaque_entry_is_not_toggled_off_by_a_real_key_and_moves_with_its_slot_number_on_swap()
    {
        PinBoard board = PinSerializer.FromLines(new[] { "0|future:x||L", "1|build:thing/A||" }).Board;
        board.Capacity = 3;

        Assert.True(board.Swap(0, 2));
        Assert.Equal(new[] { "1|build:thing/A||", "2|future:x||L" }, PinSerializer.ToLines(board));
        Assert.Equal(1, board.CountUnresolved(new AllResolved()));
        Assert.Equal(1, board.ForgetUnresolved(new AllResolved()));
    }

    [Theory]
    [InlineData("0|order:X#1#2||L")]
    [InlineData("0|build:thing/||L")]
    [InlineData("0|moddrop:bad|p|l")]
    public void Known_prefix_that_fails_to_parse_is_kept_as_opaque_not_dropped(string line)
    {
        PinLoadResult r = PinSerializer.FromLines(new[] { line });
        Assert.Equal(0, r.SkippedCount);
        Assert.Equal(new[] { line }, PinSerializer.ToLines(r.Board));
    }

    [Theory]
    [InlineData("0|Future:x||")]
    [InlineData("0|1x:x||")]
    [InlineData("0|:x||")]
    [InlineData("0|nocolon||")]
    public void Not_a_valid_future_prefix_and_not_a_known_key_is_skipped(string line)
    {
        PinLoadResult r = PinSerializer.FromLines(new[] { line });
        Assert.Equal(1, r.SkippedCount);
    }

    [Theory]
    [InlineData("0|build:thing/Wall||x^")]
    [InlineData("0|build:thing/Wall||x^n")]
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
