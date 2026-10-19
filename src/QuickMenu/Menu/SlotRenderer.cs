using QuickMenu.Core.Geometry;
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
                GUI.color = QMStyle.EmptyBorder;
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
            icon.DrawIcon(MenuRenderer.ToRect(transform.Rect(WheelVisuals.IconRect(layoutCentre, model.ButtonSize))), null, default);
        }
    }

    private static Color Fill(SlotVisualState state) => state switch
    {
        SlotVisualState.Hover => QMStyle.SlotBgHover,
        SlotVisualState.Pressed => QMStyle.SlotBgPressed,
        SlotVisualState.Empty => QMStyle.SlotBgEmpty,
        _ => QMStyle.SlotBg,
    };
}
