using QuickMenu.Core.Pins;
using Xunit;

namespace QuickMenu.Tests.Pins;

public class PinExclusionsTests
{
    [Fact]
    public void MorePlanning_colour_squares_are_not_pinnable() =>
        Assert.True(PinExclusions.IsExcluded("MorePlanning.Designators.SelectColorDesignator"));

    [Theory]
    [InlineData("MorePlanning.Designators.AddDesignator")]
    [InlineData("RimWorld.Designator_Cancel")]
    [InlineData("morePlanning.designators.selectColorDesignator")]
    [InlineData("")]
    [InlineData(null)]
    public void Everything_else_is_pinnable(string? typeFullName) =>
        Assert.False(PinExclusions.IsExcluded(typeFullName));
}
