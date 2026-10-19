using System;
using System.Collections.Generic;
using QuickMenu.Core.Pins;
using Xunit;

namespace QuickMenu.Tests.Pins;

public class PinKeyFactoryTests
{
    private static DesignatorDescriptor Thing(string def, string? group = null, string cat = "Structure") =>
        new(DescriptorKind.BuildThing, "RimWorld.Designator_Build", def, cat, group);

    private static DesignatorDescriptor Terrain(string def, string? group = null) =>
        new(DescriptorKind.BuildTerrain, "RimWorld.Designator_Build", def, "Floors", group);

    private static DesignatorDescriptor Other(string type, string cat = "Orders") =>
        new(DescriptorKind.Other, type, null, cat);

    private static DesignatorDescriptor Drop(string type, string cat, params DesignatorDescriptor[] children) =>
        new(DescriptorKind.Dropdown, type, null, cat, null, children);

    private static string[] Keys(params DesignatorDescriptor[] d)
    {
        IReadOnlyList<PinKey> keys = PinKeyFactory.Assign(d);
        var result = new string[keys.Count];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = keys[i].Value;
        }

        return result;
    }

    [Fact]
    public void Builds_GetBuildKeysBySubKind()
    {
        Assert.Equal(["build:thing/Wall", "build:terrain/Concrete"], Keys(Thing("Wall"), Terrain("Concrete")));
    }

    [Fact]
    public void Dropdown_WhoseChildrenShareOneGroup_IsDefDropdown()
    {
        string[] keys = Keys(Drop("RimWorld.Designator_Dropdown", "Furniture", Thing("Armchair", "Chairs"), Thing("Stool", "Chairs")));
        Assert.Equal(["dropdown:Chairs", "build:thing/Armchair", "build:thing/Stool"], keys);
    }

    [Fact]
    public void Dropdown_WithMixedGroups_IsModDropdownWithSortedChildren()
    {
        string[] keys = Keys(Drop("My.Drop", "Furniture", Thing("Stool", "A"), Thing("Armchair", "B")));
        Assert.Equal(
            ["moddrop:My.Drop@Furniture~build:thing/Armchair,build:thing/Stool", "build:thing/Stool", "build:thing/Armchair"],
            keys);
    }

    [Fact]
    public void Dropdown_WithUngroupedOrNonBuildChild_IsModDropdown()
    {
        Assert.StartsWith("moddrop:", Keys(Drop("T", "C", Thing("A", "G"), Thing("B")))[0], StringComparison.Ordinal);
        Assert.StartsWith("moddrop:", Keys(Drop("T", "C", Thing("A", "G"), Other("X.Tool")))[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Dropdown_ChildrenCanBeTerrain()
    {
        Assert.Equal("dropdown:Floors", Keys(Drop("D", "C", Terrain("A", "Floors"), Terrain("B", "Floors")))[0]);
    }

    [Fact]
    public void Dropdown_WithoutElements_FallsBackToOrder()
    {
        Assert.Equal(["order:My.EmptyDrop"], Keys(Drop("My.EmptyDrop", "C")));
    }

    [Fact]
    public void ModDropdown_IgnoresNestedModDropdowns()
    {
        string[] keys = Keys(Drop("Outer", "C", Thing("A", "G"), Thing("B"), Drop("Inner", "C", Thing("Z", "Q"), Thing("Y"))));
        Assert.Equal("moddrop:Outer@C~build:thing/A,build:thing/B", keys[0]);
    }

    [Fact]
    public void Other_UniqueType_HasNoOrdinal()
    {
        Assert.Equal(["order:RimWorld.Designator_Mine", "order:RimWorld.Designator_Hunt"], Keys(Other("RimWorld.Designator_Mine"), Other("RimWorld.Designator_Hunt")));
    }

    [Fact]
    public void Other_DuplicateType_GetsOrdinalsInInputOrder()
    {
        string[] keys = Keys(Other("Z.Zone", "Zone"), Other("A.Other"), Other("Z.Zone", "Zone2"), Other("Z.Zone", "Zone3"));
        Assert.Equal(["order:Z.Zone#1", "order:A.Other", "order:Z.Zone#2", "order:Z.Zone#3"], keys);
    }

    [Fact]
    public void Ordinals_CountDesignatorsInsideDropdowns_InPreOrder()
    {
        string[] keys = Keys(Other("T"), Drop("D", "C", Other("T"), Thing("A", "G")), Other("T"));
        Assert.Equal("order:T#1", keys[0]);
        Assert.Equal("order:T#2", keys[2]);
        Assert.Equal("order:T#3", keys[4]);
    }

    [Fact]
    public void Assign_IsDeterministicAndKeysAreUniqueForDistinctInput()
    {
        DesignatorDescriptor[] input = [Other("T"), Other("T"), Thing("A"), Terrain("A"), Drop("D", "C", Thing("X", "G"), Thing("Y", "G"))];
        string[] first = Keys(input);
        Assert.Equal(first, Keys(input));
        Assert.Equal(first.Length, new HashSet<string>(first).Count);
    }

    [Fact]
    public void Assign_EmptyList_ReturnsEmpty() => Assert.Empty(PinKeyFactory.Assign([]));

    [Fact]
    public void Assign_KeysRoundTripThroughParse()
    {
        foreach (string text in Keys(Other("T"), Other("T"), Drop("A.B", "Cat~", Thing("a,b"), Other("X"))))
        {
            Assert.True(PinKey.TryParse(text, out PinKey? key));
            Assert.Equal(text, key.Value);
        }
    }

    [Fact]
    public void Assign_BuildWithoutDefName_BecomesOrder()
    {
        var bad = new DesignatorDescriptor(DescriptorKind.BuildThing, "T", null, "C");
        Assert.Equal("order:T", Assert.Single(PinKeyFactory.Assign([bad])).Value);
    }

    [Fact]
    public void Assign_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => PinKeyFactory.Assign(null!));
        Assert.Throws<ArgumentNullException>(() => PinKeyFactory.Assign([null!]));
    }

    [Fact]
    public void Assign_AbsurdNesting_IsCutOff()
    {
        DesignatorDescriptor d = Other("T");
        for (int i = 0; i < 20; i++)
        {
            d = Drop("D", "C", d);
        }

        Assert.Equal(PinKeyFactory.MaxDepth + 1, PinKeyFactory.Assign([d]).Count);
    }
}
