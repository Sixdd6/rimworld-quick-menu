using System;
using System.Diagnostics;
using QuickMenu.Core.Render;
using Xunit;

namespace QuickMenu.Tests.Render;

public class MaskSetTests
{
    private static byte At(byte[] mask, int width, int x, int y) => mask[(y * width) + x];

    private static byte[] Render(MaskId id) => MaskSet.Render(id);

    // ---- Catalogue --------------------------------------------------------------------------------

    [Fact]
    public void Set_ListsTheShapeMasksBadgesAndIconsOfDesignSection6_InOrder()
    {
        string[] expected =
        [
            "Circle", "Disc", "Ring1", "Ring2", "Ring3", "RingDashed", "Panel", "PanelRim", "Plate",
            "SubMenuBadge", "Lock", "IconEdit", "IconDone",
        ];

        Assert.Equal(MaskSet.Count, expected.Length);
        Assert.Equal(expected.Length, Enum.GetValues<MaskId>().Length);
        for (int i = 0; i < expected.Length; i++)
        {
            MaskSpec spec = MaskSet.Spec((MaskId)i);
            Assert.Equal((MaskId)i, spec.Id);
            Assert.Equal(expected[i], spec.Name);
        }
    }

    [Theory]
    [InlineData(MaskId.Circle, 256, 0)]
    [InlineData(MaskId.Disc, 256, 0)]
    [InlineData(MaskId.Ring1, 128, 0)]
    [InlineData(MaskId.Ring2, 128, 0)]
    [InlineData(MaskId.Ring3, 128, 0)]
    [InlineData(MaskId.RingDashed, 128, 0)]
    [InlineData(MaskId.Panel, 64, 16)]
    [InlineData(MaskId.PanelRim, 64, 16)]
    [InlineData(MaskId.Plate, 16, 4)]
    [InlineData(MaskId.SubMenuBadge, 64, 0)]
    [InlineData(MaskId.Lock, 64, 0)]
    [InlineData(MaskId.IconEdit, 64, 0)]
    [InlineData(MaskId.IconDone, 64, 0)]
    public void Spec_HasDocumentedSizeAndSliceBorder(MaskId id, int size, int sliceBorder)
    {
        MaskSpec spec = MaskSet.Spec(id);

        Assert.Equal(size, spec.Width);
        Assert.Equal(size, spec.Height);
        Assert.Equal(sliceBorder, spec.SliceBorder);
        Assert.Equal(sliceBorder > 0, spec.IsNineSlice);
        Assert.Equal(size * size, spec.ByteCount);
        Assert.True(spec.ByteCount <= MaskSet.MaxByteCount);
    }

    [Theory]
    [InlineData(MaskId.Panel)]
    [InlineData(MaskId.PanelRim)]
    [InlineData(MaskId.Plate)]
    public void NineSlice_BorderIsAQuarterOfTheAtlas_AndHoldsTheCornerRadius(MaskId id)
    {
        // Widgets.DrawAtlas slices at atlas.width / 4 (spike S-7: radius must be ≤ atlas / 4).
        MaskSpec spec = MaskSet.Spec(id);

        Assert.Equal(spec.Width / 4, spec.SliceBorder);
        Assert.True(spec.CornerRadius > 0f);
        Assert.True(spec.CornerRadius <= spec.SliceBorder);
    }

    [Fact]
    public void Radii_MatchTheDesignSystem()
    {
        Assert.Equal(10f, MaskSet.Spec(MaskId.Panel).CornerRadius);
        Assert.Equal(10f, MaskSet.Spec(MaskId.PanelRim).CornerRadius);
        Assert.Equal(3f, MaskSet.Spec(MaskId.Plate).CornerRadius);
        Assert.Equal(0f, MaskSet.Spec(MaskId.Circle).CornerRadius);
    }

