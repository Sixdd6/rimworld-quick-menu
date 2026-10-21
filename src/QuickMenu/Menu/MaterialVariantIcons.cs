using System;
using System.Collections.Generic;
using QuickMenu.Core.Materials;
using RimWorld;
using Verse;
using Log = QuickMenu.Diagnostics.Log;

namespace QuickMenu.Menu;

/// <summary>
/// Adapter for FR-11a: reads each dropdown element's build def into plain <see cref="VariantElement"/> data, asks Core
/// whether the dropdown is a material-variant set, and if so returns the material (cost ingredient) whose icon each option shows.
/// </summary>
internal static class MaterialVariantIcons
{
    /// <summary>One material per element, or null when the dropdown is not a material-variant set. Never throws.</summary>
    internal static ThingDef?[]? For(List<Designator> elements)
    {
        try
        {
            var data = new VariantElement[elements.Count];
            var materials = new ThingDef?[elements.Count];
            for (int i = 0; i < data.Length; i++)
            {
                if (elements[i] is not Designator_Build { PlacingDef: { } def })
                {
                    return null;
                }

                ThingDef? ingredient = null;
                List<ThingDefCountClass>? cost = def.costList;
                if (cost != null && cost.Count == 1 && !def.MadeFromStuff)
                {
                    ingredient = cost[0].thingDef;
                }

                string? kind = def is TerrainDef terrain ? terrain.texturePath : (def as ThingDef)?.graphicData?.texPath;
                data[i] = new VariantElement(kind, ingredient?.defName);
                materials[i] = ingredient;
            }

            return MaterialVariants.IsMaterialVariantSet(data) ? materials : null;
        }
        catch (Exception ex)
        {
            Log.LogOnce("Menu.MaterialVariantIcons", $"Reading dropdown materials failed, the options keep their own icons: {ex}");
            return null;
        }
    }
}
