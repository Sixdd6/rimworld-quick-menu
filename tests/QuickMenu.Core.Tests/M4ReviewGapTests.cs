using System.Collections.Generic;
using System.Linq;
using QuickMenu.Core.Geometry;
using QuickMenu.Core.Input;
using QuickMenu.Core.Layout;
using QuickMenu.Core.Pins;
using QuickMenu.Core.Settings;
using Xunit;

namespace QuickMenu.Core.Tests;

/// <summary>M4 milestone review: boundary and state-machine gaps found after T-50 .. T-59 (grid hit, Auto capacity at screen limits, board snapshot, drag, preview rules).</summary>
public class M4ReviewGapTests
{
    private sealed class Resolver(params PinKey[] missing) : IPinResolver
    {
        private readonly HashSet<PinKey> _missing = new(missing);

        public bool IsResolved(PinKey key) => !_missing.Contains(key);
    }

    private static PinKey K(string n) => PinKey.Build(PinKeyKind.BuildThing, n);

    private static string[] Names(PinBoard b) => b.Entries.Select(e => e?.Key.Value ?? "-").ToArray();

    private static PinBoard Fixed(int capacity, params (int Slot, string Name)[] pins)
    {
        var b = new PinBoard(capacity);
        foreach (var (slot, name) in pins)
        {
            b.SetSlot(slot, new PinEntry(K(name), name, null));
        }

        return b;
    }

    // ---- PinBoard: Fixed <-> Auto snapshot ----

    [Fact]
    public void Fixed_layout_is_restored_after_Auto_when_nothing_changed()
    {
        PinBoard b = Fixed(8, (1, "A"), (4, "B"), (6, "C"));
        b.Mode = SizingMode.Auto;
        Assert.Equal(new[] { "build:thing/A", "build:thing/B", "build:thing/C" }, Names(b));
        b.Mode = SizingMode.Fixed;
        Assert.Equal(new[] { "-", "build:thing/A", "-", "-", "build:thing/B", "-", "build:thing/C" }, Names(b));
    }

    [Theory]
    [InlineData("toggle-new")]
    [InlineData("toggle-off")]
    [InlineData("swap")]
    [InlineData("clear")]
    [InlineData("forget")]
    [InlineData("setslot")]
    public void Any_pin_change_in_Auto_drops_the_snapshot_so_Fixed_keeps_the_compact_order(string change)
    {
        PinBoard b = Fixed(8, (1, "A"), (4, "B"), (6, "C"));
        b.Mode = SizingMode.Auto;
        switch (change)
        {
            case "toggle-new": b.Toggle(K("D")); break;
            case "toggle-off": b.Toggle(K("B")); break;
            case "swap": b.Swap(0, 2); break;
            case "clear": b.Clear(1); break;
            case "forget": b.Forget(e => e.Key.Value.EndsWith("/B", System.StringComparison.Ordinal)); break;
            default: b.SetSlot(0, new PinEntry(K("Z"), "Z", null)); break;
        }

        string[] autoOrder = Names(b);
        b.Mode = SizingMode.Fixed;

        // Compact, no gaps and no resurrected pin: exactly what Auto showed.
        Assert.Equal(autoOrder, Names(b));
        Assert.DoesNotContain("-", Names(b));
    }

    [Fact]
    public void Snapshot_restore_keeps_pins_beyond_a_shrunk_capacity_hidden_not_lost()
    {
        PinBoard b = Fixed(8, (1, "A"), (6, "C"));
        b.Mode = SizingMode.Auto;
        b.Capacity = 3;
        b.Mode = SizingMode.Fixed;
        Assert.Equal(7, b.Entries.Count);
        Assert.Equal("build:thing/C", b.Entries[6]!.Key.Value);
        Assert.Equal(1, SettingsPreview.HiddenPins(b));
        Assert.Equal(1, b.VisibleCount(new Resolver()));
    }

    [Fact]
    public void Switching_to_the_same_mode_is_a_no_op_and_Auto_twice_does_not_overwrite_the_snapshot()
    {
        PinBoard b = Fixed(8, (2, "A"), (5, "B"));
        b.Mode = SizingMode.Auto;
        b.Mode = SizingMode.Auto;
        b.Mode = SizingMode.Fixed;
        b.Mode = SizingMode.Fixed;
        Assert.Equal(new[] { "-", "-", "build:thing/A", "-", "-", "build:thing/B" }, Names(b));
    }

    [Fact]
    public void Empty_board_converts_both_ways()
    {
        var b = new PinBoard(5) { Mode = SizingMode.Auto };
        Assert.Empty(b.Entries);
        b.Mode = SizingMode.Fixed;
        Assert.Empty(b.Entries);
        Assert.Equal(0, b.VisibleCount(new Resolver()));
    }

