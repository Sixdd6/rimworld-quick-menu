using QuickMenu.Core.Geometry;
using QuickMenu.Core.Input;
using QuickMenu.Core.Layout;
using QuickMenu.Core.Render;
using Xunit;

namespace QuickMenu.Tests.Render;

/// <summary>design.md S4 states 8, 9, 11 and S5 (help panel, hint plate): T-55.</summary>
public class EditVisualsTests
{
    private static readonly Hit _none = Hit.Outside;
    private static readonly DragVisual _drag = new(2, 5, new Vec2(300f, 200f));

    // ---- Drag source (9) and drop target (11) -----------------------------------------------------

    [Fact]
    public void NoDrag_is_the_use_mode_slot_state()
    {
        Assert.False(DragVisual.None.Active);
        Assert.Equal(SlotVisualState.Hover, EditVisuals.Slot(2, SlotKind.Normal, Hit.Slot(2), _none, DragVisual.None));
        Assert.Equal(SlotVisualState.Pressed, EditVisuals.Slot(2, SlotKind.Normal, Hit.Slot(2), Hit.Slot(2), DragVisual.None));
        Assert.Equal(SlotVisualState.Empty, EditVisuals.Slot(2, SlotKind.Empty, Hit.Slot(2), _none, DragVisual.None));
    }

    [Fact]
    public void Drag_source_is_the_ghost_and_target_is_highlighted()
    {
        Assert.True(_drag.Active);
        Assert.Equal(SlotVisualState.DragSource, EditVisuals.Slot(2, SlotKind.Normal, Hit.Slot(5), Hit.Slot(2), _drag));
        Assert.Equal(SlotVisualState.DragSource, EditVisuals.Slot(2, SlotKind.Reserved, Hit.Slot(5), Hit.Slot(2), _drag));
        Assert.Equal(SlotVisualState.DropTarget, EditVisuals.Slot(5, SlotKind.Normal, Hit.Slot(5), Hit.Slot(2), _drag));
    }

    [Theory]
    [InlineData(SlotKind.Empty)]
    [InlineData(SlotKind.Reserved)]
    [InlineData(SlotKind.Disabled)]
    [InlineData(SlotKind.Submenu)]
    public void Any_slot_kind_can_be_the_drop_target_including_an_empty_fixed_slot(SlotKind kind)
    {
        Assert.Equal(SlotVisualState.DropTarget, EditVisuals.Slot(5, kind, Hit.Slot(5), Hit.Slot(2), _drag));
    }

    [Fact]
    public void Other_slots_neither_hover_nor_press_while_dragging()
    {
        var drag = new DragVisual(2, -1, default);

        Assert.Equal(SlotVisualState.Normal, EditVisuals.Slot(4, SlotKind.Normal, Hit.Slot(4), Hit.Slot(4), drag));
        Assert.Equal(SlotVisualState.Empty, EditVisuals.Slot(4, SlotKind.Empty, Hit.Slot(4), _none, drag));
        Assert.Equal(SlotVisualState.Empty, EditVisuals.Slot(4, SlotKind.Reserved, Hit.Slot(4), _none, drag));
    }

    [Fact]
    public void Dropping_back_on_the_source_keeps_the_ghost()
    {
        var drag = new DragVisual(2, 2, default);

        Assert.Equal(SlotVisualState.DragSource, EditVisuals.Slot(2, SlotKind.Normal, Hit.Slot(2), Hit.Slot(2), drag));
    }

    [Fact]
    public void Ghost_has_a_dashed_ring_and_no_icon_badge_or_reserved_mark()
    {
        Assert.Equal(SlotRing.EmptyDashed, WheelVisuals.Ring(SlotVisualState.DragSource, SlotDecor.SubMenu | SlotDecor.LastUsed));
        Assert.Equal(SlotRing.EmptyDashed, WheelVisuals.Ring(SlotVisualState.DragSource, SlotDecor.None, SlotKind.Normal, editing: true));
        Assert.False(EditVisuals.ShowsIcon(SlotVisualState.DragSource));
        Assert.False(EditVisuals.ShowsBadges(SlotVisualState.DragSource));
        Assert.False(EditVisuals.ShowsDropTint(SlotVisualState.DragSource));
        Assert.False(WheelVisuals.ShowsReservedMarker(SlotVisualState.DragSource, SlotKind.Reserved, editing: true));
    }

    [Fact]
    public void Target_has_the_accent_ring_tint_and_keeps_its_content()
    {
        Assert.Equal(SlotRing.Accent2, WheelVisuals.Ring(SlotVisualState.DropTarget, SlotDecor.LastUsed));
        Assert.Equal(SlotRing.Accent2, WheelVisuals.Ring(SlotVisualState.DropTarget, SlotDecor.None, SlotKind.Empty, editing: true));
        Assert.Equal(SlotRing.Accent2, WheelVisuals.Ring(SlotVisualState.DropTarget, SlotDecor.None, SlotKind.Reserved, editing: true));
        Assert.True(EditVisuals.ShowsIcon(SlotVisualState.DropTarget));
        Assert.True(EditVisuals.ShowsBadges(SlotVisualState.DropTarget));
        Assert.True(EditVisuals.ShowsDropTint(SlotVisualState.DropTarget));
        Assert.True(WheelVisuals.ShowsReservedMarker(SlotVisualState.DropTarget, SlotKind.Reserved, editing: true));
        Assert.Equal(0.22f, EditVisuals.DropTargetTintAlpha);
    }

