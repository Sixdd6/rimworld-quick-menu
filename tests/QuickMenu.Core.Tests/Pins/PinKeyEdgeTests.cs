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
    public void ModDropdown_DuplicateAndManyChildren_AreStableAndRoundTrip()
    {
        var children = new List<PinKey>();
        for (int i = 50; i >= 0; i--)
        {
            children.Add(PinKey.Build(PinKeyKind.BuildThing, "D" + i));
        }

        children.Add(children[0]);
        PinKey a = PinKey.ModDropdown("T", "C", children);
        children.Reverse();
        PinKey b = PinKey.ModDropdown("T", "C", children);
        Assert.Equal(a, b);
        Assert.True(PinKey.TryParse(a.Value, out PinKey? parsed));
        Assert.Equal(a.Value, parsed.Value);
        Assert.Equal(52, parsed.Children!.Count);
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

    [Theory]
    [InlineData("build:thing/A build:thing/B")]
    [InlineData("build:thing/\\")]
    [InlineData("build:thing/\\\\\\")]
    [InlineData("dropdown:a\\")]
    [InlineData("order:T\\#1")] // escaped hash is part of the name, so this is a valid key; see ValidEscapedHash
    [InlineData("order:T#1\\")]
    [InlineData("order:T#\\1")]
    [InlineData("order:T#1,")]
    [InlineData("order:T#٣")] // non-ASCII digit
    [InlineData("order:T# 1")]
    [InlineData("order:T#+1")]
    [InlineData("moddrop:T@C~build:thing/A\\")]
    [InlineData("moddrop:T@C~build:thing/A,,build:thing/B")]
    [InlineData("moddrop:T@C~build:thing/A~build:thing/B")]
    [InlineData("moddrop:T@C@D~build:thing/A")]
    [InlineData("moddrop:T\\@C~build:thing/A")]
    [InlineData("moddrop:T@C~order:X#")]
    [InlineData("moddrop:T@C~ ")]
    [InlineData("moddrop:T@C~dropdown:")]
    [InlineData("\0")]
    [InlineData("build:thing/\ud800")] // lone surrogate is a valid .NET char but must not crash
    public void TryParse_AdversarialInput_NeverThrows(string text)
    {
        bool ok = PinKey.TryParse(text, out PinKey? key);
        if (ok)
        {
            Assert.NotNull(key);
            Assert.True(PinKey.TryParse(key.Value, out PinKey? again));
            Assert.Equal(key, again);
        }
        else
        {
            Assert.Null(key);
        }
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
