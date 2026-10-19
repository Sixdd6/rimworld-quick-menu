using System;
using QuickMenu.Core.Geometry;
using QuickMenu.Core.Input;
using QuickMenu.Core.Layout;
using Xunit;

namespace QuickMenu.Core.Tests.Input;

public class TriggerStateMachineToggleTests
{
    private static readonly Vec2 _origin = new(100, 100);

    private static TriggerStateMachine Opened()
    {
        var m = new TriggerStateMachine { EditEnabled = true };
        Assert.Equal(MenuCommand.Open, m.Handle(TriggerEvent.TriggerPressed(_origin)));
        return m;
    }

    private static MenuCommand Click(TriggerStateMachine m, Hit hit, bool sub = false, int button = 0)
    {
        var down = m.Handle(TriggerEvent.MouseDown(_origin, hit, button));
        if (button != 0)
        {
            return down;
        }

        Assert.Equal(MenuCommand.None, down);
        return m.Handle(TriggerEvent.MouseUp(_origin, hit, sub ? SlotKind.Submenu : SlotKind.Normal, button));
    }

    private static TriggerStateMachine Satellite()
    {
        var m = Opened();
        Assert.Equal(MenuCommand.OpenSub(2), Click(m, Hit.Slot(2), sub: true));
        return m;
    }

    private static TriggerStateMachine Edit()
    {
        var m = Opened();
        Assert.Equal(MenuCommand.EnterEdit, Click(m, Hit.HubEdit));
        return m;
    }

    [Fact]
    public void Closed_trigger_press_opens_and_second_press_closes()
    {
        var m = new TriggerStateMachine();
        Assert.Equal(MenuState.Closed, m.State);
        Assert.Equal(MenuCommand.Open, m.Handle(TriggerEvent.TriggerPressed(_origin)));
        Assert.Equal(MenuState.Main, m.State);
        Assert.Equal(MenuCommand.Close, m.Handle(TriggerEvent.TriggerPressed(_origin)));
        Assert.Equal(MenuState.Closed, m.State);
    }

