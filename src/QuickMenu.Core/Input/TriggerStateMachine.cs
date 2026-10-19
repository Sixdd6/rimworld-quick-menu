using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;

namespace QuickMenu.Core.Input;

/// <summary>
/// Open/close/back/activate decisions (FR-2, FR-11..13, design.md S4a). Toggle mode only for now: hold mode
/// will use <see cref="TriggerEventKind.TriggerReleased"/> with a mode setting, without changing this API.
/// Left clicks act on mouse-up and only when the press began on the same target. Right-click acts on press.
/// Allocation free.
/// </summary>
public sealed class TriggerStateMachine
{
    /// <summary>Pointer travel (UI px) after opening before the hub may highlight.</summary>
    public const float HoverMoveThreshold = 4f;

    private Vec2 _openPosition;
    private bool _pressValid;
    private Hit _pressHit;

    public MenuState State { get; private set; }

    /// <summary>False until the pointer has moved <see cref="HoverMoveThreshold"/> px since opening.</summary>
    public bool HoverEnabled { get; private set; }

    public MenuCommand Handle(in TriggerEvent e)
    {
        if (e.Kind == TriggerEventKind.TriggerPressed)
        {
            if (State == MenuState.Closed)
            {
                State = MenuState.Main;
                _openPosition = e.Position;
                HoverEnabled = false;
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
                TrackHover(e.Position);
                return MenuCommand.None;
            case TriggerEventKind.Escape:
                return Cancel(true);
            case TriggerEventKind.MouseDown:
                TrackHover(e.Position);
                return OnDown(e);
            case TriggerEventKind.MouseUp:
                TrackHover(e.Position);
                return OnUp(e);
            default:
                return MenuCommand.None;
        }
    }

    private void TrackHover(Vec2 p)
    {
        if (!HoverEnabled && p.Distance(_openPosition) >= HoverMoveThreshold)
        {
            HoverEnabled = true;
        }
    }

    private MenuCommand CloseAll()
    {
        if (State == MenuState.Closed)
        {
            return MenuCommand.None;
        }

        State = MenuState.Closed;
        _pressValid = false;
        HoverEnabled = false;
        return MenuCommand.Close;
    }

    /// <summary>Esc, right-click and outside clicks: one level out (Back, ExitEdit or Close).</summary>
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

                return MenuCommand.None;
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
                return State == MenuState.Edit ? MenuCommand.None : Cancel(false);
            case HitKind.HubClose:
                return State switch
                {
                    MenuState.Satellite => Cancel(false),
                    MenuState.Edit => Cancel(true),
                    _ => CloseAll(),
                };
            case HitKind.HubEdit:
                if (State == MenuState.Satellite)
                {
                    return Cancel(false);
                }

                if (State == MenuState.Main)
                {
                    State = MenuState.Edit;
                    return MenuCommand.EnterEdit;
                }

                return MenuCommand.None;
            case HitKind.Slot:
                if (State == MenuState.Edit)
                {
                    return MenuCommand.None;
                }

                if (e.SlotIsSubmenu)
                {
                    State = MenuState.Satellite;
                    return MenuCommand.OpenSub(e.Hit.Index);
                }

                var activate = MenuCommand.Activate(e.Hit.Index);
                State = MenuState.Closed;
                HoverEnabled = false;
                return activate;
            default:
                return MenuCommand.None;
        }
    }
}
