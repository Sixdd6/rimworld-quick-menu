using QuickMenu.Core.Materials;
using Xunit;

namespace QuickMenu.Core.Tests.Materials;

public class MaterialVariantsTests
{
    private static VariantElement V(string? kind, string? ing) => new(kind, ing);

    [Fact]
    public void Same_kind_distinct_single_ingredients_is_a_variant_set()
    {
        Assert.True(MaterialVariants.IsMaterialVariantSet(new[]
        {
            V("Terrain/Surfaces/FineTile", "BlocksSandstone"),
            V("Terrain/Surfaces/FineTile", "BlocksGranite"),
            V("Terrain/Surfaces/FineTile", "BlocksMarble"),
        }));
    }

    [Fact]
    public void Carpets_sharing_one_ingredient_are_not()
    {
        Assert.False(MaterialVariants.IsMaterialVariantSet(new[] { V("Carpet", "Cloth"), V("Carpet", "Cloth") }));
    }

    [Fact]
    public void An_element_with_no_or_several_ingredients_disqualifies()
    {
        Assert.False(MaterialVariants.IsMaterialVariantSet(new[] { V("K", "A"), V("K", null) }));
    }

    [Fact]
    public void Different_kinds_disqualify()
    {
        Assert.False(MaterialVariants.IsMaterialVariantSet(new[] { V("K1", "A"), V("K2", "B") }));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Unknown_kind_disqualifies(string? kind)
    {
        Assert.False(MaterialVariants.IsMaterialVariantSet(new[] { V(kind, "A"), V(kind, "B") }));
    }

    [Fact]
    public void Fewer_than_two_elements_is_not_a_set()
    {
        Assert.False(MaterialVariants.IsMaterialVariantSet(System.Array.Empty<VariantElement>()));
        Assert.False(MaterialVariants.IsMaterialVariantSet(new[] { V("K", "A") }));
    }
}
