using System;
using System.Collections.Generic;
using System.Linq;
using QuickMenu.Core.Pins;
using Xunit;

namespace QuickMenu.Tests.Pins;

/// <summary>Edge cases for <see cref="PinKeyFactory"/> beyond the ADR 0004 rule tests.</summary>
public class PinKeyFactoryEdgeTests
{
    private static DesignatorDescriptor Thing(string def, string? group = null, string cat = "Structure") =>
        new(DescriptorKind.BuildThing, "RimWorld.Designator_Build", def, cat, group);

    private static DesignatorDescriptor Terrain(string def, string? group = null) =>
        new(DescriptorKind.BuildTerrain, "RimWorld.Designator_Build", def, "Floors", group);

    private static DesignatorDescriptor Other(string type, string cat = "Orders") =>
        new(DescriptorKind.Other, type, null, cat);

    private static DesignatorDescriptor Drop(string type, string cat, params DesignatorDescriptor[] children) =>
        new(DescriptorKind.Dropdown, type, null, cat, null, children);

    private static string[] Keys(params DesignatorDescriptor[] d) =>
        PinKeyFactory.Assign(d).Select(static k => k.Value).ToArray();

    [Fact]
    public void EmptyDropdowns_OfSameType_GetOrdinals()
    {
        Assert.Equal(["order:My.Empty#1", "order:My.Empty#2"], Keys(Drop("My.Empty", "A"), Drop("My.Empty", "B")));
    }

    [Fact]
    public void EmptyDropdown_AndOtherOfSameType_ShareOrdinalSequence()
    {
        Assert.Equal(["order:T#1", "order:T#2"], Keys(Other("T"), Drop("T", "C")));
    }

    [Fact]
    public void NonEmptyDropdown_OfSameTypeAsOrder_DoesNotConsumeOrdinal()
    {
        string[] keys = Keys(Other("T"), Drop("T", "C", Thing("A", "G")), Other("T"));
        Assert.Equal("order:T#1", keys[0]);
        Assert.Equal("order:T#2", keys[^1]);
    }

    [Fact]
    public void EmptyDropdown_NestedInsideModDropdown_IsKeptAsOrderChild()
    {
        string[] keys = Keys(Drop("Outer", "C", Thing("A"), Drop("Inner", "C")));
        Assert.Equal("moddrop:Outer@C~build:thing/A,order:Inner", keys[0]);
        Assert.Equal("order:Inner", keys[2]);
    }

    [Fact]
    public void ModDropdown_WithNestedDefDropdown_KeepsNestedKeyAsChild()
    {
        // The nested dropdown is a def dropdown (not a mod dropdown), so it stays in the parent's child list.
        string[] keys = Keys(Drop("Outer", "C", Thing("A"), Drop("Inner", "C", Thing("X", "G"), Thing("Y", "G"))));
        Assert.Equal("moddrop:Outer@C~build:thing/A,dropdown:G", keys[0]);
    }

    [Fact]
    public void DefDropdownChildren_NestedDropdown_MakesParentModDropdown()
    {
        // A dropdown child is not a build, so the parent cannot be a def dropdown.
        string[] keys = Keys(Drop("Outer", "C", Thing("A", "G"), Drop("Inner", "C", Thing("X", "H"), Thing("Y", "H"))));
        Assert.StartsWith("moddrop:Outer@C~", keys[0], StringComparison.Ordinal);
    }

    [Fact]
    public void ModDropdown_WithTwoNestedModDropdownsAndOneBuild_UsesOnlyTheBuild()
    {
        string[] keys = Keys(Drop("O", "C", Drop("I1", "C", Thing("A", "G"), Thing("B")), Thing("K"), Drop("I2", "C", Thing("C1", "G"), Thing("D"))));
        Assert.Equal("moddrop:O@C~build:thing/K", keys[0]);
        Assert.Equal(8, keys.Length);
    }

    [Fact]
    public void ThreeLevelNesting_KeysArePreOrderAndUnique()
    {
        string[] keys = Keys(Drop("L1", "C", Thing("A", "G"), Drop("L2", "C", Thing("B", "G"), Drop("L3", "C", Thing("C", "H"), Thing("D", "I")))));
        Assert.Equal(7, keys.Length);
        Assert.StartsWith("moddrop:L1@", keys[0], StringComparison.Ordinal);
        Assert.StartsWith("moddrop:L2@", keys[2], StringComparison.Ordinal);
        Assert.StartsWith("moddrop:L3@", keys[4], StringComparison.Ordinal);
        Assert.Equal(keys.Length, keys.Distinct().Count());
    }

    [Fact]
    public void MixedBuildGroups_ThingAndTerrainInSameGroup_IsDefDropdown()
    {
        Assert.Equal("dropdown:G", Keys(Drop("D", "C", Thing("A", "G"), Terrain("B", "G")))[0]);
    }

    [Fact]
    public void EmptyStringGroup_IsTreatedAsUngrouped()
    {
        Assert.StartsWith("moddrop:", Keys(Drop("D", "C", Thing("A", ""), Thing("B", "")))[0], StringComparison.Ordinal);
    }

