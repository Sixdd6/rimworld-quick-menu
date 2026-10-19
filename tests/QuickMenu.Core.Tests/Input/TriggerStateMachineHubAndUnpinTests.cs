using QuickMenu.Core.Geometry;
using QuickMenu.Core.Input;
using QuickMenu.Core.Layout;
using Xunit;

namespace QuickMenu.Core.Tests.Input;

/// <summary>T-81: the hub is one Edit target that never closes the menu; pin-modifier+click unpins; hold release over the hub cancels.</summary>
public class TriggerStateMachineHubAndUnpinTests
{
    private static readonly Vec2 _p = new(40, 40);

    private static TriggerStateMachine Opened(bool hold = false, bool edit = true)
    {
        var m = new TriggerStateMachine { EditEnabled = edit, HoldMode = hold };
        Assert.Equal(MenuCommand.Open, m.Handle(TriggerEvent.TriggerPressed(_p)));
        return m;
    }

    private static MenuCommand Click(TriggerStateMachine m, Hit hit, SlotKind kind = SlotKind.Normal, bool modifier = false)
    {
        m.Handle(TriggerEvent.MouseDown(_p, hit, 0));
        return m.Handle(TriggerEvent.MouseUp(_p, hit, kind, 0, modifier));
    }

    [Fact]
    public void Edit_is_enabled_and_hold_is_off_by_default()
    {
        Assert.True(new TriggerStateMachine().EditEnabled);
        Assert.False(new TriggerStateMachine().HoldMode);
    }

    [Theory]
    [InlineData(SlotKind.Normal)]
    [InlineData(SlotKind.Submenu)]
    [InlineData(SlotKind.Disabled)]
    public void Modifier_click_on_an_occupied_slot_unpins_it_and_the_menu_stays_open(SlotKind kind)
    {
        var m = Opened();
        Assert.Equal(MenuCommand.Unpin(3), Click(m, Hit.Slot(3), kind, modifier: true));
        Assert.Equal(MenuState.Main, m.State);
        Assert.Equal(MenuCommand.Activate(3), Click(m, Hit.Slot(3)));
    }

    [Fact]
    public void Modifier_click_on_an_empty_slot_does_nothing()
    {
        var m = Opened();
        Assert.Equal(MenuCommand.None, Click(m, Hit.Slot(3), SlotKind.Empty, modifier: true));
        Assert.Equal(MenuState.Main, m.State);
    }

    [Fact]
    public void Modifier_click_on_a_reserved_slot_unpins_it_and_a_plain_click_does_nothing()
    {
        var m = Opened();
        Assert.Equal(MenuCommand.None, Click(m, Hit.Slot(2), SlotKind.Reserved));
        Assert.Equal(MenuState.Main, m.State);
        Assert.Equal(MenuCommand.Unpin(2), Click(m, Hit.Slot(2), SlotKind.Reserved, modifier: true));
        Assert.Equal(MenuState.Main, m.State);
    }

    [Fact]
    public void Modifier_click_on_a_reserved_slot_in_edit_mode_unpins_it()
    {
        var m = Opened();
        Click(m, Hit.HubEdit);
        Assert.Equal(MenuCommand.Unpin(1), Click(m, Hit.Slot(1), SlotKind.Reserved, modifier: true));
        Assert.Equal(MenuState.Edit, m.State);
    }

    [Fact]
    public void Modifier_click_needs_press_and_release_on_the_same_slot()
    {
        var m = Opened();
        m.Handle(TriggerEvent.MouseDown(_p, Hit.Slot(1), 0));
        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.MouseUp(_p, Hit.Slot(2), SlotKind.Normal, 0, true)));
    }

    [Fact]
    public void Modifier_click_in_edit_mode_also_unpins()
    {
        var m = Opened();
        Click(m, Hit.HubEdit);
        Assert.Equal(MenuState.Edit, m.State);
        Assert.Equal(MenuCommand.Unpin(0), Click(m, Hit.Slot(0), SlotKind.Normal, modifier: true));
        Assert.Equal(MenuState.Edit, m.State);
        Assert.Equal(MenuCommand.None, Click(m, Hit.Slot(0), SlotKind.Empty, modifier: true));
    }

    [Fact]
    public void Modifier_click_in_a_satellite_is_a_plain_click()
    {
        var m = Opened();
        Click(m, Hit.Slot(2), SlotKind.Submenu);
        Assert.Equal(MenuState.Satellite, m.State);
        Assert.Equal(MenuCommand.Activate(1, MenuLevel.Satellite), Click(m, Hit.Slot(1), SlotKind.Normal, modifier: true));
    }

    [Fact]
    public void Modifier_click_elsewhere_behaves_like_a_plain_click()
    {
        var m = Opened(edit: false);
        Assert.Equal(MenuCommand.None, Click(m, Hit.HubEdit, modifier: true));
        Assert.Equal(MenuCommand.Close, Click(m, Hit.Outside, modifier: true));
    }

    [Fact]
    public void Modifier_click_on_a_disabled_slot_unpins_instead_of_rejecting()
    {
        var m = Opened();
        Assert.Equal(MenuCommand.Unpin(5), Click(m, Hit.Slot(5), SlotKind.Disabled, modifier: true));
        Assert.Equal(MenuCommand.Reject(5), Click(m, Hit.Slot(5), SlotKind.Disabled));
    }

    [Fact]
    public void Hold_release_over_the_hub_cancels_and_never_enters_edit()
    {
        var m = Opened(hold: true);
        Assert.Equal(MenuCommand.Close, m.Handle(TriggerEvent.TriggerReleased(_p, Hit.HubEdit)));
        Assert.Equal(MenuState.Closed, m.State);
    }

    [Fact]
    public void Hold_release_over_the_hub_closes_everything_with_a_satellite_open()
    {
        var m = Opened(hold: true);
        Click(m, Hit.Slot(2), SlotKind.Submenu);
        Assert.Equal(MenuCommand.Close, m.Handle(TriggerEvent.TriggerReleased(_p, Hit.HubEdit)));
        Assert.Equal(MenuState.Closed, m.State);
    }

    [Fact]
    public void Hold_release_in_edit_mode_stays_open_and_toggle_mode_ignores_releases()
    {
        var m = Opened(hold: true);
        Click(m, Hit.HubEdit);
        Assert.Equal(MenuState.Edit, m.State);
        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.TriggerReleased(_p, Hit.HubEdit)));
        Assert.Equal(MenuState.Edit, m.State);

        var toggle = Opened(hold: false);
        Assert.Equal(MenuCommand.None, toggle.Handle(TriggerEvent.TriggerReleased(_p, Hit.HubEdit)));
        Assert.Equal(MenuState.Main, toggle.State);
    }

    [Fact]
    public void Hub_click_while_holding_still_enters_edit()
    {
        var m = Opened(hold: true);
        Assert.Equal(MenuCommand.EnterEdit, Click(m, Hit.HubEdit));
    }
}
