using System;

namespace QuickMenu.Core.Render;

/// <summary>The shape masks, badges and hub icons of design.md §6, in generation order.</summary>
public enum MaskId
{
    /// <summary>Filled circle, 256 px: slot fill, hub.</summary>
    Circle,

    /// <summary>Filled disc with the radial gradient α 0.85 → 0.55 stored as grayscale (centre = 255), 256 px.</summary>
    Disc,

    /// <summary>Ring, 128 px, 2.3 px stroke (≈ 1 px at B = 56): normal and empty slot rings, disc outlines.</summary>
    Ring1,

    /// <summary>Ring, 128 px, 4.6 px stroke (≈ 2 px at B = 56): disc rim, hover, target, open parent.</summary>
    Ring2,

    /// <summary>Ring, 128 px, 6.9 px stroke (≈ 3 px at B = 56): insert cursor, last used.</summary>
    Ring3,

    /// <summary>Ring1 cut into 16 dashes at 50 % duty, first dash at 12 o'clock: empty and reserved slots.</summary>
    RingDashed,

    /// <summary>9-slice filled rounded rect, 64 px atlas, radius 10, slice border 16: grid panel fill.</summary>
    Panel,

    /// <summary>9-slice outline of <see cref="Panel"/>, 2 px stroke on the box edge: grid panel rim.</summary>
    PanelRim,

    /// <summary>9-slice filled rounded rect, 16 px atlas, radius 3, slice border 4: label, hint and help plates.</summary>
    Plate,

    /// <summary>Glyph, 64 px: right triangle filling the top-right corner; sub-menu badge at 1:30 (design.md S4 state 12).</summary>
    SubMenuBadge,

    /// <summary>Glyph, 64 px: triangle pointing right; insert-cursor caret at 10:30 (S4 state 10).</summary>
    Caret,

    /// <summary>Glyph, 64 px: padlock (body + shackle); greyed-slot badge at 7:30 (S4 state 5).</summary>
    Lock,

    /// <summary>Glyph, 64 px: ×, the hub's Close main area in use mode (S4a).</summary>
    IconClose,

    /// <summary>Glyph, 64 px: pencil, tip bottom-left, the hub's Edit sub-button (S4a).</summary>
    IconEdit,

    /// <summary>Glyph, 64 px: check mark, the hub's Done main area in edit mode (S4a).</summary>
    IconDone,

    /// <summary>Glyph, 64 px: plus, the hub's Add sub-button in edit mode (S4a) and the edit-mode empty slot (S4 state 7).</summary>
    IconAdd,
}

/// <summary>
/// Size and parameters of one mask. <see cref="SliceBorder"/> is 0 for masks drawn whole and
/// <c>Width / 4</c> for 9-slice masks, because <c>Widgets.DrawAtlas</c> slices the atlas at a quarter of its width.
/// </summary>
/// <param name="Id">Mask identifier.</param>
/// <param name="Name">Stable name, also the PNG file name of the fallback export (design.md §6, assets.md).</param>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
/// <param name="SliceBorder">9-slice border in pixels (texels = UI px when drawn by <c>DrawAtlas</c>), or 0.</param>
/// <param name="CornerRadius">Corner radius in pixels for rounded rects, else 0.</param>
/// <param name="Stroke">Stroke width in pixels for rings, outlines and glyph lines, else 0.</param>
/// <param name="DashCount">Number of dashes for dashed rings, else 0.</param>
/// <param name="DashDuty">Fraction of each dash period that is drawn, else 0.</param>
public readonly record struct MaskSpec(
    MaskId Id,
    string Name,
    int Width,
    int Height,
    int SliceBorder,
    float CornerRadius,
    float Stroke,
    int DashCount,
    float DashDuty)
{
    /// <summary>True when the mask is drawn with <c>Widgets.DrawAtlas</c>.</summary>
    public bool IsNineSlice => SliceBorder > 0;

    /// <summary>Bytes of alpha the mask needs (one per pixel).</summary>
    public int ByteCount => Width * Height;
}

/// <summary>
/// The full set of shape masks (design.md §2 components, §3, §6; spike S-7). Generation is pure and deterministic;
/// the adapter renders each mask once at startup into a reused buffer and uploads it to a <c>Texture2D</c>.
/// </summary>
/// <remarks>
/// <para>9-slice rule (spike S-7): <c>Widgets.DrawAtlas</c> uses <c>atlas.width / 4</c> as the border, so the
/// corner radius must be at most that. Atlas texels map 1:1 to UI pixels in the border, so the radius in texels is
/// the radius on screen.</para>
/// <para><see cref="MaskId.PanelRim"/> is a 2 px band on the box edge. The adapter draws the 1 px dark outlines
/// (design.md §2 <c>rimOutline</c>) with the same mask: tinted dark at the rect grown by 1 px and shrunk by 1 px,
/// then the accent at the rect itself on top.</para>
/// </remarks>
public static class MaskSet
{
    /// <summary>Size of the circle and disc masks.</summary>
    public const int RoundSize = 256;

    /// <summary>Size of the ring masks, authored for their typical drawn size (design.md §6).</summary>
    public const int RingSize = 128;

    /// <summary>Ring strokes at <see cref="RingSize"/>: ≈ 1 / 2 / 3 px at B = 56.</summary>
    public const float Ring1Stroke = 2.3f;

    /// <inheritdoc cref="Ring1Stroke"/>
    public const float Ring2Stroke = 4.6f;

    /// <inheritdoc cref="Ring1Stroke"/>
    public const float Ring3Stroke = 6.9f;

