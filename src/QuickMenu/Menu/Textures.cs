using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using QuickMenu.Core.Render;
using QuickMenu.Core.Settings;
using QuickMenu.Diagnostics;
using UnityEngine;
using Verse;
using FaultPoint = QuickMenu.Core.Diagnostics.FaultPoint;
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
/// exact: slot rings (1, 2, 3 px, dashed) at B, hub sub-button rings at e, the main disc rim (2 px accent + 1 px
/// outlines, colours baked in) at the disc diameter, and the badge and hub-icon glyphs at their badge sizes (a 64 px
/// glyph minified 4–8× without mipmaps would alias). <see cref="EnsureSized"/> runs on each open and rebuilds only the
/// groups whose size changed (<see cref="TextureSizing.Changed"/>): button size, rings in use or UI scale.</para>
/// <para><b>Satellite rim</b>: satellites have their own disc (<c>D_s</c>, design.md §1.1). The rim cache holds the
/// main rim plus one satellite rim (<see cref="EnsureSatelliteRim"/>, policy in
/// <see cref="TextureSizing.ChooseSatelliteRim"/>), so opening satellites never rebuilds the main rim.</para>
/// <para>Old textures are destroyed. Nothing here runs per frame. Uploads go through one reusable RGBA buffer
/// (<c>SetPixelData</c>) and are made non-readable (<c>Apply(false, true)</c>) so no CPU copy is kept.</para>
/// <para>Masks are top row first; Unity pixel data is bottom row first, so rows are flipped in Core.</para>
/// <para>Two fault sites: <see cref="Site"/> for the static masks and <see cref="SizedSite"/> for the size-dependent
/// textures, so a rim or rebuild failure leaves the static masks working.</para>
/// <para><b>Pin markers</b> (design.md §2 "Architect pin marker", T-89): the <see cref="MaskId.PinMarker"/> fill and its
/// 1 UI px outline for each <see cref="PinMarkerKind"/>, rasterized at their drawn size in screen px. They depend only on
/// the UI scale and are drawn on the game's architect buttons, so they do not wait for a menu open: built at startup
/// (<see cref="Prewarm"/>), checked by <see cref="EnsurePinMarkers"/> (a float compare; the marker hook calls it once per
/// frame) and rebuilt only when the UI scale changed. Their own fault site <see cref="PinMarkerSite"/> keeps a marker
/// failure away from the menu textures and the other way round.</para>
/// </remarks>
[StaticConstructorOnStartup]
internal static class Textures
{
    /// <summary>Fault site of the static masks.</summary>
    internal static readonly PatchSite Site = new("Textures");

    /// <summary>Fault site of the size-dependent textures (rings, rims, glyphs).</summary>
    internal static readonly PatchSite SizedSite = new("Textures.Sized");

    /// <summary>Fault site of the pin-marker textures (T-89).</summary>
    internal static readonly PatchSite PinMarkerSite = new("Textures.PinMarker");

    /// <summary>The RGBA buffer is kept between rebuilds up to this size (a rim of about 1000 px); larger is released.</summary>
    private const int RetainedRgbaBytes = 4 << 20;

    private const int GlyphCount = Glyphs.MenuGlyphCount;

    // Fill and outline per pin-marker kind.
    private const int PinMarkerTextureCount = 2 * PinMarkerSizing.KindCount;

    // Slot rings 1/2/3/dashed, hub rings 1/2/3, glyphs, material dot, main rim.
    private const int SizedCount = 4 + 3 + GlyphCount + 1 + 1;

    private static readonly Texture2D?[] _masks = new Texture2D?[MaskSet.Count];
    private static readonly SizedTexture[] _glyphs = new SizedTexture[MaskSet.Count];
    private static byte[] _scratch = new byte[MaskSet.MaxByteCount];
    private static byte[] _rgba = new byte[MaskSet.MaxByteCount * 4];
    private static TextureSizing? _sizing;

    /// <summary>What the sized textures are built for, null before the first build (the settings preview stretches them to the slider value meanwhile).</summary>
    internal static TextureSizing? BuiltSizing => _sizing;

