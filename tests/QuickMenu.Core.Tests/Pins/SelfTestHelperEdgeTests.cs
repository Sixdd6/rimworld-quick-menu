using System;
using System.Linq;
using QuickMenu.Core.Diagnostics;
using QuickMenu.Core.Pins;
using Xunit;

namespace QuickMenu.Core.Tests.Pins;

/// <summary>Edge cases for the pure self-test helpers (T-9).</summary>
public class SelfTestHelperEdgeTests
{
    [Fact]
    public void BoundedLines_empty_input_has_nothing_kept_or_omitted()
    {
        var lines = new BoundedLines(20);
        Assert.Empty(lines.Kept);
        Assert.Equal(0, lines.Total);
        Assert.Equal(0, lines.Omitted);
        Assert.Equal(20, lines.Limit);
    }

    [Theory]
    [InlineData(5, 4, 4, 0)]
    [InlineData(5, 5, 5, 0)]
    [InlineData(5, 6, 5, 1)]
    [InlineData(1, 1000, 1, 999)]
    [InlineData(0, 0, 0, 0)]
    [InlineData(0, 7, 0, 7)]
    public void BoundedLines_caps_at_the_limit_and_counts_the_rest(int limit, int offered, int kept, int omitted)
    {
        var lines = new BoundedLines(limit);
        for (var i = 0; i < offered; i++)
        {
            lines.Add("line" + i);
        }

        Assert.Equal(kept, lines.Kept.Count);
        Assert.Equal(omitted, lines.Omitted);
        Assert.Equal(offered, lines.Total);
        Assert.Equal(Enumerable.Range(0, kept).Select(i => "line" + i), lines.Kept);
    }

    [Fact]
    public void BoundedLines_keeps_the_first_lines_not_the_last_and_preserves_order()
    {
        var lines = new BoundedLines(3);
        foreach (string s in new[] { "z", "a", "m", "b", "c" })
        {
            lines.Add(s);
        }

        Assert.Equal(new[] { "z", "a", "m" }, lines.Kept);
        Assert.Equal(2, lines.Omitted);
    }

    [Fact]
    public void BoundedLines_keeps_empty_and_duplicate_lines_as_given()
    {
        var lines = new BoundedLines(3);
        lines.Add(string.Empty);
        lines.Add("x");
        lines.Add("x");
        lines.Add("x");
        Assert.Equal(new[] { string.Empty, "x", "x" }, lines.Kept);
        Assert.Equal(1, lines.Omitted);
    }

    [Fact]
    public void BoundedLines_limit_int_max_does_not_preallocate_or_overflow()
    {
        var lines = new BoundedLines(int.MaxValue);
        lines.Add("a");
        Assert.Single(lines.Kept);
        Assert.Equal(0, lines.Omitted);
    }

    [Fact]
    public void Parses_handles_unicode_generic_nested_and_escaped_names()
    {
        PinKey[] keys =
        {
            PinKey.Build(PinKeyKind.BuildThing, "Mur_Épais_壁_\U0001F9F1"),
            PinKey.Build(PinKeyKind.BuildTerrain, "a b/c:d"),
            PinKey.Build(PinKeyKind.BuildThing, "\\"),
            PinKey.Build(PinKeyKind.BuildThing, ",~@#|"),
            PinKey.Dropdown("Grp,1~2"),
            PinKey.Order("My.Ns.Outer+Nested`1"),
            PinKey.Order("Ünï.Type`2[[System.Int32, System.Private.CoreLib]]", 999_999_999),
            PinKey.Order("A#1", 0),
        };
        foreach (PinKey key in keys)
        {
            Assert.True(PinKeyRoundTrip.Parses(key), key.Value);
        }
    }

    [Fact]
    public void Parses_handles_mod_dropdowns_with_unsorted_duplicate_and_escaped_children()
    {
        PinKey[] children =
        {
            PinKey.Order("Z.Last", 3),
            PinKey.Build(PinKeyKind.BuildThing, "W,all"),
            PinKey.Dropdown("G~1"),
            PinKey.Build(PinKeyKind.BuildThing, "W,all"),
            PinKey.Order("A.First"),
        };
        PinKey mod = PinKey.ModDropdown("Mod.Type@x", "Cat,~#", children);
        Assert.True(PinKeyRoundTrip.Parses(mod), mod.Value);

        PinKey many = PinKey.ModDropdown("T", "C", Enumerable.Range(0, 200).Select(i => PinKey.Build(PinKeyKind.BuildThing, "Def" + i)));
        Assert.True(PinKeyRoundTrip.Parses(many));
    }

    [Fact]
    public void Parses_is_deterministic_and_does_not_mutate_the_key()
    {
        PinKey key = PinKey.Order("A.B", 7);
        string before = key.Value;
        Assert.True(PinKeyRoundTrip.Parses(key));
        Assert.True(PinKeyRoundTrip.Parses(key));
        Assert.Equal(before, key.Value);
    }
}
