using System;
using System.Collections.Generic;
using QuickMenu.Core.Pins;
using Xunit;

namespace QuickMenu.Tests.Pins;

/// <summary>Additional ADR 0004 edge cases: unicode, whitespace, escapes, adversarial input.</summary>
public class PinKeyEdgeTests
{
    public static TheoryData<string> OddNames => new()
    {
        "Ünï-名前-\U0001F600",
        " leading and trailing ",
        "with space",
        "tab\there",
        "line\nbreak",
        "colon:inside/slash",
        "\\",
        "\\\\",
        ",,,",
        "~@#|",
        "a\\,b",
        "moddrop:fake",
        "build:thing/nested",
        "x#1",
    };

    [Theory]
    [MemberData(nameof(OddNames))]
    public void EveryKind_RoundTripsOddNames(string name)
    {
        PinKey[] keys =
        {
            PinKey.Build(PinKeyKind.BuildThing, name),
            PinKey.Build(PinKeyKind.BuildTerrain, name),
            PinKey.Dropdown(name),
            PinKey.Order(name),
            PinKey.Order(name, 7),
            PinKey.ModDropdown(name, name, new[] { PinKey.Build(PinKeyKind.BuildThing, name), PinKey.Order(name, 2) }),
        };

        foreach (PinKey k in keys)
        {
            Assert.True(PinKey.TryParse(k.Value, out PinKey? parsed), k.Value);
            Assert.Equal(k.Value, parsed.Value);
            Assert.Equal(k.Kind, parsed.Kind);
            Assert.Equal(name, parsed.Name);
            Assert.Equal(k.Ordinal, parsed.Ordinal);
            Assert.Equal(k.Category, parsed.Category);
            Assert.Equal(k, parsed);
        }
    }

    [Fact]
    public void Order_OrdinalBoundaries_RoundTrip()
    {
        foreach (int n in new[] { 0, 1, 9, 10, 99, 999_999_999 })
        {
            PinKey k = PinKey.Order("T", n);
            Assert.True(PinKey.TryParse(k.Value, out PinKey? p), k.Value);
            Assert.Equal(n, p.Ordinal);
        }
    }

    [Fact]
    public void ModDropdown_ManyChildren_AreStableAndRoundTrip()
    {
        var children = new List<PinKey>();
        for (int i = 50; i >= 0; i--)
        {
            children.Add(PinKey.Build(PinKeyKind.BuildThing, "D" + i));
        }

        PinKey a = PinKey.ModDropdown("T", "C", children);
        children.Reverse();
        PinKey b = PinKey.ModDropdown("T", "C", children);
        Assert.Equal(a, b);
        Assert.True(PinKey.TryParse(a.Value, out PinKey? parsed));
        Assert.Equal(a.Value, parsed.Value);
        Assert.Equal(51, parsed.Children!.Count);
    }

    [Fact]
    public void ModDropdown_Children_AreASet_DuplicatesCollapse()
    {
        PinKey x = PinKey.Build(PinKeyKind.BuildThing, "X");
        PinKey y = PinKey.Build(PinKeyKind.BuildThing, "Y");
        PinKey dup = PinKey.ModDropdown("T", "C", new[] { y, x, y, PinKey.Build(PinKeyKind.BuildThing, "X") });
        PinKey once = PinKey.ModDropdown("T", "C", new[] { x, y });
        Assert.Equal(once, dup);
        Assert.Equal(2, dup.Children!.Count);
        Assert.True(PinKey.TryParse("moddrop:T@C~build:thing/Y,build:thing/X,build:thing/Y", out PinKey? parsed));
        Assert.Equal(once.Value, parsed.Value);
    }

    [Fact]
    public void Children_AreReadOnly()
    {
        PinKey key = PinKey.ModDropdown("T", "C", new[] { PinKey.Dropdown("A"), PinKey.Dropdown("B") });
        Assert.False(key.Children is PinKey[]);
        Assert.True(key.Children is System.Collections.ObjectModel.ReadOnlyCollection<PinKey>);
        Assert.Throws<NotSupportedException>(() => ((IList<PinKey>)key.Children!)[0] = PinKey.Dropdown("Z"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1_000_000_000)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void Order_OrdinalOutOfRange_Throws(int ordinal)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PinKey.Order("T", ordinal));
    }

