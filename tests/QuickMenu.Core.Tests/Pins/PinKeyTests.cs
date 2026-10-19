using System.Collections.Generic;
using QuickMenu.Core.Pins;
using Xunit;

namespace QuickMenu.Tests.Pins;

public class PinKeyTests
{
    public static TheoryData<string> CanonicalKeys => new()
    {
        "build:thing/Wall",
        "build:terrain/Concrete",
        "dropdown:Chairs",
        "order:RimWorld.Designator_Mine",
        "order:RimWorld.Designator_ZoneAddStockpile#2",
        "order:Ns.Outer+Nested`1",
        "moddrop:My.Mod.Dropdown@Furniture~build:thing/Armchair,build:thing/Bed",
        @"build:thing/we\|ird\,na\~me\@x\#y\\z",
        @"order:A\,B\#C#0",
        @"moddrop:T\@1@Cat\~2~build:thing/a\,b,order:X#3",
    };

    [Theory]
    [MemberData(nameof(CanonicalKeys))]
    public void RoundTrip_IsIdentical(string text)
    {
        Assert.True(PinKey.TryParse(text, out PinKey? key));
        Assert.Equal(text, key.ToString());
        Assert.Equal(text, key.Value);
        Assert.True(PinKey.TryParse(key.Value, out PinKey? again));
        Assert.Equal(key, again);
        Assert.True(key == again);
        Assert.Equal(key.GetHashCode(), again.GetHashCode());
    }

    [Fact]
    public void Factories_ProduceExpectedStrings()
    {
        Assert.Equal("build:thing/Wall", PinKey.Build(PinKeyKind.BuildThing, "Wall").Value);
        Assert.Equal("build:terrain/Soil", PinKey.Build(PinKeyKind.BuildTerrain, "Soil").Value);
        Assert.Equal("dropdown:G", PinKey.Dropdown("G").Value);
        Assert.Equal("order:T", PinKey.Order("T").Value);
        Assert.Equal("order:T#1", PinKey.Order("T", 1).Value);
    }

    [Fact]
    public void Properties_AreUnescaped()
    {
        PinKey build = PinKey.Build(PinKeyKind.BuildThing, "a|b,c~d@e#f\\g");
        Assert.Equal(PinKeyKind.BuildThing, build.Kind);
        Assert.Equal("a|b,c~d@e#f\\g", build.Name);
        Assert.True(PinKey.TryParse(build.Value, out PinKey? parsed));
        Assert.Equal("a|b,c~d@e#f\\g", parsed.Name);

        PinKey order = PinKey.Order("T#x", 4);
        Assert.True(PinKey.TryParse(order.Value, out PinKey? po));
        Assert.Equal("T#x", po.Name);
        Assert.Equal(4, po.Ordinal);
    }

    [Fact]
    public void SameNameDifferentKind_AreNotEqual()
    {
        Assert.NotEqual(PinKey.Build(PinKeyKind.BuildThing, "X"), PinKey.Build(PinKeyKind.BuildTerrain, "X"));
        Assert.NotEqual(PinKey.Order("T"), PinKey.Order("T", 1));
        Assert.False(PinKey.Dropdown("A").Equals((object)"dropdown:A"));
        Assert.False(PinKey.Dropdown("A").Equals(null));
        Assert.True(PinKey.Dropdown("A") != PinKey.Dropdown("B"));
    }

    [Fact]
    public void ModDropdown_ChildrenInAnyOrder_GiveSameKey()
    {
        PinKey a = PinKey.Build(PinKeyKind.BuildThing, "Bed");
        PinKey b = PinKey.Build(PinKeyKind.BuildThing, "Armchair");
        PinKey c = PinKey.Order("T", 2);
        PinKey k1 = PinKey.ModDropdown("My.Drop", "Furniture", new[] { a, b, c });
        PinKey k2 = PinKey.ModDropdown("My.Drop", "Furniture", new[] { c, b, a });
        Assert.Equal(k1, k2);
        Assert.Equal(k1.Value, k2.Value);
        Assert.Equal(
            "moddrop:My.Drop@Furniture~build:thing/Armchair,build:thing/Bed,order:T#2",
            k1.Value);
        Assert.Equal(3, k1.Children!.Count);
        Assert.Equal("Furniture", k1.Category);
    }

