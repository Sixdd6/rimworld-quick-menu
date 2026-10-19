using QuickMenu.Core.Layout;
using QuickMenu.Core.Render;
using UnityEngine;

namespace QuickMenu.Menu;

/// <summary>
/// Draws the grid layout (design.md S2) and grid sub-menus (S3: sub-grids and material grids) from a
/// <see cref="WheelModel"/> with a panel (<see cref="WheelModel.SetPanel"/>). The order and the routines are the
/// wheel's: panel (instead of the disc), slots (<see cref="SlotRenderer"/>), hub in the centre cell
/// (<see cref="HubRenderer"/>, the Edit button, S4a), label plate under the panel (<see cref="PlateRenderer"/>).
/// </summary>
/// <remarks>
/// <para>The model is generic: <see cref="WheelModel.Slots"/> are the cell placements (centres in reading order, hub
/// skipped) and <see cref="WheelModel.Center"/> is the centre cell, where the hub is drawn. Grid hit testing is the
/// caller's (P × P cells, the centre cell = <see cref="HitKind.HubEdit"/>); this class only reads the hits.</para>
/// <para>While a sub-grid is open the main grid is drawn at <c>dimmedMenu</c> (45 %) without hover, press or plate,
/// and the parent cell is redrawn on top at full opacity in the open-parent state (14). Then the sub-grid is drawn
/// with <see cref="DrawSubGrid"/>: its own panel, its options and its plate, and no hub (§1.1: "reading order, no
/// hub"; the open parent stays in the main grid). There is no underlay (T-86 declined). A material grid opened from a
/// sub-grid option replaces the sub-grid and is drawn the same way.</para>
/// <para>Draws only on <see cref="EventType.Repaint"/> and allocates nothing.</para>
/// </remarks>
internal static class GridRenderer
{
    /// <summary>Draws the main grid where its layout put it, at full size.</summary>
    internal static void Draw(WheelModel model, Hit hover, Hit pressed) =>
        Draw(model, RenderTransform.Identity(model.Center), hover, pressed);

    /// <summary>Draws the main grid dimmed behind an open sub-grid whose parent is slot <paramref name="openParent"/>.</summary>
    internal static void DrawDimmed(WheelModel model, int openParent) =>
        Draw(model, RenderTransform.Identity(model.Center), Hit.Outside, Hit.Outside, openParent);

    /// <summary>
    /// Draws the main grid with its hub at <c>transform.Origin</c>, scaled by <c>transform.Scale</c> (the settings
    /// preview, T-59). With <paramref name="openParent"/> ≥ 0 a sub-grid is open over that slot: the grid is dimmed and
    /// ignores <paramref name="hover"/> and <paramref name="pressed"/>.
    /// </summary>
    internal static void Draw(WheelModel model, in RenderTransform transform, Hit hover, Hit pressed, int openParent = -1)
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        bool dimmed = openParent >= 0;
        hover = WheelVisuals.Interactive(hover, dimmed);
        pressed = WheelVisuals.Interactive(pressed, dimmed);
        Color old = GUI.color;
        try
        {
            MenuRenderer.SetDimmed(dimmed);
            if (model.Panel is { } panel)
            {
                PanelRenderer.Draw(panel, transform);
            }

            MenuRenderer.DrawBody(model, transform, hover, pressed, openParent);
        }
        finally
        {
            MenuRenderer.SetDimmed(false);
            GUI.color = old;
        }
    }

    /// <summary>
    /// Draws a sub-grid or material grid (<paramref name="subGrid"/>: panel = <c>PlacedSubGrid.Panel</c>, slots =
    /// <c>PlacedSubGrid.Cells</c>) at full opacity above the dimmed main grid: panel, options, plate; no hub.
    /// </summary>
    /// <param name="subGrid">The sub-grid model, prepared once when it opens.</param>
    /// <param name="hover">Hover hit in the sub-grid's cells.</param>
    /// <param name="pressed">Where the current press began in the sub-grid's cells, or <see cref="Hit.Outside"/>.</param>
    internal static void DrawSubGrid(WheelModel subGrid, Hit hover, Hit pressed)
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        var transform = RenderTransform.Identity(subGrid.Center);
        Color old = GUI.color;
        try
        {
            if (subGrid.Panel is { } panel)
            {
                PanelRenderer.Draw(panel, transform);
            }

            for (int i = 0; i < subGrid.SlotCount; i++)
            {
                SlotRenderer.Draw(subGrid, i, WheelVisuals.Slot(i, subGrid.Kinds[i], hover, pressed), transform);
            }

            PlateRenderer.Draw(subGrid, transform, WheelVisuals.Plate(hover, subGrid.Kinds, subGrid.SlotCount), hover.Index);
        }
        finally
        {
            GUI.color = old;
        }
    }
}
