using System;
using QuickMenu.Core.Render;
using Xunit;

namespace QuickMenu.Tests.Render;

/// <summary>
/// Badge and hub-icon glyphs (design.md S4 badges, S4a hub icons, §3): 64 px grid, 4 px margin, 6 px strokes,
/// final orientation (no rotation at draw time). Rows are top-first, so "top" means small y.
/// </summary>
public class GlyphsTests
{
    private const int S = 64;

    public static readonly TheoryData<MaskId> AllGlyphs =
    [
        MaskId.SubMenuBadge, MaskId.Lock, MaskId.IconEdit, MaskId.IconDone, MaskId.PinMarker,
    ];

    private static int At(byte[] m, int x, int y) => m[(y * S) + x];

    private static byte[] R(MaskId id) => MaskSet.Render(id);

    /// <summary>Alpha mass in pixels over the half-open box [x0, x1) × [y0, y1).</summary>
    private static double Mass(byte[] m, int x0, int y0, int x1, int y1, int size = S)
    {
        double s = 0;
        for (int y = y0; y < y1; y++)
        {
            for (int x = x0; x < x1; x++)
            {
                s += m[(y * size) + x] / 255.0;
            }
        }

        return s;
    }

    // ---- Common rules -----------------------------------------------------------------------------

    [Fact]
    public void GlyphRange_IsTheFourMenuGlyphsPlusThePinMarker_WithoutCaretOrPlus()
    {
        // The insert caret and the edit-mode "+" were removed with the insert cursor and the Add slot (T-55).
        // The pin marker (T-89) follows the menu glyphs; it is sized by PinMarkerSizing, not by B.
        Assert.Equal(MaskId.SubMenuBadge, Glyphs.First);
        Assert.Equal(MaskId.PinMarker, Glyphs.Last);
        Assert.Equal(MaskId.IconDone, Glyphs.LastMenuGlyph);
        Assert.Equal(5, Glyphs.Count);
        Assert.Equal(4, Glyphs.MenuGlyphCount);
        Assert.Equal(Glyphs.Count, AllGlyphs.Count);
        Assert.Equal(MaskSet.Count - 1, (int)Glyphs.Last);
        Assert.False(Enum.IsDefined(typeof(MaskId), "Caret"));
        Assert.False(Enum.IsDefined(typeof(MaskId), "IconAdd"));
        for (MaskId id = Glyphs.First; id <= Glyphs.Last; id++)
        {
            Assert.True(Glyphs.IsGlyph(id));
        }
    }

    [Theory]
    [MemberData(nameof(AllGlyphs))]
    public void Glyph_IsA64PxMask_WithTheDesignGridParameters(MaskId id)
    {
        MaskSpec spec = MaskSet.Spec(id);

        Assert.True(Glyphs.IsGlyph(id));
        Assert.Equal(64, spec.Width);
        Assert.Equal(64, spec.Height);
        Assert.Equal(0, spec.SliceBorder);
        Assert.Equal(6f, spec.Stroke);
        Assert.Equal(64, Glyphs.GridSize);
        Assert.Equal(4, Glyphs.Margin);
        Assert.Equal(6f, Glyphs.Stroke);
    }

