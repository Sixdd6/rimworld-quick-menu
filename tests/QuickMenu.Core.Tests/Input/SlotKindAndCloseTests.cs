using QuickMenu.Core.Geometry;
using QuickMenu.Core.Input;
using QuickMenu.Core.Layout;
using Xunit;

namespace QuickMenu.Core.Tests.Input;

public class SlotKindAndCloseTests
{
    private static readonly Vec2 _p = new(10, 10);

    private static TriggerStateMachine Opened()
    {
        var m = new TriggerStateMachine { EditEnabled = true };
        m.Handle(TriggerEvent.TriggerPressed(_p));
        return m;
    }

    private static MenuCommand Click(TriggerStateMachine m, Hit h, SlotKind kind)
    {
        m.Handle(TriggerEvent.MouseDown(_p, h, 0));
        return m.Handle(TriggerEvent.MouseUp(_p, h, kind, 0));
    }

    [Fact]
    public void Empty_slot_does_nothing_and_menu_stays_open()
    {
        var m = Opened();
        Assert.Equal(MenuCommand.None, Click(m, Hit.Slot(3), SlotKind.Empty));
        Assert.Equal(MenuState.Main, m.State);
    }

    [Fact]
    public void Disabled_slot_rejects_and_menu_stays_open()
    {
        var m = Opened();
        var c = Click(m, Hit.Slot(4), SlotKind.Disabled);
        Assert.Equal(MenuCommand.Reject(4), c);
        Assert.Equal(MenuCommandKind.Reject, c.Kind);
        Assert.Equal(MenuState.Main, m.State);
        Assert.Equal(MenuCommand.Activate(1), Click(m, Hit.Slot(1), SlotKind.Normal));
    }

    [Fact]
    public void Disabled_slot_in_satellite_stays_in_satellite()
    {
        var m = Opened();
        Click(m, Hit.Slot(2), SlotKind.Submenu);
        Assert.Equal(MenuCommand.Reject(0), Click(m, Hit.Slot(0), SlotKind.Disabled));
        Assert.Equal(MenuState.Satellite, m.State);
    }

    [Fact]
    public void Activate_carries_the_level()
    {
        var m = Opened();
        var main = Click(m, Hit.Slot(1), SlotKind.Normal);
        Assert.Equal(MenuLevel.Main, main.Level);

        m = Opened();
        Click(m, Hit.Slot(2), SlotKind.Submenu);
        var sat = Click(m, Hit.Slot(0), SlotKind.Normal);
        Assert.Equal(MenuCommandKind.Activate, sat.Kind);
        Assert.Equal(MenuLevel.Satellite, sat.Level);
        Assert.NotEqual(main, sat);
    }

    private static TriggerStateMachine Editing()
    {
        var m = Opened();
        Click(m, Hit.HubEdit, SlotKind.Normal);
        Assert.Equal(MenuState.Edit, m.State);
        return m;
    }

    [Fact]
    public void Trigger_press_in_edit_closes_with_was_editing()
    {
        var c = Editing().Handle(TriggerEvent.TriggerPressed(_p));
        Assert.Equal(MenuCommandKind.Close, c.Kind);
        Assert.True(c.WasEditing);
    }

    [Fact]
    public void Map_lost_in_edit_closes_with_was_editing()
    {
        var c = Editing().Handle(TriggerEvent.MapLost());
        Assert.Equal(MenuCommandKind.Close, c.Kind);
        Assert.True(c.WasEditing);
    }

    [Fact]
    public void Close_outside_edit_is_not_was_editing()
    {
        Assert.False(Opened().Handle(TriggerEvent.TriggerPressed(_p)).WasEditing);
        Assert.False(Opened().Handle(TriggerEvent.MapLost()).WasEditing);
    }
}
