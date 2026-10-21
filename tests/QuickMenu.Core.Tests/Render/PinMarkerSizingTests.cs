using System;
using QuickMenu.Core.Render;
using Xunit;

namespace QuickMenu.Tests.Render;

/// <summary>
/// Pin-marker sizes, textures and placement (T-89; design.md §2 "Architect pin marker"; ADR 0009 hook sites 4 and 5).
/// </summary>
public class PinMarkerSizingTests
{
    private const float Tolerance = 1e-4f;

    public static readonly TheoryData<float> Scales = [1f, 1.25f, 1.5f, 1.75f, 2f];

    [Fact]
    public void NominalSizes_AreFullPartialAndEntry_PartialSmallerThanFull()
    {
        Assert.Equal(15f, PinMarkerSizing.NominalSize(PinMarkerKind.Full));
        Assert.Equal(0.2f * PinMarkerSizing.GizmoSize, PinMarkerSizing.FullSize);
        Assert.Equal(11f, PinMarkerSizing.NominalSize(PinMarkerKind.Partial));
        Assert.Equal(10f, PinMarkerSizing.NominalSize(PinMarkerKind.Entry));
        Assert.True(PinMarkerSizing.PartialSize < PinMarkerSizing.FullSize);
        Assert.Equal(PinMarkerSizing.KindCount, Enum.GetValues<PinMarkerKind>().Length);
        Assert.Throws<ArgumentOutOfRangeException>(() => PinMarkerSizing.NominalSize((PinMarkerKind)7));
    }

    [Theory]
    [MemberData(nameof(Scales))]
    public void Texels_AreTheDrawnSizeInScreenPixels_Within8To32(float scale)
    {
        PinMarkerSizing s = PinMarkerSizing.Create(scale);
        foreach (PinMarkerKind kind in Enum.GetValues<PinMarkerKind>())
        {
            int n = s.Texels(kind);
            Assert.InRange(n, Glyphs.MinSize, 32);
            Assert.True(Math.Abs(n - (PinMarkerSizing.NominalSize(kind) * scale)) <= 0.5f, $"{kind} at {scale}: {n}");

            // One texel per screen px, and the draw size stays within half a screen px of the nominal size.
            Assert.Equal(n / scale, s.DrawSize(kind), 4);
            Assert.True(Math.Abs(s.DrawSize(kind) - PinMarkerSizing.NominalSize(kind)) <= 0.5f / scale + Tolerance);
        }
    }

    [Fact]
    public void Texels_KnownValues()
    {
        PinMarkerSizing one = PinMarkerSizing.Create(1f);
        PinMarkerSizing two = PinMarkerSizing.Create(2f);
        PinMarkerSizing oneAndHalf = PinMarkerSizing.Create(1.5f);

        Assert.Equal(15, one.Texels(PinMarkerKind.Full));
        Assert.Equal(11, one.Texels(PinMarkerKind.Partial));
        Assert.Equal(10, one.Texels(PinMarkerKind.Entry));
        Assert.Equal(30, two.Texels(PinMarkerKind.Full));
        Assert.Equal(23, oneAndHalf.Texels(PinMarkerKind.Full)); // 22.5 → 23
        Assert.Equal(17, oneAndHalf.Texels(PinMarkerKind.Partial)); // 16.5 → 17
        Assert.Equal(Glyphs.MinSize, PinMarkerSizing.Create(0.5f).Texels(PinMarkerKind.Entry)); // 5 px → MinSize
    }

    [Theory]
    [InlineData(1f, 1, 1f)]
    [InlineData(1.25f, 2, 1.25f)]
    [InlineData(1.5f, 2, 1.5f)]
    [InlineData(2f, 2, 2f)]
    [InlineData(0.75f, 1, 0.75f)]
    public void Outline_IsOneUiPixel_OnACanvasPaddedByWholeTexels(float scale, int pad, float grow)
    {
        PinMarkerSizing s = PinMarkerSizing.Create(scale);

        Assert.Equal(pad, s.OutlinePad);
        Assert.Equal(grow, s.OutlineGrow, 4);
        Assert.True(s.OutlineGrow <= s.OutlinePad);
        Assert.Equal(s.Texels(PinMarkerKind.Full) + (2 * pad), s.OutlineTexels(PinMarkerKind.Full));
    }

    [Theory]
    [MemberData(nameof(Scales))]
    public void OutlineRect_LinesUpTexelForTexelWithTheFill(float scale)
    {
        PinMarkerSizing s = PinMarkerSizing.Create(scale);
        foreach (PinMarkerKind kind in Enum.GetValues<PinMarkerKind>())
        {
            var fill = new MarkerRect(100f, 50f, s.DrawSize(kind));
            MarkerRect outline = s.OutlineRect(fill, kind);

            // The outline texture is drawn at one texel per screen px too, centred on the fill.
            Assert.Equal(s.OutlineTexels(kind) / scale, outline.Size, 3);
            Assert.Equal(fill.X + (fill.Size / 2f), outline.X + (outline.Size / 2f), 3);
            Assert.Equal(fill.Y + (fill.Size / 2f), outline.Y + (outline.Size / 2f), 3);
        }

        // A shrunk fill (a 36 px gizmo) shrinks its outline in proportion.
        var half = new MarkerRect(0f, 0f, s.DrawSize(PinMarkerKind.Full) / 2f);
        Assert.Equal(s.OutlineTexels(PinMarkerKind.Full) / scale / 2f, s.OutlineRect(half, PinMarkerKind.Full).Size, 3);
    }

