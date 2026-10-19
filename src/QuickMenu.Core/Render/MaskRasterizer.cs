using System;

namespace QuickMenu.Core.Render;

/// <summary>
/// Pure, deterministic rasterizer for the grayscale alpha masks of the menu (design.md §3, §6; ADR 0007).
/// Each shape is a signed-distance field sampled at pixel centres; coverage is
/// <c>clamp(0.5 − distance, 0, 1)</c>, which gives an anti-aliased edge about one pixel wide.
/// </summary>
/// <remarks>
/// <para>Output is one byte of alpha per pixel, row-major, <b>top row first</b> (PNG order).
/// Unity's <c>SetPixels32</c> expects the bottom row first, so the adapter flips rows
/// (the round shapes are symmetric, so only asymmetric future glyphs care).</para>
/// <para>Round shapes (circle, disc, ring) are inset by <see cref="EdgePadding"/> so the border pixels are 0
/// and clamp-sampled bilinear scaling never smears the edge. Rounded rects fill the full box, so the straight
/// edges reach the border, as 9-slice drawing needs.</para>
/// <para>The array overloads allocate the result; the <see cref="Span{T}"/> overloads write into a caller buffer
/// and allocate nothing.</para>
/// </remarks>
public static class MaskRasterizer
{
    /// <summary>Gap in pixels between the texture border and the outer edge of round shapes.</summary>
    public const float EdgePadding = 1f;

    /// <summary>Disc rim level relative to the centre: <c>discEdge</c> α 0.55 over <c>discCentre</c> α 0.85 (design.md §2).</summary>
    public const float DefaultDiscEdgeLevel = 0.55f / 0.85f;

    /// <summary>Filled anti-aliased circle of diameter <c>size − 2·EdgePadding</c>.</summary>
    public static byte[] Circle(int size)
    {
        byte[] result = new byte[CheckRoundSize(size) * size];
        Circle(size, result);
        return result;
    }

    /// <inheritdoc cref="Circle(int)"/>
    public static void Circle(int size, Span<byte> destination) => Disc(size, 1f, destination);

    /// <summary>Filled disc with a radial gradient from 255 at the centre to <see cref="DefaultDiscEdgeLevel"/> at the rim.</summary>
    public static byte[] Disc(int size) => Disc(size, DefaultDiscEdgeLevel);

    /// <summary>Filled disc whose level falls from 1 at the centre to <paramref name="edgeLevel"/> at the rim (quadratic in radius).</summary>
    public static byte[] Disc(int size, float edgeLevel)
    {
        byte[] result = new byte[CheckRoundSize(size) * size];
        Disc(size, edgeLevel, result);
        return result;
    }

    /// <inheritdoc cref="Disc(int, float)"/>
    public static void Disc(int size, float edgeLevel, Span<byte> destination)
    {
        CheckRoundSize(size);
        if (!(edgeLevel >= 0f && edgeLevel <= 1f))
        {
            throw new ArgumentOutOfRangeException(nameof(edgeLevel), edgeLevel, "Edge level must be in 0…1.");
        }

        CheckDestination(destination, size, size);
        double c = size / 2.0;
        double r = c - EdgePadding;
        double falloff = 1.0 - edgeLevel;
        for (int y = 0; y < size; y++)
        {
            double dy = y + 0.5 - c;
            int row = y * size;
            for (int x = 0; x < size; x++)
            {
                double dx = x + 0.5 - c;
                double d = Math.Sqrt((dx * dx) + (dy * dy));
                double coverage = Coverage(d - r);
                double t = Math.Min(d / r, 1.0);
                destination[row + x] = ToByte(coverage * (1.0 - (falloff * t * t)));
            }
        }
    }

    /// <summary>Anti-aliased ring whose outer edge is the circle edge and whose width is <paramref name="stroke"/> px.</summary>
    public static byte[] Ring(int size, float stroke)
    {
        byte[] result = new byte[CheckRoundSize(size) * size];
        Ring(size, stroke, result);
        return result;
    }

    /// <inheritdoc cref="Ring(int, float)"/>
    public static void Ring(int size, float stroke, Span<byte> destination)
    {
        CheckRoundSize(size);
        double c = size / 2.0;
        double outer = c - EdgePadding;
        if (!(stroke > 0f && stroke <= outer))
        {
            throw new ArgumentOutOfRangeException(nameof(stroke), stroke, "Stroke must be > 0 and fit inside the ring.");
        }

        CheckDestination(destination, size, size);
        double half = stroke / 2.0;
        double mid = outer - half;
        for (int y = 0; y < size; y++)
        {
            double dy = y + 0.5 - c;
            int row = y * size;
            for (int x = 0; x < size; x++)
            {
                double dx = x + 0.5 - c;
                double d = Math.Sqrt((dx * dx) + (dy * dy));
                destination[row + x] = ToByte(Coverage(Math.Abs(d - mid) - half));
            }
        }
    }

    /// <summary>
    /// Anti-aliased rounded rectangle filling the whole <paramref name="width"/> × <paramref name="height"/> box.
    /// <paramref name="radius"/> is clamped to half the short side.
    /// </summary>
    public static byte[] RoundedRect(int width, int height, float radius)
    {
        CheckRectSize(width, height);
        byte[] result = new byte[width * height];
        RoundedRect(width, height, radius, result);
        return result;
    }

