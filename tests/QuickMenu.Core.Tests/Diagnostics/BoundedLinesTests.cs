using System;
using System.Linq;
using QuickMenu.Core.Diagnostics;
using Xunit;

namespace QuickMenu.Core.Tests.Diagnostics;

public class BoundedLinesTests
{
    [Fact]
    public void keeps_first_n_and_counts_the_rest()
    {
        var lines = new BoundedLines(2);
        lines.Add("a");
        lines.Add("b");
        lines.Add("c");
        Assert.Equal(new[] { "a", "b" }, lines.Kept);
        Assert.Equal(3, lines.Total);
        Assert.Equal(1, lines.Omitted);
    }

    [Fact]
    public void zero_limit_keeps_nothing()
    {
        var lines = new BoundedLines(0);
        lines.Add("a");
        Assert.Empty(lines.Kept);
        Assert.Equal(1, lines.Omitted);
    }

    [Fact]
    public void rejects_negative_limit() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new BoundedLines(-1));

    [Fact]
    public void empty_input_has_nothing_kept_or_omitted()
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
    public void caps_at_the_limit_and_counts_the_rest(int limit, int offered, int kept, int omitted)
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
    public void keeps_the_first_lines_not_the_last_and_preserves_order()
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
    public void keeps_empty_and_duplicate_lines_as_given()
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
    public void limit_int_max_does_not_preallocate_or_overflow()
    {
        var lines = new BoundedLines(int.MaxValue);
        lines.Add("a");
        Assert.Single(lines.Kept);
        Assert.Equal(0, lines.Omitted);
    }
}
