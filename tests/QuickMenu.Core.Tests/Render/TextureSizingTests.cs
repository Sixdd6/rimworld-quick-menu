using System;
using QuickMenu.Core.Layout;
using QuickMenu.Core.Render;
using Xunit;

namespace QuickMenu.Tests.Render;

public class TextureSizingTests
{
    // ---- RoundTextureGeometry ------------------------------------------------------------------

    [Theory]
    [InlineData(56f, 1f)]
    [InlineData(56f, 1.5f)]
    [InlineData(36f, 2f)]
    [InlineData(336f, 1.25f)]
    [InlineData(1160f, 1f)]
    public void Geometry_OuterEdgeLandsOnTheDiameter(float diameter, float scale)
    {
        RoundTextureGeometry g = RoundTextureGeometry.For(diameter, scale);

        // MaskRasterizer puts the outer edge at size/2 − 1 texels; in UI px that must be diameter/2.
        float outerUi = ((g.TextureSize / 2f) - MaskRasterizer.EdgePadding) / g.TexelsPerUi;
        Assert.Equal(diameter / 2f, outerUi, 3);
        Assert.Equal(g.TextureSize / g.TexelsPerUi, g.DrawSize, 3);

        // Texels per UI px stays within one texel over the diameter of the UI scale.
        Assert.InRange(g.TexelsPerUi, scale, scale + (1f / diameter) + 1e-4f);
    }

    [Fact]
    public void Geometry_ExactScale_HasNoFloatNoise()
    {
        RoundTextureGeometry g = RoundTextureGeometry.For(56f, 1f);

        Assert.Equal(58, g.TextureSize);
        Assert.Equal(1f, g.TexelsPerUi);
        Assert.Equal(2f, g.Texels(2f));
    }

    [Fact]
    public void Geometry_CapsHugeTextures_AndKeepsUiWidths()
    {
        RoundTextureGeometry g = RoundTextureGeometry.For(1160f, 4f);

        Assert.Equal(RoundTextureGeometry.MaxTextureSize, g.TextureSize);
        Assert.True(g.TexelsPerUi < 4f);
        Assert.Equal(2f * g.TexelsPerUi, g.Texels(2f));
        float outerUi = ((g.TextureSize / 2f) - 1f) / g.TexelsPerUi;
        Assert.Equal(580f, outerUi, 2);
    }

