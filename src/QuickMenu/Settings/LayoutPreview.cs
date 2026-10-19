using System;
using System.Globalization;
using QuickMenu.Core.Geometry;
using QuickMenu.Core.Input;
using QuickMenu.Core.Layout;
using QuickMenu.Core.Render;
using QuickMenu.Core.Settings;
using QuickMenu.Diagnostics;
using QuickMenu.Menu;
using QuickMenu.Pinning;
using UnityEngine;
using Verse;
using FaultPoint = QuickMenu.Core.Diagnostics.FaultPoint;

namespace QuickMenu.Settings;

/// <summary>
/// The live layout preview of the settings page (design.md S7): the menu as the current settings would draw it, blank
/// slots, at 50 % (smaller when it would not fit the area), with the "bigger than the screen" warning and the
/// "pins stay hidden" note. The model is rebuilt only when a setting that shapes it changes, never per frame; the sized
/// textures are rebuilt later, once per slider drag (<see cref="RebuildGate"/>, T-95), and the next open checks them anyway.
/// A fault disables the preview for the session (<see cref="Site"/>); the rest of the page keeps working.
/// </summary>
internal static class LayoutPreview
{
    internal static readonly PatchSite Site = new("Settings.Preview");

    private const float LineHeight = 22f;

    private static readonly Color _frameFill = new(0.07f, 0.07f, 0.07f, 0.55f);

    private static readonly RebuildGate _gate = new();
    private static int _lastDrawFrame = int.MinValue / 2;
    private static long _textureKey;
    private static ModelKey _key;
    private static bool _built;
    private static WheelModel? _model;
    private static Rect2 _bounds;
    private static float _scale;
    private static bool _fits = true;

    private static string? _warning;
    private static int _hiddenCount = -1;
    private static string _hiddenText = string.Empty;

    /// <summary>Height of the two note lines under the frame; reserved even when no note shows, so the page does not jump.</summary>
    private const float NotesHeight = (2f * LineHeight) + 4f;

    /// <summary>Draws the preview, the warning and the hidden-pins note in <paramref name="rect"/> (the frame takes what is left above the two note lines, at most 270).</summary>
    internal static void Draw(Rect rect, QuickMenuSettings settings)
    {
        if (Site.Disabled)
        {
            return;
        }

        try
        {
            FaultInjection.Check(FaultPoint.SettingsPreview);
            // The page was closed since the last frame: forget what the gate believes is built, so the first
            // frame of this opening checks the textures against the current settings (menus opened meanwhile may have resized them).
            int frame = Time.frameCount;
            if (frame - _lastDrawFrame > 1)
            {
                _gate.Reset();
            }

            _lastDrawFrame = frame;
            Refresh(settings);
            UpdateTextures();
            float frameHeight = Math.Max(0f, Math.Min(SettingsPreview.Height, rect.height - NotesHeight));
            if (frameHeight <= 0f)
            {
                return;
            }

            DrawFrame(new Rect(rect.x, rect.y, Math.Min(SettingsPreview.Width, rect.width), frameHeight));
            DrawNotes(new Rect(rect.x, rect.y + frameHeight + 4f, rect.width, LineHeight), settings);
        }
        catch (Exception ex)
        {
            Site.Fail(ex);
        }
    }

    private static void Refresh(QuickMenuSettings settings)
    {
        float buttonSize = SettingsRules.ClampButtonSize(settings.ButtonSize);
        int pins = PinService.Board.VisibleCount(PinService.Resolver);
        var key = new ModelKey(
            settings.Layout,
            settings.Sizing,
            SettingsRules.ClampRings(settings.Rings),
            SettingsRules.ClampColumns(settings.GridColumns),
            SettingsRules.ClampRows(settings.GridRows),
            buttonSize,
            pins,
            Prefs.UIScale);
        if (_built && key.Equals(_key))
        {
            return;
        }

        _key = key;
        _built = true;
        float plateHeight = WheelModel.DefaultPlateHeight;
        _bounds = SettingsPreview.Bounds(key.Layout, key.Sizing, key.Rings, key.Columns, key.Rows, buttonSize, pins, plateHeight);
        _scale = SettingsPreview.Scale(_bounds);
        _fits = SettingsPreview.FitsScreen(_bounds, new Vec2(UI.screenWidth, UI.screenHeight));
        _model = BuildModel(key, plateHeight);
    }

    /// <summary>
    /// Rebuilds the sized textures when the gate says so (T-95: once when a slider drag ends, or after a quiet period
    /// for stepper and shape changes), never per drag step. Until then the old textures are stretched in <see cref="DrawFrame"/>.
    /// </summary>
    private static void UpdateTextures()
    {
        if (_model == null)
        {
            return;
        }

        if (_gate.Observe(_textureKey, SettingsPage.ButtonSizeDragging, Environment.TickCount64))
        {
            Textures.EnsureSized(_model.ButtonSize, _model.RingsInUse);
        }
    }

