using System;
using System.Collections.Generic;
using QuickMenu.Core.Pins;
using RimWorld;
using Verse;
using Log = QuickMenu.Diagnostics.Log;

namespace QuickMenu.Menu;

/// <summary>
/// The reason an unavailable pinned slot shows (FR-14, design.md S4 state 5): the designator's own disabled reason, else
/// requirements composed from the placed def (research, required buildings, tech level), else "Not available right now."
/// The choice itself is <see cref="UnavailableReason.Select"/> in Core. Called at most once per slot per open.
/// </summary>
internal static class UnavailableReasons
{
    internal static string For(Designator designator)
    {
        string? own = null;
        List<string?>? requirements = null;
        try
        {
            own = designator.disabledReason;
        }
        catch (Exception ex)
        {
            Log.LogOnce("Menu.Reason.Own", $"Reading a designator's disabled reason failed: {ex.GetType().Name}");
        }

        if (string.IsNullOrWhiteSpace(own) && designator is Designator_Build { PlacingDef: { } def })
        {
            requirements = Compose(def);
        }

        return UnavailableReason.Select(own, requirements, "QuickMenu.Menu.Unavailable".Translate());
    }

    private static List<string?> Compose(BuildableDef def)
    {
        var parts = new List<string?>(3);
        try
        {
            List<ResearchProjectDef>? research = def.researchPrerequisites;
            if (research != null)
            {
                var missing = new List<string>();
                foreach (ResearchProjectDef project in research)
                {
                    if (project != null && !project.IsFinished)
                    {
                        missing.Add(project.LabelCap.ToString());
                    }
                }

                if (missing.Count > 0)
                {
                    parts.Add("QuickMenu.Menu.ReasonResearch".Translate(string.Join(", ", missing)));
                }
            }

            Map? map = Find.CurrentMap;
            List<ThingDef>? buildings = def.buildingPrerequisites;
            if (buildings != null && map != null)
            {
                var missing = new List<string>();
                foreach (ThingDef building in buildings)
                {
                    if (building != null && !map.listerBuildings.ColonistsHaveBuilding(building))
                    {
                        missing.Add(building.LabelCap.ToString());
                    }
                }

                if (missing.Count > 0)
                {
                    parts.Add("QuickMenu.Menu.ReasonBuildings".Translate(string.Join(", ", missing)));
                }
            }

            TechLevel colony = Faction.OfPlayer.def.techLevel;
            if (def.minTechLevelToBuild != TechLevel.Undefined && colony < def.minTechLevelToBuild)
            {
                parts.Add("QuickMenu.Menu.ReasonTechMin".Translate(def.minTechLevelToBuild.ToStringHuman()));
            }
            else if (def.maxTechLevelToBuild != TechLevel.Undefined && colony > def.maxTechLevelToBuild)
            {
                parts.Add("QuickMenu.Menu.ReasonTechMax".Translate(def.maxTechLevelToBuild.ToStringHuman()));
            }
        }
        catch (Exception ex)
        {
            Log.LogOnce("Menu.Reason.Compose", $"Composing an unavailable reason failed, the fallback is used: {ex.GetType().Name}");
        }

        return parts;
    }
}
