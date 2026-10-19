using QuickMenu.Core.Geometry;
using QuickMenu.Core.Input;
using QuickMenu.Core.Layout;
using Xunit;

namespace QuickMenu.Core.Tests.Input;

public class TriggerStateMachineSequenceTests
{
    private static readonly Vec2 _p = new(50, 50);

    private static TriggerStateMachine Opened()
    {
        var m = new TriggerStateMachine();
        m.Handle(TriggerEvent.TriggerPressed(_p));
        return m;
    }

    private static MenuCommand Down(TriggerStateMachine m, Hit h, int button = 0) =>
        m.Handle(TriggerEvent.MouseDown(_p, h, button));

    private static MenuCommand Up(TriggerStateMachine m, Hit h, bool sub = false, int button = 0) =>
        m.Handle(TriggerEvent.MouseUp(_p, h, sub ? SlotKind.Submenu : SlotKind.Normal, button));

    [Fact]
    public void Press_on_slot_A_release_on_slot_B_does_nothing_and_a_later_bare_release_does_nothing()
    {
        var m = Opened();
        Assert.Equal(MenuCommand.None, Down(m, Hit.Slot(1)));
        Assert.Equal(MenuCommand.None, Up(m, Hit.Slot(2)));
        Assert.Equal(MenuState.Main, m.State);
        Assert.Equal(MenuCommand.None, Up(m, Hit.Slot(2)));
        Assert.Equal(MenuCommand.Activate(2), ClickFull(m, Hit.Slot(2)));
    }

    private static MenuCommand ClickFull(TriggerStateMachine m, Hit h, bool sub = false)
    {
        Down(m, h);
        return Up(m, h, sub);
    }

    [Fact]
    public void Press_on_hub_edit_release_on_close_does_not_enter_edit_or_close()
    {
        var m = Opened();
        Down(m, Hit.HubEdit);
        Assert.Equal(MenuCommand.None, Up(m, Hit.HubClose));
        Assert.Equal(MenuState.Main, m.State);
    }

    [Fact]
    public void Press_outside_release_on_slot_does_not_activate()
    {
        var m = Opened();
        Down(m, Hit.Outside);
        Assert.Equal(MenuCommand.None, Up(m, Hit.Slot(0)));
        Assert.Equal(MenuState.Main, m.State);
    }

    [Fact]
    public void Press_on_slot_release_outside_does_not_close()
    {
        var m = Opened();
        Down(m, Hit.Slot(0));
        Assert.Equal(MenuCommand.None, Up(m, Hit.Outside));
        Assert.Equal(MenuState.Main, m.State);
    }

    [Fact]
    public void Same_slot_index_with_different_kind_is_a_different_target()
    {
        var m = Opened();
        Down(m, Hit.Slot(3));
        Assert.Equal(MenuCommand.None, Up(m, Hit.Button(3)));
    }

    [Fact]
    public void Esc_in_satellite_goes_back_then_esc_in_main_closes_then_everything_is_ignored()
    {
        var m = Opened();
        Assert.Equal(MenuCommand.OpenSub(4), ClickFull(m, Hit.Slot(4), sub: true));
        Assert.Equal(MenuCommand.Back, m.Handle(TriggerEvent.Escape(_p)));
        Assert.Equal(MenuState.Main, m.State);
        Assert.Equal(MenuCommand.Close, m.Handle(TriggerEvent.Escape(_p)));
        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.Escape(_p)));
    }

    [Fact]
    public void Satellite_press_pending_is_dropped_when_escape_goes_back()
    {
        var m = Opened();
        ClickFull(m, Hit.Slot(4), sub: true);
        Down(m, Hit.Slot(1));
        Assert.Equal(MenuCommand.Back, m.Handle(TriggerEvent.Escape(_p)));
        Assert.Equal(MenuCommand.None, Up(m, Hit.Slot(1)));
        Assert.Equal(MenuState.Main, m.State);
    }

    [Fact]
    public void Map_lost_in_edit_closes_and_reopening_starts_in_main_not_edit()
    {
        var m = Opened();
        Assert.Equal(MenuCommand.EnterEdit, ClickFull(m, Hit.HubEdit));
        Assert.Equal(MenuCommand.CloseFromEdit, m.Handle(TriggerEvent.MapLost()));
        Assert.Equal(MenuState.Closed, m.State);
        Assert.Equal(MenuCommand.Open, m.Handle(TriggerEvent.TriggerPressed(_p)));
        Assert.Equal(MenuState.Main, m.State);
        // Escape now closes (not ExitEdit).
        Assert.Equal(MenuCommand.Close, m.Handle(TriggerEvent.Escape(_p)));
    }

    [Fact]
    public void Map_lost_with_a_pending_press_does_not_leak_into_the_next_open()
    {
        var m = Opened();
        Down(m, Hit.Slot(2));
        m.Handle(TriggerEvent.MapLost());
        m.Handle(TriggerEvent.TriggerPressed(_p));
        Assert.Equal(MenuCommand.None, Up(m, Hit.Slot(2)));
        Assert.Equal(MenuState.Main, m.State);
    }

    [Fact]
    public void Map_lost_when_closed_is_none()
    {
        var m = new TriggerStateMachine();
        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.MapLost()));
    }

    [Fact]
    public void Edit_done_via_hub_then_slot_click_activates_again()
    {
        var m = Opened();
        ClickFull(m, Hit.HubEdit);
        Assert.Equal(MenuCommand.ExitEdit, ClickFull(m, Hit.HubClose));
        Assert.Equal(MenuState.Main, m.State);
        Assert.Equal(MenuCommand.Activate(0), ClickFull(m, Hit.Slot(0)));
        Assert.Equal(MenuState.Closed, m.State);
    }

    [Fact]
    public void Edit_outside_click_then_escape_still_exits_edit_first()
    {
        var m = Opened();
        ClickFull(m, Hit.HubEdit);
        Assert.Equal(MenuCommand.None, ClickFull(m, Hit.Outside));
        Assert.Equal(MenuCommand.ExitEdit, m.Handle(TriggerEvent.Escape(_p)));
        Assert.Equal(MenuCommand.Close, m.Handle(TriggerEvent.Escape(_p)));
    }

    [Fact]
    public void Right_button_up_and_middle_button_never_act()
    {
        var m = Opened();
        Assert.Equal(MenuCommand.None, Up(m, Hit.Outside, false, 1));
        Assert.Equal(MenuCommand.None, Down(m, Hit.Outside, 2));
        Assert.Equal(MenuCommand.None, Up(m, Hit.Outside, false, 2));
        Assert.Equal(MenuState.Main, m.State);
    }

    [Fact]
    public void Satellite_to_activate_from_satellite_slot_then_reopen_is_main()
    {
        var m = Opened();
        ClickFull(m, Hit.Slot(0), sub: true);
        Assert.Equal(MenuCommand.Activate(7, MenuLevel.Satellite), ClickFull(m, Hit.Slot(7)));
        m.Handle(TriggerEvent.TriggerPressed(_p));
        Assert.Equal(MenuState.Main, m.State);
    }

    [Fact]
    public void Satellite_slot_that_is_a_submenu_stays_in_satellite()
    {
        var m = Opened();
        ClickFull(m, Hit.Slot(0), sub: true);
        Assert.Equal(MenuCommand.OpenSub(3), ClickFull(m, Hit.Slot(3), sub: true));
        Assert.Equal(MenuState.Satellite, m.State);
        Assert.Equal(MenuCommand.Back, m.Handle(TriggerEvent.Escape(_p)));
        Assert.Equal(MenuState.Main, m.State);
    }
}
