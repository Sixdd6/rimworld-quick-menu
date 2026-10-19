using QuickMenu.Core.Geometry;
using QuickMenu.Core.Input;
using QuickMenu.Core.Layout;
using Xunit;

namespace QuickMenu.Core.Tests.Input;

/// <summary>M2 review: inputs that must be ignored.</summary>
public class TriggerStateMachineClosedAndUnknownTests
{
    private static readonly Vec2 _p = new(10, 10);

    [Fact]
    public void Unknown_event_kind_while_open_is_ignored_and_the_menu_stays_open()
    {
        var m = new TriggerStateMachine();
        m.Handle(TriggerEvent.TriggerPressed(_p));
        var odd = new TriggerEvent((TriggerEventKind)99, _p, Hit.Outside, 0, SlotKind.Normal);

        Assert.Equal(MenuCommand.None, m.Handle(odd));
        Assert.Equal(MenuState.Main, m.State);
    }

    [Fact]
    public void Map_lost_and_release_while_closed_do_nothing()
    {
        var m = new TriggerStateMachine { HoldMode = true };

        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.MapLost()));
        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.TriggerReleased(_p, Hit.HubEdit)));
        Assert.Equal(MenuState.Closed, m.State);
    }

    [Fact]
    public void Hold_release_off_the_hub_keeps_the_menu_open()
    {
        var m = new TriggerStateMachine { HoldMode = true };
        m.Handle(TriggerEvent.TriggerPressed(_p));

        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.TriggerReleased(_p, Hit.Outside)));
        Assert.Equal(MenuState.Main, m.State);
    }
}
