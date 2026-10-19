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
    private readonly Vec2 _openMouse;
    private MenuContent? _content;
    private WheelModel? _model;
    private Hit _pressHit = Hit.Outside;
    private int _hoverSlot = -1;
    private bool _loggedFirstDraw;

    /// <summary>The content is built on the first <see cref="DoWindowContents"/> (inside OnGUI), around <paramref name="openMouse"/>.</summary>
    internal QuickMenuWindow(Vec2 openMouse, int openFrame)
    {
        _openMouse = openMouse;
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
            if (!MenuController.Owns(this))
            {
                MenuController.CloseWindow(this);
                return;
            }

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
            if (!MenuController.Owns(this))
            {
                MenuController.CloseWindow(this);
                return;
            }

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
        if (DrawSite.Disabled || !MenuController.Owns(this))
        {
            MenuController.CloseWindow(this);
            return;
        }

        try
        {
            if (_model == null && !BuildContent())
            {
                return;
            }

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

    /// <summary>Builds the content on the first draw. A failure (a foreign designator, a font) skips this open and does not disable the menu.</summary>
    private bool BuildContent()
    {
        try
        {
            _content = MenuController.BuildFor(this, _openMouse);
        }
        catch (Exception ex)
        {
            Log.LogOnce("Menu.Build", $"Building the menu content failed, this open is skipped: {ex}");
        }

        if (_content == null)
        {
            SafeClose();
            return false;
        }

        _model = _content.Model;
        return true;
    }

    private void HandleEvent(Event ev)
    {
        WheelModel model = _model!;
        Vec2 mouse = new(ev.mousePosition.x, ev.mousePosition.y);
        HitLayout layout = model.HitLayout;
        Hit hit = HitTester.Hit(mouse, in layout);
        switch (ev.type)
        {
            case EventType.Repaint:
                if (_pressHit.Kind != HitKind.Outside && !Input.GetMouseButton(0))
                {
                    _pressHit = Hit.Outside; // released outside our window (MouseUp never arrived)
                }

                MenuController.Dispatch(TriggerEvent.MouseMove(mouse));
                Paint(model, hit);
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
                SlotKind kind = hit.Kind == HitKind.Slot ? model.Kinds[hit.Index] : SlotKind.Normal;
                bool modifier = ev.button == 0 && ModifierPolicy.IsHeld(QuickMenuMod.EffectiveModifier, ev.control || ev.command, ev.alt, ev.shift);
                ev.Use();
                MenuController.Dispatch(TriggerEvent.MouseUp(mouse, hit, kind, ev.button, modifier));
                break;
        }
    }

    /// <summary>Shows the pins as they are now, in place (after an unpin). A failed rebuild closes the menu.</summary>
    internal void Refresh()
    {
        MenuContent? content = null;
        try
        {
            content = MenuController.Rebuild(this, _openMouse);
        }
        catch (Exception ex)
        {
            Log.LogOnce("Menu.Refresh", $"Refreshing the menu content failed, the menu is closed: {ex}");
        }

        if (content == null)
        {
            SafeClose();
            return;
        }

        _content = content;
        _model = content.Model;
        _hoverSlot = -1;
    }

    private void Paint(WheelModel model, Hit hit)
    {
        bool hoverEnabled = MenuController.HoverEnabled;
        MenuRenderer.Draw(model, hit, _pressHit, hoverEnabled);
        int slot = hoverEnabled && hit.Kind == HitKind.Slot && model.Kinds[hit.Index] is not (SlotKind.Empty or SlotKind.Reserved) ? hit.Index : -1;
        if (slot != _hoverSlot)
        {
            _hoverSlot = slot;
            if (slot >= 0)
            {
                using var soundScope = new MeasureScope(StatId.WindowSound);
                SoundDefOf.Mouseover_Command.PlayOneShotOnCamera();
            }
        }

        if (slot >= 0 && _content!.Tip(slot) is { } tip)
        {
            float half = model.ButtonSize / 2f;
            Vec2 c = model.Slots[slot].Center;
            using var tipScope = new MeasureScope(StatId.WindowTip);
            TooltipHandler.TipRegion(new Rect(c.X - half, c.Y - half, model.ButtonSize, model.ButtonSize), new TipSignal(tip, TooltipIdBase + slot));
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
