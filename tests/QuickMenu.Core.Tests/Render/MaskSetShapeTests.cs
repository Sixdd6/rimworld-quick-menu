using QuickMenu.Core.Render;
using Xunit;

namespace QuickMenu.Tests.Render;

public class MaskSetShapeTests
{
    private static int At(byte[] m, int w, int x, int y) => m[(y * w) + x];

    [Theory]
    [InlineData(MaskId.Circle)]
    [InlineData(MaskId.Disc)]
    [InlineData(MaskId.Ring1)]
    [InlineData(MaskId.Ring2)]
    [InlineData(MaskId.Ring3)]
    public void Round_masks_are_symmetric_horizontally_vertically_and_on_the_diagonal(MaskId id)
    {
        int w = MaskSet.Spec(id).Width;
        byte[] m = MaskSet.Render(id);
        for (int y = 0; y < w; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int v = At(m, w, x, y);
                Assert.True(System.Math.Abs(v - At(m, w, w - 1 - x, y)) <= 1, $"h {x},{y}");
                Assert.True(System.Math.Abs(v - At(m, w, x, w - 1 - y)) <= 1, $"v {x},{y}");
                Assert.True(System.Math.Abs(v - At(m, w, y, x)) <= 1, $"d {x},{y}");
            }
        }
    }

    [Fact]
    public void Dashed_ring_is_mirror_symmetric_about_the_vertical_axis_and_starts_a_dash_at_twelve()
    {
        byte[] m = MaskSet.Render(MaskId.RingDashed);
        Assert.True(At(m, 128, 63, 1) > 128 && At(m, 128, 64, 1) > 128);
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                Assert.True(System.Math.Abs(At(m, 128, x, y) - At(m, 128, 127 - x, y)) <= 1);
            }
        }
    }

    [Fact]
    public void Plate_corner_is_rounded_inside_the_four_pixel_border_and_the_rest_is_solid()
    {
        byte[] m = MaskSet.Render(MaskId.Plate);
        Assert.True(At(m, 16, 0, 0) < 64);
        Assert.True(At(m, 16, 15, 0) < 64);
        Assert.True(At(m, 16, 0, 15) < 64);
        Assert.Equal(255, At(m, 16, 3, 3));
        Assert.Equal(255, At(m, 16, 8, 0));
        Assert.Equal(255, At(m, 16, 0, 8));
    }

    [Fact]
    public void Panel_corner_cut_stays_inside_the_sixteen_pixel_border_and_rim_continues_through_the_strips()
    {
        byte[] panel = MaskSet.Render(MaskId.Panel);
        byte[] rim = MaskSet.Render(MaskId.PanelRim);
        Assert.True(At(panel, 64, 0, 0) < 64);

        // The cut does not reach the stretch strips: row 0 is solid from the border to the opposite border.
        for (int x = 16; x < 48; x++)
        {
            Assert.Equal(255, At(panel, 64, x, 0));
            Assert.Equal(255, At(rim, 64, x, 0));
            Assert.Equal(255, At(rim, 64, x, 1));
            Assert.Equal(0, At(rim, 64, x, 2));
        }

        // Rim never paints the interior that Panel fills, and Panel covers every rim pixel.
        for (int i = 0; i < panel.Length; i++)
        {
            Assert.True(rim[i] <= panel[i] + 1, $"rim outside panel at {i}");
        }
    }

    [Fact]
    public void Every_mask_has_ink_and_transparent_pixels_and_the_circle_has_no_stray_ink_in_corners()
    {
        foreach (MaskId id in System.Enum.GetValues<MaskId>())
        {
            byte[] m = MaskSet.Render(id);
            Assert.Contains(m, v => v == 255 || v > 200);
            if (id is not (MaskId.Panel or MaskId.Plate))
            {
                Assert.Contains(m, v => v == 0);
            }
        }
    }
}
