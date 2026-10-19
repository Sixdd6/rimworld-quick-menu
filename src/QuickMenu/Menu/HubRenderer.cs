using QuickMenu.Core.Geometry;
using QuickMenu.Core.Render;
using UnityEngine;

namespace QuickMenu.Menu;

/// <summary>
/// The hub, the Edit button (design.md S4a), drawn above the slots: <c>Circle</c> at H = round(0.64 B) centred on the
/// menu centre, a ring rasterized at H, and the pencil (<c>IconEdit</c> at Hg = round(0.5 H)) in <c>text</c>, also
/// centred. Normal: <c>slotBg</c> + 1 px <c>accent</c>. Hover: <c>slotBgHover</c> + 8 % white + 2 px <c>accent</c>.
/// Pressed: <c>slotBgPressed</c> + 2 px <c>accent</c>. In edit mode (Done, check mark) the fill gets a 22 % <c>accent</c> tint over it and the rings are 2 px, 3 px on hover and press. It is smaller than a slot on purpose; the hit area (P/2) is larger.
/// </summary>
internal static class HubRenderer
{
    internal static void Draw(WheelModel model, in RenderTransform transform, HubPartState state)
    {
        float scale = transform.Scale;
        Vec2 c = transform.Point(model.Center);
        if (Textures.Ready)
        {
            GUI.color = state switch
            {
                HubPartState.Hover => QMStyle.SlotBgHover,
                HubPartState.Pressed => QMStyle.SlotBgPressed,
                _ => QMStyle.SlotBg,
            };
            MenuRenderer.DrawMask(MaskId.Circle, c, model.HubCircleDrawSize * scale);
            if (state == HubPartState.Hover)
            {
                GUI.color = QMStyle.HoverOverlay;
                MenuRenderer.DrawMask(MaskId.Circle, c, model.HubCircleDrawSize * scale);
            }

            if (model.Editing)
            {
                GUI.color = QMStyle.EditTint;
                MenuRenderer.DrawMask(MaskId.Circle, c, model.HubCircleDrawSize * scale);
            }
        }

        GUI.color = QMStyle.Accent;
        MenuRenderer.DrawSized(Ring(HubStyle.RingWidth(state, model.Editing)), c, scale);
        GUI.color = QMStyle.Text;
        MenuRenderer.DrawSized(Textures.Glyph(model.HubGlyph), c, scale);
    }

    private static SizedTexture Ring(float width) => width switch
    {
        >= TextureSizing.Ring3Width => Textures.HubRing3,
        >= TextureSizing.Ring2Width => Textures.HubRing2,
        _ => Textures.HubRing1,
    };
}
