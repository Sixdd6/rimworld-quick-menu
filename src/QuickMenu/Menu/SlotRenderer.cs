using QuickMenu.Core.Geometry;
using QuickMenu.Core.Input;
using QuickMenu.Core.Render;
using UnityEngine;
using Verse;

namespace QuickMenu.Menu;

/// <summary>
/// One slot (design.md S4), one routine for every layout. Base states Normal (1), Hover (2), Pressed (3), Empty (6),
/// combined with the decorations of T-42: last-used material (4, 3 px <c>accent</c>), greyed with lock (5), sub-menu
/// badge (12, 1:30), material dot (13, 4:30), open parent (14, 2 px <c>accent</c>), and in edit mode (T-55) the
/// reserved-missing pin (8: dashed <c>emptyBorder</c> ring and "?"; in use mode it looks empty), the drag source
/// (9: dashed empty ghost, no icon or badge) and the drop target (11: <c>slotBgHover</c> + 22 % <c>accent</c> tint +
/// 2 px <c>accent</c>, with the slot's content; an empty Fixed slot too). The dragged pin itself is drawn by
/// <see cref="EditStateRenderer.DrawDragged"/>. There is no state 7 ("+") and no state 10 (insert cursor).
/// </summary>
/// <remarks>
/// Order: fill <c>Circle</c> at B (+ hover overlay), the icon (<c>Command.DrawIcon</c> in a 0.75 B square, or a
/// material option's stuff icon in its own colour through <c>Widgets.DefIcon</c>), the icon veil (greyed and/or
/// dimmed), the ring rasterized at B, then the badges, each with a 1 px <c>rimOutline</c> under it. Every colour goes
/// through <see cref="MenuRenderer.Tint"/>, so a dimmed menu draws at 45 %. <c>Command.DrawIcon</c> sets its own colour,
/// so icons are faded by the <c>slotBg</c> veil instead (<see cref="WheelVisuals.IconVeilAlpha"/>).
/// </remarks>
internal static class SlotRenderer
{
    // The game's faded gizmo icon, used for the dragged pin (a struct: no allocation).
    private static readonly GizmoRenderParms _fadedIcon = new() { lowLight = true };

    /// <summary>Draws slot <paramref name="index"/> of <paramref name="model"/> at its layout position.</summary>
    internal static void Draw(WheelModel model, int index, SlotVisualState state, in RenderTransform transform) =>
        DrawAt(model, index, model.Slots[index].Center, state, model.Decor(index), transform);

    /// <summary>
    /// Draws slot <paramref name="index"/> of <paramref name="model"/> centred on <paramref name="layoutCentre"/>
    /// (in the coordinates of <paramref name="transform"/>) with <paramref name="decor"/>. The satellite hub uses this
    /// to redraw the parent slot at the satellite centre with <see cref="SlotDecor.OpenParent"/>.
    /// </summary>
    internal static void DrawAt(WheelModel model, int index, Vec2 layoutCentre, SlotVisualState state, SlotDecor decor, in RenderTransform transform)
    {
        Vec2 c = transform.Point(layoutCentre);
        float scale = transform.Scale;
        float circle = model.CircleDrawSize * scale;
        SlotKind kind = model.Kinds[index];
        if (Textures.Ready)
        {
            MenuRenderer.Tint(Fill(state));
            MenuRenderer.DrawMask(MaskId.Circle, c, circle);
            if (state == SlotVisualState.Hover)
            {
                MenuRenderer.Tint(QMStyle.HoverOverlay);
                MenuRenderer.DrawMask(MaskId.Circle, c, circle);
            }
            else if (EditVisuals.ShowsDropTint(state))
            {
                MenuRenderer.Tint(QMStyle.DropTargetTint);
                MenuRenderer.DrawMask(MaskId.Circle, c, circle);
            }
        }

        if (EditVisuals.ShowsIcon(state))
        {
            DrawIcon(model, index, layoutCentre, transform);
            float veil = WheelVisuals.IconVeilAlpha(kind, MenuRenderer.Dimmed);
            if (veil > 0f && Textures.Ready)
            {
                // Not through Tint: the veil alpha already includes the dimming.
                GUI.color = new Color(QMStyle.SlotBg.r, QMStyle.SlotBg.g, QMStyle.SlotBg.b, veil);
                MenuRenderer.DrawMask(MaskId.Circle, c, circle);
            }
        }

        DrawRing(WheelVisuals.Ring(state, decor, kind, model.Editing), c, scale);
        if (WheelVisuals.ShowsReservedMarker(state, kind, model.Editing))
        {
            EditStateRenderer.DrawReservedMark(transform.Rect(WheelVisuals.IconRect(layoutCentre, model.ButtonSize)), QMStyle.TextMuted);
        }

        if (!EditVisuals.ShowsBadges(state))
        {
            return;
        }

        if (WheelVisuals.ShowsBadge(SlotBadge.SubMenu, kind, decor))
        {
            DrawBadge(Textures.Glyph(MaskId.SubMenuBadge), QMStyle.TextMuted, WheelVisuals.BadgeCentre(SlotBadge.SubMenu, layoutCentre, model.ButtonSize), transform);
        }

        if (WheelVisuals.ShowsBadge(SlotBadge.Material, kind, decor))
        {
            DrawBadge(Textures.MaterialDot, model.MaterialDotColor(index), WheelVisuals.BadgeCentre(SlotBadge.Material, layoutCentre, model.ButtonSize), transform);
        }

        if (WheelVisuals.ShowsBadge(SlotBadge.Lock, kind, decor))
        {
            DrawBadge(Textures.Glyph(MaskId.Lock), QMStyle.TextMuted, WheelVisuals.BadgeCentre(SlotBadge.Lock, layoutCentre, model.ButtonSize), transform);
        }
    }

