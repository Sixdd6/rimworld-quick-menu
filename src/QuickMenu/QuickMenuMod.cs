using System;
using HarmonyLib;
using QuickMenu.Diagnostics;
using QuickMenu.Input;
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
        try
        {
            var harmony = new Harmony(HarmonyId);
            harmony.Patch(
                AccessTools.Method(typeof(UIRoot_Play), nameof(UIRoot_Play.UIRootUpdate)),
                postfix: new HarmonyMethod(typeof(TriggerPoller), nameof(TriggerPoller.Postfix)));
        }
        catch (Exception ex)
        {
            Diagnostics.Log.LogOnce("Harmony.Bootstrap", $"Applying Harmony patches failed: {ex}");
        }
    }
}
