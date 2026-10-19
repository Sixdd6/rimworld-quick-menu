using QuickMenu.Core.Geometry;
using QuickMenu.Core.Input;
using QuickMenu.Core.Layout;
using QuickMenu.Core.Render;
using Xunit;

namespace QuickMenu.Tests.Render;

/// <summary>
/// design.md S1 (dimmed menu), S3 and S4 states 4, 5, 12, 13 and 14 (T-42): rings, badges, veils and the dimmed main
/// menu behind a satellite.
/// </summary>
public class SlotStateTests
{
    private const float B = 56f;

    [Fact]
    public void Ring_follows_the_base_state_without_decorations()
    {
        Assert.Equal(SlotRing.Border1, WheelVisuals.Ring(SlotVisualState.Normal, SlotDecor.None));
        Assert.Equal(SlotRing.Accent2, WheelVisuals.Ring(SlotVisualState.Hover, SlotDecor.None));
        Assert.Equal(SlotRing.Accent2, WheelVisuals.Ring(SlotVisualState.Pressed, SlotDecor.None));
        Assert.Equal(SlotRing.EmptyBorder1, WheelVisuals.Ring(SlotVisualState.Empty, SlotDecor.None));
    }

    [Fact]
    public void Last_used_material_keeps_its_3px_ring_in_every_state()
    {
        Assert.Equal(SlotRing.Accent3, WheelVisuals.Ring(SlotVisualState.Normal, SlotDecor.LastUsed));
        Assert.Equal(SlotRing.Accent3, WheelVisuals.Ring(SlotVisualState.Hover, SlotDecor.LastUsed));
        Assert.Equal(SlotRing.Accent3, WheelVisuals.Ring(SlotVisualState.Pressed, SlotDecor.LastUsed));
    }

    [Fact]
    public void Open_parent_has_a_2px_accent_ring_even_when_not_hovered()
    {
        Assert.Equal(SlotRing.Accent2, WheelVisuals.Ring(SlotVisualState.Normal, SlotDecor.OpenParent));
        Assert.Equal(SlotRing.Accent2, WheelVisuals.Ring(SlotVisualState.Hover, SlotDecor.OpenParent | SlotDecor.SubMenu));
    }

    [Fact]
    public void Empty_slots_ignore_decorations()
    {
        SlotDecor all = SlotDecor.SubMenu | SlotDecor.Material | SlotDecor.LastUsed | SlotDecor.OpenParent;

        Assert.Equal(SlotRing.EmptyBorder1, WheelVisuals.Ring(SlotVisualState.Empty, all));
        Assert.False(WheelVisuals.ShowsBadge(SlotBadge.SubMenu, SlotKind.Empty, all));
        Assert.False(WheelVisuals.ShowsBadge(SlotBadge.Material, SlotKind.Reserved, all));
        Assert.False(WheelVisuals.ShowsBadge(SlotBadge.Lock, SlotKind.Empty, all));
    }

    [Fact]
    public void Sub_menu_and_material_badges_combine_with_normal_and_greyed()
    {
        SlotDecor both = SlotDecor.SubMenu | SlotDecor.Material;

        Assert.True(WheelVisuals.ShowsBadge(SlotBadge.SubMenu, SlotKind.Submenu, both));
        Assert.True(WheelVisuals.ShowsBadge(SlotBadge.Material, SlotKind.Normal, both));
        Assert.False(WheelVisuals.ShowsBadge(SlotBadge.Lock, SlotKind.Normal, both));

        Assert.True(WheelVisuals.ShowsBadge(SlotBadge.SubMenu, SlotKind.Disabled, both));
        Assert.True(WheelVisuals.ShowsBadge(SlotBadge.Material, SlotKind.Disabled, both));
        Assert.True(WheelVisuals.ShowsBadge(SlotBadge.Lock, SlotKind.Disabled, both));

        Assert.False(WheelVisuals.ShowsBadge(SlotBadge.SubMenu, SlotKind.Normal, SlotDecor.Material));
        Assert.False(WheelVisuals.ShowsBadge(SlotBadge.Material, SlotKind.Normal, SlotDecor.SubMenu));
    }

