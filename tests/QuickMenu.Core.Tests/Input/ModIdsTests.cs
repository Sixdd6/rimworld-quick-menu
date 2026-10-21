using QuickMenu.Core.Input;
using Xunit;

namespace QuickMenu.Core.Tests.Input;

public class ModIdsTests
{
    [Theory]
    [InlineData("Dubwise.DubsMintMenus", true)]
    [InlineData("dubwise.dubsmintmenus", true)]
    [InlineData("dubwise.dubsmintmenus_steam", true)]
    [InlineData("dubwise.dubsmintmenus_copy", true)]
    [InlineData("dubwise.dubsmintmenus_other", false)]
    [InlineData("dubwise.dubsmintmenus2", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Matches_mint(string? actual, bool expected)
    {
        Assert.Equal(expected, ModIds.Matches(actual, ModIds.Mint));
    }
}