    [Fact]
    public void ModDropdown_ParseUnsortedChildren_NormalisesToSorted()
    {
        Assert.True(PinKey.TryParse("moddrop:T@C~build:thing/B,build:thing/A", out PinKey? key));
        Assert.Equal("moddrop:T@C~build:thing/A,build:thing/B", key.Value);
    }

    [Fact]
    public void ModDropdown_ChildrenWithSpecialCharacters_RoundTrip()
    {
        PinKey weird = PinKey.Build(PinKeyKind.BuildThing, "a,b~c@d");
        PinKey plain = PinKey.Build(PinKeyKind.BuildTerrain, "Plain");
        PinKey key = PinKey.ModDropdown("T,1", "C@2", new[] { weird, plain });
        Assert.True(PinKey.TryParse(key.Value, out PinKey? parsed));
        Assert.Equal(key, parsed);
        Assert.Equal("T,1", parsed.Name);
        Assert.Equal("C@2", parsed.Category);
        Assert.Equal(2, parsed.Children!.Count);
        Assert.Contains(weird, parsed.Children);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("build:")]
    [InlineData("build:thing/")]
    [InlineData("build:terrain/")]
    [InlineData("build:other/X")]
    [InlineData("build:thing")]
    [InlineData("dropdown:")]
    [InlineData("order:")]
    [InlineData("order:#1")]
    [InlineData("order:T#")]
    [InlineData("order:T#x")]
    [InlineData("order:T#-1")]
    [InlineData("order:T#01")]
    [InlineData("order:T#1#2")]
    [InlineData("order:T#99999999999")]
    [InlineData("moddrop:")]
    [InlineData("moddrop:T")]
    [InlineData("moddrop:T@C")]
    [InlineData("moddrop:T@C~")]
    [InlineData("moddrop:@C~build:thing/A")]
    [InlineData("moddrop:T@~build:thing/A")]
    [InlineData("moddrop:T@C~build:thing/A,")]
    [InlineData("moddrop:T@C~,build:thing/A")]
    [InlineData("moddrop:T@C~junk")]
    [InlineData("moddrop:T@C~moddrop:U@D~build:thing/A")]
    [InlineData("unknown:thing/X")]
    [InlineData("Build:thing/X")]
    [InlineData("build:thing/a,b")]
    [InlineData("build:thing/a|b")]
    [InlineData("build:thing/a~b")]
    [InlineData("build:thing/a@b")]
    [InlineData("build:thing/a#b")]
    [InlineData("build:thing/a\\")]
    [InlineData("build:thing/a\\xb")]
    [InlineData("build:thing/\\")]
    public void TryParse_Malformed_ReturnsFalseWithoutThrowing(string? text)
    {
        Assert.False(PinKey.TryParse(text, out PinKey? key));
        Assert.Null(key);
    }

    [Fact]
    public void Factories_RejectInvalidArguments()
    {
        Assert.Throws<System.ArgumentException>(() => PinKey.Build(PinKeyKind.Order, "X"));
        Assert.Throws<System.ArgumentException>(() => PinKey.Build(PinKeyKind.BuildThing, ""));
        Assert.Throws<System.ArgumentException>(() => PinKey.Dropdown(null!));
        Assert.Throws<System.ArgumentException>(() => PinKey.Order(""));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => PinKey.Order("T", -1));
        Assert.Throws<System.ArgumentException>(() => PinKey.ModDropdown("T", "C", new List<PinKey>()));
        Assert.Throws<System.ArgumentException>(() => PinKey.ModDropdown("T", "", new[] { PinKey.Dropdown("A") }));
        Assert.Throws<System.ArgumentNullException>(() => PinKey.ModDropdown("T", "C", null!));
        Assert.Throws<System.ArgumentNullException>(() => PinKey.ModDropdown("T", "C", new PinKey[] { null! }));
        PinKey inner = PinKey.ModDropdown("T", "C", new[] { PinKey.Dropdown("A") });
        Assert.Throws<System.ArgumentException>(() => PinKey.ModDropdown("U", "C", new[] { inner }));
    }
}
