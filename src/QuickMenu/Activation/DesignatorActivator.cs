using System;
using QuickMenu.Menu;
using RimWorld;
using UnityEngine;
using Verse;
using Log = QuickMenu.Diagnostics.Log;

namespace QuickMenu.Activation;

/// <summary>
/// Activates a pinned designator exactly as the architect would (ADR 0006): <c>ProcessInput(Event.current)</c>, wrapped in
/// <see cref="MenuActivation"/> so the pin prefix ignores the held modifier. Stuff builds and dropdowns use the vanilla
/// menus until T-41 and T-43. A fault in a foreign designator is logged once per type and never disables the menu.
/// </summary>
internal static class DesignatorActivator
{
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

    /// <summary>Shows why a slot cannot be used. <c>RejectInput</c> plays the reject sound itself.</summary>
    internal static void Reject(string reason) => Messages.Message(reason, MessageTypeDefOf.RejectInput, historical: false);
}