    /// <inheritdoc cref="RoundedRect(int, int, float)"/>
    public static void RoundedRect(int width, int height, float radius, Span<byte> destination)
    {
        CheckRectSize(width, height);
        if (!(radius >= 0f))
        {
            throw new ArgumentOutOfRangeException(nameof(radius), radius, "Radius must be ≥ 0.");
        }

        CheckDestination(destination, width, height);
        double hx = width / 2.0;
        double hy = height / 2.0;
        double r = Math.Min(radius, Math.Min(hx, hy));
        double bx = hx - r;
        double by = hy - r;
        for (int y = 0; y < height; y++)
        {
            double qy = Math.Abs(y + 0.5 - hy) - by;
            int row = y * width;
            for (int x = 0; x < width; x++)
            {
                destination[row + x] = ToByte(Coverage(RoundedBoxDistance(Math.Abs(x + 0.5 - hx) - bx, qy, r)));
            }
        }
    }

    /// <summary>
    /// Anti-aliased dashed ring: the <see cref="Ring(int, float)"/> band cut into <paramref name="dashes"/> equal
    /// dashes, each covering <paramref name="duty"/> of its period. The first dash is centred at 12 o'clock
    /// (top row first), so the pattern is mirror-symmetric about the vertical axis.
    /// </summary>
    public static byte[] DashedRing(int size, float stroke, int dashes, float duty)
    {
        byte[] result = new byte[CheckRoundSize(size) * size];
        DashedRing(size, stroke, dashes, duty, result);
        return result;
    }

    /// <inheritdoc cref="DashedRing(int, float, int, float)"/>
    public static void DashedRing(int size, float stroke, int dashes, float duty, Span<byte> destination)
    {
        CheckRoundSize(size);
        double c = size / 2.0;
        double outer = c - EdgePadding;
        if (!(stroke > 0f && stroke <= outer))
        {
            throw new ArgumentOutOfRangeException(nameof(stroke), stroke, "Stroke must be > 0 and fit inside the ring.");
        }

        if (dashes < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(dashes), dashes, "Dash count must be ≥ 1.");
        }

        if (!(duty > 0f && duty <= 1f))
        {
            throw new ArgumentOutOfRangeException(nameof(duty), duty, "Duty must be in (0, 1].");
        }

        CheckDestination(destination, size, size);
        double half = stroke / 2.0;
        double mid = outer - half;
        double period = 2.0 * Math.PI / dashes;
        double halfDash = period * duty / 2.0;
        bool solid = duty >= 1f;
        for (int y = 0; y < size; y++)
        {
            double dy = y + 0.5 - c;
            int row = y * size;
            for (int x = 0; x < size; x++)
            {
                double dx = x + 0.5 - c;
                double d = Math.Sqrt((dx * dx) + (dy * dy));
                double sd = Math.Abs(d - mid) - half;
                if (!solid)
                {
                    // Angle measured clockwise from 12 o'clock (rows go down), folded into the nearest dash centre.
                    double a = Math.Atan2(dx, -dy);
                    double off = Math.Abs(a - (Math.Round(a / period) * period));
                    sd = Math.Max(sd, (off - halfDash) * d);
                }

                destination[row + x] = ToByte(Coverage(sd));
            }
        }
    }

    /// <summary>
    /// Anti-aliased outline of the <see cref="RoundedRect(int, int, float)"/> shape: a band <paramref name="stroke"/> px
    /// wide whose outer edge is the box edge. A stroke of at least half the short side gives the filled shape.
    /// </summary>
    public static byte[] RoundedRectRing(int width, int height, float radius, float stroke)
    {
        CheckRectSize(width, height);
        byte[] result = new byte[width * height];
        RoundedRectRing(width, height, radius, stroke, result);
        return result;
    }

    /// <inheritdoc cref="RoundedRectRing(int, int, float, float)"/>
    public static void RoundedRectRing(int width, int height, float radius, float stroke, Span<byte> destination)
    {
        CheckRectSize(width, height);
        if (!(radius >= 0f))
        {
            throw new ArgumentOutOfRangeException(nameof(radius), radius, "Radius must be ≥ 0.");
        }

        if (!(stroke > 0f))
        {
            throw new ArgumentOutOfRangeException(nameof(stroke), stroke, "Stroke must be > 0.");
        }

        CheckDestination(destination, width, height);
        double hx = width / 2.0;
        double hy = height / 2.0;
        double r = Math.Min(radius, Math.Min(hx, hy));
        double bx = hx - r;
        double by = hy - r;
        bool filled = stroke >= Math.Min(hx, hy);
        for (int y = 0; y < height; y++)
        {
            double qy = Math.Abs(y + 0.5 - hy) - by;
            int row = y * width;
            for (int x = 0; x < width; x++)
            {
                double sd = RoundedBoxDistance(Math.Abs(x + 0.5 - hx) - bx, qy, r);
                if (!filled)
                {
                    sd = Math.Max(sd, -(sd + stroke));
                }

                destination[row + x] = ToByte(Coverage(sd));
            }
        }
    }

    private static double RoundedBoxDistance(double qx, double qy, double r)
    {
        double ox = Math.Max(qx, 0.0);
        double oy = Math.Max(qy, 0.0);
        return Math.Sqrt((ox * ox) + (oy * oy)) + Math.Min(Math.Max(qx, qy), 0.0) - r;
    }

    private static double Coverage(double signedDistance) => Math.Clamp(0.5 - signedDistance, 0.0, 1.0);

    private static byte ToByte(double level) => (byte)((level * 255.0) + 0.5);

    private static int CheckRoundSize(int size)
    {
        if (size < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "Mask size must be ≥ 3 px.");
        }

        return size;
    }

    private static void CheckRectSize(int width, int height)
    {
        if (width < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be ≥ 1 px.");
        }

        if (height < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be ≥ 1 px.");
        }
    }

    private static void CheckDestination(Span<byte> destination, int width, int height)
    {
        if (destination.Length < width * height)
        {
            throw new ArgumentException($"Destination needs {width * height} bytes, has {destination.Length}.", nameof(destination));
        }
    }
}
