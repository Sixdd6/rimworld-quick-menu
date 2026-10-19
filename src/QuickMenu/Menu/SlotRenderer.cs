using QuickMenu.Core.Geometry;
using QuickMenu.Core.Input;
using QuickMenu.Core.Render;
using UnityEngine;
using Verse;

namespace QuickMenu.Menu;

/// <summary>
/// One slot (design.md S4) in its base states: Normal (1), Hover (2), Pressed (3), Empty (6). Fill <c>Circle</c> at B,
/// ring rasterized at B, then the designator icon through <c>Command.DrawIcon</c> in a 0.75 B square. Badges and the
/// other states come with their tasks (T-41, T-43, T-65, edit mode).
/// </summary>
internal static class SlotRenderer
{
    internal static void Draw(WheelModel model, int index, SlotVisualState state, in RenderTransform transform)
    {
        Vec2 layoutCentre = model.Slots[index].Center;
        Vec2 c = transform.Point(layoutCentre);
        float scale = transform.Scale;
        if (Textures.Ready)
        {
            GUI.color = Fill(state);
            MenuRenderer.DrawMask(MaskId.Circle, c, model.CircleDrawSize * scale);
            if (state == SlotVisualState.Hover)
            {
                GUI.color = QMStyle.HoverOverlay;
                MenuRenderer.DrawMask(MaskId.Circle, c, model.CircleDrawSize * scale);
            }
        }

        switch (state)
        {
            case SlotVisualState.Hover:
            case SlotVisualState.Pressed:
                GUI.color = QMStyle.Accent;
                MenuRenderer.DrawSized(Textures.SlotRing2, c, scale);
                break;
            case SlotVisualState.Empty:
                GUI.color = QMStyle.SlotBorderEmpty;
                MenuRenderer.DrawSized(Textures.SlotRing1, c, scale);
                return;
            default:
                GUI.color = QMStyle.SlotBorder;
                MenuRenderer.DrawSized(Textures.SlotRing1, c, scale);
                break;
        }

        Command? icon = model.Icon(index);
        if (icon != null)
        {
            GUI.color = QMStyle.White;
            try
            {
                icon.DrawIcon(MenuRenderer.ToRect(transform.Rect(WheelVisuals.IconRect(layoutCentre, model.ButtonSize))), null, default);
            }
            catch (System.Exception ex)
            {
                // A foreign designator's icon code is not ours to disable the menu for: blank this slot's icon and carry on.
                string type = icon.GetType().FullName ?? icon.GetType().Name;
                QuickMenu.Diagnostics.Log.LogOnce($"Menu.Icon.{type}", $"Drawing the icon of {type} failed, its icon is left blank: {ex.GetType().Name}: {ex.Message}");
                model.BlankIcon(index);
            }

            if (model.Kinds[index] == SlotKind.Disabled)
            {
                DrawUnavailable(model, layoutCentre, c, scale, transform);
            }
        }
    }

    // Greyed with reason (S4 state 5): the icon is dimmed under a slotBg veil, and the lock badge sits at 7:30, radius B/2.
    private static void DrawUnavailable(WheelModel model, Vec2 layoutCentre, Vec2 c, float scale, in RenderTransform transform)
    {
        if (!Textures.Ready)
        {
            return;
        }

        GUI.color = QMStyle.UnavailableVeil;
        MenuRenderer.DrawMask(MaskId.Circle, c, model.CircleDrawSize * scale);
        const float diagonal = 0.70710678f;
        float r = model.ButtonSize / 2f;
        Vec2 badge = new Vec2(layoutCentre.X - (r * diagonal), layoutCentre.Y + (r * diagonal));
        GUI.color = QMStyle.RimOutline;
        MenuRenderer.DrawSized(Textures.Glyph(MaskId.Lock), transform.Point(badge), scale * 1.15f);
        GUI.color = QMStyle.TextMuted;
        MenuRenderer.DrawSized(Textures.Glyph(MaskId.Lock), transform.Point(badge), scale);
    }

    private static Color Fill(SlotVisualState state) => state switch
    {
        SlotVisualState.Hover => QMStyle.SlotBgHover,
        SlotVisualState.Pressed => QMStyle.SlotBgPressed,
        SlotVisualState.Empty => QMStyle.SlotBgEmpty,
        _ => QMStyle.SlotBg,
    };
}
