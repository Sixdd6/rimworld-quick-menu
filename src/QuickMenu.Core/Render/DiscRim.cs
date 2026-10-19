using System;

namespace QuickMenu.Core.Render;

/// <summary>
/// The disc rim of design.md §2 (2 px <c>accent</c> with a 1 px <c>rimOutline</c> on each side), rasterized at the
/// actual disc diameter so the widths stay exact at any disc size. The two colours are baked into one RGBA image
/// (rim composited over outline), so the adapter draws it with a single untinted texture; <c>GUI.color</c> alpha still
/// dims it.
/// </summary>
/// <remarks>
/// <para>Geometry matches <see cref="MaskRasterizer.Ring(int, float)"/>: centre at <c>size / 2</c>, outer edge at
/// <c>size / 2 − EdgePadding</c>, coverage <c>clamp(0.5 − d, 0, 1)</c>. Only the pixels near the band are evaluated
/// (the rest is cleared), so a 2048 px rim costs about a memset.</para>
/// <para>Output is RGBA, straight alpha, <b>bottom row first</b> (Unity <c>SetPixels32</c> order), ready to be
/// viewed as <c>Color32[]</c>.</para>
/// </remarks>
public static class DiscRim
{
    /// <summary>Accent band width in UI px.</summary>
    public const float RimWidth = 2f;

    /// <summary>Dark outline width on each side of the accent band, in UI px.</summary>
    public const float OutlineWidth = 1f;

    /// <summary>Total band width (outline + rim + outline) in UI px, measured inward from the disc edge.</summary>
    public const float BandWidth = RimWidth + (2f * OutlineWidth);

    /// <summary>Bytes needed for a <paramref name="size"/> × <paramref name="size"/> RGBA image.</summary>
    public static int ByteCount(int size) => size * size * 4;

    /// <summary>
    /// Renders the rim into the first <see cref="ByteCount"/> bytes of <paramref name="rgba"/>. Allocates nothing.
    /// </summary>
    /// <param name="size">Texture side in texels (≥ 3).</param>
    /// <param name="rimTexels">Accent band width in texels (&gt; 0).</param>
    /// <param name="outlineTexels">Outline width per side in texels (≥ 0).</param>
    /// <param name="rim">Accent colour.</param>
    /// <param name="outline">Outline colour.</param>
    /// <param name="rgba">Destination, RGBA bytes, bottom row first.</param>
    public static void Render(int size, float rimTexels, float outlineTexels, Rgba32 rim, Rgba32 outline, Span<byte> rgba)
    {
        if (size < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "Size must be ≥ 3 px.");
        }

        double c = size / 2.0;
        double outer = c - MaskRasterizer.EdgePadding;
        if (!(rimTexels > 0f) || !(outlineTexels >= 0f) || rimTexels + (2.0 * outlineTexels) > outer)
        {
            throw new ArgumentOutOfRangeException(nameof(rimTexels), rimTexels, "Rim and outlines must be positive and fit inside the disc.");
        }

        int bytes = ByteCount(size);
        if (rgba.Length < bytes)
        {
            throw new ArgumentException($"Destination needs {bytes} bytes, has {rgba.Length}.", nameof(rgba));
        }

        Span<byte> image = rgba.Slice(0, bytes);
        image.Clear();

        double band = rimTexels + (2.0 * outlineTexels);
        double bandHalf = band / 2.0;
        double bandMid = outer - bandHalf;
        double rimHalf = rimTexels / 2.0;
        double rimMid = outer - outlineTexels - rimHalf;

        // Coverage is 0 at distance ≥ 0.5 outside the band; one extra texel keeps the spans conservative.
        double reachOut = outer + 1.5;
        double reachIn = outer - band - 1.5;

        double rimA = rim.A / 255.0;
        double outA = outline.A / 255.0;
        int stride = size * 4;
        for (int y = 0; y < size; y++)
        {
            double dy = y + 0.5 - c;
            if (Math.Abs(dy) >= reachOut)
            {
                continue;
            }

            double xo = Math.Sqrt((reachOut * reachOut) - (dy * dy));
            int x0 = Math.Max(0, (int)Math.Floor(c - xo - 0.5));
            int x1 = Math.Min(size - 1, (int)Math.Ceiling(c + xo - 0.5));

            // Hole: pixels strictly inside reachIn on this row are fully transparent.
            int h0 = x1 + 1;
            int h1 = x1;
            if (reachIn > 0 && Math.Abs(dy) < reachIn)
            {
                double xi = Math.Sqrt((reachIn * reachIn) - (dy * dy));
                h0 = (int)Math.Ceiling(c - xi - 0.5) + 1;
                h1 = (int)Math.Floor(c + xi - 0.5) - 1;
            }

            Span<byte> row = image.Slice((size - 1 - y) * stride, stride);
            for (int x = x0; x <= x1; x++)
            {
                if (x >= h0 && x <= h1)
                {
                    x = h1;
                    continue;
                }

                double dx = x + 0.5 - c;
                double d = Math.Sqrt((dx * dx) + (dy * dy));
                double ar = rimA * Coverage(Math.Abs(d - rimMid) - rimHalf);
                double ao = outA * Coverage(Math.Abs(d - bandMid) - bandHalf);
                double a = ar + (ao * (1.0 - ar));
                if (a <= 0.0)
                {
                    continue;
                }

                double wo = ao * (1.0 - ar);
                int p = x * 4;
                row[p] = ToByte(((rim.R * ar) + (outline.R * wo)) / (a * 255.0));
                row[p + 1] = ToByte(((rim.G * ar) + (outline.G * wo)) / (a * 255.0));
                row[p + 2] = ToByte(((rim.B * ar) + (outline.B * wo)) / (a * 255.0));
                row[p + 3] = ToByte(a);
            }
        }
    }

    private static double Coverage(double signedDistance) => Math.Clamp(0.5 - signedDistance, 0.0, 1.0);

    private static byte ToByte(double level) => (byte)((Math.Clamp(level, 0.0, 1.0) * 255.0) + 0.5);
}
