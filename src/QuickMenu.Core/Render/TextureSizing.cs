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

    /// <summary>Hub rings (1 and 2 px) at the hub diameter H.</summary>
    HubRings = 2,

    /// <summary>The disc rim at the disc diameter 2 D.</summary>
    Rim = 4,

    /// <summary>Badge and hub-icon glyphs and the material dot at their drawn sizes.</summary>
    Glyphs = 8,

    /// <summary>Every group.</summary>
    All = SlotRings | HubRings | Rim | Glyphs,
}

/// <summary>Where a satellite's disc rim comes from (<see cref="TextureSizing.ChooseSatelliteRim"/>).</summary>
public enum SatelliteRimSource
{
    /// <summary>The satellite disc has the main disc's size: draw the main rim.</summary>
    Main,

    /// <summary>The cached satellite rim already has the wanted size.</summary>
    Cached,

    /// <summary>Rasterize the satellite rim again (replacing the cached one).</summary>
    Rebuild,
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

    /// <summary>
    /// Largest accepted rings in use: the larger of the Fixed setting limit (<see cref="LayoutConstants.MaxRings"/>)
    /// and the Auto growth limit (<see cref="LayoutConstants.AutoMaxRings"/>).
    /// </summary>
    public const int MaxRingsInUse = LayoutConstants.AutoMaxRings > LayoutConstants.MaxRings
        ? LayoutConstants.AutoMaxRings
        : LayoutConstants.MaxRings;

    /// <summary>Validated constructor.</summary>
    /// <exception cref="ArgumentOutOfRangeException">An input is outside its range.</exception>
    public static TextureSizing Create(float buttonSize, int ringsInUse, float uiScale)
    {
        if (!(buttonSize > 0f) || float.IsInfinity(buttonSize))
        {
            throw new ArgumentOutOfRangeException(nameof(buttonSize), buttonSize, "Button size must be positive and finite.");
        }

        if (ringsInUse < 0 || ringsInUse > MaxRingsInUse)
        {
            throw new ArgumentOutOfRangeException(nameof(ringsInUse), ringsInUse, "Rings in use must be 0…MaxRingsInUse.");
        }

        if (!(uiScale > 0f) || float.IsInfinity(uiScale))
        {
            throw new ArgumentOutOfRangeException(nameof(uiScale), uiScale, "UI scale must be positive and finite.");
        }

        return new TextureSizing(buttonSize, ringsInUse, uiScale);
    }

    /// <summary>Hub (Edit button) diameter H.</summary>
    public float HubDiameter => LayoutConstants.HubDiameter(ButtonSize);

    /// <summary>Disc diameter 2 D.</summary>
    public float DiscDiameter => 2f * LayoutConstants.DiscRadius(ButtonSize, RingsInUse);

    /// <summary>Geometry of the slot ring textures.</summary>
    public RoundTextureGeometry SlotRing => RoundTextureGeometry.For(ButtonSize, UiScale);

    /// <summary>Geometry of the hub ring textures, rasterized at H.</summary>
    public RoundTextureGeometry HubRing => RoundTextureGeometry.For(HubDiameter, UiScale);

    /// <summary>Geometry of the disc rim texture.</summary>
    public RoundTextureGeometry Rim => RoundTextureGeometry.For(DiscDiameter, UiScale);

    /// <summary>
    /// Geometry of the material dot (design.md S4 state 13, 0.18 B), a filled circle rasterized at its drawn size so it
    /// stays round at 7–17 px. Part of the <see cref="SizedGroups.Glyphs"/> group (it depends only on B and UI scale).
    /// </summary>
    public RoundTextureGeometry MaterialDot => RoundTextureGeometry.For(WheelVisuals.MaterialDotSize(ButtonSize), UiScale);

    /// <summary>
    /// Geometry of the rim of a satellite (sub-menu or material ring) with <paramref name="rings"/> rings around a
    /// parent slot: disc <c>D_s = m·P + B/2 + rimPad</c> (design.md §1.1). Not limited by the ring settings, because
    /// the option count decides it; very large discs are capped by <see cref="RoundTextureGeometry.MaxTextureSize"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="rings"/> is below 1.</exception>
    public RoundTextureGeometry SatelliteRim(int rings)
    {
        if (rings < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(rings), rings, "A satellite has at least one ring.");
        }

        return RoundTextureGeometry.For(2f * LayoutConstants.DiscRadius(ButtonSize, rings), UiScale);
    }

    /// <summary>
    /// The rim cache policy: the main rim plus one satellite slot. A satellite whose disc matches the main disc
    /// draws the main rim; otherwise the satellite slot is reused when it has the wanted size and rebuilt when not.
    /// So opening satellites never rebuilds the main rim, and repeated opens of one satellite size rebuild nothing.
    /// </summary>
    /// <param name="main">Geometry of the built main rim, or null when none is built.</param>
    /// <param name="cached">Geometry of the cached satellite rim, or null when none is built.</param>
    /// <param name="wanted">Geometry the satellite needs (<see cref="SatelliteRim"/>).</param>
    public static SatelliteRimSource ChooseSatelliteRim(RoundTextureGeometry? main, RoundTextureGeometry? cached, RoundTextureGeometry wanted)
    {
        if (main == wanted)
        {
            return SatelliteRimSource.Main;
        }

        return cached == wanted ? SatelliteRimSource.Cached : SatelliteRimSource.Rebuild;
    }

    /// <summary>
    /// Drawn size in UI px of glyph <paramref name="id"/> (design.md §2 badges, S4a): sub-menu 0.22 B, caret 0.2 B,
    /// lock 0.25 B, hub pencil and ✓ Hg = round(0.5 H), edit-mode + 0.3 B.
    /// </summary>
    public float GlyphDrawSize(MaskId id) => id switch
    {
        MaskId.SubMenuBadge => 0.22f * ButtonSize,
        MaskId.Caret => 0.2f * ButtonSize,
        MaskId.Lock => 0.25f * ButtonSize,
        MaskId.IconEdit or MaskId.IconDone => LayoutConstants.HubGlyphSize(ButtonSize),
        MaskId.IconAdd => 0.3f * ButtonSize,
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

        if (old.HubDiameter != next.HubDiameter)
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
