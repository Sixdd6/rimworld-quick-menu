using System;
using QuickMenu.Catalog;
using QuickMenu.Core.Geometry;
using QuickMenu.Core.Input;
using QuickMenu.Core.Layout;
using QuickMenu.Core.Pins;
using QuickMenu.Diagnostics;
using QuickMenu.Skeleton;
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
    private const int Rings = 1;

    internal static readonly PatchSite DrawSite = new("QuickMenuWindow.DoWindowContents");

    private readonly int _openFrame;
    private readonly Map? _map;
    private readonly Designator?[] _designators;
    private readonly WheelModel _model;
    private readonly Vec2 _openMouse;
    private bool _hoverEnabled;
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

        // Skeleton until T-32: hard-coded pins, Fixed 1 ring, drawn by the T-29 renderer, edge-dodged with the plate.
        Vector2 mouse = UI.MousePositionOnUIInverted;
        _openMouse = new Vec2(mouse.x, mouse.y);
        float plateHeight = WheelModel.DefaultPlateHeight;
        Vec2 fit = EdgeDodger.Fit(MenuBounds.ForWheel(_openMouse, ButtonSize, Rings, plateHeight), new Vec2(UI.screenWidth, UI.screenHeight));
        Vec2 centre = _openMouse + fit;
        SlotPlacement[] wheel = WheelLayout.Compute(new WheelParams(ButtonSize, Rings, centre));
        PinKey[] pins = SkeletonPins.Create();
        _designators = new Designator?[wheel.Length];
        _model = new WheelModel(centre, ButtonSize, Rings, wheel, wheel.Length, hasHubSub: false, plateHeight);
        for (int i = 0; i < pins.Length && i < wheel.Length; i++)
        {
            Designator? d = DesignatorCatalog.Resolve(pins[i])?.Designator;
            _designators[i] = d;
            _model.SetSlot(i, d, SlotKind.Normal);
        }

        _model.SetHubLabels("QuickMenu.Menu.Close".Translate(), "QuickMenu.Menu.Edit".Translate());
        _model.Prepare();
        MenuController.LastSlotCount = pins.Length < wheel.Length ? pins.Length : wheel.Length;
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
                MenuController.Close();
            }
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

            using var scope = new MeasureScope(StatId.Window);
            Draw();
        }
        catch (Exception ex)
        {
            DrawSite.Fail(ex);
            SafeClose();
        }
    }

    private void Draw()
    {
        Event ev = Event.current;
        var mouse = new Vec2(ev.mousePosition.x, ev.mousePosition.y);
        HitLayout layout = _model.HitLayout;
        Hit hit = HitTester.Hit(mouse, in layout);
        if (!_hoverEnabled && mouse.Distance(_openMouse) >= TriggerStateMachine.HoverMoveThreshold)
        {
            _hoverEnabled = true;
        }

        int hover = hit.Kind == HitKind.Slot ? hit.Index : -1;
        if (ev.type == EventType.Repaint)
        {
            // The skeleton activates on mouse-down, so nothing is ever shown pressed until T-32.
            MenuRenderer.Draw(_model, hit, Hit.Outside, _hoverEnabled);
        }
        else if (ev.type == EventType.MouseDown)
        {
            if (hover >= 0 && ev.button == 0)
            {
                Designator? d = _designators[hover];
                if (d != null)
                {
                    // Known gap (T-26/T-65): the architect fires on mouse-up over the same slot and never activates
                    // Disabled designators; this skeleton fires on mouse-down.
                    MenuActivation.Enter();
                    try
                    {
                        d.ProcessInput(ev);
                    }
                    catch (Exception ex)
                    {
                        // A foreign designator's fault must not disable the whole menu (DrawSite stays enabled).
                        Type type = d.GetType();
                        Log.LogOnce($"Menu.Activation.{type.FullName}", $"Designator {type.FullName} threw during activation: {ex}");
                    }
                    finally
                    {
                        MenuActivation.Exit();
                    }
                }
            }

            ev.Use();
            SafeClose();
        }
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
