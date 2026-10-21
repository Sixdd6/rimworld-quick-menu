using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using QuickMenu.Core.Diagnostics;
using QuickMenu.Core.Pins;
using QuickMenu.Core.Render;
using QuickMenu.Diagnostics;
using QuickMenu.Menu;
using RimWorld;
using UnityEngine;
using Verse;
using Log = QuickMenu.Diagnostics.Log;

namespace QuickMenu.Pinning;

/// <summary>
/// Fifth patch site (ADR 0009, FR-19): the pin marker on pinned single entries of an open dropdown list. The map fill (5a) has its own
/// fault site; the two draw postfixes share another. 5a runs after <c>Designator_Dropdown.ProcessInput</c> added the float menu or float grid and, while an architect
/// menu is open, maps every option object to the entry designator its action wraps (our <c>EntryAction</c>; this happens before
/// Dubs Mint Menus rewraps the actions, and Mint keeps the option objects). 5b and 5c are postfixes on
/// <c>FloatMenuOption.DoGUI</c> and <c>FloatMenuGridOption.OnGUI</c>: one reference-identity lookup per option, then the pinned
/// lookup, then the draw (Repaint only), all allocation-free. A whole-dropdown pin marks no entries. The map holds the options of
/// the most recent architect dropdown only, and is cleared when that menu closes (chained <c>onCloseCallback</c>) and replaced on the next open.
/// </summary>
internal static class DropdownMarkerPatch
{
    internal static readonly PatchSite Site = new("DropdownMarkerPatch");

    /// <summary>Own fault site for the draw postfixes (5b, 5c), so map and draw faults can both be injected in one session.</summary>
    internal static readonly PatchSite DrawSite = new("DropdownMarkerPatch.Draw");

    private static readonly Dictionary<object, Row> _map = new(ReferenceEqualityComparer.Instance);
    private static Window? _mappedMenu;

    private static AccessTools.FieldRef<FloatMenu, List<FloatMenuOption>>? _menuOptions;
    private static AccessTools.FieldRef<FloatMenuGrid, List<FloatMenuGridOption>>? _gridOptions;
    private static AccessTools.FieldRef<FloatMenuOption, FloatMenuSizeMode>? _sizeMode;
    private static FieldInfo? _iconTex;
    private static FieldInfo? _shownItem;
    private static FieldInfo? _placeholder;

    private readonly record struct Row(Designator Entry, bool HasIcon);

    /// <summary>True when the 5a map fill was applied.</summary>
    internal static bool MapPatched { get; private set; }

    /// <summary>True when the 5b row postfix was applied.</summary>
    internal static bool RowPatched { get; private set; }

    /// <summary>True when the 5c grid postfix was applied.</summary>
    internal static bool GridPatched { get; private set; }

    /// <summary>Options currently mapped to entries (0 until an architect dropdown has been opened).</summary>
    internal static int MappedCount => _map.Count;

    internal static void Apply(Harmony harmony)
    {
        try
        {
            _menuOptions = AccessTools.FieldRefAccess<FloatMenu, List<FloatMenuOption>>("options");
            _gridOptions = AccessTools.FieldRefAccess<FloatMenuGrid, List<FloatMenuGridOption>>("options");
            _iconTex = AccessTools.Field(typeof(FloatMenuOption), "iconTex");
            _shownItem = AccessTools.Field(typeof(FloatMenuOption), "shownItem");
            _placeholder = AccessTools.Field(typeof(FloatMenuOption), "drawPlaceHolderIcon");
            try
            {
                _sizeMode = AccessTools.FieldRefAccess<FloatMenuOption, FloatMenuSizeMode>("sizeMode");
            }
            catch (Exception ex)
            {
                Log.LogOnce("Pinning.SizeMode", $"FloatMenuOption.sizeMode was not found, dropdown entry markers assume the normal icon size: {ex.Message}");
            }

            MapPatched = Patch(harmony, AccessTools.Method(typeof(Designator_Dropdown), nameof(Designator_Dropdown.ProcessInput), new[] { typeof(Event) }), nameof(MapPostfix));
            RowPatched = Patch(harmony, AccessTools.Method(typeof(FloatMenuOption), nameof(FloatMenuOption.DoGUI), new[] { typeof(Rect), typeof(bool), typeof(FloatMenu) }), nameof(RowPostfix));
            GridPatched = Patch(harmony, AccessTools.Method(typeof(FloatMenuGridOption), nameof(FloatMenuGridOption.OnGUI), new[] { typeof(Rect) }), nameof(GridPostfix));
            if (!MapPatched || !RowPatched || !GridPatched)
            {
                Site.Fail(new MissingMethodException("a dropdown marker target method was not found"));
            }
        }
        catch (Exception ex)
        {
            Site.Fail(ex);
        }
    }

    private static bool Patch(Harmony harmony, MethodBase? original, string postfix)
    {
        if (original == null)
        {
            return false;
        }

        harmony.Patch(original, postfix: new HarmonyMethod(typeof(DropdownMarkerPatch), postfix));
        return true;
    }

