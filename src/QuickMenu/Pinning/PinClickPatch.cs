using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using QuickMenu.Diagnostics;
using QuickMenu.Diagnostics.Spike;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuickMenu.Pinning;

/// <summary>
/// Shared Harmony prefix on every declared, non-abstract <c>ProcessInput(Event)</c> in <see cref="Designator"/> and its
/// subclasses (ADR 0005). In this build (spike S-1/S-3) the prefix only observes and always returns <c>true</c>.
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

    /// <summary>Observe-only prefix. Invoked on clicks and hotkeys only; one flag check when the probe is off.</summary>
    internal static bool Prefix(Designator __instance, Event ev)
    {
        if (Site.Disabled || !ProcessInputProbe.Enabled)
        {
            return true;
        }

        try
        {
            ProcessInputProbe.Record(__instance, ev);
        }
        catch (Exception ex)
        {
            Site.Fail(ex);
        }

        return true;
    }

    private static IEnumerable<MethodInfo> EnumerateTargets()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = Array.FindAll(ex.Types, t => t != null)!;
            }
            catch (Exception)
            {
                continue;
            }

            foreach (var type in types)
            {
                if (type.IsAbstract && type.IsSealed)
                {
                    continue;
                }

                if (type != typeof(Designator) && !type.IsSubclassOf(typeof(Designator)))
                {
                    continue;
                }

                if (type.ContainsGenericParameters)
                {
                    continue;
                }

                MethodInfo? method;
                try
                {
                    method = type.GetMethod(
                        "ProcessInput",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                        null,
                        new[] { typeof(Event) },
                        null);
                }
                catch (Exception)
                {
                    continue;
                }

                if (method != null && !method.IsAbstract)
                {
                    yield return method;
                }
            }
        }
    }

    private static string Describe(MethodInfo method) =>
        method.DeclaringType!.FullName + " [" + method.DeclaringType.Assembly.GetName().Name + "]";
}
