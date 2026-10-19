using QuickMenu.Core.Pins;
using Xunit;

namespace QuickMenu.Core.Tests.Pins;

public class UnavailableReasonTests
{
    [Fact]
    public void Designator_reason_wins_over_requirements()
    {
        Assert.Equal("No stuff.", UnavailableReason.Select(" No stuff. ", ["Requires research: X."], "fb"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_designator_reason_falls_through_to_requirements(string? own)
    {
        Assert.Equal("Requires research: X. Needs a Y.", UnavailableReason.Select(own, ["Requires research: X.", null, " ", "Needs a Y."], "fb"));
    }

    [Fact]
    public void Nothing_known_gives_the_fallback()
    {
        Assert.Equal("fb", UnavailableReason.Select(null, [], "fb"));
        Assert.Equal("fb", UnavailableReason.Select(null, null, "fb"));
        Assert.Equal("fb", UnavailableReason.Select("", ["", null], "fb"));
    }
}
