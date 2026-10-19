using QuickMenu.Core.Geometry;
using QuickMenu.Core.Input;
using QuickMenu.Core.Layout;
using QuickMenu.Core.Render;
using UnityEngine;
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
/// <para>Slot states that combine with the base state (T-42, design.md S4 4, 12, 13, 14): <see cref="SetDecor"/>
/// (sub-menu badge, material dot, last-used ring). Material options show the stuff's own icon: <see cref="SetStuffOption"/>.</para>
/// <para>A satellite (sub-menu or material ring, S3) is a model of its own: centre = the satellite hub (the parent
/// slot, after its own edge dodge), slots = the option placements, rings = the satellite's m. Call
/// <see cref="PrepareSatellite"/> instead of <see cref="Prepare"/> and draw it with <see cref="SatelliteRenderer"/>.</para>
/// </remarks>
internal sealed class WheelModel
{
    private readonly Command?[] _icons;
    private readonly ThingDef?[] _stuffIcons;
    private readonly SlotDecor[] _decor;
    private readonly Color[] _dotColors;
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
        _stuffIcons = new ThingDef?[SlotCount];
        _decor = new SlotDecor[SlotCount];
        _dotColors = new Color[SlotCount];
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

    private string _editLabel = string.Empty;
    private string _doneLabel = string.Empty;
    private float _editLabelWidth;
    private float _doneLabelWidth;

    /// <summary>True in edit mode: the hub is Done (check mark, "Done" plate, accent tint). Set by the window from the state machine.</summary>
    internal bool Editing { get; set; }

    /// <summary>Glyph of the hub: the pencil in use mode, the check mark in edit mode.</summary>
    internal MaskId HubGlyph => HubStyle.Glyph(Editing);

    /// <summary>The hub's plate label ("Edit"; "Done" in edit mode).</summary>
    internal string HubLabel => Editing ? _doneLabel : _editLabel;

    internal float HubLabelWidth => Editing ? _doneLabelWidth : _editLabelWidth;

    /// <summary>Slot kinds, indexed like <see cref="Slots"/> (length <see cref="SlotCount"/>).</summary>
    internal SlotKind[] Kinds => _kinds;

    /// <summary>Hit layout for <see cref="HitTester"/>; no plate (the plate is not interactive in M2).</summary>
    internal HitLayout HitLayout => new(Center, ButtonSize, DiscRadius, Slots, SlotCount);

    /// <summary>Plate height for the current Small font: line height + 8 (design.md §1.1, 30 by default).</summary>
    internal static float DefaultPlateHeight => Text.LineHeightOf(GameFont.Small) + 8f;

    internal Command? Icon(int index) => _icons[index];

    /// <summary>The stuff whose icon a material option shows (drawn with <c>Widgets.DefIcon</c>), or null.</summary>
    internal ThingDef? StuffIcon(int index) => _stuffIcons[index];

    /// <summary>Decorations of slot <paramref name="index"/> (S4 states 4, 12, 13; 14 is added by the renderer).</summary>
    internal SlotDecor Decor(int index) => _decor[index];

    /// <summary>Material dot colour of slot <paramref name="index"/> (S4 state 13), set by <see cref="SetDecor"/>.</summary>
    internal Color MaterialDotColor(int index) => _dotColors[index];

    /// <summary>
    /// The grid panel (main grid, sub-grid or material grid; T-54), or null for a wheel. Set with <see cref="SetPanel"/>
    /// before <see cref="Prepare"/>: it moves the label plate under the panel and sizes the labels to the panel.
    /// </summary>
    internal Rect2? Panel { get; private set; }

    /// <summary>
    /// Makes this a grid model drawn by <see cref="GridRenderer"/>: <paramref name="panel"/> is the panel rect from the
    /// grid layout (main grid) or <c>PlacedSubGrid.Panel</c> (sub-grid, material grid), in the same coordinates as
    /// <see cref="Slots"/>. Once per open, before <see cref="Prepare"/>.
    /// </summary>
    internal void SetPanel(Rect2 panel) => Panel = panel;

    /// <summary>The label plate for a label of <paramref name="textWidth"/>: under the disc (wheel) or the panel (grid).</summary>
    internal Rect2 PlateRect(float textWidth) => Panel is { } panel
        ? PanelVisuals.PlateRect(panel, textWidth, PlateHeight)
        : WheelVisuals.PlateRect(Center, DiscRadius, textWidth, PlateHeight);

    /// <summary>True for a satellite model (<see cref="PrepareSatellite"/>): it draws the satellite rim.</summary>
    internal bool IsSatellite { get; private set; }

    /// <summary>The disc rim of this model: the main rim, or the satellite rim kept from <see cref="PrepareSatellite"/>.</summary>
    internal SizedTexture Rim => IsSatellite ? _satelliteRim : Textures.DiscRim;

    private SizedTexture _satelliteRim;

