using System;
using QuickMenu.Core.Render;
using Xunit;

namespace QuickMenu.Tests.Render;

/// <summary>design.md §2 colour tokens: exact values and the contrast claims of the table (WCAG 2.2 AA).</summary>
public class StyleTokensTests
{
    private static readonly Rgba32 _black = Rgba32.FromHex(0x000000);

    // The game's ColorLibrary.RedReadable on 1.7.4982 is new Color(1f, 0.2f, 0.2f).
    private static readonly Rgba32 _gameRed = Rgba32.FromFloats(1f, 0.2f, 0.2f, 1f);

    public static TheoryData<string, uint, byte> Tokens() => new()
    {
        { nameof(StyleTokens.Accent), 0x38BDF8, 255 },
        { nameof(StyleTokens.DiscCentre), 0x000000, 217 },
        { nameof(StyleTokens.DiscEdge), 0x000000, 140 },
        { nameof(StyleTokens.RimOutline), 0x000000, 153 },
        { nameof(StyleTokens.SlotBg), 0x1E2328, 235 },
        { nameof(StyleTokens.SlotBgHover), 0x2A3540, 255 },
        { nameof(StyleTokens.SlotBgPressed), 0x15191D, 255 },
        { nameof(StyleTokens.SlotBorder), 0x56626E, 255 },
        { nameof(StyleTokens.EmptyBorder), 0x8A96A0, 255 },
        { nameof(StyleTokens.Text), 0xFFFFFF, 255 },
        { nameof(StyleTokens.TextMuted), 0xA7B0B8, 255 },
        { nameof(StyleTokens.Danger), 0xFF7A70, 255 },
        { nameof(StyleTokens.UnavailableIcon), 0x808080, 191 },
        { nameof(StyleTokens.Plate), 0x000000, 204 },
        { nameof(StyleTokens.WindowBg), 0x15191D, 255 },
        { nameof(StyleTokens.PinMarker), 0x38BDF8, 255 },
        { nameof(StyleTokens.PinMarkerPartial), 0xFFFFFF, 255 },
        { nameof(StyleTokens.PinMarkerOutline), 0x000000, 217 },
        { nameof(StyleTokens.GizmoBgLight), 0x2F4453, 255 },
        { nameof(StyleTokens.GizmoBgDark), 0x1D2B33, 255 },
    };

    // ---- Architect pin marker (T-89, design.md §2 "Architect pin marker"): WCAG 1.4.11, 3:1 for graphics ----------

    public static TheoryData<string> MarkerFills() => [nameof(StyleTokens.PinMarker), nameof(StyleTokens.PinMarkerPartial)];

    private static Rgba32 Token(string name) => (Rgba32)typeof(StyleTokens).GetField(name)!.GetValue(null)!;

    [Theory]
    [MemberData(nameof(MarkerFills))]
    public void PinMarker_MeetsTheComponentMinimum_OnTheVanillaButtonBackground(string fill)
    {
        // Lightest and darkest pixels of the game's button background where the marker sits (1.7.4982 DesButBG).
        Rgba32 marker = Token(fill);

        Assert.Equal(255, marker.A);
        Assert.True(ColorContrast.Ratio(marker, StyleTokens.GizmoBgLight) >= ColorContrast.ComponentMinimum);
        Assert.True(ColorContrast.Ratio(marker, StyleTokens.GizmoBgDark) >= ColorContrast.ComponentMinimum);
    }

