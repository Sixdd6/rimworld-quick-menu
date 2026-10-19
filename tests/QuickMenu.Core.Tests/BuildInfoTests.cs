using QuickMenu.Core;
using Xunit;

namespace QuickMenu.Tests;

public class BuildInfoTests
{
    [Fact]
    public void ModName_IsQuickMenu()
    {
        Assert.Equal("Quick Menu", BuildInfo.ModName);
    }

    [Fact]
    [Trait("Category", "Perf")]
    public void PerfTraitPipeline_RunsSeparately()
    {
        Assert.Equal(20261019, QuickMenu.TestData.SyntheticCatalog.DefaultSeed);
    }
}