    [Fact]
    public void Closed_ignores_everything_else()
    {
        var m = new TriggerStateMachine();
        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.Escape(_origin)));
        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.MouseDown(_origin, Hit.Outside, 0)));
        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.MouseUp(_origin, Hit.Outside, SlotKind.Normal, 0)));
        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.MouseDown(_origin, Hit.Outside, 1)));
        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.TriggerReleased(_origin)));
        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.MapLost()));
    }

    [Fact]
    public void Main_escape_closes()
    {
        var m = Opened();
        Assert.Equal(MenuCommand.Close, m.Handle(TriggerEvent.Escape(_origin)));
        Assert.Equal(MenuState.Closed, m.State);
    }

    [Fact]
    public void Main_right_click_closes_even_over_a_slot()
    {
        var m = Opened();
        Assert.Equal(MenuCommand.Close, Click(m, Hit.Slot(1), button: 1));
        Assert.Equal(MenuState.Closed, m.State);
    }

    [Fact]
    public void Main_left_click_outside_closes()
    {
        var m = Opened();
        Assert.Equal(MenuCommand.Close, Click(m, Hit.Outside));
        Assert.Equal(MenuState.Closed, m.State);
    }

    [Fact]
    public void Main_click_on_hub_never_closes_and_does_nothing_while_edit_is_disabled()
    {
        var m = new TriggerStateMachine { EditEnabled = false };
        m.Handle(TriggerEvent.TriggerPressed(_origin));
        Assert.Equal(MenuCommand.None, Click(m, Hit.HubEdit));
        Assert.Equal(MenuState.Main, m.State);
        Assert.Equal(MenuCommand.None, Click(m, Hit.HubEdit));
        m.EditEnabled = true;
        Assert.Equal(MenuCommand.EnterEdit, Click(m, Hit.HubEdit));
    }

    [Fact]
    public void Main_click_on_hub_edit_enters_edit()
    {
        var m = Opened();
        Assert.Equal(MenuCommand.EnterEdit, Click(m, Hit.HubEdit));
        Assert.Equal(MenuState.Edit, m.State);
    }

    [Fact]
    public void Hub_edit_needs_press_and_release_on_it()
    {
        var m = Opened();
        m.Handle(TriggerEvent.MouseDown(_origin, Hit.Slot(0), 0));
        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.MouseUp(_origin, Hit.HubEdit, SlotKind.Normal, 0)));
        Assert.Equal(MenuState.Main, m.State);

        m.Handle(TriggerEvent.MouseDown(_origin, Hit.HubEdit, 0));
        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.MouseUp(_origin, Hit.Slot(0), SlotKind.Normal, 0)));
        Assert.Equal(MenuState.Main, m.State);
    }

    [Fact]
    public void Release_without_a_press_in_this_menu_does_nothing()
    {
        var m = Opened();
        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.MouseUp(_origin, Hit.HubEdit, SlotKind.Normal, 0)));
        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.MouseUp(_origin, Hit.Outside, SlotKind.Normal, 0)));
        Assert.Equal(MenuState.Main, m.State);
    }

    [Fact]
    public void Press_before_the_menu_opened_is_not_a_click()
    {
        var m = new TriggerStateMachine();
        m.Handle(TriggerEvent.MouseDown(_origin, Hit.Slot(0), 0));
        m.Handle(TriggerEvent.TriggerPressed(_origin));
        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.MouseUp(_origin, Hit.Slot(0), SlotKind.Normal, 0)));
    }

    [Fact]
    public void Slot_click_activates_and_closes()
    {
        var m = Opened();
        Assert.Equal(MenuCommand.Activate(3), Click(m, Hit.Slot(3)));
        Assert.Equal(MenuState.Closed, m.State);
    }

    [Fact]
    public void Slot_press_released_on_another_target_does_nothing()
    {
        var m = Opened();
        m.Handle(TriggerEvent.MouseDown(_origin, Hit.Slot(3), 0));
        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.MouseUp(_origin, Hit.Slot(4), SlotKind.Normal, 0)));
        Assert.Equal(MenuState.Main, m.State);
    }

    [Fact]
    public void Submenu_slot_click_opens_satellite()
    {
        var m = Satellite();
        Assert.Equal(MenuState.Satellite, m.State);
    }

    [Fact]
    public void Inside_plate_and_buttons_do_nothing_on_main()
    {
        var m = Opened();
        Assert.Equal(MenuCommand.None, Click(m, Hit.Inside));
        Assert.Equal(MenuCommand.None, Click(m, Hit.Plate));
        Assert.Equal(MenuCommand.None, Click(m, Hit.Button(1)));
        Assert.Equal(MenuState.Main, m.State);
    }

    [Fact]
    public void Satellite_escape_goes_back()
    {
        var m = Satellite();
        Assert.Equal(MenuCommand.Back, m.Handle(TriggerEvent.Escape(_origin)));
        Assert.Equal(MenuState.Main, m.State);
    }

    [Fact]
    public void Satellite_right_click_goes_back()
    {
        var m = Satellite();
        Assert.Equal(MenuCommand.Back, Click(m, Hit.Slot(0), button: 1));
        Assert.Equal(MenuState.Main, m.State);
    }

    [Fact]
    public void Satellite_click_outside_goes_back()
    {
        var m = Satellite();
        Assert.Equal(MenuCommand.Back, Click(m, Hit.Outside));
        Assert.Equal(MenuState.Main, m.State);
    }

    [Fact]
    public void Satellite_click_on_hub_goes_back()
    {
        var m = Satellite();
        Assert.Equal(MenuCommand.Back, Click(m, Hit.HubEdit));
        Assert.Equal(MenuState.Main, m.State);
    }

    [Fact]
    public void Satellite_slot_click_activates_and_closes()
    {
        var m = Satellite();
        Assert.Equal(MenuCommand.Activate(1, MenuLevel.Satellite), Click(m, Hit.Slot(1)));
        Assert.Equal(MenuState.Closed, m.State);
    }

    [Fact]
    public void Satellite_trigger_press_closes()
    {
        var m = Satellite();
        Assert.Equal(MenuCommand.Close, m.Handle(TriggerEvent.TriggerPressed(_origin)));
        Assert.Equal(MenuState.Closed, m.State);
    }

    [Fact]
    public void Edit_escape_exits_edit_then_second_escape_closes()
    {
        var m = Edit();
        Assert.Equal(MenuCommand.ExitEdit, m.Handle(TriggerEvent.Escape(_origin)));
        Assert.Equal(MenuState.Main, m.State);
        Assert.Equal(MenuCommand.Close, m.Handle(TriggerEvent.Escape(_origin)));
    }

    [Fact]
    public void Edit_done_exits_edit()
    {
        var m = Edit();
        Assert.Equal(MenuCommand.ExitEdit, Click(m, Hit.HubEdit));
        Assert.Equal(MenuState.Main, m.State);
    }

    [Fact]
    public void Edit_click_outside_closes()
    {
        var m = Edit();
        Assert.Equal(MenuCommand.CloseFromEdit, Click(m, Hit.Outside));
        Assert.Equal(MenuState.Closed, m.State);
    }

    [Theory]
    [InlineData(HitKind.HubEdit)]
    [InlineData(HitKind.Slot)]
    [InlineData(HitKind.Outside)]
    public void Edit_right_click_closes_wherever_it_is(HitKind kind)
    {
        var m = Edit();
        Assert.Equal(MenuCommand.CloseFromEdit, Click(m, new Hit(kind, kind == HitKind.Slot ? 0 : -1), button: 1));
        Assert.Equal(MenuState.Closed, m.State);
    }

    [Fact]
    public void Edit_slot_clicks_are_left_to_the_caller()
    {
        var m = Edit();
        Assert.Equal(MenuCommand.None, Click(m, Hit.Slot(0)));
        Assert.Equal(MenuState.Edit, m.State);
    }

    [Fact]
    public void Edit_trigger_press_closes()
    {
        var m = Edit();
        Assert.Equal(MenuCommand.CloseFromEdit, m.Handle(TriggerEvent.TriggerPressed(_origin)));
    }

    [Fact]
    public void Trigger_release_is_ignored_in_toggle_mode()
    {
        var m = Opened();
        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.TriggerReleased(_origin)));
        Assert.Equal(MenuState.Main, m.State);
    }

    [Fact]
    public void Map_lost_closes_in_every_open_state()
    {
        foreach (var make in new Func<TriggerStateMachine>[] { Opened, Satellite, Edit })
        {
            var m = make();
            var expected = m.State == MenuState.Edit ? MenuCommand.CloseFromEdit : MenuCommand.Close;
            Assert.Equal(expected, m.Handle(TriggerEvent.MapLost()));
            Assert.Equal(MenuState.Closed, m.State);
        }
    }

    [Fact]
    public void Reopen_after_close_starts_clean()
    {
        var m = Edit();
        m.Handle(TriggerEvent.MapLost());
        m.Handle(TriggerEvent.TriggerPressed(_origin));
        Assert.Equal(MenuState.Main, m.State);
        Assert.Equal(MenuCommand.Close, Click(m, Hit.Outside));
    }

    [Fact]
    public void Mouse_move_never_produces_a_command()
    {
        var m = Opened();
        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.MouseMove(new Vec2(300, 300))));
    }
}
