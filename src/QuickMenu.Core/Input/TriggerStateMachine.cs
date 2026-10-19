using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;

namespace QuickMenu.Core.Input;

/// <summary>
/// Open/close/back/activate decisions (FR-2, FR-11..13, design.md S4a). Toggle mode by default; with
/// <see cref="HoldMode"/> a <see cref="TriggerEventKind.TriggerReleased"/> over the hub cancels (T-81; the rest of hold
/// mode is T-63). The hub is one target, Edit (Done in edit mode), and a click on it never closes the menu.
/// Left clicks act on mouse-up and only when the press began on the same target. Right-click acts on press.
/// Allocation free.
/// </summary>
public sealed class TriggerStateMachine
{
    private bool _pressValid;
    private Hit _pressHit;

    public MenuState State { get; private set; }

    /// <summary>True once edit mode exists (set by T-44); until then a hub click does nothing and the menu stays open.</summary>
    public bool EditEnabled { get; set; }

    /// <summary>True when the trigger is held to use the menu and released to choose (FR-2).</summary>
    public bool HoldMode { get; set; }

    public MenuCommand Handle(in TriggerEvent e)
    {
        if (e.Kind == TriggerEventKind.TriggerPressed)
        {
            if (State == MenuState.Closed)
            {
                State = MenuState.Main;
                _pressValid = false;
                return MenuCommand.Open;
            }

            return CloseAll();
        }

        if (State == MenuState.Closed)
        {
            return MenuCommand.None;
        }

        switch (e.Kind)
        {
            case TriggerEventKind.MapLost:
                return CloseAll();
            case TriggerEventKind.MouseMove:
                return MenuCommand.None;
            case TriggerEventKind.Escape:
                return Cancel(true);
            case TriggerEventKind.MouseDown:
                return OnDown(e);
            case TriggerEventKind.MouseUp:
                return OnUp(e);
            case TriggerEventKind.TriggerReleased:
                return HoldMode && State != MenuState.Edit && e.Hit.Kind == HitKind.HubEdit ? CloseAll() : MenuCommand.None;
            default:
                return MenuCommand.None;
        }
    }

    private MenuCommand CloseAll()
    {
        if (State == MenuState.Closed)
        {
            return MenuCommand.None;
        }

        var wasEditing = State == MenuState.Edit;
        State = MenuState.Closed;
        _pressValid = false;
        return wasEditing ? MenuCommand.CloseFromEdit : MenuCommand.Close;
    }

    /// <summary>
    /// Esc, right-click and outside clicks: one level out. Esc leaves edit mode first; a right-click or an outside click in
    /// edit mode closes the menu (the controller saves the pins).
    /// </summary>
    private MenuCommand Cancel(bool fromEscape)
    {
        _pressValid = false;
        switch (State)
        {
            case MenuState.Satellite:
                State = MenuState.Main;
                return MenuCommand.Back;
            case MenuState.Edit:
                if (fromEscape)
                {
                    State = MenuState.Main;
                    return MenuCommand.ExitEdit;
                }

                return CloseAll();
            default:
                return CloseAll();
        }
    }

    private MenuCommand OnDown(in TriggerEvent e)
    {
        if (e.Button == 1)
        {
            return Cancel(false);
        }

        if (e.Button == 0)
        {
            _pressValid = true;
            _pressHit = e.Hit;
        }

        return MenuCommand.None;
    }

    private MenuCommand OnUp(in TriggerEvent e)
    {
        if (e.Button != 0 || !_pressValid)
        {
            return MenuCommand.None;
        }

        _pressValid = false;
        if (e.Hit != _pressHit)
        {
            return MenuCommand.None;
        }

        switch (e.Hit.Kind)
        {
            case HitKind.Outside:
                return Cancel(false);
            case HitKind.HubEdit:
                switch (State)
                {
                    case MenuState.Satellite:
                        return Cancel(false);
                    case MenuState.Edit:
                        return Cancel(true);
                    default:
                        if (!EditEnabled)
                        {
                            return MenuCommand.None;
                        }

                        State = MenuState.Edit;
                        return MenuCommand.EnterEdit;
                }

            case HitKind.Slot:
                if (e.Modifier && State != MenuState.Satellite && e.SlotKind != SlotKind.Empty)
                {
                    return MenuCommand.Unpin(e.Hit.Index);
                }

                if (State == MenuState.Edit)
                {
                    return MenuCommand.None;
                }

                switch (e.SlotKind)
                {
                    case SlotKind.Empty:
                    case SlotKind.Reserved:
                        return MenuCommand.None;
                    case SlotKind.Disabled:
                        return MenuCommand.Reject(e.Hit.Index);
                    case SlotKind.Submenu:
                        State = MenuState.Satellite;
                        return MenuCommand.OpenSub(e.Hit.Index);
                }

                var level = State == MenuState.Satellite ? MenuLevel.Satellite : MenuLevel.Main;
                var activate = MenuCommand.Activate(e.Hit.Index, level);
                State = MenuState.Closed;
                return activate;
            default:
                return MenuCommand.None;
        }
    }
}
