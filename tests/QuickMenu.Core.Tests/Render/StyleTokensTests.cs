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
    };

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
