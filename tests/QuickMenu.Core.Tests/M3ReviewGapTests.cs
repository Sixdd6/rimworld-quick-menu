using System;
using System.Collections.Generic;
using QuickMenu.Core.Catalog;
using QuickMenu.Core.Geometry;
using QuickMenu.Core.Input;
using QuickMenu.Core.Layout;
using QuickMenu.Core.Materials;
using Xunit;

namespace QuickMenu.Core.Tests;

/// <summary>M3 milestone review: cheap edge cases for the sub-menu, material and edit-mode logic (FR-9, FR-11, FR-11a, FR-12).</summary>
public class M3ReviewGapTests
{
    private static readonly Vec2 _p = new(40, 40);

    private static MenuCommand Click(TriggerStateMachine m, Hit hit, SlotKind kind = SlotKind.Normal, bool modifier = false)
    {
        m.Handle(TriggerEvent.MouseDown(_p, hit, 0));
        return m.Handle(TriggerEvent.MouseUp(_p, hit, kind, 0, modifier));
    }

    private static TriggerStateMachine Satellite()
    {
        var m = new TriggerStateMachine();
        m.Handle(TriggerEvent.TriggerPressed(_p));
        Assert.Equal(MenuCommand.OpenSub(2), Click(m, Hit.Slot(2), SlotKind.Submenu));
        return m;
    }

    // MaterialVariants (FR-11a)
    [Fact]
    public void Duplicate_ingredient_anywhere_is_not_a_variant_set()
    {
        var e = new List<VariantElement> { new("t", "A"), new("t", "B"), new("t", "A") };
        Assert.False(MaterialVariants.IsMaterialVariantSet(e));
    }

    [Fact]
    public void Kind_comparison_is_ordinal_and_case_sensitive()
    {
        Assert.False(MaterialVariants.IsMaterialVariantSet(new[] { new VariantElement("Tile", "A"), new VariantElement("tile", "B") }));
    }

    [Fact]
    public void A_missing_ingredient_late_in_the_list_disqualifies_and_null_list_throws()
    {
        Assert.False(MaterialVariants.IsMaterialVariantSet(new[] { new VariantElement("t", "A"), new VariantElement("t", "B"), new VariantElement("t", null) }));
        Assert.False(MaterialVariants.IsMaterialVariantSet(new[] { new VariantElement("t", "A"), new VariantElement("t", "") }));
        Assert.Throws<ArgumentNullException>(() => MaterialVariants.IsMaterialVariantSet(null!));
    }

    [Fact]
    public void Large_distinct_set_is_a_variant_set()
    {
        var e = new VariantElement[500];
        for (var i = 0; i < e.Length; i++)
        {
            e[i] = new VariantElement("t", "m" + i);
        }

        Assert.True(MaterialVariants.IsMaterialVariantSet(e));
    }

    // StuffOrdering (FR-12 fixed order)
    [Fact]
    public void Duplicate_ids_report_the_first_match_and_empty_last_used_matches_nothing()
    {
        var o = StuffOrdering.Order(new[] { "a", "b", "a" }, "a");
        Assert.Equal(0, o.LastUsedIndex);
        Assert.Equal(new[] { "a", "b", "a" }, o.Ids);
        Assert.Equal(-1, StuffOrdering.Order(new[] { "a", "b" }, "").LastUsedIndex);
        Assert.Equal(-1, StuffOrdering.Order(new[] { "a", "b" }, "A").LastUsedIndex);
    }

    [Fact]
    public void Single_option_skip_still_reports_last_used_and_none_reports_minus_one()
    {
        var s = StuffOrdering.Order(new[] { "a" }, "a");
        Assert.Equal(StuffRingOutcome.Skip, s.Outcome);
        Assert.Equal(0, s.LastUsedIndex);
        Assert.Equal(-1, StuffOrdering.Order(Array.Empty<string>(), "a").LastUsedIndex);
        Assert.Throws<ArgumentNullException>(() => StuffOrdering.Order(null!, null));
    }

    [Fact]
    public void Last_used_at_the_end_never_moves_anything()
    {
        var ids = new[] { "wood", "steel", "gold", "jade" };
        var o = StuffOrdering.Order(ids, "jade");
        Assert.Equal(ids, o.Ids);
        Assert.Equal(3, o.LastUsedIndex);
    }

    // IdeoFingerprint / IdeoSignature
    [Fact]
    public void Fingerprint_extremes_do_not_throw_in_checked_contexts()
    {
        checked
        {
            _ = IdeoFingerprint.Of(int.MaxValue, int.MaxValue, int.MaxValue);
            _ = IdeoFingerprint.Of(int.MinValue, int.MinValue, int.MinValue);
            _ = IdeoFingerprint.Combine(long.MaxValue, long.MaxValue);
            _ = IdeoFingerprint.Combine(long.MinValue, long.MinValue);
        }
    }

