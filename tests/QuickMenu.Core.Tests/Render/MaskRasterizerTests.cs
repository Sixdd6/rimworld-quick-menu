using System;
using QuickMenu.Core.Render;
using Xunit;

namespace QuickMenu.Tests.Render;

public class MaskRasterizerTests
{
    private static byte At(byte[] mask, int width, int x, int y) => mask[(y * width) + x];

    // ---- Circle / Disc -------------------------------------------------------------------------

    [Fact]
    public void Circle256_CentreIsOpaque_CornersAreTransparent()
    {
        byte[] m = MaskRasterizer.Circle(256);

        Assert.Equal(256 * 256, m.Length);
        Assert.Equal(255, At(m, 256, 128, 128));
        Assert.Equal(255, At(m, 256, 127, 127));
        Assert.Equal(0, At(m, 256, 0, 0));
        Assert.Equal(0, At(m, 256, 255, 0));
        Assert.Equal(0, At(m, 256, 0, 255));
        Assert.Equal(0, At(m, 256, 255, 255));
    }

    [Fact]
    public void Circle256_EdgeIsAntiAliasedOverAboutOnePixel()
    {
        byte[] m = MaskRasterizer.Circle(256);
        AssertEdgeRampIsOnePixel(m, 256, insideLevel: 255);

        // Along the 45° diagonal the edge falls inside a pixel: there is a partial value there.
        int diagPartial = 0;
        for (int i = 128; i < 256; i++)
        {
            byte a = At(m, 256, i, i);
            if (a is > 0 and < 255)
            {
                diagPartial++;
            }

            if (i > 128)
            {
                Assert.True(a <= At(m, 256, i - 1, i - 1), $"not monotonic at {i}");
            }
        }

        Assert.InRange(diagPartial, 1, 2);
    }

