using System;
using System.Diagnostics;
using QuickMenu.Core.Render;
using Xunit;

namespace QuickMenu.Core.Tests.Perf;

/// <summary>
/// T-28: a size-dependent texture rebuild (rim, slot and hub rings, glyphs) stays within 5 ms; gated at 5 x the
/// budget like the other timing gates (performance.md section 4), because the Perf classes run in parallel. Measured here is the
/// Core rasterization only, for the largest rim the adapter allows (2048 px) plus the small masks; the upload is
/// measured in game (dev-mode log line "Textures rebuilt …").
/// </summary>
[Trait("Category", "Perf")]
public class TextureRebuildPerfTests
{
    private const double BudgetMilliseconds = 5.0;
    private const double GateFactor = 5.0;

    [Fact]
    public void Worst_case_sized_rebuild_rasterizes_within_budget()
    {
        TextureSizing sizing = TextureSizing.Create(96f, 5, 2f);
        RoundTextureGeometry rim = sizing.Rim;
        RoundTextureGeometry slot = sizing.SlotRing;
        RoundTextureGeometry hub = sizing.HubRing;
        Assert.Equal(RoundTextureGeometry.MaxTextureSize, rim.TextureSize);
        byte[] rimBuffer = new byte[DiscRim.ByteCount(rim.TextureSize)];
        byte[] mask = new byte[slot.TextureSize * slot.TextureSize];
        byte[] rgba = new byte[mask.Length * 4];

        void Once()
        {
            DiscRim.Render(rim.TextureSize, rim.Texels(DiscRim.RimWidth), rim.Texels(DiscRim.OutlineWidth), StyleTokens.Accent, StyleTokens.RimOutline, rimBuffer);
            foreach (float w in new[] { TextureSizing.Ring1Width, TextureSizing.Ring2Width, TextureSizing.Ring3Width })
            {
                MaskRasterizer.Ring(slot.TextureSize, slot.Texels(w), mask);
                TexturePixels.MaskToRgbaBottomUp(mask, slot.TextureSize, slot.TextureSize, rgba);
            }

            MaskRasterizer.DashedRing(slot.TextureSize, slot.Texels(TextureSizing.Ring1Width), MaskSet.DashCount, MaskSet.DashDuty, mask);
            MaskRasterizer.Ring(hub.TextureSize, hub.Texels(TextureSizing.Ring1Width), mask);
            MaskRasterizer.Ring(hub.TextureSize, hub.Texels(TextureSizing.Ring2Width), mask);
            for (MaskId id = MaskId.SubMenuBadge; id <= MaskId.IconAdd; id++)
            {
                Glyphs.Render(id, sizing.GlyphTexels(id), mask);
            }
        }

        Once();
        double[] ms = new double[5];
        for (int run = 0; run < ms.Length; run++)
        {
            long start = Stopwatch.GetTimestamp();
            Once();
            ms[run] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }

        Array.Sort(ms);
        Assert.True(ms[2] <= BudgetMilliseconds * GateFactor, $"median {ms[2]:F2} ms per worst-case sized rebuild, limit {BudgetMilliseconds * GateFactor} ms");
    }
}
