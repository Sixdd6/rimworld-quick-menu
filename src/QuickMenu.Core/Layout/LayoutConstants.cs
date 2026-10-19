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

    /// <summary>Pitch P = B + G.</summary>
    public static float Pitch(float buttonSize) => buttonSize + Gap;

    /// <summary>Hub sub-button diameter e = clamp(round(0.36 B), 18, 28).</summary>
    public static float HubSubDiameter(float buttonSize)
    {
        var rounded = MathF.Round(0.36f * buttonSize, MidpointRounding.AwayFromZero);
        return Math.Clamp(rounded, 18f, 28f);
    }

    /// <summary>Hub sub-button centre offset below the hub centre: max(B/2 - e/2 - 2, e/2 + 6).</summary>
    public static float HubSubOffset(float buttonSize)
    {
        var e = HubSubDiameter(buttonSize);
        return MathF.Max((buttonSize / 2f) - (e / 2f) - 2f, (e / 2f) + 6f);
    }

    /// <summary>Disc radius D = m P + B/2 + rimPad for m rings in use.</summary>
    public static float DiscRadius(float buttonSize, int rings) =>
        (rings * Pitch(buttonSize)) + (buttonSize / 2f) + RimPad;
}
