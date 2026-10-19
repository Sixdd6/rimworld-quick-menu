using System;
using System.Linq;
using QuickMenu.Core.Pins;
using Xunit;

namespace QuickMenu.Core.Tests.Pins;

public class PinKeyRoundTripTests
{
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

    [Fact]
    public void TryRoundTrip_returns_the_parsed_key_equal_to_the_original()
    {
        PinKey key = PinKey.Order("A.B", 2);
        Assert.True(PinKeyRoundTrip.TryRoundTrip(key, out PinKey? parsed));
        Assert.NotNull(parsed);
        Assert.Equal(key, parsed);
        Assert.NotSame(key, parsed);
    }

    [Fact]
    public void TryRoundTrip_handles_every_kind_and_escapes()
    {
        PinKey build = PinKey.Build(PinKeyKind.BuildThing, "W,all");
        foreach (PinKey key in new[] { build, PinKey.Dropdown("G~1"), PinKey.ModDropdown("T", "C#", new[] { build }) })
        {
            Assert.True(PinKeyRoundTrip.TryRoundTrip(key, out PinKey? parsed), key.Value);
            Assert.Equal(key, parsed);
        }
    }

    [Fact]
    public void TryRoundTrip_rejects_null() =>
        Assert.Throws<ArgumentNullException>(() => PinKeyRoundTrip.TryRoundTrip(null!, out _));
}
