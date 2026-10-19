using QuickMenu.Core.Geometry;
using QuickMenu.Core.Input;
using QuickMenu.Core.Layout;
using QuickMenu.Core.Render;
using Verse;

namespace QuickMenu.Menu;

/// <summary>
/// Everything the wheel renderer draws, built once per open (performance.md P-8/P-9: labels, label widths and draw
/// sizes are cached here, so drawing allocates nothing). Coordinates are the layout's UI px around
/// <see cref="Center"/>; <see cref="MenuRenderer"/> maps them through a <see cref="RenderTransform"/>.
/// </summary>
/// <remarks>
/// Use: construct with the placement array, call <see cref="SetSlot"/> for each filled slot (the others stay
/// <see cref="SlotKind.Empty"/>), <see cref="SetHubLabel"/>, then <see cref="Prepare"/> once. Hit-test with
/// <see cref="HitLayout"/>.
/// </remarks>
internal sealed class WheelModel
{
    private readonly Command?[] _icons;
    private readonly SlotKind[] _kinds;
    private readonly string[] _labels;
    private readonly float[] _labelWidths;

    /// <param name="center">Hub centre (after edge dodging).</param>
    /// <param name="buttonSize">B in UI px.</param>
    /// <param name="ringsInUse">Rings the disc covers (Fixed: the setting; Auto: m).</param>
    /// <param name="slots">Slot placements from <see cref="WheelLayout"/>, centred on <paramref name="center"/>.</param>
    /// <param name="slotCount">Slots used from <paramref name="slots"/>.</param>
    /// <param name="plateHeight">Label plate height: Small line height + 8 (<see cref="DefaultPlateHeight"/>).</param>
    internal WheelModel(Vec2 center, float buttonSize, int ringsInUse, SlotPlacement[] slots, int slotCount, float plateHeight)
    {
        Center = center;
        ButtonSize = buttonSize;
        RingsInUse = ringsInUse;
        DiscRadius = LayoutConstants.DiscRadius(buttonSize, ringsInUse);
        Slots = slots;
        SlotCount = slotCount < slots.Length ? slotCount : slots.Length;
        PlateHeight = plateHeight;
        HubDiameter = LayoutConstants.HubDiameter(buttonSize);
        DiscDrawSize = RoundTextureGeometry.DrawSizeForFixedMask(2f * DiscRadius, MaskSet.RoundSize);
        CircleDrawSize = RoundTextureGeometry.DrawSizeForFixedMask(buttonSize, MaskSet.RoundSize);
        HubCircleDrawSize = RoundTextureGeometry.DrawSizeForFixedMask(HubDiameter, MaskSet.RoundSize);
        _icons = new Command?[SlotCount];
        _kinds = new SlotKind[SlotCount];
        _labels = new string[SlotCount];
        _labelWidths = new float[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            _labels[i] = string.Empty;
        }
    }

    internal Vec2 Center { get; }

    internal float ButtonSize { get; }

    internal int RingsInUse { get; }

    internal float DiscRadius { get; }

    internal SlotPlacement[] Slots { get; }

    internal int SlotCount { get; }

    internal float PlateHeight { get; }

    /// <summary>H, the drawn hub (Edit button) diameter; the hit area is larger (P/2 radius).</summary>
    internal float HubDiameter { get; }

    /// <summary>Draw size of the 256 px <c>Disc</c> mask so its edge lands on 2 D.</summary>
    internal float DiscDrawSize { get; }

    /// <summary>Draw size of the 256 px <c>Circle</c> mask so its edge lands on B.</summary>
    internal float CircleDrawSize { get; }

    /// <summary>Draw size of the <c>Circle</c> mask for the hub (diameter H).</summary>
    internal float HubCircleDrawSize { get; }

    /// <summary>Glyph of the hub: the pencil in use mode (the check mark in edit mode, T-44).</summary>
    internal MaskId HubGlyph { get; set; } = MaskId.IconEdit;

    /// <summary>The hub's plate label ("Edit"; "Done" in edit mode).</summary>
    internal string HubLabel { get; private set; } = string.Empty;

    internal float HubLabelWidth { get; private set; }

    /// <summary>Slot kinds, indexed like <see cref="Slots"/> (length <see cref="SlotCount"/>).</summary>
    internal SlotKind[] Kinds => _kinds;

    /// <summary>Hit layout for <see cref="HitTester"/>; no plate (the plate is not interactive in M2).</summary>
    internal HitLayout HitLayout => new(Center, ButtonSize, DiscRadius, Slots, SlotCount);

    /// <summary>Plate height for the current Small font: line height + 8 (design.md §1.1, 30 by default).</summary>
    internal static float DefaultPlateHeight => Text.LineHeightOf(GameFont.Small) + 8f;

    internal Command? Icon(int index) => _icons[index];

    /// <summary>Stops drawing the icon of slot <paramref name="index"/> (its designator's icon code threw); the slot stays otherwise as it was.</summary>
    internal void BlankIcon(int index) => _icons[index] = null;

    internal string Label(int index) => _labels[index];

    internal float LabelWidth(int index) => _labelWidths[index];

    /// <summary>
    /// Fills slot <paramref name="index"/>. A null <paramref name="icon"/> or <see cref="SlotKind.Empty"/> keeps the
    /// slot empty (state 6; also reserved-missing pins in use mode). <paramref name="label"/> defaults to the
    /// command's <c>LabelCap</c>.
    /// </summary>
    internal void SetSlot(int index, Command? icon, SlotKind kind, string? label = null)
    {
        if (icon == null || kind == SlotKind.Empty || kind == SlotKind.Reserved)
        {
            _icons[index] = null;
            _kinds[index] = kind == SlotKind.Reserved ? SlotKind.Reserved : SlotKind.Empty;
            _labels[index] = string.Empty;
            return;
        }

        _icons[index] = icon;
        _kinds[index] = kind;
        _labels[index] = label ?? icon.LabelCap ?? string.Empty;
    }

    /// <summary>The hub's plate label ("Edit").</summary>
    internal void SetHubLabel(string label) => HubLabel = label;

    /// <summary>
    /// Truncates and measures every label in the Small font once. With <paramref name="ensureTextures"/> it also sizes
    /// the menu textures for this B and ring count (<see cref="Textures.EnsureSized"/>); the settings preview passes
    /// false. Call once per open, never per frame.
    /// </summary>
    internal void Prepare(bool ensureTextures = true)
    {
        if (ensureTextures)
        {
            Textures.EnsureSized(ButtonSize, RingsInUse);
        }

        GameFont oldFont = Text.Font;
        try
        {
            Text.Font = GameFont.Small;
            float max = WheelVisuals.PlateMaxTextWidth(DiscRadius);
            for (int i = 0; i < SlotCount; i++)
            {
                _labels[i] = Fit(_labels[i], max, out _labelWidths[i]);
            }

            HubLabel = Fit(HubLabel, max, out float hubWidth);
            HubLabelWidth = hubWidth;
        }
        finally
        {
            Text.Font = oldFont;
        }
    }

    private static string Fit(string label, float max, out float width)
    {
        if (label.Length == 0)
        {
            width = 0f;
            return label;
        }

        string fitted = label.Truncate(max);
        width = Text.CalcSize(fitted).x;
        if (width > max)
        {
            width = max;
        }

        return fitted;
    }
}
