namespace QuickMenu.Core.Render;

/// <summary>
/// The hub's look in use mode (Edit, pencil) and edit mode (Done, check mark) per design.md S4a. Pure, so the widths and
/// the tint are unit-tested.
/// </summary>
public static class HubStyle
{
    /// <summary>Edit mode: the fill is the state's <c>slotBg</c> with this much <c>accent</c> over it.</summary>
    public const float EditTintAlpha = 0.22f;

    /// <summary>The hub glyph: <see cref="MaskId.IconDone"/> in edit mode, <see cref="MaskId.IconEdit"/> in use mode.</summary>
    public static MaskId Glyph(bool editing) => editing ? MaskId.IconDone : MaskId.IconEdit;

    /// <summary>
    /// Stroke of the <c>accent</c> ring in UI px. Use mode: 1 normal, 2 hover and pressed. Edit mode: 2 normal, 3 hover and
    /// pressed.
    /// </summary>
    public static float RingWidth(HubPartState state, bool editing)
    {
        bool active = state != HubPartState.Normal;
        if (editing)
        {
            return active ? TextureSizing.Ring3Width : TextureSizing.Ring2Width;
        }

        return active ? TextureSizing.Ring2Width : TextureSizing.Ring1Width;
    }
}