    /// <summary>Sized-texture rebuilds this session (T-95: at most one per slider drag), for the perf report.</summary>
    internal static int SizedRebuilds { get; private set; }

    /// <summary>Milliseconds the latest sized-texture rebuild took, with upload.</summary>
    internal static double LastSizedRebuildMs { get; private set; }
    private static int _live;
    private static long _uploadTicks;
    private static SizedTexture _slotRing1;
    private static SizedTexture _slotRing2;
    private static SizedTexture _slotRing3;
    private static SizedTexture _slotRingDashed;
    private static SizedTexture _hubRing1;
    private static SizedTexture _hubRing2;
    private static SizedTexture _hubRing3;
    private static SizedTexture _discRim;
    private static SizedTexture _materialDot;
    private static SizedTexture _satelliteRim;
    private static RoundTextureGeometry? _satelliteRimGeometry;
    private static readonly Texture2D?[] _pinFill = new Texture2D?[PinMarkerSizing.KindCount];
    private static readonly Texture2D?[] _pinOutline = new Texture2D?[PinMarkerSizing.KindCount];
    private static PinMarkerSizing? _pinSizing;

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

        Prewarm();
    }

    /// <summary>False when building the static masks failed; drawing code then skips the shapes that need them.</summary>
    internal static bool Ready => !Site.Disabled;

    /// <summary>
    /// False when building a size-dependent texture failed. The sized textures are then invalid and
    /// <see cref="SizedTexture.Draw"/> draws nothing; the static masks still work.
    /// </summary>
    internal static bool SizedReady => !SizedSite.Disabled;

    /// <summary>Textures created by this class and not yet destroyed (dev self-test).</summary>
    internal static int LiveCount => _live;

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

    /// <summary>Hub ring, 3 px: hover and pressed in edit mode.</summary>
    internal static SizedTexture HubRing3 => _hubRing3;

    /// <summary>
    /// The main disc rim with baked colours; draw it with <see cref="QMStyle.White"/> (α scaled when dimmed). For a
    /// satellite use <see cref="EnsureSatelliteRim"/>.
    /// </summary>
    internal static SizedTexture DiscRim => _discRim;

    /// <summary>The material dot (S4 state 13): a filled circle of 0.18 B rasterized at its drawn size, tinted at draw time.</summary>
    internal static SizedTexture MaterialDot => _materialDot;

    /// <summary>A badge or hub-icon glyph at its drawn size (design.md §2 badges, S4a).</summary>
    internal static SizedTexture Glyph(MaskId id) => _glyphs[(int)id];

    /// <summary>What the pin markers are built for (UI scale), or null before the first build or after a failure.</summary>
    internal static PinMarkerSizing? BuiltPinMarkerSizing => PinMarkerSite.Disabled ? null : _pinSizing;

    /// <summary>Pin-marker rebuilds this session (one at startup, then one per UI-scale change), for the perf report.</summary>
    internal static int PinMarkerRebuilds { get; private set; }

    /// <summary>The pin-marker fill mask of <paramref name="kind"/> (tint <see cref="QMStyle.PinMarker"/> or <see cref="QMStyle.PinMarkerPartial"/>), or null when not built.</summary>
    internal static Texture2D? PinMarkerFill(PinMarkerKind kind) => _pinFill[(int)kind];

    /// <summary>The pin-marker outline mask of <paramref name="kind"/> (tint <see cref="QMStyle.PinMarkerOutline"/>), or null when not built.</summary>
    internal static Texture2D? PinMarkerOutline(PinMarkerKind kind) => _pinOutline[(int)kind];

    /// <summary>
    /// Makes the pin-marker textures match the current UI scale and returns whether they can be drawn. Cheap when nothing
    /// changed (one float compare, no allocation), so the marker hook may call it once per frame; it rebuilds (6 small
    /// textures) only after a UI-scale change. Never throws: a failure disables <see cref="PinMarkerSite"/> and returns false.
    /// </summary>
    internal static bool EnsurePinMarkers()
    {
        if (PinMarkerSite.Disabled)
        {
            return false;
        }

        try
        {
            float scale = Prefs.UIScale;
            if (_pinSizing is { } built && built.UiScale == scale)
            {
                return true;
            }

            PinMarkerSizing next = PinMarkerSizing.Create(scale);
            long start = Stopwatch.GetTimestamp();
            for (int i = 0; i < PinMarkerSizing.KindCount; i++)
            {
                var kind = (PinMarkerKind)i;
                int n = next.Texels(kind);
                Glyphs.Render(MaskId.PinMarker, n, Scratch(n * n));
                _pinFill[i] = ReplaceTexture(_pinFill[i], UploadMask(n, n, "PinMarker" + kind));
                int o = next.OutlineTexels(kind);
                Glyphs.RenderOutline(MaskId.PinMarker, n, next.OutlinePad, next.OutlineGrow, Scratch(o * o));
                _pinOutline[i] = ReplaceTexture(_pinOutline[i], UploadMask(o, o, "PinMarkerOutline" + kind));
            }

            _pinSizing = next;
            PinMarkerRebuilds++;
            if (Prefs.DevMode)
            {
                Log.Info($"Pin markers rebuilt UI scale={scale} full={next.Texels(PinMarkerKind.Full)}px in {Stopwatch.GetElapsedTime(start).TotalMilliseconds:F2} ms (with upload)");
            }

            return true;
        }
        catch (Exception ex)
        {
            PinMarkerSite.Fail(ex);
            return false;
        }
    }

    /// <summary>
    /// Draws a pin marker whose fill sits at <paramref name="fill"/> (from <see cref="PinMarkerSizing.Button"/>,
    /// <see cref="PinMarkerSizing.ListRow"/>, <see cref="PinMarkerSizing.ListRowNoIcon"/> or
    /// <see cref="PinMarkerSizing.GridCell"/> of <see cref="BuiltPinMarkerSizing"/>): the outline mask tinted
    /// <see cref="QMStyle.PinMarkerOutline"/>, then the fill tinted <see cref="QMStyle.PinMarker"/> (or
    /// <see cref="QMStyle.PinMarkerPartial"/>). Restores <c>GUI.color</c>. Repaint only; allocates nothing; does nothing
    /// when the textures are missing. Call <see cref="EnsurePinMarkers"/> first in the same frame.
    /// </summary>
    internal static void DrawPinMarker(MarkerRect fill, PinMarkerKind kind)
    {
        if (_pinSizing is not { } sizing || PinMarkerSite.Disabled)
        {
            return;
        }

        Texture2D? fillTex = _pinFill[(int)kind];
        Texture2D? outlineTex = _pinOutline[(int)kind];
        if (fillTex == null || outlineTex == null)
        {
            return;
        }

        MarkerRect o = sizing.OutlineRect(fill, kind);
        Color saved = GUI.color;
        GUI.color = QMStyle.PinMarkerOutline;
        GUI.DrawTexture(new Rect(o.X, o.Y, o.Size, o.Size), outlineTex);
        GUI.color = kind == PinMarkerKind.Partial ? QMStyle.PinMarkerPartial : QMStyle.PinMarker;
        GUI.DrawTexture(new Rect(fill.X, fill.Y, fill.Size, fill.Size), fillTex);
        GUI.color = saved;
    }

    /// <summary>
    /// Builds the sized textures for the sizes the saved settings will open with, during the loading screen, so the first
    /// open pays nothing (UC-2: 6.46 ms on the first open, over the 5 ms budget). A wrong guess (Auto sizing, a grid
    /// layout, another UI scale later) only costs the rebuild it would have cost anyway. Never throws.
    /// </summary>
    private static void Prewarm()
    {
        // The pin markers are drawn in the architect, before any menu open: build them whatever the layout.
        EnsurePinMarkers();
        try
        {
            QuickMenu.Settings.QuickMenuSettings settings = QuickMenuMod.Settings;
            if (settings.Layout != MenuLayout.Wheel)
            {
                return;
            }

            EnsureSized(
                SettingsRules.ClampButtonSize(settings.ButtonSize),
                SettingsRules.ClampRings(settings.Rings));
        }
        catch (Exception ex)
        {
            SizedSite.Fail(ex);
        }
    }

    /// <summary>
    /// Makes the sized textures match the button size, the rings in use and the current UI scale. Call once per open
    /// (or when the layout cache is rebuilt), never per frame. Rebuilds only what changed.
    /// </summary>
    /// <param name="buttonSize">B in UI px.</param>
    /// <param name="ringsInUse">Rings the main disc covers, 0…<see cref="TextureSizing.MaxRingsInUse"/> (Auto grows to 8).</param>
    internal static void EnsureSized(float buttonSize, int ringsInUse)
    {
        if (SizedSite.Disabled)
        {
            return;
        }

        try
        {
            FaultInjection.Check(FaultPoint.TexturesSized);
            TextureSizing next = TextureSizing.Create(buttonSize, ringsInUse, Prefs.UIScale);
            SizedGroups groups = TextureSizing.Changed(_sizing, next);
            if (groups == SizedGroups.None)
            {
                return;
            }

            long start = Stopwatch.GetTimestamp();
            _uploadTicks = 0;
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
                _hubRing3 = Replace(_hubRing3, BuildRing(g, TextureSizing.Ring3Width, 0, "HubRing3"));
            }

            if ((groups & SizedGroups.Rim) != 0)
            {
                _discRim = Replace(_discRim, BuildRim(next.Rim, "DiscRim"));
            }

            if ((groups & SizedGroups.Glyphs) != 0)
            {
                for (MaskId id = Glyphs.First; id <= Glyphs.LastMenuGlyph; id++)
                {
                    int n = next.GlyphTexels(id);
                    Glyphs.Render(id, n, Scratch(n * n));
                    _glyphs[(int)id] = Replace(_glyphs[(int)id], new SizedTexture(UploadMask(n, n, MaskSet.Spec(id).Name), next.GlyphDrawSize(id)));
                }

                RoundTextureGeometry dot = next.MaterialDot;
                MaskRasterizer.Circle(dot.TextureSize, Scratch(dot.TextureSize * dot.TextureSize));
                _materialDot = Replace(_materialDot, new SizedTexture(UploadMask(dot.TextureSize, dot.TextureSize, "MaterialDot"), dot.DrawSize));
            }

            if ((groups & SizedGroups.Glyphs) != 0)
            {
                // A UI-scale change seen at a menu open also refreshes the markers (normally the hook already did).
                EnsurePinMarkers();
            }

            _sizing = next;
            ReleaseLargeBuffer();
            SizedRebuilds++;
            LastSizedRebuildMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            if (Prefs.DevMode)
            {
                Log.Info($"Textures rebuilt ({groups}) B={buttonSize} rings={ringsInUse} UI scale={next.UiScale} rim={next.Rim.TextureSize}px in {LastSizedRebuildMs:F2} ms (with upload; texture create + SetPixelData + Apply = {_uploadTicks * 1000.0 / Stopwatch.Frequency:F2} ms)");
            }
        }
        catch (Exception ex)
        {
            SizedSite.Fail(ex);
        }
    }

    /// <summary>
    /// The rim of a satellite (sub-menu or material ring) with <paramref name="satelliteRings"/> rings, at the current
    /// button size and UI scale (disc <c>D_s</c>, design.md §1.1). Call once when the satellite opens, after
    /// <see cref="EnsureSized"/> for the open menu, and keep the result for the satellite's lifetime; never per frame.
    /// Returns the main rim when the discs match, the cached satellite rim when it has this size, and otherwise
    /// rebuilds the one satellite slot. The returned texture stays valid until the next call of this method or of
    /// <see cref="EnsureSized"/>. Invalid (draws nothing) before <see cref="EnsureSized"/> or after a failure.
    /// </summary>
    internal static SizedTexture EnsureSatelliteRim(int satelliteRings)
    {
        if (SizedSite.Disabled || _sizing is not { } sizing)
        {
            return default;
        }

        try
        {
            RoundTextureGeometry wanted = sizing.SatelliteRim(satelliteRings);
            switch (TextureSizing.ChooseSatelliteRim(sizing.Rim, _satelliteRimGeometry, wanted))
            {
                case SatelliteRimSource.Main:
                    return _discRim;
                case SatelliteRimSource.Cached:
                    return _satelliteRim;
            }

            long start = Stopwatch.GetTimestamp();
            _satelliteRim = Replace(_satelliteRim, BuildRim(wanted, "SatelliteRim"));
            _satelliteRimGeometry = wanted;
            ReleaseLargeBuffer();
            if (Prefs.DevMode)
            {
                Log.Info($"Satellite rim rebuilt rings={satelliteRings} rim={wanted.TextureSize}px in {Stopwatch.GetElapsedTime(start).TotalMilliseconds:F2} ms (with upload)");
            }

            return _satelliteRim;
        }
        catch (Exception ex)
        {
            SizedSite.Fail(ex);
            return default;
        }
    }

    /// <summary>
    /// Dev self-test section: live texture count against the expected count, the <c>Color32</c> layout, and a
    /// 1×2 pixel round trip through the same upload call the masks use.
    /// </summary>
    internal static void AppendSelfTest(StringBuilder sb)
    {
        int expected = (Site.Disabled ? 0 : MaskSet.Count)
            + (_sizing is null ? 0 : SizedCount)
            + (_satelliteRimGeometry is null ? 0 : 1)
            + (_pinSizing is null ? 0 : PinMarkerTextureCount);
        sb.AppendLine("live=" + _live.ToString(CultureInfo.InvariantCulture)
            + " expected=" + expected.ToString(CultureInfo.InvariantCulture)
            + " (static " + MaskSet.Count.ToString(CultureInfo.InvariantCulture)
            + ", sized " + SizedCount.ToString(CultureInfo.InvariantCulture) + " once a menu was opened, +1 satellite rim"
            + ", pin markers " + PinMarkerTextureCount.ToString(CultureInfo.InvariantCulture) + ")"
            + " staticDisabled=" + Site.Disabled + " sizedDisabled=" + SizedSite.Disabled + " pinMarkerDisabled=" + PinMarkerSite.Disabled);
        if (_pinSizing is { } pin)
        {
            sb.AppendLine("pinMarkers uiScale=" + pin.UiScale.ToString(CultureInfo.InvariantCulture)
                + " full=" + pin.Texels(PinMarkerKind.Full).ToString(CultureInfo.InvariantCulture) + "px"
                + " partial=" + pin.Texels(PinMarkerKind.Partial).ToString(CultureInfo.InvariantCulture) + "px"
                + " entry=" + pin.Texels(PinMarkerKind.Entry).ToString(CultureInfo.InvariantCulture) + "px"
                + " outlinePad=" + pin.OutlinePad.ToString(CultureInfo.InvariantCulture) + "px"
                + " rebuilds=" + PinMarkerRebuilds.ToString(CultureInfo.InvariantCulture));
        }
        if (_sizing is { } s)
        {
            sb.AppendLine("sizing B=" + s.ButtonSize.ToString(CultureInfo.InvariantCulture)
                + " rings=" + s.RingsInUse.ToString(CultureInfo.InvariantCulture)
                + " uiScale=" + s.UiScale.ToString(CultureInfo.InvariantCulture)
                + " rim=" + s.Rim.TextureSize.ToString(CultureInfo.InvariantCulture) + "px"
                + " satelliteRim=" + (_satelliteRimGeometry is { } g ? g.TextureSize.ToString(CultureInfo.InvariantCulture) + "px" : "none"));
        }

        var probe = new Color32(1, 2, 3, 4);
        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(new ReadOnlySpan<Color32>(in probe));
        bool layoutOk = Unsafe.SizeOf<Color32>() == 4 && bytes[0] == 1 && bytes[1] == 2 && bytes[2] == 3 && bytes[3] == 4;
        sb.AppendLine("Color32 size=" + Unsafe.SizeOf<Color32>().ToString(CultureInfo.InvariantCulture) + " rgbaOrder=" + layoutOk + (layoutOk ? " ok" : " FAIL"));

        // Bottom row (10, 20, 30, 40), top row (50, 60, 70, 80): Unity order, as the uploads write it.
        Texture2D? texture = null;
        try
        {
            texture = new Texture2D(1, 2, TextureFormat.RGBA32, false) { name = "QuickMenu.SelfTestProbe" }; // strings-ok: object name, not a Keyed key
            byte[] data = [10, 20, 30, 40, 50, 60, 70, 80];
            texture.SetPixelData(data, 0, 0);
            texture.Apply(false, false);
            Color32[] back = texture.GetPixels32();
            bool roundTrip = back.Length == 2
                && back[0].r == 10 && back[0].g == 20 && back[0].b == 30 && back[0].a == 40
                && back[1].r == 50 && back[1].a == 80;
            sb.AppendLine("SetPixelData round trip (bottom row first, RGBA)=" + (roundTrip ? "ok" : "FAIL got " + Describe(back)));
        }
        catch (Exception ex)
        {
            sb.AppendLine("SetPixelData round trip threw: " + ex.GetType().Name + ": " + ex.Message);
        }
        finally
        {
            if (texture != null)
            {
                UnityEngine.Object.Destroy(texture);
            }
        }

        sb.AppendLine("UC-2 visual check: uploads are SetPixelData + Apply(false, true) (non-readable); confirm the rim, slot rings and the lock/caret glyphs draw, upright, in the open menu");
    }

    private static string Describe(Color32[] pixels)
    {
        var sb = new StringBuilder();
        foreach (Color32 c in pixels)
        {
            sb.Append('(').Append(c.r).Append(',').Append(c.g).Append(',').Append(c.b).Append(',').Append(c.a).Append(')');
        }

        return sb.ToString();
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

    private static SizedTexture BuildRim(RoundTextureGeometry g, string name)
    {
        int n = g.TextureSize;
        Core.Render.DiscRim.Render(
            n,
            g.Texels(Core.Render.DiscRim.RimWidth),
            g.Texels(Core.Render.DiscRim.OutlineWidth),
            StyleTokens.Accent,
            StyleTokens.RimOutline,
            Rgba(Core.Render.DiscRim.ByteCount(n)));
        return new SizedTexture(Upload(n, n, name), g.DrawSize);
    }

    /// <summary>Uploads the first <c>width × height</c> bytes of the mask scratch buffer as a white mask.</summary>
    private static Texture2D UploadMask(int width, int height, string name)
    {
        TexturePixels.MaskToRgbaBottomUp(_scratch, width, height, Rgba(width * height * 4));
        return Upload(width, height, name);
    }

    /// <summary>Uploads the first <c>width × height × 4</c> bytes of the RGBA buffer (bottom row first).</summary>
    private static Texture2D Upload(int width, int height, string name)
    {
        long uploadStart = Stopwatch.GetTimestamp();
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "QuickMenu." + name,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };
        _live++;

        // SetPixelData reads exactly one mip level from the start of the buffer, so a larger buffer is fine.
        texture.SetPixelData(_rgba, 0, 0);

        // No mipmaps; makeNoLongerReadable drops the CPU copy (unverified on Waylith: checked at UC-2).
        texture.Apply(false, true);
        _uploadTicks += Stopwatch.GetTimestamp() - uploadStart;
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

    private static Span<byte> Rgba(int bytes)
    {
        if (_rgba.Length < bytes)
        {
            _rgba = new byte[bytes];
        }

        return _rgba.AsSpan(0, bytes);
    }

    private static void ReleaseLargeBuffer()
    {
        if (_rgba.Length > RetainedRgbaBytes)
        {
            _rgba = new byte[MaskSet.MaxByteCount * 4];
        }
    }

    private static Texture2D ReplaceTexture(Texture2D? old, Texture2D next)
    {
        if (old != null && old != next)
        {
            UnityEngine.Object.Destroy(old);
            _live--;
        }

        return next;
    }

    private static SizedTexture Replace(SizedTexture old, SizedTexture next)
    {
        if (old.Texture != null && old.Texture != next.Texture)
        {
            UnityEngine.Object.Destroy(old.Texture);
            _live--;
        }

        return next;
    }
}
