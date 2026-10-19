using QuickMenu.Core.Geometry;
using QuickMenu.Core.Input;
using QuickMenu.Core.Layout;
using QuickMenu.Core.Render;
using UnityEngine;

namespace QuickMenu.Menu;

/// <summary>
/// Draws a wheel menu from a per-open <see cref="WheelModel"/> (design.md S1): disc + rim, slots, hub, label plate,
/// in that order. It owns no input or menu logic: the caller hit-tests (<see cref="WheelModel.HitLayout"/>) and passes
/// the hover hit and the hit where the current press began (<see cref="Hit.Outside"/> when no button is held).
/// Hover reacts at once, also on open (user decision, UC-2b).
/// <para>While a satellite is open (T-42, S1, S3) the caller passes the parent's slot index: the main menu is then
/// drawn at <c>dimmedMenu</c> (45 %) without hover, press or plate, and the parent is redrawn on top at full opacity
/// in the open-parent state (2 px <c>accent</c>). Then the caller draws the satellite with
/// <see cref="SatelliteRenderer"/>.</para>
/// </summary>
/// <remarks>
/// Draws only on <see cref="EventType.Repaint"/> and allocates nothing: every colour comes from <see cref="QMStyle"/>,
/// every label and size from the model. IMGUI only, no rotated drawing. Size-dependent textures come from
/// <see cref="Textures.EnsureSized"/> (called by <see cref="WheelModel.Prepare"/>); with a scale other than 1 they are
/// stretched (the 50 % settings preview, T-59, may want its own sizes then).
/// </remarks>
internal static class MenuRenderer
{
    /// <summary>Draws the model where its layout put it, at full size.</summary>
    internal static void Draw(WheelModel model, Hit hover, Hit pressed) =>
        Draw(model, RenderTransform.Identity(model.Center), hover, pressed);

    /// <summary>
    /// Draws the model in edit mode with a drag in progress (T-55, S4 states 9 and 11): the source as a dashed ghost,
    /// the target highlighted, no hover elsewhere, and the dragged pin following the mouse on top. With
    /// <see cref="DragVisual.None"/> it is <see cref="Draw(WheelModel, Hit, Hit)"/>.
    /// </summary>
    internal static void Draw(WheelModel model, Hit hover, Hit pressed, in DragVisual drag) =>
        Draw(model, RenderTransform.Identity(model.Center), hover, pressed, -1, drag);

    /// <summary>
    /// Draws the main menu dimmed behind an open satellite whose parent is slot <paramref name="openParent"/>
    /// (no hover, no plate), with the parent on top at full opacity in the open-parent state.
    /// </summary>
    internal static void DrawDimmed(WheelModel model, int openParent) =>
        Draw(model, RenderTransform.Identity(model.Center), Hit.Outside, Hit.Outside, openParent);

    /// <summary>
    /// Draws the model with its hub at <c>transform.Origin</c>, scaled by <c>transform.Scale</c>. With
    /// <paramref name="openParent"/> ≥ 0 a satellite is open over slot <paramref name="openParent"/>: the menu is
    /// dimmed and ignores <paramref name="hover"/> and <paramref name="pressed"/>.
    /// </summary>
    internal static void Draw(WheelModel model, in RenderTransform transform, Hit hover, Hit pressed, int openParent = -1) =>
        Draw(model, transform, hover, pressed, openParent, DragVisual.None);

