using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using QuickMenu.Core.Pins;
using QuickMenu.Core.Render;
using QuickMenu.Diagnostics;
using QuickMenu.Menu;
using UnityEngine;
using Verse;
using FaultPoint = QuickMenu.Core.Diagnostics.FaultPoint;
using Log = QuickMenu.Diagnostics.Log;

namespace QuickMenu.Pinning;

/// <summary>
/// Fourth patch site (ADR 0009, FR-19): a postfix on the protected <c>Command.GizmoOnGUIInt(Rect, GizmoRenderParms)</c>, the one
/// method every architect button of vanilla, Better Architect Menu and Mint's grid view passes through with its exact rect.
/// It draws the pin marker on a pinned designator, only on Repaint and only while an architect menu is drawing
/// (<see cref="ArchitectDrawContext"/>), never on the command bar of a selected thing. The fast path for the many other
/// commands is a few comparisons and allocates nothing. The set is never rebuilt on the per-gizmo path; once per frame the
/// pin service re-validates itself (an allocation-free check, O(ideoligion count)), which rebuilds the set only after a game load or an Ideology change.
/// Some mods replace the whole button drawing in a <c>Designator</c> subclass without calling the base method (More Planning's
/// tools), so every declared override of <c>GizmoOnGUIInt</c> in a designator subclass gets the same postfix (ADR 0009, amendment
/// 2026-10-27). The marker is drawn only by the postfix of the instance type's most-derived override, so a button whose override
/// calls the base method is marked once, on top of everything that override draws.
/// </summary>
internal static class PinMarkerPatch
{
    internal static readonly PatchSite Site = new("PinMarkerPatch.GizmoOnGUIInt");

    private const string MethodName = "GizmoOnGUIInt";
    private static readonly Type[] _signature = { typeof(Rect), typeof(GizmoRenderParms) };
    private static readonly Dictionary<Type, Type> _drawingType = new();
    private static readonly List<string> _overrides = new();
    private static readonly List<string> _failedOverrides = new();
    private static int _lastFrame = -1;

    /// <summary>True when the postfix was applied.</summary>
    internal static bool Patched { get; private set; }

    /// <summary>Designator subclasses whose own <c>GizmoOnGUIInt</c> override was patched too (for the self-test).</summary>
    internal static IReadOnlyList<string> Overrides => _overrides;

    /// <summary>Overrides that could not be patched; each is left unmarked, nothing else is affected.</summary>
    internal static IReadOnlyList<string> FailedOverrides => _failedOverrides;

    internal static void Apply(Harmony harmony)
    {
        HarmonyMethod postfix;
        try
        {
            var original = AccessTools.Method(typeof(Command), MethodName, _signature);
            if (original == null)
            {
                Site.Fail(new MissingMethodException(typeof(Command).FullName, MethodName));
                return;
            }

            postfix = new HarmonyMethod(typeof(PinMarkerPatch), nameof(Postfix));
            harmony.Patch(original, postfix: postfix);
            Patched = true;
        }
        catch (Exception ex)
        {
            Site.Fail(ex);
            return;
        }

        foreach (MethodInfo method in EnumerateOverrides())
        {
            string label = method.DeclaringType!.FullName + " [" + method.DeclaringType.Assembly.GetName().Name + "]";
            try
            {
                harmony.Patch(method, postfix: postfix);
                _overrides.Add(label);
            }
            catch (Exception ex)
            {
                _failedOverrides.Add(label + " :: " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        if (_failedOverrides.Count > 0)
        {
            Log.LogOnce("PinMarkerPatch.Overrides", $"{_failedOverrides.Count} GizmoOnGUIInt override(s) could not be patched, those buttons show no pin marker; first: {_failedOverrides[0]}");
        }
    }

    /// <summary>Declared, non-abstract <c>GizmoOnGUIInt(Rect, GizmoRenderParms)</c> overrides in designator subclasses; one bad type is skipped.</summary>
    private static IEnumerable<MethodInfo> EnumerateOverrides()
    {
        List<Type> types;
        try
        {
            types = GenTypes.AllSubclasses(typeof(Designator));
        }
        catch (Exception ex)
        {
            _failedOverrides.Add("GenTypes.AllSubclasses :: " + ex.GetType().Name + ": " + ex.Message);
            yield break;
        }

        foreach (Type type in types)
        {
            MethodInfo? method = null;
            try
            {
                if (!type.ContainsGenericParameters)
                {
                    method = type.GetMethod(MethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, null, _signature, null);
                }
            }
            catch (Exception ex)
            {
                _failedOverrides.Add(type.FullName + " :: scan " + ex.GetType().Name + ": " + ex.Message);
            }

            if (method != null && !method.IsAbstract)
            {
                yield return method;
            }
        }
    }

    /// <summary>
    /// True when <paramref name="original"/> is the most-derived <c>GizmoOnGUIInt</c> of the instance's type, so exactly one
    /// postfix per button draws. Looked up once per type, then allocation-free.
    /// </summary>
    private static bool IsOutermost(Command command, MethodBase? original)
    {
        Type type = command.GetType();
        if (!_drawingType.TryGetValue(type, out Type? owner))
        {
            owner = AccessTools.Method(type, MethodName, _signature)?.DeclaringType ?? typeof(Command);
            _drawingType[type] = owner;
        }

        return original == null || original.DeclaringType == owner;
    }

    /// <summary>
    /// Parameters are bound by index (<c>__0</c> is the button rect): the game's own name for it is not part of our contract.
    /// The measured scope covers only the decision (what P-19 budgets); drawing a marker is IMGUI texture work in the game's
    /// backend and is counted separately under <see cref="StatId.PinMarkerDraw"/>, so it never pollutes the fast-path numbers.
    /// </summary>
    internal static void Postfix(Command __instance, Rect __0, MethodBase __originalMethod)
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
                if (!Decide(__instance, out kind, out sizing) || !IsOutermost(__instance, __originalMethod))
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