    [Theory]
    [InlineData(SlotVisualState.Normal, true)]
    [InlineData(SlotVisualState.Hover, true)]
    [InlineData(SlotVisualState.Pressed, true)]
    [InlineData(SlotVisualState.Empty, false)]
    public void Base_states_keep_their_icon_rule_and_get_no_tint(SlotVisualState state, bool icon)
    {
        Assert.Equal(icon, EditVisuals.ShowsIcon(state));
        Assert.True(EditVisuals.ShowsBadges(state));
        Assert.False(EditVisuals.ShowsDropTint(state));
    }

    [Fact]
    public void Reserved_slot_in_edit_mode_keeps_its_dashed_ring_and_mark_when_not_dragged()
    {
        Assert.Equal(SlotRing.EmptyDashed, WheelVisuals.Ring(SlotVisualState.Empty, SlotDecor.None, SlotKind.Reserved, editing: true));
        Assert.True(WheelVisuals.ShowsReservedMarker(SlotVisualState.Empty, SlotKind.Reserved, editing: true));
        Assert.False(WheelVisuals.ShowsReservedMarker(SlotVisualState.Empty, SlotKind.Reserved, editing: false));
    }

    [Fact]
    public void Hub_never_highlights_while_dragging()
    {
        Assert.Equal(HubPartState.Normal, EditVisuals.Hub(Hit.HubEdit, Hit.HubEdit, _drag));
        Assert.Equal(HubPartState.Hover, EditVisuals.Hub(Hit.HubEdit, _none, DragVisual.None));
    }

    [Fact]
    public void Plate_shows_the_target_label_but_never_the_hub_while_dragging()
    {
        SlotKind[] kinds = [SlotKind.Normal, SlotKind.Normal, SlotKind.Normal, SlotKind.Empty, SlotKind.Normal, SlotKind.Normal];

        Assert.Equal(PlateContent.None, EditVisuals.Plate(Hit.HubEdit, kinds, kinds.Length, _drag));
        Assert.Equal(PlateContent.Slot, EditVisuals.Plate(Hit.Slot(5), kinds, kinds.Length, _drag));
        Assert.Equal(PlateContent.None, EditVisuals.Plate(Hit.Slot(3), kinds, kinds.Length, _drag));
        Assert.Equal(PlateContent.Hub, EditVisuals.Plate(Hit.HubEdit, kinds, kinds.Length, DragVisual.None));
    }

    [Fact]
    public void Dragged_icon_is_09B_offset_16_from_the_pointer()
    {
        Rect2 r = EditVisuals.DraggedIconRect(new Vec2(300f, 200f), 56f);

        Assert.Equal(316f, r.X);
        Assert.Equal(216f, r.Y);
        Assert.Equal(0.9f * 56f, r.Width, 4);
        Assert.Equal(r.Width, r.Height);
        Assert.Equal(0.8f, EditVisuals.DragIconAlpha);
    }

    // ---- Help panel (S5) --------------------------------------------------------------------------

    [Fact]
    public void Help_panel_is_top_centre_60_down_and_wraps_at_520()
    {
        Assert.Equal(520f - 24f, EditVisuals.HelpTextMaxWidth(1920f));

        Rect2 wide = EditVisuals.HelpPanelRect(1920f, 2000f, 72f, 22f);
        Assert.Equal(520f, wide.Width);
        Assert.Equal((1920f - 520f) / 2f, wide.X);
        Assert.Equal(60f, wide.Y);
        Assert.Equal(12f + 72f + 6f + 22f + 12f, wide.Height);

        Rect2 narrow = EditVisuals.HelpPanelRect(1920f, 300f, 22f, 22f);
        Assert.Equal(324f, narrow.Width);
        Assert.Equal(960f, narrow.X + (narrow.Width / 2f));
    }

    [Fact]
    public void Help_panel_keeps_a_margin_on_a_small_ui_screen()
    {
        Assert.Equal(400f - 32f - 24f, EditVisuals.HelpTextMaxWidth(400f));
        Rect2 r = EditVisuals.HelpPanelRect(400f, 1000f, 100f, 22f);
        Assert.Equal(16f, r.X);
        Assert.Equal(400f - 32f, r.Width);
        Assert.Equal(0f, EditVisuals.HelpTextMaxWidth(30f));
        Assert.Equal(24f, EditVisuals.HelpPanelRect(30f, -5f, 0f, 0f).Width);
    }

    [Fact]
    public void Help_text_and_capacity_line_stack_inside_the_padding()
    {
        Rect2 panel = EditVisuals.HelpPanelRect(1920f, 400f, 66f, 22f);

        Rect2 text = EditVisuals.HelpTextRect(panel, 66f);
        Rect2 line = EditVisuals.CapacityLineRect(panel, 66f, 22f);

        Assert.Equal(new Rect2(panel.X + 12f, 72f, 400f, 66f), text);
        Assert.Equal(new Rect2(panel.X + 12f, 72f + 66f + 6f, 400f, 22f), line);
        Assert.Equal(panel.Bottom - 12f, line.Bottom);
    }

    // ---- Hint plate (S5, FR-16) -------------------------------------------------------------------

    [Fact]
    public void Hint_plate_wraps_at_360_and_sits_where_the_label_plate_goes()
    {
        Assert.Equal(348f, EditVisuals.HintTextMaxWidth);

        Rect2 wrapped = EditVisuals.HintPlateRect(500f, 470f, 900f, 44f);
        Assert.Equal(new Rect2(320f, 470f, 360f, 52f), wrapped);

        Rect2 small = EditVisuals.HintPlateRect(500f, 470f, 100f, 22f);
        Assert.Equal(new Rect2(444f, 470f, 112f, 30f), small);
    }
}
