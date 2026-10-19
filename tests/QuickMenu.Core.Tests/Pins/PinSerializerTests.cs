using System.Collections.Generic;
using System.Linq;
using QuickMenu.Core.Pins;
using Xunit;

namespace QuickMenu.Core.Tests.Pins;

public class PinSerializerTests
{
    private static PinKey Thing(string n) => PinKey.Build(PinKeyKind.BuildThing, n);

    [Fact]
    public void RoundTrip_PreservesSlotKeyPackageIdAndLabel()
    {
        var board = new PinBoard(8);
        board.SetSlot(0, new PinEntry(Thing("Wall"), "Wall", "ludeon.rimworld"));
        board.SetSlot(3, new PinEntry(PinKey.Order("A.B"), "Zone", null));

        PinLoadResult r = PinSerializer.FromLines(PinSerializer.ToLines(board));

        Assert.Equal(0, r.SkippedCount);
        Assert.Equal(4, r.Board.Entries.Count);
        Assert.Equal(board.Entries[0], r.Board.Entries[0]);
        Assert.Equal(board.Entries[3], r.Board.Entries[3]);
        Assert.Null(r.Board.Entries[1]);
    }

    [Fact]
    public void RoundTrip_EscapesPipeAndBackslashInLabelPackageAndKey()
    {
        var board = new PinBoard(4);
        var entry = new PinEntry(Thing("We|ird\\Name"), "a|b\\c|", "p|id\\");
        board.SetSlot(1, entry);

        List<string> lines = PinSerializer.ToLines(board);
        PinLoadResult r = PinSerializer.FromLines(lines);

        Assert.Single(lines);
        Assert.Equal(entry, r.Board.Entries[1]);
        Assert.Equal(0, r.SkippedCount);
    }

    [Fact]
    public void RoundTrip_EmptyLabelAndNullPackage()
    {
        var board = new PinBoard(2);
        board.SetSlot(0, new PinEntry(Thing("X")));
        PinLoadResult r = PinSerializer.FromLines(PinSerializer.ToLines(board));
        Assert.Equal(new PinEntry(Thing("X")), r.Board.Entries[0]);
        Assert.Null(r.Board.Entries[0]!.PackageId);
    }

    [Fact]
    public void RoundTrip_ModDropdownKey()
    {
        PinKey key = PinKey.ModDropdown("M.T", "Cat", new[] { Thing("A"), Thing("B") });
        var board = new PinBoard(2);
        board.SetSlot(0, new PinEntry(key, "L"));
        PinLoadResult r = PinSerializer.FromLines(PinSerializer.ToLines(board));
        Assert.Equal(key, r.Board.Entries[0]!.Key);
    }

    [Fact]
    public void UnknownKeyPrefix_IsKeptVerbatimAndWrittenBackUnchanged()
    {
        const string unknown = "2|future:thing/X|some.mod|Label";
        PinLoadResult r = PinSerializer.FromLines(new[] { "0|build:thing/Wall||W", unknown });

        Assert.Equal(0, r.SkippedCount);
        List<string> back = PinSerializer.ToLines(r.Board);
        Assert.Equal(new[] { "0|build:thing/Wall||W", unknown }, back);
    }

