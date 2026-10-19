using System;
using HarmonyLib;
using QuickMenu.Core.Input;
using QuickMenu.Diagnostics;
using QuickMenu.Menu;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuickMenu.Pinning;

/// <summary>
/// Third patch site (ADR 0005, spike S-3): postfix on the private <c>Designator_Dropdown.GetDesignatorSelectAction(Event, Designator)</c>.
/// Entry clicks in an open dropdown float menu never reach the entry's own <c>ProcessInput</c>; they reach the dropdown's, so the
/// pin prefix would pin the whole dropdown. The postfix replaces the entry's action with a wrapper: on a modifier+click it toggles
/// the entry's pin and does NOT run the original action, so the dropdown's <c>ProcessInput</c> prefix is never reached and only the
/// entry toggles. Without the modifier the original action runs unchanged. The wrapper allocates when the float menu is built,
/// not per frame.
/// </summary>
internal static class DropdownEntryPatch
{
    internal static readonly PatchSite Site = new("DropdownEntryPatch.GetDesignatorSelectAction");

    internal static void Apply(Harmony harmony)
    {
        try
        {
            var original = AccessTools.Method(typeof(Designator_Dropdown), "GetDesignatorSelectAction");
            if (original == null || original.ReturnType != typeof(Action) || original.GetParameters().Length != 2)
            {
                Site.Fail(new MissingMethodException(typeof(Designator_Dropdown).FullName, "GetDesignatorSelectAction"));
                return;
            }

            harmony.Patch(original, postfix: new HarmonyMethod(typeof(DropdownEntryPatch), nameof(Postfix)));
        }
        catch (Exception ex)
        {
            Site.Fail(ex);
        }
    }

    /// <summary>The Designator parameter is bound by index (<c>__1</c>): the game's own name for it is not part of our contract.</summary>
    internal static void Postfix(ref Action __result, Designator __1)
    {
        if (Site.Disabled || __result == null || __1 == null)
        {
            return;
        }

        try
        {
            __result = new EntryAction(__1, __result).Run;
        }
        catch (Exception ex)
        {
            Site.Fail(ex);
        }
    }

    private sealed class EntryAction
    {
        private readonly Designator _entry;
        private readonly Action _original;

        internal EntryAction(Designator entry, Action original)
        {
            _entry = entry;
            _original = original;
        }

        internal void Run()
        {
            if (!Site.Disabled && MenuActivation.Depth == 0)
            {
                try
                {
                    if (IsPinGesture())
                    {
                        PinClickPatch.Toggle(_entry);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Site.Fail(ex);
                }
            }

            _original();
        }

        private static bool IsPinGesture()
        {
            var input = new PinClickInput(
                IsKeyEvent: false,
                RawType: RawEventType.Other,
                Button: 0,
                EventCtrl: false,
                EventAlt: false,
                EventShift: false,
                RawCtrl: UnityEngine.Input.GetKey(KeyCode.LeftControl) || UnityEngine.Input.GetKey(KeyCode.RightControl),
                RawAlt: UnityEngine.Input.GetKey(KeyCode.LeftAlt) || UnityEngine.Input.GetKey(KeyCode.RightAlt),
                RawShift: UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift),
                LeftDownThisFrame: UnityEngine.Input.GetMouseButtonDown(0),
                LeftUpThisFrame: UnityEngine.Input.GetMouseButtonUp(0));
            return PinClickClassifier.Classify(input, QuickMenuMod.EffectiveModifier) == PinClickAction.Pin;
        }
    }
}
