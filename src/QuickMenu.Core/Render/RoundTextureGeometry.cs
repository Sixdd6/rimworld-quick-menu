using System;

namespace QuickMenu.Core.Render;

/// <summary>
/// Texture size and draw rect for a round mask rasterized at its drawn size (rings at the slot size, the disc rim at
/// the disc diameter). The mask's outer edge sits <see cref="MaskRasterizer.EdgePadding"/> texels inside the texture
/// border, so the texture is drawn in a rect slightly larger than the shape; this type computes that rect so the outer
/// edge lands exactly on <c>diameter / 2</c> UI px from the centre.
/// </summary>
/// <param name="TextureSize">Square texture size in texels (≥ 3).</param>
/// <param name="TexelsPerUi">Texels per UI pixel: the UI scale, adjusted by less than one texel over the diameter, or
/// lower when <see cref="MaxTextureSize"/> caps the texture.</param>
/// <param name="DrawSize">Side of the square UI rect, centred on the shape centre, in which the texture is drawn.</param>
public readonly record struct RoundTextureGeometry(int TextureSize, float TexelsPerUi, float DrawSize)
{
    /// <summary>Largest texture side for one round mask (16 MB of RGBA). Above it the mask is upscaled slightly.</summary>
    public const int MaxTextureSize = 2048;

    private const float Padding = MaskRasterizer.EdgePadding;

    /// <summary>Converts a width in UI px (a stroke) to texels of this texture.</summary>
    public float Texels(float uiPixels) => uiPixels * TexelsPerUi;

    /// <summary>
    /// Geometry for a round shape of <paramref name="diameter"/> UI px drawn at <paramref name="uiScale"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">A size or scale is not positive and finite, or the cap is below 3.</exception>
    public static RoundTextureGeometry For(float diameter, float uiScale, int maxTextureSize = MaxTextureSize)
    {
        if (!(diameter > 0f) || float.IsInfinity(diameter))
        {
            throw new ArgumentOutOfRangeException(nameof(diameter), diameter, "Diameter must be positive and finite.");
        }

        if (!(uiScale > 0f) || float.IsInfinity(uiScale))
        {
            throw new ArgumentOutOfRangeException(nameof(uiScale), uiScale, "UI scale must be positive and finite.");
        }

        if (maxTextureSize < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(maxTextureSize), maxTextureSize, "Cap must be ≥ 3 texels.");
        }

        int pad = (int)(2 * Padding);

        // A small tolerance keeps 56 × 1.0 from becoming 57 through float noise.
        double wanted = Math.Ceiling(((double)diameter * uiScale) - 1e-4);
        int inner = (int)Math.Clamp(wanted, 1.0, maxTextureSize - pad);
        int size = inner + pad;
        float texelsPerUi = inner / diameter;
        return new RoundTextureGeometry(size, texelsPerUi, size / texelsPerUi);
    }

    /// <summary>
    /// Draw size for a fixed-size round mask (for example the 256 px <c>Disc</c> or <c>Circle</c>) so that its
    /// outer edge, inset by the padding, lands on <paramref name="diameter"/>.
    /// </summary>
    public static float DrawSizeForFixedMask(float diameter, int maskSize)
    {
        if (maskSize < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(maskSize), maskSize, "Mask size must be ≥ 3 px.");
        }

        return diameter * maskSize / (maskSize - (2f * Padding));
    }
}