    [Fact]
    public void Forget_predicate_removes_only_matches_and_resets_nothing_when_none_match()
    {
        PinBoard b = Fixed(8, (1, "A"), (4, "B"));
        b.Mode = SizingMode.Auto;
        b.Mode = SizingMode.Fixed;
        Assert.Equal(0, b.Forget(_ => false));
        Assert.Equal(2, b.Entries.Count(e => e is not null));
        Assert.Equal(2, b.Forget(_ => true));
        Assert.Empty(b.Entries);
    }

    [Fact]
    public void VisibleCount_Fixed_counts_unresolved_reserved_slots_and_Auto_does_not()
    {
        PinBoard b = Fixed(4, (0, "A"), (2, "Gone"), (3, "B"));
        var r = new Resolver(K("Gone"));
        Assert.Equal(3, b.VisibleCount(r));
        b.Mode = SizingMode.Auto;
        Assert.Equal(2, b.VisibleCount(r));
        b.Capacity = 0;
        Assert.Equal(2, b.VisibleCount(r));
    }

    [Fact]
    public void Auto_toggle_at_capacity_zero_is_Full()
    {
        var b = new PinBoard(0) { Mode = SizingMode.Auto };
        Assert.Equal(PinResult.Full, b.Toggle(K("A")));
    }

    // ---- Grid hit testing ----

    [Theory]
    [InlineData(3, 1)]
    [InlineData(5, 3)]
    [InlineData(11, 9)]
    [InlineData(11, 1)]
    [InlineData(3, 9)]
    public void Every_grid_cell_centre_hits_its_slot_and_the_hub_is_the_only_other_cell(int cols, int rows)
    {
        var centre = new Vec2(500, 400);
        const float B = 56f;
        SlotPlacement[] slots = GridLayout.Compute(new GridParams(B, cols, rows, centre));
        var layout = new GridHitLayout(centre, B, cols, rows);
        for (int i = 0; i < slots.Length; i++)
        {
            Assert.Equal(Hit.Slot(i), HitTester.HitGrid(slots[i].Center, layout));
        }

        Assert.Equal(Hit.HubEdit, HitTester.HitGrid(centre, layout));
        Assert.Equal(cols * rows - 1, slots.Length);
    }

    [Fact]
    public void Grid_outer_cell_edge_is_a_slot_and_one_pixel_beyond_the_cell_is_panel_padding_or_outside()
    {
        var centre = new Vec2(500, 400);
        const float B = 56f;
        var layout = new GridHitLayout(centre, B, 5, 3);
        float pitch = LayoutConstants.Pitch(B);
        float right = centre.X + (2.5f * pitch);
        float bottom = centre.Y + (1.5f * pitch);
        Assert.Equal(Hit.Slot(13), HitTester.HitGrid(new Vec2(right, bottom), layout));
        Assert.Equal(Hit.Slot(13), HitTester.HitGrid(new Vec2(right - 0.01f, bottom - 0.01f), layout));
        Assert.Equal(Hit.Outside, HitTester.HitGrid(new Vec2(right + (2f * LayoutConstants.RimPad) + 5f, bottom), layout));
    }

    // ---- Capacity at screen limits ----

    [Fact]
    public void Auto_wheel_full_flips_exactly_at_the_screen_minus_margins_boundary()
    {
        const float B = 56f;
        int m = 3;
        int count = RingCapacity.Total(m);
        Rect2 next = MenuBounds.ForWheel(default, B, m + 1, LayoutConstants.PlateHeight);
        float w = next.Width + (2f * LayoutConstants.Margin);
        float h = next.Height + (2f * LayoutConstants.Margin);
        Assert.False(Capacity.IsAutoWheelFull(count, B, new Vec2(w, h)));
        Assert.True(Capacity.IsAutoWheelFull(count, B, new Vec2(w - 0.5f, h)));
        Assert.True(Capacity.IsAutoWheelFull(count, B, new Vec2(w, h - 0.5f)));
    }

    [Fact]
    public void Auto_grid_full_flips_exactly_at_the_screen_minus_margins_boundary()
    {
        const float B = 56f;
        const int cols = 5;
        int count = Capacity.FixedGrid(cols, 3);
        Rect2 panel = GridLayout.Panel(new GridParams(B, cols, 5, default));
        Rect2 next = MenuBounds.ForPanel(panel, 300f, LayoutConstants.PlateHeight);
        float w = next.Width + (2f * LayoutConstants.Margin);
        float h = next.Height + (2f * LayoutConstants.Margin);
        Assert.False(Capacity.IsAutoGridFull(count, cols, B, new Vec2(w, h), 300f));
        Assert.True(Capacity.IsAutoGridFull(count, cols, B, new Vec2(w, h - 0.5f), 300f));
    }

