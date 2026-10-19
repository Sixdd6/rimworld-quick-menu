using System.Collections.Generic;
using System.Linq;
using QuickMenu.Core.Pins;
using Xunit;

namespace QuickMenu.Core.Tests.Pins;

public class PinWriteLimitTests
{
    private static PinKey K(string n) => PinKey.Build(PinKeyKind.BuildThing, n);

    [Fact]
    public void Overlong_label_is_truncated_on_write_so_the_line_reloads()
    {
        var b = new PinBoard(2);
        string label = new string('a', 5000) + "|\\";

        Assert.Equal(PinResult.Pinned, b.Toggle(K("X"), label, "mod.x"));
        string line = Assert.Single(PinSerializer.ToLines(b));
        Assert.True(line.Length <= PinSerializer.MaxLineLength);

        PinLoadResult r = PinSerializer.FromLines(new[] { line });
        Assert.Equal(0, r.SkippedCount);
        Assert.Equal(K("X"), r.Board.Entries[0]!.Key);
        Assert.Equal("mod.x", r.Board.Entries[0]!.PackageId);
        Assert.StartsWith("aaa", r.Board.Entries[0]!.Label);
    }

    [Fact]
    public void Truncation_does_not_split_a_surrogate_pair_or_an_escape()
    {
        var b = new PinBoard(1);
        string head = "0|build:thing/X||";
        string label = new string('a', PinSerializer.MaxLineLength - head.Length - 1) + "\U0001F600";
        b.Toggle(K("X"), label);

        string line = PinSerializer.ToLines(b)[0];
        Assert.Equal(PinSerializer.MaxLineLength - 1, line.Length);
        Assert.Equal(0, PinSerializer.FromLines(new[] { line }).SkippedCount);

        var c = new PinBoard(1);
        c.Toggle(K("X"), new string('a', PinSerializer.MaxLineLength - head.Length - 1) + "|");
        string line2 = PinSerializer.ToLines(c)[0];
        Assert.Equal(0, PinSerializer.FromLines(new[] { line2 }).SkippedCount);
    }

    [Theory]
    [InlineData('\\')]
    [InlineData('^')]
    public void Escaped_char_at_the_exact_budget_boundary_fits_and_one_more_is_cut_whole(char special)
    {
        string head = "0|build:thing/X||";
        int budget = PinSerializer.MaxLineLength - head.Length;

        // The special char costs 2 in the line, so budget - 2 filler chars plus it fill the line exactly.
        string exact = new string('a', budget - 2) + special;
        var b = new PinBoard(1);
        b.Toggle(K("X"), exact);
        string line = PinSerializer.ToLines(b)[0];
        Assert.Equal(PinSerializer.MaxLineLength, line.Length);
        Assert.Equal(exact, PinSerializer.FromLines(new[] { line }).Board.Entries[0]!.Label);

        // One filler less: the pair no longer fits, and is dropped whole rather than split.
        string over = new string('a', budget - 1) + special;
        var c = new PinBoard(1);
        c.Toggle(K("X"), over);
        string line2 = PinSerializer.ToLines(c)[0];
        Assert.Equal(PinSerializer.MaxLineLength - 1, line2.Length);
        PinLoadResult r = PinSerializer.FromLines(new[] { line2 });
        Assert.Equal(0, r.SkippedCount);
        Assert.Equal(new string('a', budget - 1), r.Board.Entries[0]!.Label);
    }

    [Fact]
    public void Short_label_is_stored_unchanged()
    {
        var b = new PinBoard(1);
        b.Toggle(K("X"), "Wall", "mod.x");
        Assert.Equal("Wall", b.Entries[0]!.Label);
    }

    [Fact]
    public void Mod_dropdown_with_more_than_256_children_is_rejected_and_nothing_changes()
    {
        IEnumerable<PinKey> kids = Enumerable.Range(0, 257).Select(i => PinKey.Dropdown("d" + i));
        PinKey big = PinKey.ModDropdown("T", "C", kids);
        var b = new PinBoard(2);

        Assert.Equal(PinResult.Rejected, b.Toggle(big, "L"));
        Assert.Empty(b.Entries);
    }

    [Fact]
    public void Key_that_alone_exceeds_the_line_limit_is_rejected()
    {
        var b = new PinBoard(2);
        Assert.Equal(PinResult.Rejected, b.Toggle(K(new string('n', 5000))));
        Assert.Empty(b.Entries);
    }

    [Fact]
    public void Overlong_package_id_is_dropped_but_the_pin_is_stored()
    {
        var b = new PinBoard(1);
        Assert.Equal(PinResult.Pinned, b.Toggle(K(new string('n', 4000)), "L", new string('p', 500)));
        PinEntry e = b.Entries[0]!;
        Assert.Null(e.PackageId);
        Assert.Equal("L", e.Label);
        Assert.Equal(0, PinSerializer.FromLines(PinSerializer.ToLines(b)).SkippedCount);
    }
}
