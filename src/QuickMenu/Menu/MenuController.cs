using QuickMenu.Activation;
using QuickMenu.Catalog;
using QuickMenu.Core.Geometry;
using QuickMenu.Core.Input;
using QuickMenu.Core.Layout;
using QuickMenu.Core.Materials;
using QuickMenu.Core.Pins;
using QuickMenu.Core.Settings;
using QuickMenu.Diagnostics;
using QuickMenu.Pinning;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using FaultPoint = QuickMenu.Core.Diagnostics.FaultPoint;
using GridLayout = QuickMenu.Core.Layout.GridLayout;

namespace QuickMenu.Menu;

/// <summary>
/// Opens, closes and drives the quick menu window (ADR 0007, ADR 0006). Created lazily: nothing exists while the menu is
/// closed. The window only forwards input as <see cref="TriggerEvent"/>s; the Core <see cref="TriggerStateMachine"/>
/// decides, and <see cref="Dispatch"/> executes the resulting <see cref="MenuCommand"/>.
/// </summary>
internal static class MenuController
{
    internal static readonly PatchSite Site = new("MenuController.Open");

    private static readonly WheelLayoutCache _layout = new();

    private static QuickMenuWindow? _window;
    private static TriggerStateMachine? _machine;
    private static MenuContent? _content;
    private static SatelliteController? _satellite;

    /// <summary>True when edit mode removed pins that are not yet written; the one write happens when the edit session ends.</summary>
    private static bool _editDirty;

    /// <summary>Slot count of the most recently built window; the P-9 control window draws the same number.</summary>
    internal static int LastSlotCount { get; private set; }

    /// <summary>The designators of the most recently built window, for the P-9 control window.</summary>
    internal static Designator?[]? LastDesignators { get; private set; }

    internal static bool IsOpen => _window != null;

    /// <summary>The satellite ring on screen (sub-ring or material ring), or null at the main level.</summary>
    internal static RingContent? ActiveRing => _satellite?.Active;

    /// <summary>True while the open menu is in edit mode (the hub shows Done).</summary>
    internal static bool IsEditing => _machine?.State == MenuState.Edit;

    /// <summary>The slot being dragged in edit mode (after 6 px), or -1.</summary>
    internal static int DragSource => _machine?.DragSource ?? -1;

    /// <summary>True when a drop on <paramref name="slot"/> can swap: the slot has a position on the pin board.</summary>
    internal static bool CanDrop(int slot) => _content != null && slot >= 0 && slot < _content.BoardIndex.Length && _content.BoardIndex[slot] >= 0;

    /// <summary>Opens the menu in the current frame. Pressing the trigger while it is open closes it.</summary>
    internal static void Open()
    {
        if (Site.Disabled || QuickMenuWindow.DrawSite.Disabled)
        {
            return;
        }

        try
        {
            FaultInjection.Check(FaultPoint.MenuOpen);
            if (_window != null && !Find.WindowStack.IsOpen(_window))
            {
                Reset();
            }

            Vector2 pointer = UI.MousePositionOnUIInverted;
            var mouse = new Vec2(pointer.x, pointer.y);
            if (_window != null)
            {
                Dispatch(TriggerEvent.TriggerPressed(mouse));
                return;
            }

            PerfCounters.MarkOpen();
            var machine = new TriggerStateMachine();
            if (machine.Handle(TriggerEvent.TriggerPressed(mouse)).Kind != MenuCommandKind.Open)
            {
                return;
            }

            // The content (text measuring) is built on the window's first DoWindowContents, inside OnGUI (BuildFor).
            // OpenPath therefore covers the whole of that build, which is the work of opening the menu; this method
            // itself only creates the state machine and the window.
            _machine = machine;
            Find.WindowStack.Add(_window = new QuickMenuWindow(mouse, Time.frameCount));
        }
        catch (System.Exception ex)
        {
            Reset();
            Site.Fail(ex);
        }
    }

