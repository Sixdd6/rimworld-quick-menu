using System;
using QuickMenu.Core.Layout;

namespace QuickMenu.Core.Render;

/// <summary>Groups of size-dependent textures, rebuilt together (T-28).</summary>
[Flags]
public enum SizedGroups
{
    /// <summary>Nothing to rebuild.</summary>
    None = 0,

    /// <summary>Slot rings (1, 2, 3 px and dashed) at the slot diameter B.</summary>
    SlotRings = 1,

    /// <summary>Hub sub-button rings (1 and 2 px) at its diameter e.</summary>
    HubRings = 2,

    /// <summary>The disc rim at the disc diameter 2 D.</summary>
    Rim = 4,

    /// <summary>Badge and hub-icon glyphs at their drawn sizes.</summary>
    Glyphs = 8,

    /// <summary>Every group.</summary>
    All = SlotRings | HubRings | Rim | Glyphs,
}

/// <summary>
/// The inputs that decide the size of the size-dependent textures: button size B, rings in use m and the game's UI
/// scale. Masks drawn at a small size (rings, badges) or a very large one (the rim) are rasterized at their actual
/// size in screen pixels, so the stroke widths of design.md §2 stay exact. Compare two values with
/// <see cref="Changed"/> on each open; nothing is rebuilt per frame.
/// </summary>
/// <param name="ButtonSize">B in UI px.</param>
/// <param name="RingsInUse">m, the rings the disc covers (0 = hub only).</param>
/// <param name="UiScale">The game's UI scale (screen px per UI px).</param>
public readonly record struct TextureSizing(float ButtonSize, int RingsInUse, float UiScale)
{
    /// <summary>Slot ring widths in UI px (design.md §2 ring widths: normal 1, hover 2, insert 3).</summary>
    public const float Ring1Width = 1f;

    /// <inheritdoc cref="Ring1Width"/>
    public const float Ring2Width = 2f;

    /// <inheritdoc cref="Ring1Width"/>
    public const float Ring3Width = 3f;

    /// <summary>Validated constructor.</summary>
    /// <exception cref="ArgumentOutOfRangeException">An input is outside its range.</exception>
    public static TextureSizing Create(float buttonSize, int ringsInUse, float uiScale)
    {
        if (!(buttonSize > 0f) || float.IsInfinity(buttonSize))
        {
            throw new ArgumentOutOfRangeException(nameof(buttonSize), buttonSize, "Button size must be positive and finite.");
        }

        if (ringsInUse < 0 || ringsInUse > LayoutConstants.MaxRings)
        {
            throw new ArgumentOutOfRangeException(nameof(ringsInUse), ringsInUse, "Rings in use must be 0…MaxRings.");
        }

        if (!(uiScale > 0f) || float.IsInfinity(uiScale))
        {
            throw new ArgumentOutOfRangeException(nameof(uiScale), uiScale, "UI scale must be positive and finite.");
        }

        return new TextureSizing(buttonSize, ringsInUse, uiScale);
    }

    /// <summary>Hub sub-button diameter e.</summary>
    public float HubSubDiameter => LayoutConstants.HubSubDiameter(ButtonSize);

    /// <summary>Disc diameter 2 D.</summary>
    public float DiscDiameter => 2f * LayoutConstants.DiscRadius(ButtonSize, RingsInUse);

    /// <summary>Geometry of the slot ring textures.</summary>
    public RoundTextureGeometry SlotRing => RoundTextureGeometry.For(ButtonSize, UiScale);

    /// <summary>Geometry of the hub sub-button ring textures.</summary>
    public RoundTextureGeometry HubRing => RoundTextureGeometry.For(HubSubDiameter, UiScale);

    /// <summary>Geometry of the disc rim texture.</summary>
    public RoundTextureGeometry Rim => RoundTextureGeometry.For(DiscDiameter, UiScale);

    /// <summary>
    /// Drawn size in UI px of glyph <paramref name="id"/> (design.md §2 badges, S4a): sub-menu 0.22 B, caret 0.2 B,
    /// lock 0.25 B, hub × and ✓ 0.22 B, hub pencil and + 0.6 e.
    /// </summary>
    public float GlyphDrawSize(MaskId id) => id switch
    {
        MaskId.SubMenuBadge => 0.22f * ButtonSize,
        MaskId.Caret => 0.2f * ButtonSize,
        MaskId.Lock => 0.25f * ButtonSize,
        MaskId.IconClose or MaskId.IconDone => 0.22f * ButtonSize,
        MaskId.IconEdit or MaskId.IconAdd => 0.6f * HubSubDiameter,
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Not a glyph mask."),
    };

    /// <summary>Texture side for glyph <paramref name="id"/>: its drawn size in screen px, at least <see cref="Glyphs.MinSize"/>.</summary>
    public int GlyphTexels(MaskId id) =>
        Math.Max(Glyphs.MinSize, (int)MathF.Round(GlyphDrawSize(id) * UiScale, MidpointRounding.AwayFromZero));

    /// <summary>Which groups must be rebuilt going from <paramref name="previous"/> (null = nothing built) to <paramref name="next"/>.</summary>
    public static SizedGroups Changed(TextureSizing? previous, TextureSizing next)
    {
        if (previous is not { } old || old.UiScale != next.UiScale)
        {
            return SizedGroups.All;
        }

        SizedGroups groups = SizedGroups.None;
        if (old.ButtonSize != next.ButtonSize)
        {
            groups |= SizedGroups.SlotRings | SizedGroups.Glyphs;
        }

        if (old.HubSubDiameter != next.HubSubDiameter)
        {
            groups |= SizedGroups.HubRings;
        }

        if (old.DiscDiameter != next.DiscDiameter)
        {
            groups |= SizedGroups.Rim;
        }

        return groups;
    }
}
