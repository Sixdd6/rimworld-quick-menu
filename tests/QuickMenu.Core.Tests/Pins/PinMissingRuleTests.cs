using QuickMenu.Core.Pins;
using Xunit;

namespace QuickMenu.Core.Tests.Pins;

public class PinMissingRuleTests
{
    private static PinKey Thing(string n) => PinKey.Build(PinKeyKind.BuildThing, n);

    [Fact]
    public void Build_pin_with_existing_def_is_not_missing_even_if_the_mod_is_inactive()
        => Assert.False(PinMissingRule.IsMissing(Thing("Wall"), "gone.mod", _ => true, _ => false));

    [Fact]
    public void Build_pin_with_no_def_is_missing_even_if_the_mod_is_active()
        => Assert.True(PinMissingRule.IsMissing(Thing("Wall"), "ludeon.rimworld", _ => false, _ => true));

    [Fact]
    public void Terrain_pin_is_decided_on_the_def()
        => Assert.True(PinMissingRule.IsMissing(PinKey.Build(PinKeyKind.BuildTerrain, "Soil"), null, _ => false, _ => true));

    [Fact]
    public void Opaque_pin_is_never_missing()
        => Assert.False(PinMissingRule.IsMissing(PinSerializer.FromLines(new[] { "0|future:x||" }).Board.Entries[0]!.Key, "gone.mod", _ => false, _ => false));

    [Fact]
    public void Non_def_kind_uses_the_package_id()
    {
        PinKey key = PinKey.Dropdown("Group");
        Assert.True(PinMissingRule.IsMissing(key, "gone.mod", _ => true, _ => false));
        Assert.False(PinMissingRule.IsMissing(key, "here.mod", _ => true, _ => true));
        Assert.False(PinMissingRule.IsMissing(key, null, _ => true, _ => false));
        Assert.False(PinMissingRule.IsMissing(key, "", _ => true, _ => false));
    }

    [Fact]
    public void Package_id_is_lower_cased_and_loses_a_trailing_steam_suffix()
    {
        string? seen = null;
        PinKey key = PinKey.Dropdown("Group");
        Assert.False(PinMissingRule.IsMissing(key, "Some.Mod_steam", _ => true, id => { seen = id; return true; }));
        Assert.Equal("some.mod", seen);
        Assert.Equal("a.b", PinMissingRule.NormalizePackageId("A.B"));
        Assert.Equal(string.Empty, PinMissingRule.NormalizePackageId(null));
    }
}

