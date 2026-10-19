using System;

namespace QuickMenu.Core.Render;

/// <summary>
/// Badge and hub-icon glyphs (design.md S4 badges, S4a hub icons, §3 art direction): grayscale alpha masks tinted
/// with <c>GUI.color</c> at draw time, like the other masks of <see cref="MaskSet"/>.
/// </summary>
/// <remarks>
/// <para>Every glyph is authored on a <see cref="GridSize"/> px grid with a <see cref="Margin"/> px empty border and
/// <see cref="Stroke"/> px strokes, and is rasterized <b>in its final orientation</b>: the game draws no rotated GUI
/// (design.md §1), so the 45° shapes (pencil, badge triangle, check mark) are baked as they appear on screen.</para>
/// <para>Rows are written <b>top row first</b> (PNG order). These glyphs are not vertically symmetric, so the
/// adapter must flip rows for <c>SetPixels32</c>, which expects the bottom row first.</para>
/// <para>Coverage is the same signed-distance rule as <see cref="MaskRasterizer"/>: <c>clamp(0.5 − d, 0, 1)</c>
/// with <c>d</c> in output pixels, so edges stay one pixel soft at any <c>size</c>. Corners of the filled
/// triangles stay sharp (the distance is the maximum of the edge half-planes).</para>
/// <para>Placement (design.md S4, at radius B/2 on the 45° diagonals): <see cref="MaskId.SubMenuBadge"/> at 1:30
/// (0.22 B) and <see cref="MaskId.Lock"/> at 7:30 (0.25 B). The material dot at 4:30 is its own size-dependent circle.
/// 10:30 is free: the insert caret and the edit-mode "+" were removed with the insert cursor and the Add slot
/// (user decision 2026-10-19, T-55).</para>
/// </remarks>
public static class Glyphs
{
    /// <summary>Authoring grid size in pixels; also the mask size in <see cref="MaskSet"/>.</summary>
    public const int GridSize = 64;

    /// <summary>Empty border on every side, in grid pixels.</summary>
    public const int Margin = 4;

    /// <summary>Stroke width of the line glyphs (✓, lock shackle), in grid pixels.</summary>
    public const float Stroke = 6f;

    /// <summary>
    /// Smallest size <see cref="Render(MaskId, int)"/> accepts. Badges are drawn at 0.18–0.25 B (about 7–14 px at
    /// B = 36–56), so the adapter rasterizes them at their drawn size instead of minifying the 64 px grid; at 8 px the
    /// margin is 0.5 px and the strokes are 0.75 px, still one coverage sample per pixel.
    /// </summary>
    public const int MinSize = 8;

    private const double HalfStroke = Stroke / 2.0;
    private const double InvSqrt2 = 0.70710678118654752;

    // Lock (7:30): rounded body below, shackle (half-ring + legs) above.
    private const double LockBodyCx = 32, LockBodyCy = 43, LockBodyHalfW = 24, LockBodyHalfH = 15, LockBodyRadius = 5;
    private const double ShackleCy = 24, ShackleR = 13, ShackleLegEnd = 31;

    // ✓: short arm down-right to the vertex, long arm up-right.
    private const double CheckAx = 10, CheckAy = 34, CheckVx = 25, CheckVy = 49, CheckBx = 54, CheckBy = 18;

    // Pencil: axis from the tip (bottom-left) up-right at 45°; total length along the axis, half-width, tip length.
    private const double PencilTipX = 8, PencilTipY = 56, PencilLength = 62, PencilHalfW = 9, PencilTipLength = 17;

    /// <summary>The first glyph mask; the glyphs are the contiguous range <see cref="First"/> … <see cref="Last"/>.</summary>
    public const MaskId First = MaskId.SubMenuBadge;

    /// <summary>The last glyph mask.</summary>
    public const MaskId Last = MaskId.IconDone;

    /// <summary>Number of glyph masks.</summary>
    public const int Count = Last - First + 1;

    /// <summary>True for the glyph masks (<see cref="First"/> … <see cref="Last"/>).</summary>
    public static bool IsGlyph(MaskId id) => id >= First && id <= Last;

    /// <summary>Renders glyph <paramref name="id"/> at <paramref name="size"/> × <paramref name="size"/> into a new array, top row first.</summary>
    public static byte[] Render(MaskId id, int size)
    {
        Check(id, size);
        byte[] result = new byte[size * size];
        Render(id, size, result);
        return result;
    }

    /// <summary>
    /// Renders glyph <paramref name="id"/> into the first <c>size²</c> bytes of <paramref name="destination"/>,
    /// top row first. The grid scales with <paramref name="size"/> (margin and strokes included). Allocates nothing.
    /// </summary>
    public static void Render(MaskId id, int size, Span<byte> destination)
    {
        Check(id, size);
        if (destination.Length < size * size)
        {
            throw new ArgumentException($"Destination needs {size * size} bytes, has {destination.Length}.", nameof(destination));
        }

        double toGrid = (double)GridSize / size;
        double toPixels = 1.0 / toGrid;
        for (int y = 0; y < size; y++)
        {
            double gy = (y + 0.5) * toGrid;
            int row = y * size;
            for (int x = 0; x < size; x++)
            {
                double gx = (x + 0.5) * toGrid;
                double d = Distance(id, gx, gy) * toPixels;
                destination[row + x] = (byte)((Math.Clamp(0.5 - d, 0.0, 1.0) * 255.0) + 0.5);
            }
        }
    }

