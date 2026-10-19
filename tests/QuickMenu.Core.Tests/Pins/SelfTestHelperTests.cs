using System;
using QuickMenu.Core.Diagnostics;
using QuickMenu.Core.Pins;
using Xunit;

namespace QuickMenu.Core.Tests.Pins;

public class SelfTestHelperTests
{
    [Fact]
    public void BoundedLines_keeps_first_n_and_counts_the_rest()
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
    public void BoundedLines_zero_limit_keeps_nothing()
    {
        var lines = new BoundedLines(0);
        lines.Add("a");
        Assert.Empty(lines.Kept);
        Assert.Equal(1, lines.Omitted);
    }

    [Fact]
    public void BoundedLines_rejects_negative_limit() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new BoundedLines(-1));

    [Fact]
    public void Parses_is_true_for_every_kind()
    {
        PinKey build = PinKey.Build(PinKeyKind.BuildThing, "Wall");
        PinKey mod = PinKey.ModDropdown("My.Type,x", "Cat~", new[] { build, PinKey.Order("A.B", 2) });
        foreach (PinKey key in new[] { build, PinKey.Build(PinKeyKind.BuildTerrain, "T@#"), PinKey.Dropdown("G|1"), PinKey.Order("A.B"), PinKey.Order("A.B", 0), mod })
        {
            Assert.True(PinKeyRoundTrip.Parses(key), key.Value);
        }
    }

    [Fact]
    public void Parses_rejects_null() =>
        Assert.Throws<ArgumentNullException>(() => PinKeyRoundTrip.Parses(null!));
}