    /// <summary>Feeds one input event to the state machine and executes what it answers. The menu window calls this.</summary>
    internal static void Dispatch(in TriggerEvent e)
    {
        TriggerStateMachine? machine = _machine;
        if (machine == null)
        {
            return;
        }

        Execute(machine.Handle(e));
    }

    /// <summary>True when <paramref name="window"/> is the window this controller currently drives.</summary>
    internal static bool Owns(QuickMenuWindow window) => ReferenceEquals(_window, window);

    /// <summary>
    /// Builds the content for <paramref name="window"/> (called from its first DoWindowContents, so text measuring
    /// happens in OnGUI) and publishes it. Returns null when the window is not the current one.
    /// </summary>
    internal static MenuContent? BuildFor(QuickMenuWindow window, Vec2 mouse)
    {
        if (!Owns(window))
        {
            return null;
        }

        using var scope = new MeasureScope(StatId.OpenPath);
        MenuContent content = BuildContent(mouse);
        _content = content;
        LastSlotCount = content.Model.SlotCount;
        LastDesignators = content.Designators;
        return content;
    }

    /// <summary>
    /// Builds the content again for <paramref name="window"/> after the pins changed (modifier+click unpin), at the same
    /// position, and publishes it. Returns null when the window is not the current one or the build fails.
    /// </summary>
    internal static MenuContent? Rebuild(QuickMenuWindow window, Vec2 mouse)
    {
        if (!Owns(window))
        {
            return null;
        }

        using var scope = new MeasureScope(StatId.Rebuild);
        MenuContent content = BuildContent(mouse);
        _content = content;
        _satellite = null;
        if (IsEditing)
        {
            UpdateHelp();
        }

        LastSlotCount = content.Model.SlotCount;
        LastDesignators = content.Designators;
        return content;
    }

    /// <summary>Closes the window without consulting the state machine (a window fault). Pins removed in edit mode are written once.</summary>
    internal static void Close()
    {
        QuickMenuWindow? window = _window;
        Reset();
        if (window != null)
        {
            CloseWindow(window);
        }
    }

    /// <summary>Closes a window; if <c>Close</c> throws, removes it from the stack directly so it cannot linger.</summary>
    internal static void CloseWindow(Window window)
    {
        try
        {
            window.Close();
        }
        catch (System.Exception ex)
        {
            Diagnostics.Log.LogOnce("Menu.Close", $"Closing the menu window failed, removing it from the window stack: {ex}");
            try
            {
                Find.WindowStack.TryRemove(window, doCloseSound: false);
            }
            catch (System.Exception ex2)
            {
                Diagnostics.Log.LogOnce("Menu.Close.Remove", $"Removing the menu window from the window stack failed: {ex2}");
            }
        }
    }

    /// <summary>Called by the window when it has been removed from the stack for any reason.</summary>
    internal static void Closed(QuickMenuWindow window)
    {
        if (_window == window)
        {
            Reset();
        }
    }

    private static void Execute(MenuCommand command)
    {
        switch (command.Kind)
        {
            case MenuCommandKind.Close:
                Close(); // Reset writes the edit session's pins if they changed

                break;
            case MenuCommandKind.Activate:
                try
                {
                    ActivateSlot(command);
                }
                catch (System.Exception ex)
                {
                    Diagnostics.Log.LogOnce("Menu.Activate", $"Activating a slot failed: {ex}");
                    Close();
                }

                break;
            case MenuCommandKind.Reject:
                RejectSlot(command.Index);
                break;
            case MenuCommandKind.OpenSub:
                OpenSub(command.Index);
                break;
            case MenuCommandKind.Back:
                if (_satellite != null && _satellite.Back())
                {
                    _machine?.ResumeSatellite();
                }
                else
                {
                    _satellite = null;
                }

                break;
            case MenuCommandKind.Unpin:
                Unpin(command.Index);
                break;
            case MenuCommandKind.Swap:
                Swap(command.Index, command.Other);
                break;
            case MenuCommandKind.ExitEdit:
                // Done or the first Esc: the pins are already applied; one write ends the edit session, and only if something changed.
                HelpPanelRenderer.Clear();
                FlushEdit();
                break;
            case MenuCommandKind.EnterEdit:
                UpdateHelp();
                break;
            default:
                break;
        }
    }

