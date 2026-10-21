using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;
using QuickMenu.Core.Render;
using UnityEngine;

namespace QuickMenu.Menu;

/// <summary>
/// A satellite: the sub-menu or material ring of a slot (design.md S3, §1.1 "Sub-menu and material placement"),
/// drawn above the dimmed main menu (<see cref="MenuRenderer.Draw(WheelModel, in RenderTransform, Hit, Hit, int)"/>
/// with the parent index). Order: its own disc and satellite rim at <c>2·D_s</c>, the option slots (material options
/// show the stuff icon in its colour; the last-used one has the 3 px ring), the hub, and the label plate.
/// </summary>
/// <remarks>
/// <para>The hub of a satellite is the parent slot itself (state 14, open parent), redrawn at the satellite centre
/// at full opacity with the parent's icon, badges and a 2 px <c>accent</c> ring; it hovers and presses like a slot
/// (a click on it goes back). When the satellite was edge-dodged its centre is not the parent's position, and the hub
/// moves with it. For a material ring opened from a sub-menu option, pass the sub-ring model and that option as the
/// hub source ("the hub shows the chosen option").</para>
/// <para>The satellite model is built once when the satellite opens and prepared with
/// <see cref="WheelModel.PrepareSatellite"/>. The hub's plate label is the satellite model's
/// <see cref="WheelModel.HubLabel"/> (set it to the parent's label). Allocation-free; draws only on Repaint.</para>
/// </remarks>
internal static class SatelliteRenderer
{
    /// <summary>Draws <paramref name="satellite"/> with slot <paramref name="hubIndex"/> of <paramref name="hubSource"/> as its hub.</summary>
    /// <param name="satellite">The satellite model (centre = satellite hub, slots = options).</param>
    /// <param name="hubSource">The model holding the parent slot: the main menu, or the sub-ring for a material ring.</param>
    /// <param name="hubIndex">The parent slot's index in <paramref name="hubSource"/>.</param>
    /// <param name="hover">Hover hit in the satellite's <see cref="WheelModel.HitLayout"/> (<see cref="HitKind.HubEdit"/> = its hub).</param>
    /// <param name="pressed">Where the current press began in the satellite's hit layout, or <see cref="Hit.Outside"/>.</param>
    internal static void Draw(WheelModel satellite, WheelModel hubSource, int hubIndex, Hit hover, Hit pressed)
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        var transform = RenderTransform.Identity(satellite.Center);
        Color old = GUI.color;
        try
        {
            MenuRenderer.DrawDisc(satellite, transform);
            for (int i = 0; i < satellite.SlotCount; i++)
            {
                SlotRenderer.Draw(satellite, i, WheelVisuals.Slot(i, satellite.Kinds[i], hover, pressed), transform);
            }

            if (hubIndex >= 0 && hubIndex < hubSource.SlotCount)
            {
                SlotRenderer.DrawAt(
                    hubSource,
                    hubIndex,
                    satellite.Center,
                    WheelVisuals.SatelliteHub(hover, pressed),
                    hubSource.Decor(hubIndex) | SlotDecor.OpenParent,
                    transform);
            }

            PlateRenderer.Draw(satellite, transform, WheelVisuals.Plate(hover, satellite.Kinds, satellite.SlotCount), hover.Index);
        }
        finally
        {
            GUI.color = old;
        }
    }
}
