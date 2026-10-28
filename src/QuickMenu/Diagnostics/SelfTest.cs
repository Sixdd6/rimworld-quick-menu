using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using LudeonTK;
using QuickMenu.Catalog;
using QuickMenu.Core.Diagnostics;
using QuickMenu.Core.Pins;
using QuickMenu.Menu;
using QuickMenu.Pinning;
using RimWorld;
using Verse;

namespace QuickMenu.Diagnostics;

/// <summary>
/// The "Quick Menu: self-test" debug action (spikes S-4 and S-5). Logs one bounded block between
/// <c>BEGIN</c> and <c>END</c> markers; the full per-entry list is only in the catalog dump.
/// </summary>
internal static class SelfTest
{
    private const int ExampleLimit = 20;

    internal static string BuildReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine(Log.Prefix + "BEGIN self-test");
        Section(sb, "catalog", Catalog);
        Section(sb, "patches", Patches);
        Section(sb, "keys", RoundTrip);
        Section(sb, "S-5 dev-only and reasons", DevOnlyAndReasons);
        Section(sb, "startup (P-18)", PerfStartup.Append);
        Section(sb, "perf counters", PerfCounters.AppendCounters);
        Section(sb, "textures (T-28)", Textures.AppendSelfTest);
        Section(sb, "mods (T-67)", b => b.AppendLine(Compat.ModDetection.Summary() + " effectiveModifier=" + QuickMenuMod.EffectiveModifier + " firstRunShown=" + QuickMenuMod.Settings.FirstRunNoticeShown + " mintShown=" + QuickMenuMod.Settings.MintNoticeShown));
        Section(sb, "settings writes (T-57)", b => b.AppendLine("saves=" + QuickMenuMod.SaveCount.ToString(CultureInfo.InvariantCulture)));
        sb.AppendLine(Log.Prefix + "END self-test");
        return sb.ToString();
    }

    // A failure in one section must not hide the others.
    private static void Section(StringBuilder sb, string name, Action<StringBuilder> body)
    {
        try
        {
            body(sb);
        }
        catch (Exception ex)
        {
            sb.AppendLine("section '" + name + "' threw: " + ex);
        }
    }

    private static void Catalog(StringBuilder sb)
    {
        IReadOnlyList<CatalogEntry> entries = DesignatorCatalog.Entries;
        var kinds = new int[Enum.GetValues<PinKeyKind>().Length];
        foreach (CatalogEntry e in entries)
        {
            kinds[(int)e.Key.Kind]++;
        }

        sb.AppendLine("loadedMods=" + LoadedModManager.RunningModsListForReading.Count.ToString(CultureInfo.InvariantCulture)
            + " entries=" + entries.Count.ToString(CultureInfo.InvariantCulture)
            + " topLevel=" + DesignatorCatalog.TopLevelCount.ToString(CultureInfo.InvariantCulture)
            + " resolvedDesignators=" + DesignatorCatalog.ResolvedDesignatorCount.ToString(CultureInfo.InvariantCulture));
        foreach (PinKeyKind kind in Enum.GetValues<PinKeyKind>())
        {
            sb.AppendLine("  kind " + kind + "=" + kinds[(int)kind].ToString(CultureInfo.InvariantCulture));
        }

        sb.AppendLine("buildMs=" + DesignatorCatalog.BuildTime.TotalMilliseconds.ToString("F2", CultureInfo.InvariantCulture)
            + " (budget 200) builds=" + DesignatorCatalog.BuildCount.ToString(CultureInfo.InvariantCulture)
            + " (P-12 expects 1) buildSiteDisabled=" + DesignatorCatalog.BuildSite.Disabled);
        DesignatorCatalog.RefreshIdeology();
        sb.AppendLine("ideology entries=" + DesignatorCatalog.IdeoEntryCount.ToString(CultureInfo.InvariantCulture)
            + " rebuilds=" + DesignatorCatalog.IdeoRebuildCount.ToString(CultureInfo.InvariantCulture)
            + " lastMs=" + DesignatorCatalog.IdeoBuildTime.TotalMilliseconds.ToString("F2", CultureInfo.InvariantCulture)
            + " duplicateKeys=" + DesignatorCatalog.IdeoDuplicateKeyCount.ToString(CultureInfo.InvariantCulture)
            + " siteDisabled=" + DesignatorCatalog.IdeoSite.Disabled);
        sb.AppendLine("complete=" + DesignatorCatalog.IsComplete + " skipped=" + DesignatorCatalog.SkippedCount.ToString(CultureInfo.InvariantCulture)
            + " duplicateKeys=" + DesignatorCatalog.DuplicateKeyCount.ToString(CultureInfo.InvariantCulture));
        var dupes = new BoundedLines(ExampleLimit);
        foreach (PinKey key in DesignatorCatalog.DuplicateKeys)
        {
            dupes.Add(key.Value);
        }

        Append(sb, "duplicate", dupes);
        CatalogHealth(sb);
    }

    private static void Patches(StringBuilder sb)
    {
        sb.AppendLine("patched=" + PinClickPatch.PatchedCount.ToString(CultureInfo.InvariantCulture)
            + " failed=" + PinClickPatch.FailedCount.ToString(CultureInfo.InvariantCulture)
            + " patchMs=" + PinClickPatch.PatchMilliseconds.ToString("F2", CultureInfo.InvariantCulture)
            + " siteDisabled=" + PinClickPatch.Site.Disabled);
        var failed = new BoundedLines(ExampleLimit);
        foreach (string line in PinClickPatch.Failed)
        {
            failed.Add(line);
        }

        Append(sb, "patch failed", failed);
        sb.AppendLine("marker patch (Command.GizmoOnGUIInt postfix): " + (PinMarkerPatch.Patched ? "patched" : "failed")
            + " siteDisabled=" + PinMarkerPatch.Site.Disabled
            + " textureSiteDisabled=" + Textures.PinMarkerSite.Disabled
            + " markedSetEmpty=" + PinnedDesignators.IsEmpty
            + " setVersion=" + PinnedDesignators.Version.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine("dropdown marker patches (Designator_Dropdown.ProcessInput, FloatMenuOption.DoGUI, FloatMenuGridOption.OnGUI postfixes): map="
            + (DropdownMarkerPatch.MapPatched ? "patched" : "failed")
            + " row=" + (DropdownMarkerPatch.RowPatched ? "patched" : "failed")
            + " grid=" + (DropdownMarkerPatch.GridPatched ? "patched" : "failed")
            + " mapSiteDisabled=" + DropdownMarkerPatch.Site.Disabled
            + " drawSiteDisabled=" + DropdownMarkerPatch.DrawSite.Disabled
            + " mappedOptions=" + DropdownMarkerPatch.MappedCount.ToString(CultureInfo.InvariantCulture));
        AppendOwnGizmoDrawers(sb);
    }

    /// <summary>
    /// ADR 0009 (amendment 2026-10-27): every designator subclass that declares its own <c>GizmoOnGUIInt</c> is patched too, so
    /// it gets a marker even when it never calls the base method. Lists the patched and failed overrides.
    /// </summary>
    private static void AppendOwnGizmoDrawers(StringBuilder sb)
    {
        var patched = new BoundedLines(ExampleLimit);
        foreach (string line in PinMarkerPatch.Overrides)
        {
            patched.Add(line);
        }

        var failed = new BoundedLines(ExampleLimit);
        foreach (string line in PinMarkerPatch.FailedOverrides)
        {
            failed.Add(line);
        }

        sb.AppendLine("marker override patches (GizmoOnGUIInt in designator subclasses): patched=" + patched.Total.ToString(CultureInfo.InvariantCulture)
            + " failed=" + failed.Total.ToString(CultureInfo.InvariantCulture));
        Append(sb, "marker override", patched);
        Append(sb, "marker override failed", failed);
    }

    /// <summary>
    /// Catalog health in a running game. Stale: a static top-level entry whose designator is no longer listed by any category
    /// (something rebuilt a category's designators after the catalog was built, so the menu would use a detached copy).
    /// Icon-less: entries that would draw the game's bad-texture cross without <see cref="Compat.ForeignIcons"/>.
    /// </summary>
    private static void CatalogHealth(StringBuilder sb)
    {
        var live = new HashSet<Designator>(ReferenceEqualityComparer.Instance);
        foreach (DesignationCategoryDef category in DefDatabase<DesignationCategoryDef>.AllDefsListForReading)
        {
            foreach (Designator d in category.AllResolvedDesignators)
            {
                live.Add(d);
            }
        }

        IReadOnlyList<CatalogEntry> entries = DesignatorCatalog.Entries;
        int staticCount = entries.Count - DesignatorCatalog.IdeoEntryCount;
        var stale = new BoundedLines(ExampleLimit);
        var iconless = new BoundedLines(ExampleLimit);
        for (int i = 0; i < entries.Count; i++)
        {
            CatalogEntry e = entries[i];
            if (i < staticCount && e.ParentId < 0 && !live.Contains(e.Designator))
            {
                stale.Add("#" + e.Id + " " + e.Designator.GetType().FullName + " " + e.Key.Value);
            }

            if (Compat.ForeignIcons.LacksIcon(e.Designator))
            {
                iconless.Add("#" + e.Id + " " + e.Designator.GetType().FullName + " " + e.Key.Value);
            }
        }

        sb.AppendLine("catalog health: stale=" + stale.Total.ToString(CultureInfo.InvariantCulture)
            + " iconless=" + iconless.Total.ToString(CultureInfo.InvariantCulture));
        Append(sb, "stale", stale);
        Append(sb, "iconless", iconless);
    }

    private static void RoundTrip(StringBuilder sb)
    {
        var failures = new BoundedLines(ExampleLimit);
        foreach (CatalogEntry e in DesignatorCatalog.Entries)
        {
            string status = CatalogProbe.RoundTripStatus(e);
            if (status != "ok")
            {
                failures.Add("#" + e.Id + " rt=" + status + ": " + e.Key.Value);
            }
        }

        int total = DesignatorCatalog.Entries.Count;
        double rate = total == 0 ? 0 : 100.0 * (total - failures.Total) / total;
        sb.AppendLine("roundTrip entries=" + total.ToString(CultureInfo.InvariantCulture)
            + " failures=" + failures.Total.ToString(CultureInfo.InvariantCulture)
            + " rate=" + rate.ToString("F2", CultureInfo.InvariantCulture) + "%");
        Append(sb, "roundTrip failure", failures);
    }

    /// <summary>
    /// Probes god mode per entry, so run it after loading and reload the save before normal play. Dev-mode-only
    /// detection is a name hint only (not measured).
    /// </summary>
    private static void DevOnlyAndReasons(StringBuilder sb)
    {
        int invisible = 0, disabled = 0, withReason = 0, threw = 0, hinted = 0, explained = 0;
        var godOnly = new BoundedLines(ExampleLimit);
        var hints = new BoundedLines(ExampleLimit);
        var causes = new Dictionary<string, int>();
        var godCauses = new Dictionary<string, int>();
        foreach (CatalogEntry e in DesignatorCatalog.Entries)
        {
            if (CatalogProbe.VisibleText(e.Designator) == "false")
            {
                invisible++;
            }

            string d = CatalogProbe.DisabledText(e.Designator, out string reason);
            if (d == "true")
            {
                disabled++;
            }
            else if (d.StartsWith("threw", StringComparison.Ordinal))
            {
                threw++;
            }

            if (reason.Length > 0)
            {
                withReason++;
            }

            CatalogProbe.GodClass god = CatalogProbe.ClassifyGod(e, out string cause);
            foreach (string part in cause.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                string name = part.Split('=')[0];
                causes[name] = causes.GetValueOrDefault(name) + 1;
                if (god == CatalogProbe.GodClass.Explained)
                {
                    godCauses[name] = godCauses.GetValueOrDefault(name) + 1;
                }
            }

            if (god == CatalogProbe.GodClass.NoKnownCause)
            {
                godOnly.Add(e.Key.Value);
            }
            else if (god == CatalogProbe.GodClass.Explained)
            {
                explained++;
            }
            else if (CatalogProbe.LooksDevOnly(e))
            {
                hinted++;
                hints.Add(e.Key.Value);
            }
        }

        sb.AppendLine("visible=false:" + invisible + " disabled=true:" + disabled + " disabledReasonSet:" + withReason + " threw:" + threw);
        sb.AppendLine("Designator.disabledReason is the only per-designator reason text; visibility has no text, so build reasons must be composed from:");
        foreach (KeyValuePair<string, int> kv in causes)
        {
            sb.AppendLine("  cause " + kv.Key + "=" + kv.Value.ToString(CultureInfo.InvariantCulture));
        }

        sb.AppendLine("god-only, explained by build causes (not dev-only)=" + explained.ToString(CultureInfo.InvariantCulture));
        foreach (KeyValuePair<string, int> kv in godCauses)
        {
            sb.AppendLine("  god-only explained by " + kv.Key + "=" + kv.Value.ToString(CultureInfo.InvariantCulture));
        }

        sb.AppendLine("god-only, no known cause (dev-only candidates)=" + godOnly.Total.ToString(CultureInfo.InvariantCulture));
        Append(sb, "god-only no known cause", godOnly);
        sb.AppendLine("name-hint dev-only (not god-mode gated)=" + hinted.ToString(CultureInfo.InvariantCulture)
            + "; dev-mode-only detection is name-hint only (not measured)");
        Append(sb, "name hint", hints);
        sb.AppendLine("run after loading; reload the save before normal play (the god-mode probe toggles DebugSettings.godMode)");
        sb.AppendLine("full list in catalog dump: debug action 'Quick Menu: catalog dump' (rt= and dup= per line)");
    }

    private static void Append(StringBuilder sb, string label, BoundedLines lines)
    {
        foreach (string line in lines.Kept)
        {
            sb.AppendLine("  " + label + ": " + line);
        }

        if (lines.Omitted > 0)
        {
            sb.AppendLine("  ... " + lines.Omitted.ToString(CultureInfo.InvariantCulture) + " more " + label + " lines omitted (full list in catalog dump)");
        }
    }

    [DebugAction("Quick Menu", "self-test", allowedGameStates = AllowedGameStates.Playing)]
    private static void Run() => Verse.Log.Message(BuildReport());
}