    /// <summary>Stops drawing the icon of slot <paramref name="index"/> (its designator's icon code threw); the slot stays otherwise as it was.</summary>
    internal void BlankIcon(int index)
    {
        _icons[index] = null;
        _stuffIcons[index] = null;
    }

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
            _stuffIcons[index] = null;
            _decor[index] = SlotDecor.None;
            _kinds[index] = kind == SlotKind.Reserved ? SlotKind.Reserved : SlotKind.Empty;
            _labels[index] = string.Empty;
            return;
        }

        _icons[index] = icon;
        _stuffIcons[index] = null;
        _kinds[index] = kind;
        _labels[index] = label ?? icon.LabelCap ?? string.Empty;
    }

    /// <summary>
    /// Shows <paramref name="material"/>'s icon in its colour in slot <paramref name="index"/> instead of the designator's own
    /// icon (FR-11a: material-variant dropdowns). The slot keeps its designator, label and state.
    /// </summary>
    internal void SetMaterialIcon(int index, ThingDef material)
    {
        _icons[index] = null;
        _stuffIcons[index] = material;
    }

    /// <summary>
    /// Fills slot <paramref name="index"/> of a material ring with <paramref name="stuff"/> (S3): its icon in its own
    /// colour, <paramref name="lastUsed"/> adds the 3 px <c>accent</c> ring (state 4). <paramref name="label"/> defaults
    /// to the stuff's <c>LabelCap</c>.
    /// </summary>
    internal void SetStuffOption(int index, ThingDef stuff, SlotKind kind, bool lastUsed, string? label = null)
    {
        _icons[index] = null;
        _stuffIcons[index] = stuff;
        _kinds[index] = kind == SlotKind.Empty || kind == SlotKind.Reserved ? SlotKind.Normal : kind;
        _labels[index] = label ?? stuff.LabelCap.ToString();
        _decor[index] = lastUsed ? _decor[index] | SlotDecor.LastUsed : _decor[index] & ~SlotDecor.LastUsed;
    }

    /// <summary>
    /// Sets the decorations of slot <paramref name="index"/>: <see cref="SlotDecor.SubMenu"/> (badge at 1:30),
    /// <see cref="SlotDecor.Material"/> (dot at 4:30 in <paramref name="lastUsedStuff"/>'s colour, or <c>textMuted</c>
    /// when null), <see cref="SlotDecor.LastUsed"/> (3 px ring). <see cref="SlotDecor.OpenParent"/> is ignored here:
    /// the renderer adds it to the open satellite's parent. Once per open, not per frame.
    /// </summary>
    internal void SetDecor(int index, SlotDecor decor, Color? lastUsedStuff = null)
    {
        _decor[index] = decor & ~SlotDecor.OpenParent;
        Rgba32? stuff = lastUsedStuff is { } c ? QMStyle.ToRgba(c) : null;
        _dotColors[index] = QMStyle.ToColor(WheelVisuals.MaterialDotColor(stuff));
    }

    /// <summary>The hub's plate labels: <paramref name="edit"/> in use mode, <paramref name="done"/> in edit mode.</summary>
    internal void SetHubLabels(string edit, string done)
    {
        _editLabel = edit;
        _doneLabel = done;
    }

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

        PrepareLabels();
    }

    /// <summary>
    /// Prepares a satellite model: labels as in <see cref="Prepare"/>, and its rim from
    /// <see cref="Textures.EnsureSatelliteRim"/> (kept for the satellite's lifetime). Call once when the satellite
    /// opens, after the main model's <see cref="Prepare"/>; never per frame. Never rebuilds the main textures.
    /// </summary>
    internal void PrepareSatellite()
    {
        IsSatellite = true;
        RefreshSatelliteRim();
        PrepareLabels();
    }

    /// <summary>
    /// Re-acquires the satellite rim. The texture cache keeps one satellite rim, so a later satellite (a material ring over
    /// this sub-ring) may have replaced and destroyed the one this model holds: call this when this model becomes the
    /// ring on screen again.
    /// </summary>
    internal void RefreshSatelliteRim()
    {
        if (IsSatellite)
        {
            _satelliteRim = Textures.EnsureSatelliteRim(RingsInUse < 1 ? 1 : RingsInUse);
        }
    }

    private void PrepareLabels()
    {
        GameFont oldFont = Text.Font;
        try
        {
            Text.Font = GameFont.Small;
            float max = Panel is { } panel ? PanelVisuals.PlateMaxTextWidth(panel) : WheelVisuals.PlateMaxTextWidth(DiscRadius);
            for (int i = 0; i < SlotCount; i++)
            {
                _labels[i] = Fit(_labels[i], max, out _labelWidths[i]);
            }

            _editLabel = Fit(_editLabel, max, out _editLabelWidth);
            _doneLabel = Fit(_doneLabel, max, out _doneLabelWidth);
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
