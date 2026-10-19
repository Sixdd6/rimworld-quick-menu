using QuickMenu.Core.Geometry;
using QuickMenu.Core.Render;
using UnityEngine;

namespace QuickMenu.Menu;

/// <summary>
/// The hub (design.md S4a), drawn above the slots. Main area: <c>Circle</c> at B in <c>slotBg</c> 60 % + 1 px
/// <c>slotBorder</c> ring and the × glyph (0.22 B, <c>textMuted</c>) at <c>(cx, cy − 0.12 B)</c>; hover
/// <c>slotBgHover</c> + 2 px <c>accent</c> + × in <c>text</c>; pressed <c>slotBgPressed</c>. Sub-button (only when
/// <see cref="WheelModel.HasHubSub"/>): <c>Circle</c> at e in <c>slotBg</c> + 1 px <c>accent</c> + pencil (0.6 e,
/// <c>text</c>) at <c>(cx, cy + ey)</c>; hover 2 px ring on <c>slotBgHover</c>.
/// </summary>
internal static class HubRenderer
{
    internal static void Draw(WheelModel model, in RenderTransform transform, HubPartState main, HubPartState sub)
    {
        float scale = transform.Scale;
        Vec2 c = transform.Point(model.Center);
        if (Textures.Ready)
        {
            GUI.color = main switch
            {
                HubPartState.Hover => QMStyle.SlotBgHover,
                HubPartState.Pressed => QMStyle.SlotBgPressed,
                _ => QMStyle.HubBg,
            };
            MenuRenderer.DrawMask(MaskId.Circle, c, model.CircleDrawSize * scale);
        }

        bool lit = main != HubPartState.Normal;
        GUI.color = lit ? QMStyle.Accent : QMStyle.SlotBorder;
        MenuRenderer.DrawSized(lit ? Textures.SlotRing2 : Textures.SlotRing1, c, scale);
        GUI.color = lit ? QMStyle.Text : QMStyle.TextMuted;
        MenuRenderer.DrawSized(Textures.Glyph(model.HubMainGlyph), transform.Point(WheelVisuals.HubGlyphCenter(model.Center, model.ButtonSize)), scale);

        if (!model.HasHubSub)
        {
            return;
        }

        Vec2 s = transform.Point(WheelVisuals.HubSubCenter(model.Center, model.ButtonSize));
        bool subLit = sub != HubPartState.Normal;
        if (Textures.Ready)
        {
            GUI.color = sub switch
            {
                HubPartState.Hover => QMStyle.SlotBgHover,
                HubPartState.Pressed => QMStyle.SlotBgPressed,
                _ => QMStyle.SlotBg,
            };
            MenuRenderer.DrawMask(MaskId.Circle, s, model.SubCircleDrawSize * scale);
        }

        GUI.color = QMStyle.Accent;
        MenuRenderer.DrawSized(subLit ? Textures.HubRing2 : Textures.HubRing1, s, scale);
        GUI.color = QMStyle.Text;
        MenuRenderer.DrawSized(Textures.Glyph(model.HubSubGlyph), s, scale);
    }
}
