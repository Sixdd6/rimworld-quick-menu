using System;

namespace QuickMenu.Core.Layout;

/// <summary>Constants and derived sizes from design.md section 1.1. All values are UI pixels.</summary>
public static class LayoutConstants
{
    public const float Gap = 8f;
    public const float RimPad = 12f;
    public const float PlateHeight = 30f;
    public const float PlateGap = 8f;
    public const float Margin = 8f;
    public const int MinButtonSize = 36;
    public const int MaxButtonSize = 96;
    public const int ButtonSizeStep = 4;
    public const int MinRings = 1;
    public const int MaxRings = 5;

    /// <summary>
    /// Rings the Auto wheel can grow to (design.md §1.1 "Auto full ... or m = 8"). Larger than <see cref="MaxRings"/>,
    /// which limits only the Fixed rings setting; anything sized by the rings in use must accept this many.
    /// </summary>
    public const int AutoMaxRings = 8;

    /// <summary>Pitch P = B + G.</summary>
    public static float Pitch(float buttonSize) => buttonSize + Gap;

    /// <summary>Drawn hub (Edit button) diameter H = round(0.64 B), half away from zero (design.md §1.1, S4a).</summary>
    public static float HubDiameter(float buttonSize) =>
        MathF.Round(0.64f * buttonSize, MidpointRounding.AwayFromZero);

    /// <summary>Hub glyph (pencil or check mark) size Hg = round(0.5 H), half away from zero.</summary>
    public static float HubGlyphSize(float buttonSize) =>
        MathF.Round(0.5f * HubDiameter(buttonSize), MidpointRounding.AwayFromZero);

    /// <summary>Disc radius D = m P + B/2 + rimPad for m rings in use.</summary>
    public static float DiscRadius(float buttonSize, int rings) =>
        (rings * Pitch(buttonSize)) + (buttonSize / 2f) + RimPad;
}
