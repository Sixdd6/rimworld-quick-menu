using QuickMenu.Activation;
using QuickMenu.Core.Geometry;
using QuickMenu.Core.Input;
using QuickMenu.Core.Layout;
using QuickMenu.Core.Settings;
using QuickMenu.Diagnostics;
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

    /// <summary>False until the pointer has moved a few pixels since opening (S4a), so a hub under the pointer does not light up.</summary>
    internal static bool HoverEnabled => _machine?.HoverEnabled ?? false;

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

            using var scope = new MeasureScope(StatId.OpenPath);
            PerfCounters.MarkOpen();
            var machine = new TriggerStateMachine();
            if (machine.Handle(TriggerEvent.TriggerPressed(mouse)).Kind != MenuCommandKind.Open)
            {
                return;
            }

            MenuContent content = BuildContent(mouse);
            _machine = machine;
            _content = content;
            LastSlotCount = content.Model.SlotCount;
            LastDesignators = content.Designators;
            Find.WindowStack.Add(_window = new QuickMenuWindow(content, Time.frameCount));
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

    /// <summary>Closes the window without consulting the state machine (a window fault). Settings are not written.</summary>
    internal static void Close()
    {
        QuickMenuWindow? window = _window;
        Reset();
        window?.Close();
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
                if (_content != null && command.Index >= 0 && command.Index < _content.Designators.Length && _content.Designators[command.Index] is { } rejected)
                {
                    DesignatorActivator.Reject(rejected);
                }

                break;
            case MenuCommandKind.ExitEdit:
                QuickMenuMod.SaveSettings();
                break;
            default:
                // Back, OpenSub and EnterEdit cannot occur yet: no satellite (T-41, T-43) and no hub edit button (T-44).
                break;
        }
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
