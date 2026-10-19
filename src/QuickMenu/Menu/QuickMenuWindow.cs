using System;
using QuickMenu.Catalog;
using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;
using QuickMenu.Core.Pins;
using QuickMenu.Core.Render;
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
    private const float IconScale = 0.75f;
    private const int Rings = 1;

    internal static readonly PatchSite DrawSite = new("QuickMenuWindow.DoWindowContents");

    private readonly int _openFrame;
    private readonly Map? _map;
    private readonly SlotPlacement[] _slots;
    private readonly Designator?[] _designators;
    private readonly int _count;
    private readonly Rect _discRect;
    private readonly Vector2 _centre;
    private readonly float _circleDrawSize;
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

        Textures.EnsureSized(ButtonSize, Rings);
        float discRadius = LayoutConstants.DiscRadius(ButtonSize, Rings);
        Vector2 mouse = UI.MousePositionOnUIInverted;
        float x = Mathf.Clamp(mouse.x, discRadius, UI.screenWidth - discRadius);
        float y = Mathf.Clamp(mouse.y, discRadius, UI.screenHeight - discRadius);
        SlotPlacement[] wheel = WheelLayout.Compute(new WheelParams(ButtonSize, Rings, new Vec2(x, y)));
        PinKey[] pins = SkeletonPins.Create();
        _slots = wheel;
        _designators = new Designator?[wheel.Length];
        for (int i = 0; i < pins.Length && i < wheel.Length; i++)
        {
            _designators[i] = DesignatorCatalog.Resolve(pins[i])?.Designator;
        }

        _count = pins.Length < wheel.Length ? pins.Length : wheel.Length;
        MenuController.LastSlotCount = _count;
        _centre = new Vector2(x, y);
        float discDraw = RoundTextureGeometry.DrawSizeForFixedMask(2f * discRadius, MaskSet.RoundSize);
        _discRect = new Rect(x - (discDraw / 2f), y - (discDraw / 2f), discDraw, discDraw);
        _circleDrawSize = RoundTextureGeometry.DrawSizeForFixedMask(ButtonSize, MaskSet.RoundSize);
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
        Vector2 mouse = ev.mousePosition;
        int hover = WheelHitTest.SlotAt(_slots, _count, new Vec2(mouse.x, mouse.y));

        if (ev.type == EventType.Repaint)
        {
            Color old = GUI.color;
            try
            {
                // Skeleton look until the T-29 renderer: disc, rim, hub and slots from the T-28 textures and tokens.
                if (Textures.Ready)
                {
                    GUI.color = QMStyle.DiscFill;
                    GUI.DrawTexture(_discRect, Textures.Mask(MaskId.Disc));
                    GUI.color = QMStyle.White;
                    Textures.DiscRim.Draw(_centre.x, _centre.y);
                    DrawSlotShape(_centre.x, _centre.y, hovered: false);
                }

                for (int i = 0; i < _count; i++)
                {
                    Vec2 c = _slots[i].Center;
                    if (Textures.Ready)
                    {
                        DrawSlotShape(c.X, c.Y, i == hover);
                    }

                    GUI.color = QMStyle.White;
                    Designator? d = _designators[i];
                    if (d != null)
                    {
                        DrawDesignatorIcon(d, SlotRect(i, IconScale));
                    }
                }
            }
            finally
            {
                GUI.color = old;
            }
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

    private void DrawSlotShape(float cx, float cy, bool hovered)
    {
        GUI.color = hovered ? QMStyle.SlotBgHover : QMStyle.SlotBg;
        GUI.DrawTexture(new Rect(cx - (_circleDrawSize / 2f), cy - (_circleDrawSize / 2f), _circleDrawSize, _circleDrawSize), Textures.Mask(MaskId.Circle));
        GUI.color = hovered ? QMStyle.Accent : QMStyle.SlotBorder;
        (hovered ? Textures.SlotRing2 : Textures.SlotRing1).Draw(cx, cy);
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
