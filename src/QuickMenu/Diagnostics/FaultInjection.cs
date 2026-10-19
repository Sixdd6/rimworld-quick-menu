using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using LudeonTK;
using QuickMenu.Core.Diagnostics;
using QuickMenu.Menu;
using QuickMenu.Settings;
using Verse;

namespace QuickMenu.Diagnostics;

/// <summary>The exception thrown by an armed fault point. Only the dev-mode debug action can arm one.</summary>
internal sealed class InjectedFaultException : Exception
{
    internal InjectedFaultException(FaultPoint point)
        : base($"Injected test fault at {point} (Quick Menu: fault injection)")
    {
    }
}

/// <summary>
/// Dev-mode fault injection for NFR-6 (T-72). Each guarded site calls <see cref="Check"/> inside its own try block; the call is
/// one volatile int read while nothing is armed. The debug action arms one point, the next call to its site throws, and the
/// site's catch must log once, disable the site and let the game carry on. Not a feature: it does nothing outside dev mode.
/// </summary>
internal static class FaultInjection
{
    private static readonly FaultArming _arming = new();

    private sealed class Target
    {
        internal Target(FaultPoint point, PatchSite site, string hint, Action? prepare = null)
        {
            Point = point;
            Site = site;
            Hint = hint;
            Prepare = prepare;
        }

        internal FaultPoint Point { get; }

        internal PatchSite Site { get; }

        internal string Hint { get; }

        internal Action? Prepare { get; }
    }

    // One row per guarded site that can fail while the game runs. Startup-only sites (the mod constructor, the catalog
    // build, the texture masks, mod detection) run before dev mode can be used; they are covered by the code audit.
    private static readonly Target[] _targets =
    {
        new(FaultPoint.TriggerPoller, Trigger.TriggerPoller.Site, "automatic: fails on the next frame; the menu stops opening, pinning keeps working"),
        new(FaultPoint.PinPrefix, Pinning.PinClickPatch.Site, "Ctrl+click an architect button (the architect tab must be open); vanilla activation must still work"),
        new(FaultPoint.DropdownPostfix, Pinning.DropdownEntryPatch.Site, "open an architect dropdown (the chair or floor button); entries must still activate"),
        new(FaultPoint.MenuOpen, MenuController.Site, "press the menu trigger; nothing opens"),
        new(FaultPoint.WindowDraw, QuickMenuWindow.DrawSite, "press the menu trigger; the menu closes at once, the game continues"),
        new(FaultPoint.SettingsPage, SettingsPage.Site, "opens the mod settings; the page shows its error label, the window closes normally", QuickMenuMod.OpenSettings),
        new(FaultPoint.SettingsPreview, LayoutPreview.Site, "opens the mod settings; the preview vanishes, the rest of the page works", QuickMenuMod.OpenSettings),
        new(FaultPoint.CatalogIdeology, Catalog.DesignatorCatalog.IdeoSite, "press the menu trigger or Ctrl+click an architect button; vanilla and the menu still work"),
        new(FaultPoint.TexturesSized, Textures.SizedSite, "press the menu trigger; the menu opens without the ring textures, or closes, the game continues"),
        new(FaultPoint.Notice, Compat.NoticeRunner.Site, "automatic: fails on the next frame; no notice, no further attempts", Compat.NoticeRunner.Rearm),
        new(FaultPoint.FirstRunWindow, FirstRunNoticeWindow.Site, "opens the first-run notice; it closes itself", () => Find.WindowStack.Add(new FirstRunNoticeWindow(false))),
        new(FaultPoint.PinMarker, Pinning.PinMarkerPatch.Site, "open the architect menu with at least one pin; buttons keep drawing and working without markers"),
        new(FaultPoint.TexturesPinMarker, Textures.PinMarkerSite, "open the architect menu with at least one pin; the marker textures are skipped, buttons keep drawing without markers"),
        new(FaultPoint.DropdownMarkerMap, Pinning.DropdownMarkerPatch.Site, "open an architect dropdown (the chair or floor button); the list opens and works without markers"),
        new(FaultPoint.DropdownMarkerDraw, Pinning.DropdownMarkerPatch.Site, "open an architect dropdown with a pinned entry; the list keeps drawing and working without markers"),
        new(FaultPoint.BindingCaptureWindow, BindingCaptureWindow.Site, "opens the key capture window; it closes itself", () => Find.WindowStack.Add(new BindingCaptureWindow(_ => { }))),
    };

    /// <summary>Throws <see cref="InjectedFaultException"/> when <paramref name="point"/> is armed. Call it first inside the site's try block.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Check(FaultPoint point)
    {
        if (_arming.Any)
        {
            Fire(point);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Fire(FaultPoint point)
    {
        if (!Prefs.DevMode)
        {
            _arming.Clear();
            return;
        }

        if (_arming.TryConsume(point))
        {
            throw new InjectedFaultException(point);
        }
    }

    /// <summary>Arms <paramref name="target"/> unless its site is already disabled. Returns the line to log.</summary>
    private static string Arm(Target target)
    {
        if (target.Site.Disabled)
        {
            return $"{target.Site.Name} is already disabled for this session; restart the game to test it again.";
        }

        _arming.Arm(target.Point);
        target.Prepare?.Invoke();
        return $"armed {target.Site.Name}: the next call throws. {target.Hint}. Expect one error line, then the site stays disabled.";
    }

    private static string Describe(Target target) =>
        target.Site.Disabled ? $"{target.Site.Name} (already disabled)" : target.Site.Name;

    [DebugAction("Quick Menu", "fault injection", allowedGameStates = AllowedGameStates.Playing)]
    private static void Menu()
    {
        var options = new List<DebugMenuOption>(_targets.Length);
        foreach (Target target in _targets)
        {
            Target captured = target;
            options.Add(new DebugMenuOption(Describe(captured), DebugMenuOptionMode.Action, () => Log.Info(Arm(captured))));
        }

        Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
    }
}