    private static void Draw(WheelModel model, in RenderTransform transform, Hit hover, Hit pressed, int openParent, in DragVisual drag)
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
            SetDimmed(dimmed);
            DrawDisc(model, transform);
            DrawBody(model, transform, hover, pressed, openParent, drag);
        }
        finally
        {
            SetDimmed(false);
            GUI.color = old;
        }
    }

    /// <summary>
    /// Slots, hub, and the plate or the open parent: everything of a menu above its background (disc or panel), the
    /// same for the wheel and the grid. Call inside <see cref="SetDimmed"/> with the hits already passed through
    /// <see cref="WheelVisuals.Interactive"/>. With an active <paramref name="drag"/> (edit mode, never with a
    /// satellite open) the slot, hub and plate states come from <see cref="EditVisuals"/> and the dragged pin is drawn
    /// last, above everything.
    /// </summary>
    internal static void DrawBody(WheelModel model, in RenderTransform transform, Hit hover, Hit pressed, int openParent, in DragVisual drag)
    {
        for (int i = 0; i < model.SlotCount; i++)
        {
            SlotRenderer.Draw(model, i, EditVisuals.Slot(i, model.Kinds[i], hover, pressed, drag), transform);
        }

        HubRenderer.Draw(model, transform, EditVisuals.Hub(hover, pressed, drag));
        if (openParent < 0)
        {
            if (!model.Editing && HasNoPins(model))
            {
                HelpPanelRenderer.DrawHint(model, transform);
            }
            else
            {
                PlateRenderer.Draw(model, transform, EditVisuals.Plate(hover, model.Kinds, model.SlotCount, drag), hover.Index);
            }

            if (drag.Active)
            {
                EditStateRenderer.DrawDragged(model, drag, transform);
            }
        }
        else if (openParent < model.SlotCount)
        {
            // State 14: the open parent is drawn in full opacity above the dimmed menu.
            SetDimmed(false);
            SlotRenderer.DrawAt(model, openParent, model.Slots[openParent].Center, SlotVisualState.Normal, model.Decor(openParent) | SlotDecor.OpenParent, transform);
        }
    }

    /// <summary>True when every slot is <see cref="SlotKind.Empty"/> (FR-16: the empty-menu hint replaces the label plate).</summary>
    private static bool HasNoPins(WheelModel model)
    {
        SlotKind[] kinds = model.Kinds;
        for (int i = 0; i < model.SlotCount; i++)
        {
            if (kinds[i] != SlotKind.Empty)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>True while the main menu is drawn dimmed (a satellite is open).</summary>
    internal static bool Dimmed { get; private set; }

    /// <summary>Sets <c>GUI.color</c> to a §2 token, faded to <c>dimmedMenu</c> while <see cref="Dimmed"/>.</summary>
    internal static void Tint(Color color)
    {
        color.a *= _alpha;
        GUI.color = color;
    }

    private static float _alpha = 1f;

    /// <summary>Starts or ends dimmed drawing (<c>dimmedMenu</c>) for every <see cref="Tint"/> after it.</summary>
    internal static void SetDimmed(bool dimmed)
    {
        Dimmed = dimmed;
        _alpha = WheelVisuals.MenuAlpha(dimmed);
    }

    /// <summary>A static mask centred on <paramref name="centre"/> in a square of <paramref name="drawSize"/> UI px, under the current colour.</summary>
    internal static void DrawMask(MaskId id, Vec2 centre, float drawSize)
    {
        GUI.DrawTexture(Square(centre, drawSize), Textures.Mask(id));
    }

    /// <summary>A size-dependent texture centred on <paramref name="centre"/>, scaled; nothing when it is missing.</summary>
    internal static void DrawSized(in SizedTexture texture, Vec2 centre, float scale)
    {
        if (texture.Texture != null)
        {
            GUI.DrawTexture(Square(centre, texture.DrawSize * scale), texture.Texture);
        }
    }

    internal static Rect ToRect(Rect2 r) => new(r.X, r.Y, r.Width, r.Height);

    private static Rect Square(Vec2 centre, float size) => new(centre.X - (size / 2f), centre.Y - (size / 2f), size, size);

    /// <summary>The disc (gradient <c>Disc</c> mask) and the model's rim (main or satellite), under the current dimming.</summary>
    internal static void DrawDisc(WheelModel model, in RenderTransform transform)
    {
        Vec2 c = transform.Point(model.Center);
        if (Textures.Ready)
        {
            Tint(QMStyle.DiscFill);
            DrawMask(MaskId.Disc, c, transform.Length(model.DiscDrawSize));
        }

        Tint(QMStyle.White);
        DrawSized(model.Rim, c, transform.Scale);
    }
}
