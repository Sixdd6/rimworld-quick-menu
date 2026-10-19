using System;
using LudeonTK;
using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;
using UnityEngine;
using Verse;

namespace QuickMenu.Diagnostics;

/// <summary>
/// P-9 control run: an empty full-screen transparent window that makes the same <c>GUI</c>/<c>Widgets</c> calls per
/// frame as <see cref="Menu.QuickMenuWindow"/> (hub, one box and one fitted icon per slot) but no mod logic: no hit
/// test, no designators. Whatever it allocates is the engine/IMGUI baseline, not the mod's. It closes after
/// <see cref="Seconds"/> s, on any click or on Esc. Measured by <see cref="PerfCounters.ControlDraw"/>.
/// </summary>
internal sealed class ControlRunWindow : Window
{
    private const float ButtonSize = 56f;
    private const float Seconds = 10f;
    private const int SlotCount = 40; // P-8/P-9 reference size; the skeleton wheel draws far fewer

    private static readonly Color _slotColor = new(0.12f, 0.12f, 0.12f, 0.85f);
    private static readonly Color _hubColor = new(0f, 0f, 0f, 0.4f);

    private readonly float _endTime = Time.realtimeSinceStartup + Seconds;
    private readonly Rect[] _boxes;
    private readonly Rect[] _icons;
    private readonly Rect _hub;

    internal ControlRunWindow()
    {
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
        soundAppear = null;
        soundClose = null;

        Vec2 centre = new(UI.screenWidth / 2f, UI.screenHeight / 2f);
        SlotPlacement[] wheel = WheelLayout.Compute(new WheelParams(ButtonSize, 1, centre));
        _boxes = new Rect[SlotCount];
        _icons = new Rect[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            // Reuse the real ring positions, wrapping, so the draw calls cover a comparable screen area.
            Vec2 c = wheel.Length == 0 ? centre : wheel[i % wheel.Length].Center;
            _boxes[i] = new Rect(c.X - (ButtonSize / 2f), c.Y - (ButtonSize / 2f), ButtonSize, ButtonSize);
            float icon = ButtonSize * 0.75f;
            _icons[i] = new Rect(c.X - (icon / 2f), c.Y - (icon / 2f), icon, icon);
        }

        _hub = new Rect(centre.X - (ButtonSize / 2f), centre.Y - (ButtonSize / 2f), ButtonSize, ButtonSize);
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
        if (Time.realtimeSinceStartup >= _endTime)
        {
            Close();
        }
    }

    public override void DoWindowContents(Rect inRect)
    {
        using var scope = new MeasureScope(PerfCounters.ControlDraw);
        Event ev = Event.current;
        if (ev.type == EventType.Repaint)
        {
            Color old = GUI.color;
            try
            {
                GUI.color = _hubColor;
                GUI.DrawTexture(_hub, BaseContent.WhiteTex);
                for (int i = 0; i < _boxes.Length; i++)
                {
                    GUI.color = _slotColor;
                    GUI.DrawTexture(_boxes[i], BaseContent.WhiteTex);
                    GUI.color = old;
                    Widgets.DrawTextureFitted(_icons[i], BaseContent.BadTex, 1f);
                }
            }
            finally
            {
                GUI.color = old;
            }
        }
        else if (ev.type == EventType.MouseDown)
        {
            ev.Use();
            Close();
        }
    }

    [DebugAction("Quick Menu", "control window", allowedGameStates = AllowedGameStates.Playing)]
    private static void Open()
    {
        PerfCounters.ControlDraw.Reset();
        PerfCounters.Enabled = true;
        Find.WindowStack.Add(new ControlRunWindow());
        Log.Info("Quick Menu: control window open for " + (int)Seconds + " s; run 'perf report' afterwards.");
    }
}
