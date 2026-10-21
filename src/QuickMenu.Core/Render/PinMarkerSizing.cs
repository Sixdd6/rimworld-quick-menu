using System;

namespace QuickMenu.Core.Render;

/// <summary>The three pin-marker variants (design.md §2 "Architect pin marker", ADR 0009, FR-19).</summary>
public enum PinMarkerKind
{
    /// <summary>A pinned architect button, or a dropdown button whose whole dropdown is pinned: 15 UI px, <c>pinMarker</c>.</summary>
    Full,

    /// <summary>A dropdown button with only single entries pinned (user decision 11): 11 UI px, <c>pinMarkerPartial</c>.</summary>
    Partial,

    /// <summary>A pinned single entry in an open dropdown list row or grid cell: 10 UI px, <c>pinMarker</c>.</summary>
    Entry,
}

/// <summary>A square marker rect in UI px (top-left corner and side). A value type: computing it allocates nothing.</summary>
/// <param name="X">Left edge.</param>
/// <param name="Y">Top edge.</param>
/// <param name="Size">Side.</param>
public readonly record struct MarkerRect(float X, float Y, float Size)
{
    /// <summary>Right edge.</summary>
    public float XMax => X + Size;

    /// <summary>Bottom edge.</summary>
    public float YMax => Y + Size;

    /// <summary>The same square grown by <paramref name="by"/> on every side.</summary>
    public MarkerRect Expanded(float by) => new(X - by, Y - by, Size + (2f * by));
}

