using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;

namespace QuickMenu.Core.Input;

/// <summary>
/// Open/close/back/activate decisions (FR-2 press-to-toggle, FR-11..13, design.md S4a). The hub is one target, Edit (Done in edit mode), and a click on it never closes the menu.
/// Left clicks act on mouse-up and only when the press began on the same target. Right-click acts on press.
/// Allocation free.
/// </summary>
public sealed class TriggerStateMachine
{
    private bool _pressValid;
    private Hit _pressHit;
    private int _dragSource = -1;
    private Vec2 _dragStart;
    private bool _dragging;

    /// <summary>Pointer travel in UI px before a press on a slot in edit mode becomes a drag (design.md S5).</summary>
    public const float DragThreshold = 6f;

    /// <summary>The slot being dragged, or -1 when no drag has started (a press under <see cref="DragThreshold"/> is not a drag yet).</summary>
    public int DragSource => _dragging ? _dragSource : -1;

    public MenuState State { get; private set; }

    /// <summary>True when a hub click enters edit mode (T-44; the default). When false a hub click does nothing and the menu stays open.</summary>
    public bool EditEnabled { get; set; } = true;

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
                if (_dragSource >= 0 && !_dragging && State == MenuState.Edit)
                {
                    float dx = e.Position.X - _dragStart.X;
                    float dy = e.Position.Y - _dragStart.Y;
                    _dragging = (dx * dx) + (dy * dy) >= DragThreshold * DragThreshold;
                }

                return MenuCommand.None;
            case TriggerEventKind.PressLost:
                _pressValid = false;
                EndDrag();
                return MenuCommand.None;
            case TriggerEventKind.Escape:
                if (_dragging)
                {
                    EndDrag();
                    return MenuCommand.None;
                }

                return Cancel(true);
            case TriggerEventKind.MouseDown:
                return OnDown(e);
            case TriggerEventKind.MouseUp:
                return OnUp(e);
            default:
                return MenuCommand.None;
        }
    }

    /// <summary>
    /// Returns to the satellite level after a <see cref="MenuCommandKind.Back"/> that only left the material ring of a
    /// sub-menu option (the controller shows the sub-menu again). No effect unless the machine is at the main level.
    /// </summary>
    public void ResumeSatellite()
    {
        if (State == MenuState.Main)
        {
            State = MenuState.Satellite;
            _pressValid = false;
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
        EndDrag();
        return wasEditing ? MenuCommand.CloseFromEdit : MenuCommand.Close;
    }

    /// <summary>
    /// Esc, right-click and outside clicks: one level out. Esc leaves edit mode first; a right-click or an outside click in
    /// edit mode closes the menu (the controller saves the pins).
    /// </summary>
    private MenuCommand Cancel(bool fromEscape)
    {
        _pressValid = false;
        EndDrag();
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
            if (_dragging)
            {
                // A right-click cancels a drag in progress first and never unpins; the next one closes the menu.
                _pressValid = false;
                EndDrag();
                return MenuCommand.None;
            }

            return Cancel(false);
        }

        if (e.Button == 0)
        {
            _pressValid = true;
            _pressHit = e.Hit;
            EndDrag();
            if (State == MenuState.Edit && e.Hit.Kind == HitKind.Slot && e.SlotKind is not SlotKind.Empty)
            {
                _dragSource = e.Hit.Index;
                _dragStart = e.Position;
            }
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
        if (_dragging)
        {
            int from = _dragSource;
            EndDrag();
            return e.Hit.Kind == HitKind.Slot && e.Hit.Index != from ? MenuCommand.Swap(from, e.Hit.Index) : MenuCommand.None;
        }

        EndDrag();
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

    private void EndDrag()
    {
        _dragSource = -1;
        _dragging = false;
    }
}
