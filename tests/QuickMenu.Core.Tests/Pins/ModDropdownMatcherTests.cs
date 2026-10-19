using System.Linq;
using QuickMenu.Core.Pins;
using Xunit;

namespace QuickMenu.Core.Tests.Pins;

public class ModDropdownMatcherTests
{
    private static PinKey Drop(string type, string cat, params string[] defs) =>
        PinKey.ModDropdown(type, cat, defs.Select(d => PinKey.Build(PinKeyKind.BuildThing, d)));

    [Fact]
    public void Exact_match_is_found()
    {
        PinKey stored = Drop("T", "C", "a", "b");
        Assert.Same(stored, ModDropdownMatcher.Match(stored, new[] { Drop("T", "C", "z"), stored }));
    }

    [Fact]
    public void Half_overlap_matches_and_below_half_does_not()
    {
        PinKey stored = Drop("T", "C", "a", "b", "c");
        PinKey half = Drop("T", "C", "a", "b", "x"); // 2/4 = 0.5
        PinKey below = Drop("T", "C", "a", "x", "y"); // 1/5
        Assert.Same(half, ModDropdownMatcher.Match(stored, new[] { below, half }));
        Assert.Null(ModDropdownMatcher.Match(stored, new[] { below }));
    }

    [Fact]
    public void Different_type_never_matches()
    {
        PinKey stored = Drop("T1", "C", "a", "b");
        Assert.Null(ModDropdownMatcher.Match(stored, new[] { Drop("T2", "C", "a", "b") }));
    }

    [Fact]
    public void Non_dropdown_candidates_are_ignored()
    {
        PinKey stored = Drop("T", "C", "a");
        Assert.Null(ModDropdownMatcher.Match(stored, new[] { PinKey.Dropdown("T"), PinKey.Build(PinKeyKind.BuildThing, "a") }));
    }

    [Fact]
    public void Best_similarity_wins_over_order()
    {
        PinKey stored = Drop("T", "C", "a", "b", "c", "d");
        PinKey weak = Drop("T", "C", "a", "b", "x", "y"); // 2/6
        PinKey okay = Drop("T", "C", "a", "b", "c", "x"); // 3/5
        PinKey better = Drop("T", "C", "a", "b", "c", "d", "e"); // 4/5
        Assert.Same(better, ModDropdownMatcher.Match(stored, new[] { weak, okay, better }));
    }

    [Fact]
    public void Same_category_breaks_a_tie_then_first()
    {
        PinKey stored = Drop("T", "C", "a", "b");
        PinKey otherCat = Drop("T", "D", "a", "b");
        PinKey sameCat = Drop("T", "C", "a", "b");
        Assert.Same(sameCat, ModDropdownMatcher.Match(stored, new[] { otherCat, sameCat }));

        PinKey first = Drop("T", "D", "a", "b");
        PinKey second = Drop("T", "E", "a", "b");
        Assert.Same(first, ModDropdownMatcher.Match(stored, new[] { first, second }));
    }

    [Fact]
    public void Non_dropdown_stored_or_empty_candidates_give_none()
    {
        Assert.Null(ModDropdownMatcher.Match(PinKey.Dropdown("T"), new[] { Drop("T", "C", "a") }));
        Assert.Null(ModDropdownMatcher.Match(Drop("T", "C", "a"), System.Array.Empty<PinKey>()));
    }
}
