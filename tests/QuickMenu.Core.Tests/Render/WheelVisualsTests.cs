using QuickMenu.Core.Geometry;
using QuickMenu.Core.Input;
using QuickMenu.Core.Layout;
using QuickMenu.Core.Render;
using Xunit;

namespace QuickMenu.Tests.Render;

/// <summary>design.md S1, S4 states 1/2/3/6, S4a and §1.1 label plate: the pure decisions of the wheel renderer (T-29).</summary>
public class WheelVisualsTests
{
    private static readonly Hit _none = Hit.Outside;

    [Fact]
    public void Slot_is_normal_when_not_hovered_or_pressed()
    {
        Assert.Equal(SlotVisualState.Normal, WheelVisuals.Slot(2, SlotKind.Normal, Hit.Slot(1), _none));
        Assert.Equal(SlotVisualState.Normal, WheelVisuals.Slot(2, SlotKind.Submenu, Hit.HubClose, Hit.HubClose));
    }

    [Fact]
    public void Slot_is_hover_when_the_pointer_is_over_it()
    {
        Assert.Equal(SlotVisualState.Hover, WheelVisuals.Slot(2, SlotKind.Normal, Hit.Slot(2), _none));
    }

    [Fact]
    public void Slot_is_pressed_only_while_pressed_on_it_and_still_over_it()
    {
        Assert.Equal(SlotVisualState.Pressed, WheelVisuals.Slot(2, SlotKind.Normal, Hit.Slot(2), Hit.Slot(2)));
        Assert.Equal(SlotVisualState.Normal, WheelVisuals.Slot(2, SlotKind.Normal, Hit.Slot(3), Hit.Slot(2)));
        Assert.Equal(SlotVisualState.Hover, WheelVisuals.Slot(2, SlotKind.Normal, Hit.Slot(2), Hit.Slot(3)));
    }

    [Fact]
    public void Empty_slot_never_hovers_or_presses()
    {
        Assert.Equal(SlotVisualState.Empty, WheelVisuals.Slot(2, SlotKind.Empty, Hit.Slot(2), Hit.Slot(2)));
        Assert.Equal(SlotVisualState.Empty, WheelVisuals.Slot(2, SlotKind.Empty, _none, _none));
    }

    [Fact]
    public void Hub_main_area_has_no_hover_until_hover_is_enabled()
    {
        Assert.Equal(HubPartState.Normal, WheelVisuals.HubMain(Hit.HubClose, _none, hoverEnabled: false));
        Assert.Equal(HubPartState.Hover, WheelVisuals.HubMain(Hit.HubClose, _none, hoverEnabled: true));
        Assert.Equal(HubPartState.Normal, WheelVisuals.HubMain(Hit.Slot(0), _none, hoverEnabled: true));
    }

    [Fact]
    public void Hub_main_area_shows_pressed_while_pressed_on_it_even_before_moving()
    {
        Assert.Equal(HubPartState.Pressed, WheelVisuals.HubMain(Hit.HubClose, Hit.HubClose, hoverEnabled: false));
        Assert.Equal(HubPartState.Normal, WheelVisuals.HubMain(Hit.Inside, Hit.HubClose, hoverEnabled: true));
    }

    [Fact]
    public void Hub_sub_button_follows_its_own_hit_and_the_hover_gate()
    {
        Assert.Equal(HubPartState.Normal, WheelVisuals.HubSub(Hit.HubEdit, _none, hoverEnabled: false));
        Assert.Equal(HubPartState.Hover, WheelVisuals.HubSub(Hit.HubEdit, _none, hoverEnabled: true));
        Assert.Equal(HubPartState.Pressed, WheelVisuals.HubSub(Hit.HubEdit, Hit.HubEdit, hoverEnabled: true));
        Assert.Equal(HubPartState.Normal, WheelVisuals.HubSub(Hit.HubClose, _none, hoverEnabled: true));
    }

    [Fact]
    public void Plate_shows_the_hovered_filled_slot()
    {
        SlotKind[] kinds = [SlotKind.Normal, SlotKind.Empty, SlotKind.Disabled];

        Assert.Equal(PlateContent.Slot, WheelVisuals.Plate(Hit.Slot(0), false, false, kinds, 3));
        Assert.Equal(PlateContent.Slot, WheelVisuals.Plate(Hit.Slot(2), false, false, kinds, 3));
        Assert.Equal(PlateContent.None, WheelVisuals.Plate(Hit.Slot(1), true, false, kinds, 3));
        Assert.Equal(PlateContent.None, WheelVisuals.Plate(Hit.Slot(2), true, false, kinds, 2));
        Assert.Equal(PlateContent.None, WheelVisuals.Plate(Hit.Slot(-1), true, false, kinds, 3));
        Assert.Equal(PlateContent.None, WheelVisuals.Plate(Hit.Slot(5), true, false, kinds, 9));
    }

