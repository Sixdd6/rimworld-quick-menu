using System;
using System.Collections.Generic;
using QuickMenu.Core.Pins;
using RimWorld;
using Verse;
using Log = QuickMenu.Diagnostics.Log;

namespace QuickMenu.Catalog;

/// <summary>
/// The reason an unavailable pinned slot shows (FR-14, design.md S4 state 5). The designator's own <c>disabledReason</c>
/// wins; otherwise the causes <c>Designator_Build.Visible</c> checks (S-5) are found on the placed def and composed from
/// game strings where they exist; otherwise "Not available right now." (U-4). The choice and ordering are in Core
/// (<see cref="UnavailableReason"/>). Called at most once per slot per open, never per frame, and never throws: each cause
/// is probed on its own so one bad mod def loses only its own sentence.
/// </summary>
internal static class AvailabilityResolver
{
    /// <param name="designator">The designator behind the greyed slot.</param>
    /// <param name="notOffered">
    /// True when the game's own designator lists do not offer it (a synthetic pin entry, see
    /// <see cref="CatalogEntry.ForcedUnavailable"/>); that is how an Ideology precept or meme gate shows up.
    /// </param>
    internal static string ReasonFor(Designator designator, bool notOffered = false)
    {
        string? own = null;
        try
        {
            own = designator.disabledReason;
        }
        catch (Exception ex)
        {
            Log.LogOnce("Availability.Own", $"Reading a designator's disabled reason failed: {ex.GetType().Name}");
        }

        List<ReasonPart>? parts = null;
        if (string.IsNullOrWhiteSpace(own))
        {
            parts = new List<ReasonPart>(4);
            Compose(designator, notOffered, parts);
        }

        return UnavailableReason.SelectFrom(own, parts, "QuickMenu.Menu.Unavailable".Translate());
    }

    private static void Compose(Designator designator, bool notOffered, List<ReasonPart> parts)
    {
        try
        {
            switch (designator)
            {
                case Designator_Build { PlacingDef: { } def }:
                    ComposeBuild(def, notOffered, parts);
                    break;
                case Designator_Dropdown dropdown:
                    foreach (Designator element in dropdown.Elements)
                    {
                        if (element is Designator_Build { PlacingDef: { } elementDef })
                        {
                            ComposeBuild(elementDef, notOffered: false, parts);
                        }
                    }

                    break;
                case Designator_MechControlGroup:
                    Probe("Mechanitor", () =>
                    {
                        if (ModsConfig.BiotechActive && !AnyMechanitor())
                        {
                            parts.Add(new ReasonPart(ReasonCause.Mechanitor, "QuickMenu.Menu.ReasonMechanitor".Translate()));
                        }
                    });
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.LogOnce("Availability.Compose", $"Composing an unavailable reason failed, the fallback is used: {ex.GetType().Name}");
        }
    }

    private static void ComposeBuild(BuildableDef def, bool notOffered, List<ReasonPart> parts)
    {
        Probe("Research", () => AddResearch(def, parts));
        Probe("Buildings", () => AddBuildings(def, parts));
        Probe("Tech", () => AddTechLevel(def, parts));
        Probe("Monolith", () => AddMonolith(def, parts));
        Probe("GravEngine", () => AddGravEngine(def, parts));
        Probe("Difficulty", () => AddDifficulty(def, parts));
        Probe("PlaceWorkers", () => AddPlaceWorkers(def, parts));
        Probe("Discovery", () => AddDiscovery(def, parts));
        if (notOffered)
        {
            Probe("Ideology", () => AddIdeology(def, parts));
        }
    }

    private static void Probe(string name, Action probe)
    {
        try
        {
            probe();
        }
        catch (Exception ex)
        {
            Log.LogOnce("Availability." + name, $"Probing the {name} cause of an unavailable slot failed: {ex.GetType().Name}");
        }
    }

    private static void AddResearch(BuildableDef def, List<ReasonPart> parts)
    {
        List<ResearchProjectDef>? research = def.researchPrerequisites;
        if (research == null)
        {
            return;
        }

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
            parts.Add(new ReasonPart(ReasonCause.Research, "QuickMenu.Menu.ReasonResearch".Translate(string.Join(", ", missing))));
        }
    }