    [Theory]
    [MemberData(nameof(MarkerFills))]
    public void PinMarker_StaysReadableOverAnyIcon_ThroughItsOutline(string fill)
    {
        // The marker overlaps the designator icon's corner, which can be any colour. Over a white icon the outline
        // separates the marker from the icon (outline vs icon) and the fill from the outline (fill vs outline); over a
        // dark icon the fill alone does (as on the button background).
        Rgba32 marker = Token(fill);
        Rgba32 white = Rgba32.FromHex(0xFFFFFF);
        Rgba32 outlineOverWhite = ColorContrast.Over(StyleTokens.PinMarkerOutline, white);

        Assert.True(ColorContrast.Ratio(outlineOverWhite, white) >= ColorContrast.ComponentMinimum);
        Assert.True(ColorContrast.Ratio(marker, outlineOverWhite) >= ColorContrast.ComponentMinimum);
        foreach (uint icon in new uint[] { 0x000000, 0x404040, 0x808080, 0xC0C0C0, 0x38BDF8, 0xFF0000, 0x00FF00, 0xFFFF00 })
        {
            Rgba32 outlineOverIcon = ColorContrast.Over(StyleTokens.PinMarkerOutline, Rgba32.FromHex(icon));
            Assert.True(ColorContrast.Ratio(marker, outlineOverIcon) >= ColorContrast.ComponentMinimum, $"{fill} over icon {icon:X6}");
        }
    }

    [Fact]
    public void PinMarkerOutline_IsStrongerThanTheRimOutline_BecauseRimOutlineFailsOverWhite()
    {
        Rgba32 white = Rgba32.FromHex(0xFFFFFF);

        Assert.True(StyleTokens.PinMarkerOutline.A > StyleTokens.RimOutline.A);
        Assert.True(ColorContrast.Ratio(StyleTokens.PinMarker, ColorContrast.Over(StyleTokens.RimOutline, white)) < ColorContrast.ComponentMinimum);
    }

    [Fact]
    public void PinMarkerPartial_IsWhiteAndDistinctFromTheFullMarker()
    {
        Assert.Equal(Rgba32.FromHex(0xFFFFFF), StyleTokens.PinMarkerPartial);
        Assert.NotEqual(StyleTokens.PinMarker, StyleTokens.PinMarkerPartial);
        Assert.Equal(StyleTokens.Accent, StyleTokens.PinMarker);
    }

    [Theory]
    [MemberData(nameof(Tokens))]
    public void Token_HasExactDesignValue(string name, uint rgb, byte alpha)
    {
        Rgba32 token = (Rgba32)typeof(StyleTokens).GetField(name)!.GetValue(null)!;

        Assert.Equal(rgb, token.Rgb);
        Assert.Equal(alpha, token.A);
    }

    [Fact]
    public void DimmedMenuAlpha_Is045()
    {
        Assert.Equal(0.45f, StyleTokens.DimmedMenuAlpha);
    }

    [Fact]
    public void Contrast_MatchesKnownWcagValues()
    {
        Assert.Equal(21.0, ColorContrast.Ratio(StyleTokens.Text, _black), 3);
        Assert.Equal(1.0, ColorContrast.Ratio(StyleTokens.Accent, StyleTokens.Accent), 6);
        Assert.Equal(ColorContrast.Ratio(StyleTokens.Accent, _black), ColorContrast.Ratio(_black, StyleTokens.Accent), 9);
    }

    [Fact]
    public void TextPairs_MeetAaOnSlotBackground()
    {
        Rgba32 slot = ColorContrast.Over(StyleTokens.SlotBg, _black);

        Assert.True(ColorContrast.Ratio(StyleTokens.Text, slot) >= 15.5);
        Assert.True(ColorContrast.Ratio(StyleTokens.TextMuted, slot) >= 7.0);
        Assert.True(ColorContrast.Ratio(StyleTokens.Danger, slot) >= 6.0);
        Assert.True(ColorContrast.Ratio(StyleTokens.Accent, slot) >= 7.0);
        Assert.True(ColorContrast.Ratio(StyleTokens.Accent, StyleTokens.SlotBgHover) >= 5.5);
        Assert.True(ColorContrast.Ratio(StyleTokens.Danger, StyleTokens.WindowBg) >= 6.9);
        Assert.True(ColorContrast.Ratio(StyleTokens.Accent, StyleTokens.WindowBg) >= 8.0);
    }

    [Fact]
    public void EmptyBorder_MeetsComponentMinimumOverDarkGround()
    {
        Rgba32 slot = ColorContrast.Over(StyleTokens.SlotBg, _black);

        Assert.True(ColorContrast.Ratio(StyleTokens.EmptyBorder, slot) >= ColorContrast.ComponentMinimum);
    }

