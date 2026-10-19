using QuickMenu.Core.Geometry;
using QuickMenu.Core.Input;
using QuickMenu.Core.Layout;
using Xunit;

namespace QuickMenu.Core.Tests.Input;

/// <summary>T-57: drag swap in edit mode (6 px threshold, drop, cancel by right-click, Esc, hub or outside).</summary>
public class TriggerStateMachineDragTests
{
    private static readonly Vec2 _p = new(100, 100);

    private static TriggerStateMachine Editing()
    {
        var m = new TriggerStateMachine();
        m.Handle(TriggerEvent.TriggerPressed(_p));
        m.Handle(TriggerEvent.MouseDown(_p, Hit.HubEdit, 0));
        m.Handle(TriggerEvent.MouseUp(_p, Hit.HubEdit, SlotKind.Normal, 0));
        Assert.Equal(MenuState.Edit, m.State);
        return m;
    }

    private static void Press(TriggerStateMachine m, int slot, SlotKind kind = SlotKind.Normal) =>
        m.Handle(TriggerEvent.MouseDown(_p, new Hit(HitKind.Slot, slot), 0, kind));

    private static void Move(TriggerStateMachine m, float dx) => m.Handle(TriggerEvent.MouseMove(new Vec2(_p.X + dx, _p.Y)));

    [Fact]
    public void Drag_starts_only_after_six_pixels()
    {
        var m = Editing();
        Press(m, 3);
        Move(m, 5.9f);
        Assert.Equal(-1, m.DragSource);
        Move(m, 6f);
        Assert.Equal(3, m.DragSource);
    }

    [Fact]
    public void PressLost_ends_a_drag_and_forgets_the_press()
    {
        var m = Editing();
        Press(m, 3);
        Move(m, 20);
        Assert.Equal(3, m.DragSource);
        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.PressLost()));
        Assert.Equal(-1, m.DragSource);
        Assert.Equal(MenuState.Edit, m.State);
        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.MouseUp(_p, new Hit(HitKind.Slot, 7), SlotKind.Empty, 0)));
    }

    [Fact]
    public void Drop_on_another_slot_swaps()
    {
        var m = Editing();
        Press(m, 3);
        Move(m, 20);
        MenuCommand c = m.Handle(TriggerEvent.MouseUp(_p, new Hit(HitKind.Slot, 7), SlotKind.Empty, 0));
        Assert.Equal(MenuCommand.Swap(3, 7), c);
        Assert.Equal(-1, m.DragSource);
        Assert.Equal(MenuState.Edit, m.State);
    }

    [Fact]
    public void Press_and_release_under_threshold_does_nothing()
    {
        var m = Editing();
        Press(m, 3);
        Move(m, 3);
        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.MouseUp(_p, new Hit(HitKind.Slot, 3), SlotKind.Normal, 0)));
    }

    [Fact]
    public void Modifier_click_without_movement_still_unpins()
    {
        var m = Editing();
        Press(m, 3);
        Assert.Equal(MenuCommand.Unpin(3), m.Handle(TriggerEvent.MouseUp(_p, new Hit(HitKind.Slot, 3), SlotKind.Normal, 0, modifier: true)));
    }

    [Theory]
    [InlineData(HitKind.HubEdit)]
    [InlineData(HitKind.Outside)]
    [InlineData(HitKind.Slot)]
    public void Drop_on_hub_outside_or_the_source_cancels(HitKind kind)
    {
        var m = Editing();
        Press(m, 3);
        Move(m, 20);
        Hit drop = kind == HitKind.Slot ? new Hit(kind, 3) : new Hit(kind);
        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.MouseUp(_p, drop, SlotKind.Normal, 0)));
        Assert.Equal(MenuState.Edit, m.State);
        Assert.Equal(-1, m.DragSource);
    }

    [Fact]
    public void Empty_slot_is_not_a_drag_source()
    {
        var m = Editing();
        Press(m, 3, SlotKind.Empty);
        Move(m, 20);
        Assert.Equal(-1, m.DragSource);
    }

    [Fact]
    public void Reserved_slot_can_be_dragged()
    {
        var m = Editing();
        Press(m, 3, SlotKind.Reserved);
        Move(m, 20);
        Assert.Equal(3, m.DragSource);
    }

    [Fact]
    public void Right_click_cancels_the_drag_first_then_closes()
    {
        var m = Editing();
        Press(m, 3);
        Move(m, 20);
        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.MouseDown(_p, Hit.Outside, 1)));
        Assert.Equal(-1, m.DragSource);
        Assert.Equal(MenuState.Edit, m.State);
        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.MouseUp(_p, new Hit(HitKind.Slot, 7), SlotKind.Normal, 0)));
        Assert.Equal(MenuCommand.CloseFromEdit, m.Handle(TriggerEvent.MouseDown(_p, Hit.Outside, 1)));
    }

    [Fact]
    public void Esc_cancels_the_drag_first()
    {
        var m = Editing();
        Press(m, 3);
        Move(m, 20);
        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.Escape(_p)));
        Assert.Equal(MenuState.Edit, m.State);
        Assert.Equal(MenuCommand.ExitEdit, m.Handle(TriggerEvent.Escape(_p)));
    }

    [Fact]
    public void Use_mode_never_drags()
    {
        var m = new TriggerStateMachine();
        m.Handle(TriggerEvent.TriggerPressed(_p));
        Press(m, 3);
        Move(m, 20);
        Assert.Equal(-1, m.DragSource);
    }

    [Fact]
    public void A_new_press_forgets_an_old_drag_source()
    {
        var m = Editing();
        Press(m, 3);
        Move(m, 20);
        m.Handle(TriggerEvent.MouseUp(_p, Hit.Outside, SlotKind.Normal, 0));
        m.Handle(TriggerEvent.MouseDown(_p, Hit.HubEdit, 0));
        Move(m, 30);
        Assert.Equal(-1, m.DragSource);
    }
}
