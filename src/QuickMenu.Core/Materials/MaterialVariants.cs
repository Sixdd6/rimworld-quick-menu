using System;
using System.Collections.Generic;

namespace QuickMenu.Core.Materials;

/// <summary>
/// One dropdown element reduced to plain data for <see cref="MaterialVariants.IsMaterialVariantSet"/>.
/// <see cref="Kind"/> identifies what is built independent of material (for vanilla terrain, its texture path); null when
/// unknown. <see cref="SingleIngredientId"/> is the id of the one cost ingredient, or null when the element costs zero or
/// several ingredients.
/// </summary>
public readonly record struct VariantElement(string? Kind, string? SingleIngredientId);

/// <summary>
/// Detects a dropdown whose elements are the same item in different materials (FR-11a), e.g. the vanilla fine tile, flagstone
/// and metal tile floors (one terrain per stone or metal, each costing one kind of block). Such a dropdown shows each option
/// with the icon of the material it costs. Carpets do not match (all cost the same cloth, they differ by dye colour).
/// </summary>
public static class MaterialVariants
{
    /// <summary>
    /// True when there are at least two elements, every element has a known kind and exactly one cost ingredient, all
    /// kinds are equal and all ingredients are distinct.
    /// </summary>
    public static bool IsMaterialVariantSet(IReadOnlyList<VariantElement> elements)
    {
        ArgumentNullException.ThrowIfNull(elements);
        if (elements.Count < 2)
        {
            return false;
        }

        string? kind = elements[0].Kind;
        if (string.IsNullOrEmpty(kind))
        {
            return false;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < elements.Count; i++)
        {
            VariantElement e = elements[i];
            if (!string.Equals(e.Kind, kind, StringComparison.Ordinal) || string.IsNullOrEmpty(e.SingleIngredientId) || !seen.Add(e.SingleIngredientId))
            {
                return false;
            }
        }

        return true;
    }
}