    /// <summary>5a. Runs once per dropdown click (also when the pin prefix skipped the original): allocation is allowed here.</summary>
    internal static void MapPostfix(Designator_Dropdown __instance)
    {
        if (Site.Disabled)
        {
            return;
        }

        try
        {
            FaultInjection.Check(FaultPoint.DropdownMarkerMap);
            if (!ArchitectDrawContext.IsActive())
            {
                return;
            }

            Window? menu = TopMenu();
            if (menu == null || ReferenceEquals(menu, _mappedMenu))
            {
                return;
            }

            _map.Clear();
            _mappedMenu = menu;
            ChainOnClose(menu);
            if (menu is FloatMenu floatMenu && _menuOptions != null)
            {
                foreach (FloatMenuOption option in _menuOptions(floatMenu))
                {
                    Add(__instance, option, option.action, HasIcon(option));
                }
            }
            else if (menu is FloatMenuGrid grid && _gridOptions != null)
            {
                foreach (FloatMenuGridOption option in _gridOptions(grid))
                {
                    Add(__instance, option, option.action, true);
                }
            }
        }
        catch (Exception ex)
        {
            Site.Fail(ex);
        }
    }

    /// <summary>Clears the map when <paramref name="menu"/> closes, keeping the previous callback. Runs once per open: allocation is fine.</summary>
    private static void ChainOnClose(Window menu)
    {
        switch (menu)
        {
            case FloatMenu floatMenu:
                Action? previous = floatMenu.onCloseCallback;
                floatMenu.onCloseCallback = () =>
                {
                    previous?.Invoke();
                    ClearIfMapped(floatMenu);
                };
                break;
            case FloatMenuGrid grid:
                Action? previousGrid = grid.onCloseCallback;
                grid.onCloseCallback = () =>
                {
                    previousGrid?.Invoke();
                    ClearIfMapped(grid);
                };
                break;
        }
    }

    private static void ClearIfMapped(Window menu)
    {
        if (ReferenceEquals(_mappedMenu, menu))
        {
            _map.Clear();
            _mappedMenu = null;
        }
    }

    /// <summary>5b. Parameters are bound by index: the game's own names are not part of our contract.</summary>
    internal static void RowPostfix(FloatMenuOption __instance, Rect __0)
    {
        if (DrawSite.Disabled || _map.Count == 0)
        {
            return;
        }

        try
        {
            using var scope = new MeasureScope(StatId.PinMarkerEntry);
            if (!_map.TryGetValue(__instance, out Row row))
            {
                return;
            }

            FaultInjection.Check(FaultPoint.DropdownMarkerDraw);
            if (Event.current.type != EventType.Repaint || PinnedDesignators.LookupEntry(row.Entry) != PinMark.Full
                || !Textures.EnsurePinMarkers() || Textures.BuiltPinMarkerSizing is not { } sizing)
            {
                return;
            }

            MarkerRect fill;
            if (row.HasIcon)
            {
                Rect hoverRect = __0;
                hoverRect.height--;
                bool hovered = !__instance.Disabled && Mouse.IsOver(hoverRect);
                float iconSize = _sizeMode != null && _sizeMode(__instance) == FloatMenuSizeMode.Tiny ? TinyIconSize : NormalIconSize;
                fill = sizing.ListRow(__0.x, __0.y, iconSize, hovered);
            }
            else
            {
                fill = sizing.ListRowNoIcon(__0.x, __0.y, __0.width, __0.height);
            }

            Textures.DrawPinMarker(fill, PinMarkerKind.Entry);
        }
        catch (Exception ex)
        {
            DrawSite.Fail(ex);
        }
    }

    /// <summary>5c.</summary>
    internal static void GridPostfix(FloatMenuGridOption __instance, Rect __0)
    {
        if (DrawSite.Disabled || _map.Count == 0)
        {
            return;
        }

        try
        {
            using var scope = new MeasureScope(StatId.PinMarkerEntry);
            if (!_map.TryGetValue(__instance, out Row row))
            {
                return;
            }

            FaultInjection.Check(FaultPoint.DropdownMarkerDraw);
            if (Event.current.type != EventType.Repaint || PinnedDesignators.LookupEntry(row.Entry) != PinMark.Full
                || !Textures.EnsurePinMarkers() || Textures.BuiltPinMarkerSizing is not { } sizing)
            {
                return;
            }

            Textures.DrawPinMarker(sizing.GridCell(__0.x, __0.y, __0.width), PinMarkerKind.Entry);
        }
        catch (Exception ex)
        {
            DrawSite.Fail(ex);
        }
    }

    private const float NormalIconSize = 27f; // FloatMenuOption.CurIconSize (private) for the Normal and Tiny size modes
    private const float TinyIconSize = 16f;

    private static void Add(Designator_Dropdown dropdown, object option, Action? action, bool hasIcon)
    {
        if (DropdownEntryPatch.EntryOf(action) is { } entry && dropdown.Elements.Contains(entry))
        {
            _map[option] = new Row(entry, hasIcon);
        }
    }

    private static bool HasIcon(FloatMenuOption option) =>
        _iconTex?.GetValue(option) != null || _shownItem?.GetValue(option) != null || _placeholder?.GetValue(option) is true || option.iconThing != null;

    /// <summary>The top-most float menu or float grid on the window stack, or null.</summary>
    private static Window? TopMenu()
    {
        IList<Window> windows = Find.WindowStack.Windows;
        for (int i = windows.Count - 1; i >= 0; i--)
        {
            if (windows[i] is FloatMenu or FloatMenuGrid)
            {
                return windows[i];
            }
        }

        return null;
    }
}
