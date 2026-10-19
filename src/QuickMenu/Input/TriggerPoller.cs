using System;
using QuickMenu.Diagnostics;
using QuickMenu.Diagnostics.Spike;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace QuickMenu.Input;

/// <summary>
/// Harmony postfix on <c>UIRoot_Play.UIRootUpdate</c> (ADR 0003). Closed-state cost is one flag check and one
/// <c>Input</c> call, with no allocation (P-1, P-2).
/// </summary>
internal static class TriggerPoller
{
    internal const string Site = "TriggerPoller.UIRootUpdate";

    private static readonly Action _pollAction = Poll;

    /// <summary>Called once when the trigger fires in a valid context. T-7 sets this to <c>MenuController.Open</c>.</summary>
    internal static Action? OpenRequested { get; set; }

    internal static void Postfix()
    {
        if (!UnityEngine.Input.GetMouseButtonDown(SkeletonTrigger.MouseIndex))
        {
            return;
        }

        PatchGuard.Run(Site, _pollAction);
    }

    private static void Poll()
    {
        if (!CanOpen())
        {
            return;
        }

        OpenRequested?.Invoke();
    }

    private static bool CanOpen()
    {
        if (Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null || WorldRendererUtility.WorldRendered)
        {
            return false;
        }

        var windows = Find.WindowStack.Windows;
        for (var i = 0; i < windows.Count; i++)
        {
            if (windows[i].absorbInputAroundWindow)
            {
                return false;
            }
        }

        return true;
    }
}
