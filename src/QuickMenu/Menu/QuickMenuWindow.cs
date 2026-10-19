using System;
using QuickMenu.Core.Geometry;
using QuickMenu.Core.Input;
using QuickMenu.Core.Layout;
using QuickMenu.Diagnostics;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;
using Log = QuickMenu.Diagnostics.Log;

namespace QuickMenu.Menu;

/// <summary>
/// Full-screen transparent window hosting the wheel (ADR 0007). Spike S-6 settings: layer <see cref="WindowLayer.GameUI"/>,
/// no pause, no background or shadow, <c>closeOnCancel</c>, <c>absorbInputAroundWindow=false</c> (the full-screen rect
/// itself swallows map clicks). It holds no menu logic: it hit-tests, turns IMGUI events into <see cref="TriggerEvent"/>s
/// for <see cref="MenuController.Dispatch"/>, and draws. Layout and designators are resolved once per open
/// (<see cref="MenuContent"/>); OnGUI allocates nothing.
/// </summary>
internal sealed class QuickMenuWindow : Window
{
    private const int TooltipIdBase = 0x514D0000;

    internal static readonly PatchSite DrawSite = new("QuickMenuWindow.DoWindowContents");

    private readonly int _openFrame;
    private readonly Map? _map;
    private readonly MenuContent _content;
    private readonly WheelModel _model;
    private Hit _pressHit = Hit.Outside;
    private int _hoverSlot = -1;
    private bool _loggedFirstDraw;

    internal QuickMenuWindow(MenuContent content, int openFrame)
    {
        _content = content;
        _model = content.Model;
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
        TriggerBinding trigger = QuickMenuMod.Settings.Trigger;
        preventCameraMotion = trigger.Kind == TriggerBindingKind.Mouse && trigger.Code == 2; // middle mouse also drags the camera
        draggable = false;
        resizeable = false;
        focusWhenOpened = true;
        soundAppear = null;
        soundClose = null;
        if (Prefs.DevMode)
        {
            Log.Info($"Menu trigger frame={openFrame}");
        }
    }

    public override Vector2 InitialSize => new(UI.screenWidth, UI.screenHeight);

    protected override float Margin => 0f;

    protected override void SetInitialSizeAndPosition()
    {
        windowRect = new Rect(0f, 0f, UI.screenWidth, UI.screenHeight);
    }

    public override void WindowUpdate()
    {
        try
        {
            using var scope = new MeasureScope(StatId.Window);
            base.WindowUpdate();
            if (!ReferenceEquals(Find.CurrentMap, _map) || WorldRendererUtility.WorldRendered)
            {
                MenuController.Dispatch(TriggerEvent.MapLost());
            }
        }
        catch (Exception ex)
        {
            DrawSite.Fail(ex);
            SafeClose();
        }
    }

    /// <summary>Esc: one level out, decided by the state machine (it closes the menu from the main level).</summary>
    public override void OnCancelKeyPressed()
    {
        try
        {
            Event.current.Use();
            MenuController.Dispatch(TriggerEvent.Escape(MousePosition()));
        }
        catch (Exception ex)
        {
            DrawSite.Fail(ex);
            SafeClose();
        }
    }

    public override void PostClose()
    {
        try
        {
            base.PostClose();
        }
        catch (Exception ex)
        {
            DrawSite.Fail(ex);
        }
        finally
        {
            MenuController.Closed(this);
        }
    }

    public override void DoWindowContents(Rect inRect)
    {
        if (DrawSite.Disabled)
        {
            SafeClose();
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

            using var scope = new MeasureScope(StatId.Window);
            HandleEvent(Event.current);
        }
        catch (Exception ex)
        {
            DrawSite.Fail(ex);
            SafeClose();
        }
    }

    private void HandleEvent(Event ev)
    {
        Vec2 mouse = new(ev.mousePosition.x, ev.mousePosition.y);
        HitLayout layout = _model.HitLayout;
        Hit hit = HitTester.Hit(mouse, in layout);
        switch (ev.type)
        {
            case EventType.Repaint:
                MenuController.Dispatch(TriggerEvent.MouseMove(mouse));
                Paint(hit);
                break;
            case EventType.MouseDown:
                if (ev.button == 0)
                {
                    _pressHit = hit;
                }

                ev.Use();
                MenuController.Dispatch(TriggerEvent.MouseDown(mouse, hit, ev.button));
                break;
            case EventType.MouseUp:
                _pressHit = Hit.Outside;
                SlotKind kind = hit.Kind == HitKind.Slot ? _model.Kinds[hit.Index] : SlotKind.Normal;
                ev.Use();
                MenuController.Dispatch(TriggerEvent.MouseUp(mouse, hit, kind, ev.button));
                break;
        }
    }

    private void Paint(Hit hit)
    {
        bool hoverEnabled = MenuController.HoverEnabled;
        MenuRenderer.Draw(_model, hit, _pressHit, hoverEnabled);
        int slot = hoverEnabled && hit.Kind == HitKind.Slot && _model.Kinds[hit.Index] != SlotKind.Empty ? hit.Index : -1;
        if (slot != _hoverSlot)
        {
            _hoverSlot = slot;
            if (slot >= 0)
            {
                SoundDefOf.Mouseover_Command.PlayOneShotOnCamera();
            }
        }

        if (slot >= 0 && _content.Tip(slot) is { } tip)
        {
            float half = _model.ButtonSize / 2f;
            Vec2 c = _model.Slots[slot].Center;
            TooltipHandler.TipRegion(new Rect(c.X - half, c.Y - half, _model.ButtonSize, _model.ButtonSize), new TipSignal(tip, TooltipIdBase + slot));
        }
    }

    private static Vec2 MousePosition()
    {
        Vector2 p = Event.current.mousePosition;
        return new Vec2(p.x, p.y);
    }

    private static void SafeClose()
    {
        try
        {
            MenuController.Close();
        }
        catch (Exception ex)
        {
            Log.LogOnce("Menu.Close", $"Closing the menu failed: {ex}");
        }
    }
}
