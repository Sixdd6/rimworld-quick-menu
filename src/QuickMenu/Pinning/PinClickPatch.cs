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
/// and overrides do not agree on the parameter name. On a pin gesture the prefix returns <c>false</c> so the designator does not activate.
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

    /// <summary>
    /// Pins or unpins on modifier+left-click, otherwise observes. S-3 (ADR 0005): the Event is already used when this
    /// runs, so click versus hotkey comes from <c>UnityEngine.Input</c> and the modifiers from the event and from <c>Input</c>.
    /// Fast path (P-3): the effective modifier's two keys and the event flag are checked first, so a normal click costs two
    /// <c>Input</c> calls and allocates nothing. A nested call (an override that calls <c>base.ProcessInput</c>) cannot
    /// toggle twice: the deduper rejects a second call in the same frame, and a pin gesture returns false so the nested
    /// call is never reached.
    /// </summary>
    internal static bool Prefix(Designator __instance, Event __0)
    {
        if (Site.Disabled || MenuActivation.Depth > 0 || PinSuppress.Depth > 0)
        {
            return true;
        }

        using var scope = new MeasureScope(StatId.PinPrefix);
        try
        {
            PinModifier modifier = QuickMenuMod.EffectiveModifier;
            if (!TryBuildInput(__0, modifier, out PinClickInput input)
                || PinClickClassifier.Classify(input, modifier) != PinClickAction.Pin)
            {
                return true;
            }

            // Only architect designators pin: a command-bar designator (for example Designator_MechControlGroup) behaves normally.
            if (!IsArchitectDesignator(__instance))
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
    }

    /// <summary>
    /// True when <paramref name="designator"/> is in the catalog (O(1) lookup) or, for one added after the catalog was built,
    /// is listed by a <see cref="DesignationCategoryDef"/> (directly or as a dropdown element). Runs only after the
    /// modifier, click and classifier tests passed, so the allocation-free fast path is untouched.
    /// </summary>
    internal static bool IsArchitectDesignator(Designator designator)
    {
        DesignatorCatalog.RefreshIdeology();
        if (DesignatorCatalog.Find(designator) != null)
        {
            return true;
        }

        foreach (DesignationCategoryDef category in DefDatabase<DesignationCategoryDef>.AllDefsListForReading)
        {
            foreach (Designator listed in category.AllResolvedDesignators)
            {
                if (ReferenceEquals(listed, designator))
                {
                    return true;
                }

                if (listed is Designator_Dropdown dropdown && dropdown.Elements.Contains(designator))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Builds the classifier input for a left-button press or release with the effective modifier held. Returns false
    /// (no input built) as soon as the modifier is not held or the left button did not change. Only the effective
    /// modifier's raw keys are read; the classifier looks at no others. <paramref name="ev"/> may be null.
    /// </summary>
    internal static bool TryBuildInput(Event? ev, PinModifier modifier, out PinClickInput input)
    {
        bool rawHeld;
        bool eventHeld;
        switch (modifier)
        {
            case PinModifier.Ctrl:
                eventHeld = ev != null && (ev.control || ev.command);
                rawHeld = UnityEngine.Input.GetKey(KeyCode.LeftControl) || UnityEngine.Input.GetKey(KeyCode.RightControl);
                break;
            case PinModifier.Alt:
                eventHeld = ev != null && ev.alt;
                rawHeld = UnityEngine.Input.GetKey(KeyCode.LeftAlt) || UnityEngine.Input.GetKey(KeyCode.RightAlt);
                break;
            default:
                eventHeld = ev != null && ev.shift;
                rawHeld = UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift);
                break;
        }

        input = default;
        if (!eventHeld && !rawHeld)
        {
            return false;
        }

        bool down = UnityEngine.Input.GetMouseButtonDown(0);
        bool up = UnityEngine.Input.GetMouseButtonUp(0);
        if (!down && !up)
        {
            return false;
        }

        bool ctrl = modifier == PinModifier.Ctrl;
        bool alt = modifier == PinModifier.Alt;
        bool shift = !ctrl && !alt;
        input = new PinClickInput(
            IsKeyEvent: false,
            RawType: RawEventType.Other,
            Button: 0,
            EventCtrl: ctrl && eventHeld,
            EventAlt: alt && eventHeld,
            EventShift: shift && eventHeld,
            RawCtrl: ctrl && rawHeld,
            RawAlt: alt && rawHeld,
            RawShift: shift && rawHeld,
            LeftDownThisFrame: down,
            LeftUpThisFrame: up);
        return true;
    }

    /// <summary>
    /// Pins or unpins <paramref name="designator"/>. Never throws: an uncatalogued designator whose key cannot be built
    /// (or would be the shared dropdown key) is refused with a Keyed message.
    /// </summary>
    internal static void Toggle(Designator designator)
    {
        DesignatorCatalog.RefreshIdeology(); // O(1) when unchanged; an Ideology designator is findable right after a reform
        if (DesignatorCatalog.Find(designator) is { } entry)
        {
            PinService.Toggle(entry);
            return;
        }

        // Not in the catalog (for example a designator a mod adds after startup): describe it with the catalog's own builder.
        string label = designator.Label ?? string.Empty;
        try
        {
            DesignatorDescriptor descriptor = DesignatorCatalog.DescribeTree(designator, string.Empty);
            PinKey key = PinKeyFactory.Assign(new[] { descriptor })[0];
            if (designator is Designator_Dropdown && key.Kind == PinKeyKind.Order)
            {
                // Every childless dropdown would share one order key, so pinning one would pin them all.
                Refuse(label, "uncatalogued dropdown without a def group");
                return;
            }

            BuildableDef? placing = (designator as Designator_Build)?.PlacingDef;
            string? packageId = placing?.modContentPack?.PackageId;
            PinService.Toggle(key, label, string.IsNullOrEmpty(packageId) ? null : packageId);
        }
        catch (Exception ex)
        {
            Refuse(label, ex.GetType().Name + ": " + ex.Message);
        }
    }

    private static void Refuse(string label, string reason)
    {
        Diagnostics.Log.LogOnce("Pinning.Uncatalogued." + label, $"{label} was not pinned: {reason}");
        try
        {
            Messages.Message("QuickMenu.Pin.Unavailable".Translate(label), MessageTypeDefOf.RejectInput, historical: false);
        }
        catch (Exception ex)
        {
            Diagnostics.Log.LogOnce("Pinning.Refuse", $"Showing the pin refusal failed: {ex}");
        }
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
