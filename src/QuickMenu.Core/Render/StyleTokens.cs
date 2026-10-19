namespace QuickMenu.Core.Render;

/// <summary>
/// The colour tokens of design.md §2 with their exact values. The adapter's <c>QMStyle</c> converts them to Unity
/// colours; no drawing code uses colour literals. Keeping the values here lets the contrast claims of design.md §2 be
/// checked by unit tests.
/// </summary>
public static class StyleTokens
{
    /// <summary>Rim, hover/insert/target rings, Pinned pill, selected segment: <c>#38BDF8</c>.</summary>
    public static readonly Rgba32 Accent = Rgba32.FromHex(0x38BDF8);

    /// <summary>Disc and panel fill at the centre: black α 0.85 (the <c>Disc</c> mask carries the fall-off).</summary>
    public static readonly Rgba32 DiscCentre = Rgba32.FromHex(0x000000, 0.85f);

    /// <summary>Disc and panel fill at the edge: black α 0.55.</summary>
    public static readonly Rgba32 DiscEdge = Rgba32.FromHex(0x000000, 0.55f);

    /// <summary>1 px outline on each side of the 2 px rim: black α 0.60.</summary>
    public static readonly Rgba32 RimOutline = Rgba32.FromHex(0x000000, 0.60f);

    /// <summary>Slot fill, picker icon tile: <c>#1E2328</c> α 0.92.</summary>
    public static readonly Rgba32 SlotBg = Rgba32.FromHex(0x1E2328, 0.92f);

    /// <summary>Empty slot fill (S4 state 6, T-81): <see cref="SlotBg"/>'s colour at α 0.50.</summary>
    public static readonly Rgba32 SlotBgEmpty = Rgba32.FromHex(0x1E2328, 0.50f);

    /// <summary>Empty slot ring in use mode (S4 state 6, T-81): <see cref="SlotBorder"/>'s colour at α 0.70.</summary>
    public static readonly Rgba32 SlotBorderEmpty = Rgba32.FromHex(0x56626E, 0.70f);

    /// <summary>The 8 % white overlay of a hovered slot (S4 state 2).</summary>
    public static readonly Rgba32 HoverOverlay = Rgba32.FromHex(0xFFFFFF, 0.08f);

    /// <summary>Hover, drop target: <c>#2A3540</c>.</summary>
    public static readonly Rgba32 SlotBgHover = Rgba32.FromHex(0x2A3540);

    /// <summary>Pressed: <c>#15191D</c>.</summary>
    public static readonly Rgba32 SlotBgPressed = Rgba32.FromHex(0x15191D);

    /// <summary>Normal slot ring (decorative): <c>#56626E</c>.</summary>
    public static readonly Rgba32 SlotBorder = Rgba32.FromHex(0x56626E);

    /// <summary>Empty, dashed and reserved slots: <c>#8A96A0</c>.</summary>
    public static readonly Rgba32 EmptyBorder = Rgba32.FromHex(0x8A96A0);

    /// <summary>Labels, plates: <c>#FFFFFF</c>.</summary>
    public static readonly Rgba32 Text = Rgba32.FromHex(0xFFFFFF);

    /// <summary>Secondary text, lock badge, headers: <c>#A7B0B8</c>.</summary>
    public static readonly Rgba32 TextMuted = Rgba32.FromHex(0xA7B0B8);

    /// <summary>Our danger red (reasons, warnings, full): <c>#FF7A70</c>. See <see cref="ChooseDanger"/>.</summary>
    public static readonly Rgba32 Danger = Rgba32.FromHex(0xFF7A70);

    /// <summary>Greyed icons: <c>#808080</c> α 0.75, multiplied into the icon.</summary>
    public static readonly Rgba32 UnavailableIcon = Rgba32.FromHex(0x808080, 0.75f);

    /// <summary>Label, hint and help plates: black α 0.80.</summary>
    public static readonly Rgba32 Plate = Rgba32.FromHex(0x000000, 0.80f);

    /// <summary>The game's window fill (<c>Widgets.WindowBGFillColor</c>, 1.7: 21, 25, 29), used for contrast checks.</summary>
    public static readonly Rgba32 WindowBg = Rgba32.FromHex(0x15191D);

