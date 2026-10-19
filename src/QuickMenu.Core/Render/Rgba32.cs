using System;

namespace QuickMenu.Core.Render;

/// <summary>
/// An sRGB colour with straight (not premultiplied) alpha, one byte per channel, in R, G, B, A order. The layout
/// matches Unity's <c>Color32</c>, so a <c>Color32[]</c> viewed as bytes holds the same values.
/// </summary>
/// <param name="R">Red.</param>
/// <param name="G">Green.</param>
/// <param name="B">Blue.</param>
/// <param name="A">Alpha (255 = opaque).</param>
public readonly record struct Rgba32(byte R, byte G, byte B, byte A)
{
    /// <summary>Opaque colour from a 24-bit <c>0xRRGGBB</c> value.</summary>
    public static Rgba32 FromHex(uint rgb) => FromHex(rgb, 1f);

    /// <summary>Colour from a 24-bit <c>0xRRGGBB</c> value and an alpha in 0…1.</summary>
    public static Rgba32 FromHex(uint rgb, float alpha)
    {
        if (rgb > 0xFFFFFFu)
        {
            throw new ArgumentOutOfRangeException(nameof(rgb), rgb, "Expected a 24-bit 0xRRGGBB value.");
        }

        return new Rgba32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, ToByte(alpha));
    }

    /// <summary>Colour from channels in 0…1 (as Unity's <c>Color</c> stores them); values are clamped and rounded.</summary>
    public static Rgba32 FromFloats(float r, float g, float b, float a) => new(ToByte(r), ToByte(g), ToByte(b), ToByte(a));

    /// <summary>Alpha in 0…1.</summary>
    public float Alpha => A / 255f;

    /// <summary>The 24-bit <c>0xRRGGBB</c> value.</summary>
    public uint Rgb => ((uint)R << 16) | ((uint)G << 8) | B;

    private static byte ToByte(float level)
    {
        if (float.IsNaN(level))
        {
            throw new ArgumentOutOfRangeException(nameof(level), level, "Channel is NaN.");
        }

        return (byte)((Math.Clamp(level, 0f, 1f) * 255f) + 0.5f);
    }
}
