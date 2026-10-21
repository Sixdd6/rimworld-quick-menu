using System;
using HarmonyLib;
using QuickMenu.Core.Pins;
using QuickMenu.Core.Render;
using QuickMenu.Diagnostics;
using QuickMenu.Menu;
using UnityEngine;
using Verse;
using FaultPoint = QuickMenu.Core.Diagnostics.FaultPoint;

namespace QuickMenu.Pinning;

/// <summary>
/// Fourth patch site (ADR 0009, FR-19): a postfix on the protected <c>Command.GizmoOnGUIInt(Rect, GizmoRenderParms)</c>, the one
/// method every architect button of vanilla, Better Architect Menu and Mint's grid view passes through with its exact rect.
/// It draws the pin marker on a pinned designator, only on Repaint and only while an architect menu is drawing
/// (<see cref="ArchitectDrawContext"/>), never on the command bar of a selected thing. The fast path for the many other
/// commands is a few comparisons and allocates nothing. The set is never rebuilt on the per-gizmo path; once per frame the
/// pin service re-validates itself (an allocation-free check, O(ideoligion count)), which rebuilds the set only after a game load or an Ideology change.
/// </summary>
internal static class PinMarkerPatch
{
    internal static readonly PatchSite Site = new("PinMarkerPatch.GizmoOnGUIInt");

    private static int _lastFrame = -1;

    /// <summary>True when the postfix was applied.</summary>
    internal static bool Patched { get; private set; }

    internal static void Apply(Harmony harmony)
    {
        try
        {
            var original = AccessTools.Method(typeof(Command), "GizmoOnGUIInt", new[] { typeof(Rect), typeof(GizmoRenderParms) });
            if (original == null)
            {
                Site.Fail(new MissingMethodException(typeof(Command).FullName, "GizmoOnGUIInt"));
                return;
            }

            harmony.Patch(original, postfix: new HarmonyMethod(typeof(PinMarkerPatch), nameof(Postfix)));
            Patched = true;
        }
        catch (Exception ex)
        {
            Site.Fail(ex);
        }
    }

    /// <summary>
    /// Parameters are bound by index (<c>__0</c> is the button rect): the game's own name for it is not part of our contract.
    /// The measured scope covers only the decision (what P-19 budgets); drawing a marker is IMGUI texture work in the game's
    /// backend and is counted separately under <see cref="StatId.PinMarkerDraw"/>, so it never pollutes the fast-path numbers.
    /// </summary>
    internal static void Postfix(Command __instance, Rect __0)
    {
        if (Site.Disabled)
        {
            return;
        }

        try
        {
            PinMarkerKind kind;
            PinMarkerSizing sizing;
            using (new MeasureScope(StatId.PinMarker))
            {
                if (!Decide(__instance, out kind, out sizing))
                {
                    return;
                }
            }

            using var drawScope = new MeasureScope(StatId.PinMarkerDraw);
            Textures.DrawPinMarker(sizing.Button(__0.x, __0.y, __0.width, __0.height, kind), kind);
        }
        catch (Exception ex)
        {
            Site.Fail(ex);
        }
    }

    /// <summary>
    /// The per-call decision. Cheapest exits first: a non-designator, then any event but Repaint (Layout and input calls draw
    /// nothing), then the once-per-frame re-validation, then the empty set, then the lookup. Allocates nothing.
    /// </summary>
    private static bool Decide(Command command, out PinMarkerKind kind, out PinMarkerSizing sizing)
    {
        kind = PinMarkerKind.Full;
        sizing = default;
        FaultInjection.Check(FaultPoint.PinMarker);
        if (command is not Designator designator || Event.current is not { type: EventType.Repaint })
        {
            return false;
        }

        int frame = Time.frameCount;
        if (frame != _lastFrame)
        {
            _lastFrame = frame;
            if (PinService.Board.Entries.Count != 0)
            {
                PinService.Revalidate(); // before IsEmpty: Ideology-only pins leave the set empty until re-resolved
            }
        }

        if (PinnedDesignators.IsEmpty)
        {
            return false;
        }

        PinMark mark = PinnedDesignators.Lookup(designator);
        if (mark == PinMark.None || !ArchitectDrawContext.IsActive() || !Textures.EnsurePinMarkers() || Textures.BuiltPinMarkerSizing is not { } built)
        {
            return false;
        }

        kind = mark == PinMark.Partial ? PinMarkerKind.Partial : PinMarkerKind.Full;
        sizing = built;
        return true;
    }
}
