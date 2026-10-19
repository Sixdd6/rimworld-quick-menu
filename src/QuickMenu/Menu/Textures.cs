using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using QuickMenu.Core.Render;
using QuickMenu.Diagnostics;
using UnityEngine;
using Verse;
using Log = QuickMenu.Diagnostics.Log;

namespace QuickMenu.Menu;

/// <summary>A texture drawn centred on a point in a square of <see cref="DrawSize"/> UI px. A value type: drawing allocates nothing.</summary>
internal readonly struct SizedTexture
{
    internal SizedTexture(Texture2D? texture, float drawSize)
    {
        Texture = texture;
        DrawSize = drawSize;
    }

    internal Texture2D? Texture { get; }

    /// <summary>Side of the draw rect in UI px. For round masks it includes the edge padding (<see cref="RoundTextureGeometry"/>).</summary>
    internal float DrawSize { get; }

    internal bool IsValid => Texture != null;

    internal Rect RectAt(float centreX, float centreY) =>
        new(centreX - (DrawSize / 2f), centreY - (DrawSize / 2f), DrawSize, DrawSize);

    /// <summary>Draws under the current <c>GUI.color</c>; does nothing when the texture is missing.</summary>
    internal void Draw(float centreX, float centreY)
    {
        if (Texture != null)
        {
            GUI.DrawTexture(RectAt(centreX, centreY), Texture);
        }
    }
}

/// <summary>
/// Runtime textures of the menu (design.md §6, ADR 0007 spike S-7: runtime RGBA32 masks, bilinear, no mipmaps, clamp).
/// </summary>
/// <remarks>
/// <para><b>Static masks</b> (every <see cref="MaskSet"/> mask at its authored size) are uploaded once at startup.</para>
/// <para><b>Sized textures</b> are rasterized at their drawn size in screen pixels so the widths of design.md §2 stay
/// exact: slot rings (1, 2, 3 px, dashed) at B, hub sub-button rings at e, the disc rim (2 px accent + 1 px outlines,
/// colours baked in) at the disc diameter, and the badge and hub-icon glyphs at their badge sizes (a 64 px glyph
/// minified 4–8× without mipmaps would alias). <see cref="EnsureSized"/> runs on each open and rebuilds only the
/// groups whose size changed (<see cref="TextureSizing.Changed"/>): button size, rings in use or UI scale. Old textures
/// are destroyed. Nothing here runs per frame.</para>
/// <para>Masks are top row first; <c>SetPixels32</c> takes the bottom row first, so rows are flipped in Core.</para>
/// </remarks>
[StaticConstructorOnStartup]
internal static class Textures
{
    internal static readonly PatchSite Site = new("Textures");

    private static readonly Texture2D?[] _masks = new Texture2D?[MaskSet.Count];
    private static readonly SizedTexture[] _glyphs = new SizedTexture[MaskSet.Count];
    private static byte[] _scratch = new byte[MaskSet.MaxByteCount];
    private static TextureSizing? _sizing;
    private static SizedTexture _slotRing1;
    private static SizedTexture _slotRing2;
    private static SizedTexture _slotRing3;
    private static SizedTexture _slotRingDashed;
    private static SizedTexture _hubRing1;
    private static SizedTexture _hubRing2;
    private static SizedTexture _discRim;

    static Textures()
    {
        try
        {
            long start = Stopwatch.GetTimestamp();
            for (int i = 0; i < MaskSet.Count; i++)
            {
                MaskId id = (MaskId)i;
                MaskSpec spec = MaskSet.Spec(id);
                MaskSet.Render(id, _scratch);
                _masks[i] = UploadMask(spec.Width, spec.Height, spec.Name);
            }

            if (Prefs.DevMode)
            {
                Log.Info($"Textures: {MaskSet.Count} masks uploaded in {Stopwatch.GetElapsedTime(start).TotalMilliseconds:F2} ms");
            }
        }
        catch (Exception ex)
        {
            Site.Fail(ex);
        }
    }

    /// <summary>False when building a texture failed; drawing code then skips the shapes that need them.</summary>
    internal static bool Ready => !Site.Disabled;

    /// <summary>A static mask at its authored size, or the white texture when it could not be built.</summary>
    internal static Texture2D Mask(MaskId id) => _masks[(int)id] ?? BaseContent.WhiteTex;

    /// <summary>Slot rings at the slot diameter B (1, 2, 3 px and dashed 1 px).</summary>
    internal static SizedTexture SlotRing1 => _slotRing1;

    /// <inheritdoc cref="SlotRing1"/>
    internal static SizedTexture SlotRing2 => _slotRing2;

    /// <inheritdoc cref="SlotRing1"/>
    internal static SizedTexture SlotRing3 => _slotRing3;

    /// <inheritdoc cref="SlotRing1"/>
    internal static SizedTexture SlotRingDashed => _slotRingDashed;

    /// <summary>Hub sub-button rings at its diameter e (1 and 2 px).</summary>
    internal static SizedTexture HubRing1 => _hubRing1;

    /// <inheritdoc cref="HubRing1"/>
    internal static SizedTexture HubRing2 => _hubRing2;

    /// <summary>The disc rim with baked colours; draw it with <see cref="QMStyle.White"/> (α scaled when dimmed).</summary>
    internal static SizedTexture DiscRim => _discRim;