    /// <summary>Opens the satellite of a main slot (sub-ring or material ring), or the material ring of a sub-ring option.</summary>
    private static void OpenSub(int slot)
    {
        MenuContent? content = _content;
        if (content == null || slot < 0)
        {
            return;
        }

        try
        {
            SatelliteController satellite = _satellite ??= new SatelliteController(content);
            bool opened = satellite.Active == null ? slot < content.Designators.Length && satellite.OpenFromMain(slot) : satellite.OpenMaterialFromSub(slot);
            if (!opened)
            {
                Close();
                return;
            }

            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
        }
        catch (System.Exception ex)
        {
            Diagnostics.Log.LogOnce("Menu.OpenSub", $"Opening a sub-menu failed, the menu is closed: {ex}");
            Close();
        }
    }

    private static void ActivateSlot(MenuCommand command)
    {
        SatelliteController? satellite = _satellite;
        if (command.Level == MenuLevel.Satellite)
        {
            if (satellite == null)
            {
                Close();
                return;
            }

            bool found = satellite.TryResolve(command.Index, out Designator designator, out ThingDef? stuff, out Designator_Dropdown? dropdown);
            Close();
            if (found)
            {
                DesignatorActivator.Activate(designator, stuff, dropdown);
            }

            return;
        }

        MenuContent? content = _content;
        int i = command.Index;
        Designator? target = content != null && i >= 0 && i < content.Designators.Length ? content.Designators[i] : null;
        Designator? element = content != null && target != null ? content.Targets[i] : null;
        StuffOptionSet set = content != null && target != null ? content.StuffSets[i] : default;
        Designator_Dropdown? owner = content != null && target != null ? content.Dropdowns[i] : null;
        Close();
        if (target == null)
        {
            return;
        }

        if (set.IsStuffBuild && set.Outcome == StuffRingOutcome.Skip)
        {
            DesignatorActivator.Activate(element ?? target, set.Options[0].Def, owner);
        }
        else if (element != null)
        {
            DesignatorActivator.Activate(element, null, owner);
        }
        else
        {
            DesignatorActivator.Activate(target, Event.current);
        }
    }

    private static void RejectSlot(int slot)
    {
        string? reason;
        if (_satellite?.Active is { } ring)
        {
            reason = slot >= 0 && slot < ring.Count ? ring.Reason(slot) : null;
        }
        else
        {
            reason = _content != null && slot >= 0 && slot < _content.Designators.Length ? _content.Reason(slot) : null;
        }

        if (reason != null)
        {
            DesignatorActivator.Reject(reason);
        }
    }

    /// <summary>
    /// Pin-modifier+click on slot <paramref name="slot"/> (use or edit mode): toggles its pin off through
    /// <see cref="PinService"/> (Tick_Low, the "Removed" message, one settings write), then refreshes the wheel in place.
    /// The menu stays open, so several pins can be removed in a row and the player sees the slot go empty; closing and
    /// reopening would move nothing but lose the hover state and cost a second open.
    /// </summary>
    private static void Unpin(int slot)
    {
        PinEntry? pin = _content != null && slot >= 0 && slot < _content.Pins.Length ? _content.Pins[slot] : null;
        QuickMenuWindow? window = _window;
        if (pin == null || window == null)
        {
            return;
        }

        // The saved pin's own key, so a reserved (missing-mod) pin and a pin whose catalog entry has another key both unpin.
        // In edit mode the write is deferred to the end of the session; in use mode it is immediate.
        bool editing = IsEditing;
        PinResult result = PinService.Toggle(pin.Key, pin.Label, pin.PackageId, persist: !editing);
        if (editing && result == PinResult.Unpinned)
        {
            _editDirty = true;
        }

        window.Refresh();
    }