    [Fact]
    public void GroupNames_AreCaseSensitive()
    {
        Assert.StartsWith("moddrop:", Keys(Drop("D", "C", Thing("A", "g"), Thing("B", "G")))[0], StringComparison.Ordinal);
    }

    [Fact]
    public void SingleChildDropdown_WithGroup_IsDefDropdown()
    {
        Assert.Equal(["dropdown:G", "build:thing/A"], Keys(Drop("D", "C", Thing("A", "G"))));
    }

    [Fact]
    public void ModDropdown_DuplicateChildren_AreCollapsedInKey()
    {
        string[] keys = Keys(Drop("D", "C", Thing("A", "G"), Thing("A"), Thing("B", "H")));
        Assert.Equal("moddrop:D@C~build:thing/A,build:thing/B", keys[0]);
        Assert.Equal(4, keys.Length);
    }

    [Fact]
    public void ModDropdown_ChildOrder_DoesNotChangeItsOwnKey()
    {
        string a = Keys(Drop("D", "C", Thing("A", "G"), Thing("B"), Other("X.Tool")))[0];
        string b = Keys(Drop("D", "C", Other("X.Tool"), Thing("B"), Thing("A", "G")))[0];
        Assert.Equal(a, b);
    }

    [Fact]
    public void ModDropdown_ChildOrdinals_AppearInChildKeys()
    {
        string[] keys = Keys(Other("T"), Drop("D", "C", Other("T"), Thing("A")), Other("T"));
        Assert.Equal("moddrop:D@C~build:thing/A,order:T#2", keys[1]);
    }

    [Fact]
    public void ModDropdown_KeyDependsOnCategory()
    {
        Assert.NotEqual(Keys(Drop("D", "C1", Thing("A")))[0], Keys(Drop("D", "C2", Thing("A")))[0]);
    }

    [Fact]
    public void OrderOrdinals_IgnoreCategory_FollowGivenOrder()
    {
        Assert.Equal(["order:T#1", "order:T#2", "order:T#3"], Keys(Other("T", "Z"), Other("T", "A"), Other("T", "M")));
    }

    [Fact]
    public void ManyDuplicateTypes_GetConsecutiveOrdinals()
    {
        DesignatorDescriptor[] input = Enumerable.Range(0, 2000).Select(static _ => Other("T")).ToArray();
        string[] keys = Keys(input);
        for (int i = 0; i < keys.Length; i++)
        {
            Assert.Equal($"order:T#{i + 1}", keys[i]);
        }
    }

    [Fact]
    public void OrdinalsAreIndependentPerType()
    {
        string[] keys = Keys(Other("A"), Other("B"), Other("A"), Other("B"), Other("C"));
        Assert.Equal(["order:A#1", "order:B#1", "order:A#2", "order:B#2", "order:C"], keys);
    }

    [Fact]
    public void Assign_DoesNotDependOnOrderOfUnrelatedDescriptors_ForBuildAndDefDropdownKeys()
    {
        DesignatorDescriptor[] a = [Thing("A"), Drop("D", "C", Thing("X", "G"), Thing("Y", "G")), Terrain("B")];
        DesignatorDescriptor[] b = [Terrain("B"), Thing("A"), Drop("D", "C", Thing("X", "G"), Thing("Y", "G"))];
        Assert.Equal(Keys(a).OrderBy(static s => s, StringComparer.Ordinal), Keys(b).OrderBy(static s => s, StringComparer.Ordinal));
    }

    [Fact]
    public void Assign_SameInputInstancesTwice_GivesIdenticalKeys()
    {
        DesignatorDescriptor[] input = [Other("T"), Drop("D", "C", Other("T"), Thing("A")), Other("T"), Drop("E", "C")];
        Assert.Equal(Keys(input), Keys(input));
    }

    [Fact]
    public void Assign_DoesNotMutateOrReuseState_BetweenCalls()
    {
        Assert.Equal(["order:T"], Keys(Other("T")));
        Assert.Equal(["order:T"], Keys(Other("T")));
    }

    [Fact]
    public void Assign_SameDefDropdownGroupTwice_ProducesDuplicateKeys_ForSelfTestToReport()
    {
        // Documents current behaviour: the factory does not deduplicate; the T-9 self-test reports duplicates.
        string[] keys = Keys(
            Drop("D", "C1", Thing("A", "G"), Thing("B", "G")),
            Drop("D", "C2", Thing("C", "G"), Thing("D", "G")));
        Assert.Equal(2, keys.Count(static k => k == "dropdown:G"));
    }

    [Fact]
    public void Assign_SameBuildTwice_ProducesDuplicateKeys()
    {
        string[] keys = Keys(Thing("A"), Thing("A"));
        Assert.Equal(["build:thing/A", "build:thing/A"], keys);
    }

    [Fact]
    public void Assign_BuildThingAndTerrainWithSameDefName_AreDistinct()
    {
        Assert.Equal(["build:thing/A", "build:terrain/A"], Keys(Thing("A"), Terrain("A")));
    }