    [Theory]
    [InlineData(0f, 1f, 2048)]
    [InlineData(-1f, 1f, 2048)]
    [InlineData(float.NaN, 1f, 2048)]
    [InlineData(float.PositiveInfinity, 1f, 2048)]
    [InlineData(56f, 0f, 2048)]
    [InlineData(56f, float.NaN, 2048)]
    [InlineData(56f, float.PositiveInfinity, 2048)]
    [InlineData(56f, 1f, 2)]
    public void Geometry_RejectsBadInput(float diameter, float scale, int cap)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RoundTextureGeometry.For(diameter, scale, cap));
    }

    [Fact]
    public void FixedMaskDrawSize_CompensatesThePadding()
    {
        float draw = RoundTextureGeometry.DrawSizeForFixedMask(336f, 256);

        // The 256 px mask's edge is at 127 texels from the centre: 127/128 of the half draw size.
        Assert.Equal(168f, draw / 2f * 127f / 128f, 3);
        Assert.Throws<ArgumentOutOfRangeException>(() => RoundTextureGeometry.DrawSizeForFixedMask(10f, 2));
    }

    // ---- TextureSizing -------------------------------------------------------------------------

    [Fact]
    public void Sizing_DerivesDiametersFromLayout()
    {
        TextureSizing s = TextureSizing.Create(56f, 2, 1f);

        Assert.Equal(2f * LayoutConstants.DiscRadius(56f, 2), s.DiscDiameter);
        Assert.Equal(LayoutConstants.HubSubDiameter(56f), s.HubSubDiameter);
        Assert.Equal(58, s.SlotRing.TextureSize);
        Assert.Equal((int)s.DiscDiameter + 2, s.Rim.TextureSize);
        Assert.Equal(RoundTextureGeometry.For(s.HubSubDiameter, 1f), s.HubRing);
    }

    [Theory]
    [InlineData(0f, 1, 1f)]
    [InlineData(float.NaN, 1, 1f)]
    [InlineData(float.PositiveInfinity, 1, 1f)]
    [InlineData(56f, -1, 1f)]
    [InlineData(56f, 6, 1f)]
    [InlineData(56f, 1, 0f)]
    [InlineData(56f, 1, float.NaN)]
    [InlineData(56f, 1, float.PositiveInfinity)]
    public void Sizing_RejectsBadInput(float b, int rings, float scale)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TextureSizing.Create(b, rings, scale));
    }

    [Fact]
    public void Changed_FirstBuildIsEverything()
    {
        Assert.Equal(SizedGroups.All, TextureSizing.Changed(null, TextureSizing.Create(56f, 1, 1f)));
    }

    [Fact]
    public void Changed_SameSizingIsNothing()
    {
        TextureSizing s = TextureSizing.Create(56f, 1, 1f);

        Assert.Equal(SizedGroups.None, TextureSizing.Changed(s, s));
    }

    [Fact]
    public void Changed_UiScaleRebuildsEverything()
    {
        Assert.Equal(SizedGroups.All, TextureSizing.Changed(TextureSizing.Create(56f, 1, 1f), TextureSizing.Create(56f, 1, 1.5f)));
    }

    [Fact]
    public void Changed_RingCountRebuildsOnlyTheRim()
    {
        Assert.Equal(SizedGroups.Rim, TextureSizing.Changed(TextureSizing.Create(56f, 1, 1f), TextureSizing.Create(56f, 3, 1f)));
    }

    [Fact]
    public void Changed_ButtonSize_KeepsHubRingsWhileEStaysClamped()
    {
        // e = clamp(round(0.36 B), 18, 28) is 18 for B = 36 and 40.
        Assert.Equal(
            SizedGroups.SlotRings | SizedGroups.Glyphs | SizedGroups.Rim,
            TextureSizing.Changed(TextureSizing.Create(36f, 1, 1f), TextureSizing.Create(40f, 1, 1f)));
        Assert.Equal(SizedGroups.All, TextureSizing.Changed(TextureSizing.Create(56f, 1, 1f), TextureSizing.Create(60f, 1, 1f)));
    }

    [Theory]
    [InlineData(MaskId.SubMenuBadge, 0.22f)]
    [InlineData(MaskId.Caret, 0.2f)]
    [InlineData(MaskId.Lock, 0.25f)]
    [InlineData(MaskId.IconClose, 0.22f)]
    [InlineData(MaskId.IconDone, 0.22f)]
    public void GlyphDrawSize_BadgesAreFractionsOfB(MaskId id, float fraction)
    {
        Assert.Equal(fraction * 56f, TextureSizing.Create(56f, 1, 1f).GlyphDrawSize(id), 4);
    }

    [Theory]
    [InlineData(MaskId.IconEdit)]
    [InlineData(MaskId.IconAdd)]
    public void GlyphDrawSize_HubSubIconsAreFractionsOfE(MaskId id)
    {
        TextureSizing s = TextureSizing.Create(56f, 1, 1f);

        Assert.Equal(0.6f * s.HubSubDiameter, s.GlyphDrawSize(id), 4);
    }

    [Fact]
    public void GlyphTexels_AreTheDrawnSizeInScreenPixels_AtLeastMinSize()
    {
        Assert.Equal(14, TextureSizing.Create(56f, 1, 1f).GlyphTexels(MaskId.Lock));
        Assert.Equal(21, TextureSizing.Create(56f, 1, 1.5f).GlyphTexels(MaskId.Lock));
        Assert.Equal(Glyphs.MinSize, TextureSizing.Create(36f, 1, 1f).GlyphTexels(MaskId.Caret)); // 7.2 px → 8
        for (MaskId id = MaskId.SubMenuBadge; id <= MaskId.IconAdd; id++)
        {
            Assert.InRange(TextureSizing.Create(36f, 1, 1f).GlyphTexels(id), Glyphs.MinSize, 64);
        }
    }

    [Theory]
    [InlineData(MaskId.Circle)]
    [InlineData(MaskId.Plate)]
    public void GlyphDrawSize_RejectsNonGlyphs(MaskId id)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TextureSizing.Create(56f, 1, 1f).GlyphDrawSize(id));
    }
}
