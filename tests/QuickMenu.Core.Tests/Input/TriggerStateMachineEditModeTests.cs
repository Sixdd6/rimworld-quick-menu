using QuickMenu.Core.Geometry;
using QuickMenu.Core.Input;
using QuickMenu.Core.Layout;
using Xunit;

namespace QuickMenu.Core.Tests.Input;

/// <summary>T-44: edit mode shell. Enter and leave by the hub (Done) or Esc, plain slot clicks do nothing, modifier+click unpins.</summary>
public class TriggerStateMachineEditModeTests
{
    private static readonly Vec2 _p = new(40, 40);

    private static TriggerStateMachine Editing()
    {
        var m = new TriggerStateMachine();
        m.Handle(TriggerEvent.TriggerPressed(_p));
        Assert.Equal(MenuCommand.EnterEdit, Click(m, Hit.HubEdit));
        Assert.Equal(MenuState.Edit, m.State);
        return m;
    }

    private static MenuCommand Click(TriggerStateMachine m, Hit hit, SlotKind kind = SlotKind.Normal, bool modifier = false)
    {
        m.Handle(TriggerEvent.MouseDown(_p, hit, 0));
        return m.Handle(TriggerEvent.MouseUp(_p, hit, kind, 0, modifier));
    }

    [Fact]
    public void Edit_is_on_by_default_and_the_hub_enters_it()
    {
        var m = new TriggerStateMachine();
        m.Handle(TriggerEvent.TriggerPressed(_p));
        Assert.Equal(MenuCommand.EnterEdit, Click(m, Hit.HubEdit));
    }

    [Fact]
    public void Done_leaves_edit_mode_and_keeps_the_menu_open()
    {
        var m = Editing();
        Assert.Equal(MenuCommand.ExitEdit, Click(m, Hit.HubEdit));
        Assert.Equal(MenuState.Main, m.State);
    }

    [Fact]
    public void First_Esc_leaves_edit_second_closes()
    {
        var m = Editing();
        Assert.Equal(MenuCommand.ExitEdit, m.Handle(TriggerEvent.Escape(_p)));
        Assert.Equal(MenuState.Main, m.State);
        Assert.Equal(MenuCommand.Close, m.Handle(TriggerEvent.Escape(_p)));
    }

    [Theory]
    [InlineData(SlotKind.Normal)]
    [InlineData(SlotKind.Submenu)]
    [InlineData(SlotKind.Disabled)]
    [InlineData(SlotKind.Empty)]
    [InlineData(SlotKind.Reserved)]
    public void Plain_click_on_any_slot_does_nothing(SlotKind kind)
    {
        var m = Editing();
        Assert.Equal(MenuCommand.None, Click(m, Hit.Slot(2), kind));
        Assert.Equal(MenuState.Edit, m.State);
    }

    [Theory]
    [InlineData(SlotKind.Normal)]
    [InlineData(SlotKind.Submenu)]
    [InlineData(SlotKind.Disabled)]
    [InlineData(SlotKind.Reserved)]
    public void Modifier_click_unpins_occupied_and_reserved_slots_and_stays_in_edit(SlotKind kind)
    {
        var m = Editing();
        Assert.Equal(MenuCommand.Unpin(4), Click(m, Hit.Slot(4), kind, modifier: true));
        Assert.Equal(MenuState.Edit, m.State);
    }

    [Fact]
    public void Modifier_click_on_an_empty_slot_or_the_hub_does_nothing_extra()
    {
        var m = Editing();
        Assert.Equal(MenuCommand.None, Click(m, Hit.Slot(4), SlotKind.Empty, modifier: true));
        Assert.Equal(MenuCommand.ExitEdit, Click(m, Hit.HubEdit, modifier: true));
    }

    [Fact]
    public void Right_click_closes_from_edit_and_never_unpins()
    {
        var m = Editing();
        Assert.Equal(MenuCommand.CloseFromEdit, m.Handle(TriggerEvent.MouseDown(_p, Hit.Slot(1), 1)));
        Assert.Equal(MenuState.Closed, m.State);
    }

    [Fact]
    public void Click_outside_and_the_trigger_close_from_edit()
    {
        var m = Editing();
        Assert.Equal(MenuCommand.CloseFromEdit, Click(m, Hit.Outside));
        m = Editing();
        Assert.Equal(MenuCommand.CloseFromEdit, m.Handle(TriggerEvent.TriggerPressed(_p)));
    }

    [Fact]
    public void Hold_release_never_enters_edit_and_does_not_close_in_edit()
    {
        var m = new TriggerStateMachine { HoldMode = true };
        m.Handle(TriggerEvent.TriggerPressed(_p));
        Assert.Equal(MenuCommand.Close, m.Handle(TriggerEvent.TriggerReleased(_p, Hit.HubEdit)));
    }
}
