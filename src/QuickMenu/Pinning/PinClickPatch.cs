using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using QuickMenu.Catalog;
using QuickMenu.Core.Input;
using QuickMenu.Core.Pins;
using QuickMenu.Diagnostics;
using QuickMenu.Menu;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuickMenu.Pinning;

/// <summary>
/// Shared Harmony prefix on every declared, non-abstract <c>ProcessInput(Event)</c> in <see cref="Designator"/> and its
/// subclasses (ADR 0005). The event is bound by index (<c>Event __0</c>) because Harmony binds named parameters by name
/// and overrides do not agree on the parameter name. In this build (spike S-1/S-3) the prefix only observes and always returns <c>true</c>.
/// </summary>
internal static class PinClickPatch
{
    internal static readonly PatchSite Site = new("PinClickPatch.ProcessInput");

    private static readonly List<string> _patched = new();
    private static readonly List<string> _failed = new();

    internal static int PatchedCount => _patched.Count;

    internal static int FailedCount => _failed.Count;

    internal static IReadOnlyList<string> Patched => _patched;

    internal static IReadOnlyList<string> Failed => _failed;

    internal static double PatchMilliseconds { get; private set; }

    /// <summary>Patches every matching override. Each method has its own try/catch; failures are recorded, not thrown.</summary>
    internal static void Apply(Harmony harmony)
    {
        var started = Stopwatch.GetTimestamp();
        var prefix = new HarmonyMethod(typeof(PinClickPatch), nameof(Prefix));
        foreach (var method in EnumerateTargets())
        {
            var label = Describe(method);
            try
            {
                harmony.Patch(method, prefix: prefix);
                _patched.Add(label);
            }
            catch (Exception ex)
            {
                _failed.Add(label + " :: " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        PatchMilliseconds = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        if (_failed.Count > 0)
        {
            Diagnostics.Log.LogOnce("PinClickPatch.Failures", $"{_failed.Count} ProcessInput override(s) could not be patched; first: {_failed[0]}");
        }
    }

    /// <summary>Frame of the last raw left-button press, recorded by the per-frame poller for the deduper.</summary>
    internal static int LastLeftDownFrame { get; set; } = PinClickDeduper.NoPress;

    private static readonly PinClickDeduper _deduper = new();
    private static bool _busy;

    /// <summary>
    /// Pins or unpins on modifier+left-click, otherwise observes. S-3 (ADR 0005): the Event is already used when this
    /// runs, so click versus hotkey comes from <c>UnityEngine.Input</c> and the modifiers from the event and from <c>Input</c>.
    /// The fast path (no mouse button change this frame) allocates nothing. <c>_busy</c> stops a nested call (an override that
    /// calls <c>base.ProcessInput</c>) from toggling twice.
    /// </summary>
    internal static bool Prefix(Designator __instance, Event __0)
    {
        if (Site.Disabled || MenuActivation.Depth > 0 || _busy)
        {
            return true;
        }

        bool down = UnityEngine.Input.GetMouseButtonDown(0);
        bool up = UnityEngine.Input.GetMouseButtonUp(0);
        if (!down && !up)
        {
            return true;
        }

        _busy = true;
        try
        {
            Event? ev = __0;
            var input = new PinClickInput(
                IsKeyEvent: false,
                RawType: RawEventType.Other,
                Button: 0,
                EventCtrl: ev != null && (ev.control || ev.command),
                EventAlt: ev != null && ev.alt,
                EventShift: ev != null && ev.shift,
                RawCtrl: UnityEngine.Input.GetKey(KeyCode.LeftControl) || UnityEngine.Input.GetKey(KeyCode.RightControl),
                RawAlt: UnityEngine.Input.GetKey(KeyCode.LeftAlt) || UnityEngine.Input.GetKey(KeyCode.RightAlt),
                RawShift: UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift),
                LeftDownThisFrame: down,
                LeftUpThisFrame: up);
            if (PinClickClassifier.Classify(input, QuickMenuMod.EffectiveModifier) != PinClickAction.Pin)
            {
                return true;
            }

            // A pin gesture never activates the designator, including the duplicate (suppressed) frame of the same click.
            if (_deduper.Accept(Time.frameCount, LastLeftDownFrame, input))
            {
                Toggle(__instance);
            }

            return false;
        }
        catch (Exception ex)
        {
            Site.Fail(ex);
            return true;
        }
        finally
        {
            _busy = false;
        }
    }

    private static void Toggle(Designator designator)
    {
        if (DesignatorCatalog.Find(designator) is { } entry)
        {
            PinService.Toggle(entry);
            return;
        }

        // Not in the catalog (for example a designator a mod adds after startup): key it on its own.
        Type type = designator.GetType();
        BuildableDef? placing = (designator as Designator_Build)?.PlacingDef;
        DescriptorKind kind = placing switch
        {
            TerrainDef => DescriptorKind.BuildTerrain,
            ThingDef => DescriptorKind.BuildThing,
            _ => DescriptorKind.Other,
        };
        var descriptor = new DesignatorDescriptor(kind, type.FullName ?? type.Name, placing?.defName, string.Empty);
        PinKey key = PinKeyFactory.Assign(new[] { descriptor })[0];
        string? packageId = placing?.modContentPack?.PackageId;
        PinService.Toggle(key, designator.Label ?? string.Empty, string.IsNullOrEmpty(packageId) ? null : packageId);
    }

    /// <summary>
    /// <see cref="Designator"/> plus every subclass in every loaded assembly (ADR 0005). Abstract subclasses are included
    /// because they can declare a concrete override. Each type is probed in its own try/catch so one bad type
    /// (for example a mod type with an unloadable signature) cannot abort <see cref="Apply"/>.
    /// </summary>
    private static IEnumerable<MethodInfo> EnumerateTargets()
    {
        var types = new List<Type> { typeof(Designator) };
        try
        {
            types.AddRange(GenTypes.AllSubclasses(typeof(Designator)));
        }
        catch (Exception ex)
        {
            _failed.Add("GenTypes.AllSubclasses :: " + ex.GetType().Name + ": " + ex.Message);
        }

        foreach (var type in types)
        {
            MethodInfo? method = null;
            try
            {
                if (!type.ContainsGenericParameters)
                {
                    method = type.GetMethod(
                        "ProcessInput",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                        null,
                        new[] { typeof(Event) },
                        null);
                }
            }
            catch (Exception ex)
            {
                _failed.Add(type.FullName + " :: scan " + ex.GetType().Name + ": " + ex.Message);
            }

            if (method != null && !method.IsAbstract)
            {
                yield return method;
            }
        }
    }

    private static string Describe(MethodInfo method) =>
        method.DeclaringType!.FullName + " [" + method.DeclaringType.Assembly.GetName().Name + "]";
}