    [Theory]
    [MemberData(nameof(AllGlyphs))]
    public void Glyph_LeavesTheFourPixelMarginEmpty_AndHasInk(MaskId id)
    {
        byte[] m = R(id);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                bool inMargin = x < 4 || y < 4 || x >= S - 4 || y >= S - 4;
                if (inMargin)
                {
                    Assert.True(At(m, x, y) == 0, $"{id} margin pixel {x},{y} = {At(m, x, y)}");
                }
            }
        }

        Assert.True(Mass(m, 0, 0, S, S) > 150, $"{id} has too little ink");
    }

    [Theory]
    [MemberData(nameof(AllGlyphs))]
    public void Glyph_IsDeterministic_AndSpanMatchesArray(MaskId id)
    {
        byte[] a = MaskSet.Render(id);
        byte[] b = Glyphs.Render(id, S);
        byte[] buffer = new byte[(S * S) + 7];
        buffer.AsSpan().Fill(0xAB);
        Glyphs.Render(id, S, buffer);

        Assert.Equal(a, b);
        Assert.True(buffer.AsSpan(0, a.Length).SequenceEqual(a));
        Assert.Equal(0xAB, buffer[S * S]);
    }

    [Theory]
    [MemberData(nameof(AllGlyphs))]
    public void Glyph_IsAntiAliased_WithFullyOpaqueInk(MaskId id)
    {
        byte[] m = R(id);
        int full = 0;
        int partial = 0;
        foreach (byte v in m)
        {
            if (v == 255)
            {
                full++;
            }
            else if (v > 0)
            {
                partial++;
            }
        }

        Assert.True(full > 50, $"{id}: {full} opaque pixels");
        Assert.True(partial > 10, $"{id}: {partial} edge pixels");
    }

    [Fact]
    public void Glyph_ScalesWithSize_KeepingTheMarginProportional()
    {
        const int Big = 128;
        byte[] m = Glyphs.Render(MaskId.Lock, Big);

        Assert.Equal(Big * Big, m.Length);
        Assert.Equal(0.0, Mass(m, 0, 0, Big, 8, Big));
        Assert.Equal(255, m[(64 * Big) + 64]); // inside the body

        // Strokes scale too: the shackle crown on the centre column is ≈ 12 px tall at 128 (6 px at 64).
        double crown = 0;
        for (int y = 0; y < 40; y++)
        {
            crown += m[(y * Big) + 64] / 255.0;
        }

        Assert.InRange(crown, 11.5, 12.5);
    }

    [Fact]
    public void Glyph_RendersAtBadgeSizes_DownToMinSize()
    {
        // Badges are drawn at 0.18–0.25 B (≈ 7–14 px at B = 36–56) and rasterized at that size (T-28).
        for (int size = Glyphs.MinSize; size <= 14; size++)
        {
            for (MaskId id = Glyphs.First; id <= Glyphs.Last; id++)
            {
                byte[] m = Glyphs.Render(id, size);
                double ink = 0;
                foreach (byte a in m)
                {
                    ink += a / 255.0;
                }

                // Ink scales with the area: the same share of the box as at 64 px, within a loose band.
                double ink64 = 0;
                foreach (byte a in Glyphs.Render(id, 64))
                {
                    ink64 += a / 255.0;
                }

                double expected = ink64 * size * size / (64.0 * 64.0);
                Assert.InRange(ink, expected * 0.75, (expected * 1.25) + 1.0);
            }
        }
    }

    [Theory]
    [InlineData(Glyphs.MinSize - 1)]
    [InlineData(0)]
    [InlineData(-64)]
    public void Glyph_RejectsTooSmallSize(int size)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Glyphs.Render(MaskId.IconDone, size));
    }

    [Theory]
    [InlineData(MaskId.Circle)]
    [InlineData(MaskId.Plate)] // Glyphs.First - 1
    [InlineData(Glyphs.Last + 1)]
    [InlineData((MaskId)99)]
    public void Glyph_RejectsNonGlyphIds(MaskId id)
    {
        Assert.False(Glyphs.IsGlyph(id));
        Assert.Throws<ArgumentOutOfRangeException>(() => Glyphs.Render(id, S));
    }

    [Fact]
    public void Glyph_RejectsShortDestination()
    {
        byte[] small = new byte[(S * S) - 1];
        Assert.Throws<ArgumentException>(() => Glyphs.Render(MaskId.Lock, S, small));
    }

    [Fact]
    [Trait("Category", "Perf")]
    public void Glyph_SpanOverload_AllocatesNothing()
    {
        byte[] buffer = new byte[S * S];
        for (MaskId id = Glyphs.First; id <= Glyphs.Last; id++)
        {
            Glyphs.Render(id, S, buffer);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (MaskId id = Glyphs.First; id <= Glyphs.Last; id++)
        {
            Glyphs.Render(id, S, buffer);
        }

        Assert.Equal(0L, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    // ---- Badges (S4: 1:30 sub-menu, 7:30 lock) ------------------------------------------------------

    [Fact]
    public void SubMenuBadge_FillsTheTopRightCornerTriangle()
    {
        byte[] m = R(MaskId.SubMenuBadge);

        Assert.Equal(255, At(m, 56, 7));  // outer corner (towards 1:30)
        Assert.Equal(255, At(m, 40, 20));
        Assert.Equal(0, At(m, 8, 56));    // the opposite corner is empty
        Assert.Equal(0, At(m, 20, 40));   // below the diagonal is empty
        Assert.True(Mass(m, 32, 0, S, 32) > 20 * Mass(m, 0, 32, 32, S) + 300);

        // Mirror-symmetric about the anti-diagonal through the outer corner (the hypotenuse runs top-left to bottom-right).
        AssertSymmetric(m, (x, y) => (S - 1 - y, S - 1 - x), "anti-diagonal fold");
    }

    [Fact]
    public void Lock_HasABodyBelowAndAShackleAbove_WithAnOpenHole()
    {
        byte[] m = R(MaskId.Lock);

        Assert.Equal(255, At(m, 32, 45));  // body
        Assert.Equal(255, At(m, 10, 50));
        Assert.Equal(0, At(m, 32, 22));    // hole inside the shackle
        Assert.Equal(0, At(m, 8, 12));     // beside the shackle
        Assert.True(Mass(m, 0, 32, S, S) > 2.0 * Mass(m, 0, 0, S, 32));

        // Shackle crown: one 6 px stroke on the centre column above the hole.
        Assert.InRange(Mass(m, 32, 0, 33, 20), 5.5, 6.5);

        AssertSymmetric(m, (x, y) => (S - 1 - x, y), "horizontal flip");
    }

    // ---- Hub icons (S4a) --------------------------------------------------------------------------

    [Fact]
    public void IconDone_IsACheckWithTheShortArmLeftAndTheVertexLow()
    {
        byte[] m = R(MaskId.IconDone);

        Assert.Equal(255, At(m, 25, 47));  // vertex near the bottom
        Assert.Equal(255, At(m, 52, 19));  // long arm reaches the top right
        Assert.Equal(255, At(m, 12, 34));  // short arm on the left
        Assert.Equal(0, At(m, 12, 12));    // top-left empty
        Assert.Equal(0, At(m, 52, 52));    // bottom-right empty
        Assert.Equal(0, At(m, 32, 32));    // inside the V
        Assert.True(Mass(m, 32, 0, S, S) > Mass(m, 0, 0, 32, S), "long arm should carry more ink");
    }

    [Fact]
    public void IconEdit_IsAPencilWithTheTipBottomLeft_SymmetricAboutItsAxis()
    {
        byte[] m = R(MaskId.IconEdit);

        Assert.Equal(255, At(m, 48, 15));  // body near the top right
        Assert.Equal(255, At(m, 32, 31));  // body on the axis
        Assert.True(At(m, 10, 53) > 0);     // tip reaches the bottom-left
        Assert.Equal(0, At(m, 12, 12));    // off-axis corners are empty
        Assert.Equal(0, At(m, 52, 52));

        // The tip tapers: less ink in the bottom-left 16 px corner than in a same-size box on the body.
        Assert.True(Mass(m, 4, 44, 20, 60) < 0.6 * Mass(m, 36, 12, 52, 28));

        AssertSymmetric(m, (x, y) => (S - 1 - y, S - 1 - x), "fold about the pencil axis");
    }

    // ---- Pin marker (T-89, design.md §2 "Architect pin marker") --------------------------------------

    public static readonly TheoryData<int> MarkerSizes = [8, 9, 10, 11, 12, 15, 16, 20, 22, 24, 30, 32];

    [Fact]
    public void PinMarker_IsAPushPin_NeedleBottomLeft_HeadTopRight_SymmetricAboutItsAxis()
    {
        byte[] m = R(MaskId.PinMarker);

        Assert.Equal(255, At(m, 44, 19));  // head (cap) on the axis, top right
        Assert.Equal(255, At(m, 34, 28));  // waist on the axis
        Assert.Equal(255, At(m, 19, 43));  // needle on the axis
        Assert.True(At(m, 10, 53) > 0);    // the needle point reaches the bottom-left
        Assert.Equal(0, At(m, 10, 10));    // the off-axis corners are empty
        Assert.Equal(0, At(m, 53, 53));
        Assert.Equal(0, At(m, 22, 51));    // beside the needle, below the flange

        // The head end carries most of the ink; the needle is thin.
        Assert.True(Mass(m, 32, 0, S, 32) > 2.0 * Mass(m, 0, 32, 32, S), "the head should outweigh the needle and flange");

        // The flange is wider than the waist: across the axis at the flange more ink than at the waist.
        Assert.True(AcrossAxis(m, 27.5) > AcrossAxis(m, 37) + 10, "flange should be wider than the waist");

        AssertSymmetric(m, (x, y) => (S - 1 - y, S - 1 - x), "fold about the pin axis");
    }

    [Theory]
    [MemberData(nameof(MarkerSizes))]
    public void PinMarker_AtDrawnSizes_HasInk_AndAClearOuterBorder(int size)
    {
        // Drawn at 10–15 UI px × UI scale 1–2 (PinMarkerSizing), so rasterized at 8–32 px.
        byte[] m = Glyphs.Render(MaskId.PinMarker, size);

        double ink = Mass(m, 0, 0, size, size, size);
        double ink64 = Mass(R(MaskId.PinMarker), 0, 0, S, S);
        double expected = ink64 * size * size / (S * S);
        Assert.InRange(ink, expected * 0.75, (expected * 1.25) + 1.0);
        Assert.Contains(m, a => a == 255);

        // The 4/64 margin is ≥ 1 px from 16 px up: the outer pixel ring is then empty. Below that the margin is a
        // fraction of a pixel and only a faint edge may touch it.
        int limit = size >= 16 ? 0 : 40;
        for (int i = 0; i < size; i++)
        {
            Assert.True(m[i] <= limit && m[((size - 1) * size) + i] <= limit, $"size {size}: top/bottom row at {i}");
            Assert.True(m[i * size] <= limit && m[(i * size) + size - 1] <= limit, $"size {size}: left/right column at {i}");
        }
    }

    [Theory]
    [MemberData(nameof(MarkerSizes))]
    public void PinMarker_IsDeterministic_AndSpanMatchesArray_AtDrawnSizes(int size)
    {
        byte[] a = Glyphs.Render(MaskId.PinMarker, size);
        byte[] buffer = new byte[(size * size) + 3];
        buffer.AsSpan().Fill(0xAB);
        Glyphs.Render(MaskId.PinMarker, size, buffer);

        Assert.Equal(a, Glyphs.Render(MaskId.PinMarker, size));
        Assert.True(buffer.AsSpan(0, a.Length).SequenceEqual(a));
        Assert.Equal(0xAB, buffer[size * size]);
    }

    [Theory]
    [MemberData(nameof(AllGlyphs))]
    public void RenderOutline_WithoutPadOrGrow_EqualsRender(MaskId id)
    {
        byte[] outline = new byte[16 * 16];
        Glyphs.RenderOutline(id, 16, 0, 0f, outline);

        Assert.Equal(Glyphs.Render(id, 16), outline);
    }

    [Theory]
    [InlineData(10, 1, 1f)]
    [InlineData(15, 1, 1f)]
    [InlineData(17, 2, 1.25f)]
    [InlineData(23, 2, 1.5f)]
    [InlineData(30, 2, 2f)]
    public void PinMarkerOutline_CoversTheGlyph_GrowsUniformly_AndStaysInsideItsCanvas(int size, int pad, float grow)
    {
        int side = size + (2 * pad);
        byte[] fill = Glyphs.Render(MaskId.PinMarker, size);
        byte[] outline = new byte[side * side];
        Glyphs.RenderOutline(MaskId.PinMarker, size, pad, grow, outline);

        // Under every fill pixel the outline is at least as opaque (it is a dilation), and opaque wherever the fill has ink.
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int f = fill[(y * size) + x];
                int o = outline[((y + pad) * side) + x + pad];
                Assert.True(o >= f, $"{x},{y}: outline {o} < fill {f}");
                if (f > 0)
                {
                    Assert.True(o >= 200, $"{x},{y}: outline {o} under fill {f}");
                }
            }
        }

        // It adds about perimeter × grow of ink, and the canvas border stays empty.
        Assert.True(Mass(outline, 0, 0, side, side, side) > Mass(fill, 0, 0, size, size, size) + (grow * size));
        for (int i = 0; i < side; i++)
        {
            Assert.True(outline[i] == 0 && outline[((side - 1) * side) + i] == 0, $"row border at {i}");
            Assert.True(outline[i * side] == 0 && outline[(i * side) + side - 1] == 0, $"column border at {i}");
        }

        // Deterministic.
        byte[] grown = new byte[side * side];
        Glyphs.RenderOutline(MaskId.PinMarker, size, pad, grow, grown);
        Assert.Equal(outline, grown);
    }

    [Fact]
    public void PinMarkerOutline_GivesTheNeedleTheFullOutlineWidth()
    {
        // A scaled copy (the menu badges' outline) would give the 1.4 px needle almost no outline; the dilation gives it
        // 1 px on each side: across the needle the outline carries about 2 px more ink than the fill.
        const int Size = 15;
        const int Pad = 1;
        const int Side = Size + (2 * Pad);
        byte[] fill = Glyphs.Render(MaskId.PinMarker, Size);
        byte[] outline = new byte[Side * Side];
        Glyphs.RenderOutline(MaskId.PinMarker, Size, Pad, 1f, outline);

        // The needle crosses the anti-diagonal row y = 11 (of 15) near x = 3; sum one row across it.
        double fillRow = 0;
        double outlineRow = 0;
        for (int x = 0; x < 7; x++)
        {
            fillRow += fill[(11 * Size) + x] / 255.0;
        }

        for (int x = 0; x < 9; x++)
        {
            outlineRow += outline[((11 + Pad) * Side) + x] / 255.0;
        }

        Assert.InRange(outlineRow - fillRow, 2.0, 3.5);
    }

    [Theory]
    [InlineData(-1, 0f)]
    [InlineData(Glyphs.MaxOutlinePad + 1, 1f)]
    [InlineData(1, 1.5f)]
    [InlineData(1, -0.5f)]
    [InlineData(1, float.NaN)]
    public void RenderOutline_RejectsBadPadOrGrow(int pad, float grow)
    {
        byte[] buffer = new byte[64 * 64];
        Assert.Throws<ArgumentOutOfRangeException>(() => Glyphs.RenderOutline(MaskId.PinMarker, 16, pad, grow, buffer));
    }

    [Fact]
    public void RenderOutline_RejectsShortDestination_AndNonGlyphs()
    {
        Assert.Throws<ArgumentException>(() => Glyphs.RenderOutline(MaskId.PinMarker, 16, 1, 1f, new byte[(18 * 18) - 1]));
        Assert.Throws<ArgumentOutOfRangeException>(() => Glyphs.RenderOutline(MaskId.Circle, 16, 1, 1f, new byte[18 * 18]));
        Assert.Throws<ArgumentOutOfRangeException>(() => Glyphs.RenderOutline(MaskId.PinMarker, Glyphs.MinSize - 1, 1, 1f, new byte[18 * 18]));
    }

    [Fact]
    [Trait("Category", "Perf")]
    public void PinMarker_SpanOverloads_AllocateNothing()
    {
        byte[] buffer = new byte[34 * 34];
        Glyphs.Render(MaskId.PinMarker, 30, buffer);
        Glyphs.RenderOutline(MaskId.PinMarker, 30, 2, 2f, buffer);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int size = Glyphs.MinSize; size <= 30; size++)
        {
            Glyphs.Render(MaskId.PinMarker, size, buffer);
            Glyphs.RenderOutline(MaskId.PinMarker, size, 2, 2f, buffer);
        }

        Assert.Equal(0L, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    // ---- Helpers ----------------------------------------------------------------------------------

    /// <summary>Ink along the line across the pin axis at distance <paramref name="u"/> (grid px) from the needle point.</summary>
    private static double AcrossAxis(byte[] m, double u)
    {
        double cx = 9 + (u * 0.70710678);
        double cy = 55 - (u * 0.70710678);
        double ink = 0;
        for (double t = -24; t <= 24; t += 0.5)
        {
            int x = (int)Math.Floor(cx + (t * 0.70710678));
            int y = (int)Math.Floor(cy + (t * 0.70710678));
            if (x >= 0 && y >= 0 && x < S && y < S)
            {
                ink += At(m, x, y) / 255.0 * 0.5;
            }
        }

        return ink;
    }

    private static void AssertSymmetric(byte[] m, Func<int, int, (int X, int Y)> map, string what)
    {
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                (int mx, int my) = map(x, y);
                Assert.True(Math.Abs(At(m, x, y) - At(m, mx, my)) <= 1, $"{what} at {x},{y}: {At(m, x, y)} vs {At(m, mx, my)}");
            }
        }
    }
}
