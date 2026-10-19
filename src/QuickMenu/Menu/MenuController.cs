using QuickMenu.Activation;
using QuickMenu.Catalog;
using QuickMenu.Core.Geometry;
using QuickMenu.Core.Input;
using QuickMenu.Core.Layout;
using QuickMenu.Core.Pins;
using QuickMenu.Core.Settings;
using QuickMenu.Diagnostics;
using QuickMenu.Pinning;
using UnityEngine;
using Verse;

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

    /// <summary>Slot count of the most recently built window; the P-9 control window draws the same number.</summary>
    internal static int LastSlotCount { get; private set; }

    /// <summary>The designators of the most recently built window, for the P-9 control window.</summary>
    internal static Designator?[]? LastDesignators { get; private set; }

    internal static bool IsOpen => _window != null;

    /// <summary>True while the open menu is in edit mode (the hub shows Done).</summary>
    internal static bool IsEditing => _machine?.State == MenuState.Edit;

    /// <summary>Opens the menu in the current frame. Pressing the trigger while it is open closes it.</summary>
    internal static void Open()
    {
        if (Site.Disabled || QuickMenuWindow.DrawSite.Disabled)
        {
            return;
        }

        try
        {
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
        LastSlotCount = content.Model.SlotCount;
        LastDesignators = content.Designators;
        return content;
    }

    /// <summary>Closes the window without consulting the state machine (a window fault). Settings are not written.</summary>
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
                Close();
                if (command.WasEditing)
                {
                    QuickMenuMod.SaveSettings();
                }

                break;
            case MenuCommandKind.Activate:
                Designator? target = _content != null && command.Index >= 0 && command.Index < _content.Designators.Length
                    ? _content.Designators[command.Index]
                    : null;
                Close();
                if (target != null)
                {
                    DesignatorActivator.Activate(target, Event.current);
                }

                break;
            case MenuCommandKind.Reject:
                if (_content != null && command.Index >= 0 && command.Index < _content.Designators.Length && _content.Reason(command.Index) is { } reason)
                {
                    DesignatorActivator.Reject(reason);
                }

                break;
            case MenuCommandKind.Unpin:
                Unpin(command.Index);
                break;
            case MenuCommandKind.ExitEdit:
                // Done or the first Esc: the pins are already applied; one write ends the edit session (a later close is a plain Close).
                QuickMenuMod.SaveSettings();
                break;
            default:
                // Back and OpenSub are the satellite's (T-43). EnterEdit changes only the state, which the window reads when it paints.
                break;
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
        PinService.Toggle(pin.Key, pin.Label, pin.PackageId);
        window.Refresh();
    }

    private static MenuContent BuildContent(Vec2 mouse)
    {
        Settings.QuickMenuSettings settings = QuickMenuMod.Settings;
        float buttonSize = SettingsRules.ClampButtonSize(settings.ButtonSize);
        int rings = SettingsRules.ClampRings(settings.Rings);
        var key = new WheelLayoutKey(settings.Revision, UI.screenWidth, UI.screenHeight, Prefs.UIScale, buttonSize, rings);
        PlacedWheel placed = _layout.Place(key, mouse, WheelModel.DefaultPlateHeight);
        return MenuContent.Build(placed, buttonSize, rings);
    }

    private static void Reset()
    {
        _window = null;
        _machine = null;
        _content = null;
    }
}
