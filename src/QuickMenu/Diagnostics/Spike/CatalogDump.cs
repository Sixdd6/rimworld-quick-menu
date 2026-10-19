using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using LudeonTK;
using QuickMenu.Catalog;
using QuickMenu.Core.Diagnostics;
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
    private const int ChunkSize = 200;

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

    /// <summary>Name-based hint (type-name prefixes only, not measured) that a designator belongs to development tooling.</summary>
    internal static bool LooksDevOnly(CatalogEntry entry)
    {
        return DevNameHint.IsDevTypeName(entry.Designator.GetType().Name);
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

    /// <summary>How an entry fares in the god-mode probe (see <see cref="ClassifyGod"/>).</summary>
    internal enum GodClass
    {
        No,
        NoKnownCause,
        Explained,
    }

    /// <summary>
    /// Splits entries that appear only in god mode. <c>Designator_Build.Visible</c> returns true under god mode
    /// before the research, tech and prerequisite checks, so a locked build is god-only too: when
    /// <see cref="BuildCauses"/> names a cause the entry is <see cref="GodClass.Explained"/>; only the others
    /// (<see cref="GodClass.NoKnownCause"/>) are real dev-only candidates. Toggles <c>DebugSettings.godMode</c> and
    /// restores it, so run it after loading and reload the save before normal play.
    /// </summary>
    internal static GodClass ClassifyGod(CatalogEntry entry, out string causes)
    {
        causes = BuildCauses(entry);
        bool god;
        try
        {
            god = AppearsOnlyInGodMode(entry.Designator, out _);
        }
        catch (Exception)
        {
            god = false;
        }

        if (!god)
        {
            return GodClass.No;
        }

        return causes.Length == 0 ? GodClass.NoKnownCause : GodClass.Explained;
    }

    /// <summary>
    /// Round trip status of an entry's key: <c>ok</c>, <c>parse</c> (canonical string does not parse back to an equal
    /// key), <c>unresolved</c> (the parsed key finds no entry) or <c>other</c> (it resolves to a different designator).
    /// Resolves the parsed key, as a loaded save would.
    /// </summary>
    internal static string RoundTripStatus(CatalogEntry entry)
    {
        if (!PinKeyRoundTrip.TryRoundTrip(entry.Key, out PinKey? parsed))
        {
            return "parse";
        }

        CatalogEntry? resolved = DesignatorCatalog.Resolve(parsed);
        if (resolved == null)
        {
            return "unresolved";
        }

        return ReferenceEquals(resolved.Designator, entry.Designator) ? "ok" : "other";
    }

    /// <summary>
    /// Debug action. Probes god mode per entry, so run it after loading a save and reload the save before normal
    /// play. Dev-mode-only detection is a name hint only (not measured). The report is emitted in chunks of
    /// <see cref="ChunkSize"/> lines between BEGIN and END markers.
    /// </summary>
    [DebugAction("Quick Menu", "catalog dump", allowedGameStates = AllowedGameStates.Playing)]
    private static void Dump()
    {
        IReadOnlyList<CatalogEntry> entries = DesignatorCatalog.Entries;
        var duplicates = new HashSet<PinKey>(DesignatorCatalog.DuplicateKeys);
        var lines = new List<string>(entries.Count + 32);
        var devOnly = new List<string>();
        foreach (CatalogEntry e in entries)
        {
            string disabled = DisabledText(e.Designator, out string reason);
            GodClass god = ClassifyGod(e, out string causes);
            var sb = new StringBuilder();
            sb.Append(e.Id.ToString(CultureInfo.InvariantCulture)).Append(" key=").Append(e.Key.Value)
                .Append(" type=").Append(e.Designator.GetType().FullName)
                .Append(" category=").Append(e.CategoryLabel)
                .Append(" mod=").Append(e.PackageId)
                .Append(" visible=").Append(VisibleText(e.Designator))
                .Append(" disabled=").Append(disabled)
                .Append(" reason=").Append(reason)
                .Append(" causes=").Append(causes)
                .Append(" godOnly=").Append(god == GodClass.No ? "no" : god == GodClass.NoKnownCause ? "noKnownCause" : "explained")
                .Append(" rt=").Append(RoundTripStatus(e));
            if (duplicates.Contains(e.Key))
            {
                sb.Append(" dup=true");
            }

            lines.Add(sb.ToString());
            if (god == GodClass.NoKnownCause)
            {
                devOnly.Add(e.Key.Value + " [god-only, no known cause]");
            }
            else if (LooksDevOnly(e))
            {
                devOnly.Add(e.Key.Value + " [name hint]");
            }
        }

        foreach (DesignationCategoryDef category in DefDatabase<DesignationCategoryDef>.AllDefsListForReading)
        {
            if (DevNameHint.HasDevToken(category.defName))
            {
                devOnly.Add("category " + category.defName + " [name hint]");
            }
        }

        lines.Add("dev-only candidates=" + devOnly.Count.ToString(CultureInfo.InvariantCulture)
            + " (dev-mode-only detection is name-hint only, not measured)");
        foreach (string line in devOnly)
        {
            lines.Add("  " + line);
        }

        Emit(lines, entries.Count);
    }

    private static void Emit(List<string> lines, int entryCount)
    {
        int parts = (lines.Count + ChunkSize - 1) / ChunkSize;
        Verse.Log.Message(Log.Prefix + "BEGIN catalog dump entries=" + entryCount.ToString(CultureInfo.InvariantCulture)
            + " parts=" + parts.ToString(CultureInfo.InvariantCulture)
            + " (run after loading; reload the save before normal play)");
        for (int part = 0; part < parts; part++)
        {
            int from = part * ChunkSize;
            int count = Math.Min(ChunkSize, lines.Count - from);
            Verse.Log.Message(Log.Prefix + "dump part " + (part + 1).ToString(CultureInfo.InvariantCulture) + "/"
                + parts.ToString(CultureInfo.InvariantCulture) + "\n" + string.Join("\n", lines.GetRange(from, count)));
        }

        Verse.Log.Message(Log.Prefix + "END catalog dump");
    }

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
