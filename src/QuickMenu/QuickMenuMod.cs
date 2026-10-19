using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using HarmonyLib;
using QuickMenu.Diagnostics;
using QuickMenu.Menu;
using QuickMenu.Pinning;
using QuickMenu.Trigger;
using RimWorld;
using Verse;

namespace QuickMenu;

/// <summary>Mod entry point: applies the Harmony patches. A failure is logged once and never stops loading.</summary>
public sealed class QuickMenuMod : Mod
{
    internal const string HarmonyId = "sixdd.QuickMenu";

    public QuickMenuMod(ModContentPack content)
        : base(content)
    {
        long started = Stopwatch.GetTimestamp();
        try
        {
            ApplyPatches();
        }
        catch (Exception ex)
        {
            Diagnostics.Log.LogOnce("Harmony.Bootstrap", $"Harmony is unavailable, Quick Menu patches were not applied: {ex}");
        }

        // Settings load and Mint/BAM/MP detection are not implemented yet; the ctor time is currently all patching (P-18).
        long end = Stopwatch.GetTimestamp();
        PerfCounters.NoteStartup(end - started, end - started);
    }

    // Isolated and not inlined so a missing 0Harmony assembly fails here, inside the caller's try block.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ApplyPatches()
    {
        TriggerPoller.OpenRequested = MenuController.Open;
        var harmony = new Harmony(HarmonyId);
        ApplyPostfix(harmony, TriggerPoller.Site, typeof(UIRoot_Play), nameof(UIRoot_Play.UIRootUpdate), typeof(TriggerPoller), nameof(TriggerPoller.Postfix));
        PatchGuard.Run(PinClickPatch.Site, () => PinClickPatch.Apply(harmony));
    }

    private static void ApplyPostfix(Harmony harmony, Diagnostics.PatchSite site, Type targetType, string targetMethod, Type patchType, string postfix)
    {
        try
        {
            var original = AccessTools.Method(targetType, targetMethod);
            if (original == null)
            {
                site.Fail(new MissingMethodException(targetType.FullName, targetMethod));
                return;
            }

            harmony.Patch(original, postfix: new HarmonyMethod(patchType, postfix));
        }
        catch (Exception ex)
        {
            site.Fail(ex);
        }
    }
}