    private static void AddBuildings(BuildableDef def, List<ReasonPart> parts)
    {
        Map? map = Find.CurrentMap;
        List<ThingDef>? buildings = def.buildingPrerequisites;
        if (buildings == null || map == null)
        {
            return;
        }

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
            parts.Add(new ReasonPart(ReasonCause.BuildingPrerequisite, "QuickMenu.Menu.ReasonBuildings".Translate(string.Join(", ", missing))));
        }
    }

    private static void AddTechLevel(BuildableDef def, List<ReasonPart> parts)
    {
        TechLevel colony = Faction.OfPlayer.def.techLevel;
        if (def.minTechLevelToBuild != TechLevel.Undefined && colony < def.minTechLevelToBuild)
        {
            parts.Add(new ReasonPart(ReasonCause.TechLevel, "QuickMenu.Menu.ReasonTechMin".Translate(def.minTechLevelToBuild.ToStringHuman())));
        }
        else if (def.maxTechLevelToBuild != TechLevel.Undefined && colony > def.maxTechLevelToBuild)
        {
            parts.Add(new ReasonPart(ReasonCause.TechLevel, "QuickMenu.Menu.ReasonTechMax".Translate(def.maxTechLevelToBuild.ToStringHuman())));
        }
    }

    private static void AddMonolith(BuildableDef def, List<ReasonPart> parts)
    {
        if (ModsConfig.AnomalyActive && def.minMonolithLevel > Find.Anomaly.HighestLevelReached && Find.Anomaly.GenerateMonolith)
        {
            parts.Add(new ReasonPart(ReasonCause.Monolith, "QuickMenu.Menu.ReasonMonolith".Translate(def.minMonolithLevel)));
        }
    }

    private static void AddGravEngine(BuildableDef def, List<ReasonPart> parts)
    {
        if (ModsConfig.OdysseyActive && def.requireInspectedGravEngine && !Find.ResearchManager.gravEngineInspected)
        {
            parts.Add(new ReasonPart(ReasonCause.GravEngine, "QuickMenu.Menu.ReasonGravEngine".Translate()));
        }
    }

    private static void AddDifficulty(BuildableDef def, List<ReasonPart> parts)
    {
        if (!Find.Storyteller.difficulty.AllowedToBuild(def))
        {
            parts.Add(new ReasonPart(ReasonCause.Difficulty, "QuickMenu.Menu.ReasonDifficulty".Translate()));
        }
    }

    private static void AddPlaceWorkers(BuildableDef def, List<ReasonPart> parts)
    {
        if (def is not ThingDef { PlaceWorkers: { } workers })
        {
            return;
        }

        foreach (PlaceWorker worker in workers)
        {
            if (worker != null && !worker.IsBuildDesignatorVisible(def))
            {
                parts.Add(new ReasonPart(ReasonCause.PlaceWorker, "QuickMenu.Menu.ReasonPlaceWorker".Translate()));
                return;
            }
        }
    }

    private static void AddDiscovery(BuildableDef def, List<ReasonPart> parts)
    {
        List<ThingDef>? discovery = def.discoveryPrerequisites;
        if (discovery == null)
        {
            return;
        }

        foreach (ThingDef item in discovery)
        {
            if (item != null && Find.HiddenItemsManager.Hidden(item))
            {
                parts.Add(new ReasonPart(ReasonCause.Discovery, "QuickMenu.Menu.ReasonDiscovery".Translate(item.LabelCap)));
                return;
            }
        }
    }

    /// <summary>
    /// Ideology builds exist only in the designator lists of an ideoligion whose precept or meme adds them (see
    /// <c>DesignationCategoryDef.AllIdeoDesignators</c>), so a build the game does not offer is gated by one of those.
    /// </summary>
    private static void AddIdeology(BuildableDef def, List<ReasonPart> parts)
    {
        if (!ModsConfig.IdeologyActive)
        {
            return;
        }

        var memes = new List<string>();
        foreach (MemeDef meme in DefDatabase<MemeDef>.AllDefsListForReading)
        {
            if (meme.addDesignators != null && meme.addDesignators.Contains(def))
            {
                memes.Add(meme.LabelCap.ToString());
            }
        }

        if (memes.Count > 0)
        {
            parts.Add(new ReasonPart(ReasonCause.Precept, "QuickMenu.Menu.ReasonMeme".Translate(string.Join(", ", memes))));
        }
        else if (def.ideoBuilding)
        {
            parts.Add(new ReasonPart(ReasonCause.Precept, "QuickMenu.Menu.ReasonPrecept".Translate()));
        }
    }

    private static bool AnyMechanitor()
    {
        Map? map = Find.CurrentMap;
        if (map == null)
        {
            return true; // unknown: do not claim a cause
        }

        foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
        {
            if (MechanitorUtility.IsMechanitor(pawn))
            {
                return true;
            }
        }

        return false;
    }
}