    [Fact]
    public void Spec_UnknownId_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MaskSet.Spec((MaskId)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => MaskSet.Render((MaskId)(-1)));
    }

    [Fact]
    public void Render_TooSmallDestination_Throws()
    {
        byte[] small = new byte[10];
        Assert.Throws<ArgumentException>(() => MaskSet.Render(MaskId.Plate, small));
    }

    // ---- Round masks ------------------------------------------------------------------------------

    [Fact]
    public void Circle_CentreOpaque_CornersAndBorderTransparent()
    {
        byte[] m = Render(MaskId.Circle);

        Assert.Equal(255, At(m, 256, 128, 128));
        Assert.Equal(0, At(m, 256, 0, 0));
        Assert.Equal(0, At(m, 256, 0, 128));
        Assert.Equal(0, At(m, 256, 128, 255));
    }

    [Fact]
    public void Disc_IsARadialGradientFrom085To055()
    {
        byte[] m = Render(MaskId.Disc);

        Assert.Equal(255, At(m, 256, 128, 128));

        // Just inside the rim the level is discEdge / discCentre = 0.55 / 0.85 of full.
        byte nearRim = At(m, 256, 128, 2);
        Assert.InRange(nearRim, 160, 170);
        Assert.Equal(0, At(m, 256, 0, 0));

        // Monotonic fall from centre to rim along a radius.
        for (int x = 129; x < 254; x++)
        {
            Assert.True(At(m, 256, x, 128) <= At(m, 256, x - 1, 128), $"not monotonic at {x}");
        }
    }

    [Theory]
    [InlineData(MaskId.Ring1, 2.3)]
    [InlineData(MaskId.Ring2, 4.6)]
    [InlineData(MaskId.Ring3, 6.9)]
    public void Rings_HaveTheirStrokeWidthWithinHalfAPixel(MaskId id, double stroke)
    {
        MaskSpec spec = MaskSet.Spec(id);
        byte[] m = Render(id);

        Assert.Equal((float)stroke, spec.Stroke, 3);
        Assert.InRange(AlphaRightOfCentre(m, 128), stroke - 0.5, stroke + 0.5);
        Assert.InRange(AlphaAboveCentre(m, 128), stroke - 0.5, stroke + 0.5);
        Assert.Equal(0, At(m, 128, 64, 64));
        Assert.Equal(0, At(m, 128, 0, 0));
        Assert.Equal(0, At(m, 128, 127, 64));
    }

    [Fact]
    public void RingDashed_HasSixteenDashes_WithTheRing1Stroke()
    {
        MaskSpec spec = MaskSet.Spec(MaskId.RingDashed);
        byte[] m = Render(MaskId.RingDashed);

        Assert.Equal(16, spec.DashCount);
        Assert.Equal(MaskSet.Spec(MaskId.Ring1).Stroke, spec.Stroke);
        Assert.Equal(16, CountDashes(m, 128, spec.Stroke));

        // A dash is centred at 12 o'clock: the stroke there is the full width.
        Assert.InRange(AlphaAboveCentre(m, 128), spec.Stroke - 0.5, spec.Stroke + 0.5);
    }

    [Fact]
    public void RingDashed_DutyIsAboutHalf()
    {
        MaskSpec spec = MaskSet.Spec(MaskId.RingDashed);
        byte[] dashed = Render(MaskId.RingDashed);
        byte[] solid = Render(MaskId.Ring1);

        double ratio = Sum(dashed) / Sum(solid);
        Assert.InRange(ratio, 0.45, 0.55);
        Assert.Equal(0.5f, spec.DashDuty);
    }

    // ---- 9-slice masks ----------------------------------------------------------------------------

    [Theory]
    [InlineData(MaskId.Panel)]
    [InlineData(MaskId.Plate)]
    public void FilledNineSlice_CentreAndEdgeStripsAreOpaque_CornerPixelIsCut(MaskId id)
    {
        MaskSpec spec = MaskSet.Spec(id);
        byte[] m = Render(id);
        int w = spec.Width;
        int b = spec.SliceBorder;

        // Every pixel outside the four corner squares is fully opaque (centre and the stretched strips).
        for (int y = 0; y < w; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool inCorner = (x < b || x >= w - b) && (y < b || y >= w - b);
                if (!inCorner)
                {
                    Assert.Equal(255, At(m, w, x, y));
                }
            }
        }

        Assert.True(At(m, w, 0, 0) < 64);
        Assert.True(At(m, w, w - 1, w - 1) < 64);
        Assert.Equal(255, At(m, w, b - 1, b - 1));
    }

    [Fact]
    public void PanelRim_IsATwoPixelOutlineAlongTheBoxEdge_WithAClearCentre()
    {
        MaskSpec spec = MaskSet.Spec(MaskId.PanelRim);
        byte[] m = Render(MaskId.PanelRim);
        int w = spec.Width;

        Assert.Equal(2f, spec.Stroke);
        Assert.Equal(255, At(m, w, 0, 32));
        Assert.Equal(255, At(m, w, 1, 32));
        Assert.Equal(0, At(m, w, 2, 32));
        Assert.Equal(255, At(m, w, 32, 0));
        Assert.Equal(255, At(m, w, 32, w - 1));
        Assert.Equal(0, At(m, w, 32, 32));
        Assert.Equal(0, At(m, w, 0, 0));
        Assert.InRange(AlphaRightOfCentre(m, w), 1.5, 2.5);
    }

    [Theory]
    [InlineData(MaskId.Panel)]
    [InlineData(MaskId.PanelRim)]
    [InlineData(MaskId.Plate)]
    public void NineSlice_StretchedStripsAreUniformAlongTheirLength(MaskId id)
    {
        // DrawAtlas stretches the edge strips; each must look the same at every point along its length.
        MaskSpec spec = MaskSet.Spec(id);
        byte[] m = Render(id);
        int w = spec.Width;
        int b = spec.SliceBorder;

        for (int along = b; along < w - b; along++)
        {
            for (int across = 0; across < w; across++)
            {
                Assert.Equal(At(m, w, across, b), At(m, w, across, along)); // left/right strips
                Assert.Equal(At(m, w, b, across), At(m, w, along, across)); // top/bottom strips
            }
        }
    }

    [Theory]
    [InlineData(MaskId.Panel)]
    [InlineData(MaskId.PanelRim)]
    [InlineData(MaskId.Plate)]
    public void NineSlice_IsMirrorSymmetric(MaskId id)
    {
        MaskSpec spec = MaskSet.Spec(id);
        byte[] m = Render(id);
        int w = spec.Width;
        for (int y = 0; y < w; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Assert.Equal(At(m, w, x, y), At(m, w, w - 1 - x, y));
                Assert.Equal(At(m, w, x, y), At(m, w, y, x));
            }
        }
    }

    // ---- New rasterizer primitives ----------------------------------------------------------------

    [Fact]
    public void DashedRing_ArrayOverloadMatchesSpan_AndOneDashAtFullDutyIsASolidRing()
    {
        byte[] full = MaskRasterizer.DashedRing(32, 3f, 1, 1f);
        byte[] solid = MaskRasterizer.Ring(32, 3f);

        Assert.Equal(solid, full);
    }

    [Theory]
    [InlineData(2, 2f, 0, 0.5f)]
    [InlineData(32, 0f, 8, 0.5f)]
    [InlineData(32, 20f, 8, 0.5f)]
    [InlineData(32, 2f, 0, 0.5f)]
    [InlineData(32, 2f, 8, 0f)]
    [InlineData(32, 2f, 8, 1.5f)]
    [InlineData(32, 2f, 8, float.NaN)]
    public void DashedRing_RejectsBadArguments(int size, float stroke, int dashes, float duty)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MaskRasterizer.DashedRing(size, stroke, dashes, duty));
    }

    [Fact]
    public void RoundedRectRing_StrokeAtLeastHalfTheShortSide_IsTheFilledRect()
    {
        byte[] ring = MaskRasterizer.RoundedRectRing(20, 10, 3f, 5f);
        byte[] fill = MaskRasterizer.RoundedRect(20, 10, 3f);

        Assert.Equal(fill, ring);
    }

    [Theory]
    [InlineData(0, 8, 2f, 1f)]
    [InlineData(8, 0, 2f, 1f)]
    [InlineData(8, 8, -1f, 1f)]
    [InlineData(8, 8, 2f, 0f)]
    [InlineData(8, 8, 2f, float.NaN)]
    public void RoundedRectRing_RejectsBadArguments(int width, int height, float radius, float stroke)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MaskRasterizer.RoundedRectRing(width, height, radius, stroke));
    }

    [Fact]
    public void NewPrimitives_RejectShortDestination()
    {
        byte[] small = new byte[4];
        Assert.Throws<ArgumentException>(() => MaskRasterizer.DashedRing(16, 2f, 4, 0.5f, small));
        Assert.Throws<ArgumentException>(() => MaskRasterizer.RoundedRectRing(16, 16, 2f, 2f, small));
    }

    // ---- Determinism, allocation, timing ----------------------------------------------------------

    [Fact]
    public void Render_IsDeterministic_AndSpanMatchesArray()
    {
        byte[] buffer = new byte[MaskSet.MaxByteCount];
        for (int i = 0; i < MaskSet.Count; i++)
        {
            var id = (MaskId)i;
            byte[] a = MaskSet.Render(id);
            byte[] b = MaskSet.Render(id);
            Assert.Equal(a, b);

            buffer.AsSpan().Fill(0xAB);
            MaskSet.Render(id, buffer);
            Assert.True(buffer.AsSpan(0, a.Length).SequenceEqual(a), $"{id} span output differs");
        }
    }

    [Fact]
    public void Render_SpanOverload_AllocatesNothing()
    {
        byte[] buffer = new byte[MaskSet.MaxByteCount];
        for (int i = 0; i < MaskSet.Count; i++)
        {
            MaskSet.Render((MaskId)i, buffer);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < MaskSet.Count; i++)
        {
            MaskSet.Render((MaskId)i, buffer);
            _ = MaskSet.Spec((MaskId)i);
        }

        long after = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(0L, after - before);
    }

    [Fact]
    [Trait("Category", "Perf")]
    public void WholeSet_RendersWithinFiveMilliseconds_MedianOfFive()
    {
        // Gate at 5 × the 5 ms budget (design.md §6) like the other Perf gates; the real number is from the benchmark.
        byte[] buffer = new byte[MaskSet.MaxByteCount];
        RenderAll(buffer);
        double[] ms = new double[5];
        for (int run = 0; run < ms.Length; run++)
        {
            long start = Stopwatch.GetTimestamp();
            RenderAll(buffer);
            ms[run] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }

        Array.Sort(ms);
        Assert.True(ms[2] <= 25.0, $"median {ms[2]:F2} ms");
    }

    // ---- Helpers ----------------------------------------------------------------------------------

    private static void RenderAll(byte[] buffer)
    {
        for (int i = 0; i < MaskSet.Count; i++)
        {
            MaskSet.Render((MaskId)i, buffer);
        }
    }

    private static double Sum(byte[] m)
    {
        double s = 0;
        foreach (byte v in m)
        {
            s += v;
        }

        return s;
    }

    /// <summary>Alpha mass (in pixels) on the row just below the centre, from the centre to the right border.</summary>
    private static double AlphaRightOfCentre(byte[] m, int size)
    {
        double s = 0;
        int y = size / 2;
        for (int x = size / 2; x < size; x++)
        {
            s += At(m, size, x, y) / 255.0;
        }

        return s;
    }

    /// <summary>Alpha mass (in pixels) on the column just right of the centre, from the top border to the centre.</summary>
    private static double AlphaAboveCentre(byte[] m, int size)
    {
        double s = 0;
        int x = size / 2;
        for (int y = 0; y < size / 2; y++)
        {
            s += At(m, size, x, y) / 255.0;
        }

        return s;
    }

    /// <summary>Counts on/off transitions walking once around the ring's centre line.</summary>
    private static int CountDashes(byte[] m, int size, float stroke)
    {
        double c = size / 2.0;
        double mid = c - MaskRasterizer.EdgePadding - (stroke / 2.0);
        const int Steps = 3600;
        bool Sample(int i)
        {
            double a = 2.0 * Math.PI * i / Steps;
            int x = (int)Math.Floor(c + (mid * Math.Cos(a)));
            int y = (int)Math.Floor(c + (mid * Math.Sin(a)));
            return At(m, size, x, y) >= 128;
        }

        int rises = 0;
        bool prev = Sample(Steps - 1);
        for (int i = 0; i < Steps; i++)
        {
            bool on = Sample(i);
            if (on && !prev)
            {
                rises++;
            }

            prev = on;
        }

        return rises;
    }
}
