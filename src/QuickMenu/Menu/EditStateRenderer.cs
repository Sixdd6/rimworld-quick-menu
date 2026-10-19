using QuickMenu.Core.Geometry;
using QuickMenu.Core.Input;
using QuickMenu.Core.Render;
using UnityEngine;
using Verse;

namespace QuickMenu.Menu;

/// <summary>
/// Edit-mode drawing that is not a slot state of its own (T-55, design.md S4 states 8 and 9): the reserved-missing
/// "?" and the dragged pin that follows the mouse. The ghost (9) and drop target (11) are slot states drawn by
/// <see cref="SlotRenderer"/>; the states come from <see cref="EditVisuals"/>.
/// </summary>
/// <remarks>
/// <para>Input for T-57: the edit controller fills a <see cref="DragVisual"/> (source slot, target slot or -1, mouse in
/// window coordinates) and passes it to <see cref="MenuRenderer.Draw(WheelModel, Hit, Hit, in DragVisual)"/> or
/// <see cref="GridRenderer.Draw(WheelModel, Hit, Hit, in DragVisual)"/>; this class is then called from
/// <see cref="MenuRenderer.DrawBody"/>.</para>
/// <para>Repaint only, no allocation: the "?" is a constant and the icon goes through the model's cached command.</para>
/// </remarks>
internal static class EditStateRenderer
{
    // The reserved-missing marker (S4 state 8): a symbol, not a word, so it is not Keyed.
    private const string ReservedMark = "?";

    /// <summary>
    /// The dragged pin (S4 state 9): the source slot's icon in a 0.9 B square whose top-left is (+16, +16) from the
    /// mouse, faded; a reserved-missing source shows its "?" at <see cref="EditVisuals.DragIconAlpha"/> instead.
    /// </summary>
    internal static void DrawDragged(WheelModel model, in DragVisual drag, in RenderTransform transform)
    {
        int source = drag.Source;
        if (source < 0 || source >= model.SlotCount)
        {
            return;
        }

        Rect2 layoutRect = EditVisuals.DraggedIconRect(drag.Mouse, model.ButtonSize * transform.Scale);
        if (model.Kinds[source] == SlotKind.Reserved)
        {
            Color muted = QMStyle.TextMuted;
            muted.a *= EditVisuals.DragIconAlpha;
            DrawReservedMark(layoutRect, muted);
            return;
        }

        // The mouse is in window coordinates already, so the rect is not mapped through the transform.
        SlotRenderer.DrawIcon(model, source, MenuRenderer.ToRect(layoutRect), faded: true);
    }

    /// <summary>The "?" of a reserved-missing pin (S4 state 8), centred in <paramref name="rect"/> in the Medium font.</summary>
    internal static void DrawReservedMark(Rect2 rect, Color color)
    {
        GameFont oldFont = Text.Font;
        TextAnchor oldAnchor = Text.Anchor;
        try
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            MenuRenderer.Tint(color);
            Widgets.Label(MenuRenderer.ToRect(rect), ReservedMark);
        }
        finally
        {
            Text.Anchor = oldAnchor;
            Text.Font = oldFont;
        }
    }
}