    /// <summary>A badge or hub-icon glyph at its drawn size (design.md §2 badges, S4a).</summary>
    internal static SizedTexture Glyph(MaskId id) => _glyphs[(int)id];

    /// <summary>
    /// Makes the sized textures match the button size, the rings in use and the current UI scale. Call once per open
    /// (or when the layout cache is rebuilt), never per frame. Rebuilds only what changed.
    /// </summary>
    internal static void EnsureSized(float buttonSize, int ringsInUse)
    {
        if (Site.Disabled)
        {
            return;
        }

        try
        {
            TextureSizing next = TextureSizing.Create(buttonSize, ringsInUse, Prefs.UIScale);
            SizedGroups groups = TextureSizing.Changed(_sizing, next);
            if (groups == SizedGroups.None)
            {
                return;
            }

            long start = Stopwatch.GetTimestamp();
            if ((groups & SizedGroups.SlotRings) != 0)
            {
                RoundTextureGeometry g = next.SlotRing;
                _slotRing1 = Replace(_slotRing1, BuildRing(g, TextureSizing.Ring1Width, 0, "SlotRing1"));
                _slotRing2 = Replace(_slotRing2, BuildRing(g, TextureSizing.Ring2Width, 0, "SlotRing2"));
                _slotRing3 = Replace(_slotRing3, BuildRing(g, TextureSizing.Ring3Width, 0, "SlotRing3"));
                _slotRingDashed = Replace(_slotRingDashed, BuildRing(g, TextureSizing.Ring1Width, MaskSet.DashCount, "SlotRingDashed"));
            }

            if ((groups & SizedGroups.HubRings) != 0)
            {
                RoundTextureGeometry g = next.HubRing;
                _hubRing1 = Replace(_hubRing1, BuildRing(g, TextureSizing.Ring1Width, 0, "HubRing1"));
                _hubRing2 = Replace(_hubRing2, BuildRing(g, TextureSizing.Ring2Width, 0, "HubRing2"));
            }

            if ((groups & SizedGroups.Rim) != 0)
            {
                _discRim = Replace(_discRim, BuildRim(next.Rim));
            }

            if ((groups & SizedGroups.Glyphs) != 0)
            {
                for (MaskId id = MaskId.SubMenuBadge; id <= MaskId.IconAdd; id++)
                {
                    int n = next.GlyphTexels(id);
                    Glyphs.Render(id, n, Scratch(n * n));
                    _glyphs[(int)id] = Replace(_glyphs[(int)id], new SizedTexture(UploadMask(n, n, MaskSet.Spec(id).Name), next.GlyphDrawSize(id)));
                }
            }

            _sizing = next;
            if (Prefs.DevMode)
            {
                Log.Info($"Textures rebuilt ({groups}) B={buttonSize} rings={ringsInUse} UI scale={next.UiScale} rim={next.Rim.TextureSize}px in {Stopwatch.GetElapsedTime(start).TotalMilliseconds:F2} ms");
            }
        }
        catch (Exception ex)
        {
            Site.Fail(ex);
        }
    }

    private static SizedTexture BuildRing(RoundTextureGeometry g, float widthUi, int dashes, string name)
    {
        int n = g.TextureSize;
        Span<byte> mask = Scratch(n * n);
        if (dashes > 0)
        {
            MaskRasterizer.DashedRing(n, g.Texels(widthUi), dashes, MaskSet.DashDuty, mask);
        }
        else
        {
            MaskRasterizer.Ring(n, g.Texels(widthUi), mask);
        }

        return new SizedTexture(UploadMask(n, n, name), g.DrawSize);
    }

    private static SizedTexture BuildRim(RoundTextureGeometry g)
    {
        int n = g.TextureSize;
        var pixels = new Color32[n * n];
        Core.Render.DiscRim.Render(
            n,
            g.Texels(Core.Render.DiscRim.RimWidth),
            g.Texels(Core.Render.DiscRim.OutlineWidth),
            StyleTokens.Accent,
            StyleTokens.RimOutline,
            MemoryMarshal.AsBytes(pixels.AsSpan()));
        return new SizedTexture(Upload(pixels, n, n, "DiscRim"), g.DrawSize);
    }

    /// <summary>Uploads the first <c>width × height</c> bytes of the scratch buffer as a white mask.</summary>
    private static Texture2D UploadMask(int width, int height, string name)
    {
        var pixels = new Color32[width * height];
        TexturePixels.MaskToRgbaBottomUp(_scratch, width, height, MemoryMarshal.AsBytes(pixels.AsSpan()));
        return Upload(pixels, width, height, name);
    }

    private static Texture2D Upload(Color32[] pixels, int width, int height, string name)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "QuickMenu." + name,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };
        texture.SetPixels32(pixels);
        texture.Apply(false);
        return texture;
    }

    private static Span<byte> Scratch(int bytes)
    {
        if (_scratch.Length < bytes)
        {
            _scratch = new byte[bytes];
        }

        return _scratch.AsSpan(0, bytes);
    }

    private static SizedTexture Replace(SizedTexture old, SizedTexture next)
    {
        if (old.Texture != null)
        {
            UnityEngine.Object.Destroy(old.Texture);
        }

        return next;
    }
}