    private static void Check(MaskId id, int size)
    {
        if (!IsGlyph(id))
        {
            throw new ArgumentOutOfRangeException(nameof(id), id, "Not a glyph mask.");
        }

        if (size < MinSize)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, $"Glyph size must be ≥ {MinSize} px.");
        }
    }

    /// <summary>Signed distance in grid pixels (negative inside) from <c>(x, y)</c> to the glyph outline.</summary>
    private static double Distance(MaskId id, double x, double y)
    {
        switch (id)
        {
            case MaskId.SubMenuBadge:
                // Right triangle in the top-right corner of the margin box: (4,4) → (60,4) → (60,60).
                return Triangle(x, y, Margin, Margin, GridSize - Margin, Margin, GridSize - Margin, GridSize - Margin);
            case MaskId.Lock:
                return Math.Min(LockBody(x, y), Shackle(x, y) - HalfStroke);
            case MaskId.IconEdit:
                return Pencil(x, y);
            default: // IconDone
                return Math.Min(Segment(x, y, CheckAx, CheckAy, CheckVx, CheckVy), Segment(x, y, CheckVx, CheckVy, CheckBx, CheckBy)) - HalfStroke;
        }
    }

    /// <summary>Distance to the segment a–b (unsigned; subtract half a stroke for a round-capped line).</summary>
    private static double Segment(double px, double py, double ax, double ay, double bx, double by)
    {
        double vx = bx - ax;
        double vy = by - ay;
        double wx = px - ax;
        double wy = py - ay;
        double t = Math.Clamp(((wx * vx) + (wy * vy)) / ((vx * vx) + (vy * vy)), 0.0, 1.0);
        double dx = wx - (t * vx);
        double dy = wy - (t * vy);
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    /// <summary>
    /// Signed distance to a triangle whose vertices are listed clockwise on screen (y down): the maximum of the three
    /// edge half-plane distances. Exact inside and along the edges; corners stay sharp.
    /// </summary>
    private static double Triangle(double px, double py, double ax, double ay, double bx, double by, double cx, double cy)
    {
        double d = Edge(px, py, ax, ay, bx, by);
        d = Math.Max(d, Edge(px, py, bx, by, cx, cy));
        return Math.Max(d, Edge(px, py, cx, cy, ax, ay));
    }

    /// <summary>Signed distance to the line a→b, negative on the inner side of a clockwise-on-screen polygon.</summary>
    private static double Edge(double px, double py, double ax, double ay, double bx, double by)
    {
        double ex = bx - ax;
        double ey = by - ay;
        return -((ex * (py - ay)) - (ey * (px - ax))) / Math.Sqrt((ex * ex) + (ey * ey));
    }

    private static double LockBody(double x, double y)
    {
        double qx = Math.Abs(x - LockBodyCx) - (LockBodyHalfW - LockBodyRadius);
        double qy = Math.Abs(y - LockBodyCy) - (LockBodyHalfH - LockBodyRadius);
        double ox = Math.Max(qx, 0.0);
        double oy = Math.Max(qy, 0.0);
        return Math.Sqrt((ox * ox) + (oy * oy)) + Math.Min(Math.Max(qx, qy), 0.0) - LockBodyRadius;
    }

    /// <summary>Distance to the shackle centre line: the upper half-circle plus two legs down into the body.</summary>
    private static double Shackle(double x, double y)
    {
        double dx = x - LockBodyCx;
        if (y <= ShackleCy)
        {
            double dy = y - ShackleCy;
            return Math.Abs(Math.Sqrt((dx * dx) + (dy * dy)) - ShackleR);
        }

        double legX = Math.Abs(Math.Abs(dx) - ShackleR);
        double legY = Math.Max(y - ShackleLegEnd, 0.0);
        return Math.Sqrt((legX * legX) + (legY * legY));
    }

    /// <summary>Filled pencil: a convex pentagon (pointed tip, straight body, flat end) along the 45° axis.</summary>
    private static double Pencil(double x, double y)
    {
        double rx = x - PencilTipX;
        double ry = y - PencilTipY;
        double along = (rx - ry) * InvSqrt2;   // towards the top-right
        double across = Math.Abs((rx + ry) * InvSqrt2);
        double tipNorm = Math.Sqrt((PencilTipLength * PencilTipLength) + (PencilHalfW * PencilHalfW));
        double tip = ((across * PencilTipLength) - (PencilHalfW * along)) / tipNorm;
        return Math.Max(Math.Max(along - PencilLength, across - PencilHalfW), tip);
    }
}