    [Fact]
    public void EmptySlot_IsFainterThanAFilledSlot_T81()
    {
        // S4 state 6 (T-81): fill slotBg at 50 %, ring slotBorder at 70 %.
        Assert.Equal(StyleTokens.SlotBg.Rgb, StyleTokens.SlotBgEmpty.Rgb);
        Assert.Equal(Rgba32.FromHex(0x1E2328, 0.50f), StyleTokens.SlotBgEmpty);
        Assert.Equal(Rgba32.FromHex(0x56626E, 0.70f), StyleTokens.SlotBorderEmpty);
        Assert.Equal(StyleTokens.SlotBorder.Rgb, StyleTokens.SlotBorderEmpty.Rgb);
        Assert.True(StyleTokens.SlotBgEmpty.A < StyleTokens.SlotBg.A);
        Assert.True(StyleTokens.SlotBorderEmpty.A < StyleTokens.SlotBorder.A);
    }

    [Fact]
    public void Over_BlendsByTopAlpha()
    {
        Rgba32 half = Rgba32.FromHex(0xFFFFFF, 0.5f);

        Assert.Equal(new Rgba32(128, 128, 128, 255), ColorContrast.Over(half, _black));
        Assert.Equal(new Rgba32(0x38, 0xBD, 0xF8, 255), ColorContrast.Over(StyleTokens.Accent, _black));
    }

    [Fact]
    public void ChooseDanger_KeepsOurRed_WhenGameRedFailsTextContrast()
    {
        // RedReadable (#FF3333) is within 3:1 of #FF7A70 but only ≈ 3.4:1 on slotBgHover: below AA for text.
        Assert.True(ColorContrast.Ratio(_gameRed, StyleTokens.Danger) <= 3.0);
        Assert.True(ColorContrast.Ratio(_gameRed, StyleTokens.SlotBgHover) < ColorContrast.TextMinimum);
        Assert.Equal(StyleTokens.Danger, StyleTokens.ChooseDanger(_gameRed, StyleTokens.WindowBg));
    }

    [Fact]
    public void ChooseDanger_UsesGameRed_WhenCloseAndReadable()
    {
        Rgba32 lighter = Rgba32.FromHex(0xFF8A80);

        Assert.Equal(lighter, StyleTokens.ChooseDanger(lighter, StyleTokens.WindowBg));
    }

    [Fact]
    public void ChooseDanger_KeepsOurRed_WhenGameRedIsFarAway()
    {
        Rgba32 darkRed = Rgba32.FromHex(0x800000);

        Assert.True(ColorContrast.Ratio(darkRed, StyleTokens.Danger) > 3.0);
        Assert.Equal(StyleTokens.Danger, StyleTokens.ChooseDanger(darkRed, StyleTokens.WindowBg));
    }

    [Fact]
    public void ChooseDanger_ResultIsOpaque()
    {
        Rgba32 translucent = Rgba32.FromHex(0xFF8A80, 0.5f);

        Assert.Equal(255, StyleTokens.ChooseDanger(translucent, StyleTokens.WindowBg).A);
    }

    [Fact]
    public void Rgba32_FromHexAndFloats()
    {
        Assert.Equal(new Rgba32(0x12, 0x34, 0x56, 255), Rgba32.FromHex(0x123456));
        Assert.Equal(new Rgba32(255, 0, 128, 0), Rgba32.FromFloats(2f, -1f, 0.5f, 0f));
        Assert.Equal(0x123456u, Rgba32.FromHex(0x123456).Rgb);
        Assert.Equal(1f, Rgba32.FromHex(0x123456).Alpha);
        Assert.Throws<ArgumentOutOfRangeException>(() => Rgba32.FromHex(0x1000000));
        Assert.Throws<ArgumentOutOfRangeException>(() => Rgba32.FromFloats(float.NaN, 0f, 0f, 1f));
    }
}