    [Fact]
    public void Auto_never_reports_full_one_pin_below_the_last_slot_even_on_a_tiny_screen()
    {
        Assert.False(Capacity.IsAutoWheelFull(RingCapacity.Total(1) - 1, 56f, new Vec2(10, 10)));
        Assert.True(Capacity.IsAutoWheelFull(RingCapacity.Total(1), 56f, new Vec2(10, 10)));
        Assert.False(Capacity.IsAutoGridFull(Capacity.FixedGrid(3, 1) - 1, 3, 56f, new Vec2(10, 10), 100f));
        Assert.True(Capacity.IsAutoGridFull(Capacity.FixedGrid(3, 1), 3, 56f, new Vec2(10, 10), 100f));
    }

    [Fact]
    public void Auto_sizes_clamp_for_counts_beyond_the_maximum_and_below_zero()
    {
        Assert.Equal(LayoutConstants.AutoMaxRings, Capacity.AutoWheelRings(int.MaxValue));
        Assert.Equal(1, Capacity.AutoWheelRings(-5));
        Assert.Equal(GridLayout.MaxAutoRows, Capacity.AutoGridRows(int.MaxValue, 11));
        Assert.Equal(1, Capacity.AutoGridRows(-1, 3));
    }

    // ---- Drag state machine ----

    private static readonly Vec2 _p = new(100, 100);

    private static TriggerStateMachine Editing()
    {
        var m = new TriggerStateMachine();
        m.Handle(TriggerEvent.TriggerPressed(_p));
        m.Handle(TriggerEvent.MouseDown(_p, Hit.HubEdit, 0));
        m.Handle(TriggerEvent.MouseUp(_p, Hit.HubEdit, SlotKind.Normal, 0));
        return m;
    }

    private static void Press(TriggerStateMachine m, int slot) => m.Handle(TriggerEvent.MouseDown(_p, new Hit(HitKind.Slot, slot), 0));

    [Fact]
    public void Drag_threshold_is_euclidean_not_per_axis()
    {
        var m = Editing();
        Press(m, 2);
        m.Handle(TriggerEvent.MouseMove(new Vec2(_p.X + 4, _p.Y + 4)));
        Assert.Equal(-1, m.DragSource);
        m.Handle(TriggerEvent.MouseMove(new Vec2(_p.X + 4.3f, _p.Y + 4.3f)));
        Assert.Equal(2, m.DragSource);
    }

    [Fact]
    public void Once_started_a_drag_survives_the_pointer_returning_to_the_source()
    {
        var m = Editing();
        Press(m, 2);
        m.Handle(TriggerEvent.MouseMove(new Vec2(_p.X + 30, _p.Y)));
        m.Handle(TriggerEvent.MouseMove(_p));
        Assert.Equal(2, m.DragSource);
        Assert.Equal(MenuCommand.Swap(2, 9), m.Handle(TriggerEvent.MouseUp(_p, new Hit(HitKind.Slot, 9), SlotKind.Normal, 0)));
    }

    [Fact]
    public void Moving_without_a_press_never_starts_a_drag()
    {
        var m = Editing();
        m.Handle(TriggerEvent.MouseMove(new Vec2(_p.X + 50, _p.Y)));
        Assert.Equal(-1, m.DragSource);
    }

