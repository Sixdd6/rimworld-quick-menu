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
        MaskId.SubMenuBadge, MaskId.Lock, MaskId.IconEdit, MaskId.IconDone,
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
    public void GlyphRange_IsExactlyTheFourRemainingGlyphs_WithoutCaretOrPlus()
    {
        // The insert caret and the edit-mode "+" were removed with the insert cursor and the Add slot (T-55).
        Assert.Equal(MaskId.SubMenuBadge, Glyphs.First);
        Assert.Equal(MaskId.IconDone, Glyphs.Last);
        Assert.Equal(4, Glyphs.Count);
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

    // ---- Helpers ----------------------------------------------------------------------------------

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
