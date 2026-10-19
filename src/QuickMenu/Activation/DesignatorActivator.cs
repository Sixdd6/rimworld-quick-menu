using System;
using System.Reflection;
using HarmonyLib;
using QuickMenu.Menu;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Log = QuickMenu.Diagnostics.Log;

namespace QuickMenu.Activation;

/// <summary>
/// Activates a pinned designator exactly as the architect would (ADR 0006): <c>ProcessInput(Event.current)</c>, wrapped in
/// <see cref="MenuActivation"/> so the pin prefix ignores the held modifier. Stuff builds take their material from the
/// ring (<see cref="Activate(Designator, ThingDef?, Designator_Dropdown?)"/>). A fault in a foreign designator is logged once per type and never disables the menu.
/// </summary>
internal static class DesignatorActivator
{
    private static MethodInfo? _checkCanInteract;
    private static bool _checkCanInteractResolved;

    /// <summary>The tutor gate of <c>Designator.ProcessInput</c> (protected in the game). True when it cannot be reached (logged once).</summary>
    private static bool CanInteract(Designator designator)
    {
        if (!_checkCanInteractResolved)
        {
            _checkCanInteract = AccessTools.Method(typeof(Designator), "CheckCanInteract", Type.EmptyTypes);
            _checkCanInteractResolved = true;
        }

        if (_checkCanInteract == null)
        {
            Log.LogOnce("Activation.CheckCanInteract", "Designator.CheckCanInteract was not found; the tutor gate is skipped when choosing a material.");
            return true;
        }

        return (bool)_checkCanInteract.Invoke(designator, null)!;
    }

    /// <summary>Calls <c>ProcessInput</c>. Returns false when the designator threw.</summary>
    internal static bool Activate(Designator designator, Event ev)
    {
        MenuActivation.Enter();
        try
        {
            designator.ProcessInput(ev);
            return true;
        }
        catch (Exception ex)
        {
            Type type = designator.GetType();
            Log.LogOnce($"Menu.Activation.{type.FullName}", $"Designator {type.FullName} threw during activation: {ex}");
            return false;
        }
        finally
        {
            MenuActivation.Exit();
        }
    }

    /// <summary>
    /// Selects <paramref name="designator"/> exactly as the architect does after the player picks a material (ADR 0006):
    /// <c>SetStuffDef</c>, the interaction check, the activate sound, then <c>DesignatorManager.Select</c>. No <c>Designate*</c>
    /// call is made here (FR-18). <paramref name="stuff"/> is ignored (plain <c>ProcessInput</c>) for anything that is not a
    /// stuff build. A dropdown element passes its <paramref name="dropdown"/>, which then shows it as its last used element.
    /// Ideology style variants keep the game's own style because only the stuff is set. Returns false when it threw.
    /// </summary>
    internal static bool Activate(Designator designator, ThingDef? stuff, Designator_Dropdown? dropdown = null)
    {
        MenuActivation.Enter();
        try
        {
            if (stuff != null && designator is Designator_Build build && build.PlacingDef is ThingDef def && def.MadeFromStuff)
            {
                build.SetStuffDef(stuff);
                if (CanInteract(build))
                {
                    build.CurActivateSound?.PlayOneShotOnCamera(null);
                    Find.DesignatorManager.Select(build);
                    dropdown?.SetActiveDesignator(designator, true);
                }
            }
            else
            {
                designator.ProcessInput(Event.current);
                dropdown?.SetActiveDesignator(designator, true);
            }

            return true;
        }
        catch (Exception ex)
        {
            Type type = designator.GetType();
            Log.LogOnce($"Menu.Activation.{type.FullName}", $"Designator {type.FullName} threw during activation: {ex}");
            return false;
        }
        finally
        {
            MenuActivation.Exit();
        }
    }

    /// <summary>Shows why a slot cannot be used. <c>RejectInput</c> plays the reject sound itself.</summary>
    internal static void Reject(string reason) => Messages.Message(reason, MessageTypeDefOf.RejectInput, historical: false);
}