    private static long PackTextureKey(float buttonSize, int rings, float uiScale) =>
        ((long)Math.Round(buttonSize * 8f) << 40) | ((long)rings << 32) | (uint)Math.Round(uiScale * 100f);

    private static WheelModel BuildModel(in ModelKey key, float plateHeight)
    {
        bool auto = key.Sizing == SizingMode.Auto;
        Vec2 centre = default;
        WheelModel model;
        if (key.Layout == MenuLayout.Wheel)
        {
            int rings = auto ? Capacity.AutoWheelRings(key.Pins) : key.Rings;
            SlotPlacement[] slots;
            if (auto)
            {
                int count = Math.Min(key.Pins, RingCapacity.Total(LayoutConstants.AutoMaxRings));
                slots = new SlotPlacement[count];
                WheelLayout.ComputeSpread(count, key.ButtonSize, centre, slots);
            }
            else
            {
                slots = WheelLayout.Compute(new WheelParams(key.ButtonSize, rings, centre));
            }

            model = new WheelModel(centre, key.ButtonSize, rings, slots, slots.Length, plateHeight);
        }
        else
        {
            int rows = auto ? Capacity.AutoGridRows(key.Pins, key.Columns) : key.Rows;
            var p = new GridParams(key.ButtonSize, key.Columns, rows, centre);
            SlotPlacement[] slots = Core.Layout.GridLayout.Compute(p);
            model = new WheelModel(centre, key.ButtonSize, 1, slots, slots.Length, plateHeight);
            model.SetPanel(Core.Layout.GridLayout.Panel(p));
            model.SetGridShape(key.Columns, rows);
        }

        model.ShowEmptyHint = false;
        model.SetHubLabels(string.Empty, string.Empty);
        model.Prepare(ensureTextures: false); // the textures follow the gate, see UpdateTextures
        _textureKey = PackTextureKey(key.ButtonSize, model.RingsInUse, key.UiScale);
        return model;
    }

    private static void DrawFrame(Rect area)
    {
        Widgets.DrawBoxSolid(area, _frameFill);

        // 1 px quiet border in slotBorder, not the white default (UC-4 review item 8): it must not compete with the preview's accent rim.
        Color color = GUI.color;
        GUI.color = QMStyle.SlotBorder;
        try
        {
            Widgets.DrawBox(area);
        }
        finally
        {
            GUI.color = color;
        }

        if (_model == null)
        {
            return;
        }

        float scale = Math.Min(_scale, Math.Min(area.width / Math.Max(_bounds.Width, 1f), area.height / Math.Max(_bounds.Height, 1f)));
        Vec2 offset = SettingsPreview.HubOffset(_bounds, scale);
        var origin = new Vec2((area.width / 2f) + offset.X, (area.height / 2f) + offset.Y);
        var transform = new RenderTransform(default, origin, scale);
        var wanted = TextureSizing.Create(_model.ButtonSize, _model.RingsInUse, Prefs.UIScale);
        TextureSizing? built = Textures.BuiltSizing;
        GUI.BeginGroup(area);
        try
        {
            // Derived from what is actually built, not from the gate: aligned while a rebuild is pending and exactly 1 after it.
            MenuRenderer.SizedStretch = TextureSizing.ButtonStretch(built, wanted);
            MenuRenderer.RimStretch = TextureSizing.RimStretch(built, wanted);
            if (_key.Layout == MenuLayout.Grid)
            {
                GridRenderer.Draw(_model, transform, Hit.Outside, Hit.Outside);
            }
            else
            {
                MenuRenderer.Draw(_model, transform, Hit.Outside, Hit.Outside);
            }
        }
        finally
        {
            MenuRenderer.SizedStretch = 1f;
            MenuRenderer.RimStretch = 1f;
            GUI.EndGroup();
        }
    }

    private static void DrawNotes(Rect row, QuickMenuSettings settings)
    {
        Text.Font = GameFont.Tiny;
        try
        {
            if (!_fits)
            {
                _warning ??= "QuickMenu.Settings.PreviewTooBig".Translate();
                GUI.color = QMStyle.Danger;
                Widgets.Label(row, _warning);
                GUI.color = Color.white;
                row.y += LineHeight;
            }

            int hidden = SettingsPreview.HiddenPins(settings.Board);
            if (hidden > 0)
            {
                if (hidden != _hiddenCount)
                {
                    _hiddenCount = hidden;
                    _hiddenText = "QuickMenu.Settings.PreviewHidden".Translate(hidden.ToString(CultureInfo.CurrentCulture));
                }

                GUI.color = QMStyle.TextMuted;
                Widgets.Label(row, _hiddenText);
                GUI.color = Color.white;
            }
        }
        finally
        {
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }
    }

    private readonly record struct ModelKey(
        MenuLayout Layout,
        SizingMode Sizing,
        int Rings,
        int Columns,
        int Rows,
        float ButtonSize,
        int Pins,
        float UiScale);
}
