using System;

namespace QuickMenu.Core.Render;

/// <summary>Converts the rasterizers' alpha masks to the RGBA layout Unity's <c>SetPixels32</c> expects.</summary>
public static class TexturePixels
{
    /// <summary>
    /// Writes <paramref name="mask"/> (one alpha byte per pixel, <b>top row first</b>) into <paramref name="rgba"/> as
    /// white RGBA with that alpha, <b>bottom row first</b>. The rows are flipped because <c>SetPixels32</c> takes the
    /// bottom row first and the glyphs are not vertically symmetric. Allocates nothing.
    /// </summary>
    public static void MaskToRgbaBottomUp(ReadOnlySpan<byte> mask, int width, int height, Span<byte> rgba)
    {
        if (width < 1 || height < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(width), $"{width}×{height}", "Width and height must be ≥ 1.");
        }

        if (mask.Length < width * height)
        {
            throw new ArgumentException($"Mask needs {width * height} bytes, has {mask.Length}.", nameof(mask));
        }

        if (rgba.Length < width * height * 4)
        {
            throw new ArgumentException($"Destination needs {width * height * 4} bytes, has {rgba.Length}.", nameof(rgba));
        }

        for (int y = 0; y < height; y++)
        {
            ReadOnlySpan<byte> src = mask.Slice(y * width, width);
            Span<byte> dst = rgba.Slice((height - 1 - y) * width * 4, width * 4);
            for (int x = 0; x < width; x++)
            {
                int p = x * 4;
                dst[p] = 255;
                dst[p + 1] = 255;
                dst[p + 2] = 255;
                dst[p + 3] = src[x];
            }
        }
    }
}