    /// <summary>Dashes on <see cref="MaskId.RingDashed"/> (≈ 3.5 px dash, 3.5 px gap at B = 36).</summary>
    public const int DashCount = 16;

    /// <summary>Drawn fraction of each dash period.</summary>
    public const float DashDuty = 0.5f;

    /// <summary>Panel atlas size; slice border = 16.</summary>
    public const int PanelAtlas = 64;

    /// <summary>Grid panel corner radius (design.md §2 Radius).</summary>
    public const float PanelRadius = 10f;

    /// <summary>Panel rim stroke (design.md §2 Ring widths: disc rim 2).</summary>
    public const float PanelRimStroke = 2f;

    /// <summary>Plate atlas size; slice border = 4.</summary>
    public const int PlateAtlas = 16;

    /// <summary>Plate corner radius (design.md §2 Radius).</summary>
    public const float PlateRadius = 3f;

    /// <summary>Glyph mask size (<see cref="Glyphs.GridSize"/>).</summary>
    public const int GlyphSize = Glyphs.GridSize;

    /// <summary>Number of masks in the set.</summary>
    public const int Count = 16;

    /// <summary>Largest <see cref="MaskSpec.ByteCount"/> in the set: one buffer of this size fits every mask.</summary>
    public const int MaxByteCount = RoundSize * RoundSize;

    private static readonly MaskSpec[] _specs =
    [
        new(MaskId.Circle, "Circle", RoundSize, RoundSize, 0, 0f, 0f, 0, 0f),
        new(MaskId.Disc, "Disc", RoundSize, RoundSize, 0, 0f, 0f, 0, 0f),
        new(MaskId.Ring1, "Ring1", RingSize, RingSize, 0, 0f, Ring1Stroke, 0, 0f),
        new(MaskId.Ring2, "Ring2", RingSize, RingSize, 0, 0f, Ring2Stroke, 0, 0f),
        new(MaskId.Ring3, "Ring3", RingSize, RingSize, 0, 0f, Ring3Stroke, 0, 0f),
        new(MaskId.RingDashed, "RingDashed", RingSize, RingSize, 0, 0f, Ring1Stroke, DashCount, DashDuty),
        new(MaskId.Panel, "Panel", PanelAtlas, PanelAtlas, PanelAtlas / 4, PanelRadius, 0f, 0, 0f),
        new(MaskId.PanelRim, "PanelRim", PanelAtlas, PanelAtlas, PanelAtlas / 4, PanelRadius, PanelRimStroke, 0, 0f),
        new(MaskId.Plate, "Plate", PlateAtlas, PlateAtlas, PlateAtlas / 4, PlateRadius, 0f, 0, 0f),
        new(MaskId.SubMenuBadge, "SubMenuBadge", GlyphSize, GlyphSize, 0, 0f, Glyphs.Stroke, 0, 0f),
        new(MaskId.Caret, "Caret", GlyphSize, GlyphSize, 0, 0f, Glyphs.Stroke, 0, 0f),
        new(MaskId.Lock, "Lock", GlyphSize, GlyphSize, 0, 0f, Glyphs.Stroke, 0, 0f),
        new(MaskId.IconClose, "IconClose", GlyphSize, GlyphSize, 0, 0f, Glyphs.Stroke, 0, 0f),
        new(MaskId.IconEdit, "IconEdit", GlyphSize, GlyphSize, 0, 0f, Glyphs.Stroke, 0, 0f),
        new(MaskId.IconDone, "IconDone", GlyphSize, GlyphSize, 0, 0f, Glyphs.Stroke, 0, 0f),
        new(MaskId.IconAdd, "IconAdd", GlyphSize, GlyphSize, 0, 0f, Glyphs.Stroke, 0, 0f),
    ];

    /// <summary>Size and parameters of <paramref name="id"/>. Does not allocate.</summary>
    public static MaskSpec Spec(MaskId id)
    {
        if ((uint)id >= (uint)_specs.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown mask.");
        }

        return _specs[(int)id];
    }

    /// <summary>Renders <paramref name="id"/> into a new array of <see cref="MaskSpec.ByteCount"/> bytes, top row first.</summary>
    public static byte[] Render(MaskId id)
    {
        byte[] result = new byte[Spec(id).ByteCount];
        Render(id, result);
        return result;
    }

    /// <summary>
    /// Renders <paramref name="id"/> into the first <see cref="MaskSpec.ByteCount"/> bytes of
    /// <paramref name="destination"/>, top row first. Allocates nothing.
    /// </summary>
    public static void Render(MaskId id, Span<byte> destination)
    {
        MaskSpec s = Spec(id);
        switch (id)
        {
            case MaskId.Circle:
                MaskRasterizer.Circle(s.Width, destination);
                break;
            case MaskId.Disc:
                MaskRasterizer.Disc(s.Width, MaskRasterizer.DefaultDiscEdgeLevel, destination);
                break;
            case MaskId.Ring1:
            case MaskId.Ring2:
            case MaskId.Ring3:
                MaskRasterizer.Ring(s.Width, s.Stroke, destination);
                break;
            case MaskId.RingDashed:
                MaskRasterizer.DashedRing(s.Width, s.Stroke, s.DashCount, s.DashDuty, destination);
                break;
            case MaskId.PanelRim:
                MaskRasterizer.RoundedRectRing(s.Width, s.Height, s.CornerRadius, s.Stroke, destination);
                break;
            case MaskId.Panel:
            case MaskId.Plate:
                MaskRasterizer.RoundedRect(s.Width, s.Height, s.CornerRadius, destination);
                break;
            default: // SubMenuBadge … IconAdd
                Glyphs.Render(id, s.Width, destination);
                break;
        }
    }
}