    private static void DrawIcon(WheelModel model, int index, Vec2 layoutCentre, in RenderTransform transform) =>
        DrawIcon(model, index, MenuRenderer.ToRect(transform.Rect(WheelVisuals.IconRect(layoutCentre, model.ButtonSize))), faded: false);

    /// <summary>
    /// Draws the icon of slot <paramref name="index"/> in <paramref name="rect"/>: the designator's own
    /// <c>Command.DrawIcon</c>, or a material option's stuff icon. <paramref name="faded"/> draws it as the dragged pin
    /// (S4 state 9): <c>Command.DrawIcon</c> sets its own colour, so the fade is the game's own low-light icon
    /// (<c>GizmoRenderParms.lowLight</c>, 60 % on 1.7.4982); a stuff icon gets <see cref="EditVisuals.DragIconAlpha"/>.
    /// </summary>
    internal static void DrawIcon(WheelModel model, int index, Rect rect, bool faded)
    {
        Command? icon = model.Icon(index);
        ThingDef? stuff = model.StuffIcon(index);
        if (icon == null && stuff == null)
        {
            return;
        }

        GUI.color = QMStyle.White;
        try
        {
            if (icon != null)
            {
                icon.DrawIcon(rect, null, faded ? _fadedIcon : default);
            }
            else
            {
                // The stuff's own icon and colour, as the game's resource readout and stuff menus draw it.
                Widgets.DefIcon(rect, stuff, alpha: faded ? EditVisuals.DragIconAlpha : 1f);
            }
        }
        catch (System.Exception ex)
        {
            // A foreign designator's or def's icon code is not ours to disable the menu for: blank this slot's icon and carry on.
            System.Type t = icon != null ? icon.GetType() : typeof(ThingDef);
            string name = t.FullName ?? t.Name;
            QuickMenu.Diagnostics.Log.LogOnce($"Menu.Icon.{name}", $"Drawing the icon of {name} failed, its icon is left blank: {ex.GetType().Name}: {ex.Message}");
            model.BlankIcon(index);
        }
    }

    private static void DrawRing(SlotRing ring, Vec2 c, float scale)
    {
        switch (ring)
        {
            case SlotRing.Accent3:
                MenuRenderer.Tint(QMStyle.Accent);
                MenuRenderer.DrawSized(Textures.SlotRing3, c, scale);
                break;
            case SlotRing.Accent2:
                MenuRenderer.Tint(QMStyle.Accent);
                MenuRenderer.DrawSized(Textures.SlotRing2, c, scale);
                break;
            case SlotRing.EmptyDashed:
                MenuRenderer.Tint(QMStyle.EmptyBorder);
                MenuRenderer.DrawSized(Textures.SlotRingDashed, c, scale);
                break;
            case SlotRing.EmptyBorder1:
                MenuRenderer.Tint(QMStyle.SlotBorderEmpty);
                MenuRenderer.DrawSized(Textures.SlotRing1, c, scale);
                break;
            default:
                MenuRenderer.Tint(QMStyle.SlotBorder);
                MenuRenderer.DrawSized(Textures.SlotRing1, c, scale);
                break;
        }
    }

    // A badge: the same mask tinted rimOutline, 1 px larger on each side, under the tinted mask (design.md §3).
    private static void DrawBadge(in SizedTexture texture, Color color, Vec2 layoutCentre, in RenderTransform transform)
    {
        Vec2 p = transform.Point(layoutCentre);
        float scale = transform.Scale;
        MenuRenderer.Tint(QMStyle.RimOutline);
        MenuRenderer.DrawSized(texture, p, scale * WheelVisuals.OutlineScale(texture.DrawSize));
        MenuRenderer.Tint(color);
        MenuRenderer.DrawSized(texture, p, scale);
    }

    private static Color Fill(SlotVisualState state) => state switch
    {
        SlotVisualState.Hover or SlotVisualState.DropTarget => QMStyle.SlotBgHover,
        SlotVisualState.Pressed => QMStyle.SlotBgPressed,
        SlotVisualState.Empty or SlotVisualState.DragSource => QMStyle.SlotBgEmpty,
        _ => QMStyle.SlotBg,
    };
}
