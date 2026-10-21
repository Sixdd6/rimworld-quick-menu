using QuickMenu.Core.Materials;
using Xunit;

namespace QuickMenu.Core.Tests.Materials;

public class StuffOrderingTests
{
    [Fact]
    public void Order_is_fixed_and_last_used_is_only_reported_by_index()
    {
        StuffOrder r = StuffOrdering.Order(new[] { "Wood", "Steel", "Granite", "Gold" }, "Granite");
        Assert.Equal(StuffRingOutcome.Ring, r.Outcome);
        Assert.Equal(new[] { "Wood", "Steel", "Granite", "Gold" }, r.Ids);
        Assert.Equal(2, r.LastUsedIndex);
    }

    [Fact]
    public void Same_input_gives_same_order_whatever_was_last_used()
    {
        string[] input = { "A", "B", "C" };
        Assert.Equal(StuffOrdering.Order(input, "A").Ids, StuffOrdering.Order(input, "C").Ids);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Missing")]
    public void Unknown_or_null_last_used_has_no_highlight(string? last)
    {
        StuffOrder r = StuffOrdering.Order(new[] { "A", "B", "C" }, last);
        Assert.Equal(StuffRingOutcome.Ring, r.Outcome);
        Assert.Equal(new[] { "A", "B", "C" }, r.Ids);
        Assert.Equal(-1, r.LastUsedIndex);
    }

    [Fact]
    public void One_option_skips_the_ring()
    {
        StuffOrder r = StuffOrdering.Order(new[] { "Wood" }, "Steel");
        Assert.Equal(StuffRingOutcome.Skip, r.Outcome);
        Assert.Equal(new[] { "Wood" }, r.Ids);
    }

    [Fact]
    public void No_options_is_none()
    {
        StuffOrder r = StuffOrdering.Order(System.Array.Empty<string>(), "Wood");
        Assert.Equal(StuffRingOutcome.None, r.Outcome);
        Assert.Empty(r.Ids);
    }

    [Fact]
    public void Input_is_not_mutated()
    {
        string[] input = { "A", "B", "C" };
        StuffOrdering.Order(input, "C");
        Assert.Equal(new[] { "A", "B", "C" }, input);
    }
}