    /// <summary>
    /// Drag swap in edit mode: exchanges the pins behind two slots (Fixed: either may be empty, so the pin moves; Auto: the
    /// two pins exchange places), plays Tick_Tiny and refreshes the wheel. No settings write: the session's one write
    /// happens when edit mode ends.
    /// </summary>
    private static void Swap(int from, int to)
    {
        MenuContent? content = _content;
        QuickMenuWindow? window = _window;
        if (content == null || window == null || !CanDrop(from) || !CanDrop(to))
        {
            return;
        }

        if (PinService.Board.Swap(content.BoardIndex[from], content.BoardIndex[to]))
        {
            _editDirty = true;
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            window.Refresh();
        }
    }

    /// <summary>Tells the help panel the pin count and whether the menu is full (edit start and after each pin change).</summary>
    private static void UpdateHelp()
    {
        Settings.QuickMenuSettings settings = QuickMenuMod.Settings;
        int used = PinService.Board.VisibleCount(PinService.Resolver);
        bool auto = settings.Sizing == SizingMode.Auto;
        int capacity = PinService.Board.Capacity;
        bool full = auto
            ? SettingsRules.IsAutoFull(settings.Layout, settings.Sizing, used, settings.GridColumns, SettingsRules.ClampButtonSize(settings.ButtonSize), new Vec2(UI.screenWidth, UI.screenHeight), WheelModel.DefaultPlateHeight)
            : used >= capacity;
        HelpPanelRenderer.SetContent(auto, used, capacity, full);
    }

    /// <summary>Writes the settings once if edit mode changed them, then clears the flag.</summary>
    private static void FlushEdit()
    {
        if (_editDirty)
        {
            _editDirty = false;
            QuickMenuMod.SaveSettings();
        }
    }

    private static MenuContent BuildContent(Vec2 mouse)
    {
        Settings.QuickMenuSettings settings = QuickMenuMod.Settings;
        float buttonSize = SettingsRules.ClampButtonSize(settings.ButtonSize);
        var screen = new Vec2(UI.screenWidth, UI.screenHeight);
        float plateHeight = WheelModel.DefaultPlateHeight;
        bool auto = settings.Sizing == SizingMode.Auto;
        System.Collections.Generic.IReadOnlyList<PinSlot> pins = PinService.Board.VisibleSlots(PinService.Resolver);
        MenuPlacement placement;
        if (settings.Layout == MenuLayout.Grid)
        {
            int columns = SettingsRules.ClampColumns(settings.GridColumns);
            int rows = auto ? Capacity.AutoGridRows(pins.Count, columns) : SettingsRules.ClampRows(settings.GridRows);
            float plateWidth = GridLayout.Panel(new GridParams(buttonSize, columns, rows, default)).Width;
            PlacedGrid grid = GridLayout.Place(buttonSize, columns, rows, mouse, screen, plateWidth, plateHeight);
            placement = new MenuPlacement(grid.Center, grid.Slots, 1, grid.Panel, columns, rows);
        }
        else if (auto)
        {
            PlacedAutoWheel wheel = WheelLayout.PlaceAuto(pins.Count, buttonSize, mouse, screen, plateHeight);
            placement = new MenuPlacement(wheel.Center, wheel.Slots, wheel.Rings);
        }
        else
        {
            int rings = SettingsRules.ClampRings(settings.Rings);
            var key = new WheelLayoutKey(settings.Revision, UI.screenWidth, UI.screenHeight, Prefs.UIScale, buttonSize, rings);
            PlacedWheel placed = _layout.Place(key, mouse, plateHeight);
            placement = new MenuPlacement(placed.Center, placed.Slots, rings);
        }

        return MenuContent.Build(placement, buttonSize, pins);
    }

    private static void Reset()
    {
        _window = null;
        _machine = null;
        _content = null;
        _satellite = null;
        try
        {
            HelpPanelRenderer.Clear();
            FlushEdit(); // every way out of the menu (Done, Esc, outside click, a fault, an external close) ends the edit session
        }
        catch (System.Exception ex)
        {
            Diagnostics.Log.LogOnce("Menu.Reset", $"Ending the menu session failed: {ex}");
        }
    }
}
