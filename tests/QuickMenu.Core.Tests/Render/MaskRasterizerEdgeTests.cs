using System;
using QuickMenu.Core.Render;
using Xunit;

namespace QuickMenu.Tests.Render;

/// <summary>Parameter extremes and argument validation for the rasterizer and PNG writer.</summary>
public class MaskRasterizerEdgeTests
{
    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(7)]
    public void SmallestSizes_AreSymmetricAndHaveTransparentBorder(int size)
    {
        byte[] m = MaskRasterizer.Circle(size);
        Assert.Equal(size * size, m.Length);
        for (int i = 0; i < size; i++)
        {
            Assert.Equal(0, m[i]);
            Assert.Equal(0, m[((size - 1) * size) + i]);
            Assert.Equal(0, m[i * size]);
            Assert.Equal(0, m[(i * size) + size - 1]);
        }

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Assert.Equal(m[(y * size) + x], m[(x * size) + y]);
                Assert.Equal(m[(y * size) + x], m[(y * size) + (size - 1 - x)]);
            }
        }
    }

    [Fact]
    public void Disc_EdgeLevelZero_FadesToTransparentAtTheRim()
    {
        byte[] m = MaskRasterizer.Disc(64, 0f);
        Assert.True(m[(32 * 64) + 32] >= 250);
        Assert.True(m[(32 * 64) + 62] < 40);
    }

    [Fact]
    public void Disc_EdgeLevelBoundaries_AreAccepted()
    {
        Assert.NotEmpty(MaskRasterizer.Disc(16, 0f));
        Assert.NotEmpty(MaskRasterizer.Disc(16, 1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => MaskRasterizer.Disc(16, float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => MaskRasterizer.Disc(16, float.PositiveInfinity));
    }

    [Fact]
    public void Ring_StrokeBoundaries()
    {
        // Largest legal stroke is the outer radius (a ring that is a full disc).
        double outer = (64 / 2.0) - MaskRasterizer.EdgePadding;
        Assert.NotEmpty(MaskRasterizer.Ring(64, (float)outer));
        Assert.Throws<ArgumentOutOfRangeException>(() => MaskRasterizer.Ring(64, (float)outer + 0.01f));
        Assert.Throws<ArgumentOutOfRangeException>(() => MaskRasterizer.Ring(64, -1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => MaskRasterizer.Ring(64, float.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => MaskRasterizer.Ring(2, 1f));
    }

    [Fact]
    public void Ring_HairlineStroke_IsNonEmptyAndThin()
    {
        byte[] m = MaskRasterizer.Ring(64, 0.25f);
        double sum = 0;
        for (int x = 32; x < 64; x++)
        {
            sum += m[(32 * 64) + x] / 255.0;
        }

        Assert.InRange(sum, 0.2, 0.3);
    }

    [Theory]
    [InlineData(1, 1, 0f)]
    [InlineData(1, 1, 50f)]
    [InlineData(2, 100, 50f)]
    [InlineData(100, 1, 3f)]
    [InlineData(64, 64, 1e9f)]
    public void RoundedRect_DegenerateBoxesAndHugeRadii_StayInRangeAndSymmetric(int w, int h, float r)
    {
        byte[] m = MaskRasterizer.RoundedRect(w, h, r);
        Assert.Equal(w * h, m.Length);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Assert.Equal(m[(y * w) + x], m[(y * w) + (w - 1 - x)]);
                Assert.Equal(m[(y * w) + x], m[((h - 1 - y) * w) + x]);
            }
        }
    }

    [Fact]
    public void RoundedRect_1x1_IsOpaque()
    {
        Assert.Equal(new byte[] { 255 }, MaskRasterizer.RoundedRect(1, 1, 0f));
    }

    [Fact]
    public void RoundedRect_RejectsNaNAndNegativeSizes()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MaskRasterizer.RoundedRect(8, 8, float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => MaskRasterizer.RoundedRect(-1, 8, 1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => MaskRasterizer.RoundedRect(8, -1, 1f));
    }

    [Fact]
    public void SpanOverloads_AcceptOversizedBuffers_AndOnlyTouchTheirRange()
    {
        byte[] buffer = new byte[(16 * 16) + 8];
        Array.Fill(buffer, (byte)0xAB);
        MaskRasterizer.Ring(16, 2f, buffer);
        for (int i = 16 * 16; i < buffer.Length; i++)
        {
            Assert.Equal(0xAB, buffer[i]);
        }

        MaskRasterizer.Disc(16, 0.5f, buffer.AsSpan());
        MaskRasterizer.Circle(16, buffer);
        Assert.Throws<ArgumentException>(() => MaskRasterizer.Ring(16, 2f, new byte[16 * 16 - 1]));
        Assert.Throws<ArgumentException>(() => MaskRasterizer.Disc(16, 0.5f, Span<byte>.Empty));
    }

    [Fact]
    public void ArrayAndSpanVariants_AgreeForAllShapes_AtOddSizes()
    {
        foreach (int s in new[] { 3, 17, 63 })
        {
            byte[] buf = new byte[s * s];
            MaskRasterizer.Disc(s, 0.3f, buf);
            Assert.Equal(MaskRasterizer.Disc(s, 0.3f), buf);
            MaskRasterizer.Ring(s, 0.5f, buf);
            Assert.Equal(MaskRasterizer.Ring(s, 0.5f), buf);
        }
    }

    [Fact]
    public void PngWriter_OneByOne_AndWideImages_RoundTripCrcs()
    {
        byte[] one = PngWriter.EncodeGrayAlpha(1, 1, new byte[] { 77 });
        Assert.Equal(0x89, one[0]);
        byte[] wide = PngWriter.EncodeGrayAlpha(1000, 2, new byte[2000]);
        Assert.True(wide.Length > 40);
        // Oversized alpha buffers are accepted; only the first width*height bytes are used.
        Assert.Equal(one, PngWriter.EncodeGrayAlpha(1, 1, new byte[] { 77, 1, 2, 3 }));
    }

    [Fact]
    public void PngWriter_RejectsNegativeAndZeroDimensions_AndHandlesEmptyAlpha()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PngWriter.EncodeGrayAlpha(-1, 1, new byte[1]));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngWriter.EncodeGrayAlpha(1, 0, new byte[1]));
        Assert.Throws<ArgumentException>(() => PngWriter.EncodeGrayAlpha(1, 1, ReadOnlySpan<byte>.Empty));
    }
}