    [Fact]
    public void Assign_BuildWithEmptyDefName_BecomesOrder()
    {
        Assert.Equal(["order:RimWorld.Designator_Build"], Keys(Thing("")));
    }

    [Fact]
    public void Assign_BuildWithoutDefName_NestedInDropdown_BecomesOrderChild()
    {
        var bad = new DesignatorDescriptor(DescriptorKind.BuildTerrain, "T", null, "C", "G");
        Assert.Equal(["moddrop:D@C~order:T", "order:T"], Keys(Drop("D", "C", bad)));
    }

    [Theory]
    [InlineData("Ünï.Cödé")]
    [InlineData("名前.空間")]
    [InlineData("a,b~c@d#e|f\\g")]
    public void Unicode_AndSpecialCharacters_RoundTripThroughParse(string name)
    {
        foreach (string text in Keys(Other(name), Other(name), Thing(name), Drop(name, name, Thing(name), Other(name))))
        {
            Assert.True(PinKey.TryParse(text, out PinKey? key), text);
            Assert.Equal(text, key.Value);
        }
    }

    [Fact]
    public void Depth_Boundary_NineLevelsKeptInFull_TenCutOff()
    {
        Assert.Equal(17, PinKeyFactory.Assign([Nest(9)]).Count);
        IReadOnlyList<PinKey> cut = PinKeyFactory.Assign([Nest(10)]);
        Assert.Equal(17, cut.Count);
        Assert.DoesNotContain(cut, static k => k.Value == "order:Leaf");
    }

    [Fact]
    public void Depth_LimitIsAppliedToEachRootIndependently()
    {
        Assert.Equal(34, PinKeyFactory.Assign([Nest(9), Nest(9)]).Count);
    }

    [Fact]
    public void Depth_DeepChainBeyondLimit_IsCutOffInsteadOfOverflowing()
    {
        Assert.Equal(17, PinKeyFactory.Assign([Nest(10_000)]).Count);
    }

    [Fact]
    public void Dropdown_AtCutOffDepth_WithOnlyCutChildren_BecomesOrder()
    {
        DesignatorDescriptor d = Other("T");
        for (int i = 0; i < PinKeyFactory.MaxDepth + 1; i++)
        {
            d = Drop("D", "C", d);
        }

        IReadOnlyList<PinKey> keys = PinKeyFactory.Assign([d]);
        Assert.Equal(PinKeyFactory.MaxDepth + 1, keys.Count);
        Assert.Equal(PinKeyKind.Order, keys[^1].Kind);
    }

    [Fact]
    public void Dropdown_WhoseOnlyChildrenAreModDropdowns_BecomesOrder()
    {
        DesignatorDescriptor inner = Drop("Inner", "C", Other("X"));
        Assert.Equal(["order:Outer", "moddrop:Inner@C~order:X", "order:X"], Keys(Drop("Outer", "C", inner)));
    }

    [Fact]
    public void Dropdown_WhoseOnlyChildrenAreModDropdowns_CountsTowardsOrdinals()
    {
        Assert.Equal(["order:Outer#1", "moddrop:Inner@C~order:X", "order:X", "order:Outer#2"], Keys(Drop("Outer", "C", Drop("Inner", "C", Other("X"))), Other("Outer")));
    }

    [Fact]
    public void Assign_EmptyTypeAndCategory_DoNotThrow()
    {
        Assert.Equal(["moddrop:(unknown)@(unknown)~order:(unknown)", "order:(unknown)"], Keys(Drop("", "", Other(""))));
    }

    [Fact]
    public void WideDropdown_OfThousandsOfChildren_IsHandled()
    {
        DesignatorDescriptor[] kids = Enumerable.Range(0, 5000).Select(static i => Thing("D" + i, "G")).ToArray();
        IReadOnlyList<PinKey> keys = PinKeyFactory.Assign([Drop("D", "C", kids)]);
        Assert.Equal(5001, keys.Count);
        Assert.Equal("dropdown:G", keys[0].Value);
    }

    [Fact]
    public void WideModDropdown_KeyChildrenAreSorted()
    {
        DesignatorDescriptor[] kids = Enumerable.Range(0, 1000).Reverse().Select(static i => Thing("D" + i)).ToArray();
        string key = PinKeyFactory.Assign([Drop("D", "C", kids)])[0].Value;
        Assert.True(PinKey.TryParse(key, out PinKey? parsed));
        string[] values = parsed.Children!.Select(static c => c.Value).ToArray();
        Assert.Equal(values.OrderBy(static s => s, StringComparer.Ordinal), values);
    }

    [Fact]
    public void Assign_EmptyDropdownsInAList_DoNotThrow()
    {
        Assert.Equal(["order:A", "order:B"], Keys(Drop("A", "C"), Drop("B", "C")));
    }

    private static DesignatorDescriptor Nest(int levels)
    {
        DesignatorDescriptor d = Other("Leaf");
        for (int i = 1; i < levels; i++)
        {
            d = Drop("D" + i, "C", d, Thing("B" + i));
        }

        return d;
    }
}
