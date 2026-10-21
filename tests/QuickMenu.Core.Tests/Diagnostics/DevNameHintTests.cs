using QuickMenu.Core.Diagnostics;
using Xunit;

namespace QuickMenu.Core.Tests.Diagnostics;

public class DevNameHintTests
{
    [Theory]
    [InlineData("Designator_DevTool", true)]
    [InlineData("Designator_Dev", true)]
    [InlineData("DebugToolX", true)]
    [InlineData("GodModeThing", true)]
    [InlineData("Designator_Build", false)]
    [InlineData("MyDebug", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsDevTypeName_matches_prefixes_only(string? name, bool expected) =>
        Assert.Equal(expected, DevNameHint.IsDevTypeName(name));

    [Theory]
    [InlineData("Debug", true)]
    [InlineData("Dev", true)]
    [InlineData("DevTools", true)]
    [InlineData("Dev_Tools", true)]
    [InlineData("MyMod_Debug", true)]
    [InlineData("debug", true)]
    [InlineData("DEBUG", true)]
    [InlineData("Device", false)]
    [InlineData("Devilstrand", false)]
    [InlineData("Debugger", false)]
    [InlineData("Orders", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void HasDevToken_matches_whole_tokens(string? name, bool expected) =>
        Assert.Equal(expected, DevNameHint.HasDevToken(name));
}