    [Fact]
    public void Signature_game_change_without_ideo_is_a_change_and_back_to_none_is_a_change()
    {
        var s = new IdeoSignature();
        var g1 = new object();
        Assert.True(s.Update(g1, null, 0));
        Assert.False(s.HasIdeo);
        Assert.True(s.Update(new object(), null, 0));
        Assert.True(s.Update(null, null, 0));
        Assert.False(s.Update(null, null, 0, 99));
        Assert.True(s.Update(g1, new object(), 3));
        Assert.True(s.HasIdeo);
        Assert.True(s.Update(null, null, 0));
        Assert.False(s.HasIdeo);
    }

    // StuffSlotPolicy exhaustiveness
    [Fact]
    public void Unknown_outcome_value_is_treated_as_a_plain_slot_and_never_throws()
    {
        Assert.Equal(SlotKind.Normal, StuffSlotPolicy.KindFor((StuffRingOutcome)99, true));
        Assert.False(StuffSlotPolicy.IsNoMaterials((StuffRingOutcome)99, true));
    }

    // SubMenuLayout
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(37)]
    [InlineData(60)]
    public void Satellite_on_a_tiny_screen_does_not_throw_and_returns_n_slots(int n)
    {
        var parent = new Vec2(10, 10);
        var s = SubMenuLayout.PlaceSatellite(n, 56f, parent, new Vec2(200, 150));
        Assert.Equal(n, s.Slots.Length);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(36)]
    [InlineData(100)]
    public void SubGrid_columns_never_exceed_six_and_cells_cover_n(int n)
    {
        var cols = SubMenuLayout.SubGridColumns(n);
        Assert.InRange(cols, 1, 6);
        var g = SubMenuLayout.PlaceSubGrid(n, 56f, new Rect2(560f, 300f, 336f, 208f), 728f, new Vec2(800, 400), new Vec2(1920, 1080), 120f);
        Assert.Equal(n, g.Cells.Length);
        Assert.Equal((n + cols - 1) / cols, g.Rows);
    }

    // TriggerStateMachine satellite / ResumeSatellite / edit mode
    [Fact]
    public void Modifier_click_in_a_satellite_activates_at_satellite_level_never_unpins()
    {
        var m = Satellite();
        Assert.Equal(MenuCommand.Activate(1, MenuLevel.Satellite), Click(m, Hit.Slot(1), SlotKind.Normal, modifier: true));
        Assert.Equal(MenuState.Closed, m.State);
    }

    [Fact]
    public void Disabled_satellite_option_rejects_and_stays_in_the_satellite()
    {
        var m = Satellite();
        Assert.Equal(MenuCommand.Reject(1), Click(m, Hit.Slot(1), SlotKind.Disabled));
        Assert.Equal(MenuState.Satellite, m.State);
    }

    [Fact]
    public void Satellite_hub_and_outside_click_and_right_click_go_back_one_level()
    {
        var m = Satellite();
        Assert.Equal(MenuCommand.Back, Click(m, Hit.HubEdit));
        Assert.Equal(MenuState.Main, m.State);

        m = Satellite();
        Assert.Equal(MenuCommand.Back, Click(m, Hit.Outside));

        m = Satellite();
        Assert.Equal(MenuCommand.Back, m.Handle(TriggerEvent.MouseDown(_p, Hit.Slot(1), 1)));
        Assert.Equal(MenuState.Main, m.State);
    }

    [Fact]
    public void Satellite_never_enters_edit_mode_and_trigger_closes_it()
    {
        var m = Satellite();
        Assert.NotEqual(MenuCommand.EnterEdit, Click(m, Hit.HubEdit));
        m = Satellite();
        Assert.Equal(MenuCommand.Close, m.Handle(TriggerEvent.TriggerPressed(_p)));
        Assert.Equal(MenuState.Closed, m.State);
    }

    [Fact]
    public void Resume_does_nothing_in_edit_mode()
    {
        var m = new TriggerStateMachine();
        m.Handle(TriggerEvent.TriggerPressed(_p));
        Click(m, Hit.HubEdit);
        m.ResumeSatellite();
        Assert.Equal(MenuState.Edit, m.State);
    }

    [Fact]
    public void Trigger_and_map_loss_in_edit_mode_close_with_the_save_flag()
    {
        var m = new TriggerStateMachine();
        m.Handle(TriggerEvent.TriggerPressed(_p));
        Click(m, Hit.HubEdit);
        Assert.Equal(MenuCommand.CloseFromEdit, m.Handle(TriggerEvent.TriggerPressed(_p)));

        m.Handle(TriggerEvent.TriggerPressed(_p));
        Click(m, Hit.HubEdit);
        Assert.Equal(MenuCommand.CloseFromEdit, m.Handle(TriggerEvent.MapLost()));
        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.MapLost()));
    }

    [Fact]
    public void Press_on_a_slot_released_on_another_in_edit_mode_does_nothing()
    {
        var m = new TriggerStateMachine();
        m.Handle(TriggerEvent.TriggerPressed(_p));
        Click(m, Hit.HubEdit);
        m.Handle(TriggerEvent.MouseDown(_p, Hit.Slot(1), 0));
        Assert.Equal(MenuCommand.None, m.Handle(TriggerEvent.MouseUp(_p, Hit.Slot(2), SlotKind.Normal, 0, true)));
        Assert.Equal(MenuState.Edit, m.State);
    }
}
