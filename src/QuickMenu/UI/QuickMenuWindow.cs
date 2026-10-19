using System;
using QuickMenu.Catalog;
using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;
using QuickMenu.Core.Pins;
using QuickMenu.Diagnostics;
using QuickMenu.Diagnostics.Spike;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Log = QuickMenu.Diagnostics.Log;

namespace QuickMenu.Menu;

/// <summary>
/// Full-screen transparent window hosting the wheel (ADR 0007). Spike S-6 settings: layer <see cref="WindowLayer.GameUI"/>,
/// no pause, no background or shadow, <c>closeOnCancel</c>, <c>absorbInputAroundWindow=false</c> (the full-screen rect
/// itself swallows map clicks). Layout and designators are resolved once per open; OnGUI allocates nothing.
/// </summary>
internal sealed class QuickMenuWindow : Window
{
    private const float ButtonSize = 56f;
    private const float IconScale = 0.75f;

    internal static readonly PatchSite DrawSite = new("QuickMenuWindow.DoWindowContents");

    private static readonly Color _slotColor = new(0.12f, 0.12f, 0.12f, 0.85f);
    private static readonly Color _slotHoverColor = new(0.30f, 0.30f, 0.30f, 0.95f);
    private static readonly Color _hubColor = new(0f, 0f, 0f, 0.4f);

    private readonly int _openFrame;
    private readonly Map? _map;
    private readonly SlotPlacement[] _slots;
    private readonly Designator?[] _designators;
    private readonly int _count;
    private readonly Rect _hubRect;
    private bool _loggedFirstDraw;

    internal QuickMenuWindow(int openFrame)
    {
        _openFrame = openFrame;
        _map = Find.CurrentMap;
        layer = WindowLayer.GameUI;
        forcePause = false;
        doWindowBackground = false;
        drawShadow = false;
        doCloseX = false;
        doCloseButton = false;
        closeOnCancel = true;
        closeOnAccept = false;
        closeOnClickedOutside = false;
        absorbInputAroundWindow = false;
        preventCameraMotion = false;
        draggable = false;
        resizeable = false;
        focusWhenOpened = true;
        soundAppear = null;
        soundClose = null;
        if (Prefs.DevMode)
        {
            Log.Info($"Menu trigger frame={openFrame}");
        }

        Vector2 mouse = UI.MousePositionOnUIInverted;
        float x = Mathf.Clamp(mouse.x, LayoutConstants.DiscRadius(ButtonSize, 1), UI.screenWidth - LayoutConstants.DiscRadius(ButtonSize, 1));
        float y = Mathf.Clamp(mouse.y, LayoutConstants.DiscRadius(ButtonSize, 1), UI.screenHeight - LayoutConstants.DiscRadius(ButtonSize, 1));
        SlotPlacement[] wheel = WheelLayout.Compute(new WheelParams(ButtonSize, 1, new Vec2(x, y)));
        PinKey[] pins = SkeletonPins.Create();
        _slots = wheel;
        _designators = new Designator?[wheel.Length];
        for (int i = 0; i < pins.Length && i < wheel.Length; i++)
        {
            _designators[i] = DesignatorCatalog.Resolve(pins[i])?.Designator;
        }

        _count = pins.Length < wheel.Length ? pins.Length : wheel.Length;
        _hubRect = new Rect(x - (ButtonSize / 2f), y - (ButtonSize / 2f), ButtonSize, ButtonSize);
    }

    public override Vector2 InitialSize => new(UI.screenWidth, UI.screenHeight);

    protected override float Margin => 0f;

    protected override void SetInitialSizeAndPosition()
    {
        windowRect = new Rect(0f, 0f, UI.screenWidth, UI.screenHeight);
    }

    public override void WindowUpdate()
    {
        base.WindowUpdate();
        if (!ReferenceEquals(Find.CurrentMap, _map) || WorldRendererUtility.WorldRendered)
        {
            MenuController.Close();
        }
    }

    public override void PostClose()
    {
        base.PostClose();
        MenuController.Closed(this);
    }

    public override void DoWindowContents(Rect inRect)
    {
        if (DrawSite.Disabled)
        {
            MenuController.Close();
            return;
        }

        try
        {
            if (!_loggedFirstDraw)
            {
                _loggedFirstDraw = true;
                if (Prefs.DevMode)
                {
                    Log.Info($"Menu first DoWindowContents frame={Time.frameCount} (trigger frame={_openFrame}, same={Time.frameCount == _openFrame})");
                }
            }

            Draw();
        }
        catch (Exception ex)
        {
            DrawSite.Fail(ex);
            MenuController.Close();
        }
    }

    private void Draw()
    {
        Event ev = Event.current;
        Vector2 mouse = ev.mousePosition;
        int hover = WheelHitTest.SlotAt(_slots, _count, new Vec2(mouse.x, mouse.y));

        if (ev.type == EventType.Repaint)
        {
            Color old = GUI.color;
            GUI.color = _hubColor;
            GUI.DrawTexture(_hubRect, BaseContent.WhiteTex);
            for (int i = 0; i < _count; i++)
            {
                Rect box = SlotRect(i, 1f);
                GUI.color = i == hover ? _slotHoverColor : _slotColor;
                GUI.DrawTexture(box, BaseContent.WhiteTex);
                GUI.color = old;
                Designator? d = _designators[i];
                if (d != null)
                {
                    DrawDesignatorIcon(d, SlotRect(i, IconScale));
                }
            }

            GUI.color = old;
        }
        else if (ev.type == EventType.MouseDown)
        {
            if (hover >= 0 && ev.button == 0)
            {
                Designator? d = _designators[hover];
                if (d != null)
                {
                    d.ProcessInput(ev);
                }
            }

            ev.Use();
            MenuController.Close();
        }
    }

    private Rect SlotRect(int index, float scale)
    {
        float size = ButtonSize * scale;
        Vec2 c = _slots[index].Center;
        return new Rect(c.X - (size / 2f), c.Y - (size / 2f), size, size);
    }

    private static void DrawDesignatorIcon(Designator designator, Rect rect)
    {
        designator.DrawIcon(rect, null, default);
    }
}
