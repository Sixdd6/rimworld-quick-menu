using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;
using QuickMenu.Core.Render;
using UnityEngine;

namespace QuickMenu.Menu;

/// <summary>
/// Draws a wheel menu from a per-open <see cref="WheelModel"/> (design.md S1): disc + rim, slots, hub, label plate,
/// in that order. It owns no input or menu logic: the caller hit-tests (<see cref="WheelModel.HitLayout"/>) and passes
/// the hover hit, the hit where the current press began (<see cref="Hit.Outside"/> when no button is held), and
/// whether hover is enabled yet (<see cref="Core.Input.TriggerStateMachine.HoverEnabled"/>, S4a 4 px rule).
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
    internal static void Draw(WheelModel model, Hit hover, Hit pressed, bool hoverEnabled) =>
        Draw(model, RenderTransform.Identity(model.Center), hover, pressed, hoverEnabled);

    /// <summary>Draws the model with its hub at <c>transform.Origin</c>, scaled by <c>transform.Scale</c>.</summary>
    internal static void Draw(WheelModel model, in RenderTransform transform, Hit hover, Hit pressed, bool hoverEnabled)
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        Color old = GUI.color;
        try
        {
            DrawDisc(model, transform);
            for (int i = 0; i < model.SlotCount; i++)
            {
                SlotRenderer.Draw(model, i, WheelVisuals.Slot(i, model.Kinds[i], hover, pressed), transform);
            }

            HubRenderer.Draw(
                model,
                transform,
                WheelVisuals.HubMain(hover, pressed, hoverEnabled),
                model.HasHubSub ? WheelVisuals.HubSub(hover, pressed, hoverEnabled) : HubPartState.Normal);
            PlateRenderer.Draw(model, transform, WheelVisuals.Plate(hover, hoverEnabled, model.HasHubSub, model.Kinds, model.SlotCount), hover.Index);
        }
        finally
        {
            GUI.color = old;
        }
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

    private static void DrawDisc(WheelModel model, in RenderTransform transform)
    {
        Vec2 c = transform.Point(model.Center);
        if (Textures.Ready)
        {
            GUI.color = QMStyle.DiscFill;
            DrawMask(MaskId.Disc, c, transform.Length(model.DiscDrawSize));
        }

        GUI.color = QMStyle.White;
        DrawSized(Textures.DiscRim, c, transform.Scale);
    }
}