    [Theory]
    [InlineData("trigger")]
    [InlineData("maplost")]
    public void Closing_during_a_drag_ends_it_and_a_later_open_starts_clean(string how)
    {
        var m = Editing();
        Press(m, 2);
        m.Handle(TriggerEvent.MouseMove(new Vec2(_p.X + 30, _p.Y)));
        MenuCommand c = how == "trigger" ? m.Handle(TriggerEvent.TriggerPressed(_p)) : m.Handle(TriggerEvent.MapLost());
        Assert.Equal(MenuCommand.CloseFromEdit, c);
        Assert.Equal(-1, m.DragSource);
        Assert.Equal(MenuState.Closed, m.State);
        Assert.Equal(MenuCommand.Open, m.Handle(TriggerEvent.TriggerPressed(_p)));
        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.MouseUp(_p, new Hit(HitKind.Slot, 4), SlotKind.Normal, 0)));
    }

    [Theory]
    [InlineData(HitKind.Plate)]
    [InlineData(HitKind.Inside)]
    public void Dropping_on_the_plate_or_panel_padding_cancels(HitKind kind)
    {
        var m = Editing();
        Press(m, 2);
        m.Handle(TriggerEvent.MouseMove(new Vec2(_p.X + 30, _p.Y)));
        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.MouseUp(_p, new Hit(kind), SlotKind.Normal, 0)));
        Assert.Equal(MenuState.Edit, m.State);
    }

    [Fact]
    public void A_second_drag_works_after_a_cancelled_one()
    {
        var m = Editing();
        Press(m, 2);
        m.Handle(TriggerEvent.MouseMove(new Vec2(_p.X + 30, _p.Y)));
        m.Handle(TriggerEvent.PressLost());
        Press(m, 5);
        m.Handle(TriggerEvent.MouseMove(new Vec2(_p.X, _p.Y + 30)));
        Assert.Equal(5, m.DragSource);
        Assert.Equal(MenuCommand.Swap(5, 1), m.Handle(TriggerEvent.MouseUp(_p, new Hit(HitKind.Slot, 1), SlotKind.Empty, 0)));
    }

    [Fact]
    public void Right_button_press_does_not_start_a_drag_and_a_hub_press_is_not_a_source()
    {
        var m = Editing();
        m.Handle(TriggerEvent.MouseDown(_p, new Hit(HitKind.Slot, 2), 2));
        m.Handle(TriggerEvent.MouseMove(new Vec2(_p.X + 30, _p.Y)));
        Assert.Equal(-1, m.DragSource);
        m.Handle(TriggerEvent.MouseDown(_p, Hit.HubEdit, 0));
        m.Handle(TriggerEvent.MouseMove(new Vec2(_p.X + 30, _p.Y)));
        Assert.Equal(-1, m.DragSource);
    }

    // ---- SettingsPreview rules ----

    [Fact]
    public void Preview_scale_handles_degenerate_bounds_and_hits_the_area_edge_exactly()
    {
        Assert.Equal(SettingsPreview.MaxScale, SettingsPreview.Scale(new Rect2(0, 0, 0, 0)));
        Assert.Equal(SettingsPreview.MaxScale, SettingsPreview.Scale(new Rect2(0, 0, 820, 540)));
        float s = SettingsPreview.Scale(new Rect2(0, 0, 1640, 100));
        Assert.Equal(SettingsPreview.Width / 1640f, s, 5);
        float t = SettingsPreview.Scale(new Rect2(0, 0, 100, 1080));
        Assert.Equal(SettingsPreview.Height / 1080f, t, 5);
    }

    [Fact]
    public void FitsScreen_is_inclusive_at_the_margin_boundary()
    {
        var box = new Rect2(0, 0, 1920 - (2 * LayoutConstants.Margin), 1080 - (2 * LayoutConstants.Margin));
        Assert.True(SettingsPreview.FitsScreen(box, new Vec2(1920, 1080)));
        Assert.False(SettingsPreview.FitsScreen(box, new Vec2(1919, 1080)));
        Assert.False(SettingsPreview.FitsScreen(box, new Vec2(1920, 1079)));
    }

    [Theory]
    [InlineData(MenuLayout.Wheel, SizingMode.Fixed, true, false, false)]
    [InlineData(MenuLayout.Wheel, SizingMode.Auto, false, false, false)]
    [InlineData(MenuLayout.Grid, SizingMode.Fixed, false, true, true)]
    [InlineData(MenuLayout.Grid, SizingMode.Auto, false, true, false)]
    public void Control_visibility_matrix(MenuLayout layout, SizingMode sizing, bool rings, bool cols, bool rows)
    {
        Assert.Equal(rings, SettingsPreview.ShowsRings(layout, sizing));
        Assert.Equal(cols, SettingsPreview.ShowsColumns(layout, sizing));
        Assert.Equal(rows, SettingsPreview.ShowsRows(layout, sizing));
    }

    [Fact]
    public void Steppers_clamp_at_both_ends_and_repair_even_stored_values()
    {
        Assert.Equal(11, SettingsPreview.StepColumns(11, 1));
        Assert.Equal(3, SettingsPreview.StepColumns(3, -1));
        Assert.Equal(7, SettingsPreview.StepRows(7, 1));
        Assert.Equal(1, SettingsPreview.StepRows(1, -1));
        Assert.True(GridLayout.IsValidColumns(SettingsPreview.StepColumns(6, 1)));
        Assert.True(GridLayout.IsValidFixedRows(SettingsPreview.StepRows(4, -1)));
        Assert.Equal(SettingsPreview.StepColumns(5, 0), SettingsRules.ClampColumns(5));
    }

    [Fact]
    public void HiddenPins_counts_only_occupied_slots_beyond_capacity_in_Fixed()
    {
        PinBoard b = Fixed(2, (0, "A"), (3, "B"), (5, "C"));
        Assert.Equal(2, SettingsPreview.HiddenPins(b));
        b.Capacity = 6;
        Assert.Equal(0, SettingsPreview.HiddenPins(b));
    }
}