    /// <summary>
    /// Every pixel whose centre is ≥ 0.5 px outside the ideal circle is 0, every pixel ≥ 0.5 px inside is
    /// opaque (≥ <paramref name="insideLevel"/> for the circle), and the partial pixels form a band about one
    /// pixel wide: their count is close to the circumference 2πr.
    /// </summary>
    private static void AssertEdgeRampIsOnePixel(byte[] m, int size, byte insideLevel)
    {
        double c = size / 2.0;
        double r = c - MaskRasterizer.EdgePadding;
        int partial = 0;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                double dx = x + 0.5 - c;
                double dy = y + 0.5 - c;
                double sd = Math.Sqrt((dx * dx) + (dy * dy)) - r;
                byte a = At(m, size, x, y);
                if (sd >= 0.5)
                {
                    Assert.Equal(0, a);
                }
                else if (sd <= -0.5)
                {
                    Assert.True(a >= insideLevel, $"({x},{y}) inside but alpha {a}");
                }
                else if (a > 0)
                {
                    partial++;
                }
            }
        }

        double circumference = 2 * Math.PI * r;
        Assert.InRange(partial, circumference * 0.8, circumference * 1.2);
    }

    [Fact]
    public void Circle_ShapeBoundaryIsInsetByThePadding()
    {
        // Outer radius = size/2 − EdgePadding, so the border pixels are always 0 (clean clamp sampling).
        byte[] m = MaskRasterizer.Circle(64);

        for (int i = 0; i < 64; i++)
        {
            Assert.Equal(0, At(m, 64, i, 0));
            Assert.Equal(0, At(m, 64, i, 63));
            Assert.Equal(0, At(m, 64, 0, i));
            Assert.Equal(0, At(m, 64, 63, i));
        }

        Assert.Equal(1f, MaskRasterizer.EdgePadding);
    }

    [Fact]
    public void Circle_AreaMatchesPiRSquared()
    {
        byte[] m = MaskRasterizer.Circle(256);
        double sum = 0;
        foreach (byte a in m)
        {
            sum += a / 255.0;
        }

        double r = 128 - MaskRasterizer.EdgePadding;
        Assert.InRange(sum, (Math.PI * r * r) - 20, (Math.PI * r * r) + 20);
    }

    [Fact]
    public void Disc256_CentreOpaque_CornerTransparent_RadialGradientFallsToEdgeLevel()
    {
        byte[] m = MaskRasterizer.Disc(256);

        Assert.Equal(255, At(m, 256, 128, 128));
        Assert.Equal(0, At(m, 256, 0, 0));

        // Non-increasing from the centre outwards.
        for (int x = 129; x < 256; x++)
        {
            Assert.True(At(m, 256, x, 128) <= At(m, 256, x - 1, 128), $"not monotonic at x={x}");
        }

        // Just inside the rim the level is close to the default edge level (0.55 / 0.85 of full).
        byte nearRim = At(m, 256, 128 + 124, 128);
        double expected = MaskRasterizer.DefaultDiscEdgeLevel * 255;
        Assert.InRange(nearRim, expected - 1, expected + 8);

        // Then it drops to 0 over about one pixel, like the circle.
        AssertEdgeRampIsOnePixel(m, 256, insideLevel: (byte)Math.Floor(expected));
    }

    [Fact]
    public void Disc_WithEdgeLevelOne_EqualsCircle()
    {
        Assert.Equal(MaskRasterizer.Circle(96), MaskRasterizer.Disc(96, 1f));
    }

    // ---- Ring ----------------------------------------------------------------------------------

    [Theory]
    [InlineData(4.6f)]
    [InlineData(2.3f)]
    [InlineData(6.9f)]
    public void Ring128_StrokeProfileMatchesWidth_InsideIsEmpty(float stroke)
    {
        byte[] m = MaskRasterizer.Ring(128, stroke);

        // Integrated alpha along a radial scan equals the stroke width (within 0.5 px).
        double horizontal = 0;
        for (int x = 64; x < 128; x++)
        {
            horizontal += At(m, 128, x, 64) / 255.0;
        }

        Assert.InRange(horizontal, stroke - 0.5, stroke + 0.5);

        double vertical = 0;
        for (int y = 63; y >= 0; y--)
        {
            vertical += At(m, 128, 63, y) / 255.0;
        }

        Assert.InRange(vertical, stroke - 0.5, stroke + 0.5);

        // Inside the inner radius (with 1 px for the AA ramp) everything is 0.
        double centre = 64;
        double inner = 64 - MaskRasterizer.EdgePadding - stroke - 1;
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                double dx = x + 0.5 - centre;
                double dy = y + 0.5 - centre;
                if ((dx * dx) + (dy * dy) < inner * inner)
                {
                    Assert.Equal(0, At(m, 128, x, y));
                }
            }
        }

        // Corners are 0 too.
        Assert.Equal(0, At(m, 128, 0, 0));
        Assert.Equal(0, At(m, 128, 127, 127));
    }

    [Fact]
    public void Ring_WideStrokeReachesFullAlphaInTheMiddleOfTheStroke()
    {
        byte[] m = MaskRasterizer.Ring(128, 6.9f);
        // Outer radius 63 → stroke spans 56.1 … 63 from the centre (64). Pixel x = 64 + 59 has centre 59.5.
        Assert.Equal(255, At(m, 128, 64 + 59, 64));
    }

    // ---- RoundedRect ---------------------------------------------------------------------------

    [Fact]
    public void RoundedRect_EdgesAreFullAlpha()
    {
        const int w = 64;
        const int h = 40;
        const float r = 10f;
        byte[] m = MaskRasterizer.RoundedRect(w, h, r);

        Assert.Equal(w * h, m.Length);

        // Straight parts of the four edges (away from the corner arcs) are full alpha on the border pixels.
        for (int x = (int)r; x < w - (int)r; x++)
        {
            Assert.Equal(255, At(m, w, x, 0));
            Assert.Equal(255, At(m, w, x, h - 1));
        }

        for (int y = (int)r; y < h - (int)r; y++)
        {
            Assert.Equal(255, At(m, w, 0, y));
            Assert.Equal(255, At(m, w, w - 1, y));
        }

        // Interior is full.
        Assert.Equal(255, At(m, w, w / 2, h / 2));
    }

    [Fact]
    public void RoundedRect_CornersFollowTheRadius()
    {
        const int w = 64;
        const int h = 40;
        const float r = 10f;
        byte[] m = MaskRasterizer.RoundedRect(w, h, r);

        // The very corners are empty.
        Assert.Equal(0, At(m, w, 0, 0));
        Assert.Equal(0, At(m, w, w - 1, 0));
        Assert.Equal(0, At(m, w, 0, h - 1));
        Assert.Equal(0, At(m, w, w - 1, h - 1));

        // In each corner box, a pixel whose centre is ≥ 1 px outside the arc (centre (r, r)) is 0,
        // and one ≥ 1 px inside the arc is 255. That is the arc of radius r, not a square or chamfer.
        for (int y = 0; y < (int)r; y++)
        {
            for (int x = 0; x < (int)r; x++)
            {
                double dx = x + 0.5 - r;
                double dy = y + 0.5 - r;
                double d = Math.Sqrt((dx * dx) + (dy * dy));
                byte[] corners =
                {
                    At(m, w, x, y),
                    At(m, w, w - 1 - x, y),
                    At(m, w, x, h - 1 - y),
                    At(m, w, w - 1 - x, h - 1 - y),
                };
                foreach (byte a in corners)
                {
                    if (d >= r + 1)
                    {
                        Assert.Equal(0, a);
                    }
                    else if (d <= r - 1)
                    {
                        Assert.Equal(255, a);
                    }
                    else
                    {
                        // Coverage ≈ r − d + 0.5 (±1 LSB).
                        double expected = Math.Clamp(r - d + 0.5, 0, 1) * 255;
                        Assert.InRange(a, expected - 1.5, expected + 1.5);
                    }
                }
            }
        }
    }

    [Fact]
    public void RoundedRect_RadiusIsClampedToHalfTheShortSide()
    {
        // r larger than h/2 behaves like r = h/2 (a stadium), not an error.
        Assert.Equal(MaskRasterizer.RoundedRect(48, 20, 10f), MaskRasterizer.RoundedRect(48, 20, 50f));
    }

    [Fact]
    public void RoundedRect_ZeroRadiusIsAFullRectangle()
    {
        byte[] m = MaskRasterizer.RoundedRect(8, 6, 0f);
        Assert.All(m, a => Assert.Equal(255, a));
    }

    // ---- Determinism and buffers ---------------------------------------------------------------

    [Fact]
    public void SameInputsTwice_GiveIdenticalBytes()
    {
        Assert.Equal(MaskRasterizer.Circle(256), MaskRasterizer.Circle(256));
        Assert.Equal(MaskRasterizer.Disc(256), MaskRasterizer.Disc(256));
        Assert.Equal(MaskRasterizer.Ring(128, 4.6f), MaskRasterizer.Ring(128, 4.6f));
        Assert.Equal(MaskRasterizer.RoundedRect(64, 40, 10f), MaskRasterizer.RoundedRect(64, 40, 10f));
    }

    [Fact]
    public void SpanOverloads_FillACallerBuffer_AndMatchTheArrayVersions()
    {
        byte[] buffer = new byte[(64 * 64) + 10];
        Array.Fill(buffer, (byte)7);

        MaskRasterizer.Circle(64, buffer);
        Assert.Equal(MaskRasterizer.Circle(64), buffer.AsSpan(0, 64 * 64).ToArray());
        Assert.Equal(7, buffer[^1]); // nothing written past size²

        MaskRasterizer.Disc(64, 0.5f, buffer);
        Assert.Equal(MaskRasterizer.Disc(64, 0.5f), buffer.AsSpan(0, 64 * 64).ToArray());

        MaskRasterizer.Ring(64, 3f, buffer);
        Assert.Equal(MaskRasterizer.Ring(64, 3f), buffer.AsSpan(0, 64 * 64).ToArray());

        MaskRasterizer.RoundedRect(32, 16, 4f, buffer);
        Assert.Equal(MaskRasterizer.RoundedRect(32, 16, 4f), buffer.AsSpan(0, 32 * 16).ToArray());
    }

    [Fact]
    public void InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MaskRasterizer.Circle(2));
        Assert.Throws<ArgumentOutOfRangeException>(() => MaskRasterizer.Disc(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => MaskRasterizer.Disc(64, -0.1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => MaskRasterizer.Disc(64, 1.1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => MaskRasterizer.Ring(64, 0f));
        Assert.Throws<ArgumentOutOfRangeException>(() => MaskRasterizer.Ring(64, 40f));
        Assert.Throws<ArgumentOutOfRangeException>(() => MaskRasterizer.Ring(64, float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => MaskRasterizer.RoundedRect(0, 10, 2f));
        Assert.Throws<ArgumentOutOfRangeException>(() => MaskRasterizer.RoundedRect(10, 0, 2f));
        Assert.Throws<ArgumentOutOfRangeException>(() => MaskRasterizer.RoundedRect(10, 10, -1f));
        Assert.Throws<ArgumentException>(() => MaskRasterizer.Circle(64, new byte[64 * 63]));
        Assert.Throws<ArgumentException>(() => MaskRasterizer.RoundedRect(10, 10, 2f, new byte[99]));
    }
}