    [Fact]
    public void TryParse_DeeplyNestedModDropdowns_ReturnsFalseQuickly()
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < 10_000; i++)
        {
            sb.Append("moddrop:T@C~");
        }

        sb.Append("build:thing/A");
        var watch = System.Diagnostics.Stopwatch.StartNew();
        Assert.False(PinKey.TryParse(sb.ToString(), out PinKey? key));
        Assert.Null(key);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(1), watch.Elapsed.ToString());
    }

    [Fact]
    public void ModDropdown_MixedKindChildren_SortByCanonicalString()
    {
        PinKey key = PinKey.ModDropdown("T", "C", new[]
        {
            PinKey.Order("Z"),
            PinKey.Dropdown("M"),
            PinKey.Build(PinKeyKind.BuildTerrain, "B"),
            PinKey.Build(PinKeyKind.BuildThing, "A"),
        });
        Assert.Equal("moddrop:T@C~build:terrain/B,build:thing/A,dropdown:M,order:Z", key.Value);
    }

    [Fact]
    public void Equality_NullHandling_AndHashContract()
    {
        PinKey a = PinKey.Dropdown("A");
        PinKey? nothing = null;
        Assert.False(a == nothing);
        Assert.True(a != nothing);
        Assert.True(nothing == null);
        Assert.False(a.Equals((PinKey?)null));
        Assert.Equal(PinKey.Dropdown("A").GetHashCode(), a.GetHashCode());
        var set = new HashSet<PinKey> { a, PinKey.Dropdown("A"), PinKey.Dropdown("B") };
        Assert.Equal(2, set.Count);
    }

    // Grammar pinned per ADR 0004: names are non-empty, specials \ , ~ @ # | must be escaped, ordinals are
    // canonical ASCII decimals, mod dropdown children are complete non-moddrop keys. Whitespace and lone
    // surrogates are ordinary name characters.
    [Theory]
    [InlineData("order:T\\#1")] // escaped hash: part of the name
    [InlineData("build:thing/\ud800")]
    [InlineData("build:thing/A build:thing/B")] // space is an ordinary name character
    [InlineData("moddrop:T@C~build:thing/A,build:thing/B")]
    public void TryParse_ExpectedValid_ParsesAndRoundTrips(string text)
    {
        Assert.True(PinKey.TryParse(text, out PinKey? key));
        Assert.Equal(text, key.Value);
        Assert.True(PinKey.TryParse(key.Value, out PinKey? again));
        Assert.Equal(key, again);
    }

    [Theory]
    [InlineData("build:thing/\\")]
    [InlineData("build:thing/\\\\\\")]
    [InlineData("dropdown:a\\")]
    [InlineData("order:T#1\\")]
    [InlineData("order:T#\\1")]
    [InlineData("order:T#1,")]
    [InlineData("order:T#\u0663")] // non-ASCII digit
    [InlineData("order:T# 1")]
    [InlineData("order:T#+1")]
    [InlineData("order:T#1000000000")]
    [InlineData("order:T#01")]
    [InlineData("moddrop:T@C~build:thing/A\\")]
    [InlineData("moddrop:T@C~build:thing/A,,build:thing/B")]
    [InlineData("moddrop:T@C~build:thing/A~build:thing/B")]
    [InlineData("moddrop:T@C@D~build:thing/A")]
    [InlineData("moddrop:T\\@C~build:thing/A")]
    [InlineData("moddrop:T@C~order:X#")]
    [InlineData("moddrop:T@C~ ")]
    [InlineData("moddrop:T@C~dropdown:")]
    [InlineData("moddrop:T@C~moddrop:U@D~build:thing/A")]
    [InlineData("\0")]
    public void TryParse_ExpectedInvalid_ReturnsFalseWithNullKey(string text)
    {
        Assert.False(PinKey.TryParse(text, out PinKey? key));
        Assert.Null(key);
    }

    [Fact]
    public void ValidEscapedHash_IsPartOfTheNameNotAnOrdinal()
    {
        Assert.True(PinKey.TryParse("order:T\\#1", out PinKey? key));
        Assert.Equal("T#1", key.Name);
        Assert.Null(key.Ordinal);
    }

    [Fact]
    public void TryParse_VeryLongInput_DoesNotThrow()
    {
        string longName = new string('x', 100_000);
        Assert.True(PinKey.TryParse("build:thing/" + longName, out PinKey? key));
        Assert.Equal(longName, key.Name);
        Assert.False(PinKey.TryParse("moddrop:T@C~" + string.Join(",", new string[2000]), out _));
    }

    [Fact]
    public void TryParse_OutIsNullOnEveryFailure()
    {
        Assert.False(PinKey.TryParse("moddrop:T@C~build:thing/A,junk", out PinKey? key));
        Assert.Null(key);
    }
}
