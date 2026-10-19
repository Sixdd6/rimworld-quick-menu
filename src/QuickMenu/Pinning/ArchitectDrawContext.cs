using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace QuickMenu.Pinning;

/// <summary>
/// Answers "is an architect menu drawing right now" for the pin marker (ADR 0009). The vanilla Architect tab, which Better
/// Architect Menu draws into, runs from <c>MainTabWindow_Architect.ExtraOnGUI</c>, outside any window body, so no window is
/// being drawn and the open main tab decides. Dubs Mint Menus' build menu draws inside its own window body. Mint's wheel,
/// the command bars of selected things and our own menu are not architect menus. Allocation-free.
/// </summary>
internal static class ArchitectDrawContext
{
    private static Type? _mintArchitectWindow;
    private static Type? _mintWheelWindow;
    private static bool _mintTypesResolved;

    /// <summary>Mint's build-menu window type (<c>MainTabWindow_MintArchitect</c>), resolved once; null without Mint.</summary>
    internal static Type? MintArchitectWindow
    {
        get
        {
            ResolveMintTypes();
            return _mintArchitectWindow;
        }
    }

    /// <summary>Mint's wheel window type (<c>MainTabWindow_MayaMenu</c>), resolved once; null without Mint. Pin gestures work there, markers are not drawn.</summary>
    internal static Type? MintWheelWindow
    {
        get
        {
            ResolveMintTypes();
            return _mintWheelWindow;
        }
    }

    /// <summary>True while the vanilla Architect tab (and so Better Architect Menu) or Mint's build menu is the one being drawn.</summary>
    internal static bool IsActive()
    {
        Window? drawing = Find.WindowStack.currentlyDrawnWindow;
        if (drawing == null)
        {
            return Find.MainTabsRoot.OpenTab == MainButtonDefOf.Architect;
        }

        return Compat.ModDetection.Mint && MintArchitectWindow is { } mint && drawing.GetType() == mint;
    }

    private static void ResolveMintTypes()
    {
        if (_mintTypesResolved || !Compat.ModDetection.Mint)
        {
            return;
        }

        _mintTypesResolved = true;
        _mintArchitectWindow = AccessTools.TypeByName("DubsMintMenus.MainTabWindow_MintArchitect");
        _mintWheelWindow = AccessTools.TypeByName("DubsMintMenus.MainTabWindow_MayaMenu");
    }
}
