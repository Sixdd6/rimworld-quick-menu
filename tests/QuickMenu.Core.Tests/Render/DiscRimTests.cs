using System;
using QuickMenu.Core.Render;
using Xunit;

namespace QuickMenu.Tests.Render;

public class DiscRimTests
{
    private static readonly Rgba32 _accent = StyleTokens.Accent;
    private static readonly Rgba32 _outline = StyleTokens.RimOutline;

    private static byte[] Render(int size, float rim = 2f, float outline = 1f)
    {
        byte[] rgba = new byte[DiscRim.ByteCount(size)];
        DiscRim.Render(size, rim, outline, _accent, _outline, rgba);
        return rgba;
    }

    // Pixel (x, y) with y counted from the top; the buffer is bottom row first.
    private static Rgba32 At(byte[] rgba, int size, int x, int y)
    {
        int p = (((size - 1 - y) * size) + x) * 4;
        return new Rgba32(rgba[p], rgba[p + 1], rgba[p + 2], rgba[p + 3]);
    }

    [Fact]
    public void Rim_HasOutlineAccentOutline_FromTheEdgeInward()
    {
        const int Size = 338; // disc diameter 336 at UI scale 1 → outer edge 168 px from the centre
        byte[] img = Render(Size);
        int cy = Size / 2;

        // Walk inward along the horizontal through the centre, right side. Pixel x has centre x + 0.5;
        // the edge is at 169 + 168 = 337 → pixel 336 is the outer outline, 335/334 accent, 333 inner outline.
        Assert.Equal(0, At(img, Size, 337, cy).A);
        Assert.Equal(_outline, At(img, Size, 336, cy));
        Assert.Equal(_accent, At(img, Size, 335, cy));
        Assert.Equal(_accent, At(img, Size, 334, cy));
        Assert.Equal(_outline, At(img, Size, 333, cy));
        Assert.Equal(0, At(img, Size, 332, cy).A);
        Assert.Equal(0, At(img, Size, Size / 2, cy).A);
        Assert.Equal(0, At(img, Size, 0, 0).A);
    }

    [Fact]
    public void Rim_AccentWidthIsTwoPixels_AtEverySize()
    {
        foreach (int size in new[] { 60, 338, 1162, 2048 })
        {
            byte[] img = Render(size);
            int cy = size / 2;
            double accentMass = 0;
            for (int x = cy; x < size; x++)
            {
                Rgba32 p = At(img, size, x, cy);

                // The outline is black, so blue · alpha / accent blue is the accent coverage of the pixel.
                accentMass += p.A / 255.0 * (p.B / (double)_accent.B);
            }

            Assert.InRange(accentMass, 1.9, 2.1);
        }
    }

    [Fact]
    public void Rim_IsMirrorSymmetric()
    {
        const int Size = 101;
        byte[] img = Render(Size, 2.5f, 1.25f);
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                Assert.Equal(At(img, Size, x, y), At(img, Size, Size - 1 - x, y));
                Assert.Equal(At(img, Size, x, y), At(img, Size, x, Size - 1 - y));
            }
        }
    }

    [Theory]
    [InlineData(5, 1f, 0.25f)]
    [InlineData(17, 2f, 1f)]
    [InlineData(64, 3f, 1.5f)]
    [InlineData(301, 2f, 1f)]
    [InlineData(400, 2f, 0f)]
    [InlineData(80, 39f, 0f)]
    public void BandOnlyEvaluation_MatchesBruteForce(int size, float rim, float outline)
    {
        byte[] fast = Render(size, rim, outline);
        byte[] slow = BruteForce(size, rim, outline);

        Assert.Equal(slow, fast);
    }

    [Fact]
    public void Render_ClearsTheDestination_AndIgnoresBytesBeyondIt()
    {
        const int Size = 40;
        byte[] buffer = new byte[DiscRim.ByteCount(Size) + 8];
        Array.Fill(buffer, (byte)0xAB);

        DiscRim.Render(Size, 2f, 1f, _accent, _outline, buffer);

        Assert.Equal(Render(Size), buffer[..DiscRim.ByteCount(Size)]);
        Assert.Equal(0xAB, buffer[^1]);
    }

    [Fact]
    public void Render_AllocatesNothing()
    {
        byte[] buffer = new byte[DiscRim.ByteCount(200)];
        DiscRim.Render(200, 2f, 1f, _accent, _outline, buffer);

        long before = GC.GetAllocatedBytesForCurrentThread();
        DiscRim.Render(200, 2f, 1f, _accent, _outline, buffer);
        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Theory]
    [InlineData(2, 1f, 0f)]
    [InlineData(64, 0f, 1f)]
    [InlineData(64, float.NaN, 1f)]
    [InlineData(64, 2f, -1f)]
    [InlineData(64, 2f, float.NaN)]
    [InlineData(64, 30f, 1f)]
    public void Render_RejectsBadInput(int size, float rim, float outline)
    {
        byte[] buffer = new byte[DiscRim.ByteCount(Math.Max(size, 3))];

        Assert.Throws<ArgumentOutOfRangeException>(() => DiscRim.Render(size, rim, outline, _accent, _outline, buffer));
    }

    [Fact]
    public void Render_RejectsShortDestination()
    {
        byte[] buffer = new byte[DiscRim.ByteCount(64) - 1];

        Assert.Throws<ArgumentException>(() => DiscRim.Render(64, 2f, 1f, _accent, _outline, buffer));
    }

    [Fact]
    public void BandWidth_IsRimPlusTwoOutlines()
    {
        Assert.Equal(4f, DiscRim.BandWidth);
    }

    // Independent reference: every pixel, same formula, top row first converted to bottom row first.
    private static byte[] BruteForce(int size, float rimTexels, float outlineTexels)
    {
        byte[] rgba = new byte[DiscRim.ByteCount(size)];
        double c = size / 2.0;
        double outer = c - 1.0;
        double band = rimTexels + (2.0 * outlineTexels);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                double d = Math.Sqrt(Math.Pow(x + 0.5 - c, 2) + Math.Pow(y + 0.5 - c, 2));
                double ar = _accent.A / 255.0 * Cov(Math.Abs(d - (outer - outlineTexels - (rimTexels / 2.0))) - (rimTexels / 2.0));
                double ao = _outline.A / 255.0 * Cov(Math.Abs(d - (outer - (band / 2.0))) - (band / 2.0));
                double a = ar + (ao * (1.0 - ar));
                if (a <= 0)
                {
                    continue;
                }

                double wo = ao * (1.0 - ar);
                int p = (((size - 1 - y) * size) + x) * 4;
                rgba[p] = B(((_accent.R * ar) + (_outline.R * wo)) / (a * 255.0));
                rgba[p + 1] = B(((_accent.G * ar) + (_outline.G * wo)) / (a * 255.0));
                rgba[p + 2] = B(((_accent.B * ar) + (_outline.B * wo)) / (a * 255.0));
                rgba[p + 3] = B(a);
            }
        }

        return rgba;
    }

    private static double Cov(double sd) => Math.Clamp(0.5 - sd, 0.0, 1.0);

    private static byte B(double v) => (byte)((Math.Clamp(v, 0.0, 1.0) * 255.0) + 0.5);
}
