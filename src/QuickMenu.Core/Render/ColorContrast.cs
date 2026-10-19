using System;

namespace QuickMenu.Core.Render;

/// <summary>WCAG 2.2 relative luminance and contrast ratio for <see cref="Rgba32"/> colours (design.md §2, §5).</summary>
public static class ColorContrast
{
    /// <summary>WCAG AA minimum for normal text.</summary>
    public const double TextMinimum = 4.5;

    /// <summary>WCAG AA minimum for user-interface components and graphics.</summary>
    public const double ComponentMinimum = 3.0;

    /// <summary>WCAG relative luminance of the colour's RGB (alpha ignored), 0 (black) … 1 (white).</summary>
    public static double Luminance(Rgba32 c) =>
        (0.2126 * Linear(c.R)) + (0.7152 * Linear(c.G)) + (0.0722 * Linear(c.B));

    /// <summary>WCAG contrast ratio of two opaque colours, 1 … 21, independent of order.</summary>
    public static double Ratio(Rgba32 a, Rgba32 b)
    {
        double la = Luminance(a);
        double lb = Luminance(b);
        double hi = Math.Max(la, lb);
        double lo = Math.Min(la, lb);
        return (hi + 0.05) / (lo + 0.05);
    }

    /// <summary>
    /// Composites <paramref name="top"/> over the opaque <paramref name="below"/> by its alpha (sRGB blending, as
    /// IMGUI does). The result is opaque.
    /// </summary>
    public static Rgba32 Over(Rgba32 top, Rgba32 below)
    {
        float a = top.Alpha;
        return new Rgba32(Mix(top.R, below.R, a), Mix(top.G, below.G, a), Mix(top.B, below.B, a), 255);
    }

    private static byte Mix(byte top, byte below, float a) => (byte)((top * a) + (below * (1f - a)) + 0.5f);

    private static double Linear(byte channel)
    {
        double s = channel / 255.0;
        return s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
    }
}
