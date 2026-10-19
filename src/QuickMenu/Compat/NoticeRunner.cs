using System;
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
    internal static bool Pending { get; private set; } = true;

    internal static void Tick()
    {
        if (Current.ProgramState != ProgramState.Playing)
        {
            return;
        }

        Map? map = Find.CurrentMap;
        if (map == null || !map.IsPlayerHome)
        {
            return;
        }

        Pending = false;
        try
        {
            QuickMenuSettings s = QuickMenuMod.Settings;
            NoticeDecision d = NoticePolicy.Decide(s.FirstRunNoticeShown, s.MintNoticeShown, QuickMenuMod.MintActive);
            switch (d.Kind)
            {
                case NoticeKind.FirstRun:
                    Find.WindowStack.Add(new FirstRunNoticeWindow(d.IncludeMintLine));
                    break;
                case NoticeKind.MintMessage:
                    Messages.Message("QuickMenu.Notice.Mint".Translate(), MessageTypeDefOf.NeutralEvent, historical: false);
                    break;
                default:
                    return;
            }

            s.FirstRunNoticeShown |= d.SetFirstRunShown;
            s.MintNoticeShown |= d.SetMintShown;
            QuickMenuMod.SaveSettings();
        }
        catch (Exception ex)
        {
            Diagnostics.Log.LogOnce("Compat.Notice", $"Showing the notice failed: {ex}");
        }
    }
}
