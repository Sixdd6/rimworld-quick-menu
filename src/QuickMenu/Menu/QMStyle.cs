using System;
using QuickMenu.Core.Render;
using UnityEngine;
using Verse;
using Log = QuickMenu.Diagnostics.Log;

namespace QuickMenu.Menu;

/// <summary>
/// The colour tokens of design.md §2 as Unity colours. The values live in <see cref="StyleTokens"/> (Core, unit-tested);
/// drawing code takes every colour from here and uses no colour literals.
/// </summary>
internal static class QMStyle
{
    internal static readonly Color Accent = ToColor(StyleTokens.Accent);

    /// <summary>Tint for the <c>Disc</c> mask: black α 0.85 at the centre, falling to 0.55 at the edge through the mask.</summary>
    internal static readonly Color DiscFill = ToColor(StyleTokens.DiscCentre);

    /// <summary>The edit-mode hub tint (S4a): <c>accent</c> at <see cref="HubStyle.EditTintAlpha"/>, drawn over the hub fill.</summary>
    internal static readonly Color EditTint = new(Accent.r, Accent.g, Accent.b, HubStyle.EditTintAlpha);

    /// <summary>The drop-target tint (S4 state 11): <c>accent</c> at <see cref="EditVisuals.DropTargetTintAlpha"/>, over <c>slotBgHover</c>.</summary>
    internal static readonly Color DropTargetTint = new(Accent.r, Accent.g, Accent.b, EditVisuals.DropTargetTintAlpha);

    internal static readonly Color RimOutline = ToColor(StyleTokens.RimOutline);
    internal static readonly Color SlotBg = ToColor(StyleTokens.SlotBg);
    internal static readonly Color SlotBgHover = ToColor(StyleTokens.SlotBgHover);
    internal static readonly Color SlotBgPressed = ToColor(StyleTokens.SlotBgPressed);
    internal static readonly Color SlotBorder = ToColor(StyleTokens.SlotBorder);

    /// <summary>Empty slot fill (S4 state 6): <c>slotBg</c> at 50 %.</summary>
    internal static readonly Color SlotBgEmpty = ToColor(StyleTokens.SlotBgEmpty);

    /// <summary>Empty slot ring in use mode (S4 state 6): <c>slotBorder</c> at 70 %.</summary>
    internal static readonly Color SlotBorderEmpty = ToColor(StyleTokens.SlotBorderEmpty);

    /// <summary>The 8 % white overlay of a hovered slot (S4 state 2).</summary>
    internal static readonly Color HoverOverlay = ToColor(StyleTokens.HoverOverlay);

    internal static readonly Color EmptyBorder = ToColor(StyleTokens.EmptyBorder);
    internal static readonly Color Text = ToColor(StyleTokens.Text);
    internal static readonly Color TextMuted = ToColor(StyleTokens.TextMuted);
    internal static readonly Color UnavailableIcon = ToColor(StyleTokens.UnavailableIcon);
    internal static readonly Color Plate = ToColor(StyleTokens.Plate);

    /// <summary>Architect pin marker, full and entry (design.md §2 "Architect pin marker"): <c>accent</c>.</summary>
    internal static readonly Color PinMarker = ToColor(StyleTokens.PinMarker);

    /// <summary>Partial pin marker (only single entries of a dropdown pinned): white <c>#FFFFFF</c> (user decision 14).</summary>
    internal static readonly Color PinMarkerPartial = ToColor(StyleTokens.PinMarkerPartial);

    /// <summary>Outline mask under the pin marker: black α 0.85.</summary>
    internal static readonly Color PinMarkerOutline = ToColor(StyleTokens.PinMarkerOutline);

    /// <summary>Untinted drawing: textures with baked colour (the disc rim) and designator icons.</summary>
    internal static readonly Color White = ToColor(StyleTokens.White);

    /// <summary><c>danger</c>: our red, or the game's <c>ColorLibrary.RedReadable</c> under <see cref="StyleTokens.ChooseDanger"/>.</summary>
    internal static readonly Color Danger = ResolveDanger();

    /// <summary><c>dimmedMenu</c>: alpha of the main menu behind a satellite.</summary>
    internal const float DimmedMenuAlpha = StyleTokens.DimmedMenuAlpha;

    internal static Color ToColor(Rgba32 c) => new(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);

    internal static Rgba32 ToRgba(Color c) => Rgba32.FromFloats(c.r, c.g, c.b, c.a);

    private static Color ResolveDanger()
    {
        try
        {
            return ToColor(StyleTokens.ChooseDanger(ToRgba(ColorLibrary.RedReadable), ToRgba(Widgets.WindowBGFillColor)));
        }
        catch (Exception ex)
        {
            Log.WarnOnce("QMStyle.Danger", $"Could not read the game's colours, the mod's own danger red is used: {ex.GetType().Name}");
            return ToColor(StyleTokens.Danger);
        }
    }
}
