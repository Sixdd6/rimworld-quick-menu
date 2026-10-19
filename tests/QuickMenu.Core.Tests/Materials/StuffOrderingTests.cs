using QuickMenu.Core.Materials;
using Xunit;

namespace QuickMenu.Core.Tests.Materials;

public class StuffOrderingTests
{
    [Fact]
    public void LastUsed_goes_first_and_rest_keep_order()
    {
        StuffOrder r = StuffOrdering.Order(new[] { "Wood", "Steel", "Granite", "Gold" }, "Granite");
        Assert.Equal(StuffRingOutcome.Ring, r.Outcome);
        Assert.Equal(new[] { "Granite", "Wood", "Steel", "Gold" }, r.Ids);
    }

    [Fact]
    public void LastUsed_already_first_is_unchanged()
    {
        StuffOrder r = StuffOrdering.Order(new[] { "A", "B" }, "A");
        Assert.Equal(new[] { "A", "B" }, r.Ids);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Missing")]
    public void Unknown_or_null_last_used_keeps_input_order(string? last)
    {
        StuffOrder r = StuffOrdering.Order(new[] { "A", "B", "C" }, last);
        Assert.Equal(StuffRingOutcome.Ring, r.Outcome);
        Assert.Equal(new[] { "A", "B", "C" }, r.Ids);
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
