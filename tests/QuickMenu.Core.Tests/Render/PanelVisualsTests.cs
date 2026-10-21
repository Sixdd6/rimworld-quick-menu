using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;
using QuickMenu.Core.Render;
using Xunit;

namespace QuickMenu.Tests.Render;

/// <summary>design.md §1.1 grid panel and label plate, §2 grid panel component (9-slice <c>Panel</c> + <c>PanelRim</c>): T-54.</summary>
public class PanelVisualsTests
{
    // Worked default (design.md §1.1): grid 5 × 3 at B = 56 around (500, 400): panel 336 × 208.
    private static readonly Rect2 _panel = new(332f, 296f, 336f, 208f);

    [Fact]
    public void Plate_is_centred_under_the_panel_with_the_plate_gap()
    {
        Rect2 plate = PanelVisuals.PlateRect(_panel, 88f, 30f);

        Assert.Equal(new Rect2(450f, 504f + LayoutConstants.PlateGap, 100f, 30f), plate);
    }

    [Fact]
    public void Plate_is_at_most_as_wide_as_the_panel()
    {
        Rect2 plate = PanelVisuals.PlateRect(_panel, 1000f, 30f);

        Assert.Equal(_panel.X, plate.X);
        Assert.Equal(_panel.Width, plate.Width);
    }

    [Fact]
    public void Plate_text_fits_inside_the_widest_plate()
    {
        float max = PanelVisuals.PlateMaxTextWidth(_panel);

        Assert.Equal(336f - WheelVisuals.PlatePadding, max);
        Assert.Equal(_panel.Width, PanelVisuals.PlateRect(_panel, max, 30f).Width);
    }

    [Fact]
    public void Sub_grid_plate_is_centred_on_its_own_panel()
    {
        var sub = new Rect2(676f, 296f, 144f, 80f);

        Rect2 plate = PanelVisuals.PlateRect(sub, 40f, 30f);

        Assert.Equal(676f + 72f - 26f, plate.X);
        Assert.Equal(376f + LayoutConstants.PlateGap, plate.Y);
    }

    [Fact]
    public void Rim_outlines_sit_one_px_outside_and_inside_the_two_px_band()
    {
        // PanelRim is a 2 px band on the inside of its rect: the rect grown by 1 px puts the outer outline 1 px
        // outside the band, the rect shrunk by 1 px puts the inner one 1 px inside it (design.md §2 rimOutline).
        Assert.Equal(new Rect2(331f, 295f, 338f, 210f), PanelVisuals.OuterOutlineRect(_panel));
        Assert.Equal(new Rect2(333f, 297f, 334f, 206f), PanelVisuals.InnerOutlineRect(_panel));
        Assert.Equal(2f, MaskSet.PanelRimStroke);
        Assert.Equal(PanelVisuals.OutlineWidth * 2f, MaskSet.PanelRimStroke);
    }

    [Fact]
    public void Corner_radius_fits_the_atlas_border()
    {
        // spike S-7: DrawAtlas keeps round corners only while the radius is at most a quarter of the atlas.
        Assert.True(MaskSet.PanelRadius <= MaskSet.PanelAtlas / 4f);
        Assert.Equal(MaskSet.PanelAtlas / 2f, PanelVisuals.MinAtlasSide);
    }

    [Fact]
    public void Smallest_panel_at_full_size_still_takes_the_nine_slice()
    {
        // Smallest B (36), a sub-grid of 2 options: 2 columns, 1 row.
        float side = LayoutConstants.Pitch(36f) - LayoutConstants.Gap + (2f * LayoutConstants.RimPad);
        var smallest = new Rect2(0f, 0f, 2f * side, side);

        Assert.True(PanelVisuals.FitsAtlas(PanelVisuals.InnerOutlineRect(smallest)));
    }

    [Fact]
    public void Too_small_rects_do_not_take_the_nine_slice()
    {
        Assert.False(PanelVisuals.FitsAtlas(new Rect2(0f, 0f, 31f, 200f)));
        Assert.False(PanelVisuals.FitsAtlas(new Rect2(0f, 0f, 200f, 31.5f)));
        Assert.True(PanelVisuals.FitsAtlas(new Rect2(0f, 0f, 32f, 32f)));
    }
}
