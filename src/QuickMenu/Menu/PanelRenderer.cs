using QuickMenu.Core.Geometry;
using QuickMenu.Core.Render;
using UnityEngine;
using Verse;

namespace QuickMenu.Menu;

/// <summary>
/// The grid panel (design.md §2 "Grid panel", S2, S3): the background of the main grid, of sub-grids and of material
/// grids. Fill: the 9-slice <c>Panel</c> mask (radius 10) in <c>discCentre</c>, flat, because a 9-slice cannot carry
/// the disc's radial fall-off (the darker end keeps slots readable over snow). Rim: the 9-slice <c>PanelRim</c> mask,
/// a 2 px band inside the rect, in <c>accent</c>, over the same mask in <c>rimOutline</c> at the rect grown and shrunk
/// by 1 px, so the rim has a 1 px dark outline on each side like the disc rim.
/// </summary>
/// <remarks>
/// Drawn with <c>Widgets.DrawAtlas</c> (border = atlas / 4 = 16 ≥ radius 10, spike S-7). A rect smaller than two
/// borders (only possible in a scaled-down preview) is drawn stretched instead, so its corners do not overlap.
/// Every colour goes through <see cref="MenuRenderer.Tint"/>, so a dimmed menu draws at <c>dimmedMenu</c>. Allocation-free.
/// </remarks>
internal static class PanelRenderer
{
    /// <summary>Draws the panel <paramref name="panel"/> (layout coordinates) through <paramref name="transform"/>; call on Repaint only.</summary>
    internal static void Draw(Rect2 panel, in RenderTransform transform)
    {
        if (!Textures.Ready)
        {
            return;
        }

        Rect2 r = transform.Rect(panel);
        MenuRenderer.Tint(QMStyle.DiscFill);
        DrawSlice(r, MaskId.Panel);

        // Outlines are 1 UI px whatever the transform scale: grow and shrink the rect on screen, not in the layout.
        MenuRenderer.Tint(QMStyle.RimOutline);
        DrawSlice(PanelVisuals.OuterOutlineRect(r), MaskId.PanelRim);
        DrawSlice(PanelVisuals.InnerOutlineRect(r), MaskId.PanelRim);
        MenuRenderer.Tint(QMStyle.Accent);
        DrawSlice(r, MaskId.PanelRim);
    }

    private static void DrawSlice(Rect2 r, MaskId mask)
    {
        Rect rect = MenuRenderer.ToRect(r);
        if (PanelVisuals.FitsAtlas(r))
        {
            Widgets.DrawAtlas(rect, Textures.Mask(mask));
        }
        else
        {
            GUI.DrawTexture(rect, Textures.Mask(mask));
        }
    }
}