    [Fact]
    public void Badges_sit_on_their_45_degree_diagonals_at_radius_B_over_2()
    {
        var c = new Vec2(100f, 200f);
        float d = 28f * 0.70710678f;

        AssertNear(new Vec2(100f + d, 200f - d), WheelVisuals.BadgeCentre(SlotBadge.SubMenu, c, B));
        AssertNear(new Vec2(100f + d, 200f + d), WheelVisuals.BadgeCentre(SlotBadge.Material, c, B));
        AssertNear(new Vec2(100f - d, 200f + d), WheelVisuals.BadgeCentre(SlotBadge.Lock, c, B));
    }

    [Fact]
    public void Material_dot_uses_the_last_used_stuff_colour_or_text_muted()
    {
        var wood = new Rgba32(133, 97, 67, 255);

        Assert.Equal(wood, WheelVisuals.MaterialDotColor(wood));
        Assert.Equal(StyleTokens.TextMuted, WheelVisuals.MaterialDotColor(null));
    }

    [Fact]
    public void Material_dot_is_opaque_even_for_a_translucent_stuff_colour()
    {
        Assert.Equal(new Rgba32(10, 20, 30, 255), WheelVisuals.MaterialDotColor(new Rgba32(10, 20, 30, 40)));
    }

    [Fact]
    public void Badge_outline_is_one_px_larger_on_each_side()
    {
        Assert.Equal(16f / 14f, WheelVisuals.OutlineScale(14f), 5);
        Assert.Equal(1f, WheelVisuals.OutlineScale(0f));
    }

    [Fact]
    public void Main_menu_is_drawn_at_45_percent_while_a_satellite_is_open()
    {
        Assert.Equal(1f, WheelVisuals.MenuAlpha(dimmed: false));
        Assert.Equal(StyleTokens.DimmedMenuAlpha, WheelVisuals.MenuAlpha(dimmed: true));
    }

    [Fact]
    public void Dimmed_menu_does_not_react_to_the_mouse()
    {
        Assert.Equal(Hit.Outside, WheelVisuals.Interactive(Hit.Slot(3), dimmed: true));
        Assert.Equal(Hit.Outside, WheelVisuals.Interactive(Hit.HubEdit, dimmed: true));
        Assert.Equal(Hit.Slot(3), WheelVisuals.Interactive(Hit.Slot(3), dimmed: false));
    }

    [Fact]
    public void Icon_veil_greys_disabled_icons_and_dims_icons_of_a_dimmed_menu()
    {
        // Command.DrawIcon sets its own colour, so icons are faded by a slotBg veil drawn over them.
        Assert.Equal(0f, WheelVisuals.IconVeilAlpha(SlotKind.Normal, dimmed: false));
        Assert.Equal(StyleTokens.UnavailableVeilAlpha, WheelVisuals.IconVeilAlpha(SlotKind.Disabled, dimmed: false));
        Assert.Equal(1f - StyleTokens.DimmedMenuAlpha, WheelVisuals.IconVeilAlpha(SlotKind.Submenu, dimmed: true), 5);

        // Both: the icon keeps (1 − 0.6) of its strength, then 45 % of that.
        float both = 1f - ((1f - StyleTokens.UnavailableVeilAlpha) * StyleTokens.DimmedMenuAlpha);
        Assert.Equal(both, WheelVisuals.IconVeilAlpha(SlotKind.Disabled, dimmed: true), 5);
    }

    [Fact]
    public void Satellite_hub_is_the_parent_slot_in_hover_and_pressed_states()
    {
        Assert.Equal(SlotVisualState.Normal, WheelVisuals.SatelliteHub(Hit.Slot(0), Hit.Outside));
        Assert.Equal(SlotVisualState.Hover, WheelVisuals.SatelliteHub(Hit.HubEdit, Hit.Outside));
        Assert.Equal(SlotVisualState.Pressed, WheelVisuals.SatelliteHub(Hit.HubEdit, Hit.HubEdit));
        Assert.Equal(SlotVisualState.Normal, WheelVisuals.SatelliteHub(Hit.Inside, Hit.HubEdit));
    }

    [Fact]
    public void Material_dot_size_is_018_B()
    {
        Assert.Equal(0.18f * B, WheelVisuals.MaterialDotSize(B), 4);
    }

    private static void AssertNear(Vec2 expected, Vec2 actual)
    {
        Assert.Equal(expected.X, actual.X, 3);
        Assert.Equal(expected.Y, actual.Y, 3);
    }
}
