using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using LudeonTK;
using QuickMenu.Catalog;
using QuickMenu.Core.Pins;
using RimWorld;
using Verse;

namespace QuickMenu.Diagnostics.Spike;

/// <summary>
/// Spike S-5: the full catalog dump (one line per entry, so it is long) and the dev-only / unavailable-reason probes
/// that <see cref="SelfTest"/> summarises. Spike-only; nothing here runs outside a debug action.
/// </summary>
internal static class CatalogDump
{
    /// <summary>Reads <c>Designator.Visible</c> without letting a bad mod designator throw.</summary>
    internal static string VisibleText(Designator designator)
    {
        try
        {
            return designator.Visible ? "true" : "false";
        }
        catch (Exception ex)
        {
            return "threw:" + ex.GetType().Name;
        }
    }

    internal static string DisabledText(Designator designator, out string reason)
    {
        reason = string.Empty;
        try
        {
            reason = designator.disabledReason ?? string.Empty;
            return designator.Disabled ? "true" : "false";
        }
        catch (Exception ex)
        {
            return "threw:" + ex.GetType().Name;
        }
    }

    /// <summary>True when the designator is visible with god mode on but not with it off (restores the setting).</summary>
    internal static bool AppearsOnlyInGodMode(Designator designator, out bool visibleNow)
    {
        bool saved = DebugSettings.godMode;
        try
        {
            DebugSettings.godMode = false;
            bool off = SafeVisible(designator);
            DebugSettings.godMode = true;
            bool on = SafeVisible(designator);
            visibleNow = saved ? on : off;
            return on && !off;
        }
        finally
        {
            DebugSettings.godMode = saved;
        }
    }

    /// <summary>Name-based hint that a designator or category belongs to development tooling.</summary>
    internal static bool LooksDevOnly(CatalogEntry entry)
    {
        string type = entry.Designator.GetType().Name;
        return Contains(type, "Dev") || Contains(type, "Debug") || Contains(type, "God") ||
               Contains(entry.Key.Value, "Debug") || Contains(entry.Key.Value, "God");
    }

    /// <summary>Which game checks make a build designator invisible or unavailable right now (S-5).</summary>
    internal static string BuildCauses(CatalogEntry entry)
    {
        if (entry.Designator is not Designator_Build build)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        try
        {
            BuildableDef def = build.PlacingDef;
            if (!def.IsResearchFinished)
            {
                sb.Append("research;");
            }

            if (def.researchPrerequisites is { Count: > 0 })
            {
                sb.Append("researchPrereqs=").Append(def.researchPrerequisites.Count).Append(';');
            }

            if (def.buildingPrerequisites is { Count: > 0 })
            {
                sb.Append("buildingPrereqs=").Append(def.buildingPrerequisites.Count).Append(';');
            }

            if (def.minTechLevelToBuild != TechLevel.Undefined || def.maxTechLevelToBuild != TechLevel.Undefined)
            {
                sb.Append("techLevel=").Append(def.minTechLevelToBuild).Append('-').Append(def.maxTechLevelToBuild).Append(';');
            }

            if (def is ThingDef thing)
            {
                if (thing.PlaceWorkers is { Count: > 0 })
                {
                    sb.Append("placeWorkers=").Append(thing.PlaceWorkers.Count).Append(';');
                }

                if (Contains(thing.defName, "Monolith"))
                {
                    sb.Append("monolith;");
                }

                if (Contains(thing.defName, "GravEngine"))
                {
                    sb.Append("gravEngine;");
                }
            }
        }
        catch (Exception ex)
        {
            sb.Append("threw:").Append(ex.GetType().Name);
        }

        return sb.ToString();
    }

    [DebugAction("Quick Menu", "catalog dump", allowedGameStates = AllowedGameStates.Playing)]
    private static void Dump()
    {
        IReadOnlyList<CatalogEntry> entries = DesignatorCatalog.Entries;
        var sb = new StringBuilder();
        sb.AppendLine(Log.Prefix + "BEGIN catalog dump entries=" + entries.Count.ToString(CultureInfo.InvariantCulture));
        var devOnly = new List<string>();
        foreach (CatalogEntry e in entries)
        {
            string disabled = DisabledText(e.Designator, out string reason);
            sb.Append(e.Id.ToString(CultureInfo.InvariantCulture)).Append(" key=").Append(e.Key.Value)
                .Append(" type=").Append(e.Designator.GetType().FullName)
                .Append(" category=").Append(e.CategoryLabel)
                .Append(" mod=").Append(e.PackageId)
                .Append(" visible=").Append(VisibleText(e.Designator))
                .Append(" disabled=").Append(disabled)
                .Append(" reason=").Append(reason)
                .Append(" causes=").Append(BuildCauses(e)).AppendLine();
            bool godOnly = false;
            try
            {
                godOnly = AppearsOnlyInGodMode(e.Designator, out _);
            }
            catch (Exception ex)
            {
                sb.AppendLine("  god-mode probe threw: " + ex.GetType().Name);
            }

            if (godOnly || LooksDevOnly(e))
            {
                devOnly.Add(e.Key.Value + (godOnly ? " [god-mode only]" : " [name hint]"));
            }
        }

        foreach (DesignationCategoryDef category in DefDatabase<DesignationCategoryDef>.AllDefsListForReading)
        {
            if (LooksDevName(category.defName))
            {
                devOnly.Add("category " + category.defName + " [name hint]");
            }
        }

        sb.AppendLine("dev-only candidates=" + devOnly.Count.ToString(CultureInfo.InvariantCulture));
        foreach (string line in devOnly)
        {
            sb.AppendLine("  " + line);
        }

        sb.AppendLine(Log.Prefix + "END catalog dump");
        Verse.Log.Message(sb.ToString());
    }

    private static bool LooksDevName(string name) => Contains(name, "Dev") || Contains(name, "Debug") || Contains(name, "God");

    private static bool SafeVisible(Designator designator)
    {
        try
        {
            return designator.Visible;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool Contains(string? text, string part) => text != null && text.Contains(part, StringComparison.OrdinalIgnoreCase);
}