    [Fact]
    public void Plate_shows_hub_labels_only_after_hover_is_enabled()
    {
        SlotKind[] kinds = [];

        Assert.Equal(PlateContent.None, WheelVisuals.Plate(Hit.HubClose, false, true, kinds, 0));
        Assert.Equal(PlateContent.HubClose, WheelVisuals.Plate(Hit.HubClose, true, true, kinds, 0));
        Assert.Equal(PlateContent.HubSub, WheelVisuals.Plate(Hit.HubEdit, true, true, kinds, 0));
        Assert.Equal(PlateContent.None, WheelVisuals.Plate(Hit.HubEdit, true, false, kinds, 0));
        Assert.Equal(PlateContent.None, WheelVisuals.Plate(Hit.Inside, true, true, kinds, 0));
        Assert.Equal(PlateContent.None, WheelVisuals.Plate(Hit.Plate, true, true, kinds, 0));
    }

    [Fact]
    public void Plate_rect_sits_under_the_disc_centred_on_the_hub()
    {
        // B = 56, 2 rings: D = 168. Text 100 wide → plate 112 wide, top = cy + D + 8.
        float d = LayoutConstants.DiscRadius(56f, 2);
        Rect2 plate = WheelVisuals.PlateRect(new Vec2(500f, 400f), d, 100f, 30f);

        Assert.Equal(new Rect2(444f, 576f, 112f, 30f), plate);
    }

    [Fact]
    public void Plate_rect_is_capped_at_the_disc_diameter()
    {
        Rect2 plate = WheelVisuals.PlateRect(new Vec2(500f, 400f), 168f, 1000f, 30f);

        Assert.Equal(336f, plate.Width);
        Assert.Equal(332f, plate.X);
    }

    [Fact]
    public void Plate_text_width_is_the_plate_width_minus_padding()
    {
        Assert.Equal(324f, WheelVisuals.PlateMaxTextWidth(168f));
    }

    [Fact]
    public void Icon_rect_is_a_centred_square_of_three_quarters_of_B()
    {
        Assert.Equal(new Rect2(79f, 179f, 42f, 42f), WheelVisuals.IconRect(new Vec2(100f, 200f), 56f));
    }

    [Fact]
    public void Hub_glyph_and_sub_button_centres_follow_S4a()
    {
        var c = new Vec2(100f, 200f);

        Assert.Equal(100f, WheelVisuals.HubGlyphCenter(c, 56f).X);
        Assert.Equal(200f - (0.12f * 56f), WheelVisuals.HubGlyphCenter(c, 56f).Y, 4);
        Assert.Equal(new Vec2(100f, 216f), WheelVisuals.HubSubCenter(c, 56f));
    }

    [Fact]
    public void Transform_identity_keeps_points_and_lengths()
    {
        var t = RenderTransform.Identity(new Vec2(10f, 20f));

        Assert.Equal(new Vec2(33f, 44f), t.Point(new Vec2(33f, 44f)));
        Assert.Equal(7f, t.Length(7f));
        Assert.Equal(new Rect2(1f, 2f, 3f, 4f), t.Rect(new Rect2(1f, 2f, 3f, 4f)));
    }

    [Fact]
    public void Transform_scales_around_the_model_centre_and_moves_it_to_the_origin()
    {
        // The settings preview (T-59) draws at 50 % with the hub at the preview's centre.
        var t = new RenderTransform(new Vec2(500f, 400f), new Vec2(100f, 80f), 0.5f);

        Assert.Equal(new Vec2(100f, 80f), t.Point(new Vec2(500f, 400f)));
        Assert.Equal(new Vec2(132f, 80f), t.Point(new Vec2(564f, 400f)));
        Assert.Equal(28f, t.Length(56f));
        Assert.Equal(new Rect2(78f, 168f, 56f, 15f), t.Rect(new Rect2(456f, 576f, 112f, 30f)));
    }

    [Fact]
    public void Derived_tokens_match_S4_and_S4a()
    {
        // Empty: slotBg at 70 %. Hub main area: slotBg at 60 %. Hover overlay: 8 % white.
        Assert.Equal(StyleTokens.SlotBg.Rgb, StyleTokens.SlotBgEmpty.Rgb);
        Assert.Equal(179, StyleTokens.SlotBgEmpty.A);
        Assert.Equal(StyleTokens.SlotBg.Rgb, StyleTokens.HubBg.Rgb);
        Assert.Equal(153, StyleTokens.HubBg.A);
        Assert.Equal(0xFFFFFFu, StyleTokens.HoverOverlay.Rgb);
        Assert.Equal(20, StyleTokens.HoverOverlay.A);
    }
}