/// <summary>
/// Sizes, textures and placement of the architect pin marker (design.md §2 "Architect pin marker", ADR 0009). The marker
/// is the <see cref="MaskId.PinMarker"/> glyph tinted <c>pinMarker</c> / <c>pinMarkerPartial</c> over an outline mask
/// (the glyph dilated by 1 UI px) tinted <c>pinMarkerOutline</c>. Both are rasterized at their drawn size in screen px,
/// so they depend only on the game's UI scale: rebuild when <see cref="Changed"/> says so, never per frame.
/// </summary>
/// <param name="UiScale">The game's UI scale (screen px per UI px).</param>
public readonly record struct PinMarkerSizing(float UiScale)
{
    /// <summary>Side of a vanilla architect button (gizmo) in UI px; the button placement scales with <c>height / 75</c>.</summary>
    public const float GizmoSize = 75f;

    /// <summary>Full marker side in UI px on a 75 px button (0.2 × 75).</summary>
    public const float FullSize = 15f;

    /// <summary>Partial marker side in UI px (smaller than <see cref="FullSize"/>; same top-right anchor).</summary>
    public const float PartialSize = 11f;

    /// <summary>Dropdown entry marker side in UI px (list rows with a 27 px icon, 34 px grid cells).</summary>
    public const float EntrySize = 10f;

    /// <summary>Outline width in UI px on every side of the glyph (the badge outline convention, design.md S4).</summary>
    public const float OutlineWidth = 1f;

    /// <summary>Gap between the button's right edge and the marker, in UI px at 75 px (× k). The outline fills it.</summary>
    public const float ButtonRightInset = 1f;

    /// <summary>Marker top below the button top, in UI px at 75 px (× k): just under the vanilla "+" slot (y + 1 … y + 17).</summary>
    public const float ButtonTop = 18f;

    /// <summary>Vanilla float-menu row: the icon square starts 4 px right of and below the row's top-left corner.</summary>
    public const float RowIconInset = 4f;

    /// <summary>Vanilla float-menu row: the icon (and label) shift right by this much while the row is hovered and enabled.</summary>
    public const float RowHoverShift = 4f;

    /// <summary>A list-row marker is at most this fraction of the row's icon square (Tiny rows have a 12 px icon).</summary>
    public const float RowMaxIconFraction = 0.5f;

    /// <summary>Gap between a row's right end and the marker when the row has no icon.</summary>
    public const float RowNoIconRightInset = 4f;

    /// <summary>Gap between a grid cell's top-right corner and the marker.</summary>
    public const float CellInset = 1f;

    /// <summary>Number of <see cref="PinMarkerKind"/> values (textures come in fill and outline pairs).</summary>
    public const int KindCount = 3;

    /// <summary>Validated constructor.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The scale is not positive and finite.</exception>
    public static PinMarkerSizing Create(float uiScale)
    {
        if (!(uiScale > 0f) || float.IsInfinity(uiScale))
        {
            throw new ArgumentOutOfRangeException(nameof(uiScale), uiScale, "UI scale must be positive and finite.");
        }

        return new PinMarkerSizing(uiScale);
    }

    /// <summary>Whether textures built for <paramref name="built"/> (null = none built) must be rebuilt for <paramref name="next"/>.</summary>
    public static bool Changed(PinMarkerSizing? built, PinMarkerSizing next) => built is not { } b || b.UiScale != next.UiScale;

    /// <summary>Nominal side of <paramref name="kind"/> in UI px.</summary>
    public static float NominalSize(PinMarkerKind kind) => kind switch
    {
        PinMarkerKind.Full => FullSize,
        PinMarkerKind.Partial => PartialSize,
        PinMarkerKind.Entry => EntrySize,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown pin-marker kind."),
    };

    /// <summary>Fill texture side in texels: the nominal size in screen px, at least <see cref="Glyphs.MinSize"/>.</summary>
    public int Texels(PinMarkerKind kind) =>
        Math.Max(Glyphs.MinSize, (int)MathF.Round(NominalSize(kind) * UiScale, MidpointRounding.AwayFromZero));

    /// <summary>
    /// Side in UI px at which the fill texture is drawn: <see cref="Texels"/> / UI scale, so one texel is one screen px
    /// (within rounding of the nominal size).
    /// </summary>
    public float DrawSize(PinMarkerKind kind) => Texels(kind) / UiScale;

    /// <summary>Extra outline canvas on each side in texels: 1 UI px rounded up, at least 1.</summary>
    public int OutlinePad => Math.Clamp((int)MathF.Ceiling((OutlineWidth * UiScale) - 1e-4f), 1, Glyphs.MaxOutlinePad);

    /// <summary>Outline dilation in texels (1 UI px, at most <see cref="OutlinePad"/>).</summary>
    public float OutlineGrow => Math.Min(OutlineWidth * UiScale, OutlinePad);

    /// <summary>Outline texture side in texels: <see cref="Texels"/> + 2 × <see cref="OutlinePad"/>.</summary>
    public int OutlineTexels(PinMarkerKind kind) => Texels(kind) + (2 * OutlinePad);

    /// <summary>
    /// The outline's draw rect for a fill drawn at <paramref name="fill"/> of kind <paramref name="kind"/>: grown by the
    /// pad, scaled with the fill (a shrunk gizmo draws a smaller fill), so the two textures line up texel for texel.
    /// </summary>
    public MarkerRect OutlineRect(MarkerRect fill, PinMarkerKind kind) =>
        fill.Expanded(OutlinePad / UiScale * (fill.Size / DrawSize(kind)));

    /// <summary>
    /// Marker rect on an architect button (ADR 0009 hook site 4): the top-right column directly under the vanilla "+"
    /// slot, <c>(xMax − 1·k − s, y + 18·k, s, s)</c> with <c>k = height / 75</c> and <c>s = DrawSize(kind) · k</c>. The
    /// partial marker uses the same right edge and top with its smaller side.
    /// </summary>
    public MarkerRect Button(float x, float y, float width, float height, PinMarkerKind kind)
    {
        float k = height / GizmoSize;
        float s = DrawSize(kind) * k;
        return new MarkerRect(x + width - (ButtonRightInset * k) - s, y + (ButtonTop * k), s);
    }

    /// <summary>
    /// Entry marker in a vanilla float-menu row (ADR 0009 hook site 5b): the top-right corner of the option's icon square
    /// <c>(x + 4 [+ 4 hovered], y + 4, iconSize − 4)</c>, where <paramref name="iconSize"/> is the menu's
    /// <c>CurIconSize</c> (27 Normal, 16 Tiny). The side is <see cref="EntrySize"/>, at most half the icon square.
    /// </summary>
    public MarkerRect ListRow(float rowX, float rowY, float iconSize, bool hoverShifted)
    {
        float iconX = rowX + RowIconInset + (hoverShifted ? RowHoverShift : 0f);
        float iconSide = Math.Max(iconSize - RowIconInset, 0f);
        float s = Math.Min(DrawSize(PinMarkerKind.Entry), RowMaxIconFraction * iconSide);
        return new MarkerRect(iconX + iconSide - s, rowY + RowIconInset, s);
    }

    /// <summary>Entry marker in a float-menu row without an icon: at the row's right end, centred vertically.</summary>
    public MarkerRect ListRowNoIcon(float rowX, float rowY, float rowWidth, float rowHeight)
    {
        float s = DrawSize(PinMarkerKind.Entry);
        return new MarkerRect(rowX + rowWidth - RowNoIconRightInset - s, rowY + ((rowHeight - s) / 2f), s);
    }

    /// <summary>Entry marker in a float-menu grid cell (ADR 0009 hook site 5c): its top-right corner, 1 px in.</summary>
    public MarkerRect GridCell(float x, float y, float width)
    {
        float s = DrawSize(PinMarkerKind.Entry);
        return new MarkerRect(x + width - CellInset - s, y + CellInset, s);
    }
}
