using System;
using System.Collections.Generic;
using QuickMenu.Core.Diagnostics;
using QuickMenu.Core.Input;
using QuickMenu.Menu;
using QuickMenu.Settings;
using RimWorld;
using Verse;

namespace QuickMenu.Compat;

/// <summary>
/// Shows the first-run window or the Mint message once, the first time a colony map is current. Driven by the trigger
/// poller; costs one static bool check per frame until it has run (then <see cref="Pending"/> is false for the session).
/// </summary>
internal static class NoticeRunner
{
    internal static readonly Diagnostics.PatchSite Site = new("Compat.Notice");

    internal static bool Pending { get; private set; } = true;

    /// <summary>Lets <see cref="Tick"/> run again (dev fault injection only).</summary>
    internal static void Rearm() => Pending = true;

    internal static void Tick()
    {
        if (Site.Disabled)
        {
            Pending = false;
            return;
        }

        try
        {
            Diagnostics.FaultInjection.Check(FaultPoint.Notice);
            if (Current.ProgramState != ProgramState.Playing)
            {
                return;
            }

            Map? map = Find.CurrentMap;
            if (map == null || !map.IsPlayerHome || BlockedByWindow())
            {
                return;
            }

            Pending = false;
            QuickMenuSettings s = QuickMenuMod.Settings;
            NoticeDecision d = NoticePolicy.Decide(s.FirstRunNoticeShown, s.MintNoticeShown, QuickMenuMod.MintActive, s.PinModifierUserSet);
            switch (d.Kind)
            {
                case NoticeKind.FirstRun:
                    Find.WindowStack.Add(new FirstRunNoticeWindow(d.IncludeMintLine));
                    break;
                case NoticeKind.MintMessage:
                    Messages.Message("QuickMenu.Notice.Mint".Translate(), MessageTypeDefOf.NeutralEvent, historical: false);
                    break;
            }

            bool changed = (d.SetFirstRunShown && !s.FirstRunNoticeShown) || (d.SetMintShown && !s.MintNoticeShown);
            s.FirstRunNoticeShown |= d.SetFirstRunShown;
            s.MintNoticeShown |= d.SetMintShown;
            if (changed)
            {
                QuickMenuMod.SaveSettings();
            }
        }
        catch (Exception ex)
        {
            Pending = false;
            Site.Fail(ex);
        }
    }

    /// <summary>True while a pausing or input-absorbing window (a dialog, the options screen) is open; the notice waits for it. Plain loop, runs only until the notice is shown.</summary>
    private static bool BlockedByWindow()
    {
        WindowStack stack = Find.WindowStack;
        for (int i = 0; i < stack.Count; i++)
        {
            Window window = stack[i];
            if (window.forcePause || window.absorbInputAroundWindow)
            {
                return true;
            }
        }

        return false;
    }
}