    [Fact]
    public void Button_FullMarker_SitsInTheTopRightColumn_UnderThePlusSlot()
    {
        // ADR 0009: (xMax − 1·k − s, y + 18·k, s, s), k = height / 75.
        PinMarkerSizing s = PinMarkerSizing.Create(1f);
        MarkerRect r = s.Button(200f, 300f, 75f, 75f, PinMarkerKind.Full);

        Assert.Equal(new MarkerRect(200f + 75f - 1f - 15f, 318f, 15f), r);
        Assert.Equal(274f, r.XMax);

        // Clear of the vanilla "+" (x ≥ xMax − 17, y + 1 … y + 17) and of the label strip (bottom 20 px).
        Assert.True(r.Y >= 300f + 17f);
        Assert.True(r.YMax <= 300f + 75f - 20f);

        // The outline stays inside the button: it fills the 1 px gap to the right edge.
        MarkerRect o = s.OutlineRect(r, PinMarkerKind.Full);
        Assert.Equal(275f, o.XMax, 4);
        Assert.Equal(317f, o.Y, 4);
    }

    [Theory]
    [MemberData(nameof(Scales))]
    public void Button_PartialMarker_SharesTheTopRightAnchor_AndIsSmaller(float scale)
    {
        PinMarkerSizing s = PinMarkerSizing.Create(scale);
        MarkerRect full = s.Button(10f, 20f, 75f, 75f, PinMarkerKind.Full);
        MarkerRect partial = s.Button(10f, 20f, 75f, 75f, PinMarkerKind.Partial);

        Assert.Equal(full.XMax, partial.XMax, 4);
        Assert.Equal(full.Y, partial.Y, 4);
        Assert.True(partial.Size < full.Size);
    }

    [Fact]
    public void Button_ScalesWithAShrunkGizmo()
    {
        PinMarkerSizing s = PinMarkerSizing.Create(1f);
        MarkerRect r = s.Button(0f, 0f, 36f, 36f, PinMarkerKind.Full);
        float k = 36f / 75f;

        Assert.Equal(15f * k, r.Size, 4);
        Assert.Equal(36f - k - (15f * k), r.X, 4);
        Assert.Equal(18f * k, r.Y, 4);
    }

    [Fact]
    public void ListRow_MarkerSitsInTheIconSquaresTopRightCorner()
    {
        // Vanilla Normal row: icon square (x + 4, y + 4) to (x + 27, y + 27); +4 px right while hovered.
        PinMarkerSizing s = PinMarkerSizing.Create(1f);

        Assert.Equal(new MarkerRect(10f + 27f - 10f, 24f, 10f), s.ListRow(10f, 20f, 27f, false));
        Assert.Equal(new MarkerRect(10f + 31f - 10f, 24f, 10f), s.ListRow(10f, 20f, 27f, true));

        // Tiny rows (16 px, a 12 px icon square): at most half the square, still in its top-right corner.
        MarkerRect tiny = s.ListRow(0f, 0f, 16f, false);
        Assert.Equal(6f, tiny.Size, 4);
        Assert.Equal(16f, tiny.XMax, 4);
        Assert.Equal(4f, tiny.Y, 4);
    }

    [Fact]
    public void ListRowNoIcon_MarkerSitsAtTheRightEnd_CentredVertically()
    {
        PinMarkerSizing s = PinMarkerSizing.Create(1f);
        MarkerRect r = s.ListRowNoIcon(0f, 100f, 200f, 30f);

        Assert.Equal(196f, r.XMax, 4);
        Assert.Equal(110f, r.Y, 4);
        Assert.Equal(10f, r.Size, 4);
    }

    [Fact]
    public void GridCell_MarkerSitsInTheTopRightCorner_OnePixelIn()
    {
        PinMarkerSizing s = PinMarkerSizing.Create(1f);

        Assert.Equal(new MarkerRect(50f + 34f - 1f - 10f, 81f, 10f), s.GridCell(50f, 80f, 34f));
    }

    [Fact]
    public void Changed_OnlyWhenNothingIsBuiltOrTheUiScaleChanged()
    {
        PinMarkerSizing one = PinMarkerSizing.Create(1f);

        Assert.True(PinMarkerSizing.Changed(null, one));
        Assert.False(PinMarkerSizing.Changed(one, PinMarkerSizing.Create(1f)));
        Assert.True(PinMarkerSizing.Changed(one, PinMarkerSizing.Create(1.5f)));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void Create_RejectsBadScale(float scale)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PinMarkerSizing.Create(scale));
    }

    [Fact]
    public void MarkerRect_Expanded_GrowsEverySide()
    {
        Assert.Equal(new MarkerRect(9f, 19f, 7f), new MarkerRect(10f, 20f, 5f).Expanded(1f));
    }

    [Fact]
    [Trait("Category", "Perf")]
    public void Placement_AllocatesNothing()
    {
        PinMarkerSizing s = PinMarkerSizing.Create(1.5f);
        float sink = 0f;
        sink += s.Button(0f, 0f, 75f, 75f, PinMarkerKind.Full).X;

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
        {
            MarkerRect r = s.Button(i, 0f, 75f, 75f, PinMarkerKind.Partial);
            sink += s.OutlineRect(r, PinMarkerKind.Partial).Size + s.ListRow(i, 0f, 27f, true).X + s.GridCell(i, 0f, 34f).Y;
        }

        Assert.Equal(0L, GC.GetAllocatedBytesForCurrentThread() - before);
        Assert.True(sink > 0f);
    }
}
