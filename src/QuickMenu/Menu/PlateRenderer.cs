using QuickMenu.Core.Render;
using UnityEngine;
using Verse;

namespace QuickMenu.Menu;

/// <summary>
/// The label plate under the menu (design.md §1.1, S1 step 4): 9-slice <c>Plate</c> mask in <c>plate</c>, centred on
/// cx, top = <c>cy + D + plateGap</c>, width = text + 12 (max 2 D), the cached label in Small <c>text</c>, centred.
/// Labels are truncated to the plate once per open (<see cref="WheelModel.Prepare"/>).
/// </summary>
internal static class PlateRenderer
{
    internal static void Draw(WheelModel model, in RenderTransform transform, PlateContent content, int slotIndex)
    {
        string label;
        float width;
        switch (content)
        {
            case PlateContent.Slot:
                label = model.Label(slotIndex);
                width = model.LabelWidth(slotIndex);
                break;
            case PlateContent.HubClose:
                label = model.HubMainLabel;
                width = model.HubMainLabelWidth;
                break;
            case PlateContent.HubSub:
                label = model.HubSubLabel;
                width = model.HubSubLabelWidth;
                break;
            default:
                return;
        }

        if (label.Length == 0)
        {
            return;
        }

        Rect rect = MenuRenderer.ToRect(transform.Rect(WheelVisuals.PlateRect(model.Center, model.DiscRadius, width, model.PlateHeight)));
        if (Textures.Ready)
        {
            GUI.color = QMStyle.Plate;
            Widgets.DrawAtlas(rect, Textures.Mask(MaskId.Plate));
        }

        GameFont oldFont = Text.Font;
        TextAnchor oldAnchor = Text.Anchor;
        try
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = QMStyle.Text;
            Widgets.Label(rect, label);
        }
        finally
        {
            Text.Anchor = oldAnchor;
            Text.Font = oldFont;
        }
    }
}