    [Theory]
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData("0|build:thing/X|p")]
    [InlineData("0|build:thing/X|p|l|extra")]
    [InlineData("x|build:thing/X||l")]
    [InlineData("-1|build:thing/X||l")]
    [InlineData("01|build:thing/X||l")]
    [InlineData("0||p|l")]
    [InlineData("0|Build:thing/X||l")]
    [InlineData("0|build:thing/X||bad^x")]
    [InlineData("0|build:thing/X||trail^")]
    public void MalformedLine_IsSkippedAndCounted_OthersLoad(string bad)
    {
        PinLoadResult r = PinSerializer.FromLines(new[] { "1|build:thing/Good||G", bad });

        Assert.Equal(1, r.SkippedCount);
        Assert.Equal(new SkippedPinLine(1, PinLineIssue.Malformed), r.Skipped[0]);
        Assert.Equal("G", r.Board.Entries[1]!.Label);
    }

    [Fact]
    public void CorruptFile_MixOfGarbageNullAndUnknownLines_LoadsValidOnesAndKeepsUnknownVerbatim()
    {
        const string unknown = "3|future:thing/X|some.mod|Label";
        PinLoadResult r = PinSerializer.FromLines(new string?[] { "\u0000\u0001", null, "0|build:thing/Wall||W", "not a pin", unknown, "" });

        Assert.Equal(4, r.SkippedCount);
        Assert.Equal(new[] { "0|build:thing/Wall||W", unknown }, PinSerializer.ToLines(r.Board));
    }

    [Fact]
    public void NullLineAndNullList_DoNotThrow()
    {
        Assert.Equal(1, PinSerializer.FromLines(new string?[] { null }).SkippedCount);
        Assert.Equal(0, PinSerializer.FromLines(null).SkippedCount);
    }

    [Fact]
    public void LineLongerThan4KB_IsSkipped_ExactLimitLoads()
    {
        string head = "0|build:thing/X||";
        string atLimit = head + new string('a', 4096 - head.Length);
        string over = head + new string('a', 4097 - head.Length);

        PinLoadResult ok = PinSerializer.FromLines(new[] { atLimit });
        Assert.Equal(0, ok.SkippedCount);

        PinLoadResult r = PinSerializer.FromLines(new[] { over, "1|build:thing/Good||G" });
        Assert.Equal(new SkippedPinLine(0, PinLineIssue.TooLong), Assert.Single(r.Skipped));
        Assert.Equal("G", r.Board.Entries[1]!.Label);
    }

    private static string ModDrop(int children)
    {
        var kids = Enumerable.Range(0, children).Select(i => "dropdown:" + i.ToString("x3", System.Globalization.CultureInfo.InvariantCulture));
        return "0|moddrop:M@C~" + string.Join(",", kids).Replace("^", "^^").Replace("|", "^|") + "||L";
    }

    [Fact]
    public void ModDropdownWithMoreThan256Children_IsSkipped_256Loads()
    {
        // Short child keys keep 257 children under the 4 KB line limit, so the count check is what rejects it.
        Assert.Equal(0, PinSerializer.FromLines(new[] { ModDrop(256) }).SkippedCount);

        PinLoadResult r = PinSerializer.FromLines(new[] { ModDrop(257), "1|build:thing/Good||G" });
        Assert.Equal(PinLineIssue.TooManyChildren, Assert.Single(r.Skipped).Issue);
        Assert.NotNull(r.Board.Entries[1]);
    }

    [Fact]
    public void DuplicateSlotOrHugeSlot_IsSkipped()
    {
        PinLoadResult r = PinSerializer.FromLines(new[]
        {
            "0|build:thing/A||a", "0|build:thing/B||b", "4096|build:thing/C||c", "999999999|build:thing/D||d",
        });

        Assert.Equal(3, r.SkippedCount);
        Assert.All(r.Skipped, s => Assert.Equal(PinLineIssue.BadSlot, s.Issue));
        Assert.Equal("a", r.Board.Entries[0]!.Label);
        Assert.Single(r.Board.Entries);
    }

    [Fact]
    public void ToLines_NullBoard_Throws() =>
        Assert.Throws<System.ArgumentNullException>(() => PinSerializer.ToLines(null!));

    [Fact]
    public void ToLines_DropsCharactersXmlCannotStore()
    {
        var board = new PinBoard(4);
        board.SetSlot(0, new PinEntry(Thing("Wall"), "a\u0001b\uD800c\uDC00d\uFFFEe\uD83D\uDE00", "p\u0000q"));

        PinLoadResult r = PinSerializer.FromLines(PinSerializer.ToLines(board));

        PinEntry e = r.Board.Entries[0]!;
        Assert.Equal("abcde\uD83D\uDE00", e.Label);
        Assert.Equal("pq", e.PackageId);
    }

    [Fact]
    public void Toggle_SanitizesLabelAndPackage()
    {
        var board = new PinBoard(4);
        Assert.Equal(PinResult.Pinned, board.Toggle(Thing("Wall"), "l\u0003", "x\u0002"));
        PinEntry e = board.Entries[0]!;
        Assert.Equal("l", e.Label);
        Assert.Equal("x", e.PackageId);
    }
}
