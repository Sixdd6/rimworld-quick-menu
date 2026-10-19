using System;
using QuickMenu.Core.Render;
using Xunit;

namespace QuickMenu.Tests.Render;

public class TexturePixelsTests
{
    [Fact]
    public void MaskToRgba_FlipsRows_AndWritesWhiteWithMaskAlpha()
    {
        // 3 × 2 mask, top row first: 1 2 3 / 4 5 6.
        byte[] mask = [1, 2, 3, 4, 5, 6];
        byte[] rgba = new byte[3 * 2 * 4];

        TexturePixels.MaskToRgbaBottomUp(mask, 3, 2, rgba);

        // Bottom row first: 4 5 6 / 1 2 3.
        byte[] expected =
        [
            255, 255, 255, 4, 255, 255, 255, 5, 255, 255, 255, 6,
            255, 255, 255, 1, 255, 255, 255, 2, 255, 255, 255, 3,
        ];
        Assert.Equal(expected, rgba);
    }

    [Fact]
    public void MaskToRgba_KeepsAsymmetricGlyphOrientation()
    {
        // The caret points right and is wider on the left; the lock's shackle is on top. After the flip the first
        // texture row (bottom of the screen) must hold the lock body, not the shackle.
        const int Size = 64;
        byte[] mask = Glyphs.Render(MaskId.Lock, Size);
        byte[] rgba = new byte[Size * Size * 4];

        TexturePixels.MaskToRgbaBottomUp(mask, Size, Size, rgba);

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                Assert.Equal(mask[(y * Size) + x], rgba[((((Size - 1 - y) * Size) + x) * 4) + 3]);
            }
        }
    }

    [Fact]
    public void MaskToRgba_AllocatesNothing()
    {
        byte[] mask = new byte[64 * 64];
        byte[] rgba = new byte[64 * 64 * 4];
        TexturePixels.MaskToRgbaBottomUp(mask, 64, 64, rgba);

        long before = GC.GetAllocatedBytesForCurrentThread();
        TexturePixels.MaskToRgbaBottomUp(mask, 64, 64, rgba);
        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact]
    public void MaskToRgba_RejectsBadInput()
    {
        byte[] mask = new byte[4];
        byte[] rgba = new byte[16];

        Assert.Throws<ArgumentOutOfRangeException>(() => TexturePixels.MaskToRgbaBottomUp(mask, 0, 2, rgba));
        Assert.Throws<ArgumentOutOfRangeException>(() => TexturePixels.MaskToRgbaBottomUp(mask, 2, 0, rgba));
        Assert.Throws<ArgumentException>(() => TexturePixels.MaskToRgbaBottomUp(mask, 3, 2, new byte[24]));
        Assert.Throws<ArgumentException>(() => TexturePixels.MaskToRgbaBottomUp(mask, 2, 2, new byte[15]));
    }
}