    /// <summary>
    /// Architect pin marker, full (design.md §2 "Architect pin marker"): the <see cref="Accent"/> colour, so the marker
    /// reads as "in the Quick Menu". 4.7:1 on the lightest pixel of the vanilla button background under the marker.
    /// </summary>
    public static readonly Rgba32 PinMarker = Accent;

    /// <summary>
    /// Partial pin marker (a dropdown with only single entries pinned, user decision 11): a dimmer, opaque accent
    /// <c>#2E9ACB</c>. Opaque so its contrast does not depend on what is under it: 3.2:1 on the lightest button pixel.
    /// </summary>
    public static readonly Rgba32 PinMarkerPartial = Rgba32.FromHex(0x2E9ACB);

    /// <summary>
    /// Outline under the pin marker, 1 UI px on every side: black α 0.85. Stronger than <see cref="RimOutline"/> (α 0.60)
    /// because the marker overlaps the designator icon, which can be white: over white the outline is <c>#262626</c>, and
    /// the marker colours keep ≥ 4.7:1 against it (with α 0.60 the full marker would reach only 2.7:1).
    /// </summary>
    public static readonly Rgba32 PinMarkerOutline = Rgba32.FromHex(0x000000, 0.85f);

    /// <summary>
    /// Reference only, not drawn: the lightest pixel of the vanilla architect button background (<c>UI/Widgets/DesButBG</c>,
    /// 1.7.4982) in the marker's area <c>(xMax − 17 … xMax − 1, y + 18 … y + 34)</c>, for the pin-marker contrast checks.
    /// </summary>
    public static readonly Rgba32 GizmoBgLight = Rgba32.FromHex(0x2F4453);

    /// <summary>Reference only, not drawn: the darkest pixel of the vanilla button background in the marker's area.</summary>
    public static readonly Rgba32 GizmoBgDark = Rgba32.FromHex(0x1D2B33);

    /// <summary>Untinted drawing (masks with baked colour, designator icons).</summary>
    public static readonly Rgba32 White = Rgba32.FromHex(0xFFFFFF);

    /// <summary><c>dimmedMenu</c>: the main menu behind a satellite is drawn at this alpha.</summary>
    public const float DimmedMenuAlpha = 0.45f;

    /// <summary>
    /// Greyed slot (S4 state 5): a <c>slotBg</c> veil of this alpha over the icon. <c>Command.DrawIcon</c> sets its own
    /// colour, so <see cref="UnavailableIcon"/> cannot be multiplied in; the veil gives the same dim, desaturated look.
    /// </summary>
    public const float UnavailableVeilAlpha = 0.6f;

    /// <summary>
    /// The <c>danger</c> rule of design.md §2: use the game's red (<c>ColorLibrary.RedReadable</c>) for consistency
    /// only when it is "within 3:1" of our value, read as a luminance contrast between the two reds of at most 3:1,
    /// <b>and</b> it meets the WCAG text minimum (4.5:1) on every background danger text can sit on:
    /// <see cref="SlotBg"/> (composited over black), <see cref="SlotBgHover"/> and <paramref name="windowBg"/>.
    /// Otherwise <see cref="Danger"/> is used. On 1.7.4982 the game's red (#FF3333) reaches only 3.4:1 on
    /// <see cref="SlotBgHover"/>, so <see cref="Danger"/> is used.
    /// </summary>
    /// <param name="gameRed">The game's readable red (its alpha is ignored).</param>
    /// <param name="windowBg">The game's window fill.</param>
    /// <returns>The colour to use for <c>danger</c> (opaque).</returns>
    public static Rgba32 ChooseDanger(Rgba32 gameRed, Rgba32 windowBg)
    {
        Rgba32 black = Rgba32.FromHex(0x000000);
        Rgba32 red = new(gameRed.R, gameRed.G, gameRed.B, 255);
        bool close = ColorContrast.Ratio(red, Danger) <= ColorContrast.ComponentMinimum;
        bool readable = ColorContrast.Ratio(red, ColorContrast.Over(SlotBg, black)) >= ColorContrast.TextMinimum
            && ColorContrast.Ratio(red, SlotBgHover) >= ColorContrast.TextMinimum
            && ColorContrast.Ratio(red, ColorContrast.Over(windowBg, black)) >= ColorContrast.TextMinimum;
        return close && readable ? red : Danger;
    }
}
